using System;
using System.Collections.Concurrent;
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
        private ConcurrentDictionary<int, RenderBlock> _blocksInProgress = new();
        private ConcurrentQueue<RenderBlock> _completedBlocks = new();

        public Canvas OutputCanvas { get; set; }

        public Scene Scene { get; set; }
        public Renderer Renderer { get; set; }

        public WriteableBitmap Bitmap { get; set; }

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

        public enum RenderStatus
        {
            NotStarted,
            Complete,
            Cancelled,
            Error
        }

        private void CopyAnyBlocks()
        {
            // Update any blocks that are currently being renderered.
            foreach (var kvp in _blocksInProgress)
            {
                RenderBlock block = kvp.Value;
                Update(block.StartX, block.StartY, block.Width, block.Height);
            }

            // Do a final update of any blocks which have been completed.
            while (_completedBlocks.TryDequeue(out RenderBlock block))
            {
                Update(block.StartX, block.StartY, block.Width, block.Height);
            }
        }

        public OutputWindow()
        {
            InitializeComponent();

            DataContext = this;

            CompositionTarget.Rendering += (s, e) =>
            {
                // This event fires ~60 times per second.
                CopyAnyBlocks();
                
                //Update(0, 0, OutputCanvas.Width, OutputCanvas.Height);
            };

            //SizeToContent = SizeToContent.WidthAndHeight;
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
            OutputCanvas = new(width, height);

            GetDpi(out double dpiX, out double dpiY);

            Bitmap = new WriteableBitmap(OutputCanvas.Width, OutputCanvas.Height, dpiX, dpiY, PixelFormats.Bgr32, null);
        }

        public void Update()
        {
            Update(0, 0, OutputCanvas.Width, OutputCanvas.Height);
        }

        // Copy block from OutputCanvas to Bitmap and update image.
        public unsafe void Update(int startX, int startY, int numColumns, int numRows)
        {
            Bitmap.Lock();

            try
            {
                for (int y = startY; y < startY + numRows; y++)
                {
                    IntPtr buffer = Bitmap.BackBuffer + y * Bitmap.BackBufferStride;
                    buffer += 4 * startX;

                    fixed (uint* srcPtr = OutputCanvas.buffer)
                    {
                        int offset = OutputCanvas.GetOffset(startX, y);
                        Buffer.MemoryCopy((void*)(srcPtr + offset), (void*)buffer, numColumns*4, numColumns*4);
                    }
/*
                    for (int x = startX; x < startX + numColumns; x++)
                    {
                        UInt32 c = OutputCanvas.Get(x, y);

                        System.Runtime.InteropServices.Marshal.WriteInt32(buffer, (int)c);

                        buffer += 4;
                    }
*/
                }

//                Bitmap.AddDirtyRect(new Int32Rect(0, 0, OutputCanvas.Width, OutputCanvas.Height));
                Bitmap.AddDirtyRect(new Int32Rect(startX, startY, numColumns, numRows));

                image.Source = Bitmap;

                BlocksDone++;
            }
            finally
            {
                Bitmap.Unlock();
            }
        }

        // Render a single pixel from the current Canvas.
        private void RenderPoint(int x, int y)
        {
            if (OutputCanvas != null)
            {
                x = Math.Min(x, OutputCanvas.Width - 1);
                y = Math.Min(y, OutputCanvas.Height - 1);

                x = Math.Max(x, 0);
                y = Math.Max(y, 0);

                Renderer.Render(Scene, OutputCanvas, x, y, 1, 1, CancellationToken.None);
            }
        }

        public CancellationTokenSource TokenSource { get; set; }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
