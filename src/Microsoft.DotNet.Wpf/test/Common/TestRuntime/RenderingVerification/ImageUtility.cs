// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification
{
    #region usings
        using System;
        using System.IO;
        using System.Drawing;
        using System.Collections;
        using System.Drawing.Imaging;
        using System.Security.Permissions;
        using System.Runtime.InteropServices;
        using Microsoft.Test.Logging;
        using Microsoft.Test.RenderingVerification.UnmanagedProxies;
        using Microsoft.Test.Win32;
/*
        // Define D3DFORMAT struct as dword (from vrm9.h)
        using D3DFORMAT = System.Int32;
*/
    #endregion usings

    /// <summary>
    /// Image manipulation and ScreenShot
    /// </summary>
    [PermissionSet(SecurityAction.Assert, Name="FullTrust")]
#if CLR_VERSION_BELOW_2
    public class ImageUtility : IImageUtilUnmanaged, IDisposable
#else
    public partial class ImageUtility : IImageUtilityUnmanaged, IDisposable
#endif
    {
/*
        #region Constants
            private const int D3DFMT_A8R8G8B8 = 21; // from "d3d8types.h"
            private const int D3DFMT_X8R8G8B8 = 22; // from "d3d8types.h"
            private const string DWMAPI_DLL = "dwmapi.dll";
            private const string DWM_API_CAPTURE_NAME = "DwmCaptureDesktop";
            private const string DWM_API_ISCOMPOSITIONENABLED = "DwmIsCompositionEnabled";
            private const int E_DWM_SERVICE_NOT_RUNNING = unchecked((int)0xd000042);
        #endregion Constants


        #region Delegates
            private delegate int DwmCaptureDesktopDelegate(uint left, uint top, uint right, uint bottom, ref D3DFORMAT fmtBits, ref IntPtr hBits);
            private delegate int DwmIsCompositionEnabledDelegate(ref int enabled);
        #endregion Delegates
*/
        #region Properties
            #region Private properties
                /// <summary>
                /// Store if instance has been disposed yet.
                /// </summary>
                private bool _disposed = false;
                /// <summary>
                /// The bitmap we are using
                /// </summary>
                private Bitmap _bmpDoNotUse = null;
                /// <summary>
                /// Store if the GetSetBeginPixel has been called (and commit not called yet)
                /// </summary>
                private bool _getsetPixelBegin = false;
                /// <summary>
                /// Hold a copy of the bitmap stream
                /// </summary>
                private byte[] _buffer = null;
                /// <summary>
                /// The Bitmap Width
                /// </summary>
                private int _bmpWidth = 0;
                /// <summary>
                /// The Bitmap Height
                /// </summary>
                private int _bmpHeight = 0;
                /// <summary>
                /// The original Bitmap stride
                /// </summary>
                private int _bmpStride = 0;
                /// <summary>
                /// The original bitmap PixelFormat
                /// </summary>
                private PixelFormat _originalImageFormat = PixelFormat.Undefined;
                /// <summary>
                /// Provide a safe access to the bitmap (and perform minor cleanup)
                /// </summary>
                private Bitmap Bmp
                {
                    get
                    {
                        return _bmpDoNotUse;
                    }
                    set
                    {
                        if (Bmp != value)
                        {
                            if (_bmpDoNotUse != null)
                            {
                                _bmpDoNotUse.Dispose();
                            }
                            if (value == null)
                            {
                                _disposed = true;
                                _getsetPixelBegin = false;
                                _buffer = null;
                                _bmpWidth = 0;
                                _bmpHeight = 0;
                                _originalImageFormat = PixelFormat.Undefined;
                                _bmpDoNotUse = null;
                                _bmpStride = 0;
                            }
                            else 
                            {
                                _disposed = false;
                                _getsetPixelBegin = false;
                                _buffer = null;
                                _bmpWidth = value.Width;
                                _bmpHeight = value.Height;
                                _originalImageFormat = value.PixelFormat;
                                _bmpDoNotUse = ImageUtility.ConvertPixelFormat(value, PixelFormat.Format32bppArgb);
                                _bmpStride = _bmpWidth * 4 /*(_bmpDepth >> 3)*/ ;
                            }
                        }
                    }
                }
           #endregion Private properties
            #region Public properties (get/set)
                /// <summary>
                /// Get the bitmap generated
                /// </summary>
                public Bitmap Bitmap32Bits
                {
                    get
                    {
                        return _bmpDoNotUse;
                    }
                }
            #endregion Public properties (get/set)
        #endregion Properties

        #region Constructors
            /// <summary>
            /// Create an instance of the ImageSnaphot class
            /// </summary>
            public ImageUtility()
            {
                _disposed = true;
                Bmp = null;
            }
            /// <summary>
            /// Create an instance of the ImageUtility class
            /// </summary>
            /// <param name="width">Width of the image</param>
            /// <param name="height">Height of the image</param>
            public ImageUtility(int width, int height)
            {
                if (width <= 0 || height <= 0)
                { 
                    throw new ArgumentException("Neither width nor height can be set <= 0");
                }
                Bmp = new Bitmap(width, height,PixelFormat.Format32bppArgb);
            }
            /// <summary>
            /// Create an instance of the ImageSnaphot class
            /// Note : this will not lock the underlying file as a copy of it is created.
            /// </summary>
            /// <param name="filename">(string) The bitmap to load</param>
            public ImageUtility(string filename)
            {
                if (filename == null || filename.Trim() == string.Empty)
                {
                    throw new ArgumentNullException("filename", "Argument passed in must be a valid instance of string (null or empty string passed in)");
                }
                if (File.Exists(filename) == false)
                {
                    throw new FileNotFoundException("The specified file ('" + filename + "') cannot be found. ( File still exist ? Network issues ?) ");
                }
                Bmp = GetUnlockedBitmap(filename);
            }
            /// <summary>
            /// Create an instance of the ImageSnaphot class
            /// </summary>
            /// <param name="bmp">(Bitmap) The bitmap to use</param>
            public ImageUtility(Bitmap bmp)
            {
                if (bmp == null)
                {
                    throw new ArgumentNullException("Bitmap passed in cannot be null");
                }
                Bmp = bmp;
            }
        #endregion Constructors

        #region Public Methods
            /// <summary>
            /// Initialize the GetPixelUnsafe / SetPixelUnsafe APIs
            /// </summary>
            public unsafe void GetSetPixelUnsafeBegin()
            {
                if (_getsetPixelBegin)
                {
                    throw new RenderingVerificationException("GetSetPixelBegin already called, call it only once.");
                }
                _getsetPixelBegin = true;

                BitmapData bmpData = null;
                try
                {
                    Rectangle rectangle = new Rectangle(0, 0, _bmpWidth, _bmpHeight);
                    bmpData = _bmpDoNotUse.LockBits(rectangle, ImageLockMode.ReadOnly, _bmpDoNotUse.PixelFormat);
                    IntPtr dataPtr = bmpData.Scan0;
                    _buffer = new byte[(int)(bmpData.Width * bmpData.Height /* * _bmpDepth / 8*/ * 4)];

                    // Some images are Bottom to top other top to bottom (depending on the sign of the stride property), internal buffer will always be top to bottom
                    int delta = 0;
                    int offset = bmpData.Stride;
                    int size = Math.Abs (offset);
                    for (int t = 0; t < bmpData.Height; t++)
                    {
                        delta = t * offset;
                        Marshal.Copy ((IntPtr)((Int64)dataPtr + delta), _buffer, Math.Abs(delta)/*size * t*/, size);
                    }

                }
                finally
                {
                    if (bmpData != null)
                    {
                        _bmpDoNotUse.UnlockBits(bmpData);
                        bmpData = null;
                    }
                }
            }
            /// <summary>
            /// Commit the change done with SetPixelUnsafe to the Bitmap 
            /// </summary>
            public unsafe void GetSetPixelUnsafeCommit()
            {
                if (! _getsetPixelBegin)
                {
                    throw new RenderingVerificationException("GetSetPixelBegin must be called before calling GetPixelCommit.");
                }
                _getsetPixelBegin = false;

                BitmapData bmpData = null;
                try
                {
                    Rectangle rectangle = new Rectangle(0, 0, _bmpWidth, _bmpHeight);
                    bmpData = _bmpDoNotUse.LockBits(rectangle, ImageLockMode.WriteOnly, _bmpDoNotUse.PixelFormat);

                    // Bottom to top bitmap needs to be inverted for RGB image (not ARGB img)
                    int delta = 0;
                    int offset = bmpData.Stride;
                    int size = Math.Abs (offset);
                    for (int t = 0; t < bmpData.Height; t++)
                    {
                        Marshal.Copy(_buffer, size * t, (IntPtr)((Int64)bmpData.Scan0 + delta), size);
                        delta += offset;
                    }
                }
                finally
                {
                    if (bmpData != null)
                    {
                        _bmpDoNotUse.UnlockBits(bmpData);
                        bmpData = null;
                    }
                }
                _buffer = null;
                GC.Collect(GC.MaxGeneration);
            }
            /// <summary>
            /// Discard the change done with SetPixelUnsafe to the Bitmap 
            /// </summary>
            public unsafe void GetSetPixelUnsafeRollBack()
            {
                if (! _getsetPixelBegin)
                {
                    throw new RenderingVerificationException("GetSetPixelBegin must be called before calling GetPixelCommit.");
                }
                _getsetPixelBegin = false;
                _buffer = null;
                GC.Collect(GC.MaxGeneration);
            }
            /// <summary>
            /// Retrieve a Pixel within the bitmap (Fast but Unsafe)
            /// </summary>
            /// <param name="x">(int) The position on the x axis</param>
            /// <param name="y">(int) The position on the y axis</param>
            /// <returns>(Color) A Color struct containing the ARGB value of this pixel</returns>
            public unsafe IColor GetPixelUnsafe(int x, int y)
            {
                if (! _getsetPixelBegin)
                {
                    throw new RenderingVerificationException("The GetSetPixelBegin method must be called before calling GetPixel");
                }

                IColor color = ColorByte.Empty;
                int index = (x * 4) + (y * _bmpWidth * 4);

                fixed (byte* ptr = _buffer)
                {
                    color = new ColorByte(*(ptr + index + 3), *(ptr + index + 2), *(ptr + index + 1), *(ptr + index));
                }

                return color;
            }
            /// <summary>
            /// Modify a Pixel within the bitmap (Fast but Unsafe)
            /// </summary>
            /// <param name="x">(int) The position on the x axis</param>
            /// <param name="y">(int) The position on the y axis</param>
            /// <param name="color">(Color) A Color struct containing the ARGB value of this pixel</param>
            public unsafe void SetPixelUnsafe(int x, int y, IColor color)
            {
                if (! _getsetPixelBegin)
                {
                    throw new RenderingVerificationException("The GetSetPixelBegin method must be called before calling GetPixel");
                }

                int index = (x * 4) + (y * _bmpWidth * 4);
                fixed (byte* ptr = _buffer)
                {
                    *(ptr + index) = color.B;
                    *(ptr + index + 1) = color.G;
                    *(ptr + index + 2) = color.R;
                    *(ptr + index + 3) = color.A;
                }
            }
            /// <summary>
            /// return the underlying stream for expensive operation
            /// </summary>
            /// <returns></returns>
            internal byte[] GetStreamBufferBGRA()
            {
                if (! _getsetPixelBegin)
                {
                    throw new RenderingVerificationException("The GetSetPixelBegin method must be called before calling GetPixel");
                }
                return _buffer;
            }
        #endregion Public Methods

        #region Static Public & Private Methods
            
            ///// <summary>
            ///// Adds ImageUtility Proxy to the property Bag and also Sets the Hwnd of the Screen Proxy
            ///// </summary>
            ///// <param name="hwnd">Handle of the window from which Capture needs to be made</param>
            //static internal void SetScreenProxy(IntPtr hwnd)
            //{
            //    Harness.Current.RemoteSite["ImageUtilityProxy"] = new Microsoft.Test.RenderingVerification.ImageUtilityProxy(hwnd);
            //}

            ///// <summary>
            ///// Identifies if ScreenProxy is set or not
            ///// </summary>
            //static internal bool IsScreenProxySet
            //{
            //    get
            //    {
            //        ImageUtilityProxy iu = (ImageUtilityProxy)Harness.Current.RemoteSite["ImageUtilityProxy"];
            //        if (null == iu)
            //        {
            //            return false;
            //        }
            //        else
            //        {
            //            return true;
            //        }
            //    }
            //}

            ///// <summary>
            ///// Removes ImageUtility Proxy from the Property Bag
            ///// </summary>
            //static internal void ResetScreenProxy()
            //{
            //    Harness.Current.RemoteSite["ImageUtilityProxy"] = null;
            //}

            /// <summary>
            /// Get a snapshot of the screen for the specified area of the Local Machine or RemoteMachine
            /// </summary>
            /// <param name="rectangle">(Rectangle) The area - in screen coordinate - to be captured</param>
            /// <returns>(Bitmap) The bitmap captured</returns>
            static public Bitmap CaptureScreen(Rectangle rectangle)
            {
                return ImageUtility.CaptureScreen(rectangle, true);
            }

            /// <summary>
            /// Get a snapshot of the screen for the specified area on the machine in which it is called
            /// </summary>
            /// <param name="rectangle"> The area - in screen coordinate - to be captured</param>
            /// <param name="allowScreenProxy"> Flag to determine whether capture must be taken from Local or Remote machine</param>
            /// <returns>(Bitmap) The bitmap captured</returns>
            static public Bitmap CaptureScreen(Rectangle rectangle, bool allowScreenProxy)
            {
                if (rectangle.IsEmpty)
                {
                    throw new RenderingVerificationException("Invalid value passed in, rectangle must be valid ('Empty' rectangle passed in)");
                }
                return ImageUtility.InternalScreenCapture(rectangle);                
            }
            /// <summary>
            /// Get a snapshot of the screen for the specified area
            /// </summary>
            /// <param name="HWND">(IntPtr) The HWND of the window to take a snapshot of</param>
            /// <param name="clientAreaOnly">(bool) Include just the client area or the whole window</param>
            /// <returns>(Bitmap) The bitmap captured</returns>
            static public Bitmap CaptureScreen(IntPtr HWND, bool clientAreaOnly)
            {
                return ImageUtility.CaptureScreen(HWND, clientAreaOnly, true);
            }
            /// <summary>
            /// Gets a Snap shot of screen which contains the Window
            /// </summary>
            /// <param name="HWND">(IntPtr) The HWND of the window to take a snapshot of</param>
            /// <param name="clientAreaOnly">(bool) Include just the client area or the whole window</param>
            /// <param name="allowScreenProxy">If true forwards the call to remote machine for Capture</param>
            /// <returns></returns>
            static public Bitmap CaptureScreen(IntPtr HWND, bool clientAreaOnly, bool allowScreenProxy)
            {
                // Check if HWND is valid
                if (HWND == IntPtr.Zero)
                {
                    throw new RenderingVerificationException("(ImageLibraby::CaptureScreen) The HWND must be a valid pointer (set to NULL)");
                }
                if (User32.IsWindow(HWND) == false)
                {
                    throw new RenderingVerificationException("(ImageLibraby::CaptureScreen) The HWND must be a valid window ('" + HWND.ToString() + "' is not a window handler)");
                }

                //Gets WindowInfor Both Client and Non Client Area
                User32.WINDOWINFO wi = new User32.WINDOWINFO();
                bool success = User32.GetWindowInfo(HWND, ref wi);
                if (!success)
                {
                    throw new ExternalException("Win32 API call to 'GetWindowInfo' failed !");
                }
                
                Rectangle area = new Rectangle(0,0,0,0);
                Point pt1,pt2;
                if (clientAreaOnly == false)
                {
                    pt1 = new Point(wi.rcWindow.Left, wi.rcWindow.Top);
                    pt2 = new Point(wi.rcWindow.Right, wi.rcWindow.Bottom);
                }
                else
                {
                    pt1 = new Point(wi.rcClient.Left, wi.rcClient.Top);
                    pt2 = new Point(wi.rcClient.Right, wi.rcClient.Bottom);
                }

                //Ensure that client x values are correct for both LTR and RTL windows
                if (pt2.X > pt1.X)
                    // update the rectangle with the new values
                    area = new Rectangle(pt1.X, pt1.Y, pt2.X - pt1.X, pt2.Y - pt1.Y);
                else
                    area = new Rectangle(pt2.X, pt1.Y, pt1.X - pt2.X, pt2.Y - pt1.Y);


                if (allowScreenProxy)
                {
                    return ImageUtility.CaptureScreen(area);
                }
                else
                {
                    return ImageUtility.InternalScreenCapture(area);
                }

            }
            /// <summary>
            /// Get the bitmap associated with a DC in the specified area
            /// Note : You are responsible for releasing the HDC passed in
            /// </summary>
            /// <param name="HDC">(IntPtr) The HDC associated with the Bitmap</param>
            /// <param name="areaToCopy">(Rectangle) The area to copy</param>
            static public Bitmap CaptureBitmapFromDC(IntPtr HDC, Rectangle areaToCopy)
            {
                IntPtr hdcSrc = IntPtr.Zero;
                IntPtr hdcDest = IntPtr.Zero;
                IntPtr hBMP = IntPtr.Zero;
                IntPtr hPreviousObj = IntPtr.Zero;
                Bitmap bmp = null;

                if (HDC == IntPtr.Zero)
                { 
                    throw new RenderingVerificationException("Invalid handler passed in as HDC (IntPtr.Zero)");
                }
                if (areaToCopy == Rectangle.Empty)
                {
                    throw new RenderingVerificationException("Area is empty, cannot get the bitmap of an undefined surface");
                }

                try
                {
                    hdcSrc = HDC;

                    // Allocate memory for the bitmap
                    hBMP = Gdi32.CreateCompatibleBitmap(hdcSrc, areaToCopy.Width, areaToCopy.Height);
                    if (hBMP == IntPtr.Zero)
                    {
                        throw new ExternalException("Win32 API 'CreateCompatibleBitmap ' failed in ImageUtility::InternalCapture");
                    }

                    // Create destination DC
                    hdcDest = Gdi32.CreateCompatibleDC(hdcSrc);
                    if (hdcDest == IntPtr.Zero)
                    {
                        throw new ExternalException("Win32 API 'CreateCompatibleDC ' failed in ImageUtility::InternalCapture");
                    }

                    // copy screen to bitmap
                    hPreviousObj = Gdi32.SelectObject(hdcDest, hBMP);
                    if (hPreviousObj == IntPtr.Zero)
                    {
                        throw new ExternalException("Win32 API 'SelectObject' failed in ImageUtility::InternalCapture");
                    }

                    // Note : CAPTUREBLT Needed to capture layered windows
                    bool result = Gdi32.BitBlt(hdcDest, 0, 0, areaToCopy.Width, areaToCopy.Height, hdcSrc, areaToCopy.Left, areaToCopy.Top, (Int32)(Gdi32.RasterOperationCodeEnum.SRCCOPY | Gdi32.RasterOperationCodeEnum.CAPTUREBLT) );

                    if (result == false)
                    {
                        throw new ExternalException("Win32 API 'BitBlt ' failed in ImageUtility::InternalCapture. Check if the 'Monitor Power Off' feature is not turned on.");
                    }

                    bmp = Bitmap.FromHbitmap(hBMP);
                }
                finally
                {
                    if (hPreviousObj != IntPtr.Zero)
                    {
                        Gdi32.SelectObject(hdcDest, hPreviousObj);
                        hPreviousObj = IntPtr.Zero;
                    }

                    if (hdcSrc != IntPtr.Zero)
                    {
                        Gdi32.DeleteDC(hdcSrc);
                        hdcSrc = IntPtr.Zero;
                    }

                    if (hdcDest != IntPtr.Zero)
                    {
                        Gdi32.DeleteDC(hdcDest);
                        hdcDest = IntPtr.Zero;
                    }

                    if (hBMP != IntPtr.Zero)
                    {
                        Gdi32.DeleteObject(hBMP);
                        hBMP = IntPtr.Zero;
                    }
                }
                return bmp;
            }
            /// <summary>
            /// Convert a bitmap from one format to another one
            /// Note : caller is responsible for diposing [in] and [out] Bitmaps
            /// </summary>
            /// <param name="bitmap">The bitmap to be converted</param>
            /// <param name="pixelFormat">The format to be used in the returned bitmap</param>
            /// <returns>The converted bitmap</returns>
            static public Bitmap ConvertPixelFormat(Bitmap bitmap, PixelFormat pixelFormat)
            {
                // Check params
                if (bitmap == null)
                {
                    throw new ArgumentNullException("bitmap", "value passed in must be a valid instance of Bitmap object (null passed in)");
                }
                

                //We need to clone from the original bitmap
                //or else the dpi info will be lost in the new one,
                //even though we copy the metadata as well
                //Sys.Drawing doesnt seem to expose a way to specify dpi when creating a bmp
                if (bitmap.PixelFormat == pixelFormat)
                {
                    return bitmap.Clone() as Bitmap;
                }

                //we will lose DPI info here if pixel format is diff,
                //since Bitmap ctor doesnt take dpi as argument.
                //Also, Graphics.DrawImage actually scales the image
                //base on the dpi ratio of source and target.
                //e.g. source=192dpi;target=96dpi, then target will be 1/4 the size
                //of source after DrawImage().
                Bitmap retVal = new Bitmap(bitmap.Width, bitmap.Height, pixelFormat);
                using(Graphics gr = Graphics.FromImage(retVal))
                {
                    gr.DrawImage(bitmap, 0, 0);
                }

                // Set the property items
                PropertyItem[] propertyItems = bitmap.PropertyItems;
                for (int t = 0; t < propertyItems.Length; t++)
                {
                    retVal.SetPropertyItem(propertyItems[t]);
                }

                return retVal;
            }
            /// <summary>
            /// Create a Bitmap based on the original one
            /// The newly created Bitmap is the size of the rectangle specified as paramenter.
            /// If the size of originalbitmap is smaller than what is specified in the rectangle 
            /// paramenter the region represented by the portions outside of the original bitmap 
            /// are represented by Color.Empty. If X and Y values of rectangle parameter is negative
            /// then the returned bitmap is filled with Color.Empty in the regions outside of the original 
            /// Bitmap relative to the original Bitmap which is considere to start at (X,Y)=(0,0).
            /// Note : Caller is responsible of Disposing the [in] and [out] Bitmaps
            /// </summary>
            /// <param name="originalBmp">The BMP to clip</param>
            /// <param name="rectangle">(Rectangle) The area to be copied</param>
            /// <returns>(Bitmap) A new Bitmap containing only the specified rectangle</returns>
            static public Bitmap ClipBitmap (Bitmap originalBmp, Rectangle rectangle)
            {
                // BUGBUG :
                // This function is not clipping properly.
                // Problem : The clipped result is shifted up-left.
                // Probable cause : Passing negative coordinates to
                //  Graphics.DrawImage is the cause of the problem.

                // check params
                if (originalBmp == null)
                {
                    throw new ArgumentNullException("originalBmp", "Bitmap must be set to a valid instance (null passed in)");
                }
                if (originalBmp.Width < 1 || originalBmp.Height < 1)
                {
                    throw new RenderingVerificationException("Bitmap passed in is invalid (width or height is zero)");
                }

                Bitmap retVal = new Bitmap(rectangle.Width, rectangle.Height, originalBmp.PixelFormat);
                using (Graphics gr = Graphics.FromImage(retVal))
                {
                    gr.Clear(Color.Empty);
                    gr.DrawImage(originalBmp, -rectangle.Left, -rectangle.Top);
                }
                return retVal;
            }
            /// <summary>
            /// Return a bitmap that does not lock the underlying file
            /// Note : User still have to call Dispose() to release memory when done with it.
            /// </summary>
            /// <param name="fileName">The name of the Bitmap to open</param>
            /// <returns>A new bitmap</returns>
            static public Bitmap GetUnlockedBitmap(string fileName)
            {
                Bitmap retVal = null;
                if(File.Exists(fileName) == false)
                {
                    throw new RenderingVerificationException("The specified file was not found");
                }
                Bitmap lockingFileBmp = null;
                try
                {
                    lockingFileBmp = new Bitmap(fileName);
                    retVal = (Bitmap)lockingFileBmp.Clone();

                }
                catch(IOException e)
                {
                    throw new RenderingVerificationException("Error when trying to access specified file. Check if this is a valid bitmap", e);
                }
                finally
                {
                    if(lockingFileBmp != null)
                    {
                        lockingFileBmp.Dispose();
                        lockingFileBmp = null;
                    }
                }
                return retVal;
            }
            /// <summary>
            /// Concatenate one or multiple bitmap into one
            /// </summary>
            /// <param name="vertical">Vertical concatenation if true, horizontal otherwise</param>
            /// <param name="bitmaps">The bitmaps to be concatenated</param>
            /// <returns>The resulting bitmap</returns>
            static public Bitmap ConcatenateBitmaps(bool vertical, params Bitmap[] bitmaps)
            {
                if (bitmaps == null || bitmaps.Length < 1)
                {
                    throw new ArgumentNullException("You must pass at least one bitmap for the concatenation to take place (null or no empty array passed in)");
                }

                int width = 0;
                int height = 0;
                PixelFormat pixelFormat = PixelFormat.Format1bppIndexed;

                // Check Parameters and 
                // Get the resulting Bitmap size and depth
                for (int t = 0; t < bitmaps.Length; t++)
                {
                    if (bitmaps[t] == null)
                    {
                        throw new ArgumentNullException("One of the bitmap (bitmap # " + (t+1) + ") passed in is null");
                    }

                    if (Bitmap.GetPixelFormatSize(bitmaps[t].PixelFormat) > Bitmap.GetPixelFormatSize(pixelFormat))
                    {
                        pixelFormat = bitmaps[t].PixelFormat;
                    }

                    if (vertical)
                    {
                        if (t == 0)
                        {
                            width = bitmaps[t].Width;
                        }
                        else
                        {
                            if (bitmaps[t].Width != width)
                            {
                                // @ Review : Should we pad other image (with Color.Empty) instead of throwing ?
                                throw new RenderingVerificationException("One of the bitmap (bitmap #" + (t + 1) + ") has a Width different than the previous ones");
                            }
                        }

                        height += bitmaps[t].Height;
                    }
                    else
                    {
                        if (t == 0)
                        {
                            height = bitmaps[t].Height;
                        }
                        else
                        {
                            if (bitmaps[t].Height != height)
                            {
                                // @ Review : Should we pad other image (with Color.Empty) instead of throwing ?
                                throw new RenderingVerificationException("One of the bitmap (bitmap #" + (t + 1) + ") has a Height different than the previous ones");
                            }
                        }

                        width += bitmaps[t].Width;
                    }
                }

                if (width == 0 || height == 0)
                {
                    throw new RenderingVerificationException("Cannot have a bitmap of width or heigth = 0");
                }

                Bitmap retVal = new Bitmap(width, height, pixelFormat);
                Graphics gr = Graphics.FromImage(retVal);
                Point pt = new Point(0, 0);

                for (int t = 0; t < bitmaps.Length; t++)
                {
                    gr.DrawImage(bitmaps[t], pt);
                    if (vertical)
                    {
                        pt = new Point(pt.X, pt.Y + bitmaps[t].Height);
                    }
                    else
                    {
                        pt = new Point(pt.X + bitmaps[t].Width, pt.Y);
                    }
                }

                gr.Dispose();
                return retVal;
            }
            /// <summary>
            /// Stretch the bitmap to the specified size
            /// NOTE : Caller is responsible for Disposing [in] and [out] Bitmaps
            /// </summary>
            /// <param name="originalBitmap">The bitmap to be stretched</param>
            /// <param name="newSize">(Size) The new Size of the Bitmap</param>
            /// <returns></returns>
            static public Bitmap Stretch(Bitmap originalBitmap, Size newSize)
            {
                //Check params
                if (originalBitmap == null)
                {
                    throw new ArgumentNullException("originalBitmap", "Value passed in must be a valid instance of a bitmap (null was passed in)");
                }
                if(newSize.IsEmpty)
                {
                    throw new RenderingVerificationException("Value passed in must be a valid instance of size ('Empty' size was passed in)");
                }
                if (newSize.Height < 1 || newSize.Width < 1)
                {
                    throw new ArgumentOutOfRangeException("Width and Height must be strictly positive (size='" + newSize.ToString() + "')");
                }

                // BUGBUG : Slow way of doing this, PInvoke into StretchBit instead (in-place modification).
                // ( Another option is to use a Transform : System.Drawing.Graphics or SpacialTransformFilter object)
                return new Bitmap(originalBitmap, newSize);
            }
            /// <summary>
            /// Get the Bounding box for a specific color
            /// </summary>
            /// <param name="bitmapToQuery">The bitmap to search</param>
            /// <param name="color">(Color) The color to find</param>
            /// <returns>(Rectangle) BoundingBox containing the color</returns>
            static public Rectangle GetBoundingBoxForColor(Bitmap bitmapToQuery, Color color)
            {
                //Check params
                if (bitmapToQuery == null)
                {
                    throw new ArgumentNullException("bitmapToQuery", "Value passed in must be a valid instance of a bitmap (null was passed in)");
                }

                Color col = Color.Empty;
                int xMax = int.MinValue;
                int xMin = int.MaxValue;
                int yMax = int.MinValue;
                int yMin = int.MaxValue;

                int width = bitmapToQuery.Width;
                int height = bitmapToQuery.Height;

                if (color == Color.Empty)
                {
                    const int ARBITRARY = 64000; // Arbitrary number of pixel: 320 * 200 = 64 000

                    // User wants background color for this bitmap
                    // "Monte-carlo" algorithm : Pick random pixel, the ones with the highest probabylity is assumed to be the background
                    int x = 0;
                    int y = 0;
                    Random rnd = new Random();
                    Hashtable colorFound = new Hashtable();
                    int totalPix = width * height;

                    if (totalPix > ARBITRARY)
                    {
                        // Get ARBITRARY or 10%, whatever is bigger.
                        totalPix = (int)((totalPix * 0.1 > ARBITRARY) ? totalPix * 0.1f : ARBITRARY);
                    }

                    for (int t = 0; t < ARBITRARY; t++)
                    {
                        x = rnd.Next(width);
                        y = rnd.Next(height);

                        Color colorRnd = bitmapToQuery.GetPixel(x, y);

                        if (colorFound.Contains(colorRnd))
                        {
                            colorFound[colorRnd] = ((int)colorFound[colorRnd]) + 1;
                        }
                        else
                        {
                            colorFound[colorRnd] = 1;
                        }
                    }

                    DictionaryEntry max = new DictionaryEntry(Color.Empty, 0);
                    IDictionaryEnumerator iter = colorFound.GetEnumerator();

                    while (iter.MoveNext())
                    {
                        if ((int)iter.Value > (int)max.Value)
                        {
                            max = iter.Entry;
                        }
                    }

                    // Find the Background color
                    color = (Color)max.Key;
                    colorFound.Clear();

                    // Look for anything but the background and return the bounding rect
                    for (int i = 0; i < height; i++)
                    {
                        for (int j = 0; j < width; j++)
                        {
                            col = bitmapToQuery.GetPixel(j, i);
                            if (col != color)
                            {
                                if (j > xMax) { xMax = j; }

                                if (j < xMin) { xMin = j; }

                                if (i > yMax) { yMax = i; }

                                if (i < yMin) { yMin = i; }
                            }
                        }
                    }
                }
                else
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            col = bitmapToQuery.GetPixel(x, y);
                            if (col == color)
                            {
                                if (x > xMax) { xMax = x; }

                                if (x < xMin) { xMin = x; }

                                if (y > yMax) { yMax = y; }

                                if (y < yMin) { yMin = y; }
                            }
                        }
                    }
                }

                return (xMax == int.MinValue) ? Rectangle.Empty : new Rectangle(xMin, yMin, xMax - xMin + 1, yMax - yMin + 1);
            }
            /// <summary>
            /// Convert an IImageAdapter into a System.Drawing.Bitmap
            /// NOTE : caller is responsible for Disposing the bitmap returned
            /// </summary>
            /// <param name="imageAdapter">The IImageAdapter to be converted</param>
            /// <returns>The Bitmap representation of the IImageAdapter</returns>
