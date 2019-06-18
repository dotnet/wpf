// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.IO;
using System.Collections;
using System.Security;
using System.Security.Permissions;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using MS.Internal;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Windows.Media;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Windows.Media.Imaging
{
    #region BmpBitmapDecoder

    /// <summary>
    /// The built-in Microsoft Bmp (Bitmap) Decoder.
    /// </summary>
    public sealed class BmpBitmapDecoder : BitmapDecoder
    {
        /// <summary>
        /// Don't allow construction of a decoder with no params
        /// </summary>
        private BmpBitmapDecoder()
        {
        }

        /// <summary>
        /// Create a BmpBitmapDecoder given the Uri
        /// </summary>
        /// <param name="bitmapUri">Uri to decode</param>
        /// <param name="createOptions">Bitmap Create Options</param>
        /// <param name="cacheOption">Bitmap Caching Option</param>
        /// <SecurityNote>
        /// Critical - access critical resource
        /// PublicOK - inputs verified or safe
        /// </SecurityNote>
        public BmpBitmapDecoder(
            Uri bitmapUri,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption
            ) : base(bitmapUri, createOptions, cacheOption, MILGuidData.GUID_ContainerFormatBmp)
        {
        }

        /// <summary>
        /// If this decoder cannot handle the bitmap stream, it will throw an exception.
        /// </summary>
        /// <param name="bitmapStream">Stream to decode</param>
        /// <param name="createOptions">Bitmap Create Options</param>
        /// <param name="cacheOption">Bitmap Caching Option</param>
        /// <SecurityNote>
        /// Critical - access critical resource
        /// PublicOK - inputs verified or safe
        /// </SecurityNote>
        public BmpBitmapDecoder(
            Stream bitmapStream,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption
            ) : base(bitmapStream, createOptions, cacheOption, MILGuidData.GUID_ContainerFormatBmp)
        {
        }

        /// <summary>
        /// Internal Constructor
        /// </summary>
        /// <SecurityNote>
        /// Critical: Uses a SafeFileHandle, which is a SecurityCritical type (in v4).
        ///     Calls SecurityCritical base class constructor.
        /// </SecurityNote>
        internal BmpBitmapDecoder(
            SafeMILHandle decoderHandle,
            BitmapDecoder decoder,
            Uri baseUri,
            Uri uri,
            Stream stream,
            BitmapCreateOptions createOptions,
            BitmapCacheOption cacheOption,
            bool insertInDecoderCache,
            bool originalWritable,
            Stream uriStream,
            UnmanagedMemoryStream unmanagedMemoryStream,
            SafeFileHandle safeFilehandle
            ) : base(decoderHandle, decoder, baseUri, uri, stream, createOptions, cacheOption, insertInDecoderCache, originalWritable, uriStream, unmanagedMemoryStream, safeFilehandle)
        {
        }

        #region Internal Abstract

        /// Need to implement this to derive from the "sealed" object
        internal override void SealObject()
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    #endregion
}

