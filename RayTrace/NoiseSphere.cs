using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    public class NoiseSphere : Sphere
    {
        public NoiseSphere(Point p, int radius, Colour col) : base(p, radius, col) { } 

        public override HitResult HitTest(Camera camera, Ray ray)
        {

            return null;
        }

    }
}
