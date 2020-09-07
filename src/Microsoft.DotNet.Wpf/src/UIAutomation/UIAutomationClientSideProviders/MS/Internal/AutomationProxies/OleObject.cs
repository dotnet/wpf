// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************************************************

        BE CAREFUL WHEN MODIFYING THIS FILE. ORDER OF METHODS SHOULD MATCH EXACTLY TO V-TABLE LAYOUT !!!!!

        Note that DispId attribute is skipped since we are the only client and DISPIDs are never
        used when .Net component invokes dual interface methods on a COM object since CLR always calls
        through the v-table. TypeLibTypeAttribute is ommitted as well, once again since we are the only client
        All base interface methods must be duplicated on the definition of the derived interface.
        Methods that will not be used (at least at this time) can be replcased with the void Placeholder()
        If method returning the interface that will not be used, in order to prevent the baloon  effect
        simply return object.

        [PreserveSig] Can be used when needed in order to get back HRESULT. If not used && FAILED(hr)
                      Exception will be thrown

*****************************************************************************************************************/

using System;
using System.Runtime.InteropServices;
using MS.Win32;


namespace MS.Internal.AutomationProxies
{
    //------------------------------------------------------
    //
    //  IOleObject
    //
    //------------------------------------------------------

    #region IOleObject

    [ComImport, Guid("00000112-0000-0000-C000-000000000046"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IOleObject
    {
        void Placeholder_SetClientSite();
        void Placeholder_GetClientSite();
        void Placeholder_SetHostNames();
        void Placeholder_Close();
        void Placeholder_SetMoniker();
        void Placeholder_GetMoniker();
        void Placeholder_InitFromData();
        [PreserveSig]int GetClipboardData(int dwReserved, out IDataObject data);
        void Placeholder_DoVerb();
        void Placeholder_EnumVerbs();
        void Placeholder_OleUpdate();
        void Placeholder_IsUpToDate();
        void Placeholder_GetUserClassID();
        void Placeholder_GetUserType();
        void Placeholder_SetExtent();
        void Placeholder_GetExtent();
        void Placeholder_Advise();
        void Placeholder_Unadvise();
        void Placeholder_EnumAdvise();
        void Placeholder_GetMiscStatus();
        void Placeholder_SetColorScheme();
    }

    #endregion IOleObject

    //------------------------------------------------------
    //
    //  IOleDataObject
    //
    //------------------------------------------------------

    #region IDataObject

    [ComImport, Guid("0000010E-0000-0000-C000-000000000046"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IDataObject 
    {
        [PreserveSig]int GetData(ref UnsafeNativeMethods.FORMATETC pFormatetc, [In, Out]ref UnsafeNativeMethods.STGMEDIUM pMedium);
        void Placeholder_GetDataHere();
        void Placeholder_QueryGetData();
        void Placeholder_GetCanonicalFormatEtc();
        void Placeholder_SetData();
        void Placeholder_OleEnumFormatEtc();
        void Placeholder_DAdvise();
        void Placeholder_DUnadvise();
        void Placeholder_EnumDAdvise();
    }

    #endregion IDataObject

    //------------------------------------------------------
    //
    //  DataObject constants
    //
    //------------------------------------------------------

    #region DataObjectConstants

    internal static class DataObjectConstants
    {
        internal const int CF_TEXT = 1;
        internal const int CF_UNICODETEXT = 13;

        internal const int DVASPECT_CONTENT = 1;

        internal const int TYMED_HGLOBAL = 1;
    }

    #endregion DataObjectConstants
}
