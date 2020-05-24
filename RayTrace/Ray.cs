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
        public Point Origin { get; set; } = new Point(0, 0, 0);
        public Vector Direction { get; set; } = new Vector(0, 0, 1);

        public float TMin { get; set; } = 0.0f;
        public float TMax { get; set; } = float.MaxValue;

        public Ray(Point p, Vector d) => (Origin, Direction) = (p, d);

        public Ray(Point p, Point d) : this(p, d - p)
        {
            TMax = 1.0f;
        }

    }
}
