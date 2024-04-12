// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//-----------------------------------------------------------------------------
//
//
//  Description:
//      CHybridSurfaceRenderTarget implementation
//
//      This object creates the hyper render target with sw and hw.
//

#include "precomp.hpp"

//+------------------------------------------------------------------------
//
//  Function:  CHybridSurfaceRenderTarget::Create
//
//  Synopsis:  1. Create the CD3DDeviceLevel1
//             2. Check format support
//             3. Create and initialize the CHybridSurfaceRenderTarget
//
//-------------------------------------------------------------------------
HRESULT 
CHybridSurfaceRenderTarget::Create(
    __in_ecount_opt(1) CDisplaySet const *pDisplaySet,
    MilRTInitialization::Flags dwFlags,
    __deref_out_ecount(1) CHybridSurfaceRenderTarget **ppRenderTarget
    ) 
{
    HRESULT hr = S_OK;

    *ppRenderTarget = NULL;

    CDisplay *pDisplay = NULL;
    D3DDEVTYPE type = D3DDEVTYPE_SW;

    if (pDisplaySet && pDisplaySet->GetDisplayCount() > 0) 
    {
        pDisplay = const_cast<CDisplay *>(pDisplaySet->Display(0));
        type = D3DDEVTYPE_HAL;
    }

    CD3DDeviceLevel1 *pD3DDevice = NULL;

    D3DPRESENT_PARAMETERS D3DPresentParams;
    UINT AdapterOrdinalInGroup;

    CD3DDeviceManager *pD3DDeviceManager = CD3DDeviceManager::Get();
    Assert(pDisplay->D3DObject()); // we should not get here with null pID3D

    IFC(pD3DDeviceManager->GetD3DDeviceAndPresentParams(
        NULL,
        dwFlags,
        pDisplay,
        type,
        &pD3DDevice,
        &D3DPresentParams,
        &AdapterOrdinalInGroup
        ));

    HRESULT const *phrTestGetDC;

    IFC(pD3DDevice->CheckRenderTargetFormat(
        D3DPresentParams.BackBufferFormat,
        OUT &phrTestGetDC
        ));

    {
        DisplayId associatedDisplay = pDisplay->GetDisplayId();

        *ppRenderTarget = new CHybridSurfaceRenderTarget(
            pD3DDevice,
            D3DPresentParams,
            associatedDisplay
            );

        IFCOOM(*ppRenderTarget);
        (*ppRenderTarget)->AddRef(); // CHybridSurfaceRenderTarget::ctor sets ref count == 0
    }

Cleanup:
    if (FAILED(hr))
    {
        ReleaseInterface(*ppRenderTarget);
    }
    ReleaseInterfaceNoNULL(pD3DDevice);
    pD3DDeviceManager->Release();
    RRETURN(hr);
}


//+------------------------------------------------------------------------
//
//  Function:  CHybridSurfaceRenderTarget::HrFindInterface
//
//  Synopsis:  HrFindInterface implementation
//
//-------------------------------------------------------------------------
STDMETHODIMP
CHybridSurfaceRenderTarget::HrFindInterface(
    __in_ecount(1) REFIID riid,
    __deref_out void** ppvObject
)
{
    AssertMsg(false, "CHybridSurfaceRenderTarget is not allowed to be QI'ed.");
    RRETURN(E_NOINTERFACE);
}

//+------------------------------------------------------------------------
//
//  Function:  CHwDisplayRenderTarget::CHwDisplayRenderTarget
//
//  Synopsis:  ctor
//
//-------------------------------------------------------------------------
CHybridSurfaceRenderTarget::CHybridSurfaceRenderTarget(
    __inout_ecount(1) CD3DDeviceLevel1 *pD3DDevice,
    __in_ecount(1) D3DPRESENT_PARAMETERS const &D3DPresentParams,
    DisplayId associatedDisplay
    ) :
    CHwSurfaceRenderTarget(
        pD3DDevice,
        D3DFormatToPixelFormat(D3DPresentParams.BackBufferFormat, TRUE),
        D3DPresentParams.BackBufferFormat,
        associatedDisplay
    )
{ }

//+----------------------------------------------------------------------------
//
//  Member:    CHybridSurfaceRenderTarget::IsValid
//
//  Synopsis:  Returns FALSE when rendering with this render target or any use
//             is no longer allowed.  Mode change is a common cause of of
//             invalidation.
//
//-----------------------------------------------------------------------------

bool
CHybridSurfaceRenderTarget::IsValid() const
{
    return true;
}


#if DBG_STEP_RENDERING

//+------------------------------------------------------------------------
//
//  Function:  CHwDisplayRenderTarget::ShowSteppedRendering
//
//  Synopsis:  Present the current backbuffer or the given texture
//             when enabled in debug builds
//
//-------------------------------------------------------------------------

void
CHybridSurfaceRenderTarget::ShowSteppedRendering(
    __in LPCTSTR pszRenderDesc,
    __in_ecount(1) const ISteppedRenderingSurfaceRT *pRT
    )
{ }

#endif DBG_STEP_RENDERING
