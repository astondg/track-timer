namespace TrackTimer.Core.Geolocation
{
    public enum PositionStatus
    {
        // Summary:
        //     Location data is available.
        Ready = 0,
        //
        // Summary:
        //     The location provider is initializing. This is the status if a GPS is the
        //     source of location data and the GPS receiver does not yet have the required
        //     number of satellites in view to obtain an accurate position.
        Initializing = 1,
        //
        // Summary:
        //     No location data is available from any location provider. LocationStatus
        //     will have this value if the application calls GetGeopositionAsync or registers
        //     an event handler for the PositionChanged event, before data is available
        //     from a location sensor. Once data is available LocationStatus transitions
        //     to the Ready state.
        NoData = 2,
        //
        // Summary:
        //     The location provider is disabled. This status indicates that the user has
        //     not granted the application permission to access location.
        Disabled = 3,
        //
        // Summary:
        //     An operation to retrieve location has not yet been initialized. LocationStatus
        //     will have this value if the application has not yet called GetGeopositionAsync
        //     or registered an event handler for the PositionChanged event.
        NotInitialized = 4,
        //
        // Summary:
        //     The Windows Sensor and Location Platform is not available on this version
        //     of Windows.
        NotAvailable = 5,
    }
}