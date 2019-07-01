// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Test.VisualVerification
{
    /// <summary>
    /// Represents image pixels in a two-dimensional array for use in visual verification.
    /// Every element of the array represents a pixel at the given [row, column] of the image.
    /// A Snapshot object can be instantiated from a file or captured from the screen.
    /// </summary>
    ///
    /// <example>Takes a snapshot and verifies it is an absolute match to an expected image.
    /// <code>
    /// // Take a snapshot, compare to master image, validate match and save the diff
    /// // in case of a poor match.
    /// Snapshot actual = Snapshot.FromRectangle(new Rectangle(0, 0, 800, 600));
    /// Snapshot expected = Snapshot.FromFile("Expected.bmp");
    /// Snapshot diff = actual.CompareTo(expected);
    /// 
    /// // The SnapshotColorVerifier.Verify() method compares every pixel of a diff bitmap 
    /// // against the threshold defined by the ColorDifference tolerance. If all pixels
    /// // fall within the tolerance, then the method returns VerificationResult.Pass
    /// SnapshotVerifier v = new SnapshotColorVerifier(Color.Black, new ColorDifference(0, 0, 0, 0));
    /// if (v.Verify(diff) == VerificationResult.Fail)
    /// {
    ///     diff.ToFile("Actual.bmp", ImageFormat.Bmp);
    /// }
    /// </code>
    /// </example>
    public class Snapshot : ICloneable
    {
        #region Constructor

        /// <summary>
        /// Snapshot Constructor - Creates buffer of black, opaque pixels 
        /// </summary>
        internal Snapshot(int height, int width)
        {
            buffer = new Color[height, width];
        }

        #endregion

        #region Public Static Initializers

        /// <summary>
        /// Creates a Snapshot instance from data in the specified image file.
        /// </summary>
        /// <param name="filePath">Path to the image file.</param>
        /// <returns>A Snapshot instance containing the pixels in the loaded file.</returns>
        public static Snapshot FromFile(string filePath)
        {
            // Note: open the stream directly because the underlying Bitmap class does not consistently throw on access failures.
            using (Stream s = new FileInfo(filePath).OpenRead())
            {
                // Load the bitmap from disk. NOTE: imageFormat argument is not used in Bitmap.
                // Then convert to Snapshot format

                Bitmap bmp = new System.Drawing.Bitmap(s);
                return FromBitmap(bmp);
            }
        }

        /// <summary>
        /// Creates a Snapshot instance populated with pixels sampled from the rectangle of the specified window.
        /// </summary>
        /// <param name="windowHandle">The Win32 window handle (also known as an HWND), identifying the window to capture from.</param>
        /// <param name="windowSnapshotMode">Determines if window border region should captured as part of Snapshot.</param>
        /// <returns>A Snapshot instance of the pixels captured.</returns>
        public static Snapshot FromWindow(IntPtr windowHandle, WindowSnapshotMode windowSnapshotMode)
        {
            Snapshot result;
            RECT rect;
            if (windowSnapshotMode == WindowSnapshotMode.ExcludeWindowBorder)
            {
                if (!NativeMethods.GetClientRect(windowHandle, out rect)) { throw new Win32Exception(); }
                IntPtr deviceContext = NativeMethods.GetDC(windowHandle);
                try
                {
                    if (deviceContext == IntPtr.Zero) { throw new Win32Exception(); }
                    result = FromBitmap(CaptureBitmap(deviceContext, rect.ToRectangle()));
                }
                finally
                {
                    NativeMethods.ReleaseDC(windowHandle, deviceContext);
                }
            }
            else if (windowSnapshotMode == WindowSnapshotMode.IncludeWindowBorder)
            {
                if (!NativeMethods.GetWindowRect(windowHandle, out rect)) { throw new Win32Exception(); }
                result = FromRectangle(rect.ToRectangle());
            }
            else
            {
                throw new ArgumentOutOfRangeException("windowSnapshotMode");
            }

            return result;
        }

        /// <summary>
        /// Creates a Snapshot instance populated with pixels sampled from the specified screen rectangle, from the context of the Desktop.
        /// </summary>
        /// <param name="rectangle">Rectangle of the screen region to be sampled from.</param>
        /// <returns>A Snapshot instance of the pixels from the bounds of the screen rectangle.</returns>
        public static Snapshot FromRectangle(Rectangle rectangle)
        {
            IntPtr deviceContext = NativeMethods.CreateDC("DISPLAY", string.Empty, string.Empty, IntPtr.Zero);
            if (deviceContext == IntPtr.Zero) { throw new Win32Exception(); }

            Bitmap bitmap;
            try
            {
                bitmap = CaptureBitmap(deviceContext, rectangle);
            }
            finally
            {
                NativeMethods.DeleteDC(deviceContext);
            }
            return FromBitmap(bitmap);
        }

        /// <summary>
        /// Instantiates a Snapshot representation from a Windows Bitmap.
        /// </summary>
        /// <param name="source">Source bitmap to be converted.</param>
        /// <returns>A snapshot based on the source buffer.</returns>
        public static Snapshot FromBitmap(System.Drawing.Bitmap source)
        {
            Snapshot bmp = new Snapshot(source.Height, source.Width);
            for (int row = 0; row < source.Height; row++)
            {
                for (int column = 0; column < source.Width; column++)
                {
                    bmp[row, column] = source.GetPixel(column, row);
                }
            }
            return bmp;
        }

        #endregion

        #region ICloneable Members

        /// <summary>
        /// Creates a deep-copied clone Snapshot with the same value as the existing instance.
        /// </summary>
        /// <returns>Clone instance</returns>
        public object Clone()
        {
            int height = this.Height;
            int width = this.Width;
            Snapshot result = new Snapshot(height, width);
            Parallel.For (0, width, (column) =>
            {
                for (int row = 0; row < height; row++)
                {
                    result[row, column] = this[row, column];
                }
            });
            return result;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Compares the current Snapshot instance to the specified Snapshot to produce a difference image. 
        /// Note: This does not compare alpha channels.
        /// </summary>
        /// <param name="snapshot">The Snapshot to be compared to.</param>
        /// <returns>A new Snapshot object representing the difference image (i.e. the result of the comparison).</returns>
        public Snapshot CompareTo(Snapshot snapshot)
        {
            return CompareTo(snapshot, false);
        }

        /// <summary>
        /// Compares the current Snapshot instance to the specified Snapshot to produce a difference image. 
        /// </summary>
        /// <param name="snapshot">The target Snapshot to be compared to.</param>
        /// <param name="compareAlphaChannel">If true, compares alpha channels. If false, the alpha channel difference values are fixed to 255.</param>
        /// <returns>A new Snapshot object representing the difference image (i.e. the result of the comparison).</returns>
        public Snapshot CompareTo(Snapshot snapshot, bool compareAlphaChannel)
        {
            if (this.Width != snapshot.Width || this.Height != snapshot.Height)
            {
                throw new InvalidOperationException("Snapshots must be of identical size to be compared.");
            }

            int height = snapshot.Height;
            int width = snapshot.Width;
            Snapshot result = new Snapshot(height, width);
            Parallel.For (0,  height, (row) =>
            {
                for (int column = 0; column < width; column++)
                {
                    result[row, column] = this[row, column].Subtract(snapshot[row, column], compareAlphaChannel);
                }
            });
            return result;
        }

        /// <summary>
        /// Draw a vertical line from the bottom of the specified column up to the height given.
        /// </summary>
        /// <param name="col">which column to draw in</param>
        /// <param name="floor">the lowest pixel of the line</param>
        /// <param name="height">the height of the line</param>
        /// <param name="color">the color of the line</param>
        public void DrawLine(int col, int floor, int height, Color color)
        {
            for (int row = Math.Max(floor,0); row < Math.Min(this.Height, height); row++)
            {
                this[row, col] = color;
            }
        }

        /// <summary>
        /// Writes the current Snapshot (at 32 bits per pixel) to a file.
        /// </summary>
        /// <param name="filePath">The path to the output file.</param>
        /// <param name="imageFormat">The file storage format to be used.</param>
        public void ToFile(string filePath, ImageFormat imageFormat)
        {
            Bitmap temp = CreateBitmapFromSnapshot();

            using (Stream s = new FileInfo(filePath).OpenWrite())
            {
                temp.Save(s, imageFormat);
            }
        }

        /// <summary>
        /// Creates a new Snapshot based on the cropped bounds of the current snapshot.
        /// </summary>
        /// <param name="bounds">The bounding rectangle of the Snapshot.</param>
        /// <returns></returns>
        public Snapshot Crop(Rectangle bounds)
        {
            if (bounds.Right > this.Width || bounds.Bottom > this.Height ||
                bounds.Height <= 0 || bounds.Width <= 0 ||
                bounds.Left < 0 || bounds.Top < 0)
            {
                throw new ArgumentOutOfRangeException("bounds");
            }
            int height = bounds.Height;
            int width = bounds.Width;
            Snapshot croppedImage = new Snapshot(height, width);
            Parallel.For (0,  height,  (row) =>
            {
                for (int col = 0; col < width; col++)
                {
                    croppedImage.buffer[row, col] = buffer[bounds.Top + row, bounds.Left + col];
                }
            });
            return croppedImage;
        }

        /// <summary>
        /// Creates a new Snapshot of the specified size from the original using bilinear interpolation. 
        /// </summary>
        /// <param name="size">Desired size of new image</param>
        /// <returns></returns>
        public Snapshot Resize(Size size)
        {
            int thisHeight = this.Height;
            int thisWidth = this.Width;
            int height = size.Height;
            int width = size.Width;
            Snapshot resizedImage = new Snapshot(height, width);
            Parallel.For (0,  height,  (row)=>
            {
                float myRow = row * thisHeight / (float)height;
                for (int col = 0; col < width; col++)
                {
                    float myCol = col * thisWidth / (float)width;
                    resizedImage[row, col] = BilinearSample(myRow, myCol);
                }
            });
            return resizedImage;
        }

        /// <summary>
        /// Modifies the current image to contain the result of a bitwise OR of this Snapshot 
        /// and the mask.  This technique can be used to merge data from two images.
        /// http://en.wikipedia.org/wiki/Bitmask#Image_masks
        /// </summary>
        /// <param name="mask">Mask Snapshot to use in the bitwise OR operation.</param>
        public void Or(Snapshot mask)
        {
            if (mask.Width != Width || mask.Height != Height)
            {
                throw new InvalidOperationException("mask Snapshot must be of equal size to this Snapshot");
            }

            int thisHeight = Height;
            int thisWidth = Width;
            Parallel.For( 0, thisHeight, (row) =>
            {
                for (int col = 0; col < thisWidth; col++)
                {
                    Color col1 = this[row, col];
                    Color col2 = mask[row, col];
                    this[row, col] = col1.Or(col2);
                }
            });
        }

        /// <summary>
        /// Modifies the current image to contain the result of a bitwise AND of this Snapshot 
        /// and the mask.  This technique can be used to remove data from an image.
        /// http://en.wikipedia.org/wiki/Bitmask#Image_masks
        /// </summary>
        /// <param name="mask">Mask Snapshot to use in the bitwise AND operation.</param>
        public void And(Snapshot mask)
        {
            if (mask.Width != Width || mask.Height != Height)
            {
                throw new InvalidOperationException("mask Snapshot must be of equal size to this Snapshot");
            }

            int thisHeight = Height;
            int thisWidth = Width;
            Parallel.For( 0, thisHeight, (row) =>
            {
                for (int col = 0; col < thisWidth; col++)
                {
                    Color col1 = this[row, col];
                    Color col2 = mask[row, col];
                    this[row, col] = col1.And(col2);
                }
            });
        }

        /// <summary>
        /// Find all instances of a subimage in this image. 
        /// </summary>
        /// <param name="subimage">The subimage to find in the larger image. Must be smaller than the image in both dimensions.</param>
        /// <returns>A Collection of rectangles indicating the matching locations.
        /// If there is no match, the Collection returned will be empty.</returns>
        public Collection<Rectangle> Find(Snapshot subimage)
        {
            return Find(subimage, null);
        }

        /// <summary>
        /// Find all instances of a subimage in this image. 
        /// </summary>
        /// <param name="subimage">The subimage to find in the larger image. Must be smaller than the image in both dimensions.</param>
        /// <param name="verifier">Custom SnapshotVerifier, used to compare the subimages.</param>
        /// <returns>A Collection of rectangles indicating the matching locations.
        /// If there is no match, the Collection returned will be empty.</returns>
        public Collection<Rectangle> Find(Snapshot subimage, SnapshotVerifier verifier)
        {
            if (Width < subimage.Width || Height < subimage.Height)
            {
                throw new InvalidOperationException("Subimage snapshot must fit into larger snapshot to be found.");
            }

            Collection<Rectangle> resultList = new Collection<Rectangle>();
            
            // This is a binary 2D convolve with early exit on any imperfect match.
            // Future optimisations will test the middle pixel first, then a diagonal through the subarea, then the full image.
            // This will make pathological cases much faster.
            for (int row = 0; row < Height - subimage.Height; row++)
            {
                for (int column = 0; column < Width - subimage.Width; column++)
                {
                    // This is our success condition, add this location to the Collection.
                    if (CompareSubImage(subimage, row, column, verifier)) 
                    {
                        resultList.Add((new Rectangle(column, row, subimage.Width, subimage.Height)));
                    }
                }
            }

            return resultList;
        }

        #endregion

        #region Public Properties
        /// <summary>
        /// Returns a Color instance for the pixel at the specified row and column.
        /// </summary>
        /// <param name="row">Zero-based row position of the pixel.</param>
        /// <param name="column">Zero-based column position of the pixel.</param>
        /// <returns>A Color instance for the pixel at the specified row and column.</returns>
        // Suppressed the warning as our usage is more obvious than single argument indexer for a 2D array
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1023")]
        public Color this[int row, int column]
        {
            get
            {
                IsValidPixel(row, column);
                return buffer[row, column];
            }
            set
            {
                IsValidPixel(row, column);
                buffer[row, column] = value;
            }
        }

        /// <summary>
        /// Returns the width of the pixel buffer.
        /// </summary>
        public int Width
        {
            get
            {
                return buffer.GetLength(1);
            }
        }

        /// <summary>
        /// Returns the height of the pixel buffer.
        /// </summary>
        public int Height
        {
            get
            {
                return buffer.GetLength(0);
            }
        }
        #endregion

        #region Private Methods

        /// <summary>
        /// Private subimage comparer to help FindSubImage. This performs the actual work of comparing pixels.
        /// </summary>
        /// <param name="subimage">The subimage to find.</param>
        /// <param name="startingRow">The row offset into this snapshot to look for the subimage.</param>
        /// <param name="startingColumn">The column offset into this snapshot to look for the subimage.</param>
        /// <param name="verifier">Custom subimage comparison verifier, if specified.</param>
        /// <returns></returns>
        private bool CompareSubImage(
            Snapshot subimage, 
            int startingRow, 
            int startingColumn,
            SnapshotVerifier verifier)
        {         
            //compare pixel-by-pixel
            for (int row = 0; row < subimage.Height; row++)
            {
                for (int column = 0; column < subimage.Width; column++)
                {       
                    // Use the custom verifier if the user has specified one
                    if (verifier != null)
                    {
                        //verify this pixel
                        Snapshot thisPixelImage = this.Crop(new Rectangle(startingColumn + column, startingRow + row, 1, 1));
                        Snapshot thatPixelImage = subimage.Crop(new Rectangle(column, row, 1, 1));
                        VerificationResult subCompareResult = verifier.Verify(thisPixelImage.CompareTo(thatPixelImage, false));
                        if (subCompareResult == VerificationResult.Fail)
                        {
                            //mismatch
                            return false;
                        }
                    }
                    else
                    {
                        Color thisPixel = this[startingRow + row, startingColumn + column];
                        Color thatPixel = subimage[row, column];
                        int R = thisPixel.R - thatPixel.R;
                        int G = thisPixel.G - thatPixel.G;
                        int B = thisPixel.B - thatPixel.B;
                        int A = thisPixel.A - thatPixel.A;

                        if (R != 0x00 || G != 0x00 || B != 0x00)
                        {
                            //mismatch.
                            return false;
                        }
                    }
                        
                    
                }
            }

            return true;
        }

        /// <summary>
        /// Bilinearly interpolate to blend the pixels around the specified point - Takes the weighted average of the nearest four pixels.
        /// Not mip-mapped - downsampling can be mis-representative.
        /// </summary>
        /// <param name="row">The row being sampled on.</param>
        /// <param name="col">The column being sampled on.</param>
        /// <returns></returns>
        private Color BilinearSample(float row, float col)
        {
            int left = (int)Math.Max(0, Math.Floor(col));
            int right = (int)Math.Min(Width - 1, Math.Floor(col) + 1);

            int top = (int)Math.Max(0, Math.Floor(row));
            int bottom = (int)Math.Min(Height - 1, Math.Floor(row) + 1);

            float wBottom = (col - (float)Math.Floor(col)); //Weight of bottom pixels
            float wRight = (row - (float)Math.Floor(row)); //Weight of right pixels

            HighFidelityColor topLeft = new HighFidelityColor(this[top, left]).Modulate((1 - wBottom) * (1 - wRight));
            HighFidelityColor topRight = new HighFidelityColor(this[top, right]).Modulate((1 - wBottom) * wRight);
            HighFidelityColor bottomLeft = new HighFidelityColor(this[bottom, left]).Modulate(wBottom * (1 - wRight));
            HighFidelityColor bottomRight = new HighFidelityColor(this[bottom, right]).Modulate(wBottom * wRight);

            return (topLeft + topRight + bottomLeft + bottomRight).ToColor();

        }

        /// <summary>
        /// Instantiates a Bitmap with contents of TestBitmap.
        /// </summary>
        /// <returns>A Bitmap containing the a copy of the Snapshot buffer data.</returns>
        unsafe private System.Drawing.Bitmap CreateBitmapFromSnapshot()
        {
            System.Drawing.Bitmap temp = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);

            Rectangle bounds = new Rectangle(Point.Empty, temp.Size);
            BitmapData bitmapData = temp.LockBits(bounds, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            Byte* pBase = (Byte*)bitmapData.Scan0.ToPointer();
            int thisHeight = Height;
            int thisWidth = Width;
            Parallel.For(0, thisHeight, (row) =>
            {
                for (int column = 0; column < thisWidth; column++)
                {
                    // The active implementation is the faster, but unsafe alternative to setpixel API:
                    //    temp.SetPixel(column,row,buffer[row, column]);

                    PixelData* pixelDataAddress = (PixelData*)(pBase + row * thisWidth * sizeof(PixelData) + column * sizeof(PixelData));
                    pixelDataAddress->A = buffer[row, column].A;
                    pixelDataAddress->R = buffer[row, column].R;
                    pixelDataAddress->G = buffer[row, column].G;
                    pixelDataAddress->B = buffer[row, column].B;
                }
            });
            temp.UnlockBits(bitmapData);
            return temp;
        }

        /// <summary>
        /// Captures a Bitmap from the deviceContext on the specified areaToCopy.
        /// </summary>
        /// <param name="sourceDeviceContext">The device context to capture the region from.</param>
        /// <param name="rectangle">The rectangular bounds of the area to be captured.</param>
        /// <returns>A Bitmap representation of the region specified.</returns>
        static private Bitmap CaptureBitmap(IntPtr sourceDeviceContext, Rectangle rectangle)
        {
            //Empty rectangle semantic is undefined.
            if (rectangle.IsEmpty)
            {
                throw new ArgumentOutOfRangeException("rectangle");
            }

            IntPtr handleDeviceContextSrc = sourceDeviceContext;
            IntPtr handleDeviceContextDestination = IntPtr.Zero;
            IntPtr handleBmp = IntPtr.Zero;
            IntPtr handlePreviousObj = IntPtr.Zero;
            System.Drawing.Bitmap bmp = null;

            try
            {
                // Allocate memory for the bitmap
                handleBmp = NativeMethods.CreateCompatibleBitmap(handleDeviceContextSrc, rectangle.Width, rectangle.Height);
                if (handleBmp == IntPtr.Zero) { throw new Win32Exception(); }

                // Create destination DC
                handleDeviceContextDestination = NativeMethods.CreateCompatibleDC(handleDeviceContextSrc);
                if (handleDeviceContextDestination == IntPtr.Zero) { throw new Win32Exception(); }

                // copy screen to bitmap
                handlePreviousObj = NativeMethods.SelectObject(handleDeviceContextDestination, handleBmp);
                if (handlePreviousObj == IntPtr.Zero) { throw new Win32Exception(); }

                // Note : CAPTUREBLT is needed to capture layered windows
                bool result = NativeMethods.BitBlt(handleDeviceContextDestination, 0, 0, rectangle.Width, rectangle.Height, handleDeviceContextSrc, rectangle.Left, rectangle.Top, (Int32)(RasterOperationCodeEnum.SRCCOPY | RasterOperationCodeEnum.CAPTUREBLT));
                if (result == false) { throw new Win32Exception(); }

                //Convert Win32 Handle to Bitmap to a Winforms Bitmap
                bmp = Bitmap.FromHbitmap(handleBmp);
            }

            // Do Unmanaged cleanup
            finally
            {
                if (handlePreviousObj != IntPtr.Zero)
                {
                    NativeMethods.SelectObject(handleDeviceContextDestination, handlePreviousObj);
                    handlePreviousObj = IntPtr.Zero;
                }
                if (handleDeviceContextDestination != IntPtr.Zero)
                {
                    NativeMethods.DeleteDC(handleDeviceContextDestination);
                    handleDeviceContextDestination = IntPtr.Zero;
                }

                if (handleBmp != IntPtr.Zero)
                {
                    NativeMethods.DeleteObject(handleBmp);
                    handleBmp = IntPtr.Zero;
                }
            }
            return bmp;
        }

        /// <summary>
        /// Tests if the specified pixel coordinate is contained within the bounds of the buffer.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        private void IsValidPixel(int row, int column)
        {
            if (row < 0 || row >= Height)
            {
                throw new ArgumentOutOfRangeException("row");
            }

            if (column < 0 || column >= Width)
            {
                throw new ArgumentOutOfRangeException("column");
            }
        }

        #endregion

        #region Private Fields and Structures
        /// <summary>
        /// A BGRA pixel data structure - This is only used for data conversion and export purposes for conversion with Bitmap buffer. 
        /// NOTE: This order aligns with 32 bpp ARGB pixel Format.
        /// </summary>
        private struct PixelData
        {
            public byte B;
            public byte G;
            public byte R;
            public byte A;
        }

        /// <summary>
        /// The color buffer is organized in row-Major form i.e. [row, column] => [y,x]
        /// </summary>
        private Color[,] buffer;

        #endregion
    }
}
