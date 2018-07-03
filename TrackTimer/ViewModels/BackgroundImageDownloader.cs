namespace TrackTimer.ViewModels
{
    using System;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using BugSense;
    using Microsoft.Phone.Controls;
    using TrackTimer.Resources;
    using Windows.Storage;

    public class BackgroundImageDownloader
    {
        private const double backgroundOpacity = 0.5;

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.RegisterAttached("Source", typeof(string), typeof(BitmapImage), new PropertyMetadata(null, callback));

        public static void SetSource(DependencyObject element, string value)
        {
            element.SetValue(SourceProperty, value);
        }

        public static string GetSource(DependencyObject element)
        {
            return (string)element.GetValue(SourceProperty);
        }

        private static async void callback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Panorama panorama = d as Panorama;

            if (panorama != null)
            {
                var path = e.NewValue as string;
                {
                    if (!string.IsNullOrEmpty(path))
                    {
                        if (path.StartsWith("isostore:/"))
                        {
                            string localPath = path.Substring(10);
                            var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(AppConstants.LOCALFOLDERNAME_BACKGROUND, CreationCollisionOption.OpenIfExists);
                            var file = await folder.GetFileAsync(localPath);
                            panorama.Background = await LoadFileIntoImageBrush(file);
                        }
                        else
                        {
                            var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(AppConstants.LOCALFOLDERNAME_BACKGROUND, CreationCollisionOption.OpenIfExists);
                            string localPath = GetFileNameFromPath(path);
                            StorageFile file = null;
                            try
                            {
                                file = await folder.GetFileAsync(localPath);
                            }
                            catch (FileNotFoundException)
                            { }
                            ImageBrush brush;
                            Exception imageLoadException = null;
                            try
                            {
                                if (file == null)
                                {
                                    BitmapImage image = new BitmapImage(new Uri(path, UriKind.Absolute));
                                    image.ImageOpened += image_ImageOpened;
                                    brush = new ImageBrush
                                    {
                                        Opacity = backgroundOpacity,
                                        Stretch = Stretch.UniformToFill,
                                        ImageSource = image
                                    };
                                }
                                else
                                {
                                    brush = await LoadFileIntoImageBrush(file);
                                }
                                panorama.Background = brush;
                            }
                            catch (Exception ex)
                            {
                                imageLoadException = ex;
                            }

                            if (imageLoadException != null)
                            {
                                await BugSenseHandler.Instance.LogExceptionAsync(imageLoadException, "Message", string.Format("Failed to load image: {0}", path));
                            }
                        }
                    }
                    else
                    {
                        var parentGrid = panorama.Parent as Grid;
                        if (parentGrid == null) return;
                        var parentPage = parentGrid.Parent as PhoneApplicationPage;
                        if (parentPage != null)
                            panorama.Background = parentPage.Resources["PhoneBackgroundBrush"] as Brush;
                    }
                }
            }
        }

        private static string GetFileNameFromPath(string path)
        {
            int lastSlashIndex = path.LastIndexOf('/');
            return HttpUtility.UrlDecode(path.Substring(lastSlashIndex + 1, path.Length - lastSlashIndex - 1));
        }

        private static async void image_ImageOpened(object sender, RoutedEventArgs e)
        {
            var image = sender as BitmapImage;
            if (image == null) return;
            string fileName = GetFileNameFromPath(image.UriSource.AbsolutePath);
            var writableBitmap = new WriteableBitmap(image);
            var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(AppConstants.LOCALFOLDERNAME_BACKGROUND, CreationCollisionOption.OpenIfExists);
            var file = await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            using (var stream = await file.OpenStreamForWriteAsync())
                Extensions.SaveJpeg(writableBitmap, stream, image.PixelWidth, image.PixelHeight, 0, 100);
        }

        private static async Task<ImageBrush> LoadFileIntoImageBrush(StorageFile file)
        {
            // TODO - Investigate 'Access Denied' exception
            using (var stream = await file.OpenStreamForWriteAsync())
            {
                BitmapImage image = new BitmapImage();
                image.SetSource(stream);
                return new ImageBrush
                {
                    Opacity = backgroundOpacity,
                    Stretch = Stretch.UniformToFill,
                    ImageSource = image
                };
            }
        }
    }
}