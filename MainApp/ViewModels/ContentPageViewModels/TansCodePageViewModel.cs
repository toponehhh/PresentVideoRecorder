using GalaSoft.MvvmLight.Command;
using PresentVideoRecorder.Helpers;
using PresentVideoRecorder.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Media.Editing;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace PresentVideoRecorder.ViewModels.ContentPageViewModels
{
    public class TansCodePageViewModel : UwpContentPageViewModel<Course>
    {
        private RelayCommand _loadCourseCommand;
        private RelayCommand _startTanscodeCommand;

        //private Dictionary<string, MediaTranscodeProfile> mediaTanscodeProfiles;

        

        public TansCodePageViewModel(UwpPageViewModel parentPage, IDialogService dialogService) : base(parentPage, dialogService)
        {
            _loadCourseCommand = new RelayCommand(async () => await loadCourseData());
            _startTanscodeCommand = new RelayCommand(StartTanscode);
            CombinAudioWaySource = new Dictionary<string, CombineAudioWay>();
            CombinAudioWaySource.Add(CombineAudioWay.WithCameraVideo.ToString(), CombineAudioWay.WithCameraVideo);
            CombinAudioWaySource.Add(CombineAudioWay.WithDesktopVideo.ToString(), CombineAudioWay.WithDesktopVideo);
            CombinAudioWaySource.Add(CombineAudioWay.Independent.ToString(), CombineAudioWay.Independent);
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

        public bool IsTanscodeing
        {
            get
            {
                return false;
            }
        }

        public ICommand LoadCourseCommand
        {
            get
            {
                return _loadCourseCommand;
            }
        }

        public ICommand StartTanscodeCommand
        {
            get
            {
                return _startTanscodeCommand;
            }
        }

        public Dictionary<string, CombineAudioWay> CombinAudioWaySource { get; private set; }



        private void loadDefaultTancodeProfiles()
        {
            //var highQuantityProfile = new MediaTranscodeProfile();

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
                    TanscodeFileSaveDirectory = CourseSavePath;
                }
            }
        }
        private async Task<MediaComposition> loadFilesIntoMediaComposition(IEnumerable<string> videoFilesPath, IEnumerable<string> audioFilesPath = null)
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

        private async void StartTanscode()
        {
            if (NeedTanscodeHighQuantity || NeedTanscodeMediumQuantity || NeedTanscodeLowQuantity)
            {
                if (innerData.CameraVideoFiles?.Count > 0)
                {
                    MediaComposition cameraVideoComposition = null, desktopVideoComposition = null;
                    switch (SelectedCombinAudioWay)
                    {
                        case CombineAudioWay.WithCameraVideo:
                            if (innerData.AudioFiles?.Count > 0)
                            {
                                cameraVideoComposition = await loadFilesIntoMediaComposition(innerData.CameraVideoFiles, innerData.AudioFiles);
                            }
                            else
                            {
                                cameraVideoComposition = await loadFilesIntoMediaComposition(innerData.CameraVideoFiles);
                            }
                            desktopVideoComposition = await loadFilesIntoMediaComposition(innerData.ScreenVideoFiles);
                            break;
                        case CombineAudioWay.WithDesktopVideo:
                            if (innerData.AudioFiles?.Count > 0)
                            {
                                desktopVideoComposition = await loadFilesIntoMediaComposition(innerData.ScreenVideoFiles, innerData.AudioFiles);
                            }
                            else
                            {
                                desktopVideoComposition = await loadFilesIntoMediaComposition(innerData.ScreenVideoFiles);
                            }
                            cameraVideoComposition = await loadFilesIntoMediaComposition(innerData.CameraVideoFiles);
                            break;
                        case CombineAudioWay.Independent:
                            break;
                        default:
                            break;
                    }

                    var tansCodeSaveFolder = await StorageFolder.GetFolderFromPathAsync(TanscodeFileSaveDirectory);

                    if (NeedTanscodeHighQuantity)
                    {
                        if (cameraVideoComposition != null)
                        {
                            var cameraVideoHighFile = await tansCodeSaveFolder.CreateFileAsync("CameraVideo_High.mp4");
                            await cameraVideoComposition.RenderToFileAsync(cameraVideoHighFile, MediaTrimmingPreference.Fast);
                        }
                        if (desktopVideoComposition != null)
                        {
                            var desktopVideoHighFile = await tansCodeSaveFolder.CreateFileAsync("DesktopVideo_High.mp4");
                            await desktopVideoComposition.RenderToFileAsync(desktopVideoHighFile, MediaTrimmingPreference.Fast);
                        }
                    }
                    if (NeedTanscodeMediumQuantity)
                    {
                        if (cameraVideoComposition != null)
                        {
                            MediaEncodingProfile cameraVideoTanscodeMediumProfile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Wvga);
                            var cameraVideoMediumFile = await tansCodeSaveFolder.CreateFileAsync("CameraVideo_Medium.mp4");
                            await cameraVideoComposition.RenderToFileAsync(cameraVideoMediumFile, MediaTrimmingPreference.Fast, cameraVideoTanscodeMediumProfile);
                        }
                        if (desktopVideoComposition != null)
                        {
                            MediaEncodingProfile desktopVideoTanscodeMediumProfile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.HD720p);
                            var desktopVideoMediumFile = await tansCodeSaveFolder.CreateFileAsync("DesktopVideo_Medium.mp4");
                            await desktopVideoComposition.RenderToFileAsync(desktopVideoMediumFile, MediaTrimmingPreference.Fast, desktopVideoTanscodeMediumProfile);
                        }
                    }
                    if (NeedTanscodeLowQuantity)
                    {
                        if (cameraVideoComposition != null)
                        {
                            MediaEncodingProfile cameraVideoTanscodeLowProfile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Qvga);
                            var cameraVideoLowFile = await tansCodeSaveFolder.CreateFileAsync("CameraVideo_Low.mp4");
                            await cameraVideoComposition.RenderToFileAsync(cameraVideoLowFile, MediaTrimmingPreference.Fast, cameraVideoTanscodeLowProfile);
                        }
                        if (desktopVideoComposition != null)
                        {
                            MediaEncodingProfile desktopVideoTanscodeLowProfile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Wvga);
                            var desktopVideoLowFile = await tansCodeSaveFolder.CreateFileAsync("DesktopVideo_Low.mp4");
                            await desktopVideoComposition.RenderToFileAsync(desktopVideoLowFile, MediaTrimmingPreference.Fast, desktopVideoTanscodeLowProfile);
                        }
                    }
                }
            }
            _dialogService.ShowInformationMessage("转码结果", "转码工作已完成");
        }
    }
}
