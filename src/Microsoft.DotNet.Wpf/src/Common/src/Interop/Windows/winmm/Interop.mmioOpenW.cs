// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class WinMM
    {
        [DllImport(Libraries.WinMM, ExactSpelling = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr mmioOpenW(string fileName, IntPtr lpmmioinfo, int flags);
    }
}
