// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//------------------------------------------------------------------------------
//
//
//
//  File:           SynchronizingStream.cs
//
//  Description:    Stream that locks on given syncRoot before entering any public API's.
//
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace MS.Internal.IO.Packaging
{
    /// <summary>
    /// Wrap returned stream to protect non-thread-safe API's from race conditions
    /// </summary>
    internal class SynchronizingStream : Stream
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
        /// <summary>
        /// Serializes access by locking on the given object
        /// </summary>
        /// <param name="stream">stream to read from (baseStream)</param>
        /// <param name="syncRoot">object to lock on</param>
        internal SynchronizingStream(Stream stream, Object syncRoot)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            if (syncRoot == null)
                throw new ArgumentNullException("syncRoot");

            _baseStream = stream;
            _syncRoot = syncRoot;
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
        public override int Read(byte[] buffer, int offset, int count)
        {
            lock (_syncRoot)
            {
                CheckDisposed();
                return (_baseStream.Read(buffer, offset, count));
            }
        }

        /// <summary>
        /// Read a single byte
        /// </summary>
        /// <returns>The unsigned byte cast to an Int32, or -1 if at the end of the stream.</returns>
        public override int ReadByte()
        {
            lock (_syncRoot)
            {
                CheckDisposed();
                return (_baseStream.ReadByte());
            }
        }

        /// <summary>
        /// Write a single byte
        /// </summary>
        /// <param name="b">byte to write</param>
        public override void WriteByte(byte b)
        {
            lock (_syncRoot)
            {
                CheckDisposed();
                _baseStream.WriteByte(b);
            }
        }

        /// <summary>
        /// Seek
        /// </summary>
        /// <param name="offset">only zero is supported</param>
        /// <param name="origin">only SeekOrigin.Begin is supported</param>
        /// <returns>zero</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            lock (_syncRoot)
            {
                CheckDisposed();
                return _baseStream.Seek(offset, origin);
            }
        }
        /// <summary>
        /// SetLength
        /// </summary>
        /// <exception cref="NotSupportedException">not supported</exception>
        public override void SetLength(long newLength)
        {
            lock (_syncRoot)
            {
                CheckDisposed();
                _baseStream.SetLength(newLength);
            }
        }

        /// <summary>
        /// Write
        /// </summary>
        /// <exception cref="NotSupportedException">not supported</exception>
        public override void Write(byte[] buf, int offset, int count)
        {
            lock (_syncRoot)
            {
                CheckDisposed();
                _baseStream.Write(buf, offset, count);
            }
        }

        /// <summary>
        /// Flush
        /// </summary>
        /// <exception cref="NotSupportedException">not supported</exception>
        public override void Flush()
        {
            lock (_syncRoot)
            {
                CheckDisposed();
                _baseStream.Flush();
            }
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
                lock (_syncRoot)
                {
                    return ((_baseStream != null) && _baseStream.CanRead);
                }
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
                lock (_syncRoot)
                {
                    return ((_baseStream != null) && _baseStream.CanSeek);
                }
            }
        }
        /// <summary>
        /// Is stream writeable?
        /// </summary>
        public override bool CanWrite
        {
            get
            {
                lock (_syncRoot)
                {
                    return ((_baseStream != null) && _baseStream.CanWrite);
                }
            }
        }

        /// <summary>
        /// Logical byte position in this stream
        /// </summary>
        public override long Position
        {
            get
            {
                lock (_syncRoot)
                {
                    CheckDisposed();
                    return _baseStream.Position;
                }
            }
            set
            {
                lock (_syncRoot)
                {
                    CheckDisposed();
                    _baseStream.Position = value;
                }
            }
        }

        /// <summary>
        /// Length
        /// </summary>
        public override long Length
        {
            get
            {
                lock (_syncRoot)
                {
                    CheckDisposed();
                    return _baseStream.Length;
                }
            }
        }

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------
        protected override void Dispose(bool disposing)
        {
            lock (_syncRoot)
            {
                try
                {
                    if (disposing && (_baseStream != null))
                    {
                        // close the underlying Stream
                        _baseStream.Close();

                        // NOTE: We cannot set _syncRoot to null because it is used
                        // on entry to every public method and property.
                    }
                }
                finally
                {
                    base.Dispose(disposing);
                    _baseStream = null;
                }
            }
        }

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        /// <summary>
        /// CheckDisposed
        /// </summary>
        /// <remarks>Pre-condition that lock has been acquired.</remarks>
        private void CheckDisposed()
        {
            if (_baseStream == null)
                throw new ObjectDisposedException("Stream");
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        private Stream      _baseStream;    // stream we are wrapping
        private Object      _syncRoot;      // object to lock on
    }
}

