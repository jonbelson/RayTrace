#define USE_SIMD

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Media.Media3D;

using static RayTrace.ModelData;
using RayTrace.Importers;
using RayTrace.Preview;

namespace RayTrace
{
    public static class Ext
    {
        public static Vector4 VectorProduct(this Vector4 vv, Vector4 v)
        {
            return new(vv.Y*v.Z - vv.Z*v.Y, vv.Z*v.X - vv.X*v.Z, vv.X*v.Y - vv.Y*v.X, 0);
        }

    }

    //
    // Class containing the geometry and textures that comprise a 3D model.
    //
    public class ModelData
    {
        // Source file name if loaded from a file.
        public string fileName;

        // Directory containing model if loaded from a file.
        public string directoryName;

//        public TextureBitmap texture = null;
        /// <summary>
        /// /public Texture texture = null;
        /// </summary>

        public struct Texel
        {
            public float u, v, w;
        }

        public class Triangle
        {
            public int v0, v1, v2;  // Indices into Point array.
            public int t0, t1, t2;  // Indexes into Texel array.
            public int c0, c1, c2;  // Indices into Colour array.
            public int n0, n1, n2;  // Indices into normal array.
            public int n;           // Index into normal array for triangle normal.
        }

        public SPoint[] points;
        public Texel[] texels;
        public SVector[] normals;
        public Colour[] colours;
        public Triangle[] triangles;

        public class SubGroup
        {
            public bool Enabled { get; set; } = true;
            public string Name { get; set; }

            public int Material { get; set; } = -1;
            public List<int> Triangles { get; set; } = new();

            public List<SubGroup> Children { get; set; } = [];
        }

        //public SubGroup[] SubGroups { get; set; }
        //public List<SubGroup> Children { get; set; }
        public SubGroup Node { get; set; } = new();

        public float minX = 0.0f;
        public float minY = 0.0f;
        public float minZ = 0.0f;
        public float maxX = 0.0f;
        public float maxY = 0.0f;
        public float maxZ = 0.0f;

        public bool HasVertexNormals { get; set; } = false;
        public bool HasVertexColours { get; set; } = false;
        public bool HasVertexTexels { get; set; } = false;

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

        public Colour GetTriangleColour0(int index) => colours[triangles[index].c0];
        public Colour GetTriangleColour1(int index) => colours[triangles[index].c1];
        public Colour GetTriangleColour2(int index) => colours[triangles[index].c2];
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

        // Updated by CreateCache() 
        private FatTriangle[] _fatTriangles;
        private ModelSubGroup[] _subGroupArray;

        // All ModelSubGroups created, indexed by name.
        Dictionary<string, ModelSubGroup> _subGroups = new();

        private ModelSubGroup _modelSubGroup;

        // Recurse through SubGroup tree, creating matching ModelSubGroups.
        private void UpdateSubGroupRecurse(ModelData.SubGroup group, ModelSubGroup parentSubGroup)
        {
            string groupName = group.Name ?? "Untitled";

            // Make sure we're not adding a group with a duplicate name.
            if (_subGroups.ContainsKey(groupName))
            {
                int i = 1;
                do
                {
                    i++;
                    groupName = group.Name + i;
                }
                while (_subGroups.ContainsKey(groupName));
            }

            parentSubGroup.Name = groupName;
            try
            {
                _subGroups.Add(groupName, parentSubGroup);
            }
            catch (Exception e)
            {
                _ = 0;
            }

            foreach (ModelData.SubGroup child in group.Children)
            {
                ModelSubGroup childSubGroup = new(this, child) { Material = Scene.GetDefaultMaterial() };    // XXX Until subgroup Material GUIDs are saved.

                //int materialId = child.Material;
                //if (materialId != -1)
                //{
                //    childSubGroup.Material = Scene.Materials.;
                //}

                parentSubGroup.Children.Add(childSubGroup);

                UpdateSubGroupRecurse(child, childSubGroup);
            }
        }

