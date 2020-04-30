using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    public class SpotLight : PointLight
    {
        public Vector Direction { get; set; }

        // Width in degrees.
        double widthRad;
        int widthDeg;
        public int Width
        {
            get => widthDeg;
            set { widthDeg = value; widthRad = value / 360.0 * 2.0 * Math.PI; }
        }

        public SpotLight(Point p, Vector direction, int widthDeg, float intensity, Colour c) : base (p, intensity, c)
        {
            Direction = direction;
            Width = widthDeg;
        }

        public override Vector GetDirection(Point p)
        {
            Vector vecToLight = Pos.Difference(p);

            return vecToLight;
        }

        public override float GetAttenuation(Point p)
        {
            Vector vecFromLight = p.Difference(Pos).Negate();

            Vector vecNorm = vecFromLight;
            vecNorm.Normalise();

            double dp = vecNorm.DotProduct(Direction);

            double angleRad = Math.Acos(dp);
            if (angleRad > widthRad)
            {
                return 0.0f;
            }

            // Assume light attenuatiaon is 1 over distance squared.
            double lightAttenuation = 1.0 / vecFromLight.MagnitudeSquared();

            return (float)lightAttenuation;
        }

    }
}
