// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Test.Imaging
{
    #region Namespaces.

    using System;
    using System.Collections;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Security.Permissions;
    using System.Text;
    using System.Windows;
    using Media = System.Windows.Media;

    #endregion Namespaces.

    /// <summary>Encapsulates pure operations on bitmaps.</summary>
    /// <remarks>
    /// Many of the methods require running unsafe code. When
    /// this happens, the public method asserts the required permissions
    /// and then invokes an unsafe private method.
    /// </remarks>
    public class BitmapUtils
    {
        #region Constructors.

        // Prevent creation of instances of this class.
        private BitmapUtils() {}

        #endregion Constructors.

        #region Private methods.

        private unsafe static bool UnsafeCheckCaretItalic(Bitmap caretBitmap)
        {
            bool blackPixelFoundInColumn;
            int blackColumnStart, blackColumnEnd;

            System.Drawing.Imaging.BitmapData data = BitmapUtils.LockBitmapDataRead(caretBitmap);
            int width = PixelData.GetScanLineWidth(caretBitmap.Size);

            blackColumnStart = blackColumnEnd = -1;            
            //The logic here is to find the column index (blackColumnStart) where we first find a 
            //black pixel in its column, and then last column index (blackColumnEnd) where we are 
            //continuously able to find black pixels in columns.
            //Since the width of italic caret should be >= 2, we return true if the difference between 
            //both the indexes is >= 2
            try
            {
                for (int i = 0; i < caretBitmap.Width; i++)
                {
                    blackPixelFoundInColumn = false;
                    for (int j = 0;j < caretBitmap.Height; j++)
                    {
                        PixelData* pixel = (PixelData*)
                            ((byte*)data.Scan0.ToPointer() + (j * width));
                        pixel += i;
                        bool isBlack = (pixel->red == 0);
                        if (isBlack)
                        {
                            blackPixelFoundInColumn = true;
                            break;
                        }
                    }

                    if (blackPixelFoundInColumn)
                    {
                        //Start and End are not yet set. So initialize them to the index of this column.
                        if (blackColumnStart == -1)
                        {
                            blackColumnStart = blackColumnEnd = i;
                        }
                        else
                        {
                            blackColumnEnd++;
                        }
                    }
                    else
                    {
                        //If Start is already intialized, then it means we found a column which has no 
                        //black pixels. Exit the for loops.
                        if (blackColumnStart != -1)
                        {
                            break;
                        }
                    }
                }
            }
            finally
            {
                caretBitmap.UnlockBits(data);
            }
            
            //When no black pixel columns are found, blackColumnEnd = blackColumnStart = -1.
            //Hence false will be returned.
            //For italic caret the difference between blackColumnStart and blackColumnEnd 
            //should be >= 2
            return (blackColumnEnd - blackColumnStart) >= 2;
        }

        private unsafe static Bitmap UnsafeColorToBlackWhite(Bitmap bitmap,
            byte luminanceThreshold)
        {
            Bitmap result = new Bitmap(bitmap.Width, bitmap.Height);
            BitmapData sourceData = null;
            BitmapData destData = LockBitmapDataWrite(result);
            try
            {
                sourceData = LockBitmapDataRead(bitmap);
                int width = PixelData.GetScanLineWidth(bitmap.Size);
                for (int y=0; y < bitmap.Height; y++)
                {
                    PixelData* sourcePixels;
                    PixelData* destPixels;
                    sourcePixels = (PixelData*)
                        ((byte*)sourceData.Scan0.ToPointer() + y * width);
                    destPixels = (PixelData*)
                        ((byte*)destData.Scan0.ToPointer() + y * width);
                    for (int x=0; x < bitmap.Width; x++)
                    {
                        *destPixels = *sourcePixels;
                        destPixels->ToBlackWhite(luminanceThreshold);
                        sourcePixels++;
                        destPixels++;
                    }
                }
            }
            finally
            {
                result.UnlockBits(destData);
                if (sourceData != null)
                    bitmap.UnlockBits(sourceData);
            }
            return result;
        }

        /// <summary>
        /// Count number of pixel of matched color. you can specify a specific color elements combination.
        /// </summary>
        /// <param name="bitmap">bitmap to be count</param>
        /// <param name="color">color to be matched</param>
        /// <param name="RGB">Color combination such as red only, RedBlue etc</param>
        /// <returns></returns>
        private unsafe static int UnsafeCountColorPixels(Bitmap bitmap, Media.Color color, ColorElement RGB)
        {
            int result = 0;
            BitmapData data = LockBitmapDataRead(bitmap);
            try
            {
                int width = PixelData.GetScanLineWidth(bitmap.Size);
                for (int y = 0; y < bitmap.Height; y++)
                {
                    PixelData* pixels;
                    pixels = (PixelData*)
                        ((byte*)data.Scan0.ToPointer() + y * width);
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        switch (RGB)
                        {
                            case ColorElement.Blue:
                            {
                                if (pixels->blue == color.B)
                                    result++;
                                break;
                            }
                            case ColorElement.Green:
                            {
                                if (pixels->green == color.G)
                                    result++;
                                break;
                            }
                            case ColorElement.Red:
                            {
                                if (pixels->red == color.R)
                                    result++;
                                break;
                            }
                            case ColorElement.GreenBlue:
                            {
                                if (pixels->blue == color.B && pixels->green == color.G)
                                    result++;
                                break; 
                            }
                            case ColorElement.RedBlue:
                            {
                                if (pixels->red == color.R && pixels->blue == color.B)
                                    result++;
                                break;
                            }
                            case ColorElement.RedGreen:
                            {
                                if (pixels->red == color.R && pixels->green == color.G)
                                    result++;
                                break; 
                            }
                            default:
                            {
                                if (pixels->red == color.R && pixels->blue == color.B && pixels->green == color.G)
                                    result++;
                                break; 
                            }
                        }
                        pixels++;
                    }
                }
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
            return result;
        }
      
        /// <summary>Estimates the number of text lines in a bitmap.</summary>
        private unsafe static int UnsafeCountTextLines(Bitmap bitmap)
        {
            BitmapData data = LockBitmapDataRead(bitmap);
            int result = 0;
            try
            {
                int width = PixelData.GetScanLineWidth(bitmap.Size);
                bool isOnLine = false;
                for (int y=0; y < bitmap.Height; y++)
                {
                    PixelData* pixels;
                    pixels = (PixelData*)
                        ((byte*)data.Scan0.ToPointer() + y * width);
                    bool blackFound = false;
                    for (int x=0; x < bitmap.Width; x++)
                    {
                        bool isBlack = (pixels->red == 0);
                        if (isBlack)
                        {
                            blackFound = true;
                            break;
                        }
                        pixels++;
                    }
                    bool lineFinished = (isOnLine && !blackFound);
                    bool lineStarted = (!isOnLine && blackFound);
                    if (lineFinished)
                        isOnLine = false;
                    if (lineStarted)
                    {
                        isOnLine = true;
                        result++;
                    }
                }
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
            return result;
        }

        /// <summary>
        /// Returns an array of bounding rectangles for the
        /// text lines in a bitmap.
        /// </summary>
        private unsafe static Rectangle[] UnsafeGetTextLines(Bitmap bitmap)
        {
            ArrayList results = new ArrayList(4);
            BitmapData data = LockBitmapDataRead(bitmap);
            try
            {
                int width = PixelData.GetScanLineWidth(bitmap.Size);

                int leftmost = Int32.MaxValue;
                int rightmost = Int32.MinValue;
                int topmost = Int32.MaxValue;
                int bottommost = Int32.MinValue;

                for (int y=0; y < bitmap.Height; y++)
                {
                    bool pixelFound = false;
                    PixelData* pixels;
                    pixels = (PixelData*)
                        ((byte*)data.Scan0.ToPointer() + y * width);
                    for (int x=0; x < bitmap.Width; x++)
                    {
                        if (pixels->red == 0 &&
                            pixels->blue == 0 &&
                            pixels->green == 0)
                        {
                            pixelFound = true;
                            leftmost = (x < leftmost)? x : leftmost;
                            rightmost = (x > rightmost)? x : rightmost;
                            topmost = (y < topmost)? y : topmost;
                            bottommost = (y > bottommost)? y : bottommost;
                        }
                        pixels++;
                    }

                    // If we have a blank line, add the current rectangle
                    // and reset it.
                    if (!pixelFound && leftmost != Int32.MaxValue)
                    {
                        results.Add(Rectangle.FromLTRB(
                            leftmost, topmost, rightmost, bottommost));
                        leftmost = topmost = Int32.MaxValue;
                        rightmost = bottommost = Int32.MaxValue;
                    }
                }

                // Add any remaining rectangles.
                if (leftmost != Int32.MaxValue)
                {
                    results.Add(Rectangle.FromLTRB(
                        leftmost, topmost, rightmost, bottommost));
                    leftmost = topmost = Int32.MaxValue;
                    rightmost = bottommost = Int32.MaxValue;
                }
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
            return (Rectangle[]) results.ToArray(typeof(Rectangle));
        }

        private unsafe static Bitmap UnsafeColorToGreyScale(Bitmap bitmap)
        {
            Bitmap result = new Bitmap(bitmap.Width, bitmap.Height);
            BitmapData sourceData = null;
            BitmapData destData = LockBitmapDataWrite(result);
            try
            {
                sourceData = LockBitmapDataRead(bitmap);
                int width = PixelData.GetScanLineWidth(bitmap.Size);
                for (int y=0; y < bitmap.Height; y++)
                {
                    PixelData* sourcePixels;
                    PixelData* destPixels;
                    sourcePixels = (PixelData*)
                        ((byte*)sourceData.Scan0.ToPointer() + y * width);
                    destPixels = (PixelData*)
                        ((byte*)destData.Scan0.ToPointer() + y * width);
                    for (int x=0; x < bitmap.Width; x++)
                    {
                        *destPixels = *sourcePixels;
                        destPixels->ToGreyScale();
                        sourcePixels++;
                        destPixels++;
                    }
                }
            }
            finally
            {
                result.UnlockBits(destData);
                if (sourceData != null)
                    bitmap.UnlockBits(sourceData);
            }
            return result;
        }

        private static unsafe Rectangle UnsafeGetBoundingRectangle(Bitmap bitmap)
        {
            const int neverSet = -1;
            int leftMost = neverSet;
            int topMost = neverSet;
            int rightMost = neverSet;
            int bottomMost = neverSet;
            BitmapData data = LockBitmapDataRead(bitmap);
            try
            {
                int width = PixelData.GetScanLineWidth(bitmap.Size);
                for (int y=0; y < bitmap.Height; y++)
                {
                    PixelData* pixel = (PixelData*)
                        ((byte*)data.Scan0.ToPointer() + y * width);
                    for (int x=0; x < bitmap.Width; x++)
                    {
                        // Assume the bitmap is black and white, therefore
                        // just one component in 0 means the pixel is black.
                        bool isBlack = (pixel->red == 0);
                        if (isBlack)
                        {
                            if (leftMost == neverSet || x < leftMost)
                                leftMost = x;
                            if (rightMost == neverSet || x > rightMost)
                                rightMost = x;
                            if (topMost == neverSet || y < topMost)
                                topMost = y;
                            if (bottomMost == neverSet || y > bottomMost)
                                bottomMost = y;
                        }
                        pixel++;
                    }
                }
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
            if (leftMost == neverSet)
                return Rectangle.Empty;
            else
            {
                int width = rightMost - leftMost+1;
                int height = bottomMost - topMost+1;
                int x = leftMost;
                int y = topMost;
                return new Rectangle(x, y, width, height);
            }
        }

        private unsafe static Rect UnsafeGetDifferencesRect(Bitmap bitmap1, Bitmap bitmap2)
        {
            int left, right, top, bottom;
            BitmapData bitmapData1, bitmapData2;

            left = top = Int32.MaxValue;
            right = bottom = 0;
            bitmapData1 = bitmapData2 = null;

            try
            {
                bitmapData1 = BitmapUtils.LockBitmapDataRead(bitmap1);
                bitmapData2 = BitmapUtils.LockBitmapDataRead(bitmap2);

                int width = PixelData.GetScanLineWidth(bitmap1.Size);
                for (int y = 0; y < bitmap1.Height; y++)
                {
                    PixelData* bitmapPixels1;
                    PixelData* bitmapPixels2;
                    bitmapPixels1 = (PixelData*)
                        ((byte*)bitmapData1.Scan0.ToPointer() + y * width);
                    bitmapPixels2 = (PixelData*)
                        ((byte*)bitmapData2.Scan0.ToPointer() + y * width);
                    for (int x = 0; x < bitmap1.Width; x++)
                    {
                        bool match = ((bitmapPixels1->blue == bitmapPixels2->blue) &&
                            (bitmapPixels1->green == bitmapPixels2->green) &&
                            (bitmapPixels1->red == bitmapPixels2->red));

                        if (!match)
                        {
                            if (left > x)
                            {
                                left = x;
                            }
                            if (right < x)
                            {
                                right = x;
                            }
                            if (bottom < y)
                            {
                                bottom = y;
                            }
                            if (top > y)
                            {
                                top = y;
                            }
                        }

                        bitmapPixels1++;
                        bitmapPixels2++;
                    }
                }
                
                return new Rect((double)left, (double)top, (double)(right - left + 1), (double)(bottom - top + 1));
            }
            finally
            {
                if (bitmapData1 != null)
                {
                    bitmap1.UnlockBits(bitmapData1);
                }
                if (bitmapData2 != null)
                {
                    bitmap2.UnlockBits(bitmapData2);
                }
            }
        }

        private unsafe static bool UnsafeGetTextCaret(Bitmap line, out Rectangle rectangle)
        {
            //
            // The length of each vertical line is calculated here.
            // Vertical lines with gaps are discarded.
            // Lines less than 85% of the bitmap line are discarded.
            // The longest line is considered the caret.
            //
            // NOTE: this will not work for text with Italics.
            //
            int[] lengths = new int[line.Width];

            BitmapData data = LockBitmapDataRead(line);
            try
            {
                int width = PixelData.GetScanLineWidth(line.Size);
                for (int x=0; x < line.Width; x++)
                {
                    int top = -1;
                    int bottom = -1;
                    for (int y=0; y < line.Height; y++)
                    {
                        PixelData* pixel = (PixelData*)
                            ((byte*)data.Scan0.ToPointer() + y * width);
                        pixel += x;
                        bool isBlack = (pixel->red == 0);
                        if (isBlack)
                        {
                            // Black after line is a gap - discard.
                            if (bottom != -1)
                            {
                                top = bottom = -1;
                                break;
                            }
                            if (top == -1)
                            {
                                top = y;
                            }
                        }
                        else
                        {
                            if (top != -1 && bottom == -1)
                                bottom = y;
                        }
                    }

                    // Consider the case where there is no white beneath line.
                    if (top != -1 && bottom == -1) bottom = line.Height;
                    if (top == -1)
                    {
                        lengths[x] = 0;
                    }
                    else
                    {
                        lengths[x] = bottom - top+1;
                    }
                }
            }
            finally
            {
                line.UnlockBits(data);
            }

            // Discard short lines and find the longest.
            int minimum = (int)((float) line.Height * 0.85);
            int longestIndex = -1;
            int longestSize = 0;
            for (int i = 0; i < lengths.Length; i++)
            {
                if (lengths[i] < minimum) lengths[i] = 0;
                if (lengths[i] > longestSize)
                {
                    longestIndex = i;
                    longestSize = lengths[i];
                }
            }

            // Return the appropriate values.
            if (longestIndex == -1)
            {
                rectangle = Rectangle.Empty;
                return false;
            }
            else
            {
                rectangle = Rectangle.FromLTRB(
                    longestIndex, 0, longestIndex + 1, line.Height);
                return true;
            }
        }        

        /// <summary>Inverts the black and white pixels in an image.</summary>
        private unsafe static Bitmap UnsafeInvertBlackWhite(Bitmap bitmap)
        {
            Bitmap result = new Bitmap(bitmap.Width, bitmap.Height);
            BitmapData sourceData = null;
            BitmapData destData = LockBitmapDataWrite(result);
            try
            {
                sourceData = LockBitmapDataRead(bitmap);
                int width = PixelData.GetScanLineWidth(bitmap.Size);
                for (int y=0; y < bitmap.Height; y++)
                {
                    PixelData* sourcePixels;
                    PixelData* destPixels;
                    sourcePixels = (PixelData*)
                        ((byte*)sourceData.Scan0.ToPointer() + y * width);
                    destPixels = (PixelData*)
                        ((byte*)destData.Scan0.ToPointer() + y * width);
                    for (int x=0; x < bitmap.Width; x++)
                    {
                        byte val = (sourcePixels->red == 0)? (byte)255 : (byte)0;
                        destPixels->red = val;
                        destPixels->green = val;
                        destPixels->blue = val;
                        sourcePixels++;
                        destPixels++;
                    }
                }
            }
            finally
            {
                result.UnlockBits(destData);
                if (sourceData != null)
                    bitmap.UnlockBits(sourceData);
            }
            return result;
        }

        private unsafe static bool UnsafeVerifyCaretType(Bitmap caretBitmap, CaretType expCaretType)
        {
            bool result = false;
            int topBlackPixelCount;

            System.Drawing.Imaging.BitmapData data = BitmapUtils.LockBitmapDataRead(caretBitmap);

            int width = PixelData.GetScanLineWidth(caretBitmap.Size);

            topBlackPixelCount = 0;

            try
            {
                //Count the number of black pixels in the top 3 lines
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < caretBitmap.Width; j++)
                    {
                        PixelData* pixel = (PixelData*)
                            ((byte*)data.Scan0.ToPointer() + (i * width));
                        pixel += j;
                        bool isBlack = (pixel->red == 0);
                        if (isBlack)
                        {
                            topBlackPixelCount++;
                        }
                    }
                }

                /* 
                //Alternate algorithm: Compare the number of black pixels between top and bottom half.
                //Not working because of extra pixels from surrounding characters come in the bitmap capture.
                //Keeping this code for future reference to add support for testing Block Caret
                int bottomBlackPixelCount;
                bottomBlackPixelCount = 0;
                int height = ((caretBitmap.Height % 2) == 1) ? caretBitmap.Height - 1 : caretBitmap.Height;
                //Count the number of black pixels on the top half area.
                for (int i = 0; i < height / 2; i++)
                {
                    for (int j = 0; j < caretBitmap.Width; j++)
                    {
                        PixelData* pixel = (PixelData*)
                            ((byte*)data.Scan0.ToPointer() + (i * width));
                        pixel += j;
                        bool isBlack = (pixel->red == 0);
                        if (isBlack)
                        {
                            topBlackPixelCount++;
                        }   
                    }
                }

                //Count the number of black pixels on the below half area.
                for (int i = height / 2; i < height; i++)
                {
                    for (int j = 0; j < caretBitmap.Width; j++)
                    {
                        PixelData* pixel = (PixelData*)
                            ((byte*)data.Scan0.ToPointer() + (i * width));
                        pixel += j;
                        bool isBlack = (pixel->red == 0);
                        if (isBlack)
                        {
                            bottomBlackPixelCount++;
                        }
                    }
                }
                */
            }
            finally
            {
                caretBitmap.UnlockBits(data);
            }

            switch (expCaretType)
            {
                case CaretType.Normal:
                    if (topBlackPixelCount == 3)
                    {
                        result = true;
                    }
                    break;
                case CaretType.BiDi:
                    if (topBlackPixelCount > 3)
                    {
                        result = true;
                    }
                    break;
                default:
                    throw new NotImplementedException("This function can currently verify only Normal and BiDi carets");
            }

            /*
            //Switch statement corresponding to the alternate algorithm in comments above.
            switch (expCaretType)
            {
                case CaretType.Normal:
                    if ((topBlackPixelCount == bottomBlackPixelCount) &&
                         (bottomBlackPixelCount > 0))
                    {
                        result = true;
                    }
                    break;
                case CaretType.BiDi:
                    if ((topBlackPixelCount > bottomBlackPixelCount) &&
                        (bottomBlackPixelCount > 0))
                    {
                        result = true;
                    }
                    break;
                default:
                    throw new NotImplementedException("This function can currently verify only Normal and BiDi carets");
            }
            */

            return result;
        }
        
        #endregion Private methods.

        #region Public methods.

        /// <summary>
        /// Verifies that the caret in the bitmap is Italic caret. 
        /// Right now this works only with FontSizes >= 7
        /// </summary>
        /// <param name='caretBitmap'>Bitmap of the caret.</param>        
        /// <returns>True if a caret is Italic, False otherwise.</returns>
        public static bool CheckCaretItalic(Bitmap caretBitmap)
        {
            new System.Security.Permissions.SecurityPermission(
                System.Security.Permissions.PermissionState.Unrestricted)
                .Assert();
            return UnsafeCheckCaretItalic(caretBitmap);
        }

        /// <summary>Converts a color bitmap to a black/white binary
        /// bitmap.</summary>
        /// <param name="bitmap">Bitmap to convert to black and white.</param>
        /// <returns>A new bitmap with black and white pixels.</returns>
        /// <remarks>
        /// The threshold between black and white is set at mid-intensity.
        /// Better results may be obtained by building a histogram and calculating
        /// a middle value. Even better results may be obtained by performing this
        /// technique in subareas of the image.
        /// </remarks>
        public static Bitmap ColorToBlackWhite(Bitmap bitmap)
        {
            return ColorToBlackWhite(bitmap, 128);
        }

        /// <summary>
        /// Converts a color bitmap to a black/white binary bitmap.
        /// </summary>
        /// <param name="bitmap">Bitmap to convert to black and white.</param>
        /// <param name="luminanceThreshold">
        /// Luminance threshold at which a color is considered to be white.
        /// </param>
        /// <returns>A new bitmap with black and white pixels.</returns>
        /// <remarks>
        /// <p>Luminance is a properly-weighted calculation of
        /// how much "light" a color corries or how bright it is.</p>
        /// <p>A 0 will produce an all-white image, a 255 will produce an
        /// all-black image.</p>
        /// </remarks>
        /// <example>The following sample shows how to capture a bitmap
        /// for an Avalon element, convert it to black and white,
        /// and save it.<code>...
        /// public void SaveBlackWhiteElement(Element e) {
        ///   using (Bitmap b = BitmapCapture.CreateBitmapFromElement(e))
        ///   using (Bitmap bw = BitmapUtils.ColorToBlackWhite(b, 128)) {
        ///     bw.Save(@"c:\element.png");
        ///   }
        /// }
        /// </code></example>
        public static Bitmap ColorToBlackWhite(Bitmap bitmap,
            byte luminanceThreshold)
        {
            new System.Security.Permissions.SecurityPermission(
                System.Security.Permissions.PermissionState.Unrestricted)
                .Assert();
            return UnsafeColorToBlackWhite(bitmap, luminanceThreshold);
        }

        /// <summary>Converts a color bitmap to a greyscale bitmap.</summary>
        /// <param name="bitmap">Bitmap to convert to greyscale.</param>
        /// <returns>A new bitmap with grescale pixels.</returns>
        public static Bitmap ColorToGreyScale(Bitmap bitmap)
        {
            new System.Security.Permissions.SecurityPermission(
                System.Security.Permissions.PermissionState.Unrestricted)
                .Assert();
            return UnsafeColorToGreyScale(bitmap);
        }

        /// <summary>
        /// Convert Avalon color to an uint so that we can specify it for system color. 
        /// The color order: 0x00bbggrr
        /// </summary>
        /// <param name="color">Avalon color</param>
        /// <returns>Unsigned int</returns>
        [CLSCompliant(false)]
        public static uint ColorTouint(System.Windows.Media.Color color)
        {
            uint u = 0; 

            u = u | color.B;
            u = u << 8 | color.G;
            u = u << 8 | color.R;

            return u; 
        }
        
        /// <summary>
        /// Count the number of pixels of a specific color.
        /// </summary>
        /// <param name="bitmap">Bitmap to analyze</param>
        /// <param name="color">color of a pixel</param>
        /// <returns>The number of specific pixels</returns>
        public static int CountColoredPixels(Bitmap bitmap, System.Windows.Media.Color color)
        {
            new System.Security.Permissions.SecurityPermission(
                System.Security.Permissions.PermissionState.Unrestricted).Assert();
            return UnsafeCountColorPixels(bitmap, color, ColorElement.All);

        }

        /// <summary>
        /// Count the number of pixels of specific color element(s)
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="color"></param>
        /// <param name="RGB"></param>
        /// <returns></returns>
        public static int CountColoredPixels(Bitmap bitmap, System.Windows.Media.Color color, ColorElement RGB)
        {
            new System.Security.Permissions.SecurityPermission(
                System.Security.Permissions.PermissionState.Unrestricted).Assert();
            return UnsafeCountColorPixels(bitmap, color, RGB);
        }

        /// <summary>Estimates the number of text lines in a bitmap.</summary>
        /// <param name="bitmap">Black and white bitmap to analyze.</param>
        /// <returns>The number of text lines found.</returns>
        public static int CountTextLines(Bitmap bitmap)
        {
            new System.Security.Permissions.SecurityPermission(
                System.Security.Permissions.PermissionState.Unrestricted)
                .Assert();
            return UnsafeCountTextLines(bitmap);
        }

        /// <summary>
        /// Returns an array of bounding rectangles for the
        /// text lines in a bitmap.
        /// </summary>
        /// <param name="bitmap">Black and white bitmap to analyze.</param>
        /// <returns>The bounding boxes for the lines.</returns>
        public static Rectangle[] GetTextLines(Bitmap bitmap)
        {
            new System.Security.Permissions.SecurityPermission(
                System.Security.Permissions.PermissionState.Unrestricted)
                .Assert();
            return UnsafeGetTextLines(bitmap);
        }

        /// <summary>Creates a copy of a bitmap blocking an area.</summary>
        /// <param name="bitmap">Bitmap to copy.</param>
        /// <param name="blockArea">Area to block.</param>
        /// <returns>A copy of the bitmap with a blocked area.</returns>
        public static Bitmap CreateBlockedBitmap(Bitmap bitmap, Rectangle blockArea)
        {            
            Bitmap result = new Bitmap(bitmap);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.FillRectangle(Brushes.Black, blockArea);
            }
            return result;
        }

        /// <summary>Creates a smaller copy of a bitmap with
        /// borders removed.</summary>
        /// <param name="bitmap">Bitmap to copy.</param>
        /// <param name="width">Width of borders.</param>
        /// <returns>A copy of the bitmap with the borders removed.</returns>
        /// <remarks>Note that the same border width is used for all four
        /// borders. To specify different widths, use the CreateSubBitmap
        /// method.</remarks>
        /// <example>The following sample shows how to use this method.<code>...
        /// public void SaveTextBoxContents(TextBox box) {
        ///   using (Bitmap b = BitmapCapture.CreateBitmapFromElement(box))
        ///   using (Bitmap b2 = BitmapUtils.CreateBorderlessBitmap(b, 2))
        ///     b2.Save(@"contents.png");
        /// }</code></example>
        public static Bitmap CreateBorderlessBitmap(Bitmap bitmap, int width)
        {
            Rectangle subArea =
                new Rectangle(width, width,
                bitmap.Width - width * 2, bitmap.Height - width * 2);
            return CreateSubBitmap(bitmap, subArea);
        }

        /// <summary>
        /// Creates a bitmap with regions specified on the border clipped
        /// </summary>
        /// <param name="bitmap">Bitmap to extract area from.</param>
        /// <param name="left">Left margin to clip.</param>
        /// /// <param name="right">right margin to clip.</param>
        /// /// <param name="top">top margin to clip.</param>
        /// /// <param name="bottom">bottom margin to clip.</param>
        /// <returns>A new bitmap with a copy of the requested area.</returns>
        public static Bitmap CreateBitmapClipped(Bitmap bitmap, int left, int right, int top, int bottom)
        {                        
            Rectangle subArea =
                new Rectangle(left, top,
                bitmap.Width - left - right, bitmap.Height - top - bottom);
            
            return CreateSubBitmap(bitmap, subArea);
        }

        /// <summary>
        /// Creates a bitmap with regions specified on the border clipped
        /// </summary>
        /// <param name="bitmap">Bitmap to extract area from.</param>
        /// <param name="borderClipThickness">border to be clipped.</param>
        /// <param name="useFloatValues">use bool values</param>
        /// <returns>A new bitmap with a copy of the requested area.</returns>        
        public static Bitmap CreateBitmapClipped(Bitmap bitmap, Thickness borderClipThickness, bool useFloatValues)
        {
            if (useFloatValues)
            {                
                float width = (float)(bitmap.Width - borderClipThickness.Left - borderClipThickness.Right );
                float height = (float)(bitmap.Height - borderClipThickness.Top - borderClipThickness.Bottom );
                RectangleF subArea =
                new RectangleF((float)borderClipThickness.Left,(float)borderClipThickness.Top,width,height);
                return CreateSubBitmap(bitmap, subArea);
            }
            else
            {                
                return CreateBitmapClipped(bitmap, (int)borderClipThickness.Left, (int)borderClipThickness.Right, (int)borderClipThickness.Top, (int)borderClipThickness.Bottom);
            }
        }

        /// <summary>
        /// Returns an adjusted bitmap sub area of the specified Rect according to bitmap's Dpi
        /// </summary>
        /// <param name="bitmap">Bitmap on which sub area is adjusted</param>
        /// <param name="subArea">Sub area of the bitmap which needs to be adjusted</param>
        /// <returns>Adjusted sub area for the bitmap</returns>
        /// <remarks>
        /// All WPF APIs treat the world as 96 DPI, regardless of the true DPI.  So
        /// when using non-WPF APIs, such as System.Drawing methods, dimensions and
        /// offsets need to be converted from 96 DPI to the true DPI.
        /// </remarks>
        public static Rect AdjustBitmapSubAreaForDpi(Bitmap bitmap, Rect subArea)
        {            
            // Scale subArea to proper DPI
            if (bitmap.HorizontalResolution != 96)
            {
                subArea.X = (double)((float)subArea.X * bitmap.HorizontalResolution / 96.0f);
                subArea.Width = (double)((float)subArea.Width * bitmap.HorizontalResolution / 96.0f);
            }
            if (bitmap.VerticalResolution != 96)
            {
                subArea.Y = (double)((float)subArea.Y * bitmap.VerticalResolution / 96.0f);
                subArea.Height = (double)((float)subArea.Height * bitmap.VerticalResolution / 96.0f);
            }

            return subArea;
        }

        /// <summary>
        /// Returns an adjusted bitmap sub area of the specified Rectangle according to bitmap's Dpi
        /// </summary>
        /// <param name="bitmap">Bitmap on which sub area is adjusted</param>
        /// <param name="subArea">Sub area of the bitmap which needs to be adjusted</param>
        /// <returns>Adjusted sub area for the bitmap</returns>
        /// <remarks>
        /// All WPF APIs treat the world as 96 DPI, regardless of the true DPI.  So
        /// when using non-WPF APIs, such as System.Drawing methods, dimensions and
        /// offsets need to be converted from 96 DPI to the true DPI.
        /// </remarks>
        public static Rectangle AdjustBitmapSubAreaForDpi(Bitmap bitmap, Rectangle subArea)
        {            
            // Scale subArea to proper DPI
            if (bitmap.HorizontalResolution != 96)
            {
                subArea.X = (int)((float)subArea.X * bitmap.HorizontalResolution / 96.0f);
                subArea.Width = (int)((float)subArea.Width * bitmap.HorizontalResolution / 96.0f);
            }
            if (bitmap.VerticalResolution != 96)
            {
                subArea.Y = (int)((float)subArea.Y * bitmap.VerticalResolution / 96.0f);
                subArea.Height = (int)((float)subArea.Height * bitmap.VerticalResolution / 96.0f);
            }

            return subArea;
        }       

        /// <summary>
        /// Creates a bitmap from a rectangular area in an existing bitmap.
        /// </summary>
        /// <param name="bitmap">Bitmap to extract area from.</param>
        /// <param name="subArea">Area to copy to new bitmap.</param>
        /// <returns>A new bitmap with a copy of the requested area.</returns>
        public static Bitmap CreateSubBitmap(Bitmap bitmap, Rect subArea)
        {            
            Rectangle r = new Rectangle(
                (int)Math.Round(subArea.Left, 0), (int)Math.Round(subArea.Top, 0),
                (int)Math.Round(subArea.Width, 0), (int)Math.Round(subArea.Height, 0));
            
            return CreateSubBitmap(bitmap, r);
        }

        /// <summary>
        /// Creates a bitmap from a rectangular area in an existing bitmap.
        /// </summary>
        /// <param name="bitmap">Bitmap to extract area from.</param>
        /// <param name="subArea">Area to copy to new bitmap.</param>
        /// <returns>A new bitmap with a copy of the requested area.</returns>
        public static Bitmap CreateSubBitmap(Bitmap bitmap, Rectangle subArea)
        {            
            using (Graphics source = Graphics.FromImage(bitmap))
            {
                Bitmap result = new Bitmap(subArea.Width, subArea.Height, source);
                using (Graphics destination = Graphics.FromImage(result))
                {
                    Rectangle destRect = new Rectangle(
                        new System.Drawing.Point(0), subArea.Size);
                    destination.DrawImage(bitmap, destRect, subArea, GraphicsUnit.Pixel);
                }
                return result;
            }
        }        

        /// <summary>
        /// Creates a bitmap from a rectangular area in an existing bitmap.
        /// </summary>
        /// <param name="bitmap">Bitmap to extract area from.</param>
        /// <param name="subArea">Area to copy to new bitmap.</param>
        /// <returns>A new bitmap with a copy of the requested area.</returns>
        public static Bitmap CreateSubBitmap(Bitmap bitmap, RectangleF subArea)
        {            
            using (Graphics source = Graphics.FromImage(bitmap))
            {
                Bitmap result = new Bitmap((int)subArea.Width, (int)subArea.Height, source);
                using (Graphics destination = Graphics.FromImage(result))
                {
                    RectangleF destRect = new RectangleF(
                        new System.Drawing.PointF(0,0), subArea.Size);
                    destination.DrawImage(bitmap, destRect, subArea, GraphicsUnit.Pixel);
                }
                return result;
            }
        }

        /// <summary>
        /// Creates an array of rectangles that are possible
        /// text selection rectangles in a black and white image with white
        /// background.
        /// </summary>
        /// <param name="bitmap">Black and white bitmap to examine.</param>
        /// <returns>An array of possible text selection rectangles.</returns>
        /// <remarks>
        /// The algorithm to find selection rectangles is as follows.
        /// Each line is examined for sequences of black pixels greater than two.
        /// This is recorded as a new rectangle candidate or an extension to
        /// a previous rectangle. After each analysis is complete, all rectangles
        /// the smaller rectangles (less than 3 pixels wide and less than 5
        /// pixels tall) are discarded.
        /// A rectangle is considered closed if, for any given line, > 50% of its
        /// pixels are white (background).
        /// </remarks>
        /// <example>The following sample shows how to use this method.<code>...
        /// private Rectangle[] FindRects(Element e) {
        ///   Bitmap b = BitmapCapture.CreateBitmapFromElement(e);
        ///   Bitmap noborder = BitmapUtils.CreateBorderlessBitmap(b, 2);
        ///   Bitmap bw = BitmapUtils.ColorToBlackWhite(noborder);
        ///   return BitmapUtils.FindSelectionRectangles(bw);
        /// }</code></example>
        public static Rectangle[] FindSelectionRectangles(Bitmap bitmap)
        {
            new System.Security.Permissions.SecurityPermission(
                System.Security.Permissions.PermissionState.Unrestricted)
                .Assert();
            TextSelectionFinderAlgorithm finder =
                new TextSelectionFinderAlgorithm();
            finder.Bitmap = bitmap;
            finder.Execute();
            return finder.Result;
        }

        /// <summary>
        /// Calculates the smallest rectangle that encompasses all
        /// black pixels in a black and white bitmap.
        /// </summary>
        /// <param name="bitmap">Black and white bitmap to examine.</param>
        /// <returns>
        /// The smallest rectangle that bounds the black pixels
        /// in the given bitmap. If no black pixels are found, the rectangle
        /// is Rectangle.Empty.
        /// </returns>
        public static Rectangle GetBoundingRectangle(Bitmap bitmap)
        {
            new System.Security.Permissions.SecurityPermission(
                System.Security.Permissions.PermissionState.Unrestricted)
                .Assert();
            return UnsafeGetBoundingRectangle(bitmap);
        }

        /// <summary>
        /// Returns the bounding rectangle which encompasses all the differences between bitmap1 and bitmap2
        /// </summary>
        /// <param name="bitmap1">bitmap1</param>
        /// <param name="bitmap2">bitmap2</param>
        /// <returns>Bounding Rect which encompasses all the differences between bitmap1 and bitmap2</returns>
        public static Rect GetDifferencesRect(Bitmap bitmap1, Bitmap bitmap2)
        {
            new System.Security.Permissions.SecurityPermission(
                System.Security.Permissions.PermissionState.Unrestricted)
                .Assert();
            return UnsafeGetDifferencesRect(bitmap1, bitmap2);
        }        

        /// <summary>
        /// Gets the bounding box for a caret in the bitmap capture of a
        /// single text line.
        /// </summary>
        /// <param name='line'>Bitmap with line capture.</param>
        /// <param name='rectangle'>Rectangle that bounds the caret.</param>
        /// <returns>true if a caret was found, false otherwise.</returns>
        public static bool GetTextCaret(Bitmap line, out Rectangle rectangle)
        {
            new System.Security.Permissions.SecurityPermission(
                System.Security.Permissions.PermissionState.Unrestricted)
                .Assert();
            return UnsafeGetTextCaret(line, out rectangle);
        }        

        /// <summary>Creates an image with a bright rectangle outline.</summary>
        /// <param name="bitmap">Bitmap to copy image from.</param>
        /// <param name="rectangle">Rectangle to highlight.</param>
        /// <returns>A new bitmap with the specified rectangle highlighted.</returns>
        /// <example>The following sample shows how to use this method.<code>...
        /// private void LogContentBounds(Element e) {
        ///   // Capture the bitmap, make it black and white, and remove border.
        ///   Bitmap b = BitmapCapture.CreateBitmapFromElement(e);
        ///   b = BitmapUtils.ColorToBlackWhite(b);
        ///   b = BitmapUtils.CreateBorderlessBitmap(b, 2);
        ///
        ///   // Get the content bounding rectangle and highlight it.
        ///   Rectangle r = BitmapUtils.GetBoundingRectangle(b);
        ///   b = BitmapUtils.HighlightRectangle(b, r);
        ///
        ///   Logger.Current.LogImage(e, "ContentBounds");
        /// }
        /// </code></example>
        public static Bitmap HighlightRectangle(Bitmap bitmap,
            Rectangle rectangle)
        {            
            Bitmap result = new Bitmap(bitmap);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.DrawRectangle(Pens.HotPink, rectangle);
            }
            return result;
        }

        /// <summary>Creates an image with bright rectangle outlines.</summary>
        /// <param name="bitmap">Bitmap to copy image from.</param>
        /// <param name="rectangles">Rectangles to highlight.</param>
        /// <returns>A new bitmap with the rectangles highlighted.</returns>
        public static Bitmap HighlightRectangles(Bitmap bitmap,
            Rectangle[] rectangles)
        {
            Bitmap result = new Bitmap(bitmap);
            using (Graphics g = Graphics.FromImage(result))
            {
                Pen[] pens = new Pen[]
                    { Pens.HotPink, Pens.LimeGreen, Pens.Blue };
                int penIndex = 0;
                foreach (Rectangle r in rectangles)
                {                    
                    g.DrawRectangle(pens[penIndex], r);
                    penIndex++;
                    if (penIndex == pens.Length)
                    {
                        penIndex = 0;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Locks bitmap data in a standard format for reading.
        /// </summary>
        /// <param name="bitmap">Bitmap to lock.</param>
        /// <returns>Locked bitmap data.</returns>
        /// <example>The following sample shows how to use this method.<code>...
        /// private void DoWork(Bitmap b) {
        ///   BitmapData data = LockBitmapDataRead(b);
        ///   try {
        ///     int width = PixelData.GetScanLineWidth(b.Size);
        ///     for (int y=0; y &lt; bitmap.Height; y++) {
        ///       PixelData* pixel;
        ///       pixel = (PixelData*)
        ///         ((byte*)data.Scan0.ToPointer() + y * width);
        ///       for (int x=0; x &lt; b.Width; x++) {
        ///         System.Console.WriteLine("Red: " + pixel-&gt;red);
        ///         pixel++;
        ///       }
        ///     }
        ///   } finally {
        ///     b.UnlockBits(data);
        /// }</code></example>
        public static BitmapData LockBitmapDataRead(Bitmap bitmap)
        {
            return bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        }

        /// <summary>
        /// Locks bitmap data in a standard format for reading and writing.
        /// </summary>
        /// <param name="bitmap">Bitmap to lock.</param>
        /// <returns>Locked bitmap data.</returns>
        /// <example>The following sample shows how to use this method.<code>...
        /// private void RemoveGreen(Bitmap b) {
        ///   BitmapData data = LockBitmapDataWrite(b);
        ///   try {
        ///     int width = PixelData.GetScanLineWidth(b.Size);
        ///     for (int y=0; y &lt; bitmap.Height; y++) {
        ///       PixelData* pixel;
        ///       pixel = (PixelData*)
        ///         ((byte*)data.Scan0.ToPointer() + y * width);
        ///       for (int x=0; x &lt; b.Width; x++) {
        ///         pixel-&gt;green = 0;
        ///         pixel++;
        ///       }
        ///     }
        ///   } finally {
        ///     b.UnlockBits(data);
        /// }</code></example>
        public static BitmapData LockBitmapDataWrite(Bitmap bitmap)
        {
            return bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
        }

        /// <summary>Inverts the black and white pixels in an image.</summary>
        /// <param name="bitmap">Bitmap to invert.</param>
        /// <returns>A new bitmap with inverted black and white pixels.</returns>
        /// <remarks>If the bitmap is not black and white, the results are
        /// undefined.</remarks>
        public unsafe static Bitmap InvertBlackWhite(Bitmap bitmap)
        {
            return UnsafeInvertBlackWhite(bitmap);
        }

        /// <summary>
        /// Verifies that the caret in the bitmap is of expected type
        /// </summary>
        /// <param name='caretBitmap'>Bitmap of the caret.</param>
        /// <param name='expCaretType'>Expected caret type</param>
        /// <returns>true if a caret is of type expected type, false otherwise.</returns>
        public static bool VerifyCaretType(Bitmap caretBitmap, CaretType expCaretType)
        {
            new System.Security.Permissions.SecurityPermission(
                System.Security.Permissions.PermissionState.Unrestricted)
                .Assert();
            return UnsafeVerifyCaretType(caretBitmap, expCaretType);
        }        

        #endregion Public methods.

        #region Inner classes.

        /// <summary>Algorithm to find text selection candidates.</summary>
        private class TextSelectionFinderAlgorithm
        {
            /// <summary>Bitmap being processed.</summary>
            private Bitmap bitmap;
            /// <summary>Array of final candidates.</summary>
            private Rectangle[] result;
            /// <summary>Candidate rectangles.</summary>
            private ArrayList candidates = new ArrayList();
            /// <summary>List of approved rectangles.</summary>
            private ArrayList approved = new ArrayList();

            /// <summary>Bitmap being processed.</summary>
            public Bitmap Bitmap
            {
                get { return this.bitmap; }
                set { this.bitmap = value; }
            }

            /// <summary>Results of selection search.</summary>
            public Rectangle[] Result { get { return this.result; } }

            /// <summary>Tries to approve all candidates.</summary>
            private void ApproveAllCandidates()
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    ApproveCandidate((Rectangle)candidates[i]);
                }
            }

            /// <summary>
            /// Approves a rectangular selection candidate if it
            /// is big enough.
            /// </summary>
            /// <param name="rect">Rectangle to evaluate.</param>
            /// <returns>
            /// true if the rectangle was approved, false otherwise.
            /// </returns>
            private bool ApproveCandidate(Rectangle rect)
            {
                if (rect.Width < 3)
                    return false;
                if (rect.Height < 5)
                    return false;
                approved.Add(rect);
                return true;
            }

            /// <summary>Executes the search.</summary>
            /// <remarks>Results can be read from the Result property.</remarks>
            public unsafe void Execute()
            {
                BitmapData data = LockBitmapDataRead(bitmap);
                try
                {
                    int width = PixelData.GetScanLineWidth(bitmap.Size);
                    for (int y=0; y < bitmap.Height; y++)
                    {
                        PixelData* pixels = (PixelData*)
                            ((byte*)data.Scan0.ToPointer() + y * width);
                        ProcessLine(y, pixels);
                        TrimCandidates();
                    }
                }
                finally
                {
                    bitmap.UnlockBits(data);
                }
                TrimCandidates();
                ApproveAllCandidates();
                TrimApproved();
                result = (Rectangle[]) approved.ToArray(typeof(Rectangle));
            }

            /// <summary>
            /// Processes a single line in the bitmap, looking for areas that
            /// might be part of a text selection.
            /// </summary>
            /// <remarks>
            /// There are two steps in line processing. The first step is to
            /// either enlarge a rectangle (if >35% of its area is still
            /// black) or close it for further enlargment and try to add it to
            /// an approved rectangle list. The second is finding new
            /// candidates for selection rectangles by identifying
            /// strips of black pixels.
            /// </remarks>
            private unsafe void ProcessLine(int y, PixelData* pixels)
            {
                // Figure out which candidates need to be closed, and whether
                // they are approved or discarded.
                for (int i = candidates.Count - 1; i >= 0; i--)
                {
                    Rectangle r = (Rectangle) candidates[i];
                    PixelData* stripPixel = pixels + r.Left;
                    int blackPixelCount = 0;
                    for (int j = 0; j < r.Width; j++)
                    {
                        if (stripPixel->red == 0)
                            blackPixelCount++;
                        stripPixel++;
                    }
                    if (blackPixelCount > (int)(r.Width * 0.35))
                    {
                        r.Height += 1;
                        candidates[i] = r;
                    }
                    else
                    {
                        ApproveCandidate(r);
                        candidates.RemoveAt(i);
                    }
                }

                // Figure out new candidates to add.
                // leftMostBlack is set to -1 to indicate that a strip is not
                // being recognized
                int leftMostBlack = -1;
                for (int x=0; x < bitmap.Width; x++)
                {
                    bool isBlack = (pixels->red == 0);
                    if (isBlack)
                    {
                        // A black pixel means we continue with a strip or
                        // we are starting a new one.
                        if (leftMostBlack == -1)
                            leftMostBlack = x;
                    }
                    else
                    {
                        if (leftMostBlack != -1)
                        {
                            candidates.Add(Rectangle.FromLTRB(leftMostBlack, y, x-1, y));
                        }
                        leftMostBlack = -1;
                     }
                    pixels++;
                }
                // Add a new candidate if required.
                if (leftMostBlack != -1)
                    candidates.Add(Rectangle.FromLTRB(leftMostBlack, y, bitmap.Width-1, y));
            }

            /// <summary>
            /// Returns the results in a string suitable for logging.
            /// </summary>
            public string ResultsToString()
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append("Text selection results:");
                foreach(Rectangle r in result)
                {
                    sb.Append("\r\n  Result: ");
                    sb.Append(r.ToString());
                }
                return sb.ToString();
            }

            /// <summary>Removes candidates enclosed in others.</summary>
            /// <remarks>This could be ugly because there is no order to
            /// the candidates. It should be reasonably performant, though,
            /// as trimming is quite aggressive.</remarks>
            private void TrimCandidates()
            {
                TrimRectangles(this.candidates);
            }

            /// <summary>Removes approved areas enclosed in others.</summary>
            private void TrimApproved()
            {
                TrimRectangles(this.approved);
            }

            /// <summary>Removes rectangles enclosed in others.</summary>
            private static void TrimRectangles(ArrayList rects)
            {
                // We remove from the back of the array to use a
                // for loop, instead of using while and fixing
                // the indexes along as we go.
                for (int i = rects.Count - 1; i >= 1; i--)
                {
                    Rectangle ri = (Rectangle) rects[i];
                    for (int j = i-1; j >= 0; j--)
                    {
                        Rectangle rj = (Rectangle) rects[j];
                        if (rj.Contains(ri))
                        {
                            rects.RemoveAt(i);
                            break;
                        }
                        else if (ri.Contains(rj))
                        {
                            rects[j] = ri;
                            rects.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
        }

        #endregion Inner classes.
    }

    /// <summary>
    /// Specify which element of the color you are interested in when compare.
    /// </summary>
    public enum ColorElement
    {
        /// <summary>Red color only</summary>
        Red,
        /// <summary>Green color only</summary>
        Green,
        /// <summary>blue color only</summary>
        Blue,
        /// <summary>Red green only</summary>
        RedGreen,
        /// <summary>Red and blue color only</summary>
        RedBlue,
        /// <summary>green and blue color only</summary>
        GreenBlue,
        /// <summary>all color</summary>
        All,
    }
}
