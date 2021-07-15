// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Threading;
using MS.Internal;
using MS.Internal.Interop;
using MS.Internal.KnownBoxes;
using MS.Internal.WindowsBase;
using MS.Utility;
using MS.Win32;

namespace System.Windows
{
    public class SplashScreen
    {
        private IntPtr _hwnd = IntPtr.Zero;
        private string _resourceName;
        private IntPtr _hInstance;
        private NativeMethods.BitmapHandle _hBitmap;
        private ushort _wndClass;
        private DispatcherTimer _dt;
        private TimeSpan _fadeoutDuration;
        private DateTime _fadeoutEnd;
        NativeMethods.BLENDFUNCTION _blendFunc;
        private ResourceManager _resourceManager;
        private Dispatcher _dispatcher;
        // keep this delegate alive as long as the window class is registered
        private static NativeMethods.WndProc _defWndProc;

        private const string CLASSNAME = "SplashScreen";

        public SplashScreen(string resourceName) : this(Assembly.GetEntryAssembly(), resourceName)
        {
        }

        public SplashScreen(Assembly resourceAssembly, string resourceName)
        {
            if (resourceAssembly == null)
            {
                throw new ArgumentNullException("resourceAssembly");
            }
            if (String.IsNullOrEmpty(resourceName))
            {
                throw new ArgumentNullException("resourceName");
            }
            _resourceName = resourceName.ToLowerInvariant();
            _hInstance = Marshal.GetHINSTANCE(resourceAssembly.ManifestModule);
            AssemblyName name = new AssemblyName(resourceAssembly.FullName);
            _resourceManager = new ResourceManager(name.Name + ".g", resourceAssembly);
        }

        public void Show(bool autoClose)
        {
            Show(autoClose, false);
        }

        public void Show(bool autoClose, bool topMost)
        {
            // If we've already been shown it isn't an error to call show
            // again (maybe you forgot) since you will still be shown state.
            if (_hwnd == IntPtr.Zero)
            {
                UnmanagedMemoryStream umemStream;
                using (umemStream = GetResourceStream())
                {
                    if (umemStream != null)
                    {
                        umemStream.Seek(0, SeekOrigin.Begin); // ensure stream position
                        IntPtr pImageSrcBuffer;
                        unsafe
                        {
                            pImageSrcBuffer = new IntPtr(umemStream.PositionPointer);
                        }

                        if (CreateLayeredWindowFromImgBuffer(pImageSrcBuffer, umemStream.Length, topMost) && autoClose == true)
                        {
                            Dispatcher.CurrentDispatcher.BeginInvoke(
                                DispatcherPriority.Loaded,
                                (DispatcherOperationCallback)ShowCallback,
                                this);
                        }
                        // The HWND that we just created is owned by this thread.  When we close we should ensure that it 
                        // does not get accessed from a different thread.  We don't want to reference the .CurrentDispatcher
                        // property before the window is created due to cold start performance concerns.
                        _dispatcher = Dispatcher.CurrentDispatcher;                        
                    }
                    else
                    {
                        throw new IOException(SR.Get(SRID.UnableToLocateResource, _resourceName));
                    }
                }
            }
        }
        
        private static object ShowCallback(object arg)
        {
            SplashScreen splashScreen = (SplashScreen)arg;
            splashScreen.Close(TimeSpan.FromSeconds(0.3));
            return null;
        }

        // This is 200-300 ms slower than Assembly.GetManifestResourceStream() but works with localization.
        private UnmanagedMemoryStream GetResourceStream()
        {
            // Try to get the stream with the string the developer supplied, in the app.g.cs case
            // this will always work.
            UnmanagedMemoryStream stream = _resourceManager.GetStream(_resourceName, System.Globalization.CultureInfo.CurrentUICulture);
            if (stream != null)
            {
                return stream;
            }

            // IF that fails then the resource name had special characters in it which would not
            // be encoded literally into the resource stream.  Unfortunately we need to rely on the
            // slow URI class to get the correct name.  We try to avoid doing this in the common case
            // since URI has quite a bit of code associated with it.
            string resourceName = ResourceIDHelper.GetResourceIDFromRelativePath(_resourceName);
            return _resourceManager.GetStream(resourceName, System.Globalization.CultureInfo.CurrentUICulture);
        }

