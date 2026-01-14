using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Xaml;

namespace RayTrace
{
    using SMatrix = System.Numerics.Matrix4x4;

    //
    // Column vector 4x4 Matrix stored in row major format.
    //
    //  0  1  2  3
    //  4  5  6  7
    //  8  9 10 11
    // 12 13 14 15
    //
    // M11 M12 M13 M14
    // M21 M22 M23 M24
    // M31 M32 M33 M44
    // M41 M42 M43 M44
    //
    // M14 M24 M34 - translation
    // M11 M22 M33 - scale
    //
    public class Matrix
    {
        private static readonly int Size = 4;
        private static readonly int NumElements = Size * Size;

        public float[] M { get; set; } = new float[NumElements];

        // M|row|column
        public float M11 { get { return M[0]; } set { M[0] = value; } }
        public float M12 { get { return M[1]; } set { M[1] = value; } }
        public float M13 { get { return M[2]; } set { M[2] = value; } }
        public float M14 { get { return M[3]; } set { M[3] = value; } }

        public float M21 { get { return M[4]; } set { M[4] = value; } }
        public float M22 { get { return M[5]; } set { M[5] = value; } }
        public float M23 { get { return M[6]; } set { M[6] = value; } }
        public float M24 { get { return M[7]; } set { M[7] = value; } }

        public float M31 { get { return M[8]; } set { M[8] = value; } }
        public float M32 { get { return M[9]; } set { M[9] = value; } }
        public float M33 { get { return M[10]; } set { M[10] = value; } }
        public float M34 { get { return M[11]; } set { M[11] = value; } }

        public float M41 { get { return M[12]; } set { M[12] = value; } }
        public float M42 { get { return M[13]; } set { M[13] = value; } }
        public float M43 { get { return M[14]; } set { M[14] = value; } }
        public float M44 { get { return M[15]; } set { M[15] = value; } }

        //
        // Constructor.
        //
        public Matrix()
        {
            SetIdentity();
        }

        //
        // Constructor.
        //
        public Matrix(Matrix other)
        {
            other.M.CopyTo(M, 0);
        }

        //
        // Constructor.
        //
        public Matrix(double[] values)
        {
            if (values.Length == 16)
            {
                values.CopyTo(M, 0);
            }
            else
            {
                SetIdentity();
            }
        }

        //
        // Constructor.
        //
        public Matrix(SVector xaxis, SVector yaxis, SVector zaxis)
        {
            SetIdentity();

            /*
            M11 = xaxis.I;
            M12 = xaxis.J;
            M13 = xaxis.K;

            M21 = yaxis.I;
            M22 = yaxis.J;
            M23 = yaxis.K;

            M31 = zaxis.I;
            M32 = zaxis.J;
            M33 = zaxis.K;
            */

            M11 = xaxis.I;
            M21 = xaxis.J;
            M31 = xaxis.K;

            M12 = yaxis.I;
            M22 = yaxis.J;
            M32 = yaxis.K;

            M13 = zaxis.I;
            M23 = zaxis.J;
            M33 = zaxis.K;
        }

        private static Matrix NewMatrix { get { return new Matrix(); } }

        public static SVector operator*(Matrix m, in SVector v)
        {
            SVector vv = new SVector
            (
                m.M11 * v.I + m.M12 * v.J + m.M13 * v.K + m.M14 * 1.0f,
                m.M21 * v.I + m.M22 * v.J + m.M23 * v.K + m.M24 * 1.0f,
                m.M31 * v.I + m.M32 * v.J + m.M33 * v.K + m.M34 * 1.0f,
                0f
            );

            return vv;
        }

        /*
        public static SPoint operator*(Matrix m, in SPoint p)
        {
            SPoint pp = new SPoint
            (
                m.M11 * p.X + m.M12 * p.Y + m.M13 * p.Z + m.M14 * 1.0f,
                m.M21 * p.X + m.M22 * p.Y + m.M23 * p.Z + m.M24 * 1.0f,
                m.M31 * p.X + m.M32 * p.Y + m.M33 * p.Z + m.M34 * 1.0f,
                1.0f
            );

            return pp;
        }
        */

        public static Vector operator*(Matrix m, Vector v)
        {
            Vector vv = new Vector
            {
                I = m.M11 * v.I + m.M12 * v.J + m.M13 * v.K + m.M14 * 1.0f,
                J = m.M21 * v.I + m.M22 * v.J + m.M23 * v.K + m.M24 * 1.0f,
                K = m.M31 * v.I + m.M32 * v.J + m.M33 * v.K + m.M34 * 1.0f
            };

            return vv;
        }

