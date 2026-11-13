// Copyright (c) 2026 Jonathan Belson
// Licensed under the MIT License — use freely, keep this notice.
// SPDX-License-Identifier: MIT
// Full terms: see LICENSE in the project root.

﻿using System;
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

        static public Model CornellBox(Scene scene, float width =20, float height =20, float depth =20)
        {
            ModelData modelData = new ModelData();

            List<SPoint> points = new List<SPoint>();
            List<SVector> normals = new List<SVector>();
            List<ModelData.Triangle> triangles = new List<ModelData.Triangle>();


            points.Add(new SPoint(0, 0, 0, 1.0f)); // Dummy point.

            points.Add(new SPoint(width/2, height, depth, 1.0f));
            points.Add(new SPoint(width / 2, height, 0, 1.0f));
            points.Add(new SPoint(width / 2, 0, depth, 1.0f));
            points.Add(new SPoint(width / 2, 0, 0, 1.0f));
            points.Add(new SPoint(-width / 2, height, depth, 1.0f));
            points.Add(new SPoint(-width / 2, height, 0, 1.0f));
            points.Add(new SPoint(-width / 2, 0, depth, 1.0f));
            points.Add(new SPoint(-width / 2, 0, 0, 1.0f));

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
                SVector v1 = points[triangle.v1] - points[triangle.v0];
                SVector v2 = points[triangle.v2] - points[triangle.v0];
                SVector normal = v1.VectorProduct(v2);
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

            return new Model(scene, new Point(0, 0, 0), modelData);
        }

        static public Model Cube(Scene scene)
        {
            ModelData modelData = new ModelData();

            List<SPoint> points = new List<SPoint>();
            List<SVector> normals = new List<SVector>();
            List<ModelData.Triangle> triangles = new List<ModelData.Triangle>();

            points.Add(new SPoint(0, 0, 0, 1.0f)); // Dummy point.

            points.Add(new SPoint(1, 1, 1, 1.0f));
            points.Add(new SPoint(1, 1, -1, 1.0f));
            points.Add(new SPoint(1, -1, 1, 1.0f));
            points.Add(new SPoint(1, -1, -1, 1.0f));
            points.Add(new SPoint(-1, 1, 1, 1.0f));
            points.Add(new SPoint(-1, 1, -1, 1.0f));
            points.Add(new SPoint(-1, -1, 1, 1.0f));
            points.Add(new SPoint(-1, -1, -1, 1.0f));

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
                SVector v1 = points[triangle.v1] - points[triangle.v0];
                SVector v2 = points[triangle.v2] - points[triangle.v0];
                SVector normal = v1.VectorProduct(v2);
                normals.Add(normal.Normalise());
                triangle.n = normals.Count - 1;
            }

            modelData.points = points.ToArray();
            modelData.normals = normals.ToArray();
            modelData.triangles = triangles.ToArray();

            return new Model(scene, new Point(0, 0, 0), modelData);
        }


        static public Model Octahedron(Scene scene)
        {
            ModelData modelData = new ModelData();

            List<SPoint> points = new List<SPoint>();
            List<SVector> normals = new List<SVector>();
            List<ModelData.Triangle> triangles = new List<ModelData.Triangle>();

            points.Add(new SPoint(0, 0, 0, 1.0f)); // Dummy point.

            points.Add(new SPoint(1, 0, 0, 1.0f));
            points.Add(new SPoint(-1, 0, 0, 1.0f));
            points.Add(new SPoint(0, 1, 0, 1.0f));
            points.Add(new SPoint(0, -1, 0, 1.0f));
            points.Add(new SPoint(0, 0, 1, 1.0f));
            points.Add(new SPoint(0, 0, -1, 1.0f));

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
                SVector v1 = points[triangle.v1] - points[triangle.v0];
                SVector v2 = points[triangle.v2] - points[triangle.v0];
                SVector normal = v1.VectorProduct(v2);
                normals.Add(normal.Normalise());
                triangle.n = normals.Count - 1;
            }

            modelData.points = points.ToArray();
            modelData.normals = normals.ToArray();
            modelData.triangles = triangles.ToArray();

            return new Model(scene, new Point(0, 0, 0), modelData);
        }


        static public Model Tetahedron(Scene scene)
        {
            ModelData modelData = new ModelData();

            List<SPoint> points = new List<SPoint>();
            List<SVector> normals = new List<SVector>();
            List<ModelData.Triangle> triangles = new List<ModelData.Triangle>();

            points.Add(new SPoint(0, 0, 0, 1.0f)); // Dummy point.

            points.Add(new SPoint(1, 1, 1, 1.0f));
            points.Add(new SPoint(1, -1, -1, 1.0f));
            points.Add(new SPoint(-1, 1, -1, 1.0f));
            points.Add(new SPoint(-1, -1, 1, 1.0f));

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
                SVector v1 = points[triangle.v1] - points[triangle.v0];
                SVector v2 = points[triangle.v2] - points[triangle.v0];
                SVector normal = v1.VectorProduct(v2);
                normals.Add(normal.Normalise());
                triangle.n = normals.Count - 1;
            }

            modelData.points = points.ToArray();
            modelData.normals = normals.ToArray();
            modelData.triangles = triangles.ToArray();

            return new Model(scene, new Point(0, 0, 0), modelData);
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

        // 
        // resolution   number of sub-squares per squre of width or height.
        static public Model Rectangle(Scene scene, int width, int height, int resolution)
        {
            ModelData modelData = new();

            int widthPoints = width*resolution + 1;
            int heightPoints = height*resolution + 1;

            int totalPoints = widthPoints * heightPoints;

            List<SPoint> points = new();
            List<SVector> normals = new();
            List<ModelData.Texel> texels = new();
            List<ModelData.Triangle> triangles = new();

            points.Add(new SPoint(0, 0, 0, 1.0f)); // Dummy point.
            normals.Add(SVector.Null());
            texels.Add(new ModelData.Texel());

            float startY = -height/2.0f;
            float startX = -width/2.0f;
            float stepY = 1.0f/resolution;
            float stepX = 1.0f/resolution;

            float y = startY;
            for (int h=0; h<heightPoints; h++)
            {
                float x = startX;
                for (int w=0; w<widthPoints; w++)
                {
//                    points.Add(new SPoint(x, 0, y));
                    points.Add(new SPoint(x, y, 0, 1.0f));
                    normals.Add(new SVector(0, 0, -1, 0));

                    ModelData.Texel texel = new() { u = w/(widthPoints - 1.0f), v = h/(heightPoints - 1.0f) };
                    texels.Add(texel);

                    if (h > 0 && w > 0)
                    {
                        int numPoints = points.Count - 1;
                        int numTexels = texels.Count - 1;

                        ModelData.Triangle triangle = new();
                        triangle.v0 = numPoints - widthPoints - 1;
                        triangle.v1 = numPoints - widthPoints;
                        triangle.v2 = numPoints - 1;

                        triangle.n = normals.Count - 1;
                        triangle.n0 = normals.Count - 1;
                        triangle.n1 = normals.Count - 1;
                        triangle.n2 = normals.Count - 1;

                        triangle.t0 = numTexels - widthPoints- 1;
                        triangle.t1 = numTexels - widthPoints;
                        triangle.t2 = numTexels - 1;

                        triangles.Add(triangle);

                        triangle = new();
                        triangle.v0 = numPoints - 1;
                        triangle.v1 = numPoints - widthPoints;
                        triangle.v2 = numPoints;

                        triangle.n = normals.Count - 1;
                        triangle.n0 = normals.Count - 1;
                        triangle.n1 = normals.Count - 1;
                        triangle.n2 = normals.Count - 1;

                        triangle.t0 = numTexels - 1;
                        triangle.t1 = numTexels - widthPoints;
                        triangle.t2 = numTexels;

                        triangles.Add(triangle);
                    }

                    x += stepX;
                }

                y += stepY;
            }



            modelData.points = points.ToArray();
            modelData.normals = normals.ToArray();
            modelData.texels = texels.ToArray();
            modelData.triangles = triangles.ToArray();

            //modelData.SubGroups = new ModelData.SubGroup[0];

            modelData.HasVertexNormals = true;

            return new Model(scene, new Point(0, 0, 0), modelData);
        }

    }
}
