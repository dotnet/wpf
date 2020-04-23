// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
//***************************************************************************
// HOW TO USE THIS FILE
//
// If you need access to a Win32 API that is not exposed, simply uncomment
// it in one of the following files:
//
// NativeMethods.cs
// UnsafeNativeMethods.cs
// SafeNativeMethods.cs
//
// Only uncomment what you need to avoid code bloat.
//
// DO NOT adjust the visibility of anything in these files.  They are marked
// internal on pupose.
//***************************************************************************

namespace MS.Win32
{
    using System;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;

    internal static class SafeNativeMethods
    {
        [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern int GetMessageTime();

        [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = CharSet.Auto)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int x, int y, int cx, int cy, int flags);
    }
}

