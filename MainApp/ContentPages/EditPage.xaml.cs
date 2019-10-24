using Microsoft.Toolkit.Uwp.UI.Controls;
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

        MediaComposition desktopMediaComposition;

        MediaComposition cameraMediaComposition;

        private async void ButtonPlay_Click(object sender, RoutedEventArgs e)
        {
            //CommonFileQuery fileQuery = CommonFileQuery.OrderByName;
            //StorageFile desktopVideoFile = ApplicationData.Current.LocalFolder.GetFilesAsync(CommonFileQuery.OrderByName, )
            desktopMediaComposition = new MediaComposition();
            var desktopVideoFile = _pickedFiles[1];
            var carmeraVideoFile = _pickedFiles[0];
            var videoTask = PreviewAsync(desktopMediaComposition, desktopVideoFile, desktopVideoPlayer);
            var cameraTask = PreviewAsync(cameraMediaComposition, carmeraVideoFile, cameraVideoPlayer);
            //Task.WaitAll( cameraTask);
            
        }

        private async System.Threading.Tasks.Task PreviewAsync(
            MediaComposition mediaComposition, 
            StorageFile mediaFile,
            MediaElement mediaPlayer)
        {
            //CommonFileQuery fileQuery = CommonFileQuery.OrderByName;
            //StorageFile desktopVideoFile = ApplicationData.Current.LocalFolder.GetFilesAsync(CommonFileQuery.OrderByName, )
            mediaComposition = new MediaComposition();
            var desktopVideoFile = mediaFile;
            MediaClip mc = await MediaClip.CreateFromFileAsync(desktopVideoFile);

            durationSelector.Minimum = 0;
            durationSelector.Maximum = (int)mc.OriginalDuration.TotalSeconds;
            durationSelector.StepFrequency = 1;
            //durationSelector.Minimum = 1;

            mediaComposition.Clips.Add(mc);
            var mss = mediaComposition.GeneratePreviewMediaStreamSource(
                Convert.ToInt32(mediaPlayer.ActualWidth),
                Convert.ToInt32(mediaPlayer.ActualHeight));

            mediaPlayer.SetMediaStreamSource(mss);
            mediaPlayer.Pause();
        }

        

        private IReadOnlyList<StorageFile> _pickedFiles;

        private async void ButtonPickButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
            picker.FileTypeFilter.Add(".mp4");
            _pickedFiles = await picker.PickMultipleFilesAsync();
            if (_pickedFiles == null)
            {
                //ShowErrorMessage("File picking cancelled");
                return;
            }

        }

        //private 

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
