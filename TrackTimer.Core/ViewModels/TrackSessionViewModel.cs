namespace TrackTimer.Core.ViewModels
{
    using System.Collections.ObjectModel;
    using System.Windows.Input;

    public class TrackSessionViewModel : TrackSessionHeaderViewModel
    {
        private LapViewModel selectedLap;
        public TrackViewModel Track { get; set; }
        public LapViewModel SelectedLap
        {
            get { return selectedLap; }
            set { SetProperty(ref selectedLap, value); }
        }
        public ObservableCollection<LapViewModel> Laps { get; set; }
    }
}