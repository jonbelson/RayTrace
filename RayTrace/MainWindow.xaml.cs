using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
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
//using System.Windows.Shapes;
using System.Windows.Threading;
using static System.Formats.Asn1.AsnWriter;

using RayTrace.Importers;
using RayTrace.Preview;
using RayTrace.Skydomes;
using RayTrace.Brdfs;

namespace RayTrace
{
    // Utiity class to return application-specific directories.
    public static class Storage
    {
        private static string _appDataDir;
        private static string _appTempDir;

        // Return directory for storing app-specific data.
        public static string DataDir {
            get
            {
                if (_appDataDir == null)
                {
                    string appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

                    _appDataDir = System.IO.Path.Combine(appDataDir, AppDomain.CurrentDomain.FriendlyName);

                    if (!Directory.Exists(_appDataDir))
                    {
                        Directory.CreateDirectory(_appDataDir);
                    }
                }
                return _appDataDir;
            }
        }

        // Return directory for storing temporary app-specific data.
        public static string TempDir {
            get
            {
                if (_appTempDir == null)
                {
                    string tempPath = System.IO.Path.GetTempPath();
                    _appTempDir = System.IO.Path.Combine(tempPath, AppDomain.CurrentDomain.FriendlyName);

                    if (!Directory.Exists(_appTempDir))
                    {
                        Directory.CreateDirectory(_appTempDir);
                    }
                }

                return _appTempDir;
            }
        }
    }

    // Model for Entities displayed in the TreeView.
    public class TreeViewEntityModel
    {
        public bool? IsChecked
        {
            get => Instance != null && CanBeDisabled ? Instance.Enabled : true;
            set
            {
                if (Instance != null)
                {
                    Instance.Enabled = (bool)value;
                }
            }
        }

        public bool IsSelected { get; set; } = false;

//        public Visibility IsVisible => Instance == null ? Visibility.Hidden : Visibility.Visible;
        public bool IsVisible => Instance != null;

        public bool CanBeDisabled { get; set; } = true; // Kludge to prevent Camera from being unchecked.

        public bool IsCategory { get; set; } = false;

        public string Name { get; set; }
        public Entity Instance { get; set; }
        //public IEnumerable<TreeViewEntityModel> Children { get; set; }
        public List<TreeViewEntityModel> Children { get; set; } = new();
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        private Scene scene = new();
        private Renderer renderer = new();

        private OutputWindow outputWindow = new();

        //private PreviewVisual3D preview3D;

        // Cameras for each of the views.
        private PerspectiveCamera camera3D;
        private OrthographicCamera cameraFront;
        private OrthographicCamera cameraTop;
        private OrthographicCamera cameraLeft;

        private InputMode _inputMode;

       // private BoundingBox3D boundingBox;
       // private ModelAxis3D modelAxis;

        // Site of Material tile bitmaps. SHould be divisible by 8.
        private const int MaterialTileSize = 152;

        // Number of render blocks in x and y directions.
        public int NumRenderBlocks { get; set; } = 16;

        public enum RenderDirection
        {
            TopToBottom, Diagonal, CentreOut
        }

        private readonly RenderDirection[] renderDirection = { RenderDirection.TopToBottom, RenderDirection.Diagonal, RenderDirection.CentreOut };
        public RenderDirection[] RenderDirections => renderDirection;

        private readonly List<Renderer.AntiAliasType> antiAlias = new List<Renderer.AntiAliasType>() { Renderer.AntiAliasType.None, Renderer.AntiAliasType.NoiseGrid, Renderer.AntiAliasType.SuperSampling, Renderer.AntiAliasType.AdaptiveSupersampling };
        public List<Renderer.AntiAliasType> AntiAliasType => antiAlias;

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


