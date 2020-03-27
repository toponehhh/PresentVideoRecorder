using GalaSoft.MvvmLight.Command;
using Microsoft.Toolkit.Uwp.Helpers;
using PresentVideoRecorder.Helpers;
using PresentVideoRecorder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Editing;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Controls;

namespace PresentVideoRecorder.ViewModels.ContentPageViewModels
{
    public class EditPageViewModel : UwpContentPageViewModel<Course>
    {
        private RelayCommand _loadCourseCommand;
        private RelayCommand _playCourseMediaCommand;
        private RelayCommand _pauseOrResumeMediaCommand;
        private RelayCommand _moveForwardMediaCommand;
        private RelayCommand _moveBackwardMediaCommand;

        private MediaComposition _screenMediaComposition, _cameraMediaComposition;
        private MediaPlayerElement _cameraPlayerElement, _screenPlayerElement;
        private MediaTimelineController _mediaPlayerController;

        public EditPageViewModel(UwpPageViewModel parentPage, IDialogService dialogService) : base(parentPage, dialogService)
        {
            _loadCourseCommand = new RelayCommand(async ()=> await loadCourseData());
            _playCourseMediaCommand = new RelayCommand(playByMediaController);
            _pauseOrResumeMediaCommand = new RelayCommand(pauseOrResumeByMediaController, () => _mediaPlayerController != null);
            _moveForwardMediaCommand = new RelayCommand(() => _mediaPlayerController.Position = _mediaPlayerController.Position.Add(TimeSpan.FromSeconds(5)), () => _mediaPlayerController != null);
            _moveBackwardMediaCommand = new RelayCommand(() => _mediaPlayerController.Position = _mediaPlayerController.Position.Add(TimeSpan.FromSeconds(-5)), () => _mediaPlayerController != null);

            PauseOrResumeStatusText = LocalizedStrings.GetResourceString("Pause");
        }

