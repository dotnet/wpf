// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  This is an internal class that is build around ArrayList of Memory streams to enable really large (63 bit size)
//  virtual streams.
//
//
//
//
//

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows;
using MS.Internal.WindowsBase;

namespace MS.Internal.IO.Packaging
{
    internal class SparseMemoryStream:  Stream
    {
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
        override public bool CanRead
        {
            get
            {
                return (!_disposedFlag);
            }
        }

        override public bool CanSeek
        {
            get
            {
                return (!_disposedFlag);
            }
        }

        override public bool CanWrite
        {
            get
            {
                return (!_disposedFlag);
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

            if (newLength < 0)
            {
                throw new ArgumentOutOfRangeException("newLength");
            }

#if DEBUG
    DebugAssertConsistentArrayStructure();
#endif

            if (_currentStreamLength != newLength)
            {
                if (_isolatedStorageMode)
                {
                    lock (PackagingUtilities.IsolatedStorageFileLock)
                    {
                        _isolatedStorageStream.SetLength(newLength);
                    }
                }
                else
                {
                    // if length become smaller , we might be able to close some of memoryStreams that we keep around
                    if (_currentStreamLength > newLength)
                    {
                        int removeIndex = _memoryStreamList.BinarySearch(GetSearchBlockForOffset(newLength));

                        // the new end of the stream does not fall into any existing blocks
                        if (removeIndex < 0)
                            // ~removeIndex represents the place at which we would insert the new block for write
                            removeIndex = ~removeIndex;
                        else
                        {
                            // we need to truncate the MemoryStream
                            MemoryStreamBlock memStreamBlock = _memoryStreamList[removeIndex];
                            checked
                            {
                                long temp = newLength - memStreamBlock.Offset;
                                if (temp > 0)
                                {
                                    memStreamBlock.Stream.SetLength(temp);
                                    ++removeIndex;
                                }
                                // else fall through and remove below
                            }
                        }

                        for (int i = removeIndex; i < _memoryStreamList.Count; ++i)
                        {
                            _memoryStreamList[i].Stream.Close();    // we need to carefully close the memoryStreams so they properly report the memory usage
                        }

                        _memoryStreamList.RemoveRange(removeIndex, _memoryStreamList.Count - removeIndex);
                    }
                }

                _currentStreamLength = newLength;
                if (_currentStreamPosition > _currentStreamLength)
                    _currentStreamPosition = _currentStreamLength;
            }

            // this can potentially affect memory consumption
            SwitchModeIfNecessary();

#if DEBUG
    DebugAssertConsistentArrayStructure();
#endif
        }

        override public long Seek(long offset, SeekOrigin origin)
        {
            CheckDisposed();
            long newStreamPosition = _currentStreamPosition;

            if (origin ==SeekOrigin.Begin)
            {
                newStreamPosition = offset;
            }
            else if  (origin == SeekOrigin.Current)
            {
                checked { newStreamPosition += offset; }
            }
            else if  (origin == SeekOrigin.End)
            {
                checked { newStreamPosition = _currentStreamLength + offset; }
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

            // No need to use checked{} since _currentStreamLength > _currentStreamPosition
            int bytesToRead = (int) Math.Min((long)count, _currentStreamLength - _currentStreamPosition);

            checked
            {
                Debug.Assert(bytesToRead > 0);

                int bytesRead;  // how much data we actually were able to read
                if (_isolatedStorageMode)
                {
                    lock (PackagingUtilities.IsolatedStorageFileLock)
                    {
                        _isolatedStorageStream.Seek(_currentStreamPosition, SeekOrigin.Begin);
                        bytesRead = _isolatedStorageStream.Read(buffer, offset, bytesToRead);
                    }
                }
                else
                {
                    // let's reset data to 0 first, so that gaps will be filled with 0s
                    // this is required for consistent behavior between the read calls used by the CRC Calculator
                    // and the WriteToStream calls used by the Flush/Save routines
                    Array.Clear(buffer,offset,bytesToRead);

                    int index = _memoryStreamList.BinarySearch(GetSearchBlockForOffset(_currentStreamPosition));
                    if (index < 0) // the head of new write block does not overlap with any existing blocks
                        // ~startIndex represents the insertion position
                        index = ~index;

                    for ( ; index < _memoryStreamList.Count; ++index)
                    {
                        MemoryStreamBlock memStreamBlock = _memoryStreamList[index];
                        long overlapBlockOffset;
                        long overlapBlockSize;
                        // let's check for overlap and fill up appropriate data
                        PackagingUtilities.CalculateOverlap(memStreamBlock.Offset, (int)memStreamBlock.Stream.Length,
                                                _currentStreamPosition, bytesToRead,
                                                out overlapBlockOffset, out overlapBlockSize);
                        if (overlapBlockSize > 0)
                        {
                            // we got an overlap let's copy data over to the target buffer
                            // _currentStreamPosition is not updated in this foreach loop; it will be updated later
                            Array.Copy(memStreamBlock.Stream.GetBuffer(), (int)(overlapBlockOffset - memStreamBlock.Offset),
                                            buffer, (int)(offset + overlapBlockOffset - _currentStreamPosition),
                                            (int)overlapBlockSize);
                        }
                        else
                            break;
                    }
                    // for memory stream case we get as much as we asked for
                    bytesRead = bytesToRead;
                }

                _currentStreamPosition += bytesRead;

                return bytesRead;
            }
        }

        override public void Write(byte[] buffer, int offset, int count)
        {
            CheckDisposed();
#if DEBUG
    DebugAssertConsistentArrayStructure();
#endif

            PackagingUtilities.VerifyStreamWriteArgs(this, buffer, offset, count);

            Debug.Assert(_currentStreamPosition >= 0);

            if (count == 0)
            {
                return;
            }

            checked
            {
                if (_isolatedStorageMode)
                {
                    lock (PackagingUtilities.IsolatedStorageFileLock)
                    {
                        _isolatedStorageStream.Seek(_currentStreamPosition, SeekOrigin.Begin);
                        _isolatedStorageStream.Write(buffer, offset, count);
                    }
                    _currentStreamPosition += count;
                }
                else
                {
                    WriteAndCollapseBlocks(buffer, offset, count);
                }
                _currentStreamLength = Math.Max(_currentStreamLength, _currentStreamPosition);
            }

             // this can potentially affect memory consumption
            SwitchModeIfNecessary();
#if DEBUG
    DebugAssertConsistentArrayStructure();
#endif
        }

        override public void Flush()
        {
            CheckDisposed();
        }

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        /// <summary>
        /// WriteToStream(Stream stream) writes the sparse Memory stream to the Stream provided as parameter
        /// starting at the current position in the stream
        /// </summary>
        internal void WriteToStream(Stream stream)
        {
            checked
            {
                if (_isolatedStorageMode)
                {
                    lock (PackagingUtilities.IsolatedStorageFileLock)
                    {
                        _isolatedStorageStream.Seek(0, SeekOrigin.Begin);
                        PackagingUtilities.CopyStream(_isolatedStorageStream, stream,
                                                Int64.MaxValue/*bytes to copy*/,
                                                0x80000 /*512K buffer size */);
                    }
                 }
                else
                {
                    CopyMemoryBlocksToStream(stream);
                }
            }
        }

        /////////////////////////////
        // Internal Constructor
        /////////////////////////////

        /// <summary>
        /// SparseMemoryStream constructor
        /// </summary>
        /// <param name="lowWaterMark">
        ///     if we consume less memory than lowWaterMark implementation will use arraList of MemoryStreams
        ///     (vaue 0 will disable Memory Stream based mode)
        /// </param>
        /// <param name="highWaterMark">
        ///      if we consume more memory than highWaterMark implementation will use the isolatedStorage
        ///      (vaue Int64.MaxVaue will disable isolated storage mode )
        /// </param>
        internal  SparseMemoryStream(
                                        long lowWaterMark,
                                        long highWaterMark): this(lowWaterMark, highWaterMark, true)
        {
        }

        /// <summary>
        /// SparseMemoryStream constructor
        /// </summary>
        /// <param name="lowWaterMark">
        ///     if we consume less memory than lowWaterMark implementation will use arraList of MemoryStreams
        ///     (vaue 0 will disable Memory Stream based mode)
        /// </param>
        /// <param name="highWaterMark">
        ///      if we consume more memory than highWaterMark implementation will use the isolatedStorage
        ///      (vaue Int64.MaxVaue will disable isolated storage mode )
        /// </param>
        /// <param name="autoCloseSmallBlockGaps">
        ///      There are 2 basic usages for the sparse memory stream. We use it as a buffering mechanism in ZIP IO,
        ///       in which case it is acceptable to assume that gaps between blocks are 0s. In the other scenario
        ///       (Encryption Stream ) we use it as a caching mechanism; in this case we ca not assume any values for the
        ///       that data located between blocks, so we shouldn't merge them (if the gap is small and doesn't justify an
        ///       overhead of the extra block record)
        /// </param>
        internal  SparseMemoryStream(
                                        long lowWaterMark,
                                        long highWaterMark,
                                        bool autoCloseSmallBlockGaps)
        {
            Invariant.Assert(lowWaterMark >=0 && highWaterMark >=0); // both of them must be positive or 0
            Invariant.Assert(lowWaterMark < highWaterMark); // low water mark must below high water mark
            Invariant.Assert(lowWaterMark <= Int32.MaxValue);  // low water mark must fit single memory stream 2G

            _memoryStreamList = new List<MemoryStreamBlock>(5);
            _lowWaterMark = lowWaterMark;
            _highWaterMark = highWaterMark;
            _autoCloseSmallBlockGaps = autoCloseSmallBlockGaps;
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
                    if (!_disposedFlag)
                    {
                        // go through all the Memory Streams and close them
                        foreach (MemoryStreamBlock memStreamBlock in _memoryStreamList)
                        {
                            // this will report the appropriate Memory usage back to the  ITrackingMemoryStreamFactory
                            memStreamBlock.Stream.Close();
                        }

                        // clean up isolated storage resources if in use
                        if (_isolatedStorageStream != null)
                        {
                            // can only rely on _isolatedStorageStream behaving correctly if we are not in our finalizer
                            _isolatedStorageStream.Close();
                        }
                    }
                }
            }
            finally
            {
                _disposedFlag = true;
                _isolatedStorageStream = null;
                _memoryStreamList = null;

                base.Dispose(disposing);
            }
        }

