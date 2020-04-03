using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    public class Plane : Shape
    {
        public Vector Normal { get; set; }
        //public Point Point { get; set; }

        private HitResult GetIntersection(Point p1, Point p2, bool segmentOnly)
        {
            HitResult hr = new HitResult(this);

            double A = Normal.I;
            double B = Normal.J;
            double C = Normal.K;

            // Since Ax + By + Cz + D = 0...
            double D = -(A * Pos.X + B * Pos.Y + C * Pos.Z);

            double num = A * p1.X + B * p1.Y + C * p1.Z + D;
            double denom = A * (p1.X - p2.X) + B * (p1.Y - p2.Y) + C * (p1.Z - p2.Z);

            if (denom == 0)
            {
                return hr;
            }

            double u = num / denom;

            if (u == 0.0)
            {
                // Either parallel to plane or on the plane - treat both as no intersection for our purposes.
                return hr;
            }

            if (u < 0.0)
            {
                // Not interested in intersections that happen behind us.
                return hr;
            }

            if (segmentOnly)
            {
                // Check if segment actually intersects us.
                double THRESH = 0.000001;

                if (u < THRESH || u > 1.0 - THRESH)
                {
                    return hr;
                }
            }

            //Vector vecP1P2 = p2 - p1;
            Vector vecP2P1 = p1 - p2;
            Point Intersection = p1 + vecP2P1.Scale(u);

            hr.Entry = Intersection;

            // Intersection could have occurred from either side of the Plane, so adjust the surface normal appropriately.
            //Vector vecP2P1 = p1 - p2;
            vecP2P1.Normalise();
            double dp = Normal.DotProduct(vecP2P1);
            if (dp > 0.0)
            {
                hr.EntryNormal = Normal;
            }
            else
            {
                hr.EntryNormal = Normal.Negate();
            }

            hr.EntryNormal = Normal;

            //            Vector v = Intersection.Difference(Pos);
            //            hr.u = (float) v.I;
            //            hr.v = (float) v.J;

            // https://gamedev.stackexchange.com/questions/172352/finding-texture-coordinates-for-plane
            Vector e1 = Normal.VectorProduct(new Vector(1.0, 0.0, 0.0));
            e1.Normalise();
            if (e1.I == 0.0 && e1.J == 0.0 && e1.K == 0.0)
            {
                e1 = Normal.VectorProduct(new Vector(0.0, 0.0, 1.0));
                e1.Normalise();
            }
            Vector e2 = Normal.VectorProduct(e1);
            e2.Normalise();

            Vector vecIntersection = new Vector(Intersection.X, Intersection.Y, Intersection.Z);
            vecIntersection.Normalise();
            hr.u = (float)e1.DotProduct(vecIntersection);
            hr.v = (float)e2.DotProduct(vecIntersection);

            return hr;
        }

        public Plane(Vector normal, Point p) : base(p)
        {
            Normal = normal;
        }

        public Plane(Vector normal, Point p, Colour c) : base(p, c)
        {
            Normal = normal;
        }

        public override HitResult HitTest(Point pos, Vector ray)
        {
            HitResult hr = new HitResult(this);

            Vector longRay = ray.Scale(1000.0);

            Point p1 = pos;
            Point p2 = pos.Offset(longRay);

            return GetIntersection(p1, p2, false);
        }

        public override HitResult HitTest(Point p1, Point p2, bool ignoreClosest = false)
        {
            return GetIntersection(p1, p2, true);
        }

        public override string ToString()
        {
            return "Plane[" + Normal + "," + Pos.ToString() + "]," + Material.ToString() + "]";
        }
    }
}
