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
using System.Collections;
using System.Globalization;
using System.Runtime.Serialization;
using System.Windows;  
using MS.Internal.WindowsBase;

namespace MS.Internal.IO.Zip
{
    /// <summary>
    ///  This is the main clas of the ZIP IO internal APIs. It has 2 stsatic constructors
    /// OpenOnFile and OpenOnStream. It provides client app with ability to manipulate
    /// a Zip Archive (create, open, open/add/delete file items).
    /// </summary>
    internal sealed class ZipArchive : IDisposable
    {
        //------------------------------------------------------
        //
        //  Public Members
        //
        //------------------------------------------------------
        // None 

        //------------------------------------------------------
        //
        //  Internal Constructors
        //
        //------------------------------------------------------
        // None

        //------------------------------------------------------
        //
        // Internal API Methods (although these methods are marked as 
        // Internal, they are part of the internal ZIP IO API surface 
        //
        //------------------------------------------------------
        /// <summary>
        ///  Static constructor File based constructor. all parameteres are obvious. 
        ///  This constructor wil  open the file in requested mode, and it will not do any futher parsing.
        ///
        /// In the case of Create/CreateNew, it will also prebuild in cached in-memory 
        /// EndOfCentralDirectoryRecord (which is a minimal zip file requirement), so that if user 
        /// chooses to close right after he will get a file with just EndOfCentralDirectoryRecord.
        /// </summary>
        internal static ZipArchive OpenOnFile(string path, FileMode mode, FileAccess access, FileShare share, bool streaming)
        {
            if (mode == FileMode.OpenOrCreate || mode == FileMode.Open)
            {
                // for OpenOrCreate cases we need to check whether it is an exisiting file of size 0.
                // Files of size 0 are onsidered invalid ZipArchives. If we skip this check here, later we wouldn't be able to distinguish 
                // between brand new file being created as a result of OpenOrCreate mode, or old 0 length file being open as a result of 
                // OpenOrCreate mode.
                FileInfo fileInfo = new FileInfo(path);
            
                if (fileInfo.Exists && fileInfo.Length == 0 && (fileInfo.Attributes & FileAttributes.ReparsePoint) == 0)
                {
                    throw new FileFormatException(SR.Get(SRID.ZipZeroSizeFileIsNotValidArchive));
                }
            }
            
            ZipArchive archive = null;
            FileStream archiveStream = null;

            // We would like to run initialization after openning stream in try/catch block 
            // so that if anything goes wrong we can close the stream (release file handler) 
            try
            {
                archiveStream = new FileStream(path, mode, access, share, 4096, streaming);
            
                ValidateModeAccessShareStreaming(archiveStream, mode, access, share, streaming);

                archive = new ZipArchive(archiveStream, mode, access, streaming, true);
            }
            catch
            {
                if (archive != null)
                {
                    archive.Close();
                }
                
                if(archiveStream != null)
                {
                    archiveStream.Close();
                }

                throw;
            }
                    
            return archive;
        }

        /// <summary>
        ///  Static constructor Stream based constructor. all parameteres are obvious. 
        ///  This constructor wil not do any futher parsing.
        ///
        /// In the case of Create/CreateNew, it will also prebuild in cached in-memory 
        /// EndOfCentralDirectoryRecord (which is a minimal zip file requirement), so that if user 
        /// chooses to close right after he will get a file with just EndOfCentralDirectoryRecord.
        /// </summary>
        internal static ZipArchive OpenOnStream(Stream stream, FileMode mode, FileAccess access, bool streaming)
        {
            // we can assume FileShare.None, as there is absolutely nothing we can do 
            // about other people working with the underlying storage  
            ValidateModeAccessShareStreaming(stream, mode, access, FileShare.None, streaming);

            if (stream.CanSeek)
            {
                bool emptyStream = (stream.Length == 0);

                switch (mode)
                {
                    // for Open cases we need to check whether it is an existing file of size 0.
                    // Streams of size 0 are considered invalid ZipArchives.
                    case FileMode.Open:
                        if (emptyStream)
                        {
                            throw new FileFormatException(SR.Get(SRID.ZipZeroSizeFileIsNotValidArchive));
                        }
                        break;

                    // for Create cases, we need to check if the stream is empty or not
                    case FileMode.CreateNew:
                        if (!emptyStream)
                        {
                            throw new IOException(SR.Get(SRID.CreateNewOnNonEmptyStream));
                        }
                        break;
                    case FileMode.Create:
                        if (!emptyStream)
                        {
                            // discard existing data
                            stream.SetLength(0);
                        }
                        break;
                }
            }

            return new ZipArchive(stream, mode, access, streaming, false);
        }


