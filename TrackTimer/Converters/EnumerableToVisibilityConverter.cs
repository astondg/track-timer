namespace TrackTimer.Converters
{
    using System;
    using System.Collections;
    using System.Windows;
    using System.Windows.Data;

    public class EnumerableToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool assert = parameter == null || !parameter.ToString().Equals("NEGATE", StringComparison.OrdinalIgnoreCase);
            var enumerableValue = value as ICollection;
            if (value == null) return assert ? Visibility.Collapsed : Visibility.Visible;
            return enumerableValue.Count > 0 && assert ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}