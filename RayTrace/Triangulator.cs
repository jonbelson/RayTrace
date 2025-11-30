using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Media3D;

namespace RayTrace
{
    internal class Triangulator
    {
        private struct Vec2D
        {
            public float x;
            public float y;

            public Vec2D(float x, float y) { this.x = x; this.y = y; }

            public float Magnitude() => (float) Math.Sqrt(x*x + y*y);
            public Vec2D Normalised()
            {
                float magnitude = Magnitude();
                if (magnitude != 0)
                {
                    return new Vec2D(x/magnitude, y/magnitude);
                }
                return new Vec2D(0, 0);
            }
            public static Vec2D operator-(in Vec2D v1, in Vec2D v2) => new(v1.x - v2.x, v1.y - v2.y);

        }

        private enum Plane
        {
            XY, XZ, YZ
        }

        private enum Orientation
        {
            CW, CCW
        }

        public struct Triangle
        {
            public int v1, v2, v3;

            public Triangle(int v1, int v2, int v3) { this.v1 = v1; this.v2 = v2; this.v3 = v3; }
        }

        // List of points describing the polygon to be polygonised.
        private readonly List<SPoint> Points;

        // List of 2D points representing the polygon to be polygonised.
        private readonly List<Vec2D> Vertices = new();

        // List of indexes into Vertices representing the polygon being triangulated.
        // Indices are removed as triangles are extracted.
        private List<int> Indices = new();

        // List of triangles in polygonised polygon.
        public readonly List<Triangle> Triangles = new();

       // Calculate the determinate from three consecutive vectices.
        // If det > 0, points are CCW, else CW.
        static float GetDeterminate(in Vec2D v1, in Vec2D v2, in Vec2D v3)
        {
            return (v2.x - v1.x)*(v3.y - v2.y) - (v3.x - v2.x)*(v2.y - v1.y);
        }

        // Calculate barycentric u, v of a triangle given by three consecutive vertices.
        // A, B, C respectively.
        // https://www.scratchapixel.com/lessons/3d-basic-rendering/ray-tracing-rendering-a-triangle/barycentric-coordinates.html
        static (float, float, float) GetBarycentricUV(in Vec2D v1, in Vec2D v2, in Vec2D v3, in Vec2D p)
        {
            float areaABC = GetArea(v1, v2, v3);

            float areaCAP = GetArea(v3, v1, p);
            float areaABP = GetArea(v1, v2, p);
            float areaBCP = GetArea(v2, v3, p);

            float u = areaCAP/areaABC;
            float v = areaABP/areaABC;
            //float w = areaBCP/areaABC;

            float w = 1.0f - u - v;

            return (u, v, w);
        }

        static Vector3D CrossProduct(in Vec2D v1, in Vec2D v2)
        {
            return new Vector3D(0, 0, v1.x*v2.y - v1.y*v2.x);
        }

        static float CrossProductZ(in Vec2D v1, in Vec2D v2)
        {
            return v1.x*v2.y - v1.y*v2.x;
        }

        static float DotProduct(in Vec2D v1, in Vec2D v2)
        {
            return v1.x*v2.y - v2.x*v1.y;
        }

        // Check if p1 is on the same of refLine as p2. 
        static bool SameSide(in Vec2D refLine, in Vec2D a, in Vec2D p1, in Vec2D p2)
        {
            float cp1 = CrossProductZ(refLine, p1 - a);
            float cp2 = CrossProductZ(refLine, p2 - a);

            if (cp1 >= 0.0f && cp2 >= 0.0f) return true;
            else if (cp1 < 0.0f && cp2 < 0.0f) return true;
            
            return false;
        }

        //static bool GetSide(in Vec2D v1, in Vec2D v2)
        //{
        //    CrossProduct(v1, v2).Z > 0.0f;
        //}

        static bool IsPointInTriangle(in Vec2D v1, in Vec2D v2, in Vec2D v3, in Vec2D p)
        {
            //if (GetSide(p, v1) == GetSide() && GetSide() == GetSide() && GetSide() == GetSide())
            //{
            //    return true;
            //}

            //return false;

            var (u, v, w) = GetBarycentricUV(v1, v2, v3, p);
            //float w = 1.0f - u - v;

//            return (u >= 0.0f && u <= 1.0f) && (v >= 0.0f && v <= 1.0f) && (w >= 0.0f && w <= 1.0f);
            return (u > 0.0f && u < 1.0f) && (v > 0.0f && v < 1.0f) && (w > 0.0f && w < 1.0f);
        }

        // Check if point p is in the triangle formed by vertices v1, v2 and v3.
        bool IsPointInTriangle(int v1, int v2, int v3, int p)
        {
            return IsPointInTriangle(Vertices[Indices[v1]], Vertices[Indices[v2]], Vertices[Indices[v3]], Vertices[Indices[p]]);
        }

