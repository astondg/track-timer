namespace TrackTimer.Converters
{
    using System;
    using System.Windows.Data;
    using TrackTimer.Resources;

    class BoolToToggleTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (value as bool?) ?? false ? AppResources.Text_Toggle_On : AppResources.Text_Toggle_Off;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string stringValue = value as string;
            return stringValue == AppResources.Text_Toggle_On ? true : false;
        }
    }
}