using Microsoft.Windows.Themes;
using RayTrace.Brdfs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Xml.Linq;
using static RayTrace.Importers.ObjImporter;
using static RayTrace.ModelData;

namespace RayTrace.Importers
{
    //
    // Importer for glTF format.
    // https://github.com/KhronosGroup/glTF/blob/main/specification/2.0/Specification.adoc
    //
    internal class glTFImporter : IImporter
    {

        class GltfPrimitive
        {
            // Attributes.
            //public Vec3T<float> positions = new Vec3T<float>();
            public Vec3T<float>[] positions;
            //public Vec3T<float> normals = new Vec3T<float>();
            public Vec3T<float>[] normals;
            //public Vec4T<float> tangents = new Vec4T<float>();
            public Vec4T<float>[] tangents;
            public Vec2T<float>[] texCoords;
            //public Vec3T<float> colours = new Vec3T<float>();
            public Vec3T<float>[] colours;
            public Vec4T<float> joints = new Vec4T<float>();
            public Vec4T<float> weights = new Vec4T<float>();

            //public List<int> Indices = new List<int>();
            public UInt32[] indices;
            public int material = 0;
            public MeshPrimitiveMode mode = MeshPrimitiveMode.TRIANGLES;
        }

        class GltfMaterial
        {
            public string name;

            // NormalTexureInfo
            public string normalTexture;    // +X right, +Y up, +Z towards viewer.
            public float normalScale = 1.0f;

            // textureInfo
            public string emissiveTexture;  // sRGB

            public Vec3T<float> emissiveFactor = new( 0.0f, 0.0f, 0.0f );
            public float emissiveStrength = 1.0f;

            // pbrMetallicRoughness
            public Vec4T<float> baseColorFactor = new( 1.0f, 1.0f, 1.0f, 1.0f );
            public string baseColorTexture;     // sRGB
            public float metallicFactor = 1.0f;
            public float roughnessFactor = 1.0f;
            public string metallicRoughnessTexture; // sRGB

            public bool doubleSided = false;
        }

        class GltfSampler
        {
            public MagFilter magFilter { get; set; }
            public MinFilter minFilter { get; set; }
            public SamplerWrap wrapS { get; set; } = SamplerWrap.REPEAT;
            public SamplerWrap wrapT { get; set; } = SamplerWrap.REPEAT;
            public string name { get; set; }

        }

        // Provides mapping from textures to images.
        class GltfTexture
        {
            //public int sampler { get; set; }
            public int source { get; set; }
        }

        class GltfNode
        {
            public string name;
            public List<GltfNode> children = new();
            public GltfMesh mesh = null;
            //public float[] matrix = new float[16];
            public Matrix4x4 matrix = Matrix4x4.Identity;
        }

        class GltfMesh
        {
            public string name;
            public List<GltfPrimitive> primitives = new();
        }

        class GltfScene
        {
            public string name;
            public List<GltfNode> nodes = new();
        }

        class GltfAsset
        {
            // Name of loaded glTF file.
            public string fileName;

            // Directory from which glTF was loaded.
            public string directory;

            public List<GltfNode> nodes = new();
            public List<GltfMesh> meshes = new();
            public List<GltfMaterial> materials = new();
            public List<GltfTexture> textures { get; set; } = new();
            public List<GltfSampler> samplers = new();
            public List<GltfScene> scenes = new();
        }

        private GltfAsset _gltfAsset = null;

        enum AccessorDataType
        {
            SignedByte = 5120,
            UnsignedByte = 5121,
            SignedShort = 5122,
            UnsignedShort = 5123,
            UnsignedInt = 5125,
            Float = 5126
        }

        // The topology type of primitives to render.
        // https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#_mesh_primitive_mode
        enum MeshPrimitiveMode
        {
            POINTS = 0,
            LINES = 1,
            LINE_LOOP = 2,
            LINE_STRIP = 3,
            TRIANGLES = 4,
            TRIANGLE_STRIP = 5,
            TRIANGLE_FAN = 6
        }

        enum MagFilter
        {
            NEAREST,
            LINEAR
        }

        enum MinFilter
        {
            NEAREST,
            LINEAR,
            NEAREST_MIPMAP_NEAREST,
            LINEAR_MIPMAP_NEAREST,
            NEAREST_MIPMAP_LINEAR,
            LINEAR_MIPMAP_LINEAR
        }

        enum SamplerWrap
        {
            CLAMP_TO_EDGE = 33071,
            MIRRORED_REPEAT = 33648,
            REPEAT = 10497 
        }

        /*
            "SCALAR" 1
            "VEC2" 2
            "VEC3" 3
            "VEC4" 4
            "MAT2" 4
            "MAT3" 9
            "MAT4" 16
         */

        /*
         * Vertex attributes
         * 
            POSITION VEC3 float Unitless XYZ vertex positions
            NORMAL VEC3 float Normalized XYZ vertex normals
            TANGENT VEC4 float XYZW vertex tangents where the XYZ portion is normalized, and the W component is a sign value (-1 or +1) indicating handedness of the tangent basis
            TEXCOORD_n VEC2 float nsigned byte normalized unsigned short normalized ST texture coordinates
            COLOR_n VEC3 EC4 float unsigned byte normalized unsigned short normalized RGB or RGBA vertex color linear multiplier
            JOINTS_n VEC4 unsigned byte unsigned short See Skinned Mesh Attributes
            WEIGHTS_n VEC4 float nsigned byte normalized nsigned short normalized See Skinned JsonMesh Attributes
         */

        // JsonBufferView.Target
        private const Int32 ARRAY_BUFFER = 34962;
        private const Int32 ELEMENT_ARRAY_BUFFER = 34963;

        struct Vec2T<T>
        {
            public T x; public T y;
            public Vec2T(T x, T y, T z) { this.x = x; this.y = y; }
        }

        struct Vec3T<T>
        {
            public T x; public T y; public T z; 
            public Vec3T(T x, T y, T z) { this.x = x; this.y = y; this.z = z; }
        }

        struct Vec4T<T>
        {
            public T x; public T y; public T z; public T w;
            public Vec4T(T x, T y, T z, T w) { this.x = x; this.y = y; this.z = z; this.w = w;  }
        }

        private Dictionary<int, byte[]> _buffers = new();

        class JsonExtras
        {
            public string author { get; set; }
            public string license { get; set; }
            public string source { get; set; }
            public string title { get; set; }
        }

        class JsonAsset
        {
            public JsonExtras extras { get; set; }
            public string generator { get; set; }
            public string version { get; set; }
        }

        private class JsonBuffer
        {
            public Int32 byteLength { get; set; }
            public string uri { get; set; }
        }

