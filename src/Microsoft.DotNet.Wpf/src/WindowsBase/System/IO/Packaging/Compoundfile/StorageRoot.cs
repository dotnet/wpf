// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  The root object for manipulating the WPP container.
//
//
//
//
//
//
//
//
//

using System;
using System.IO;
using System.Runtime.InteropServices;

using System.Windows;    // For exception string lookup table

using MS.Internal;                          // for Invariant
using MS.Internal.IO.Packaging.CompoundFile;
using MS.Internal.WindowsBase;

namespace System.IO.Packaging
{
/// <summary>
/// The main container class, one instance per compound file
/// </summary>
internal  class StorageRoot : StorageInfo
{
    /***********************************************************************/
    // Default values to use for the StorageRoot.Open shortcuts
    const FileMode   defaultFileMode   = FileMode.OpenOrCreate;
    const FileAccess defaultFileAccess = FileAccess.ReadWrite;
    const FileShare  defaultFileShare  = FileShare.None;
    const int        defaultSectorSize = 512;
    const int        stgFormatDocFile  = 5; // STGFMT_DOCFILE

    /***********************************************************************/
    // Instance values

    /// <summary>
    /// The reference to the IStorage root of this container.  This value 
    /// is initialized at Open and zeroed out at Close.  If this value is
    /// zero, it means the object has been disposed.
    /// </summary>
    IStorage rootIStorage;

    /// <summary>
    /// Cached instance to our data space manager
    /// </summary>
    DataSpaceManager dataSpaceManager;

    /// <summary>
    /// If we know we are in a read-only mode, we know not to do certain things.
    /// </summary>
    bool containerIsReadOnly;

    /// <summary>
    /// When data space manager is being initialized, sometimes it trips
    /// actions that would (in other circumstances) require checking the
    /// data space manager.  To avoid an infinite loop, we break it by knowing
    /// when data space manager is being initialized.
    /// </summary>
    bool dataSpaceManagerInitializationInProgress;

    /***********************************************************************/
    private StorageRoot(IStorage root, bool readOnly )
        : base( root )
    {
        rootIStorage = root;
        containerIsReadOnly = readOnly;
        dataSpaceManagerInitializationInProgress = false;
    }

    /// <summary>
    /// The access mode available on this container
    /// </summary>
    internal FileAccess OpenAccess
    {
        get
        {
            CheckRootDisposedStatus();
            if(containerIsReadOnly)
            {
                return FileAccess.Read;
            }
            else
            {
                return FileAccess.ReadWrite;
            }
        }
    }

    /// <summary>
    /// Create a container StorageRoot based on the given System.IO.Stream object
    /// </summary>
    /// <param name="baseStream">The new Stream upon which to build the new StorageRoot</param>
    /// <returns>New StorageRoot object built on the given Stream</returns>
    internal static StorageRoot CreateOnStream( Stream baseStream )
    {
        if (baseStream == null)
        {
            throw new ArgumentNullException("baseStream");
        }
        
        if( 0 == baseStream.Length )
        {
            return CreateOnStream( baseStream, FileMode.Create );
        }
        else
        {
            return CreateOnStream( baseStream, FileMode.Open );
        }
    }

