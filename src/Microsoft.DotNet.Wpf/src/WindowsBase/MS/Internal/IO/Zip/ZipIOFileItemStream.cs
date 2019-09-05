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
using System.Runtime.Serialization;
using System.Windows;
using MS.Internal.IO.Packaging;         // for PackagingUtilities
using MS.Internal.WindowsBase;
    
namespace MS.Internal.IO.Zip
{
    internal class ZipIOFileItemStream :  Stream
    {
        ////////////////////////////////////
        // Stream section  
        /////////////////////////////////
        override public bool CanRead
        {
            get
            {
                return (!_disposedFlag) && (_blockManager.Stream.CanRead);
            }
        }

        override public bool CanSeek
        {
            get
            {
                return (!_disposedFlag) && (_blockManager.Stream.CanSeek);
            }
        }

        override public bool CanWrite
        {
            get
            {
                return (!_disposedFlag) && (_blockManager.Stream.CanWrite);
            }
        }

        override public long Length
        {
            get
            {
                CheckDisposed();            

                return  _currentStreamLength;
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

            Debug.Assert(_cachePrefixStream == null); // we only expect this thing to be not null during Archive Save execution
                                                                                // that would between PreSaveNotofication call and Save SaveStreaming
                
            if (newLength < 0)
            {
                throw new ArgumentOutOfRangeException("newLength");
            }

            if (_currentStreamLength != newLength)
            {
                _dirtyFlag = true;
                _dataChanged = true;

                if  (newLength <= _persistedSize)
                {
                    // the stream becomes smaller than our disk block, which means that  
                    // we can drop the in-memory-sparse-suffix 
                    if (_sparseMemoryStreamSuffix  != null)
                    {
                        _sparseMemoryStreamSuffix.Close();
                        _sparseMemoryStreamSuffix  = null;                
                    }
                }
                else
                {
                    // we need to construct Sparse Memory stream if we do not have one yet 
                    if (_sparseMemoryStreamSuffix  == null)
                    {
                        _sparseMemoryStreamSuffix  = new SparseMemoryStream(_lowWaterMark, _highWaterMark);
                    }

                    // set size on the Sparse Memory Stream 
                    _sparseMemoryStreamSuffix.SetLength(newLength - _persistedSize); // no need for checked as it was verified above 
                }

                _currentStreamLength = newLength;

                // if stream was truncated to the point that our current position is beyond the end of the stream,
                // we need to reset position so it is at the end of the stream 
                if (_currentStreamLength < _currentStreamPosition)
                    Seek(_currentStreamLength, SeekOrigin.Begin);
            }
        }

        override public long Seek(long offset, SeekOrigin origin)
        {
            CheckDisposed();      

            Debug.Assert(_cachePrefixStream == null); // we only expect this thing to be not null during Archive Save execution
                                                                                // that would between PreSaveNotofication call and Save SaveStreaming
            
            long newStreamPosition = _currentStreamPosition;
            
            if (origin ==SeekOrigin.Begin) 
            {
                newStreamPosition = offset;
            }   
            else if  (origin == SeekOrigin.Current)
            {
                checked{newStreamPosition += offset;}
            }   
            else if  (origin == SeekOrigin.End) 
            {
                checked{newStreamPosition = _currentStreamLength + offset;}
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

            PackagingUtilities.VerifyStreamReadArgs(this, buffer, offset, count);

            Debug.Assert(_cachePrefixStream == null); // we only expect this thing to be not null during Archive Save execution
                                                      // that would between PreSaveNotofication call and Save SaveStreaming

            Debug.Assert(_currentStreamPosition >= 0);

            if (count == 0)
            {
                return 0; 
            }

            if (_currentStreamLength <= _currentStreamPosition)
            {
                // we are past the end of the stream so let's just return 0
                return 0; 
            }

            int totalBytesRead;
            int diskBytesRead = 0;
            int diskBytesToRead = 0;
            long persistedTailSize = 0;

            int memoryBytesRead = 0;
            long newStreamPosition = _currentStreamPosition;

            checked
            {
                // Try to satisfy request with the Read from the Disk 
                if  (newStreamPosition < _persistedSize)
                {
                    // we have at least partial overlap between request and the data on disk 

                    //first let's get min between size of the stream's tail and the tail of the persisted chunk
                    // in some cases stream might be smaller 
                    // e.g. _currentStreamLength  < _persistedSize, if let's say stream was truncated
                    persistedTailSize = Math.Min(_currentStreamLength, _persistedSize) - newStreamPosition;
                    Debug.Assert(persistedTailSize > 0);

                    // we also do not want to read more data than was requested by the user
                    diskBytesToRead = (int)Math.Min((long)count, persistedTailSize); // this is a safe cast as count has int type 
                    Debug.Assert(diskBytesToRead > 0);
                    
                    // and now we can actually read it 
                    _blockManager.Stream.Seek(_persistedOffset + newStreamPosition, SeekOrigin.Begin);

                    // we are ready for getting fewer bytes than reqested 
                    diskBytesRead = _blockManager.Stream.Read(buffer, offset, diskBytesToRead);

                    newStreamPosition += diskBytesRead;
                     count -= diskBytesRead;
                     offset +=diskBytesRead;

                    if (diskBytesRead  <  diskBytesToRead)
                    {
                        // we didn't everything that we hae asked for. In such case we shouldn't 
                        // try to get data from the   _sparseMemoryStreamSuffix  
                        _currentStreamPosition = newStreamPosition;

                        return diskBytesRead;
                    }
                }

                // check whether we need to get data from the memory Stream;
                if  ((_sparseMemoryStreamSuffix  != null) && (newStreamPosition + count > _persistedSize))
                {
                    // we are either trying to finish the request partially satisfied by the 
                    // on disk data  or  the read is entirely within the suffix 
                     _sparseMemoryStreamSuffix.Seek(newStreamPosition - _persistedSize, SeekOrigin.Begin);
                    memoryBytesRead = _sparseMemoryStreamSuffix.Read(buffer, offset, count);
                    
                    newStreamPosition += memoryBytesRead;
                }
     
                totalBytesRead = diskBytesRead + memoryBytesRead;
            }

            _currentStreamPosition = newStreamPosition;
            return totalBytesRead ;
        }

        /// <summary>
        /// Write
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <remarks>In streaming mode, write should accumulate data into the SparseMemoryStream.</remarks>
        override public void Write(byte[] buffer, int offset, int count)
        {
            CheckDisposed();   

            PackagingUtilities.VerifyStreamWriteArgs(this, buffer, offset, count);

            Debug.Assert(_cachePrefixStream == null); // we only expect this thing to be not null during Archive Save execution
                                                      // that would between PreSaveNotofication call and Save SaveStreaming

            Debug.Assert(_currentStreamPosition >= 0);

            if (count == 0)
            {
                return; 
            }
        
            int diskBytesToWrite = 0;
    
            _dirtyFlag = true;
            _dataChanged = true;
            long newStreamPosition = _currentStreamPosition;
            checked
            {
                // Try to satisfy request with the Write to the Disk 
                if  (newStreamPosition  < _persistedSize)
                {
                    Debug.Assert(!_blockManager.Streaming);

                    // we have at least partial overlap between request and the data on disk 
                    _blockManager.Stream.Seek(_persistedOffset + newStreamPosition, SeekOrigin.Begin);
                    // Note on casting:
                    //  It is safe to cast the result of Math.Min(count, _persistedSize - newStreamPosition))
                    //      from long to int since it cannot be bigger than count and count is int type
                    diskBytesToWrite = (int) (Math.Min(count, _persistedSize - newStreamPosition));  // this is a safe cast as count has int type 

                    _blockManager.Stream.Write(buffer, offset, diskBytesToWrite);
                    newStreamPosition += diskBytesToWrite;
                    count -= diskBytesToWrite;
                    offset += diskBytesToWrite;
                }

                // check whether we need to save data to the memory Stream;
                if  (newStreamPosition + count > _persistedSize)
                {
                    if (_sparseMemoryStreamSuffix  == null)
                    {
                         _sparseMemoryStreamSuffix  = new SparseMemoryStream(_lowWaterMark, _highWaterMark);
                    }
                    
                    _sparseMemoryStreamSuffix.Seek(newStreamPosition - _persistedSize, SeekOrigin.Begin);

                    _sparseMemoryStreamSuffix.Write(buffer, offset, count);
                    newStreamPosition += count;
                }

                _currentStreamPosition = newStreamPosition;
                _currentStreamLength = Math.Max(_currentStreamLength, _currentStreamPosition);                                 
            }
            return;
        }
        
        override public void Flush()
        {
            CheckDisposed();

            // tell the BlockManager that the caller called Flush on us. Block manager will process this 
            // and possibly call us back on Save or SaveStreaming 
            _blockManager.SaveStream(_block, false);  // second parameter is a closing indicator 
        }  

        /////////////////////////////
        // Internal Constructor
        /////////////////////////////        
        internal  ZipIOFileItemStream(ZipIOBlockManager blockManager,   // blockManager is only needed 
                                                                        // to pass through to it Flush requests 
                                      ZipIOLocalFileBlock block,        // our owning block - needed for Streaming scenarios
                                      long persistedOffset,             // to map to the stream 
                                      long persistedSize)               // to map to the stream )
        {
            Debug.Assert(blockManager != null);
            Debug.Assert(persistedOffset >=0);
            Debug.Assert(persistedSize >= 0);
            Debug.Assert(block != null);
                
            _persistedOffset = persistedOffset; 
            _offset = persistedOffset; 
            _persistedSize = persistedSize;
                
            _blockManager = blockManager;
            _block = block;
            
            _currentStreamLength = persistedSize;
        }

        /////////////////////////////
        // Internal Methods for the LocalFileBlock to call in order to know Dirty status and the new size 
        /////////////////////////////        
        internal PreSaveNotificationScanControlInstruction PreSaveNotification(long offset, long size)
        {
            return ZipIOBlockManager.CommonPreSaveNotificationHandler(
                    _blockManager.Stream,
                    offset, 
                    size,
                    _persistedOffset, 
                    Math.Min(_persistedSize, _currentStreamLength),  // in case the stream is smaller then our persisted block on 
                                                                            // disk, there is no need to preserve the meaningless persisted suffix 
                    ref _cachePrefixStream); 
        }
    
        internal bool DirtyFlag
        {
            get
            {
                return _dirtyFlag;
            }
        }

        internal bool DataChanged
        {
            get
            {
                return _dataChanged;
            }
        }

        internal long Offset
        {
            get
            {
                return _offset;
            }
        }

        internal void Move(long shiftSize)
        {
            CheckDisposed();
            if (shiftSize != 0)
            {
                checked{_offset +=shiftSize;}
                _dirtyFlag = true;
                Debug.Assert(_offset >=0);
            }
        }

        /// <summary>
        /// Streaming-specific variant of Save()
        /// </summary>
        /// <remarks>Writes current data to the underlying stream.
        /// Assumes the stream is in the correct place.</remarks>
        internal void SaveStreaming()
        {
            CheckDisposed();

            Debug.Assert(_cachePrefixStream == null); // _cachePrefixStream must not be used in streaming cases at all 
            
            Debug.Assert(_blockManager.Streaming);

            if (_dirtyFlag)
            {
                // in streaming cases all the data collected in the _sparseMemoryStreamSuffix 
                // and now we can save the SparseMemoryStream 
                if (_sparseMemoryStreamSuffix  != null)
                {
                    _sparseMemoryStreamSuffix.WriteToStream(_blockManager.Stream);

                    // update so that subsequent MemoryStreams will know where they begin
                    checked{_persistedSize += _sparseMemoryStreamSuffix.Length;}   
                    _sparseMemoryStreamSuffix.Close();
                    _sparseMemoryStreamSuffix  = null;
                }

                _dirtyFlag = false;
                _dataChanged = false;
            }
        }

        /// <summary>
        /// Save - called by the BlockManager to cause us to Flush to the underlying stream
        /// </summary>
        internal void Save()
        {
            CheckDisposed();
            Debug.Assert(!_blockManager.Streaming);
            
            if(_dirtyFlag)
            {                
                // we need to move the whole persisted block to the new position 
                long moveBlockSourceOffset = _persistedOffset;

                // in case the stream is smaller then our persisted block on disk there is 
                // no need to move meaningless persisted suffix 
                long moveBlockSize = Math.Min(_persistedSize, _currentStreamLength); 
                
                long moveBlockTargetOffset = _offset;

                long newPersistedSize = 0;
    
                if (_cachePrefixStream != null)
                {
                    // if we have something in cache we only should move whatever isn't cached
                    checked{moveBlockSourceOffset += _cachePrefixStream.Length;}
                    checked{moveBlockTargetOffset += _cachePrefixStream.Length;}
                    checked{moveBlockSize -= _cachePrefixStream.Length;}
                    Debug.Assert(moveBlockSize >=0);                    
                }

                _blockManager.MoveData(moveBlockSourceOffset, moveBlockTargetOffset, moveBlockSize);
                checked{newPersistedSize += moveBlockSize;}
    
                // only after data on disk was moved it is safe to flush the cached prefix buffer 
                if (_cachePrefixStream != null)
                {
                    // we need to seek and it is safe to do as we are not in the streaming mode 
                    _blockManager.Stream.Seek(_offset, SeekOrigin.Begin);
                    
                    Debug.Assert(_cachePrefixStream.Length > 0); // should be taken care of by the constructor 
                                                                                        // and PreSaveNotification                

                    _cachePrefixStream.WriteToStream(_blockManager.Stream);
                    checked{newPersistedSize += _cachePrefixStream.Length;}
                
                    // we can free the memory
                    _cachePrefixStream.Close();
                    _cachePrefixStream = null;
                }


                // and now we can save the SparseMemoryStream 
                if (_sparseMemoryStreamSuffix  != null)
                {
                    if (_blockManager.Stream.Position != checked (_offset + _persistedSize))
                    {
                        // we need to seek 
                        _blockManager.Stream.Seek(_offset + _persistedSize, SeekOrigin.Begin);
                    }
                    _sparseMemoryStreamSuffix.WriteToStream(_blockManager.Stream);
                    checked{newPersistedSize += _sparseMemoryStreamSuffix.Length;}

                    _sparseMemoryStreamSuffix.Close();
                    _sparseMemoryStreamSuffix  = null;
                }

                _blockManager.Stream.Flush();
            
                // we are not shifted between on disk image and in memory image any more 
                _persistedOffset = _offset;
                _persistedSize = newPersistedSize;

                Debug.Assert(newPersistedSize == _currentStreamLength);

                Debug.Assert(_cachePrefixStream == null); // we only expect this thing to be not null during Archive Save execution
                                                                                // after we are saved this field must be clear 

                _dirtyFlag = false;
                _dataChanged = false;
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
        /// <remarks>We implement this because we want a consistent experience (essentially Flush our data) if the user chooses to 
        /// call Dispose() instead of Close().</remarks>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    //streams wrapping this stream shouldn't pass Dispose calls through 
                    // it is responsibility of the BlockManager or LocalFileBlock (in case of Remove) to call 
                    // this dispose as appropriate (that is the reason why Flush isn't called here)

                    // multiple calls are fine - just ignore them
                    if (!_disposedFlag)
                    {
                        if (_sparseMemoryStreamSuffix  != null)
                        {
                            _sparseMemoryStreamSuffix.Close();
                        }

                        if (_cachePrefixStream != null)
                        {
                            _cachePrefixStream.Close();
                        }
                    }
                }
            }
            finally
            {
                _sparseMemoryStreamSuffix  = null;                
                _cachePrefixStream = null;                
                _disposedFlag = true;
                
                base.Dispose(disposing);
            }
        }

        /////////////////////////////
        // Private Methods
        /////////////////////////////        
        private void CheckDisposed()
        {
            if (_disposedFlag)
            {
                throw new ObjectDisposedException(null, SR.Get(SRID.ZipFileItemDisposed));            
            }
        }

        private ZipIOBlockManager _blockManager;
        private ZipIOLocalFileBlock _block;         // our owning block

        private long _offset;
        private long _persistedOffset;
        private long _persistedSize;

        private SparseMemoryStream _cachePrefixStream;
    
        private bool  _dirtyFlag;
        private bool  _dataChanged;

        //support for Stream methods 
        private bool _disposedFlag;

        private long _currentStreamLength;
        private long _currentStreamPosition;

        private SparseMemoryStream _sparseMemoryStreamSuffix;

        private const long _lowWaterMark = 0x19000;                 // we definately would like to keep everythuing under 100 KB in memory  
        private const long _highWaterMark = 0xA00000;   // we would like to keep everything over 10 MB on disk
    }
}
