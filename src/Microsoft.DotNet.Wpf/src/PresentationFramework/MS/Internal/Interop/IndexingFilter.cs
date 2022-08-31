// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//   IFilter interop definitions.
//
//   These structures and constants are managed equivalents to COM structures
//   constants required by the IFilter interface.
//


using System;
using System.Runtime.InteropServices;
using MS.Internal.IO.Packaging;

namespace MS.Internal.Interop
{
    #region COM structs and enums

    /// <summary>
    /// The IFilter error codes as defined in file filterr.h of the Windows SDK.
    /// </summary>
    internal enum FilterErrorCode : int
    {
        /// <summary>
        /// No more chunks of text available in object.
        /// </summary>
        FILTER_E_END_OF_CHUNKS = unchecked((int)0x80041700),

        /// <summary>
        /// No more text available in chunk.
        /// </summary>
        FILTER_E_NO_MORE_TEXT = unchecked((int)0x80041701),

        /// <summary>
        /// No more property values available in chunk.
        /// </summary>
        FILTER_E_NO_MORE_VALUES = unchecked((int)0x80041702),

        /// <summary>
        /// Unable to access object.
        /// </summary>
        FILTER_E_ACCESS = unchecked((int)0x80041703),

        /// <summary>
        /// Moniker doesn't cover entire region.
        /// </summary>
        FILTER_W_MONIKER_CLIPPED = unchecked((int)0x00041704),

        /// <summary>
        /// No text in current chunk.
        /// </summary>
        FILTER_E_NO_TEXT = unchecked((int)0x80041705),

        /// <summary>
        /// No values in current chunk.
        /// </summary>
        FILTER_E_NO_VALUES = unchecked((int)0x80041706),

        /// <summary>
        /// Unable to bind IFilter for embedded object.
        /// </summary>
        FILTER_E_EMBEDDING_UNAVAILABLE = unchecked((int)0x80041707),

        /// <summary>
        /// Unable to bind IFilter for linked object.
        /// </summary>
        FILTER_E_LINK_UNAVAILABLE = unchecked((int)0x80041708),

        /// <summary>
        /// This is the last text in the current chunk.
        /// </summary>
        FILTER_S_LAST_TEXT = unchecked((int)0x00041709),

        /// <summary>
        /// This is the last value in the current chunk.
        /// </summary>
        FILTER_S_LAST_VALUES = unchecked((int)0x0004170A),

        /// <summary>
        /// File was not filtered due to password protection.
        /// </summary>
        FILTER_E_PASSWORD = unchecked((int)0x8004170B),

        /// <summary>
        /// The document format is not recognized by the filter.
        /// </summary>
        FILTER_E_UNKNOWNFORMAT = unchecked((int)0x8004170C),
    }

    /// <summary>
    /// The mode/access/sharing flags that are passed to IPersistXXX.Load.
    /// </summary>
    [Flags]
    internal enum STGM_FLAGS
    {
        //
        // Mode
        //

        /// <summary>
        /// Create. Subsumes Create, CreateNew and OpenOrCreate.
        /// </summary>
        CREATE = 0x00001000,

        /// <summary>
        /// Select the mode bit.
        /// </summary>
        MODE = 0x00001000, // mask

        // 
        // Access
        //

        /// <summary>
        /// Read access.
        /// </summary>
        READ = 0x00000000,
        /// <summary>
        /// Write access.
        /// </summary>
        WRITE = 0x00000001,
        /// <summary>
        /// Read-write access.
        /// </summary>
        READWRITE = 0x00000002,
        /// <summary>
        /// Flag to zero in on the access bits.
        /// </summary>
        ACCESS = 0x00000003, // mask

        // 
        // Sharing
        // 

        /// <summary>
        /// ReadWrite
        /// </summary>
        SHARE_DENY_NONE = 0x00000040,

        /// <summary>
        /// Write
        /// </summary>
        SHARE_DENY_READ = 0x00000030,

        /// <summary>
        /// Read
        /// </summary>
        SHARE_DENY_WRITE = 0x00000020,

        /// <summary>
        /// None
        /// </summary>
        SHARE_EXCLUSIVE = 0x00000010,

