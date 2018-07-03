namespace TrackTimer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Windows;
    using Microsoft.Live;
    using Microsoft.Phone.Controls;
    using Microsoft.Phone.Tasks;
    using TrackTimer.Core.ViewModels;
    using TrackTimer.Resources;
    using TrackTimer.ViewModels;
    using Windows.Storage;

    public partial class SettingsView : PhoneApplicationPage
    {
        private CancellationTokenSource loadingCancellationToken;

        public SettingsView()
        {
            InitializeComponent();
        }

        protected async override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            loadingCancellationToken = new CancellationTokenSource();

            this.DataContext = App.ViewModel.Settings;

            App.ViewModel.Settings.NotifyUser += Settings_NotifyUser;

            if (App.ViewModel.IsAuthenticated)
                btnSignin.Content = AppResources.Text_Button_SignOut;
            else
                btnSignin.Content = AppResources.Text_Button_SignIn;

            string fullName = App.LiveClient.IsConnected ? await App.LiveClient.GetUsersFullName(loadingCancellationToken.Token) : null;
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                txtAccountName.Text = fullName;
                txtAccountName.Visibility = Visibility.Visible;
            }
            App.ViewModel.Settings.PropertyChanged += Settings_PropertyChanged;
            AssemblyName assemblyName = new AssemblyName(Assembly.GetExecutingAssembly().FullName);
            Version version = assemblyName.Version;
            tbkAppVersion.Text = App.ViewModel.Settings.IsTrial
                                    ? string.Format("{0} {1}", version.ToString(), AppResources.Text_Blurb_Trial)
                                    : version.ToString();

            Visibility darkBackgroundVisibility = (Visibility)Application.Current.Resources["PhoneDarkThemeVisibility"];
            if (darkBackgroundVisibility == Visibility.Visible)
                logoWeatherUnderground.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("/Assets/wundergroundLogo_4c_rev_horz.png", UriKind.Relative));
            else
                logoWeatherUnderground.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("/Assets/wundergroundLogo_4c_horz.png", UriKind.Relative));

#if DEBUG
            btnToggleTrial.Visibility = Visibility.Visible;
