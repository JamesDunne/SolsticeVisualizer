using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;

namespace SolsticeVisualizer
{
    public class GameData
    {
        public byte[] RawImage;
        public int[] RoomIndicies;

        private GameData()
        {
        }

        public static GameData LoadFromRom(string path)
        {
            if (!File.Exists(path)) return null;

            GameData gd = new GameData();
            using (Stream str = File.OpenRead(path))
            {
                int a1, a2, a3, a4;
                a1 = str.ReadByte();
                a2 = str.ReadByte();
                a3 = str.ReadByte();
                a4 = str.ReadByte();

                // Check NES header:
                if ((a1 == 0x4E) && (a2 == 0x45) && (a3 == 0x53) && (a4 == 0x1A))
                {
                    // Strip off the header and get to the good stuff:
                    str.Seek(16, SeekOrigin.Begin);
                    gd.RawImage = new byte[str.Length - 16];
                    str.Read(gd.RawImage, 0, gd.RawImage.Length);
                }
                else
                {
                    // Read the raw image:
                    str.Seek(0, SeekOrigin.Begin);
                    gd.RawImage = new byte[str.Length];
                    str.Read(gd.RawImage, 0, gd.RawImage.Length);
                }
            }

            gd.getRoomIndicies();

            return gd;
        }

        private void getRoomIndicies()
        {
            int count = 0xFD;
            RoomIndicies = new int[count];
            for (int i = 0; i < count; ++i)
            {
                int idx = (int)(RawImage[0x18000 + i] | (RawImage[0x18000 + count + i] << 8));
                RoomIndicies[i] = idx + 0x10000;
            }
        }

        private int findWidth(int roomSize)
        {
            if (roomSize >= 0x00 && roomSize <= 0x08)
            {
                int x = roomSize;
                if ((x & 1) == 1)
                    return (7 - ((x + 1) >> 1));
                else
                    return 7;
            }
            else if (roomSize >= 0x09 && roomSize <= 0x0F)
            {
                int x = roomSize - 0x09;
                if ((x & 1) == 1)
                    return (6 - ((x + 1) >> 1));
                else
                    return 6;
            }
            else if (roomSize >= 0x10 && roomSize <= 0x14)
            {
                int x = roomSize - 0x10;
                if ((x & 1) == 1)
                    return (5 - ((x + 1) >> 1));
                else
                    return 5;
            }
            else if (roomSize >= 0x15 && roomSize <= 0x17)
            {
                int x = roomSize - 0x15;
                if ((x & 1) == 1)
                    return (4 - ((x + 1) >> 1));
                else
                    return 4;
            }
            else
            {
                return 3;
            }
        }

        private int findHeight(int roomSize)
        {
            if (roomSize >= 0x00 && roomSize <= 0x08)
            {
                int x = roomSize;
                if ((x & 1) == 0)
                    return (7 - (x >> 1));
                else
                    return 7;
            }
            else if (roomSize >= 0x09 && roomSize <= 0x0F)
            {
                int x = roomSize - 0x09;
                if ((x & 1) == 0)
                    return (6 - (x >> 1));
                else
                    return 6;
            }
            else if (roomSize >= 0x10 && roomSize <= 0x14)
            {
                int x = roomSize - 0x10;
                if ((x & 1) == 0)
                    return (5 - (x >> 1));
                else
                    return 5;
            }
            else if (roomSize >= 0x15 && roomSize <= 0x17)
            {
                int x = roomSize - 0x15;
                if ((x & 1) == 0)
                    return (4 - (x >> 1));
                else
                    return 4;
            }
            else
            {
                return 3;
            }
        }