        /// <summary>
        /// Expose collection for use by clients that use this as
        /// a caching mechanism.
        /// </summary>
        /// <remarks>Cannot be IList because clients use
        /// BinarySearch() method.</remarks>
        internal List<MemoryStreamBlock> MemoryBlockCollection
        {
            get
            {
                CheckDisposed();
                return _memoryStreamList;
            }
        }

        internal long MemoryConsumption
        {
            get
            {
                CheckDisposed();
                return _trackingMemoryStreamFactory.CurrentMemoryConsumption;
            }
        }

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        private void CheckDisposed()
        {
            if (_disposedFlag)
            {
                throw new ObjectDisposedException(SR.Get(SRID.StreamObjectDisposed));
            }
        }

        private MemoryStreamBlock GetSearchBlockForOffset(long offset)
        {
            if (_searchBlock == null)
                _searchBlock = new MemoryStreamBlock(null, offset);
            else
                _searchBlock.Offset = offset;
            return _searchBlock;
        }

        private bool CanCollapseWithPreviousBlock(MemoryStreamBlock memStreamBlock,
                                                        long offset,
                                                        long length)
        {
            // There was an explicit request by the client not to merge near- by blocks
            if (!_autoCloseSmallBlockGaps || memStreamBlock == null)
            {
                return false;
            }

            checked
            {
                long gap = offset - (memStreamBlock.Offset + memStreamBlock.Stream.Length);

                Debug.Assert(gap >= 0);

                // if gap between them is smaller then a fix overhead of an extra block
                // and these are not too big to not fit in the MemoryStream Int32.MaxValue
                if (gap <= _fixBlockInMemoryOverhead
                    && gap + length + memStreamBlock.Stream.Length <= Int32.MaxValue)
                {
                    return true;
                }
            }

            return false;
        }

