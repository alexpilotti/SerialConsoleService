using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Security;
using System.Text;
using System.Threading;

namespace Cloudbase.SerialConsole
{
    class SerialConsoleManager
    {
        void WriteStream(Stream s, SerialPort port)
        {
            var buf = new byte[1024];
            var read = s.Read(buf, 0, buf.Length);
            Console.Write(System.Text.Encoding.UTF8.GetString(buf, 0, read));
            if (read > 0)
            {
                for (int i = 0; i < read; i++)
                {
                    port.BaseStream.Write(buf, i, 1);
                }
            }
        }

        string GetInputString(SerialPort port, bool echo = true)
        {
            var done = false;
            StringBuilder sb = new StringBuilder();
            while (!done)
            {
                var buf = new byte[1024];
                var read = port.BaseStream.Read(buf, 0, buf.Length);
                if (echo)
                    port.BaseStream.Write(buf, 0, read);

                // TODO check for newlines in the array
                if (buf[0] == 0xd)
                    done = true;
                else
                    sb.Append(Encoding.UTF8.GetString(buf, 0, read));
            }

            return sb.ToString();
        }

        SecureString GetInputSecureString(SerialPort port)
        {
            var ss = new SecureString();
            var c = 0;
            while (true)
            {
                c = port.ReadChar();
                if (c == 0xd)
                {
                    ss.MakeReadOnly();
                    return ss;
                }
                ss.AppendChar((char)c);
            }
        }

        void GetCredentials(SerialPort port, out string username, out SecureString password)
        {
            do
            {
                port.Write("Username: ");
                username = GetInputString(port);
                port.WriteLine("");
            }
            while (username.Length == 0);

            port.Write("Password: ");
            password = GetInputSecureString(port);
            port.WriteLine("");
        }

        public void HandleRequest()
        {
            SerialPort port = new SerialPort("COM1", 115200, Parity.None, 8, StopBits.One);
            try
            {
                port.Open();

                try
                {
                    string username;
                    SecureString password;
                    GetCredentials(port, out username, out password);

                    SpawnPS(port, username, password);

                    port.WriteLine("");
                    port.WriteLine("");

                    port.BaseStream.Flush();
                }
                catch (Exception ex)
                {
                    port.WriteLine(ex.Message);
                }
            }
            finally
            {
                port.Close();
            }
        }

        void SpawnPS(SerialPort port, string username, SecureString password)
        {
            string retMessage = String.Empty;
            ProcessStartInfo startInfo = new ProcessStartInfo();
            Process p = new Process();

            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardInput = true;
            startInfo.ErrorDialog = false;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            //startInfo.UserName = username;
            //startInfo.Password = password;
            startInfo.LoadUserProfile = true;

            startInfo.UseShellExecute = false;
            startInfo.Arguments = null;
            startInfo.FileName = "cmd.exe";

            p.StartInfo = startInfo;
            p.Start();

            Thread t1 = new Thread(() =>
            {
                while (!p.HasExited)
                    WriteStream(p.StandardOutput.BaseStream, port);
            });
            t1.Start();

            Thread t2 = new Thread(() =>
            {
                while (!p.HasExited)
                    WriteStream(p.StandardError.BaseStream, port);
            });
            t2.Start();

            Thread t3 = new Thread(() =>
            {
                while (!p.HasExited)
                {
                    var buf = new byte[1024];
                    var read = port.BaseStream.Read(buf, 0, buf.Length);

                    port.BaseStream.Write(buf, 0, read);
                    p.StandardInput.BaseStream.Write(buf, 0, read);
                    if (buf[0] == 0xd)
                        p.StandardInput.BaseStream.Write(new byte[] { 10 }, 0, 1);

                    p.StandardInput.BaseStream.Flush();
                    port.BaseStream.Flush();
                }
            });
            t3.Start();

            p.WaitForExit();

            t1.Join();
            t2.Join();
            t3.Join();
        }

    }
}
