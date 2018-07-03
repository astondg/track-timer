namespace TrackTimer.Core.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Spatial;
    using Newtonsoft.Json.Linq;
    using TrackTimer.Core.Models;
    using TrackTimer.Core.Resources;
    using TrackTimer.Core.ViewModels;

    public static class ViewModelExtensions
    {
        #region ToModel

        public static Track AsModel(this TrackViewModel trackViewModel, bool isMetricUnits)
        {
            return new Track
            {
                BackgroundImagePath = trackViewModel.BackgroundImagePath,
                Description = trackViewModel.Description,
                id = trackViewModel.Id,
                Latitude = trackViewModel.Latitude,
                Longitude = trackViewModel.Longitude,
                Length = isMetricUnits
                            ? trackViewModel.Length
                            : trackViewModel.Length * Constants.MILES_TO_KILOMETRES,
                Name = trackViewModel.Name,
                Sectors = trackViewModel.Sectors.Select(s => s.AsModel()),
                TotalLaps = trackViewModel.TotalLaps,
                Country = trackViewModel.Country,
                Timestamp = trackViewModel.Timestamp
            };
        }

        public static TrackSector AsModel(this TrackSectorViewModel sectorCoordinateViewModel)
        {
            return new TrackSector
            {
                id = sectorCoordinateViewModel.Id,
                EndLatitude = sectorCoordinateViewModel.EndLatitude,
                EndLongitude = sectorCoordinateViewModel.EndLongitude,
                Heading = sectorCoordinateViewModel.Heading,
                IsFinishLine = sectorCoordinateViewModel.IsFinishLine,
                Number = sectorCoordinateViewModel.SectorNumber,
                StartLatitude = sectorCoordinateViewModel.StartLatitude,
                StartLongitude = sectorCoordinateViewModel.StartLongitude
            };
        }

        public static TrackSession AsModel(this TrackSessionViewModel trackSession, bool isMetricUnits)
        {
            return new TrackSession
            {
                IsUploaded = false,
                NumberOfLaps = trackSession.Laps.Count,
                BestLapTime = trackSession.BestLapTime,
                Laps = trackSession.Laps.Select(l => l.AsModel(isMetricUnits)),
                Timestamp = trackSession.Timestamp.ToUniversalTime(),
                Vehicle = trackSession.Vehicle.AsModel(),
                TrackId = trackSession.Track.Id,
                TrackName = trackSession.Track.Name,
                DeviceOrientation = trackSession.DeviceOrientation,
                GpsDeviceName = trackSession.GpsDeviceName,
                Notes = trackSession.Notes,
                Weather = trackSession.Weather.AsModel(isMetricUnits)
            };
        }

        public static Lap AsModel(this LapViewModel lapViewModel, bool isMetricUnits)
        {
            return new Lap
            {
                LapNumber = lapViewModel.LapNumber,
                DataPoints = lapViewModel.DataPoints.Select(p => p.AsLapDataPoint(isMetricUnits)),
                StartTicks = lapViewModel.StartElapsedTime.Ticks,
                EndTicks = lapViewModel.EndElapsedTime.Ticks,
                Timestamp = lapViewModel.Timestamp.ToUniversalTime(),
                IsComplete = lapViewModel.IsComplete
            };
        }

        public static LapDataPoint AsLapDataPoint(this LapDataPointViewModel lapDataPointViewModel, bool isMetricUnits)
        {
            return new LapDataPoint
            {
                AccelerationX = lapDataPointViewModel.AccelerationX,
                AccelerationY = lapDataPointViewModel.AccelerationY,
                AccelerationZ = lapDataPointViewModel.AccelerationZ,
                Altitude = lapDataPointViewModel.Altitude,
                ElapsedTimeTicks = lapDataPointViewModel.ElapsedTime.Ticks,
                Heading = lapDataPointViewModel.Heading,
                IsEndOfLap = lapDataPointViewModel.IsEndOfLap,
                Latitude = lapDataPointViewModel.Latitude,
                Longitude = lapDataPointViewModel.Longitude,
                SectorNumber = lapDataPointViewModel.SectorNumber,
                Speed = isMetricUnits
                            ? lapDataPointViewModel.Speed / Constants.KMH_TO_METRES_PER_SECOND
                            : lapDataPointViewModel.Speed / Constants.KMH_TO_METRES_PER_SECOND * Constants.MILES_TO_KILOMETRES,
                Timestamp = lapDataPointViewModel.Timestamp.ToUniversalTime()
            };
        }

        public static BestLapDataPoint AsBestLapDataPoint(this LapDataPointViewModel lapDataPointViewModel, bool isMetricUnits)
        {
            return new BestLapDataPoint
            {
                AccelerationX = lapDataPointViewModel.AccelerationX,
                AccelerationY = lapDataPointViewModel.AccelerationY,
                AccelerationZ = lapDataPointViewModel.AccelerationZ,
                Altitude = lapDataPointViewModel.Altitude,
                ElapsedTimeTicks = lapDataPointViewModel.ElapsedTime.Ticks,
                Heading = lapDataPointViewModel.Heading,
                IsEndOfLap = lapDataPointViewModel.IsEndOfLap,
                Latitude = lapDataPointViewModel.Latitude,
                Longitude = lapDataPointViewModel.Longitude,
                SectorNumber = lapDataPointViewModel.SectorNumber,
                Speed = isMetricUnits
                            ? lapDataPointViewModel.Speed / Constants.KMH_TO_METRES_PER_SECOND
                            : lapDataPointViewModel.Speed / Constants.KMH_TO_METRES_PER_SECOND * Constants.MILES_TO_KILOMETRES,
                Timestamp = lapDataPointViewModel.Timestamp.ToUniversalTime()
            };
        }

        public static Vehicle AsModel(this VehicleViewModel vehicle)
        {
            if (vehicle == null) return null;

            return new Vehicle
            {
                id = vehicle.Id,
                Key = vehicle.Key,
                Make = vehicle.Make,
                Model = vehicle.Model,
                Class = (int)vehicle.Class
            };
        }

        public static WeatherConditions AsModel(this WeatherConditionsViewModel weather, bool isMetricUnits)
        {
            if (weather == null) return null;

            return new WeatherConditions
            {
                Condition = weather.Condition,
                Temperature = isMetricUnits ? weather.Temperature : ((5 * (weather.Temperature - 32)) / 9),
                WindDegrees = weather.WindDirection,
                WindSpeed = isMetricUnits
                            ? weather.WindSpeed / Constants.KMH_TO_METRES_PER_SECOND
                            : weather.WindSpeed / Constants.KMH_TO_METRES_PER_SECOND * Constants.MILES_TO_KILOMETRES,
                PreviousHourPrecipitation = isMetricUnits
                                                ? weather.PreviousHourPrecipitation
                                                : weather.PreviousHourPrecipitation * Constants.MILLIMETRES_TO_INCHES,
                TotalDayPrecipitation = isMetricUnits
                                        ? weather.TotalDayPrecipitation
                                        : weather.TotalDayPrecipitation * Constants.MILLIMETRES_TO_INCHES
            };
        }

        public static Activity AsModel(this ActivityViewModel activity)
        {
            return new Activity
            {
                id = activity.Id,
                CreatedAt = activity.CreatedAt,
                Data = activity.Data,
                Text = activity.Text,
                TrackId = activity.TrackId,
                Type = (int)activity.Type,
                UserDisplayName = activity.UserDisplayName,
                UserId = activity.UserId,
                VehicleId = activity.VehicleId
            };
        }

        public static Friend AsModel(this FriendViewModel friend)
        {
            return new Friend
            {
                id = friend.Id,
                UserId = friend.UserId,
                FirstName = friend.FirstName,
                LastName = friend.LastName,
                IsConfirmed = friend.IsConfirmed,
                CurrentUserInitiatedFriendship = friend.CurrentUserInitiatedFriendship,
                LastActivityTime = friend.LastActivityTime
            };
        }

        public static Dictionary<int, TrackSectorViewModel> ToTrackSectors(this IList<LapDataPointViewModel> lapDataPoints)
        {
            if (lapDataPoints.Count() < 3) return new Dictionary<int, TrackSectorViewModel>();
            int lastDataPointInFirstSectorIndex = (lapDataPoints.Count() / 3) - 1;
            int lastDataPointInSecondSectorIndex = lastDataPointInFirstSectorIndex * 2;
            LapDataPointViewModel lastDataPointInFirstSector = null;
            LapDataPointViewModel lastDataPointInSecondSector = null;
            LapDataPointViewModel lastDataPointInFinalSector = null;

            for (int i = 0 + 1; i < lapDataPoints.Count(); i++)
            {
                var dataPoint = lapDataPoints[i];
                if (i <= lastDataPointInFirstSectorIndex)
                {
                    if (dataPoint.Latitude.HasValue && dataPoint.Longitude.HasValue && dataPoint.Heading.HasValue)
                        lastDataPointInFirstSector = dataPoint;
                }
                else if (i <= lastDataPointInSecondSectorIndex)
                {
                    if (dataPoint.Latitude.HasValue && dataPoint.Longitude.HasValue && dataPoint.Heading.HasValue)
                        lastDataPointInSecondSector = dataPoint;
                    dataPoint.SectorNumber = 2;
                }
                else
                {
                    if (dataPoint.Latitude.HasValue && dataPoint.Longitude.HasValue && dataPoint.Heading.HasValue)
                        lastDataPointInFinalSector = dataPoint;
                    dataPoint.SectorNumber = 3;
                }
            }

            if (lastDataPointInFirstSector == null || lastDataPointInSecondSector == null || lastDataPointInFinalSector == null)
                return new Dictionary<int, TrackSectorViewModel>();

            var firstSector = lastDataPointInFirstSector.ToTrackSector(1);
            var secondSector = lastDataPointInSecondSector.ToTrackSector(2);
            var finalSector = lastDataPointInFinalSector.ToTrackSector(3, true);

            return new Dictionary<int, TrackSectorViewModel> { { 1, firstSector }, { 2, secondSector }, { 3, finalSector } };
        }

        public static TrackSectorViewModel ToTrackSector(this LapDataPointViewModel lapDataPoint, int sectorNumber, bool isFinishLine = false)
        {
            var sectorPoint = GeographyPoint.Create(lapDataPoint.Latitude.Value, lapDataPoint.Longitude.Value, lapDataPoint.Altitude, lapDataPoint.Heading);
            var sectorLine = sectorPoint.ConvertToLine(0.03);
            return new TrackSectorViewModel
            {
                SectorNumber = sectorNumber,
                StartLatitude = sectorLine[0].Latitude,
                StartLongitude = sectorLine[0].Longitude,
                EndLatitude = sectorLine[1].Latitude,
                EndLongitude = sectorLine[1].Longitude,
                Heading = lapDataPoint.Heading.Value,
                IsFinishLine = isFinishLine
            };
        }

        #endregion

        #region ToViewModel

        public static TrackViewModel AsViewModel(this Track track, Guid currentVehicleKey, bool isMetricUnits, bool isLocal = false, string fileNameWhenLoaded = null)
        {
            var bestLap = track.BestLaps == null
                            ? null
                            : track.BestLaps.SingleOrDefault(l => l.Vehicle.Key == currentVehicleKey);

            return CreateTrackViewModelFromTrack(track, isMetricUnits, isLocal, bestLap, fileNameWhenLoaded);
        }

        public static TrackViewModel AsViewModel(this Track track, bool isMetricUnits, bool isLocal = false)
        {
            var bestLap = track.BestLaps == null
                            ? null
                            : track.BestLaps.MaxBy(l => l.Timestamp);

            return CreateTrackViewModelFromTrack(track, isMetricUnits, isLocal, bestLap, null);
        }

        public static TrackSectorViewModel AsViewModel(this TrackSector sector)
        {
            return new TrackSectorViewModel
            {
                Id = sector.id,
                SectorNumber = sector.Number,
                StartLatitude = sector.StartLatitude,
                StartLongitude = sector.StartLongitude,
                EndLatitude = sector.EndLatitude,
                EndLongitude = sector.EndLongitude,
                Heading = sector.Heading,
                IsFinishLine = sector.IsFinishLine
            };
        }

        public static BestLapViewModel AsViewModel(this BestLap bestLap, bool isMetricUnits)
        {
            var lapViewModel = new BestLapViewModel
            {
                Id = bestLap.id,
                IsPublic = bestLap.IsPublic,
                TrackId = bestLap.TrackId,
                UserName = bestLap.UserName,
                UserDisplayName = bestLap.UserDisplayName,
                VehicleId = bestLap.VehicleId,
                Vehicle = bestLap.Vehicle != null
                            ? bestLap.Vehicle.AsViewModel()
                            : null,
                VerificationCode = bestLap.VerificationCode,
                StartElapsedTime = new TimeSpan(0),
                EndElapsedTime = new TimeSpan(bestLap.LapTimeTicks),
                DataPoints = new ObservableCollection<LapDataPointViewModel>(),
                Timestamp = bestLap.Timestamp.ToLocalTime(),
                IsUnofficial = bestLap.IsUnofficial,
                GpsDeviceName = bestLap.GpsDeviceName,
                WeatherCondition = bestLap.WeatherCondition,
                AmbientTemperature = bestLap.AmbientTemperature
            };

            double maximumSpeed = 0;
            foreach (var dataPoint in bestLap.DataPoints)
            {
                double? speedInUserUnits = isMetricUnits
                                            ? dataPoint.Speed * Constants.KMH_TO_METRES_PER_SECOND
                                            : dataPoint.Speed * Constants.KMH_TO_METRES_PER_SECOND / Constants.MILES_TO_KILOMETRES;

                lapViewModel.DataPoints.Add(new LapDataPointViewModel
                {
                    SectorNumber = dataPoint.SectorNumber,
                    ElapsedTime = new TimeSpan(dataPoint.ElapsedTimeTicks),
                    AccelerationX = dataPoint.AccelerationX,
                    AccelerationY = dataPoint.AccelerationY,
                    AccelerationZ = dataPoint.AccelerationZ,
                    Altitude = dataPoint.Altitude,
                    Heading = dataPoint.Heading,
                    Latitude = dataPoint.Latitude,
                    Longitude = dataPoint.Longitude,
                    Speed = speedInUserUnits
                });
                if (speedInUserUnits.HasValue && speedInUserUnits.Value > maximumSpeed)
                    maximumSpeed = speedInUserUnits.Value;
            }
            lapViewModel.MaximumSpeed = maximumSpeed;

            return lapViewModel;
        }

        public static BestLapViewModel AsViewModel(this LapViewModel lap, bool isMetricUnits, bool lapIsUnofficial = false)
        {
            var bestLapViewModel = new BestLapViewModel
            {
                DataPoints = new ObservableCollection<LapDataPointViewModel>(),
                StartElapsedTime = TimeSpan.Zero,
                EndElapsedTime = lap.LapTime,
                MaximumSpeed = lap.MaximumSpeed,
                SectorNumber = lap.SectorNumber,
                Timestamp = lap.Timestamp.ToLocalTime(),
                TotalLaps = lap.TotalLaps,
                IsUnofficial = lapIsUnofficial,
                IsComplete = lap.IsComplete
            };

            foreach (var dataPoint in lap.DataPoints)
            {
                bestLapViewModel.DataPoints.Add(new LapDataPointViewModel
                {
                    SectorNumber = dataPoint.SectorNumber,
                    ElapsedTime = dataPoint.ElapsedTime - lap.StartElapsedTime,
                    AccelerationX = dataPoint.AccelerationX,
                    AccelerationY = dataPoint.AccelerationY,
                    AccelerationZ = dataPoint.AccelerationZ,
                    Altitude = dataPoint.Altitude,
                    Heading = dataPoint.Heading,
                    Latitude = dataPoint.Latitude,
                    Longitude = dataPoint.Longitude,
                    IsEndOfLap = dataPoint.IsEndOfLap,
                    Speed = dataPoint.Speed,
                    Timestamp = dataPoint.Timestamp.ToLocalTime()
                });
            }

            return bestLapViewModel;
        }

        public static VehicleViewModel AsViewModel(this Vehicle vehicle)
        {
            return new VehicleViewModel
            {
                Id = vehicle.id,
                Key = vehicle.Key,
                Make = vehicle.Make,
                Model = vehicle.Model,
                Class = (VehicleClass)vehicle.Class
            };
        }

        public static LapViewModel AsViewModel(this Lap lap, TimeSpan? bestLapTime, bool isMetricUnits)
        {
            var lapViewModel = new LapViewModel
            {
                LapNumber = lap.LapNumber,
                StartElapsedTime = new TimeSpan(lap.StartTicks),
                EndElapsedTime = new TimeSpan(lap.EndTicks),
                // IsComplete is a new field, assume it is true if it hasn't been set
                IsComplete = lap.IsComplete ?? true,
                DataPoints = new ObservableCollection<LapDataPointViewModel>(),
                Timestamp = lap.Timestamp.ToLocalTime()
            };

            if (bestLapTime.HasValue && bestLapTime > TimeSpan.Zero)
                lapViewModel.DifferenceToBest = lapViewModel.LapTime - bestLapTime.Value;
            else
                lapViewModel.DifferenceToBest = TimeSpan.Zero;

            double maximumSpeed = 0;
            foreach (var dataPoint in lap.DataPoints)
            {
                double? speedInUserUnits = isMetricUnits
                                            ? dataPoint.Speed * Constants.KMH_TO_METRES_PER_SECOND
                                            : dataPoint.Speed * Constants.KMH_TO_METRES_PER_SECOND / Constants.MILES_TO_KILOMETRES;

                lapViewModel.DataPoints.Add(new LapDataPointViewModel
                {
                    SectorNumber = dataPoint.SectorNumber,
                    ElapsedTime = new TimeSpan(dataPoint.ElapsedTimeTicks),
                    AccelerationX = dataPoint.AccelerationX,
                    AccelerationY = dataPoint.AccelerationY,
                    AccelerationZ = dataPoint.AccelerationZ,
                    Altitude = dataPoint.Altitude,
                    Heading = dataPoint.Heading,
                    Latitude = dataPoint.Latitude,
                    Longitude = dataPoint.Longitude,
                    IsEndOfLap = dataPoint.IsEndOfLap,
                    Speed = speedInUserUnits,
                    Timestamp = dataPoint.Timestamp.ToLocalTime()
                });
                if (speedInUserUnits.HasValue && speedInUserUnits.Value > maximumSpeed)
                    maximumSpeed = speedInUserUnits.Value;
            }
            lapViewModel.MaximumSpeed = maximumSpeed;

            return lapViewModel;
        }

        public static TrackSessionHeaderViewModel AsViewModel(this TrackSessionHeader trackSession, string filePath, TrackSessionLocation location, bool isMetricUnits)
        {
            return new TrackSessionHeaderViewModel
            {
                BestLapTime = trackSession.BestLapTime,
                DeviceOrientation = trackSession.DeviceOrientation,
                GpsDeviceName = trackSession.GpsDeviceName,
                LocalFilePath = location == TrackSessionLocation.LocalFile || location == TrackSessionLocation.ServerWithLocalFile ? filePath : string.Empty,
                ServerFilePath = location == TrackSessionLocation.ServerFile ? filePath : string.Empty,
                IsUploaded = location == TrackSessionLocation.ServerFile,
                Location = location,
                NumberOfLaps = trackSession.NumberOfLaps,
                Timestamp = trackSession.Timestamp.ToLocalTime(),
                TrackId = trackSession.TrackId,
                TrackName = trackSession.TrackName,
                Notes = trackSession.Notes,
                Vehicle = trackSession.Vehicle.AsViewModel(),
                Weather = trackSession.Weather.AsViewModel(isMetricUnits)
            };
        }

        public static TrackSessionViewModel AsViewModel(this TrackSession session, TrackViewModel track, VehicleViewModel vehicle, TimeSpan? bestLapTime, TrackSessionLocation location, string filePath, bool isMetricUnits)
        {
            return new TrackSessionViewModel
            {
                TrackId = track != null ? track.Id : 0,
                Track = track,
                TrackName = track != null ? track.Name : string.Empty,
                Vehicle = vehicle,
                BestLapTime = session.BestLapTime,
                NumberOfLaps = session.NumberOfLaps,
                DeviceOrientation = session.DeviceOrientation,
                GpsDeviceName = session.GpsDeviceName,
                Location = location,
                LocalFilePath = location == TrackSessionLocation.LocalFile || location == TrackSessionLocation.ServerWithLocalFile ? filePath : string.Empty,
                ServerFilePath = location == TrackSessionLocation.ServerFile ? filePath : string.Empty,
                IsUploaded = location == TrackSessionLocation.ServerFile,
                Laps = new ObservableCollection<LapViewModel>(session.Laps.Select(lap => lap.AsViewModel(bestLapTime, isMetricUnits))),
                Timestamp = session.Timestamp.ToLocalTime(),
                Notes = session.Notes,
                Weather = session.Weather.AsViewModel(isMetricUnits)
            };
        }

        public static WeatherConditionsViewModel AsViewModel(this WeatherConditions weather, bool isMetricUnits)
        {
            if (weather == null) return null;

            return new WeatherConditionsViewModel
            {
                Condition = weather.Condition,
                Temperature = isMetricUnits ? weather.Temperature : ((weather.Temperature * 1.8) + 32),
                WindDirection = weather.WindDegrees,
                WindSpeed = isMetricUnits
                            ? weather.WindSpeed * Constants.KMH_TO_METRES_PER_SECOND
                            : weather.WindSpeed * Constants.KMH_TO_METRES_PER_SECOND / Constants.MILES_TO_KILOMETRES,
                PreviousHourPrecipitation = isMetricUnits
                                                ? weather.PreviousHourPrecipitation
                                                : weather.PreviousHourPrecipitation / Constants.MILLIMETRES_TO_INCHES,
                TotalDayPrecipitation = isMetricUnits
                                        ? weather.TotalDayPrecipitation
                                        : weather.TotalDayPrecipitation / Constants.MILLIMETRES_TO_INCHES
            };
        }

        public static ActivityViewModel AsViewModel(this Activity activity)
        {
            var activityData = activity.Data != null ? JObject.Parse(activity.Data) : new JObject();
            activityData.Add("userName", activity.UserDisplayName);
            string displayText = activity.Text;
            foreach (var property in activityData)
                displayText = displayText.Replace("{" + property.Key + "}", property.Value.ToString());

            return new ActivityViewModel
            {
                Id = activity.id,
                CreatedAt = activity.CreatedAt,
                Data = activity.Data,
                Text = activity.Text,
                FormattedText = displayText,
                TrackId = activity.TrackId,
                Type = (ActivityType)activity.Type,
                UserDisplayName = activity.UserDisplayName,
                UserId = activity.UserId,
                VehicleId = activity.VehicleId
            };
        }

        public static FriendViewModel AsViewModel(this Friend friend)
        {
            return new FriendViewModel
            {
                Id = friend.id,
                UserId = friend.UserId,
                FirstName = friend.FirstName,
                LastName = friend.LastName,
                IsConfirmed = friend.IsConfirmed,
                CurrentUserInitiatedFriendship = friend.CurrentUserInitiatedFriendship,
                LastActivityTime = friend.LastActivityTime
            };
        }

        public static void SetFromModel(this TrackViewModel trackViewModel, Track trackModel, bool isMetricUnits, BestLapViewModel bestLap)
        {
            trackViewModel.Id = trackModel.id;
            trackViewModel.Name = trackModel.Name;
            trackViewModel.Latitude = trackModel.Latitude;
            trackViewModel.Longitude = trackModel.Longitude;
            trackViewModel.Description = trackModel.Description;
            trackViewModel.Length = trackModel.Length;
            trackViewModel.IsMetricUnits = isMetricUnits;
            trackViewModel.BackgroundImagePath = trackModel.BackgroundImagePath;
            trackViewModel.Sectors = trackModel.Sectors != null
                                        ? new ObservableCollection<TrackSectorViewModel>(trackModel.Sectors.Select(AsViewModel))
                                        : null;
            trackViewModel.BestLap = bestLap;
            trackViewModel.MaximumSpeed = bestLap != null ? bestLap.MaximumSpeed : 0;
            trackViewModel.TotalLaps = trackModel.TotalLaps;
            trackViewModel.Country = trackModel.Country;
        }

        #endregion

        public static double ProjectedLength(this LapViewModel lap)
        {
            var geographicLapDataPoints = lap.DataPoints.Where(dp => dp.Latitude.HasValue && dp.Longitude.HasValue);
            if (!geographicLapDataPoints.Any()) return 0d;

            GeographyPoint firstGeographyPoint = null;
            GeographyPoint lastGeographyPoint = null;
            double length = geographicLapDataPoints.Aggregate(Tuple.Create(0d, (GeographyPoint)null), (a, b) =>
                               {
                                   var currentGeoPoint = b.AsGeographyPoint();
                                   if (firstGeographyPoint == null) firstGeographyPoint = currentGeoPoint;
                                   lastGeographyPoint = currentGeoPoint;
                                   double distance = a.Item2 == null || (a.Item2.Latitude == currentGeoPoint.Latitude &&  a.Item2.Longitude == currentGeoPoint.Longitude)
                                                        ? a.Item1
                                                        : a.Item1 + a.Item2.Distance(currentGeoPoint);
                                   return Tuple.Create(distance, currentGeoPoint);
                               }, a => a.Item1);

            // Add distance between last and first points to complete circuit
            if (lastGeographyPoint != null)
                length += lastGeographyPoint.Distance(firstGeographyPoint);

            return length;
        }

        public static double ActualLength(this LapViewModel lap)
        {
            var geographicLapDataPoints = lap.DataPoints.Where(dp => dp.Latitude.HasValue && dp.Longitude.HasValue);
            if (!geographicLapDataPoints.Any()) return 0d;

            double length = geographicLapDataPoints.Aggregate(Tuple.Create(0d, (GeographyPoint)null), (a, b) =>
            {
                var currentGeoPoint = b.AsGeographyPoint();
                double distance = a.Item2 == null ? 0 : a.Item1 + a.Item2.Distance(currentGeoPoint);
                return Tuple.Create(distance, currentGeoPoint);
            }, a => a.Item1);

            return length;
        }

        private static TrackViewModel CreateTrackViewModelFromTrack(Track track, bool isMetricUnits, bool isLocal, BestLap bestLap, string fileNameWhenLoaded)
        {
            var bestLapViewModel = bestLap != null ? bestLap.AsViewModel(isMetricUnits) : null;

            return new TrackViewModel
            {
                Id = track.id,
                Name = track.Name,
                Latitude = track.Latitude,
                Longitude = track.Longitude,
                IsLocal = isLocal,
                Description = track.Description,
                Length = isMetricUnits
                                ? track.Length
                                : track.Length / Constants.MILES_TO_KILOMETRES,
                IsMetricUnits = isMetricUnits,
                BackgroundImagePath = track.BackgroundImagePath,
                Sectors = track.Sectors != null
                            ? new ObservableCollection<TrackSectorViewModel>(track.Sectors.Select(AsViewModel))
                            : null,
                BestLap = bestLapViewModel,
                MaximumSpeed = bestLapViewModel != null ? bestLapViewModel.MaximumSpeed : 0,
                FileNameWhenLoaded = fileNameWhenLoaded,
                TotalLaps = track.TotalLaps,
                Country = track.Country,
                Timestamp = track.Timestamp
            };
        }
    }
}