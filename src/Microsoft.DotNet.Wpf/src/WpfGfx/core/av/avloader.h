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
//      Maintains primary references to MF related modules.  This is partially
//      based on CD3DLoader.
//
//  $ENDTAG
//
//  Module Name:
//      CAVLoader
//
//------------------------------------------------------------------------------

class CEvrFilterWrapper;

class CAVLoader
{
public:

    static HRESULT Startup();
    static void Shutdown();

    static HRESULT GetEVRLoadRefAndCreateMedia(
        __deref_out_ecount(1) IMFSample **ppIMFSample
        );

    static HRESULT GetEVRLoadRefAndCreateDXSurfaceBuffer(
        __in    REFIID riid,
        __in    IUnknown* punkSurface,
        __in    BOOL fBottomUpWhenLinear,
        __deref_out IMFMediaBuffer** ppBuffer
        );

    static HRESULT GetEVRLoadRefAndCreateEnhancedVideoRendererForDShow(
        __in                  IUnknown              *pOuterIUnknown,
        __deref_out_ecount(1) IUnknown              **ppInnerIUnknown
        );

    static HRESULT GetDXVA2LoadRefAndCreateVideoAccelerationManager(
        __out UINT* resetToken,
        __deref_out_ecount(1) IDirect3DDeviceManager9** ppDXVAManager
        );

    static HRESULT GetEVRLoadRef();

    static HRESULT GetDXVA2LoadRef();

    static HRESULT ReleaseEVRLoadRef();

    static HRESULT ReleaseDXVA2LoadRef();

    static HRESULT GlobalGetEVRLoadRef();

    static HRESULT GlobalReleaseEVRLoadRef();

    static HRESULT CreateWmpOcx(__deref_out_ecount(1) IWMPPlayer **ppPlayer);
};






