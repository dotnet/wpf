// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

// Description: Helper methods for code that uses types from System.Drawing.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

using System.Windows;
using System.Windows.Media.Imaging;

namespace MS.Internal
{
    //FxCop can't tell that this class is instantiated via reflection, so suppress the FxCop warning.
    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal class SystemDrawingExtension : SystemDrawingExtensionMethods
    {
        internal override bool IsBitmap(object? data) => data is Bitmap;

        internal override bool IsImage(object? data) => data is Image;

        internal override bool IsMetafile(object? data) => data is Metafile;

        internal override nint GetHandleFromMetafile(object? data) => data switch
        {
            Metafile metafile => metafile.GetHenhmetafile(),
            _ => 0
        };

        internal override object GetMetafileFromHemf(nint hMetafile) => new Metafile(hMetafile, deleteEmf: false);

        internal override object? GetBitmap(object? data) => GetBitmapImpl(data);

        internal override nint GetHBitmap(object? data, out int width, out int height)
        {
            Bitmap? bitmapData = GetBitmapImpl(data);

            if (bitmapData is null)
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

        internal override nint GetHBitmapFromBitmap(object? data) => data is Bitmap bitmap ? bitmap.GetHbitmap() : 0;

        internal override nint ConvertMetafileToHBitmap(nint handle)
        {
            Metafile metafile = new(handle, deleteEmf: false);

            // Initialize the bitmap size to render the metafile.
            int bitmapheight = metafile.Size.Height;
            int bitmapwidth = metafile.Size.Width;

            // We use System.Drawing to render metafile into the bitmap.
            Bitmap bmp = new Bitmap(bitmapwidth, bitmapheight);
            Graphics graphics = Graphics.FromImage(bmp);
            graphics.DrawImage(metafile, 0, 0, bitmapwidth, bitmapheight);

            return bmp.GetHbitmap();
        }

        internal override Stream GetCommentFromGifStream(Stream stream)
        {
            // Read the GIF header
            Bitmap bitmap = new(stream);

            // Read the comment as that is where the ISF is stored.
            // For reference the tag is PropertyTagExifUserComment [0x9286] or 37510 (int)
            PropertyItem? piComment = bitmap.GetPropertyItem(37510);
            return new MemoryStream(piComment!.Value!);
        }

        internal override void SaveMetafileToImageStream(MemoryStream metafileStream, Stream imageStream)
        {
            Metafile metafile = new(metafileStream);
            metafile.Save(imageStream, ImageFormat.Png);
        }

        // Get a bitmap from the given data (either BitmapSource or Bitmap)
        private static Bitmap? GetBitmapImpl(object? data)
        {
            if (data is BitmapSource bitmapSource)
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

        internal override object GetBitmapFromBitmapSource(object source)
        {
            BitmapSource contentImage = (BitmapSource)source;
            int imageWidth = (int)contentImage.Width;
            int imageHeight = (int)contentImage.Height;

            Bitmap bitmapFinal = new(imageWidth, imageHeight, PixelFormat.Format32bppRgb);

            BitmapData bmData = bitmapFinal.LockBits(
                new Rectangle(0, 0, imageWidth, imageHeight),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppRgb);

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
