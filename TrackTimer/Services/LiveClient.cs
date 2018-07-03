namespace TrackTimer.Services
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using BugSense;
    using BugSense.Core.Model;
    using Microsoft.Live;
    using TrackTimer.Core.Resources;
    using Windows.Storage;

    public class LiveClient
    {
        private LiveConnectClient client;
        private string trackTimerFolderId;
        private LiveOperationResult meResult;

        public LiveClient()
        {
            client = null;
            trackTimerFolderId = null;
            meResult = null;
        }

        public bool IsConnected { get { return client != null; } }

        public async Task<string> GetUserAuthenticationToken(bool includeEmailAddress)
        {
            LiveConnectSessionStatus connectStatus;
            if (client != null || (connectStatus = await Connect(includeEmailAddress)) == LiveConnectSessionStatus.Connected)
            {
                return client.Session.AuthenticationToken;
            }
            else
            {
                return null;
            }
        }

        public async Task<object> GetFilesInTrackTimerFolder()
        {
            LiveConnectSessionStatus connectStatus;
            if (client != null || (connectStatus = await Connect()) == LiveConnectSessionStatus.Connected)
            {
                int numberOfFolderFiles = await FindTrackTimerFolder(client);
                if (numberOfFolderFiles == 0)
                    return null;

                dynamic clientResult = await client.GetAsync(trackTimerFolderId + "/files");
                return clientResult.Result.data;
            }
            return null;
        }

        public async Task<Stream> DownloadFileAsStream(string fileId)
        {
            LiveConnectSessionStatus connectStatus;
            if (client != null || (connectStatus = await Connect()) == LiveConnectSessionStatus.Connected)
            {
                LiveDownloadOperationResult fileDownloadResult = await client.DownloadAsync(fileId + "/content");
                return fileDownloadResult.Stream;
            }
            return null;
        }

        public async Task<string> UploadFileToTrackTimerFolder(StorageFile localFile, bool uploadOverWifi, bool overwrite = false)
        {
            LiveConnectSessionStatus connectStatus;
            if (client != null || (connectStatus = await Connect()) == LiveConnectSessionStatus.Connected)
            {
                client.BackgroundTransferPreferences = uploadOverWifi
                                                        ? BackgroundTransferPreferences.AllowBattery
                                                        : BackgroundTransferPreferences.AllowCellularAndBattery;

                Exception fileUploadException = null;
                try
                {
                    // Copy file to transfers folder
                    var sharedFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("shared");
                    var transfersFolder = await sharedFolder.GetFolderAsync("transfers");
                    var fileToUpload = await localFile.CopyAsync(transfersFolder, localFile.Name, NameCollisionOption.ReplaceExisting);

                    // Upload to SkyDrive
                    await FindTrackTimerFolder(client);
                    var localFileUri = new Uri(string.Format("/shared/transfers/{0}", fileToUpload.Name), UriKind.RelativeOrAbsolute);
                    var result = await client.BackgroundUploadAsync(trackTimerFolderId, localFileUri, overwrite ? OverwriteOption.Overwrite : OverwriteOption.Rename);
                    // Delete local file once upload is complete
                    if (!uploadOverWifi)
                        await fileToUpload.DeleteAsync(StorageDeleteOption.PermanentDelete);

                    // Return the OneDrive Id of the uploaded file
                    return result.Result != null && result.Result.ContainsKey("id")
                            ? result.Result["id"].ToString()
                            : string.Empty;
                }
                catch (Exception ex)
                {
                    fileUploadException = ex;
                }

                if (fileUploadException != null)
                {
                    var extraData = new LimitedCrashExtraDataList();
                    extraData.Add("Message", string.Format("Failed to upload file {0}", localFile.Name));
                    extraData.Add("UploadOverWifi", uploadOverWifi.ToString());
                    await BugSenseHandler.Instance.LogExceptionAsync(fileUploadException, extraData);
                }
            }
            return string.Empty;
        }

        public async Task<bool> RenameFileInTrackTimerFolder(string existingFilePath, string newFileName)
        {
            LiveConnectSessionStatus connectStatus;
            if (client != null || (connectStatus = await Connect()) == LiveConnectSessionStatus.Connected)
            {
                // Upload to SkyDrive
                await FindTrackTimerFolder(client);
                var result = await client.PutAsync(existingFilePath, new Dictionary<string, object> { { "name", newFileName } });
                return true;
            }
            return false;
        }

        public async Task<string> GetUsersFullName(CancellationToken ct)
        {
            LiveConnectSessionStatus connectStatus;
            if (client != null || (connectStatus = await Connect()) == LiveConnectSessionStatus.Connected)
            {
                LiveOperationResult operationResult;
                try
                {
                    operationResult = meResult != null
                                        ? meResult
                                        : meResult = await client.GetAsync("me", ct);
                }
                catch (TaskCanceledException)
                {
                    return string.Empty;
                }
                if (ct.IsCancellationRequested) return string.Empty;

                dynamic result = operationResult.Result;
                if (result.first_name != null &&
                    result.last_name != null)
                {
                    return string.Format(CultureInfo.CurrentCulture, "{0} {1}", result.first_name, result.last_name);
                }
            }
            return string.Empty;
        }

        public async Task<string> GetUsersEmailAddress(CancellationToken ct)
        {
            LiveConnectSessionStatus connectStatus;
            if (client != null || (connectStatus = await Connect()) == LiveConnectSessionStatus.Connected)
            {
                LiveOperationResult operationResult;
                try
                {
                    operationResult = meResult != null
                                        ? meResult
                                        : meResult = await client.GetAsync("me", ct);
                }
                catch (TaskCanceledException)
                {
                    return string.Empty;
                }
                if (ct.IsCancellationRequested) return string.Empty;

                dynamic result = operationResult.Result;
                if (result.emails != null)
                    return result.emails.account;
            }
            return string.Empty;
        }

        public async Task<IEnumerable<LivePendingUpload>> GetPendingUploads()
        {
            LiveConnectSessionStatus connectStatus;
            if (client != null || (connectStatus = await Connect()) == LiveConnectSessionStatus.Connected)
                return client.GetPendingBackgroundUploads();
            return Enumerable.Empty<LivePendingUpload>();
        }

        public async Task DeleteFileFromTrackTimerFolder(string filePath)
        {
            LiveConnectSessionStatus connectStatus;
            if (client != null || (connectStatus = await Connect()) == LiveConnectSessionStatus.Connected)
                await client.DeleteAsync(filePath);
        }

        public void Logout()
        {
            var liveAuth = new LiveAuthClient(Constants.MICROSOFT_LIVE_CLIENTID);
            liveAuth.Logout();
        }

        private async Task<LiveConnectSessionStatus> Connect(bool includeEmailAddress = false)
        {
            if (client != null)
                return LiveConnectSessionStatus.Connected;
            
            LiveAuthException liveConnectException = null;
            try
            {
                var liveAuth = new LiveAuthClient(Constants.MICROSOFT_LIVE_CLIENTID);
                var liveAuthResult = includeEmailAddress
                                        ? await liveAuth.InitializeAsync(new[]
                    {
                        Constants.MICROSOFT_LIVE_SCOPE_BASIC,
                        Constants.MICROSOFT_LIVE_SCOPE_EMAILS,
                        Constants.MICROSOFT_LIVE_SCOPE_SIGNIN,
                        Constants.MICROSOFT_LIVE_SCOPE_OFFLINEACCESS,
                        Constants.MICROSOFT_LIVE_SCOPE_SKYDRIVE,
                        Constants.MICROSOFT_LIVE_SCOPE_SKYDRIVEUPDATE
                    })
                                        : await liveAuth.InitializeAsync(new[]
                    {
                        Constants.MICROSOFT_LIVE_SCOPE_BASIC,
                        Constants.MICROSOFT_LIVE_SCOPE_SIGNIN,
                        Constants.MICROSOFT_LIVE_SCOPE_OFFLINEACCESS,
                        Constants.MICROSOFT_LIVE_SCOPE_SKYDRIVE,
                        Constants.MICROSOFT_LIVE_SCOPE_SKYDRIVEUPDATE
                    });

                if (liveAuthResult.Status == LiveConnectSessionStatus.Connected)
                {
                    client = CreateClientForSession(liveAuth.Session);
                }
                else
                {
                    liveAuthResult = includeEmailAddress
                                        ? await liveAuth.LoginAsync(new[]
                        {
                            Constants.MICROSOFT_LIVE_SCOPE_BASIC,
                            Constants.MICROSOFT_LIVE_SCOPE_EMAILS,
                            Constants.MICROSOFT_LIVE_SCOPE_SIGNIN,
                            Constants.MICROSOFT_LIVE_SCOPE_OFFLINEACCESS,
                            Constants.MICROSOFT_LIVE_SCOPE_SKYDRIVE,
                            Constants.MICROSOFT_LIVE_SCOPE_SKYDRIVEUPDATE
                        })
                                        : await liveAuth.LoginAsync(new[]
                        {
                            Constants.MICROSOFT_LIVE_SCOPE_BASIC,
                            Constants.MICROSOFT_LIVE_SCOPE_SIGNIN,
                            Constants.MICROSOFT_LIVE_SCOPE_OFFLINEACCESS,
                            Constants.MICROSOFT_LIVE_SCOPE_SKYDRIVE,
                            Constants.MICROSOFT_LIVE_SCOPE_SKYDRIVEUPDATE
                        });
                    client = liveAuthResult.Status == LiveConnectSessionStatus.Connected ? CreateClientForSession(liveAuth.Session) : null;
                    meResult = null;
                }
                return liveAuthResult.Status;
            }
            catch (TaskCanceledException)
            {
                client = null;
                return LiveConnectSessionStatus.NotConnected;
            }
            catch (LiveAuthException ex)
            {
                if (ex.ErrorCode.Equals("access_denied", StringComparison.OrdinalIgnoreCase))
                {
                    client = null;
                    return LiveConnectSessionStatus.NotConnected;
                }
                liveConnectException = ex;
            }
            if (liveConnectException != null)
            {
                var extraCrashData = new BugSense.Core.Model.LimitedCrashExtraDataList();
                extraCrashData.Add("ErrorCode", liveConnectException.ErrorCode);
                extraCrashData.Add("Message", liveConnectException.Message);
                await BugSenseHandler.Instance.LogExceptionAsync(liveConnectException, extraCrashData);
            }
            return LiveConnectSessionStatus.Unknown;
        }

        private static LiveConnectClient CreateClientForSession(LiveConnectSession session)
        {
            var client = new LiveConnectClient(session);
            return client;
        }

        private async Task<int> FindTrackTimerFolder(LiveConnectClient client)
        {
            dynamic trackTimerDataFolder = null;
            if (string.IsNullOrWhiteSpace(trackTimerFolderId))
            {
                var topLevelFolders = await client.GetAsync("me/skydrive/files?filter=folders");                
                foreach (dynamic folder in ((dynamic)topLevelFolders.Result).data)
                    if (folder.name == Constants.SKYDRIVE_FOLDER_NAME)
                    {
                        trackTimerFolderId = folder.id;
                        trackTimerDataFolder = folder;
                    }

                if (trackTimerDataFolder == null)
                {
                    var folderData = new Dictionary<string, object>();
                    folderData.Add("name", Constants.SKYDRIVE_FOLDER_NAME);
                    LiveOperationResult operationResult = await client.PostAsync("me/skydrive", folderData);
                    trackTimerDataFolder = operationResult.Result;
                    trackTimerFolderId = trackTimerDataFolder["id"].ToString();
                }
            }
            
            if (trackTimerDataFolder == null)
            {
                var clientResult = await client.GetAsync(trackTimerFolderId);
                trackTimerDataFolder = clientResult.Result;
            }

            return trackTimerDataFolder.count;
        }

        private byte[] Base64UrlDecode(string encodedSegment)
        {
            string s = encodedSegment;
            s = s.Replace('-', '+'); // 62nd char of encoding
            s = s.Replace('_', '/'); // 63rd char of encoding
            switch (s.Length % 4) // Pad with trailing '='s
            {
                case 0: break; // No pad chars in this case
                case 2: s += "=="; break; // Two pad chars
                case 3: s += "="; break; // One pad char
                default: throw new System.Exception("Illegal base64url string");
            }
            return Convert.FromBase64String(s); // Standard base64 decoder
        }
    }
}