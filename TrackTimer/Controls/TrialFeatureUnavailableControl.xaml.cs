using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;

namespace TrackTimer.Controls
{
    public partial class TrialFeatureUnavailable : UserControl
    {
        public TrialFeatureUnavailable()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var marketplaceDetailTask = new MarketplaceDetailTask();
            marketplaceDetailTask.Show();
        }
    }
}
