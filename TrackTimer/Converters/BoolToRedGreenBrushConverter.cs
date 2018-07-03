namespace TrackTimer.Converters
{
    using System;
    using System.Windows.Data;
    using System.Windows.Media;

    public class BoolToRedGreenBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool valueAsBool = value as bool? ?? false;
            return valueAsBool ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
