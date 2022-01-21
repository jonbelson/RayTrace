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

        public float t;

        // Texture cordinates.
        public float u;
        public float v;

        // Optional value used by certain Shapes (e.g. triangle that was hit in a Model).
        public int Index { get; set; }

        public void Reset()
        {
            t = 0;
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
            t = 0;
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

        private bool updateMatrix = false;

        private float scale = 1.0f;

        private float rotateX = 0.0f;
        private float rotateY = 0.0f;
        private float rotateZ = 0.0f;

        private static float DegreesToRadians(float degrees) => (float)(degrees / 360.0 * 2.0 * Math.PI);

        public Point Pos { get { return pos; } set { pos = value; Notify("Pos"); } }

        public float Scale
        {
            get { return scale; }
            set { if (scale != value) { scale = value; Notify("Scale"); updateMatrix = true; } }
        }

        // Clockwise rotation around X axis in degrees.
        public float RotateX {
            get { return rotateX; }
            set {
                if (rotateX != value) {
                    rotateX = value;
                    //rotateXRadians = DegreesToRadians(rotateXDegrees);
                    updateMatrix = true;
                    Notify("RotateX");
                }
            }
        }

        public float RotateY
        {
            get { return rotateY; }
            set
            {
                if (rotateY != value)
                {
                    rotateY = value;
                    //rotateYRadians = DegreesToRadians(rotateYDegrees);
                    updateMatrix = true;
                    Notify("RotateY");
                }
            }
        }

        public float RotateZ
        {
            get { return rotateZ; }
            set
            {
                if (rotateZ != value)
                {
                    rotateZ = value;
                    //rotateZRadians = DegreesToRadians(rotateZ);
                    updateMatrix = true;
                    Notify("RotateZ");
                }
            }
        }

        public Material Material { get; set; }

        //public Guid MaterialGuid { get; set; }

        private Matrix _objectToWorld = new Matrix();
        private Matrix _objectToWorldNormal = new Matrix();
        public Matrix ObjectToWorld {
            get {
                if (updateMatrix)
                {
                    UpdateMatrix();
                    updateMatrix = false;
                }
                return _objectToWorld;
            }
/*            set {
                _objectToWorld = value;
                _objectToWorldNormal = new Matrix(_objectToWorld)
                {
                    // Remove translation component.
                    M14 = 0.0f,
                    M24 = 0.0f,
                    M34 = 0.0f,
                };
            }*/
        }

        public Matrix ObjectToWorldNormal {
            get {
                if (updateMatrix)
                {
                    UpdateMatrix();
                    updateMatrix = false;
                }
                return _objectToWorldNormal;
            }
        }

        public bool HasRoughness() => Material.HasRoughness();
        public bool HasTransparency() => Material.HasTransparency();
        public bool HasReflectivity() => Material.HasReflectivity();

        private void UpdateMatrix()
        {
            float rotateXRadians = DegreesToRadians(rotateX);
            float rotateYRadians = DegreesToRadians(rotateY);
            float rotateZRadians = DegreesToRadians(rotateZ);

            _objectToWorld = Matrix.GetScale(scale) * Matrix.GetRotationZ(rotateZRadians) * Matrix.GetRotationY(rotateYRadians) * Matrix.GetRotationX(rotateXRadians);
            _objectToWorldNormal = new Matrix(_objectToWorld)
            {
                // Remove translation component.
                M14 = 0.0f,
                M24 = 0.0f,
                M34 = 0.0f,
            };
        }

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
            Scale = xmlShape.Scale;
            RotateX = xmlShape.RotateX;
            RotateY = xmlShape.RotateY;
            RotateZ = xmlShape.RotateZ;

            //Material = new Material(xmlShape.Material);
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
                xmlShape.Scale = Scale;
                xmlShape.RotateX = RotateX;
                xmlShape.RotateY = RotateY;
                xmlShape.RotateZ = RotateZ;
                xmlShape.MaterialGuid = Material.Guid;// ToXML();
            }
        }

        public override XMLEntity ToXML()
        {
            XMLShape xml = new();
            AsXMLImpl(xml);
            return xml;
        }
    }
}
