using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using OnlineGame.Core.Types;
using OnlineGame.Network;
using OnlineGame.Utility;

namespace OnlineGame.Core
{
    internal class ConsoleManager
    {
        private bool _mainMenuLoop = false;

        public void Initialize()
        {
            _mainMenuLoop = true;
            DefaultMenuLoop();
        }

        private void DefaultMenuLoop()
        {
            while (_mainMenuLoop)
            {
                PrintCommands();
                WritePrompt();

                string? userInput = Console.ReadLine()?.Trim().ToLower();

                if (string.IsNullOrEmpty(userInput))
                {
                    Console.WriteLine("Invalid input. Please try again.");
                    continue;
                }

                char command = userInput[0];

                switch (command)
                {
                    case 's':
                        AttemptStart();
                        break;
                    case 'h':
                        PrintCommands();
                        break;
                    case 'p':
                        SystemWizard.Instance.ListProcesses();
                        break;
                    case 't':
                        AttemptStop();
                        break;
                    case 'q':
                        QuitProgram();
                        return; // Exit the loop when quitting
                    default:
                        Console.WriteLine("Unknown command. Please try again.");
                        break;
                }
            }
        }


        private static void AttemptStart()
        {
            if(SystemWizard.Instance.CurrentState == SubsystemState.Stopped)
            {
                SystemWizard.Instance.StartAll();
            }
        }

        private static void AttemptStop()
        {
            if(SystemWizard.Instance.CurrentState != Types.SubsystemState.Running)
            {
                SystemWizard.Instance.Shutdown();
            }
        }

        private void QuitProgram()
        {
            _mainMenuLoop = false;
            AttemptStop();
        }

        private static void WritePrompt()
        {
            string prompt = $"{Constellations.TIMESTAMP} [SHPTQ]> ";

            if(Sentinel.Instance.CurrentSystemState == SubsystemState.Running)
            {
                prompt = $"{Constellations.TIMESTAMP} [SHPTQ][LISTENING]> ";
            }

            Console.Write(prompt);
        }

        private static void PrintCommands()
        {
            Scribe.Scry("(S)tart - Start");
            Scribe.Scry("(H)elp - This command");
            Scribe.Scry("(P)roccesses - check the status of necessary system processes");
            Scribe.Scry("s(T)op - Halt the entire MUD for some reason");
            Scribe.Scry("(Q)uit - Quit and Shutdown");
        }
    }
}
