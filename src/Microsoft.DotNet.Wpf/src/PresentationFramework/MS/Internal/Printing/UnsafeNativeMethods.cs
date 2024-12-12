// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

using MS.Internal.PresentationFramework;

namespace MS.Internal.Printing
{
    internal static class UnsafeNativeMethods
    {
        [DllImport("comdlg32.dll", CharSet = CharSet.Auto)]
        internal static extern HRESULT PrintDlgEx(IntPtr pdex);

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
