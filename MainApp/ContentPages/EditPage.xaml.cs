using Microsoft.Toolkit.Uwp.UI.Controls;
using PresentVideoRecorder.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Editing;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace PresentVideoRecorder.ContentPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class EditPage : Page
    {
        public EditPage()
        {
            this.InitializeComponent();
        }

        private MediaComposition desktopMediaComposition, cameraMediaComposition;
        private StorageFile cameraVideoFile, desktopVideoFile;

        private void ButtonPlay_Click(object sender, RoutedEventArgs e)
        {
            //PreviewAsync(desktopMediaComposition, desktopVideoFile, desktopVideoPlayer);
            //PreviewAsync(cameraMediaComposition, cameraVideoFile, cameraVideoPlayer);
        }

        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            desktopVideoPlayer.Stop();
            cameraVideoPlayer.Stop();
        }

        private async void PreviewAsync(
            MediaComposition mediaComposition, 
            StorageFile mediaFile,
            MediaElement mediaPlayer)
        {
            mediaComposition = new MediaComposition();
            MediaClip mc = await MediaClip.CreateFromFileAsync(mediaFile);

            durationSelector.Minimum = 0;
            durationSelector.Maximum = (int)mc.OriginalDuration.TotalSeconds;
            durationSelector.StepFrequency = 1;
            //durationSelector.Minimum = 1;

            mediaComposition.Clips.Add(mc);
            var mss = mediaComposition.GeneratePreviewMediaStreamSource(
                Convert.ToInt32(mediaPlayer.ActualWidth),
                Convert.ToInt32(mediaPlayer.ActualHeight));

            mediaPlayer.SetMediaStreamSource(mss);
            //mediaPlayer.Pause();
        }

        private async void ButtonPickButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FolderPicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
            picker.FileTypeFilter.Add("*");
            var pickedFolder = await picker.PickSingleFolderAsync();
            
            if (pickedFolder != null)
            {
                Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", pickedFolder);
                txtVideoPath.Text = pickedFolder.Path;

                var courseFile=await pickedFolder.GetFileAsync("Course.json");
                var courseData = await Course.LoadFromFile(courseFile.Path);
            }
        }


        private void DurationSelector_ValueChanged(object sender, Microsoft.Toolkit.Uwp.UI.Controls.RangeChangedEventArgs e)
        {
            var selectedDuration = 
                TimeSpan.FromSeconds(durationSelector.RangeMax - durationSelector.RangeMin);
            
        }

        private void ButtonReserveClip_Click(object sender, RoutedEventArgs e)
        {
            TrimAllButCurrentRange(desktopMediaComposition, durationSelector);
        }


        private void TrimAllButCurrentRange(MediaComposition composition, RangeSelector rangeSelector)
        {
            var startPosition = TimeSpan.FromSeconds((int)rangeSelector.RangeMin);
            var endPosition = TimeSpan.FromSeconds((int)rangeSelector.RangeMax);
            var currentClip = composition.Clips.FirstOrDefault(
                mc => mc.StartTimeInComposition <= startPosition &&
                mc.EndTimeInComposition >= endPosition);

            TimeSpan positionFromStart = startPosition - currentClip.StartTimeInComposition;
            currentClip.TrimTimeFromStart = positionFromStart;
            currentClip.TrimTimeFromEnd = currentClip.EndTimeInComposition - endPosition;
            //currentClip.
        }

        private void TrimClipBeforeCurrentRange(MediaComposition composition, RangeSelector rangeSelector)
        {
            var startPosition = TimeSpan.FromSeconds((int)rangeSelector.RangeMin);
            var endPosition = TimeSpan.FromSeconds((int)rangeSelector.RangeMax);
            var currentClip = composition.Clips.FirstOrDefault(
                mc => mc.StartTimeInComposition <= startPosition &&
                mc.EndTimeInComposition >= endPosition);

            TimeSpan positionFromStart = startPosition - currentClip.StartTimeInComposition;
            currentClip.TrimTimeFromStart = positionFromStart;

        }


        private void TrimClipAfterCurrentRange(MediaComposition composition, RangeSelector rangeSelector)
        {
            var startPosition = TimeSpan.FromSeconds((int)rangeSelector.RangeMin);
            var endPosition = TimeSpan.FromSeconds((int)rangeSelector.RangeMax);
            var currentClip = composition.Clips.FirstOrDefault(
                mc => mc.StartTimeInComposition <= startPosition &&
                mc.EndTimeInComposition >= endPosition);

            TimeSpan positionFromStart = startPosition - currentClip.StartTimeInComposition;
            currentClip.TrimTimeFromStart = positionFromStart;

        }
    }
}
