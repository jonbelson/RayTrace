using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    public class TextureNoise : Texture
    {
        public TextureNoise()
        {
        }

        public override Colour GetColour(HitResult hr) { return Palette.White; }

    }
}
