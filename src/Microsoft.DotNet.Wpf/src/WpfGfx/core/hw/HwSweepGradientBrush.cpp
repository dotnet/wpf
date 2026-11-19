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
//      Contains CHwSweepGradientBrush implementation
//
//  $ENDTAG
//
//------------------------------------------------------------------------------

#include "precomp.hpp"

MtDefine(CHwSweepGradientBrush, MILRender, "CHwSweepGradientBrush");

//+-----------------------------------------------------------------------------
//
//  Member:
//      CHwSweepGradientBrush::CHwSweepGradientBrush
//
//  Synopsis:
//      ctor
//
//------------------------------------------------------------------------------
CHwSweepGradientBrush::CHwSweepGradientBrush(
    __in_ecount(1) IMILPoolManager *pManager,
    __in_ecount(1) CD3DDeviceLevel1 *pDevice
    ) : CHwLinearGradientBrush(pManager, pDevice)
{
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CHwSweepGradientBrush::dtor
//
//------------------------------------------------------------------------------
CHwSweepGradientBrush::~CHwSweepGradientBrush()
{
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CHwSweepGradientBrush::SetBrushAndContext
//
//  Synopsis:
//      Called at the beginning of a rendering operation to set context and
//      device independent brush to realize.
//
//------------------------------------------------------------------------------
HRESULT
CHwSweepGradientBrush::SetBrushAndContext(
    __inout_ecount(1) CMILBrush *pBrush,
    __in_ecount(1) const CHwBrushContext &hwBrushContext
    )
{
    HRESULT hr = S_OK;
    
    if (!m_pLinGradSource)
    {
        CHwSweepGradientColorSource *pSweepGradientColorSource = NULL;

        IFC(CHwSweepGradientColorSource::Create(
            m_pDevice,
            &pSweepGradientColorSource
            ));

        m_pLinGradSource = pSweepGradientColorSource;
        pSweepGradientColorSource = NULL; // steal ref
    }

    IFC(CHwLinearGradientBrush::SetBrushAndContextInternal(
        pBrush,
        hwBrushContext
        ));

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CHwSweepGradientBrush::SendOperations
//
//  Synopsis:
//      Send primary blend operations and color source(s) to builder
//
//+-----------------------------------------------------------------------------
HRESULT
CHwSweepGradientBrush::SendOperations(
    __inout_ecount(1) CHwPipelineBuilder *pBuilder
    )
{
    HRESULT hr = S_OK;

    Assert(m_pLinGradSource);

    CHwSweepGradientColorSource *pSweepGradientColorSource =
        DYNCAST(CHwSweepGradientColorSource, m_pLinGradSource);
    Assert(pSweepGradientColorSource);

    IFC(pBuilder->Set_SweepGradient(pSweepGradientColorSource));

Cleanup:
    RRETURN(hr);
}

