using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineGame.Utility
{
    public class SystemEventArgs(string type, string source, string message) : EventArgs
    {
        public string Type { get; } = type;
        public string Source { get; } = source;
        public string Message { get; } = message;        
    }
}
