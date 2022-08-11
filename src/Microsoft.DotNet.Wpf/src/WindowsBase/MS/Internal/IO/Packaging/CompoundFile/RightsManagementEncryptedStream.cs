// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  This class implements stream subclass that is responsible for actual encryption decryption 
//
//
//
//


using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;           // for List<>
using System.IO.Packaging;
using System.Security.Cryptography;
using System.Security.RightsManagement;
using MS.Internal.IO.Packaging;
    
using System.Windows;
using MS.Internal.WindowsBase;

namespace MS.Internal.IO.Packaging.CompoundFile
{
    /// <summary>
    /// This class inherits from base abstract Stream class and provides 
    /// RM decryption and encryption services 
    /// </summary>
    internal class RightsManagementEncryptedStream: Stream
    {
        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
        /// <summary>
        /// See .NET Framework SDK under System.IO.Stream
        /// </summary>
        public override bool CanRead
        {
            get
            {
                // always return false if disposed
                return (_baseStream != null) && 
                            _baseStream.CanRead && 
                            _baseStream.CanSeek &&
                            _cryptoProvider.CanDecrypt;
            }
        }

        /// <summary>
        /// See .NET Framework SDK under System.IO.Stream
        /// </summary>
        public override bool CanSeek
        {
            get
            {
                // always return false if disposed
                return (_baseStream != null) && 
                            _baseStream.CanSeek;
            }
        }

        /// <summary>
        /// See .NET Framework SDK under System.IO.Stream
        /// </summary>
        public override bool CanWrite
        {
            get
            {
                // always return false if disposed
                return (_baseStream != null) && 
                            _baseStream.CanWrite && 
                            _baseStream.CanRead && 
                            _baseStream.CanSeek &&
                            _cryptoProvider.CanDecrypt &&
                            _cryptoProvider.CanEncrypt;
            }
        }
        
        /// <summary>
        /// See .NET Framework SDK under System.IO.Stream
        /// </summary>
        public override long Length
        {
            get
            {
                CheckDisposed();            

                // guaranteed initialized by constructor
                return _streamCachedLength;
            }
        }
 
        /// <summary>
        /// See .NET Framework SDK under System.IO.Stream
        /// </summary>
        public override long Position
        {
            get
            {
                CheckDisposed();            
                return _streamPosition;
            }
            set
            {
                // share logic
                Seek(value, SeekOrigin.Begin);
            }
        }

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
        /// <summary>
        /// See .NET Framework SDK under System.IO.Stream
        /// </summary>
        public override void Flush()
        {
            CheckDisposed();        

            FlushCache();
            
            _baseStream.Flush();
        }        

        /// <summary>
        /// See .NET Framework SDK under System.IO.Stream
        /// </summary>
        public override long Seek(long offset, SeekOrigin origin)
        {
            CheckDisposed();

            long temp = 0;
            checked
            {
                switch (origin)
                {
                    case SeekOrigin.Begin:
                        {
                            temp = offset;
                            break;
                        }
                    case SeekOrigin.Current:
                        {
                            temp = _streamPosition + offset;
                            break;
                        }
                    case SeekOrigin.End:
                        {
                            temp = Length + offset;
                            break;
                        }
                    default:
                        {
                            throw new ArgumentOutOfRangeException("origin", SR.SeekOriginInvalid);
                        }
                }
            }
            
            if (temp < 0)
            {
                throw new ArgumentOutOfRangeException("offset", SR.SeekNegative);
            }

            _streamPosition = temp;
            return _streamPosition;
        }        

        /// <summary>
        /// See .NET Framework SDK under System.IO.Stream
        /// </summary>
        public override void SetLength(long newLength)
        {
            CheckDisposed(); 
            
            if (newLength < 0)
            {
                throw new ArgumentOutOfRangeException("newLength", SR.CannotMakeStreamLengthNegative);
            }

            _streamCachedLength = newLength;

            // We are not caching this transaction for the following reason. The extra data that might 
            // be added to stream when the new length is higher than the existing length, 
            // although undefined (could be junk) must be consistent. Consistent 
            // is defined in a sense of multiple read requests performed on the same stream area.
            // In order to guarantee this consistency we either have to come up with some initial value 
            // (0 or anything else) remember the initialized area (or multiple areas like that if get a set 
            // of non contiguous writes), and take it into account during all the transactions.
            // Alternatively we can get some real bits allocated on the baseStream and take advantage 
            // of the underlying stream capability to preserve consistent content of the extra bits being 
            // allocated here.  
            FlushLength();

            if (_streamPosition > Length)
            {
                _streamPosition = Length;
            }
        }

