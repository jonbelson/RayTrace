using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    // Simple texture that just return s asingle colour.
    public class TextureSolid : Texture
    {
        private Colour colour;

        public TextureSolid(Colour colour)
        {
            this.colour = colour;
        }

        public override Colour GetColour(HitResult hr) { return colour; }
    }
}
