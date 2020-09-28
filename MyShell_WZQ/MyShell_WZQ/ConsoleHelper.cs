using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace MyShell_WZQ
{
    // ConsoleHelper用于辅助输入输出，实现带有颜色的输入与输出,默认颜色为白色
    public static class ConsoleHelper
    {
        //输出一行后换行
        public static void WriteLine(string printLine,ConsoleColor color = ConsoleColor.White)
        {
            Write(printLine, color);

            Console.WriteLine();
        }
        //输出一行后不换行
        public static void Write(string printLine, ConsoleColor color = ConsoleColor.White)
        {
            ConsoleColor defCol = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(printLine);
            Console.ForegroundColor = defCol;

        }
        //读取文字，颜色代表输入文字的颜色
        public static string ReadLine(ConsoleColor color = ConsoleColor.White)
        {
            ConsoleColor defaultColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            string readline = Console.ReadLine();
            Console.ForegroundColor = defaultColor;

            return readline;
        }

    }
}
