// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//+-----------------------------------------------------------------------------
//

//
//  $TAG ENGR

//      $Module:    wim_mil_graphics_brush
//      $Keywords:
//
//  $Description:
//      Contains CHwSweepGradientColorSource implementation
//
//  $ENDTAG
//
//------------------------------------------------------------------------------

#include "precomp.hpp"

MtDefine(CHwSweepGradientColorSource, MILRender, "CHwSweepGradientColorSource");

//+-----------------------------------------------------------------------------
//
//  Member:
//      CHwSweepGradientColorSource::Create
//
//------------------------------------------------------------------------------
HRESULT
CHwSweepGradientColorSource::Create(
    __in_ecount(1) CD3DDeviceLevel1 *pDevice,
    __deref_out_ecount(1) CHwSweepGradientColorSource **ppHwSweepGradCS
    )
{
    HRESULT hr = S_OK;

    *ppHwSweepGradCS = new CHwSweepGradientColorSource(pDevice);
    IFCOOM(*ppHwSweepGradCS);
    (*ppHwSweepGradCS)->AddRef();

Cleanup:
    if (FAILED(hr))
    {
        Assert(*ppHwSweepGradCS == NULL);
    }
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CHwSweepGradientColorSource::CHwSweepGradientColorSource
//
//  Synopsis:
//      ctor
//
//------------------------------------------------------------------------------
CHwSweepGradientColorSource::CHwSweepGradientColorSource(
    __in_ecount(1) CD3DDeviceLevel1 *pDevice
    ) :
    CHwLinearGradientColorSource(pDevice)
{
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CHwSweepGradientColorSource::~CHwSweepGradientColorSource
//
//  Synopsis:
//      dtor
//
//------------------------------------------------------------------------------
CHwSweepGradientColorSource::~CHwSweepGradientColorSource()
{
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CHwSweepGradientColorSource::SendShaderData
//
//  Synopsis:
//      Sends the linear gradient shader data, then sets the appropriate data
//      in the shader constants.
//
//------------------------------------------------------------------------------
HRESULT
CHwSweepGradientColorSource::SendShaderData(
    __inout_ecount(1) CHwPipelineShader *pShader
    )
{
    HRESULT hr = S_OK;

    IFC(CHwLinearGradientColorSource::SendShaderData(
        pShader
        ));

    const CMILBrushSweepGradient *pSweepGradientBrushNoRef =
        DYNCAST(const CMILBrushSweepGradient, this->GetGradientBrushNoRef());
    Assert(pSweepGradientBrushNoRef);

    Assert(m_hflStartAngleRadians != MILSP_INVALID_HANDLE);
    Assert(m_hflInvAngularSpan != MILSP_INVALID_HANDLE);
    Assert(m_hflAngularSpanIsZero != MILSP_INVALID_HANDLE);

    const FLOAT flDegToRad = static_cast<FLOAT>(M_PI) / 180.0f;
    FLOAT flStartRad = pSweepGradientBrushNoRef->GetStartAngle() * flDegToRad;
    FLOAT flSpanRad =
        (pSweepGradientBrushNoRef->GetEndAngle() - pSweepGradientBrushNoRef->GetStartAngle()) * flDegToRad;
    bool fSpanIsZero = (flSpanRad == 0.0f);
    FLOAT flInvSpan = fSpanIsZero ? 0.0f : (1.0f / flSpanRad);

    IFC(pShader->SetFloat(m_hflStartAngleRadians, flStartRad));
    IFC(pShader->SetFloat(m_hflInvAngularSpan, flInvSpan));
    IFC(pShader->SetFloat(m_hflAngularSpanIsZero, fSpanIsZero ? 1.0f : 0.0f));

Cleanup:
    RRETURN(hr);
}




