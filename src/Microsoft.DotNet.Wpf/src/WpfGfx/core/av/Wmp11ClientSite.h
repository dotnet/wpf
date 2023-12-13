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
//      Header for the Wmp11ClientSite class, which implements several
//      interfaces needed for setting up the WMP OCX, but aren't needed after
//      that.
//
//  $ENDTAG
//
//------------------------------------------------------------------------------

#pragma once

MtExtern(Wmp11ClientSite);

class CEvrFilterWrapper;

class Wmp11ClientSite :
    public CMILCOMBase,
    public IServiceProvider,
    public IWMPRemoteMediaServices,
    public IOleClientSite
{
public:
    DECLARE_METERHEAP_CLEAR(ProcessHeap, Mt(Wmp11ClientSite));

    static
    HRESULT
    Create(
        __in        UINT             uiID,
        __deref_out Wmp11ClientSite  **ppSetup
        );

    // Declares IUnknown functions
    DECLARE_COM_BASE;

    //
    // IServiceProvider
    //
    STDMETHOD(QueryService)(REFGUID guidService, REFIID riid, void ** ppv);

    //
    // IWMPRemoteMediaServices
    //
    STDMETHOD(GetApplicationName)(BSTR *pName) NOTIMPL_METHOD; // not used for in-proc hosting
    STDMETHOD(GetCustomUIMode)(BSTR *pFile) NOTIMPL_METHOD;
    STDMETHOD(GetScriptableObject)(__out BSTR *pName, __deref_out IDispatch **ppDispatch);
    STDMETHOD(GetServiceType)(__out     BSTR    *pType);

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

    Wmp11ClientSite(
        __in    UINT                uiID
        );

    virtual ~Wmp11ClientSite();

    UINT               m_uiID;
};


