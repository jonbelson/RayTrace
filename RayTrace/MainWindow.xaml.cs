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
    // Model for Entities displayed in the TreeView.
    public class TreeViewEntityModel
    {
        public string Category { get; set; }
        public Entity Instance { get; set; }
        public IEnumerable<TreeViewEntityModel> Children { get; set; }
    }

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
        public ObservableCollection<Entity> SelectedEntity { get; set; } = new ObservableCollection<Entity>();

        public ObservableCollection<MaterialTile> MaterialTiles { get; set; } = new ObservableCollection<MaterialTile>();
        public List<Material> Materials { get; set; } = new();

        private readonly List<ImageFormat> saveFormats = new List<ImageFormat>() { ImageFormat.Tiff, ImageFormat.Jpeg, ImageFormat.Bmp, ImageFormat.Png };
        public List<ImageFormat> SaveFormats { get { return saveFormats; } }

        public ImageFormat SelectedFormat { get; set; }

        // Flags for Scene Preview.
        public bool PreviewGroundPlane { get; set; } = true;
        public bool PreviewAxis { get; set; } = true;

        // Update the Entity TreeView from teh current Entity list.
        private void UpdateEntityTreeView()
        {
//            var category = Entities.OfType<Shape>().Select(x => new TreeViewEntityModel() { Category = "Shape", Children = x.Select(y => new TreeViewEntityModel() { }).ToArray() });
//            var category = Entities.OfType<Shape>().GroupBy(x => x.GetType().Name).Select(y => new TreeViewEntityModel() { Category = y.Key, Children = y.Select(k => new TreeViewEntityModel() { Category=k.Name }).ToArray() });

            IEnumerable<TreeViewEntityModel> shapes = Entities.OfType<Shape>().GroupBy(x => typeof(Shape).ToString()).Select(y => new TreeViewEntityModel() { Category = "Shapes" /*y.Key*/, Children = y.Select(k => new TreeViewEntityModel() { Category = k.Name, Instance = k }).ToArray() });
            IEnumerable<TreeViewEntityModel> lights = Entities.OfType<Light>().GroupBy(x => typeof(Light).ToString()).Select(y => new TreeViewEntityModel() { Category = "Lights" /*y.Key*/, Children = y.Select(k => new TreeViewEntityModel() { Category = k.Name, Instance = k }).ToArray() });
            IEnumerable<TreeViewEntityModel> categories = shapes.Concat(lights);

            categories = categories.Prepend(new TreeViewEntityModel() { Category = "Camera", Instance = scene.Camera });

            //entityTreeView.Items = null;
            entityTreeView.ItemsSource = categories;
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

            textBoxGINumRays.Text = renderer.NumGIRays.ToString();
            textBoxGIDepth.Text = renderer.GIDepth.ToString();

            //scene = SceneFactory.PhongDemoScene();
            //scene = SceneFactory.RefractionDemoScene();
            //scene = SceneFactory.BoxDemoScene();
            //scene = SceneFactory.SphereDemoScene();
            //scene = SceneFactory.SoftLightDemoScene();
            //scene = SceneFactory.FovDemoScene();
            //scene = SceneFactory.ModelScene();
            //scene = SceneFactory.GIDemoScene();
            //scene = SceneFactory.GIDemoScene2();
//            scene = SceneFactory.TexturedSpheresDemoScene();
            //scene = SceneFactory.ObjScene();
            scene = SceneFactory.BRDFScene();

            //scene = SceneFactory.EmptyScene();

            /*
                        Matrix m = new Matrix(new double[] { 2, 5, 1, 4,  4, 1, 6, 3,  5, 3, 7, 2,  1, 0, 2, 4 });

                        double det = m.GetDeterminant();


                        Debug.WriteLine(m.ToString());


                        Matrix inverse = m.GetInverse();

                        Debug.WriteLine(inverse.ToString());

                        Matrix product = m * inverse;

                        Debug.WriteLine(product.ToString());
            */

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


            UpdateEntityTreeView();


            //materialListBox.ItemsSource = Entities;// MaterialTiles;

            // materialListBox.Items.Add();
            //*/
        }

        private void Entity_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            UpdateViewport3D(e.PropertyName);
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

            renderer.NumGIRays = giNumRays;

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

        private void UpdateViewport3D(string propName = "")
        {
            // Set up camera.
            Point cameraPos = scene.Camera.Pos;
            Vector cameraView = scene.Camera.GetViewVector().Normalise();
            viewport3D.Camera = new PerspectiveCamera(new Point3D(cameraPos.X, cameraPos.Y, -cameraPos.Z), new Vector3D(cameraView.I, cameraView.J, -cameraView.K), new Vector3D(0.0, 1.0, 0.0), scene.Camera.FovH /* /2 */);

            if (propName == "Pos" || propName=="Target")
            {
                viewport3D.Visibility = Visibility.Visible;
                return;
            }

            viewport3D.Visibility = Visibility.Hidden;
            viewport3D.Children.Clear();

            // Add Entities to preview.
            Preview3D preview3D = new(scene);

            preview3D.Update();

            viewport3D.Children.Add(preview3D);


            Func<Point3D, Point3D, Point3DCollection> CreateLine = delegate (Point3D p1, Point3D p2) { return new Point3DCollection(new List<Point3D>() { p1, p2 }); };

            // Add ground plane to preview.
            if (PreviewGroundPlane)
            {
                int planeSize = 64;

                for (int z = -planeSize / 2; z <= planeSize / 2; z += 2)
                {
                    _3DTools.ScreenSpaceLines3D line = new();

                    line.Points = CreateLine(new Point3D(-planeSize / 2, 0, z), new Point3D(planeSize / 2, 0, z));
                    line.Color = Colors.DarkGray;
                    line.Thickness = (z % 10 == 0) ? 2 : 1;

                    viewport3D.Children.Add(line);
                }


                for (int x = -planeSize / 2; x <= planeSize / 2; x += 2)
                {
                    _3DTools.ScreenSpaceLines3D line = new();

                    line.Points = CreateLine(new Point3D(x, 0, -planeSize / 2), new Point3D(x, 0, planeSize / 2));
                    line.Color = Colors.DarkGray;
                    line.Thickness = (x % 10 == 0) ? 2 : 1;

                    viewport3D.Children.Add(line);
                }
            }

            // Add axis to preview.
            if (PreviewAxis)
            {
                int axisSize = 10;
                {
                    _3DTools.ScreenSpaceLines3D lines = new();

                    lines.Points = CreateLine(new Point3D(0, 0, 0), new Point3D(axisSize, 0, 0));
                    lines.Thickness = 3;
                    lines.Color = Colors.Red;

                    viewport3D.Children.Add(lines);
                }
                {
                    _3DTools.ScreenSpaceLines3D lines = new();

                    lines.Points = CreateLine(new Point3D(0, 0, 0), new Point3D(0, axisSize, 0));
                    lines.Thickness = 3;
                    lines.Color = Colors.Green;

                    viewport3D.Children.Add(lines);
                }
                {
                    _3DTools.ScreenSpaceLines3D lines = new();

                    lines.Points = CreateLine(new Point3D(0, 0, 0), new Point3D(0, 0, -axisSize));
                    lines.Thickness = 3;
                    lines.Color = Colors.Blue;

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

        bool dragging = false;
        private System.Windows.Point clickPoint = new();

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            clickPoint = e.GetPosition(viewport3D);
            dragging = true;
        }

        private void Border_MouseUp(object sender, MouseButtonEventArgs e)
        {
            dragging = false;
        }

        private void Border_MouseMove(object sender, MouseEventArgs e)
        {
            bool ctrlPressed = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
            bool shiftPressed = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

            if (!dragging)
            {
                clickPoint = e.GetPosition(viewport3D);
                dragging = true;
                return;
            }

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                System.Windows.Point p = e.GetPosition(viewport3D);

                float xDiff = (float)(p.X - clickPoint.X);
                float yDiff = (float)(p.Y - clickPoint.Y);

                if (!ctrlPressed && !shiftPressed)
                {
                    // Move camera Target

                    //Vector left = scene.Camera.GetViewVector().VectorProduct(new Vector(0, 1, 0));

                    // Rotate around camera target? Maybe with modifier key.
                    //double radius = scene.Camera.GetViewVector().Magnitude();

                    Vector offset = new Vector(-xDiff, 0, 0 /*yDiff*/);
                    scene.Camera.Target += offset;

                    Vector offset2 = new Vector(/*-xDiff*/ 0, yDiff, 0);
                    scene.Camera.Target += offset2;
                }
                else if (ctrlPressed && !shiftPressed)
                {
                    // Move camera in its plane.

                    Vector view = scene.Camera.GetViewVector().Normalise();

                    Vector left = view.VectorProduct(new Vector(0, 1, 0));

                    Vector up = view.VectorProduct(left);

                    Vector offset = left * xDiff - up * yDiff;

                    scene.Camera.Pos += offset;
                    scene.Camera.Target += offset;
                }
                else if (!ctrlPressed && shiftPressed)
                {
                    // Move camera in XZ plane

                    Vector view = scene.Camera.GetViewVector().Normalise();

                    Vector left = view.VectorProduct(new Vector(0, 1, 0));

                    left.J = 0.0f;

                    view.J = 0.0f;

                    Vector offset = left * xDiff + view * yDiff;

                    scene.Camera.Pos += offset;
                    scene.Camera.Target += offset;
                }
                else if (ctrlPressed && shiftPressed)
                {
                    // Rotate camera around target.

                    Vector view = scene.Camera.GetViewVector();

                    Vector targetToCamera = -view;

                    Point target = scene.Camera.Target;

                    //Matrix toOrigin = Matrix.GetTranslation(-target.X, -target.Y, -target.Z);
                    Matrix rotation = Matrix.GetRotationY((float)(xDiff / 10.0));
                    //Matrix fromOrigin = Matrix.GetTranslation(target.X, target.Y, target.Z);

                    //targetToCamera = toOrigin * targetToCamera;
                    targetToCamera = rotation * targetToCamera;
                    //targetToCamera = fromOrigin * targetToCamera;

                    scene.Camera.Pos = scene.Camera.Target + targetToCamera;
                }

                clickPoint = p;
            }
        }

        private void Border_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            int delta = e.Delta;

            Vector v = scene.Camera.GetViewVector().Normalise();

            if (delta < 0) v = -v;

            scene.Camera.Pos += v;

            e.Handled = true;
        }

        private void entityTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeViewEntityModel model = e.NewValue as TreeViewEntityModel;
            if (model != null)
            {
                // Only interested in leaf nodes.
                if (model.Children == null || !model.Children.Any())
                {
                    if (model.Instance != null)
                    {
                        SelectedEntity.Clear();
                        SelectedEntity.Add(model.Instance);
                    }
                }
                else
                {
                    SelectedEntity.Clear();
                }

                //var v = entityTreeView.SelectedItem;
            }
        }
    }
}
