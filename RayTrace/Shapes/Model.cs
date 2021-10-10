using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Media.Media3D;

namespace RayTrace
{
    //
    // Class containing the geometry and textures that comprise a 3D model.
    //
    public class ModelData
    {
        // Source file name if loaded from a file.
        public string fileName;

//        public TextureBitmap texture = null;
        public Texture texture = null;

        public struct Texel
        {
            public float u, v;
        }

        public class Triangle
        {
            public int v0, v1, v2;  // Indices into Point array.
            public int t0, t1, t2;  // Indexes into Texel array.
            public int n0, n1, n2;  // Indices into normal array.
            public int n;           // Index into normal array for triangle normal.
        }

        public SPoint[] points;
        public Texel[] texels;
        public SVector[] normals;
        public Triangle[] triangles;

        public float minX = 0.0f;
        public float minY = 0.0f;
        public float minZ = 0.0f;
        public float maxX = 0.0f;
        public float maxY = 0.0f;
        public float maxZ = 0.0f;

        // Constructor.
        public ModelData()
        {
        }

        public BoundingBox GetBoundingBox() => new BoundingBox(minX, maxX, minY, maxY, minZ, maxZ);

        public SPoint GetTrianglePointV0(int index) => points[triangles[index].v0];
        public SPoint GetTrianglePointV1(int index) => points[triangles[index].v1];
        public SPoint GetTrianglePointV2(int index) => points[triangles[index].v2];

        public SVector GetTriangleNormal0(int index) => normals[triangles[index].n0];
        public SVector GetTriangleNormal1(int index) => normals[triangles[index].n1];
        public SVector GetTriangleNormal2(int index) => normals[triangles[index].n2];

        public SVector GetTriangleNormal(int index) => normals[triangles[index].n];

        public Texel GetTriangleColour0(int index) => texels[triangles[index].t0];
        public Texel GetTriangleColour1(int index) => texels[triangles[index].t1];
        public Texel GetTriangleColour2(int index) => texels[triangles[index].t2];
    }

    //
    // Shape represented by triangles.
    //
    public class Model : Shape
    {
        private ModelData _modelData;

        public ModelData GetModelData() => _modelData;

        // Cached vertices and normal vectors converted to View space.
        private SPoint[] _viewPoints = null;
        private SVector[] _viewNormals = null;
        private BoundingBox _viewBoundingBox;

        // Root Node of space partition tree.
        private Node _root = null;

        // Utility functions to get pre-cached vertices.
        private SPoint GetTrianglePointV0(int index) => _viewPoints[_modelData.triangles[index].v0];
        private SPoint GetTrianglePointV1(int index) => _viewPoints[_modelData.triangles[index].v1];
        private SPoint GetTrianglePointV2(int index) => _viewPoints[_modelData.triangles[index].v2];

        // Utility functions to get pre-cached normals.
        public SVector GetTriangleNormal0(int index) => _viewNormals[_modelData.triangles[index].n0];
        public SVector GetTriangleNormal1(int index) => _viewNormals[_modelData.triangles[index].n1];
        public SVector GetTriangleNormal2(int index) => _viewNormals[_modelData.triangles[index].n2];

        // If normal index is set to -1, assume that there are no vertex normals for this triangle.
        public bool HasVectorNormal(int index) => _modelData.triangles[index].n0 != -1;

        public SVector GetTriangleNormal(int index) => _viewNormals[_modelData.triangles[index].n];

        public float MinX => _modelData.minX;
        //public double MinY => _modelData.minY;
        public float MinY { get { BoundingBox bb = _modelData.GetBoundingBox(); BoundingBox bb2 = bb.Transform(ObjectToWorld); return bb2.MinY; } }
        public float MinZ => _modelData.minZ;
        public float MaxX => _modelData.maxX;
        public float MaxY => _modelData.maxY;
        public float MaxZ => _modelData.maxZ;

        private class FatTriangle
        {
            public SPoint v0, v1, v2;
            public ModelData.Texel t0, t1, t2;
            public SVector n0, n1, n2;
            public SVector n;

            //public int v0, v1, v2;  // Indices into Point array.
            //public int t0, t1, t2;  // Indexes into Texel array.
            //public int n0, n1, n2;  // Indices into normal array.
            //public int n;           // Index into normal array for triangle normal.
        }

        private FatTriangle[] fatTriangles;

