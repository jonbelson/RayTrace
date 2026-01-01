using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    // Represents an AABB.
    public class BoundingBox
    {
        private static void Swap(ref float d1, ref float d2)
        {
            (d2, d1) = (d1, d2);
        }

        public float MinX { get; set; }
        public float MaxX { get; set; }
        public float MinY { get; set; }
        public float MaxY { get; set; }
        public float MinZ { get; set; }
        public float MaxZ { get; set; }

        public enum Axis { X, Y, Z };

        public SPoint GetCentre()
        {
            return new SPoint
            (
                (MinX + MaxX) / 2.0f,
                (MinY + MaxY) / 2.0f,
                (MinZ + MaxZ) / 2.0f,
                1.0f
            );
        }

        public float GetCentreX() => (MinX + MaxX) / 2.0f;
        public float GetCentreY() => (MinY + MaxY) / 2.0f;
        public float GetCentreZ() => (MinZ + MaxZ) / 2.0f;

        public float GetWidth() => (MaxX - MinX);
        public float GetHeight() => (MaxY - MinY);
        public float GetDepth() => (MaxZ - MinZ);

        //public SVector Diagonal() => 

        public Axis GetLongestAxis()
        {
            float width = Math.Abs(MaxX - MinX);
            float height = Math.Abs(MaxY - MinY);
            float depth = Math.Abs(MaxZ - MinZ);

            if (width > height)
            {
                return (width > depth) ? Axis.X : Axis.Z;
            }
            else
            {
                return (height > depth) ? Axis.Y : Axis.Z;
            }
        }

        // Default BoundingBox constructor that contains, well, nothing.
        public BoundingBox()
        {
            MinX = float.MaxValue;
            MinY = float.MaxValue;
            MinZ = float.MaxValue;
            MaxX = float.MinValue;
            MaxY = float.MinValue;
            MaxZ = float.MinValue;
        }

        //
        // Constructor.
        //
        public BoundingBox(float minX, float maxX, float minY, float maxY, float minZ, float maxZ)
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

        //
        // Constructor.
        //
        public BoundingBox(BoundingBox bb) : this(bb.MinX, bb.MaxX, bb.MinY, bb.MaxY, bb.MinZ, bb.MaxZ) {}

        // Increase size of box to contain specified Point.
        public void AddPoint(SPoint p)
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
            SPoint p1 = matrix * new SPoint(MinX, MinY, MinZ, 1.0f);
            SPoint p2 = matrix * new SPoint(MinX, MaxY, MinZ, 1.0f);
            SPoint p3 = matrix * new SPoint(MaxX, MaxY, MinZ, 1.0f);
            SPoint p4 = matrix * new SPoint(MaxX, MinY, MinZ, 1.0f);

            SPoint p5 = matrix * new SPoint(MinX, MinY, MaxZ, 1.0f);
            SPoint p6 = matrix * new SPoint(MinX, MaxY, MaxZ, 1.0f);
            SPoint p7 = matrix * new SPoint(MaxX, MaxY, MaxZ, 1.0f);
            SPoint p8 = matrix * new SPoint(MaxX, MinY, MaxZ, 1.0f);

            BoundingBox bb = new();
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

        public bool Intersects(BoundingBox bb)
        {
            if (bb.MinX > MaxX) return false;
            if (bb.MaxX < MinX) return false;
            if (bb.MinY > MaxY) return false;
            if (bb.MaxY < MinY) return false;
            if (bb.MinZ > MaxZ) return false;
            if (bb.MaxZ < MinZ) return false;
            return true;
        }

        // Shrink this Bounding Box to only encompass the specified Bounding Box.
        public BoundingBox Intersection(BoundingBox bb)
        {
            BoundingBox intersection = new();

            if (!(bb.MinX > MaxX || bb.MaxX < MinX))
            {
                intersection.MinX = Math.Max(MinX, bb.MinX);
                intersection.MaxX = Math.Min(MaxX, bb.MaxX);
            }

            if (!(bb.MinY > MaxY || bb.MaxY < MinY))
            {
                intersection.MinY = Math.Max(MinY, bb.MinY);
                intersection.MaxY = Math.Min(MaxY, bb.MaxY);
            }

            if (!(bb.MinZ > MaxZ || bb.MaxZ < MinZ))
            {
                intersection.MinZ = Math.Max(MinZ, bb.MinZ);
                intersection.MaxZ = Math.Min(MaxZ, bb.MaxZ);
            }

            return intersection;
        }

        // https://tavianator.com/2011/ray_box.html
        //public bool Intersects2(SPoint p1, SPoint p2)
        public ( float tmin, float tmax ) GetIntersections(in SPoint p1, in SPoint p2)
        {
            SVector D = p2 - p1;

            float InvDI = 1.0f/D.I;
            float InvDJ = 1.0f/D.J;
            float InvDK = 1.0f/D.K;

            // X axis
            float tx1 = (MinX - p1.X)*InvDI;
            float tx2 = (MaxX - p1.X)*InvDI;

            float tmin = Math.Min(tx1, tx2);
            float tmax = Math.Max(tx1, tx2);

            // Y axis
            float ty1 = (MinY - p1.Y)*InvDJ;
            float ty2 = (MaxY - p1.Y)*InvDJ;

            tmin = Math.Max(tmin, Math.Min(ty1, ty2));
            tmax = Math.Min(tmax, Math.Max(ty1, ty2));

            // Z axis
            float tz1 = (MinZ - p1.Z)*InvDK;
            float tz2 = (MaxZ - p1.Z)*InvDK;

            tmin = Math.Max(tmin, Math.Min(tz1, tz2));
            tmax = Math.Min(tmax, Math.Max(tz1, tz2));

            //return tmax >= tmin;
            return (tmin, tmax);
        }

        public bool Intersects2(in SPoint p1, in SPoint p2)
        {
            var t = GetIntersections(in p1, in p2);

            return t.tmax >= t.tmin;
        }

        public bool Intersects3(in SPoint p1, in SPoint p2, out float distance)
        {
            SVector D = p2 - p1;

            var (tmin, tmax) = GetIntersections(in p1, in p2);

            if (tmax >= tmin)
            {

                if (tmin > 0)
                {
                    distance = tmin*D.Magnitude();
                }
                else
                {
                    distance = tmax*D.Magnitude();
                }
                return true;
            }

            distance = float.MaxValue;
            return false;

        }

        // Get distance to closest intersection point between ray and bounding box.
        public float GetClosestIntersection(in SPoint p1, in SPoint p2)
        {
            var (tmin, tmax)= GetIntersections(in p1, in p2);

            SVector D = p2 - p1;

            if (tmin > 0)
            {
                return tmin*D.Magnitude();
            }

            return tmax*D.Magnitude();
        }

        public bool Intersects(in SPoint p1, in SPoint p2, out float distance)
        {
            distance = float.MaxValue;

            // Get bounding box extents.
            SPoint B0 = new(MinX, MinY, MinZ, 1.0f);
            SPoint B1 = new(MaxX, MaxY, MaxZ, 1.0f);

            SVector D = p2 - p1;
            //D = D.Normalise();

            float tnear = float.MinValue;
            float tfar = float.MaxValue;

            // X
            float InvDI = 1.0f / D.I;
            float t0x = (B0.X - p1.X) * InvDI;
            float t1x = (B1.X - p1.X) * InvDI;

            if (t0x > t1x) Swap(ref t0x, ref t1x);

            if (t0x > tnear) tnear = t0x;
            if (t1x < tfar) tfar = t1x;

            if (tnear > tfar) return false;    // Actually impossible here :)
            if (tfar < 0) return false;

            // Y
            float InvDJ = 1.0f / D.J;
            float t0y = (B0.Y - p1.Y) * InvDJ;
            float t1y = (B1.Y - p1.Y) * InvDJ;

            if (t0y > t1y) Swap(ref t0y, ref t1y);

            if (t0y > tnear) tnear = t0y;
            if (t1y < tfar) tfar = t1y;

            if (tnear > tfar) return false;
            if (tfar < 0) return false;

            // Z
            float InvDK = 1.0f / D.K;
            float t0z = (B0.Z - p1.Z) * InvDK;
            float t1z = (B1.Z - p1.Z) * InvDK;

            if (t0z > t1z) Swap(ref t0z, ref t1z);

            if (t0z > tnear) tnear = t0z;
            if (t1z < tfar) tfar = t1z;

            if (tnear > tfar) return false;
            if (tfar < 0) return false;

            if (tnear > 0)
            {
                distance = tnear;// *D.Magnitude();
            }
            else
            {
                distance = tfar;// *D.Magnitude();
            }

            return true;
        }

        public override string ToString()
        {
            string s = $"Min: [{MinX}, {MinY}, {MinZ}] Max: [{MaxX}, {MaxY}, {MaxZ}]";

            return s;
        }
    }
}
