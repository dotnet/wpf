// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// PimcTablet.cpp : Implementation of CPimcTablet

#include "stdafx.h"
#include "PimcContext.h"
#include "PimcTablet.h"
#include <strsafe.h>
#include <intsafe.h>

using namespace ComUtils;

#if WANT_PROFILING
const INT  s_cGuidsProfiling = 2;
#endif // WANT_PROFILING
const INT  s_cGuids = 5;
GUID s_guids[s_cGuids] = {
    {0x598A6A8F, 0x52C0, 0x4BA0, 0x93, 0xAF, 0xAF, 0x35, 0x74, 0x11, 0xA5, 0x61}, // GUID_X
    {0xB53F9F75, 0x04E0, 0x4498, 0xA7, 0xEE, 0xC3, 0x0D, 0xBB, 0x5A, 0x90, 0x11},  // GUID_Y
    {0x6E0E07BF, 0xAFE7, 0x4CF7, 0x87, 0xD1, 0xAF, 0x64, 0x46, 0x20, 0x84, 0x18},  // GUID_PACKETSTATUS
    {0x39143d3, 0x78cb, 0x449c, 0xa8, 0xe7, 0x67, 0xd1, 0x88, 0x64, 0xc3, 0x32},  // GUID_TIPBUTTON
    {0xf0720328, 0x663b, 0x418f, 0x85, 0xa6, 0x95, 0x31, 0xae, 0x3e, 0xcd, 0xfa}  // GUID_BARRELBUTTON
};

// s_guids is used as is in some places as TABLET_CONTEXT_SETTINGS->pguidPktProps.
// NormalPressure is an optional property and should not be included in s_guids array
// as a generic entry. Hence a separate constant.
static const GUID GUID_NORMALPRESSURE = {0x7307502D, 0xF9F4, 0x4E18, {0xB3, 0xF2, 0x2C, 0xE1, 0xB1, 0xA3, 0x61, 0x0C}};

typedef enum GUID_INDEXES
{
	GUID_X = 0,
	GUID_Y,
	GUID_PACKETSTATUS,
	GUID_TIPBUTTON,
	GUID_BARRELBUTTON
} GUID_INDEXES;


// Fake Mouse Device constants
static const WCHAR* MOUSEDEVICE_CURSOR_NAME         = L"Mouse";
static const WCHAR* MOUSEDEVICE_BUTTON_ONE_NAME     = L"Tip Switch";
static const WCHAR* MOUSEDEVICE_BUTTON_TWO_NAME    = L"Barrel Switch";
static const WCHAR* MOUSEDEVICE_PLUGANDPLAYID    = L"SCREEN";

static void EnsureNoDuplicateGUIDs(__in GUID *pGUID, __inout ULONG &cGUID)
{
    ULONG iIndex = 0;

    // Move all the unique guids to the beginning of the buffer.
    for (ULONG i = 0; i < cGUID; i++)
    {
        ULONG j = 0;
        for (; j < iIndex; j++)
        {
            if (pGUID[i] == pGUID[j])
            {
                break;
            }
        }
        if (j == iIndex)
        {
            pGUID[iIndex++] = pGUID[i];
        }
    }

    // Set empty guid at left over spots
    for (ULONG i = iIndex; i < cGUID; i++)
    {
        pGUID[i] = GUID_NULL;
    }

    // Fix the count
    cGUID = iIndex;
}

// Helper routine to remove duplicate entries from TABLET_CONTEXT_SETTINGS'
// pguidPktProps and pguidPktBtns
static void EnsureNoDuplicates(__in TABLET_CONTEXT_SETTINGS * pTCS)
{
    EnsureNoDuplicateGUIDs(pTCS->pguidPktProps, pTCS->cPktProps);
    EnsureNoDuplicateGUIDs(pTCS->pguidPktBtns, pTCS->cPktBtns);
}

// Helper routine to sort TABLET_CONTEXT_SETTINGS->pguidPktProps such that
// X, Y and NormalPressure are always at the beginning in that order.
static void EnsureXYPressureOrder(__in TABLET_CONTEXT_SETTINGS * pTCS)
{
    bool bFoundX = FALSE;
    bool bFoundY = FALSE;
    bool bFoundPressure = FALSE;

    ULONG iIncreament = 0;
    // Guard against integer underflow
    if (pTCS->cPktProps > 0) 
    {
		ULONG i = pTCS->cPktProps-1;
		do {
			if (pTCS->pguidPktProps[i] == s_guids[GUID_X])
            {
                bFoundX = TRUE;
                iIncreament++;
            }
            else if (pTCS->pguidPktProps[i] == s_guids[GUID_Y])
            {
                bFoundY = TRUE;
                iIncreament++;
            }
            else if (pTCS->pguidPktProps[i] == GUID_NORMALPRESSURE)
            {
                bFoundPressure = TRUE;
                iIncreament++;
            }
            else
            {
                // Move other guids to right by an index equal to number of
                // guids among X,Y and NormalPressure encountered so far.
                pTCS->pguidPktProps[i + iIncreament] = pTCS->pguidPktProps[i];
            }
		} while (i-- > 0);
    }

    // Set X, Y and NormalPressure at their appropriate indices.
    if (bFoundPressure)
    {
        pTCS->pguidPktProps[--iIncreament] = GUID_NORMALPRESSURE;
    }
    if (bFoundY)
    {
        pTCS->pguidPktProps[--iIncreament] = s_guids[GUID_Y];
    }
    if (bFoundX)
    {
        pTCS->pguidPktProps[--iIncreament] = s_guids[GUID_X];
    }
}

