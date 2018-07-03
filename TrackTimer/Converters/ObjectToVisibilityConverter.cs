namespace TrackTimer.Converters
{
    using System;
    using System.Windows;
    using System.Windows.Data;

    public class ObjectToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool assert = parameter == null || !parameter.ToString().Equals("NEGATE", StringComparison.OrdinalIgnoreCase);
            bool valueAsBool = value != null;
            return (valueAsBool && assert) || (!valueAsBool && !assert) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}