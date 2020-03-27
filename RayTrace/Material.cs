using System;
namespace RayTrace
{
    public class Material
    {
        // Phong reflection constants.
        public double Il = 1.0;
        public double k = 0.5;     // Specular reflection coefficient/constant.
        public double ns = 4.0;    // 'Shininess' of surface, 1.0 (matt) - 100 (shiny)

        readonly Colour colour;

        public Material() : this(Palette.White) { }

        public Material(Colour c)
        {
            colour = c;
        }

        public virtual Colour GetColour(HitResult hr) { return colour; }
    }
}