        /// <summary>
        ///  This method will result in a complete parsing of the EndOfCentralDirectory 
        /// and CentralDirectory records (if it hasn't been done yet). 
        /// After that (assuming no duplicates were found). It will create in appropriate 
        /// in memory Local FileHeaders and Central Directory Headers.  
        /// </summary>
        internal ZipFileInfo AddFile(string zipFileName, CompressionMethodEnum compressionMethod, DeflateOptionEnum deflateOption)
        {
            CheckDisposed();

            if (_openAccess == FileAccess.Read)
            {
                throw new InvalidOperationException(SR.Get(SRID.CanNotWriteInReadOnlyMode));
            }

            // Validate parameteres 
            zipFileName = ZipIOBlockManager.ValidateNormalizeFileName(zipFileName);

            if ((compressionMethod != CompressionMethodEnum.Stored) && 
                (compressionMethod != CompressionMethodEnum.Deflated))
            {
                throw new ArgumentOutOfRangeException("compressionMethod");            
            }

            // non-contiguous range requires more complex test
            if (deflateOption < DeflateOptionEnum.Normal || (
                deflateOption > DeflateOptionEnum.SuperFast && deflateOption != DeflateOptionEnum.None))
            {
                throw new ArgumentOutOfRangeException("deflateOption");            
            }

            // Check for duplicates , 
            if (FileExists(zipFileName))
            {
                throw new System.InvalidOperationException(SR.Get(SRID.AttemptedToCreateDuplicateFileName));            
            }
                
            // Create Local File Block through Block Manager 
            ZipIOLocalFileBlock fileBlock = _blockManager.CreateLocalFileBlock(zipFileName, compressionMethod, deflateOption);

            //build new ZipFileInfo and add reference to the collection, so we can keep track of the instances of the ZipFileInfo, 
            // that were given out  and invalidate any collection that was returned on GetFiles calls
            ZipFileInfo zipFileInfo = new ZipFileInfo(this, fileBlock);
            ZipFileInfoDictionary.Add(zipFileInfo.Name, zipFileInfo);
            
            return zipFileInfo; 
        }

        /// <summary>
        ///  This method will result in a complete parsing of the EndOfCentralDirectory 
        /// and CentralDirectory records (if it hasn't been done yet). 
        /// After that (assuming the file was found). It will parse the apropriate local file block 
        /// header and data descriptor (if present).
        /// </summary>
        internal ZipFileInfo GetFile(string zipFileName)
        {
            CheckDisposed();        

            if (_openAccess == FileAccess.Write)
            {
                throw new InvalidOperationException(SR.Get(SRID.CanNotReadInWriteOnlyMode));
            }

            // Validate parameteres 
            zipFileName = ZipIOBlockManager.ValidateNormalizeFileName(zipFileName);

            // try to get it from the ZipFileInfo dictionary 
            if (ZipFileInfoDictionary.Contains(zipFileName))
            {
                // this ZipFileInfo was already built through AddFile or GetFile(s)
                // we have this cached 
                return (ZipFileInfo)(ZipFileInfoDictionary[zipFileName]);
            }
            else
            {
                // we need to check whether it is present in the central directory 
                if (!FileExists(zipFileName))
                {
                    throw new InvalidOperationException(SR.Get(SRID.FileDoesNotExists));                
                }

                // Load Local File Block through Block Manager 
                ZipIOLocalFileBlock fileBlock = _blockManager.LoadLocalFileBlock(zipFileName);

                // build new ZipFileInfo and add reference to the collection, so we can keep track of the instances of the ZipFileInfo, 
                // that were given out  and invalidate any collection that was returned on GetFiles calls
                ZipFileInfo zipFileInfo = new ZipFileInfo(this, fileBlock);

                //this should invalidate any outstanding collections                 
                ZipFileInfoDictionary.Add(zipFileInfo.Name, zipFileInfo);

                return zipFileInfo;
            }
        }

