// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.IO;

using Windows.Win32.Graphics.Gdi;

namespace MS.Internal
{
    internal abstract class SystemDrawingExtensionMethods
    {
        /// <summary>
        ///  Return <see langword="true"/> if the data is a System.Drawing.Bitmap.
        /// </summary>
        /// <param name="data">The data to check.</param>
        /// <returns><see langword="true"/> if the data is a bitmap, otherwise <see langword="false"/>.</returns>
        internal abstract bool IsBitmap(object? data);

        /// <summary>
        ///  Return <see langword="true"/> if the data is a System.Drawing.Image.
        /// </summary>
        /// <param name="data">The data to check.</param>
        /// <returns><see langword="true"/> if the data is an image, otherwise <see langword="false"/>.</returns>
        internal abstract bool IsImage(object? data);

        /// <summary>
        ///  Return <see langword="true"/> if the data is a System.Drawing.Metafile.
        /// </summary>
        /// <param name="data">The data to check.</param>
        /// <returns><see langword="true"/> if the data is a metafile, otherwise <see langword="false"/>.</returns>
        internal abstract bool IsMetafile(object? data);

        /// <summary>
        ///  Return the handle from a metafile.
        /// </summary>
        /// <param name="data">The metafile data.</param>
        /// <returns>The <see cref="HENHMETAFILE"/> handle of the metafile.</returns>
        internal abstract nint GetHandleFromMetafile(object? data);

        /// <summary>
        ///  Get the metafile from the <see cref="HENHMETAFILE"/> handle of the enhanced metafile.
        /// </summary>
        /// <param name="hMetafile">The handle of the enhanced metafile.</param>
        /// <returns>The metafile object.</returns>
        internal abstract object GetMetafileFromHemf(nint hMetafile);

        /// <summary>
        ///  Get a System.Drawing.Bitmap from the given data (either BitmapSource or Bitmap).
        /// </summary>
        /// <param name="data">The data to get the bitmap from.</param>
        /// <returns>The bitmap object.</returns>
        internal abstract object? GetBitmap(object? data);

        /// <summary>
        ///  Get a <see cref="HBITMAP"/> bitmap handle from the given data (either BitmapSource or Bitmap).
        ///  Also return its width and height.
        /// </summary>
        /// <param name="data">The data to get the bitmap handle from.</param>
        /// <param name="width">The width of the bitmap.</param>
        /// <param name="height">The height of the bitmap.</param>
        /// <returns>The handle of the bitmap.</returns>
        internal abstract nint GetHBitmap(object? data, out int width, out int height);

        /// <summary>
        ///  Get a <see cref="HBITMAP"/> bitmap handle from a Bitmap.
        /// </summary>
        /// <param name="data">The bitmap data.</param>
        /// <returns>The handle of the bitmap.</returns>
        internal abstract nint GetHBitmapFromBitmap(object? data);

        /// <summary>
        ///  Convert a metafile to a <see cref="HBITMAP"/>.
        /// </summary>
        /// <param name="handle">The <see cref="HENHMETAFILE"/> handle of the metafile.</param>
        /// <returns>The <see cref="HBITMAP"/> handle.</returns>
        internal abstract nint ConvertMetafileToHBitmap(nint handle);

        /// <summary>
        ///  Return a stream for the ExifUserComment in the given Gif.
        /// </summary>
        /// <param name="stream">The Gif stream.</param>
        /// <returns>The stream for the ExifUserComment.</returns>
        internal abstract Stream GetCommentFromGifStream(Stream stream);

        /// <summary>
        ///  Write a metafile stream to the output stream in PNG format.
        /// </summary>
        /// <param name="metafileStream">The metafile stream.</param>
        /// <param name="imageStream">The output image stream.</param>
        internal abstract void SaveMetafileToImageStream(MemoryStream metafileStream, Stream imageStream);

        /// <summary>
        ///  Takes a BitmapImage and converts it to a Bitmap so it can be put on the clipboard.
        /// </summary>
        /// <param name="source">The BitmapImage source.</param>
        /// <returns>The bitmap object.</returns>
        internal abstract object GetBitmapFromBitmapSource(object source);
    }
}
