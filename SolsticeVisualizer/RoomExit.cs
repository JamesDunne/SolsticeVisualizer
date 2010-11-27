using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolsticeVisualizer
{
    public class RoomExit
    {
        public int RoomNumber { get; private set; }

        public int W { get; private set; }
        public int Z { get; private set; }

        public RoomExit(int roomNumber, int w, int z)
        {
            RoomNumber = roomNumber;
            W = w;
            Z = z;
        }
    }
}
