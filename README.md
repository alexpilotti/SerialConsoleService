SerialConsoleService
====================

Attaches a command prompt to a serial console, excellent for troubleshooting a virtual machine 
without graphical console access.

As an example on Hyper-V you can use a simple Powershell script on the host:

function Get-ComPortConsole($vmname) {
    $pipename = "\\.\pipe\" + [guid]::NewGuid().ToString()
    Set-VMComPort $vmname -Number 1 -Path $pipename
    C:\Tools\putty.exe -serial $pipename
}

Note: this requires putty.exe available in C:\Tools, modify the script to match your environment.

Get-ComPortConsole "VM_Name"

This will start Putty and connect it to the COM1 port of your VM. On the VM side, the 
SerialConsoleService spawns a command prompt and redirects the serial I/O to teh process.


Install
=======

Add .Net x64 if not already configured:

Add-WindowsFeature Net-Framework-Core

Install and start the service:

%SystemRoot%\Microsoft.Net\Framework64\v2.0.50727\InstallUtil.exe SerialConsoleService.exe
net start SerialConsoleService
