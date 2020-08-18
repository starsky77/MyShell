using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Linq;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Data;

namespace MyShell_WZQ
{
    public partial class ShellConsole
    {
        public static String PWD { get; set; }
        public static String HOME { get; set; }

        //存储所有临时变量
        public static Dictionary<string, string> variables = new Dictionary<string, string>();

        List<string> _pathVariables = new List<string>();
        public void MainLoop()
        {
            PWD = HOME = "./";
            string path = Environment.GetEnvironmentVariable("PATH");
            _pathVariables.AddRange(path.Split(';'));
            while (true)
            {
                ConsoleHelper.Write(PWD + ">");
                string line = ConsoleHelper.ReadLine(ConsoleColor.Yellow);

                string[] args = Regex.Split(line, "\\s+(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
                for (int i = 0; i < args.Length; i++)
                {
                    args[i] = args[i].Trim('\"');
                }
                Execute(args);
            }
        }


        public (StreamReader, bool) handledBuiltin(string[] args)
        {
            bool isBuiltinCommand = false;
            StreamReader builtin_stream = null;
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
                HandleQuote(args);
                //处理批量指令
                if (args[0] == "myshell")
                {
                    ExecuteFile(args[1], args);
                }
                else
                {
                    //当前版本下，同一个指令只能使用重定向和管道中的一个；
                    HandleReDir(args);
                    HandlePipe(args);
                }

            }
        }

        private void ExecuteFile(string filePath, string[] parameter)
        {
            StreamReader inputStream = new StreamReader(PWD + filePath);
            string line = inputStream.ReadLine();
            HandleArgs(parameter);
            while (line != null)
            {
                line = inputStream.ReadLine();
                string[] args = Regex.Split(line, "\\s+(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
                for (int i = 0; i < args.Length; i++)
                {
                    args[i] = args[i].Trim('\"');
                }
                Execute(args);
            }
            ClearArgs(parameter);
        }

        //将所有带有$的字符改为其对应数值
        private void HandleQuote(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                //替换键值对
                if(args[i][0]=='$')
                {
                    string key = args[i].Replace("$", string.Empty);
                    args[i] = variables[key];
                }
            }
        }

        private void HandleArgs(string[] parameter)
        {
            for (int i = 1; i < parameter.Length - 1; i++)
            {
                variables.Add(i.ToString(), parameter[i + 1]);
            }
        }

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


