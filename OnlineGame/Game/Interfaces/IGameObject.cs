using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnlineGame.Core.Interfaces;
using OnlineGame.Game.GameObjects.ComponentInterfaces;

namespace OnlineGame.Game.Interfaces
{
    public interface IGameObject : IUpdateable, IDescriptiveComponent, IMassProperties
    {
        static bool IsGameObject { get; }

    }
}
