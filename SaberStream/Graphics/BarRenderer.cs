using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace SaberStream.Graphics
{
    /// <summary>Used to render a bar of smaller segments, each individually coloured.</summary>
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

        /// <summary>Prepares the bar to generate new geometry.</summary>
        /// <param name="width">The width, in pixels, of the full bar when rendered</param>
        /// <param name="height">The height, in pixels, of the full bar when rendered</param>
        /// <param name="expectedSegments">How many individual segments are expected to be added, this is just for optimization</param>
        public void Prepare(float width, float height, int expectedSegments)
        {
            this.Width = width;
            this.Height = height;
            this.NewGeometry = new(expectedSegments * FLOATS_PER_VERTEX * VERTICES_PER_SEGMENT);
            this.XFilled = 0F;
        }

        /// <summary>Adds a segemnt into the new bar geometry. Make sure to call <see cref="Prepare(float, float, int)"/> first.</summary>
        /// <param name="width">The width of this segment. 1.0 would be the entire bar.</param>
        /// <param name="widthIsIncremental">Whether the width of this segment is refernced off the right edge of the previous one (true), or the left edge of the bar (false)</param>
        /// <param name="red">The red component of the segment colour. Between 0.0~1.0</param>
        /// <param name="green">The green component of the segment colour. Between 0.0~1.0</param>
        /// <param name="blue">The blue component of the segment colour. Between 0.0~1.0</param>
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

        /// <summary>Used while adding segments to determine how full the bar currently is with the added segments.</summary>
        /// <returns>The amount of the bar filled. 1.0 is completely full</returns>
        public float GetCurrentWidth() => this.XFilled;

        /// <summary>Finalizes the geometry and renders the bar</summary>
        /// <param name="x">The x location where the bar should be rendered in the window</param>
        /// <param name="y">The y location where the bar should be rendered in the window</param>
        public void Render(float x, float y)
        {
            if (this.NewGeometry == null) { return; }
            this.Geometry = this.NewGeometry.ToArray();
            RectShader!.Use();
            GL.BindVertexArray(this.VertexArrayHandle);
            GL.BindBuffer(BufferTarget.ArrayBuffer, this.VertexBufferHandle);
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * Geometry.Length, Geometry, BufferUsageHint.StaticDraw);
            Matrix4 TranslationMatrix = Matrix4.CreateScale(new Vector3(this.Width, this.Height, 1.0f)) * Matrix4.CreateTranslation(new Vector3(x, y, 0f));
            GL.UniformMatrix4(0, false, ref TranslationMatrix);
            GL.DrawArrays(PrimitiveType.Triangles, 0, this.Geometry.Length / 5);
        }

        /// <summary>Renders the bar without changing geometry.</summary>
        /// <param name="x">The x location where the bar should be rendered in the window</param>
        /// <param name="y">The y location where the bar should be rendered in the window</param>
        public void RenderExisting(float x, float y)
        {
            RectShader!.Use();
            GL.BindVertexArray(this.VertexArrayHandle);
            GL.BindBuffer(BufferTarget.ArrayBuffer, this.VertexBufferHandle);
            Matrix4 TranslationMatrix = Matrix4.CreateScale(new Vector3(this.Width, this.Height, 1.0f)) * Matrix4.CreateTranslation(new Vector3(x, y, 0f));
            GL.UniformMatrix4(0, false, ref TranslationMatrix);
            GL.DrawArrays(PrimitiveType.Triangles, 0, this.Geometry.Length / 5);
        }

        /// <summary>Updates the screen-space projection to correctly place and size the bar in the window</summary>
        /// <param name="newProj">The new projection matrix to use</param>
        public void UpdateProjection(ref Matrix4 newProj)
        {
            RectShader!.Use();
            GL.UniformMatrix4(1, false, ref newProj);
        }
    }
}
