// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  Class for manipulating streams in the container file
//
//
//
//
//
//
//
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics; // For Debug.Assert
using System.IO;
using System.Runtime.InteropServices;
using System.Security;

using System.Windows;                 //  SR.[exception message]
using MS.Internal.IO.Packaging.CompoundFile;
using CU = MS.Internal.IO.Packaging.CompoundFile.ContainerUtilities;

using System.IO.Packaging;
using MS.Internal.WindowsBase;

namespace System.IO.Packaging
{
    /// <summary>
    /// Core information for a StreamInfo object.
    /// </summary>
    internal class StreamInfoCore
    {
        internal StreamInfoCore( 
            string nameStream, 
            string label ) : this( nameStream, label, null ) {;}

        internal StreamInfoCore( 
            string nameStream, 
            string label, 
            IStream s )
        {
            streamName = nameStream;
            dataSpaceLabel = label;
            safeIStream = s;
            exposedStream = null;
        }

        /// <summary>
        /// The compound-file friendly version of streamName.
        /// </summary>
        internal string      streamName;

        /// <summary>
        /// A cached reference to the stream object for accessing the data  This
        /// may be null if we haven't had need to open the stream.
        /// </summary>
        internal IStream safeIStream;

        /// <summary>
        /// The label for the data space definition that is associated with this
        /// stream.  This can only be set at the time of StreamInfo.Create().  A
        /// null string indicates that we are not in a data space.
        /// </summary>
        internal string dataSpaceLabel;

        /// <summary>
        /// This represents visible stream object.  When the stream represented by this StreamInfo is supposed
        /// to go away, this will be reset to null.
        /// </summary>
        internal object exposedStream;
    }
    
    /// <summary>
    /// Class for manipulating streams in the container file
    /// </summary>
    public class StreamInfo
    {
        /***********************************************************************/
        // Default values to use for shortcuts
        const FileMode   defaultFileOpenMode   = FileMode.OpenOrCreate;
        const FileMode   defaultFileCreateMode = FileMode.Create;
        const string     defaultDataSpace      = null; // Programmatic change-able?

        /***********************************************************************/
        // Instance values

        /// <summary>
        /// A reference back to the parent storage object
        /// </summary>
        StorageInfo parentStorage;

        /// <summary>
        /// Reference to a class that contains our core information.  This is
        /// maintained by our parent storage.
        /// </summary>
        StreamInfoCore core;

        /// <summary>
        /// CompoundFileStreamReference for this StreamInfo object
        /// </summary>
        CompoundFileStreamReference _streamReference;

        /// <summary>
        /// We need to rememeber the FileAccess that was used for openning 
        /// in order to provide correct information, we can not used underlying structures,  
        /// as the same stream can be subsequently opened in different modes  
        /// </summary>
        private FileAccess openFileAccess;

        private CompressionOption _compressionOption;
        private EncryptionOption   _encryptionOption;
        private bool _needToGetTransformInfo = true;

        /***********************************************************************/
        // Constructors

        private void BuildStreamInfoRelativeToStorage( StorageInfo parent, string path )
        {
            parentStorage = parent;
            core = parentStorage.CoreForChildStream( path );
        }

        /// <summary>
        /// Creates a new instance relative to the root
        /// </summary>
        /// <param name="root">The root storage</param>
        /// <param name="streamPath">Path to stream under root storage</param>
        private StreamInfo( StorageRoot root, string streamPath ) : this((StorageInfo)root, streamPath)
        {
        }

        /// <summary>
        /// Creates a new instance relative to the given parent
        /// </summary>
        /// <param name="parent">The parent storage</param>
        /// <param name="streamName">Path to stream under parent storage</param>
        internal StreamInfo( StorageInfo parent, string streamName ) : this (parent, streamName, CompressionOption.NotCompressed, EncryptionOption.None)
        {
        }

 
        /// <summary>
        /// Creates a new instance relative to the given parent
        /// </summary>
        /// <param name="parent">The parent storage</param>
        /// <param name="streamName">Path to stream under parent storage</param>
        /// <param name="compressionOption">CompressionOption</param>
        /// <param name="encryptionOption">EncryptionOption</param>
        internal StreamInfo( StorageInfo parent, string streamName, CompressionOption compressionOption, 
            EncryptionOption encryptionOption )
        {
             // Parameter validation
            CU.CheckAgainstNull( parent, "parent" );
            CU.CheckStringAgainstNullAndEmpty( streamName, "streamName" );

            // Parse path relative to given parent.
            BuildStreamInfoRelativeToStorage( parent, 
                streamName);
            
             _compressionOption = compressionOption;
            _encryptionOption   = encryptionOption;
            _streamReference = new CompoundFileStreamReference(this.parentStorage.FullNameInternal, this.core.streamName);
        }