        // Check if point p is in the triangle formed by v-1, v, v+1
        bool IsPointInTriangleAt(int v, int p)
        {
            int v1 = (v > 0) ? v - 1 : Indices.Count - 1;
            int v2 = v;
            int v3 = (v < Indices.Count - 1) ? v + 1 : 0;

            if (v1 == p || v3 == p) return false;

            return IsPointInTriangle(Vertices[Indices[v1]], Vertices[Indices[v2]], Vertices[Indices[v3]], Vertices[Indices[p]]);
        }

        bool IsPointInTriangleAt2(int v, int p)
        {
            int v1 = (v > 0) ? v - 1 : Indices.Count - 1;
            int v2 = v;
            int v3 = (v < Indices.Count - 1) ? v + 1 : 0;

            if (v1 == p || v3 == p) return false;


            Vec2D a = Vertices[Indices[v1]];
            Vec2D b = Vertices[Indices[v2]];
            Vec2D c = Vertices[Indices[v3]];
            Vec2D pp = Vertices[Indices[p]];

            Vec2D ab = b - a;
            Vec2D bc = c - b;
            Vec2D ca = a - c;

            if (SameSide(ab, a, pp, c) && SameSide(bc, b, pp, a) && SameSide(ca, c, pp, b)) return true;

            return false;

            //return IsPointInTriangle(Vertices[Indices[v1]], Vertices[Indices[v2]], Vertices[Indices[v3]], Vertices[Indices[p]]);
        }


        void AddOutputTriangle(int v)
        {
            int v1 = (v > 0) ? v - 1 : Indices.Count - 1;
            int v2 = v;
            int v3 = (v < Indices.Count - 1) ? v + 1 : 0;

            Triangle tri = new(Indices[v1], Indices[v2], Indices[v3]);
            Triangles.Add(tri);
        }

        // Calculate area of a triangle given by three consecutive vertices.
        // A, B, C respectively.
        static float GetArea(in Vec2D v1, in Vec2D v2, in Vec2D v3)
        {
/*
            // Get AB and AC vectors.
            Vec2D v2v1 = new(v1.x - v2.x, v1.y - v2.y);
            Vec2D v2v3 = new(v3.x - v2.x, v3.y - v2.y);

            // https://www.scratchapixel.com/lessons/3d-basic-rendering/ray-tracing-rendering-a-triangle/barycentric-coordinates.html
            float areaParallelogram = Math.Abs(v2v1.Magnitude()*v2v3.Magnitude());

            float area = 0.5f*areaParallelogram;

            return Math.Abs(area);
*/

            // Get AB and AC vectors.
            Vec2D v2v1 = new(v2.x - v1.x, v2.y - v1.y);
            Vec2D v3v1 = new(v3.x - v1.x, v3.y - v1.y);

            // Area of triangle is half the area of the parallelogram AB + AC.
            // https://demonstrations.wolfram.com/Signed2DTriangleAreaFromTheCrossProductOfEdgeVectors/
            float area = 0.5f*(-v1.y*v2.x + v1.x*v2.y + v1.y*v3.x - v2.y*v3.x - v1.x*v3.y + v2.x*v3.y);

            float a = 0.5f*(v2v1.x*v3v1.y - v3v1.x*v2v1.y);

            // https://www.scratchapixel.com/lessons/3d-basic-rendering/ray-tracing-rendering-a-triangle/barycentric-coordinates.html
//            float areaParallelogram = Math.Abs(v2v1.Magnitude()*v3v1.Magnitude());

//            area = 0.5f*areaParallelogram;

            return Math.Abs(area);
        }

        private Orientation GetOrientation(int v)
        {
            int v1 = (v > 0) ? v - 1 : Indices.Count - 1;
            int v2 = v;
            int v3 = (v < Indices.Count - 1) ? v + 1 : 0;

            return GetDeterminate(Vertices[Indices[v1]], Vertices[Indices[v2]], Vertices[Indices[v3]]) > 0 ? Orientation.CCW: Orientation.CW;
        }

        // Get the orientation of the input polygon.
        private Orientation GetPolygonOrientation()
        {
            // Find top-left vertex.
            Vec2D tl = Vertices[0];
            int tlIndex = 0;
            for (int i=1; i<Vertices.Count; i++)
            {
                if (Vertices[i].x < tl.x || (Vertices[i].x == tl.x && Vertices[i].y < tl.y))
                {
                    tl = Vertices[i];
                    tlIndex = i;
                }
            }

            //return GetOrientation(tlIndex);
            int v = tlIndex;
            int v1 = (v > 0) ? v - 1 : Vertices.Count - 1;
            int v2 = v;
            int v3 = (v < Vertices.Count - 1) ? v + 1 : 0;

            return GetDeterminate(Vertices[v1], Vertices[v2], Vertices[v3]) > 0 ? Orientation.CCW : Orientation.CW;

        }

