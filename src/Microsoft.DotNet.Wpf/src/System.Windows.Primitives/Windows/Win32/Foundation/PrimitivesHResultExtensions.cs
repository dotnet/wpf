// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.System.Diagnostics.Debug;

namespace Windows.Win32.Foundation;

internal static class PrimitivesHResultExtensions
{
    // Can't find these defined or documented anywhere
    private const int COMPONENT_MASK = 0b_11100000_00000000;
    private const int COMPONENT_WINCODEC_ERROR = 0b_00100000_00000000;

    /// <summary>
    ///  <see langword="true"/> if the HRESULT represents a Windows Imaging Component (WIC) error.
    /// </summary>
    internal static bool IsWindowsCodecError(this HRESULT result) =>
        result.Facility == FACILITY_CODE.FACILITY_WINCODEC_DWRITE_DWM
            && ((result.Value & COMPONENT_MASK) == COMPONENT_WINCODEC_ERROR);

    /// <summary>
    ///  <see langword="true"/> if the <see cref="HRESULT"/> represents an <see cref="NTSTATUS"/> code.
    /// </summary>
    internal static bool IsNtStatus(this HRESULT result) =>
        (result.Value & (int)FACILITY_CODE.FACILITY_NT_BIT) == (int)FACILITY_CODE.FACILITY_NT_BIT;

    /// <summary>
    ///  Extracts the <see cref="NTSTATUS"/> code. Check <see cref="IsNtStatus"/> before calling this method.
    /// </summary>
    internal static NTSTATUS ToNtStatus(this HRESULT result)
    {
        Debug.Assert(result.IsNtStatus());
        return new NTSTATUS(result.Value & ~(int)FACILITY_CODE.FACILITY_NT_BIT);
    }

    /// <summary>
    ///  Throws an exception if the <see cref="HRESULT"/> represents a failure. If the error would normally result
    ///  in a <see cref="COMException"/> and the result is a Win32 error, a <see cref="Win32Exception"/> is thrown
    ///  instead.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static HRESULT ThrowOnFailureUnwrapWin32(this HRESULT result)
    {
        if (result.Failed)
        {
            Throw(result);
        }

        return result;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Throw(HRESULT result)
        {
            throw result.GetExceptionUnwrapWin32()
                ?? new InvalidOperationException() { HResult = result };
        }
    }

    /// <summary>
    ///  Gets an exception if the <see cref="HRESULT"/> represents a failure. If the error would normally result
    ///  in a <see cref="COMException"/> and the result is a Win32 error, a <see cref="Win32Exception"/> is thrown
    ///  instead.
    /// </summary>
    /// <returns>
    ///  The exception, or <see langword="null"/> if the result is a success code.
    /// </returns>
    internal static Exception? GetExceptionUnwrapWin32(this HRESULT result)
    {
        if (result.Succeeded)
        {
            return null;
        }

        // Pasing in -1 prevents thread IErrorInfo from trumping what we're trying to raise.
        Exception? e = Marshal.GetExceptionForHR(result, -1);
        Debug.Assert(e is not null);

        return e is COMException && result.Facility == FACILITY_CODE.FACILITY_WIN32
            ? new Win32Exception(result.Code)
            : e;
    }
}
