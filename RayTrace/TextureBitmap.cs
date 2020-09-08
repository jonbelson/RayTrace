using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;

namespace RayTrace
{
    public class TextureBitmap : Texture
    {
        private int width;
        private int height;
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

                    return true;
                }
            }
            catch (ArgumentException e)
            {
                MessageBox.Show(e.Message);
            }

            return false;
        }

        public TextureBitmap(string fileName)
        {
            width = height = 0;

            LoadTexture(fileName);
        }

        public Colour GetColour(float u, float v)
        {
            if (texture != null)
            {
                int x = (int)(u * width);
                int y = (int)(v * height);

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

                int x = (int)(u * width);
                int y = (int)(v * height);

                return texture[y * width + x];
            }

            return Palette.Grey;
        }
    }
}