        public static Point operator*(Matrix m, Point p)
        {
            Point pp = new Point
            {
                X = m.M11 * p.X + m.M12 * p.Y + m.M13 * p.Z + m.M14 * 1.0f,
                Y = m.M21 * p.X + m.M22 * p.Y + m.M23 * p.Z + m.M24 * 1.0f,
                Z = m.M31 * p.X + m.M32 * p.Y + m.M33 * p.Z + m.M34 * 1.0f
            };

            return pp;
        }

        // Multiply two matrices.
        public static Matrix operator*(Matrix m1, Matrix m2)
        {
            Matrix mm = new Matrix();

            for (int i = 0; i < Size; i++)
            {
                for (int j = 0; j < Size; j++)
                {
                    float sum = 0.0f;
                    for (int k = 0; k < Size; k++)
                    {
                        sum += m2.M[k * Size + i] * m1.M[j * Size + k];
                    }
                    mm.M[j * Size + i] = sum;
                }
            }

            return mm;
        }

        public static Matrix operator*(Matrix m, float factor)
        {
            Matrix mm = new Matrix();

            for (int i=0; i<Size*Size; i++)
            {
                mm.M[i] = m.M[i] * factor;
            }

            return mm;
        }

        // Set all values to 0. Also known as a zero matrix.
        public void SetNull()
        {
            for (int i=0; i<NumElements; i++)
            {
                M[i] = 0.0f;
            }
        }

        public void Transpose()
        {
            Matrix m2 = new Matrix();

            for (int r=0; r<Size; r++)
            {
                for (int c = 0; c < Size; c++)
                {
                    m2.M[r * Size + c] = M[c * Size + r];
                }
            }

            //M = m2.M;
            m2.M.CopyTo(M, 0);
        }

        public Matrix GetTranspose()
        {
            Matrix m2 = new(this);
            m2.Transpose();
            return m2;
        }

        public static Matrix GetIdentity()
        {
            Matrix m = new Matrix();
            m.SetIdentity();
            return m;
        }

        public static Matrix GetTranslation(float x, float y, float z)
        {
            Matrix m = new Matrix();
            m.SetTranslation(x, y, z);
            return m;
        }

        // Utility function to compute the determinant of the 3x3 matrix in the top left of the Matrix.
        private float GetDeterminant33()
        {
            float a11 = M11 * ((M22 * M33) - (M32 * M23));
            float a12 = M12 * ((M21 * M33) - (M31 * M23));
            float a13 = M13 * ((M21 * M32) - (M31 * M22));

            return a11 - a12 + a13;
        }

        // Create 3x3 matrix in top left of Matrix by removing top row and column 'column'.
        // 'Expansion by top row'.
        // Return its determinant.
        private float GetCofactor(int skipRow, int skipColumn)
        {
            Matrix m = new Matrix();

            int src;
            int dest;

            int srcRow = 0;
            int destRow = 0;

            for (int row = 0; row < Size; row++)
            {
                src = srcRow * Size;
                dest = destRow * Size;

                if (row != skipRow)
                {
                    for (int col = 0; col < Size; col++)
                    {
                        if (col != skipColumn)
                        {
                            m.M[dest] = M[src];
                            dest++;
                        }

                        src++;
                    }

                    destRow++;
                }

                srcRow++;
            }

            //Debug.WriteLine(m.ToString());

            return m.GetDeterminant33();
        }

        // Calculate determinant of Matrix.
        // Based on method shown in Mathematical Techniques (Jordan, Smith).
        public float GetDeterminant()
        {
            float a11 = M11 * GetCofactor(0, 0);
            float a12 = M12 * GetCofactor(0, 1);
            float a13 = M13 * GetCofactor(0, 2);
            float a14 = M14 * GetCofactor(0, 3);

            return a11 - a12 + a13 - a14;
        }

        public Matrix GetInverse()
        {
            float determinant = GetDeterminant();

            // If determinant is zero, no inverse exists.
            if (determinant == 0.0) return GetIdentity();

            // Create adjoint matrix (a matrix of cofactors).
            Matrix adjoint = new Matrix();
            int index = 0;
            for (int skipRow = 0; skipRow < Size; skipRow++)
            {
                for (int skipColumn = 0; skipColumn < Size; skipColumn++)
                {
                    adjoint.M[index] = GetCofactor(skipRow, skipColumn);
                    //adjoint.M[index] = GetCofactor(skipColumn, skipRow);

                    if ((skipRow + skipColumn)%2 == 1)
                    {
                        adjoint.M[index] *= -1;
                    }

                    index++;
                }
            }

            adjoint.Transpose();

            // The inverse is 1/det * adjoint.

            return adjoint * (1.0f / determinant);
        }

