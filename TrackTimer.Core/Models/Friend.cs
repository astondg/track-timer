namespace TrackTimer.Core.Models
{
    using System;

    public class Friend
    {
        public string id { get; set; }
        public string UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool IsConfirmed { get; set; }
        public bool CurrentUserInitiatedFriendship { get; set; }
        public DateTimeOffset? LastActivityTime { get; set; }
    }
}