        // Starting with ModelData.Node, create a matching ModelSubGroup tree.
        private void UpdateSubGroups()
        {
            if (_modelData != null && _modelData.Node != null)
            {
                _subGroups.Clear();

                ModelSubGroup parentGroup = new(this, _modelData.Node) { Material = Scene.GetDefaultMaterial() };    // XXX Until subgroup Material GUIDs are saved.
                _modelSubGroup = parentGroup;

                UpdateSubGroupRecurse(_modelData.Node, _modelSubGroup);
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

            return new ModelData.Texel() { u = texelU, v = texelV };
        }

        // c0, c1, c2   Colours for triangle.
        // u, v         Barycentric coordinates of hit point.
        private static Colour GetColour(Colour c0, Colour c1, Colour c2, float u, float v)
        {
            Colour c = c0*(1.0f - u - v) + c1*u + c2*v;

            return c;

/*
            float red = c0.Red*(1.0f - u - v) + c1.Red*u + c2.Red*v;
            float green = c0.Green*(1.0f - u - v) + c1.Green*u + c2.Green*v;
            float blue = c0.Blue*(1.0f - u - v) + c1.Blue*u + c2.Blue*v;

            return new Colour(red, green, blue);
*/
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
        public Model(Scene scene, Point p, ModelData modelData, bool centreModel=true) : base(scene, p)
        {
            _modelData = modelData??throw new ArgumentNullException("modelData");

            if (false && centreModel && modelData != null)
            {
                BoundingBox bb = modelData.GetBoundingBox();

                Pos.X = -bb.GetCentreX();
                Pos.Y = -bb.MinY;
                Pos.Z = -bb.GetCentreZ();
            }

            UpdateSubGroups();
        }

        public Model(Scene scene, Point p, ModelData modelData, List<Material> materials, bool centreModel = false) : this(scene, p, modelData, centreModel)
        {
            //_subGroups.Clear();

            //foreach (ModelData.SubGroup group in _modelData.SubGroups)
            //for (int i = 0; i<_modelData.SubGroups.Length; i++)
            foreach (var subGroup in _subGroups.Values)
            {
                int materialIndex = subGroup.Group.Material;

                if (materialIndex != -1)
                {
                    subGroup.Material = materials[subGroup.Group.Material];
                }
                else
                {
                    subGroup.Material = Scene.GetDefaultMaterial();    // XXX Until subgroup Material GUIDs are saved.
                }

                //ModelData.SubGroup group = _modelData.SubGroups[i];
/*
                //ModelSubGroup subGroup = new(this, group) { Material = this.Material };
                ModelSubGroup subGroup = new(this, group);
                if (group.Material != -1)
                {
                    subGroup.Material = materials[group.Material];
                }
                else
                {
                    subGroup.Material = Scene.GetDefaultMaterial();    // XXX Until subgroup Material GUIDs are saved.
                }
                _subGroups.Add(group.Name, subGroup);
*/
            }
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
                _modelData.directoryName = xmlModel.DirectoryName;

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
                // XXX Doesn't work for imported Models.
                /*
                ObjImporter importer = new();
                ModelData modelData = importer.ImportModel(Path.Join(xmlModel.DirectoryName, xmlModel.FileName));

                _modelData = modelData;
                //Material.Texture = objData.Item2;
                */
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

#if USE_SIMD

                Matrix m = ObjectToView.GetTranspose();
                m.MatrixMultiplySIMD(_modelData.points, _viewPoints);

#else
                for (int i = 0; i < _modelData.points.Length; i++)
                {
                    _viewPoints[i] = ObjectToView * _modelData.points[i];
                }
#endif

                Matrix ObjectToViewNormal = camera.NormalMatrix * ObjectToWorldNormal;

                _viewNormals = new SVector[_modelData.normals.Length];

#if USE_SIMD
                Matrix m2 = ObjectToViewNormal.GetTranspose();
                m2.MatrixMultiplySIMD(_modelData.normals, _viewNormals);
#else
                for (int i=0; i<_modelData.normals.Length; i++)
                {
                    _viewNormals[i] = ObjectToViewNormal * _modelData.normals[i];
                }
#endif

//                _subGroupArray = new ModelSubGroup[_modelData.Children.Count];
                _subGroupArray = new ModelSubGroup[_subGroups.Count];
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

                        if (_modelData.HasVertexTexels)
                        {
                            if (_modelData.texels != null)
                            {
                                fatTriangle.t0 = _modelData.GetTriangleTexel0(idx);
                                fatTriangle.t1 = _modelData.GetTriangleTexel1(idx);
                                fatTriangle.t2 = _modelData.GetTriangleTexel2(idx);
                            }
                        }

                        _fatTriangles[idx++] = fatTriangle;
                    }
                }

                Debug.Assert(idx == _modelData.triangles.Length);
            }
        }

