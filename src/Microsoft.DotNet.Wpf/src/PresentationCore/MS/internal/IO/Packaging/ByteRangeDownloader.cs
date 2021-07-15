// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if DEBUG
#define TRACE
#endif
//
//
// Description:
// Downloader for downloading resources from the Internet in byte ranges using .NET
//  web requests. This class is used by ByteWrapper (unmanaged code) to make additional
//  web requests other than through WININET

using System;
using System.Collections;
using System.ComponentModel;              // For Win32Exception
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;        // For IsolatedStorage temp file
using System.Net;
using System.Net.Cache;                     // For RequestCachePolicy
using System.Runtime.InteropServices;   // For Marshal
using System.Security;                  // SecurityCritical, SecurityTreatAsSafe
using System.Threading;                  // For Mutex
using Microsoft.Win32.SafeHandles;
using MS.Internal.PresentationCore;

namespace MS.Internal.IO.Packaging
{
    /// <summary>
    /// Downloader for byte range requests
    /// </summary>
    /// <remarks>
    /// For now, we will only process one batch of requests at a time. We will most likely
    ///  want to spin multiple threads and do multiple batches at a time in the future
    /// </remarks>
    [FriendAccessAllowed]
    internal class ByteRangeDownloader : IDisposable
    {
         //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor for ByteRangeDownloader - for UrlMon usage
        /// </summary>
        /// <param name="eventHandle">event to signal when new data is available in tempStream</param>
        /// <param name="requestedUri">uri we should make requests to</param>
        /// <param name="tempFileName">temp file to write to</param>
        internal ByteRangeDownloader(Uri requestedUri, string tempFileName, SafeWaitHandle eventHandle)
            : this(requestedUri, eventHandle)
        {
            if (tempFileName == null)
            {
                throw new ArgumentNullException("tempFileName");
            }

            if (tempFileName.Length <= 0)
            {
                throw new ArgumentException(SR.Get(SRID.InvalidTempFileName), "tempFileName");
            }

            _tempFileStream = File.Open(tempFileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        }

        /// <summary>
        /// Constructor for ByteRangeDownloader - used by NetStream who owns the tempStream
        /// </summary>
        /// <param name="eventHandle">event to signal when new data is available in tempStream</param>
        /// <param name="fileMutex">mutex to synchronize access to tempStream</param>
        /// <param name="requestedUri">uri we should make requests to</param>
        /// <param name="tempStream">stream to write data to</param>
        internal ByteRangeDownloader(Uri requestedUri, Stream tempStream, SafeWaitHandle eventHandle, Mutex fileMutex)
            : this(requestedUri, eventHandle)
        {
            Debug.Assert(fileMutex != null, "FileMutex must be a valid mutex");
            Debug.Assert(tempStream != null, "ByteRangeDownloader requires a stream to write to");
            _tempFileStream = tempStream;
            _fileMutex = fileMutex;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Interfaces
        //
        //------------------------------------------------------

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);  // not strictly necessary, but if we ever have a subclass with a finalizer, this will be more efficient
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_syncObject)
                {
                    if (!_disposed)
                    {
                        try
                        {
                            // if there is no mutex, then we own the stream and we should close it
                            if (FileMutex == null && _tempFileStream != null)
                            {
                                _tempFileStream.Close();
                            }
                        }
                        finally
                        {
                            _requestedUri = null;
                            _byteRangesInProgress = null;
                            _requestsOnWait = null;
                            _byteRangesAvailable = null;
                            _tempFileStream = null;
                            _eventHandle = null;
                            _proxy = null;
                            _credentials = null;
                            _cachePolicy = null;
                            _disposed = true;
                        }
                    }
                }
            }
        }

        #endregion IDisposable

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Get the byte ranges that are downloaded
        /// </summary>
        /// <returns>byte ranges that are downloaded; byteRanges is one dimensional
        /// array consisting pairs of offset and length</returns>
        internal int[,] GetDownloadedByteRanges()
        {
            int[,] byteRanges = null;

            // The worker thread will never call dispose nor this method; no need lock
            CheckDisposed();

            lock (_syncObject)
            {
                CheckErroredOutCondition();

                int rangeCount = _byteRangesAvailable.Count / 2;

                // The worker thread will update the bytes downloaded; need to lock
                byteRanges = new int[rangeCount, 2];
                for (int i = 0; i < rangeCount; ++i)
                {
                    byteRanges[i, Offset_Index] = (int)_byteRangesAvailable[(i * 2) + Offset_Index];
                    byteRanges[i, Length_Index] = (int)_byteRangesAvailable[(i * 2) + Length_Index];
                }
                _byteRangesAvailable.Clear();
            }

            return byteRanges;
        }

        /// <summary>
        /// Make byte range request
        /// </summary>
        /// <param name="byteRanges">byte ranges to be downloaded; byteRanges is two dimensional
        /// array consisting pairs of offset and length</param>
        internal void RequestByteRanges(int[,] byteRanges)
        {
            // The worker thread will never call dispose nor this method; no need to lock
            CheckDisposed();

            if (byteRanges == null)
            {
                throw new ArgumentNullException("byteRanges");
            }

            CheckTwoDimensionalByteRanges(byteRanges);

            // When multiple byte ranges are requested through one HttpWebRequest, the current HttpWebResponse
            // returns a stream with headers which has to be manually parsed. At this point, we decided to
            // make only one byte range request at a time

            // At this point, none of the callers of this class will make more than one range
            //  So, we will assert if more than one byte range request is made
            Debug.Assert(byteRanges.GetLength(0) == 1, "We don't support a request with multiple byte ranges");

            _firstRequestMade = true;

            // If there is no request in progress, start the request process; otherwise put it in the wait queue
            lock (_syncObject)
            {
                CheckErroredOutCondition();

                // _byteRangeInprogress can be cleared out from the worker thread and this method can be called
                //  from the caller thread; need to lock
                if (_byteRangesInProgress == null)
                {
                    _webRequest = CreateHttpWebRequest(byteRanges);
                    _byteRangesInProgress = byteRanges;
                    _webRequest.BeginGetResponse(ResponseCallback, this);
                }
                else    // cannot make request yet, put the request in the wait queue
                {
                    // Lazy Init
                    if (_requestsOnWait == null)
                    {
                        // there are currently only ever two of these (one pair)
                        // so optimize
                        _requestsOnWait = new ArrayList(2);
                    }

                    for (int i = 0; i < byteRanges.GetLength(0); ++i)
                    {
                        // Add requests to the wait queue
                        _requestsOnWait.Add((int) byteRanges[i, Offset_Index]);
                        _requestsOnWait.Add((int) byteRanges[i, Length_Index]);
                    }
                }
            }
        }

        /// <summary>
        /// Convert bytes ranges from one dimensional array (pairs of offset and length)
        /// to two dimensional array
        /// </summary>
        /// <param name="inByteRanges">byteRanges in one dimensional array consisting pairs of offset and length</param>
        /// <returns>byteRanges in two dimensional array consisting pairs of offset and length</returns>
        static internal int[,] ConvertByteRanges(int[] inByteRanges)
        {
            CheckOneDimensionalByteRanges(inByteRanges);

            int[,] outByteRanges = new int[(inByteRanges.Length / 2),2];

            for (int i=0, j=0; i < inByteRanges.Length; ++i, ++j)
            {
                outByteRanges[j,Offset_Index] = inByteRanges[i];
                outByteRanges[j,Length_Index] = inByteRanges[i+1];

                ++i;
            }

            return outByteRanges;
        }

        /// <summary>
        /// Convert bytes ranges from two dimensional array (paris of offiset and length)
        /// to one dimensional array
        /// </summary>
        /// <param name="inByteRanges">byteRanges in two dimensional array consisting pairs of offset and length</param>
        /// <returns>byteRanges in one dimensional array consisting pairs of offset and length</returns>
        static internal int[] ConvertByteRanges(int[,] inByteRanges)
        {
            // Normallly we will check the input from the caller
            //  but in our scenario, the two dimensional array is always generated by ByteRangeDownloader
            //  No need to check
#if DEBUG
            CheckTwoDimensionalByteRanges(inByteRanges);
#endif

            int[] outByteRanges = new int[inByteRanges.Length];

            for (int i=0, j=0; i < inByteRanges.GetLength(0); ++i, ++j)
            {
                outByteRanges[j] = inByteRanges[i, Offset_Index];
                outByteRanges[++j] = inByteRanges[i, Length_Index];
            }

            return outByteRanges;
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Proxy for all requests to ByteRangeDownloader
        /// </summary>
        internal IWebProxy Proxy
        {
            set
            {
                CheckDisposed();
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                if (!_firstRequestMade)
                {
                    _proxy = value;
                }
                else    // Once first request is made it cannot change
                {
                    throw new InvalidOperationException(SR.Get(SRID.RequestAlreadyStarted));
                }
            }
        }

        /// <summary>
        /// Authentication information for all requests to ByteRangeDownloader
        /// </summary>
        internal ICredentials Credentials
        {
            set
            {
                CheckDisposed();
                _credentials = value;
            }
        }

        /// <summary>
        /// Cache Policy for all requests to ByteRangeDownloader
        /// </summary>
        internal RequestCachePolicy CachePolicy
        {
            set
            {
                CheckDisposed();
                if (!_firstRequestMade)
                {
                    _cachePolicy = value;
                }
                else    // Once first request is made it cannot cahnge
                {
                    throw new InvalidOperationException(SR.Get(SRID.RequestAlreadyStarted));
                }
            }
        }

        /// <summary>
        /// OS synchronization object use to synchronize access to the temp file - if null, no sync is needed
        /// </summary>
        internal Mutex FileMutex
        {
            get
            {
                CheckDisposed();
                return _fileMutex;
            }
        }

        /// <summary>
        /// Returns true if any request errored out
        /// </summary>
        internal bool ErroredOut
        {
            get
            {
                CheckDisposed();

                lock (_syncObject)
                {
                    return _erroredOut;
                }
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Classes
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Check if the object is disposed
        /// </summary>
        /// <remarks>this._disposed is not changed by a thread while another thread is calling this function</remarks>
        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(null, SR.Get(SRID.ByteRangeDownloaderDisposed));
            }
        }

        /// <summary>
        /// Constructor for ByteRangeDownloader
        /// </summary>
        private ByteRangeDownloader(Uri requestedUri, SafeWaitHandle eventHandle)
        {
            if (requestedUri == null)
            {
                throw new ArgumentNullException("requestedUri");
            }

            // Ensure uri is correct scheme (http or https) Do case-sensitive comparison since Uri.Scheme contract is to return in lower case only.
            if (String.Compare(requestedUri.Scheme, Uri.UriSchemeHttp, StringComparison.Ordinal) != 0
                    && String.Compare(requestedUri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal) != 0)
            {
                throw new ArgumentException(SR.Get(SRID.InvalidScheme), "requestedUri");
            }

            if (eventHandle == null)
            {
                throw new ArgumentNullException("eventHandle");
            }

            if (eventHandle.IsInvalid || eventHandle.IsClosed)
            {
                throw new ArgumentException(SR.Get(SRID.InvalidEventHandle), "eventHandle");
            }

            _requestedUri = requestedUri;
            _eventHandle = eventHandle;
        }

        /// <summary>
        /// Check if it has been errored out from the worker thread and re-throw the exception that was
        /// thrown from the worker thread
        /// </summary>
        /// <remarks>No need to lock in this function since the caller always locks before making this call</remarks>
        private void CheckErroredOutCondition()
        {
            if (_erroredOut)
            {
                throw new InvalidOperationException(SR.Get(SRID.ByteRangeDownloaderErroredOut), _erroredOutException);
            }
        }

        /// <summary>
        /// Download the requested bytes
        /// </summary>
        private HttpWebRequest CreateHttpWebRequest(int[,] byteRanges)
        {
            HttpWebRequest request;

            // Create the request object
            request = (HttpWebRequest)WpfWebRequestHelper.CreateRequest(_requestedUri);
            request.ProtocolVersion = HttpVersion.Version11;
            request.Method = "GET";

            // Set the Proxy to Empty one; If we don't set this to empty one, it will try to find one for us
            //  and ends up triggering JScript in another assembly. This will throw PolicyException since the JScript
            //  dll doesn't have execution right. This is a bug in CLR; supposed to be fixed later
            // Future work: Need to keep consistent HTTP stack with the WININET one (e.g. authentication, proxy, cookies)
            //            IWebProxy emptyProxy = GlobalProxySelection.GetEmptyWebProxy();
            //            request.Proxy = emptyProxy;

            request.Proxy = _proxy;

            request.Credentials = _credentials;
            request.CachePolicy = _cachePolicy;

            // Add byte ranges (to header)
            for (int i = 0; i < byteRanges.GetLength(0); ++i)
            {
                request.AddRange(byteRanges[i,Offset_Index],
                                 byteRanges[i,Offset_Index] + byteRanges[i,Length_Index] - 1);
            }

            return request;
        }

        /// <summary>
        /// Raise Win32 events informing a client that the requested bytes are available or it errored out
        /// </summary>
        /// <param name="throwExceptionOnError">indicates if an exception to be thrown on fail to set event</param>
        /// <remarks></remarks>
        private void RaiseEvent(bool throwExceptionOnError)
        {
            if (_eventHandle != null && !_eventHandle.IsInvalid && !_eventHandle.IsClosed)
            {
                if (MS.Win32.UnsafeNativeMethods.SetEvent(_eventHandle) == 0)
                {
                    if (throwExceptionOnError)
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }
                }
            }
        }

        /// <summary>
        /// ResponseCallBack
        /// </summary>
        /// <param name="ar">async result</param>
        /// <remarks>static method not necessary</remarks>
        private void ResponseCallback(IAsyncResult ar)
        {
            HttpWebResponse webResponse = null;

            lock (_syncObject)
            {
                try
                {
                    if (_disposed)
                    {
                        return;
                    }

                    // The caller thread can dispose this class and the worker thread need to check the disposed
                    //  condition; need to lock
                    // If disposed, there is nothing to handle
                    webResponse = (HttpWebResponse)WpfWebRequestHelper.EndGetResponse(_webRequest, ar);

                    // If it is not partial content, no need to look further
                    if (webResponse.StatusCode == HttpStatusCode.PartialContent)
                    {
                        //
                        // Check for few conditions
                        //

                        // Get the header and make sure that it was indeed the byte range response
                        int beginOffset = _byteRangesInProgress[0, Offset_Index];
                        int endOffset = beginOffset+ _byteRangesInProgress[0,Length_Index] - 1;

                        // HttpWebRequest in the current CLR does not allow multiple byte range requests.
                        // At this point, none of the callers of this class will make more than one range at a time
                        //  So, we should not receive any response with more than one range returned

                        // When multiple byte ranges requests are support in HttpWebRequest eventually,
                        // there is a question on how to handle multipart response (Content-Type=multipart/byteranges)

                        // At this point we only need to handle one byte range response (Content-Range header) only
                        // Request was successful
                        // Note: endOffset could be trimmed offset in the case where the response didn't
                        //  satisfy the entire request
                        if (CheckContentRange(webResponse.Headers, beginOffset, ref endOffset))
                        {
                            // Write out the bytes to the temp file
                            if (WriteByteRange(webResponse, beginOffset, endOffset - beginOffset + 1))
                            {
                                // The range is downloaded successfully; add it to the list
                                _byteRangesAvailable.Add(beginOffset);
                                _byteRangesAvailable.Add(endOffset - beginOffset + 1);
                            }
                            else
                                _erroredOut = true;
                        }
                        else
                        {
                            _erroredOut = true;
                            _erroredOutException = new NotSupportedException(SR.Get(SRID.ByteRangeRequestIsNotSupported));
                        }
                    }
                    else
                    {
                        _erroredOut = true;
                    }
                }
                catch (Exception e)  // catch (and re-throw) exceptions so we can inform the other thread
                {
                    _erroredOut = true;
                    _erroredOutException = e;

                    throw;
                }
                catch   // catch (and re-throw) all kinds of exceptions so we can inform the other thread
                {
                    // inform other thread of error condition
                    _erroredOut= true;
                    _erroredOutException = null;

                    throw;
                }
                finally
                {
                    if (webResponse != null)
                    {
                        webResponse.Close();
                    }

                    // bytes requested are downloaded or errored out
                    //  inform the caller that these ranges are available
                    RaiseEvent(!_erroredOut);
                }

                // If we haven't errored out already, process the next batch
                if (!_erroredOut)
                {
                    ProcessWaitQueue();
                }
           }
        }

        /// <summary>
        /// Shared code for mutex and non-mutex to call
        /// </summary>
        private bool Write(Stream s, int offset, int length)
        {
            int readBytes;

            // Process the data chunk at a time (Size of the data that can be processed one time is WriteBufferSize)
            _tempFileStream.Seek(offset, SeekOrigin.Begin);
            while (length > 0)
            {
                // Read in the data into the buffer
                readBytes = s.Read(_buffer, 0, WriteBufferSize);
                if (readBytes == 0)
                    break;

                // Write it out
                _tempFileStream.Write(_buffer, 0, readBytes);
                length -= readBytes;
            }

            if (length != 0)
                return false;

            _tempFileStream.Flush();
            return true;
        }

        /// <summary>
        /// Write out the downloaded byte ranges to the temp file
        /// </summary>
        /// <param name="response">Http web response</param>
        /// <param name="offset">Offset of the byte range</param>
        /// <param name="length">Length of the byte range</param>
        /// <returns>True if it is successfully written out</returns>
        private bool WriteByteRange(HttpWebResponse response, int offset, int length)
        {
            bool result = false;

            // Get the downloaded stream
            using (Stream s = response.GetResponseStream())
            {
                if (_buffer == null)
                {
                    _buffer = new byte[WriteBufferSize];
                }

                // mutex available?
                if (_fileMutex != null)
                {
                    // use it
                    try
                    {
                        // block until temp file is available
                        _fileMutex.WaitOne();
                        lock (PackagingUtilities.IsolatedStorageFileLock)
                        {
                            result = Write(s, offset, length);
                        }
                    }
                    finally
                    {
                        _fileMutex.ReleaseMutex();
                    }
                }
                else
                    result = Write(s, offset, length);
            }

            return result;
        }

        /// <summary>
        /// Process the requests that are in the wait queue
        /// </summary>
        /// <remarks>This is only called from ResponseCallback which synchronize the call</remarks>
        private void ProcessWaitQueue()
        {
            // There is other requests waiting in the queue; Process those
            if (_requestsOnWait != null && _requestsOnWait.Count > 0)
            {
                // _byteRangesInProgress is already allocated and can be reused
                _byteRangesInProgress[0,Offset_Index] = (int) _requestsOnWait[Offset_Index];
                _byteRangesInProgress[0,Length_Index] = (int) _requestsOnWait[Length_Index];
                _requestsOnWait.RemoveRange(0, 2);

                _webRequest = CreateHttpWebRequest(_byteRangesInProgress);
                _webRequest.BeginGetResponse(ResponseCallback, this);
            }
            else
            {
                // If there is nothing more to process in the wait queue, clear out
                //  _byteRangesInProgress so that subsequent byte range requests can be made
                _byteRangesInProgress = null;
            }
        }

        /// <summary>
        /// Check if the given byte ranges follows correct format
        /// 1) number of items in the array should be even number (It should be one dimensional array consisting pairs
        ///     of offset and length
        /// 2) offset cannot be less than 0
        /// 3) length cannot be less than equal to 0
        /// </summary>
        /// <param name="byteRanges">Byte ranges to be checked</param>
        /// <returns>True if the byte ranges are in correct format</returns>
        static private void CheckOneDimensionalByteRanges(int[] byteRanges)
        {
            // The byteRanges should never be less; perf optimization
            if (byteRanges.Length < 2 || (byteRanges.Length % 2) != 0)
            {
                throw new ArgumentException(SR.Get(SRID.InvalidByteRanges, "byteRanges"));
            }

            for (int i = 0; i < byteRanges.Length; i++)
            {
                if (byteRanges[i] < 0 || byteRanges[i+1] <= 0)
                {
                    throw new ArgumentException(SR.Get(SRID.InvalidByteRanges, "byteRanges"));
                }
                i++;
            }
        }

        /// <summary>
        /// Check if the given byte ranges follows correct format
        /// 1) array should consist pairs of offset and length
        /// 2) offset cannot be less than 0
        /// 3) length cannot be less than equal to 0
        /// </summary>
        /// <param name="byteRanges">Byte ranges to be checked</param>
        /// <returns>True if the byte ranges are in correct format</returns>
        static private void CheckTwoDimensionalByteRanges(int[,] byteRanges)
        {
            if (byteRanges.GetLength(0) <= 0 || byteRanges.GetLength(1) != 2)
            {
                throw new ArgumentException(SR.Get(SRID.InvalidByteRanges, "byteRanges"));
            }

            for (int i = 0; i < byteRanges.GetLength(0); ++i)
            {
                if (byteRanges[i,Offset_Index] < 0 || byteRanges[i,Length_Index] <= 0)
                {
                    throw new ArgumentException(SR.Get(SRID.InvalidByteRanges, "byteRanges"));
                }
            }
        }

        /// <summary>
        /// Check if the some of byte range request is satisfied by the given response
        /// This method make sure of the following:
        /// 1) It contains Content-Range field
        /// 2) Content-Range follows the format defined in RFC2616
        ///     a. it should be in the form of "bytes 0-499/1234" or "bytes 0-499/*"
        ///     b. it should not be "bytes */*" or "*/1234"
        ///     c. last-byte-pos must not be less than its first byte pos
        /// 3) Ignore the response that does not conform to condition #1 and #2
        /// </summary>
        /// <param name="responseHeaders">collection of headers returned with WebResponse</param>
        /// <param name="beginOffset">first byte pos the requested byte range</param>
        /// <param name="endOffset">last byte pos the requested byte range</param>
        /// <returns>False if the response contains invalid Content-Range field
        ///         or none of bytes requested is included in the response.
        ///         True if the some bytes of the requested bytes are included in the response.</returns>
        static private bool CheckContentRange(WebHeaderCollection responseHeaders, int beginOffset, ref int endOffset)
        {
            String contentRange = responseHeaders[ContentRangeHeader];

            // No Content-Range (condition #1)
            if (contentRange == null)
            {
                return false;
            }

            contentRange = contentRange.ToUpperInvariant();

            // No Content-Range (condition #1)
            if (contentRange.Length == 0
                    || !contentRange.StartsWith(ByteRangeUnit, StringComparison.Ordinal))
            {
                return false;
            }

            // ContentRange: BYTES XXX-YYY/ZZZ
            int index = contentRange.IndexOf('-');

            if (index == -1)
            {
                return false;
            }

            // Get the first byte offset of the range (XXX)
            int firstByteOffset = Int32.Parse(contentRange.Substring(ByteRangeUnit.Length,
                                                                        index - ByteRangeUnit.Length),
                                                NumberStyles.None, NumberFormatInfo.InvariantInfo);

            contentRange = contentRange.Substring(index + 1);
            // ContentRange: YYY/ZZZ
            index = contentRange.IndexOf('/');

            if (index == -1)
            {
                return false;
            }

            // Get the last byte offset of the range (YYY)
            int lastByteOffset = Int32.Parse(contentRange.Substring(0, index), NumberStyles.None, NumberFormatInfo.InvariantInfo);

            // Get the instance length
            // ContentRange: ZZZ
            contentRange = contentRange.Substring(index + 1);
            if (String.CompareOrdinal(contentRange, "*") != 0)
            {
                // Note: for firstByteOffset and lastByteOffset, we are using Int32.Parse to make sure Int32.Parse to throw
                //  if it is not an integer or the integer is bigger than Int32 since HttpWebRequest.AddRange
                //  only supports Int32
                //  Once HttpWebRequest.AddRange start supporting Int64 we should change it to Int64 and long
                Int32.Parse(contentRange, NumberStyles.None, NumberFormatInfo.InvariantInfo);
            }

            // The response is considered to be successful if
            //  the last byte offset is greater than the first offset of the response
            //  and the response range satisfies some or all of the requested range
            //  However, we don't want to deal with the situation where the first byte of the response is not the beginning
            //  of the requested range
            bool successful = (firstByteOffset <= lastByteOffset
                                    && beginOffset == firstByteOffset);

            // Check if the response didn't satisfy the end part of the requested range
            if (successful && lastByteOffset < endOffset)
            {
                endOffset = lastByteOffset;
            }

            return successful;
        }

        #endregion Private Methods

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

        #region Private Fields

        private bool _firstRequestMade;
        private bool _disposed;
        private Object _syncObject = new Object();
        private bool _erroredOut;
        private Exception _erroredOutException;

        private Uri _requestedUri;            // url to be downloaded
        private RequestCachePolicy _cachePolicy;

        private IWebProxy _proxy;
        private ICredentials _credentials;
        private CookieContainer _cookieContainer = new CookieContainer(1);

        private SafeWaitHandle _eventHandle;    // event handle which needs to be raised to inform the caller that
                                         //  the requested bytes are available
        private Mutex _fileMutex;       // object controlling synchronization on the temp file - if this is null, we own the stream
        private System.IO.Stream _tempFileStream;   // stream to write to

        private ArrayList _byteRangesAvailable = new ArrayList(2); // byte ranges that are downloaded
        private ArrayList _requestsOnWait;      // List of byte ranges requested need to be processed
        private int[,] _byteRangesInProgress;

        private HttpWebRequest _webRequest;

        private byte[] _buffer;             // Buffer used for writing out the downloaded bytes to temp file

        private const int WriteBufferSize = 4096; // Buffer size for writing out to the temp file
        private const int TimeOut = 5000;
        private const int Offset_Index = 0;
        private const int Length_Index = 1;

        private const String ByteRangeUnit = "BYTES ";
        private const String ContentRangeHeader = "Content-Range";

        #endregion Private Fields
    }
}
