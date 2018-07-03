namespace TrackTimer.Contracts
{
    using System.Threading.Tasks;

    public interface IGeolocatorFactory
    {
        Task<IGeolocator> GetGeolocator();
    }
}