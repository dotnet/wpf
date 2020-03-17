// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon.Primitives
#else
namespace Microsoft.Windows.Controls.Ribbon.Primitives
#endif
{
    public class RibbonWindowSmallIconConverter : IValueConverter
    {
        #region IValueConverter Members

        /// <summary>
        ///     Returns small icon size variant (if available) of given icon.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ImageSource imageSource = value as ImageSource;
            ImageSource returnImageSource = null;
            if (imageSource != null)
            {
                returnImageSource = GetSmallIconImageSource(imageSource);

                if (returnImageSource == null)
                {
                    returnImageSource = imageSource;
                }
            }
            return returnImageSource;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Helper Methods

        private ImageSource GetSmallIconImageSource(ImageSource imageSource)
        {
#if RIBBON_IN_FRAMEWORK
            Size size = new Size(SystemParameters.SmallIconWidth, SystemParameters.SmallIconHeight);
#else
            Size size = Microsoft.Windows.Shell.SystemParameters2.Current.SmallIconSize;
#endif
            bool asGoodAsItGets = false;

            var bf = imageSource as BitmapFrame;
            if (bf != null && bf.Decoder != null && bf.Decoder.Frames != null && bf.Decoder.Frames.Count > 0)
            {
                bf = GetBestMatch(bf.Decoder.Frames, size);

                // If this was actually a multi-framed icon then we don't want to do any corrections.
                // Let Windows do its thing.  We don't want to unnecessarily deviate from the system.
                // If this was a jpeg or png, then we're doing something Windows doesn't do,
                // and we can be better. (unless it was a perfect match)
                asGoodAsItGets = bf.Decoder is IconBitmapDecoder // i.e. was this a .ico?
                    || bf.PixelWidth == size.Width && bf.PixelHeight == size.Height;

                imageSource = bf;
            }

            if (!asGoodAsItGets)
            {
                // Unless this was a .ico, render it into a new BitmapFrame with the appropriate dimensions
                // to preserve the aspect ratio in the HICON and do the appropriate padding.
                bf = BitmapFrame.Create(GenerateBitmapSource(imageSource, size));
            }
            return bf;
        }

        /// From a list of BitmapFrames find the one that best matches the requested dimensions.
        /// The methods used here are copied from Win32 sources.  We want to be consistent with
        /// system behaviors.
        private static BitmapFrame GetBestMatch(ReadOnlyCollection<BitmapFrame> frames, Size size)
        {
            Debug.Assert(size.Width != 0, "input param width should not be zero");
            Debug.Assert(size.Height != 0, "input param height should not be zero");

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

        ///
        /// This is the algorithm Windows uses to pick icons.
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
            int score = 2 * WeightedAbs(bpp, GetBitDepth(), false) +
                    WeightedAbs(frame.PixelWidth, (int)size.Width, true) +
                    WeightedAbs(frame.PixelHeight, (int)size.Height, true);

            return score;
        }

        /// Calcules custom weighted absolute value of the difference between 2 nums.
        /// This of course normalizes values to >= zero.  But it can also "punish" the
        /// returned value by a factor of two if valueHave < valueWant.  This is
        /// because you get worse results trying to extrapolate from less info up then
        /// interpolating from more info down.
        private static int WeightedAbs(int valueHave, int valueWant, bool fPunish)
        {
            int diff = (valueHave - valueWant);

            if (diff < 0)
            {
                diff = (fPunish ? -2 : -1) * diff;
            }

            return diff;
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

        private static int GetBitDepth()
        {
            if (s_systemBitDepth == 0)
            {
                var hdcDesktop = new HandleRef(null, NativeMethods.GetDC(new HandleRef()));
                try
                {
                    int sysBitDepth = NativeMethods.GetDeviceCaps(hdcDesktop, (int)NativeMethods.DeviceCap.BITSPIXEL);
                    sysBitDepth *= NativeMethods.GetDeviceCaps(hdcDesktop, (int)NativeMethods.DeviceCap.PLANES);

                    // If the s_systemBitDepth is 8, make it 4.  Why?  Because windows does not
                    // choose a 256 color icon if the display is running in 256 color mode
                    // because of palette flicker.  
                    if (sysBitDepth == 8)
                    {
                        sysBitDepth = 4;
                    }
                    s_systemBitDepth = sysBitDepth;
                }
                finally
                {
                    NativeMethods.ReleaseDC(new HandleRef(), hdcDesktop);
                }
            }
            return s_systemBitDepth;
        }

        #endregion

        #region Data
        private static int s_systemBitDepth; // = 0;
        #endregion
    }
}