        // Recurse through partition tree, checking for intersections in boxes that that ray passees through.
        private void HitTestRecurseFast(Camera camera, List<HitResult> hitResults, Node node, ref SPoint p1, ref SPoint p2, bool segmentOnly, bool firstHitOnly)
        {
            if (firstHitOnly && hitResults.Count > 0)
            {
                return;
            }

            // Does ray passes through us?
            if (node.Box.Intersects(in p1, in p2, out float distance))
            {
                HitResult hr = new HitResult(this);

                //SVector ray = p2 - p1;

//                Vector4 pp1 = p1.ToVector4();
//                Vector4 pp2 = p2.ToVector4();
                Vector4 pp1 = p1.ToPoint4();
                Vector4 pp2 = p2.ToPoint4();
                Vector4 vRay = pp2 - pp1;

                if (node.Left == null && node.Right == null)
                {
                    // We're a leaf node, so test our triangles.
                    foreach (int i in node.Triangles)
                    {
                        hr.Reset();
                        HitTestTriangleFast(camera, ref hr, i, pp1, pp2, vRay, segmentOnly);
                        if (hr.HasEntry())
                        {
                            hitResults.Add(hr);

                            if (firstHitOnly) break;
                        }

                    }
                    return;
                }

                if (node.Left != null)
                {
                    HitTestRecurseFast(camera, hitResults, node.Left, ref p1, ref p2, segmentOnly, firstHitOnly);
                }
                if (node.Right != null)
                {
                    HitTestRecurseFast(camera, hitResults, node.Right, ref p1, ref p2, segmentOnly, firstHitOnly);
                }
            }
            else
            {
                _ = 0;
            }
        }

        // Recurse through partition tree, checking for intersections in boxes that that ray passees through.
        private void HitTestRecurse(Camera camera, List<HitResult> hitResults, Node node, in SPoint p1, in SPoint p2, bool segmentOnly, bool firstHitOnly)
        {
            if (firstHitOnly && hitResults.Count > 0)
            {
                return;
            }

            // Does ray passes through us?
            //if (node.Box.Intersects(p1, p2))
            //if (node.GeometryBox.Intersects(p1, p2))
            if (node.GeometryBox.Intersects(in p1, in p2, out float distance))
            {
                foreach (HitResult result in hitResults)
                {
                    if (result.HasEntry())
                    {
                        float d = result.t;

                        // If BoundingBox is further away than the current closest hit, don't bother testing it.
                        if (distance > d) return;
                    }
                }


                HitResult hr = new HitResult(this);

                SVector ray = p2 - p1;

                if (node.Left == null && node.Right == null)
                {
                    // We're a leaf node, so test our triangles.
                    foreach (int i in node.Triangles)
                    {
                        HitTestTriangle(camera, ref hr, i, p1, p2, ray, segmentOnly);
                        if (hr.HasEntry())
                        {
                            hitResults.Add(hr);

                            if (firstHitOnly) break;
                        }

                    }
                    return;
                }

                if (node.Left != null)
                {
                    HitTestRecurse(camera, hitResults, node.Left, in p1, in p2, segmentOnly, firstHitOnly);
                }
                if (node.Right != null)
                {
                    HitTestRecurse(camera, hitResults, node.Right, in p1, in p2, segmentOnly, firstHitOnly);
                }
            }
            else
            {
                _ = 0;
            }
        }

