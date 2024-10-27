// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace MS.Win32
{
    internal delegate IntPtr HwndWrapperHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled);
}
