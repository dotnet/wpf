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
//      Internal Audio/Video interfaces
//
//  $ENDTAG
//
//------------------------------------------------------------------------------

#pragma once

class CD3DDeviceLevel1;
class CMilSlaveVideo;

interface IMediaDeviceConsumer
{
    STDMETHOD_(void, SetIDirect3DDevice9)(
        THIS_
        IN IDirect3DDevice9 *pIDirect3DDevice9
        ) PURE;
};

DECLARE_INTERFACE_(IAVSurfaceRenderer, IUnknown)
{
    STDMETHOD(BeginComposition)(
        THIS_
        IN      CMilSlaveVideo  *pCaller,
        IN      BOOL            displaySetChanged,
        IN      BOOL            syncChannel,
        IN OUT  LONGLONG        *pLastCompositionSampleTime,
        OUT     BOOL            *pbNewFrame
        ) PURE;

    STDMETHOD(BeginRender)(
        THIS_
        IN CD3DDeviceLevel1 *pDeviceLevel1,
        OUT IWGXBitmapSource **ppWICBitmapSource
        ) PURE;

    STDMETHOD(EndRender)(
        THIS_
        ) PURE;

    STDMETHOD(EndComposition)(
        THIS_
        IN  CMilSlaveVideo  *pCaller
        ) PURE;

    STDMETHOD(GetContentRectF)(
        THIS_
        OUT MilPointAndSizeF *prcContent
        ) = 0;

    STDMETHOD(GetContentRect)(
        THIS_
        OUT MilPointAndSizeL *prcContent
        ) = 0;
};

DECLARE_INTERFACE_(IMILSurfaceRendererProvider, IUnknown)
{
    STDMETHOD(GetSurfaceRenderer)(
        THIS_
        OUT IAVSurfaceRenderer **ppSurfaceRenderer
        ) PURE;

    STDMETHOD(RegisterResource)(
        THIS_
        IN CMilSlaveVideo *pSlaveVideo
        ) PURE;

    STDMETHOD(UnregisterResource)(
        THIS_
        IN CMilSlaveVideo *pSlaveVideo
        ) PURE;
};

HRESULT
AvDllInitialize(
    void
    );

void
AvDllShutdown(
    void
    );

HRESULT
AvDllCanUnloadNow(
    void
    );

HRESULT
AvDllGetClassObject(
    __in        REFCLSID        clsid,
    __in        REFIID          riid,
    __deref_out void            **ppv
    );

