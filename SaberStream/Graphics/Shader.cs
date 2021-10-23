using OpenTK.Graphics.OpenGL;
using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace SaberStream.Graphics
{
    /// <summary>Class for creating and managing a graphics shader.</summary>
    public sealed class Shader : IDisposable
    {
        private const string PATH_PREFIX = "SaberStream.Graphics.Resources.";
        private readonly int ShaderHandle;
        private bool IsDisposed = false;

        public Shader(string vertexPath, string fragmentPath)
        {
            int VertexShaderHandle, FragmentShaderHandle;

            string VertexShaderSource, FragmentShaderSource;

            Assembly Asm = Assembly.GetExecutingAssembly();
            using (Stream? VertexStream = Asm.GetManifestResourceStream(PATH_PREFIX + vertexPath))
            {
                if (VertexStream == null) { throw new Exception($"Could not load vertex shader \"{PATH_PREFIX}{vertexPath}\""); }
                using (StreamReader VertexReader = new(VertexStream, Encoding.UTF8)) { VertexShaderSource = VertexReader.ReadToEnd(); }
            }
            using (Stream? FragmentStream = Asm.GetManifestResourceStream(PATH_PREFIX + fragmentPath))
            {
                if (FragmentStream == null) { throw new Exception($"Could not load fragment shader \"{PATH_PREFIX}{vertexPath}\""); }
                using (StreamReader FragmentReader = new(FragmentStream, Encoding.UTF8)) { FragmentShaderSource = FragmentReader.ReadToEnd(); }
            }

            VertexShaderHandle = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(VertexShaderHandle, VertexShaderSource);

            FragmentShaderHandle = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(FragmentShaderHandle, FragmentShaderSource);

            GL.CompileShader(VertexShaderHandle);
            string LogVertex = GL.GetShaderInfoLog(VertexShaderHandle);
            if (!string.IsNullOrEmpty(LogVertex)) { Console.WriteLine("Vertex Shader Problems Found! Log:\n" + LogVertex); }

            GL.CompileShader(FragmentShaderHandle);
            string LogFragment = GL.GetShaderInfoLog(FragmentShaderHandle);
            if (!string.IsNullOrEmpty(LogFragment)) { Console.WriteLine("Fragment Shader Problems Found! Log:\n" + LogFragment); }

            this.ShaderHandle = GL.CreateProgram();
            GL.AttachShader(this.ShaderHandle, VertexShaderHandle);
            GL.AttachShader(this.ShaderHandle, FragmentShaderHandle);
            GL.LinkProgram(this.ShaderHandle);

            GL.DetachShader(this.ShaderHandle, VertexShaderHandle);
            GL.DetachShader(this.ShaderHandle, FragmentShaderHandle);
            GL.DeleteShader(VertexShaderHandle);
            GL.DeleteShader(FragmentShaderHandle);
        }

        public void Use() => GL.UseProgram(this.ShaderHandle);

        public int GetUniformLocation(string name) => GL.GetUniformLocation(this.ShaderHandle, name);

        internal void Dispose(bool disposing)
        {
            if (!this.IsDisposed)
            {
                GL.DeleteProgram(this.ShaderHandle);
                this.IsDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
