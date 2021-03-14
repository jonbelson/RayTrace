using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
//using RayTrace.ModelData.Triangle;

namespace RayTrace
{
    static public class ModelFactory
    {
        static ModelData.Triangle MakeTriangle(int v0, int v1, int v2)
        {
            ModelData.Triangle triangle = new ModelData.Triangle();
            triangle.v0 = v0;
            triangle.v1 = v1;
            triangle.v2 = v2;

            //triangle.v0 = v2;
            //triangle.v1 = v1;
            //triangle.v2 = v0;

            return triangle;
        }

        static public Model CornellBox(double width=20.0, double height=20.0, double depth=20.0)
        {
            ModelData modelData = new ModelData();

            List<Point> points = new List<Point>();
            List<Vector> normals = new List<Vector>();
            List<ModelData.Triangle> triangles = new List<ModelData.Triangle>();


            points.Add(new Point(0, 0, 0)); // Dummy point.

            points.Add(new Point(width/2, height, depth));
            points.Add(new Point(width / 2, height, 0));
            points.Add(new Point(width / 2, 0, depth));
            points.Add(new Point(width / 2, 0, 0));
            points.Add(new Point(-width / 2, height, depth));
            points.Add(new Point(-width / 2, height, 0));
            points.Add(new Point(-width / 2, 0, depth));
            points.Add(new Point(-width / 2, 0, 0));

            // Right
            triangles.Add(MakeTriangle(2, 1, 3));
            triangles.Add(MakeTriangle(2, 3, 4));

            // Left
            triangles.Add(MakeTriangle(5, 6, 8));
            triangles.Add(MakeTriangle(5, 8, 7));

            // Top.
            triangles.Add(MakeTriangle(1, 2, 6));
            triangles.Add(MakeTriangle(1, 6, 5));

            // Bottom
            triangles.Add(MakeTriangle(4, 3, 7));
            triangles.Add(MakeTriangle(4, 7, 8));

            // Back
            triangles.Add(MakeTriangle(3, 1, 5));
            triangles.Add(MakeTriangle(3, 5, 7));

            // Front
            //triangles.Add(MakeTriangle(2, 4, 8));
            //triangles.Add(MakeTriangle(2, 8, 6));

            foreach (ModelData.Triangle triangle in triangles)
            {
                Vector v1 = points[triangle.v1] - points[triangle.v0];
                Vector v2 = points[triangle.v2] - points[triangle.v0];
                Vector normal = v1.VectorProduct(v2);
//                Vector normal = v2.VectorProduct(v1);
                normals.Add(normal.Normalise());
                triangle.n = normals.Count - 1;

                triangle.n0 = -1;
            }

            modelData.points = points.ToArray();
            modelData.normals = normals.ToArray();
            modelData.triangles = triangles.ToArray();

            TexturePerTriangle texture = new TexturePerTriangle();
            texture.AddEntry(0, Palette.Red);
            texture.AddEntry(1, Palette.Red);
            texture.AddEntry(2, Palette.Green);
            texture.AddEntry(3, Palette.Green);

/////////            modelData.texture = texture;

            return new Model(new Point(0, 0, 0), modelData);
        }


        static public Model Cube()
        {
            ModelData modelData = new ModelData();

            List<Point> points = new List<Point>();
            List<Vector> normals = new List<Vector>();
            List<ModelData.Triangle> triangles = new List<ModelData.Triangle>();

            points.Add(new Point(0, 0, 0)); // Dummy point.

            points.Add(new Point(1, 1, 1));
            points.Add(new Point(1, 1, -1));
            points.Add(new Point(1, -1, 1));
            points.Add(new Point(1, -1, -1));
            points.Add(new Point(-1, 1, 1));
            points.Add(new Point(-1, 1, -1));
            points.Add(new Point(-1, -1, 1));
            points.Add(new Point(-1, -1, -1));

            triangles.Add(MakeTriangle(2, 1, 3));
            triangles.Add(MakeTriangle(2, 3, 4));

            triangles.Add(MakeTriangle(5, 6, 8));
            triangles.Add(MakeTriangle(5, 8, 7));

            triangles.Add(MakeTriangle(1, 2, 6));
            triangles.Add(MakeTriangle(1, 6, 5));

            triangles.Add(MakeTriangle(4, 3, 7));
            triangles.Add(MakeTriangle(4, 7, 8));

            triangles.Add(MakeTriangle(3, 1, 5));
            triangles.Add(MakeTriangle(3, 5, 7));

            triangles.Add(MakeTriangle(2, 4, 8));
            triangles.Add(MakeTriangle(2, 8, 6));

            foreach (ModelData.Triangle triangle in triangles)
            {
                Vector v1 = points[triangle.v1] - points[triangle.v0];
                Vector v2 = points[triangle.v2] - points[triangle.v0];
                Vector normal = v1.VectorProduct(v2);
                normals.Add(normal.Normalise());
                triangle.n = normals.Count - 1;
            }

            modelData.points = points.ToArray();
            modelData.normals = normals.ToArray();
            modelData.triangles = triangles.ToArray();

            return new Model(new Point(0, 0, 0), modelData);
        }


