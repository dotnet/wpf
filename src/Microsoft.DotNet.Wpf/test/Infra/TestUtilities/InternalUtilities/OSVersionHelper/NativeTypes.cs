// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace Microsoft.Internal.OSVersionHelper.NativeTypes
{
    /// <summary>
    /// P/Invoke compatible managed representation of the Win32 OSVERSIONINFOEX structure 
    /// </summary>
    /// <remarks>
    /// Also see documentation for <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms724833(v=vs.85).aspx">OSVERSIONINFOEX </a> structure.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    [BestFitMapping(BestFitMapping: false, ThrowOnUnmappableChar = true)]
    internal class OSVERSIONINFOEX
    {
        private uint dwOSVersionInfoSize;
        internal uint dwMajorVersion;
        internal uint dwMinorVersion;
        internal uint dwBuildNumber;
        internal uint dwPlatformId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        internal string szCSDVersion;
        internal ushort wServicePackMajor;
        internal ushort wServicePackMinor;
        internal ushort wSuiteMask;
        internal byte wProductType;
        private byte wReserved;

        internal OSVERSIONINFOEX()
        {
            dwOSVersionInfoSize = (uint)Marshal.SizeOf(typeof(OSVERSIONINFOEX));
            dwMajorVersion      = 0;
            dwMinorVersion      = 0;
            dwBuildNumber       = 0;
            dwPlatformId        = 0;
            szCSDVersion        = string.Empty;
            wServicePackMajor   = 0;
            wServicePackMinor   = 0;
            wSuiteMask          = 0;
            wProductType        = 0;
            wReserved           = 0;
        }
    }
}
