namespace TrackTimer.Core.Models
{
    using System;

    public class LapDataPoint
    {
        public long ElapsedTimeTicks { get; set; }
        public double? AccelerationX { get; set; }
        public double? AccelerationY { get; set; }
        public double? AccelerationZ { get; set; }
        public double? Altitude { get; set; }
        public double? Heading { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? Speed { get; set; }
        public int SectorNumber { get; set; }
        public bool IsEndOfLap { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}