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
        public override TextureType Type { get; } = TextureType.NoiseVolume;

        private double noiseRange = Perlin.GetRange();

        public Colour Colour { get; set; }

        public int Scale { get; set; } = 10;

        public TextureNoiseVolume(Colour col)
        {
            Colour = col;
        }

        public override SColour GetSColour(HitResult hr)
        {
            // Step through Shape incremnentally until we hit 'solid' part of noise function.

            // XXX Totally WIP

            Shape space = hr.shape;

            SPoint start = hr.Entry;
            SVector step = hr.Ray.Normalise();
            step = step.Scale(0.1f);

            do
            {
                double val = Perlin.Noise(start.X, start.Y, start.Z);
                if (val >= 0)
                {
                    return Colour.ToSColour();
                }
            } while (false);

            //double x = hr.u * Scale;
            //double y = hr.v * Scale;

            return SPalette.Black;
        }
    }
}
