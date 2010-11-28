using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolsticeVisualizer
{
    public enum EntityType : byte
    {
        GruntEW = 2,                // room 17, grunt facing east
        Blob = 5,
        SolidStripedBlock = 7,
        MovingLift = 8,
        GruntNS = 14,
        GruntTurnableNS = 17,
        SlidingCrystalBall = 19,
        FloatingSpike = 24,
        SliderEW = 26,
        WalkingEyeball = 35,
        StriderNS = 40,
        SlugNS = 49,
        WalkingFootNS = 55,
        SolidCube = 129,
        Hemisphere = 130,
        TransparentCube = 131,
        ExtraLife = 203,
        Boots = 208,
        Key = 209,
        Key1 = 210,
        Key3 = 212,
        StaffPiece = 214,
        StaffPiece2 = 216,
        StaffPiece3 = 217,
        GreenPotion = 221,
        Credit = 235,
    }
}