        static public Model Octahedron()
        {
            ModelData modelData = new ModelData();

            List<Point> points = new List<Point>();
            List<Vector> normals = new List<Vector>();
            List<ModelData.Triangle> triangles = new List<ModelData.Triangle>();

            points.Add(new Point(0, 0, 0)); // Dummy point.

            points.Add(new Point(1, 0, 0));
            points.Add(new Point(-1, 0, 0));
            points.Add(new Point(0, 1, 0));
            points.Add(new Point(0, -1, 0));
            points.Add(new Point(0, 0, 1));
            points.Add(new Point(0, 0, -1));

            triangles.Add(MakeTriangle(1, 3, 5));
            triangles.Add(MakeTriangle(3, 1, 6));
            triangles.Add(MakeTriangle(4, 1, 5));
            triangles.Add(MakeTriangle(1, 4, 6));
            triangles.Add(MakeTriangle(3, 2, 5));
            triangles.Add(MakeTriangle(2, 3, 6));
            triangles.Add(MakeTriangle(2, 4, 5));
            triangles.Add(MakeTriangle(4, 2, 6));

            foreach (ModelData.Triangle triangle in triangles)
            {
                Vector v1 = points[triangle.v1] - points[triangle.v0];
                Vector v2 = points[triangle.v2] - points[triangle.v0];
                Vector normal = v1.VectorProduct(v2);
                normals.Add(normal.Normalise());
                triangle.n = normals.Count - 1;
            }

            modelData.points = points.ToArray();
            modelData.normals = normals.ToArray();
            modelData.triangles = triangles.ToArray();

            return new Model(new Point(0, 0, 0), modelData);
        }


        static public Model Tetahedron()
        {
            ModelData modelData = new ModelData();

            List<Point> points = new List<Point>();
            List<Vector> normals = new List<Vector>();
            List<ModelData.Triangle> triangles = new List<ModelData.Triangle>();

            points.Add(new Point(0, 0, 0)); // Dummy point.

            points.Add(new Point(1, 1, 1));
            points.Add(new Point(1, -1, -1));
            points.Add(new Point(-1, 1, -1));
            points.Add(new Point(-1, -1, 1));

            //triangles.Add(MakeTriangle(4, 3, 2));
            //triangles.Add(MakeTriangle(3, 4, 1));
            //triangles.Add(MakeTriangle(2, 1, 4));
            //triangles.Add(MakeTriangle(1, 2, 3));

            triangles.Add(MakeTriangle(2, 3, 4));
            triangles.Add(MakeTriangle(1, 4, 3));
            triangles.Add(MakeTriangle(4, 1, 2));
            triangles.Add(MakeTriangle(3, 2, 1));

            foreach (ModelData.Triangle triangle in triangles)
            {
                Vector v1 = points[triangle.v1] - points[triangle.v0];
                Vector v2 = points[triangle.v2] - points[triangle.v0];
                Vector normal = v1.VectorProduct(v2);
                normals.Add(normal.Normalise());
                triangle.n = normals.Count - 1;
            }

            modelData.points = points.ToArray();
            modelData.normals = normals.ToArray();
            modelData.triangles = triangles.ToArray();

            return new Model(new Point(0, 0, 0), modelData);
        }

        /*
        static public Model Isosahedron()
        {
            ModelData modelData = new ModelData();

            List<Point> points = new List<Point>();
            List<Vector> normals = new List<Vector>();
            List<ModelData.Triangle> triangles = new List<ModelData.Triangle>();

            points.Add(new Point(0, 0, 0)); // Dummy point.

            double phi = (1.0 + Math.Sqrt(5.0)) / 2.0; ;

            points.Add(new Point(1, 1, 1));
        }
        */

    }
}
