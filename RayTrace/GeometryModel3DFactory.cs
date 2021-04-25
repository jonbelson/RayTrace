using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace RayTrace
{
    static class GeometryModel3DFactory
    {

        private static double[] cubePositions = new double[] {
                                        -0.5, -0.5, -0.5,   0.5, -0.5, -0.5,
                                        -0.5, 0.5, -0.5,   0.5, 0.5, -0.5,

                                        -0.5, -0.5, 0.5,   0.5, -0.5, 0.5,
                                        -0.5, 0.5, 0.5,   0.5, 0.5, 0.5
                                    };

        private static int[] triangleIndices = new[] {
                                        0,2,1, 1,2,3,   // Back
                                        0,4,2, 2,4,6,   // Left
                                        0,1,4, 1,5,4,   // Bottom
                                        1,7,5, 1,3,7,   // Right
                                        4,5,6, 7,6,5,   // Front
                                        2,6,3, 3,6,7    // Top
                                    };

        private static int[] pointNormals = new[] {
                                        0,1,0, 0,1,0, 1,0,0, 1,0,0,
                                        0,1,0, 0,1,0, 1,0,0, 1,0,0
                                    };


        public static MeshGeometry3D MakeCube()
        {
            MeshGeometry3D mesh = new MeshGeometry3D();

            Debug.Assert(cubePositions.Length / 3 != 0);
            Debug.Assert(pointNormals.Length / 3 != 0);

            for (int i = 0; i < cubePositions.Length; i += 3)
            {
                Point3D p = new Point3D(cubePositions[i], cubePositions[i + 1], cubePositions[i + 2]);
                mesh.Positions.Add(p);
            }
            for (int i = 0; i < triangleIndices.Length; i++)
            {
                mesh.TriangleIndices.Add(triangleIndices[i]);
            }

            for (int i = 0; i < pointNormals.Length / 3; i += 3)
            {
                Vector3D v = new(pointNormals[i], pointNormals[i + 1], pointNormals[i + 2]);
                mesh.Normals.Add(v);
            }

            return mesh;
        }

        public static MeshGeometry3D MakePlane(int slices = 40)
        {
            MeshGeometry3D mesh = new();

            double step = 1.0 / slices;

            int numSteps = slices + 1;

            int pointIndex = 0;

            for (int z = 0; z < numSteps; z++)
            {
                double zval = -0.5 + z * step;

                int sliceIndex = pointIndex;

                for (int x = 0; x < numSteps; x++)
                {
                    double xval = -0.5 + (x * step);

                    mesh.Positions.Add(new Point3D(xval, 0.0, zval));

                    // Can't start adding points until the second row.
                    if (z > 0)
                    {
                        mesh.TriangleIndices.Add(sliceIndex);
                        mesh.TriangleIndices.Add(sliceIndex - numSteps + 1);
                        mesh.TriangleIndices.Add(sliceIndex - numSteps);

                        mesh.TriangleIndices.Add(sliceIndex);
                        mesh.TriangleIndices.Add(sliceIndex + 1);
                        mesh.TriangleIndices.Add(sliceIndex + 1 - numSteps);

                    }

                    sliceIndex++;
                }

                pointIndex += numSteps;

            }



            //mesh.Positions.Add(new Point3D(-0.5, 0.0, -0.5));
            //mesh.Positions.Add(new Point3D(0.5, 0.0, -0.5));
            //mesh.Positions.Add(new Point3D(0.5, 0.0, 0.5));
            //mesh.Positions.Add(new Point3D(-0.5, 0.0, 0.5));

            //mesh.TriangleIndices.Add(0);
            //mesh.TriangleIndices.Add(2);
            //mesh.TriangleIndices.Add(3);
            //mesh.TriangleIndices.Add(0);
            //mesh.TriangleIndices.Add(1);
            //mesh.TriangleIndices.Add(2);

            return mesh;
        }

        public static MeshGeometry3D MakeSphere(int slices = 16)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();

            int samplesPerSlice = 2 * (slices - 1) + 2;

            // First point
            mesh.Positions.Add(new Point3D(0.0, 1.0, 0.0));

            int pointIndex = 1;

            for (int slice = 1; slice < slices; slice++)
            {
                double ya = ((double)slice / slices) * Math.PI;
                double y = Math.Cos(ya);

                // How much to scale each slice to make it spherical.
                double sliceScale = Math.Sin(ya);

                int sliceIndex = pointIndex;

                for (int sample = 0; sample < samplesPerSlice; sample++)
                {
                    double xa = Math.PI * 2.0 * sample / samplesPerSlice;

                    double s = Math.Sin(xa);
                    double c = Math.Cos(xa);

                    mesh.Positions.Add(new Point3D(s * sliceScale, y, c * sliceScale));

                    if (sliceIndex > pointIndex && slice > 1)
                    {
                        // Triangle 1.
                        mesh.TriangleIndices.Add(sliceIndex);
                        mesh.TriangleIndices.Add(sliceIndex - 1 - samplesPerSlice);
                        mesh.TriangleIndices.Add(sliceIndex - 1);
                        // Triangle 2.
                        mesh.TriangleIndices.Add(sliceIndex);
                        mesh.TriangleIndices.Add(sliceIndex - samplesPerSlice);
                        mesh.TriangleIndices.Add(sliceIndex - samplesPerSlice - 1);
                    }
                    sliceIndex++;
                }

                // Triangle 1.
                mesh.TriangleIndices.Add(sliceIndex);
                mesh.TriangleIndices.Add(sliceIndex - 1 - samplesPerSlice);
                mesh.TriangleIndices.Add(sliceIndex - 1);
                // Triangle 2.
                mesh.TriangleIndices.Add(sliceIndex);
                mesh.TriangleIndices.Add(sliceIndex - samplesPerSlice);
                mesh.TriangleIndices.Add(sliceIndex - samplesPerSlice - 1);


                pointIndex = sliceIndex;
            }


            return mesh;
        }

        public static GeometryModel3D MakeSphere(Sphere sphere)
        {
            MeshGeometry3D mesh = MakeSphere();

            GeometryModel3D model = new();
            model.Geometry = mesh;

            model.Material = new DiffuseMaterial(new SolidColorBrush(System.Windows.Media.Colors.DarkGray));

            var scale = new ScaleTransform3D(sphere.Radius, sphere.Radius, sphere.Radius);
            var translate = new TranslateTransform3D(sphere.Pos.X, sphere.Pos.Y, sphere.Pos.Z);

            Transform3DGroup transformGroup = new();

            transformGroup.Children.Add(scale);
            transformGroup.Children.Add(translate);

            model.Transform = transformGroup;

            return model;
        }

        public static GeometryModel3D MakePlane(Plane plane)
        {
            GeometryModel3D model = new();

            MeshGeometry3D mesh = MakePlane();
            model.Geometry = mesh;

//            model.Material = new DiffuseMaterial(new SolidColorBrush(System.Windows.Media.Colors.Purple));
            model.Material = new DiffuseMaterial(new SolidColorBrush(System.Windows.Media.Colors.DarkGray));

            var scale = new ScaleTransform3D(1000, 1, 1000);
            var translate = new TranslateTransform3D(plane.Pos.X, plane.Pos.Y, plane.Pos.Z);

            Transform3DGroup transformGroup = new();

            transformGroup.Children.Add(scale);
            transformGroup.Children.Add(translate);

            model.Transform = transformGroup;

            return model;
        }

        public static GeometryModel3D MakeModel(Model model)
        {
            GeometryModel3D geometryModel = new();

            MeshGeometry3D mesh = new();

            Func<Point, Point3D> PointToPoint3D = (p) => new Point3D(p.X, p.Y, p.Z);
            Func<Vector, Vector3D> VectorToVector3D = (v) => new Vector3D(v.I, v.J, v.K);

            ModelData modelData =  model.GetModelData();

            //int numTriangles = modelData.triangles.Length;

            int numPoints = modelData.points.Length;

            // Add all the vertices.
            Point3DCollection pointCollection = new(numPoints);
            foreach (Point p in modelData.points)
            {
                pointCollection.Add(PointToPoint3D(p));
            }
            mesh.Positions = pointCollection;

            /*
                        // Add all the vertex normal vectors.
                        Vector3DCollection vectorCollection = new(modelData.normals.Length);
                        foreach (Vector v in modelData.normals)
                        {
                            vectorCollection.Add(VectorToVector3D(v));
                        }
                        mesh.Normals = vectorCollection;
            */
            /*
                        // Add triangle indices.
                        foreach (ModelData.Triangle triangle in modelData.triangles)
                        {
                            mesh.TriangleIndices.Add(triangle.v0);
                            mesh.TriangleIndices.Add(triangle.v1);
                            mesh.TriangleIndices.Add(triangle.v2);
                        }
            */

            Vector[] pointNormals = new Vector[numPoints];

            // Add vector normals.
            int numTriangles = modelData.triangles.Length;
            for (int i=0; i<numTriangles; i++)
            {
                ModelData.Triangle triangle = modelData.triangles[i];

                int v0 = triangle.v0;
                int v1 = triangle.v1;
                int v2 = triangle.v2;

                mesh.TriangleIndices.Add(v2);
                mesh.TriangleIndices.Add(v1);
                mesh.TriangleIndices.Add(v0);

                pointNormals[v0] = (modelData.GetTriangleNormal0(i));
                pointNormals[v1] = (modelData.GetTriangleNormal1(i));
                pointNormals[v2] = (modelData.GetTriangleNormal2(i));

/*
                mesh.TriangleIndices.Add(v0);
                mesh.TriangleIndices.Add(v1);
                mesh.TriangleIndices.Add(v2);

                pointNormals[v0] = (modelData.GetTriangleNormal0(i));
                pointNormals[v1] = (modelData.GetTriangleNormal1(i));
                pointNormals[v2] = (modelData.GetTriangleNormal2(i));
*/
            }

            foreach (Vector v in pointNormals)
            {
                Vector v2 = v;//.Negate();

                //v2.I = -v2.I;
                //v2.J = -v2.J;

                Vector3D v3d = VectorToVector3D(v2);
                //v3d.Z = -v3d.Z;

                //v3d = -v3d;

                mesh.Normals.Add(v3d);
            }

            geometryModel.Geometry = mesh;

            geometryModel.Material = new DiffuseMaterial(new SolidColorBrush(System.Windows.Media.Colors.DarkGray));
            //geometryModel.BackMaterial = new DiffuseMaterial(new SolidColorBrush(System.Windows.Media.Colors.Blue));

            //var scale = new ScaleTransform3D(1, 1, 1);
            //var translate = new TranslateTransform3D(plane.Pos.X, plane.Pos.Y, plane.Pos.Z);

            var rotate = new AxisAngleRotation3D(new Vector3D(1.0, 0.0, 0.0), /*(float)Math.PI / 2*/ 90);
            var scale = new ScaleTransform3D(2.0, 2.0, 2.0);
            var translate = new TranslateTransform3D(0.0, 0.0, 10.0);

            Transform3DGroup transformGroup = new();

            //transformGroup.Children.Add(scale);
            //transformGroup.Children.Add(translate);

            geometryModel.Transform = transformGroup;

            return geometryModel;
        }
    }
}

