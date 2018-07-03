namespace TrackTimer.Core.ViewModels
{
    using System;
    using TrackTimer.Core.Resources;

    public class TrackSessionHeaderViewModel : BaseViewModel
    {
        private long trackId;
        private string trackName;
        private DeviceOrientation deviceOrientation;
        private bool isUploaded;
        private TimeSpan bestLaptTime;
        private int numberOfLaps;
        private DateTimeOffset timeStamp;
        private string localFilePath;
        private string serverFilePath;
        private TrackSessionLocation location;
        private string gpsDeviceName;
        private string notes;

        public long TrackId
        {
            get { return trackId; }
            set { SetProperty(ref trackId, value); }
        }
        public string TrackName
        {
            get { return trackName; }
            set { SetProperty(ref trackName, value); }
        }
        public DeviceOrientation DeviceOrientation
        {
            get { return deviceOrientation; }
            set { SetProperty(ref deviceOrientation, value); }
        }
        public string GpsDeviceName
        {
            get { return gpsDeviceName; }
            set { SetProperty(ref gpsDeviceName, value); }
        }
        public bool IsUploaded
        {
            get { return isUploaded; }
            set { SetProperty(ref isUploaded, value); }
        }
        public TimeSpan BestLapTime
        {
            get { return bestLaptTime; }
            set { SetProperty(ref bestLaptTime, value); }
        }
        public int NumberOfLaps
        {
            get { return numberOfLaps; }
            set { SetProperty(ref numberOfLaps, value); }
        }
        public DateTimeOffset Timestamp
        {
            get { return timeStamp; }
            set { SetProperty(ref timeStamp, value); }
        }
        public string LocalFilePath
        {
            get { return localFilePath; }
            set { SetProperty(ref localFilePath, value); }
        }
        public string ServerFilePath
        {
            get { return serverFilePath; }
            set { SetProperty(ref serverFilePath, value); }
        }
        public TrackSessionLocation Location
        {
            get { return location; }
            set { SetProperty(ref location, value); }
        }
        public string Notes
        {
            get { return notes; }
            set { SetProperty(ref notes, value); }
        }
        public VehicleViewModel Vehicle { get; set; }

        public WeatherConditionsViewModel Weather { get; set; }
    }
}