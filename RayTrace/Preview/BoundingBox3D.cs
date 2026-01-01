using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace RayTrace.Preview
{
    //
    // Represent an axis-aligned bounding box.
    //
    public class BoundingBox3D : ModelVisual3D
    {
        private GeometryModel3D CreateBoundingBox()
        {
            double[,] box3D = new double[8, 3]
            {
                // Top
                { -0.5, 0.5, 0.5 },
                { 0.5, 0.5, 0.5 },
                { 0.5, 0.5, -0.5 },
                { -0.5, 0.5, -0.5 },

                // Bottom
                { -0.5, -0.5, 0.5 },
                { 0.5, -0.5, 0.5 },
                { 0.5, -0.5, -0.5 },
                { -0.5, -0.5, -0.5 }
            };

            int[,] boxEdgesX = new int[4, 2]
            {
                { 0, 1 }, { 2, 3 }, { 4, 5 }, { 6, 7 },     // X axis aligned.
            };

            int[,] boxEdgesY = new int[4, 2]
            {
                { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 },     // Y axis aligned.
            };

            int[,] boxEdgesZ = new int[4, 2]
            {
                { 0, 3 }, { 1, 2 }, { 4, 7 }, { 5, 6 }     // Z axis aligned.
            };

            Point3D MakePoint(int i) { return new Point3D(box3D[i, 0], box3D[i, 1], box3D[i, 2]); }

            Func<Point3D, Point3D, Point3DCollection> CreateLine = delegate (Point3D p1, Point3D p2) { return new Point3DCollection(new List<Point3D>() { p1, p2 }); };

            // Aligned with X axis.
            for (int i = 0; i < 4; i++)
            {
                int p1 = boxEdgesX[i, 0];
                int p2 = boxEdgesX[i, 1];

                _3DTools.ScreenSpaceLines3D line = new();
                line.Points = CreateLine(MakePoint(p1), MakePoint(p2));
                line.Thickness = 2;
                line.Color = Colors.Red;

                this.Children.Add(line);
                //viewport3D.Children.Add(line);
            }

            // Aligned with Y axis.
            for (int i = 0; i < 4; i++)
            {
                int p1 = boxEdgesY[i, 0];
                int p2 = boxEdgesY[i, 1];

                _3DTools.ScreenSpaceLines3D line = new();
                line.Points = CreateLine(MakePoint(p1), MakePoint(p2));
                line.Thickness = 2;
                line.Color = Colors.Green;

                this.Children.Add(line);
                //                viewport3D.Children.Add(line);
            }

            // Aligned with Z axis.
            for (int i = 0; i < 4; i++)
            {
                int p1 = boxEdgesZ[i, 0];
                int p2 = boxEdgesZ[i, 1];

                _3DTools.ScreenSpaceLines3D line = new();
                line.Points = CreateLine(MakePoint(p1), MakePoint(p2));
                line.Thickness = 2;
                line.Color = Colors.Blue;

                this.Children.Add(line);
                //                viewport3D.Children.Add(line);
            }

            return null;
        }

        public BoundingBox3D(float sx, float sy, float sz, SPoint p)
        {
            CreateBoundingBox();

            var scale = new ScaleTransform3D(sx, sy, sz);
            var translate = new TranslateTransform3D(p.X, p.Y, -p.Z);
            Transform3DGroup transformGroup = new();

            transformGroup.Children.Add(scale);
            //transformGroup.Children.Add(new RotateTransform3D(axisRotationX));
            //transformGroup.Children.Add(new RotateTransform3D(axisRotationY));
            //transformGroup.Children.Add(new RotateTransform3D(axisRotationZ));
            transformGroup.Children.Add(translate);

            Transform = transformGroup;

        }

        public BoundingBox3D()
        {
            CreateBoundingBox();
        }

        public BoundingBox3D(SPoint min, SPoint max)
        {
            CreateBoundingBox();

            float dx = max.X - min.X;
            float dy = max.Y - min.Y;
            float dz = max.Z - min.Z;

            SPoint mid = new((min.X + max.X)/2.0f, (min.Y + max.Y)/2.0f, (min.Z + max.Z)/2.0f, 1.0f);

            var scale = new ScaleTransform3D(dx/1.0f, dy/1.0f, dz/1.0f);
            //var translate = new TranslateTransform3D(mid.X, mid.Y/2.0f, -mid.Z/2.0f);

            float tx = min.X + dx/2.0f;
            float ty = min.Y + dy/2.0f;
            float tz = min.Z + dz/2.0f;

            var translate = new TranslateTransform3D(tx, ty, -tz);
            Transform3DGroup transformGroup = new();

            transformGroup.Children.Add(scale);
            transformGroup.Children.Add(translate);

            Transform = transformGroup;

        }
    }
}
