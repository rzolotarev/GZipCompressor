using System;

namespace GZipCompressor.Outputs
{
    public class ProgressBar
    {
        public static void Print(long current, long total, string message)
        {
            Console.CursorVisible = false;
            Console.CursorLeft = 0;
            
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Write(message + (long)(current * 100 / total) + "%");           
        }
    }
}