        private SVector GetNormal(SPoint P0, SPoint P1, SPoint P2)
        {
            SVector v1 = P1 - P0;
            SVector v2 = P2 - P0;

            SVector normal = v1.VectorProduct(v2);

            return normal.Normalise();
        }

        // https://www.scratchapixel.com/lessons/3d-basic-rendering/ray-tracing-rendering-a-triangle/barycentric-coordinates
        // Return u, v (w = 1 - u - v)
        private (float, float) GetBarymetricCoords(SPoint P0, SPoint P1, SPoint P2, SPoint hit)
        {
            // Get triangle area.
            SVector p0p1 = P1 - P0;
            SVector p0p2 = P2 - P0;

            SVector N = p0p1.VectorProduct(p0p2);
            float area = N.Magnitude() / 2.0f;

            // CAP
            SVector p2p0 = P0 - P2;
            SVector p2hit = hit - P2;
            N = p2p0.VectorProduct(p2hit);
            float u = N.Magnitude() / 2.0f / area;

            // ABP
            //Vector p0p1 = P1 - P0;
            SVector hitP1 = hit - P0;
            N = p0p1.VectorProduct(hitP1);
            float v = N.Magnitude() / 2.0f / area;

            // BCP
            //double w = 1.0 - u - v;

            //return new Vector(u, v, w);

            Debug.Assert(u <= 1.0f);
            Debug.Assert(v <= 1.0f);

            return (u, v);
        }

        // n0, n1, n2   Vector normals for triangle.
        // u, v         Barycentric coordinates of hit point.
        private SVector GetNormal(SVector n0, SVector n1, SVector n2, float u, float v)
        {
            SVector N = n0*(1.0f - u - v) + n1*u + n2*v;

            return N.Normalise();
        }

        // c0, c1, c2   Colours for triangle.
        // u, v         Barycentric coordinates of hit point.
        private ModelData.Texel GetColour(ModelData.Texel c0, ModelData.Texel c1, ModelData.Texel c2, float u, float v)
        {
            float texelU = c0.u * (1.0f - u - v) + c1.u * u + c2.u * v;
            float texelV = c0.v * (1.0f - u - v) + c1.v * u + c2.v * v;

            //texelU = Math.Min(texelU, 1.0f);
            //texelV = Math.Min(texelV, 1.0f);

            //Debug.Assert(texelU <= 1.0);
            //Debug.Assert(texelV <= 1.0);


            return new ModelData.Texel() { u = texelU, v = texelV };
        }

        // Constructor.
        public Model(Point p, ModelData modelData) : base(p)
        {
            _modelData = modelData;

//            Matrix rotate = Matrix.GetRotationY((float) Math.PI/2);
            Matrix rotate = Matrix.GetRotationX((float)Math.PI / 2);
            Matrix scale = Matrix.GetScale(2.0f, 2.0f, 2.0f);
            Matrix translate = Matrix.GetTranslation(0.0f, 0.0f, 10.0f);

            //rotate = Matrix.GetRotationY((float)Math.PI / 2) * rotate;

            //ObjectToWorld = rotate * scale * translate;

            //ObjectToWorld = scale * translate;
            //ObjectToWorld = scale;
            //ObjectToWorld = rotate * scale * translate;
            //ObjectToWorld = rotate;
        }

        public Model(XMLModel xmlModel) : base(xmlModel)
        {
            _modelData = new ModelData();
/*
            if (String.IsNullOrEmpty(xmlModel.FileName))
            {

                _modelData.fileName = xmlModel.FileName;

                List<Point> points = new List<Point>();
                foreach (XMLPoint p in xmlModel.Points)
                {
                    points.Add(new Point(p));
                }
                _modelData.points = points.ToArray();

                List<ModelData.Texel> texels = new List<ModelData.Texel>();
                foreach (XMLTexel t in xmlModel.Texels)
                {
                    texels.Add(new ModelData.Texel() { u = t.u, v = t.v });
                }
                _modelData.texels = texels.ToArray();

                List<Vector> Normals = new List<Vector>();
                foreach (XMLVector n in xmlModel.Normals)
                {
                    Normals.Add(new Vector() { I = n.I, J = n.J, K = n.K });
                }
                _modelData.normals = Normals.ToArray();

                List<ModelData.Triangle> Triangles = new List<ModelData.Triangle>();
                foreach (XMLTriangle t in xmlModel.Triangles)
                {
                    Triangles.Add(new ModelData.Triangle() { v0 = t.v0, v1 = t.v1, v2 = t.v2, t0 = t.t0, t1 = t.t1, t2 = t.t2, n0 = t.n0, n1 = t.n1, n2 = t.n2, n = t.n });
                }
                _modelData.triangles = Triangles.ToArray();

                _modelData.texture = xmlModel.Texure.Create();
            }
            else
            {
                ModelImporter importer = new ModelImporter();
                _modelData = importer.ImportObj(xmlModel.FileName);
            }
*/
        }

