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
        public static System.Windows.Media.Color DefaultColour { get; set; } = System.Windows.Media.Colors.LightGray;

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

        public static MeshGeometry3D MakeDisc(Vector normal, int slices = 40)
        {
            // Make two orthogonal Vectors on the plane.
            Vector v = new Vector(normal.I, normal.K, normal.J);
            if ((v - normal).Magnitude() < 0.01)
            {
                v = new Vector(normal.K, normal.J, normal.I);
            }
            Vector vec1 = normal.VectorProduct(v);
            Vector vec2 = normal.VectorProduct(vec1);

            vec1 = vec1.Normalise();
            vec2 = vec2.Normalise();

            MeshGeometry3D mesh = new();

            // Centre point
            mesh.Positions.Add(new Point3D(0.0, 0.0, 0.0));

            double step = 1.0 / slices;
            int numSteps = slices + 1;

            double angle = 0.0;
            for (int i = 0; i<numSteps; i++)
            {
                angle = i*step*2.0*Math.PI;

                float x = (float) Math.Cos(angle);
                float y = (float) Math.Sin(angle);

                Vector x2 = new(x * vec1.I, x * vec1.J, x * vec1.K);
                Vector y2 = new(y * vec2.I, y * vec2.J, y * vec2.K);
                Vector sum = x2 + y2;
                mesh.Positions.Add(new Point3D(sum.I, sum.J, sum.K));
            }

            int numPoints = mesh.Positions.Count;
            for (int i=1; i<=numPoints; i++)
            {
                mesh.TriangleIndices.Add(0);
                mesh.TriangleIndices.Add(i - 1);
                mesh.TriangleIndices.Add(i%numPoints);
            }

            return mesh;
        }

        public static GeometryModel3D MakeDisc(Disc disc)
        {
            GeometryModel3D model = new();

            MeshGeometry3D mesh = MakeDisc(disc.Normal);

            model.Geometry = mesh;

            model.Material = new DiffuseMaterial(new SolidColorBrush(DefaultColour));

            Vector up = new Vector(0, 0, -1);
            Vector axis = up.VectorProduct(disc.Normal);

            double cosangle = up.DotProduct(disc.Normal);
            double angle = Math.Acos(cosangle);

            var axisRotation = new AxisAngleRotation3D(new Vector3D(axis.I, axis.J, axis.K), angle / (2 * Math.PI) * 360);
            var rotate = new RotateTransform3D(axisRotation);


            //            var scale = new ScaleTransform3D(1000, 1, 1000);
            var scale = new ScaleTransform3D(disc.Scale, disc.Scale, disc.Scale);
            var translate = new TranslateTransform3D(disc.Pos.X, disc.Pos.Y, disc.Pos.Z);

            Transform3DGroup transformGroup = new();

            // var transform = new MatrixTransform3D(lookAt);

            transformGroup.Children.Add(scale);
            //transformGroup.Children.Add(rotate);
            transformGroup.Children.Add(translate);

            model.Transform = transformGroup;

            //model.Material = new DiffuseMaterial(new SolidColorBrush(Colors.Red));
            //model.BackMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Green));

            return model;
        }

        public static MeshGeometry3D MakePlane(Vector normal, int slices = 40)
        {
            // Make two orthogonal Vectors on the plane.
            Vector v = new Vector(normal.I, normal.K, normal.J);
            if ((v - normal).Magnitude() < 0.01)
            {
                v = new Vector(normal.K, normal.J, normal.I);
            }
            Vector vec1 = normal.VectorProduct(v);
            Vector vec2 = normal.VectorProduct(vec1);

            vec1 = vec1.Normalise();
            vec2 = vec2.Normalise();

            MeshGeometry3D mesh = new();

            float step = 1.0f / slices;

            vec1 = vec1 * step;
            vec2 = vec2 * step;

            int numSteps = slices + 1;

            int pointIndex = 0;

            for (int z = 0; z < numSteps; z++)
            {
                float zval = -0.5f + z * step;
                Vector zpos = vec1*-0.5f + vec1 * z;

                int sliceIndex = pointIndex;

                for (int x = 0; x < numSteps; x++)
                {
                    float xval = -0.5f + (x * step);
                    Vector xpos = vec2 * -0.5f + vec2 * x;

                    Vector p = zpos + xpos;

//                    mesh.Positions.Add(new Point3D(xval, 0.0, zval));
                    mesh.Positions.Add(new Point3D(xval, zval, 0.0));
                    //                    mesh.Positions.Add(new Point3D(p.I, p.J, p.K));

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

            return mesh;
        }

        public static MeshGeometry3D MakeSphere(int slices = 16)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();

            int samplesPerSlice = 2 * (slices - 1) + 2;

            // First point
            mesh.Positions.Add(new Point3D(0.0, 1.0, 0.0));

            int pointIndex = 1;

            // For each slice, from top to bottom.
            for (int slice = 1; slice < slices; slice++)
            {
                // Angle to current slice elevation.
                double ya = ((double)slice / slices) * Math.PI;

                // How much to scale each slice to make it spherical.
                double sliceScale = Math.Sin(ya);
                double y = Math.Cos(ya);

                // Index of first point in this slice.
                int sliceStartIndex = pointIndex;


                for (int sample = 0; sample < samplesPerSlice; sample++)
                {
                    // Angles around this slice.
                    double sliceAngle = Math.PI * 2.0 * sample / samplesPerSlice;

                    double s = Math.Sin(sliceAngle);
                    double c = Math.Cos(sliceAngle);

                    mesh.Positions.Add(new Point3D(s * sliceScale, y, c * sliceScale));

                    // Start making triangles after the first slice
                    if (sample > 0)
                    {
                        if (slice > 1)
                        {
                            // Triangle 1.
                            mesh.TriangleIndices.Add(sliceStartIndex);
                            mesh.TriangleIndices.Add(sliceStartIndex - 1 - samplesPerSlice);
                            mesh.TriangleIndices.Add(sliceStartIndex - 1);
                            // Triangle 2.
                            mesh.TriangleIndices.Add(sliceStartIndex);
                            mesh.TriangleIndices.Add(sliceStartIndex - samplesPerSlice);
                            mesh.TriangleIndices.Add(sliceStartIndex - samplesPerSlice - 1);
                        }
                        else if (slice == 1)
                        {
                            mesh.TriangleIndices.Add(sliceStartIndex);
                            mesh.TriangleIndices.Add(0);
                            mesh.TriangleIndices.Add(sliceStartIndex - 1);
                        }
                    }
                    sliceStartIndex++;
                }

                // Stitch the start and end to complete the triangle strip.
                if (slice == 1)
                {
                    mesh.TriangleIndices.Add(pointIndex);
                    mesh.TriangleIndices.Add(0);
                    mesh.TriangleIndices.Add(pointIndex + samplesPerSlice - 1);
                }
                else if (slice == slices)
                {
                    mesh.Positions.Add(new Point3D(0.0, -1.0, 0.0));

                    int final = mesh.Positions.Count - 1;

                    mesh.TriangleIndices.Add(final);
                    mesh.TriangleIndices.Add(final - samplesPerSlice);
                    mesh.TriangleIndices.Add(final - 1);
                }
                else
                {
                    int first = pointIndex;

                    // Triangle 1.
                    mesh.TriangleIndices.Add(first);
                    mesh.TriangleIndices.Add(first - 1);
                    mesh.TriangleIndices.Add(first + samplesPerSlice - 1);
                    // Triangle 2.
                    mesh.TriangleIndices.Add(first);
                    mesh.TriangleIndices.Add(first - samplesPerSlice);
                    mesh.TriangleIndices.Add(first - 1);
                }

                pointIndex = sliceStartIndex;

            }


            return mesh;
        }

        public static GeometryModel3D MakeSphere(Sphere sphere)
        {
            MeshGeometry3D mesh = MakeSphere();

            GeometryModel3D model = new();
            model.Geometry = mesh;

            model.Material = new DiffuseMaterial(new SolidColorBrush(DefaultColour));

            var scale = new ScaleTransform3D(sphere.Scale, sphere.Scale, sphere.Scale);
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

            MeshGeometry3D mesh = MakePlane(plane.Normal);
            model.Geometry = mesh;

//            model.Material = new DiffuseMaterial(new SolidColorBrush(System.Windows.Media.Colors.Purple));
            model.Material = new DiffuseMaterial(new SolidColorBrush(DefaultColour));

            if (false)
            {
                /////////////////////////////////////////////////////////////
                // Rotate to match specified normal vector.
                Matrix3D lookAt = new Matrix3D();
                lookAt.SetIdentity();

                //Point cameraTarget = to;
                //Point cameraPosition = from;
                //Vector zaxis = cameraTarget - cameraPosition;
                //            Vector zaxis = cameraPosition - cameraTarget;

                Vector3D zaxis = new(plane.Normal.I, plane.Normal.J, plane.Normal.K);
                //            Vector3D zaxis = new(0, 1, 0);
                zaxis.Normalize();

                //zaxis = zaxis.Normalise();
                //Vector cameraUpVector = new Vector(0, 1, 0);

                Vector3D cameraUpVector = new(0, 1, 0);

                if (Vector3D.DotProduct(zaxis, cameraUpVector) < 0.01)
                {

                }

                Vector3D xaxis = Vector3D.CrossProduct(cameraUpVector, zaxis);
                xaxis.Normalize();

                //                        Vector xaxis = cameraUpVector.VectorProduct(zaxis);
                //xaxis = zaxis.VectorProduct(cameraUpVector);
                //                        xaxis = xaxis.Normalise();

                Vector3D yaxis = Vector3D.CrossProduct(zaxis, xaxis);

                //                        Vector yaxis = zaxis.VectorProduct(xaxis);
                //yaxis = xaxis.VectorProduct(zaxis);

                Vector3D vecPosition = new(0, 0, 0);

                //                        Vector vecPosition = new Vector(cameraPosition.X, cameraPosition.Y, cameraPosition.Z);

                lookAt.M11 = xaxis.X;
                lookAt.M21 = xaxis.Y;
                lookAt.M31 = xaxis.Z;

                lookAt.M12 = yaxis.X;
                lookAt.M22 = yaxis.Y;
                lookAt.M32 = yaxis.Z;

                lookAt.M13 = zaxis.X;
                lookAt.M23 = zaxis.Y;
                lookAt.M33 = zaxis.Z;

                lookAt.OffsetX = Vector3D.DotProduct(xaxis, -vecPosition);
                lookAt.OffsetY = Vector3D.DotProduct(yaxis, -vecPosition);
                lookAt.OffsetZ = Vector3D.DotProduct(zaxis, -vecPosition);
            }

            Vector up = new Vector(0, 0, -1);
            Vector axis = up.VectorProduct(plane.Normal);

            double cosangle = up.DotProduct(plane.Normal);
            double angle = Math.Acos(cosangle);

            var axisRotation = new AxisAngleRotation3D(new Vector3D(axis.I, axis.J, axis.K), angle/(2*Math.PI)*360);
            var rotate = new RotateTransform3D(axisRotation);


//            var scale = new ScaleTransform3D(1000, 1, 1000);
            var scale = new ScaleTransform3D(100, 100, 1);
            var translate = new TranslateTransform3D(plane.Pos.X, plane.Pos.Y, plane.Pos.Z);

            Transform3DGroup transformGroup = new();

           // var transform = new MatrixTransform3D(lookAt);

            transformGroup.Children.Add(scale);
            transformGroup.Children.Add(rotate);
            transformGroup.Children.Add(translate);



            model.Transform = transformGroup;


            //model.Material = new DiffuseMaterial(new SolidColorBrush(Colors.Red));
            //model.BackMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Green));


            return model;
        }

        public static GeometryModel3D MakeModel(Model model)
        {
            GeometryModel3D geometryModel = new();

            MeshGeometry3D mesh = new();

            Func<SPoint, Point3D> PointToPoint3D = (p) => new Point3D(p.X, p.Y, p.Z);
            Func<SVector, Vector3D> VectorToVector3D = (v) => new Vector3D(v.I, v.J, v.K);

            ModelData modelData = model.GetModelData();

            //int numTriangles = modelData.triangles.Length;

            if (modelData.points != null)
            {
                int numPoints = modelData.points.Length;

                // Add all the vertices.
                Point3DCollection pointCollection = new(numPoints);
                foreach (SPoint p in modelData.points)
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

                SVector[] pointNormals = new SVector[numPoints];

                // Add vector normals.
                int numTriangles = modelData.triangles.Length;
                for (int i = 0; i < numTriangles; i++)
                {
                    ModelData.Triangle triangle = modelData.triangles[i];

                    int v0 = triangle.v0;
                    int v1 = triangle.v1;
                    int v2 = triangle.v2;

                    mesh.TriangleIndices.Add(v2);
                    mesh.TriangleIndices.Add(v1);
                    mesh.TriangleIndices.Add(v0);

                    if (modelData.HasVertexNormals)
                    {
                        pointNormals[v0] = (modelData.GetTriangleNormal0(i));
                        pointNormals[v1] = (modelData.GetTriangleNormal1(i));
                        pointNormals[v2] = (modelData.GetTriangleNormal2(i));
                    }
                    else
                    {
                        pointNormals[v0] = modelData.GetTriangleNormal(i);
                        pointNormals[v1] = modelData.GetTriangleNormal(i);
                        pointNormals[v2] = modelData.GetTriangleNormal(i);
                    }
                }

                foreach (SVector v in pointNormals)
                {
                    SVector v2 = v;//.Negate();

                    //v2.I = -v2.I;
                    //v2.J = -v2.J;

                    //v2 = new(v2.I, v2.J, -v2.K);
                    //v2 = new(-v2.I, -v2.J, -v2.K);

                    Vector3D v3d = VectorToVector3D(v2);

                    mesh.Normals.Add(v3d);
                }
            }
            geometryModel.Geometry = mesh;

            geometryModel.Material = new DiffuseMaterial(new SolidColorBrush(DefaultColour));
            //geometryModel.BackMaterial = new DiffuseMaterial(new SolidColorBrush(System.Windows.Media.Colors.Blue));

            //var scale = new ScaleTransform3D(1, 1, 1);
            //var translate = new TranslateTransform3D(plane.Pos.X, plane.Pos.Y, plane.Pos.Z);

            var axisRotationX = new AxisAngleRotation3D(new Vector3D(1, 0, 0), model.RotateX);
            var axisRotationY = new AxisAngleRotation3D(new Vector3D(0, 1, 0), model.RotateY);
            var axisRotationZ = new AxisAngleRotation3D(new Vector3D(0, 0, 1), model.RotateZ);
            var scale = new ScaleTransform3D(model.Scale, model.Scale, model.Scale);
            var translate = new TranslateTransform3D(model.Pos.X, model.Pos.Y, model.Pos.Z);

            Transform3DGroup transformGroup = new();

            transformGroup.Children.Add(scale);
            transformGroup.Children.Add(new RotateTransform3D(axisRotationX));
            transformGroup.Children.Add(new RotateTransform3D(axisRotationY));
            transformGroup.Children.Add(new RotateTransform3D(axisRotationZ));
            transformGroup.Children.Add(translate);

            geometryModel.Transform = transformGroup;

            return geometryModel;
        }
    }
}

