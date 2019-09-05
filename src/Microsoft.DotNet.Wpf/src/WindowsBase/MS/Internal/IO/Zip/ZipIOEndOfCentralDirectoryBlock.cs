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
using System.Runtime.Serialization;
using System.Windows;
using MS.Internal.IO.Packaging;         // for PackagingUtilities
using MS.Internal.WindowsBase;

namespace MS.Internal.IO.Zip
{
    internal class ZipIOEndOfCentralDirectoryBlock : IZipIOBlock
    {   
        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
        // standard IZipIOBlock functionality
        public long Offset
        {
            get
            {
                return _offset;
            }
        }

        public long Size
        {
            get
            {
                return _fixedMinimalRecordSize + _zipFileCommentLength;
            }
        }

        public bool GetDirtyFlag(bool closingFlag)
        {
            return _dirtyFlag;
        }

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
        public void Move(long shiftSize)
        {
            if (shiftSize != 0)
            {
                checked{_offset +=shiftSize;}
                _dirtyFlag = true;
                Debug.Assert(_offset >=0);                
            }
        }

        public void Save()
        {
            if (GetDirtyFlag(true)) 
            {
                BinaryWriter writer = _blockManager.BinaryWriter;

                // never seek in streaming mode
                if (!_blockManager.Streaming && _blockManager.Stream.Position != _offset)
                {
                    // we need to seek 
                    _blockManager.Stream.Seek(_offset, SeekOrigin.Begin);
                }

                writer.Write(_signatureConstant);
                writer.Write(_numberOfThisDisk);
                writer.Write(_numberOfTheDiskWithTheStartOfTheCentralDirectory);
                writer.Write(_totalNumberOfEntriesInTheCentralDirectoryOnThisDisk);
                writer.Write(_totalNumberOfEntriesInTheCentralDirectory);
                writer.Write(_sizeOfTheCentralDirectory);
                writer.Write(_offsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber);
                writer.Write(_zipFileCommentLength);
                if (_zipFileCommentLength > 0)
                {
                    writer.Write(_zipFileComment, 0,  _zipFileCommentLength);                
                }
                writer.Flush();
    
                _dirtyFlag = false;                
            }
        }

        public void UpdateReferences(bool closingFlag)
        {
            // check whether Central directory is loaded and update references accordingly
            //  if one or more of the following conditions are true
            //  1. Central Directory is dirty
            //  2. Zip64 End of Central Directory is dirty
            //  3. Zip64 End of Central Directory Locator is dirty
            //  4. streaming mode
            // if Central Directory isn't loded or none of the relevant structure is dirty,
            //  there is nothing to update for End Of Central directory record 
            if (_blockManager.IsCentralDirectoryBlockLoaded
                    && (_blockManager.Streaming
                        || _blockManager.CentralDirectoryBlock.GetDirtyFlag(closingFlag)
                        || _blockManager.Zip64EndOfCentralDirectoryBlock.GetDirtyFlag(closingFlag)
                        || _blockManager.Zip64EndOfCentralDirectoryLocatorBlock.GetDirtyFlag(closingFlag)))
            {
                // intialize them to zIP64 case, and update them if needed 
                UInt16 centralDirCount = UInt16.MaxValue;
                UInt32 centralDirBlockSize = UInt32.MaxValue;
                UInt32 centralDirOffset = UInt32.MaxValue;
                UInt16 numberOfTheDiskWithTheStartOfTheCentralDirectory = 0;
                UInt16 numberOfThisDisk = 0;
                

                // If we don't need Zip 64 struture
                if (!_blockManager.CentralDirectoryBlock.IsZip64BitRequiredForStoring)
                {
                    // if it isn't zip 64 let's get the data out 
                    centralDirCount = (UInt16)_blockManager.CentralDirectoryBlock.Count;
                    centralDirBlockSize = (UInt32)_blockManager.CentralDirectoryBlock.Size;
                    centralDirOffset = (UInt32)_blockManager.CentralDirectoryBlock.Offset;
                }

                // update value and mark record dirty if either it is already dirty or there is a mismatch
                if ((_dirtyFlag) || 
                    (_totalNumberOfEntriesInTheCentralDirectoryOnThisDisk != centralDirCount) ||
                    (_totalNumberOfEntriesInTheCentralDirectory != centralDirCount ) ||
                    (_sizeOfTheCentralDirectory != centralDirBlockSize) ||
                    (_offsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber != centralDirOffset) ||
                    (_numberOfTheDiskWithTheStartOfTheCentralDirectory != numberOfTheDiskWithTheStartOfTheCentralDirectory) ||
                    (_numberOfThisDisk != numberOfThisDisk))
                {
                    _totalNumberOfEntriesInTheCentralDirectoryOnThisDisk = centralDirCount;
                    _totalNumberOfEntriesInTheCentralDirectory = centralDirCount;
                    _sizeOfTheCentralDirectory = centralDirBlockSize;
                    _offsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber = centralDirOffset;
                    _numberOfTheDiskWithTheStartOfTheCentralDirectory = numberOfTheDiskWithTheStartOfTheCentralDirectory;
                    _numberOfThisDisk = numberOfThisDisk;
                    
                    _dirtyFlag = true;
                }
            }
        }

