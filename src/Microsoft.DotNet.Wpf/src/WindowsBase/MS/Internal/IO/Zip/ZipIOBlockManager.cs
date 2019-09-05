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
//

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Collections;
using System.Globalization;
using System.Runtime.Serialization;
using System.Windows;
using MS.Internal.IO.Packaging;  // for PackagingUtilities
using MS.Internal.WindowsBase;

namespace MS.Internal.IO.Zip
{
    /// <summary>
    /// This is the main class of the actual ZIP IO implementation. It is primary responsibility
    /// is to maintain the map and status of the parsed and loaded areas(blocks) of the file.   
    /// It is also supports manipulating this map (adding and deleting blocks)
    /// </summary>                
    internal class ZipIOBlockManager : IDisposable, IEnumerable
    {
        //------------------------------------------------------
        //
        //  Public Methods  
        //
        //------------------------------------------------------
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            CheckDisposed();

            return _blockList.GetEnumerator();
        }

        //------------------------------------------------------
        //
        //  Internal Properties  
        //
        //------------------------------------------------------
        /// <summary>
        /// This property returns the status of whether Central directory is loaded or not. 
        /// This property is rarely used, as most clients will just ask for CentralDirectoryBlock
        /// and oif it isn't loaded it will be. 
        /// The only reason to use IsCentralDirectoryBlockLoaded property is to differentiate 
        /// scenarios in which some optimization is possible, if central directory isn't loaded yet. 
        /// </summary>                        
        internal bool IsCentralDirectoryBlockLoaded 
        {
            get
            {
                CheckDisposed();
                return (_centralDirectoryBlock != null);
            }
        }

        /// <summary>
        /// This property returns the CentralDirectoryBlock and provides lazy load 
        /// fuinctionality. This isthe only way other classes can access information 
        /// from the Central Directory Block
        /// </summary>                                
        internal ZipIOCentralDirectoryBlock CentralDirectoryBlock
        {
            get
            {
                CheckDisposed();
                if (_centralDirectoryBlock == null)
                {
                    // figure out if we are in ZIP64 mode or not 
                    if (Zip64EndOfCentralDirectoryBlock.TotalNumberOfEntriesInTheCentralDirectory > 0)
                    {
                        LoadCentralDirectoryBlock();
                    }
                    else
                    {
                        // We need to be aware of the special case of empty Zip Archive
                        // with a single record : End Of Central directory 
                        //In  such cases we should create new CentralDirectoryBlock 
                        CreateCentralDirectoryBlock();
                    }
                }

                return _centralDirectoryBlock;
            }
        }

        /// <summary>
        /// This property returns the Zip64EndOfCentralDirectoryBlock and provides lazy load 
        /// fuinctionality. This is the only way other classes can access information 
        /// from the Zip64EndOfCentralDirectoryBlock
        /// </summary>        
        internal ZipIOZip64EndOfCentralDirectoryBlock Zip64EndOfCentralDirectoryBlock 
        {
            get
            {
                CheckDisposed();
                
                if (_zip64EndOfCentralDirectoryBlock == null)
                {
                    CreateLoadZip64Blocks();
                }

                return _zip64EndOfCentralDirectoryBlock;
            }
        }

        /// <summary>
        /// This property returns the Zip64EndOfCentralDirectoryLocatorBlock and provides lazy load 
        /// fuinctionality. This is the only way other classes can access information 
        /// from the Zip64EndOfCentralDirectoryLocator Block
        /// </summary>   
        internal ZipIOZip64EndOfCentralDirectoryLocatorBlock Zip64EndOfCentralDirectoryLocatorBlock 
        {
            get
            {
                CheckDisposed();
                
                if (_zip64EndOfCentralDirectoryLocatorBlock == null)
                {
                    CreateLoadZip64Blocks();
                }

                return _zip64EndOfCentralDirectoryLocatorBlock;
            }
        }

        /// <summary>
        /// This property returns the CentralDirectoryBlock and provides lazy load 
        /// fuinctionality. This is the only way other classes can access information 
        /// from the Central Directory Block
        /// </summary>                                
        internal ZipIOEndOfCentralDirectoryBlock EndOfCentralDirectoryBlock
        {
            get
            {
                CheckDisposed();
                if (_endOfCentralDirectoryBlock == null)
                {
                    LoadEndOfCentralDirectoryBlock();
                }

                return _endOfCentralDirectoryBlock;
            }
        }

        internal Stream Stream 
        {
            get
            {
                CheckDisposed();
                return _archiveStream;
            }
        }

        internal bool Streaming
        {
            get
            {
                CheckDisposed();
                return _openStreaming;
            }
        }

        internal BinaryReader BinaryReader
        {
            get
            {
                CheckDisposed();
                Debug.Assert(!_openStreaming, "Not legal in Streaming mode");

                if (_binaryReader == null)
                {
                    _binaryReader = new BinaryReader(Stream, Encoding);
                }
                return _binaryReader;
            }
        }

        internal BinaryWriter BinaryWriter
        {
            get
            {
                CheckDisposed();
                if (_binaryWriter == null)
                {
                    _binaryWriter = new BinaryWriter(Stream, Encoding);
                }
                return _binaryWriter;
            }
        }

        internal Encoding Encoding
        {
            get
            {
                CheckDisposed();
                return _encoding;
            }
        }

        internal bool DirtyFlag
        {
            set
            {
                CheckDisposed();
                _dirtyFlag = value;
            }
            get
            {
                CheckDisposed();
                return _dirtyFlag;
            }
        }
        
