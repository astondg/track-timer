namespace TrackTimer.Core.Geolocation
{
    using System;

    public class PositionChangedEventArgs : EventArgs
    {
        public PositionChangedEventArgs(Geocoordinate position)
        {
            Position = position;
        }

        public PositionChangedEventArgs(Geocoordinate position, TimeSpan elapsedTime, bool isEndOfFinalLap = false)
        {
            Position = position;
            ElapsedTime = elapsedTime;
            IsEndOfFinalLap = isEndOfFinalLap;
        }

        public Geocoordinate Position { get; private set; }
        public TimeSpan ElapsedTime { get; private set; }
        public bool IsEndOfFinalLap { get; private set; }
    }
}