        private HitResult HitTestIterative(Camera camera, in SPoint p1, in SPoint p2, bool segmentOnly, bool firstHitOnly)
        {
            if (_root is null) return new HitResult(this);

            Stack<Node> stack = new();
            stack.Push(_root);

            HitResult bestHit = new(this);
            float closestT = Single.MaxValue;

            SVector ray = p2 - p1;

            HitResult tmp = new(this);

            while (stack.Count > 0)
            {
                Node node = stack.Pop();

                // Skip this Node if we don't intersect it.
                if (!node.GeometryBox.Intersects(in p1, in p2, out float nodeDist))
                {
                    continue;
                }

                // SKip this node if it's further away than our closest hit.
                if (nodeDist >= closestT) continue;

                // Is this a leaf Node? If so, check its triangles.
                if (node.Left == null && node.Right == null)
                {
                    // We're a leaf node, so test our triangles.
                    foreach (int i in node.Triangles)
                    {
                        HitTestTriangle(camera, ref tmp, i, p1, p2, ray, segmentOnly);
                        if (tmp.HasEntry())
                        {
                            float t = tmp.t;
                            if (t < closestT)
                            {
                                bestHit = tmp;
                                closestT = t;
                                if (firstHitOnly) return bestHit;
                            }
                        }

                    }
                }
                else
                {
                    // Push the further Node first so the nearer will be processed first.
                    Node left = node.Left;
                    Node right = node.Right;

                    if (left is not null && right is not null)
                    {
                        bool leftHit = left.GeometryBox.Intersects(in p1, in p2, out float leftDist);
                        bool rightHit = right.GeometryBox.Intersects(in p1, in p2, out float rightDist);

                        if (leftHit && rightHit)
                        {
                            if (leftDist < rightDist)
                            {
                                stack.Push(right);
                                stack.Push(left);
                            }
                            else
                            {
                                stack.Push(left);
                                stack.Push(right);
                            }
                        }
                        else if (leftHit)
                        {
                            stack.Push(left);
                        }
                        else if (rightHit)
                        {
                            stack.Push(right);
                        }
                    }

                    else
                    {
                        if (left is not null) stack.Push(left);
                        if (right is not null) stack.Push(right);
                    }

                }
            }

            if (bestHit.HasEntry())
            {
               CalculateTexelAndNormal(camera, ref bestHit);
            }

            return bestHit;
        }


