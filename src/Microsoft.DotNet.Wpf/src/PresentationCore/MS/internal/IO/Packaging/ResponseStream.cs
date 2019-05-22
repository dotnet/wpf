// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//  Description:    Exists so that the gc lifetime for the container
//                  and the webresponse are shared.
//
//                  This wrapper is returned for any PackWebResponse satisified
//                  with a container.  It ensures that the container lives until
//                  the stream is closed because we are unaware of the lifetime of
//                  the stream and the client is unaware of the existence of the
//                  container.
//
//                  Container is never closed because it may be used by other
//                  responses.
//
//                  12/11/03 - brucemac - adapted from ResponseStream
//                  15/10/04 - brucemac - adapted from ContainerResponseStream

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;      // for PackWebResponse
using MS.Utility;
using System.Windows;

namespace MS.Internal.IO.Packaging
{
    /// <summary>
    /// Wrap returned stream so we can release the webresponse container when the stream is closed
    /// </summary>
    internal class ResponseStream : Stream
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
        /// <summary>
        /// Wraps PackWebResponse to ensure correct lifetime handling and stream length functionality
        /// </summary>
        /// <param name="s">stream to read from (baseStream)</param>
        /// <param name="response">response</param>
        /// <param name="owningStream">stream under the package</param>
        /// <param name="container">container to hold on to</param>
        internal ResponseStream(Stream s, PackWebResponse response, Stream owningStream, System.IO.Packaging.Package container)
        {
            Debug.Assert(container != null, "Logic error: use other constructor for full package request streams");
            Debug.Assert(owningStream != null, "Logic error: use other constructor for full package request streams");
            Init(s, response, owningStream, container);
        }

        /// <summary>
        /// Wraps stream returned by PackWebResponse to ensure correct lifetime handlingy
        /// </summary>
        /// <param name="s">stream to read from (baseStream)</param>
        /// <param name="response">webresponse to close when we close</param>
        internal ResponseStream(Stream s, PackWebResponse response)
        {
            Init(s, response, null, null);
        }

        /// <summary>
        /// Wraps PackWebResponse to ensure correct lifetime handling and stream length functionality
        /// </summary>
        /// <param name="s">stream to read from (baseStream)</param>
        /// <param name="owningStream">stream under the container</param>
        /// <param name="response">response</param>
        /// <param name="container">container to hold on to</param>
        private void Init(Stream s, PackWebResponse response, Stream owningStream, System.IO.Packaging.Package container)
        {
            Debug.Assert(s != null, "Logic error: base stream cannot be null");
            Debug.Assert(response != null, "Logic error: response cannot be null");

            _innerStream = s;
            _response = response;
            _owningStream = owningStream;
            _container = container;
        }

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Return the bytes requested
        /// </summary>
        /// <param name="buffer">destination buffer</param>
        /// <param name="offset">offset to write into that buffer</param>
        /// <param name="count">how many bytes requested</param>
        /// <returns>how many bytes were written into buffer</returns>
        /// <remarks>
        /// Blocks until data is available.
        /// The read semantics, and in particular the restoration of the position in case of an
        /// exception, is implemented by the inner stream, i.e. the stream returned by PackWebResponse.
        /// </remarks>
        public override int Read(byte[] buffer, int offset, int count)
        {
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXPS, EventTrace.Level.Verbose, EventTrace.Event.WClientDRXReadStreamBegin, count);

            CheckDisposed();

            int rslt = _innerStream.Read(buffer, offset, count);

            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXPS, EventTrace.Level.Verbose, EventTrace.Event.WClientDRXReadStreamEnd, rslt);

            return rslt;
        }

        /// <summary>
        /// Seek
        /// </summary>
        /// <param name="offset">only zero is supported</param>
        /// <param name="origin">only SeekOrigin.Begin is supported</param>
        /// <returns>zero</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            CheckDisposed();
            return _innerStream.Seek(offset, origin);
        }
        /// <summary>
        /// SetLength
        /// </summary>
        /// <exception cref="NotSupportedException">not supported</exception>
        public override void SetLength(long newLength)
        {
            CheckDisposed();
            _innerStream.SetLength(newLength);
        }

        /// <summary>
        /// Write
        /// </summary>
        /// <exception cref="NotSupportedException">not supported</exception>
        public override void Write(byte[] buf, int offset, int count)
        {
            CheckDisposed();
            _innerStream.Write(buf, offset, count);
        }

        /// <summary>
        /// Flush
        /// </summary>
        /// <exception cref="NotSupportedException">not supported</exception>
        public override void Flush()
        {
            CheckDisposed();
            _innerStream.Flush();
        }

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        /// <summary>
        /// Is stream readable?
        /// </summary>
        public override bool CanRead
        {
            get
            {
                return (!_closed && _innerStream.CanRead);
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
                return (!_closed && _innerStream.CanSeek);
            }
        }
        /// <summary>
        /// Is stream writeable?
        /// </summary>
        public override bool CanWrite
        {
            get
            {
                return (!_closed && _innerStream.CanWrite);
            }
        }

        /// <summary>
        /// Logical byte position in this stream
        /// </summary>
        public override long Position
        {
            get
            {
                CheckDisposed();
                return _innerStream.Position;
            }
            set
            {
                CheckDisposed();
                _innerStream.Position = value;
            }
        }

        /// <summary>
        /// Length
        /// </summary>
        public override long Length
        {
            get
            {
                CheckDisposed();

                // inner stream should always know its length because it's based on a local file
                // or because it's on a NetStream that can fake this
                return _innerStream.Length;
            }
        }

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && !_closed)
                {
#if DEBUG
                    if (PackWebRequestFactory._traceSwitch.Enabled)
                        System.Diagnostics.Trace.TraceInformation("ContainerResponseStream.Dispose(bool)");
#endif
                    _container = null;

                    // close the Part or NetStream
                    _innerStream.Close();

                    if (_owningStream != null)
                    {
                        // in this case, the innerStream was the part so this is the NetStream
                        _owningStream.Close();
                    }
                }
            }
            finally
            {
                _innerStream = null;
                _owningStream = null;
                _response = null;
                _closed = true;
                base.Dispose(disposing);
            }
        }

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        private void CheckDisposed()
        {
            if (_closed)
                throw new ObjectDisposedException("ResponseStream");
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        private bool            _closed;        // prevent recursion
        private Stream          _innerStream;   // stream we are emulating
        private System.IO.Packaging.Package         _container;     // container to release when we are closed
        private Stream          _owningStream;  // stream under the _innerStream when opening a Part
        private PackWebResponse _response;      // packWebResponse we can consult for reliable length
    }
}


