using System;
namespace RayTrace
{
    public class HitResult
    {
        public Shape shape;

        private Point _entry;
        //private Point _exit;

        private bool hasEntry = false;
        //private bool hasExit = false;

        public bool HasEntry () => hasEntry;
        //public bool HasExit() => hasExit;

        public Point Entry { get { return _entry; } set { _entry = value; hasEntry = true; } }
        //public Point Exit { get { return _exit; } set { _exit = value; hasExit = true; } }

        public Vector EntryNormal;  // Normal vector at intersetion point.

        // Texture cordinates.
        public float u;
        public float v;

        public HitResult(Shape shape)
        {
            this.shape = shape;
            u = v = 0.0f;
        }
    }

    // Base class for objects,
    abstract public class Shape : Entity
    {
        protected double noiseRange = Perlin.GetRange();

        public Point Pos { get; set; }
        public Material Material { get; set; }

        //public float Roughness { get; set; } = 0.0f;
        //public float Transparency { get; set; } = 0.0f;
        //public float Reflectivity { get; set; } = 1.0f;

        public bool HasRoughness() => Material.HasRoughness();
        public bool HasTransparency() => Material.HasTransparency();
        public bool HasReflectivity() => Material.HasReflectivity();

        //public bool HasRoughness() => Roughness > 0.0f;
        //public bool HasTransparency() => Transparency > 0.0f;
        //public bool HasReflectivity() => Reflectivity > 0.0f;

        public Shape(Point p) : this(p, Palette.Red) { }

        public Shape(Point p, Colour c)
        {
            Pos = p;
            Material = new Material(c);
        }

        public Shape(Point p, Material material)
        {
            Pos = p;
            Material = material;
        }

        public Colour GetColour(HitResult hr)
        {
            return Material.GetColour(hr);
        }

        public abstract HitResult HitTest(Point p, Vector ray);
        public abstract HitResult HitTest(Point p1, Point p2, bool ignoreClosest=false);

        public virtual (Point, Vector) GetRefractedVector(Vector ray, HitResult hr) { return (hr.Entry, ray); }
    }
}
