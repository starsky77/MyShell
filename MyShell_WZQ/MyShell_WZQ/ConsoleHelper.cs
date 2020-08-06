using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace MyShell_WZQ
{
    public static class ConsoleHelper
    {
        public static void WriteLine(string printLine,ConsoleColor color)
        {
            ConsoleColor defCol = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(printLine);
            Console.ForegroundColor = defCol;

            Console.WriteLine();
        }

        public static string ReadLine(ConsoleColor color)
        {
            ConsoleColor defaultColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            string readline = Console.ReadLine();
            Console.ForegroundColor = defaultColor;

            return readline;
        }

    }
}
