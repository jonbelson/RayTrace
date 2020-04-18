using System;
namespace RayTrace
{
    public class Light : Entity
    {
        public Point Pos { get; set; }

        public float Intensity { get; set; }        // 'Brightness' of light.
        public Colour Colour { get; set; }          // Base colour

        public Colour Output { get { return Colour * Intensity; }  }    // Combined colour and intensity.

        public Light(Point p, float intensity, Colour c)
        {
            Pos = p;
            Intensity = intensity;
            Colour = c;
        }

        public Light(Point p, float intensity = 1.0f) : this(p, intensity, Palette.White) { }

        public override string ToString()
        {
            return "Light[" + Pos.ToString() + "]," + Intensity + "," + Colour.ToString() + "]";
        }
    }
}
