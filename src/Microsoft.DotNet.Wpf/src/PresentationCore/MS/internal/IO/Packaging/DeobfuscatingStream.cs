// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  This class provides stream implementation that de-obfuscates the bytes that are obfuscated in accordance
//      with the section under Embbedded Font Obfuscation in the XPS spec.
//
//  Recap of font obfuscation:
//      1. Generate a 128-bit GUID (a 128-bit random number may be use instead)
//      2. Generate a part name using the GUID
//      3. XOR the first 32 bytes of the binary data of the font with the binary representation of the GUID
//      4. in the step #3, start with the LSB of the binary GUID
//
// Notes:
//  The stream is read only


using System;
using System.IO;
using System.IO.Packaging;

using MS.Internal.PresentationCore;     // for ExceptionStringTable

namespace MS.Internal.IO.Packaging
{
    //------------------------------------------------------
    //
    //  Internal Members
    //
    //------------------------------------------------------
    /// <summary>
    /// Wrapper stream that returns de-obfuscated bytes from obfuscated stream
    /// </summary>
    internal class DeobfuscatingStream : Stream
    {
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
        #region Stream Methods
        /// <summary>
        /// Read bytes from the stream
        /// </summary>
        /// <param name="buffer">destination buffer</param>
        /// <param name="offset">offset to read into that buffer</param>
        /// <param name="count">how many bytes requested</param>
        /// <returns>how many bytes were wread <paramref name="buffer" />.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckDisposed();

            long readPosition = _obfuscatedStream.Position;

            // Read in the raw data from the underlying stream
            int bytesRead = _obfuscatedStream.Read(buffer, offset, count);

            // Apply de-obfuscatation as necessary
            Deobfuscate(buffer, offset, bytesRead, readPosition);

            return bytesRead;
        }

        /// <summary>
        /// Write
        /// </summary>
        /// <remarks>This is a read-only stream; throw now supported exception</remarks>
        public override void Write(byte[] buffer, int offset, int count)
        {
            CheckDisposed();

            throw new NotSupportedException(SR.Get(SRID.WriteNotSupported));
        }

        /// <summary>
        /// Seek
        /// </summary>
        /// <param name="offset">offset</param>
        /// <param name="origin">origin</param>
        public override long Seek(long offset, SeekOrigin origin)
        {
            CheckDisposed();

            return _obfuscatedStream.Seek(offset, origin);
        }

        /// <summary>
        /// SetLength
        /// </summary>
        /// <remarks>This is a read-only stream; throw now supported exception</remarks>
        public override void SetLength(long newLength)
        {
            CheckDisposed();

            throw new NotSupportedException(SR.Get(SRID.SetLengthNotSupported));
       }

        /// <summary>
        /// Flush
        /// </summary>
        public override void Flush()
        {
            CheckDisposed();

            _obfuscatedStream.Flush();
        }
        #endregion Stream Methods

        #region Stream Properties
        /// <summary>
        /// Current position of the stream
        /// </summary>
        public override long Position
        {
            get
            {
                CheckDisposed();

                return _obfuscatedStream.Position;
            }
            set
            {
                CheckDisposed();

                _obfuscatedStream.Position = value;
            }
        }

        /// <summary>
        /// Length
        /// </summary>
        public override long Length
        {
            get
            {
                return _obfuscatedStream.Length;
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
                // cannot read from a close stream, but don't throw if asked
                return (_obfuscatedStream != null) && _obfuscatedStream.CanRead;
            }
        }

        /// <summary>
        /// Is stream seekable
        /// </summary>
        /// <remarks>returns false when called on disposed stream</remarks>
        public override bool CanSeek
        {
            get
            {
                // cannot seek on a close stream, but don't throw if asked
                return (_obfuscatedStream != null) && _obfuscatedStream.CanSeek;
            }
        }

