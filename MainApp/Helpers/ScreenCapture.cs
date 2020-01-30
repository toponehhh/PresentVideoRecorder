using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Composition;
using System;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;

namespace PresentVideoRecorder.Helpers
{
    public class ScreenCapture
    {
        private CanvasDevice _canvasDevice;
        private CompositionGraphicsDevice _compositionGraphicsDevice;
        private Compositor _compositor;
        private CompositionDrawingSurface _surface;

        private SizeInt32 _lastSize;
        private GraphicsCaptureItem _item;
        private Direct3D11CaptureFramePool _framePool;
        private GraphicsCaptureSession _session;

        public ScreenCapture(UIElement shower)
        {
            initScreenCapturePreviewArea(shower);
        }

        private void initScreenCapturePreviewArea(UIElement graphicShower)
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
            ElementCompositionPreview.SetElementChildVisual(graphicShower, visual);
        }

        private void startCaptureInternal()
        {
            _lastSize = _item.Size;

            _framePool = Direct3D11CaptureFramePool.Create(
               _canvasDevice, // D3D device 
               DirectXPixelFormat.B8G8R8A8UIntNormalized, // Pixel format 
               1, // Number of frames 
               _item.Size); // Size of the buffers 

            _framePool.FrameArrived += (s, a) =>
            {
                using (var frame = _framePool.TryGetNextFrame())
                {
                    processFrame(frame);
                }
            };

            _item.Closed += (s, a) =>
            {
                StopScreenCapturePreview();
            };

            _session = _framePool.CreateCaptureSession(_item);
            _session.StartCapture();
        }

        private void processFrame(Direct3D11CaptureFrame frame)
        {
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
                var canvasBitmap = CanvasBitmap.CreateFromDirect3D11Surface(_canvasDevice, frame.Surface);

                CanvasRenderTarget renderTarget = null;
                renderTarget = new CanvasRenderTarget(_canvasDevice, canvasBitmap.SizeInPixels.Width, canvasBitmap.SizeInPixels.Height, 96);
                using (CanvasDrawingSession ds = renderTarget.CreateDrawingSession())
                {
                    ds.Clear(Colors.Black);
                    ds.DrawImage(canvasBitmap);
                }
                //Helper that handles the drawing for us.
                fillSurfaceWithBitmap(canvasBitmap);
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
                resetFramePool(frame.ContentSize, recreateDevice);
            }
        }

        private void fillSurfaceWithBitmap(CanvasBitmap canvasBitmap)
        {
            CanvasComposition.Resize(_surface, canvasBitmap.Size);

            using (var session = CanvasComposition.CreateDrawingSession(_surface))
            {
                session.Clear(Colors.Transparent);
                session.DrawImage(canvasBitmap);
            }
        }

        private void resetFramePool(SizeInt32 size, bool recreateDevice)
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
                        1,
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

        public void StopScreenCapturePreview()
        {
            using (var session = CanvasComposition.CreateDrawingSession(_surface))
            {
                session.Clear(Colors.Transparent);
            }
            _session?.Dispose();
            _framePool?.Dispose();
            _item = null;
            _session = null;
            _framePool = null;
        }

        public void ShowScreenCapturePreviewAsync(GraphicsCaptureItem captureItem)
        {
            _item = captureItem;
            if (_item != null)
            {
                // We'll define this method later in the document.
                startCaptureInternal();
            }
        }
    }
}
