namespace TrackTimer.Converters
{
    using System;
    using System.Windows.Data;
    using TrackTimer.Core.Resources;

    public class TimeSpanToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            TimeSpan? item = value as TimeSpan?;
            if (!item.HasValue) return string.Empty;
            return item.Value.ToString(Constants.LONG_POSITIVE_TIME_FORMAT, culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            TimeSpan item;
            if (TimeSpan.TryParse(value.ToString(), culture, out item))
                return item;
            else
                throw new InvalidCastException();
        }
    }
}