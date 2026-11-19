// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//-----------------------------------------------------------------------------
//
//
//  Description:
//      CHybridSurfaceRenderTarget implementation
//
//      This object creates the hybrid render target.
//      Which means it automatically creates HW or SW based on MilRTInitialization::Flags
//      and fallback to SW if HW is not available.
//

#include "precomp.hpp"

HRESULT CHybridSurfaceRenderTarget::CreateRenderTargetBitmap(
    __in_ecount_opt(1) CDisplaySet const *pDisplaySet,
    MilRTInitialization::Flags dwFlags,
    UINT width,
    UINT height,
    MilPixelFormat::Enum format,
    FLOAT dpiX, 
    FLOAT dpiY,
    IntermediateRTUsage usageInfo,
    __deref_out_ecount(1) IMILRenderTargetBitmap **ppIRenderTargetBitmap
) 
{
    HRESULT hr = S_OK;
    D3DDEVTYPE d3dDeviceType;
    CD3DDeviceLevel1 *pD3DDevice = NULL;
    CD3DDeviceManager *pD3DDeviceManager = NULL;
    CBaseRenderTarget *pRenderTarget = NULL;
    const CDisplay *pDisplay = NULL;
    UINT uAdapter = 0;
    CHwTextureRenderTarget *pTextureRT = NULL;

    DisplayId associatedDisplay;
    CD3DDeviceManager::D3DDeviceCreationParameters CreateParams;
    DynArrayIA<D3DDISPLAYMODEEX, 4> drgDisplayModes;

    //
    // check whether any adapters don't support Hw acceleration or D3D is not
    // available.
    //
    if (RenderOptions::IsSoftwareRenderingForcedForProcess() ||
        !pDisplaySet || 
        (!RenderOptions::IsHardwareAccelerationInRdpEnabled() && pDisplaySet->IsNonLocalDisplayPresent()) || 
        !pDisplaySet->D3DObject())
    {
        if (dwFlags & MilRTInitialization::HardwareOnly)
        {
            IFC(WGXERR_INVALIDCALL);
        }
        else
        {
            dwFlags |= MilRTInitialization::SoftwareOnly;
        }
    }
    
    if (dwFlags & MilRTInitialization::UseRgbRast)
    {
        if (dwFlags & MilRTInitialization::HardwareOnly)
        {
            IFC(WGXERR_INVALIDCALL);
        }
        else 
        {
            d3dDeviceType = D3DDEVTYPE_SW;
        }
    }
    else if (dwFlags & MilRTInitialization::SoftwareOnly)
    {
        d3dDeviceType = D3DDEVTYPE_SW;
    }
    else if (dwFlags & MilRTInitialization::HardwareOnly)
    {
        if (dwFlags & MilRTInitialization::UseRefRast)
        {
            d3dDeviceType = D3DDEVTYPE_REF;
        }
        else
        {
            d3dDeviceType = D3DDEVTYPE_HAL;
        }
    }
    else
    {
        d3dDeviceType = D3DDEVTYPE_HAL;
    }

    if (d3dDeviceType == D3DDEVTYPE_SW)
    {
        MIL_THR(CSwRenderTargetBitmap::Create(
            width,
            height,
            format,
            dpiX,
            dpiY,
            DisplayId::None,
            ppIRenderTargetBitmap
            DBG_STEP_RENDERING_COMMA_PARAM(NULL) // pDisplayRTParent
            ));
        RRETURN(hr);
    }

    if (FAILED(pDisplaySet->GetDisplay(0, &pDisplay)) || pDisplay == NULL)
    {
        IFC(WGXERR_INTERNALERROR);
    }

    pD3DDeviceManager = CD3DDeviceManager::Get();
    Assert(pDisplay->D3DObject()); // we should not get here with null pID3D
    IFC(pD3DDeviceManager->InitializeD3DReferences(pDisplay ? pDisplay->DisplaySet() : NULL));

    if (pDisplay)
    {
        uAdapter = pDisplay->GetDisplayIndex();
    }
    
    IFC(pD3DDeviceManager->ComposeCreateParameters(
            NULL,
            dwFlags,
            uAdapter,
            d3dDeviceType,
            &CreateParams
        ));

    D3DDISPLAYMODEEX *rgDisplayModes;
    IFC(drgDisplayModes.AddMultiple(
            CreateParams.NumberOfAdaptersInGroup,
            &rgDisplayModes
        ));
    IFC(pD3DDeviceManager->GetDisplayMode(
            &CreateParams,
            rgDisplayModes
        ));

    D3DPRESENT_PARAMETERS PresentParameters;
    pD3DDeviceManager->ComposePresentParameters(
        rgDisplayModes[CreateParams.AdapterOrdinalInGroup],
        CreateParams,
        &PresentParameters
        );
    IFC(pD3DDeviceManager->CreateNewDevice(
            &CreateParams,
            &PresentParameters,
            rgDisplayModes,
            &pD3DDevice
        ));

    associatedDisplay = pDisplay->GetDisplayId();
    IFC(CHwTextureRenderTarget::Create(
            width,
            height,
            pD3DDevice,
            associatedDisplay,
            (usageInfo.flags & IntermediateRTUsage::ForBlending) ? true : false,
            &pTextureRT
        ));
    *ppIRenderTargetBitmap = pTextureRT;

Cleanup:
    ReleaseInterfaceNoNULL(pD3DDevice);
    ReleaseInterfaceNoNULL(pD3DDeviceManager);
    ReleaseInterfaceNoNULL(pRenderTarget);
    RRETURN(hr);
}