        // Transform all vertices to View space based on current Camera position and orientation.
        public override void CreateCache(Camera camera)
        {
            if (_modelData != null)
            {
                // Convert vertices from Model space to View space.
                Matrix ObjectToView = camera.WorldToView * ObjectToWorld;

                _viewPoints = new SPoint[_modelData.points.Length];

                for (int i = 0; i < _modelData.points.Length; i++)
                {
                    _viewPoints[i] = ObjectToView * _modelData.points[i];
                }

                Matrix ObjectToViewNormal = camera.NormalMatrix * ObjectToWorldNormal;

                _viewNormals = new SVector[_modelData.normals.Length];

                for (int i=0; i<_modelData.normals.Length; i++)
                {
                    _viewNormals[i] = ObjectToViewNormal * _modelData.normals[i];
                }

                BoundingBox bb = _modelData.GetBoundingBox();
                _viewBoundingBox = bb.Transform(ObjectToView);

                TreeMaker treeMaker = new TreeMaker();
                _root = treeMaker.MakeTree(_modelData, ObjectToView);

                // Populate the fat triangle list.
                fatTriangles = new FatTriangle[_modelData.triangles.Length];

                for (int i=0; i<_modelData.triangles.Length; i++)
                {
                    FatTriangle fatTriangle = new FatTriangle();
                    fatTriangle.v0 = GetTrianglePointV0(i);
                    fatTriangle.v1 = GetTrianglePointV1(i);
                    fatTriangle.v2 = GetTrianglePointV2(i);

                    fatTriangle.n0 = GetTriangleNormal0(i);
                    fatTriangle.n1 = GetTriangleNormal1(i);
                    fatTriangle.n2 = GetTriangleNormal2(i);

                    fatTriangle.n = GetTriangleNormal(i).Negate();

                    fatTriangle.t0 = _modelData.GetTriangleColour0(i);
                    fatTriangle.t1 = _modelData.GetTriangleColour1(i);
                    fatTriangle.t2 = _modelData.GetTriangleColour2(i);

                    fatTriangles[i] = fatTriangle;
                }
            }
        }

        // Recurse through partition tree, checking for intersections in boxes that that ray passees through.
        private void HitTestRecurse(List<HitResult> hitResults, Node node, SPoint p1, SPoint p2, bool segmentOnly, bool firstHitOnly)
        {
            if (firstHitOnly && hitResults.Count > 0)
            {
                return;
            }

            HitResult hr = new HitResult(this);

            // Does ray passes through us?
            if (node.Box.Intersects(p1, p2))
            {
                SVector ray = p2 - p1;

                if (node.Left == null && node.Right == null)
                {
                    // We're a leaf node, so test our triangles.
                    foreach (int i in node.Triangles)
                    {
                        hr.Reset();
                        /*HitResult hr =*/ HitTestTriangle(ref hr, i, p1, p2, ray, segmentOnly);
                        if (hr.HasEntry())
                        {
                            hitResults.Add(hr);
                        }

                    }
                    return;
                }

                if (node.Left != null)
                {
                    HitTestRecurse(hitResults, node.Left, p1, p2, segmentOnly, firstHitOnly);
                }
                if (node.Right != null)
                {
                    HitTestRecurse(hitResults, node.Right, p1, p2, segmentOnly, firstHitOnly);
                }
            }
        }

