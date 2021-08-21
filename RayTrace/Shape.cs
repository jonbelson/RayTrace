using System;
using System.ComponentModel;
using System.Dynamic;

namespace RayTrace
{
    // Result of an intersection test.
    public struct HitResult
    {
        public Shape shape;

        private SPoint _entry;
        private bool _hasEntry;// = false;

        public bool HasEntry () => _hasEntry;

        public SPoint Entry { get { return _entry; } set { _entry = value; _hasEntry = true; } }
        public SVector EntryNormal;  // Normal vector at intersetion point.

        // Ray that resulted in this HitResult.
        public SVector Ray { get; set; }

        // Texture cordinates.
        public float u;
        public float v;

        // Optional value used by certain Shapes (e.g. triangle that was hit in a Model).
        public int Index { get; set; }

        public void Reset()
        {
            u = v = 0.0f;
            _entry = new SPoint();
            _hasEntry = false;
            Ray = new SVector();
            EntryNormal = new SVector();
            Index = 0;
        }

        public HitResult(Shape shape)
        {
            this.shape = shape;
            u = v = 0.0f;
            _entry = new SPoint();
            _hasEntry = false;
            Ray = new SVector();
            EntryNormal = new SVector();
            Index = 0;
        }
    }

    // Base class for objects.
    abstract public class Shape : Entity
    {
        protected double noiseRange = Perlin.GetRange();

        private Point pos;

        public Point Pos { get { return pos; } set { pos = value; Notify("Pos"); } }
        public Material Material { get; set; }

        //public Guid MaterialGuid { get; set; }

        private Matrix _objectToWorld = new Matrix();
        private Matrix _objectToWorldNormal = new Matrix();
        public Matrix ObjectToWorld {
            get { return _objectToWorld; }
            set {
                _objectToWorld = value;
                _objectToWorldNormal = new Matrix(_objectToWorld)
                {
                    // Remove translation component.
                    M14 = 0.0f,
                    M24 = 0.0f,
                    M34 = 0.0f,
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

        // Constructor.
        public Shape(XMLShape xmlShape)
        {
            Pos = new Point(xmlShape.Pos);
            Material = new Material(xmlShape.Material);
        }

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

        public virtual (SPoint, SVector) GetRefractedVector(Camera camera, SVector ray, HitResult hr) { return (hr.Entry, ray); }

        protected override void AsXMLImpl(XMLEntity xml)
        {
            base.AsXMLImpl(xml);

            if (xml is XMLShape xmlShape)
            {
                xmlShape.Pos = Pos.ToXML();
                xmlShape.Material = Material.ToXML();
            }
        }

        public override XMLEntity ToXML()
        {
            XMLShape xml = new XMLShape();
            AsXMLImpl(xml);
            return xml;
        }
    }
}