        //NOTICE：由于内部指令不支持输入重定向，输入重定向符不会对指令产生任何影响；
        private void HandleReDir(string[] args)
        {
            //检测重定向符号并找到输入输出的文件名
            int checkPipe = Array.FindIndex(args, x => x == "|");
            if (checkPipe > 0)
                return;
            int inputReDirIndex, outputReDirIndex;
            string InputFile = null, OutputFile = null;

            inputReDirIndex = Array.FindIndex(args, x => (x == "<"));
            if (inputReDirIndex > 0)
                InputFile = args[inputReDirIndex + 1];

            outputReDirIndex = Array.FindIndex(args, x => (x == ">" || x == ">>"));
            if (outputReDirIndex > 0)
                OutputFile = args[outputReDirIndex + 1];


            StreamReader inputStream = null;
            //输入重定向，从指定文件读取
            if (inputReDirIndex > 0)
            {
                inputStream = new StreamReader(PWD + InputFile);
            }
            //输出重定向
            if (outputReDirIndex > 0)
            {
                //找到最靠前的重定向符位置
                int minIndex;
                if (inputReDirIndex < 0)
                    minIndex = outputReDirIndex;
                else
                    minIndex = outputReDirIndex > inputReDirIndex ? inputReDirIndex : outputReDirIndex;

                //分离指令的重定向部分，将剩余部分传入内部指令
                string[] partition = new string[minIndex];
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
                    using (StreamWriter streamWriter = new StreamWriter(PWD + OutputFile, false))
                    {
                        streamWriter.WriteLine(output_str);
                    }

                }
                else if (args[outputReDirIndex] == ">>")
                {
                    //追加文件
                    using (StreamWriter streamWriter = new StreamWriter(PWD + OutputFile, true))
                    {
                        streamWriter.WriteLine(output_str);
                    }
                }
            }
            else
            {
                //无重定向符号，标准输入输出
                StreamReader output_sw = null;
                bool hasBulitin = false;
                (output_sw, hasBulitin) = handledBuiltin(args);
                if (!hasBulitin)
                {
                    LaunchOneProcess(args, inputStream, false);
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

        //old vision
        private void HandleReDir(string[] args, StreamReader builtin_stream, bool hasBuiltinComm)
        {
            int checkPipe = Array.FindIndex(args, x => x == "|");
            if (checkPipe > 0)
                return;
            int inputReDirIndex, outputReDirIndex;
            string InputFile = null, OutputFile = null;

            inputReDirIndex = Array.FindIndex(args, x => (x == "<"));
            if (inputReDirIndex > 0)
                InputFile = args[inputReDirIndex + 1];

            outputReDirIndex = Array.FindIndex(args, x => (x == ">" || x == ">>"));
            if (outputReDirIndex > 0)
                OutputFile = args[outputReDirIndex + 1];

            //处理内部指令重定向
            if (hasBuiltinComm)
            {
                //内部指令不支持输入重定向，报错
                if (inputReDirIndex > 0)
                {
                    ConsoleHelper.WriteLine("ERROR:builtin command doesn't supprot Input Redirect", ConsoleColor.Red);
                    return;
                }
                //对于输出重定向
                else if (outputReDirIndex > 0)
                {
                    if (args[outputReDirIndex] == ">")
                    {
                        using (StreamWriter streamWriter = new StreamWriter(PWD + OutputFile, false))
                        {
                            streamWriter.WriteLine(builtin_stream.ReadToEnd());
                        }

                    }
                    else if (args[outputReDirIndex] == ">>")
                    {
                        using (StreamWriter streamWriter = new StreamWriter(PWD + OutputFile, true))
                        {
                            streamWriter.WriteLine(builtin_stream.ReadToEnd());
                        }
                    }
                    else
                    {
                        ConsoleHelper.WriteLine("ERROR:Unrecognizable symbol", ConsoleColor.Red);
                    }
                }
                //无重定向，直接输出
                else
                {
                    //无输出的内部指令
                    if (builtin_stream == null)
                    {
                        return;
                    }
                    ConsoleHelper.WriteLine(builtin_stream.ReadToEnd());
                }
            }
            //处理外部指令重定向
            else
            {

                StreamReader inputStream = null;
                if (inputReDirIndex > 0)
                {
                    inputStream = new StreamReader(PWD + InputFile);
                }
                if (outputReDirIndex > 0)
                {
                    int minIndex;
                    if (inputReDirIndex < 0)
                        minIndex = outputReDirIndex;
                    else
                        minIndex = outputReDirIndex > inputReDirIndex ? inputReDirIndex : outputReDirIndex;
                    string[] partition = new string[minIndex];
                    Array.Copy(args, partition, minIndex);
                    StreamReader output_sw = LaunchOneProcess(partition, inputStream, true);
                    string output_str = output_sw.ReadToEnd();
                    if (args[outputReDirIndex] == ">")
                    {
                        using (StreamWriter streamWriter = new StreamWriter(PWD + OutputFile, false))
                        {
                            streamWriter.WriteLine(output_str);
                        }

                    }
                    else if (args[outputReDirIndex] == ">>")
                    {
                        using (StreamWriter streamWriter = new StreamWriter(PWD + OutputFile, true))
                        {
                            streamWriter.WriteLine(output_str);
                        }
                    }
                }
                else
                {
                    LaunchOneProcess(args, inputStream, false);
                }

            }

        }

        //由于内部指令不支持输入重定向，因此对于管道操作，如果存在内部指令则必须放在第一位
        private void HandlePipe(string[] args)
        {
            int start = 0;
            int foundIndex;
            string[] partition;
            StreamReader lastOutput = null;
            bool hasBulitin = false;

            foundIndex = Array.FindIndex(args, start, x => x == "|");
            if (foundIndex < 0)
            {
                return;
            }
            partition = new string[foundIndex - start];
            Array.Copy(args, start, partition, 0, foundIndex - start);
            (lastOutput, hasBulitin) = handledBuiltin(partition);
            if (hasBulitin)
            {
                start = foundIndex + 1;
            }
            while (start < args.Length)
            {
                foundIndex = Array.FindIndex(args, start, x => x == "|");

                if (foundIndex < 0)
                {
                    foundIndex = args.Length;
                }

                partition = new string[foundIndex - start];
                Array.Copy(args, start, partition, 0, foundIndex - start);

                bool isReturnOutput = true;
                if (foundIndex >= args.Length)
                    isReturnOutput = false;

                // PATH
                // string path = Environment.GetEnvironmentVariable(tokens[0]);
                lastOutput = LaunchOneProcess(partition, lastOutput, isReturnOutput);

                start = foundIndex + 1;
            }
        }

        private StreamReader LaunchOneProcess(string[] args, StreamReader standardInput, bool isReturnOutput)
        {
            string fullFilePath;
            StreamReader standardOutput = null;

            // check ./
            fullFilePath = Path.Combine(PWD, args[0]);

            try
            {
                standardOutput = StartProcess(fullFilePath, args, standardInput, isReturnOutput);
            }
            catch (ExternalException)
            {
                // check PATH
                bool isFound = false;
                foreach (string directory in _pathVariables)
                {
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
                    catch (ExternalException e)
                    {
                        //Console.WriteLine("ERROR message:{0}", e.Message);
                    }
                }
                if (!isFound)
                {
                    ConsoleHelper.WriteLine($"[Error] \"{args[0]}\" not found.", ConsoleColor.Red);
                    return standardOutput;
                }
            }
            return standardOutput;
        }


        private StreamReader StartProcess(string fullFilePath, string[] args, StreamReader standardInput, bool isReturnOutput)
        {
            Process process = new Process();
            // Configure the process using the StartInfo properties.
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
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = false;
            if (isReturnOutput)
                process.StartInfo.RedirectStandardOutput = true;
            if (standardInput != null)
                process.StartInfo.RedirectStandardInput = true;
            process.Start();
            // push the output from the previous pipe into this process
            if (standardInput != null)
                process.StandardInput.Write(standardInput.ReadToEnd());

            if (isReturnOutput)
                return process.StandardOutput;

            process.WaitForExit();// Waits here for the process to exit.
            return null;
        }


    }
}