        // Update the Entity TreeView from the current Entity list.
        private void UpdateEntityTreeView()
        {
            //var category = Entities.OfType<Shape>().Select(x => new TreeViewEntityModel() { Category = "Shape", Children = x.Select(y => new TreeViewEntityModel() { }).ToArray() });
            //var category = Entities.OfType<Shape>().GroupBy(x => x.GetType().Name).Select(y => new TreeViewEntityModel() { Category = y.Key, Children = y.Select(k => new TreeViewEntityModel() { Category=k.Name }).ToArray() });

            //object selectedItem = entityTreeView.SelectedItem;

            Entity selectedEntity = SelectedEntity.FirstOrDefault();

            List<TreeViewEntityModel> itemsSource = new();

            itemsSource.Add(new TreeViewEntityModel() { Name = "Skydome", Instance = scene.Skydome });
            itemsSource.Add(new TreeViewEntityModel() { Name = "Camera", CanBeDisabled = false, Instance = scene.Camera });

            TreeViewEntityModel shapeCategory = new() { Name = "Shapes", IsCategory = true };
            itemsSource.Add(shapeCategory);
            foreach (Shape shape in Entities.OfType<Shape>())
            {
                var treeModel = shape.ToTreeModel();
                if (shape == selectedEntity) treeModel.IsSelected = true;
                shapeCategory.Children.Add(treeModel);
            }

            TreeViewEntityModel lightCategory = new() { Name = "Lights", IsCategory = true };
            itemsSource.Add(lightCategory);
            foreach (Light light in Entities.OfType<Light>())
            {
                lightCategory.Children.Add(light.ToTreeModel());
            }



            //IEnumerable<TreeViewEntityModel> shapes = Entities.OfType<Shape>().GroupBy(x => typeof(Shape).ToString()).Select(y => new TreeViewEntityModel() { Category = "Shapes", Children = y.Select(k => new TreeViewEntityModel() { Category = k.Name, Instance = k, IsSelected = k != null && k == selectedEntity })/*.ToArray()*/ });
            //List<TreeViewEntityModel> lights = Entities.OfType<Light>().GroupBy(x => typeof(Light).ToString()).Select(y => new TreeViewEntityModel() { Category = "Lights", Children = y.Select(k => new TreeViewEntityModel() { Category = k.Name, Instance = k, IsSelected = k != null && k == selectedEntity }).ToList() });
            //IEnumerable<TreeViewEntityModel> categories = shapes.Concat(lights);

            //categories = categories.Prepend(new TreeViewEntityModel() { Category = "Skydome", Instance = scene.Skydome });
            //categories = categories.Prepend(new TreeViewEntityModel() { Category = "Camera", CanBeDisabled = false, Instance = scene.Camera });

            entityTreeView.ItemsSource = itemsSource;
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

        //
        // Udpate the tree view of Entities.
        //
        private void UpdateEntities()
        {
            Entities.Clear();

            Entities.Add(scene.Camera);

            Entities.Add(scene.Skydome);

            foreach (Light light in scene.Lights)
            {
                Entities.Add(light);
            }
            foreach (Shape shape in scene.Shapes)
            {
                Entities.Add(shape);
            }

            foreach (Entity entity in Entities)
            {
                //entity.PropertyChanged += Entity_PropertyChanged;

                entity.AddPropertyChangedHandler(Entity_PropertyChanged);
            }

            //listView.ItemsSource = Entities;

            UpdateEntityTreeView();
        }

        //
        // Update the display of Material tiles in the MainWindow.
        //
        private void UpdateMaterials()
        {
            Materials = scene.Materials;

            MaterialTiles.Clear();
            MaterialTileMaker materialTileMaker = new() { Size = MaterialTileSize };
            foreach (Material mat in scene.Materials)
            {
                MaterialTile materialTile = materialTileMaker.MakeTile(mat);

                materialTile.WriteableBitmap = BitmapToWriteableBitmap(materialTile.Bitmap);

                MaterialTiles.Add(materialTile);
            }
        }

        private void UpdateForScene()
        {
            UpdateEntities();

            UpdateMaterials();

            _inputMode = new InputMode3D(this, viewport3D, camera3D, scene);
        }

        public MainWindow()
        {
            InitializeComponent();

            //List<SPoint> points = new List<SPoint>() { new SPoint(0, 0, 0), new SPoint(1, 0, 0), new SPoint(1, 1, 0), new SPoint(0, 1, 0) };
            List<SPoint> points = new List<SPoint>() { new SPoint(0, 0, 0), new SPoint(5, 0, 0), new SPoint(5, 5, 0), new SPoint(0, 5, 0) };
            Triangulator triangulator = new(points);

            if (triangulator.Triangulate())
            {

            }

            Logger.Instance.Info($"Launched from {Environment.CurrentDirectory}");
            Logger.Instance.Info($"Physical location {AppDomain.CurrentDomain.BaseDirectory}");
            Logger.Instance.Info($"AppContext.BaseDir {AppContext.BaseDirectory}");
            Logger.Instance.Info($"Runtime Call {System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)}");
            Logger.Instance.Info($"Assembly Path {System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}");
            Logger.Instance.Info($"CWD {Directory.GetCurrentDirectory()}");

            {
                Matrix m2 = Matrix.GetTranslation(1.0f, 2.0f, 3.0f);

                Matrix m = m2.GetTranspose();

                SPoint[] p =
                [
                    new(0.0f, 0.0f, 0.0f),
                    new(10.0f, 20.0f, 30.0f)
                ];

                SPoint[] res = new SPoint[2];

                m.MatrixMultiplySIMD(p, res);

/*

                SVector[] vectors = new SVector[]
                {
                    new(0.0f, 0.0f, 0.0f),
                    new(10.0f, 20.0f, 30.0f)
                };

                SVector[] results = new SVector[2];

                m.MatrixMultiplySIMD(vectors, results);
*/            }


            ModelData.Triangle triangle = new();
            triangle.v0 = 0;
            triangle.v1 = 1;
            triangle.v2 = 2;

            SVector normal = ObjImporter.GetNormal(points, triangle);

            {
                Debug.Assert(Sse.IsSupported);
                Debug.Assert(Avx.IsSupported);
                Debug.Assert(Avx2.IsSupported);

                Matrix4x4 m = Matrix4x4.CreateTranslation(1, 2, 3);

                Vector4 v = new Vector4(0, 0, 0, 1);

                Vector4 result = Vector4.Transform(v, m);

                Matrix m2 = Matrix.GetTranslation(1, 2, 3);
                Matrix4x4 m3 = m2.ToMatrix4x4();

                //Vector3 vv = new();

                //int i = 0;
            }

            {
                Quaternion q = new Quaternion(new SVector(0, 1, 0), (float)Math.PI / 2);
                SVector v = new(0, 0, 1);
                SVector vr = q.Rotate(v);


                q = new Quaternion(new SVector(1, 0, 0), (float)Math.PI / 2);
                v = new(0, 1, 0);
                vr = q.Rotate(v);
            }

 /*           {
                Random rand = new();
                SVector Y = Renderer.GetLocalVectorInHemisphere(rand);
                SVector Up = new SVector(0f, 1f, 0f);
                LocalCoordinateSystem lcs = new LocalCoordinateSystem(Y);
                for (int i = 0; i<1000; i++)
                {
//                    SVector v = Renderer.GetLocalVectorInHemisphere(rand);
                    SVector v = Renderer.GetLocalCosineVector(rand);
                    SVector v2 = lcs.Reproject(v);

                    float dp = v.DotProduct(Up);
                    Debug.Assert(dp >= 0f);

                    dp = lcs.N.DotProduct(v2);
                    Debug.Assert(dp >= 0.0f && dp <= 1.0f);

                }

            }
 */
/*            {
                // Test LCS forward and backward conversion.
                Random rand = new();
                SVector Y = Renderer.GetLocalVectorInHemisphere(rand);
                SVector Up = new SVector(0f, 1f, 0f);
                LocalCoordinateSystem lcs = new LocalCoordinateSystem(Y);
                for (int i = 0; i<1000; i++)
                {
                    SVector v = Renderer.GetLocalVectorInHemisphere(rand);
                    //SVector v = Renderer.GetLocalCosineVector(rand);
                    SVector v2 = lcs.Reproject(v);

                    float dp = v.DotProduct(Up);
                    Debug.Assert(dp >= 0f);

                    dp = lcs.N.DotProduct(v2);
                    Debug.Assert(dp >= 0.0f && dp <= 1.0f);

                    SVector v3 = lcs.FromWorld(v2);

                    Debug.Assert(MathF.Abs(v.I - v3.I) < 0.0001f);
                    Debug.Assert(MathF.Abs(v.J - v3.J) < 0.0001f);
                    Debug.Assert(MathF.Abs(v.K - v3.K) < 0.0001f);
                }

            }
*/
/*            {
                // Test glTF BRDF
                Random rand = new();
                SVector Y = Renderer.GetLocalVectorInHemisphere(rand);
                SVector Up = new SVector(0f, 1f, 0f);
                LocalCoordinateSystem lcs = new LocalCoordinateSystem(Y);
                GltfBrdf brdf = new();
                Material material = new(MaterialType.BRDF_Gltf);
                material.Brdf = brdf;
                HitResult hr = new() { Entry = new(0f, 0f, 0f), EntryNormal = new SVector(0f, 0f, -1f), Ray = new SVector(0f, 0f, 1f),  };
                for (int i = 0; i<1000; i++)
                {
                    //SVector v = Renderer.GetLocalVectorInHemisphere(rand);
                    //SVector v = Renderer.GetLocalCosineVector(rand);
                    SVector v = material.Brdf.GetSample(hr, lcs, rand, out float pdf);

                    float dp = v.DotProduct(hr.EntryNormal);
                    Debug.Assert(dp >= 0f);

                    SVector v2 = lcs.Reproject(v);
                    dp = lcs.N.DotProduct(v2);
                    Debug.Assert(dp >= 0.0f && dp <= 1.0f);

                    SVector v3 = lcs.FromWorld(v2);

                    Debug.Assert(MathF.Abs(v.I - v3.I) < 0.0001f);
                    Debug.Assert(MathF.Abs(v.J - v3.J) < 0.0001f);
                    Debug.Assert(MathF.Abs(v.K - v3.K) < 0.0001f);
                }

            }
*/

#if false
            {
                Random rand = new();
//               LocalCoordinateSystem lcs = new LocalCoordinateSystem(new SVector(-0.512975156f, 0.49471274f, -0.6838266f).Normalise());
                LocalCoordinateSystem lcs = new LocalCoordinateSystem(Renderer.GetLocalVectorInHemisphere(rand));
                //                LocalCoordinateSystem lcs = new LocalCoordinateSystem(new SVector(0f, 0f, -1f));
                //Debug.Write("[");
                GltfBrdf brdf = new();

                for (int i=0; i<1000; i++)
                {
                    if (i != 0) Debug.Write(", ");
                    //                    SVector v = Renderer.GetLocalVectorInHemisphere(rand);
                    //                    SVector v = brdf.GetSample(rand.NextSingle(), rand.NextSingle());
//                    SVector v = GltfBrdf.ImportanceSampleGGX(rand, new SVector(0f, -1f, 1f).Normalise(), 0.5f, out float pdf);
                    SVector v = GltfBrdf.ImportanceSampleGGX(rand, new SVector(0f, -1f, 1f).Normalise(), 0.15f, out float pdf);

                    //float dp = v.DotProduct(lcs.N);
                    //Debug.Assert(dp >= 0.0f && dp <= 1.0f);

                    //v = lcs.Reproject(v);
                    Debug.Write($"({v.I}, {v.J}, {v.K})");
                }
                //Debug.WriteLine("]");

                Debug.WriteLine("");

                for (int i = 0; i<250; i++)
                {
                    if (i != 0) Debug.Write(", ");
                    //                    SVector v = Renderer.GetLocalVectorInHemisphere(rand);
                    //                    SVector v = brdf.ImportanceSampleGGX(rand.NextSingle(), rand.NextSingle());
                    SVector v = GltfBrdf.ImportanceSampleGGX(rand, new SVector(0f, -1f, 1f).Normalise(), 0.5f, out float pdf);
                    //float dp = v.DotProduct(lcs.N);
                    //Debug.Assert(dp >= 0.0f && dp <= 1.0f);

                    v = lcs.Reproject(v);
                    Debug.Write($"({v.I}, {v.J}, {v.K})");
                }

                int ii = 0;
            }
#endif

            checkBoxAmbient.IsChecked = renderer.UseAmbient;
            checkBoxLighting.IsChecked = renderer.UseDiffuse;
            checkBoxSpecular.IsChecked = renderer.UseSpecular;
            checkBoxRefraction.IsChecked = renderer.UseRefraction;
            checkBoxShadows.IsChecked = renderer.UseShadows;
            checkBoxSoftShadows.IsChecked = renderer.UseSoftShadows;
            checkBoxReflections.IsChecked = renderer.UseReflections;
            checkBoxGIEnable.IsChecked = renderer.UseGlobalIllumination;
            checkBoxDepthOfField.IsChecked = renderer.UseDepthOfField;
            checkBoxBRDF.IsChecked = renderer.UseBRDF;
            checkBoxImportanceSampling.IsChecked = renderer.UseImportanceSampling;

            textBoxDepth.Text = renderer.RecursionDepth.ToString();
            textBoxAdaptiveSubSamplingDepth.Text = renderer.AdaptiveSuperSamplingDepth.ToString();
            textBoxAdaptiveSubSamplingDepth.IsEnabled = false;

            textBoxGINumRays.Text = renderer.NumGIRays.ToString();
            textBoxGIDepth.Text = renderer.GIDepth.ToString();

            textBoxBRDFSamples.Text = renderer.NumBRDFSamples.ToString();

            //scene = SceneFactory.PhongDemoScene();
            //scene = SceneFactory.RefractionDemoScene();
            //scene = SceneFactory.BoxDemoScene();
            //scene = SceneFactory.SoftLightDemoScene();
            //scene = SceneFactory.FovDemoScene();
            //scene = SceneFactory.ModelScene();
            //scene = SceneFactory.GIDemoScene2();
            //            scene = SceneFactory.TexturedSpheresDemoScene();
            //scene = SceneFactory.BRDFScene();
            //scene = SceneFactory.SensorScene();

            //scene = SceneFactory.GIDemoScene();
            //scene = SceneFactory.SphereDemoScene();
            //scene = SceneFactory.ObjScene();
            scene = SceneFactory.GltfMaterialScene();
            //scene = SceneFactory.GltfScene();
            //scene = SceneFactory.NormalMapScene();

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

            {
                Matrix4x4 m = Matrix4x4.CreateTranslation(2, 0, 4);

                Matrix4x4 m2 = Matrix4x4.CreateRotationY((float)Math.PI/2.0f);

                var m3 = m*m2;

                Vector3 v = new(1.0f, 0, 0);

                Vector3 v2 = Vector3.Transform(v, m3);

            }


            Vector3D up = new(0.0, 1.0, 0.0);

            camera3D = new PerspectiveCamera(new Point3D(0, 0, 6), new Vector3D(0, 0, -1), up, 45);
            cameraFront = new OrthographicCamera(new Point3D(0, 0, 10), new Vector3D(0, 0, -1), up, 50);
            cameraTop = new OrthographicCamera(new Point3D(0, 20, 0), new Vector3D(0, -0.99999, 0.001), up, 50);
            cameraLeft = new OrthographicCamera(new Point3D(-10.0, 0, 0), new Vector3D(1, 0, 0), up, 50);

            SelectedEntity.CollectionChanged += SelectedEntity_CollectionChanged;

            _inputMode = new InputMode3D(this, viewport3D, camera3D, scene);

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


            UpdateForScene();

            //materialListBox.ItemsSource = Entities;// MaterialTiles;

            // materialListBox.Items.Add();
            //*/
        }

        private void UpdateBoundingBox()
        {
            ClearBoundingBox();

            Entity entity = SelectedEntity.First();

            if (entity is Shape shape)
            {
                viewport3D.UpdateBoundingBox(shape);
            }
        }

        private void ClearBoundingBox()
        {
            viewport3D.ClearBoundingBox();
        }

        private void SelectedEntity_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (SelectedEntity.Count != 0)
            {
                UpdateBoundingBox();
            }
        }

