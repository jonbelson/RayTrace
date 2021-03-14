using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        private Scene scene = new Scene();
        private Renderer renderer = new Renderer();
   
        private Canvas canvas = null;
        WriteableBitmap bitmap = null;

        public int NumRenderBlocks { get; set; } = 8;

        public enum RenderDirection
        {
            TopToBottom, Diagonal, CentreOut
        }

        private RenderDirection[] renderDirection = { RenderDirection.TopToBottom, RenderDirection.Diagonal, RenderDirection.CentreOut };
        //public List<RenderDirection> renderDirecton = new List<RenderDirection>() { RenderDirection.TopToBottom, RenderDirection.Diagonal, RenderDirection.CentreOut };
        public RenderDirection[] RenderDirections { get { return renderDirection; } }

        private readonly List<Renderer.AntiAliasType> antiAlias = new List<Renderer.AntiAliasType>() { Renderer.AntiAliasType.None, Renderer.AntiAliasType.NoiseGrid, Renderer.AntiAliasType.SuperSampling, Renderer.AntiAliasType.AdaptiveSupersampling };
        public List<Renderer.AntiAliasType> AntiAliasType { get { return antiAlias; } }

//        public List<Entity> Entities { get; set; }
        public ObservableCollection<Entity> Entities { get; set; } = new ObservableCollection<Entity>();

        private readonly List<ImageFormat> saveFormats = new List<ImageFormat>() { ImageFormat.Tiff, ImageFormat.Jpeg, ImageFormat.Bmp, ImageFormat.Png };
        public List<ImageFormat> SaveFormats { get { return saveFormats; } }

        public ImageFormat SelectedFormat { get; set; }

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

                image.Source = bitmap;
            }
            finally
            {
                bitmap.Unlock();
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            checkBoxAmbient.IsChecked = renderer.UseAmbient;
            checkBoxLighting.IsChecked = renderer.UseDiffuse;
            checkBoxSpecular.IsChecked = renderer.UseSpecular;
            checkBoxRefraction.IsChecked = renderer.UseRefraction;
            checkBoxShadows.IsChecked = renderer.UseShadows;
            checkBoxSoftShadows.IsChecked = renderer.UseSoftShadows;
            checkBoxReflections.IsChecked = renderer.UseReflections;
            checkBoxGIEnable.IsChecked = renderer.UseGlobalIllumination;
            checkBoxDepthOfField.IsChecked = renderer.UseDepthOfField;

            textBoxDepth.Text = renderer.RecursionDepth.ToString();
            textBoxAdaptiveSubSamplingDepth.Text = renderer.AdaptiveSuperSamplingDepth.ToString();
            textBoxAdaptiveSubSamplingDepth.IsEnabled = false;

            textBoxGINumRays.Text = renderer.GINumRays.ToString();
            textBoxGIDepth.Text = renderer.GIDepth.ToString();

            //scene = SceneFactory.PhongDemoScene();
            //scene = SceneFactory.TexturedSpheresDemoScene();
            //scene = SceneFactory.RefractionDemoScene();
            //scene = SceneFactory.BoxDemoScene();
            //scene = SceneFactory.SphereDemoScene();
            //scene = SceneFactory.SoftLightDemoScene();
            //scene = SceneFactory.FovDemoScene();
            //scene = SceneFactory.ModelScene();
            scene = SceneFactory.ObjScene();
            //scene = SceneFactory.GIDemoScene();
            //scene = SceneFactory.GIDemoScene2();

            comboBoxFormat.SelectedItem = ImageFormat.Tiff;

            //renderDirections.ItemsSource = renderDirection;
            comboBoxDirection.SelectedItem = RenderDirection.CentreOut;
            comboBoxAntiAlias.SelectedItem = Renderer.AntiAliasType.None;

            //Entities = new List<Entity>();
            //Entities = new ObservableCollection<Entity>();
            //listView.ItemsSource = null;
            Entities.Add(scene.Camera);
            foreach (var light in scene.Lights)
            {
                Entities.Add(light);
            }
            foreach (var shape in scene.Shapes)
            {
                Entities.Add(shape);
            }

            //listView.ItemsSource = Entities;
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

                renderer.Render(scene, canvas, x, y, 1, 1);
            }
        }

        private async Task<bool> Render()
        {
            renderer.UseAmbient = (bool)checkBoxAmbient.IsChecked;
            renderer.UseDiffuse = (bool)checkBoxLighting.IsChecked;
            renderer.UseSpecular = (bool)checkBoxSpecular.IsChecked;
            renderer.UseRefraction = (bool)checkBoxRefraction.IsChecked;
            renderer.UseShadows = (bool)checkBoxShadows.IsChecked;
            renderer.UseSoftShadows = (bool)checkBoxSoftShadows.IsChecked;
            renderer.UseReflections = (bool)checkBoxReflections.IsChecked;
            renderer.UseGlobalIllumination = (bool)checkBoxGIEnable.IsChecked;
            renderer.UseDepthOfField = (bool)checkBoxDepthOfField.IsChecked;

            int depth;
            if (!Int32.TryParse(textBoxDepth.Text, out depth))
            {
                MessageBox.Show("Recursion depth must be an integer.");
                return false;
            }

            renderer.RecursionDepth = depth;

            if (!Int32.TryParse(textBoxGIDepth.Text, out int giDepth))
            {
                MessageBox.Show("Recursion depth must be an integer.");
                return false;
            }

            renderer.GIDepth = giDepth;

            if (!Int32.TryParse(textBoxGINumRays.Text, out int giNumRays))
            {
                MessageBox.Show("Number of GI rays must be an integer.");
                return false;
            }

            renderer.GINumRays = giNumRays;

            Renderer.AntiAliasType antiAlias = (Renderer.AntiAliasType)comboBoxAntiAlias.SelectedItem;
            renderer.AntiAlias = antiAlias;

            int adaptiveSuperSamplingDepth;
            if (!Int32.TryParse(textBoxAdaptiveSubSamplingDepth.Text, out adaptiveSuperSamplingDepth))
            {
                MessageBox.Show("Recursion depth must be an integer.");
                return false;
            }

            if (adaptiveSuperSamplingDepth > 10)
            {
                MessageBox.Show("Adaptive Super-Sampling depth must be 10 or less.");
                return false;
            }

            renderer.AdaptiveSuperSamplingDepth = adaptiveSuperSamplingDepth;

            int width = 800;
            if (!Int32.TryParse(textWidth.Text, out width))
            {
                MessageBox.Show("Invalid image width.");
                return false;
            }

            int numRenderBlocks = NumRenderBlocks;
            if (!Int32.TryParse(textNumberBlocks.Text, out numRenderBlocks))
            {
                MessageBox.Show("Invalid number of render blocks.");
                return false;
            }

            NumRenderBlocks = numRenderBlocks;

            RenderDirection direction = (RenderDirection)comboBoxDirection.SelectedItem;

            int height = width;

            canvas = new Canvas(width, height);

            GetDpi(out double dpiX, out double dpiY);

            bitmap = new WriteableBitmap(canvas.Width, canvas.Height, dpiX, dpiY, PixelFormats.Bgr32, null);

            buttonRender.IsEnabled = false;

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
                case RenderDirection.CentreOut: renderBlocks.Sort(new TopToBottomComparer(canvas.Width / 2, canvas.Height / 2));  break;
                case RenderDirection.Diagonal:  renderBlocks.Sort(new DiagonalComparer());  break;
                case RenderDirection.TopToBottom:  /* The default */ break;
            }

            scene.Camera.UpdateMatrix();    // XXX Needed?
            scene.CreateCaches();

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
                    renderer.Render(scene, canvas, rb.StartX, rb.StartY, rb.Width, rb.Height);
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

            stopWatch.Stop();
            // Get the elapsed time as a TimeSpan value.
            TimeSpan ts = stopWatch.Elapsed;

            // Format and display the TimeSpan value.
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            Console.WriteLine("REader time: " + elapsedTime);

            buttonRender.IsEnabled = true;

            bool success = true;
            if (success)
            {
                Update();

                if ((bool)checkBoxSaveImage.IsChecked)
                {
                    ImageFormat format = (ImageFormat)comboBoxFormat.SelectedItem;
                    string fileName = textFileName.Text;
                    if (!String.IsNullOrEmpty(fileName))
                    {
                        canvas.Save(fileName, format);
                    }
                }
            }

            return true;
        }

        private void buttonRender_Click(object sender, RoutedEventArgs e)
        {
            Render();

            //bool success = await Render();

            //buttonRender.IsEnabled = false;

            //Task renderTask = Task.Factory.StartNew(() =>
            //{
             //   Render();
            //});

            //await renderTask;

            //buttonRender.IsEnabled = true;


        }

        private void checkBoxLighting_Click(object sender, RoutedEventArgs e)
        {
            checkBoxSpecular.IsEnabled = (bool) checkBoxLighting.IsChecked;
        }

        private void checkBoxReflections_Click(object sender, RoutedEventArgs e)
        {
            //labelDepth.IsEnabled = (bool)checkBoxReflections.IsChecked;
            textBoxDepth.IsEnabled = (bool)checkBoxReflections.IsChecked;
        }

        private void checkBoxSaveImage_Click(object sender, RoutedEventArgs e)
        {
            //textFileName
        }

        private void textWidth_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (textHeight != null)
            {
                textHeight.Text = textWidth.Text;
            }
        }

        private void comboBoxAntiAlias_SelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Renderer.AntiAliasType antiAlias = (Renderer.AntiAliasType)comboBoxAntiAlias.SelectedItem;
            textBoxAdaptiveSubSamplingDepth.IsEnabled = antiAlias == Renderer.AntiAliasType.AdaptiveSupersampling;
        }

        private void image_MouseMove(object sender, MouseEventArgs e)
        {
            //sender;
            var pos = e.GetPosition(image /*(IInputElement) sender*/);

            var x = bitmap.PixelWidth * pos.X / image.ActualWidth;
            var y = bitmap.PixelHeight* pos.Y / image.ActualHeight;

            //Console.WriteLine(x + ", " + y);
        }

        private void image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(image /*(IInputElement) sender*/);

            var x = bitmap.PixelWidth * pos.X / image.ActualWidth;
            var y = bitmap.PixelHeight * pos.Y / image.ActualHeight;

            RenderPoint((int) x, (int) y);
        }

        private void LoadScene_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog() { Filter = "XML Files | *.xml", FileName = "Scene.xml" };
            if (dialog.ShowDialog() == true)
            {
                string fileName = dialog.FileName;
                XMLScene xmlScene = Exporter.Import(fileName);
                if (xmlScene != null)
                {
                    scene = new Scene(xmlScene);
                    MessageBox.Show("Scene loaded successfully.");
                }
            }
        }

        private void SaveScene_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog { Filter = "XML Files | *.xml", FileName = "Scene.xml" };
            if (dialog.ShowDialog() == true)
            {
                string fileName = dialog.FileName;
                XMLScene xmlScene = scene.ToXML();
                if (!Exporter.Export(xmlScene, fileName))
                {
                    MessageBox.Show("Failed to save Scene");
                }
            }
        }
    }
}
