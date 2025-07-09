using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using System.Numerics;
using System.Windows;

namespace RayTrace
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

    public class PlaneXZ : ModelVisual3D
    {

        public PlaneXZ()
        {
            GeometryModel3D plane = new();
            //MeshGeometry3D mesh = GeometryModel3DFactory.MakePlane(new Vector(0, 1.0f, 0));
            MeshGeometry3D mesh = new();

            float Size = 100.0f;
            mesh.Positions.Add(new Point3D(-Size, 0, Size));
            mesh.Positions.Add(new Point3D(Size, 0, Size));
            mesh.Positions.Add(new Point3D(Size, 0, -Size));
            mesh.Positions.Add(new Point3D(-Size, 0, -Size));

            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(3);

            plane.Geometry = mesh;

            DiffuseMaterial material = new(new SolidColorBrush(Color.FromArgb(0, 255, 255, 0)));
            plane.Material = material;

            //plane.BackMaterial = material;

            Transform3DGroup transformGroup = new();

            // var transform = new MatrixTransform3D(lookAt);

            var scale = new ScaleTransform3D(10, 10, 1);
            transformGroup.Children.Add(scale);

            plane.Transform = transformGroup;

            Content = plane;
            //this.Children.Add(plane);
        }
    };

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

        private ModelVisual3D _dragXZPlane;

        public bool UseAmbient { get; set; } = false;

        private static Point3DCollection CreateLine(Point3D p1, Point3D p2)
        {
            return new Point3DCollection(new List<Point3D>() { p1, p2 });
        }


        public PreviewViewport3D()
        {
            //Children.Add(_groundPlane);

            CreateDragPlaneXZ();

        }

        public void ClearDragPlaneXZ()
        {
            if (_dragXZPlane != null)
            {
                Children.Remove(_dragXZPlane);

            }
        }

        public /*private*/ void CreateDragPlaneXZ()
        {
            ClearDragPlaneXZ();

            _dragXZPlane = new PlaneXZ();

            Children.Add(_dragXZPlane);
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

        public PreviewHitResult HitTestPlaneXZ(System.Windows.Point mousePos)
        {
            PreviewHitResult hitResult = new PreviewHitResult();

            HitTestFilterBehavior FilterCallback(DependencyObject o)
            {
                if (o.GetType() == typeof(PlaneXZ))
                {
                    return HitTestFilterBehavior.Continue;
                }

//                return HitTestFilterBehavior.ContinueSkipSelfAndChildren;
                return HitTestFilterBehavior.ContinueSkipSelf;

            }

            HitTestResultBehavior HitTestCallback(HitTestResult hitTestResult)
            {
                if (hitTestResult is RayHitTestResult rayHitResult)
                {
                    //PreviewHitResult result = new();

                    if (rayHitResult is RayMeshGeometry3DHitTestResult meshHitResults)
                    {
                        hitResult.Distance = meshHitResults.DistanceToRayOrigin;

                        Visual3D visual = meshHitResults.VisualHit;
                        hitResult.Visual3D = (visual is ModelVisual3D modelVisual) ? modelVisual : null;

                        GeometryModel3D hitGeometry = meshHitResults.ModelHit as GeometryModel3D;
                        hitResult.GeometryModel3D = hitGeometry;

                        if (_dragXZPlane != null)
                        {
                            if (visual == _dragXZPlane)
                            {
                                hitResult.Point = meshHitResults.PointHit;
                                hitResult.Type = PreviewHitResult.HitType.DragPlaneXZ;
                            }
                        }

                    }
                }

                return HitTestResultBehavior.Continue;

            }

            Point3D testPoint3D = new Point3D(mousePos.X, mousePos.Y, 0);
            Vector3D testDirection3D = new Vector3D(mousePos.X, mousePos.Y, 10);
            PointHitTestParameters hitTestParams = new(mousePos);
            RayHitTestParameters rayParams = new RayHitTestParameters(testPoint3D, testDirection3D);

            VisualTreeHelper.HitTest(this, null /*FilterCallback*/, HitTestCallback, hitTestParams);

            return hitResult;
        }

        private PreviewHitResult HitTestGeometry(System.Windows.Point mousePos)
        {
            //List<GeometryModel3D> hitResults = new();
            List<PreviewHitResult> hitResults = new();


            //GeometryModel3D model;

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
