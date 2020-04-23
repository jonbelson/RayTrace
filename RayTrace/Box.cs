using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    // Represents an axis-aligned Box centred at Pos.
    public class Box : Shape
    {
        public double Width { get; set; }       // X
        public double Height { get; set; }      // Y
        public double Depth { get; set; }      // Z

        public Box(Point p, double width, double height, double depth) : base(p)
        {
            Width = width;
            Height = height;
            Depth = depth;
        }

        public Box(Point p) : this(p, 50, 50, 50) { }

        private void Swap(ref double d1, ref double d2)
        {
            double temp = d1;
            d1 = d2;
            d2 = temp;
        }

        private HitResult HitTestImpl(Point p1, Point p2, bool segmentOnly)
        {
            HitResult hr = new HitResult(this);

            // Get bounding box extents.
            Point B0 = new Point(Pos.X - Width / 2, Pos.Y - Height / 2, Pos.Z - Depth / 2);
            Point B1 = new Point(Pos.X + Width / 2, Pos.Y + Height / 2, Pos.Z + Depth / 2);

            Vector D = p2 - p1;
            //D.Normalise();

            double tnear = -9999999999;
            double tfar = 999999999;

            // X
            double InvDI = 1.0 / D.I;
            double t0x = (B0.X - p1.X) * InvDI;
            double t1x = (B1.X - p1.X) * InvDI;

            if (t0x > t1x) Swap(ref t0x, ref t1x);

            if (t0x > tnear) tnear = t0x;
            if (t1x < tfar) tfar = t1x;

            if (tnear > tfar) return hr;    // Actually impossible here :)
            if (tfar < 0) return hr;

            // Y
            double InvDJ = 1.0 / D.J;
            double t0y = (B0.Y - p1.Y) * InvDJ;
            double t1y = (B1.Y - p1.Y) * InvDJ;

            if (t0y > t1y) Swap(ref t0y, ref t1y);

            if (t0y > tnear) tnear = t0y;
            if (t1y < tfar) tfar = t1y;

            if (tnear > tfar) return hr;
            if (tfar < 0) return hr;

            // Z
            double InvDK = 1.0 / D.K;
            double t0z = (B0.Z - p1.Z) * InvDK;
            double t1z = (B1.Z - p1.Z) * InvDK;

            if (t0z > t1z) Swap(ref t0z, ref t1z);

            if (t0z > tnear) tnear = t0z;
            if (t1z < tfar) tfar = t1z;

            if (tnear > tfar) return hr;
            if (tfar < 0) return hr;

            Vector d = D;
            d.Normalise();

            Point Intersection = p1 + d.Scale(tnear /*+ 0.1*/);
//            Intersection.X += d.I * t0x;
//            Intersection.Y += d.J * t0y;
//            Intersection.Z += d.K * t0z;

            hr.Entry = Intersection;


            // Calculate surfce normal at intersection point.
            Vector P2I = Intersection - Pos;

            P2I.I /= (Width / 2);
            P2I.J /= (Height / 2);
            P2I.K /= (Depth / 2);

            //P2I.Normalise();

            Vector normal = new Vector();

            double AbsI = Math.Abs(P2I.I);
            double AbsJ = Math.Abs(P2I.J);
            double AbsK = Math.Abs(P2I.K);

            if (AbsI > AbsJ)
            {
                if (AbsI > AbsK)
                    normal.I = P2I.I > 0.0 ? 1.0 : -1.0;
                else
                    normal.K = P2I.K > 0.0 ? 1.0 : -1.0;
            }
            else
            {
                if (AbsJ > AbsK)
                    normal.J = P2I.J > 0.0 ? 1.0 : -1.0;
                else
                    normal.K = P2I.K > 0.0 ? 1.0 : -1.0;
            }

            hr.EntryNormal = normal;

            hr.Entry += normal.Scale(0.01);

            Debug.Assert(!normal.IsNull());

            return hr;
        }

        public override HitResult HitTest(Point p, Vector ray)
        {
            Point p2 = p.Offset(ray);

            return HitTestImpl(p, p2, false);
        }

        public override HitResult HitTest(Point p1, Point p2)
        {
            return HitTestImpl(p1, p2, true);
        }

        public override (Point, Vector) GetRefractedVector(Vector ray, HitResult hr)
        {
            Debug.Assert(hr.HasEntry());

            float n1 = RefractiveIndex.Vacuum;
            float n2 = hr.shape.Material.RefractiveIndex;

            //			Vector refraction1 = Refraction.GetAngle(ray, hr.EntryNormal, RefractiveIndex.Vacuum, RefractiveIndex.WindowGlass);
            Vector refraction1 = Refraction.GetAngle(ray, hr.EntryNormal, n1, n2);

            if (!refraction1.IsNull())
            {
                // Hit test refracted ray against ourself to find exit point.
                HitResult hrInternal = HitTest(hr.Entry, refraction1);
                if (hrInternal.HasEntry())
                {
                    // Found the exit point, so calculate refraction as ray exits the sphere.
                    Vector refraction2 = Refraction.GetAngle(refraction1, hrInternal.EntryNormal.Negate(), n2, n1);

                    return (hrInternal.Entry, refraction2);
                }
            }

            // Didn't mange to get e refracted ray, so return values passed.
            return (hr.Entry, ray);	// XXX Or return Vector.Null()?
        }

    }
}
