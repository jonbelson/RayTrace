using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace RayTrace
{
    public class BoundingBox3D : ModelVisual3D
    {
        private GeometryModel3D CreateBoundingBox(Viewport3D viewport3D)
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

            //Model3DGroup group = new();

            //Content = group;

            //GeometryModel3D boundingBox;

            // Aligned with X axis.
            for (int i = 0; i < 4; i++)
            {
                int p1 = boxEdgesX[i, 0];
                int p2 = boxEdgesX[i, 1];

                _3DTools.ScreenSpaceLines3D line = new();
                line.Points = CreateLine(MakePoint(p1), MakePoint(p2));
                line.Thickness = 2;
                line.Color = Colors.Red;

                viewport3D.Children.Add(line);
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

                viewport3D.Children.Add(line);
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

                viewport3D.Children.Add(line);
            }

            return null;
        }

        public BoundingBox3D(Viewport3D viewport3D)
        {
            CreateBoundingBox(viewport3D);
        }
    }


    //
    // Create the Geometry required to depict a Scene.
    // Each Shape is a separate child geometry.
    //
    public class Preview3D : ModelVisual3D
    {
        private Scene scene;

        private Model3DGroup group = new();

        private Dictionary<GeometryModel3D, Entity> models = new();

        //private GeometryModel3D boundingBox;

        // Utility delagates for converting between formats.
        private readonly Func<Point, Point3D> PointToPoint3D = (p) => new Point3D(p.X, p.Y, p.Z);
        private readonly Func<Vector, Vector3D> VectorToVector3D = (v) => new Vector3D(v.I, v.J, v.K);
        private readonly Func<Colour, System.Windows.Media.Color> ColourToColor = (c) => new System.Windows.Media.Color() { ScR = c.Red, ScG = c.Green, ScB = c.Blue };

        public bool UseAmbient { get; set; } = true;

        //
        // Constructor.
        //
        public Preview3D(Scene scene)
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
                return models[geometry];
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

            if (light.Enabled)
            {
                if (light is PointLight pointLight)
                {
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
        }

        // Update geometry for a single Shape
        public void Update(Shape shape)
        {
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

