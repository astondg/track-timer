namespace TrackTimer.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.IO.IsolatedStorage;
    using System.Linq;
    using System.Net.Http;
    using System.Spatial;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using System.Windows.Threading;
    using BugSense;
    using BugSense.Core.Model;
    using GalaSoft.MvvmLight.Command;
    using Microsoft.Phone.Controls.Primitives;
    using Microsoft.Phone.Maps.Services;
    using Microsoft.Phone.Net.NetworkInformation;
    using Microsoft.WindowsAzure.MobileServices;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using TrackTimer.Core.Extensions;
    using TrackTimer.Core.Models;
    using TrackTimer.Core.Resources;
    using TrackTimer.Core.ViewModels;
    using TrackTimer.Extensions;
    using TrackTimer.Resources;
    using TrackTimer.Services;
    using Windows.Devices.Geolocation;
    using Windows.Foundation;
    using Windows.Networking.Connectivity;
    using Windows.Storage;

    public class AppViewModel : BaseViewModel
    {
        private bool isDataLoading;
        private bool isTrackLoading;
        private bool isAuthenticated;
        private bool isAuthenticating;
        private bool showIntertitialAd;
        private bool appIsRunningInBackground;
        private bool isRegisteredForNetworkStatusChanged;
        private bool canOperateOnFriendsCollection;
        private string speedUnitText;
        private string lengthUnitText;
        private string loadingStatusText;
        private MobileServiceUser user;
        private User userProfile;
        private Track currentTrackItem;
        private BestLapViewModel currentBestLap;
        private SettingsViewModel settings;
        private TrackViewModel currentTrack;
        private TimerViewModel timerViewModel;
        private BackgroundTransfersViewModel backgroundTransfers;
        private VehicleViewModel currentVehicle;
        private List<CancellationTokenSource> loadingCancellationTokens;
        private Queue<bool> dataLoaders;
        private ICommand shareTrack;
        private ICommand deleteTrack;
        private ICommand deleteSector;
        private ICommand saveTrack;
        private ICommand acceptFriendCommand;
        private ICommand deleteFriendCommand;
        private ICommand addFriendCommand;
        private ILoopingSelectorDataSource degreesData;

        public event TypedEventHandler<AppViewModel, TrackViewModel> TrackLoadComplete;
        public event TypedEventHandler<AppViewModel, TrackViewModel> TrackHistoryLoadComplete;
        public event TypedEventHandler<AppViewModel, string> NotifyUser;
        public event TypedEventHandler<AppViewModel, ActivityStep> CurrentSessionSavedToLocalStore;

        public AppViewModel()
        {
            isAuthenticated = false;
            isRegisteredForNetworkStatusChanged = false;
            canOperateOnFriendsCollection = true;
            Track = new TrackViewModel { Name = AppResources.Text_Default_Loading, BestLap = new BestLapViewModel { IsUnofficial = false } };
            AllTracks = new ObservableCollection<TrackGroup>();
            ActivityFeed = new ObservableCollection<ActivityViewModel>();
            Friends = new ObservableCollection<FriendViewModel>();
            Settings = new SettingsViewModel();
            Settings.PropertyChanged += Settings_PropertyChanged;
            SpeedUnitText = Settings.IsMetricUnits ? AppResources.Text_Unit_MetricSpeed : AppResources.Text_Unit_ImperialSpeed;
            LengthUnitText = Settings.IsMetricUnits ? AppResources.Text_Unit_Kilometres : AppResources.Text_Unit_Miles;
            degreesData = new DegreesLoopingSelectorDataSource();
            backgroundTransfers = new BackgroundTransfersViewModel();
            loadingCancellationTokens = new List<CancellationTokenSource>();
            dataLoaders = new Queue<bool>();
        }

        public Dispatcher dispatcher { get; set; }
        public ActivityState CurrentSessionSaveState { get; set; }
        public bool NeedToResumeMusic { get; set; }
        public bool IsTiming { get; set; }

        public string LoadingStatusText
        {
            get { return loadingStatusText; }
            set { SetProperty(ref loadingStatusText, value); }
        }
        public bool IsDataLoading
        {
            get { return isDataLoading; }
            private set { SetProperty(ref isDataLoading, value); }
        }
        public bool DataIsLoaded
        {
            get;
            private set;
        }
        public bool IsTrackLoading
        {
            get { return isTrackLoading; }
            private set { SetProperty(ref isTrackLoading, value); }
        }
        public bool IsTrackLoaded
        {
            get;
            private set;
        }
        public bool SystemTrayIsVisible
        {
            get { return dataLoaders.Count > 0; }
        }
        public bool IsAuthenticated
        {
            get { return isAuthenticated; }
            internal set { SetProperty(ref isAuthenticated, value); }
        }
        public bool IsAuthenticating
        {
            get { return isAuthenticating; }
            internal set { SetProperty(ref isAuthenticating, value); }
        }
        public bool ShowInterstitialAd
        {
            get { return showIntertitialAd; }
            internal set { SetProperty(ref showIntertitialAd, value); }
        }
        public bool AppIsRunningInBackground
        {
            get { return appIsRunningInBackground; }
            set { SetProperty(ref appIsRunningInBackground, value); }
        }
        public bool CanOperateOnFriendCollection
        {
            get { return canOperateOnFriendsCollection; }
            set { SetProperty(ref canOperateOnFriendsCollection, value); }
        }
        public string SpeedUnitText
        {
            get { return speedUnitText; }
            set { SetProperty(ref speedUnitText, value); }
        }
        public string LengthUnitText
        {
            get { return lengthUnitText; }
            set { SetProperty(ref lengthUnitText, value); }
        }
        public User UserProfile
        {
            get { return userProfile; }
        }
        public Track CurrentTrackItem
        {
            get { return currentTrackItem; }
            set { SetProperty(ref currentTrackItem, value); }
        }
        public SettingsViewModel Settings
        {
            get { return settings; }
            set { SetProperty(ref settings, value); }
        }
        public TrackViewModel Track
        {
            get { return currentTrack; }
            set { SetProperty(ref currentTrack, value); }
        }
        public TimerViewModel Timer
        {
            get { return timerViewModel; }
            set { SetProperty(ref timerViewModel, value); }
        }
        public BackgroundTransfersViewModel BackgroundTransfers
        {
            get { return backgroundTransfers; }
            set { SetProperty(ref backgroundTransfers, value); }
        }
        public VehicleViewModel CurrentVehicle
        {
            get { return currentVehicle; }
            set
            {
                if (SetProperty(ref currentVehicle, value) && currentVehicle != null)
                    Settings.CurrentVehicle = value;
            }
        }

        public ICommand ShareTrack
        {
            get
            {
                if (shareTrack == null)
                    shareTrack = new RelayCommand(async () => await UploadTrackToServer(), () => Track.IsLocal);
                return shareTrack;
            }
        }
        public ICommand DeleteTrack
        {
            get
            {
                if (deleteTrack == null)
                    deleteTrack = new RelayCommand(async () => await DeleteLocalTrack(), () => Track.IsLocal);
                return deleteTrack;
            }
        }
        public ICommand DeleteSector
        {
            get
            {
                if (deleteSector == null)
                    deleteSector = new RelayCommand<TrackSectorViewModel>((sector) => Track.Sectors.Remove(sector), (sector) => Track.IsLocal);
                return deleteSector;
            }
        }
        public ICommand SaveTrack
        {
            get
            {
                if (saveTrack == null)
                    saveTrack = new RelayCommand<TrackViewModel>(async track =>
                        {
                            IsDataLoading = true;
                            var trackModel = track.AsModel(Settings.IsMetricUnits);
                            Track.FileNameWhenLoaded = await CacheTrack(trackModel, track.History != null ? track.History.Sessions : Enumerable.Empty<TrackSessionViewModel>(), oldFileName: Track.FileNameWhenLoaded, overwriteLocal: true);
                            if (track.Sectors.Any() && !track.Sectors.Last().IsFinishLine)
                            {
                                track.Sectors.Clear();
                                foreach (var sector in trackModel.Sectors)
                                    track.Sectors.Add(sector.AsViewModel());
                            }
                            IsDataLoading = false;
                        }, track => !IsDataLoading);
                return saveTrack;
            }
        }

        public ICommand AcceptFriend
        {
            get
            {
                if (acceptFriendCommand == null)
                {
                    acceptFriendCommand = new RelayCommand<FriendViewModel>(async friendVm =>
                    {
                        bool refreshActivityFeed = false;
                        try
                        {
                            CanOperateOnFriendCollection = false;
                            Exception friendException = null;

                            try
                            {
                                var friend = friendVm.AsModel();
                                friend.IsConfirmed = true;
                                var friendsTable = App.MobileService.GetTable<Friend>();
                                await friendsTable.UpdateAsync(friend);
                                friendVm.IsConfirmed = friend.IsConfirmed;
                                refreshActivityFeed = true;
                            }
                            catch (Exception ex)
                            {
                                friendException = ex;
                            }

                            if (friendException != null)
                            {
                                var errorData = new LimitedCrashExtraDataList();
                                errorData.Add("Message", string.Format("Failed to accept Friend relationship between {0} & {1}",
                                                                       App.MobileService.CurrentUser.UserId, friendVm.UserId));
                                BugSenseHandler.Instance.UserIdentifier = App.MobileService.CurrentUser.UserId;
                                await BugSenseHandler.Instance.LogExceptionAsync(friendException, errorData);
                            }
                        }
                        finally
                        {
                            CanOperateOnFriendCollection = true;
                        }

                        if (refreshActivityFeed)
                            await LoadActivityFeed();
                    });
                }

                return acceptFriendCommand;
            }
        }

        public ICommand DeleteFriend
        {
            get
            {
                if (deleteFriendCommand == null)
                {
                    deleteFriendCommand = new RelayCommand<FriendViewModel>(async friendVm =>
                    {
                        try
                        {
                            CanOperateOnFriendCollection = false;
                            Exception friendException = null;

                            try
                            {
                                var friend = friendVm.AsModel();
                                friend.IsConfirmed = true;
                                var friendsTable = App.MobileService.GetTable<Friend>();
                                await friendsTable.DeleteAsync(friend);
                                Friends.Remove(friendVm);
                            }
                            catch (Exception ex)
                            {
                                friendException = ex;
                            }

                            if (friendException != null)
                            {
                                var errorData = new LimitedCrashExtraDataList();
                                errorData.Add("Message", string.Format("Failed to accept Friend relationship between {0} & {1}",
                                                                       App.MobileService.CurrentUser.UserId, friendVm.UserId));
                                BugSenseHandler.Instance.UserIdentifier = App.MobileService.CurrentUser.UserId;
                                await BugSenseHandler.Instance.LogExceptionAsync(friendException, errorData);
                            }
                        }
                        finally
                        {
                            CanOperateOnFriendCollection = true;
                        }
                    });
                }

                return deleteFriendCommand;
            }
        }

        public ICommand AddFriend
        {
            get
            {
                if (addFriendCommand == null)
                {
                    addFriendCommand = new RelayCommand<BestLapViewModel>(async bestLap =>
                    {
                        if ((!IsAuthenticated || !InternetConnectionIsAvailable())
                            || string.Equals(bestLap.UserName, App.MobileService.CurrentUser.UserId)
                            || Friends.Any(f => string.Equals(f.UserId, bestLap.UserName)))
                            return;

                        try
                        {
                            CanOperateOnFriendCollection = false;
                            var user = new User { UserId = bestLap.UserName };
                            FriendViewModel addedFriend = await AddFriendViewModel.CreateFriendRequestOnServer(user);
                            await LoadFriends();
                            using (var cancelationToken = new CancellationTokenSource())
                            {
                                loadingCancellationTokens.Add(cancelationToken);
                                await LoadLeaderboardForTrack(cancelationToken.Token);
                                loadingCancellationTokens.Remove(cancelationToken);
                            }
                        }
                        finally
                        {
                            CanOperateOnFriendCollection = true;
                        }
                    });
                }

                return addFriendCommand;
            }
        }

        public ObservableCollection<TrackGroup> AllTracks { get; private set; }
        public ObservableCollection<ActivityViewModel> ActivityFeed { get; private set; }
        public ObservableCollection<FriendViewModel> Friends { get; private set; }
        public ILoopingSelectorDataSource DegreesData { get { return degreesData; } }

        public async Task<bool> Authenticate(bool inlcudeEmailAddress)
        {
            ShowSystemTray();
            LoadingStatusText = AppResources.Text_LoadingStatus_Authenticating;

            if (IsAuthenticating || (!inlcudeEmailAddress && user != null) || !InternetConnectionIsAvailable())
            {
                HideSystemTray();
                return false;
            }
            IsAuthenticating = true;
            IsAuthenticated = false;
            if (Settings != null)
                Settings.IsAuthenticated = false;
            user = null;

            try
            {
                var authenticationToken = await App.LiveClient.GetUserAuthenticationToken(inlcudeEmailAddress);
                if (authenticationToken != null)
                {
                    var jsonAuthenticationToken = JObject.Parse(@"{""authenticationToken"": """ + authenticationToken + @"""}");
                    user = await App.MobileService.LoginAsync(MobileServiceAuthenticationProvider.MicrosoftAccount, jsonAuthenticationToken);
                }
                if (user == null)
                {
                    HideSystemTray();
                    return false;
                }
                var userTable = App.MobileService.GetTable<User>();
                var userProfiles = await userTable.Where(u => u.UserId == user.UserId).ToListAsync();
                if (!userProfiles.Any())
                {
                    using (var cancellationToken = new CancellationTokenSource())
                    {
                        var usersFullName = await App.LiveClient.GetUsersFullName(cancellationToken.Token);
                        string usersEmailAddress = inlcudeEmailAddress ? await App.LiveClient.GetUsersEmailAddress(cancellationToken.Token) : null;
                        string[] usersNameTokens;
                        if (string.IsNullOrWhiteSpace(usersFullName) || (usersNameTokens = usersFullName.Split(' ')).Length < 2) return true;
                        userProfile = new User { UserId = user.UserId, FirstName = usersNameTokens[0], LastName = usersNameTokens[1], EmailAddress = usersEmailAddress, ProfileIsPublic = false, ProfileIsSearchable = true };
                        await userTable.InsertAsync(userProfile);
                    }
                }
                else
                {
                    userProfile = userProfiles.Single();
                    if (inlcudeEmailAddress && string.IsNullOrWhiteSpace(userProfile.EmailAddress))
                    {
                        using (var cancellationToken = new CancellationTokenSource())
                        {
                            userProfile.EmailAddress = await App.LiveClient.GetUsersEmailAddress(cancellationToken.Token);
                            await userTable.UpdateAsync(userProfile);
                        }
                    }
                }
            }
            catch (Exception)
            {
                HideSystemTray();
                return false;
            }
            finally
            {
                IsAuthenticating = false;
                HideSystemTray();
            }
            IsAuthenticated = true;
            if (Settings != null)
                Settings.IsAuthenticated = true;
            HideSystemTray();
            return true;
        }

        public async Task LoadData()
        {
            if (IsDataLoading) return;
            IsDataLoading = true;
            DataIsLoaded = false;
            IsTrackLoaded = false;
            ShowSystemTray();

            this.PropertyChanged -= AppViewModel_PropertyChanged;
            this.PropertyChanged += AppViewModel_PropertyChanged;
             
            LoadingStatusText = AppResources.Text_LoadingStatus_LoadingSettings;
            await Settings.LoadData(IsAuthenticated ? App.MobileService.CurrentUser.UserId : null, userProfile);
            await LoadTracks();
            await LoadActivityFeed();

            if (Settings.LastActivityState != null)
            {
                await ProcessCancelledSessionSave(App.ViewModel.Settings.LastActivityState, true);
                Settings.LastActivityState = null;
            }

            IsDataLoading = false;
            DataIsLoaded = true;
            HideSystemTray();
        }

        public void CreateTimerViewModelForCurrentSettings()
        {
            var camera = !Settings.IsTrial && Settings.StartCameraWithTiming
                            ? new GoProCamera(AppConstants.DEFAULT_GOPRO_IPADDRESS, Settings.CameraPassword)
                            : null;
            var geolocatorFactory = Settings.IsTrial
                                    ? new GeolocatorFactory()
                                    : new GeolocatorFactory(Settings.EnabledBuetoothDevicesCanonicalNames.ToArray());
            Timer = new TimerViewModel(App.LiveClient, Track.BestLap, Track.Sectors, geolocatorFactory, camera, dispatcher);
            Timer.TimingStopped += Timer_TimingStopped;
        }

        public async Task<StorageFile> SaveSessionToLocalStore(TrackSessionViewModel session, SessionSaveType saveType = SessionSaveType.New)
        {
            string fileName = !string.IsNullOrWhiteSpace(session.LocalFilePath)
                                ? session.LocalFilePath
                                : string.Format("{0}_{1}{2}_{3}.json", session.TrackName, session.Vehicle.Make, session.Vehicle.Model, session.Timestamp.ToString("yyyyMMddHHmmssFF"))
                                        .Replace(" ", "").Replace(",", "").Replace(":", "");
            session.LocalFilePath = fileName;

            var localSessionsFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(AppConstants.LOCALFOLDERNAME_SESSIONS, CreationCollisionOption.OpenIfExists);
            StorageFile sessionFile;
            if (saveType == SessionSaveType.New)
            {
                var trackSession = session.AsModel(Settings.IsMetricUnits);
                sessionFile = await localSessionsFolder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);
                await sessionFile.WriteJsonToFile(trackSession);
            }
            else if (saveType == SessionSaveType.ReadIfExists)
            {
                sessionFile = await localSessionsFolder.GetFileAsync(fileName);
            }
            else if (saveType == SessionSaveType.Replace)
            {
                var trackSession = session.AsModel(Settings.IsMetricUnits);
                sessionFile = await localSessionsFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
                await sessionFile.WriteJsonToFile(trackSession);
            }
            else
            {
                throw new InvalidOperationException(string.Format("The saveType {0} is not supported", saveType));
            }

            return sessionFile;
        }

        public async Task<string> SaveSessionToCloudStore(TrackSessionViewModel session, StorageFile localSessionFile)
        {
            string fileId = await App.LiveClient.UploadFileToTrackTimerFolder(localSessionFile, Settings.UploadSessionsOverWifi);
            if (!string.IsNullOrEmpty(fileId))
            {
                await dispatcher.InvokeAsync(() =>
                {
                    session.IsUploaded = true;
                    session.Location = TrackSessionLocation.ServerWithLocalFile;
                });
            }

            return fileId;
        }

        public async Task DeleteSession(TrackSessionViewModel session, bool deleteLocalOnly = true)
        {
            // Delete the local file
            if (session.Location == TrackSessionLocation.LocalFile || session.Location == TrackSessionLocation.ServerWithLocalFile)
            {
                var localTrackFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(AppConstants.LOCALFOLDERNAME_SESSIONS, CreationCollisionOption.OpenIfExists);
                var localFile = await localTrackFolder.GetFileAsync(session.LocalFilePath);
                await localFile.DeleteAsync();
                if (session.Location == TrackSessionLocation.ServerWithLocalFile)
                    session.Location = TrackSessionLocation.ServerFile;
            }

            // Delete the file in OneDrive
            if (session.Location == TrackSessionLocation.ServerFile || session.Location == TrackSessionLocation.ServerWithLocalFile)
            {
                if (deleteLocalOnly) return;
                await App.LiveClient.DeleteFileFromTrackTimerFolder(session.ServerFilePath);
            }

            // Remove the session from memory
            var selectedSession = App.ViewModel.Track.History.Sessions.SingleOrDefault(s => ((s.TrackId > 0 && s.TrackId == session.TrackId) || (s.TrackId < 1 && s.TrackName == session.TrackName)) && s.Timestamp == session.Timestamp);
            App.ViewModel.Track.History.Sessions.Remove(selectedSession);
        }

        public async Task LoadFriends()
        {
            try
            {
                // Friends are filtered on the server to only return those related to the current user
                var friends = await App.MobileService.GetTable<Friend>().ToEnumerableAsync();

                // Show unconfirmed friends first so that they can be easily managed
                App.ViewModel.Friends.Clear();
                foreach (var friend in friends.Where(f => !f.IsConfirmed))
                    App.ViewModel.Friends.Add(friend.AsViewModel());
                foreach (var friend in friends.Where(f => f.IsConfirmed))
                    App.ViewModel.Friends.Add(friend.AsViewModel());
            }
            catch (MobileServiceInvalidOperationException ex)
            {
                var crashData = new List<CrashExtraData>
                {
                    new CrashExtraData("userEmail", UserProfile.EmailAddress)
                };
                if (ex.Response != null)
                {
                    crashData.Add(new CrashExtraData("responseStatusCode", ex.Response.StatusCode.ToString()));
                }
                if (ex.Response?.Content != null)
                {
                    crashData.Add(new CrashExtraData("response", await ex.Response.Content.ReadAsStringAsync()));
                }
                string errorId = await BugSenseHandler.Instance.LogExceptionWithId(user, ex, crashData.ToArray());
                if (NotifyUser != null)
                    NotifyUser(this, string.Format(AppResources.Text_Error_UnableToRetrieveFriends, errorId));
            }
        }

        public void CancelAllActivities()
        {
            loadingCancellationTokens.ForEach(t => t.Cancel());
        }

        private async Task LoadActivityFeed()
        {
            ShowSystemTray();
            ActivityFeed.Clear();

            LoadingStatusText = AppResources.Text_LoadingStatus_LoadingActivityFeed;

            // Load items from activity feed cache
            var localActivityFeedFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(AppConstants.LOCALFILENAME_ACTIVITYFEED, CreationCollisionOption.OpenIfExists);
            var existingActivityFeedItems = await localActivityFeedFile.ReadJsonFileAs<IEnumerable<Activity>>();
            foreach (var activity in (existingActivityFeedItems ?? Enumerable.Empty<Activity>()).OrderByDescending(a => a.CreatedAt))
                ActivityFeed.Add(activity.AsViewModel());

            if (!IsAuthenticated)
            {
                HideSystemTray();
                return;
            }

            // Load new items from server
            DateTimeOffset mostRecentActivityDate = Settings.LastActivityFeedRefresh;
            IEnumerable<Activity> newActivityFeedItems = null;
            if (InternetConnectionIsAvailable())
            {
                try
                {
                    newActivityFeedItems = await App.MobileService.GetTable<Activity>().Where(a => a.CreatedAt > mostRecentActivityDate).ToEnumerableAsync();
                }
                catch (MobileServiceInvalidOperationException)
                {
                    LoadingStatusText = AppResources.Text_LoadingStatus_ServerUnavailable;
                }
            }
            else
            {
                LoadingStatusText = AppResources.Text_LoadingStatus_ServerUnavailable;
            }

            bool updateLastActivityFeedRefresh = false;
            foreach (var activity in (newActivityFeedItems ?? Enumerable.Empty<Activity>()).OrderByDescending(a => a.CreatedAt))
            {
                ActivityFeed.Add(activity.AsViewModel());
                if (activity.CreatedAt > mostRecentActivityDate)
                {
                    mostRecentActivityDate = activity.CreatedAt;
                    updateLastActivityFeedRefresh = true;
                }
            }

            if (updateLastActivityFeedRefresh)
            {
                await localActivityFeedFile.WriteJsonToFile(newActivityFeedItems);
                Settings.LastActivityFeedRefresh = mostRecentActivityDate;
            }

            HideSystemTray();
        }

        private async Task LoadTracks(long? selectedTrackId = null)
        {
            ShowSystemTray();
            AllTracks.Clear();

            // Lookup tracks local to current position
            LoadingStatusText = AppResources.Text_LoadingStatus_FindingClosestCircuit;
            Geoposition position = await GetCurrentGeoposition();
            Track serverTrack = null;
            bool loadedDataFromServer = true;
            List<Tuple<string, Track>> allTracks = new List<Tuple<string, Track>>();

            Track newTrack = null;
            if (position != null)
            {
                ReverseGeocodeQuery reverseGeocode = new ReverseGeocodeQuery { GeoCoordinate = position.Coordinate.ToGeoCoordinate() };
                var locations = await reverseGeocode.GetMapLocationsAsync();
                var location = locations.FirstOrDefault();

                newTrack = new Track
                {
                    id = -1,
                    Name = string.Format("{0} Lat: {1}, Lng: {2}", AppResources.Text_Blurb_New, position.Coordinate.Latitude.ToString("F3", CultureInfo.CurrentCulture), position.Coordinate.Longitude.ToString("F3", CultureInfo.CurrentCulture)),
                    Country = location != null ? location.Information.Address.Country : AppResources.Text_Default_UnknownCountry,
                    Latitude = position.Coordinate.Latitude,
                    Longitude = position.Coordinate.Longitude,
                    Length = 0,
                    Description = AppResources.Text_Blurb_UnknownTrackDescription,
                    Sectors = new List<TrackSector>(),
                };
                allTracks.Add(Tuple.Create("NEW", newTrack));
            }

            if (InternetConnectionIsAvailable())
            {
                try
                {
                    IEnumerable<Track> nearbyTracks;
                    if (position != null && position.Coordinate != null)
                    {
                        string locationQuery = string.Format("{0}, {1}, {2}", position.Coordinate.Latitude.ToString(CultureInfo.InvariantCulture), position.Coordinate.Longitude.ToString(CultureInfo.InvariantCulture), AppConstants.TRACK_SEARCH_BUFFER.ToString(CultureInfo.InvariantCulture));
                        nearbyTracks = await App.MobileService.GetTable<Track>().Where(t => t.Filter == locationQuery).ToEnumerableAsync();
                        serverTrack = nearbyTracks.FirstOrDefault();
                        if (serverTrack != null)
                            selectedTrackId = serverTrack.id;
                    }
                    else
                    {
                        nearbyTracks = Enumerable.Empty<Track>();
                    }

                    // TODO - Verify performance of the below
                    bool retrieveAllTracks = Settings.ShowUnofficialTracks;
                    var serverTracks = await App.MobileService.GetTable<Track>().Where(t => t.Filter == "all").ToEnumerableAsync();
                    allTracks.AddRange(serverTracks.Where(t => nearbyTracks.Any(nt => nt.id == t.id) && (t.IsOfficial || retrieveAllTracks || (IsAuthenticated && t.CreatedBy == user.UserId))).Select(t => Tuple.Create("LOCAL", t)));
                    allTracks.AddRange(serverTracks.Where(t => !nearbyTracks.Any(nt => nt.id == t.id) && (t.IsOfficial || retrieveAllTracks || (IsAuthenticated && t.CreatedBy == user.UserId))).Select(t => Tuple.Create("ALL", t)));
                }
                catch (MobileServiceInvalidOperationException ex)
                {
                    // Unable to retrieve track from server, continue to local track
                    LoadingStatusText = AppResources.Text_LoadingStatus_ServerUnavailable;
                    loadedDataFromServer = false;
                }
            }
            else
            {
                // Unable to retrieve track from server, continue to local track
                LoadingStatusText = AppResources.Text_LoadingStatus_ServerUnavailable;
                loadedDataFromServer = false;
            }

            var cachedTracks = await TryFindLocalTracks(loadedDataFromServer, position, AppConstants.TRACK_SEARCH_BUFFER / 1000);
            allTracks.AddRange(cachedTracks.SelectMany(tg => tg.Select(t => Tuple.Create(tg.Key, t))));

            Track closestLocalTrack = null;
            bool foundLocalTrack = false;
            if (cachedTracks != null && cachedTracks.Any())
            {
                var localTracks = cachedTracks.FirstOrDefault(tg => tg.Key == "LOCAL");
                if (localTracks != null && localTracks.Any())
                {
                    foundLocalTrack = true;
                    closestLocalTrack = localTracks.First();
                }
            }

            var tracksByCountry = allTracks.GroupBy(t => t.Item1.Equals("NEW") ? AppResources.Text_Default_New
                                                            : t.Item1.Equals("LOCAL") ? AppResources.Text_Default_Nearby
                                                            : t.Item1.Equals(AppConstants.DEFAULT_UNKNOWNCOUNTRY_NAME) ? AppResources.Text_Default_UnknownCountry
                                                            : t.Item2.Country, t => t.Item2)
                                           .OrderBy(tg => tg.Key == AppResources.Text_Default_New || tg.Key == AppResources.Text_Default_Nearby ? "_" : tg.Key);
            Track selectedTrack = null;
            foreach (var track in tracksByCountry)
            {
                var trackGroup = new TrackGroup(track.Key, track);
                AllTracks.Add(trackGroup);
                if (selectedTrackId.HasValue && selectedTrack == null)
                    selectedTrack = track.FirstOrDefault(t => t.id == selectedTrackId.Value);
            }
            CurrentTrackItem = selectedTrack != null ? selectedTrack : foundLocalTrack ? closestLocalTrack : newTrack;
            HideSystemTray();
        }

        private async Task<IEnumerable<IGrouping<string, Track>>> TryFindLocalTracks(bool loadedDataFromServer, Geoposition position, int buffer)
        {
            List<Track> localTracks = new List<Track>();
            var positionAsGeography = position != null ? GeographyPoint.Create(position.Coordinate.Latitude, position.Coordinate.Longitude) : null;
            try
            {
                var trackCacheFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(AppConstants.LOCALFOLDERNAME_TRACKCACHE, CreationCollisionOption.OpenIfExists);
                var localFilesResult = await trackCacheFolder.GetFilesAsync();
                foreach (var trackFile in localFilesResult)
                {
                    if (loadedDataFromServer && !trackFile.Name.StartsWith("0") && !trackFile.Name.StartsWith("-1"))
                        continue;

                    var localTrack = await trackFile.ReadJsonFileAs<Track>();
                    if (localTrack != null)
                        localTracks.Add(localTrack);
                }
            }
            catch (FileNotFoundException ex)
            {
                return null;
            }
            return localTracks.GroupBy(track =>
                {
                    var trackLocationAsGeography = GeographyPoint.Create(track.Latitude, track.Longitude);
                    return trackLocationAsGeography != null && (trackLocationAsGeography.Distance(positionAsGeography) < buffer) ? "LOCAL" : "ALL";
                });
        }

        private async Task<string> CacheTrack(Track track, IEnumerable<TrackSessionViewModel> sessions, long? oldId = null, string oldFileName = null, bool overwriteLocal = false)
        {
            ShowSystemTray();
            LoadingStatusText = AppResources.Text_LoadingStatus_UpdatingTrackCache;

            bool trackWasCached = false;
            string oldTrackName = string.Empty;
            StorageFile localTrackFile = null;
            if (track.id < 0) track.id = 0;
            string fileName = BuildTrackFileName(track, oldId);
            try
            {
                var localTracksFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(AppConstants.LOCALFOLDERNAME_TRACKCACHE, CreationCollisionOption.OpenIfExists);
                var trackFiles = await localTracksFolder.GetFilesAsync();
                foreach (var trackFile in trackFiles)
                {
                    if (!trackFile.Name.Equals(oldFileName ?? fileName, StringComparison.OrdinalIgnoreCase)) continue;
                    localTrackFile = trackFile;
                    trackWasCached = true;
                    break;
                }
                if (localTrackFile == null)
                    localTrackFile = await localTracksFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            }
            catch (FileNotFoundException ex)
            { }

            var localTrack = trackWasCached ? await localTrackFile.ReadJsonFileAs<Track>() : new Track { id = track.id, BackgroundImagePath = track.BackgroundImagePath };
            if (localTrack == null)
                localTrack = new Track { id = track.id, BackgroundImagePath = track.BackgroundImagePath };
            else
                oldTrackName = localTrack.Name;

            if (oldId.HasValue) localTrack.id = track.id;
            localTrack.Latitude = track.Latitude;
            localTrack.Longitude = track.Longitude;
            localTrack.Name = track.Name;
            localTrack.Length = track.Length;
            localTrack.Timestamp = track.Timestamp;
            localTrack.Description = track.Description;
            localTrack.Country = track.Country;
            if (!trackWasCached || track.TotalLaps > localTrack.TotalLaps)
                localTrack.TotalLaps = track.TotalLaps;
            if (overwriteLocal)
                localTrack.BackgroundImagePath = track.BackgroundImagePath;
            if (!track.Sectors.Any() || track.Sectors.Last().IsFinishLine)
            {
                localTrack.Sectors = track.Sectors;
            }
            else
            {
                IList<TrackSector> sectors;
                sectors = track.Sectors.Where(s => !s.IsFinishLine).OrderBy(s => s.Number).ToList();
                var finishLineSector = track.Sectors.SingleOrDefault(s => s.IsFinishLine);
                if (finishLineSector != null)
                    sectors.Add(finishLineSector);
                else
                    sectors.Last().IsFinishLine = true;
                int sectorNumber = 1;
                foreach (var sector in sectors)
                {
                    sector.Number = sectorNumber;
                    sectorNumber++;
                }
                localTrack.Sectors = sectors;
                track.Sectors = sectors;
            }
            if (oldId.HasValue || (!string.IsNullOrWhiteSpace(oldFileName) && fileName != oldFileName))
            {
                foreach (var bestLap in localTrack.BestLaps ?? Enumerable.Empty<BestLap>())
                    bestLap.RecreateVerificationCode(IsAuthenticated ? user.UserId : null, track.id);
                track.BestLaps = localTrack.BestLaps;
            }
            else
            {
                var serverBestLap = track.BestLaps != null ? track.BestLaps.SingleOrDefault(l => l.Vehicle.Key == Settings.CurrentVehicle.Key) : null;
                if (serverBestLap != null)
                {
                    serverBestLap.Vehicle = settings.CurrentVehicle.AsModel();
                    FindAndUpdateBestLap(localTrack, serverBestLap);
                }
                else if (localTrack.BestLaps != null)
                {
                    track.BestLaps = localTrack.BestLaps.Select(bestLap => new BestLap
                                        {
                                            id = bestLap.id,
                                            IsPublic = bestLap.IsPublic,
                                            IsUnofficial = bestLap.IsUnofficial,
                                            LapTimeTicks = bestLap.LapTimeTicks,
                                            Timestamp = bestLap.Timestamp,
                                            TrackId = bestLap.TrackId,
                                            UserName = bestLap.UserName,
                                            Vehicle = bestLap.Vehicle,
                                            VehicleClass = bestLap.VehicleClass,
                                            VehicleId = bestLap.VehicleId,
                                            VerificationCode = bestLap.VerificationCode,
                                            DataPoints = bestLap.DataPoints }).ToList();
                }
            }

            await localTrackFile.WriteJsonToFile(localTrack);

            if (oldId.HasValue)
                fileName = BuildTrackFileName(track);

            if (oldId.HasValue || (!string.IsNullOrWhiteSpace(oldFileName) && fileName != oldFileName))
            {
                LoadingStatusText = AppResources.Text_LoadingStatus_RenamingTrackFile;
                await localTrackFile.RenameAsync(fileName, NameCollisionOption.ReplaceExisting);
                await RenameTrackHistoryFiles(localTrack, sessions, oldTrackName, oldId);
            }

            HideSystemTray();
            return fileName;
        }

        private async Task RenameTrackHistoryFiles(Track newTrack, IEnumerable<TrackSessionViewModel> trackSessions, string oldTrackName, long? oldTrackId)
        {
            ShowSystemTray();
            LoadingStatusText = AppResources.Text_LoadingStatus_RenamingTrackSessionFiles;
            try
            {
                var localTrackFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(AppConstants.LOCALFOLDERNAME_SESSIONS, CreationCollisionOption.OpenIfExists);

                foreach (var sessionViewModel in trackSessions)
                {
                    sessionViewModel.TrackId = newTrack.id;
                    sessionViewModel.TrackName = newTrack.Name;

                    if (sessionViewModel.Location == TrackSessionLocation.InMemory)
                        continue;

                    if (sessionViewModel.Location == TrackSessionLocation.LocalFile || sessionViewModel.Location == TrackSessionLocation.ServerWithLocalFile)
                    {
                        var trackHistoryFile = await localTrackFolder.GetFileAsync(sessionViewModel.LocalFilePath);
                        var session = await trackHistoryFile.ReadJsonFileAs<TrackSession>();
                        if (session != null && ((session.TrackId == 0 && session.TrackName == oldTrackName) || (oldTrackId.HasValue && session.TrackId == oldTrackId.Value)))
                        {
                            session.TrackId = newTrack.id;
                            session.TrackName = newTrack.Name;
                            await trackHistoryFile.WriteJsonToFile(session);
                            string newFileName = BuildTrackHistoryFileName(session);
                            await trackHistoryFile.RenameAsync(newFileName, NameCollisionOption.ReplaceExisting);
                            sessionViewModel.LocalFilePath = trackHistoryFile.Name;
                            if (sessionViewModel.Location == TrackSessionLocation.ServerWithLocalFile && !string.IsNullOrWhiteSpace(sessionViewModel.ServerFilePath))
                            {
                                await App.LiveClient.RenameFileInTrackTimerFolder(sessionViewModel.ServerFilePath, newFileName);
                                var fileId = App.LiveClient.UploadFileToTrackTimerFolder(trackHistoryFile, Settings.UploadSessionsOverWifi, overwrite: true);
                            }
                        }
                    }
                    if (sessionViewModel.Location == TrackSessionLocation.ServerFile)
                    {
                        var historicLapsJsonResult = await App.LiveClient.DownloadFileAsStream(sessionViewModel.ServerFilePath);
                        var session = historicLapsJsonResult.ReadJsonStreamAs<TrackSession>();
                        if (session != null && ((session.TrackId == 0 && session.TrackName == oldTrackName) || (oldTrackId.HasValue && session.TrackId == oldTrackId.Value)))
                        {
                            session.TrackId = newTrack.id;
                            session.TrackName = newTrack.Name;
                            string newFileName = BuildTrackHistoryFileName(session);
                            await App.LiveClient.RenameFileInTrackTimerFolder(sessionViewModel.ServerFilePath, newFileName);
                            var fullSessionViewModel = session.AsViewModel(Track, Settings.CurrentVehicle, session.BestLapTime, TrackSessionLocation.ServerFile, sessionViewModel.ServerFilePath, Settings.IsMetricUnits);
                            var localSessionFile = await SaveSessionToLocalStore(fullSessionViewModel);
                            var fileId = App.LiveClient.UploadFileToTrackTimerFolder(localSessionFile, Settings.UploadSessionsOverWifi, overwrite: true)
                                                       .ContinueWith((result, s) =>
                                                        {
                                                            if (result.IsFaulted) return;
                                                            var uploadedSession = s as TrackSessionViewModel;
                                                            uploadedSession.Location = TrackSessionLocation.ServerWithLocalFile;
                                                        }, sessionViewModel);
                        }
                    }
                }

                await BackgroundTransfers.LoadData(false);
            }
            catch (FileNotFoundException ex)
            {
                throw;
            }
            finally
            {
                HideSystemTray();
            }
        }

        private async Task LoadTrack(Track selectedTrack, CancellationToken cancellationToken)
        {
            if (selectedTrack == null || cancellationToken.IsCancellationRequested) return;
            ShowSystemTray();
            LoadingStatusText = AppResources.Text_LoadingStatus_LoadingCircuit;
            IsTrackLoaded = false;
            IsTrackLoading = true;

            Track = new TrackViewModel { Name = AppResources.Text_Default_Loading, BestLap = new BestLapViewModel { IsUnofficial = false } };
            Track serverTrack = null;
            Track localTrack = null;
            bool localTrackNeedsUpdating = false;
            if (selectedTrack.id > 0 && InternetConnectionIsAvailable())
            {
                try
                {
                    var serverTracks = await App.MobileService.GetTable<Track>().Where(t => t.id == selectedTrack.id).ToEnumerableAsync();
                    serverTrack = serverTracks.Single();
                }
                catch (MobileServiceInvalidOperationException)
                { }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                HideSystemTray();
                return;
            }

            if (selectedTrack.id <= 0)
            {
                localTrack = selectedTrack;
            }
            else
            {
                localTrack = await LoadLocalTrack(serverTrack ?? selectedTrack);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                HideSystemTray();
                return;
            }

            if (serverTrack != null && (localTrack == null || localTrack.Timestamp < serverTrack.Timestamp))
            {
                Track = serverTrack.AsViewModel(Settings.CurrentVehicle.Key, Settings.IsMetricUnits);
                localTrackNeedsUpdating = true;
            }
            else if (localTrack != null)
            {
                Track = localTrack.AsViewModel(Settings.CurrentVehicle.Key, Settings.IsMetricUnits, isLocal: selectedTrack.id <= 0, fileNameWhenLoaded: BuildTrackFileName(localTrack));
            }
            else
            {
                Track = new TrackViewModel
                {
                    Id = selectedTrack.id,
                    Name = selectedTrack.Name,
                    IsLocal = true,
                    IsMetricUnits = Settings.IsMetricUnits
                };
            }

            if (cancellationToken.IsCancellationRequested)
            {
                HideSystemTray();
                return;
            }

            IEnumerable<TrackSector> trackSectors = null;
            if (localTrack == null || localTrack.Sectors == null)
            {
                try
                {
                    serverTrack.Sectors = trackSectors = await App.MobileService.GetTable<TrackSector>().Where(s => s.TrackId == serverTrack.id).ToEnumerableAsync();
                    localTrackNeedsUpdating = true;
                }
                catch (MobileServiceInvalidOperationException ex)
                { }
                if (cancellationToken.IsCancellationRequested)
                {
                    HideSystemTray();
                    return;
                }
            }
            else
            {
                trackSectors = localTrack.Sectors;
            }

            Track.Sectors = new ObservableCollection<TrackSectorViewModel>(from sector in trackSectors ?? Enumerable.Empty<TrackSector>()
                                                                           select sector.AsViewModel());

            IsTrackLoaded = true;
            IsTrackLoading = false;
            if (TrackLoadComplete != null)
                TrackLoadComplete(this, Track);

            if (Track.Id < 0)
            {
                if (TrackHistoryLoadComplete != null)
                    TrackHistoryLoadComplete(this, Track);
                HideSystemTray();
                return;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                HideSystemTray();
                return;
            }

            await LoadBestLapTime(localTrack, serverTrack, cancellationToken);
            
            if (cancellationToken.IsCancellationRequested)
            {
                HideSystemTray();
                return;
            }

            if (serverTrack != null && localTrackNeedsUpdating)
                    Track.FileNameWhenLoaded = await CacheTrack(serverTrack, Enumerable.Empty<TrackSessionViewModel>());

            cancellationToken = await LoadLeaderboardForTrack(cancellationToken);

            await LoadTrackHistory(cancellationToken);
            HideSystemTray();
        }

        private async Task<CancellationToken> LoadLeaderboardForTrack(CancellationToken cancellationToken)
        {
            LoadingStatusText = AppResources.Text_LoadingStatus_LoadingTrackLeaderboard;
            Track.Leaderboard.Clear();
            if (IsAuthenticated && Track.Id > 0 && InternetConnectionIsAvailable())
            {
                var leaderboardLaps = await RetrieveLeaderboardTimesForTrack(Track.Id, cancellationToken);
                foreach (var lap in leaderboardLaps)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    Track.Leaderboard.Add(lap.AsViewModel(Settings.IsMetricUnits));
                }
            }
            return cancellationToken;
        }

        private async Task LoadBestLapTime(Track localTrack, Track serverTrack, CancellationToken cancellationToken)
        {
            ShowSystemTray();
            LoadingStatusText = AppResources.Text_LoadingStatus_LoadingBestLapTime;
            var bestLap = serverTrack != null ? await RetrieveBestLapTimeForTrackAndVechicle(localTrack, serverTrack.id) : null;

            if (cancellationToken.IsCancellationRequested)
            {
                HideSystemTray();
                return;
            }

            currentBestLap = bestLap != null ? bestLap.AsViewModel(Settings.IsMetricUnits) : Track.BestLap ?? new BestLapViewModel { IsUnofficial = false };
            if (serverTrack != null)
                serverTrack.BestLaps = bestLap != null ? new[] { bestLap } : new BestLap[0];
            Track.BestLap = currentBestLap;
            Track.MaximumSpeed = currentBestLap.MaximumSpeed;
            HideSystemTray();
        }

        private async Task LoadTrackHistory(CancellationToken cancellationToken)
        {
            if (Settings.CurrentVehicle.Key == Guid.Empty)
                return;

            ShowSystemTray();
            LoadingStatusText = AppResources.Text_LoadingStatus_LoadingLapHistory;
            Track.History = new TrackHistoryViewModel(); 

            var historicLapSessions = new List<TrackSessionHeaderViewModel>();

            string fileNamePrefix = string.Format("{0}_{1}{2}", Track.Name, Settings.CurrentVehicle.Make, Settings.CurrentVehicle.Model).Replace(" ", "").Replace(",", "").Replace(":", "");

            IList<string> localFileNames = new List<string>();
            try
            {
                var localTrackFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(AppConstants.LOCALFOLDERNAME_SESSIONS, CreationCollisionOption.OpenIfExists);
                var localFilesResult = await localTrackFolder.GetFilesAsync();

                foreach (var trackHistoryFile in localFilesResult)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        HideSystemTray();
                        return;
                    }

                    // TODO - Improve the speed of this by using a sorted list and stopping once the prefix is no longer found
                    string fileName = trackHistoryFile.Name;
                    if (!fileName.StartsWith(fileNamePrefix)) continue;

                    var session = await trackHistoryFile.ReadJsonFileAs<TrackSessionHeader>();
                    if (session != null && ((session.TrackId <= 0 && session.TrackName == Track.Name) || session.TrackId == Track.Id) && session.Vehicle.Key == Settings.CurrentVehicle.Key)
                    {
                        historicLapSessions.Add(session.AsViewModel(fileName, TrackSessionLocation.LocalFile, Settings.IsMetricUnits));
                        localFileNames.Add(fileName);
                    }
                }
            }
            catch (FileNotFoundException ex)
            {
                HideSystemTray();
                throw;
            }

            if (IsAuthenticated && Settings.IsConnectedToSkyDrive && InternetConnectionIsAvailable())
            {
                var trackHistoryFiles = isAuthenticated ? await App.LiveClient.GetFilesInTrackTimerFolder() : null;
                foreach (dynamic trackHistoryFile in ((dynamic)trackHistoryFiles) ?? Enumerable.Empty<dynamic>())
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        HideSystemTray();
                        return;
                    }

                    if (!trackHistoryFile.name.StartsWith(fileNamePrefix))
                        continue;

                    string filePath = trackHistoryFile.id;
                    if (localFileNames.Any(f => f.Equals(trackHistoryFile.name, StringComparison.OrdinalIgnoreCase)))
                    {
                        var localSession = historicLapSessions.Single(l => l.LocalFilePath.Equals(trackHistoryFile.name, StringComparison.OrdinalIgnoreCase));
                        localSession.IsUploaded = true;
                        localSession.Location = TrackSessionLocation.ServerWithLocalFile;
                        localSession.ServerFilePath = filePath;
                        continue;
                    }

                    var historicLapsJsonResult = await App.LiveClient.DownloadFileAsStream(filePath);
                    var session = historicLapsJsonResult.ReadJsonStreamAs<TrackSessionHeader>();
                    if (session != null && ((session.TrackId == 0 && session.TrackName == Track.Name) || session.TrackId == Track.Id) && session.Vehicle.Key == Settings.CurrentVehicle.Key)
                    {
                        var sessionViewModel = session.AsViewModel(filePath, TrackSessionLocation.ServerFile, Settings.IsMetricUnits);
                        historicLapSessions.Add(sessionViewModel);
                    }
                }
            }

            var groupedHistoricLaps = historicLapSessions.OrderByDescending(s => s.Timestamp)
                                                         .Select(s => new TrackSessionViewModel
                                                         {
                                                             BestLapTime = s.BestLapTime,
                                                             DeviceOrientation = s.DeviceOrientation,
                                                             NumberOfLaps = s.NumberOfLaps,
                                                             Vehicle = s.Vehicle,
                                                             Timestamp = s.Timestamp,
                                                             Location = s.Location,
                                                             LocalFilePath = s.LocalFilePath,
                                                             ServerFilePath = s.ServerFilePath,
                                                             IsUploaded = s.IsUploaded,
                                                             TrackId = s.TrackId,
                                                             TrackName = s.TrackName
                                                         });

            if (cancellationToken.IsCancellationRequested)
            {
                HideSystemTray();
                return;
            }

            foreach (var historicLap in groupedHistoricLaps)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Track.History.Sessions.Clear();
                    HideSystemTray();
                    return;
                }
                Track.History.Sessions.Add(historicLap);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                HideSystemTray();
                return;
            }

            HideSystemTray();
            if (!cancellationToken.IsCancellationRequested && TrackHistoryLoadComplete != null)
                TrackHistoryLoadComplete(this, Track);
        }

        private async Task<Geoposition> GetCurrentGeoposition()
        {
            if (!Settings.LocationConsent) return null;
            IAsyncOperation<Geoposition> locationTask = null;
            Geoposition position = null;
            Exception locationException = null;
            try
            {
                var geolocator = new Geolocator();
                geolocator.DesiredAccuracy = PositionAccuracy.Default;
                locationTask = geolocator.GetGeopositionAsync(TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(15));
                position = await locationTask;
            }
            catch (Exception ex)
            {
                if ((uint)ex.HResult == 0x80004004)
                {
                    if (NotifyUser != null)
                        NotifyUser(this, AppResources.Text_Blurb_LocationServicesSwitchedOff);
                }
                else
                {
                    locationException = ex;
                }
            }
            finally
            {
                if (locationTask != null)
                {
                    if (locationTask.Status == AsyncStatus.Started)
                        locationTask.Cancel();

                    locationTask.Close();
                }
            }

            if (locationException != null)
            {
                string errorId = await BugSenseHandler.Instance.LogExceptionWithId(user, locationException);
                if (NotifyUser != null)
                    NotifyUser(this, string.Format(AppResources.Text_Error_UnableToRetrieveLocation, errorId));
            }

            return position;
        }

        private async Task<Track> LoadLocalTrack(Track serverTrack)
        {
            StorageFile localTracksFile = null;
            try
            {
                string fileNamePrefix = BuildTrackFileName(serverTrack);
                var localTrackFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(AppConstants.LOCALFOLDERNAME_TRACKCACHE, CreationCollisionOption.OpenIfExists);
                var localFilesResult = await localTrackFolder.GetFilesAsync();
                foreach (var trackFile in localFilesResult)
                {
                    if (trackFile.Name.StartsWith(fileNamePrefix))
                    {
                        localTracksFile = trackFile;
                        break;
                    }
                }
            }
            catch (FileNotFoundException ex)
            {
                return null;
            }
            if (localTracksFile == null) return null;
            var localTrack = await localTracksFile.ReadJsonFileAs<Track>();
            if (localTrack == null) return null;
            if (serverTrack.Timestamp > localTrack.Timestamp)
                localTrack.Sectors = null;
            return localTrack;
        }

        private async Task<IEnumerable<BestLap>> RetrieveLeaderboardTimesForTrack(long trackId, CancellationToken cancellationToken)
        {
            // TODO - Consider grouping by class
            if (!isAuthenticated) return Enumerable.Empty<BestLap>();
            var serverBestLaps = await App.MobileService.GetTable<BestLap>().Where(l => l.TrackId == trackId).Take(20).ToEnumerableAsync();
            if (cancellationToken.IsCancellationRequested) return Enumerable.Empty<BestLap>();
            var bestLaps = new List<BestLap>();
            foreach (var lap in serverBestLaps.OrderBy(l => l.LapTimeTicks))
            {
                lap.DataPoints = JsonConvert.DeserializeObject<IEnumerable<BestLapDataPoint>>(lap.DataPointsCollection);
                bestLaps.Add(lap);
            }
            return cancellationToken.IsCancellationRequested ? Enumerable.Empty<BestLap>() : bestLaps;
        }

        private async Task<BestLap> RetrieveBestLapTimeForTrackAndVechicle(Track localTrack, long? trackId = null)
        {
            BestLap serverBestLap = null;
            if (isAuthenticated && trackId.HasValue && InternetConnectionIsAvailable())
            {
                // TODO - Figure out how to compare the GUID key to the UniqueIdentifier key
                var serverCurrentVehicles = await App.MobileService.GetTable<Vehicle>().Where(v => v.UserName == user.UserId).ToEnumerableAsync();
                var serverCurrentVehicle = serverCurrentVehicles.SingleOrDefault(v => v.Key == Settings.CurrentVehicle.Key);
                if (serverCurrentVehicle != null)
                {
                    var serverBestLaps = await App.MobileService.GetTable<BestLap>().Where(l => l.TrackId == trackId.Value && l.UserName == user.UserId && l.VehicleId == serverCurrentVehicle.id)
                                                                                    .OrderByDescending(l => l.Timestamp)
                                                                                    .ToEnumerableAsync();
                    serverBestLap = serverBestLaps.FirstOrDefault();
                    if (serverBestLap != null)
                        serverBestLap.Vehicle = serverCurrentVehicle;
                }
            }

            var localBestLap = localTrack != null && localTrack.BestLaps != null
                                ? localTrack.BestLaps.SingleOrDefault(bl => bl.Vehicle.Key == Settings.CurrentVehicle.Key)
                                : null;
            if (localBestLap != null && (serverBestLap == null || serverBestLap.LapTimeTicks > localBestLap.LapTimeTicks))
            {
                if (trackId.HasValue && isAuthenticated && InternetConnectionIsAvailable())
                    await UploadBestLapTimeToServer(localBestLap);
                return localBestLap;
            }
            else if (serverBestLap != null)
            {
                serverBestLap.DataPoints = JsonConvert.DeserializeObject<IEnumerable<BestLapDataPoint>>(serverBestLap.DataPointsCollection);
                return serverBestLap;
            }
            else
            {
                return null;
            }
        }

        private void FindAndUpdateBestLap(Track localTrack, BestLap newBestLap)
        {
            if (localTrack.BestLaps == null)
                localTrack.BestLaps = new List<BestLap>();

            var bestLap = localTrack.BestLaps.SingleOrDefault(bl => bl.Vehicle.Key == newBestLap.Vehicle.Key);
            if (bestLap != null)
            {
                bestLap.id = newBestLap.id;
                bestLap.IsPublic = newBestLap.IsPublic;
                bestLap.UserName = newBestLap.UserName;
                bestLap.VehicleId = newBestLap.VehicleId;
                bestLap.DataPoints = newBestLap.DataPoints;
                bestLap.LapTimeTicks = newBestLap.LapTimeTicks;
                bestLap.Timestamp = newBestLap.Timestamp;
                bestLap.TrackId = newBestLap.TrackId;
                bestLap.UserName = IsAuthenticated ? user.UserId : null;
                bestLap.Vehicle = Settings.CurrentVehicle.AsModel();
                bestLap.VerificationCode = newBestLap.VerificationCode;
            }
            else
            {
                localTrack.BestLaps.Add(new BestLap
                {
                    id = newBestLap.id,
                    IsPublic = newBestLap.IsPublic,
                    IsUnofficial = newBestLap.IsUnofficial,
                    LapTimeTicks = newBestLap.LapTimeTicks,
                    Timestamp = newBestLap.Timestamp,
                    TrackId = newBestLap.TrackId,
                    UserName = newBestLap.UserName,
                    Vehicle = newBestLap.Vehicle,
                    VehicleClass = newBestLap.VehicleClass,
                    VehicleId = newBestLap.VehicleId,
                    VerificationCode = newBestLap.VerificationCode,
                    DataPoints = newBestLap.DataPoints
                });
            }
        }

        private async void Timer_TimingStopped(TimerViewModel sender, TimingStoppedViewModel args)
        {
            ShowInterstitialAd = Settings.IsTrial;
            if (args == null) return;
            int sessionLapCount = args.Laps.Count();
            if (sessionLapCount < 1) return;

            ShowSystemTray();
            CurrentSessionSaveState = new ActivityState { State = ActivityStep.SessionBeginSave, TrackId = Track.Id, TrackName = Track.Name };

            // Identify best lap
            LoadingStatusText = AppResources.Text_LoadingStatus_IdentifyingBestLap;
            BestLapViewModel bestLap = null;
            double? bestLapLength = null;
            var completeLaps = args.Laps.Where(l => l.IsComplete);
            if (completeLaps.Any())
            {
                var bestOfficialLap = completeLaps.Where(l => LapMeetsMinimimLength(Track, l)).MinBy(l => l.LapTime);
                bestLap = bestOfficialLap != null
                            ? bestOfficialLap.AsViewModel(Settings.IsMetricUnits, lapIsUnofficial: false)
                            : completeLaps.MinBy(l => l.LapTime).AsViewModel(Settings.IsMetricUnits, lapIsUnofficial: true);
                bestLap.GpsDeviceName = args.GpsDeviceName;
                bestLapLength = bestLap.ProjectedLength();
            }

            // If this is a new track then the Id must be updated to 0 as it will be cached
            if (Track.Id < 0)
                Track.Id = 0;

            // Create session and save to phone
            LoadingStatusText = AppResources.Text_LoadingStatus_CreatingSession;
            var session = new TrackSessionViewModel
            {
                GpsDeviceName = args.GpsDeviceName,
                DeviceOrientation = sender.StartPhoneOrientation,
                Laps = new ObservableCollection<LapViewModel>(),
                NumberOfLaps = args.Laps.Count(),
                BestLapTime = bestLap != null ? bestLap.LapTime : TimeSpan.Zero,
                Timestamp = sender.StartTime.ToLocalTime(),
                Track = Track,
                TrackId = Track.Id,
                TrackName = Track.Name,
                Vehicle = Settings.CurrentVehicle,
                Location = TrackSessionLocation.InMemory
            };
            foreach (var lap in args.Laps.OrderBy(l => l.LapNumber).ToList())
                session.Laps.Add(lap);

            // Save the session to the phone as soon as possible in case the user exists the app or there is a processing failure
            var localSessionFile = await SaveSessionToLocalStore(session);
            CurrentSessionSaveState.State = ActivityStep.SessionSavedToLocalStore;
            CurrentSessionSaveState.SessionFileName = localSessionFile.Name;
            if (CurrentSessionSavedToLocalStore != null)
                CurrentSessionSavedToLocalStore(this, ActivityStep.SessionSavedToLocalStore);

            // Get the weather conditions and update the stored session
            var trackCoordinate = new System.Device.Location.GeoCoordinate(Track.Latitude, Track.Longitude);
            var weather = await RetrieveWeatherConditionsForLocation(trackCoordinate);
            LoadingStatusText = AppResources.Text_LoadingStatus_SavingSessionToPhone;
            session.Weather = weather;
            if (weather != null)
            {
                bestLap.WeatherCondition = weather.Condition;
                bestLap.AmbientTemperature = weather.Temperature;
                localSessionFile = await SaveSessionToLocalStore(session, SessionSaveType.Replace);
            }

            // Set generated sectors on Track model
            if ((Track.Sectors == null || !Track.Sectors.Any()) && args.GeneratedTrackSectors != null)
            {
                Track.Sectors = args.GeneratedTrackSectors;
                if (TrackLoadComplete != null)
                    TrackLoadComplete(this, Track);

                if (settings.IsMetricUnits)
                    Track.Length = bestLapLength ?? 0;
                else
                    Track.Length = bestLapLength.HasValue
                                    ? bestLapLength.Value / Constants.MILES_TO_KILOMETRES
                                    : 0;
            }
          
            var bestLapForServer = CreateBestLapForServer(Track, bestLap);
            if (bestLapForServer != null)
            {
                currentBestLap = bestLap;
                Track.BestLap = currentBestLap;
                Track.MaximumSpeed = currentBestLap.MaximumSpeed;
            }

            // Save updated Track to local track cache
            Track.TotalLaps += sessionLapCount;
            var trackToCache = Track.AsModel(Settings.IsMetricUnits);
            if (bestLapForServer != null)
                trackToCache.BestLaps = new List<BestLap> { bestLapForServer };
            Track.FileNameWhenLoaded = await CacheTrack(trackToCache, Enumerable.Empty<TrackSessionViewModel>());
            currentTrackItem = trackToCache;
            Track.Id = trackToCache.id;

            CurrentSessionSaveState.State = ActivityStep.SessionUpdatedTrackCache;

            if (IsAuthenticated && InternetConnectionIsAvailable())
            {
                // Save best lap time to server
                if (Track.Id > 0 && bestLapForServer != null)
                    await UploadBestLapTimeToServer(bestLapForServer);

                CurrentSessionSaveState.State = ActivityStep.SessionSavedBestLapTimeToServer;

                if (Settings.IsConnectedToSkyDrive && Settings.AutouploadSessions)
                {
                    // Save session to OneDrive
                    LoadingStatusText = AppResources.Text_LoadingStatus_SavingSessionToSkyDrive;
                    var fileId = await SaveSessionToCloudStore(session, localSessionFile);
                    session.Location = TrackSessionLocation.ServerWithLocalFile;
                    session.IsUploaded = true;
                    session.ServerFilePath = fileId;
                    await BackgroundTransfers.LoadData(false);
                }

                // Set latestSessionFileName on the current User
                await UpdateUsersLatestSession(localSessionFile.Name);
            }

            CurrentSessionSaveState.State = ActivityStep.SessionReloadTrackHistory;

            // Reload Session History
            CancelAllActivities();
            using (var cancellationToken = new CancellationTokenSource())
            {
                loadingCancellationTokens.Add(cancellationToken);
                try
                {
                    await LoadTrackHistory(cancellationToken.Token);
                }
                finally
                {
                    loadingCancellationTokens.Remove(cancellationToken);
                    CurrentSessionSaveState = null;
                    HideSystemTray();
                }
            }
        }

        private async Task UpdateUsersLatestSession(string fileName)
        {
            bool failedToUpdateUsersLatestSession = false;
            Exception updateException = null;
            try
            {
                var userTable = App.MobileService.GetTable<User>();
                var userProfiles = await userTable.Where(u => u.UserId == user.UserId).ToListAsync();
                var userProfile = userProfiles.SingleOrDefault();
                if (userProfile == null) return;

                userProfile.LatestSessionFileName = fileName;
                await userTable.UpdateAsync(userProfile);
            }
            catch (Exception ex)
            {
                failedToUpdateUsersLatestSession = true;
                updateException = ex;
            }
            if (failedToUpdateUsersLatestSession)
            {
                await BugSenseHandler.Instance.LogExceptionAsync(updateException);
            }
        }

        private double? JsonValueAsNullableDouble(string jsonValue)
        {
            double parsedValue;
            if (double.TryParse(jsonValue, out parsedValue))
                return parsedValue;
            else
                return null;
        }

        private int? JsonValueAsNullableInt(string jsonValue)
        {
            int parsedValue;
            if (int.TryParse(jsonValue, out parsedValue))
                return parsedValue;
            else
                return null;
        }

        private async Task<WeatherConditionsViewModel> RetrieveWeatherConditionsForLocation(System.Device.Location.GeoCoordinate coordinate)
        {
            if (Settings.IsTrial || !InternetConnectionIsAvailable()) return null;

            ShowSystemTray();
            LoadingStatusText = AppResources.Text_LoadingStatus_RetrievingWeatherConditions;
            Exception weatherLoadException = null;
            string responseContent = null;
            dynamic jsonResult;
            try
            {
                using (var client = new HttpClient())
                {
                    var uri = new Uri(string.Format(Constants.WEATHERUNDERGROUND_CONDITIONS_PATH_FORMAT, Constants.WEATHERUNDERGROUND_API_KEY, coordinate.Latitude, coordinate.Longitude));
                    var result = await client.GetAsync(uri);
                    if (!result.IsSuccessStatusCode)
                    {
                        await BugSenseHandler.Instance.LogEventAsync(string.Format("Weather not available: {0}", result.StatusCode));
                        HideSystemTray();
                        return null;
                    }
                    responseContent = await result.Content.ReadAsStringAsync();
                    jsonResult = JObject.Parse(responseContent);
                    if (jsonResult.Property("current_observation") == null)
                    {
                        await BugSenseHandler.Instance.LogEventAsync(string.Format("Weather current_observation not available for: {0}", uri.ToString()));
                        HideSystemTray();
                        return null;
                    }
                }

                var weather = new WeatherConditionsViewModel
                {
                    Condition = jsonResult.current_observation.weather,
                    Temperature = JsonValueAsNullableDouble((string)(Settings.IsMetricUnits ? jsonResult.current_observation.temp_c : jsonResult.current_observation.temp_f)),
                    WindDirection = JsonValueAsNullableInt((string)jsonResult.current_observation.wind_degrees),
                    WindSpeed = JsonValueAsNullableDouble((string)(Settings.IsMetricUnits ? jsonResult.current_observation.wind_kph : jsonResult.current_observation.wind_mph)),
                    PreviousHourPrecipitation = JsonValueAsNullableDouble((string)(Settings.IsMetricUnits ? jsonResult.current_observation.precip_1hr_metric : jsonResult.current_observation.precip_1hr_in)),
                    TotalDayPrecipitation = JsonValueAsNullableDouble((string)(Settings.IsMetricUnits ? jsonResult.current_observation.precip_today_metric : jsonResult.current_observation.precip_today_in))
                };

                // Cleanse the data from Weather Underground as it can have irregularities
                if ((weather.PreviousHourPrecipitation ?? 0) < 0)
                    weather.PreviousHourPrecipitation = 0;
                if ((weather.TotalDayPrecipitation ?? 0) < 0)
                    weather.TotalDayPrecipitation = 0;

                await BugSenseHandler.Instance.LogEventAsync(string.Format("Weather retrieved at {0}, {1}", coordinate.Latitude, coordinate.Longitude));
                HideSystemTray();
                return weather;
            }
            catch (Exception ex)
            {
                weatherLoadException = ex;
            }

            // If there was an exception then log it to Bugsense
            if (weatherLoadException != null)
            {
                var extraData = new LimitedCrashExtraDataList();
                extraData.Add("Message", string.Format("Failed to retrieve weather at {0}, {1}. Response: {2}", coordinate.Latitude, coordinate.Longitude, responseContent));
                await BugSenseHandler.Instance.LogExceptionAsync(weatherLoadException, extraData);
            }

            return null;
        }

        private async Task UploadBestLapTimeToServer(BestLap bestLapForServer)
        {
            ShowSystemTray();
            LoadingStatusText = AppResources.Text_LoadingStatus_RegisteringBestLapTime;
            var lapToUpload = new BestLap
                {
                    IsPublic = bestLapForServer.IsPublic,
                    IsUnofficial = bestLapForServer.IsUnofficial,
                    LapTimeTicks = bestLapForServer.LapTimeTicks,
                    Timestamp = bestLapForServer.Timestamp,
                    TrackId = bestLapForServer.TrackId,
                    UserName = bestLapForServer.UserName,
                    Vehicle = null,
                    VehicleClass = bestLapForServer.VehicleClass,
                    VehicleId = bestLapForServer.VehicleId,
                    VerificationCode = bestLapForServer.VerificationCode,
                    GpsDeviceName = bestLapForServer.GpsDeviceName,
                    WeatherCondition = bestLapForServer.WeatherCondition,
                    AmbientTemperature = bestLapForServer.AmbientTemperature,
                    DataPoints = null,
                    DataPointsCollection = JsonConvert.SerializeObject(bestLapForServer.DataPoints)
                };
            bool failedToUploadBestLap = false;
            Exception bestLapUploadException = null;
            try
            {
                await App.MobileService.GetTable<BestLap>().InsertAsync(lapToUpload);
            }
            catch (Exception ex)
            {
                failedToUploadBestLap = true;
                bestLapUploadException = ex;
            }
            if (failedToUploadBestLap)
            {
                string errorId = await BugSenseHandler.Instance.LogExceptionWithId(user, bestLapUploadException);
                if (NotifyUser != null)
                    NotifyUser(this, string.Format(AppResources.Text_Error_UnableToUploadBestLapToServer, errorId));
            }
            HideSystemTray();
        }

        private bool LapMeetsMinimimLength(TrackViewModel track, LapViewModel lap)
        {
            // Lap must meet a minimum length to be valid for a best lap
            double minimumLength = track.Length * Constants.APP_MINIMUMLAPLENGTH_FACTOR;
            double projectedLapLength = lap.ProjectedLength();
            if (projectedLapLength < minimumLength) return false;
            double actualLapLength = lap.ActualLength();
            var lastDataPoint = lap.DataPoints.Where(dp => dp.Speed.HasValue).LastOrDefault();
            if (lastDataPoint == null) return false;
            // Maximum distance between last and first datapoints is 2sec @ last datapoint speed and assumes datapoints are 1 sec apart
            double maximumDataPointDistance = lastDataPoint.Speed.Value / 3600 * 2;
            return (actualLapLength - projectedLapLength) < maximumDataPointDistance;
        }

        private BestLap CreateBestLapForServer(TrackViewModel track, BestLapViewModel localBestLap)
        {
            if (localBestLap != null && (track.BestLap == null || track.BestLap.LapTime == TimeSpan.Zero || track.BestLap.LapTime > localBestLap.LapTime))
                return localBestLap.AsModel(Settings.CurrentVehicle, Settings.IsMetricUnits, true, IsAuthenticated ? user.UserId : null, track.Id, track.Sectors);
            return null;
        }

        private async void AppViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CurrentTrackItem")
            {
                CancelAllActivities();
                using (var cancellationToken = new CancellationTokenSource())
                {
                    loadingCancellationTokens.Add(cancellationToken);
                    try
                    {
                        await LoadTrack(currentTrackItem, cancellationToken.Token);
                    }
                    finally
                    {
                        loadingCancellationTokens.Remove(cancellationToken);
                    }
                }
            }
            else if (e.PropertyName == "IsAuthenticated")
            {
                if (IsAuthenticated)
                    await LoadData();
            }
        }

        private async void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "CurrentVehicle":
                    currentVehicle = Settings.CurrentVehicle;
                    NotifyPropertyChanged("CurrentVehicle");
                    CancelAllActivities();
                    using (var cancellationToken = new CancellationTokenSource())
                    {
                        loadingCancellationTokens.Add(cancellationToken);
                        try
                        {
                            await LoadTrack(currentTrackItem, cancellationToken.Token);
                        }
                        finally
                        {
                            loadingCancellationTokens.Remove(cancellationToken);
                        }
                    }
                    break;
                case "IsMetricUnits":
                    SpeedUnitText = Settings.IsMetricUnits ? AppResources.Text_Unit_MetricSpeed : AppResources.Text_Unit_ImperialSpeed;
                    LengthUnitText = Settings.IsMetricUnits ? AppResources.Text_Unit_Kilometres : AppResources.Text_Unit_Miles;
                    CancelAllActivities();
                    using (var cancellationToken = new CancellationTokenSource())
                    {
                        loadingCancellationTokens.Add(cancellationToken);
                        try
                        {
                            await LoadTrack(currentTrackItem, cancellationToken.Token);
                        }
                        finally
                        {
                            loadingCancellationTokens.Remove(cancellationToken);
                        }
                    }
                    break;
                case "IsConnectedToSkyDrive":
                    using (var cancellationToken = new CancellationTokenSource())
                    {
                        loadingCancellationTokens.Add(cancellationToken);
                        try
                        {
                            await LoadTrackHistory(cancellationToken.Token);
                        }
                        finally
                        {
                            loadingCancellationTokens.Remove(cancellationToken);
                        }
                    }
                    break;
                default :
                    return;
            }
        }

        private async Task UploadTrackToServer()
        {
            if (Track.Id != 0) return;
            ShowSystemTray();
            LoadingStatusText = AppResources.Text_LoadingStatus_UploadingTrack;
            if (Track.Description.Equals(AppResources.Text_Blurb_UnknownTrackDescription))
                Track.Description = string.Empty;
            var serverTrack = Track.AsModel(Settings.IsMetricUnits);
            if (serverTrack.Country.Equals(AppResources.Text_Default_UnknownCountry))
                serverTrack.Country = AppConstants.DEFAULT_UNKNOWNCOUNTRY_NAME;
            serverTrack.IsOfficial = false;
            await App.MobileService.GetTable<Track>().InsertAsync(serverTrack);
            bool failedToUploadSectors = false;
            Exception sectorUploadException = null;
            var trackSectorTable = App.MobileService.GetTable<TrackSector>();
            List<TrackSector> createdTrackSectors = new List<TrackSector>();
            try
            {
                foreach (var sector in serverTrack.Sectors)
                {
                    sector.TrackId = serverTrack.id;
                    await trackSectorTable.InsertAsync(sector);
                    createdTrackSectors.Add(sector);
                }
            }
            catch (Exception ex)
            {
                failedToUploadSectors = true;
                sectorUploadException = ex;
            }
            if (failedToUploadSectors)
            {
                string errorId = await BugSenseHandler.Instance.LogExceptionWithId(user, sectorUploadException);
                // Rollback Track
                foreach (var sector in createdTrackSectors)
                    await trackSectorTable.DeleteAsync(sector);
                await App.MobileService.GetTable<Track>().DeleteAsync(serverTrack);
                if (NotifyUser != null)
                    NotifyUser(this, string.Format(AppResources.Text_Error_UnableToUploadTrackToServer, errorId));
                HideSystemTray();
                return;
            }
            Track.FileNameWhenLoaded = await CacheTrack(serverTrack, Track.History != null ? Track.History.Sessions : Enumerable.Empty<TrackSessionViewModel>(), Track.Id, Track.FileNameWhenLoaded);
            foreach (var bestLapTime in serverTrack.BestLaps ?? Enumerable.Empty<BestLap>())
            {
                bestLapTime.TrackId = serverTrack.id;
                if (bestLapTime.RecreateVerificationCode(user.UserId, serverTrack.id))
                    await UploadBestLapTimeToServer(bestLapTime);
                else if (NotifyUser != null)
                    NotifyUser(this, AppResources.Text_Error_BestLapTimeCouldNotBeVerified);
            }
            await LoadTracks(serverTrack.id);
            HideSystemTray();
        }

        private async Task DeleteLocalTrack()
        {
            if (Track.Id != 0) return;

            // Delete local track image
            if (!string.IsNullOrWhiteSpace(Track.BackgroundImagePath) && Track.BackgroundImagePath.StartsWith("isostore:/"))
            {
                string localPath = Track.BackgroundImagePath.Substring(10);
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                    if (store.FileExists(localPath))
                        store.DeleteFile(localPath);
            }

            // Delete local sessions
            if (Track.History != null && Track.History.Sessions != null && Track.History.Sessions.Any())
            {
                var sessionsToDelete = Track.History.Sessions.ToList();
                foreach (var session in sessionsToDelete)
                    await DeleteSession(session);
            }

            // Delete local track
            string fileName = BuildTrackFileName(Track);
            var localTrackFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(AppConstants.LOCALFOLDERNAME_TRACKCACHE, CreationCollisionOption.OpenIfExists);
            var localFileResult = await localTrackFolder.GetFileAsync(fileName);
            await localFileResult.DeleteAsync();
            await LoadTracks();
        }

        private bool InternetConnectionIsAvailable()
        {
            if (isRegisteredForNetworkStatusChanged)
                return Settings.InternetAccessIsAvailable;

            // Determine if there is Internet access and register for future connection status changes
            NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;
            isRegisteredForNetworkStatusChanged = true;
            try
            {
                var internetConnectionProfile = NetworkInformation.GetInternetConnectionProfile();
                return Settings.InternetAccessIsAvailable = internetConnectionProfile != null
                                                             && internetConnectionProfile.GetNetworkConnectivityLevel()
                                                                    == NetworkConnectivityLevel.InternetAccess;
            }
            catch (NotImplementedException)
            {
                return Settings.InternetAccessIsAvailable = NetworkInterface.GetIsNetworkAvailable();
            }
        }

        private void NetworkInformation_NetworkStatusChanged(object sender)
        {
            try
            {
                var internetConnectionProfile = NetworkInformation.GetInternetConnectionProfile();
                Settings.InternetAccessIsAvailable = internetConnectionProfile != null
                                                        && internetConnectionProfile.GetNetworkConnectivityLevel()
                                                            == NetworkConnectivityLevel.InternetAccess;
            }
            catch (NotImplementedException)
            {
                Settings.InternetAccessIsAvailable = NetworkInterface.GetIsNetworkAvailable();
            }
        }

        private static string BuildTrackFileName(Track track, long? oldId = null)
        {
            return string.Format("{0}_{1}.json", oldId ?? track.id, track.Name).Replace(" ", "").Replace(",", "").Replace(":", "");
        }
        private static string BuildTrackFileName(TrackViewModel track, long? oldId = null)
        {
            return string.Format("{0}_{1}.json", oldId ?? track.Id, track.Name).Replace(" ", "").Replace(",", "").Replace(":", "");
        }

        private static string BuildTrackHistoryFileName(TrackSession session)
        {
            string newFileName = string.Format("{0}_{1}{2}_{3}.json", session.TrackName, session.Vehicle.Make, session.Vehicle.Model, session.Timestamp.ToString("yyyyMMddHHmmssFF"))
                                       .Replace(" ", "").Replace(",", "").Replace(":", "");
            return newFileName;
        }

        private void ShowSystemTray()
        {
            dataLoaders.Enqueue(true);
            NotifyPropertyChanged("SystemTrayIsVisible");
        }

        private void HideSystemTray()
        {
            if (!dataLoaders.Any()) return;
            dataLoaders.Dequeue();
            NotifyPropertyChanged("SystemTrayIsVisible");
            if (dataLoaders.Count == 0)
                LoadingStatusText = string.Empty;
        }

        internal async Task ProcessCancelledSessionSave(ActivityState sessionSaveState, bool isLaunching)
        {
            if ((int)sessionSaveState.State < (int) ActivityStep.SessionReloadTrackHistory)
            {
                if (NotifyUser != null && Settings.IsConnectedToSkyDrive && Settings.AutouploadSessions)
                    NotifyUser(this, AppResources.Text_Blurb_IncompleteSessionSavePrompt);
            }
            else if (sessionSaveState.State == ActivityStep.SessionReloadTrackHistory && !isLaunching)
            {
                CancelAllActivities();
                using (var cancellationToken = new CancellationTokenSource())
                {
                    loadingCancellationTokens.Add(cancellationToken);
                    try
                    {
                        await LoadTrackHistory(cancellationToken.Token);
                    }
                    finally
                    {
                        loadingCancellationTokens.Remove(cancellationToken);
                    }
                }
            }
        }
    }
}