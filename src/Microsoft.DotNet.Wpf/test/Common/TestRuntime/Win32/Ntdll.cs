// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace Microsoft.Test.Win32
{
    public static class NtDll
    {
        private const string NTDLLDLL = "ntdll.dll";

        [CLSCompliant(false)]
        [DllImport(NTDLLDLL)]
        [SecuritySafeCritical]
        [SuppressUnmanagedCodeSecurity]
        [SecurityPermission(SecurityAction.Assert, UnmanagedCode = true)]
        public static extern uint RtlVerifyVersionInfo(ref NativeStructs.RTL_OSVERSIONINFOEXW osvi, uint typeMask, ulong conditionMask);
       
    }
}
