using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace RayTrace
{
    public class Ray
    {
        public SPoint Origin { get; set; } = new SPoint(0, 0, 0, 1);
        public SVector Direction { get; set; } = new SVector(0, 0, 1, 0);

        public float TMin { get; set; } = 0.001f;
        public float TMax { get; set; } = float.MaxValue;

        // Intersection can be at any point along the line.
        public Ray(SPoint p, SVector d)
        {
            (Origin, Direction) = (p, d);
        }

        // Intersection must be between the two points.
        public static Ray FromPoints(SPoint p, SPoint d)
        {
            return new Ray(p, d - p)
            {
                TMax = 1.0f
            };

        }
    }
}
