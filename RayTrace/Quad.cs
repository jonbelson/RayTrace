using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    //
    // Class representing a rectangle with width and height
    //
    public class Quad : Shape
    {
        public Vector Up { get; set; }
        public Vector Right { get; set; }

        public double Width { get; set; }
        public double Height { get; set; }

        private HitResult HitTestImpl(Camera camera, Point p1, Point p2, bool segmentOnly)
        {
            HitResult hr = new HitResult(this);

            Point vPos = camera.WorldToView * Pos;
            Vector vUp = camera.NormalMatrix * Up;
            Vector vRight = camera.NormalMatrix * Right;

            Vector vNormal = vUp.VectorProduct(vRight);

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

            double THRESH = 0.000001;

            if (mu < THRESH)
            {
                // Not interested in intersections that happen behind us.
                return hr;
            }

            if (segmentOnly)
            {
                // Check if segment actually intersects us.
                if (mu < THRESH || mu > 1.0 - THRESH)
                {
                    return hr;
                }
            }

            Vector vecP2P1 = p2 - p1;
            //Vector vecP2P1 = p1 - p2;
            Point Intersection = p1 + vecP2P1.Scale(mu);

            hr.Entry = Intersection;
            hr.EntryNormal = vNormal;



            return hr;
        }

        // Define rectangle (quad) using two Vectors and a position.
        public Quad(Point p, Vector up, Vector right, double width, double height) : base(p)
        {
            Up = up;
            Right = right;

            Width = width;
            Height = height;
        }

        public override HitResult HitTest(Camera camera, Ray ray)
        {
            HitResult hr = new HitResult(this);

            bool segmentOnly = ray.TMax == 1.0;

            Point p1 = ray.Origin;
            Point p2 = ray.Origin + ray.Direction;

            return HitTestImpl(camera, p1, p2, segmentOnly);
        }


    }
}
