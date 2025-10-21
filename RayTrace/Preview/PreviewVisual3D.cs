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
    // Utility class for managing two-way mapping between two types of object.
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
        private readonly Scene scene;

        private readonly Model3DGroup group = new();

        private readonly BiDiMap<GeometryModel3D, Entity> models = new();

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

        //
        // Update geometry for a single Light.
        //
        public void Update(Light light)
        {
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

            // Create new geometry.
            GeometryModel3D geometryModel = null;

            if (light.Enabled)
            {
                if (light is PointLight pointLight)
                {
                    geometryModel = GeometryModel3DFactory.MakePointLight(pointLight);

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

            // Create new geometry.
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

