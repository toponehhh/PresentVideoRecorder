using GalaSoft.MvvmLight.Command;
using Microsoft.Toolkit.Uwp.Helpers;
using PresentVideoRecorder.Helpers;
using PresentVideoRecorder.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Media.Editing;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;
using Windows.Storage;
using Windows.UI.Xaml.Controls;

namespace PresentVideoRecorder.ViewModels.ContentPageViewModels
{
    public class TansCodePageViewModel : UwpContentPageViewModel
    {
        private RelayCommand _startTanscodeCommand, _stopTanscodeCommand;
        private MediaComposition _cameraVideoComposition, _screenVideoComposition;
        private IStorageFile _cameraVideoFinalFile, _screenVideoFinalFile;
        private IAsyncOperationWithProgress<TranscodeFailureReason, double> _cameraVideoHighConvertOperation, _cameraVideoMediumConvertOperation, _cameraVideoLowConvertOperation, _cameraVideoFinalConvertOperation;
        private IAsyncOperationWithProgress<TranscodeFailureReason, double> _screenVideoHighConvertOperation, _screenVideoMediumConvertOperation, _screenVideoLowConvertOperation, _screenVideoFinalConvertOperation;

        public TansCodePageViewModel(MainPageViewModel parentPage, IDialogService dialogService) : base(parentPage, dialogService)
        {
            _startTanscodeCommand = new RelayCommand(async () => await StartTanscode(), canStartTanscode);
            _stopTanscodeCommand = new RelayCommand(StopTanscode, () => IsTanscodeing);
            CombinAudioWaySource = new Dictionary<string, CombineAudioWay>();
            CombinAudioWaySource.Add(LocalizedStrings.GetResourceString("AudioCombineWithCameraVideo"), CombineAudioWay.WithCameraVideo);
            CombinAudioWaySource.Add(LocalizedStrings.GetResourceString("AudioCombineWithScreenVideo"), CombineAudioWay.WithScreenVideo);
            CombinAudioWaySource.Add(LocalizedStrings.GetResourceString("AudioCombineNoneVideo"), CombineAudioWay.Independent);
        }

        private string _tanscodeFileSaveDirectory;
        public string TanscodeFileSaveDirectory
        {
            get
            {
                return _tanscodeFileSaveDirectory;
            }
            set
            {
                Set(ref _tanscodeFileSaveDirectory, value);
            }
        }

        private bool _needTanscodeHighQuantity;
        public bool NeedTanscodeHighQuantity
        {
            get
            {
                return _needTanscodeHighQuantity;
            }
            set
            {
                Set(ref _needTanscodeHighQuantity, value);
                _startTanscodeCommand.RaiseCanExecuteChanged();
            }
        }

        private bool _needTanscodeMediumQuantity;
        public bool NeedTanscodeMediumQuantity
        {
            get
            {
                return _needTanscodeMediumQuantity;
            }
            set
            {
                Set(ref _needTanscodeMediumQuantity, value);
                _startTanscodeCommand.RaiseCanExecuteChanged();
            }
        }

        private bool _needTanscodeLowQuantity;
        public bool NeedTanscodeLowQuantity
        {
            get
            {
                return _needTanscodeLowQuantity;
            }
            set
            {
                Set(ref _needTanscodeLowQuantity, value);
                _startTanscodeCommand.RaiseCanExecuteChanged();
            }
        }

        private CombineAudioWay _selectedCombinAudioWay;
        public CombineAudioWay SelectedCombinAudioWay
        {
            get
            {
                return _selectedCombinAudioWay;
            }
            set
            {
                Set(ref _selectedCombinAudioWay, value);
            }
        }

        private double _cameraFinalVideoConvertProgress;
        public double CameraFinalVideoConvertProgress
        {
            get
            {
                return _cameraFinalVideoConvertProgress;
            }
            set
            {
                Set(ref _cameraFinalVideoConvertProgress, value);
            }
        }

        private double _cameraHighVideoConvertProgress;
        public double CameraHighVideoConvertProgress
        {
            get
            {
                return _cameraHighVideoConvertProgress;
            }
            set
            {
                Set(ref _cameraHighVideoConvertProgress, value);
            }
        }

        private double _cameraMediumVideoConvertProgress;
        public double CameraMediumVideoConvertProgress
        {
            get
            {
                return _cameraMediumVideoConvertProgress;
            }
            set
            {
                Set(ref _cameraMediumVideoConvertProgress, value);
            }
        }

