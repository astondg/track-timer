namespace TrackTimer.Converters
{
    using System;
    using System.Windows;
    using System.Windows.Data;

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool assert = parameter == null || !parameter.ToString().Equals("NEGATE", StringComparison.OrdinalIgnoreCase);
            bool valueAsBool = value as bool? ?? false;
            return ((valueAsBool && assert) || (!valueAsBool && !assert)) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool assert = parameter == null || !parameter.ToString().Equals("NEGATE", StringComparison.OrdinalIgnoreCase);
            Visibility? valueAsVisibility = value as Visibility?;
            return assert
                    ? (valueAsVisibility ?? Visibility.Collapsed) == Visibility.Visible
                    : (valueAsVisibility ?? Visibility.Collapsed) == Visibility.Collapsed;
        }
    }
}