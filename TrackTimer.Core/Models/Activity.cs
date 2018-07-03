namespace TrackTimer.Core.Models
{
    using System;

    public class Activity
    {
        public string id { get; set; }
        public string UserId { get; set; }
        public string UserDisplayName { get; set; }
        public int Type { get; set; }
        public string Text { get; set; }
        public string Data { get; set; }
        public long? TrackId { get; set; }
        public long? VehicleId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}