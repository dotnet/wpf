// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  An object to provide a stream that supports a RCW-ed COM ILockBytes interface on top of a
//  managed class providing for progressivity through multiple simultaneous WebRequests.
//
// Notes:
//  Most of this code need not be re-entrant because only one thread is ever operating here.
//  The ReadCallback is the only code that will be entered on a separate thread.
//
//  The temp file is shared with the ByteRangeDownloader object (if in use) which is why
//  all access to the stream is protected by Mutex.
//
//              operation.
//              causes stack overflow
//              - only allocate byteRangeReadEvent if it might be used
//              - narrow IsolatedStorage scope to User level (GetUserStoreForDomain)
//              - re-enabled tracing
//              - removed Closed property
//              - introduce checked{} keyword where integers could overflow
//              - improved Length perf for non-cooperative servers
//              - return earlier when data is available from byte-range requests
//              - corrected block-merge logic to merge adjacent blocks
//              - documented what is and is not protected by _syncLock
//              - don't Release the mutex unless we won it (if we didn't time-out waiting)
//              - encapsulate finally block within lock() statement to ensure that the
//                finally is executed while we hold the lock
//              - always call base.Dispose() regardless of our state
//              BruceMac: SyncObject was defined as static
//              - _syncObject should be per-instance because it only shields access
//                to instance variables.
#if DEBUG
#define TRACE
#endif

using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections;               // for IComparer
using System.Diagnostics;               // for Debug.Assert
using System.Security;                  // SecurityCritical, SecurityTreatAsSafe
using System.IO.IsolatedStorage;        // for IsolatedStorageFileStream
using MS.Internal.IO.Packaging;         // ByteRangeDownloader
using MS.Internal.PresentationCore;     // for ExceptionStringTable

namespace MS.Internal.IO.Packaging
{
    /// <summary>
    /// Implements a Stream to support ILockBytes.  This supports progressive download for performant file access over HTTP
    /// </summary>
    /// <remarks>NetStream spawns two download requests.  One downloads the entire file and the other
    /// downloads portions as needed by calls to Read().</remarks>
    internal class NetStream: Stream
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="responseStream">stream we are based on</param>
        /// <param name="uri">URI to access - not marked as critical so no guarantees that it will remain private</param>
        /// <param name="fullStreamLength">actual length of responseStream (which does not support Length call)</param>
        /// <param name="originalRequest"> the original request that was used to get the responseStream </param>
        /// <param name="originalResponse"> the original response that was used to get the responseStream </param>
        internal NetStream(
            Stream responseStream,
            long fullStreamLength,
            Uri uri,
            WebRequest originalRequest, WebResponse originalResponse)
        {
#if DEBUG
            if (System.IO.Packaging.PackWebRequestFactory._traceSwitch.Enabled)
                System.Diagnostics.Trace.TraceInformation("NetStream.NetStream()");
#endif

            // check parms
            Invariant.Assert(uri != null);
            Invariant.Assert(responseStream != null);
            Invariant.Assert(originalRequest != null);
            Invariant.Assert(originalResponse != null);

            // use this to resolve random requests
            _uri = uri;         // uri we are reading from
            _fullStreamLength = fullStreamLength;
            _responseStream = responseStream;
            _originalRequest = originalRequest;

            // only attempt out-of-order requests on well-behaved HTTP servers
            // (Note: MSDN indicates that uri.Scheme is always lower case)
            if (fullStreamLength > 0 && ((String.Compare(uri.Scheme, Uri.UriSchemeHttp, StringComparison.Ordinal) == 0) ||
                (String.Compare(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal) == 0)))
            {
                _allowByteRangeRequests = true;
                _readEventHandles[(int)ReadEvent.ByteRangeReadEvent] = new AutoResetEvent(false);
            }

            // read events - two sources of data.  These events are signalled to indicate that new data is
            // available in the temp file
            _readEventHandles[(int)ReadEvent.FullDownloadReadEvent] = new AutoResetEvent(false);

            // we need to start this
            StartFullDownload();
        }


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
        #region Stream Interface
        /// <summary>
        /// Return the bytes requested
        /// </summary>
        /// <param name="buffer">destination buffer</param>
        /// <param name="offset">offset to write into that buffer</param>
        /// <param name="count">how many bytes requested</param>
        /// <returns>how many bytes were written into buffer</returns>
        /// <remarks>blocks until data is available</remarks>
        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckDisposed();
#if DEBUG
            if (System.IO.Packaging.PackWebRequestFactory._traceSwitch.Enabled)
                System.Diagnostics.Trace.TraceInformation("NetStream.Read() offset:{0} length:{1}", _position, count );
#endif

            PackagingUtilities.VerifyStreamReadArgs(this, buffer, offset, count);

            // quick exit
            if (count == 0)
                return count;

            int bytesRead = 0;

