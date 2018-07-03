namespace TrackTimer.Converters
{
    using System;
    using System.Windows.Data;
    using System.Windows.Media;

    public class TimeSpanToColourConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // TODO - Figure out how to return the default PhoneForegroundBrush
            TimeSpan? item = value as TimeSpan?;
            if (!item.HasValue) new SolidColorBrush(System.Windows.Media.Colors.Green);
            if (item == TimeSpan.Zero)
                return new SolidColorBrush(System.Windows.Media.Colors.Green);
            else if (item < TimeSpan.Zero)
                return new SolidColorBrush(System.Windows.Media.Colors.Green);
            else
                return new SolidColorBrush(System.Windows.Media.Colors.Red);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
