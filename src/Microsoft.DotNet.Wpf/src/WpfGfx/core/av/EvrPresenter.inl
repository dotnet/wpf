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
//      Provides inline methods for the EvrPresenter and AVSurfaceRenderer
//
//  $ENDTAG
//
//------------------------------------------------------------------------------

//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
// EvrPresenter implementation
//
/*static*/ inline
bool
EvrPresenter::
IsSoftwareFallbackError(
    __in    HRESULT                     hr
    )
{
    return    hr == D3DERR_NOTAVAILABLE
           || hr == E_NOINTERFACE
           || hr == WGXERR_AV_VIDEOACCELERATIONNOTAVAILABLE
           || hr == E_FAIL
           || hr == D3DERR_DRIVERINTERNALERROR
           || hr == E_OUTOFMEMORY
           || hr == DDERR_CURRENTLYNOTAVAIL
           || hr == WGXERR_NO_HARDWARE_DEVICE
           || hr == WGXERR_AV_UNKNOWNHARDWAREERROR
           || IsMandatorySoftwareFallbackError(hr);
}

/*static*/ inline
bool
EvrPresenter::
IsMandatorySoftwareFallbackError(
    __in    HRESULT                     hr
    )
{
    return hr == DDERR_SURFACELOST;
}

inline
SampleScheduler &
EvrPresenter::
GetSampleScheduler(
    void
    )
{
    return m_sampleScheduler;
}

/*static*/ inline
HRESULT
EvrPresenter::
CheckForShutdown(
    __in    RenderState::Enum       renderState
    )
{
    if (renderState == RenderState::Shutdown)
    {
        return MF_E_SHUTDOWN;
    }

    return S_OK;
}

//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
// EvrPresenter::AVSurfaceRenderer implementation
//
inline
CD3DDeviceLevel1 *
EvrPresenter::AVSurfaceRenderer::
CurrentRenderDevice(
    void
    )
{
    return m_pCurrentRenderDevice;
}

inline
HRESULT
EvrPresenter::AVSurfaceRenderer::
FallbackToSoftwareIfNecessary(
    __in    HRESULT                     hr
    )
{
    if (EvrPresenter::IsSoftwareFallbackError(hr))
    {
        hr = FallbackToSoftware();
    }

    return hr;
}

/*static*/ inline
bool
EvrPresenter::AVSurfaceRenderer::
IsTransientError(
    __in    HRESULT         hr
    )
{
    return    hr == WGXERR_AV_NOREADYFRAMES
           || hr == WGXERR_AV_NOMEDIATYPE;
}

/*static*/ inline
HRESULT
EvrPresenter::
TreatNonSoftwareFallbackErrorAsUnknownHardwareError(
    __in    HRESULT         hr
    )
{
    if (   FAILED(hr)
        && !IsSoftwareFallbackError(hr)
        //
        // These failures occur even when we have a normal D3D device.
        //
        && hr != MF_E_TRANSFORM_TYPE_NOT_SET)
    {
        return WGXERR_AV_UNKNOWNHARDWAREERROR;
    }
    else
    {
        return hr;
    }
}


