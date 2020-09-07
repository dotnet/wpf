// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description:
//      Interop service between managed ByteRangeDownloader and
//      and unmanaged ByteWrapper. This interface is implemented on
//      the client application end to support services to the 
//      unmanaged docobj hosted in the browser 
//
//  ***********************IMPORTANT**************************
//
//      If you change any of the interface definitions here
//      make sure you also change the interface definitions
//      in the managed side. If you are not sure about how to
//      define it here, TEMPORARILY mark the interface as 
//      ComVisible in the managed side, use tlbexp to generate
//      a typelibrary from the managed dll and copy the method
//      signatures from there. REMEMBER to remove the ComVisible
//      in the managed code when you are done. 
//      Defining the interfaces at both ends prevents us from
//      publicly exposing these interfaces to the outside world.
//      In order for marshaling to work correctly, the vtable
//      and data types should match EXACTLY in both the managed
//      and unmanaged worlds
//


using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Security;

namespace MS.Internal.Progressivity
{
    // <summary>
    // This interface is used to provide ByteRangeDownloader in Windows Client Applications
    // The unmanaged ByteWrapper communicates with ByteRangeDownloader through this
    // interface using COM interop.
    // </summary>
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("e7b92912-c7ca-4629-8f39-0f537cfab57e")]
    internal interface IByteRangeDownloaderService
    {
        // <summary>
        // Initialize the downloader for byte range request
        // </summary>
        // <param name="url">url to be downloaded</param>
        // <param name="tempFile">temporary file where the downloaded bytes should be saved</param>
        // <param name="eventHandle">event handle to be raised when a byte range request is done</param>
        void InitializeByteRangeDownloader(
            [MarshalAs(UnmanagedType.LPWStr)] string url,
            [MarshalAs(UnmanagedType.LPWStr)] string tempFile,
            SafeWaitHandle eventHandle);

        // <summary>
        // Make HTTP byte range web request
        // </summary>
        // <param name="byteRanges">byte ranges to be downloaded; byteRanges is one dimensional
        // array consisting pairs of offset and length</param>
        // <param name="size">number of elements in byteRanges</param>
        void RequestDownloadByteRanges(
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=1)] int [] byteRanges,
            int size);

        // <summary>
        // Get the byte ranges that are downloaded
        // </summary>
        // <param name="byteRanges">byte ranges that are downloaded; byteRanges is one dimensional
        // array consisting pairs of offset and length</param>
        // <param name="size">numbe of elements in byteRanges</param>
        void GetDownloadedByteRanges(
            [MarshalAs(UnmanagedType.LPArray)] out int [] byteRanges,
            [MarshalAs(UnmanagedType.I4)] out int size);

        // <summary>
        // Release the byte range downloader
        // </summary>
        void ReleaseByteRangeDownloader();
    }
}
