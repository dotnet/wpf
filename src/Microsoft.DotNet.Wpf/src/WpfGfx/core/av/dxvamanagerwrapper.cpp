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
#include "dxvamanagerwrapper.tmh"

MtDefine(CDXVAManagerWrapper, Mem, "CDXVAManagerWrapper");

// +---------------------------------------------------------------------------
//
// CDXVAManagerWrapper::CDXVAManagerWrapper
//
// +---------------------------------------------------------------------------
CDXVAManagerWrapper::
CDXVAManagerWrapper(
    UINT uiID
    ) : m_uiID(uiID),
        m_pIDirect3DDevice9(NULL),
        m_pDXVAManager(NULL)
{
    TRACEF(NULL);
    AddRef();


    //
    // The critical section initialization can fail on Windows XP, so it will
    // be done separately in the Initialize method
    //
}

// +---------------------------------------------------------------------------
//
// CDXVAManagerWrapper::~CDXVAManagerWrapper
//
// +---------------------------------------------------------------------------
CDXVAManagerWrapper::~CDXVAManagerWrapper()
{
    TRACEF(NULL);

    ReleaseInterface(m_pDXVAManager);

    // The constructor is private and this class is only creatable through Create() which guarantees
    // we've added a ref on the DXVA2 module.
    IGNORE_HR(CAVLoader::ReleaseDXVA2LoadRef());

    //
    // Release the D3D device and load ref. We do this step last, since
    // we're still holding onto D3D stuff until this point.
    //
    IGNORE_HR(ResetDevice(NULL, 0));
}

// +---------------------------------------------------------------------------
//
// CDXVAManagerWrapper::Initialize
//
// +---------------------------------------------------------------------------
HRESULT CDXVAManagerWrapper::Initialize()
{
    HRESULT hr = S_OK;

    IFC(m_csEntry.Init());

Cleanup:
    RRETURN(hr);
}

// +---------------------------------------------------------------------------
//
// CDXVAManagerWrapper::Create
//
// +---------------------------------------------------------------------------
HRESULT CDXVAManagerWrapper::Create(
    UINT uiID,
    UINT* resetToken,
    __deref_out_ecount(1) CDXVAManagerWrapper **ppDXVAManagerWrapper
    )
{
    HRESULT hr = S_OK;
    TRACEFID(uiID, &hr);
    CDXVAManagerWrapper *pDXVAManagerWrapper = NULL;
    IDirect3DDeviceManager9 *pManager = NULL;
    BOOL fLoaded = FALSE;

    CHECKPTRARG(ppDXVAManagerWrapper);
    *ppDXVAManagerWrapper = NULL;

    IFC(CAVLoader::GetDXVA2LoadRefAndCreateVideoAccelerationManager(resetToken, &pManager));
    fLoaded = TRUE;

    pDXVAManagerWrapper = new CDXVAManagerWrapper(uiID);
    IFCOOM(pDXVAManagerWrapper);
    IFC(pDXVAManagerWrapper->Initialize());
    // No need for AddRef as CDXVAManagerWrapper is AddRef'd in the constructor

    pDXVAManagerWrapper->m_pDXVAManager = pManager;
    pDXVAManagerWrapper->m_pDXVAManager->AddRef();

    // Transfer reference
    *ppDXVAManagerWrapper = pDXVAManagerWrapper;
    pDXVAManagerWrapper = NULL;

Cleanup:
    if (FAILED(hr) && fLoaded)
    {
        IGNORE_HR(CAVLoader::ReleaseDXVA2LoadRef());
    }

    ReleaseInterface(pDXVAManagerWrapper);
    ReleaseInterface(pManager);
    RRETURN(hr);
}


//
// IDirect3DDeviceManager9
//

STDMETHODIMP
CDXVAManagerWrapper::ResetDevice(
    /* [in] */ IDirect3DDevice9 *pDevice,
    /* [in] */ UINT resetToken)
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_DXVAMANWRAP,
        "ResetDevice(0x%p, %u)",
        pDevice,
        resetToken);

    CGuard<CCriticalSection> guard(m_csEntry);

    if (m_pIDirect3DDevice9 == NULL && pDevice != NULL)
    {
        CD3DLoader::GetLoadRef();
        SetInterface(m_pIDirect3DDevice9, pDevice);
    }
    else if (m_pIDirect3DDevice9 != NULL && pDevice == NULL)
    {
        ReleaseInterface(m_pIDirect3DDevice9);
        CD3DLoader::ReleaseLoadRef();
    }
    else
    {
        // D3D load ref remains unchanged.
        ReplaceInterface(m_pIDirect3DDevice9, pDevice);
    }

    if (pDevice)
    {
        IFC(m_pDXVAManager->ResetDevice(pDevice, resetToken));
    }

