using System;
namespace RayTrace
{
    public struct Vector
    {
        public double I { get; set; }
        public double J { get; set; }
        public double K { get; set; }

        //public Vector() : this(0.0, 0.0, 0.0) { }

        public Vector(double i, double j, double k, bool normalise = false)
        {
            I = i;
            J = j;
            K = k;

            if (normalise)
            {
                double magnitude = Magnitude();
                if (magnitude != 0.0)
                {
                    I /= magnitude;
                    J /= magnitude;
                    K /= magnitude;
                }
            }
        }

        public static Vector operator+(Vector v1, Vector v2) => new Vector(v1.I + v2.I, v1.J + v2.J, v1.K + v2.K);
        //{
        //    return new Vector(v1.I + v2.I, v1.J + v2.J, v1.K + v2.K);
        //}

        public static Vector operator-(Vector v1, Vector v2) => new Vector(v1.I - v2.I, v1.J - v2.J, v1.K - v2.K);
        //{
        //    return new Vector(v1.I - v2.I, v1.J - v2.J, v1.K - v2.K);
        //}

        public static Vector operator*(Vector v, double scale) => new Vector(v.I * scale, v.J * scale, v.K * scale);
        //{
        //    return new Vector(v.I * scale, v.J * scale, v.K * scale);
        //}

        public double DotProduct(Vector v) => I* v.I + J* v.J + K* v.K;
        //{
        //    return I * v.I + J * v.J + K * v.K;
        //}

        public Vector VectorProduct(Vector v) => new Vector(J * v.K - K * v.J, K * v.I - I * v.K, I * v.J - J * v.I);
        //{
        //    return new Vector(J*v.K - K*v.J, K*v.I - I*v.K, I*v.J - J*v.I);
        //}

        public Vector Add(Vector v) => new Vector(I + v.I, J + v.J, K + v.K);
        //{
        //    return new Vector(I + v.I, J + v.J, K + v.K);
       // }

        public Vector Subtract(Vector v) => new Vector(I - v.I, J - v.J, K - v.K);
        //{
        //    return new Vector(I - v.I, J - v.J, K - v.K);
        //}

        public Vector Scale(double scale) => new Vector(I * scale, J * scale, K * scale);
        //{
        //    return new Vector(I * scale, J * scale, K * scale);
        //}

        public Vector Negate() => new Vector(-I, -J, -K);
        //{
        //    return new Vector(-I, -J, -K);
        //}

        // Return angle between Vectors in radians.
        public double Angle(Vector v)
        {
            double dp = DotProduct(v);
            if (dp > 1.0) return 0.0;
            if (dp < -1.0) return Math.PI;

            return Math.Cos(dp);
        }

        public double Magnitude() => Math.Sqrt(I * I + J * J + K * K);
        //{
        //    return Math.Sqrt(I*I + J*J + K*K);
        //}
        public double MagnitudeSquared() => I * I + J * J + K * K;
        //{
        //    return I * I + J * J + K * K;
        //}

        public Vector Normalise() => new Vector(I, J, K, true);
        //{
        //    return new Vector(I, J, K, true);
        //}

        // Return a null vector, i.e. one with all components set to 0.
        public static Vector Null() => new Vector(0.0, 0.0, 0.0);
        //{
        //    return new Vector(0.0, 0.0, 0.0);
        //}

        public bool IsNull() => I == 0.0 && J == 0.0 && K == 0.0;
        //{
        //    return I == 0.0 && J == 0.0 && K == 0.0;
        //}

        public override string ToString()
        {
//            return "Vector[" + I + "," + J + "," + K + "]";
            return "[" + I.ToString("F2") + "," + J.ToString("F2") + "," + K.ToString("F2") + "]";
        }
    }
}
