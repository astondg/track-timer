namespace TrackTimer.Converters
{
    using System;
    using System.Windows.Data;
    using TrackTimer.Core.Resources;
    using TrackTimer.Resources;

    public class SessionLocationToSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            TrackSessionLocation valueAsLocation = value as TrackSessionLocation? ?? TrackSessionLocation.InMemory;
            switch (valueAsLocation)
            {
                case TrackSessionLocation.LocalFile:
                    return AppResources.Text_DataSource_Local;
                case TrackSessionLocation.ServerFile:
                    return AppResources.Text_DataSource_SkyDrive;
                case TrackSessionLocation.ServerWithLocalFile:
                    return AppResources.Text_DataSource_LocalAndOneDrive;
                case TrackSessionLocation.InMemory:
                default:
                    return AppResources.Text_DataSource_InMemory;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}