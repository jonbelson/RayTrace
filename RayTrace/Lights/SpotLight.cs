using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    public class SpotLight : PointLight
    {
        //public Vector Direction { get; set; }
        public Vector Normal { get; set; }

        // Width in degrees.
        private double widthRad;
        private int widthDeg;
        public int Width
        {
            get => widthDeg;
            set { widthDeg = value; widthRad = value / 360.0 * 2.0 * Math.PI; }
        }

        //
        // Constructor.
        //
        public SpotLight(Scene scene, Point p, Vector direction, int widthDeg, float intensity, Colour c) : base(scene, p, intensity, c)
        {
            Normal = direction;
            Width = widthDeg;
        }

        private SVector _direction = new SVector();

        // Allow Shape to pre-transform data using the Camera's WorldToView Matrix.
        public override void CreateCache(Camera camera)
        {
            base.CreateCache(camera);
            _direction = new SVector(Normal);
        }

        public override SVector GetDirection(Camera camera, SPoint p)
        {
            SPoint viewPos = camera.WorldToView * _pos;

            //Vector vecToLight = Pos.Difference(p);
            SVector vecToLight = viewPos.Difference(p);

            return vecToLight;
        }

        public override float GetAttenuation(Camera camera, SPoint p)
        {
            SPoint viewPos = camera.WorldToView * _pos;
            SVector viewDirection = camera.NormalMatrix*_direction;

            //Vector vecFromLight = p.Difference(Pos).Negate();
            //Vector vecFromLight = p.Difference(viewPos).Negate();
            SVector vecFromLight = viewPos.Difference(p);

            SVector vecNorm = vecFromLight;
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