        public class JsonBufferView
        {
            public int buffer { get; set; }
            public Int32 byteLength { get; set; }
            public Int32 byteOffset { get; set; }
            public Int32 byteStride { get; set; }
            public string name { get; set; }
            public Int32 target { get; set; }
        }

        public class JsonImages
        {
            public string uri { get; set; }
        }

        class JsonNode
        {
            public int mesh { get; set; } = -1;
            public string name { get; set; }
            public List<int> children { get; set; }

            public float[] translation { get; set; }
            public float[] scale { get; set; }
            public float[] rotation { get; set; }   // XYZW

            public float[] matrix { get; set; }
        }

        class JsonAccessor
        {
            public Int32 bufferView { get; set; }
            public Int32 byteOffset { get; set; }
            public Int32 componentType { get; set; }
            public Int32 count { get; set; }

            public List<float> min { get; set; }
            public List<float> max { get; set; }

            public string type { get; set; }
        }

        class JsonAttribute
        {
            // XXX Use a map instead?
            // XXX Handle '_n' ?
            //public Dictionary<string, UInt32> ?? { get; set; }

            public Int32? POSITION { get; set; }
            public Int32? NORMAL { get; set; }
            public Int32? TANGENT { get; set; }
            public Int32? TEXCOORD_0 { get; set; }
            public Int32? COLOR_0 { get; set; }
            public Int32? JOINTS_0 { get; set; }
            public Int32? WEIGHTS_0 { get; set; }
        }

        class JsonPrimitive
        {
            public JsonAttribute attributes { get; set; }

            public Int32? indices { get; set; }     // The index of the accessor that contains the vertex indices
            public Int32? material { get; set; }
            public Int32? mode { get; set; }
        }

        class JsonMesh
        {
            public string name { get; set; }

            public List<JsonPrimitive> primitives { get; set; }
        }

        // https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#reference-sampler
        class JsonSampler
        {
            public MagFilter magFilter { get; set; }
            public MinFilter minFilter { get; set; }
            public SamplerWrap wrapS { get; set; } = SamplerWrap.REPEAT;
            public SamplerWrap wrapT { get; set; } = SamplerWrap.REPEAT;
            public string name { get; set; }
        }

        class JsonMaterialExtension { };

        class JsonMaterialKHR_materials_pbrSpecularGlossiness
        {
            public float[] diffuseFactor { get; set; }

            public class DiffuseTexture { public int index { get; set; } }
            public DiffuseTexture diffuseTexture { get; set; }

            public float glossinessFactor { get; set; } = 1.0f;

            public float[] specularFactor { get; set; }

            public class SpecularGlossinessTexture { public int index { get; set; } }
            public SpecularGlossinessTexture specularGlossinessTexture { get; set; }
        }

        // https://github.com/KhronosGroup/glTF/blob/main/extensions/2.0/Khronos/KHR_materials_emissive_strength/README.md
        class JsonMaterialKHR_materials_emissive_strength
        {
            public float emissiveStrength { get; set; } = 1.0f;
        }

        class JsonMaterial
        {
            public string name { get; set; }

            public class Extensions
            {
                //public List<JsonMaterialExtension> extensions { get; set; }
                public JsonMaterialKHR_materials_pbrSpecularGlossiness KHR_materials_pbrSpecularGlossiness { get; set; }
                public JsonMaterialKHR_materials_emissive_strength KHR_materials_emissive_strength { get; set; }
            }

            public Extensions extensions { get; set; }

            public class PbrMetallicRoughness
            {
                public class BaseColorTexture
                {
                    public int index { get; set; }
                }

                public class MetallicRoughnessTexture
                {
                    public int index { get; set; }
                }

                public float[] baseColorFactor { get; set; } = new float[] { 1f, 1f, 1f, 1f };

                public BaseColorTexture baseColorTexture { get; set; }
                public MetallicRoughnessTexture metallicRoughnessTexture { get; set; }

                public float metallicFactor { get; set; } = 1;
                public float roughnessFactor { get; set; } = 1;
            }

            public PbrMetallicRoughness pbrMetallicRoughness { get; set; }

            public class NormalTexture
            {
                public int index { get; set; }
                public float scale { get; set; } = 1.0f;
            }

            public NormalTexture normalTexture { get; set; }

            public class EmissiveTexture { public int index { get; set; } }
            public EmissiveTexture emissiveTexture { get; set; }

            public float[] emissiveFactor { get; set; }

        }

        class JsonScene
        {
            public string name { get; set; }
            public List<Int32> nodes { get; set; }
        }

        class JsonTexture
        {
            public int source { get; set; }
        }

        private class Asset
        {
            public JsonAsset asset { get; set; }

            public List<JsonBuffer> buffers { get; set; }
            public List<JsonBufferView> bufferViews { get; set; }
            public List<JsonImages> images { get; set; }

            public List<JsonAccessor> accessors { get; set; }

            public List<JsonMesh> meshes { get; set; }
            public List<JsonMaterial> materials { get; set; }

            public List<JsonNode> nodes { get; set; }

            public List<JsonSampler> samplers { get; set; }

            public List<JsonScene> scenes { get; set; }
            public Int32? scene { get; set; }

            public List<JsonTexture> textures { get; set; }
        };

        private void Dump(Vec3T<float>[] array, string name)
        {
            Logger.Instance.Trace($"Dumping array: {name}");

            for (int i=0; i<array.Count(); i++)
            {
                Logger.Instance.Trace($"  {i} {array[i].x}, {array[i].y}, {array[i].z}");
            }
        }

        private void Dump(Vec4T<float>[] array, string name)
        {
            Logger.Instance.Trace($"Dumping array: {name}");

            for (int i = 0; i<array.Count(); i++)
            {
                Logger.Instance.Trace($"  {i} {array[i].x}, {array[i].y}, {array[i].z}, {array[i].w}");
            }
        }

        private void Dump(uint[] array, string name)
        {
            Logger.Instance.Trace($"Dumping array: {name}");

            for (int i = 0; i<array.Count(); i++)
            {
                Logger.Instance.Trace($"  {i} {array[i]}");
            }
        }

        private static JsonAccessor GetAccessor(Asset asset, int index)
        {
            if (asset is not null && asset.accessors is not null)
            {
                if (asset.accessors.Count > index)
                {
                    return asset.accessors[index];
                }
            }

            return null;
        }

        private static JsonBufferView GetBufferView(Asset asset, int index)
        {
            if (asset is not null && asset.accessors is not null)
            {
                if (asset.bufferViews.Count > index)
                {
                    return asset.bufferViews[index];
                }
            }

            return null;
        }

