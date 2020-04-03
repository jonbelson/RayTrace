using System;
namespace RayTrace
{
    public class Light : Entity
    {
        public Point Pos { get; set; }
        public float Intensity { get; set; }

        private Colour _colour;
        public Colour Colour { get { return _colour * Intensity; } set { _colour = value; } }

        public Light(Point p, float intensity, Colour c)
        {
            Pos = p;
            Intensity = intensity;
            _colour = c;
        }

        public Light(Point p, float intensity = 1.0f) : this(p, intensity, Palette.White) { }

        public override string ToString()
        {
            return "Light[" + Pos.ToString() + "]," + Intensity + "," + Colour.ToString() + "]";
        }
    }
}
