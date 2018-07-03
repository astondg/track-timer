namespace TrackTimer.Contracts
{
    using Windows.Foundation;

    public interface IAccelerometer
    {
        event TypedEventHandler<IAccelerometer, IAccelerometerReadingChangedEventArgs> ReadingChanged;
    }
}