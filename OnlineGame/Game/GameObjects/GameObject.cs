using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnlineGame.Core.Interfaces;
using OnlineGame.Game.Interfaces;

namespace OnlineGame.Game.GameObjects
{
    public class GameObject : IGameObject
    {
        /// <summary>
        /// Can this object be picked up?
        /// </summary>
        public bool CanPickUp { get; set; } = true;
        /// <summary>
        /// Once picked up can this object be put down?
        /// </summary>
        public bool CanPutDown { get; set; } = true;
        /// <summary>
        /// What is the weight of this object (I thought an integer would be useful for players to 'guess' and never quite know)
        /// </summary>
        public int Weight { get; set; } = 0;

        /// <summary>
        ///  The actual weight they are carrying.
        /// </summary>
        public float RealWeight { get; set; } = 0f;
        
        public string Name { get; set; } = "Default Game Object"; // Defaults for safety
        public string ShortName { get; set; } = "Game Object";
        public string Description { get; set; } = "This is the default game object. You shouldn't see this.";

        public void Update()
        {

        }
    }

}
