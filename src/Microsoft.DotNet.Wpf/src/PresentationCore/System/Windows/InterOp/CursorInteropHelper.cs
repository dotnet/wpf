// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Windows.Input;

namespace System.Windows.Interop;

/// <summary>
/// Implements Avalon CursorInteropHelper classes, which helps
/// interop b/w legacy Cursor handles and Avalon Cursor objects.
/// </summary>
public static class CursorInteropHelper
{
    /// <summary>
    /// Creates a Cursor from a SafeHandle to a native Win32 Cursor.
    /// </summary>
    /// <param name="cursorHandle">SafeHandle to a native Win32 cursor.</param>
    public static Cursor Create(SafeHandle cursorHandle)
    {
        return new Cursor(cursorHandle);
    }
}

