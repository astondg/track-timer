namespace TrackTimer.Services
{
    using System;
    using TrackTimer.Core.ViewModels;

    public class LapEventArgs : EventArgs
    {
        public LapEventArgs(LapViewModel lap)
        {
            this.Lap = lap;
        }
        public LapViewModel Lap { get; set; }
    }
}