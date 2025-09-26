using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NetworkService.Converters
{
    // Konverter za prikaz elemenata kada je vrednost NULL
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value == null ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    // Konverter za prikaz elemenata kada vrednost NIJE NULL
    public class NotNullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value != null ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    // Konverter za pozicioniranje stubića na grafu
    public class InvertedHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double height = (value is double d) ? d : 0.0;

            double baseline = 340.0; // podrazumevano: Y koordinata x-ose u XAML-u
            if (parameter != null && double.TryParse(parameter.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double p))
            {
                baseline = p;
            }

            return Math.Max(0.0, baseline - height); // Logika
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}