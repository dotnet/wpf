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
//      Header for the CWmpPlayer class, which adapts the WMP OCX interface to
//      match the interface we'd like to have. For example, we convert volume
//      from a double between 0 and 1 to an integer between 0 and 100.
//
//  $ENDTAG
//
//------------------------------------------------------------------------------

#pragma once

MtExtern(CWmpPlayer);

class UpdateState;

class CWmpPlayer : public IMILMedia,
                   public IMILSurfaceRendererProvider,
                   public CMILCOMBase
{
public:
    DECLARE_METERHEAP_CLEAR(ProcessHeap, Mt(CWmpPlayer));

    static
    STDMETHODIMP
    Create(
        __in            MediaInstance       *pMediaInstance,
        __in            bool                canOpenAnyMedia,
        __deref_out     CWmpPlayer          **ppPlayer
        );

    // Declares IUnknown functions
    DECLARE_COM_BASE;

    //
    // IMILMedia
    //
    STDMETHOD(Open)(
        __in LPCWSTR pwszURL
        );

    STDMETHOD(Stop)();

    STDMETHOD(Close)();

    STDMETHOD(GetPosition)(
        __out    LONGLONG    *pllTime
        );

    STDMETHOD(SetPosition)(
        __in    LONGLONG    llTime
        );

    STDMETHOD(SetRate)(
        __in    double      dblRate
        );

    STDMETHOD(SetVolume)(
        __in    double      dblVolume
        );

    STDMETHOD(SetBalance)(
        __in    double      dblBalance
        );

    STDMETHOD(SetIsScrubbingEnabled)(
        __in    bool        isScrubbingEnabled
        );

    /* Return whether or not we're currently buffering */
    STDMETHOD(IsBuffering)(
        __out   bool        *pIsBuffering
        );

    /* Return whether or not we can pause */
    STDMETHOD(CanPause)(
        __out   bool        *pCanPause
        );

    /* Get the download progress */
    STDMETHOD(GetDownloadProgress)(
        __out   double      *pProgress
        );

    /* Get the buffering progress */
    STDMETHOD(GetBufferingProgress)(
        __out   double      *pProgress
        );

    STDMETHOD(HasVideo)(
        __out   bool        *pfHasVideo
        );

    STDMETHOD(HasAudio)(
        __out   bool        *pfHasAudio
        );

    STDMETHOD(GetNaturalHeight)(
        __out   UINT        *puiHeight
        );

    STDMETHOD(GetNaturalWidth)(
        __out   UINT        *puiWidth
        );

    // Get the duration of the clip in 100 nanosecond ticks
    STDMETHOD(GetMediaLength)(
        __out   LONGLONG    *pllLength
        );

    //
    // IMILSurfaceRendererProvider
    //
    STDMETHOD(GetSurfaceRenderer)(
        __deref_out IAVSurfaceRenderer **ppSurfaceRenderer
        );

    STDMETHOD(RegisterResource)(
        __in    CMilSlaveVideo *pSlaveVideo
        );

    STDMETHOD(UnregisterResource)(
        __in    CMilSlaveVideo *pSlaveVideo
        );

    STDMETHOD(NeedUIFrameUpdate)(
        );

    STDMETHOD(Shutdown)();

    STDMETHOD(ProcessExitHandler)();

protected:
    //
    // CMILCOMBase
    //
    STDMETHOD(HrFindInterface)(
        __in_ecount(1) REFIID riid,
        __deref_out void **ppvObject
        );

private:
    CWmpPlayer(
        __in    MediaInstance       *pMediaInstance
        );

    virtual ~CWmpPlayer();

    HRESULT
    Init(
        __in            bool                canOpenAnyMedia
        );

    SharedState         m_sharedState;
    UpdateState         *m_pUpdateState;
    CWmpStateEngine     *m_pCWmpStateEngine;
    MediaInstance       *m_pMediaInstance;
    bool                m_fShutdown;
    UINT                m_uiID;
    PWSTR               m_currentUrl;
};

