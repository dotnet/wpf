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
using System.Collections.Specialized;       // OrderedDictionary
using System.Globalization;
using System.Windows;  
using MS.Internal.WindowsBase;

namespace MS.Internal.IO.Zip
{
    internal class ZipIOCentralDirectoryBlock : IZipIOBlock
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
                long result = 0;
                if (CentralDirectoryDictionary.Count > 0)
                {
                    foreach(ZipIOCentralDirectoryFileHeader fileHeader in CentralDirectoryDictionary.Values)
                    {
                        checked{result += fileHeader.Size;}
                    }

// disable creation/parsing of zip archive digital signatures
#if ArchiveSignaturesEnabled
                    if (_centralDirectoryDigitalSignature != null)
                    {
                        checked{result += _centralDirectoryDigitalSignature.Size;}
                    }
#endif
                }
                return result;
            }
        }

        // This property will only return reliable result if Update is called prior  
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
            if (_dirtyFlag) 
            {        
                // Central directory is an optional component of the ZIP Archive 
                // we need to save it if it isn't empty
                if (CentralDirectoryDictionary.Count > 0)
                {
                    BinaryWriter writer = _blockManager.BinaryWriter;

                    // Emit entries in the same order as the corresponding file items as this
                    // improves interoperability with tools that expect this convention.

                    // Streaming mode must be handled differently
                    if (_blockManager.Streaming)
                    {
                        // In Streaming mode we cannot rely on the order that entries were inserted via AddFiles()
                        // as files can be closed in different order than they are added.

                        //   NOTE: Neither ZipIOBlockManager._blockList nor CentralDirectoryDicstionry
                        //      are NOT in offset order

#if DEBUG
                        long lastOffset = -1;
#endif

                        // collect all file headers in central directory into a local list for sorting
                        SortedList blockList = new SortedList(CentralDirectoryDictionary.Count);

                        // We know that in Streaming mode there can be no RawDataFile blocks in
                        // the block list.  Therefore, we can emit our headers in the order that they
                        // appear in the block list.  
                        foreach (ZipIOCentralDirectoryFileHeader header in CentralDirectoryDictionary.Values)
                        {
                            blockList.Add(header.OffsetOfLocalHeader, header);
                        }

                        // then write out the files headers for central directory in sorted order
                        foreach (ZipIOCentralDirectoryFileHeader header in blockList.Values)
                        {
                            header.Save(writer);
#if DEBUG
                            Debug.Assert(lastOffset < header.OffsetOfLocalHeader, "Sort order violated");
                            lastOffset = header.OffsetOfLocalHeader;
#endif
                        }
                    }
                    else
                    {
                        // Non-streaming mode - CentralDirectoryDictionary has correct order.
                        // Assume correct location if streaming - otherwise explicitly seek.
                        if (_blockManager.Stream.Position != _offset)
                        {
                            // we need to seek 
                            _blockManager.Stream.Seek(_offset, SeekOrigin.Begin);
                        }

                        // Save the headers in the order they were added as this matches the physical offsets
                        foreach (ZipIOCentralDirectoryFileHeader fileHeader in CentralDirectoryDictionary.Values)
                        {
                            fileHeader.Save(writer);
                        }
}

                    // disable creation/parsing of zip archive digital signatures
#if ArchiveSignaturesEnabled
                    //central directory dig sig is optional 
                    if (_centralDirectoryDigitalSignature != null)
                    {
                        _centralDirectoryDigitalSignature.Save(writer);
                    }
#endif
                    writer.Flush();                    
                }

                _dirtyFlag = false;                
            }
        }

        public void UpdateReferences(bool closingFlag)
        {
            // we just need to ask Block Manager for the new Values for each header 
            // there are 2 distinct cases here 
            //    1. local file data is mapped . loaded and might have been changed in size and position 
            //    2. local file data is not  loaded and might have been changed only in position (not in size) 

            foreach(IZipIOBlock block in _blockManager)
            {
                ZipIOLocalFileBlock localFileBlock = block as ZipIOLocalFileBlock;
                ZipIORawDataFileBlock rawDataFileBlock = block as ZipIORawDataFileBlock;
                    
                if (localFileBlock != null)
                {
                    // this is case 1 data is mapped and loaded, so we only need to find the matching 
                    // Centraldirectory record and update it 
                    Debug.Assert(CentralDirectoryDictionary.Contains(localFileBlock.FileName)); 

                    ZipIOCentralDirectoryFileHeader centralDirFileHeader = 
                                    (ZipIOCentralDirectoryFileHeader)CentralDirectoryDictionary[localFileBlock.FileName]; 
                    
                    if (centralDirFileHeader.UpdateIfNeeded(localFileBlock))
                    {
                        //update was required let's mark ourselves as dirty 
                        _dirtyFlag = true;
                    }
                }
                            //check whether we deal with raw data block and it was moved 
                else if (rawDataFileBlock != null)
                {
                    long diskImageShift = rawDataFileBlock.DiskImageShift;
                    if (diskImageShift != 0)
                    {
                        //this is case #2 data isn't loaded based on the shift in the RawData Block
                        // we need to move all overlapping central directory references 
                        foreach (ZipIOCentralDirectoryFileHeader centralDirFileHeader in CentralDirectoryDictionary.Values)
                        {
                            // check whether central dir header points into the region of a moved RawDataBlock 
                            if (rawDataFileBlock.DiskImageContains(centralDirFileHeader.OffsetOfLocalHeader))
                            {
                                centralDirFileHeader.MoveReference(diskImageShift);
                                _dirtyFlag = true;
                            }
                        }
                    }
                }
            }
        }

        public PreSaveNotificationScanControlInstruction PreSaveNotification(long offset, long size)
        {
            // we can safely ignore this notification as we do not keep any data 
            // after parsing on disk. Everything is in memory, it is ok to override 
            // original Central directory without any additional backups

            // we can also safely state that there is no need to continue the PreSafeNotification loop 
            // as all the blocks after the central directory (EOCD, Zip64 ....) do not have 
            // data that is buffered on disk
            return PreSaveNotificationScanControlInstruction.Stop;
        }
    
        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------
        // although Zip 64 supports 64 bit counter for the number of 
        // entries in the central directory, we have chossen to not 
        // support those scenarios and stick wit the basic CLR type 
        //          int Collections.Count {get;}        
        internal int Count
        {
            get
            {
                return CentralDirectoryDictionary.Count;
            }
        }

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        internal static ZipIOCentralDirectoryBlock SeekableLoad(ZipIOBlockManager blockManager)
        {
            // get proper values from  zip 64 records request will be redirected to the 
            // regular EOCD if ZIP 64 record wasn't originated from the parsing 

            ZipIOZip64EndOfCentralDirectoryBlock zip64EOCD = blockManager.Zip64EndOfCentralDirectoryBlock;
        
            blockManager.Stream.Seek(zip64EOCD.OffsetOfStartOfCentralDirectory, SeekOrigin.Begin);

            ZipIOCentralDirectoryBlock block = new ZipIOCentralDirectoryBlock(blockManager);
            
            block.ParseRecord(blockManager.BinaryReader, 
                                            zip64EOCD.OffsetOfStartOfCentralDirectory, 
                                            zip64EOCD.TotalNumberOfEntriesInTheCentralDirectory,
                                            zip64EOCD.SizeOfCentralDirectory);

            return block;
        }

        internal static ZipIOCentralDirectoryBlock CreateNew(ZipIOBlockManager blockManager)          
        {
            ZipIOCentralDirectoryBlock block = new ZipIOCentralDirectoryBlock(blockManager);

            block._offset = 0;              // it just an initial value, that will be adjusted later 
                                                   // it doesn't matter whether this offset overlaps anything or not 
                                                            
            block._dirtyFlag = true;

            // this dig sig is optional if we ever wanted to make this record, we would need to call
            //       ZipIOCentralDirectoryDigitalSignature.CreateNew();
            block._centralDirectoryDigitalSignature = null;  

            return block;
        }

        // This properrty returns current snapsot which might be out of date 
        // if there were changes after parsing or last UpdateReferences call 
        internal bool IsZip64BitRequiredForStoring
        {
            get
            {
                // These values are duplicated the EndOfCentralDirectory record
                // and if any of them are to big we need to introduce 
                //      Zip64 end of central directory record
                //      Zip64 end of central directory locator
                return (Count >= UInt16.MaxValue) ||  
                            (Offset >= UInt32.MaxValue) ||
                            (Size >= UInt32.MaxValue);
            }
        }

        internal void AddFileBlock(ZipIOLocalFileBlock fileBlock)
        {
            _dirtyFlag = true;

            ZipIOCentralDirectoryFileHeader fileHeader = 
                        ZipIOCentralDirectoryFileHeader.CreateNew
                                                            (_blockManager.Encoding, fileBlock);
            
            CentralDirectoryDictionary.Add(fileHeader.FileName, fileHeader);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <remarks>precondition: caller must ensure that fileName exists</remarks>
        internal void RemoveFileBlock(string fileName)
        {
            _dirtyFlag = true;

            CentralDirectoryDictionary.Remove(fileName);
            
            if (CentralDirectoryDictionary.Count == 0)
            {
                // in case of the the last one ,we also need to drop the signature record 
                _centralDirectoryDigitalSignature = null;
            }
        }

        internal bool FileExists(string fileName)  
        {
            return CentralDirectoryDictionary.Contains(fileName);
        }

        // this function should be used carefully as it returns reference to an object 
        // that is owned by CentralDirectoryBlock, and should be used by other classes only 
        // for querying information not for updating it.
        internal ZipIOCentralDirectoryFileHeader GetCentralDirectoryFileHeader (string fileName)
        {
            return ((ZipIOCentralDirectoryFileHeader)CentralDirectoryDictionary[fileName]);
        }

        internal ICollection GetFileNamesCollection()
        {
            return CentralDirectoryDictionary.Keys;
        }

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        private ZipIOCentralDirectoryBlock(ZipIOBlockManager blockManager)
        {
            _blockManager = blockManager;
        }

        /// <summary>
        /// Compare FileOffsets for LocalFileHeaders - used by Sort() routine in ParseRecord
        /// </summary>
        private class HeaderFileOffsetComparer : IComparer
        {
            int IComparer.Compare(object o1, object o2)
            {
                ZipIOCentralDirectoryFileHeader h1 = o1 as ZipIOCentralDirectoryFileHeader;
                ZipIOCentralDirectoryFileHeader h2 = o2 as ZipIOCentralDirectoryFileHeader;
                Debug.Assert(h1 != null && h2 != null, "HeaderFileOffsetComparer: Comparing the wrong data types");

                // avoid boxing - don't cast long value to (IComparable)
                if (h1.OffsetOfLocalHeader > h2.OffsetOfLocalHeader)
                    return 1;
                else if (h1.OffsetOfLocalHeader < h2.OffsetOfLocalHeader)
                    return -1;
                else
                    return 0;
            }
        }

        private void ParseRecord (BinaryReader reader, 
                                                        long centralDirectoryOffset, 
                                                        int centralDirectoryCount,  
                                                        long expectedCentralDirectorySize)
        {
            if (centralDirectoryCount > 0)
            {
                // collect all headers into a local array list for sorting
                SortedList headerList = new SortedList(centralDirectoryCount);
                ZipIOCentralDirectoryFileHeader header;
                for (int i = 0; i < centralDirectoryCount; i++)
                {
                    header = ZipIOCentralDirectoryFileHeader.ParseRecord(reader, _blockManager.Encoding);
                    headerList.Add(header.OffsetOfLocalHeader, header);
                }

                if (reader.BaseStream.Position - centralDirectoryOffset > expectedCentralDirectorySize)
                {   // it looks like a corrupted file, as we have parsed more than central directory supposed to contain 
                    throw new FileFormatException(SR.Get(SRID.CorruptedData));
                }

                // then add to the ordered dictionary in sorted order
                foreach (ZipIOCentralDirectoryFileHeader fileHeader in headerList.Values)
                {
                    // at this point fileHeader.FileName is normalized using 
                    // the ZipIOBlockManager.ValidateNormalizeFileName 
                    CentralDirectoryDictionary.Add(fileHeader.FileName, fileHeader);
                }

                //load central directory [digital signature] - this has nothing to 
                // do with OPC digital signing 
                // this record is optional, and the function might return null 
                _centralDirectoryDigitalSignature = ZipIOCentralDirectoryDigitalSignature.ParseRecord(reader);
            }

            _offset = centralDirectoryOffset;
            _dirtyFlag = false;

            Validate(expectedCentralDirectorySize);
        }

        private void Validate(long expectedCentralDirectorySize)
        {
            checked
            {
                    // We only have information about the Compressed data size and the offset of the 
                    // local headers. We do not have information about the size of the local header 
                    // which varies depending on the file name and the extra field records size. 
                    // (Although we do know the expected size of the file name, there is no way to 
                    // predict the extra field size, for example it might have a padding record that we use 
                    // optimize Disk IO for ZIP 64 scenarios). 
                    // We are going to make sure that Blocks do not overlap each other and do not 
                    // overlap Central Directory
                
                long checkedMark = 0;
                
                foreach (ZipIOCentralDirectoryFileHeader fileHeader in CentralDirectoryDictionary.Values)
                {
                    if ((checkedMark == 0) && (fileHeader.OffsetOfLocalHeader != 0))
                    {
                        // first block doesn't start at 0
                        throw new FileFormatException(SR.Get(SRID.CorruptedData));
                    }
                    else if (fileHeader.OffsetOfLocalHeader < checkedMark)
                    {
                        // the current block overlaps the previously analyzed block
                        throw new FileFormatException(SR.Get(SRID.CorruptedData));
                    }

                    // we move the checked mark up by the sum of the compressed size file name size 
                    // and the fixed minimal Local file header size 
                    checkedMark += fileHeader.CompressedSize + 
                                                ZipIOLocalFileHeader.FixedMinimalRecordSize + 
                                                fileHeader.FileName.Length;
                }

                // now we can ensure that that checked mark didn't reach over the start of the Central directory 
                 if (_offset < checkedMark)
                {
                    // the central directory block overlaps the last file block 
                    throw new FileFormatException(SR.Get(SRID.CorruptedData));
                }

                 //check the total parsed size of the central directory against value declared in EOCd or ZIP64 EOCD records 
                 if (Size != expectedCentralDirectorySize)
                {
                    // the central directory block overlaps the last file block 
                    throw new FileFormatException(SR.Get(SRID.CorruptedData));
                }

                // we should also check ofr presence of gaps between 
                //              Central Directory
                //              ZIP64 EOCD 
                //              ZIP 64 EOCD locator
                //              EOCD
                // Zip64Eocd and Zip64EocdLocator must be either present or absent together
                Debug.Assert(! (_blockManager.Zip64EndOfCentralDirectoryBlock.Size==0)         
                                                    ^ 
                                            (_blockManager.Zip64EndOfCentralDirectoryLocatorBlock.Size==0)); 

                if (_blockManager.Zip64EndOfCentralDirectoryBlock.Size==0)
                {
                    // no ZIP 64 record 
                    if (_offset + expectedCentralDirectorySize != _blockManager.EndOfCentralDirectoryBlock.Offset)
                    {
                        throw new FileFormatException(SR.Get(SRID.CorruptedData));
                    }
                }
                else
                {
                    // ZIP 64 records present
                    if ((_offset + expectedCentralDirectorySize 
                                        != _blockManager.Zip64EndOfCentralDirectoryBlock.Offset) ||
                                        
                         (_blockManager.Zip64EndOfCentralDirectoryBlock.Offset + _blockManager.Zip64EndOfCentralDirectoryBlock.Size 
                                        != _blockManager.Zip64EndOfCentralDirectoryLocatorBlock.Offset) ||

                         (_blockManager.Zip64EndOfCentralDirectoryLocatorBlock.Offset + _blockManager.Zip64EndOfCentralDirectoryLocatorBlock.Size 
                                        != _blockManager.EndOfCentralDirectoryBlock.Offset))
                    {
                        throw new FileFormatException(SR.Get(SRID.CorruptedData));
                    }
                }
            }                
        }

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------
        private IDictionary CentralDirectoryDictionary 
        {
            get
            {
                if (_centralDirectoryDictionary == null)
                {
                    // StringComparer.Ordinal guarantees ordinal, case-sensitive comparison for both cases.

                    // if streaming - order is unimportant for us and we can use the cheaper Hashtable
                    if (_blockManager.Streaming)
                    {
                        // We take our order during Save() from the physical order of the elements of the
                        // block table in BlockManager.
                        _centralDirectoryDictionary = new Hashtable(_centralDirectoryDictionaryInitialSize, StringComparer.Ordinal);
                    }
                    else
                    {
                        // This ordered dictionary serves two purposes.  It allows hash-table lookup by file name
                        // and it also maintains the physical order of the blocks on disk.  Like any OrderedDictionary,
                        // any of the enumerator, or integer indexer will return the items in the order that they were added.
                        _centralDirectoryDictionary = new OrderedDictionary(_centralDirectoryDictionaryInitialSize, StringComparer.Ordinal);
                    }
                }
                return _centralDirectoryDictionary;
            }
        }

        //------------------------------------------------------
        //
        //  Private Members
        //
        //------------------------------------------------------
        private const int _centralDirectoryDictionaryInitialSize = 50;

        // used in Parse
        private static IComparer _headerOffsetComparer = new HeaderFileOffsetComparer();

        // This may be a HashTable (Streaming case) or an OrderedDictionary - see private property
        // for explanation.
        private IDictionary _centralDirectoryDictionary;
        
        private ZipIOCentralDirectoryDigitalSignature _centralDirectoryDigitalSignature;
        
        private ZipIOBlockManager _blockManager;

        private long _offset;
        private bool  _dirtyFlag;        
    }
}