        private void WriteAndCollapseBlocks(byte[] buffer,
                                                int offset,
                                                int count)
        {
            int index = _memoryStreamList.BinarySearch(GetSearchBlockForOffset(_currentStreamPosition));
            bool writeDone = false;
            MemoryStreamBlock memStreamBlock = null;
            MemoryStreamBlock prevMemStreamBlock = null;

            checked
            {
                if (index < 0) // the head of new write block does not overlap with any existing blocks
                {
                    // ~startIndex represents the place at which we would insert the new block for write
                    index = ~index;
                    if (index != 0)    // Get the previous block of the new write block
                        prevMemStreamBlock = _memoryStreamList[index - 1];

                    // If the write request is close enough to the previous block and if the collapsing is allowed
                    if (CanCollapseWithPreviousBlock(prevMemStreamBlock, _currentStreamPosition, (long) count))
                    {
                        // write out any intervening zero's
                        prevMemStreamBlock.Stream.Seek(0, SeekOrigin.End);
                        SkipWrite(prevMemStreamBlock.Stream, prevMemStreamBlock.EndOffset, _currentStreamPosition);
                        prevMemStreamBlock.Stream.Write(buffer, offset, count);
                        writeDone = true;
                    }
                }
                else
                {
                    prevMemStreamBlock = _memoryStreamList[index];

                    // Write the requested bytes to the existing block if possible
                    if (prevMemStreamBlock.Stream.Length + count <= Int32.MaxValue) // Make sure there is enough space to append
                    {
                        prevMemStreamBlock.Stream.Seek(_currentStreamPosition - prevMemStreamBlock.Offset, SeekOrigin.Begin);
                        prevMemStreamBlock.Stream.Write(buffer, offset, count);
                        writeDone = true;
                        ++index;
                    }
                    else    // Not enough space
                    {
                        // There is overlap but we will created a new block for the write request; need to truncate the prev block
                        prevMemStreamBlock.Stream.SetLength(_currentStreamPosition - prevMemStreamBlock.Offset);
                        Debug.Assert(prevMemStreamBlock.Stream.Length > 0);
                    }
                }

                if (!writeDone)    // create a new block for the write request
                {
                    prevMemStreamBlock = ConstructMemoryStreamFromWriteRequest(buffer, _currentStreamPosition, count, offset);
                    Debug.Assert(prevMemStreamBlock.Stream.Length > 0);
                    _memoryStreamList.Insert(index, prevMemStreamBlock);
                    ++index;
                }

                _currentStreamPosition += count;   // Update the stream position since the write request is satisfied by this point

                int i;
                // Close and remove all completely-overlapping blocks
                for (i = index; i < _memoryStreamList.Count; ++i)
                {
                    if (_memoryStreamList[i].EndOffset > _currentStreamPosition)
                        break;

                    _memoryStreamList[i].Stream.Close();    // we need to carefully close the memoryStreams so they properly report the memory usage
                }
                if (i - index > 0)
                    _memoryStreamList.RemoveRange(index, i - index);

                ///////////////////////////////////////////
                // Check if the tail of the new write block needs to be collapsed with the following block
                ///////////////////////////////////////////

                long blockOffset = -1;
                if (index < _memoryStreamList.Count)   // Get the next block of the new write block
                {
                    memStreamBlock = _memoryStreamList[index];
                    blockOffset = _currentStreamPosition - memStreamBlock.Offset;
                }
                else
                    memStreamBlock = null;  // No next block to check

                if (blockOffset <= 0)   // No overlapping
                {
                    // Check if we should collapse the block
                    if (memStreamBlock != null
                        && (CanCollapseWithPreviousBlock(prevMemStreamBlock, memStreamBlock.Offset, memStreamBlock.Stream.Length)))
                    {
                        // remove the following block  memStreamBlock
                        _memoryStreamList.RemoveAt(index);

                        // write out any intervening zero's
                        prevMemStreamBlock.Stream.Seek(0, SeekOrigin.End);
                        SkipWrite(prevMemStreamBlock.Stream, _currentStreamPosition, memStreamBlock.Offset);
                        prevMemStreamBlock.Stream.Write(memStreamBlock.Stream.GetBuffer(), 0, (int) memStreamBlock.Stream.Length);
                    }
                }
                else    // Overlapping
                {
                    _memoryStreamList.RemoveAt(index);
                    // Memory stream length or buffer offset cannot be bigger than Int32.MaxValue
                    int leftoverSize = (int) (memStreamBlock.Stream.Length - blockOffset);

                    if (prevMemStreamBlock.Stream.Length + leftoverSize <= Int32.MaxValue)
                    {
                        prevMemStreamBlock.Stream.Seek(0, SeekOrigin.End);
                        prevMemStreamBlock.Stream.Write(memStreamBlock.Stream.GetBuffer(), (int) blockOffset, leftoverSize);
                    }
                    else
                    {
                        memStreamBlock = ConstructMemoryStreamFromWriteRequest(memStreamBlock.Stream.GetBuffer(),
                                                                _currentStreamPosition,
                                                                leftoverSize,
                                                                (int) blockOffset);
                        Debug.Assert(memStreamBlock.Stream.Length > 0);
                        _memoryStreamList.Insert(index, memStreamBlock);
                    }
                }
            }
        }

