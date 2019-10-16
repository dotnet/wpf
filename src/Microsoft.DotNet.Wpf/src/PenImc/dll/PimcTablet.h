// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// PimcTablet.h : Declaration of the CPimcTablet

#pragma once
#include<intsafe.h>
#include "resource.h"       // main symbols

#include "PenImc.h"
#include "PimcManager.h"
#include "ComLockableWrapper.hpp"
#include "GitComLockableWrapper.hpp"

/////////////////////////////////////////////////////////////////////////////
// CPimcTablet

class ATL_NO_VTABLE CPimcTablet :
    public CComObjectRootEx<CComSingleThreadModel>,
    public CComCoClass<CPimcTablet, &CLSID_PimcTablet3>,
    public IPimcTablet3
{
public:
    /////////////////////////////////////////////////////////////////////////

    CPimcTablet();
    HRESULT FinalConstruct() { return S_OK; };
    void    FinalRelease();

    // DDVSO:174153
    // Cleanup initialization to use CComPtr throughout.  This fixes COM reference count issues
    // that arise due to conversion from round-tripping conversions from CComPtr to raw and back.
    HRESULT Init(__in CComPtr<ITablet> pTabS, __in CComPtr<CPimcManager> pMgr);
    void    ReleaseCursorInfo();
    void    ReleasePacketDescription();

    STDMETHOD(GetKey)(__out INT * pKey);
    STDMETHOD(GetName)(__out LPWSTR * ppszName);
    STDMETHOD(GetPlugAndPlayId)(__out LPWSTR * ppszPlugAndPlayId);
    STDMETHOD(GetTabletAndDisplaySize)(__out INT * piTabletWidth, __out INT * piTabletHeight, __out INT * piDisplayWidth, __out INT * piDisplayHeight);
    STDMETHOD(GetHardwareCaps)(__out INT * pdwCaps);
    STDMETHOD(GetDeviceType)(__out INT * pKind);
    STDMETHOD(RefreshCursorInfo)();
    STDMETHOD(GetCursorCount)(__out INT * pcCursors);
    STDMETHOD(GetCursorInfo)(INT iCursor, __out LPWSTR * ppszName, __out INT * pId, __out BOOL * pfInverted);
    STDMETHOD(GetCursorButtonCount)(INT iCursor, __out INT * pcButtons);
    STDMETHOD(GetCursorButtonCountImpl)(INT iCursor, __out INT * pcButtons);
    STDMETHOD(GetCursorButtonInfo)(INT iCursor, INT iButton, __out LPWSTR * ppszName, __out GUID * pGuid);
    STDMETHOD(IsPropertySupported)(GUID guid, __out BOOL * pfSupported);
    STDMETHOD(GetPropertyInfo)(GUID guid, __out INT * piMin, __out INT * piMax, __out INT * piUnit, __out FLOAT *pflResolution);
    STDMETHOD(CreateContext)(__typefix(HWND) __in INT_PTR pwnd, BOOL fEnable, UINT uiTimeout, __deref_out IPimcContext3** ppCtx, __out INT * pId, __out INT64 *pCommHandle);
    STDMETHOD(GetPacketDescriptionInfo)(__out INT * pcProps, __out INT * pcButtons);
    STDMETHOD(GetPacketPropertyInfo)(INT iProp, __out GUID * pGuid, __out INT * piMin, __out INT * piMax, __out INT * piUnits, __out FLOAT *pflResolution);
    STDMETHOD(GetPacketButtonInfo)(INT iButton, __out GUID * pGuid);

    /////////////////////////////////////////////////////////////////////////

BEGIN_COM_MAP(CPimcTablet)
    COM_INTERFACE_ENTRY(IPimcTablet3)
END_COM_MAP()

    DECLARE_PROTECT_FINAL_CONSTRUCT()

    /////////////////////////////////////////////////////////////////////////

    struct CURSORBUTTONINFO
    {
        LPWSTR      pszName;
        GUID        guid;

        CURSORBUTTONINFO()
        {
            pszName = nullptr;
            ZeroMemory(&guid, sizeof(guid));
        }

        void Clear()
        {
            if (nullptr != pszName)
            {
                CoTaskMemFree(pszName);
                pszName = nullptr;
            }
        }
    };
    typedef CURSORBUTTONINFO * PCURSORBUTTONINFO;

    /////////////////////////////////////////////////////////////////////////

    struct CURSORINFO
    {
        LPWSTR              pszName;
        CURSOR_ID           id;
        BOOL                fInverted;
        INT                 cButtons;
        PCURSORBUTTONINFO * apButtonInfo;

        CURSORINFO()
        {
            pszName = nullptr;
            id = 0;
            fInverted = false;
            cButtons = 0;
            apButtonInfo = nullptr;
        }

        void Clear()
        {
            if (nullptr != pszName)
            {
                CoTaskMemFree(pszName);
                pszName = nullptr;
            }
            for (INT i = 0; i < cButtons; i++)
            {
                if (nullptr != apButtonInfo[i])
                {
                    apButtonInfo[i]->Clear();
                    delete apButtonInfo[i];
                    apButtonInfo[i] = nullptr;
                }
            }
            delete [] apButtonInfo;
        }
    };
    typedef CURSORINFO * PCURSORINFO;

    /////////////////////////////////////////////////////////////////////////

    CComPtr<CPimcManager> m_pMgr;
    CComPtr<ITablet>      m_pTabS;
    ComUtils::GitComLockableWrapper<ITablet> m_wispTabletLock;
    DWORD                 m_cCursors;
    PCURSORINFO *         m_apCursorInfo;
    TABLET_CONTEXT_SETTINGS * m_pTCS;
    ComUtils::ComLockableWrapper m_tabletLock;

    // DDVSO:514949
    // Special param flags for COM operations in GetCursorButtonCount
    const static int RELEASE_TABLET_EXT = -1;
    const static int QUERY_WISP_TABLET_KEY = -2;
    const static int QUERY_WISP_MANAGER_KEY = -3;
    const static int LOCK_TABLET_EXT = -4;
};