        /// <summary>
        /// See .NET Framework SDK under System.IO.Stream
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckDisposed();         

            PackagingUtilities.VerifyStreamReadArgs(this, buffer, offset, count);

            int result = InternalRead(_streamPosition, buffer, offset, count);

            FlushCacheIfNecessary();

            checked { _streamPosition += result; }

            return result;
        }

        /// <summary>
        /// See .NET Framework SDK under System.IO.Stream
        /// </summary>
        public override void Write(byte[] buffer, int offset, int count)
        {
            CheckDisposed();

            PackagingUtilities.VerifyStreamWriteArgs(this, buffer, offset, count);

            _writeCache.Seek(this.Position, SeekOrigin.Begin);
            
            _writeCache.Write(buffer, offset, count);

            // we also might need to recalculate the size of the new updated stream 
            if (_writeCache.Length > Length)
            {
                // update our size accordingly
                SetLength(_writeCache.Length);
            }

            checked { _streamPosition += count; }

            FlushCacheIfNecessary();            
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
                    if (_baseStream != null)
                    {
                        FlushCache();

                        _baseStream.Close();

                        _readCache.Close(); // the life time of these two streams is exactly the 
                        _writeCache.Close();  // same as the lifetime of the object itself
                    }
                }
            }
            finally
            {
                _baseStream = null;     // we better not get any calls after this                            
                _readCache = null;
                _writeCache = null;
                base.Dispose(disposing);
            }
        }

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        internal RightsManagementEncryptedStream(
                                        Stream baseStream,
                                        CryptoProvider cryptoProvider)
        {
            Debug.Assert(baseStream != null);
            Debug.Assert(cryptoProvider != null);

            if (!cryptoProvider.CanDecrypt )
            {
                throw new ArgumentException(SR.CryptoProviderCanNotDecrypt, "cryptoProvider");            
            }

            if (!cryptoProvider.CanMergeBlocks)
            {
                throw new ArgumentException(SR.CryptoProviderCanNotMergeBlocks, "cryptoProvider");            
            }
            
            _baseStream = baseStream;
            _cryptoProvider = cryptoProvider;

            // Currently BitConverter is implemented as only supporting Little Endian byte order    
            // regardless of the machine type. We would like to make sure that this doesn't change 
            // as we need Little Endian byte order decoding capability on all machines in order to 
            // parse files that travel across different machine types.
            Debug.Assert(BitConverter.IsLittleEndian);

            // initialize stream length
            ParseStreamLength();
        }

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        /// <summary>
        /// Initial update of _streamCachedLength and _streamOnDiskLength
        /// </summary>
        private void ParseStreamLength()
        {
            if (_streamCachedLength < 0)
            {
                // seek to the beginning of the stream 
                _baseStream.Seek(0, SeekOrigin.Begin);

                // read the size prefix 
                byte[] prefixData = new byte[_prefixLengthSize];
                int bytesRead = PackagingUtilities.ReliableRead
                                            (_baseStream, prefixData, 0, prefixData.Length);

                // decode length data (from the prefix)
                if (bytesRead == 0)
                {
                    // probably a new stream - just assume length is zero
                    _streamOnDiskLength = 0;
                }
                else
                    if (bytesRead < _prefixLengthSize)
                    {
                        // not zero and shorter than legal length == corrupt file
                        throw new FileFormatException(SR.EncryptedDataStreamCorrupt);
                    }
                    else
                    {
                        checked
                        {
                            // This will throw on a negative value so we need not
                            // explicitly check for that
                            _streamOnDiskLength = (long)BitConverter.ToUInt64(prefixData, 0);
                        }
                    }
                _streamCachedLength = _streamOnDiskLength;
            }
        }

        private int InternalRead(long streamPosition, byte[] buffer, int offset, int count)
        {
            // use the explicitly passed in Position or reading in the stream 
            // we do not want to rely and change the real stream position 
            // as this function is called as a part of the Flush (which shouldn't change the stream Seek pointer)
            long start = streamPosition;

            // calculate how many bytes we can actually read 
            int realCount = count;

            checked
            {
                if (start + count > Length) 
                {
                    // if we have been asked to read something beyond the size 
                    // we need to truncate the size of the request
                    realCount = (int)(Length - start);
                }
            
                if (realCount <= 0)
                {
                    return 0;
                }
                
                ///////////////////////////////
                // Check if we can satisfy this request from the cache
                ///////////////////////////////
                // write cache first as it has the most fresh data 
                int cacheReadResult = ReadFromCache(_writeCache, start, realCount, buffer, offset);
                if (cacheReadResult > 0)
                {
                    return cacheReadResult;     
                }

                ///////////////////////////////
                // We need to be careful about WriteCache Blocks that are ahead of the 
                // requested area. If we satisfy the result from read cache or from Disk 
                // fetching we need to make sure we do not get the data that overlaps 
                // Write Cache, and therefore contains stale bits (that are not updated 
                // based on the write cache).
                // let's check how far write cache starts from the beginning of the request  
                ///////////////////////////////                
                long writeCacheStartOftheNextBlock = FindOffsetOfNextAvailableBlockAfter(_writeCache, start);
                //negative value indicates that no such block could be found 
                if (writeCacheStartOftheNextBlock >= 0) 
                {
                    Debug.Assert(writeCacheStartOftheNextBlock > start);
                    if (start + realCount > writeCacheStartOftheNextBlock)
                    {
                        realCount = (int)(writeCacheStartOftheNextBlock - start);
                    }
                }

                // read cache is second as it might have data that was overridden in the write cache  
                cacheReadResult = ReadFromCache(_readCache, start, realCount, buffer, offset);
                if (cacheReadResult > 0)
                {
                    return cacheReadResult;     
                }

                ///////////////////////////////
                // We will fetch some data from the Disk into the read cache
                // while fetching we need to be careful, so that  
                //  we do not read data that might be already in the read cache (that would be a perf penalty for no reason)
                ///////////////////////////////
                // let's check how far read cache starts from the beginning of the request  
                long readCacheStartOftheNextBlock = FindOffsetOfNextAvailableBlockAfter(_readCache, start);
                //negative value indicates that no such block could be found 
                if (readCacheStartOftheNextBlock >= 0) 
                {
                    Debug.Assert(readCacheStartOftheNextBlock > start);
                    if (start + realCount > readCacheStartOftheNextBlock)
                    {
                        realCount = (int)(readCacheStartOftheNextBlock - start);
                    }
                }
                                
                // Read Data from disk, decrypt it and cache it 
                FetchBlockIntoReadCache(start, realCount);

                // at this point we must be able to satisfy request from the read cache 
                // important thing is we are guaranteed based on the logic above that the data we read 
                // will not overlap with any data that might be dirty in the write cache 
                cacheReadResult = ReadFromCache(_readCache, start, realCount, buffer, offset);
                Debug.Assert(cacheReadResult > 0);
                return cacheReadResult;     
            }
        }

        private int ReadFromCache(SparseMemoryStream cache, long start, int count, byte[] buffer, int bufferOffset)
        {
#if DEBUG        
            // debug only check for valid parameters, as we generally expect callers to verify them 
            PackagingUtilities.VerifyStreamReadArgs(this, buffer, bufferOffset, count);
#endif
            Debug.Assert(cache != null);
            Debug.Assert(start >=0);
            IList<MemoryStreamBlock> collection = cache.MemoryBlockCollection;

            checked
            {
                // use BinarySearch to locate blocks of interest quickly
                bool match;     // exact match?
                int index = FindIndexOfBlockAtOffset(cache, start, out match);

                // if match was found, read from it
                int bytesRead = 0;
                if (match)
                {
                    MemoryStreamBlock memStreamBlock = collection[index];
                    long overlapBlockOffset;
                    long overlapBlockSize;
                    
                    // we have got an overlap which can be used to satisfy the read request,
                    // at least  partially
                    PackagingUtilities.CalculateOverlap(memStreamBlock.Offset, memStreamBlock.Stream.Length,
                                          start, count,
                                          out overlapBlockOffset, out overlapBlockSize);

                    if (overlapBlockSize > 0)
                    {
                        // overlap must be starting at the start as we know for sure that 
                        // memStreamBlock.Offset <= start
                        Debug.Assert(overlapBlockOffset == start); 

                        memStreamBlock.Stream.Seek(overlapBlockOffset - memStreamBlock.Offset, SeekOrigin.Begin);

                        // we know that memStream will return as much data as we requested
                        // even if this logic changes we do not have to return everything 
                        // a partially complete read is acceptable here 
                        bytesRead = memStreamBlock.Stream.Read(buffer, bufferOffset, (int)overlapBlockSize);
                    }
                }

                return bytesRead;
            }
        }

        /// <summary>
        /// Uses BinarySearch to locate the index of the block that contains start
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="start"></param>
        /// <param name="match">True if match found.  If this is false, the index returned is where the item would appear if it existed.</param>
        /// <returns>Index</returns>
        private int FindIndexOfBlockAtOffset(SparseMemoryStream cache, long start, out bool match)
        {
            if (cache.MemoryBlockCollection.Count == 0)
            {
                match = false;
                return 0;
            }

            // use BinarySearch to locate blocks of interest quickly
            if (_comparisonBlock == null)
                _comparisonBlock = new MemoryStreamBlock(null, start);
            else
                _comparisonBlock.Offset = start;

            int index = cache.MemoryBlockCollection.BinarySearch(_comparisonBlock);
            if (index < 0) // no match
            {
                // ~index represents the place at which the block we asked for
                // would appear if it existed
                index = ~index;
                match = false;
            }
            else
            {
                match = true;
            }
            return index;
        }

        /// <summary>
        /// Find offset of next available block after this offset
        /// </summary>
        /// <param name="cache">cache to inspect</param>
        /// <param name="start">offset to start search from</param>
        /// <returns>offset of block start if found, otherwise -1</returns>
        /// <remarks>Contract: Call only when start is not inside any existing block.</remarks>
        private long FindOffsetOfNextAvailableBlockAfter(SparseMemoryStream cache, long start)
        {
            Debug.Assert(start >= 0);
            Debug.Assert(cache != null);

            // Find the index where a new block with offset start would be placed
            bool match;
            int index = FindIndexOfBlockAtOffset(cache, start, out match);
            Debug.Assert(!match, "Must only be called when there is no match");

            // If we have an exact match, we return the offset of the next block, unless
            // there are no more blocks - then we return -1
            if (index >= (cache.MemoryBlockCollection.Count))
                return -1;      // no block beyond start
            else
            {
                return cache.MemoryBlockCollection[index].Offset;
            }
        }

        private void FetchBlockIntoReadCache(long start, int count)        
        {
            ///////////////////////////////
            // Let's calculate the block information that need to be read 
            ///////////////////////////////
            long firstBlockOffset; 
            long blockCount; 
            int blockSize = _cryptoProvider.BlockSize;

            // this call might potentially change blockSize and in case of the CryptoProvider supporting merging 
            // blocks it will become a multiple of original block size, big enough to cover the requested area
            CalcBlockData(start, count, _cryptoProvider.CanMergeBlocks, 
                ref blockSize,    // can be modified to be a multiple of the original value 
                out firstBlockOffset, 
                out blockCount);

            Debug.Assert(blockCount > 0, 
                    "RightsManagementEncryptedStream.Read Unable to process the request, calculated block count <= 0");

            checked
            { 
                ///////////////////////////////
                // READ CRYPTO DATA 
                ///////////////////////////////
                // try to seek to the first block 
                // this will take the prefix size into account
                long newPosition = _baseStream.Seek(_prefixLengthSize + firstBlockOffset, SeekOrigin.Begin);

                Debug.Assert( newPosition == _prefixLengthSize + firstBlockOffset, 
                    "RightsManagementEncryptedStream.Read Unable to seek to required position");

                // try to read all the required blocks into memory 
                int totalByteCount = (int)(blockCount * blockSize);                
              
                byte[] cryptoBuffer = new byte [totalByteCount];

                int bytesRead = PackagingUtilities.ReliableRead(
                                        _baseStream,
                                        cryptoBuffer, 
                                        0, 
                                        totalByteCount,  //  we are asking for all the bytes _cryptoProvider.BlockSize
                                        _cryptoProvider.BlockSize); // we are guaranteed to get at least that much, unless the end of stream is encountered

                if (bytesRead < _cryptoProvider.BlockSize)
                {
                    // we have found an unexpected end of stream 
                    throw new FileFormatException(SR.EncryptedDataStreamCorrupt);
                }

                /////////////////////////////////////////////
                // DECRYPT DATA  AND STORE IT IN THE READ CACHE 
                /////////////////////////////////////////////

                //adjust block count according to the data that we were able to read 
                // it could be as few as cryptoProvider.BlockSize bytes or as many as totalByteCount
                int readCryptoBlockSize = _cryptoProvider.BlockSize;                
                int readCryptoBlockCount = (int)(bytesRead/readCryptoBlockSize);  // figure out how many blocks we read
                Debug.Assert(readCryptoBlockCount >=1); // we must have at least 1 

                if (_cryptoProvider.CanMergeBlocks)
                {
                    readCryptoBlockSize *= readCryptoBlockCount;
                    readCryptoBlockCount = 1;
                }
                    
                byte[] cryptoTextBlock = new byte [readCryptoBlockSize];

                //prepare read cache stream to accept data in the right position 
                _readCache.Seek(firstBlockOffset, SeekOrigin.Begin);
                for (long i = 0; i < readCryptoBlockCount; i++)
                {
                    // copy the appropriate data from the cryptoBuffer (read from disk) 
                    // into the cryptoTextBlock for decryption 
                    Array.Copy(cryptoBuffer,  i * readCryptoBlockSize,  cryptoTextBlock  ,  0,  readCryptoBlockSize);                    
                    byte[] clearTextBlock = _cryptoProvider.Decrypt(cryptoTextBlock);

                    // put the results into the read cache 
                    _readCache.Write(clearTextBlock, 0, readCryptoBlockSize);
                }
            }
        }

        private void FlushLength()
        {
            // update size of the physical stream according to the cached stream size value 
            if ((_streamCachedLength >=0)  &&                // negative value indicates that it isn't dirty nothing to update
               (_streamCachedLength != _streamOnDiskLength)) // if these 2 are not equal it is an andicator of a "dirty" date 
            {
                _baseStream.Seek(0, SeekOrigin.Begin);

                // write data into the prefix 
                byte[] prefixData = BitConverter.GetBytes((ulong)_streamCachedLength);
                _baseStream.Write(prefixData, 0, prefixData.Length);

                checked
                {
                    // update base stream length , base stream always must have size equal to a 
                    // multiple of block size plus +prefixLengthSize (do not truncate a half of a block at the end)
                    int blockSize = _cryptoProvider.BlockSize;                    
                    long physicalBaseStreamLength = 
                               _prefixLengthSize + 
                               GetBlockSpanCount(blockSize, 0, _streamCachedLength) * blockSize;
                    
                    // NOTE: This call will not randomize or zero any data beyond the end of the stream.  This is not considered
                    // a privacy issue because the data is encrypted.  If the caller expects privacy, they should create a new stream
                    // and copy the data to that new stream.
                    _baseStream.SetLength(physicalBaseStreamLength);
                    _streamOnDiskLength = _streamCachedLength; 
                }
            }
        }


        private static long GetBlockNo(long blockSize, long index)
        {
            Debug.Assert(blockSize > 1, "GetBlockNo recieved blockSize parameter value <= 1");
            Debug.Assert(index >= 0 , "GetBlockNo recieved index parameter value < 0");

            checked
            {
                return index / blockSize;
            }
        }

        private static long GetBlockSpanCount(long blockSize, long index, long size)
        {
            checked
            {
                if (size == 0)
                {
                    return 0;
                }
                else
                {
                    return GetBlockNo(blockSize, index + size - 1) - GetBlockNo(blockSize, index) + 1;
                }
            }
        }

        private static void CalcBlockData(
            long start,        // offset of the first byte in the chunk of data 
            long size,         // size of the chunk of data  
            bool canMergeBlocks, // controls whether we can automatically merge blocks (we can for block ciphers and not for stream ciphers)
            ref int blockSize,  // blockSize which is used as a base (it can be adjusted if canMergeBlocks == true)            
            // This index is only used for decryption/encryption as a 
            // counter measure against frequency analysis 
            // (it is not used to calculate actual offsets in the file )
            out long firstBlockOffset, // byte offset of the first block that overlaps our chunk of data 
            out long blockCount)  // total number of block required to completely cover our data 
        {
            checked
            {
                long firstBlockNumber = GetBlockNo(blockSize, start);
                firstBlockOffset = firstBlockNumber * blockSize;
                blockCount = GetBlockSpanCount(blockSize, start, size);

                if (canMergeBlocks)
                {
                    // we need to recalculate everything as if it were a single large block 
                    blockSize = (int)(blockSize * blockCount);
                    blockCount = 1;
                }
            }
        }

        private void CheckDisposed()
        {
            if (_baseStream == null)
            {
                throw new ObjectDisposedException(null, SR.StreamObjectDisposed);
            }
        }

        private void FlushCacheIfNecessary()
        {
            checked
            {
                if (_readCache.MemoryConsumption + _writeCache.MemoryConsumption > _autoFlushHighWaterMark)
                {
                    FlushCache();
                }                
            }
}
        
        private void FlushCache()
        {
            checked
            {
                FlushLength();

                // we know that it is a sorted list, which means we can keep track of update highWaterMark
                // it will greately help in the case of small (1-2 bytes) update blocks
                long updatedHighWaterMark = 0;
                byte[] clearTextBuffer = null; // lazy init
                foreach(MemoryStreamBlock memStreamBlock in _writeCache.MemoryBlockCollection)
                {
                    long dirtyBlockOffset = memStreamBlock.Offset;
                    long dirtyBlockSize = memStreamBlock.Stream.Length;

                    //Adjust dirty block parameters according to the updatedHighWaterMark
                    // this way we can the blocks (part of the block) that have been taken care of by the previous reads  
                    if (dirtyBlockOffset < updatedHighWaterMark)
                    {
                        dirtyBlockSize = dirtyBlockOffset + dirtyBlockSize -updatedHighWaterMark;
                        dirtyBlockOffset = updatedHighWaterMark;
                    }
    
                    // There is a chance that this was a small block that was updated in the previous loop cycle 
                    // as a result of being in the same crypto block 
                    if (dirtyBlockSize <= 0)
                    {
                        continue;
                    }
                        
                    ///////////////////////////////
                    // Let's calculate the block information that need to be read 
                    ///////////////////////////////
                    long firstBlockOffset; 
                    long blockCount; 
                    int blockSize = _cryptoProvider.BlockSize;

                    // this call might potentially change blockSize and in case of the CryptoProvider supporting merging 
                    // blocks it will become a multiple of original block size, big enough to cover the requested area
                    CalcBlockData(dirtyBlockOffset, dirtyBlockSize, _cryptoProvider.CanMergeBlocks, 
                        ref blockSize,    // can be modified to the multiple of the original value 
                        out firstBlockOffset, 
                        out blockCount);

                    // We can use our own reading functionality to read this data into a buffer (possibly using cached data)
                    int totalByteCount = (int)(blockCount * blockSize);
                    if ((clearTextBuffer == null) || (clearTextBuffer.Length < totalByteCount))
                    {
                        // Allocate at least 4k (to improve chances of re-use on subsequent loop iterations)
                        // and with enough room for the current operation.
                        clearTextBuffer = new byte[Math.Max(0x1000, totalByteCount)];
                    }

                    int readCount = InternalReliableRead(firstBlockOffset, clearTextBuffer, 0, totalByteCount);

                    // if we have found an end of stream we should pre-populate the buffer suffix with some random data 
                    // to make sure we do not always encrypt 0's 
                    if (readCount < totalByteCount)
                    {
                        RandomFillUp(clearTextBuffer, readCount, totalByteCount - readCount);
                    }

                    // Encrypt The data 
                    byte[] cryptoTextBuffer = _cryptoProvider.Encrypt(clearTextBuffer);

                    // Write the encrypted data out  
                    _baseStream.Seek(firstBlockOffset + _prefixLengthSize, SeekOrigin.Begin);
                    _baseStream.Write(cryptoTextBuffer,0, totalByteCount);

                    updatedHighWaterMark = firstBlockOffset + totalByteCount;
                }
            }
            _writeCache.SetLength(0);
            _readCache.SetLength(0);
        }


        // We arte not using the standard library reliable read as we want to bypass the 
        // auto Flushing Logic which might result in recursive calls  
        private int InternalReliableRead(long streamPosition, byte[] buffer, int offset, int count)
        {
            Debug.Assert(streamPosition >= 0);        
            Debug.Assert(buffer != null);
            Debug.Assert(count >= 0);
            Debug.Assert(checked(offset + count <= buffer.Length));
            
            // let's read the whole block into our buffer 
            int totalBytesRead = 0;

            checked
            {
                while (totalBytesRead < count)
                {
                    int bytesRead = InternalRead(
                                    streamPosition + totalBytesRead,
                                    buffer,
                                    offset + totalBytesRead,
                                    count - totalBytesRead);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    totalBytesRead += bytesRead;
                }
            }

            return totalBytesRead;
        }
                    
        private void RandomFillUp(Byte[] buffer, int offset, int count)
        {
            Debug.Assert(buffer != null);
            Debug.Assert(buffer.Length > 0);
            Debug.Assert(count >= 0);
            Debug.Assert(checked(offset + count <= buffer.Length));

            if (count == 0)
            {
                return;
            }
            
            if (_random == null)
            {
                _random = new Random();
            }

            if (_randomBuffer == null || (_randomBuffer.Length < count))
                _randomBuffer = new byte[Math.Max(16, count)];              // current block size is 16

            _random.NextBytes(_randomBuffer);

            Array.Copy(_randomBuffer, 0,  buffer, offset, count);
        }

        //------------------------------------------------------
        //
        //  Private Members
        //
        //------------------------------------------------------
        private Random _random;
        private Stream _baseStream;

        private long _streamCachedLength = -1;  // starts as an invalid value (which force a read on the first usage)
                                                // it potentially might contain information that need to be flushed 
                                                                
        private long _streamOnDiskLength = -1;  // starts as an invalid value (which force a read on the first usage)
                                                // this value always matches to the real stream size 
                                                // as it is saved in the stream prefix.
        
        private long _streamPosition;           // always start at 0 
        

        private CryptoProvider _cryptoProvider;
            
        private const int _prefixLengthSize = 8; // 8 byte original stream size prefix  

        private byte[]              _randomBuffer;      // re-usable buffer for random 
        private MemoryStreamBlock   _comparisonBlock;   // re-usable comparison block

        /////////////////////////////////////////////////////////
        // CACHING DATA SECTION 
        // The caching policy is the following: 
        // There are 2 Tracking memory streams, one used to cache Writes (data coming from the 
        // consumer of the APIs), and the other is used to cache Read (data read from the underlying storage)
        // In both cases we cache clear text data. 
        // Both Caches are completely cleaned if we Get Flush call or 
        // we choose to clear the cache to reduce memory consumption. Potentially some kind of more advanced 
        // policy can be introduced here. (FIFO, or something based on a usage pattern activity) 
        ////////////////////////////////////////////////////////

        // MaxValues below are used in order to ensure that we do not trigger any form of Isolated Storage Backup 
        // This is not a goal here. We are definitely would like to keep  SparseMemoryStream in the cached(in - memory) mode.
        // In the context of our auto flush logic set at 16K we are pretty safe with those values…
        private SparseMemoryStream _readCache =  new SparseMemoryStream(Int32.MaxValue, Int64.MaxValue, false); 
        private SparseMemoryStream _writeCache =  new SparseMemoryStream(Int32.MaxValue, Int64.MaxValue, false);

        private const long _autoFlushHighWaterMark  = 0x4000; // 16 K 
    }
} 

