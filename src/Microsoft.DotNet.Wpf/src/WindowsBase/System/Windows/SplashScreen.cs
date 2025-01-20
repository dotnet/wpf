// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using MS.Internal;
using MS.Internal.KnownBoxes;
using MS.Win32;
using MS.Internal.WindowsBase;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.Graphics.Imaging;
using Windows.Win32.System.Com;
using Windows.Win32.UI.WindowsAndMessaging;

namespace System.Windows
{
    public class SplashScreen
    {
        private HWND _hwnd = HWND.Null;
        private readonly string _resourceName;
        private readonly HINSTANCE _hInstance;
        private HBITMAP _hBitmap;
        private ushort _windowClass;
        private DispatcherTimer _timer;
        private TimeSpan _fadeoutDuration;
        private DateTime _fadeoutEnd;
        private BLENDFUNCTION _blendFunction;
        private readonly ResourceManager _resourceManager;
        private Dispatcher _dispatcher;

        private const string ClassName = "SplashScreen";

        public SplashScreen(string resourceName) : this(Assembly.GetEntryAssembly(), resourceName)
        {
        }

        public SplashScreen(Assembly resourceAssembly, string resourceName)
        {
            ArgumentNullException.ThrowIfNull(resourceAssembly);
            if (string.IsNullOrEmpty(resourceName))
            {
                throw new ArgumentNullException(nameof(resourceName));
            }

            _resourceName = resourceName.ToLowerInvariant();
            _hInstance = (HINSTANCE)Marshal.GetHINSTANCE(resourceAssembly.ManifestModule);
            _resourceManager = new ResourceManager($"{ReflectionUtils.GetAssemblyPartialName(resourceAssembly)}.g", resourceAssembly);
        }

        public void Show(bool autoClose)
        {
            Show(autoClose, topMost: false);
        }

        public unsafe void Show(bool autoClose, bool topMost)
        {
            if (!_hwnd.IsNull)
            {
                return;
            }

            // If we've already been shown it isn't an error to call show
            // again (maybe you forgot) since you will still be shown state.

            using UnmanagedMemoryStream resourceStream = GetResourceStream()
                ?? throw new IOException(SR.Format(SR.UnableToLocateResource, _resourceName));

            resourceStream.Seek(0, SeekOrigin.Begin); // ensure stream position

            CreateLayeredWindowFromImgBuffer(new(resourceStream.PositionPointer, (int)resourceStream.Length), topMost);

            if (autoClose)
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(
                    DispatcherPriority.Loaded,
                    (DispatcherOperationCallback)(static arg =>
                    {
                        ((SplashScreen)arg).Close(TimeSpan.FromSeconds(0.3));
                        return null;
                    }),
                    this);
            }

            // The HWND that we just created is owned by this thread.  When we close we should ensure that it 
            // does not get accessed from a different thread.  We don't want to reference the .CurrentDispatcher
            // property before the window is created due to cold start performance concerns.
            _dispatcher = Dispatcher.CurrentDispatcher;
        }

        // This is 200-300 ms slower than Assembly.GetManifestResourceStream() but works with localization.
        private UnmanagedMemoryStream GetResourceStream()
        {
            // Try to get the stream with the string the developer supplied, in the app.g.cs case
            // this will always work.
            UnmanagedMemoryStream stream = _resourceManager.GetStream(_resourceName, CultureInfo.CurrentUICulture);
            if (stream is not null)
            {
                return stream;
            }

            // IF that fails then the resource name had special characters in it which would not
            // be encoded literally into the resource stream.  Unfortunately we need to rely on the
            // slow URI class to get the correct name.  We try to avoid doing this in the common case
            // since URI has quite a bit of code associated with it.
            string resourceName = ResourceIDHelper.GetResourceIDFromRelativePath(_resourceName);
            return _resourceManager.GetStream(resourceName, CultureInfo.CurrentUICulture);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static LRESULT WndProc(HWND hWnd, uint Msg, WPARAM wParam, LPARAM lParam) =>
            PInvoke.DefWindowProc(hWnd, Msg, wParam, lParam);

        private unsafe HWND CreateWindow(HBITMAP hBitmap, int width, int height, bool topMost)
        {
            fixed (char* className = ClassName)
            {
                WNDCLASSEXW wndClass = new()
                {
                    cbSize = (uint)sizeof(WNDCLASSEXW),
                    style = WNDCLASS_STYLES.CS_HREDRAW | WNDCLASS_STYLES.CS_VREDRAW,
                    lpszClassName = className,
                    lpfnWndProc = &WndProc,
                    hInstance = _hInstance
                };

                // We chose to ignore re-registration errors in RegisterClassEx on the off chance that the user
                // wants to open multiple splash screens.
                _windowClass = PInvoke.RegisterClassEx(wndClass);
            }

            if (_windowClass == 0)
            {
                int lastWin32Error = Marshal.GetLastWin32Error();
                if (lastWin32Error != (int)WIN32_ERROR.ERROR_CLASS_ALREADY_EXISTS)
                {
                    throw new Win32Exception(lastWin32Error);
                }
            }

            int screenWidth = PInvokeCore.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSCREEN);
            int screenHeight = PInvokeCore.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSCREEN);
            int x = (screenWidth - width) / 2;
            int y = (screenHeight - height) / 2;