        static internal int MaxFileNameSize
        {
            get
            {
                return UInt16.MaxValue;
            }
        }

        //------------------------------------------------------
        //
        //  Internal Methods  
        //
        //------------------------------------------------------

        internal void  CreateEndOfCentralDirectoryBlock()  
        {
            CheckDisposed();

            // Prevent accidental call if underlying stream is non-empty since
            // any legal zip archive contains an EOCD block.
            Debug.Assert(_openStreaming || _archiveStream.Length == 0);

            // Disallow multiple calls.
            Debug.Assert(_endOfCentralDirectoryBlock == null);

            // construct Block find it and parse it 
            long blockOffset = 0;   // this will be updated later
            _endOfCentralDirectoryBlock = ZipIOEndOfCentralDirectoryBlock.CreateNew(this, blockOffset);

            // this will add a block to the tail
            AppendBlock(_endOfCentralDirectoryBlock); 
            DirtyFlag = true;
        }

        internal void LoadEndOfCentralDirectoryBlock()
        {
            Debug.Assert(_endOfCentralDirectoryBlock == null);
            Debug.Assert(!_openStreaming, "Not legal in Streaming mode");

            // construct Block find it and parse it 
            _endOfCentralDirectoryBlock = ZipIOEndOfCentralDirectoryBlock.SeekableLoad(this);

            //ask block manager to MAP this block 
            MapBlock(_endOfCentralDirectoryBlock);
        }

        internal ZipIOLocalFileBlock CreateLocalFileBlock(string zipFileName, CompressionMethodEnum compressionMethod, DeflateOptionEnum deflateOption)  
        {
            CheckDisposed();        
        
            // we are guaranteed uniqueness at this point , so let's just add a 
            // block at the end of the file, just before the central directory             
            // construct Block find it and parse it

            // STREAMING Mode:
            //   NOTE: _blockList is NOT in offset order except the last four blocks
            //      (CD, Zip64 EOCD, Zip64 EOCD Locator, and EOCD)

            ZipIOLocalFileBlock localFileBlock = ZipIOLocalFileBlock.CreateNew(this, 
                                                zipFileName, 
                                                compressionMethod, 
                                                deflateOption);
            
            InsertBlock(CentralDirectoryBlockIndex, localFileBlock); 

            CentralDirectoryBlock.AddFileBlock(localFileBlock);

            DirtyFlag = true;
            
            return localFileBlock;
        }

        internal ZipIOLocalFileBlock LoadLocalFileBlock(string zipFileName)  
        {
            CheckDisposed();

            Debug.Assert(!_openStreaming, "Not legal in Streaming mode");
            Debug.Assert(CentralDirectoryBlock.FileExists(zipFileName)); // it must be in the central directory

            // construct Block find it and parse it 
            ZipIOLocalFileBlock localFileBlock = ZipIOLocalFileBlock.SeekableLoad(this, zipFileName);
            
            MapBlock(localFileBlock);
            return localFileBlock;
        }

        internal void RemoveLocalFileBlock(ZipIOLocalFileBlock localFileBlock)  
        {
            CheckDisposed();

            Debug.Assert(!_openStreaming, "Not legal in Streaming mode");

            Debug.Assert(localFileBlock != null, " At this point local File block must be preloaded");
            Debug.Assert(CentralDirectoryBlock.FileExists(localFileBlock.FileName), 
                            " At this point local File block must be mapped in central directory");

            
            // remove it from our list
            _blockList.Remove(localFileBlock);

            // remove this from Central Directory 
            CentralDirectoryBlock.RemoveFileBlock(localFileBlock.FileName);
            DirtyFlag = true;
            
            // at this point we can Dispose it to make sure that any calls 
            // to this file block through outstanding indirect references will result in object Disposed exception 
            localFileBlock.Dispose();
        }

        internal void MoveData(long moveBlockSourceOffset, long moveBlockTargetOffset, long moveBlockSize)
        {
            Debug.Assert(moveBlockSize >=0);
            Debug.Assert(!_openStreaming, "Not legal in Streaming mode");

            if ((moveBlockSize ==0) || (moveBlockSourceOffset == moveBlockTargetOffset))
            {
                //trivial empty move case 
                return;
            }

            checked
            {
                byte[] tempBuffer = new byte [Math.Min(moveBlockSize,0x100000)]; // min(1mb, requested block size)
                long bytesMoved = 0;
                while(bytesMoved < moveBlockSize)
                {
                    long subBlockSourceOffset;
                    long subBlockTargetOffset;
                    int subBlockSize = (int)Math.Min((long)tempBuffer.Length,  moveBlockSize - bytesMoved);
                    
                    if (moveBlockSourceOffset > moveBlockTargetOffset)
                    {
                        subBlockSourceOffset = moveBlockSourceOffset  + bytesMoved; 
                        subBlockTargetOffset = moveBlockTargetOffset  + bytesMoved; 
                    }
                    else
                    {
                        subBlockSourceOffset = moveBlockSourceOffset + moveBlockSize - bytesMoved - subBlockSize; 
                        subBlockTargetOffset =  moveBlockTargetOffset  + moveBlockSize - bytesMoved - subBlockSize;
                    }
                    
                    _archiveStream.Seek(subBlockSourceOffset, SeekOrigin.Begin);
                    int bytesRead = PackagingUtilities.ReliableRead(_archiveStream, tempBuffer, 0, subBlockSize);

                    if (bytesRead != subBlockSize)
                    {
                        throw new FileFormatException(SR.Get(SRID.CorruptedData));
                    }
                    
                    _archiveStream.Seek(subBlockTargetOffset, SeekOrigin.Begin);                
                    _archiveStream.Write(tempBuffer, 0, subBlockSize);

                    checked{bytesMoved += subBlockSize;}
                }
            }
        }

