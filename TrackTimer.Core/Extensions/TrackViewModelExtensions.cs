namespace TrackTimer.Core.Extensions
{
    using TrackTimer.Core.Resources;
    using TrackTimer.Core.ViewModels;

    public static class TrackViewModelExtensions
    {
        public static double LengthInMetres(this TrackViewModel track)
        {
            return track.IsMetricUnits ? track.Length * 1000 : track.Length * Constants.MILES_TO_KILOMETRES * 1000;
        }
    }
}