using PresentVideoRecorder.ViewModels.ContentPageViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace PresentVideoRecorder.ContentPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RecordPage : Page
    {
        public RecordPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            var viewModel = DataContext as RecordPageViewModel;
            if (viewModel != null)
            {
                viewModel.InitCaptureDeviceWithUIControl(PreviewControl, ScreenPlayer);
                await viewModel.GetAudioProfileSupportedDevicesAsync();
            }
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            var viewModel = DataContext as RecordPageViewModel;
            if (viewModel != null)
            {
                viewModel.SetAppBusyFlag(true);
                await viewModel.Reset();
                viewModel.SetAppBusyFlag(false);
            }
        }
    }
}
