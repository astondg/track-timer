namespace TrackTimer.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using TrackTimer.Contracts;
    using TrackTimer.Core.Geolocation;
    using TrackTimer.Core.Resources;
    using TrackTimer.Extensions;
    using TrackTimer.Resources;
    using Windows.Foundation;
    using Windows.Networking.Proximity;
    using Windows.Networking.Sockets;
    using Windows.Storage;
    using Windows.Storage.Streams;

    public class QstarzGeolocator : IGeolocator
    {
        protected enum GpsDataType
        {
            GPGGA,
            GPGSA,
            GPGSV,
            GPRMC,
            Unsupported = 100
        }

        private bool disposed;
        private StreamSocket socket;
        private PositionStatus positionStatus;
        private Encoding deviceEncoding;
        private Stream debugFileStream;
        private StreamWriter debugFileWriter;
        private Task deviceStreamReader;
        private Task dataReaderLoad;
        private CancellationTokenSource cancellationTokenSource;
        private static readonly string[] GpggaPropertyNames = new[] { "Timestamp", "Latitude", "NS", "Longitude", "EW", "FixQuality", "NumberOfSatellites", "HDOP", "Altitude", "GeoidHeight", "TimeSinceDGPSUpdate", "DGPSStationId" };
        private static readonly string[] GpgsaPropertyNames = new[] { "Mode", "FixType", "StationID1", "StationID2", "StationID3", "StationID4", "StationID5", "StationID6", "StationID7", "StationID8", "StationID9", "StationID10", "StationID11", "StationID12", "StationID13", "StationID14", "PDOP", "HDOP", "VDOP" };
        private static readonly string[] GpgsvPropertyNames = new[] { "NumberOfMessages", "MessageNumber", "TotalNumberOfSVsInView", "SV1PRN", "SV1Elevation", "SV1Azimuth", "SV1SNR", "SV2PRN", "SV2Elevation", "SV2Azimuth", "SV2SNR", "SV3PRN", "SV3Elevation", "SV3Azimuth", "SV3SNR", "SV4PRN", "SV4Elevation", "SV4Azimuth", "SV4SNR" };
        private static readonly string[] GprmcPropertyNames = new[] { "Timestamp", "Validity", "Latitude", "NS", "Longitue", "EW", "Speed", "Course", "Datestamp", "Variation", "VEW" };

        public QstarzGeolocator()
        {
            // NMEA is ASCII, possible encodings include: "iso-8859-1", "windows-1252", "us-ascii"
            deviceEncoding = Encoding.UTF8;
            debugFileStream = null;
            debugFileWriter = null;
            cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task<BluetoothConnectionStatus> ConnectToDevice(string hostCanonicalName)
        {
            PositionStatus = PositionStatus.Initializing;
            // GPS Data Standard - http://aprs.gids.nl/nmea/
            //PeerFinder.AlternateIdentities["Bluetooth:SDP"] = "{00001101-0000-1000-8000-00805f9b34fb}";
            PeerFinder.AlternateIdentities["Bluetooth:Paired"] = string.Empty;
            IReadOnlyList<PeerInformation> availableDevices;
            try
            {
                availableDevices = await PeerFinder.FindAllPeersAsync();
            }
            catch (Exception ex)
            {
                if ((uint)ex.HResult == 0x8007048F)
                {
                    PositionStatus = PositionStatus.Disabled;
                    return BluetoothConnectionStatus.BluetoothDisabled;
                }
                throw;
            }
            PeerInformation pi;
            if (availableDevices.Count == 0
                || (pi = availableDevices.FirstOrDefault(info => info.HostName.CanonicalName == hostCanonicalName)) == null)
            {
                PositionStatus = PositionStatus.Disabled;
                return BluetoothConnectionStatus.DeviceNotFound;
            }

            socket = new StreamSocket();
            try
            {
                DeviceName = pi.DisplayName;
                await socket.ConnectAsync(pi.HostName, "{00001101-0000-1000-8000-00805f9b34fb}");
            }
            catch (Exception)
            {
                return BluetoothConnectionStatus.DeviceNotFound;
            }
            deviceStreamReader = Task.Factory.StartNew(() => ReadStream(cancellationTokenSource.Token), cancellationTokenSource.Token);
            return BluetoothConnectionStatus.Connected;
        }

        public PositionAccuracy DesiredAccuracy { get; set; }

        public uint? DesiredAccuracyInMeters { get; set; }

        public PositionStatus LocationStatus { get; private set; }

        public double MovementThreshold { get; set; }

        public uint ReportInterval { get; set; }

        public string DeviceName { get; private set; }

        public event TypedEventHandler<IGeolocator, PositionChangedEventArgs> PositionChanged;

        public event TypedEventHandler<IGeolocator, StatusChangedEventArgs> StatusChanged;

        public event TypedEventHandler<IGeolocator, GeolocationErrorEventArgs> UnrecoverableError;

        protected PositionStatus PositionStatus
        {
            get { return positionStatus; }
            set
            {
                if (positionStatus == value)
                    return;
                positionStatus = value;
                if (StatusChanged != null)
                    StatusChanged(this, new StatusChangedEventArgs(positionStatus));
            }
        }

        public Task<Geocoordinate> GetGeopositionAsync()
        {
            throw new NotImplementedException();
        }

        public Task<Geocoordinate> GetGeopositionAsync(TimeSpan maximumAge, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        private async void ReadStream(CancellationToken cancellationToken)
        {
            if (socket == null || disposed || cancellationToken.IsCancellationRequested) return;

            Exception geolocatorException = null;

            try
            {
                await OpenDataFileForWrite();

                //string dataToProcess;

                using (var dataReader = new DataReader(socket.InputStream))
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var gpsDataSet = new Dictionary<GpsDataType, Dictionary<string, string>>();
                        string readValue;
                        string[] dataTypePropertyNames = null;
                        do
                        {
                            if (cancellationToken.IsCancellationRequested) break;

                            GpsDataType currentDataType;
                            do
                            {
                                if (cancellationToken.IsCancellationRequested) break;

                                if (dataReader.UnconsumedBufferLength < 300)
                                {
                                    dataReaderLoad = dataReader.LoadAsync(300).AsTask();
                                    await dataReaderLoad;
                                    // TODO - verify unit to int conversion
                                    //dataToProcess = await ReadString(dataReader, (int)dataReader.UnconsumedBufferLength);
                                }

                                if (cancellationToken.IsCancellationRequested || dataReader.UnconsumedBufferLength < 6)
                                    continue;

                                readValue = await ReadString(dataReader, 6);
                                if (string.IsNullOrWhiteSpace(readValue))
                                    break;

                                switch (readValue)
                                {
                                    case "$GPGGA":
                                        currentDataType = GpsDataType.GPGGA;
                                        dataTypePropertyNames = GpggaPropertyNames;
                                        break;
                                    case "$GPGSA":
                                        currentDataType = GpsDataType.GPGSA;
                                        dataTypePropertyNames = GpgsaPropertyNames;
                                        break;
                                    // TODO - Need to handle multiple GPGSV messages in one gpsDataSet
                                    //case "$GPGSV":
                                    //    currentDataType = GpsDataType.GPGSV;
                                    //    dataTypePropertyNames = GpgsvPropertyNames;
                                    //    break;
                                    case "$GPRMC":
                                        currentDataType = GpsDataType.GPRMC;
                                        dataTypePropertyNames = GprmcPropertyNames;
                                        break;
                                    default:
                                        currentDataType = GpsDataType.Unsupported;
                                        break;
                                }

                                if (currentDataType == GpsDataType.Unsupported)
                                {
                                    // TODO - refactor to improve performance, reading a sinlge char at a time is slow
                                    // Ignore record - i.e. read to end of line and then continue
                                    char character = 'a';
                                    while (dataReader.UnconsumedBufferLength >= 2 && character != Environment.NewLine.Last() && !cancellationToken.IsCancellationRequested)
                                        character = (await ReadString(dataReader, 1))[0];
                                    continue;
                                }

                                if (gpsDataSet.ContainsKey(currentDataType))
                                {
                                    var geocoordinate = CreateGeocoordinateFromRawData(gpsDataSet);
                                    if (geocoordinate != null && PositionChanged != null)
                                        PositionChanged(this, new PositionChangedEventArgs(geocoordinate));
                                    gpsDataSet.Clear();
                                }

                                int propertyNameIndex = 0;
                                Dictionary<string, string> dataTypeProperties = new Dictionary<string, string>();
                                readValue = await ReadString(dataReader, 1);
                                // Ignore first colon - relates to line type header
                                if (readValue == ",")
                                    readValue = await ReadString(dataReader, 1);

                                bool encounteredChecksum = false;
                                while (dataReader.UnconsumedBufferLength > 0 && readValue != Environment.NewLine && !cancellationToken.IsCancellationRequested)
                                {
                                    var stringBuilder = new StringBuilder();
                                    while (dataReader.UnconsumedBufferLength > 0 && readValue != "," && !cancellationToken.IsCancellationRequested)
                                    {
                                        if (readValue == "*")
                                        {
                                            encounteredChecksum = true;
                                            break;
                                        }

                                        stringBuilder.Append(readValue);
                                        readValue = await ReadString(dataReader, 1);
                                    }

                                    if (!encounteredChecksum && propertyNameIndex < dataTypePropertyNames.Length - 1)
                                    {
                                        dataTypeProperties.Add(dataTypePropertyNames[propertyNameIndex], stringBuilder.ToString());
                                        propertyNameIndex++;
                                    }
                                    else if (encounteredChecksum)
                                    {
                                        dataTypeProperties.Add("Checksum", await ReadString(dataReader, 2));
                                        if (dataReader.UnconsumedBufferLength >= 2)
                                            readValue = await ReadString(dataReader, 2);
                                        continue;
                                    }

                                    if (dataReader.UnconsumedBufferLength >= 1)
                                        readValue = await ReadString(dataReader, 1);
                                }

                                gpsDataSet.Add(currentDataType, dataTypeProperties);
                            } while (dataReader.UnconsumedBufferLength > 0);
                        } while (dataReader.UnconsumedBufferLength > 0);
                    }
                }

                CloseDataFile();
            }
            catch (Exception ex)
            {
                geolocatorException = ex;
            }

            if (geolocatorException != null)
            {
                if (UnrecoverableError != null)
                    UnrecoverableError(this, new GeolocationErrorEventArgs { Exception = geolocatorException, DeviceName = DeviceName });
            }
        }

        /// <summary>
        /// Reads a string from the data reader of the specified length and using the currently configured deviceEncoding 
        /// (will write to the open data file when running in debug)
        /// </summary>
        /// <param name="dataReader"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private async Task<string> ReadString(DataReader dataReader, int length)
        {
            string value = dataReader.ReadString(deviceEncoding, length);
#if DEBUG
            await debugFileWriter.WriteAsync(value);
            return value;
#else
            return await Task.FromResult(value);
#endif
        }

        /// <summary>
        /// In debug this opens a local file for writing QStarz data as it is received 
        /// in release this just returns a completed Task
        /// </summary>
        /// <returns></returns>
        private async Task OpenDataFileForWrite()
        {
#if DEBUG
            var sessionFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(AppConstants.LOCALFILENAME_QSTARZDATA, CreationCollisionOption.ReplaceExisting);
            debugFileStream = await sessionFile.OpenStreamForWriteAsync();
            debugFileWriter = new StreamWriter(debugFileStream, deviceEncoding);
#else
            await Task.FromResult(true);
#endif
        }

        private Geocoordinate CreateGeocoordinateFromRawData(Dictionary<GpsDataType, Dictionary<string, string>> rawDataSet)
        {
            double parsedValue1;
            double parsedValue2;
            double latitude;
            double longitude;
            double? accuracy;
            double? altitude;
            double? altitudeAccuracy;
            double? heading;
            double? positionAccuracy;
            double? speed;
            DateTimeOffset timestamp;

            if (!rawDataSet.ContainsKey(GpsDataType.GPGGA)
                || !rawDataSet.ContainsKey(GpsDataType.GPGSA)
                || !rawDataSet.ContainsKey(GpsDataType.GPRMC))
                return null;

            // NMEA data is in degrees & minutes so convert to decimal
            string nmeaLatitude = rawDataSet[GpsDataType.GPGGA]["Latitude"];
            if (nmeaLatitude.Length > 2 && double.TryParse(nmeaLatitude.Substring(2, nmeaLatitude.Length - 2), out parsedValue1) && double.TryParse(nmeaLatitude.Substring(0, 2), out parsedValue2))
            {
                parsedValue1 /= 60;
                parsedValue2 = parsedValue2 + parsedValue1;
                string ns = rawDataSet[GpsDataType.GPGGA]["NS"];
                latitude = !string.IsNullOrWhiteSpace(ns) && ns.Equals("S", StringComparison.OrdinalIgnoreCase)
                            ? -parsedValue2 : parsedValue2;
            }
            else
            {
                return null;
            }
            // NMEA data is in degrees & minutes so convert to decimal
            string nmeaLongitude = rawDataSet[GpsDataType.GPGGA]["Longitude"];
            if (nmeaLongitude.Length > 3 && double.TryParse(nmeaLongitude.Substring(3, nmeaLongitude.Length - 3), out parsedValue1) && double.TryParse(nmeaLongitude.Substring(0, 3), out parsedValue2))
            {
                parsedValue1 /= 60;
                parsedValue2 = parsedValue2 + parsedValue1;
                string ew = rawDataSet[GpsDataType.GPGGA]["EW"];
                longitude = !string.IsNullOrWhiteSpace(ew) && ew.Equals("W", StringComparison.OrdinalIgnoreCase)
                            ? -parsedValue2 : parsedValue2;
            }
            else
            {
                return null;
            }
            int parsedFixQuality;
            if (int.TryParse(rawDataSet[GpsDataType.GPGGA]["FixQuality"], out parsedFixQuality))
            {
                if (parsedFixQuality == 0 && PositionStatus == Core.Geolocation.PositionStatus.Ready)
                    PositionStatus = Core.Geolocation.PositionStatus.NoData;
                if (parsedFixQuality > 0 && PositionStatus != Core.Geolocation.PositionStatus.Ready)
                    PositionStatus = Core.Geolocation.PositionStatus.Ready;
            }
            altitude = double.TryParse(rawDataSet[GpsDataType.GPGGA]["Altitude"], out parsedValue1)
                        ? parsedValue1 : (double?)null;
            accuracy = double.TryParse(rawDataSet[GpsDataType.GPGGA]["HDOP"], out parsedValue1)
                        ? parsedValue1 : double.MaxValue;

            string vdop;
            altitudeAccuracy = rawDataSet[GpsDataType.GPGSA].TryGetValue("VDOP", out vdop) && double.TryParse(vdop, out parsedValue1)
                                ? parsedValue1 : (double?)null;
            string pdop;
            positionAccuracy = rawDataSet[GpsDataType.GPGSA].TryGetValue("PDOP", out pdop) && double.TryParse(pdop, out parsedValue1)
                                ? parsedValue1 : (double?)null;

            heading = double.TryParse(rawDataSet[GpsDataType.GPRMC]["Course"], out parsedValue1)
                                ? parsedValue1 : (double?)null;
            speed = double.TryParse(rawDataSet[GpsDataType.GPRMC]["Speed"], out parsedValue1)
                                ? parsedValue1 * Constants.KNOTS_TO_METRES_PER_SECOND : (double?)null;

            // Timestamp will remain UTC at this point to match the WP8 internal geolocator
            string dateTime = rawDataSet[GpsDataType.GPRMC]["Datestamp"] + rawDataSet[GpsDataType.GPRMC]["Timestamp"];
            timestamp = DateTimeOffset.TryParse(dateTime, out timestamp)
                        ? timestamp : DateTime.UtcNow;

            return new Geocoordinate
            {
                Accuracy = accuracy.Value,
                Altitude = altitude,
                AltitudeAccuracy = altitudeAccuracy,
                Heading = heading,
                Latitude = latitude,
                Longitude = longitude,
                PositionSource = Core.Geolocation.PositionSource.Satellite,
                SatelliteData = new GeocoordinateSatelliteData
                {
                    HorizontalDilutionOfPrecision = accuracy,
                    PositionDilutionOfPrecision = positionAccuracy,
                    VerticalDilutionOfPrecision = altitudeAccuracy
                },
                Speed = speed,
                Timestamp = timestamp
            };
        }

        private void CloseDataFile()
        {
            if (debugFileWriter != null)
            {
                debugFileWriter.Dispose();
                debugFileWriter = null;
            }

            if (debugFileStream != null)
            {
                debugFileStream.Dispose();
                debugFileStream = null;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;
            if (disposing)
            {
                if (cancellationTokenSource != null)
                {
                    cancellationTokenSource.Cancel(true);
                    if (deviceStreamReader != null)
                        deviceStreamReader.Wait();
                    if (dataReaderLoad != null)
                        dataReaderLoad.Wait();
                    cancellationTokenSource.Dispose();
                    cancellationTokenSource = null;
                }
                if (socket != null)
                {
                    socket.Dispose();
                    socket = null;
                }
                CloseDataFile();
            }
            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}