        private void Entity_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            UpdateViewport3D(sender, e.PropertyName);
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
            renderer.UseBRDF = (bool)checkBoxBRDF.IsChecked;
            renderer.UseImportanceSampling = (bool)checkBoxImportanceSampling.IsChecked;

            if (!Int32.TryParse(textBoxDepth.Text, out int depth))
            {
                MessageBox.Show("Recursion depth must be an integer.");
                return false;
            }

            renderer.RecursionDepth = depth;

            if (!Int32.TryParse(textBoxBRDFSamples.Text, out int BRDFSamples))
            {
                MessageBox.Show("Number of BRDF samples must be an integer.");
                return false;
            }

            if (BRDFSamples < 0)
            {
                MessageBox.Show("Number of BRDF samples must be at least 0.");
                return false;
            }

            renderer.NumBRDFSamples = BRDFSamples;

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
                _ = MessageBox.Show("Adaptive Super-Sampling depth must be 10 or less.");
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

            try
            {

                //scene.Camera.UpdateMatrix();    // XXX Needed?
                //scene.CreateCaches();

                //ThreadPool.GetMaxThreads(out int workerThreads, out int completonPortThreads);

                //_ = ThreadPool.SetMaxThreads(16, completonPortThreads);

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
                //CancellationToken token = tokenSource.Token;

                outputWindow.TokenSource = tokenSource;

                OutputWindow.RenderStatus status = await outputWindow.DoRender(numRenderBlocks, direction);

                stopWatch.Stop();

                if (status == OutputWindow.RenderStatus.Complete)
                {
                    // Get the elapsed time as a TimeSpan value.
                    TimeSpan ts = stopWatch.Elapsed;

                    // Format and display the TimeSpan value.
                    string elapsedTime = String.Format("Time taken: {0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                    string numRays = "Number rays: " + renderer.NumRays;
                    string raysPerSec = "Rays per second: " + (1000.0 * renderer.NumRays / stopWatch.ElapsedMilliseconds).ToString("0.00");

                    MessageBox.Show(outputWindow, String.Join("\n", elapsedTime, numRays, raysPerSec));

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
                                _ = outputWindow.OutputCanvas.Save(fileName, format);
                            }
                        }
                    }
                }
                else if (status == OutputWindow.RenderStatus.Cancelled)
                {
                    MessageBox.Show("Operation was cancelled");
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("An error occurred: " + e.Message);
            }

