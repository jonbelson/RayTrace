using _3DTools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using static System.Formats.Asn1.AsnWriter;

using RayTrace.Preview;

namespace RayTrace
{
    internal class InputMode
    {
        public virtual void DoMouseWheel(int delta) { }
        public virtual void DoMouseMove(System.Windows.Point p) { }
        public virtual void DoLButtonDown(System.Windows.Point p) { }
        public virtual void DoLButtonUp(System.Windows.Point p) { }
    }

    // Represent a plane to be hit tested against.
    public readonly struct HitPlane
    {
        public readonly Point3D Origin;
        public readonly Vector3D Normal;

        public HitPlane(Point3D origin, Vector3D normal) => (Origin, Normal) = ( origin, normal);
    }

    internal class InputMode3D : InputMode
    {
        private MainWindow _mainWindow; // XXX Required to update selected Entity :(
        private PreviewViewport3D _viewport3D;
        private readonly PerspectiveCamera _camera;
        private readonly Scene _scene;

        // XZ plane for hit testing against.
        private readonly HitPlane _XZPlane = new HitPlane(new Point3D(0, 0, 0), new Vector3D(0, 1, 0));

        private System.Windows.Point _clickPoint;
        private System.Windows.Point _mousePoint;

        private Point3D _XZOffset; // Offset from XZ click point to position of hit Entity.

        private PreviewHitResult _hitResult;

        private GeometryModel3D _yPlane;

        private enum State
        {
            Normal, DragView, DragObjectX, DragObjectY, DragObjectZ, DragObject
        }

        private State _state = State.Normal;

        //private Entity _entity = null;


        public InputMode3D(MainWindow mainWindow, PreviewViewport3D viewport3D, PerspectiveCamera camera, Scene scene)
        {
            _mainWindow = mainWindow;
            _viewport3D = viewport3D;
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

            switch (_state)
            {
                case State.DragView:
                    {
                        HandleDragView(p);
                        break;
                    }
                case State.DragObjectX:
                case State.DragObjectY:
                case State.DragObjectZ:
                case State.DragObject:
                    {
                        HandleDragEntity(p);
                        break;
                    }
            }


            _mousePoint = p;
        }

