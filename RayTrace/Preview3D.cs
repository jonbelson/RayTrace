using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace RayTrace
{
    //
    // Create the Geometry required to depict a Scene.
    // Each Shape is a separate child geometry.
    //
    public class Preview3D : ModelVisual3D
    {
        private Scene scene;

        private Model3DGroup group = new();

        // Utility delagates for converting between formats.
        private readonly Func<Point, Point3D> PointToPoint3D = (p) => new Point3D(p.X, p.Y, p.Z);
        private readonly Func<Vector, Vector3D> VectorToVector3D = (v) => new Vector3D(v.I, v.J, v.K);
        private readonly Func<Colour, System.Windows.Media.Color> ColourToColor = (c) => new System.Windows.Media.Color() { ScR = c.Red, ScG = c.Green, ScB = c.Blue };

        public bool UseAmbient { get; set; } = true;

        public Preview3D(Scene scene)
        {
            this.scene = scene;

            group.Transform = new ScaleTransform3D(1.0, 1.0, -1.0);

            this.Content = group;
        }

        // Update geometry for all Shapes
        public void Update()
        {
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

                if (shape is Disc || shape is Plane)
                {
                    geometryModel.BackMaterial = geometryModel.Material;
                }
            }
        }
    }
}