        private double _cameraLowVideoConvertProgress;
        public double CameraLowVideoConvertProgress
        {
            get
            {
                return _cameraLowVideoConvertProgress;
            }
            set
            {
                Set(ref _cameraLowVideoConvertProgress, value);
            }
        }

        private double _screenLowVideoConvertProgress;
        public double ScreenLowVideoConvertProgress
        {
            get
            {
                return _screenLowVideoConvertProgress;
            }
            set
            {
                Set(ref _screenLowVideoConvertProgress, value);
            }
        }

        private double _screenMediumVideoConvertProgress;
        public double ScreenMediumVideoConvertProgress
        {
            get
            {
                return _screenMediumVideoConvertProgress;
            }
            set
            {
                Set(ref _screenMediumVideoConvertProgress, value);
            }
        }

        private double _screenHighVideoConvertProgress;
        public double ScreenHighVideoConvertProgress
        {
            get
            {
                return _screenHighVideoConvertProgress;
            }
            set
            {
                Set(ref _screenHighVideoConvertProgress, value);
            }
        }

        private double _screenFinalVideoConvertProgress;
        public double ScreenFinalVideoConvertProgress
        {
            get
            {
                return _screenFinalVideoConvertProgress;
            }
            set
            {
                Set(ref _screenFinalVideoConvertProgress, value);
            }
        }

        public bool IsTanscodeing
        {
            get; private set;
        }

        public ICommand StartTanscodeCommand
        {
            get
            {
                return _startTanscodeCommand;
            }
        }

        public ICommand StopTanscodeCommand
        {
            get
            {
                return _stopTanscodeCommand;
            }
        }

        public Dictionary<string, CombineAudioWay> CombinAudioWaySource { get; private set; }

        public void LoadCourseData()
        {
            if (pageParent.CurrentWorkingCourse != null)
            {
                TanscodeFileSaveDirectory = pageParent.CurrentWorkingCourse.DataSaveDirectory;
            }
        }

        private bool canStartTanscode()
        {
            return (NeedTanscodeHighQuantity || NeedTanscodeMediumQuantity || NeedTanscodeLowQuantity)
                    && (pageParent.CurrentWorkingCourse.CameraVideoFiles?.Count > 0 || pageParent.CurrentWorkingCourse.ScreenVideoFiles?.Count > 0)
                    && !string.IsNullOrEmpty(TanscodeFileSaveDirectory)
                    && !IsTanscodeing;
        }

        private void StopTanscode()
        {
            if (IsTanscodeing)
            {
                _cameraVideoFinalConvertOperation?.Cancel();
                _screenVideoFinalConvertOperation?.Cancel();

                _cameraVideoHighConvertOperation?.Cancel();
                _screenVideoHighConvertOperation?.Cancel();

                _cameraVideoMediumConvertOperation?.Cancel();
                _screenVideoMediumConvertOperation?.Cancel();

                _cameraVideoLowConvertOperation?.Cancel();
                _screenVideoLowConvertOperation?.Cancel();

                IsTanscodeing = false;
            }
            CheckCommandExecutable();
            pageParent.UnlockNavigation();
        }

        private async Task<bool> showExitConfirmMessage()
        {
            return await _dialogService.ShowConfirmMessage(LocalizedStrings.GetResourceString("Warning"), LocalizedStrings.GetResourceString("CourseIsTanscodingNeedExit"));
        }

