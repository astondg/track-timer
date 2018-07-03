namespace TrackTimer.Converters
{
    using System;
    using System.Windows.Data;
    using TrackTimer.Core.Resources;

    public class WeatherConditionToIconPathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return null;
            var condition = value.ToString().Replace(" ", "").ToLower();
            return string.Format(Constants.WEATHERUNDERGROUND_ICONS_PATH_FORMAT, condition);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}