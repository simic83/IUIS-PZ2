using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NetworkService.Converters
{
    public class StringToVisibilityConverter : IValueConverter
    {
        public bool CollapseWhenEmpty { get; set; } = true;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isEmpty = string.IsNullOrWhiteSpace(value as string);
            if (CollapseWhenEmpty)
                return isEmpty ? Visibility.Collapsed : Visibility.Visible;
            return isEmpty ? Visibility.Hidden : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("StringToVisibilityConverter does not support ConvertBack");
        }
    }
}