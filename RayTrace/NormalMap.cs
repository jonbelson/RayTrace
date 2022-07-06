using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RayTrace
{
    public class NormalMap
    {
        private int _width = 0;
        private int _height = 0;
        private string _fileName;

        private uint[] texture;

        public string Name { get; set; }

        public string FileName
        {
            get => _fileName;
            set
            {
                if (_fileName != value)
                {
                    _fileName = value;
                    LoadTexture(_fileName);
                }
            }
        }

        private bool LoadTexture(string fileName)
        {
            try
            {
                Bitmap bitmap = new(fileName);
                if (bitmap != null)
                {
                    _width = bitmap.Width;
                    _height = bitmap.Height;
                    BitmapData bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, _width, _height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                    texture = new uint[_width * _height];

                    int[] row = new int[_width];

                    for (int y = 0; y < _height; y++)
                    {
                        IntPtr source = bitmapData.Scan0 + (_height - y - 1) * bitmapData.Stride;

                        //                    System.Runtime.InteropServices.Marshal.Copy(row, 0, dest, width);
                        System.Runtime.InteropServices.Marshal.Copy(source, row, 0, _width);

                        for (int i = 0; i < _width; i++)
                        {
                            uint rgb = (uint)row[i];
                            //texture[y * _width + i] = new Colour(rgb);
                            texture[y * _width + i] = rgb;
                        }
                    }

                    bitmap.UnlockBits(bitmapData);

                    _fileName = fileName;

                    return true;
                }
            }
            catch (ArgumentException e)
            {
                MessageBox.Show(e.Message);
            }

            _fileName = "";

            return false;
        }

        //
        // Constructor.
        //
        public NormalMap()
        {
            Name = "Normal Map";

            _width = _height = 0;
        }

        //
        // Constructor.
        //
        public NormalMap(string fileName)
        {
            Name = "Normal Map";

            _width = _height = 0;

            LoadTexture(fileName);
        }

        public bool IsValid => texture != null;

        public SVector GetNormal(HitResult hr)
        {
            return GetNormal(hr.u, hr.v);
        }

        public SVector GetNormal(float u, float v)
        {
            if (IsValid)
            {
                u = Math.Clamp(u, 0, 1);
                v = Math.Clamp(v, 0, 1);

                int x = (int)(u * (_width - 1));
                int y = (int)(v * (_height - 1));

                Debug.Assert(y * _width + x < texture.Length);

                uint val = texture[y * _width + x];

                byte r = (byte)(val >> 16);
                byte g = (byte)(val >> 8);
                byte b = (byte)val;

                float i = (r - 128) / 128.0f;
                float j = (g - 128) / 128.0f;
                float k = (b - 128) / 128.0f;

//                SVector n = new SVector(-i, -j, -k);
                SVector n = new SVector(i, -j, -k);
                return n.Normalise();
            }

            return SVector.Null();
        }

    }
}
