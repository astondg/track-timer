namespace TrackTimer.Converters
{
    using System;
    using System.Windows.Data;
    using TrackTimer.Resources;

    public class SessionIsUploadedToSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool valueAsBool = value as bool? ?? false;
            return valueAsBool ? AppResources.Text_DataSource_SkyDrive : AppResources.Text_DataSource_Local;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}