            buttonRender.IsEnabled = true;

            return true;
        }

        /*private*/ public void UpdateViewport3D(object sender = null, string propName = "")
        {
            // Set up camera.
            Point cameraPos = scene.Camera.Pos;
            Vector cameraView = scene.Camera.GetViewVector().Normalise();

///*
            viewport3D.Camera = camera3D;

            camera3D.Position = new Point3D(cameraPos.X, cameraPos.Y, -cameraPos.Z);
            camera3D.LookDirection = new Vector3D(cameraView.I, cameraView.J, -cameraView.K);
            camera3D.UpDirection = new Vector3D(0.0, 1.0, 0.0);
            camera3D.FieldOfView = scene.Camera.FovH;
//*/
            if (sender is Camera && (propName == "Pos" || propName == "Target"))
            {
                //viewport3D.Visibility = Visibility.Visible;
                return;
            }

            if (sender is Light light && (propName == "Pos"))
            {
                viewport3D.Update(light);
                return;
            }

            if (sender is Shape shape && (propName == "Pos"))
            {
                viewport3D.Update(shape);
                return;
            }

            viewport3D.Visibility = Visibility.Hidden;
            viewport3D.Children.Clear();

            // Add Entities to preview.
            //preview3D = new(scene);

            //preview3D.UseAmbient = (bool)checkBoxAmbient.IsChecked;
            //preview3D.Update();

            //viewport3D.Children.Add(preview3D);

            viewport3D.UseAmbient = (bool)checkBoxAmbient.IsChecked;
            viewport3D.UpdatePreview(scene);

            //Func<Point3D, Point3D, Point3DCollection> CreateLine = delegate (Point3D p1, Point3D p2) { return new Point3DCollection(new List<Point3D>() { p1, p2 }); };

            // Add ground plane to preview.
            if (PreviewGroundPlane)
            {
                viewport3D.CreateGroundPlane();
            }

            // Add axis to preview.
            if (PreviewAxis)
            {
                viewport3D.CreateGroundAxis();
            }

            viewport3D.Visibility = Visibility.Visible;
        }

        private async void buttonRender_Click(object sender, RoutedEventArgs e)
        {
            await Render();

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
            checkBoxSpecular.IsEnabled = (bool)checkBoxLighting.IsChecked;
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

                    UpdateForScene();

                    UpdateViewport3D();

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

        private void AddMaterial()
        {
            // Create a new Material.
            Material material = new();
            MaterialEditorDialog dlg = new(material);
            dlg.Owner = this;

            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                MaterialTileMaker materialTileMaker = new() { Size = MaterialTileSize };

                MaterialTile newTile = materialTileMaker.MakeTile(material);
                newTile.WriteableBitmap = BitmapToWriteableBitmap(newTile.Bitmap);

                //materialTile.Name = newTile.Name;
                //materialTile.Bitmap = newTile.Bitmap;
                //materialTile.WriteableBitmap = newTile.Bitmap.ToWriteableBitmap();

                scene.AddMaterial(material);
                MaterialTiles.Add(newTile);
                materialListBox.Items.Refresh();
            }
        }

        private void EditMaterial(MaterialTile materialTile)
        {
            MaterialEditorDialog dlg = new(materialTile.Material);
            dlg.Owner = this;
            //dlg.Margin = this.documentTextBox.Margin;

            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                MaterialTileMaker materialTileMaker = new() { Size = MaterialTileSize };

                MaterialTile newTile = materialTileMaker.MakeTile(materialTile.Material);

                materialTile.Name = newTile.Name;
                materialTile.Bitmap = newTile.Bitmap;
                materialTile.WriteableBitmap = newTile.Bitmap.ToWriteableBitmap();
                materialListBox.Items.Refresh();

                UpdateViewport3D();
            }
        }

        private void materialListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            MaterialTile materialTile = (MaterialTile)materialListBox.SelectedItem;

            if (materialTile == null)
            {
                AddMaterial();
            }
            else
            {
                EditMaterial(materialTile);
            }
        }

        private void comboBoxMaterialType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
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
                    shape.Material = (Material)selectedValue;
                    UpdateViewport3D();
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

        //bool dragging = false;
        //private System.Windows.Point clickPoint = new();

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Mouse.Capture(viewport3D, CaptureMode.SubTree);

            _inputMode.DoLButtonDown(e.GetPosition(viewport3D));
        }

        private void Border_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Mouse.Capture(null);

            _inputMode.DoLButtonUp(e.GetPosition(viewport3D));
        }

        //
        // Handle mouse drags over the 3D preview.
        //
        private void Border_MouseMove(object sender, MouseEventArgs e)
        {
            _inputMode.DoMouseMove(e.GetPosition(viewport3D));
        }

        private void Border_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            int delta = e.Delta;

            _inputMode?.DoMouseWheel(delta);

            e.Handled = true;
        }

        private void entityTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewEntityModel model)
            {
                // Only interested in leaf nodes.
                if (!model.IsCategory /*|| model.Children == null || !model.Children.Any()*/)
                {
                    if (model.Instance != null)
                    {
                        //SelectedEntity2 = model.Instance;

                        SelectedEntity.Clear();
                        SelectedEntity.Add(model.Instance);
                    }
                }
                else
                {
                    SelectedEntity.Clear();

                    //SelectedEntity2 = null;
                }

                //var v = entityTreeView.SelectedItem;
            }
        }

        private void UpdateMaterialListBox(Model model)
        {
            UpdateMaterials();  // TODO For now.

            materialListBox.Items.Refresh();

            /*
                        List<Material> materials = model.GetMaterials();

                        MaterialTileMaker materialTileMaker = new() { Size = MaterialTileSize };

                        foreach (Material material in materials)
                        {
                            MaterialTile newTile = materialTileMaker.MakeTile(material);
                            newTile.WriteableBitmap = BitmapToWriteableBitmap(newTile.Bitmap);

                            MaterialTiles.Add(newTile);
                        }

                        materialListBox.Items.Refresh();
            */
        }

        private void galleryModel_SelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            RibbonGallery gallery = (RibbonGallery)sender;
            if (gallery != null)
            {
                Shape shape = null;

                RibbonGalleryItem item = (RibbonGalleryItem)e.NewValue;
                switch (item.Content)
                {
                    case "Model":
                        {
                            // Configure open file dialog box
                            OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
                            dialog.FileName = ""; // Default file name
                            dialog.DefaultExt = ".obj"; // Default file extension
//                            dialog.Filter = "OBJ documents (.obj)|*.obj|glTF documents (.gltf)|*.gltf"; // Filter files by extension
                            dialog.Filter = "All Models (.obj;.gltf)|*.obj;*.gltf| OBJ Models (.obj)|*.obj|glTF Models (.gltf)|*.gltf"; // Filter files by extension
                            dialog.FilterIndex = 0;

                            // Show open file dialog box
                            bool? result = dialog.ShowDialog();

                            // Process open file dialog box results
                            if (result == true)
                            {
                                // Open document
                                string fileName = dialog.FileName;

                                shape = scene.ImportModel(fileName);

                                UpdateMaterials();
                            }

                        }
                        break;
                }

                UpdateEntities();

                if (shape != null)
                {
                    SelectedEntity.Clear();
                    SelectedEntity.Add(shape);
                }

                UpdateEntityTreeView();

                UpdateViewport3D();

                gallery.SelectedItem = null;
            }
        }

        private void galleryGeometric_SelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            RibbonGallery gallery = (RibbonGallery)sender;
            if (gallery != null)
            {
                Shape shape = null;
                RibbonGalleryItem item = (RibbonGalleryItem)e.NewValue;
                switch (item.Content)
                {
                    case "Disc":
                        shape = new Disc(scene, new Point(0, 0, 0), new Vector(0, 1, 0), 1);
                        break;
                    case "Plane":
                        shape = new Plane(scene, new Point(0, 0, 0), new Vector(0, 1, 0));
                        break;
                    case "Sphere":
                        shape = new Sphere(scene, new Point(0, 0, 0), 1);
                        break;
                }

                UpdateEntities();

                if (shape != null)
                {
                    SelectedEntity.Clear();
                    SelectedEntity.Add(shape);
                }

                UpdateEntityTreeView();

                UpdateViewport3D();

                gallery.SelectedItem = null;
            }
        }

        private void galleryLight_SelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            RibbonGallery gallery = (RibbonGallery)sender;
            if (gallery != null)
            {
                Light light = null;
                RibbonGalleryItem item = (RibbonGalleryItem)e.NewValue;
                switch (item.Content)
                {
                    case "Distant":
                        light = new DistantLight(scene, new Vector(1, 0, 0), 100, new Colour(1, 1, 1));
                        scene.AddLight(light);
                        break;
                    case "Point":
                        light = new PointLight(scene, new Point(0, 0, 0), 100, new Colour(1, 1, 1));
                        scene.AddLight(light);
                        break;
                    case "Spot":
                        light = new SpotLight(scene, new Point(0, 0, 0), new Vector(1, 0, 0), 45, 100, new Colour(1, 1, 1));
                        scene.AddLight(light);
                        break;
                }

                UpdateEntities();

                if (light != null)
                {
                    SelectedEntity.Clear();
                    SelectedEntity.Add(light);
                }

                UpdateEntityTreeView();

                UpdateViewport3D();

                gallery.SelectedItem = null;

            }
        }

        private void RibbonApplicationMenuItem_Click(object sender, RoutedEventArgs e)
        {
            scene = new();
            UpdateForScene();
            UpdateViewport3D();
        }

        private void RibbonApplicationMenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            scene = new();

