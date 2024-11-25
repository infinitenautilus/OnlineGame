using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnlineGame.Core.Interfaces;

namespace OnlineGame.Game.GameObjects
{
    public class GameEntity(string name) : IUpdateable
    {
        public string Name { get; set; } = name ?? "Game Entity";

        public void Update()
        {

        }
    }
}
