using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;



namespace RayTrace
{
    // Extension methods to make SVector act more like Vector.
    public static class SVectorExtensionMethods
    {
        extension(SVector v)
        {
            public float I => v.X;
            public float J => v.Y;
            public float K => v.Z;
            public float W => v.W;

            public static SVector Create(float i, float j, float k) => new Vector4(i, j, k, 1f);

            public float DotProduct(SVector v2) => Vector4.Dot(v, v2);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public SVector VectorProduct(SVector v2) => Vector4.Cross(v, v2);

            public SVector Add(SVector v2) => v+v2;// Vector4.Cross(v, v2);

            public SVector Scale(float scale) => v*scale;

            public SVector Negate() => Vector4.Negate(v);

            public float DotProductSaturated(SVector v2)
            {
                float dp = Vector4.Dot(v, v2);
                return Math.Clamp(dp, 0f, 1f);
            }

            public SVector Normalise()
            {
                float magnitude = Magnitude(v);
                if (magnitude == 0.0f)
                {
                    magnitude = 1.0f;
                }

                return new SVector(v.X/magnitude, v.Y/magnitude, v.Z/magnitude, 0f);
            }

            public float Magnitude() => v.Length();
            public float MagnitudeSquared() => v.LengthSquared();

            public bool IsNull() => v == Vector4.Zero;

            //public SVector Normalise() => Vector4.Normalize(v);

            public static SVector Null() => Vector4.Zero;

            public SPoint AsPoint() => new SPoint(v.I, v.J, v.K);

        }

    }