        /// <summary>
        /// Flag to select the Share bits.
        /// </summary>
        SHARING = 0x00000070, // mask
    }

    /// <summary>
    /// Property ID's for PSGUID_STORAGE
    /// </summary>
    internal enum PID_STG : int
    {
        /// <summary>
        /// The directory in which a file is located. Default type is VT_LPWSTR
        /// </summary>
        DIRECTORY = 0x02,

        /// <summary>
        /// CLID of a file. Default type is VT_CLSID.
        /// </summary>
        CLASSID = 0x03,

        /// <summary>
        /// Storage type for this file
        /// </summary>
        STORAGETYPE = 0x04,

        /// <summary>
        /// Volume ID of the disk
        /// </summary>
        VOLUME_ID = 0x05,

        /// <summary>
        /// Internal work ID for the parent or folder for this file.
        /// </summary>
        PARENT_WORKID = 0x06,

        /// <summary>
        /// Whether the file has been placed in secondary storage.
        /// </summary>
        SECONDARYSTORE = 0x07,

        /// <summary>
        /// Internal file index for this file.
        /// </summary>
        FILEINDEX = 0x08,

        /// <summary>
        /// Last change Update Sequence Number (USN) for this file.
        /// </summary>
        LASTCHANGEUSN = 0x09,

        /// <summary>
        /// The name of the file. Default type is VT_LPWSTR.
        /// </summary>
        NAME = 0x0a,

        /// <summary>
        /// The complete path for a file. Default type is VT_LPWSTR.
        /// </summary>
        PATH = 0x0b,

        /// <summary>
        /// The size of a file. Default type is VT_I8. 
        /// </summary>
        SIZE = 0x0c,

        /// <summary>
        /// The attribute flags for a file. Default type is VT_UI4. 
        /// </summary>
        ATTRIBUTES = 0x0d,

        /// <summary>
        /// The date and time of the last write to the file. Default type is VT_FILETIME.
        /// </summary>
        WRITETIME = 0x0e,

        /// <summary>
        /// The date and time the file was created. Default type is VT_FILETIME.
        /// </summary>
        CREATETIME = 0x0f,

        /// <summary>
        /// The time of the last access to the file. Default type is VT_FILETIME.
        /// </summary>
        ACCESSTIME = 0x10,

        /// <summary>
        /// The time of the last change to a file in an NTFS file system, including changes in the main data stream and secondary streams.  
        /// </summary>
        CHANGETIME = 0x11,

        /// <summary>
        /// File allocation size.
        /// </summary>
        ALLOCSIZE = 0x12,

        /// <summary>
        /// The contents of the file. This property is for query restrictions only; it cannot be retrieved in a query result. Default type is VT_LPWSTR.
        /// </summary>
        CONTENTS = 0x13,

        /// <summary>
        /// The short (8.3) file name for the file. Default type is VT_LPWSTR.
        /// </summary>
        SHORTNAME = 0x14
    }

    /// <summary>
    /// FULLPROPSPEC
    /// </summary>
    /// <remarks>
    /// typedef struct tagFULLPROPSPEC
    /// {
    ///     GUID guidPropSet;
    ///     PROPSPEC psProperty;
    /// } FULLPROPSPEC;
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    internal struct FULLPROPSPEC
    {
        /// <summary>
        /// The globally unique identifier (GUID) that identifies the property set.
        /// </summary>
        internal Guid guid;

        /// <summary>
        /// Pointer to the PROPSPEC structure that specifies a property either by its property 
        /// identifier or by the associated string name. 
        /// </summary>
        internal PROPSPEC property;
    };

