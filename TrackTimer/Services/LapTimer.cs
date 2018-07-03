namespace TrackTimer.Services
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using TrackTimer.Contracts;
    using TrackTimer.Core.Extensions;
    using TrackTimer.Core.Geolocation;
    using TrackTimer.Core.Resources;
    using TrackTimer.Core.ViewModels;
    using TrackTimer.Extensions;
    using TrackTimer.Resources;
    using Windows.Devices.Sensors;
    using Windows.Foundation;

    public class LapTimer : BaseViewModel, IDisposable
    {
        private bool isTiming;
        private bool isFirstReading;
        private bool generateSectors;
        private bool isMetricUnits;
        private int previousLapEndCoordinateIndex;
        private int lastSectorNumber;
        private int firstSectorNumber;
        private int lastGeographicLapDataPointIndex;
        private int lapNumber;
        private List<double> headingNormalisation;
        private TimeSpan lapStartElapsedTime;
        private IGeolocator geolocator;
        private Accelerometer accelerometer;
        private Stopwatch stopwatch;
        private LapDataPointViewModel lastGeographicLapDataPoint;
        private TrackSectorViewModel currentSector;
        private IGeolocatorFactory geolocatorFactory;
        private IEnumerable<TrackSectorViewModel> sectorCoordinates;
        private IList<LapDataPointViewModel> lapCoordinates;

        public event TypedEventHandler<LapTimer, StatusChangedEventArgs> GeolocatorStatusChanged;
        public event TypedEventHandler<LapTimer, GeolocationErrorEventArgs> GeolocatorUnrecoverableError;
        public event TypedEventHandler<LapTimer, object> TimingStarted;
        public event TypedEventHandler<LapTimer, LapEventArgs> LapComplete;
        public event TypedEventHandler<LapTimer, LapEventArgs> SectorComplete;

        public LapTimer(IGeolocatorFactory geolocatorFactory, bool isMetricUnits)
        {
            this.geolocatorFactory = geolocatorFactory;
            this.isMetricUnits = isMetricUnits;
        }

        public bool IsTiming { get { return isTiming; } }

        public bool GenerateSectors { get { return generateSectors; } }

        public bool UsingInternalGps { get { return geolocator != null ? geolocator.DeviceName == AppConstants.DEFAULT_INTERNALGPS_NAME : false; } }

        public string DeviceIdStamp { get { return geolocator != null ? geolocator.DeviceName : string.Empty; } }

        public TimeSpan Elapsed { get { return stopwatch.Elapsed; } }

        public DeviceOrientation CurrentDeviceOrientation { get; set; }

        public IEnumerable<TrackSectorViewModel> SectorCoordinates { get { return sectorCoordinates; } }

        /// <summary>
        /// Returns a snapshot of the current lap at the current point in time, including all Data Points collected since the lap started
        /// </summary>
        public LapViewModel CurrentLap
        {
            get
            {
                var currentLapCoordinates = lapCoordinates.ToList()
                                                          .Skip(previousLapEndCoordinateIndex + 1)
                                                          .ToList();
                if (!currentLapCoordinates.Any()) return null;

                var endPoint = currentLapCoordinates.Last();

                return new LapViewModel
                {
                    DataPoints = new ObservableCollection<LapDataPointViewModel>(currentLapCoordinates),
                    StartElapsedTime = lapStartElapsedTime,
                    EndElapsedTime = endPoint.ElapsedTime,
                    MaximumSpeed = currentLapCoordinates.Max(c => c.Speed) ?? 0,
                    Timestamp = currentLapCoordinates.First().Timestamp,
                    SectorNumber = endPoint.SectorNumber,
                    LapNumber = lapNumber,
                    IsComplete = false
                };
            }
        }

        public async Task Initialize(IEnumerable<TrackSectorViewModel> sectorCoordinates)
        {
            if (isTiming)
                throw new InvalidOperationException("Cannot initialize LapTimer while it is already running. Please stop timing first.");

            isTiming = false;
            isFirstReading = true;

            if (geolocator != null)
            {
                geolocator.StatusChanged -= geolocator_StatusChanged;
                geolocator.UnrecoverableError -= geolocator_UnrecoverableError;
                geolocator.Dispose();
            }

            this.sectorCoordinates = sectorCoordinates ?? Enumerable.Empty<TrackSectorViewModel>();
            if (this.sectorCoordinates.Any())
            {
                generateSectors = false;
                lastSectorNumber = this.sectorCoordinates.Max(s => s.SectorNumber);
                firstSectorNumber = this.sectorCoordinates.Min(s => s.SectorNumber);
            }
            else
            {
                generateSectors = true;
                lastSectorNumber = 1;
                firstSectorNumber = 1;
            }
            lapNumber = 1;
            lapStartElapsedTime = TimeSpan.Zero;
            stopwatch = new Stopwatch();
            geolocator = await geolocatorFactory.GetGeolocator();
            geolocator.DesiredAccuracy = PositionAccuracy.High;
            geolocator.MovementThreshold = 1;
            geolocator.StatusChanged += geolocator_StatusChanged;
            geolocator.UnrecoverableError += geolocator_UnrecoverableError;
        }

        private void geolocator_UnrecoverableError(IGeolocator sender, GeolocationErrorEventArgs args)
        {
            if (GeolocatorUnrecoverableError != null)
                GeolocatorUnrecoverableError(this, args);
        }

        public void StartTiming(bool startOnMovement = true)
        {
            if (geolocator == null)
                throw new InvalidOperationException("LapTimer must be initialized before calling StartTiming.");

            if (isTiming)
                StopTiming();

            lapNumber = 1;
            lapStartElapsedTime = TimeSpan.Zero;
            headingNormalisation = new List<double>();
            lapCoordinates = new List<LapDataPointViewModel>();
            currentSector = generateSectors ? null : sectorCoordinates.MinBy(s => s.SectorNumber);
            accelerometer = Accelerometer.GetDefault();
            if (accelerometer != null)
                accelerometer.ReadingChanged += accelerometer_ReadingChanged;

            geolocator.PositionChanged += geolocator_PositionChanged;
            if (!startOnMovement)
            {
                stopwatch.Start();
                isTiming = true;
                if (TimingStarted != null)
                    TimingStarted(this, null);
            }
            isFirstReading = true;
        }

        public void StopTiming()
        {
            if (isTiming)
            {
                if (accelerometer != null)
                {
                    accelerometer.ReadingChanged -= accelerometer_ReadingChanged;
                    accelerometer = null;
                }
                geolocator.PositionChanged -= geolocator_PositionChanged;

                stopwatch.Reset();
                isTiming = false;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && geolocator != null)
            {
                geolocator.Dispose();
                geolocator = null;
            }
        }

        private void accelerometer_ReadingChanged(Accelerometer sender, AccelerometerReadingChangedEventArgs args)
        {
            if (!isTiming)
                return;

            var elapsedTime = stopwatch.Elapsed;
            var accelerometerDataPoint = new LapDataPointViewModel
            {
                ElapsedTime = elapsedTime,
                AccelerationX = args.Reading.AccelerationX,
                AccelerationY = args.Reading.AccelerationY,
                AccelerationZ = args.Reading.AccelerationZ,
                SectorNumber = currentSector != null ? currentSector.SectorNumber : 1,
                Timestamp = args.Reading.Timestamp.ToLocalTime()
            };
            lapCoordinates.Add(accelerometerDataPoint);
        }

        private void geolocator_StatusChanged(IGeolocator sender, StatusChangedEventArgs args)
        {
            if (GeolocatorStatusChanged != null)
                GeolocatorStatusChanged(this, args);
        }

        private void geolocator_PositionChanged(IGeolocator sender, PositionChangedEventArgs args)
        {
            if (!isTiming && isFirstReading)
            {
                // Speed threshold for starting timing - 4m/s
#if DEBUG
                if (!args.Position.Speed.HasValue || double.IsNaN(args.Position.Speed.Value) || args.Position.Speed.Value < 0.5d) return;
#else
                if (!args.Position.Speed.HasValue || double.IsNaN(args.Position.Speed.Value) || args.Position.Speed.Value < 4d) return;
#endif
                isFirstReading = false;
                stopwatch.Start();
                isTiming = true;
                if (TimingStarted != null)
                    TimingStarted(this, null);
            }

            var elapsedTime = stopwatch.Elapsed;
            double? speedInUserUnits = args.Position.Speed.HasValue && !double.IsNaN(args.Position.Speed.Value)
                                        ? isMetricUnits
                                            ? args.Position.Speed * Constants.KMH_TO_METRES_PER_SECOND
                                            : args.Position.Speed * Constants.KMH_TO_METRES_PER_SECOND / Constants.MILES_TO_KILOMETRES
                                        : null;

            var newGeographicLapDataPoint = new LapDataPointViewModel
            {
                ElapsedTime = elapsedTime,
                Altitude = args.Position.Altitude.HasValue && !double.IsNaN(args.Position.Altitude.Value) ?  args.Position.Altitude : null,
                Heading = args.Position.Heading.HasValue && !double.IsNaN(args.Position.Heading.Value) ?  args.Position.Heading : null,
                Latitude = args.Position.Latitude,
                Longitude = args.Position.Longitude,
                Speed = speedInUserUnits,
                SectorNumber = currentSector != null ? currentSector.SectorNumber : 1,
                Timestamp = args.Position.Timestamp.ToLocalTime()
            };
            lapCoordinates.Add(newGeographicLapDataPoint);
            // Assuming this adding to the end of the list for performance
            int possibleLapEndGeographicLapDataPointIndex = lastGeographicLapDataPointIndex;
            lastGeographicLapDataPointIndex = lapCoordinates.Count - 1;

            bool skipCurrentCoordinate = false;
            if (generateSectors && currentSector == null)
            {
                skipCurrentCoordinate = true;
                if (newGeographicLapDataPoint.Heading.HasValue)
                {
                    double? lastHeading = headingNormalisation.Any() ? headingNormalisation.Last() : (double?)null;
                    headingNormalisation.Add(newGeographicLapDataPoint.Heading.Value);
                    if (lastHeading.HasValue && HeadingIsWithinRange(lastHeading.Value, newGeographicLapDataPoint.Heading.Value, 30d))
                        currentSector = newGeographicLapDataPoint.ToTrackSector(1, true);
                }
            }

            if (skipCurrentCoordinate)
                return;

            int currentSectorNumber = currentSector.SectorNumber;
            bool sectorCompleted = PositionCompletesSector(newGeographicLapDataPoint, lastGeographicLapDataPoint, currentSector);
            var possibleSectorEndCoordinate = lastGeographicLapDataPoint;
            lastGeographicLapDataPoint = newGeographicLapDataPoint;
            bool lapComplete = false;
            if (sectorCompleted)
            {
                lapComplete = currentSector.IsFinishLine;
                int nextSectorNumber = currentSectorNumber + 1;
                if (nextSectorNumber > lastSectorNumber)
                    nextSectorNumber = firstSectorNumber;

                // Fix the sector number for the current point, i.e. Move it over into the next sector and possibleSectorEndCoordinate is the end of the last sector
                newGeographicLapDataPoint.SectorNumber = nextSectorNumber;

                if (sectorCoordinates.Any())
                    currentSector = sectorCoordinates.Single(s => s.SectorNumber == nextSectorNumber);
                else
                    lapComplete = true;

                if (!lapComplete)
                    EndSector(possibleSectorEndCoordinate, currentSectorNumber);
            }
            if (lapComplete)
                EndLap(possibleSectorEndCoordinate, possibleLapEndGeographicLapDataPointIndex, currentSectorNumber);
        }

        private void EndSector(LapDataPointViewModel sectorEndDataPoint, int currentSectorNumber)
        {
            if (SectorComplete == null) return;

            var lap = new LapViewModel
            {
                StartElapsedTime = lapStartElapsedTime,
                EndElapsedTime = sectorEndDataPoint.ElapsedTime,
                MaximumSpeed = sectorEndDataPoint.Speed ?? 0,
                Timestamp = sectorEndDataPoint.Timestamp,
                SectorNumber = currentSectorNumber,
                IsComplete = false
            };
            SectorComplete(this, new LapEventArgs(lap));
        }

        private void EndLap(LapDataPointViewModel lapEndDataPoint, int lapEndDataPointIndex, int currentSectorNumber)
        {
            if (!lapCoordinates.Any(lc => lc.Latitude.HasValue && lc.Longitude.HasValue && lc.Heading.HasValue))
                return;

            var lapStartTime = lapStartElapsedTime;
            var lapTime = lapStartElapsedTime = lapEndDataPoint.ElapsedTime;
            lapEndDataPoint.IsEndOfLap = true;
            int currentLapEndCoordinate = lapEndDataPointIndex;
            var allCoordinates = lapCoordinates.ToList();
            var currentLapCoordinates = allCoordinates.Skip(previousLapEndCoordinateIndex + 1).Take(currentLapEndCoordinate - previousLapEndCoordinateIndex).ToList();
            if (!currentLapCoordinates.Any())
                return;
            var lap = new LapViewModel
            {
                StartElapsedTime = lapStartTime,
                EndElapsedTime = lapTime,
                MaximumSpeed = currentLapCoordinates.Max(c => c.Speed) ?? 0,
                Timestamp = currentLapCoordinates.First().Timestamp,
                SectorNumber = currentSectorNumber,
                LapNumber = lapNumber,
                IsComplete = true
            };
            previousLapEndCoordinateIndex = currentLapEndCoordinate;

            if (generateSectors)
            {
                if (currentLapCoordinates.Count() < 3) return;
                lap.DataPoints = new ObservableCollection<LapDataPointViewModel>(currentLapCoordinates);

                var sectors = currentLapCoordinates.ToTrackSectors();
                if (sectors.Count < 3) return;

                currentSector = sectors[1];
                sectorCoordinates = sectors.Select(s => s.Value);
                lastSectorNumber = 3;
            }
            else
            {
                // Some accelerometer readings in this lap but received before currentSector changed will have the last SectorNumber
                // so change them to be the first sector
                foreach (var coordinate in currentLapCoordinates)
                {
                    if (coordinate.SectorNumber == 1)
                        break;

                    coordinate.SectorNumber = 1;
                }
                lap.DataPoints = new ObservableCollection<LapDataPointViewModel>(currentLapCoordinates);
            }

            lapNumber++;
            if (LapComplete != null)
                LapComplete(this, new LapEventArgs(lap));
        }

        private bool PositionCompletesSector(LapDataPointViewModel currentPosition, LapDataPointViewModel previousPosition, TrackSectorViewModel sector)
        {
            if (previousPosition == null) return false;
            if (currentPosition.Heading.HasValue && !HeadingIsWithinRange(sector.Heading, currentPosition.Heading.Value, 170d))
                return false;

            return sector.IsCompletedByLapDataPoints(previousPosition, currentPosition);
        }

        private bool HeadingIsWithinRange(double baseHeading, double headingToTest, double degreesToBound)
        {
            if (double.IsNaN(headingToTest)) return false;

            double halfBound = degreesToBound / 2;
            double headingDifference = headingToTest - baseHeading;
            if (headingDifference <= -180)
                headingDifference += 360;
            else if (headingDifference >= 180)
                headingDifference -= 360;

            return headingDifference < halfBound && headingDifference > -halfBound;
        }
    }
}