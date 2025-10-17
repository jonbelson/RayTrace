using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace RayTrace.Preview
{
    public class PreviewHitResult
    {
        public enum HitType
        {
            None, GroundPlane, GroundAxis, Entity, BoundingBox, ModelAxisX, ModelAxisY, ModelAxisZ, DragPlaneXZ
        }

        public PreviewHitResult() { Type = HitType.None; }

        public HitType Type { get; set; }
        public ModelVisual3D Visual3D { get; set; }
        public GeometryModel3D GeometryModel3D { get; set; }
        public Point3D Point { get; set; }
        public Entity Entity { get; set; }

        public double Distance { get; set; }

        public bool IsHit() => Type != HitType.None;
    }


    public static class MyExtensions
    {
        public static void MultiplyVector(this Matrix3D m, ref Vector3D v)
        {
            double xx = v.X;
            double yy = v.Y;
            double zz = v.Z;

            v.X = xx*m.M11 + yy*m.M21 + zz*m.M31;
            v.Y = xx*m.M12 + yy*m.M22 + zz*m.M32;
            v.Z = xx*m.M13 + yy*m.M23 + zz*m.M33;
        }

        public static void MultiplyPoint(this Matrix3D m, ref Point3D p)
        {
            double xx = p.X;
            double yy = p.Y;
            double zz = p.Z;

            p.X = xx*m.M11 + yy*m.M21 + zz*m.M31 + m.OffsetX;
            p.Y = xx*m.M12 + yy*m.M22 + zz*m.M32 + m.OffsetY;
            p.Z = xx*m.M13 + yy*m.M23 + zz*m.M33 + m.OffsetZ;

            if (!m.IsAffine)
            {
                double w = xx*m.M14 + yy*m.M24 + zz*m.M34 + m.M44;

                p.X /= w;
                p.Y /= w;
                p.Z /= w;
            }

        }
    }


    public class PreviewViewport3D : Viewport3D 
    {
        // Size of ground plane.
        private readonly int PlaneSize = 64;
        private readonly int AxisSize = 10;

        private PreviewVisual3D _preview3D;

        private ModelVisual3D _groundPlane = new();
        private ModelVisual3D _groundAxis = new();

        private BoundingBox3D _boundingBox;
        private ModelAxis3D _modelAxis;

        public bool UseAmbient { get; set; } = false;

        private static Point3DCollection CreateLine(Point3D p1, Point3D p2)
        {
            return new Point3DCollection(new List<Point3D>() { p1, p2 });
        }


        public PreviewViewport3D()
        {

        }

        public void ClearPreview()
        {
            if (_preview3D != null)
            {
                Children.Remove(_preview3D);
            }
        }

        public void UpdatePreview(Scene scene)
        {
            ClearPreview();

            _preview3D = new(scene);

            _preview3D.UseAmbient = UseAmbient;
            _preview3D.Update();

            Children.Add(_preview3D);
        }

        public void Update(Shape shape)
        {
            _preview3D?.Update(shape);
        }

        public void Update(Light light)
        {
            _preview3D?.Update(light);
        }

        public void ClearBoundingBox()
        {
            if (_boundingBox != null)
            {
                Children.Remove(_boundingBox);
            }
            if (_modelAxis != null)
            {
                Children.Remove(_modelAxis);
            }
        }

        public void UpdateBoundingBox(Shape shape)
        {
            ClearBoundingBox();

            _boundingBox = GeometryModel3DFactory.CreateBoundingBox(shape);

            _modelAxis = GeometryModel3DFactory.CreateAxis(shape);

            Children.Add(_boundingBox);
            Children.Add(_modelAxis);
        }

        public void ClearGroundPlane()
        {
            if (_groundPlane != null) Children.Remove(_groundPlane);
        }

        public void CreateGroundPlane()
        {
            ClearGroundPlane();

            for (int z = -PlaneSize / 2; z <= PlaneSize / 2; z += 2)
            {
                _3DTools.ScreenSpaceLines3D line = new();

                line.Points = CreateLine(new Point3D(-PlaneSize / 2, 0, z), new Point3D(PlaneSize / 2, 0, z));
                line.Color = Colors.DarkGray;
                line.Thickness = (z % 10 == 0) ? 2 : 1;

                _groundPlane.Children.Add(line);
            }


            for (int x = -PlaneSize / 2; x <= PlaneSize / 2; x += 2)
            {
                _3DTools.ScreenSpaceLines3D line = new();

                line.Points = CreateLine(new Point3D(x, 0, -PlaneSize / 2), new Point3D(x, 0, PlaneSize / 2));
                line.Color = Colors.DarkGray;
                line.Thickness = (x % 10 == 0) ? 2 : 1;

                _groundPlane.Children.Add(line);
            }

            Children.Add(_groundPlane);
        }

        public void ClearGroundAxis()
        {
            Children.Remove(_groundAxis);
        }

        public void CreateGroundAxis()
        {
            ClearGroundAxis();

            {
                _3DTools.ScreenSpaceLines3D lines = new();

                lines.Points = CreateLine(new Point3D(0, 0, 0), new Point3D(AxisSize, 0, 0));
                lines.Thickness = 5;
                lines.Color = Colors.Red;

                _groundAxis.Children.Add(lines);
            }
            {
                _3DTools.ScreenSpaceLines3D lines = new();

                lines.Points = CreateLine(new Point3D(0, 0, 0), new Point3D(0, AxisSize, 0));
                lines.Thickness = 5;
                lines.Color = Colors.Green;

                _groundAxis.Children.Add(lines);
            }
            {
                _3DTools.ScreenSpaceLines3D lines = new();

                lines.Points = CreateLine(new Point3D(0, 0, 0), new Point3D(0, 0, -AxisSize));
                lines.Thickness = 5;
                lines.Color = Colors.Blue;

                _groundAxis.Children.Add(lines);
            }

            Children.Add(_groundAxis);
        }

        public PreviewHitResult HitTest(System.Windows.Point mousePos)
        {
            return HitTestGeometry(mousePos);
        }


        private struct Ray3D(Point3D origin, Vector3D direction)
        {
            public Point3D Origin = origin;
            public Vector3D Direction = direction;
        }


        private static Matrix3D CreateViewMatrix(PerspectiveCamera camera)
        {
            Vector3D zAxis = camera.LookDirection;
            zAxis.Normalize();

            Vector3D yAxis = camera.UpDirection;
            yAxis.Normalize();

            Vector3D xAxis = Vector3D.CrossProduct(zAxis, yAxis);
            xAxis.Normalize();

            yAxis = Vector3D.CrossProduct(xAxis, zAxis);

            Vector3D positionVec = (Vector3D)camera.Position;

            double cx = -Vector3D.DotProduct(xAxis, positionVec);
            double cy = -Vector3D.DotProduct(yAxis, positionVec);
            double cz = Vector3D.DotProduct(zAxis, positionVec);

            Matrix3D viewMatrix = new Matrix3D(
                xAxis.X, yAxis.X, -zAxis.X, 0,
                xAxis.Y, yAxis.Y, -zAxis.Y, 0,
                xAxis.Z, yAxis.Z, -zAxis.Z, 0,
                cx, cy, cz, 1
                );

            // Prepend camera.Transform ?

            return viewMatrix;
        }

        private static Ray3D GetRayFromPerspectiveCamera(System.Windows.Point mousePosition, Viewport3D viewport, PerspectiveCamera camera)
        {
            double width = viewport.ActualWidth;
            double height = viewport.ActualHeight;

            var x = (2.0*mousePosition.X/width) - 1.0;
            var y = 1.0 - (2.0*mousePosition.Y/height);

            double aspect = width/height;
            double fov = camera.FieldOfView/180.0*Math.PI;
            double halfTanFov = Math.Tan(fov/2.0);

            Vector3D directionCameraSpece = new Vector3D(x*halfTanFov, y*halfTanFov/aspect, -1.0);

            Matrix3D viewMatrix = CreateViewMatrix(camera);
            Matrix3D inverseViewMatrix = viewMatrix;

            inverseViewMatrix.Invert();

            Vector3D directionWorldSpace = inverseViewMatrix.Transform(directionCameraSpece);
            directionWorldSpace.Normalize();

            return new Ray3D(camera.Position, directionWorldSpace);
        }

        public PreviewHitResult HitTestPlaneXZ5(System.Windows.Point screenPoint, HitPlane hitPlane)
        {
            PerspectiveCamera camera = Camera as PerspectiveCamera;

            var ray = GetRayFromPerspectiveCamera(screenPoint, this, camera);

            PreviewHitResult hitResult = new PreviewHitResult();

            if (Math.Abs(ray.Direction.Y) > 1e-6)
            {
                Point3D P1 = ray.Origin;
                Point3D P2 = ray.Origin + ray.Direction;

                Point3D P3 = hitPlane.Origin;
                Vector3D N = hitPlane.Normal;

                Vector3D P3P1 = P3 - P1;
                double denom = Vector3D.DotProduct(N, (P2 - P1));
                if (Math.Abs(denom) > 1e-6)
                {
                    double num = Vector3D.DotProduct(N, P3P1);
                    double u = num/denom;

                    var intersection2 = ray.Origin + ray.Direction*u;

                    hitResult.Type = PreviewHitResult.HitType.DragPlaneXZ;
                    hitResult.Point = intersection2;
                }

                /*
                var t = -ray.Origin.Y/ray.Direction.Y;
                var intersection = ray.Origin + ray.Direction*t;


                hitResult.Type = PreviewHitResult.HitType.DragPlaneXZ;
                hitResult.Point = intersection;
                */
            }

            return hitResult;
        }

        private PreviewHitResult HitTestGeometry(System.Windows.Point mousePos)
        {
            List<PreviewHitResult> hitResults = new();

            HitTestResultBehavior HitTestCallback(HitTestResult hitTestResult)
            {
                if (hitTestResult is RayHitTestResult rayHitResult)
                {
                    PreviewHitResult result = new();

                    if (rayHitResult is RayMeshGeometry3DHitTestResult meshHitResults)
                    {
                        result.Distance = meshHitResults.DistanceToRayOrigin;


                        Visual3D visual = meshHitResults.VisualHit;
                        result.Visual3D = (visual is ModelVisual3D modelVisual) ? modelVisual : null;

                        GeometryModel3D hitGeometry = meshHitResults.ModelHit as GeometryModel3D;
                        result.GeometryModel3D = hitGeometry;

                        if (_groundPlane != null)
                        {
                            if (visual == _groundPlane)
                            {
                                result.Type = PreviewHitResult.HitType.GroundPlane;
                            }

                            if (_groundPlane.Children.Contains(visual))
                            {
                                result.Type = PreviewHitResult.HitType.GroundPlane;
                            }
                        }

                        if (_groundAxis != null)
                        {
                            if (visual == _groundAxis)
                            {
                                result.Type = PreviewHitResult.HitType.GroundAxis;
                            }

                            if (_groundAxis.Children.Contains(visual))
                            {
                                result.Type = PreviewHitResult.HitType.GroundAxis;
                            }
                        }

                        if (_boundingBox != null)
                        {
                            if (visual == _boundingBox)
                            {
                                result.Type = PreviewHitResult.HitType.GroundAxis;
                            }

                            if (_boundingBox.Children.Contains(visual))
                            {
                                result.Type = PreviewHitResult.HitType.BoundingBox;
                            }
                        }

                        if (_modelAxis != null)
                        {
                            if (visual == _modelAxis)
                            {
                                result.Type = PreviewHitResult.HitType.ModelAxisX;
                            }

                            int index = _modelAxis.Children.IndexOf(visual);
                            if (index != -1)
                            {
                                switch (index)
                                {
                                    case 0: result.Type = PreviewHitResult.HitType.ModelAxisX; break;
                                    case 1: result.Type = PreviewHitResult.HitType.ModelAxisY; break;
                                    case 2: result.Type = PreviewHitResult.HitType.ModelAxisZ; break;
                                }

                            }

                            //if (_modelAxis.Children.Contains(visual))
                            //{
                            //    result.Type = PreviewHitResult.HitType.GroundAxis;
                            //}
                        }

                        Entity entity = _preview3D.GetEntityFromGeometry(hitGeometry);
                        if (entity != null)
                        {
                            result.Type = PreviewHitResult.HitType.Entity;
                            result.Entity = entity;
                        }

                        hitResults.Add(result);

                        /*
                        Entity entity = preview3D.GetEntityFromGeometry(hitGeometry);
                        if (entity != null)
                        {
                            SelectedEntity.Clear();
                            SelectedEntity.Add(entity);

                            UpdateEntityTreeView();

                            return HitTestResultBehavior.Stop;
                        }
                        */
                    }
                }
                return HitTestResultBehavior.Continue;

            }
            Point3D testPoint3D = new Point3D(mousePos.X, mousePos.Y, 0);
            Vector3D testDirection3D = new Vector3D(mousePos.X, mousePos.Y, 10);
            PointHitTestParameters hitTestParams = new(mousePos);
            RayHitTestParameters rayParams = new RayHitTestParameters(testPoint3D, testDirection3D);

            VisualTreeHelper.HitTest(this, null, HitTestCallback, hitTestParams);

            if (hitResults.Count > 0) { return hitResults[0]; }

            return new PreviewHitResult();
        }
    }
}
