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
using MS.Internal.WindowsBase;
    
namespace MS.Internal.IO.Zip
{
    internal class ZipIOCentralDirectoryFileHeader
    {
        internal static ZipIOCentralDirectoryFileHeader CreateNew(Encoding encoding, ZipIOLocalFileBlock fileBlock)
        {
            ZipIOCentralDirectoryFileHeader header = new ZipIOCentralDirectoryFileHeader(encoding);

            // initialize fields that are not duplicated in the local file block(header) 
            header._fileCommentLength =0;
            header._fileComment = null; 
            header._diskNumberStart = 0;
            header._internalFileAttributes = 0;
            header._externalFileAttributes = 0;
            header._versionMadeBy = (ushort)ZipIOVersionNeededToExtract.Zip64FileFormat;
            header._extraField = ZipIOExtraField.CreateNew(false /* no padding */);
            
            // update the rest of the fields based on the local file header             
            header.UpdateFromLocalFileBlock(fileBlock); 
            
            return header;
        }

        internal static ZipIOCentralDirectoryFileHeader ParseRecord(BinaryReader reader, Encoding encoding)
        {
            ZipIOCentralDirectoryFileHeader header = new ZipIOCentralDirectoryFileHeader(encoding);
                
            header._signature =  reader.ReadUInt32();
            header._versionMadeBy = reader.ReadUInt16();
            header._versionNeededToExtract = reader.ReadUInt16();
            header._generalPurposeBitFlag = reader.ReadUInt16();
            header._compressionMethod = reader.ReadUInt16();
            header._lastModFileDateTime = reader.ReadUInt32();
            header._crc32 = reader.ReadUInt32();
            header._compressedSize = reader.ReadUInt32();
            header._uncompressedSize = reader.ReadUInt32();
            header._fileNameLength = reader.ReadUInt16();
            header._extraFieldLength = reader.ReadUInt16();
            header._fileCommentLength = reader.ReadUInt16();
            header._diskNumberStart = reader.ReadUInt16();
            header._internalFileAttributes = reader.ReadUInt16();
            header._externalFileAttributes = reader.ReadUInt32();
            header._relativeOffsetOfLocalHeader = reader.ReadUInt32();

            header._fileName = reader.ReadBytes(header._fileNameLength);

            // check for the ZIP 64 version and escaped values 
            ZipIOZip64ExtraFieldUsage zip64extraFieldUsage = ZipIOZip64ExtraFieldUsage.None;
            if (header._versionNeededToExtract >= (ushort)ZipIOVersionNeededToExtract.Zip64FileFormat)
            {
                if (header._compressedSize == UInt32.MaxValue)
                {
                    zip64extraFieldUsage |= ZipIOZip64ExtraFieldUsage.CompressedSize;
                }
                if (header._uncompressedSize == UInt32.MaxValue)
                {
                    zip64extraFieldUsage |= ZipIOZip64ExtraFieldUsage.UncompressedSize;
                }
                if (header._relativeOffsetOfLocalHeader == UInt32.MaxValue)
                {
                    zip64extraFieldUsage |= ZipIOZip64ExtraFieldUsage.OffsetOfLocalHeader;
                }
                if (header._diskNumberStart == UInt16.MaxValue)
                {
                    zip64extraFieldUsage |= ZipIOZip64ExtraFieldUsage.DiskNumber;
                }
            }

            // if the ZIP 64 record is missing the zip64extraFieldUsage value will be ignored
            header._extraField = ZipIOExtraField.ParseRecord(reader, 
                                             zip64extraFieldUsage, 
                                            header._extraFieldLength);

            header._fileComment = reader.ReadBytes(header._fileCommentLength);

            //populate frequently used field with user friendly data representations
            header._stringFileName = ZipIOBlockManager.ValidateNormalizeFileName(encoding.GetString(header._fileName));

            header.Validate();

            return header;
        }

        internal void Save(BinaryWriter writer)
        {
            writer.Write(_signatureConstant);
            writer.Write(_versionMadeBy);
            writer.Write(_versionNeededToExtract);
            writer.Write(_generalPurposeBitFlag);
            writer.Write(_compressionMethod);
            writer.Write(_lastModFileDateTime);
            writer.Write(_crc32);
            writer.Write(_compressedSize);
            writer.Write(_uncompressedSize);
            writer.Write(_fileNameLength);
            writer.Write(_extraField.Size);
            writer.Write(_fileCommentLength);
            writer.Write(_diskNumberStart);
            writer.Write(_internalFileAttributes);
            writer.Write(_externalFileAttributes);
            writer.Write(_relativeOffsetOfLocalHeader);


            Debug.Assert(_fileNameLength > 0); // we validate this for both parsing and API entry points
            writer.Write(_fileName, 0, _fileNameLength);

            _extraField.Save(writer);
            
            if (_fileCommentLength > 0)            
            {
                writer.Write(_fileComment , 0, _fileCommentLength);
            }
        }

