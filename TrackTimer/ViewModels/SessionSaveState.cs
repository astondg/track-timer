namespace TrackTimer.ViewModels
{
    public enum ActivityStep
    {
        SessionBeginSave,
        SessionSavedToLocalStore,
        SessionUpdatedTrackCache,
        SessionSavedBestLapTimeToServer,
        SessionSaveComplete,
        SessionReloadTrackHistory,
    }

    public class ActivityState
    {
        public ActivityStep State { get; set; }
        public long TrackId { get; set; }
        public string TrackName { get; set; }
        public string SessionFileName { get; set; }
    }
}