        private JsonBuffer GetBuffer(Asset asset, int index)
        {
            if (asset is not null && asset.buffers is not null)
            {
                if (asset.buffers.Count > index)
                {
                    return asset.buffers[index];
                }
            }

            return null;
        }

        private static string GetImage(Asset asset, int index)
        {
            if (asset is not null && asset.images is not null)
            {
                if (asset.images.Count > index)
                {
                    string s = asset.images[index].uri;
                    return System.Uri.UnescapeDataString(s);
                }
            }

            return null;
        }

        // Return a ReadOnlySpan<> of Vec2T<T> to a memory buffer.
        private ReadOnlySpan<Vec2T<T>> ParseVEC2<T>(int bufferIndex, Int32 byteOffset, Int32 count)
        {
            ReadOnlySpan<Vec2T<T>> vecs = new();

            if (bufferIndex >= 0 || bufferIndex < _buffers.Count)
            {
                byte[] bytes = _buffers[bufferIndex];

                ReadOnlySpan<byte> span = bytes.AsSpan(byteOffset);

                ReadOnlySpan<Vec2T<T>> asVecT = MemoryMarshal.Cast<byte, Vec2T<T>>(span);

                vecs = asVecT.Slice(0, count);
            }

            return vecs;
        }

        // Return a ReadOnlySpan<> of Vec3T<T> to a memory buffer.
        private ReadOnlySpan<Vec3T<T>> ParseVEC3<T>(int bufferIndex, Int32 byteOffset, Int32 count)
        {
            ReadOnlySpan<Vec3T<T>> vecs = new();

            if (bufferIndex >= 0 || bufferIndex < _buffers.Count)
            {
                byte[] bytes = _buffers[bufferIndex];

                ReadOnlySpan<byte> span = bytes.AsSpan(byteOffset);

                ReadOnlySpan<Vec3T<T>> asVecT = MemoryMarshal.Cast<byte, Vec3T<T>>(span);

                vecs = asVecT.Slice(0, count);
            }

            return vecs;
        }

        // Return a ReadOnlySpan<> of Vec4T<T> to a memory buffer.
        private ReadOnlySpan<Vec4T<T>> ParseVEC4<T>(int bufferIndex, Int32 byteOffset, Int32 count)
        {
            ReadOnlySpan<Vec4T<T>> vecs = new();

            if (bufferIndex >= 0 || bufferIndex < _buffers.Count)
            {
                byte[] bytes = _buffers[bufferIndex];

                ReadOnlySpan<byte> span = bytes.AsSpan(byteOffset);

                ReadOnlySpan<Vec4T<T>> asVecT = MemoryMarshal.Cast<byte, Vec4T<T>>(span);

                vecs = asVecT.Slice(0, count);
            }

            return vecs;
        }

        /*
                private List<Vec3> ParseVEC3(JsonBuffer buffer, Int32 byteOffset, Int32 count, Int32 componentType)
                {
                    Debug.Assert(componentType == (int)AccessorDataType.Float);

                    List<Vec3> array = new();

                    using (FileStream fs = File.OpenRead(buffer.uri))
                    {
                        fs.Position = byteOffset;

                        using (BinaryReader reader = new BinaryReader(fs))
                        {
                            for (UInt32 i = 0; i<count; i++)
                            {
                                float f1 = (float)reader.ReadSingle();
                                float f2 = (float)reader.ReadSingle();
                                float f3 = (float)reader.ReadSingle();

                                array.Add(new(f1, f2, f3));
                            }
                        }
                    }

                    return array;
                }
        */
        private ReadOnlySpan<T> ParseSCALAR<T>(int bufferIndex, Int32 byteOffset, Int32 count) where T : struct
        {
            ReadOnlySpan<T> vecs = new();

            if (bufferIndex >= 0 || bufferIndex < _buffers.Count)
            {
                byte[] bytes = _buffers[bufferIndex];

                ReadOnlySpan<byte> span = bytes.AsSpan(byteOffset);

                ReadOnlySpan<T> asT = MemoryMarshal.Cast<byte, T>(span);

                vecs = asT.Slice(0, count);
            }

            return vecs;
        }
        /*
                private List<Int32> ParseSCALAR(JsonBuffer buffer, Int32 byteOffset, Int32 count, Int32 componentType)
                {
                    Debug.Assert(componentType == (int)AccessorDataType.UnsignedInt);

                    List<Int32> array = new();

                    using (FileStream fs = File.OpenRead(buffer.uri))
                    {
                        fs.Position = byteOffset;

                        using (BinaryReader reader = new BinaryReader(fs))
                        {
                            for (UInt32 i = 0; i<count; i++)
                            {
                                int val = reader.ReadInt32();

                                array.Add(val);
                            }
                        }
                    }

                    return array;
                }
        */

        private ReadOnlySpan<Vec2T<T>> ParseAccessorAsVEC2<T>(Asset asset, JsonAccessor accessor)
        {
            Debug.Assert(accessor.type == "VEC2");

            JsonBufferView bufferView = GetBufferView(asset, (int)accessor.bufferView);
            if (bufferView is not null)
            {
                int offset = accessor.byteOffset + bufferView.byteOffset;

                ReadOnlySpan<Vec2T<T>> vec = ParseVEC2<T>(bufferView.buffer, offset, accessor.count);

                return vec;
            }

            return null;
        }

        private ReadOnlySpan<Vec3T<T>> ParseAccessorAsVEC3<T>(Asset asset, JsonAccessor accessor)
        {
            Debug.Assert(accessor.type == "VEC3");

            JsonBufferView bufferView = GetBufferView(asset, (int)accessor.bufferView);
            if (bufferView is not null)
            {
                int offset = accessor.byteOffset + bufferView.byteOffset;

                ReadOnlySpan<Vec3T<T>> vec = ParseVEC3<T>(bufferView.buffer, offset, accessor.count);

                return vec;
            }

            return null;
        }

        private ReadOnlySpan<Vec4T<T>> ParseAccessorAsVEC4<T>(Asset asset, JsonAccessor accessor)
        {
            Debug.Assert(accessor.type == "VEC4");

            JsonBufferView bufferView = GetBufferView(asset, (int)accessor.bufferView);
            if (bufferView is not null)
            {
                int offset = accessor.byteOffset + bufferView.byteOffset;

                ReadOnlySpan<Vec4T<T>> vec = ParseVEC4<T>(bufferView.buffer, offset, accessor.count);

                return vec;
            }

            return null;
        }