//            Disc disc = new(scene, new Point(0, 0, 0), new Vector(0, 1, 0), 25);
            Sphere sphere = new(scene, new Point(0, 0, 0), 1);

            PointLight pointLight = new(scene, new Point(0, 10, 0), 100, Palette.White);
            scene.AddLight(pointLight);

            var skyBox = new SkydomeGradient(new Colour(0f, 0f, 1f), new Colour(0f, 1f, 0f));
            scene.Skydome = skyBox;

            UpdateForScene();
            UpdateViewport3D();
        }

        private void EntityTreeView_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                TreeViewEntityModel model = entityTreeView.SelectedItem as TreeViewEntityModel;
                if (model != null)
                {
                    Entity entity = model.Instance;
                    if (entity != null)
                    {
                        //TreeViewEntityModel item = ((sender as MenuItem).DataContext) as TreeViewEntityModel;
                        //item.FindDirectParent(CategoriesList).Add(new CustomTreeItem() { ItemName = "New Item" });
                        switch (menuItem.Header)
                        {
                            case "Delete":
                                if (entity is Light light)
                                {
                                    _ = scene.RemoveLight(light);
                                }
                                else if (entity is Shape shape)
                                {
                                    _ = scene.RemoveShape(shape);
                                }
                                break;
                            default:
                                break;
                        }

                        UpdateEntities();
                        UpdateEntityTreeView();
                        UpdateViewport3D();
                    }
                }
            }
        }

        private static DependencyObject
            SearchTreeView<T>(DependencyObject source)
        {
            while (source != null && source.GetType() != typeof(T))
            {
                source = VisualTreeHelper.GetParent(source);
            }
            return source;
        }

        private void entityTreeView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem treeViewItem = (TreeViewItem)SearchTreeView<TreeViewItem>((DependencyObject)e.OriginalSource);
            if (treeViewItem != null)
            {
                treeViewItem.IsSelected = true;
                e.Handled = true;
            }
        }

        private void materialContextMenuEdit_Click(object sender, RoutedEventArgs e)
        {
            MaterialTile materialTile = (MaterialTile)materialListBox.SelectedItem;
            if (materialTile != null)
            {
                EditMaterial(materialTile);
            }
        }

        private void materialContextMenuDelete_Click(object sender, RoutedEventArgs e)
        {
            MaterialTile tile = (MaterialTile)materialListBox.SelectedItem;
            if (tile != null)
            {
                scene.RemoveMaterial(tile.Material);
                _ = MaterialTiles.Remove(tile);
            }
        }

        private void entityTreeView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            TreeView treeView = (TreeView)sender;
            if (treeView != null)
            {
                TreeViewEntityModel entityModel = (TreeViewEntityModel)treeView.SelectedItem;
                if (entityModel != null)
                {
                    ContextMenu contextMenu = treeView.ContextMenu;
                    if (contextMenu != null)
                    {
                        // Only show context menu for Shapes or Lights.
                        if (entityModel.Instance is not (Shape or Light))
                        {
                            e.Handled = true;
                        }
                    }
                }
            }
        }

        private void materialListBox_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            ListBox listBox = sender as ListBox;
            if (listBox != null)
            {
                ContextMenu contextMenu = listBox.ContextMenu;
                ItemCollection itemCollection = contextMenu.Items;

                MaterialTile materialTile = listBox.SelectedItem as MaterialTile;
                bool itemSelected = materialTile != null;

                for (int i = 0; i < itemCollection.Count; i++)
                {
                    MenuItem menuItem = (MenuItem)itemCollection.GetItemAt(i);
                    string header = menuItem.Header as string;
                    if (header == "Edit" || header == "Delete")
                    {
                        menuItem.IsEnabled = itemSelected;
                    }
                }
            }
        }

        private void materialContextMenuNew_Click(object sender, RoutedEventArgs e)
        {
            AddMaterial();
        }

        public GeometryModel3D HitTestGeometry(System.Windows.Point mousePos)
        {
            List<GeometryModel3D> hitResults = new();

            //GeometryModel3D model;

            HitTestResultBehavior HitTestCallback(HitTestResult hitTestResult)
            {
                if (hitTestResult is RayHitTestResult results)
                {
                    if (results is RayMeshGeometry3DHitTestResult meshResults)
                    {
                        GeometryModel3D hitGeometry = meshResults.ModelHit as GeometryModel3D;

                        hitResults.Add(hitGeometry);

                        /*
                        Entity entity = preview3D.GetEntityFromGeometry(hitGeometry);
                        if (entity != null)
                        {
                            SelectedEntity.Clear();
                            SelectedEntity.Add(entity);

                            UpdateEntityTreeView();

                            return HitTestResultBehavior.Stop;
                        }
                        */
                    }
                }
                return HitTestResultBehavior.Continue;
            }

            Point3D testPoint3D = new Point3D(mousePos.X, mousePos.Y, 0);
            Vector3D testDirection3D = new Vector3D(mousePos.X, mousePos.Y, 10);
            PointHitTestParameters hitTestParams = new(mousePos);
            RayHitTestParameters rayParams = new RayHitTestParameters(testPoint3D, testDirection3D);

            VisualTreeHelper.HitTest(viewport3D, null, HitTestCallback, hitTestParams);

            if (hitResults.Count > 0) { return hitResults[0];  }

            return null;
        }
