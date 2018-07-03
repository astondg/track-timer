namespace TrackTimer.Core.Models
{
    public class User
    {
        public string id { get; set; }
        public string UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string LatestSessionFileName { get; set; }
        public string EmailAddress { get; set; }
        public bool ProfileIsPublic { get; set; }
        public bool ProfileIsSearchable { get; set; }
    }
}