namespace TrackTimer.Core.Models
{
    public class TrackSector
    {
        public long id { get; set; }
        public int Number { get; set; }
        public bool IsFinishLine { get; set; }
        public double Heading { get; set; }
        public double StartLatitude { get; set; }
        public double StartLongitude { get; set; }
        public double EndLatitude { get; set; }
        public double EndLongitude { get; set; }
        public long TrackId { get; set; }
    }
}