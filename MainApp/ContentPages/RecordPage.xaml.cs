using CaptureEncoder;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Composition;
using PresentVideoRecorder.ViewModels.ContentPageViewModels;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Display;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.System.Display;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
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
                await viewModel.Reset();
            }
        }
    }
}
