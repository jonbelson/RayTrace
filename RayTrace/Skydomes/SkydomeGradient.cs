using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace.Skydomes
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

        public SkydomeGradient(XMLSkydomeGradient xmlSkydomeGradient) : base()
        {
            Top = new(xmlSkydomeGradient.Top);
            Middle = new(xmlSkydomeGradient.Middle);
            Bottom = new(xmlSkydomeGradient.Bottom);
        }

        public override SColour GetColour(Camera camera, SVector ray)
        {
            double dp = ray.DotProduct(Up.ToSVector());

            if (dp < 0)
            {
                double ratio = 1.0 - Math.Abs(dp);
                return SColour.LinearBlend(Bottom.ToSColour(), Middle.ToSColour(), (float) ratio);
            }
            else
            {
                return SColour.LinearBlend(Middle.ToSColour(), Top.ToSColour(), (float) dp);
            }
        }

        public override XMLSkydome ToXML()
        {
            XMLSkydomeGradient xml = new()
            {
                Top = Top.ToXML(),
                Middle = Middle.ToXML(),
                Bottom = Bottom.ToXML()
            };

            return xml;
        }

        public override HitResult HitTest(Camera camera, Ray ray)
        {
            return new HitResult(null);
        }
    }
}
