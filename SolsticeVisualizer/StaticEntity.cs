using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolsticeVisualizer
{
    public class StaticEntity
    {
        public EntityType EntityType { get; private set; }

        public int X { get; private set; }
        public int Y { get; private set; }
        public int Z { get; private set; }

        public int Extra { get; private set; }

        public int Color1 { get; private set; }
        public int Color2 { get; private set; }

        public StaticEntity(EntityType entityType, int x, int y, int z, int extra, int color1, int color2)
        {
            EntityType = entityType;
            X = x;
            Y = y;
            Z = z;
            Extra = extra;
            Color1 = color1;
            Color2 = color2;
        }
    }
}
