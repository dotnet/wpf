// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  WebResponse class to handle pack-specific URI's
//
//

#if DEBUG
#define TRACE
#endif

using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Diagnostics;               // For Assert
using System.Threading;                 // for ManualResetEvent
using System.Globalization;             // for CultureInfo
using MS.Internal.PresentationCore;     // for ExceptionStringTable
using MS.Internal.IO.Packaging;              // for ResponseStream
using System.Security;
using System.Windows.Navigation;
using MS.Utility;
using MS.Internal;

#pragma warning disable 1634, 1691      // disable warning about unknown Presharp warnings

namespace System.IO.Packaging
{
    /// <summary>
    /// Pack-specific WebRequest handler
    /// </summary>
    /// <remarks>
    /// This WebRequest overload exists to handle Pack-specific URI's based on our custom schema
    /// </remarks>
    public sealed class PackWebResponse: WebResponse
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static PackWebResponse()
        {
#if DEBUG
            _forceWebResponseLengthFailureSwitch = new BooleanSwitch("PackWebResponseBadServerLength", "Simulate PackWebResponse handling of server that returns bogus content length");
#endif
        }

        /// <summary>
        /// Constructor
        /// </summary>
        ///  <param name="innerRequest">real web request</param>
        /// <param name="uri">full uri</param>
        /// <param name="innerUri">inner uri</param>
        /// <param name="partName">part name in the container - null if uri is to entire container only</param>
        /// <remarks>intended for use only by PackWebRequest</remarks>
        internal PackWebResponse(Uri uri, Uri innerUri, Uri partName, WebRequest innerRequest)
        {
            if (uri == null)
                throw new ArgumentNullException("uri");

            if (innerUri == null)
                throw new ArgumentNullException("innerUri");

            if (innerRequest == null)
                throw new ArgumentNullException("innerRequest");

            _lockObject = new Object();     // required for synchronization

            _uri = uri;
#if DEBUG
            if (PackWebRequestFactory._traceSwitch.Enabled)
                System.Diagnostics.Trace.TraceInformation(
                        DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                        System.Threading.Thread.CurrentThread.ManagedThreadId + ": " +
                        "PackWebResponse - Creating response ");
#endif
            _innerUri = innerUri;
            _partName = partName;           // may be null

            _webRequest = innerRequest;
            _mimeType = null;               // until we find out the real value

            // only create these in non-cache case
            // Create before any Timeout timer to prevent a race condition with the Timer callback
            // (that expects _responseAvailable to exist)
            _responseAvailable = new ManualResetEvent(false);

            // do we need a timer?
            // if the TimeOut has been set on the innerRequest, we need to simulate this behavior for
            // our synchronous clients
            if (innerRequest.Timeout != Timeout.Infinite)
            {
#if DEBUG
                if (PackWebRequestFactory._traceSwitch.Enabled)
                    System.Diagnostics.Trace.TraceInformation(
                        DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                        System.Threading.Thread.CurrentThread.ManagedThreadId + ": " +
                        "PackWebResponse() starting timeout timer " + innerRequest.Timeout + " ms");
#endif
                _timeoutTimer = new Timer(new TimerCallback(TimeoutCallback), null, innerRequest.Timeout, Timeout.Infinite);
            }

#if DEBUG
            if (PackWebRequestFactory._traceSwitch.Enabled)
                System.Diagnostics.Trace.TraceInformation(
                        DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                        System.Threading.Thread.CurrentThread.ManagedThreadId + ": " +
                        "PackWebResponse() BeginGetResponse()");
#endif

            // Issue the async request to get our "real" WebResponse
            // don't access this value until we set the ManualResetEvent
            _webRequest.BeginGetResponse(new AsyncCallback(ResponseCallback), this);
        }

