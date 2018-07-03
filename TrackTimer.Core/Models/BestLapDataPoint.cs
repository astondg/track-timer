namespace TrackTimer.Core.Models
{
    public class BestLapDataPoint : LapDataPoint
    {
        public BestLapDataPoint()
        { }
        public BestLapDataPoint(LapDataPoint ldp)
        {
            AccelerationX = ldp.AccelerationX;
            AccelerationY = ldp.AccelerationY;
            AccelerationZ = ldp.AccelerationZ;
            Altitude = ldp.Altitude;
            ElapsedTimeTicks = ldp.ElapsedTimeTicks;
            Heading = ldp.Heading;
            IsEndOfLap = ldp.IsEndOfLap;
            Latitude = ldp.Latitude;
            Longitude = ldp.Longitude;
            SectorNumber = ldp.SectorNumber;
            Speed = ldp.Speed;
            Timestamp = ldp.Timestamp;
        }

        public long id { get; set; }
        public long BestLapId { get; set; }
    }
}