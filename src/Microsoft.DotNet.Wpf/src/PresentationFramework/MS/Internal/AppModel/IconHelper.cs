// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Static internal class implements utility functions for icon
//              implementation for the Window class.
//

using System;
using System.Security;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.ComponentModel;

using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Media;

using MS.Internal;
using MS.Internal.Interop;
using MS.Internal.PresentationFramework;                   // SecurityHelper
using MS.Win32;

namespace MS.Internal.AppModel
{
    internal static class IconHelper
    {
        private static Size s_smallIconSize;
        private static Size s_iconSize;
        private static int s_systemBitDepth;

        /// Lazy init of static fields.  Call this at the beginning of any external entrypoint.
        private static void EnsureSystemMetrics()
        {
            if (s_systemBitDepth == 0)
            {
                // The values here *may* change, but it's not worthwhile to requery.

                // We need to release the DC to correctly track our native handles.
                var hdcDesktop = new HandleRef(null, UnsafeNativeMethods.GetDC(new HandleRef()));
                try
                {
                    int sysBitDepth = UnsafeNativeMethods.GetDeviceCaps(hdcDesktop, NativeMethods.BITSPIXEL);
                    sysBitDepth *= UnsafeNativeMethods.GetDeviceCaps(hdcDesktop, NativeMethods.PLANES);

                    // If the s_systemBitDepth is 8, make it 4.  Why?  Because windows does not
                    // choose a 256 color icon if the display is running in 256 color mode
                    // because of palette flicker.  
                    if (sysBitDepth == 8)
                    {
                        sysBitDepth = 4;
                    }

                    // We really want to be pixel aware here.  Don't use the SystemParameters class.
                    int cxSmallIcon = UnsafeNativeMethods.GetSystemMetrics(SM.CXSMICON);
                    int cySmallIcon = UnsafeNativeMethods.GetSystemMetrics(SM.CYSMICON);
                    int cxIcon = UnsafeNativeMethods.GetSystemMetrics(SM.CXICON);
                    int cyIcon = UnsafeNativeMethods.GetSystemMetrics(SM.CYICON);

                    s_smallIconSize = new Size(cxSmallIcon, cySmallIcon);
                    s_iconSize = new Size(cxIcon, cyIcon);
                    s_systemBitDepth = sysBitDepth;
                }
                finally
                {
                    UnsafeNativeMethods.ReleaseDC(new HandleRef(), hdcDesktop);
                }
            }
        }

        /// <returns></returns>
        public static void GetDefaultIconHandles(out NativeMethods.IconHandle largeIconHandle, out NativeMethods.IconHandle smallIconHandle)
        {
            largeIconHandle = null;
            smallIconHandle = null;


            // Get the handle of the module that created the running process.
            string iconModuleFile = UnsafeNativeMethods.GetModuleFileName(new HandleRef());

            // We don't really care about the return value.  Handles will be invalid on error.
            int extractedCount = UnsafeNativeMethods.ExtractIconEx(iconModuleFile, 0, out largeIconHandle, out smallIconHandle, 1);
        }

        public static void GetIconHandlesFromImageSource(ImageSource image, out NativeMethods.IconHandle largeIconHandle, out NativeMethods.IconHandle smallIconHandle)
        {
            EnsureSystemMetrics();
            largeIconHandle = CreateIconHandleFromImageSource(image, s_iconSize);
            smallIconHandle = CreateIconHandleFromImageSource(image, s_smallIconSize);
        }