        private MemoryStreamBlock ConstructMemoryStreamFromWriteRequest(
                                                                                byte[] buffer,  // data buffer to be used for the new Memory Stream Block
                                                                                long writeRequestOffset,
                                                                                int  writeRequestSize,
                                                                                int  bufferOffset)
        {
            Debug.Assert(!_isolatedStorageMode);
            MemoryStreamBlock newMemStreamBlock  = new MemoryStreamBlock
                                                    (_trackingMemoryStreamFactory.Create(writeRequestSize),
                                                    writeRequestOffset);

            newMemStreamBlock.Stream.Seek(0,SeekOrigin.Begin);
            newMemStreamBlock.Stream.Write(buffer,bufferOffset,writeRequestSize);

            return newMemStreamBlock;
        }

        private void SwitchModeIfNecessary()
        {
            if (_isolatedStorageMode)
            {
                Debug.Assert(_memoryStreamList.Count ==0); // it must be empty in isolated storage mode

                // if we are in isolated storage mode we need to check the Low Water Mark crossing
                if (_isolatedStorageStream.Length < _lowWaterMark)
                {
                    if (_isolatedStorageStream.Length > 0)
                    {
                        //build memory stream
                        MemoryStreamBlock newMemStreamBlock  = new MemoryStreamBlock
                                                    (_trackingMemoryStreamFactory.Create((int)_isolatedStorageStream.Length),
                                                    0);

                        //copy data from iso storage to memory stream
                        lock (PackagingUtilities.IsolatedStorageFileLock)
                        {
                            _isolatedStorageStream.Seek(0, SeekOrigin.Begin);
                            newMemStreamBlock.Stream.Seek(0, SeekOrigin.Begin);
                            PackagingUtilities.CopyStream(_isolatedStorageStream, newMemStreamBlock.Stream,
                                                    Int64.MaxValue/*bytes to copy*/,
                                                    0x80000 /*512K buffer size */);
                        }

                        Debug.Assert(newMemStreamBlock.Stream.Length > 0);
                        _memoryStreamList.Add(newMemStreamBlock);
                    }

                    //switch mode
                     _isolatedStorageMode = false;

                    // release isolated storage disk space by setting its length to 0
                    // This way we don't have to re-open the isolated storage again if the memory consumption
                    //  goes above the High Water Mark
                    lock (PackagingUtilities.IsolatedStorageFileLock)
                    {
                        _isolatedStorageStream.SetLength(0);
                        _isolatedStorageStream.Flush();
                    }
                }
            }
            else
            {
                // if we are in Memory Stream mode we need to check the High Water Mark crossing
                if (_trackingMemoryStreamFactory.CurrentMemoryConsumption > _highWaterMark)
                {
                    // This outer lock is required to prevent creating an inverted lock pattern between PackagingUtilities.IsoStoreSyncRoot
                    // and PackagingUtilities.IsolatedStorageFileLock further down in the code.
                    lock (PackagingUtilities.IsoStoreSyncRoot)
                    {
                        // Copy data to isolated storage
                        lock (PackagingUtilities.IsolatedStorageFileLock)
                        {
                            EnsureIsolatedStoreStream();
                            CopyMemoryBlocksToStream(_isolatedStorageStream);
                        }
                    }

                    //switch mode
                    _isolatedStorageMode = true;

                    //release memory stream resources
                    foreach(MemoryStreamBlock memStreamBlock in _memoryStreamList)
                    {
                        // this will report the appropriate Memory usage back to the  ITrackingMemoryStreamFactory
                        memStreamBlock.Stream.Close();
                    }
                    _memoryStreamList.Clear();
                }
            }
        }