        private async Task StartTanscode()
        {
            IsTanscodeing = true;
            CheckCommandExecutable();
            pageParent.LockNavigation(showExitConfirmMessage);
            try
            {
                switch (SelectedCombinAudioWay)
                {
                    case CombineAudioWay.WithCameraVideo:
                        if (pageParent.CurrentWorkingCourse.CameraVideoFiles?.Count > 0)
                        {
                            if (pageParent.CurrentWorkingCourse.AudioFiles?.Count > 0)
                            {
                                _cameraVideoComposition = await MediaProcessHelper.LoadFilesIntoMediaComposition(pageParent.CurrentWorkingCourse.CameraVideoFiles, pageParent.CurrentWorkingCourse.AudioFiles);
                            }
                            else
                            {
                                _cameraVideoComposition = await MediaProcessHelper.LoadFilesIntoMediaComposition(pageParent.CurrentWorkingCourse.CameraVideoFiles);
                            }
                        }
                        if (pageParent.CurrentWorkingCourse.ScreenVideoFiles?.Count > 0)
                        {
                            _screenVideoComposition = await MediaProcessHelper.LoadFilesIntoMediaComposition(pageParent.CurrentWorkingCourse.ScreenVideoFiles);
                        }
                        break;
                    case CombineAudioWay.WithScreenVideo:
                        if (pageParent.CurrentWorkingCourse.ScreenVideoFiles?.Count > 0)
                        {
                            if (pageParent.CurrentWorkingCourse.AudioFiles?.Count > 0)
                            {
                                _screenVideoComposition = await MediaProcessHelper.LoadFilesIntoMediaComposition(pageParent.CurrentWorkingCourse.ScreenVideoFiles, pageParent.CurrentWorkingCourse.AudioFiles);
                            }
                            else
                            {
                                _screenVideoComposition = await MediaProcessHelper.LoadFilesIntoMediaComposition(pageParent.CurrentWorkingCourse.ScreenVideoFiles);
                            }
                        }
                        if (pageParent.CurrentWorkingCourse.CameraVideoFiles?.Count > 0)
                        {
                            _cameraVideoComposition = await MediaProcessHelper.LoadFilesIntoMediaComposition(pageParent.CurrentWorkingCourse.CameraVideoFiles);
                        }
                        break;
                    case CombineAudioWay.Independent:
                        break;
                    default:
                        break;
                }

                var tansCodeSaveFolder = await StorageFolder.GetFolderFromPathAsync(TanscodeFileSaveDirectory);

                if (_cameraVideoComposition != null)
                {
                    _cameraVideoFinalFile = await tansCodeSaveFolder.CreateFileAsync(MediaProcessHelper.CAMERA_VIDEO_FINAL_FILE_NAME, CreationCollisionOption.ReplaceExisting);
                    _cameraVideoFinalConvertOperation = _cameraVideoComposition.RenderToFileAsync(_cameraVideoFinalFile, MediaTrimmingPreference.Fast);
                    _cameraVideoFinalConvertOperation.Progress = new AsyncOperationProgressHandler<TranscodeFailureReason, double>(async (info, progress) => { await DispatcherHelper.ExecuteOnUIThreadAsync(() => CameraFinalVideoConvertProgress = progress); });
                    //await _cameraVideoFinalConvertOperation;
                }
                if (_screenVideoComposition != null)
                {
                    _screenVideoFinalFile = await tansCodeSaveFolder.CreateFileAsync(MediaProcessHelper.SCREEN_VIDEO_FINAL_FILE_NAME, CreationCollisionOption.ReplaceExisting);
                    _screenVideoFinalConvertOperation = _screenVideoComposition.RenderToFileAsync(_screenVideoFinalFile, MediaTrimmingPreference.Fast);
                    _screenVideoFinalConvertOperation.Progress = new AsyncOperationProgressHandler<TranscodeFailureReason, double>(async (info, progress) => { await DispatcherHelper.ExecuteOnUIThreadAsync(() => ScreenFinalVideoConvertProgress = progress); });
                    //await _screenVideoFinalConvertOperation;
                }
                await Task.WhenAll(_cameraVideoFinalConvertOperation?.AsTask(), _screenVideoFinalConvertOperation?.AsTask());
                if (_cameraVideoFinalFile != null && StorageFileHelper.IsFilePathValid(_cameraVideoFinalFile.Path))
                {
                    pageParent.CurrentWorkingCourse.CameraFinalVideoFile = _cameraVideoFinalFile.Path;
                }
                if (_screenVideoFinalFile != null && StorageFileHelper.IsFilePathValid(_screenVideoFinalFile.Path))
                {
                    pageParent.CurrentWorkingCourse.ScreenFinalVideoFile = _screenVideoFinalFile.Path;
                }
                pageParent.CurrentWorkingCourse?.SaveToStorageFileAsync();
                pageParent.CurrentWorkingCourse?.CameraTansCodeVideoFiles?.Clear();
                pageParent.CurrentWorkingCourse?.ScreenTansCodeVideoFiles?.Clear();

                if (NeedTanscodeHighQuantity)
                {
                    if (_cameraVideoComposition != null)
                    {
                        var cameraVideoHighFile = await tansCodeSaveFolder.CreateFileAsync(string.Format(MediaProcessHelper.CAMERA_VIDEO_TRANS_FILE_NAME_FORMAT, "High"), CreationCollisionOption.ReplaceExisting);
                        pageParent.CurrentWorkingCourse.CameraTansCodeVideoFiles.Add(cameraVideoHighFile.Path);
                        _cameraVideoHighConvertOperation = _cameraVideoComposition.RenderToFileAsync(cameraVideoHighFile, MediaTrimmingPreference.Fast);
                        _cameraVideoHighConvertOperation.Progress = new AsyncOperationProgressHandler<TranscodeFailureReason, double>(async (info, progress) => { await DispatcherHelper.ExecuteOnUIThreadAsync(() => CameraHighVideoConvertProgress = progress / 2 + 50); });
                        //await _cameraVideoHighConvertOperation;
                    }
                    if (_screenVideoComposition != null)
                    {
                        var screenVideoHighFile = await tansCodeSaveFolder.CreateFileAsync(string.Format(MediaProcessHelper.SCREEN_VIDEO_TRANS_FILE_NAME_FORMAT, "High"), CreationCollisionOption.ReplaceExisting);
                        pageParent.CurrentWorkingCourse.ScreenTansCodeVideoFiles.Add(screenVideoHighFile.Path);
                        _screenVideoHighConvertOperation = _screenVideoComposition.RenderToFileAsync(screenVideoHighFile, MediaTrimmingPreference.Fast);
                        _screenVideoHighConvertOperation.Progress = new AsyncOperationProgressHandler<TranscodeFailureReason, double>(async (info, progress) => { await DispatcherHelper.ExecuteOnUIThreadAsync(() => ScreenHighVideoConvertProgress = progress / 2 + 50); });
                        //await _screenVideoHighConvertOperation;
                    }
                }

                if (NeedTanscodeMediumQuantity)
                {
                    IsTanscodeing = true;
                    if (_cameraVideoComposition != null)
                    {
                        MediaEncodingProfile cameraVideoTanscodeMediumProfile = await MediaEncodingProfile.CreateFromFileAsync(_cameraVideoFinalFile);
                        cameraVideoTanscodeMediumProfile.Video.Width = cameraVideoTanscodeMediumProfile.Video.Width / 3 * 2;
                        cameraVideoTanscodeMediumProfile.Video.Height = cameraVideoTanscodeMediumProfile.Video.Height / 3 * 2;
                        cameraVideoTanscodeMediumProfile.Video.Bitrate = cameraVideoTanscodeMediumProfile.Video.Bitrate / 3 * 2;
                        var cameraVideoMediumFile = await tansCodeSaveFolder.CreateFileAsync(string.Format(MediaProcessHelper.CAMERA_VIDEO_TRANS_FILE_NAME_FORMAT, "Medium"), CreationCollisionOption.ReplaceExisting);
                        pageParent.CurrentWorkingCourse.CameraTansCodeVideoFiles.Add(cameraVideoMediumFile.Path);
                        _cameraVideoMediumConvertOperation = _cameraVideoComposition.RenderToFileAsync(cameraVideoMediumFile, MediaTrimmingPreference.Fast, cameraVideoTanscodeMediumProfile);
                        _cameraVideoMediumConvertOperation.Progress = new AsyncOperationProgressHandler<TranscodeFailureReason, double>(async (info, progress) => { await DispatcherHelper.ExecuteOnUIThreadAsync(() => CameraMediumVideoConvertProgress = progress); });
                        //await _cameraVideoMediumConvertOperation;
                    }
                    if (_screenVideoComposition != null)
                    {
                        MediaEncodingProfile screenVideoTanscodeMediumProfile = await MediaEncodingProfile.CreateFromFileAsync(_screenVideoFinalFile);
                        screenVideoTanscodeMediumProfile.Video.Width = screenVideoTanscodeMediumProfile.Video.Width / 3 * 2;
                        screenVideoTanscodeMediumProfile.Video.Height = screenVideoTanscodeMediumProfile.Video.Height / 3 * 2;
                        screenVideoTanscodeMediumProfile.Video.Bitrate = screenVideoTanscodeMediumProfile.Video.Bitrate / 3 * 2;
                        var screenVideoMediumFile = await tansCodeSaveFolder.CreateFileAsync(string.Format(MediaProcessHelper.SCREEN_VIDEO_TRANS_FILE_NAME_FORMAT, "Medium"), CreationCollisionOption.ReplaceExisting);
                        pageParent.CurrentWorkingCourse.ScreenTansCodeVideoFiles.Add(screenVideoMediumFile.Path);
                        _screenVideoMediumConvertOperation = _screenVideoComposition.RenderToFileAsync(screenVideoMediumFile, MediaTrimmingPreference.Fast, screenVideoTanscodeMediumProfile);
                        _screenVideoMediumConvertOperation.Progress = new AsyncOperationProgressHandler<TranscodeFailureReason, double>(async (info, progress) => { await DispatcherHelper.ExecuteOnUIThreadAsync(() => ScreenMediumVideoConvertProgress = progress); });
                        //await _screenVideoMediumConvertOperation;
                    }
                }
                if (NeedTanscodeLowQuantity)
                {
                    IsTanscodeing = true;
                    if (_cameraVideoComposition != null)
                    {
                        MediaEncodingProfile cameraVideoTanscodeLowProfile = await MediaEncodingProfile.CreateFromFileAsync(_cameraVideoFinalFile);
                        cameraVideoTanscodeLowProfile.Video.Width = cameraVideoTanscodeLowProfile.Video.Width / 3;
                        cameraVideoTanscodeLowProfile.Video.Height = cameraVideoTanscodeLowProfile.Video.Height / 3;
                        cameraVideoTanscodeLowProfile.Video.Bitrate = cameraVideoTanscodeLowProfile.Video.Bitrate / 3;
                        var cameraVideoLowFile = await tansCodeSaveFolder.CreateFileAsync(string.Format(MediaProcessHelper.CAMERA_VIDEO_TRANS_FILE_NAME_FORMAT, "Low"), CreationCollisionOption.ReplaceExisting);
                        pageParent.CurrentWorkingCourse.CameraTansCodeVideoFiles.Add(cameraVideoLowFile.Path);
                        _cameraVideoLowConvertOperation = _cameraVideoComposition.RenderToFileAsync(cameraVideoLowFile, MediaTrimmingPreference.Fast, cameraVideoTanscodeLowProfile);
                        _cameraVideoLowConvertOperation.Progress = new AsyncOperationProgressHandler<TranscodeFailureReason, double>(async (info, progress) => { await DispatcherHelper.ExecuteOnUIThreadAsync(() => CameraLowVideoConvertProgress = progress); });
                        //await _cameraVideoLowConvertOperation;
                    }
                    if (_screenVideoComposition != null)
                    {
                        MediaEncodingProfile screenVideoTanscodeLowProfile = await MediaEncodingProfile.CreateFromFileAsync(_screenVideoFinalFile);
                        screenVideoTanscodeLowProfile.Video.Width = screenVideoTanscodeLowProfile.Video.Width / 3;
                        screenVideoTanscodeLowProfile.Video.Height = screenVideoTanscodeLowProfile.Video.Height / 3;
                        screenVideoTanscodeLowProfile.Video.Bitrate = screenVideoTanscodeLowProfile.Video.Bitrate / 3;
                        var screenVideoLowFile = await tansCodeSaveFolder.CreateFileAsync(string.Format(MediaProcessHelper.SCREEN_VIDEO_TRANS_FILE_NAME_FORMAT, "Low"), CreationCollisionOption.ReplaceExisting);
                        pageParent.CurrentWorkingCourse.ScreenTansCodeVideoFiles.Add(screenVideoLowFile.Path);
                        _screenVideoLowConvertOperation = _screenVideoComposition.RenderToFileAsync(screenVideoLowFile, MediaTrimmingPreference.Fast, screenVideoTanscodeLowProfile);
                        _screenVideoLowConvertOperation.Progress = new AsyncOperationProgressHandler<TranscodeFailureReason, double>(async (info, progress) => { await DispatcherHelper.ExecuteOnUIThreadAsync(() => ScreenLowVideoConvertProgress = progress); });
                        //await _screenVideoLowConvertOperation;
                    }
                }

                await Task.WhenAll(_cameraVideoHighConvertOperation?.AsTask(), _cameraVideoMediumConvertOperation?.AsTask(), _cameraVideoLowConvertOperation?.AsTask());
                await Task.WhenAll(_screenVideoHighConvertOperation?.AsTask(), _screenVideoMediumConvertOperation?.AsTask(), _screenVideoLowConvertOperation?.AsTask());
                await pageParent.CurrentWorkingCourse.SaveToStorageFileAsync();

                _dialogService.ShowInformationMessage("转码结果", "转码工作已完成");
            }
            catch (TaskCanceledException)
            {
                _dialogService.ShowInformationMessage("转码取消", "转码工作已被用户取消");
            }

            catch (Exception ex)
            {
                _dialogService.ShowInformationMessage("转码失败", $"转码工作出现错误，具体信息是{ex}");
                Logger.Instance.Error(ex.ToString());
            }

            IsTanscodeing = false;
            CheckCommandExecutable();
            pageParent.UnlockNavigation();
        }

        private void CheckCommandExecutable()
        {
            _startTanscodeCommand.RaiseCanExecuteChanged();
            _stopTanscodeCommand.RaiseCanExecuteChanged();
        }
    }
}
