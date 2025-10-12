using System;
using System.Globalization;
using System.Windows.Data;

namespace DesktopShortcutManager.Converters
{
    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            // 将参数从字符串转换为对应的枚举值
            string parameterString = parameter.ToString();
            object parameterValue = Enum.Parse(value.GetType(), parameterString);

            // 比较两个枚举值是否相等
            return value.Equals(parameterValue);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value?.Equals(false) == true || parameter == null)
                return Binding.DoNothing;

            // 将参数从字符串转换为对应的枚举值并返回
            string parameterString = parameter.ToString();
            return Enum.Parse(targetType, parameterString);
        }
    }
}