// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//   The object for manipulating data spaces within the WPP Package.
//
//
//
//
//
//
//

using System;
using System.Collections;
using System.Diagnostics;           // For Debug.Assert
using System.Globalization;
using System.IO;
using System.Reflection;            // For finding transform objects & their constructor
using System.Collections.Generic;

using System.Windows;               // ExceptionStringTable
using MS.Internal.IO.Packaging;
using MS.Internal.IO.Packaging.CompoundFile;
using CU = MS.Internal.IO.Packaging.CompoundFile.ContainerUtilities;
using MS.Internal.WindowsBase;

namespace System.IO.Packaging
{
/// <summary>
/// This class is used to manipulate the data spaces within a specific instance
/// of the Avalon container.  This is how data transform modules are plugged
/// into the container to enable features like data compression and data
/// encryption.
/// </summary>
internal class DataSpaceManager
{
    /***********************************************************************/
    // Constants

    // The header bytes that this version understands and supports
    const int KnownBytesInMapTableHeader = 8; // Two Int32s == 8 bytes
    const int KnownBytesInDataSpaceDefinitionHeader = 8;
    const int KnownBytesInTransformDefinitionHeader = 8;
    const int AllowedExtraDataMaximumSize = 8192; // 8K

    // Names for streams and storages within the container
    const string DataSpaceStorageName = "\x0006DataSpaces";
    const string DataSpaceVersionName = "Version";
    const string DataSpaceMapTableName= "DataSpaceMap";
    const string DataSpaceDefinitionsStorageName = "DataSpaceInfo";
    const string TransformDefinitions = "TransformInfo";
    const string TransformPrimaryInfo = "\x0006Primary";

    // The string used in FormatVersion
    private static readonly string DataSpaceVersionIdentifier = "Microsoft.Container.DataSpaces";

    // Version Writer - 1.0, Reader - 1.0, Updater - 1.0
    private static readonly VersionPair DataSpaceCurrentWriterVersion  = new VersionPair(1 /*major*/, 0 /*minor*/);
    private static readonly VersionPair DataSpaceCurrentReaderVersion  = new VersionPair(1 /*major*/, 0 /*minor*/);
    private static readonly VersionPair DataSpaceCurrentUpdaterVersion = new VersionPair(1 /*major*/, 0 /*minor*/);

    // The version information we read from the file
    private FormatVersion _fileFormatVersion;

    private bool _dirtyFlag;

    /***********************************************************************/
    // Data space manager instance values

    /// <summary>
    /// There is only one data space manager per container instance.  This
    /// points back to "our" reference.
    /// </summary>
    StorageRoot _associatedStorage;

    /// <summary>
    /// Maps container references to data spaces  
    /// 
    /// Keys into this list are CompoundFileReference instances, each 
    /// representing a subset of the container that is encoded with a 
    /// particular data space.  
    /// 
    /// Values are strings, which are data space labels and can be used
    /// as keys into _dataSpaceDefinitions for more details
    /// </summary>
    SortedList _dataSpaceMap;

    /// <summary>
    /// Extra data in the data space mapping table header is preserved
    /// in this byte array.
    /// </summary>
    byte[] _mapTableHeaderPreservation;

    /// <summary>
    /// Maps a data space name to a string array of transform names.
    /// 
    /// Keys into this hash table are strings, each a unique label for
    /// a data space.
    /// 
    /// Values from this hash table are ArrayLists, each an array of
    /// strings.  Each string is a data space label.  This transform
    /// stack is stored in bottom-up order.  The first transform listed
    /// is the first to get the raw bytes from disk.
    /// </summary>
    Hashtable _dataSpaceDefinitions;
 

    /// <summary>
    /// Maps a transform name to an instance of transform handle class
    /// 
    /// Keys into this hash table are strings, each a unique label for
    /// a transform object instance.
    /// 
    /// Values from this hash table are references to the TransformInstance
    /// class defined below, each of which contains information for a 
    /// particular transform instance.
    /// </summary>
    Hashtable _transformDefinitions;

    /// <summary>
    /// When shutting down, we need to flush each open transformed stream in
    /// order to ensure that all encoding data has been propagated through
    /// the transform stack before we shut things down.  Otherwise we may leave
    /// data in a state where it could not be written out because parts of the
    /// transform stack has already been disposed.
    /// </summary>
    ArrayList _transformedStreams;

    /// <summary>
    /// Table of "well-known" -- that is, "built-in" -- transforms. The keys are
    /// the TransformClassName identifier strings for the well-known transforms,
    /// such as encryption and compression. The values are the assembly-qualified
    /// .NET class names of the classes that implement the transforms.
    /// </summary>
    static readonly Hashtable _transformLookupTable;

    /***********************************************************************/
    // Private class for tracking individual transform instances

    private class TransformInstance
    {
        // When we only know the CLR type name
        internal TransformInstance( int classType, string name ) : this(classType, name, null, null, null, null ) {;}

        // When we also have an actual object in memory and its associated
        //  environment object
        internal TransformInstance( 
            int classType,
            string name, 
            IDataTransform instance, 
            TransformEnvironment environment ) : this(classType, name, instance, environment, null, null ) {;}

        // When we know everything to put into a TransformInstance.
        internal TransformInstance( 
            int classType,
            string name, 
            IDataTransform instance, 
            TransformEnvironment environment, 
            Stream primaryStream, 
            StorageInfo storage )
        {
            typeName = name;
            transformReference = instance;
            transformEnvironment = environment;
            transformPrimaryStream = primaryStream;
            transformStorage = storage;
            _classType = classType;
        }

        internal byte[] ExtraData
        {
            get
            {
                return _extraData;
            }

            set
            {
                _extraData = value;
            }
        }

        internal int ClassType
        {
            get
            {
                return _classType;
            }
        }

        /// <summary>
        /// This is the CLR name used to define this transform.  Keep in
        /// mind that this is not necessarily what we retrieve when we
        /// call Type.FullName or Type.AssemblyQualifiedName.  This is
        /// the name that needs to be persisted in the file because it is
        /// what the caller told us to use to find the type.
        /// </summary>
        internal string          typeName;

        /// <summary>
        /// If we have actually created the transform object, we keep
        /// a reference to it here.  This may be null if we haven't
        /// had a need to create the transform object.
        /// </summary>
        internal IDataTransform  transformReference;

        /// <summary>
        /// The instance of TransformEnvironment that we created and
        /// handed off to the transform object to tell us about things.
        /// This can also be null, but only when transformReference is
        /// null.  It is not valid for only one of these two to be null,
        /// either they're both null or they're both non-null.
        /// </summary>
        internal TransformEnvironment transformEnvironment;

        /// <summary>
        /// The stream that is the primary data storage for this instance
        /// of the transform object.
        /// </summary>
        internal Stream transformPrimaryStream;

        /// <summary>
        /// The storage that is available if the transform object requires
        /// more than the primary stream
        /// </summary>
        internal StorageInfo transformStorage;

        private int _classType;
        private byte[] _extraData;
    }

    private class DirtyStateTrackingStream:  Stream
    {
        ////////////////////////////////////
        // Stream section  
        /////////////////////////////////
        public override bool CanRead
        {
            get
            {
                return (_baseStream != null && _baseStream.CanRead);
            }
        }

        public override bool CanSeek
        {
            get
            {
                return (_baseStream != null && _baseStream.CanSeek);
            }
        }

        public override bool CanWrite
        {
            get
            {
                return (_baseStream != null && _baseStream.CanWrite);
            }
        }

