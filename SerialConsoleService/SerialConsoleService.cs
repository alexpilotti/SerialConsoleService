using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace Cloudbase.SerialConsole.Service
{
    public partial class SerialConsoleService : ServiceBase
    {
        Thread t;
        volatile bool stop = false;

        public SerialConsoleService()
        {
            InitializeComponent();
            t = new Thread(Run);
        }

        void Run()
        {
            SerialConsoleManager scm = new SerialConsoleManager();
            while (!stop)
            {
                scm.HandleRequest();
            }
        }

        protected override void OnStart(string[] args)
        {
            stop = false;
            t.Start();
        }

        protected override void OnStop()
        {
            stop = true;
            if(!t.Join(4000))
                t.Interrupt();
        }
    }
}
