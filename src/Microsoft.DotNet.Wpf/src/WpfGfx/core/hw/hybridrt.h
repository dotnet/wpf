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
class CHybridSurfaceRenderTarget
{
public:
    static HRESULT CHybridSurfaceRenderTarget::CreateRenderTargetBitmap(
        __in_ecount_opt(1) CDisplaySet const *pDisplaySet,
        MilRTInitialization::Flags dwFlags,
        UINT width,
        UINT height,
        MilPixelFormat::Enum format,
        FLOAT dpiX, 
        FLOAT dpiY,
        IntermediateRTUsage usageInfo,
        __deref_out_ecount(1) IMILRenderTargetBitmap **ppIRenderTargetBitmap
    );
};
