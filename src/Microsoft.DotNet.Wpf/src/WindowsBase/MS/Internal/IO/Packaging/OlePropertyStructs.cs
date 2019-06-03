// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//   These structures and constants are managed equivalents to COM structures
//   used to access OLE properties.
//
//
//


using System;
using System.Runtime.InteropServices;

using MS.Internal.WindowsBase;        // for FriendAccessAllowedAttribute

namespace MS.Internal.IO.Packaging
{
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    internal struct STATPROPSTG
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        string lpwstrName; 
        UInt32 propid;
        VARTYPE vt;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    internal struct STATPROPSETSTG
    {
        Guid fmtid;
        Guid clsid;
        UInt32 grfFlags;
        System.Runtime.InteropServices.ComTypes.FILETIME mtime;
        System.Runtime.InteropServices.ComTypes.FILETIME ctime;
        System.Runtime.InteropServices.ComTypes.FILETIME atime;
        UInt32 dwOSVersion;
    }

    #region PROPVARIANT

    /// <summary>
    /// Managed view of unmanaged PROPVARIANT type
    /// </summary>
    /// <remarks>
    /// PROPVARIANT can represent many different things.  We are only interested in strings
    /// for this version but the full range of values is listed her for completeness.
    /// 
    /// typedef unsigned short VARTYPE;
    /// typedef unsigned short WORD;
    /// typedef struct PROPVARIANT {  
    /// VARTYPE vt;  WORD wReserved1;  WORD wReserved2;  WORD wReserved3;  
    /// union {    
    ///     CHAR cVal;    
    ///     UCHAR bVal;    
    ///     SHORT iVal;    
    ///     USHORT uiVal;    
    ///     LONG lVal;    
    ///     INT intVal;    
    ///     ULONG ulVal;    
    ///     UINT uintVal;    
    ///     LARGE_INTEGER hVal;    
    ///     ULARGE_INTEGER uhVal;    
    ///     FLOAT fltVal;    DOUBLE dblVal;    CY cyVal;    DATE date;    
    ///     BSTR bstrVal;    VARIANT_BOOL boolVal;    SCODE scode;    
    ///     FILETIME filetime;    LPSTR pszVal;    LPWSTR pwszVal;    
    ///     CLSID* puuid;    CLIPDATA* pclipdata;    BLOB blob;    
    ///     IStream* pStream;    IStorage* pStorage;    IUnknown* punkVal;    
    ///     IDispatch* pdispVal;    LPSAFEARRAY parray;    CAC cac;    
    ///     CAUB caub;    CAI cai;    CAUI caui;    CAL cal;    CAUL caul;    
    ///     CAH cah;    CAUH cauh;    CAFLT caflt;    CADBL cadbl;    
    ///     CACY cacy;    CADATE cadate;    CABSTR cabstr;    
    ///     CABOOL cabool;    CASCODE cascode;    CALPSTR calpstr;    
    ///     CALPWSTR calpwstr;    CAFILETIME cafiletime;    CACLSID cauuid;    
    ///     CACLIPDATA caclipdata;    CAPROPVARIANT capropvar;    
    ///     CHAR* pcVal;    UCHAR* pbVal;    SHORT* piVal;    USHORT* puiVal;    
    ///     LONG* plVal;    ULONG* pulVal;    INT* pintVal;    UINT* puintVal;    
    ///     FLOAT* pfltVal;    DOUBLE* pdblVal;    VARIANT_BOOL* pboolVal;    
    ///     DECIMAL* pdecVal;    SCODE* pscode;    CY* pcyVal;    
    ///     PROPVARIANT* pvarVal;  
    /// }; 
    /// } PROPVARIANT;
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    [FriendAccessAllowed]
    internal struct PROPVARIANT
    {
        /// <summary>
        /// Variant type
        /// </summary>
        internal VARTYPE          vt;

        /// <summary>
        /// unused
        /// </summary>
        internal ushort wReserved1;

        /// <summary>
        /// unused
        /// </summary>
        internal ushort wReserved2;

        /// <summary>
        /// unused
        /// </summary>
        internal ushort wReserved3;

        /// <summary>
        /// union where the actual variant value lives
        /// </summary>
        internal PropVariantUnion union;
    }

    /// <summary>
    /// enumeration for all legal types of a PROPVARIANT
    /// </summary>
    /// <remarks>add definitions as needed</remarks>
    [FriendAccessAllowed]
    internal enum VARTYPE : short
    {
        /// <summary>
        /// BSTR
        /// </summary>
        VT_BSTR = 8,        // BSTR allocated using SysAllocString

        /// <summary>
        /// LPSTR
        /// </summary>
        VT_LPSTR = 30,

        /// <summary>
        /// FILETIME
        /// </summary>
        VT_FILETIME = 64,
    }

