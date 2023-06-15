// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Windows.Win32.Foundation;

internal static unsafe class IID
{
    private static ref readonly Guid IID_NULL
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = new byte[]
            {
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };

            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    // We cast away the "readonly" here as there is no way to communicate that through a pointer and
    // Marshal APIs take the Guid as ref. Even though none of our usages actually change the state.

    /// <summary>
    ///  Gets a pointer to the IID <see cref="Guid"/> for the given <typeparamref name="T"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid* Get<T>() where T : unmanaged, IComIID
        => (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in T.Guid));

    /// <summary>
    ///  Gets a reference to the IID <see cref="Guid"/> for the given <typeparamref name="T"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref Guid GetRef<T>() where T : unmanaged, IComIID
        => ref Unsafe.AsRef(in T.Guid);

    /// <summary>
    ///  Empty <see cref="Guid"/> (GUID_NULL in docs).
    /// </summary>
    public static Guid* NULL() => (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in IID_NULL));
}
