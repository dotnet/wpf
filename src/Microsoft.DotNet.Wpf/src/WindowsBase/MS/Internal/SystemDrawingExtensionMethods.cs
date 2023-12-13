// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Helper methods for code that uses types from System.Drawing.
//

using System;
using System.IO;
using System.Security;

namespace MS.Internal
{
    internal abstract class SystemDrawingExtensionMethods
    {
        // return true if the data is a bitmap
        internal abstract bool IsBitmap(object data);

        // return true if the data is an Image
        internal abstract bool IsImage(object data);

        // return true if the data is a graphics metafile
        internal abstract bool IsMetafile(object data);

        // return the handle from a metafile
        internal abstract IntPtr GetHandleFromMetafile(Object data);

        // Get the metafile from the handle of the enhanced metafile.
        internal abstract Object GetMetafileFromHemf(IntPtr hMetafile);

        // Get a bitmap from the given data (either BitmapSource or Bitmap)
        internal abstract object GetBitmap(object data);

        // Get a bitmap handle from the given data (either BitmapSource or Bitmap)
        // Also return its width and height.
        internal abstract IntPtr GetHBitmap(object data, out int width, out int height);

        // Get a bitmap handle from a Bitmap
        internal abstract IntPtr GetHBitmapFromBitmap(object data);

        // Convert a metafile to HBitmap
        internal abstract IntPtr ConvertMetafileToHBitmap(IntPtr handle);

        // return a stream for the ExifUserComment in the given Gif
        internal abstract Stream GetCommentFromGifStream(Stream stream);

        // write a metafile stream to the output stream in PNG format
        internal abstract void SaveMetafileToImageStream(MemoryStream metafileStream, Stream imageStream);

        //returns bitmap snapshot of selected area
        //this code takes a BitmapImage and converts it to a Bitmap so it can be put on the clipboard
        internal abstract object GetBitmapFromBitmapSource(object source);
    }
}
