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

        private HitResult HitTestImpl(Camera camera, Point p1, Point p2, bool segmentOnly)
        {
            HitResult hr = new HitResult(this);

            Point vPos = camera.WorldToView * Pos;
            Vector vNormal = camera.NormalMatrix * Normal;

            double A = vNormal.I;
            double B = vNormal.J;
            double C = vNormal.K;


            // Since Ax + By + Cz + D = 0...
            double D = -(A * vPos.X + B * vPos.Y + C * vPos.Z);

            double num = A * p1.X + B * p1.Y + C * p1.Z + D;
            double denom = A * (p1.X - p2.X) + B * (p1.Y - p2.Y) + C * (p1.Z - p2.Z);

            if (denom == 0)
            {
                return hr;
            }

            double mu = num / denom;

            if (mu == 0.0)
            {
                // Either parallel to plane or on the plane - treat both as no intersection for our purposes.
                return hr;
            }

            if (mu < 0.0)
            {
                // Not interested in intersections that happen behind us.
                return hr;
            }

            if (segmentOnly)
            {
                // Check if segment actually intersects us.
                double THRESH = 0.000001;

                if (mu < THRESH || mu > 1.0 - THRESH)
                {
                    return hr;
                }
            }

            Vector vecP2P1 = p2 - p1;
            //Vector vecP2P1 = p1 - p2;
            Point Intersection = p1 + vecP2P1.Scale(mu);

            hr.Entry = Intersection;

            // Intersection could have occurred from either side of the Plane, so adjust the surface normal appropriately.
            //Vector vecP2P1 = p1 - p2;
            vecP2P1 = vecP2P1.Normalise();
            double dp = vNormal.DotProduct(vecP2P1);
            if (dp > 0.0)
            {
                hr.EntryNormal = vNormal;
            }
            else
            {
                hr.EntryNormal = vNormal.Negate();
            }

            hr.EntryNormal = vNormal;

            //            Vector v = Intersection.Difference(Pos);
            //            hr.u = (float) v.I;
            //            hr.v = (float) v.J;

            // https://gamedev.stackexchange.com/questions/172352/finding-texture-coordinates-for-plane
            Vector e1 = vNormal.VectorProduct(new Vector(1.0, 0.0, 0.0));
            e1 = e1.Normalise();
            if (e1.I == 0.0 && e1.J == 0.0 && e1.K == 0.0)
            {
                e1 = vNormal.VectorProduct(new Vector(0.0, 0.0, 1.0));
                e1 = e1.Normalise();
            }

            e1 = camera.NormalMatrix * e1;
            Vector e2 = vNormal.VectorProduct(e1);
            e2 = e2.Normalise();

            Vector vecIntersection = new Vector(Intersection.X, Intersection.Y, Intersection.Z);
            double mag = vecIntersection.Magnitude();
            vecIntersection = vecIntersection.Normalise();

            double u = e1.DotProduct(vecIntersection);
            double v = e2.DotProduct(vecIntersection);

            int scale = 400;

            u *= mag;
            v *= mag;

            u %= scale;
            v %= scale;

            u /= scale;
            v /= scale;

            if (u < 0.0) u = 1.0f + u;
            if (v < 0.0) v = 1.0f + v;

            hr.u = (float) u;
            hr.v = (float) v;

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

        public override HitResult HitTest(Camera camera, Ray ray)
        {
            HitResult hr = new HitResult(this);

            bool segmentOnly = ray.TMax == 1.0;

            //Point p2 = ray.Origin.Offset(ray.Direction);

            //Vector longRay = ray.Scale(1000.0);

            Point p1 = ray.Origin;
            Point p2 = ray.Origin + ray.Direction;

            return HitTestImpl(camera, p1, p2, segmentOnly);
        }

        //public override HitResult HitTest(Camera camera, Point p1, Point p2)
        //{
        //    return HitTestImpl(camera, p1, p2, true);
        //}

        public override string ToString()
        {
            return "Plane[" + Normal + "," + Pos.ToString() + "]," + Material.ToString() + "]";
        }
    }
}
