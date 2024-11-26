using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnlineGame.Core.Interfaces;
using OnlineGame.Core.Types;
using OnlineGame.Game.GameObjects.ComponentInterfaces;

namespace OnlineGame.Game.Interfaces
{
    public interface IRoomObject : IDescriptiveComponent, IUpdateable, ISimpleContainer
    {
        PlayableRegions RoomRegion { get; set; }
        bool IsOutdoors { get; set; }

    }
}
