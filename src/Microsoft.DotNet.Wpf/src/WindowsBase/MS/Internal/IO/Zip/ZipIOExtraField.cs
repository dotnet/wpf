// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
//
// Description:
//  This is an internal class that is used to implement parsing and 
//  of the extra field optionally present in the fileHeader and Central Dir 
// 

using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Globalization;
using System.Runtime.Serialization;
using System.Windows;  
using MS.Internal.WindowsBase;

namespace MS.Internal.IO.Zip
{
    internal class ZipIOExtraField
    {
        internal static ZipIOExtraField CreateNew(bool createPadding)          
        {
            // we have been asked to create a new record, current zip io implementation will add at most one Zip64 block
            ZipIOExtraField extraField = new ZipIOExtraField();
            
            extraField._zip64Element = ZipIOExtraFieldZip64Element.CreateNew();

            if (createPadding)
            {
                extraField._paddingElement = ZipIOExtraFieldPaddingElement.CreateNew();
            }

            return extraField;
        }
                                
        internal static ZipIOExtraField ParseRecord(BinaryReader reader, ZipIOZip64ExtraFieldUsage zip64extraFieldUsage, ushort expectedExtraFieldSize)
        {
            // most of the files are not ZIP 64, and instead of trying to parse it we should create new empty record 
            if (expectedExtraFieldSize == 0)
            {
                if (zip64extraFieldUsage != ZipIOZip64ExtraFieldUsage.None) 
                {   
                    // in case there is an expectation by the caller for a non empty record we should throw 
                    throw new FileFormatException(SR.Get(SRID.CorruptedData));                    
                }

                // We are creating Extra Fields for the existing Local File Header,
                //  so no need to create a new padding field
                return CreateNew(false);
            }

            ZipIOExtraField extraField = new ZipIOExtraField();
            
            // Parse all Extra elements from Extra Field
            while (expectedExtraFieldSize > 0)
            {
                if (expectedExtraFieldSize < ZipIOExtraFieldElement.MinimumSize)
                {
                    throw new FileFormatException(SR.Get(SRID.CorruptedData));                    
                }
                
                ZipIOExtraFieldElement newElement = ZipIOExtraFieldElement.Parse(reader, zip64extraFieldUsage);
                ZipIOExtraFieldZip64Element zip64Element = newElement as ZipIOExtraFieldZip64Element;
                ZipIOExtraFieldPaddingElement paddingElement = newElement as ZipIOExtraFieldPaddingElement;

                // if we have found the Zip 64 record. let's remember it
                if (zip64Element != null)
                {
                    if (extraField._zip64Element != null)
                    {
                        // multiple ZIP 64 extra fields are not allowed
                        throw new FileFormatException(SR.Get(SRID.CorruptedData));                    
                    }

                    extraField._zip64Element = zip64Element;
                }
                else if (paddingElement != null)
                {
                    if (extraField._paddingElement != null)
                    {
                        // multiple padding extra fields are not allowed
                        throw new FileFormatException(SR.Get(SRID.CorruptedData));                    
                    }

                    extraField._paddingElement = paddingElement;
                }
                else
                {
                    if (extraField._extraFieldElements == null)
                        extraField._extraFieldElements = new ArrayList(3);    // we expect to see a few records there, as it sould have been produced by other authoring systems.   

                    // any other instances of extra fields with the same id are allowed
                    extraField._extraFieldElements.Add(newElement);
                }
                    
                checked { expectedExtraFieldSize -= newElement.Size; }
            }
            
            // if we didn't end up at the exact expected position, we are treating this as a corrupted file
            if (expectedExtraFieldSize != 0) 
            {
                throw new FileFormatException(SR.Get(SRID.CorruptedData));            
            }

            // As we treat the ZIP 64 extra field as optional for all version >= 4.5 
            // we need to explicitly consider a case when it is missing 
            if (extraField._zip64Element == null)
            {
                extraField._zip64Element = ZipIOExtraFieldZip64Element.CreateNew();
            }

            /////////////////////////////////////////////////////////////////////
            //              extraField.Validate();
            // an instance Validate function is removed to fix FxCop violation, please add it back
            // if extra validation steps are required
            //
            // we are checking for uniqueness of the Zip 64 header ID in the Parse function.
            // Although it might be a good idea to check for record id uniqueness in general, 
            // we are treating the rest of the field as a bag of bits, so it is probably not worth it to 
            // search for other duplicate ID especially as appnote considers ID duplication a possibility 
            // and even suggest a work around for file producers. 
            /////////////////////////////////////////////////////////////////////

            return extraField;
        }

