using System;
using System.Runtime.InteropServices;
using MongoDB.Bson;
using MongoDB.Driver;
using OnlineGame.Network;
using OnlineGame.Utility;

namespace OnlineGame.Core
{
    public class Program
    {
        public Program()
        {

        }

        public static void Main()
        { 
            ConsoleManager cManager = new();
            cManager.Initialize();

            Console.WriteLine("Goodbye.");
        }
    }
}