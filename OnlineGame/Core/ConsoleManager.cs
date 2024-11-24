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
            DefaultMenuLoop(null);
        }

        private void DefaultMenuLoop(string? previousResponse)
        {
            if(_mainMenuLoop)
            {
                if (string.IsNullOrWhiteSpace(previousResponse))
                {
                    PrintCommands();
                    DefaultMenuLoop(Console.ReadLine());
                    return;
                }

                WritePrompt();

                char c = previousResponse.ToLower().Trim().ToCharArray()[0];

                switch(c)
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
                        break;
                    default:
                        SystemWizard.Instance.ListProcesses();
                        break;
                }

                DefaultMenuLoop(Console.ReadLine());
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

            SystemWizard.Instance.Shutdown();
        }

        private static void WritePrompt()
        {
            string prompt = $"{Constellations.TIMESTAMP} [SHPTQ]> ";

            if(Sentinel.Instance.CurrentSystemState == SubsystemState.Running)
            {
                prompt = $"{Constellations.TIMESTAMP} [SHPTQ][RUNNING]> ";
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
