using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolsticeVisualizer
{
    public enum BlockCosmeticType : byte
    {
        Solid = 0,
        VerticalColumn = 1,
        RoundedStoneSlab = 2,
        ConveyerEW = 3,
        ConveyerNS = 4,
        TransparentOutlined = 5,
        SandwichBlock = 6,
        StoneSlabHemisphereTopCap = 8,
        StoneSlabHemisphereBottomCap = 9,
        PyramidSpikes = 10,
        TeleporterTop = 15,
        TeleporterPad = 16
    }
}
