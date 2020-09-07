// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Helper methods for code that uses types from System.Drawing.

using System;
using System.Security;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;

using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MS.Internal
{
    //FxCop can't tell that this class is instantiated via reflection, so suppress the FxCop warning.
    [SuppressMessage("Microsoft.Performance","CA1812:AvoidUninstantiatedInternalClasses")]
    internal class SystemDrawingExtension : SystemDrawingExtensionMethods
    {
        // return true if the data is a bitmap
        internal override bool IsBitmap(object data)
        {
            return data is Bitmap;
        }

        // return true if the data is an Image
        internal override bool IsImage(object data)
        {
            return data is Image;
        }

        // return true if the data is a graphics metafile
        internal override bool IsMetafile(object data)
        {
            return data is Metafile;
        }

        // return the handle from a metafile
        internal override IntPtr GetHandleFromMetafile(Object data)
        {
            IntPtr hMetafile = IntPtr.Zero;
            Metafile metafile = data as Metafile;

            if (metafile != null)
            {
                // Get the Windows handle from the metafile object.
                hMetafile = metafile.GetHenhmetafile();
            }

            return hMetafile;
        }

        // Get the metafile from the handle of the enhanced metafile.
        internal override Object GetMetafileFromHemf(IntPtr hMetafile)
        {
            return new Metafile(hMetafile, false);
        }

        // Get a bitmap from the given data (either BitmapSource or Bitmap)
        internal override object GetBitmap(object data)
        {
            return GetBitmapImpl(data);
        }

        // Get a bitmap handle from the given data (either BitmapSource or Bitmap)
        // Also return its width and height.
        internal override IntPtr GetHBitmap(object data, out int width, out int height)
        {
            Bitmap bitmapData = GetBitmapImpl(data);

            if (bitmapData == null)
            {
                width = height = 0;
                return IntPtr.Zero;
            }

            // GDI+ returns a DIBSECTION based HBITMAP. The clipboard deals well
            // only with bitmaps created using CreateCompatibleBitmap(). So, we
            // convert the DIBSECTION into a compatible bitmap.
            width = bitmapData.Size.Width;
            height = bitmapData.Size.Height;
            return bitmapData.GetHbitmap();
        }

        // Get a bitmap handle from a Bitmap
        internal override IntPtr GetHBitmapFromBitmap(object data)
        {
            Bitmap bitmap = data as Bitmap;
            return (bitmap != null) ? bitmap.GetHbitmap() : IntPtr.Zero;
        }

        // Convert a metafile to HBitmap
        internal override IntPtr ConvertMetafileToHBitmap(IntPtr handle)
        {
            Metafile metafile = new Metafile(handle, false);

            // Initialize the bitmap size to render the metafile.
            int bitmapheight = metafile.Size.Height;
            int bitmapwidth =  metafile.Size.Width;

            // We use System.Drawing to render metafile into the bitmap.
            Bitmap bmp = new Bitmap(bitmapwidth, bitmapheight);
            Graphics graphics = Graphics.FromImage(bmp);
            // graphics.FillRectangle(new System.Drawing.SolidBrush(System.Drawing.Color.White), 0, 0, bitmapwidth, bitmapheight);
            graphics.DrawImage(metafile, 0, 0, bitmapwidth, bitmapheight);

            return bmp.GetHbitmap();
        }

        // return a stream for the ExifUserComment in the given Gif
        internal override Stream GetCommentFromGifStream(Stream stream)
        {
            // Read the GIF header ...
            Bitmap img = new Bitmap(stream);
            // Read the comment as that is where the ISF is stored...
            // for reference the tag is PropertyTagExifUserComment [0x9286] or 37510 (int)
            PropertyItem piComment = img.GetPropertyItem(37510);
            return new MemoryStream(piComment.Value);
        }

        // write a metafile stream to the output stream in PNG format
        internal override void SaveMetafileToImageStream(MemoryStream metafileStream, Stream imageStream)
        {
            Metafile metafile = new Metafile(metafileStream);
            metafile.Save(imageStream, ImageFormat.Png);
        }

        // Get a bitmap from the given data (either BitmapSource or Bitmap)
        private static Bitmap GetBitmapImpl(object data)
        {
            BitmapSource bitmapSource = data as BitmapSource;
            if (bitmapSource != null)
            {
                // Convert BitmapSource to System.Drawing.Bitmap to get Win32 HBITMAP.
                BitmapEncoder bitmapEncoder;
                Stream bitmapStream;

                bitmapEncoder = new BmpBitmapEncoder();
                bitmapEncoder.Frames.Add(BitmapFrame.Create(bitmapSource));

                bitmapStream = new MemoryStream();
                bitmapEncoder.Save(bitmapStream);

                return new Bitmap(bitmapStream);
            }
            else
            {
                // Get Bitmap data from data object.
                return data as Bitmap;
            }
        }

        //returns bitmap snapshot of selected area
        //this code takes a BitmapImage and converts it to a Bitmap so it can be put on the clipboard
        internal override object GetBitmapFromBitmapSource(object source)
        {
            BitmapSource contentImage = (BitmapSource)source;
            int imageWidth = (int)contentImage.Width;
            int imageHeight = (int)contentImage.Height;

            Bitmap bitmapFinal = new Bitmap(
                                        imageWidth,
                                        imageHeight,
                                        System.Drawing.Imaging.PixelFormat.Format32bppRgb);

            BitmapData bmData = bitmapFinal.LockBits(
                                    new Rectangle(0, 0, imageWidth, imageHeight),
                                    ImageLockMode.WriteOnly,
                                    System.Drawing.Imaging.PixelFormat.Format32bppRgb);

            FormatConvertedBitmap formatConverter = new FormatConvertedBitmap();
            formatConverter.BeginInit();
            formatConverter.Source = contentImage;
            formatConverter.DestinationFormat = System.Windows.Media.PixelFormats.Bgr32;
            formatConverter.EndInit();

            formatConverter.CopyPixels(
                        new Int32Rect(0, 0, imageWidth, imageHeight),
                        bmData.Scan0,
                        bmData.Stride * (bmData.Height - 1) + (bmData.Width * 4),
                        bmData.Stride);

            bitmapFinal.UnlockBits(bmData);

            return bitmapFinal;
        }

#if WindowsMetaFile
        // Convert the bitmap to the windows metafile to write it as "\wmetafile" control on rtf content
        internal override string ConvertToMetafileHexDataString(Stream imageStream)
        {
            MemoryStream metafileStream = new MemoryStream();

            // Get the graphics to write image on the metafile
            Graphics graphics = Graphics.FromHwnd(IntPtr.Zero);

            // Create the empty metafile
            Metafile metafile = new Metafile(metafileStream, graphics.GetHdc(), EmfType.EmfOnly);

            // Release HDC
            graphics.ReleaseHdc();

            // Create the graphics for metafile destination
            Graphics graphicsTarget = Graphics.FromImage(metafile);

            // Create bitmap from the image source stream
            Bitmap bitmap = new Bitmap(imageStream);

            // Draw the bitmap image to the metafile
            graphicsTarget.DrawImage(bitmap, Point.Empty);

            // Dispose graphics target
            graphicsTarget.Dispose();

            // Move to the start position
            metafileStream.Position = 0;

            // Create the enhance metafile from metafile stream what we rendered from the bitmap image
            Metafile enhanceMetafile = new Metafile(metafileStream);

            // Get the enhance metafile handle from the enhance metafile
            IntPtr hdc = UnsafeNativeMethods.CreateCompatibleDC(new HandleRef(this, IntPtr.Zero));
            IntPtr hEnhanceMetafile = enhanceMetafile.GetHenhmetafile();

            // Convert the enhance metafile to the windows metafile with Win32 GDI
            uint windowsMetafileSize = UnsafeNativeMethods.GetWinMetaFileBits(hEnhanceMetafile, 0, null, /*MapMode:MM_ANISOTROPIC*/8, hdc);
            Byte[] windowsMetafileBytes = new Byte[windowsMetafileSize];
            UnsafeNativeMethods.GetWinMetaFileBits(hEnhanceMetafile, windowsMetafileSize, windowsMetafileBytes, /*MapMode:MM_ANISOTROPIC*/8, hdc);

            // Dispose metafile and metafile stream
            metafile.Dispose();
            metafileStream.Dispose();

            // return the windows metafile hex data string
            return ConvertToImageHexDataString(windowsMetafileBytes);
        }
#endif
    }
}
