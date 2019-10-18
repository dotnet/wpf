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
#include "precomp.hpp"
#include "swmediabuffer.tmh"

MtDefine(CSWMFMediaBuffer, Mem, "CSWMFMediaBuffer");

CSWMFMediaBuffer::
CSWMFMediaBuffer(
    __in    UINT             uiID,
    __in    LONG             continuity,
    __in    UINT             uiWidth,
    __in    UINT             uiHeight,
    __in    D3DFORMAT        format,
    __in    CD3DDeviceLevel1 *pRenderDevice
    ) : CMFMediaBuffer(
            uiID,
            continuity,
            uiWidth,
            uiHeight,
            format,
            pRenderDevice),
        m_pIBitmapSurface(NULL),
        m_pBitmap(NULL)
{
}

__override
CSWMFMediaBuffer::
~CSWMFMediaBuffer(
    void
    )
{
    //
    // In the software case, we should always have returned our surface at the
    // end of a composition pass.
    //
    Assert(!m_systemMemoryValid);

    ReleaseInterface(m_pIBitmapSurface);
    ReleaseInterface(m_pBitmap);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      CMFMediaBuffer::GetBitmapSource
//
//  Synopsis:
//      Returns a bitmap source that encapsulates either the video surface
//      directly, or it points to a system memory D3D surface that can act as
//      the bitmap source.
//
//------------------------------------------------------------------------------
__override
HRESULT
CSWMFMediaBuffer::
GetBitmapSource(
    __in            bool                syncChannel,
        // Whether the channel is synchronous (not relevant for SW case).
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
    // IF we don't have a bitmap (we won't have one the first time the call comes
    // through), create it.
    //
    if (!m_pBitmap)
    {
        //
        // This is always called from BeginRender and hence on the composition thread.
        //
        IFC(CreateCompositionObjects());
    }

    //
    // If we are dirty, we need to get the client bitmap alaised to the surface
    // render target.
    //
    if (!m_systemMemoryValid)
    {
        IFC(AliasBitmap(m_pBitmap,
                        /*initializing =*/ false));
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
//      CSWMFMediaBuffer::DoneWithBitmap
//
//  Synopsis:
//      This is called by composition when it is done with the bitmap. We unlock
//      our surface so that the EVR can write into it.
//
//------------------------------------------------------------------------------
__override
HRESULT
CSWMFMediaBuffer::
DoneWithBitmap(
    void
    )
{
    HRESULT     hr = S_OK;

    TRACEF(&hr);

    if (m_systemMemoryValid)
    {
        IFC(m_pIBitmapSurface->UnlockRect());

        m_systemMemoryValid = false;
    }

Cleanup:

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//
// Protected methods
//
//+-----------------------------------------------------------------------------
//
//  Function:
//      CSWMFMediaBuffer::Init
//
//  Synopsis:
//      This is called on instantiation, we can't access any composition objects
//      because we are off on a new thread.
//
//------------------------------------------------------------------------------
__override
HRESULT
CSWMFMediaBuffer::
Init(
    void
    )
{
    HRESULT hr = S_OK;
    IDirect3DDevice9 *pIDevice = NULL;

    TRACEF(&hr);

    GetUnderlyingDevice(m_pRenderDevice, &pIDevice);

    IFC(pIDevice->CreateRenderTarget(
            m_uiWidth,
            m_uiHeight,
            PixelFormatToD3DFormat(MilPixelFormat::BGR32bpp),
            D3DMULTISAMPLE_NONE,
            0,                      // Multisample quality is irrelevant
            TRUE,                   // This surface must be lockable
            &m_pIBitmapSurface,
            NULL));                 // No shared handle required for the render target.

    //
    // Especially in this case, need to pass this up to the CMFMediaBuffer to allow
    // this to be lockable.
    //
    IFC(CMFMediaBuffer::Init(m_pIBitmapSurface));

Cleanup:
    ReleaseInterface(pIDevice);

    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}


//
// Private methods
//
//+-----------------------------------------------------------------------------
//
//  Function:
//      CMFMediaBuffer::CreateCompositionObjects
//
//  Synopsis:
//      Creates the objects that are useful to pass back to composition if we are
//      doing software processing. In this case, we just use a system memory surface
//      directly aliased as the bitmap.
//
//------------------------------------------------------------------------------
HRESULT
CSWMFMediaBuffer::
CreateCompositionObjects(
    void
    )
{
    HRESULT hr = S_OK;

    TRACEF(&hr);

    Assert(m_pBitmap == NULL);

    CClientMemoryBitmap         *pClientBitmap = NULL;

    //
    // Lock the D3D device while we are creating the associated composition objects.
    //
    CGuard<CD3DDeviceLevel1>    guard(*m_pRenderDevice);

    //
    // Create the client bitmap (no addref at this stage).
    //
    pClientBitmap = new CClientMemoryBitmap();

    IFCOOM(pClientBitmap);

    pClientBitmap->AddRef();

    IFC(AliasBitmap(pClientBitmap,
                    /* initializing = */ true));

    //
    // Assign the created objects over at the end.
    //
    m_pBitmap = pClientBitmap;
    pClientBitmap = NULL;

Cleanup:

    ReleaseInterface(pClientBitmap);

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CMFMediaBuffer::AliasBitmap
//
//  Synopsis:
//      Alias the given render target with the client bitmap.
//
//------------------------------------------------------------------------------
HRESULT
CSWMFMediaBuffer::
AliasBitmap(
    __in    CClientMemoryBitmap         *pClientBitmap,
    __in    bool                        initializing
    )
{
    HRESULT     hr = S_OK;

    bool            surfaceLocked = false;
    D3DLOCKED_RECT  lockedRect = { 0 };

    //
    // This first time we create the surface, system memory is not valid
    // but the surface isn't locked. This isn't true at any other point.
    //
    if (!m_systemMemoryValid && !initializing)
    {
        m_pIBitmapSurface->UnlockRect();
    }

    //
    // Lock the corresponding lockable texture
    //
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
        pClientBitmap->HrInit(
            m_uiWidth,
            m_uiHeight,
            MilPixelFormat::BGR32bpp,
            lockedRect.Pitch * m_uiHeight,  // Buffer size
            lockedRect.pBits,
            lockedRect.Pitch));

    //
    // In case we are doing SW video processing, but still rendering to
    // hardware, we need to make sure that the cached resources associated
    // with the bitmap are invalidated.
    //
    IFC(pClientBitmap->ReleaseResources());

    //
    // This marks that the bitmap is not valid.
    //
    m_systemMemoryValid = true;

Cleanup:

    if (FAILED(hr) && surfaceLocked)
    {
        IGNORE_HR(m_pIBitmapSurface->UnlockRect());
    }

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}



