using System;
using System.Collections.Generic;
using System.Text;

namespace SolsticeVisualizer
{
    public class DynamicBlock
    {
        public BlockCosmeticType CosmeticType { get; private set; }
        public BlockFunctionalType FunctionalType { get; private set; }

        public int X { get; private set; }
        public int Y { get; private set; }
        public int Z { get; private set; }

        public DynamicBlock(BlockCosmeticType cosmeticType, int x, int y, int z, BlockFunctionalType functionalType)
        {
            if (x >= 8) throw new ArgumentOutOfRangeException("x");
            if (y >= 8) throw new ArgumentOutOfRangeException("y");
            if (z >= 8) throw new ArgumentOutOfRangeException("z");

            CosmeticType = cosmeticType;
            X = x;
            Y = y;
            Z = z;
            FunctionalType = functionalType;
        }
    }
}
