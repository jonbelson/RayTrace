using System;
using System.ComponentModel;

namespace RayTrace
{
    public class Palette
    {
        public static Colour Red { get { return new Colour(1.0f, 0.0f, 0.0f); } } 
        public static Colour Green { get { return new Colour(0.0f, 1.0f, 0.0f); } }
        public static Colour Blue { get { return new Colour(0.0f, 0.0f, 1.0f); } }
        public static Colour White { get { return new Colour(1.0f, 1.0f, 1.0f); } }
        public static Colour Black { get { return new Colour(0.0f, 0.0f, 0.0f); } }
        public static Colour Grey { get { return new Colour(0.7f, 0.7f, 0.7f); } }
    }

    public class Colour
    {
        [NotifyParentProperty(true)]
        public float Red { get; set; }
        [NotifyParentProperty(true)]
        public float Blue { get; set; }
        [NotifyParentProperty(true)]
        public float Green { get; set; }

        //public float Alpha { get; set; }

        public Colour(System.Windows.Media.Color color) : this(color.ScR, color.ScG, color.ScB)
        {
        }

        public Colour() : this(0.0f, 0.0f, 0.0f)
        {
            //Red = Green = Blue = 0.0f;
        }

        public Colour(float red, float green, float blue)
        {
            Red = red;
            Green = green;
            Blue = blue;
            //Alpha = alpha;
        }

        public Colour(UInt32 rgb)
        {
            byte r = (byte) (rgb >> 16);
            byte g = (byte) (rgb >> 8);
            byte b = (byte) rgb;

            Red = r / 255.0f;
            Green = g / 255.0f;
            Blue = b / 255.0f;
        }

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

        public uint ToRGB()
        {
            byte r = (byte)(Red * 255);
            byte g = (byte)(Green * 255);
            byte b = (byte)(Blue * 255);

            return (uint) (r << 16 | g << 8 | b);
        }

        public override string ToString()
        {
//            return "Colour[" + Red + "," + Green + "," + Blue + "]";
            return "[" + Red + "," + Green + "," + Blue + "]";
        }

        // Blend two Colours in the specified ratio.
        // ratio       Ranges from 0.0 (all col1) to 1.0 (all col2).
        public static Colour Blend(Colour col1, Colour col2, float ratio)
        {
            ratio = Math.Max(ratio, 0.0f);
            ratio = Math.Min(ratio, 1.0f);

            float r = col1.Red * ratio + (1 - ratio) * col2.Red;
            float g = col1.Green * ratio + (1 - ratio) * col2.Green;
            float b = col1.Blue * ratio + (1 - ratio) * col2.Blue;

            return new Colour(r, g, b);
        }

        // Return the average of two Colours.
        public static Colour Average(Colour col1, Colour col2)
        {
            return (col1 + col2).Scale(0.5f);
        }
    }
}
