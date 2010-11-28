using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SolsticeVisualizer
{
    public class Room
    {
        public int[] Palette { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }

        public StaticEntity[] Entities { get; set; }

        public byte WindowMaskNW { get; set; }
        public byte WindowMaskNE { get; set; }

        public WallType WallNW { get; set; }
        public WallType WallNE { get; set; }

        public int Floor1Behavior { get; set; }
        public FloorCosmeticType Floor1Cosmetic { get; set; }
        public int Floor2Behavior { get; set; }
        public FloorCosmeticType Floor2Cosmetic { get; set; }

        public bool[,] FloorVisible { get; set; }

        public StaticBlock[] StaticBlocks { get; set; }

        public bool HasExitCeiling { get; set; }
        public bool HasExitFloor { get; set; }
        public bool HasExitNW { get; set; }
        public bool HasExitNE { get; set; }
        public bool HasExitSE { get; set; }
        public bool HasExitSW { get; set; }

        public RoomExit ExitCeiling { get; set; }
        public RoomExit ExitFloor { get; set; }
        public RoomExit ExitNW { get; set; }
        public RoomExit ExitNE { get; set; }
        public RoomExit ExitSE { get; set; }
        public RoomExit ExitSW { get; set; }

        public DynamicBlock[] DynamicBlocks { get; set; }

        public int RoomNumber { get; set; }
    }
}
