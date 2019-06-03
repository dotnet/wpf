// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
//
// Description:
//  This is an internal class that is used to implement parsing and editing of a generic extra field structure.   
//  It doesn't contain informaton about any specific (like Zip64) extra fields that are supported by 
//  this implementation. In order to isolate this from IO and streams, it deals with the data in the Byte[] form 


using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Globalization;
using System.Runtime.Serialization;
using System.Windows;

using MS.Internal.IO.Packaging;
using MS.Internal.WindowsBase;

namespace MS.Internal.IO.Zip
{
    /// <summary>
    ///  This is an internal class that is used to implement parsing and editing of generic extra field structures.   
    ///  It doesn't contain informaton about any specific (like Zip64) extra fields that are supported by 
    ///  this implementation. In order to isolate this from IO and streams, it deals with the data in the Byte[] form 
    /// </summary>
    internal class ZipIOExtraFieldElement
    {
        internal static ZipIOExtraFieldElement Parse(BinaryReader reader, ZipIOZip64ExtraFieldUsage zip64extraFieldUsage)
        {
            // we can safely parse the Id and Size here
            //  since the minimum size is checked from the caller (ZipIoExtraField)
            UInt16 id = reader.ReadUInt16();
            UInt16 size = reader.ReadUInt16();

            ZipIOExtraFieldElement newElement;

            if (id == ZipIOExtraFieldZip64Element.ConstantFieldId)  // Found Zip64 Extra Field element
            {
                newElement = new ZipIOExtraFieldZip64Element();
                ((ZipIOExtraFieldZip64Element) newElement).Zip64ExtraFieldUsage = zip64extraFieldUsage;
            }
            else if (id == ZipIOExtraFieldPaddingElement.ConstantFieldId)   // Found potential Padding Extra Field element
            {
                // Even if the id matches Padding Extra Element id, we cannot be sure if it is Microsoft defined one
                // until its signature is verified

                // This extra field matches Padding Extra Element id, but it is not big enough to be ours
                if (size < ZipIOExtraFieldPaddingElement.MinimumFieldDataSize)
                {
                    // Treat it as an unknown Extra Field element
                    newElement = new ZipIOExtraFieldElement(id);
                }
                else
                {
                    // Sniff bytes to check it it matches our Padding extra field signature
                    byte[] sniffedBytes = reader.ReadBytes(ZipIOExtraFieldPaddingElement.SignatureSize);
                    if (ZipIOExtraFieldPaddingElement.MatchesPaddingSignature(sniffedBytes))
                    {
                        // Signature matches the Padding Extra Field signature
                        newElement = new ZipIOExtraFieldPaddingElement();
                    }
                    else
                    {
                        // Signature doesn't match; treat it a an unknown Extra Field element
                        newElement = new ZipIOExtraFieldElement(id, sniffedBytes);
                    }
                }
            }
            else
            {
                newElement = new ZipIOExtraFieldElement(id);
            }

            // Parse the data field of the Extra Field element
            newElement.ParseDataField(reader, size);

            return newElement;
        }

        internal virtual void ParseDataField(BinaryReader reader, UInt16 size)
        {
            if (_data == null)
            {
                _data = reader.ReadBytes(size);

                //vaiadte that we didn't reach the end of stream too early 
                if (_data.Length != size)
                {
                    throw new FileFormatException(SR.Get(SRID.CorruptedData));
                }
            }
            else    // There were some data we sniffed already
            {
                Byte[] tempBuffer =  _data;
                _data = new Byte[size];

                Array.Copy(tempBuffer, _data, _size);   // _size contains the size of data in _data

                checked
                {
                    Debug.Assert(size >= _size);

                    if ((PackagingUtilities.ReliableRead(reader, _data, _size, size - _size) + _size) != size)
                        throw new FileFormatException(SR.Get(SRID.CorruptedData));
                }
            }
            _size = size;
        }

        internal virtual void Save(BinaryWriter writer)
        {
            Debug.Assert(_size == _data.Length);

            writer.Write(_id);
            writer.Write(_size);
            writer.Write(_data);
        }
     
        // This property calculates the value of the size record whch holds the size without the Id and without the size itself.
        // we are alkways guranteed that   Size == SizeField + 2 * sizeof(UInt16))
        internal virtual UInt16 SizeField
        {
            get
            {
                return _size;
            }
        }            

        // this fiels needs to be used very carefully as it retuns a writeble array 
        // element of which could be potentially modifyed by the caller 
        internal virtual byte[] DataField
        {
            get
            {
                return _data;
            }
        }            

        // This property calculates size of the field on disk (how many bytes need to be allocated on the disk)
        internal virtual UInt16 Size
        {
            get
            {
                return checked((UInt16) (_size +  _minimumSize)); // the real field size has 2 UInt16 fields for Id and size 
            }
        }

        // Minimum size of any type of Extra Field Element
        //  UInt16 id
        //  UInt16 size
        internal static UInt16 MinimumSize
        {
            get
            {
                return _minimumSize;
            }
        }

        //------------------------------------------------------
        //
        //  Private Constructor 
        //
        //------------------------------------------------------
        internal ZipIOExtraFieldElement(UInt16 id)
        {
            _id = id;
        }

        // data: sniffied data field
        private ZipIOExtraFieldElement(UInt16 id, byte[] data)
        {
            Debug.Assert(data != null);

            _id = id;
            _data = data;
            _size = checked((UInt16) data.Length);
        }

        //------------------------------------------------------
        //
        //  Private fields 
        //
        //------------------------------------------------------
        private UInt16 _id;
        private UInt16 _size;
        private byte[] _data;

        // UInt16 id:   2
        // UInt16 size: 2
        private static readonly UInt16 _minimumSize = (UInt16) (2 * sizeof(UInt16));
    }
}

