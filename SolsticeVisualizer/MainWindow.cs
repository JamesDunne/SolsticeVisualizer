using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform;
using System.Drawing;

namespace SolsticeVisualizer
{
    class MainWindow : GameWindow
    {
        //const int firstRoom = 0;
        const int firstRoom = 15;
        //const int firstRoom = 198;

        const float rotation_speed = 15.0f;
        float angle;

        GameData gameData = null;

        private Room room;
        private float rmHalfWidth;
        private float rmHalfHeight;

        public MainWindow() : base(800, 600) { }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            string version = GL.GetString(StringName.Version);
            int major = (int)version[0];
            int minor = (int)version[2];
            //if (major <= 1 && minor < 5)
            //{
            //    System.Windows.Forms.MessageBox.Show("You need at least OpenGL 1.5 to run this example. Aborting.", "VBOs not supported",
            //        System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Exclamation);
            //    this.Exit();
            //}

            GL.ClearColor(System.Drawing.Color.Black);
            GL.Enable(EnableCap.DepthTest);
            GL.LineWidth(1.5f);

            GL.Light(LightName.Light0, LightParameter.Position, new Vector4(0.0f, 8.0f, 8.0f, 0.0f));
            GL.Light(LightName.Light0, LightParameter.SpotDirection, new Vector4(-1.0f, 0.0f, 0.0f, 0.0f));
            GL.Light(LightName.Light0, LightParameter.Ambient, new Color4(0f, 0f, 0f, 1.0f));
            GL.Light(LightName.Light0, LightParameter.Diffuse, new Color4(1f, 1f, 1f, 1.0f));
            GL.Light(LightName.Light0, LightParameter.Specular, new Color4(1f, 1f, 1f, 1.0f));
            GL.LightModel(LightModelParameter.LightModelAmbient, new float[4] { 0.5f, 0.5f, 0.5f, 1.0f });
            GL.Enable(EnableCap.Lighting);
            GL.Enable(EnableCap.Light0);

            GL.Enable(EnableCap.Normalize);

            GL.Enable(EnableCap.ColorMaterial);
            GL.ColorMaterial(MaterialFace.FrontAndBack, ColorMaterialParameter.AmbientAndDiffuse);
            //GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Specular, new Color4(1f, 1f, 1f, 1f));
            //GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Emission, new Color4(0f, 0f, 0f, 1f));

            GL.ShadeModel(ShadingModel.Flat);

            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            GL.Hint(HintTarget.LineSmoothHint, HintMode.Nicest);	// Use The Good Calculations
            GL.Enable(EnableCap.LineSmooth);			// Enable Anti-Aliasing

            // Process command-line args:
            Queue<string> args = new Queue<string>( Environment.GetCommandLineArgs().Skip(1) );
            if (args.Count >= 1)
            {
                string path = args.Dequeue();
                gameData = GameData.LoadFromRom(path);
            }

            if (gameData == null)
                gameData = GameData.LoadFromRom(@"Solstice (U).nes");
            if (gameData == null)
                gameData = GameData.LoadFromRom(@"..\..\Solstice (U).nes");
            
