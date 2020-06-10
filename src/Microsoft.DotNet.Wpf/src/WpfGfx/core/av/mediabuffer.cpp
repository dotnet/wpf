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
//
//  $ENDTAG
//
//------------------------------------------------------------------------------

#include "precomp.hpp"
#include "mediabuffer.tmh"

// +---------------------------------------------------------------------------
//
// CMFMediaBuffer::CMFMediaBuffer
//
// +---------------------------------------------------------------------------
CMFMediaBuffer::
CMFMediaBuffer(
    __in    UINT                uiID,
    __in    LONG                continuity,
    __in    UINT                uiWidth,
    __in    UINT                uiHeight,
    __in    D3DFORMAT           format,
    __in    CD3DDeviceLevel1    *pRenderDevice
    ) :
    m_uiID(uiID),
    m_uiWidth(uiWidth),
    m_uiHeight(uiHeight),
    m_format(format),
    m_pIMFMediaBuffer(NULL),
    m_pIMFGetService(NULL),
    m_pRenderDevice(NULL),
    m_systemMemoryValid(false),
    m_continuity(continuity)
{
    TRACEF(NULL);

    AddRef();

    // CMFMediaBuffer holds D3D load references
    CD3DLoader::GetLoadRef();

    SetInterface(m_pRenderDevice, pRenderDevice);
}

// +---------------------------------------------------------------------------
//
// CMFMediaBuffer::~CMFMediaBuffer
//
// +---------------------------------------------------------------------------
CMFMediaBuffer::~CMFMediaBuffer()
{
    TRACEF(NULL);

    if (m_pIMFMediaBuffer)
    {
        m_pIMFMediaBuffer->Release();

        IGNORE_HR(CAVLoader::ReleaseEVRLoadRef());
    }

    ReleaseInterface(m_pIMFGetService);
    ReleaseInterface(m_pRenderDevice);

    CD3DLoader::ReleaseLoadRef();
}

// +---------------------------------------------------------------------------
//
// CMFMediaBuffer::Init
//
// +---------------------------------------------------------------------------
HRESULT
CMFMediaBuffer::
Init(
    __in    IDirect3DSurface9       *pIDecodeSurface
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    //
    // Create a DXSurface buffer over our D3D9 surface.
    //
    IFC(CAVLoader::GetEVRLoadRefAndCreateDXSurfaceBuffer(IID_IDirect3DSurface9, pIDecodeSurface, FALSE, &m_pIMFMediaBuffer));

    IFC(
        m_pIMFMediaBuffer->QueryInterface(
            __uuidof(IMFGetService),
            reinterpret_cast<void **>(&m_pIMFGetService)));

Cleanup:

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

// +---------------------------------------------------------------------------
//
// CMFMediaBuffer::Create
//
// +---------------------------------------------------------------------------
/*static*/
HRESULT
CMFMediaBuffer::
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
    )
{
    HRESULT hr = S_OK;
    TRACEFID(uiID, &hr);
    CMFMediaBuffer *pMFMediaBuffer = NULL;

    switch(deviceType)
    {
    case D3DDEVTYPE_HAL:

        //
        // We have removed the shared surface optimization so we never
        // use CLDDMHWMFMediaBuffer
        //
        pMFMediaBuffer
            = new CHWMFMediaBuffer(
                        uiID,
                        continuity,
                        uiWidth,
                        uiHeight,
                        format,
                        pRenderDevice,
                        pMixerDevice);
        break;

    case D3DDEVTYPE_SW:

        //
        // There is only one software device
        //
        Assert(pRenderDevice == pMixerDevice);

        pMFMediaBuffer
            = new CSWMFMediaBuffer(
                        uiID,
                        continuity,
                        uiWidth,
                        uiHeight,
                        format,
                        pRenderDevice);
        break;

    default:

        //
        // We only support HAL and SW buffers.
        //
        IFC(E_INVALIDARG);
        break;
    }

    IFCOOM(pMFMediaBuffer);

    //
    // Do all of the initialization that can fail.
    //
    IFC(pMFMediaBuffer->Init());

    *ppMFMediaBuffer = pMFMediaBuffer;
    pMFMediaBuffer = NULL;

Cleanup:
    ReleaseInterface(pMFMediaBuffer);
    RRETURN(hr);
}

