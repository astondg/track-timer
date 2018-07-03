using System;
using System.ComponentModel;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using NdefLibrary.Ndef;
using TrackTimer.Core.Models;
using TrackTimer.ViewModels;
using Windows.Networking.Proximity;

namespace TrackTimer
{
    public partial class AddFriendPage : PhoneApplicationPage
    {
        private const string VisibilityGroupName = "VisibilityStates";
        private const string OpenVisibilityStateName = "Open";
        private const string ClosedVisibilityStateName = "Closed";

        private Storyboard _closedStoryboard;
        private ProgressIndicator progressIndicator;
        private ProximityDevice proximityDevice;

        public AddFriendPage()
        {
            InitializeComponent();
            DataContext = new AddFriendViewModel(App.ViewModel.Friends);
            ((AddFriendViewModel)DataContext).FriendAdded += AddFriendPage_FriendAdded;
            progressIndicator = new ProgressIndicator();
            SystemTray.SetProgressIndicator(this, progressIndicator);
            
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

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            PeerFinder.TriggeredConnectionStateChanged += PeerFinder_TriggeredConnectionStateChanged;
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
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
        }

        private void AddFriendPage_FriendAdded(AddFriendViewModel sender, Core.ViewModels.FriendViewModel args)
        {
            progressIndicator.IsVisible = false;
            ClosePickerPage();
        }

        private void StackPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            progressIndicator.IsVisible = true;
            var viewModel = ((AddFriendViewModel)DataContext);
            var user = ((StackPanel)sender).DataContext as User;
            if (user == null) return;
            if (viewModel.AddFriend.CanExecute(user))
                viewModel.AddFriend.Execute(user);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            // Update the binding source
            BindingExpression bindingExpr = textBox.GetBindingExpression(TextBox.TextProperty);
            bindingExpr.UpdateSource();
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
    }
}