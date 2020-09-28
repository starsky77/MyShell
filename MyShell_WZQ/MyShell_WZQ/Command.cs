using System;
using System.IO;
using System.Collections;
using System.Runtime.InteropServices;
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

        //存储所有内部指令的名称
        public static string[] builtin_str =
        {
            "bg","cd","clr","dir","echo" ,"exec","exit",
            "environ","fg","help","jobs" ,"pwd" ,"quit",
            "set","shift" ,"test","time" ,"umask","unset"
        };

        //通过C#的委托（即函数指针）来封装所有内部指令的函数
        public static builtin_fun[] builtin_com =
        {
            Command.bg, Command.cd, Command.clr,Command.dir,Command.echo,
            Command.exec,Command.exit,Command.environ,Command.fg,Command.help,
            Command.jobs,Command.pwd,Command.quit,Command.set,Command.shift,
            Command.test,Command.time,Command.umask,Command.unset
        };

        //返回内部指令的梳理
        public static int builtin_num()
        {
            return builtin_str.Length;
        }

    }

    //静态类，用于存储所有指令代码
    public static class Command
    {


        [DllImport("./umask.so", EntryPoint = "umask_C")]
        static extern int umask_C(int input);

        [DllImport("./CLib.so", EntryPoint = "fg_C")]
        static extern int fg_C(int pid);

        [DllImport("./CLib.so", EntryPoint = "bg_C")]
        static extern int bg_C(int pid);


        //字符串转为输出流
        public static StreamReader convert_str_stream(string str)
        {
            //转为字节
            byte[] array = Encoding.ASCII.GetBytes(str);
            //转为流
            MemoryStream stream = new MemoryStream(array);
            StreamReader result = new StreamReader(stream);
            return result;
        }

        //进程转到前台
        public static StreamReader bg(string[] args)
        {
            if (args[0] != "bg")
            {
                ConsoleHelper.WriteLine("ERROR:The comand is not bg", ConsoleColor.Red);
                return null;
            }
            else
            {
                //确保输入格式无误
                if (args.Length > 2)
                {
                    ConsoleHelper.WriteLine("ERROR:The comand has too many args!", ConsoleColor.Red);
                }
                else
                {
                    //作业号转为int形式
                    int PID = int.Parse(args[1]);
                    //调用C语言函数
                    int success = bg_C(PID);
                    //函数执行不成功，说明作业号不存在
                    if (success == 0)
                    {
                        ConsoleHelper.WriteLine("ERROR:The PID doesn't exist!", ConsoleColor.Red);
                    }

                }
            }
            return null;
        }

        //目录跳转
        public static StreamReader cd(string[] args)
        {
            if (args[0] != "cd")
            {
                ConsoleHelper.WriteLine("ERROR:The comand is not cd", ConsoleColor.Red);
                return null;
            }
            else
            {
                //无参数，回到主目录
                if (args.Length == 1)
                {
                    ShellConsole.PWD = ShellConsole.HOME;
                }
                //过多参数
                else if (args.Length > 2)
                {
                    ConsoleHelper.WriteLine("ERROR:The comand has too many args!", ConsoleColor.Red);
                }
                else
                {
                    string filePath = null;
                    //转到当前目录，通过以下方式来保证PWD末尾不会出现"/."
                    if (args[1] == ".")
                    {
                        filePath = Environment.CurrentDirectory + "/.";
                    }
                    //转到父目录，通过以下方式来保证PWD末尾不会出现"/.."
                    else if (args[1] == "..")
                    {
                        filePath = Environment.CurrentDirectory + "/..";
                    }
                    //一般情况
                    else
                    {
                        filePath = Environment.CurrentDirectory + "/" + args[1];
                    }
                    //不存在的目录
                    if (!Directory.Exists(filePath))
                    {
                        ConsoleHelper.WriteLine("ERROR:The Directory doesn't exist!", ConsoleColor.Red);
                    }
                    //确保无误后更改目录
                    else
                    {
                        Environment.CurrentDirectory = filePath;
                        ShellConsole.PWD = Environment.CurrentDirectory;
                    }

                }
                return null;
            }
        }

        //清屏
        public static StreamReader clr(string[] args)
        {
            if (args[0] != "clr")
            {
                ConsoleHelper.WriteLine("ERROR:The comand is not clr", ConsoleColor.Red);
                return null;
            }
            else
            {
                //直接调用函数即可
                Console.Clear();
                return null;
            }
        }

        //列出当前目录内的内容
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
                string [] files = null;
                //无参数，显示当前目录
                if (args.Length==1)
                {
                    files = Directory.GetFiles(ShellConsole.PWD);
                }
                //参数过多
                else if(args.Length>2)
                {
                    ConsoleHelper.WriteLine("ERROR:The comand has too many args", ConsoleColor.Red);
                    return null;
                }
                //有参数，以PWD为基准开始执行命令
                else
                {
                    files = Directory.GetFiles(ShellConsole.PWD + "/" + args[1]);
                }
                //将文件逐个添加进入输出字符串中
                foreach (var file in files)
                {
                    result_str += file;
                    result_str += "\n";
                }
                return convert_str_stream(result_str);
            }

        }

        //显示字符，其中多余的空格会简化为一个
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
                //将echo后面的参数集中起来
                Array.Copy(args, 1, output, 0, args.Length - 1);
                //通过" "将其连接并输出
                string result_str = string.Join(" ", output);
                return convert_str_stream(result_str);
            }

        }

        //exec目前无法处理外部指令
        //执行其他指令，随后退出该shell
        public static StreamReader exec(string[] args)
        {
            if (args[0] != "exec")
            {
                ConsoleHelper.WriteLine("ERROR:The comand is not exec", ConsoleColor.Red);
                return null;
            }
            else
            {
                //截取exec后方的指令
                string[] newCommand = new string[args.Length - 1];
                Array.Copy(args, 1, newCommand, 0, args.Length - 1);

                //处理内部指令
                for (int i = 0; i < ShellConsole.builtin_num(); i++)
                {
                    if (newCommand[0] == ShellConsole.builtin_str[i])
                    {
                        StreamReader builtin_stream = ShellConsole.builtin_com[i](newCommand);
                        return builtin_stream;
                    }
                }
                //退出当前进程
                Environment.Exit(0);
                return null;
            }
        }

        //退出指令，带有返回参数
        public static StreamReader exit(string[] args)
        {
            if (args[0] != "exit")
            {
                ConsoleHelper.WriteLine("ERROR:The comand is not exit", ConsoleColor.Red);
                return null;
            }
            else
            {
                //无参数则默认返回0
                if (args.Length == 1)
                {
                    Environment.Exit(0);
                }
                else if (args.Length > 2)
                {
                    ConsoleHelper.WriteLine("ERROR:The comand has too many args", ConsoleColor.Red);
                }
                //有参数则按照参数返回
                else
                {
                    Environment.Exit(Int32.Parse(args[1]));
                }
            }
            return null;
        }

        //列出所有环境变量
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
                //遍历所有环境变量
                foreach (DictionaryEntry var in Environment.GetEnvironmentVariables())
                {
                    string tmp = var.Key.ToString() + " = " + var.Value.ToString() + "\n";
                    str += tmp;
                }
                return convert_str_stream(str);
            }
        }

        //将后台任务进程调至前台继续运行
        public static StreamReader fg(string[] args)
        {
            if (args[0] != "fg")
            {
                ConsoleHelper.WriteLine("ERROR:The comand is not help", ConsoleColor.Red);
                return null;
            }
            else
            {
                //与bg指令的处理方式相同，通过调用C语言函数
                if (args.Length > 2)
                {
                    ConsoleHelper.WriteLine("ERROR:The comand has too many args!", ConsoleColor.Red);
                }
                else
                {
                    int PID = int.Parse(args[1]);
                    int success = fg_C(PID);
                    if (success == 0)
                    {
                        ConsoleHelper.WriteLine("ERROR:The PID doesn't exist!", ConsoleColor.Red);
                    }

                }
            }
            return null;
        }

        //调用用户手册
        public static StreamReader help(string[] args)
        {
            if (args[0] != "help")
            {
                ConsoleHelper.WriteLine("ERROR:The comand is not help", ConsoleColor.Red);
                return null;
            }
            else
            {

                StreamReader streamReader = new StreamReader("./help");
                return streamReader;
            }
        }

        //显示进程
        public static StreamReader jobs(string[] args)
        {
            if (args[0] != "jobs")
            {
                ConsoleHelper.WriteLine("ERROR:The comand is not jobs", ConsoleColor.Red);
                return null;
            }
            else
            {
                string result_str = "ID   ProcessName   MainWindowTitle   process.StartTime \n";
                //获取所有的进程
                Process[] processes = Process.GetProcesses();
                foreach (Process process in processes)
                {
                    //逐个输出进程信息
                    try
                    {
                        result_str += (process.Id + " " + process.ProcessName + " " + process.MainWindowTitle + " " + process.StartTime + "\n");
                    }
                    //存在无法输出的信息则显示错误信息
                    catch (Exception e)
                    {
                        result_str += e.Message;
                    }
                }
                return convert_str_stream(result_str);
            }
        }

        //显示当前目录
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

        //退出，无返回参数
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

        //该指令区别于一般的shell指令，set用于设置临时变量
        //语法格式:set X = Y 或 set X
        public static StreamReader set(string[] args)
        {
            bool success = false;
            if (args[0] != "set")
            {
                ConsoleHelper.WriteLine("ERROR:The comand is not set", ConsoleColor.Red);
                return null;
            }
            else
            {
                //单独输入set指令没有效果
                if (args.Length == 1) { }
                //如果没有第三个参数则将空内容赋值给变量
                else if (args.Length == 2)
                {

                    success = ShellConsole.variables.TryAdd(args[1], "");
                }
                //三个以上参数齐全的情况
                else
                {
                    //找到赋值号位置
                    int label = Array.FindIndex(args, x => x == "=");
                    //赋值号不存在则报错
                    if (label < 0)
                    {
                        ConsoleHelper.WriteLine("[ERROR]:Input does not match the format!", ConsoleColor.Red);
                    }
                    //将赋值号后的内容全部作为变量值传给变量
                    else
                    {
                        string Value = "";
                        for (int i = label + 1; i < args.Length; i++)
                        {
                            Value += args[i];
                        }
                        success = ShellConsole.variables.TryAdd(args[1], Value);
                    }
                }
            }
            //添加不成功则代表变量已经存在，报错
            if (!success)
            {

                ConsoleHelper.WriteLine("[ERROR]:The variable " + args[1] + " already exists!", ConsoleColor.Red);
            }
            return null;
        }

        public static StreamReader shift(string[] args)
        {
            if (args[0] != "shift")
            {
                ConsoleHelper.WriteLine("ERROR:The comand is not shift", ConsoleColor.Red);
                return null;
            }
            else
            {
                bool fir = true;
                int lasti = 0;
                int i;
                //依次查询所有诸如$1,$2的变量，直到变量不存在为止
                for (i = 1; ShellConsole.variables.ContainsKey(i.ToString()); i++)
                {
                    if (fir)
                    {
                        fir = false;
                    }
                    else
                    {
                        //后方变量值赋予前方变量
                        ShellConsole.variables[lasti.ToString()] = ShellConsole.variables[i.ToString()];
                    }
                    //存储上一个变量名
                    lasti = i;
                }
                //移除最后一个变量
                if (!fir)
                {
                    ShellConsole.variables.Remove(i.ToString());
                }
            }

            return null;
        }

        public static StreamReader test(string[] args)
        {
            StreamReader result = null;
            if (args[0] != "test")
            {
                ConsoleHelper.WriteLine("ERROR:The comand is not test", ConsoleColor.Red);
                return null;
            }
            else
            {
                //test语句的参数必须为3个：指令+选项+参数
                if (args.Length != 3)
                {
                    ConsoleHelper.WriteLine("[ERROR]:Input does not meet the requirements!", ConsoleColor.Red);
                }
                else
                {
                    switch (args[1])
                    {
                        //判断文件存在
                        case "-e":
                            //检测文件是否存在或目录是否存在
                            if(File.Exists(ShellConsole.PWD+"/"+args[2]) || Directory.Exists(ShellConsole.PWD + "/" + args[2]))
                            {
                                result = convert_str_stream("The input File/Directory exists!");
                            }
                            else
                            {
                                result = convert_str_stream("The input File/Directory doesn't exist!");
                            }
                            break;

                        //判断是否为普通文件
                        case "-F":
                            if (File.Exists(ShellConsole.PWD + "/" + args[2]))
                            {
                                result = convert_str_stream("This is a normal file!");
                            }
                            else
                            {
                                result = convert_str_stream("This isn't a normal file!");
                            }
                            break;

                        //等待扩充
                        default:
                            break;
                    }
                }
            }
            return result;
        }

        //显示时间
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

        //修改掩码
        public static StreamReader umask(string[] args)
        {
            if (args[0] != "umask")
            {
                ConsoleHelper.WriteLine("ERROR:The comand is not umask", ConsoleColor.Red);
                return null;
            }
            else
            {
                //没有参数输入，则直接返回当前umask码
                if (args.Length == 1)
                {
                    int result_int = umask_C(0);
                    string result_str = result_int.ToString();
                    return convert_str_stream(result_str);
                }
                //输入格式错误
                else if (args.Length != 2 || args[1].Length != 4)
                {
                    ConsoleHelper.WriteLine("[ERROR]:Input does not match the format!", ConsoleColor.Red);
                }
                //有参数输入则改变当前umask码
                else
                {
                    int input_int = int.Parse(args[1]);
                    umask_C(input_int);
                }
            }
            return null;
        }

        //清除特定变量
        public static StreamReader unset(string[] args)
        {
            if (args[0] != "unset")
            {
                ConsoleHelper.WriteLine("ERROR:The comand is not unset", ConsoleColor.Red);
                return null;
            }
            else
            {
                //输入不规范
                if (args.Length != 2)
                {
                    ConsoleHelper.WriteLine("[ERROR]:Input does not match the format!", ConsoleColor.Red);
                }
                else
                {
                    //清除不存在的变量
                    if (!ShellConsole.variables.ContainsKey(args[1]))
                    {
                        ConsoleHelper.WriteLine("[ERROR]:The variable " + args[1] + " doesn't exist!", ConsoleColor.Red);
                    }
                    //正常清除
                    else
                    {
                        ShellConsole.variables.Remove(args[1]);
                    }
                }
            }
            return null;
        }
    }

}