        internal bool UpdateIfNeeded(ZipIOLocalFileBlock fileBlock)
        {
            if (CheckIfUpdateNeeded(fileBlock))
            {
                UpdateFromLocalFileBlock(fileBlock);
                return true;
            }
            else
            {
                return false;
            }
        }

        internal string FileName
        {
            get
            {
                return _stringFileName;   
            }
            // set method if needed will have to update both the _stringFileName and 
            // _fileName
        }

        internal UInt16 VersionNeededToExtract
        {
            get
            {
                return _versionNeededToExtract;
            }
        }

        internal UInt16 GeneralPurposeBitFlag
        {
            get
            {
                return _generalPurposeBitFlag;
            }
        }

        internal CompressionMethodEnum CompressionMethod
        {
            get
            {
                // cast is safe because the value is validated in Validate()
                return (CompressionMethodEnum)_compressionMethod;
            }
        }

        internal long Size
        {
            get
            {
                return checked(_fixedMinimalRecordSize + _fileNameLength + _extraField.Size + _fileCommentLength);
            }
        }

        internal long OffsetOfLocalHeader
        {
            get
            {
                if ((_extraField.Zip64ExtraFieldUsage & ZipIOZip64ExtraFieldUsage.OffsetOfLocalHeader) != 0)
                {
                    // zip 64 extra field is there 
                    return _extraField.OffsetOfLocalHeader;
                }
                else
                {
                    // 32 bit case 
                    return _relativeOffsetOfLocalHeader;
                }
            }
        }

        internal long CompressedSize
        {
            get
            {
                if ((_extraField.Zip64ExtraFieldUsage & ZipIOZip64ExtraFieldUsage.CompressedSize) != 0)
                {
                    // zip 64 extra field is there 
                    return _extraField.CompressedSize;
                }
                else
                {
                    // 32 bit case 
                    return _compressedSize;
                }
            }
        }

        internal long UncompressedSize
        {
            get
            {
                if ((_extraField.Zip64ExtraFieldUsage & ZipIOZip64ExtraFieldUsage.UncompressedSize) != 0)
                {
                    // zip 64 extra field is there 
                    return _extraField.UncompressedSize;
                }
                else
                {
                    // 32 bit case 
                    return _uncompressedSize;
                }
            }
        }

        internal UInt32 Crc32
        {
            get
            {
                return _crc32;
            }
        }

        internal UInt32 DiskNumberStart
        {
            get
            {
                if ((_extraField.Zip64ExtraFieldUsage & ZipIOZip64ExtraFieldUsage.DiskNumber) != 0)
                {
                    // zip 64 extra field is there (32 bit value returned)
                    return _extraField.DiskNumberOfFileStart;
                }
                else
                {
                    // 16 bit case 
                    return _diskNumberStart;;
                }
            }
        }
        
        internal bool FolderFlag
        {
            get
            {
                // The upper byte of version made by indicates the compatibility of the file attribute information.  
                // If the external file attributes are compatible with MS-DOS then this value 
                // will be zero.  

                // lower byte of the external file attribute is the the MS-DOS directory attribute byte 
                // 
                //                          0x20 5   file has been changed since last backup
                //                          0x10 4   entry represents a subdirectory XXXXXXXXX
                //                          0x08 3   entry represents a volume label  
                //                          0x04 2   system file
                //                          0x02 1   hidden file
                //                          0x01 0   read-only

                return  ((_versionMadeBy & 0xFF00) == _constantUpperVersionMadeByMsDos) 
                        && 
                            ((_externalFileAttributes & 0x10) != 0);
            }
        }

        internal bool VolumeLabelFlag 
        {
            get
            {
                // The upper byte of version made by indicates the compatibility of the file attribute information.  
                // If the external file attributes are compatible with MS-DOS then this value 
                // will be zero.  

                // lower byte of the external file attribute is the the MS-DOS directory attribute byte 
                // 
                //                          0x20 5   file has been changed since last backup
                //                          0x10 4   entry represents a subdirectory
                //                          0x08 3   entry represents a volume label  XXXXXXXXX
                //                          0x04 2   system file
                //                          0x02 1   hidden file
                //                          0x01 0   read-only

                return  ((_versionMadeBy & 0xFF00) == _constantUpperVersionMadeByMsDos) 
                        && 
                            ((_externalFileAttributes & 0x08) != 0);
            }
        }

