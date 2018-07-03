namespace TrackTimer.Converters
{
    using System;
    using System.Windows.Data;
    using TrackTimer.Resources;

    public class DoubleToWholeNumberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double? item = value as double?;
            if (!item.HasValue) return AppResources.Text_Default_NoValue;
            return Math.Round(item.Value).ToString(culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}