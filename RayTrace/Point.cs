using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Numerics;

namespace RayTrace
{
    public readonly struct SPoint
    {
        public readonly float X { get; }
        public readonly float Y { get; }
        public readonly float Z { get; }
        private readonly float W = 1.0f;

        public SPoint(Point p) => (X, Y, Z) = (p.X, p.Y, p.Z);  // Temporary

        //public SPoint() => (X, Y, Z) = (0, 0, 0);

        public SPoint(SPoint p) => (X, Y, Z) = (p.X, p.Y, p.Z);
        public SPoint(float x, float y, float z) => (X, Y, Z) = (x, y, z);

        public SPoint Scale(float scale) => new(X * scale, Y * scale, Z * scale);
        public SVector Difference(in SPoint p) => new(p.X - X, p.Y - Y, p.Z - Z, 0);
        public SPoint Offset(in SVector v) => new(X + v.I, Y + v.J, Z + v.K);
        public double Distance(in SPoint p) => Difference(p).Magnitude();

        public static SPoint operator*(in SPoint p, float scale) => new(p.X * scale, p.Y * scale, p.Z * scale);
        public static SVector operator-(in SPoint p1, in SPoint p2) => new(p1.X - p2.X, p1.Y - p2.Y, p1.Z - p2.Z, 0);
        public static SPoint operator+(in SPoint p, in SVector v) => new(p.X + v.I, p.Y + v.J, p.Z + v.K);
        public static SPoint operator-(in SPoint p, in SVector v) => new(p.X - v.I, p.Y - v.J, p.Z - v.K);

        public Vector4 ToVector4() => new(X, Y, Z, 0);
        public Vector4 ToPoint4() => new(X, Y, Z, 1);
    }

    // Represent a point in 3D space.
    public class /*struct*/ Point : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void Notify(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        private float x;
        private float y;
        private float z;

        public float X { get { return x; } set { if (x != value) { x = value; Notify("X"); } } }
        public float Y { get { return y; } set { if (y != value) { y = value; Notify("Y"); } } }
        public float Z { get { return z; } set { if (z != value) { z = value; Notify("Z"); } } }

        public Point(SPoint p) => (X, Y, Z) = (p.X, p.Y, p.Z);  // Temporary

        public Point() : this(0.0f, 0.0f, 0.0f) { }

        public Point(float x, float y, float z) => (X, Y, Z) = (x, y, z);

        public Point(XMLPoint xmlPoint) => (X, Y, Z) = (xmlPoint.X, xmlPoint.Y, xmlPoint.Z);

        public Point Scale(float scale) => new Point(X * scale, Y * scale, Z * scale);

        public static Point operator*(Point p, float scale) => new Point(p.X * scale, p.Y * scale, p.Z * scale);

        public static Vector operator-(Point p1, Point p2) => new Vector(p1.X - p2.X, p1.Y - p2.Y, p1.Z - p2.Z);

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

        public Vector4 ToVector4() => new(X, Y, Z, 0);
    }
}
