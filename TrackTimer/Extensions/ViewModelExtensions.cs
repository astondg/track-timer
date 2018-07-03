namespace TrackTimer.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using TrackTimer.Core.Extensions;
    using TrackTimer.Core.Models;
    using TrackTimer.Core.Resources;
    using TrackTimer.Core.ViewModels;

    public static class ViewModelExtensions
    {
        #region ToModel

        public static BestLap AsModel(this BestLapViewModel lapViewModel, VehicleViewModel vehicle, bool isMetricUnits, bool isPublic, string userName, long trackId, IEnumerable<TrackSectorViewModel> sectors)
        {
            DateTimeOffset utcLapTimestamp = lapViewModel.Timestamp.ToUniversalTime();
            var hashAlgorithm = new HMACSHA256(Convert.FromBase64String(Constants.APP_LAPVERIFICATION_HASHKEY));
            string verificationText = string.Format("{0}{1}{2}{3}{4}{5}", Constants.APP_LAPVERIFICATION_PREFIX, trackId, vehicle.Key.ToString().ToUpperInvariant(), lapViewModel.LapTime.Ticks, utcLapTimestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffK"), userName);
            string verificationCode = Convert.ToBase64String(hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(verificationText)));

            var model = new BestLap
            {
                LapTimeTicks = lapViewModel.LapTime.Ticks,
                Timestamp = utcLapTimestamp,
                VehicleId = vehicle.Id,
                VehicleClass = (int)vehicle.Class,
                Vehicle = vehicle.AsModel(),
                VerificationCode = verificationCode,
                TrackId = trackId,
                IsPublic = isPublic,
                UserName = userName,
                IsUnofficial = lapViewModel.IsUnofficial,
                GpsDeviceName = lapViewModel.GpsDeviceName,
                WeatherCondition = lapViewModel.WeatherCondition,
                AmbientTemperature = lapViewModel.AmbientTemperature
            };

            var dataPoints = new List<LapDataPoint>();
            var geographicDataPoints = lapViewModel.DataPoints.Where(dp => dp != null && dp.Latitude.HasValue && dp.Longitude.HasValue);
            foreach (var sector in sectors)
            {
                LapDataPointViewModel lastDataPoint = null;
                LapDataPointViewModel previousDataPoint = null;
                foreach (var dataPoint in geographicDataPoints.SkipWhile(dp => dp.SectorNumber != sector.SectorNumber))
                {
                    if (dataPoint.SectorNumber != sector.SectorNumber) break;
                    lastDataPoint = dataPoint;
                    if (previousDataPoint != null && (dataPoint.ElapsedTime - previousDataPoint.ElapsedTime < TimeSpan.FromMilliseconds(900d)))
                        continue;
                    previousDataPoint = dataPoint;
                    dataPoints.Add(dataPoint.AsBestLapDataPoint(isMetricUnits));
                }
                // Last data point used for split times
                dataPoints.Add(lastDataPoint.AsBestLapDataPoint(isMetricUnits));
            }
            model.DataPoints = dataPoints;
            return model;
        }

        public static bool RecreateVerificationCode(this BestLap bestLap, string userName, long newTrackId)
        {
            DateTimeOffset utcLapTimestamp = bestLap.Timestamp.ToUniversalTime();
            var hashAlgorithm = new HMACSHA256(Convert.FromBase64String(Constants.APP_LAPVERIFICATION_HASHKEY));
            string verificationText = string.Format("{0}{1}{2}{3}{4}{5}", Constants.APP_LAPVERIFICATION_PREFIX, bestLap.TrackId, bestLap.Vehicle.Key.ToString().ToUpperInvariant(), bestLap.LapTimeTicks, utcLapTimestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffK"), userName);
            string verificationCode = Convert.ToBase64String(hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(verificationText)));
            if (verificationCode != bestLap.VerificationCode) return false;
            bestLap.TrackId = newTrackId;
            verificationText = string.Format("{0}{1}{2}{3}{4}{5}", Constants.APP_LAPVERIFICATION_PREFIX, bestLap.TrackId, bestLap.Vehicle.Key.ToString().ToUpperInvariant(), bestLap.LapTimeTicks, utcLapTimestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffK"), userName);
            verificationCode = Convert.ToBase64String(hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(verificationText)));
            bestLap.VerificationCode = verificationCode;
            return true;
        }

        #endregion
    }
}