    /// <summary>
    /// Init flags
    /// </summary>
    [Flags]
    internal enum IFILTER_INIT : int
    {
        /// <summary>
        /// Paragraph breaks should be marked with the Unicode PARAGRAPH SEPARATOR (0x2029)
        /// </summary>
        IFILTER_INIT_CANON_PARAGRAPHS = 0x01,
        /// <summary>
        /// Soft returns, such as the newline character in Word, 
        /// should be replaced by hard returnsLINE SEPARATOR (0x2028)
        /// </summary>
        IFILTER_INIT_HARD_LINE_BREAKS = 0x02,
        /// <summary>
        /// Various word-processing programs have forms of hyphens that are not represented 
        /// in the host character set, such as optional hyphens (appearing only at the end of a line) 
        /// and nonbreaking hyphens. This flag indicates that optional hyphens are to be converted 
        /// to nulls, and non-breaking hyphens are to be converted to normal hyphens (0x2010), or 
        /// HYPHEN-MINUSES (0x002D). 
        /// </summary>
        IFILTER_INIT_CANON_HYPHENS = 0x04,
        /// <summary>
        /// Just as the IFILTER_INIT_CANON_HYPHENS flag standardizes hyphens, this one standardizes spaces. 
        /// All special space characters, such as nonbreaking spaces, are converted to the standard space 
        /// character (0x0020). 
        /// </summary>
        IFILTER_INIT_CANON_SPACES = 0x08,
        /// <summary>
        /// Indicates that the client wants text split into chunks representing internal value-type properties.
        /// </summary>
        IFILTER_INIT_APPLY_INDEX_ATTRIBUTES = 0x10,
        /// <summary>
        /// Any properties not covered by the IFILTER_INIT_APPLY_INDEX_ATTRIBUTES and 
        /// IFILTER_INIT_APPLY_CRAWL_ATTRIBUTES flags should be emitted.
        /// </summary>
        IFILTER_INIT_APPLY_OTHER_ATTRIBUTES = 0x20,
        /// <summary>
        /// Optimizes IFilter for indexing because the client calls the IFilter::Init method only once and does 
        /// not call IFilter::BindRegion. This eliminates the possibility of accessing a chunk both before and 
        /// after accessing another chunk. 
        /// </summary>
        IFILTER_INIT_INDEXING_ONLY = 0x40,
        /// <summary>
        /// The text extraction process must recursively search all linked objects within the document. If a link
        /// is unavailable, the IFilter::GetChunk call that would have obtained the first chunk of the link should 
        /// return FILTER_E_LINK_UNAVAILABLE.
        /// </summary>
        IFILTER_INIT_SEARCH_LINKS = 0x80,
        /// <summary>
        /// Indicates that the client wants text split into chunks representing properties determined 
        /// during the indexing process. 
        /// </summary>
        IFILTER_INIT_APPLY_CRAWL_ATTRIBUTES = 0x100,
        /// <summary>
        /// The content indexing process can return property values set by the filter
        /// </summary>
        IFILTER_INIT_FILTER_OWNED_VALUE_OK = 0x200
    };

    /// <summary>
    /// more flags
    /// </summary>
    /// <remarks>
    /// typedef enum tagIFILTER_FLAGS
    /// {
    ///         IFILTER_FLAGS_OLE_PROPERTIES    = 1
    /// } IFILTER_FLAGS;
    /// </remarks>
    [Flags]
    internal enum IFILTER_FLAGS : int
    {
        /// <summary>
        /// Zero
        /// </summary>
        IFILTER_FLAGS_NONE = 0,

        /// <summary>
        /// This filter returns OLE properties
        /// </summary>
        IFILTER_FLAGS_OLE_PROPERTIES = 1
    };

    /// <summary>
    /// Break Type
    /// </summary>
    internal enum CHUNK_BREAKTYPE : int
    {
        /// <summary>
        /// No break is placed between the current chunk and the previous chunk. The chunks are glued together.
        /// </summary>
        CHUNK_NO_BREAK = 0,
        /// <summary>
        /// A word break is placed between this chunk and the previous chunk that had the same attribute. Use 
        /// of CHUNK_EOW should be minimized because the choice of word breaks is language-dependent, so 
        /// determining word breaks is best left to the search engine. 
        /// </summary>
        CHUNK_EOW = 1,
        /// <summary>
        /// A sentence break is placed between this chunk and the previous chunk that had the same attribute.
        /// </summary>
        CHUNK_EOS = 2,
        /// <summary>
        /// A paragraph break is placed between this chunk and the previous chunk that had the same attribute. 
        /// </summary>
        CHUNK_EOP = 3,
        /// <summary>
        /// A chapter break is placed between this chunk and the previous chunk that had the same attribute. 
        /// </summary>
        CHUNK_EOC = 4
    }

