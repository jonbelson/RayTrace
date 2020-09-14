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
        private Vector GetNormal(List<Point> points, ModelData.Triangle triangle)
        {
            Vector v1 = points[triangle.v1] - points[triangle.v0];
            Vector v2 = points[triangle.v2] - points[triangle.v0];
            Vector normal = v1.VectorProduct(v2);
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

        private int Parse(string[] tokens, out double f1, out double f2, out double f3)
        {
            f1 = f2 = f3 = 0.0;

            int count = 0;
            if (Double.TryParse(tokens[1], out f1))
            {
                count++;
                if (Double.TryParse(tokens[2], out f2))
                {
                    count++;
                    if (Double.TryParse(tokens[3], out f3))
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
                    if (numbers.Length == 3)
                    {
                        int i1, i2, i3;
                        if (Int32.TryParse(numbers[0], out i1))
                        {
                            if (Int32.TryParse(numbers[1], out i2))
                            {
                                if (Int32.TryParse(numbers[2], out i3))
                                {
                                    FaceVertex faceVertex = new FaceVertex();
                                    faceVertex.vertex = i1;
                                    faceVertex.texture = i2;
                                    faceVertex.normal = i3;
                                    face.vertices.Add(faceVertex);

                                    Debug.Assert(i1 >= 0 && i2 >= 0 && i3 >= 0);
                                }
                            }
                        }
                    }
                }
            }
            return face;
        }

        // XXX Rewrite using Span<> for performance.
        public ModelData ImportObj(string path)
        {
            ModelData modelData = null;

            string sourcePath = path + @"\source";
            string texturesPath = path + @"\textures";

            List<Point> points = new List<Point>();
            List<Vector> normals = new List<Vector>();
            List<ModelData.Texel> texels = new List<ModelData.Texel>();
            List<Face> faces = new List<Face>();

            // Indexes start at '1' so add a dummy entry to each list.
            points.Add(new Point(0.0, 0.0, 0.0));
            normals.Add(new Vector(0.0, 0.0, 0.0));
            texels.Add(new ModelData.Texel());

            try
            {

                string[] dirs = Directory.GetFiles(sourcePath, "*.obj");
                if (dirs.Length > 0)
                {
                    StreamReader file = new StreamReader(dirs[0]);

                    string line;
                    modelData = new ModelData();
                    char[] delim = new char[] { ' ' };
                    double d1, d2, d3;
                    float f1, f2, f3;
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
                            if (tokens.Length >= 4 && Parse(tokens, out d1, out d2, out d3) == 3)
                            {
                                Point p = new Point(d1, d2, -d3);
                                points.Add(p);
                            }
                        }
                        else if (tokens[0] == "vn")
                        {
                            if (tokens.Length == 4 && Parse(tokens, out d1, out d2, out d3) == 3)
                            {
                                Vector v = new Vector(d1, d2, -d3);
                                normals.Add(v);
                            }
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
                        }
                        else if (tokens[0] == "f")
                        {
                            Face face = ParseFace(tokens);
                            if (face != null)
                            {
                                faces.Add(face);
                            }
                        }
                        else
                        {
                            Console.WriteLine("Ignored line: " + line);
                        }

                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

            if (points.Count > 0)
            {
                //modelData.points = new Point[points.Count];
                modelData.points = points.ToArray();
                //modelData.normals = normals.ToArray();
                modelData.texels = texels.ToArray();

                modelData.minX = modelData.minY = modelData.minZ = Double.MaxValue;
                modelData.maxX = modelData.maxY = modelData.maxZ = Double.MinValue;

                foreach (Point p in modelData.points)
                {
                    modelData.minX = Math.Min(p.X, modelData.minX);
                    modelData.minY = Math.Min(p.Y, modelData.minY);
                    modelData.minZ = Math.Min(p.Z, modelData.minZ);
                    modelData.maxX = Math.Max(p.X, modelData.maxX);
                    modelData.maxY = Math.Max(p.Y, modelData.maxY);
                    modelData.maxZ = Math.Max(p.Z, modelData.maxZ);
                }

                Console.WriteLine($"X : [ {modelData.minX} {modelData.maxX} ]");
                Console.WriteLine($"Y : [ {modelData.minY} {modelData.maxY} ]");
                Console.WriteLine($"Z : [ {modelData.minZ} {modelData.maxZ} ]");

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

                        Vector n = GetNormal(points, triangle);
                        normals.Add(n);
                        triangle.n = normals.Count() - 1;

                        triangle.t0 = face.vertices[0].texture;
                        triangle.t1 = face.vertices[1].texture;
                        triangle.t2 = face.vertices[2].texture;

                        triangles.Add(triangle);
                    }
                    else if (face.vertices.Count == 4)
                    {
                        // Turn quad into two triangles, 013 and 123
                        ModelData.Triangle triangle = new ModelData.Triangle();
                        triangle.v0 = face.vertices[0].vertex;
                        triangle.v1 = face.vertices[1].vertex;
                        triangle.v2 = face.vertices[2].vertex;

                        triangle.n0 = face.vertices[0].normal;
                        triangle.n1 = face.vertices[1].normal;
                        triangle.n2 = face.vertices[2].normal;

                        Debug.Assert(triangle.n != 1);

                        Vector n = GetNormal(points, triangle);
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
                }
                modelData.triangles = triangles.ToArray();

                modelData.normals = normals.ToArray();

                // Load texture
                try
                {
//                    string[] dirs = Directory.GetFiles(texturesPath, "*.png");
                    string[] dirs = Directory.GetFiles(texturesPath);

                    foreach (string file in dirs)
                    {
                        // XXX For now just load the first texture.
                        TextureBitmap texture = new TextureBitmap(file);
                        if (texture.IsValid())
                        {
                            modelData.texture = texture;
                            break;
                        }
                        modelData.texture = new TextureBitmap(file);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to load textures for {path}: " + e.Message);
                }
            }

            return modelData;
        }

    }
}
