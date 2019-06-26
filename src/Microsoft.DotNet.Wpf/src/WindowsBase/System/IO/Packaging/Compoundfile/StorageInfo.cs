// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//   Class for manipulating storages in the container file.



using System;
using System.Collections;
using System.ComponentModel; // For EditorBrowsable attribute
using System.Diagnostics; // For Assert
using System.Security;
using System.IO;
using System.Globalization;             //  CultureInfo.InvariantCulture


using System.Windows;                 //  SR.Get(SRID.[exception message])
using MS.Internal.IO.Packaging.CompoundFile;
using CU = MS.Internal.IO.Packaging.CompoundFile.ContainerUtilities;
using MS.Internal; // for Invariant & CriticalExceptions
using System.Runtime.InteropServices;        // COMException
using MS.Internal.WindowsBase;

#pragma warning disable 1634, 1691  // suppressing PreSharp warnings

namespace System.IO.Packaging
{
/// <summary>
/// This class holds the core information for a StorageInfo object.
/// </summary>
internal class StorageInfoCore
{
    internal StorageInfoCore( 
        string nameStorage
            ) : this( nameStorage, null ) {;}

    internal StorageInfoCore( 
        string nameStorage, 
        IStorage storage )
    {
        storageName = nameStorage;
        safeIStorage = storage;
        validEnumerators = new Hashtable();
        // Storage and Stream names: we preserve casing, but do case-insensitive comparison (Native CompoundFile API behavior)
        elementInfoCores = new Hashtable(CU.StringCaseInsensitiveComparer);
    }

    /// <summary>
    /// The compound-file friendly version name.
    /// </summary>
    internal string   storageName;

    /// <summary>
    /// A reference to "this" storage.  This value is non-null only when
    /// 1) The storage exists
    /// 2) We had reason to open it.
    /// The value may be null even when the storage exists because we may not
    /// have need to go and open it.
    /// </summary>
    internal IStorage safeIStorage;

    /// <summary>
    /// We keep track of the enumerator objects that we've handed out.
    /// If anything is changed in this storage, we go and invalidate all of
    /// them and clear the list.
    /// 
    /// In theory a simple ListDictionary class is more efficient than using
    /// a Hashtable class.  But since we're bringing in the code for the
    /// HashTable class anyway, the savings of using ListDictionary went away.
    /// So even with a maximum of three elements, we use a Hashtable.
    /// </summary>
    internal Hashtable validEnumerators;

    /// <summary>
    /// This hash table holds the standing "core" objects for its child
    /// elements.  Each element may be a StorageInfoCore or a
    /// StreamInfoCore.
    /// </summary>
    internal Hashtable elementInfoCores;
}

/// <summary>
/// Class for manipulating storages in the container file
/// </summary>
public class StorageInfo
{
    /***********************************************************************/
    // Instance values

    /// <summary>
    /// Each storage holds a reference to its parent, this way even if the
    /// client app releases the reference it'll be kept in the reference graph
    /// to avoid getting prematurely garbage-collected.
    /// 
    /// The only time this is allowed to be null is when this storage is the
    /// root storage.
    /// </summary>
    StorageInfo parentStorage;

    /// <summary>
    /// Each storage holds a reference to the container root.  This value will
    /// be equal to null for the container root.
    /// </summary>
    StorageRoot rootStorage;

    /// <summary>
    /// There is one StorageInfoCore object per underlying IStorage. If 
    /// multiple StorageInfo objects are created that point to the same
    /// underlying storage, they share the same StorageInfoCore object.
    /// These are maintained by the parent storage for all its child
    /// storages, with the exception of the root StorageInfo which keeps
    /// its own instance in StorageRoot.
    /// </summary>
    internal StorageInfoCore core;

    // Instance name for the compression transform
    private static readonly string sc_compressionTransformName = "CompressionTransform";

    //Dataspace label definitions for compression and encryption combinations while creating a stream
    private static readonly string sc_dataspaceLabelNoEncryptionNormalCompression = "NoEncryptionNormalCompression";
    private static readonly string sc_dataspaceLabelRMEncryptionNormalCompression = "RMEncryptionNormalCompression";

    /// <summary>
    /// We can have three valid enumerator types.
    /// </summary>
    private enum EnumeratorTypes
    {
        Everything,
        OnlyStorages,
        OnlyStreams
    }

    /***********************************************************************/
    // Constructors

    /// <summary>
    /// A constructor for building the root storage.
    /// This should only happen for, well, the root storage!
    /// </summary>
    internal StorageInfo( IStorage safeIStorage )
    {
        core = new StorageInfoCore( null, safeIStorage );
    }

    /// <summary>
    ///     Given a parent and a path under it, step through each of the path
    /// elements and create an intermediate StorageInfo at each step because
    /// a StorageInfo is only meaningful relative to its immediate parent -
    /// multi-step relations can't be represented.
    /// </summary>
    private void BuildStorageInfoRelativeToStorage( StorageInfo parent, string fileName )
    {
        parentStorage = parent;
        core = parent.CoreForChildStorage( fileName );
        rootStorage = parent.Root;
    }

    /// <summary>
    /// Constructor for a StorageInfo given a parent StorageInfo and a name
    /// </summary>
    /// <param name="parent">Reference to the parent storage</param>
    /// <param name="fileName">filename for the new StorageInfo</param>
    internal StorageInfo( StorageInfo parent, string fileName )
    {
        CU.CheckAgainstNull( parent, "parent" );
        CU.CheckAgainstNull( fileName, "fileName" );

        BuildStorageInfoRelativeToStorage( parent,  fileName );
    }

