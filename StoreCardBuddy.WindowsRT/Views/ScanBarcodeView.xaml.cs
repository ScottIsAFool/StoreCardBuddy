// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

using System;
using GalaSoft.MvvmLight.Messaging;
using StoreCardBuddy.ViewModel;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using ZXing;

namespace StoreCardBuddy.WindowsRT.Views
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class ScanBarcodeView : StoreCardBuddy.WindowsRT.Common.LayoutAwarePage
    {
        private readonly MediaCapture _mediaCapture = new MediaCapture();
        private Result _result;
        private bool _barcodeFound;

        public ScanBarcodeView()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                var cameras = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
                if (cameras.Count < 1)
                {
                    await DecodeStaticResource();
                    return;
                }
                MediaCaptureInitializationSettings settings;
                settings = cameras.Count == 1 ? new MediaCaptureInitializationSettings { VideoDeviceId = cameras[0].Id } : new MediaCaptureInitializationSettings { VideoDeviceId = cameras[1].Id };

                await _mediaCapture.InitializeAsync(settings);
                VideoCapture.Source = _mediaCapture;
                await _mediaCapture.StartPreviewAsync();

                while (_result == null)
                {
                    var photoStorageFile = await KnownFolders.PicturesLibrary.CreateFileAsync("scan.jpg", CreationCollisionOption.GenerateUniqueName);
                    await _mediaCapture.CapturePhotoToStorageFileAsync(ImageEncodingProperties.CreateJpeg(), photoStorageFile);

                    var stream = await photoStorageFile.OpenReadAsync();
                    // initialize with 1,1 to get the current size of the image
                    var writeableBmp = new WriteableBitmap(1, 1);
                    writeableBmp.SetSource(stream);
                    // and create it again because otherwise the WB isn't fully initialized and decoding
                    // results in a IndexOutOfRange
                    writeableBmp = new WriteableBitmap(writeableBmp.PixelWidth, writeableBmp.PixelHeight);
                    stream.Seek(0);
                    writeableBmp.SetSource(stream);

                    _result = ScanBitmap(writeableBmp);

                    if (_result != null)
                    {
                        if (!_barcodeFound)
                        {
                            Messenger.Default.Send(new NotificationMessage(_result, "ResultFoundMsg"));
                            _barcodeFound = true;
                        }
                    }

                    await photoStorageFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }

                await _mediaCapture.StopPreviewAsync();
            }
            catch (Exception ex)
            {
                var s = "";
            }
        }

        private async System.Threading.Tasks.Task DecodeStaticResource()
        {
            var file = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync(@"Assets\1.jpg");
            var stream = await file.OpenReadAsync();
            // initialize with 1,1 to get the current size of the image
            var writeableBmp = new WriteableBitmap(1, 1);
            writeableBmp.SetSource(stream);
            // and create it again because otherwise the WB isn't fully initialized and decoding
            // results in a IndexOutOfRange
            writeableBmp = new WriteableBitmap(writeableBmp.PixelWidth, writeableBmp.PixelHeight);
            stream.Seek(0);
            writeableBmp.SetSource(stream);

            _result = ScanBitmap(writeableBmp);
            if (_result != null)
            {
                if (!_barcodeFound)
                {
                    Messenger.Default.Send(new NotificationMessage(_result, "ResultFoundMsg"));
                    _barcodeFound = true;
                }
            }
            return;
        }

        private Result ScanBitmap(WriteableBitmap writeableBmp)
        {
            var barcodeReader = new BarcodeReader
            {
                TryHarder = true,
                AutoRotate = true
            };
            var result = barcodeReader.Decode(writeableBmp);

            if (result != null)
            {

            }

            return result;
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            await _mediaCapture.StopPreviewAsync();

            base.OnNavigatingFrom(e);
        }
    }
}
