// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

MtExtern(CHybridSurfaceRenderTarget);

//------------------------------------------------------------------------------
//
//  Class: CHybridSurfaceRenderTarget
//
//  Description:
//      This object creates the hybrid render target.
//      Which means it automatically creates HW or SW based on MilRTInitialization::Flags
//      and fallback to SW if HW is not available.
//
//------------------------------------------------------------------------------
class CHybridSurfaceRenderTarget :
    public CMILCOMBase,
    public CHwSurfaceRenderTarget
{
public:

    static HRESULT Create(
        __in_ecount(1) CDisplaySet const *pDisplaySet,
        MilRTInitialization::Flags dwFlags,
        FLOAT dpiX, FLOAT dpiY,
        __deref_out_ecount(1) CHybridSurfaceRenderTarget **ppRenderTarget
        );

    //
    // IUnknown methods
    //

    DECLARE_COM_BASE;
    STDMETHOD(HrFindInterface)(
        __in_ecount(1) REFIID riid,
        __deref_out void** ppv
        );

private:

    CHybridSurfaceRenderTarget(
        __inout_ecount(1) CD3DDeviceLevel1 *pD3DDevice,
        __in_ecount(1) D3DPRESENT_PARAMETERS const &D3DPresentParams,
        DisplayId associatedDisplay,
        FLOAT dpiX, FLOAT dpiY
        );

protected:

    //
    // CHybridSurfaceRenderTarget methods
    //

    //+------------------------------------------------------------------------
    //
    //  Member:    IsValid
    //
    //  Synopsis:  Returns FALSE when rendering with this render target or any
    //             use is no longer allowed.  Mode change is a common cause of
    //             of invalidation.
    //
    //-------------------------------------------------------------------------
    bool IsValid() const;

#if DBG_STEP_RENDERING
public:
    void ShowSteppedRendering(
        __in LPCTSTR pszRenderDesc,
        __in_ecount(1) const ISteppedRenderingSurfaceRT *pRT
        );
#endif DBG_STEP_RENDERING
};
