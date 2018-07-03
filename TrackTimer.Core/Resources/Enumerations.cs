namespace TrackTimer.Core.Resources
{
    public enum BluetoothConnectionStatus
    {
        BluetoothDisabled,
        DeviceNotFound,
        Connected
    }

    public enum VehicleClass
    {
        Standard,
        LightModifications,
        HeavyModifications,
        RacePrepared
    }

    public enum DeviceOrientation
    {
        // Summary:
        //     No orientation is specified.
        None = 0,
        //
        // Summary:
        //     Portrait orientation.
        Portrait = 1,
        //
        // Summary:
        //     Landscape orientation.
        Landscape = 2,
        //
        // Summary:
        //     Portrait orientation.
        PortraitUp = 5,
        //
        // Summary:
        //     Portrait orientation. This orientation is never used.
        PortraitDown = 9,
        //
        // Summary:
        //     Landscape orientation with the top of the page rotated to the left.
        LandscapeLeft = 18,
        //
        // Summary:
        //     Landscape orientation with the top of the page rotated to the right.
        LandscapeRight = 34,
    }

    public enum TrackSessionLocation
    {
        InMemory,
        LocalFile,
        ServerFile,
        ServerWithLocalFile
    }
    
    public enum AuthenticationResult
    {
        NotAuthenticated,
        Authenticated,
        Error
    }

    public enum ActivityType
    {
        SharedTrack = 0,
        RacedAtTrack,
        PersonalBest,
        FastestLap,
        AddedVehicle
    }
}