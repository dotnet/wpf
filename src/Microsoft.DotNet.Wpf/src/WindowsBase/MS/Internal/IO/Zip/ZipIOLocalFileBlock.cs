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
//

using System;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Collections;
using System.Runtime.Serialization;
using System.Globalization;
using System.Windows;

using MS.Internal.IO.Packaging; // For CompressStream
using MS.Internal.WindowsBase;
    
namespace MS.Internal.IO.Zip
{
    internal class ZipIOLocalFileBlock : IZipIOBlock, IDisposable
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
                CheckDisposed();
                return _offset;
            }
        }

        public long Size
        {
            get
            {
                CheckDisposed();

                checked
                {
                    long size = _localFileHeader.Size + _fileItemStream.Length;

                    if (_localFileDataDescriptor != null)
                    {
                        // we only account for the data descriptor 
                        // if it is there and data wasn't changed yet , 
                        // because we will discard it as a part of saving
                        size += _localFileDataDescriptor.Size;
                    }

                    return  size;
                }
            }
        }

        public bool GetDirtyFlag(bool closingFlag)
        {
                CheckDisposed();

                bool deflateStreamDirty = false;
                if (_deflateStream != null)
                    deflateStreamDirty = ((CompressStream) _deflateStream).IsDirty(closingFlag);

                //     !!! ATTENTION !!!!
                //  We know for a fact that ZipIoModeEnforcingStream doesn't perform any buffering and is never "dirty". 
                // In the past we had Dirty flag on the ZipIoModeEnforcing stream that was always false. Enumerating 
                // those flags had significant perf cost (allocating all the Enumerator classes). We are removing Dirty flag 
                // from the ZipIoModeEnforcingStream and all the processing code associated with that.
                //If at any point we choose to add some buffering to the ZipIoModeEnforcingStream  we will have to 
                // reintroduce Dirty state/flag and properly account for this value in the ZipIoLocalFileBlock.DirtyFlag.
                return _dirtyFlag || _fileItemStream.DirtyFlag || deflateStreamDirty; 
        }

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
        public void Move(long shiftSize)
        {
            CheckDisposed();
            Debug.Assert(!_blockManager.Streaming, "Not legal in Streaming mode");
        
            if (shiftSize != 0)
            {
                checked{_offset +=shiftSize;}
                _fileItemStream.Move(shiftSize);
                _dirtyFlag = true;
                Debug.Assert(_offset >=0);                
            }
        }

        /// <summary>
        /// Streaming-specific variant of Save()
        /// </summary>
        /// <param name="closingFlag"></param>
        internal void SaveStreaming(bool closingFlag)
        {
            CheckDisposed();

            Debug.Assert(_blockManager.Streaming, "Only legal in Streaming mode");

            if (GetDirtyFlag(closingFlag))
            {
                BinaryWriter writer = _blockManager.BinaryWriter;

                // write the local file header if not already done so
                if (!_localFileHeaderSaved)
                {
                    // our first access to the ArchiveStream - note our offset
                    _offset = _blockManager.Stream.Position;
                    _localFileHeader.Save(writer);
                    _localFileHeaderSaved = true;
                }

                FlushExposedStreams();

                //this will cause the actual write to disk, and it safe to do so,
                // because all we're in streaming mode and there is 
                // no data in the way
                _fileItemStream.SaveStreaming();

                // Data Descriptor required for streaming mode
                if (closingFlag)
                {
                    // now prior to possibly closing streams we need to preserve uncompressed Size 
                    // otherwise Length function will fail to give it to us later after closing 
                    _localFileDataDescriptor.UncompressedSize = _crcCalculatingStream.Length;

                    // calculate CRC prior to closing 
                    _localFileDataDescriptor.Crc32 = _crcCalculatingStream.CalculateCrc();

                    // If we are closing we can do extra things , calculate CRC , close deflate stream
                    // it is particularly important to close the deflate stream as it might hold some extra bytes 
                    // even after Flush()

                    // close outstanding streams to signal that we need new pieces if more data comes
                    CloseExposedStreams();

                    // in order to get proper compressed size we have to close the deflate stream 
                    if (_deflateStream != null)
                    {
                        _deflateStream.Close();
                        _fileItemStream.SaveStreaming(); // get the extra bytes emitted by the DeflateStream
                    }

                    _localFileDataDescriptor.CompressedSize = _fileItemStream.Length;

                    _localFileDataDescriptor.Save(writer);
                    _dirtyFlag = false;
                }
            }
        }
        /// <summary>
        /// Save()
        /// </summary>
        public void Save()
        {
            CheckDisposed();
            Debug.Assert(!_blockManager.Streaming, "Not legal in Streaming mode");

            // Note: This triggers a call to UpdateReferences() which will
            // discard any _localFileDataDescriptor.
            if (GetDirtyFlag(true)) // if we do not have closingFlag value (we should be using closingFlag=true as a more conservative approach) 
            {
                // We need to notify the _fileItemStream that we about to save our FileHeader; 
                // otherwise we might be overriding some of the FileItemStream data with the 
                // FileHeader. Specifically we are concerned about scenario when a previous 
                // block become large by just a couple of bytes, so that the PreSaveNotification 
                // issued prior to saving the previous block didn’t trigger caching of our FileItemStream, 
                // but we still need to make sure that the current FileHeader will not override any data 
                // in our FileItemStream. 
                _fileItemStream.PreSaveNotification(_offset, _localFileHeader.Size);

                //position the stream 
                BinaryWriter writer = _blockManager.BinaryWriter;
                if (_blockManager.Stream.Position != _offset)
                {
                    // we need to seek 
                    _blockManager.Stream.Seek(_offset, SeekOrigin.Begin);
                }

                _localFileHeader.Save(writer);

                //this will cause the actual write to disk, and it safe to do so,
                // because all overlapping data was moved out of the way 
                // by the calling BlockManager 
                _fileItemStream.Save();

                _dirtyFlag = false;                
            }
        }


        // !!! ATTENTION !!!! This function is only supposed to be called by 
        // Block Manager.Save which has proper protection to ensure no stack overflow will happen 
        // as a result of Stream.Flush calls which in turn result in BlockManager.Save calls 
        public void UpdateReferences(bool closingFlag)
        {
            Invariant.Assert(!_blockManager.Streaming);

            long uncompressedSize;
            long compressedSize;
            
            CheckDisposed();

            if (closingFlag)
            {
                CloseExposedStreams();
            }
            else
            {
                FlushExposedStreams();
            }

            // At this point we can update Local Headers with the proper CRC Value 
            // We can also update other Local File Header Values (compressed/uncompressed size)
            // we rely on our DirtyFlag property to properly account for all possbile modifications within streams 
            if (GetDirtyFlag(closingFlag))
            {
                // Remember the size of the header before update
                long headerSizeBeforeUpdate = _localFileHeader.Size;

                // now prior to possibly closing streams we need to preserve uncompressed Size 
                // otherwise Length function will fail to give it to us later after closing 
                uncompressedSize = _crcCalculatingStream.Length; 

                // calculate CRC prior to closing 
                _localFileHeader.Crc32 = _crcCalculatingStream.CalculateCrc();

                // If we are closing we can do extra things , calculate CRC , close deflate stream
                // it is particularly important to close the deflate stream as it might hold some extra bytes 
                // even after Flush()
                if (closingFlag)
                {
                    // we have got the CRC so we can close the stream 
                    _crcCalculatingStream.Close();

                    // in order to get proper compressed size we have to close the deflate stream 
                    if (_deflateStream != null)
                    {
                        _deflateStream.Close();    
                    }
                }

                if (_fileItemStream.DataChanged)
                {
                     _localFileHeader.LastModFileDateTime = ZipIOBlockManager.ToMsDosDateTime(DateTime.Now);
                }

                // get compressed size after possible closing Deflated stream            
                // as a result of some ineffeciencies in CRC calculation it might result in Seek in compressed stream 
                // and there fore switching mode and flushing extra compressed bytes 
                compressedSize = _fileItemStream.Length;

                // this will properly (taking into account ZIP64 scenario) update local file header
                // Offset is passed in to determine whether ZIP 64 is required for small files that 
                // happened to be located required 32 bit offset in the archive 
                _localFileHeader.UpdateZip64Structures(compressedSize, uncompressedSize, Offset);

                // Add/remove padding to compensate the header size change
                // NOTE: Padding needs to be updated only after updating all the header fields
                //  that can affect the header size
                _localFileHeader.UpdatePadding(_localFileHeader.Size - headerSizeBeforeUpdate);

                // We always save File Items in Non-streaming mode unless it wasn't touched 
                //in which case we leave them alone
                _localFileHeader.StreamingCreationFlag = false;
                _localFileDataDescriptor = null;

                // in some cases UpdateZip64Structures call might result in creation/removal
                // of extra field if such thing happened we need to move FileItemStream appropriatel 
                _fileItemStream.Move(checked(Offset + _localFileHeader.Size - _fileItemStream.Offset));

                _dirtyFlag = true;
            }
#if FALSE 
        // we would like to take this oppportunity and validate basic asumption 
        // that our GetDirtyFlag method is a reliable way to finding changes 
        // there is no scenario in which change will happened, affecting sizes 
        // and will not be registered by the GetDirtyFlag 
        // ???????????????????????
            else
            {
                // we even willing to recalculate CRC just in case for verification purposes 

                UInt32 calculatedCRC32 = CalculateCrc32();
                if  (!_localFileHeader.StreamingCreationFlag)
                {
                    Debug.Assert(_localFileHeader.Crc32 == calculatedCRC32);
                    Debug.Assert(_localFileHeader.CompressedSize == CompressedSize);
                    Debug.Assert(_localFileHeader.UncompressedSize == UncompressedSize);
                }
                else 
                {
                    Debug.Assert(_localFileDataDescriptor.Crc32 == calculatedCRC32);
                    Debug.Assert(_localFileDataDescriptor.CompressedSize == CompressedSize);
                    Debug.Assert(_localFileDataDescriptor.UncompressedSize == UncompressedSize);
                }
                    
///////////////////////////////////////////////////////////////////////                    

                // we do not have an initialized value for the compressed size in this case  
                compressedSize = _fileItemStream.Length;
                
                Debug.Assert(CompressedSize == compressedSize);
                Debug.Assert(UncompressedSize == uncompressedSize);
            }
#endif            
        }

        public PreSaveNotificationScanControlInstruction PreSaveNotification(long offset, long size)
        {
            CheckDisposed();
        
            // local file header and data descryptor are completely cached
            // we only need to worry about the actual data 
            return _fileItemStream.PreSaveNotification(offset, size);
        }

        /// <summary>
        /// Dispose pattern - required implementation for classes that introduce IDisposable
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);  // not strictly necessary, but if we ever have a subclass with a finalizer, this will be more efficient
        }

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        internal static ZipIOLocalFileBlock SeekableLoad (ZipIOBlockManager blockManager, 
                                                            string fileName)
        {
            Debug.Assert(!blockManager.Streaming);
            Debug.Assert(blockManager.CentralDirectoryBlock.FileExists(fileName));

            // Get info from the central directory             
            ZipIOCentralDirectoryBlock centralDir = blockManager.CentralDirectoryBlock;
            ZipIOCentralDirectoryFileHeader centralDirFileHeader = centralDir.GetCentralDirectoryFileHeader(fileName);

            long localHeaderOffset = centralDirFileHeader.OffsetOfLocalHeader;  
            bool folderFlag = centralDirFileHeader.FolderFlag;
            bool volumeLabelFlag = centralDirFileHeader.VolumeLabelFlag;
            
            blockManager.Stream.Seek(localHeaderOffset, SeekOrigin.Begin);
            
            ZipIOLocalFileBlock block = new ZipIOLocalFileBlock(blockManager, folderFlag, volumeLabelFlag);
            
            block.ParseRecord(
                    blockManager.BinaryReader, 
                    fileName,
                    localHeaderOffset,
                    centralDir,
                    centralDirFileHeader);

            return block;
        }

        internal static ZipIOLocalFileBlock CreateNew(ZipIOBlockManager blockManager, 
                                            string fileName, 
                                            CompressionMethodEnum compressionMethod, 
                                            DeflateOptionEnum deflateOption)          
        {
            //this should be ensured by the higher levels 
            Debug.Assert(Enum.IsDefined(typeof(CompressionMethodEnum), compressionMethod)); 
            Debug.Assert(Enum.IsDefined(typeof(DeflateOptionEnum), deflateOption)); 
            
            ZipIOLocalFileBlock block = new ZipIOLocalFileBlock(blockManager, false, false);

            block._localFileHeader = ZipIOLocalFileHeader.CreateNew
                                (fileName, 
                                blockManager.Encoding, 
                                compressionMethod, 
                                deflateOption, blockManager.Streaming);

            // if in streaming mode - force to Zip64 mode in case the streams get large
            if (blockManager.Streaming)
            {
                block._localFileDataDescriptor = ZipIOLocalFileDataDescriptor.CreateNew();
            }

            block._offset = 0; // intial value, that is not too important for the brand new File item 
            block._dirtyFlag = true;

            block._fileItemStream = new  ZipIOFileItemStream(blockManager,
                                        block,
                                        block._offset + block._localFileHeader.Size, 
                                        0); 

            // create deflate wrapper if necessary
            if (compressionMethod == CompressionMethodEnum.Deflated)
            {
                Debug.Assert(block._fileItemStream.Position == 0, "CompressStream assumes base stream is at position zero");
                // Pass bool to indicate that this stream is "new" and must be dirty so that
                // the valid empty deflate stream is emitted (2-byte sequence - see CompressStream for details).
                block._deflateStream = new CompressStream(block._fileItemStream, 0, true);

                block._crcCalculatingStream = new ProgressiveCrcCalculatingStream(blockManager, block._deflateStream);
            }
            else
            {
                block._crcCalculatingStream = new ProgressiveCrcCalculatingStream(blockManager, block._fileItemStream);
            }
            
            return block;
        }

        internal Stream GetStream(FileMode mode, FileAccess access)
        {
            CheckDisposed();

            // the main stream held by block Manager must be compatible with the request
            CheckFileAccessParameter(_blockManager.Stream, access);
                
            // validate mode and Access 
            switch(mode)
            {
                case FileMode.Create:
                    // Check to make sure that stream isn't read only 
                    if (!_blockManager.Stream.CanWrite)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.CanNotWriteInReadOnlyMode));
                    }
                    
                    if (_crcCalculatingStream != null && !_blockManager.Streaming)
                    {
                        _crcCalculatingStream.SetLength(0);
                    }
                    break;
                case FileMode.Open:
                    break;
                case FileMode.OpenOrCreate:
                    break;
                case FileMode.CreateNew:       
                    // because we deal with the GetStream call CreateNew is a really strange
                    // request, as the FileInfo is already there 
                    throw new ArgumentException(SR.Get(SRID.FileModeUnsupported, "CreateNew"));
                case FileMode.Append:
                    throw new ArgumentException(SR.Get(SRID.FileModeUnsupported, "Append"));
                case FileMode.Truncate:
                    throw new ArgumentException(SR.Get(SRID.FileModeUnsupported, "Truncate"));
                default:
                    throw new ArgumentOutOfRangeException("mode");
            }

            // Streaming mode: always return the same stream (if it exists already)
            Stream exposedStream;
            if (_blockManager.Streaming && _exposedPublicStreams != null && _exposedPublicStreams.Count > 0)
            {
                Debug.Assert(_exposedPublicStreams.Count == 1, "Should only be one stream returned in streaming mode");
                exposedStream = (Stream)_exposedPublicStreams[0];
            }
            else
            {
                Debug.Assert((!_blockManager.Streaming) || (_exposedPublicStreams == null), 
                                    "Should be first and only stream returned in streaming mode");

                exposedStream =  new ZipIOModeEnforcingStream(_crcCalculatingStream, access, _blockManager, this);

                RegisterExposedStream(exposedStream);
           }


            return exposedStream;             
        }


        // NOTE: This method should NOT be called anywhere else except from ZipIOModeEnforcingStream.Dispose(bool)
        // This is not designed to be the part of the cyclic process of flushing 
        internal void DeregisterExposedStream(Stream exposedStream)
        {
            Debug.Assert(_exposedPublicStreams != null);

            _exposedPublicStreams.Remove(exposedStream);
        }
        
        /// <summary>
        /// Throwes exception if object already Disposed/Closed. This is the only internal 
        /// (and not private CheckDisposed method). It ismade internal for ZipFileInfo to call 
        /// </summary> 
        internal void CheckDisposed()
        {
            if (_disposedFlag)
            {
                throw new ObjectDisposedException(null, SR.Get(SRID.ZipFileItemDisposed));            
            }
        }

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------
        internal UInt16 VersionNeededToExtract
        {
            get
            {
                CheckDisposed();

                return _localFileHeader.VersionNeededToExtract;
            }
        }

        internal UInt16 GeneralPurposeBitFlag
        {
            get
            {
                CheckDisposed();

                return _localFileHeader.GeneralPurposeBitFlag;
            }
        }

        internal CompressionMethodEnum CompressionMethod
        {
            get
            {
                CheckDisposed();

                return _localFileHeader.CompressionMethod;
            }
        }

        internal UInt32 LastModFileDateTime
        {
            get
            {
                CheckDisposed();

                return _localFileHeader.LastModFileDateTime;
            }
        }

        /// <summary>
        /// Return stale CRC value stored in the header.
        /// This property doesn't flush streams nor does it recalculates CRC BY DESIGN 
        /// all updates and revcalculations should be made as a part of the UpdateReferences function
        /// which is called by the BlockManager.Save
        /// </summary> 
        internal UInt32 Crc32
        {
            get
            {
                CheckDisposed();

                if (_localFileHeader.StreamingCreationFlag)
                {
                    Invariant.Assert(_localFileDataDescriptor != null);                
                    return _localFileDataDescriptor.Crc32;
                }
                else
                {
                    return _localFileHeader.Crc32;
                }
            }
        }

        /// <summary>
        /// Return stale Compressed Size based on the local file header
        /// This property doesn't flush streams, so it is possible that 
        /// this value will be out of date if Updatereferences isn't 
        /// called before getting this property 
        /// </summary> 
        internal long CompressedSize
        {
            get
            {
                CheckDisposed();

                if  (_localFileHeader.StreamingCreationFlag)
                {
                    Invariant.Assert(_localFileDataDescriptor != null);                
                    return _localFileDataDescriptor.CompressedSize;                
                }
                else
                {
                    return _localFileHeader.CompressedSize;
                }
            }
        }

        /// <summary>
        /// Return possibly stale Uncompressed Size based on the local file header
        /// This property doesn't flush streams, so it is possible that 
        /// this value will be out of date if Updatereferences isn't 
        /// called before getting this property 
        /// </summary> 
        internal long UncompressedSize
        {
            get
            {
                CheckDisposed();

                if  (_localFileHeader.StreamingCreationFlag)
                {
                    Invariant.Assert(_localFileDataDescriptor != null);                
                    return _localFileDataDescriptor.UncompressedSize;                
                }
                else
                {
                    return _localFileHeader.UncompressedSize;
                }                
            }
        }

        internal DeflateOptionEnum DeflateOption
        {
            get
            {
                CheckDisposed();
                return _localFileHeader.DeflateOption;
            }
        }

        internal string FileName
        {
            get
            {
                CheckDisposed();
                return _localFileHeader.FileName;
            }
        }
        
        internal bool FolderFlag
        {
            get
            {
                CheckDisposed();            
                return _folderFlag;
            }
        }                

        internal bool VolumeLabelFlag
        {
            get
            {
                CheckDisposed();            
                return _volumeLabelFlag;
            }
        }

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------
        /// <summary>
        /// Dispose(bool)
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // multiple calls are fine - just ignore them
                if (!_disposedFlag)
                {
                    try
                    {
                        // close all the public streams that have been exposed 
                        CloseExposedStreams();

                        _crcCalculatingStream.Close();

                        if (_deflateStream != null)
                            _deflateStream.Close();

                        _fileItemStream.Close();
                    }
                    finally
                    {
                        _disposedFlag = true;
                        _crcCalculatingStream = null;
                        _deflateStream = null;
                        _fileItemStream = null;
                    }
                }
            }
        }

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        private ZipIOLocalFileBlock(ZipIOBlockManager blockManager, 
                                                        bool folderFlag, 
                                                        bool volumeLabelFlag)
        {
            _blockManager = blockManager;
            _folderFlag = folderFlag;
            _volumeLabelFlag = volumeLabelFlag;
        }

        private void ParseRecord (BinaryReader reader, 
                                            string fileName, 
                                            long position,    
                                            ZipIOCentralDirectoryBlock centralDir,
                                            ZipIOCentralDirectoryFileHeader centralDirFileHeader)
        {
            CheckDisposed();
            Debug.Assert(!_blockManager.Streaming, "Not legal in Streaming mode");
            
            _localFileHeader = ZipIOLocalFileHeader.ParseRecord(reader, _blockManager.Encoding);

            // Let's find out whether local file descriptor is there or not 
            if (_localFileHeader.StreamingCreationFlag)
            {
                // seek forward by the uncompressed size 
                _blockManager.Stream.Seek(centralDirFileHeader.CompressedSize, SeekOrigin.Current);
                _localFileDataDescriptor = ZipIOLocalFileDataDescriptor.ParseRecord(reader, 
                                                        centralDirFileHeader.CompressedSize, 
                                                        centralDirFileHeader.UncompressedSize,
                                                        centralDirFileHeader.Crc32,
                                                        _localFileHeader.VersionNeededToExtract);
            }
            else
            {
                _localFileDataDescriptor = null;
            }

            _offset = position;
            _dirtyFlag = false;

            checked
            {
                _fileItemStream = new ZipIOFileItemStream(_blockManager,
                                            this,
                                            position + _localFileHeader.Size,
                                            centralDirFileHeader.CompressedSize);
            }

            // init deflate stream if necessary
            if ((CompressionMethodEnum)_localFileHeader.CompressionMethod == CompressionMethodEnum.Deflated)
            {
                Debug.Assert(_fileItemStream.Position == 0, "CompressStream assumes base stream is at position zero");
                _deflateStream = new CompressStream(_fileItemStream, centralDirFileHeader.UncompressedSize);
                
                _crcCalculatingStream = new ProgressiveCrcCalculatingStream(_blockManager, _deflateStream, Crc32);
            }
            else if ((CompressionMethodEnum)_localFileHeader.CompressionMethod == CompressionMethodEnum.Stored)
            {
                _crcCalculatingStream = new ProgressiveCrcCalculatingStream(_blockManager, _fileItemStream, Crc32);
            }
            else
            {
                throw new NotSupportedException(SR.Get(SRID.ZipNotSupportedCompressionMethod));
            }

            Validate(fileName, centralDir, centralDirFileHeader);
}

        /// <summary>
        /// Validate
        /// </summary>
        /// <param name="fileName">pre-trimmed and normalized filename (see ValidateNormalizeFileName)</param>
        /// <param name="centralDir">central directory block</param>
        /// <param name="centralDirFileHeader">file header from central directory</param>
        private void Validate(string fileName, 
            ZipIOCentralDirectoryBlock centralDir,
            ZipIOCentralDirectoryFileHeader centralDirFileHeader)
        {
            // check that name matches parameter in a case sensitive culture neutral way
            if (0 != String.CompareOrdinal(_localFileHeader.FileName, fileName))
            {
                throw new FileFormatException(SR.Get(SRID.CorruptedData));
            }

            // compare compressed and uncompressed sizes, crc from central directory 
            if ((VersionNeededToExtract != centralDirFileHeader.VersionNeededToExtract) ||
                (GeneralPurposeBitFlag != centralDirFileHeader.GeneralPurposeBitFlag) ||
                (CompressedSize != centralDirFileHeader.CompressedSize) ||
                (UncompressedSize != centralDirFileHeader.UncompressedSize) ||
                (CompressionMethod != centralDirFileHeader.CompressionMethod) ||
                (Crc32 != centralDirFileHeader.Crc32))
            {
                throw new FileFormatException(SR.Get(SRID.CorruptedData));
            }

            // check for read into central directory (which would indicate file corruption)
            if (Offset + Size > centralDir.Offset)
                throw new FileFormatException(SR.Get(SRID.CorruptedData));

            // No CRC check here
            // delay validating the actual CRC until it is possible to do so without additional read operations
            // This is only for non-streaming mode (at this point we only support creation not consumption)
            // This is to avoid the forced reading of entire stream just for CRC check
            // CRC check is delegated  to ProgressiveCrcCalculatingStream and CRC is only validated
            //  once calculated CRC is available. This implies that CRC check operation is not
            //  guaranteed to be performed
        }

        static private void CheckFileAccessParameter(Stream stream, FileAccess access)
        {
            switch(access)
            {
                case FileAccess.Read:
                    if (!stream.CanRead)
                    {
                        throw new ArgumentException (SR.Get(SRID.CanNotReadInWriteOnlyMode));
                    }
                    break;
                case FileAccess.Write:
                    if (!stream.CanWrite)
                    {
                        throw new ArgumentException (SR.Get(SRID.CanNotWriteInReadOnlyMode));
                    }
                    break;                
                case FileAccess.ReadWrite:
                    if (!stream.CanRead || !stream.CanWrite)
                    {
                        throw new ArgumentException (SR.Get(SRID.CanNotReadWriteInReadOnlyWriteOnlyMode));
                    }
                    break;                
                default:
                    throw new ArgumentOutOfRangeException ("access");
            }
        }

        private void RegisterExposedStream(Stream exposedStream)
        {
            if (_exposedPublicStreams == null)
            {
                _exposedPublicStreams = new ArrayList(_initialExposedPublicStreamsCollectionSize);
            }
            _exposedPublicStreams.Add(exposedStream);
        }
        
        private void CloseExposedStreams()
        {
            if (_exposedPublicStreams != null)
            {
                for (int i = _exposedPublicStreams.Count - 1; i >= 0; i--)
                {
                    ZipIOModeEnforcingStream exposedStream = 
                                           (ZipIOModeEnforcingStream)_exposedPublicStreams[i];

                    exposedStream.Close();
                }
            }
        }

        private void FlushExposedStreams()
        {
            //  !!! ATTENTION !!!!        
            // We know for a fact that ZipIoModeEnforcingStream doesn't perform any buffering and is never "dirty"; 
            // therefore, there is no need to flush them. Enumerating and flashing those streams has some non-trivial 
            // perf costs. Instead, it is much cheaper to flush the CrcCalculatingStream. 
            // If at any point we choose to add some buffering to the ZipIoModeEnforcingStream we will have to flush 
            // all of the _exposedPublicStreams in this method.
            _crcCalculatingStream.Flush();


            //  We are going to walk through the exposed streams and if we can not find any stream that isn't Disposed yet 
            // we will switch deflate stream into Start Mode, by doing this we will achieve 2 goals:
            //              1. Relieve Memory Pressure in the Sparse Memory Stream 
            //              2. Close Deflate stream in the write through mode and get the tail bytes (we can only get them if 
            //              we close Deflate stream). This way we can make the disk layout of the File Items that are closed final. 
            if  ((_deflateStream != null) && 
                (!_localFileHeader.StreamingCreationFlag))
            {
                if ((_exposedPublicStreams == null) ||(_exposedPublicStreams.Count == 0))
                {
                    ((CompressStream)_deflateStream).Reset();            
                }
            }
        }

        private const int _initialExposedPublicStreamsCollectionSize= 5;
            
        private ZipIOFileItemStream _fileItemStream;
        private Stream _deflateStream;      // may be null - only used if stream is Deflated
        // _crcCalculatingStream is used to do optimal CRC calcuation when it is possible.
        //  This stream can wrap either _fileItemStream or _deflateStream
        //  For CRC to be calculated correctly, all regualar stream operations have to
        //   go through this stream. This means file item streams we hand out to a client
        //   need to be wrapped as ProgressiveCrcCalculatingStream.
        //  Any other operations specific to ZipIOFileItemStream or DeflateStream should
        //   be directed to those streams.
        private ProgressiveCrcCalculatingStream _crcCalculatingStream;
        private ArrayList _exposedPublicStreams;

        private ZipIOLocalFileHeader _localFileHeader = null;
        private bool _localFileHeaderSaved;         // only used in Streaming mode
        private ZipIOLocalFileDataDescriptor _localFileDataDescriptor = null;

        private ZipIOBlockManager _blockManager;
        
        private long _offset;
        
        // This is a shallow dirtyFlag which doesn't account for the substructures 
        // (ModeEnforcing Streams, compression stream FileItem Stream )
        // GetDirtyFlag method is supposed to be used everywhere where a 
        // complete (deep ;  non-shallow) check for "dirty" is required;
        private bool  _dirtyFlag;

        private bool _disposedFlag = false;        

        private bool _folderFlag = false;        
        private bool _volumeLabelFlag = false;                
    }
}