        //public static Matrix GetScale(double sx, double sy, double sz)
        //{
        //    Matrix m = new Matrix();
        //    m.SetScale(sx, sy, sz);
        //    return m;
        //}

        public static Matrix GetScale(float sx, float sy, float sz) => NewMatrix.SetScale(sx, sy, sz);
        public static Matrix GetScale(float s) => NewMatrix.SetScale(s, s, s);

        public static Matrix GetRotationX(float radians) => NewMatrix.SetRotationX(radians);
        public static Matrix GetRotationY(float radians) => NewMatrix.SetRotationY(radians);
        public static Matrix GetRotationZ(float radians) => NewMatrix.SetRotationZ(radians);

        public void SetIdentity()
        {
            SetNull();
            M11 = 1.0f;
            M22 = 1.0f;
            M33 = 1.0f;
            M44 = 1.0f;
        }

        public void SetTranslation(float x, float y, float z)
        {
            SetIdentity();
            M14 = x;
            M24 = y;
            M34 = z;
        }

        public Matrix SetScale(float sx, float sy, float sz)
        {
            SetIdentity();
            M11 = sx;
            M22 = sy;
            M33 = sz;

            return this;
        }

        // Rotation matrix around the x axis (clockwise);
        public Matrix SetRotationX(float radians)
        {
            SetIdentity();
            M22 = MathF.Cos(radians);
            M23 = -MathF.Sin(radians);
            M32 = MathF.Sin(radians);
            M33 = MathF.Cos(radians);

            return this;
        }

        // Rotation matrix around the y axis (clockwise).
        public Matrix SetRotationY(float radians)
        {
            SetIdentity();
            M11 = MathF.Cos(radians);
            M13 = MathF.Sin(radians);
            M31 = -MathF.Sin(radians);
            M33 = MathF.Cos(radians);

            return this;
        }

        // Rotation matrix around the z axis (anticlockwise).
        public Matrix SetRotationZ(float radians)
        {
            SetIdentity();
            M11 = MathF.Cos(radians);
            M12 = -MathF.Sin(radians);
            M21 = MathF.Sin(radians);
            M22 = MathF.Cos(radians);

            return this;
        }

        // Make camToWorld matrix.
        public void SetLookAt(Point from, Point to)
        {
            SetIdentity();

            Point cameraTarget = to;
            Point cameraPosition = from;

//            Vector zaxis = cameraTarget - cameraPosition;
            Vector zaxis = cameraPosition - cameraTarget;
            zaxis = zaxis.Normalise();

            Vector cameraUpVector = new Vector(0, 1, 0);

//            Vector xaxis = zaxis.VectorProduct(cameraUpVector);
            Vector xaxis = cameraUpVector.VectorProduct(zaxis);
            xaxis = xaxis.Normalise();

//            Vector yaxis = xaxis.VectorProduct(zaxis);
            Vector yaxis = zaxis.VectorProduct(xaxis);

            Vector vecPosition = new Vector(cameraPosition.X, cameraPosition.Y, cameraPosition.Z);

            M11 = xaxis.I;
            M12 = xaxis.J;
            M13 = xaxis.K;

            M21 = yaxis.I;
            M22 = yaxis.J;
            M23 = yaxis.K;

            M31 = zaxis.I;
            M32 = zaxis.J;
            M33 = zaxis.K;


            M14 = -xaxis.DotProduct(vecPosition);
            M24 = -yaxis.DotProduct(vecPosition);
            M34 = -zaxis.DotProduct(vecPosition);

            //M14 = -vecPosition.I;
            //M24 = -vecPosition.J;
            //M34 = -vecPosition.K;

            return;
        }

        public override string ToString()
        {
            StringBuilder s = new StringBuilder();

            for (int i=0; i<NumElements; i++)
            {
                if ((i % Size) == 0) s.Append("\n");
                s.Append(M[i].ToString("F3") + " ");
            }

            return s.ToString();
        }

        // Convert to SIMD-capable Matrix4x4 format.
        public Matrix4x4 ToMatrix4x4()
        {
            Matrix4x4 m = new Matrix4x4(M[0], M[1], M[2], M[3],  M[4], M[5], M[6], M[7],  M[8], M[9], M[10], M[11],  M[12], M[13], M[14], M[15]);

            // We need to transpose our values to match the Matrix4x4 format.
            m = Matrix4x4.Transpose(m);

            return m;

        }

