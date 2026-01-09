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
    // Represent an XYZ axis.
    //
    public class ModelAxis3D : ModelVisual3D
    {
        //Point3D MakePoint(int i) { return new Point3D(box3D[i, 0], box3D[i, 1], box3D[i, 2]); }

        static private Point3D origin = new Point3D(0, 0, 0);
        static private Point3D x = new(1, 0, 0);
        static private Point3D y = new(0, 1, 0);
        static private Point3D z = new(0, 0, 1);

        private static readonly double Thickness = 8;

        public ModelAxis3D(SPoint origin)
        {
            CreateModelAxis3D();

            Transform3DGroup transformGroup = new();

            var translate = new TranslateTransform3D(origin.X, origin.Y, origin.Z);

            //var scale = new ScaleTransform3D(10, 10, 10);
            //transformGroup.Children.Add(scale);
            transformGroup.Children.Add(translate);

            Transform = transformGroup;

        }

        public void CreateModelAxis3D()
        {
            static Point3DCollection CreateLine(Point3D p1, Point3D p2) { return new Point3DCollection(new List<Point3D>() { p1, p2 }); }

            _3DTools.ScreenSpaceLines3D line = new()
            {
                Points = CreateLine(new(0, 0, 0), new(1, 0, 0)),
                Thickness = Thickness,
                Color = Colors.Red
            };

            this.Children.Add(line);

            line = new()
            {
                Points = CreateLine(new(0, 0, 0), new(0, 1, 0)),
                Thickness = Thickness,
                Color = Colors.Green
            };

            this.Children.Add(line);

            line = new()
            {
                Points = CreateLine(new(0, 0, 0), new(0, 0, 1)),
                Thickness = Thickness,
                Color = Colors.Blue
            };

            this.Children.Add(line);

        }
    };
}