        public void InitMediaPlayers(MediaPlayerElement cameraPlayerEle, MediaPlayerElement screenPlayerEle)
        {
            _cameraPlayerElement = cameraPlayerEle;
            _screenPlayerElement = screenPlayerEle;
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

        private double _courseTotalSeconds;
        public double CourseTotalSeconds
        {
            get
            {
                return _courseTotalSeconds;
            }
            private set
            {
                Set(ref _courseTotalSeconds, value);
            }
        }

        private double _currentPlayProgress;
        public double CurrentPlayProgress
        {
            get
            {
                return _currentPlayProgress;
            }
            set
            {
                Set(ref _currentPlayProgress, value);
            }
        }

        public ICommand LoadCourseCommand
        {
            get
            {
                return _loadCourseCommand;
            }
        }

        public ICommand PlayCourseMediaCommand
        {
            get
            {
                return _playCourseMediaCommand;
            }
        }

        public ICommand PauseOrResumeMediaCommand
        {
            get
            {
                return _pauseOrResumeMediaCommand;
            }
        }

        public ICommand MoveBackwardMediaCommand
        {
            get
            {
                return _moveBackwardMediaCommand;
            }
        }

        public ICommand MoveForwardMediaCommand
        {
            get
            {
                return _moveForwardMediaCommand;
            }
        }

        private async Task loadCourseData()
        {
            var picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
            picker.FileTypeFilter.Add("*");
            var pickedCourseFolder = await picker.PickSingleFolderAsync();

            if (pickedCourseFolder != null)
            {
                Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", pickedCourseFolder);
                var courseFile = await pickedCourseFolder.GetFileAsync(Course.SAVE_FILE_NAME);
                innerData = await Course.LoadFromFile(courseFile.Path);
                if (innerData != null)
                {
                    CourseName = innerData.Name;
                    CourseSavePath = innerData.DataSaveDirectory;

                    if (innerData.CameraVideoFiles?.Count > 0)
                    {
                        _cameraPlayerElement.SetMediaPlayer(new MediaPlayer());
                        _cameraMediaComposition = await loadMediaFilesForVideoPlayer(innerData.CameraVideoFiles, innerData.AudioFiles, _cameraPlayerElement.MediaPlayer, (int)_cameraPlayerElement.ActualWidth, (int)_cameraPlayerElement.ActualHeight);
                    }
                    if (innerData.ScreenVideoFiles?.Count > 0)
                    {
                        _screenPlayerElement.SetMediaPlayer(new MediaPlayer());
                        if (_cameraMediaComposition?.BackgroundAudioTracks?.Count > 0)
                        {
                            _screenMediaComposition = await loadMediaFilesForVideoPlayer(innerData.ScreenVideoFiles, null, _screenPlayerElement.MediaPlayer, (int)_screenPlayerElement.ActualWidth, (int)_screenPlayerElement.ActualHeight);
                        }
                        else
                        {
                            _screenMediaComposition = await loadMediaFilesForVideoPlayer(innerData.ScreenVideoFiles, innerData.AudioFiles, _screenPlayerElement.MediaPlayer, (int)_screenPlayerElement.ActualWidth, (int)_screenPlayerElement.ActualHeight);
                        }
                    }

                    if (_cameraMediaComposition != null || _screenMediaComposition != null)
                    {
                        _mediaPlayerController = new MediaTimelineController();
                        _moveForwardMediaCommand.RaiseCanExecuteChanged();
                        _moveBackwardMediaCommand.RaiseCanExecuteChanged();
                        _pauseOrResumeMediaCommand.RaiseCanExecuteChanged();
                        _mediaPlayerController.PositionChanged += _mediaPlayerController_PositionChanged;

                        if (_cameraMediaComposition?.Clips?.Count > 0)
                        {
                            setupMediaPlayerForController(_cameraPlayerElement.MediaPlayer);
                            CourseTotalSeconds = _cameraMediaComposition.Duration.TotalSeconds;
                        }

                        if (_screenMediaComposition?.Clips?.Count > 0)
                        {
                            setupMediaPlayerForController(_screenPlayerElement.MediaPlayer);
                            if (_screenMediaComposition.Duration.TotalSeconds < CourseTotalSeconds)
                            {
                                CourseTotalSeconds = _screenMediaComposition.Duration.TotalSeconds;
                            }
                        }

                        if (!_mediaPlayerController.Duration.HasValue)
                        {
                            _mediaPlayerController.Duration = TimeSpan.FromSeconds(CourseTotalSeconds);
                        }
                    }
                }
                _playCourseMediaCommand.RaiseCanExecuteChanged();
            }
        }

        private void _mediaPlayerController_PositionChanged(MediaTimelineController sender, object args)
        {
            if (CourseTotalSeconds > 0)
            {
                DispatcherHelper.ExecuteOnUIThreadAsync(() => CurrentPlayProgress = sender.Position.TotalSeconds);
            }
        }

        private void playByMediaController()
        {
            _mediaPlayerController?.Start();
        }

        private void pauseOrResumeByMediaController()
        {
            if (_mediaPlayerController != null)
            {
                if (_mediaPlayerController.State == MediaTimelineControllerState.Paused)
                {
                    _mediaPlayerController.Resume();
                    PauseOrResumeStatusText = LocalizedStrings.GetResourceString("Pause");
                }
                else if (_mediaPlayerController.State == MediaTimelineControllerState.Running)
                {
                    _mediaPlayerController.Pause();
                    PauseOrResumeStatusText = LocalizedStrings.GetResourceString("Resume");
                }
                else
                {
                    _mediaPlayerController.Start();
                }
            }
        }

        private void setupMediaPlayerForController(MediaPlayer player)
        {
            player.CommandManager.IsEnabled = false;
            player.TimelineController = _mediaPlayerController;
        }

        private async Task<MediaComposition> loadMediaFilesForVideoPlayer(IEnumerable<string> videoMediaFiles, IEnumerable<string> audioMediaFiles, MediaPlayer player, int previewWidth = 0, int previewHeight = 0)
        {
            var mediaComposition = await loadFilesIntoMediaComposition(videoMediaFiles, audioMediaFiles);
            if (mediaComposition?.Clips?.Count > 0)
            {
                MediaStreamSource playerStreamSource = mediaComposition.GenerateMediaStreamSource();
                //if (previewWidth > 0 && previewHeight > 0)
                //{
                //    playerStreamSource = mediaComposition.GeneratePreviewMediaStreamSource(previewWidth, previewHeight);
                //}
                //else
                //{
                //    playerStreamSource = mediaComposition.GenerateMediaStreamSource();
                //}
                if (playerStreamSource != null)
                {
                    player.Source = MediaSource.CreateFromMediaStreamSource(playerStreamSource);
                }
            }
            return mediaComposition;
        }

        private async Task<MediaComposition> loadFilesIntoMediaComposition(IEnumerable<string> videoFilesPath, IEnumerable<string> audioFilesPath=null)
        {
            MediaComposition composition = null;
            if (videoFilesPath?.Count() > 0)
            {
                composition = new MediaComposition();
                foreach (var videoFilePath in videoFilesPath)
                {
                    var mediaFile = await StorageFile.GetFileFromPathAsync(videoFilePath);
                    if (mediaFile != null)
                    {
                        var mediaClip = await MediaClip.CreateFromFileAsync(mediaFile);
                        composition.Clips.Add(mediaClip);
                    }
                }
            }
            if (composition != null && audioFilesPath?.Count() > 0)
            {
                for (int audioIndex = 0; audioIndex < audioFilesPath.Count(); audioIndex++)
                {
                    var mediaFile = await StorageFile.GetFileFromPathAsync(audioFilesPath.ElementAt(audioIndex));
                    if (mediaFile != null)
                    {
                        var audioTrack = await BackgroundAudioTrack.CreateFromFileAsync(mediaFile);
                        audioTrack.Delay = composition.Clips[audioIndex].StartTimeInComposition;
                        composition.BackgroundAudioTracks.Add(audioTrack);
                    }
                }
            }
            return composition;
        }
    }
}
