namespace TrackTimer.Core.Models
{
    using System;
    using System.Collections.Generic;

    public class Lap
    {
        public int LapNumber { get; set; }
        public long StartTicks { get; set; }
        public long EndTicks { get; set; }
        public bool? IsComplete { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public IEnumerable<LapDataPoint> DataPoints { get; set; }
    }
}