    /// <summary>
    /// Respond to a request from a child StorageInfo object to give
    /// it a StorageInfoCore object for the given name.
    /// </summary>
    StorageInfoCore CoreForChildStorage( string storageNname )
    {
        CheckDisposedStatus();
        
        object childElement = core.elementInfoCores[ storageNname ];

        if( null != childElement &&
            null == childElement as StorageInfoCore )
        {
            // Name is already in use, but not as a StorageInfo
            throw new InvalidOperationException(
                SR.Get(SRID.NameAlreadyInUse, storageNname ));
        }
        else if( null == childElement )
        {
            // No such element with the name exist - create one.
            // 
            childElement = new StorageInfoCore( storageNname);
            core.elementInfoCores[ storageNname ] = childElement;
        }

        Debug.Assert( null != childElement as StorageInfoCore,
            "We should have already checked to make sure childElement is either StorageInfoCore, created as one, or thrown an exception if neither is possible");

        return childElement as StorageInfoCore;
    }

    internal StreamInfoCore CoreForChildStream( string streamName )
    {
        CheckDisposedStatus();
        
        object childElement = core.elementInfoCores[ streamName ];

        if( null != childElement &&
            null == childElement as StreamInfoCore )
        {
            // Name is already in use, but not as a StreamInfo
            throw new InvalidOperationException(
                SR.Get(SRID.NameAlreadyInUse, streamName ));
        }
        else if( null == childElement )
        {
            // No such element with the name exist - create one.
            // 

            // Check to see if there is a data space mapping on this guy
            DataSpaceManager manager = Root.GetDataSpaceManager();
            if( null != manager )
            {
                // Have data space manager - retrieve data space label for 
                //  the child stream.  The data space manager will return 
                //  null if there isn't a data space associated with this
                //  stream.
                childElement = new StreamInfoCore( 
                    streamName,
                    manager.DataSpaceOf( 
                        new CompoundFileStreamReference( FullNameInternal, streamName ) ) );
            }
            else
            {
                // Data space manager not yet initialized - correct behavior
                //  is that nothing transformed should be required at this
                //  point but in case of incorrect behavior we can not possibly
                //  recover.  User gets un-transformed data instead.
                childElement = new StreamInfoCore( streamName, null, null );
            }
            core.elementInfoCores[ streamName ] = childElement;
        }

        Debug.Assert( null != childElement as StreamInfoCore,
            "We should have already checked to make sure childElement is either StreamInfoCore, created as one, or thrown an exception if neither is possible");

        return childElement as StreamInfoCore;
    }

    /***********************************************************************/
    // public Properties

    /// <summary>
    /// The name for this storage
    /// </summary>
    public string Name
    {
        get
        {
            CheckDisposedStatus();
            return core.storageName;
        }
    }

    /***********************************************************************/
    // public Methods

    /// <summary>
    /// Creates "this" stream
    /// </summary>
    /// <param name="name">Name of stream</param>
    /// <param name="compressionOption">CompressionOptiont</param>
    /// <param name="encryptionOption">EncryptionOption</param>
    /// <returns>Reference to new stream</returns>
    public StreamInfo CreateStream( string name, CompressionOption compressionOption, EncryptionOption encryptionOption )
    {
        CheckDisposedStatus();

        //check the arguments
        if( null == name )
            throw new ArgumentNullException("name");

        // Stream names: we preserve casing, but do case-insensitive comparison (Native CompoundFile API behavior)
        if (((IEqualityComparer) CU.StringCaseInsensitiveComparer).Equals(name,
                    EncryptedPackageEnvelope.PackageStreamName))
            throw new ArgumentException(SR.Get(SRID.StreamNameNotValid,name));

        //create a new streaminfo object
        StreamInfo streamInfo = new StreamInfo(this, name, compressionOption, encryptionOption);
        if (streamInfo.InternalExists())
        {
            throw new IOException(SR.Get(SRID.StreamAlreadyExist));
        }

        //Define the compression and encryption options in the dataspacemanager
        DataSpaceManager manager = Root.GetDataSpaceManager();
        string dataSpaceLabel = null;
            
        if (manager != null)
        {
            //case : Compression option is set. Stream need to be compressed. Define compression transform.
            //At this time, we only treat CompressionOption - Normal and None. The rest are treated as Normal
            if (compressionOption != CompressionOption.NotCompressed)
            {
                //If it is not defined already, define it.
                if (!manager.TransformLabelIsDefined(sc_compressionTransformName))
                        manager.DefineTransform(CompressionTransform.ClassTransformIdentifier, sc_compressionTransformName);                
            }
             //case : Encryption option is set. Stream need to be encrypted. Define encryption transform.
            if (encryptionOption == EncryptionOption.RightsManagement)
            {
                //If it not defined already, define it.
                if (!manager.TransformLabelIsDefined(EncryptedPackageEnvelope.EncryptionTransformName))
                {
                    //We really cannot define RM transform completely here because the transform initialization cannot be done here without publishlicense and cryptoprovider.
                    //However it will always be defined because this method is accessed only through an EncryptedPackageEnvelope and RM transform is always defined in EncryptedPackageEnvelope.Create()
                    throw new SystemException(SR.Get(SRID.RightsManagementEncryptionTransformNotFound));
                }
            }

            //Now find the dataspace label that we need to define these transforms in.
            //CASE: When both CompressionOption and EncryptionOption are set
            if ( (compressionOption != CompressionOption.NotCompressed) && (encryptionOption == EncryptionOption.RightsManagement) )
            {
                dataSpaceLabel = sc_dataspaceLabelRMEncryptionNormalCompression;
                if (!manager.DataSpaceIsDefined(dataSpaceLabel))
                {
                    string[] transformStack = new string[2];
                    //compress the data first. then encrypt it. This ordering will cause the content to be compressed, then encrypted, then written to the stream.
                    transformStack[0] = EncryptedPackageEnvelope.EncryptionTransformName;
                    transformStack[1] = sc_compressionTransformName; 

                    manager.DefineDataSpace(transformStack, dataSpaceLabel);
                }
            }
            //CASE : when CompressionOption alone is set
            else if ( (compressionOption != CompressionOption.NotCompressed)  && (encryptionOption == EncryptionOption.None) )
            {
                dataSpaceLabel = sc_dataspaceLabelNoEncryptionNormalCompression;
                if (!manager.DataSpaceIsDefined(dataSpaceLabel))
                {
                    string[] transformStack = new string[1];
                    transformStack[0] = sc_compressionTransformName; 

                    manager.DefineDataSpace(transformStack, dataSpaceLabel);
                }
            }
            //CASE : when EncryptionOption alone is set
            else if (encryptionOption == EncryptionOption.RightsManagement)
            {
                dataSpaceLabel = EncryptedPackageEnvelope.DataspaceLabelRMEncryptionNoCompression;
                if (!manager.DataSpaceIsDefined(dataSpaceLabel))
                {
                    string[] transformStack = new string[1];
                    transformStack[0] = EncryptedPackageEnvelope.EncryptionTransformName;

                    manager.DefineDataSpace(transformStack, dataSpaceLabel);
                }
            }
            //All the other cases are not handled at this point.
        }

        //create the underlying stream
        if (null == dataSpaceLabel)
            streamInfo.Create(); //create the stream with default parameters
        else
            streamInfo.Create(dataSpaceLabel); //create the stream in the defined dataspace
 
        return streamInfo;
    }