        private HitResult HitTestImpl(Camera camera, in SPoint p1, in SPoint p2, bool segmentOnly, bool firstHitOnly)
        {
            HitResult hitResult2 = HitTestIterative(camera, in p1, in p2, segmentOnly, firstHitOnly);
            return hitResult2;

#if false
            HitResult hitResult = new(null);

            Node node = _root;

            if (node != null)
            {
                List<HitResult> hitResults = new List<HitResult>();

                HitTestRecurse(camera, hitResults, node, in p1, in p2, segmentOnly, firstHitOnly);
//                HitTestRecurseFast(camera, hitResults, node, ref p1, ref p2, segmentOnly, firstHitOnly);

                if (hitResults.Count > 0)
                {
                    float closest2 = Single.MaxValue;

                    foreach (HitResult hr in hitResults)
                    {
                        if (hr.HasEntry())
                        {
                            float d = hr.t;
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
#endif
        }

        private bool CalculateTexelAndNormal(Camera camera, ref HitResult hr)
        {
            int index = hr.Index;

            SPoint intersection = hr.Entry;
            FatTriangle tri = _fatTriangles[index];
            ModelSubGroup subGroup = _subGroupArray[tri.subGroup];

            SPoint V0 = tri.v0;
            SPoint V1 = tri.v1;
            SPoint V2 = tri.v2;

            (float, float) uv = GetBarymetricCoords(V0, V1, V2, intersection);
            Debug.Assert(uv.Item1 <= 1.0);
            Debug.Assert(uv.Item2 <= 1.0);

            hr.bu = uv.Item1;
            hr.bv = uv.Item2;
            hr.bw = 1.0f - uv.Item1 - uv.Item2;

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

            // Calculate texel at hit point if texels are available.
            if (_modelData.HasVertexTexels)
            {
                // Use barycentric coordinates to get texel coordinates.
                ModelData.Texel t0 = _modelData.GetTriangleTexel0(index);
                ModelData.Texel t1 = _modelData.GetTriangleTexel1(index);
                ModelData.Texel t2 = _modelData.GetTriangleTexel2(index);

                ModelData.Texel texel = GetTexel(t0, t1, t2, uv.Item1, uv.Item2);
                hr.u = texel.u;
                hr.v = texel.v;

                Material material = subGroup.Material ?? Material;

                // If there's a normal map, apply it to the vertex normal.
                if (material.HasNormalMap())
                {
                    SVector tNormal = material.NormalMap.GetNormal(texel.u, texel.v);

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
            }
            else
            {
                hr.EntryNormal = vTriNormal;
            }

            return true;
        }

        private HitResult HitTestTriangleFast(Camera camera, ref HitResult hr, int index, in Vector4 vvP1, in Vector4 vvP2, in Vector4 vvRay, bool segmentOnly)
        {
            FatTriangle tri = _fatTriangles[index];

            //SPoint vPos = tri.v0;
            Vector4 vvPos = tri.v0.ToVector4();

            //SVector vNormal = tri.n;//.Negate();
            Vector4 vvNormal = tri.n.ToVector4();

            //Vector4 vvRay = ray.ToVector4();

            // Check if face is pointing away from us.
            float dp = Vector4.Dot(vvNormal, vvRay);
            if (dp > 0.0f) return hr;    // Check >0 since vectors point in opposite directions.

            // Hit test against the triangle's plane.

            vvNormal.W = 0;

            Vector4 vvNP = vvNormal*vvPos;
            //vvNP = Vector4.Negate(vvNP);

            float D = -Vector4.Dot(vvNP, Vector4.One);

            //Vector4 vvP1 = p1.ToPoint4();      // W must be 1
            //Vector4 vvP2 = p2.ToPoint4();

            vvNormal.W = D;                     // vvNormal = A, B, C, D

            float num = Vector4.Dot(vvNormal*vvP1, Vector4.One);

            Vector4 vvP1P2 = vvP1 - vvP2;
            Vector4 vvNP1P2 = vvNormal*vvP1P2;

            float denom = Vector4.Dot(vvNP1P2, Vector4.One);


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



            Vector4 pp1 = vvP1;// p1.ToVector4();// new Vector4(p1.X, p1.Y, p1.Z, 1);
            Vector4 pp2 = vvP2;// p2.ToVector4();// new Vector4(p2.X, p2.Y, p2.Z, 1);
            Vector4 vecP2P1 = pp2 - pp1;
            Vector4 Intersection = pp1 + vecP2P1*mu;

            Vector4 v0 = tri.v0.ToVector4();
            Vector4 v1 = tri.v1.ToVector4();
            Vector4 v2 = tri.v2.ToVector4();

            Vector4 edge0 = v1 - v0;
            Vector4 edge1 = v2 - v1;
            Vector4 edge2 = v0 - v2;

            Vector4 C0 = Intersection - v0;
            Vector4 C1 = Intersection - v1;
            Vector4 C2 = Intersection - v2;

            Vector4 normal = tri.n.ToVector4();

            Vector4 vp0 = edge0.VectorProduct(C0);
            Vector4 vp1 = edge1.VectorProduct(C1);
            Vector4 vp2 = edge2.VectorProduct(C2);

            //float dp1 = Vector4.Dot(normal, vp0);
            //float dp2 = Vector4.Dot(normal, vp1);
            //float dp3 = Vector4.Dot(normal, vp2);

            if (Vector4.Dot(normal, edge0.VectorProduct(C0)) <= 0.0f &&
                Vector4.Dot(normal, edge1.VectorProduct(C1)) <= 0.0f &&
                Vector4.Dot(normal, edge2.VectorProduct(C2)) <= 0.0f)
            {
                hr.Entry = new SPoint(Intersection.X, Intersection.Y, Intersection.Z);
                hr.EntryNormal = tri.n; // vNormal;
                hr.t = mu;
                hr.Index = index;
            }


            return hr;
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

            hr.Reset();

            FatTriangle tri = _fatTriangles[index];

            SPoint vPos = tri.v0;

            SVector vNormal = tri.n;//.Negate();

            // Check if face is pointing away from us.
            float dp = vNormal.DotProduct(ray);
            if (dp > 0.0f) return hr;    // Check >0 since vectors point in opposite directions.


            float A = vNormal.I;
            float B = vNormal.J;
            float C = vNormal.K;

            // Since Ax + By + Cz + D = 0...
            float D = -(A*vPos.X + B*vPos.Y + C*vPos.Z);
//            float D = -(A * tri.v0.X + B * tri.v0.Y + C * tri.v0.Z);

            float num = A*p1.X + B*p1.Y + C*p1.Z + D;
            float denom = A*(p1.X - p2.X) + B*(p1.Y - p2.Y) + C*(p1.Z - p2.Z);


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

            SVector vp0 = edge0.VectorProduct(C0);
            SVector vp1 = edge1.VectorProduct(C1);
            SVector vp2 = edge2.VectorProduct(C2);

            //float dp1 = vNormal.DotProduct(vp0);
            //float dp2 = vNormal.DotProduct(vp1);
            //float dp3 = vNormal.DotProduct(vp2);

            if (vNormal.DotProduct(edge0.VectorProduct(C0)) <= 0.0f &&
                vNormal.DotProduct(edge1.VectorProduct(C1)) <= 0.0f &&
                vNormal.DotProduct(edge2.VectorProduct(C2)) <= 0.0f)
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

        // Check that at least one triangle is hit by the ray.
        public override bool HitTestQuick(Camera camera, Ray ray)
        {
            bool segmentOnly = (ray.TMax == 1.0);

            SPoint _p2 = ray.Origin.Offset(ray.Direction);
            SPoint p2 = _p2;

            HitResult hr = HitTestImpl(camera, ray.Origin, p2, segmentOnly, true);
            return hr.HasEntry();

#if false
            //bool segmentOnly = (ray.TMax == 1.0);

            SPoint p1 = ray.Origin;
            //Point p2 = ray.Origin.Offset(ray.Direction);

            Matrix ObjectToView = camera.WorldToView * ObjectToWorld;

            //BoundingBox bb = _modelData.GetBoundingBox();
            //BoundingBox bb2 = bb.Transform(ObjectToView);

            //if (!bb2.Intersects(p1, p2))
            if (!_viewBoundingBox.Intersects(in p1, in p2, out float distance))
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
                    if (mu < THRESH || mu > 1.0f - THRESH)
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

                if (vNormal.DotProduct(edge0.VectorProduct(C0)) < 0.0f &&
                    vNormal.DotProduct(edge1.VectorProduct(C1)) < 0.0f &&
                    vNormal.DotProduct(edge2.VectorProduct(C2)) < 0.0f)
                {
                    return true;
                }
            }

            return false;
#endif
        }

        public override Colour GetColour(HitResult hr)
        {
            FatTriangle triangle = _fatTriangles[hr.Index];
            ModelSubGroup subGroup = _subGroupArray[triangle.subGroup];

            Material material = subGroup.Material;

            material ??= Material;

            if (_modelData.HasVertexColours)
            {
                int index = hr.Index;

                Colour c0 = _modelData.GetTriangleColour0(index);
                Colour c1 = _modelData.GetTriangleColour1(index);
                Colour c2 = _modelData.GetTriangleColour2(index);

                Colour colour = GetColour(c0, c1, c2, hr.bu, hr.bv);

                return colour;
            }

            //TextureBitmap textureBitmap = _modelData.texture as TextureBitmap;
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

                if (material.Texture != null)
                {
                    return material.Texture.GetColour(hr);
                }
            }

            return Palette.Grey;
        }

        // Retrieve a list of Material instances used by this Model.
        public List<Material> GetMaterials()
        {
            List<Material> materials = [];

            foreach (var subGroup in _subGroups.Values)
            {
                if (subGroup.Material != null)
                {
                    materials.Add(subGroup.Material);
                }
            }
            return materials;
        }

        protected override void AsXMLImpl(XMLEntity xml)
        {
            base.AsXMLImpl(xml);

            if (xml is XMLModel xmlModel)
            {
                xmlModel.FileName = _modelData.fileName;
                xmlModel.DirectoryName = _modelData.directoryName;

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

                /*
                if (_modelData.texture != null)
                {
                    //    xmlModel.Texure = _modelData.texture.ToXML();
                    xmlModel.MaterialGuid = Material.Guid;// _modelData.texture.;
                }
                */
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

        private void ToTreeModelRecurse(ModelSubGroup subGroup, TreeViewEntityModel entityModel)
        {
            var child = new TreeViewEntityModel() { Name = subGroup.Name, Instance = subGroup };

            entityModel.Children.Add(child);

            foreach (var childGroup in subGroup.Children)
            {
                ToTreeModelRecurse(childGroup, child);
            }
        }

        public override TreeViewEntityModel ToTreeModel()
        {
            TreeViewEntityModel modelModel = new TreeViewEntityModel() { Name = Name, Instance = this };

            foreach (var childGroup in _modelSubGroup.Children)
            {
                ToTreeModelRecurse(childGroup, modelModel);
            }

            ///ToTreeModelRecurse(_modelSubGroup, modelModel);

/*
            //foreach (ModelData.Group group in _modelData.groups)
            foreach (var subGroup in _subGroups.Values)
            {
//                modelModel.Children.Add(new TreeViewEntityModel() { Category = group.name, Instance = this } );
                modelModel.Children.Add(new TreeViewEntityModel() { Name = subGroup.Name, Instance = subGroup });
            }
*/
            return modelModel;
        }

        public override (Point, Point) GetBoundingBox()
        {
            //Point min = new(_modelData.minX*Scale, _modelData.minY*Scale, _modelData.minZ*Scale);
            //Point max = new(_modelData.maxX*Scale, _modelData.maxY*Scale, _modelData.maxZ*Scale);

            Point min = new(_modelData.minX, _modelData.minY, _modelData.minZ);
            Point max = new(_modelData.maxX, _modelData.maxY, _modelData.maxZ);


            //min.X += Pos.X;
            //min.Y += Pos.Y;
            //min.Z += Pos.Z;

            //max.X += Pos.X;
            //max.Y += Pos.Y;
            //max.Z += Pos.Z;

            return (min, max);
        }

        public virtual BoundingBox3D GetBoundingBox2()
        {
            BoundingBox3D bb = new();

            var axisRotationX = new AxisAngleRotation3D(new Vector3D(1, 0, 0), RotateX);
            var axisRotationY = new AxisAngleRotation3D(new Vector3D(0, 1, 0), RotateY);
            var axisRotationZ = new AxisAngleRotation3D(new Vector3D(0, 0, 1), RotateZ);
            var scale = new ScaleTransform3D(Scale, Scale, Scale);
            var translate = new TranslateTransform3D(Pos.X, Pos.Y, Pos.Z);

            Transform3DGroup transformGroup = new();

            transformGroup.Children.Add(scale);
            transformGroup.Children.Add(new RotateTransform3D(axisRotationX));
            transformGroup.Children.Add(new RotateTransform3D(axisRotationY));
            transformGroup.Children.Add(new RotateTransform3D(axisRotationZ));
            transformGroup.Children.Add(translate);

            bb.Transform = transformGroup;

            return bb;
        }


    }
}
