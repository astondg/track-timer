namespace TrackTimer.Services
{
    using System;

    public class GeolocationErrorEventArgs : EventArgs
    {
        public Exception Exception { get; set; }
        public string DeviceName { get; set; }
    }
}