        private HitResult HitTestImpl(Camera camera, SPoint p1, SPoint p2, bool segmentOnly, bool firstHitOnly)
        {
            HitResult hitResult = new HitResult(null);

            Node node = _root;
            List<HitResult> hitResults = new List<HitResult>();
            HitTestRecurse(hitResults, node, p1, p2, segmentOnly, firstHitOnly);

            if (hitResults.Count > 0)
            {
                double closest2 = Double.MaxValue;

                foreach (HitResult hr in hitResults)
                {
                    if (hr.HasEntry())
                    {
                        double d = p1.Distance(hr.Entry);
                        if (d < closest2)
                        {
                            closest2 = d;
                            hitResult = hr;
                        }
                    }
                }

                return hitResult;

            }

            return new HitResult(this);





/*
            Matrix ObjectToView = camera.WorldToView * ObjectToWorld;

//            BoundingBox bb = _modelData.GetBoundingBox();
//            BoundingBox bb2 = bb.Transform(ObjectToView);

            if (!_viewBoundingBox.Intersects(p1, p2))
            {
                return hitResult;
            }

            Matrix ObjectToViewNormal = camera.NormalMatrix * ObjectToWorldNormal;

            double closest = Double.MaxValue;

            for (int i=0; i<_modelData.triangles.Length; i++)
            {
                try
                {
                    HitResult hr = HitTestTriangle(ObjectToView, ObjectToViewNormal, i, p1, p2, segmentOnly);
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
*/
        }

        private HitResult HitTestTriangle(ref HitResult hr, int index, in SPoint p1, in SPoint p2, in SVector ray, bool segmentOnly)
        {
            //HitResult hr = new HitResult(this);
            /*
                        // XXX Hacky test against exceptions.
                        if (_modelData.points.Length <= _modelData.triangles[index].v0) return hr;
                        if (_modelData.points.Length <= _modelData.triangles[index].v1) return hr;
                        if (_modelData.points.Length <= _modelData.triangles[index].v2) return hr;
                        if (_modelData.normals.Length <= _modelData.triangles[index].n) return hr;
            */

            //Vector Normal = _modelData.GetTriangleNormal(index).Negate();

            //ModelData.Triangle tri = _modelData.triangles[index];
            FatTriangle tri = fatTriangles[index];

            //SPoint vPos = GetTrianglePointV0(index);
            //SPoint vPos = _viewPoints[tri.v0];
            SPoint vPos = tri.v0;

            // SVector vNormal = GetTriangleNormal(index).Negate();

            //SVector vNormal = _viewNormals[tri.n].Negate();
            SVector vNormal = tri.n;//.Negate();

            // Check if face is pointing away from us.
            //SVector ray = p2 - p1;
            float dp = vNormal.DotProduct(ray);
            if (dp > 0.0f) return hr;    // Check >0 since vectors point in opposite directions.

            float A = vNormal.I;
            float B = vNormal.J;
            float C = vNormal.K;

            // Since Ax + By + Cz + D = 0...
            float D = -(A * vPos.X + B * vPos.Y + C * vPos.Z);
//            float D = -(A * tri.v0.X + B * tri.v0.Y + C * tri.v0.Z);

            float num = A * p1.X + B * p1.Y + C * p1.Z + D;
            float denom = A * (p1.X - p2.X) + B * (p1.Y - p2.Y) + C * (p1.Z - p2.Z);

            if (denom == 0)
            {
                return hr;
            }

            float mu = num / denom;

            if (mu == 0.0)
            {
                // Either parallel to plane or on the plane - treat both as no intersection for our purposes.
                return hr;
            }

            float THRESH = 0.000001f;

            if (mu < THRESH)
            {
                // Not interested in intersections that happen behind us.
                return hr;
            }

            if (segmentOnly)
            {
                // Check if segment actually intersects us.
                if (mu < THRESH || mu > 1.0f - THRESH)
                {
                    return hr;
                }
            }

            SVector vecP2P1 = p2 - p1;
            SPoint Intersection = p1 + vecP2P1.Scale(mu);

            // We have a hit against the trianlge's plane, so see if the intersection point actually lies within the triangle itself.
            // From https://www.scratchapixel.com/lessons/3d-basic-rendering/ray-tracing-rendering-a-triangle/ray-triangle-intersection-geometric-solution

            //SPoint V0 = vPos;
            SPoint V0 = tri.v0;

            //SPoint V1 = GetTrianglePointV1(index);
            //SPoint V2 = GetTrianglePointV2(index);
            //SPoint V1 = _viewPoints[tri.v1];
            //SPoint V2 = _viewPoints[tri.v2];
            SPoint V1 = tri.v1;
            SPoint V2 = tri.v2;

            SVector edge0 = V1 - V0;
            SVector edge1 = V2 - V1;
            SVector edge2 = V0 - V2;
            SVector C0 = Intersection - V0;
            SVector C1 = Intersection - V1;
            SVector C2 = Intersection - V2;

            if (vNormal.DotProduct(edge0.VectorProduct(C0)) <= 0.0 &&
                vNormal.DotProduct(edge1.VectorProduct(C1)) <= 0.0 &&
                vNormal.DotProduct(edge2.VectorProduct(C2)) <= 0.0)
            {
                hr.Entry = Intersection;
                hr.EntryNormal = vNormal;

                var uv = GetBarymetricCoords(V0, V1, V2, Intersection);
                Debug.Assert(uv.Item1 <= 1.0);
                Debug.Assert(uv.Item2 <= 1.0);

                hr.u = (float) uv.Item1;
                hr.v = (float) uv.Item2;

                //Vector n0 = _modelData.GetTriangleNormal0(index);
                //Vector n1 = _modelData.GetTriangleNormal1(index);
                //Vector n2 = _modelData.GetTriangleNormal2(index);

                if (HasVectorNormal(index))
                {
                    //SVector n0 = GetTriangleNormal0(index);
                    //SVector n1 = GetTriangleNormal1(index);
                    //SVector n2 = GetTriangleNormal2(index);

                    SVector n0 = tri.n0;
                    SVector n1 = tri.n1;
                    SVector n2 = tri.n2;


                    SVector N = GetNormal(n0, n1, n2, uv.Item1, uv.Item2);
                    //N = ObjectToViewNormal * N; // XXX Check normal is being treated correctly re. transforms.
                    hr.EntryNormal = N;
                }

                hr.Index = index;
            }

            return hr;
        }

