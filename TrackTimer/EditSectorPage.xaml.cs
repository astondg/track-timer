namespace TrackTimer
{
    using System.Spatial;
    using System.Windows.Navigation;
    using Microsoft.Phone.Controls;
    using TrackTimer.Core.ViewModels;
    using TrackTimer.Core.Extensions;

    public partial class EditSectorPage : PhoneApplicationPage
    {
        public EditSectorPage()
        {
            InitializeComponent();
            degreesPicker.PickerPageUri = new System.Uri("/DegreesPickerPage.xaml", System.UriKind.Relative);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            DataContext = App.ViewModel.Track.SelectedSector;
            App.ViewModel.Track.SelectedSector.PropertyChanged += SelectedSector_PropertyChanged;
            base.OnNavigatedTo(e);
        }

        private void SelectedSector_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "Heading") return;
            var selectedSector = sender as TrackSectorViewModel;
            if (selectedSector == null) return;

            var geographyPoint = GeographyPoint.Create(selectedSector.StartLatitude, selectedSector.StartLongitude, null, selectedSector.Heading);
            var lineCoordinates = geographyPoint.ConvertToLine(0.02);
            selectedSector.StartLatitude = lineCoordinates[0].Latitude;
            selectedSector.StartLongitude = lineCoordinates[0].Longitude;
            selectedSector.EndLatitude = lineCoordinates[1].Latitude;
            selectedSector.EndLongitude = lineCoordinates[1].Longitude;
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var selectedSector = App.ViewModel.Track.SelectedSector;
            GeographyPoint geographyPoint;
            if (double.IsNaN(selectedSector.EndLatitude) && double.IsNaN(selectedSector.EndLongitude))
            {
                geographyPoint = GeographyPoint.Create(selectedSector.StartLatitude, selectedSector.StartLongitude, null, 0);
            }
            else
            {
                var startPoint = GeographyPoint.Create(selectedSector.StartLatitude, selectedSector.StartLongitude, null, 0);
                var endPoint = GeographyPoint.Create(selectedSector.EndLatitude, selectedSector.EndLongitude, null, 0);
                geographyPoint = GeoUtmConverter.Midpoint(startPoint, endPoint, selectedSector.Heading);
            }

            var lineCoordinates = geographyPoint.ConvertToLine(0.02);
            selectedSector.StartLatitude = lineCoordinates[0].Latitude;
            selectedSector.StartLongitude = lineCoordinates[0].Longitude;
            selectedSector.EndLatitude = lineCoordinates[1].Latitude;
            selectedSector.EndLongitude = lineCoordinates[1].Longitude;
        }
    }
}