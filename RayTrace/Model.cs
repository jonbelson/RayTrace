using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Media.Media3D;

namespace RayTrace
{
    //
    // Shape represented by triangles.
    //
    class Model : Shape
    {
        //private struct Point3D
        //{
        //    public double x, y, z;
        //    public Point3D(double x, double y, double z)
        //    {
        //        this.x = x;
        //        this.y = y;
        //        this.z = z;
        //    }
        //}

        //private struct Normal3D
        //{
        //    public double i, j, k;
        //}

        public struct Texel
        {
            public float u, v;
        }

        public struct Triangle
        {
            public int v1, v2, v3;  // Indices into Point array.
            public int t1, t2, t3;  // Indexes into Texel array.
            public int n;           // Index into normal array.
            //public Triangle(int v1, int v2, int v3)
            //{
            //    this.v1 = v1;
            //    this.v2 = v2;
            //    this.v3 = v3;
            //}
        }

        private Point[] points;
        private Texel[] texels;
        private Vector[] normals;
        private Triangle[] triangles;

        private void CreateSquare()
        {
            points = new Point[4];
            points[0] = new Point(0.0, 0.0, 30.0);
            points[1] = new Point(0.0, 10.0, 30.0);
            points[2] = new Point(10.0, 10.0, 30.0);
            points[3] = new Point(10.0, 0.0, 30.0);

            normals = new Vector[1];
            normals[0].I = 0.0;
            normals[0].J = 0.0;
            normals[0].K = 1.0;

            triangles = new Triangle[2];
            triangles[0].v1 = 0;
            triangles[0].v2 = 1;
            triangles[0].v3 = 2;
            triangles[0].n = 0;

            triangles[1].v1 = 0;
            triangles[1].v2 = 2;
            triangles[1].v3 = 3;
            triangles[1].n = 0;
        }

        private void CreateCube()
        {
            points = new Point[8];
            // Front points
            points[0] = new Point(0.0, 0.0, 30.0);
            points[1] = new Point(0.0, 10.0, 30.0);
            points[2] = new Point(10.0, 10.0, 30.0);
            points[3] = new Point(10.0, 0.0, 30.0);
            // Back points
            points[4] = new Point(0.0, 0.0, 40.0);
            points[5] = new Point(0.0, 10.0, 40.0);
            points[6] = new Point(10.0, 10.0, 40.0);
            points[7] = new Point(10.0, 0.0, 40.0);

            normals = new Vector[6];
            normals[0] = new Vector(0.0, 0.0, -1.0);
            normals[1] = new Vector(0.0, 0.0, 1.0);

            normals[2] = new Vector(-1.0, 0.0, 0.0);
            normals[3] = new Vector(1.0, 0.0, 0.0);

            normals[4] = new Vector(0.0, 1.0, 0.0);
            normals[5] = new Vector(0.0, -1.0, 0.0);

            triangles = new Triangle[12];
            // Front
            triangles[0].v1 = 0;
            triangles[0].v2 = 1;
            triangles[0].v3 = 2;
            triangles[0].n = 0;

            triangles[1].v1 = 0;
            triangles[1].v2 = 2;
            triangles[1].v3 = 3;
            triangles[1].n = 0;

            // Left
            triangles[2].v1 = 4;
            triangles[2].v2 = 5;
            triangles[2].v3 = 0;
            triangles[2].n = 2;

            triangles[3].v1 = 5;
            triangles[3].v2 = 1;
            triangles[3].v3 = 0;
            triangles[3].n = 2;

            // Right
            triangles[4].v1 = 3;
            triangles[4].v2 = 2;
            triangles[4].v3 = 6;
            triangles[4].n = 3;

            triangles[5].v1 = 6;
            triangles[5].v2 = 7;
            triangles[5].v3 = 3;
            triangles[5].n = 3;

            // Top
            triangles[6].v1 = 5;
            triangles[6].v2 = 6;
            triangles[6].v3 = 1;
            triangles[6].n = 4;

            triangles[7].v1 = 6;
            triangles[7].v2 = 2;
            triangles[7].v3 = 1;
            triangles[7].n = 4;

            // Bottom
            triangles[8].v1 = 0;
            triangles[8].v2 = 3;
            triangles[8].v3 = 4;
            triangles[8].n = 5;

            triangles[9].v1 = 3;
            triangles[9].v2 = 7;
            triangles[9].v3 = 4;
            triangles[9].n = 5;

            // Right
            triangles[10].v1 = 3;
            triangles[10].v2 = 2;
            triangles[10].v3 = 7;
            triangles[10].n = 3;

            triangles[11].v1 = 3;
            triangles[11].v2 = 6;
            triangles[11].v3 = 7;
            triangles[11].n = 3;

        }

