using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;

namespace RayTrace
{
    using SColour = Vector4;

    public class Palette
    {
        public static Colour Red { get { return new Colour(1.0f, 0.0f, 0.0f); } } 
        public static Colour Green { get { return new Colour(0.0f, 1.0f, 0.0f); } }
        public static Colour Blue { get { return new Colour(0.0f, 0.0f, 1.0f); } }
        public static Colour White { get { return new Colour(1.0f, 1.0f, 1.0f); } }
        public static Colour Black { get { return new Colour(0.0f, 0.0f, 0.0f); } }
        public static Colour Grey { get { return new Colour(0.7f, 0.7f, 0.7f); } }
        public static Colour Copper { get { return new Colour(0.82f, 0.475f, 0.23f); } }
        public static Colour MetallicGold { get { return new Colour(212/255f, 175/255f, 55/255f); } }
        public static Colour MetallicSilver { get { return new Colour(170/255f, 169 / 255f, 173 / 255f); } }
        public static Colour Iron { get { return new Colour(161 / 255f, 157 / 255f, 148 / 255f); } }
    }

    public class Colour : INotifyPropertyChanged
    {
        private float red, green, blue;

        public float Red { get { return red; } set { red = value; Notify("Red"); } }
        public float Blue { get { return blue; } set { blue = value; Notify("Blue"); } }
        public float Green { get { return green; } set { green = value; Notify("Green"); } }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void Notify(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public Colour(System.Windows.Media.Color color) : this(color.R/255f, color.G/255f, color.B/255f)
        {
        }

        public Colour() : this(0.0f, 0.0f, 0.0f) { }

        public Colour(float red, float green, float blue)
        {
            Red = red;
            Green = green;
            Blue = blue;
        }

        public Colour(uint rgb)
        {
            byte r = (byte) (rgb >> 16);
            byte g = (byte) (rgb >> 8);
            byte b = (byte) rgb;

            Red = r / 255.0f;
            Green = g / 255.0f;
            Blue = b / 255.0f;
        }

        public Colour(XMLColour xmlColour) => (Red, Green, Blue) = (xmlColour.Red, xmlColour.Green, xmlColour.Blue);

        public static Colour operator+(Colour c1, Colour c2)
        {
            return new Colour(c1.Red + c2.Red, c1.Green + c2.Green, c1.Blue + c2.Blue);
        }

        public static Colour operator-(Colour c1, Colour c2)
        {
            return new Colour(c1.Red - c2.Red, c1.Green - c2.Green, c1.Blue - c2.Blue);
        }

        public static Colour operator*(Colour c1, Colour c2)
        {
            return new Colour(c1.Red * c2.Red, c1.Green * c2.Green, c1.Blue * c2.Blue);
        }

        public static Colour operator*(Colour c, float scale)
        {
            return new Colour(c.Red * scale, c.Green * scale, c.Blue * scale);
        }

        public static Colour operator*(Colour c, double scale) => new Colour(c.Red * (float) scale, c.Green * (float) scale, c.Blue * (float) scale);

        public static Colour operator/(Colour c, double scale) => new Colour(c.Red / (float)scale, c.Green / (float)scale, c.Blue / (float)scale);

        public Colour Add(Colour c)
        {
            return new Colour(Red + c.Red, Green + c.Green, Blue + c.Blue);
        }

        public Colour Subtract(Colour c)
        {
            return new Colour(Red - c.Red, Green - c.Green, Blue - c.Blue);
        }

        // Multiply this Colour by another, e.g. a red light striking a blue surface.
        public Colour Multiply(Colour c)
        {
            return new Colour(Red*c.Red, Green*c.Green, Blue*c.Blue);
        }

        public Colour Scale(float scale)
        {
            return new Colour(Red*scale, Green*scale, Blue*scale);
        }

        public void Clamp()
        {
            Red = Math.Min(1.0f, Math.Max(Red, 0.0f));
            Green = Math.Min(1.0f, Math.Max(Green, 0.0f));
            Blue = Math.Min(1.0f, Math.Max(Blue, 0.0f));
            //Alpha = Math.Min(1.0f, Math.Max(Alpha, 1.0f));
        }

        public Colour Exposure(float exp)
        {
            Colour c = new Colour();
            c.Red = 1.0f - (float) Math.Exp((float) (-Red*exp));
            c.Green = 1.0f - (float)Math.Exp((float)(-Green * exp));
            c.Blue = 1.0f - (float)Math.Exp((float)(-Blue * exp));

            return c;
        }

        // Normalise to range 0.0 - 1.0 by desaturating.
        public void Normalise()
        {
            float max = (Red > Green) ? Red : Green;
            max = (max > Blue) ? max : Blue;

            if (max > 1.0f)
            {
                Red /= max;
                Green /= Green;
                Blue /= Blue;
            }
        }

        // Return true if Colours are 'similar'.
        public bool IsSimilar(Colour c)
        {
            const float MinDiff = 0.1f;
            //Vector v1 = new Vector(Red, Green, Blue);
            //Vector v2 = new Vector(c.Red, c.Green, c.Blue);
            //return v1.Subtract(v2).MagnitudeSquared() < MinDiff * MinDiff; 

            float diffRed = Red - c.Red;
            float diffGreen = Green - c.Green;
            float diffBlue = Blue - c.Blue;

            float diffSquared = diffRed * diffRed + diffGreen * diffGreen + diffBlue * diffBlue;

            return diffSquared < MinDiff * MinDiff;
        }

        // Return gamma-corrected version of this Colour.
        public Colour ApplyGamma(float gamma)
        {
            double gammaRed = Math.Pow(Red, gamma);
            double gammaGreen = Math.Pow(Green, gamma);
            double gammaBlue = Math.Pow(Blue, gamma);

            return new Colour((float)gammaRed, (float)gammaGreen, (float)gammaBlue);
        }

        public uint ToRGB(float gamma=1/2.2f)
        {
            double gammaRed = Math.Pow(Red, gamma);
            double gammaGreen = Math.Pow(Green, gamma);
            double gammaBlue = Math.Pow(Blue, gamma);

           // Debug.Assert(gammaRed <= 1.0 && gammaGreen <= 1.0 && gammaBlue <= 1.0);

            gammaRed = Math.Max(0.0, Math.Min(1.0, gammaRed));
            gammaGreen = Math.Max(0.0, Math.Min(1.0, gammaGreen));
            gammaBlue = Math.Max(0.0, Math.Min(1.0, gammaBlue));

            byte r = (byte)(gammaRed * 255);
            byte g = (byte)(gammaGreen * 255);
            byte b = (byte)(gammaBlue * 255);

            return (uint) (r << 16 | g << 8 | b);
        }

        public override string ToString()
        {
//            return "Colour[" + Red + "," + Green + "," + Blue + "]";
            return "[" + Red + "," + Green + "," + Blue + "]";
        }

        // Select one of two colours
        // ratio       Return col1 if ratio < 0.5, else col2.
        public static Colour StepBlend(Colour col1, Colour col2, float ratio)
        {
            ratio = Math.Max(ratio, 0.0f);
            ratio = Math.Min(ratio, 1.0f);

            return (ratio < 0.5) ? col1 : col2;
        }

        // Blend two Colours in the specified ratio.
        // ratio       Ranges from 0.0 (all col1) to 1.0 (all col2).
        public static Colour LinearBlend(Colour col1, Colour col2, float ratio)
        {
            ratio = Math.Max(ratio, 0.0f);
            ratio = Math.Min(ratio, 1.0f);

            float r = col2.Red * ratio + (1 - ratio) * col1.Red;
            float g = col2.Green * ratio + (1 - ratio) * col1.Green;
            float b = col2.Blue * ratio + (1 - ratio) * col1.Blue;

            return new Colour(r, g, b);
        }

        // From https://en.wikipedia.org/wiki/Smoothstep
        private static float SmoothStep(float edge0, float edge1, float x)
        {
            // Scale, bias and saturate x to 0..1 range
            x = Math.Clamp((x - edge0) / (edge1 - edge0), 0.0f, 1.0f);

            // Evaluate polynomial
            return x * x * (3 - 2 * x);
        }

        // Perlin's variation.
        private static float SmootherStep(float edge0, float edge1, float x)
        {
            // Scale, bias and saturate x to 0..1 range
            x = Math.Clamp((x - edge0) / (edge1 - edge0), 0.0f, 1.0f);

            // Evaluate polynomial
            return x * x * x * (x * (x * 6 - 15) + 10);
        }

        public static Colour SmoothBlend(Colour col1, Colour col2, float ratio)
        {
//            ratio = SmoothStep(0.0f, 1.0f, ratio);
            ratio = SmootherStep(0.0f, 1.0f, ratio);

            float r = col2.Red * ratio + (1 - ratio) * col1.Red;
            float g = col2.Green * ratio + (1 - ratio) * col1.Green;
            float b = col2.Blue * ratio + (1 - ratio) * col1.Blue;

/*
            float r = SmoothStep(col1.Red, col2.Red, ratio);
            float g = SmoothStep(col1.Green, col2.Green, ratio);
            float b = SmoothStep(col1.Blue, col2.Blue, ratio);
*/
            return new Colour(r, g, b);
        }

        // Return the average of two Colours.
        public static Colour Average(Colour col1, Colour col2)
        {
            return (col1 + col2).Scale(0.5f);
        }

        public XMLColour ToXML()
        {
            return new XMLColour() { Red = Red, Green = Green, Blue = Blue };
        }

        public SColour ToVector4()
        {
            return new SColour(Red, Green, Blue, 0);
        }
    }
}
