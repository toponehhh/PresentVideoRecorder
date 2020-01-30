using GalaSoft.MvvmLight.Command;
using Microsoft.Toolkit.Uwp.Helpers;
using PresentVideoRecorder.Helpers;
using PresentVideoRecorder.Models;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Devices.Enumeration;
using Windows.Graphics.Capture;
using Windows.Media.Capture;
using Windows.System.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PresentVideoRecorder.ViewModels.ContentPageViewModels
{
    public class RecordPageViewModel : UwpContentPageViewModel<Course>
    {
        private RelayCommand _startRecordCommand;
        private RelayCommand _checkAudioCaptureDevicesCommand;
        private RelayCommand _checkVideoCaptureDevicesCommand;
        private RelayCommand<object> _videoCaptureDeviceSelectedCommand;
        private RelayCommand _screenCaptureAreaSelectCommand;

        // MediaCapture and its state variables
        private ScreenCapture _screenCapture;
        private MediaCapture _mediaCapture;
        private bool _isInitialized;
        private bool _isPreviewing;
        private bool _isRecording;
        
        private readonly DisplayRequest _displayRequest = new DisplayRequest();

        public CaptureElement _cameraPreviewControl;

        public RecordPageViewModel(UwpPageViewModel parentPage, IDialogService dialogService) : base(parentPage, dialogService)
        {
            _isRecording = false;
            _startRecordCommand = new RelayCommand(StartRecordCourse, CanAllowRecordStart);
            _checkAudioCaptureDevicesCommand = new RelayCommand(GetAudioProfileSupportedDevicesAsync);
            _checkVideoCaptureDevicesCommand = new RelayCommand(GetVideoProfileSupportedDevicesAsync);
            _videoCaptureDeviceSelectedCommand = new RelayCommand<object>(showCameraPreview);
            _screenCaptureAreaSelectCommand = new RelayCommand(selectScreenCaptureArea, () => _needRecordScreenVideo);
        }

        public void InitCaptureDeviceWithUIControl(CaptureElement cameraPreviewControl, UIElement screenPreviewControl)
        {
            _cameraPreviewControl = cameraPreviewControl;
            _screenCapture = new ScreenCapture(screenPreviewControl);
        }
        

        private string _courseName;
        public string CourseName
        {
            get
            {
                return _courseName;
            }
            set
            {
                Set(ref _courseName, value);
                _startRecordCommand.RaiseCanExecuteChanged();
            }
        }

        private string _courseSavePath;
        public string CourseSavePath
        {
            get
            {
                return _courseSavePath;
            }
            set
            {
                Set(ref _courseSavePath, value);
                _startRecordCommand.RaiseCanExecuteChanged();
            }
        }

        private bool _needRecordAudio;
        public bool NeedRecordAudio 
        { 
            get
            {
                return _needRecordAudio;
            }
            set
            {
                Set(ref _needRecordAudio, value);
                _startRecordCommand.RaiseCanExecuteChanged();
            }
        }

        private bool _needRecordCameraVideo;
        public bool NeedRecordCameraVideo
        {
            get
            {
                return _needRecordCameraVideo;
            }
            set
            {
                Set(ref _needRecordCameraVideo, value);
                _startRecordCommand.RaiseCanExecuteChanged();
                if (!value)
                {
                    if (_isPreviewing)
                    {
                        StopCameraPreviewAsync().Wait(TimeSpan.FromSeconds(5));
                    }
                    VideoCaptureDevices = null;
                    SelectedCameraDevice = null;
                }
            }
        }

        private bool _needRecordScreenVideo;
        public bool NeedRecordScreenVideo
        {
            get
            {
                return _needRecordScreenVideo;
            }
            set
            {
                Set(ref _needRecordScreenVideo, value);
                _startRecordCommand.RaiseCanExecuteChanged();
                _screenCaptureAreaSelectCommand.RaiseCanExecuteChanged();

                if (!value)
                {
                    _screenCapture.StopScreenCapturePreview();
                }
            }
        }

        private DeviceInformationCollection _audioCaptureDevices;
        public DeviceInformationCollection AudioCaptureDevices
        {
            get
            {
                return _audioCaptureDevices;
            }
            set
            {
                Set(ref _audioCaptureDevices, value);
            }
        }

        private DeviceInformationCollection _videoCaptureDevices;
        public DeviceInformationCollection VideoCaptureDevices
        {
            get
            {
                return _videoCaptureDevices;
            }
            set
            {
                Set(ref _videoCaptureDevices, value);
            }
        }

        private DeviceInformation _selectedCameraDevice;
        public DeviceInformation SelectedCameraDevice 
        { 
            get
            {
                return _selectedCameraDevice;
            }
            set
            {
                if (NeedRecordCameraVideo)
                {
                    var _preCameraDevice = _selectedCameraDevice;
                    _selectedCameraDevice = value;
                    if (_selectedCameraDevice == null)
                    {
                        StopCameraPreviewAsync().Wait(TimeSpan.FromSeconds(5));
                    }
                    else
                    {
                        if (_preCameraDevice != null && _preCameraDevice.Id != _selectedCameraDevice.Id)
                        {
                            StopCameraPreviewAsync().Wait(TimeSpan.FromSeconds(5));
                        }
                        if (!_isPreviewing)
                        {
                            showCameraPreview(_selectedCameraDevice);
                        }
                    }
                }
                else
                {
                    _selectedCameraDevice = null;
                }
            }
        }


        public ICommand StartRecordCommand
        {
            get
            {
                return _startRecordCommand;
            }
        }

        public ICommand CheckAudioCaptureDevicesCommand
        {
            get
            {
                return _checkAudioCaptureDevicesCommand;
            }
        }

        public ICommand CheckVideoCaptureDevicesCommand
        {
            get
            {
                return _checkVideoCaptureDevicesCommand;
            }
        }

        public ICommand VideoCaptureDeviceSelectedCommand
        {
            get
            {
                return _videoCaptureDeviceSelectedCommand;
            }
        }

        public ICommand ScreenCaptureAreaSelectCommand
        {
            get
            {
                return _screenCaptureAreaSelectCommand;
            }
        }

        private async void showCameraPreview(object selectedVideoDevice)
        {
            if (selectedVideoDevice != null)
            {
                var videoCaptureDevice = selectedVideoDevice as DeviceInformation;
                await InitializeCameraAsync(videoCaptureDevice);
                if (_isInitialized)
                {
                    await StartCameraPreviewAsync();
                }
            }
        }

        private async Task StartCameraPreviewAsync()
        {
            // Prevent the device from sleeping while the preview is running
            _displayRequest.RequestActive();

            // Set the preview source in the UI and mirror it if necessary
            _cameraPreviewControl.Source = _mediaCapture;

            // Start the preview
            await _mediaCapture.StartPreviewAsync();
            _isPreviewing = true;
        }

        private async Task StopCameraPreviewAsync()
        {
            // Stop the preview
            if (_mediaCapture != null)
            {
                await _mediaCapture.StopPreviewAsync();

                // Use the dispatcher because this method is sometimes called from non-UI threads
                await DispatcherHelper.ExecuteOnUIThreadAsync(() =>
                {
                    // Cleanup the UI
                    _cameraPreviewControl.Source = null;

                    // Allow the device screen to sleep now that the preview is stopped
                    _displayRequest.RequestRelease();
                });
            }
            _isPreviewing = false;
        }

        private async Task InitializeCameraAsync(DeviceInformation cameraDevice)
        {
            Logger.Instance.Info("InitializeCameraAsync Started!");

            if (_mediaCapture == null)
            {
                // Attempt to get the back camera if one is available, but use any camera device if not
                if (cameraDevice == null)
                {
                    Logger.Instance.Warning("No camera device found!");
                    return;
                }

                // Create MediaCapture and its settings
                _mediaCapture = new MediaCapture();

                // Register for a notification when something goes wrong
                _mediaCapture.Failed += MediaCapture_Failed;

                var settings = new MediaCaptureInitializationSettings { VideoDeviceId = cameraDevice.Id };

                // Initialize MediaCapture
                try
                {
                    await _mediaCapture.InitializeAsync(settings);
                    _isInitialized = true;
                }
                catch (UnauthorizedAccessException)
                {
                    Logger.Instance.Error("The app was denied access to the camera");
                }
            }
        }

        private void MediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            Logger.Instance.Error(string.Format("MediaCapture_Failed: (0x{0:X}) {1}", errorEventArgs.Code, errorEventArgs.Message));

            CleanupCamera();
        }

        private void CleanupCamera()
        {
            Logger.Instance.Info("CleanupCameraAsync started!");

            if (_isInitialized)
            {
                if (_isPreviewing)
                {
                    // The call to stop the preview is included here for completeness, but can be
                    // safely removed if a call to MediaCapture.Dispose() is being made later,
                    // as the preview will be automatically stopped at that point
                    StopCameraPreviewAsync().Wait(TimeSpan.FromSeconds(5));
                }

                _isInitialized = false;
            }

            if (_mediaCapture != null)
            {
                _mediaCapture.Failed -= MediaCapture_Failed;
                _mediaCapture.Dispose();
                _mediaCapture = null;
            }

            Logger.Instance.Info("CleanupCameraAsync end!");
        }

        private async void selectScreenCaptureArea()
        {
            var captureItem = await _dialogService.ShowGraphicsCapturePicker();
            _screenCapture?.ShowScreenCapturePreviewAsync(captureItem);
        }

        private async void StartRecordCourse()
        {
            
        }

        public async void GetAudioProfileSupportedDevicesAsync()
        {
            if (NeedRecordAudio)
            {
                AudioCaptureDevices = await DeviceInformation.FindAllAsync(DeviceClass.AudioCapture);
            }
        }

        public async void GetVideoProfileSupportedDevicesAsync()
        {
            if (NeedRecordCameraVideo)
            {
                VideoCaptureDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            }
        }

        private bool CanAllowRecordStart()
        {
            return !_isRecording 
                    && !string.IsNullOrEmpty(CourseName) 
                    && !string.IsNullOrEmpty(CourseSavePath)
                    && (NeedRecordAudio || NeedRecordCameraVideo || NeedRecordScreenVideo);
        }
    }
}
