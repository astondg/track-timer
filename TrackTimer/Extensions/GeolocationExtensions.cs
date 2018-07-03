namespace TrackTimer.Extensions
{
    using Windows.Devices.Geolocation;

    public static class GeolocationExtensions
    {
        public static Core.Geolocation.Geocoordinate AsTrackTimerModel(this Geocoordinate coordinate)
        {
            return new Core.Geolocation.Geocoordinate
            {
                Accuracy = coordinate.Accuracy,
                Altitude = coordinate.Altitude,
                AltitudeAccuracy = coordinate.AltitudeAccuracy,
                Heading = coordinate.Heading,
                Latitude = coordinate.Latitude,
                Longitude = coordinate.Longitude,
                PositionSource = coordinate.PositionSource.AsTackTimerModel(),
                SatelliteData = coordinate.SatelliteData.AsTrackTimerModel(),
                Speed = coordinate.Speed,
                Timestamp = coordinate.Timestamp
            };
        }

        public static Core.Geolocation.GeocoordinateSatelliteData AsTrackTimerModel(this GeocoordinateSatelliteData satelliteData)
        {
            return new Core.Geolocation.GeocoordinateSatelliteData
            {
                HorizontalDilutionOfPrecision = satelliteData.HorizontalDilutionOfPrecision,
                PositionDilutionOfPrecision = satelliteData.PositionDilutionOfPrecision,
                VerticalDilutionOfPrecision = satelliteData.VerticalDilutionOfPrecision
            };
        }

        public static Core.Geolocation.PositionSource AsTackTimerModel(this PositionSource source)
        {
            return (Core.Geolocation.PositionSource)(int)source;
        }

        public static Core.Geolocation.PositionStatus AsTrackTimerModel(this PositionStatus status)
        {
            return (Core.Geolocation.PositionStatus)(int)status;
        }

        public static Core.Geolocation.PositionAccuracy AsTrackTimerModel(this PositionAccuracy accuracy)
        {
            return (Core.Geolocation.PositionAccuracy)(int)accuracy;
        }
    }
}
