namespace TrackTimer.ViewModels
{
    using System.Collections.ObjectModel;
    using TrackTimer.Core.ViewModels;

    public class TimingStoppedViewModel : BaseViewModel
    {
        public bool IsSuccessful { get; set; }
        public string CompletionMessage { get; set; }
        public string GpsDeviceName { get; set; }
        public ObservableCollection<LapViewModel> Laps { get; set; }
        public ObservableCollection<TrackSectorViewModel> GeneratedTrackSectors { get; set; }
    }
}