/////////////////////////////////////////////////////////////////////////////
// CPimcTablet

/////////////////////////////////////////////////////////////////////////////

CPimcTablet::CPimcTablet()
{
    m_cCursors = 0;
    m_apCursorInfo = NULL;
    m_pTCS = NULL;
    m_pMgr = NULL;
}

/////////////////////////////////////////////////////////////////////////////

HRESULT CPimcTablet::Init(__in CComPtr<ITablet> pTabS, __in CComPtr<CPimcManager> pMgr)
{
    DHR;
    m_pMgr = pMgr;
    m_pTabS = pTabS;

    // Ensure the WISP tablet is stored in the GIT.
    m_wispTabletLock = GitComLockableWrapper<ITablet>(m_pTabS, ComApartmentVerifier::Mta());
    CHR(m_wispTabletLock.CheckCookie());

    // Prefetch packet description info so we don't have to call wisp later for it.
    // This avoids reentrancy issues with doing an Out Of Proc COM call.
    INT cProps, cButtons;
    CHR(GetPacketDescriptionInfo(&cProps, &cButtons));
    CHR(RefreshCursorInfo());
CLEANUP:
    RHR;
}

/////////////////////////////////////////////////////////////////////////////

void CPimcTablet::FinalRelease()
{
    m_pMgr = nullptr;
    m_pTabS = nullptr;

    ReleaseCursorInfo();
    ReleasePacketDescription();

    m_wispTabletLock.RevokeIfValid();
}

/////////////////////////////////////////////////////////////////////////////

