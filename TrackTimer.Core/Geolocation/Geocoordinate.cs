namespace TrackTimer.Core.Geolocation
{
    using System;

    public class Geocoordinate
    {
        public double Accuracy { get; set; }
        public double? Altitude { get; set; }
        public double? AltitudeAccuracy { get; set; }
        public double? Heading { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public PositionSource PositionSource { get; set; }
        public GeocoordinateSatelliteData SatelliteData { get; set; }
        public double? Speed { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}