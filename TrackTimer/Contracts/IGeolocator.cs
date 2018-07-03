namespace TrackTimer.Contracts
{
    using System;
    using System.Threading.Tasks;
    using TrackTimer.Core.Geolocation;
    using TrackTimer.Services;
    using Windows.Foundation;

    public interface IGeolocator : IDisposable
    {
        PositionAccuracy DesiredAccuracy { get; set; }
        uint? DesiredAccuracyInMeters { get; set; }
        PositionStatus LocationStatus { get; }
        double MovementThreshold { get; set; }
        uint ReportInterval { get; set; }
        string DeviceName { get; }
        event TypedEventHandler<IGeolocator, PositionChangedEventArgs> PositionChanged;
        event TypedEventHandler<IGeolocator, StatusChangedEventArgs> StatusChanged;
        event TypedEventHandler<IGeolocator, GeolocationErrorEventArgs> UnrecoverableError;
        Task<Geocoordinate> GetGeopositionAsync();
        Task<Geocoordinate> GetGeopositionAsync(TimeSpan maximumAge, TimeSpan timeout);
    }
}