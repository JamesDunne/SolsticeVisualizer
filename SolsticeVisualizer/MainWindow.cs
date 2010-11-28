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
        //const int firstRoom = 36;   // top hemispheres
        //const int firstRoom = 166;  // cylinders
        //const int firstRoom = 37;   // pyramid spikes
        //const int firstRoom = 76;       // rounded stones and transparent boxes
        //const int firstRoom = 82;       // sandwich blocks and crystal ball
        const int firstRoom = 243;

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
            GL.LineWidth(1.2f);

            GL.Light(LightName.Light0, LightParameter.Position, new Vector4(-8f, 4f, 4f, 0.0f));
            //GL.Light(LightName.Light0, LightParameter.SpotDirection, new Vector4(-1.0f, 0.0f, 0.0f, 0.0f));
            GL.Light(LightName.Light0, LightParameter.Ambient, new Color4(0f, 0f, 0f, 1.0f));
            GL.Light(LightName.Light0, LightParameter.Diffuse, new Color4(1f, 1f, 1f, 1.0f));
            GL.Light(LightName.Light0, LightParameter.Specular, new Color4(1f, 1f, 1f, 1.0f));
            GL.LightModel(LightModelParameter.LightModelAmbient, new float[4] { 0.35f, 0.35f, 0.35f, 1.0f });
            GL.Enable(EnableCap.Lighting);
            GL.Enable(EnableCap.Light0);

            GL.Enable(EnableCap.Normalize);

            GL.Enable(EnableCap.ColorMaterial);
            GL.ColorMaterial(MaterialFace.FrontAndBack, ColorMaterialParameter.AmbientAndDiffuse);
            //GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Specular, new Color4(0.2f, 0.2f, 0.2f, 1f));
            //GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Emission, new Color4(0f, 0f, 0f, 1f));

            GL.ShadeModel(ShadingModel.Flat);

            GL.Enable(EnableCap.PolygonOffsetFill);
            GL.PolygonOffset(1f, 1f);

            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            GL.Hint(HintTarget.LineSmoothHint, HintMode.Nicest);	// Use The Good Calculations
            GL.Enable(EnableCap.LineSmooth);			// Enable Anti-Aliasing

            // Process command-line args:
            Queue<string> args = new Queue<string>(Environment.GetCommandLineArgs().Skip(1));
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

        private void loadRoom(int roomNumber)
        {
            room = gameData.ParseRoomData(roomNumber);

            rmHalfWidth = (room.Width * 0.5f);
            rmHalfHeight = (room.Height * 0.5f);
        }

        #region Form events

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

        #endregion

        #region Rendering primitives

        const float zscale = 0.3333333f;

        private void drawLeftWall(float z, float h)
        {
            float hHalf = h * 0.5f;

            float y1 = -hHalf;
            float y2 = hHalf;
            float z1 = 0;
            float z2 = z * zscale;

            // Draw box left:
            GL.Begin(BeginMode.Quads);
            GL.Normal3(-1f, 0f, 0f);
            GL.Vertex3(0f, z1, y1);
            GL.Vertex3(0f, z1, y2);
            GL.Vertex3(0f, z2, y2);
            GL.Vertex3(0f, z2, y1);
            GL.End();
        }

        private void drawRightWall(float z, float h)
        {
            float hHalf = h * 0.5f;

            float y1 = -hHalf;
            float y2 = hHalf;
            float z1 = 0;
            float z2 = z * zscale;

            // Draw box right:
            GL.Begin(BeginMode.Quads);
            GL.Normal3(1f, 0f, 0f);
            GL.Vertex3(0f, z1, y2);
            GL.Vertex3(0f, z1, y1);
            GL.Vertex3(0f, z2, y1);
            GL.Vertex3(0f, z2, y2);
            GL.End();
        }

        private void drawFrontWall(float w, float z)
        {
            float wHalf = w * 0.5f;

            float x1 = -wHalf;
            float x2 = wHalf;
            float z1 = 0;
            float z2 = z * zscale;

            // Draw box front:
            GL.Begin(BeginMode.Quads);
            GL.Normal3(0f, 0f, 1f);
            GL.Vertex3(x1, z1, 0f);
            GL.Vertex3(x2, z1, 0f);
            GL.Vertex3(x2, z2, 0f);
            GL.Vertex3(x1, z2, 0f);
            GL.End();
        }

        private void drawBackWall(float w, float z)
        {
            float wHalf = w * 0.5f;

            float x1 = -wHalf;
            float x2 = wHalf;
            float z1 = 0;
            float z2 = z * zscale;

            // Draw box back:
            GL.Begin(BeginMode.Quads);
            GL.Normal3(0f, 0f, -1f);
            GL.Vertex3(x1, z2, 0f);
            GL.Vertex3(x2, z2, 0f);
            GL.Vertex3(x2, z1, 0f);
            GL.Vertex3(x1, z1, 0f);
            GL.End();
        }

        private void drawOpenCube(float w, float z, float h)
        {
            float w2 = w * 0.5f;
            float h2 = h * 0.5f;

            // Switch to line drawing for the quads:
            GL.PushAttrib(AttribMask.PolygonBit);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);

            drawSolidCube(w, z, h);

            GL.PopAttrib();
        }

        private void drawOutlinedSolidCube(float w, float z, float h)
        {
            GL.PushAttrib(AttribMask.AllAttribBits);

            //GL.Enable(EnableCap.PolygonOffsetFill); // Avoid Stitching!
            //GL.PolygonOffset(1.0f, 1.0f);
            drawSolidCube(w, z, h);
            //GL.Disable(EnableCap.PolygonOffsetFill);

            GL.Color4(0f, 0f, 0f, 1f);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            drawSolidCube(w, z, h);
            //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            GL.PopAttrib();
        }

        private void drawSolidCube(float w, float z, float h)
        {
            float wHalf = w * 0.5f;
            float hHalf = h * 0.5f;

            float x1 = -wHalf;
            float x2 = wHalf;
            float y1 = -hHalf;
            float y2 = hHalf;
            float z1 = 0;
            float z2 = z * zscale;

            // Draw box front:
            GL.Begin(BeginMode.Quads);
            GL.Normal3(0f, 0f, 1f);
            GL.Vertex3(x1, z1, y2);
            GL.Vertex3(x2, z1, y2);
            GL.Vertex3(x2, z2, y2);
            GL.Vertex3(x1, z2, y2);
            GL.End();

            // Draw box top:
            GL.Begin(BeginMode.Quads);
            GL.Normal3(0f, 1f, 0f);
            GL.Vertex3(x1, z2, y2);
            GL.Vertex3(x2, z2, y2);
            GL.Vertex3(x2, z2, y1);
            GL.Vertex3(x1, z2, y1);
            GL.End();

            // Draw box back:
            GL.Begin(BeginMode.Quads);
            GL.Normal3(0f, 0f, -1f);
            GL.Vertex3(x1, z2, y1);
            GL.Vertex3(x2, z2, y1);
            GL.Vertex3(x2, z1, y1);
            GL.Vertex3(x1, z1, y1);
            GL.End();

            // Draw box left:
            GL.Begin(BeginMode.Quads);
            GL.Normal3(-1f, 0f, 0f);
            GL.Vertex3(x1, z1, y1);
            GL.Vertex3(x1, z1, y2);
            GL.Vertex3(x1, z2, y2);
            GL.Vertex3(x1, z2, y1);
            GL.End();

            // Draw box bottom:
            GL.Begin(BeginMode.Quads);
            GL.Normal3(0f, -1f, 0f);
            GL.Vertex3(x1, z1, y2);
            GL.Vertex3(x2, z1, y2);
            GL.Vertex3(x2, z1, y1);
            GL.Vertex3(x1, z1, y1);
            GL.End();

            // Draw box right:
            GL.Begin(BeginMode.Quads);
            GL.Normal3(1f, 0f, 0f);
            GL.Vertex3(x2, z1, y2);
            GL.Vertex3(x2, z1, y1);
            GL.Vertex3(x2, z2, y1);
            GL.Vertex3(x2, z2, y2);
            GL.End();
        }

        private void drawOutlinedSolidFlat(float w, float z, float h)
        {
            GL.PushAttrib(AttribMask.AllAttribBits);

            //GL.Enable(EnableCap.PolygonOffsetFill); // Avoid Stitching!
            //GL.PolygonOffset(1.0f, 1.0f);
            drawSolidFlat(w, z, h);
            //GL.Disable(EnableCap.PolygonOffsetFill);

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
            GL.Normal3(0f, 1f, 0f);
            GL.Vertex3(-w2, z * zscale, -h2);
            GL.Vertex3(-w2, z * zscale, h2);
            GL.Vertex3(w2, z * zscale, h2);
            GL.Vertex3(w2, z * zscale, -h2);
            GL.End();
        }

        private void drawOutlinedSmallTiles(float w, float z, float h)
        {
            GL.PushAttrib(AttribMask.AllAttribBits);

            //GL.Enable(EnableCap.PolygonOffsetFill); // Avoid Stitching!
            //GL.PolygonOffset(1.0f, 1.0f);
            drawSmallTiles(w, z, h);
            //GL.Disable(EnableCap.PolygonOffsetFill);

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
            GL.Normal3(0f, 1f, 0f);
            GL.Vertex3(-w2, z * zscale, -h2);
            GL.Vertex3(-w2, z * zscale, 0);
            GL.Vertex3(0, z * zscale, 0);
            GL.Vertex3(0, z * zscale, -h2);

            GL.Vertex3(-w2, z * zscale, 0);
            GL.Vertex3(-w2, z * zscale, h2);
            GL.Vertex3(0, z * zscale, h2);
            GL.Vertex3(0, z * zscale, 0);

            GL.Vertex3(0, z * zscale, -h2);
            GL.Vertex3(0, z * zscale, 0);
            GL.Vertex3(w2, z * zscale, 0);
            GL.Vertex3(w2, z * zscale, -h2);

            GL.Vertex3(0, z * zscale, 0);
            GL.Vertex3(0, z * zscale, h2);
            GL.Vertex3(w2, z * zscale, h2);
            GL.Vertex3(w2, z * zscale, 0);
            GL.End();
        }

        private void circleNormal(int i, float r, float x, float y, float z)
        {
            double angle = i * Math.PI / 8.0d;
            GL.Normal3(Math.Cos(angle), 0f, Math.Sin(angle));
        }

        private void circleVertex(int i, float r, float x, float y, float z)
        {
            double angle = i * Math.PI / 8.0d;
            GL.Vertex3(x + Math.Cos(angle) * r, y, z + Math.Sin(angle) * r);
        }

        private void drawSingleSpike(float x, float y)
        {
            GL.Begin(BeginMode.TriangleFan);
            GL.Vertex3(x, 0.25f, y);
            for (int i = 0; i <= 16; ++i)
            {
                circleNormal(i, 0.125f, x, 0f, y);
                circleVertex(i, 0.125f, x, 0f, y);
            }
            GL.End();
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

            // Draw outer shell:
            GL.Begin(BeginMode.QuadStrip);
            for (int i = 0; i <= 16; ++i)
            {
                double ang = ((double)i - 0.5d) * Math.PI / 8.0f;
                circleNormal(i, 0.5f, 0f, 0f, 0f);
                circleVertex(i, 0.5f, 0f, 0f, 0f);
                circleVertex(i, 0.5f, 0f, z * zscale, 0f);
            }
            GL.End();

            // Draw top circle:
            GL.Begin(BeginMode.TriangleFan);
            GL.Normal3(0f, 1f, 0f);
            GL.Vertex3(0f, z * zscale, 0f);
            for (int i = 0; i <= 16; ++i)
            {
                circleVertex(i, 0.5f, 0f, z * zscale, 0f);
            }
            GL.End();
        }

        private void drawPyramid(float x, float z, float y, float r)
        {
            GL.Begin(BeginMode.Triangles);
            GL.Normal3(-1f, 0f, 0f);
            GL.Vertex3(x, z, y);
            GL.Vertex3(x - r, 0f, y - r);
            GL.Vertex3(x - r, 0f, y + r);

            GL.Normal3(0f, 0f, 1f);
            GL.Vertex3(x, z, y);
            GL.Vertex3(x - r, 0f, y + r);
            GL.Vertex3(x + r, 0f, y + r);

            GL.Normal3(1f, 0f, 0f);
            GL.Vertex3(x, z, y);
            GL.Vertex3(x + r, 0f, y + r);
            GL.Vertex3(x + r, 0f, y - r);

            GL.Normal3(0f, 0f, -1f);
            GL.Vertex3(x, z, y);
            GL.Vertex3(x + r, 0f, y - r);
            GL.Vertex3(x - r, 0f, y - r);
            GL.End();
        }

        private void drawOutlinedPyramidSpikes(float w, float z, float h)
        {
            GL.PushAttrib(AttribMask.AllAttribBits);

            //GL.Enable(EnableCap.PolygonOffsetFill); // Avoid Stitching!
            //GL.PolygonOffset(1.0f, 1.0f);
            drawPyramidSpikes(w, z, h);
            //GL.Disable(EnableCap.PolygonOffsetFill);

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

        private void drawTopHemisphere(float z, float r)
        {
            const int slices = 16;

            // Draw top of sphere as a triangle fan around the top-most point.
            GL.Begin(BeginMode.TriangleFan);
            GL.Vertex3(0f, (z + r) * zscale, 0f);
            int j = 1;
            for (int i = 0; i <= 16; ++i)
            {
                double jangle = j * 0.5d * Math.PI / (double)slices;
                float z1 = (float)Math.Cos(jangle);
                float r1 = (float)Math.Sin(jangle);
                circleNormal(i, r * r1, 0f, 0f, 0f);
                circleVertex(i, r * r1, 0f, (z * zscale + r * z1), 0f);
            }
            GL.End();

            for (j = 2; j <= slices; ++j)
            {
                double j1angle = (j - 1) * 0.5d * Math.PI / (double)slices;
                float z1 = (float)Math.Cos(j1angle);
                float r1 = (float)Math.Sin(j1angle);

                double j2angle = (j) * 0.5d * Math.PI / (double)slices;
                float z2 = (float)Math.Cos(j2angle);
                float r2 = (float)Math.Sin(j2angle);

                GL.Begin(BeginMode.QuadStrip);
                for (int i = 0; i <= 16; ++i)
                {
                    circleNormal(i, r * (r1 + r2) * 0.5f, 0f, 0f, 0f);
                    circleVertex(i, r * r1, 0f, (z * zscale + r * z1), 0f);
                    circleVertex(i, r * r2, 0f, (z * zscale + r * z2), 0f);
                }
                GL.End();
            }
        }

        private void drawBottomHemisphere(float z, float r)
        {
            const int slices = 16;

            // Draw top of sphere as a triangle fan around the top-most point.
            GL.Begin(BeginMode.TriangleFan);
            GL.Vertex3(0f, (z - r) * zscale, 0f);
            int j = 1;
            for (int i = 16; i >= 0; --i)
            {
                double jangle = j * 0.5d * Math.PI / (double)slices;
                float z1 = (float)Math.Cos(jangle);
                float r1 = (float)Math.Sin(jangle);
                circleNormal(i, r * r1, 0f, 0f, 0f);
                circleVertex(i, r * r1, 0f, (z * zscale - r * z1), 0f);
            }
            GL.End();

            for (j = 2; j <= slices; ++j)
            {
                double j1angle = (j - 1) * 0.5d * Math.PI / (double)slices;
                float z1 = (float)Math.Cos(j1angle);
                float r1 = (float)Math.Sin(j1angle);

                double j2angle = (j) * 0.5d * Math.PI / (double)slices;
                float z2 = (float)Math.Cos(j2angle);
                float r2 = (float)Math.Sin(j2angle);

                GL.Begin(BeginMode.QuadStrip);
                for (int i = 16; i >= 0; --i)
                {
                    circleNormal(i, r * (r1 + r2) * 0.5f, 0f, 0f, 0f);
                    circleVertex(i, r * r1, 0f, (z * zscale - r * z1), 0f);
                    circleVertex(i, r * r2, 0f, (z * zscale - r * z2), 0f);
                }
                GL.End();
            }
        }

        #endregion

        #region Rendering specific game objects

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
                    setBGGLColor(room.Palette[2]);
                    drawOutlinedSolidCube(1f, 1f, 1f);
                    break;
                case BlockCosmeticType.StoneSlabHemisphereTopCap:
                    setBGGLColor(room.Palette[2]);
                    drawOutlinedSolidCube(1f, 0.25f, 1f);
                    setBGGLColor(room.Palette[0]);
                    drawTopHemisphere(0.25f, 0.333f);
                    break;
                case BlockCosmeticType.StoneSlabHemisphereBottomCap:
                    setBGGLColor(room.Palette[2]);
                    GL.PushMatrix();
                    GL.Translate(0f, 0.75f * zscale, 0f);
                    drawOutlinedSolidCube(1f, 0.25f, 1f);
                    GL.PopMatrix();
                    setBGGLColor(room.Palette[0]);
                    drawBottomHemisphere(0.75f, 0.333f);
                    break;
                case BlockCosmeticType.SandwichBlock:
                    // Bottom solid part:
                    setBGGLColor(room.Palette[0]);
                    drawOutlinedSolidCube(1f, 0.4f, 1f);

                    // Middle creme filling:
                    setBGGLColor(room.Palette[2]);
                    GL.PushMatrix();
                    GL.Translate(0f, 0.4f * zscale, 0f);
                    drawOutlinedSolidCube(1f, 0.2f, 1f);
                    GL.PopMatrix();

                    // Top solid part:
                    setBGGLColor(room.Palette[0]);
                    GL.PushMatrix();
                    GL.Translate(0f, 0.6f * zscale, 0f);
                    drawOutlinedSolidCube(1f, 0.4f, 1f);
                    GL.PopMatrix();
                    break;
                case BlockCosmeticType.VerticalColumn:
                    setBGGLColor(room.Palette[0]);
                    drawVerticalColumn(1.0f, 0.8f, 1.0f);
                    break;
                case BlockCosmeticType.TransparentOutlined:
                    setBGGLColor(room.Palette[0]);
                    drawOpenCube(0.95f, 0.9f, 0.95f);
                    break;
                case BlockCosmeticType.RoundedStoneSlab:
                    setBGGLColor(room.Palette[0]);
                    GL.PushMatrix();
                    GL.Translate(0f, 0.1f * zscale, 0f);
                    drawSolidCube(0.95f, 0.8f, 0.95f);
                    GL.PopMatrix();
                    GL.PushMatrix();
                    GL.Translate(0f, 0.05f * zscale, 0f);
                    drawSolidCube(0.9f, 0.9f, 0.9f);
                    GL.PopMatrix();
                    GL.PushMatrix();
                    GL.Translate(0f, 0.05f * zscale, 0f);
                    drawSolidCube(0.85f, 0.95f, 0.85f);
                    GL.PopMatrix();
                    break;
                case BlockCosmeticType.PyramidSpikes:
                    setBGGLColor(room.Palette[2]);
                    drawOutlinedPyramidSpikes(1.0f, 1.0f, 1.0f);
                    break;
                default:
                    setBGGLColor(room.Palette[2]);
                    drawOpenCube(0.95f, 0.95f, 0.95f);
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

        private void drawEntity(StaticEntity ent)
        {
            GL.PushMatrix();
            GL.Translate(
                (ent.X * 0.5f - rmHalfWidth + 0.5f),
                (ent.Z * zscale + (zscale * 0.5f)),
                (ent.Y * 0.5f - rmHalfHeight + 0.5f)
            );
            switch (ent.EntityType)
            {
                case EntityType.TransparentCube:
                    setFGGLColor(ent.Color1);
                    drawOpenCube(0.5f, 1.0f, 0.5f);
                    break;
                case EntityType.MovingLift:
                    setFGGLColor(ent.Color1);
                    drawOutlinedSolidCube(0.5f, 1.0f, 0.5f);
                    break;
                case EntityType.SlidingCrystalBall:
                    setFGGLColor(ent.Color1);
                    drawBottomHemisphere(0.25f, 0.25f);
                    drawTopHemisphere(0.25f, 0.25f);
                    break;
                default:
                    setFGGLColor(ent.Color1);
                    drawOutlinedSolidCube(0.5f, 1.0f, 0.5f);
                    break;
            }
            GL.PopMatrix();
        }

        private void drawFloor(FloorCosmeticType floorCosmeticType, int c, int r)
        {
            GL.PushMatrix();
            GL.Translate(c - rmHalfWidth + 0.5f, 0.0f, r - rmHalfHeight + 0.5f);
            //GL.Translate(rmHalfWidth - c + 0.5f, 0.0f, rmHalfHeight - r + 0.5f);
            switch (floorCosmeticType)
            {
                case FloorCosmeticType.Stone:
                    setBGGLColor(room.Palette[1]);
                    drawOutlinedSolidFlat(1.0f, 0.0f, 1.0f);
                    break;
                case FloorCosmeticType.BedOfSpikes2:
                case FloorCosmeticType.BedOfSpikes:
                    setBGGLColor(room.Palette[2]);
                    drawBedOfSpikes();
                    break;
                case FloorCosmeticType.SmallTiles:
                    setBGGLColor(room.Palette[1]);
                    drawOutlinedSmallTiles(1.0f, 0.0f, 1.0f);
                    break;
                case FloorCosmeticType.Empty:
                    break;
                case FloorCosmeticType.ForestDirt:
                case FloorCosmeticType.Gravel:
                    setBGGLColor(room.Palette[1]);
                    drawOutlinedSolidFlat(1.0f, 0.0f, 1.0f);
                    break;
                default:
                    setBGGLColor(room.Palette[1]);
                    drawOutlinedSolidFlat(1.0f, 0.0f, 1.0f);
                    break;
            }
            GL.PopMatrix();
        }

        #endregion

        #region Palette mapping

        private Color getColorByFGPalette(int pidx)
        {
            switch (pidx)
            {
            // :)
                case 19: return Color.Purple;
                case 20: return Color.Magenta;
                case 21: return Color.HotPink;
                case 22: return Color.Red;
                case 23: return Color.SandyBrown;
                case 24: return Color.DarkOrange;
                case 25: return Color.DarkGoldenrod;
                case 37: return Color.HotPink;
                case 39: return Color.Honeydew;
                case 40: return Color.Goldenrod;
                case 44: return Color.SeaGreen;

            // :(
                case 6: return Color.DarkRed;
                case 7: return Color.Crimson;
                case 17: return Color.SkyBlue;
                case 18: return Color.MidnightBlue;
                case 26: return Color.LimeGreen;
                case 27: return Color.ForestGreen;
                case 28: return Color.Blue;
                case 33: return Color.GreenYellow;
                case 35: return Color.DarkGoldenrod;
                case 36: return Color.Magenta;
                case 38: return Color.Salmon;
                case 43: return Color.LightGreen;
                default: return Color.Silver;
            }
        }

        private Color getColorByBGPalette(int pidx)
        {
            switch (pidx)
            {
            // :)
                case 4: return Color.DarkMagenta;
                case 7: return Color.FromArgb(0x74, 0x54, 0x20);
                case 9: return Color.FromArgb(0, 0x10, 0);
                case 10: return Color.FromArgb(0, 0x24, 0);
                case 11: return Color.FromArgb(0x0D, 0x35, 0x2A);
                case 12: return Color.FromArgb(0x0D, 0x22, 0x35);
                case 17: return Color.FromArgb(0x33, 0x55, 0xBB);
                case 18: return Color.FromArgb(0x33, 0x33, 0xCC);
                case 19: return Color.FromArgb(0x44, 0x44, 0xDD);

                case 20: return Color.Magenta;
                case 23: return Color.FromArgb(0xC4, 0xA4, 0x50);
                case 24: return Color.YellowGreen;
                case 25: return Color.FromArgb(0x12, 0x42, 0);
                case 26: return Color.FromArgb(0x32, 0x9A, 0x22);
                case 27: return Color.FromArgb(0x0D, 0x55, 0x4A);
                case 28: return Color.FromArgb(0x0D, 0x55, 0x6A);
                case 33: return Color.CornflowerBlue;
                case 34: return Color.FromArgb(0x8D, 0xCC, 0xF0);
                case 35: return Color.FromArgb(0x7C, 0xDC, 0xFF);
                case 36: return Color.Magenta;

                case 39: return Color.FromArgb(0xC4, 0xA4, 0x50);
                case 41: return Color.FromArgb(0x9A, 0xAA, 0x35);
                case 42: return Color.FromArgb(0x3A, 0x90, 0x28);
                case 43: return Color.LightGoldenrodYellow;
                case 49: return Color.LightGray;

                default: return Color.Silver;
            }
        }

        private void setFGGLColor(int pidx)
        {
            Color clr = getColorByFGPalette(pidx);
            GL.Color3(clr.R, clr.G, clr.B);
        }

        private void setBGGLColor(int pidx)
        {
            Color clr = getColorByBGPalette(pidx);
            GL.Color3(clr.R, clr.G, clr.B);
        }

        #endregion

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Matrix4 lookat = Matrix4.LookAt(8, 6, 8, 0, 1, 0, 0, 1, 0);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref lookat);

            // Auto-rotate the room about the Y axis:
            angle += rotation_speed * (float)e.Time;
            GL.Rotate(Math.Cos(angle * Math.PI / 180.0f) * 5f, 0.0f, 1.0f, 0.0f);
            //GL.Rotate(angle, 0.0f, 1.0f, 0.0f);

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
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            if (room.HasExitNW)
            {
                GL.PushMatrix();
                GL.Translate(0 - rmHalfWidth, room.ExitNW.Z * zscale, rmHalfHeight - (room.ExitNW.W + 2) + 0.5f);
                drawLeftWall(4.0f, 1.0f);
                GL.PopMatrix();
            }
            if (room.HasExitNE)
            {
                GL.PushMatrix();
                GL.Translate((room.ExitNE.W + 1) - rmHalfWidth + 0.5f, room.ExitNE.Z * zscale, 0 - rmHalfHeight);
                drawFrontWall(1.0f, 4.0f);
                GL.PopMatrix();
            }
            if (room.HasExitSE)
            {
                GL.PushMatrix();
                GL.Translate(rmHalfWidth, room.ExitSE.Z * zscale, rmHalfHeight - (room.ExitSE.W + 2) + 0.5f);
                drawRightWall(4.0f, 1.0f);
                GL.PopMatrix();
            }
            if (room.HasExitSW)
            {
                GL.PushMatrix();
                GL.Translate((room.ExitSW.W + 1) - rmHalfWidth + 0.5f, room.ExitSW.Z * zscale, rmHalfHeight);
                drawFrontWall(1.0f, 4.0f);
                GL.PopMatrix();
            }

            // Draw windows on the walls:
            if (room.WindowMaskNW > 0)
            {
                setBGGLColor(room.Palette[1]);
                for (int i = 0; i < room.Height; ++i)
                {
                    if ((room.WindowMaskNW & (1 << (7 - i))) == 0) continue;

                    GL.PushMatrix();
                    GL.Translate(0 - rmHalfWidth, 4 * zscale, rmHalfHeight - (i + 1) + 0.5f);
                    drawLeftWall(2.0f, 1.0f);
                    GL.PopMatrix();
                }
            }
            if (room.WindowMaskNE > 0)
            {
                setBGGLColor(room.Palette[1]);
                for (int i = 0; i < room.Width; ++i)
                {
                    if ((room.WindowMaskNE & (1 << (7 - i))) == 0) continue;

                    GL.PushMatrix();
                    GL.Translate(rmHalfWidth - (i + 1) + 0.5f, 4 * zscale, 0 - rmHalfHeight);
                    drawFrontWall(1.0f, 2.0f);
                    GL.PopMatrix();
                }
            }
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            // Draw static blocks:
            for (int i = 0; i < room.StaticBlocks.Length; ++i)
            {
                StaticBlock b = room.StaticBlocks[i];

                drawStaticBlock(b);
            }

            // Draw dynamic blocks:
            for (int i = 0; i < room.DynamicBlocks.Length; ++i)
            {
                DynamicBlock b = room.DynamicBlocks[i];

                drawDynamicBlock(b);
            }

            // Draw entities:
            for (int i = 0; i < room.Entities.Length; ++i)
            {
                StaticEntity ent = room.Entities[i];

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