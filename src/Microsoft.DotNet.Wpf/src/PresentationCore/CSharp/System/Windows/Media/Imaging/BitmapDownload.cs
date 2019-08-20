// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
#pragma warning disable 1634, 1691 // Allow suppression of certain presharp messages

using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using System.Threading;
using System.Windows.Threading;
using MS.Internal;
using System.Diagnostics;
using System.Windows.Media;
using System.Globalization;
using System.Security;
using System.Runtime.InteropServices;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows.Media.Imaging;
using MS.Win32.PresentationCore;
using MS.Internal.AppModel;
using MS.Internal.PresentationCore;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
using System.Net;
using System.Net.Cache;
using System.Text;
using MS.Win32;
using Microsoft.Win32.SafeHandles;

namespace System.Windows.Media.Imaging
{
    #region QueueEntry

    ///
    /// QueueEntry
    ///
    internal class QueueEntry
    {
        internal List<WeakReference> decoders;
        internal Uri inputUri;
        internal Stream inputStream;
        internal Stream outputStream;
        internal string streamPath;
        internal byte[] readBuffer;
        internal long contentLength;
        internal string contentType;
        internal int lastPercent;
        internal WebRequest webRequest;
    }

    #endregion

    #region BitmapDownload

    ///
    /// The BitmapDownload class provides a way to download streams off-thread to
    /// a cache
    ///
    internal static class BitmapDownload
    {           
        static BitmapDownload()
        {
            _waitEvent = new AutoResetEvent(false);

            _workQueue = Queue.Synchronized(new Queue());

            _uriTable = Hashtable.Synchronized(new Hashtable());

            _readCallback = new AsyncCallback(ReadCallback);

            _responseCallback = new AsyncCallback(ResponseCallback);

            _thread = new Thread(new ThreadStart(DownloadThreadProc));

            _syncLock = new object();
}
        #region Methods

        ///
        /// Begin a download
        ///
        internal static void BeginDownload(
            BitmapDecoder decoder, 
            Uri uri, 
            RequestCachePolicy uriCachePolicy, 
            Stream stream
            )
        {
            lock (_syncLock)
            {
                if (!_thread.IsAlive)
                {
                    _thread.IsBackground = true;
                    _thread.Start();
                }
            }

            QueueEntry entry;

            // If there is already a download for this uri, just add the decoder to the list
            if (uri != null)
            {
                lock (_syncLock)
                {
                    if (_uriTable[uri] != null)
                    {
                        entry = (QueueEntry)_uriTable[uri];
                        entry.decoders.Add(new WeakReference(decoder));

                        return;
                    }
                }
            }

            entry = new QueueEntry();
            entry.decoders  = new List<WeakReference>();

            lock (_syncLock)
            {
                entry.decoders.Add(new WeakReference(decoder));
            }

            entry.inputUri = uri;
            entry.inputStream = stream;

            string cacheFolder = MS.Win32.WinInet.InternetCacheFolder.LocalPath;
            bool passed = false;

            // Get the file path 
            StringBuilder tmpFileName = new StringBuilder(NativeMethods.MAX_PATH);
            MS.Win32.UnsafeNativeMethods.GetTempFileName(cacheFolder, "WPF", 0, tmpFileName);
              
            try
            {
                string pathToUse = tmpFileName.ToString();
                SafeFileHandle fileHandle = MS.Win32.UnsafeNativeMethods.CreateFile(
                    pathToUse, 
                    NativeMethods.GENERIC_READ | NativeMethods.GENERIC_WRITE, /* dwDesiredAccess */
                    0,                                                        /* dwShare */
                    null,                                                     /* lpSecurityAttributes */
                    NativeMethods.CREATE_ALWAYS,                              /* dwCreationDisposition */
                    NativeMethods.FILE_ATTRIBUTE_TEMPORARY | 
                    NativeMethods.FILE_FLAG_DELETE_ON_CLOSE,                  /* dwFlagsAndAttributes */
                    IntPtr.Zero                                               /* hTemplateFile */
                    );

                if (fileHandle.IsInvalid)
                {
                    throw new Win32Exception();
                }
                    
                entry.outputStream = new FileStream(fileHandle, FileAccess.ReadWrite);
                entry.streamPath = pathToUse;
                passed = true;
            }
            catch(Exception e)
            {
                if (CriticalExceptions.IsCriticalException(e))
                {
                    throw;
                }
            }

            if (!passed)
            {
                throw new IOException(SR.Get(SRID.Image_CannotCreateTempFile));
            }

            entry.readBuffer  = new byte[READ_SIZE];
            entry.contentLength = -1;
            entry.contentType = string.Empty;
            entry.lastPercent = 0;

            // Add the entry to the table if we know the uri
            if (uri != null)
            {
                lock (_syncLock)
                {
                    _uriTable[uri] = entry;
                }
            }

            if (stream == null)
            {
                entry.webRequest = WpfWebRequestHelper.CreateRequest(uri);
                if (uriCachePolicy != null)
                {
                    entry.webRequest.CachePolicy = uriCachePolicy;
                }

                entry.webRequest.BeginGetResponse(_responseCallback, entry);
            }
            else
            {
                _workQueue.Enqueue(entry);
                // Signal
                _waitEvent.Set();
            }
        }

