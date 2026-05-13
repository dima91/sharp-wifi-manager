using System;

namespace WifiManager.Cli
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("WifiManager CLI started.");

            if (args.Length == 0)
            {
                Console.WriteLine("No arguments provided.");
                return;
            }

            Console.WriteLine("Arguments:");
            foreach (var arg in args)
            {
                Console.WriteLine($" - {arg}");
            }
        }
    }
}
