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
#include "MediaInstance.tmh"

MtDefine(MediaInstance, Mem, "MediaInstance");

/*static*/ LONG MediaInstance::ms_id = 0;

//
// Public methods
//

/*static*/
HRESULT
MediaInstance::
Create(
    __in            CEventProxy         *pCEventProxy,
    __deref_out     MediaInstance       **ppMediaInstance
    )
{
    HRESULT         hr = S_OK;
    UINT            id = InterlockedIncrement(&ms_id);
    MediaInstance   *pMediaInstance = NULL;

    TRACEFID(id, &hr);

    pMediaInstance = new MediaInstance(id, pCEventProxy);

    IFCOOM(pMediaInstance);

    IFC(pMediaInstance->Init());

    *ppMediaInstance = pMediaInstance;
    pMediaInstance = NULL;

Cleanup:
    ReleaseInterface(pMediaInstance);

    RRETURN(hr);
}

HRESULT
MediaInstance::
Init(
    void
    )
{
    HRESULT hr = S_OK;

    IFC(m_compositionNotifier.Init(this));

    IFC(m_mediaEventProxy.Init());

Cleanup:
    RRETURN(hr);
}

//
// Protected methods
//

//
// CMILCOMBase
//
STDMETHODIMP
MediaInstance::
HrFindInterface(__in_ecount(1) REFIID riid, __deref_out void **ppv)
{
    RRETURN(E_NOINTERFACE);
}

//
// Private methods
//

MediaInstance::
MediaInstance(
    __in        UINT        uiID,
    __in        CEventProxy *pCEventProxy
    ) : m_uiID(uiID),
        m_mediaEventProxy(uiID, pCEventProxy)
{
    AddRef();

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_MILAV,
        "MediaInstance(%u,%p)",
        uiID,
        pCEventProxy);
}

MediaInstance::
~MediaInstance(
    void
    )
{
    TRACEF(NULL);
}

UINT
MediaInstance::
GetID(
    void
    ) const
{
    return m_uiID;
}

