using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace RayTrace
{
    /// <summary>
    /// Interaction logic for OutputWindow.xaml
    /// </summary>
    public partial class OutputWindow : Window, INotifyPropertyChanged
    {
        public Canvas Canvas { get; set; }

        public Scene Scene { get; set; }
        public Renderer Renderer { get; set; }

        public WriteableBitmap Bitmap { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private double _blocksToDo = 100;
        public double BlocksToDo { get { return _blocksToDo; } set { _blocksToDo = value; OnPropertyChanged("BlocksToDo"); } }

        private double _blocksDone;

        public double BlocksDone
        {
            get { return _blocksDone; }
            set
            {
                _blocksDone = value;
                OnPropertyChanged("BlocksDone");
            }
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        //private Canvas _canvas = null;
        //private WriteableBitmap _bitmap = null;

        //private Scene _scene;
        //private Renderer _renderer;

        //private int _width = 0;
        //private int _height = 0;

        //private List<RenderBlock> renderBlocks = null;

        public OutputWindow(/*Scene scene, Renderer renderer, int width, int height, List<RenderBlock> renderBlocks*/)
        {
            InitializeComponent();

//            BlocksDone = 10;

            this.DataContext = this;

/*            _scene = scene;
            _renderer = renderer;

            _width = width;
            _height = height;

            this.renderBlocks = renderBlocks;

            DoRender();*/
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
            Update(0, 0, Canvas.Width, Canvas.Height);
        }

        public void Update(int startX, int startY, int numColumns, int numRows)
        {
            try
            {
                Bitmap.Lock();

                for (int y = startY; y < startY + numRows; y++)
                {
                    IntPtr buffer = Bitmap.BackBuffer + y * Bitmap.BackBufferStride;
                    //                  for (int x = 0; x < canvas.Width; x++)
                    buffer += 4 * startX;
                    for (int x = startX; x < startX + numColumns; x++)
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
                Bitmap.AddDirtyRect(new Int32Rect(0, 0, Canvas.Width, Canvas.Height));

                //image.Source = bitmap;
                image.Source = Bitmap;

                BlocksDone++;
            }
            finally
            {
                Bitmap.Unlock();
            }
        }

        public CancellationTokenSource TokenSource { get; set; }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            TokenSource.Cancel();
        }

        // Don't actually close the window, just hide it.
        private void outputWindow_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;

            Visibility = Visibility.Hidden;
        }


        /*
                private async Task<bool> DoRender()
                {
                    _canvas = new Canvas(_width, _height);

                    GetDpi(out double dpiX, out double dpiY);

                    _bitmap = new WriteableBitmap(_canvas.Width, _canvas.Height, dpiX, dpiY, PixelFormats.Bgr32, null);

                    _scene.Camera.UpdateMatrix();    // XXX Needed?
                    _scene.CreateCaches();

                    ThreadPool.GetMaxThreads(out int workerThreads, out int completonPortThreads);

                    ThreadPool.SetMaxThreads(16, completonPortThreads);


                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();

                    List<Task> tasks = new List<Task>();

                    int i = 0;
                    foreach (RenderBlock rb in renderBlocks)
                    {
                        int idx = i;
                        Task renderTask = Task.Factory.StartNew(() =>
                        {
                            Thread.CurrentThread.Name = $"Task {idx}, lines {rb.StartY} to {rb.StartY + rb.Height}...";
                            Console.WriteLine($"Starting Task {idx}, lines {rb.StartY} to {rb.StartY + rb.Height}...");
                            _renderer.Render(_scene, _canvas, rb.StartX, rb.StartY, rb.Width, rb.Height);
                            Console.WriteLine($"Done Task {idx}...");

                            this.Dispatcher.Invoke(DispatcherPriority.Normal,
                                                (ThreadStart)delegate ()
                                                {
                                                    //Console.WriteLine($"Updating {idx}, lines {rb.StartY} to {rb.StartY + rb.Height}...");
                                                    Update(rb.StartX, rb.StartY, rb.Width, rb.Height);
                                                }
                            );
                        });
                        tasks.Add(renderTask);
                        i++;
                    }

                    //List<Task> Tasks = new List<Task>() { renderTask1, renderTask2 };
                    //await Task.WaitAll(Tasks.ToArray());
                    try
                    {
                        foreach (Task task in tasks)
                        {
                            await task;
                        }
                    }
                    catch (Exception e)
                    {
                        var m = e.Message;
                    }

                    return true;
                }


                private  void Update()
                {
                    Update(0, 0, _canvas.Width, _canvas.Height);
                }

                private void Update(int startX, int startY, int numColumns, int numRows)
                {
                    try
                    {
                        _bitmap.Lock();

                        for (int y = startY; y < startY + numRows; y++)
                        {
                            IntPtr buffer = _bitmap.BackBuffer + y * _bitmap.BackBufferStride;
                            //                  for (int x = 0; x < canvas.Width; x++)
                            buffer += 4 * startX;
                            for (int x = startX; x < startX + numColumns; x++)
                            {
                                //int val = data[y * width + x];
                                //val %= palettes[currentPalette].colours.Count;
                                //Color c = palettes[currentPalette].colours[val];

                                UInt32 c = _canvas.Get(x, y);

                                //System.Runtime.InteropServices.Marshal.WriteInt32(buffer, GetColour(0, 0, (byte)val));
                                System.Runtime.InteropServices.Marshal.WriteInt32(buffer, (int)c);

                                buffer += 4;
                            }

                        }
                        _bitmap.AddDirtyRect(new Int32Rect(0, 0, _canvas.Width, _canvas.Height));

                        image.Source = _bitmap;
                    }
                    finally
                    {
                        _bitmap.Unlock();
                    }
                }
        */


        /*

                public void Update()
                {
                    double dpiX, dpiY;
                    GetDpi(out dpiX, out dpiY);

                    WriteableBitmap bitmap = new WriteableBitmap(_canvas.Width, _canvas.Height, dpiX, dpiY, PixelFormats.Bgr32, null);

                    try
                    {
                        bitmap.Lock();

                        for (int y = 0; y < Height; y++)
                        {
                            IntPtr buffer = bitmap.BackBuffer + y * bitmap.BackBufferStride;
                            for (int x = 0; x < _canvas.Width; x++)
                            {
                                //int val = data[y * width + x];
                                //val %= palettes[currentPalette].colours.Count;
                                //Color c = palettes[currentPalette].colours[val];

                                UInt32 c = _canvas.Get(x, y);

                                //System.Runtime.InteropServices.Marshal.WriteInt32(buffer, GetColour(0, 0, (byte)val));
                                System.Runtime.InteropServices.Marshal.WriteInt32(buffer, (int)c);

                                buffer += 4;
                            }

                        }
                        bitmap.AddDirtyRect(new Int32Rect(0, 0, _canvas.Width, _canvas.Height));

                        image.Source = bitmap;
                    }
                    finally
                    {
                        bitmap.Unlock();
                    }
                }
        */
    }
}
