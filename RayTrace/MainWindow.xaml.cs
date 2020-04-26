using System;
using System.Collections;
using System.Collections.Generic;
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

        private readonly List<Renderer.AntiAliasType> antiAlias = new List<Renderer.AntiAliasType>() { Renderer.AntiAliasType.None, Renderer.AntiAliasType.SuperSampling, Renderer.AntiAliasType.AdaptiveSupersampling };
        public List<Renderer.AntiAliasType> AntiAliasType { get { return antiAlias; } }

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
            checkBoxPhong.IsChecked = renderer.UsePhong;
            checkBoxReflections.IsChecked = renderer.UseReflections;
            checkBoxRefraction.IsChecked = renderer.UseRefraction;

            textBoxDepth.Text = renderer.RecursionDepth.ToString();
            textBoxAdaptiveSubSamplingDepth.Text = renderer.AdaptiveSuperSamplingDepth.ToString();
            textBoxAdaptiveSubSamplingDepth.IsEnabled = false;

            //output.Hide();

            //scene = SceneFactory.PhongDemoScene();
            //scene = SceneFactory.TexturedSpheresDemoScene();
            //scene = SceneFactory.RefractionDemoScene();
            scene = SceneFactory.BoxDemoScene();

            comboBoxFormat.SelectedItem = ImageFormat.Tiff;

            //renderDirections.ItemsSource = renderDirection;
            comboBoxDirection.SelectedItem = RenderDirection.CentreOut;
            comboBoxAntiAlias.SelectedItem = Renderer.AntiAliasType.None;

            TreeViewItem item = new TreeViewItem();
            item.Header = "Lights";
            treeView.Items.Add(item);
            foreach (var light in scene.Lights)
            {
                item.Items.Add(new TreeViewItem() { Header=light.ToString() });
            }

            item = new TreeViewItem() { Header = "Shapes" };
            treeView.Items.Add(item);
            foreach (var shape in scene.Shapes)
            {
                item.Items.Add(new TreeViewItem() { Header = shape.ToString() });
            }
        }

        private async Task<bool> Render()
        {
            renderer.UseAmbient = (bool)checkBoxAmbient.IsChecked;
            renderer.UseDiffuse = (bool)checkBoxLighting.IsChecked;
            renderer.UsePhong = (bool)checkBoxPhong.IsChecked;
            renderer.UseReflections = (bool)checkBoxReflections.IsChecked;
            renderer.UseRefraction = (bool)checkBoxRefraction.IsChecked;

            int depth;
            if (!Int32.TryParse(textBoxDepth.Text, out depth))
            {
                MessageBox.Show("Recursion depth must be an integer.");
                return false;
            }

            renderer.RecursionDepth = depth;

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

            double dpiX, dpiY;
            GetDpi(out dpiX, out dpiY);

            bitmap = new WriteableBitmap(canvas.Width, canvas.Height, dpiX, dpiY, PixelFormats.Bgr32, null);

            buttonRender.IsEnabled = false;

            int step = height / NumRenderBlocks;

            // Make list of blocks to redner.
            List<RenderBlock> renderBlocks = new List<RenderBlock>();

            //const int NumBlocks = 8;

            for (int y = 0; y < NumRenderBlocks; y++)
            {
                for (int x = 0; x < NumRenderBlocks; x++)
                {
                    RenderBlock renderBlock = new RenderBlock() { StartX = x * step, StartY = y * step, Width = step, Height = step };
                    renderBlocks.Add(renderBlock);
                }
            }

            switch (direction)
            {
                case RenderDirection.CentreOut: renderBlocks.Sort(new TopToBottomComparer(canvas.Width / 2, canvas.Height / 2));  break;
                case RenderDirection.Diagonal:  renderBlocks.Sort(new DiagonalComparer());  break;
                case RenderDirection.TopToBottom:  /* The default */ break;
            }


            List<Task> tasks = new List<Task>();

            //for (int i = 0; i < height / step; i++)
            int i = 0;
            foreach (RenderBlock rb in renderBlocks)
            {
                int idx = i;
                Task renderTask = Task.Factory.StartNew(() =>
                {
                    //int idx = i;
                    Console.WriteLine($"Starting Task {idx}, lines {rb.StartY} to {rb.StartY + rb.Height}...");
                    renderer.Render(scene, canvas, rb.StartX, rb.StartY, rb.Width, rb.Height);
                    Console.WriteLine($"Done Task {idx}...");

                    this.Dispatcher.Invoke(DispatcherPriority.Normal,
                                        (ThreadStart)delegate ()
                                        {
                                            Console.WriteLine($"Updating {idx}, lines {rb.StartY} to {rb.StartY + rb.Height}...");
                                            Update(rb.StartX, rb.StartY, rb.Width, rb.Height);
                                        }
                    );
                });
                tasks.Add(renderTask);
                i++;
            }
            
            //List<Task> Tasks = new List<Task>() { renderTask1, renderTask2 };
            //await Task.WaitAll(Tasks.ToArray());

            foreach (Task task in tasks)
            {
                await task;
            }

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
            checkBoxPhong.IsEnabled = (bool) checkBoxLighting.IsChecked;
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
    }
}
