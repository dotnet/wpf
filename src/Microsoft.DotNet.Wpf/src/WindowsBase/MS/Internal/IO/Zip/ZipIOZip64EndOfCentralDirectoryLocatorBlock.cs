// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
//
// Description:
//  This is an internal class that enables interactions with Zip archives
//  for OPC scenarios (Zip 64 bit support)
//
//
//
//

using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Windows;  
using MS.Internal.WindowsBase;

namespace MS.Internal.IO.Zip
{
    internal class ZipIOZip64EndOfCentralDirectoryLocatorBlock : IZipIOBlock
    {
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
                return _size;
            }
        }

        // This property will only return reliable result if Update is called prior  
        public bool GetDirtyFlag(bool closingFlag)        
        {
            return _dirtyFlag;
        }

        public void Move(long shiftSize)
        {
            if (shiftSize != 0)
            {
                checked{_offset +=shiftSize;}

                if (_size > 0)
                {
                    _dirtyFlag = true;
                }

                Debug.Assert(_offset >=0);                
            }
        }

        public void Save()
        {
            if (GetDirtyFlag(true) && (Size > 0))
            {
                BinaryWriter writer = _blockManager.BinaryWriter;
                if (_blockManager.Stream.Position != _offset)
                {
                    // we need to seek 
                    _blockManager.Stream.Seek(_offset, SeekOrigin.Begin);

                    // in non seekable streams we are expected to be at the right position, no seeking required 
                    // if this assumption is ever violated. Seek will throw on non-seekable stream, which would provide us 
                    // with a detection mechanism for such problems 
                }
                
                writer.Write(_signatureConstant);
                writer.Write(_numberOfTheDiskWithTheStartOfZip64EndOfCentralDirectory); 
                writer.Write(_offsetOfStartOfZip64EndOfCentralDirectoryRecord); 
                writer.Write(_totalNumberOfDisks); 
                
                writer.Flush();
            }

            _dirtyFlag = false;
        }

        public void UpdateReferences(bool closingFlag)
        {
            checked
            {
                // check whether Central directory is loaded and update references accordingly 
                //  if one or more of the following conditions are true
                //  1. Central Directory is dirty
                //  2. Zip64 End of Central Directory is dirty
                //  3. streaming mode
                // if Central Directory isn't loded or none of the relevant structure is dirty,
                //  there is nothing to update for Zip64 End Of Central directory record 
                if ((_blockManager.IsCentralDirectoryBlockLoaded
                            && (_blockManager.Streaming
                                || _blockManager.CentralDirectoryBlock.GetDirtyFlag(closingFlag))
                                || _blockManager.Zip64EndOfCentralDirectoryBlock.GetDirtyFlag(closingFlag)))
                {
                    if (_blockManager.CentralDirectoryBlock.IsZip64BitRequiredForStoring)
                    {
                        UInt64 offsetOfStartOfZip64EndOfCentralDirectoryRecord = 
                                                (UInt64)_blockManager.Zip64EndOfCentralDirectoryBlock.Offset;

                        // update value and mark record dirty if either it is already dirty or there is a mismatch
                        if ((_dirtyFlag) || 
                            (offsetOfStartOfZip64EndOfCentralDirectoryRecord != _offsetOfStartOfZip64EndOfCentralDirectoryRecord) ||
                            (_fixedMinimalRecordSize != _size))
                        {
                            _offsetOfStartOfZip64EndOfCentralDirectoryRecord = offsetOfStartOfZip64EndOfCentralDirectoryRecord;
                            _size = _fixedMinimalRecordSize;
                            
                            _dirtyFlag = true;
                        }
                    }
                    else
                    {
                        // we do not need zip 64 structures
                        if (_size != 0)
                        {
                            _size = 0;
                            _dirtyFlag = true;
                        }
                    }
                }
            }
        }

        public PreSaveNotificationScanControlInstruction PreSaveNotification(long offset, long size)
        {
            // we can safely ignore this notification as we do not keep any data 
            // after parsing on disk. Everything is in memory, it is ok to override 
            // original End of Central directory without any additional backups

            // we can also safely state that there is no need to continue the PreSafeNotification loop 
            // as the only blocks after the Zip64 EOCD (EOCD) doesn't have 
            // data that is buffered on disk
            return PreSaveNotificationScanControlInstruction.Stop;
        }
        
        internal static ZipIOZip64EndOfCentralDirectoryLocatorBlock SeekableLoad (ZipIOBlockManager blockManager)  
        {
            // This Debug assert is a secondary check in debug builds, callers are responcible for verifying condition 
            // in both retail and debug builds
            Debug.Assert(SniffTheBlockSignature(blockManager));
            
            long blockPosition = checked(blockManager.EndOfCentralDirectoryBlock.Offset - _fixedMinimalRecordSize);
            
            blockManager.Stream.Seek(blockPosition, SeekOrigin.Begin);

            ZipIOZip64EndOfCentralDirectoryLocatorBlock block = new ZipIOZip64EndOfCentralDirectoryLocatorBlock(blockManager);
            
            block.ParseRecord(blockManager.BinaryReader, blockPosition);
            return block;
        }
        
        internal static ZipIOZip64EndOfCentralDirectoryLocatorBlock CreateNew(ZipIOBlockManager blockManager)          
        {
            // This Debug assert is a secondary check in debug builds, callers are responcible for verifying condition 
            // in both retail and debug builds
            Debug.Assert(!SniffTheBlockSignature(blockManager));

            ZipIOZip64EndOfCentralDirectoryLocatorBlock block = new ZipIOZip64EndOfCentralDirectoryLocatorBlock(blockManager);

            block._offset = 0;
            block._size = 0;
            block._dirtyFlag = false;
            
            return block;
        }

        internal static bool SniffTheBlockSignature(ZipIOBlockManager blockManager)
        {
            long suspectPos = checked(blockManager.EndOfCentralDirectoryBlock.Offset - _fixedMinimalRecordSize);

            // let's check that EndOfCentralDirectoryBlock.Offset is not too close to the start of the stream
            // for the record to fit there 

            // the second check isn't required, strictily speaking, as we are stepping back from the  EOCD.offset
            // however in some theoretical cases EOCD might not be trustable so to ensure that  ReadUInt32
            // isn't going to throw we do additional check
            if ((suspectPos < 0) ||
                (checked(suspectPos + sizeof(UInt32)) > blockManager.Stream.Length))  
            {
                return false;
            }

            blockManager.Stream.Seek(suspectPos, SeekOrigin.Begin);
                
            UInt32 signature = blockManager.BinaryReader.ReadUInt32();
            return (signature == _signatureConstant);
        }
    
        internal long OffsetOfZip64EndOfCentralDirectoryRecord
        {
            get
            {
                return (long)_offsetOfStartOfZip64EndOfCentralDirectoryRecord;
            }
        }

        private ZipIOZip64EndOfCentralDirectoryLocatorBlock(ZipIOBlockManager blockManager)
        {
            Debug.Assert(blockManager != null);
            _blockManager= blockManager;
        }

        private void ParseRecord (BinaryReader reader, long position)
        {
            _signature = reader.ReadUInt32();
            _numberOfTheDiskWithTheStartOfZip64EndOfCentralDirectory = reader.ReadUInt32();
            _offsetOfStartOfZip64EndOfCentralDirectoryRecord = reader.ReadUInt64();
            _totalNumberOfDisks = reader.ReadUInt32();

            _offset = position;
            _size = _fixedMinimalRecordSize;
            _dirtyFlag = false;
            
            Validate();
        }

        private void Validate() 
        {
            if (_offsetOfStartOfZip64EndOfCentralDirectoryRecord > Int64.MaxValue)  // C# does proper upcasting to ULONG of both operands
            {
                // although we are trying to support 64 bit structures 
                // we are limited by the CLR model for streams down to 63 
                // bit size for all the Uint64 fields 

                throw new NotSupportedException(SR.Get(SRID.Zip64StructuresTooLarge)); 
            }
            
            if (_signature != _signatureConstant)
            {
                throw new FileFormatException(SR.Get(SRID.CorruptedData));
            }

            if ((_totalNumberOfDisks != 1) ||
                (_numberOfTheDiskWithTheStartOfZip64EndOfCentralDirectory != 0))
            {
                throw new NotSupportedException(SR.Get(SRID.NotSupportedMultiDisk));
            }

            // The offset of the ZIP 64 EOCD must preceed the location of the ZIP64 EOCD locator 
            if ((UInt64)_offset <= _offsetOfStartOfZip64EndOfCentralDirectoryRecord) // we assume that _offset >=0
            {
                    throw new FileFormatException(SR.Get(SRID.CorruptedData));
            }            

            // this record is optional size must be either 0 or _fixedMinimalRecordSize
            if ((_size != _fixedMinimalRecordSize) && (_size != 0))
            {
                    throw new FileFormatException(SR.Get(SRID.CorruptedData));
            }
        }
        
        private ZipIOBlockManager _blockManager;
        
        private long _offset;
        private long _size;        
        private bool _dirtyFlag;        

        private  const UInt32 _signatureConstant  = 0x07064b50;
        private const int _fixedMinimalRecordSize = 20;

        // data persisted on disk 
        private UInt32 _signature = _signatureConstant;
        private UInt32 _numberOfTheDiskWithTheStartOfZip64EndOfCentralDirectory;
        private UInt64 _offsetOfStartOfZip64EndOfCentralDirectoryRecord;
        private UInt32 _totalNumberOfDisks = 1;
    }
}
