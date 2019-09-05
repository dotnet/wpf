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
    internal class ZipIOLocalFileDataDescriptor
    {
        // used in streaming case
        internal static ZipIOLocalFileDataDescriptor CreateNew()
        {
            ZipIOLocalFileDataDescriptor descriptor = new ZipIOLocalFileDataDescriptor();
            descriptor._size = _fixedMinimalRecordSizeWithSignatureZip64;

            return descriptor;
        }

        internal static ZipIOLocalFileDataDescriptor ParseRecord(BinaryReader reader,  
                                    long compressedSizeFromCentralDir, 
                                    long uncompressedSizeFromCentralDir,
                                    UInt32 crc32FromCentralDir,
                                    UInt16 versionNeededToExtract)
        {
            ZipIOLocalFileDataDescriptor descriptor = new ZipIOLocalFileDataDescriptor();

            // There are 4 distinct scenario we would like to support 
            //   1.based on the appnote it seems that the structure of this record is following:
            //                  crc-32                          4 bytes
            //                  compressed size                 4 bytes (scenario 1.a has 8 bytes)
            //                  uncompressed size               4 bytes (scenario 1.a has 8 bytes)
            //
            //   2.based on files that we have been able to examine 
            //                  data descriptor signature        4 bytes  (0x08074b50)
            //                  crc-32                                  4 bytes
            //                  compressed size                   4 bytes (scenario 2.a has 8 bytes)
            //                  uncompressed size               4 bytes (scenario 2.a has 8 bytes)
            //
            // we can safely assume that this record is not the last one in the file, so let's just 
            // read the max Bytes required to store the largest structure , and compare results 

            // at most we are looking at 6 * 4  = 24 bytes      
            UInt32[] buffer = new UInt32[6];

            // let's try to match the smallest possible structure (3 x 4) 32 bit without signature 
            buffer[0] = reader.ReadUInt32(); 
            buffer[1] = reader.ReadUInt32(); 
            buffer[2] = reader.ReadUInt32(); 

            if (descriptor.TestMatch(_signatureConstant, 
                                    crc32FromCentralDir,
                                    compressedSizeFromCentralDir, 
                                    uncompressedSizeFromCentralDir,
                               _signatureConstant,      
                                    buffer[0],
                                    buffer[1],
                                    buffer[2]))
            {
                descriptor._size = _fixedMinimalRecordSizeWithoutSignature;
                return descriptor;
            }

            // let's try to match the next record size (4 x 4) 32 bit with signature 
            buffer[3] = reader.ReadUInt32(); 
            if (descriptor.TestMatch(_signatureConstant, 
                                    crc32FromCentralDir,
                                    compressedSizeFromCentralDir, 
                                    uncompressedSizeFromCentralDir,
                                 buffer[0],      
                                    buffer[1],
                                    buffer[2],
                                    buffer[3]))
            {
                descriptor._size = _fixedMinimalRecordSizeWithSignature;
                return descriptor;
            }

            // At this point prior to trying to match 64 bit structures we need to make sure that version is high enough 
            if (versionNeededToExtract <  (UInt16)ZipIOVersionNeededToExtract.Zip64FileFormat)
            {
                throw new FileFormatException(SR.Get(SRID.CorruptedData));
            }

            //let's try to match the 64 bit structures 64 bit without signature 
            buffer[4] = reader.ReadUInt32();             
            if (descriptor.TestMatch(_signatureConstant, 
                                    crc32FromCentralDir,
                                    compressedSizeFromCentralDir, 
                                    uncompressedSizeFromCentralDir,
                                 _signatureConstant,      
                                    buffer[0],
                                    ZipIOBlockManager.ConvertToUInt64(buffer[1], buffer[2]),
                                    ZipIOBlockManager.ConvertToUInt64(buffer[3], buffer[4])))
            {
                descriptor._size = _fixedMinimalRecordSizeWithoutSignatureZip64;
                return descriptor;
            }            

            //let's try to match the 64 bit structures 64 bit with signature 
            buffer[5] = reader.ReadUInt32();             
            if (descriptor.TestMatch(_signatureConstant, 
                                    crc32FromCentralDir,
                                    compressedSizeFromCentralDir, 
                                    uncompressedSizeFromCentralDir,
                                 buffer[0],      
                                    buffer[1],
                                    ZipIOBlockManager.ConvertToUInt64(buffer[2], buffer[3]),
                                    ZipIOBlockManager.ConvertToUInt64(buffer[4], buffer[5])))
            {
                descriptor._size = _fixedMinimalRecordSizeWithSignatureZip64;
                return descriptor;
            }   

            // we couldn't match anything at this point we need to fail 
            throw new FileFormatException(SR.Get(SRID.CorruptedData));
        }

        /// <summary>
        /// Save - persist to given binary writer
        /// </summary>
        /// <param name="writer"></param>
        internal void Save(BinaryWriter writer)
        {
            // we only supposed to be saving a brand new record which is created with signature in Zip64 Mode 
            // this only supported for streaming publishing cases. 
            // For editing scenarios we never save them (not even preserve them), as any editing (even trivial moving of record around)
            // will result in switching File Item to a non-streaming mode without data desciptor 
            Invariant.Assert(_size == _fixedMinimalRecordSizeWithSignatureZip64);

            // always persist Zip64 version signature
            writer.Write(_signatureConstant);               // this is apparently optional, but omitting it causes wzzip to complain
            writer.Write(_crc32);
            writer.Write((UInt64)_compressedSize);          // 8 bytes
            writer.Write((UInt64)_uncompressedSize);        // 8 bytes
        }

        internal long Size
        {
            get
            {
                return _size;
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
                return _compressedSize;
            }
            set
            {
                Invariant.Assert((value >= 0), "CompressedSize must be non-negative");

                _compressedSize = value;
            }
        }

        internal long UncompressedSize
        {
            get
            {
                return _uncompressedSize;
            }
            set
            {
                Invariant.Assert((value >= 0), "UncompressedSize must be non-negative");

                _uncompressedSize = value;
            }
        }


        private bool TestMatch(UInt32 signature,
                                                UInt32 crc32FromCentralDir,
                                                long compressedSizeFromCentralDir, 
                                                long uncompressedSizeFromCentralDir,
                                                UInt32 suspectSignature,
                                                UInt32 suspectCrc32,
                                                UInt64 suspectCompressedSize, 
                                                UInt64 suspectUncompressedSize)
        {
            checked
            {
                // Don't compare compressedSize and uncompressedSize as long since the sniffed
                //  bytes might not be the actual match and can be bigger than Int64.MaxValue
                //  Convert them as long only if it is a match
                if  ((signature == suspectSignature) &&
                     ((UInt64) compressedSizeFromCentralDir == suspectCompressedSize) &&
                     ((UInt64) uncompressedSizeFromCentralDir == suspectUncompressedSize) &&
                     (crc32FromCentralDir == suspectCrc32))
                {
                    _signature = suspectSignature;
                    _compressedSize = (long) suspectCompressedSize;
                    _uncompressedSize = (long) suspectUncompressedSize;
                    _crc32 = suspectCrc32;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
            
        private  const UInt32 _signatureConstant  = 0x08074b50;
        private const int _fixedMinimalRecordSizeWithoutSignature = 12;
        private const int _fixedMinimalRecordSizeWithSignature = 16;
        private const int _fixedMinimalRecordSizeWithoutSignatureZip64 = 20;
        private const int _fixedMinimalRecordSizeWithSignatureZip64 = 24;
        
        private int _size;

        private UInt32 _signature = _signatureConstant;
        private UInt32 _crc32;
        private long _compressedSize = 0;
        private long _uncompressedSize = 0;
}
}
