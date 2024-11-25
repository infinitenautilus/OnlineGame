using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnlineGame.Game.Core.Types;

namespace OnlineGame.Game.GameObjects.ComponentInterfaces
{
    public interface IMassProperties : IComponentInterface
    {
        bool CanPickUp { get; set; }
        bool CanDrop { get; set; }

        bool CanAdjustSize { get; set; }

        GameObjectSize Size { get; set; }

        float RealWeight { get; set; }

    }
}
