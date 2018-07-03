namespace TrackTimer.Core.Extensions
{
    using System;
    using System.Spatial;
    using TrackTimer.Core.ViewModels;

    public static class GeoUtmConverter
    {
        private const double pi = 3.14159265358979;
        private const double sm_a = 6378137.0;
        private const double sm_b = 6356752.314;
        private const double sm_EccSquared = 6.69437999013e-03;
        private const double UTMScaleFactor = 0.9996;

        public enum Hemisphere
        {
            Northern = 0,
            Southern = 1
        }

        public static GeometryPoint AsUtmGeometryPoint(this GeographyPoint point)
        {
            double zone = point.GetZone();
            return GeoUTMConverterXY(DegToRad(point.Latitude), DegToRad(point.Longitude), zone);
        }

        public static GeographyPoint AsGeographyPoint(this GeometryPoint point, double zone, Hemisphere Hemi)
        {
            return Hemi == Hemisphere.Northern
                ? UTMXYToLatLon(point.X, point.Y, false, zone)
                : UTMXYToLatLon(point.X, point.Y, true, zone);
        }

        public static GeometryPoint ToUTM(double Latitude, double Longitude)
        {
            double zone = Math.Floor((Longitude + 180.0) / 6) + 1;
            return GeoUTMConverterXY(DegToRad(Latitude), DegToRad(Longitude), zone);
        }

        public static GeographyPoint ToLatLon(double x, double y, double zone, Hemisphere Hemi)
        {
            return Hemi == Hemisphere.Northern
                ? UTMXYToLatLon(x, y, false, zone)
                : UTMXYToLatLon(x, y, true, zone);
        }

        public static GeographyPoint Midpoint(GeographyPoint latlong1, GeographyPoint latlong2, double heading)
        {
            var dLat = DegToRad(latlong2.Latitude - latlong1.Latitude);
            var dLon = DegToRad(latlong2.Longitude - latlong1.Longitude);
            var lat1 = DegToRad(latlong1.Latitude);
            var lon1 = DegToRad(latlong1.Longitude);
            var lat2 = DegToRad(latlong2.Latitude);
            var Bx = Math.Cos(lat2) * Math.Cos(dLon);
            var By = Math.Cos(lat2) * Math.Sin(dLon);
            var lat3 = Math.Atan2(Math.Sin(lat1) + Math.Sin(lat2),
                                  Math.Sqrt((Math.Cos(lat1) + Bx) * (Math.Cos(lat1) + Bx) + By * By));
            var lon3 = lon1 + Math.Atan2(By, Math.Cos(lat1) + Bx);
            return GeographyPoint.Create(RadToDeg(lat3), RadToDeg(lon3), null, heading);

            //double arcLength = HaversineDistance(latlong1, latlong2);
            //double brng = CalculateBearing(latlong1, latlong2);
            //return CalculateCoord(latlong1, brng, arcLength / 2, heading);
        }

        private static double HaversineDistance(GeographyPoint latlong1, GeographyPoint latlong2)
        {
            var lat1 = DegToRad(latlong1.Latitude);
            var lon1 = DegToRad(latlong1.Longitude);
            var lat2 = DegToRad(latlong2.Latitude);
            var lon2 = DegToRad(latlong2.Longitude);
            var dLat = lat2 - lat1;
            var dLon = lon2 - lon1;
            var cordLength = Math.Pow(Math.Sin(dLat / 2), 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dLon / 2), 2);
            var centralAngle = 2 * Math.Atan2(Math.Sqrt(cordLength), Math.Sqrt(1 - cordLength));
            return (sm_b/1000) * centralAngle;
        }

        private static double CalculateBearing(GeographyPoint latlong1, GeographyPoint latlong2)
        {
            var lat1 = DegToRad(latlong1.Latitude);
            var lon1 = DegToRad(latlong1.Longitude);
            var lat2 = DegToRad(latlong2.Latitude);
            var lon2 = DegToRad(latlong2.Longitude);
            var dLon = DegToRad(lon2 - lon1);
            var y = Math.Sin(dLon) * Math.Cos(lat2);
            var x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);
            var brng = (RadToDeg(Math.Atan2(y, x)) + 360) % 360;
            return brng;
        }

        private static GeographyPoint CalculateCoord(GeographyPoint origin, double brng, double arcLength, double heading)
        {
            var lat1 = DegToRad(origin.Latitude);
            var lon1 = DegToRad(origin.Longitude);
            var centralAngle = arcLength / (sm_b / 2);
            var lat2 = Math.Asin(Math.Sin(lat1) * Math.Cos(centralAngle) + Math.Cos(lat1) * Math.Sin(centralAngle) * Math.Cos(DegToRad(brng)));

            var lon2 = lon1 + Math.Atan2(Math.Sin(DegToRad(brng)) * Math.Sin(centralAngle) * Math.Cos(lat1), Math.Cos(centralAngle) - Math.Sin(lat1) * Math.Sin(lat2));
            return GeographyPoint.Create(RadToDeg(lat2), RadToDeg(lon2), null, heading);
        }

        private static double DegToRad(double degrees)
        {
            return (degrees / 180.0 * pi);
        }

        private static double RadToDeg(double radians)
        {
            return (radians / pi * 180.0);
        }

        private static double MetersToFeet(double meters)
        {
            return (meters * 3.28084);
        }

        private static double FeetToMeters(double feet)
        {
            return (feet / 3.28084);
        }

        private static double ArcLengthOfMeridian(double phi)
        {
            double alpha, beta, gamma, delta, epsilon, n;
            double result;

            /* Precalculate n */
            n = (sm_a - sm_b) / (sm_a + sm_b);

            /* Precalculate alpha */
            alpha = ((sm_a + sm_b) / 2.0)
               * (1.0 + (Math.Pow(n, 2.0) / 4.0) + (Math.Pow(n, 4.0) / 64.0));

            /* Precalculate beta */
            beta = (-3.0 * n / 2.0) + (9.0 * Math.Pow(n, 3.0) / 16.0)
               + (-3.0 * Math.Pow(n, 5.0) / 32.0);

            /* Precalculate gamma */
            gamma = (15.0 * Math.Pow(n, 2.0) / 16.0)
                + (-15.0 * Math.Pow(n, 4.0) / 32.0);

            /* Precalculate delta */
            delta = (-35.0 * Math.Pow(n, 3.0) / 48.0)
                + (105.0 * Math.Pow(n, 5.0) / 256.0);

            /* Precalculate epsilon */
            epsilon = (315.0 * Math.Pow(n, 4.0) / 512.0);

            /* Now calculate the sum of the series and return */
            result = alpha
                * (phi + (beta * Math.Sin(2.0 * phi))
                    + (gamma * Math.Sin(4.0 * phi))
                    + (delta * Math.Sin(6.0 * phi))
                    + (epsilon * Math.Sin(8.0 * phi)));

            return result;
        }

        private static double UTMCentralMeridian(double zone)
        {
            return (DegToRad(-183.0 + (zone * 6.0)));
        }

        private static double FootpointLatitude(double y)
        {
            double y_, alpha_, beta_, gamma_, delta_, epsilon_, n;
            double result;

            /* Precalculate n (Eq. 10.18) */
            n = (sm_a - sm_b) / (sm_a + sm_b);

            /* Precalculate alpha_ (Eq. 10.22) */
            /* (Same as alpha in Eq. 10.17) */
            alpha_ = ((sm_a + sm_b) / 2.0)
                * (1 + (Math.Pow(n, 2.0) / 4) + (Math.Pow(n, 4.0) / 64));

            /* Precalculate y_ (Eq. 10.23) */
            y_ = y / alpha_;

            /* Precalculate beta_ (Eq. 10.22) */
            beta_ = (3.0 * n / 2.0) + (-27.0 * Math.Pow(n, 3.0) / 32.0)
                + (269.0 * Math.Pow(n, 5.0) / 512.0);

            /* Precalculate gamma_ (Eq. 10.22) */
            gamma_ = (21.0 * Math.Pow(n, 2.0) / 16.0)
                + (-55.0 * Math.Pow(n, 4.0) / 32.0);

            /* Precalculate delta_ (Eq. 10.22) */
            delta_ = (151.0 * Math.Pow(n, 3.0) / 96.0)
                + (-417.0 * Math.Pow(n, 5.0) / 128.0);

            /* Precalculate epsilon_ (Eq. 10.22) */
            epsilon_ = (1097.0 * Math.Pow(n, 4.0) / 512.0);

            /* Now calculate the sum of the series (Eq. 10.21) */
            result = y_ + (beta_ * Math.Sin(2.0 * y_))
                + (gamma_ * Math.Sin(4.0 * y_))
                + (delta_ * Math.Sin(6.0 * y_))
                + (epsilon_ * Math.Sin(8.0 * y_));

            return result;
        }

        private static double[] MapLatLonToXY(double phi, double lambda, double lambda0)
        {
            double[] xy = new double[2];
            double N, nu2, ep2, t, t2, l;
            double l3coef, l4coef, l5coef, l6coef, l7coef, l8coef;
            double tmp;

            /* Precalculate ep2 */
            ep2 = (Math.Pow(sm_a, 2.0) - Math.Pow(sm_b, 2.0)) / Math.Pow(sm_b, 2.0);
            /* Precalculate nu2 */
            nu2 = ep2 * Math.Pow(Math.Cos(phi), 2.0);
            /* Precalculate N */
            N = Math.Pow(sm_a, 2.0) / (sm_b * Math.Sqrt(1 + nu2));
            /* Precalculate t */
            t = Math.Tan(phi);

            t2 = t * t;
            tmp = (t2 * t2 * t2) - Math.Pow(t, 6.0);

            /* Precalculate l */
            l = lambda - lambda0;

            /* Precalculate coefficients for l**n in the equations below
               so a normal human being can read the expressions for easting
               and northing
               -- l**1 and l**2 have coefficients of 1.0 */

            l3coef = 1.0 - t2 + nu2;

            l4coef = 5.0 - t2 + 9 * nu2 + 4.0 * (nu2 * nu2);

            l5coef = 5.0 - 18.0 * t2 + (t2 * t2) + 14.0 * nu2
                - 58.0 * t2 * nu2;

            l6coef = 61.0 - 58.0 * t2 + (t2 * t2) + 270.0 * nu2
                - 330.0 * t2 * nu2;

            l7coef = 61.0 - 479.0 * t2 + 179.0 * (t2 * t2) - (t2 * t2 * t2);
            l8coef = 1385.0 - 3111.0 * t2 + 543.0 * (t2 * t2) - (t2 * t2 * t2);

            /* Calculate easting (x) */
            xy[0] = N * Math.Cos(phi) * l
                + (N / 6.0 * Math.Pow(Math.Cos(phi), 3.0) * l3coef * Math.Pow(l, 3.0))
                + (N / 120.0 * Math.Pow(Math.Cos(phi), 5.0) * l5coef * Math.Pow(l, 5.0))
                + (N / 5040.0 * Math.Pow(Math.Cos(phi), 7.0) * l7coef * Math.Pow(l, 7.0));

            /* Calculate northing (y) */
            xy[1] = ArcLengthOfMeridian(phi)
                + (t / 2.0 * N * Math.Pow(Math.Cos(phi), 2.0) * Math.Pow(l, 2.0))
                + (t / 24.0 * N * Math.Pow(Math.Cos(phi), 4.0) * l4coef * Math.Pow(l, 4.0))
                + (t / 720.0 * N * Math.Pow(Math.Cos(phi), 6.0) * l6coef * Math.Pow(l, 6.0))
                + (t / 40320.0 * N * Math.Pow(Math.Cos(phi), 8.0) * l8coef * Math.Pow(l, 8.0));


            return xy;
        }

        private static double[] MapXYToLatLon(double x, double y, double lambda0)
        {
            double[] latlon = new double[2];

            double phif, Nf, Nfpow, nuf2, ep2, tf, tf2, tf4, cf;
            double x1frac, x2frac, x3frac, x4frac, x5frac, x6frac, x7frac, x8frac;
            double x2poly, x3poly, x4poly, x5poly, x6poly, x7poly, x8poly;

            /* Get the value of phif, the footpoint latitude. */
            phif = FootpointLatitude(y);

            /* Precalculate ep2 */
            ep2 = (Math.Pow(sm_a, 2.0) - Math.Pow(sm_b, 2.0))
                  / Math.Pow(sm_b, 2.0);
            /* Precalculate cos (phif) */
            cf = Math.Cos(phif);
            /* Precalculate nuf2 */
            nuf2 = ep2 * Math.Pow(cf, 2.0);
            /* Precalculate Nf and initialize Nfpow */
            Nf = Math.Pow(sm_a, 2.0) / (sm_b * Math.Sqrt(1 + nuf2));
            Nfpow = Nf;
            /* Precalculate tf */
            tf = Math.Tan(phif);
            tf2 = tf * tf;
            tf4 = tf2 * tf2;
            /* Precalculate fractional coefficients for x**n in the equations
               below to simplify the expressions for latitude and longitude. */
            x1frac = 1.0 / (Nfpow * cf);

            Nfpow *= Nf;   /* now equals Nf**2) */
            x2frac = tf / (2.0 * Nfpow);

            Nfpow *= Nf;   /* now equals Nf**3) */
            x3frac = 1.0 / (6.0 * Nfpow * cf);

            Nfpow *= Nf;   /* now equals Nf**4) */
            x4frac = tf / (24.0 * Nfpow);

            Nfpow *= Nf;   /* now equals Nf**5) */
            x5frac = 1.0 / (120.0 * Nfpow * cf);

            Nfpow *= Nf;   /* now equals Nf**6) */
            x6frac = tf / (720.0 * Nfpow);

            Nfpow *= Nf;   /* now equals Nf**7) */
            x7frac = 1.0 / (5040.0 * Nfpow * cf);

            Nfpow *= Nf;   /* now equals Nf**8) */
            x8frac = tf / (40320.0 * Nfpow);

            /* Precalculate polynomial coefficients for x**n.
               -- x**1 does not have a polynomial coefficient. */
            x2poly = -1.0 - nuf2;

            x3poly = -1.0 - 2 * tf2 - nuf2;

            x4poly = 5.0 + 3.0 * tf2 + 6.0 * nuf2 - 6.0 * tf2 * nuf2
                - 3.0 * (nuf2 * nuf2) - 9.0 * tf2 * (nuf2 * nuf2);

            x5poly = 5.0 + 28.0 * tf2 + 24.0 * tf4 + 6.0 * nuf2 + 8.0 * tf2 * nuf2;

            x6poly = -61.0 - 90.0 * tf2 - 45.0 * tf4 - 107.0 * nuf2
                + 162.0 * tf2 * nuf2;

            x7poly = -61.0 - 662.0 * tf2 - 1320.0 * tf4 - 720.0 * (tf4 * tf2);

            x8poly = 1385.0 + 3633.0 * tf2 + 4095.0 * tf4 + 1575 * (tf4 * tf2);

            /* Calculate latitude */
            latlon[0] = phif + x2frac * x2poly * (x * x)
                + x4frac * x4poly * Math.Pow(x, 4.0)
                + x6frac * x6poly * Math.Pow(x, 6.0)
                + x8frac * x8poly * Math.Pow(x, 8.0);

            /* Calculate longitude */
            latlon[1] = lambda0 + x1frac * x
                + x3frac * x3poly * Math.Pow(x, 3.0)
                + x5frac * x5poly * Math.Pow(x, 5.0)
                + x7frac * x7poly * Math.Pow(x, 7.0);

            return latlon;
        }

        private static GeometryPoint GeoUTMConverterXY(double lat, double lon, double zone)
        {
            double[] xy = MapLatLonToXY(lat, lon, UTMCentralMeridian(zone));

            xy[0] = xy[0] * UTMScaleFactor + 500000.0;
            xy[1] = xy[1] * UTMScaleFactor;
            if (xy[1] < 0.0)
                xy[1] = xy[1] + 10000000.0;

            return GeometryPoint.Create(xy[0], xy[1]);
        }

        private static GeographyPoint UTMXYToLatLon(double x, double y, bool southhemi, double zone)
        {
            double cmeridian;

            x -= 500000.0;
            x /= UTMScaleFactor;

            /* If in southern hemisphere, adjust y accordingly. */
            if (southhemi)
                y -= 10000000.0;

            y /= UTMScaleFactor;

            cmeridian = UTMCentralMeridian(zone);
            double[] latlon = MapXYToLatLon(x, y, cmeridian);

            return GeographyPoint.Create(RadToDeg(latlon[0]), RadToDeg(latlon[1]));
        }
    }
}