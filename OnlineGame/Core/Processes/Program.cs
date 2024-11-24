using System;
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

            SystemWizard.Instance.StartAll();

            Thread.Sleep(1000);

            SystemWizard.Instance.ListProcesses();

            Console.WriteLine($"{Scribe.IsShuttingDown}");

            string? response = string.Empty;

            while (string.IsNullOrEmpty(response))
            {
                SystemWizard.Instance.ListProcesses();
                response = Console.ReadLine();
                Console.WriteLine($"{Scribe.IsShuttingDown}");
            }

            SystemWizard.Instance.StopAll();
            SystemWizard.Instance.ListProcesses();

            Console.WriteLine("Goodbye.");
        }
    }
}