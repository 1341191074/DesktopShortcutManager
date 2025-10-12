using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DesktopShortcutManager.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isVisible = value is bool b && b;

            // 检查转换器参数是否为 "Invert"，用于反转逻辑
            if (parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase))
            {
                isVisible = !isVisible;
            }

            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 在我们的应用中，不需要从Visibility转换回bool，所以不实现
            throw new NotImplementedException();
        }
    }
}