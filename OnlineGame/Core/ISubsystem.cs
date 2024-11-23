using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnlineGame.Utility.Types;

namespace OnlineGame.Core
{
    public interface ISubsystem
    {
        string Name { get; }
        SubsystemState CurrentSystemState { get; }
        void Start();
        void Stop();

        event EventHandler<SystemEventArgs>? StateChanged;
    }
}
