using System;
using System.ComponentModel;
using System.Dynamic;

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

        // Optional value used by certain Shapes (e.g. triangle that was hit in a Model).
        public int index;

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

        [NotifyParentProperty(true)]
        public Point Pos { get; set; }
        public Material Material { get; set; }

        private Matrix _objectToWorld = new Matrix();
        private Matrix _objectToWorldNormal = new Matrix();
        public Matrix ObjectToWorld {
            get { return _objectToWorld; }
            set {
                _objectToWorld = value;
                _objectToWorldNormal = new Matrix(_objectToWorld)
                {
                    // Remove translation component.
                    M14 = 0.0,
                    M24 = 0.0,
                    M34 = 0.0,
                };
            }
        }

        public Matrix ObjectToWorldNormal { get { return _objectToWorldNormal; } }

        public bool HasRoughness() => Material.HasRoughness();
        public bool HasTransparency() => Material.HasTransparency();
        public bool HasReflectivity() => Material.HasReflectivity();

        // Constructor.
        public Shape(Point p) : this(p, Palette.Red) { }

        // Constructor.
        public Shape(Point p, Colour c)
        {
            Pos = p;
            Material = new Material(c);
        }

        // Constructor.
        public Shape(Point p, Material material)
        {
            Pos = p;
            Material = material;
        }

        // Allow Shape to pre-transform data using the Camera's WorldToView Matrix.
        public virtual void CreateCache(Camera camera) { }

        // Get colour at hit point.
        // Default implementation just returns the Material's colour at that point.
        public virtual Colour GetColour(HitResult hr)
        {
            return Material.GetColour(hr);
        }

        public abstract HitResult HitTest(Camera camera, Ray ray);

        // Optimisable version of HitTest() which just returns true or false.
        public virtual bool HitTestQuick(Camera camera, Ray ray)
        {
            HitResult hr = HitTest(camera, ray);
            return hr.HasEntry();
        }

        public virtual (Point, Vector) GetRefractedVector(Camera camera, Vector ray, HitResult hr) { return (hr.Entry, ray); }
    }
}
