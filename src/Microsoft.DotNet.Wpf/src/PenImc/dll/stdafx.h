// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently,
// but are changed infrequently

#pragma once

#ifndef STRICT
#define STRICT
#endif

// Modify the following defines if you have to target a platform prior to the ones specified below.
// Refer to MSDN for the latest info on corresponding values for different platforms.
#ifndef WINVER				// Allow use of features specific to Windows 95 and Windows NT 4 or later.
#define WINVER 0x0501		// Change this to the appropriate value to target Windows 98 and Windows 2000 or later.
#endif

#ifndef _WIN32_WINNT		// Allow use of features specific to Windows XP or later.
#define _WIN32_WINNT 0x0501	// Change this to the appropriate value to target Windows XP or later.
#endif						

#ifndef _WIN32_WINDOWS		// Allow use of features specific to Windows 98 or later.
#define _WIN32_WINDOWS 0x0410 // Change this to the appropriate value to target Windows Me or later.
#endif

#ifndef _WIN32_IE			// Allow use of features specific to IE 4.0 or later.
#define _WIN32_IE 0x0400	// Change this to the appropriate value to target IE 5.0 or later.
#endif

/////////////////////////////////////////////////////////////////////////////

#define _ATL_SINGLE_THREADED // #define _ATL_APARTMENT_THREADED // #define _ATL_FREE_THREADED

#define _ATL_NO_AUTOMATIC_NAMESPACE

#define _ATL_CSTRING_EXPLICIT_CONSTRUCTORS	// some CString constructors will be explicit

#define _ATL_ALL_WARNINGS // turns off ATL's hiding of some common and often safely ignored warning messages

#include <wpfsdl.h>
#include "resource.h"
#include <atlbase.h>
#include <atlcom.h>

using namespace ATL;

#include <windows.h>

#define WM_UNINITMENUPOPUP              0x0125

/////////////////////////////////////////////////////////////////////////////

#include <peninc.h>
#include <wisptis.h>
#include <wisptics.h>

// #define DELIVERY_PROFILING   // DO NOT LEAVE ENABLED in the checked in code

#if 1 // from csutil.h //..WIP (alexz) proper includes
#define WISPTIS_SHAREDMEMORY_MAXPACKETS                 64

#define WISPTIS_SHAREDMEMORY_AVAILABLE                  0xFFFFFFFF

struct SHAREDMEMORY_HEADER
{
    DWORD               cbTotal;
    DWORD               cbOffsetSns;

    DWORD               idxEvent;
    DWORD               dwEvent;

    CURSOR_ID           cid;
    DWORD               sn;
    SYSTEM_EVENT        sysEvt;
    SYSTEM_EVENT_DATA   sysEvtData;
    DWORD               cPackets;
    DWORD               cbPackets;
    BOOL                fSnsPresent;

    void Clear()
    {
        INT cbUnclearable = 2 * sizeof(DWORD);
        ZeroMemory(((BYTE*)this) + cbUnclearable, sizeof(*this) - cbUnclearable);
    }
};
#endif

