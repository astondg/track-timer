namespace TrackTimer.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO.IsolatedStorage;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using BugSense;
    using GalaSoft.MvvmLight.Command;
    using Microsoft.WindowsAzure.MobileServices;
    using TrackTimer.Core.Extensions;
    using TrackTimer.Core.Models;
    using TrackTimer.Core.Resources;
    using TrackTimer.Core.ViewModels;
    using TrackTimer.Extensions;
    using TrackTimer.Resources;
    using TrackTimer.Services;
    using Windows.Foundation;
    using Windows.Networking.Proximity;
    using Microsoft.Phone.Marketplace;

    public class SettingsViewModel : BaseViewModel
    {
        // The default value of our settings
        private const bool DEFAULT_ISCONNECTEDTOSKYDRIVE = false;
        private const bool DEFAULT_AUTOUPLOADSESSIONS = false;
        private const bool DEFAULT_UPLOADSESSIONSOVERWIFI = false;
        private const bool DEFAULT_SHAREBESTLAPTIMES = false;
        private const bool DEFAULT_STARTCAMERAWITHTIMING = false;
        private const bool DEFAULT_LOCATIONCONSTENT = false;
        private const bool DEFAULT_SHOWUNOFFICIALTRACKS = false;
        private const bool DEFAULT_SHOWINITIALSIGNINPROMPT = true;
        private const bool DEFAULT_SHOULDATTEMPTAUTHENTICATION = true;
        private const bool DEFAULT_INTERNETACCESSISAVAILABLE = false;
        private const bool DEFAULT_REQUESTUPGRADEREGISTRATION = true;
        // Use date Track Timer was released
        private static DateTimeOffset DEFAULT_LASTACTIVITYFEEDREFRESH = new DateTimeOffset(2014, 3, 20, 0, 0, 0, TimeZoneInfo.Local.BaseUtcOffset);
        private static VehicleViewModel DEFAULT_VEHICLE = new VehicleViewModel { Key = Guid.NewGuid(), Model = AppResources.Text_Default_UnknownVehicle };
        private static bool DEFAULT_ISMETRICUNITS
        {
            get
            {
                switch (System.Globalization.CultureInfo.CurrentCulture.Name)
                {
                    case "en-US":
                    case "en-GB":
                        return false;
                    default:
                        return true;
                }
            }
        }

        private bool isAuthenticated;
        private bool bluetoothIsEnabled;
        private bool? isTrial;
        private bool savingVehicle;
        private User userProfile;
        private IsolatedStorageSettings settings;
        private RelayCommand<VehicleViewModel> addVehicle;
        private RelayCommand<VehicleViewModel> deleteVehicle;
        private RelayCommand testCamera;
        private VehicleViewModel currentVehicle;

        /// <summary>
        /// Constructor that gets the application settings.
        /// </summary>
        public SettingsViewModel()
        {
            // Get the settings for this application.
            if (!System.ComponentModel.DesignerProperties.IsInDesignTool)
                settings = IsolatedStorageSettings.ApplicationSettings;
            currentVehicle = null;
            savingVehicle = false;
            SetNewVehicle(new VehicleViewModel { Class = VehicleClass.Standard });
            Vehicles = new ObservableCollection<VehicleViewModel>();
            BluetoothDevices = new ObservableCollection<BluetoothDeviceViewModel>();
        }

        public bool IsConnectedToSkyDrive
        {
            get { return GetValueOrDefault(DEFAULT_ISCONNECTEDTOSKYDRIVE); }
            set
            {
                if (AddOrUpdateValue(value))
                    Save();
            }
        }
        public bool AutouploadSessions
        {
            get { return GetValueOrDefault(DEFAULT_AUTOUPLOADSESSIONS); }
            set
            {
                if (AddOrUpdateValue(value))
                    Save();
            }
        }
        public bool UploadSessionsOverWifi
        {
            get { return GetValueOrDefault(DEFAULT_UPLOADSESSIONSOVERWIFI); }
            set
            {
                if (AddOrUpdateValue(value))
                    Save();
            }
        }
        public bool ShareBestLapTimes
        {
            get { return GetValueOrDefault(DEFAULT_SHAREBESTLAPTIMES); }
            set
            {
                if (AddOrUpdateValue(value))
                    Save();
            }
        }
        public bool UserProfileIsPublic
        {
            get { return userProfile.ProfileIsPublic; }
            set
            {
                userProfile.ProfileIsPublic = value;
                var userTable = App.MobileService.GetTable<User>();
                userTable.UpdateAsync(userProfile);
            }
        }
        public bool ShowUnofficialTracks
        {
            get { return GetValueOrDefault(DEFAULT_SHOWUNOFFICIALTRACKS); } 
            set
            {
                if (AddOrUpdateValue(value))
                    Save();
            }
        }
        public VehicleViewModel CurrentVehicle
        {
            get
            {
                if (currentVehicle == null)
                {
                    Vehicle vehicle = null;
                    var existingVehicleItem = GetValueOrDefault<object>(null);
                    if (existingVehicleItem is Vehicle)
                        vehicle = existingVehicleItem as Vehicle;
                    if (Vehicles != null && Vehicles.Any())
                    {
                        if (vehicle != null && vehicle.Key != Guid.Empty)
                        {
                            currentVehicle = Vehicles.SingleOrDefault(v => v.Key == vehicle.Key) ?? Vehicles.First();
                            NotifyPropertyChanged();
                        }
                        else if (existingVehicleItem != null && existingVehicleItem is Guid && (Guid)existingVehicleItem != Guid.Empty)
                        {
                            currentVehicle = Vehicles.Single(v => v.Key == (Guid)existingVehicleItem);
                            NotifyPropertyChanged();
                        }
                    }
                    else if (vehicle != null)
                    {
                        currentVehicle = vehicle.AsViewModel();
                    }
                }
                return currentVehicle;
            }
            set
            {
                currentVehicle = value;
                if (AddOrUpdateValue(value.AsModel()))
                    Save();
            }
        }
        public bool IsMetricUnits
        {
            get { return GetValueOrDefault(DEFAULT_ISMETRICUNITS); }
            set
            {
                if (AddOrUpdateValue(value))
                    Save();
            }
        }
        public bool StartCameraWithTiming
        {
            get { return GetValueOrDefault(DEFAULT_STARTCAMERAWITHTIMING); }
            set
            {
                if (AddOrUpdateValue(value))
                    Save();
            }
        }
        public string CameraPassword
        {
            get { return GetValueOrDefault(string.Empty); }
            set
            {
                if (AddOrUpdateValue(value))
                    Save();
            }
        }

        public IEnumerable<string> EnabledBuetoothDevicesCanonicalNames
        {
            get { return GetValueOrDefault(Enumerable.Empty<string>()); }
            set
            {
                if (AddOrUpdateValue(value))
                    Save();
            }
        }

        public bool BluetoothIsEnabled
        {
            get { return bluetoothIsEnabled; }
            set { SetProperty(ref bluetoothIsEnabled, value); }
        }

        public bool LocationConsent
        {
            get { return GetValueOrDefault(DEFAULT_LOCATIONCONSTENT); }
            set
            {
                if (AddOrUpdateValue(value))
                    Save();
            }
        }

        public ActivityState LastActivityState
        {
            get { return GetValueOrDefault((ActivityState)null); }
            set
            {
                if (AddOrUpdateValue(value))
                    Save();
            }
        }

        public bool ShowInitialSignInPrompt
        {
            get { return GetValueOrDefault(DEFAULT_SHOWINITIALSIGNINPROMPT); }
            set
            {
                if (AddOrUpdateValue(value))
                    Save();
            }
        }

        public bool ShouldAttemptAuthentication
        {
            get { return GetValueOrDefault(DEFAULT_SHOULDATTEMPTAUTHENTICATION); }
            set
            {
                if (AddOrUpdateValue(value))
                    Save();
            }
        }

        public bool IsAuthenticated
        {
            get { return isAuthenticated; }
            set
            {
                if (SetProperty(ref isAuthenticated, value))
                    NotifyPropertyChanged("IsAuthenticatedAndFullLicence");
            }
        }

        public bool IsTrial
        {
            get
            {
                if (!isTrial.HasValue)
                {
#if DEBUG
                    IsTrial = false;
#else
                    var licence = new LicenseInformation();
                    IsTrial = licence.IsTrial();
#endif
                }
                return isTrial.Value;
            }
            private set
            {
                if (SetProperty(ref isTrial, value))
                {
                    NotifyPropertyChanged("IsAuthenticatedAndFullLicence");

                    // If this is a trial then ensure all full features are disabled
                    if (value)
                        RestrictToTrialFeatures();
                }
            }
        }

        public DateTimeOffset LastActivityFeedRefresh
        {
            get { return GetValueOrDefault(DEFAULT_LASTACTIVITYFEEDREFRESH); }
            set
            {
                if (AddOrUpdateValue(value))
                    Save();
            }
        }

        public bool IsAuthenticatedAndFullLicence
        {
            get { return IsAuthenticated && !IsTrial; }
        }

        public bool InternetAccessIsAvailable
        {
            get { return GetValueOrDefault(DEFAULT_INTERNETACCESSISAVAILABLE); }
            set
            {
                if (AddOrUpdateValue(value))
                    Save();
            }
        }

        public bool RequestUpgradeRegistration
        {
            get { return GetValueOrDefault(DEFAULT_REQUESTUPGRADEREGISTRATION); }
            set
            {
                if (AddOrUpdateValue(value))
                    Save();
            }
        }

        public bool IsRegisteredForUpgrade
        {
            get { return !string.IsNullOrWhiteSpace(App.ViewModel.UserProfile.EmailAddress); }
            set { NotifyPropertyChanged(); }
        }

        public ICommand AddVehicle
        {
            get
            {
                if (addVehicle == null)
                    addVehicle = new RelayCommand<VehicleViewModel>(async (newVehicle) =>
                        {
                            if (newVehicle == null || newVehicle.HasErrors) return;

                            savingVehicle = true;
                            if (!App.ViewModel.IsAuthenticated)
                            {
                                if (newVehicle.Key == Guid.Empty)
                                    newVehicle.Key = Guid.NewGuid();
                                CurrentVehicle = newVehicle;
                                savingVehicle = false;
                                NotifyPropertyChanged("SavedVehicle");
                                return;
                            }
                            var vehicleTable = App.MobileService.GetTable<Vehicle>();
                            if (newVehicle.Id < 1)
                            {
                                if (IsTrial)
                                {
                                    savingVehicle = false;
                                    return;
                                }

                                if (newVehicle.Key == Guid.Empty)
                                    newVehicle.Key = Guid.NewGuid();

                                MobileServiceInvalidOperationException saveException = null;

                                try
                                {
                                    await vehicleTable.InsertAsync(newVehicle.AsModel());
                                }
                                catch (MobileServiceInvalidOperationException ex)
                                {
                                    saveException = ex;
                                }

                                if (saveException != null)
                                {
                                    string errorId = await BugSenseHandler.Instance.LogExceptionWithId(App.MobileService.CurrentUser, saveException);
                                    newVehicle.AddServerError(string.Format(AppResources.Text_Error_UnableToSaveVehicle, errorId));
                                    savingVehicle = false;
                                    return;
                                }

                                var serverVehicles = await vehicleTable.Where(v => v.UserName == App.MobileService.CurrentUser.UserId).ToEnumerableAsync();
                                // TODO - Figure out how to compare the GUID key to the UniqueIdentifier key
                                newVehicle = serverVehicles.SingleOrDefault(v => v.Key == newVehicle.Key).AsViewModel();
                                Vehicles.Add(newVehicle);
                                SetNewVehicle(new VehicleViewModel());
                            }
                            else
                            {
                                // Updated Vehicle
                                await vehicleTable.UpdateAsync(newVehicle.AsModel());
                            }
                            savingVehicle = false;
                            NotifyPropertyChanged("SavedVehicle");
                        }, (newVehicle) => newVehicle != null && !newVehicle.HasErrors && !savingVehicle);
                return addVehicle;
            }
        }

        public ICommand DeleteVehicle
        {
            get
            {
                if (deleteVehicle == null)
                    deleteVehicle = new RelayCommand<VehicleViewModel>(async (vehicle) =>
                    {
                        if (vehicle.Id > 0)
                        {
                            var vehicleTable = App.MobileService.GetTable<Vehicle>();
                            await vehicleTable.DeleteAsync(vehicle.AsModel());
                        }
                        Vehicles.Remove(vehicle);
                    });
                return deleteVehicle;
            }
        }

        public ICommand TestCamera
        {
            get
            {
                if (testCamera == null)
                    testCamera = new RelayCommand(async () =>
                    {
                        var camera = !IsTrial && StartCameraWithTiming
                            ? new GoProCamera(AppConstants.DEFAULT_GOPRO_IPADDRESS, CameraPassword)
                            : null;

                        if (camera != null)
                        {
                            // Send the power on command and wait 5sec for the camera to turn on
                            if (!await camera.PowerOn() || !await ThreadingExtensions.Delay(5000))
                                DisplayUserMessage(AppResources.Text_Error_Camera_UnableToPowerOn);
                            // Start recording command and let it record for 2sec
                            else if (!await camera.StartRecording() || !await ThreadingExtensions.Delay(2000))
                                DisplayUserMessage(AppResources.Text_Error_Camera_UnableToStartRecording);
                            // Stop recording
                            else if (!await camera.StopRecording() || !await ThreadingExtensions.Delay(1000))
                                DisplayUserMessage(AppResources.Text_Error_Camera_UnableToStopRecording);
                            // Power off
                            else if (!await camera.PowerOff())
                                DisplayUserMessage(AppResources.Text_Error_Camera_UnableToPowerOff);
                            // All successful
                            else
                                DisplayUserMessage(AppResources.Text_Blurb_Camera_TestSuccessfulPleaseVerifyYourCameraRecordedAShortClip);
                        }
                    });
                return testCamera;
            }
        }

        public event TypedEventHandler<SettingsViewModel, string> NotifyUser;

        public IDictionary<VehicleClass, string> VehicleClasses { get { return Constants.VehicleClasses; } }
        public VehicleViewModel NewVehicle { get; set; }
        public ObservableCollection<VehicleViewModel> Vehicles { get; private set; }
        public ObservableCollection<BluetoothDeviceViewModel> BluetoothDevices { get; private set; }

        public async Task LoadData(string userId, User userProfile)
        {
            this.userProfile = userProfile;
            currentVehicle = null;
            Vehicles.Clear();
            bool isAuthenticated = !string.IsNullOrWhiteSpace(userId) && InternetAccessIsAvailable;
            bool replaceCurrentVehicle = false;
            IMobileServiceTable<Vehicle> vehicleTable = isAuthenticated ? App.MobileService.GetTable<Vehicle>() : null;
            if (isAuthenticated)
            {
                IEnumerable<Vehicle> serverVehicles = await vehicleTable.Where(v => v.UserName == userId).ToEnumerableAsync();
                // If the vehicle is not the default and it does not already exist on the server then insert it
                if (CurrentVehicle != null && CurrentVehicle.Model != DEFAULT_VEHICLE.Model && !serverVehicles.Any(v => v.Key == CurrentVehicle.Key))
                {
                    var vehicleModel = CurrentVehicle.AsModel();
                    await vehicleTable.InsertAsync(vehicleModel);
                    CurrentVehicle.Id = vehicleModel.id;
                    Vehicles.Add(CurrentVehicle);
                }
                else
                {
                    replaceCurrentVehicle = true;
                }
                foreach (var serverVehicle in serverVehicles)
                    Vehicles.Add(serverVehicle.AsViewModel());
            }
            else if (CurrentVehicle != null)
            {
                Vehicles.Add(currentVehicle);
            }

            if (!Vehicles.Any())
            {
                Vehicles.Add(DEFAULT_VEHICLE);
                CurrentVehicle = DEFAULT_VEHICLE;
            }
            else if (CurrentVehicle == null || replaceCurrentVehicle)
            {
                // Either select the first vehicle OR refresh the current vehicle details from the server
                CurrentVehicle = CurrentVehicle == null ? Vehicles.First() : Vehicles.SingleOrDefault(v => v.Key == CurrentVehicle.Key) ?? Vehicles.First();
            }
        }

        public void ResetApplicationLicence()
        {
            // Set to false, will be retrieved when IsTrial is accessed
            isTrial = null;
        }

#if DEBUG
        // For development only
        public void ToggleTrialMode()
        {
            IsTrial = !IsTrial;
        }
#endif

        public async Task<bool> LoadBluetoothDevices()
        {
            BluetoothDevices.Clear();
            //PeerFinder.AlternateIdentities["Bluetooth:SDP"] = "{00001101-0000-1000-8000-00805f9b34fb}";
            if (PeerFinder.AlternateIdentities.ContainsKey("Bluetooth:Paired"))
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
                    BluetoothIsEnabled = false;
                    return false;
                }
                throw;
            }
            BluetoothIsEnabled = true;
            foreach (var device in availableDevices)
                BluetoothDevices.Add(new BluetoothDeviceViewModel
                    {
                        DisplayName = device.DisplayName,
                        CanonicalName = device.HostName.CanonicalName
                    });
            return true;
        }

        public void SetNewVehicle(VehicleViewModel vehicle)
        {
            if (NewVehicle != null)
                NewVehicle.PropertyChanged -= NewVehicle_PropertyChanged;
            NewVehicle = vehicle;
            NewVehicle.PropertyChanged += NewVehicle_PropertyChanged;
        }

        /// <summary>
        /// Update a setting value for our application. If the setting does not
        /// exist, then add the setting.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected bool AddOrUpdateValue(Object value, [CallerMemberName] string propertyName = null)
        {
            if (!settings.Contains(propertyName))
            {
                settings.Add(propertyName, value);
                NotifyPropertyChanged(propertyName);
                return true;
            }
            if (settings[propertyName] != value)
            {
                settings[propertyName] = value;
                NotifyPropertyChanged(propertyName);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get the current value of the setting, or if it is not found, set the 
        /// setting to the default setting.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        protected T GetValueOrDefault<T>(T defaultValue, [CallerMemberName] string propertyName = null)
        {
            return settings.Contains(propertyName)
                        ? (T)settings[propertyName]
                        : defaultValue;
        }

        /// <summary>
        /// Save the settings.
        /// </summary>
        protected void Save()
        {
            settings.Save();
        }

        private void NewVehicle_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            addVehicle.RaiseCanExecuteChanged();
        }

        private void RestrictToTrialFeatures()
        {
            StartCameraWithTiming = false;
            ShowUnofficialTracks = false;
            ShareBestLapTimes = false;
            EnabledBuetoothDevicesCanonicalNames = Enumerable.Empty<string>();
        }

        private void DisplayUserMessage(string message)
        {
            if (NotifyUser != null)
                NotifyUser(this, message);
        }
    }
}