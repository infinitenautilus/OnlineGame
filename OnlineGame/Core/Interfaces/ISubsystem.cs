using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnlineGame.Core.Types;
using OnlineGame.Utility.Types;

namespace OnlineGame.Core.Interfaces
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
