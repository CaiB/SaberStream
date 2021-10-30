using System;
using System.IO;
using System.Reflection;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SharpFont;

namespace SaberStream.Graphics
{
    /// <summary>Used to render a specific font in the overlay</summary>
    public class FontRenderer
    {
        private static readonly Vector2 HORIZONTAL = new(1F, 0F);
        private const string PATH_PREFIX = "SaberStream.Graphics.Resources.";

        private readonly Library Library;
        private readonly Face Face;
        private readonly Character[] Characters;
        private readonly int VertexBufferHandle, VertexArrayHandle;
        private readonly Shader Shader;

        public struct Character
        {
            public int TextureID { get; set; }
            public Vector2 Size { get; set; }
            public Vector2 Bearing { get; set; }
            public int Advance { get; set; }
        }

        /// <summary>Prepares the font and renderer for use.</summary>
        /// <param name="fontFile">The name of the font file to use, it should be an embedded resource in the Resources folder</param>
        /// <param name="baseHeight">The base height, in pixels, to render the font textures with. Trying to render fonts larger than this will create pixelated text, but making this larger increases the size of the textures</param>
        public FontRenderer(string fontFile, uint baseHeight)
        {
            byte[] FontFile;
            Assembly Asm = Assembly.GetExecutingAssembly();
            using (Stream? FontStream = Asm.GetManifestResourceStream(PATH_PREFIX + fontFile))
            {
                if (FontStream == null) { throw new Exception($"Could not load font file \"{PATH_PREFIX}{fontFile}\""); }
                using (MemoryStream Memory = new())
                {
                    FontStream.CopyTo(Memory);
                    FontFile = Memory.ToArray();
                }
            }

            this.Library = new();
            this.Face = new(this.Library, FontFile, 0);
            this.Face.SetPixelSizes(0, baseHeight);

            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

            this.Characters = new Character[128];
            for (uint CharID = 0; CharID < this.Characters.Length; CharID++)
            {
                try
                {
                    // load glyph
                    this.Face.LoadChar(CharID, LoadFlags.Render, LoadTarget.Normal);
                    GlyphSlot Glyph = this.Face.Glyph;
                    FTBitmap GlyphBitmap = Glyph.Bitmap;
                    //if (GlyphBitmap.Width == 0 && GlyphBitmap.Rows == 0) { continue; }

                    // create glyph texture
                    int TextureHandle = GL.GenTexture();
                    GL.BindTexture(TextureTarget.Texture2D, TextureHandle);
                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R8, GlyphBitmap.Width, GlyphBitmap.Rows, 0, PixelFormat.Red, PixelType.UnsignedByte, GlyphBitmap.Buffer);

                    // set texture parameters
                    GL.TextureParameter(TextureHandle, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                    GL.TextureParameter(TextureHandle, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                    GL.TextureParameter(TextureHandle, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                    GL.TextureParameter(TextureHandle, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

                    // add character
                    Character Char = new();
                    Char.TextureID = TextureHandle;
                    Char.Size = new Vector2(GlyphBitmap.Width, GlyphBitmap.Rows);
                    Char.Bearing = new Vector2(Glyph.BitmapLeft, Glyph.BitmapTop);
                    Char.Advance = Glyph.Advance.X.Value;
                    this.Characters[CharID] = Char;
                }
                catch (Exception ex) { Console.WriteLine(ex); }
            }

            // create shader
            this.Shader = new("Font.vert", "Font.frag");
            this.Shader.Use();

            // bind default texture
            GL.BindTexture(TextureTarget.Texture2D, 0);

            // set default (4 byte) pixel alignment 
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 4);

            float[] QuadGeometry =
            {
            //   x      y     u     v    
                0.0f, -1.0f, 0.0f, 0.0f,
                0.0f,  0.0f, 0.0f, 1.0f,
                1.0f,  0.0f, 1.0f, 1.0f,
                0.0f, -1.0f, 0.0f, 0.0f,
                1.0f,  0.0f, 1.0f, 1.0f,
                1.0f, -1.0f, 1.0f, 0.0f
            };

            // Create geometry objects
            this.VertexBufferHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, this.VertexBufferHandle);
            GL.BufferData(BufferTarget.ArrayBuffer, 4 * 6 * 4, QuadGeometry, BufferUsageHint.StaticDraw);

            this.VertexArrayHandle = GL.GenVertexArray();
            GL.BindVertexArray(this.VertexArrayHandle);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * 4, 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * 4, 2 * 4);
        }

