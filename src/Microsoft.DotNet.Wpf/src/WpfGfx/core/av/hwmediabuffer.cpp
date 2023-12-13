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
//      This provides the implementation that handles a hardware media buffer.
//      (I.e. a buffer that is only decoded to by hardware video processing).
//
//  $ENDTAG
//
//------------------------------------------------------------------------------
#include "precomp.hpp"
#include "hwmediabuffer.tmh"

MtDefine(CHWMFMediaBuffer, Mem, "CHWMFMediaBuffer");

//
// Public methods
//
CHWMFMediaBuffer::
CHWMFMediaBuffer(
    __in    UINT             uiID,
    __in    LONG             continuity,
    __in    UINT             uiWidth,
    __in    UINT             uiHeight,
    __in    D3DFORMAT        format,
    __in    CD3DDeviceLevel1 *pRenderDevice,
    __in    CD3DDeviceLevel1 *pMixerDevice
    ) : CMFMediaBuffer(
            uiID,
            continuity,
            uiWidth,
            uiHeight,
            format,
            pRenderDevice),
        m_pIMixerSurface(NULL),
        m_pIBitmapSurface(NULL),
        m_pIMixerTexture(NULL),
        m_fTextureCachedOnBitmap(false),
        m_pBitmap(NULL),
        m_surfaceLocked(false)
{
    TRACEF(NULL);

    SetInterface(m_pMixerDevice, pMixerDevice);
}

