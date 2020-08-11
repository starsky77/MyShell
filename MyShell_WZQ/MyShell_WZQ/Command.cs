using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Linq;

namespace MyShell_WZQ
{
    public partial class ShellConsole
    {

        delegate StreamReader builtin_fun(string[] args);


        private static string[] builtin_str =
        {
            "bg","cd","clr","dir","echo" ,"exec","exit",
            "environ","fg","help","jobs" ,"pwd" ,"quit",
            "set","shift" ,"test","time" ,"umask","unset"
        };

        private static builtin_fun[] builtin_com =
        {
            Command.bg, Command.cd, Command.clr,Command.dir,Command.echo,
            Command.exec,Command.exit,Command.environ,Command.fg,Command.help,
            Command.jobs,Command.pwd,Command.quit,Command.set,Command.shift,
            Command.test,Command.time,Command.umask,Command.unset
        };

        private static int builtin_num()
        {
            return builtin_str.Length;
        }

    }
    public static class Command
    {
        public static StreamReader convert_str_stream(string str)
        {
            byte[] array = Encoding.ASCII.GetBytes(str);
            MemoryStream stream = new MemoryStream(array);
            StreamReader result = new StreamReader(stream);
            return result;
        }

        public static StreamReader bg(string[] args)
        {
            return null;
        }

        public static StreamReader cd(string[] args)
        {
            return null;
        }

        public static StreamReader clr(string[] args)
        {
            if (args[0] != "clr")
            {
                ConsoleHelper.WriteLine("ERROR:The comand is not clr", ConsoleColor.Red);
                return null;
            }
            else
            {
                Console.Clear();
                return null;
            }
        }

        public static StreamReader dir(string[] args)
        {
            StreamReader result = null;

            return result;
        }

        public static StreamReader echo(string[] args)
        {
            StreamReader result = null;

            return result;
        }

        public static StreamReader exec(string[] args)
        {
            return null;
        }

        public static StreamReader exit(string[] args)
        {
            return null;
        }

        public static StreamReader environ(string[] args)
        {
            MemoryStream stream = new MemoryStream();
            StreamReader result = null;
            string str = "";
            if (args[0] != "environ")
            {
                ConsoleHelper.WriteLine("ERROR:The comand is not environ", ConsoleColor.Red);
                return null;
            }
            else
            {
                foreach (DictionaryEntry var in Environment.GetEnvironmentVariables())
                {
                    string tmp = var.Key.ToString() + " = " + var.Value.ToString() + "\n";
                    str += tmp;
                }
                return convert_str_stream(str);
               
            }
        }

        public static StreamReader fg(string[] args)
        {
            return null;
        }

        public static StreamReader help(string[] args)
        {
            StreamReader result = null;

            return result;
        }

        public static StreamReader jobs(string[] args)
        {
            return null;
        }

        public static StreamReader pwd(string[] args)
        {
            StreamReader result = null;

            return result;
        }

        public static StreamReader quit(string[] args)
        {
            if (args[0] != "quit")
            {
                ConsoleHelper.WriteLine("ERROR:The comand is not quit", ConsoleColor.Red);
                return null;
            }
            else
            {
                ConsoleHelper.WriteLine("Exit successfully!", ConsoleColor.Red);
                Environment.Exit(0);
                return null;
            }
        }

        public static StreamReader set(string[] args)
        {
            return null;
        }

        public static StreamReader shift(string[] args)
        {
            return null;
        }

        public static StreamReader test(string[] args)
        {
            return null;
        }

        public static StreamReader time(string[] args)
        {

            if (args[0] != "time")
            {
                ConsoleHelper.WriteLine("ERROR:The comand is not time", ConsoleColor.Red);
                return null;
            }
            else
            {
                string result_str = DateTime.Now.ToString();
                return convert_str_stream(result_str);
            }
        }

        public static StreamReader umask(string[] args)
        {
            return null;
        }

        public static StreamReader unset(string[] args)
        {
            return null;
        }
    }

}
