namespace TrackTimer
{
    using System;
    using System.Collections.Generic;
    using System.Device.Location;
    using System.Globalization;
    using System.Linq;
    using System.Spatial;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Navigation;
    using Microsoft.Phone.Controls;
    using Microsoft.Phone.Maps.Controls;
    using Microsoft.Phone.Maps.Toolkit;
    using Microsoft.Phone.Tasks;
    using TrackTimer.Core.Extensions;
    using TrackTimer.Core.Resources;
    using TrackTimer.Core.ViewModels;
    using TrackTimer.Extensions;
    using TrackTimer.Resources;
    using TrackTimer.ViewModels;

    public partial class LapDetailPage : PhoneApplicationPage
    {
        private IList<Tuple<GeoCoordinate, string>> apexDataLayer;

        public LapDetailPage()
        {
            InitializeComponent();
            apexDataLayer = new List<Tuple<GeoCoordinate, string>>();
            Visibility darkBackgroundVisibility = (Visibility)Application.Current.Resources["PhoneDarkThemeVisibility"];
            if (darkBackgroundVisibility == System.Windows.Visibility.Visible)
                mapCircuit.ColorMode = MapColorMode.Dark;
            else
                mapCircuit.ColorMode = MapColorMode.Light;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            string navigationParameter;
            if (!NavigationContext.QueryString.TryGetValue(AppConstants.NAVIGATIONPARAMETER_TYPE, out navigationParameter))
                navigationParameter = null;

            if (App.ViewModel.Settings.IsTrial)
            {
                adRotatorControl.Visibility = System.Windows.Visibility.Visible;
                adDefaultControl.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                adRotatorControl.Visibility = System.Windows.Visibility.Collapsed;
                adDefaultControl.Visibility = System.Windows.Visibility.Collapsed;
            }

            var lapViewModel = App.ViewModel.Track.History.SelectedSession.SelectedLap;
            string lapMinimumLengthBlurb = string.Format("* {0} {1} {2}",
                                                         AppResources.Text_Blurb_OfficialLength,
                                                         Math.Round(App.ViewModel.Track.Length * Constants.APP_MINIMUMLAPLENGTH_FACTOR, 3),
                                                         App.ViewModel.Settings.IsMetricUnits
                                                         ? AppResources.Text_Unit_Kilometres
                                                         : AppResources.Text_Unit_Miles);

            var lapDetailViewModel = new LapDetailViewModel
            {
                Lap = lapViewModel,
                SpeedUnitText = App.ViewModel.SpeedUnitText,
                LengthUnitText = App.ViewModel.LengthUnitText,
                ProjectedLength = lapViewModel.ProjectedLength(),
                SessionWeather = App.ViewModel.Track.History.SelectedSession.Weather,
                GpsDeviceName = App.ViewModel.Track.History.SelectedSession.GpsDeviceName,
                LapMinimumLengthBlurb = lapMinimumLengthBlurb,
                IsOfficial = null,
                LapBelongsToCurrentUser = true,
                IsLeaderboardTime = string.Equals(navigationParameter, AppConstants.NAVIGATIONPARAMETER_VALUE_LEADERBOARD)
            };

            var bestLapViewModel = lapViewModel as BestLapViewModel;
            if (bestLapViewModel != null)
            {
                lapDetailViewModel.UserDisplayName = bestLapViewModel.UserDisplayName;
                lapDetailViewModel.GpsDeviceName = bestLapViewModel.GpsDeviceName;
                lapDetailViewModel.IsOfficial = !bestLapViewModel.IsUnofficial;
                lapDetailViewModel.LapBelongsToCurrentUser = App.ViewModel.IsAuthenticated && bestLapViewModel.UserName == App.MobileService.CurrentUser.UserId;
            }

            DataContext = lapDetailViewModel;
            var sectorSplitsAhead = new Dictionary<int, bool>();
            foreach (var sector in App.ViewModel.Track.Sectors)
            {
                var sectorSplitDataPoint = lapDetailViewModel.Lap.DataPoints.Where(dp => dp.Latitude.HasValue && dp.Longitude.HasValue).SkipWhile(dp => dp.SectorNumber != sector.SectorNumber).TakeWhile(dp => dp.SectorNumber == sector.SectorNumber).LastOrDefault();
                if (!sector.IsFinishLine && sectorSplitDataPoint == null)
                {
                    sectorSplitsAhead.Add(sector.SectorNumber, false);
                    continue;
                }

                var sectorSplit = sector.IsFinishLine
                                    ? lapDetailViewModel.Lap.LapTime
                                    : sectorSplitDataPoint.ElapsedTime - lapDetailViewModel.Lap.StartElapsedTime;

                TimeSpan bestSplit;
                if (sector.IsFinishLine)
                {
                    bestSplit = App.ViewModel.Track.BestLap != null && App.ViewModel.Track.BestLap.DataPoints != null
                                ? App.ViewModel.Track.BestLap.LapTime
                                : sectorSplit;
                }
                else
                {
                    var bestSplitDataPoint = App.ViewModel.Track.BestLap != null && App.ViewModel.Track.BestLap.DataPoints != null
                                                ? App.ViewModel.Track.BestLap.DataPoints.Where(dp => dp.Latitude.HasValue && dp.Longitude.HasValue).SkipWhile(dp => dp.SectorNumber != sector.SectorNumber).TakeWhile(dp => dp.SectorNumber == sector.SectorNumber).LastOrDefault()
                                                : null;
                    bestSplit = bestSplitDataPoint != null
                                ? bestSplitDataPoint.ElapsedTime - App.ViewModel.Track.BestLap.StartElapsedTime
                                : sectorSplit;
                }

                var splitStatus = sectorSplit - bestSplit;
                lapDetailViewModel.SectorSplits.Add(Tuple.Create(sector.SectorNumber,
                                                                 sectorSplit,
                                                                 splitStatus));
                sectorSplitsAhead.Add(sector.SectorNumber, splitStatus <= TimeSpan.Zero);
            }

            mapCircuit.Center = new GeoCoordinate(App.ViewModel.Track.Latitude, App.ViewModel.Track.Longitude);
            mapCircuit.SetZoomLevelForTrack(App.ViewModel.Track.LengthInMetres());

            TrackSectorViewModel finishLineSector = null;
            foreach (var sector in App.ViewModel.Track.Sectors)
            {
                if (sector.IsFinishLine)
                    finishLineSector = sector;

                var sectorLine = new MapPolyline();
                sectorLine.StrokeColor = sector.IsFinishLine ? Colors.Orange : Colors.Black;
                sectorLine.Path.Add(new GeoCoordinate(sector.StartLatitude, sector.StartLongitude));
                sectorLine.Path.Add(new GeoCoordinate(sector.EndLatitude, sector.EndLongitude));
                mapCircuit.MapElements.Add(sectorLine);
            }

            LapDataPointViewModel previousGeographicDataPoint = App.ViewModel.Track.History.SelectedSession.SelectedLap.DataPoints.FirstOrDefault(dp => dp.Latitude != null && dp.Longitude != null);
            if (previousGeographicDataPoint == null) return;

            var finishLineMidPoint = finishLineSector == null
                                        ? null
                                        : GeoUtmConverter.Midpoint(GeographyPoint.Create(finishLineSector.StartLatitude, finishLineSector.StartLongitude),
                                                                   GeographyPoint.Create(finishLineSector.EndLatitude, finishLineSector.EndLongitude),
                                                                   finishLineSector.Heading);

            if (finishLineMidPoint != null && App.ViewModel.Track.History.SelectedSession.SelectedLap.IsComplete)
                AddCoordinatesAsPathLineToMap(previousGeographicDataPoint.SectorNumber, sectorSplitsAhead, finishLineMidPoint, previousGeographicDataPoint.AsGeographyPoint());

            LapDataPointViewModel previousAccelerometerDataPoint = null;
            LapDataPointViewModel maxSpeedDataPoint = null;
            double? minSpeed = null;
            var decelerationSpeeds = new List<Tuple<double?, TimeSpan>>();
            var mapLineAheadColor = new SolidColorBrush(System.Windows.Media.Colors.Green);
            var mapLineBehindColor = new SolidColorBrush(System.Windows.Media.Colors.Red);
            foreach (var dataPoint in App.ViewModel.Track.History.SelectedSession.SelectedLap.DataPoints)
            {
                if (!dataPoint.Latitude.HasValue || !dataPoint.Longitude.HasValue)
                {
                    if (dataPoint.AccelerationX.HasValue || dataPoint.AccelerationY.HasValue || dataPoint.AccelerationZ.HasValue)
                        previousAccelerometerDataPoint = dataPoint;
                    continue;
                }

                if (maxSpeedDataPoint == null || dataPoint.Speed > maxSpeedDataPoint.Speed)
                    maxSpeedDataPoint = dataPoint;

                if (!minSpeed.HasValue || minSpeed < dataPoint.Speed)
                {
                    if (decelerationSpeeds.Count > 1 && decelerationSpeeds.Sum(ds => ds.Item2.TotalSeconds) > 0.5)
                    {
                        // Create a pushpin for maximum speed before braking
                        if (maxSpeedDataPoint != null)
                        {
                            string pushpinContent = string.Format("{0}{1}",
                                                                    Math.Round(maxSpeedDataPoint.Speed.Value).ToString(CultureInfo.CurrentCulture),
                                                                    App.ViewModel.Settings.IsMetricUnits
                                                                        ? AppResources.Text_Unit_MetricSpeed
                                                                        : AppResources.Text_Unit_ImperialSpeed);
                            var pushpinData = Tuple.Create(new GeoCoordinate(maxSpeedDataPoint.Latitude.Value, maxSpeedDataPoint.Longitude.Value), pushpinContent);
                            apexDataLayer.Add(pushpinData);

                            maxSpeedDataPoint = null;
                        }

                        double decelerationTime = decelerationSpeeds.Sum(ds => ds.Item2.TotalSeconds);
                        double? totalSpeedLost = decelerationSpeeds.First().Item1 - decelerationSpeeds.Last().Item1;
                        if (totalSpeedLost.HasValue && (totalSpeedLost.Value / decelerationTime) > 3)
                        {
                            // Create a pushpin for minimum speed through corner and the nearest g-force reading (if available)
                            string pushpinContent;
                            if (previousAccelerometerDataPoint != null)
                            {
                                pushpinContent = string.Format("{0}{1} | {2}{3}",
                                                                Math.Round(minSpeed.Value).ToString(CultureInfo.CurrentCulture),
                                                                App.ViewModel.Settings.IsMetricUnits
                                                                    ? AppResources.Text_Unit_MetricSpeed
                                                                    : AppResources.Text_Unit_ImperialSpeed,
                                                                ComputeRelativeGforce(previousAccelerometerDataPoint, App.ViewModel.Track.History.SelectedSession.DeviceOrientation),
                                                                AppResources.Text_Unit_GForce);
                            }
                            else
                            {
                                pushpinContent = string.Format("{0}{1}",
                                                                Math.Round(minSpeed.Value).ToString(CultureInfo.CurrentCulture),
                                                                App.ViewModel.Settings.IsMetricUnits
                                                                    ? AppResources.Text_Unit_MetricSpeed
                                                                    : AppResources.Text_Unit_ImperialSpeed);
                            }
                            var pushpinData = Tuple.Create(new GeoCoordinate(previousGeographicDataPoint.Latitude.Value, previousGeographicDataPoint.Longitude.Value), pushpinContent);
                            apexDataLayer.Add(pushpinData);
                        }
                    }
                    decelerationSpeeds.Clear();
                    minSpeed = dataPoint.Speed;
                }
                else
                {
                    minSpeed = dataPoint.Speed;
                    decelerationSpeeds.Add(Tuple.Create(minSpeed, dataPoint.ElapsedTime - previousGeographicDataPoint.ElapsedTime));
                }
                AddCoordinatesAsPathLineToMap(dataPoint.SectorNumber, sectorSplitsAhead, previousGeographicDataPoint.AsGeographyPoint(), dataPoint.AsGeographyPoint());

                previousGeographicDataPoint = dataPoint;
            }

            if (finishLineMidPoint != null && App.ViewModel.Track.History.SelectedSession.SelectedLap.IsComplete)
                AddCoordinatesAsPathLineToMap(previousGeographicDataPoint.SectorNumber, sectorSplitsAhead, previousGeographicDataPoint.AsGeographyPoint(), finishLineMidPoint);

            base.OnNavigatedTo(e);
        }

