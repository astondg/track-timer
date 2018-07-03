namespace TrackTimer
{
    using System;
    using System.Device.Location;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Navigation;
    using BugSense;
    using GoogleAds;
    using Microsoft.Phone.Controls;
    using Microsoft.Phone.Maps.Controls;
    using Microsoft.Phone.Shell;
    using Microsoft.Phone.Tasks;
    using TrackTimer.Core.Extensions;
    using TrackTimer.Core.Resources;
    using TrackTimer.Core.ViewModels;
    using TrackTimer.Extensions;
    using TrackTimer.Resources;
    using TrackTimer.ViewModels;
    using System.Threading.Tasks;
    public partial class MainPage : PhoneApplicationPage
    {
        private InterstitialAd interstitialAd;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Set the data context of the listbox control to the sample data
            DataContext = App.ViewModel;
            Loaded += MainPage_Loaded;

            Visibility darkBackgroundVisibility = (Visibility)Application.Current.Resources["PhoneDarkThemeVisibility"];
            if (darkBackgroundVisibility == System.Windows.Visibility.Visible)
                mapCircuit.ColorMode = MapColorMode.Dark;
            else
                mapCircuit.ColorMode = MapColorMode.Light;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!App.ViewModel.Settings.LocationConsent)
            {
                MessageBoxResult result = MessageBox.Show(AppResources.Text_Blurb_LocationPrompt,
                                                          AppResources.Title_Prompt_Location,
                                                          MessageBoxButton.OKCancel);

                App.ViewModel.Settings.LocationConsent = result == MessageBoxResult.OK;
            }

            ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).Text = AppResources.Text_ApplicationBar_EditTrack;
            ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).Text = AppResources.Text_ApplicationBar_ShareTrack;
            ((ApplicationBarIconButton)ApplicationBar.Buttons[2]).Text = AppResources.Text_ApplicationBar_Settings;
            ((ApplicationBarMenuItem)ApplicationBar.MenuItems[0]).Text = AppResources.Text_ApplicationBar_ManageFriends;
            ((ApplicationBarMenuItem)ApplicationBar.MenuItems[1]).Text = AppResources.Text_ApplicationBar_DeleteTrack;

            App.ViewModel.dispatcher = Dispatcher;
            bool showSignInPrompt = App.ViewModel.Settings.ShowInitialSignInPrompt;
            App.ViewModel.Settings.ShowInitialSignInPrompt = false;

            if (!App.ViewModel.DataIsLoaded && !App.ViewModel.IsAuthenticated && App.ViewModel.Settings.ShouldAttemptAuthentication)
            {
                if (!showSignInPrompt || MessageBox.Show(AppResources.Text_Blurb_MustAuthenticatePrompt, AppResources.Title_Prompt_MustAuthenticate, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    await App.ViewModel.Authenticate(showSignInPrompt);
                else if (!App.ViewModel.DataIsLoaded)
                    App.ViewModel.Settings.ShouldAttemptAuthentication = false;
                
                if (!App.ViewModel.IsAuthenticating)
                    await App.ViewModel.LoadData();
            }
            else if (!App.ViewModel.DataIsLoaded && !App.ViewModel.IsAuthenticating)
            {
                await App.ViewModel.LoadData();
            }
            if (App.ViewModel.IsAuthenticated)
            {
                BugSenseHandler.Instance.UserIdentifier = App.ViewModel.UserProfile.UserId;
                ((ApplicationBarMenuItem)ApplicationBar.MenuItems[0]).IsEnabled = true;
                await App.ViewModel.BackgroundTransfers.LoadData(false);
                await RequestUpgradeRegistration();
            }
            if (App.ViewModel.Track.History != null)
                App.ViewModel.Track.History.SelectedSession = null;
            LapHistorySelector.SelectedItem = null;
            LeaderboardSelector.SelectedItem = null;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            App.ViewModel.TrackLoadComplete += ViewModel_TrackLoadComplete;
            App.ViewModel.NotifyUser += ViewModel_NotifyUser;
            if (App.ViewModel.IsTrackLoaded && !App.ViewModel.IsTrackLoading)
                DrawTrackSectorsOnMap(App.ViewModel.Track);
            if (App.ViewModel.IsAuthenticated)
            {
                ((ApplicationBarMenuItem)ApplicationBar.MenuItems[0]).IsEnabled = true;
                await App.ViewModel.BackgroundTransfers.LoadData(false);

                string launchArgs;
                if (NavigationContext.QueryString.TryGetValue("ms_nfp_launchargs", out launchArgs))
                {
                    // TODO - Launched via NFC
                }
            }
            if (App.ViewModel.Settings.IsTrial && App.ViewModel.ShowInterstitialAd)
            {
                Exception adLoadException = null;
                try
                {
                    interstitialAd = new InterstitialAd("ca-app-pub-6379678754307485/6021653656");
                    interstitialAd.FailedToReceiveAd += interstitialAd_FailedToReceiveAd;
                    interstitialAd.ReceivedAd += interstitialAd_ReceivedAd;
                    var adRequest = new AdRequest();
                    interstitialAd.LoadAd(adRequest);
                }
                catch (Exception ex)
                {
                    adLoadException = ex;
                }

                if (adLoadException != null)
                {
                    await BugSenseHandler.Instance.LogExceptionAsync(adLoadException, "Message", "Error creating Google InterstitialAd request");
                }
            }
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            App.ViewModel.TrackLoadComplete -= ViewModel_TrackLoadComplete;
            App.ViewModel.NotifyUser -= ViewModel_NotifyUser;
            if (interstitialAd != null)
            {
                interstitialAd.FailedToReceiveAd -= interstitialAd_FailedToReceiveAd;
                interstitialAd.ReceivedAd -= interstitialAd_ReceivedAd;
            }
            base.OnNavigatedFrom(e);
        }

        private void ViewModel_TrackLoadComplete(AppViewModel sender, TrackViewModel track)
        {
            DrawTrackSectorsOnMap(track);
        }

        private void DrawTrackSectorsOnMap(TrackViewModel track)
        {
            if (mapCircuit == null) return;

            if (ApplicationBar != null)
            {
                ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = true;
                ((ApplicationBarMenuItem)ApplicationBar.MenuItems[1]).IsEnabled = track.IsLocal;
                ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IsEnabled = (track.IsLocal && !App.ViewModel.Settings.IsTrial);
            }

            mapCircuit.Center = new GeoCoordinate(track.Latitude, track.Longitude);
            mapCircuit.SetZoomLevelForTrack(App.ViewModel.Track.LengthInMetres());
            mapCircuit.MapElements.Clear();
            foreach (var sector in track.Sectors)
            {
                var sectorLine = new MapPolyline();
                sectorLine.StrokeColor = sector.IsFinishLine ? Colors.Orange : Colors.Black;
                sectorLine.Path.Add(new GeoCoordinate(sector.StartLatitude, sector.StartLongitude));
                sectorLine.Path.Add(new GeoCoordinate(sector.EndLatitude, sector.EndLongitude));
                mapCircuit.MapElements.Add(sectorLine);
            }
        }

        private void ViewModel_NotifyUser(AppViewModel sender, string args)
        {
            MessageBox.Show(args);
        }

        private void Border_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!App.ViewModel.IsTrackLoaded || App.ViewModel.IsTrackLoading || App.ViewModel.IsTiming) return;

            if (!App.ViewModel.Settings.LocationConsent)
            {
                MessageBoxResult result = MessageBox.Show(AppResources.Text_Blurb_LocationPrompt,
                                                          AppResources.Title_Prompt_Location,
                                                          MessageBoxButton.OKCancel);

                if (result == MessageBoxResult.Cancel)
                    return;

                App.ViewModel.Settings.LocationConsent = true;
            }

            App.ViewModel.IsTiming = true;
            NavigationService.Navigate(new Uri("/TimerPage.xaml", System.UriKind.Relative));
        }

        private void ApplicationBarIconButton_Edit_Click(object sender, System.EventArgs e)
        {
            NavigationService.Navigate(new Uri("/EditTrackPage.xaml", System.UriKind.Relative));
        }

        private async void ApplicationBarIconButton_Share_Click(object sender, System.EventArgs e)
        {
            if (!App.ViewModel.IsAuthenticated)
            {
                if (MessageBox.Show(AppResources.Text_Blurb_MustAuthenticatePrompt, AppResources.Title_Prompt_MustAuthenticate, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    await App.ViewModel.Authenticate(false);
                
                if (!App.ViewModel.IsAuthenticated) return;
            }
            if (App.ViewModel.ShareTrack.CanExecute(null))
                App.ViewModel.ShareTrack.Execute(null);
        }

        private void ApplicationBarIconButton_ManageFriends_Click(object sender, System.EventArgs e)
        {
            NavigationService.Navigate(new Uri("/ManageFriendsPage.xaml", UriKind.Relative));
        }

        private void ApplicationBarIconButton_Delete_Click(object sender, System.EventArgs e)
        {
            var result = MessageBox.Show(AppResources.Text_Blurb_DeleteTrackPrompt, AppResources.Title_Prompt_DeleteTrack, MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel || result == MessageBoxResult.No || result == MessageBoxResult.None)
                return;

            if (App.ViewModel.DeleteTrack.CanExecute(null))
                App.ViewModel.DeleteTrack.Execute(null);
        }

        private void ApplicationBarIconButton_Settings_Click(object sender, System.EventArgs e)
        {
            NavigationService.Navigate(new Uri("/SettingsPage.xaml", System.UriKind.Relative));
        }

        private void Map_Loaded(object sender, RoutedEventArgs e)
        {
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.ApplicationId = Constants.MICROSOFT_BINGMAPS_CLIENTID;
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.AuthenticationToken = Constants.MICROSOFT_BINGMAPS_AUTHENTICATIONTOKEN;
        }

        private void TextBlock_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/PendingUploads.xaml", System.UriKind.Relative));
        }

        private void LongListSelector_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            App.ViewModel.Track.History.SelectedSession = LapHistorySelector.SelectedItem as TrackSessionViewModel;
            if (App.ViewModel.Track.History.SelectedSession != null)
                NavigationService.Navigate(new Uri("/SessionHistoryPage.xaml", System.UriKind.Relative));
        }

        private void LongListSelector_SelectionChanged_1(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var selectedLap = LeaderboardSelector.SelectedItem as BestLapViewModel;
            if (selectedLap == null) return;
            App.ViewModel.Track.History.SelectedSession = new TrackSessionViewModel
            {
                GpsDeviceName = selectedLap.GpsDeviceName,
                Weather = new WeatherConditionsViewModel { Condition = selectedLap.WeatherCondition, Temperature = selectedLap.AmbientTemperature },
                SelectedLap = selectedLap
            };
            NavigationService.Navigate(new Uri(string.Format("/LapDetailPage.xaml?{0}={1}",
                                                             AppConstants.NAVIGATIONPARAMETER_TYPE,
                                                             AppConstants.NAVIGATIONPARAMETER_VALUE_LEADERBOARD),
                                               UriKind.Relative));
        }

        private void ToggleSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            mapCircuit.CartographicMode = MapCartographicMode.Road;
        }

        private void ToggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            mapCircuit.CartographicMode = MapCartographicMode.Hybrid;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/TrackPickerPage.xaml", UriKind.Relative));
        }

        private void Image_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var marketplaceDetailTask = new MarketplaceDetailTask();
            marketplaceDetailTask.Show();
        }

        private void interstitialAd_FailedToReceiveAd(object sender, AdErrorEventArgs e)
        {
            // TODO - Show Track Timer full screen ad
            return;
        }

        private void interstitialAd_ReceivedAd(object sender, AdEventArgs e)
        {
            if (!App.ViewModel.Settings.IsTrial) return;
            interstitialAd.ShowAd();
            App.ViewModel.ShowInterstitialAd = false;
        }

        private async Task RequestUpgradeRegistration()
        {
            if (!App.ViewModel.Settings.RequestUpgradeRegistration
                || !string.IsNullOrWhiteSpace(App.ViewModel.UserProfile.EmailAddress))
                return;

            bool registerForUpgrade = false;
            await Dispatcher.InvokeAsync(() =>
            {
                var result = MessageBox.Show(AppResources.Text_Blurb_RequestUpgradeRegistration, AppResources.Text_Title_RequestUpgradeRegistration, MessageBoxButton.OKCancel);
                registerForUpgrade = result == MessageBoxResult.OK || result == MessageBoxResult.Yes;
            });

            if (registerForUpgrade)
                await App.ViewModel.Authenticate(true);

            App.ViewModel.Settings.RequestUpgradeRegistration = false;
        }
    }
}