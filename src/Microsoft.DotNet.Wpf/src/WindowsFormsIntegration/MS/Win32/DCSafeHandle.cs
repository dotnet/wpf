// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace MS.Win32
{
    internal sealed class DCSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private DCSafeHandle() : base(true) { }

        override protected bool ReleaseHandle()
        {
            return UnsafeNativeMethods.DeleteDC(handle);
        }
    }
}