    /// <summary>
    /// Creates "this" stream
    /// </summary>
    /// <param name="name">Name of stream</param>
    /// <returns>Reference to new stream</returns>
    public StreamInfo CreateStream( string name )
    {
        //create the stream with out any compression or encryption options.
        return CreateStream(name,CompressionOption.NotCompressed,EncryptionOption.None);
    }

    /// <summary>
    /// Returns the streaminfo by the passed name.
    /// </summary>
    /// <param name="name">Name of stream</param>
    /// <returns>Reference to the stream</returns>
    public StreamInfo GetStreamInfo(string name)
    {
        CheckDisposedStatus();
        
         //check the arguments
        if( null == name )
            throw new ArgumentNullException("name");

        StreamInfo streamInfo = new StreamInfo(this, name);
        if (streamInfo.InternalExists())
        {
            return streamInfo;
        }
        else
        {
            throw new IOException(SR.Get(SRID.StreamNotExist));
        }
    }

    /// <summary>
    /// Check if the stream exists.
    /// </summary>
    /// <param name="name">Name of stream</param>
    /// <returns>True if exists, False if not</returns>
    public bool StreamExists(string name)
    {
        CheckDisposedStatus();
        
        bool streamExists = false;

        StreamInfo streamInfo = new StreamInfo(this, name);
        streamExists = streamInfo.InternalExists();

        return streamExists;
    }

    /// <summary>
    /// Deleted the stream with the passed name.
    /// </summary>
    /// <param name="name">Name of stream</param>
    public void DeleteStream(string name)
    {
        CheckDisposedStatus();
        
         //check the arguments
        if( null == name )
            throw new ArgumentNullException("name");
        
        StreamInfo streamInfo = new StreamInfo(this, name);
        if (streamInfo.InternalExists())
        {
            streamInfo.Delete();
        }
    }

    /// <summary>
    /// Creates a storage using "this" one as parent
    /// </summary>
    /// <param name="name">Name of new storage</param>
    /// <returns>Reference to new storage</returns>
    public StorageInfo CreateSubStorage( string name )
    {
        CheckDisposedStatus();
        
         //check the arguments
        if( null == name )
            throw new ArgumentNullException("name");
        
        return CreateStorage(name);
    }

    /// <summary>
    /// Returns the storage by the passed name.
    /// </summary>
    /// <param name="name">Name of storage</param>
    /// <returns>Reference to the storage</returns>
    public StorageInfo GetSubStorageInfo(string name)
    {
        //Find if this storage exists
        StorageInfo storageInfo = new StorageInfo(this, name);

        if (storageInfo.InternalExists(name))
        {
            return storageInfo;
        }
        else
        {
            throw new IOException(SR.Get(SRID.StorageNotExist));
        }
    }

     /// <summary>
    /// Checks if a storage exists by the passed name.
    /// </summary>
    /// <param name="name">Name of storage</param>
    /// <returns>Reference to new storage</returns>
    public bool SubStorageExists(string name)
    {
        StorageInfo storageInfo = new StorageInfo(this, name);
        return storageInfo.InternalExists(name);
    }
    
    /// <summary>
    /// Deletes a storage recursively.
    /// </summary>
    /// <param name="name">Name of storage</param>
    public void DeleteSubStorage(string name)
    {
        CheckDisposedStatus();

         //check the arguments
        if( null == name )
            throw new ArgumentNullException("name");

        StorageInfo storageInfo = new StorageInfo(this, name);
        if (storageInfo.InternalExists(name))
        {
            InvalidateEnumerators();
            // Go ahead and delete "this" storage
            DestroyElement( name );
        }
        //We will not throw exceptions if the storage does not exist. This is to be consistent with Package.DeletePart.
    }

