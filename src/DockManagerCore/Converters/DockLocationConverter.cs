using System;
using System.Globalization;
using System.Windows.Data;

namespace DockManagerCore.Converters
{
    [ValueConversion(typeof(DockLocation), typeof(string))]
    public class DockLocationConverter:IValueConverter 
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DockLocation dockLocation = (DockLocation) value;
            switch (dockLocation)
            {
                case DockLocation.TopLeft:
                    return "Top Left";
                case DockLocation.Top:
                    return "Top";
                case DockLocation.TopRight:
                    return "Top Right";
                case DockLocation.Left:
                    return "Left";
                case DockLocation.Center: 
                    return "Tabbed";
                case DockLocation.Right:
                    return "Right";
                case DockLocation.BottomLeft:
                    return "Bottom Left";
                case DockLocation.Bottom:
                    return "Bottom";
                case DockLocation.BottomRight:
                    return "Bottom Right";
                default:
                    return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
