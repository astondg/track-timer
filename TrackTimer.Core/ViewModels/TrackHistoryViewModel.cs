namespace TrackTimer.Core.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;

    public class TrackHistoryViewModel : BaseViewModel
    {
        private TrackSessionViewModel selectedSession;
        public TrackHistoryViewModel()
        {
            this.Sessions = new ObservableCollection<TrackSessionViewModel>();
        }

        public TrackSessionViewModel SelectedSession
        {
            get { return selectedSession; }
            set { SetProperty(ref selectedSession, value); }
        }

        public ObservableCollection<TrackSessionViewModel> Sessions { get; private set; }

    }
}