    /// <summary>
    /// Provides a snapshot picture of the streams currently within this storage
    /// object.  The array that returns will not be updated with additions/
    /// removals of streams, but the individual StreamInfo objects within will
    /// reflect the state of their respective streams.
    /// </summary>
    /// <remark>
    /// This follows the precedent of DirectoryInfo.GetFiles()
    /// </remark>
    /// <returns>
    /// Array of StreamInfo objects, each pointing to a stream within this
    /// storage.  Empty (zero-length) array if there are no streams.
    /// </returns>
    public StreamInfo[] GetStreams()
    {
        // Make sure 'this' storage is alive and well.
        CheckDisposedStatus();
        VerifyExists();
        
        // Build an array of StreamInfo objects
        EnsureArrayForEnumeration(EnumeratorTypes.OnlyStreams);
        
        // Because we're handing out a snapshot, we can't simply hand out a
        //  reference to the core.validEnumerators array.  We need to make a
        //  copy that has the references of the array.  This way the array
        //  we return will remain if we invalidate the arrays.
        // Fortunately ArrayList.ToArray makes a copy, perfect for our needs.
        ArrayList streamArray = 
            (ArrayList)core.validEnumerators[EnumeratorTypes.OnlyStreams];

        Invariant.Assert(streamArray  != null);

        #pragma warning suppress 6506 // Invariant.Assert(streamArray  != null)
        return (StreamInfo[])streamArray.ToArray(typeof(StreamInfo));
    }

    /// <summary>
    /// Provides a snapshot picture of the sub-storages currently within this storage
    /// object.  The array that returns will not be updated with additions/
    /// removals of storages, but the individual StorageInfo objects within will
    /// reflect the state of their respective sub-storages.
    /// </summary>
    /// <remark>
    /// This follows the precedent of DirectoryInfo.GetDirectories()
    /// </remark>
    /// <returns>
    /// Array of StorageInfo objects, each pointing to a sub-storage within this
    /// storage.  Empty (zero-length) array if there are no sub-storages.
    /// </returns>
    public StorageInfo[] GetSubStorages()
    {
        // Make sure 'this' storage is alive and well.
        CheckDisposedStatus();
        VerifyExists();
        
        // See GetStreams counterpart for details.
        EnsureArrayForEnumeration(EnumeratorTypes.OnlyStorages);
        ArrayList storageArray = 
            (ArrayList)core.validEnumerators[EnumeratorTypes.OnlyStorages];

        Invariant.Assert(storageArray != null);

        #pragma warning suppress 6506 // Invariant.Assert(streamArray  != null)
        return (StorageInfo[])storageArray.ToArray(typeof(StorageInfo));
    }

    /***********************************************************************/
    // Internal/Private functionality

    internal string FullNameInternal
    {
        get
        {
            CheckDisposedStatus();
            return CU.ConvertStringArrayPathToBackSlashPath(BuildFullNameInternalFromParentNameInternal());
        }
    }

    /// <summary>
    /// Get a reference to the container instance
    /// </summary>
    internal StorageRoot Root
    {
        get
        {
            CheckDisposedStatus();
            if( null == rootStorage )
                return (StorageRoot)this;
            else
                return rootStorage;
        }
    }

    /// <summary>
    /// Because it is valid to have a StorageInfo point to a storage that 
    /// doesn't yet actually exist, use this to see if it does.
    /// </summary>
    internal bool Exists
    {
        get
        {
            CheckDisposedStatus();
            return InternalExists();
        }
    }

    /// <summary>
    /// Creates "this" storage
    /// </summary>
    internal void Create()
    {
        CheckDisposedStatus();
        if( null != parentStorage ) // Only root storage has null parentStorage
        {
            // Root storage always exists so we don't do any of this 
            //  if we're not the root.
    
            if( !parentStorage.Exists )
            {
                // We need the parent to exist before we can exist.
                parentStorage.Create();
            }

            if( !InternalExists() )
            {
                // If we don't exist, then ask parent to create us.
                parentStorage.CreateStorage( core.storageName );
            }
            //else if we already exist, we're done here.
        }
    }

    private StorageInfo CreateStorage(string name)
    {
        // Create new StorageInfo
        StorageInfo newSubStorage = new StorageInfo( this, name );
    
        // Make it real
        if( !newSubStorage.InternalExists(name) )
        {
            /* TBD
            if( !CU.IsValidCompoundFileName(name))
            {
                throw new IOException(
                        SR.Get(SRID.UnableToCreateStorage),
                        new COMException( 
                            SR.Get(SRID.NamedAPIFailure, "IStorage.CreateStorage"), 
                            nativeCallErrorCode ));
                }
                */
            // It doesn't already exist, please create.
            StorageInfoCore newStorage = core.elementInfoCores[ name ] as StorageInfoCore;
            Invariant.Assert( null != newStorage);
    
            int nativeCallErrorCode = core.safeIStorage.CreateStorage(
                        name, 
                    (GetStat().grfMode & SafeNativeCompoundFileConstants.STGM_READWRITE_Bits)
                        | SafeNativeCompoundFileConstants.STGM_SHARE_EXCLUSIVE,
                    0,
                    0,
                #pragma warning suppress 6506 // Invariant.Assert(null != newStorage)
                out newStorage.safeIStorage );
            if( SafeNativeCompoundFileConstants.S_OK != nativeCallErrorCode )
            {
                if( nativeCallErrorCode == SafeNativeCompoundFileConstants.STG_E_ACCESSDENIED )
                {
                    throw new UnauthorizedAccessException(
                            SR.Get(SRID.CanNotCreateAccessDenied),
                            new COMException( 
                            SR.Get(SRID.NamedAPIFailure, "IStorage.CreateStorage"), 
                            nativeCallErrorCode ));
                }
                else
                {
                    throw new IOException(
                        SR.Get(SRID.UnableToCreateStorage),
                        new COMException( 
                            SR.Get(SRID.NamedAPIFailure, "IStorage.CreateStorage"), 
                            nativeCallErrorCode ));
                }
            }
    
            // Invalidate enumerators
            InvalidateEnumerators();
        }
        else
        {
            throw new IOException(SR.Get(SRID.StorageAlreadyExist));
        }
    
        // Return a reference
        return newSubStorage;
    }
    