        // Figure out best plane to project onto for the 3D to 2D conversion.
        private Plane GetBestPlane()
        {
            if (Points.Count > 2)
            {
                SVector v1 = (Points[1] - Points[0]).Normalise();
                SVector v2 = (Points[2] - Points[0]).Normalise();

                SVector normal = v1.VectorProduct(v2).Normalise();

                // Compare the normal against each of X/Y/Z.
                float dpX = Math.Abs(new SVector(1, 0, 0, 0).DotProduct(normal));
                float dpY = Math.Abs(new SVector(0, 1, 0, 0).DotProduct(normal));
                float dpZ = Math.Abs(new SVector(0, 0, 1, 0).DotProduct(normal));

                if (dpX > dpY)
                {
//                    return (dpX > dpZ) ? Plane.YZ : Plane.XY;
                    return (dpY > dpZ) ? Plane.XY : Plane.XZ;
                }
                else
                {
                    return (dpY > dpZ) ? Plane.XZ : Plane.XY;
                    //return (dpX > dpZ) ? Plane.XY : Plane.XZ;
                }
            }

            throw new ArgumentException("Need at least three vertices.");
        }


        public Triangulator(List<SPoint> polygon)
        {
            Points = polygon;

            if (polygon.Count < 3) throw new ArgumentException("Need at least three vertices.");
        }

        public bool Triangulate(in SVector normal)
        {
            Plane plane;

            if (Math.Abs(normal.I) > Math.Abs(normal.J))
            {
                plane = Math.Abs(normal.I) > Math.Abs(normal.K) ? Plane.YZ : Plane.XY;
            }
            else
            {
                plane = Math.Abs(normal.J) > Math.Abs(normal.K) ? Plane.XZ : Plane.XY;
            }

            return Triangulate(plane);
        }

        public bool Triangulate()
        {
            Plane plane = GetBestPlane();

            return Triangulate(plane);
        }

        private bool Triangulate(Plane plane)
        {
            //Debug.Assert(Points.Count < 20);

            //Plane plane = GetBestPlane();

            Vertices.Clear();

            switch (plane)
            {
                case Plane.XY:
                    foreach (var p in Points)
                    {
                        Vertices.Add(new Vec2D(p.X, p.Y));
                    }
                    break;
                case Plane.XZ:
                    foreach (var p in Points)
                    {
                        Vertices.Add(new Vec2D(p.X, p.Z));
                    }
                    break;
                case Plane.YZ:
                    foreach (var p in Points)
                    {
                        Vertices.Add(new Vec2D(p.Y, p.Z));
                    }
                    break;
            }

            // Create initial list of indices into points.
            Indices = Enumerable.Range(0, Points.Count).ToList();

            // Get the winding order.
            Orientation orientation = GetPolygonOrientation();

            int numVertices = Indices.Count;

            // XXX Worth doing?
            // How many times we loop without finding ears before we quit.
            //int numAttempts = numVertices;

            while (Indices.Count > 3)
            {
                // Search for an ear.
                for (int i = 0; i<Indices.Count; i++)
                {
                    Orientation o = GetOrientation(i);

                    if (o == orientation)
                    {
                        bool pointInTriangle = false;

                        // Found an ear, so check if any of its vertices are inside the polygon.
                        for (int v = 0; v<Indices.Count; v++)
                        {
                            if (v == i) continue;

                            if (IsPointInTriangleAt2(i, v))
                            {
                                pointInTriangle = true;
                                break;
                            }
                        }

                        if (!pointInTriangle)
                        {
                            // Snip out this ear.
                            AddOutputTriangle(i);

                            Indices.RemoveAt(i);
                        }
                    }
                }

                //Debug.Assert(numVertices != Indices.Count);

                if (numVertices == Indices.Count)
                {
                    StringBuilder sb = new StringBuilder();

                    for (int i = 0; i<Points.Count; i++)
                    {
                        sb.Append("v ");
                        sb.Append(Points[i].X.ToString() + " " + Points[i].Y.ToString() + " " + Points[i].Z.ToString() + "\n");
                    }

                    string s = sb.ToString();

                    sb.Clear();

                    for (int i = 0; i<Indices.Count; i++)
                    {
                        sb.Append("v ");
                        sb.Append(Points[Indices[i]].X.ToString() + " " + Points[Indices[i]].Y.ToString() + " " + Points[Indices[i]].Z.ToString() + "\n");
                    }

                    s = sb.ToString();


                    return true; // false;
                }

                numVertices = Indices.Count;
            }

            if (Indices.Count == 3)
            {
                AddOutputTriangle(1);
            }

            return true;
        }


    }
}
