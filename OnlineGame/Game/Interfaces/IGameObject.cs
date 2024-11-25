using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnlineGame.Core.Interfaces;

namespace OnlineGame.Game.Interfaces
{
    public interface IGameObject : IUpdateable
    {
        public bool CanPickUp { get; set; }
        public bool CanPutDown { get; set; }
        public int Weight { get; set; }
        public float RealWeight { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string Description { get; set; }

    }
}
