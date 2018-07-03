namespace TrackTimer.Core.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using TrackTimer.Core.Resources;

    public class LapViewModel : BaseViewModel
    {
        private DateTimeOffset timestamp;
        private TimeSpan startElapsedTime;
        private TimeSpan endElapsedTime;
        private TimeSpan differenceToBest;
        private double maximumSpeed;
        private int sectorNumber;
        private int lapNumber;
        private bool isIncomplete;

        /// <summary>
        /// The Date and Time the lap was started
        /// </summary>
        /// <returns></returns>
        public DateTimeOffset Timestamp
        {
            get { return timestamp;}
            set { SetProperty(ref timestamp, value); }
        }

        /// <summary>
        /// The elapsed time at the start of the Lap
        /// </summary>
        /// <returns></returns>
        public TimeSpan StartElapsedTime
        {
            get { return startElapsedTime; }
            set
            {
                SetProperty(ref startElapsedTime, value);
                NotifyPropertyChanged("LapTime");
            }
        }

        /// <summary>
        /// The elapsed time at the end of the lap
        /// </summary>
        /// <returns></returns>
        public TimeSpan EndElapsedTime
        {
            get { return endElapsedTime; }
            set
            {
                SetProperty(ref endElapsedTime, value);
                NotifyPropertyChanged("LapTime");
            }
        }

        /// <summary>
        /// The difference between this lap time and the best lap time
        /// </summary>
        /// <returns></returns>
        public TimeSpan DifferenceToBest
        {
            get { return differenceToBest; }
            set
            {
                SetProperty(ref differenceToBest, value);
            }
        }

        public TimeSpan LapTime
        {
            get { return EndElapsedTime - StartElapsedTime; }
        }

        /// <summary>
        /// The maximum speed achieved on the lap
        /// </summary>
        /// <returns></returns>
        public double MaximumSpeed
        {
            get { return maximumSpeed; }
            set { SetProperty(ref maximumSpeed, value); }
        }

        public int SectorNumber
        {
            get { return sectorNumber; }
            set { SetProperty(ref sectorNumber, value); }
        }

        public int LapNumber
        {
            get { return lapNumber; }
            set { SetProperty(ref lapNumber, value); }
        }

        public ObservableCollection<LapDataPointViewModel> DataPoints { get; set; }

        public int TotalLaps { get; set; }

        public bool IsComplete
        {
            get { return isIncomplete; }
            set { SetProperty(ref isIncomplete, value); }
        }
    }
}