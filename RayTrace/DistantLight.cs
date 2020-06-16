using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    public class DistantLight : Light
    {
        public Vector Direction { get; set; }

        public DistantLight(Vector direction, float intensity, Colour c) : base(intensity, c)
        {
            Direction = direction;
        }

        public override Vector GetDirection(Camera camera, Point p)
        {
            return Direction;
        }

        public override float GetAttenuation(Camera camera, Point p)
        {
            return 1.0f;
        }
    }
}
