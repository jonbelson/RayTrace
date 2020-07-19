using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace RayTrace
{
    // Converter class to convert floating point RGB values to a Color.
    class RgbConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Count() != 3)
            {
                return Binding.DoNothing;
            }

            var red = System.Convert.ToSingle(values[0]);
            var green = System.Convert.ToSingle(values[1]);
            var blue = System.Convert.ToSingle(values[2]);

            if (red < 0.0f || red > 1.0f) return Binding.DoNothing;
            if (green < 0.0f || green > 1.0f) return Binding.DoNothing;
            if (blue < 0.0f || blue > 1.0f) return Binding.DoNothing;

            var color = System.Windows.Media.Color.FromRgb((Byte) (red*255.0), (Byte) (green*255.0), (Byte) (blue*255.0f));
            return new SolidColorBrush(color);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
