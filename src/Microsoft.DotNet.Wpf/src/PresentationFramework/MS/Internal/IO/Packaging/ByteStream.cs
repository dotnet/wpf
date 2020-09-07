// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//   Stream interface for manipulating data within a stream.
//


using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;      // for IStream
using System.Windows;
using MS.Win32;                                     // for NativeMethods
using System.Security;                              // for marking critical methods

namespace MS.Internal.IO.Packaging
{
    /// <summary>
    /// Class for managing an COM IStream interface.
    /// </summary>
    internal sealed class ByteStream : Stream
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
        #region Constructors

        /// <summary>
        /// Constructs a ByteStream class from a supplied unmanaged stream with the specified access.
        /// </summary>
        /// <param name="underlyingStream"></param>
        /// <param name="openAccess"></param>
        internal ByteStream(object underlyingStream, FileAccess openAccess)
        {
            SecuritySuppressedIStream stream = underlyingStream as SecuritySuppressedIStream;
            Debug.Assert(stream != null);

            _securitySuppressedIStream = new SecurityCriticalDataForSet<SecuritySuppressedIStream>(stream);
            
            _access = openAccess;
            // we only work for reading.
            Debug.Assert(_access == FileAccess.Read);
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
        #region Public Properties

        /// <summary>
        /// Gets a value indicating whether the current stream supports reading.
        /// </summary>
        public override bool CanRead
        {
            get
            {
                return (!StreamDisposed && 
                        (FileAccess.Read == (_access & FileAccess.Read) ||
                         FileAccess.ReadWrite == (_access & FileAccess.ReadWrite)));
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports seeking.
        /// </summary>
        public override bool CanSeek
        {
            get
            {
                return (!StreamDisposed);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports writing.
        /// </summary>
        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the length in bytes of the stream.
        /// </summary>
        public override long Length
        {
            get
            {
                CheckDisposedStatus();

                if (!_isLengthInitialized)
                {
                    System.Runtime.InteropServices.ComTypes.STATSTG streamStat;

                    // call Stat to get length back.  STATFLAG_NONAME means string buffer
                    // is not populated.

                    _securitySuppressedIStream.Value.Stat(out streamStat, NativeMethods.STATFLAG_NONAME);

                    _isLengthInitialized = true;
                    _length = streamStat.cbSize;
                }

                Debug.Assert(_length > 0);
                return _length;
            }
        }


        /// <summary>
        /// Gets or sets the position within the current stream.
        /// </summary>
        public override long Position
        {
            get
            {
                CheckDisposedStatus();
                
                long seekPos = 0;

                _securitySuppressedIStream.Value.Seek(0,
                                                      NativeMethods.STREAM_SEEK_CUR,
                                                      out seekPos);

                return seekPos;
            }

            set
            {
                CheckDisposedStatus();

                if (!CanSeek)
                {
                    throw new NotSupportedException(SR.Get(SRID.SetPositionNotSupported));
                }
                
                long seekPos = 0;

                _securitySuppressedIStream.Value.Seek(value,
                                                      NativeMethods.STREAM_SEEK_SET,
                                                      out seekPos);

                if (value != seekPos)
                {
                    throw new IOException(SR.Get(SRID.SeekFailed));
                }
            }
        }

        #endregion Public Properties


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
        #region Public Methods

        public override void Flush()
        {
            // deliberate overidding noop
            // do not want to do anything on a read-only stream
        }

        /// <summary>
        /// Sets the position within the current stream.
        /// </summary>
        /// <remarks>
        /// ByteStream relies on underlying COM interface to
        /// validate offsets.
        /// </remarks>
        /// <param name="offset">Offset byte count</param>
        /// <param name="origin">Offset origin</param>
        /// <returns>The new position within the current stream.</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            CheckDisposedStatus();

            if (!CanSeek)
            {
                throw new NotSupportedException(SR.Get(SRID.SeekNotSupported));
            }

            long seekPos = 0;
            int translatedSeekOrigin = 0;

            switch (origin)
            {
                case SeekOrigin.Begin:
                    translatedSeekOrigin = NativeMethods.STREAM_SEEK_SET;
                    if (0 > offset)
                    {
                        throw new ArgumentOutOfRangeException("offset",
                                                              SR.Get(SRID.SeekNegative));
                    }
                    break;

                case SeekOrigin.Current:
                    translatedSeekOrigin = NativeMethods.STREAM_SEEK_CUR;
                    break;

                case SeekOrigin.End:
                    translatedSeekOrigin = NativeMethods.STREAM_SEEK_END;
                    break;
                default:
                    throw new System.ComponentModel.InvalidEnumArgumentException("origin",
                                                                                 (int)origin,
                                                                                 typeof(SeekOrigin));
            }

            _securitySuppressedIStream.Value.Seek(offset, translatedSeekOrigin, out seekPos);

            return seekPos;
        }

        /// <summary>
        /// Sets the length of the current stream.
        /// 
        /// Not Supported in this implementation.
        /// </summary>
        /// <param name="newLength">New length</param>
        public override void SetLength(long newLength)
        {
            throw new NotSupportedException(SR.Get(SRID.SetLengthNotSupported));
        }

        /// <summary>
        /// Reads a sequence of bytes from the current stream and advances the 
        /// position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">Read data buffer</param>
        /// <param name="offset">Buffer start position</param>
        /// <param name="count">Number of bytes to read</param>
        /// <returns>Number of bytes actually read</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckDisposedStatus();

            if (!CanRead)
            {
                throw new NotSupportedException(SR.Get(SRID.ReadNotSupported));
            }
            
            int read = 0;

            // optimization: if we are being asked to read zero bytes, be done.
            if (count == 0)
            {
                return read;
            }
            
            // count has to be positive number
            if (0 > count)
            {
                throw new ArgumentOutOfRangeException("count",
                                                      SR.Get(SRID.ReadCountNegative));
            }

            // offset has to be a positive number
            if (0 > offset)
            {
                throw new ArgumentOutOfRangeException("offset",
                                                      SR.Get(SRID.BufferOffsetNegative));
            }

            // make sure that we have a buffer that matches number of bytes we need to read 
            // since all values are > 0, there is no chance of overflow
            if (!((buffer.Length > 0) && ((buffer.Length - offset) >= count)))
            {
                throw new ArgumentException(SR.Get(SRID.BufferTooSmall), "buffer");
            }
            
            // offset == 0 is the normal case
            if (0 == offset)
            {
                _securitySuppressedIStream.Value.Read(buffer, count, out read);
            }
            // offset involved.  Must be positive
            else if (0 < offset)
            {
                // Read into local array and then copy it into the given buffer at
                //  the specified offset.
                byte[] localBuffer = new byte[count];

                _securitySuppressedIStream.Value.Read(localBuffer, count, out read);

                if (read > 0)
                {
                    Array.Copy(localBuffer, 0, buffer, offset, read);
                }
            }
            // Negative offsets are not allowed and coverd above.

            return read;
        }

        /// <summary>
        /// Writes a sequence of bytes to the current stream and advances the 
        /// current position within this stream by the number of bytes written.
        /// 
        /// Not Supported in this implementation.
        /// </summary>
        /// <param name="buffer">Data buffer</param>
        /// <param name="offset">Buffer write start position</param>
        /// <param name="count">Number of bytes to write</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException(SR.Get(SRID.WriteNotSupported));
        }

        /// <summary>
        /// Closes the current stream and releases any resources (such as 
        /// sockets and file handles) associated with the current stream.
        /// </summary>
        public override void Close()
        {
            _disposed = true;
        }

        #endregion Public Methods


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        #region Internal Methods

        /// <summary>
        /// Check whether this Stream object is still valid.  If not, thrown an
        /// ObjectDisposedException.
        /// </summary>
        internal void CheckDisposedStatus()
        {
            if (StreamDisposed)
                throw new ObjectDisposedException(null, SR.Get(SRID.StreamObjectDisposed));
        }

        #endregion Internal Methods


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        #region Internal Properties

        /// <summary>
        /// Check whether this Stream object is still valid.
        /// </summary>
        private bool StreamDisposed
        {
            get
            {
                return _disposed;
            }
        }

        #endregion Internal Properties
        

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        #region Private Fields

        // This class does not control the life cycle of _securitySupressedIStream
        //  thus it should not dispose it when this class gets disposed
        //  the client code of this class should be the one that dispose _securitySupressedIStream
        SecurityCriticalDataForSet<SecuritySuppressedIStream> _securitySuppressedIStream;

        FileAccess                 _access;
        long                       _length = 0;
        bool                       _isLengthInitialized = false;
        bool                       _disposed = false;

        #endregion Private Fields
        
        //------------------------------------------------------
        //
        //  Private Unmanaged Interfaces
        //
        //------------------------------------------------------
        #region Private Unmanaged Interface imports
        
        // ****CAUTION****: Be careful using this interface, because it suppresses
        //  the check for unmanaged code security permission.  It is recommended
        //  that all instances of this interface also have "SecuritySuppressed" in
        //  its name to make it clear that it is a dangerous interface.  Also, it
        //  is expected that each use of this interface be reviewed for security
        //  soundness.
        [Guid("0000000c-0000-0000-C000-000000000046")]
        [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
        [ComImport]
        public interface SecuritySuppressedIStream
        {
            // ISequentialStream portion
            void Read([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), Out] Byte[] pv, int cb, out int pcbRead);
            void Write([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Byte[] pv, int cb, out int pcbWritten);

            // IStream portion
            void Seek(long dlibMove, int dwOrigin, out long plibNewPosition);
            void SetSize(long libNewSize);
            void CopyTo(SecuritySuppressedIStream pstm, long cb, out long pcbRead, out long pcbWritten);
            void Commit(int grfCommitFlags);
            void Revert();
            void LockRegion(long libOffset, long cb, int dwLockType);
            void UnlockRegion(long libOffset, long cb, int dwLockType);
            void Stat(out System.Runtime.InteropServices.ComTypes.STATSTG pstatstg, int grfStatFlag);
            void Clone(out SecuritySuppressedIStream ppstm);
        }

        #endregion Private Unmanaged Interface imports
    }
}
