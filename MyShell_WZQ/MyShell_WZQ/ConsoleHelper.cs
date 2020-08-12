using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace MyShell_WZQ
{
    public static class ConsoleHelper
    {
        public static void WriteLine(string printLine,ConsoleColor color = ConsoleColor.White)
        {
            Write(printLine, color);

            Console.WriteLine();
        }
        public static void Write(string printLine, ConsoleColor color = ConsoleColor.White)
        {
            ConsoleColor defCol = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(printLine);
            Console.ForegroundColor = defCol;

        }

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
