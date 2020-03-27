using GalaSoft.MvvmLight.Command;
using Microsoft.Toolkit.Uwp.Helpers;
using PresentVideoRecorder.Helpers;
using PresentVideoRecorder.Models;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.System.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PresentVideoRecorder.ViewModels.ContentPageViewModels
{
    public class RecordPageViewModel : UwpContentPageViewModel<Course>
    {
        private const string SCREEN_VIDEO_FILE_NAME = "ScreenCaptureVideo.mp4";
        private const string CAMERA_VIDEO_FILE_NAME = "CameraCaptureVideo.mp4";
        private const string AUDIO_CAPTURE_FILE_NAME = "AudioCaptureVideo.mp3";

        private RelayCommand _startRecordCommand;
        private RelayCommand _stopRecordCommand;
        private RelayCommand _checkVideoCaptureDevicesCommand;
        private RelayCommand<object> _videoCaptureDeviceSelectedCommand;
        private RelayCommand<object> _audioCaptureDeviceSelectedCommand;
        private RelayCommand _screenCaptureAreaSelectCommand;
        private RelayCommand _pickCourseSaveFolderCommand;
        private RelayCommand _createNewRecordCommand;
        private RelayCommand<bool> _muteRecordControlCommand;

        // MediaCapture and its state variables
        private ScreenCapture _screenCapture;
        private MediaCapture _videoMediaCapture;
        private MediaCapture _audioMediaCapture;

        private bool _isVideoCaptureInitialized;
        private bool _isAudioCaptureInitialized;
        private bool _isCameraPreviewing;
        private bool _isScreenPreviewing;
        private bool _isVideoCaptureRecording;
        private bool _isAudioCaptureRecording;
        private bool _isScreenCaptureRecording;

        private Stopwatch _recordStopWatch;
        private DispatcherTimer _recordTimer;

        private readonly DisplayRequest _displayRequest = new DisplayRequest();

        public CaptureElement _cameraPreviewControl;

        public RecordPageViewModel(UwpPageViewModel parentPage, IDialogService dialogService) : base(parentPage, dialogService)
        {
            _startRecordCommand = new RelayCommand(async () => await StartRecordCourse(), CanAllowRecordStart);
            _stopRecordCommand = new RelayCommand(async () => await StopRecordCourse(), () => IsRecording);
            _checkVideoCaptureDevicesCommand = new RelayCommand(getVideoProfileSupportedDevicesAsync, () => !_isVideoCaptureRecording);
            _videoCaptureDeviceSelectedCommand = new RelayCommand<object>(showCameraPreview, (obj) => !_isVideoCaptureRecording);
            _audioCaptureDeviceSelectedCommand = new RelayCommand<object>(showAudioPreview, (obj) => !_isAudioCaptureRecording);
            _screenCaptureAreaSelectCommand = new RelayCommand(async () => await selectScreenCaptureArea(), () => _needRecordScreenVideo);
            _pickCourseSaveFolderCommand = new RelayCommand(showCourseSaveFolderPicker, () => !IsRecording);
            _createNewRecordCommand = new RelayCommand(async () => await createNewRecord());
            _muteRecordControlCommand = new RelayCommand<bool>(switchMuteOrUnmute, (m) => _isAudioCaptureRecording);

            _recordStopWatch = new Stopwatch();
            _recordTimer = new DispatcherTimer();
            _recordTimer.Interval = TimeSpan.FromSeconds(1);
            _recordTimer.Tick += _recordTimer_Tick;
            
            MuteStatusText = LocalizedStrings.GetResourceString("SetMute");
            CourseTotalRecordTime = TimeSpan.Zero.ToString(@"hh\:mm\:ss");
        }

        private void _recordTimer_Tick(object sender, object e)
        {
            if (_recordStopWatch.IsRunning)
            {
                CourseTotalRecordTime = _recordStopWatch.Elapsed.ToString(@"hh\:mm\:ss");
            }
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
            }
        }

        private string _courseTotalRecordTime;
        public string CourseTotalRecordTime
        {
            get
            {
                return _courseTotalRecordTime;
            }
            set
            {
                Set(ref _courseTotalRecordTime, value);
            }
        }

        private async void switchOffVideoCaptureDevice()
        {
            if (!NeedRecordCameraVideo)
            {
                await cleanupCamera();
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

                if (!value && _isScreenPreviewing)
                {
                    _screenCapture.StopScreenCapturePreview();
                    _isScreenPreviewing = false;
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

        private string _pauseOrResumeStatusText;
        public string PauseOrResumeStatusText
        {
            get
            {
                return _pauseOrResumeStatusText;
            }
            set
            {
                Set(ref _pauseOrResumeStatusText, value);
            }
        }

        private string _muteStatusText;
        public string MuteStatusText
        {
            get
            {
                return _muteStatusText;
            }
            set
            {
                Set(ref _muteStatusText, value);
            }
        }

        public DeviceInformation SelectedCameraDevice
        {
            get;
            private set;
        }

        public DeviceInformation SelectedAudioDevice
        {
            get;
            private set;
        }

        public bool IsRecording
        {
            get
            {
                return _isAudioCaptureRecording || _isScreenCaptureRecording || _isScreenCaptureRecording;
            }
        }

        public ICommand StartRecordCommand
        {
            get
            {
                return _startRecordCommand;
            }
        }

        public ICommand StopRecordCommand
        {
            get
            {
                return _stopRecordCommand;
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

        public ICommand AudioCaptureDeviceSelectedCommand
        {
            get
            {
                return _audioCaptureDeviceSelectedCommand;
            }
        }

        public ICommand ScreenCaptureAreaSelectCommand
        {
            get
            {
                return _screenCaptureAreaSelectCommand;
            }
        }

        public ICommand PickCourseSaveFolderCommand
        {
            get
            {
                return _pickCourseSaveFolderCommand;
            }
        }

        public ICommand CreateNewRecordCommand
        {
            get
            {
                return _createNewRecordCommand;
            }
        }

        public ICommand MuteRecordControlCommand
        {
            get
            {
                return _muteRecordControlCommand;
            }
        }

        private async void showCameraPreview(object selectedVideoDevice)
        {
            if (selectedVideoDevice != null && NeedRecordCameraVideo)
            {
                var videoCaptureDevice = selectedVideoDevice as DeviceInformation;
                var preCameraDevice = SelectedCameraDevice;
                SelectedCameraDevice = videoCaptureDevice;
                if (videoCaptureDevice == null || (preCameraDevice != null && preCameraDevice.Id != videoCaptureDevice.Id))
                {
                    await cleanupCamera();
                }
                await initializeCameraAsync(videoCaptureDevice);
                if (_isVideoCaptureInitialized)
                {
                    await StartCameraPreviewAsync();
                }
            }
        }

        private async void showAudioPreview(object selectedAudioDevice)
        {
            if (selectedAudioDevice != null)
            {
                var audioCaptureDevice = selectedAudioDevice as DeviceInformation;
                var preAudioDevice = SelectedCameraDevice;
                SelectedAudioDevice = audioCaptureDevice;
                if (audioCaptureDevice == null || (preAudioDevice != null && preAudioDevice.Id != audioCaptureDevice.Id))
                {
                    cleanupAudio();
                }
                await initializeAudioInputAsync(audioCaptureDevice);
            }
        }

        private async Task StartCameraPreviewAsync()
        {
            // Prevent the device from sleeping while the preview is running
            _displayRequest.RequestActive();

            // Set the preview source in the UI and mirror it if necessary
            _cameraPreviewControl.Source = _videoMediaCapture;

            // Start the preview
            await _videoMediaCapture.StartPreviewAsync();
            _isCameraPreviewing = true;
        }

        private async Task StopCameraPreviewAsync()
        {
            // Stop the preview
            if (_videoMediaCapture != null)
            {
                await _videoMediaCapture.StopPreviewAsync();

                // Use the dispatcher because this method is sometimes called from non-UI threads
                await DispatcherHelper.ExecuteOnUIThreadAsync(() =>
                {
                    // Cleanup the UI
                    _cameraPreviewControl.Source = null;

                    // Allow the device screen to sleep now that the preview is stopped
                    _displayRequest.RequestRelease();
                });
            }
            _isCameraPreviewing = false;
        }

        private async Task initializeAudioInputAsync(DeviceInformation audioDevice)
        {
            Logger.Instance.Info("InitializeAudioAsync Started!");

            if (_audioMediaCapture == null)
            {
                // Attempt to get the back camera if one is available, but use any camera device if not
                if (audioDevice == null)
                {
                    Logger.Instance.Warning("No audio input device found!");
                    return;
                }

                if (!audioDevice.IsEnabled)
                {
                    Logger.Instance.Warning($"Selected device {audioDevice.Name} is not enabled!");
                    return;
                }

                // Create MediaCapture and its settings
                _audioMediaCapture = new MediaCapture();

                // Register for a notification when something goes wrong
                _audioMediaCapture.Failed += MediaCapture_Failed;
                _audioMediaCapture.RecordLimitationExceeded += MediaCapture_RecordLimitationExceeded;

                var settings = new MediaCaptureInitializationSettings { AudioDeviceId = audioDevice.Id, StreamingCaptureMode = StreamingCaptureMode.Audio };

                // Initialize MediaCapture
                try
                {
                    await _audioMediaCapture.InitializeAsync(settings);
                    _isAudioCaptureInitialized = true;
                }
                catch (UnauthorizedAccessException)
                {
                    Logger.Instance.Error("The app was denied access to the audio device!");
                }
            }
        }

        private async Task initializeCameraAsync(DeviceInformation cameraDevice)
        {
            Logger.Instance.Info("InitializeCameraAsync Started!");

            if (_videoMediaCapture == null)
            {
                // Attempt to get the back camera if one is available, but use any camera device if not
                if (cameraDevice == null)
                {
                    Logger.Instance.Warning("No camera device found!");
                    return;
                }

                if (!cameraDevice.IsEnabled)
                {
                    Logger.Instance.Warning($"Selected device {cameraDevice.Name} is not enabled!");
                    return;
                }

                // Create MediaCapture and its settings
                _videoMediaCapture = new MediaCapture();

                // Register for a notification when something goes wrong
                _videoMediaCapture.Failed += MediaCapture_Failed;
                _videoMediaCapture.RecordLimitationExceeded += MediaCapture_RecordLimitationExceeded;

                var settings = new MediaCaptureInitializationSettings { VideoDeviceId = cameraDevice.Id, StreamingCaptureMode = StreamingCaptureMode.Video };

                // Initialize MediaCapture
                try
                {
                    await _videoMediaCapture.InitializeAsync(settings);
                    _isVideoCaptureInitialized = true;
                }
                catch (UnauthorizedAccessException)
                {
                    Logger.Instance.Error("The app was denied access to the camera!");
                }
            }
        }

        private async void MediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            Logger.Instance.Error(string.Format("MediaCapture_Failed: (0x{0:X}) {1}", errorEventArgs.Code, errorEventArgs.Message));
            if (sender.MediaCaptureSettings.StreamingCaptureMode == StreamingCaptureMode.Audio)
            {
                cleanupAudio();
            }
            else
            {
                await cleanupCamera();
            }
        }

        private async void MediaCapture_RecordLimitationExceeded(MediaCapture sender)
        {
            // This is a notification that recording has to stop, and the app is expected to finalize the recording

            //await StopRecordingAsync();
            await _dialogService.ShowConfirmMessage(LocalizedStrings.GetResourceString("Warning"), LocalizedStrings.GetResourceString("MediaCaptureRecordLimitationExceeded"));
        }

        private void cleanupAudio()
        {
            Logger.Instance.Info("CleanupAudioAsync started!");

            if (_isAudioCaptureInitialized)
            {
                _isAudioCaptureInitialized = false;
            }

            if (_audioMediaCapture != null)
            {
                _audioMediaCapture.Failed -= MediaCapture_Failed;
                _audioMediaCapture.RecordLimitationExceeded -= MediaCapture_RecordLimitationExceeded;
                _audioMediaCapture.Dispose();
                _audioMediaCapture = null;
            }

            //SelectedAudioDevice = null;

            Logger.Instance.Info("CleanupAudioAsync end!");
        }

        private async Task cleanupCamera()
        {
            Logger.Instance.Info("CleanupCameraAsync started!");

            if (_isVideoCaptureInitialized)
            {
                if (_isCameraPreviewing)
                {
                    // The call to stop the preview is included here for completeness, but can be
                    // safely removed if a call to MediaCapture.Dispose() is being made later,
                    // as the preview will be automatically stopped at that point
                    await StopCameraPreviewAsync();
                }

                _isVideoCaptureInitialized = false;
            }

            if (_videoMediaCapture != null)
            {
                _videoMediaCapture.Failed -= MediaCapture_Failed;
                _videoMediaCapture.RecordLimitationExceeded -= MediaCapture_RecordLimitationExceeded;
                _videoMediaCapture.Dispose();
                _videoMediaCapture = null;
            }

            //SelectedCameraDevice = null;

            Logger.Instance.Info("CleanupCameraAsync end!");
        }

        private async Task selectScreenCaptureArea()
        {
            var captureItem = await _dialogService.ShowGraphicsCapturePicker();
            if (captureItem != null)
            {
                _screenCapture?.ShowScreenCapturePreviewAsync(captureItem);
                _isScreenPreviewing = true;
            }
            else
            {
                NeedRecordScreenVideo = false;
            }
        }

        private async Task StartRecordCourse()
        {
            var courseSaveParentFolder = await StorageFolder.GetFolderFromPathAsync(CourseSavePath);
            var courseSaveFolder = await courseSaveParentFolder.CreateFolderAsync(CourseName, CreationCollisionOption.OpenIfExists);
            if (innerData == null)
            {
                innerData = Course.CreateNewCourse(CourseName);
                innerData.DataSaveDirectory = courseSaveFolder.Path;
            }

            //if (NeedRecordScreenVideo && !_isScreenPreviewing)
            //{
            //    selectScreenCaptureArea();
            //}
            var audioRecordTask = Task.Run(async () =>
                                  {
                                      if (_isAudioCaptureInitialized)
                                      {
                                          var audioCaptureFile = await courseSaveFolder.CreateFileAsync(AUDIO_CAPTURE_FILE_NAME, CreationCollisionOption.GenerateUniqueName);
                                          innerData.AudioFiles.Add(audioCaptureFile.Path);
                                          await _audioMediaCapture.StartRecordToStorageFileAsync(MediaEncodingProfile.CreateMp3(AudioEncodingQuality.Auto), audioCaptureFile);
                                          _isAudioCaptureRecording = true;
                                      }
                                  });
            var cameraRecordTask = Task.Run(async () =>
                                  {
                                      if (NeedRecordCameraVideo && _isVideoCaptureInitialized)
                                      {
                                          var videoCaptureFile = await courseSaveFolder.CreateFileAsync(CAMERA_VIDEO_FILE_NAME, CreationCollisionOption.GenerateUniqueName);
                                          innerData.CameraVideoFiles.Add(videoCaptureFile.Path);
                                          await _videoMediaCapture.StartRecordToStorageFileAsync(MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto), videoCaptureFile);
                                          _isVideoCaptureRecording = true;
                                      }
                                  });
            var screenRecordTask = Task.Run(async () =>
                                  {
                                      if (NeedRecordScreenVideo)
                                      {
                                          var screenCaptureFile = await courseSaveFolder.CreateFileAsync(SCREEN_VIDEO_FILE_NAME, CreationCollisionOption.GenerateUniqueName);
                                          innerData.ScreenVideoFiles.Add(screenCaptureFile.Path);
                                          _isScreenCaptureRecording = true;
                                          _screenCapture.StartScreenRecordAsync(screenCaptureFile);
                                      }
                                  });
            await Task.WhenAll(audioRecordTask, cameraRecordTask, screenRecordTask);

            _recordTimer.Start();
            _recordStopWatch.Start();
            _startRecordCommand.RaiseCanExecuteChanged();
            _stopRecordCommand.RaiseCanExecuteChanged();
            _muteRecordControlCommand.RaiseCanExecuteChanged();
        }

        public async Task StopRecordCourse()
        {
            var audioRecordTask = Task.Run(async () =>
                                  {
                                      if (_isAudioCaptureRecording)
                                      {
                                          await _audioMediaCapture.StopRecordAsync();
                                          _isAudioCaptureRecording = false;
                                      }
                                  });
            var cameraRecordTask = Task.Run(async () =>
                                  {
                                      if (_isVideoCaptureRecording)
                                      {
                                          await _videoMediaCapture.StopRecordAsync();
                                          _isVideoCaptureRecording = false;
                                      }
                                  });
            var screenRecordTask = Task.Run(() =>
                                  {
                                      if (_isScreenCaptureRecording)
                                      {
                                          _screenCapture.StopScreenRecord();
                                          _isScreenCaptureRecording = false;
                                      }
                                  });

            await Task.WhenAll(audioRecordTask, cameraRecordTask, screenRecordTask);
            
            _recordStopWatch.Stop();
            _recordTimer.Stop();

            var dataSaved = await innerData.SaveToStorageFileAsync();
            if (!dataSaved)
            {
                _dialogService.ShowInformationMessage(LocalizedStrings.GetResourceString("Error"), LocalizedStrings.GetResourceString("DataFileSaveFailed"));
            }
            else
            {
                _startRecordCommand.RaiseCanExecuteChanged();
                _stopRecordCommand.RaiseCanExecuteChanged();
                _muteRecordControlCommand.RaiseCanExecuteChanged();
            }
            
        }

        public async Task GetAudioProfileSupportedDevicesAsync()
        {
            AudioCaptureDevices = await DeviceInformation.FindAllAsync(DeviceClass.AudioCapture);
        }

        private async void getVideoProfileSupportedDevicesAsync()
        {
            if (NeedRecordCameraVideo)
            {
                VideoCaptureDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            }
            else
            {
                switchOffVideoCaptureDevice();
                VideoCaptureDevices = null;
            }
        }

        private bool CanAllowRecordStart()
        {
            var reslt = !_isVideoCaptureRecording
                    && !_isAudioCaptureRecording
                    && !_isScreenCaptureRecording
                    && !string.IsNullOrEmpty(CourseName)
                    && !string.IsNullOrEmpty(CourseSavePath)
                    && (SelectedAudioDevice != null || (NeedRecordCameraVideo && SelectedCameraDevice != null) || NeedRecordScreenVideo);
            return reslt;
        }

        private async void showCourseSaveFolderPicker()
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
            folderPicker.FileTypeFilter.Add("*");

            StorageFolder saveFolder = await folderPicker.PickSingleFolderAsync();
            if (saveFolder != null)
            {
                // Application now has read/write access to all contents in the picked folder
                // (including other sub-folder contents)
                Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", saveFolder);
                CourseSavePath = saveFolder.Path;
            }
            else
            {
                CourseSavePath = string.Empty;
            }
        }

        public async Task Reset()
        {
            if (IsRecording)
            {
                await StopRecordCourse();
            }

            if(_isScreenPreviewing)
            {
                _screenCapture.StopScreenCapturePreview();
            }

            cleanupAudio();
            AudioCaptureDevices = null;
            await GetAudioProfileSupportedDevicesAsync();
            await cleanupCamera();
            VideoCaptureDevices = null;
            innerData = null;
            CourseName = string.Empty;
            CourseSavePath = string.Empty;
            NeedRecordCameraVideo = false;
            NeedRecordScreenVideo = false;
            _recordStopWatch.Reset();
            CourseTotalRecordTime = TimeSpan.Zero.ToString(@"hh\:mm\:ss");
        }
        
        private async Task createNewRecord()
        {
            await Reset();
        }

        private void switchMuteOrUnmute(bool needMute)
        {
            if (_isAudioCaptureRecording)
            {
                _audioMediaCapture.AudioDeviceController.Muted = needMute;
                MuteStatusText = needMute ? LocalizedStrings.GetResourceString("SetUnMute") : LocalizedStrings.GetResourceString("SetMute");
            }
        }
    }
}
