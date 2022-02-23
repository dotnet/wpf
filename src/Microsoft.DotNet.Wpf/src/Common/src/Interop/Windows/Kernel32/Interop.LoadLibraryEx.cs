// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        public const int LOAD_LIBRARY_SEARCH_SYSTEM32 = 0x00000800;

        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        internal static extern IntPtr LoadLibraryExW(string lpwLibFileName, IntPtr hFile, uint dwFlags);
    }
}