        private ReadOnlySpan<T> ParseAccessorAsSCALAR<T>(Asset asset, JsonAccessor accessor) where T : struct
        {
            Debug.Assert(accessor.type == "SCALAR");

            JsonBufferView bufferView = GetBufferView(asset, (int)accessor.bufferView);
            if (bufferView is not null)
            {
                JsonBuffer buffer = GetBuffer(asset, bufferView.buffer);

                Int32 target = bufferView.target;

                int offset = accessor.byteOffset + bufferView.byteOffset;

                ReadOnlySpan<T> vec = ParseSCALAR<T>(bufferView.buffer, offset, accessor.count);

                return vec;
            }

            return null;
        }
        /*
                private bool ParseAccessor(JsonAsset asset, JsonAccessor accessor)
                {
                    JsonBufferView bufferView = GetBufferView(asset, (int)accessor.bufferView);
                    if (bufferView is not null)
                    {
                        JsonBuffer buffer = GetBuffer(asset, bufferView.buffer);

                        Int32 target = bufferView.target;

                        int offset = bufferView.byteOffset + accessor.byteOffset;

                        switch (accessor.type)
                        {
                            case "VEC3":
                                var array = ParseVEC3(buffer, offset, accessor.count, accessor.componentType);
                                break;
                            case "SCALAR":
                                var array2 = ParseSCALAR(buffer, offset, accessor.count, accessor.componentType);
                                break;
                            default:
                                Debug.Assert(false);
                                break;
                        }
                    }

                    return true;
                }
        */