__override
CHWMFMediaBuffer::
~CHWMFMediaBuffer(
    void
    )
{
    TRACEF(NULL);

    ReleaseInterface(m_pMixerDevice);
    ReleaseInterface(m_pIMixerTexture);

    //
    // If we have asked to copy our contents over to system memory,
    // this will be locked.
    //
    if (m_surfaceLocked)
    {
        IGNORE_HR(m_pIBitmapSurface->UnlockRect());
    }

    ReleaseInterface(m_pIBitmapSurface);
    ReleaseInterface(m_pIMixerSurface);

    ReleaseInterface(m_pBitmap);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CHWMFMediaBuffer::GetBitmapSource
//
//  Synopsis:
//      Returns a bitmap source that encapsulates either the video surface
//      directly, or it points to a system memory D3D surface that can act as
//      the bitmap source.
//
//------------------------------------------------------------------------------
__override
HRESULT
CHWMFMediaBuffer::
GetBitmapSource(
    __in            bool                syncChannel,
        // Whether this is a synchronous channel (normally a bitmap effect),
        // if this is the case, we can't create all of our composition objects.
        // (The cached bitmap requires a locked device).
    __in_opt        CD3DDeviceLevel1    *pDisplayDevice,
        // The display device or NULL if this is a software target. If this is a
        // software device, we copy to a system memory surface.
    __deref_out     IWGXBitmapSource    **ppIBitmapSource
        // The returned bitmap source for the data we have in the buffer. We work
        // out whether the cached surface in the bitmap is sufficient
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    //
    // The cached bitmap can only be created when guarding the D3D device and this
    // can only be done on the composition thread, not the UI thread.
    //
    if (!m_fTextureCachedOnBitmap && !syncChannel)
    {
        IFC(CacheTextureOnBitmap());
    }

    //
    // If we are being asked to render to a different display device than our existing
    // one, and, we have had our existing system memory invalidated.
    //
    if (pDisplayDevice != m_pRenderDevice && !m_systemMemoryValid)
    {
        IFC(CopyBitmap());
    }

    //
    // Return our bitmap.
    //
    SetInterface(*ppIBitmapSource, m_pBitmap);

Cleanup:

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CHWMFMediaBuffer::DoneWithBitmap
//
//  Synopsis:
//      Called by composition at the end of the entire composition pass to let us
//      know that it is done with the bitmap. This isn't interesting in hardware
//      where data comes from HW to system memory via GetRenderTargetData,
//      but, this is interesting in SW where we need to unlock the render target
//      to allow the EVR to do processing on it.
//
//------------------------------------------------------------------------------
__override
HRESULT
CHWMFMediaBuffer::
DoneWithBitmap(
    void
    )
{
    return S_OK;
}

//
// Protected methods
//
//+-----------------------------------------------------------------------------
//
//  Function:
//      CHWMFMediaBuffer::Init
//
//  Synopsis:
//      Initialize the hardware media buffer. This can be called on any thread,
//      so, it should not access the composition objects since these can't be
//      locked.
//
//------------------------------------------------------------------------------
__override
HRESULT
CHWMFMediaBuffer::
Init(
    void
    )
{
    HRESULT             hr = S_OK;
    IDirect3DDevice9    *pIMixerDevice = NULL;
    D3DSURFACE_DESC     ddsd;

    ZeroMemory(&ddsd, sizeof(ddsd));

    TRACEF(&hr);

    IFC(CreateMixerTexture());

    GetUnderlyingDevice(m_pMixerDevice, &pIMixerDevice);

    IFC(m_pIMixerTexture->GetSurfaceLevel(0, &m_pIMixerSurface));

    //
    // Colorfill to black.
    //
    IFC(pIMixerDevice->ColorFill(m_pIMixerSurface, NULL, D3DCOLOR_XRGB(0, 0, 0)));

    IFC(GetSurfaceDescription(D3DPOOL_SYSTEMMEM, &ddsd));

    //
    // Create the offscreen plain surface we will be using to capture the system
    // memory bitmap. This can be done on the mixer device. It must be since effects
    // require this to be present and don't call us on the composition thread.
    //
    IFC(pIMixerDevice->CreateOffscreenPlainSurface(
            ddsd.Width,
            ddsd.Height,
            ddsd.Format,
            ddsd.Pool,      // System memory to write to other devices if necessary
            &m_pIBitmapSurface,
            NULL));                 // No shared handle

    //
    // Create the client bitmap (no addref at this stage).
    //
    m_pBitmap = new CClientMemoryBitmap();

    IFCOOM(m_pBitmap);

    m_pBitmap->AddRef();

    //
    // We really want to do this because otherwise the client
    // memory bitmap won't be associated with a valid buffer.
    //
    IFC(CopyBitmap(false)); // Don't fetch the render target data
                            // just alias.

    //
    // Now, initialize the Mediabuffer on this surface.
    //
    IFC(CMFMediaBuffer::Init(m_pIMixerSurface));

Cleanup:

    ReleaseInterface(pIMixerDevice);

    EXPECT_SUCCESS(hr);

    if (hr != WGXERR_AV_NOMEDIATYPE)
    {
        hr = EvrPresenter::TreatNonSoftwareFallbackErrorAsUnknownHardwareError(hr);
    }

    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      CHWMFMediaBuffer::CreateMixerTexture
//
//  Synopsis:
//            This is called to create the texture used by the mixer for HW accelerated
//      /     media.
//
//------------------------------------------------------------------------------
HRESULT
CHWMFMediaBuffer::
CreateMixerTexture(
    void
    )
{
    HRESULT             hr = S_OK;
    IDirect3DDevice9    *pIMixerDevice = NULL;
    D3DSURFACE_DESC     ddsd;
    TRACEF(&hr);

    IFC(GetSurfaceDescription(D3DPOOL_DEFAULT, &ddsd));

    GetUnderlyingDevice(m_pMixerDevice, &pIMixerDevice);

    IFC(pIMixerDevice->CreateTexture(
            ddsd.Width,
            ddsd.Height,
            1,
            ddsd.Usage,
            ddsd.Format,
            ddsd.Pool,
            &m_pIMixerTexture,
            NULL));         // No shared handle to the texture.

Cleanup:

    ReleaseInterface(pIMixerDevice);

    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}


//
// Private methods
//
//+----------------------------------------------------------------------------
//
//  Function:
//      CHWMFMediaBuffer::CacheTextureOnBitmap
//
//  Synopsis:
//      Associates the hardware texture with the client memory bitmap. This can
//      only be called from the composition thread since the CD3DDeviceLevel1
//      isn't really lockable.
//
//-----------------------------------------------------------------------------
HRESULT
CHWMFMediaBuffer::
CacheTextureOnBitmap(
    void
    )
{
    HRESULT             hr = S_OK;

    TRACEF(&hr);

    Assert(!m_fTextureCachedOnBitmap);

    //
    // We create a lot of objects here, guard against this on the device.
    //
    {
        CGuard<CD3DDeviceLevel1>    guard(*m_pRenderDevice);

        //
        // If we don't have a mixture texture it would mean we are trying to
        // render a texture before the mixer has written to it. The sample queue
        // prevents this from happening.
        //
        Assert(m_pIMixerTexture);

        IFC(CacheHwTextureOnBitmap(
            m_pIMixerTexture,
            m_pBitmap,
            m_pRenderDevice
            ));

        m_fTextureCachedOnBitmap = true;
    }

Cleanup:

    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CHWMFMediaBuffer::GetSurfaceDescription
//
//  Synopsis:
//      Gets a surface description based on our current height and the pool.
//
//------------------------------------------------------------------------------
HRESULT
CHWMFMediaBuffer::
GetSurfaceDescription(
    __in    D3DPOOL             d3dPool,
        // Memory pool to use
    __out   D3DSURFACE_DESC     *pD3DSurfaceDesc
        // populated surface description.
    )
{
    HRESULT hr = S_OK;

    TRACEF(&hr);

    ZeroMemory(pD3DSurfaceDesc, sizeof(*pD3DSurfaceDesc));

    pD3DSurfaceDesc->Format = m_format;
    pD3DSurfaceDesc->Type = D3DRTYPE_TEXTURE;
    pD3DSurfaceDesc->Usage = D3DUSAGE_RENDERTARGET;
    pD3DSurfaceDesc->Pool = d3dPool;
    pD3DSurfaceDesc->Height = m_uiHeight;
    pD3DSurfaceDesc->Width = m_uiWidth;
    pD3DSurfaceDesc->MultiSampleType = D3DMULTISAMPLE_NONE;
    pD3DSurfaceDesc->MultiSampleQuality = 0;    // Multisample quality is irrelevant.

    hr = m_pRenderDevice->GetMinimalTextureDesc(pD3DSurfaceDesc, false, GMTD_CHECK_ALL | GMTD_NONPOW2CONDITIONAL_OK);

    if (hr == S_FALSE) // means we can't create a big enough texture
    {
        IFC(WGXERR_AV_REQUESTEDTEXTURETOOBIG);
    }
    else
    {
        IFC(hr);
    }

Cleanup:

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CHWMFMediaBuffer::CopyBitmap
//
//  Synopsis:
//      Copies the data from the surface render target stored in the cached
//      bitmap to the system memory surface stored in a lockable texture, locks
//      it and initializes a client memory bitmap with the surface which is then
//      aliased to our data.
//
//------------------------------------------------------------------------------
HRESULT
CHWMFMediaBuffer::
CopyBitmap(
    __in    bool                fetchData
        // Whether we should actually fetch the data to populate the surface
        // or whether we should just alias the un-initialized buffer.
    )
{
    HRESULT             hr = S_OK;
    IDirect3DDevice9    *pIMixerDevice = NULL;
    TRACEF(&hr);

    bool                surfaceLocked = false;
    D3DLOCKED_RECT      lockedRect = { 0 };

    if (m_surfaceLocked)
    {
        IFC(m_pIBitmapSurface->UnlockRect());
    }

    GetUnderlyingDevice(m_pMixerDevice, &pIMixerDevice);

    //
    // Now, copy the contents of the render target to the D3D surface.
    //
    if (fetchData)
    {
        IFC(pIMixerDevice->GetRenderTargetData(m_pIMixerSurface, m_pIBitmapSurface));
    }

    IFC(m_pIBitmapSurface->LockRect(
            &lockedRect,
            NULL,       // Just lock everything
            0));

    surfaceLocked = true;

    //
    // Bit of a cheat calling HrInit multiple times on the client bitmap,
    // but all it does is alias its memory to the locked texture.
    //
    IFC(
        m_pBitmap->HrInit(
            m_uiWidth,
            m_uiHeight,
            D3DFormatToPixelFormat(
                m_format,
                FALSE),
            lockedRect.Pitch * m_uiHeight,  // Buffer size
            lockedRect.pBits,
            lockedRect.Pitch));

    {
        IMILResourceCache::ValidIndex   deviceCacheIndex;

        //
        // Now, tell the bitmap resource manager that we want all the other copies
        // of the hardware surface invalidated. This is to handle two cases:
        // 1. HW->SW->HW for spanning.
        //    (Otherwise the other cached HW textures would be invalid).
        // 2. SW->Realized SW
        //    (this can happen with alpha channels due to the difference between
        //    PBGRA and BGRA)
        //
        IFC(m_pRenderDevice->GetCacheIndex(&deviceCacheIndex));

        IFC(m_pBitmap->ReleaseOtherResources(deviceCacheIndex));

        //
        // Because we are manipulating the resource cache on a different thread
        // in the case of RenderTargetBitmap (and therefore effects), we need
        // multi-threaded resource caches. This is a compile time assert to
        // ensure that the resource cache is truly multi-threaded.
        //
        C_ASSERT(!RESOURCE_CACHE_SINGLE_THREADED);
    }

    //
    // Everything succeeded, indicate that the surface has been locked.
    //
    m_surfaceLocked = true;
    m_systemMemoryValid = true;

Cleanup:

    ReleaseInterface(pIMixerDevice);

    if (FAILED(hr) && surfaceLocked)
    {
        IGNORE_HR(m_pIBitmapSurface->UnlockRect());
    }

    EXPECT_SUCCESS(hr);

    if (hr != WGXERR_AV_NOMEDIATYPE)
    {
        hr = EvrPresenter::TreatNonSoftwareFallbackErrorAsUnknownHardwareError(hr);
    }

    RRETURN(hr);
}