    /// <summary>
    /// Chunk State
    /// </summary>
    [Flags]
    internal enum CHUNKSTATE
    {
        /// <summary>
        /// The current chunk is a text-type property
        /// </summary>
        CHUNK_TEXT = 0x1,
        /// <summary>
        /// The current chunk is a value-type property
        /// </summary>
        CHUNK_VALUE = 0x2,
        /// <summary>
        /// Reserved
        /// </summary>
        CHUNK_FILTER_OWNED_VALUE = 0x4
    }

    /// <summary>
    /// Stat Chunk
    /// </summary>
    /// <remarks>
    /// typedef struct tagSTAT_CHUNK
    /// {
    ///     ULONG idChunk;
    ///     CHUNK_BREAKTYPE breakType;
    ///     CHUNKSTATE flags;
    ///     LCID locale;
    ///     FULLPROPSPEC attribute;
    ///     ULONG idChunkSource;
    ///     ULONG cwcStartSource;
    ///     ULONG cwcLenSource;
    ///     }     STAT_CHUNK;
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    internal struct STAT_CHUNK
    {
        /// <summary>
        /// The chunk identifier.
        /// Chunk identifiers must be unique for the current instance of the IFilter interface.
        /// Chunk identifiers must be in ascending order. The order in which chunks are numbered should correspond 
        /// to the order in which they appear in the source document. Some search engines can take advantage of the 
        /// proximity of chunks of various properties. If so, the order in which chunks with different properties
        /// are emitted will be important to the search engine. 
        /// </summary>
        internal uint idChunk;

        /// <summary>
        /// The type of break that separates the previous chunk from the current chunk.
        /// </summary>
        internal CHUNK_BREAKTYPE breakType;

        /// <summary>
        /// Flags indicate whether this chunk contains a text-type or a value-type property
        /// </summary>
        internal CHUNKSTATE flags;

        /// <summary>
        /// The language and sublanguage associated with a chunk of text
        /// </summary>
        /// <remarks>
        /// The managed equivalent to LCID is CultureInfo which takes an int constructor (the LCID) and
        /// offers a read-only LCID property.
        /// 
        /// COM definition:
        /// typedef DWORD LCID;       
        /// </remarks>
        internal uint locale;

        /// <summary>
        /// The property to be applied to the chunk
        /// </summary>
        internal FULLPROPSPEC attribute;

        /// <summary>
        /// The ID of the source of a chunk
        /// </summary>
        internal uint idChunkSource;

        /// <summary>
        /// The offset from which the source text for a derived chunk starts in the source chunk
        /// </summary>
        internal uint cwcStartSource;

        /// <summary>
        /// The length in characters of the source text from which the current chunk was derived
        /// </summary>
        internal uint cwcLenSource;
    };

    /// <summary>
    /// Unused but required for function signature on IFilter
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    internal struct FILTERREGION
    {
        /// <summary>
        /// Chunk ID
        /// </summary>
        uint idChunk;

        /// <summary>
        /// Beginning of the region, specified as an offset from the beginning of the chunk
        /// </summary>
        uint cwcStart;

        /// <summary>
        /// Extent of the region, specified as a number of Unicode characters
        /// </summary>
        uint cwcExtent;
    };
    #endregion