        /// <summary>
        ///  This method will result in a complete parsing of the EndOfCentralDirectory 
        /// and CentralDirectory records (if it hasn't been done yet). 
        /// After that it will check whether central directory contains the file. 
        /// It will not attempt the parsing of the local file headers / descriptors. 
        /// </summary>
        internal bool FileExists (string zipFileName)
        {
            CheckDisposed();        

            // Validate parameteres 
            zipFileName = ZipIOBlockManager.ValidateNormalizeFileName(zipFileName);
            
            return _blockManager.CentralDirectoryBlock.FileExists(zipFileName);
        }


        /// <summary>
        ///  This method will result in a complete parsing of the EndOfCentralDirectory 
        /// and CentralDirectory records (if it hasn't been done yet). 
        /// After that it will check whether central directory contains the file. 
        /// If it is present it will parse local fileheader, and remove their in memory 
        /// representation
        /// </summary>
        internal void DeleteFile (string zipFileName)
        {
            CheckDisposed();        

            if (_openAccess == FileAccess.Read)
            {
                throw new InvalidOperationException(SR.Get(SRID.CanNotWriteInReadOnlyMode));
            }

            // Validate parameteres 
            zipFileName = ZipIOBlockManager.ValidateNormalizeFileName(zipFileName);

            if (FileExists(zipFileName)) // is it in central Directory ? 
            {
                ZipFileInfo fileInfoToBeDeleted  = GetFile(zipFileName);

                //this should invalidate any outstanding collections 
                // and update central directory status as appropriate
                ZipFileInfoDictionary.Remove(zipFileName); 

                //this should remove the local file block 
                // from the blockManager's collection 
                _blockManager.RemoveLocalFileBlock(fileInfoToBeDeleted.LocalFileBlock);
            }
        }

        /// <summary>
        ///  This method will result in a complete parsing of the EndOfCentralDirectory 
        /// and CentralDirectory records (if it hasn't been done yet). 
        /// After that it will go through allfiles in the central directory and parse their 
        /// local headers and desciptors one by one. 
        /// </summary>
        internal ZipFileInfoCollection GetFiles()
        {
            CheckDisposed();        

            if (_openAccess == FileAccess.Write)
            {
                throw new InvalidOperationException(SR.Get(SRID.CanNotReadInWriteOnlyMode));
            }

            // We need to scan through the central Directory, and for each file 
            // call GetFile(fileName), which will result in adding missing (not loaded) 
            // information to the ZipFileInfoDictionary.
            foreach(string fileName in _blockManager.CentralDirectoryBlock.GetFileNamesCollection())
            {
                GetFile(fileName);  // fileName must be validated and normalized at this 
                                            // point by the central directory parsing routine 
            }
            return new ZipFileInfoCollection(ZipFileInfoDictionary.Values);
        }
        
        /// <summary>
        ///  This method will result in a complete Flushing of any outstanding data in buffers and 
        /// any streams ever returned by the GetStream calls.This call results in Archive file that 
        /// has a completely valid state. If application were to crash right afte the Flush is complete, 
        /// the resulting files would be a "valid" Zip archive  
        /// </summary>
        internal void Flush()
        {
            CheckDisposed();        
            _blockManager.Save(false);
        }

        /// <summary>
        /// Results in a complete Flush of all the outstanding buffers and closing/disposing all of the objects
        /// ZipFileInfo, Streams ZipArchive.
        /// </summary>        
        internal void Close()
        {
            Dispose();
        }

        /// <summary>
        /// Results in a complete Flush of all the outstanding buffers and closing/disposing all of the objects
        /// ZipFileInfo, Streams ZipArchive.
        /// </summary>                
        public void Dispose()
        {
            Dispose(true);
            // GC.SuppressFinalize(this);  // Because this class is sealed and there is no Finalizer, 
                                                        // there is no need for this call. Leaving it in case we decide to unseal it  
        }