        /// <summary>
        /// Save - stream level
        /// </summary>
        /// <param name="blockRequestingFlush"></param>
        /// <param name="closingFlag">closing or flushing</param>
        internal void SaveStream(ZipIOLocalFileBlock blockRequestingFlush, bool closingFlag)
        {
            // Prevent recursion when propagating Flush or Disposed to our minions
            // because ZipIOFileItemStream.Flush calls us.
            if (_propagatingFlushDisposed)
                return;
            else
                _propagatingFlushDisposed = true;   // enter first time

            try
            {
                // redirect depending on our mode
                if (_openStreaming)
                {
                    StreamingSaveStream(blockRequestingFlush, closingFlag);
                }
                else
                    SaveContainer(false);
            }
            finally
            {
                // all done so restore state
                _propagatingFlushDisposed = false;
            }
        }
    
        /// <summary>
        /// Save - container level
        /// </summary>
        /// <param name="closingFlag">true if closing, false if flushing</param>
        internal void Save(bool closingFlag)
        {
            CheckDisposed();

            // Prevent recursion when propagating Flush or Disposed to our minions
            // because ZipIOFileItemStream.Flush calls us.
            if (_propagatingFlushDisposed)
                return;
            else
                _propagatingFlushDisposed = true;   // enter first time

            try
            {
                // redirect depending on our mode
                if (_openStreaming)
                {
                    StreamingSaveContainer(closingFlag);
                }
                else
                    SaveContainer(closingFlag);
            }
            finally
            {
                // all done so restore state
                _propagatingFlushDisposed = false;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="archiveStream">stream we operate on</param>
        /// <param name="streaming"></param>
        /// <param name="ownStream">true if we own the stream and are expected to close it when we are disposed</param>
        internal ZipIOBlockManager(Stream archiveStream, bool streaming, bool ownStream)
        {
            Debug.Assert(archiveStream != null);

            _archiveStream = archiveStream;
            _openStreaming = streaming;
            _ownStream = ownStream;

            if (streaming)
            {
                // wrap the archive stream in a WriteTimeStream which keeps track of current position
                _archiveStream = new WriteTimeStream(_archiveStream);
            }
            else if (archiveStream.Length > 0)
            {
                // for non-empty stream we need to map the whole stream into a raw data block 
                // which helps keep track of shifts and dirty areas 
                ZipIORawDataFileBlock rawBlock = ZipIORawDataFileBlock.Assign(this, 0, archiveStream.Length);

                _blockList.Add(rawBlock);
            }
        }

        internal static UInt32 ToMsDosDateTime(DateTime dateTime)
        {
            UInt32 result = 0;

            result |= (((UInt32)dateTime.Second) /2) & 0x1F;   // seconds need to be divided by 2
                                                                                // as they stored in 5 bits
            result |= (((UInt32)dateTime.Minute) & 0x3F) << 5;   
            result |= (((UInt32)dateTime.Hour) & 0x1F) << 11;

            result |= (((UInt32)dateTime.Day) & 0x1F) << 16;
            result |= (((UInt32)dateTime.Month) & 0xF) << 21;
            result |= (((UInt32)(dateTime.Year - 1980)) & 0x7F) << 25;

            return result;
        }

        internal static DateTime FromMsDosDateTime(UInt32 dosDateTime)
        {
            int seconds = (int)((dosDateTime & 0x1F) << 1); // seconds need to be multiplied by 2
                                                                                       // as they stored in 5 bits
            int minutes  = (int)((dosDateTime >> 5) & 0x3F);
            int hours = (int)((dosDateTime >> 11) & 0x1F);

            int day = (int)((dosDateTime >> 16) & 0x1F);
            int month  =(int)((dosDateTime >> 21) & 0xF);
            int year = (int)(1980 + ((dosDateTime >> 25) & 0x7F));

            //this will throw if parameters are out of range 
            return new DateTime(year, month,day,hours,minutes,seconds);
        }

        /// <summary>
        /// This is standard way to normalize Zip File Item names. At this point we only 
        /// getting rid of the spaces. The Exists calls are responsible or making sure 
        /// that they check for uniqueness in a case insensitive manner. It is up to the 
        /// higher levels to add stricter restrictions like URI character set, and so on.
        /// </summary>   
        static internal string ValidateNormalizeFileName(string zipFileName)
        {
            // Validate parameteres 
            if (zipFileName == null)
            {
                throw new ArgumentNullException("zipFileName");
            }

            if (zipFileName.Length > ZipIOBlockManager.MaxFileNameSize)
            {
                throw new ArgumentOutOfRangeException("zipFileName");
            }

            zipFileName = zipFileName.Trim();

            if (zipFileName.Length < 1)//it must be at least one character 
            {
                throw new ArgumentOutOfRangeException("zipFileName");
            }

            //Based on the  Appnote : 
            //    << The path stored should not contain a drive or device letter, or a leading slash.  >>
           
            return zipFileName;
        }

        //------------------------------------------------------
        //  Internal helper CopyBytes functions for storing data into a byte[]
        // it is a similar to a BinaryWriter , but not for streams but ratrher
        // for byte[]
        // These functiona used in the Extra field parsing, as that functionality is buit
        // in terms of byte[] not streams
        //------------------------------------------------------
        internal static int CopyBytes(Int16 value, byte[] buffer, int offset)
        {
            Debug.Assert(checked(buffer.Length-offset) >= sizeof(Int16));

             byte[] tempBuffer = BitConverter.GetBytes(value);  
            Array.Copy(tempBuffer, 0, buffer, offset, tempBuffer.Length);

            return offset + tempBuffer.Length;
        }

        internal static int CopyBytes(Int32 value, byte[] buffer, int offset)
        {
            Debug.Assert(checked(buffer.Length-offset) >= sizeof(Int32));

             byte[] tempBuffer = BitConverter.GetBytes(value);  
            Array.Copy(tempBuffer, 0, buffer, offset, tempBuffer.Length);

            return offset + tempBuffer.Length;
        }

        internal static int CopyBytes(Int64 value, byte[] buffer, int offset)
        {
            Debug.Assert(checked(buffer.Length-offset) >= sizeof(Int64));

             byte[] tempBuffer = BitConverter.GetBytes(value);  
            Array.Copy(tempBuffer, 0, buffer, offset, tempBuffer.Length);

            return offset + tempBuffer.Length;
        }
        
        internal static int CopyBytes(UInt16 value, byte[] buffer, int offset)
        {
            Debug.Assert(checked(buffer.Length-offset) >= sizeof(UInt16));

             byte[] tempBuffer = BitConverter.GetBytes(value);  
            Array.Copy(tempBuffer, 0, buffer, offset, tempBuffer.Length);

            return offset + tempBuffer.Length;
        }

        internal static int CopyBytes(UInt32 value, byte[] buffer, int offset)
        {
            Debug.Assert(checked(buffer.Length-offset) >= sizeof(UInt32));

             byte[] tempBuffer = BitConverter.GetBytes(value);  
            Array.Copy(tempBuffer, 0, buffer, offset, tempBuffer.Length);

            return offset + tempBuffer.Length;
        }

        internal static int CopyBytes(UInt64 value, byte[] buffer, int offset)
        {
            Debug.Assert(checked(buffer.Length-offset) >= sizeof(UInt64));

             byte[] tempBuffer = BitConverter.GetBytes(value);  
            Array.Copy(tempBuffer, 0, buffer, offset, tempBuffer.Length);

            return offset + tempBuffer.Length;
        }

        internal static UInt64 ConvertToUInt64(UInt32 loverAddressValue, UInt32 higherAddressValue)
        {
            return checked((UInt64)loverAddressValue + (((UInt64)higherAddressValue)  << 32));
        }

        internal static ZipIOVersionNeededToExtract CalcVersionNeededToExtractFromCompression
                                                                                    (CompressionMethodEnum compression)
        {
            switch (compression)
            {
                case CompressionMethodEnum.Stored: 
                        return ZipIOVersionNeededToExtract.StoredData;
                case CompressionMethodEnum.Deflated:
                        return ZipIOVersionNeededToExtract.DeflatedData;
                default:
                        throw new NotSupportedException();    // Deflated64 this is OFF 
            }
        }


        /// <summary>
        /// This is the common Pre Save notiofication handler for 
        ///  RawDataFile Block and File Item Stream 
        /// It makes assumption that the overlap generally start coming in at the beginning of a 
        /// large disk image, so we should only try to cache cache overlaped data in the prefix 
        /// of the disk block  
        /// Block can also return a value indicating whether PreSaveNotification should be extended to the blocks that are positioned after 
        /// it in the Block List. For example, if block has completely handled PreSaveNotification in a way that it cached the whole area that 
        /// was in danger (of being overwritten) it means that no blocks need to worry about this anymore. After all no 2 blocks should have 
        /// share on disk buffers. Another scenario is when block can determine that area in danger is positioned before the block's on disk 
        /// buffers; this means that all blocks that are positioned later in the block list do not need to worry about this PreSaveNotification 
        /// as their buffers should be positioned even further alone in the file. 
        /// </summary>                
        internal static PreSaveNotificationScanControlInstruction CommonPreSaveNotificationHandler(
                                                            Stream stream,
                                                            long offset, long size,
                                                            long onDiskOffset, long onDiskSize,
                                                            ref SparseMemoryStream cachePrefixStream)
        {
            checked
            {
                Debug.Assert(size >=0);
                Debug.Assert(offset >=0);
                Debug.Assert(onDiskSize >=0);
                Debug.Assert(onDiskOffset >=0);

                // trivial request 
                if (size == 0)
                {
                    // The area being overwritten is of size 0 so there is no need to notify any blocks about this.
                    return PreSaveNotificationScanControlInstruction.Stop;
                }

                if (cachePrefixStream != null)
                {   
                    // if we have something in cache prefix buffer  we only should check whatever tail data isn't cached
                    checked{onDiskOffset += cachePrefixStream.Length;}
                    checked{onDiskSize -= cachePrefixStream.Length;}
                    Debug.Assert(onDiskSize >=0);                    
                }

                if (onDiskSize == 0)
                {
                    // the raw data block happened to be fully cached 
                    // in this case (onDiskSize==0) can not be used as a reliable indicator of the position of the 
                    // on disk buffer relative to the other; it is just an indicator of an empty buffer which might have a meaningless offset 
                    // that shouldn't be driving any decisions
                    return PreSaveNotificationScanControlInstruction.Continue;
                }

                // we need to first find out if the raw data that isn't cached yet overlaps with any disk space 
                // that is about to be overriden 
                long overlapBlockOffset;
                long  overlapBlockSize;

                PackagingUtilities.CalculateOverlap(onDiskOffset, onDiskSize, 
                                           offset, size ,
                                            out overlapBlockOffset, out overlapBlockSize);
                if (overlapBlockSize <= 0)
                {
                    // No overlap , we can ignore this message.
                    // In addition to that, if (onDiskOffset > offset) it means that, given the fact that all blocks after 
                    // the current one will have even larger offsets, they couldn't possibly overlap with (offset ,size ) chunk .
                    return (onDiskOffset > offset) ?
                                                PreSaveNotificationScanControlInstruction.Stop  : 
                                                PreSaveNotificationScanControlInstruction.Continue;
                }

                // at this point we have an overlap, we need to read the data that is overlapped 
                // and merge it with whatever we already have in cache 
                // let's figure out the part that isn't cached yet, and needs to be  
                long blockSizeToCache;
                checked
                {
                    blockSizeToCache = overlapBlockOffset + overlapBlockSize - onDiskOffset;
                }
                Debug.Assert(blockSizeToCache >0); // there must be a non empty block at this point that needs to be cached 

                // We need to ensure that we do have a place to store this data 
                if (cachePrefixStream == null)
                {   
                    cachePrefixStream = new SparseMemoryStream(_lowWaterMark, _highWaterMark);                
                }
                else
                {
                    // if we already have some cached prefix data we have to make sure we are 
                    // appending new data tro the tail of the already cached chunk
                    cachePrefixStream.Seek(0, SeekOrigin.End); 
                }

                stream.Seek(onDiskOffset, SeekOrigin.Begin);            
                long bytesCopied = PackagingUtilities.CopyStream(stream, cachePrefixStream, blockSizeToCache, 4096);

                if (bytesCopied  != blockSizeToCache)
                {
                    throw new FileFormatException(SR.Get(SRID.CorruptedData));
                }

                // if the contdition below is true it means that, given the fact that all blocks after 
                // the current one will have even larger offsets, they couldn't possibly overlap with (offset ,size ) chunk 
                return ((onDiskOffset + onDiskSize) >=  (offset + size)) ?
                                            PreSaveNotificationScanControlInstruction.Stop  : 
                                            PreSaveNotificationScanControlInstruction.Continue;
            }
        }

        //------------------------------------------------------
        //
        //  Protected Methods  
        //
        //------------------------------------------------------
        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                // multiple calls are fine - just ignore them
                if (!_disposedFlag)
                {
                    // Prevent recursion into Save() when propagating Flush or Disposed to our minions
                    // because ZipIOFileItemStream.Flush calls us.
                    if (_propagatingFlushDisposed)
                        return;
                    else
                        _propagatingFlushDisposed = true;   // enter first time

                    try
                    {
                        try
                        {
                            if (_blockList != null)
                            {
                                foreach (IZipIOBlock block in _blockList)
                                {
                                    IDisposable disposableBlock = block as IDisposable;
                                    if (disposableBlock != null)
                                    {
                                        // only some Blocks are disposable, most are not 
                                        disposableBlock.Dispose();
                                    }
                                }
                            }
                        }
                        finally
                        {
                            // If we own the stream, we should close it.
                            // If not, we cannot even close the binary reader or writer as these close the
                            // underlying stream on us.
                            if (_ownStream)
                            {
                                if (_binaryReader != null)
                                {   // this one might be null of we have been only writing 
                                    _binaryReader.Close();
                                }

                                if (_binaryWriter != null)
                                {   // this one might be null of we have been only reading
                                    _binaryWriter.Close();
                                }

                                if (_archiveStream != null)
                                {
                                    _archiveStream.Close();
                                }
                            }
                        }
                    }
                    finally
                    {
                        _blockList = null;
                        _encoding = null;
                        _endOfCentralDirectoryBlock = null;
                        _centralDirectoryBlock = null;

                        _disposedFlag = true;
                        _propagatingFlushDisposed = false;   // reset
                    }
                }
            }
        }

