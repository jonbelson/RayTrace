using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Xaml;

namespace RayTrace
{
    //
    // Column vector 4x4 Matrix stored in row major format.
    //
    //  0  1  2  3
    //  4  5  6  7
    //  8  9 10 11
    // 12 13 14 15
    //
    public class Matrix
    {
        private static readonly int Size = 4;
        private static readonly int NumElements = Size * Size;

        public double[] M { get; set;  } = new double[NumElements];

        // M|row|column
        public double M11 { get { return M[0]; } set { M[0] = value; } }
        public double M12 { get { return M[1]; } set { M[1] = value; } }
        public double M13 { get { return M[2]; } set { M[2] = value; } }
        public double M14 { get { return M[3]; } set { M[3] = value; } }

        public double M21 { get { return M[4]; } set { M[4] = value; } }
        public double M22 { get { return M[5]; } set { M[5] = value; } }
        public double M23 { get { return M[6]; } set { M[6] = value; } }
        public double M24 { get { return M[7]; } set { M[7] = value; } }

        public double M31 { get { return M[8]; } set { M[8] = value; } }
        public double M32 { get { return M[9]; } set { M[9] = value; } }
        public double M33 { get { return M[10]; } set { M[10] = value; } }
        public double M34 { get { return M[11]; } set { M[11] = value; } }

        public double M44 { get { return M[15]; } set { M[15] = value; } }

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
            M = other.M;
        }

        private static Matrix NewMatrix { get { return new Matrix(); } }

        public static Vector operator*(Matrix m, Vector v)
        {
            Vector vv = new Vector
            {
                I = m.M11 * v.I + m.M12 * v.J + m.M13 * v.K + m.M14 * 1.0,
                J = m.M21 * v.I + m.M22 * v.J + m.M23 * v.K + m.M24 * 1.0,
                K = m.M31 * v.I + m.M32 * v.J + m.M33 * v.K + m.M34 * 1.0
            };

            return vv;
        }

        public static Point operator*(Matrix m, Point p)
        {
            Point pp = new Point
            {
                X = m.M11 * p.X + m.M12 * p.Y + m.M13 * p.Z + m.M14 * 1.0,
                Y = m.M21 * p.X + m.M22 * p.Y + m.M23 * p.Z + m.M24 * 1.0,
                Z = m.M31 * p.X + m.M32 * p.Y + m.M33 * p.Z + m.M34 * 1.0
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
                    double sum = 0.0;
                    for (int k = 0; k < Size; k++)
                    {
                        sum += m1.M[k * Size + i] * m2.M[j * Size + k];
                    }
                    mm.M[j * Size + i] = sum;
                }
            }

            return mm;
        }

        // Set all values to 0. Also known as a zero matrix.
        public void SetNull()
        {
            for (int i=0; i<NumElements; i++)
            {
                M[i] = 0.0;
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

            M = m2.M;
        }

        public static Matrix GetIdentity()
        {
            Matrix m = new Matrix();
            m.SetIdentity();
            return m;
        }

        //public double GetDeterminant()
        //{

        //}

        public static Matrix GetTranslation(double x, double y, double z)
        {
            Matrix m = new Matrix();
            m.SetTranslation(x, y, z);
            return m;
        }

        //public static Matrix GetScale(double sx, double sy, double sz)
        //{
        //    Matrix m = new Matrix();
        //    m.SetScale(sx, sy, sz);
        //    return m;
        //}

        public static Matrix GetScale(double sx, double sy, double sz) => NewMatrix.SetScale(sx, sy, sz);

        public void SetIdentity()
        {
            SetNull();
            M11 = 1.0;
            M22 = 1.0;
            M33 = 1.0;
            M44 = 1.0;
        }

        public void SetTranslation(double x, double y, double z)
        {
            SetIdentity();
            M14 = x;
            M24 = y;
            M34 = z;
        }

        public Matrix SetScale(double sx, double sy, double sz)
        {
            SetIdentity();
            M11 = sx;
            M22 = sy;
            M33 = sz;

            return this;
        }

        // Rotation matrix around the x axis (clockwise);
        public void SetRotationX(double radians)
        {
            SetIdentity();
            M22 = Math.Cos(radians);
            M23 = -Math.Sin(radians);
            M32 = Math.Sin(radians);
            M33 = Math.Cos(radians);
        }

        // Rotation matrix around the y axis (clockwise).
        public void SetRotationY(double radians)
        {
            SetIdentity();
            M11 = Math.Cos(radians);
            M13 = Math.Sin(radians);
            M31 = -Math.Sin(radians);
            M33 = Math.Cos(radians);
        }

        // Rotation matrix around the z axis (anticlockwise).
        public void SetRotationZ(double radians)
        {
            SetIdentity();
            M11 = Math.Cos(radians);
            M12 = -Math.Sin(radians);
            M21 = Math.Sin(radians);
            M22 = Math.Cos(radians);
        }

        // Make camToWorld matrix.
        public void SetLookAt(Point from, Point to)
        {
            SetIdentity();

            //return;

            Point cameraTarget = to;
            Point cameraPosition = from;
            Vector zaxis = cameraTarget - cameraPosition;
//            Vector zaxis = cameraPosition - cameraTarget;

            zaxis.Normalise();
            Vector cameraUpVector = new Vector(0, 1, 0);

            Vector xaxis = cameraUpVector.VectorProduct(zaxis);
            //xaxis = zaxis.VectorProduct(cameraUpVector);
            xaxis.Normalise();

            Vector yaxis = zaxis.VectorProduct(xaxis);
            //yaxis = xaxis.VectorProduct(zaxis);

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

//            xaxis.x yaxis.x zaxis.x          0
// xaxis.y yaxis.y zaxis.y          0
// xaxis.z yaxis.z zaxis.z          0
//- dot(xaxis, cameraPosition) - dot(yaxis, cameraPosition) - dot(zaxis, cameraPosition)  1





//            Vector forward = from - to;
            Vector forward = to - from;
            forward.Normalise();

            Vector temp = new Vector(0.0, 1.0, 0.0);
            Vector right = temp.VectorProduct(forward);
            Vector up = forward.VectorProduct(right);

            //M11 = right.I;
            //M21 = right.J;
            //M31 = right.K;

            //M12 = up.I;
            //M22 = up.J;
            //M32 = up.K;

            //M13 = forward.I;
            //M23 = forward.J;
            //M33 = forward.K;

            M11 = right.I;
            M12 = right.J;
            M13 = right.K;

            M21 = up.I;
            M22 = up.J;
            M23 = up.K;

            M31 = forward.I;
            M32 = forward.J;
            M33 = forward.K;

            M14 = -from.X;
            M24 = -from.Y;
            M34 = -from.Z;

            // XXX Probably need to take the transpose of this.
        }

        public override string ToString()
        {
            StringBuilder s = new StringBuilder();

            for (int i=0; i<NumElements; i++)
            {
                if ((i % Size) == 0) s.Append("\n");
                s.Append(M[i] + " ");
            }

            return s.ToString();
        }
    }
}