        // this function is called by the Central Dir in order to notify us that 
        // the appropriate file item was shifted (as detected by the shift in the Raw Data Block)
        // holding given file item.
        // for us it means that although all the size characteristics are preserved (local file header 
        // wasn't even parsed if it still in the Raw). But the offset could have changed which 
        // might result in Zip64 struicture. 
        internal void MoveReference(long shiftSize)
        {
            UpdateZip64Structures(CompressedSize, 
                                                            UncompressedSize, 
                                                            checked(OffsetOfLocalHeader +shiftSize));
        }

        // this function is sets the sizes into the either 64 or 32 bit structures based on values of the fields 
        // It used in 2 places by the MoveReference and by the UpdateFromLocalFileBlock 
        private void UpdateZip64Structures
                (long compressedSize, long uncompressedSize, long offset)
        {
            Debug.Assert((compressedSize >= 0) && (uncompressedSize>=0) && (offset >=0));

            // according to the appnote central directory extra field might be a mix of any values based on escaping 
            // we will fully (without disk number) use it every time we are building a ZIP 64 arhichive 
            // we also trying to stay on the safe side and treeat the boundary case of 32 escape values 
            // as a zip 64 scenrio 
            if ((compressedSize >= UInt32.MaxValue) || 
                    (uncompressedSize >= UInt32.MaxValue) ||
                    (offset >= UInt32.MaxValue))
            {
                // Zip 64 case            
                _extraField.CompressedSize = compressedSize;
                _extraField.UncompressedSize = uncompressedSize;
                _extraField.OffsetOfLocalHeader = offset;

                //set proper escape values 
               _compressedSize = UInt32.MaxValue;
               _uncompressedSize = UInt32.MaxValue;
               _relativeOffsetOfLocalHeader = UInt32.MaxValue;

                // update version needed to extract to 4.5  
                _versionNeededToExtract = (UInt16)ZipIOVersionNeededToExtract.Zip64FileFormat;
            }
            else
            {
                // 32 bit case
               _compressedSize = checked((UInt32)compressedSize);
               _uncompressedSize = checked((UInt32)uncompressedSize);
               _relativeOffsetOfLocalHeader = checked((UInt32)offset);

                // reset the extra ZIP 64 field to empty 
               _extraField.Zip64ExtraFieldUsage = ZipIOZip64ExtraFieldUsage.None;

                // version needed to extract needs to be recalculated from scratch based on compression  
                _versionNeededToExtract = (UInt16)ZipIOBlockManager.CalcVersionNeededToExtractFromCompression
                                                                                            ((CompressionMethodEnum)_compressionMethod);
            }
        }
        
        private void  UpdateFromLocalFileBlock(ZipIOLocalFileBlock fileBlock)
        {
            Debug.Assert(DiskNumberStart == 0);
            
            _signature = _signatureConstant;
            _generalPurposeBitFlag = fileBlock.GeneralPurposeBitFlag;
            _compressionMethod = (UInt16)fileBlock.CompressionMethod;
            _lastModFileDateTime = fileBlock.LastModFileDateTime;
            _crc32 = fileBlock.Crc32;

            // file name is easy to copy 
            _fileNameLength = (UInt16)fileBlock.FileName.Length; // this is safe cast as file name is always validate for size
            _fileName = _encoding.GetBytes(fileBlock.FileName);
            _stringFileName = fileBlock.FileName;

            // this will properly update the 32 or zip 64 fields 
            UpdateZip64Structures(fileBlock.CompressedSize,
                                    fileBlock.UncompressedSize,
                                    fileBlock.Offset);

            // Previous instruction may determine that we don't really need 4.5, but we
            // want to ensure that the version is identical with what is stored in the local file header.
            Debug.Assert(_versionNeededToExtract <= fileBlock.VersionNeededToExtract, "Should never be making this smaller");

            _versionNeededToExtract = fileBlock.VersionNeededToExtract; 

            // These fields are intentionally ignored, as they are not present in the local header 
                        //_fileCommentLength;
                        //_fileComment; 
                        //_diskNumberStart;
                        //_internalFileAttributes;
                        //_externalFileAttributes;
        }

