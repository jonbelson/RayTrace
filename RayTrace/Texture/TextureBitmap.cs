using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Windows;

namespace RayTrace
{
    public class TextureBitmap : Texture, INotifyPropertyChanged
    {
        private int width;
        private int height;
        private String fileName;

        private int[] texture = null;

        public event PropertyChangedEventHandler PropertyChanged;

        private void Notify([CallerMemberName] String propName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        // Lookup table from sRGB luminance to linear.
        private float[] _sRGBToLinearLUT = new float[256];

        private void CreateLUT()
        {
            int entries = _sRGBToLinearLUT.Length;

            for (int i=0; i<entries; i++)
            {
                float val = ((float)i)/entries;

                _sRGBToLinearLUT[i] = val <= 0.04045f ? val / 12.92f : MathF.Pow((val + 0.055f) / 1.055f, 2.4f);
            }
        }

        public override TextureType Type { get; } = TextureType.Bitmap;

        // How to deal with uv values that are ouside of the 0 - 1 range.
        public enum WrappingMode
        {
            ClampToEdge, MirroredRepeat, Repeat
        }

        public WrappingMode WrapU { get; set; } = WrappingMode.Repeat;
        public WrappingMode WrapV { get; set; } = WrappingMode.Repeat;

        public enum TextureOrigin
        {
            TopLeft,        // Used by glTF (https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#images)
            BottomLeft      // Used by OpenGL.
        }

        public TextureOrigin Origin = TextureOrigin.BottomLeft;

        public string FileName {
            get => fileName;
            set {
                if (fileName != value)
                {
                    fileName = value;
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        LoadTexture(fileName);
                    }
                    Notify();
                }
            }
        }

        // Attempt to load the specified bitmap texture.
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

                    texture = new int[width * height];

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

            CreateLUT();
        }

        //
        // Constructor.
        //
        public TextureBitmap(string fileName)
        {
            Name = "Bitmap";

            width = height = 0;

            CreateLUT();

            LoadTexture(fileName);
        }

        //
        // Constructor.
        //
        public TextureBitmap(XMLTextureBitmap xml)
        {
            width = height = 0;

            ConvertToLinearRGB = xml.ConvertToLinearRGB;

            LoadTexture(xml.fileName);
        }

        public void Clear()
        {
            texture = null;
        }

        public bool IsValid() => texture != null;

        public SColour GetSColour(float u, float v)
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


                if (Origin == TextureOrigin.TopLeft) v = 1.0f - v;

                int x = (int)(u * (width - 1));
                int y = (int)(v * (height - 1));

                Debug.Assert(y * width + x < texture.Length);

                uint rgb = (uint) texture[y * width + x];

                SColour c = SColour.FromRGB(rgb);

                if (ConvertToLinearRGB)
                {
                    c = c.ToLinearRGB();

                    //c = new SColour(
                    //    _sRGBToLinearLUT[(int)(c.Red * 255.0f)] ,
                    //    _sRGBToLinearLUT[(int)(c.Green * 255.0f)] ,
                    //    _sRGBToLinearLUT[(int)(c.Blue * 255.0f)] );
                }

                return c;
            }

            return SPalette.Grey;
        }

        public override SColour GetSColour(HitResult hr)
        {
            return GetSColour(hr.u, hr.v);
        }

        public override XMLTexture ToXML()
        {
            XMLTextureBitmap xml = new XMLTextureBitmap();

            xml.ConvertToLinearRGB = ConvertToLinearRGB;
            xml.fileName = fileName;

            return xml;
        }
    }
}
