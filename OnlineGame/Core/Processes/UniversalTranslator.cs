using System;
using System.Text.RegularExpressions;
using OnlineGame.Core.Types;
using OnlineGame.Utility.Types;
using OnlineGame.Core.Interfaces;
using OnlineGame.Network.Client;
using System.Text;

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
            // Standard Colors
            { "%^BLACK%^", "\x1B[30m" },    // Black
            { "%^RED%^", "\x1B[31m" },      // Red
            { "%^GREEN%^", "\x1B[32m" },    // Green
            { "%^YELLOW%^", "\x1B[33m" },   // Yellow
            { "%^BLUE%^", "\x1B[34m" },     // Blue
            { "%^MAGENTA%^", "\x1B[35m" },  // Magenta
            { "%^CYAN%^", "\x1B[36m" },     // Cyan
            { "%^WHITE%^", "\x1B[37m" },    // White

            // Bold Colors
            { "%^BOLD%^BLACK%^", "\x1B[1;30m" },    // Bold Black
            { "%^BOLD%^RED%^", "\x1B[1;31m" },      // Bold Red
            { "%^BOLD%^GREEN%^", "\x1B[1;32m" },    // Bold Green
            { "%^BOLD%^YELLOW%^", "\x1B[1;33m" },   // Bold Yellow
            { "%^BOLD%^BLUE%^", "\x1B[1;34m" },     // Bold Blue
            { "%^BOLD%^MAGENTA%^", "\x1B[1;35m" },  // Bold Magenta
            { "%^BOLD%^CYAN%^", "\x1B[1;36m" },     // Bold Cyan
            { "%^BOLD%^WHITE%^", "\x1B[1;37m" },    // Bold White

            // Background Colors
            { "%^BG_BLACK%^", "\x1B[40m" },   // Black Background
            { "%^BG_RED%^", "\x1B[41m" },     // Red Background
            { "%^BG_GREEN%^", "\x1B[42m" },   // Green Background
            { "%^BG_YELLOW%^", "\x1B[43m" },  // Yellow Background
            { "%^BG_BLUE%^", "\x1B[44m" },    // Blue Background
            { "%^BG_MAGENTA%^", "\x1B[45m" }, // Magenta Background
            { "%^BG_CYAN%^", "\x1B[46m" },    // Cyan Background
            { "%^BG_WHITE%^", "\x1B[47m" },   // White Background

            // Bright Colors
            { "%^BRIGHT%^BLACK%^", "\x1B[90m" },    // Bright Black (Gray)
            { "%^BRIGHT%^RED%^", "\x1B[91m" },      // Bright Red
            { "%^BRIGHT%^GREEN%^", "\x1B[92m" },    // Bright Green
            { "%^BRIGHT%^YELLOW%^", "\x1B[93m" },   // Bright Yellow
            { "%^BRIGHT%^BLUE%^", "\x1B[94m" },     // Bright Blue
            { "%^BRIGHT%^MAGENTA%^", "\x1B[95m" },  // Bright Magenta
            { "%^BRIGHT%^CYAN%^", "\x1B[96m" },     // Bright Cyan
            { "%^BRIGHT%^WHITE%^", "\x1B[97m" },    // Bright White

            // Bright Background Colors
            { "%^BG_BRIGHT%^BLACK%^", "\x1B[100m" },   // Bright Black Background
            { "%^BG_BRIGHT%^RED%^", "\x1B[101m" },     // Bright Red Background
            { "%^BG_BRIGHT%^GREEN%^", "\x1B[102m" },   // Bright Green Background
            { "%^BG_BRIGHT%^YELLOW%^", "\x1B[103m" },  // Bright Yellow Background
            { "%^BG_BRIGHT%^BLUE%^", "\x1B[104m" },    // Bright Blue Background
            { "%^BG_BRIGHT%^MAGENTA%^", "\x1B[105m" }, // Bright Magenta Background
            { "%^BG_BRIGHT%^CYAN%^", "\x1B[106m" },    // Bright Cyan Background
            { "%^BG_BRIGHT%^WHITE%^", "\x1B[107m" },   // Bright White Background

            // Reset
            { "%^RESET%^", "\x1B[0m" } // Reset
        };

        public string Name => "UniversalTranslator";

        public SubsystemState CurrentSystemState { get; private set; } = SubsystemState.Stopped;

        private static readonly char[] separator = [];

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
        public string TranslateMessageToANSI(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return message;

            foreach (var (tag, code) in _colorMap)
            {
                message = message.Replace(tag, code);
            }

            return message;
        }

        public static string CapitalizeFirstLetter(string input)
        {
            char first = input[0];
            return char.ToUpper(first) + input[1..];
        }

        public static string CleanTelnetInput(string input, int columns)
        {
            // Step 1: Remove unwanted control characters, keeping only newlines and carriage returns.
            string cleanedInput = new string(input.Where(c => !char.IsControl(c) || c == '\n' || c == '\r').ToArray()).Trim();

            // Step 2: Split the input into words.
            string[] words = cleanedInput.Split(separator, StringSplitOptions.RemoveEmptyEntries);

            // Step 3: Build the output with word wrapping.
            StringBuilder wrappedText = new();
            int currentLineLength = 0;

            foreach (string word in words)
            {
                // If adding this word would exceed the column limit, move to the next line.
                if (currentLineLength + word.Length > columns)
                {
                    wrappedText.AppendLine();
                    currentLineLength = 0;
                }

                // Add a space before the word if it's not the start of a new line.
                if (currentLineLength > 0)
                {
                    wrappedText.Append(' ');
                    currentLineLength++;
                }

                // Append the word and update the current line length.
                wrappedText.Append(word);
                currentLineLength += word.Length;
            }

            return wrappedText.ToString();
        }
    }
}
