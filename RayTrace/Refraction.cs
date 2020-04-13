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
         * Calculate refracted angle of ray based on relative refractive indices.
         * 'An Introduction to Ray Tracing' page section 5.2.
         * 
         * I    Incoming light ray.
         * N    Surface normal.
         * n1   Refractive index of medium 1 wrt vacuum.
         * n2   Refraction index of medium 2 wrt vacuum.
         * 
         * Return   Vector of refracted ray.
         */
        public static Vector GetAngle(Vector I, Vector N, float n1, float n2)
        {
            /*
            double eta = n1 / n2;

            double c1 = -I.DotProduct(N);
            double cs2 = 1.0 - eta * eta * (1.0 - c1 * c1);
            if (cs2 < 0.0)
            {
                return Vector.Null();
            }

            Vector T = I.Scale(eta) + N.Scale(eta*c1 - Math.Sqrt(cs2));
            return T;
            */

            float nit = n1 / n2;
            double Ci = N.DotProduct(I.Negate());   // cos(θi)
            double v = (1.0 - nit * nit * (1.0 - Ci * Ci));

            if (v < 0.0)
            {
                // Total internal reflection, so no light transmission occurs.
                return Vector.Null();
            }

            Vector vT = I.Scale(nit) + N.Scale(nit * Ci - Math.Sqrt(v));

            return vT;
        }
    }
}