        /// <returns>A new HICON based on the image source</returns>
        public static NativeMethods.IconHandle CreateIconHandleFromImageSource(ImageSource image, Size size)
        {
            EnsureSystemMetrics();

            bool asGoodAsItGets = false;

            var bf = image as BitmapFrame;
            if (bf?.Decoder?.Frames != null)
            {
                bf = GetBestMatch(bf.Decoder.Frames, size);

                // If this was actually a multi-framed icon then we don't want to do any corrections.
                //   Let Windows do its thing.  We don't want to unnecessarily deviate from the system.
                // If this was a jpeg or png, then we're doing something Windows doesn't do,
                //   and we can be better. (unless it was a perfect match :)
                asGoodAsItGets = bf.Decoder is IconBitmapDecoder // i.e. was this a .ico?
                    || bf.PixelWidth == size.Width && bf.PixelHeight == size.Height;

                image = bf;
            }

            if (!asGoodAsItGets)
            {
                // Unless this was a .ico, render it into a new BitmapFrame with the appropriate dimensions
                // to preserve the aspect ratio in the HICON and do the appropriate padding.
                bf = BitmapFrame.Create(GenerateBitmapSource(image, size));
            }

            return CreateIconHandleFromBitmapFrame(bf);
        }

        /// <summary>
        /// Creates a BitmapSource from an arbitrary ImageSource.
        /// </summary>
        private static BitmapSource GenerateBitmapSource(ImageSource img, Size renderSize)
        {
            // By now we should just assume it's a vector image that we need to rasterize.
            // We want to keep the aspect ratio, but one of the dimensions will go the full length.
            var drawingDimensions = new Rect(0, 0, renderSize.Width, renderSize.Height);

            // There's no reason to assume that the requested image dimensions are square.
            double renderRatio = renderSize.Width / renderSize.Height;
            double aspectRatio = img.Width / img.Height;

            // If it's smaller than the requested size, then place it in the middle and pad the image.
            if (img.Width <= renderSize.Width && img.Height <= renderSize.Height)
            {
                drawingDimensions = new Rect((renderSize.Width - img.Width) / 2, (renderSize.Height - img.Height) / 2, img.Width, img.Height);
            }
            else if (renderRatio > aspectRatio)
            {
                double scaledRenderWidth = (img.Width / img.Height) * renderSize.Width;
                drawingDimensions = new Rect((renderSize.Width - scaledRenderWidth) / 2, 0, scaledRenderWidth, renderSize.Height);
            }
            else if (renderRatio < aspectRatio)
            {
                double scaledRenderHeight = (img.Height / img.Width) * renderSize.Height;
                drawingDimensions = new Rect(0, (renderSize.Height - scaledRenderHeight) / 2, renderSize.Width, scaledRenderHeight);
            }

            var dv = new DrawingVisual();
            DrawingContext dc = dv.RenderOpen();
            dc.DrawImage(img, drawingDimensions);
            dc.Close();

            // Need to use Pbgra32 because that's all that RenderTargetBitmap currently supports.
            // 96 is the right DPI to use here because we're being very pixel aware.
            var bmp = new RenderTargetBitmap((int)renderSize.Width, (int)renderSize.Height, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(dv);

            return bmp;
        }

        /// <returns></returns>
        //
        //  Creates and HICON from a bitmap frame
        private static NativeMethods.IconHandle CreateIconHandleFromBitmapFrame(BitmapFrame sourceBitmapFrame)
        {
            Invariant.Assert(sourceBitmapFrame != null, "sourceBitmapFrame cannot be null here");

            BitmapSource bitmapSource = sourceBitmapFrame;

            if (bitmapSource.Format != PixelFormats.Bgra32 && bitmapSource.Format != PixelFormats.Pbgra32)
            {
                bitmapSource = new FormatConvertedBitmap(bitmapSource, PixelFormats.Bgra32, null, 0.0);
            }

            // data used by CopyPixels
            int w = bitmapSource.PixelWidth;
            int h = bitmapSource.PixelHeight;
            int bpp = bitmapSource.Format.BitsPerPixel;
            // ensuring it is in 4 byte increments since we're dealing
            // with ARGB fromat
            int stride = (bpp * w + 31) / 32 * 4;
            int sizeCopyPixels = stride * h;
            byte[] xor = new byte[sizeCopyPixels];
            bitmapSource.CopyPixels(xor, stride, 0);

            return CreateIconCursor(xor, w, h, 0, 0, true);
        }

        // Also used by PenCursorManager
        // Creates a 32 bit per pixel Icon or cursor.  This code is moved from framework\ms\internal\ink\pencursormanager.cs
        internal static NativeMethods.IconHandle CreateIconCursor(
            byte[] colorArray,
            int width,
            int height,
            int xHotspot,
            int yHotspot,
            bool isIcon)
        {
            //   1. We are going to generate a WIN32 color bitmap which represents the color cursor.
            //   2. Then we need to create a monochrome bitmap which is used as the cursor mask.
            //   3. At last we create a WIN32 HICON from the above two bitmaps
            NativeMethods.BitmapHandle colorBitmap = null;
            NativeMethods.BitmapHandle maskBitmap = null;

            try
            {
                // 1) Create the color bitmap using colorArray
                // Fill in the header information
                NativeMethods.BITMAPINFO bi = new NativeMethods.BITMAPINFO(
                                                    width,      // width
                                                    -height,    // A negative value indicates the bitmap is top-down DIB
                                                    32          // biBitCount
                                                    );
                bi.bmiHeader_biCompression = NativeMethods.BI_RGB;

                IntPtr bits = IntPtr.Zero;
                colorBitmap = MS.Win32.UnsafeNativeMethods.CreateDIBSection(
                                        new HandleRef(null, IntPtr.Zero),   // A device context. Pass null in if no DIB_PAL_COLORS is used.
                                        ref bi,                             // A BITMAPINFO structure which specifies the dimensions and colors.
                                        NativeMethods.DIB_RGB_COLORS,       // Specifies the type of data contained in the bmiColors array member of the BITMAPINFO structure
                                        ref bits,                           // An out Pointer to a variable that receives a pointer to the location of the DIB bit values
                                        null,                        // Handle to a file-mapping object that the function will use to create the DIB. This parameter can be null.
                                        0                                   // dwOffset. This value is ignored if hSection is NULL
                                        );

                if (colorBitmap.IsInvalid || bits == IntPtr.Zero)
                {
                    // Note we will release the GDI resources in the finally block.
                    return NativeMethods.IconHandle.GetInvalidIcon();
                }

                // Copy the color bits to the win32 bitmap
                Marshal.Copy(colorArray, 0, bits, colorArray.Length);


                // 2) Now create the mask bitmap which is monochrome
                byte[] maskArray = GenerateMaskArray(width, height, colorArray);
                Invariant.Assert(maskArray != null);

                maskBitmap = UnsafeNativeMethods.CreateBitmap(width, height, 1, 1, maskArray);
                if (maskBitmap.IsInvalid)
                {
                    // Note we will release the GDI resources in the finally block.
                    return NativeMethods.IconHandle.GetInvalidIcon();
                }

                // Now create HICON from two bitmaps.
                NativeMethods.ICONINFO iconInfo = new NativeMethods.ICONINFO();
                iconInfo.fIcon = isIcon;            // fIcon == ture means creating an Icon, otherwise Cursor
                iconInfo.xHotspot = xHotspot;
                iconInfo.yHotspot = yHotspot;
                iconInfo.hbmMask = maskBitmap;
                iconInfo.hbmColor = colorBitmap;

                return UnsafeNativeMethods.CreateIconIndirect(iconInfo);
            }
            finally
            {
                if (colorBitmap != null)
                {
                    colorBitmap.Dispose();
                    colorBitmap = null;
                }

                if (maskBitmap != null)
                {
                    maskBitmap.Dispose();
                    maskBitmap = null;
                }
            }
        }

        // generates the mask array for the input colorArray.
        // The mask array is 1 bpp
        private static byte[] GenerateMaskArray(int width, int height, byte[] colorArray)
        {
            int nCount = width * height;

            // NOTICE-2005/04/26-WAYNEZEN,
            // Check out the notes in CreateBitmap in MSDN. The scan line has to be aliged to WORD.
            int bytesPerScanLine = AlignToBytes(width, 2) / 8;

            byte[] bitsMask = new byte[bytesPerScanLine * height];

            // We are scaning all pixels in color bitmap.
            // If the alpha value is 0, we should set the corresponding mask bit to 1. So the pixel will show
            // the screen pixel. Otherwise, we should set the mask bit to 0 which causes the cursor to display
            // the color bitmap pixel.
            for (int i = 0; i < nCount; i++)
            {
                // Get the i-th pixel position (hPos, vPos)
                int hPos = i % width;
                int vPos = i / width;

                // For each byte in 2-bit color bitmap, the lowest the bit represents the right-most display pixel.
                // For example the bollow mask -
                //    1 1 1 0 0 0 0 1
                //    ^             ^
                //  offsetBit = 0x80   offsetBit = 0x01
                int byteIndex = hPos / 8;
                byte offsetBit = (byte)(0x80 >> (hPos % 8));

                // Now we turn the mask on or off accordingly.
                if (colorArray[i * 4 + 3] /* Alpha value since it's in Argb32 Format */ == 0x00)
                {
                    // Set the mask bit to 1.
                    bitsMask[byteIndex + bytesPerScanLine * vPos] |= (byte)offsetBit;
                }
                else
                {
                    // Reset the mask bit to 0
                    bitsMask[byteIndex + bytesPerScanLine * vPos] &= (byte)(~offsetBit);
                }

                // Since the scan line of the mask bitmap has to be aligned to word. We have set all padding bits to 1.
                // So the extra pixel can be seen through.
                if (hPos == width - 1 && width == 8)
                {
                    bitsMask[1 + bytesPerScanLine * vPos] = 0xff;
                }
            }

            return bitsMask;
        }

        // Also used by PenCursorManager
        /// <summary>
        /// Calculate the bits count aligned to N-Byte based on the input count
        /// </summary>
        /// <param name="original">The original value</param>
        /// <param name="nBytesCount">N-Byte</param>
        /// <returns>the nearest bit count which is aligned to N-Byte</returns>
        internal static int AlignToBytes(double original, int nBytesCount)
        {
            Debug.Assert(nBytesCount > 0, "The N-Byte has to be greater than 0!");

            int nBitsCount = 8 << (nBytesCount - 1);
            return (((int)Math.Ceiling(original) + (nBitsCount - 1)) / nBitsCount) * nBitsCount;
        }

        ///
        /// We're copying the algorithm Windows uses to pick icons.
        /// The comments and implementation are based on core\ntuser\client\clres.c
        ///
        /// MatchImage
        /// 
        /// This function takes LPINTs for width & height in case of "real size".
        /// For this option, we use dimensions of 1st icon in resdir as size to
        /// load, instead of system metrics.
        /// Returns a number that measures how "far away" the given image is
        /// from a desired one.  The value is 0 for an exact match.  Note that our
        /// formula has the following properties:
        ///     (1) Differences in width/height count much more than differences in
        ///             color format.
        ///     (2) Bigger images are better than smaller, since shrinking produces
        ///             better results than stretching.
        ///     (3) Color matching is done by the difference in bit depth.  No
        ///             preference is given to having a candidate equally different
        ///             above and below the target.
        ///
        /// The formula is the sum of the following terms:
        ///     abs(bppCandidate - bppTarget)
        ///     abs(cxCandidate - cxTarget), times 2 if the image is
        ///        narrower than what we'd like.  This is because we will get a
        ///        better result when consolidating more information into a smaller
        ///        space, than when extrapolating from less information to more.
        ///     abs(cxCandidate - cxTarget), times 2 if the image is
        ///        shorter than what we'd like.  This is for the same reason as
        ///        the width.
        ///
        /// Let's step through an example.  Suppose we want a 4bpp (16 color),
        /// 32x32 image.  We would choose the various candidates in the following order:
        ///
        /// Candidate     Score   Formula
        /// 
        /// 32x32x4bpp  = 0       abs(32-32)*1 + abs(32-32)*1 + 2*abs(4-4)*1
        /// 32x32x2bpp  = 4
        /// 32x32x8bpp  = 8
        /// 32x32x16bpp = 24
        /// 48x48x4bpp  = 32
        /// 48x48x2bpp  = 36
        /// 48x48x8bpp  = 40
        /// 32x32x32bpp = 56
        /// 48x48x16bpp = 56      abs(48-32)*1 + abs(48-32)*1 + 2*abs(16-4)*1
        /// 16x16x4bpp  = 64
        /// 16x16x2bpp  = 68      abs(16-32)*2 + abs(16-32)*2 + 2*abs(2-4)*1
        /// 16x16x8bpp  = 72
        /// 48x48x32bpp = 88      abs(48-32)*1 + abs(48-32)*1 + 2*abs(32-4)*1
        /// 16x16x16bpp = 88
        /// 16x16x32bpp = 104
        private static int MatchImage(BitmapFrame frame, Size size, int bpp)
        {
            /*
             * Here are the rules for our "match" formula:
             *      (1) A close size match is much preferable to a color match
             *      (2) Bigger icons are better than smaller
             *      (3) The smaller the difference in bit depths the better
             */
            int score = 2 * MyAbs(bpp, s_systemBitDepth, false) +
                    MyAbs(frame.PixelWidth, (int)size.Width, true) +
                    MyAbs(frame.PixelHeight, (int)size.Height, true);

            return score;
        }

        ///
        /// MyAbs (also from core\ntuser\client\clres.c)
        ///
        /// Calcules my weighted absolute value of the difference between 2 nums.
        /// This of course normalizes values to >= zero.  But it can also "punish" the
        /// returned value by a factor of two if valueHave < valueWant.  This is
        /// because you get worse results trying to extrapolate from less info up then
        /// interpolating from more info down.
        ///
        private static int MyAbs(int valueHave, int valueWant, bool fPunish)
        {
            int diff = (valueHave - valueWant);

            if (diff < 0)
            {
                diff = (fPunish ? -2 : -1) * diff;
            }

            return diff;
        }

        /// From a list of BitmapFrames find the one that best matches the requested dimensions.
        /// The methods used here are copied from Win32 sources.  We want to be consistent with
        /// system behaviors.
        private static BitmapFrame GetBestMatch(ReadOnlyCollection<BitmapFrame> frames, Size size)
        {
            Invariant.Assert(size.Width != 0, "input param width should not be zero");
            Invariant.Assert(size.Height != 0, "input param height should not be zero");

            int bestScore = int.MaxValue;
            int bestBpp = 0;
            int bestIndex = 0;

            bool isBitmapIconDecoder = frames[0].Decoder is IconBitmapDecoder;

            for (int i = 0; i < frames.Count && bestScore != 0; ++i)
            {
                // determine the bit-depth (# of colors) in the
                // current frame
                //
                // if the icon is palettized, Format.BitsPerPixel gives
                // the # of bits required to index into the palette (thus,
                // the # of colors in the palette).  If it is a true
                // color icon, it gives the # of bits required to support
                // true colors.
                // For icons, get the Format from the Thumbnail rather than from the
                // BitmapFrame directly because the unmanaged icon decoder 
                // converts every icon to 32-bit. Thumbnail.Format.BitsPerPixel
                // will give us the original bit depth.
                int currentIconBitDepth = isBitmapIconDecoder ? frames[i].Thumbnail.Format.BitsPerPixel : frames[i].Format.BitsPerPixel;

                // If it looks like nothing is specified at this point, assume a bpp of 8.
                if (currentIconBitDepth == 0)
                {
                    currentIconBitDepth = 8;
                }

                int score = MatchImage(frames[i], size, currentIconBitDepth);
                if (score < bestScore)
                {
                    bestIndex = i;
                    bestBpp = currentIconBitDepth;
                    bestScore = score;
                }
                else if (score == bestScore)
                {
                    // Tie breaker: choose the higher color depth.  If that fails, choose first one.
                    if (bestBpp < currentIconBitDepth)
                    {
                        bestIndex = i;
                        bestBpp = currentIconBitDepth;
                    }
                }
            }

            return frames[bestIndex];
        }
    }
}

