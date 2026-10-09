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
//      Contains CHwSweepGradientColorSource declaration
//
//  $ENDTAG
//
//------------------------------------------------------------------------------

MtExtern(CHwSweepGradientColorSource);

//+-----------------------------------------------------------------------------
//
//  Class:
//      CHwSweepGradientColorSource
//
//  Synopsis:
//      Provides a sweep gradient color source for a HW device
//
//------------------------------------------------------------------------------

class CHwSweepGradientColorSource : public CHwLinearGradientColorSource
{
public:

    static HRESULT Create(
        __in_ecount(1) CD3DDeviceLevel1 *pDevice,
        __deref_out_ecount(1) CHwSweepGradientColorSource **ppHwSweepGradCS
        );

private:

    DECLARE_METERHEAP_ALLOC(ProcessHeap, Mt(CHwSweepGradientColorSource));

    CHwSweepGradientColorSource(
        __in_ecount(1) CD3DDeviceLevel1 *pDevice
        );
    ~CHwSweepGradientColorSource();

public:

    void SetSweepGradientParamData(
        MILSPHandle hflStartAngleRadians,
        MILSPHandle hflInvAngularSpan,
        MILSPHandle hflAngularSpanIsZero
        )
    {
        m_hflStartAngleRadians  = hflStartAngleRadians;
        m_hflInvAngularSpan     = hflInvAngularSpan;
        m_hflAngularSpanIsZero  = hflAngularSpanIsZero;
    }

    override HRESULT SendShaderData(
        __inout_ecount(1) CHwPipelineShader *pShader
        );

private:
    MILSPHandle m_hflStartAngleRadians;
    MILSPHandle m_hflInvAngularSpan;
    MILSPHandle m_hflAngularSpanIsZero;
};



