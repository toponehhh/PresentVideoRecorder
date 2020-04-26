using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Editing;
using Windows.Media.Playback;
using Windows.Storage;

namespace PresentVideoRecorder.Helpers
{
    public static class MediaProcessHelper
    {
        public const string SCREEN_VIDEO_FILE_NAME = "ScreenCaptureVideo.mp4";
        public const string CAMERA_VIDEO_FILE_NAME = "CameraCaptureVideo.mp4";
        public const string AUDIO_CAPTURE_FILE_NAME = "AudioCaptureVideo.mp3";

        public const string SCREEN_VIDEO_TRANS_FILE_NAME_FORMAT = "ScreenCaptureVideo_{0}.mp4";
        public const string CAMERA_VIDEO_TRANS_FILE_NAME_FORMAT = "CameraCaptureVideo_{0}.mp4";

        public const string SCREEN_VIDEO_FINAL_FILE_NAME = "ScreenCaptureVideo_Final.mp4";
        public const string CAMERA_VIDEO_FINAL_FILE_NAME = "CameraCaptureVideo_Final.mp4";

        public static async Task<MediaComposition> LoadFilesIntoMediaComposition(IEnumerable<string> videoFilesPath, IEnumerable<string> audioFilesPath = null)
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

        public static async Task<MediaComposition> LoadMediaFilesForVideoPlayer(IEnumerable<string> videoMediaFiles, IEnumerable<string> audioMediaFiles, MediaPlayer player, int previewWidth = 0, int previewHeight = 0)
        {
            var mediaComposition = await LoadFilesIntoMediaComposition(videoMediaFiles, audioMediaFiles);
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
    }
}
