// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//+-----------------------------------------------------------------------------
//

//
//  $TAG ENGR

//      $Module:    win_mil_graphics_media
//      $Keywords:
//
//  $Description:
//
//  $ENDTAG
//
//------------------------------------------------------------------------------

#include "precomp.hpp"
#include "milav.tmh"

//+-----------------------------------------------------------------------------
//
//  Function:
//      AvDllInitialize
//
//  Synopsis:
//      Initialize whatever is needed by media inside the MIL DLL.
//
//------------------------------------------------------------------------------
HRESULT
AvDllInitialize(
    void
    )
{
    HRESULT hr = S_OK;

    //
    // Initialize the critical section for the Media Player appartment thread.
    //
    IFC(CStateThread::Initialize());

Cleanup:

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Function:
//      AvDllShutdown
//
//  Synopsis:
//      Free media resources
//
//------------------------------------------------------------------------------
void
AvDllShutdown(
    void
    )
{
    CStateThread::FinalShutdown();

    WPP_CLEANUP();
}

// +---------------------------------------------------------------------------
//
// CMILAV::CreateMedia
//
// +---------------------------------------------------------------------------
HRESULT
CMILAV::
CreateMedia(
    __in        CEventProxy *pEventProxy,
    __in        bool        canOpenAnyMedia,
    __deref_out IMILMedia   **ppMedia
    )
{
    HRESULT hr = S_OK;
    MediaInstance       *pMediaInstance = NULL;

    TRACEFID(0, &hr);

    *ppMedia = NULL;

    IFC(MediaInstance::Create(pEventProxy, &pMediaInstance));

    IFC(ChoosePlayer(pMediaInstance, canOpenAnyMedia, ppMedia));

Cleanup:
    ReleaseInterface(pMediaInstance);

    EXPECT_SUCCESSID(0, hr);
    RRETURN(hr);
}

// +---------------------------------------------------------------------------
//
// CMILAV::ChoosePlayer
//
// +---------------------------------------------------------------------------
HRESULT
CMILAV::
ChoosePlayer(
    __in            MediaInstance       *pMediaInstance,
    __in            bool                canOpenAnyMedia,
    __deref_out     IMILMedia           **ppMedia
    )
{
    HRESULT hr = S_OK;
    TRACEFID(pMediaInstance->GetID(), &hr);
    CWmpPlayer      *pRealAvPlayer = NULL;

#if DBG
#if PRERELEASE
    bool fRealPlayer = true;
    HKEY hKey = NULL;
    CFakePP *pFakeAvPlayer = NULL;

    if (SUCCEEDED(GetAvalonRegistrySettingsKey(&hKey)))
    {
        DWORD dwType;
        DWORD dwEnableFakePP;
        DWORD dwDataSize = 4;

        if (RegQueryValueEx(
            hKey,
            _T("EnableFakePlayerPresenter"),
            NULL,
            &dwType,
            (LPBYTE)&dwEnableFakePP,
            &dwDataSize
            ) == ERROR_SUCCESS)
        {
            if (dwType != REG_DWORD || dwEnableFakePP)
            {
                fRealPlayer = false;
            }
        }
    }

    if (fRealPlayer)
    {
#endif // PRERELEASE
#endif // DBG

        LogAVDataX(
            AVTRACE_LEVEL_INFO,
            AVCOMP_MILAV,
            "Creating real player" #
            " [%u,]",
            pMediaInstance->GetID());

        //
        // All failures from the player are now considered fatal.
        //
        IFC(CWmpPlayer::Create(
                pMediaInstance,
                canOpenAnyMedia,
                &pRealAvPlayer));

        SetInterface(*ppMedia, pRealAvPlayer);

#if DBG
#if PRERELEASE
    }
    else
    {
#pragma prefast(push)
#pragma warning(suppress:26015) //debug-only bits won't appear in retail: suppress prefast warning

        DWORD dwType;
        DWORD dwDataSize = sizeof(DWORD);
        DWORD dwFrameDuration = 100;
        UINT uiFrames = 100;
        UINT uiVideoWidth = 100;
        UINT uiVideoHeight = 100;

        DWORD dwRegFrameDuration = 0;
        DWORD dwRegFrames = 0;
        DWORD dwRegVideoWidth = 0;
        DWORD dwRegVideoHeight = 0;

        if (RegQueryValueEx(
            hKey,
            _T("FrameDuration"),
            NULL,
            &dwType,
            (LPBYTE)&dwRegFrameDuration,
            &dwDataSize
            ) == ERROR_SUCCESS)
        {
            if (dwType == REG_DWORD)
            {
                dwFrameDuration = dwRegFrameDuration;
            }
        }

        if (RegQueryValueEx(
            hKey,
            _T("Frames"),
            NULL,
            &dwType,
            (LPBYTE)&dwRegFrames,
            &dwDataSize
            ) == ERROR_SUCCESS)
        {
            if (dwType == REG_DWORD)
            {
                uiFrames = UINT(dwRegFrames);
            }
        }
        if (RegQueryValueEx(
            hKey,
            _T("VideoWidth"),
            NULL,
            &dwType,
            (LPBYTE)&dwRegVideoWidth,
            &dwDataSize
            ) == ERROR_SUCCESS)
        {
            if (dwType == REG_DWORD)
            {
                uiVideoWidth = UINT(dwRegVideoWidth);
            }
        }
        if (RegQueryValueEx(
            hKey,
            _T("VideoHeight"),
            NULL,
            &dwType,
            (LPBYTE)&dwRegVideoHeight,
            &dwDataSize
            ) == ERROR_SUCCESS)
        {
            if (dwType == REG_DWORD)
            {
                uiVideoHeight = UINT(dwRegVideoHeight);
            }
        }
        IFC(CFakePP::Create(pMediaInstance,
                            dwFrameDuration,
                            uiFrames,
                            uiVideoWidth,
                            uiVideoHeight,
                            &pFakeAvPlayer));
        ReplaceInterface(*ppMedia, pFakeAvPlayer);

#pragma prefast(pop)
    }
#endif // PRERELEASE
#endif // DBG

Cleanup:
#if DBG
#if PRERELEASE
    if (hKey)
    {
        RegCloseKey(hKey);
    }
    ReleaseInterface(pFakeAvPlayer);
#endif // PRERELEASE
#endif // DBG
    ReleaseInterface(pRealAvPlayer);

    RRETURN(hr);
}

