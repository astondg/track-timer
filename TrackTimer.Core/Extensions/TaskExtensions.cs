namespace TrackTimer.Core.Extensions
{
    using System.Threading.Tasks;

    public static class ThreadingExtensions
    {
        public static async Task<bool> Delay(int time)
        {
            await Task.Delay(time);
            return true;
        }
    }
}