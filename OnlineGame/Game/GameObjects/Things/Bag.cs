using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnlineGame.Game.Core.Types;
using OnlineGame.Game.GameObjects.ComponentInterfaces;
using OnlineGame.Game.Interfaces;
using OnlineGame.Utility.Types;

namespace OnlineGame.Game.GameObjects.Things
{
    public class Bag : IGameObject, ISimpleContainer
    {
        public float MaximumCarryingCapacity { get; set; } = 90f;

        public ThreadSafeList<GameObject> Contents { get; set; } = [];

        public GameObjectSize Size { get; set; } = GameObjectSize.Small;
        
        public float RealWeight { get; set; } = 5f;
        
        public bool CanPickUp { get; set; } = true;

        public bool CanDrop { get; set; } = true;
        public bool CanAdjustSize { get; set; } = true;

        public string Name { get; set; } = "bag";
        public string LongName { get; set; } = "simple bag";
        public string Description { get; set; } = "A small bag made from leather.";

        public void Withdraw(GameObject item)
        {

        }
        public void Insert(GameObject item)
        {

        }

        public void EmptyContents()
        {

        }

        public void Update()
        {

        }
    }
}
