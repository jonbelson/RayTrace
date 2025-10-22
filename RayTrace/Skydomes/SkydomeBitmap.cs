using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RayTrace.Skydomes
{
    public class SkydomeBitmap : Skydome
    {
        private int width;
        private int height;
        private string fileName;

        private Colour[] texture = null;

        public Bitmap GetAsBitmap()
        {
            Bitmap bitmap = null;

            try
            {
                bitmap = new Bitmap(width, height, PixelFormat.Format32bppRgb);
                Graphics g = Graphics.FromImage(bitmap);
                //g.DrawLine(new Pen(Color.White), 0, 0, Width, Height);

                // Transfer data to Bitmap.
                BitmapData bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
                //IntPtr dest = bitmapData.Scan0;

                int[] row = new int[width];

                for (int y = 0; y < height; y++)
                {
                    IntPtr dest = bitmapData.Scan0 + y * bitmapData.Stride;

                    // Copy the unsigned values in the buffer array to an int array.
                    //Buffer.BlockCopy(buffer, y*Width, row, 0, Width*sizeof(int));

//                    for (int i = 0; i < width; i++) row[i] = (int)buffer[y * width + i];
                    for (int i = 0; i < width; i++) row[i] = (int) texture[y * width + i].ToRGB(1.0f);

                    // Copy to the Bitmap scanline.
                    System.Runtime.InteropServices.Marshal.Copy(row, 0, dest, width);
                }

                bitmap.UnlockBits(bitmapData);

            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

            return bitmap;
        }

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
                            Colour c = new Colour(rgb);
                            texture[y * width + i] = c.ApplyGamma(2.2f);
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

        private (float u, float v) CalculateUV(Camera camera, SVector ray)
        {
            // Sn   Normal vector at point of interest.
            // Sp   Vector of north pole.
            // Se   Vector to reference point on equator.

            SVector Sn = ray;// hr.EntryNormal;
            SVector Sp = new SVector(0.0f, 1.0f, 0.0f);
            Sp = camera.NormalMatrix * Sp;
            double dp = Sn.DotProduct(Sp);
            double phi = Math.Acos(-dp);

            Debug.Assert(!Double.IsNaN(phi));

            double v = phi / Math.PI;

            SVector Se = new SVector(0, 0, 1);
            Se = camera.NormalMatrix * Se;
            dp = Se.DotProduct(Sn);

            double d = 0.0;
            if (phi != 0.0)
            {
                d = dp / Math.Sin(phi);
            }
            if (d > 1.0) d = 1.0;
            if (d < -1.0) d = -1.0;
            double theta = (Math.Acos(d)) / (2.0 * Math.PI);

            double u = theta;

            SVector Spe = Sp.VectorProduct(Se);
            if (Spe.DotProduct(Sn) <= 0.0)
            {
                u = 1.0 - theta;
            }

            Debug.Assert(!Double.IsNaN(u) && !Double.IsNaN(v));

            //hr.u = (float)(1.0 - u);
            //hr.v = (float)v;

            //u = 1.0 - u;

            return ((float) u, (float) v);
        }


        public SkydomeBitmap(string fileName)
        {
            this.fileName = fileName;

            _ = LoadTexture(fileName);
        }

        public SkydomeBitmap(XMLSkydomeBitmap xmlSkydomeBitmap) : this(xmlSkydomeBitmap.FileName)
        {
        }

        public override Colour GetColour(Camera camera, SVector ray)
        {
            if (texture != null)
            {
                ray = ray.Normalise();

                var result = CalculateUV(camera, ray);

                float u = result.u;
                float v = result.v;

                u = Math.Clamp(u, 0, 1);
                v = Math.Clamp(v, 0, 1);

                int x = (int)(u * (width - 1));
                int y = (int)(v * (height - 1));

                Debug.Assert(y * width + x < texture.Length);

                try
                {
                    Colour c = texture[y * width + x];
                    return c;
                }
                catch (Exception e)
                {

                    MessageBox.Show(e.Message);
                }
            }

            return Palette.Black;
        }

        public override XMLSkydome ToXML()
        {
            XMLSkydomeBitmap xml = new();

            xml.FileName = fileName;

            return xml;
        }

        public override HitResult HitTest(Camera camera, Ray ray)
        {
            return new HitResult(null);
        }
    }
}
