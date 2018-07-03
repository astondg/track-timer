namespace TrackTimer
{
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Navigation;
    using Microsoft.Phone.Controls;
    using Microsoft.Phone.Shell;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Media;
    using TrackTimer.Core.Resources;
    using TrackTimer.Resources;

    public partial class TimerPage : PhoneApplicationPage
    {
        public TimerPage()
        {
            InitializeComponent();
            OrientationChanged += TimerPage_OrientationChanged;
            Loaded += TimerPage_Loaded;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;

            if (App.ViewModel.Timer == null || !App.ViewModel.Timer.IsTiming)
                App.ViewModel.CreateTimerViewModelForCurrentSettings();

            App.ViewModel.Timer.CurrentPhoneOrientation = (DeviceOrientation)Orientation;
            App.ViewModel.Timer.Dispatcher = Dispatcher;
            this.DataContext = App.ViewModel.Timer;
            metTimingStarted.MediaEnded += metTimingStarted_MediaEnded;
            base.OnNavigatedTo(e);
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            var result = MessageBox.Show(AppResources.Text_Blurb_CancelTimingPrompt, AppResources.Title_Prompt_CancelTiming, MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel)
                e.Cancel = true;
            base.OnBackKeyPress(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Enabled;
            App.ViewModel.Timer.TimingStopped -= Timer_TimingStopped;
            App.ViewModel.Timer.TimingStarted -= ViewModel_TimingStarted;
            metTimingStarted.MediaEnded -= metTimingStarted_MediaEnded;

            if (App.ViewModel.NeedToResumeMusic)
            {
                MediaPlayer.Resume();
                App.ViewModel.NeedToResumeMusic = false;
            }

            // Only stop timing if the user has navigated back, otherwise app is being sent to background and timing should continue
            if (e.NavigationMode == NavigationMode.Back)
            {
                if (App.ViewModel.Timer.IsTiming)
                    App.ViewModel.Timer.StopLapTimer();
                App.ViewModel.Timer.UnloadGps();
                App.ViewModel.IsTiming = false;
                App.ViewModel.Timer = null;
            }
            base.OnNavigatedFrom(e);
        }

        private async void TimerPage_Loaded(object sender, RoutedEventArgs e)
        {
            App.ViewModel.Timer.TimingStopped += Timer_TimingStopped;
            await App.ViewModel.Timer.Initialise();
            App.ViewModel.Timer.TimingStarted += ViewModel_TimingStarted;
            await App.ViewModel.Timer.StartLapTimer();
        }

        private void Timer_TimingStopped(ViewModels.TimerViewModel sender, ViewModels.TimingStoppedViewModel args)
        {
            if (!args.IsSuccessful && !string.IsNullOrWhiteSpace(args.CompletionMessage))
                Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show(args.CompletionMessage);
                        
                        if (NavigationService.CanGoBack)
                            NavigationService.GoBack();
                    });
        }

        private void TimerPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            if (e.Orientation == PageOrientation.Portrait || e.Orientation == PageOrientation.PortraitDown || e.Orientation == PageOrientation.PortraitUp)
                tbxLapTime.FontSize = 104;
            else
                tbxLapTime.FontSize = 108;

            if (App.ViewModel.Timer != null)
                App.ViewModel.Timer.CurrentPhoneOrientation = (DeviceOrientation)e.Orientation;
        }

        private void ViewModel_TimingStarted(ViewModels.TimerViewModel sender, object args)
        {
            FrameworkDispatcher.Update();
            if (!MediaPlayer.GameHasControl)
            {
                App.ViewModel.NeedToResumeMusic = true;
                MediaPlayer.Pause();
            }

            Dispatcher.BeginInvoke(async () =>
                {
                    metTimingStarted.Play();
                    var currentBackground = LayoutRoot.Background;
                    LayoutRoot.Background = this.Resources[AppConstants.RESOURCE_BRUSH_PHONECONTRASTBACKGROUND] as Brush;
                    await Task.Delay(1000);
                    LayoutRoot.Background = currentBackground;
                });
        }

        private void metTimingStarted_MediaEnded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (App.ViewModel.NeedToResumeMusic)
            {
                MediaPlayer.Resume();
                App.ViewModel.NeedToResumeMusic = false;
            }
        }
    }
}