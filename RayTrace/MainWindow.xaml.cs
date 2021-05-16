using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace RayTrace
{
    /*
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
    */

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        private Scene scene = new();
        private Renderer renderer = new();
   
        //private Canvas canvas = null;
        //WriteableBitmap bitmap = null;

        private OutputWindow outputWindow = new OutputWindow();

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

        public ObservableCollection<MaterialTile> MaterialTiles { get; set; } = new ObservableCollection<MaterialTile>();
        public List<Material> Materials { get; set; } = new();

        private readonly List<ImageFormat> saveFormats = new List<ImageFormat>() { ImageFormat.Tiff, ImageFormat.Jpeg, ImageFormat.Bmp, ImageFormat.Png };
        public List<ImageFormat> SaveFormats { get { return saveFormats; } }

        public ImageFormat SelectedFormat { get; set; }

        // Flags for Scene Preview.
        public bool PreviewGroundPlane { get; set; } = true;
        public bool PreviewAxis { get; set; } = true;

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

        // https://social.msdn.microsoft.com/Forums/vstudio/en-US/29936562-3fa5-45e9-a76a-626f8a212a00/createbitmapsourcefromhbitmap-dpi
        private WriteableBitmap BitmapToWriteableBitmap(Bitmap bmp)
        {
            // Lock the bitmap's bits.
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height);
            System.Drawing.Imaging.BitmapData bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppRgb);

            // Declare an array to hold the bytes of the bitmap.
            int bufferSize = bmpData.Stride * bmp.Height;

            WriteableBitmap wb = new WriteableBitmap(bmp.Width, bmp.Height, bmp.HorizontalResolution, bmp.VerticalResolution, PixelFormats.Bgr32, null);

            wb.WritePixels(new Int32Rect(0, 0, bmp.Width, bmp.Height), bmpData.Scan0, bufferSize, bmpData.Stride);

            // Unlock the bits.
            bmp.UnlockBits(bmpData);

            //ShowMe.Source = wb;

            return wb;
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
            scene = SceneFactory.TexturedSpheresDemoScene();
            //scene = SceneFactory.RefractionDemoScene();
            //scene = SceneFactory.BoxDemoScene();
            //scene = SceneFactory.SphereDemoScene();
            //scene = SceneFactory.SoftLightDemoScene();
            //scene = SceneFactory.FovDemoScene();
            //scene = SceneFactory.ModelScene();
            //scene = SceneFactory.ObjScene();
            //scene = SceneFactory.GIDemoScene();
            //scene = SceneFactory.GIDemoScene2();

            //scene = SceneFactory.EmptyScene();

            UpdateViewport3D();
/*
            // Subscribe to PropertyChanged events for each Entity.
            foreach (Entity entity in scene.Shapes)
            {
                entity.PropertyChanged += Entity_PropertyChanged;
            }
            foreach (Entity entity in scene.Lights)
            {
                entity.PropertyChanged += Entity_PropertyChanged;
            }
            scene.Camera.PropertyChanged += Entity_PropertyChanged;
*/

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

            foreach (Entity entity in Entities)
            {
                entity.PropertyChanged += Entity_PropertyChanged;
            }



            //listView.ItemsSource = Entities;

            Materials = scene.Materials;

            ///*
            MaterialTiles.Clear();
            MaterialTileMaker materialTileMaker = new() { Size = 150 };
            foreach (Material mat in scene.Materials)
            {
                MaterialTile materialTile = materialTileMaker.MakeTile(mat);

                materialTile.WriteableBitmap = BitmapToWriteableBitmap(materialTile.Bitmap);

                MaterialTiles.Add(materialTile);
            }

            //materialListBox.ItemsSource = Entities;// MaterialTiles;

           // materialListBox.Items.Add();
//*/
        }

        private void Entity_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            UpdateViewport3D();
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

            //canvas = new Canvas(width, height);

            //GetDpi(out double dpiX, out double dpiY);

            //bitmap = new WriteableBitmap(canvas.Width, canvas.Height, dpiX, dpiY, PixelFormats.Bgr32, null);

            outputWindow.SetOutputSize(width, height);

            buttonRender.IsEnabled = false;

            /*
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
                case RenderDirection.CentreOut: renderBlocks.Sort(new TopToBottomComparer(width / 2, height / 2));  break;
                case RenderDirection.Diagonal:  renderBlocks.Sort(new DiagonalComparer());  break;
                case RenderDirection.TopToBottom:  / The default / break;
            }
        */


            scene.Camera.UpdateMatrix();    // XXX Needed?
            scene.CreateCaches();

            ThreadPool.GetMaxThreads(out int workerThreads, out int completonPortThreads);

            ThreadPool.SetMaxThreads(16, completonPortThreads);

            outputWindow.Scene = scene;
            outputWindow.Renderer = renderer;

            //outputWindow.BlocksToDo = renderBlocks.Count;
            //outputWindow.BlocksDone = 0;

            outputWindow.Visibility = Visibility.Visible;
            outputWindow.Show();


            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            List<Task> tasks = new();

            CancellationTokenSource tokenSource = new();
            CancellationToken token = tokenSource.Token;

            outputWindow.TokenSource = tokenSource;

            await outputWindow.DoRender(numRenderBlocks, direction);

