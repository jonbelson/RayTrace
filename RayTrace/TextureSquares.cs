using System;
namespace RayTrace
{
    public class TextureSquares : Material
    {
        public Colour Colour1 { get; set; }
        public Colour Colour2 { get; set; }

        public TextureSquares() : this(Palette.Blue, Palette.White) { }
        public TextureSquares(Colour colour1, Colour colour2)
        {
            Colour1 = colour1;
            Colour2 = colour2;
        }

        public override Colour GetColour(HitResult hr)
        {
            int x = (int)(hr.u * 20);
            int y = (int)(hr.v * 20);

            if (x % 2 == 1)
            {
                if (y % 2 == 1) return Colour1; else return Colour2;
            }
            else
            {
                if (y % 2 == 0) return Colour1; else return Colour2;
            }

            //return Palette.Grey;
        }
    }
}
