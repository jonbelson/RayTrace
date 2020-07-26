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
    public class ModelData
    {
        public struct Texel
        {
            public float u, v;
        }

        public class /*struct*/ Triangle
        {
            public int v0, v1, v2;  // Indices into Point array.
            public int t0, t1, t2;  // Indexes into Texel array.
            public int n;           // Index into normal array.
            //public Triangle(int v1, int v2, int v3)
            //{
            //    this.v1 = v1;
            //    this.v2 = v2;
            //    this.v3 = v3;
            //}
        }

        public Point[] points;
        public Texel[] texels;
        public Vector[] normals;
        public Triangle[] triangles;

        public double minX = 0.0;
        public double minY = 0.0;
        public double minZ = 0.0;
        public double maxX = 0.0;
        public double maxY = 0.0;
        public double maxZ = 0.0;

        public ModelData()
        {
        }

        public Point GetTrianglePointV0(int index) => points[triangles[index].v0];
        public Point GetTrianglePointV1(int index) => points[triangles[index].v1];
        public Point GetTrianglePointV2(int index) => points[triangles[index].v2];
        public Vector GetTriangleNormal(int index) => normals[triangles[index].n];
    }

    //
    // Shape represented by triangles.
    //
    public class Model : Shape
    {
        private ModelData _modelData;

        public double MinX => _modelData.minX;
        public double MinY => _modelData.minY;
        public double MinZ => _modelData.minZ;
        public double MaxX => _modelData.maxX;
        public double MaxY => _modelData.maxY;
        public double MaxZ => _modelData.maxZ;

        private Vector GetNormal(Point P0, Point P1, Point P2)
        {
            Vector v1 = P1 - P0;
            Vector v2 = P2 - P0;

            Vector normal = v1.VectorProduct(v2);

            return normal.Normalise();
        }

        public Model(Point p, ModelData modelData) : base(p)
        {
            _modelData = modelData;

            Matrix rotate = Matrix.GetRotationY((float) Math.PI);
            Matrix scale = Matrix.GetScale(2.0, 2.0, 2.0);
            Matrix translate = Matrix.GetTranslation(0.0, -5.0, 10.0);

            //            ObjectToWorld = scale * translate;
            //ObjectToWorld = scale;
            //ObjectToWorld = rotate * scale * translate;
            //ObjectToWorld = rotate;
        }

        private HitResult HitTestImpl(Camera camera, Point p1, Point p2, bool segmentOnly)
        {
            HitResult hitResult = new HitResult(null);

            double closest = Double.MaxValue;

            for (int i=0; i<_modelData.triangles.Length; i++)
            {
                try
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
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            return hitResult;
        }

        private HitResult HitTestTriangle(Camera camera, int index, Point p1, Point p2, bool segmentOnly)
        {
            HitResult hr = new HitResult(this);

            //Point V0 = _modelData.points[_modelData.triangles[index].v0];
            //Vector Normal = _modelData.normals[_modelData.triangles[index].n].Negate();

            // XXX Hacky test against exceptions.
            if (_modelData.points.Length <= _modelData.triangles[index].v0) return hr;
            if (_modelData.points.Length <= _modelData.triangles[index].v1) return hr;
            if (_modelData.points.Length <= _modelData.triangles[index].v2) return hr;
            if (_modelData.normals.Length <= _modelData.triangles[index].n) return hr;


            Point V0 = _modelData.GetTrianglePointV0(index);
            Vector Normal = _modelData.GetTriangleNormal(index).Negate();
            //Vector Normal = _modelData.GetTriangleNormal(index);

            V0 = ObjectToWorld * V0;

            Point vPos = camera.WorldToView * V0;
            Vector vNormal = camera.NormalMatrix * Normal;


            Point v0 = camera.WorldToView * _modelData.GetTrianglePointV0(index);
            Point v1 = camera.WorldToView * _modelData.GetTrianglePointV1(index);
            Point v2 = camera.WorldToView * _modelData.GetTrianglePointV2(index);

            vNormal = GetNormal(v0, v1, v2);
            vNormal = vNormal.Negate();

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

            V0 = vPos;
            Point V1 = ObjectToWorld * camera.WorldToView * _modelData.GetTrianglePointV1(index);
            Point V2 = ObjectToWorld * camera.WorldToView * _modelData.GetTrianglePointV2(index);

            Vector edge0 = V1 - V0;
            Vector edge1 = V2 - V1;
            Vector edge2 = V0 - V2;
            Vector C0 = Intersection - V0;
            Vector C1 = Intersection - V1;
            Vector C2 = Intersection - V2;

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
