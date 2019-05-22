// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  Emulates a fully functional stream that persists using the Deflate compression algorithm
//
//  This class provides a fully functional Stream on a restricted functionality compression
//  stream (System.IO.Compression.DeflateStream).
//
//  CompressStream operates in "transparent" mode (ReadThrough or WriteThrough) as long as possible for efficiency,
//  reverting to full emulation mode as required to satisfy Stream requests that would violate the capabilities
//  of the DeflateStream that actually does the reading or writing (decompress or compress).  Emulation
//  mode is implemented by class CompressEmulationStream.
//
//  Note that the reason we need these modes is that DeflateStream is entirely modal in nature once
//  constructed.  If it is created in "compress" mode, it can only be used for compression.  If it is
//  opened in "decompress" mode, it can only be used for decompression.  This means that Reading is only
//  natively support in decompress mode, and writing is only natively supported in compress mode.
//
// Notes:
//  If baseStream is non-seekable and non-readable it is not possible to enter Emulation mode.  In this case
//  we need to throw appropriate exception.
//
//
//

using System;
using System.IO;
using System.IO.Compression;                // for DeflateStream
using System.Diagnostics;

using System.IO.Packaging;
using MS.Internal.IO.Zip;

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
    /// Emulates a fully functional stream that persists using the Deflate compression algorithm
    /// </summary>
    /// <remarks>Attempts to provide ReadThrough or WriteThrough functionality as possible.  If not possible, 
    /// a CompressEmulationStream is created and work is delegated to that class.</remarks>
    internal class CompressStream : Stream
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
        /// The underlying stream, expected to be a DeflateStream or a CompressEmulationStream,
        /// is in charge of leaving the IO position unchanged in case of an exception.
        /// </remarks>
        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckDisposed();

            PackagingUtilities.VerifyStreamReadArgs(this, buffer, offset, count);

            // no-op
            if (count == 0)
                return 0;

            checked     // catch any integer overflows
            {
                switch (_mode)
                {
                    case Mode.Start:    
                        {
                            // skip to the correct logical position if necessary (DeflateStream starts at position zero)
                            if (_position == 0)
                            {
                                // enter ReadPassThrough mode if it is efficient
                                ChangeMode(Mode.ReadPassThrough);
                            }
                            else
                                ChangeMode(Mode.Emulation);

                            break;
                        }

                    case Mode.ReadPassThrough:  // continue in ReadPassThrough mode
                    case Mode.Emulation:        // continue to read from existing emulation stream
                        {
                            break;
                        }

                    case Mode.WritePassThrough: // enter Emulation mode
                        {
                            // optimization - if they are trying to jump back to the start to read, simply jump to ReadPassThrough mode
                            if (_position == 0)
                                ChangeMode(Mode.ReadPassThrough);
                            else
                                ChangeMode(Mode.Emulation);
                            break;
                        }
                    default: Debug.Assert(false, "Illegal state for CompressStream - logic error"); break;
                }

                // we might be in Start mode now if we are beyond the end of stream - just return zero
                if (_current == null)
                    return 0;

                int bytesRead = _current.Read(buffer, offset, count);

                // optimization for ReadPassThrough mode - we actually know the length because we ran out of bytes
                if (_mode == Mode.ReadPassThrough && bytesRead == 0)
                {
                    // possible first chance to set and verify length from header against real data length
                    UpdateUncompressedDataLength(_position);

                    // since we've exhausted the deflateStream, discard it to reduce working set
                    ChangeMode(Mode.Start);
                }

                // Stream contract - don't update position until we are certain that no exceptions have occurred 
                _position += bytesRead;

                return bytesRead;
            }
        }

        /// <summary>
        /// Write
        /// </summary>
        /// <remarks>Note that zero length write to deflate stream actually results in a stream containing 2 bytes.  This is
        /// required to maintain compatibility with the standard.</remarks>
        public override void Write(byte[] buffer, int offset, int count)
        {
            CheckDisposed();

            PackagingUtilities.VerifyStreamWriteArgs(this, buffer, offset, count);

            // no-op
            if (count == 0)
                return;

            checked
            {
                switch (_mode)
                {
                    case Mode.Start:             // enter WritePassThrough mode if possible
                        {
                            // Special case: If stream has existing content, we need to go straight
                            // to Emulation mode otherwise we'll potentially destroy existing data.
                            // Don't bother entering WritePassThroughMode if position is non-zero because
                            // we'll just enter emulation later.
                            if (_position == 0 && IsDeflateStreamEmpty(_baseStream))
                                ChangeMode(Mode.WritePassThrough);
                            else
                                ChangeMode(Mode.Emulation);
                            break;
                        }
                    case Mode.WritePassThrough: // continue in Write mode
                    case Mode.Emulation:        // continue to read from existing emulation stream
                        {
                            break;
                        }
                    case Mode.ReadPassThrough:  // enter Emulation mode
                        {
                            ChangeMode(Mode.Emulation); break;
                        }
                    default: Debug.Assert(false, "Illegal state for CompressStream - logic error"); break;
                }

                _current.Write(buffer, offset, count);

                _position += count;
            }

            // keep track of the current length in case someone asks for it
            if (_mode == Mode.WritePassThrough)
                CachedLength = _position;

            _dirtyForFlushing= true;
            _dirtyForClosing= true;
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

            if (!CanSeek)
                throw new NotSupportedException(SR.Get(SRID.SeekNotSupported));

            checked
            {
                // Calculate newPos
                // If origin is Begin or Current newPos can be calculated without knowing
                // the stream length.  If origin is End, switch to Emulation immediately.
                long newPos = -1;
                switch (origin)
                {
                    case SeekOrigin.Begin: newPos = offset; break;
                    case SeekOrigin.Current: newPos = _position + offset; break;
                    case SeekOrigin.End:
                        ChangeMode(Mode.Emulation);     // has no effect if already in Emulation mode
                        newPos = Length + offset;       // Length is now legal to call
                        break;
                }

                // we have a reliable newPos now - throw if its illegal
                if (newPos < 0)
                    throw new ArgumentException(SR.Get(SRID.SeekNegative));

                // is the new position any different than the current position?
                long delta = newPos - _position;
                if (delta == 0)
                    return _position;

                // We optimize for very restricted case - short seek forward in read-only mode.
                // This prevents the expense of entering Emulation mode when a stream reader is 
                // skipping a few bytes while parsing binary data structures (for example).
                if ((delta > 0) && (delta < _readPassThroughModeSeekThreshold) 
                    && (_mode == Mode.ReadPassThrough))
                {
                    // We're able to fake the seek by reading in this one corner case.
                    // We cannot be in ReadPassThroughMode if currently beyond end of physical
                    // data so it is safe to assume that the value returned from
                    // this call represents real data.
                    long bytesNotRead = ReadPassThroughModeSeek(delta);
                    if (bytesNotRead > 0)
                    {
                        // Stream was exhausted - seek was beyond end of physical
                        // stream so we need to update our cachedLength and
                        // move to Start mode.
                        UpdateUncompressedDataLength(newPos - bytesNotRead);
                        ChangeMode(Mode.Start);
                    }
                }
                else
                {
                    // Enter Emulation for efficiency
                    ChangeMode(Mode.Emulation);     // No-op if already in Emulation
                    _current.Position = newPos;     // Update to new value
                }

                // update logical position
                _position = newPos; 
            }

            return _position;
        }

        /// <summary>
        /// SetLength
        /// </summary>
        public override void SetLength(long newLength)
        {
            CheckDisposed();

            if (!CanSeek)
                throw new NotSupportedException(SR.Get(SRID.SetLengthNotSupported));

            _lengthVerified = true;         // no longer need to verify our length against our constructor value
            switch (_mode)
            {
                case Mode.Start:
                case Mode.WritePassThrough:
                case Mode.ReadPassThrough:
                {
                    // optimize for "clear the whole stream" - no need to enter emulation
                    if (newLength == 0)
                    {
                        ChangeMode(Mode.Start);     // discard any existing deflate stream
                        _baseStream.SetLength(0);   // clear the underlying stream
                        UpdateUncompressedDataLength(newLength);              
                    }
                    else
                        ChangeMode(Mode.Emulation);

                    break;
                }

                case Mode.Emulation: break;

                default: Debug.Assert(false, "Illegal state for CompressStream - logic error"); break;
            }

            if (_mode == Mode.Emulation)
                _current.SetLength(newLength);

            // position seek pointer appropriately
            if (newLength < _position)
                Seek(newLength, SeekOrigin.Begin);

            // still need to mark ourselves dirty so that our caller can get the correct result
            // when they query the IsDirty property
            _dirtyForFlushing= true;
            _dirtyForClosing= true;
        }

        /// <summary>
        /// Flush
        /// </summary>
        /// <remarks>Flushes to stream (if necessary)</remarks>
        public override void Flush()
        {
            CheckDisposed();

            // Always pass through to subordinates because they may be caching things (ignore _dirty flag here).

            // Current must be non-null if changes have been made.
            if (_current != null)
            {
                _current.Flush();
                _dirtyForFlushing = false; // extra flushes after this will not produce more data 

                // avoid clearing flag when we are empty because it would prevent generation
                // of the 2-byte sequence on dispose
                if ((_mode == Mode.Emulation) && (Length != 0))
                {
                    _dirtyForClosing = false; // if it is ReadThrough or Start (it shouldn't be dirty in the first place)
                                        // if it is WriteThrough it is going to be dirty untill it is closed 
                }
            }
            _baseStream.Flush();
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
                return _position;
            }
            set
            {
                CheckDisposed();

                // convert to a Seek so we don't have to replicate the Seek logic here
                Seek(checked(value - _position), SeekOrigin.Current);
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

//               if (!CanSeek)
//                    throw new NotSupportedException(SR.Get(SRID.LengthNotSupported));

                switch (_mode)
                {
                    case Mode.Start:
                    case Mode.WritePassThrough:
                    case Mode.ReadPassThrough:
                    {
                        // use cached length if possible
                        if (CachedLength >= 0)
                            return CachedLength;
                        else
                        {
                            // Special optimization for new/empty streams - avoid entering Emulation as long as possible.
                            if (_position == 0 && IsDeflateStreamEmpty(_baseStream))
                                return 0;

                            ChangeMode(Mode.Emulation);
                        }
                        break;
                    }

                    case Mode.Emulation: break;

                    default: Debug.Assert(false, "Illegal state for CompressStream - logic error"); break;
                }

                // must be in Emulation mode to get here
                // possible first chance to verify length from header against real data length
                UpdateUncompressedDataLength(_current.Length);
                return _current.Length;
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
                return (_mode != Mode.Disposed) && _baseStream.CanRead;
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
                // cannot seek on a close stream, but don't throw if asked
                return (_mode != Mode.Disposed) && _baseStream.CanSeek;
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
                // cannot write to a close stream, but don't throw if asked
                return (_mode != Mode.Disposed) && _baseStream.CanWrite;
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
        /// <param name="length">uncompressed length if known, or -1 if not known</param>
        /// <param name="baseStream">part stream</param>
        internal CompressStream(Stream baseStream, long length) : this (baseStream, length, false)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="baseStream">part stream</param>
        /// <param name="creating">new stream or not?</param>
        /// <param name="length">uncompressed length if known, or -1 if not known</param>
        internal CompressStream(Stream baseStream, long length, bool creating) 
        {
            if (baseStream == null)
                throw new ArgumentNullException("baseStream");

            if (length < -1)
                throw new ArgumentOutOfRangeException("length");

            _baseStream = baseStream;
            _cachedLength = length;

            Debug.Assert(_baseStream.Position == 0,
                "Our logic assumes position zero and we don't seek because sometimes it's not supported");

            // we need to be dirty if this is a new stream because an empty deflate 
            // stream actually causes a write (this happens only on close ); therefore 
            // we are dirty for close (in case of creation) and not dirty for flush 
            _dirtyForFlushing= false; 
            _dirtyForClosing= creating;
        }

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------
        /// <summary>
        /// IsDirty
        /// </summary>
        /// <value></value>
        internal bool IsDirty(bool closingFlag)
        {
            return closingFlag ? _dirtyForClosing : _dirtyForFlushing;
        }

        /// <summary>
        /// IsDisposed
        /// </summary>
        /// <value></value>
        internal bool IsDisposed
        {
            get
            {
                return (_mode == Mode.Disposed);
            }
        }

        #endregion

        #region Protected
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
                    if (_mode != Mode.Disposed)
                    {
                        Flush();

                        if (_current != null)
                        {
                            _current.Close();     // call Dispose()
                            _current = null;
                        }

                        // Special handling for "empty" deflated streams - they actually persist
                        // a 2 byte sequence.

                        // Three separate cases (assuming the stream is dirty):
                        // 1) Stream is seekable - check Length and write the 2-byte sequence
                        //    if the stream is empty.
                        // 2) Stream is non-seekable and negative CachedLength - this means we were created 
                        //    (not opened) and there have been no writes so we need the 2-byte sequence.
                        // 3) Stream is non-seekable and zero CachedLength - this means we are
                        //    really zero-bytes long which indicates we need the 2-byte sequence.
                        if (_dirtyForClosing && ((_baseStream.CanSeek && _baseStream.Length == 0) ||
                            (_cachedLength <= 0)))
                        {
                            _baseStream.Write(_emptyDeflateStreamConstant, 0, 2);
                            _baseStream.Flush();
                        }

                        // _baseStream.Close();     // never close a stream we do not own
                        _baseStream = null;

                        ChangeMode(Mode.Disposed);
                        _dirtyForClosing = false;                        
                        _dirtyForFlushing = false;
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
        #endregion

        // Changed the mode from Emulation to Start
        internal void Reset()
        {
            CheckDisposed();

            ChangeMode(Mode.Start);
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
        /// Verify Uncompressed length from data against what we were given in the constructor
        /// </summary>
        /// <param name="dataLength"></param>
        /// <remarks>verify length from header against real data length</remarks>
        private void UpdateUncompressedDataLength(long dataLength)
        {
            Debug.Assert(dataLength >= 0);

            // only compare if we have a value
            if (_cachedLength >= 0)
            {
                if (!_lengthVerified)
                {
                    if (_cachedLength != dataLength)
                        throw new FileFormatException(SR.Get(SRID.CompressLengthMismatch));

                    _lengthVerified = true;
                }
            }

            _cachedLength = dataLength;     // always set
        }

        /// <summary>
        /// Helper method to reduce complexity in the public Seek method
        /// </summary>
        /// <param name="bytesToSeek"></param>
        /// <returns>bytes remaining - will be non-zero if stream was exhausted</returns>
        /// <remarks>Attempts to "seek" by reading an discarding bytes using the current
        /// Decompressing DeflateStream.
        /// _position is updated by our caller - this function does not change it</remarks>
        private long ReadPassThroughModeSeek(long bytesToSeek)
        {
            checked
            {
                Debug.Assert(bytesToSeek > 0, "Logic Error - bytesToSeek should be positive");

                // allocate buffer just big enough for the seek, maximum of 4k
                byte[] buf = new byte[Math.Min(0x1000, bytesToSeek)];

                // read to simulate Seek
                while (bytesToSeek > 0)
                {
                    // don't exceed the buffer size
                    long n = Math.Min(bytesToSeek, buf.Length);
                    n = _current.Read(buf, 0, (int)n);

                    // seek beyond end of stream is legal
                    if (n == 0)
                    {
                        break;  // just exit
                    }

                    bytesToSeek -= n;
                }

                // return bytes not read
                return bytesToSeek;
            }
        }

        /// <summary>
        /// Call this before accepting any public API call (except some Stream calls that
        /// are allowed to respond even when Closed
        /// </summary>
        private void CheckDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(null, SR.Get(SRID.StreamObjectDisposed));
        }

        /// <summary>
        /// ChangeMode
        /// </summary>
        /// <param name="newMode"></param>
        /// <remarks>Does not update Position of _current for change to ReadPassThroughMode.</remarks>
        private void ChangeMode(Mode newMode)
        {
            // ignore redundant calls (allowing these actually simplifies the logic in SetLength)
            if (newMode == _mode)
                return;

            // every state change requires this logic
            if (_current != null)
            {
                _current.Close();
                _dirtyForClosing = false;                        
                _dirtyForFlushing = false;
            }

            // set the new mode - must be done before the call to Seek
            _mode = newMode;

            switch (newMode)
            {
                case Mode.Start:
                    {
                        _current = null;
                        _baseStream.Position = 0;
                        break;
                    }

                case Mode.ReadPassThrough:
                case Mode.WritePassThrough:
                    {
                        Debug.Assert(_baseStream.Position == 0);

                        // create the appropriate DeflateStream
                        _current = new DeflateStream(_baseStream, 
                            newMode == Mode.WritePassThrough ? CompressionMode.Compress : CompressionMode.Decompress, 
                            true);

                        break;
                    }
                case Mode.Emulation:
                    {
                        // Create emulation stream.  Use a MemoryStream for local caching.
                        // Do not change this logic for RM cases because the data is "in the clear" and must
                        // not be persisted in a vulnerable location.
                       
                        SparseMemoryStream memStream = new SparseMemoryStream(_lowWaterMark, _highWaterMark);
                        _current = new CompressEmulationStream(_baseStream, memStream, _position, new DeflateEmulationTransform());

                        // verify and set length
                        UpdateUncompressedDataLength(_current.Length);
                        break;
                    }
                case Mode.Disposed: break;
                default:
                    Debug.Assert(false, "Illegal state for CompressStream - logic error"); break;
            }
        }

        /// <summary>
        /// Call this to determine if a deflate stream is empty - pass the actual compressed stream
        /// </summary>
        /// <param name="s"></param>
        /// <returns>true if empty</returns>
        private static bool IsDeflateStreamEmpty(Stream s)
        {
            bool empty = false;

            // Special case: If stream has existing content, we need to go straight
            // to Emulation mode otherwise we'll potentially destroy existing data.
            // This will not be possible if the base stream is write-only and non-seekable.
            // The minimal length of a persisted DeflateStream is 2 so if the length
            // is 2, we can safely overwrite.  We explicitly call Deflate on a stream of length
            // 1 so that we can get a consistent exception because this will be an illegally 
            // compressed stream.
            if (s.CanSeek && s.CanRead)
            {
                Debug.Assert(s.Position == 0);

                // read the two bytes and commpare to the known 2 bytes that represent
                // and empty deflate stream
                byte[] buf = new byte[2];
                int bytesRead = s.Read(buf, 0, 2);
                empty = ((bytesRead == 0) || 
                    (buf[0] == _emptyDeflateStreamConstant[0] && buf[1] == _emptyDeflateStreamConstant[1]));

                s.Position = 0;     // restore position
            }
            else
                empty = true;       //  if write-time-streaming we're going to destroy what's there anyway

            return empty;
        }

        private long CachedLength
        {
            get
            {
                // only maintained when NOT in Emulation mode
                Debug.Assert(_mode != Mode.Emulation, "Logic error: CachedLength not maintained in Emulation mode - illegal Get");
                return _cachedLength;
            }
            set
            {
                // only maintained when NOT in Emulation mode
                Debug.Assert(_mode != Mode.Emulation, "Logic error: CachedLength not maintained in Emulation mode - illegal Set");
                Debug.Assert(value >= 0, "Length cannot be negative - logic error?");
                _cachedLength = value;
            }
        }

        //------------------------------------------------------
        //
        //  Private Variables
        //
        //------------------------------------------------------

        // Add explicit values to these enum variables because we do some arithmetic with them and don't want to 
        // rely on the default behavior.
        private enum Mode 
        { 
            Start = 0,                      // we have no outstanding memory in use - state on construction
            ReadPassThrough = 1,            // we are able to read from the current position
            WritePassThrough = 2,           // we are able to write to the current position
            Emulation = 3,                  // we have moved all data to a memory stream and all operations are supported
            Disposed = 4                    // we are disposed
        };
        private Mode    _mode;              // current stream mode
        private Int64   _position;          // current logical position - only copy - shared with all helpers
        private Stream  _baseStream;        // stream we ultimately decompress from and to in the container
        private Stream  _current;           // current stream object

        private bool    _dirtyForFlushing;             // are we dirty, these 2 flags are going to differ in the case of the FLushed Write Through mode
        private bool    _dirtyForClosing;             // _dirtyForFlushing will be false (meaning that there is no data to be flushed) while
                                                                        // _dirtyForClosing will be true as there might be some data that need to be added for closing 
                                                                        // Note: DirtyForFlushing can never be true when DirtyForClosing is false.

        private bool    _lengthVerified;    // true if we have successfully compared the length given in our constructor against that obtained from
                                            // actually decompressing the data
        private long    _cachedLength;      // cached value prevents us from entering Emulation to obtain length after ReadPassThrough reads all bytes
                                            // -1 means not set
        // this is what is persisted when a deflate stream is of length zero
        private static byte[] _emptyDeflateStreamConstant = new byte[] { 0x03, 0x00 };

        private const long _lowWaterMark = 0x19000;                     // we definately would like to keep everythuing under 100 KB in memory  
        private const long _highWaterMark = 0xA00000;                   // we would like to keep everything over 10 MB on disk
        private const long _readPassThroughModeSeekThreshold = 0x40;    // amount we can seek in a reasonable amount of time while decompressing                                    

        #endregion
    }
}
