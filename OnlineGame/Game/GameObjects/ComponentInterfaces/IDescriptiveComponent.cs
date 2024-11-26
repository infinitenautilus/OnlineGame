using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineGame.Game.GameObjects.ComponentInterfaces
{
    public interface IDescriptiveComponent : IComponentInterface
    {
        string Name { get; set; }

        string LongName { get; set; }
        string Description { get; set; }
    }
}
