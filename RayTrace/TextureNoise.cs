using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public TextureNoise() : this(Palette.Green, Palette.Red) { }

        public TextureNoise(Colour col1, Colour col2)
        {
            Name = "Perlin Noise";

            Colour1 = col1;
            Colour2 = col2;
        }

        public override Colour GetColour(HitResult hr)
        {
            double x = hr.u * Scale;
            double y = hr.v * Scale;

            double val = Perlin.Noise(x, y, 0.0);

            Debug.Assert(val >= - noiseRange);
            Debug.Assert(val <= noiseRange);

            //Console.WriteLine(val);

            Colour c = Colour1;

            switch (Function)
            {
                case ColourFunction.EitherOr:
                    c = (val < 0.0) ? Colour1 : Colour2;
                    break;
                case ColourFunction.Blend:
                    {
                        float ratio = (float) ((val + noiseRange) / (2.0 * noiseRange));

                        //Debug.Assert(ratio >= 0.0);
                        //Debug.Assert(ratio <= 1.0);

                        c = Colour.Blend(Colour1, Colour2, ratio);

                        Debug.Assert(c.Red >= 0.0 && c.Red <= 1.0);
                        Debug.Assert(c.Green >= 0.0 && c.Green <= 1.0);
                        Debug.Assert(c.Blue >= 0.0 && c.Blue <= 1.0);
                    }
                    break;
            }

            return c;
        }

    }
}
