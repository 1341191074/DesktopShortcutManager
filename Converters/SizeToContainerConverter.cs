using System;
using System.Globalization;
using System.Windows.Data;

namespace DesktopShortcutManager.Converters
{
    public class SizeToContainerConverter : IValueConverter
    {
        /// <summary>
        /// Converts an icon size (e.g., 32) to a larger container size (e.g., 62).
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double size)
            {
                // We add a fixed margin to the size to get the container dimension.
                // e.g., Icon size 32 -> Container size 32 + 30 = 62
                return size + 30;
            }
            // Return a default value if conversion fails.
            return 62;
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