//            
            static public Bitmap ToBitmap(IImageAdapter imageAdapter)
            {
                if (imageAdapter == null) { return null; }

                return InternalConvertIImageAdapter(imageAdapter, 0, 0, imageAdapter.Width, imageAdapter.Height);
            }
            /// <summary>
            /// Convert part of an IImageAdapter into a System.Drawing.Bitmap
            /// Note : caller is responsible for Disposing the bitmap returned
            /// Note : API do not throw if pixel is out of bound, the point is just not plotted
            /// </summary>
            /// <param name="imageAdapter">The IImageAdapter to be converted</param>
            /// <param name="offsetX">The x offset on the imageadapter</param>
            /// <param name="offsetY">The y offset on the imageadapter</param>
            /// <param name="width">The width of the returned bitmap</param>
            /// <param name="height">The height of the returned bitmap</param>
            /// <returns>The Bitmap representation of the IImageAdapter</returns>
//            
            static public Bitmap ToBitmap(IImageAdapter imageAdapter, int offsetX, int offsetY, int width, int height)
            {
                if (imageAdapter == null) { return null; }
                return InternalConvertIImageAdapter(imageAdapter, offsetX, offsetY, width, height);
            }
            /// <summary>
            /// Convert a serie of pixel into a System.Drawing.Bitmap
            /// Note : caller is responsible for Disposing the bitmap returned
            /// Note : API do not throw if pixel is out of bound, the point is just not plotted
            /// </summary>
            /// <param name="pixels">An array of Pixel containing the position and color or the points</param>
            /// <param name="offsetX">The x offset</param>
            /// <param name="offsetY">The y offset</param>
            /// <param name="width">The width of the resulting bitmap</param>
            /// <param name="height">The Height of the resulting bitmap</param>
            /// <returns></returns>
            static public Bitmap ToBitmap(Pixel[] pixels, int offsetX, int offsetY, int width, int height)
            {
                return ToBitmap(pixels, offsetX, offsetY, width, height, ColorByte.Empty);
            }
            /// <summary>
            /// Convert a serie of pixel into a System.Drawing.Bitmap
            /// Note : caller is responsible for Disposing the bitmap returned
            /// Note : API do not throw if pixel is out of bound, the point is just not plotted
            /// </summary>
            /// <param name="pixels">An array of Pixel containing the position and color or the points</param>
            /// <param name="offsetX">The x offset</param>
            /// <param name="offsetY">The y offset</param>
            /// <param name="width">The width of the resulting bitmap</param>
            /// <param name="height">The Height of the resulting bitmap</param>
            /// <param name="bgColor">The background color (unassigned pixel)</param>
            /// <returns></returns>
            static public Bitmap ToBitmap(Pixel[] pixels, int offsetX, int offsetY, int width, int height, IColor bgColor)
            {
                return ToBitmap(pixels, offsetX, offsetY, width, height, bgColor, bgColor);
            }
            /// <summary>
            /// Convert a serie of pixel into a System.Drawing.Bitmap
            /// Note : caller is responsible for Disposing the bitmap returned
            /// Note : API do not throw if pixel is out of bound, the point is just not plotted
            /// Note : bgColor == fgColor is a special case where the color of the pixel will be used instead of the default color
            /// </summary>
            /// <param name="pixels">An array of Pixel containing the position and color or the points</param>
            /// <param name="offsetX">The x offset</param>
            /// <param name="offsetY">The y offset</param>
            /// <param name="width">The width of the resulting bitmap</param>
            /// <param name="height">The Height of the resulting bitmap</param>
            /// <param name="backgroundColor">The background color (unassigned pixel)</param>
            /// <param name="foregroundColor">Assigned pixels will be defaulted to this color</param>
            /// <returns></returns>
            static public Bitmap ToBitmap(Pixel[] pixels, int offsetX, int offsetY, int width, int height, IColor backgroundColor, IColor foregroundColor)
            {
                ImageAdapter ia = new ImageAdapter(width, height, backgroundColor);
                foreach(Pixel pixel in pixels)
                {
                    if (pixel.X - offsetX >= ia.Width || pixel.Y - offsetY >= ia.Height) 
                    {
                        continue;
                    }
                    if (backgroundColor == foregroundColor)
                    {
                        // Special case, original color will be used.
                        ia[pixel.X-offsetX , pixel.Y-offsetY] = (IColor)pixel.Color.Clone();
                    }
                    else
                    {
                        ia[pixel.X - offsetX, pixel.Y - offsetY] = foregroundColor;
                    }
                }

                return ImageUtility.ToBitmap(ia);
            }
            /// <summary>
            /// Save an IImageAdapter on disk as a Bitmap
            /// </summary>
            /// <param name="imageAdapter">The IImageAdapter to convert</param>
            /// <param name="fileName">The name of the file to create</param>
            static public void ToImageFile(IImageAdapter imageAdapter, string fileName)
            {
                if (imageAdapter == null)
                {
                    throw new ArgumentNullException("imageAdapter", "IImageAdapter passed in cannot be null");
                }

                ToImageFile(imageAdapter, fileName, ImageFormat.Png);
            }
            /// <summary>
            /// Save an IImageAdapter on disk under the specified format (png, jpeg, gif, ...)
            /// </summary>
            /// <param name="imageAdapter">The IImageAdapter to convert</param>
            /// <param name="fileName">The name of the file to create</param>
            /// <param name="imageFormat">The format to save the image as</param>
            static public void ToImageFile(IImageAdapter imageAdapter, string fileName, ImageFormat imageFormat)
            {
                if (imageAdapter == null)
                {
                    throw new ArgumentNullException("imageAdapter", "IImageAdapter passed in cannot be null");
                }

                FileStream fileStream = null;
                try
                {
                    fileStream = new FileStream(fileName, FileMode.Create);
                    ToImageStream(imageAdapter, fileStream, imageFormat);
                }
                finally
                {
                    if (fileStream != null) { fileStream.Close(); }
                }
            }
            /// <summary>
            /// Serialize an IImageAdapter as a Bitmap
            /// </summary>
            /// <param name="imageAdapter">The imageAdapter to serialize</param>
            /// <param name="stream">The stream containing the serialize bitmap</param>