            WINDOW_EX_STYLE windowCreateFlags = WINDOW_EX_STYLE.WS_EX_WINDOWEDGE
                | WINDOW_EX_STYLE.WS_EX_TOOLWINDOW
                | WINDOW_EX_STYLE.WS_EX_LAYERED
                | (topMost ? WINDOW_EX_STYLE.WS_EX_TOPMOST : 0);

            // CreateWindowEx will either succeed or throw
            HWND hwnd = PInvoke.CreateWindowEx(
                windowCreateFlags,
                ClassName,
                SR.SplashScreenIsLoading,
                WINDOW_STYLE.WS_POPUP | WINDOW_STYLE.WS_VISIBLE,
                x, y, width, height,
                HWND.Null,
                HMENU.Null,
                _hInstance,
                null);

            // Display the image on the window
            using GetDcScope screenContext = GetDcScope.ScreenDC;
            using CreateDcScope memoryContext = new(screenContext);
            using SelectObjectScope selectObjectScope = new(memoryContext, hBitmap);

            BLENDFUNCTION blendFunction = new()
            {
                BlendOp = (byte)PInvoke.AC_SRC_OVER,
                SourceConstantAlpha = 255,
                AlphaFormat = (byte)PInvoke.AC_SRC_ALPHA
            };

            if (!PInvoke.UpdateLayeredWindow(
                hwnd,
                screenContext,
                new(x, y),
                new(width, height),
                memoryContext,
                new(0, 0),
                default,
                blendFunction,
                UPDATE_LAYERED_WINDOW_FLAGS.ULW_ALPHA))
            {
                ((HRESULT)Marshal.GetHRForLastWin32Error()).ThrowOnFailure();
            }

