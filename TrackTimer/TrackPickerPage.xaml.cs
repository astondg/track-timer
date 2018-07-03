using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using TrackTimer.Core.Models;

namespace TrackTimer
{
    public partial class TrackPickerPage : PhoneApplicationPage
    {
        private const string NAVIGATED_EXTERNAL = "TrackPickerPage_NavigatedExternal";
        private const string VisibilityGroupName = "VisibilityStates";
        private const string OpenVisibilityStateName = "Open";
        private const string ClosedVisibilityStateName = "Closed";

        private Storyboard _closedStoryboard;

        public TrackPickerPage()
        {
            InitializeComponent();

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

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            DataContext = App.ViewModel;
            llsTracks.SelectionChanged += LongListSelector_SelectionChanged;
            base.OnNavigatedTo(e);
            if (State.ContainsKey(NAVIGATED_EXTERNAL))
            {
                // Back out from picker page for consistency with behavior of core pickers in this scenario
                if (NavigationService.CanGoBack)
                    NavigationService.GoBack();
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            llsTracks.SelectionChanged -= LongListSelector_SelectionChanged;
            if ("app://external/" == e.Uri.ToString())
                State[NAVIGATED_EXTERNAL] = true;
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

        private void LongListSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count < 1) return;
            var newSelection = e.AddedItems[0] as Track;
            if (newSelection != null)
                App.ViewModel.CurrentTrackItem = e.AddedItems[0] as Track;
            ClosePickerPage();
        }
    }
}