        public PreSaveNotificationScanControlInstruction PreSaveNotification(long offset, long size)
        {
            // we can safely ignore this notification as we do not keep any data 
            // after parsing on disk. Everything is in memory, it is ok to override 
            // original End of Central directory without any additional backups

            // we can also safely state that there is no need to continue the PreSafeNotification loop 
            // as there shouldn't be any blocks after the EOCD 
            return PreSaveNotificationScanControlInstruction.Stop;
        }
        
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        internal static ZipIOEndOfCentralDirectoryBlock SeekableLoad (ZipIOBlockManager blockManager)  
        {
            // perform custom serach for record 
            long blockPosition = FindPosition(blockManager.Stream);
            blockManager.Stream.Seek(blockPosition, SeekOrigin.Begin);

            ZipIOEndOfCentralDirectoryBlock block = new ZipIOEndOfCentralDirectoryBlock(blockManager);
            
            block.ParseRecord(blockManager.BinaryReader, blockPosition);
            return block;
        }
        
        internal static ZipIOEndOfCentralDirectoryBlock CreateNew(ZipIOBlockManager blockManager, long offset)          
        {
            ZipIOEndOfCentralDirectoryBlock block = new ZipIOEndOfCentralDirectoryBlock(blockManager);

            block._offset = offset;
            block._dirtyFlag = true;

            return block;
        }

        internal void ValidateZip64TriggerValues() 
        {
            if ((_offsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber > _offset) 
                ||
                ((_offsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber == _offset) &&
                (_totalNumberOfEntriesInTheCentralDirectoryOnThisDisk > 0)))
            {
                // central directory must start prior to the offset of the end of central directory.
                // the only exception is when size of the central directory is 0 
                throw new FileFormatException(SR.Get(SRID.CorruptedData));
            }

            if ((_numberOfThisDisk != 0) ||
                (_numberOfTheDiskWithTheStartOfTheCentralDirectory != 0) ||
                (_totalNumberOfEntriesInTheCentralDirectoryOnThisDisk != 
                                                    _totalNumberOfEntriesInTheCentralDirectory))
            {
                throw new NotSupportedException(SR.Get(SRID.NotSupportedMultiDisk));
            }
        }
        
        internal uint NumberOfThisDisk
        {
            get
            {
                return _numberOfThisDisk;
            }
        }

        internal uint NumberOfTheDiskWithTheStartOfTheCentralDirectory
        {
            get
            {
                return _numberOfTheDiskWithTheStartOfTheCentralDirectory;
            }
        }

