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
    public struct RenderBlock
    {
        public int StartX;
        public int StartY;
        public int Width;
        public int Height;
        public int CentreX { get { return StartX + Width / 2; } }
        public int CentreY { get { return StartY + Height / 2; } }
    }

    public class TopToBottomComparer : IComparer<RenderBlock>
    {
        private readonly int centreX;
        private readonly int centreY;

        public TopToBottomComparer(int _centreX, int _centreY) { centreX = _centreX; centreY = _centreY; }

        public int Compare(RenderBlock rb1, RenderBlock rb2)
        {
            float distSquared1 = (centreX - rb1.CentreX) * (centreX - rb1.CentreX) + (centreY - rb1.CentreY) * (centreY - rb1.CentreY);
            float distSquared2 = (centreX - rb2.CentreX) * (centreX - rb2.CentreX) + (centreY - rb2.CentreY) * (centreY - rb2.CentreY);
            if (distSquared1 > distSquared2) return 1;
            if (distSquared1 < distSquared2) return -1;
            return 0;
        }
    }

    public class DiagonalComparer : IComparer<RenderBlock>
    {
        public int Compare(RenderBlock rb1, RenderBlock rb2)
        {
            float distSquared1 = rb1.CentreX * rb1.CentreX + rb1.CentreY * rb1.CentreY;
            float distSquared2 = rb2.CentreX * rb2.CentreX + rb2.CentreY * rb2.CentreY;
            if (distSquared1 > distSquared2) return 1;
            if (distSquared1 < distSquared2) return -1;
            return 0;
        }
    }


    /// <summary>
    /// Interaction logic for OutputWindow.xaml
    /// </summary>
    public partial class OutputWindow : Window, INotifyPropertyChanged
    {
        public Canvas canvas { get; set; }

        public Scene Scene { get; set; }
        public Renderer Renderer { get; set; }

        public WriteableBitmap bitmap { get; set; }

        public CancellationTokenSource tokenSource = new();

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

        public void SetOutputSize(int width, int height)
        {
            canvas = new(width, height);

            GetDpi(out double dpiX, out double dpiY);

            bitmap = new WriteableBitmap(canvas.Width, canvas.Height, dpiX, dpiY, PixelFormats.Bgr32, null);

            image.MinWidth = width;
            image.MinHeight = height;
        }

        public void Update()
        {
            Update(0, 0, canvas.Width, canvas.Height);
        }

        public void Update(int startX, int startY, int numColumns, int numRows)
        {
            try
            {
                bitmap.Lock();

                for (int y = startY; y < startY + numRows; y++)
                {
                    IntPtr buffer = bitmap.BackBuffer + y * bitmap.BackBufferStride;
                    //                  for (int x = 0; x < canvas.Width; x++)
                    buffer += 4 * startX;
                    for (int x = startX; x < startX + numColumns; x++)
                    {
                        //int val = data[y * width + x];
                        //val %= palettes[currentPalette].colours.Count;
                        //Color c = palettes[currentPalette].colours[val];

                        UInt32 c = canvas.Get(x, y);

                        //System.Runtime.InteropServices.Marshal.WriteInt32(buffer, GetColour(0, 0, (byte)val));
                        System.Runtime.InteropServices.Marshal.WriteInt32(buffer, (int)c);

                        buffer += 4;
                    }

                }
                bitmap.AddDirtyRect(new Int32Rect(0, 0, canvas.Width, canvas.Height));

                //image.Source = bitmap;
                image.Source = bitmap;

                BlocksDone++;
            }
            finally
            {
                bitmap.Unlock();
            }
        }

        // Render a single pixel from the current Canvas.
        private void RenderPoint(int x, int y)
        {
            if (canvas != null)
            {
                x = Math.Min(x, canvas.Width - 1);
                y = Math.Min(y, canvas.Height - 1);

                x = Math.Max(x, 0);
                y = Math.Max(y, 0);

                Renderer.Render(Scene, canvas, x, y, 1, 1);
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

        private void image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(image /*(IInputElement) sender*/);

            var x = bitmap.PixelWidth * pos.X / image.ActualWidth;
            var y = bitmap.PixelHeight * pos.Y / image.ActualHeight;

            RenderPoint((int)x, (int)y);
        }

        private List<RenderBlock> CalcRenderBlocks(int NumRenderBlocks, MainWindow.RenderDirection direction)
        {
            int width = canvas.Width;
            int height = canvas.Height;

            float floatStep = ((float)height) / NumRenderBlocks;
            int intStep = (int)floatStep;

            // Make list of blocks to render.
            List<RenderBlock> renderBlocks = new List<RenderBlock>();

            int intY = 0;
            float floatY = 0.0f;
            for (int y = 0; y < NumRenderBlocks; y++)
            {
                int intX = 0;
                float floatX = 0.0f;

                int blockHeight = intStep;
                if (floatY > intY) blockHeight++;

                for (int x = 0; x < NumRenderBlocks; x++)
                {
                    int blockWidth = intStep;
                    if (floatX > intX) blockWidth++;

                    RenderBlock renderBlock = new RenderBlock() { StartX = intX, StartY = intY, Width = blockWidth, Height = blockHeight };
                    renderBlocks.Add(renderBlock);

                    intX += blockWidth;
                    floatX += floatStep;
                }

                intY += blockHeight;
                floatY += floatStep;
            }

            switch (direction)
            {
                case MainWindow.RenderDirection.CentreOut: renderBlocks.Sort(new TopToBottomComparer(width / 2, height / 2)); break;
                case MainWindow.RenderDirection.Diagonal: renderBlocks.Sort(new DiagonalComparer()); break;
                case MainWindow.RenderDirection.TopToBottom:  /* The default */ break;
            }

            return renderBlocks;
        }

        public async Task<bool> DoRender(int NumRenderBlocks, MainWindow.RenderDirection direction)
        {
            var renderBlocks = CalcRenderBlocks(NumRenderBlocks, direction);

            BlocksDone = 0;
            BlocksToDo = renderBlocks.Count;

            //canvas = new Canvas(canvas.Width, canvas.Height);

            GetDpi(out double dpiX, out double dpiY);

            //_bitmap = new WriteableBitmap(_canvas.Width, _canvas.Height, dpiX, dpiY, PixelFormats.Bgr32, null);

            canvas.Clear();

            Scene.Camera.UpdateMatrix();    // XXX Needed?
            Scene.CreateCaches();

            ThreadPool.GetMaxThreads(out int workerThreads, out int completonPortThreads);

            ThreadPool.SetMaxThreads(16, completonPortThreads);

            CancellationToken token = tokenSource.Token;

            //Stopwatch stopWatch = new Stopwatch();
            //stopWatch.Start();

            List<Task> tasks = new List<Task>();

            int i = 0;
            foreach (RenderBlock rb in renderBlocks)
            {
                int idx = i;
                Task renderTask = Task.Factory.StartNew(() =>
                {
                    if (token.IsCancellationRequested) return;

                    Thread.CurrentThread.Name = $"Task {idx}, lines {rb.StartY} to {rb.StartY + rb.Height}...";
                    Console.WriteLine($"Starting Task {idx}, lines {rb.StartY} to {rb.StartY + rb.Height}...");
                    Renderer.Render(Scene, canvas, rb.StartX, rb.StartY, rb.Width, rb.Height);
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

                if (e is TaskCanceledException)
                {
                    outputWindow.BlocksDone = 0;
                    outputWindow.BlocksToDo = 0;
                }

            }

            return true;
        }
    }
}
