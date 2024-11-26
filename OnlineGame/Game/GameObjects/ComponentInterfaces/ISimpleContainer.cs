using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnlineGame.Game.GameObjects.Things;
using OnlineGame.Utility.Types;

namespace OnlineGame.Game.GameObjects.ComponentInterfaces
{
    public interface ISimpleContainer
    {
        ThreadSafeList<GameObject> Contents { get; set; }
        float MaximumCarryingCapacity { get; }
        
        void Insert(GameObject item);
        void Withdraw(GameObject item);
        void EmptyContents();
        
    }
}
