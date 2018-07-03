namespace TrackTimer.Core.Models
{
    using System;
    using System.Collections.Generic;

    public class Track
    {
        public long id { get; set; }
        public string Name { get; set; }
        public string BackgroundImagePath { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Length { get; set; }
        public string Description { get; set; }
        public bool IsOfficial { get; set; }
        public string Filter { get; set; }
        // NumberOfLaps property does not exist in Azure Mobile Services
        public int TotalLaps { get; set; }
        public string CreatedBy { get; set; }
        public string Country { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public IList<BestLap> BestLaps { get; set; }
        public IEnumerable<TrackSector> Sectors { get; set; }
    }
}