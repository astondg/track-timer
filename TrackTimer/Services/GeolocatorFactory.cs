namespace TrackTimer.Services
{
    using System.Threading.Tasks;
    using TrackTimer.Contracts;
    using TrackTimer.Core.Resources;

    public class GeolocatorFactory : IGeolocatorFactory
    {
        string[] deviceCanonicalNames;

        public GeolocatorFactory(params string[] deviceCanonicalNames)
        {
            this.deviceCanonicalNames = deviceCanonicalNames;
        }

        public async Task<IGeolocator> GetGeolocator()
        {
            IGeolocator geoLocator = null;
            if (deviceCanonicalNames != null && deviceCanonicalNames.Length > 0)
            {
                var qstarsGeolocator = new QstarzGeolocator();
                foreach (string deviceName in deviceCanonicalNames)
	            {
                    BluetoothConnectionStatus status = await qstarsGeolocator.ConnectToDevice(deviceName);
                    if (status == BluetoothConnectionStatus.BluetoothDisabled)
                    {
                        qstarsGeolocator.Dispose();
                        break;
                    }
                    if (status == BluetoothConnectionStatus.DeviceNotFound)
                    {
                        qstarsGeolocator.Dispose();
                        continue;
                    }
                    geoLocator = qstarsGeolocator;
                    break;
	            }
            }
            if (geoLocator == null)
                geoLocator = new InternalGeolocator();

            return geoLocator;
        }
    }
}