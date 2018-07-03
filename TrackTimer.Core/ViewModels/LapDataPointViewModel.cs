namespace TrackTimer.Core.ViewModels
{
    using System;

    public class LapDataPointViewModel : BaseViewModel
    {
        private bool isEndOfLap;
        private TimeSpan elapsedTime;
        private double? accelerationX;
        private double? accelerationY;
        private double? accelerationZ;
        private double? altitude;
        private double? heading;
        private double? latitude;
        private double? longitude;
        private double? speed;
        private int sectorNumber;

        public bool IsEndOfLap
        {
            get { return isEndOfLap;  }
            set { SetProperty(ref isEndOfLap, value); }
        }
        public TimeSpan ElapsedTime
        {
            get { return elapsedTime; }
            set { SetProperty(ref elapsedTime, value); }
        }
        public double? AccelerationX
        {
            get { return accelerationX; }
            set { SetProperty(ref accelerationX, value); }
        }
        public double? AccelerationY
        {
            get { return accelerationY; }
            set { SetProperty(ref accelerationY, value); }
        }
        public double? AccelerationZ
        {
            get { return accelerationZ; }
            set { SetProperty(ref accelerationZ, value); }
        }
        public double? Altitude
        {
            get { return altitude; }
            set { SetProperty(ref altitude, value); }
        }
        public double? Heading
        {
            get { return heading; }
            set { SetProperty(ref heading, value); }
        }
        public double? Latitude
        {
            get { return latitude; }
            set { SetProperty(ref latitude, value); }
        }
        public double? Longitude
        {
            get { return longitude; }
            set { SetProperty(ref longitude, value); }
        }
        public double? Speed
        {
            get { return speed; }
            set { SetProperty(ref speed, value); }
        }
        public int SectorNumber
        {
            get { return sectorNumber; }
            set { SetProperty(ref sectorNumber, value); }
        }
        public DateTimeOffset Timestamp { get; set; }
    }
}