Cleanup:
    RRETURN(hr);
}

STDMETHODIMP
CDXVAManagerWrapper::OpenDeviceHandle(
    /* [out] */ HANDLE *phDevice)
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    CGuard<CCriticalSection> guard(m_csEntry);
    IFC(m_pDXVAManager->OpenDeviceHandle(phDevice));
Cleanup:
    RRETURN(hr);
}

STDMETHODIMP
CDXVAManagerWrapper::CloseDeviceHandle(
    /* [in] */ HANDLE hDevice)
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    CGuard<CCriticalSection> guard(m_csEntry);
    IFC(m_pDXVAManager->CloseDeviceHandle(hDevice));
Cleanup:
    RRETURN(hr);
}

STDMETHODIMP
CDXVAManagerWrapper::TestDevice(
    /* [in] */ HANDLE hDevice)
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    CGuard<CCriticalSection> guard(m_csEntry);
    IFC(m_pDXVAManager->TestDevice(hDevice));
Cleanup:
    RRETURN(hr);
}

STDMETHODIMP
CDXVAManagerWrapper::LockDevice(
    /* [in] */ HANDLE hDevice,
    /* [out] */ IDirect3DDevice9 **ppDevice,
    /* [in] */ BOOL fBlock)
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    bool fEntryLockObtained = false;

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_DXVAMANWRAP,
        "LockDevice(%p, (not logged), %!bool!)",
        hDevice,
        fBlock);

    if (fBlock)
    {
        m_csEntry.Enter();
        fEntryLockObtained = true;
    }
    else
    {
        fEntryLockObtained = m_csEntry.TryEnter();
        if (!fEntryLockObtained)
        {
            IFC(DXVA2_E_VIDEO_DEVICE_LOCKED);
        }
    }

    IFC(m_pDXVAManager->LockDevice(hDevice, ppDevice, fBlock));

Cleanup:

    if (fEntryLockObtained)
    {
        m_csEntry.Leave();
    }

    RRETURN(hr);
}

STDMETHODIMP
CDXVAManagerWrapper::UnlockDevice(
    /* [in] */ HANDLE hDevice,
    /* [in] */ BOOL fSaveState)
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    CGuard<CCriticalSection> guard(m_csEntry);

    IFC(m_pDXVAManager->UnlockDevice(hDevice, fSaveState));

Cleanup:

    RRETURN(hr);
}

STDMETHODIMP
CDXVAManagerWrapper::GetVideoService(
    /* [in] */ HANDLE hDevice,
    /* [in] */ REFIID riid,
    /* [out] */ void **ppAccelServices)
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    CGuard<CCriticalSection> guard(m_csEntry);
    IFC(m_pDXVAManager->GetVideoService(hDevice, riid, ppAccelServices));
Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpOcxSetup::HrFindInterface, CMILCOMBase
//
//  Synopsis:
//            Get a pointer to another interface implemented by
//      CDXVAManagerWrapper
//
//------------------------------------------------------------------------------
STDMETHODIMP
CDXVAManagerWrapper::HrFindInterface(
    __in_ecount(1) REFIID riid,
    __deref_out void **ppvObject
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    CGuard<CCriticalSection> guard(m_csEntry);

    if (!ppvObject)
    {
        IFCN(E_INVALIDARG);
    }

    if (riid == __uuidof(IDirect3DDeviceManager9))
    {
        *ppvObject = static_cast<IDirect3DDeviceManager9*>(this);
    }
    else
    {
        LogAVDataM(
            AVTRACE_LEVEL_ERROR,
            AVCOMP_DXVAMANWRAP,
            "Unexpected interface request: %!IID!",
            &riid);

        IFCN(E_NOINTERFACE);
    }

Cleanup:
    RRETURN(hr);
}

