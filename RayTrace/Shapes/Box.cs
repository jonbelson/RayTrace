using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    // Represents an axis-aligned Box centred at Pos.
    //[ObsoleteAttribute]
    public class Box : Shape
    {
        public float Width { get; set; }       // X
        public float Height { get; set; }      // Y
        public float Depth { get; set; }      // Z

        // Precalculated values to speed up calculations.
        private SPoint pos;

        public Box(Scene scene, Point p, float width, float height, float depth) : base(scene, p)
        {
            Width = width;
            Height = height;
            Depth = depth;
        }

        public Box(Scene scene, Point p) : this(scene, p, 50, 50, 50) { }
        public Box(Scene scene, Point p, float size) : this(scene, p, size, size, size) { }

        // Allow Shape to pre-transform data using the Camera's WorldToView Matrix.
        public override void CreateCache(Camera camera)
        {
            pos = new(Pos.X, Pos.Y, Pos.Z, 1.0f);
        }

        private void Swap(ref float d1, ref float d2)
        {
            float temp = d1;
            d1 = d2;
            d2 = temp;
        }

        private void SetTextureCoords(HitResult hr)
        {
            SVector normal = hr.EntryNormal;
            SPoint Intersection = hr.Entry;

            // https://gamedev.stackexchange.com/questions/172352/finding-texture-coordinates-for-plane
            SVector e1 = normal.VectorProduct(new SVector(1.0f, 0.0f, 0.0f, 0.0f));
            e1 = e1.Normalise();
            if (e1.I == 0.0 && e1.J == 0.0 && e1.K == 0.0)
            {
                e1 = normal.VectorProduct(new SVector(0.0f, 0.0f, 1.0f, 0f));
                e1 = e1.Normalise();
            }
            SVector e2 = normal.VectorProduct(e1);
            e2 = e2.Normalise();

            SVector vecIntersection = new SVector(Intersection.X, Intersection.Y, Intersection.Z, 0f);
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

            hr.u = (float)u;
            hr.v = (float)v;
        }

        private HitResult HitTestImpl(Camera camera, SPoint p1, SPoint p2, bool segmentOnly)
        {
            HitResult hr = new HitResult(this);

            //Matrix worldToView = new Matrix();
            //worldToView.SetLookAt(new Point(0, 30, 0), new Point(0, 30, 10));
            //worldToView.SetLookAt(new Point(30, 60, 0), new Point(0, 0, 110));
            //worldToView.SetLookAt(new Point(0, 40, 0), new Point(0, 0, 100));
            SPoint wPos = camera.WorldToView * pos;

            // Get bounding box extents.
            SPoint B0 = new SPoint(wPos.X - Width / 2, wPos.Y - Height / 2, wPos.Z - Depth / 2, 1.0f);
            SPoint B1 = new SPoint(wPos.X + Width / 2, wPos.Y + Height / 2, wPos.Z + Depth / 2, 1.0f);

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

            if (tnear > tfar) return hr;    // Actually impossible here :)
            if (tfar < 0) return hr;

            // Y
            float InvDJ = 1.0f / D.J;
            float t0y = (B0.Y - p1.Y) * InvDJ;
            float t1y = (B1.Y - p1.Y) * InvDJ;

            if (t0y > t1y) Swap(ref t0y, ref t1y);

            if (t0y > tnear) tnear = t0y;
            if (t1y < tfar) tfar = t1y;

            if (tnear > tfar) return hr;
            if (tfar < 0) return hr;

            // Z
            float InvDK = 1.0f / D.K;
            float t0z = (B0.Z - p1.Z) * InvDK;
            float t1z = (B1.Z - p1.Z) * InvDK;

            if (t0z > t1z) Swap(ref t0z, ref t1z);

            if (t0z > tnear) tnear = t0z;
            if (t1z < tfar) tfar = t1z;

            if (tnear > tfar) return hr;
            if (tfar < 0) return hr;

            SVector d = D;
            d = d.Normalise();

            SPoint Intersection = p1 + d.Scale(tnear);

            hr.Entry = Intersection;

            // Calculate surfce normal at intersection point.
            SVector P2I = Intersection - wPos;

            P2I = new SVector(
                P2I.I / (Width / 2),
                P2I.J / (Height / 2),
                P2I.K / (Depth / 2),
                0f
            );

            //P2I.I /= (Width / 2);
            //P2I.J /= (Height / 2);
            //P2I.K /= (Depth / 2);

            //P2I = P2I.Normalise();

            SVector P2Inorm = P2I;
            P2Inorm = P2Inorm.Normalise();

            //SVector normal = new SVector();

            double AbsI = Math.Abs(P2I.I);
            double AbsJ = Math.Abs(P2I.J);
            double AbsK = Math.Abs(P2I.K);

            float nI, nJ, nK;
            nI = nJ = nK = 0.0f;

            if (AbsI > AbsJ)
            {
                if (AbsI > AbsK)
                {
                    // YZ plane
                    nI = P2I.I > 0.0f ? 1.0f : -1.0f;
                }
                else
                {
                    // XY plane
                    nK = P2I.K > 0.0f ? 1.0f : -1.0f;
                }
            }
            else
            {
                if (AbsJ > AbsK)
                {
                    // XZ plane
                    nJ = P2I.J > 0.0f ? 1.0f : -1.0f;
                }
                else
                {
                    // XY plane
                    nK = P2I.K > 0.0f ? 1.0f : -1.0f;
                }
            }

            SVector normal = new SVector(nI, nJ, nK, 0.0f);

            hr.EntryNormal = normal;

            SetTextureCoords(hr);

            // Add a slight offset to the intersection point to avoid self-intersection.
            hr.Entry += normal.Scale(0.01f);

            Debug.Assert(!normal.IsNull());

            return hr;
        }

        public override HitResult HitTest(Camera camera, Ray ray)
        {
            bool segmentOnly = (ray.TMax == 1.0);

            SPoint p2 = ray.Origin.Offset(ray.Direction);

            return HitTestImpl(camera, ray.Origin, p2, segmentOnly);
        }

        //public override HitResult HitTest(Camera camera, Point p1, Point p2)
        //{
        //    return HitTestImpl(camera, p1, p2, true);
        //}

        public override (SPoint, SVector) GetRefractedVector(Camera camera, SVector ray, HitResult hr)
        {
            Debug.Assert(hr.HasEntry());

            float n1 = RefractiveIndex.Vacuum;
            float n2 = hr.shape.Material.RefractiveIndex;

            //			Vector refraction1 = Refraction.GetAngle(ray, hr.EntryNormal, RefractiveIndex.Vacuum, RefractiveIndex.WindowGlass);
            SVector refraction1 = Refraction.GetAngle(ray, hr.EntryNormal, n1, n2);

            if (!refraction1.IsNull())
            {
                Ray ray2 = new Ray(hr.Entry, refraction1);

                // Hit test refracted ray against ourself to find exit point.
                HitResult hrInternal = HitTest(camera, ray2 /*hr.Entry, refraction1*/);
                if (hrInternal.HasEntry())
                {
                    // Found the exit point, so calculate refraction as ray exits the sphere.
                    SVector refraction2 = Refraction.GetAngle(refraction1, hrInternal.EntryNormal.Negate(), n2, n1);

                    return (hrInternal.Entry, refraction2);
                }
            }

            // Didn't mange to get e refracted ray, so return values passed.
            return (hr.Entry, ray);	// XXX Or return Vector.Null()?
        }

    }
}
