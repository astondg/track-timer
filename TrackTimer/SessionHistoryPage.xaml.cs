namespace TrackTimer
{
    using System;
    using System.Windows;
    using System.Windows.Navigation;
    using Microsoft.Phone.Controls;
    using Microsoft.Phone.Shell;
    using Microsoft.Phone.Tasks;
    using TrackTimer.Core.Extensions;
    using TrackTimer.Core.Models;
    using TrackTimer.Core.Resources;
    using TrackTimer.Core.ViewModels;
    using TrackTimer.Extensions;
    using TrackTimer.Resources;
    using Windows.Storage;

    public partial class SessionHistoryPage : PhoneApplicationPage
    {
        public SessionHistoryPage()
        {
            InitializeComponent();
            Loaded += SessionHistoryPage_Loaded;
        }

        private void SessionHistoryPage_Loaded(object sender, RoutedEventArgs e)
        {
            ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).Text = AppResources.Text_ApplicationBar_Upload;
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (App.ViewModel.Settings.IsTrial)
            {
                SetAddMargins(this.Orientation);
                adRotatorControl.Visibility = System.Windows.Visibility.Visible;
                adDefaultControl.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                adRotatorControl.Visibility = System.Windows.Visibility.Collapsed;
                adDefaultControl.Visibility = System.Windows.Visibility.Collapsed;
            }

            var settings = App.ViewModel.Settings;
            settings.PropertyChanged += settings_PropertyChanged;
            SetFieldsUnitText(settings.IsMetricUnits);

            var progressIndicator = SystemTray.GetProgressIndicator(this);
            progressIndicator.Text = AppResources.Text_LoadingStatus_LoadingSession;
            progressIndicator.IsVisible = true;

            // TODO - Could App.ViewModel.Track or App.ViewModel.Track.History be null?
            // Could currentSession be null?
            var currentSession = App.ViewModel.Track.History.SelectedSession;
            TimeSpan? bestLapTime = App.ViewModel.Track.BestLap != null && App.ViewModel.Track.BestLap.LapTime > TimeSpan.Zero ? App.ViewModel.Track.BestLap.LapTime : (TimeSpan?)null;
            TrackSessionViewModel trackSession;
            if (currentSession.Location == TrackSessionLocation.InMemory)
            {
                trackSession = currentSession;
            }
            else if (currentSession.Location == TrackSessionLocation.LocalFile || currentSession.Location == TrackSessionLocation.ServerWithLocalFile)
            {
                var localTrackFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(AppConstants.LOCALFOLDERNAME_SESSIONS, CreationCollisionOption.OpenIfExists);
                var localFile = await localTrackFolder.GetFileAsync(currentSession.LocalFilePath);
                // Local file might not exist

                var session = await localFile.ReadJsonFileAs<TrackSession>();
                trackSession = session.AsViewModel(App.ViewModel.Track, settings.CurrentVehicle, bestLapTime, currentSession.Location, currentSession.LocalFilePath, settings.IsMetricUnits);
                trackSession.ServerFilePath = currentSession.ServerFilePath;
                trackSession.IsUploaded = currentSession.Location == TrackSessionLocation.ServerWithLocalFile;
            }
            else
            {
                var historicLapsJsonResult = await App.LiveClient.DownloadFileAsStream(currentSession.ServerFilePath);
                var session = historicLapsJsonResult.ReadJsonStreamAs<TrackSession>();
                trackSession = session.AsViewModel(App.ViewModel.Track, settings.CurrentVehicle, bestLapTime, currentSession.Location, currentSession.ServerFilePath, settings.IsMetricUnits);
                trackSession.IsUploaded = true;

                // Cache the session locally
                await App.ViewModel.SaveSessionToLocalStore(trackSession, SessionSaveType.Replace);
                trackSession.Location = currentSession.Location = TrackSessionLocation.ServerWithLocalFile;
                currentSession.LocalFilePath = trackSession.LocalFilePath;
            }

            // trackSession could be null
            ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = settings.IsConnectedToSkyDrive && !trackSession.IsUploaded;
            progressIndicator.IsVisible = false;
            progressIndicator.Text = string.Empty;
            DataContext = trackSession;
            trackSession.PropertyChanged += trackSession_PropertyChanged;
            base.OnNavigatedTo(e);
        }

        private async void trackSession_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("Notes"))
            {
                var progressIndicator = SystemTray.GetProgressIndicator(this);
                progressIndicator.Text = AppResources.Text_LoadingStatus_SavingSessionToPhone;
                progressIndicator.IsVisible = true;

                StorageFile localSessionFile = null;
                var session = DataContext as TrackSessionViewModel;
                if (session != null)
                {
                    localSessionFile = await App.ViewModel.SaveSessionToLocalStore(session, SessionSaveType.Replace);
                }

                if (localSessionFile != null && App.ViewModel.Settings.IsConnectedToSkyDrive && session.IsUploaded)
                {
                    progressIndicator.Text = AppResources.Text_LoadingStatus_SavingSessionToSkyDrive;
                    await App.LiveClient.UploadFileToTrackTimerFolder(localSessionFile, App.ViewModel.Settings.UploadSessionsOverWifi, overwrite: true);
                }

                progressIndicator.IsVisible = false;
                progressIndicator.Text = string.Empty;
            }
        }

        private async void ApplicationBarIconButton_Upload_Click(object sender, EventArgs e)
        {
            ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = false;

            if (!App.ViewModel.IsAuthenticated)
            {
                if (MessageBox.Show(AppResources.Text_Blurb_MustAuthenticatePrompt, AppResources.Title_Prompt_MustAuthenticate, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    await App.ViewModel.Authenticate(false);

                if (!App.ViewModel.IsAuthenticated)
                {
                    ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = true;
                    return;
                }
            }

            var progressIndicator = SystemTray.GetProgressIndicator(this);
            progressIndicator.Text = AppResources.Text_LoadingStatus_SavingSessionToSkyDrive;
            progressIndicator.IsVisible = true;

            var session = DataContext as TrackSessionViewModel;
            var mainPageSession = App.ViewModel.Track.History.SelectedSession;
            if (session != null && App.ViewModel.Settings.IsConnectedToSkyDrive && !session.IsUploaded)
            {
                var localSessionFile = await App.ViewModel.SaveSessionToLocalStore(session, SessionSaveType.ReadIfExists);
                var fileId = await App.ViewModel.SaveSessionToCloudStore(session, localSessionFile);
                await Dispatcher.InvokeAsync(() =>
                    {
                        session.IsUploaded = mainPageSession.IsUploaded = true;
                        session.Location = mainPageSession.Location = TrackSessionLocation.ServerWithLocalFile;
                        session.ServerFilePath = mainPageSession.ServerFilePath = fileId;
                    });

            }

            progressIndicator.IsVisible = false;
            progressIndicator.Text = string.Empty;
        }

        private async void ApplicationBarIconButton_Delete_Click(object sender, EventArgs e)
        {
            var progressIndicator = SystemTray.GetProgressIndicator(this);
            progressIndicator.Text = AppResources.Text_LoadingStatus_DeletingSession;
            progressIndicator.IsVisible = true;

            var result = MessageBox.Show(AppResources.Text_Blurb_DeleteSessionPrompt, AppResources.Title_Prompt_DeleteSession, MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel || result == MessageBoxResult.No || result == MessageBoxResult.None)
                return;

            var session = DataContext as TrackSessionViewModel;
            if (session == null) return;

            await App.ViewModel.DeleteSession(session, false);

            progressIndicator.IsVisible = false;
            progressIndicator.Text = string.Empty;

            NavigationService.GoBack();
        }

        private void LongListSelector_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            App.ViewModel.Track.History.SelectedSession.SelectedLap = lapSelector.SelectedItem as LapViewModel;
            if (App.ViewModel.Track.History.SelectedSession.SelectedLap != null)
                NavigationService.Navigate(new Uri("/LapDetailPage.xaml", System.UriKind.Relative));
        }

        private void PhoneApplicationPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            SetAddMargins(e.Orientation);
        }

        private void Image_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var marketplaceDetailTask = new MarketplaceDetailTask();
            marketplaceDetailTask.Show();
        }

        private void SetAddMargins(PageOrientation orientation)
        {
            if (Orientation == PageOrientation.Landscape
                || Orientation == PageOrientation.LandscapeLeft
                || Orientation == PageOrientation.LandscapeRight)
            {
                adRotatorControl.Margin = new Thickness(0);
                adDefaultControl.Margin = new Thickness(0);
            }
            else
            {
                adRotatorControl.Margin = new Thickness(0, 0, 0, 20);
                adDefaultControl.Margin = new Thickness(0, 0, 0, 20);
            }
        }

        private void SetFieldsUnitText(bool isMetricUnits)
        {
            if (isMetricUnits)
            {
                unitTemperature.Text = AppResources.Text_Unit_DegreesCentigrade;
                unitWindSpeed.Text = AppResources.Text_Unit_MetricSpeed;
                unitWindHeading.Text = AppResources.Text_Unit_Degrees;
                unitPreviousHourPrecipitation.Text = AppResources.Text_Unit_Millimetres;
                unitTotalDayPrecipitation.Text = AppResources.Text_Unit_Millimetres;
            }
            else
            {
                unitTemperature.Text = AppResources.Text_Unit_DegreesFarenheit;
                unitWindSpeed.Text = AppResources.Text_Unit_ImperialSpeed;
                unitWindHeading.Text = AppResources.Text_Unit_Degrees;
                unitPreviousHourPrecipitation.Text = AppResources.Text_Unit_Inches;
                unitTotalDayPrecipitation.Text = AppResources.Text_Unit_Inches;
            }
        }

        private void settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("IsMetricUnits"))
                SetFieldsUnitText(App.ViewModel.Settings.IsMetricUnits);
        }
    }
}