        private void ConvertPoint(System.Windows.Point p)
        {
            Viewport3DVisual vpv = VisualTreeHelper.GetParent(_viewport3D.Children[0]) as Viewport3DVisual; ;

            bool success;
            Matrix3D m = MathUtils.TryWorldToViewportTransform(vpv, out success);
            if (success)
            {
                m.Invert();

                Point3D pointClicked = new Point3D(p.X, p.Y, 1.0);

                Point3D point3D = m.Transform(pointClicked);

                Debug.WriteLine("click: " + p + " in 3D: " + point3D);

            }
        }
/*
        private void GetLocalLine(out Point3D origin, out Vector3D direction)
        {
            if (HasWorldTransformMatrix)
            {
                LineUtil.Transform(WorldTransformMatrix, ref origin, ref direction, out isRay);
            }

        }

        private void HitTestXZPlane(System.Windows.Point p)
        {
            Point3D testPoint3D = new(p.X, p.Y, 0);
            Vector3D testDirection3D = new(p.X, p.Y, 10);
            PointHitTestParameters hitTestParams = new(p);
            RayHitTestParameters rayParams = new(testPoint3D, testDirection3D);

            GeometryModel3D model;

            model.RayHitTest();

            Geometry3D geometry;// = new();

            geometry.RayHitTest();

            Point3D origin;
            Vector3D direction;

            rayParams.GetLocalLine(out origin, out direction);

            geometry.RayHitTestCore();

        }
*/
        private SVector PointToVector(System.Windows.Point p)
        {
            double width = _viewport3D.ActualWidth;
            double height = _viewport3D.ActualHeight;
            double aspect = width/height;

            float sceneWidth = _scene.Camera.GetSceneWidth();
            float sceneHeight = _scene.Camera.GetSceneHeight();

            double hfov = _camera.FieldOfView*Math.PI/180.0;
            sceneWidth = 2.0f*(float)Math.Tan(hfov/2);
            sceneHeight = sceneWidth;

            float Size = sceneWidth;
            height = width;

            float sceneStartX = -Size / 2.0f;
            float sceneStartY = Size / 2.0f;
            float sceneStepX = (float)(Size / width);       // Step per pixel in Scene units.
            float sceneStepY = (float)(Size / height);


            float ratioX = (float)(p.X/width);
            float ratioY = 1.0f - (float)(p.Y/height);

            float sceneX = ratioX*sceneWidth;
            float sceneY = ratioY*sceneHeight;

            //float x = (float)(-Size/2 + p.X);
            //float y = (float)(Size/2 - p.Y);

            float x = sceneStartX + (float)(p.X*sceneStepX);
            float y = sceneStartY - (float)(p.Y*sceneStepY);



            SPoint origin = new(0, 0, 0, 1.0f);
            SVector dir = new(x, y, 1.0f, 0f);
            Ray ray = new(origin, dir.Normalise());

            SPoint planePos = new(0, 0, 0, 1.0f);
            SVector planeNormal = new(0, 1, 0, 0);

            SPoint vPos = _scene.Camera.WorldToView*planePos;
            SVector vNormal = _scene.Camera.NormalMatrix*planeNormal;

            SPoint p1 = ray.Origin;
            SPoint p2 = ray.Origin + ray.Direction;

            float A = vNormal.I;
            float B = vNormal.J;
            float C = vNormal.K;

            // Since Ax + By + Cz + D = 0...
            float D = -(A * vPos.X + B * vPos.Y + C * vPos.Z);

            float num = A * p1.X + B * p1.Y + C * p1.Z + D;
            float denom = A * (p1.X - p2.X) + B * (p1.Y - p2.Y) + C * (p1.Z - p2.Z);

            if (denom == 0)
            {
                return SVector.Null();// new(0, 0, 0);
            }

            float mu = num / denom;

            double THRESH = 0.000001;

            if (mu < THRESH)
            {
                // Not interested in intersections that happen behind us.
                return SVector.Null();
            }

            SVector vecP2P1 = p2 - p1;
            //Vector vecP2P1 = p1 - p2;
            SPoint Intersection = p1 + vecP2P1.Scale(mu);

            //hr.t = mu;
            //hr.Entry = Intersection;


            return new(Intersection.X, Intersection.Y, Intersection.Z, 0f);





#if false
            // Get normalised click position (0 - 1)
            double xNorm = p.X/width;
            double yNorm = p.Y/height;

            // Calculate vectors to edges of frustum/
            var look = _camera.LookDirection;
            //var up = _camera.UpDirection;
            var Yaxis = new SVector(0, 1, 0);

            double hFov = _camera.FieldOfView;
            double vFov = hFov/aspect;



            SVector vLook = new((float)look.X, (float)look.Y, (float)look.Z);
            //SVector vUp = new((float)up.X, (float)up.Y, (float)up.Z);

            SVector vRight = vLook.VectorProduct(Yaxis);

            SVector vUp = vRight.VectorProduct(vLook);

            Quaternion rotLeft = new(vUp, (float)DegToRad(-hFov/2));
            Quaternion rotRight = new(vUp, (float)DegToRad(hFov/2));

            Quaternion rotUp = new(vRight, (float)DegToRad(-vFov/2));
            Quaternion rotDown = new(vRight, (float)DegToRad(vFov/2));


            SVector vLeftEdge = rotLeft.Rotate(vLook.Normalise());
            SVector vRightEdge = rotRight.Rotate(vLook.Normalise());

            SVector vTopEdge = rotUp.Rotate(vLook.Normalise());
            SVector vBottomEdge = rotDown.Rotate(vLook.Normalise());

            //SVector vClick = SVector.Lerp();


            Debug.WriteLine("vLeftEdge: " + vLeftEdge.AsString());
            Debug.WriteLine("vRightEdge: " + vRightEdge.AsString());
            Debug.WriteLine("vTopEdge: " + vTopEdge.AsString());
            Debug.WriteLine("vBottomEdge: " + vBottomEdge.AsString());


            float x = (float)(vLeftEdge.I + (vRightEdge.I - vLeftEdge.I)*xNorm);
            float y = (float)(vBottomEdge.J + (vTopEdge.J - vBottomEdge.J)*yNorm);

            float z = 1.0f - x*x + y*y;

            Debug.WriteLine($"x/y/z -> {x}, {y}, {z}");

            SVector vClick = new(x, y, z);

            return vClick;
#endif
        }

        private readonly Func<double, double> DegToRad = (a) => (a/360.0)*2*Math.PI;