        /// <summary>
        /// CopyMemoryBlocksToStream - makes the stream reflect what is in memory
        /// </summary>
        /// <param name="targetStream">Stream that is modified to be contain the same data as
        /// that logically represented by the memory blocks.  The stream length is modified as
        /// necessary, and any "gaps" are filled with zero's.</param>
        /// <remarks>This function copies Memory Stream Array List to the target stream.
        /// It is used in 2 cases:
        /// 1. When we need to switch to isolated storage mode
        /// 2. When WriteToStream function is called</remarks>
        private void CopyMemoryBlocksToStream(Stream targetStream)
        {
            Debug.Assert(!_isolatedStorageMode);
            checked
            {
                // emit all memory blocks
                long trackingPosition = 0;
                foreach(MemoryStreamBlock memStreamBlock in _memoryStreamList)
                {
                    // write out any intervening zero's
                    trackingPosition = SkipWrite(targetStream, trackingPosition, memStreamBlock.Offset);

                    // write the memory block data
                    targetStream.Write(memStreamBlock.Stream.GetBuffer(), 0, (int)memStreamBlock.Stream.Length);
                    trackingPosition += memStreamBlock.Stream.Length;
                }

                // emit any trailing zero's that could result from a SetLength with no corresponding Write()
                if (trackingPosition < _currentStreamLength)
                    trackingPosition = SkipWrite(targetStream, trackingPosition, _currentStreamLength);

                Debug.Assert(trackingPosition == _currentStreamLength);
            }
            targetStream.Flush();
        }