    /// <summary>
    /// Create a container StorageRoot based on the given System.IO.Stream object
    /// </summary>
    /// <param name="baseStream">The new Stream upon which to build the new StorageRoot</param>
    /// <param name="mode">The mode (Open or Create) to use on the lock bytes</param>
    /// <returns>New StorageRoot object built on the given Stream</returns>
    internal static StorageRoot CreateOnStream(Stream baseStream, FileMode mode)
    {
        if( null == baseStream )
            throw new ArgumentNullException("baseStream");
        
        IStorage storageOnStream;
        int returnValue;
        int openFlags = SafeNativeCompoundFileConstants.STGM_SHARE_EXCLUSIVE;

        if( baseStream.CanRead )
        {
            if( baseStream.CanWrite )
            {
                openFlags |= SafeNativeCompoundFileConstants.STGM_READWRITE;
            }
            else
            {
                openFlags |= SafeNativeCompoundFileConstants.STGM_READ;
                
                if( FileMode.Create == mode )
                    throw new ArgumentException(
                        SR.Get(SRID.CanNotCreateContainerOnReadOnlyStream));
            }
        }
        else
        {
            throw new ArgumentException(
                SR.Get(SRID.CanNotCreateStorageRootOnNonReadableStream));
        }

        if( FileMode.Create == mode )
        {
            returnValue = SafeNativeCompoundFileMethods.SafeStgCreateDocfileOnStream(
                baseStream,
                openFlags | SafeNativeCompoundFileConstants.STGM_CREATE,
                out storageOnStream);
        }
        else if( FileMode.Open == mode )
        {
            returnValue = SafeNativeCompoundFileMethods.SafeStgOpenStorageOnStream(
                baseStream,
                openFlags,
                out storageOnStream );
        }
        else
        {
            throw new ArgumentException(
                SR.Get(SRID.CreateModeMustBeCreateOrOpen));
        }
        
        switch( (uint) returnValue )
        {
            case SafeNativeCompoundFileConstants.S_OK:
                return StorageRoot.CreateOnIStorage( 
                    storageOnStream );

            default:
                throw new IOException(
                    SR.Get(SRID.UnableToCreateOnStream),
                    new COMException(
                        SR.Get(SRID.CFAPIFailure), 
                        returnValue));
        }
    }

    /// <summary>Open a container, given only the path.</summary>
    /// <param name="path">Path to container file on local file system</param>
    /// <returns>StorageRoot instance representing the file</returns>
    internal static StorageRoot Open( 
        string path )
    {
        return Open( path, defaultFileMode, defaultFileAccess, defaultFileShare, defaultSectorSize );
    }

    /// <summary>Open a container, given path and open mode</summary>
    /// <param name="path">Path to container file on local file system</param>
    /// <param name="mode">Access mode, see System.IO.FileMode in .NET SDK</param>
    /// <returns>StorageRoot instance representing the file</returns>
    internal static StorageRoot Open( 
        string path, 
        FileMode mode )
    {
        return Open( path, mode, defaultFileAccess, defaultFileShare, defaultSectorSize );
    }

    /// <summary>Open a container, given path, open mode, and access flag</summary>
    /// <param name="path">Path to container file on local file system</param>
    /// <param name="mode">See System.IO.FileMode in .NET SDK</param>
    /// <param name="access">See System.IO.FileAccess in .NET SDK</param>
    /// <returns>StorageRoot instance representing the file</returns>
    internal static StorageRoot Open( 
        string path, 
        FileMode mode, 
        FileAccess access )
    {
        return Open( path, mode, access, defaultFileShare, defaultSectorSize );
    }

