namespace TrackTimer.Core.Models
{
    using System;
    using System.Collections.Generic;

    public class BestLap
    {
        public long id { get; set; }
        public long LapTimeTicks { get; set; }
        public string UserName { get; set; }
        public string UserDisplayName { get; set; }
        public string VerificationCode { get; set; }
        public string GpsDeviceName { get; set; }
        public string WeatherCondition { get; set; }
        public double? AmbientTemperature { get; set; }
        public bool IsPublic { get; set; }        
        public long VehicleId { get; set; }
        public int VehicleClass { get; set; }
        public Vehicle Vehicle { get; set; }
        public long TrackId { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public bool IsUnofficial { get; set; }
        public string DataPointsCollection { get; set; }
        public IEnumerable<LapDataPoint> DataPoints { get; set; }
    }
}