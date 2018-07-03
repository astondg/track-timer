namespace TrackTimer
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.InteropServices.WindowsRuntime;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using System.Windows.Navigation;
    using Microsoft.Phone.Controls;
    using Microsoft.Phone.Shell;
    using NdefLibrary.Ndef;
    using TrackTimer.Core.ViewModels;
    using TrackTimer.Resources;
    using TrackTimer.ViewModels;
    using Windows.Networking.Proximity;

    public partial class ManageFriendsPage : PhoneApplicationPage
    {
        private const string VisibilityGroupName = "VisibilityStates";
        private const string OpenVisibilityStateName = "Open";
        private const string ClosedVisibilityStateName = "Closed";

        private Storyboard _closedStoryboard;
        private ProximityDevice proximityDevice;
        private ApplicationBarIconButton selectFriendRequestsButton;
        private ApplicationBarIconButton acceptFriendRequestsButton;
        private ApplicationBarIconButton rejectFriendRequestsButton;
        private ApplicationBarIconButton selectFriendsButton;
        private ApplicationBarIconButton deFriendButton;
        private ManageFriendsViewModel viewModel;

        public ManageFriendsPage()
        {
            InitializeComponent();

            DataContext = viewModel = new ManageFriendsViewModel(App.ViewModel.Friends);

            selectFriendRequestsButton = new ApplicationBarIconButton(new Uri("/Toolkit.Content/ApplicationBar.Select.png", UriKind.Relative)) { Text = AppResources.Text_ApplicationBar_Select };
            selectFriendRequestsButton.Click += selectFriendRequestsButton_Click;
            acceptFriendRequestsButton = new ApplicationBarIconButton(new Uri("/Toolkit.Content/ApplicationBar.Check.png", UriKind.Relative)) { Text = AppResources.Text_ContextMenu_AcceptFriend };
            acceptFriendRequestsButton.Click += acceptFriendRequestsButton_Click;
            rejectFriendRequestsButton = new ApplicationBarIconButton(new Uri("/Toolkit.Content/ApplicationBar.Cancel.png", UriKind.Relative)) { Text = AppResources.Text_ContextMenu_RejectFriend };
            rejectFriendRequestsButton.Click += rejectFriendRequestsButton_Click;
            selectFriendsButton = new ApplicationBarIconButton(new Uri("/Toolkit.Content/ApplicationBar.Select.png", UriKind.Relative)) { Text = AppResources.Text_ApplicationBar_Select };
            selectFriendsButton.Click += selectFriendsButton_Click;
            deFriendButton = new ApplicationBarIconButton(new Uri("/Toolkit.Content/ApplicationBar.Delete.png", UriKind.Relative)) { Text = AppResources.Text_ContextMenu_DeleteFriend };
            deFriendButton.Click += deFriendButton_Click;

            // Hook up to storyboard(s)
            FrameworkElement templateRoot = VisualTreeHelper.GetChild(this, 0) as FrameworkElement;
            if (null != templateRoot)
            {
                foreach (VisualStateGroup group in VisualStateManager.GetVisualStateGroups(templateRoot))
                {
                    if (VisibilityGroupName == group.Name)
                    {
                        foreach (VisualState state in group.States)
                        {
                            if ((ClosedVisibilityStateName == state.Name) && (null != state.Storyboard))
                            {
                                _closedStoryboard = state.Storyboard;
                                _closedStoryboard.Completed += OnClosedStoryboardCompleted;
                            }
                        }
                    }
                }
            }

            // Play the Open state
            VisualStateManager.GoToState(this, OpenVisibilityStateName, true);
        }

        protected async override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            SetupFriendsPivotAppBar();
            PeerFinder.TriggeredConnectionStateChanged += PeerFinder_TriggeredConnectionStateChanged;
            App.ViewModel.Friends.Clear();
            await App.ViewModel.LoadFriends();
            selectFriendsButton.IsEnabled = viewModel.ConfirmedFriends.Any();
            selectFriendRequestsButton.IsEnabled = viewModel.FriendRequests.Any();
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            PeerFinder.TriggeredConnectionStateChanged -= PeerFinder_TriggeredConnectionStateChanged;
            base.OnNavigatedFrom(e);
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            if (null == e)
            {
                throw new ArgumentNullException("e");
            }

            // Cancel back action so we can play the Close state animation (then go back)
            e.Cancel = true;
            ClosePickerPage();
        }

        private void ClosePickerPage()
        {
            // Play the Close state (if available)
            if (null != _closedStoryboard)
            {
                VisualStateManager.GoToState(this, ClosedVisibilityStateName, true);
            }
            else
            {
                OnClosedStoryboardCompleted(null, null);
            }
        }

        private void OnClosedStoryboardCompleted(object sender, EventArgs e)
        {
            // Close the picker page
            NavigationService.GoBack();
        }

        private void PeerFinder_TriggeredConnectionStateChanged(object sender, TriggeredConnectionStateChangedEventArgs args)
        {
            if (args.State == TriggeredConnectState.PeerFound)
            {
                proximityDevice = ProximityDevice.GetDefault();
                var message = new NdefMessage { new NdefUriRecord { Uri = "tracktimer:addFriend?" + App.MobileService.CurrentUser.UserId } };
                proximityDevice.PublishBinaryMessage("NDEF", message.ToByteArray().AsBuffer(), MessageWrittenHandler);
            }
        }

        private void MessageWrittenHandler(ProximityDevice sender, long messageId)
        {
            proximityDevice.StopPublishingMessage(messageId);
        }

        private void wplAddFriend_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!App.ViewModel.IsAuthenticated)
            {
                MessageBox.Show(AppResources.Text_Blurb_MustAuthenticatePrompt);
                return;
            }
            NavigationService.Navigate(new System.Uri("/AddFriendPage.xaml", System.UriKind.Relative));
        }

        private void friendsPivot_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (friendsPivot.SelectedItem == friendsPivotItem)
                SetupFriendsPivotAppBar();
            else if (friendsPivot.SelectedItem == requestsPivotItem)
                SetupRequestsPivotAppBar();
        }

        private void SetupFriendsPivotAppBar()
        {
            this.ApplicationBar.Buttons.Clear();
            selectFriendsButton.IsEnabled = viewModel.ConfirmedFriends.Any();
            deFriendButton.IsEnabled = false;
            this.ApplicationBar.Buttons.Add(selectFriendsButton);
            this.ApplicationBar.Buttons.Add(deFriendButton);
        }

        private void SetupRequestsPivotAppBar()
        {
            this.ApplicationBar.Buttons.Clear();
            selectFriendRequestsButton.IsEnabled = viewModel.FriendRequests.Any();
            acceptFriendRequestsButton.IsEnabled = rejectFriendRequestsButton.IsEnabled = false;
            this.ApplicationBar.Buttons.Add(selectFriendRequestsButton);
            this.ApplicationBar.Buttons.Add(acceptFriendRequestsButton);
            this.ApplicationBar.Buttons.Add(rejectFriendRequestsButton);
        }

        private void friendsList_IsSelectionEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            selectFriendsButton.IsEnabled = !friendsList.IsSelectionEnabled;
            deFriendButton.IsEnabled = friendsList.IsSelectionEnabled && friendsList.SelectedItems.Count > 0;
        }

        private void friendsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            deFriendButton.IsEnabled = friendsList.IsSelectionEnabled && friendsList.SelectedItems.Count > 0;
            this.ApplicationBar.Mode = friendsList.IsSelectionEnabled ? ApplicationBarMode.Default : ApplicationBarMode.Minimized;
        }

        private void friendRequestsList_IsSelectionEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            selectFriendRequestsButton.IsEnabled = !friendRequestsList.IsSelectionEnabled;
            acceptFriendRequestsButton.IsEnabled = friendRequestsList.IsSelectionEnabled && friendRequestsList.SelectedItems.Count > 0;
            rejectFriendRequestsButton.IsEnabled = friendRequestsList.IsSelectionEnabled && friendRequestsList.SelectedItems.Count > 0;
        }

        private void friendRequestsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            acceptFriendRequestsButton.IsEnabled = friendRequestsList.IsSelectionEnabled && friendRequestsList.SelectedItems.Count > 0;
            rejectFriendRequestsButton.IsEnabled = friendRequestsList.IsSelectionEnabled && friendRequestsList.SelectedItems.Count > 0;
            this.ApplicationBar.Mode = friendRequestsList.IsSelectionEnabled ? ApplicationBarMode.Default : ApplicationBarMode.Minimized;
        }

        private void selectFriendRequestsButton_Click(object sender, EventArgs e)
        {
            friendRequestsList.IsSelectionEnabled = true;
        }

        private void acceptFriendRequestsButton_Click(object sender, EventArgs e)
        {
            foreach (FriendViewModel friend in friendRequestsList.SelectedItems)
            {
                if (App.ViewModel.AcceptFriend.CanExecute(friend))
                {
                    App.ViewModel.AcceptFriend.Execute(friend);
                    viewModel.FriendRequests.Remove(friend);
                    viewModel.ConfirmedFriends.Add(friend);
                }
            }
        }

        private void rejectFriendRequestsButton_Click(object sender, EventArgs e)
        {
            foreach (FriendViewModel friend in friendRequestsList.SelectedItems)
            {
                if (App.ViewModel.DeleteFriend.CanExecute(friend))
                {
                    App.ViewModel.DeleteFriend.Execute(friend);
                }
            }
        }

        private void selectFriendsButton_Click(object sender, EventArgs e)
        {
            friendsList.IsSelectionEnabled = true;
        }

        private void deFriendButton_Click(object sender, EventArgs e)
        {
            foreach (FriendViewModel friend in friendsList.SelectedItems)
            {
                if (App.ViewModel.DeleteFriend.CanExecute(friend))
                {
                    App.ViewModel.DeleteFriend.Execute(friend);
                }
            }
        }
    }
}