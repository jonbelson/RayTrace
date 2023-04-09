using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
//using System.Windows.Shapes;
//using System.Windows.Shapes;

namespace RayTrace
{
    public class ModelImporter
    {
        public static SVector GetNormal(List<SPoint> points, ModelData.Triangle triangle)
        {
            SVector v1 = points[triangle.v1] - points[triangle.v0];
            SVector v2 = points[triangle.v2] - points[triangle.v0];
//            SVector normal = v1.VectorProduct(v2);
            SVector normal = v2.VectorProduct(v1);
            return normal.Normalise();
        }

        private static int Parse(string[] tokens, out float f1, out float f2)
        {
            f1 = f2 = 0.0f;

            int count = 0;
            if (Single.TryParse(tokens[1], out f1))
            {
                count++;
                if (Single.TryParse(tokens[2], out f2))
                {
                    count++;
                }
            }
            return count;
        }

        private static int Parse(string[] tokens, out float f1, out float f2, out float f3)
        {
            f1 = f2 = f3 = 0.0f;

            int count = 0;
            if (float.TryParse(tokens[1], out f1))
            {
                count++;
                if (float.TryParse(tokens[2], out f2))
                {
                    count++;
                    if (float.TryParse(tokens[3], out f3))
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        public struct FaceVertex
        {
            public int vertex;
            public int texture;
            public int normal;
        }

        public class Face
        {
            public List<FaceVertex> vertices = new List<FaceVertex>();
        }

        public class Group
        {
            public string name;
            public List<Face> faces = new();
        }

        public class MaterialRgb
        {
            public float red;
            public float green;
            public float blue;
        }

        /*
            -blendu on | off
            -blendv on | off
            -cc on | off
            -clamp on | off
            -imfchan r | g | b | m | l | z
            -mm base gain
            -o u v w
            -s u v w
            -t u v w
            -texres value
        */
        public class TextureOptions
        {

        }

        public class ObjMaterial
        {
            public string Name;
            public string Directory;     // Path specified in material file (whcih may or may not be correct).

            public MaterialRgb Ambient; // Ka;    // Ambient reflectivity.
            public MaterialRgb Diffuse; // Kd;    // Diffuse reflectivity.
            public MaterialRgb Specular;    // Ks;    // Specular reflectivity.
            public MaterialRgb Transmission;    // Tf;    // Transmission filter (i.e. which colours can pass through).

            public int Illum;    // Illumination model to use.

            public float dissolve = 1.0f;  // 'Dissolve'. 1.0 is opaque, 0.0 is transparent.

            public float Shininess; // Ns;    // Specular exponent.
            public float Sharpness; // Sharpness of reflections from reflection map.
            public float Ior;   // Ni;    // Index of refraction. Optical density of surface.

            public string AmbientFilename;
            public string DiffuseFilename;      // map_Kd
            public string BumpFilename;         // map_Bump

            // Texture map statements.
            public TextureOptions map_Ka;
            public TextureOptions map_Kd;
            public TextureOptions map_Ks;
            public TextureOptions map_Ns;

            // Reflection map statements.
        }

        public class MaterialLib
        {
            //public string Name;
            //            public Mtl[] Materials;
            public List<ObjMaterial> Materials = new();
        }

        public MaterialLib MaterialLibrary { get; } = new();

        private static Face ParseFace(string[] tokens)
        {
            Face face = null;
            if (tokens.Length > 0)
            {
                face = new Face();

                for (int i = 1; i < tokens.Length; i++)
                {
                    string[] numbers = tokens[i].Split('/');
                    if (numbers.Length == 2)
                    {
                        // 'v/vt' format.
                        if (Int32.TryParse(numbers[0], out int v))
                        {
                            if (Int32.TryParse(numbers[1], out int vt))
                            {
                                FaceVertex faceVertex = new FaceVertex();
                                faceVertex.vertex = v;
                                faceVertex.texture = vt;
                                faceVertex.normal = 0;
                                face.vertices.Add(faceVertex);

                                Debug.Assert(v >= 0 && vt >= 0);
                            }
                        }
                    }
                    else if (numbers.Length == 3)
                    {
                        // 'v/vt/vn' format.
                        int vt;
                        if (Int32.TryParse(numbers[0], out int v))
                        {
                            vt = 0;
                            if (!String.IsNullOrEmpty(numbers[1]) && Int32.TryParse(numbers[1], out vt))
                            {
                            }

                            if (Int32.TryParse(numbers[2], out int vn))
                            {
                                FaceVertex faceVertex = new FaceVertex();
                                faceVertex.vertex = v;
                                faceVertex.texture = vt;
                                faceVertex.normal = vn;
                                face.vertices.Add(faceVertex);

                                Debug.Assert(v >= 0 && vt >= 0 && vn >= 0);
                            }
                        }
                    }
                    else if (numbers.Length == 1)
                    {
                        // 'v' format
                        if (Int32.TryParse(numbers[0], out int v))
                        {
                            FaceVertex faceVertex = new FaceVertex();
                            faceVertex.vertex = v;
                            faceVertex.texture = 0;
                            faceVertex.normal = -1;
                            face.vertices.Add(faceVertex);

                            Debug.Assert(v >= 0);
                        }
                    }
                }
            }
            return face;
        }


        private static void AddTriangles(ModelData.SubGroup group, List<Face> faces, List<ModelData.Triangle> triangles, List<SPoint> points, List<SVector> normals)
        {
            foreach (Face face in faces)
            {
                if (face.vertices.Count == 3)
                {
                    // Triangle, so just copy across
                    ModelData.Triangle triangle = new ModelData.Triangle();
                    triangle.v0 = face.vertices[0].vertex;
                    triangle.v1 = face.vertices[1].vertex;
                    triangle.v2 = face.vertices[2].vertex;

                    triangle.n0 = face.vertices[0].normal;
                    triangle.n1 = face.vertices[1].normal;
                    triangle.n2 = face.vertices[2].normal;

                    //Debug.Assert(triangle.n != 1);

                    //SVector n = GetNormal(points, triangle);
                    //faceNormals.Add(n);
                    //triangle.n = normals.Count() - 1;

                    triangle.t0 = face.vertices[0].texture;
                    triangle.t1 = face.vertices[1].texture;
                    triangle.t2 = face.vertices[2].texture;

                    triangles.Add(triangle);

                    group.Triangles.Add(triangles.Count - 1);
                }
                else if (face.vertices.Count > 3)
                {
                    //Debug.Assert(face.vertices.Count < 5);

                    List<SPoint> polygon = new();
                    List<int> mapping = new();

                    for (int i=0; i<face.vertices.Count; i++)
                    {
                        int index = face.vertices[i].vertex;
                        polygon.Add(new(points[index]));
                        mapping.Add(index);
                    }

                    Triangulator triangulator = new (polygon);
                    if (triangulator.Triangulate(normals[face.vertices[0].normal]))
                    {
                        var tris = triangulator.Triangles;
                        foreach (var tri in tris)
                        {
                            // Convert polygoniser vertex indices back into our ones.
                            int idx1 = mapping[tri.v1];
                            int idx2 = mapping[tri.v2];
                            int idx3 = mapping[tri.v3];


                            ModelData.Triangle triangle = new();
                            triangle.v0 = idx1;
                            triangle.v1 = idx2;
                            triangle.v2 = idx3;

                            triangle.n0 = face.vertices[tri.v1].normal;
                            triangle.n1 = face.vertices[tri.v2].normal;
                            triangle.n2 = face.vertices[tri.v3].normal;

                            //Debug.Assert(triangle.n != 1);

                            //SVector n = GetNormal(points, triangle);
                            //faceNormals.Add(n);
                            //triangle.n = normals.Count() - 1;

                            triangle.t0 = face.vertices[tri.v1].texture;
                            triangle.t1 = face.vertices[tri.v2].texture;
                            triangle.t2 = face.vertices[tri.v3].texture;

                            triangles.Add(triangle);

                            group.Triangles.Add(triangles.Count - 1);


                        }
                    }

/*
                    int numTris = face.vertices.Count - 2;
                    //int v1 = 0; // Anchor point.
                    int v2 = 1; // Second point.
                    int v3 = 2;
                    for (int v = 0; v < numTris; v++)
                    {
                        ModelData.Triangle triangle = new ModelData.Triangle();
                        triangle.v0 = face.vertices[0].vertex;
                        triangle.v1 = face.vertices[v2].vertex;
                        triangle.v2 = face.vertices[v3].vertex;

                        triangle.n0 = face.vertices[0].normal;
                        triangle.n1 = face.vertices[v2].normal;
                        triangle.n2 = face.vertices[v3].normal;

                        Debug.Assert(triangle.n != 1);

                        //SVector n = GetNormal(points, triangle);
                        //normals.Add(n);
                        //triangle.n = normals.Count() - 1;

                        triangle.t0 = face.vertices[0].texture;
                        triangle.t1 = face.vertices[v2].texture;
                        triangle.t2 = face.vertices[v3].texture;

                        triangles.Add(triangle);
                        group.Triangles.Add(triangles.Count - 1);

                        v2++;
                        v3 = (v3 + 1) % face.vertices.Count;
                    }
*/
                }
            }
        }

        public bool ParseMtlLib(string fileName)
        {
            try
            {
                StreamReader fileStream = new(fileName);

                string directoryName = Path.GetDirectoryName(fileName); // XXX Specify path to function? Material paths are often incorrect.

                ObjMaterial material = null;// new();

                //Mtl mtl = new();

                char[] delim = new char[] { ' ' };
                string line;
                while ((line = fileStream.ReadLine()) != null)
                {
                    if (line.Length == 0) continue;
                    if (line[0] == '#') continue;

                    string[] tokens = line.Split(delim, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length == 0) continue;

                    string cmd = tokens[0].Trim();

                    if (cmd == "newmtl")
                    {
                        if (material != null)
                        {
                            MaterialLibrary.Materials.Add(material);
                        }

                        if (tokens.Length > 1)
                        {
                            material = new();
                            material.Name = tokens[1];
                            material.Directory = directoryName;
                        }
                    }
                    else if (cmd == "Ka")
                    {
                        if (tokens.Length == 4 && Parse(tokens, out float r, out float g, out float b) == 3)
                        {
                            material.Ambient = new() { red = r, green = g, blue = b };
                        }
                    }
                    else if (cmd == "Kd")
                    {
                        if (tokens.Length == 4 && Parse(tokens, out float r, out float g, out float b) == 3)
                        {
                            material.Diffuse = new() { red = r, green = g, blue = b };
                        }
                    }
                    else if (cmd == "Ks")
                    {
                        if (tokens.Length == 4 && Parse(tokens, out float r, out float g, out float b) == 3)
                        {
                            material.Specular = new() { red = r, green = g, blue = b };
                        }
                    }
                    else if (cmd == "map_Ka")
                    {
                        int idx = 1;
                        while (idx < tokens.Length)
                        {
                            if (tokens[idx].Length > 0)
                            {
                                if (tokens[idx][0] == '-')
                                {
                                    // XXX Process options
                                }
                                else
                                {
                                    string name = string.Join(" ", tokens, idx, tokens.Length - idx);   // Handle filenames with spaces.
                                    material.AmbientFilename = name;    // Path.Join(directoryName, name);
                                    idx = tokens.Length;
                                }
                            }
                            idx++;
                        }
                    }
                    else if (cmd == "map_Kd")
                    {
                        int idx = 1;
                        while (idx < tokens.Length)
                        {
                            if (tokens[idx].Length > 0)
                            {
                                if (tokens[idx][0] == '-')
                                {
                                    // XXX Process options
                                }
                                else
                                {
                                    string name = string.Join(" ", tokens, idx, tokens.Length - idx);   // Handle filenames with spaces.
                                    material.DiffuseFilename = name;    // Path.Join(directoryName, name);
                                    idx = tokens.Length;
                                }
                            }
                            idx++;
                        }
                    }
                }

                if (material != null)
                {
                    MaterialLibrary.Materials.Add(material);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Failed to load texture '{fileName}'");
                return false;
            }

            return true;
        }

        //
        // Attempt to load a .OBJ file.
        // XXX Rewrite using Span<> for performance.
        public ModelData ImportObj(string path)
        {
            ModelData modelData = null;

            string fileName = Path.GetFileName(path);
            string directoryName = Path.GetDirectoryName(path);

            List<SPoint> points = new List<SPoint>();
            List<SVector> normals = new List<SVector>();
            List<ModelData.Texel> texels = new List<ModelData.Texel>();
            List<Face> faces = new List<Face>();
            List<ModelData.Triangle> triangles = new List<ModelData.Triangle>();

            List<ModelData.SubGroup> groups = new();

            ModelData.SubGroup group = new() { Name = "default" };
            //groups.Add(group);

            // Indexes start at '1' so add a dummy entry to each list.
            points.Add(new SPoint(0.0f, 0.0f, 0.0f));
            normals.Add(new SVector(0.0f, 0.0f, 0.0f));

            texels.Add(new ModelData.Texel());

            try
            {
                StreamReader file = new StreamReader(path);

                modelData = new ModelData();
                modelData.fileName = fileName;

                char[] delim = new char[] { ' ' };
                //double d1, d2, d3;
                float f1, f2, f3;
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    if (line.Length == 0) continue;
                    if (line[0] == '#') continue;

                    string[] tokens = line.Split(delim, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length == 0)
                    {
                        continue;
                    }

                    if (tokens[0] == "v")
                    {
                        if (tokens.Length >= 4 && Parse(tokens, out f1, out f2, out f3) == 3)
                        {
                            SPoint p = new SPoint(f1, f2, -f3);
                            points.Add(p);
                        }
                        else
                            Debug.WriteLine($"v parse failed with {tokens}");

                    }
                    else if (tokens[0] == "vn")
                    {
                        if (tokens.Length == 4 && Parse(tokens, out f1, out f2, out f3) == 3)
                        {
                            SVector v = new SVector(f1, f2, -f3);
                            v = v.Normalise();
                            normals.Add(v);
                        }
                        else
                            Debug.WriteLine($"vn parse failed with {tokens}");
                    }
                    else if (tokens[0] == "vt")
                    {
                        if (tokens.Length == 3 && Parse(tokens, out f1, out f2) == 2)
                        {
                            ModelData.Texel t = new ModelData.Texel();
                            t.u = f1;
                            t.v = f2;
                            texels.Add(t);
                        }
                        else if (tokens.Length == 4 && Parse(tokens, out f1, out f2, out f3) == 3)
                        {
                            ModelData.Texel t = new ModelData.Texel();
                            t.u = f1;
                            t.v = f2;
                            t.w = f3;
                            texels.Add(t);
                        }
                        else
                            Debug.WriteLine($"vt parse failed with {tokens}");
                    }
                    else if (tokens[0] == "f")
                    {
                        Face face = ParseFace(tokens);
                        if (face != null)
                        {
                            faces.Add(face);
                        }
                        else
                            Debug.WriteLine($"f parse failed with {tokens}");
                    }
                    else if (tokens[0] is "g" or "o")
                    {
                        // Add any faces from the current group.
                        if (faces.Any())
                        {
                            AddTriangles(group, faces, triangles, points, normals);
                            group.Material = MaterialLibrary.Materials.Count - 1;
                            groups.Add(group);
                        }
                        faces.Clear();

                        string name = "Default";

                        if (tokens.Length > 0)
                        {
                            name = string.Join("_", tokens, 1, tokens.Length - 1);
                        }

                        //Debug.Assert(!String.Equals(name, "Cylinder.043_Cylinder.036"));

                        // Create the new group.
                        group = new() { Name = name };
                    }
                    else if (tokens[0] == "mtllib")
                    {
                        for (int i=1; i<tokens.Length; i++)
                        {
                            string materialPath = Path.Join(directoryName, tokens[i]);
                            if (!ParseMtlLib(materialPath))
                            {
                                Debug.WriteLine("Failed to parse material file: " + tokens[i]);
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Ignored line: " + line);
                    }

                }

                if (faces.Any())
                {
                    AddTriangles(group, faces, triangles, points, normals);
                    groups.Add(group);
                }
            }
            catch (Exception e)
            {
                _ = MessageBox.Show(e.Message);
            }

            if (modelData != null)
            {
                if (points.Count > 0)
                {
                    modelData.points = points.ToArray();
                    //modelData.normals = normals.ToArray();
                    //modelData.texels = texels.ToArray();

                    modelData.minX = modelData.minY = modelData.minZ = float.MaxValue;
                    modelData.maxX = modelData.maxY = modelData.maxZ = float.MinValue;

                    //foreach (SPoint p in modelData.points)
                    for (int i=1; i<modelData.points.Length; i++)
                    {
                        SPoint p = modelData.points[i];
                        modelData.minX = Math.Min(p.X, modelData.minX);
                        modelData.minY = Math.Min(p.Y, modelData.minY);
                        modelData.minZ = Math.Min(p.Z, modelData.minZ);
                        modelData.maxX = Math.Max(p.X, modelData.maxX);
                        modelData.maxY = Math.Max(p.Y, modelData.maxY);
                        modelData.maxZ = Math.Max(p.Z, modelData.maxZ);
                    }

                    Debug.WriteLine($"X : [ {modelData.minX} {modelData.maxX} ]");
                    Debug.WriteLine($"Y : [ {modelData.minY} {modelData.maxY} ]");
                    Debug.WriteLine($"Z : [ {modelData.minZ} {modelData.maxZ} ]");

                    Debug.WriteLine($"Number points : {points.Count}");
                    Debug.WriteLine($"Number faces : {faces.Count}");

                    modelData.HasVertexNormals = normals.Count > 1;

                    // XXX Try to figure out if the normals need inverting.

                    //modelData.triangles = triangles.ToArray();
                    //group.triangles = triangles.ToArray();
                    modelData.triangles = triangles.ToArray();
                    modelData.SubGroups = groups.ToArray();

                    Debug.WriteLine($"Number triangles : {triangles.Count}");

                    string groupInfo = "Groups :";
                    foreach (ModelData.SubGroup g in modelData.SubGroups)
                    {
                        groupInfo += " '" + g.Name + "'";
                    }
                    Debug.WriteLine(groupInfo);

                    string materialsInfo = "Materials :";
                    foreach (var material in MaterialLibrary.Materials)
                    {
                        materialsInfo += " '" + material.Name + "'";
                    }
                    Debug.WriteLine(materialsInfo);


                    //modelData.HasVertexNormals = normals.Count > 1;

                    // Finally, calculate the face normals for each triangle. These are used when there are no vertex normals.
                    foreach (ModelData.Triangle triangle in triangles)
                    {
                        SVector n = GetNormal(points, triangle);
                        normals.Add(n);
                        triangle.n = normals.Count - 1;
                    }

                    // Indexes start at '1' so add a dummy entry to the list.
                    modelData.normals = normals.ToArray();

                    // Indexes start at '1' so add a dummy entry to the list.
                    //texels.Add(new ModelData.Texel());
                    modelData.texels = texels.ToArray();

                }
            }

                //return (modelData, modelData.texture);

            return modelData;
        }

        private string FindTexture(string directory, string relativeOrAbsolutePath)
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

            return String.Empty;
        }

       public List<Material> CreateMaterials()
        {
            List<Material> materials = new();

            foreach (var mat in MaterialLibrary.Materials)
            {
                Material material = new(MaterialType.Diffuse);

                material.Name = mat.Name;

                if (mat.BumpFilename != null)
                {
                    string path = FindTexture(mat.Directory, mat.BumpFilename);
                    if (!String.IsNullOrEmpty(path))
                    {
                        NormalMap normalMap = new(path);
                        material.NormalMap = normalMap;
                    }
                }

                if (mat.DiffuseFilename != null)
                {
                    string path = FindTexture(mat.Directory, mat.DiffuseFilename);
                    if (!String.IsNullOrEmpty(path))
                    {
                        TextureBitmap texture = new(path);
                        material.Texture = texture;
                    }
                }

                materials.Add(material);
            }

            return materials;
        }

    }
}
