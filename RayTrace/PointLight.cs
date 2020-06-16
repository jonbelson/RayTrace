using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    public class PointLight : Light
    {
        public Point Pos { get; set; }

        // Effective radius of point soruce, as used in soft shadow calculations.
        public int Radius { get; set; } = 0;

        public PointLight(Point p, float intensity, Colour c) : base(intensity, c)
        {
            Pos = p;
        }

        public override Vector GetDirection(Camera camera, Point p)
        {
            Point viewPos = camera.WorldToView*Pos;
            return viewPos.Difference(p);

/*
            Vector vecToLight = Pos.Difference(p);
//            Vector vecToLight = p.Difference(Pos);

            return vecToLight;
*/
        }

        public override float GetAttenuation(Camera camera, Point p)
        {
            Point viewPos = camera.WorldToView * Pos;

            //Vector vecToLight = p.Difference(Pos).Negate();
            Vector vecToLight = viewPos.Difference(p);

            // Assume light attenuatiaon is 1 over distance squared.
            double lightAttenuation = 1.0 / vecToLight.MagnitudeSquared();

            return (float) lightAttenuation;
        }

        public override string ToString()
        {
            return "Light[" + Pos.ToString() + "]," + Intensity + "," + Colour.ToString() + "]";
        }
    }
}
