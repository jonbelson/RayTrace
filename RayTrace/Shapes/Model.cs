using System;
using System.Collections.Generic;
using System.ComponentModel;
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
            public float u, v, w;
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

        public class SubGroup
        {
            public bool Enabled { get; set; } = true;
            public string Name { get; set; }

            public int Material { get; set; }
            public List<int> Triangles { get; set; } = new();
        }

        public SubGroup[] SubGroups { get; set; }

        public float minX = 0.0f;
        public float minY = 0.0f;
        public float minZ = 0.0f;
        public float maxX = 0.0f;
        public float maxY = 0.0f;
        public float maxZ = 0.0f;

        public bool HasVertexNormals { get; set; } = false;

        //
        // Constructor.
        //
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

        public Texel GetTriangleTexel0(int index) => texels[triangles[index].t0];
        public Texel GetTriangleTexel1(int index) => texels[triangles[index].t1];
        public Texel GetTriangleTexel2(int index) => texels[triangles[index].t2];
    }

    //
    // Shape represented by triangles.
    //
    public class Model : Shape
    {
        private ModelData _modelData;

        public ModelData GetModelData() => _modelData;

        //public List<Model> ChildModels { get; set; }

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

        public Node TreeRoot { get => _root; }

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

            public int subGroup;

            //public int v0, v1, v2;  // Indices into Point array.
            //public int t0, t1, t2;  // Indexes into Texel array.
            //public int n0, n1, n2;  // Indices into normal array.
            //public int n;           // Index into normal array for triangle normal.
        }

        private FatTriangle[] _fatTriangles;

        private ModelSubGroup[] _subGroupArray;

        Dictionary<string, ModelSubGroup> _subGroups = new();

        private void UpdateSubGroups()
        {
            if (_modelData != null && _modelData.SubGroups != null)
            {
                _subGroups.Clear();

                foreach (ModelData.SubGroup group in _modelData.SubGroups)
                {
//                    ModelSubGroup subGroup = new(this, group) { Material = this.Material };
                    ModelSubGroup subGroup = new(this, group) { Material = Scene.GetDefaultMaterial() };    // XXX Until subgroup Material GUIDs are saved.

                    _subGroups.Add(group.Name, subGroup);
                }
            }
        }

        private static SVector GetNormal(SPoint P0, SPoint P1, SPoint P2)
        {
            SVector v1 = P1 - P0;
            SVector v2 = P2 - P0;

            SVector normal = v1.VectorProduct(v2);

            return normal.Normalise();
        }

        // https://www.scratchapixel.com/lessons/3d-basic-rendering/ray-tracing-rendering-a-triangle/barycentric-coordinates
        // Return u, v (w = 1 - u - v)
        private static (float, float) GetBarymetricCoords(SPoint P0, SPoint P1, SPoint P2, SPoint hit)
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
        private static SVector GetNormal(SVector n0, SVector n1, SVector n2, float u, float v)
        {
            SVector N = n0*(1.0f - u - v) + n1*u + n2*v;

            return N.Normalise();
        }

        // c0, c1, c2   Colours for triangle.
        // u, v         Barycentric coordinates of hit point.
        private static ModelData.Texel GetTexel(ModelData.Texel c0, ModelData.Texel c1, ModelData.Texel c2, float u, float v)
        {
            float texelU = c0.u * (1.0f - u - v) + c1.u * u + c2.u * v;
            float texelV = c0.v * (1.0f - u - v) + c1.v * u + c2.v * v;

            //texelU = Math.Min(texelU, 1.0f);
            //texelV = Math.Min(texelV, 1.0f);

            //Debug.Assert(texelU <= 1.0);
            //Debug.Assert(texelV <= 1.0);


            return new ModelData.Texel() { u = texelU, v = texelV };
        }

        public override void AddPropertyChangedHandler(PropertyChangedEventHandler eventHandler)
        {
            PropertyChanged += eventHandler;
            foreach (var subGroup in _subGroups.Values)
            {
                subGroup.AddPropertyChangedHandler(eventHandler);
            }
        }

        public override Material GetMaterial(HitResult hr)
        {
            Material material = new();

            int index = hr.Index;
            if (index >= 0 && index < _fatTriangles.Length)
            {
                FatTriangle fatTriangle = _fatTriangles[index];
                if (fatTriangle != null)
                {
                    int subGroup = fatTriangle.subGroup;
                    if (subGroup >= 0 && subGroup < _subGroupArray.Length)
                    {
                        Material subMaterial = _subGroupArray[subGroup].Material;
                        if (subMaterial != null) material = subMaterial;
                    }
                }
            }

            return material;
        }


        //
        // Constructor.
        //
        public Model(Scene scene, Point p, ModelData modelData, bool centreModel=false) : base(scene, p)
        {
            _modelData = modelData;

            if (centreModel && modelData != null)
            {
                BoundingBox bb = modelData.GetBoundingBox();

                Pos.X = -bb.GetCentreX();
                Pos.Y = -bb.MinY;
                Pos.Z = -bb.GetCentreZ();
            }

            UpdateSubGroups();
        }

        //
        // Constructor.
        //
        public Model(Scene scene, XMLModel xmlModel) : base(scene, xmlModel)
        {
            _modelData = new ModelData();

            if (string.IsNullOrEmpty(xmlModel.FileName))
            {
                _modelData.fileName = xmlModel.FileName;

                List<SPoint> points = new List<SPoint>();
                foreach (XMLPoint p in xmlModel.Points)
                {
                    points.Add(new SPoint(p.X, p.Y, p.Z));
                }
                _modelData.points = points.ToArray();

                List<ModelData.Texel> texels = new List<ModelData.Texel>();
                foreach (XMLTexel t in xmlModel.Texels)
                {
                    texels.Add(new ModelData.Texel() { u = t.u, v = t.v });
                }
                _modelData.texels = texels.ToArray();

                List<SVector> Normals = new List<SVector>();
                foreach (XMLVector n in xmlModel.Normals)
                {
                    Normals.Add(new SVector(n.I, n.J, n.K));
                }
                _modelData.normals = Normals.ToArray();

                List<ModelData.Triangle> Triangles = new List<ModelData.Triangle>();
                foreach (XMLTriangle t in xmlModel.Triangles)
                {
                    Triangles.Add(new ModelData.Triangle() { v0 = t.v0, v1 = t.v1, v2 = t.v2, t0 = t.t0, t1 = t.t1, t2 = t.t2, n0 = t.n0, n1 = t.n1, n2 = t.n2, n = t.n });
                }
                _modelData.triangles = Triangles.ToArray();

                //_modelData.texture = xmlModel.Texure.Create();
            }
            else
            {
                ModelImporter importer = new();
                ModelData modelData = importer.ImportObj(xmlModel.FileName);

                _modelData = modelData;
                //Material.Texture = objData.Item2;
            }

            UpdateSubGroups();
        }

        // Transform all vertices to View space based on current Camera position and orientation.
        public override void CreateCache(Camera camera)
        {
            if (_modelData != null && _modelData.points != null && _modelData.normals != null)
            {
                updateMatrix = true;

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

                _subGroupArray = new ModelSubGroup[_modelData.SubGroups.Length];
                int idx = 0;
                foreach (var group in _subGroups)
                {
                    _subGroupArray[idx++] = group.Value;
                }
                    
                BoundingBox bb = _modelData.GetBoundingBox();
                _viewBoundingBox = bb.Transform(ObjectToView);

                TreeMaker treeMaker = new TreeMaker();
                _root = treeMaker.MakeTree(_modelData, ObjectToView);

                //string graphviz = CreateGraphviz();

                // Populate the fat triangle list.
                _fatTriangles = new FatTriangle[_modelData.triangles.Length];

                idx = 0;
                //foreach (var group in _modelData.SubGroups)
                for (int i=0; i<_subGroupArray.Length; i++)
                {
                    ModelSubGroup group = _subGroupArray[i];
                    for (int tri = 0; tri < group.Group.Triangles.Count; tri++)
                    {
                        FatTriangle fatTriangle = new();
                        fatTriangle.v0 = GetTrianglePointV0(idx);
                        fatTriangle.v1 = GetTrianglePointV1(idx);
                        fatTriangle.v2 = GetTrianglePointV2(idx);

                        fatTriangle.subGroup = i;

                        if (_modelData.HasVertexNormals)
                        {
                            fatTriangle.n0 = GetTriangleNormal0(idx);
                            fatTriangle.n1 = GetTriangleNormal1(idx);
                            fatTriangle.n2 = GetTriangleNormal2(idx);
                        }
                        else
                        {
                            fatTriangle.n0 = GetTriangleNormal(idx);
                            fatTriangle.n1 = GetTriangleNormal(idx);
                            fatTriangle.n2 = GetTriangleNormal(idx);
                        }

                        fatTriangle.n = GetTriangleNormal(idx);//.Negate(); XXX

                        fatTriangle.t0 = _modelData.GetTriangleTexel0(idx);
                        fatTriangle.t1 = _modelData.GetTriangleTexel1(idx);
                        fatTriangle.t2 = _modelData.GetTriangleTexel2(idx);

                        _fatTriangles[idx++] = fatTriangle;
                    }
                }

                Debug.Assert(idx == _modelData.triangles.Length);
            }
        }

        // Recurse through partition tree, checking for intersections in boxes that that ray passees through.
        private void HitTestRecurse(Camera camera, List<HitResult> hitResults, Node node, SPoint p1, SPoint p2, bool segmentOnly, bool firstHitOnly)
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
                        /*HitResult hr =*/ HitTestTriangle(camera, ref hr, i, p1, p2, ray, segmentOnly);
                        if (hr.HasEntry())
                        {
                            hitResults.Add(hr);
                        }

                    }
                    return;
                }

                if (node.Left != null)
                {
                    HitTestRecurse(camera, hitResults, node.Left, p1, p2, segmentOnly, firstHitOnly);
                }
                if (node.Right != null)
                {
                    HitTestRecurse(camera, hitResults, node.Right, p1, p2, segmentOnly, firstHitOnly);
                }
            }
        }

        private HitResult HitTestImpl(Camera camera, SPoint p1, SPoint p2, bool segmentOnly, bool firstHitOnly)
        {
            HitResult hitResult = new(null);

            Node node = _root;

            if (node != null)
            {
                List<HitResult> hitResults = new List<HitResult>();

                HitTestRecurse(camera, hitResults, node, p1, p2, segmentOnly, firstHitOnly);

                if (hitResults.Count > 0)
                {
                    double closest2 = Double.MaxValue;

                    foreach (HitResult hr in hitResults)
                    {
                        if (hr.HasEntry())
                        {
                            double d = hr.t;
                            if (d < closest2)
                            {
                                closest2 = d;
                                hitResult = hr;
                            }
                        }
                    }

                    CalculateTexelAndNormal(camera, ref hitResult);
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

        private bool CalculateTexelAndNormal(Camera camera, ref HitResult hr)
        {
            int index = hr.Index;

            SPoint intersection = hr.Entry;
            FatTriangle tri = _fatTriangles[index];

            SPoint V0 = tri.v0;
            SPoint V1 = tri.v1;
            SPoint V2 = tri.v2;

            (float, float) uv = GetBarymetricCoords(V0, V1, V2, intersection);
            Debug.Assert(uv.Item1 <= 1.0);
            Debug.Assert(uv.Item2 <= 1.0);

            // Use barycentric coordinates to get texel coordinates.
            ModelData.Texel t0 = _modelData.GetTriangleTexel0(index);
            ModelData.Texel t1 = _modelData.GetTriangleTexel1(index);
            ModelData.Texel t2 = _modelData.GetTriangleTexel2(index);

            ModelData.Texel texel = GetTexel(t0, t1, t2, uv.Item1, uv.Item2);
            hr.u = texel.u;
            hr.v = texel.v;

            // Use barycentric coordinates to get normal vector.
            SVector n0 = tri.n0;
            SVector n1 = tri.n1;
            SVector n2 = tri.n2;

            // Get the triangle face normal, or vertex normal if available.
            SVector vTriNormal = GetTriangleNormal(index);
            if (HasVectorNormal(index))
            {
                vTriNormal = GetNormal(n0, n1, n2, uv.Item1, uv.Item2);
            }

            // If there's a normal map, apply it to the vertex normal.
            if (HasNormalMap())
            {
                SVector tNormal = Material.NormalMap.GetNormal(texel.u, texel.v);

                // Transform normal map vector to the triangle's normal.

                SVector up = new(0, 1, 0);
                SVector vUp = camera.NormalMatrix*up;

                SVector vNorm = vTriNormal;

                if (vUp == vNorm)
                {
                    SVector nearlyUp = new(1, 100, 0);
                    nearlyUp = nearlyUp.Normalise();
                    vUp = camera.NormalMatrix*nearlyUp;

                    vNorm = vTriNormal;
                }

                //SVector vNorm = oNormal;
                SVector vRight = vNorm.VectorProduct(vUp);
                vRight = vRight.Normalise();

                SVector xaxis = vRight;
                SVector yaxis = vUp;
                SVector zaxis = vNorm;

                Matrix m = new(xaxis, yaxis, -zaxis);

                m.Transpose();

                SVector vMapNormal = m * tNormal;

                hr.EntryNormal = vMapNormal;
            }
            else
            {
                hr.EntryNormal = vTriNormal;
            }

            return true;
        }

        private HitResult HitTestTriangle(Camera camera, ref HitResult hr, int index, in SPoint p1, in SPoint p2, in SVector ray, bool segmentOnly)
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
            FatTriangle tri = _fatTriangles[index];

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
                hr.t = mu;
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
            FatTriangle triangle = _fatTriangles[hr.Index];
            ModelSubGroup subGroup = _subGroupArray[triangle.subGroup];

            Material material = subGroup.Material;

            material ??= Material;

//            TextureBitmap textureBitmap = _modelData.texture as TextureBitmap;
            if (material.Texture is TextureBitmap textureBitmap)
            {
                //ModelData.Texel t0 = _modelData.GetTriangleTexel0(hr.Index);
                //ModelData.Texel t1 = _modelData.GetTriangleTexel1(hr.Index);
                //ModelData.Texel t2 = _modelData.GetTriangleTexel2(hr.Index);

                //ModelData.Texel texel = GetTexel(t0, t1, t2, hr.u, hr.v);

                //hr.u = texel.u;
                //hr.v = texel.v;

//                return textureBitmap.GetColour(texel.u, texel.v);
                return textureBitmap.GetColour(hr.u, hr.v);
            }
            else
            {
//                return _modelData.texture.GetColour(hr);
                return material.Texture.GetColour(hr);
            }

            //return Palette.Grey;
        }

        protected override void AsXMLImpl(XMLEntity xml)
        {
            base.AsXMLImpl(xml);

            if (xml is XMLModel xmlModel)
            {
                xmlModel.FileName = _modelData.fileName;

                if (String.IsNullOrEmpty(_modelData.fileName))
                {
                    // Assume model data created by hand, so write out vertex geometry.
                    xmlModel.Points = new List<XMLPoint>();
                    if (_modelData.points != null)
                    {
                        foreach (SPoint p in _modelData.points)
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
                        foreach (SVector v in _modelData.normals)
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
                    //    xmlModel.Texure = _modelData.texture.ToXML();
                    xmlModel.MaterialGuid = Material.Guid;// _modelData.texture.;
                }
            }
        }

        public override XMLEntity ToXML()
        {
            XMLModel xml = new XMLModel();
            AsXMLImpl(xml);
            return xml;
        }

        private int nodeNumber = 0;

        private string CreateGraphviz()
        {
            nodeNumber = 0;

            StringBuilder sb = new();
            sb.Append($"digraph Model {{\n");

            CreateGraphvizRecurse(sb, _root);

            //graphviz += "}";
            sb.Append("\n}");

            return sb.ToString(); ;
        }
        
        private string CreateGraphvizRecurse(StringBuilder sb, Node node)
        {
            int thisNode = nodeNumber;
            string nameName = $"n{thisNode} [ label=\"{node.Triangles.Count}\" ]";

            //            s += $" [{nodeNumber}]{node.Triangles.Count} -> {nodeNumber + 1}   [ Label = \"Left\" ];";
            //            s += $" {nodeNumber} -> {nodeNumber + 1}   [ Label = \"Left ({node.Triangles.Count})\" ];";
            //s += $" {nameName};\n";
            sb.Append($" {nameName};\n");

            if (node.Left != null)
            {
                nodeNumber++;
                int leftNode = nodeNumber;
//                s += $" {thisNode} -> {nodeNumber};\n";
//                s += CreateGraphvizRecurse(s, node.Left);
                CreateGraphvizRecurse(sb, node.Left);
                sb.Append($" n{thisNode} -> n{leftNode};\n");
            }

            if (node.Right != null)
            {
                nodeNumber++;
                int rightNode = nodeNumber;
//                s += $" {thisNode} -> {nodeNumber};\n";
//                s += CreateGraphvizRecurse(s, node.Right);
                CreateGraphvizRecurse(sb, node.Right);
                sb.Append($" n{thisNode} -> n{rightNode};\n");
            }

            return "";
        }

        public override TreeViewEntityModel ToTreeModel()
        {
            TreeViewEntityModel modelModel = new TreeViewEntityModel() { Name = Name, Instance = this };
            //foreach (ModelData.Group group in _modelData.groups)
            foreach (var subGroup in _subGroups.Values)
            {
//                modelModel.Children.Add(new TreeViewEntityModel() { Category = group.name, Instance = this } );
                modelModel.Children.Add(new TreeViewEntityModel() { Name = subGroup.Name, Instance = subGroup });
            }

            return modelModel;
        }
    }
}