        public override HitResult HitTest(Camera camera, Ray ray)
        {
            bool segmentOnly = (ray.TMax == 1.0);

            SPoint p2 = ray.Origin.Offset(ray.Direction);

            return HitTestImpl(camera, ray.Origin, p2, segmentOnly, false);
        }

        public override bool HitTestQuick(Camera camera, Ray ray)
        {
            bool segmentOnly = (ray.TMax == 1.0);

            SPoint _p2 = ray.Origin.Offset(ray.Direction);
            SPoint p2 = _p2;

            HitResult hr = HitTestImpl(camera, ray.Origin, p2, segmentOnly, true);
            return hr.HasEntry();

            //bool segmentOnly = (ray.TMax == 1.0);

            SPoint p1 = ray.Origin;
            //Point p2 = ray.Origin.Offset(ray.Direction);

            Matrix ObjectToView = camera.WorldToView * ObjectToWorld;

            //BoundingBox bb = _modelData.GetBoundingBox();
            //BoundingBox bb2 = bb.Transform(ObjectToView);

            //if (!bb2.Intersects(p1, p2))
            if (!_viewBoundingBox.Intersects(p1, p2))
            {
                return false;
            }

            //Matrix ObjectToViewNormal = camera.NormalMatrix * ObjectToWorldNormal;

            for (int i = 0; i < _modelData.triangles.Length; i++)
            {
                //Vector Normal = _modelData.GetTriangleNormal(i).Negate();

                //Point vPos = ObjectToView * _modelData.GetTrianglePointV0(i);
                //Vector vNormal = ObjectToViewNormal * Normal;

                SPoint vPos = GetTrianglePointV0(i);
                SVector vNormal = GetTriangleNormal(i).Negate();

                float A = vNormal.I;
                float B = vNormal.J;
                float C = vNormal.K;

                // Since Ax + By + Cz + D = 0...
                float D = -(A * vPos.X + B * vPos.Y + C * vPos.Z);

                float num = A * p1.X + B * p1.Y + C * p1.Z + D;
                float denom = A * (p1.X - p2.X) + B * (p1.Y - p2.Y) + C * (p1.Z - p2.Z);

                if (denom == 0)
                {
                    //return hr;
                    continue;
                }

                float mu = num / denom;

                if (mu == 0.0)
                {
                    // Either parallel to plane or on the plane - treat both as no intersection for our purposes.
                    //return hr;
                    continue;
                }

                double THRESH = 0.000001;

                if (mu < THRESH)
                {
                    // Not interested in intersections that happen behind us.
                    //return hr;
                    continue;
                }

                if (segmentOnly)
                {
                    // Check if segment actually intersects us.
                    if (mu < THRESH || mu > 1.0 - THRESH)
                    {
                        //return hr;
                        continue;
                    }
                }

                SVector vecP2P1 = p2 - p1;
                SPoint Intersection = p1 + vecP2P1.Scale(mu);

                // We have a hit against the trianlge's plane, so see if the intersection point actually lies within the triangle itself.
                // From https://www.scratchapixel.com/lessons/3d-basic-rendering/ray-tracing-rendering-a-triangle/ray-triangle-intersection-geometric-solution

                SPoint V0 = vPos;
                //Point V1 = ObjectToView * _modelData.GetTrianglePointV1(i);
                //Point V2 = ObjectToView * _modelData.GetTrianglePointV2(i);
                SPoint V1 = GetTrianglePointV1(i);
                SPoint V2 = GetTrianglePointV2(i);

                SVector edge0 = V1 - V0;
                SVector edge1 = V2 - V1;
                SVector edge2 = V0 - V2;
                SVector C0 = Intersection - V0;
                SVector C1 = Intersection - V1;
                SVector C2 = Intersection - V2;

                if (vNormal.DotProduct(edge0.VectorProduct(C0)) < 0.0 &&
                    vNormal.DotProduct(edge1.VectorProduct(C1)) < 0.0 &&
                    vNormal.DotProduct(edge2.VectorProduct(C2)) < 0.0)
                {
                    return true;
                }
            }

            return false;
        }

