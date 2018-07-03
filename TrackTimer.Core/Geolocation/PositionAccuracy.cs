namespace TrackTimer.Core.Geolocation
{
    public enum PositionAccuracy
    {
        // Summary:
        //     Optimize for power, performance, and other cost considerations.
        Default = 0,
        //
        // Summary:
        //     Deliver the most accurate report possible. This includes using services that
        //     might charge money, or consuming higher levels of battery power or connection
        //     bandwidth. An accuracy level of High may degrade system performance and should
        //     be used only when necessary.
        High = 1,
    }
}