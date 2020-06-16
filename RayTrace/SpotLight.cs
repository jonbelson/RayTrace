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

        public override Vector GetDirection(Camera camera, Point p)
        {
            Point viewPos = camera.WorldToView * Pos;

            //Vector vecToLight = Pos.Difference(p);
            Vector vecToLight = viewPos.Difference(p);

            return vecToLight;
        }

        public override float GetAttenuation(Camera camera, Point p)
        {
            Point viewPos = camera.WorldToView * Pos;
            Vector viewDirection = camera.NormalMatrix*Direction;

            //Vector vecFromLight = p.Difference(Pos).Negate();
            //Vector vecFromLight = p.Difference(viewPos).Negate();
            Vector vecFromLight = viewPos.Difference(p);

            Vector vecNorm = vecFromLight;
            vecNorm = vecNorm.Normalise();

            double dp = vecNorm.DotProduct(viewDirection);

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
