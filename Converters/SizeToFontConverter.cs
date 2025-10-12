using System;
using System.Globalization;
using System.Windows.Data;

namespace DesktopShortcutManager.Converters
{
    public class SizeToFontConverter : IValueConverter
    {
        /// <summary>
        /// Converts an icon size (e.g., 32) to an appropriate font size (e.g., 12).
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double size)
            {
                // A simple formula to scale font size with icon size.
                // e.g., Icon size 32 -> Font size 32 / 4 + 4 = 12
                // e.g., Icon size 64 -> Font size 64 / 4 + 4 = 20
                return size / 4 + 4;
            }
            // Return a default value if conversion fails.
            return 12;
        }

        /// <summary>
        /// This conversion is not needed for our one-way binding.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}