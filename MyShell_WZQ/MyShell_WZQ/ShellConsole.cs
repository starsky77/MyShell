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
            switch(args[0])
            {
                
            }
        }


    }
}
