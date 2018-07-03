namespace TrackTimer.Core.Models
{
    using System.Collections.Generic;

    public class TrackSession : TrackSessionHeader
    {
        public IEnumerable<Lap> Laps { get; set; }
    }
}