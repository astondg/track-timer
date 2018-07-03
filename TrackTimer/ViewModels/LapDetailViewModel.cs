namespace TrackTimer.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using TrackTimer.Core.ViewModels;

    public class LapDetailViewModel : BaseViewModel
    {
        private bool lapBelongsToCurrentUser;
        private bool isLeaderboardTime;
        private bool? isOfficial;
        private double projectedLength;
        private string speedUnitText;
        private string lengthUnitText;
        private string userDisplayName;
        private string gpsDeviceName;
        private string lapMinimumLengthBlurb;

        public LapDetailViewModel()
        {
            SectorSplits = new ObservableCollection<Tuple<int,TimeSpan, TimeSpan>>();
        }

        public bool LapBelongsToCurrentUser
        {
            get { return lapBelongsToCurrentUser; }
            set
            {
                if (SetProperty(ref lapBelongsToCurrentUser, value))
                    NotifyPropertyChanged("ShowWeather");
            }
        }

        public bool ShowWeather
        {
            get { return SessionWeather != null && !LapBelongsToCurrentUser; }
        }

        public bool IsLeaderboardTime
        {
            get { return isLeaderboardTime; }
            set { SetProperty(ref isLeaderboardTime, value); }
        }

        public bool? IsOfficial
        {
            get { return isOfficial; }
            set { SetProperty(ref isOfficial, value); }
        }

        public double ProjectedLength
        {
            get { return projectedLength; }
            set { SetProperty(ref projectedLength, value); }
        }

        public string UserDisplayName
        {
            get { return userDisplayName; }
            set { SetProperty(ref userDisplayName, value); }
        }

        public string GpsDeviceName
        {
            get { return gpsDeviceName; }
            set { SetProperty(ref gpsDeviceName, value); }
        }

        public string LapMinimumLengthBlurb
        {
            get { return lapMinimumLengthBlurb; }
            set { SetProperty(ref lapMinimumLengthBlurb, value); }
        }

        public string SpeedUnitText
        {
            get { return speedUnitText; }
            set { SetProperty(ref speedUnitText, value); }
        }

        public string LengthUnitText
        {
            get { return lengthUnitText; }
            set { SetProperty(ref lengthUnitText, value); }
        }

        public LapViewModel Lap { get; set; }

        public WeatherConditionsViewModel SessionWeather { get; set; }

        public ObservableCollection<Tuple<int, TimeSpan, TimeSpan>> SectorSplits { get; private set; }

    }
}