        /// <summary>
        /// Constructor: Cache-entry overload
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="innerUri"></param>
        /// <param name="partName"></param>
        /// <param name="cacheEntry">entry from cache</param>
        /// <param name="cachedPackageIsThreadSafe">is entry thread safe?</param>
        internal PackWebResponse(Uri uri, Uri innerUri, Uri partName, Package cacheEntry,
            bool cachedPackageIsThreadSafe)
        {
            _lockObject = new Object();     // required for synchronization

            if (uri == null)
                throw new ArgumentNullException("uri");

            if (innerUri == null)
                throw new ArgumentNullException("innerUri");

            if (partName == null)
                throw new ArgumentNullException("partName");

            if (cacheEntry == null)
                throw new ArgumentNullException("cacheEntry");

#if DEBUG
            if (PackWebRequestFactory._traceSwitch.Enabled)
                System.Diagnostics.Trace.TraceInformation(
                        DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                        System.Threading.Thread.CurrentThread.ManagedThreadId + ": " +
                        "PackWebResponse - Creating response from Package Cache");
#endif
            _uri = uri;
            _innerUri = innerUri;
            _partName = partName;           // may not be null

            _mimeType = null;               // until we find out the real value

            // delegate work to private class
            _cachedResponse = new CachedResponse(this, cacheEntry, cachedPackageIsThreadSafe);
        }


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region WebResponse Overloads
        /// <summary>
        /// Retrieves a stream for reading bytes from the requested resource
        /// </summary>
        /// <returns>stream</returns>
        public override Stream GetResponseStream()
        {
            CheckDisposed();

            // redirect
            if (FromPackageCache)
                return _cachedResponse.GetResponseStream();

            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXPS, EventTrace.Event.WClientDRXGetStreamBegin);

#if DEBUG
            if (PackWebRequestFactory._traceSwitch.Enabled)
                System.Diagnostics.Trace.TraceInformation(
                        DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                        System.Threading.Thread.CurrentThread.ManagedThreadId + ": " +
                        "PackWebResponse - GetResponseStream()");
#endif
            // create and return only a single stream for multiple calls
            if (_responseStream == null)
            {
                // can't do this until the response is available
                WaitForResponse();  // after this call, we have a viable _fullResponse object because WaitForResponse would have thrown otherwise

                // determine content length
                long streamLength = _fullResponse.ContentLength;

#if DEBUG
                if (_forceWebResponseLengthFailureSwitch.Enabled)
                    streamLength = -1;

                // special handling for servers that won't or can't give us the length of the resource - byte-range downloading is impossible
                if (streamLength <= 0)
                {
                    if (PackWebRequestFactory._traceSwitch.Enabled)
                        System.Diagnostics.Trace.TraceInformation(
                                DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                                System.Threading.Thread.CurrentThread.ManagedThreadId + ": " +
                                "PackWebResponse - GetResponseStream() - stream length not available - disabling progressive download");
                }
#endif

                //  Start reading data from the response stream.
                _responseStream = _fullResponse.GetResponseStream();

                // require NetStream for progressivity and for network streams that don't
                // directly support seeking.
                if (!_responseStream.CanSeek || !_innerUri.IsFile)
                {
                    // Create a smart stream that will spawn byte-range requests as needed
                    // and support seeking. Each read has overhead of Mutex and many of the
                    // reads come through asking for 4 bytes at a time
                    _responseStream = new NetStream(
                        _responseStream, streamLength,
                        _innerUri, _webRequest, _fullResponse);

                    // wrap our stream for efficiency (short reads are expanded)
                    _responseStream = new BufferedStream(_responseStream);
                }

                // handle degenerate case where there is no part name
                if (_partName == null)
                {
                    _fullStreamLength = streamLength;    // entire container
                    _mimeType = WpfWebRequestHelper.GetContentType(_fullResponse);

                    // pass this so that ResponseStream holds a reference to us until the stream is closed
                    _responseStream = new ResponseStream(_responseStream, this);
                }
                else
                {
                    // open container on netStream
                    Package c = Package.Open(_responseStream);
                    if (!c.PartExists(_partName))
                        throw new WebException(SR.Get(SRID.WebResponsePartNotFound));

                    PackagePart p = c.GetPart(_partName);

                    Stream s = p.GetSeekableStream(FileMode.Open, FileAccess.Read);

                    _mimeType = new MS.Internal.ContentType(p.ContentType);      // save this for use in ContentType property - may still be null
                    _fullStreamLength = s.Length;   // just this stream

                    // Wrap in a ResponseStream so that this container will be released
                    // when the stream is closed
                    _responseStream = new ResponseStream(s, this, _responseStream, c);
                }

                // length available? (-1 means the server chose not to report it)
                if (_fullStreamLength >= 0)
                {
                    _lengthAvailable = true;
                }
            }

            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXPS, EventTrace.Event.WClientDRXGetStreamEnd);

            return _responseStream;
        }


        /// <summary>
        /// Close stream
        /// </summary>
        public override void Close()
        {
            Dispose(true);
        }
        #endregion

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
        /// <summary>
        /// InnerResponse
        /// </summary>
        /// <value>inner WebResponse</value>
        public WebResponse InnerResponse
        {
            get
            {
                CheckDisposed();

                // no inner response
                if (FromPackageCache)
                    return null;

                // can't do this until the response is available
                WaitForResponse();
                return _fullResponse;
            }
        }

        /// <summary>
        /// Headers
        /// </summary>
        /// <value>web headers</value>
        public override WebHeaderCollection Headers
        {
            get
            {
                CheckDisposed();

                // redirect
                if (FromPackageCache)
                    return _cachedResponse.Headers;

                // can't do this until the response is available
                WaitForResponse();
                return _fullResponse.Headers;
            }
        }

        /// <summary>
        /// ResponseUri
        /// </summary>
        /// <value>Fully-qualified pack uri reflecting any server redirection.</value>
        /// <remarks>For the inner ResponseUri access the InnerResponse like this:
        /// Uri innerResponseUri = packResponse.InnerResponse.ResponseUri</remarks>
        public override Uri ResponseUri
        {
            get
            {
                CheckDisposed();

                // If data is served from a cached package, we simply return the original, fully-qualified
                // pack uri.
                if (FromPackageCache)
                    return _uri;
                else
                {
                    // If data is served from an actual webResponse, we need to re-compose the original pack uri
                    // with the responseUri provided by the real webResponse to account for server redirects.
                    // We can't do this until the response is available so we wait.
                    WaitForResponse();

                    // create new pack uri with webResponse and original part name uri
                    return PackUriHelper.Create(_fullResponse.ResponseUri, _partName);
                }
            }
        }

        /// <summary>
        /// IsFromCache
        /// </summary>
        /// <value>true if result is from the cache</value>
        public override bool IsFromCache
        {
            get
            {
                CheckDisposed();

                // quick answer
                if (FromPackageCache)
                    return true;

                // can't do this until the response is available
                WaitForResponse();
                return _fullResponse.IsFromCache;
            }
        }

        /// <summary>
        /// ContentType
        /// </summary>
        /// <value>string</value>
        /// <remarks>There are four separate results possible from this property.  If the PartName is
        /// empty, then the container MIME type is returned.  If it is not, we return the MIME type of the
        /// stream in the following order or preference:
        /// 1. If the PackagePart offers a MIME type, we return that
        /// 2. If the stream name has an extension, we determine the MIME type from that using a lookup table
        /// 3. We return String.Empty if no extension is found.</remarks>
        public override string ContentType
        {
            get
            {
                CheckDisposed();

                // No need to wait if working from cache
                // But we can share the logic as none of it hits the _webResponse
                if (!FromPackageCache)
                {
                    // can't do this until the response is available
                    WaitForResponse();
                }

                // cache the value - if it's empty that means we already tried (and failed) to find a real type
                if (_mimeType == null)
                {
                    // Get the response stream which has the side effect of setting the _mimeType member.
                    GetResponseStream();
                }

                return _mimeType.ToString();
            }
        }

        /// <summary>
        /// ContentLength
        /// </summary>
        /// <value>length of response stream</value>
        public override long ContentLength
        {
            get
            {
                CheckDisposed();

                // redirect
                if (FromPackageCache)
                    return _cachedResponse.ContentLength;

                // can't do this until the response is available
                WaitForResponse();

                // is the length available?
                if (!_lengthAvailable)
                {
                    _fullStreamLength = GetResponseStream().Length;
                    _lengthAvailable = true;
                }

                // use the stored value
                return _fullStreamLength;
            }
        }

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------
        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        /// <summary>
        /// AbortResponse - called only from Close()
        /// </summary>
        /// <remarks>assumes caller has locked the syncObject and that we are not disposed</remarks>
        private void AbortResponse()
        {
// Disable the PreSharp warning about empty catch blocks - we need this one because sub-classes of WebResponse may or may
// not implement Abort() and we want to silently ignore this if they don't.
#pragma warning disable 56502
            // Close was called - abort the response if necessary
            try
            {
                // Only abort if the response is still "outstanding".
                // Non-blocking "peek" at the event to see if it is set or not.
                if (!_responseAvailable.WaitOne(0, false))
                {
                    _webRequest.Abort();    // response not back yet so abort it
                }
            }
            catch (NotImplementedException)
            {
                // Ignore - innerRequest class chose to implement BeginGetResponse but not Abort.  This is allowed.
            }
#pragma warning restore 56502
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // ignore multiple calls to Close()
                    // no lock required here because the only place where _disposed is changed is later in this same function and this
                    // function is never entered by more than one thread
                    if (_disposed)
                        return;

                    // redirect for cache case
                    // NOTE: FromPackageCache need not be synchronized because it is only set once in the constructor and never
                    // changes.
                    if (FromPackageCache)
                    {
                        _cachedResponse.Close();    // indirectly sets _disposed on this class
                        _cachedResponse = null;     // release
                        return;
                    }
#if DEBUG
                    if (PackWebRequestFactory._traceSwitch.Enabled)
                        System.Diagnostics.Trace.TraceInformation(
                                DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                                System.Threading.Thread.CurrentThread.ManagedThreadId + ": " +
                                "PackWebResponse.Close()");
#endif
                    // prevent async callback from accessing these resources while we are disposing them
                    lock (_lockObject)
                    {
                        try
                        {
                            // abort any outstanding response
                            AbortResponse();

                            // prevent recursion in our call to _responseStream.Close()
                            _disposed = true;

                            if (_responseStream != null)
                            {
#if DEBUG
                        if (PackWebRequestFactory._traceSwitch.Enabled)
                           System.Diagnostics.Trace.TraceInformation(
                                   DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                                   System.Threading.Thread.CurrentThread.ManagedThreadId + ": " +
                                   "PackWebResponse.Close() - close stream");
#endif
                                _responseStream.Close();
                            }

                            // FullResponse
                            if (_fullResponse != null)
                            {
#if DEBUG
                        if (PackWebRequestFactory._traceSwitch.Enabled)
                            System.Diagnostics.Trace.TraceInformation(
                                    DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                                    System.Threading.Thread.CurrentThread.ManagedThreadId + ": " +
                                    "PackWebResponse.Close() - close response");
#endif
                                // always call Dispose to satisfy FxCop
                                ((IDisposable)_fullResponse).Dispose();
                            }

                            // must free this regardless of whether GetResponseStream was invoked
                            _responseAvailable.Close();     // this call can not throw an exception

                            // timer
                            if (_timeoutTimer != null)
                            {
                                _timeoutTimer.Dispose();
                            }
}
                        finally
                        {
                            _timeoutTimer = null;
                            _responseStream = null;
                            _fullResponse = null;
                            _responseAvailable = null;
#if DEBUG
                            if (PackWebRequestFactory._traceSwitch.Enabled)
                                System.Diagnostics.Trace.TraceInformation(
                                        DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                                        System.Threading.Thread.CurrentThread.ManagedThreadId + ": " +
                                        "PackWebResponse.Close() - exiting");
#endif
                        }
                    } // lock
                }
            }
        }

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------
        private bool FromPackageCache
        {
            get
            {
                return (_cachedResponse != null);
            }
        }

        //------------------------------------------------------
        //
        //  Private Classes
        //
        //------------------------------------------------------
        /// <summary>
        /// CachedResponse class
        /// </summary>
        /// <remarks>Isolate cache-specific functionality to reduce complexity</remarks>
        private class CachedResponse
        {
            //------------------------------------------------------
            //
            //  Internal Methods
            //
            //------------------------------------------------------

            internal CachedResponse(PackWebResponse parent, Package cacheEntry, bool cachedPackageIsThreadSafe)
            {
                _parent = parent;
                _cacheEntry = cacheEntry;
                _cachedPackageIsThreadSafe = cachedPackageIsThreadSafe;
            }

            /// <summary>
            /// Cache version of GetResponseStream
            /// </summary>
            /// <returns></returns>
            internal Stream GetResponseStream()
            {
                // prevent concurrent access to GetPart() which is not thread-safe
                lock (_cacheEntry)
                {
#if DEBUG
            if (PackWebRequestFactory._traceSwitch.Enabled)
                System.Diagnostics.Trace.TraceInformation(
                        DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                        System.Threading.Thread.CurrentThread.ManagedThreadId + ": " +
                        "CachedResponse - Getting response stream");
#endif
                    // only one copy
                    if (_parent._responseStream == null)
                    {
                        // full container request?
                        if (_parent._partName == null)
                        {
                            Debug.Assert(false, "Cannot return full-container stream from cached container object");
                        }
                        else
                        {
#if DEBUG
                            if (PackWebRequestFactory._traceSwitch.Enabled)
                                System.Diagnostics.Trace.TraceInformation(
                                        DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                                        System.Threading.Thread.CurrentThread.ManagedThreadId + ": " +
                                        "CachedResponse - Getting part " + _parent._partName);
#endif
                            // open the requested stream
                            PackagePart p = _cacheEntry.GetPart(_parent._partName);
#if DEBUG
                            if (PackWebRequestFactory._traceSwitch.Enabled)
                                System.Diagnostics.Trace.TraceInformation(
                                        DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                                        System.Threading.Thread.CurrentThread.ManagedThreadId + ": " +
                                        "CachedResponse - Getting part stream ");
#endif
                            Stream s = p.GetSeekableStream(FileMode.Open, FileAccess.Read);

                            // Unless package is thread-safe, wrap the returned stream so that
                            // package access is serialized
                            if (!_cachedPackageIsThreadSafe)
                            {
                                // Return a stream that provides thread-safe access
                                // to the cached package by locking on the cached Package
                                s = new SynchronizingStream(s, _cacheEntry);
                            }

#if DEBUG
                            if (PackWebRequestFactory._traceSwitch.Enabled)
                                System.Diagnostics.Trace.TraceInformation(
                                        DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                                        System.Threading.Thread.CurrentThread.ManagedThreadId + ": " +
                                        "CachedResponse - Getting part contenttype");
#endif
                            _parent._mimeType = new MS.Internal.ContentType(p.ContentType);

                            // cache this in case they ask for it after the stream has been closed
                            _parent._lengthAvailable = s.CanSeek;
                            if (s.CanSeek)
                            {
#if DEBUG
                                if (PackWebRequestFactory._traceSwitch.Enabled)
                                    System.Diagnostics.Trace.TraceInformation(
                                            DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                                            System.Threading.Thread.CurrentThread.ManagedThreadId + ": " +
                                            "CachedResponse - Length is available from stream");
#endif
                                _parent._fullStreamLength = s.Length;
                            }
#if DEBUG
                            else
                            {
                                if (PackWebRequestFactory._traceSwitch.Enabled)
                                    System.Diagnostics.Trace.TraceInformation(
                                            DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                                            System.Threading.Thread.CurrentThread.ManagedThreadId + ": " +
                                            "CachedResponse - Length is not available from stream" + _parent._partName);
                            }
#endif
                            // re-use existing member variable
                            _parent._responseStream = s;
                        }
                    }
                }

                return _parent._responseStream;
            }

            /// <summary>
            /// Release some resources
            /// </summary>
            internal void Close()
            {
                try
                {
                    // Prevent recursion - this sync-protected member is safe to set in a CachedResponse
                    // mode because we have no other thread in operation.
                    _parent._disposed = true;
                    if (_parent._responseStream != null)
                        _parent._responseStream.Close();
                }
                finally
                {
                    _cacheEntry = null;
                    _parent._uri = null;
                    _parent._mimeType = null;
                    _parent._innerUri = null;
                    _parent._partName = null;
                    _parent._responseStream = null;
                    _parent = null;
                }
            }

            //------------------------------------------------------
            //
            //  Internal Properties
            //
            //------------------------------------------------------
            internal WebHeaderCollection Headers
            {
                get
                {
                    // empty - bogus collection - prevents exceptions for callers
                    return new WebHeaderCollection();
                }
            }

            public long ContentLength
            {
                get
                {
                    // if fullStreamLength not already set, get the stream which has
                    // the side effect of updating the length
                    if (!_parent._lengthAvailable)
                        GetResponseStream();

                    return _parent._fullStreamLength;
                }
            }

            // fields
            private PackWebResponse _parent;
            private Package        _cacheEntry;
            private bool            _cachedPackageIsThreadSafe;
        }

        /// <summary>
        /// Throw exception if we are already closed
        /// </summary>
        private void CheckDisposed()
        {
            // no need to lock here because only Close() sets this variable and we are not ThreadSafe
            if (_disposed)
                throw new ObjectDisposedException("PackWebResponse");
        }

        /// <summary>
        /// ResponseCallBack
        /// </summary>
        /// <param name="ar">async result</param>
        /// <remarks>static method not necessary</remarks>
        private void ResponseCallback(IAsyncResult ar)
        {
            lock (_lockObject)   // prevent race condition accessing _timeoutTimer, _disposed, _responseAvailable
            {
                try
                {
                    // If disposed, the message is too late
                    // Exit early and don't access members as they have been disposed
                    if (!_disposed)
                    {
                        // dispose the timer - it is no longer needed
                        if (_timeoutTimer != null)
                            _timeoutTimer.Dispose();
#if DEBUG
                        if (PackWebRequestFactory._traceSwitch.Enabled)
                            System.Diagnostics.Trace.TraceInformation(
                                    DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                                    System.Threading.Thread.CurrentThread.ManagedThreadId + ": " +
                                    "PackWebResponse.ResponseCallBack()");
#endif
                        // Dispose/Close waits on _responseAvailable so we know that these are available
                        // No need to lock.
                        // Call EndGetResponse, which produces the WebResponse object
                        // that came from the request issued above.
                        _fullResponse = MS.Internal.WpfWebRequestHelper.EndGetResponse(_webRequest, ar);
                    }
                }
                catch (WebException e)
                {
                    // web exceptions are meaningful to our client code - keep these to re-throw on the other thread
                    _responseException = e;
                    _responseError = true;
                }
                catch   // catch (and re-throw) all kinds of exceptions so we can inform the other thread
                {
#if DEBUG
                    if (PackWebRequestFactory._traceSwitch.Enabled)
                        System.Diagnostics.Trace.TraceError(
                                DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                                System.Threading.Thread.CurrentThread.ManagedThreadId + ": " +
                                "PackWebResponse.ResponseCallBack() exception");
#endif
                    // inform other thread of error condition
                    _responseError = true;

                    throw;
                }
                finally
                {
                    _timeoutTimer = null;       // harmless if already null, and removes need for extra try/finally block above
    #if DEBUG
                    if (PackWebRequestFactory._traceSwitch.Enabled)
                        System.Diagnostics.Trace.TraceInformation(
                                DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                                System.Threading.Thread.CurrentThread.ManagedThreadId + ": " +
                                "PackWebResponse.ResponseCallBack() - signal response available");
    #endif

// We need the original webRequest to get HttpStack information so that they can be used to make
//  additional byte range request; So we cannot null it out anymore
//                    _webRequest = null;     // don't need this anymore so release

                    // this must be set even when there is an exception so that our client
                    // can be unblocked
                    // make sure this wasn't already free'd when the Timercallback thread released
                    // the blocked Close() thread
                    if (!_disposed)
                        _responseAvailable.Set();
                }
            }
        }

        /// <summary>
        /// All methods that need to access variables only available after the response callback has been
        /// handled should call this to block.  Doing it in one place simplifies our need to respond to any
        /// exceptions encountered in the other thread and rethrow these.
        /// </summary>
        private void WaitForResponse()
        {
#if DEBUG
            if (PackWebRequestFactory._traceSwitch.Enabled)
                System.Diagnostics.Trace.TraceInformation(
                        DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                        System.Threading.Thread.CurrentThread.ManagedThreadId + ": " +
                        "PackWebResponse.WaitForResponse()");
#endif
            // wait for the response callback
            _responseAvailable.WaitOne();

            // We get here only when the other thread signals.
            // Need to inspect for errors and throw if there was trouble on the other thread.
            if (_responseError)
            {
                if (_responseException == null)
                    throw new WebException(SR.Get(SRID.WebResponseFailure));
                else
                    throw _responseException;   // throw literal exception if there is one
            }
        }

        /// <summary>
        /// Timeout callback
        /// </summary>
        /// <param name="stateInfo"></param>
        private void TimeoutCallback(Object stateInfo)
        {
            lock (_lockObject)   // prevent race condition accessing _timeoutTimer, _disposed, _responseAvailable
            {
                // If disposed, the message is too late
                // Exit early and don't access members as they have been disposed
                // Let Close() method clean up our Timer object
                if (_disposed)
                    return;

                try
                {
                    // If we get called, need to check if response is available before escalating
                    // just in case it arrived "just now".  If the event is already signalled, we can ignore
                    // the callback as no "timeout" occurred.
                    if (!_responseAvailable.WaitOne(0, false))
                    {
#if DEBUG
                        if (PackWebRequestFactory._traceSwitch.Enabled)
                            System.Diagnostics.Trace.TraceError(
                                    DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                                    System.Threading.Thread.CurrentThread.ManagedThreadId + ": " +
                                    "PackWebResponse.TimerCallback() timeout - throwing exception");
#endif
                        // caller is still blocked so need to throw to indicate timeout
                        // create exception to be thrown on client thread, then unblock the caller
                        // thread will be discovered and re-thrown in WaitForResponse() method
                        _responseError = true;
                        _responseException = new WebException(SR.Get(SRID.WebRequestTimeout, null), WebExceptionStatus.Timeout);
                    }
#if DEBUG
                    else
                    {
                        if (PackWebRequestFactory._traceSwitch.Enabled)
                            System.Diagnostics.Trace.TraceInformation(
                                    DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                                    System.Threading.Thread.CurrentThread.ManagedThreadId + ": " +
                                    "PackWebResponse.TimerCallback() no timeout - ignoring callback");
                    }
#endif
                    // clean up
                    if (_timeoutTimer != null)
                    {
                        _timeoutTimer.Dispose();
                    }
                }
                finally
                {
                    _timeoutTimer = null;
                    if (!_disposed)
                    {
                        // this must be set so that our client can be unblocked and then discover the exception
                        _responseAvailable.Set();
                    }
                }
            } // lock
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        private MS.Internal.ContentType     _mimeType;              // type of the returned stream - cached because it never changes
        private const int       _bufferSize = 0x1000;   // 4k
        private Uri             _uri;                   // full uri
        private Uri             _innerUri;              // inner uri
        private Uri             _partName;              // path to stream
        private bool            _disposed;              // closed?

        private WebRequest      _webRequest;            // the real web request

        private WebResponse     _fullResponse;          // the real web response
        private long            _fullStreamLength;      // need to return this in call to get_Length
        private Stream          _responseStream;            // mimic existing Response behavior by creating and returning
                                                            // one and only one stream
        private bool            _responseError;         // will be true if exception occurs calling EndGetResponse()
        private Exception       _responseException;     // actual exception to throw (if any)
        private Timer           _timeoutTimer;          // used if Timeout specified

        // OS event used to signal that the response is available
        private ManualResetEvent _responseAvailable;    // protects access to _fullResponse object

        // flag to signal that _fullStreamLength is valid - needed because some servers don't return
        // the length (usually FTP servers) so calls to Stream.Length must block until we know the actual length.
        private bool            _lengthAvailable;

        // PackageCache response?
        private CachedResponse  _cachedResponse;        // null if cache not used

        // private object to prevent deadlock (should not lock(_lockObject) based on PreSharp rule 6517)
        private Object            _lockObject;          // Serialize access to _disposed, _timoutTimer and _responseAvailable because even though the main client
                                                        // thread blocks on WaitForResponse (_responseAvailable event) the optional Timer thread and the
                                                        // Response callback thread may arrive independently at any time.

#if DEBUG
        // toggle this switch to force execution of code that handles servers that return bogus content length
        internal static System.Diagnostics.BooleanSwitch _forceWebResponseLengthFailureSwitch;
#endif
    }
}




