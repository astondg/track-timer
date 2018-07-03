namespace TrackTimer.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using System.Windows.Threading;
    using BugSense;
    using BugSense.Core.Model;
    using GalaSoft.MvvmLight.Command;
    using TrackTimer.Contracts;
    using TrackTimer.Core.Geolocation;
    using TrackTimer.Core.Resources;
    using TrackTimer.Core.ViewModels;
    using TrackTimer.Extensions;
    using TrackTimer.Resources;
    using TrackTimer.Services;
    using Windows.Foundation;
    using TrackTimer.Core.Extensions;

    public class TimerViewModel : BaseViewModel
    {
        private int currentSessionLapCount;
        private TimeSpan currentLapTime;
        private TimeSpan currentLapStatus;
        private bool isDataLoading;
        private string loadingStatusText;
        private bool gpsStatusIsOk;
        private string gpsStatusText;
        private DeviceOrientation currentDeviceOrientation;
        private DeviceOrientation startDeviceOrientation;
        private DateTimeOffset startTime;
        private Timer lapUpdateTimer;
        private IDictionary<int, TimeSpan?> bestLapSectors;
        private LapTimer lapTimer;
        private ICommand startTiming;
        private ICommand stopTiming;
        private TimeSpan lapStartElapsed;
        private GeolocatorFactory geolocatorFactory;
        private ICamera camera;
        private IEnumerable<TrackSectorViewModel> trackSectors;

        public event TypedEventHandler<TimerViewModel, object> TimingStarted;
        public event TypedEventHandler<TimerViewModel, TimingStoppedViewModel> TimingStopped;

        public TimerViewModel(LiveClient liveClient, LapViewModel bestLap, IEnumerable<TrackSectorViewModel> trackSectors, GeolocatorFactory geolocatorFactory, ICamera camera, Dispatcher dispatcher)
        {
            Dispatcher = dispatcher;
            LapCount = 1;
            CurrentLapTime = TimeSpan.Zero;
            CurrentLapStatus = TimeSpan.Zero;
            GpsStatusText = AppResources.Text_Unit_InternalGps;
            Laps = new ObservableCollection<LapViewModel>();
            this.trackSectors = trackSectors;
            bestLapSectors = new Dictionary<int, TimeSpan?>();
            SetBestLapSectorsFromLap(bestLap);
            this.geolocatorFactory = geolocatorFactory;
            this.camera = camera;
            lapTimer = new LapTimer(geolocatorFactory, App.ViewModel.Settings.IsMetricUnits);
            lapTimer.GeolocatorStatusChanged += lapTimer_GeolocatorStatusChanged;
            lapTimer.GeolocatorUnrecoverableError += lapTimer_GeolocatorUnrecoverableError;
        }

        public int LapCount
        {
            get { return currentSessionLapCount; }
            set { SetProperty(ref currentSessionLapCount, value); }
        }
        public TimeSpan CurrentLapTime
        {
            get { return currentLapTime; }
            set { SetProperty(ref currentLapTime, value); }
        }
        public TimeSpan CurrentLapStatus
        {
            get { return currentLapStatus; }
            set { SetProperty(ref currentLapStatus, value); }
        }
        public bool IsDataLoading
        {
            get { return isDataLoading; }
            set { SetProperty(ref isDataLoading, value); }
        }
        public string LoadingStatusText
        {
            get { return loadingStatusText; }
            set { SetProperty(ref loadingStatusText, value); }
        }
        public bool IsTiming { get; set; }
        public bool GpsStatusIsOk
        {
            get { return gpsStatusIsOk; }
            set { SetProperty(ref gpsStatusIsOk, value); }
        }
        public string GpsStatusText
        {
            get { return gpsStatusText; }
            set { SetProperty(ref gpsStatusText, value); }
        }
        public DateTimeOffset StartTime
        {
            get { return startTime; }
            set { SetProperty(ref startTime, value); }
        }
        public DeviceOrientation StartPhoneOrientation
        {
            get { return startDeviceOrientation; }
            set { SetProperty(ref startDeviceOrientation, value); }
        }
        public DeviceOrientation CurrentPhoneOrientation
        {
            get { return currentDeviceOrientation; }
            set
            {
                SetProperty(ref currentDeviceOrientation, value);
                if (lapTimer != null && !lapTimer.IsTiming)
                    lapTimer.CurrentDeviceOrientation = value;
            }
        }
        public ICommand StartTiming
        {
            get
            {
                if (startTiming == null)
                    startTiming = new RelayCommand(async () => await StartLapTimer(), () => lapTimer == null || !lapTimer.IsTiming);
                return startTiming;
            }
        }
        public ICommand StopTiming
        {
            get
            {
                if (stopTiming == null)
                    stopTiming = new RelayCommand(() => StopLapTimer(), () => lapTimer != null && lapTimer.IsTiming);
                return stopTiming;
            }
        }
        public Dispatcher Dispatcher { get; set; }
        public ObservableCollection<LapViewModel> Laps { get; private set; }

        public async Task Initialise()
        {
            IsDataLoading = true;
            LoadingStatusText = AppResources.Text_LoadingStatus_StartingGps;
            await lapTimer.Initialize(trackSectors);
        }

        public void UnloadGps()
        {
            if (lapTimer == null) return;

            lapTimer.GeolocatorStatusChanged -= lapTimer_GeolocatorStatusChanged;
            lapTimer.GeolocatorUnrecoverableError -= lapTimer_GeolocatorUnrecoverableError;
            lapTimer.Dispose();
            lapTimer = null;
        }

        public async Task StartLapTimer()
        {
            // Fire & forget - don't care about result
            await TurnOnCamera();
            IsTiming = true;
            lapTimer.TimingStarted += lapTimer_TimingStarted;
            lapTimer.SectorComplete += lapTimer_SectorComplete;
            lapTimer.LapComplete += lapTimer_LapComplete;

            lapUpdateTimer = new Timer(UpdateStopwatchDisplay);
            lapUpdateTimer.Change(0, 50);
            LapCount = 1;
            CurrentLapStatus = TimeSpan.Zero;
            CurrentLapTime = TimeSpan.Zero;
            lapTimer.StartTiming(true);
        }

        public void StopLapTimer(string errorMessage = null)
        {
            // Stop timing and clean up associated resources
            IsTiming = false;
            lapTimer.StopTiming();
            lapTimer.SectorComplete -= lapTimer_SectorComplete;
            lapTimer.LapComplete -= lapTimer_LapComplete;
            if (lapUpdateTimer != null)
            {
                lapUpdateTimer.Dispose();
                lapUpdateTimer = null;
            }

            // There may be an incomplete lap in progress, if so then add it to the lap collection
            var finalLap = lapTimer.CurrentLap;
            if (finalLap != null)
                Laps.Add(finalLap);

            // Notify subscribers that timing is complete
            if (TimingStopped != null)
                TimingStopped(this, new TimingStoppedViewModel
                {
                    IsSuccessful = errorMessage == null,
                    CompletionMessage = errorMessage,
                    GpsDeviceName = lapTimer.DeviceIdStamp,
                    Laps = Laps,
                    GeneratedTrackSectors = lapTimer.GenerateSectors
                                            ? new ObservableCollection<TrackSectorViewModel>(lapTimer.SectorCoordinates)
                                            : null
                });

            // Fire & forget - don't care about result
            var cameraResult = StopCameraRecordingAndTurnOff();
        }

        public void SuspendLapTimerUpdates()
        {
            lapTimer.GeolocatorStatusChanged -= lapTimer_GeolocatorStatusChanged;
            lapTimer.GeolocatorUnrecoverableError -= lapTimer_GeolocatorUnrecoverableError;
            if (lapUpdateTimer != null)
            {
                lapUpdateTimer.Dispose();
                lapUpdateTimer = null;
            }
        }

        public void ResumeLapTimerUpdates()
        {
            lapTimer.GeolocatorStatusChanged += lapTimer_GeolocatorStatusChanged;
            lapTimer.GeolocatorUnrecoverableError += lapTimer_GeolocatorUnrecoverableError;
            lapUpdateTimer = new Timer(UpdateStopwatchDisplay);
            lapUpdateTimer.Change(0, 50);
        }

        private void UpdateStopwatchDisplay(object state)
        {
            Dispatcher.BeginInvoke(() =>
            {
                if (lapTimer == null || !lapTimer.IsTiming) return;
                CurrentLapTime = (lapTimer.Elapsed - lapStartElapsed);
            });
        }

        private void SetBestLapSectorsFromLap(LapViewModel bestLap)
        {
            foreach (var sectorCoordinate in trackSectors ?? Enumerable.Empty<TrackSectorViewModel>())
            {
                if (bestLap != null && bestLap.DataPoints != null)
                {
                    TimeSpan? sectorSplit = null;
                    if (sectorCoordinate.IsFinishLine)
                    {
                        sectorSplit = bestLap.LapTime;
                    }
                    else
                    {
                        var lastLapCoordinate = bestLap.DataPoints.LastOrDefault(lc => lc.SectorNumber == sectorCoordinate.SectorNumber);
                        if (lastLapCoordinate != null)
                            sectorSplit = lastLapCoordinate.ElapsedTime - bestLap.StartElapsedTime;
                    }
                    bestLapSectors[sectorCoordinate.SectorNumber] = sectorSplit;
                }
                else
                {
                    bestLapSectors[sectorCoordinate.SectorNumber] = null;
                }
            }
        }

        private void lapTimer_GeolocatorStatusChanged(LapTimer sender, StatusChangedEventArgs args)
        {
            Dispatcher.BeginInvoke(() =>
                {
                    if (lapTimer == null) return;

                    switch (args.Status)
                    {
                        case PositionStatus.Initializing:
                            IsDataLoading = true;
                            LoadingStatusText = AppResources.Text_LoadingStatus_StartingGps;
                            GpsStatusText = lapTimer.UsingInternalGps ? AppResources.Text_Unit_InternalGps : AppResources.Text_Unit_ExternalGps;
                            GpsStatusIsOk = false;
                            break;
                        case PositionStatus.Ready:
                            LoadingStatusText = string.Empty;
                            IsDataLoading = false;
                            GpsStatusText = lapTimer.UsingInternalGps ? AppResources.Text_Unit_InternalGps : AppResources.Text_Unit_ExternalGps;
                            GpsStatusIsOk = true;
                            break;
                        case PositionStatus.NoData:
                        case PositionStatus.NotAvailable:
                        case PositionStatus.Disabled:
                        case PositionStatus.NotInitialized:
                        default:
                            LoadingStatusText = string.Empty;
                            IsDataLoading = false;
                            GpsStatusText = lapTimer.UsingInternalGps ? AppResources.Text_Unit_InternalGps : AppResources.Text_Unit_ExternalGps;
                            GpsStatusIsOk = false;
                            break;
                    }
                });
        }

        private async void lapTimer_GeolocatorUnrecoverableError(LapTimer sender, GeolocationErrorEventArgs args)
        {
            string errorId = string.Empty;

            if (args.Exception != null)
            {
                var extraCrashData = new CrashExtraData(AppConstants.BUGSENSE_EXTRADATA_DEVICENAME, args.DeviceName);
                errorId = await BugSenseHandler.Instance.LogExceptionWithId(App.MobileService.CurrentUser, args.Exception, extraCrashData);
            }

            StopLapTimer(string.Format(args.DeviceName.Equals(AppConstants.DEFAULT_INTERNALGPS_NAME, StringComparison.OrdinalIgnoreCase)
                                        ? AppResources.Text_Error_UnableToCommunicateWithInternalGpsDevice
                                        : AppResources.Text_Error_UnableToCommunicateWithExternalGpsDevice,
                                        errorId));
        }

        private async void lapTimer_TimingStarted(LapTimer sender, object args)
        {
            StartTime = DateTime.Now;
            StartPhoneOrientation = CurrentPhoneOrientation;
            await StartCameraRecording();
            if (TimingStarted != null)
                TimingStarted(this, Laps);
        }

        private void lapTimer_SectorComplete(LapTimer sender, LapEventArgs args)
        {
            Dispatcher.BeginInvoke(() =>
            {
                var sectorSplit = args.Lap.EndElapsedTime - lapStartElapsed;
                var bestLapSectorTime = bestLapSectors.Any()
                                            ? bestLapSectors[args.Lap.SectorNumber]
                                            : (TimeSpan?)null;
                if (bestLapSectorTime.HasValue)
                {
                    var sectorTimeDifference = sectorSplit - bestLapSectorTime.Value;
                    CurrentLapStatus = sectorTimeDifference;
                }
                else
                {
                    CurrentLapStatus = TimeSpan.Zero;
                }
            });
        }

        private void lapTimer_LapComplete(LapTimer sender, LapEventArgs args)
        {
            Dispatcher.BeginInvoke(() =>
            {
                lapStartElapsed = args.Lap.EndElapsedTime;
                Laps.Insert(0, args.Lap);
                LapCount = args.Lap.LapNumber+1;
                if ((bestLapSectors == null || !bestLapSectors.Any()) && lapTimer.GenerateSectors && lapTimer.SectorCoordinates != null && lapTimer.SectorCoordinates.Any())
                {
                    trackSectors = lapTimer.SectorCoordinates;
                    SetBestLapSectorsFromLap(args.Lap);
                    CurrentLapStatus = TimeSpan.Zero;
                    return;
                }

                if (bestLapSectors == null || !bestLapSectors.Any())
                    SetBestLapSectorsFromLap(args.Lap);

                var bestLapSectorTime = bestLapSectors.Any()
                                                ? bestLapSectors[args.Lap.SectorNumber]
                                                : (TimeSpan?)null;
                if (bestLapSectorTime.HasValue)
                {
                    var sectorTimeDifference = args.Lap.LapTime - bestLapSectorTime.Value;
                    CurrentLapStatus = sectorTimeDifference;
                    if (sectorTimeDifference < TimeSpan.Zero)
                        SetBestLapSectorsFromLap(args.Lap);
                }
                else
                {
                    SetBestLapSectorsFromLap(args.Lap);
                    CurrentLapStatus = TimeSpan.Zero;
                }
            });
        }

        private async Task<bool> TurnOnCamera()
        {
            if (camera == null) return false;
            return await camera.PowerOn();
        }

        private async Task<bool> TurnOffCamera()
        {
            if (camera == null) return false;
            return await camera.PowerOff();
        }

        private async Task<bool> StartCameraRecording()
        {
            if (camera == null) return false;
            return await camera.StartRecording();
        }

        private async Task<bool> StopCameraRecording()
        {
            if (camera == null) return false;
            return await camera.StopRecording();
        }

        private async Task StopCameraRecordingAndTurnOff()
        {
            bool cameraStopped = await StopCameraRecording();
            if (cameraStopped)
            {
                // Short delay to allow previous command to complete
                await Task.Delay(1000);
                await TurnOffCamera();
            }
        }
    }
}