    /// <summary>
    /// Deletes a storage, recursively if specified.
    /// </summary>
    /// <param name="recursive">Whether to recursive delete all existing content</param>
    /// <param name="name">Name of storage</param>
    internal bool Delete( bool recursive , string name)
    {
        bool storageDeleted = false;
        CheckDisposedStatus();
        if( null == parentStorage )
        {
            // We are the root storage, you can't "delete" the root storage!
            throw new InvalidOperationException(
                SR.Get(SRID.CanNotDeleteRoot));
        }

        if( InternalExists(name) )
        {
            if( !recursive && !StorageIsEmpty())
            {
                throw new IOException(
                    SR.Get(SRID.CanNotDeleteNonEmptyStorage));
            }

            InvalidateEnumerators();
            // Go ahead and delete "this" storage
            parentStorage.DestroyElement( name );
            storageDeleted = true;
        }
        //We will not throw exceptions if the storage does not exist. This is to be consistent with Package.DeletePart.
        
        return storageDeleted;
    }

    /// <summary>
    /// When a substorage is getting deleted, its references in the dataspacemanager's transform definition are removed.
    /// This is called recursively because the DeleteSubStorage deletes all its children by default.
    /// </summary>
    internal void RemoveSubStorageEntryFromDataSpaceMap(StorageInfo storageInfo)
    {
        StorageInfo[] subStorages = storageInfo.GetSubStorages();
        foreach(StorageInfo storage in subStorages)
        {
            //If this is a storage, call recursively till we encounter a stream. Then we can use that container (storage,stream) reference to remove from the
            // dataspace manager's data space map.
            RemoveSubStorageEntryFromDataSpaceMap(storage); //recursive call
        }

        //Now we have StorageInfo. Find if there is a stream underneath so a container can be used as reference in data space map of data spacemanager.
        StreamInfo[] streams = storageInfo.GetStreams();
        DataSpaceManager manager = Root.GetDataSpaceManager();
        foreach(StreamInfo stream in streams)
        {
            manager.RemoveContainerFromDataSpaceMap(new CompoundFileStreamReference( storageInfo.FullNameInternal, stream.Name ));
        }
    }

    /// <summary>
    /// Destroys an element and removes the references ued internally.
    /// </summary>
    internal void DestroyElement( string elementNameInternal )
    {
        object deadElementWalking = core.elementInfoCores[ elementNameInternal ];
        // It's an internal error if we try to call this without first
        //  verifying that it is indeed there.
        Debug.Assert( null != deadElementWalking,
            "Caller should have already verified that there's something to delete.");

        // Can't delete if we're in read-only mode.  This catches some but not
        //  all invalid delete scenarios - anything else would come back as a
        //  COMException of some kind that will be caught and wrapped in an
        //  IOException in the try/catch below.
        if( FileAccess.Read == Root.OpenAccess )
        {
            throw new UnauthorizedAccessException(
                SR.Get(SRID.CanNotDeleteInReadOnly));
        }

        //Clean out the entry in dataspacemanager for stream transforms
        DataSpaceManager manager = Root.GetDataSpaceManager();
        if( null != manager )
        {
             if( deadElementWalking is StorageInfoCore )
            {
                //if the element getting deleted is a storage, make sure to delete all its children's references.
                string name = ((StorageInfoCore)deadElementWalking).storageName;
                StorageInfo stInfo = new StorageInfo(this, name);
                RemoveSubStorageEntryFromDataSpaceMap(stInfo);
            }
            else if( deadElementWalking is StreamInfoCore )
            {
                //if the element getting deleted is a stream, the container reference should be removed from dataspacemap of dataspace manager.
                manager.RemoveContainerFromDataSpaceMap(new CompoundFileStreamReference( FullNameInternal, elementNameInternal ));
            }
        }
        
        // Make the call to the underlying OLE mechanism to remove the element.            
        try
        {
            core.safeIStorage.DestroyElement( elementNameInternal );
        }
        catch( COMException e )
        {
            if( e.ErrorCode == SafeNativeCompoundFileConstants.STG_E_ACCESSDENIED )
            {
                throw new UnauthorizedAccessException(
                    SR.Get(SRID.CanNotDeleteAccessDenied),
                    e );
            }
            else
            {
                throw new IOException(
                    SR.Get(SRID.CanNotDelete),
                    e );
            }
        }

        // Invalidate enumerators
        InvalidateEnumerators();
        
        // Remove the now-meaningless name, which also signifies disposed status.
        if( deadElementWalking is StorageInfoCore )
        {
            StorageInfoCore deadStorageInfoCore = (StorageInfoCore)deadElementWalking;

            // Erase this storage's existence
            deadStorageInfoCore.storageName = null;
            if( null != deadStorageInfoCore.safeIStorage )
            {
                ((IDisposable) deadStorageInfoCore.safeIStorage).Dispose();
                deadStorageInfoCore.safeIStorage = null;
            }
        }
        else if( deadElementWalking is StreamInfoCore )
        {
            StreamInfoCore deadStreamInfoCore = (StreamInfoCore)deadElementWalking;

            // Erase this stream's existence
            deadStreamInfoCore.streamName = null;

            try
            {
                if (null != deadStreamInfoCore.exposedStream)
                {
                    ((Stream)(deadStreamInfoCore.exposedStream)).Close();
                }
            }
            catch(Exception e)
            {
                if(CriticalExceptions.IsCriticalException(e))
                {
                    // PreSharp Warning 56500
                    throw;
                }
                else
                {
                    // We don't care if there are any issues - 
                    //  the user wanted this stream gone anyway.
                }
            }

            deadStreamInfoCore.exposedStream = null;
            
            if( null != deadStreamInfoCore.safeIStream ) 
            {
                ((IDisposable) deadStreamInfoCore.safeIStream).Dispose();
                deadStreamInfoCore.safeIStream = null;
            }
        }
        
        // Remove reference for destroyed element
        core.elementInfoCores.Remove(elementNameInternal);
    }
    /// <summary>
    /// Looks for a storage element with the given name, retrieves its
    /// STATSTG if found.
    /// </summary>
    /// <param name="streamName">Name to look for in this storage</param>
    /// <param name="statStg">If found, a copy of STATSTG for it</param>
    /// <returns>true if found</returns>
    internal bool FindStatStgOfName( string streamName, out System.Runtime.InteropServices.ComTypes.STATSTG statStg )
    {
        bool nameFound = false;
        UInt32 actual;
        IEnumSTATSTG safeIEnumSTATSTG = null;

        // Set up IEnumSTATSTG 
        core.safeIStorage.EnumElements(
            0, 
            IntPtr.Zero, 
            0, 
            out safeIEnumSTATSTG );
        safeIEnumSTATSTG.Reset();
        safeIEnumSTATSTG.Next( 1, out statStg, out actual );

        // Loop and get everything
        while( 0 < actual && !nameFound )
        {
            // Stream names: we preserve casing, but do case-insensitive comparison (Native CompoundFile API behavior)
            if(((IEqualityComparer) CU.StringCaseInsensitiveComparer).Equals(streamName,
                                            statStg.pwcsName))
            {
                nameFound = true;
            }
            else
            {
                // Move on to the next element
                safeIEnumSTATSTG.Next( 1, out statStg, out actual );
            }
        }

        // Release enumerator
        ((IDisposable) safeIEnumSTATSTG).Dispose();
        safeIEnumSTATSTG = null;

        return nameFound;
    }

