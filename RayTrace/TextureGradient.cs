using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    class TextureGradient : Texture
    {
        public Colour Colour1 { get; set; }
        public Colour Colour2 { get; set; }

        public enum GradientType
        {
            Vertical, Horizontal, Diagonal, Radial
        }

        public GradientType Type { get; set; } = GradientType.Vertical;

        public TextureGradient() : this(Palette.Blue, Palette.Green) { }

        public TextureGradient(Colour colour1, Colour colour2)
        {
            Name = "Gradient";

            Colour1 = colour1;
            Colour2 = colour2;
        }

        //public TextureGradient(XMLTextureGradient xml)
        //{
        //    Colour1 = new Colour(xml.Colour1);
        //    Colour2 = new Colour(xml.Colour2);
        //      Type = xml.Type;
        //}

        public override Colour GetColour(HitResult hr)
        {
            switch (Type)
            {
                case GradientType.Vertical:
                    return Colour.Blend(Colour1, Colour2, hr.v);

                case GradientType.Horizontal:
                    return Colour.Blend(Colour1, Colour2, hr.u);

                case GradientType.Diagonal:
                    // Find shortest distance from 0,0 to 1,1 and use 'm' as blend.
                    throw new NotImplementedException();

                case GradientType.Radial:
                    Vector v = new Point(0.5, 0.5, 0) - new Point(hr.u, hr.v, 0);
                    double distance = v.Magnitude();
                    return Colour.Blend(Colour1, Colour2, (float) distance);

            }

            return Palette.Grey;
        }

        //public override XMLTexture ToXML()
        //{
        //    XMLTextureGradient xml = new();
        //    xml.Colour1 = Colour1.ToXML();
        //    xml.Colour2 = Colour2.ToXML();
        //    xml.Type = Type;
        //    return xml;
        //}
    }
}