            checked
            {
                if (offset + count > buffer.Length)
                    throw new ArgumentException(SR.Get(SRID.IOBufferOverflow), "buffer");

                // make sure some data is in the stream - block until it is
                int bytesAvailable = GetData(new Block(_position, count));
                count = Math.Min(bytesAvailable, count);    // don't return more than they requested, and don't return more than is available

                // read into the buffer and return (if any data is available)
                if (count > 0)
                {
                    try
                    {
                        _tempFileMutex.WaitOne();

                        lock (PackagingUtilities.IsolatedStorageFileLock)
                        {
                            _tempFileStream.Seek(_position, SeekOrigin.Begin);      // align the temp stream with our logical position
                            bytesRead = _tempFileStream.Read(buffer, offset, count);     // read from the temp file
                        }
                    }
                    finally
                    {
                        _tempFileMutex.ReleaseMutex();
                    }

                   // Update our position - we do this last because the Stream contract guarantees the position is only updated
                    // if the Read() call was successful.
                    _position += bytesRead;
                }
            }

            return bytesRead;
        }


        /// <summary>
        /// Is stream readable?
        /// </summary>
        public override bool CanRead
        {
            get
            {
                return !_disposed;  // always true if we are not disposed
            }
        }


        /// <summary>
        /// Is stream seekable?
        /// </summary>
        /// <remarks>We MUST support seek as this is used to implement ILockBytes.ReadAt()</remarks>
        public override bool CanSeek
        {
            get
            {
                return !_disposed;  // always true if we are not disposed
            }
        }


        /// <summary>
        /// Is stream writeable?
        /// </summary>
        public override bool CanWrite
        {
            get
            {
                return false;       // we never support writing
            }
        }


        /// <summary>
        /// Seek
        /// </summary>
        /// <param name="offset">offset from origin</param>
        /// <param name="origin">origin of seek</param>
        /// <returns>zero</returns>
        /// <remarks>SeekOrigin.End can be expensive when operating against a server that fails to report the full length of the
        /// resource being downloaded. Use SeekOrigin.Begin or SeekOrigin.Current if possible.</remarks>
        public override long Seek(long offset, SeekOrigin origin)
        {
            CheckDisposed();

            long temp = 0;

            checked
            {
                switch (origin)
                {
                    case SeekOrigin.Begin:
                        {
                            temp = offset;
                            break;
                        }

                    case SeekOrigin.Current:
                        {
                            temp = _position + offset;
                            break;
                        }

                    case SeekOrigin.End:
                        {
                            temp = Length + offset;
                            break;
                        }

                    default:
                        {
                            throw new ArgumentOutOfRangeException("origin", SR.Get(SRID.SeekOriginInvalid));
                        }
                }
            }
            if (temp < 0)
            {
                throw new ArgumentException(SR.Get(SRID.SeekNegative));
            }

#if DEBUG
            if (System.IO.Packaging.PackWebRequestFactory._traceSwitch.Enabled)
                System.Diagnostics.Trace.TraceInformation("NetStream.Seek() pos:{0}", temp);
#endif
            _position = temp;
            return _position;
        }


        /// <summary>
        /// Logical byte position in this stream
        /// </summary>
        public override long Position
        {
            get
            {
                CheckDisposed();
                return _position;
            }
            set
            {
                CheckDisposed();

                if (value < 0)
                    throw new ArgumentException(SR.Get(SRID.SeekNegative));

#if DEBUG
                if (System.IO.Packaging.PackWebRequestFactory._traceSwitch.Enabled)
                    System.Diagnostics.Trace.TraceInformation("NetStream.set_Position() pos:{0}", value);
#endif

                _position = value;
            }
        }


        /// <summary>
        /// SetLength
        /// </summary>
        /// <exception cref="NotSupportedException">not supported</exception>
        public override void SetLength(long newLength)
        {
            throw new NotSupportedException(SR.Get(SRID.SetLengthNotSupported));
        }


        /// <summary>
        /// Write
        /// </summary>
        /// <exception cref="NotSupportedException">not supported</exception>
        public override void Write(byte[] buf, int offset, int count)
        {
            throw new NotSupportedException(SR.Get(SRID.WriteNotSupported));
        }


        /// <summary>
        /// Length
        /// </summary>
        public override long Length
        {
            get
            {
                CheckDisposed();

                // handle ftp servers that don't return a length
                if (_fullStreamLength < 0)
                {
                    // fallback for servers that refuse to provide the length of the resource
                    checked
                    {
                        // Length could not be determined so we need to block our caller
                        // while reading the entire stream to determine the length
                        long temp = _position;          // squirrel away for later
                        _position = _highWaterMark;     // make sure we get the full length in case they seek'd before call get_Length
                        byte[] buf = new byte[0x1000];

                        // when this while loop exits, _fullStreamLength contains the length of the stream
                        while (Read(buf, 0, buf.Length) > 0)
                            ;

                        // restore
                        _position = temp;
                    }
                }

                return _fullStreamLength;
            }
        }

        /// <summary>
        /// Flush
        /// </summary>
        public override void Flush()
        {
            // ignore flush calls as we are read-only by definition
        }

        #endregion  // Stream

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        /// <remarks>PreSharp 6519 dictates that we not throw exceptions from Dispose() methods.</remarks>
        protected override void Dispose(bool disposing)
        {
            // always call base.Dispose(bool) regardless of our state
            try
            {
                if (disposing)
                {
#if DEBUG
                    if (System.IO.Packaging.PackWebRequestFactory._traceSwitch.Enabled)
                        System.Diagnostics.Trace.TraceInformation("NetStream.Close()");
#endif
                    lock (_syncObject)
                    {
                        // ignore multiple calls
                        if (_disposed)
                            return;

                        try
                        {
#if DEBUG
                        if (System.IO.Packaging.PackWebRequestFactory._traceSwitch.Enabled)
                            System.Diagnostics.Trace.TraceInformation("NetStream.Dispose(bool) - mark as closed");
#endif

                            // No matter what, mark ourselves as disposed.
                            // This is critical to prevent race condition.
                            _disposed = true;

                            // release any blocked threads - Set() does not throw any exceptions
                            if (_readEventHandles[(int)ReadEvent.FullDownloadReadEvent] != null)
                                _readEventHandles[(int)ReadEvent.FullDownloadReadEvent].Set();
                            if (_readEventHandles[(int)ReadEvent.ByteRangeReadEvent] != null)
                                _readEventHandles[(int)ReadEvent.ByteRangeReadEvent].Set();

                            // Free ByteRangeDownloader
                            FreeByteRangeDownloader();

                            // Free Event Handles - should not throw
                            if (_readEventHandles[(int)ReadEvent.FullDownloadReadEvent] != null)
                            {
                                _readEventHandles[(int)ReadEvent.FullDownloadReadEvent].Close();
                                _readEventHandles[(int)ReadEvent.FullDownloadReadEvent] = null;
                            }
                            if (_readEventHandles[(int)ReadEvent.ByteRangeReadEvent] != null)
                            {
                                _readEventHandles[(int)ReadEvent.ByteRangeReadEvent].Close();
                                _readEventHandles[(int)ReadEvent.ByteRangeReadEvent] = null;
                            }

                            // Free Full Download
                            if (_responseStream != null)
                            {
                                _responseStream.Close();
                            }

                            FreeTempFile();
#if DEBUG
                        if (System.IO.Packaging.PackWebRequestFactory._traceSwitch.Enabled)
                            System.Diagnostics.Trace.TraceInformation("NetStream.Dispose(bool) - exiting");
#endif
                        }
                        finally
                        {
                            // final housekeeping
                            _responseStream = null;
                            _readEventHandles = null;
                            _byteRangesAvailable = null;
                            _readBuf = null;
                        }
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Starts the asynchronous full-file download request - after the response is available
        /// </summary>
        /// <remarks> byte-range requests will be entertained as appropriate during Read() calls</remarks>
        private void StartFullDownload()
        {
            _highWaterMark = 0;
            _readBuf = new byte[_bufferSize];

            // This outer lock is required to prevent creating an inverted lock pattern between PackagingUtilities.IsoStoreSyncRoot
            // and PackagingUtilities.IsolatedStorageFileLock further down in the code.
            lock (PackagingUtilities.IsoStoreSyncRoot)
            {
                // open the stream for read and write with a retry count of 3 (we try 3 times before giving up on name
                // collision)
                // no need for mutex because this is guaranteed to be the first access (ByteRangeDownloader not yet created
                // and BeginRead not yet started)
                lock (PackagingUtilities.IsolatedStorageFileLock)
                {
                    _tempFileStream = PackagingUtilities.CreateUserScopedIsolatedStorageFileStreamWithRandomName(
                        3, out _tempFileName);
                }
            }

            // initiate the data retrieval - must do this at least once to kick off the process
            _responseStream.BeginRead(_readBuf, 0, _readBuf.Length, new AsyncCallback(ReadCallBack), this);
        }

        /// <summary>
        /// Throw exception if we are already closed
        /// </summary>
        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException("Stream");
        }


        #region FullDownload
        /// <summary>
        /// ReadCallBack
        /// </summary>
        /// <param name="ar">async read result containing our NetLockBytes reference</param>
        /// <remarks>This method is called back when an async read is complete</remarks>
        private void ReadCallBack(IAsyncResult ar)
        {
            // prevent simultaneous BeginRead/EndRead
            // after this lock is released, either _highWaterMark or _fullDownloadComplete is updated (or we are closed)
            lock (_syncObject)
            {
                // make sure we always signal the event, even when we exit early (see finally clause)
                try
                {
                    // exit early if we are closed
                    if (_disposed)
                    {
#if DEBUG
                        if (System.IO.Packaging.PackWebRequestFactory._traceSwitch.Enabled)
                            System.Diagnostics.Trace.TraceInformation("NetStream.ReadCallBack() - exiting early because we are closed");
#endif
                        return;
                    }

                    // verify that it contains data
                    int read = _responseStream.EndRead(ar);
                    if (read > 0)
                    {
                        // append the data to our temp file
                        // synchronize access to the file
                        try
                        {
                            _tempFileMutex.WaitOne();

#if DEBUG
                            if (System.IO.Packaging.PackWebRequestFactory._traceSwitch.Enabled)
                                System.Diagnostics.Trace.TraceInformation("NetStream.ReadCallBack (offset,length):({0},{1})", _highWaterMark, read);
#endif
                            lock(PackagingUtilities.IsolatedStorageFileLock)
                            {
                                _tempFileStream.Seek(_highWaterMark, SeekOrigin.Begin);
                                _tempFileStream.Write(_readBuf, 0, read);
                                _tempFileStream.Flush();        // force flush because we are sharing this file with ByteRangeDownloader
                            }

                            checked
                            {
                                _highWaterMark += read; // update the high-water mark
                            }
                        }
                        finally
                        {
                            _tempFileMutex.ReleaseMutex();
                        }
                    }
                    else
                    {
#if DEBUG
                        if (System.IO.Packaging.PackWebRequestFactory._traceSwitch.Enabled)
                            System.Diagnostics.Trace.TraceInformation("NetStream.ReadCallBack() - read complete - EndRead() returned zero");
#endif
                        // set Length if not already done so
                        if (_fullStreamLength < 0)
                            _fullStreamLength = _highWaterMark;
                    }

                    // all done?
                    if (_fullStreamLength == _highWaterMark)
                    {
                        // prevent further requests
                        _fullDownloadComplete = true;
                    }
                }
                finally
                {
                    // Set the ManualResetEvent
                    if (!_disposed && _readEventHandles[(int)ReadEvent.FullDownloadReadEvent] != null)
                        _readEventHandles[(int)ReadEvent.FullDownloadReadEvent].Set();
                }
            }
            return;
        }

        #endregion

        #region ByteRangeRequest
        /// <summary>
        /// Ensure ByteRangeDownloader is created and available
        /// </summary>
        private void EnsureDownloader()
        {
            if (_byteRangeDownloader == null)
            {
                _byteRangeDownloader = new ByteRangeDownloader(_uri,
                                                               _tempFileStream,
                                                               _readEventHandles[(int)ReadEvent.ByteRangeReadEvent].SafeWaitHandle,
                                                               _tempFileMutex);

                _byteRangeDownloader.Proxy = _originalRequest.Proxy;

                _byteRangeDownloader.Credentials = _originalRequest.Credentials;
                _byteRangeDownloader.CachePolicy = _originalRequest.CachePolicy;

                _byteRangesAvailable = new ArrayList(); // byte ranges that are downloaded
            }
        }


        /// <summary>
        /// MakeByteRangeRequest
        /// </summary>
        /// <remarks>helper method to reduce complexity in GetData().</remarks>
        private void MakeByteRangeRequest(Block block)
        {
            // Currently HttpWebRequest.AddRange can only handle int while the offset of stream can be long
            //  we should not make additional webrequest in that case
            // block.Offset > Int32.MaxValue
            // block.Offset + block.Length - 1 > Int32.MaxValue
            // No need to do "checked" since block.Length > 0 && block.Length <= Int32.MaxValue
            if (block.Offset > (Int32.MaxValue - block.Length + 1))
                return;

            // spawn a request
            EnsureDownloader();

            // make it worth the trouble - pad out to some reasonable size
            if (block.Length < _additionalRequestMinSize)
            {
#if DEBUG
                if (System.IO.Packaging.PackWebRequestFactory._traceSwitch.Enabled)
                    System.Diagnostics.Trace.TraceInformation("NetStream.MakeByteRangeRequest() offset:{0} length:{1} (padded to {2})",
                        block.Offset, block.Length, _additionalRequestMinSize);
#endif
                block.Length = _additionalRequestMinSize;
            }
#if DEBUG
            else
            {
                if (System.IO.Packaging.PackWebRequestFactory._traceSwitch.Enabled)
                    System.Diagnostics.Trace.TraceInformation("NetStream.MakeByteRangeRequest() offset:{0} length:{1}", block.Offset, block.Length);
            }
#endif

            // don't ask for more than the stream can accomodate
            TrimBlockToStreamLength(block);

            checked
            {
                // don't ask if there is no data to ask for
                if (block.Length > 0)
                {
                    // request the data
                    int[,] ranges = new int[1, 2];

                    ranges[0, 0] = (int)block.Offset;
                    ranges[0, 1] = block.Length;

                    _byteRangeDownloader.RequestByteRanges(ranges);

                    _inAdditionalRequest = true;            // only do these one at a time
                }
            }
        }


        /// <summary>
        /// GetByteRangeData
        /// </summary>
        /// <remarks>PRECONDITION: lock (_syncObject).
        /// Side effects of updating _byteRangesAvailable and _inAdditionalRequest.</remarks>
        private void GetByteRangeData()
        {
            int[,] ranges;

            // query the ByteRangeDownloader for the details
            ranges = _byteRangeDownloader.GetDownloadedByteRanges();
            if (ranges.GetLength(0) > 0)
            {
                // Add our "fullDownload" range just in case we can satisfy a request that straddles the
                // boundary between the highWaterMark and a byte range.
                // We can just "blindly" add this every time because the merging code will keep the
                // growth from getting out of control.
                _byteRangesAvailable.Insert(0, new Block(0, (int)_highWaterMark));

                // add to our collection of previously downloaded ranges
                int r = 0;  // index into ranges

                while (r < ranges.GetLength(0))
                {
                    _byteRangesAvailable.Add(new Block(ranges[r,0], ranges[r,1]));
                    r++;
#if DEBUG
                    if (System.IO.Packaging.PackWebRequestFactory._traceSwitch.Enabled)
                        _unmergedBlocks++;  // statistics on merge performance
#endif
                }

                // sort them
                _byteRangesAvailable.Sort();     // must sort before merging

                // merge them
                MergeByteRanges(_byteRangesAvailable);
#if DEBUG
                // Note that this includes the "fullDownload" range
                if (System.IO.Packaging.PackWebRequestFactory._traceSwitch.Enabled)
                    System.Diagnostics.Trace.TraceInformation("NetStream.GetData() total byteranges:{0} after merging:{1}", _unmergedBlocks, _byteRangesAvailable.Count);
#endif
                _inAdditionalRequest = false;        // allow more byte-range requests
            }
        }


        /// <summary>
        /// IsByteRangeAvailable
        /// </summary>
        /// <param name="block">query</param>
        /// <returns>number of bytes that are available starting at the beginning of the given block</returns>
        private int BytesInByteRangeAvailable(Block block)
        {
            int bytesAvailable = 0;

            // we can be called even if the byteRangeDownloader is not in use
            if (_byteRangesAvailable != null)
            {
                checked
                {
                    // handle "over the end requests" which we truncate automatically when making the actual request
                    Debug.Assert(_fullStreamLength >= 0, "We assume _fullStreamLength is correct for Http cases - only Ftp can return bogus values");
                    TrimBlockToStreamLength(block);

                    // now search - could be replaced with BinarySearch as list is ordered by offset
                    foreach (Block data in _byteRangesAvailable)
                    {
                        // we need the bytes to start from the beginning because that's the
                        // only type of partial response we can give
                        if ((data.Offset <= block.Offset) && (data.End > block.Offset))
                            bytesAvailable = Math.Min(block.Length, (int)(data.End - block.Offset));

                        // if we have some data, or we are beyond any possibility of a match then exit
                        if (bytesAvailable > 0 || data.Offset >= block.End)
                            break;
                    }
                }
            }

            return bytesAvailable;
        }

        /// <summary>
        /// TrimByteRangeRequest - reduce the request to eliminate request for existing data
        /// </summary>
        /// <param name="block">requested block</param>
        /// <returns>bytes currently available at the start of the original request block</returns>
        /// <remarks>We currently ignore the case where our request entirely contains an existing data block because
        /// there is no support for non-contiguous byte-range requests.  If such capability is introduced, we might
        /// revisit this logic and split the request into two requests that don't coincide with the existing data.</remarks>
        private int TrimByteRangeRequest(Block block)
        {
            int bytesAvailable = 0;

            // we can be called even if the byteRangeDownloader is not in use
            if (_byteRangesAvailable != null)
            {
                checked
                {
                    // search through sorted list - move to BinarySearch if we predict huge number of entries
                    foreach (Block data in _byteRangesAvailable)
                    {
                        // Exit early when we know we cannot possibly have a match.
                        // We know this when the current data block offset is beyond the end of our request.
                        if (block.End <= data.Offset)
                            break;

                        // check for Head intersection (or complete co-incidence)
                        if ((block.Offset >= data.Offset) &&
                            (data.End > block.Offset))
                        {
                            // completely satisfies?
                            if (block.End <= data.End)
                                bytesAvailable = block.Length;
                            else
                            {
                                bytesAvailable = (int)(data.End - block.Offset);
                                block.Offset = data.End;
                            }
                            block.Length -= bytesAvailable;
                        }

                        // check for Tail intersection (but not request extending beyond data block as this would split the request)
                        if ((block.Offset <= data.Offset) &&
                            (block.End > data.Offset) && (block.End <= data.End))
                        {
                            block.Length = (int)(data.Offset - block.Offset);
                        }

                        if (bytesAvailable > 0)
                            break;
                    }

                    // zero length block is possible if request is a perfect match
                }
            }

            return bytesAvailable;
        }

        /// <summary>
        /// Stream Block
        /// </summary>
        /// <remarks>represents a byte range that has been downloaded and is available</remarks>
        private class Block: IComparable
        {
            internal Block(long offset, int length)
            {
                Debug.Assert(offset >= 0);
                Debug.Assert(length >= 0);
                _offset = offset;
                _length = length;
            }

            // the index of the byte after the last byte in the block - useful for calculations
            internal long End
            {
                get
                {
                    checked
                    {
                        return _offset + _length;
                    }
                }
            }

            internal long Offset
            {
                get
                {
                    return _offset;
                }
                set
                {
                    Debug.Assert(value >= 0);
                    _offset = value;
                }
            }

            internal int Length
            {
                get
                {
                    return _length;
                }
                set
                {
                    Debug.Assert(value >= 0);
                    _length = value;
                }
            }

            // this allows Sort()
            int IComparable.CompareTo(object x)
            {
                // sort by offset
                Block b = (Block)x;

                if (_offset < b._offset)
                    return -1;

                if (_offset > b._offset)
                    return 1;

                // offsets are equal so now the shortest one goes first
                if (_length == b._length)
                    return 0;

                if (_length < b._length)
                    return -1;

                // _length > b._length
                return 1;
            }

            // returns true if these two blocks overlap or are contiguous
            // assumes _offset <= b._offset because they are supposed to be sorted
            internal bool Mergeable(Block b)
            {
                checked
                {
                    if (_offset <= b._offset)
                        return (_offset + _length - b._offset >= 0);
                    else
                        return (b._offset + b._length - _offset >= 0);
                }
            }

            // combine two blocks that overlap or are adjacent
            internal void Merge(Block b)
            {
                checked
                {
                    Debug.Assert(_offset <= b._offset);
                    Debug.Assert(Mergeable(b));
                    _length = (int)(Math.Max(_offset + _length, b._offset + b._length) - _offset);
                }
            }

            private long    _offset;        // zero-based index from start of stream
            private int     _length;        // number of bytes starting at _offset
        };

        // Merge all overlapping and adjacent ranges
        // This function assumes the list of ranges are already sorted
        // Function is destructive (in-place)
        private void MergeByteRanges(ArrayList ranges)
        {
            checked
            {
                // For each byte range
                for (int i = 0; i + 1 < ranges.Count; i++)
                {
                    Block b = (Block)ranges[i];

                    // handle possible multiple-overlap (or adjacency)
                    while (b.Mergeable((Block)ranges[i + 1]))
                    {
                        b.Merge((Block)ranges[i + 1]);
                        ranges.RemoveAt(i + 1);

                        // don't index off the end of the list
                        if (i + 1 >= ranges.Count)
                            break;
                    }
                }
            }
        }
        #endregion

        /// <summary>
        /// ByteRange event was fired
        /// </summary>
        /// <param name="block">current request</param>
        /// <returns>data known available</returns>
        /// <remarks>pre-condition - SyncLock must be acquired</remarks>
        private int HandleByteRangeReadEvent(Block block)
        {
#if DEBUG
            if (System.IO.Packaging.PackWebRequestFactory._traceSwitch.Enabled)
                System.Diagnostics.Trace.TraceInformation("NetStream.GetData() - byteRange data Event signaled");
#endif
            Debug.Assert(block.Length > 0);

            int bytesAvailable = 0;

            checked
            {
                // We want the "full range test" at first because we don't want to tweak our heuristic unless
                // we really underestimated.  If not all data is available, we take a more relaxed result
                // in the "else" clause below.
                if (_highWaterMark > block.Offset)
                    bytesAvailable = (int)Math.Min(block.Length, _highWaterMark - block.Offset);

                // maybe our request can be satisfied without the byte-range?
                if (bytesAvailable == block.Length)
                {
                    // network traffic is flowing better than expected - increase the threshold
                    _additionalRequestThreshold *= 2;
#if DEBUG
                if (System.IO.Packaging.PackWebRequestFactory._traceSwitch.Enabled)
                    System.Diagnostics.Trace.TraceInformation("NetStream.GetData() - byteRange request satisfied by full download - increasing threshold");
#endif
                }
                else
                {
                    // query the byte-range object for the new range
                    if (!_byteRangeDownloader.ErroredOut)
                    {
                        // update our local list from the ByteRangeDownloader
                        GetByteRangeData();

                        // determine if the ByteRangeDownloader provided any of the data we need
                        bytesAvailable = BytesInByteRangeAvailable(block);
                    }
                    else
                    {
                        // prevent future attempts if downloader has had trouble (could be HTTP server that does not support 1.1 protocol)
                        _allowByteRangeRequests = false;
                    }
                }
            }

            return bytesAvailable;
        }

        /// <summary>
        /// FullDownload event was fired
        /// </summary>
        /// <param name="block">current request</param>
        /// <returns>true if ANY data is available</returns>
        /// <remarks>pre-condition - SyncLock must be acquired</remarks>
        private int HandleFullDownloadReadEvent(Block block)
        {
            int dataAvailable = 0;

            if (_fullDownloadComplete)
            {
                TrimBlockToStreamLength(block);
                dataAvailable = block.Length;
            }
            else
            {
                checked
                {
#if DEBUG
                if (System.IO.Packaging.PackWebRequestFactory._traceSwitch.Enabled)
                    System.Diagnostics.Trace.TraceInformation("NetStream.GetData() - Request Data (BeginRead)");
#endif

                    // Continue reading data until
                    // responseStream.EndRead exhausts the stream
                    _responseStream.BeginRead(_readBuf, 0, _readBuf.Length, new AsyncCallback(ReadCallBack), this);

                    // any data is reason to return true
                    if (_highWaterMark > block.Offset)
                        dataAvailable = (int)Math.Min(block.Length, _highWaterMark - block.Offset);
                }
            }

            return dataAvailable;
        }

        /// <summary>
        /// Get data, blocking until at least one byte is available
        /// </summary>
        /// <param name="block">current request</param>
        /// <returns>bytes available</returns>
        /// <remarks>Attempts to obtain the data from the temp file.  Spawns a ByteRange
        /// request if enabled and appropriate.  Returns when any data is available or
        /// the request exceeded the actual stream length and the entire stream is available.</remarks>
        private int GetData(Block block)
        {
            TrimBlockToStreamLength(block);
            if (block.Length == 0)
                return 0;

            int dataAvailable = 0;

            // no point in waiting if all data is available
            while (dataAvailable == 0)
            {
                Debug.Assert(block.Length > 0);

                lock (_syncObject)
                {
                    if (_highWaterMark > block.Offset)
                    {
                        dataAvailable = (int)Math.Min(block.Length, _highWaterMark - block.Offset);
                    }
                    else
                    {
                        // Check for overlap with existing data - do this even if we are currently in a byte-range request
                        dataAvailable = TrimByteRangeRequest(block);

                        // Should we spawn a byte-range request?
                        // All Criteria must be met:
                        // 1. _allowByteRangeRequests - protocol is http and we know the full stream length
                        // 2. !_inAdditionalRequest - there is no outstanding request - we currently only support one at a time
                        // 3. block.Offset > _highWaterMark + _additionalRequestThreshold - heuristic that says it's "worth it" to spawn a separate request
                        // 4. ((_byteRangeDownloader == null) || !_byteRangeDownloader.ErroredOut) - either there is no
                        //    existing ByteRangeDownloader (this is our first byte-range request), or the downloader is non-null and has not Errored out.
                        // 5. The block we were asked to retrieve was not satisfied by existing data
                        if (_allowByteRangeRequests
                            && !_inAdditionalRequest
                            && (_highWaterMark <= Int64.MaxValue - (long) _additionalRequestThreshold) // Ensure that we don't get overflow from the next line
                            && (block.Offset > _highWaterMark + (long) _additionalRequestThreshold)
                            && ((_byteRangeDownloader == null) || !_byteRangeDownloader.ErroredOut) && (block.Length > 0))
                        {
                            MakeByteRangeRequest(block);            // request data
                        }
                    }
                }

                // We were unable to satisfy the request so we must wait for either the main download thread to signal
                // that new data is available, or the byte-range downloader to signal that new data is available.
                if (dataAvailable == 0)
                {
#if DEBUG
                    if (System.IO.Packaging.PackWebRequestFactory._traceSwitch.Enabled)
                        System.Diagnostics.Trace.TraceInformation("NetStream.GetData() - wait start");   // for debugging deadlock
#endif
                    // WaitAny if both events are in use - or just
                    ReadEvent eventFired;
                    if (_allowByteRangeRequests)
                    {
                        // either way, we must wait for data either from the full-file request or any spawned byte-range request
                        int index = WaitHandle.WaitAny(_readEventHandles);
                        if (index > 128)    // handle +128 case - see SDK
                            index -= 128;

                        eventFired = (ReadEvent)index;
                    }
                    else
                    {
                        // no byte-range downloader in use - wait only for the fulldownload event
                        eventFired = ReadEvent.FullDownloadReadEvent;
                        _readEventHandles[(int)eventFired].WaitOne();
                    }
#if DEBUG
                    if (System.IO.Packaging.PackWebRequestFactory._traceSwitch.Enabled)
                        System.Diagnostics.Trace.TraceInformation("NetStream.GetData() - wait end [{0}]", eventFired);
#endif
                    lock (_syncObject)
                    {
                        // if byteRange we need to keep track of what data is now available
                        if (eventFired == ReadEvent.ByteRangeReadEvent)
                        {
                            dataAvailable = HandleByteRangeReadEvent(block);
                        }
                        else    // FullDownloadReadEvent
                        {
                            dataAvailable = HandleFullDownloadReadEvent(block);

                            // break regardless of if we satisfied the request because no more data is forthcoming
                            if (_fullDownloadComplete)
                            {
                                ReleaseFullDownloadResources();
                                break;
                            }
                        }
                    }
                }
            }

#if DEBUG
            if (System.IO.Packaging.PackWebRequestFactory._traceSwitch.Enabled)
                System.Diagnostics.Trace.TraceInformation("NetStream.GetData() satisfied with {0} bytes", dataAvailable);
#endif
            // could exit with dataAvailable == 0 if we didn't know the full stream length coming in and the request
            // was beyond the actual stream length

            return dataAvailable;
        }

        /// <summary>
        /// Restricts the block length so that it does not extend beyond the end of the stream
        /// </summary>
        /// <param name="block">block to inspect and possibly modify</param>
        /// <remarks>has no effect if full stream length unknown</remarks>
        private void TrimBlockToStreamLength(Block block)
        {
            checked
            {
                // trim to be sure we don't say we have more bytes than the stream holds
                if (_fullStreamLength >= 0)
                    block.Length = (int)Math.Min(block.Length, _fullStreamLength - block.Offset);
            }
        }

        /// <summary>
        /// Release resources only needed for fulldownload
        /// </summary>
        private void ReleaseFullDownloadResources()
        {
            Debug.Assert(_fullDownloadComplete, "Do not call this unless full download is complete.");

            // ignore logic errors - only do this once
            if (_readBuf != null)
            {
                // don't need these anymore
                _byteRangesAvailable = null;
                _readBuf = null;

                try
                {
                    try
                    {
                        FreeByteRangeDownloader();

                        // release the full download read event as it is no longer needed
                        if (_readEventHandles[(int)ReadEvent.FullDownloadReadEvent] != null)
                        {
                            _readEventHandles[(int)ReadEvent.FullDownloadReadEvent].Close();
                            _readEventHandles[(int)ReadEvent.FullDownloadReadEvent] = null;
                        }
                    }
                    finally
                    {
                        // FreeFullDownload
                        if (_responseStream != null)
                        {
                            _responseStream.Close();
                        }
                    }
                }
                finally
                {
                    _responseStream = null;
                }
            }
        }

        /// <summary>
        /// Free ByteRangeDownloader if it is allocated
        /// </summary>
        private void FreeByteRangeDownloader()
        {
            if (_byteRangeDownloader != null)
            {
                try
                {
                    ((IDisposable)_byteRangeDownloader).Dispose();

                    if (_readEventHandles[(int)ReadEvent.ByteRangeReadEvent] != null)
                    {
                        _readEventHandles[(int)ReadEvent.ByteRangeReadEvent].Close();
                        _readEventHandles[(int)ReadEvent.ByteRangeReadEvent] = null;
                    }
                }
                finally
                {
                    _byteRangeDownloader = null;
                }
            }
        }

        /// <summary>
        /// FreeTempFile - frees resources related to the tempfile
        /// </summary>
        private void FreeTempFile()
        {
            // Stream and Mutex
            bool mutexObtained = false;
            Invariant.Assert(_tempFileStream != null);

            try
            {
                mutexObtained = _tempFileMutex.WaitOne(_tempFileSyncTimeout, false);    // wait up to 5 seconds
                lock (PackagingUtilities.IsolatedStorageFileLock)
                {
                    _tempFileStream.Close();
                }
            }
            finally
            {
                // only release it if we own it
                if (mutexObtained)
                {
                    // make sure this is released even if there is a stream error
                    _tempFileMutex.ReleaseMutex();

                    // only close this if we obtained it
                    // let the garbage collector get it eventually if we didn't
                    _tempFileMutex.Close(); // does not throw an exception
                }

                _tempFileStream = null;
                _tempFileName = null;
                _tempFileMutex = null;
            }
        }


        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        private enum ReadEvent { FullDownloadReadEvent = 0, ByteRangeReadEvent = 1, MaxReadEventEnum };

        Uri                     _uri;               // uri we are resolving

        WebRequest              _originalRequest;   // Proxy member is Critical
        Stream                  _tempFileStream;    // local temp stream we are writing to and reading from - protected by _tempFileMutex
        long                    _position;          // our "logical stream position"

        // syncObject - provides mutually-exclusive access control to the following entities:
        // 1. _highWaterMark - this is actually queried outside of a lock in get_Length, but this is safe as a stale value only impacts perf
        // Does not fully protect the following entities:
        // 1. _disposed - Not in all cases - CheckDisposed(), CanRead, CanSeek do not lock first but we are not "threadsafe" and _disposed is only
        //                modified in the Dispose() call.  The only other thread that can inspect it does so in ReadCallBack - both places
        //                we lock on _syncObject so this is safe.
        // 2. _responseStream - yes
        // 3. _readEventHandles - cannot be as these are synchronization objects which must be freely accessible
        // 4. _byteRangeDownloader - yes except this object can be disposed while it is still "active" - it is expected to behave correctly in this
        //                scenario.
        private Object          _syncObject = new Object();
        private volatile bool   _disposed;

        // full-file download
        private const int       _readTimeOut = 40000;   // how long before we give-up on async Read? (milliseconds)
        private const int       _additionalRequestMinSize = 0x1000;     // minimum size for a ByteRangeRequest - make it worth the trouble (overhead)
        private const int       _bufferSize = 0x1000;                   // smaller allows for quicker response
        private const int       _tempFileSyncTimeout = 5000;            // wait 5 seconds for byteRangeDownloader to release it's File mutex before closing it
        private uint            _additionalRequestThreshold = 0x4000;   // dynamically adjusting this value based on network conditions (start small because this only goes up)
        private Stream          _responseStream;        // Stream returned by WebResponse
        private byte[]          _readBuf;               // destination buffer for async inner webResponse reads
        private string          _tempFileName;          // file name of temp file
        private long            _fullStreamLength;      // need to return this in call to get_Length
        private volatile bool   _fullDownloadComplete;  // download complete if this is true - prevents us from waiting for more data
                                                        // this is volatile because it can be updated and inspected by different threads
        private long            _highWaterMark;         // how much data is currently available from full download
                                                        // used to determine whether it makes sense to spawn a byte-range download
                                                        // access to this value must be synchronized using lock()

        // OS synchronization event used to signal that new data is available
        private EventWaitHandle[]   _readEventHandles = new EventWaitHandle[(int)ReadEvent.MaxReadEventEnum];

        // protects the _tempFileStream object and allows both our thread and the ByteRangeDownloader thread to safely access the temp stream
        private Mutex               _tempFileMutex = new Mutex(false);

        // byte-range downloads
        private bool                _allowByteRangeRequests;        // toggle
        private ByteRangeDownloader _byteRangeDownloader;           // handles byte-range downloads for us
        private bool                _inAdditionalRequest;           // only spawn one byte-range request at a time
        private ArrayList           _byteRangesAvailable;           // byte ranges that are downloaded
#if DEBUG
        private int                 _unmergedBlocks;                // for trace only
#endif
    }
}
