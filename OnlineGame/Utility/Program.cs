using System;
using OnlineGame.Network;
using OnlineGame.Utility.Wizards;

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

            sysWiz.StartAll();
            sysWiz.ListProcesses();
            Console.ReadLine();

            sysWiz.StopAll();
            sysWiz.ListProcesses();

            Console.WriteLine("Goodbye.");
        }
    }
}