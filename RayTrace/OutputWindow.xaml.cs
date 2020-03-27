using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RayTrace
{
    /// <summary>
    /// Interaction logic for OutputWindow.xaml
    /// </summary>
    public partial class OutputWindow : Window
    {
        public Canvas Canvas { get; set; }

        public OutputWindow()
        {
            InitializeComponent();
        }

        private void GetDpi(out double dpiX, out double dpiY)
        {
            PresentationSource source = PresentationSource.FromVisual(this);

            dpiX = 96.0;
            dpiY = 96.0;

            //double dpiX, dpiY;
            if (source != null)
            {
                dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;
                dpiY = 96.0 * source.CompositionTarget.TransformToDevice.M22;
            }
        }

        public void Update()
        {
            double dpiX, dpiY;
            GetDpi(out dpiX, out dpiY);

            WriteableBitmap bitmap = new WriteableBitmap(Canvas.Width, Canvas.Height, dpiX, dpiY, PixelFormats.Bgr32, null);

            try
            {
                bitmap.Lock();

                for (int y = 0; y < Height; y++)
                {
                    IntPtr buffer = bitmap.BackBuffer + y * bitmap.BackBufferStride;
                    for (int x = 0; x < Canvas.Width; x++)
                    {
                        //int val = data[y * width + x];
                        //val %= palettes[currentPalette].colours.Count;
                        //Color c = palettes[currentPalette].colours[val];

                        UInt32 c = Canvas.Get(x, y);

                        //System.Runtime.InteropServices.Marshal.WriteInt32(buffer, GetColour(0, 0, (byte)val));
                        System.Runtime.InteropServices.Marshal.WriteInt32(buffer, (int)c);

                        buffer += 4;
                    }

                }
                bitmap.AddDirtyRect(new Int32Rect(0, 0, Canvas.Width, Canvas.Height));

                image.Source = bitmap;
            }
            finally
            {
                bitmap.Unlock();
            }
        }
    }
}