    /// <summary>Open a container, given path, open mode, access flag, and sharing settings</summary>
    /// <param name="path">Path to container on local file system</param>
    /// <param name="mode">See System.IO.FileMode in .NET SDK</param>
    /// <param name="access">See System.IO.FileAccess in .NET SDK</param>
    /// <param name="share">See System.IO.FileSharing in .NET SDK</param>
    /// <returns>StorageRoot instance representing the file</returns>
    internal static StorageRoot Open( 
        string path, 
        FileMode mode, 
        FileAccess access, 
        FileShare share )
    {
        return Open( path, mode, access, share, defaultSectorSize );
    }
    /// <summary>Open a container given all the settings</summary>
    /// <param name="path">Path to container on local file system</param>
    /// <param name="mode">See System.IO.FileMode in .NET SDK</param>
    /// <param name="access">See System.IO.FileAccess in .NET SDK</param>
    /// <param name="share">See System.IO.FileShare in .NET SDK</param>
    /// <param name="sectorSize">Compound File sector size, must be 512 or 4096</param>
    /// <returns>StorageRoot instance representing the file</returns>
    internal static StorageRoot Open( 
        string path,
        FileMode mode,
        FileAccess access,
        FileShare share,
        int sectorSize )
    {
        int  grfMode = 0;
        int  returnValue = 0;

        // Simple path validation
        ContainerUtilities.CheckStringAgainstNullAndEmpty( path, "Path" );

        Guid IID_IStorage = new Guid(0x0000000B,0x0000,0x0000,0xC0,0x00,
                                     0x00,0x00,0x00,0x00,0x00,0x46);

        IStorage newRootStorage;

        ////////////////////////////////////////////////////////////////////
        // Generate STGM from FileMode
        switch(mode)
        {
            case FileMode.Append:
                throw new ArgumentException(
                    SR.Get(SRID.FileModeUnsupported));
            case FileMode.Create:
                grfMode |= SafeNativeCompoundFileConstants.STGM_CREATE;
                break;
            case FileMode.CreateNew:
                {
                    FileInfo existTest = new FileInfo(path);
                    if( existTest.Exists )
                    {
                        throw new IOException(
                            SR.Get(SRID.FileAlreadyExists));
                    }
                }
                goto case FileMode.Create;
            case FileMode.Open:
                break;
            case FileMode.OpenOrCreate:
                {
                    FileInfo existTest = new FileInfo(path);
                    if( existTest.Exists )
                    {
                        // File exists, use open code path
                        goto case FileMode.Open;
                    }
                    else
                    {
                        // File does not exist, use create code path
                        goto case FileMode.Create;
                    }
                }
            case FileMode.Truncate:
                throw new ArgumentException(
                    SR.Get(SRID.FileModeUnsupported));
            default:
                throw new ArgumentException(
                    SR.Get(SRID.FileModeInvalid));
        }

        // Generate the access flags from the access parameter
        SafeNativeCompoundFileMethods.UpdateModeFlagFromFileAccess( access, ref grfMode );

        // Generate STGM from FileShare

        // Note: the .NET SDK does not specify the proper behavior in reaction to
        //  incompatible flags being sent in together.  Should ArgumentException be
        //  thrown?  Or do some values "trump" others?
        if( 0 != (share & FileShare.Inheritable) )
        {
            throw new ArgumentException(
                SR.Get(SRID.FileShareUnsupported));
        }
        else if( share == FileShare.None ) // FileShare.None is zero, using "&" to check causes unreachable code error
        {
            grfMode |= SafeNativeCompoundFileConstants.STGM_SHARE_EXCLUSIVE;
        }
        else if( share == FileShare.Read )
        {
            grfMode |= SafeNativeCompoundFileConstants.STGM_SHARE_DENY_WRITE;
        }
        else if( share == FileShare.Write )
        {
            grfMode |= SafeNativeCompoundFileConstants.STGM_SHARE_DENY_READ; // Note that this makes little sense when we don't support combination of flags
        }
        else if( share == FileShare.ReadWrite )
        {
            grfMode |= SafeNativeCompoundFileConstants.STGM_SHARE_DENY_NONE;
        }
        else
        {
            throw new ArgumentException(
                SR.Get(SRID.FileShareInvalid));
        }

        if( 0 != (grfMode & SafeNativeCompoundFileConstants.STGM_CREATE))
        {
            // STGM_CREATE set, call StgCreateStorageEx.
            returnValue = SafeNativeCompoundFileMethods.SafeStgCreateStorageEx(
                path,
                grfMode,
                stgFormatDocFile,
                0,
                IntPtr.Zero,
                IntPtr.Zero,
                ref IID_IStorage,
                out newRootStorage );
        }
        else
        {
            // STGM_CREATE not set, call StgOpenStorageEx.
            returnValue = SafeNativeCompoundFileMethods.SafeStgOpenStorageEx(
                path,
                grfMode,
                stgFormatDocFile,
                0,
                IntPtr.Zero,
                IntPtr.Zero,
                ref IID_IStorage,
                out newRootStorage );
        }

        switch( returnValue )
        {
            case SafeNativeCompoundFileConstants.S_OK:
                return StorageRoot.CreateOnIStorage( 
                    newRootStorage );
            case SafeNativeCompoundFileConstants.STG_E_FILENOTFOUND:
                throw new FileNotFoundException( 
                    SR.Get(SRID.ContainerNotFound));
            case SafeNativeCompoundFileConstants.STG_E_INVALIDFLAG:
                throw new ArgumentException( 
                    SR.Get(SRID.StorageFlagsUnsupported),
                    new COMException(
                        SR.Get(SRID.CFAPIFailure), 
                        returnValue));
            default:
                throw new IOException(
                    SR.Get(SRID.ContainerCanNotOpen),
                    new COMException(
                        SR.Get(SRID.CFAPIFailure), 
                        returnValue));
        }
    }