    /// <summary>
    /// Union portion of PROPVARIANT
    /// </summary>
    /// <remarks>
    /// All fields (or their placeholders) are declared even if 
    /// they are not used. This is to make sure that the size of
    /// the union matches the size of the union in
    /// the actual unmanaged PROPVARIANT structure 
    /// for all architectures (32-bit/64-bit). 
    /// Points to note:
    /// - All pointer type fields are declared as IntPtr.
    /// - CAxxx type fields (like CAC, CAUB, etc.) are all of same
    ///     structural layout, hence not all declared individually 
    ///     since they are not used. A placeholder CArray 
    ///     is used to represent all of them to account for the
    ///     size of these types. CArray is defined later.
    /// - Rest of the fields are declared with corresponding 
    ///     managed equivalent types.
    /// </remarks>
    [StructLayout(LayoutKind.Explicit)]
    [FriendAccessAllowed]
    internal struct PropVariantUnion
    {
        /// <summary>
        /// CHAR
        /// </summary>
        [FieldOffset(0)]
        internal sbyte cVal;

        /// <summary>
        /// UCHAR
        /// </summary>
        [FieldOffset(0)]
        internal byte bVal;

        /// <summary>
        /// SHORT
        /// </summary>
        [FieldOffset(0)]
        internal short iVal;

        /// <summary>
        /// USHORT
        /// </summary>
        [FieldOffset(0)]
        internal ushort uiVal;

        /// <summary>
        /// LONG
        /// </summary>
        [FieldOffset(0)]
        internal int lVal;

        /// <summary>
        /// ULONG
        /// </summary>
        [FieldOffset(0)]
        internal uint ulVal;

        /// <summary>
        /// INT
        /// </summary>
        [FieldOffset(0)]
        internal int intVal;

        /// <summary>
        /// UINT
        /// </summary>
        [FieldOffset(0)]
        internal uint uintVal;

        /// <summary>
        /// LARGE_INTEGER
        /// </summary>
        [FieldOffset(0)]
        internal Int64 hVal;

        /// <summary>
        /// ULARGE_INTEGER
        /// </summary>
        [FieldOffset(0)]
        internal UInt64 uhVal;

        /// <summary>
        /// FLOAT
        /// </summary>
        [FieldOffset(0)]
        internal float fltVal;

        /// <summary>
        /// DOUBLE
        /// </summary>
        [FieldOffset(0)]
        internal double dblVal;

        /// <summary>
        /// VARIANT_BOOL
        /// </summary>
        [FieldOffset(0)]
        internal short boolVal;

        /// <summary>
        /// SCODE
        /// </summary>
        [FieldOffset(0)]
        internal int scode;

        /// <summary>
        /// CY
        /// </summary>
        [FieldOffset(0)]
        internal CY cyVal;

        /// <summary>
        /// DATE
        /// </summary>
        [FieldOffset(0)]
        internal double date;

        /// <summary>
        /// FILETIME
        /// </summary>
        [FieldOffset(0)]
        internal System.Runtime.InteropServices.ComTypes.FILETIME filetime;


        /// <summary>
        /// CLSID*   
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr puuid;

        /// <summary>
        /// CLIPDATA*    
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pclipdata;

        /// <summary>
        /// BSTR
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr bstrVal;

        /// <summary>
        /// BSTRBLOB   
        /// </summary>
        [FieldOffset(0)]
        internal BSTRBLOB bstrblobVal;

        /// <summary>
        /// BLOB
        /// </summary>
        [FieldOffset(0)]
        internal BLOB blob;

        /// <summary>
        /// LPSTR
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pszVal;

        /// <summary>
        /// LPWSTR
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pwszVal;

        /// <summary>
        /// IUnknown*
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr punkVal;

        /// <summary>
        /// IDispatch*
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pdispVal;

        /// <summary>
        /// IStream*
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pStream;

        /// <summary>
        /// IStorage*
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pStorage;

        /// <summary>
        /// LPVERSIONEDSTREAM
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pVersionedStream;

        /// <summary>
        /// LPSAFEARRAY 
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr parray;

        /// <summary>
        /// Placeholder for
        /// CAC, CAUB, CAI, CAUI, CAL, CAUL, CAH, CAUH; CAFLT,
        /// CADBL, CABOOL, CASCODE, CACY, CADATE, CAFILETIME, 
        /// CACLSID, CACLIPDATA, CABSTR, CABSTRBLOB, 
        /// CALPSTR, CALPWSTR, CAPROPVARIANT 
        /// </summary>
        [FieldOffset(0)]
        internal CArray cArray;

        /// <summary>
        /// CHAR*
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pcVal;

        /// <summary>
        /// UCHAR* 
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pbVal;

        /// <summary>
        /// SHORT* 
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr piVal;

        /// <summary>
        /// USHORT* 
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr puiVal;

        /// <summary>
        /// LONG* 
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr plVal;

        /// <summary>
        /// ULONG* 
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pulVal;

        /// <summary>
        /// INT* 
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pintVal;

        /// <summary>
        /// UINT* 
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr puintVal;

        /// <summary>
        /// FLOAT* 
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pfltVal;

        /// <summary>
        /// DOUBLE* 
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pdblVal;

        /// <summary>
        /// VARIANT_BOOL* 
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pboolVal;

        /// <summary>
        /// DECIMAL* 
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pdecVal;

        /// <summary>
        /// SCODE* 
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pscode;