        private bool CheckIfUpdateNeeded(ZipIOLocalFileBlock fileBlock)
        {
            // there is a special case for the _generalPurposeBitFlag.Bit #3
            // it could be set in the local file header indicating streaming  
            // creation, while it doesn't need to be set in the Central directory 
            // so having 
            //  (fileBlock.GeneralPurposeBitFlag == 8 && and _generalPurposeBitFlag  == 0)
            // is a valid case when update is not required 

            // let's compare the 3rd bit of the general purpose bit flag 
            bool localFileHeaderStreamingFlag = (0 != (fileBlock.GeneralPurposeBitFlag & _streamingBitMask));
            bool centralDirStreamingFlag = (0 != (_generalPurposeBitFlag & _streamingBitMask));
            
            if (!localFileHeaderStreamingFlag  && centralDirStreamingFlag)
            {
                // the mismatch if local file header in non streaming but the central directory is in streaming mode 
                // all the other combinations do not require an update and valid as is 
                // this includes scenario when local file header is in streaming and central dir is not 
                return true; 
            }

            Debug.Assert(String.CompareOrdinal(_stringFileName, fileBlock.FileName) == 0);

            return 
                (_signature != _signatureConstant) ||
                (_versionNeededToExtract != fileBlock.VersionNeededToExtract) ||
                (_generalPurposeBitFlag != fileBlock.GeneralPurposeBitFlag) ||
                (_compressionMethod != (UInt16)fileBlock.CompressionMethod) ||
                (_crc32 != fileBlock.Crc32) ||
                (CompressedSize != fileBlock.CompressedSize) ||
                (UncompressedSize != fileBlock.UncompressedSize) ||
                (OffsetOfLocalHeader != fileBlock.Offset);

                // These fields are intentionally ignored, as they are not present in the local header 
                            //_fileCommentLength;
                            //_fileComment; 
                            //_diskNumberStart;
                            //_internalFileAttributes;
                            //_externalFileAttributes;
        }
            

        private ZipIOCentralDirectoryFileHeader(Encoding encoding) 
        {
            _encoding = encoding;
        }            

        private void Validate ()
        {
            if (_signature != _signatureConstant)
            {
                throw new FileFormatException(SR.Get(SRID.CorruptedData));
            }

            if (DiskNumberStart != 0)
            {
                throw new NotSupportedException(SR.Get(SRID.NotSupportedMultiDisk));
            }

            if (_fileNameLength != _fileName.Length)
            {
                throw new FileFormatException(SR.Get(SRID.CorruptedData));
            }

            if (_extraFieldLength != _extraField.Size)
            {
                throw new FileFormatException(SR.Get(SRID.CorruptedData));
            }

            ZipArchive.VerifyVersionNeededToExtract(_versionNeededToExtract);

            // if verson is below 4.5 make sure that ZIP 64 extra filed isn't present 
            // if it is it might be a security concern 
            if ((_versionNeededToExtract < (UInt16)ZipIOVersionNeededToExtract.Zip64FileFormat) &&
                (_extraField.Zip64ExtraFieldUsage != ZipIOZip64ExtraFieldUsage.None))
            {
                throw new FileFormatException(SR.Get(SRID.CorruptedData));                
            }

            if (_fileCommentLength != _fileComment.Length)
            {
                throw new FileFormatException(SR.Get(SRID.CorruptedData));
            }

            if ((_compressionMethod != (UInt16)CompressionMethodEnum.Stored) && 
                (_compressionMethod != (UInt16)CompressionMethodEnum.Deflated))
            {
                throw new NotSupportedException(SR.Get(SRID.ZipNotSupportedCompressionMethod));
            }
        }

        private Encoding _encoding;
        private const int _fixedMinimalRecordSize = 46;

        private const byte _constantUpperVersionMadeByMsDos = 0x0;

        private const UInt16 _streamingBitMask = 0x08; // bit #3  
    
        private const UInt32 _signatureConstant = 0x02014b50;
        private UInt32 _signature = _signatureConstant;

        // we expect all variables to be initialized to 0
        private UInt16 _versionMadeBy;
        private UInt16 _versionNeededToExtract;
        private UInt16 _generalPurposeBitFlag;  
        private UInt16 _compressionMethod;
        private UInt32 _lastModFileDateTime;
        private UInt32 _crc32;
        private UInt32 _compressedSize;
        private UInt32 _uncompressedSize;
        private UInt16 _fileNameLength;
        private UInt16 _extraFieldLength;
        private UInt16 _fileCommentLength;
        private UInt16 _diskNumberStart;
        private UInt16 _internalFileAttributes;
        private UInt32 _externalFileAttributes;
        private UInt32 _relativeOffsetOfLocalHeader;
        private byte[] _fileName;
        private ZipIOExtraField _extraField;
        private byte[] _fileComment;

        //duplicate dat for fast access
        private string _stringFileName;
    }
}
