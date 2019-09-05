// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
//
// Description:
//  This is an internal stream class that calcuates CRC values progressively
//      if possible
//


using System;
using System.Diagnostics;
using System.IO;
using System.Windows;               // For Exception strings - SRID

using MS.Internal.IO.Zip;
using MS.Internal.WindowsBase;

namespace MS.Internal.IO.Zip
{
    internal class ProgressiveCrcCalculatingStream:  Stream
    {
        ////////////////////////////////////
        // Stream section  
        /////////////////////////////////
        override public bool CanRead
        {
            get
            {
                return (_underlyingStream != null && _underlyingStream.CanRead);
            }
        }

        override public bool CanSeek
        {
            get
            {
                return (_underlyingStream != null && _underlyingStream.CanSeek);
            }
        }

        override public bool CanWrite
        {
            get
            {
                return (_underlyingStream != null && _underlyingStream.CanWrite);
            }
        }

        override public long Length
        {
            get
            {
                CheckDisposed();

                return  _underlyingStream.Length;
            }
        }

        override public long Position
        {
            get
            {
                CheckDisposed();
                
                return _underlyingStream.Position;
            }
            
            set
            {
                CheckDisposed();

                _underlyingStream.Position = value;
            }
        }

        public override void SetLength(long newLength)
        {
            CheckDisposed(); 
            
            if (newLength < 0)
            {
                throw new ArgumentOutOfRangeException("newLength");
            }

            if (newLength < _highWaterMark)
            {
                _highWaterMark = -1;
            }

            // We don't do any check if newLength == current length here
            //  this normally should result in no-op, but this will complicate
            //  the logic due to the need of caching the underlying stream length
            // Not doing this check here might result in CRC check being skipped

            _underlyingStream.SetLength(newLength);
            // Setting a new length is the same as write operation
            // CRC cannot be checked against the to-be-validated CRC anymore
            _validateCrcWithExpectedCrc = false;

            // mark the global dirty flag
            _blockManager.DirtyFlag = true;
        }

        override public long Seek(long offset, SeekOrigin origin)
        {
            CheckDisposed();

            return _underlyingStream.Seek(offset, origin);
        }        

        override public int Read(byte[] buffer, int offset, int count)
        {
            CheckDisposed();

            int readCount = 0;

            // We should calculate CRC accumulatively for the following conditions
            // 1. Seek is not supported by the underlying stream: this will be the case for
            //      writing stream in streaming mode
            // 2. This write request is consequtive to the highwater mark of the CRC calculation
            // 3. This write request is at 0 offset and the CRC hasn't been calculated yet
            if (!_underlyingStream.CanSeek)                   // Case #1
            {
                readCount = _underlyingStream.Read(buffer, offset, count);
                CrcCalculator.Accumulate(buffer, offset, readCount);
            }
            else
            {
                long originalPosition = _underlyingStream.Position;

                readCount = _underlyingStream.Read(buffer, offset, count);

                // This operation needs to be done after Read since read can throw an exception; in that case
                //  we want to preserve the original CRC
                if (originalPosition == 0 && _highWaterMark == -1)
                {
                    _highWaterMark = 0;
                    CrcCalculator.ClearCrc();
                }

                if (originalPosition == _highWaterMark)
                {
                    CrcCalculator.Accumulate(buffer, offset, readCount);
                    _highWaterMark = _underlyingStream.Position;
                }

                if (_validateCrcWithExpectedCrc && CanValidateCrcWithoutRead())
                {
                    if (CrcCalculator.Crc != _expectedCrc)
                    {
                        throw new FileFormatException(SR.Get(SRID.CorruptedData));
                    }
                }
            }

            return readCount;
        }

