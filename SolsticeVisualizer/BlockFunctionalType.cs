using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolsticeVisualizer
{
    public enum BlockFunctionalType : byte
    {
        ConveyerEW = 0,
        ConveyerNS = 1,
        DisappearsWhenTouched = 4,
        AppearsWhenTouched = 5,
        Teleport = 7
    }
}