        public override void DoLButtonDown(System.Windows.Point p)
        {
            /*
                        PreviewHitResult hitResult2 = _viewport3D.HitTestPlaneXZ(p);

                        if (hitResult2.Type == PreviewHitResult.HitType.DragPlaneXZ)
                        {
                            Point3D point = hitResult2.Point;
                            new Sphere(_scene, new Point((float)point.X, (float)point.Y, (float)-point.Z), 1);
                            _viewport3D.UpdatePreview(_scene);
                        }
            */
            /*
            SVector vToClick = PointToVector(p);

            if (!vToClick.IsNull())
            {
                new Sphere(_scene, new Point(vToClick.I, vToClick.J, vToClick.K), 1);
                _viewport3D.UpdatePreview(_scene);
            }
            */

            //Point3D cam = _camera.Position;

            // Vector intersects the XZ place at Y = 0.
            // y = camy + t.j
            // 0 = camy + t.j
            // t = -camy/j

            /*
            if (vToClick.J != 0)
            {
                float t = (float)(-cam.Y/vToClick.J);

                float XZx = (float) cam.X + t*vToClick.I;
                float XZz = (float) cam.Z + t*vToClick.K;

                SPoint pointOnPlane = new(XZx, 0.0f, XZz);

                var p3d = new Point(pointOnPlane);
                var v3d = new Vector(vToClick);
                Debug.WriteLine(p + " -> " + v3d + " -> " + p3d);
            }


            Matrix3D m = GetProjectionMatrix(_camera, 1.0);

            Point3D point = new(p.X, p.Y, 1.0);
            Point3D.Multiply(point, m);

            //Debug.WriteLine(p + " -> " + point);

            ConvertPoint(p);
            */




            //var hr = _viewport3D.HitTestPlaneXZ5(p);

            //if (hr.IsHit())
            //{
            //    Debug.WriteLine($"HitTestPlaneXZ: {hr.Point.X}, {hr.Point.Y}, {hr.Point.Z}");
            //}



            _mousePoint = p;
            _clickPoint = p;

            PreviewHitResult hitResult = _viewport3D.HitTest(p);

            PreviewHitResult.HitType hitType = hitResult.Type;

            Debug.WriteLine(hitType.ToString());

            PreviewHitResult hitResultXZ = _viewport3D.HitTestPlaneXZ5(p, _XZPlane);

            if (hitType == PreviewHitResult.HitType.None)
            {
                _state = State.DragView;
            }
            else if (hitType == PreviewHitResult.HitType.Entity)
            {
                _mainWindow.SelectedEntity.Clear();
                _mainWindow.SelectedEntity.Add(hitResult.Entity);

                _state = State.DragObject;

                if (hitResult.Entity is Shape shape)
                {
                    _viewport3D.UpdateBoundingBox(shape);

                    _XZOffset = new((double)shape.Pos.X - hitResultXZ.Point.X, (double)shape.Pos.Y - hitResultXZ.Point.Y, (double)shape.Pos.Z + hitResultXZ.Point.Z);
                    Debug.WriteLine($"shape.Pos = {shape.Pos.X}, {shape.Pos.Y}, {shape.Pos.Z}");
                    Debug.WriteLine($"hitResultXZ.Point = {hitResultXZ.Point.X}, {hitResultXZ.Point.Y}, {hitResultXZ.Point.Z}");
                    Debug.WriteLine($"_XZOffset = {_XZOffset.X}, {_XZOffset.Y}, {_XZOffset.Z}");
                }
            }
            else
            {
                switch (hitType)
                {
                    case PreviewHitResult.HitType.ModelAxisX: { _state = State.DragObjectX; break; }
                    case PreviewHitResult.HitType.ModelAxisY: { _state = State.DragObjectY; break; }
                    case PreviewHitResult.HitType.ModelAxisZ: { _state = State.DragObjectZ; break; }
                }
                if (hitResult.Entity is Shape shape)
                {
                    _XZOffset = new((double)shape.Pos.X - hitResult.Point.X, (double)shape.Pos.Y - hitResult.Point.Y, (double)shape.Pos.Z + hitResult.Point.Z);
                }
            }

            _hitResult = hitResult;
        }