        override public void Write(byte[] buffer, int offset, int count)
        {
            CheckDisposed();

            // We should calculate CRC accumulatively for the following conditions
            // 1. Seek is not supported by the underlying stream: this will be the case for
            //      writing stream in streaming mode
            // 2. This write request is consequtive to the highwater mark of the CRC calculation
            // 3. This write request is at 0 offset and the CRC hasn't been calculated yet
            if (!_underlyingStream.CanSeek)                   // Case #1
            {
                _underlyingStream.Write(buffer, offset, count);
                CrcCalculator.Accumulate(buffer, offset, count);
            }
            else
            {
                long originalPosition = _underlyingStream.Position;

                // If we ever fail to Write below _highWaterMark, we want CRC to be recalculated in case
                //  if a caller decides to recover from the error
                if (originalPosition < _highWaterMark)
                {
                    _highWaterMark = -1;
                }

                _underlyingStream.Write(buffer, offset, count);

                if (originalPosition == 0)
                {
                    _highWaterMark = 0;
                    CrcCalculator.ClearCrc();
}

                if (originalPosition == _highWaterMark)
                {
                    CrcCalculator.Accumulate(buffer, offset, count);
                    _highWaterMark = _underlyingStream.Position;
                }
            }

            // CRC cannot be checked against the to-be-validated CRC anymore
            _validateCrcWithExpectedCrc = false;

            // mark the global dirty flag
            _blockManager.DirtyFlag = true;
        }

        override public void Flush()
        {
            CheckDisposed();

            _underlyingStream.Flush();
        }

        /////////////////////////////
        // Internal Constructor
        /////////////////////////////        
        internal  ProgressiveCrcCalculatingStream(ZipIOBlockManager blockManager, Stream underlyingStream) :
            this(blockManager, underlyingStream, 0)
        {
            _validateCrcWithExpectedCrc = false;
        }

        internal  ProgressiveCrcCalculatingStream(ZipIOBlockManager blockManager, Stream underlyingStream, UInt32 expectedCrc) 
        {
            Debug.Assert(underlyingStream != null);
            Debug.Assert(blockManager != null);

            _blockManager = blockManager;
            _underlyingStream = underlyingStream;
            _validateCrcWithExpectedCrc = true;
            _expectedCrc = expectedCrc;
            _highWaterMark = -1;
        }

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
                    //streams wrapping this stream shouldn't pass Dipose calls through 
                    // it is responsibility of the BlockManager or LocalFileBlock (in case of Remove) to call 
                    // this dispose as appropriate (that is the reason why Flush isn't called here)

                    // multiple calls are fine - just ignore them
                    // and we shouldn't be closing a stream which we do not own 
                    _underlyingStream = null;   
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        /////////////////////////////
        // Internal Methods
        /////////////////////////////        

            
        // !!!!!!!!!!!!!!!!IMPORTANT !!!!!!!!!!!!!!!!
        // This method doesn't preserve the seek position of the underlying stream. 
        // It (non-preservation of the seek pointer) is mostly done for compression 
        // scenarios in which seeking back in the compressed streams will result in 
        // switching to the expensive simulation stream. This method is only called 
        // from scenarios during Flush Close of the package where position of the 
        // Compressed stream is insignificant 
        internal UInt32 CalculateCrc()
        {
            CheckDisposed();

            if (_underlyingStream.CanSeek)
            {
                long originalPosition = _underlyingStream.Position;

                if (_highWaterMark == -1)
                {
                    CrcCalculator.ClearCrc();
                    _highWaterMark = 0;
                }

                if (_highWaterMark < _underlyingStream.Length)
                {
                    _underlyingStream.Position = _highWaterMark;
                    CrcCalculator.CalculateStreamCrc(_underlyingStream);
                    _highWaterMark = _underlyingStream.Length;
                }
            }

            return CrcCalculator.Crc;
        }

        /////////////////////////////
        // Private Methods
        /////////////////////////////        

        private void CheckDisposed()
        {
            if (_underlyingStream == null)
            {
                throw new ObjectDisposedException(null, SR.Get(SRID.StreamObjectDisposed));            
            }
        }

        private Crc32Calculator CrcCalculator
        {
            get
            {
                if (_crcCalculator == null)
                {
                    _crcCalculator = new Crc32Calculator();
                }
                return _crcCalculator;
            }
        }
        
        private bool CanValidateCrcWithoutRead()
        {
            if (_underlyingStream.CanSeek && _highWaterMark == _underlyingStream.Length)
            {
                return true;
            }

            return false;
        }

        // this is only used to switch the dirty flag in case of Write or SetLength
        // no other communication is done with the BlockManager from this class
        private ZipIOBlockManager _blockManager;
    
        private long _highWaterMark;
        private Crc32Calculator _crcCalculator;
        private bool _validateCrcWithExpectedCrc;
        private UInt32 _expectedCrc;

        private Stream _underlyingStream;
    }
}