STDMETHODIMP CPimcTablet::CreateContext(__typefix(HWND) __in INT_PTR pwnd, BOOL fEnable, UINT uiTimeout, __deref_out IPimcContext3** ppCtx, __out INT * pId, __out INT64 *pCommHandle)
{
    // pCommHandle cannot be a INT_PTR. The reason being that INT_PTR (__int3264) always gets 
    // marshalled as a 32 bit value, which means in a 64 bit process we would lose the first half of the pointer
    // by the time it reaches the client. Instead this way we always pass a 64 bit value to the client
    // (irrespective of process bitness) so that nothing gets lost during marshalling.
                    
    DHR;
    HWND hwnd = (HWND)pwnd;
    CComPtr<ITabletContext>     pCtxS;
    CComObject<CPimcContext> *  pCtxC;
    TABLET_CONTEXT_ID           tcid;
    DWORD                       dwOptions = TCXO_CURSOR_STATE | TCXO_ALLOW_FLICKS | TCXO_ALLOW_FEEDBACK_TAPS | TCXO_ALLOW_FEEDBACK_BARREL | TCXO_REPORT_RECT_MAPPING_CHANGE;
    PACKET_DESCRIPTION *        pPacketDescription = NULL;
    TABLET_CONTEXT_SETTINGS *   pTCS = nullptr;

    // Make sure we use the default context settings if not already created.
    if (m_pTabS && !m_pTCS)
    {
        CHR(m_pTabS->GetDefaultContextSettings(&m_pTCS));
        EnsureNoDuplicates(m_pTCS);
        EnsureXYPressureOrder(m_pTCS);
        CHR(m_pTCS ? S_OK : MAKE_HRESULT(SEVERITY_ERROR, FACILITY_NULL, E_GETDEFAULTCONTEXT_CALL));
    }

    pTCS  = m_pTCS; // NULL;

    CHR(IsWindow(hwnd) ? S_OK : E_INVALIDARG);

#if WANT_PROFILE
    if (m_pMgr->IsProfiling())
    {
        CHR(m_pTabS->GetDefaultContextSettings(&pTCS));

        CoTaskMemFree(pTCS->pguidPktProps);
        CoTaskMemFree(pTCS->pguidPktBtns);
        CoTaskMemFree(pTCS->pdwBtnDnMask);
        CoTaskMemFree(pTCS->pdwBtnUpMask);

        pTCS->cPktProps     = s_cGuidsProfiling;
        pTCS->pguidPktProps = s_guids;
        pTCS->cPktBtns = 0;
        pTCS->pguidPktBtns = NULL;
        pTCS->pdwBtnDnMask = NULL;
        pTCS->pdwBtnUpMask = NULL;

        dwOptions = TCXO_DONT_VALIDATE_TCS | TCXO_DONT_SHOW_CURSOR;
    }
#endif // WANT_PROFILING

    CHR(CComObject<CPimcContext>::CreateInstance(&pCtxC));
    CHR(pCtxC->QueryInterface(IID_IPimcContext3, (void**)ppCtx));

    if (m_pTabS)
    {
        CHR(m_pTabS->CreateContext(
            hwnd,                               // hwnd
            NULL,                               // rc
            dwOptions,                          // options
            pTCS,                               // tablet context settings
            fEnable ? CONTEXT_ENABLE :
                    CONTEXT_DISABLE,         // enable type
            &pCtxS,                             // the ctx
            &tcid,                              // context id
            &pPacketDescription,                // packet description
            (ITabletEventSink*)pCtxC->m_sink // sink
            ));

        CHR(pCtxC->Init(m_pMgr, pCtxS, hwnd, tcid, pPacketDescription));
        pPacketDescription = NULL; // transfered ownership to the context
        CHR(pCtxC->GetKey(pId));  // really just grabs tcid so could avoid call but would have to add param validation.
        CHR(pCtxC->SetSingleFireTimeout(uiTimeout));
        CHR(pCtxC->GetCommHandle(pCommHandle)); // This adds a ref to keep pCtxC alive.
    }
    else
    {
        //need to fill in the context ///
        pPacketDescription = (PACKET_DESCRIPTION *)CoTaskMemAlloc (sizeof(PACKET_DESCRIPTION));
        CHR_MEMALLOC(pPacketDescription);

        // Fill in the packet properties.
        pPacketDescription->cbPacketSize = 3;
        pPacketDescription->cPacketProperties = 3;
        pPacketDescription->pPacketProperties = (PACKET_PROPERTY *)CoTaskMemAlloc (sizeof(PACKET_PROPERTY) * pPacketDescription->cbPacketSize);
        CHR_MEMALLOC(pPacketDescription->pPacketProperties);

        // X
        pPacketDescription->pPacketProperties[0].guid = s_guids[GUID_X];
        pPacketDescription->pPacketProperties[0].PropertyMetrics.nLogicalMin = LONG_MIN;
        pPacketDescription->pPacketProperties[0].PropertyMetrics.nLogicalMax = LONG_MAX;
        pPacketDescription->pPacketProperties[0].PropertyMetrics.Units = PROPERTY_UNITS_DEFAULT;
        pPacketDescription->pPacketProperties[0].PropertyMetrics.fResolution = 1.0f;

        // Y
        pPacketDescription->pPacketProperties[1].guid = s_guids[GUID_Y];
        pPacketDescription->pPacketProperties[1].PropertyMetrics.nLogicalMin = LONG_MIN;
        pPacketDescription->pPacketProperties[1].PropertyMetrics.nLogicalMax = LONG_MAX;
        pPacketDescription->pPacketProperties[1].PropertyMetrics.Units = PROPERTY_UNITS_DEFAULT;
        pPacketDescription->pPacketProperties[1].PropertyMetrics.fResolution = 1.0f;

        // PacketStatus
        pPacketDescription->pPacketProperties[2].guid = s_guids[GUID_PACKETSTATUS];
        pPacketDescription->pPacketProperties[2].PropertyMetrics.nLogicalMin = LONG_MIN;
        pPacketDescription->pPacketProperties[2].PropertyMetrics.nLogicalMax = LONG_MAX;
        pPacketDescription->pPacketProperties[2].PropertyMetrics.Units = PROPERTY_UNITS_DEFAULT;
        pPacketDescription->pPacketProperties[2].PropertyMetrics.fResolution =  1.0f;

        // Fill in button data....
        pPacketDescription->cButtons  = 2;
        pPacketDescription->pguidButtons = (GUID *)CoTaskMemAlloc (sizeof(GUID)*2);
        CHR_MEMALLOC(pPacketDescription->pguidButtons);
        pPacketDescription->pguidButtons[0] = s_guids[GUID_TIPBUTTON];
        pPacketDescription->pguidButtons[1] = s_guids[GUID_BARRELBUTTON];

        CHR(UIntPtrToULong(pwnd, &tcid));
        CHR(pCtxC->Init(m_pMgr, pCtxS, hwnd, tcid, pPacketDescription));
        pPacketDescription = NULL; // transfered ownership to the context
        CHR(pCtxC->GetKey(pId));
        // These calls are really not neccessary for mouse context.
        CHR(pCtxC->SetSingleFireTimeout(uiTimeout));
        CHR(pCtxC->GetCommHandle(pCommHandle)); // This adds a ref to keep pCtxC alive.
    }

CLEANUP:
    if (pPacketDescription)
    {
        CPimcContext::DestroyPacketDescription(pPacketDescription);
    }

    RHR;
}

/////////////////////////////////////////////////////////////////////////////

STDMETHODIMP CPimcTablet::GetKey(__out INT * pKey)
{
    DHR;
    CHR(pKey ? S_OK : E_INVALIDARG);
    *pKey = (INT)PtrToInt(m_pTabS.p);
CLEANUP:
    RHR;
}

/////////////////////////////////////////////////////////////////////////////

