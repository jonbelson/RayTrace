using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    public class SkydomeGradient : Skydome
    {
        public Vector Up { get; set; } = new Vector(0, 1, 0);

        public Colour Top { get; set; }
        public Colour Middle { get; set; }
        public Colour Bottom { get; set; }

        public SkydomeGradient(Colour top, Colour middle, Colour bottom)
        {
            Top = top;
            Middle = middle;
            Bottom = bottom;
        }

        public SkydomeGradient(Colour top, Colour middle) : this(top, middle, middle) { }

        public SkydomeGradient() : this(Palette.Black, Palette.Black) { }

        public override Colour GetColour(Camera camera, SVector ray)
        {
            double dp = ray.DotProduct(new(Up));

            if (dp < 0)
            {
                double ratio = 1.0 - Math.Abs(dp);
                return Colour.LinearBlend(Bottom, Middle, (float) ratio);
            }
            else
            {
                return Colour.LinearBlend(Middle, Top, (float) dp);
            }
        }
    }
}