    /// <summary>
    /// Clean up this container storage instance
    /// </summary>
    internal void Close()
    {
        if( null == rootIStorage )
            return; // Extraneous calls to Close() are ignored
            
        if( null != dataSpaceManager )
        {
            // Tell data space manager to flush all information as necessary
            dataSpaceManager.Dispose();
            dataSpaceManager = null;
        }

        try
        {
            // Shut down the underlying storage
            if( !containerIsReadOnly )
                rootIStorage.Commit(0);
        }
        finally
        {
            // We need these clean up steps to run even if there's a problem 
            //  with the commit above.
            RecursiveStorageInfoCoreRelease( core );

            rootIStorage = null;
        }
    }

    /// <summary>
    /// Flush the storage root.
    /// </summary>
    internal void Flush()
    {
        CheckRootDisposedStatus();

        // Shut down the underlying storage
        if( !containerIsReadOnly )
            rootIStorage.Commit(0);
    }

    /// <summary>
    /// Obtains the data space manager object for this instance of the
    /// container.  Subsequent calls will return reference to the same
    /// object.
    /// </summary>
    /// <returns>Reference to the manager object</returns>
    internal DataSpaceManager GetDataSpaceManager()
    {
        CheckRootDisposedStatus();
        if( null == dataSpaceManager )
        {
            if ( dataSpaceManagerInitializationInProgress )
                return null;  // initialization in progress - abort
            
            // Create new instance of data space manager
            dataSpaceManagerInitializationInProgress = true;
            dataSpaceManager = new DataSpaceManager( this );
            dataSpaceManagerInitializationInProgress = false;
        }

        return dataSpaceManager;
    }

    internal IStorage GetRootIStorage()
    {
        return rootIStorage;
    }

    // Check whether this StorageRoot class still has its underlying storage.
    //  If not, throw an object disposed exception.  This should be checked from
    //  every non-static container external API.
    internal void CheckRootDisposedStatus()
    {
        if( RootDisposed )
            throw new ObjectDisposedException(null, SR.Get(SRID.StorageRootDisposed));
    }

    // Check whether this StorageRoot class still has its underlying storage.
    internal bool RootDisposed
    {
        get
        {
            return ( null == rootIStorage );
        }
    }

    /// <summary>
    /// This will create a container StorageRoot based on the given IStorage
    /// interface
    /// </summary>
    /// <param name="root">The new IStorage (RCW) upon which to build the new StorageRoot</param>
    /// <returns>New StorageRoot object built on the given IStorage</returns>
    private static StorageRoot CreateOnIStorage( IStorage root )
    {
        // The root is created by calling unmanaged CompoundFile APIs. The return value from the call is always
        //  checked to see if is S_OK. If it is S_OK, the root should never be null. However, just to make sure
        //  call Invariant.Assert
        Invariant.Assert(root != null);

        System.Runtime.InteropServices.ComTypes.STATSTG rootSTAT;
        bool readOnly;

        root.Stat( out rootSTAT, SafeNativeCompoundFileConstants.STATFLAG_NONAME );

        readOnly =( SafeNativeCompoundFileConstants.STGM_WRITE != ( rootSTAT.grfMode & SafeNativeCompoundFileConstants.STGM_WRITE ) &&
            SafeNativeCompoundFileConstants.STGM_READWRITE != (rootSTAT.grfMode & SafeNativeCompoundFileConstants.STGM_READWRITE) );

        return new StorageRoot( root, readOnly );
    }
}
}
       
