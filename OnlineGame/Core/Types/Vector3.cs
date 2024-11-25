using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineGame.Core.Types
{
    public class Vector3(int x, int y, int z)
    {
        public int X { get; set; } = x;
        public int Y { get; set; } = y;
        public int Z { get; set; } = z;
    }
}