        private void HandleDragView(System.Windows.Point p)
        {
            bool ctrlPressed = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
            bool shiftPressed = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

            float xDiff = (float)(p.X - _mousePoint.X);
            float yDiff = (float)(p.Y - _mousePoint.Y);

            Action<string, SVector> WriteSVector = delegate (string name, SVector v) { Debug.WriteLine(name + ":" + v.I + ", " + v.J + ", " + v.K); };

            if (!ctrlPressed && !shiftPressed)
            {
                // Move camera Target

                SVector viewVector = _scene.Camera.GetViewVector().ToSVector();
                float magnitude = viewVector.Magnitude();
                SVector view = viewVector.Normalise();
                SVector left = view.VectorProduct(new SVector(0, 1, 0, 0));
                //SVector right = view.VectorProduct(new SVector(0, 1, 0));
                SVector up = view.VectorProduct(left);

                //WriteSVector("viewVector", viewVector);
                //WriteSVector("left", left);

                const float factor = 200.0f;

                // Yaw.
                Quaternion q = new(new SVector(0, 1, 0, 0f), xDiff / factor);
                SVector rotY = q.Rotate(view);

                //WriteSVector("rotY", rotY);

                // Pitch.
                Quaternion qPitch = new(-left, yDiff / factor);
                SVector rotLeft = qPitch.Rotate(view);

                //WriteSVector("rotLeft", rotLeft);

                SVector total = ((rotY + rotLeft).Normalise()) * magnitude;
                if (Double.IsNaN(total.I) || Double.IsInfinity(total.I))
                {
                    _ = 0;
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
        }

        private void HandleDragEntity(System.Windows.Point p)
        {
            //SVector vToClick = PointToVector(p);

            Point3D cam = _camera.Position;

            // Vector intersects the XZ place at Y = 0.
            // y = camy + t.j
            // 0 = camy + t.j
            // t = -camy/j

            //if (vToClick.J != 0)
            //{
            //    float t = (float)(-cam.Y/vToClick.J);

            //    float XZx = (float)cam.X + t*vToClick.I;
            //    float XZz = (float)cam.Z + t*vToClick.K;

            //    SPoint pointOnPlane = new(XZx, 0.0f, XZz);

            //}


            if (_mainWindow.SelectedEntity.Count > 0)
            {
                Entity entity = _mainWindow.SelectedEntity[0];

                if (entity is Shape shape)
                {

//                  PreviewHitResult hitResult = _viewport3D.HitTestPlaneXZ(p);
                    PreviewHitResult hitResult = _viewport3D.HitTestPlaneXZ5(p, _XZPlane);

                    Point newPos = new((float)hitResult.Point.X, (float)hitResult.Point.Y, (float)-hitResult.Point.Z);

                    switch (_state)
                    {
                        case State.DragObject:
                            if (hitResult.Type == PreviewHitResult.HitType.DragPlaneXZ)
                            {
                                //Point newPos = new((float) hitResult.Point.X, shape.Pos.Y, (float) -hitResult.Point.Z);
                                //shape.Pos = newPos;
                                newPos.Y = shape.Pos.Y;

                                newPos.X += (float)_XZOffset.X;
                                //newPos.Y += (float)_XZOffset.Y;
                                newPos.Z += (float)_XZOffset.Z;
                            }
                            break;
                        case State.DragObjectX:
                            if (hitResult.Type == PreviewHitResult.HitType.DragPlaneXZ)
                            {
                                newPos.Y = shape.Pos.Y;
                                newPos.Z = shape.Pos.Z;
                            }

                            break;
                        case State.DragObjectY:
                            newPos.X = shape.Pos.X;
                            newPos.Z = shape.Pos.Z;

                            break;
                        case State.DragObjectZ:
                            if (hitResult.Type == PreviewHitResult.HitType.DragPlaneXZ)
                            {
                                newPos.X = shape.Pos.X;
                                newPos.Y = shape.Pos.Y;
                            }

                            break;
                    }


                    shape.Pos = newPos;

                    _viewport3D.UpdateBoundingBox(shape);

                }
            }
        }

        // Get transform matrix from PerspectiveCamera.
        // Based on D3DXMatrixPerspectiveFovRH() docs and '3DTools'
        private static Matrix3D GetProjectionMatrix(PerspectiveCamera camera, double aspectRatio)
        {
            double fieldOfView = camera.FieldOfView/360.0*2*Math.PI;

            double far = camera.FarPlaneDistance;
            double near = camera.NearPlaneDistance;

            double xScale = 1.0/Math.Tan(fieldOfView/2);
            double yScale = xScale*aspectRatio;

            double m33 = far == double.PositiveInfinity ? -1 : far/(near - far);
            double m43 = near*m33;

            var m = new Matrix3D(xScale, 0, 0, 0,
                                0, yScale, 0, 0,
                                0, 0, m33, -1.0,
                                0, 0, m43, 0);

            return m;
        }

        public override void DoLButtonUp(System.Windows.Point p)
        {
            _state = State.Normal;

            _mousePoint = p;
            _clickPoint = p;

            //if (_mainWindow.SelectedEntity.
            //_viewport3D.UpdatePreview(_scene);
        }
    }
}
