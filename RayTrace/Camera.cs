using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    public class Camera : Entity
    {
        private Point _pos;// = new Point(0, 0, 0);
        private Point _target;// = new Point(0, 0, 1.0f);

        private float _fovHRadians;
        private float _fovHDegrees;

        public Point Pos {
            get { return _pos; }
            set {
                _pos = value;
                UpdateMatrix();
                Pos.PropertyChanged += Pos_PropertyChanged;
                Notify("Pos");
            }
        }
        public Point Target {
            get { return _target; }
            set {
                _target = value;
                UpdateMatrix();
                Target.PropertyChanged += Target_PropertyChanged;
                Notify("Target");
            }
        }

        public float DepthOfField { get; set; } = 10.0f;
        public float Aperture { get; set; } = 30.0f;

        public float FovH
        {
            get => _fovHDegrees;
            set
            {
                _fovHDegrees = value;
                _fovHRadians = (value / 180.0f) * MathF.PI;
                Notify("FovH");
            }
        }

        // Matrix for World to View transform.
        public Matrix WorldToView { get; set; } = new Matrix();

        // Matrix for transforming World normals to View.
        public Matrix NormalMatrix { get; set; } = new Matrix();

        /*private*/ public void UpdateMatrix()
        {
            WorldToView.SetLookAt(_pos, _target);

            NormalMatrix = new Matrix(WorldToView);
            NormalMatrix.M14 = 0.0f;
            NormalMatrix.M24 = 0.0f;
            NormalMatrix.M34 = 0.0f;

            // XXX For transforming surface normals, we need to use the transverse of the inverse of the WorldToView matrix.
        }

        // Constructor.
        public Camera(Point from, Point to)
        {
            //Pos = from;
            //Target = to;
            _pos = from;
            _target = to;

            FovH = 60.0f;

            UpdateMatrix();

            Pos.PropertyChanged += Pos_PropertyChanged;
            Target.PropertyChanged += Target_PropertyChanged;
        }

        private void Target_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Notify("Target");
        }

        private void Pos_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Notify("Pos");
        }

        public Camera(XMLCamera xmlCamera)
        {
            _pos = new Point(xmlCamera.Pos);
            _target = new Point(xmlCamera.Target);
            FovH = xmlCamera.FovH;
            DepthOfField = xmlCamera.DepthOfField;
            Aperture = xmlCamera.Aperture;

            UpdateMatrix();

            Pos.PropertyChanged += Pos_PropertyChanged;
            Target.PropertyChanged += Target_PropertyChanged;
        }

        public float GetSceneWidth()
        {
            return 2.0f * MathF.Tan(_fovHRadians / 2.0f);
        }

        public float GetSceneHeight()
        {
            return 2.0f * MathF.Tan(_fovHRadians / 2.0f);
        }

        // Return Vector from camera position to viewed point.
        public Vector GetViewVector()
        {
            Vector v = Target - Pos;
            return v;//.Normalise();
        }

        public new virtual XMLCamera ToXML()
        {
            return new XMLCamera() { Pos = Pos.ToXML(), Target = Target.ToXML(), Aperture = Aperture, FovH = FovH };
        }

        public override HitResult HitTest(Camera camera, Ray ray)
        {
            return new HitResult(null);
        }
    }
}
