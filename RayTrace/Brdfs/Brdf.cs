using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

using RayTrace;

namespace RayTrace.Brdfs
{
    internal class BrdfSample
    {
        public SVector sample;             // Direction to sample from.
        public bool isDiffuse = true;      // True for diffuse lobe, else specular lobe.
        public float diffusePdf = 1.0f;    // PDF for diffuse direction.
        public float specularPdf = 1.0f;   // PDF for specular sample.
    }

    // Base class of Bidirectional Reflectance Distribution Functions.
    public abstract class Brdf
    {
        public string Name { get; set; }

        // Amount of reflected light in direction v.
        protected abstract float F(in SVector v);

        // Probability density function for direction v.
        protected abstract float Pdf(in SVector v);

        // Get random sample direction for this BRDF type.
        public abstract SVector GetSample(HitResult hr, LocalCoordinateSystem lcs, Random rand, SColour colour, out float pdf);

        public abstract SColour GetBrdfReflectance(HitResult hr, LocalCoordinateSystem lcs, Random rand, SColour colour, SVector source);// => RayTrace.SPalette.Magenta;

        // Calculate the colour of the hit point based on a light sample from the specified direction.
        public abstract SColour GetColour(HitResult hr, LocalCoordinateSystem lcs, Random rand, SColour colour, SVector source, SColour Li);

        public abstract XMLBrdf ToXML();
    }
}
