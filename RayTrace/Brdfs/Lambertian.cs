using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace.Brdfs
{
    public class Lambertian : Brdf
    {
        //public Colour Colour { get; set; } = Palette.Copper;    // XXX Should use TextureSolid
        //public Texture Texture { get; set; }

        public Lambertian()
        {
            Name = "Lambertian (BRDF)";
        }

        public Lambertian(XMLBrdf xml)
        {
            Name = xml.Name;
        }

        //public Lambertian(Colour colour) : base()
        //{
        //    Colour = colour;
        //}

        //public Lambertian(Texture texture) : base()
        //{
        //    Texture = texture;
        //}

        // Calculate Colour at point given in hr based on radiance (L) coming from direction source.
        // colour   Colour of surface at bit point.
        // source   Vector pointing outwards from hit point.
        // Li       Colour received from hit point.
        public override Colour GetColour(HitResult hr, Colour colour, SVector source, Colour Li)
        {
            // Calculate wo (L towards viewer)
            /*            SVector up = new(0, 0, 1);
                        float coswo = up.DotProduct(hr.Ray.Normalise());
                        float wo = (float) Math.Acos(coswo);

                        // Calculate wi (L from source)
                        float coswi = up.DotProduct(source.Normalise().Negate());
                        float wi = (float)Math.Acos(coswi);
            */
            Colour Lo = colour / MathF.PI * Li * hr.EntryNormal.DotProduct(source);

            return Lo;
        }

        //public override Colour GetBaseColour()
        //{
        //    // XXX Texture
        //    return Texture.GetColour(new HitResult(null) { u = 0.5f, v = 0.5f });
        //}

        public override XMLBrdf ToXML()
        {
            XMLLambertian xml = new();

            xml.Name = Name;

            return xml;
        }
    }
}