//            
            static public void ToImageStream(IImageAdapter imageAdapter, Stream stream)
            {
                if (imageAdapter == null)
                {
                    throw new ArgumentNullException("imageAdapter", "IImageAdapter passed in cannot be null");
                }
                ToImageStream(imageAdapter, stream, ImageFormat.Bmp);
            }
            /// <summary>
            /// Serialize an IImageAdapter under a specific imageFormat
            /// </summary>
            /// <param name="imageAdapter">The imageAdapter to serialize</param>
            /// <param name="stream">The stream containing the serialize bitmap</param>
            /// <param name="imageFormat">The format in which the image will be saved</param>
//            
            static public void ToImageStream(IImageAdapter imageAdapter, Stream stream, ImageFormat imageFormat)
            {
                if (imageAdapter == null) { throw new ArgumentNullException("imageAdapter", "IImageAdapter passed in cannot be null"); }
                if (stream == null) { throw new ArgumentNullException("stream", "Stream passed in cannot be null"); }
                if (imageFormat == null) { throw new ArgumentNullException("imageFormat", "ImageFormat passed in cannot be null"); }
                if (imageAdapter.Width <= 0 || imageAdapter.Height <= 0) { throw new ArgumentOutOfRangeException("imageAdapter", "Width and Height must be strictly positive"); }
                if (stream.CanWrite == false) { throw new ArgumentException("Cannot write to stream (was the stream previously closed ?)"); }

                using (Bitmap bmp = InternalConvertIImageAdapter(imageAdapter, 0, 0, imageAdapter.Width, imageAdapter.Height))
                {
                    bmp.Save(stream, ImageFormat.Png);
                }
            }

            static internal IImageAdapter CopyImageAdapter(IImageAdapter background, IImageAdapter foreground, Point offset, Size size, bool useTransparency)
            {
                IImageAdapter retVal = (IImageAdapter)background.Clone();

                for (int y = 0; y < background.Height; y++)
                {
                    for (int x = 0; x < background.Width; x++)
                    {
                        if ( (x >= offset.X && y >= offset.Y) &&
                             (x < foreground.Width + offset.X && y < foreground.Height + offset.Y) )
                        {
                            if (foreground[x - offset.X, y - offset.Y].IsEmpty == false)
                            {
                                if (useTransparency && foreground[x - offset.X, y - offset.Y].ExtendedAlpha < 1.0)
                                {
                                    double fgAlpha = foreground[x - offset.X, y - offset.Y].Alpha;
                                    double bgAlpha = 1.0 - fgAlpha;
                                    retVal[x, y].ExtendedAlpha = Math.Max(fgAlpha, bgAlpha);
                                    retVal[x, y].ExtendedRed = bgAlpha * background[x, y].ExtendedRed + fgAlpha * foreground[x - offset.X, y - offset.Y].ExtendedRed;
                                    retVal[x, y].ExtendedGreen = bgAlpha * background[x, y].ExtendedGreen + fgAlpha * foreground[x - offset.X, y - offset.Y].ExtendedGreen;
                                    retVal[x, y].ExtendedBlue = bgAlpha * background[x, y].ExtendedBlue + fgAlpha * foreground[x - offset.X, y - offset.Y].ExtendedBlue;
                                }
                                else 
                                {
                                    retVal[x, y] = (IColor)foreground[x - offset.X, y - offset.Y].Clone();
                                }
                            }

                        }
                    }
                }
                return retVal;
            }
            static internal IImageAdapter ClipImageAdapter(IImageAdapter originalImage, Rectangle area)
            {
                if (originalImage == null) { throw new ArgumentNullException("originalImage", "A valid instance of an object implementing IImageAdapter must be used (null passed in)"); }

                // TBD : Should throw if requested width if height > original ?
                // For now fill with IColor.Empty if it occurs.
                IImageAdapter retVal = new ImageAdapter(area.Width, area.Height, ColorByte.Empty);

                for (int y = area.Top; y < area.Height + area.Top; y++)
                {
                    for (int x = area.Left; x < area.Width + area.Left; x++)
                    {
                        if (x < originalImage.Width && y < originalImage.Height)
                        { 
                            retVal[x - area.Left, y - area.Top] = (IColor)originalImage[x,y].Clone();
                        }
                    }
                }

                return retVal;
            }
            static internal void FillRect(IImageAdapter imageAdapter, Rectangle rect, IColor color)
            {
                if (imageAdapter == null) { throw new ArgumentNullException(); }
                if (rect == Rectangle.Empty) { throw new ArgumentException(); }
                if (color == null) { throw new ArgumentNullException(); }

                for (int y = rect.Top; y <= rect.Bottom; y++)
                {
                    for (int x = rect.Left; x <= rect.Right; x++)
                    {
                        if(x > 0 && x < imageAdapter.Width && y > 0 && y < imageAdapter.Height)
                        {
                            imageAdapter[x, y] = (ColorByte)imageAdapter[x, y] + (ColorByte)color;
                        }
                    }
                }
            }
            static internal void DrawRect(IImageAdapter imageAdapter, Rectangle rect, IColor color)
            {
                if (imageAdapter == null) { throw new ArgumentNullException(); }
                if (rect == Rectangle.Empty) { throw new ArgumentException(); }
                if (color == null) { throw new ArgumentNullException(); }

                for (int y = rect.Top; y <= rect.Bottom; y++)
                {
                    if (rect.Left > 0 & rect.Left < imageAdapter.Width && y > 0 && y < imageAdapter.Height) { imageAdapter[rect.Left, y] = (IColor)color.Clone(); }
                    if (rect.Right > 0 & rect.Right < imageAdapter.Width && y > 0 && y < imageAdapter.Height) { imageAdapter[rect.Right, y] = (IColor)color.Clone(); }
                }
                for (int x = rect.Left; x <= rect.Right; x++)
                {
                    if (rect.Top > 0 & rect.Top < imageAdapter.Height && x > 0 && x < imageAdapter.Width) { imageAdapter[x, rect.Top] = (IColor)color.Clone(); }
                    if (rect.Bottom > 0 & rect.Bottom < imageAdapter.Height && x > 0 && x < imageAdapter.Width) { imageAdapter[x, rect.Bottom] = (IColor)color.Clone(); }
                }
            }

