using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    public enum TextureType
    {
        Solid, Perlin, Squares, Gradient, Bitmap, PerTriangle, NoiseVolume
    }

    // Base class of all Textures.
    abstract public class Texture
    {
        //public abstract Colour GetColour(float u, float v);

        public string Name { get; set; }

        public abstract Colour GetColour(HitResult hr);

        public /*abstract*/ virtual Colour GetBaseColour() => Palette.Grey;

        public abstract TextureType Type { get; }

        public override string ToString()
        {
            return "[" + GetType().Name + "]";
        }

        public virtual /*abstract*/ XMLTexture ToXML() { return null; }
    }
}