STDMETHODIMP CPimcTablet::GetName(__out LPWSTR * ppszName)
{
    DHR;
    LPWSTR pszNameCpy = NULL;
    HMONITOR hMonitor = nullptr;
    CHR(ppszName ? S_OK : E_INVALIDARG);
    *ppszName = NULL;

    if (m_pTabS)
    {
        // We ignore the result code because otherwise we will throw a COM exception
        // Invalid name does not mean invalid device.
        if (!SUCCEEDED(m_pTabS->GetName(ppszName)))
        {
            // Do not rely on failure = null behavior of underlying COM component.
            // We define failure = NULL here explicitly.
            ppszName = NULL;
        }
        goto CLEANUP;
    }

    // This is the same code that wisptis uses to determine the name of the Mouse device.
    // Since this is a not very common called API we don't cache the name.
    MONITORINFOEX MonitorInfoEx;
    MonitorInfoEx.cbSize = SIZEOFSTRUCT(MonitorInfoEx);
    hMonitor = MonitorFromWindow(GetDesktopWindow(), MONITOR_DEFAULTTOPRIMARY);
    CHR(hMonitor != NULL ? S_OK : MAKE_HRESULT(SEVERITY_ERROR, FACILITY_NULL, E_MONITORFROMWINDOW_CALL));
    CHR(GetMonitorInfo(hMonitor, &MonitorInfoEx) ? S_OK : MAKE_HRESULT(SEVERITY_ERROR, FACILITY_NULL, E_GETMONITORINFO_CALL));

    size_t cbName;
    CHR(StringCchLengthW(MonitorInfoEx.szDevice, STRSAFE_MAX_CCH, &cbName));
    CHR(SizeTAdd(cbName, 1, &cbName));
    CHR(SizeTMult(cbName, sizeof(WCHAR), &cbName));
    pszNameCpy = (LPWSTR)CoTaskMemAlloc (cbName);
    CHR_MEMALLOC(pszNameCpy);
    CHR(StringCbCopy(pszNameCpy, cbName, MonitorInfoEx.szDevice));

    *ppszName = pszNameCpy;
    pszNameCpy = NULL;

CLEANUP:
    if (pszNameCpy != NULL)
    {
        ::CoTaskMemFree(pszNameCpy);
    }
    RHR
}

/////////////////////////////////////////////////////////////////////////////

STDMETHODIMP CPimcTablet::GetPlugAndPlayId(__out LPWSTR * ppszPlugAndPlayId)
{
    DHR;
    if (m_pTabS)
        return m_pTabS->GetPlugAndPlayId(ppszPlugAndPlayId);
    else
    {
        // mousetab.cpp in wisptis is hard coded to return "SCREEN" for the mouse device.
        size_t cbName;
        CHR(StringCchLength(MOUSEDEVICE_PLUGANDPLAYID, STRSAFE_MAX_CCH, &cbName));
        CHR(SizeTAdd(cbName, 1, &cbName));
        CHR(SizeTMult(cbName, sizeof(WCHAR), &cbName));
        
        *ppszPlugAndPlayId = (LPWSTR)CoTaskMemAlloc (cbName);
        CHR_MEMALLOC(*ppszPlugAndPlayId);
        return StringCbCopy(*ppszPlugAndPlayId, cbName, MOUSEDEVICE_PLUGANDPLAYID);
    }
    CLEANUP:
    RHR
}

/////////////////////////////////////////////////////////////////////////////

STDMETHODIMP CPimcTablet::GetTabletAndDisplaySize(__out INT * piTabletWidth, __out INT * piTabletHeight, __out INT * piDisplayWidth, __out INT * piDisplayHeight)
{
    DHR;
    RECT rcTablet = {0,0,0,0};
    CHR(piTabletWidth ? S_OK : E_INVALIDARG);
    CHR(piTabletHeight ? S_OK : E_INVALIDARG);
    CHR(piDisplayWidth ? S_OK : E_INVALIDARG);
    CHR(piDisplayHeight ? S_OK : E_INVALIDARG);
    
    if (m_pTabS)
    {
        // First get tablet info...
        CHR(m_pTabS->GetMaxInputRect(&rcTablet));
        *piTabletWidth = rcTablet.right - rcTablet.left;
        *piTabletHeight = rcTablet.bottom - rcTablet.top;

        // Now get the display info...
        
        // First see if we have Vista wisptis that supports the new method
        // that supports mapping integrated digitizers to displays.
        CComQIPtr<ITablet2, &IID_ITablet2> spTablet2(m_pTabS);
        if (nullptr != spTablet2)
        {
            RECT rcScreen;
            CHR(spTablet2->GetMatchingScreenRect(&rcScreen));
            *piDisplayWidth = rcScreen.right - rcScreen.left;
            *piDisplayHeight = rcScreen.bottom - rcScreen.top;
            goto CLEANUP; // we're done.
        }

        // otherwise figure things out using the XP logic which maps to primary monitor 
        // always for integrated digitizers.
        int iHwCaps = 0;
        CHR(GetHardwareCaps(&iHwCaps));

        // See if we are integrated.
        if ((iHwCaps & THWC_INTEGRATED) != 0)
        {
            // integrated, so use primary monitor rect.
            HMONITOR hMonitor = MonitorFromWindow(GetDesktopWindow(), MONITOR_DEFAULTTOPRIMARY);
            if (hMonitor != NULL)
            {
                MONITORINFOEX monitorInfo;
                monitorInfo.cbSize = sizeof(MONITORINFOEX);
                GetMonitorInfo(hMonitor, &monitorInfo);
                *piDisplayWidth = monitorInfo.rcMonitor.right - monitorInfo.rcMonitor.left;
                *piDisplayHeight = monitorInfo.rcMonitor.bottom - monitorInfo.rcMonitor.top;
                goto CLEANUP;
            }
        }
        
        // If we fail above then just do non integrated code.
        // non integrated so use desktop rect.
        *piDisplayWidth = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        *piDisplayHeight = GetSystemMetrics(SM_CYVIRTUALSCREEN);
    }
    else
    {
        // By default just return same for tablet and display (no scaling).
        *piTabletWidth = *piDisplayWidth = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        *piTabletHeight = *piDisplayHeight = GetSystemMetrics(SM_CYVIRTUALSCREEN);
    }
    
CLEANUP:
    RHR;
}

