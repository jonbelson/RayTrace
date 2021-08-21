using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;

namespace RayTrace
{
    // From https://social.msdn.microsoft.com/Forums/en-US/549eeb7c-8df7-4a6c-a264-91f06ca75293/debug-wpf-binding
    public class DebugConverter : IValueConverter
    {
        public static DebugConverter Instance = new DebugConverter();
        private DebugConverter() { }

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //Debugger.Break();
            return value; //Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //Debugger.Break();
            return value; //Binding.DoNothing;
        }

        #endregion
    }

    public class DebugExtension : MarkupExtension
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return DebugConverter.Instance;
        }
    }
}
