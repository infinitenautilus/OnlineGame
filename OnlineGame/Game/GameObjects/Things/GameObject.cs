using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnlineGame.Game.Core.Types;
using OnlineGame.Game.Interfaces;

namespace OnlineGame.Game.GameObjects.Things
{
    public class GameObject : IGameObject
    {
        public bool IsGameObject { get; } = true;
        public string Name { get; set; } = "gameobject";
        public string LongName { get; set; } = "Game Object";
        public string Description { get; set; } = "The default game object class.";
        public List<string> Adjectives { get; set; } = [];
        public List<string> Nouns { get; set; } = [];
        public bool CanPickUp { get; set; } = true;
        public bool CanDrop { get; set; } = true;
        public bool CanAdjustSize { get; set; } = true;
        public GameObjectSize Size { get; set; } = GameObjectSize.Medium;
        public float RealWeight { get; set; } = 100f;

        public void Initialize()
        {

        }

        public void Update()
        {
        }
    }
}
