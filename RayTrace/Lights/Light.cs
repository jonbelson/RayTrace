using System;
using System.ComponentModel;

namespace RayTrace
{
    public abstract class Light : Entity
    {
        private float intensity;         // 'Brightness' of light.
        private Colour colour;          // Base colour
        private int radius = 0;

        public float Intensity {
            get { return intensity; }
            set { if (intensity != value) { intensity = value; Notify("Intensity"); } }
        }

        public Colour Colour {
            get => colour;
            set { colour = value; colour.PropertyChanged += Colour_PropertyChanged; Notify("Colour"); }
        }

        public Colour Output { get { return Colour * Intensity; }  }    // Combined colour and intensity.

        // Effective radius of point source, as used in soft shadow calculations.
        public int Radius {
            get => radius;
            set { if (value != radius) { radius = value; Notify("Radius"); } }
        }

        public Light(float intensity, Colour c)
        {
            Intensity = intensity;
            Colour = c;

            Colour.PropertyChanged += Colour_PropertyChanged;
        }

        private void Colour_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Notify("Colour");
        }

        public Light(float intensity = 1.0f) : this(intensity, Palette.White) { }

        public Light(XMLLight xmlLight)
        {
            Intensity = xmlLight.Intensity;
            Colour = new Colour(xmlLight.Colour);
            Radius = xmlLight.Radius;

            Colour.PropertyChanged += Colour_PropertyChanged;
        }

        public override string ToString()
        {
            return "Light[" + Intensity + "," + Colour.ToString() + "]";
        }

        public abstract SVector GetDirection(Camera camera, SPoint p);
        public abstract float GetAttenuation(Camera camera, SPoint p);
        public abstract Colour GetIntensityAtPoint(Camera camera, SPoint p);

        protected override void AsXMLImpl(XMLEntity xml)
        {
            base.AsXMLImpl(xml);

            if (xml is XMLLight xmlLight)
            {
                xmlLight.Intensity = Intensity;
                xmlLight.Colour = Colour.ToXML();
                xmlLight.Radius = Radius;
            }
        }

        public override XMLEntity ToXML()
        {
            XMLLight xml = new XMLLight();
            AsXMLImpl(xml);
            return xml;
        }
    }
}