        /***********************************************************************/
        // Properties

        /// <summary>
        /// The CompressionOption on the stream
        /// </summary>
        public CompressionOption CompressionOption
        {
            get
            {
                if( StreamInfoDisposed ) // Null name in core signifies the core object is disposed
                {
                    // The .Net Design Guidelines instruct us not to throw exceptions in property getters.
                    return CompressionOption.NotCompressed;
                }

                EnsureTransformInformation();

                return _compressionOption;
            }
        }

        /// <summary>
        /// The EncryptionOption on the stream
        /// </summary>
        public EncryptionOption EncryptionOption
        {
            get
            {
                if( StreamInfoDisposed ) // Null name in core signifies the core object is disposed
                {
                    // The .Net Design Guidelines instruct us not to throw exceptions in property getters.
                    return EncryptionOption.None;
                }

                EnsureTransformInformation();

                return _encryptionOption;
            }
        }

        /// <summary>
        /// The name of this stream
        /// </summary>
        public string Name
        {
            get
            {
                if( StreamInfoDisposed ) // Null name in core signifies the core object is disposed
                {
                    // The .Net Design Guidelines instruct us not to throw exceptions in property getters.
                    return "";
                }

                return core.streamName;
            }
        }

        /***********************************************************************/
        // Methods

        /// <summary>
        /// Opens a stream
        /// </summary>
        /// <returns>Stream object to manipulate data</returns>
        public Stream GetStream()
        {
            return GetStream( defaultFileOpenMode, parentStorage.Root.OpenAccess );
        }

        /// <summary>
        /// Opens a stream with the given open mode flags
        /// </summary>
        /// <param name="mode">Open mode flags</param>
        /// <returns>Stream object to manipulate data</returns>
        public Stream GetStream( FileMode mode )
        {
            return GetStream( mode, parentStorage.Root.OpenAccess );
        }

