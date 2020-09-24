using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    public class BoundingBox
    {
        private void Swap(ref double d1, ref double d2)
        {
            double temp = d1;
            d1 = d2;
            d2 = temp;
        }

        public double MinX { get; set; }
        public double MaxX { get; set; }
        public double MinY { get; set; }
        public double MaxY { get; set; }
        public double MinZ { get; set; }
        public double MaxZ { get; set; }

        public enum Axis { X, Y, Z };

        public Point GetCentre()
        {
            return new Point()
            {
                X = (MinX + MaxX) / 2.0,
                Y = (MinY + MaxY) / 2.0,
                Z = (MinZ + MaxZ) / 2.0
            };
        }

        public Axis GetLongestAxis()
        {
            double width = Math.Abs(MaxX - MinX);
            double height = Math.Abs(MaxY - MinY);
            double depth = Math.Abs(MaxZ - MinZ);

            if (width > height)
            {
                return (width > depth) ? Axis.X : Axis.Z;
            }
            else
            {
                return (height > depth) ? Axis.Y : Axis.Z;
            }
        }

        // Default BoundingBox that contains, well, nothing.
        public BoundingBox()
        {
            MinX = Double.MaxValue;
            MinY = Double.MaxValue;
            MinZ = Double.MaxValue;
            MaxX = Double.MinValue;
            MaxY = Double.MinValue;
            MaxZ = Double.MinValue;
        }

        public BoundingBox(double minX, double maxX, double minY, double maxY, double minZ, double maxZ)
        {
            MinX = minX;
            MaxX = maxX;
            MinY = minY;
            MaxY = maxY;
            MinZ = minZ;
            MaxZ = maxZ;

            //Debug.Assert(MinX < MaxX);
            //Debug.Assert(MinY < MaxY);
            //Debug.Assert(MinZ < MaxZ);
        }

        // Increase size of box to contain specified Point.
        public void AddPoint(Point p)
        {
            MinX = Math.Min(MinX, p.X);
            MaxX = Math.Max(MaxX, p.X);

            MinY = Math.Min(MinY, p.Y);
            MaxY = Math.Max(MaxY, p.Y);

            MinZ = Math.Min(MinZ, p.Z);
            MaxZ = Math.Max(MaxZ, p.Z);
        }

        // Return a AABB BoundingBox that contains this BoundingBox rotated by a Matrix.
        public BoundingBox Transform(Matrix matrix)
        {
            // Rate each of the eight corner points and create a BoundingBox that contains them all.
            Point p1 = matrix * new Point(MinX, MinY, MinZ);
            Point p2 = matrix * new Point(MinX, MaxY, MinZ);
            Point p3 = matrix * new Point(MaxX, MaxY, MinZ);
            Point p4 = matrix * new Point(MaxX, MinY, MinZ);

            Point p5 = matrix * new Point(MinX, MinY, MaxZ);
            Point p6 = matrix * new Point(MinX, MaxY, MaxZ);
            Point p7 = matrix * new Point(MaxX, MaxY, MaxZ);
            Point p8 = matrix * new Point(MaxX, MinY, MaxZ);

            BoundingBox bb = new BoundingBox();
            bb.AddPoint(p1);
            bb.AddPoint(p2);
            bb.AddPoint(p3);
            bb.AddPoint(p4);
            bb.AddPoint(p5);
            bb.AddPoint(p6);
            bb.AddPoint(p7);
            bb.AddPoint(p8);

            return bb;
        }

        public bool Intersects(Point p1, Point p2)
        {
            // Get bounding box extents.
            Point B0 = new Point(MinX, MinY, MinZ);
            Point B1 = new Point(MaxX, MaxY, MaxZ);

            Vector D = p2 - p1;
            //D = D.Normalise();

            double tnear = Double.MinValue;   // -9999999999;
            double tfar = Double.MaxValue;  // 999999999;

            // X
            double InvDI = 1.0 / D.I;
            double t0x = (B0.X - p1.X) * InvDI;
            double t1x = (B1.X - p1.X) * InvDI;

            if (t0x > t1x) Swap(ref t0x, ref t1x);

            if (t0x > tnear) tnear = t0x;
            if (t1x < tfar) tfar = t1x;

            if (tnear > tfar) return false;    // Actually impossible here :)
            if (tfar < 0) return false;

            // Y
            double InvDJ = 1.0 / D.J;
            double t0y = (B0.Y - p1.Y) * InvDJ;
            double t1y = (B1.Y - p1.Y) * InvDJ;

            if (t0y > t1y) Swap(ref t0y, ref t1y);

            if (t0y > tnear) tnear = t0y;
            if (t1y < tfar) tfar = t1y;

            if (tnear > tfar) return false;
            if (tfar < 0) return false;

            // Z
            double InvDK = 1.0 / D.K;
            double t0z = (B0.Z - p1.Z) * InvDK;
            double t1z = (B1.Z - p1.Z) * InvDK;

            if (t0z > t1z) Swap(ref t0z, ref t1z);

            if (t0z > tnear) tnear = t0z;
            if (t1z < tfar) tfar = t1z;

            if (tnear > tfar) return false;
            if (tfar < 0) return false;

            return true;
        }
    }
}