/*     
            /// <summary>
            /// Retrieve if Dwm is currently running on this machine.
            /// </summary>
            /// <param name="hDwmModule">The Win32 hModule value (returned by LoadLibrary)</param>
            /// <returns></returns>
            static private bool DwmIsCompositionEnabled(IntPtr hDwmModule)
            {
                int enabled = 0;

                // Get API to retrieve if Dwm is running and call it.
                IntPtr pFunctionDwmRunning = Kernel32.GetProcAddress(hDwmModule, DWM_API_ISCOMPOSITIONENABLED);
                if (pFunctionDwmRunning == IntPtr.Zero) { throw new ExternalException("Couldn't find the '" + DWM_API_ISCOMPOSITIONENABLED + "' API in '" + DWMAPI_DLL + "'."); }
                DwmIsCompositionEnabledDelegate dwmIsCompositionEnabled = (DwmIsCompositionEnabledDelegate)Marshal.GetDelegateForFunctionPointer(pFunctionDwmRunning, typeof(DwmIsCompositionEnabledDelegate));
                if (dwmIsCompositionEnabled == null) { throw new MarshalDirectiveException("CLR internal error (Marshal.GetDelegateForFunctionPointer) returned no delegate !"); }
                int hresult = dwmIsCompositionEnabled(ref enabled);
                if (COM.Failed(hresult)) { throw new ExternalException("Native call to '" + DWM_API_ISCOMPOSITIONENABLED+ "' in library '" + DWMAPI_DLL + "' failed (HRESULT = 0x" + hresult.ToString("x") + ")"); }

                return (enabled != 0);
            }
            /// <summary>
            /// LoadLibrary 'DwmApi.dll' and invoke 'DwmCaptureDesktop' to get the bitmap.
            /// Note : Assuming the Dwm service is running.
            /// </summary>
            /// <param name="hDwmModule">The Win32 hModule value (returned by LoadLibrary)</param>
            /// <param name="rectangle">The area of the screen to capture</param>
            /// <returns>The screen bitmap</returns>
            static private Bitmap DirectXScreenCapture(IntPtr hDwmModule, Rectangle rectangle)
            {
                Bitmap retVal = null;
                IntPtr pBaseAddress = IntPtr.Zero;
                IntPtr hBits = IntPtr.Zero;
                D3DFORMAT bitsFormat = 0;

                System.Diagnostics.Debug.Assert(System.Environment.OSVersion.Version.Major > 5, "Dwm not supported to OS < LH");

                // Dwm is running, Get the capture API and call it
                IntPtr pFunctionCaptureName = Kernel32.GetProcAddress(hDwmModule, DWM_API_CAPTURE_NAME);
                if (pFunctionCaptureName == IntPtr.Zero || Marshal.GetLastWin32Error() != 0) { throw new ExternalException("Couldn't find the '" + DWM_API_ISCOMPOSITIONENABLED + "' API in '" + DWMAPI_DLL + "'. Are you running on a platform > XP ? (if no, use the 'StandardScreenCapture' API instead of this one)"); }
                DwmCaptureDesktopDelegate dwmCaptureDesktop = (DwmCaptureDesktopDelegate)Marshal.GetDelegateForFunctionPointer(pFunctionCaptureName, typeof(DwmCaptureDesktopDelegate));
                if (dwmCaptureDesktop == null) { throw new MarshalDirectiveException("CLR internal error (Marshal.GetDelegateForFunctionPointer) returned no delegate !"); }
                int hresult = dwmCaptureDesktop((uint)rectangle.Left, (uint)rectangle.Top, (uint)rectangle.Width, (uint)rectangle.Height, ref bitsFormat, ref hBits);
                if (COM.Failed(hresult) || hBits == IntPtr.Zero) { throw new ExternalException("Native call to '" + DWM_API_CAPTURE_NAME + "' in library '" + DWMAPI_DLL + "' failed (HRESULT = 0x" + hresult.ToString("x") + ", hBits = 0x" + hBits.ToString("x") + ")"); }
                if (bitsFormat != D3DFMT_A8R8G8B8 && bitsFormat != D3DFMT_X8R8G8B8) { throw new ApplicationException("Unexpected pixel format returned from native call to '" + DWM_API_CAPTURE_NAME + "'"); }

                try
                {
                    // Mapfile to get the bit stream
                    pBaseAddress = Kernel32.MapViewOfFile(hBits, Kernel32.FILE_MAP_WRITE | Kernel32.FILE_MAP_READ, 0, 0, rectangle.Width * rectangle.Height * 4);
                    if (pBaseAddress == IntPtr.Zero) { throw new ExternalException("Native call to MapViewOfFile failed (error code : " + Marshal.GetLastWin32Error() + ")", Marshal.GetLastWin32Error()); }

                    // NOTE : cannot create using 'ctor Bitmap(w,h,srtide,scan0) because we need to UnmapViewOfFile (ie free memory), this will release the underlying bitstream
                    // Furthermore creating it and using Clone does NOT perform a deep copy (bit stream not copied, scan0 pointer copied)
                    using (ImageUtility imageUtility = new ImageUtility(rectangle.Width, rectangle.Height))
                    {
                        imageUtility.GetSetPixelUnsafeBegin();
                        byte[] bitStream = imageUtility.GetStreamBufferBGRA();
                        Marshal.Copy(pBaseAddress, bitStream, 0, bitStream.Length);
                        imageUtility.GetSetPixelUnsafeCommit();
                        retVal = (Bitmap)imageUtility.Bitmap32Bits.Clone();
                    }
                }
                finally
                {
                    // Clean up : UnmapViewOfFile
                    if (pBaseAddress != IntPtr.Zero) { Kernel32.UnmapViewOfFile(pBaseAddress); pBaseAddress = IntPtr.Zero; }
                }

                return retVal;

            }
*/
            /// <summary>
            /// Get the screen HDC, call CaptureBitmapFromDC and release the HDC
            /// </summary>
            /// <param name="rectangle">The area of the screen to capture</param>
            /// <returns>The screen bitmap</returns>
            static private Bitmap StandardScreenCapture(Rectangle rectangle)
            {
                Bitmap retVal = null;
                IntPtr hdcSrc = IntPtr.Zero;

                try
                {
                    // Get the Screen DC (source DC)
                    hdcSrc = Gdi32.CreateDC("DISPLAY", string.Empty, string.Empty, IntPtr.Zero);
                    if (hdcSrc == IntPtr.Zero)
                    {
                        throw new ExternalException("Win32 API 'CreateDC' failed in ImageUtility::InternalCapture");
                    }

                    retVal = ImageUtility.CaptureBitmapFromDC(hdcSrc, rectangle);
                }
                finally
                {
                    if (hdcSrc != IntPtr.Zero)
                    {
                        Gdi32.DeleteDC(hdcSrc);
                        hdcSrc = IntPtr.Zero;
                    }
                }

                return retVal;
            }
            /// <summary>
            /// Get a windows standard screen capture or a DirectX one depending on what platform it runs on.
            /// </summary>
            /// <param name="rectangle">The area of the screen to capture</param>
            /// <returns>The screen bitmap</returns>
            static internal Bitmap InternalScreenCapture(Rectangle rectangle)
            {
/*
                Bitmap retVal = null;

                if (System.Environment.OSVersion.Version.Major > 5)
                {
                    IntPtr hDwmModule = IntPtr.Zero;
                    try
                    {
                        hDwmModule = Kernel32.LoadLibrary(DWMAPI_DLL);
                        if (hDwmModule == IntPtr.Zero) { throw new ExternalException("'" + DWMAPI_DLL + "' not found although OS >= LH"); }
                        if (DwmIsCompositionEnabled(hDwmModule) == true) { retVal = DirectXScreenCapture(hDwmModule, rectangle); }
                    }
                    finally
                    {
                        if (hDwmModule != IntPtr.Zero) { Kernel32.FreeLibrary(hDwmModule); hDwmModule = IntPtr.Zero; }
                    }
                }

                return (retVal == null) ? StandardScreenCapture(rectangle) : retVal;
*/
                return StandardScreenCapture(rectangle);
            }
            /// <summary>
            /// Convert an object implementing IImageAdapter into an ImageUtility
            /// Note : caller is responsible for Disposing the bitmap returned
            /// Note : API do not throw if pixel is out of bound, the point is just not plotted
            /// </summary>
            /// <param name="imageAdapter">The imageAdapter to convert</param>
            /// <param name="offsetX">The x offset</param>
            /// <param name="offsetY">The y offset</param>
            /// <param name="width">The width of the included bitmap</param>
            /// <param name="height">The height of the included bitmap</param>
            /// <returns></returns>
            static private Bitmap InternalConvertIImageAdapter(IImageAdapter imageAdapter, int offsetX, int offsetY, int width, int height)
            {
                int index = 0;
                IColor color = null;
                Bitmap retVal = null;
                using (ImageUtility imageUtility = new ImageUtility(width, height))
                {
                    imageUtility.GetSetPixelUnsafeBegin();
                    byte[] buffer = imageUtility.GetStreamBufferBGRA();
                    for (int y = offsetY; (y < offsetY + height) && (y < imageAdapter.Height); y++)
                    {
                        for (int x = offsetX; (x < offsetX + width) && (x < imageAdapter.Width); x++)
                        {
                            color = imageAdapter[x, y];
                            index = (x - offsetX + (y - offsetY) * width) * 4;
                            buffer[index] = color.B;
                            buffer[index + 1] = color.G;
                            buffer[index + 2] = color.R;
                            buffer[index + 3] = color.A;
                        }
                    }
                    imageUtility.GetSetPixelUnsafeCommit();

                    // Copy Image Property (DPI / Copyright / ...)
                    if (imageAdapter.Metadata != null)
                    {
                        PropertyItem[] propertyItems = imageAdapter.Metadata.PropertyItems;
                        foreach (PropertyItem item in propertyItems)
                        {
                            imageUtility.Bitmap32Bits.SetPropertyItem(item);
                        }

//                        // Convert to requested PixelFormat
//                        PixelFormat pixelFormat = MetadataInfoHelper.GetPixelFormat(imageAdapter.Metadata);
//                        if (Bitmap.GetPixelFormatSize(pixelFormat) != 32)
//                        {
//                            retVal = ImageUtility.ConvertPixelFormat(imageUtility.Bitmap32Bits, pixelFormat);
//                        }
                    }
                    retVal = (Bitmap)imageUtility.Bitmap32Bits.Clone();

                }

                return retVal;
            }
        #endregion Static Public Methods

        #region IDisposable Members
            /// <summary>
            /// Releases all resources used by this object
            /// </summary>
            public void Dispose()
            {
                if (_disposed == false)
                {
                    _disposed = true;
                    if (_bmpDoNotUse != null)
                    {
                        _bmpDoNotUse.Dispose();
                        _bmpDoNotUse = null;
                    }
                    GC.SuppressFinalize(this);
                }
            }
        #endregion

        #region IImageUtilUnmanaged extra stuff
            void IImageUtilityUnmanaged.ToImageFile(IImageAdapterUnmanaged image, string fileName)
            { 
                ImageUtility.ToImageFile((ImageAdapter)image, fileName);
            }
            IImageAdapterUnmanaged IImageUtilityUnmanaged.ScreenSnapshotRc (int x, int y, int width, int height)
            {
                IImageAdapterUnmanaged retVal = null;
                using (Bitmap screen = ImageUtility.CaptureScreen(new Rectangle(x, y, width, height)))
                {
                    retVal = new ImageAdapter(screen);
                }
                return retVal;
            }
            IImageAdapterUnmanaged IImageUtilityUnmanaged.ScreenSnapshotDc(IntPtr HDC, NativeStructs.RECT areaToCopy)
            {
                IImageAdapterUnmanaged retVal = null;
                using (Bitmap screen = ImageUtility.CaptureBitmapFromDC(HDC, new Rectangle(areaToCopy.left, areaToCopy.top, /*needed for BIDI OS*/Math.Abs(areaToCopy.right - areaToCopy.left), areaToCopy.bottom - areaToCopy.top)))
                {
                    retVal = new ImageAdapter(screen);
                }
                return retVal;
            }
            IImageAdapterUnmanaged IImageUtilityUnmanaged.ScreenSnapshotWnd(IntPtr HWND, bool clientAreaOnly)
            {
                IImageAdapterUnmanaged retVal = null;
                using (Bitmap screen = ImageUtility.CaptureScreen(HWND, clientAreaOnly))
                {
                    retVal = new ImageAdapter(screen);
                }
                return retVal;
            }
        #endregion IImageUtilUnmanaged extra stuff
    }
}