        private void AddCoordinatesAsPathLineToMap(int sectorNumber, Dictionary<int, bool> sectorSplitsAhead, GeographyPoint startPoint, GeographyPoint endPoint)
        {
            var finalMapLine = new MapPolyline { StrokeColor = sectorSplitsAhead[sectorNumber] ? Colors.Green : Colors.Red };
            finalMapLine.Path.Add(new GeoCoordinate(startPoint.Latitude, startPoint.Longitude));
            finalMapLine.Path.Add(new GeoCoordinate(endPoint.Latitude, endPoint.Longitude));
            mapCircuit.MapElements.Add(finalMapLine);
        }

        private void Map_Loaded(object sender, RoutedEventArgs e)
        {
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.ApplicationId = Constants.MICROSOFT_BINGMAPS_CLIENTID;
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.AuthenticationToken = Constants.MICROSOFT_BINGMAPS_AUTHENTICATIONTOKEN;
        }

        private void ToggleSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            var mapChildren = Microsoft.Phone.Maps.Toolkit.MapExtensions.GetChildren(mapCircuit);
            mapChildren.Clear();
        }

        private void ToggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            var mapChildren = Microsoft.Phone.Maps.Toolkit.MapExtensions.GetChildren(mapCircuit);
            foreach (var pushpinContent in apexDataLayer)
            {
                var pushpin = new Pushpin { GeoCoordinate = pushpinContent.Item1, Content = pushpinContent.Item2 };
                Microsoft.Phone.Maps.Toolkit.MapExtensions.Add(mapChildren, pushpin, pushpinContent.Item1);
            }
        }

        private string ComputeRelativeGforce(LapDataPointViewModel dataPoint, DeviceOrientation deviceOrientation)
        {
            if (deviceOrientation == DeviceOrientation.Landscape || deviceOrientation == DeviceOrientation.LandscapeLeft || deviceOrientation == DeviceOrientation.LandscapeRight)
            {
                return dataPoint.AccelerationY.HasValue
                        ? Math.Round(dataPoint.AccelerationY.Value, 2).ToString(CultureInfo.CurrentCulture)
                        : "?";
            }
            else
            {
                return dataPoint.AccelerationY.HasValue
                        ? Math.Round(dataPoint.AccelerationX.Value, 2).ToString(CultureInfo.CurrentCulture)
                        : "?";
            }
        }

        private void Image_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var marketplaceDetailTask = new MarketplaceDetailTask();
            marketplaceDetailTask.Show();
        }
    }
}