//            TokenSource.Cancel();
            tokenSource.Cancel();
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

            var x = Bitmap.PixelWidth * pos.X / image.ActualWidth;
            var y = Bitmap.PixelHeight * pos.Y / image.ActualHeight;

            RenderPoint((int)x, (int)y);
        }

        private List<RenderBlock> CalcRenderBlocks(int NumRenderBlocks, MainWindow.RenderDirection direction)
        {
            int width = OutputCanvas.Width;
            int height = OutputCanvas.Height;

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

        public async Task<RenderStatus> DoRender(int NumRenderBlocks, MainWindow.RenderDirection direction)
        {
            List<RenderBlock> renderBlocks = CalcRenderBlocks(NumRenderBlocks, direction);

            BlocksDone = 0;
            BlocksToDo = renderBlocks.Count;

            //canvas = new Canvas(canvas.Width, canvas.Height);

            //GetDpi(out double dpiX, out double dpiY);
            //_bitmap = new WriteableBitmap(_canvas.Width, _canvas.Height, dpiX, dpiY, PixelFormats.Bgr32, null);

            OutputCanvas.Clear();
            _completedBlocks.Enqueue(new RenderBlock() { StartX=0, StartY=0, Width=OutputCanvas.Width, Height=OutputCanvas.Height } );

            Scene.Camera.UpdateMatrix();    // XXX Needed?

            Debug.WriteLine("Precalculating...");
            Scene.CreateCaches();
            Debug.WriteLine("...done");

            ThreadPool.GetMaxThreads(out int workerThreads, out int completonPortThreads);

            _ = ThreadPool.SetMaxThreads(16, completonPortThreads);

            tokenSource = new();
            CancellationToken token = tokenSource.Token;

            Renderer.NumRays = 0;

            //OutputCanvas.Background = 0xff00ff00;
            //OutputCanvas.Clear();

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            List<Task> tasks = new();

            int i = 0;

            foreach (RenderBlock rb in renderBlocks)
            {
                int idx = i;

                Task renderTask = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        if (token.IsCancellationRequested) return;

                        Thread.CurrentThread.Name = $"Task {idx}, lines {rb.StartY} to {rb.StartY + rb.Height}...";
                        Debug.WriteLine($"Starting Task {idx}, lines {rb.StartY} to {rb.StartY + rb.Height}...");

                        OutputCanvas.DrawRectangle(rb.StartX, rb.StartY, rb.Width, rb.Height, SPalette.Orange);
                        //_completedBlocks.Enqueue(rb);
                        _blocksInProgress[idx] = rb;

                        Renderer.Render(Scene, OutputCanvas, rb.StartX, rb.StartY, rb.Width, rb.Height, token);
                        Debug.WriteLine($"Done Task {idx}...");

                        _blocksInProgress.TryRemove(idx, out RenderBlock removed);

                        _completedBlocks.Enqueue(rb);
                        //_completedBlocks.Enqueue(new RenderBlock() { StartX=0, StartY=0, Width=OutputCanvas.Width, Height=OutputCanvas.Height });
                        //_completedBlocks.Enqueue(new RenderBlock() { StartX=x, StartY=y, Width=width, Height=height });
                    }
                    catch (Exception e)
                    {
                        Logger.Instance.Warn($"{e.Message}");
                        throw;
                    }
                });
                tasks.Add(renderTask);
                i++;
            }

            RenderStatus status = RenderStatus.NotStarted;

            //List<Task> Tasks = new List<Task>() { renderTask1, renderTask2 };
            //await Task.WaitAll(Tasks.ToArray());
            try
            {
                foreach (Task task in tasks)
                {
                    await task;
                }

                status = RenderStatus.Complete;

                _completedBlocks.Enqueue(new RenderBlock() { StartX=0, StartY=0, Width=OutputCanvas.Width, Height=OutputCanvas.Height });

            }
            catch (Exception e)
            {
                string m = e.Message;

                if (e is OperationCanceledException)
                {
                    outputWindow.BlocksDone = 0;
                    outputWindow.BlocksToDo = 0;

                    status = RenderStatus.Cancelled;
                }
                else
                {
                    status = RenderStatus.Error;
                }
            }

            stopwatch.Stop();

/*
            TimeSpan ts = stopwatch.Elapsed;

            // Format and display the TimeSpan value.
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            Trace.WriteLine("Render time: " + elapsedTime);

            Trace.WriteLine("Number rays: " + Renderer.NumRays);
            Trace.WriteLine("Rays per second: " + 1000.0*Renderer.NumRays/stopwatch.ElapsedMilliseconds);
*/

            _completedBlocks.Clear();
            _blocksInProgress.Clear();

            return status;
        }
    }
}