    /// <summary>
    /// Find out if a storage is empty of elements
    /// </summary>
    /// <returns>true if storage is empty</returns>
    internal bool StorageIsEmpty()
    {
        // Is there a better way than to check if enumerator has nothing?
        UInt32 actual;
        IEnumSTATSTG safeIEnumSTATSTG = null;
        System.Runtime.InteropServices.ComTypes.STATSTG dummySTATSTG;

        // Set up IEnumSTATSTG 
        core.safeIStorage.EnumElements(
            0, 
            IntPtr.Zero, 
            0, 
            out safeIEnumSTATSTG );
        safeIEnumSTATSTG.Reset();
        safeIEnumSTATSTG.Next( 1, out dummySTATSTG, out actual );

        // Release enumerator
        ((IDisposable) safeIEnumSTATSTG).Dispose();
        safeIEnumSTATSTG = null;

        // If the first "Next" call returns nothing, then there's nothing here.
        return ( 0 == actual );
    }

    /// <summary>
    /// If anything about this storage has changed, we need to go out and
    /// invalidate every outstanding enumerator so any attempt to use them
    /// will result in InvalidOperationException as specified for IEnumerator
    /// interface implementers
    /// </summary>
    internal void InvalidateEnumerators()
    {
        InvalidateEnumerators( core );
    }

    /// <summary>
    ///   Given a StorageInfoCore, clears the enumerators associated with it.
    /// </summary>
    private static void InvalidateEnumerators( StorageInfoCore invalidateCore )
    {
        // It is not enough to simply clear the validEnumerators collection,
        //  we have to clear the individual elements to let them know the
        //  outstanding enumerator is no longer valid.
        foreach( object entry in invalidateCore.validEnumerators.Values )
        {
            ((ArrayList)entry).Clear();
        }
        invalidateCore.validEnumerators.Clear();
    }

    /// <summary>
    /// This will build a full path to this storage from the parent full
    /// name and our storage name.  The array is basically the same as the
    /// parent storage's Name property plus one element - our 
    /// storageName
    /// </summary>
    /// <returns>ArrayList designating the path of this storage</returns>
    internal ArrayList BuildFullNameFromParentName()
    {
        if( null == parentStorage )
        {
            // special case for root storage
            return new ArrayList();
        }
        else
        {
            ArrayList parentArray = parentStorage.BuildFullNameFromParentName();
            parentArray.Add(core.storageName);
            return parentArray;
        }
    }

    /// <summary>
    ///     Counterpart to BuildFullNameFromParentName that uses the internal
    /// normalized names instead.
    /// </summary>
    internal ArrayList BuildFullNameInternalFromParentNameInternal()
    {
        if( null == parentStorage )
        {
            // special case for root storage
            return new ArrayList();
        }
        else
        {
            ArrayList parentArray = parentStorage.BuildFullNameInternalFromParentNameInternal();
            parentArray.Add(core.storageName);
            return parentArray;
        }
    }

    /// <summary>
    /// This needs to be available to StreamInfo so it can actually create itself
    /// </summary>
    internal IStorage SafeIStorage
    {
        get
        {
            VerifyExists();
            return core.safeIStorage;
        }
    }

    /// <summary>
    /// Every method here have a need to check if the storage exists before
    /// proceeeding with the operation.  However, for reasons I don't fully
    /// understand we're discouraged from methods calling on other externally
    /// visible methods, so they can't just call Exists().  So I just pull
    /// it out to an InternalExists method.
    /// 
    /// At this time I believe only two methods call this - Exists() because
    /// it really wants to know, VerifyExists() because it's called by
    /// everybody else just to see that the storage exists before proceeding.
    /// 
    /// If this returns true, the storage cache pointer should be live.
    /// </summary>
    /// <returns>Whether "this" storage exists</returns>
    bool InternalExists()
    {
        return InternalExists( core.storageName );
    }

