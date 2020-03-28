using System;
namespace RayTrace
{
    // Characteristics of a Shape's surface.
    public class Material
    {
        // Phong reflection constants.
        public double Il = 1.0;
        public double k = 0.5;     // Specular reflection coefficient/constant.
        public double ns = 4.0;    // 'Shininess' of surface, 1.0 (matt) - 100 (shiny)

        public Texture Texture;

        public Material() : this(Palette.White) { }

        public Material(Colour c)
        {
            Texture = new TextureSolid(c);
        }

        public Material(Texture texture)
        {
            Texture = texture;
        }

        public virtual Colour GetColour(HitResult hr) { return Texture.GetColour(hr); }
    }
}
