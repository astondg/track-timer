namespace TrackTimer.Core.Extensions
{
    using System;
    using System.Spatial;
    using TrackTimer.Core.ViewModels;

    public static class GeographyExtensions
    {
        private const double R = 6371; // km
        public static double GetZone(this GeographyPoint point)
        {
            return Math.Floor((point.Longitude + 180.0) / 6) + 1;
        }

        public static LapDataPointViewModel AsLapDataPointViewModel(this GeographyPoint point)
        {
            return new LapDataPointViewModel
            {
                Latitude = point.Latitude,
                Longitude = point.Longitude,
                Heading = point.M
            };
        }

        public static GeographyPoint[] ConvertToLine(this GeographyPoint point, double lineLength)
        {
            double dR = lineLength / 2 / R;
            double headingPlus90Degrees = Radians((point.M.Value + 90) % 360);
            double headingMinus90Degrees = Radians((point.M.Value - 90) % 360);
            double latitude = Radians(point.Latitude);
            double longitude = Radians(point.Longitude);
            double pointPlusLatitude = Math.Asin(Math.Sin(latitude) * Math.Cos(dR) + Math.Cos(latitude) * Math.Sin(dR) * Math.Cos(headingPlus90Degrees));
            double pointPlusLongitude = longitude + Math.Atan2(Math.Sin(headingPlus90Degrees) * Math.Sin(dR) * Math.Cos(latitude), Math.Cos(dR) - Math.Sin(latitude) * Math.Sin(pointPlusLatitude));
            double pointMinusLatitude = Math.Asin(Math.Sin(latitude) * Math.Cos(dR) + Math.Cos(latitude) * Math.Sin(dR) * Math.Cos(headingMinus90Degrees));
            double pointMinusLongitude = longitude + Math.Atan2(Math.Sin(headingMinus90Degrees) * Math.Sin(dR) * Math.Cos(latitude), Math.Cos(dR) - Math.Sin(latitude) * Math.Sin(pointMinusLatitude));
            
            return new[]
                {
                    GeographyPoint.Create(Degrees(pointPlusLatitude), Degrees(pointPlusLongitude)),
                    GeographyPoint.Create(Degrees(pointMinusLatitude), Degrees(pointMinusLongitude))
                };
        }

        public static double Distance(this GeographyPoint thisPoint, GeographyPoint otherPoint)
        {
            double sLat1 = Math.Sin(Radians(thisPoint.Latitude));
            double sLat2 = Math.Sin(Radians(otherPoint.Latitude));
            double cLat1 = Math.Cos(Radians(thisPoint.Latitude));
            double cLat2 = Math.Cos(Radians(otherPoint.Latitude));
            double cLon = Math.Cos(Radians(thisPoint.Longitude) - Radians(otherPoint.Longitude));
            double cosD = sLat1 * sLat2 + cLat1 * cLat2 * cLon;
            double d = Math.Acos(cosD);
            double dist = R * d;

            return dist;
        }

        private static double Radians(double x)
        {
            return x * Math.PI / 180;
        }

        private static double Degrees(double x)
        {
            return x * 180 / Math.PI;
        }
    }
}