/*
            int i = 0;
            foreach (RenderBlock rb in renderBlocks)
            {
                int idx = i;
                Task renderTask = Task.Factory.StartNew(() =>
                {
                    if (token.IsCancellationRequested) return;
                    Thread.CurrentThread.Name = $"Task {idx}, lines {rb.StartY} to {rb.StartY + rb.Height}...";
                    Console.WriteLine($"Starting Task {idx}, lines {rb.StartY} to {rb.StartY + rb.Height}...");
                    renderer.Render(scene, outputWindow.canvas, rb.StartX, rb.StartY, rb.Width, rb.Height);
                    Console.WriteLine($"Done Task {idx}...");

                    this.Dispatcher.Invoke(DispatcherPriority.Normal,
                                        (ThreadStart)delegate ()
                                        {
                                            //Console.WriteLine($"Updating {idx}, lines {rb.StartY} to {rb.StartY + rb.Height}...");
                                            outputWindow.Update(rb.StartX, rb.StartY, rb.Width, rb.Height);
                                        }
                    );
                }, token);
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
*/
            stopWatch.Stop();
            // Get the elapsed time as a TimeSpan value.
            TimeSpan ts = stopWatch.Elapsed;

            // Format and display the TimeSpan value.
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            Console.WriteLine("Render time: " + elapsedTime);

            buttonRender.IsEnabled = true;

            bool success = true;
            if (success)
            {
                outputWindow.Update();

                if ((bool)checkBoxSaveImage.IsChecked)
                {
                    ImageFormat format = (ImageFormat)comboBoxFormat.SelectedItem;
                    string fileName = textFileName.Text;
                    if (!String.IsNullOrEmpty(fileName))
                    {
                        outputWindow.canvas.Save(fileName, format);
                    }
                }
            }

            return true;
        }

        private void UpdateViewport3D()
        {
            //Visual3DCollection children = viewport3D.Children;
            //Visual3D visual = children.ElementAt(0);

            viewport3D.Visibility = Visibility.Hidden;
            viewport3D.Children.Clear();

            Model3DGroup group = new Model3DGroup();

            ModelVisual3D modelVisual = new();
            modelVisual.Content = group;
            viewport3D.Children.Add(modelVisual);

            //var rotation = new AxisAngleRotation3D(new Vector3D(0 ,0 ,1), 10);
            //group.Transform = new RotateTransform3D(rotation);

            group.Transform = new ScaleTransform3D(1.0, 1.0, -1.0);

            Point cameraPos = scene.Camera.Pos;
            Vector cameraView = scene.Camera.GetViewVector().Normalise();
            viewport3D.Camera = new PerspectiveCamera(new Point3D(cameraPos.X, cameraPos.Y, -cameraPos.Z), new Vector3D(cameraView.I, cameraView.J, -cameraView.K), new Vector3D(0.0, 1.0, 0.0), scene.Camera.FovH /* /2 */);
            //viewport3D.Camera.Transform = new ScaleTransform3D(-1.0, -1.0, 1.0);

            // Handy lambda expressions for converting to Wpf format.
            Func<Point, Point3D> PointToPoint3D = (p) => new Point3D(p.X, p.Y, p.Z);
            Func<Vector, Vector3D> VectorToVector3D = (v) => new Vector3D(v.I, v.J, v.K);
            Func<Colour, System.Windows.Media.Color> ColourToColor = (c) => new System.Windows.Media.Color() { ScR = c.Red, ScG = c.Green, ScB = c.Blue };


            foreach (Light light in scene.Lights)
            {
                if (!light.Enabled) continue;

                if (light is PointLight)
                {
                    PointLight pointLight = light as PointLight;

                    System.Windows.Media.Media3D.PointLight l = new(ColourToColor(pointLight.Output), PointToPoint3D(pointLight.Pos));

                    // Attenuation formula based on distance (D):
                    // ConstantAttenuation + LinearAttenuation * D + QuadraticAttenuation * D^2
                    l.QuadraticAttenuation = 0.001;
                    //l.LinearAttenuation = 0.01;

                    group.Children.Add(l);
                }
                else if (light is DistantLight)
                {
                    DistantLight distantLight = light as DistantLight;

                    System.Windows.Media.Media3D.DirectionalLight l = new(ColourToColor(distantLight.Output), VectorToVector3D(distantLight.Direction));

                    group.Children.Add(l);
                }
            }

            AmbientLight al = new(ColourToColor(scene.Ambient));
            group.Children.Add(al);

            foreach (Shape shape in scene.Shapes)
            {
                if (!shape.Enabled) continue;

                Sphere sphere = shape as Sphere;
                if (sphere != null)
                {
                    GeometryModel3D model2 = GeometryModel3DFactory.MakeSphere(sphere);

                    group.Children.Add(model2);
                }
                else if (shape is Plane)
                {
                    Plane plane = (Plane)shape;

                    GeometryModel3D model2 = GeometryModel3DFactory.MakePlane(plane);

                    group.Children.Add(model2);
                }
                else if (shape is Model)
                {
                    Model model = (Model)shape;

                    GeometryModel3D geometryModel = GeometryModel3DFactory.MakeModel(model);

                    group.Children.Add(geometryModel);
                }
            }

            //ModelVisual3D modelVisual = new();
            //modelVisual.Content = group;
            //viewport3D.Children.Add(modelVisual);

            if (PreviewGroundPlane)
            {
                int planeSize = 64;

                for (int z = -planeSize / 2; z <= planeSize / 2; z += 2)
                {
                    _3DTools.ScreenSpaceLines3D line = new();
                    line.Points.Add(new Point3D(-planeSize / 2, 0, z));
                    line.Points.Add(new Point3D(planeSize / 2, 0, z));

                    line.Color = System.Windows.Media.Colors.DarkGray;
                    line.Thickness = (z % 10 == 0) ? 2 : 1;

                    viewport3D.Children.Add(line);
                }


                for (int x = -planeSize / 2; x <= planeSize / 2; x += 2)
                {
                    _3DTools.ScreenSpaceLines3D line = new();
                    line.Points.Add(new Point3D(x, 0, -planeSize / 2));
                    line.Points.Add(new Point3D(x, 0, planeSize / 2));

                    line.Color = System.Windows.Media.Colors.DarkGray;
                    line.Thickness = (x % 10 == 0) ? 2 : 1;

                    viewport3D.Children.Add(line);
                }
            }


            if (PreviewAxis)
            {
                int axisSize = 10;
                {
                    _3DTools.ScreenSpaceLines3D lines = new();

                    //lines.Transform = new ScaleTransform3D(0, 0, -1);

                    //System.Windows.Media.Color axisColor = System.Windows.Media.Colors.White;
                    lines.Points.Add(new Point3D(0, 0, 0));
                    lines.Points.Add(new Point3D(axisSize, 0, 0));

                    lines.Thickness = 3;
                    lines.Color = System.Windows.Media.Colors.Red;

                    viewport3D.Children.Add(lines);
                }
                {
                    _3DTools.ScreenSpaceLines3D lines = new();

                    lines.Points.Add(new Point3D(0, 0, 0));
                    lines.Points.Add(new Point3D(0, axisSize, 0));

                    lines.Points.Add(new Point3D(0, 0, 0));
                    lines.Points.Add(new Point3D(0, 0, -axisSize));

                    lines.Thickness = 3;
                    lines.Color = System.Windows.Media.Colors.Green;

                    viewport3D.Children.Add(lines);

                }
                {
                    _3DTools.ScreenSpaceLines3D lines = new();

                    lines.Points.Add(new Point3D(0, 0, 0));
                    lines.Points.Add(new Point3D(0, 0, -axisSize));

                    lines.Thickness = 3;
                    lines.Color = System.Windows.Media.Colors.Blue;

                    viewport3D.Children.Add(lines);
                }
            }

            viewport3D.Visibility = Visibility.Visible;
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

        private void materialListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //MessageBox.Show("Edit material");

            MaterialTile materialTile = (MaterialTile)materialListBox.SelectedItem;

            if (materialTile != null)
            {
                MaterialEditorDialog dlg = new(materialTile.Material);
                dlg.Owner = this;
                //dlg.Margin = this.documentTextBox.Margin;

                bool? result = dlg.ShowDialog();
                if (result == true)
                {
                    MaterialTileMaker materialTileMaker = new() { Size = 150 };

                    MaterialTile newTile = materialTileMaker.MakeTile(materialTile.Material);

                    materialTile.Name = newTile.Name;
                    materialTile.Bitmap = newTile.Bitmap;
                    materialTile.WriteableBitmap = newTile.Bitmap.ToWriteableBitmap();
                    materialListBox.Items.Refresh();
                }
            }
        }

        private void comboBoxMaterialType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //ListBox listBox;

            ComboBox comboBox = sender as ComboBox;
            if (comboBox != null)
            {
                var selectedValue = comboBox.SelectedValue;
                var selectedItem = comboBox.SelectedItem;

//                MaterialTile materialTile = (MaterialTile) comboBox.SelectedItem;
//                Material material = (Material)comboBox.SelectedItem;

                Shape shape = listBox.SelectedItem as Shape;
                if (shape != null)
                {
                    shape.Material = (Material) selectedValue;
                }

            }
        }

        private void window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // We need to do this to make sure the Render Window closes when the MainWindow closes.
            // However, we can't do this until the Main Window has been shown.
            outputWindow.Owner = this;
        }

        private void checkBoxGroundPlane_Click(object sender, RoutedEventArgs e)
        {
            UpdateViewport3D();
        }

        private void checkBoxAxis_Click(object sender, RoutedEventArgs e)
        {
            UpdateViewport3D();
        }
    }
}