        /// <summary>
        /// Opens a stream with the given open mode flags and access flags
        /// </summary>
        /// <param name="mode">Open mode flags</param>
        /// <param name="access">File access flags</param>
        /// <returns>Stream object to manipulate data</returns>
        public Stream GetStream( FileMode mode, FileAccess access )
        {
            CheckDisposedStatus();
        
            int grfMode = 0;
            IStream openedIStream = null;

            openFileAccess = access;
            // becasue of the stream caching mechanism we must adjust FileAccess parameter. 
            // We want to open stream with the widest access posible, in case Package was open in ReadWrite 
            // we need to open stream in ReadWrite even if user explicitly asked us to do ReadOnly/WriteOnly. 
            // There is a possibility of a next request coming in as as ReadWrite request, and we would like to
            // take advanatage of the cached stream by wrapping with appropriate access limitations.
            if (parentStorage.Root.OpenAccess == FileAccess.ReadWrite)
            {
                // Generate the access flags from the access parameter
                access = FileAccess.ReadWrite;
            }

            // Generate the access flags from the access parameter
            SafeNativeCompoundFileMethods.UpdateModeFlagFromFileAccess( access, ref grfMode );

            // Only SHARE_EXCLUSIVE for now, FileShare issue TBD
            grfMode |= SafeNativeCompoundFileConstants.STGM_SHARE_EXCLUSIVE;

            CheckAccessMode(grfMode);

            // Act based on FileMode
            switch(mode)
            {
                case FileMode.Append:
                    throw new ArgumentException(
                        SR.FileModeUnsupported);
                case FileMode.Create:
                    // Check to make sure root container is not read-only, and that
                    //  we're not pointlessly trying to create a read-only stream.
                    CreateTimeReadOnlyCheck(openFileAccess);
                    
                    // Close down any existing streams floating out there
                    if (null != core.exposedStream)
                    {
                        ((Stream)(core.exposedStream)).Close();
                    }
                    core.exposedStream = null;
                    
                    if( null != core.safeIStream )
                    {
                        // Close out existing stream
                        ((IDisposable) core.safeIStream).Dispose();
                        core.safeIStream = null;
                    }

                    // Cleanup done, create new stream in its place
                    grfMode |= SafeNativeCompoundFileConstants.STGM_CREATE;
                    openedIStream = CreateStreamOnParentIStorage(
                            core.streamName, 
                        grfMode );
                    break;
                case FileMode.CreateNew:
                    throw new ArgumentException(
                        SR.FileModeUnsupported);
                case FileMode.Open:
                    // If we've got a stream, return a CFStream built from its clone
                    if( null != core.safeIStream )
                    {
                        return CFStreamOfClone(openFileAccess);
                    }

                    // Need to call Open API with NULL open flags
                    openedIStream = OpenStreamOnParentIStorage(
                            core.streamName, 
                        grfMode );
                    break;
                case FileMode.OpenOrCreate:
                    // If we've got a stream, return a CFStream built from its clone
                    if( null != core.safeIStream )
                    {
                        return CFStreamOfClone(openFileAccess);
                    }

                    // Skip creation attempt for read-only container or specifying
                    //  read-only stream
                    if( FileAccess.Read != parentStorage.Root.OpenAccess &&
                        FileAccess.Read != openFileAccess )
                    {
                        // Try creating first.  If it already exists then do an open.  This
                        //  seems ugly but this method involves the fewest number of 
                        //  managed/unmanaged transitions.

                        if( !parentStorage.Exists )
                        {
                            parentStorage.Create();
                        }
                        int nativeCallErrorCode = 
                            parentStorage.SafeIStorage.CreateStream(
                                core.streamName, 
                            grfMode,
                            0,
                            0,
                            out openedIStream );

                        if( SafeNativeCompoundFileConstants.S_OK != nativeCallErrorCode &&
                            SafeNativeCompoundFileConstants.STG_E_FILEALREADYEXISTS != nativeCallErrorCode )
                        {
                            throw new IOException(
                                SR.UnableToCreateStream,
                                new COMException( 
                                    SR.Format(SR.NamedAPIFailure, "IStorage.CreateStream"),
                                    nativeCallErrorCode ));
                        }
                        
                        // Parent storage has changed - invalidate all standing enuemrators
                        parentStorage.InvalidateEnumerators();

                        // else - proceed with open
                    }

                    if( null == openedIStream )
                    {
                        // If we make it here, it means the create stream call failed
                        //  because of a STG_E_FILEALREADYEXISTS 
                        //  or container is read-only
                        openedIStream = OpenStreamOnParentIStorage(
                                core.streamName, 
                            grfMode );
                    }
                    break;
                case FileMode.Truncate:
                    throw new ArgumentException(
                        SR.FileModeUnsupported);
                default:
                    throw new ArgumentException(
                        SR.FileModeInvalid);
            }

            core.safeIStream = openedIStream;

            Stream returnStream = 
                BuildStreamOnUnderlyingIStream( core.safeIStream, openFileAccess, this );

            core.exposedStream = returnStream;

            return returnStream;
        }

        /***********************************************************************/
        // Internal/Private functionality

        /// <summary>
        /// Creates a stream with all default parameters
        /// </summary>
        /// <returns>Stream object to manipulate data</returns>
        internal Stream Create()
        {
            return Create( defaultFileCreateMode, parentStorage.Root.OpenAccess, defaultDataSpace );
        }

        /// <summary>
        /// Creates a stream with the given create mode
        /// </summary>
        /// <param name="mode">Desired create mode</param>
        /// <returns>Stream object to manipulate data</returns>
        private Stream Create( FileMode mode )
        {
            return Create( mode, parentStorage.Root.OpenAccess, defaultDataSpace );
        }

        /// <summary>
        /// Creates a stream encoded in the given data space
        /// </summary>
        /// <param name="dataSpaceLabel">Data space label</param>
        /// <returns>Stream object to manipulate data</returns>
        internal Stream Create( string dataSpaceLabel )
        {
            return Create( defaultFileCreateMode, parentStorage.Root.OpenAccess, dataSpaceLabel );
        }

        /// <summary>
        /// Creates a stream with the given create and access flags
        /// </summary>
        /// <param name="mode">Desired create mode flag</param>
        /// <param name="access">Access flags</param>
        /// <returns>Stream object to manipulate data</returns>
        private Stream Create( FileMode mode, FileAccess access )
        {
            return Create( mode, access, defaultDataSpace );
        }

