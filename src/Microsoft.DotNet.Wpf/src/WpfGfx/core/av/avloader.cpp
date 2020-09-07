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
//      Maintains primary references to AV modules.  This is partially based on
//      CD3DLoader.
//
//  $ENDTAG
//
//  Module Name:
//      CAVLoader
//
//------------------------------------------------------------------------------

#include "precomp.hpp"
#include "avloader.tmh"

class CAVLoaderInternal
{
public:

    CAVLoaderInternal();
    ~CAVLoaderInternal();

    HRESULT Init();

    HRESULT GetEVRLoadRefAndCreateMedia(
        __deref_out_ecount(1) IMFSample **ppIMFSample
        );

    HRESULT GetEVRLoadRefAndCreateDXSurfaceBuffer(
        __in    REFIID riid,
        __in    IUnknown* punkSurface,
        __in    BOOL fBottomUpWhenLinear,
        __deref_out IMFMediaBuffer** ppBuffer
        );

    HRESULT GetDXVA2LoadRefAndCreateVideoAccelerationManager(
        __out UINT* resetToken,
        __deref_out_ecount(1) IDirect3DDeviceManager9** ppDXVAManager
        );

    HRESULT GetEVRLoadRefAndCreateEnhancedVideoRendererForDShow(
        __in                  IUnknown              *pOuterIUnknown,
        __deref_out_ecount(1) IUnknown              **ppInnerIUnknown
        );

    HRESULT GetEVRLoadRef();

    HRESULT GetDXVA2LoadRef();

    HRESULT ReleaseEVRLoadRef();

    HRESULT ReleaseDXVA2LoadRef();

    HRESULT CleanupEVR();

    HRESULT CleanupDXVA2();

    HRESULT CreateWmpOcx(__deref_out_ecount(1) IWMPPlayer **ppPlayer);

private:

    typedef
    HRESULT
    (WINAPI *
     FnDllGetClassObject)(
        REFCLSID            rClsID,
        REFIID              riid,
        __deref_out void    **pv
        );

    typedef 
    HRESULT 
    (WINAPI *
     MFCreateDXSurfaceBufferFunction)(
        REFIID riid,
        IUnknown* punkSurface,
        BOOL fBottomUpWhenLinear,
        __deref_out IMFMediaBuffer** ppBuffer
        ); 
    
    typedef HRESULT (WINAPI *MFCREATEMEDIAFUNCTION)(__in_opt IUnknown *pSurf, __deref_out_ecount(1) IMFSample **ppIMFSample);
    
    typedef HRESULT (WINAPI *DXVA2CREATEVIDEOACCELERATIONMANAGER)(__out UINT* resetToken, __deref_out_ecount(1) IDirect3DDeviceManager9** ppDXVAManager);

    CCriticalSection m_csManagement;

    HRESULT m_hrEVRInitialization;
    HRESULT m_hrDXVA2Initialization;
    HMODULE m_hEVR;
    HMODULE m_hDXVA2;
    MFCREATEMEDIAFUNCTION m_pfnMFCreateMedia;
    DXVA2CREATEVIDEOACCELERATIONMANAGER m_pfnDXVA2CreateVideoAccelerationManager;

    FnDllGetClassObject m_pfnEvrGetClassObject;
    MFCreateDXSurfaceBufferFunction m_pfnCreateDXSurfaceBufferFunction;

    ULONG m_cEVRRefs;
    ULONG m_cDXVA2Refs;
};

// This object's scope is limited to this file.  Any interaction
// with it should go through the CAVLoader class.
CAVLoaderInternal g_AVLoader;

BOOL g_fGlobalEVRLoadRef = FALSE;