        //------------------------------------------------------
        //
        //  Private Properties  
        //
        //------------------------------------------------------
        /// <summary>
        /// This property returns the index of CentralDirectoryBlock within _blockList
        /// </summary>                                
        private int CentralDirectoryBlockIndex
        {
            get
            {
                Invariant.Assert(_blockList.Count >= _requiredBlockCount);
                Debug.Assert(_centralDirectoryBlock != null
                                && _endOfCentralDirectoryBlock != null
                                && _zip64EndOfCentralDirectoryBlock != null
                                && _zip64EndOfCentralDirectoryLocatorBlock != null);

                // We always have following blocks at the end of the block lists:
                //  CD, Zip64 EOCD, Zip64 EOCD Locator, and EOCD
                // Thus the index of CD can be calculated from the total number of blocks
                //  and _requiredBlockCount which is 4
                return _blockList.Count - _requiredBlockCount;
            }
        }


        //------------------------------------------------------
        //
        //  Private Methods  
        //
        //------------------------------------------------------
        /// <summary>
        /// Throwes exception if object already Disposed/Closed. 
        /// </summary> 
        private void CheckDisposed()
        {
            if (_disposedFlag)
            {
                throw new ObjectDisposedException(null, SR.Get(SRID.ZipArchiveDisposed));            
            }
        }

