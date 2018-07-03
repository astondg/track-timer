namespace TrackTimer.Converters
{
    using System;
    using System.Windows;
    using System.Windows.Data;
    
    public class IntToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var visibleValue = parameter as int?;
            var valueAsInt = value as int?;
            return (valueAsInt.HasValue && visibleValue.HasValue && valueAsInt == visibleValue)
                    || (valueAsInt.HasValue && !visibleValue.HasValue && valueAsInt > 0) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}