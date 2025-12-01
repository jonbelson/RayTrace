using System;
using System.ComponentModel;
using System.Dynamic;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

using RayTrace.Preview;

namespace RayTrace
{
    // Result of an intersection test.
    public struct HitResult
    {
        public Shape shape;

        private SPoint _entry;
        private bool _hasEntry;// = false;

        public readonly bool HasEntry() => _hasEntry;

        public SPoint Entry { get => _entry; set { _entry = value; _hasEntry = true; } }
        public SVector EntryNormal;  // Normal vector at intersetion point.

        // Ray that resulted in this HitResult.
        public SVector Ray { get; set; }

        public float t;

        // Texture cordinates.
        public float u;
        public float v;

        // Barycentric coordinates.
        public float bu;
        public float bv;
        public float bw;

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
            bu = bv = bw = 0.0f;
            _entry = new SPoint();
            _hasEntry = false;
            Ray = new SVector();
            EntryNormal = new SVector();
            Index = 0;
        }
    }

    // Base class for objects.
    public abstract class Shape : Entity
    {
        protected double noiseRange = Perlin.GetRange();

        private Point pos;

        protected bool updateMatrix = false;

        private float scale = 1.0f;

        private float rotateX;
        private float rotateY;
        private float rotateZ;

        private static float DegreesToRadians(float degrees) => (float)(degrees / 360.0 * 2.0 * Math.PI);

        public Point Pos { get => pos; set { pos = value; Notify("Pos"); updateMatrix = true; } }

        public float Scale
        {
            get => scale;
            set { if (scale != value) { scale = value; Notify("Scale"); updateMatrix = true; } }
        }

        // Clockwise rotation around X axis in degrees.
        public float RotateX {
            get => rotateX;
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
            get => rotateY;
            set
            {
                if (rotateY != value)
                {
                    rotateY = value;
                    updateMatrix = true;
                    Notify("RotateY");
                }
            }
        }

        public float RotateZ
        {
            get => rotateZ;
            set
            {
                if (rotateZ != value)
                {
                    rotateZ = value;
                    updateMatrix = true;
                    Notify("RotateZ");
                }
            }
        }

        //public Guid MaterialGuid { get; set; }
        private Material material;
        public Material Material {
            get => material;
            set
            {
                material = value;
                Scene.AddMaterial(material);
            }
        }

        public virtual Material GetMaterial(HitResult hr)
        {
            return Material;
        }

        private Matrix _objectToWorld = new();
        private Matrix _objectToWorldNormal = new();
        public Matrix ObjectToWorld {
            get
            {
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

        public bool HasMaterial() => Material != null;
        public bool HasRoughness() => HasMaterial() && Material.HasRoughness();
        public bool HasTransparency() => HasMaterial() && Material.HasTransparency();
        public bool HasReflectivity() => HasMaterial() && Material.HasReflectivity();
        public bool HasNormalMap() => HasMaterial() && Material.HasNormalMap();

        private void UpdateMatrix()
        {
            float rotateXRadians = DegreesToRadians(rotateX);
            float rotateYRadians = DegreesToRadians(rotateY);
            float rotateZRadians = DegreesToRadians(rotateZ);

//            _objectToWorld = Matrix.GetScale(scale) * Matrix.GetRotationZ(rotateZRadians) * Matrix.GetRotationY(rotateYRadians) * Matrix.GetRotationX(rotateXRadians) * Matrix.GetTranslation(Pos.X, Pos.Y, Pos.Z);
            _objectToWorld = Matrix.GetTranslation(Pos.X, Pos.Y, Pos.Z) * Matrix.GetRotationZ(rotateZRadians) * Matrix.GetRotationY(rotateYRadians) * Matrix.GetRotationX(rotateXRadians) * Matrix.GetScale(scale);

            _objectToWorldNormal = Matrix.GetRotationZ(rotateZRadians) * Matrix.GetRotationY(rotateYRadians) * Matrix.GetRotationX(rotateXRadians);

            /*
            _objectToWorldNormal = new Matrix(_objectToWorld)
            {
                // Remove translation component.
                M14 = 0.0f,
                M24 = 0.0f,
                M34 = 0.0f,
            };
            */
        }

        //
        // Constructor.
        //
        public Shape(Scene scene, Point p)
        {
            Scene = scene;
            Pos = p;

            Name = scene.GetUniqueShapeName(GetType());

            Material = scene.GetDefaultMaterial();

            scene.AddShape(this);
        }

        //
        // Constructor.
        //
        public Shape(Scene scene, Point p, Colour c)
        {
            Scene = scene;
            Pos = p;

            Name = scene.GetUniqueShapeName(GetType());

            Material = new Material(c);
            scene.AddMaterial(Material);

            scene.AddShape(this);
        }

        // Constructor.
/*        public Shape(Scene scene, Point p, Material material)
        {
            Scene = scene;
            Pos = p;
            Material = material;
        }
*/

        //
        // Constructor.
        //
        public Shape(Scene scene, XMLShape xmlShape)
        {
            Scene = scene;

            base.AsXMLImpl(xmlShape);

            Name = xmlShape.Name;

            Pos = new Point(xmlShape.Pos);
            Scale = xmlShape.Scale;
            RotateX = xmlShape.RotateX;
            RotateY = xmlShape.RotateY;
            RotateZ = xmlShape.RotateZ;

            //Material = new Material(xmlShape.Material);

            scene.AddShape(this);
        }

        // Get colour at hit point.
        // Default implementation just returns the Material's colour at that point.
        public virtual Colour GetColour(HitResult hr)
        {
            return Material.GetColour(hr);
        }

        // Get colour at hit point.
        // Default implementation just returns the Material's colour at that point.
        public virtual SColour GetSColour(HitResult hr)
        {
            return GetColour(hr).ToSColour();
        }

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

        public virtual (Point, Point) GetBoundingBox()
        {
            return (new Point(), new Point());
        }

        public virtual BoundingBox3D GetBoundingBox2()
        {
            BoundingBox3D bb = new();

            var axisRotationX = new AxisAngleRotation3D(new Vector3D(1, 0, 0), RotateX);
            var axisRotationY = new AxisAngleRotation3D(new Vector3D(0, 1, 0), RotateY);
            var axisRotationZ = new AxisAngleRotation3D(new Vector3D(0, 0, 1), RotateZ);
            var scale = new ScaleTransform3D(Scale, Scale, Scale);
            var translate = new TranslateTransform3D(Pos.X, Pos.Y, Pos.Z);

            Transform3DGroup transformGroup = new();

            transformGroup.Children.Add(scale);
            transformGroup.Children.Add(new RotateTransform3D(axisRotationX));
            transformGroup.Children.Add(new RotateTransform3D(axisRotationY));
            transformGroup.Children.Add(new RotateTransform3D(axisRotationZ));
            transformGroup.Children.Add(translate);

            bb.Transform = transformGroup;

            return bb;
        }
    }
}
