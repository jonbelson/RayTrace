using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RayTrace
{
    public class ModelImporter
    {
        private SVector GetNormal(List<SPoint> points, ModelData.Triangle triangle)
        {
            SVector v1 = points[triangle.v1] - points[triangle.v0];
            SVector v2 = points[triangle.v2] - points[triangle.v0];
            SVector normal = v1.VectorProduct(v2);
            return normal.Normalise();
        }

        private int Parse(string[] tokens, out float f1, out float f2)
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

        private int Parse(string[] tokens, out float f1, out float f2, out float f3)
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

        private Face ParseFace(string[] tokens)
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
                        if (Int32.TryParse(numbers[0], out int v))
                        {
                            if (Int32.TryParse(numbers[1], out int vn))
                            {
                                FaceVertex faceVertex = new FaceVertex();
                                faceVertex.vertex = v;
                                faceVertex.texture = 0;
                                faceVertex.normal = vn;
                                face.vertices.Add(faceVertex);

                                Debug.Assert(v >= 0 && vn >= 0);
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

        //
        // Attempt to load a .OBJ file.
        // XXX Rewrite using Span<> for performance.
        public ModelData ImportObj(string path)
        {
            ModelData modelData = null;

            List<SPoint> points = new List<SPoint>();
            List<SVector> normals = new List<SVector>();
            List<ModelData.Texel> texels = new List<ModelData.Texel>();
            List<Face> faces = new List<Face>();

            // Indexes start at '1' so add a dummy entry to each list.
            points.Add(new SPoint(0.0f, 0.0f, 0.0f));
            normals.Add(new SVector(0.0f, 0.0f, 0.0f));

            try
            {
                StreamReader file = new StreamReader(path);

                modelData = new ModelData();
                modelData.fileName = path;

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
                    else
                    {
                        Debug.WriteLine("Ignored line: " + line);
                    }

                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
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

                    // Try to figure out if the normals need inverting.
                    //if ()

                    List<ModelData.Triangle> triangles = new List<ModelData.Triangle>();
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

                            SVector n = GetNormal(points, triangle);
                            normals.Add(n);
                            triangle.n = normals.Count() - 1;

                            triangle.t0 = face.vertices[0].texture;
                            triangle.t1 = face.vertices[1].texture;
                            triangle.t2 = face.vertices[2].texture;

                            triangles.Add(triangle);
                        }
                        else if (face.vertices.Count > 3)
                        {
#if false
                            // Turn quad into two triangles, 013 and 123
                            ModelData.Triangle triangle = new ModelData.Triangle();
                            triangle.v0 = face.vertices[0].vertex;
                            triangle.v1 = face.vertices[1].vertex;
                            triangle.v2 = face.vertices[2].vertex;

                            triangle.n0 = face.vertices[0].normal;
                            triangle.n1 = face.vertices[1].normal;
                            triangle.n2 = face.vertices[2].normal;

                            Debug.Assert(triangle.n != 1);

                            SVector n = GetNormal(points, triangle);
                            normals.Add(n);
                            triangle.n = normals.Count() - 1;

                            triangle.t0 = face.vertices[0].texture;
                            triangle.t1 = face.vertices[1].texture;
                            triangle.t2 = face.vertices[2].texture;

                            triangles.Add(triangle);

                            triangle = new ModelData.Triangle();
                            triangle.v0 = face.vertices[0].vertex;
                            triangle.v1 = face.vertices[2].vertex;
                            triangle.v2 = face.vertices[3].vertex;

                            triangle.n0 = face.vertices[0].normal;
                            triangle.n1 = face.vertices[2].normal;
                            triangle.n2 = face.vertices[3].normal;

                            Debug.Assert(triangle.n != 1);

                            n = GetNormal(points, triangle);
                            normals.Add(n);
                            triangle.n = normals.Count() - 1;

                            triangle.t0 = face.vertices[0].texture;
                            triangle.t1 = face.vertices[2].texture;
                            triangle.t2 = face.vertices[3].texture;

                            triangles.Add(triangle);
                        }
                        else
                        {
#endif
                            //Debug.WriteLine($"Skipping face with {face.vertices.Count} vertices");

                            int numTris = face.vertices.Count - 2;
                            //int v1 = 0; // Anchor point.
                            int v2 = 1; // Second point.
                            int v3 = 2;
                            for (int v=0; v<numTris; v++)
                            {
                                ModelData.Triangle triangle = new ModelData.Triangle();
                                triangle.v0 = face.vertices[0].vertex;
                                triangle.v1 = face.vertices[v2].vertex;
                                triangle.v2 = face.vertices[v3].vertex;

                                triangle.n0 = face.vertices[0].normal;
                                triangle.n1 = face.vertices[v2].normal;
                                triangle.n2 = face.vertices[v3].normal;

                                Debug.Assert(triangle.n != 1);

                                SVector n = GetNormal(points, triangle);
                                normals.Add(n);
                                triangle.n = normals.Count() - 1;

                                triangle.t0 = face.vertices[0].texture;
                                triangle.t1 = face.vertices[v2].texture;
                                triangle.t2 = face.vertices[v3].texture;

                                triangles.Add(triangle);

                                v2++;
                                v3 = (v3 + 1) % face.vertices.Count;
                            }

                        }
                    }
                    modelData.triangles = triangles.ToArray();

                    Console.WriteLine($"Number triangles : {triangles.Count}");

                    //modelData.HasVertexNormals = normals.Count > 1;

                    // Indexes start at '1' so add a dummy entry to the list.
                    modelData.normals = normals.ToArray();

                    // Indexes start at '1' so add a dummy entry to the list.
                    texels.Add(new ModelData.Texel());
                    modelData.texels = texels.ToArray();

                }
            }

                //return (modelData, modelData.texture);

            return modelData;
        }

    }
}
