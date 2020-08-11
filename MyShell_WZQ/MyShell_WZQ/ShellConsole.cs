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
        String PWD = "C:/Users/39968/Documents/GitHub/MyShell/file/";
        List<string> _pathVariables = new List<string>();
        public void MainLoop()
        {
            string path = Environment.GetEnvironmentVariable("PATH");
            _pathVariables.AddRange(path.Split(';'));
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
            // bool isCommand = false;
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
                        //isCommand = true;
                        break;
                    }

                }
                //if(!isCommand)
                //    ConsoleHelper.WriteLine("Error:Non-existent instruction!", ConsoleColor.Red);

                HandleReDir(args, builtin_stream);
                HandlePipe(args, builtin_stream);
            }
        }


        private void HandleReDir(string[] args, StreamReader builtin_stream)
        {
            int inputReDirIndex, outputReDirIndex;
            string InputFile = null, OutputFile = null;

            inputReDirIndex = Array.FindIndex(args, x => (x == "<"));
            if (inputReDirIndex > 0)
                InputFile = args[inputReDirIndex + 1];

            outputReDirIndex = Array.FindIndex(args, x => (x == ">" || x == ">>"));
            if (outputReDirIndex > 0)
                OutputFile = args[outputReDirIndex + 1];

            //处理内部指令重定向
            if (builtin_stream != null)
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
                    StreamReader output_sw = LaunchOneProcess(args, inputStream, true);
                    string output_str = output_sw.ReadToEnd();
                    using(StreamWriter streamWriter=new StreamWriter(PWD+OutputFile))
                    {
                        streamWriter.WriteLine(output_str);
                    }
                }
                else
                {
                    //这部分交给管道处理
                    //LaunchOneProcess(args, inputStream, false);
                }
               
            }

        }

        private void HandlePipe(string[] args, StreamReader builtin_stream)
        {
            int start = 0;
            int foundIndex;
            string[] partition;
            StreamReader lastOutput = null;
            if (builtin_stream != null)
                start++;
            while (start < args.Length)
            {
                foundIndex = Array.FindIndex(args, start, x => x == "|");

                if (foundIndex < 0)
                {
                    foundIndex = args.Length;
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
                    catch (ExternalException) { }
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
            process.WaitForExit();// Waits here for the process to exit.

            if (isReturnOutput)
                return process.StandardOutput;
            return null;
        }


    }
}