        /// <summary>
        /// Creates a stream with the given parameters
        /// </summary>
        /// <param name="mode">Creation mode</param>
        /// <param name="access">Access mode</param>
        /// <param name="dataSpace">Data space encoding</param>
        /// <returns>Stream object to manipulate data</returns>
        internal Stream Create( FileMode mode, FileAccess access, string dataSpace )
        {
            CheckDisposedStatus();
        
            int grfMode = 0;
            IStream createdSafeIStream = null;
            DataSpaceManager dataSpaceManager = null;

            // Check to make sure root container is not read-only, and that
            //  we're not pointlessly trying to create a read-only stream.
            CreateTimeReadOnlyCheck( access );

            // Check to see if the data space label is valid
            if( null != dataSpace )
            {
                if( 0 == dataSpace.Length )
                    throw new ArgumentException(
                        SR.DataSpaceLabelInvalidEmpty);
            
                dataSpaceManager = parentStorage.Root.GetDataSpaceManager();
                if( !dataSpaceManager.DataSpaceIsDefined( dataSpace ) )
                    throw new ArgumentException(
                        SR.DataSpaceLabelUndefined);
            }

            openFileAccess = access;
            // becasue of the stream caching mechanism we must adjust FileAccess parameter. 
            // We want to open stream with the widest access posible, in case Package was open in ReadWrite 
            // we need to open stream in ReadWrite even if user explicitly asked us to do ReadOnly/WriteOnly. 
            // There is a possibility of a next request coming in as as ReadWrite request, and we would like to
            // take advanatage of the cached stream by wrapping with appropriate access limitations.
            if (parentStorage.Root.OpenAccess == FileAccess.ReadWrite)
            {
                access = FileAccess.ReadWrite;
            }

            // Generate the access flags from the access parameter
            SafeNativeCompoundFileMethods.UpdateModeFlagFromFileAccess( access, ref grfMode );
            
            // Only SHARE_EXCLUSIVE for now, FileShare issue TBD
            grfMode |= SafeNativeCompoundFileConstants.STGM_SHARE_EXCLUSIVE;

            CheckAccessMode(grfMode);

            // Act based on FileMode
            switch(mode)
            {
                case FileMode.Create:
                    // Close down any existing streams floating out there
                    if (null != core.exposedStream)
                    {
                        ((Stream)(core.exposedStream)).Close();
                    }
                    core.exposedStream = null;

                    if( null != core.safeIStream )
                    {
                        // Release reference
                        ((IDisposable) core.safeIStream).Dispose();
                        core.safeIStream = null;
                    }

                    // Cleanup done, create new stream in its place.
                    grfMode |= SafeNativeCompoundFileConstants.STGM_CREATE;
                    createdSafeIStream = CreateStreamOnParentIStorage(
                            core.streamName, 
                        grfMode );    
                    break;
                case FileMode.CreateNew:
                    // If we've created a CFStream, this fails because stream is already there.
                    if( null != core.safeIStream )
                        throw new IOException(
                            SR.StreamAlreadyExist);

                    // Need to call Create API with NULL create flags
                    createdSafeIStream = CreateStreamOnParentIStorage(
                            core.streamName, 
                        grfMode );
                    break;
                case FileMode.Append:   // None of these are valid in a Create
                case FileMode.Open:
                case FileMode.OpenOrCreate:
                case FileMode.Truncate:
                default:
                    throw new ArgumentException(
                        SR.FileModeInvalid);
            }

            core.safeIStream = createdSafeIStream;
            // At this point we passed all previous checks and got the underlying IStream.
            //  Set our data space label to the given label, and the stream to the retrieved stream.
            core.dataSpaceLabel = dataSpace;
            if( null != dataSpace )
            {
                dataSpaceManager.CreateDataSpaceMapping( 
                    new CompoundFileStreamReference( parentStorage.FullNameInternal, core.streamName ), 
                    core.dataSpaceLabel );
            }

            Stream returnStream = 
                BuildStreamOnUnderlyingIStream( core.safeIStream, openFileAccess, this );

            _needToGetTransformInfo = false;    // We created stream with the given dataspace setting
                                                //  so, there is no need to get the dataspace setting
            core.exposedStream = returnStream;

            return returnStream;
        }

