using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    // Refractive indexes at wavelength of 589nm.
    public static class RefractiveIndex
    {
        public const float Vacuum = 1.0f;

        public const float Air = 1.0003f;
        public const float Diamond = 2.42f;
        public const float FlintGlass = 1.69f;
        public const float FusedQuartz = 1.46f;
        public const float Ice = 1.31f;
        public const float Pyrex = 1.470f;
        public const float Sapphire = 1.77f;
        public const float Water = 1.333f;
        public const float WindowGlass = 1.52f;
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
        public static SVector GetAngle(SVector I, SVector N, float n1, float n2)
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
            /*float*/ double Ci = N.DotProduct(I.Negate());   // cos(θi)
            /*float*/ double v = (1.0f - nit * nit * (1.0f - Ci * Ci));

            Debug.Assert(!float.IsNaN(nit));
            //Debug.Assert(!float.IsNaN(Ci));
            //Debug.Assert(!float.IsNaN(v));

            if (v < 0.0)
            {
                // Total internal reflection, so no light transmission occurs.
                return SVector.Null();
            }

//            SVector vT = I.Scale(nit) + N.Scale(nit * Ci - (float)Math.Sqrt(v));
            SVector vT = I.Scale(nit) + N.Scale((float)(nit * Ci - Math.Sqrt(v)));

            Debug.Assert(!float.IsNaN(vT.I));
            Debug.Assert(!float.IsNaN(vT.J));
            Debug.Assert(!float.IsNaN(vT.K));

            return vT;
        }
    }
}