    public static class ExtensionMethods
    {
        public static string AsString(this SVector v)
        {
            Vector vector = new(v);
            return vector.ToString();
        }
    }


#if false
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct SVector
    {
        public readonly float I { get; }
        public readonly float J { get; }
        public readonly float K { get; }
        private readonly float W = 0.0f;

        private bool IsNaN()
        {
            return float.IsNaN(I) || float.IsNaN(J) || float.IsNaN(K);
        }

        //public SVector() => (I, J, K) = (0, 0, 0);

        public SVector(Vector v) => (I, J, K) = (v.I, v.J, v.K);

        // Temporary, until SVector2 replaces SVector.
        public SVector(SVector2 v) => (I, J, K, W) = (v.I, v.J, v.K, v.Z);

        public SVector(float i, float j, float k) => (I, J, K, W) = (i, j, k, 0f);

        public float DotProduct(in SVector v) => I * v.I + J * v.J + K * v.K;
        public SVector VectorProduct(in SVector v) => new(J * v.K - K * v.J, K * v.I - I * v.K, I * v.J - J * v.I);
        public SVector Add(in SVector v) => new(I + v.I, J + v.J, K + v.K);
        public SVector Subtract(in SVector v) => new(I - v.I, J - v.J, K - v.K);
        public SVector Scale(float scale) => new(I * scale, J * scale, K * scale);
        public SVector Negate() => new(-I, -J, -K);

        // Dot product clamped to range 0 - 1.
        public float DotProductSaturated(in SVector v)
        {
            return Math.Clamp(DotProduct(v), 0f, 1f);
//            return Math.Clamp(DotProduct(v), 0.0001f, 0.9999f);
        }

        // Return angle between Vectors in radians.
        public float Angle(in SVector v)
        {
            float dp = DotProduct(v);
            if (dp > 1.0) return 0.0f;
            if (dp < -1.0) return (float) Math.PI;

            return (float) Math.Cos(dp);
        }

        public float Magnitude() => (float) Math.Sqrt(I * I + J * J + K * K);
        public float MagnitudeSquared() => I * I + J * J + K * K;

        public bool IsNull() => I == 0.0 && J == 0.0 && K == 0.0;

        public SVector Normalise()
        {
            float magnitude = Magnitude();
            if (magnitude == 0.0f)
            {
                magnitude = 1.0f;
            }

            return new SVector(I/magnitude, J/magnitude, K/magnitude);
        }

        public SPoint AsPoint() => new(I, J, K);
        public Vector4 ToVector4() => new(I, J, K, 0);
        public Vector4 ToPoint4() => new(I, J, K, 1);

        // Return a null vector, i.e. one with all components set to 0.
        public static SVector Null() => new(0.0f, 0.0f, 0.0f);

        public static SVector Lerp(in SVector v1, in SVector v2, float ratio)
        {
            ratio = ratio < 0.0f ? 0.0f : ratio;
            ratio = ratio > 1.0f ? 1.0f : ratio;

            SVector v = v1 + (v2 - v1)*ratio;

            return v;
        }

        public static SVector operator+(in SVector v1, in SVector v2) => new(v1.I + v2.I, v1.J + v2.J, v1.K + v2.K);
        public static SVector operator-(in SVector v1, in SVector v2) => new(v1.I - v2.I, v1.J - v2.J, v1.K - v2.K);
        public static SVector operator-(in SVector v) => new(-v.I, -v.J, -v.K);
        public static SVector operator*(in SVector v, float scale) => new(v.I * scale, v.J * scale, v.K * scale);

        public static bool operator==(in SVector v1, in SVector v2) => v1.I == v2.I && v1.J == v2.J && v1.K == v2.K;
        public static bool operator!=(in SVector v1, in SVector v2) => !(v1 == v2); //v1.I == v2.I && v1.J == v2.J && v1.K == v2.K;

        public override bool Equals(object obj)
        {
            if (obj is SVector other)
            {
                return this == other;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(I, J, K);
        }
    }
#endif


    public class Vector : INotifyPropertyChanged
    {
        private float i;
        private float j;
        private float k;

        public event PropertyChangedEventHandler PropertyChanged;

        private void Notify(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        private bool IsNaN()
        {
            return float.IsNaN(i) || float.IsNaN(j) || float.IsNaN(k);
        }

        public float I { get { return i; } set { if (i != value) { i = value; Notify("I"); } } }
        public float J { get { return j; } set { if (j != value) { j = value; Notify("J"); } } }
        public float K { get { return k; } set { if (k != value) { k = value; Notify("K"); } } }

        public Vector(SVector v) => (I, J, K) = (v.I, v.J, v.K);  // Temporary

        public Vector() : this(0.0f, 0.0f, 0.0f) { }

        public Vector(float i, float j, float k, bool normalise = false)
        {
            I = i;
            J = j;
            K = k;

            Debug.Assert(!IsNaN()); 

            if (normalise)
            {
                float magnitude = Magnitude();
                if (magnitude != 0.0)
                {
                    I /= magnitude;
                    J /= magnitude;
                    K /= magnitude;
                }
            }
        }

        public Vector(XMLVector xmlVector) => (I, J, K) = (xmlVector.I, xmlVector.J, xmlVector.K);

        public static Vector operator+(Vector v1, Vector v2) => new (v1.I + v2.I, v1.J + v2.J, v1.K + v2.K);

        public static Vector operator-(Vector v1, Vector v2) => new (v1.I - v2.I, v1.J - v2.J, v1.K - v2.K);

        public static Vector operator -(Vector v) => new (-v.I, -v.J, -v.K);

        public static Vector operator*(Vector v, float scale) => new (v.I * scale, v.J * scale, v.K * scale);

        public float DotProduct(Vector v) => I* v.I + J* v.J + K* v.K;

        public Vector VectorProduct(Vector v) => new (J * v.K - K * v.J, K * v.I - I * v.K, I * v.J - J * v.I);

        public Vector Add(Vector v) => new (I + v.I, J + v.J, K + v.K);

        public Vector Subtract(Vector v) => new (I - v.I, J - v.J, K - v.K);

        public Vector Scale(float scale) => new (I * scale, J * scale, K * scale);

        public Vector Negate() => new (-I, -J, -K);

        // Return angle between Vectors in radians.
        public float Angle(Vector v)
        {
            float dp = DotProduct(v);
            if (dp > 1.0) return 0.0f;
            if (dp < -1.0) return (float) Math.PI;

            return (float) Math.Cos(dp);
        }

        public float Magnitude() => (float) Math.Sqrt(I * I + J * J + K * K);
        public float MagnitudeSquared() => I * I + J * J + K * K;

        public Vector Normalise() => new (I, J, K, true);

        // Return a null vector, i.e. one with all components set to 0.
        public static Vector Null() => new (0.0f, 0.0f, 0.0f);

        public bool IsNull() => I == 0.0 && J == 0.0 && K == 0.0;

        public Point AsPoint() => new Point(I, J, K);

        public override string ToString()
        {
//            return "Vector[" + I + "," + J + "," + K + "]";
            return "[" + I.ToString("F2") + "," + J.ToString("F2") + "," + K.ToString("F2") + "]";
        }

        public XMLVector ToXML()
        {
            return new XMLVector() { I = this.I, J = this.J, K = this.K };
        }

        public SVector ToSVector()
        {
            return new Vector4(I, J, K, 0.0f);
        }

        // Convert to SIMD-capable Vector4 format.
        public Vector4 ToVector4()
        {
            return new Vector4(I, J, K, 0.0f);
        }
    }
}
