using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DockManagerCore.Converters
{
    public class BoolToThicknessConverter:IValueConverter
    {
        public Thickness TrueValue { get; set; }
        public Thickness FalseValue { get; set; }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToBoolean(value) ? TrueValue : FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