        /// <summary>
        /// Save - container level
        /// </summary>
        /// <param name="closingFlag">true if closing, false if flushing</param>
        private void SaveContainer(bool closingFlag)
        {
            CheckDisposed();
            Debug.Assert(!_openStreaming, "Not legal in Streaming mode");


            if (!closingFlag && !DirtyFlag)
            {
                // we are trying to save some cycles in the case of the subsequent Flush calls
                // that do not close the container
                // if it is being closed DirtyFlag isn't reliable as the Compressed streams carry 
                // some extra bytes after flushing and only write them out on closing.  
                return;
            }

            // We need a separate cycle to update all the cross block references prior to saving blocks
            // specifically the central directory needs "dirty" information from blocks in order to properly 
            // update it's references, otherwise (if we call UpdateReferences and Save in the same loop) 
            // information about block shifts will be lost by the time we ask central directory to update it's 
            // references   

            // offset of the first block
            long currentOffset = 0; // ZIP64 review type here 
            foreach (IZipIOBlock currentBlock in _blockList)
            {
                // move block so it is positioned right after the previous block 
                currentBlock.Move(currentOffset - currentBlock.Offset);

                // this will update references and as well as other internal structures (size)
                // specifically for the FileItemBlock it will flush buffers of 
                // all the outstanding streams 
                currentBlock.UpdateReferences(closingFlag);

                //advance current stream position according to the size of the block 
                checked{currentOffset += currentBlock.Size;}
            }

            // save dirty blocks 
            bool dirtyBlockFound = false;

            int blockListCount  = _blockList.Count;
            for (int i = 0; i < blockListCount ; i++)
            {
                IZipIOBlock currentBlock = (IZipIOBlock)_blockList[i];

                if (currentBlock.GetDirtyFlag(closingFlag))
                {
                    dirtyBlockFound = true;

                    long currentBlockOffset = currentBlock.Offset;
                    long currentBlockSize = currentBlock.Size;

                    if (currentBlockSize > 0)
                    {
                        // before saving we need to warn all the blocks that have still some data on disk
                        // that might be overriden   
                        // second loop must start at the current position of the extrrnal loop
                        // as all the items before have been saved 
                        for (int j = i + 1; j < blockListCount; j++)
                        {
                            // This is an optimization which enabled us to stop going through the 
                            // tail blocks as soon as we find a block that returns a status indicating 
                            // that it took care of the tail of the target area or is positioned after the 
                            // target area.
                            if (((IZipIOBlock)_blockList[j]).PreSaveNotification(currentBlockOffset, currentBlockSize) == 
                                        PreSaveNotificationScanControlInstruction.Stop )
                            {
                                break;
                            }
                        }
                    }

                    currentBlock.Save();    // Even if currentBlockSize == 0, call Save to clear DirtyFlag
                }
            }

            // originally we have had an assert for the case when no changes were made to the file 
            // but calculated size didn't match the actual stream size.
            // As a result of the XPS Viewer dynamically switching streams underneath ZIP IO, we 
            // need to treat this case as a normal non-dirty scenario. So if nothing changed and 
            // nothing was written out we shouldn't even validate whether stream underneath 
            // was modified in any way or not (even such simple modifications as an unexpected 
            // Stream.Length change). If it was modified by someone we assume that the stream 
            // owner was aware of it's action.
            if (dirtyBlockFound && (Stream.Length > currentOffset))
            {
                Stream.SetLength(currentOffset);
            }

            Stream.Flush();
            DirtyFlag = false;
        }

