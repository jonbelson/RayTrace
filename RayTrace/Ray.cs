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
        public SPoint Origin { get; set; } = new SPoint(0, 0, 0);
        public SVector Direction { get; set; } = new SVector(0, 0, 1);

        public float TMin { get; set; } = 0.0f;
        public float TMax { get; set; } = float.MaxValue;

        public Ray(SPoint p, SVector d)
        {
            (Origin, Direction) = (p, d);
            //TMin = 0.0f;
            //TMax = float.MaxValue;
        }

        public Ray(SPoint p, SPoint d) : this(p, d - p)
        {
            TMin = 0.0f;
            TMax = 1.0f;
        }
    }
}