    /// <summary>
    /// Every method here have a need to check if the storage exists before
    /// proceeeding with the operation.  However, for reasons I don't fully
    /// understand we're discouraged from methods calling on other externally
    /// visible methods, so they can't just call Exists().  So I just pull
    /// it out to an InternalExists method.
    /// 
    /// At this time I believe only two methods call this - Exists() because
    /// it really wants to know, VerifyExists() because it's called by
    /// everybody else just to see that the storage exists before proceeding.
    /// 
    /// If this returns true, the storage cache pointer should be live.
    /// </summary>
    /// <returns>Whether "this" storage exists</returns>
    bool InternalExists(string name)
    {
        // We can't have an IStorage unless we exist.
        if( null != core.safeIStorage )
        {
            return true; 
        }

        // If we are the root storage, we always exist.
        if( null == parentStorage )
        {
            return true;
        }

        // If the parent storage does not exist, we can't possibly exist
        if( !parentStorage.Exists )
        {
            return false;
        }

        // Now things get more complicated... we know that:
        //  * We are not the root
        //  * We have a valid parent
        //  * We don't have an IStorage interface

        // The most obvious way to check is to try opening the storage and
        //  see what happens.  It's supposed to be fairly fast and easy 
        //  because it stays within the DocFile FAT, and that'll give
        //  us our IStorage cache pointer too.

        return parentStorage.CanOpenStorage( name );
    }
    
    bool CanOpenStorage( string nameInternal )
    {
        bool openSuccess = false;
        StorageInfoCore childCore = core.elementInfoCores[ nameInternal ] as StorageInfoCore ;

        Debug.Assert( null != childCore, "Expected a child with valid core object in cache" );

        int nativeCallErrorCode = 0;

        nativeCallErrorCode = core.safeIStorage.OpenStorage(
                nameInternal,
                null,
                (GetStat().grfMode & SafeNativeCompoundFileConstants.STGM_READWRITE_Bits)
                    | SafeNativeCompoundFileConstants.STGM_SHARE_EXCLUSIVE,
                IntPtr.Zero,
                0,
                out childCore.safeIStorage );

        if( SafeNativeCompoundFileConstants.S_OK == nativeCallErrorCode )
        {
            openSuccess = true;
        }
        else if( SafeNativeCompoundFileConstants.STG_E_FILENOTFOUND != nativeCallErrorCode )
        {
            // Error is not STG_E_FILENOTFOUND, pass it on.
            throw new IOException(
                SR.Get(SRID.CanNotOpenStorage), 
                new COMException( 
                    SR.Get(SRID.NamedAPIFailure, "IStorage::OpenStorage"), 
                    nativeCallErrorCode ));
        }
        // STG_E_NOTFOUND - return openSuccess as false 

        return openSuccess;
    }

    /// <summary>
    /// Most of the time internal methods that want to do an internal check 
    /// to see if a storage exists is only interested in proceeding if it does.
    /// If it doesn't, abort with an exception.  This implements the little
    /// shortcut.
    /// </summary>
    void VerifyExists()
    {
        if( !InternalExists() )
        {
            throw new DirectoryNotFoundException(
                SR.Get(SRID.CanNotOnNonExistStorage));
        }
        return;
    }

    /// <summary>
    /// Grabs the STATSTG representing us
    /// </summary>
    System.Runtime.InteropServices.ComTypes.STATSTG GetStat()
    {
        System.Runtime.InteropServices.ComTypes.STATSTG returnValue;

        VerifyExists();

        core.safeIStorage.Stat( out returnValue, 0 );

        return returnValue;
    }

    /// <summary>
    /// Convert a System.Runtime.InteropServices.FILETIME struct to the CLR
    /// DateTime class.  Strange that the straightforward conversion doesn't 
    /// already exist.  Perhaps I'm just not finding it.  DateTime has a 
    /// method to convert itself to FILETIME, but only supports creating a
    /// DateTime from a 64-bit value representing FILETIME instead of the
    /// FILETIME struct itself.
    /// </summary>
    DateTime ConvertFILETIMEToDateTime( System.Runtime.InteropServices.ComTypes.FILETIME time )
    {
        // We should let the user know when the time is not valid, rather than 
        //  return a bogus date of Dec 31. 1600.
        if( 0 == time.dwHighDateTime &&
            0 == time.dwLowDateTime )
            throw new NotSupportedException(
                SR.Get(SRID.TimeStampNotAvailable));
        
        // CLR 









        return DateTime.FromFileTime(
            (((long)time.dwHighDateTime) << 32) +
              (uint)time.dwLowDateTime ); // This second uint is very important!!
    }

