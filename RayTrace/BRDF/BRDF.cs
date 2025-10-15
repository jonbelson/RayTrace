using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    // Base class of Bidirectional Reflectance Distribution Functions.
    public abstract class BRDF
    {
        public string Name { get; set; }

        public abstract Colour GetColour(HitResult hr, Colour colour, SVector source, Colour Li);

        public abstract XMLBrdf ToXML();
    }
}
