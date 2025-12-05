using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    class TextureGradient : Texture, INotifyPropertyChanged
    {
        private void Notify([CallerMemberName] String propName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        private void Colour_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Notify(e.PropertyName);
        }

        public event PropertyChangedEventHandler PropertyChanged;


        public override TextureType Type { get; } = TextureType.Gradient;

        public Colour Colour1
        {
            get;
            set
            {
                if (field != value)
                {
                    field?.PropertyChanged -= Colour_PropertyChanged;

                    field = value;

                    field?.PropertyChanged += Colour_PropertyChanged;

                    Notify();
                }
            }
        }
        public Colour Colour2
        {
            get;
            set
            {
                if (field != value)
                {
                    field?.PropertyChanged -= Colour_PropertyChanged;

                    field = value;

                    field?.PropertyChanged += Colour_PropertyChanged;

                    Notify();
                }
            }
        }
        public enum GradientType
        {
            Vertical, Horizontal, Diagonal, Radial
        }

        public enum BlendType
        {
            Linear, Smooth, Step
        }

        public GradientType Gradient { get; set; } = GradientType.Vertical;

        public BlendType Blend { get; set; } = TextureGradient.BlendType.Smooth;

        //
        // Constructor.
        //
        public TextureGradient() : this(Palette.Blue, Palette.Green) { }

        //
        // Constructor.
        //
        public TextureGradient(Colour colour1, Colour colour2)
        {
            Name = "Gradient";

            Colour1 = colour1;
            Colour2 = colour2;
        }

        //public TextureGradient(XMLTextureGradient xml)
        //{
        //    Colour1 = new Colour(xml.Colour1);
        //    Colour2 = new Colour(xml.Colour2);
        //      Type = xml.Type;
        //}

        public delegate Colour Blender(Colour c1, Colour c2, float ratio);

        public override Colour GetColour(HitResult hr)
        {
            Blender handler = null;

            switch (Blend)
            {
                case BlendType.Linear: handler = Colour.LinearBlend;    break;
                case BlendType.Smooth: handler = Colour.SmoothBlend; break;
                case BlendType.Step: handler = Colour.StepBlend; break;
            }

            switch (Gradient)
            {
                case GradientType.Vertical:
                    return handler?.Invoke(Colour1, Colour2, hr.v);

                case GradientType.Horizontal:
                    return handler?.Invoke(Colour1, Colour2, hr.u);

                case GradientType.Diagonal:
                    // Find shortest distance from 0,0 to 1,1 and use 'm' as blend.
                    throw new NotImplementedException();

                case GradientType.Radial:
                    Vector v = new Point(0.5f, 0.5f, 0) - new Point(hr.u, hr.v, 0);
                    double distance = v.Magnitude();
                    return handler?.Invoke(Colour1, Colour2, (float) distance);

            }

            return Palette.Grey;
        }

        public override Colour GetBaseColour()
        {
            return Colour.SmoothBlend(Colour1, Colour2, 0.5f);

        }

        //public override XMLTexture ToXML()
        //{
        //    XMLTextureGradient xml = new();
        //    xml.Colour1 = Colour1.ToXML();
        //    xml.Colour2 = Colour2.ToXML();
        //    xml.Type = Type;
        //    return xml;
        //}
    }
}
