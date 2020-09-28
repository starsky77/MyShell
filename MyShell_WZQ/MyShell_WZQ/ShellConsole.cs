using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Linq;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Data;
using System.Threading;

namespace MyShell_WZQ
{
    public partial class ShellConsole
    {
        //以下两个变量用于存储环境变量PWD和HOME
        public static String PWD { get; set; }
        public static String HOME { get; set; }

        //存储所有临时变量
        public static Dictionary<string, string> variables = new Dictionary<string, string>();

        //用于存储环境变量PATH
        List<string> _pathVariables = new List<string>();
        public void MainLoop()
        {
            //环境变量读取或初始化
            //PWD = HOME = "./";
            PWD = HOME = Environment.CurrentDirectory;
            string path = Environment.GetEnvironmentVariable("PATH");
            _pathVariables.AddRange(path.Split(';'));
            //程序主循环，反复输入与命令处理
            while (true)
            {
                //输出命令提示符
                ConsoleHelper.Write(PWD + ">");
                string line = ConsoleHelper.ReadLine(ConsoleColor.Yellow);
                //通过正则表达式将输入分割为多个字符串
                string[] args = Regex.Split(line, "\\s+(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
                for (int i = 0; i < args.Length; i++)
                {
                    args[i] = args[i].Trim('\"');
                }
                //空指令，跳过
                if (args.Length == 1 && args[0] == "")
                    continue;
                //处理指令
                Execute(args);
            }
        }

        //处理内部指令，返回内部指令的返回值（没有则为null），以及是否具有内部指令
        public (StreamReader, bool) handledBuiltin(string[] args)
        {
            bool isBuiltinCommand = false;
            StreamReader builtin_stream = null;
            //for 循环来检查所有的内部指令
            for (int i = 0; i < builtin_num(); i++)
            {
                if (args[0] == builtin_str[i])
                {
                    builtin_stream = builtin_com[i](args);
                    isBuiltinCommand = true;
                    break;
                }
            }
            return (builtin_stream, isBuiltinCommand);
        }

        private void Execute(string[] args)
        {
            if (args[0] == null)
            {
                ConsoleHelper.WriteLine("Error:No command name!", ConsoleColor.Red);
            }
            else
            {
                //将带有$的变量进行替换
                bool existent = HandleQuote(args);
                if(!existent)
                {
                    return;
                }
                //处理批量指令
                if (args[0] == "myshell")
                {
                    ExecuteFile(args[1], args);
                }
                //单独处理指令
                else
                {
                    //当前版本下，同一个指令只能使用重定向和管道中的一个；
                    HandleReDir(args);
                    HandlePipe(args);
                }

            }
        }

        //处理批量指令
        private void ExecuteFile(string filePath, string[] parameter)
        {
            StreamReader inputStream = new StreamReader(PWD + "/" + filePath);
            string line = inputStream.ReadLine();
            //处理脚本参数
            HandleArgs(parameter);
            //逐行处理脚本命令
            while (line != null)
            {
                string[] args = Regex.Split(line, "\\s+(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
                for (int i = 0; i < args.Length; i++)
                {
                    args[i] = args[i].Trim('\"');
                }
                Execute(args);
                line = inputStream.ReadLine();
            }
            //清除脚本参数
            ClearArgs(parameter);
        }

        //将所有带有$的字符改为其对应数值
        private bool HandleQuote(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                //替换键值对
                if(args[i][0]=='$')
                {
                    string key = args[i].Replace("$", string.Empty);
                    if (variables.ContainsKey(key))
                    {
                        args[i] = variables[key];
                    }
                    //变量不存在，报错
                    else
                    {
                        ConsoleHelper.WriteLine("[ERROR]:The variable " + key + " doesn't exist!", ConsoleColor.Red);
                        return false;
                    }
                    
                }
            }
            return true;
        }

        //将所有传入的参数记录为$1,$2等格式
        private void HandleArgs(string[] parameter)
        {
            for (int i = 1; i < parameter.Length - 1; i++)
            {
                variables.Add(i.ToString(), parameter[i + 1]);
            }
        }

        //清除所有传入的参数
        private void ClearArgs(string[] parameter)
        {
            for (int i = 1; i < parameter.Length - 1; i++)
            {
                if (!variables.ContainsKey(i.ToString()))
                {
                    break;
                }
                variables.Remove(i.ToString());
            }
        }

        //重定向指令处理
        //NOTICE：由于内部指令不支持输入重定向，输入重定向符不会对指令产生任何影响；
        private void HandleReDir(string[] args)
        {
            //如果指令中存在管道符号则跳过该函数
            int checkPipe = Array.FindIndex(args, x => x == "|");
            if (checkPipe > 0)
                return;
            int inputReDirIndex, outputReDirIndex;
            string InputFile = null, OutputFile = null;

            //检测重定向符号并找到输入输出的文件名
            inputReDirIndex = Array.FindIndex(args, x => (x == "<"));
            if (inputReDirIndex > 0)
                InputFile = args[inputReDirIndex + 1];

            outputReDirIndex = Array.FindIndex(args, x => (x == ">" || x == ">>"));
            if (outputReDirIndex > 0)
                OutputFile = args[outputReDirIndex + 1];


            StreamReader inputStream = null;
            //用于分离重定向符号和指令本身
            int minIndex = -1;
            string[] partition;
            //输入重定向，从指定文件读取
            if (inputReDirIndex > 0)
            {
                inputStream = new StreamReader(PWD + "/" + InputFile);
                minIndex = inputReDirIndex;
            }
            //输出重定向
            if (outputReDirIndex > 0)
            {
                //找到最靠前的重定向符位置
                if (inputReDirIndex < 0)
                    minIndex = outputReDirIndex;
                else
                    minIndex = outputReDirIndex > inputReDirIndex ? inputReDirIndex : outputReDirIndex;

                //分离指令的重定向部分，将剩余部分传入内部指令
                partition = new string[minIndex];
                Array.Copy(args, partition, minIndex);
                StreamReader output_sw = null;
                bool hasBulitin = false;
                (output_sw, hasBulitin) = handledBuiltin(partition);
                if (!hasBulitin)
                {
                    //不是内部指令，进行外部指令查询
                    output_sw = LaunchOneProcess(partition, inputStream, true);
                }
                string output_str = output_sw.ReadToEnd();
                if (args[outputReDirIndex] == ">")
                {
                    //覆盖文件
                    using (StreamWriter streamWriter = new StreamWriter(PWD + "/" + OutputFile, false))
                    {
                        streamWriter.WriteLine(output_str);
                    }

                }
                else if (args[outputReDirIndex] == ">>")
                {
                    //追加文件
                    using (StreamWriter streamWriter = new StreamWriter(PWD + "/" + OutputFile, true))
                    {
                        streamWriter.WriteLine(output_str);
                    }
                }
            }
            //无重定向符号，标准输入输出
            else
            {
                //在无输出重定向的情况下，也可能存在输入重定向，此处要将输入重定向的部分分离出
                if(minIndex > 0)
                {
                    partition = new string[minIndex];
                    Array.Copy(args, partition, minIndex);
                }
                //无重定向
                else
                {
                    partition = args;
                }

                StreamReader output_sw = null;
                bool hasBulitin = false;
                (output_sw, hasBulitin) = handledBuiltin(partition);
                if (!hasBulitin)
                {
                    LaunchOneProcess(partition, inputStream, false);
                }
                else
                {
                    //没有输出的指令
                    if (output_sw == null)
                    {
                        return;
                    }
                    //有输出的指令
                    ConsoleHelper.WriteLine(output_sw.ReadToEnd());
                }

            }
        }
        

        //处理管道操作，当程序没有进行重定向时会运行管道处理函数
        //由于内部指令不支持输入重定向，因此如果参与管道，则必须放置于第一条指令
        private void HandlePipe(string[] args)
        {
            int start = 0;
            int foundIndex;
            string[] partition;
            StreamReader lastOutput = null;
            bool hasBulitin = false;

            foundIndex = Array.FindIndex(args, start, x => x == "|");
            //无管道符号则返回
            if (foundIndex < 0)
            {
                return;
            }
            //首先处理第一个指令，先测试其是否为内部指令
            partition = new string[foundIndex - start];
            Array.Copy(args, start, partition, 0, foundIndex - start);
            //如果存在内部指令，则lastOutput为内部指令的输出，否则为null
            (lastOutput, hasBulitin) = handledBuiltin(partition);
            //存在内部指令，从下一个管道符号开始执行外部指令
            if (hasBulitin)
            {
                start = foundIndex + 1;
            }
            //处理外部指令
            while (start < args.Length)
            {
                //不断寻找管道符号
                foundIndex = Array.FindIndex(args, start, x => x == "|");

                if (foundIndex < 0)
                {
                    foundIndex = args.Length;
                }

                //将指令单独分出，并执行
                partition = new string[foundIndex - start];
                Array.Copy(args, start, partition, 0, foundIndex - start);

                //进程是否需要重定向
                bool isReturnOutput = true;
                if (foundIndex >= args.Length)
                    isReturnOutput = false;

                // PATH
                //调用进程执行外部指令
                lastOutput = LaunchOneProcess(partition, lastOutput, isReturnOutput);

                start = foundIndex + 1;
            }
        }

        //调用进程
        private StreamReader LaunchOneProcess(string[] args, StreamReader standardInput, bool isReturnOutput)
        {
            string fullFilePath;
            StreamReader standardOutput = null;

            //首先检查指令是否存在与当前目录下
            fullFilePath = Path.Combine("./", args[0]);
            try
            {
                standardOutput = StartProcess(fullFilePath, args, standardInput, isReturnOutput);
            }
            //指令不存在与当前目录下
            catch (ExternalException)
            {
                //遍历所有PATH变量，查询外部指令
                bool isFound = false;
                
                foreach (string directory in _pathVariables)
                {
                    //获取完整路径
                    fullFilePath = Path.Combine(directory, args[0]);
                    if (isFound)
                    {
                        return standardOutput;
                    }
                    try
                    {
                        standardOutput = StartProcess(fullFilePath, args, standardInput, isReturnOutput);
                        isFound = true;
                    }
                    //出现错误，即该目录下不存在该指令，则跳入下一个目录查询
                    catch (ExternalException e)
                    {
                        //Console.WriteLine("ERROR message:{0}", e.Message);
                    }
                }
                //不存在该外部指令
                if (!isFound)
                {
                    ConsoleHelper.WriteLine($"[Error] \"{args[0]}\" not found.", ConsoleColor.Red);
                    return standardOutput;
                }
            }
            return standardOutput;
        }


        //启动进程
        private StreamReader StartProcess(string fullFilePath, string[] args, StreamReader standardInput, bool isReturnOutput)
        {
            Process process = new Process();
            //通过StartInfo的调整来启动进程
            process.StartInfo.FileName = fullFilePath;
            bool isFirstToken = true;
            foreach (string arg in args)
            {
                if (isFirstToken)
                {
                    isFirstToken = false;
                    continue;
                }
                process.StartInfo.ArgumentList.Add(arg);
            }
            //更具MC官方文档，UseShellExecute = false来避免在重定向时报错
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = false;
            //启动输出重定向
            if (isReturnOutput)
                process.StartInfo.RedirectStandardOutput = true;
            //启动输入重定向
            if (standardInput != null)
                process.StartInfo.RedirectStandardInput = true;
            process.Start();
            //将上一个进程的输入传入该进程
            if (standardInput != null)
                process.StandardInput.Write(standardInput.ReadToEnd());

            if (isReturnOutput)
                return process.StandardOutput;

            //等待进程结束
            process.WaitForExit();
            return null;
        }


    }
}