    #region IFilter Interface
    /// <summary>
    /// Managed equivalent for IFilter indexing interface
    /// </summary>
    [ComImport, ComVisible(true)]
    [Guid("89BCB740-6119-101A-BCB7-00DD010655AF")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IFilter
    {
        /// <summary>
        /// Init
        /// </summary>
        /// <param name="grfFlags">usage flags</param>
        /// <param name="cAttributes">number of items in aAttributes</param>
        /// <param name="aAttributes">array of FULLPROPSPEC structs</param>
        /// <returns></returns>
        IFILTER_FLAGS Init(
            [In] IFILTER_INIT grfFlags,         // IFILTER_INIT value     
            [In] uint cAttributes,              // number of items in aAttributes
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] 
            FULLPROPSPEC[] aAttributes);        // restrict responses to the specified attributes

        /// <summary>
        /// GetChunk
        /// </summary>
        /// <returns></returns>
        STAT_CHUNK GetChunk();

        /// <summary>
        ///    GetText
        /// </summary>
        /// <param name="pcwcBuffer">buffer size in characters before and after we write to it</param>
        /// <param name="pBuffer">preallocated buffer to write into</param>
        /// <returns></returns>
        void GetText([In, Out] ref uint pcwcBuffer, [In] IntPtr pBuffer);

        /// <summary>
        /// GetValue - unsafe code because marshaller cannot handle the complex argument
        /// </summary>
        /// <returns>PROPVARIANT object</returns>
        IntPtr GetValue();

        /// <summary>
        /// BindRegion - do not implement - unused
        /// </summary>
        /// <param name="origPos"></param>
        /// <param name="riid"></param>
        /// <returns></returns>
        IntPtr BindRegion([In]  FILTERREGION origPos, [In] ref Guid riid);
    }
    #endregion IFilter Interface

