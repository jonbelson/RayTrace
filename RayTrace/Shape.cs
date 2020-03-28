using System;
namespace RayTrace
{
    public class HitResult
    {
        public Shape shape;

        private Point _entry;
        private Point _exit;

        private bool hasEntry = false;
        private bool hasExit = false;

        public bool HasEntry () => hasEntry;
        public bool HasExit() => hasExit;

        public Point Entry { get { return _entry; } set { _entry = value; hasEntry = true; } }
        public Point Exit { get { return _exit; } set { _exit = value; hasExit = true; } }

        public Vector EntryNormal;

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
    abstract public class Shape
    {
        public Point Pos { get; set; }
        public Material Material { get; set; }

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
    }
}
