// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace WinRT
{
    static class Context
    {
        [DllImport("api-ms-win-core-com-l1-1-0.dll")]
        private static extern int CoGetObjectContext(ref Guid riid, out IntPtr ppv);

        public static IntPtr GetContextCallback()
        {
            Guid riid = typeof(IContextCallback).GUID;
            Marshal.ThrowExceptionForHR(CoGetObjectContext(ref riid, out IntPtr contextCallbackPtr));
            return contextCallbackPtr;
        }
    }
}
