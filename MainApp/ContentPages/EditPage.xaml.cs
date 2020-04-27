using Microsoft.Toolkit.Uwp.UI.Controls;
using PresentVideoRecorder.ViewModels.ContentPageViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Editing;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
        }

        private void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            cameraVideoPlayer.IsFullWindow = false;
            screenVideoPlayer.IsFullWindow = false;
            ApplicationView.GetForCurrentView().ExitFullScreenMode();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var viewModel = DataContext as EditPageViewModel;
            if (viewModel != null)
            {
                LoadingControl.IsLoading = true;
                viewModel.InitMediaPlayers(cameraVideoPlayer, screenVideoPlayer);
                while (viewModel.IsAppInBusyStatus) await Task.Delay(1000);
                await viewModel.LoadCourseData();
                await Task.Delay(3000);
                LoadingControl.IsLoading = false;
            }
        }

        protected async override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            var viewModel = DataContext as EditPageViewModel;
            if (viewModel != null)
            {
                await viewModel.Reset();
            }
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

        private void btnSetCameraVideoFull_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
            cameraVideoPlayer.IsFullWindow = true;
        }

        private void btnSetScreenVideoFull_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
            screenVideoPlayer.IsFullWindow = true;
        }

    }
}
