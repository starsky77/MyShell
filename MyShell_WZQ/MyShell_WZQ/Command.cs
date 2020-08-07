using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace MyShell_WZQ
{
    public partial class ShellConsole
    {

        delegate void builtin_fun(string[] args);


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
        public static void bg(string[] args)
        {

        }

        public static void cd(string[] args)
        {

        }

        public static void clr(string[] args)
        {
            if (args[0] != "clr")
            {
                ConsoleHelper.WriteLine("ERROR:The comand is not clr", ConsoleColor.Red);
                return;
            }
            else
            {
                Console.Clear();
            }
        }

        public static void dir(string[] args)
        {

        }

        public static void echo(string[] args)
        {

        }

        public static void exec(string[] args)
        {

        }

        public static void exit(string[] args)
        {

        }

        public static void environ(string[] args)
        {
            if (args[0] != "environ")
            {
                ConsoleHelper.WriteLine("ERROR:The comand is not environ", ConsoleColor.Red);
                return;
            }
            else
            {
                foreach (DictionaryEntry var in Environment.GetEnvironmentVariables())
                    Console.WriteLine("  {0} = {1}", var.Key, var.Value);
            }
        }

        public static void fg(string[] args)
        {

        }

        public static void help(string[] args)
        {

        }

        public static void jobs(string[] args)
        {

        }

        public static void pwd(string[] args)
        {

        }

        public static void quit(string[] args)
        {
            if (args[0] != "quit")
            {
                ConsoleHelper.WriteLine("ERROR:The comand is not quit", ConsoleColor.Red);
                return;
            }
            else
            {
                ConsoleHelper.WriteLine("Exit successfully!", ConsoleColor.Red);
                Environment.Exit(0);
            }
        }

        public static void set(string[] args)
        {

        }

        public static void shift(string[] args)
        {

        }

        public static void test(string[] args)
        {

        }

        public static void time(string[] args)
        {
            if (args[0] != "time")
            {
                ConsoleHelper.WriteLine("ERROR:The comand is not time", ConsoleColor.Red);
                return;
            }
            else
            {
                ConsoleHelper.WriteLine(DateTime.Now.ToString());
            }
        }

        public static void umask(string[] args)
        {
            
        }

        public static void unset(string[] args)
        {

        }
    }
    
}
