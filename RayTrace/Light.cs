using System;
using System.ComponentModel;

namespace RayTrace
{
    public abstract class Light : Entity
    {
        public float Intensity { get; set; }        // 'Brightness' of light.
        [NotifyParentProperty(true)]
        public Colour Colour { get; set; }          // Base colour

        public Colour Output { get { return Colour * Intensity; }  }    // Combined colour and intensity.

        // Effective radius of point source, as used in soft shadow calculations.
        public int Radius { get; set; } = 0;

        public Light(float intensity, Colour c)
        {
            Intensity = intensity;
            Colour = c;
        }

        public Light(float intensity = 1.0f) : this(intensity, Palette.White) { }

        public override string ToString()
        {
            return "Light[" + Intensity + "," + Colour.ToString() + "]";
        }

        public abstract Vector GetDirection(Camera camera, Point p);
        public abstract float GetAttenuation(Camera camera, Point p);
        public abstract Colour GetIntensityAtPoint(Camera camera, Point p);
    }
}
