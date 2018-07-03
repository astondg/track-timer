namespace TrackTimer.Core.Geolocation
{
    using System;
    
    public class StatusChangedEventArgs : EventArgs
    {
        public StatusChangedEventArgs(PositionStatus status)
        {
            Status = status;
        }

        public PositionStatus Status { get; private set; }
    }
}