        internal void Save(BinaryWriter writer)
        {
            // write Out the Zip 64  extra field first 
            if (_zip64Element.SizeField > 0)
            {
                _zip64Element.Save(writer);
            }

            // write Out the padding field
            if (_paddingElement != null)
            {
                _paddingElement.Save(writer);
            }

            if (_extraFieldElements != null)
            {
                foreach (ZipIOExtraFieldElement extraFieldElement in _extraFieldElements)
                {
                    extraFieldElement.Save(writer);
                }
            }
        }

        // Add or remove padding for the given size change
        internal void UpdatePadding(long size)
        {
            // If the local file header changed more than 100 bytes, it means
            //  there are some logical errors
            Debug.Assert(Math.Abs(size) <= 100);

            // The header size change should be no more than what we can hold in UInt16
            if (Math.Abs(size) > UInt16.MaxValue)
                return;

            // Header size increased; need to remove padding if there is an existing padding structure
            if (size > 0 && _paddingElement != null)
            {
                // There is enough padding left over to do size adjustment
                // No need to use checked{} since _paddingElement.PaddingSize >= size
                if (_paddingElement.PaddingSize >= size)
                    _paddingElement.PaddingSize -= (UInt16) size;
                // The size of the whole padding structure exactly matches the size change
                else if (_paddingElement.Size == size)
                {
                    // Then the padding structure can be completely removed
                    //  to accommodate the size change
                    _paddingElement = null;
                }

                return;
            }

            // Header size decreased; need to add padding
            if (size < 0)
            {
                // Padding structure is not there but, the size change is big enough for one
                //  to be created
                if (_paddingElement == null)
                {
                    // No need to use checked{} since size is long type
                    //  and size < 0
                    //  and (ZipIOExtraFieldPaddingElement.MinimumFieldDataSize + ZipIOExtraFieldElement.MinimumSize)
                    //      is small number that can not cause the overflow
                    size += (ZipIOExtraFieldPaddingElement.MinimumFieldDataSize
                                + ZipIOExtraFieldElement.MinimumSize);
                    
                    if (size >= 0)
                    {
                        _paddingElement = new ZipIOExtraFieldPaddingElement();
                        // No need to use checked{} since size > 0 and less than UInt16.MaxValue
                        _paddingElement.PaddingSize = (UInt16) size;
                    }
                }
                else
                {
                    // Check if we hit the max padding allowed
                    if ((_paddingElement.PaddingSize - size) > UInt16.MaxValue)
                        return;

                    // No need to use checked{} since we already check the overflow
                    _paddingElement.PaddingSize = (UInt16) (_paddingElement.PaddingSize - size);
                }
            }
        }

        internal UInt16 Size
        {
            get
            {
                UInt16 size = 0;

                if (_extraFieldElements != null)
                {
                    foreach (ZipIOExtraFieldElement extraFieldElement in _extraFieldElements)
                    {
                        checked{size += extraFieldElement.Size;}
                    }
                }

                checked{size += _zip64Element.Size;}

                if (_paddingElement != null)
                {
                    checked { size += _paddingElement.Size; }
                 }
                
                return size;
            }
        }

   
        internal ZipIOZip64ExtraFieldUsage Zip64ExtraFieldUsage
        {
            get
            {
                return _zip64Element.Zip64ExtraFieldUsage;
            }
            set 
            {
                _zip64Element.Zip64ExtraFieldUsage = value;                
            }
        }

        internal UInt32 DiskNumberOfFileStart
        {
            get
            {
                return _zip64Element.DiskNumber;
            }
        }

        internal long OffsetOfLocalHeader
        {
            get
            {
                return _zip64Element.OffsetOfLocalHeader;
            }
            set
            {
                _zip64Element.OffsetOfLocalHeader = value;
            }
        }
        
        internal long CompressedSize
        {
            get
            {
                return _zip64Element.CompressedSize;
            }
            set
            {
                _zip64Element.CompressedSize = value;
            }
        }

        internal long UncompressedSize
        {
            get
            {
                return _zip64Element.UncompressedSize;
            }
            set
            {
                _zip64Element.UncompressedSize = value;
            }
        }

        private ZipIOExtraField()
        {   
        }
           
        private ArrayList _extraFieldElements; 
        private ZipIOExtraFieldZip64Element _zip64Element;
        private ZipIOExtraFieldPaddingElement _paddingElement;
    }
}