/////////////////////////////////////////////////////////////////////////////

STDMETHODIMP CPimcTablet::GetHardwareCaps(__out INT * pCaps)
{
    DHR;
    DWORD dwCaps;
    CHR(pCaps ? S_OK : E_INVALIDARG);
    if (m_pTabS)
    {
        CHR(m_pTabS->GetHardwareCaps(&dwCaps));
        *pCaps = (INT)dwCaps;
    }
    else
    {
        // return the data for our 'fake mouse'
        *pCaps = (INT)0x2; //StylusMustTouch
    }
CLEANUP:
    RHR;
}

/////////////////////////////////////////////////////////////////////////////


/////////////////////////////////////////////////////////
STDMETHODIMP CPimcTablet::GetDeviceType(__out INT * pKind)
{
    HRESULT hr = S_OK;
    LPWSTR pszName = NULL;
    CHR(pKind ? S_OK : E_INVALIDARG);
    *pKind = 0;

    if (m_pTabS)
    {
        CComQIPtr<ITablet2, &IID_ITablet2> spTablet2(m_pTabS);
        if (nullptr != spTablet2)
        {
           TABLET_DEVICE_KIND kind;
           hr = spTablet2->GetDeviceKind(&kind);
           if (SUCCEEDED(hr))
           {
               *pKind = (INT)kind;
               goto CLEANUP;
           }
        }
    }

    hr = GetName(&pszName);
    if (SUCCEEDED(hr))
    {
        *pKind = (NULL == wcsstr(pszName, L"\\\\.\\DISPLAY") ? 1 /*Pen*/: 0 /*Mouse*/);
    }

CLEANUP:
    ::CoTaskMemFree(pszName);
    RHR;
}



/////////////////////////////////////////////////////////////////////////////

