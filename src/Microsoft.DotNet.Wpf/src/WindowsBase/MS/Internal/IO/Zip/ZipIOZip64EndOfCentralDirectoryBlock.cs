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
    internal class ZipIOZip64EndOfCentralDirectoryBlock : IZipIOBlock
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

        // This property will only return reliable result if UpdateReferences is called prior  
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
            // this record is optional and shouldn't be saved if size is 0
            if (GetDirtyFlag(true) && (Size > 0))
            {
                BinaryWriter writer = _blockManager.BinaryWriter;
                if (_blockManager.Stream.Position != _offset)
                {
                    // we need to seek , as current position isn't accurate 
                    _blockManager.Stream.Seek(_offset, SeekOrigin.Begin);
                }
               
                writer.Write(_signatureConstant);
                writer.Write(_sizeOfZip64EndOfCentralDirectory);
                writer.Write(_versionMadeBy);
                writer.Write(_versionNeededToExtract);
                writer.Write(_numberOfThisDisk);
                writer.Write(_numberOfTheDiskWithTheStartOfTheCentralDirectory);
                writer.Write(_totalNumberOfEntriesInTheCentralDirectoryOnThisDisk);
                writer.Write(_totalNumberOfEntriesInTheCentralDirectory);
                writer.Write(_sizeOfTheCentralDirectory);
                writer.Write(_offsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber);

                if (_sizeOfZip64EndOfCentralDirectory > _fixedMinimalValueOfSizeOfZip64EOCD)
                {
                    Debug.Assert(_zip64ExtensibleDataSector != null);
                    Debug.Assert(_zip64ExtensibleDataSector.Length == 
                                    checked((int)(_sizeOfZip64EndOfCentralDirectory -_fixedMinimalValueOfSizeOfZip64EOCD)));
                    
                    writer.Write(_zip64ExtensibleDataSector, 
                                    0,  
                                    checked((int)(_sizeOfZip64EndOfCentralDirectory -_fixedMinimalValueOfSizeOfZip64EOCD)));                
                }

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
                //  2. streaming mode
                // if Central Directory isn't loaded or none of the relevant structure is dirty,
                //  there is nothing to update for Zip64 End Of Central directory record 
                if (_blockManager.IsCentralDirectoryBlockLoaded
                        && (_blockManager.Streaming
                            || _blockManager.CentralDirectoryBlock.GetDirtyFlag(closingFlag)))
                {
                    if (_blockManager.CentralDirectoryBlock.IsZip64BitRequiredForStoring)
                    {
                        UInt64 centralDirCount = (UInt64)_blockManager.CentralDirectoryBlock.Count;
                        UInt64 centralDirBlockSize = (UInt64)_blockManager.CentralDirectoryBlock.Size;
                        UInt64 centralDirOffset = (UInt64)_blockManager.CentralDirectoryBlock.Offset;

            // Here is a diagram of the record 
            //----------------------------------------------------------------------------------------------------------------------
            //|SignatureConst (4 bytes)|sizeOfZip64Eocd (8 bytes)|misc fixed fields (44 bytes)|Variable Size Extensible Data sector|
            //A------------------------B-------------------------C----------------------------D------------------------------------E
            // 
            // in order to calculate the actual record size we subtract _fixedMinimalValueOfSizeOfZip64EOCD (This is a chunk marked 
            // (C,D) in thre diagram above) from _fixedMinimalRecordSize (This is a chunk marked (A,D) in the diagram above).
            // Then we add the resulting value (which would be chunked marked (A,C) to the value of  sizeOfZip64Eocd field which 
            // contains the size of the record starting at point (C) and going to the end (E). So we get the total size as 
            //   (A,C) + (C,E) = (A,E)
            // 

                        long size =  checked((long)(
                                                    // value that was either parsed from a file or initialized to the _fixedMinimalValueOfSizeOfZip64EOCD
                                        _sizeOfZip64EndOfCentralDirectory +  
                                                    // const (value indicating minimal whole record size, how many bytes on disk it needs) 56
                                        _fixedMinimalRecordSize -                           
                                                    // const (value indicating minimal value for the SizeOfZip64EOCD field as it is contains 
                                                    // the whole size without record signature(4), and the itself (8) it is 56 - 12 = 44
                                        _fixedMinimalValueOfSizeOfZip64EOCD));    
                    
                        // update value and mark record dirty if either it is already dirty or there is a mismatch
                        if ((_dirtyFlag) || 
                            (_totalNumberOfEntriesInTheCentralDirectoryOnThisDisk != centralDirCount) ||
                            (_totalNumberOfEntriesInTheCentralDirectory != centralDirCount ) ||
                            (_sizeOfTheCentralDirectory != centralDirBlockSize) ||
                            (_offsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber != centralDirOffset) ||
                            (_size != size))
                        {
                            _versionMadeBy = (ushort)ZipIOVersionNeededToExtract.Zip64FileFormat;
                            _versionNeededToExtract = (ushort)ZipIOVersionNeededToExtract.Zip64FileFormat;

                            _numberOfThisDisk = 0;
                            _numberOfTheDiskWithTheStartOfTheCentralDirectory  = 0;
                        
                            _totalNumberOfEntriesInTheCentralDirectoryOnThisDisk = centralDirCount;
                            _totalNumberOfEntriesInTheCentralDirectory = centralDirCount;
                            _sizeOfTheCentralDirectory = centralDirBlockSize;
                            _offsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber = centralDirOffset;

                            _size = size;
                                
                            _dirtyFlag = true;
                        }
                    }
                    else
                    {
                        // we do not need zip 64 structures
                        if (_size != 0)
                        {
                            _dirtyFlag = true;
                            _size = 0;
                        }
                    }
                }
            }
        }

        public PreSaveNotificationScanControlInstruction PreSaveNotification(long offset, long size)
        {
            // we can safely ignore this notification as we do not keep any data on disk 
            // after parsing on disk. Everything is in memory, it is ok to override 
            // original Zip64 EndOf Central Directory Block without any additional backups

            // we can also safely state that there is no need to continue the PreSafeNotification loop 
            // as the blocks after the Zip64 Eocd (EOCD, Zip64 locator ) do not have 
            // data that is buffered on disk
            return PreSaveNotificationScanControlInstruction.Stop;
        }
        
        internal static ZipIOZip64EndOfCentralDirectoryBlock SeekableLoad (ZipIOBlockManager blockManager)  
        {
            ZipIOZip64EndOfCentralDirectoryLocatorBlock zip64endOfCentralDirectoryLocator = 
                                                blockManager.Zip64EndOfCentralDirectoryLocatorBlock;

            long zip64EndOfCentralDirectoryOffset =                    
                                                zip64endOfCentralDirectoryLocator.OffsetOfZip64EndOfCentralDirectoryRecord;

            ZipIOZip64EndOfCentralDirectoryBlock block = new ZipIOZip64EndOfCentralDirectoryBlock(blockManager);
            
            blockManager.Stream.Seek(zip64EndOfCentralDirectoryOffset, SeekOrigin.Begin);
            
            block.ParseRecord(blockManager.BinaryReader, zip64EndOfCentralDirectoryOffset);

            return block;            
        }
        
        internal static ZipIOZip64EndOfCentralDirectoryBlock CreateNew(ZipIOBlockManager blockManager)          
        {
            ZipIOZip64EndOfCentralDirectoryBlock block = new ZipIOZip64EndOfCentralDirectoryBlock(blockManager);

            block._size = 0; // brand new created records are optional by definition untill UpdateReferences is called, so size must be 0
            block._offset = 0;
            block._dirtyFlag = false;

            // initialize fields with ythe data from the EOCD 
            block.InitializeFromEndOfCentralDirectory(blockManager.EndOfCentralDirectoryBlock);
            
            return block;
        }

        internal long OffsetOfStartOfCentralDirectory
        {
            get
            {
                return (long)_offsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber;
            }
        }

        internal int TotalNumberOfEntriesInTheCentralDirectory
        {
            get
            {
                return (int)_totalNumberOfEntriesInTheCentralDirectory; // checked isn't required as we do validation during parsing
            }
        }

        internal long SizeOfCentralDirectory
        {
            get
            {
                return (long)_sizeOfTheCentralDirectory;
            }
        }

        private ZipIOZip64EndOfCentralDirectoryBlock(ZipIOBlockManager blockManager)
        {
            Debug.Assert(blockManager != null);
            _blockManager= blockManager;
        }

        private void ParseRecord (BinaryReader reader, long position)
        {
            _signature = reader.ReadUInt32();
            _sizeOfZip64EndOfCentralDirectory = reader.ReadUInt64();
            _versionMadeBy = reader.ReadUInt16();
            _versionNeededToExtract = reader.ReadUInt16();
            _numberOfThisDisk = reader.ReadUInt32();
            _numberOfTheDiskWithTheStartOfTheCentralDirectory = reader.ReadUInt32();
            _totalNumberOfEntriesInTheCentralDirectoryOnThisDisk = reader.ReadUInt64();
            _totalNumberOfEntriesInTheCentralDirectory = reader.ReadUInt64();
            _sizeOfTheCentralDirectory = reader.ReadUInt64();
            _offsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber = reader.ReadUInt64();

                                                // pre validate before reading data based on parsed values 
            if ((_sizeOfZip64EndOfCentralDirectory < _fixedMinimalValueOfSizeOfZip64EOCD) ||
                                                // we are refusing to buffer large extended areas 
                (_sizeOfZip64EndOfCentralDirectory > UInt16.MaxValue)) 
            {
                throw new FileFormatException(SR.Get(SRID.CorruptedData));
            }

            if (_sizeOfZip64EndOfCentralDirectory > _fixedMinimalValueOfSizeOfZip64EOCD)
            {
                _zip64ExtensibleDataSector = reader.ReadBytes((int)(_sizeOfZip64EndOfCentralDirectory -_fixedMinimalValueOfSizeOfZip64EOCD));
            }
            
            // override some numbers bvased on the EOCD data according to the  apnote 
            // even in presence of Zip64Eocd we still need to use the regular EOCD data 
            OverrideValuesBasedOnEndOfCentralDirectory(_blockManager.EndOfCentralDirectoryBlock);
            
            _size =  checked((long)(    // value that was either parsed from a file or initialized to the _fixedMinimalValueOfSizeOfZip64EOCD
                                        _sizeOfZip64EndOfCentralDirectory +  
                                                    // const (value indicating minimal whole record size, how many bytes on disk it needs) 56
                                        _fixedMinimalRecordSize -                           
                                                    // const (value indicating minimal value for the SizeOfZip64EOCD field as it is contains 
                                                    // the whole size without record signature(4), and the itself (8) it is 56 - 12 = 44
                                        _fixedMinimalValueOfSizeOfZip64EOCD));
            Debug.Assert(_size >= _fixedMinimalRecordSize);

            _offset = position;
            _dirtyFlag = false;

            Validate();
        }

        /// <summary>
        /// This function is called from the Create New routine. The purpose of this exercise , is to copy data from 32 bit EOCD into this record,
        /// for scenarios when ZIP64 EOCD wasn't parsed from a file, but was just made up.
        /// This is done so that Central Dir parsing code can ask the ZIP64 EOCD for this data, and regardless of whether it is real zip 64 file or    
        /// not a zip 64 file it will get the right CD offset , size and so on 
        /// </summary>
        private void InitializeFromEndOfCentralDirectory(ZipIOEndOfCentralDirectoryBlock zipIoEocd)
        {
            _numberOfThisDisk = zipIoEocd.NumberOfThisDisk;
            _numberOfTheDiskWithTheStartOfTheCentralDirectory = zipIoEocd.NumberOfTheDiskWithTheStartOfTheCentralDirectory;
            _totalNumberOfEntriesInTheCentralDirectoryOnThisDisk  = zipIoEocd.TotalNumberOfEntriesInTheCentralDirectoryOnThisDisk;
            _totalNumberOfEntriesInTheCentralDirectory = zipIoEocd.TotalNumberOfEntriesInTheCentralDirectory;
            _sizeOfTheCentralDirectory = zipIoEocd.SizeOfTheCentralDirectory;
            _offsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber = zipIoEocd.OffsetOfStartOfCentralDirectory;
        }
        
        /// <summary>
        /// This function is called from the Parse routine. The purpose of this exercise , is to figure out the escape 
        /// values in the regular 32 bit EOCD. We shouldn't be using values from the 64 bit structure if it wasn't 
        /// escaped in the 32 bit structure. 
        /// </summary>
        private void OverrideValuesBasedOnEndOfCentralDirectory(ZipIOEndOfCentralDirectoryBlock zipIoEocd)
        {
            // 16 bit numbers 
            if (zipIoEocd.NumberOfThisDisk < UInt16.MaxValue)
                {_numberOfThisDisk = zipIoEocd.NumberOfThisDisk;}
            
            if (zipIoEocd.NumberOfTheDiskWithTheStartOfTheCentralDirectory < UInt16.MaxValue)
                {_numberOfTheDiskWithTheStartOfTheCentralDirectory = zipIoEocd.NumberOfTheDiskWithTheStartOfTheCentralDirectory;}
            
            if (zipIoEocd.TotalNumberOfEntriesInTheCentralDirectoryOnThisDisk  < UInt16.MaxValue)
                {_totalNumberOfEntriesInTheCentralDirectoryOnThisDisk  = zipIoEocd.TotalNumberOfEntriesInTheCentralDirectoryOnThisDisk;}
            
            if (zipIoEocd.TotalNumberOfEntriesInTheCentralDirectory < UInt16.MaxValue)
                {_totalNumberOfEntriesInTheCentralDirectory = zipIoEocd.TotalNumberOfEntriesInTheCentralDirectory;}
            
            // 32  bit numbers         
            if (zipIoEocd.SizeOfTheCentralDirectory < UInt32.MaxValue)
                {_sizeOfTheCentralDirectory = zipIoEocd.SizeOfTheCentralDirectory;}
            
            if (zipIoEocd.OffsetOfStartOfCentralDirectory < UInt32.MaxValue)
                {_offsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber = zipIoEocd.OffsetOfStartOfCentralDirectory;}
}
            
        private void Validate() 
        {
            if (_signature != _signatureConstant)
            {
                throw new FileFormatException(SR.Get(SRID.CorruptedData));
            }

            if ((_numberOfThisDisk != 0) ||
                (_numberOfTheDiskWithTheStartOfTheCentralDirectory != 0) ||
                (_totalNumberOfEntriesInTheCentralDirectoryOnThisDisk != 
                                                    _totalNumberOfEntriesInTheCentralDirectory))
            {
                throw new NotSupportedException(SR.Get(SRID.NotSupportedMultiDisk));
            }

            // this will throw an unsupported version exception if we see a version that we do not support
            ZipArchive.VerifyVersionNeededToExtract(_versionNeededToExtract);

            // if it is one of the supported version but it isn't a ZIP64, it is an indication of a corrupted file
            if (_versionNeededToExtract !=  (UInt16)ZipIOVersionNeededToExtract.Zip64FileFormat)
            {
                // if version isn't equal to the 4.5 it is a corrupted file (as we)
                // as appnote explicitly states that  
                //            When using ZIP64 extensions, the corresponding value in the
                //            Zip64 end of central directory record should also be set.  
                //            This field currently supports only the value 45 to indicate
                //            ZIP64 extensions are present. 
                throw new FileFormatException(SR.Get(SRID.CorruptedData));
            }

            if ((_totalNumberOfEntriesInTheCentralDirectoryOnThisDisk > Int32.MaxValue) || 
                (_totalNumberOfEntriesInTheCentralDirectory > Int32.MaxValue) ||
                (_sizeOfTheCentralDirectory > Int64.MaxValue) ||
                (_offsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber > Int64.MaxValue))
            {
                // although we are trying to support 64 bit structures 
                // we are limited by the CLR model for collections (down to 32 bit collection size for 
                // _totalNumberOfEntriesInTheCentralDirectoryOnThisDisk ) 
                // and streams (down to 63 bit size) for all the outher Uint64 fields 

                throw new NotSupportedException(SR.Get(SRID.Zip64StructuresTooLarge)); 
            }

            ulong sizeOfZip64ExtensibleDataSector = 0;
            if (_zip64ExtensibleDataSector != null)
            {
                sizeOfZip64ExtensibleDataSector = (ulong)_zip64ExtensibleDataSector.Length;
            }

            // the subtraction below doesn't need to be checked as we have validation in the parse logic 
            //    if (_sizeOfZip64EndOfCentralDirectory < _fixedMinimalValueOfSizeOfZip64EOCD)   {   throw ..  }
            if (_sizeOfZip64EndOfCentralDirectory - _fixedMinimalValueOfSizeOfZip64EOCD != sizeOfZip64ExtensibleDataSector)
            {
                throw new FileFormatException(SR.Get(SRID.CorruptedData));
            }

            //calculated record size must be larger than the min value 
            // it could be 0 for newly created from scratch records, but we do not pass those records through validation
            // we only validate parsed data 
            if (_size < _fixedMinimalRecordSize)
            {
                throw new FileFormatException(SR.Get(SRID.CorruptedData));
            }
        }
        
        
        private ZipIOBlockManager _blockManager;
        
        private long _offset;
        private long _size;

        private bool  _dirtyFlag;        

        private  const UInt32 _signatureConstant  = 0x06064b50;
        private const uint _fixedMinimalRecordSize = 56;
        private const uint _fixedMinimalValueOfSizeOfZip64EOCD = 44; // doesn't include the signature and the size itself
        
        // data persisted on disk 
        private UInt32 _signature = _signatureConstant;
                                                                
        private UInt64 _sizeOfZip64EndOfCentralDirectory = _fixedMinimalValueOfSizeOfZip64EOCD; 
        private UInt16 _versionMadeBy = (ushort)ZipIOVersionNeededToExtract.Zip64FileFormat; 
        private UInt16 _versionNeededToExtract = (ushort)ZipIOVersionNeededToExtract.Zip64FileFormat; 
        private UInt32 _numberOfThisDisk;
        private UInt32 _numberOfTheDiskWithTheStartOfTheCentralDirectory;
        private UInt64 _totalNumberOfEntriesInTheCentralDirectoryOnThisDisk;     // all int64s declared as signed values
        private UInt64 _totalNumberOfEntriesInTheCentralDirectory;                       // as we can not suport true unsigned 64 bit sizes 
        private UInt64 _sizeOfTheCentralDirectory;                                                    // as a result of limitations in Stream interface 
        private UInt64 _offsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber;
        private byte[] _zip64ExtensibleDataSector;
    }
}
