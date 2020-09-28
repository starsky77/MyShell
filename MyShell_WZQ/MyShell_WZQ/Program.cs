using System;
using System.Runtime.InteropServices;
using MyShell_WZQ;

namespace Main_Program
{
    class Program
    {
        static void Main(string[] args)
        {
            //实例化shell并启动主循环
            ShellConsole shell = new ShellConsole();
            shell.MainLoop();
        }
    }
}