STDMETHODIMP CPimcTablet::RefreshCursorInfo()
{
    DHR;
    CComPtr<ITabletCursor>          pCursorS;
    CComPtr<ITabletCursorButton>    pButtonS;

    ReleaseCursorInfo();

    if (m_pTabS)
    {
        CHR(m_pTabS->GetCursorCount(&m_cCursors));
        m_apCursorInfo = new PCURSORINFO[m_cCursors]();

        CHR(m_apCursorInfo ? S_OK : E_OUTOFMEMORY);

        for (DWORD iCursor = 0; iCursor < m_cCursors; iCursor++)
        {
            CHR(m_pTabS->GetCursor(iCursor, &pCursorS));

            PCURSORINFO pCursorInfo = new CURSORINFO();
            CHR(pCursorInfo ? S_OK : E_OUTOFMEMORY);
    #pragma prefast( suppress: 11, "Dereferencing NULL pointer 'm_apCursorInfo'." )
            m_apCursorInfo[iCursor] = pCursorInfo;

            CHR(pCursorS->GetName(&pCursorInfo->pszName));
            CHR(pCursorS->GetId  (&pCursorInfo->id));
            HRESULT hrInverted = pCursorS->IsInverted();
            CHR(hrInverted);
            pCursorInfo->fInverted = hrInverted == S_OK;

            DWORD cButtons;
            CHR(pCursorS->GetButtonCount(&cButtons));
            pCursorInfo->cButtons = cButtons;
            pCursorInfo->apButtonInfo = new PCURSORBUTTONINFO[cButtons]();

            for (DWORD iButton = 0; iButton < cButtons; iButton++)
            {
                CHR(pCursorS->GetButton(iButton, &pButtonS));

                PCURSORBUTTONINFO pButtonInfo = new CURSORBUTTONINFO();
                CHR(pButtonInfo ? S_OK : E_OUTOFMEMORY);
    #pragma prefast( suppress: 11, "Dereferencing NULL pointer 'pCursorInfo'." )
                pCursorInfo->apButtonInfo[iButton] = pButtonInfo;

                CHR(pButtonS->GetName(&pButtonInfo->pszName));
                CHR(pButtonS->GetGuid(&pButtonInfo->guid));

                // The smart pointer should be smart enough.
    #pragma prefast( suppress: 416, "Dereferencing NULL smart pointer 'pButtonS'." )
                pButtonS = nullptr;
            }

            pCursorS = nullptr;
        }
    }
    else
    {
        // fake it up for a mouse...
        m_cCursors = 1;
        m_apCursorInfo = new PCURSORINFO[m_cCursors]();
        CHR(m_apCursorInfo ? S_OK : E_OUTOFMEMORY);

        PCURSORINFO pCursorInfo = new CURSORINFO();
        CHR(pCursorInfo ? S_OK : E_OUTOFMEMORY);
   #pragma prefast( suppress: 11, "Dereferencing NULL pointer 'm_apCursorInfo'." )
        m_apCursorInfo[0] = pCursorInfo;

            
        size_t cbName;
        CHR(StringCchLength(MOUSEDEVICE_CURSOR_NAME, STRSAFE_MAX_CCH, &cbName));
        CHR(SizeTAdd(cbName, 1, &cbName));
        CHR(SizeTMult(cbName, sizeof(WCHAR), &cbName));

        pCursorInfo->pszName = (LPWSTR)CoTaskMemAlloc (cbName);
        CHR_MEMALLOC(pCursorInfo->pszName);
        StringCbCopy(pCursorInfo->pszName, cbName, MOUSEDEVICE_CURSOR_NAME);

        pCursorInfo->id = 1; // default for mouse device
        pCursorInfo->fInverted = false;

        int cButtons = 2; // there are two buttons for a mouse...
        pCursorInfo->cButtons = cButtons;
        pCursorInfo->apButtonInfo = new PCURSORBUTTONINFO[cButtons]();

        //Get some memory for the button info...
        for(int i=0; i<cButtons; i++)
        {
            PCURSORBUTTONINFO pButtonInfo = new CURSORBUTTONINFO();
            CHR(pButtonInfo ? S_OK : E_OUTOFMEMORY);
#pragma prefast( suppress: 11, "Dereferencing NULL pointer 'pCursorInfo'." )

           // Fill in the name and guid for the two buttons...
            if (i == 0)
            {
                CHR(StringCchLength(MOUSEDEVICE_BUTTON_ONE_NAME, STRSAFE_MAX_CCH, &cbName));
                CHR(SizeTAdd(cbName, 1, &cbName));
                CHR(SizeTMult(cbName, sizeof(WCHAR), &cbName));
                pButtonInfo->pszName = (LPWSTR)CoTaskMemAlloc (cbName);
                CHR_MEMALLOC(pButtonInfo->pszName);
                StringCbCopy(pButtonInfo->pszName, cbName, MOUSEDEVICE_BUTTON_ONE_NAME);
                pButtonInfo->guid   = s_guids[GUID_TIPBUTTON];
            }
            else
            {
                CHR(StringCchLength(MOUSEDEVICE_BUTTON_TWO_NAME, STRSAFE_MAX_CCH, &cbName));
                CHR(SizeTAdd(cbName, 1, &cbName));
                CHR(SizeTMult(cbName, sizeof(WCHAR), &cbName));
                
                pButtonInfo->pszName = (LPWSTR)CoTaskMemAlloc (cbName);
                CHR_MEMALLOC(pButtonInfo->pszName);
                StringCbCopy(pButtonInfo->pszName, cbName, MOUSEDEVICE_BUTTON_TWO_NAME);
                pButtonInfo->guid = s_guids[GUID_BARRELBUTTON];
            }
            pCursorInfo->apButtonInfo[i] = pButtonInfo;
        }
    }

CLEANUP:

    if ( FAILED(hr) )
    {
        ReleaseCursorInfo();
    }

    RHR;
}

/////////////////////////////////////////////////////////////////////////////

void CPimcTablet::ReleaseCursorInfo()
{
    if (m_cCursors > 0)
    {
        if (nullptr != m_apCursorInfo)
        {
            for (DWORD iCursor = 0; iCursor < m_cCursors; iCursor++)
            {
                if (nullptr != m_apCursorInfo[iCursor])
                {
                    m_apCursorInfo[iCursor]->Clear();
                    delete m_apCursorInfo[iCursor];
                    m_apCursorInfo[iCursor] = nullptr;
                }
            }
            delete [] m_apCursorInfo;
            m_apCursorInfo = nullptr;
        }
        m_cCursors = 0;
    }
}

/////////////////////////////////////////////////////////////////////////////

STDMETHODIMP CPimcTablet::GetCursorCount(__out INT * pcCursors)
{
    DHR;
    CHR(pcCursors ? S_OK : E_INVALIDARG);
    *pcCursors = m_cCursors;
CLEANUP:
    RHR;
}

/////////////////////////////////////////////////////////////////////////////

