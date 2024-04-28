using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using static System.Formats.Asn1.AsnWriter;

namespace RayTrace
{
    internal class InputMode
    {
        public virtual void DoMouseWheel(int delta) { }
        public virtual void DoMouseMove(System.Windows.Point p) { }
        public virtual void DoLButtonDown(System.Windows.Point p) { }
        public virtual void DoLButtonUp(System.Windows.Point p) { }
    }

    internal class InputMode3D : InputMode
    {
        private readonly PerspectiveCamera _camera;
        private readonly Scene _scene;

        private System.Windows.Point _clickPoint;
        private bool _dragging = false;

        public InputMode3D(PerspectiveCamera camera, Scene scene)
        {
            _camera = camera;
            _scene = scene;
        }

        public override void DoMouseWheel(int delta)
        {
            Vector v = _scene.Camera.GetViewVector().Normalise();

            if (delta < 0) v = -v;

            _scene.Camera.Pos += v;
        }

        public override void DoMouseMove(System.Windows.Point p)
        {
            bool ctrlPressed = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
            bool shiftPressed = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

            if (!_dragging)
            {
                _clickPoint = p;
                //_dragging = true;

                return;
            }

            float xDiff = (float)(p.X - _clickPoint.X);
            float yDiff = (float)(p.Y - _clickPoint.Y);

            Action<string, SVector> WriteSVector = delegate (string name, SVector v) { Debug.WriteLine(name + ":" + v.I + ", " + v.J + ", " + v.K); };

            if (!ctrlPressed && !shiftPressed)
            {
                // Move camera Target

                SVector viewVector = new(_scene.Camera.GetViewVector());
                float magnitude = viewVector.Magnitude();
                SVector view = viewVector.Normalise();
                SVector left = view.VectorProduct(new SVector(0, 1, 0));
                //SVector right = view.VectorProduct(new SVector(0, 1, 0));
                SVector up = view.VectorProduct(left);

                //WriteSVector("viewVector", viewVector);
                //WriteSVector("left", left);

                const float factor = 200.0f;

                // Yaw.
                Quaternion q = new(new SVector(0, 1, 0), xDiff / factor);
                SVector rotY = q.Rotate(view);

                //WriteSVector("rotY", rotY);

                // Pitch.
                Quaternion qPitch = new(-left, yDiff / factor);
                SVector rotLeft = qPitch.Rotate(view);

                //WriteSVector("rotLeft", rotLeft);

                SVector total = ((rotY + rotLeft).Normalise()) * magnitude;
                if (Double.IsNaN(total.I) || Double.IsInfinity(total.I))
                {
                    int i = 0;
                }

                //WriteSVector("total", total);

                //Debug.WriteLine("");

                _scene.Camera.Target = _scene.Camera.Pos + new Vector(total); // new Vector(rotY) + new Vector(rotLeft);

                //Vector offset = new Vector(-xDiff/20, 0, 0 /*yDiff*/);
                //scene.Camera.Target += offset;

                //Vector offset2 = new Vector(/*-xDiff*/ 0, yDiff/20, 0);
                //scene.Camera.Target += offset2;
            }
            else if (ctrlPressed && !shiftPressed)
            {
                // Move camera in its plane.

                Vector view = _scene.Camera.GetViewVector().Normalise();

                Vector left = view.VectorProduct(new Vector(0, 1, 0));

                Vector up = view.VectorProduct(left);

                Vector offset = left * xDiff - up * yDiff;

                const float factor = 0.15f;
                offset *= factor;

                _scene.Camera.Pos += offset;
                _scene.Camera.Target += offset;
            }
            else if (!ctrlPressed && shiftPressed)
            {
                // Move camera in XZ plane

                Vector view = _scene.Camera.GetViewVector().Normalise();

                Vector left = view.VectorProduct(new Vector(0, 1, 0));

                left.J = 0.0f;

                view.J = 0.0f;

                Vector offset = left * xDiff + view * yDiff;

                const float factor = 0.15f;
                offset *= factor;

                _scene.Camera.Pos += offset;
                _scene.Camera.Target += offset;
            }
            else if (ctrlPressed && shiftPressed)
            {
                // Rotate camera around target.

                Vector view = _scene.Camera.GetViewVector();

                Vector targetToCamera = -view;

                Point target = _scene.Camera.Target;

                Matrix toOrigin = Matrix.GetTranslation(-target.X, -target.Y, -target.Z);
                Matrix rotation = Matrix.GetRotationY((float)(xDiff / 10.0));
                Matrix fromOrigin = Matrix.GetTranslation(target.X, target.Y, target.Z);

                // targetToCamera = toOrigin * targetToCamera;
                targetToCamera = rotation * targetToCamera;
                //targetToCamera = fromOrigin * targetToCamera;

                _scene.Camera.Pos = _scene.Camera.Target + targetToCamera;
            }

            _clickPoint = p;
        }

        public override void DoLButtonDown(System.Windows.Point p)
        {
            _clickPoint = p;

            _dragging = true;
        }

        public override void DoLButtonUp(System.Windows.Point p)
        {
            _dragging = false;
        }


    }
}
