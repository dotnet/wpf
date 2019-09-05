// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
//
// Description:
//  This is an internal class that enables interactions with Zip archives
//  for OPC scenarios 
//
//
//
//

using System;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Collections;
using System.Windows;  
using MS.Internal.WindowsBase;
    
namespace MS.Internal.IO.Zip
{
    internal class ZipIOModeEnforcingStream:  Stream, IDisposable
    {
        // !!!!!!!!! Attention !!!!!!!!!!!!
        // This stream is NOT doing any data buffering by design 
        // All data buffering is done underneath, by either  
        // FileItemStream or Compression Stream    
        // This assumption is used through the code of the 
        // FileItemBlock to calculate up to data CompressedSize UncompressedSize DirtyFlag and others 
        // !!!!!!!!! Attention !!!!!!!!!!!!
        
        ////////////////////////////////////
        // Stream section  
        /////////////////////////////////
        override public bool CanRead
        {
            get
            {
                return (!_disposedFlag && 
                            _baseStream.CanRead && 
                            ((_access == FileAccess.Read) || (_access == FileAccess.ReadWrite)));
            }
        }

        override public bool CanSeek
        {
            get
            {
                return (!_disposedFlag) && (_baseStream.CanSeek);
            }
        }

        override public bool CanWrite
        {
            get
            {
                return (!_disposedFlag && 
                            _baseStream.CanWrite && 
                            ((_access == FileAccess.Write) || (_access == FileAccess.ReadWrite)));
            }
        }

        override public long Length
        {
            get
            {
                CheckDisposed();            
                long result = _baseStream.Length;

                // This can definetly lead to consuming more memory Compression stream
                // might sitch to Emulation mode
                // disabling auto flushing functionality As a part of implementing Isolated storage fallback in the Sparse MemoryStream 
                //_trackingMemoryStreamFactory.ReportTransactionComplete();

                return result;
            }
        }

        override public long Position
        {
            get
            {
                CheckDisposed();            
                return _currentStreamPosition;
            }
            set
            {
                CheckDisposed();            
                Seek(value, SeekOrigin.Begin);            
            }
        }

        public override void SetLength(long newLength)
        {
            CheckDisposed();

            if (!CanWrite)
            {
                throw new NotSupportedException(SR.Get(SRID.SetLengthNotSupported));
            }

            _baseStream.SetLength(newLength);

            if (newLength < _currentStreamPosition)
                _currentStreamPosition = newLength;

            // This can definetly lead to consuming more memory 
            // disabling auto flushing functionality As a part of implementing Isolated storage fallback in the Sparse MemoryStream 
            //_trackingMemoryStreamFactory.ReportTransactionComplete();
        }

        override public long Seek(long offset, SeekOrigin origin)
        {
            CheckDisposed();        
            long newStreamPosition = _currentStreamPosition;

            if (origin ==SeekOrigin.Begin) 
            {
                newStreamPosition  = offset;
            }   
            else if  (origin == SeekOrigin.Current)
            {
                checked{newStreamPosition  += offset;}
            }   
            else if  (origin == SeekOrigin.End) 
            {
                checked{newStreamPosition  = Length + offset;}
            }
            else
            {
                throw new ArgumentOutOfRangeException("origin");
            }

            if (newStreamPosition  < 0) 
            {
                 throw new ArgumentException(SR.Get(SRID.SeekNegative));
            }
            _currentStreamPosition = newStreamPosition;
            
            return _currentStreamPosition;
        }        

        override public int Read(byte[] buffer, int offset, int count)
        {
            CheckDisposed();

            if (!CanRead)
            {
                throw new NotSupportedException(SR.Get(SRID.ReadNotSupported));
            }

            long originalStreamPosition = _currentStreamPosition;
            int readResult;

            try
            {
                _baseStream.Seek(_currentStreamPosition, SeekOrigin.Begin);
                readResult = _baseStream.Read(buffer, offset, count);
                checked{_currentStreamPosition += readResult;}
            }
            catch
            {
                // when an exception is thrown, the stream position should remain unchanged
                _currentStreamPosition = originalStreamPosition;

                throw;
            }

            // This can definetly lead to consuming more memory (compression emulation mode)
            // disabling auto flushing functionality As a part of implementing Isolated storage fallback in the Sparse MemoryStream 
            //_trackingMemoryStreamFactory.ReportTransactionComplete();
            
            return readResult;
        }

