using System;
using System.ComponentModel;

namespace RayTrace
{
    public readonly struct SVector
    {
        public float I { get; }
        public float J { get; }
        public float K { get; }

        //public SVector() => (I, J, K) = (0, 0, 0);

        public SVector(Vector v) => (I, J, K) = (v.I, v.J, v.K);

        public SVector(float i, float j, float k) => (I, J, K) = (i, j, k);

        public float DotProduct(in SVector v) => I * v.I + J * v.J + K * v.K;
        public SVector VectorProduct(in SVector v) => new(J * v.K - K * v.J, K * v.I - I * v.K, I * v.J - J * v.I);
        public SVector Add(in SVector v) => new(I + v.I, J + v.J, K + v.K);
        public SVector Subtract(in SVector v) => new(I - v.I, J - v.J, K - v.K);
        public SVector Scale(float scale) => new(I * scale, J * scale, K * scale);
        public SVector Negate() => new(-I, -J, -K);

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

        // Return a null vector, i.e. one with all components set to 0.
        public static SVector Null() => new(0.0f, 0.0f, 0.0f);

        public static SVector operator+(in SVector v1, in SVector v2) => new(v1.I + v2.I, v1.J + v2.J, v1.K + v2.K);
        public static SVector operator-(in SVector v1, in SVector v2) => new(v1.I - v2.I, v1.J - v2.J, v1.K - v2.K);
        public static SVector operator-(in SVector v) => new(-v.I, -v.J, -v.K);
        public static SVector operator*(in SVector v, float scale) => new(v.I * scale, v.J * scale, v.K * scale);
    }

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

        public static Vector operator+(Vector v1, Vector v2) => new Vector(v1.I + v2.I, v1.J + v2.J, v1.K + v2.K);

        public static Vector operator-(Vector v1, Vector v2) => new Vector(v1.I - v2.I, v1.J - v2.J, v1.K - v2.K);

        public static Vector operator -(Vector v) => new Vector(-v.I, -v.J, -v.K);

        public static Vector operator*(Vector v, float scale) => new Vector(v.I * scale, v.J * scale, v.K * scale);

        public float DotProduct(Vector v) => I* v.I + J* v.J + K* v.K;

        public Vector VectorProduct(Vector v) => new Vector(J * v.K - K * v.J, K * v.I - I * v.K, I * v.J - J * v.I);

        public Vector Add(Vector v) => new Vector(I + v.I, J + v.J, K + v.K);

        public Vector Subtract(Vector v) => new Vector(I - v.I, J - v.J, K - v.K);

        public Vector Scale(float scale) => new Vector(I * scale, J * scale, K * scale);

        public Vector Negate() => new Vector(-I, -J, -K);

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

        public Vector Normalise() => new Vector(I, J, K, true);

        // Return a null vector, i.e. one with all components set to 0.
        public static Vector Null() => new Vector(0.0f, 0.0f, 0.0f);

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
    }
}