//+-----------------------------------------------------------------------------
//
//  Member:
//      CAVLoader::Startup
//
//  Synopsis:
//      Initialize global D3D loader
//
//------------------------------------------------------------------------------
HRESULT
CAVLoader::Startup()
{
    Assert(g_fGlobalEVRLoadRef == FALSE);

    RRETURN(g_AVLoader.Init());
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CAVLoader::Shutdown
//
//  Synopsis:
//      Uninitialize global D3D loader
//
//------------------------------------------------------------------------------
void
CAVLoader::Shutdown()
{
    IGNORE_HR(GlobalReleaseEVRLoadRef());

    IGNORE_HR(g_AVLoader.CleanupEVR());
    IGNORE_HR(g_AVLoader.CleanupDXVA2());
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CAVLoader::GetEVRLoadRefAndCreateMedia
//
//  Synopsis:
//      Returns the IMFSample interface for this module and increments the EVR
//      module load count.
//
//------------------------------------------------------------------------------
HRESULT
CAVLoader::GetEVRLoadRefAndCreateMedia(
        __deref_out_ecount(1) IMFSample **ppIMFSample
    )
{
    RRETURN(g_AVLoader.GetEVRLoadRefAndCreateMedia(ppIMFSample));
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CAVLoader::GetEVRLoadRefAndCreateDXSurfaceBuffer
//
//  Synopsis:
//      Returns the IMFMediaBuffer interface for this module and
//      increments the EVR module load count.
//
//------------------------------------------------------------------------------
/*static*/
HRESULT
CAVLoader::GetEVRLoadRefAndCreateDXSurfaceBuffer(
    __in    REFIID riid,
    __in    IUnknown* punkSurface,
    __in    BOOL fBottomUpWhenLinear,
    __deref_out IMFMediaBuffer  **ppIMFBuffer
    )
{
    RRETURN(
        g_AVLoader.GetEVRLoadRefAndCreateDXSurfaceBuffer(
            riid,
            punkSurface,
            fBottomUpWhenLinear,
            ppIMFBuffer));
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      CAVLoader::GetDXVA2LoadRefAndCreateVideoAccelerationManager
//
//  Synopsis:
//      Returns the IDirect3DDeviceManager9 interface for this module and
//      increments the DXVA2 module load count.
//
//------------------------------------------------------------------------------
HRESULT
CAVLoader::GetDXVA2LoadRefAndCreateVideoAccelerationManager(
    __out UINT* resetToken,
    __deref_out_ecount(1)IDirect3DDeviceManager9** ppDXVAManager
    )
{
    RRETURN(g_AVLoader.GetDXVA2LoadRefAndCreateVideoAccelerationManager(resetToken, ppDXVAManager));
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CAVLoader::GetEVRLoadRefAndCreateEnhancedVideoRenderer
//
//  Synopsis:
//      Returns the IDirect3DDeviceManager9 interface for this module and
//      increments the DXVA2 module load count.
//
//------------------------------------------------------------------------------
HRESULT
CAVLoader::GetEVRLoadRefAndCreateEnhancedVideoRendererForDShow(
        __in                  IUnknown              *pOuterIUnknown,
        __deref_out_ecount(1) IUnknown              **ppInnerIUnknown
    )
{
    RRETURN(g_AVLoader.GetEVRLoadRefAndCreateEnhancedVideoRendererForDShow(pOuterIUnknown, ppInnerIUnknown));
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CAVLoader::GetEVRLoadRef
//
//  Synopsis:
//      Increase EVR load reference for this module.
//
//------------------------------------------------------------------------------
HRESULT
CAVLoader::GetEVRLoadRef()
{
    return g_AVLoader.GetEVRLoadRef();
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CAVLoader::GetDXVA2LoadRef
//
//  Synopsis:
//      Increase DXVA2 load reference for this module.
//
//------------------------------------------------------------------------------
HRESULT
CAVLoader::GetDXVA2LoadRef()
{
    return g_AVLoader.GetDXVA2LoadRef();
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CAVLoader::GlobalGetEVRLoadRef
//
//  Synopsis:
//      Increase EVR load reference for this module.
//
//------------------------------------------------------------------------------
HRESULT
CAVLoader::GlobalGetEVRLoadRef()
{
    if (!g_fGlobalEVRLoadRef)
    {
        g_fGlobalEVRLoadRef = TRUE;

        return g_AVLoader.GetEVRLoadRef();
    }
    else
    {
        return S_OK;
    }
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CAVLoader::ReleaseEVRLoadRef
//
//  Synopsis:
//      Handle release of EVR load reference for this module.
//
//------------------------------------------------------------------------------
HRESULT
CAVLoader::ReleaseEVRLoadRef()
{
    RRETURN(g_AVLoader.ReleaseEVRLoadRef());
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CAVLoader::ReleaseDXVA2LoadRef
//
//  Synopsis:
//      Handle release of DXVA2 load reference for this module.
//
//------------------------------------------------------------------------------
HRESULT
CAVLoader::ReleaseDXVA2LoadRef()
{
    RRETURN(g_AVLoader.ReleaseDXVA2LoadRef());
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CAVLoader::GlobalReleaseEVRLoadRef
//
//  Synopsis:
//      Global handle release of EVR load reference for this module.
//
//------------------------------------------------------------------------------
HRESULT
CAVLoader::GlobalReleaseEVRLoadRef()
{
    if (g_fGlobalEVRLoadRef)
    {
        g_fGlobalEVRLoadRef = FALSE;
        RRETURN(g_AVLoader.ReleaseEVRLoadRef());
    }
    else
    {
        RRETURN(S_OK);
    }
}

HRESULT
CAVLoader::CreateWmpOcx(__deref_out_ecount(1) IWMPPlayer **ppPlayer)
{
    RRETURN(g_AVLoader.CreateWmpOcx(ppPlayer));
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CAVLoaderInternal::CAVLoaderInternal
//
//  Synopsis:
//      ctor
//
//------------------------------------------------------------------------------
CAVLoaderInternal::CAVLoaderInternal()
{

    m_hrEVRInitialization = WGXERR_AV_MODULENOTLOADED;
    m_hrDXVA2Initialization = WGXERR_AV_MODULENOTLOADED;
    m_hEVR = NULL;
    m_hDXVA2 = NULL;
    m_pfnMFCreateMedia = NULL;
    m_pfnDXVA2CreateVideoAccelerationManager = NULL;
    m_pfnCreateDXSurfaceBufferFunction = NULL;
    m_pfnEvrGetClassObject = NULL;

    m_cEVRRefs = 0;
    m_cDXVA2Refs = 0;
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CAVLoaderInternal::~CAVLoaderInternal
//
//  Synopsis:
//      dtor
//
//------------------------------------------------------------------------------
CAVLoaderInternal::~CAVLoaderInternal()
{

    {
        CGuard<CCriticalSection> oGuard(m_csManagement);
    }
    m_csManagement.DeInit();
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CAVLoaderInternal::Init
//
//  Synopsis:
//      Initialize basic members
//
//------------------------------------------------------------------------------
HRESULT
CAVLoaderInternal::Init()
{
    HRESULT hr = S_OK;
    TRACEFID(0, &hr);

    Assert(!m_csManagement.IsValid());
    Assert(m_hrEVRInitialization == WGXERR_AV_MODULENOTLOADED);
    Assert(m_hrDXVA2Initialization == WGXERR_AV_MODULENOTLOADED);

    hr = m_csManagement.Init();

    Assert(m_csManagement.IsValid() || FAILED(hr));

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CAVLoaderInternal::GetEVRLoadRefAndCreateMedia
//
//  Synopsis:
//      Returns the IMFSample interface for this module and increments the EVR
//      module load count.
//
//------------------------------------------------------------------------------
HRESULT
CAVLoaderInternal::GetEVRLoadRefAndCreateMedia(
        __deref_out_ecount(1) IMFSample **ppIMFSample
    )
{
    HRESULT hr = S_OK;
    TRACEFID(0, &hr);
    BOOL fLoadRef = FALSE;

    *ppIMFSample = NULL;

    IFC(GetEVRLoadRef());
    Assert(m_hEVR != NULL);
    fLoadRef = TRUE;

    {
        CGuard<CCriticalSection> oGuard(m_csManagement);

        if (m_pfnMFCreateMedia == NULL)
        {
            m_pfnMFCreateMedia =
                (MFCREATEMEDIAFUNCTION) TW32(
                    NULL,
                    GetProcAddress(m_hEVR, "MFCreateVideoSampleFromSurface")
                    );

            if (!m_pfnMFCreateMedia)
            {
                IFC(HRESULT_FROM_WIN32(GetLastError()));
            }
        }
    }

    Assert(m_pfnMFCreateMedia != NULL);

    IFC(m_pfnMFCreateMedia(NULL, ppIMFSample));

Cleanup:
    if (FAILED(hr) && fLoadRef)
    {
        IGNORE_HR(ReleaseEVRLoadRef());
    }
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CAVLoaderInternal::GetEVRLoadRefAndCreateDXSurfaceBuffer
//
//  Synopsis:
//      Returns the IMFMediaBuffer interface for this module and
//      increments the EVR module load count.
//
//------------------------------------------------------------------------------
HRESULT
CAVLoaderInternal::GetEVRLoadRefAndCreateDXSurfaceBuffer(
    __in    REFIID riid,
    __in    IUnknown* punkSurface,
    __in    BOOL fBottomUpWhenLinear,
    __deref_out IMFMediaBuffer  **ppIMFBuffer
    )
{
    HRESULT hr = S_OK;
    TRACEFID(0, &hr);
    BOOL fLoadRef = FALSE;

    *ppIMFBuffer = NULL;

    IFC(GetEVRLoadRef());
    Assert(m_hEVR != NULL);
    fLoadRef = TRUE;

    {
        CGuard<CCriticalSection> oGuard(m_csManagement);

        if (m_pfnCreateDXSurfaceBufferFunction == NULL)
        {
            m_pfnCreateDXSurfaceBufferFunction =
                (MFCreateDXSurfaceBufferFunction)TW32(
                    NULL,
                    GetProcAddress(m_hEVR, "MFCreateDXSurfaceBuffer")
                    );

            if (!m_pfnCreateDXSurfaceBufferFunction)
            {
                IFC(GetLastErrorAsFailHR());
            }
        }
    }

    Assert(m_pfnCreateDXSurfaceBufferFunction != NULL);

    IFC(m_pfnCreateDXSurfaceBufferFunction(riid, punkSurface, fBottomUpWhenLinear, ppIMFBuffer));

Cleanup:

    if (FAILED(hr) && fLoadRef)
    {
        IGNORE_HR(ReleaseEVRLoadRef());
    }

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CAVLoaderInternal::GetDXVA2LoadRefAndCreateVideoAccelerationManager
//
//  Synopsis:
//      Returns the IDirect3DDeviceManager9 interface for this module and
//      increments the DXVA2 module load count.
//
//------------------------------------------------------------------------------
HRESULT
CAVLoaderInternal::GetDXVA2LoadRefAndCreateVideoAccelerationManager(
    __out UINT* resetToken,
    __deref_out_ecount(1) IDirect3DDeviceManager9** ppDXVAManager
    )
{
    HRESULT hr = S_OK;
    TRACEFID(0, &hr);
    BOOL fLoadRef = FALSE;

    *ppDXVAManager = NULL;

    IFC(GetDXVA2LoadRef());
    Assert(m_hDXVA2 != NULL);
    fLoadRef = TRUE;

    {
        CGuard<CCriticalSection> oGuard(m_csManagement);

        if (m_pfnDXVA2CreateVideoAccelerationManager == NULL)
        {
            m_pfnDXVA2CreateVideoAccelerationManager =
                (DXVA2CREATEVIDEOACCELERATIONMANAGER) TW32(
                        NULL,
                    GetProcAddress(m_hDXVA2, "DXVA2CreateDirect3DDeviceManager9")
                    );

            if (!m_pfnDXVA2CreateVideoAccelerationManager)
            {
                LogAVDataX(
                    AVTRACE_LEVEL_ERROR,
                    AVCOMP_DEFAULT,
                    "Failed to GetProcAddress" #
                    " [,%p]",
                    this);

                IFC(HRESULT_FROM_WIN32(GetLastError()));
            }
        }
    }

    Assert(m_pfnDXVA2CreateVideoAccelerationManager != NULL);

    LogAVDataX(
        AVTRACE_LEVEL_INFO,
        AVCOMP_DEFAULT,
        "Attempting to create manager" #
        " [,%p]",
        this);

    IFC(m_pfnDXVA2CreateVideoAccelerationManager(resetToken, ppDXVAManager));

Cleanup:

    if (FAILED(hr) && fLoadRef)
    {
        IGNORE_HR(ReleaseDXVA2LoadRef());
    }

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CAVLoaderInternal::GetEVRLoadRefAndCreateEnhancedVideoRenderer
//
//  Synopsis:
//      Returns the IDirect3DDeviceManager9 interface for this module and
//      increments the DXVA2 module load count.
//
//------------------------------------------------------------------------------
HRESULT
CAVLoaderInternal::GetEVRLoadRefAndCreateEnhancedVideoRendererForDShow(
        __in                  IUnknown              *pOuterIUnknown,
        __deref_out_ecount(1) IUnknown              **ppInnerIUnknown
    )
{
    HRESULT hr = S_OK;
    BOOL fLoadRef = FALSE;
    IClassFactory *pIClassFactory = NULL;
    IBaseFilter *pIBaseFilter = NULL;

    TRACEFID(0, &hr);

    IFC(GetEVRLoadRef());
    Assert(m_hEVR != NULL);
    fLoadRef = TRUE;

    {
        CGuard<CCriticalSection> oGuard(m_csManagement);

        if (NULL == m_pfnEvrGetClassObject)
        {
            m_pfnEvrGetClassObject =
                reinterpret_cast<FnDllGetClassObject>(
                    TW32(
                        NULL,
                        GetProcAddress(m_hEVR, "DllGetClassObject")));

            if (NULL == m_pfnEvrGetClassObject)
            {
                LogAVDataX(
                    AVTRACE_LEVEL_ERROR,
                    AVCOMP_DEFAULT,
                    "Failed to GetProcAddress" #
                    " [,%p]",
                    this);

                IFC(HRESULT_FROM_WIN32(GetLastError()));
            }
        }
    }

    Assert(m_pfnEvrGetClassObject != NULL);

    //
    // Need to get the class factory here.
    //
    IFC(
        m_pfnEvrGetClassObject(
            CLSID_EnhancedVideoRenderer,
            __uuidof(IClassFactory),
            reinterpret_cast<void **>(&pIClassFactory)));

    IFC(
        pIClassFactory->CreateInstance(
            pOuterIUnknown,
            IID_IUnknown,
            reinterpret_cast<void **>(ppInnerIUnknown)));

Cleanup:

    ReleaseInterface(pIClassFactory);
    ReleaseInterface(pIBaseFilter);

    if (FAILED(hr) && fLoadRef)
    {
        IGNORE_HR(ReleaseEVRLoadRef());
    }
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CAVLoaderInternal::GetEVRLoadRef
//
//  Synopsis:
//      Increase EVR load reference for this module.
//
//------------------------------------------------------------------------------
HRESULT
CAVLoaderInternal::GetEVRLoadRef()
{
    HRESULT hr = S_OK;
    TRACEFID(0, &hr);

    CGuard<CCriticalSection> oGuard(m_csManagement);

    if (m_cEVRRefs == 0 && m_hEVR == NULL)
    {
        Assert(FAILED(m_hrEVRInitialization));
        m_hEVR = TW32(NULL, LoadLibrary(TEXT("evr.dll")));
        if (!m_hEVR)
        {
            IFC(HRESULT_FROM_WIN32(GetLastError()));
        }

        m_hrEVRInitialization = S_OK;
    }

    m_cEVRRefs++;

    Assert(m_cEVRRefs > 0 && m_hEVR != NULL);

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CAVLoaderInternal::GetEVRLoadRef
//
//  Synopsis:
//      Increase DXVA2 load reference for this module.
//
//------------------------------------------------------------------------------
HRESULT
CAVLoaderInternal::GetDXVA2LoadRef(
    )
{
    HRESULT hr = S_OK;
    TRACEFID(0, &hr);

    CGuard<CCriticalSection> oGuard(m_csManagement);

    if (m_cDXVA2Refs == 0 && m_hDXVA2 == NULL)
    {
        Assert(FAILED(m_hrDXVA2Initialization));
        m_hDXVA2 = TW32(NULL, LoadLibrary(TEXT("dxva2.dll")));
        if (!m_hDXVA2)
        {
            IFC(HRESULT_FROM_WIN32(GetLastError()));
        }

        m_hrDXVA2Initialization = S_OK;
    }

    m_cDXVA2Refs++;

    Assert(m_cDXVA2Refs > 0 && m_hDXVA2 != NULL);

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CAVLoaderInternal::ReleaseEVRLoadRef
//
//  Synopsis:
//      Handle release of EVR load reference for this module.
//
//------------------------------------------------------------------------------
HRESULT
CAVLoaderInternal::ReleaseEVRLoadRef(
    )
{
    HRESULT hr = S_OK;
    TRACEFID(0, &hr);

    CGuard<CCriticalSection> oGuard(m_csManagement);

    Assert(m_cEVRRefs > 0);

    if (--m_cEVRRefs == 0)
    {
        IFC(CleanupEVR());
    }

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CAVLoaderInternal::ReleaseDXVA2LoadRef
//
//  Synopsis:
//      Handle release of DXVA2 load reference for this module.
//
//------------------------------------------------------------------------------
HRESULT
CAVLoaderInternal::ReleaseDXVA2LoadRef(
    )
{
    HRESULT hr = S_OK;
    TRACEFID(0, &hr);

    CGuard<CCriticalSection> oGuard(m_csManagement);

    Assert(m_cDXVA2Refs > 0);

    if (--m_cDXVA2Refs == 0)
    {
        IFC(CleanupDXVA2());
    }

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CAVLoaderInternal::CleanupEVR
//
//  Synopsis:
//      Handle release of EVR load reference for this module.
//
//------------------------------------------------------------------------------
HRESULT
CAVLoaderInternal::CleanupEVR(
    )
{
    HRESULT hr = S_OK;
    TRACEFID(0, &hr);

    if (SUCCEEDED(m_hrEVRInitialization))
    {
        FreeLibrary(m_hEVR);
        m_pfnMFCreateMedia = NULL;
        m_pfnDXVA2CreateVideoAccelerationManager = NULL;
        m_pfnEvrGetClassObject = NULL;
        m_pfnCreateDXSurfaceBufferFunction = NULL;
        m_hrEVRInitialization = WGXERR_AV_MODULENOTLOADED;
        m_cEVRRefs = 0;
        m_hEVR = NULL;
    }

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CAVLoaderInternal::CleanupDXVA2
//
//  Synopsis:
//      Handle release of DXVA2 load reference for this module.
//
//------------------------------------------------------------------------------
HRESULT
CAVLoaderInternal::CleanupDXVA2(
    )
{
    HRESULT hr = S_OK;
    TRACEFID(0, &hr);

    if (SUCCEEDED(m_hrDXVA2Initialization))
    {
        FreeLibrary(m_hDXVA2);

        m_pfnDXVA2CreateVideoAccelerationManager = NULL;
        m_hrDXVA2Initialization = WGXERR_AV_MODULENOTLOADED;
        m_cDXVA2Refs = 0;
        m_hDXVA2 = NULL;
    }

    RRETURN(hr);
}

HRESULT
CAVLoaderInternal::CreateWmpOcx(
    __deref_out_ecount(1) IWMPPlayer **ppPlayer
    )
{
    HRESULT hr = S_OK;

    TRACEFID(0, &hr);
    {
        CGuard<CCriticalSection> oGuard(m_csManagement);
        IFC(CoCreateInstance(CLSID_WindowsMediaPlayer, NULL, CLSCTX_INPROC_SERVER, __uuidof(IWMPPlayer), (void**)ppPlayer));
    }

Cleanup:
    RRETURN(hr);
}



