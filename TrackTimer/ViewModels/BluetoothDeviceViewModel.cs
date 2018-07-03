namespace TrackTimer.ViewModels
{
    using System.Windows.Media;
    using TrackTimer.Core.ViewModels;

    public class BluetoothDeviceViewModel : BaseViewModel
    {
        private string displayName;
        private string canonicalName;

        public string DisplayName
        {
            get { return displayName; }
            set { SetProperty(ref displayName, value); }
        }
        public string CanonicalName
        {
            get { return canonicalName; }
            set { SetProperty(ref canonicalName, value); }
        }
    }
}