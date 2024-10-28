// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MS.Win32
{
    /// <summary>
    /// Specialized version of <see cref="HwndWrapper"/> for message-only windows.
    /// </summary>
    internal sealed class MessageOnlyHwndWrapper : HwndWrapper
    {
        public MessageOnlyHwndWrapper() : base(0, 0, 0, 0, 0, 0, 0, string.Empty, NativeMethods.HWND_MESSAGE) { }

    }
}
