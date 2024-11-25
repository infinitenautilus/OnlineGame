using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OnlineGame.Game.GameObjects.ComponentInterfaces
{
    public interface ICoordinatePosition : IComponentInterface
    {
        Vector3 Position { get; set; }
    }
}
