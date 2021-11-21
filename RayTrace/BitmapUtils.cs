using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RayTrace
{
    public static class BitmapUtils
    {
        public static WriteableBitmap ToWriteableBitmap(this Bitmap bmp)
        {
            // Lock the bitmap's bits.
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height);
            System.Drawing.Imaging.BitmapData bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppRgb);

            // Declare an array to hold the bytes of the bitmap.
            int bufferSize = bmpData.Stride * bmp.Height;

            WriteableBitmap wb = new WriteableBitmap(bmp.Width, bmp.Height, bmp.HorizontalResolution, bmp.VerticalResolution, PixelFormats.Bgr32, null);

            wb.WritePixels(new Int32Rect(0, 0, bmp.Width, bmp.Height), bmpData.Scan0, bufferSize, bmpData.Stride);

            // Unlock the bits.
            bmp.UnlockBits(bmpData);

            //ShowMe.Source = wb;

            return wb;
        }

        public class ColourTexture
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public Colour[] Texture { get; set; }

            public ColourTexture(Colour[] texture, int width, int height) { Width = width; Height = height; Texture = texture; }
        };

        public static bool CreateTexture(Colour colour, int width, int height, out ColourTexture colourTexture)
        {
            Colour[] texture = new Colour[width*height];

            for (int i=0; i<width*height; i++)
            {
                texture[i] = colour;
            }

            colourTexture = new(texture, width, height);

            return true;
        }

        public static bool LoadTexture(string fileName, out ColourTexture colourTexture)
        {
            Colour[] texture = null;
            try
            {
                Bitmap bitmap = new Bitmap(fileName);
                if (bitmap != null)
                {
                    int width = bitmap.Width;
                    int height = bitmap.Height;
                    BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb);

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
                            // Need to 'ungamma' the image since we apply gamma when saving.
                            Colour c = new(rgb);
                            texture[y * width + i] = c.ApplyGamma(2.2f);
//                            texture[y * width + i] = new Colour(rgb);
                        }
                    }

                    bitmap.UnlockBits(bitmapData);

                    //this.fileName = fileName;

                    colourTexture = new ColourTexture(texture, width, height);

                    return true;
                }
            }
            catch (ArgumentException e)
            {
                MessageBox.Show(e.Message);
            }

            colourTexture = null;

            return false;
        }
    }
}
