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
//      Provides a presenter to the Composition engine that doesn't do anything
//      except display black. This is useful when in various conditions that we
//      don't want to treat as hard failures but where we can't achieve having a
//      real media pipeline (out of resources, no WMP10 Ocx etc).
//
//  $ENDTAG
//
//------------------------------------------------------------------------------
#include "precomp.hpp"
#include "DummyPlayer.tmh"

MtDefine(DummySurfaceRenderer, Mem, "DummySurfaceRenderer");

/*static*/
HRESULT
DummySurfaceRenderer::
Create(
    __in        MediaInstance           *pMediaInstance,
    __deref_out DummySurfaceRenderer    **ppDummySurfaceRenderer
    )
{
    HRESULT     hr = S_OK;

    TRACEFID(pMediaInstance->GetID(), &hr);

    DummySurfaceRenderer    *pDummySurfaceRenderer = NULL;

    pDummySurfaceRenderer = new DummySurfaceRenderer(pMediaInstance);

    IFCOOM(pDummySurfaceRenderer);

    pDummySurfaceRenderer->InternalAddRef();

    IFC(pDummySurfaceRenderer->InitializeDummySource(msc_mediaWidth, msc_mediaHeight));

    *ppDummySurfaceRenderer = pDummySurfaceRenderer;
    pDummySurfaceRenderer = NULL;

Cleanup:

    ReleaseInterface(pDummySurfaceRenderer);

    EXPECT_SUCCESSID(pMediaInstance->GetID(), hr);

    RRETURN(hr);
}

//
// IAVSurfaceRenderer
//
STDMETHODIMP
DummySurfaceRenderer::
BeginComposition(
    __in    CMilSlaveVideo  *pCaller,
    __in    BOOL            displaySetChanged,
    __in    BOOL            syncChannel,
    __inout LONGLONG        *pLastCompositionSampleTime,
    __out   BOOL            *pbFrameReady
    )
{
    HRESULT     hr = S_OK;

    TRACEF(&hr);

    CHECKPTRARG(pbFrameReady);

    //
    // We only update if ForceFrameUpdate is called, and then
    // we only do so once. The frames never change for the
    // dummy media player.
    //
    *pbFrameReady = !!m_needToUpdate;
    m_needToUpdate = false;

Cleanup:

    return hr;
}

STDMETHODIMP
DummySurfaceRenderer::
BeginRender(
    __in_ecount_opt(1) CD3DDeviceLevel1 *pDeviceLevel1,        // NULL OK (in SW)
    __deref_out_ecount(1) IWGXBitmapSource **ppMILBitmapSource
    )
{
    HRESULT hr = S_OK;

    CHECKPTRARG(ppMILBitmapSource);

    *ppMILBitmapSource = m_pDummySource;
    m_pDummySource->AddRef();

Cleanup:

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

STDMETHODIMP
DummySurfaceRenderer::
EndRender(
    )
{
    return S_OK;
}

STDMETHODIMP
DummySurfaceRenderer::EndComposition(
    __in    CMilSlaveVideo  *pCaller
    )
{
    return S_OK;
}

STDMETHODIMP
DummySurfaceRenderer::
GetContentRect(
    __out_ecount(1) MilPointAndSizeL *prcContent
    )
{
    HRESULT hr = S_OK;

    TRACEF(&hr);

    CHECKPTRARG(prcContent);

    prcContent->X = 0;
    prcContent->Y = 0;
    prcContent->Height = m_mediaHeight;
    prcContent->Width = m_mediaWidth;
Cleanup:

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

STDMETHODIMP
DummySurfaceRenderer::
GetContentRectF(
    __out_ecount(1) MilPointAndSizeF *prcContent
    )
{
    HRESULT hr = S_OK;

    TRACEF(&hr);

    CHECKPTRARG(prcContent);

    prcContent->X = 0;
    prcContent->Y = 0;
    prcContent->Height = static_cast<float>(m_mediaHeight);
    prcContent->Width = static_cast<float>(m_mediaWidth);

Cleanup:

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

void
DummySurfaceRenderer::
SetIDirect3DDevice9(
    __in IDirect3DDevice9 *pIDirect3DDevice9
    )
{
    //
    // We just return a bitmap each time we are queried.
    //
}

//
// Protected methods
//
//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpPlayer::HrFindInterface, CMILCOMBase
//
//  Synopsis:
//      Get a pointer to another interface implemented by CWmpPlayer
//
//------------------------------------------------------------------------------
STDMETHODIMP
DummySurfaceRenderer::
HrFindInterface(
    __in_ecount(1) REFIID riid,
    __deref_out void **ppvObject
    )
{
    HRESULT     hr = S_OK;

    TRACEF(&hr);

    if (!ppvObject)
    {
        IFCN(E_INVALIDARG);
    }

    if (riid == IID_IAVSurfaceRenderer)
    {
        *ppvObject = static_cast<IAVSurfaceRenderer *>(this);
    }
    else
    {
        IFCN(E_NOINTERFACE);
    }

Cleanup:

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//
// Private methods
//
DummySurfaceRenderer::
DummySurfaceRenderer(
    __in        MediaInstance       *pMediaInstance
    ) : m_uiID(pMediaInstance->GetID()),
        m_pMediaInstance(NULL),
        m_pDummySource(NULL),
        m_mediaWidth(0),
        m_mediaHeight(0),
        m_needToUpdate(false)
{
    SetInterface(m_pMediaInstance, pMediaInstance);
}

DummySurfaceRenderer::
~DummySurfaceRenderer(
    void
    )
{
    ReleaseInterface(m_pDummySource);
    ReleaseInterface(m_pMediaInstance);
}

HRESULT
DummySurfaceRenderer::
InitializeDummySource(
    __in    UINT    width,
    __in    UINT    height
    )
{
    HRESULT         hr = S_OK;
    CDummySource    *pDummySource = NULL;

    pDummySource = new CDummySource(width, height, MilPixelFormat::BGR32bpp);
    IFCOOM(pDummySource);
    pDummySource->AddRef();

    m_mediaWidth = width;
    m_mediaHeight = height;

    ReleaseInterface(m_pDummySource);
    m_pDummySource = pDummySource;
    pDummySource = NULL;

Cleanup:
    ReleaseInterface(pDummySource);

    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

void
DummySurfaceRenderer::
ForceFrameUpdate(
    __in    UINT    mediaWidth,
    __in    UINT    mediaHeight
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    m_needToUpdate = true;
    IFC(InitializeDummySource(mediaWidth, mediaHeight));

Cleanup:
    if (FAILED(hr))
    {
        m_pMediaInstance->GetMediaEventProxy().RaiseEvent(AVMediaFailed, E_OUTOFMEMORY);
    }
}

