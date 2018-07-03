namespace TrackTimer
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Navigation;
    using Microsoft.Phone.Controls;
    using TrackTimer.Core.ViewModels;
    using TrackTimer.Resources;

    public partial class AddVehiclePage : PhoneApplicationPage
    {
        public AddVehiclePage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.DataContext = App.ViewModel.Settings;
            if (App.ViewModel.Settings.NewVehicle.Key != Guid.Empty)
                btnAddEdit.Content = AppResources.Text_Button_SaveVehicle;
            else
                btnAddEdit.Content = AppResources.Text_Button_AddVehicle;
            App.ViewModel.Settings.PropertyChanged += Settings_PropertyChanged;
            App.ViewModel.Settings.NewVehicle.PropertyChanged += NewVehicle_PropertyChanged;
            base.OnNavigatedTo(e);
        }

        protected async override void OnNavigatedFrom(NavigationEventArgs e)
        {
            App.ViewModel.Settings.PropertyChanged -= Settings_PropertyChanged;
            App.ViewModel.Settings.NewVehicle.PropertyChanged -= NewVehicle_PropertyChanged;
            await App.ViewModel.Settings.LoadData(App.ViewModel.IsAuthenticated ? App.MobileService.CurrentUser.UserId : null, App.ViewModel.UserProfile);
            base.OnNavigatedFrom(e);
        }

        private void NewVehicle_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var newVehicle = sender as VehicleViewModel;
            if (newVehicle != null && newVehicle.HasErrors)
            {
                var errorMessage = new StringBuilder(AppResources.Text_Error_TheFollowingErrorsOccurred);

                var errors = newVehicle.GetErrors("Make").Cast<string>();
                errorMessage.AppendFormat("- {0}\n", errors.FirstOrDefault());

                errors = newVehicle.GetErrors("Model").Cast<string>();
                errorMessage.AppendFormat("- {0}\n", errors.FirstOrDefault());

                errors = newVehicle.GetErrors(string.Empty).Cast<string>();
                errorMessage.AppendFormat("- {0}\n", errors.FirstOrDefault());

                MessageBox.Show(errorMessage.ToString());
            }
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SavedVehicle" && NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }
    }
}