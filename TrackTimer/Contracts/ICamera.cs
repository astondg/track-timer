namespace TrackTimer.Contracts
{
    using System.Threading.Tasks;

    public interface ICamera
    {
        Task<bool> PowerOn();
        Task<bool> StartRecording();
        Task<bool> StopRecording();
        Task<bool> PowerOff();
    }
}