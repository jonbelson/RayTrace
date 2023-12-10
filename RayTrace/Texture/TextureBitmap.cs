using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;

namespace RayTrace
{
    public class TextureBitmap : Texture
    {
        private int width;
        private int height;
        private String fileName;

        //private Colour[] texture = null;
        private int[] texture = null;

        public override TextureType Type { get; } = TextureType.Bitmap;

        // How to deal with uv values that are ouside of the 0 - 1 range.
        public enum WrappingMode
        {
            ClampToEdge, MirroredRepeat, Repeat
        }

        public WrappingMode WrapU { get; set; } = WrappingMode.Repeat;
        public WrappingMode WrapV { get; set; } = WrappingMode.Repeat;

        public string FileName {
            get => fileName;
            set {
                if (fileName != value)
                {
                    fileName = value;
                    LoadTexture(fileName);
                }
            }
        }

        // Attempt to load teh specified bitmap texture.
        private bool LoadTexture(string fileName)
        {
            try
            {
                Bitmap bitmap = new Bitmap(fileName);
                if (bitmap != null)
                {
                    width = bitmap.Width;
                    height = bitmap.Height;
                    BitmapData bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);

                    //texture = new Colour[width * height];
                    texture = new int[width * height];

                    //int[] row = new int[width];

                    for (int y = 0; y < height; y++)
                    {
                        IntPtr source = bitmapData.Scan0 + (height - y - 1) * bitmapData.Stride;

                        //System.Runtime.InteropServices.Marshal.Copy(source, row, 0, width);
                        System.Runtime.InteropServices.Marshal.Copy(source, texture, y*width, width);
                    }

                    bitmap.UnlockBits(bitmapData);

                    this.fileName = fileName;

                    return true;
                }
            }
            catch (ArgumentException e)
            {
                MessageBox.Show(e.Message);
            }

            fileName = "";

            return false;
        }

        //
        // Constructor.
        //
        public TextureBitmap()
        {
            Name = "Bitmap";

            width = height = 0;
        }

        //
        // Constructor.
        //
        public TextureBitmap(string fileName)
        {
            Name = "Bitmap";

            width = height = 0;

            LoadTexture(fileName);
        }

        //
        // Constructor.
        //
        public TextureBitmap(XMLTextureBitmap xml)
        {
            width = height = 0;

            LoadTexture(xml.fileName);
        }

        public bool IsValid() => texture != null;

        public Colour GetColour(float u, float v)
        {
            if (texture != null)
            {
                switch (WrapU)
                {
                    case WrappingMode.ClampToEdge:
                        u = Math.Clamp(u, 0, 1);
                        break;
                    case WrappingMode.Repeat:
                        {
                            if (u > 0.0f)
                            {
                                u = u - (float) Math.Floor(u);
                            }
                            else
                            {
                                float u2 = Math.Abs(u);
                                u2 = u2 - (float)Math.Floor(u2);
                                u = 1.0f - u2;
                            }
                            u = Math.Clamp(u, 0, 1);
                        }
                        break;
                    case WrappingMode.MirroredRepeat:
                        {
                            // TODO
                        }
                        break;
                }

                switch (WrapV)
                {
                    case WrappingMode.ClampToEdge:
                        v = Math.Clamp(v, 0, 1);
                        break;
                    case WrappingMode.Repeat:
                        {
                            if (v > 0.0f)
                            {
                                v = v - (float)Math.Floor(v);
                            }
                            else
                            {
                                float v2 = Math.Abs(v);
                                v2 = v2 - (float)Math.Floor(v2);
                                v = 1.0f - v2;
                            }
                            v = Math.Clamp(v, 0, 1);
                        }
                        break;
                    case WrappingMode.MirroredRepeat:
                        {
                            // TODO
                        }
                        break;
                }


                //u = Math.Clamp(u, 0, 1);
                //v = Math.Clamp(v, 0, 1);

                int x = (int)(u * (width - 1));
                int y = (int)(v * (height - 1));

                Debug.Assert(y * width + x < texture.Length);

                //return texture[y * width + x];

                uint rgb = (uint) texture[y * width + x];
                return new Colour(rgb);
            }

            return Palette.Grey;
        }

        public override Colour GetColour(HitResult hr)
        {
            if (texture != null)
            {
                float u = hr.u;
                float v = hr.v;

                u = Math.Clamp(u, 0, 1);
                v = Math.Clamp(v, 0, 1);

                int x = (int)(u * (width - 1));
                int y = (int)(v * (height - 1));

                //return texture[y * width + x];
                uint rgb = (uint)texture[y * width + x];
                return new Colour(rgb);
            }

            return Palette.Grey;
        }

        public override XMLTexture ToXML()
        {
            XMLTextureBitmap xml = new XMLTextureBitmap();
            //xml.width = width;
            //xml.height = height;
            xml.fileName = fileName;
            return xml;
        }
    }
}