            if (gameData == null)
            {
                System.Windows.Forms.MessageBox.Show("Could not find Solstice ROM image!", "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Exclamation);
                this.Exit();
            }

            // Load the first room:
            loadRoom(firstRoom);

            Keyboard.KeyDown += new EventHandler<OpenTK.Input.KeyboardKeyEventArgs>(Keyboard_KeyDown);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(0, 0, Width, Height);

            float aspect_ratio = Width / (float)Height;
            Matrix4 perpective = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspect_ratio, 1, 64);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref perpective);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            if (Keyboard[OpenTK.Input.Key.Escape])
                this.Exit();
        }

        private void loadRoom(int roomNumber)
        {
            room = gameData.ParseRoomData(roomNumber);

            rmHalfWidth = (room.Width * 0.5f);
            rmHalfHeight = (room.Height * 0.5f);
        }

        void Keyboard_KeyDown(object sender, OpenTK.Input.KeyboardKeyEventArgs e)
        {
            if (room == null) return;

            if (e.Key == OpenTK.Input.Key.Left)
            {
                if (!room.HasExitNW) return;
                loadRoom(room.ExitNW.RoomNumber);
            }
            else if (e.Key == OpenTK.Input.Key.Up)
            {
                if (!room.HasExitNE) return;
                loadRoom(room.ExitNE.RoomNumber);
            }
            else if (e.Key == OpenTK.Input.Key.Right)
            {
                if (!room.HasExitSE) return;
                loadRoom(room.ExitSE.RoomNumber);
            }
            else if (e.Key == OpenTK.Input.Key.Down)
            {
                if (!room.HasExitSW) return;
                loadRoom(room.ExitSW.RoomNumber);
            }
            else if (e.Key == OpenTK.Input.Key.PageUp)
            {
                if (!room.HasExitCeiling) return;
                loadRoom(room.ExitCeiling.RoomNumber);
            }
            else if (e.Key == OpenTK.Input.Key.PageDown)
            {
                if (!room.HasExitFloor) return;
                loadRoom(room.ExitFloor.RoomNumber);
            }
        }

        private float rampUp(float r)
        {
            return (r * 0.75f) + 0.25f;
        }

        const float zscale = 0.5f;
        const float lineWidth = 0.01f;

        private void drawOpenCube(float w, float z, float h)
        {
            float w2 = w * 0.5f;
            float h2 = h * 0.5f;

            GL.PushAttrib(AttribMask.PolygonBit);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            // Draw bounding box top:
            GL.Begin(BeginMode.LineLoop);
            GL.Vertex3(-w2, 0, -h2);
            GL.Vertex3(-w2, 0, h2);
            GL.Vertex3(w2, 0, h2);
            GL.Vertex3(w2, 0, -h2);
            GL.End();
            // Draw bounding box bottom:
            GL.Begin(BeginMode.LineLoop);
            GL.Vertex3(-w2, z * zscale, -h2);
            GL.Vertex3(-w2, z * zscale, h2);
            GL.Vertex3(w2, z * zscale, h2);
            GL.Vertex3(w2, z * zscale, -h2);
            GL.End();
            // Draw 4 vertical connectors:
            GL.Begin(BeginMode.Lines);
            GL.Vertex3(-w2, 0, -h2);
            GL.Vertex3(-w2, z * zscale, -h2);
            GL.Vertex3(-w2, 0, h2);
            GL.Vertex3(-w2, z * zscale, h2);
            GL.Vertex3(w2, 0, h2);
            GL.Vertex3(w2, z * zscale, h2);
            GL.Vertex3(w2, 0, -h2);
            GL.Vertex3(w2, z * zscale, -h2);
            GL.End();
            GL.PopAttrib();
        }

        private void drawOutlinedSolidCube(float w, float z, float h)
        {
            GL.PushAttrib(AttribMask.AllAttribBits);

            GL.Enable(EnableCap.PolygonOffsetFill); // Avoid Stitching!
            GL.PolygonOffset(1.0f, 1.0f);
            drawSolidCube(w, z, h);
            GL.Disable(EnableCap.PolygonOffsetFill);

            GL.Color4(0f, 0f, 0f, 1f);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            drawSolidCube(w, z, h);
            //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            GL.PopAttrib();
        }

        private void drawSolidCube(float w, float z, float h)
        {
            float w2 = w * 0.5f;
            float h2 = h * 0.5f;
            
            // Draw box bottom:
            GL.Begin(BeginMode.Quads);
            GL.Vertex3(-w2 + lineWidth, 0 + lineWidth, -h2 + lineWidth);
            GL.Vertex3(-w2 + lineWidth, 0 + lineWidth, h2 - lineWidth);
            GL.Vertex3(w2 - lineWidth, 0 + lineWidth, h2 - lineWidth);
            GL.Vertex3(w2 - lineWidth, 0 + lineWidth, -h2 + lineWidth);
            GL.End();

            // Draw box top:
            GL.Begin(BeginMode.Quads);
            GL.Vertex3(-w2 + lineWidth, z * zscale - lineWidth, -h2 + lineWidth);
            GL.Vertex3(-w2 + lineWidth, z * zscale - lineWidth, h2 - lineWidth);
            GL.Vertex3(w2 - lineWidth, z * zscale - lineWidth, h2 - lineWidth);
            GL.Vertex3(w2 - lineWidth, z * zscale - lineWidth, -h2 + lineWidth);
            GL.End();
            
            // Left:
            GL.Begin(BeginMode.Quads);
            GL.Vertex3(-w2 + lineWidth, 0 + lineWidth, -h2 + lineWidth);
            GL.Vertex3(-w2 + lineWidth, z * zscale - lineWidth, -h2 + lineWidth);
            GL.Vertex3(-w2 + lineWidth, z * zscale - lineWidth, h2 - lineWidth);
            GL.Vertex3(-w2 + lineWidth, 0 + lineWidth, h2 - lineWidth);
            GL.End();

            // Right:
            GL.Begin(BeginMode.Quads);
            GL.Vertex3(w2 - lineWidth, 0 + lineWidth, -h2 + lineWidth);
            GL.Vertex3(w2 - lineWidth, z * zscale - lineWidth, -h2 + lineWidth);
            GL.Vertex3(w2 - lineWidth, z * zscale - lineWidth, h2 - lineWidth);
            GL.Vertex3(w2 - lineWidth, 0 + lineWidth, h2 - lineWidth);
            GL.End();

            // Up:
            GL.Begin(BeginMode.Quads);
            GL.Vertex3(-w2 + lineWidth, 0 + lineWidth, -h2 + lineWidth);
            GL.Vertex3(-w2 + lineWidth, z * zscale - lineWidth, -h2 + lineWidth);
            GL.Vertex3(w2 - lineWidth, z * zscale - lineWidth, -h2 + lineWidth);
            GL.Vertex3(w2 - lineWidth, 0 + lineWidth, -h2 + lineWidth);
            GL.End();

            // Down:
            GL.Begin(BeginMode.Quads);
            GL.Vertex3(-w2 + lineWidth, 0 + lineWidth, h2 - lineWidth);
            GL.Vertex3(-w2 + lineWidth, z * zscale - lineWidth, h2 - lineWidth);
            GL.Vertex3(w2 - lineWidth, z * zscale - lineWidth, h2 - lineWidth);
            GL.Vertex3(w2 - lineWidth, 0 + lineWidth, h2 - lineWidth);
            GL.End();
        }

        private void drawOutlinedSolidFlat(float w, float z, float h)
        {
            GL.PushAttrib(AttribMask.AllAttribBits);

            GL.Enable(EnableCap.PolygonOffsetFill); // Avoid Stitching!
            GL.PolygonOffset(1.0f, 1.0f);
            drawSolidFlat(w, z, h);
            GL.Disable(EnableCap.PolygonOffsetFill);

            GL.Color4(0f, 0f, 0f, 1f);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            drawSolidFlat(w, z, h);
            //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            GL.PopAttrib();
        }

        private void drawSolidFlat(float w, float z, float h)
        {
            float w2 = w * 0.5f;
            float h2 = h * 0.5f;

            GL.Begin(BeginMode.Quads);
            GL.Vertex3(-w2 + lineWidth, z * zscale - lineWidth, -h2 + lineWidth);
            GL.Vertex3(-w2 + lineWidth, z * zscale - lineWidth, h2 - lineWidth);
            GL.Vertex3(w2 - lineWidth, z * zscale - lineWidth, h2 - lineWidth);
            GL.Vertex3(w2 - lineWidth, z * zscale - lineWidth, -h2 + lineWidth);
            GL.End();
        }

        private void drawOutlinedSmallTiles(float w, float z, float h)
        {
            GL.PushAttrib(AttribMask.AllAttribBits);

            GL.Enable(EnableCap.PolygonOffsetFill); // Avoid Stitching!
            GL.PolygonOffset(1.0f, 1.0f);
            drawSmallTiles(w, z, h);
            GL.Disable(EnableCap.PolygonOffsetFill);

            GL.Color4(0f, 0f, 0f, 1f);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            drawSmallTiles(w, z, h);
            //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            GL.PopAttrib();
        }

        private void drawSmallTiles(float w, float z, float h)
        {
            float w2 = w * 0.5f;
            float h2 = h * 0.5f;

            GL.Begin(BeginMode.Quads);
            GL.Vertex3(-w2 + lineWidth, z * zscale, -h2 + lineWidth);
            GL.Vertex3(-w2 + lineWidth, z * zscale, 0 - lineWidth);
            GL.Vertex3(0 - lineWidth, z * zscale, 0 - lineWidth);
            GL.Vertex3(0 - lineWidth, z * zscale, -h2 + lineWidth);

            GL.Vertex3(-w2 + lineWidth, z * zscale, 0 + lineWidth);
            GL.Vertex3(-w2 + lineWidth, z * zscale, h2 - lineWidth);
            GL.Vertex3(0 - lineWidth, z * zscale, h2 - lineWidth);
            GL.Vertex3(0 - lineWidth, z * zscale, 0 + lineWidth);

            GL.Vertex3(0 + lineWidth, z * zscale, -h2 + lineWidth);
            GL.Vertex3(0 + lineWidth, z * zscale, 0 - lineWidth);
            GL.Vertex3(w2 - lineWidth, z * zscale, 0 - lineWidth);
            GL.Vertex3(w2 - lineWidth, z * zscale, -h2 + lineWidth);

            GL.Vertex3(0 + lineWidth, z * zscale, 0 + lineWidth);
            GL.Vertex3(0 + lineWidth, z * zscale, h2 - lineWidth);
            GL.Vertex3(w2 - lineWidth, z * zscale, h2 - lineWidth);
            GL.Vertex3(w2 - lineWidth, z * zscale, 0 + lineWidth);
            GL.End();
        }

        private void circleVertex(int i, float r, float x, float y, float z)
        {
            GL.Vertex3(x + Math.Cos(i * Math.PI / 8.0f) * r, y, z + Math.Sin(i * Math.PI / 8.0f) * r);
        }

        private void drawSingleSpike(float x, float y)
        {
            GL.Begin(BeginMode.TriangleFan);
            GL.Vertex3(x, 0.25f, y);
            for (int i = 0; i <= 16; ++i)
            {
                circleVertex(i, 0.125f, x, 0.0f, y);
            }
            GL.End();
        }

        private void drawOutlinedBedOfSpikes()
        {
            GL.PushAttrib(AttribMask.AllAttribBits);

            GL.Enable(EnableCap.PolygonOffsetFill); // Avoid Stitching!
            GL.PolygonOffset(1.0f, 1.0f);
            drawBedOfSpikes();
            GL.Disable(EnableCap.PolygonOffsetFill);

            GL.Color4(0f, 0f, 0f, 1f);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            drawBedOfSpikes();
            //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            GL.PopAttrib();
        }

        private void drawBedOfSpikes()
        {
            drawSingleSpike(-0.25f, -0.25f);
            drawSingleSpike(0.25f, -0.25f);
            drawSingleSpike(0.25f, 0.25f);
            drawSingleSpike(-0.25f, 0.25f);
        }

        private void drawVerticalColumn(float w, float z, float h)
        {
            // TODO: consider w/2, h/2 as r1 and r2 for ellipse
            GL.Begin(BeginMode.TriangleStrip);
            circleVertex(0, 0.5f, 0.0f, 0.0f, 0.0f);
            for (int i = 0; i < 16; ++i)
            {
                circleVertex(i, 0.5f, 0.0f, 0.0f, 0.0f);
                circleVertex(i + 1, 0.5f, 0.0f, z * zscale, 0.0f);
            }
            circleVertex(0, 0.5f, 0.0f, 0.0f, 0.0f);
            GL.End();
        }

        private void drawPyramid(float x, float z, float y, float r)
        {
            GL.Begin(BeginMode.TriangleFan);
            GL.Vertex3(x, z, y);
            GL.Vertex3(x - r, 0f, y - r);
            GL.Vertex3(x - r, 0f, y + r);
            GL.Vertex3(x + r, 0f, y + r);
            GL.Vertex3(x + r, 0f, y - r);
            GL.End();
        }

        private void drawOutlinedPyramidSpikes(float w, float z, float h)
        {
            GL.PushAttrib(AttribMask.AllAttribBits);

            GL.Enable(EnableCap.PolygonOffsetFill); // Avoid Stitching!
            GL.PolygonOffset(1.0f, 1.0f);
            drawPyramidSpikes(w, z, h);
            GL.Disable(EnableCap.PolygonOffsetFill);

            GL.Color4(0f, 0f, 0f, 1f);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            drawPyramidSpikes(w, z, h);
            //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            GL.PopAttrib();
        }

        private void drawPyramidSpikes(float w, float z, float h)
        {
            drawPyramid(-0.25f, z * zscale, -0.25f, 0.25f);
            drawPyramid(-0.25f, z * zscale, 0.25f, 0.25f);
            drawPyramid(0.25f, z * zscale, 0.25f, 0.25f);
            drawPyramid(0.25f, z * zscale, -0.25f, 0.25f);
        }

        private void drawBlockType(BlockCosmeticType ty, int x, int y, int z)
        {
            GL.PushMatrix();
            GL.Translate(
                (x - rmHalfWidth + 0.5f),
                (z * zscale),
                (y - rmHalfHeight + 0.5f)
            );
            switch (ty)
            {
                case BlockCosmeticType.Solid:
                    drawOutlinedSolidCube(1f, 1f, 1f);
                    break;
                case BlockCosmeticType.StoneSlabHemisphereCap:
                    drawOutlinedSolidCube(1f, 0.25f, 1f);
                    // TODO: draw hemisphere cap.
                    break;
                case BlockCosmeticType.VerticalColumn:
                    drawVerticalColumn(1.0f, 1.0f, 1.0f);
                    break;
                case (BlockCosmeticType)10:
                    // 2x2 pyramid spikes
                    drawOutlinedPyramidSpikes(1.0f, 1.0f, 1.0f);
                    break;
                default:
                    drawOpenCube(1.0f, 1.0f, 1.0f);
                    break;
            }
            GL.PopMatrix();
        }

        private void drawStaticBlock(StaticBlock b)
        {
            drawBlockType(b.CosmeticType, b.X, b.Y, b.Z);
        }

        private void drawDynamicBlock(DynamicBlock b)
        {
            drawBlockType(b.CosmeticType, b.X, b.Y, b.Z);
        }

        private void drawEntity(StaticEntity e)
        {
            GL.PushMatrix();
            GL.Translate(
                (e.X * 0.5f - rmHalfWidth + 0.5f),
                (e.Z * zscale + (zscale * 0.5f)),
                (e.Y * 0.5f - rmHalfHeight + 0.5f)
            );
            switch (e.EntityType)
            {
                case EntityType.TransparentCube:
                    drawOpenCube(0.5f, 1.0f, 0.5f);
                    break;
                case EntityType.MovingLift:
                    drawSolidCube(0.5f, 1.0f, 0.5f);
                    GL.Color3(0, 0, 0);
                    drawOpenCube(0.5f, 1.0f, 0.5f);
                    break;
                default:
                    drawSolidCube(0.5f, 1.0f, 0.5f);
                    GL.Color3(0, 0, 0);
                    drawOpenCube(0.5f, 1.0f, 0.5f);
                    break;
            }
            GL.PopMatrix();
        }

        private void drawFloor(FloorCosmeticType floorCosmeticType, int c, int r)
        {
            GL.PushMatrix();
            GL.Translate(c - rmHalfWidth + 0.5f, 0.0f, r - rmHalfHeight + 0.5f);
            switch (floorCosmeticType)
            {
                case FloorCosmeticType.Stone:
                    setGLColor(room.Palette[0]);
                    drawOutlinedSolidFlat(1.0f, 0.0f, 1.0f);
                    break;
                case FloorCosmeticType.BedOfSpikes2:
                case FloorCosmeticType.BedOfSpikes:
                    setGLColor(room.Palette[0]);
                    drawBedOfSpikes();
                    break;
                case FloorCosmeticType.SmallTiles:
                    setGLColor(room.Palette[0]);
                    drawOutlinedSmallTiles(1.0f, 0.0f, 1.0f);
                    break;
                default:
                    setGLColor(room.Palette[0]);
                    drawOutlinedSolidFlat(1.0f, 0.0f, 1.0f);
                    break;
            }
            GL.PopMatrix();
        }

        private Color getColorByPalette(int pidx)
        {
            switch (pidx)
            {
                case 6: return Color.DarkRed;
                case 7: return Color.Crimson;
                case 17: return Color.SkyBlue;
                case 18: return Color.MidnightBlue;
                case 19: return Color.Purple;
                case 20: return Color.Magenta;
                case 21: return Color.Pink;
                case 23: return Color.Red;
                case 24: return Color.DarkGreen;
                case 26: return Color.LimeGreen;
                case 27: return Color.ForestGreen;
                case 28: return Color.Blue;
                case 33: return Color.GreenYellow;
                case 35: return Color.DarkGoldenrod;
                case 36: return Color.Magenta;
                case 38: return Color.Salmon;
                case 39: return Color.SandyBrown;
                case 40: return Color.DarkGreen;
                case 43: return Color.LightGreen;
                case 44: return Color.DarkBlue;
                default: return Color.Silver;
            }
        }

        private void setGLColor(int pidx)
        {
            Color clr = getColorByPalette(pidx);
            GL.Color3(clr.R, clr.G, clr.B);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Matrix4 lookat = Matrix4.LookAt(8, 8, 8, 0, 2, 0, 0, 1, 0);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref lookat);

            // Auto-rotate the room about the Y axis:
            angle += rotation_speed * (float)e.Time;
            GL.Rotate(Math.Cos(angle * Math.PI / 180.0f) * 15.0f + 5.0f, 0.0f, 1.0f, 0.0f);

            // Draw the wall outline:
            GL.Color3(0.0f, 0.0f, 1.0f);
            GL.PushMatrix();
            drawOpenCube(room.Width, 8.0f, room.Height);
            GL.PopMatrix();

            // Draw floor sections:
            for (int r = 0; r < room.Height; ++r)
                for (int c = 0; c < room.Width; ++c)
                    if (room.FloorVisible[r, c])
                    {
                        drawFloor(room.Floor1Cosmetic, c, r);
                    }
                    else if (room.Floor2Behavior != 0)
                    {
                        drawFloor(room.Floor2Cosmetic, c, r);
                    }

            // Draw exits on the walls:
            GL.Color3(0.4f, 0.4f, 0.95f);
            if (room.HasExitNW)
            {
                GL.PushMatrix();
                GL.Translate(0 - rmHalfWidth, room.ExitNW.Z * zscale, rmHalfHeight - (room.ExitNW.W + 2) + 0.5f);
                drawOpenCube(0.0f, 4.0f, 1.0f);
                GL.PopMatrix();
            }
            if (room.HasExitNE)
            {
                GL.PushMatrix();
                GL.Translate((room.ExitNE.W + 1) - rmHalfWidth + 0.5f, room.ExitNE.Z * zscale, 0 - rmHalfHeight);
                drawOpenCube(1.0f, 4.0f, 0.0f);
                GL.PopMatrix();
            }
            if (room.HasExitSE)
            {
                GL.PushMatrix();
                GL.Translate(rmHalfWidth, room.ExitSE.Z * zscale, rmHalfHeight - (room.ExitSE.W + 2) + 0.5f);
                drawOpenCube(0.0f, 4.0f, 1.0f);
                GL.PopMatrix();
            }
            if (room.HasExitSW)
            {
                GL.PushMatrix();
                GL.Translate((room.ExitSW.W + 1) - rmHalfWidth + 0.5f, room.ExitSW.Z * zscale, rmHalfHeight);
                drawOpenCube(1.0f, 4.0f, 0.0f);
                GL.PopMatrix();
            }

            // Draw windows on the walls:
            if (room.WindowMaskNW > 0)
            {
                setGLColor(room.Palette[1]);
                for (int i = 0; i < room.Height; ++i)
                {
                    if ((room.WindowMaskNW & (1 << (7 - i))) == 0) continue;

                    GL.PushMatrix();
                    GL.Translate(0 - rmHalfWidth, 4 * zscale, rmHalfHeight - (i + 1) + 0.5f);
                    drawOpenCube(0.0f, 2.0f, 1.0f);
                    GL.PopMatrix();
                }
            }
            if (room.WindowMaskNE > 0)
            {
                setGLColor(room.Palette[1]);
                for (int i = 0; i < room.Width; ++i)
                {
                    if ((room.WindowMaskNE & (1 << (7 - i))) == 0) continue;

                    GL.PushMatrix();
                    //GL.Translate((room.ExitNE.W + 1) - rmHalfWidth + 0.5f, room.ExitNE.Z * zscale, 0 - rmHalfHeight);
                    GL.Translate(rmHalfWidth - (i + 1) + 0.5f, 4 * zscale, 0 - rmHalfHeight);
                    drawOpenCube(1.0f, 2.0f, 0.0f);
                    GL.PopMatrix();
                }
            }

            // Draw static blocks:
            for (int i = 0; i < room.StaticBlocks.Length; ++i)
            {
                StaticBlock b = room.StaticBlocks[i];

                setGLColor(room.Palette[0]);
                //GL.Color3(0.7f, 0.7f, 0.4f);

                drawStaticBlock(b);
            }

            // Draw dynamic blocks:
            for (int i = 0; i < room.DynamicBlocks.Length; ++i)
            {
                DynamicBlock b = room.DynamicBlocks[i];

                setGLColor(room.Palette[0]);
                drawDynamicBlock(b);
            }

            // Draw entities:
            for (int i = 0; i < room.Entities.Length; ++i)
            {
                StaticEntity ent = room.Entities[i];

                setGLColor(ent.Color1);
                drawEntity(ent);
            }

            SwapBuffers();
        }
        
        /// <summary>
        /// Entry point of this example.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            using (MainWindow example = new MainWindow())
            {
                example.Title = "Solstice viewer";
                example.Run(30.0, 0.0);
            }
        }
    }
}