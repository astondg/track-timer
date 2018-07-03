namespace TrackTimer.Core.Models
{
    using System;

    public class Vehicle
    {
        public long id { get; set; }
        public string UserName { get; set; }
        public Guid Key { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public int Class { get; set; }
    }
}