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
//      Header for the CFakePP class (fake player-presenter)
//
//  $ENDTAG
//
//------------------------------------------------------------------------------

#pragma once

#if DBG // This file is not needed in production code

MtExtern(CFakePP);

class CMediaEventProxy;

class CFakePP
: public CMILCOMBase
, public IMILMedia
, public IMILSurfaceRendererProvider
, public IAVSurfaceRenderer
{
public:
    DECLARE_METERHEAP_CLEAR(ProcessHeap, Mt(CFakePP));

    static HRESULT Create(
        __in MediaInstance* pMediaInstance,
        __in DWORD dwFrameDuration,
        __in UINT uiFrames,
        __in UINT uiVideoWidth,
        __in UINT uiVideoHeight,
        __deref_out_ecount(1) CFakePP** ppFake
        );

    // Declares IUnknown functions
    DECLARE_COM_BASE;

    //
    // IAVSurfaceRenderer
    //
    STDMETHOD(BeginComposition)(
        __in    CMilSlaveVideo  *pCaller,
        __in    BOOL            displaySetChanged,
        __in    BOOL            syncChannel,
        __inout LONGLONG        *pLastCompositionSampleTime,
        __out   BOOL            *pbFrameReady
        );

    STDMETHOD(BeginRender)(
        __in_ecount_opt(1) CD3DDeviceLevel1 *pDeviceLevel1,        // NULL OK (in SW)
        __deref_out_ecount(1) IWGXBitmapSource **ppMILBitmapSource
        );

    STDMETHOD(EndRender)(
        );

    STDMETHOD(EndComposition)(
        __in    CMilSlaveVideo  *pCaller
        );

    STDMETHOD(GetContentRect)(
        __out_ecount(1) MilPointAndSizeL *prcContent
        );

    STDMETHOD(GetContentRectF)(
        __out_ecount(1) MilPointAndSizeF *prcContent
        );

    STDMETHOD_(BOOL, CanUseBackBuffer)() { return false; };

    STDMETHOD(RegisterResource)(
        __in CMilSlaveVideo *pSlaveVideo
        );

    STDMETHOD(UnregisterResource)(
        __in CMilSlaveVideo *pSlaveVideo
        );

    //
    // IMILMedia
    //

    STDMETHOD(Open)(
        __in LPCWSTR pwszURL
        );

    STDMETHOD(Close)();

    STDMETHOD(Stop)();

    STDMETHOD(GetPosition)(__out_ecount(1) LONGLONG *pllTime);

    STDMETHOD(SetPosition)(LONGLONG llTime);

    STDMETHOD(SetRate)(double dRate);

    STDMETHOD(SetVolume)(double dblVolume);

    STDMETHOD(SetBalance)(double dblBalance);

    STDMETHOD(SetIsScrubbingEnabled)(bool isScrubbingEnabled);

    /* Return whether or not we're currently buffering */
    STDMETHOD(IsBuffering)(
        __out_ecount(1) bool *pIsBuffering
        );

    /* Return whether or not we can pause */
    STDMETHOD(CanPause)(
        __out_ecount(1) bool *pCanPause
        );

    /* Get the download progress */
    STDMETHOD(GetDownloadProgress)(
        __out_ecount(1) double *pProgress
        );

    /* Get the buffering progress */
    STDMETHOD(GetBufferingProgress)(
        __out_ecount(1) double *pProgress
        );

    STDMETHOD(HasVideo)(
        __out_ecount(1) bool *pfHasVideo
        );

    STDMETHOD(HasAudio)(
        __out_ecount(1) bool *pfHasAudio
        );

    STDMETHOD(GetNaturalHeight)(
        __out_ecount(1) UINT *puiHeight
        );

    STDMETHOD(GetNaturalWidth)(
        __out_ecount(1) UINT *puiWidth
        );

    // Get the duration of the clip in 100 nanosecond ticks
    STDMETHOD(GetMediaLength)(
        __out_ecount(1) LONGLONG *pllLength
        );

    STDMETHOD(NeedUIFrameUpdate)();

    STDMETHOD(Shutdown)();

    STDMETHOD(ProcessExitHandler)();

    //
    // IMILSurfaceRendererProvider
    //
    STDMETHOD(GetSurfaceRenderer)(
        __deref_out_ecount(1) IAVSurfaceRenderer **ppSurfaceRenderer
        );

protected:
    //
    // CMILCOMBase
    //
    STDMETHOD(HrFindInterface)(__in_ecount(1) REFIID riid, __deref_out void **ppv);

private:
    CFakePP(MediaInstance *pMediaInstance);
    virtual ~CFakePP();

    STDMETHOD(Start)();
    STDMETHOD(Pause)();

    HRESULT Initialize();
    HRESULT VerifyConsistency();
    void NotifyVideoResource();
    void ThreadProc();
    HRESULT RaiseEvent(__in AVEvent avEventType);

    void SetFrameDuration(DWORD dwFrameDuration) { m_dwFrameDuration = dwFrameDuration; };
    void SetFrames(UINT uiFrames) { m_uiFrames = uiFrames; };
    void SetVideoWidth(UINT uiVideoWidth) { m_uiVideoWidth = uiVideoWidth; };
    void SetVideoHeight(UINT uiVideoHeight) { m_uiVideoHeight = uiVideoHeight; };

    static DWORD WINAPI StaticThreadProc(VOID *pv);

    HRESULT
    CreateDevice(
        __deref_out CD3DDeviceLevel1            **ppD3DDevice
            // The returned device
        );

    MediaInstance *m_pMediaInstance;
    CMilSlaveVideo *m_pVideoResource;
    CCriticalSection m_csEntry;
    bool m_fThreadRunning;
    HANDLE m_hThread;
    HANDLE m_hEvent;
    UINT m_uiFrames; // number of frames in the video
    UINT m_uiCurrentFrame;
    DWORD m_dwFrameDuration; // frame duration in milliseconds
    UINT m_uiColor; // used for alternating colors

    UINT m_uiVideoWidth;
    UINT m_uiVideoHeight;
    double m_dblRate;

    CMFMediaBuffer      *m_pMediaBuffer;
    CD3DDeviceLevel1    *m_pCD3DDeviceLevel1;

    enum Status
    {
        Playing,
        Paused,
        Stopped,
        Terminated
    };

    Status m_status;
    UINT m_uiID;

    FILE *m_pLogFile; // log for DRT verification
};
#endif //DBG