            return hwnd;
        }

        public void Close(TimeSpan fadeoutDuration)
        {
            object result = null;
            if (_dispatcher is not null)
            {
                if (_dispatcher.CheckAccess())
                {
                    result = CloseInternal(fadeoutDuration);
                }
                else
                {
                    result = _dispatcher.Invoke(DispatcherPriority.Normal, (DispatcherOperationCallback)CloseInternal, fadeoutDuration);
                }
            }

            if (result != BooleanBoxes.TrueBox)
            {
                // If all else fails try to destroy the resources on this thread
                // this will probably end up throwing but it will be the same
                // exception as the previous version.
                DestroyResources();
            }
        }

        private object CloseInternal(object fadeOutArg)
        {
            TimeSpan fadeoutDuration = (TimeSpan)fadeOutArg;
            if (fadeoutDuration <= TimeSpan.Zero)
            {
                DestroyResources();
                return BooleanBoxes.TrueBox;
            }

            // In the case where the developer has specified AutoClose=True and then calls
            // Close(non_zero_timespan) before the auto close operation is dispatched we begin
            // the fadeout immidiately and ignore the later call to close.
            if (_timer is not null || _hwnd.IsNull)
            {
                return BooleanBoxes.TrueBox;
            }

            // By default close gets called as soon as the first application window is created
            // since it will have become the active window we need to steal back the active window
            // status so that the fade out animation is visible.
            HWND previousWindow = PInvoke.SetActiveWindow(_hwnd);
            if (previousWindow.IsNull)
            {
                // SetActiveWindow fails (returns NULL) if the application is not in the foreground.
                // If this is the case, don't bother animating the fade out.
                DestroyResources();
                return BooleanBoxes.TrueBox;
            }

            _timer = new DispatcherTimer
            {
                // Shoot for ~30 fps
                Interval = TimeSpan.FromMilliseconds(30)
            };

            _fadeoutDuration = fadeoutDuration;
            _fadeoutEnd = DateTime.UtcNow + _fadeoutDuration;
            _timer.Tick += Fadeout_Tick;
            _timer.Start();

            return BooleanBoxes.TrueBox;
        }

        private void Fadeout_Tick(object unused, EventArgs args)
        {
            DateTime now = DateTime.UtcNow;
            if (now >= _fadeoutEnd)
            {
                DestroyResources();
            }
            else
            {
                double progress = (_fadeoutEnd - now).TotalMilliseconds / _fadeoutDuration.TotalMilliseconds;
                _blendFunction.SourceConstantAlpha = (byte)(255 * progress);
                PInvoke.UpdateLayeredWindow(
                    _hwnd,
                    HDC.Null,
                    null,
                    null,
                    HDC.Null,
                    null,
                    default,
                    _blendFunction,
                    UPDATE_LAYERED_WINDOW_FLAGS.ULW_ALPHA);
            }
        }

        private unsafe void DestroyResources(bool finalizer = false)
        {
            if (!finalizer)
            {
                _timer?.Stop();
                _timer = null;
            }

            if (!_hwnd.IsNull)
            {
                if (PInvoke.IsWindow(_hwnd))
                {
                    PInvoke.DestroyWindow(_hwnd);
                }

                _hwnd = HWND.Null;
            }

            if (!_hBitmap.IsNull)
            {
                PInvokeCore.DeleteObject(_hBitmap);
                _hBitmap = HBITMAP.Null;
            }

            if (_windowClass != 0)
            {
                // Attempt to unregister the window class.  If the application has a second
                // splash screen which is still open this call will fail.  That's OK.
                PInvoke.UnregisterClass((PCWSTR)(char*)(nint)_windowClass, _hInstance);
                _windowClass = 0;
            }

            // Mark this so it doesn't get finalized and try to release resources again.
            GC.SuppressFinalize(this);

            if (!finalizer)
            {
                _resourceManager?.ReleaseAllResources();
            }
        }

        private unsafe void CreateLayeredWindowFromImgBuffer(Span<byte> buffer, bool topMost)
        {
            try
            {
                PInvoke.CoInitialize().ThrowOnFailure();
                PInvokeCore.CoCreateInstance(
                    PInvoke.CLSID_WICImagingFactory,
                    null,
                    CLSCTX.CLSCTX_INPROC_SERVER,
                    out IWICImagingFactory* pImagingFactory).ThrowOnFailure();

                using ComScope<IWICImagingFactory> scope = new(pImagingFactory);

                // Use the WIC stream class to wrap the unmanaged pointer
                using ComScope<IWICStream> pIStream = new(null);
                pImagingFactory->CreateStream(pIStream).ThrowOnFailure();
                pIStream.Value->InitializeFromMemory(buffer).ThrowOnFailure();

                // Create an object that will decode the encoded image
                using ComScope<IWICBitmapDecoder> pDecoder = new(null);
                pImagingFactory->CreateDecoderFromStream(
                    (IStream*)(void*)pIStream,
                    IID.NULL(),
                    WICDecodeOptions.WICDecodeMetadataCacheOnDemand,
                    pDecoder).ThrowOnFailure();

                // Get the frame from the decoder. Most image formats have only a single frame, in the case
                // of animated gifs we are ok with only displaying the first frame of the animation.
                using ComScope<IWICBitmapFrameDecode> pDecodedFrame = new(null);
                pDecoder.Value->GetFrame(0, pDecodedFrame).ThrowOnFailure();

                using ComScope<IWICFormatConverter> pBitmapSourceFormatConverter = new(null);
                pImagingFactory->CreateFormatConverter(pBitmapSourceFormatConverter).ThrowOnFailure();

                // Convert the image from whatever format it is in to 32bpp premultiplied alpha BGRA
                pBitmapSourceFormatConverter.Value->Initialize(
                    (IWICBitmapSource*)(void*)pDecodedFrame,
                    PInvoke.GUID_WICPixelFormat32bppPBGRA,
                    WICBitmapDitherType.WICBitmapDitherTypeNone,
                    null,
                    0,
                    WICBitmapPaletteType.WICBitmapPaletteTypeCustom).ThrowOnFailure();

                // Reorient the image
                using ComScope<IWICBitmapFlipRotator> pBitmapFlipRotator = new(null);
                pImagingFactory->CreateBitmapFlipRotator(pBitmapFlipRotator).ThrowOnFailure();

                pBitmapFlipRotator.Value->Initialize(
                    (IWICBitmapSource*)(void*)pBitmapSourceFormatConverter,
                    WICBitmapTransformOptions.WICBitmapTransformFlipVertical).ThrowOnFailure();

                pBitmapFlipRotator.Value->GetSize(out uint width, out uint height).ThrowOnFailure();

                uint stride = width * 4;

                // Initialize the bitmap header
                BITMAPINFOHEADER bmInfo = new()
                {
                    biSize = (uint)sizeof(BITMAPINFOHEADER),
                    biWidth = (int)width,
                    biHeight = (int)height,
                    biBitCount = 32,
                    biPlanes = 1,
                    biCompression = (uint)BI_COMPRESSION.BI_RGB,
                    biSizeImage = stride * height
                };

                // Create a 32bpp DIB.  This DIB must have an alpha channel for UpdateLayeredWindow to succeed.
                void* pBitmapBits = null;

                _hBitmap = PInvokeCore.CreateDIBSection(default, (BITMAPINFO*)&bmInfo, DIB_USAGE.DIB_RGB_COLORS, &pBitmapBits, HANDLE.Null, 0);

                if (_hBitmap.IsNull)
                {
                    throw new Win32Exception();
                }

                // Copy the decoded image to the new buffer which backs the HBITMAP
                WICRect rect = new()
                {
                    X = 0,
                    Y = 0,
                    Width = (int)width,
                    Height = (int)height
                };

                pBitmapFlipRotator.Value->CopyPixels(&rect, stride, stride * height, (byte*)pBitmapBits).ThrowOnFailure();

                _hwnd = CreateWindow(_hBitmap, (int)width, (int)height, topMost);
            }
            catch
            {
                // Cleans up _hwnd and _hBitmap
                DestroyResources();
                throw;
            }
        }

        ~SplashScreen()
        {
            DestroyResources(finalizer: true);
        }
    }
}
