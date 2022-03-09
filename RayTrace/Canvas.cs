using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;

namespace RayTrace
{
    public class Canvas
    {
        // Pixel buffer in RGB format.
        public uint[] buffer;

        public uint Background { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }

        private bool InRange(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        //
        // Constructor
        //
        public Canvas(int width, int height, Colour Background) : this(width, height, Background.ToRGB()) { }

        //
        // Constructor
        //
        public Canvas(int width, int height, uint background = 0)
        {
            Width = width;
            Height = height;

            Background = background;

            buffer = new uint[width * height];

            Clear();
        }

        public void Fill(int x, int y, int width, int height, Colour colour)
        {
            uint rgb = colour.ToRGB();

            for (int row = x; row < x + width; row++)
            {
                for (int col = y; col < y + height; col++)
                {
                    if (InRange(row, col))
                    {
                        buffer[row * Width + col] = rgb;
                    }
                }
            }
        }

        public void Clear()
        {
             Array.Fill<uint>(buffer, Background);
        }

        public void Set(int x, int y, uint val)
        {
            if (InRange(x, y))
            {
                buffer[y * Width + x] = val;
            }
        }

        public uint Get(int x, int y)
        {
            if (InRange(x, y))
            {
                return buffer[y * Width + x];
            }
            return 0;
        }

        public bool Save(string path)
        {
            return Save(path, ImageFormat.Tiff);
        }

        public Bitmap GetAsBitmap()
        {
            Bitmap bitmap = null;

            try
            {
                bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppRgb);
                Graphics g = Graphics.FromImage(bitmap);
                //g.DrawLine(new Pen(Color.White), 0, 0, Width, Height);

                // Transfer data to Bitmap.
                BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
                //IntPtr dest = bitmapData.Scan0;

                int[] row = new int[Width];

                for (int y = 0; y < Height; y++)
                {
                    IntPtr dest = bitmapData.Scan0 + y * bitmapData.Stride;

                    // Copy the unsigned values in the buffer array to an int array.
                    //Buffer.BlockCopy(buffer, y*Width, row, 0, Width*sizeof(int));

                    for (int i = 0; i < Width; i++) row[i] = (int)buffer[y * Width + i];

                    // Copy to the Bitmap scanline.
                    System.Runtime.InteropServices.Marshal.Copy(row, 0, dest, Width);
                }

                bitmap.UnlockBits(bitmapData);

            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

            return bitmap;
        }

        public bool Save(string path, ImageFormat format)
        {
            bool success = false;

            try
            {
                Bitmap bitmap = GetAsBitmap();

                path += "." + format.ToString();
                bitmap.Save(path, format);

                success = true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

            return success;
        }
    }
}
