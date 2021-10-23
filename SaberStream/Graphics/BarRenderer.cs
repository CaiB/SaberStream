using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace SaberStream.Graphics
{
    public class BarRenderer
    {
        private static Shader? RectShader;
        private readonly int VertexArrayHandle, VertexBufferHandle;

        private float Width, Height;
        private float XFilled;

        private const int FLOATS_PER_VERTEX = 5;
        private const int VERTICES_PER_SEGMENT = 6;
        private float[] Geometry =
        {
        //  X    Y    R    G   B
            0F, -1F, 0.2F, 0F, 0F,
            0F,  0F, 0.2F, 0F, 0F,
            1F,  0F, 0.2F, 0F, 0F,
            0F, -1F, 0.2F, 0F, 0F,
            1F,  0F, 0.2F, 0F, 0F,
            1F, -1F, 0.2F, 0F, 0F,
        };
        private List<float>? NewGeometry;

        public BarRenderer()
        {
            if (RectShader == null) { RectShader = new("Passthrough2Colour.vert", "Passthrough2Colour.frag"); }
            this.VertexBufferHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, this.VertexBufferHandle);
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * Geometry.Length, Geometry, BufferUsageHint.StaticDraw);

            this.VertexArrayHandle = GL.GenVertexArray();
            GL.BindVertexArray(this.VertexArrayHandle);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, FLOATS_PER_VERTEX * sizeof(float), 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, FLOATS_PER_VERTEX * sizeof(float), 2 * sizeof(float));
        }

        public void Prepare(float width, float height, int expectedSegments)
        {
            this.Width = width;
            this.Height = height;
            this.NewGeometry = new(expectedSegments * FLOATS_PER_VERTEX * VERTICES_PER_SEGMENT);
            this.XFilled = 0F;
        }

        public void AddSegment(float width, bool widthIsIncremental, float red, float green, float blue)
        {
            if (this.NewGeometry == null) { throw new Exception("Cannot add segments before calling Prepare()"); }
            float Left = this.XFilled;
            float Right = widthIsIncremental ? (Left + width) : MathF.Max(Left, width);

            void AddColours(float red, float green, float blue)
            {
                this.NewGeometry.Add(red);
                this.NewGeometry.Add(green);
                this.NewGeometry.Add(blue);
            }

            this.NewGeometry.Add(Left);
            this.NewGeometry.Add(-1F);
            AddColours(red, green, blue);
            this.NewGeometry.Add(Left);
            this.NewGeometry.Add(0F);
            AddColours(red, green, blue);
            this.NewGeometry.Add(Right);
            this.NewGeometry.Add(0F);
            AddColours(red, green, blue);

            this.NewGeometry.Add(Left);
            this.NewGeometry.Add(-1F);
            AddColours(red, green, blue);
            this.NewGeometry.Add(Right);
            this.NewGeometry.Add(0F);
            AddColours(red, green, blue);
            this.NewGeometry.Add(Right);
            this.NewGeometry.Add(-1F);
            AddColours(red, green, blue);

            this.XFilled = Right;
        }

        public float GetCurrentWidth() => this.XFilled;

        public void Render(float x, float y)
        {
            RectShader!.Use();
            GL.BindVertexArray(this.VertexArrayHandle);
            GL.BindBuffer(BufferTarget.ArrayBuffer, this.VertexBufferHandle);
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * Geometry.Length, Geometry, BufferUsageHint.StaticDraw);
            Matrix4 TranslationMatrix = Matrix4.CreateScale(new Vector3(this.Width, this.Height, 1.0f)) * Matrix4.CreateTranslation(new Vector3(x, y, 0f));
            GL.UniformMatrix4(0, false, ref TranslationMatrix);
            GL.DrawArrays(PrimitiveType.Triangles, 0, this.Geometry.Length / 5);
        }

        public void UpdateProjection(ref Matrix4 newProj)
        {
            RectShader!.Use();
            GL.UniformMatrix4(1, false, ref newProj);
        }
    }
}
