using System;
using OnlineGame.Core.Types;

namespace OnlineGame.Utility.Types
{
    public class SystemEventArgs(string type, string source, string message) : EventArgs
    {
        private readonly string? _name;
        private readonly SubsystemState? _running;

        public SystemEventArgs(string name, SubsystemState running)
            : this("defaultType", "defaultSource", "defaultMessage") // Adjust default values as needed
        {
            
            _name = name;
            _running = running;
        }

        public string Type { get; } = type;
        public string Source { get; } = source;
        public string Message { get; } = message;
    }
}
