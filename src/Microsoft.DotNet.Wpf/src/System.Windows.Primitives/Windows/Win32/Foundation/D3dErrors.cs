// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Windows.Win32.System.Diagnostics.Debug;

namespace Windows.Win32.Foundation;

#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable format

internal static class D3dErrors
{
    // DirectX 9 specific defines aren't exposed in the Windows metadata (too old).
    // Manually defining all of the codes that actually get used in the WPF codebase.

    // https://learn.microsoft.com/windows/win32/direct3d9/d3derr

    // Copied from d3d9.h

    private const int _FACD3D = 0x876;

    internal const FACILITY_CODE FacilityCode = (FACILITY_CODE)_FACD3D;

    // MAKE_HRESULT definition:
    //
    // ((HRESULT) (((unsigned long)(sev)<<31) | ((unsigned long)(fac)<<16) | ((unsigned long)(code))) )
    private const int ErrorCode = unchecked((int)(((ulong)1 << 31) | ((ulong)(_FACD3D) << 16)));

    internal static readonly HRESULT D3DERR_DEVICEHUNG                 = (HRESULT)(ErrorCode | 2164);
    internal static readonly HRESULT D3DERR_DEVICELOST                 = (HRESULT)(ErrorCode | 2152);
    internal static readonly HRESULT D3DERR_DEVICEREMOVED              = (HRESULT)(ErrorCode | 2160);
    internal static readonly HRESULT D3DERR_DRIVERINTERNALERROR        = (HRESULT)(ErrorCode | 2087);
    internal static readonly HRESULT D3DERR_INVALIDCALL                = (HRESULT)(ErrorCode | 2156);
    internal static readonly HRESULT D3DERR_NOTAVAILABLE               = (HRESULT)(ErrorCode | 2154);
    internal static readonly HRESULT D3DERR_OUTOFVIDEOMEMORY           = (HRESULT)(ErrorCode | 380);
    internal static readonly HRESULT D3DERR_WRONGTEXTUREFORMAT         = (HRESULT)(ErrorCode | 2072);
}

#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore format
