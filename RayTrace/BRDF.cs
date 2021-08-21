using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    // Base class of Bidirectional Reflectance Distribution Functions.
    public abstract class BRDF
    {
        public string Name { get; set; }

        public abstract Colour GetColour(HitResult hr, SVector source, Colour L);

    }

    public class Lambertian : BRDF
    {
        public Colour Colour { get; set; } = Palette.Copper;

        public Lambertian()
        {
            Name = "Lambertian";
        }

        // Calculate Colour at point given in hr based on radiance (L) coming from direction source.
        public override Colour GetColour(HitResult hr, SVector source, Colour Li)
        {
            // Calculate wo (L towards viewer)
            SVector up = new(0, 0, 1);
            float coswo = up.DotProduct(hr.Ray.Normalise());
            float wo = (float) Math.Acos(coswo);

            // Calculate wi (L from source)
            float coswi = up.DotProduct(source.Normalise().Negate());
            float wi = (float)Math.Acos(coswi);

            Colour Lo = (Colour / Math.PI) * Li * hr.EntryNormal.DotProduct(source);

            return Lo;
        }

    }
}
