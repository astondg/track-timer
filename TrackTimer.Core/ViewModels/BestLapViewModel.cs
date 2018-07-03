namespace TrackTimer.Core.ViewModels
{
    public class BestLapViewModel : LapViewModel
    {
        private long id;
        private string userName;
        private string userDisplayName;
        private string verificationCode;
        private string gpsDeviceName;
        private bool isPublic;
        private long vehicleId;
        private VehicleViewModel vehicle;
        private long trackId;
        private bool isUnofficial;
        private string weatherCondition;
        private double? ambientTemperature;

        public long Id
        {
            get { return id; }
            set { SetProperty(ref id, value); }
        }
        public string UserName
        {
            get { return userName; }
            set { SetProperty(ref userName, value); }
        }
        public string UserDisplayName
        {
            get { return userDisplayName; }
            set { SetProperty(ref userDisplayName, value); }
        }
        public string VerificationCode
        {
            get { return verificationCode; }
            set { SetProperty(ref verificationCode, value); }
        }
        public bool IsPublic
        {
            get { return isPublic; }
            set { SetProperty(ref isPublic, value); }
        }
        public long VehicleId
        {
            get { return vehicleId; }
            set { SetProperty(ref vehicleId, value); }
        }
        public VehicleViewModel Vehicle
        {
            get { return vehicle; }
            set { SetProperty(ref vehicle, value); }
        }
        public long TrackId
        {
            get { return trackId; }
            set { SetProperty(ref trackId, value); }
        }
        public bool IsUnofficial
        {
            get { return isUnofficial; }
            set { SetProperty(ref isUnofficial, value); }
        }
        public string GpsDeviceName
        {
            get { return gpsDeviceName; }
            set { SetProperty(ref gpsDeviceName, value); }
        }
        public string WeatherCondition
        {
            get { return weatherCondition; }
            set { SetProperty(ref weatherCondition, value); }
        }
        public double? AmbientTemperature
        {
            get { return ambientTemperature; }
            set { SetProperty(ref ambientTemperature, value); }
        }
    }
}