        private IntPtr CreateWindow(NativeMethods.BitmapHandle hBitmap, int width, int height, bool topMost)
        {
            if (_defWndProc == null)
            {
                _defWndProc = new MS.Win32.NativeMethods.WndProc(UnsafeNativeMethods.DefWindowProc);
            }

            MS.Win32.NativeMethods.WNDCLASSEX_D wndClass = new MS.Win32.NativeMethods.WNDCLASSEX_D();
            wndClass.cbSize = Marshal.SizeOf(typeof(MS.Win32.NativeMethods.WNDCLASSEX_D));
            wndClass.style = 3; /* CS_HREDRAW | CS_VREDRAW */
            wndClass.lpfnWndProc = null;
            wndClass.hInstance = _hInstance;
            wndClass.hCursor = IntPtr.Zero;
            wndClass.lpszClassName = CLASSNAME;
            wndClass.lpszMenuName = string.Empty;
            wndClass.lpfnWndProc = _defWndProc;

            // We chose to ignore re-registration errors in RegisterClassEx on the off chance that the user
            // wants to open multiple splash screens.
            _wndClass = MS.Win32.UnsafeNativeMethods.IntRegisterClassEx(wndClass);
            if (_wndClass == 0)
            {
                var lastWin32Error = Marshal.GetLastWin32Error();
                if (lastWin32Error != 0x582) /* class already registered */
                    throw new Win32Exception(lastWin32Error);
            }

            int screenWidth = MS.Win32.UnsafeNativeMethods.GetSystemMetrics(SM.CXSCREEN);
            int screenHeight = MS.Win32.UnsafeNativeMethods.GetSystemMetrics(SM.CYSCREEN);
            int x = (screenWidth - width) / 2;
            int y = (screenHeight - height) / 2;

            HandleRef nullHandle = new HandleRef(null, IntPtr.Zero);
            int windowCreateFlags =
                (int) NativeMethods.WS_EX_WINDOWEDGE |
                      NativeMethods.WS_EX_TOOLWINDOW |
                      NativeMethods.WS_EX_LAYERED |
                      (topMost ? NativeMethods.WS_EX_TOPMOST : 0);

            // CreateWindowEx will either succeed or throw
            IntPtr hWnd =  MS.Win32.UnsafeNativeMethods.CreateWindowEx(
                windowCreateFlags,
                CLASSNAME, SR.Get(SRID.SplashScreenIsLoading),
                MS.Win32.NativeMethods.WS_POPUP | MS.Win32.NativeMethods.WS_VISIBLE,
                x, y, width, height,
                nullHandle, nullHandle, new HandleRef(null, _hInstance), IntPtr.Zero);

            // Display the image on the window
            IntPtr hScreenDC = UnsafeNativeMethods.GetDC(new HandleRef());
            IntPtr memDC = UnsafeNativeMethods.CreateCompatibleDC(new HandleRef(null, hScreenDC));
            IntPtr hOldBitmap = UnsafeNativeMethods.SelectObject(new HandleRef(null, memDC), hBitmap.MakeHandleRef(null).Handle);

            NativeMethods.POINT newSize = new NativeMethods.POINT(width, height);
            NativeMethods.POINT newLocation = new NativeMethods.POINT(x, y);
            NativeMethods.POINT sourceLocation = new NativeMethods.POINT(0, 0);
            _blendFunc = new NativeMethods.BLENDFUNCTION();
            _blendFunc.BlendOp = NativeMethods.AC_SRC_OVER;
            _blendFunc.BlendFlags = 0;
            _blendFunc.SourceConstantAlpha = 255;
            _blendFunc.AlphaFormat = 1; /*AC_SRC_ALPHA*/

            bool result = UnsafeNativeMethods.UpdateLayeredWindow(hWnd, hScreenDC, newLocation, newSize,
                memDC, sourceLocation, 0, ref _blendFunc, NativeMethods.ULW_ALPHA);

            UnsafeNativeMethods.SelectObject(new HandleRef(null, memDC), hOldBitmap);
            UnsafeNativeMethods.ReleaseDC(new HandleRef(), new HandleRef(null, memDC));
            UnsafeNativeMethods.ReleaseDC(new HandleRef(), new HandleRef(null, hScreenDC));

            if (result == false)
            {
                UnsafeNativeMethods.HRESULT.Check(Marshal.GetHRForLastWin32Error());
            }

            return hWnd;
        }

