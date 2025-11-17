using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace.Brdfs
{
    // Base class of Bidirectional Reflectance Distribution Functions.
    public abstract class Brdf
    {
        public string Name { get; set; }

        // Amount of reflected light in direction v.
        protected abstract float F(in SVector v);

        // Probability density function for direction v.
        protected abstract float Pdf(in SVector v);

        // Get random sample direction for this BRDF type.
        public abstract SVector GetSample(HitResult hr, LocalCoordinateSystem lcs, Random rand, out float pdf);

        // Calculate the colour of the hit point based on a light sample from the specified direction.
        public abstract Colour GetColour(HitResult hr, LocalCoordinateSystem lcs, Random rand, Colour colour, SVector source, Colour Li);

        public abstract XMLBrdf ToXML();
    }
}
