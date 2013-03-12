using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Devices;
using Microsoft.Phone.Controls;
using ZXing;

namespace ClubcardManager.Views
{
    /// <summary>
    /// Description for BarcodeScanningView.
    /// </summary>
    public partial class BarcodeScanningView : PhoneApplicationPage
    {
        private readonly BackgroundWorker scannerWorker;

        private DispatcherTimer timer;
        private DispatcherTimer focusTimer;
        private PhotoCameraLuminanceSource luminance;
        private IBarcodeReader reader;
        private PhotoCamera photoCamera;
        private readonly WriteableBitmap dummyBitmap = new WriteableBitmap(1, 1);

        private bool barcodeFound;
        /// <summary>
        /// Initializes a new instance of the BarcodeScanningView class.
        /// </summary>
        public BarcodeScanningView()
        {
            InitializeComponent();

            scannerWorker = new BackgroundWorker();
            scannerWorker.DoWork += scannerWorker_DoWork;
            scannerWorker.RunWorkerCompleted += scannerWorker_RunWorkerCompleted;

            Loaded += (sender, args) =>
                          {
                              if (photoCamera == null)
                              {
                                  photoCamera = new PhotoCamera();
                                  photoCamera.Initialized += OnPhotoCameraInitialized;
                                  previewVideo.SetSource(photoCamera);

                                  CameraButtons.ShutterKeyHalfPressed += (o, arg) => FocusTheCamera();
                              }

                              if (timer == null)
                              {
                                  timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                                  timer.Tick += (o, arg) => ScanPreviewBuffer();
                              }

                              if (focusTimer == null)
                              {
                                  focusTimer = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(4500)};
                                  focusTimer.Tick += (o, eventArgs) => FocusTheCamera();
                              }

                              timer.Start();
                          };
        }

        private void FocusTheCamera()
        {
            if (photoCamera.IsFocusAtPointSupported)
            {
                try
                {
                    photoCamera.FocusAtPoint(0.5, 0.5);
                }
                catch
                {
                }
            }
            else
            {
                if (photoCamera.IsFocusSupported)
                {
                    try
                    {
                        photoCamera.Focus();
                    }
                    catch
                    {
                    }
                }
            }
        }

        private void ScanPreviewBuffer()
        {
            if (luminance == null)
                return;

            photoCamera.GetPreviewBufferY(luminance.PreviewBufferY);
            // use a dummy writeable bitmap because the luminance values are written directly to the luminance buffer
            var result = reader.Decode(dummyBitmap);
            if (result == null) return; // if no barcode is found, don't do anything
            Dispatcher.BeginInvoke(() =>
                                       {
                                           // if a barcode has already been found, we don't want any more going through this time
                                           if (!barcodeFound) 
                                           {
                                               DisplayResult(result);
                                               barcodeFound = true;
                                           }
                                       });
        }

        private void DisplayResult(Result result)
        {
            if (result != null)
            {
                //previewVideo.SetSource(new MediaElement());
                Messenger.Default.Send(new NotificationMessage(result, "ResultFoundMsg"));
            }
        }

        private void OnPhotoCameraInitialized(object sender, CameraOperationCompletedEventArgs e)
        {
            var width = Convert.ToInt32(photoCamera.PreviewResolution.Width);
            var height = Convert.ToInt32(photoCamera.PreviewResolution.Height);

            photoCamera.FlashMode = FlashMode.Off;

            Dispatcher.BeginInvoke(() =>
            {
                previewTransform.Rotation = photoCamera.Orientation;
                // create a luminance source which gets its values directly from the camera
                // the instance is returned directly to the reader
                luminance = new PhotoCameraLuminanceSource(width, height);
                reader = new BarcodeReader(null, bmp => luminance, null);

                //if(focusTimer!= null) focusTimer.Start();
            });
        }

        void scannerWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // processing the result of the background scanning
            if (e.Cancelled)
            {
            }
            else if (e.Error != null)
            {
            }
            else
            {
                var result = (Result)e.Result;
                DisplayResult(result);
            }
        }

        static void scannerWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            // scanning for a barcode
            e.Result = new BarcodeReader().Decode((WriteableBitmap)e.Argument);
        }

        private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (NavigationService.CanGoBack && e.NavigationMode == NavigationMode.New)
                NavigationService.RemoveBackEntry();
        }
    }
}