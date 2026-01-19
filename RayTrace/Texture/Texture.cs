using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace RayTrace
{
    public enum TextureType
    {
        Solid, Perlin, Squares, Gradient, Bitmap, PerTriangle, NoiseVolume
    }

    // Base class of all Textures.
    public abstract class Texture
    {
        public string Name { get; set; }

        public Colour GetColour(HitResult hr) => new Colour(GetSColour(hr)); 
        public abstract SColour GetSColour(HitResult hr);

        public /*abstract*/ virtual Colour GetBaseColour() => Palette.Grey;

        public abstract TextureType Type { get; }

        // Set to true if the source RGB is in sRGB format and needs converting to linear RGB.
        // Set to false if the source RGB is already in linear RGB format or sRGB values are required.
        public bool ConvertToLinearRGB = false;

        public override string ToString()
        {
            return "[" + GetType().Name + "]";
        }

        public virtual /*abstract*/ XMLTexture ToXML() { return null; }
    }
}
