using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Linq;
using System.Runtime.InteropServices;



namespace MyShell_WZQ
{
    public partial class ShellConsole
    {
        public static String PWD { get; set; }
        public static String HOME { get; set; }
        List<string> _pathVariables = new List<string>();
        public void MainLoop()
        {
            PWD = HOME = "C:/Users/39968/Documents/GitHub/MyShell/file/";
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

        private void Execute(string[] args)
        {
            bool isCommand = false;
            StreamReader builtin_stream = null;
            if (args[0] == null)
            {
                ConsoleHelper.WriteLine("Error:No command name!", ConsoleColor.Red);
            }
            else
            {
                for (int i = 0; i < builtin_num(); i++)
                {
                    if (args[0] == builtin_str[i])
                    {
                        builtin_stream = builtin_com[i](args);
                        isCommand = true;
                        break;
                    }
                }
                
                //当前版本下，同一个指令只能支持重定向和管道中的一个；
                HandleReDir(args, builtin_stream, isCommand);
                HandlePipe(args, builtin_stream, isCommand);

            }
        }


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
                    if(builtin_stream==null)
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
                    string []partition = new string[minIndex];
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

        private void HandlePipe(string[] args, StreamReader builtin_stream, bool hasBuiltinComm)
        {
            int start = 0;
            int foundIndex;
            string[] partition;
            StreamReader lastOutput = builtin_stream;
            if (hasBuiltinComm)
                start++;
            while (start < args.Length)
            {
                foundIndex = Array.FindIndex(args, start, x => x == "|");

                if (foundIndex < 0)
                {
                    return;
                    //foundIndex = args.Length;
                }

                partition = new string[foundIndex - start];
                Array.Copy(args, partition, foundIndex - start);

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
            if (isReturnOutput)
                process.StartInfo.RedirectStandardOutput = true;
            if (standardInput != null)
                process.StartInfo.RedirectStandardInput = true;
            process.Start();
            // push the output from the previous pipe into this process
            if (standardInput != null)
                process.StandardInput.Write(standardInput.ReadToEnd());

            //这段代码会造成重定向卡住
            //process.WaitForExit();// Waits here for the process to exit.

            if (isReturnOutput)
                return process.StandardOutput;
            return null;
        }


    }
}
