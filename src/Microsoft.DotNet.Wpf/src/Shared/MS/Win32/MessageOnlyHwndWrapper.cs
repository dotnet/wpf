// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
