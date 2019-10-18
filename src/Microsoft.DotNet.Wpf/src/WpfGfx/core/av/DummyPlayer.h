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
#pragma once

MtExtern(DummySurfaceRenderer);

class DummySurfaceRenderer : public IAVSurfaceRenderer,
                             public CMILCOMBase
{
public:

    DECLARE_METERHEAP_ALLOC(ProcessHeap, Mt(DummySurfaceRenderer));

    static
    HRESULT
    Create(
        __in        MediaInstance           *pMediaInstance,
        __deref_out DummySurfaceRenderer    **ppDummySurfaceRenderer
        );

    DECLARE_COM_BASE;

    //
    // IAVSurfaceRenderer
    //
    STDMETHOD(BeginComposition)(
        __in    CMilSlaveVideo  *pCaller,
        __in    BOOL            displaySetChanged,
        __in    BOOL            syncChannel,
        __inout LONGLONG        *pLastCompositionSampleTime,
        __out   BOOL            *pbFrameReady
        );

    STDMETHOD(BeginRender)(
        __in_ecount_opt(1)    CD3DDeviceLevel1 *pDeviceLevel1,        // NULL OK (in SW)
        __deref_out_ecount(1) IWGXBitmapSource **ppMILBitmapSource
        );

    STDMETHOD(EndRender)(
        );

    STDMETHOD(EndComposition)(
        __in    CMilSlaveVideo  *pCaller
        );

    STDMETHOD(GetContentRect)(
        __out_ecount(1) MilPointAndSizeL *prcContent
        );

    STDMETHOD(GetContentRectF)(
        __out_ecount(1) MilPointAndSizeF *prcContent
        );

    STDMETHOD_(void, SetIDirect3DDevice9)(
        __in IDirect3DDevice9 *pIDirect3DDevice9
        );

    //
    // Regular methods
    //
    void
    ForceFrameUpdate(
        __in    UINT    width,
        __in    UINT    height
        );

protected:
    //
    // CMILCOMBase
    //
    STDMETHOD(HrFindInterface)(
        __in_ecount(1) REFIID riid,
        __deref_out void **ppvObject
        );

private:

    //
    // Cannot copy or assign a Dummy Player
    //
    DummySurfaceRenderer(
        __in    const DummySurfaceRenderer  &
        );

    DummySurfaceRenderer &
    operator=(
        __in    const DummySurfaceRenderer &
        );

    DummySurfaceRenderer(
        __in        MediaInstance       *pMediaInstance
        );

    ~DummySurfaceRenderer(
        void
        );

    HRESULT
    InitializeDummySource(
        __in        UINT                width,
        __in        UINT                height
        );

    UINT                m_uiID;
    MediaInstance       *m_pMediaInstance;
    CDummySource        *m_pDummySource;
    bool                m_needToUpdate;

    UINT                m_mediaWidth;
    UINT                m_mediaHeight;

    static const UINT       msc_mediaWidth = 320;
    static const UINT       msc_mediaHeight = 200;
};


