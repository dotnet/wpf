// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security;

using MS.Internal.PresentationFramework;

namespace MS.Internal.Printing
{
    internal static class UnsafeNativeMethods
    {
        [DllImport("comdlg32.dll", CharSet = CharSet.Auto)]
        internal
        static
        extern
        Int32
        PrintDlgEx(
            IntPtr pdex
            );

        [DllImport("kernel32.dll")]
        internal
        static
        extern
        IntPtr
        GlobalFree(
            IntPtr hMem
            );

        [DllImport("kernel32.dll")]
        internal
        static
        extern
        IntPtr
        GlobalLock(
            IntPtr hMem
            );

        [DllImport("kernel32.dll")]
        internal
        static
        extern
        bool
        GlobalUnlock(
            IntPtr hMem
            );
    }
}
