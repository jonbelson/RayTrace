using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    // Simple texture that just returns asingle colour.
    public class TextureSolid : Texture
    {
        public override TextureType Type { get; } = TextureType.Solid;

        public Colour Colour { get; set; }              // Diffuse colour
        //public Colour ColourSpecular { get; set; }    // Colour for specular reflections.

        public TextureSolid() : this(Palette.Grey) { }

        public TextureSolid(Colour colour)
        {
            Name = "Solid";

            Colour = colour;
        }

        public TextureSolid(XMLTextureSolid xmlTextureSolid)
        {
            Colour = new Colour(xmlTextureSolid.Colour);
        }

        public override Colour GetColour(HitResult hr) { return Colour; }

        public override Colour GetBaseColour() { return Colour; }

        public override XMLTexture ToXML()
        {
            XMLTextureSolid xml = new XMLTextureSolid();
            xml.Colour = Colour.ToXML();
            return xml;
        }
    }
}
