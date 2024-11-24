using System;
using System.Runtime.InteropServices;
using OnlineGame.Network;
using OnlineGame.Utility;

namespace OnlineGame.Core.Processes
{
    public class Program
    {
        public Program()
        {

        }

        public static void Main()
        {
            Console.WriteLine("Press enter to start.");

            Console.ReadLine();

            ConsoleManager cManager = new();
            cManager.Initialize();

            Console.WriteLine("Goodbye.");

        }
    }
}