        /// <summary>
        /// Is stream writeable?
        /// </summary>
        /// <remarks>returns false always since it is a read-only stream</remarks>
        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }
        #endregion

        #region Internal
        //------------------------------------------------------
        //
        //  Internal Constructors
        //
        //------------------------------------------------------
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="obfuscatedStream">stream that holds obfuscated resource</param>
        /// <param name="streamUri">the original Uri which is used to obtain obfuscatedStream; it holds
        ///                         the GUID information which is used to obfuscate the resources</param>
        /// <param name="leaveOpen">if it is false, obfuscatedStream will be also disposed when
        ///                         DeobfuscatingStream is disposed</param>
        /// <remarks>streamUri has to be a pack Uri</remarks>
        internal DeobfuscatingStream(Stream obfuscatedStream, Uri streamUri, bool leaveOpen)
        {
            if (obfuscatedStream == null)
            {
                throw new ArgumentNullException("obfuscatedStream");
            }

            // Make sure streamUri is in the correct form; getting partUri from it will do all necessary checks for error
            //    conditions; We also have to make sure that it has a part name
            Uri partUri = System.IO.Packaging.PackUriHelper.GetPartUri(streamUri);
            if (partUri == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.InvalidPartName));
            }

            // Normally we should use PackUriHelper.GetStringForPartUri to get the string representation of part Uri
            //    however, since we already made sure that streamUris is in the correct form (such as to check if it is an absolute Uri
            //    and there is a correct authority (package)), it doesn't have to be fully validated again.
            // Get the escaped string for the part name as part names should have only ascii characters
            String guid = Path.GetFileNameWithoutExtension(
                                    streamUri.GetComponents(UriComponents.Path | UriComponents.KeepDelimiter, UriFormat.UriEscaped));

            _guid = GetGuidByteArray(guid);
            _obfuscatedStream = obfuscatedStream;
            _ownObfuscatedStream = !leaveOpen;
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
                    // If this class owns the underlying steam, close it
                    if (_obfuscatedStream != null && _ownObfuscatedStream)
                    {
                        _obfuscatedStream.Close();
                    }
                    _obfuscatedStream = null;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        #region Private
        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------
        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        /// <summary>
        /// Call this before accepting any public API call (except some Stream calls that
        /// are allowed to respond even when Closed
        /// </summary>
        private void CheckDisposed()
        {
            if (_obfuscatedStream == null)
                throw new ObjectDisposedException(null, SR.Get(SRID.Media_StreamClosed));
        }

        /// <summary>
        /// Apply de-obfuscation as necessary (the first 32 bytes of the underlying stream are obfuscated
        /// </summary>
        private void Deobfuscate(byte[] buffer, int offset, int count, long readPosition)
        {
            // only the first 32 bytes of the underlying stream are obfuscated
            //   if the read position is beyond offset 32, there is no need to do de-obfuscation
            if (readPosition >= ObfuscatedLength || count <= 0)
                return;

            // Find out how many bytes in the buffer are needed to be XORed
            // Note on casting:
            //      count can be safely cast to long since it is int
            //      We don't need to check for overflow of (readPosition + (long) count) since count is int and readPosition is less than
            //          ObfuscatedLength
            //      The result of (Math.Min(ObfuscatedLength, readPosition + (long) count) - readPosition) can be safely cast to int
            //          since it cannot be bigger than ObfuscatedLength which is 32
            int bytesToXor = (int) (Math.Min(ObfuscatedLength, unchecked (readPosition + (long) count)) - readPosition);

            int guidBytePosition = _guid.Length - ((int) readPosition % _guid.Length) - 1;

            for (int i = offset; bytesToXor > 0; --bytesToXor, ++i, --guidBytePosition)
            {
                // If we exhausted the Guid bytes, go back to the least significant byte
                if (guidBytePosition < 0)
                    guidBytePosition = _guid.Length - 1;

                // XOR the obfuscated byte with the appropriate byte from the Guid byte array
                buffer[i] ^= _guid[guidBytePosition];
            }
        }

        /// <summary>
        /// Returns the byte representation of guidString
        /// </summary>
        /// <remarks>
        /// We cannot use Guid.GetByteArray directly due to little or big endian issues
        /// Guid is defined as below:
        /// typedef struct _GUID
        /// {
        ///     DWORD Data1;
        ///     WORD Data2;
        ///     WORD Data3;
        ///     BYTE Data4[8];
        ///  } GUID;
        /// So, Guid.GetByteArray returns a byte array where the first 8 bytes are ordered according to a specific endian format
        /// </remarks>
        private static byte[] GetGuidByteArray(string guidString)
        {
            // Make sure we have at least on '-' since Guid constructor will take both dash'ed and non-dash'ed format of GUID string
            //  while XPS spec requires dash'ed format of GUID
            if (guidString.IndexOf('-') == -1)
            {
                throw new ArgumentException(SR.Get(SRID.InvalidPartName));
            }
            
            // Use Guid constructor to do error checking in parsing
            Guid guid = new Guid(guidString);

            // Convert the GUID into string in 32 digits format (xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx)
            string wellFormedGuidString = guid.ToString("N");

            // Now it is safe to do parsing of the well-formed GUID string
            byte[] guidBytes = new byte[16];

            // We don't need to check the length of wellFormedGuidString since it is guaranteed to be 32
            for (int i = 0; i < guidBytes.Length; i++)
            {
                guidBytes[i] = Convert.ToByte(wellFormedGuidString.Substring(i * 2, 2), 16);
            }

            return guidBytes;
        }
        
        //------------------------------------------------------
        //
        //  Private Variables
        //
        //------------------------------------------------------

        private Stream  _obfuscatedStream;        // stream we ultimately decompress from and to in the container
        private byte[] _guid;
        private bool _ownObfuscatedStream;      // Does this class own the underlying stream?
                                                                    //  if it does, it should dispose the underlying stream when this class is disposed
        private const long ObfuscatedLength = 32;
        #endregion
    }
}
