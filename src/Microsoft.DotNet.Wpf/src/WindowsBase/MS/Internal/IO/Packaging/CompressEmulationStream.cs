// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  Abstract base class that provides a fully functional Stream on top of different
//  various compression implementations.
//
//
//


using System;
using System.IO;
using System.IO.Compression;                // for DeflateStream
using System.Diagnostics;

using System.IO.Packaging;
using System.Windows;
using MS.Internal.WindowsBase;

namespace MS.Internal.IO.Packaging
{
    //------------------------------------------------------
    //
    //  Internal Members
    //
    //------------------------------------------------------
    /// <summary>
    /// Interface for Deflate transform object that we use to decompress and compress the actual bytes
    /// </summary>
    interface IDeflateTransform
    {
        void Decompress(Stream source, Stream sink);
        void Compress(Stream source, Stream sink);
    }

    /// <summary>
    /// Emulates a fully functional stream using restricted functionality DeflateStream and a temp file
    /// </summary>
    internal class CompressEmulationStream : Stream
    {
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
        #region Stream Methods
        /// <summary>
        /// Return the bytes requested from the container
        /// </summary>
        /// <param name="buffer">destination buffer</param>
        /// <param name="offset">offset to write into that buffer</param>
        /// <param name="count">how many bytes requested</param>
        /// <returns>how many bytes were written into <paramref name="buffer" />.</returns>
        /// <remarks>
        /// The underlying stream, expected to be an IsolatedStorageFileStream,
        /// is trusted to leave the IO position unchanged in case of an exception.
        /// </remarks>
        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckDisposed();

            PackagingUtilities.VerifyStreamReadArgs(this, buffer, offset, count);
            
            return _tempStream.Read(buffer, offset, count);
        }

        /// <summary>
        /// Seek
        /// </summary>
        /// <param name="offset">offset</param>
        /// <param name="origin">origin</param>
        /// <returns>zero</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            CheckDisposed();

