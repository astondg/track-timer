namespace TrackTimer.Core.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using TrackTimer.Core.Resources;

    public class TrackViewModel : BaseViewModel
    {
        private long id;
        private string name;
        private double length;
        private string description;
        private double latitude;
        private double longitude;
        private int totalLaps;
        private double maximumSpeed;
        private string backgroundImagePath;
        private bool isLocal;
        private string fileNameWhenLoaded;
        private string country;
        private DateTimeOffset timestamp;
        private BestLapViewModel bestLap;
        private TrackHistoryViewModel history;
        private TrackSectorViewModel selectedSector;

        public TrackViewModel()
        {
            Leaderboard = new ObservableCollection<BestLapViewModel>();
        }
        public bool IsMetricUnits { get; set; }

        public long Id
        {
            get { return id; }
            set { SetProperty(ref id, value); }
        }
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }
        public double Length
        {
            get { return length; }
            set { SetProperty(ref length, value); }
        }
        public string Description
        {
            get { return description; }
            set { SetProperty(ref description, value); }
        }
        public double Latitude
        {
            get { return latitude; }
            set { SetProperty(ref latitude, value); }
        }
        public double Longitude
        {
            get { return longitude; }
            set { SetProperty(ref longitude, value); }
        }
        public BestLapViewModel BestLap
        {
            get { return bestLap; }
            set
            {
                SetProperty(ref bestLap, value);
                NotifyPropertyChanged("BestLapTimeDisplay");
            }
        }
        public string BestLapTimeDisplay
        {
            get { return bestLap == null ? "00:00.000" : bestLap.EndElapsedTime.ToString(Constants.LONG_POSITIVE_TIME_FORMAT); }
        }
        public int TotalLaps
        {
            get { return totalLaps; }
            set { SetProperty(ref totalLaps, value); }
        }
        public double MaximumSpeed
        {
            get { return maximumSpeed; }
            set { SetProperty(ref maximumSpeed, value); }
        }
        public string BackgroundImagePath
        {
            get { return backgroundImagePath; }
            set { SetProperty(ref backgroundImagePath, value); }
        }
        public bool IsLocal
        {
            get { return isLocal; }
            set { SetProperty(ref isLocal, value); }
        }
        public string FileNameWhenLoaded
        {
            get { return fileNameWhenLoaded; }
            set { SetProperty(ref fileNameWhenLoaded, value); }
        }
        public string Country
        {
            get { return country; }
            set { SetProperty(ref country, value); }
        }
        public DateTimeOffset Timestamp
        {
            get { return timestamp; }
            set { SetProperty(ref timestamp, value); }
        }
        public TrackHistoryViewModel History
        {
            get { return history; }
            set { SetProperty(ref history, value); }
        }
        public TrackSectorViewModel SelectedSector
        {
            get { return selectedSector; }
            set { SetProperty(ref selectedSector, value); }
        }
        public ObservableCollection<TrackSectorViewModel> Sectors { get; set; }
        public ObservableCollection<LapDataPointViewModel> CornerApexCoordinates { get; set; }
        public ObservableCollection<BestLapViewModel> Leaderboard { get; private set; }
    }
}