using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;

namespace SaberStream.Graphics
{
    public class TextureRenderer
    {
        private static Shader? IconShader;
        private static readonly float[] Geometry =
        {
            //x      y     u     v    
            0.0f, -1.0f, 0.0f, 0.0f,
            0.0f,  0.0f, 0.0f, 1.0f,
            1.0f,  0.0f, 1.0f, 1.0f,
            0.0f, -1.0f, 0.0f, 0.0f,
            1.0f,  0.0f, 1.0f, 1.0f,
            1.0f, -1.0f, 1.0f, 0.0f
        };

        private readonly int VertexArrayHandle, VertexBufferHandle;

        /// <summary>Preapres a textured rectangle renderer for rendering supplied textures.</summary>
        public TextureRenderer()
        {
            if (IconShader == null) { IconShader = new("Passthrough2Textured.vert", "Passthrough2Textured.frag"); }
            this.VertexBufferHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, this.VertexBufferHandle);
            GL.BufferData(BufferTarget.ArrayBuffer, 4 * 6 * 4, Geometry, BufferUsageHint.StaticDraw);

            this.VertexArrayHandle = GL.GenVertexArray();
            GL.BindVertexArray(this.VertexArrayHandle);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
        }

        public void Render(Texture tex, float x, float y, float scale, float zIndex)
        {
            Matrix4 TranslationMatrix = Matrix4.CreateScale(new Vector3(scale, scale, 1F)) * Matrix4.CreateTranslation(new Vector3(x, y, zIndex));
            Render(tex, ref TranslationMatrix);
        }

        public void Render(Texture tex, float x, float y, float scaleX, float scaleY, float zIndex)
        {
            Matrix4 TranslationMatrix = Matrix4.CreateScale(new Vector3(scaleX, scaleY, 1F)) * Matrix4.CreateTranslation(new Vector3(x, y, zIndex));
            Render(tex, ref TranslationMatrix);
        }

        private void Render(Texture tex, ref Matrix4 transform)
        {
            IconShader!.Use();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindVertexArray(this.VertexArrayHandle);
            GL.UniformMatrix4(0, false, ref transform);
            tex.Use();
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        }

        public void UpdateProjection(ref Matrix4 newProj)
        {
            IconShader!.Use();
            GL.UniformMatrix4(1, false, ref newProj);
        }
    }
}
