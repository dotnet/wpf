// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//   This class provides file versioning support for streams provided by 
//   IDataTransform implementations.
//
//
//
//

using System;
using System.IO;                                // for Stream
using System.Windows;                           // ExceptionStringTable
using System.Globalization;                     // for CultureInfo
using MS.Internal.WindowsBase;

namespace MS.Internal.IO.Packaging.CompoundFile
{
    /// <summary>
    /// Maintains a FormatVersion for this stream and any number of sibling streams that semantically
    /// share the same version information (which is only persisted in one of the streams).
    /// </summary>
    internal class VersionedStream : Stream
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
        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckDisposed();

            // ReadAttempt accepts an optional boolean.  If this is true, that means
            // we are expecting a legal FormatVersion to exist and that it is readable by our
            // code version.  We do not want to force this check if we are empty.
            _versionOwner.ReadAttempt(_stream.Length > 0);
            return _stream.Read(buffer, offset, count);
        }

        /// <summary>
        /// Write
        /// </summary>
        public override void Write(byte[] buffer, int offset, int count)
        {
            CheckDisposed();
            _versionOwner.WriteAttempt();
            _stream.Write(buffer, offset, count);
        }

        /// <summary>
        /// ReadByte
        /// </summary>
        public override int ReadByte()
        {
            CheckDisposed();

            // ReadAttempt accepts an optional boolean.  If this is true, that means
            // we are expecting a legal FormatVersion to exist and that it is readable by our
            // code version.  We do not want to force this check if we are empty.
            _versionOwner.ReadAttempt(_stream.Length > 0);
            return _stream.ReadByte();
        }

        /// <summary>
        /// WriteByte
        /// </summary>
        public override void WriteByte(byte b)
        {
            CheckDisposed();
            _versionOwner.WriteAttempt();
            _stream.WriteByte(b);
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
            return _stream.Seek(offset, origin);
        }

        /// <summary>
        /// SetLength
        /// </summary>
        public override void SetLength(long newLength)
        {
            CheckDisposed();

            if (newLength < 0)
                throw new ArgumentOutOfRangeException("newLength");

            _versionOwner.WriteAttempt();
            _stream.SetLength(newLength);
        }

        /// <summary>
        /// Flush
        /// </summary>
        public override void Flush()
        {
            CheckDisposed();
            _stream.Flush();
        }
        #endregion Stream Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
        #region Stream Properties
        /// <summary>
        /// Current logical position within the stream
        /// </summary>
        public override long Position
        {
            get
            {
                CheckDisposed();
                return _stream.Position;
            }
            set
            {
                Seek(value, SeekOrigin.Begin);
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
                return _stream.Length;
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
                return (_stream != null) && _stream.CanRead && _versionOwner.IsReadable;
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
                return (_stream != null) && _stream.CanSeek && _versionOwner.IsReadable;
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
                return (_stream != null) && _stream.CanWrite && _versionOwner.IsUpdatable;
            }
        }
        #endregion

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        /// <summary>
        /// Constructor to use for any stream that shares versioning information with another stream
        /// but is not the one that houses the FormatVersion data itself.
        /// </summary>
        /// <param name="baseStream"></param>
        /// <param name="versionOwner"></param>
        internal VersionedStream(Stream baseStream, VersionedStreamOwner versionOwner)
        {
            if (baseStream == null)
                throw new ArgumentNullException("baseStream");

            if (versionOwner == null)
                throw new ArgumentNullException("versionOwner");

            _stream = baseStream;
            _versionOwner = versionOwner;
        }

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------
        /// <summary>
        /// Constructor for use by our subclass VersionedStreamOwner.
        /// </summary>
        /// <param name="baseStream"></param>
        protected VersionedStream(Stream baseStream)
        {
            if (baseStream == null)
                throw new ArgumentNullException("baseStream");

            _stream = baseStream;

            // we are actually a VersionedStreamOwner
            _versionOwner = (VersionedStreamOwner)this;
        }

        /// <summary>
        /// Sometimes our subclass needs to read/write directly to the stream
        /// </summary>
        /// <remarks>Don't use CheckDisposed() here as we need to return null if we are disposed</remarks>
        protected Stream BaseStream
        {
            get
            {
                return _stream;
            }
        }

        /// <summary>
        /// Dispose(bool)
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && (_stream != null))
                {
                    _stream.Close();
                }
            }
            finally
            {
                _stream = null;
                base.Dispose(disposing);
            }
        }

        /// <summary>
        /// Call this before accepting any public API call (except some Stream calls that
        /// are allowed to respond even when Closed
        /// </summary>
        protected void CheckDisposed()
        {
            if (_stream == null)
                throw new ObjectDisposedException(null, SR.StreamObjectDisposed);
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        private VersionedStreamOwner    _versionOwner;
        private Stream                  _stream;            // null indicates Disposed state
    }
}