        /// <summary>
        /// Writes out zero's to the targetStream from currentPos to the current offset
        /// </summary>
        /// <param name="targetStream"></param>
        /// <param name="currentPos"></param>
        /// <param name="offset"></param>
        /// <remarks>writes from the current stream position</remarks>
        /// <returns>offset</returns>
        private long SkipWrite(Stream targetStream, long currentPos, long offset)
        {
            long toSkip = offset - currentPos;
            Debug.Assert(toSkip >= 0);

            if (toSkip > 0)
            {
                // we must write out 0s so that the behavior is consistent between Read calls used by the CRC calculations
                // and the WriteToStream calls used by the Flush/Save logic
                byte[] zeroBytesBuf = new byte[Math.Min(0x80000, toSkip)]; // 512K chunks max
                while (toSkip > 0)
                {
                    int bytes = (int)Math.Min(toSkip, zeroBytesBuf.Length);
                    targetStream.Write(zeroBytesBuf, 0, bytes);
                    toSkip -= bytes;
                }
            }

            return offset;
        }

#if DEBUG
        private void DebugAssertConsistentArrayStructure()
        {
            if (_memoryStreamList != null)
            {
                long testTrackingPosition = 0;
                foreach(MemoryStreamBlock memStreamBlock in _memoryStreamList)
                {
                    Debug.Assert(testTrackingPosition  <= memStreamBlock.Offset);
                    testTrackingPosition  = memStreamBlock.Offset + memStreamBlock.Stream.Length;
                }

                Debug.Assert(testTrackingPosition <= _currentStreamLength);
            }
        }
#endif

        private void EnsureIsolatedStoreStream()
        {
            if (_isolatedStorageStream == null)
            {
                _isolatedStorageStream = PackagingUtilities.CreateUserScopedIsolatedStorageFileStreamWithRandomName(
                    3, out _isolatedStorageStreamFileName);
            }
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        //we use this class to track total memory consumed by the Memory streams that we are using
        private TrackingMemoryStreamFactory _trackingMemoryStreamFactory = new TrackingMemoryStreamFactory();

        private string _isolatedStorageStreamFileName;
        private Stream _isolatedStorageStream;
        private const int _fixBlockInMemoryOverhead = 100; // If the gap between blocks is smaller than this
                                    // threshold, it is not worth keeping them sperate due to overhead
                                    // This value is used to determine if blocks need to be collapsed

        //support for Stream methods
        private bool _disposedFlag;

        private bool _isolatedStorageMode;

        private long _currentStreamLength;
        private long _currentStreamPosition;

        private List<MemoryStreamBlock> _memoryStreamList;  // list of memory streams for buffering data
                                                        // it contains non-contiguous blocks of MemoryStreams which represents a whole stream
                                                        // Memory Streams in Array must not overlap
                                                        // This list is also maintained in offset order
        private MemoryStreamBlock _searchBlock;

        private long _lowWaterMark;
        private long _highWaterMark;

        private bool _autoCloseSmallBlockGaps;
    }

    internal class MemoryStreamBlock : IComparable<MemoryStreamBlock>
    {
        internal MemoryStreamBlock(MemoryStream stream, long offset)
        {
            Debug.Assert(offset >=0);

            _stream = stream;
            _offset = offset;
        }

        internal MemoryStream Stream
        {
            get
            {
                return _stream;
            }
        }

        internal long Offset
        {
            get
            {
                return _offset;
            }
            set
            {
               Debug.Assert(value >= 0);

                _offset = value;
            }
        }

        internal long EndOffset
        {
            get
            {
                checked
                {
                    return _offset + (_stream == null ? 0 : _stream.Length);
                }
            }
        }

        int IComparable<MemoryStreamBlock>.CompareTo(MemoryStreamBlock other)
        {
            if (other == null)
                return 1;

            if (_offset == other.Offset)
                return 0;
            else if (_offset > other.Offset)
            {
                if (_offset < other.EndOffset)
                    return 0;
                else
                    return 1;
            }
            else
            {
                if (other.Offset < EndOffset)
                    return 0;
                else
                    return -1;
            }
        }

        private MemoryStream _stream;
        private long _offset;
    }
}

