namespace TrackTimer.Services
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using TrackTimer.Contracts;

    public class GoProCamera : ICamera
    {
        private static TimeSpan DEFAULT_REQUEST_TIMEOUT = TimeSpan.FromMilliseconds(2000);

        private readonly string ipAddress;
        private readonly string password;

        public GoProCamera(string ipAddress, string password)
        {
            this.ipAddress = ipAddress;
            this.password = password;
        }

        public async Task<bool> PowerOn()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = DEFAULT_REQUEST_TIMEOUT;
                    string path = string.Format("http://{0}/bacpac/PW?t={1}&p=%01", ipAddress, password);
                    var powerOnResult = await httpClient.GetAsync(path);
                    return powerOnResult.IsSuccessStatusCode;
                }
            }
            catch (TaskCanceledException)
            {
                // Timeout, not fatal. Camera is probably not connected.
                return false;
            }
        }

        public async Task<bool> StartRecording()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = DEFAULT_REQUEST_TIMEOUT;
                    string path = string.Format("http://{0}/camera/SH?t={1}&p=%01", ipAddress, password);
                    var startCaptureResult = await httpClient.GetAsync(path);
                    return startCaptureResult.IsSuccessStatusCode;
                }
            }
            catch (TaskCanceledException)
            {
                // Timeout, not fatal. Camera is probably not connected.
                return false;
            }
        }

        public async Task<bool> StopRecording()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = DEFAULT_REQUEST_TIMEOUT;
                    string path = string.Format("http://{0}/camera/SH?t={1}&p=%00", ipAddress, password);
                    var stopCaptureResult = await httpClient.GetAsync(path);
                    return stopCaptureResult.IsSuccessStatusCode;
                }
            }
            catch (TaskCanceledException)
            {
                // Timeout, not fatal. Camera is probably not connected.
                return false;
            }
        }

        public async Task<bool> PowerOff()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = DEFAULT_REQUEST_TIMEOUT;
                    string path = string.Format("http://{0}/bacpac/PW?t={1}&p=%00", ipAddress, password);
                    var startCaptureResult = await httpClient.GetAsync(path);
                    return startCaptureResult.IsSuccessStatusCode;
                }
            }
            catch (TaskCanceledException)
            {
                // Timeout, not fatal. Camera is probably not connected.
                return false;
            }
        }
    }
}