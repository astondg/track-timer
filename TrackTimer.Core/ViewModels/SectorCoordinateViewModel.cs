namespace TrackTimer.Core.ViewModels
{
    public class TrackSectorViewModel : BaseViewModel
    {
        private long id;
        private double startLatitude;
        private double startLongitude;
        private double endLatitude;
        private double endLongitude;
        private double heading;
        private bool isFinishLine;
        private int sectorNumber;

        public long Id
        {
            get { return id; }
            set { SetProperty(ref id, value); }
        }
        public double StartLatitude
        {
            get { return startLatitude; }
            set { SetProperty(ref startLatitude, value); }
        }
        public double StartLongitude
        {
            get { return startLongitude; }
            set { SetProperty(ref startLongitude, value); }
        }
        public double EndLatitude
        {
            get { return endLatitude; }
            set { SetProperty(ref endLatitude, value); }
        }
        public double EndLongitude
        {
            get { return endLongitude; }
            set { SetProperty(ref endLongitude, value); }
        }
        public double Heading
        {
            get { return heading;  }
            set { SetProperty(ref heading, value); }
        }
        public bool IsFinishLine
        {
            get { return isFinishLine; }
            set { SetProperty(ref isFinishLine, value); }
        }
        public int SectorNumber
        {
            get { return sectorNumber; }
            set { SetProperty(ref sectorNumber, value); }
        }
    }
}