namespace TrackTimer.Extensions
{
    using System.Spatial;
    using TrackTimer.Core.Extensions;
    using TrackTimer.Core.ViewModels;

    public static class SpatialExtensions
    {
        public static bool IsCompletedByLapDataPoints(this TrackSectorViewModel sector, LapDataPointViewModel startPoint, LapDataPointViewModel endPoint)
        {
            var sectorLineStartPoint = GeoUtmConverter.ToUTM(sector.StartLatitude, sector.StartLongitude);
            var sectorLineEndPoint = GeoUtmConverter.ToUTM(sector.EndLatitude, sector.EndLongitude);
            var dataLineStartPoint = GeoUtmConverter.ToUTM(startPoint.Latitude.Value, startPoint.Longitude.Value);
            var dataLineEndPoint = GeoUtmConverter.ToUTM(endPoint.Latitude.Value, endPoint.Longitude.Value);

            return IsIntersect(sectorLineStartPoint, sectorLineEndPoint, dataLineStartPoint, dataLineEndPoint);
        }

        private static bool CCW(GeometryPoint p1, GeometryPoint p2, GeometryPoint p3) {
          var a = p1.Y;
          var b = p1.X; 
          var c = p2.Y;
          var d = p2.X;
          var e = p3.Y;
          var f = p3.X;
          return (f - b) * (c - a) > (d - b) * (e - a);
        }

        private static bool IsIntersect(GeometryPoint p1, GeometryPoint p2, GeometryPoint p3, GeometryPoint p4) {
          return (CCW(p1, p3, p4) != CCW(p2, p3, p4)) && (CCW(p1, p2, p3) != CCW(p1, p2, p4));
        }
    }
}