        public override long Length
        {
            get
            {
                CheckDisposed();

                return  _baseStream.Length;
            }
        }

        public override long Position
        {
            get
            {
                CheckDisposed();
                
                return _baseStream.Position;
            }
            
            set
            {
                CheckDisposed();

                _baseStream.Position = value;
            }
        }

        public override void SetLength(long newLength)
        {
            CheckDisposed(); 
            
            if (newLength != _baseStream.Length)
            {
                _dirty = true;
            }

            _baseStream.SetLength(newLength);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            CheckDisposed();

            return _baseStream.Seek(offset, origin);
        }        

        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckDisposed();

            return _baseStream.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            CheckDisposed();

            _baseStream.Write(buffer, offset, count);
            _dirty = true;
        }

        public override void Flush()
        {
            CheckDisposed();

            _baseStream.Flush();
        }

        /////////////////////////////
        // Internal Constructor
        /////////////////////////////        
        internal  DirtyStateTrackingStream(Stream baseStream) 
        {
            Debug.Assert(baseStream != null);

            _baseStream = baseStream;
        }

        internal bool DirtyFlag
        {
            get
            {
                return (_baseStream != null && _dirty);
            }
        }

        internal Stream BaseStream
        {
            get
            {
                return _baseStream;
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
        /// <remarks>We implement this because we want a consistent experience (essentially Flush our data) if the user chooses to 
        /// call Dispose() instead of Close().</remarks>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    if (_baseStream != null)
                        _baseStream.Close();
                }
            }
            finally
            {
                _baseStream = null;
                base.Dispose(disposing);
            }
        }

        /////////////////////////////
        // Private Methods
        /////////////////////////////        

        private void CheckDisposed()
        {
            if (_baseStream == null)
            {
                throw new ObjectDisposedException(null, SR.Get(SRID.StreamObjectDisposed));            
            }
        }

