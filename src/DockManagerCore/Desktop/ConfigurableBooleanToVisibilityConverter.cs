using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DockManagerCore.Desktop
{
    public class ConfigurableBooleanToVisibilityConverter : IValueConverter
    {
        public ConfigurableBooleanToVisibilityConverter()
        {
            // default values
            VisibilityWhenTrue = Visibility.Visible;
            VisibilityWhenFalse = Visibility.Collapsed;
            VisibilityWhenNull = Visibility.Collapsed;
        }

        public Visibility VisibilityWhenTrue { get; set; }

        public Visibility VisibilityWhenFalse { get; set; }

        public Visibility VisibilityWhenNull { get; set; }

        public object Convert(object value_, Type targetType_, object parameter_, CultureInfo culture_)
        {
            bool? boolValue = value_ as bool?;
            if (boolValue == null)
            {
                return VisibilityWhenNull;
            }

            return boolValue.Value ? VisibilityWhenTrue : VisibilityWhenFalse;
        }

        public object ConvertBack(object value_, Type targetType_, object parameter_, CultureInfo culture_)
        {
            throw new NotSupportedException("ConfigurableBooleanToVisibility COnverter only works one way.");
        }
    }
}
