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
//      Header for the CWmpClientSite class, which implements several interfaces
//      needed for setting up the WMP OCX, but aren't needed after that.
//
//  $ENDTAG
//
//------------------------------------------------------------------------------

#pragma once

MtExtern(CWmpClientSite);

class CEvrFilterWrapper;

class CWmpClientSite :
    public CMILCOMBase,
    public IServiceProvider,
    public IWMPGraphCreation,
    public IOleClientSite
{
public:
    DECLARE_METERHEAP_CLEAR(ProcessHeap, Mt(CWmpClientSite));

    static
    HRESULT
    Create(
        __in    UINT                uiID,
        __deref_out CWmpClientSite  **ppSetup,
        __in    CWmpStateEngine     *pPlayerState
        );

    // Declares IUnknown functions
    DECLARE_COM_BASE;

    //
    // IServiceProvider
    //
    STDMETHOD(QueryService)(REFGUID guidService, REFIID riid, void ** ppv);

    //
    // IWMPGraphCreation
    //
    STDMETHOD(GetGraphCreationFlags)(DWORD* pdwFlags);
    STDMETHOD(GraphCreationPostRender)(IUnknown* pFilterGraph);
    STDMETHOD(GraphCreationPreRender)(IUnknown* pFilterGraph, IUnknown* pReserved);

    //
    // IOleClientSite
    //
    STDMETHOD(GetContainer)(LPOLECONTAINER FAR* ppContainer);
    STDMETHOD(GetMoniker)(DWORD dwAssign, DWORD dwWhichMoniker, IMoniker ** ppmk);
    STDMETHOD(OnShowWindow)(BOOL fShow) NOTIMPL_METHOD;
    STDMETHOD(RequestNewObjectLayout)() NOTIMPL_METHOD;
    STDMETHOD(SaveObject)() NOTIMPL_METHOD;
    STDMETHOD(ShowObject)() NOTIMPL_METHOD;

protected:
    //
    // CMILCOMBase
    //
    STDMETHOD(HrFindInterface)(__in_ecount(1) REFIID riid, __deref_out void **ppv);

private:

    CWmpClientSite(
        __in    UINT                uiID,
        __in    CWmpStateEngine     *pPlayerState
        );

    virtual ~CWmpClientSite();

    HRESULT HasMedia(IFilterGraph *pGraph, bool *pfAudio, bool *pfVideo);
    HRESULT FilterHasMedia(IBaseFilter *pFilter, bool *pfAudio, bool *pfVideo);
    HRESULT PinHasMedia(IPin *pPin, bool *pfAudio, bool *pfVideo);
    EvrPresenterObj    *m_pPresenter;
    IFilterGraph       *m_pFilterGraph;
    CWmpStateEngine    *m_pPlayerState;
    CEvrFilterWrapper  *m_pCEvrFilterWrapper;
    UINT               m_uiID;
};


