using System;
using System.ComponentModel;
using System.Windows;
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
        private readonly BackgroundWorker _scannerWorker;

        private DispatcherTimer _timer;
        private DispatcherTimer _focusTimer;
        private PhotoCameraLuminanceSource _luminance;
        private IBarcodeReader _reader;
        private PhotoCamera _photoCamera;
        private readonly WriteableBitmap _dummyBitmap = new WriteableBitmap(1, 1);

        private bool _barcodeFound;
        /// <summary>
        /// Initializes a new instance of the BarcodeScanningView class.
        /// </summary>
        public BarcodeScanningView()
        {
            InitializeComponent();

            _scannerWorker = new BackgroundWorker();
            _scannerWorker.DoWork += scannerWorker_DoWork;
            _scannerWorker.RunWorkerCompleted += scannerWorker_RunWorkerCompleted;

            Loaded += (sender, args) =>
                          {
                              if (_photoCamera == null)
                              {
                                  _photoCamera = new PhotoCamera();
                                  _photoCamera.Initialized += OnPhotoCameraInitialized;
                                  previewVideo.SetSource(_photoCamera);

                                  CameraButtons.ShutterKeyHalfPressed += (o, arg) => FocusTheCamera();
                              }

                              if (_timer == null)
                              {
                                  _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                                  _timer.Tick +=TimerOnTick;
                              }

                              if (_focusTimer == null)
                              {
                                  _focusTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(4500) };
                                  _focusTimer.Tick += (o, eventArgs) => FocusTheCamera();
                              }

                              _timer.Start();
                          };
        }

        private void TimerOnTick(object sender, EventArgs eventArgs)
        {
            ScanPreviewBuffer();
        }

        private void FocusTheCamera()
        {
            if (_photoCamera.IsFocusAtPointSupported)
            {
                try
                {
                    _photoCamera.FocusAtPoint(0.5, 0.5);
                }
                catch
                {
                }
            }
            else
            {
                if (_photoCamera.IsFocusSupported)
                {
                    try
                    {
                        _photoCamera.Focus();
                    }
                    catch
                    {
                    }
                }
            }
        }

        private void ScanPreviewBuffer()
        {
            if (_luminance == null)
                return;

            try
            {
                // Because of the timer not being stopped in time, put an empty try/catch
                // to prevent an ObjectDisposedException to occur because the camera is done.
                _photoCamera.GetPreviewBufferY(_luminance.PreviewBufferY);
            }
            catch { }
            // use a dummy writeable bitmap because the luminance values are written directly to the luminance buffer
            var result = _reader.Decode(_dummyBitmap);
            if (result == null) return; // if no barcode is found, don't do anything
            Dispatcher.BeginInvoke(() =>
                                       {
                                           // if a barcode has already been found, we don't want any more going through this time
                                           if (!_barcodeFound)
                                           {
                                               _timer.Stop();
                                               DisplayResult(result);
                                               _barcodeFound = true;
                                           }
                                       });

        }

        private void DisplayResult(Result result)
        {
            if (result != null)
            {
                Messenger.Default.Send(new NotificationMessage(result, "ResultFoundMsg"));
            }
        }

        private void OnPhotoCameraInitialized(object sender, CameraOperationCompletedEventArgs e)
        {
            var width = Convert.ToInt32(_photoCamera.PreviewResolution.Width);
            var height = Convert.ToInt32(_photoCamera.PreviewResolution.Height);

            _photoCamera.FlashMode = FlashMode.Off;

            Dispatcher.BeginInvoke(() =>
            {
                previewTransform.Rotation = _photoCamera.Orientation;
                // create a luminance source which gets its values directly from the camera
                // the instance is returned directly to the reader
                _luminance = new PhotoCameraLuminanceSource(width, height);
                _reader = new BarcodeReader(null, bmp => _luminance, null);

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