        internal uint TotalNumberOfEntriesInTheCentralDirectoryOnThisDisk 
        {
            get
            {
                return _totalNumberOfEntriesInTheCentralDirectoryOnThisDisk ;
            }
        }

        internal uint TotalNumberOfEntriesInTheCentralDirectory
        {
            get
            {
                return _totalNumberOfEntriesInTheCentralDirectory;
            }
        }

        internal uint SizeOfTheCentralDirectory
        {
            get
            {
                return _sizeOfTheCentralDirectory;
            }
        }
        
        internal uint OffsetOfStartOfCentralDirectory
        {
            get
            {
                return _offsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber;
            }
        }
#if false
        internal string Comment
        {
            get
            {
                return _stringZipFileComment;
            }
        }        
#endif

        internal bool ContainValuesHintingToPossibilityOfZip64
        {
            get
            {
                return ((_numberOfThisDisk == UInt16.MaxValue) ||
                            (_numberOfTheDiskWithTheStartOfTheCentralDirectory == UInt16.MaxValue) ||
                            (_totalNumberOfEntriesInTheCentralDirectoryOnThisDisk == UInt16.MaxValue) ||
                            (_totalNumberOfEntriesInTheCentralDirectory == UInt16.MaxValue) ||
                            (_sizeOfTheCentralDirectory == UInt32.MaxValue) ||
                            (_offsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber == UInt32.MaxValue));
            }
        }              

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        private ZipIOEndOfCentralDirectoryBlock(ZipIOBlockManager blockManager)
        {
            Debug.Assert(blockManager != null);
            _blockManager= blockManager;
        }

        private static long FindPosition(Stream archiveStream)
        {
            Debug.Assert(archiveStream.CanSeek);
            byte [] buffer = new byte[_scanBlockSize + _fixedMinimalRecordSize];
            long streamLength = archiveStream.Length;
            
            for(long endPos = streamLength; endPos > 0; endPos -= _scanBlockSize) 
            {
                // calculate offset position of the block to be read based on the end 
                // Position loop variable  
                long beginPos = Math.Max(0, endPos -_scanBlockSize);

                //read the block 
                archiveStream.Seek(beginPos, SeekOrigin.Begin);

                // the reads that we do actually overlap each other by the size == _fixedMinimalRecordSize
                // this is done in order to simplify our searching logic, this way we do not need to specially 
                // process matches that cross buffer boundaries, as we are guaranteed that if match is present 
                // it falls completely inside one of the buffers, as a result of overlapping in the read requests
                int bytesRead = PackagingUtilities.ReliableRead(archiveStream, buffer, 0, buffer.Length);

                // We need to pass this parameter into the function, so it knows
                // the relative positon of the buffer in regard to the end of the stream; 
                // it needs this info in order to checke whether the candidate record 
                // has length of Comment field consistent with the postion of the record
                long distanceFromStartOfBufferToTheEndOfStream = streamLength -beginPos;
                for(int i = bytesRead - _fixedMinimalRecordSize; i>=0; i--)
                {
                    if (IsPositionMatched(i, buffer, distanceFromStartOfBufferToTheEndOfStream))
                    {
                        return beginPos + i;
                    }
                }
            }

            // At this point we have finished scanning the file and haven't find anything
            throw new FileFormatException(SR.Get(SRID.CorruptedData));
        }


