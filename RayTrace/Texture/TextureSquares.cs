using System;

namespace RayTrace
{
    public class TextureSquares : Texture
    {
        public override TextureType Type { get; } = TextureType.Squares;

        public Colour Colour1 { get; set; }
        public Colour Colour2 { get; set; }

        public int Scale { get; set; } = 20;

        public TextureSquares() : this(Palette.Blue, Palette.White) { }

        public TextureSquares(Colour colour1, Colour colour2)
        {
            Name = "Squares";

            Colour1 = colour1;
            Colour2 = colour2;
        }

        public TextureSquares(XMLTextureSquares xml)
        {
            Colour1 = new Colour(xml.Colour1);
            Colour2 = new Colour(xml.Colour2);
            Scale = xml.Scale;
        }

        public override Colour GetColour(HitResult hr)
        {
            int x = (int)(hr.u * Scale);
            int y = (int)(hr.v * Scale);

            if (x % 2 == 1)
            {
                return y % 2 == 1 ? Colour1 : Colour2;
            }
            else
            {
                return y % 2 == 0 ? Colour1 : Colour2;
            }
        }

        public override Colour GetBaseColour() { return Colour.LinearBlend(Colour1, Colour2, 0.5f); }

        public override XMLTexture ToXML()
        {
            XMLTextureSquares xml = new XMLTextureSquares();
            xml.Colour1 = Colour1.ToXML();
            xml.Colour2 = Colour2.ToXML();
            xml.Scale = Scale;
            return xml;
        }
    }
}