        unsafe public void MatrixMultiplySIMD(SPoint[] points, SPoint[] results)
        {
            //fixed (float* matrixPtr = M, SVector* vectorPtr = vectors)
            fixed (SPoint* pointPtr = points)
            fixed (SPoint* resultPtr = results)
            {
                float* pointFloats = (float*)pointPtr;
                float* resultFloats = (float*)resultPtr;

                fixed (float* matrixPtr = M)
                {
                    int numPoints = points.Length;

                    MatrixMultiplySIMD(matrixPtr, pointFloats, resultFloats, numPoints);

                }
            }
        }

        /*
        unsafe public void MatrixMultiplySIMD(SVector[] vectors, SVector[] results)
        {
            //fixed (float* matrixPtr = M, SVector* vectorPtr = vectors)
            fixed (SVector* vectorPtr = vectors)
                fixed (SVector* resultPtr = results)
                {
                    float* vectorFloats = (float*)vectorPtr;
                    float* resultFloats = (float*)resultPtr;

                    fixed (float* matrixPtr = M)
                    {
                        int numVectors = vectors.Length;

                        MatrixMultiplySIMD(matrixPtr, vectorFloats, resultFloats, numVectors);

                    }
                }
        }
        */

        unsafe static void MatrixMultiplySIMD(float* matrixPtr, float* vectorsPtr, float* resultsPtr, int numVectors)
        {
            //fixed (float* matrixPtr = matrix, vectorsPtr = vectors, resultsPtr = results)
            {
                Vector256<float> row0;
                Vector256<float> row1;
                Vector256<float> row2;
                Vector256<float> row3;

                row0 = Avx2.BroadcastVector128ToVector256(matrixPtr);
                row1 = Avx2.BroadcastVector128ToVector256(matrixPtr + 4);
                row2 = Avx2.BroadcastVector128ToVector256(matrixPtr + 8);
                row3 = Avx2.BroadcastVector128ToVector256(matrixPtr + 12);

                // Process vectors in pairs.
                for (int v=0; v<numVectors - 1; v += 2)
                {
                    // Load two 4-element vectors (8 floats total)
                    Vector256<float> vecs = Avx.LoadVector256(vectorsPtr + v*4);

                    // Extract and broadcast elements from each vector
                    // Broadcast element 0 within 128-bit lanes (0x00 = 0)
                    Vector256<float> v0 = Avx.Permute(vecs, 0x00);

                    // Broadcast element 1 within 128-bit lanes (0x55 = 85)
                    Vector256<float> v1 = Avx.Permute(vecs, 0x55);

                    // Broadcast element 2 within 128-bit lanes (0xAA = 170)
                    Vector256<float> v2 = Avx.Permute(vecs, 0xAA);

                    // Broadcast element 3 within 128-bit lanes (0xFF = 255)
                    Vector256<float> v3 = Avx.Permute(vecs, 0xFF);

                    // Multiply each row bythe corresponding vector element and accumulate using FMA
                    Vector256<float> result = Avx.Multiply(row0, v0);

                    // Now use FMA instructions for better performance
                    result = Fma.MultiplyAdd(row1, v1, result);    // result += row1*v1
                    result = Fma.MultiplyAdd(row2, v2, result);    // result += row2*v2
                    result = Fma.MultiplyAdd(row3, v3, result);    // result += row3*v3

                    // Store the results for both vectors (8 floats total)
                    Avx.Store(resultsPtr + v*4, result);

                }

                // Process the last vector is numVectors is odd
                if (numVectors%2 != 0)
                {
                    int v = numVectors - 1;

                    // Use the single vector approach for the last one
                    Vector256<float> vec0 = Vector256.Create(vectorsPtr[v*4]);
                    Vector256<float> vec1 = Vector256.Create(vectorsPtr[v*4 + 1]);
                    Vector256<float> vec2 = Vector256.Create(vectorsPtr[v*4 + 2]);
                    Vector256<float> vec3 = Vector256.Create(vectorsPtr[v*4 + 3]);

                    Vector256<float> sum = Avx.Multiply(row0, vec0);

                    // Use FMA for the remaining operations
                    sum = Fma.MultiplyAdd(row1, vec1, sum);
                    sum = Fma.MultiplyAdd(row2, vec2, sum);
                    sum = Fma.MultiplyAdd(row3, vec3, sum);

                    // Store just the first 4 elements of the result
                    Sse.Store(resultsPtr + v*4, Avx.ExtractVector128(sum, 0));
                }
            }

        }
    }
}