        /// <summary>
        /// Streaming version of Save routine
        /// </summary>
        /// <param name="closingFlag">true if closing the package</param>
        private void StreamingSaveContainer(bool closingFlag)
        {
            // STREAMING Mode:
            //   NOTE: _blockList is NOT in offset order except the last four blocks
            //      (CD, Zip64 EOCD, Zip64 EOCD Locator, and EOCD)

            try
            {
                // save dirty blocks 
                long currentOffset = 0;
                for (int i = 0; i < _blockList.Count; i++)
                {
                    IZipIOBlock currentBlock = (IZipIOBlock)_blockList[i];
                    ZipIOLocalFileBlock localFileBlock = currentBlock as ZipIOLocalFileBlock;

                    if (localFileBlock == null)
                    {
                        if (closingFlag)
                        {
                            // Move block so it is positioned right after the previous block.
                            // No need for nested loops like in SaveContainer because none of these
                            // calls can cause a block to move in the Streaming case.
                            currentBlock.Move(currentOffset - currentBlock.Offset);
                            currentBlock.UpdateReferences(closingFlag);
                            if (currentBlock.GetDirtyFlag(closingFlag))
                            {
                                currentBlock.Save();
                            }
                        }
                    }
                    else if (currentBlock.GetDirtyFlag(closingFlag))
                    {
                        // no need to call UpdateReferences in streaming mode for regular
                        // local file blocks because
                        // we manually emit the local file header and the local file descriptor
                        localFileBlock.SaveStreaming(closingFlag);
                    }
                    checked{currentOffset += currentBlock.Size;}
                }

                Stream.Flush();
            }
            finally
            {
                // all done so restore state
                _propagatingFlushDisposed = false;
            }
        }