        public void Close(TimeSpan fadeoutDuration)
        {
            object result = null;
            if (_dispatcher != null)
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


        private object CloseInternal(Object fadeOutArg)
        {
            TimeSpan fadeoutDuration = (TimeSpan) fadeOutArg;
            if (fadeoutDuration <= TimeSpan.Zero)
            {
                DestroyResources();
                return BooleanBoxes.TrueBox;
            }

            // In the case where the developer has specified AutoClose=True and then calls
            // Close(non_zero_timespan) before the auto close operation is dispatched we begin
            // the fadeout immidiately and ignore the later call to close.
            if (_dt != null || _hwnd == IntPtr.Zero)
            {
                return BooleanBoxes.TrueBox;
            }

            // by default close gets called as soon as the first application window is created
            // since it will have become the active window we need to steal back the active window
            // status so that the fade out animation is visible.
            IntPtr prevHwnd = UnsafeNativeMethods.SetActiveWindow(new HandleRef(null, _hwnd));
            if (prevHwnd == IntPtr.Zero)
            {
                // SetActiveWindow fails (returns NULL) if the application is not in the foreground.
                // If this is the case, don't bother animating the fade out.
                DestroyResources();
                return BooleanBoxes.TrueBox;
            }

            _dt = new DispatcherTimer();
            _dt.Interval = TimeSpan.FromMilliseconds(30); // shoot for ~30 fps
            _fadeoutDuration = fadeoutDuration;
            _fadeoutEnd = DateTime.UtcNow + _fadeoutDuration;
            _dt.Tick += new EventHandler(Fadeout_Tick);
            _dt.Start();

            return BooleanBoxes.TrueBox;
        }

        private void Fadeout_Tick(object unused, EventArgs args)
        {
            DateTime dtNow = DateTime.UtcNow;
            if (dtNow >= _fadeoutEnd)
            {
                DestroyResources();
            }
            else
            {
                double progress = (_fadeoutEnd - dtNow).TotalMilliseconds / _fadeoutDuration.TotalMilliseconds;
                _blendFunc.SourceConstantAlpha = (byte)(255 * progress);
                UnsafeNativeMethods.UpdateLayeredWindow(_hwnd, IntPtr.Zero, null, null, IntPtr.Zero, null, 0, ref _blendFunc, NativeMethods.ULW_ALPHA);
            }
        }

        private void DestroyResources()
        {
            if (_dt != null)
            {
                _dt.Stop();
                _dt = null;
            }
            if (_hwnd != IntPtr.Zero)
            {
                HandleRef hwnd = new HandleRef(null, _hwnd);
                if (UnsafeNativeMethods.IsWindow(hwnd))
                {
                    UnsafeNativeMethods.IntDestroyWindow(hwnd);
                }
                _hwnd = IntPtr.Zero;
            }
            if (_hBitmap != null && !_hBitmap.IsClosed)
            {
                UnsafeNativeMethods.DeleteObject(_hBitmap.MakeHandleRef(null).Handle);
                _hBitmap.Close();
                _hBitmap = null;
            }
            if (_wndClass != 0)
            {
                // Attempt to unregister the window class.  If the application has a second
                // splash screen which is still open this call will fail.  That's OK.
                if (UnsafeNativeMethods.IntUnregisterClass(new IntPtr(_wndClass), _hInstance) != 0)
                {
                    _defWndProc = null; // Can safely release the wndproc delegate when there are no more splash screen instances
                }
                _wndClass = 0;
            }
            if (_resourceManager != null)
            {
                _resourceManager.ReleaseAllResources();
            }
        }

        private bool CreateLayeredWindowFromImgBuffer(IntPtr pImgBuffer, long cImgBufferLen, bool topMost)
        {
            bool bSuccess = false;
            IntPtr pImagingFactory = IntPtr.Zero;
            IntPtr pDecoder = IntPtr.Zero;
            IntPtr pIStream = IntPtr.Zero;
            IntPtr pDecodedFrame = IntPtr.Zero;
            IntPtr pBitmapSourceFormatConverter = IntPtr.Zero;
            IntPtr pBitmapFlipRotator = IntPtr.Zero;

            try
            {
                UnsafeNativeMethods.HRESULT.Check(
                    UnsafeNativeMethods.WIC.CreateImagingFactory(UnsafeNativeMethods.WIC.WINCODEC_SDK_VERSION, out pImagingFactory));

                // Use the WIC stream class to wrap the unmanaged pointer
                UnsafeNativeMethods.HRESULT.Check(
                    UnsafeNativeMethods.WIC.CreateStream(pImagingFactory, out pIStream));

                UnsafeNativeMethods.HRESULT.Check(
                    UnsafeNativeMethods.WIC.InitializeStreamFromMemory(pIStream, pImgBuffer, (uint)cImgBufferLen));

                // Create an object that will decode the encoded image
                Guid vendor = Guid.Empty;
                UnsafeNativeMethods.HRESULT.Check(
                    UnsafeNativeMethods.WIC.CreateDecoderFromStream(pImagingFactory, pIStream,
                                                                    ref vendor, 0, out pDecoder));

                // Get the frame from the decoder. Most image formats have only a single frame, in the case
                // of animated gifs we are ok with only displaying the first frame of the animation.
                UnsafeNativeMethods.HRESULT.Check(
                    UnsafeNativeMethods.WIC.GetFrame(pDecoder, 0, out pDecodedFrame));

                UnsafeNativeMethods.HRESULT.Check(
                    UnsafeNativeMethods.WIC.CreateFormatConverter(pImagingFactory, out pBitmapSourceFormatConverter));

                // Convert the image from whatever format it is in to 32bpp premultiplied alpha BGRA
                Guid pixelFormat = UnsafeNativeMethods.WIC.WICPixelFormat32bppPBGRA;
                UnsafeNativeMethods.HRESULT.Check(
                    UnsafeNativeMethods.WIC.InitializeFormatConverter(pBitmapSourceFormatConverter, pDecodedFrame,
                                                                      ref pixelFormat, 0 /*DitherTypeNone*/, IntPtr.Zero,
                                                                      0, UnsafeNativeMethods.WIC.WICPaletteType.WICPaletteTypeCustom));
                // Reorient the image
                UnsafeNativeMethods.HRESULT.Check(
                    UnsafeNativeMethods.WIC.CreateBitmapFlipRotator(pImagingFactory, out pBitmapFlipRotator));

                UnsafeNativeMethods.HRESULT.Check(
                    UnsafeNativeMethods.WIC.InitializeBitmapFlipRotator(pBitmapFlipRotator, pBitmapSourceFormatConverter,
                                                                        UnsafeNativeMethods.WIC.WICBitmapTransformOptions.WICBitmapTransformFlipVertical));
                Int32 width, height;
                UnsafeNativeMethods.HRESULT.Check(
                    UnsafeNativeMethods.WIC.GetBitmapSize(pBitmapFlipRotator, out width, out height));

                Int32 stride = width * 4;

                // initialize the bitmap header
                MS.Win32.NativeMethods.BITMAPINFO bmInfo = new MS.Win32.NativeMethods.BITMAPINFO(width, height, 32 /*bpp*/);
                bmInfo.bmiHeader_biCompression = MS.Win32.NativeMethods.BI_RGB;
                bmInfo.bmiHeader_biSizeImage = (int)(stride * height);

                // Create a 32bpp DIB.  This DIB must have an alpha channel for UpdateLayeredWindow to succeed.
                IntPtr pBitmapBits = IntPtr.Zero;
                _hBitmap = UnsafeNativeMethods.CreateDIBSection(new HandleRef(), ref bmInfo, 0 /* DIB_RGB_COLORS*/, ref pBitmapBits, null, 0);

                // Copy the decoded image to the new buffer which backs the HBITMAP
                Int32Rect rect = new Int32Rect(0, 0, width, height);
                UnsafeNativeMethods.HRESULT.Check(
                    UnsafeNativeMethods.WIC.CopyPixels(pBitmapFlipRotator, ref rect, stride, stride * height, pBitmapBits));

                _hwnd = CreateWindow(_hBitmap, width, height, topMost);

                bSuccess = true;
            }
            finally
            {
                if (pImagingFactory != IntPtr.Zero)
                {
                    Marshal.Release(pImagingFactory);
                }
                if (pDecoder != IntPtr.Zero)
                {
                    Marshal.Release(pDecoder);
                }
                if (pIStream != IntPtr.Zero)
                {
                    Marshal.Release(pIStream);
                }
                if (pDecodedFrame != IntPtr.Zero)
                {
                    Marshal.Release(pDecodedFrame);
                }
                if (pBitmapSourceFormatConverter != IntPtr.Zero)
                {
                    Marshal.Release(pBitmapSourceFormatConverter);
                }
                if (pBitmapFlipRotator != IntPtr.Zero)
                {
                    Marshal.Release(pBitmapFlipRotator);
                }

                if (bSuccess == false)
                {
                    DestroyResources(); // cleans up _hwnd and _hBitmap
                }
            }

            return bSuccess;
        }
    }
}

