namespace TrackTimer.Core.Extensions
{
    using System.Spatial;
    using TrackTimer.Core.ViewModels;

    public static class LapDataPointViewModelExtensions
    {
        public static GeographyPoint AsGeographyPoint(this LapDataPointViewModel point)
        {
            if (!point.Latitude.HasValue || !point.Longitude.HasValue) return null;
            return GeographyPoint.Create(point.Latitude.Value, point.Longitude.Value, null, point.Heading);
        }
    }
}