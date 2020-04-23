// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#ifndef __PENINC_H_
#define __PENINC_H_

#define STRSAFE_NO_DEPRECATE
#include <strsafe.h>
#include <tabinc.h>

///////////////////////////////////////////////////////////////////////////////

#define MIN_SPACE64_X           0
#define MIN_SPACE64_Y           0
#define MAX_SPACE64_X           65535
#define MAX_SPACE64_Y           65535

///////////////////////////////////////////////////////////////////////////////

typedef unsigned (__stdcall *PTHREAD_START) (void *);

#define chBEGINTHREADEX(lpsa, cbStack, lpStartAddr, \
   lpvThreadParm, fdwCreate, lpIDThread)            \
      ((HANDLE)_beginthreadex(                      \
         (void *) (lpsa),                           \
         (unsigned) (cbStack),                      \
         (PTHREAD_START) (lpStartAddr),             \
         (void *) (lpvThreadParm),                  \
         (unsigned) (fdwCreate),                    \
         (unsigned *) (lpIDThread)))

///////////////////////////////////////////////////////////////////////////////

#define MICROSOFT_TABLETPENSERVICE_PROPERTY _T("MicrosoftTabletPenServiceProperty")

#define WISPTIS_PRESS_AND_HOLD_DISABLE_MASK     0x01
#define WISPTIS_SYSTEM_GESTURE_WM_DISABLE_MASK  0x02
#define WISPTIS_FLICK_LEARNING_MODE_MASK        0x04

#define PENPROCESS_COMMANDLINE              _T("/ProcessActivate:%p;%p; /ProcessDeActivate:%p;%p; /EndSessionInfo:%p;%p;")

#define PENPROCESS_ACTIVATEINFO             _T("/ProcessActivate:")
#define PENPROCESS_DEACTIVATEINFO           _T("/ProcessDeActivate:")
#define PENPROCESS_ENDSESSIONINFO           _T("/EndSessionInfo:")

#define PENPROCESS_PATH                     _T("\\SYSTEM32\\WISPTIS.EXE")

#define WISPTIS_WITHNOINTEGRATEDDEVICE  _T("/EndSessionInfo:%p;%p;")
#define WISPTIS_ENDSESSIONINFO          _T("/EndSessionInfo:")

#define WISPTIS_DEBUGGING               _T("/Debugging")
#define WISPTIS_DEBUGGING                   _T("/Debugging")

/////////////////////////////////////////////////////////////////////////////
//
// HR etc helpers
//

#define DHR                                         \
    HRESULT hr = S_OK;

#define RHR                                         \
    return hr;

#define CHR(hr_op)                                  \
    {                                               \
        hr = hr_op;                                 \
        if (FAILED(hr))                             \
            goto CLEANUP;                           \
    }

#define CHR_VERIFY(hr_op)                           \
    {                                               \
        CHR(hr_op);                                \
        ASSERT (SUCCEEDED(hr));                     \
    }

#define CHR_MEMALLOC(pv_op)                         \
    {                                               \
        CHR((pv_op) != NULL ? S_OK : E_OUTOFMEMORY); \
    }

#define CHR_WIN32(bool_or_handle_op)                \
    {                                               \
        CHR((bool_or_handle_op) ?                   \
            S_OK :                                  \
            HRESULT_FROM_WIN32(GetLastError()));    \
    }

// Shared by Wisptis and PenImc
#define WISPTIS_SM_MORE_DATA_EVENT_NAME     _T("wisptis-1-%d-%u")
#define WISPTIS_SM_MUTEX_NAME               _T("wisptis-2-%d-%u")
#define WISPTIS_SM_SECTION_NAME             _T("wisptis-3-%d-%u")
#define WISPTIS_SM_THREAD_EVENT_NAME        _T("wisptis-4-%u")

#endif  // __PENINC_H_

