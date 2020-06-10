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
//      Header for the CDXVAManagerWrapper class, which wraps an instance of the
//      IDirect3DSurface9 interface. This wrapper was written for the purpose of
//      logging D3D calls, but it may also be used to restrict and/or redirect
//      D3D calls.
//
//  $ENDTAG
//
//------------------------------------------------------------------------------

#pragma once

MtExtern(CDXVAManagerWrapper);

class CDXVAManagerWrapper :
    public CMILCOMBase,
    public IDirect3DDeviceManager9
{
public:
    DECLARE_METERHEAP_CLEAR(ProcessHeap, Mt(CDXVAManagerWrapper));

    static HRESULT Create(
        UINT uiID,
        UINT* resetToken,
        __deref_out_ecount(1) CDXVAManagerWrapper **ppDXVAManagerWrapper
        );

public:
    DECLARE_COM_BASE;

    //
    // IDirect3DDeviceManager9
    //

    STDMETHOD(ResetDevice)(
        /* [in] */ IDirect3DDevice9 *pDevice,
        /* [in] */ UINT Reason);

    STDMETHOD(OpenDeviceHandle)(
        /* [out] */ HANDLE *phDevice);

    STDMETHOD(CloseDeviceHandle)(
        /* [in] */ HANDLE hDevice);

    STDMETHOD(TestDevice)(
        /* [in] */ HANDLE hDevice);

    STDMETHOD(LockDevice)(
        /* [in] */ HANDLE hDevice,
        /* [out] */ IDirect3DDevice9 **ppDevice,
        /* [in] */ BOOL fBlock);

    STDMETHOD(UnlockDevice)(
        /* [in] */ HANDLE hDevice,
        /* [in] */ BOOL fSaveState);

    STDMETHOD(GetVideoService)(
        /* [in] */ HANDLE hDevice,
        /* [in] */ REFIID riid,
        /* [out] */ void **ppAccelServices);

protected:
    //
    // CMILCOMBase
    //
    STDMETHOD(HrFindInterface)(__in_ecount(1) REFIID riid, __deref_out void **ppv);

    CDXVAManagerWrapper(UINT uiID);
    virtual ~CDXVAManagerWrapper();

    HRESULT Initialize();

private:

    UINT m_uiID;
    IDirect3DDevice9 *m_pIDirect3DDevice9;
    IDirect3DDeviceManager9 *m_pDXVAManager;
    CCriticalSection m_csEntry;
};