        private static bool IsPositionMatched (int pos, byte[] buffer, long bufferOffsetFromEndOfStream)
        {
            Debug.Assert(buffer != null);
            
            Debug.Assert(buffer.Length >= _fixedMinimalRecordSize); // the end of central directory record must fit in there 

            Debug.Assert(pos <= buffer.Length - _fixedMinimalRecordSize); // enough space to fit the record after pos
            
            Debug.Assert(bufferOffsetFromEndOfStream >= _fixedMinimalRecordSize); // there is no reason to start searching for the record 
                                                                                                            // after less than 22 byrtes left till the end of stream 

            for(int i = 0; i<_signatureBuffer.Length; i++) 
            {
                if (_signatureBuffer[i] !=  buffer[pos+i])
                {
                    //signature mismatch
                    return false;
                }
            }

            //we got signature matching, let's see if we can get comment length to match 
            // to handle little endian order of the bytes in the 16 bit length 
            long commentLengthFromRecord = buffer[pos + _fixedMinimalRecordSize-2] + 
                                            (buffer[pos + _fixedMinimalRecordSize-1] << 8);

            long commentLengthFromPos = bufferOffsetFromEndOfStream - pos - _fixedMinimalRecordSize; 
            if (commentLengthFromPos != commentLengthFromRecord) 
            {
                return false;
            }

            return true;            
        }

        private void ParseRecord (BinaryReader reader, long position)
        {
            _signature = reader.ReadUInt32();
            _numberOfThisDisk = reader.ReadUInt16();
            _numberOfTheDiskWithTheStartOfTheCentralDirectory = reader.ReadUInt16();
            _totalNumberOfEntriesInTheCentralDirectoryOnThisDisk = reader.ReadUInt16();
            _totalNumberOfEntriesInTheCentralDirectory = reader.ReadUInt16();
            _sizeOfTheCentralDirectory = reader.ReadUInt32();
            _offsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber = reader.ReadUInt32();
            _zipFileCommentLength = reader.ReadUInt16();
            _zipFileComment = reader.ReadBytes(_zipFileCommentLength);

            _stringZipFileComment = _blockManager.Encoding.GetString(_zipFileComment);

            _offset = position;

            _dirtyFlag = false;

            Validate();                
        }

        // Do minimum validatation here
        //  The rest of validation on the fields that can indicate the possiblity of Zip64 will be validated later
        // If there is the zip64 End of Central Directory, thoses values will be valided
        //  by ZipIO64EndOfCentralDirectoryBlock
        // Otherwise it will be validated in ZipIoBlockManager when it tries load ZipIO64EndOfCentralDirectoryBlock
        // In all of the supported scenarios we always try to load ZipIO64EndOfCentralDirectoryBlock immediately
        //  after it loads ZipIOEndOfCentralDirectoryBlock; so there is not much difference in the timing of
        //  the validation
        private void Validate() 
        {
            if (_signature != _signatureConstant)
            {
                throw new FileFormatException(SR.Get(SRID.CorruptedData));
            }

            if (_zipFileCommentLength != _zipFileComment.Length)
            {
                throw new FileFormatException(SR.Get(SRID.CorruptedData));
            }
        }
        
        //------------------------------------------------------
        //
        //  Private Members
        //
        //------------------------------------------------------
        // constant that is used for locating EndOf record signature 
        private static byte [] _signatureBuffer = new byte[] {0x50, 0x4b, 0x05, 0x06};
        
        // this blocks size is used to read data thro the tail of stream block by block  
        private static int _scanBlockSize = 0x01000; 
            
        private ZipIOBlockManager _blockManager;
        
        private long _offset;
        private bool  _dirtyFlag;        

        private  const UInt32 _signatureConstant  = 0x06054b50;
        private const int _fixedMinimalRecordSize = 22;
        
        // data persisted on disk 
        private  UInt32 _signature = _signatureConstant;
        private  UInt16 _numberOfThisDisk;
        private  UInt16 _numberOfTheDiskWithTheStartOfTheCentralDirectory;
        private  UInt16 _totalNumberOfEntriesInTheCentralDirectoryOnThisDisk;
        private  UInt16 _totalNumberOfEntriesInTheCentralDirectory;
        private  UInt32 _sizeOfTheCentralDirectory;
        private  UInt32 _offsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber;
        private  UInt16 _zipFileCommentLength;
        private  byte[] _zipFileComment;
        private string _stringZipFileComment;
    }
}