        private bool _dirty;
        private Stream _baseStream;
    }

    private struct DataSpaceDefinition
    {
        ArrayList _transformStack;
        Byte[]    _extraData;

        internal DataSpaceDefinition(ArrayList transformStack, Byte[] extraData)
        {
            _transformStack = transformStack;
            _extraData = extraData;
        }

        internal ArrayList TransformStack
        {
            get
            {
                return _transformStack;
            }
        }

        internal Byte[] ExtraData
        {
            get
            {
                return _extraData;
            }
        }
    }

    /***********************************************************************/
    // Constructors

    /// <summary>
    /// Static constructor that initializes the transformLookupTable (which see).
    /// </summary>
    static DataSpaceManager()
    {
        // Transform Identifier: we preserve casing, but do case-insensitive comparison
        _transformLookupTable = new Hashtable(CU.StringCaseInsensitiveComparer);

        _transformLookupTable[RightsManagementEncryptionTransform.ClassTransformIdentifier]
            = "System.IO.Packaging.RightsManagementEncryptionTransform";
        _transformLookupTable[CompressionTransform.ClassTransformIdentifier]
            = "System.IO.Packaging.CompressionTransform";
    }

    /// <summary>
    /// Internally visible constructor
    /// </summary>
    /// <param name="containerInstance">The container instance we're associated with</param>
    internal DataSpaceManager( StorageRoot containerInstance )
    {
        _associatedStorage = containerInstance;

        // Storage under which all data space information is stored.
        StorageInfo dataSpaceStorage = 
            new StorageInfo( _associatedStorage, DataSpaceStorageName );

        // Initialize internal data structures.
        _dataSpaceMap = new SortedList();
        _mapTableHeaderPreservation = Array.Empty<byte>();
        _dataSpaceDefinitions = new Hashtable(CU.StringCaseInsensitiveComparer);
        _transformDefinitions = new Hashtable(CU.StringCaseInsensitiveComparer);
        _transformedStreams = new ArrayList();

        // Check to see if we have any data space information to read
        if (dataSpaceStorage.Exists)
        {
            // Read any existing data space mapping information from the container
            ReadDataSpaceMap();
            ReadDataSpaceDefinitions();
            ReadTransformDefinitions();
        }
        return; 
    }

    /// <summary>
    /// Returns the number of data spaces defined in this manager object
    /// </summary>
    internal int Count
    {
        get
        {
            CheckDisposedStatus();
            return _dataSpaceMap.Count;
        }
    }

    private bool DirtyFlag
    {
        get
        {
            if (_dirtyFlag)     // It is already dirty don't need to check further
                return true;
            
            foreach( string transformDef in _transformDefinitions.Keys )
            {
                TransformInstance transformInstance = GetTransformInstanceOf( transformDef );
                if (((DirtyStateTrackingStream) transformInstance.transformPrimaryStream).DirtyFlag)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Clean up the data space information and flush all data to the container.
    /// </summary>
    public void Dispose()
    {
        CheckDisposedStatus();
        
        // Flush any outstanding data in the transformed streams
        foreach( StreamWithDictionary dataStream in _transformedStreams )
        {
            if (!dataStream.Disposed)
                dataStream.Flush();
        }
        _transformedStreams.Clear();

        // Now that all data have been flushed, shut down the transform
        //  objects.
        foreach( object o in _transformDefinitions.Values )
        {
            IDataTransform idt = ((TransformInstance)o).transformReference;

            if( null != idt &&
                null != (idt as IDisposable))
            {
                ((IDisposable)idt).Dispose();
            }
        }

        // Now write the tables out.
        //  Future idea: Write to temporary storages/streams at first so we can cleanly abort without leaving the file in an inconsistent state

        if (FileAccess.Read != _associatedStorage.OpenAccess && DirtyFlag)
        {
            WriteDataSpaceMap();
            WriteDataSpaceDefinitions();
            WriteTransformDefinitions();
        }

        _dataSpaceMap = null;
        _dataSpaceDefinitions = null;
        _transformDefinitions = null;
            
        return;
    }

    /// <summary>
    /// Removes the container reference from the dataspace map. This is called when the container/substorage is getting deleted from the root storage. [DeleteSubStorage in StorageInfo]
    /// </summary>
    internal void RemoveContainerFromDataSpaceMap(CompoundFileReference target)
    {
       CheckDisposedStatus();
       if (_dataSpaceMap.Contains(target))
       {
            _dataSpaceMap.Remove(target);
            _dirtyFlag = true;
       }
    }

    // Check to see if the dispose method has been called.  If so, throw an
    //  ObjectDisposedException.
    internal void CheckDisposedStatus()
    {
        // First check root
        _associatedStorage.CheckRootDisposedStatus();

        // Check if we've been disposed
        if( null == _dataSpaceMap )
        {
            Debug.Assert( null == _dataSpaceDefinitions, 
                "Having a null data space map and a non-null data space definitions map is an inconsistent state" );
            Debug.Assert( null == _transformDefinitions,
                "Having a null data space map and a non-null transform definition map is an inconsistent state" );

            throw new ObjectDisposedException(null, SR.Get(SRID.DataSpaceManagerDisposed));
        }
    }

    /// <summary>
    /// Define a data space with the given stack of transform objects and
    /// labeled with the given name.  The transform stack is interpreted in
    /// bottom-up order.  (Clear-text transform is last.)
    /// </summary>
    /// <param name="transformStack">Transform stack</param>
    /// <param name="newDataSpaceLabel">New data space label</param>
    internal void DefineDataSpace( string[] transformStack, string newDataSpaceLabel )
    {
        CheckDisposedStatus();
        // Data space must have at least one transform
        if( null == transformStack ||
            0 == transformStack.Length )
            throw new ArgumentException(
                SR.Get(SRID.TransformStackValid));

        // Given label must be a non-empty string
        CU.CheckStringAgainstNullAndEmpty(newDataSpaceLabel, "newDataSpaceLabel");

        // Given label must not be a reserved string
        CU.CheckStringAgainstReservedName(newDataSpaceLabel, "newDataSpaceLabel");
        
        // Given label must not already be in use
        if( DataSpaceIsDefined( newDataSpaceLabel ) )
            throw new ArgumentException(
                SR.Get(SRID.DataSpaceLabelInUse));

        // Given transform array must include labels that have already been defined
        foreach( string transformLabel in transformStack )
        {
            CU.CheckStringAgainstNullAndEmpty( transformLabel, "Transform label" );
            
            if( !TransformLabelIsDefined( transformLabel ) )
                throw new ArgumentException(
                    SR.Get(SRID.TransformLabelUndefined));
        }

        // Passes all inspection, data space definition successful.
        SetDataSpaceDefinition( newDataSpaceLabel, new DataSpaceDefinition(new ArrayList(transformStack), null));
        _dirtyFlag = true;

        return;
    }

    /// <summary>
    /// Internal shortcut to check if data space is defined.  When we start
    /// doing on-demand reads of data space definitions, the "demand" could
    /// be triggered by this.
    /// </summary>
    /// <param name="dataSpaceLabel">Label to check</param>
    /// <returns>True if label is in use</returns>
    internal bool DataSpaceIsDefined( string dataSpaceLabel )
    {
        CU.CheckStringAgainstNullAndEmpty(dataSpaceLabel, "dataSpaceLabel");

        return _dataSpaceDefinitions.Contains( dataSpaceLabel );
    }

    /// <summary>
    ///     Central place to set a data space definition, centralizing the
    /// call to the name manager.
    /// </summary>
    private void SetDataSpaceDefinition( string dataSpaceLabel, DataSpaceDefinition definition )
    {
        _dataSpaceDefinitions[ dataSpaceLabel ] = definition;
    }

    /// <summary>
    ///     Central place to get a data space definition, centralizing the
    /// call to the name manager.
    /// </summary>
    private DataSpaceDefinition GetDataSpaceDefinition( string dataSpaceLabel )
    {
        return ((DataSpaceDefinition) _dataSpaceDefinitions[dataSpaceLabel]);
    }
    /// <summary>
    /// Internal method to retrieve the data space label corresponding to
    /// a container reference.  Returns null if no data space is associated.
    /// </summary>
    /// <param name="target">CompoundFileReference whose data space label is to be retrieved</param>
    /// <returns>Data space label</returns>
    internal string DataSpaceOf( CompoundFileReference target )
    {
        // Can't simply return _dataSpaceMap[target] because I need to cast it
        //  into a string, and if it's null the cast `blows up.
        if( _dataSpaceMap.Contains(target) )
        {
            return (string)_dataSpaceMap[target];
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// This method returns all the transforms that are applied to a particular stream as an 
    /// List of IDataTransform objects.
    /// </summary>
    /// <param name="streamInfo">StreamInfo for the stream whose transforms are requested</param>
    /// <returns>A List of IDataTransform objects that are applied to the stream represented by streamInfo</returns>
    internal List<IDataTransform> GetTransformsForStreamInfo(StreamInfo streamInfo)
    {
        string dataSpaces = this.DataSpaceOf(streamInfo.StreamReference);

        if (dataSpaces == null) // No datas pace is associated with the stream
        {
            return new List<IDataTransform>(0);     // return an empty list
        }

        ArrayList transformsList = this.GetDataSpaceDefinition(dataSpaces).TransformStack;
        List<IDataTransform> dataTransforms = new List<IDataTransform>(transformsList.Count);

        for (int i = 0; i < transformsList.Count; i++)
        {
            dataTransforms.Add(this.GetTransformFromName(transformsList[i] as string));
        }

        return dataTransforms;
    }

    /// <summary>
    /// Define a data space with the given stack of transform objects and
    /// labeled with an auto-generated name
    /// </summary>
    /// <param name="transformStack">Transform stack</param>
    /// <returns>The auto-generated label for this data space</returns>
    internal string DefineDataSpace( string[] transformStack )
    {
        CheckDisposedStatus();
        Int64   timeSeed      = DateTime.Now.ToFileTime();
        string  generatedName = timeSeed.ToString(CultureInfo.InvariantCulture);

        // If there is a name collision, just keep incrementing the number
        while( DataSpaceIsDefined( generatedName ) )
        {
            timeSeed++;
            generatedName = timeSeed.ToString(CultureInfo.InvariantCulture);
        }

        // Submit the definition
        DefineDataSpace( transformStack, generatedName );

        return generatedName;
    }

    /// <summary>
    /// Tranform object is created. We are no longer using reflection to do this. We are supporting limited data transforms.
    /// </summary>
    private IDataTransform InstantiateDataTransformObject(int transformClassType,  string transformClassName, TransformEnvironment transformEnvironment )
    {
        object transformInstance = null;

        if (transformClassType != (int) TransformIdentifierTypes_PredefinedTransformName)
            throw new NotSupportedException(SR.Get(SRID.TransformTypeUnsupported));

        // Transform Identifier: we preserve casing, but do case-insensitive comparison
        if (((IEqualityComparer) CU.StringCaseInsensitiveComparer).Equals(transformClassName,
                RightsManagementEncryptionTransform.ClassTransformIdentifier))
        {
            transformInstance = new RightsManagementEncryptionTransform( transformEnvironment);
        }
        else if (((IEqualityComparer) CU.StringCaseInsensitiveComparer).Equals(transformClassName,
                CompressionTransform.ClassTransformIdentifier))
        {
             transformInstance = new CompressionTransform( transformEnvironment );
        }
        else
        {
            //this transform class is not supported. Need to change this to appropriate error.
            throw new ArgumentException(
                    SR.Get(SRID.TransformLabelUndefined));
        }

        if (null != transformInstance)
        {
            if( !( transformInstance is IDataTransform ) )
                throw new ArgumentException(
                    SR.Get(SRID.TransformObjectImplementIDataTransform));
            return (IDataTransform)transformInstance;
        }

        return null;        
    }

    /// <summary>
    /// Private method to check if a transform label is defined.  When we
    /// start reading transform defintions on-demand, we would probably do it 
    /// here as necessary.
    /// </summary>
    /// <param name="transformLabel">Transform label to check</param>
    /// <returns>True if label is defined in hash table</returns>
    internal bool TransformLabelIsDefined( string transformLabel )
    {
        // Idea: When we start reading transform definitions on-demand,
        //  be able to check this without hitting the disk.
        
        return _transformDefinitions.Contains( transformLabel );
    }

    /// <summary>
    ///     Central place to set a transform definition, centralizing the
    /// call to the name manager.
    /// </summary>
    private void SetTransformDefinition( string transformLabel, TransformInstance definition )
    {
        _transformDefinitions[ transformLabel ] = definition;
    }

    /// <summary>
    /// Private method to get the TransformInstance class representing
    /// a transform instance.
    /// </summary>
    private TransformInstance GetTransformInstanceOf( string transformLabel )
    {
        Debug.Assert( TransformLabelIsDefined( transformLabel ),
            "Data space manager caller failed to verify transform exists before retrieving instance" );

        // Idea: When we start reading transform definitions on-demand,
        //  here is where we find if it's been read in and if not,
        //  hit the disk.
        
        return _transformDefinitions[ transformLabel ] as TransformInstance;
    }

    /// <summary>
    /// Internal method to get a MemoryStream whose contents will be
    /// stored in the "\x0006Primary" data stream after our type identification 
    /// information
    /// </summary>
    /// <param name="transformLabel">Transform Label</param>
    /// <returns>Memory stream object for transform instance primary stream</returns>
    internal Stream GetPrimaryInstanceStreamOf( string transformLabel )
    {
        TransformInstance targetInstance = GetTransformInstanceOf( transformLabel );

        if( null == targetInstance.transformPrimaryStream )
        {
            //build memory stream on the byte[0] , and allow writes only if 
            // FileAccess is Write or ReadWrite
            if (_associatedStorage.OpenAccess == FileAccess.Read)
            {
                targetInstance.transformPrimaryStream =
                    new DirtyStateTrackingStream (new MemoryStream
                            (Array.Empty<byte>(), 
                            false /* Not writable */));
            }
            else
            {
                targetInstance.transformPrimaryStream = new DirtyStateTrackingStream (new MemoryStream());
            }
        }

        return targetInstance.transformPrimaryStream;
    }

    /// <summary>
    /// Internal method to get the StorageInfo where the transform instance
    /// data is stored.  This StorageInfo may not yet exist!
    /// </summary>
    /// <param name="transformLabel">Transform Label</param>
    /// <returns>StorageInfo pointing to transform instance data storage</returns>
    internal StorageInfo GetInstanceDataStorageOf( string transformLabel )
    {
        TransformInstance targetInstance = GetTransformInstanceOf( transformLabel );

        if( null == targetInstance.transformStorage )
        {
            //string name = DataSpaceStorageName + '\\' + TransformDefinitions + '\\' + transformLabel;
            
            //targetInstance.transformStorage  = new StorageInfo(_associatedStorage,name);

            StorageInfo dataSpaceStorage = new StorageInfo( _associatedStorage, DataSpaceStorageName );
            if (!dataSpaceStorage.Exists )
            {
                dataSpaceStorage.Create();
            }
            StorageInfo transformDefinition = new StorageInfo( dataSpaceStorage, TransformDefinitions );
            if (!transformDefinition.Exists)
            {
                transformDefinition.Create();
            }
            targetInstance.transformStorage  = new StorageInfo(transformDefinition,transformLabel);
        }
        return targetInstance.transformStorage;
    }

    /// <summary>
    /// Internal method to get the transform object corresponding to the specified
    /// transform instance label. The transform object is created if it does not exist.
    /// </summary>
    /// <param name="transformLabel">
    /// String that identifies the transform instance.
    /// </param>
    /// <returns>
    /// An IDataTransform interface pointer to the transform object identified by
    /// <paramref name="transformLabel"/>, or null if there is no such transform.
    /// </returns>
    internal IDataTransform GetTransformFromName(string transformLabel)
    {
        TransformInstance transformInstance = _transformDefinitions[transformLabel] as TransformInstance;

        if (transformInstance == null)
        {
            //
            // There is no transform instance with the specified name.
            //
            return null;
        }

        IDataTransform transformObject = transformInstance.transformReference;
        if (transformObject == null)
        {
            //
            // There is a transform instance with the specified name, but its transform
            // object has not yet been created. Create it now. This code is modeled on the
            // code in DefineTransform.
            //
            TransformEnvironment transformEnvironment = new TransformEnvironment(this, transformLabel);

            // Create the transform object.
            transformObject = InstantiateDataTransformObject(
                                    transformInstance.ClassType,
                                    transformInstance.typeName,
                                    transformEnvironment);
            transformInstance.transformReference = transformObject;
        }

        return transformObject;
    }

    /// <summary>
    /// Define a data transform object with the given object identification and
    /// labeled with the given name.
    /// </summary>
    /// <param name="transformClassName">Transform identification string</param>
    /// <param name="newTransformLabel">Label to use for new transform</param>
    internal void DefineTransform(string transformClassName, string newTransformLabel )
    {
        CheckDisposedStatus();

        // Check to see if transform name is obviously invalid
        CU.CheckStringAgainstNullAndEmpty( transformClassName, "Transform identifier name" );

        // Check to see if transform name is valid
        CU.CheckStringAgainstNullAndEmpty( newTransformLabel, "Transform label" );

        // Given transform name must not be a reserved string
        CU.CheckStringAgainstReservedName( newTransformLabel, "Transform label" );
        
        // Can't re-use an existing transform name
        if( TransformLabelIsDefined( newTransformLabel ) )
            throw new ArgumentException(
                SR.Get(SRID.TransformLabelInUse));

        // Create class the transform object will use to communicate to us
        TransformEnvironment transformEnvironment = new TransformEnvironment( this, newTransformLabel );

        // Create a TransformInstance object to represent this transform instance.
        TransformInstance newTransform = new TransformInstance(
            TransformIdentifierTypes_PredefinedTransformName,
            transformClassName,
            null,
            transformEnvironment );

        SetTransformDefinition( newTransformLabel, newTransform );

        // Create the transform object
        IDataTransform transformObject = InstantiateDataTransformObject(
                                                    TransformIdentifierTypes_PredefinedTransformName,
                                                    transformClassName,
                                                    transformEnvironment );
        newTransform.transformReference = transformObject;

        // If transform is not ready out-of-the-box, do an initialization run.
        //  Note: Transform is not required to be "ready" after this.  This is
        //  done for those transforms that need initialization work up-front.
        if( ! transformObject.IsReady )
        {
            CallTransformInitializers( 
                new TransformInitializationEventArgs(
                    transformObject,
                    null,
                    null,
                    newTransformLabel) 
                );
        }

        _dirtyFlag = true;

        return;
    }

    /// <summary>
    /// Define a data transform object with the given object identification and
    /// labeled with an auto-generated name.
    /// </summary>
    /// <param name="transformClassName">Transform identification string</param>
    /// <returns>The auto-generated label for this transform</returns>
    internal string DefineTransform( string transformClassName )
    {
        CheckDisposedStatus();
        Int64   timeSeed      = DateTime.Now.ToFileTime();
        string generatedName = timeSeed.ToString(CultureInfo.InvariantCulture);

        // If there is a name collision, just keep incrementing the number
        while( TransformLabelIsDefined( generatedName ) )
        {
            timeSeed++;
            generatedName = timeSeed.ToString(CultureInfo.InvariantCulture);
        }

        // Submit the definition
        DefineTransform( transformClassName, generatedName );

        return generatedName;
    }

    //+----------------------------------------------------------------------
    //  Transform initialization event/delegate/etc.
    
    /// <summary>
    ///     Delegate method for initializing transforms
    /// </summary>
    internal delegate void TransformInitializeEventHandler(
        object sender,
        TransformInitializationEventArgs e );
    
    /// <summary>
    ///     Transform initialization event
    /// </summary>
    internal event TransformInitializeEventHandler OnTransformInitialization;

    /// <summary>
    /// Internal shortcut to kick off all the initializers
    /// </summary>
    /// <param name="initArguments">Arguments for the initializers</param>
    internal void CallTransformInitializers( TransformInitializationEventArgs initArguments )
    {
        if( null != OnTransformInitialization )
            OnTransformInitialization( this, initArguments );
    }

    /// <summary>
    /// Reads a data space map from the associated container, if such a thing
    /// is written to the file.
    /// </summary>
    void ReadDataSpaceMap()
    {    
        // See if there's even a data spaces storage
        StorageInfo dataSpaceStorage = 
            new StorageInfo( _associatedStorage, DataSpaceStorageName );
        StreamInfo dataSpaceMapStreamInfo = 
            new StreamInfo( dataSpaceStorage, DataSpaceMapTableName );

        if( dataSpaceStorage.StreamExists(DataSpaceMapTableName) )
        {
            // There is an existing data space mapping table to read.

            // Read the versioning information
            ReadDataSpaceVersionInformation(dataSpaceStorage);      
     
            // Check if its the correct version for reading
            ThrowIfIncorrectReaderVersion();

            // Read the data space mapping table
            using(Stream dataSpaceMapStream = dataSpaceMapStreamInfo.GetStream(FileMode.Open))
            {
                using(BinaryReader dataSpaceMapReader = 
                    new BinaryReader( dataSpaceMapStream, System.Text.Encoding.Unicode ))
                {
                    int headerLength = dataSpaceMapReader.ReadInt32();
                    int entryCount = dataSpaceMapReader.ReadInt32();

                    if (headerLength < KnownBytesInMapTableHeader || entryCount < 0)
                        throw new FileFormatException(SR.Get(SRID.CorruptedData));

                    int extraDataSize = headerLength - KnownBytesInMapTableHeader;

                    if( 0 < extraDataSize )
                    {
                        if (extraDataSize > AllowedExtraDataMaximumSize)
                            throw new FileFormatException(SR.Get(SRID.CorruptedData));

                        _mapTableHeaderPreservation = dataSpaceMapReader.ReadBytes(extraDataSize);

                        if (_mapTableHeaderPreservation.Length != extraDataSize)
                            throw new FileFormatException(SR.Get(SRID.CorruptedData));
                    }

                    _dataSpaceMap.Capacity = entryCount;

                    int entryLength;
                    int bytesRead;
                    int totalBytesRead;

                    for( int i = 0; i < entryCount; i++ )
                    {
                        entryLength = dataSpaceMapReader.ReadInt32();

                        if (entryLength < 0)
                            throw new FileFormatException(SR.Get(SRID.CorruptedData));

                        totalBytesRead = 4; // entryLength
                        // Read the container reference entry
                        CompoundFileReference entryRef = 
                            CompoundFileReference.Load( dataSpaceMapReader, out bytesRead );
                        checked { totalBytesRead += bytesRead; }

                        // Read data space string and add to data space mapping table
                        string label = CU.ReadByteLengthPrefixedDWordPaddedUnicodeString(dataSpaceMapReader, out bytesRead);
                        checked { totalBytesRead += bytesRead; }

                        _dataSpaceMap[entryRef] = label;

                        // Verify entryLength against what was actually read:
                        if (entryLength != totalBytesRead)
                        {
                            throw new IOException(SR.Get(SRID.DataSpaceMapEntryInvalid));
                        }
                    }
                }                    
            }
        }
    }

    /// <summary>
    /// Write the data space mapping table to underlying storage.
    /// </summary>
    void WriteDataSpaceMap()
    {
        ThrowIfIncorrectUpdaterVersion();

        StorageInfo dataSpaceStorage = 
            new StorageInfo( _associatedStorage, DataSpaceStorageName );
        StreamInfo dataSpaceMapStreamInfo = 
            new StreamInfo ( dataSpaceStorage, DataSpaceMapTableName );

        if( 0 < _dataSpaceMap.Count )
        {
            // Write versioning information
            StreamInfo versionStreamInfo = null;
            if (dataSpaceStorage.StreamExists( DataSpaceVersionName ) )
                versionStreamInfo = dataSpaceStorage.GetStreamInfo( DataSpaceVersionName );
            else
                versionStreamInfo = dataSpaceStorage.CreateStream( DataSpaceVersionName );
            Stream versionStream = versionStreamInfo.GetStream();
            _fileFormatVersion.SaveToStream(versionStream);
            versionStream.Close();

            // Create stream for write, overwrite any existing
            using(Stream dataSpaceMapStream = dataSpaceMapStreamInfo.GetStream(FileMode.Create))
            {
                using(BinaryWriter dataSpaceMapWriter =
                    new BinaryWriter( dataSpaceMapStream, System.Text.Encoding.Unicode ))
                {
                    // Write header 

                    // header length = our known size + preserved array size
                    dataSpaceMapWriter.Write( 
                        checked ((Int32) (KnownBytesInMapTableHeader + _mapTableHeaderPreservation.Length)));
                    // number of entries
                    dataSpaceMapWriter.Write(
                        _dataSpaceMap.Count );
                    // anything else we've preserved
                    dataSpaceMapWriter.Write(
                        _mapTableHeaderPreservation );

                    // Loop to write entries
                    foreach( CompoundFileReference o in _dataSpaceMap.Keys )
                    {
                        // determine the entry length
                        string label = (string)_dataSpaceMap[o];
                        int entryLength = CompoundFileReference.Save(o, null);

                        checked { entryLength += CU.WriteByteLengthPrefixedDWordPaddedUnicodeString(null, label); }

                        // length of entryLength itself
                        checked { entryLength += 4; }

                        // write out the entry length
                        dataSpaceMapWriter.Write((Int32) entryLength);

                        // Write out reference
                        CompoundFileReference.Save( o, dataSpaceMapWriter);

                        // Write out dataspace label
                        CU.WriteByteLengthPrefixedDWordPaddedUnicodeString(
                            dataSpaceMapWriter, label);
                    }
                }
            }
        }
        else
        {
            // data space map is empty, remove existing stream if there.
            if ( dataSpaceStorage.StreamExists( DataSpaceMapTableName ) )
                dataSpaceStorage.DeleteStream( DataSpaceMapTableName );
        }
    }

    /// <summary>
    /// Read all data space definitions in one chunk.  To be replaced
    /// with on-demand reading mechanism.
    /// </summary>
    void ReadDataSpaceDefinitions()
    {
        ThrowIfIncorrectReaderVersion();

        StorageInfo dataSpaceStorage = 
            new StorageInfo( _associatedStorage, DataSpaceStorageName );
        StorageInfo dataSpaceDefinitionsStorage =
            new StorageInfo( dataSpaceStorage, DataSpaceDefinitionsStorageName );

        if( dataSpaceDefinitionsStorage.Exists )
        {
            // Fill in the Data Space Definitions hash table
            foreach( StreamInfo definitionStreamInfo in dataSpaceDefinitionsStorage.GetStreams())
            {
                // Open up the stream for this data space definition
                using(Stream definitionStream = definitionStreamInfo.GetStream(FileMode.Open))
                {
                    using(BinaryReader definitionReader = new BinaryReader( definitionStream, System.Text.Encoding.Unicode ))
                    {
                        // Read data space definition stream
                        int headerLength = definitionReader.ReadInt32();
                        int transformCount = definitionReader.ReadInt32();

                        if (headerLength < KnownBytesInDataSpaceDefinitionHeader || transformCount < 0)
                            throw new FileFormatException(SR.Get(SRID.CorruptedData));

                        ArrayList transformLabels = new ArrayList(transformCount);
                        byte[] extraData = null;
                        int extraDataSize = headerLength - KnownBytesInDataSpaceDefinitionHeader;

                        if (extraDataSize > AllowedExtraDataMaximumSize)
                            throw new FileFormatException(SR.Get(SRID.CorruptedData));

                        if (extraDataSize > 0)
                        {
                            extraData = definitionReader.ReadBytes(extraDataSize);

                            if (extraData.Length != extraDataSize)
                                throw new FileFormatException(SR.Get(SRID.CorruptedData));
                        }

                        // Read the array of strings that make up the transform stack
                        for( int i = 0; i < transformCount; i++ )
                        {
                            transformLabels.Add(
                                CU.ReadByteLengthPrefixedDWordPaddedUnicodeString( definitionReader ) );
                        }

                        // Add data space definition to table
                        SetDataSpaceDefinition( definitionStreamInfo.Name, new DataSpaceDefinition(transformLabels, extraData));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Write all data space definitions to underlying storage in one chunk.
    /// </summary>
    /// 
    // Idea: Optimize and write only those dataspaces that have changed.
    void WriteDataSpaceDefinitions()
    {
        ThrowIfIncorrectUpdaterVersion();

        StorageInfo dataSpaceStorage = 
            new StorageInfo( _associatedStorage, DataSpaceStorageName );

        // BUGBUG: Any data spaces that have been undefined would still stick
        //  around in the underlying file.  Not yet a problem since data spaces
        //  can't yet be undefined.

        // Write out data space definitions
        if( 0 < _dataSpaceDefinitions.Count )
        {
            StorageInfo dataSpaceDefinitionsStorage =
                new StorageInfo(dataSpaceStorage, DataSpaceDefinitionsStorageName);
            dataSpaceDefinitionsStorage.Create();

            foreach( string name in _dataSpaceDefinitions.Keys )
            {
                StreamInfo singleDefinitionInfo =
                    new StreamInfo(dataSpaceDefinitionsStorage,name);

                using(Stream singleDefinition = singleDefinitionInfo.GetStream())
                {
                    using(BinaryWriter definitionWriter =new BinaryWriter( singleDefinition, System.Text.Encoding.Unicode ))
                    {
                        DataSpaceDefinition definition = (DataSpaceDefinition) _dataSpaceDefinitions[name];

                        int headerSize = KnownBytesInDataSpaceDefinitionHeader;

                        if (definition.ExtraData != null)
                        {
                            checked { headerSize += definition.ExtraData.Length; }
                        }
                        definitionWriter.Write( headerSize );
                        definitionWriter.Write( definition.TransformStack.Count );
                        if (definition.ExtraData != null)
                        {
                            definitionWriter.Write(definition.ExtraData);
                        }

                        foreach( object transformLabel in definition.TransformStack)
                        {
                            CU.WriteByteLengthPrefixedDWordPaddedUnicodeString( 
                                definitionWriter, (string)transformLabel);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Read all transform definitions in one chunk
    /// </summary>
    // Idea: Replace with on-demand transform definition reading system

    void ReadTransformDefinitions()
    {
        ThrowIfIncorrectReaderVersion();

        StorageInfo dataSpaceStorage = 
            new StorageInfo( _associatedStorage, DataSpaceStorageName );
        StorageInfo transformDefinitionsStorage =
            new StorageInfo( dataSpaceStorage, TransformDefinitions );

        if( transformDefinitionsStorage.Exists )
        {
            // Read transform definitions from file
            foreach( StorageInfo transformStorage in transformDefinitionsStorage.GetSubStorages() )
            {
                // Read from primary stream
                StreamInfo transformPrimary = new StreamInfo(
                    transformStorage, TransformPrimaryInfo );

                using(Stream transformDefinition = transformPrimary.GetStream(FileMode.Open))
                {
                    using(BinaryReader definitionReader = new BinaryReader( transformDefinition, System.Text.Encoding.Unicode ))
                    {
                        int headerLength = definitionReader.ReadInt32(); // We don't actually do anything with HeaderLen at the moment
                        int transformType = definitionReader.ReadInt32();

                        if (headerLength < 0)
                            throw new FileFormatException(SR.Get(SRID.CorruptedData));

                        // Create a TransformInstance class using name from file
                        TransformInstance transformInstance =
                            new TransformInstance(transformType, CU.ReadByteLengthPrefixedDWordPaddedUnicodeString( definitionReader ) );

                        
                        int extraDataSize = checked ((int) (headerLength - transformDefinition.Position));

                        if (extraDataSize < 0)
                            throw new FileFormatException(SR.Get(SRID.CorruptedData));

                        if (extraDataSize > 0)
                        {
                            if (extraDataSize > AllowedExtraDataMaximumSize)
                                throw new FileFormatException(SR.Get(SRID.CorruptedData));

                            // Preserve the fields we don't know about.
                            byte[] extraData = definitionReader.ReadBytes(extraDataSize);

                            if (extraData.Length != extraDataSize)
                                throw new FileFormatException(SR.Get(SRID.CorruptedData));

                            transformInstance.ExtraData = extraData;
                        }

                        if( transformDefinition.Length > transformDefinition.Position )
                        {
                            // We have additional data in the primary instance data stream
                            int instanceDataSize = checked ((int)(transformDefinition.Length - transformDefinition.Position));
                            byte[] instanceData = new byte[instanceDataSize];
                            PackagingUtilities.ReliableRead(transformDefinition, instanceData, 0, instanceDataSize);

                            //build memory stream on the byte[] , and allow writes only if 
                            // FileAccess is Write or ReadWrite
                            MemoryStream instanceDataStream;
                            if (_associatedStorage.OpenAccess == FileAccess.Read)
                            {
                                //  NOTE: Building MemoryStream directly on top of
                                //  instanceData byte array because we want it to be
                                //  NOT resizable and NOT writable.                    
                                instanceDataStream = new MemoryStream(instanceData, false /* Not writable */);
                            }
                            else
                            {
                                // Copy additional data into a memory stream
                                //  NOTE: Not building MemoryStream directly on top of
                                //  instanceData byte array because we want it to be
                                //  resizable.                    
                                instanceDataStream = new MemoryStream();
                                instanceDataStream.Write( instanceData, 0, instanceDataSize );
                            }
                            instanceDataStream.Seek( 0, SeekOrigin.Begin );

                            // Dirty state should be tracked after the original data is read in from the disk into the memory stream
                            transformInstance.transformPrimaryStream = new DirtyStateTrackingStream(instanceDataStream);
                        }

                        transformInstance.transformStorage = transformStorage;

                        SetTransformDefinition( transformStorage.Name, transformInstance );
                    }
                }
            }
        }
    }

    /// <summary>
    /// Write out all transform definitions all at once
    /// </summary>
    /// 
    // Idea: Replace with system that writes only "dirty" transform definitions

    void WriteTransformDefinitions()
    {
        ThrowIfIncorrectUpdaterVersion();

        StorageInfo dataSpaceStorage = 
            new StorageInfo( _associatedStorage, DataSpaceStorageName );
        StorageInfo transformDefinitionsStorage =
            new StorageInfo( dataSpaceStorage, TransformDefinitions );

        // BUGBUG: Any transforms that have been undefined would still stick
        //  around in the underlying file, not yet a problem because transform
        //  un-definition is not yet implemented.

        if( 0 < _transformDefinitions.Count )
        {
            foreach( string transformDef in _transformDefinitions.Keys )
            {
                // 'transformDef' is the normalized label.  We need to dig a
                //  bit to retrieve the original transform label.
                string transformLabel = null;
                TransformInstance transformInstance = GetTransformInstanceOf( transformDef );
                Debug.Assert( transformInstance != null, "A transform instance should be available if its name is in the transformDefinitions hashtable");
                
                if( transformInstance.transformEnvironment != null )
                {
                    // We have a transform environment object - it has the transform label.
                    transformLabel = transformInstance.transformEnvironment.TransformLabel;
                }
                else
                {
                    // We don't have a transform environment object - we'll need to do a
                    //  more expensive reverse-lookup with the name manager.
                    transformLabel = transformDef;
                }

                // Now use transformLabel to create the storage.
                StorageInfo singleTransformStorage =
                    new StorageInfo( transformDefinitionsStorage, transformLabel );
                StreamInfo transformPrimaryInfo =
                    new StreamInfo( singleTransformStorage, TransformPrimaryInfo );

                using(Stream transformPrimary = transformPrimaryInfo.GetStream())
                {
                    using(BinaryWriter transformWriter = new BinaryWriter( transformPrimary, System.Text.Encoding.Unicode ))
                    {
                        // Header length size = Known (the number itself + identifier) +
                        //  to be calculated (length of type name)
                        int headerLength = checked (KnownBytesInTransformDefinitionHeader +
                                                CU.WriteByteLengthPrefixedDWordPaddedUnicodeString( null, transformInstance.typeName));

                        if (transformInstance.ExtraData != null)
                        {
                            Debug.Assert(transformInstance.ExtraData.Length > 0);

                            checked { headerLength += transformInstance.ExtraData.Length; }
                        }

                        transformWriter.Write(headerLength);
                        
                        transformWriter.Write((int)TransformIdentifierTypes_PredefinedTransformName);
                        CU.WriteByteLengthPrefixedDWordPaddedUnicodeString( 
                            transformWriter, transformInstance.typeName);

                        // Write out the preserved unknown data if there are some
                        if (transformInstance.ExtraData != null)
                        {
                            transformWriter.Write(transformInstance.ExtraData);
                        }

                        if( null != transformInstance.transformPrimaryStream )
                        {
                            byte[] memoryBuffer = ((MemoryStream) ((DirtyStateTrackingStream) transformInstance.transformPrimaryStream).BaseStream).GetBuffer();
                            transformPrimary.Write( memoryBuffer, 0, memoryBuffer.Length );
                        }
                    }
                }
            }
        }
        else // No transform definitions
        {
            if ( transformDefinitionsStorage.Exists)
            {
                dataSpaceStorage.Delete(true, TransformDefinitions);
                //transformDefinitionStorage.Delete(true);
            }
        }
    }

    /// <summary>
    /// Internal method to create an entry in the data space mapping table
    /// </summary>
    /// <param name="containerReference">Package reference describing the scope of the data space</param>
    /// <param name="label">Label of the data space definition</param>
    internal void CreateDataSpaceMapping( CompoundFileReference containerReference, string label )
    {
        Debug.Assert( DataSpaceIsDefined( label ),
            "Internal method illegally defining a data space reference on a data space that has not yet been defined");
        _dataSpaceMap[containerReference] = label;
        _dirtyFlag = true;
    }

    /// <summary>
    /// Internal method to create the actual transform stack of streams which
    /// will handle the encoding and decoding
    /// </summary>
    /// <param name="containerReference">Package reference for the data space stream</param>
    /// <param name="rawStream">Stream with raw bytes on disk</param>
    /// <returns>Stream with clear text</returns>
    internal Stream CreateDataSpaceStream( CompoundFileStreamReference containerReference , Stream rawStream )
    {
        Stream outputStream = rawStream;
        // RightsManagementEncryptionTransform and CompressionTransform do not use transformContext
        IDictionary transformContext = new Hashtable(); // Transform context object

        // See which data space we're creating this stream in
        string dataSpaceLabel = _dataSpaceMap[containerReference] as string;
        Debug.Assert( null != dataSpaceLabel,
            "Internal caller has asked to create a data space stream but the reference is not associated with a data space" );

        // Retrieve the transform object stack
        ArrayList transformStack = GetDataSpaceDefinition(dataSpaceLabel).TransformStack;
        Debug.Assert( null != transformStack,
            "Internal inconsistency - data space name does not have a transform stack definition" );

        // Iterate the initialization process for each transform on the stack
        foreach( string transformLabel in transformStack )
        {
            // Get information on this layer of the transform stack
            TransformInstance transformLayer = GetTransformInstanceOf( transformLabel );
            Debug.Assert( null != transformLayer, "Data space definition included an undefined transform" );

            // If we haven't gotten around to creating the transform object, go do it.
            if( null == transformLayer.transformReference )
            {
                transformLayer.transformEnvironment = new TransformEnvironment( this, transformLabel );

                transformLayer.transformReference = InstantiateDataTransformObject(
                                                        transformLayer.ClassType,
                                                        transformLayer.typeName,
                                                        transformLayer.transformEnvironment );
            }
            Debug.Assert( null != transformLayer.transformReference,
                "Failed to have a transform instance going in" );
            IDataTransform transformObject = transformLayer.transformReference;

            // If transform is not ready, call initializers to make it ready.
            if( ! transformObject.IsReady )
            {
                CallTransformInitializers( 
                    new TransformInitializationEventArgs(
                        transformObject,
                        dataSpaceLabel,
                        containerReference.FullName,
                        transformLabel)
                    );

                if( ! transformObject.IsReady ) // If STILL not ready, nobody could make it "ready".
                    throw new InvalidOperationException(
                        SR.Get(SRID.TransformObjectInitFailed));
            }
            // Everything is setup, get a transformed stream
            outputStream = transformObject.GetTransformedStream( outputStream, transformContext );
        }

        outputStream = new BufferedStream( outputStream ); // Add buffering layer

        outputStream = new StreamWithDictionary( outputStream, transformContext );

        _transformedStreams.Add( outputStream ); // Remember this for later use

        return outputStream;
    }

    /// <summary>
    /// When naming a transform object, the string being passed in can be
    /// interpreted in one of several ways.  This enumerated type is used
    /// to specify the semantics of the identification string.
    /// 
    /// The transform identification string is key into a table of
    ///  well-known transform definitions.
    /// </summary>
    internal const int TransformIdentifierTypes_PredefinedTransformName = 1;

    #region Version Methods

    /// <summary>
    /// Read the version information that specifies the minimum versions of the 
    /// DataSpaceManager software that can read, write, or update the data space 
    /// information in this file.
    /// </summary>
    /// <param name="dataSpaceStorage"></param>
    /// <exception cref="FileFormatException">
    /// If the format version information in the stream is corrupt.
    /// </exception>
    private void ReadDataSpaceVersionInformation(StorageInfo dataSpaceStorage)
    {      
        if (_fileFormatVersion == null)
        {
            if (dataSpaceStorage.StreamExists( DataSpaceVersionName ))
            {
                StreamInfo versionStreamInfo = dataSpaceStorage.GetStreamInfo( DataSpaceVersionName );
                using (Stream versionStream = versionStreamInfo.GetStream(FileMode.Open))
                {
                    _fileFormatVersion = FormatVersion.LoadFromStream(versionStream);
                                        
                    // Transform Identifier: we preserve casing, but do case-insensitive comparison
                    //Case-insensitive comparison. As per recommendations, we convert both strings
                    //to Upper case and then compare with StringComparison.Ordinal
                    if (!((IEqualityComparer) CU.StringCaseInsensitiveComparer).Equals(_fileFormatVersion.FeatureIdentifier,
                                       DataSpaceVersionIdentifier))
                    {
                        throw new FileFormatException(
                                            SR.Get(SRID.InvalidTransformFeatureName,
                                            _fileFormatVersion.FeatureIdentifier,
                                            DataSpaceVersionIdentifier));                       
                    }
                    // If we ever write this version number out again, we will want to record
                    // the fact that it was done by the current version of the Dataspace software.
                    _fileFormatVersion.WriterVersion = DataSpaceCurrentWriterVersion;
                }
            }
        }        
    }

    /// <summary>
    /// If the DataSpace version information was not present in the file, we initialize it with the
    /// values for the current DataSpace software.
    /// If the information was present we should have already read it as a part of the ReadDataSpaceMap.
    /// </summary>
    private void EnsureDataSpaceVersionInformation()
    {
        if (_fileFormatVersion == null)
        {
            _fileFormatVersion = new FormatVersion(
                                        DataSpaceVersionIdentifier,
                                        DataSpaceCurrentWriterVersion,
                                        DataSpaceCurrentReaderVersion,
                                        DataSpaceCurrentUpdaterVersion
                                     );
        }
    }
      
    /// <summary>
    /// Verify that the current version of this class can read the DataSpace information in
    /// this file.
    /// </summary>
    /// <exception cref="FileFormatException">
    /// If the current version of this class cannot read the DataSpace information in this file.
    /// </exception>
    private void ThrowIfIncorrectReaderVersion()
    {
        EnsureDataSpaceVersionInformation();
      
        if (!_fileFormatVersion.IsReadableBy(DataSpaceCurrentReaderVersion))
        {
            throw new FileFormatException(
                            SR.Get(
                                SRID.ReaderVersionError,
                                _fileFormatVersion.ReaderVersion,
                                DataSpaceCurrentReaderVersion
                                )
                            );
        }
    }

    /// <summary>
    /// Verify that the current version of this class can update the DataSpace information in
    /// this file.
    /// </summary>
    /// <exception cref="FileFormatException">
    /// If the current version of this class cannot update the DataSpace information in this file,
    /// or if the header information in the stream is corrupt.
    /// </exception>
    private void ThrowIfIncorrectUpdaterVersion()
    {
        EnsureDataSpaceVersionInformation();

        if (!_fileFormatVersion.IsUpdatableBy(DataSpaceCurrentUpdaterVersion))
        {
            throw new FileFormatException(
                            SR.Get(
                                SRID.UpdaterVersionError,
                                _fileFormatVersion.UpdaterVersion,
                                DataSpaceCurrentUpdaterVersion
                                )
                            );
        }
    }

    #endregion Version Methods
}

/// <summary>
/// Interface to be implemented by all data transform objects
/// </summary>
internal interface IDataTransform
{
    /// <summary>
    /// Whether this transform is ready to perform.  If false, it needs
    /// further initialization.
    /// </summary>
    bool IsReady { get; }

    /// <summary>
    /// Whether this transform is in a stable state
    /// </summary>
    bool FixedSettings { get; }

    /// <summary>
    /// Data transform identifier object
    /// </summary>
    object TransformIdentifier { get; }

    /// <summary>
    /// Given a stream for storing the encoded data, return a stream for
    /// manipulating the "cleartext" data.
    /// </summary>
    Stream GetTransformedStream( Stream encodedDataStream, IDictionary transformContext );
}

/// <summary>
/// Internal class for passing arguments into event handlers
/// </summary>
internal class TransformInitializationEventArgs : EventArgs
{
    IDataTransform  dataInstance;
    string          dataSpaceLabel;
    string          streamPath;
    string          transformLabel;

    internal TransformInitializationEventArgs(
        IDataTransform instance,
        string dataSpaceInstanceLabel,
        string transformedStreamPath,
        string transformInstanceLabel )
    {
        dataInstance = instance;
        dataSpaceLabel = dataSpaceInstanceLabel;
        streamPath = transformedStreamPath;
        transformLabel = transformInstanceLabel;
    }
    /// <summary>
    /// Reference to the data object that requires initialization
    /// </summary>
    internal IDataTransform DataTransform
    {
        get
        {
            return dataInstance;
        }
    }

    /// <summary>
    /// Label for the data space whose initialization this is a part of
    /// </summary>
    internal string DataSpaceLabel
    {
        get
        {
            return dataSpaceLabel;
        }
    }

    /// <summary>
    /// The path to the stream whose use initiated this process
    /// </summary>
    internal string Path
    {
        get
        {
            return streamPath;
        }
    }

    /// <summary>
    /// Label for the transform object instance being initialized
    /// </summary>
    internal string TransformInstanceLabel
    {
        get
        {
            return transformLabel;
        }
    }
}

/// <summary>
/// An instance of this class is given to each transform object as a 
/// means for the transform object to interact with the environment 
/// provided by the data space manager.  It is not mandatory for a 
/// transform object to keep a reference on the given TransformEnvironment 
/// object  it may choose to discard it if there is no need to interact 
/// with the transform environment.
/// </summary>
internal  class TransformEnvironment
{
    DataSpaceManager transformHost;
    string  transformLabel;

    /// <summary>
    /// This object is only created internally by the data space manager.
    /// </summary>
    /// <param name="host">The data space manager who created this class</param>
    /// <param name="instanceLabel">Text label for this transform instance</param>
    internal TransformEnvironment( DataSpaceManager host, string instanceLabel )
    {
        transformHost = host;
        transformLabel = instanceLabel;
    }

    /// <summary>
    /// Whether this transform demands that all instance data of other
    /// transform above it in the transform stack be transformed too
    /// </summary>
    internal bool RequireOtherInstanceData
    {
        get
        {
            return false;
        }
        set
        {
            transformHost.CheckDisposedStatus();
            throw new NotSupportedException(
                SR.Get(SRID.NYIDefault));
        }
    }

    /// <summary>
    /// Whether this transform demands that its own instance data be
    /// free from other data transformation
    /// </summary>
    internal bool RequireInstanceDataUnaltered
    {
        get
        {
            return false;
        }
        set
        {
            transformHost.CheckDisposedStatus();
            throw new NotSupportedException(
                SR.Get(SRID.NYIDefault));
        }
    }

    /// <summary>
    /// Whether this transform requests that its own instance data be
    /// processed by other transforms below it in the stack
    /// </summary>
    internal bool DefaultInstanceDataTransform
    {
        get
        {
            return false;
        }
        set
        {
            transformHost.CheckDisposedStatus();
            throw new NotSupportedException(
                SR.Get(SRID.NYIDefault));
        }
    }

    /// <summary>
    ///     In the transform definition hashtable, we only have the normalized
    /// label as the key.  By digging our way to this property via the hashtable
    /// value, we can recover the original label.
    /// </summary>
    internal string TransformLabel
    {
        get
        {
            return transformLabel;
        }
    }

    /// <summary>
    /// Returns the stream specific to this instance of the transform
    /// object, in which it can store any instance data it needs
    /// </summary>
    /// <returns>System.IO.Stream object for storing instance data</returns>
    internal Stream GetPrimaryInstanceData()
    {
        transformHost.CheckDisposedStatus();
        return transformHost.GetPrimaryInstanceStreamOf( transformLabel );
    }

    /// <summary>
    /// When the primary instance data stream is insufficient, this returns
    /// a storage in which to store multiple streams that comprise the instance
    /// data that this transform object wants to store.
    /// </summary>
    /// <returns>StorageInfo pointing to the storage that this transform is free to use</returns>
    internal StorageInfo GetInstanceDataStorage()
    {
        transformHost.CheckDisposedStatus();

        StorageInfo storageInfo = transformHost.GetInstanceDataStorageOf( transformLabel );
        
        if (! storageInfo.Exists)
        {
            storageInfo.Create();
        }
        
        return storageInfo;
    }
}
}