#endif

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            loadingCancellationToken.Cancel();
            loadingCancellationToken.Dispose();
            App.ViewModel.Settings.NotifyUser -= Settings_NotifyUser;
            App.ViewModel.Settings.PropertyChanged -= Settings_PropertyChanged;
            base.OnNavigatedFrom(e);
        }

        private void Settings_NotifyUser(SettingsViewModel sender, string args)
        {
            MessageBox.Show(args);
        }

        private async void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("IsAuthenticated", StringComparison.OrdinalIgnoreCase))
            {
                string fullName = App.LiveClient.IsConnected ? await App.LiveClient.GetUsersFullName(loadingCancellationToken.Token) : null;
                if (!string.IsNullOrWhiteSpace(fullName))
                {
                    txtAccountName.Text = fullName;
                    txtAccountName.Visibility = Visibility.Visible;
                }
            }
        }

        private void WrapPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!App.ViewModel.IsAuthenticated)
            {
                MessageBox.Show(AppResources.Text_Blurb_MustAuthenticatePrompt);
                return;
            }
            App.ViewModel.Settings.NewVehicle = new VehicleViewModel { Class = Core.Resources.VehicleClass.Standard };
            NavigationService.Navigate(new System.Uri("/AddVehiclePage.xaml", System.UriKind.Relative));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ConnectionSettingsTask connectionSettingsTask = new ConnectionSettingsTask();
            connectionSettingsTask.ConnectionSettingsType = ConnectionSettingsType.Bluetooth;
            connectionSettingsTask.Show();
        }

        private async void LongListSelector_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            List<string> selectedDeviceNames = App.ViewModel
                                                  .Settings
                                                  .EnabledBuetoothDevicesCanonicalNames
                                                  .Where(d => !e.RemovedItems.Cast<BluetoothDeviceViewModel>().Any(ri => ri.CanonicalName.Equals(d, StringComparison.OrdinalIgnoreCase)))
                                                  .ToList();

            try
            {
                foreach (var item in e.AddedItems)
                {
                    var bluetoothDevice = item as BluetoothDeviceViewModel;
                    if (bluetoothDevice == null || selectedDeviceNames.Any(dn => dn.Equals(bluetoothDevice.CanonicalName, StringComparison.OrdinalIgnoreCase)))
                        continue;
                    using (var socket = new Windows.Networking.Sockets.StreamSocket())
                        await socket.ConnectAsync(new Windows.Networking.HostName(bluetoothDevice.CanonicalName), "{00001101-0000-1000-8000-00805f9b34fb}");
                    selectedDeviceNames.Add(bluetoothDevice.CanonicalName);
                }
            }
            catch (System.Exception)
            {
                MessageBox.Show("One or more selected devices do not support the Serial Port Profile and cannot be used");
            }

            App.ViewModel.Settings.EnabledBuetoothDevicesCanonicalNames = selectedDeviceNames;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            ConnectionSettingsTask connectionSettingsTask = new ConnectionSettingsTask();
            connectionSettingsTask.ConnectionSettingsType = ConnectionSettingsType.WiFi;
            connectionSettingsTask.Show();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (App.ViewModel.Settings.IsTrial) return;
            var vehicle = e.OriginalSource as VehicleViewModel;
            if (vehicle != null && App.ViewModel.Settings.DeleteVehicle.CanExecute(vehicle))
                App.ViewModel.Settings.DeleteVehicle.Execute(vehicle);
        }

        private void LongListSelector_SelectionChanged_1(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 1 || e.AddedItems[0] == null) return;
            App.ViewModel.Settings.SetNewVehicle(e.AddedItems[0] as VehicleViewModel);
            var list = sender as LongListSelector;
            NavigationService.Navigate(new System.Uri("/AddVehiclePage.xaml", System.UriKind.Relative));
            if (list != null)
                list.SelectedItem = null;
        }

        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(AppResources.Text_Blurb_DeleteTrackCachePrompt, AppResources.Title_Prompt_DeleteTrackCache, MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel || result == MessageBoxResult.No || result == MessageBoxResult.None)
                return;

            try
            {
                var trackCacheFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync(AppConstants.LOCALFOLDERNAME_TRACKCACHE);
                await trackCacheFolder.DeleteAsync();
                var backgroundFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync(AppConstants.LOCALFOLDERNAME_BACKGROUND);
                await backgroundFolder.DeleteAsync();
            }
            catch (Exception)
            { }
            await ApplicationData.Current.LocalFolder.CreateFolderAsync(AppConstants.LOCALFOLDERNAME_TRACKCACHE, CreationCollisionOption.ReplaceExisting);
            await ApplicationData.Current.LocalFolder.CreateFolderAsync(AppConstants.LOCALFOLDERNAME_BACKGROUND, CreationCollisionOption.ReplaceExisting);
            await App.ViewModel.LoadData();
        }

        private async void Button_Click_3(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(AppResources.Text_Blurb_DeleteAllLocalSessionsPrompt, AppResources.Title_Prompt_DeleteAllLocalSessions, MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel || result == MessageBoxResult.No || result == MessageBoxResult.None)
                return;

            try
            {
                var sessionsFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync(AppConstants.LOCALFOLDERNAME_SESSIONS);
                await sessionsFolder.DeleteAsync();
            }
            catch (Exception)
            { }
            await ApplicationData.Current.LocalFolder.CreateFolderAsync(AppConstants.LOCALFOLDERNAME_SESSIONS, CreationCollisionOption.ReplaceExisting);
            await App.ViewModel.LoadData();
        }

        private async void LongListMultiSelector_Loaded(object sender, RoutedEventArgs e)
        {
            bool result = await App.ViewModel.Settings.LoadBluetoothDevices();
            if (result)
            {
                // TODO - There has to be a better way to do this
                foreach (var deviceCanonicalName in App.ViewModel.Settings.EnabledBuetoothDevicesCanonicalNames)
                {
                    var device = App.ViewModel.Settings.BluetoothDevices.SingleOrDefault(d => d.CanonicalName == deviceCanonicalName);
                    if (device != null)
                        llms.SelectedItems.Add(device);
                }
            }
            llms.SelectionChanged += LongListSelector_SelectionChanged;
        }

        private void LongListMultiSelector_Unloaded(object sender, RoutedEventArgs e)
        {
            llms.SelectionChanged -= LongListSelector_SelectionChanged;
        }

        private async void btnSignin_Click(object sender, RoutedEventArgs e)
        {
            if (App.ViewModel.IsAuthenticated)
            {
                App.LiveClient.Logout();
                App.ViewModel.IsAuthenticated = false;
                App.ViewModel.Settings.IsAuthenticated = false;
                App.ViewModel.Settings.ShouldAttemptAuthentication = false;
                txtAccountName.Text = string.Empty;
                txtAccountName.Visibility = Visibility.Collapsed;
                btnSignin.Content = AppResources.Text_Button_SignIn;
            }
            else
            {
                App.ViewModel.Settings.ShouldAttemptAuthentication = true;
                bool authenticated = await App.ViewModel.Authenticate(true);
                if (authenticated)
                {
                    string fullName = await App.LiveClient.GetUsersFullName(loadingCancellationToken.Token);
                    txtAccountName.Text = fullName;
                    txtAccountName.Visibility = Visibility.Visible;
                    btnSignin.Content = AppResources.Text_Button_SignOut;
                }
                else
                {
                    txtAccountName.Text = string.Empty;
                    txtAccountName.Visibility = Visibility.Collapsed;
                    btnSignin.Content = AppResources.Text_Button_SignIn;
                }
            }
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            var marketplaceDetailTask = new MarketplaceDetailTask();
            marketplaceDetailTask.Show();
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
#if DEBUG
            App.ViewModel.Settings.ToggleTrialMode();
#endif
        }

        private async void Button_Click_6(object sender, RoutedEventArgs e)
        {
            RegisterForUpgrade.IsEnabled = false;
            await App.ViewModel.Authenticate(true);
            App.ViewModel.Settings.RequestUpgradeRegistration = false;
            App.ViewModel.Settings.IsRegisteredForUpgrade = true;
            RegisterForUpgrade.IsEnabled = true;
        }
    }
}