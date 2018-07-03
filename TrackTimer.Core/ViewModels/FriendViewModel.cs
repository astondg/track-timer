namespace TrackTimer.Core.ViewModels
{
    using System;

    public class FriendViewModel : BaseViewModel
    {
        private string id;
        private string userId;
        private string firstName;
        private string lastName;
        private bool isConfirmed;
        private bool currentUserInitiatedFriendship;
        private DateTimeOffset? lastActivityTime;

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
        public string FirstName
        {
            get { return firstName; }
            set { SetProperty(ref firstName, value); }
        }
        public string LastName
        {
            get { return lastName; }
            set { SetProperty(ref lastName, value); }
        }
        public bool IsConfirmed
        {
            get { return isConfirmed; }
            set
            {
                if (SetProperty(ref isConfirmed, value))
                    NotifyPropertyChanged("CanConfirmFriendship");
            }
        }
        public bool CurrentUserInitiatedFriendship
        {
            get { return currentUserInitiatedFriendship; }
            set
            {
                if (SetProperty(ref currentUserInitiatedFriendship, value))
                    NotifyPropertyChanged("CanConfirmFriendship");
            }
        }
        public bool CanConfirmFriendship
        {
            get { return !IsConfirmed && !CurrentUserInitiatedFriendship; }
        }
        public DateTimeOffset? LastActivityTime
        {
            get { return lastActivityTime; }
            set { SetProperty(ref lastActivityTime, value); }
        }
    }
}