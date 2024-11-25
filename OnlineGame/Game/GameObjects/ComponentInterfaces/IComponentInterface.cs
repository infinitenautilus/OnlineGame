using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineGame.Game.GameObjects.ComponentInterfaces
{
    public interface IComponentInterface
    {
        string Name { get; set; }
        void Initialize();
        void Update();

    }
}
