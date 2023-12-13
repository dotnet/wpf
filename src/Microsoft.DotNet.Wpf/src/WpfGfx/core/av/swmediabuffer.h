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
//      This provides the implementation that handles a buffer that we use for
//      decoding in software.
//
//  $ENDTAG
//
//------------------------------------------------------------------------------
#pragma once

MtExtern(CSWMFMediaBuffer);

class CSWMFMediaBuffer : public CMFMediaBuffer
{
public:

    DECLARE_METERHEAP_CLEAR(ProcessHeap, Mt(CSWMFMediaBuffer));

    CSWMFMediaBuffer(
        __in    UINT             uiID,
        __in    LONG             continuity,
        __in    UINT             uiWidth,
        __in    UINT             uiHeight,
        __in    D3DFORMAT        format,
        __in    CD3DDeviceLevel1 *pRenderDevice
        );

    __override
    ~CSWMFMediaBuffer(
        void
        );

    __override
    HRESULT
    GetBitmapSource(
        __in            bool                syncChannel,
        __in_opt        CD3DDeviceLevel1    *pDisplayDevice,
        __deref_out     IWGXBitmapSource    **ppIBitmapSource
        );

    __override
    HRESULT
    DoneWithBitmap(
        void
        );

protected:

    __override
    HRESULT
    Init(
        void
        );

private:

    //
    // Cannot copy or assign a Hardware media buffer
    //
    CSWMFMediaBuffer(
        __in    const CSWMFMediaBuffer &
        );

    CSWMFMediaBuffer &
    operator=(
        __in    const CSWMFMediaBuffer &
        );

    HRESULT
    CreateCompositionObjects(
        void
        );

    HRESULT
    AliasBitmap(
        __in    CClientMemoryBitmap         *pBitmap,
        __in    bool                        initializing
        );

    IDirect3DSurface9       *m_pIBitmapSurface;
    CClientMemoryBitmap     *m_pBitmap;
};

