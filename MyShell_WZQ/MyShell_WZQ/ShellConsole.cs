using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace MyShell_WZQ
{
    public partial class ShellConsole
    {
        public void MainLoop()
        {

            while (true)
            {
                string line = ConsoleHelper.ReadLine(ConsoleColor.Yellow);

                string[] args = Regex.Split(line, "\\s+(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
                for (int i = 0; i < args.Length; i++)
                {
                    args[i] = args[i].Trim('\"');
                }
                Execute(args);
            }
        }

        private void Execute(string[] args)
        {
            bool isCommand = false;
            if(args[0]==null)
            {
                ConsoleHelper.WriteLine("Error:No command name!", ConsoleColor.Red);
            }
            else
            {
                for(int i=0;i<builtin_num();i++)
                {
                    if (args[0] == builtin_str[i])
                    {
                        builtin_com[i](args);
                        isCommand = true;
                        break;
                    }
                    
                }
                if(!isCommand)
                    ConsoleHelper.WriteLine("Error:Non-existent instruction!", ConsoleColor.Red);

            }
        }


    }
}