        public Room ParseRoomData(int roomNumber)
        {
            if (roomNumber == 254)
            {
                return new Room
                {
                    RoomNumber = 254,
                    ExitSE = new RoomExit(243, 0, 0),
                    HasExitSE = true,
                    Width = 7,
                    Height = 7,
                    FloorVisible = new bool[7, 7] {
                        {true, true, true, true, true, true, true},
                        {true, true, true, true, true, true, true},
                        {true, true, true, true, true, true, true},
                        {true, true, true, true, true, true, true},
                        {true, true, true, true, true, true, true},
                        {true, true, true, true, true, true, true},
                        {true, true, true, true, true, true, true}
                    },
                    RenderFloor = new bool[7, 7] {
                        {true, true, true, true, true, true, true},
                        {true, true, true, true, true, true, true},
                        {true, true, true, true, true, true, true},
                        {true, true, true, true, true, true, true},
                        {true, true, true, true, true, true, true},
                        {true, true, true, true, true, true, true},
                        {true, true, true, true, true, true, true}
                    },
                    Palette = new int[3] { 43, 9, 17 },
                    WallNW = WallType.Logs4,
                    WallNE = WallType.Logs4,
                    Floor1Cosmetic = FloorCosmeticType.ForestDirt,
                    Floor1Behavior = 0,
                    Floor2Cosmetic = FloorCosmeticType.ForestDirt,
                    Floor2Behavior = 0,
                    Entities = new StaticEntity[0],
                    DynamicBlocks = new DynamicBlock[0],
                    StaticBlocks = new StaticBlock[0],
                };
            }

            using (var ms = new MemoryStream(RawImage, RoomIndicies[roomNumber], 1024))
            using (var br = new BinaryReader(ms))
            {
                byte[] k;

                Room rm = new Room();

                rm.RoomNumber = roomNumber;

                rm.Palette = new int[3];
                rm.Palette[0] = (int)br.ReadByte();
                rm.Palette[1] = (int)br.ReadByte();
                rm.Palette[2] = (int)br.ReadByte();

                byte roomSize = br.ReadByte();
                rm.Width = findWidth(roomSize);
                rm.Height = findHeight(roomSize);

                // Read entities:
                int entityCount = br.ReadByte();
                rm.Entities = new StaticEntity[entityCount];
                if (entityCount > 0)
                {
                    k = new byte[5];
                    for (int i = 0; i < entityCount; ++i)
                    {
                        br.Read(k, 0, 5);
                        rm.Entities[i] = new StaticEntity((EntityType)k[0], k[1] & 15, k[1] >> 4, k[2] & 15, k[2] >> 4, k[3], k[4]);
                    }
                }

                // Windows, walls, floors:
                rm.WindowMaskNW = br.ReadByte();
                rm.WindowMaskNE = br.ReadByte();

                rm.WallNW = (WallType)((br.ReadByte() >> 1) & 15);
                rm.WallNE = (WallType)((br.ReadByte() >> 1) & 15);

                byte flr = br.ReadByte();
                rm.Floor1Behavior = flr >> 4;
                rm.Floor1Cosmetic = (FloorCosmeticType)(flr & 15);

                flr = br.ReadByte();
                rm.Floor2Behavior = flr >> 4;
                rm.Floor2Cosmetic = (FloorCosmeticType)(flr & 15);

                // Floor visibility mask:
                k = new byte[7];
                br.Read(k, 0, 7);

                // Fill out floor visibility 2D array:
                rm.FloorVisible = new bool[rm.Height, rm.Width];
                rm.RenderFloor = new bool[rm.Height, rm.Width];
                for (int r = 0; r < rm.Height; ++r)
                    for (int c = 0; c < rm.Width; ++c)
                    {
                        rm.RenderFloor[r, c] = true;
                        rm.FloorVisible[rm.Height - r - 1, c] = (k[c] & (1 << (7 - r))) != 0;
                    }

                // Exits:
                byte exitMask = br.ReadByte();

                rm.HasExitFloor = (exitMask & (1 << 5)) != 0;
                rm.HasExitCeiling = (exitMask & (1 << 4)) != 0;
                rm.HasExitNW = (exitMask & (1 << 3)) != 0;
                rm.HasExitNE = (exitMask & (1 << 2)) != 0;
                rm.HasExitSE = (exitMask & (1 << 1)) != 0;
                rm.HasExitSW = (exitMask & (1 << 0)) != 0;

                int roomDest;
                byte position;

                // Ceiling and floor exits:

                if (rm.HasExitFloor)
                {
                    roomDest = br.ReadByte();
                    position = br.ReadByte();
                    rm.ExitFloor = new RoomExit(roomDest, 0, 0);
                }
                if (rm.HasExitCeiling)
                {
                    roomDest = br.ReadByte();
                    position = br.ReadByte();
                    rm.ExitCeiling = new RoomExit(roomDest, 0, 0);
                }

                // Position and roomDest bytes are in opposite order for walls:

                // Single exit per wall.

                if (rm.HasExitNW)
                {
                    position = br.ReadByte();
                    roomDest = br.ReadByte();
                    rm.ExitNW = new RoomExit(roomDest, findW(position), findZ(position));
                }
                if (rm.HasExitNE)
                {
                    position = br.ReadByte();
                    roomDest = br.ReadByte();
                    rm.ExitNE = new RoomExit(roomDest, findW(position), findZ(position));
                }
                if (rm.HasExitSE)
                {
                    position = br.ReadByte();
                    roomDest = br.ReadByte();
                    rm.ExitSE = new RoomExit(roomDest, findW(position), findZ(position));
                }
                if (rm.HasExitSW)
                {
                    position = br.ReadByte();
                    roomDest = br.ReadByte();
                    rm.ExitSW = new RoomExit(roomDest, findW(position), findZ(position));
                }

                // Blocks:
                int blockCount = br.ReadByte();
                rm.StaticBlocks = new StaticBlock[blockCount];
                if (blockCount > 0)
                {
                    k = new byte[4];
                    byte[] extra = new byte[12];
                    for (int i = 0; i < blockCount; ++i)
                    {
                        br.Read(k, 0, 4);
                        var blk = new StaticBlock((BlockCosmeticType)k[0], k[1] & 15, k[1] >> 4, k[2] & 15);
                        rm.StaticBlocks[i] = blk;
                        if (k[3] == 0x00)
                        {
                            br.Read(extra, 0, 12);
                        }

                        // Determine whether this block overrides a floor tile, to prevent things like spikes from
                        // shooting up into this block.
                        if ((blk.Z == 0) &&
                            // One of the solid block types:
                            ((blk.CosmeticType == BlockCosmeticType.Solid) ||
                             (blk.CosmeticType == BlockCosmeticType.ConveyerEW) ||
                             (blk.CosmeticType == BlockCosmeticType.ConveyerNS) ||
                             (blk.CosmeticType == BlockCosmeticType.PyramidSpikes) ||
                             (blk.CosmeticType == BlockCosmeticType.SandwichBlock)
                            ))
                        {
                            rm.RenderFloor[blk.Y, blk.X] = false;
                        }
                    }
                }

                // Dynamic blocks:
                blockCount = br.ReadByte();
                rm.DynamicBlocks = new DynamicBlock[blockCount];
                if (blockCount > 0)
                {
                    k = new byte[4];
                    byte[] extra = new byte[36];
                    for (int i = 0; i < blockCount; ++i)
                    {
                        br.Read(k, 0, 4);
                        var blk = new DynamicBlock((BlockCosmeticType)k[0], k[1] & 15, k[1] >> 4, k[2], (BlockFunctionalType)k[3]);
                        rm.DynamicBlocks[i] = blk;

                        // This extra data is PPU tile references for the NES:
                        if (blk.FunctionalType == BlockFunctionalType.DisappearsWhenTouched)
                        {
                            br.Read(extra, 0, 36);
                        }
                        else
                        {
                            br.Read(extra, 0, 12);
                        }

                        // Determine whether this block overrides a floor tile, to prevent things like spikes from
                        // shooting up into this block.
                        if ((blk.Z == 0) &&
                            (blk.FunctionalType != BlockFunctionalType.AppearsWhenTouched) &&
                            // One of the solid block types:
                            ((blk.CosmeticType == BlockCosmeticType.Solid) ||
                             (blk.CosmeticType == BlockCosmeticType.ConveyerEW) ||
                             (blk.CosmeticType == BlockCosmeticType.ConveyerNS) ||
                             (blk.CosmeticType == BlockCosmeticType.PyramidSpikes) ||
                             (blk.CosmeticType == BlockCosmeticType.SandwichBlock)
                            ))
                        {
                            rm.RenderFloor[blk.Y, blk.X] = false;
                        }
                    }
                }

                return rm;
            }
        }

        private int findZ(byte position)
        {
            return (position >> 3) & 7;
        }

        private int findW(byte position)
        {
            return (position & 7);
        }
    }
}
