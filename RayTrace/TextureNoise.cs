using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    public class TextureNoise : Texture
    {
        private double noiseRange = Perlin.GetRange();

        public enum ColourFunction
        {
            EitherOr, Blend
        };
        
        public Colour Colour1 { get; set; }
        public Colour Colour2 { get; set; }

        public int Scale { get; set; } = 10;
        public ColourFunction Function { get; set; } = ColourFunction.EitherOr;

        public TextureNoise(Colour col1, Colour col2)
        {
            Colour1 = col1;
            Colour2 = col2;
        }

        public override Colour GetColour(HitResult hr)
        {
            double x = hr.u * Scale;
            double y = hr.v * Scale;

            double val = Perlin.Noise(x, y, 0.0);

            //Console.WriteLine(val);

            Colour c = Colour1;

            switch (Function)
            {
                case ColourFunction.EitherOr:
                    c = (val < 0.0) ? Colour1 : Colour2;
                    break;
                case ColourFunction.Blend:
                    val = (val + noiseRange) / (2.0*noiseRange);
                    
                    c = Colour.Blend(Colour1, Colour2, (float) val);
                    break;

            }

            return c;
        }

    }
}
