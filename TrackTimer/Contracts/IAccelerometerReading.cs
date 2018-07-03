namespace TrackTimer.Contracts
{
    using System;

    public interface IAccelerometerReading
    {
        double AccelerationX { get; }
        double AccelerationY { get; }
        double AccelerationZ { get; }
        DateTimeOffset Timestamp { get; }
    }
}