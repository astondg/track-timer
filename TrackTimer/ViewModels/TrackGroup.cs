namespace TrackTimer.ViewModels
{
    using System.Collections.Generic;
    using System.Linq;
    using TrackTimer.Core.Models;

    public class TrackGroup : List<Track>
    {
        private readonly string key;

        public TrackGroup(string key)
            : this(key, new List<Track>()) { }

        public TrackGroup(string key, IEnumerable<Track> tracks)
        {
            this.key = key;
            if (tracks.Any())
                foreach (var track in tracks)
                    this.Add(track);
        }

        public string Key { get { return key; } }
    }
}