﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace SolsticeVisualizer
{
    class MainWindow : GameWindow
    {
        const int firstRoom = 0;
        //const int firstRoom = 248;
        //const int firstRoom = 42;
        //const int firstRoom = 192;          // room with special static block
        //const int firstRoom = 36;         // top hemispheres
        //const int firstRoom = 166;        // cylinders
        //const int firstRoom = 37;         // pyramid spikes
        //const int firstRoom = 76;         // rounded stones and transparent boxes
        //const int firstRoom = 82;         // sandwich blocks and crystal ball
        //const int firstRoom = 25;           // Spikes littered about floor not yet visible
        //const int firstRoom = 243;        // last room

        const float rotation_speed = 15.0f;
        float angle;

        GameData gameData = null;

        private Room room;
        private float rmHalfWidth;
        private float rmHalfHeight;

        int[] wallTextures;
        int[] floorTextures;

        public MainWindow() : base(800, 600) { }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            string version = GL.GetString(StringName.Version);
            int major = (int)version[0];
            int minor = (int)version[2];

            InitializeGL();

            // Process command-line args:
            Queue<string> args = new Queue<string>(Environment.GetCommandLineArgs());
            args.Dequeue();
            string path = @"Solstice (U).nes";
            if (args.Count >= 1)
            {
                path = args.Dequeue();
                gameData = GameData.LoadFromRom(path);
            }

            if (gameData == null)
            {
                path = @"Solstice (U).nes";
                gameData = GameData.LoadFromRom(path);
            }
            if (gameData == null)
            {
                path = @"..\..\Solstice (U).nes";
                gameData = GameData.LoadFromRom(path);
            }

            if (gameData == null)
            {
                System.Windows.Forms.MessageBox.Show("Could not find Solstice ROM image!", "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Exclamation);
                this.Exit();
            }

            // Load the palette:
            string palPath = path;
            if (palPath.EndsWith(".nes", StringComparison.OrdinalIgnoreCase))
            {
                palPath = palPath.Substring(0, palPath.Length - 4) + ".pal";
                LoadPalette(palPath);
            }

            // Load the first room:
            loadRoom(firstRoom);

            Keyboard.KeyDown += new EventHandler<OpenTK.Input.KeyboardKeyEventArgs>(Keyboard_KeyDown);
        }

        private void InitializeGL()
        {
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

            // Enable alpha blending for partially-visible blocks:
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            // Enable all glColor calls to set the lighting material's ambient and diffuse color:
            GL.Enable(EnableCap.ColorMaterial);
            GL.ColorMaterial(MaterialFace.FrontAndBack, ColorMaterialParameter.AmbientAndDiffuse);
            //GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Specular, new Color4(0.2f, 0.2f, 0.2f, 1f));
            //GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Emission, new Color4(0f, 0f, 0f, 1f));

            // Flat shading model gives more of a NES feel:
            GL.ShadeModel(ShadingModel.Smooth);

            // Avoid stitching lines as much as possible, especially when outlining solid quads:
            GL.Enable(EnableCap.PolygonOffsetFill);
            GL.PolygonOffset(1f, 1f);

            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            GL.Hint(HintTarget.LineSmoothHint, HintMode.Nicest);	// Use The Good Calculations
            GL.Enable(EnableCap.LineSmooth);			// Enable Anti-Aliasing

            // Cull back-facing polygons:
            GL.CullFace(CullFaceMode.Back);

            GL.Enable(EnableCap.Texture2D);

            GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);

            #region Load wall textures

            // Set up an array of texture IDs that maps by WallType enum:
            wallTextures = new int[0x16 + 1];
            // Let's assume -1 is an invalid ID:
            for (int i = 0; i <= 0x16; ++i)
                wallTextures[i] = -1;

            loadDirectoryToTextures<WallType>(@"Textures\Walls", wallTextures, e => (int)e);

            #endregion

            #region Load floor textures

            // Set up an array of texture IDs that maps by FloorCosmeticType enum:
            floorTextures = new int[16];

            // Let's assume -1 is an invalid ID:
            for (int i = 0; i < 16; ++i)
                floorTextures[i] = -1;

            loadDirectoryToTextures<FloorCosmeticType>(@"Textures\Floor", floorTextures, e => (int)e);

            #endregion
        }

        private void loadDirectoryToTextures<T>(string path, int[] textures, Converter<T, int> toIndex) where T : struct
        {
            foreach (var fi in new System.IO.DirectoryInfo(path).GetFiles())
            {
                if (fi.Extension.Length == 0) continue;

                string fileName = fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length);

                // Parse the file name as an enum to bind it:
                T texEnum = (T)Enum.Parse(typeof(T), fileName);

                // Load the texture data into graphics memory:
                using (var bitmap = new Bitmap(fi.FullName))
                {
                    textures[toIndex(texEnum)] = LoadTexture(bitmap);
                }
            }
        }

        int LoadTexture(Bitmap bitmap)
        {
            int texture;

            GL.GenTextures(1, out texture);
            GL.BindTexture(TextureTarget.Texture2D, texture);

            BitmapData data = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb
            );

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

            bitmap.UnlockBits(data);

            // Indicates wrapping texture:
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            return texture;
        }

        Color[] palette = null;

        private void LoadPalette(string path)
        {
            palette = new Color[64];
            byte[] raw = File.ReadAllBytes(path);
            for (int i = 0; i < 64; ++i)
            {
                palette[i] = Color.FromArgb(raw[i * 3], raw[i * 3 + 1], raw[i * 3 + 2]);
            }
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

        private void drawLeftWall(float h, float z, float s1, float t1)
        {
            float hHalf = h * 0.5f;

            float y1 = -hHalf;
            float y2 = hHalf;
            float z1 = 0;
            float z2 = z * zscale;

            float s2 = s1 + h / 2.0f;
            float t2 = t1 + z / 8.0f;

            // Draw box left:
            GL.Begin(BeginMode.Quads);
            GL.Normal3(-1f, 0f, 0f);
            GL.TexCoord2(s1, t1);
            GL.Vertex3(0f, z1, y1);
            GL.TexCoord2(s2, t1);
            GL.Vertex3(0f, z1, y2);
            GL.TexCoord2(s2, t2);
            GL.Vertex3(0f, z2, y2);
            GL.TexCoord2(s1, t2);
            GL.Vertex3(0f, z2, y1);
            GL.End();
        }

        private void drawFrontWall(float w, float z, float s1, float t1)
        {
            float wHalf = w * 0.5f;

            float x1 = -wHalf;
            float x2 = wHalf;
            float z1 = 0;
            float z2 = z * zscale;

            float s2 = s1 + w / 2.0f;
            float t2 = t1 + z / 8.0f;

            // Draw box front:
            GL.Begin(BeginMode.Quads);
            GL.Normal3(0f, 0f, 1f);
            GL.TexCoord2(s1, t1);
            GL.Vertex3(x1, z1, 0f);
            GL.TexCoord2(s2, t1);
            GL.Vertex3(x2, z1, 0f);
            GL.TexCoord2(s2, t2);
            GL.Vertex3(x2, z2, 0f);
            GL.TexCoord2(s1, t2);
            GL.Vertex3(x1, z2, 0f);
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
            GL.TexCoord2(0f, 1f);
            GL.Vertex3(0f, z1, y2);
            GL.TexCoord2(0f, 0f);
            GL.Vertex3(0f, z1, y1);
            GL.TexCoord2(1f, 0f);
            GL.Vertex3(0f, z2, y1);
            GL.TexCoord2(1f, 1f);
            GL.Vertex3(0f, z2, y2);
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
            GL.TexCoord2(0f, 1f);
            GL.Vertex3(x1, z2, 0f);
            GL.TexCoord2(1f, 1f);
            GL.Vertex3(x2, z2, 0f);
            GL.TexCoord2(1f, 0f);
            GL.Vertex3(x2, z1, 0f);
            GL.TexCoord2(0f, 0f);
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

            drawSolidCube(w, z, h);

            GL.Color4(0f, 0f, 0f, 1f);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            drawSolidCube(w, z, h);

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

        private void drawOutlinedSolidFlat(float w, float z, float h, float s1, float t1)
        {
            GL.PushAttrib(AttribMask.AllAttribBits);

            drawSolidFlat(w, z, h, s1, t1);

            GL.Color4(0f, 0f, 0f, 1f);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            drawSolidFlat(w, z, h, s1, t1);

            GL.PopAttrib();
        }

        private void drawSolidFlat(float w, float z, float h, float s1, float t1)
        {
            float w2 = w * 0.5f;
            float h2 = h * 0.5f;

            float s2 = s1 + w * 0.5f;
            float t2 = t1 + h * 0.5f;

            GL.Begin(BeginMode.Quads);
            GL.Normal3(0f, 1f, 0f);
            GL.TexCoord2(0f, 0f);
            GL.Vertex3(-w2, z * zscale, -h2);
            GL.TexCoord2(0f, 1f);
            GL.Vertex3(-w2, z * zscale, h2);
            GL.TexCoord2(1f, 1f);
            GL.Vertex3(w2, z * zscale, h2);
            GL.TexCoord2(1f, 0f);
            GL.Vertex3(w2, z * zscale, -h2);
            GL.End();
        }

        private void drawOutlinedSmallTiles(float w, float z, float h)
        {
            GL.PushAttrib(AttribMask.AllAttribBits);

            drawSmallTiles(w, z, h);

            GL.Color4(0f, 0f, 0f, 1f);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            drawSmallTiles(w, z, h);

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

        const int circlePoints = 12;

        private void circleNormal(int i, int n, float r, float x, float y, float z)
        {
            double angle = i * Math.PI / (double)(n * 0.5d);
            GL.Normal3(Math.Cos(angle), y, Math.Sin(angle));
        }

        private void circleVertex(int i, int n, float r, float x, float y, float z)
        {
            double angle = i * Math.PI / (double)(n * 0.5d);
            GL.Vertex3(x + Math.Cos(angle) * r, y, z + Math.Sin(angle) * r);
        }

        private void drawSingleSpike(float x, float y)
        {
            // A vertically oriented cone rendered with a triangle fan with the center point at the tip of the cone:
            GL.Begin(BeginMode.TriangleFan);
            GL.Normal3(0f, 1f, 0f);
            GL.Vertex3(x, 0.19f, y);
            for (int i = 0; i <= circlePoints; ++i)
            {
                circleNormal(i, circlePoints, 0.075f, x, 0.707f, y);
                circleVertex(i, circlePoints, 0.075f, x, 0f, y);
            }
            GL.End();
        }

        private void drawBedOfSpikes()
        {
#if false
            // 2x2:
            drawSingleSpike(-0.25f, -0.25f);
            drawSingleSpike(0.25f, -0.25f);
            drawSingleSpike(0.25f, 0.25f);
            drawSingleSpike(-0.25f, 0.25f);
#else
            const int div = 3;
            const int offs = (div - 1) / 2;
            const float frac = (1f / div);
            for (int r = 0; r < div; ++r)
                for (int c = 0; c < div; ++c)
                    drawSingleSpike((c - offs) * frac, (r - offs) * frac);
#endif
        }

        private void drawVerticalCylinder(float r, float x, float z, float y)
        {
            // Draw outer shell:
            GL.Begin(BeginMode.QuadStrip);
            for (int i = 0; i <= circlePoints; ++i)
            {
                double ang = ((double)i - 0.5d) * Math.PI / 8.0f;
                circleNormal(i, circlePoints, r, 0f, 0f, 0f);
                circleVertex(i, circlePoints, r, x, 0, y);
                circleVertex(i, circlePoints, r, x, z, y);
            }
            GL.End();
        }

        private void drawFlatCircle(float r, float x, float z, float y)
        {
            // Draw top circle:
            GL.Begin(BeginMode.TriangleFan);
            GL.Normal3(0f, 1f, 0f);
            GL.Vertex3(0f, z, 0f);
            for (int i = 0; i <= circlePoints; ++i)
            {
                circleVertex(i, circlePoints, r, x, z, y);
            }
            GL.End();
        }

        private void drawVerticalColumn(float w, float z, float h)
        {
            // TODO: consider w/2, h/2 as r1 and r2 for ellipse

            // Draw outer shell:
            drawVerticalCylinder(0.5f, 0f, z * zscale, 0f);

            // Draw top circle:
            drawFlatCircle(0.5f, 0f, z * zscale, 0f);
        }

        private void drawPyramid(float x, float z, float y, float r)
        {
            GL.Begin(BeginMode.Triangles);
            GL.Normal3(-1f, 0f, 0f);
            GL.Vertex3(x, z * zscale, y);
            GL.Vertex3(x - r, 0f, y - r);
            GL.Vertex3(x - r, 0f, y + r);

            GL.Normal3(0f, 0f, 1f);
            GL.Vertex3(x, z * zscale, y);
            GL.Vertex3(x - r, 0f, y + r);
            GL.Vertex3(x + r, 0f, y + r);

            GL.Normal3(1f, 0f, 0f);
            GL.Vertex3(x, z * zscale, y);
            GL.Vertex3(x + r, 0f, y + r);
            GL.Vertex3(x + r, 0f, y - r);

            GL.Normal3(0f, 0f, -1f);
            GL.Vertex3(x, z * zscale, y);
            GL.Vertex3(x + r, 0f, y - r);
            GL.Vertex3(x - r, 0f, y - r);
            GL.End();
        }

        private void drawOutlinedPyramidSpikes(float w, float z, float h)
        {
            GL.PushAttrib(AttribMask.AllAttribBits);

            drawPyramidSpikes(w, z, h);

            GL.Color4(0f, 0f, 0f, 1f);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            drawPyramidSpikes(w, z, h);

            GL.PopAttrib();
        }

        private void drawPyramidSpikes(float w, float z, float h)
        {
            drawPyramid(-0.25f, z, -0.25f, 0.25f);
            drawPyramid(-0.25f, z, 0.25f, 0.25f);
            drawPyramid(0.25f, z, 0.25f, 0.25f);
            drawPyramid(0.25f, z, -0.25f, 0.25f);
        }

        private void drawTopHemisphere(float z, float r)
        {
            const int slices = 8;

            // Draw top of sphere as a triangle fan around the top-most point.
            GL.Begin(BeginMode.TriangleFan);
            GL.Normal3(0f, 1f, 0f);
            GL.Vertex3(0f, (z + r) * zscale, 0f);
            int j = 1;
            for (int i = 0; i <= circlePoints; ++i)
            {
                double jangle = j * 0.5d * Math.PI / (double)slices;
                float z1 = (float)Math.Cos(jangle);
                float r1 = (float)Math.Sin(jangle);
                circleNormal(i, circlePoints, r * r1, 0f, z1, 0f);
                circleVertex(i, circlePoints, r * r1, 0f, (z * zscale + r * z1), 0f);
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
                for (int i = 0; i <= circlePoints; ++i)
                {
                    //circleNormal(i, circlePoints, r * (r1 + r2) * 0.5f, 0f, z1, 0f);
                    circleNormal(i, circlePoints, r1, 0f, z1, 0f);
                    circleVertex(i, circlePoints, r * r1, 0f, (z * zscale + r * z1), 0f);
                    circleNormal(i, circlePoints, r2, 0f, z2, 0f);
                    circleVertex(i, circlePoints, r * r2, 0f, (z * zscale + r * z2), 0f);
                }
                GL.End();
            }
        }

        private void drawBottomHemisphere(float z, float r)
        {
            const int slices = 8;

            // Draw top of sphere as a triangle fan around the top-most point.
            GL.Begin(BeginMode.TriangleFan);
            GL.Normal3(0f, -1f, 0f);
            GL.Vertex3(0f, (z - r) * zscale, 0f);
            int j = 1;
            for (int i = 0; i <= circlePoints; ++i)
            {
                double jangle = j * 0.5d * Math.PI / (double)slices;
                float z1 = (float)Math.Cos(jangle);
                float r1 = (float)Math.Sin(jangle);
                //circleNormal(i, circlePoints, r * r1, 0f, -z1, 0f);
                circleNormal(i, circlePoints, r1, 0f, -z1, 0f);
                circleVertex(i, circlePoints, r * r1, 0f, (z * zscale - r * z1), 0f);
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
                for (int i = 0; i <= circlePoints; ++i)
                {
                    //circleNormal(i, circlePoints, r * (r1 + r2) * 0.5f, 0f, -z1, 0f);
                    circleNormal(i, circlePoints, r1, 0f, -z1, 0f);
                    circleVertex(i, circlePoints, r * r1, 0f, (z * zscale - r * z1), 0f);
                    circleNormal(i, circlePoints, r2, 0f, -z2, 0f);
                    circleVertex(i, circlePoints, r * r2, 0f, (z * zscale - r * z2), 0f);
                }
                GL.End();
            }
        }

        #endregion

        #region Rendering specific game objects

        private void drawBlockType(BlockCosmeticType ty, int x, int y, int z, byte alpha)
        {
            const float epsilon = 0.01f;
            GL.PushMatrix();
            GL.Translate(
                (x - rmHalfWidth + 0.5f),
                (z * zscale),
                (y - rmHalfHeight + 0.5f)
            );
            GL.Scale(1f - epsilon, 1f - (epsilon / zscale), 1f - epsilon);
            //GL.Translate(0f, (epsilon / (zscale * 2f)), 0f);
            switch (ty)
            {
                case BlockCosmeticType.Solid:
                    setBGGLColorAlpha(room.Palette[2], alpha);
                    drawOutlinedSolidCube(1f, 1f, 1f);
                    break;
                case BlockCosmeticType.ConveyerEW:
                    setBGGLColorAlpha(room.Palette[2], alpha);
                    GL.Begin(BeginMode.QuadStrip);
                    for (int i = 24; i >= 8; --i)
                    {
                        double angle = (i) * Math.PI / 16f;
                        GL.Normal3(Math.Cos(angle), Math.Sin(angle), 0f);
                        GL.Vertex3(-0.375f + 0.125f * Math.Cos(angle), (0.5f + Math.Sin(angle) * 0.495f) * zscale, -0.5f);
                        GL.Vertex3(-0.375f + 0.125f * Math.Cos(angle), (0.5f + Math.Sin(angle) * 0.495f) * zscale, 0.5f);
                    }
                    for (int i = 0; i < 6; ++i)
                    {
                        GL.Normal3(0f, 1f, 0f);
                        GL.Vertex3(-0.375f + 0.125f * i, 0.99f * zscale, -0.5f);
                        GL.Vertex3(-0.375f + 0.125f * i, 0.99f * zscale, 0.5f);
                    }
                    for (int i = 8; i <= 24; ++i)
                    {
                        double angle = (i) * Math.PI / 16f;
                        GL.Normal3(Math.Cos(angle), Math.Sin(angle), 0f);
                        GL.Vertex3(0.375f - 0.125f * Math.Cos(angle), (0.5f + Math.Sin(angle) * 0.495f) * zscale, -0.5f);
                        GL.Vertex3(0.375f - 0.125f * Math.Cos(angle), (0.5f + Math.Sin(angle) * 0.495f) * zscale, 0.5f);
                    }
                    for (int i = 5; i >= 0; --i)
                    {
                        GL.Normal3(0f, -1f, 0f);
                        GL.Vertex3(-0.375f + 0.125f * i, 0.01f * zscale, -0.5f);
                        GL.Vertex3(-0.375f + 0.125f * i, 0.01f * zscale, 0.5f);
                    }
                    GL.End();
                    break;
                case BlockCosmeticType.ConveyerNS:
                    setBGGLColorAlpha(room.Palette[2], alpha);
                    GL.Begin(BeginMode.QuadStrip);
                    for (int i = 24; i >= 8; --i)
                    {
                        double angle = (i) * Math.PI / 16f;
                        GL.Normal3(Math.Cos(angle), Math.Sin(angle), 0f);
                        GL.Vertex3(-0.5f, (0.5f + Math.Sin(angle) * 0.495f) * zscale, -0.375f + 0.125f * Math.Cos(angle));
                        GL.Vertex3(0.5f, (0.5f + Math.Sin(angle) * 0.495f) * zscale, -0.375f + 0.125f * Math.Cos(angle));
                    }
                    for (int i = 0; i < 6; ++i)
                    {
                        GL.Normal3(0f, 1f, 0f);
                        GL.Vertex3(-0.5f, 0.99f * zscale, -0.375f + 0.125f * i);
                        GL.Vertex3(0.5f, 0.99f * zscale, -0.375f + 0.125f * i);
                    }
                    for (int i = 8; i <= 24; ++i)
                    {
                        double angle = (i) * Math.PI / 16f;
                        GL.Normal3(Math.Cos(angle), Math.Sin(angle), 0f);
                        GL.Vertex3(-0.5f, (0.5f + Math.Sin(angle) * 0.495f) * zscale, 0.375f - 0.125f * Math.Cos(angle));
                        GL.Vertex3(0.5f, (0.5f + Math.Sin(angle) * 0.495f) * zscale, 0.375f - 0.125f * Math.Cos(angle));
                    }
                    for (int i = 5; i >= 0; --i)
                    {
                        GL.Normal3(0f, -1f, 0f);
                        GL.Vertex3(-0.5f, 0.01f * zscale, -0.375f + 0.125f * i);
                        GL.Vertex3(0.5f, 0.01f * zscale, -0.375f + 0.125f * i);
                    }
                    GL.End();
                    break;
                case BlockCosmeticType.StoneSlabHemisphereTopCap:
                    setBGGLColorAlpha(room.Palette[2], alpha);
                    drawOutlinedSolidCube(1f, 0.25f, 1f);
                    setBGGLColor(room.Palette[0]);
                    drawTopHemisphere(0.25f, 0.333f);
                    break;
                case BlockCosmeticType.TeleporterPad:
                    setBGGLColorAlpha(room.Palette[2], alpha);
                    drawOutlinedSolidCube(0.75f, 0.375f, 0.75f);
                    setBGGLColor(room.Palette[0]);

                    GL.PushMatrix();
                    GL.Translate(0f, 0.375f * zscale, 0f);
                    drawVerticalCylinder(0.3f, 0f, 0.125f * zscale, 0f);
                    GL.PopMatrix();

                    drawFlatCircle(0.3f, 0f, (0.375f + 0.126f) * zscale, 0f);
                    break;
                case BlockCosmeticType.TeleporterTop:
                    setBGGLColorAlpha(room.Palette[2], alpha);
                    GL.PushMatrix();
                    GL.Translate(0f, 0.625f, 0f);
                    drawOutlinedSolidCube(0.75f, 0.375f, 0.75f);
                    GL.PopMatrix();
                    break;
                case BlockCosmeticType.StoneSlabHemisphereBottomCap:
                    setBGGLColorAlpha(room.Palette[2], alpha);
                    GL.PushMatrix();
                    GL.Translate(0f, 0.75f * zscale, 0f);
                    drawOutlinedSolidCube(1f, 0.25f, 1f);
                    GL.PopMatrix();

                    setBGGLColorAlpha(room.Palette[0], alpha);
                    drawBottomHemisphere(0.75f, 0.333f);
                    break;
                case BlockCosmeticType.SandwichBlock:
                    // Bottom solid part:
                    setBGGLColorAlpha(room.Palette[0], alpha);
                    drawOutlinedSolidCube(1f, 0.4f, 1f);

                    // Middle creme filling:
                    setBGGLColorAlpha(room.Palette[2], alpha);
                    GL.PushMatrix();
                    GL.Translate(0f, 0.4f * zscale, 0f);
                    drawOutlinedSolidCube(1f, 0.2f, 1f);
                    GL.PopMatrix();

                    // Top solid part:
                    setBGGLColorAlpha(room.Palette[0], alpha);
                    GL.PushMatrix();
                    GL.Translate(0f, 0.6f * zscale, 0f);
                    drawOutlinedSolidCube(1f, 0.4f, 1f);
                    GL.PopMatrix();
                    break;
                case BlockCosmeticType.VerticalColumn:
                    setBGGLColorAlpha(room.Palette[0], alpha);
                    drawVerticalColumn(1.0f, 0.8f, 1.0f);
                    break;
                case BlockCosmeticType.TransparentOutlined:
                    setBGGLColorAlpha(room.Palette[0], alpha);
                    drawOpenCube(0.975f, 0.975f, 0.975f);
                    break;
                case BlockCosmeticType.RoundedStoneSlab:
                    // TODO: This is awful, fix this.
                    setBGGLColorAlpha(room.Palette[0], alpha);
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
                    setBGGLColorAlpha(room.Palette[2], alpha);
                    drawOutlinedPyramidSpikes(1.0f, 1.0f, 1.0f);
                    break;
                case BlockCosmeticType.PyramidalColumnBottom:
                    setBGGLColorAlpha(room.Palette[2], alpha);
                    drawPyramid(0f, 1.5f, 0f, 0.5f);
                    GL.PushMatrix();
                    GL.Translate(0f, 1f * zscale, 0f);
                    drawSolidCube(0.25f, 2f, 0.25f);
                    GL.PopMatrix();
                    break;
                case BlockCosmeticType.PyramidalColumnTop:
                    // TODO: fix this to be an inverted flange-like thing:
                    setBGGLColorAlpha(room.Palette[0], alpha);
                    drawPyramid(0f, 0.5f, 0f, 0.25f);
                    break;
                default:
                    setBGGLColorAlpha(room.Palette[2], alpha);
                    drawOpenCube(0.975f, 0.975f, 0.975f);
                    break;
            }
            GL.PopMatrix();
        }

        private void drawStaticBlock(StaticBlock b)
        {
            drawBlockType(b.CosmeticType, b.X, b.Y, b.Z, (byte)255);
        }

        private void drawDynamicBlock(DynamicBlock b)
        {
            byte alpha = 255;
            if (b.FunctionalType == BlockFunctionalType.AppearsWhenTouched)
            {
                alpha = 127;
            }
            drawBlockType(b.CosmeticType, b.X, b.Y, b.Z, alpha);
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
                case EntityType.SliderEW:
                    setFGGLColor(ent.Color1);
                    drawOutlinedSolidCube(0.5f, 0.25f, 0.5f);
                    break;
                case EntityType.TransparentCube:
                    setFGGLColor(ent.Color1);
                    drawOpenCube(0.5f, 1.0f, 0.5f);
                    break;
                case EntityType.Hemisphere:
                    // Draw bottom hemisphere:
                    setFGGLColor(ent.Color1);
                    drawBottomHemisphere(0.75f, 0.275f);
                    // Draw top circle:
                    setFGGLColor(ent.Color2);
                    drawFlatCircle(0.275f, 0f, 0.75f * zscale, 0f);
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
            int tex;

            GL.PushMatrix();
            GL.Translate(c - rmHalfWidth + 0.5f, 0.0f, r - rmHalfHeight + 0.5f);
            switch (floorCosmeticType)
            {
                case FloorCosmeticType.Empty:
                    break;
                case FloorCosmeticType.BedOfSpikes2:
                case FloorCosmeticType.BedOfSpikes:
                    setBGGLColor(room.Palette[1], 0.1f);
                    drawSolidFlat(1.0f, 0.0f, 1.0f, (c % 4) * 0.25f, (r % 4) * 0.25f);
                    setBGGLColor(room.Palette[2]);
                    drawBedOfSpikes();
                    break;
#if false
                case FloorCosmeticType.SmallTiles:
                    setBGGLColor(room.Palette[1]);
                    drawOutlinedSmallTiles(1.0f, 0.0f, 1.0f);
                    break;
#endif
                default:
                    tex = floorTextures[(int)floorCosmeticType];
                    if (tex != -1)
                    {
                        GL.Enable(EnableCap.Texture2D);
                        GL.BindTexture(TextureTarget.Texture2D, tex);
                    }
                    setBGGLColor(room.Palette[1], 0.25f);
                    if (tex != -1)
                        drawSolidFlat(1.0f, 0.0f, 1.0f, (c % 4) * 0.25f, (r % 4) * 0.25f);
                    else
                        drawOutlinedSolidFlat(1.0f, 0.0f, 1.0f, (c % 4) * 0.25f, (r % 4) * 0.25f);
                    if (tex != -1)
                    {
                        GL.Disable(EnableCap.Texture2D);
                    }
                    break;
            }
            GL.PopMatrix();
        }

        #endregion

        #region Palette mapping

        private Color getColorByFGPalette(int pidx)
        {
            // Uses NES palette:
            if (palette != null) return palette[pidx];

            // My lame, colorblind attempt at palette mapping:
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
            // Uses NES palette:
            if (palette != null) return palette[pidx];

            // My lame, colorblind attempt at palette mapping:
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
            GL.Color4(clr.R, clr.G, clr.B, (byte)255);
        }

        private void setFGGLColorAlpha(int pidx, byte alpha)
        {
            Color clr = getColorByFGPalette(pidx);
            GL.Color4(clr.R, clr.G, clr.B, alpha);
        }

        private void setBGGLColor(int pidx)
        {
            Color clr = getColorByBGPalette(pidx);
            GL.Color4(clr.R, clr.G, clr.B, (byte)255);
        }

        private void setBGGLColor(int pidx, float scale)
        {
            Color clr = getColorByBGPalette(pidx);
            float multiplier = scale / 255f;
            GL.Color4(clr.R * multiplier, clr.G * multiplier, clr.B * multiplier, 1f);
        }

        private void setBGGLColorAlpha(int pidx, byte alpha)
        {
            Color clr = getColorByBGPalette(pidx);
            GL.Color4(clr.R, clr.G, clr.B, alpha);
        }

        #endregion

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            //Matrix4 lookat = Matrix4.LookAt(7, 5, 7, 1, 1, 1, 0, 1, 0);
            Matrix4 lookat = Matrix4.LookAt(room.Width / 2 + 4, 6, room.Height / 2 + 4, 0, 1, 0, 0, 1, 0);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref lookat);

            // Auto-rotate the room about the Y axis:
            angle += rotation_speed * (float)e.Time;
            GL.Rotate(0f + Math.Cos(angle * Math.PI / 180.0f) * 30f, 0.0f, 1.0f, 0.0f);
            //GL.Rotate(angle, 0.0f, 1.0f, 0.0f);

#if false
            // Draw the wall outline:
            GL.Color3(0.0f, 0.0f, 1.0f);
            GL.PushMatrix();
            GL.Translate(-0.01f, -0.01f, -0.01f);
            drawOpenCube(room.Width + 0.02f, 8.0f + 0.02f, room.Height + 0.02f);
            GL.PopMatrix();
#endif

            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);

            // Draw exits on the walls:
            GL.Color3(0.4f, 0.4f, 0.95f);

#if false
            if (room.HasExitNW)
            {
                GL.PushMatrix();
                GL.Translate(0 - rmHalfWidth, room.ExitNW.Z * zscale + 0.01f, rmHalfHeight - (room.ExitNW.W + 2) + 0.5f);
                drawLeftWall(0.99f, 3.99f);
                GL.PopMatrix();
            }
            if (room.HasExitNE)
            {
                GL.PushMatrix();
                GL.Translate((room.ExitNE.W + 1) - rmHalfWidth + 0.5f, room.ExitNE.Z * zscale + 0.01f, 0 - rmHalfHeight);
                drawFrontWall(0.99f, 3.99f);
                GL.PopMatrix();
            }
#endif
            if (room.HasExitSE)
            {
                GL.PushMatrix();
                GL.Translate(rmHalfWidth, room.ExitSE.Z * zscale + 0.01f, rmHalfHeight - (room.ExitSE.W + 2) + 0.5f);
                drawRightWall(3.99f, 0.99f);
                GL.PopMatrix();
            }
            if (room.HasExitSW)
            {
                GL.PushMatrix();
                GL.Translate((room.ExitSW.W + 1) - rmHalfWidth + 0.5f, room.ExitSW.Z * zscale + 0.01f, rmHalfHeight);
                drawBackWall(0.99f, 3.99f);
                GL.PopMatrix();
            }

            // Draw outlines for exits:
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);

            setBGGLColor(room.Palette[1]);

            for (int i = 0; i < room.Height; ++i)
            {
                bool isExitRow = room.HasExitNW && (room.ExitNW.W == (i - 1));
                if (isExitRow)
                {
                    // Draw the exit above the rest of the wall:
                    GL.PushMatrix();
                    GL.Translate(-rmHalfWidth, room.ExitNW.Z * zscale + 0.01f, rmHalfHeight - (i + 1) + 0.5f);
                    drawLeftWall(1f, 3.99f, (i % 2) * 0.5f, room.ExitNW.Z / 8.0f);
                    GL.PopMatrix();
                }
            }

            for (int i = 0; i < room.Width; ++i)
            {
                bool isExitRow = room.HasExitNE && (room.ExitNE.W == room.Width - (i + 2));
                if (isExitRow)
                {
                    // Draw the exit above the rest of the wall:
                    GL.PushMatrix();
                    GL.Translate(rmHalfWidth - (i + 1) + 0.5f, room.ExitNE.Z * zscale + 0.01f, 0 - rmHalfHeight);
                    drawFrontWall(1f, 3.99f, (i % 2) * 0.5f, room.ExitNE.Z / 8.0f);
                    GL.PopMatrix();
                }
            }

            // Enable fill mode for drawing solid wall sections:
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            // ---- Northwest wall:

            // Enable texturing if we have a texture to use:
            int tex = wallTextures[(int)room.WallNW];
            if (tex != -1)
            {
                GL.Enable(EnableCap.Texture2D);
                GL.BindTexture(TextureTarget.Texture2D, tex);
            }

            setBGGLColor(room.Palette[1]);

            for (int i = 0; i < room.Height; ++i)
            {
                bool isExitRow = room.HasExitNW && (room.ExitNW.W == (i - 1));
                bool hasWindow = (room.WindowMaskNW & (1 << (7 - i))) != 0;
                if (isExitRow)
                {
                    if (room.ExitNW.Z > 0)
                    {
                        // Draw lower part:
                        GL.PushMatrix();
                        GL.Translate(-rmHalfWidth, 0f * zscale + 0.01f, rmHalfHeight - (i + 1) + 0.5f);
                        drawLeftWall(1f, room.ExitNW.Z - 0.01f, (i % 2) * 0.5f, 0f);
                        GL.PopMatrix();
                    }
                    float z2 = ((hasWindow ? 4f : 8f) - (room.ExitNW.Z + 4f));
                    if (z2 > 0)
                    {
                        // Draw upper part:
                        GL.PushMatrix();
                        GL.Translate(-rmHalfWidth, (room.ExitNW.Z + 4f) * zscale + 0.01f, rmHalfHeight - (i + 1) + 0.5f);
                        drawLeftWall(1f, z2 - 0.01f, (i % 2) * 0.5f, (room.ExitNW.Z + 4f) / 8.0f);
                        GL.PopMatrix();
                    }
                }
                else
                {
                    // Just a wall or not:
                    GL.PushMatrix();
                    GL.Translate(0 - rmHalfWidth, 0f * zscale + 0.01f, rmHalfHeight - (i + 1) + 0.5f);
                    drawLeftWall(1f, hasWindow ? 3.99f : 7.99f, (i % 2) * 0.5f, 0f);
                    GL.PopMatrix();
                }
            }

            // Disable texturing if it was enabled:
            if (tex != -1)
            {
                GL.Disable(EnableCap.Texture2D);
            }

            // ---- Northeast wall:

            // Enable texturing if we have a texture to use:
            tex = wallTextures[(int)room.WallNE];
            if (tex != -1)
            {
                GL.Enable(EnableCap.Texture2D);
                GL.BindTexture(TextureTarget.Texture2D, tex);
            }

            setBGGLColor(room.Palette[1]);

            for (int i = 0; i < room.Width; ++i)
            {
                bool isExitRow = room.HasExitNE && (room.ExitNE.W == room.Width - (i + 2));
                bool hasWindow = (room.WindowMaskNE & (1 << (7 - i))) != 0;
                if (isExitRow)
                {
                    if (room.ExitNE.Z > 0)
                    {
                        // Draw lower part:
                        GL.PushMatrix();
                        GL.Translate(rmHalfWidth - (i + 1) + 0.5f, 0f * zscale + 0.01f, 0 - rmHalfHeight);
                        drawFrontWall(1f, room.ExitNE.Z - 0.01f, (i % 2) * 0.5f, 0f);
                        GL.PopMatrix();
                    }
                    float z2 = ((hasWindow ? 4f : 8f) - (room.ExitNE.Z + 4f));
                    if (z2 > 0)
                    {
                        // Draw upper part:
                        GL.PushMatrix();
                        GL.Translate(rmHalfWidth - (i + 1) + 0.5f, (room.ExitNE.Z + 4f) * zscale + 0.01f, 0 - rmHalfHeight);
                        drawFrontWall(1f, z2 - 0.01f, (i % 2) * 0.5f, (room.ExitNE.Z + 4f) / 8.0f);
                        GL.PopMatrix();
                    }
                }
                else
                {
                    // Just a window or not:
                    GL.PushMatrix();
                    GL.Translate(rmHalfWidth - (i + 1) + 0.5f, 0f * zscale + 0.01f, 0 - rmHalfHeight);
                    drawFrontWall(1f, hasWindow ? 3.99f : 7.99f, (i % 2) * 0.5f, 0f);
                    GL.PopMatrix();
                }
            }

            // Disable texturing if it was enabled:
            if (tex != -1)
            {
                GL.Disable(EnableCap.Texture2D);
            }

            // Draw floor sections:
            for (int r = 0; r < room.Height; ++r)
                for (int c = 0; c < room.Width; ++c)
                    if (room.RenderFloor[r, c])
                    {
                        if (room.FloorVisible[r, c])
                        {
                            drawFloor(room.Floor1Cosmetic, c, r);
                        }
                        else// if (room.Floor2Behavior != room.Floor1Behavior)
                        {
                            drawFloor(room.Floor2Cosmetic, c, r);
                        }
                    }

            // TODO: enable shadowing here

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