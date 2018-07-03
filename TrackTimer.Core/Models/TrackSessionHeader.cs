namespace TrackTimer.Core.Models
{
    using System;
    using System.Collections.Generic;
    using TrackTimer.Core.Resources;

    public class TrackSessionHeader
    {
        public long TrackId { get; set; }
        public string TrackName { get; set; }
        public Vehicle Vehicle { get; set; }
        public DeviceOrientation DeviceOrientation { get; set; }
        public string GpsDeviceName { get; set; }
        public bool IsUploaded { get; set; }
        public TimeSpan BestLapTime { get; set; }
        public int NumberOfLaps { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public string Notes { get; set; }
        public WeatherConditions Weather { get; set; }
    }
}