        public override Colour GetColour(HitResult hr)
        {
//            TextureBitmap textureBitmap = _modelData.texture as TextureBitmap;
            if (Material.Texture is TextureBitmap textureBitmap)
            {
                ModelData.Texel t0 = _modelData.GetTriangleColour0(hr.Index);
                ModelData.Texel t1 = _modelData.GetTriangleColour1(hr.Index);
                ModelData.Texel t2 = _modelData.GetTriangleColour2(hr.Index);

                ModelData.Texel texel = GetColour(t0, t1, t2, hr.u, hr.v);

                //hr.u = texel.u;
                //hr.v = texel.v;

                return textureBitmap.GetColour(texel.u, texel.v);
            }
            else
            {
//                return _modelData.texture.GetColour(hr);
                return Material.Texture.GetColour(hr);
            }

            //return Palette.Grey;
        }

        protected override void AsXMLImpl(XMLEntity xml)
        {
            base.AsXMLImpl(xml);
/*
            if (xml is XMLModel xmlModel)
            {
                xmlModel.FileName = _modelData.fileName;

                if (String.IsNullOrEmpty(_modelData.fileName))
                {
                    // Assume model data created by hand, so write out vertex geometry.
                    xmlModel.Points = new List<XMLPoint>();
                    if (_modelData.points != null)
                    {
                        foreach (Point p in _modelData.points)
                        {
                            xmlModel.Points.Add(new XMLPoint() { X = p.X, Y = p.Y, Z = p.Z });
                        }
                    }
                    xmlModel.Texels = new List<XMLTexel>();
                    if (_modelData.texels != null)
                    {
                        foreach (ModelData.Texel t in _modelData.texels)
                        {
                            xmlModel.Texels.Add(new XMLTexel() { u = t.u, v = t.v });
                        }
                    }
                    xmlModel.Normals = new List<XMLVector>();
                    if (_modelData.normals != null)
                    {
                        foreach (Vector v in _modelData.normals)
                        {
                            xmlModel.Normals.Add(new XMLVector() { I = v.I, J = v.J, K = v.K });
                        }
                    }
                    xmlModel.Triangles = new List<XMLTriangle>();
                    if (_modelData.triangles != null)
                    {
                        foreach (ModelData.Triangle t in _modelData.triangles)
                        {
                            xmlModel.Triangles.Add(new XMLTriangle() { v0 = t.v0, v1 = t.v1, v2 = t.v2, t0 = t.t0, t1 = t.t1, t2 = t.t2, n0 = t.n0, n1 = t.n1, n2 = t.n2, n = t.n });
                        }
                    }
                }

                if (_modelData.texture != null)
                {
                    xmlModel.Texure = _modelData.texture.ToXML();
                }
            }
*/
        }

        public override XMLEntity ToXML()
        {
            XMLModel xml = new XMLModel();
            AsXMLImpl(xml);
            return xml;
        }
    }
}
