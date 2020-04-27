using GalaSoft.MvvmLight.Command;
using Microsoft.Toolkit.Uwp.Helpers;
using PresentVideoRecorder.Helpers;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Media;
using Windows.Media.Editing;
using Windows.Media.Playback;
using Windows.UI.Xaml.Controls;

namespace PresentVideoRecorder.ViewModels.ContentPageViewModels
{
    public class EditPageViewModel : UwpContentPageViewModel
    {
        private RelayCommand _playCourseMediaCommand;
        private RelayCommand _pauseOrResumeMediaCommand;
        private RelayCommand _moveForwardMediaCommand;
        private RelayCommand _moveBackwardMediaCommand;

        private MediaComposition _screenMediaComposition, _cameraMediaComposition;
        private MediaPlayerElement _cameraPlayerElement, _screenPlayerElement;
        private MediaTimelineController _mediaPlayerController;

        public EditPageViewModel(MainPageViewModel parentPage, IDialogService dialogService) : base(parentPage, dialogService)
        {
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

        public async Task LoadCourseData()
        {
            if (pageParent.CurrentWorkingCourse != null)
            {
                if (pageParent.CurrentWorkingCourse.CameraVideoFiles?.Count > 0)
                {
                    _cameraPlayerElement.SetMediaPlayer(new MediaPlayer());
                    _cameraMediaComposition = await MediaProcessHelper.LoadMediaFilesForVideoPlayer(pageParent.CurrentWorkingCourse.CameraVideoFiles, pageParent.CurrentWorkingCourse.AudioFiles, _cameraPlayerElement.MediaPlayer, (int)_cameraPlayerElement.ActualWidth, (int)_cameraPlayerElement.ActualHeight);
                }
                if (pageParent.CurrentWorkingCourse.ScreenVideoFiles?.Count > 0)
                {
                    _screenPlayerElement.SetMediaPlayer(new MediaPlayer());
                    if (_cameraMediaComposition?.BackgroundAudioTracks?.Count > 0)
                    {
                        _screenMediaComposition = await MediaProcessHelper.LoadMediaFilesForVideoPlayer(pageParent.CurrentWorkingCourse.ScreenVideoFiles, null, _screenPlayerElement.MediaPlayer, (int)_screenPlayerElement.ActualWidth, (int)_screenPlayerElement.ActualHeight);
                    }
                    else
                    {
                        _screenMediaComposition = await MediaProcessHelper.LoadMediaFilesForVideoPlayer(pageParent.CurrentWorkingCourse.ScreenVideoFiles, pageParent.CurrentWorkingCourse.AudioFiles, _screenPlayerElement.MediaPlayer, (int)_screenPlayerElement.ActualWidth, (int)_screenPlayerElement.ActualHeight);
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

        public async Task Reset()
        {
            await DispatcherHelper.ExecuteOnUIThreadAsync(() =>
            {
                if (_mediaPlayerController.State == MediaTimelineControllerState.Running)
                {
                    _mediaPlayerController.Pause();
                }

                _cameraPlayerElement.Source = null;
                _screenPlayerElement.Source = null;

                CourseTotalSeconds = 0;
                CurrentPlayProgress = 0;
            });
        }
    }
}