        private bool LoadBuffers(Asset asset, string directoryName)
        {
            // Load all the buffers.
            for (int i=0; i<asset.buffers.Count; i++)
            {
                JsonBuffer buffer = asset.buffers[i];

                try
                {
                    string uriPath = Path.Join(directoryName, buffer.uri);

                    using (FileStream fs = File.OpenRead(uriPath))
                    {
                        byte[] temp = new byte[buffer.byteLength];
                        fs.ReadExactly(temp);

                        _buffers[i] = temp;
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Failed to open buffer {buffer.uri} : {e.Message}");

                    return false;
                }
            }

            return true;
        }

        private List<SPoint> points = new List<SPoint>();
        private List<SVector> normals = new List<SVector>();
        private List<SVector> tangents = new List<SVector>();
        private List<ModelData.Texel> texels = new List<ModelData.Texel>();
        private List<SColour> colours = new List<SColour>();
        private List<ModelData.Triangle> triangles = new List<ModelData.Triangle>();


        // Check if a GltfNode has a Mesh with Primitives, or a descendent with a Mesh with Primitves.
        private bool HasRendererableDescendent(GltfNode node)
        {

            foreach (var childNode in node.children)
            {
                if (HasRendererableDescendent(childNode)) return true;
            }

            if (node.mesh is null) return false;

            if (node.mesh.primitives.Count > 0) return true;

            return false;
        }

        // node     Node which we are working on.
        // matrix   Current transform matrix, which is the product of all parent Node matrixes.
        private void CreateGroupFromNode(ModelData.SubGroup group, GltfNode node, Matrix4x4 matrix)
        {
//            Matrix4x4 m = matrix*node.matrix;
            Matrix4x4 m = node.matrix*matrix;

            foreach (var child in node.children)
            {
                if (HasRendererableDescendent(child))
                {
                    ModelData.SubGroup childGroup = new() { Name = child.name ?? "Node" };
                    group.Children.Add(childGroup);

                    CreateGroupFromNode(childGroup, child, m /*matrix*/ /*m2*/);
                }
            }

            GltfMesh mesh = node.mesh;

            if (mesh is not null)
            {
                // XXX We really need to create a SubGroup per Primitive rather than a SubGroup per Mesh, as each Mesh can have
                //  multiple Primitives, each with its own Material.
                int index = 0;
                foreach (GltfPrimitive primitive in mesh.primitives)
                {
                    ModelData.SubGroup primGroup = new() { Name=$"Primitive {index++}" };

                    // Get current offset so we know the offset of existing items.
                    int pointOffset = points.Count;
                    int normalOffset = normals.Count;
                    int tangentOffset = tangents.Count;
                    int colourOffset = colours.Count;
                    int texelOffset = texels.Count;
                    int triangleOffset = triangles.Count;

                    foreach (var position in primitive.positions)
                    {
                        Vector3 v = new(position.x, position.y, position.z);
                        //v = Vector3.Transform(v, node.matrix);
                        v = Vector3.Transform(v, m /*matrix*/);
                        points.Add(new SPoint(v.X, v.Y, v.Z, 1.0f));
                    }

                    if (primitive.texCoords != null)
                    {
                        foreach (var texCoord in primitive.texCoords)
                        {
                            var texel = new ModelData.Texel()
                            {
                                u = texCoord.x,
                                v = texCoord.y
                            };
                            texels.Add(texel);
                        }
                    }

                    if (primitive.colours != null)
                    {
                        foreach (var colour in primitive.colours)
                        {
                            colours.Add(new SColour(colour.x, colour.y, colour.z));
                        }
                    }

                    if (primitive.normals is not null)
                    {
                        foreach (var normal in primitive.normals)
                        {
                            normals.Add(new SVector(normal.x, normal.y, normal.z, 0f));
                        }
                    }

                    if (primitive.tangents is not null)
                    {
                        foreach (var tangent in primitive.tangents)
                        {
                            tangents.Add(new SVector(tangent.x, tangent.y, tangent.z, tangent.w));
                        }
                    }
                    else
                    {
                        ///CalculateTangents(tangents, points, );
                    }

                    for (uint i = 0; i<=primitive.indices.Length - 3; i+=3)
                    {
                        if (i + 3 > primitive.indices.Length) break;

                        try
                        {
                            ModelData.Triangle triangle = new();
                            triangle.v0 = pointOffset + (int)primitive.indices[i];
                            triangle.v1 = pointOffset + (int)primitive.indices[i + 1];
                            triangle.v2 = pointOffset + (int)primitive.indices[i + 2];

                            triangle.n0 = normalOffset + (int)primitive.indices[i];
                            triangle.n1 = normalOffset + (int)primitive.indices[i + 1];
                            triangle.n2 = normalOffset + (int)primitive.indices[i + 2];

                            triangle.c0 = colourOffset + (int)primitive.indices[i];
                            triangle.c1 = colourOffset + (int)primitive.indices[i + 1];
                            triangle.c2 = colourOffset + (int)primitive.indices[i + 2];

                            triangle.t0 = texelOffset + (int)primitive.indices[i];
                            triangle.t1 = texelOffset + (int)primitive.indices[i + 1];
                            triangle.t2 = texelOffset + (int)primitive.indices[i + 2];

                            triangle.tg0 = tangentOffset + (int)primitive.indices[i];
                            triangle.tg1 = tangentOffset + (int)primitive.indices[i + 1];
                            triangle.tg2 = tangentOffset + (int)primitive.indices[i + 2];

                            triangles.Add(triangle);

                            primGroup.Triangles.Add(triangles.Count - 1);

                        }
                        catch (Exception e)
                        {
                            Logger.Instance.Error($"Failed to create group from node : {e.Message}");
                            break;
                        }
                    }

                    // If there are no tangents for this primitive, calculate them.
                    if (primitive.tangents is null && primitive.texCoords is not null && primitive.positions is not null)
                    {
                        Logger.Instance.Info($"Generating tangent vectors for Primitive {mesh.name}");

                        int numPoints = points.Count - pointOffset;
                        int numTriangles = triangles.Count - triangleOffset;

                        SVector[] tangentSum = new SVector[numPoints];
                        SVector[] bitangentSum = new SVector[numPoints];

                        // First accumulate all the tangents and bitangents per triangle vertex.
                        for (int tri=triangleOffset; tri<triangles.Count; tri++)
                        {
                            ModelData.Triangle triangle = triangles[tri];

                            SVector p0 = points[triangle.v0];
                            SVector p1 = points[triangle.v1];
                            SVector p2 = points[triangle.v2];

                            Texel uv0 = texels[triangle.t0];
                            Texel uv1 = texels[triangle.t1];
                            Texel uv2 = texels[triangle.t2];

                            SVector dp1 = p1 - p0;
                            SVector dp2 = p2 - p0;

                            Vector2 duv1 = new(uv1.u - uv0.u, uv1.v - uv0.v);
                            Vector2 duv2 = new(uv2.u - uv0.u, uv2.v - uv0.v);

                            float denom = (duv1.X * duv2.Y - duv1.Y * duv2.X);
                            if (denom == 0f) continue;

                            float r = 1.0f / denom;

                            SVector T = (dp1*duv2.Y - dp2*duv1.Y)*r;
                            SVector B = (dp2*duv1.X - dp1*duv2.X)*r;

                            tangentSum[triangle.v0] += T;
                            tangentSum[triangle.v1] += T;
                            tangentSum[triangle.v2] += T;

                            bitangentSum[triangle.v0] += B;
                            bitangentSum[triangle.v1] += B;
                            bitangentSum[triangle.v2] += B;
                        }

                        for (int i = 0; i<numPoints; i++)
                        {
                            SVector N = normals[normalOffset + i].Normalise();
                            SVector T = tangentSum[i];

                            T = (T - N*N.DotProduct(T)).Normalise();
                            float w = (N.VectorProduct(T).DotProduct(bitangentSum[i]) < 0.0f) ? -1.0f : 1.0f;

                            SVector tangent = new(T.X, T.Y, T.Z, w);
                            tangents.Add(tangent);
                        }

                        for (int tri = triangleOffset; tri<triangles.Count; tri++)
                        {
                            ModelData.Triangle triangle = triangles[tri];

                            triangle.tg0 = triangle.v0;
                            triangle.tg1 = triangle.v1;
                            triangle.tg2 = triangle.v2;
                        }

                        Debug.Assert(tangents.Count == points.Count);
                    }


                    int material = primitive.material;

                    //group.Material = material;
                    primGroup.Material = material;

                    group.Children.Add(primGroup);
                }
            }

        }

        // Create a ModelData instance from the contents of _asset
        private ModelData CreateModel()
        {
            ModelData modelData = new();

            modelData.fileName = _gltfAsset.fileName;
            modelData.directoryName = _gltfAsset.directory;

            points.Clear();
            normals.Clear();
            tangents.Clear();
            texels.Clear();
            colours.Clear();
            triangles.Clear();

            List<ModelData.SubGroup> groups = new();


            // Convert each Node in a Scene to a SubGroup.
            // XXX Only the first Scene is handled.
            if (_gltfAsset.scenes.Count > 0)
            {
                GltfScene gltfScene = _gltfAsset.scenes[0];

                // For each of the top-level Nodes in the Scene...
                foreach (GltfNode node in gltfScene.nodes)
                {
                    ModelData.SubGroup group = new() { Name = node.name };

                    CreateGroupFromNode(group, node, /*node.matrix*/ Matrix4x4.Identity);

                    groups.Add(group);
                }
            }


            modelData.HasVertexNormals = normals.Count > 0;
            modelData.HasVertexTangents = tangents.Count > 0;
            modelData.HasVertexColours = colours.Count > 0;
            modelData.HasVertexTexels = texels.Count > 0;

            // Finally, calculate the face normals for each triangle. These are used when there are no vertex normals.
            foreach (ModelData.Triangle triangle in triangles)
            {
                SVector n = GetNormal(points, triangle);
                //n = -n;
                normals.Add(n);
                triangle.n = normals.Count - 1;
            }

            modelData.points = points.ToArray();
            modelData.colours = colours.ToArray();
            modelData.normals = normals.ToArray();
            modelData.tangents = tangents.ToArray();
            modelData.triangles = triangles.ToArray();
            //modelData.Node = new();
            modelData.Node.Children = groups;
            modelData.texels = texels.ToArray();

            modelData.minX = modelData.minY = modelData.minZ = float.MaxValue;
            modelData.maxX = modelData.maxY = modelData.maxZ = float.MinValue;

            for (int i = 1; i<modelData.points.Length; i++)
            {
                SPoint p = modelData.points[i];
                modelData.minX = Math.Min(p.X, modelData.minX);
                modelData.minY = Math.Min(p.Y, modelData.minY);
                modelData.minZ = Math.Min(p.Z, modelData.minZ);
                modelData.maxX = Math.Max(p.X, modelData.maxX);
                modelData.maxY = Math.Max(p.Y, modelData.maxY);
                modelData.maxZ = Math.Max(p.Z, modelData.maxZ);
            }

            Logger.Instance.Info($"X : [ {modelData.minX} {modelData.maxX} ]");
            Logger.Instance.Info($"Y : [ {modelData.minY} {modelData.maxY} ]");
            Logger.Instance.Info($"Z : [ {modelData.minZ} {modelData.maxZ} ]");

            Logger.Instance.Info($"Number points : {points.Count}");
            //Logger.Instance.Info($"Number faces : {faces.Count}");

            Logger.Instance.Info($"Number triangles : {triangles.Count}");

            string groupInfo = "Groups :";
            if (modelData.Node is not null)
            {
                foreach (ModelData.SubGroup g in modelData.Node.Children)
                {
                    groupInfo += " '" + g.Name + "'";
                }
                Logger.Instance.Info(groupInfo);
            }


            return modelData;
        }

        private static JsonNode GetNode(Asset asset, int index)
        {
            if (asset is not null && asset.nodes is not null)
            {
                if (asset.nodes.Count > index)
                {
                    return asset.nodes[index];
                }
            }

            return null;
        }

        /*
        private GltfScene ParseScenes()
        {

        }

        private GltfNode ParseNode(Asset asset, JsonNode node)
        {
            GltfNode gltfNode = new();

            foreach (int i in node.children )
            {
                JsonNode child = GetNode(asset, i);
            }

        }
        */

        /*
        // XXX Only a single layer of Nodes are supported, so 
        private GltfNode ParseNode(Asset asset, JsonNode node)
        {
            var gltfNode = new GltfNode();

            foreach (var nodeIndex in node.children)
            {
                JsonNode childNode = GetNode(asset, nodeIndex);

                var gltfChildNode = ParseNode(asset, childNode);

                foreach ()
                {

                }
            }
        }
        */

        // Convert the json into an intermediate format.
        private bool ParseAsset(Asset asset, string fileName, string directoryName)
        {
            if (!LoadBuffers(asset, directoryName)) return false;

            _gltfAsset = new()
            {
                fileName = fileName,
                directory = directoryName
            };

            // First of all, extract all the mesh data.
            foreach (JsonMesh mesh in asset.meshes)
            {
                Logger.Instance.Info($"Processing mesh {mesh.name}");

                GltfMesh gltfMesh = new()
                {
                    name = mesh.name
                };

                // A JsonMesh contains one or more Primitives.
                foreach (JsonPrimitive primitive in mesh.primitives)
                {
                    GltfPrimitive gltfPrimitive = new();

                    JsonAttribute attributes = primitive.attributes;

                    if (attributes != null)
                    {
                        if (attributes.POSITION.HasValue)
                        {
                            JsonAccessor accessor = GetAccessor(asset, (int)attributes.POSITION.Value);
                            if (accessor != null)
                            {
                                Debug.Assert((AccessorDataType)accessor.componentType == AccessorDataType.Float);
                                Debug.Assert(accessor.type == "VEC3");

                                ReadOnlySpan<Vec3T<float>> positions = ParseAccessorAsVEC3<float>(asset, accessor);

                                gltfPrimitive.positions = positions.ToArray();
                            }
                        }

                        if (attributes.NORMAL.HasValue)
                        {
                            JsonAccessor accessor = GetAccessor(asset, (int)attributes.NORMAL.Value);
                            if (accessor != null)
                            {
                                Debug.Assert((AccessorDataType)accessor.componentType == AccessorDataType.Float);
                                Debug.Assert(accessor.type == "VEC3");

                                ReadOnlySpan<Vec3T<float>> normals = ParseAccessorAsVEC3<float>(asset, accessor);

                                gltfPrimitive.normals = normals.ToArray();
                            }
                        }

                        if (attributes.TANGENT.HasValue)
                        {
                            JsonAccessor accessor = GetAccessor(asset, (int)attributes.TANGENT.Value);
                            if (accessor != null)
                            {
                                Debug.Assert((AccessorDataType)accessor.componentType == AccessorDataType.Float);
                                Debug.Assert(accessor.type == "VEC4");

                                ReadOnlySpan<Vec4T<float>> tangents = ParseAccessorAsVEC4<float>(asset, accessor);

                                gltfPrimitive.tangents = tangents.ToArray();
                            }
                        }

                        if (attributes.TEXCOORD_0.HasValue)
                        {
                            JsonAccessor accessor = GetAccessor(asset, (int)attributes.TEXCOORD_0.Value);
                            if (accessor != null)
                            {
                                Debug.Assert((AccessorDataType)accessor.componentType == AccessorDataType.Float);
                                Debug.Assert(accessor.type == "VEC2");

                                ReadOnlySpan<Vec2T<float>> texcoords = ParseAccessorAsVEC2<float>(asset, accessor);

                                gltfPrimitive.texCoords = texcoords.ToArray();
                            }
                        }

                        if (attributes.COLOR_0.HasValue)
                        {
                            JsonAccessor accessor = GetAccessor(asset, (int)attributes.COLOR_0.Value);
                            if (accessor != null)
                            {
                                Debug.Assert((AccessorDataType)accessor.componentType == AccessorDataType.Float);   // Can also be unsigned byte or unsigned short.
                                Debug.Assert(accessor.type == "VEC3");

                                ReadOnlySpan<Vec3T<float>> colours = ParseAccessorAsVEC3<float>(asset, accessor);

                                gltfPrimitive.colours = colours.ToArray();
                            }
                        }

                        //Debug.Assert(!attributes.JOINTS_0.HasValue);
                        //Debug.Assert(!attributes.WEIGHTS_0.HasValue);
                        if (attributes.JOINTS_0.HasValue) Logger.Instance.Warn("JOINTS_0 attribute ignored");
                        if (attributes.WEIGHTS_0.HasValue) Logger.Instance.Warn("WEIGHTS_0 attribute ignored");
                    }

                    if (primitive.indices.HasValue)
                    {
                        // indices
                        JsonAccessor accessor = GetAccessor(asset, primitive.indices.Value);
                        if (accessor != null)
                        {
                            AccessorDataType dataType = (AccessorDataType)accessor.componentType;

                            Debug.Assert(dataType == AccessorDataType.UnsignedInt || dataType == AccessorDataType.UnsignedShort);
                            Debug.Assert(accessor.type == "SCALAR");

                            if (dataType == AccessorDataType.UnsignedInt)
                            {
                                ReadOnlySpan<UInt32> indices = ParseAccessorAsSCALAR<UInt32>(asset, accessor);
                                gltfPrimitive.indices = indices.ToArray();
                            }
                            else if (dataType == AccessorDataType.UnsignedShort)
                            {
                                ReadOnlySpan<UInt16> indices = ParseAccessorAsSCALAR<UInt16>(asset, accessor);

                                var array16 = indices.ToArray();
                                var array32 = Array.ConvertAll<UInt16, UInt32>(array16, x => (UInt16)x);
                                gltfPrimitive.indices = array32;
                            }
                        }
                    }

                    gltfPrimitive.material = primitive.material.HasValue ? primitive.material.Value : -1;
                    gltfPrimitive.mode = primitive.mode.HasValue ? (MeshPrimitiveMode)primitive.mode.Value : MeshPrimitiveMode.TRIANGLES;

                    //Dump(primitive.);

                    gltfMesh.primitives.Add(gltfPrimitive);
                }

                _gltfAsset.meshes.Add(gltfMesh);
            }

            // Now extract all the Nodes. The first pass is to create all the GltfNode
            //  instances, the second is to populate the GltfNode children.

            //for (int i = 0; i<asset.nodes.Count; i++)
            //{
            //    _asset.nodes.Add(new GltfNode());
            //}

            _gltfAsset.nodes.Capacity = asset.nodes.Count;

            foreach (JsonNode node in asset.nodes)
            {
                Logger.Instance.Info($"Processing Node {node.name}");

                GltfNode gltfNode = new()
                {
                    name = node.name
                };

                //if (node.children != null)
                //{
                //    foreach (var child in node.children)
                //    {
                //        GltfNode childNode = _asset.nodes[child];
                //        gltfNode.children.Add(childNode);

                //    }
                //}

                // Not all Nodes have a mesh.
                if (node.mesh >= 0)
                {
                    gltfNode.mesh = _gltfAsset.meshes[node.mesh];
                }

                if (node.scale is not null && node.scale.Length == 3)
                {
                    Matrix4x4 S = Matrix4x4.CreateScale(node.scale[0], node.scale[1], node.scale[2]);
                    gltfNode.matrix *= S;
                }

                if (node.rotation is not null && node.rotation.Length == 4)
                {
                    System.Numerics.Quaternion q = new(node.rotation[0], node.rotation[1], node.rotation[2], node.rotation[3]);
                    Matrix4x4 R = Matrix4x4.CreateFromQuaternion(q);
                    //R = Matrix4x4.Transpose(R);
                    gltfNode.matrix *= R;
                }

                if (node.translation is not null && node.translation.Length == 3)
                {
                    Matrix4x4 T = Matrix4x4.CreateTranslation(node.translation[0], node.translation[1], node.translation[2]);
                    gltfNode.matrix *= T;
                }

                if (node.matrix is not null && node.matrix.Length == 16)
                {
                    var M = node.matrix;
                    Matrix4x4 matrix = new(M[0], M[1], M[2], M[3],
                                            M[4], M[5], M[6], M[7],
                                            M[8], M[9], M[10], M[11],
                                            M[12], M[13], M[14], M[15]);

                    //Vector3 scale;
                    System.Numerics.Quaternion rotation;
                    //Vector3 translation;
                    Matrix4x4.Decompose(matrix, out Vector3 scale, out rotation, out Vector3 translation);

                    gltfNode.matrix = matrix;

                }

                /*
                if (node.children is not null)
                {
                    foreach (int childIndex in node.children)
                    {
                        gltfNode.children.Add(_asset.nodes[childIndex]);
                    }
                }
                */

                _gltfAsset.nodes.Add(gltfNode);
            }

            for (int i=0; i<asset.nodes.Count; i++)
            {
                GltfNode gltfNode = _gltfAsset.nodes[i];
                JsonNode node = asset.nodes[i];

                if (node.children != null)
                {
                    foreach (var child in node.children)
                    {
                        GltfNode childNode = _gltfAsset.nodes[child];
                        gltfNode.children.Add(childNode);

                    }
                }

            }

            // XXX We can only handle a single Scene at the moment.
            foreach (JsonScene scene in asset.scenes)
            {
                Logger.Instance.Info($"Processing scene {scene.name}");

                GltfScene gltfScene = new()
                {
                    name = scene.name
                };

                foreach (int nodeIndex in scene.nodes)
                {
                    GltfNode gltfNode = _gltfAsset.nodes[nodeIndex];

                    if (gltfNode is null)
                    {
                        Logger.Instance.Info($"Failed to retrieve GltfNode for {nodeIndex} in Scene {scene.name}");
                        continue;
                    }

                    gltfScene.nodes.Add(gltfNode);
                }

                _gltfAsset.scenes.Add(gltfScene);
            }

            if (asset.textures is not null)
            {
                foreach (var texture in asset.textures)
                {
                    GltfTexture gltfTexture = new()
                    {
                        source = texture.source
                    };
                    _gltfAsset.textures.Add(gltfTexture);
                }
            }

            if (asset.materials is not null)
            {
                foreach (JsonMaterial material in asset.materials)
                {
                    Logger.Instance.Info($"Processing material {material.name}");

                    GltfMaterial gltfMaterial = new()
                    {
                        name = material.name,
                        metallicFactor = material.pbrMetallicRoughness?.metallicFactor ?? 1f,
                        roughnessFactor = material.pbrMetallicRoughness?.roughnessFactor ?? 1f,
                    };

                    if (material.emissiveFactor is not null && material.emissiveFactor.Length == 3)
                    {
                        gltfMaterial.emissiveFactor = new(material.emissiveFactor[0], material.emissiveFactor[1], material.emissiveFactor[2]);
                    }

                    if (material.extensions is not null)
                    {
                        if (material.extensions.KHR_materials_pbrSpecularGlossiness is not null)
                        {
                            int textureIndex = material.extensions.KHR_materials_pbrSpecularGlossiness.diffuseTexture?.index ?? -1;
                            if (textureIndex != -1 && textureIndex < asset.textures.Count)
                            {
                                int imageIndex = asset.textures[textureIndex].source;

                                string baseColor = GetImage(asset, imageIndex);
                                if (baseColor is not null) gltfMaterial.baseColorTexture = baseColor;
                            }

                        }

                        // https://github.com/KhronosGroup/glTF/tree/main/extensions/2.0/Khronos/KHR_materials_emissive_strength
                        if (material.extensions.KHR_materials_emissive_strength is not null)
                        {
                            gltfMaterial.emissiveStrength = material.extensions.KHR_materials_emissive_strength.emissiveStrength;
                        }

                    }


                    if (material.pbrMetallicRoughness is not null)
                    {
                        if (material.pbrMetallicRoughness.baseColorFactor != null)
                        {
                            var bcf = material.pbrMetallicRoughness.baseColorFactor;
                            if (bcf.Length == 4)
                            {
                                gltfMaterial.baseColorFactor = new Vec4T<float>(bcf[0], bcf[1], bcf[2], bcf[3]);
                            }
                        }

                        if (material.pbrMetallicRoughness.baseColorTexture != null)
                        {
                            int textureIndex = material.pbrMetallicRoughness.baseColorTexture.index;
                            int imageIndex = asset.textures[textureIndex].source;

                            string baseColor = GetImage(asset, imageIndex);
                            if (baseColor is not null) gltfMaterial.baseColorTexture = baseColor;
                        }

                        if (material.pbrMetallicRoughness.metallicRoughnessTexture != null)
                        {
                            int textureIndex = material.pbrMetallicRoughness.metallicRoughnessTexture.index;
                            int imageIndex = asset.textures[textureIndex].source;

                            string metallicRoughnessTexture = GetImage(asset, imageIndex);
                            if (metallicRoughnessTexture is not null) gltfMaterial.metallicRoughnessTexture = metallicRoughnessTexture;
                        }
                    }

                    if (material.normalTexture != null)
                    {
                        int textureIndex = material.normalTexture.index;
                        int imageIndex = asset.textures[textureIndex].source;

                        string normalTexture = GetImage(asset, imageIndex);
                        if (normalTexture is not null) gltfMaterial.normalTexture = normalTexture;

                        float scale = material.normalTexture.scale;
                        gltfMaterial.normalScale = scale;
                    }

                    if (material.emissiveTexture is not null)
                    {
                        int textureIndex = material.emissiveTexture.index;
                        if (textureIndex >= 0 && textureIndex < asset.textures.Count)
                        {
                            int imageIndex = asset.textures[textureIndex].source;
                            string emissiveTexture = GetImage(asset, imageIndex);
                            if (emissiveTexture is not null) gltfMaterial.emissiveTexture = emissiveTexture;
                        }
                    }

                    _gltfAsset.materials.Add(gltfMaterial);
                }
            }

            if (asset.samplers is not null)
            {
                foreach (JsonSampler sampler in asset.samplers)
                {
                    Logger.Instance.Info($"Processing sampler {sampler?.name ?? "<unnamed>"}");

                    GltfSampler gltfSampler = new()
                    {
                        name = sampler?.name,
                        magFilter = sampler.magFilter,
                        minFilter = sampler.minFilter,
                        wrapS = sampler.wrapS,
                        wrapT = sampler.wrapT
                    };

                    _gltfAsset.samplers.Add(gltfSampler);
                }
            }

            return true;
        }

        // Try to find the specified image file, which could be a relative or absolute path.
        // directory                Directory from which the glTF was loaded.
        // relativeOrAbsolutePath   JsonTexture path read from the glTF (techinically a URI, but hey).
        private static string FindImage(string directory, string relativeOrAbsolutePath)
        {
            if (File.Exists(relativeOrAbsolutePath))
            {
                return relativeOrAbsolutePath;
            }

            string fileName = Path.GetFileName(relativeOrAbsolutePath);

            string path = Path.Join(directory, fileName);

            if (File.Exists(path))
            {
                return path;
            }

            path = Path.Join(directory, relativeOrAbsolutePath);

            if (File.Exists(path))
            {
                return path;
            }

            return String.Empty;


        }

        // 
        public ModelData ImportModel(string path)
        {
            Logger.Instance.Info($"Importing glTF model {path}");

            _gltfAsset = null;

            ModelData modelData = null;

            string fileName = Path.GetFileName(path);
            string directoryName = Path.GetDirectoryName(path);

            try
            {
                Asset asset;
                using (StreamReader r = new(path))
                {
                    string json = r.ReadToEnd();

                    asset = System.Text.Json.JsonSerializer.Deserialize<Asset>(json);

                }

                if (ParseAsset(asset, fileName, directoryName))
                {
                    modelData = CreateModel();
                }
            }
            catch (Exception e)
            {
                Logger.Instance.Warn($"Failed to parse file {path} : {e.Message}");
                MessageBox.Show($"Failed to parse file {path} : {e.Message}");
            }

            return modelData;
        }

        //
        // Create a List of Materials based on the materials imported from the glTF file
        //
        public List<RayTrace.Material> CreateMaterials()
        {
            List<RayTrace.Material> materials = new();

            if (_gltfAsset is null || _gltfAsset.materials is null)
                return materials;

            foreach (GltfMaterial material in _gltfAsset.materials)
            {
                GltfBrdf brdf = new();
                brdf.MetallicFactor = material.metallicFactor;
                brdf.RoughnessFactor = material.roughnessFactor;

                RayTrace.Material mat = new(MaterialType.BRDF_Gltf)
                {
                    Name = material.name,
                    Brdf = brdf,
                    EmissiveFactor = new Colour(material.emissiveFactor.x, material.emissiveFactor.y, material.emissiveFactor.z),
                    EmissiveStrength = material.emissiveStrength
                };

                if (!string.IsNullOrEmpty(material.baseColorTexture))
                {
                    string path = FindImage(_gltfAsset.directory, material.baseColorTexture);
                    if (!String.IsNullOrEmpty(path))
                    {
                        TextureBitmap texture = new()
                        {
                            ConvertToLinearRGB = true,
                            FileName = path,
                            Origin = TextureBitmap.TextureOrigin.TopLeft
                        };
                        mat.Texture = texture;
                    }
                    else
                    {
                        Logger.Instance.Warn($"Failed to find image {material.baseColorTexture}");
                    }
                }
                else
                {
                    var bcf = material.baseColorFactor;

                    TextureSolid texture = new(new Colour(bcf.x, bcf.y, bcf.z))
                    {
                        ConvertToLinearRGB = false
                    };

                    mat.Texture = texture;
                }

                if (!string.IsNullOrEmpty(material.normalTexture))
                {
                    string path = FindImage(_gltfAsset.directory, material.normalTexture);
                    if (!String.IsNullOrEmpty(path))
                    {
                        NormalMap normalMap = new NormalMap(path)
                        {
                            Scale = material.normalScale,
                            Origin = NormalMap.TextureOrigin.TopLeft
                        };
                        mat.NormalMap = normalMap;
                    }
                    else
                    {
                        Logger.Instance.Warn($"Failed to find image {material.normalTexture}");
                    }
                }

                if (!string.IsNullOrEmpty(material.metallicRoughnessTexture))
                {
                    string path = FindImage(_gltfAsset.directory, material.metallicRoughnessTexture);
                    if (!string.IsNullOrEmpty(path))
                    {
                        TextureBitmap texture = new(path)
                        {
                            ConvertToLinearRGB = false,
                            Origin = TextureBitmap.TextureOrigin.TopLeft
                        };
                        brdf.MetallicRoughnessTexture = texture;
                    }
                }

                if (!string.IsNullOrEmpty(material.emissiveTexture))
                {
                    string path = FindImage(_gltfAsset.directory, material.emissiveTexture);
                    if (!String.IsNullOrEmpty(path))
                    {
                        TextureBitmap emissiveTexture = new()
                        {
                            ConvertToLinearRGB = true,
                            FileName = path,
                            Origin = TextureBitmap.TextureOrigin.TopLeft
                        };

                        mat.EmissiveTexture = emissiveTexture;
                    }
                    else
                    {
                        Logger.Instance.Warn($"Failed to find image {material.emissiveTexture}");
                    }
                }

                materials.Add(mat);
            }

            return materials;
        }
    }
}
