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
                Normalise();
            }
        }

        public static Vector operator+(Vector v1, Vector v2)
        {
            return new Vector(v1.I + v2.I, v1.J + v2.J, v1.K + v2.K);
        }

        public static Vector operator-(Vector v1, Vector v2)
        {
            return new Vector(v1.I - v2.I, v1.J - v2.J, v1.K - v2.K);
        }

        public static Vector operator*(Vector v, double scale)
        {
            return new Vector(v.I * scale, v.J * scale, v.K * scale);
        }

        public double DotProduct(Vector v)
        {
            return I * v.I + J * v.J + K * v.K;
        }

        public Vector VectorProduct(Vector v)
        {
            return new Vector(J*v.K - K*v.J, K*v.I - I*v.K, I*v.J - J*v.I);
        }

        public Vector Add(Vector v)
        {
            return new Vector(I + v.I, J + v.J, K + v.K);
        }

        public Vector Subtract(Vector v)
        {
            return new Vector(I - v.I, J - v.J, K - v.K);
        }

        public Vector Scale(double scale)
        {
            return new Vector(I * scale, J * scale, K * scale);
        }

        public double Angle(Vector v)
        {
            double dp = DotProduct(v);
            if (dp > 1.0) return 0.0;
            if (dp < -1.0) return Math.PI;

            return Math.Cos(dp);
        }

        public double Magnitude()
        {
            return Math.Sqrt(I*I + J*J + K*K);
        }

        public void Normalise()
        {
            double magnitude = Magnitude();
            if (magnitude != 0.0)
            {
                I /= magnitude;
                J /= magnitude;
                K /= magnitude;
            }
        }

        public override string ToString()
        {
            return "Vector[" + I + "," + J + "," + K + "]";
        }
    }
}
