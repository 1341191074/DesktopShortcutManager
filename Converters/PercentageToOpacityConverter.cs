// C#: PercentageToOpacityConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;

namespace DesktopShortcutManager.Converters
{
    public class PercentageToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double val)
            {
                return val / 100.0;
            }
            return 1.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double val)
            {
                return val * 100.0;
            }
            return 100.0;
        }
    }
}