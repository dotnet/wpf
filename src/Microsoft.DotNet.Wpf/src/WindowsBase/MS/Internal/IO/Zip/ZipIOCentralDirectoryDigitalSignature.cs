// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
//
// Description:
//  This is an internal class that enables interactions with Zip archives
//  for OPC scenarios 
//
//   Hisotrical Note: 
//            Disable creation of signatures and throw on parse if signature found:
//              If we encounter archive with encrypted and /or signed data we consider those as 
//              “invalid OPC packages”. Even in case when client application has never requested
//              read/write operations with the encrypted and/or signed area of the archive.  Motivations include:
//              1. Security based 
//              2. It is inappropriate to silently ignore digital signatures because this may lead to users 
//                 assuming that the signature has been validated when in fact it has not.
//

using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Runtime.Serialization;
using System.Windows;  
using MS.Internal.WindowsBase;
    
namespace MS.Internal.IO.Zip
{
    internal class ZipIOCentralDirectoryDigitalSignature
    {
#if false
        internal static ZipIOCentralDirectoryDigitalSignature CreateNew()
         {
            ZipIOCentralDirectoryDigitalSignature record = new ZipIOCentralDirectoryDigitalSignature ();

            return record;
        }
#endif   
        internal static ZipIOCentralDirectoryDigitalSignature ParseRecord(BinaryReader reader)
        {
            // this record is optional, so let's check for presence of signature rightaway 

            //let's ensure we have at least enough data to cover _fixedMinimalRecordSize bytes of signature
            if ((reader.BaseStream.Length - reader.BaseStream.Position) < _fixedMinimalRecordSize)
            {
                return null;
            }

            UInt32 signatureValue = reader.ReadUInt32();
            if (signatureValue != _signatureConstant)
            {
                return null;
            }

            //at this point we can assume that Digital Signature Record is there 
            // Convention is to throw
            throw new NotSupportedException(SR.Get(SRID.ZipNotSupportedSignedArchive));

// disable creation/parsing of zip archive digital signatures
#if ArchiveSignaturesEnabled
            ZipIOCentralDirectoryDigitalSignature record = new ZipIOCentralDirectoryDigitalSignature ();

            record._signature = signatureValue;
            record._sizeOfData = reader.ReadUInt16();
            record._signatureData = reader.ReadBytes(record._sizeOfData );

            record.Validate();

            return record;
#endif
        }

// disable creation/parsing of zip archive digital signatures
#if ArchiveSignaturesEnabled
        internal void Save(BinaryWriter writer)
        {
            writer.Write(_signatureConstant);
            writer.Write(_sizeOfData);
            if (_sizeOfData > 0)
            {
                writer.Write(_signatureData , 0, _sizeOfData);
            }

            writer.Flush();
        }
        internal long Size
        {
            get
            {
                return _fixedMinimalRecordSize + _sizeOfData; 
            }
        }
#endif

        private ZipIOCentralDirectoryDigitalSignature() 
        {
        }

// disable creation/parsing of zip archive digital signatures
#if ArchiveSignaturesEnabled
        private void Validate ()
        {
            if (_signature != _signatureConstant)
            {
                throw new FileFormatException(SR.Get(SRID.CorruptedData));
            }

            if (_sizeOfData != _signatureData.Length)
            {
                throw new FileFormatException(SR.Get(SRID.CorruptedData));
            }
        }
#endif
        private const long _fixedMinimalRecordSize = 6;
    
        private const UInt32 _signatureConstant = 0x05054b50;

// disable creation/parsing of zip archive digital signatures
#if ArchiveSignaturesEnabled
        private UInt16 _sizeOfData;
        private UInt32 _signature = _signatureConstant;
        private byte[] _signatureData = null;
#endif
    }
}
