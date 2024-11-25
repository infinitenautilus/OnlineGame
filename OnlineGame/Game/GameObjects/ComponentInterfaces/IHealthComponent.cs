using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineGame.Game.GameObjects.ComponentInterfaces
{
    public interface IHealthComponent : IComponentInterface
    {
        int CurrentHealth { get; set; }
        int MaximumHealth { get; set; }

        void AdjustHealth(int amount);
    }
}