        ///
        /// Thread Proc
        ///
        internal static void DownloadThreadProc()
        {
            Queue workQueue = _workQueue;
            for (;;)
            {
                _waitEvent.WaitOne();

                while (workQueue.Count != 0)
                {
                    QueueEntry entry = (QueueEntry)workQueue.Dequeue();

                    #pragma warning disable 6500

                    // Catch all exceptions and marshal them to the correct thread
                    try
                    {
                        entry.inputStream.BeginRead(
                            entry.readBuffer,
                            0,
                            READ_SIZE,
                            _readCallback,
                            entry
                            );
                    }
                    catch (Exception e)
                    {
                        MarshalException(entry, e);
                    }
                    finally
                    {
                        //
                        // This method never exits, and 'entry' is _not_ scoped
                        // to the while loop, so if we don't null entry out it will
                        // be rooted while _waitEvent.WaitOne() blocks.
                        //
                        entry = null;
                    }

                    #pragma warning restore 6500
                }
            }
        }

        ///
        /// Response callback
        private static void ResponseCallback(IAsyncResult result)
        {
            QueueEntry entry = (QueueEntry)result.AsyncState;

            #pragma warning disable 6500

            // Catch all exceptions and marshal them to the correct thread
            try
            {
                WebResponse response = WpfWebRequestHelper.EndGetResponse(entry.webRequest, result);
                entry.inputStream = response.GetResponseStream();
                entry.contentLength = response.ContentLength;
                entry.contentType = response.ContentType;
                entry.webRequest = null; // GC the WebRequest
                _workQueue.Enqueue(entry);

                // Signal
                _waitEvent.Set();
            }
            catch(Exception e)
            {
                MarshalException(entry, e);
            }

            #pragma warning restore 6500
        }

