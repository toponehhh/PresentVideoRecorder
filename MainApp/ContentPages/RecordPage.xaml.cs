using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Composition;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.Display;
using Windows.Media.Capture;
using Windows.Media.Core;
using Windows.Media.Editing;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Display;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace PresentVideoRecorder.ContentPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RecordPage : Page
    {
        LowLagMediaRecording _mediaRecording;
        MediaCapture mediaCapture;
        bool isPaused, isMuted, isPreviewing;

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


        private DateTime startTime;
        private System.Timers.Timer captureTimer;

        private object sampleLocker=new object();
        ManualResetEvent sampleArrived = new ManualResetEvent(false);
        int captureInterval = 42; //milliseconds, namely 40 frames per second.

        MediaComposition mediaComposition;

        public RecordPage()
        {
            this.InitializeComponent();
            Setup();
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
                Debug.WriteLine("The app was denied access to the camera");
                return;
            }

            try
            {
                PreviewControl.Source = mediaCapture;
                await mediaCapture.StartPreviewAsync();
                isPreviewing = true;
            }
            catch (System.IO.FileLoadException)
            {
                //mediaCapture.CaptureDeviceExclusiveControlStatusChanged += _mediaCapture_CaptureDeviceExclusiveControlStatusChanged;
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await StartPreviewAsync();
            await GetVideoProfileSupportedDevicesAsync();
        }

        public async Task GetVideoProfileSupportedDevicesAsync()
        {
            string deviceId = string.Empty;

            // Finds all video capture devices
            DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            cbCameras.Items.Clear();
            foreach (var device in devices)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = device.Name;
                item.IsSelected = true;
                cbCameras.Items.Add(item);
            }
        }

        
        public async Task StartCaptureAsync()
        {
            // The GraphicsCapturePicker follows the same pattern the 
            // file pickers do. 
            var picker = new GraphicsCapturePicker();
            GraphicsCaptureItem item = await picker.PickSingleItemAsync();
            //GraphicsCaptureItem item = GraphicsCaptureItem.CreateFromVisual(Window.Current.CoreWindow.Bounds);

            // The item may be null if the user dismissed the 
            // control without making a selection or hit Cancel. 
            if (item != null)
            {
                // We'll define this method later in the document.
                StartCaptureInternal(item);
            }
        }

        private void CaptureTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            sampleArrived.Set();
        }

        private void Setup()
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

        private void StartCaptureInternal(GraphicsCaptureItem item)
        {
            // Stop the previous capture if we had one.
            StopCapture();

            if (null == captureTimer)
            {
                captureTimer = new System.Timers.Timer(captureInterval);
                captureTimer.Elapsed += CaptureTimer_Elapsed;
            }

            mediaComposition = new MediaComposition();
            _item = item;
            _lastSize = _item.Size;

            _framePool = Direct3D11CaptureFramePool.Create(
               _canvasDevice, // D3D device 
               DirectXPixelFormat.B8G8R8A8UIntNormalized, // Pixel format 
               2, // Number of frames 
               _item.Size); // Size of the buffers 

            if (!captureTimer.Enabled)
            {
                captureTimer.Start();
            }

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
            //startTime = DateTime.Now;
            _session.StartCapture();
        }

        private async void ProcessFrame(Direct3D11CaptureFrame frame)
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
                if (!isPaused && sampleArrived.WaitOne(5))
                //if (!isPaused)
                {

                    var canvasBitmap = CanvasBitmap.CreateFromDirect3D11Surface(
                    _canvasDevice,
                    frame.Surface);

                    //byte[] buff = new byte[canvasBitmap.SizeInPixels.Height * canvasBitmap.SizeInPixels.Width * 4];

                    //IBuffer pixels = buff.AsBuffer();
                    //canvasBitmap.GetPixelBytes(pixels);
                    //var stream = new MemoryStream().AsRandomAccessStream();
                    CanvasRenderTarget renderTarget = null;
                    renderTarget = new CanvasRenderTarget(_canvasDevice, canvasBitmap.SizeInPixels.Width, canvasBitmap.SizeInPixels.Height, 96);
                    using (CanvasDrawingSession ds = renderTarget.CreateDrawingSession())
                    {
                        ds.Clear(Colors.Black);
                        ds.DrawImage(canvasBitmap);
                    }
                    

                    MediaClip mediaClip = MediaClip.CreateFromSurface(renderTarget, TimeSpan.FromMilliseconds(captureInterval));
                    mediaComposition.Clips.Add(mediaClip);


                    //CreateVideoFromWritableBitmapAsync(pixels, (int)canvasBitmap.SizeInPixels.Width, (int)canvasBitmap.SizeInPixels.Height, TimeSpan.FromMilliseconds(captureInterval), null);


                    //Helper that handles the drawing for us.
                    FillSurfaceWithBitmap(canvasBitmap);


                    //var can2 = CanvasBitmap.CreateFromDirect3D11Surface(
                    //_canvasDevice,
                    //frame.Surface);
                    //var clip = MediaClip.CreateFromSurface(frame.Surface, TimeSpan.FromMilliseconds(captureInterval));
                    //mediaComposition.Clips.Add(clip);
                    sampleArrived.Reset();
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

        private void CreateVideoFromWritableBitmapAsync(IBuffer bitmap,
            int widthInPixels, int heightInPixels,
            TimeSpan originalDuration,
            List<WriteableBitmap> WBs)
        {
            //var bb = CanvasBitmap.CreateFromBytes(_canvasDevice,
            //    bitmap, widthInPixels, heightInPixels, DirectXPixelFormat.B8G8R8A8UIntNormalized);

            CanvasRenderTarget renderTarget = null;
            using (CanvasBitmap canvas = CanvasBitmap.CreateFromBytes(_canvasDevice, bitmap, widthInPixels, heightInPixels, DirectXPixelFormat.B8G8R8A8UIntNormalized))
            {
                renderTarget = new CanvasRenderTarget(_canvasDevice, canvas.SizeInPixels.Width, canvas.SizeInPixels.Height, 96);
                using (CanvasDrawingSession ds = renderTarget.CreateDrawingSession())
                {
                    ds.Clear(Colors.Black);
                    ds.DrawImage(canvas);
                }
            }

            MediaClip mediaClip = MediaClip.CreateFromSurface(renderTarget, originalDuration);
            mediaComposition.Clips.Add(mediaClip);
        }

        const string DESKTOP_VIDEO_FILE_NAME_PREFIX = "DesktopCaptureVideo";

        private async void SaveFile(string fileName = null, StorageFolder folder = null)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                //string fileNameSuffix = DateTime.Now.ToString("yyyyMMdd_HH_mm_ss_fff");
                fileName = $"DesktopCaptureVideo.mp4";
            }

            StorageFile file;
            if (null == folder)
            {
                var myVideos = await Windows.Storage.StorageLibrary.GetLibraryAsync(Windows.Storage.KnownLibraryId.Videos);
                file = await myVideos.SaveFolder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);
            }
            else
            {
                file = await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            }

            var saveOperation = mediaComposition.RenderToFileAsync(file,MediaTrimmingPreference.Fast);
            await saveOperation;
        }


        public void StopCapture()
        {
            captureTimer?.Stop();
            _session?.Dispose();
            _framePool?.Dispose();
            _item = null;
            _session = null;
            _framePool = null;
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            var myVideos = await Windows.Storage.StorageLibrary.GetLibraryAsync(Windows.Storage.KnownLibraryId.Videos);
            StorageFile file = await myVideos.SaveFolder.CreateFileAsync("camera.mp4", CreationCollisionOption.GenerateUniqueName);
            _mediaRecording = await mediaCapture.PrepareLowLagRecordToStorageFileAsync(MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto), file);
            await _mediaRecording.StartAsync();
            btnStart.IsEnabled = false;
            btnStop.IsEnabled = btnPause.IsEnabled = true;
        }

        private async void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            var action = _mediaRecording.FinishAsync();
            action.Completed += async (a, s) =>
            {
                StopCapture();
                SaveFile();
                await Dispatcher.TryRunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => 
                {
                    btnStart.IsEnabled = true;
                    btnStop.IsEnabled = btnPause.IsEnabled = false;
                    LoadingControl.IsLoading = false;
                });
            };
            LoadingControl.IsLoading = true;
        }

        private async void ChbCamera_Click(object sender, RoutedEventArgs e)
        {
            if(chbCamera.IsChecked.HasValue && chbCamera.IsChecked.Value)
            {
                await mediaCapture.StartPreviewAsync();
            }
            else
            {
                await mediaCapture.StopPreviewAsync();
            }
        }

        private async void ChbScreen_Click(object sender, RoutedEventArgs e)
        {
            if(chbScreen.IsChecked.HasValue && chbScreen.IsChecked.Value)
            {
                await StartCaptureAsync();
            }
            else
            {
                StopCapture();
            }
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            StopCapture();
            await mediaCapture.StopPreviewAsync();
            base.OnNavigatingFrom(e);
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

        private async void BtnChangeCapureSource_Click(object sender, RoutedEventArgs e)
        {
            await StartCaptureAsync();
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