            long temp = 0;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    {
                        temp = offset;
                        break;
                    }
                case SeekOrigin.Current:
                    {
                        checked { temp = _tempStream.Position + offset; }
                        break;
                    }
                case SeekOrigin.End:
                    {
                        checked { temp = _tempStream.Length + offset; }
                        break;
                    }
                default:
                    {
                        throw new ArgumentOutOfRangeException("origin", SR.Get(SRID.SeekOriginInvalid));
                    }
            }

            if (temp < 0)
            {
                throw new ArgumentException(SR.Get(SRID.SeekNegative));
            }

            return _tempStream.Seek(offset, origin);
        }

        /// <summary>
        /// SetLength
        /// </summary>
        public override void SetLength(long newLength)
        {
            CheckDisposed();

            _tempStream.SetLength(newLength);

            // truncation always involves change of stream pointer
            if (newLength < _tempStream.Position)
                _tempStream.Position = newLength;

            _dirty = true;
        }

        /// <summary>
        /// Write
        /// </summary>
        public override void Write(byte[] buffer, int offset, int count)
        {
            CheckDisposed();

            PackagingUtilities.VerifyStreamWriteArgs(this, buffer, offset, count);

            // no-op
            if (count == 0)
                return;

            _tempStream.Write(buffer, offset, count);
            _dirty = true;
        }

        /// <summary>
        /// Flush
        /// </summary>
        /// <remarks>Flushes to stream (if necessary)</remarks>
        public override void Flush()
        {
            CheckDisposed();

            if (_dirty)
            {
                // don't disturb our current position
                long tempPosition = _tempStream.Position;

                // compress
                _tempStream.Position = 0;
                _baseStream.Position = 0;
                _transformer.Compress(_tempStream, _baseStream);

                // restore
                _tempStream.Position = tempPosition;

                _baseStream.Flush();
                _dirty = false;
            }
        }
        #endregion Stream Methods

        #region Stream Properties
        /// <summary>
        /// Current logical position within the stream
        /// </summary>
        public override long Position
        {
            get
            {
                CheckDisposed();
                return _tempStream.Position;
            }
            set
            {
                CheckDisposed();

                if (value < 0)
                    throw new ArgumentException(SR.Get(SRID.SeekNegative));

                _tempStream.Position = value;
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
                return _tempStream.Length;
            }
        }

        /// <summary>
        /// Is stream readable?
        /// </summary>
        /// <remarks>returns false when called on disposed stream</remarks>
        public override bool CanRead
        {
            get
            {
                return (!_disposed && _baseStream.CanRead);
            }
        }

        /// <summary>
        /// Is stream seekable - should be handled by our owner
        /// </summary>
        /// <remarks>returns false when called on disposed stream</remarks>
        public override bool CanSeek
        {
            get
            {
                return (!_disposed &&  _baseStream.CanSeek);
            }
        }

        /// <summary>
        /// Is stream writeable?
        /// </summary>
        /// <remarks>returns false when called on disposed stream</remarks>
        public override bool CanWrite
        {
            get
            {
                return (!_disposed && _baseStream.CanWrite);
            }
        }
        #endregion

        #region Internal
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="baseStream">part stream - not closed - caller determines lifetime</param>
        /// <param name="position">current logical stream position</param>
        /// <param name="tempStream">should be an IsolatedStorageFileStream - not closed - caller determines lifetime</param>
        /// <param name="transformer">class that does the compression/decompression</param>
        /// <remarks>This class should only invoked when emulation is required.  
        /// Does not close any given stream, even when Close is called. This means that it requires
        /// another wrapper Stream class.</remarks>
        internal CompressEmulationStream(Stream baseStream, Stream tempStream, long position, IDeflateTransform transformer)
        {
            if (position < 0)
                throw new ArgumentOutOfRangeException("position");
        
            if (baseStream == null)
                throw new ArgumentNullException("baseStream");

            // seek and read required for emulation
            if (!baseStream.CanSeek)
                throw new InvalidOperationException(SR.Get(SRID.SeekNotSupported));

            if (!baseStream.CanRead)
                throw new InvalidOperationException(SR.Get(SRID.ReadNotSupported));

            if (tempStream == null)
                throw new ArgumentNullException("tempStream");

            if (transformer == null)
                throw new ArgumentNullException("transformer");

            _baseStream = baseStream;
            _tempStream = tempStream;
            _transformer = transformer;

            // extract to temporary stream
            _baseStream.Position = 0;
            _tempStream.Position = 0;
            _transformer.Decompress(baseStream, tempStream);

            // seek to the current logical position
            _tempStream.Position = position;
        }
        #endregion

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------
        /// <summary>
        /// Dispose(bool)
        /// </summary>
        /// <param name="disposing"></param>
        /// <remarks>We implement this because we want a consistent experience (essentially Flush our data) if the user chooses to 
        /// call Dispose() instead of Close().</remarks>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    if (!_disposed)
                    {
                        Flush();

                        _tempStream.Close();
                        _tempStream = null;

                        // never close base stream - we don't own it
//                        _baseStream.Close();
                        _baseStream = null;

                        _disposed = true;
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        /// <summary>
        /// Call this before accepting any public API call (except some Stream calls that
        /// are allowed to respond even when Closed
        /// </summary>
        protected void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(null, SR.Get(SRID.StreamObjectDisposed));
        }

        #region Private
        //------------------------------------------------------
        //
        //  Private Variables
        //
        //------------------------------------------------------
        private bool    _disposed;          // disposed?
        private bool    _dirty;             // do we need to recompress?
        protected Stream  _baseStream;      // stream we ultimately decompress from and to in the container
        protected Stream _tempStream;       // temporary storage for the uncompressed stream
        IDeflateTransform _transformer;   // does the actual compress/decompress for us
        #endregion
    }
}