        public Model(Point p) : base(p)
        {
            /*
                        points = new Point[3];
                        points[0] = new Point(0.0, 0.0, 30.0);
                        points[1] = new Point(0.0, 10.0, 30.0);
                        points[2] = new Point(10.0, 10.0, 30.0);

                        normals = new Vector[1];
                        normals[0].I = 0.0;
                        normals[0].J = 0.0;
                        normals[0].K = 1.0;

                        triangles = new Triangle[1];
                        triangles[0].v1 = 0;
                        triangles[0].v2 = 1;
                        triangles[0].v3 = 2;
                        triangles[0].n = 0;
            */

            //CreateSquare();
            CreateCube();

        }

        private HitResult HitTestImpl(Camera camera, Point p1, Point p2, bool segmentOnly)
        {
            HitResult hitResult = new HitResult(null);

            double closest = Double.MaxValue;

            for (int i=0; i<triangles.Length; i++)
            {
                HitResult hr = HitTestTriangle(camera, i, p1, p2, segmentOnly);
                if (hr.HasEntry())
                {
                    double d = p1.Distance(hr.Entry);
                    if (d < closest)
                    {
                        closest = d;
                        hitResult = hr;
                    }
                }
            }

            return hitResult;
        }

        private HitResult HitTestTriangle(Camera camera, int index, Point p1, Point p2, bool segmentOnly)
        {
            HitResult hr = new HitResult(this);

            Point V1 = points[triangles[index].v1];
            Vector Normal = normals[triangles[index].n].Negate();

            Point vPos = camera.WorldToView * V1;
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
            Point Intersection = p1 + vecP2P1.Scale(mu);

            // We have a hit against the trianlge's plane, so see if the intersection point actually lies withhin the triangle itself.
            // From https://www.scratchapixel.com/lessons/3d-basic-rendering/ray-tracing-rendering-a-triangle/ray-triangle-intersection-geometric-solution

            V1 = vPos;
            Point V2 = camera.WorldToView * points[triangles[index].v2];
            Point V3 = camera.WorldToView * points[triangles[index].v3];

            Vector edge0 = V2 - V1;
            Vector edge1 = V3 - V2;
            Vector edge2 = V1 - V3;
            Vector C0 = Intersection - V1;
            Vector C1 = Intersection - V2;
            Vector C2 = Intersection - V3;

            if (vNormal.DotProduct(edge0.VectorProduct(C0)) < 0.0 &&
                vNormal.DotProduct(edge1.VectorProduct(C1)) < 0.0 &&
                vNormal.DotProduct(edge2.VectorProduct(C2)) < 0.0)
            {
                hr.Entry = Intersection;
                hr.EntryNormal = vNormal;
            }

            return hr;
        }

        public override HitResult HitTest(Camera camera, Ray ray)
        {
            bool segmentOnly = (ray.TMax == 1.0);

            //Debug.Assert(!segmentOnly);

            Point p2 = ray.Origin.Offset(ray.Direction);

            //return new HitResult(null);
            return HitTestImpl(camera, ray.Origin, p2, segmentOnly);
        }
    }
}