STDMETHODIMP CPimcTablet::GetCursorInfo(INT iCursor, __out LPWSTR * ppszName, __out INT * pId, __out BOOL * pfInverted)
{
    size_t cbName;
    PCURSORINFO pCursorInfo = nullptr;
    DHR;

    CHR(iCursor >= 0 ? S_OK : E_INVALIDARG);
    CHR((DWORD)iCursor < m_cCursors ? S_OK : E_INVALIDARG);
    CHR(ppszName   ? S_OK : E_INVALIDARG);
    CHR(pId        ? S_OK : E_INVALIDARG);
    CHR(pfInverted ? S_OK : E_INVALIDARG);

    // iCursor value is checked above, disable prefast signedness warning
#pragma prefast(suppress: 37001 37002 37003)
    pCursorInfo = m_apCursorInfo[iCursor];
    CHR(StringCchLength(pCursorInfo->pszName, STRSAFE_MAX_CCH, &cbName));
    CHR(SizeTAdd(cbName, 1, &cbName));
    CHR(SizeTMult(cbName, sizeof(WCHAR), &cbName));
    *ppszName = (LPWSTR)CoTaskMemAlloc (cbName);
    CHR_MEMALLOC(*ppszName);
    StringCbCopy(*ppszName, cbName, pCursorInfo->pszName);

    *pId        = pCursorInfo->id;
    *pfInverted = pCursorInfo->fInverted;

CLEANUP:
    RHR;
}

/////////////////////////////////////////////////////////////////////////////

STDMETHODIMP CPimcTablet::GetCursorButtonCount(INT iCursor, __out INT * pcButtons)
{
    DHR;

    switch (iCursor)
    {
        case LOCK_TABLET_EXT:
        {
            // DDVSO:514949
            // Calling this ensures that the CStdIdentity for this IPimcTablet3 is
            // not released if we hit a COM rundown due to OSGVSO:10779198.
            m_tabletLock = ComLockableWrapper(this, ComApartmentVerifier::CurrentSta());
            CHR(m_tabletLock.Lock());
        }
        break;
        case RELEASE_TABLET_EXT:
        {
            CHR(m_tabletLock.Unlock());
        }
        break;
        case QUERY_WISP_TABLET_KEY:
        {
            if (nullptr == pcButtons)
            {
                CHR(E_INVALIDARG);
            }
            else
            {
                *pcButtons = m_wispTabletLock.GetCookie();
            }
        }
        break;
        case QUERY_WISP_MANAGER_KEY:
        {
            if (nullptr == pcButtons)
            {
                CHR(E_INVALIDARG);
            }
            else
            {
                *pcButtons = m_pMgr->m_wispManagerLock.GetCookie();
            }
        }
        break;
        default:
        {
            CHR(GetCursorButtonCountImpl(iCursor, pcButtons));
        }
    }

CLEANUP:
    RHR;
}

STDMETHODIMP CPimcTablet::GetCursorButtonCountImpl(INT iCursor, __out INT * pcButtons)
{
    DHR;
    CHR(iCursor >= 0 ? S_OK : E_INVALIDARG);
    CHR((DWORD)iCursor < m_cCursors ? S_OK : E_INVALIDARG);
    CHR(pcButtons ? S_OK : E_INVALIDARG);
    // iCursor value is checked above, disable prefast signedness warning
#pragma prefast(suppress: 37001 37002 37003)
    *pcButtons = m_apCursorInfo[iCursor]->cButtons;
CLEANUP:
    RHR;
}

/////////////////////////////////////////////////////////////////////////////

STDMETHODIMP CPimcTablet::GetCursorButtonInfo(INT iCursor, INT iButton, __out LPWSTR * ppszName, __out GUID * pGuid)
{
    DHR;
    size_t cbName = 0;
    LPWSTR pszNameSrc = nullptr;
    PCURSORBUTTONINFO pButtonInfo = nullptr;
    CHR(iCursor >= 0 ? S_OK : E_INVALIDARG);
    CHR((DWORD)iCursor < m_cCursors ? S_OK : E_INVALIDARG);
    CHR(iButton >= 0 ? S_OK : E_INVALIDARG);
    
        // iCursor is checked for underflow above, disable prefast signedness warnings
#pragma prefast(suppress: 37001 37002 37003)
    CHR(iButton < m_apCursorInfo[iCursor]->cButtons ? S_OK : E_INVALIDARG);
    CHR(ppszName ? S_OK : E_INVALIDARG);
    CHR(pGuid    ? S_OK : E_INVALIDARG);

    // iButton and iCursor are checked for underflow above, disable prefast signedness warnings
#pragma prefast(suppress: 37001 37002 37003)
    pButtonInfo =  m_apCursorInfo[iCursor]->apButtonInfo[iButton];

    pszNameSrc = pButtonInfo->pszName;
    CHR(StringCchLength(pszNameSrc, STRSAFE_MAX_CCH, &cbName));
    CHR(SizeTAdd(cbName, 1, &cbName));
    CHR(SizeTMult(cbName, sizeof(WCHAR), &cbName));
    *ppszName = (LPWSTR)CoTaskMemAlloc (cbName);
    CHR_MEMALLOC(*ppszName);
    StringCbCopy(*ppszName, cbName, pszNameSrc);

    *pGuid = pButtonInfo->guid;

CLEANUP:
    RHR;
}

/////////////////////////////////////////////////////////////////////////////

STDMETHODIMP CPimcTablet::IsPropertySupported(GUID guid, __out BOOL * pfSupported)
{
    DHR;
    CHR(pfSupported ? S_OK : E_INVALIDARG);
    PROPERTY_METRICS    metric;
    *pfSupported = S_OK == m_pTabS->GetPropertyMetrics(guid, &metric);
CLEANUP:
    RHR;
}