        ///
        /// Read callback
        ///
        private static void ReadCallback(IAsyncResult result)
        {
            QueueEntry entry = (QueueEntry)result.AsyncState;
            int bytesRead = 0;

            #pragma warning disable 6500

            try
            {
                bytesRead = entry.inputStream.EndRead(result);
            }
            catch(Exception e)
            {
                MarshalException(entry, e);
            }

            #pragma warning restore 6500

            if (bytesRead == 0)
            {
                //
                // We're done reading from the input stream.
                //
                entry.inputStream.Close();
                entry.inputStream = null;

                //
                // We're done writing to the output stream. Make sure everything
                // is written to disk.
                //
                entry.outputStream.Flush();
                entry.outputStream.Seek(0, SeekOrigin.Begin);


                lock (_syncLock)
                {
                    // Fire download progress & completion event for each decoder
                    foreach (WeakReference decoderReference in entry.decoders)
                    {
                        LateBoundBitmapDecoder decoder = decoderReference.Target as LateBoundBitmapDecoder;
                        if (decoder != null)
                        {
                            //
                            // Marshal events to UI thread
                            //
                            MarshalEvents(
                                decoder,
                                new DispatcherOperationCallback(decoder.ProgressCallback),
                                100
                                );

                            MarshalEvents(
                                decoder,
                                new DispatcherOperationCallback(decoder.DownloadCallback),
                                entry.outputStream
                                );
                        }
                    }
                }

                // Delete entry from uri table
                if (entry.inputUri != null)
                {
                    lock (_syncLock)
                    {
                        _uriTable[entry.inputUri] = null;
                    }
                }
            }
            else
            {
                entry.outputStream.Write(entry.readBuffer, 0, bytesRead);

                if (entry.contentLength > 0)
                {
                    int percentComplete = (int)Math.Floor(100.0 * (double)entry.outputStream.Length / (double)entry.contentLength);

                    // Only raise if percentage went up by ~1% (ie value changed).
                    if (percentComplete != entry.lastPercent)
                    {
                        // Update last value
                        entry.lastPercent = percentComplete;

                        lock (_syncLock)
                        {
                            // Fire download progress event for each decoder
                            foreach (WeakReference decoderReference in entry.decoders)
                            {
                                LateBoundBitmapDecoder decoder = decoderReference.Target as LateBoundBitmapDecoder;
                                if (decoder != null)
                                {
                                    //
                                    // Marshal events to UI thread
                                    //
                                    MarshalEvents(
                                        decoder,
                                        new DispatcherOperationCallback(decoder.ProgressCallback),
                                        percentComplete
                                        );
                                }
                            }
                        }
                    }
                }

                _workQueue.Enqueue(entry);
                _waitEvent.Set();
            }
        }

        ///
        /// Marshals a call to the Dispatcher thread
        ///
        private static void MarshalEvents(
            LateBoundBitmapDecoder decoder,
            DispatcherOperationCallback doc,
            object arg
            )
        {
            Dispatcher dispatcher = decoder.Dispatcher;
            if (dispatcher != null)
            {
                dispatcher.BeginInvoke(DispatcherPriority.Normal, doc, arg);
            }
            else
            {
                // Decoder seems to be thread free. This is probably bad
                Debug.Assert(false);
            }
        }

        ///
        /// Marshal an exception to the Dispatcher thread
        ///
        private static void MarshalException(
            QueueEntry entry,
            Exception e
            )
        {
            lock (_syncLock)
            {
                // Fire download completion event for each decoder
                foreach (WeakReference decoderReference in entry.decoders)
                {
                    LateBoundBitmapDecoder decoder = decoderReference.Target as LateBoundBitmapDecoder;
                    if (decoder != null)
                    {
                        MarshalEvents(
                            decoder,
                            new DispatcherOperationCallback(decoder.ExceptionCallback),
                            e
                            );
                    }
                }

                if (entry.inputUri != null)
                {
                    lock (_syncLock)
                    {
                        _uriTable[entry.inputUri] = null;
                    }
                }
            }
        }

        #endregion

        #region Data Members

        /// Thread event
        internal static AutoResetEvent _waitEvent = new AutoResetEvent(false);

        /// Work Queue
        internal static Queue _workQueue;

        /// Uri hash table
        internal static Hashtable _uriTable;

        /// Read callback
        internal static AsyncCallback _readCallback;

        /// Response callback
        internal static AsyncCallback _responseCallback;

        /// Thread object
        private static Thread _thread;

        /// lock object
        private static object _syncLock;

        /// Default async read size
        private const int READ_SIZE = 1024;

        #endregion
    }

    #endregion
}

