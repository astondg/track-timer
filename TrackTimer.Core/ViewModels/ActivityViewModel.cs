namespace TrackTimer.Core.ViewModels
{
    using System;
    using TrackTimer.Core.Resources;

    public class ActivityViewModel : BaseViewModel
    {
        private string id;
        private string userId;
        private string userDisplayName;
        private ActivityType type;
        private string text;
        private string formattedText;
        private string data;
        private long? trackId;
        private long? vehicleId;
        private DateTimeOffset createdAt;

        public string Id
        {
            get { return id; }
            set { SetProperty(ref id, value); }
        }
        public string UserId
        {
            get { return userId; }
            set { SetProperty(ref userId, value); }
        }
        public string UserDisplayName
        {
            get { return userDisplayName; }
            set { SetProperty(ref userDisplayName, value); }
        }
        public ActivityType Type
        {
            get { return type; }
            set { SetProperty(ref type, value); }
        }
        public string Text
        {
            get { return text; }
            set { SetProperty(ref text, value); }
        }
        public string FormattedText
        {
            get { return formattedText; }
            set { SetProperty(ref formattedText, value); }
        }
        public string Data
        {
            get { return data; }
            set { SetProperty(ref data, value); }
        }
        public long? TrackId
        {
            get { return trackId; }
            set { SetProperty(ref trackId, value); }
        }
        public long? VehicleId
        {
            get { return vehicleId; }
            set { SetProperty(ref vehicleId, value); }
        }
        public DateTimeOffset CreatedAt
        {
            get { return createdAt; }
            set { SetProperty(ref createdAt, value); }
        }
    }
}