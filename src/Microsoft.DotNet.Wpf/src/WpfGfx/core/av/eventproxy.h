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
//      Acts as a proxy to relay events to managed objects
//
//  $ENDTAG
//
//------------------------------------------------------------------------------

#pragma once

MtExtern(CEventProxy);

//
// CEventProxyDescriptor
//
class CEventProxyDescriptor
{
public:

    CEventProxyDescriptor();

    void (__stdcall *pfnDispose)(
        void *pEPD
        );

    HRESULT (__stdcall *pfnRaiseEvent)(
        void *pEPD,
        VOID *pb,
        ULONG cb
        );

    DWORD_PTR m_handle;
};

//
// CEventProxy
//
class CEventProxy :
    public IMILEventProxy
{
private:
    CEventProxy();
    virtual ~CEventProxy();

    HRESULT
    Init(
        __in_ecount(1) const CEventProxyDescriptor &epd
        );

public:
    DECLARE_METERHEAP_ALLOC(ProcessHeap, Mt(CEventProxy));

    static HRESULT Create(
        __in_ecount(1) const CEventProxyDescriptor &epd,
        __deref_out_ecount(1) CEventProxy **ppEventProxy
        );

    //
    // IUnknown
    //


    STDMETHOD(QueryInterface)(
        REFIID riid,
        LPVOID* ppvObject
        );

    STDMETHOD_(ULONG, AddRef)(
        );

    STDMETHOD_(ULONG, Release)(
        );

    STDMETHOD(RaiseEvent)(
        BYTE *pb,
        ULONG cb
        );

    void
    Shutdown(
        void
        );

private:
    long m_ulRef;
    CEventProxyDescriptor m_epd;
    bool m_isShutdown;
    CCriticalSection m_lock;

    static LONG ms_mediaCount;
};



