using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.VisualBasic.FileIO;

namespace MyShell_WZQ
{
    public partial class ShellConsole
    {

        public delegate StreamReader builtin_fun(string[] args);


        public static string[] builtin_str =
        {
            "bg","cd","clr","dir","echo" ,"exec","exit",
            "environ","fg","help","jobs" ,"pwd" ,"quit",
            "set","shift" ,"test","time" ,"umask","unset"
        };

        public static builtin_fun[] builtin_com =
        {
            Command.bg, Command.cd, Command.clr,Command.dir,Command.echo,
            Command.exec,Command.exit,Command.environ,Command.fg,Command.help,
            Command.jobs,Command.pwd,Command.quit,Command.set,Command.shift,
            Command.test,Command.time,Command.umask,Command.unset
        };

        public static int builtin_num()
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
            if (args[0] != "cd")
            {
                ConsoleHelper.WriteLine("ERROR:The comand is not cd", ConsoleColor.Red);
                return null;
            }
            else
            {
                if(args.Length==1)
                {
                    ShellConsole.PWD = ShellConsole.HOME;
                }
                else if(args.Length>2)
                {
                    ConsoleHelper.WriteLine("ERROR:The comand has too many args!", ConsoleColor.Red);
                }
                else
                {
                    string filePath = null;
                    if (args[1] == ".") { }
                    else if(args[1]=="..")
                    {
                        //目前在处理..时存在问题
                        //filePath = Regex.Replace(ShellConsole.PWD, "/*/&", "/");
                    }
                    else
                    {
                        filePath = ShellConsole.PWD + args[1] + "/";
                    }
                    
                    if(!Directory.Exists(filePath))
                    {
                        ConsoleHelper.WriteLine("ERROR:The Directory doesn't exist!", ConsoleColor.Red);
                    }
                    else
                    {
                        ShellConsole.PWD = filePath;
                    }

                }
                return null;
            }
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
            if (args[0] != "dir")
            {
                ConsoleHelper.WriteLine("ERROR:The comand is not dir", ConsoleColor.Red);
                return null;
            }
            else
            {
                string result_str = "";
                var files = Directory.GetFiles(ShellConsole.PWD);
                foreach(var file in files)
                {
                    result_str += file;
                    result_str += "\n";
                }
                return convert_str_stream(result_str);
            }
            
        }

        public static StreamReader echo(string[] args)
        {
            if (args[0] != "echo")
            {
                ConsoleHelper.WriteLine("ERROR:The comand is not echo", ConsoleColor.Red);
                return null;
            }
            else
            {
                string[] output = new string[args.Length - 1];
                Array.Copy(args, 1, output, 0, args.Length - 1);
                string result_str = string.Join(" ", output);
                return convert_str_stream(result_str);
            }
            
        }

        public static StreamReader exec(string[] args)
        {
            if (args[0] != "exec")
            {
                ConsoleHelper.WriteLine("ERROR:The comand is not exec", ConsoleColor.Red);
                return null;
            }
            else
            {
                string[] newCommand = new string[args.Length - 1];
                Array.Copy(args, 1, newCommand, 0, args.Length - 1);

                for (int i = 0; i < ShellConsole.builtin_num(); i++)
                {
                    if (newCommand[0] == ShellConsole.builtin_str[i])
                    {
                        StreamReader builtin_stream = ShellConsole.builtin_com[i](newCommand);
                        return builtin_stream;
                    }
                }
                return null;
            }
        }

        public static StreamReader exit(string[] args)
        {
            if (args[0] != "exit")
            {
                ConsoleHelper.WriteLine("ERROR:The comand is not exit", ConsoleColor.Red);
                return null;
            }
            else
            {
                if(args.Length==1)
                {
                    Environment.Exit(0);
                }
                else if(args.Length>2)
                {
                    ConsoleHelper.WriteLine("ERROR:The comand has too many args", ConsoleColor.Red);
                }
                else
                {
                    Environment.Exit(Int32.Parse(args[1]));
                }
            }
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
            if (args[0] != "pwd")
            {
                ConsoleHelper.WriteLine("ERROR:The comand is not pwd", ConsoleColor.Red);
                return null;
            }
            else
            {
                StreamReader result = null;
                result = convert_str_stream(ShellConsole.PWD);
                return result;
            }
            
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