    /// <summary>
    ///  Given a StorageInfoCore, recursively release objects associated with
    /// sub-storages and then releases objects associated with self.    
    /// </summary>
    /// <remarks>
    ///  This didn't used to be a static method - but this caused some problems.
    /// All operations need to be on the given parameter "startCore".  If this
    /// wasn't static, the code had access to the member "core" and that's not
    /// the right StorageInfoCore to use.  This caused some bugs that would have
    /// been avoided if this was static.  To prevent future bugs of similar
    /// nature, I made this static.
    /// </remarks>
    internal static void RecursiveStorageInfoCoreRelease( StorageInfoCore startCore )
    {
        // If the underlying IStorage pointer is null, we never did anything
        //  with this storage or anything under it.  We can halt our recursion
        //  here.
        if( startCore.safeIStorage == null )
            return;

        try
        {
            //////////////////////////////////////////////////////////////////////
            //
            //  Call clean-up code for things on the storage represented by startCore.
            
            //////////////////////////////////////////////////////////////////////
            //
            //  Call clean-up code for things *under* startCore.
            
            // See if we have child storages and streams, and if so, close them
            //  down.
            foreach( object o in startCore.elementInfoCores.Values )
            {
                if( o is StorageInfoCore )
                {
                    RecursiveStorageInfoCoreRelease( (StorageInfoCore)o );
                }
                else if( o is StreamInfoCore )
                {
                    StreamInfoCore streamRelease = (StreamInfoCore)o;

                    try
                    {
                        if (null != streamRelease.exposedStream)
                        {
                            ((Stream)(streamRelease.exposedStream)).Close();
                        }
                        streamRelease.exposedStream = null;
                    }
                    finally
                    {
                        // We need this release and null-out to happen even if we
                        //  ran into problems with the clean-up code above.
                        if( null != streamRelease.safeIStream)
                        {
                            ((IDisposable) streamRelease.safeIStream).Dispose();
                            streamRelease.safeIStream = null;
                        }

                        // Null name in core signifies the core object is disposed
                        ((StreamInfoCore)o).streamName = null;
                    }
                }
            }

            // All child objects freed, clear out the enumerators
            InvalidateEnumerators( startCore );
        }
        finally
        {
            //  We want to make sure this happens even if any of the cleanup
            //  above fails, so that's why it's in a "finally" block here.
            
            //////////////////////////////////////////////////////////////////////
            //
            //  Free unmanaged resources associated with the startCore storage
            
            if( null != startCore.safeIStorage)
            {           
                ((IDisposable) startCore.safeIStorage).Dispose();
                startCore.safeIStorage = null;
            }

            // Null name in core signifies the core object is disposed
            startCore.storageName = null;
        }
    }

    // Check whether this StorageInfo is still valid.  Throw if disposed.
    internal void CheckDisposedStatus()
    {
        // null == parentStorage means we're root.
        if( StorageDisposed )
            throw new ObjectDisposedException(null, SR.Get(SRID.StorageInfoDisposed));
    }

    // Check whether this StorageInfo is still valid.
    internal bool StorageDisposed
    {
        get
        {
            // null == parentStorage means we're root.
            if( null != parentStorage )
            {
                // Check our core reference to see if we're valid
                if( null == core.storageName ) // Null name in core signifies the core object is disposed
                {
                    // We have been deleted
                    return true;
                }

                // We're not the root storage - check parent.
                return parentStorage.StorageDisposed;
            }
            else if (this is StorageRoot)
            {
                return ((StorageRoot)this).RootDisposed;
            }
            else
            {
                Debug.Assert(rootStorage != null, "Root storage cannot be null if StorageInfo and empty parentStorage");
                return rootStorage.RootDisposed;
            }
        }
    }

    // There is a hash table in the core object core.validEnumerators that
    //  caches the arrays used to hand out the enumerations of this storage 
    //  object.  A call to this method will ensure that the array exists.  If
    //  it already exists, this is a no-op.  If it doesn't yet exist, it is
    //  built before we return.
    // When this function exits, core.validEnumerators(desiredArrayType) will
    //  have an array of the appropriate type.
    private void EnsureArrayForEnumeration( EnumeratorTypes desiredArrayType )
    {
        Debug.Assert(
            desiredArrayType == EnumeratorTypes.Everything ||
            desiredArrayType == EnumeratorTypes.OnlyStorages ||
            desiredArrayType == EnumeratorTypes.OnlyStreams,
            "Invalid type enumeration value is being used to build enumerator array" );
        Debug.Assert( InternalExists(),
            "It is the responsibility of the caller to ensure that storage exists (and is not disposed - which is harder to check at this point so it wasn't done.)");
            
        if( null == core.validEnumerators[ desiredArrayType ] )
        {
            ArrayList storageElems = new ArrayList();
            string externalName = null;

            // Set up IEnumSTATSTG 
            System.Runtime.InteropServices.ComTypes.STATSTG enumElement;
            UInt32 actual;
            IEnumSTATSTG safeIEnumSTATSTG = null;

            core.safeIStorage.EnumElements(
                0, 
                IntPtr.Zero, 
                0, 
                out safeIEnumSTATSTG );
            safeIEnumSTATSTG.Reset();
            safeIEnumSTATSTG.Next( 1, out enumElement, out actual );

            // Loop and get everything
            while( 0 < actual )
            {
                externalName = enumElement.pwcsName;
                
                // In an enumerator, we don't return anything within the reserved
                //  name range.  (First character is \x0001 - \x001F)
                if( CU.IsReservedName(externalName ) )
                {
                    ; // Do nothing for reserved names.
                }
                else if( SafeNativeCompoundFileConstants.STGTY_STORAGE == enumElement.type )
                {
                    if( desiredArrayType == EnumeratorTypes.Everything ||
                        desiredArrayType == EnumeratorTypes.OnlyStorages )
                    {
                        // Add reference to a storage to enumerator array
                        storageElems.Add(new StorageInfo(this, externalName));
                    }
                }
                else if( SafeNativeCompoundFileConstants.STGTY_STREAM == enumElement.type )
                {
                    if( desiredArrayType == EnumeratorTypes.Everything ||
                        desiredArrayType == EnumeratorTypes.OnlyStreams )
                    {
                        // Add reference to a stream to enumerator array
                         storageElems.Add(new StreamInfo(this, externalName));
                    }
                }
                else
                {
                    throw new NotSupportedException(
                        SR.Get(SRID.UnsupportedTypeEncounteredWhenBuildingStgEnum));
                }

                // Move on to the next element
                safeIEnumSTATSTG.Next( 1, out enumElement, out actual );
            }

            core.validEnumerators[ desiredArrayType ] = storageElems;

            // Release IEnumSTATSTG
            ((IDisposable) safeIEnumSTATSTG).Dispose();
            safeIEnumSTATSTG = null;
        }

        Debug.Assert( null != core.validEnumerators[ desiredArrayType ],
            "We failed to ensure the proper array for enumeration" );
    }
}
}


