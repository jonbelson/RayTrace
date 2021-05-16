using System;
using System.Collections.Specialized;
using System.ComponentModel;

namespace RayTrace
{
    // Represent a point in 3D space.
    public class /*struct*/ Point : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void Notify(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        private double x;
        private double y;
        private double z;

        public double X { get { return x; } set { if (x != value) { x = value; Notify("X"); } } }
        public double Y { get { return y; } set { if (y != value) { y = value; Notify("Y"); } } }
        public double Z { get { return z; } set { if (z != value) { z = value; Notify("Z"); } } }

        public Point() : this(0.0, 0.0, 0.0) { }

        public Point(double x, double y, double z) => (X, Y, Z) = (x, y, z);

        public Point(XMLPoint xmlPoint) => (X, Y, Z) = (xmlPoint.X, xmlPoint.Y, xmlPoint.Z);

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

        public XMLPoint ToXML()
        {
            return new XMLPoint() { X = X, Y = Y, Z = Z };
        }
    }
}
