// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

// This Source Code is partially based on reverse engineering of the Windows Operating System,
// and is intended for use on Windows systems only.
// This Source Code is partially based on the source code provided by the .NET Foundation.

using System.Runtime.InteropServices;

namespace System.Windows.Interop.WinDef;

/// <summary>
/// The POINT structure defines the x- and y-coordinates of a point.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
// ReSharper disable InconsistentNaming
public struct POINT
{
    /// <summary>
    /// Specifies the x-coordinate of the point.
    /// </summary>
    public int x;

    /// <summary>
    /// Specifies the y-coordinate of the point.
    /// </summary>
    public int y;
}
