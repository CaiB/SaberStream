using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using System.IO;

namespace SaberStream.Graphics
{
    public class Texture
    {
        private const string PATH_PREFIX = "SaberStream.Graphics.Resources.";
        private readonly int Handle;
        private readonly TextureUnit Unit;
        private readonly TextureTarget Type;

        /// <summary>Makes a standard 2D texture</summary>
        /// <param name="path">Where to load the texture from. Specify null if no texture should be loaded now.</param>
        /// <param name="useFiltering">Whether to apply linear interpolation to the texture when rendering it scaled</param>
        /// <param name="unit">The texture unit to place the texture into</param>
        public Texture(string? path, bool useFiltering, TextureUnit unit = TextureUnit.Texture0)
        {
            this.Type = TextureTarget.Texture2D;
            this.Unit = unit;
            GL.BindTexture(this.Type, 0);
            this.Handle = GL.GenTexture();
            Use(this.Unit);

            if(path != null)
            {
                Assembly Asm = Assembly.GetExecutingAssembly();
                Image<Rgba32> TexImage;
                using (Stream? TexStream = Asm.GetManifestResourceStream(PATH_PREFIX + path))
                {
                    if (TexStream == null) { throw new Exception($"Could not load texture \"{PATH_PREFIX}{path}\""); }
                    TexImage = Image.Load<Rgba32>(TexStream);
                }

                byte[] Pixels = ImageToRGBA(TexImage);
                GL.TexImage2D(this.Type, 0, PixelInternalFormat.Rgba, TexImage.Width, TexImage.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, Pixels);
            }
            else { GL.TexImage2D(this.Type, 0, PixelInternalFormat.Rgba, 1, 1, 0, PixelFormat.Rgba, PixelType.UnsignedByte, new byte[] { 0, 0, 0, 0 }); }

            GL.TexParameter(this.Type, TextureParameterName.TextureMinFilter, (int)(useFiltering ? TextureMinFilter.Linear : TextureMinFilter.Nearest));
            GL.TexParameter(this.Type, TextureParameterName.TextureMagFilter, (int)(useFiltering ? TextureMagFilter.Linear : TextureMagFilter.Nearest));

            GL.TexParameter(this.Type, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
            GL.TexParameter(this.Type, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }

        private byte[] ImageToRGBA(Image<Rgba32> img)
        {
            if (!img.TryGetSinglePixelSpan(out Span<Rgba32> PixelSpan)) { throw new Exception("Could not get pixel span"); }
            byte[] Pixels = new byte[PixelSpan.Length * 4];
            for (int i = 0; i < PixelSpan.Length; i++) // TODO: This seems inefficient.
            {
                Pixels[(i * 4) + 0] = PixelSpan[i].R;
                Pixels[(i * 4) + 1] = PixelSpan[i].G;
                Pixels[(i * 4) + 2] = PixelSpan[i].B;
                Pixels[(i * 4) + 3] = PixelSpan[i].A;
            }
            return Pixels;
        }

        public void NewDataPNG(byte[] pngData)
        {
            Image<Rgba32> TexImage = Image.Load<Rgba32>(pngData);
            byte[] Pixels = ImageToRGBA(TexImage);
            GL.BindTexture(this.Type, this.Handle);
            Use(this.Unit);
            GL.TexImage2D(this.Type, 0, PixelInternalFormat.Rgba, TexImage.Width, TexImage.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, Pixels);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }

        public void NewDataRGBA(byte[] rgbaData, int width, int height)
        {
            GL.BindTexture(this.Type, this.Handle);
            Use(this.Unit);
            GL.TexImage2D(this.Type, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, rgbaData);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }

        public void Use(TextureUnit unit = TextureUnit.Texture0)
        {
            GL.ActiveTexture(unit);
            GL.BindTexture(this.Type, this.Handle);
        }
    }
}
