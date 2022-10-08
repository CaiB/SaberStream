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

        /// <summary>Converts image data (e.g. PNG) to raw pixel data</summary>
        /// <param name="img">The raw image data</param>
        /// <returns>An array of pixels in {A B G R} format for each pixel</returns>
        private static byte[] ImageToRGBA(Image<Rgba32> img)
        {
            byte[] Pixels = Array.Empty<byte>();
            img.ProcessPixelRows(PixAccess =>
            {
                Pixels = new byte[(PixAccess.Height * PixAccess.Width) * 4];
                for (int Y = 0; Y < PixAccess.Height; Y++)
                {
                    Span<Rgba32> Row = PixAccess.GetRowSpan(Y);
                    for (int X = 0; X < Row.Length; X++)
                    {
                        int Index = Y * PixAccess.Width + X;
                        Pixels[(Index * 4) + 0] = Row[X].R;
                        Pixels[(Index * 4) + 1] = Row[X].G;
                        Pixels[(Index * 4) + 2] = Row[X].B;
                        Pixels[(Index * 4) + 3] = Row[X].A;
                    }
                }
            });

            return Pixels;
        }

        /// <summary>Uploads new texture data to the GPU from a PNG file.</summary>
        /// <param name="pngData">The PNG file's raw contents</param>
        public void NewDataPNG(byte[] pngData)
        {
            Image<Rgba32> TexImage = Image.Load<Rgba32>(pngData);
            byte[] Pixels = ImageToRGBA(TexImage);
            GL.BindTexture(this.Type, this.Handle);
            Use(this.Unit);
            GL.TexImage2D(this.Type, 0, PixelInternalFormat.Rgba, TexImage.Width, TexImage.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, Pixels);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }

        /// <summary>Uploads new texture data to the GPU from raw pixel data.</summary>
        /// <param name="rgbaData">The raw pixel data. Format is {A B G R}, repeated for each pixel. The length must be a multiple of 4.</param>
        /// <param name="width">The width of the texture, in pixels</param>
        /// <param name="height">The height of the texture, in pixels</param>
        public void NewDataRGBA(byte[] rgbaData, int width, int height)
        {
            GL.BindTexture(this.Type, this.Handle);
            Use(this.Unit);
            GL.TexImage2D(this.Type, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, rgbaData);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }

        /// <summary>Binds this texture so it's ready for use.</summary>
        /// <param name="unit">The texture unit to use</param>
        public void Use(TextureUnit unit = TextureUnit.Texture0)
        {
            GL.ActiveTexture(unit);
            GL.BindTexture(this.Type, this.Handle);
        }
    }
}