        /// <summary>
        /// Flush was called on a ZipIOFileItemStream
        /// </summary>
        /// <param name="blockRequestingFlush">block that owns the stream that Flush was called on</param>
        /// <param name="closingFlag">close or dispose</param>
        private void StreamingSaveStream(ZipIOLocalFileBlock blockRequestingFlush, bool closingFlag)
        {
            // STREAMING MODE:
            // Flush will do one of two things, depending on the currently open stream:
            // 1) If the currently open stream matches the one passed (or none is currently opened)
            //    then write will occur to the open stream.
            // 2) Otherwise, the currently opened stream will be flushed and closed, and the
            //    given stream will become the currently opened stream
            // NOTE: _blockList is NOT in offset order except the last four blocks
            //      (CD, Zip64 EOCD, Zip64 EOCD Locator, and EOCD)

            // different stream?
            if (_streamingCurrentlyOpenStreamBlock != blockRequestingFlush)
            {
                // need to close the currently opened stream 
                // unless its our first time through
                if (_streamingCurrentlyOpenStreamBlock != null)
                {
                    _streamingCurrentlyOpenStreamBlock.SaveStreaming(true);
                }

                // Now make the given stream the new "currently opened stream".
                _streamingCurrentlyOpenStreamBlock = blockRequestingFlush;
            }

            // this should now be flushable/closable
            _streamingCurrentlyOpenStreamBlock.SaveStreaming(closingFlag);

            // if closing - discard the stream because it is now closed
            if (closingFlag)
                _streamingCurrentlyOpenStreamBlock = null;
        }

        private void  CreateCentralDirectoryBlock()  
        {
            CheckDisposed();
            Debug.Assert(_zip64EndOfCentralDirectoryBlock != null);

            // It must not be loaded yet 
            Debug.Assert(!IsCentralDirectoryBlockLoaded);

            // The proper position is just before the Zip64EndOfCentralDirectoryRecord
            // Zip64EndOfCentralDirectoryRecord - might be of size 0 (if file is small enough)
            int blockPosition = _blockList.IndexOf(Zip64EndOfCentralDirectoryBlock);
            Debug.Assert(blockPosition >= 0);
            
            // construct Block find it and parse it 
            _centralDirectoryBlock = ZipIOCentralDirectoryBlock.CreateNew(this);

            //ask block manager to insert this this block             
            InsertBlock(blockPosition , _centralDirectoryBlock); 
        }
        
        private void LoadCentralDirectoryBlock()  
        {
            Debug.Assert(_centralDirectoryBlock == null);
            Debug.Assert(!_openStreaming, "Not legal in Streaming mode");

            // construct Block find it and parse it 
            _centralDirectoryBlock = ZipIOCentralDirectoryBlock.SeekableLoad(this);

            //ask block manager to MAP this block 
            MapBlock(_centralDirectoryBlock);
        }
        
        private void  CreateLoadZip64Blocks()
        {
            CheckDisposed();

            Debug.Assert ((_zip64EndOfCentralDirectoryBlock == null) && 
                                  (_zip64EndOfCentralDirectoryLocatorBlock == null));

            // determine whether we want to create it or load it 
            // this check doesn't provide us with a 100% guarantee. 
            // After discussion we have agreed that this should be sufficient 
            if (!Streaming && EndOfCentralDirectoryBlock.ContainValuesHintingToPossibilityOfZip64 &&
                ZipIOZip64EndOfCentralDirectoryLocatorBlock.SniffTheBlockSignature(this))
            {
                // attempt to sniff the header of the  
                LoadZip64EndOfCentralDirectoryLocatorBlock();
                LoadZip64EndOfCentralDirectoryBlock();
            }
            else
            {
                // We delayed validation of some values in End of Central Directory that can give possible
                //  hints for Zip64; Since there is no Zip64 structure, we need to validate them here
                _endOfCentralDirectoryBlock.ValidateZip64TriggerValues();

                CreateZip64EndOfCentralDirectoryLocatorBlock();
                CreateZip64EndOfCentralDirectoryBlock();
            }
        }

        private void  CreateZip64EndOfCentralDirectoryBlock()
        {
            Debug.Assert(_zip64EndOfCentralDirectoryBlock == null);

            // The proper position is just before the Zip64EndOfCentralDirectoryRecordLocator
            // Zip64EndOfCentralDirectoryRecord - might be of size 0 (if file is small enough)
            int blockPosition = _blockList.IndexOf(Zip64EndOfCentralDirectoryLocatorBlock);

            // construct Block find it and parse it 
            _zip64EndOfCentralDirectoryBlock = ZipIOZip64EndOfCentralDirectoryBlock.CreateNew(this);

            //ask block manager to insert this this block 
            InsertBlock(blockPosition, _zip64EndOfCentralDirectoryBlock);
        }

        private void  LoadZip64EndOfCentralDirectoryBlock()
        {
            Debug.Assert(_zip64EndOfCentralDirectoryBlock == null);
            Debug.Assert(!_openStreaming, "Not legal in Streaming mode");

            // construct Block find it and parse it 
            _zip64EndOfCentralDirectoryBlock = ZipIOZip64EndOfCentralDirectoryBlock.SeekableLoad(this);

            //ask block manager to insert this this block 
            MapBlock(_zip64EndOfCentralDirectoryBlock);
        }
        
