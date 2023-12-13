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
//      This is the base mediabuffer class that instantiates either a Hardware
//      or a Software media buffer. It provides some basic code that is in
//      common to both the hardware and the software case, including some
//      virtual methods.
//
//  $ENDTAG
//
//------------------------------------------------------------------------------

#pragma once

MtExtern(CMFMediaBuffer);

class CMFMediaBuffer :
    public CMILCOMBase,
    public IMFMediaBuffer,
    public IMFGetService
{

public:

    static
    HRESULT
    Create(
        __in            UINT                uiID,
        __in            LONG                continuity,
        __in            UINT                uiWidth,
        __in            UINT                uiHeight,
        __in            D3DFORMAT           format,
        __in            CD3DDeviceLevel1    *pRenderDevice,
        __in            CD3DDeviceLevel1    *pMixerDevice,
        __in            D3DDEVTYPE          deviceType,
        __deref_out     CMFMediaBuffer      **ppMFMediaBuffer
        );

    // CMILCOMBase - declares IUnknown methods
    DECLARE_COM_BASE;

    //
    // IMFGetService
    //
    STDMETHOD(GetService)(
        /* [in] */ REFGUID guidService,
        /* [in] */ REFIID riid,
        __deref_out_ecount(1) LPVOID *ppvObject);


    //
    // IMFMediaBuffer
    //
    STDMETHOD(Lock)(
        __deref_out_bcount_part(*pcbMaxLength, *pcbCurrentLength) BYTE **ppbBuffer,
        __out_opt DWORD *pcbMaxLength,
        __out_opt DWORD *pcbCurrentLength
        );

    STDMETHOD(Unlock)();

    STDMETHOD(GetCurrentLength)(
        __out_ecount(1) DWORD *pcbCurrentLength
        );

    STDMETHOD(SetCurrentLength)(
        DWORD cbCurrentLength
        );

    STDMETHOD(GetMaxLength)(
        __out_ecount(1) DWORD *pcbMaxLength
        );

    //
    // Normal methods
    //
    virtual
    HRESULT
    GetBitmapSource(
        __in            bool                syncChannel,
        __in_opt        CD3DDeviceLevel1    *pDisplayDevice,
        __deref_out     IWGXBitmapSource    **ppIBitmapSource
        ) = 0;

    virtual
    void
    InvalidateCachedResources(
        void
        );

    virtual
    HRESULT
    DoneWithBitmap(
        void
        ) = 0;

    HRESULT
    GetDevice(
        __out   CD3DDeviceLevel1        **ppD3DDevice
        );

    inline
    LONG
    GetContinuity(
        void
        ) const;

protected:

    HRESULT
    Init(
        __in    IDirect3DSurface9       *pIDecodeSurface
        );

    virtual
    HRESULT
    Init(
        void
        ) = 0;

    CMFMediaBuffer(
        __in    UINT             uiID,
        __in    LONG             continuity,
        __in    UINT             uiWidth,
        __in    UINT             uiHeight,
        __in    D3DFORMAT        format,
        __in    CD3DDeviceLevel1 *pRenderDevice
        );

    virtual
    ~CMFMediaBuffer(
        );

    //
    // CMILCOMBase
    //
    STDMETHOD(HrFindInterface)(__in_ecount(1) REFIID riid, __deref_out void **ppv);

    //
    // These are useful to all derived classes.
    //
    UINT                m_uiID;
    UINT                m_uiWidth;
    UINT                m_uiHeight;
    D3DFORMAT           m_format;
    CD3DDeviceLevel1    *m_pRenderDevice;
    bool                m_systemMemoryValid;
    LONG                m_continuity;

private:

    //
    // Cannot copy or assign a CMFMediaBuffer
    //
    CMFMediaBuffer(
        __in    const CMFMediaBuffer    &
        );

    CMFMediaBuffer &
    operator=(
        __in    const CMFMediaBuffer    &
        );

    IMFMediaBuffer      *m_pIMFMediaBuffer;
    IMFGetService       *m_pIMFGetService;
};

#include "mediabuffer.inl"

