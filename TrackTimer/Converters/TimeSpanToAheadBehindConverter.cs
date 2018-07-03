namespace TrackTimer.Converters
{
    using System;
    using System.Windows.Data;
    using TrackTimer.Core.Resources;

    public class TimeSpanToAheadBehindConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            TimeSpan? item = value as TimeSpan?;
            if (!item.HasValue) return string.Empty;
            return item.Value.ToString(item.Value >= TimeSpan.Zero ? Constants.SHORT_POSITIVE_TIME_FORMAT : Constants.SHORT_NEGATIVE_TIME_FORMAT, culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            TimeSpan item;
            if (TimeSpan.TryParseExact(value.ToString(), item >= TimeSpan.Zero ? Constants.SHORT_POSITIVE_TIME_FORMAT : Constants.SHORT_NEGATIVE_TIME_FORMAT, culture, out item))
                return item;
            else
                throw new InvalidCastException();
        }
    }
}
