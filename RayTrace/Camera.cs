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

        private double _fovHRadians;
        private double _fovHDegrees;

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

        public double DepthOfField { get; set; } = 10.0;
        public double Aperture { get; set; } = 30.0;

        public double FovH
        {
            get => _fovHDegrees;
            set
            {
                _fovHDegrees = value;
                _fovHRadians = (value / 180.0) * Math.PI;
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
            NormalMatrix.M14 = 0.0;
            NormalMatrix.M24 = 0.0;
            NormalMatrix.M34 = 0.0;
        }

        // Constructor.
        public Camera(Point from, Point to)
        {
            //Pos = from;
            //Target = to;
            _pos = from;
            _target = to;

            FovH = 60.0;

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

        public double GetSceneWidth()
        {
            return 2.0 * Math.Tan(_fovHRadians / 2.0);
        }

        public double GetSceneHeight()
        {
            return 2.0 * Math.Tan(_fovHRadians / 2.0);
        }

        // Return normalised Vector from camera position to viewed point.
        public Vector GetViewVector()
        {
            Vector v = Target - Pos;
            return v;//.Normalise();
        }

        public new virtual XMLCamera ToXML()
        {
            return new XMLCamera() { Pos = Pos.ToXML(), Target = Target.ToXML(), Aperture = Aperture, FovH = FovH };
        }

    }
}