        /// <summary>
        /// Throw if version needed to extract is not supported
        /// </summary>
        /// <param name="version">version to inspect</param>
        static internal void VerifyVersionNeededToExtract(UInt16 version)
        {
            // strictly enforce this list
            switch (version)
            {
                case (UInt16)ZipIOVersionNeededToExtract.StoredData: break;
                case (UInt16)ZipIOVersionNeededToExtract.VolumeLabel: break;
                case (UInt16)ZipIOVersionNeededToExtract.DeflatedData: break;
                case (UInt16)ZipIOVersionNeededToExtract.Zip64FileFormat: break;
                default:
                    throw new NotSupportedException(SR.Get(SRID.NotSupportedVersionNeededToExtract)); 
            }
        }

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------
        /// <summary>
        /// Read only property, that returns the value of FileAccess that 
        /// was passed into the OpenOnFile or OpenOnStream call  
        /// </summary>                
        internal FileAccess OpenAccess
        {
            get
            {
                CheckDisposed();              
                return _openAccess;
            }
        }

//  This functionality commented out in order to comply with FX Cop rule 
//  AvoidUncalledPrivateCode. However, because of a chance that this functionality 
//  might eventually get public exposure we would like to keep this code around
#if ZIP_IO_PUBLIC

        /// <summary>
        /// Returns Comment field from the End Of Central Directory Record. 
        /// Therefore, call to this property might result in some parsing. 
        /// If the End Of Central Directory isn't parsed yet, it will be as a 
        /// result of querying this property.  
        /// </summary>                
        internal string Comment
        {
            get
            {
                CheckDisposed();  
                return _blockManager.EndOfCentralDirectoryBlock.Comment;
            }
        }
#endif


