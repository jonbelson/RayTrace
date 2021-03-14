using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    // Base class of all Textures.
    abstract public class Texture
    {
        //public abstract Colour GetColour(float u, float v);

        public abstract Colour GetColour(HitResult hr);

        public override string ToString()
        {
            return "[" + GetType().Name + "]";
        }

        public virtual /*abstract*/ XMLTexture ToXML() { return null; }
    }
}