    #region IPersistStream Interface
    /// <summary>
    /// Managed definition of IPersistStream.
    /// </summary>
    /// <remarks>
    /// The declaration order strictly follows that in objidl.h, otherwise
    /// the vtbl would not be correctly accessed when invoking a COM object!
    /// </remarks>
    [ComImport, ComVisible(true)]
    [Guid("00000109-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPersistStream
    {
        /// <summary>
        /// Retrieves the ClassID of this object. It returns
        /// S_OK if successful, E_FAIL if not.
        /// 
        /// original interface:
        /// HRESULT GetClassID(CLSID * pClassID); 
        ///
        /// </summary>
        /// <remarks>
        /// Strictly speaking, this method is inherited from IPersist, but since
        /// IPersist is the only super-interface of IPersistStream, all we care about
        /// is for its method to be declared first (since we do not need IPersist per se).
        /// </remarks>
        void GetClassID(out Guid pClassID);

        /// <summary>
        /// Determines if the object has changed since the last
        /// save. S_OK means the file is clean. S_FALSE the file
        /// has changed.
        /// 
        /// original interface:
        /// HRESULT IsDirty(void);
        /// 
        /// </summary>
        [PreserveSig()]
        int IsDirty();

        /// <summary>
        /// Points the component to the specified stream.
        /// </summary>
        /// <remarks>
        /// Uses our own managed definition of IStream in order to allow marshaling
        /// the result of pointer arithmetic in Read.
        /// </remarks>
        void Load(IStream pstm);

        /// <summary>
        /// This is used when saving an object to a stream.
        /// </summary>
        /// <remarks>
        /// In the case of an indexing filter, this function is not supported.
        /// </remarks>
        void Save(IStream pstm, [MarshalAs(UnmanagedType.Bool)] bool fRemember);

        /// <summary>
        /// Return the size in bytes of the stream needed to save the object.
        /// </summary>
        /// <param name="pcbSize">Points to a 64-bit unsigned integer value indicating the size in bytes of the stream needed to save this object.</param>
        /// <remarks>
        /// This is meaningful only when the interface is implemented in such a way
        /// as to define a persistent object that is backed against the stream.
        /// </remarks>
        void GetSizeMax([Out] out Int64 pcbSize);
    }
    #endregion IPersistStream Interface

    #region IStream
    /// <summary>
    /// Managed definition of IStream.
    /// </summary>
    /// <remarks>
    /// The InteropServices provide a standard definition for this interface, but it is redefined
    /// here to allow Read and Write to pass pointers rather than arrays, allowing them to optimize
    /// the handling of the offset parameter.
    /// The hard core of this definition consists of the Guid attribute and the order in which methods are declared.
    /// This will map to an IStream vtbl, just like the standard definition.
    /// So, the only difference with the standard definition is in the way the buffer parameter to Read and Write
    /// will be marshaled.
    /// </remarks>
    [ComImport, ComVisible(true)]
    [Guid("0000000C-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IStream
    {
        /// <summary>
        /// Read no more than sizeInBytes bytes into the byte array located at bufferBase.
        /// Return the result at the address refToNumBytesRead.
        /// </summary>
        /// <remarks>
        /// Strictly speaking, Read and Write are part of ISequentialStream, which is inherited by IStream. Placing
        /// them at the head of the list of methods reflects vtbl layout in COM C++.
        ///
        /// IntPtr gets marshaled as ELEMENT_TYPE_I, which is defined in CorHdr.h as the "native integer size".
        /// For all practical purposes, this is the size of an address, for there's no architecture we support whose
        /// data bus and address bus have different widths.
        /// </remarks>
        void Read(IntPtr bufferBase, int sizeInBytes, IntPtr refToNumBytesRead);

        /// <summary>
        /// Write at most sizeInBytes bytes into the byte array located at buffer base.
        /// Return the number of bytes effectively written at the address refToNumBytesWritten.
        /// </summary>
        void Write(IntPtr bufferBase, int sizeInBytes, IntPtr refToNumBytesWritten);

        /// <summary>
        /// Move the position to 'offset' with respect to 'origin' (one of STREAM_SEEK_SET, STREAM_SEEK_CUR,
        /// and STREAM_SEEK_END).
        /// The new position is returned at address refToNewOffsetNullAllowed unless this param is set to null.
        /// </summary>
        void Seek(long offset, int origin, IntPtr refToNewOffsetNullAllowed);

        /// <summary>
        /// Set the stream size.
        /// </summary>
        void SetSize(long newSize);

        /// <summary>
        /// Copy bytesToCopy bytes from the current position into targetStream.
        /// </summary>
        void CopyTo(
            MS.Internal.Interop.IStream targetStream,
            long bytesToCopy,
            IntPtr refToNumBytesRead,
            IntPtr refToNumBytesWritten);

        /// <summary>
        /// Flush or, in transacted mode, commit. The commitFlags parameter is a member of the STGC enumeration.
        /// The most commonly used value is STGC_DEFAULT, i.e. 0.
        /// </summary>
        void Commit(int commitFlags);

        /// <summary>
        /// Discards all changes that have been made to a transacted stream since the last call to IStream::Commit.
        /// </summary>
        void Revert();

        /// <summary>
        /// Restricts access to a specified range of bytes in the stream.
        /// Supporting this functionality is optional since some file systems do not provide it.
        /// </summary>
        void LockRegion(long offset, long sizeInBytes, int lockType);

        /// <summary>
        /// Removes the access restriction on a range of bytes previously restricted with IStream::LockRegion.
        /// </summary>
        void UnlockRegion(long offset, long sizeInBytes, int lockType);

        /// <summary>
        /// Retrieves the STATSTG structure for this stream.
        /// </summary>
        void Stat(out System.Runtime.InteropServices.ComTypes.STATSTG statStructure, int statFlag);

        /// <summary>
        /// Creates a new stream object that references the same bytes as the original stream but provides a separate
        /// seek pointer to those bytes.
        /// </summary>
        void Clone(out MS.Internal.Interop.IStream newStream);
    }
    #endregion IStream

    #region IPersistStreamWithArrays Interface
    /// <summary>
    /// Managed definition of IPersistStream.
    /// </summary>
    /// <remarks>
    /// This particular version uses the standard managed definition of IStream,
    /// which marshals arrays rather than buffer pointers. It is destined to be used
    /// with the class ManagedIStream, which implements IStream in managed code.
    /// 
    /// The declaration order strictly follows that in objidl.h, otherwise
    /// the vtbl would not be correctly accessed when invoking a COM object!
    /// </remarks>
    [ComImport, ComVisible(true)]
    [Guid("00000109-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPersistStreamWithArrays
    {
        /// <summary>
        /// Retrieves the ClassID of this object. It returns
        /// S_OK if successful, E_FAIL if not.
        /// 
        /// original interface:
        /// HRESULT GetClassID(CLSID * pClassID); 
        ///
        /// </summary>
        /// <remarks>
        /// Strictly speaking, this method is inherited from IPersist, but since
        /// IPersist is the only super-interface of IPersistStreamWithArrays, all we care about
        /// is for its method to be declared first (since we do not need IPersist per se).
        /// </remarks>
        void GetClassID(out Guid pClassID);

        /// <summary>
        /// Determines if the object has changed since the last
        /// save. S_OK means the file is clean. S_FALSE the file
        /// has changed.
        /// 
        /// original interface:
        /// HRESULT IsDirty(void);
        /// 
        /// </summary>
        [PreserveSig()]
        int IsDirty();

        /// <summary>
        /// Points the component to the specified stream.
        /// </summary>
        /// <remarks>
        /// Uses the standard array-based definition of IStream.
        /// </remarks>
        void Load(System.Runtime.InteropServices.ComTypes.IStream pstm);

        /// <summary>
        /// This is used when saving an object to a stream.
        /// </summary>
        /// <remarks>
        /// In the case of an indexing filter, this function is not supported.
        /// </remarks>
        void Save(System.Runtime.InteropServices.ComTypes.IStream pstm, [MarshalAs(UnmanagedType.Bool)] bool fRemember);

        /// <summary>
        /// Return the size in bytes of the stream needed to save the object.
        /// </summary>
        /// <param name="pcbSize">Points to a 64-bit unsigned integer value indicating the size in bytes of the stream needed to save this object.</param>
        /// <remarks>
        /// This is meaningful only when the interface is implemented in such a way
        /// as to define a persistent object that is backed against the stream.
        /// </remarks>
        void GetSizeMax([Out] out Int64 pcbSize);
    }
    #endregion IPersistStreamWithArrays Interface


    #region IPersistFile Interface

    /// <summary>
    /// Managed definition of IPersistFile.
    /// </summary>
    /// <remarks>
    /// The System.Runtime.InteropServices.ComTypes namespace provides a standard definition for 
    /// this interface, but it is redefined here to allow IPersistFile.GetCurFile() to have
    /// two successful return values S_OK and S_FALSE. In order to return S_FALSE and still
    /// keep the output parameter ppszFileName valid, we cannot return by throwing COMExceptions.
    /// Instead, IPersistFile.GetCurFile() needs to be marked [PreserveSig] and explicitly
    /// return S_OK or S_FALSE.
    /// </remarks>
    [ComImport, ComVisible(true)]
    [Guid("0000010b-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPersistFile
    {
        /// <summary>
        /// Retrieves the class identifier (CLSID) of an object.
        /// </summary>
        /// <param name="pClassID">When this method returns, contains a reference to the CLSID. 
        /// This parameter is passed uninitialized.
        /// </param>
        void GetClassID(out Guid pClassID);

        /// <summary>
        /// Checks an object for changes since it was last saved to its current file.
        /// </summary>
        /// <returns>
        /// S_OK if the file has changed since it was last saved; S_FALSE if the file
        /// has not changed since it was last saved.
        /// </returns>
        [PreserveSig]
        int IsDirty();

        /// <summary>
        /// Opens the specified file and initializes an object from the file contents.
        /// </summary>
        /// <param name="pszFileName">A zero-terminated string containing the absolute path of the file to open.</param>
        /// <param name="dwMode">
        /// A combination of values from the STGM enumeration to indicate the access
        /// mode in which to open pszFileName.
        /// </param>
        void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, int dwMode);

        /// <summary>
        /// Saves a copy of the object into the specified file.
        /// </summary>
        /// <param name="pszFileName">
        /// A zero-terminated string containing the absolute path of the file to which
        /// the object is saved.
        /// </param>
        /// <param name="fRemember">
        /// true to used the pszFileName parameter as the current working file; otherwise false.
        /// </param>
        void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName,
                    [MarshalAs(UnmanagedType.Bool)] bool fRemember);

        /// <summary>
        /// Notifies the object that it can write to its file.
        /// </summary>
        /// <param name="pszFileName">The absolute path of the file where the object was previously saved.</param>
        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);

        /// <summary>
        /// Retrieves either the absolute path to the current working file of the object
        /// or, if there is no current working file, the default file name prompt of
        /// the object.
        /// </summary>
        /// <param name="ppszFileName">
        /// When this method returns, contains the address of a pointer to a zero-terminated
        /// string containing the path for the current file, or the default file name
        /// prompt (such as *.txt). This parameter is passed uninitialized.
        /// </param>
        [PreserveSig]
        int GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
    }

    #endregion IPersistFile Interface
}