        Stream BuildStreamOnUnderlyingIStream( 
            IStream underlyingIStream,
            FileAccess access, 
            StreamInfo parent )
        {
            Stream rawStream = new CFStream( underlyingIStream, access, parent );

            if( null == core.dataSpaceLabel )
            {
                // The stream is not transformed in any data space, add buffering and return
                return new BufferedStream( rawStream );
            }
            else
            {
                // Pass raw stream to data space manager to get real stream
               return parentStorage.Root.GetDataSpaceManager().CreateDataSpaceStream(
                    StreamReference, rawStream);
            }
        }

        /// <summary>
        /// A check against FileAccess.Read at create time.  It should fail if
        ///  the root container is read-only, or if we're pointlessly trying
        ///  to create a read-only stream.
        /// </summary>
        void CreateTimeReadOnlyCheck( FileAccess access )
        {
            // Can't create a stream if the root container is read-only
            if( FileAccess.Read == parentStorage.Root.OpenAccess )
                throw new IOException(
                    SR.CanNotCreateInReadOnly);

            // Doesn't make sense to create a new stream just to make it read-only
            if( access == FileAccess.Read )
                throw new ArgumentException(
                    SR.CanNotCreateAsReadOnly);
        }
        
        /// <summary>
        /// Shortcut macro - calls the IStorage::CreateStream method on the parent
        /// storage object.
        /// </summary>
        IStream CreateStreamOnParentIStorage(
            string name, 
            int mode )
        {
            IStream createdStream = null;
            int nativeCallErrorCode = 0;

            if( !parentStorage.Exists )
            {
                parentStorage.Create();
            }

            nativeCallErrorCode = parentStorage.SafeIStorage.CreateStream(
                name,
                mode,
                0,
                0,
                out createdStream );

            if( SafeNativeCompoundFileConstants.STG_E_INVALIDFLAG == nativeCallErrorCode )
            {
                throw new ArgumentException(
                    SR.StorageFlagsUnsupported);
            }
            else if ( SafeNativeCompoundFileConstants.S_OK != nativeCallErrorCode )
            {
                throw new IOException(
                    SR.UnableToCreateStream,
                    new COMException( 
                        SR.Format(SR.NamedAPIFailure, "IStorage.CreateStream"),
                        nativeCallErrorCode ));
            }

            // Parent storage has changed - invalidate all standing enuemrators
            parentStorage.InvalidateEnumerators();
        
            return createdStream;
        }

        /// <summary>
        /// Shortcut macro - calls the IStorage::OpenStream method on the parent
        /// storage object.
        /// </summary>
        IStream OpenStreamOnParentIStorage(
            string name, 
            int mode )
        {
            IStream openedStream = null;
            int nativeCallErrorCode = 0;

            nativeCallErrorCode = parentStorage.SafeIStorage.OpenStream(
                name,
                0,
                mode,
                0,
                out openedStream );

            if( SafeNativeCompoundFileConstants.S_OK != nativeCallErrorCode )
            {
                throw new IOException(
                    SR.UnableToOpenStream,
                    new COMException( 
                        SR.Format(SR.NamedAPIFailure, "IStorage.OpenStream"),
                        nativeCallErrorCode ));
            }
            return openedStream;
        }

        /// <summary>
        /// Deletes the stream specified by this StreamInfo
        /// </summary>
        internal void Delete()
        {
            CheckDisposedStatus();
        
            if( InternalExists() )
            {
                if( null != core.safeIStream )
                {
                    // Close out existing stream
                    ((IDisposable) core.safeIStream).Dispose();
                    core.safeIStream = null;
                }
                parentStorage.DestroyElement( core.streamName );

                // Parent storage has changed - invalidate all standing enuemrators
                parentStorage.InvalidateEnumerators();
            }
            else
            {
                // If a FileInfo is told to delete a file that does not
                //  exist, nothing happens.  We follow that example here.
            }
        }

        /// <summary>
        /// It is valid to have a StreamInfo class that points to a stream
        /// that does not (yet) exist.  However, it is impossible to perform
        /// operations on a stream that does not exst, so the methods that
        /// require an existing stream need to be able to check if the stream
        /// exists before trying to perform its operations.
        /// </summary>
        /// <returns>Whether "this" stream exists</returns>
        internal bool InternalExists()
        {
            // If we have a stream, it's pretty obvious that we exist.
            if( null != core.safeIStream ) 
                return true;
                
            // If parent storage does not exist, we can't possibly exist either
            if( !parentStorage.Exists )
                return false;

            // At this point we know the parent storage exists, but we don't know
            //  if we do.  Try to open the stream.
            return SafeNativeCompoundFileConstants.S_OK == parentStorage.SafeIStorage.OpenStream(
                core.streamName,
                0,
                SafeNativeCompoundFileConstants.STGM_READ | SafeNativeCompoundFileConstants.STGM_SHARE_EXCLUSIVE,
                0,
                out core.safeIStream );
        }

