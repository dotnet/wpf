// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Security ; 
using System.Runtime.InteropServices;

namespace MS.Win32
{
    // Specialized version of HwndWrapper for message-only windows.

    internal class MessageOnlyHwndWrapper : HwndWrapper
    {
        public MessageOnlyHwndWrapper() : base(0, 0, 0, 0, 0, 0, 0, "", NativeMethods.HWND_MESSAGE, null)
        {
        }
    }
}