/*
        private HitTestResultBehavior ViewportHitResult(HitTestResult hitTestResult)
        {
            if (hitTestResult is RayHitTestResult results)
            {
                if (results is RayMeshGeometry3DHitTestResult meshResults)
                {
                    GeometryModel3D hitGeometry = meshResults.ModelHit as GeometryModel3D;

                    Entity entity = preview3D.GetEntityFromGeometry(hitGeometry);
                    if (entity != null)
                    {
                        SelectedEntity.Clear();
                        SelectedEntity.Add(entity);

                        UpdateEntityTreeView();

                        return HitTestResultBehavior.Stop;
                    }
                }
            }
            return HitTestResultBehavior.Continue;
        }
*/
        private void viewport3D_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _inputMode.DoLButtonDown(e.GetPosition(viewport3D));
            return;
/*
            System.Windows.Point mousePos = e.GetPosition(viewport3D);
            Point3D testPoint3D = new Point3D(mousePos.X, mousePos.Y, 0);
            Vector3D testDirection3D = new Vector3D(mousePos.X, mousePos.Y, 10);
            PointHitTestParameters hitTestParams = new(mousePos);
            RayHitTestParameters rayParams = new RayHitTestParameters(testPoint3D, testDirection3D);

            VisualTreeHelper.HitTest(viewport3D, null, ViewportHitResult, hitTestParams);
 */       }

        private void ButtonSkybox_Click(object sender, RoutedEventArgs e)
        {
            SkyboxSelectDlg dialog = new();
            dialog.Owner = this;

            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                SkyboxSelectDlg.SkyboxEntry skybox = dialog.Selection;

                if (skybox.Name != null)
                {
                    scene.Skydome = new Skybox(System.IO.Path.Combine("Data/Skyboxes/", skybox.Name));

                    UpdateEntities();
                    UpdateEntityTreeView();
                }
            }

            /*
                        OpenFileDialog dialog = new OpenFileDialog() { Filter = "Image Files | *.bmp;*.jpg;*.jpeg;*.png;*.tiff", FileName = "" };
                        if (dialog.ShowDialog() == true)
                        {
                            string fileName = dialog.FileName;

                            scene.Skydome = new Skybox(fileName);
                        }
            */
        }

        //
        // Handle changes to VIew mode.
        //
        private void TabCtrlView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is TabControl tc)
            {
                Vector3D up;
                up.X = 0;
                up.Y = 1;
                up.Z = 0;

                switch (tc.SelectedIndex)
                {
                    case 0: // 3D
                        {
                            Point cameraPos = scene.Camera.Pos;
                            Vector cameraView = scene.Camera.GetViewVector().Normalise();

                            camera3D.Position = new Point3D(cameraPos.X, cameraPos.Y, -cameraPos.Z);
                            camera3D.LookDirection = new Vector3D(cameraView.I, cameraView.J, -cameraView.K);
                            camera3D.UpDirection = new Vector3D(0.0, 1.0, 0.0);
                            camera3D.FieldOfView = scene.Camera.FovH;

                            //camera3D.NearPlaneDistance = 0;
                            camera3D.FarPlaneDistance = 1000;

                            viewport3D.Camera = camera3D;   // new PerspectiveCamera(new Point3D(0, 0, 6), new Vector3D(0, 0, -1), up, 45);

                            _inputMode = new InputMode3D(this, viewport3D, camera3D, scene);

                            UpdateViewport3D();
                        }
                        break;
                    case 1: // Front
                        viewport3D.Camera = cameraFront;
                        _inputMode = new InputModeFront(cameraFront);
                        break;
                    case 2: // Top
                        viewport3D.Camera = cameraTop;
                        _inputMode = new InputModeTop(cameraTop);
                        break;
                    case 3: // Left
                        viewport3D.Camera = cameraLeft;
                        _inputMode = new InputModeLeft(cameraLeft);
                        break;
                    default:
                        break;
                }
            }
        }

        private void window_Closed(object sender, EventArgs e)
        {
            Logger.Instance.Dispose();
        }

        // XXX Probably temporary, preview3d moving to PreviewVisual3D/
        //public Entity GetEntityFromGeometry(GeometryModel3D geometry)
        //{
        //    return preview3D.GetEntityFromGeometry(geometry);
        //}
    }
}
