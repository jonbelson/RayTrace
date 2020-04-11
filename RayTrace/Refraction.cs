using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    // Refractive indexes at wavelength of 589nm.
    public class RefractiveIndex
    {
        public static float Vacuum = 1.0f;

        public static float Air = 1.0003f;
        public static float Diamond = 2.42f;
        public static float FlintGlass = 1.69f;
        public static float FusedQuartz = 1.46f;
        public static float Ice = 1.31f;
        public static float Pyrex = 1.470f;
        public static float Sapphire = 1.77f;
        public static float Water = 1.333f;
        public static float WindowGlass = 1.52f;
    }

    public class Refraction
    {
        /*
         * I    Incoming light ray.
         * N    Surface normal.
         * n1   Refractive index of medium 1 wrt vacuum.
         * n2   Refraction index of medium 2 wrt vacuum.
         * 
         * Return   Vector of refracted ray.
         */
        public static Vector GetAngle(Vector I, Vector N, float n1, float n2)
        {
            float nit = n2 / n1;

            double Ci = N.DotProduct(I.Negate());   // cos(θi)
            //double Ct = I.DotProduct(T.Negate());   // cos(θt)

            double v = (1.0 + nit * nit * (Ci * Ci - 1));

            if (v < 0.0)
            {
                // Total internal reflection, so no light transmission occurs.
                return Vector.Null();
            }

            Vector T1 = I.Scale(nit);
            double T2 = (nit * Ci - Math.Sqrt(v));
            Vector T = T1 + N.Scale(T2);

            return T;
        }
    }
}
