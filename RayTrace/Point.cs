using System;
namespace RayTrace
{
    // Represent a point in 3D space.
    public struct Point
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        //public Point() : this(0.0, 0.0, 0.0) { }

        public Point(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Point Scale(double scale)
        {
            return new Point(X * scale, Y * scale, Z * scale);
        }

        public static Point operator*(Point p, double scale)
        {
            return new Point(p.X * scale, p.Y * scale, p.Z * scale);
        }

        public static Vector operator -(Point p1, Point p2)
        {
            return new Vector(p1.X - p2.X, p1.Y - p2.Y, p1.Z - p2.Z);
        }

        //public static Vector operator -(Point p1, Point p2)
        //{
        //    return new Vector(p2.X - p1.X, p2.Y - p1.Y, p2.Z - p1.Z);
        //}

        public Vector Difference(Point p)
        {
            return new Vector(p.X - X, p.Y - Y, p.Z - Z);
        }

        public static Point operator+(Point p, Vector v)
        {
            return new Point(p.X + v.I, p.Y + v.J, p.Z + v.K);
        }

        public Point Offset(Vector v)
        {
            return new Point(X + v.I, Y + v.J, Z + v.K);
        }

        public double Distance(Point p)
        {
            return Difference(p).Magnitude();
        }

        public override string ToString()
        {
            return "Point[" + X + "," + Y + "," + Z + "]";
        }
    }
}
