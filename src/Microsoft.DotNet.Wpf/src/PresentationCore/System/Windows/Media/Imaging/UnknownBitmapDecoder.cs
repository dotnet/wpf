// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.IO;
using System.Collections;
using System.Security;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using Microsoft.Win32.SafeHandles;
using MS.Internal;
using System.Diagnostics;
using System.Windows.Media;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Windows.Media.Imaging
{
    #region UnknownBitmapDecoder

    /// <summary>
    /// The built-in Microsoft Unknown (Bitmap) Decoder.
    /// </summary>
    internal sealed class UnknownBitmapDecoder : BitmapDecoder
    {
        /// <summary>
        /// This class is to allow us to call CoInitialize when the UnknownBitmapDecoder
        /// is created, so that the unmanaged dll does not get unload until we are.
        /// </summary>
        private class CoInitSafeHandle : SafeMILHandle
        {
            public CoInitSafeHandle()
            {
                MS.Win32.PresentationCore.UnsafeNativeMethods.WICCodec.CoInitialize(IntPtr.Zero);
            }

            protected override bool ReleaseHandle()
            {
                MS.Win32.PresentationCore.UnsafeNativeMethods.WICCodec.CoUninitialize();

                return true;
            }
        }

        /// <summary>
        /// Don't allow construction of a decoder with no params
        /// </summary>
        private UnknownBitmapDecoder()
        {
        }

        /// <summary>
        /// Internal Constructor
        /// </summary>
        internal UnknownBitmapDecoder(
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

        /// <summary>
        /// Store a safe handle to take care of calling CoInitialize
        /// and CoUninitialize for us when the object is created/disposed.
        /// </summary>
        private CoInitSafeHandle _safeHandle = new CoInitSafeHandle(); 
}

    #endregion
}

