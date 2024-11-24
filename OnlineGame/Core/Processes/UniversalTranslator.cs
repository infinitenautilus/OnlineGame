using System;
using System.Text.RegularExpressions;
using OnlineGame.Core.Types;
using OnlineGame.Utility.Types;
using OnlineGame.Core.Interfaces;

namespace OnlineGame.Core.Processes
{
    public class UniversalTranslator : ISubsystem
    {
        private static readonly Lazy<UniversalTranslator> _instance = new(() => new UniversalTranslator());

        // Private constructor ensures that the class cannot be instantiated externally.
        private UniversalTranslator() { }

        public static UniversalTranslator Instance => _instance.Value;

        private readonly Dictionary<string, string> _colorMap = new()
        {
            { "%^BLUE%^", "\x1B[34m" },  // Blue
            { "%^GREEN%^", "\x1B[32m" }, // Green
            { "%^RED%^", "\x1B[31m" },   // Red
            { "%^WHITE%^", "\x1B[37m" }, // White
            { "%^BOLD%^BLUE%^", "\x1B[1;34m" }, // Bold Blue
            { "%^BOLD%^GREEN%^", "\x1B[1;32m" }, // Bold Green
            { "%^BOLD%^RED%^", "\x1B[1;31m" },   // Bold Red
            { "%^BOLD%^WHITE%^", "\x1B[1;37m" }, // Bold White
            { "%^RESET%^", "\x1B[0m" }          // Reset
        };

        public string Name => "UniversalTranslator";

        public SubsystemState CurrentSystemState { get; private set; } = SubsystemState.Stopped;

        public event EventHandler<SystemEventArgs>? StateChanged;

        public void Start()
        {
            if (CurrentSystemState == SubsystemState.Running)
                return;

            CurrentSystemState = SubsystemState.Running;
            StateChanged?.Invoke(this, new SystemEventArgs(Name, SubsystemState.Running));
        }

        public void Stop()
        {
            if (CurrentSystemState == SubsystemState.Stopped)
                return;

            CurrentSystemState = SubsystemState.Stopped;
            StateChanged?.Invoke(this, new SystemEventArgs(Name, SubsystemState.Stopped));
        }

        /// <summary>
        /// Translates a message by replacing custom color tags with ASCII color codes.
        /// </summary>
        /// <param name="message">The input message containing custom tags.</param>
        /// <returns>The translated message with ASCII color codes.</returns>
        public string TranslateMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return message;

            foreach (var (tag, code) in _colorMap)
            {
                message = message.Replace(tag, code);
            }

            return message;
        }
    }
}
