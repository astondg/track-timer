namespace TrackTimer.Services
{
    using System;
    using System.Threading.Tasks;
    using TrackTimer.Contracts;
    using TrackTimer.Extensions;
    using TrackTimer.Resources;
    using Windows.Devices.Geolocation;

    public class InternalGeolocator : IGeolocator
    {
        private Geolocator geolocator;
        private Windows.Foundation.TypedEventHandler<IGeolocator, Core.Geolocation.PositionChangedEventArgs> positionChanged;
        private Windows.Foundation.TypedEventHandler<IGeolocator, Core.Geolocation.StatusChangedEventArgs> statusChanged;

        public InternalGeolocator()
        {
            geolocator = new Geolocator();
        }

        public Core.Geolocation.PositionAccuracy DesiredAccuracy
        {
            get { return geolocator.DesiredAccuracy.AsTrackTimerModel(); }
            set { geolocator.DesiredAccuracy = (PositionAccuracy)(int)value; }
        }

        public uint? DesiredAccuracyInMeters
        {
            get { return geolocator.DesiredAccuracyInMeters; }
            set { geolocator.DesiredAccuracyInMeters = value; }
        }

        public Core.Geolocation.PositionStatus LocationStatus
        {
            get { return geolocator.LocationStatus.AsTrackTimerModel(); }
        }

        public double MovementThreshold
        {
            get
            {
                return geolocator.MovementThreshold;
            }
            set
            {
                geolocator.MovementThreshold = value;
            }
        }

        public uint ReportInterval
        {
            get
            {
                return geolocator.ReportInterval;
            }
            set
            {
                geolocator.ReportInterval = value;
            }
        }

        public string DeviceName { get { return AppConstants.DEFAULT_INTERNALGPS_NAME; } }

        public event Windows.Foundation.TypedEventHandler<IGeolocator, Core.Geolocation.PositionChangedEventArgs> PositionChanged
        {
            add
            {
                geolocator.PositionChanged += geolocator_PositionChanged;
                positionChanged += value;
            }
            remove { positionChanged -= value; }
        }

        public event Windows.Foundation.TypedEventHandler<IGeolocator, Core.Geolocation.StatusChangedEventArgs> StatusChanged
        {
            add
            {
                geolocator.StatusChanged += geolocator_StatusChanged;
                statusChanged += value;
            }
            remove { statusChanged -= value; }
        }

        public event Windows.Foundation.TypedEventHandler<IGeolocator, GeolocationErrorEventArgs> UnrecoverableError;

        public async Task<Core.Geolocation.Geocoordinate> GetGeopositionAsync()
        {
            var position = await geolocator.GetGeopositionAsync();
            return position.Coordinate.AsTrackTimerModel();
        }

        public async Task<Core.Geolocation.Geocoordinate> GetGeopositionAsync(TimeSpan maximumAge, TimeSpan timeout)
        {
            var position = await geolocator.GetGeopositionAsync(maximumAge, timeout);
            return position.Coordinate.AsTrackTimerModel();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && geolocator != null)
            {
                geolocator.PositionChanged -= geolocator_PositionChanged;
                geolocator.StatusChanged -= geolocator_StatusChanged;
            }
        }

        private void geolocator_StatusChanged(Geolocator sender, StatusChangedEventArgs args)
        {
            if (statusChanged != null)
                statusChanged(this, new Core.Geolocation.StatusChangedEventArgs(args.Status.AsTrackTimerModel()));
        }

        private void geolocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            if (positionChanged != null)
                positionChanged(this, new Core.Geolocation.PositionChangedEventArgs(args.Position.Coordinate.AsTrackTimerModel()));
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}