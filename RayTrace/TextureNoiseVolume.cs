using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    public class TextureNoiseVolume : Texture
    {
        private double noiseRange = Perlin.GetRange();

        public Colour Colour { get; set; }

        public int Scale { get; set; } = 10;

        public TextureNoiseVolume(Colour col)
        {
            Colour = col;
        }

        public override Colour GetColour(HitResult hr)
        {
            // Step through Shape incremnentally until we hit 'solid' part of noise function.

            // XXX Totally WIP

            Shape space = hr.shape;

            Point start = hr.Entry;
            Vector step = hr.Ray.Normalise();
            step = step.Scale(0.1);

            do
            {
                double val = Perlin.Noise(start.X, start.Y, start.Z);
                if (val >= 0)
                {
                    return Colour;
                }
            } while (false);

            //double x = hr.u * Scale;
            //double y = hr.v * Scale;

            return Palette.Black;
        }
    }
}