/////////////////////////////////////////////////////////////////////////////

STDMETHODIMP CPimcTablet::GetPropertyInfo(GUID guid, __out INT * piMin, __out INT * piMax, __out INT * piUnit, __out FLOAT *pflResolution)
{
    DHR;
    PROPERTY_METRICS    metric;
    CHR(piMin         ? S_OK : E_INVALIDARG);
    CHR(piMax         ? S_OK : E_INVALIDARG);
    CHR(piUnit        ? S_OK : E_INVALIDARG);
    CHR(pflResolution ? S_OK : E_INVALIDARG);
    CHR(m_pTabS->GetPropertyMetrics(guid, &metric));
    *piMin         = metric.nLogicalMin;
    *piMax         = metric.nLogicalMax;
    *piUnit        = metric.Units;
    *pflResolution = metric.fResolution;
CLEANUP:
    RHR;
}

/////////////////////////////////////////////////////////////////////////////

STDMETHODIMP CPimcTablet::GetPacketDescriptionInfo(__out INT * pcProps, __out INT * pcButtons)
{
    DHR;
    CHR(pcProps ? S_OK : E_INVALIDARG);
    CHR(pcButtons ? S_OK : E_INVALIDARG);

    if (m_pTabS)
    {
        if (!m_pTCS)
        {
            CHR(m_pTabS->GetDefaultContextSettings(&m_pTCS));
            EnsureNoDuplicates(m_pTCS);
            EnsureXYPressureOrder(m_pTCS);
            CHR(m_pTCS ? S_OK : MAKE_HRESULT(SEVERITY_ERROR, FACILITY_NULL, E_GETDEFAULTCONTEXT_CALL));
        }

        *pcProps   = m_pTCS->cPktProps;
        *pcButtons = m_pTCS->cPktBtns;
    }
    else
    {
        // No wisptis case, so return mouse settings
        *pcProps   = 3;
        *pcButtons = 2;
    }

CLEANUP:
    RHR;
}

/////////////////////////////////////////////////////////////////////////////

STDMETHODIMP CPimcTablet::GetPacketPropertyInfo(INT iProp, __out GUID * pGuid, __out INT * piMin, __out INT * piMax, __out INT * piUnits, __out FLOAT *pflResolution)
{
    DHR;

    CHR(0 <= iProp && (DWORD)iProp < (m_pTCS ? m_pTCS->cPktProps : 3) ? S_OK : E_INVALIDARG);
    CHR(pGuid         ? S_OK : E_INVALIDARG);
    CHR(piMin         ? S_OK : E_INVALIDARG);
    CHR(piMax         ? S_OK : E_INVALIDARG);
    CHR(piUnits        ? S_OK : E_INVALIDARG);
    CHR(pflResolution ? S_OK : E_INVALIDARG);

    // iProp is checked for overflow/underflow above, disable prefast signedness warnings
#pragma prefast(suppress: 37001 37002 37003)
    *pGuid          = m_pTCS ? m_pTCS->pguidPktProps[iProp] : s_guids[iProp];
    *piMin          = 0; // pProp->PropertyMetrics.nLogicalMin;
    *piMax          = 0; // pProp->PropertyMetrics.nLogicalMax;
    *piUnits        = 0; // pProp->PropertyMetrics.Units;
    *pflResolution  = 0.0f; // pProp->PropertyMetrics.fResolution;
CLEANUP:
    RHR;
}

/////////////////////////////////////////////////////////////////////////////

STDMETHODIMP CPimcTablet::GetPacketButtonInfo(INT iButton, __out GUID * pGuid)
{
    DHR;
    CHR(0 <= iButton && (DWORD)iButton < (m_pTCS ? m_pTCS->cPktBtns : 2) ? S_OK : E_INVALIDARG);
    CHR(pGuid ? S_OK : E_INVALIDARG);

    // Value of iButton is checked above. Since iButton is within known limits we assume the addition of 3 is accounted
    // for and will not produce overflow. Disable prefast warnings
#pragma prefast(suppress: 37001 37002 37003)
    *pGuid = m_pTCS ?
                      m_pTCS->pguidPktBtns[iButton] : // if we have context descr
                      s_guids[3+iButton]; // TipButton or BarrelButton equals index 3 or 4
CLEANUP:
    RHR;
}


/////////////////////////////////////////////////////////////////////////////

void CPimcTablet::ReleasePacketDescription()
{
    if (m_pTCS)
    {
        if (m_pTCS->pguidPktProps)
            CoTaskMemFree(m_pTCS->pguidPktProps);
        if (m_pTCS->pguidPktBtns)
            CoTaskMemFree(m_pTCS->pguidPktBtns);
        if (m_pTCS->pdwBtnDnMask)
            CoTaskMemFree(m_pTCS->pdwBtnDnMask);
        if (m_pTCS->pdwBtnUpMask)
            CoTaskMemFree(m_pTCS->pdwBtnUpMask);

        CoTaskMemFree(m_pTCS);
        m_pTCS = NULL;
    }
}


