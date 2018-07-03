namespace TrackTimer.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    public class DateTimeOffsetToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            DateTimeOffset? item = value as DateTimeOffset?;
            if (!item.HasValue) return parameter != null ? parameter.ToString() : string.Empty;
            return item.Value.ToString("g", culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string valueAsString = value.ToString();
            DateTimeOffset item;
            if (DateTimeOffset.TryParse(valueAsString, culture, DateTimeStyles.None, out item))
                return item;
            else
                throw new InvalidCastException();
        }
    }
}