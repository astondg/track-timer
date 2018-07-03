namespace TrackTimer
{
    using System.Windows.Navigation;
    using Microsoft.Phone.Controls;

    public partial class PendingUploads : PhoneApplicationPage
    {
        public PendingUploads()
        {
            InitializeComponent();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (App.ViewModel.IsAuthenticated)
                await App.ViewModel.BackgroundTransfers.LoadData();
            this.DataContext = App.ViewModel.BackgroundTransfers;
            base.OnNavigatedTo(e);
        }

        private void StackPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {

        }
    }
}