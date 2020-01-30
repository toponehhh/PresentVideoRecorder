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
        private IDirect3DDevice _device;
        private Encoder _encoder;
        private const string DESKTOP_VIDEO_FILE_NAME_PREFIX = "DesktopCaptureVideo";

        LowLagMediaRecording _mediaRecording;
        MediaCapture mediaCapture;
        bool isPaused, isMuted;

        DisplayRequest displayRequest = new DisplayRequest();

        private SizeInt32 _lastSize;
        private GraphicsCaptureItem _item;
        private Direct3D11CaptureFramePool _framePool;
        private GraphicsCaptureSession _session;

        // Non-API related members.
        private CanvasDevice _canvasDevice;
        private CompositionGraphicsDevice _compositionGraphicsDevice;
        private Compositor _compositor;
        private CompositionDrawingSurface _surface;

        private StorageFolder _videoSaveFolder;
        private DispatcherTimer _recorderCountTimer;
        private Stopwatch _recordWatch;

        public RecordPage()
        {
            this.InitializeComponent();
            //if (!GraphicsCaptureSession.IsSupported())
            //{
            //    IsEnabled = false;

            //    var dialog = new MessageDialog("Screen capture is not supported on this device for this release of Windows!", "Screen capture unsupported");

            //    var ignored = dialog.ShowAsync();
            //    return;
            //}
            //else
            //{
            //    _device = Direct3D11Helpers.CreateDevice();
            //    InitScreenCapturePreviewArea();
            //}
        }

        private void _recorderCountTimer_Tick(object sender, object e)
        {
            throw new NotImplementedException();
        }

        private async Task StartPreviewAsync()
        {
            try
            {
                mediaCapture = new MediaCapture();
                await mediaCapture.InitializeAsync();

                displayRequest.RequestActive();
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
            }
            catch (UnauthorizedAccessException)
            {
                // This will be thrown if the user denied access to the camera in privacy settings
                var dialog = new MessageDialog("The app was denied access to the camera!", "Screen capture unsupported");
                var ignored = dialog.ShowAsync();
                return;
            }

            try
            {
                PreviewControl.Source = mediaCapture;
                await mediaCapture.StartPreviewAsync();
                //isPreviewing = true;
            }
            catch (System.IO.FileLoadException)
            {
                //mediaCapture.CaptureDeviceExclusiveControlStatusChanged += _mediaCapture_CaptureDeviceExclusiveControlStatusChanged;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var viewModel = DataContext as RecordPageViewModel;
            if (viewModel != null)
            {
                viewModel.InitCaptureDeviceWithUIControl(PreviewControl, ScreenPlayer);
            }
        }

        public async Task GetVideoProfileSupportedDevicesAsync()
        {
            //string deviceId = string.Empty;

            //// Finds all video capture devices
            //DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            //cbCameras.Items.Clear();
            //foreach (var device in devices)
            //{
            //    ComboBoxItem item = new ComboBoxItem();
            //    item.Content = device.Name;
            //    item.IsSelected = true;
            //    cbCameras.Items.Add(item);
            //}
        }
        
        public async Task StartScreenCaptureAsync()
        {
            // The GraphicsCapturePicker follows the same pattern the 
            // file pickers do. 
            var picker = new GraphicsCapturePicker();
           _item = await picker.PickSingleItemAsync();

            // The item may be null if the user dismissed the 
            // control without making a selection or hit Cancel. 
            if (_item != null)
            {
                // We'll define this method later in the document.
                StartCaptureInternal();
            }
        }

        private uint EnsureEven(uint number)
        {
            if (number % 2 == 0)
            {
                return number;
            }
            else
            {
                return number + 1;
            }
        }

        private async void RecordScreenCaptureAsync()
        {
            var frameRate = 30u;
            var quality = VideoEncodingQuality.HD1080p;

            var temp = MediaEncodingProfile.CreateMp4(quality);
            var bitrate = temp.Video.Bitrate;
            var width = temp.Video.Width;
            var height = temp.Video.Height;

            // Use the capture item's size for the encoding if desired

            width = (uint)_item.Size.Width;
            height = (uint)_item.Size.Height;

            // Even if we're using the capture item's real size,
            // we still want to make sure the numbers are even.
            // Some encoders get mad if you give them odd numbers.
            width = EnsureEven(width);
            height = EnsureEven(height);


            // Find a place to put our vidoe for now
            var file = await _videoSaveFolder.CreateFileAsync($"{DESKTOP_VIDEO_FILE_NAME_PREFIX}.mp4", CreationCollisionOption.GenerateUniqueName);

            // Kick off the encoding
            try
            {
                using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                using (_encoder = new Encoder(_device, _item))
                {
                    await _encoder.EncodeAsync(
                        stream,
                        width, height, bitrate,
                        frameRate);
                }
            }
            catch (Exception ex)
            {
                var dialog = new MessageDialog(
                    $"Uh-oh! Something went wrong!\n0x{ex.HResult:X8} - {ex.Message}",
                    "Recording failed");

                await dialog.ShowAsync();
            }
        }

        private void InitScreenCapturePreviewArea()
        {
            _canvasDevice = new CanvasDevice();
            _compositionGraphicsDevice = CanvasComposition.CreateCompositionGraphicsDevice(Window.Current.Compositor, _canvasDevice);
            _compositor = Window.Current.Compositor;

            _surface = _compositionGraphicsDevice.CreateDrawingSurface(
                new Size(640, 480),
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                DirectXAlphaMode.Premultiplied);    // This is the only value that currently works with the composition APIs.

            var visual = _compositor.CreateSpriteVisual();
            visual.RelativeSizeAdjustment = Vector2.One;
            var brush = _compositor.CreateSurfaceBrush(_surface);
            brush.HorizontalAlignmentRatio = 0.5f;
            brush.VerticalAlignmentRatio = 0.5f;
            brush.Stretch = CompositionStretch.Uniform;
            visual.Brush = brush;
            ElementCompositionPreview.SetElementChildVisual(ScreenPlayer, visual);
        }

        private void StartCaptureInternal()
        {
            // Stop the previous capture if we had one.
            //StopCapture();

            _lastSize = _item.Size;

            _framePool = Direct3D11CaptureFramePool.Create(
               _canvasDevice, // D3D device 
               DirectXPixelFormat.B8G8R8A8UIntNormalized, // Pixel format 
               2, // Number of frames 
               _item.Size); // Size of the buffers 

            _framePool.FrameArrived += (s, a) =>
            { 
                using (var frame = _framePool.TryGetNextFrame())
                {
                    ProcessFrame(frame);
                }
            };

            _item.Closed += (s, a) =>
            {
                StopCapture();
            };

            _session = _framePool.CreateCaptureSession(_item);
            _session.StartCapture();
        }

        private void ProcessFrame(Direct3D11CaptureFrame frame)
        {
            // Resize and device-lost leverage the same function on the
            // Direct3D11CaptureFramePool. Refactoring it this way avoids 
            // throwing in the catch block below (device creation could always 
            // fail) along with ensuring that resize completes successfully and 
            // isn’t vulnerable to device-lost.   
            bool needsReset = false;
            bool recreateDevice = false;

            if ((frame.ContentSize.Width != _lastSize.Width) ||
                (frame.ContentSize.Height != _lastSize.Height))
            {
                needsReset = true;
                _lastSize = frame.ContentSize;
            }

            try
            {
                if (!isPaused)
                {

                    var canvasBitmap = CanvasBitmap.CreateFromDirect3D11Surface(
                    _canvasDevice,
                    frame.Surface);

                    CanvasRenderTarget renderTarget = null;
                    renderTarget = new CanvasRenderTarget(_canvasDevice, canvasBitmap.SizeInPixels.Width, canvasBitmap.SizeInPixels.Height, 96);
                    using (CanvasDrawingSession ds = renderTarget.CreateDrawingSession())
                    {
                        ds.Clear(Colors.Black);
                        ds.DrawImage(canvasBitmap);
                    }
                    

                    //MediaClip mediaClip = MediaClip.CreateFromSurface(renderTarget, TimeSpan.FromMilliseconds(captureInterval));
                    //mediaComposition.Clips.Add(mediaClip);

                    //Helper that handles the drawing for us.
                    FillSurfaceWithBitmap(canvasBitmap);
                }
            }

            // This is the device-lost convention for Win2D.
            catch (Exception e) when (_canvasDevice.IsDeviceLost(e.HResult))
            {
                // We lost our graphics device. Recreate it and reset 
                // our Direct3D11CaptureFramePool.  
                needsReset = true;
                recreateDevice = true;
            }

            if (needsReset)
            {
                ResetFramePool(frame.ContentSize, recreateDevice);
            }
        }

        private void FillSurfaceWithBitmap(CanvasBitmap canvasBitmap)
        {
            CanvasComposition.Resize(_surface, canvasBitmap.Size);

            using (var session = CanvasComposition.CreateDrawingSession(_surface))
            {
                session.Clear(Colors.Transparent);
                session.DrawImage(canvasBitmap);
            }
        }

        private void ResetFramePool(SizeInt32 size, bool recreateDevice)
        {
            do
            {
                try
                {
                    if (recreateDevice)
                    {
                        _canvasDevice = new CanvasDevice();
                    }

                    _framePool.Recreate(
                        _canvasDevice,
                        DirectXPixelFormat.B8G8R8A8UIntNormalized,
                        2,
                        size);
                }
                // This is the device-lost convention for Win2D.
                catch (Exception e) when (_canvasDevice.IsDeviceLost(e.HResult))
                {
                    _canvasDevice = null;
                    recreateDevice = true;
                }
            } while (_canvasDevice == null);
        }

        /*
        private async void SaveFile(string fileName = null, StorageFolder folder = null)
        {
            //if (string.IsNullOrWhiteSpace(fileName))
            //{
            //    //string fileNameSuffix = DateTime.Now.ToString("yyyyMMdd_HH_mm_ss_fff");
            //    fileName = $"DesktopCaptureVideo.mp4";
            //}

            //StorageFile file;
            //if (null == folder)
            //{
            //    var myVideos = await Windows.Storage.StorageLibrary.GetLibraryAsync(Windows.Storage.KnownLibraryId.Videos);
            //    file = await myVideos.SaveFolder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);
            //}
            //else
            //{
            //    file = await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            //}

            //var saveOperation = mediaComposition.RenderToFileAsync(file,MediaTrimmingPreference.Fast);
            //await saveOperation;
        }
        */


        public void StopCapture()
        {
            _session?.Dispose();
            _framePool?.Dispose();
            _item = null;
            _session = null;
            _framePool = null;
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            //if (_videoSaveFolder != null)
            //{
            //    StorageFile file = await _videoSaveFolder.CreateFileAsync("CameraVideo.mp4", CreationCollisionOption.GenerateUniqueName);
            //    _mediaRecording = await mediaCapture.PrepareLowLagRecordToStorageFileAsync(MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto), file);
            //    await _mediaRecording.StartAsync();
            //    RecordScreenCaptureAsync();
            //    _recordWatch = new Stopwatch();
            //    _recorderCountTimer = new DispatcherTimer();
            //    _recorderCountTimer.Interval = TimeSpan.FromSeconds(1d);
            //    _recorderCountTimer.Tick += (obj, et) => 
            //    {
            //        if (_recordWatch.IsRunning)
            //        {
            //            //txbCount.Text = _recordWatch.Elapsed.ToString(@"hh\:mm\:ss");
            //        }
            //    };
                
            //    _recorderCountTimer.Start();
            //    _recordWatch.Start();

            //    btnStart.IsEnabled = false;
            //    btnStop.IsEnabled = btnPause.IsEnabled = true;
            //}
        }

        private async void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            //_encoder?.Dispose();
            //await _mediaRecording.StopAsync();
            //var action = _mediaRecording.FinishAsync();
            //action.Completed += async (a, s) =>
            //{
            //    await Dispatcher.TryRunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => 
            //    {
            //        _recorderCountTimer.Stop();

            //        btnStart.IsEnabled = true;
            //        btnStop.IsEnabled = btnPause.IsEnabled = false;
            //        LoadingControl.IsLoading = false;
            //    });
            //};
            //LoadingControl.IsLoading = true;
        }

        private async void ChbCamera_Click(object sender, RoutedEventArgs e)
        {
            //if (chbCamera.IsChecked.HasValue && chbCamera.IsChecked.Value)
            //{
            //    if (mediaCapture == null)
            //    {
            //        await StartPreviewAsync();
            //    }
            //    else
            //    {
            //        await mediaCapture.StartPreviewAsync();
            //    }
            //}
            //else
            //{
            //    await mediaCapture.StopPreviewAsync();
            //}
        }

        private async void ChbScreen_Click(object sender, RoutedEventArgs e)
        {
            //if(chbScreen.IsChecked.HasValue && chbScreen.IsChecked.Value)
            //{
            //    await StartScreenCaptureAsync();
            //}
            //else
            //{
            //    StopCapture();
            //}
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            //StopCapture();
            //if (mediaCapture != null)
            //{
            //    await mediaCapture?.StopPreviewAsync();
            //}
            //base.OnNavigatingFrom(e);
        }

        private async void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            if(!isPaused)
            {
                tbPauseContent.Text = "Resume";
                await _mediaRecording.PauseAsync(Windows.Media.Devices.MediaCapturePauseBehavior.RetainHardwareResources);
            }
            else
            {
                tbPauseContent.Text = "Pause";
                await _mediaRecording.ResumeAsync();
            }
            isPaused = !isPaused;
        }

        private async void btnSavePath_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
            folderPicker.FileTypeFilter.Add("*");

            Windows.Storage.StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                // Application now has read/write access to all contents in the picked folder
                // (including other sub-folder contents)
                Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);
                this.txtSavePath.Text = folder.Path;
                _videoSaveFolder = folder;
            }
            else
            {
                this.txtSavePath.Text = string.Empty;
            }
        }

        private async void BtnChangeCapureSource_Click(object sender, RoutedEventArgs e)
        {
            StopCapture();
            await StartScreenCaptureAsync();
        }

        private void BtnMute_Click(object sender, RoutedEventArgs e)
        {
            if(!isMuted)
            {
                tbMuteContent.Text = "UnMute";
            }
            else
            {
                tbMuteContent.Text = "Mute";
            }
            isMuted = !isMuted;
            mediaCapture.AudioDeviceController.Muted = isMuted;
        }
    }
}