        override public void Write(byte[] buffer, int offset, int count)
        {
            CheckDisposed();

            if (!CanWrite)
            {
                throw new NotSupportedException(SR.Get(SRID.WriteNotSupported));                
            }
            
            if (_baseStream.CanSeek)
                _baseStream.Seek(_currentStreamPosition, SeekOrigin.Begin);
            // if CanSeek() is false, we are already in the correct position by default
                
            _baseStream.Write(buffer, offset, count);
            checked{_currentStreamPosition += count;}

            // This can definetly lead to consuming more memory (compression emulation mode)
            // disabling auto flushing functionality As a part of implementing Isolated storage fallback in the Sparse MemoryStream 
            //_trackingMemoryStreamFactory.ReportTransactionComplete();
        }
        
        override public void Flush()
        {
            CheckDisposed();        
             _baseStream.Flush();  // we must be calling flush on underlying stream 
                                                // we can not do something like _blockManager.SaveStream here
                                                // as it will make impossible to push data through the stream stack 
        }  

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------
        internal bool Disposed
        {
            get
            {
                return _disposedFlag;
            }
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
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    // multiple calls are fine - just ignore them
                    if (!_disposedFlag)
                    {
                        // we do that so that the block Manager and Local File Block can make a decision whether 
                        // there are any more open streams or not 
                        _disposedFlag = true;

                        // This will enable us to make accurate decisions in regard to the number of currently opened streams 
                        _block.DeregisterExposedStream(this);                            
                        
                        // we must NOT Dispose underlying stream, as it canbe used by other opened 
                        // ZipIOModeEnforcingStream(s) and even if there no other open 
                        // ZipIOModeEnforcingStream(s) at the moment, Cleint app can ask for more later; 
                        // we can only Dispose underlying stream as a part of the Container Close/Dispose calls
#if !DEBUG
                        // Don't call Flush() in retail builds but do in debug builds to catch any
                        // logic errors.
                        if (_access == FileAccess.ReadWrite || _access == FileAccess.Write)
#endif
                        {
                            // Tell the block manager that we want to be closed
                            // the second parameter is ignored in the non-streaming cases , and it will result 
                            // in a non-streaming container level flush 
                            _blockManager.SaveStream(_block, true);
                        }
                    }
                }
            }
            finally
            {
                _baseStream = null;
                base.Dispose(disposing);
            }
        }

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Constructor - streaming and non-streaming mode
        /// </summary>
        /// <param name="baseStream"></param>
        /// <param name="access"></param>
        /// <param name="blockManager">block manager</param>
        /// <param name="block">associated block</param>
        internal ZipIOModeEnforcingStream(Stream baseStream, FileAccess access,
            ZipIOBlockManager blockManager,
            ZipIOLocalFileBlock block)
        {
            Debug.Assert(baseStream != null);

            _baseStream = baseStream;
            _access = access;
            _blockManager = blockManager;
            _block = block;
        }

        private void CheckDisposed()
        {
            if (_disposedFlag)
            {
                throw new ObjectDisposedException(null, SR.Get(SRID.StreamObjectDisposed));
            }
        }
        
       //------------------------------------------------------
        //
        //  Private Members
        //
        //------------------------------------------------------
        private Stream                          _baseStream;
        private FileAccess                      _access;

        private bool                            _disposedFlag;
        private long                            _currentStreamPosition;

        // Streaming Mode only
        private ZipIOLocalFileBlock       _block;                         // null if not in streaming mode
        private ZipIOBlockManager      _blockManager;                  // null if not in streaming mode
    }
}