        /// <summary>
        /// Most of the time internal methods that want to do an internal check 
        /// to see if a stream exists is only interested in proceeding if it does.
        /// If it doesn't, abort with an exception.  This implements the little
        /// shortcut.
        /// </summary>
        void VerifyExists()
        {
            if( !InternalExists() )
            {
                throw new IOException(
                    SR.StreamNotExist);
            }
            return;
        }

        private Stream CFStreamOfClone( FileAccess access )
        {
            long dummy = 0;

            IStream cloneStream = null;
            core.safeIStream.Clone( out cloneStream );
            cloneStream.Seek( 0, SafeNativeCompoundFileConstants.STREAM_SEEK_SET, out dummy );

            Stream returnStream = 
                BuildStreamOnUnderlyingIStream( cloneStream, access, this );

            core.exposedStream = returnStream;

            return returnStream;
        }

        // Check whether this StreamInfo object is still valid.  If not, thrown an
        //  ObjectDisposedException.
        internal void CheckDisposedStatus()
        {
            // Check to see if we're still valid.
            if( StreamInfoDisposed ) // Null name in core signifies the core object is disposed
                throw new ObjectDisposedException(null, SR.StreamInfoDisposed);
        }

        // Check whether this StreamInfo object is still valid.  Return result.
        internal bool StreamInfoDisposed
        {
            get
            {
                // Check to see if we're still valid.
                // Null name in core signifies the core object is disposed.
                // Also check the parent storage.
                return (( null == core.streamName ) || parentStorage.StorageDisposed);
            }
        }


        // If we opened the IStream but haven't publicly exposed any Streams yet (i.e. InternalExists),
        // check to make sure the access modes match.
        internal void CheckAccessMode(int grfMode)
        {
            // Do we have an IStream?
            if( null != core.safeIStream )
            {
                // Have we exposed it publicly yet?
                if( null == core.exposedStream )
                {
                    System.Runtime.InteropServices.ComTypes.STATSTG mySTATs;
                    core.safeIStream.Stat( out mySTATs, SafeNativeCompoundFileConstants.STATFLAG_NONAME );

                    // Do the modes match?
                    if( grfMode != mySTATs.grfMode )
                    {
                        // Modes don't match, close out existing stream.
                        ((IDisposable) core.safeIStream).Dispose();
                        core.safeIStream = null;
                    }
                }
            }
        }

        internal CompoundFileStreamReference StreamReference
        {
            get
            {
                return _streamReference;
            }
        }

        // Inspect the transforms applied this stream and retreive the compression and
        //  RM encryption options
        private void EnsureTransformInformation()
        {
            if (_needToGetTransformInfo && InternalExists())
            {
                _encryptionOption = EncryptionOption.None;
                _compressionOption = CompressionOption.NotCompressed;

                //If the StreamInfo exists we go on to check if correct transform has been
                //applied to the Stream

                DataSpaceManager dsm = parentStorage.Root.GetDataSpaceManager();

                List<IDataTransform> transforms = dsm.GetTransformsForStreamInfo(this);

                foreach (IDataTransform dataTransform in transforms)
                {
                    string id = dataTransform.TransformIdentifier as string;
                    if (id != null)
                    {
                        id = id.ToUpperInvariant();

                        if (String.CompareOrdinal(id,
                            RightsManagementEncryptionTransform.ClassTransformIdentifier.ToUpperInvariant()) == 0
                            &&
                            (dataTransform as RightsManagementEncryptionTransform) != null)
                        {
                            _encryptionOption = EncryptionOption.RightsManagement;
                        }
                        else if (String.CompareOrdinal(id,
                            CompressionTransform.ClassTransformIdentifier.ToUpperInvariant()) == 0
                            &&
                            (dataTransform as CompressionTransform) != null)
                        {
                            // We don't persist the compression level used during compression process
                            // When we access the stream, all we can determine is whether it is compressed or not
                            // In all our scenarios, the level we use is Level 9 which is equivalent to Maximum
                            _compressionOption = CompressionOption.Maximum;
                        }
                    }
                }
                _needToGetTransformInfo = false;
            }
        }
    }
}


