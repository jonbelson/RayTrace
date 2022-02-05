using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    public class NoiseSphere : Sphere
    {
        public NoiseSphere(Scene scene, Point p, int radius, Colour col) : base(scene, p, radius, col) { } 

        public override HitResult HitTest(Camera camera, Ray ray)
        {

            return new HitResult(this);
        }

    }
}
