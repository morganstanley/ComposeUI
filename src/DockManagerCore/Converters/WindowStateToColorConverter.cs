using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace DockManagerCore.Converters
{
    public class WindowStateToBrushConverter:IValueConverter
    {
        public Brush NormalBrush { get; set; }
        public Brush MaximizedBrush { get; set; }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((WindowState)value)
            { 
                case WindowState.Maximized:
                    return MaximizedBrush; 
                default: 
                    return NormalBrush;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