//+---------------------------------------------------------------------------
//
// CMFMediaBuffer::Lock, IMFMediaBuffer
//
//+---------------------------------------------------------------------------
HRESULT
CMFMediaBuffer::Lock(
    __deref_out_bcount_part(*pcbMaxLength, *pcbCurrentLength) BYTE **ppbBuffer,
    __out_opt DWORD *pcbMaxLength,
    __out_opt DWORD *pcbCurrentLength
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    IFC(
        m_pIMFMediaBuffer->Lock(
            ppbBuffer,
            pcbMaxLength,
            pcbCurrentLength));

Cleanup:

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

// +---------------------------------------------------------------------------
//
// CMFMediaBuffer::Unlock, IMFMediaBuffer
//
// +---------------------------------------------------------------------------
HRESULT CMFMediaBuffer::Unlock()
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    IFC(m_pIMFMediaBuffer->Unlock());

Cleanup:

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

// +---------------------------------------------------------------------------
//
// CMFMediaBuffer::GetCurrentLength, IMFMediaBuffer
//
// +---------------------------------------------------------------------------
HRESULT CMFMediaBuffer::GetCurrentLength(
    __out_ecount(1) DWORD *pcbCurrentLength
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    IFC(m_pIMFMediaBuffer->GetCurrentLength(pcbCurrentLength));

Cleanup:

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

// +---------------------------------------------------------------------------
//
// CMFMediaBuffer::SetCurrentLength, IMFMediaBuffer
//
// +---------------------------------------------------------------------------
HRESULT CMFMediaBuffer::SetCurrentLength(
    DWORD cbCurrentLength
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    IFC(m_pIMFMediaBuffer->SetCurrentLength(cbCurrentLength));

Cleanup:

    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

// +---------------------------------------------------------------------------
//
// CMFMediaBuffer::GetMaxLength, IMFMediaBuffer
//
// +---------------------------------------------------------------------------
HRESULT CMFMediaBuffer::GetMaxLength(
    __out_ecount(1) DWORD *pcbMaxLength
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    IFC(m_pIMFMediaBuffer->GetMaxLength(pcbMaxLength));

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CMFMediaBuffer::InvalidateCachedResources
//
//  Synopsis:
//      Signals that the system memory bitmap is invalidated, lets us know to
//      copy the surface from the hardware cache to a system memory surface if
//      it is stale.
//
//------------------------------------------------------------------------------
void
CMFMediaBuffer::
InvalidateCachedResources(
    void
    )
{
    m_systemMemoryValid = false;
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      CMFMediaBuffer::GetService, IMFGetService
//
//------------------------------------------------------------------------------
STDMETHODIMP
CMFMediaBuffer::GetService(
    /* [in] */ REFGUID guidService,
    /* [in] */ REFIID riid,
    __deref_out_ecount(1) LPVOID *ppvObject)
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    IFC(m_pIMFGetService->GetService(guidService, riid, ppvObject));

Cleanup:

    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CMFMediaBuffer::GetDevice
//
//  Synopsis:
//      Retrieve the D3D device associated with this media buffer. We use this
//      to check whether this buffer was processed with a particular device.
//
//------------------------------------------------------------------------------
HRESULT
CMFMediaBuffer::
GetDevice(
    __out   CD3DDeviceLevel1        **ppD3DDevice
    )
{
    *ppD3DDevice = m_pRenderDevice;
    (*ppD3DDevice)->AddRef();

    return S_OK;
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CMFMediaBuffer::HrFindInterface, CMILCOMBase
//
//------------------------------------------------------------------------------
HRESULT CMFMediaBuffer::HrFindInterface(
    __in_ecount(1) REFIID riid,
    __deref_out void **ppvObject
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    if (!ppvObject)
    {
        IFCN(E_INVALIDARG);
    }

    if (riid == __uuidof(IMFMediaBuffer))
    {
        // No AddRef because CMILCOMBase does it for me
        *ppvObject = static_cast<IMFMediaBuffer *>(this);
    }
    else if (riid == __uuidof(IMFGetService))
    {
        // No AddRef because CMILCOMBase does it for me
        *ppvObject = static_cast<IMFGetService *>(this);
    }
    else if (riid == IID_CMFMediaBuffer)
    {
        // No AddRef because CMILCOMBase does it for me
        *ppvObject = static_cast<CMFMediaBuffer *>(this);
    }
    else
    {
        LogAVDataM(
            AVTRACE_LEVEL_ERROR,
            AVCOMP_BUFFER,
            "Unexpected interface request: %!IID!",
            &riid);

        IFCN(E_NOINTERFACE);
    }

Cleanup:
    RRETURN(hr);
}


