namespace TrackTimer.Extensions
{
    using Microsoft.Phone.Maps.Controls;

    public static class MapExtensions
    {
        public static void SetZoomLevelForTrack(this Map map, double trackLength)
        {
            if (trackLength < 2000)
                map.ZoomLevel = 17;
            else if (trackLength < 4000)
                map.ZoomLevel = 16;
            else if (trackLength < 6000)
                map.ZoomLevel = 15;
            else
                map.ZoomLevel = 14;
        }
    }
}