// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: NativeDirectoryServicesQueryAPIs contains managed wrappers 
//              for native calls related to Directory Services query APIs
//              and helper methods necessary to make use of them.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace MS.Internal.Documents
{
    internal partial class PeoplePickerWrapper
    {
        /// <summary>
        /// NativeDirectoryServices contains managed wrappers 
        /// for native calls related to Directory Services query APIs
        /// and helper methods necessary to make use of them.
        /// </summary>
        internal static class UnsafeNativeMethods
        {
            ///<summary>
            /// Managed interface definition for the Active Directory ICommonQuery interface
            /// defined in cmnquery.h as having the following method:
            /// 
            ///  HRESULT OpenQueryWindow(
            ///     HWND hwdnParent,
            ///     LPOPENQUERYWINDOW* pQueryWnd,
            ///     IDataObject** ppDataObj
            ///  );
            /// See also: 
            /// https://docs.microsoft.com/en-us/windows/desktop/api/cmnquery/nn-cmnquery-icommonquery
            /// and
            /// https://docs.microsoft.com/en-us/windows/desktop/api/cmnquery/nf-cmnquery-icommonquery-openquerywindow
            /// </summary>
            [Guid("ab50dec0-6f1d-11d0-a1c4-00aa00c16e65")]
            [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
            [ComImport]
            internal interface ICommonQuery
            {
                [PreserveSig]
                UInt32 OpenQueryWindow(
                    [In] IntPtr hwndParent,
                    [In] ref OpenQueryWindowParams pQueryWnd,
                    [Out] out IDataObject ppDataObj
                    );
            }

            /// <summary>
            /// Managed wrapper for the OPENQUERYWINDOW struct defined in
            /// cmnquery.h as:
            /// typedef struct {
            ///     DWORD cbStruct;
            ///     DWORD dwFlags;
            ///     CLSID clsidHandler;
            ///     LPVOID pHandlerParameters;
            ///     CLSID clsidDefaultForm;
            ///     IPersistQuery* pPersistQuery;
            ///     union {
            ///         void* pFormParameters;
            ///         IPropertyBag* ppbFormParameters
            ///     };
            /// } OPENQUERYWINDOW;
            /// 
            /// See also:
            /// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/ad/ad/openquerywindow.asp
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            internal struct OpenQueryWindowParams
            {
                public UInt32 cbStruct;
                public UInt32 dwFlags;
                public Guid clsidHandler;
                public IntPtr pHandlerParameters;
                public Guid clsidDefaultForm;
                public IntPtr pPersistQuery;      // Originally an IPersistQuery (not an IntPtr), which we do not use.
                public IntPtr pFormParameters;    // Originally a union in the COM definition;
            }                                     // but because we do not use this field we leave this
            // as a single field to avoid 64-bit layout issues.

            /// <summary>
            /// Managed wrapper for the DSQUERYINITPARAMS struct defined in
            /// dsquery.h as:
            /// typedef struct {
            ///     DWORD cbStruct;
            ///     DWORD dwFlags;
            ///     LPWSTR pDefaultScope;
            ///     LPWSTR pDefaultSaveLocation;
            ///     LPWSTR pUserName;
            ///     LPWSTR pPassword;
            ///     LPWSTR pServer
            /// } DSQUERYINITPARAMS;
            /// 
            /// See also:
            /// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/ad/ad/dsqueryinitparams.asp
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            internal struct QueryInitParams
            {
                public uint cbStruct;
                public uint dwFlags;
                [MarshalAs(UnmanagedType.LPWStr)]
                public String pDefaultScope;

                [MarshalAs(UnmanagedType.LPWStr)]
                public String pDefaultSaveLocation;

                [MarshalAs(UnmanagedType.LPWStr)]
                public String pUserName;

                [MarshalAs(UnmanagedType.LPWStr)]
                public String pPassword;

                [MarshalAs(UnmanagedType.LPWStr)]
                public String pServer;
            }

            /// <summary>
            /// Managed wrapper for the DSOBJECT struct defined in
            /// dsclient.h as:
            /// typedef struct {
            ///     DWORD dwFlags;
            ///     DWORD dwProviderFlags;
            ///     DWORD offsetName;
            ///     DWORD offsetClass;
            /// } DSOBJECT;
            /// 
            /// See also:
            /// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/ad/ad/dsobject.asp
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            internal struct DsObject
            {
                public UInt32 dwFlags;
                public UInt32 dwProviderFlags;
                public UInt32 offsetName;
                public UInt32 offsetClass;
            }

            /// <summary>
            /// Managed wrapper for the DSOBJECTNAMES struct defined in
            /// dsclient.h as:
            /// typedef struct {
            ///     CLSID clsidNamespace;
            ///     UINT cItems;
            ///     DSOBJECT aObjects[1];
            /// } DSOBJECTNAMES;
            /// 
            /// See also:
            /// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/ad/ad/dsobjectnames.asp
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            internal struct DsObjectNames
            {
                public Guid clsidNamespace;
                public UInt32 cItems;
                [MarshalAs(UnmanagedType.ByValArray)]
                public DsObject[] aObjects;
            }

            // CLSIDs for objects we're concerned with (from cmnquery.h and dsquery.h)
            internal static readonly Guid CLSID_CommonQuery = new Guid(0x83bc5ec0, 0x6f2a, 0x11d0, 0xa1, 0xc4, 0x00, 0xaa, 0x00, 0xc1, 0x6e, 0x65);
            internal static readonly Guid CLSID_DsQuery = new Guid(0x08a23e65e, 0x31c2, 0x11d0, 0x89, 0x1c, 0x00, 0xa0, 0x24, 0xab, 0x2d, 0xbb);
            internal static readonly Guid CLSID_DsFindPeople = new Guid(0x83ee3fe2, 0x57d9, 0x11d0, 0xb9, 0x32, 0x0, 0xa0, 0x24, 0xab, 0x2d, 0xbb);

            // CommonQuery parameters (from cmnquery.h) used in OpenQueryWindowParams to define the
            // state of the People Picker when invoked.
            internal static readonly uint OQWF_OKCANCEL = 0x00000001; // = 1 => Provide OK/Cancel buttons
            internal static readonly uint OQWF_DEFAULTFORM = 0x00000002; // = 1 => clsidDefaultQueryForm is valid        
            internal static readonly uint OQWF_SINGLESELECT = 0x00000004; // = 1 => allow single selection only
            internal static readonly uint OQWF_REMOVEFORMS = 0x00000020; // = 1 => remove form picker from dialog
            internal static readonly uint OQWF_SHOWOPTIONAL = 0x00000080; // = 1 => list optional forms by default
            internal static readonly uint OQWF_HIDEMENUS = 0x00000400; // = 1 => no menu bar displayed

            // Clipboard formats (from winuser.h)
            internal static readonly String CFSTR_DSOBJECTNAMES = "DsObjectNames";

            // Success/Failure codes
            internal static readonly uint S_OK = 0x00000000;
            internal static readonly uint S_FALSE = 0x00000001;
            internal static readonly uint E_FAIL = 0x80000008;

        }
    }
}