        //------------------------------------------------------
        //
        // Internal NON API Methods (these methods are marked as 
        // Internal, and they are trully internal and not the part of the 
        // internal ZIP IO API surface 
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Private Constructors
        //
        //------------------------------------------------------
        /// <summary>
        /// This private constructor isonly supposed to be called by the 
        /// OpenOnFile and OpenOnStream static members. 
        /// </summary>         
        /// <param name="archiveStream"></param>
        /// <param name="mode"></param>
        /// <param name="access"></param>
        /// <param name="streaming"></param>
        /// <param name="ownStream">true if this class is responsible for closing the archiveStream</param>
        private ZipArchive(Stream archiveStream, FileMode mode, FileAccess access, bool streaming, bool ownStream)
        {
            // as this contructor is only called from the static member 
            // all checks should have been done before 

            _blockManager = new ZipIOBlockManager(archiveStream, streaming, ownStream);

            _openMode = mode;
            _openAccess = access;

            // In case of "create" we also need to create at least an end of Central Directory Record.
            // For FileMode OpenOrCreate we use stream Length to distinguish open and create scenarios.
            // Implications of this decision is that existing file of size 0 opened in OpenOrCreate Mode 
            // will be treated as a newly/created file.
            if ((_openMode == FileMode.CreateNew) ||
               (_openMode == FileMode.Create) ||
               ((_openMode == FileMode.OpenOrCreate) && archiveStream.Length == 0))
            {
                _blockManager.CreateEndOfCentralDirectoryBlock();
            }
            else
            {
                _blockManager.LoadEndOfCentralDirectoryBlock();
            }
        }

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Dispose(bool)
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_disposedFlag)
                {
                    try
                    {
                        if (_blockManager != null)
                        {
                            // allow this in Debug mode to catch any place where we accidentally
                            // make something dirty when we are read-only
#if !DEBUG
                            if (_openAccess == FileAccess.ReadWrite || _openAccess == FileAccess.Write)
#endif
                            {
                                _blockManager.Save(true);
                            }

                            ((IDisposable)_blockManager).Dispose();
                        }
                    }
                    finally
                    {
                        _disposedFlag = true;
                    }
                }
            }
        }

        /// <summary>
        /// This is function is called by the OpenOnFile and OpenOnStream in order
        /// to validate parameteresgiven to those functions. The combinations of valid
        /// parameters is quite complex and not obvious, so after basic range checks, 
        /// it is actually using a lookup table to answer the question whether the given 
        /// parameter  combination is valid or not.
        /// </summary>   
        static private void ValidateModeAccessShareStreaming(Stream stream, FileMode mode, FileAccess access, FileShare share, bool streaming)
        {
            if (stream == null)        
            {
                throw new ArgumentNullException("stream");
            }

            ////////////
            // filter out values that are out of enum ranges first 
            ////////////
            ValidateModeAccessShareValidEnums(mode, access, share);

            ////////////            
            // filter out values that are not supported regardless of other parameters 
            // but still validate enum members 
            ////////////            
            ValidateModeAccessShareSupportedEnums(mode, share);

            ////////////
            // let's makes sure that given stream is capable of supporting required functionality 
            ////////////            
            ValidateModeAccessStreamStreamingCombinations(stream, access, streaming);        

            ////////////
            //let's make sure that comnbintaion of mode, access,share,,streaming 
            // parameters is supported 
            ////////////            
            int intMode =  Convert.ToInt32(mode, CultureInfo.InvariantCulture);
            int intAccess = Convert.ToInt32(access, CultureInfo.InvariantCulture);
            int intShare = Convert.ToInt32(share, CultureInfo.InvariantCulture);
            int intStreaming = Convert.ToInt32(streaming, CultureInfo.InvariantCulture);
            for(int i=0; i<_validOpenParameters.GetLength(0); i++)
            {
                if ((_validOpenParameters [i,0] == intMode) &&
                    (_validOpenParameters [i,1] == intAccess) &&
                    (_validOpenParameters [i,2] == intShare) &&
                    (_validOpenParameters [i,3] == intStreaming))
                {
                    return;
                }
            }

            throw new ArgumentException(SR.Get(SRID.UnsupportedCombinationOfModeAccessShareStreaming));
        }

        static private void ValidateModeAccessStreamStreamingCombinations(Stream stream, FileAccess access, bool streaming)
        {
            ////////////
            // let's makes sure that given stream is capable of supporting required functionality 
            ////////////            
            if ((access== FileAccess.Read || access == FileAccess.ReadWrite) && !stream.CanRead)
            {
                throw new ArgumentException(SR.Get(SRID.CanNotReadDataFromStreamWhichDoesNotSupportReading));
            }

            // if user want to be able to write stream needs to support it 
            if ((access == FileAccess.Write || access == FileAccess.ReadWrite) && !stream.CanWrite)
            {
                throw new ArgumentException(SR.Get(SRID.CanNotWriteDataToStreamWhichDoesNotSupportWriting));
            }            

            // if user works in non-streaming mode we need to Seek on underlying stream 
            if (! streaming && !stream.CanSeek)
            {
                throw new ArgumentException(SR.Get(SRID.CanNotOperateOnStreamWhichDoesNotSupportSeeking));
            }            
        }
        
        static private void ValidateModeAccessShareSupportedEnums(FileMode mode, FileShare share)
        {
            ////////////            
            // filter out values that are not supported regardless of other parameters 
            // but still validate enum members 
            ////////////            
             if (mode == FileMode.Append || mode == FileMode.Truncate)
            {
                throw new NotSupportedException(SR.Get(SRID.TruncateAppendModesNotSupported));
            }
            else if (share != FileShare.Read && share != FileShare.None)
            {
                // later as we get to streaming other FileShare values will be supported too 
                throw new NotSupportedException (SR.Get(SRID.OnlyFileShareReadAndFileShareNoneSupported));   
            }
        }
        
        static private void ValidateModeAccessShareValidEnums(FileMode mode, FileAccess access, FileShare share)
        {
            ////////////
            // filter out values that are out of enum ranges first 
            ////////////
            if ((mode != FileMode.Append) && (mode != FileMode.Create) && (mode != FileMode.CreateNew) && (mode != FileMode.Open)
                && (mode != FileMode.OpenOrCreate) && (mode != FileMode.Truncate))
            {
                throw new ArgumentOutOfRangeException("mode");            
            }
            else if ((access != FileAccess.Read) && (access != FileAccess.ReadWrite) && (access != FileAccess.Write))
            {
                throw new ArgumentOutOfRangeException("access");            
            }
            else if ((share != FileShare.Delete) && (share != FileShare.Inheritable) && (share != FileShare.None) &&
                (share != FileShare.Read) && (share != FileShare.ReadWrite) && (share != FileShare.Write))
            {
                throw new ArgumentOutOfRangeException("share");            
            }
        }
        
        /// <summary>
        /// Throws exception if object already Disposed/Closed. 
        /// </summary> 
        private void CheckDisposed()
        {
            if (_disposedFlag)
            {
                throw new ObjectDisposedException(null, SR.Get(SRID.ZipArchiveDisposed));
            }
        }

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------
        /// <summary>
        /// This private property is used as a mean to achieve lazy memory allocation for the 
        /// hashtable that maintains a cahce of the returned instrances of ZipFileInfo(s).
        /// This hashtable uses file names as keys in the case insensitive and culture invariant fashion 
        /// </summary>   
        private IDictionary ZipFileInfoDictionary 
        {
            get
            {
                if (_zipFileInfoDictionary == null)
                {
                                // ordinal case sensitive comparison 
                    _zipFileInfoDictionary = new Hashtable(_zipFileInfoDictionaryInitialSize, StringComparer.Ordinal); 
                }
                return _zipFileInfoDictionary;
            }
        }
        
        //------------------------------------------------------
        //
        //  Private Fields 
        //
        //------------------------------------------------------        

        // This 2 dimensional table is used by the ValidateModeAccessShareStreaming 
        // function as a set of valid parameter combinations 
        static private int[,] _validOpenParameters = new int[,]
        {   // FileMode                             // FileAccess                     / FileShare                     // streaming 
            {(int)FileMode.Create,             (int)FileAccess.Write,        (int)FileShare.None,        1},
            {(int)FileMode.Create,             (int)FileAccess.Write,        (int)FileShare.Read,        1},
            {(int)FileMode.Create,             (int)FileAccess.ReadWrite, (int)FileShare.None,        0},
            {(int)FileMode.CreateNew,       (int)FileAccess.Write,        (int)FileShare.None,        1},
            {(int)FileMode.CreateNew,       (int)FileAccess.Write,        (int)FileShare.Read,        1},
            {(int)FileMode.CreateNew,       (int)FileAccess.ReadWrite, (int)FileShare.None,        0},
            {(int)FileMode.Open,               (int)FileAccess.Read,         (int)FileShare.None,        1},
            {(int)FileMode.Open,               (int)FileAccess.Read,         (int)FileShare.None,        0},
            {(int)FileMode.Open,               (int)FileAccess.Read,         (int)FileShare.Read,        1},
            {(int)FileMode.Open,               (int)FileAccess.Read,         (int)FileShare.Read,        0},
            {(int)FileMode.Open,               (int)FileAccess.Read,         (int)FileShare.Write,        1},
            {(int)FileMode.Open,               (int)FileAccess.Read,         (int)FileShare.ReadWrite, 1},
            {(int)FileMode.Open,               (int)FileAccess.ReadWrite,  (int)FileShare.None,        0},
            {(int)FileMode.OpenOrCreate,  (int)FileAccess.ReadWrite,  (int)FileShare.None,        0}
        };

        // modes that were used for openning (OpenOnStream or OpenOnFile),
        // there is no way to change these values after class is constructed 
        private FileMode _openMode; 
        private FileAccess  _openAccess;

        private bool _disposedFlag;

        // reference to the ZipIOBlockManager, this reference is instantiated as 
        // a part of the OpenOnFile/OpenOnStream contruction 
        private ZipIOBlockManager _blockManager;

        // this is a Dictionary of all the ZipFileInfos that were given out.
        // It uses file name as key in case insensitive and culture invariant fashion.
        // all members of the class are supposed to use this field indirectly through 
        // ZipFileInfoDictionary property, as ZipFileInfoDictionary is respnsible 
        // for lazy allocation of the hashtable.
        private IDictionary _zipFileInfoDictionary;
        private const int _zipFileInfoDictionaryInitialSize = 50;  
    }
}