        /// <summary>
        /// CY* 
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pcyVal;

        /// <summary>
        /// DATE* 
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pdate;

        /// <summary>
        /// BSTR*
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pbstrVal;

        /// <summary>
        /// IUnknown** 
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr ppunkVal;

        /// <summary>
        /// IDispatch** 
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr ppdispVal;

        /// <summary>
        /// LPSAFEARRAY* 
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pparray;

        /// <summary>
        /// PROPVARIANT*
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pvarVal;
    }

    #region Structs used by PropVariantUnion

    // 
    // NOTE: Verifiability requires that the 
    // fields of these value-types need to be public
    // since PropVariantUnion has explicit layout,
    // and has these value-types as its fields in a way that 
    // overlaps with other PropVariantUnion fields
    // (same FieldOffset for multiple fields).
    //

    /// <summary>
    /// CY, used in PropVariantUnion.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    [FriendAccessAllowed]
    internal struct CY
    {
        public uint Lo;
        public int Hi;
    }

    /// <summary>
    /// BSTRBLOB, used in PropVariantUnion.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    [FriendAccessAllowed]
    internal struct BSTRBLOB
    {
        public uint cbSize;
        public IntPtr pData;
    }

    /// <summary>
    /// BLOB, used in PropVariantUnion.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    [FriendAccessAllowed]
    internal struct BLOB
    {
        public uint cbSize;
        public IntPtr pBlobData;
    }

    /// <summary>
    /// CArray, used in PropVariantUnion.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    [FriendAccessAllowed]
    internal struct CArray
    {
        public uint cElems;
        public IntPtr pElems;
    }

    #endregion Structs used by PropVariantUnion

    #endregion PROPVARIANT

    #region PROPSPEC
    /// <summary>
    /// Selector for union
    /// </summary>
    [FriendAccessAllowed]
    internal enum PropSpecType : int
    {
        /// <summary>
        /// type is a string
        /// </summary>
        Name = 0,
        /// <summary>
        /// type is a property id
        /// </summary>
        Id = 1,
    }

    /// <summary>
    /// Explicitly packed union
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    [FriendAccessAllowed]
    internal struct PROPSPECunion
    {
        /// <summary>
        /// first value of union - String
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr name;            // kind=0

        /// <summary>
        /// second value of union - ULong
        /// </summary>
        [FieldOffset(0)]
        internal uint propId;        // kind=1   
    }

    /// <summary>
    /// PROPSPEC - needed for IFilter.Init to specify which properties to extract
    /// </summary>
    /// <remarks>
    /// typedef struct tagPROPSPEC 
    /// {  
    ///    ULONG ulKind;  
    ///    union {    
    ///         PROPID propid;          // ulKind = 1
    ///         LPOLESTR lpwstr;        // ulKind = 0
    ///     };
    /// } PROPSPEC;
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    [FriendAccessAllowed]
    internal struct PROPSPEC
    {
        /// <summary>
        /// Selector value
        /// </summary>
        internal uint propType; // ULONG in COM is UInt32 in .NET

        /// <summary>
        /// Union struct
        /// </summary>
        internal PROPSPECunion union;
    }

    #endregion PROPSPEC

    #region FormatId

    /// <summary>
    /// Format identifiers.
    /// </summary>
    [FriendAccessAllowed]
    internal static class FormatId
    {
        /// <summary>
        /// Property sets.
        /// </summary>
        /// <remarks>
        /// Can't be declared readonly since they have to passed by ref
        /// to EncryptedPackageCoreProperties.GetOleProperty, because 
        /// IPropertyStorage.ReadMultiple takes a "ref Guid" as its first argument.
        /// </remarks>
        internal static Guid SummaryInformation =
            new Guid("{F29F85E0-4FF9-1068-AB91-08002B27B3D9}");
        internal static Guid DocumentSummaryInformation =
            new Guid("{D5CDD502-2E9C-101B-9397-08002B2CF9AE}");
    }

    #endregion FormatId

    #region PropertyId

    /// <summary>
    /// Property identifiers.
    /// </summary>
    [FriendAccessAllowed]
    internal static class PropertyId
    {
        /// <summary>
        /// Summary Information property identifiers.
        /// </summary>
        internal const uint Title = 0x00000002;
        internal const uint Subject = 0x00000003;
        internal const uint Creator = 0x00000004;
        internal const uint Keywords = 0x00000005;
        internal const uint Description = 0x00000006;
        internal const uint LastModifiedBy = 0x00000008;
        internal const uint Revision = 0x00000009;
        internal const uint LastPrinted = 0x0000000B;
        internal const uint DateCreated = 0x0000000C;
        internal const uint DateModified = 0x0000000D;

        /// <summary>
        /// Document Summary Information property identifiers.
        /// </summary>
        internal const uint Category = 0x00000002;
        internal const uint Identifier = 0x00000012;
        internal const uint ContentType = 0x0000001A;
        internal const uint Language = 0x0000001B;
        internal const uint Version = 0x0000001C;
        internal const uint ContentStatus = 0x0000001D;
    }

    #endregion PropertyId
}   
