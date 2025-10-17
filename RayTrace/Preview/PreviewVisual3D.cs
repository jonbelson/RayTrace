using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Converters;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace RayTrace.Preview
{
    //
    // Represent an XYZ axis.
    //
    public class ModelAxis3D : ModelVisual3D
    {
        //Point3D MakePoint(int i) { return new Point3D(box3D[i, 0], box3D[i, 1], box3D[i, 2]); }

        static private Point3D origin = new Point3D(0, 0, 0);
        static private Point3D x = new (1, 0, 0);
        static private Point3D y = new(0, 1, 0);
        static private Point3D z = new(0, 0, -1);

        private static readonly double Thickness = 8;

        public ModelAxis3D(SPoint origin)
        {
            CreateModelAxis3D();

            Transform3DGroup transformGroup = new();

            var translate = new TranslateTransform3D(origin.X, origin.Y, -origin.Z);

//var scale = new ScaleTransform3D(10, 10, 10);
            //transformGroup.Children.Add(scale);
            transformGroup.Children.Add(translate);

            Transform = transformGroup;

        }

        public void CreateModelAxis3D()
        {
            static Point3DCollection CreateLine(Point3D p1, Point3D p2) { return new Point3DCollection(new List<Point3D>() { p1, p2 }); }

            _3DTools.ScreenSpaceLines3D line = new()
            {
                Points = CreateLine(new(0, 0, 0), new(1, 0, 0)),
                Thickness = Thickness,
                Color = Colors.Red
            };

            this.Children.Add(line);

            line = new()
            {
                Points = CreateLine(new(0, 0, 0), new(0, 1, 0)),
                Thickness = Thickness,
                Color = Colors.Green
            };

            this.Children.Add(line);

            line = new()
            {
                Points = CreateLine(new(0, 0, 0), new(0, 0, -1)),
                Thickness = Thickness,
                Color = Colors.Blue
            };

            this.Children.Add(line);

        }
    };

    //
    // Represent an axis-aligned bounding box.
    //
    public class BoundingBox3D : ModelVisual3D
    {
        private GeometryModel3D CreateBoundingBox()
        {
            double[,] box3D = new double[8, 3]
            {
                // Top
                { -0.5, 0.5, 0.5 },
                { 0.5, 0.5, 0.5 },
                { 0.5, 0.5, -0.5 },
                { -0.5, 0.5, -0.5 },

                // Bottom
                { -0.5, -0.5, 0.5 },
                { 0.5, -0.5, 0.5 },
                { 0.5, -0.5, -0.5 },
                { -0.5, -0.5, -0.5 }
            };

            int[,] boxEdgesX = new int[4, 2]
            {
                { 0, 1 }, { 2, 3 }, { 4, 5 }, { 6, 7 },     // X axis aligned.
            };

            int[,] boxEdgesY = new int[4, 2]
            {
                { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 },     // Y axis aligned.
            };

            int[,] boxEdgesZ = new int[4, 2]
            {
                { 0, 3 }, { 1, 2 }, { 4, 7 }, { 5, 6 }     // Z axis aligned.
            };

            Point3D MakePoint(int i) { return new Point3D(box3D[i, 0], box3D[i, 1], box3D[i, 2]); }

            Func<Point3D, Point3D, Point3DCollection> CreateLine = delegate (Point3D p1, Point3D p2) { return new Point3DCollection(new List<Point3D>() { p1, p2 }); };

            // Aligned with X axis.
            for (int i = 0; i < 4; i++)
            {
                int p1 = boxEdgesX[i, 0];
                int p2 = boxEdgesX[i, 1];

                _3DTools.ScreenSpaceLines3D line = new();
                line.Points = CreateLine(MakePoint(p1), MakePoint(p2));
                line.Thickness = 2;
                line.Color = Colors.Red;

                this.Children.Add(line);
                //viewport3D.Children.Add(line);
            }

            // Aligned with Y axis.
            for (int i = 0; i < 4; i++)
            {
                int p1 = boxEdgesY[i, 0];
                int p2 = boxEdgesY[i, 1];

                _3DTools.ScreenSpaceLines3D line = new();
                line.Points = CreateLine(MakePoint(p1), MakePoint(p2));
                line.Thickness = 2;
                line.Color = Colors.Green;

                this.Children.Add(line);
//                viewport3D.Children.Add(line);
            }

            // Aligned with Z axis.
            for (int i = 0; i < 4; i++)
            {
                int p1 = boxEdgesZ[i, 0];
                int p2 = boxEdgesZ[i, 1];

                _3DTools.ScreenSpaceLines3D line = new();
                line.Points = CreateLine(MakePoint(p1), MakePoint(p2));
                line.Thickness = 2;
                line.Color = Colors.Blue;

                this.Children.Add(line);
//                viewport3D.Children.Add(line);
            }

            return null;
        }

        public BoundingBox3D(float sx, float sy, float sz, SPoint p)
        {
            CreateBoundingBox();

            var scale = new ScaleTransform3D(sx, sy, sz);
            var translate = new TranslateTransform3D(p.X, p.Y, -p.Z);
            Transform3DGroup transformGroup = new();

            transformGroup.Children.Add(scale);
            //transformGroup.Children.Add(new RotateTransform3D(axisRotationX));
            //transformGroup.Children.Add(new RotateTransform3D(axisRotationY));
            //transformGroup.Children.Add(new RotateTransform3D(axisRotationZ));
            transformGroup.Children.Add(translate);

            Transform = transformGroup;

        }

        public BoundingBox3D()
        {
            CreateBoundingBox();
        }

        public BoundingBox3D(SPoint min, SPoint max)
        {
            CreateBoundingBox();

            float dx = max.X - min.X;
            float dy = max.Y - min.Y;
            float dz = max.Z - min.Z;

            SPoint mid = new((min.X + max.X)/2.0f, (min.Y + max.Y)/2.0f, (min.Z + max.Z)/2.0f);

            var scale = new ScaleTransform3D(dx/1.0f, dy/1.0f, dz/1.0f);
            //var translate = new TranslateTransform3D(mid.X, mid.Y/2.0f, -mid.Z/2.0f);

            float tx = min.X + dx/2.0f;
            float ty = min.Y + dy/2.0f;
            float tz = min.Z + dz/2.0f;

            var translate = new TranslateTransform3D(tx, ty, -tz);
            Transform3DGroup transformGroup = new();

            transformGroup.Children.Add(scale);
            transformGroup.Children.Add(translate);

            Transform = transformGroup;

        }
    }

    internal class BiDiMap<TFirst, TSecond>
    {
        private Dictionary<TFirst, TSecond> firstToSecond = new();
        private Dictionary<TSecond, TFirst> secondToFirst = new();

        public BiDiMap() { }

        public void Clear() { firstToSecond.Clear(); secondToFirst.Clear();  }

        public void Add(TFirst first, TSecond second) { firstToSecond[first] = second; secondToFirst[second] = first; }

        public TSecond Lookup(TFirst first) { return firstToSecond[first]; }
        public TFirst Lookup(TSecond second) { return secondToFirst[second]; }

        public bool ContainsKey(TFirst first)
        {
            return firstToSecond.ContainsKey(first);
        }

        public bool ContainsKey(TSecond second)
        {
            return secondToFirst.ContainsKey(second);
        }

        public bool Remove(TFirst first)
        {
            if (first == null) throw new ArgumentNullException("first");

            if (firstToSecond.ContainsKey(first))
            {
                TSecond second = firstToSecond[first];
                secondToFirst.Remove(second);
                firstToSecond.Remove(first);

                return true;
            }

            return false;
        }

        public bool Remove(TSecond second)
        {
            if (second == null) throw new ArgumentNullException("second");

            if (secondToFirst.ContainsKey(second))
            {
                TFirst first = secondToFirst[second];
                firstToSecond.Remove(first);
                secondToFirst.Remove(second);

                return true;
            }

            return false;
        }
    };

    //
    // Create the Geometry required to depict a Scene.
    // Each Shape is a separate child geometry.
    //
    public class PreviewVisual3D : ModelVisual3D
    {
        private Scene scene;

        private Model3DGroup group = new();

        //private Dictionary<GeometryModel3D, Entity> models = new();
        private BiDiMap<GeometryModel3D, Entity> models = new();

        //private GeometryModel3D boundingBox;

        // Utility delagates for converting between formats.
        private readonly Func<Point, Point3D> PointToPoint3D = (p) => new Point3D(p.X, p.Y, p.Z);
        private readonly Func<Vector, Vector3D> VectorToVector3D = (v) => new Vector3D(v.I, v.J, v.K);
        private readonly Func<Colour, System.Windows.Media.Color> ColourToColor = (c) => new System.Windows.Media.Color() { ScR = c.Red, ScG = c.Green, ScB = c.Blue };

        public bool UseAmbient { get; set; } = true;

        //
        // Constructor.
        //
        public PreviewVisual3D(Scene scene)
        {
            this.scene = scene;

            group.Transform = new ScaleTransform3D(1.0, 1.0, -1.0);

            this.Content = group;

            //UpdateBoundingBox();
        }

        public Entity GetEntityFromGeometry(GeometryModel3D geometry)
        {
            if (models.ContainsKey(geometry))
            {
                return models.Lookup(geometry);
            }
            return null;
        }

        // Update geometry for all Shapes
        public void Update()
        {
            models.Clear();

            foreach (Shape shape in scene.Shapes)
            {
                Update(shape);
            }

            foreach (Light light in scene.Lights)
            {
                Update(light);
            }

            if (UseAmbient)
            {
                AmbientLight al = new(ColourToColor(scene.Ambient));
                group.Children.Add(al);
            }
        }

        public void Update(Light light)
        {
            // Handy lambda expressions for converting to Wpf format.
            //Func<Point, Point3D> PointToPoint3D = (p) => new Point3D(p.X, p.Y, p.Z);
            //Func<Vector, Vector3D> VectorToVector3D = (v) => new Vector3D(v.I, v.J, v.K);
            //Func<Colour, System.Windows.Media.Color> ColourToColor = (c) => new System.Windows.Media.Color() { ScR = c.Red, ScG = c.Green, ScB = c.Blue };

            // Get rid of the old geometry.
            if (models.ContainsKey(light))
            {
                var oldGeometry = models.Lookup(light);
                if (oldGeometry != null)
                {
                    group.Children.Remove(oldGeometry);
                }
            }

            models.Remove(light);

            GeometryModel3D geometryModel = null;

            if (light.Enabled)
            {
                if (light is PointLight pointLight)
                {
                    //Sphere sphere = new Sphere(scene, new Point(1, 1, 1), 2);
                    //geometryModel = GeometryModel3DFactory.MakeSphere(sphere);
                    geometryModel = GeometryModel3DFactory.MakePointLight(pointLight);
                    //Colour c = sphere.Material.GetBaseColour();
                    //geometryModel.Material = new DiffuseMaterial(new SolidColorBrush(Color.FromScRgb(1, c.Red, c.Green, c.Blue)));

                    group.Children.Add(geometryModel);

                    System.Windows.Media.Media3D.PointLight pl = new(ColourToColor(pointLight.Output), PointToPoint3D(pointLight.Pos));

                    // Attenuation formula based on distance (D):
                    // ConstantAttenuation + LinearAttenuation * D + QuadraticAttenuation * D^2
                    pl.QuadraticAttenuation = 0.001;
                    //l.LinearAttenuation = 0.01;

                    group.Children.Add(pl);
                }
                else if (light is DistantLight distantLight)
                {
                    System.Windows.Media.Media3D.DirectionalLight dl = new(ColourToColor(distantLight.Output), VectorToVector3D(distantLight.Normal));

                    group.Children.Add(dl);
                }
            }

            if (geometryModel != null)
            {
                Colour c = light.Colour;
                //geometryModel.Material = new DiffuseMaterial(new SolidColorBrush(Color.FromScRgb(1, c.Red, c.Green, c.Blue)));

                MaterialGroup materialGroup = new();
                materialGroup.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromScRgb(1, c.Red, c.Green, c.Blue))));
                materialGroup.Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromScRgb(1, c.Red, c.Green, c.Blue))));

                geometryModel.Material = materialGroup;
                models.Add(geometryModel, light);
            }
        }

        // Update geometry for a single Shape
        public void Update(Shape shape)
        {
            // Get rid of the old geometry.
            if (models.ContainsKey(shape))
            {
                var oldGeometry = models.Lookup(shape);
                if (oldGeometry != null)
                {
                    group.Children.Remove(oldGeometry);
                }
            }

            models.Remove(shape);

            GeometryModel3D geometryModel = null;

            if (shape.Enabled)
            {
                if (shape is Sphere sphere)
                {
                    geometryModel = GeometryModel3DFactory.MakeSphere(sphere);

                    group.Children.Add(geometryModel);
                }
                else if (shape is Plane plane)
                {
                    geometryModel = GeometryModel3DFactory.MakePlane(plane);

                    group.Children.Add(geometryModel);
                }
                else if (shape is Disc disc)
                {
                    geometryModel = GeometryModel3DFactory.MakeDisc(disc);

                    group.Children.Add(geometryModel);
                }
                else if (shape is Model model)
                {
                    geometryModel = GeometryModel3DFactory.MakeModel(model);

                    group.Children.Add(geometryModel);
                }
            }

            if (geometryModel != null)
            {
                Colour c = shape.Material.GetBaseColour();
                geometryModel.Material = new DiffuseMaterial(new SolidColorBrush(Color.FromScRgb(1, c.Red, c.Green, c.Blue)));

                if (shape is Disc or Plane or Model)
                {
                    geometryModel.BackMaterial = geometryModel.Material;
                }

                models.Add(geometryModel, shape);
            }
        }
    }
}

