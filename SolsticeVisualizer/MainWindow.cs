using System;
using System.Collections.Generic;
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
        const float rotation_speed = 15.0f;
        float angle;

        GameData gameData;

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
            if (major <= 1 && minor < 5)
            {
                System.Windows.Forms.MessageBox.Show("You need at least OpenGL 1.5 to run this example. Aborting.", "VBOs not supported",
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Exclamation);
                this.Exit();
            }

            GL.ClearColor(System.Drawing.Color.Black);
            GL.Enable(EnableCap.DepthTest);
            GL.LineWidth(1.5f);

            gameData = new GameData(@"..\..\Solstice (U).nes");
            //loadRoom(0);
            loadRoom(6);

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

        private void drawWireframeFlat(float w, float z, float h)
        {
            float w2 = w * 0.5f;
            float h2 = h * 0.5f;

            GL.Begin(BeginMode.LineStrip);
            GL.Vertex3(-w2, z * zscale, -h2);
            GL.Vertex3(-w2, z * zscale, h2);
            GL.Vertex3(w2, z * zscale, h2);

            GL.Vertex3(-w2, z * zscale, -h2);
            GL.Vertex3(w2, z * zscale, -h2);
            GL.Vertex3(w2, z * zscale, h2);
            GL.End();
        }

        private void drawStaticBlock(StaticBlock b)
        {
            GL.Color3(0.7f, 0.7f, 0.4f);

            GL.PushMatrix();
            GL.Translate(
                (b.X - rmHalfWidth + 0.5f),
                (b.Z * zscale),
                (b.Y - rmHalfHeight + 0.5f)
            );
            switch (b.CosmeticType)
            {
                case BlockCosmeticType.Solid:
                    drawSolidCube(1.0f, 1.0f, 1.0f);
                    GL.Color3(0, 0, 0);
                    drawOpenCube(1.0f, 1.0f, 1.0f);
                    break;
                default:
                    drawOpenCube(1.0f, 1.0f, 1.0f);
                    break;
            }
            GL.PopMatrix();
        }

        private void drawDynamicBlock(DynamicBlock b)
        {
            Color clr = Color.FromKnownColor((KnownColor)((int)b.CosmeticType * 2 + 28));

            GL.Color3(rampUp(clr.R), rampUp(clr.G), rampUp(clr.B));

            GL.PushMatrix();
            GL.Translate(
                (b.X - rmHalfWidth + 0.5f) * 1.0f,
                (b.Z * zscale) * 1.0f,
                (b.Y - rmHalfHeight + 0.5f) * 1.0f
            );
            drawOpenCube(1.0f, 1.0f, 1.0f);
            GL.PopMatrix();
        }

        private void drawEntity(StaticEntity e)
        {
            Color clr = Color.FromKnownColor((KnownColor)(e.EntityType * 2 + 28));

            GL.Color3(rampUp(clr.R), rampUp(clr.G), rampUp(clr.B));

            GL.PushMatrix();
            GL.Translate(
                (e.X * 0.5f - rmHalfWidth + 0.5f) * 1.0f,
                (e.Z * zscale + (zscale * 0.5f)) * 1.0f,
                (e.Y * 0.5f - rmHalfHeight + 0.5f) * 1.0f
            );
            drawSolidCube(0.5f, 1.0f, 0.5f);
            GL.PopMatrix();
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

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Matrix4 lookat = Matrix4.LookAt(8, 10, 8, 0, 2, 0, 0, 1, 0);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref lookat);

            // Auto-rotate the room about the Y axis:
            angle += rotation_speed * (float)e.Time;
            GL.Rotate(Math.Cos(angle * Math.PI / 180.0f) * 15.0f + 5.0f, 0.0f, 1.0f, 0.0f);

            // Draw the wall outline:
            GL.Color3(0.0f, 0.0f, 1.0f);
            GL.PushMatrix();
            drawOpenCube(room.Width + 0.1f, 8.0f, room.Height + 0.1f);
            GL.PopMatrix();

            // Draw floor sections:
            for (int r = 0; r < room.Height; ++r)
                for (int c = 0; c < room.Width; ++c)
                    if (room.FloorVisible[r, c])
                    {
                        GL.PushMatrix();
                        GL.Translate(c - rmHalfWidth + 0.5f, 0.0f, r - rmHalfHeight + 0.5f);
                        GL.Color3(0.1f, 0.4f, 0.1f);
                        switch (room.Floor1Cosmetic)
                        {
                            case FloorCosmeticType.SmallTiles:
                                drawSmallTiles(1.0f, 0.0f, 1.0f);
                                break;
                            default:
                                drawSolidFlat(1.0f, 0.0f, 1.0f);
                                break;
                        }
                        GL.PopMatrix();
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