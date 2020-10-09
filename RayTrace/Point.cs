using System;
using System.Collections.Specialized;
using System.ComponentModel;

namespace RayTrace
{
    // Represent a point in 3D space.
    public class /*struct*/ Point
    {
        [NotifyParentProperty(true)]
        public double X { get ;set; }
        [NotifyParentProperty(true)]
        public double Y { get; set; }
        [NotifyParentProperty(true)]
        public double Z { get; set; }

        public Point() : this(0.0, 0.0, 0.0) { }

        public Point(double x, double y, double z) => (X, Y, Z) = (x, y, z);

        public Point Scale(double scale) => new Point(X * scale, Y * scale, Z * scale);

        public static Point operator*(Point p, double scale) => new Point(p.X * scale, p.Y * scale, p.Z * scale);

        public static Vector operator -(Point p1, Point p2) => new Vector(p1.X - p2.X, p1.Y - p2.Y, p1.Z - p2.Z);

        public Vector Difference(Point p) => new Vector(p.X - X, p.Y - Y, p.Z - Z);

        public static Point operator+(Point p, Vector v) => new Point(p.X + v.I, p.Y + v.J, p.Z + v.K);

        public Point Offset(Vector v) => new Point(X + v.I, Y + v.J, Z + v.K);

        public double Distance(Point p) => Difference(p).Magnitude();

        public override string ToString()
        {
//            return "Point[" + X + "," + Y + "," + Z + "]";
            return "[" + X.ToString("F2") + "," + Y.ToString("F2") + "," + Z.ToString("F2") + "]";
        }
    }
}
