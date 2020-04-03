using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace RayTrace
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Scene scene = new Scene();
        private Renderer renderer = new Renderer();
   
        private Canvas canvas = null;
        WriteableBitmap bitmap = null;

        private readonly List<ImageFormat> saveFormats = new List<ImageFormat>() { ImageFormat.Tiff, ImageFormat.Jpeg, ImageFormat.Bmp, ImageFormat.Png };

        public ImageFormat SelectedFormat { get; set; }

        private void PhongDemoScene()
        {
            for (int j = 0; j < 5; j++)
            {
                for (int i = 0; i <= 5; i++)
                {
                    Sphere sphere = new Sphere(new Point(-50 + i * 20, -40 + j * 20, 80), 8, Palette.Green);
                    sphere.Material.k = i / 5.0;
                    sphere.Material.ns = (j + 1) * (j + 1);
                    scene.AddShape(sphere);
                }
            }
            Light light = new Light(new Point(-50, 0, 0), 0.5f, new Colour(1.0f, 1.0f, 1.0f));
            scene.AddLight(light);
        }

        private void TexturedSpheresDemoScene()
        {
            Sphere sphere = new Sphere(new Point(-20, 0, 50), 20, Palette.White);
            sphere.Material.Texture = new TextureSquares();
           scene.AddShape(sphere);

            sphere = new Sphere(new Point(20, 0, 50), 20, Palette.White);
            sphere.Material.Texture = new TextureSquares();
            sphere.Material.Texture = new TextureNoise(Palette.White, Palette.Black);
            sphere.Material.Texture = new TextureNoise(Palette.Red, Palette.Green) { Function=TextureNoise.ColourFunction.Blend, Scale=5 };
            scene.AddShape(sphere);

              Plane plane = new Plane(new Vector(0.0, 1.0, 0.0), new Point (0.0, -30.0, 0.0));
            plane.Material.Texture = new TextureSolid(Palette.White);
            //plane.Material.Texture = new TextureSquares() { Scale = 4 };
            scene.AddShape(plane);

            Light light = new Light(new Point(-50, 0, 0), 0.5f, new Colour(1.0f, 0.0f, 0.0f));
            scene.AddLight(light);
            light = new Light(new Point(50, 50, 0), .85f, new Colour(1.0f, 1.0f, 1.0f));
            scene.AddLight(light);
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
            Update(0, canvas.Height);
        }

        public void Update(int startY, int numLines)
        {
            //double dpiX, dpiY;
            //GetDpi(out dpiX, out dpiY);
            //WriteableBitmap bitmap = new WriteableBitmap(canvas.Width, canvas.Height, dpiX, dpiY, PixelFormats.Bgr32, null);

            try
            {
                bitmap.Lock();

                for (int y = startY; y < startY + numLines; y++)
                {
                    IntPtr buffer = bitmap.BackBuffer + y * bitmap.BackBufferStride;
                    for (int x = 0; x < canvas.Width; x++)
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

            checkBoxLighting.IsChecked = renderer.UseLighting;
            checkBoxPhong.IsChecked = renderer.UsePhong;
            checkBoxReflections.IsChecked = renderer.UseReflections;

            textBoxDepth.Text = renderer.RecursionDepth.ToString();

            //output.Hide();

            //PhongDemoScene();
            TexturedSpheresDemoScene();

            //List<ImageFormat> formats = new List<ImageFormat>() { ImageFormat.Tiff, ImageFormat.Jpeg, ImageFormat.Bmp, ImageFormat.Png } ;
            //comboFormat.Items.Clear();
            //comboFormat.ItemsSource = saveFormats;
            //comboFormat.SelectedItem;
            //comboFormat.SelectedValue;
            comboFormat.ItemsSource = saveFormats;
//            comboFormat.SelectedItem = ImageFormat.Tiff;
            comboFormat.SelectedIndex = 0;

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
            renderer.UseLighting = (bool)checkBoxLighting.IsChecked;
            renderer.UsePhong = (bool)checkBoxPhong.IsChecked;
            renderer.UseReflections = (bool)checkBoxReflections.IsChecked;

            int depth;
            if (!Int32.TryParse(textBoxDepth.Text, out depth))
            {
                MessageBox.Show("Recursion depth must be an integer.");
                return false;
            }

            renderer.RecursionDepth = depth;

            int width = 800;
            if (!Int32.TryParse(textWidth.Text, out width))
            {
                MessageBox.Show("Invalid image width.");
                return false;
            }

            int height = width;

            canvas = new Canvas(width, height);

            double dpiX, dpiY;
            GetDpi(out dpiX, out dpiY);

            bitmap = new WriteableBitmap(canvas.Width, canvas.Height, dpiX, dpiY, PixelFormats.Bgr32, null);

            buttonRender.IsEnabled = false;

            int step = height / 4;

            List<Task> tasks = new List<Task>();

            for (int i = 0; i < height / step; i++)
            {
                int idx = i;
                Task renderTask = Task.Factory.StartNew(() =>
                {
                    //int idx = i;
                    Console.WriteLine($"Starting Task {idx}, lines {idx*step} to {idx*step + step}...");
                    renderer.Render(scene, canvas, idx*step, step);
                    Console.WriteLine($"Done Task {idx}...");

                    this.Dispatcher.Invoke(DispatcherPriority.Normal,
                                        (ThreadStart)delegate ()
                                        {
                                            Console.WriteLine($"Updating {idx}, lines {idx * step} to {idx*step + step}...");
                                            Update(idx*step, step);
                                        }
                    );
                });
                tasks.Add(renderTask);
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
                    ImageFormat format = (ImageFormat)comboFormat.SelectedItem;
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
    }
}
