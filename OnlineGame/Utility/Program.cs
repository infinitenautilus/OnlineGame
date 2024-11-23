using System;
using OnlineGame.Network;

namespace OnlineGame.Utility
{
    public class Program
    {
        public Program()
        {

        }

        public static void Main()
        {
            SystemWizard sysWiz = SystemWizard.Instance;

            Console.WriteLine("Press enter to start.");

            Console.ReadLine();

            sysWiz.StartAll();
            sysWiz.ListProcesses();

            string? response = string.Empty;

            while(string.IsNullOrEmpty(response))
            {
                sysWiz.ListProcesses();
                response = Console.ReadLine();
            }

            sysWiz.StopAll();
            sysWiz.ListProcesses();

            Console.WriteLine("Goodbye.");
        }
    }
}