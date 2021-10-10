using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;

namespace RayTrace
{
    public class TextureBitmap : Texture
    {
        public override TextureType Type { get; } = TextureType.Bitmap;

        private int width;
        private int height;
        private String fileName;

        private Colour[] texture = null;

        private bool LoadTexture(string fileName)
        {
            try
            {
                Bitmap bitmap = new Bitmap(fileName);
                if (bitmap != null)
                {
                    width = bitmap.Width;
                    height = bitmap.Height;
                    BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);

                    texture = new Colour[width * height];

                    int[] row = new int[width];

                    for (int y = 0; y < height; y++)
                    {
                        IntPtr source = bitmapData.Scan0 + (height - y - 1) * bitmapData.Stride;

                        //                    System.Runtime.InteropServices.Marshal.Copy(row, 0, dest, width);
                        System.Runtime.InteropServices.Marshal.Copy(source, row, 0, width);

                        for (int i = 0; i < width; i++)
                        {
                            uint rgb = (uint)row[i];
                            texture[y * width + i] = new Colour(rgb);
                        }
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

        public TextureBitmap(string fileName)
        {
            Name = "Bitmap";

            width = height = 0;

            LoadTexture(fileName);
        }

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
                u = Math.Clamp(u, 0, 1);
                v = Math.Clamp(v, 0, 1);

                int x = (int)(u * (width - 1));
                int y = (int)(v * (height - 1));

                Debug.Assert(y * width + x < texture.Length);

                return texture[y * width + x];
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

                return texture[y * width + x];
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
