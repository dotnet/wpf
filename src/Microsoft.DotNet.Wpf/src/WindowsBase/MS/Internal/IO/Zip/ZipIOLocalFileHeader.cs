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
    internal class ZipIOLocalFileHeader 
    {
        /// <summary>
        /// Create a new LocalFileHeader
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="encoding"></param>
        /// <param name="compressionMethod"></param>
        /// <param name="deflateOption"></param>
        /// <param name="streaming">true if in streaming mode</param>
        /// <returns></returns>
        internal static ZipIOLocalFileHeader CreateNew(string fileName, Encoding encoding,
            CompressionMethodEnum compressionMethod, DeflateOptionEnum deflateOption, bool streaming)
        {
            //this should be ensured by the higher levels 
            Debug.Assert(Enum.IsDefined(typeof(CompressionMethodEnum), compressionMethod)); 
            Debug.Assert(Enum.IsDefined(typeof(DeflateOptionEnum), deflateOption)); 

            byte[] asciiName = encoding.GetBytes(fileName);
            if (asciiName.Length > ZipIOBlockManager.MaxFileNameSize)
            {
                throw new ArgumentOutOfRangeException("fileName");
            }

            ZipIOLocalFileHeader header = new ZipIOLocalFileHeader();
            header._signature = ZipIOLocalFileHeader._signatureConstant;
            header._compressionMethod = (ushort)compressionMethod;

            if (streaming)
                header._versionNeededToExtract = (UInt16)ZipIOVersionNeededToExtract.Zip64FileFormat;
            else
            {
                header._versionNeededToExtract = (UInt16)ZipIOBlockManager.CalcVersionNeededToExtractFromCompression(
                                                    compressionMethod);
            }

            if (compressionMethod != CompressionMethodEnum.Stored)
            {
                Debug.Assert(deflateOption != DeflateOptionEnum.None); //this should be ensured by the higher levels 
                header.DeflateOption =  deflateOption;
            }

            if (streaming)
            {
                // set bit 3
                header.StreamingCreationFlag = true;
            }

            header._lastModFileDateTime = ZipIOBlockManager.ToMsDosDateTime(DateTime.Now);

            header._fileNameLength = (UInt16)asciiName.Length;

            header._fileName = asciiName;
            header._extraField = ZipIOExtraField.CreateNew(!streaming /* creating padding if it is not in streaming creation mode */);
            header._extraFieldLength = header._extraField.Size;

            //populate frequently used field with user friendly data representations
            header._stringFileName = fileName;

            return header;
        }

        internal static ZipIOLocalFileHeader ParseRecord(BinaryReader reader, Encoding encoding)
        {
            ZipIOLocalFileHeader header = new ZipIOLocalFileHeader();
                
            header._signature =  reader.ReadUInt32();
            header._versionNeededToExtract = reader.ReadUInt16();
            header._generalPurposeBitFlag = reader.ReadUInt16();
            header._compressionMethod = reader.ReadUInt16();
            header._lastModFileDateTime = reader.ReadUInt32();
            header._crc32 = reader.ReadUInt32();
            header._compressedSize = reader.ReadUInt32();
            header._uncompressedSize = reader.ReadUInt32();
            header._fileNameLength = reader.ReadUInt16();
            header._extraFieldLength = reader.ReadUInt16();

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
            }

            // if the ZIP 64 record is missing from the Extra Field then the  zip64extraFieldUsage 
            // value will be ignored and default ZipIOZip64ExtraFieldUsage.None value will be used/assumed 
            header._extraField = ZipIOExtraField.ParseRecord(reader, 
                                             zip64extraFieldUsage, 
                                            header._extraFieldLength);
 
            //populate frequently used field with user friendly data representations
            header._stringFileName = ZipIOBlockManager.ValidateNormalizeFileName(encoding.GetString(header._fileName));

            header.Validate();

            return header;
        }

        internal void Save(BinaryWriter writer)
        {
            writer.Write(_signatureConstant);
            writer.Write(_versionNeededToExtract);
            writer.Write(_generalPurposeBitFlag);
            writer.Write(_compressionMethod);
            writer.Write(_lastModFileDateTime);
            writer.Write(_crc32);
            writer.Write(_compressedSize);
            writer.Write(_uncompressedSize);
            writer.Write(_fileNameLength);
            writer.Write(_extraField.Size);

            if (_fileNameLength > 0)
            {
                writer.Write(_fileName, 0, _fileNameLength);
            }

            _extraField.Save(writer);
            
            writer.Flush();
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
                return (CompressionMethodEnum)_compressionMethod;   
            }
        }

        internal UInt32 LastModFileDateTime
        {
            get
            {
                return _lastModFileDateTime;
            }
            set
            {
                _lastModFileDateTime = value;
            }
        }

        internal UInt32 Crc32
        {
            get
            {
                return _crc32;
            }
            set
            {
                _crc32 = value;
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

        /// <summary>
        /// FileName - already scrubbed via ValidateNormalizeFileName()
        /// </summary>
        internal string FileName
        {
            get
            {
                return _stringFileName;   
            }
        }

        internal long Size
        {
            get
            {
                return checked(_fixedMinimalRecordSize + _fileNameLength + _extraField.Size);
            }
        }

        internal bool StreamingCreationFlag
        {
            get
            {
                return ((_generalPurposeBitFlag & 0x8) != 0x0);
            }
            set
            {
                if (value)
                {
                    _generalPurposeBitFlag |= 0x8; //set bit #3
                }
                else
                {
                    _generalPurposeBitFlag &= 0xFFF7; //clear bit #3
                }
            }
        }

        internal DeflateOptionEnum DeflateOption
        {
            get
            {
                if (CompressionMethod == CompressionMethodEnum.Deflated)
                {
                    return (DeflateOptionEnum)(_generalPurposeBitFlag & 0x6);
                }
                else
                {
                    // defalte option is validated to be one of the 2 (deflated or stored)
                    return DeflateOptionEnum.None;
                }
            }
            set
            {
                // this checks must be done in the levels above 
                Debug.Assert(Enum.IsDefined(typeof(DeflateOptionEnum), value)); 
                Debug.Assert(value != DeflateOptionEnum.None); 
            
                // clean the value (bits 1 and 2)
                _generalPurposeBitFlag &= 0xFFF9;
                _generalPurposeBitFlag |= (UInt16)value; // safe cast because of the enum validation 
            }
        }

        static internal int FixedMinimalRecordSize
        {
            get
            {
                return _fixedMinimalRecordSize;
            }
        }

        private bool EncryptedFlag
        {
            get
            {
                return ((_generalPurposeBitFlag & 0x1) == 0x1);   
            }
        }

        internal void UpdateZip64Structures(long compressedSize, 
                                            long uncompressedSize, 
                                            long offset)
        {
            // according to the appnote local file header ZIP 64 extra field if used must contain both 
            // compressed and uncompressed size
            // we also trying to stay on the safe side and treeat the boundary case of 32 escape values 
            // as a zip 64 scenario 
            // we will also make this ZIP64 file if it is small but positioned beyond Uint32.MaxValue offset 
            if ((compressedSize >= UInt32.MaxValue) || 
                (uncompressedSize >= UInt32.MaxValue) ||
                (offset >= UInt32.MaxValue))
            {
                // Zip 64 case            
                _extraField.CompressedSize = compressedSize;
                _extraField.UncompressedSize = uncompressedSize;

                //set proper escape values 
                _compressedSize = UInt32.MaxValue;
                _uncompressedSize = UInt32.MaxValue;

                // update version needed to extract to 4.5  
                _versionNeededToExtract = (UInt16)ZipIOVersionNeededToExtract.Zip64FileFormat;
            }
            else
            {
                // 32 bit case
               _compressedSize = (UInt32)compressedSize;  // checked{} isn't required because of the checks above
               _uncompressedSize = (UInt32)uncompressedSize;

                // reset the extra ZIP 64 field to empty 
               _extraField.Zip64ExtraFieldUsage = ZipIOZip64ExtraFieldUsage.None;

                // version needed to extract needs to be recalculated from scratch based on compression  
                _versionNeededToExtract = 
                        (UInt16)ZipIOBlockManager.CalcVersionNeededToExtractFromCompression(CompressionMethod);
            }
        }

        internal void UpdatePadding(long headerSizeChange)
        {
            _extraField.UpdatePadding(headerSizeChange);
        }

        private ZipIOLocalFileHeader() 
        {
        }            

        private void Validate ()
        {
            if (_signature != _signatureConstant)
            {
                throw new FileFormatException(SR.Get(SRID.CorruptedData));
            }

            if (_fileNameLength != _fileName.Length)
            {
                throw new FileFormatException(SR.Get(SRID.CorruptedData));
            }

            ZipArchive.VerifyVersionNeededToExtract(_versionNeededToExtract);

            // encryption is not supported
            if (EncryptedFlag)
                throw new NotSupportedException(SR.Get(SRID.ZipNotSupportedEncryptedArchive)); 

            // if verson is below 4.5 make sure that ZIP 64 extra filed isn't present 
            // if it is it might be a security concern 
            if ((_versionNeededToExtract < (UInt16)ZipIOVersionNeededToExtract.Zip64FileFormat) &&
                (_extraField.Zip64ExtraFieldUsage != ZipIOZip64ExtraFieldUsage.None))
            {
                throw new FileFormatException(SR.Get(SRID.CorruptedData));                
            }

            // ZIP 64 validation; if extra field is present it 
            // must have both compressed and uncompressed size in the local file header 
            // according to the appnote 
            if ((_extraField.Zip64ExtraFieldUsage != ZipIOZip64ExtraFieldUsage.None)  &&
                (_extraField.Zip64ExtraFieldUsage !=  
                            (ZipIOZip64ExtraFieldUsage.CompressedSize | 
                            ZipIOZip64ExtraFieldUsage.UncompressedSize)))
            {
                throw new FileFormatException(SR.Get(SRID.CorruptedData));
            }

            if (_extraFieldLength != _extraField.Size)
            {
                throw new FileFormatException(SR.Get(SRID.CorruptedData));
            }

            if ((CompressionMethod != CompressionMethodEnum.Stored) && 
                (CompressionMethod != CompressionMethodEnum.Deflated))
            {
                throw new NotSupportedException(SR.Get(SRID.ZipNotSupportedCompressionMethod));
            }            
        }

        private const int _fixedMinimalRecordSize = 30;
            
        private const UInt32 _signatureConstant = 0x04034b50;
        private UInt32 _signature = _signatureConstant;
        private UInt16 _versionNeededToExtract;
        private UInt16 _generalPurposeBitFlag;  
        private UInt16 _compressionMethod;
        private UInt32 _lastModFileDateTime;
        private UInt32 _crc32;
        private UInt32 _compressedSize;
        private UInt32 _uncompressedSize;
        private UInt16 _fileNameLength;
        private UInt16 _extraFieldLength;
        private byte[] _fileName;
        private ZipIOExtraField _extraField;

        private string _stringFileName;
        }
}