        /// <summary>Renders the desired text horizontally.</summary>
        /// <param name="text">The string to render</param>
        /// <param name="x">The x-location, in pixels, in the window to render the text at</param>
        /// <param name="y">The y-location, in pixels, in the window to render the text at</param>
        /// <param name="scale">The height scaling from the base height (specified in constructor) which determines how tall the text will be rendered</param>
        /// <returns>The width, in pixles, of the rendered text</returns>
        public float RenderText(string text, float x, float y, float scale) => RenderText(text, x, y, scale, HORIZONTAL);

        /// <summary>Renders the desired text in the specified direction.</summary>
        /// <param name="text">The string to render</param>
        /// <param name="x">The x-location, in pixels, in the window to render the text at</param>
        /// <param name="y">The y-location, in pixels, in the window to render the text at</param>
        /// <param name="scale">The height scaling from the base height (specified in constructor) which determines how tall the text will be rendered</param>
        /// <param name="dir">The rotation to apply to the text</param>
        /// <returns>The width, in pixles, of the rendered text</returns>
        public float RenderText(string text, float x, float y, float scale, Vector2 dir)
        {
            this.Shader.Use();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindVertexArray(this.VertexArrayHandle);

            float AngleRadians = (float)Math.Atan2(dir.Y, dir.X);
            Matrix4 RotationMatrix = Matrix4.CreateRotationZ(AngleRadians);
            Matrix4 TranslationMatrix = Matrix4.CreateTranslation(new Vector3(x, y, 0f));

            // Iterate through all characters
            float CharXOffset = 0.0f;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c > this.Characters.Length) { c = '_'; }
                if (this.Characters[c].TextureID <= 0) { continue; }
                Character Character = this.Characters[c];

                float Width = Character.Size.X * scale;
                float Height = Character.Size.Y * scale;
                float XRel = CharXOffset + (Character.Bearing.X * scale);
                float YRel = ((Character.Size.Y - Character.Bearing.Y) * scale);

                // Now advance cursors for next glyph (note that advance is number of 1/64 pixels)
                CharXOffset += (Character.Advance >> 6) * scale; // Bitshift by 6 to get value in pixels (2^6 = 64 (divide amount of 1/64th pixels by 64 to get amount of pixels))

                Matrix4 scaleM = Matrix4.CreateScale(new Vector3(Width, Height, 1.0f));
                Matrix4 transRelM = Matrix4.CreateTranslation(new Vector3(XRel, YRel, 0.0f));

                Matrix4 modelM = scaleM * transRelM * RotationMatrix * TranslationMatrix; // OpenTK `*`-operator is reversed
                GL.UniformMatrix4(0, false, ref modelM);

                // Render glyph texture over quad
                GL.BindTexture(TextureTarget.Texture2D, Character.TextureID);

                // Render quad
                GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            }

            GL.BindVertexArray(0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            return CharXOffset;
        }

        /// <summary>Calculates how wide a given string will be when rendered, without doing any rendering work.</summary>
        /// <param name="text">The text to analyze</param>
        /// <param name="scale">The font scaling that will be used when rendering</param>
        /// <returns>The width, in pixels, that this text at this scale will occupy</returns>
        public float TextWidth(string text, float scale)
        {
            float CharXOffset = 0.0f;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c > this.Characters.Length) { c = '_'; }
                if (this.Characters[c].TextureID <= 0) { continue; }
                Character Character = this.Characters[c];
                CharXOffset += (Character.Advance >> 6) * scale;
            }
            return CharXOffset;
        }

        public float RenderTextCentered(string text, float xCenter, float y, float scale)
        {
            float Width = TextWidth(text, scale);
            return RenderText(text, xCenter - (Width / 2F), y, scale);
        }

        /// <summary>Sets the colour of the text. This colour will remain until changed again.</summary>
        /// <param name="red">The red component of the colour, in range 0.0~1.0</param>
        /// <param name="green">The green component of the colour, in range 0.0~1.0</param>
        /// <param name="blue">The blue component of the colour, in range 0.0~1.0</param>
        public void SetColour(float red, float green, float blue)
        {
            this.Shader.Use();
            GL.Uniform3(2, red, green, blue);
        }

        /// <summary>Updates the screen-space projection to correctly place and size the text in the window</summary>
        /// <param name="newProj">The new projection matrix to use</param>
        public void UpdateProjection(ref Matrix4 newProj)
        {
            this.Shader.Use();
            GL.UniformMatrix4(1, false, ref newProj);
        }
    }
}
