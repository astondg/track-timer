namespace TrackTimer.Contracts
{
    public interface IAccelerometerReadingChangedEventArgs
    {
        IAccelerometerReading Reading { get; }
    }
}