        private void  CreateZip64EndOfCentralDirectoryLocatorBlock()
        {
            Debug.Assert(_zip64EndOfCentralDirectoryLocatorBlock == null);

            // The proper position is just before the EOCD 
            int blockPosition = _blockList.IndexOf(EndOfCentralDirectoryBlock);

            // construct Block find it and parse it 
            _zip64EndOfCentralDirectoryLocatorBlock = ZipIOZip64EndOfCentralDirectoryLocatorBlock.CreateNew(this);

            //ask block manager to MAP this block 
            InsertBlock(blockPosition, _zip64EndOfCentralDirectoryLocatorBlock);
        }

        private void  LoadZip64EndOfCentralDirectoryLocatorBlock()
        {
            Debug.Assert(_zip64EndOfCentralDirectoryLocatorBlock == null);
            Debug.Assert(!_openStreaming, "Not legal in Streaming mode");

            // construct Block find it and parse it 
            _zip64EndOfCentralDirectoryLocatorBlock = ZipIOZip64EndOfCentralDirectoryLocatorBlock.SeekableLoad(this);

            //ask block manager to MAP this block 
            MapBlock(_zip64EndOfCentralDirectoryLocatorBlock);
        }
                            
        private void MapBlock(IZipIOBlock block)
        {
            // as we map a block to existing file space it must be not dirty.
            Debug.Assert(!block.GetDirtyFlag(true)); // closingFlag==true used as a more conservative option
            Debug.Assert(!_openStreaming, "Not legal in Streaming mode");

            for (int blockIndex = _blockList.Count - 1; blockIndex >= 0; --blockIndex)
            {
                // if we need to find a RawDataBlock that maps to the target area 
                ZipIORawDataFileBlock rawBlock = _blockList[blockIndex] as ZipIORawDataFileBlock;

                //check the original loaded RawBlock size / offset against the new block 
                if ((rawBlock != null) && rawBlock.DiskImageContains(block))
                {
                    ZipIORawDataFileBlock prefixBlock, suffixBlock;

                    //split raw block into prefixRawBlock, SuffixRawBlock
                    rawBlock.SplitIntoPrefixSuffix(block, out prefixBlock, out suffixBlock);

                    _blockList.RemoveAt(blockIndex); // remove the old big raw data block

                    // add suffix Raw data block 
                    if (suffixBlock != null)
                    {
                        _blockList.Insert(blockIndex, suffixBlock);
                    }

                    // add new mapped block 
                    _blockList.Insert(blockIndex, block);

                    // add prefix Raw data block 
                    if (prefixBlock != null)
                    {
                        _blockList.Insert(blockIndex, prefixBlock);
                    }

                    return;
                }
            }

            // we couldn't find a raw data block for mapping this, we can only throw
            throw new FileFormatException(SR.Get(SRID.CorruptedData));
        }

        private void InsertBlock(int blockPosition, IZipIOBlock block)
        {
            // as we are adding a new block it must be dirty unless its size is 0
            Debug.Assert(block.GetDirtyFlag(true) ||   // closingFlag==true used as a more conservative option
                                    block.Size == 0); 

            _blockList.Insert(blockPosition, block);
        }

        private void AppendBlock(IZipIOBlock block)
        {
            // as we are adding a new block it must be dirty unless its size is 0
            Debug.Assert(block.GetDirtyFlag(true) || // closingFlag==true used as a more conservative option
                                    block.Size == 0);

            // CentralDirectory persistence logic relies on the fact that we always add headers in a fashion that
            // matches the order of the corresponding file items in the physical archive (currently to the end of the list).
            // If this invariant is violated, the corresponding central directory persistence logic must be updated.
            _blockList.Add(block);
        }

        // this flag is used for Perf reasons, it doesn't carry any additional information that isn't stored somewhere 
        // else. In order to prevent complex dirty calculations on the sequential flush calls, we are going to keep 
        // this flag which will be set to true at the end of the flush (or close). This flag will be set from the ZipArchive 
        // and CrcCalculating entry points that can potentially make our structure dirty.
        // This flag is only used for non-streaming cases. In streaming cases we do not believe there is a perf
        // penalty of that nature. 
        private bool _dirtyFlag = false;
        
        private bool _disposedFlag;
        private bool _propagatingFlushDisposed;              // if true, we ignore calls back to Save to prevent recursion
        private Stream _archiveStream;
        private bool _openStreaming;
        private bool _ownStream;                                    // true if we own the archive stream

        // Streaming Mode Only: stream that is currently able to write without interfering with other streams
        private ZipIOLocalFileBlock _streamingCurrentlyOpenStreamBlock;

        private BinaryReader _binaryReader;
        private BinaryWriter _binaryWriter;

        private const int _initialBlockListSize = 50;
        private ArrayList _blockList = new ArrayList(_initialBlockListSize); 

        private ASCIIEncoding _encoding = new ASCIIEncoding();

        ZipIOZip64EndOfCentralDirectoryBlock  _zip64EndOfCentralDirectoryBlock;
        ZipIOZip64EndOfCentralDirectoryLocatorBlock _zip64EndOfCentralDirectoryLocatorBlock; 
        ZipIOEndOfCentralDirectoryBlock _endOfCentralDirectoryBlock;
        ZipIOCentralDirectoryBlock _centralDirectoryBlock;

        private const long _lowWaterMark = 0x19000;                 // we definately would like to keep everythuing under 100 KB in memory  
        private const long _highWaterMark = 0xA00000;   // we would like to keep everything over 10 MB on disk
        private const int _requiredBlockCount = 4;      // We always have following blocks: CD, Zip64 EOCD, Zip64 EOCD Locator, and EOCD
                                                       // This value is used to calculate the index of CD within _blockList
    }
} 

