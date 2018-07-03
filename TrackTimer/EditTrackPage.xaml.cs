namespace TrackTimer
{
    using System;
    using System.Device.Location;
    using System.IO;
    using System.IO.IsolatedStorage;
    using System.Linq;
    using System.Spatial;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Navigation;
    using Microsoft.Phone.Controls;
    using Microsoft.Phone.Maps.Controls;
    using Microsoft.Phone.Tasks;
    using TrackTimer.Core.Extensions;
    using TrackTimer.Core.Resources;
    using TrackTimer.Core.ViewModels;
    using TrackTimer.Extensions;
    using TrackTimer.Resources;
    using Windows.Storage;

    public partial class EditTrackPage : PhoneApplicationPage
    {
        private int lastSectorNumber;
        private PhotoChooserTask photoChooser;

        public EditTrackPage()
        {
            InitializeComponent();
            photoChooser = new PhotoChooserTask();
            photoChooser.Completed += photoChooser_Completed;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            DataContext = App.ViewModel;
            sectorSelector.SelectedItem = null;
            sectorSelector.SelectionChanged += sectorSelector_SelectionChanged;
            lastSectorNumber = App.ViewModel.Track.Sectors.Any() ? App.ViewModel.Track.Sectors.Max(s => s.SectorNumber) : 0;
            mapCircuit.Hold += mapCircuit_Hold;
            mapCircuit.Tap += mapCircuit_Tap;
            mapCircuit.Center = new GeoCoordinate(App.ViewModel.Track.Latitude, App.ViewModel.Track.Longitude);
            mapCircuit.SetZoomLevelForTrack(App.ViewModel.Track.LengthInMetres());
            DrawSectorLinesOnMap();
            base.OnNavigatedTo(e);
        }

        void sectorSelector_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (!App.ViewModel.Track.IsLocal) return;
            App.ViewModel.Track.SelectedSector = sectorSelector.SelectedItem as TrackSectorViewModel;
            if (App.ViewModel.Track.SelectedSector != null)
                NavigationService.Navigate(new Uri("/EditSectorPage.xaml", System.UriKind.Relative));
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            mapCircuit.Hold -= mapCircuit_Hold;
            mapCircuit.Tap -= mapCircuit_Tap;
            sectorSelector.SelectionChanged -= sectorSelector_SelectionChanged;
            base.OnNavigatedFrom(e);
        }

        private void DrawSectorLinesOnMap()
        {
            mapCircuit.MapElements.Clear();
            foreach (var sector in App.ViewModel.Track.Sectors)
            {
                var sectorLine = new MapPolyline();
                sectorLine.StrokeColor = sector.IsFinishLine ? Colors.Orange : Colors.Black;
                sectorLine.Path.Add(new GeoCoordinate(sector.StartLatitude, sector.StartLongitude));
                sectorLine.Path.Add(new GeoCoordinate(sector.EndLatitude, sector.EndLongitude));
                mapCircuit.MapElements.Add(sectorLine);
            }
        }

        private void mapCircuit_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!App.ViewModel.Track.IsLocal) return;
            double smallestDistance = double.MaxValue;
            TrackSectorViewModel selectedSector = null;
            var mapPosition = e.GetPosition(mapCircuit);
            var geoCoordinate = mapCircuit.ConvertViewportPointToGeoCoordinate(mapPosition);
            var goegraphyPoint = GeographyPoint.Create(geoCoordinate.Latitude, geoCoordinate.Longitude, null, geoCoordinate.Course);
            foreach (var sector in App.ViewModel.Track.Sectors)
            {
                if (double.IsNaN(sector.StartLatitude)
                    || double.IsNaN(sector.StartLongitude)
                    || double.IsNaN(sector.EndLatitude)
                    || double.IsNaN(sector.EndLongitude))
                    continue;

                var sectorLineStartPoint = GeographyPoint.Create(sector.StartLatitude, sector.StartLongitude);
                var sectorLineEndPoint = GeographyPoint.Create(sector.EndLatitude, sector.EndLongitude);
                var midpoint = GeoUtmConverter.Midpoint(sectorLineStartPoint, sectorLineEndPoint, sector.Heading);
                double distance = midpoint.Distance(goegraphyPoint);
                if (distance < 0.5 && distance < smallestDistance)
                {
                    smallestDistance = distance;
                    selectedSector = sector;
                }
            }
            if (selectedSector == null) return;
            App.ViewModel.Track.SelectedSector = selectedSector;
            NavigationService.Navigate(new Uri("/EditSectorPage.xaml", System.UriKind.Relative));
        }

        private void mapCircuit_Hold(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!App.ViewModel.Track.IsLocal) return;
            var mapPosition = e.GetPosition(mapCircuit);
            var mapLayer = new MapLayer();
            
            var geoCoordinate = mapCircuit.ConvertViewportPointToGeoCoordinate(mapPosition);
            lastSectorNumber++;
            double heading = (double)App.ViewModel.DegreesData.SelectedItem;
            var geographyPoint = GeographyPoint.Create(geoCoordinate.Latitude, geoCoordinate.Longitude, null, heading);
            var lineCoordinates = geographyPoint.ConvertToLine(0.02);
            var newSector = new TrackSectorViewModel
            {
                StartLatitude = lineCoordinates[0].Latitude,
                StartLongitude = lineCoordinates[0].Longitude,
                EndLatitude = lineCoordinates[0].Latitude,
                EndLongitude = lineCoordinates[0].Longitude,
                Heading = heading,
                IsFinishLine = false,
                SectorNumber = lastSectorNumber
            };
            App.ViewModel.Track.Sectors.Add(newSector);
            App.ViewModel.Track.SelectedSector = newSector;
            NavigationService.Navigate(new Uri("/EditSectorPage.xaml", System.UriKind.Relative));
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            photoChooser.Show();
        }

        private async void photoChooser_Completed(object sender, PhotoResult e)
        {
            if (e.TaskResult != TaskResult.OK) return;
            int index = e.OriginalFileName.LastIndexOf('\\');
            App.ViewModel.Track.BackgroundImagePath = await SaveToIsolatedStorage(e.ChosenPhoto, e.OriginalFileName.Substring(index).Trim('\\'));
        }

        private void Map_Loaded(object sender, RoutedEventArgs e)
        {
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.ApplicationId = Constants.MICROSOFT_BINGMAPS_CLIENTID;
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.AuthenticationToken = Constants.MICROSOFT_BINGMAPS_AUTHENTICATIONTOKEN;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!App.ViewModel.Track.IsLocal) return;
            var menuItem = sender as MenuItem;
            if (menuItem == null) return;
            var sector = menuItem.CommandParameter as TrackSectorViewModel;
            if (sector == null) return;
            App.ViewModel.Track.Sectors.Remove(sector);
            DrawSectorLinesOnMap();
        }

        private async Task<string> SaveToIsolatedStorage(Stream imageStream, string fileName)
        {
            var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(AppConstants.LOCALFOLDERNAME_BACKGROUND, CreationCollisionOption.OpenIfExists);
            var file = await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            using (var stream = await file.OpenStreamForWriteAsync())
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.SetSource(imageStream);

                WriteableBitmap wb = new WriteableBitmap(bitmap);
                wb.SaveJpeg(stream, wb.PixelWidth, wb.PixelHeight, 0, 85);
                stream.Close();
                return (string.Format("isostore:/{0}", fileName));
            }
        }

    }
}