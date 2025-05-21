// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Internal.Interop;
using System.Windows.Input;

namespace System.Windows.Interop
{
    /// <summary>
    /// Implements an input provider per hwnd for WM_POINTER messages
    /// </summary>
    internal interface IStylusInputProvider : IInputProvider, IDisposable
    {
        #region Message Filtering

        /// <summary>
        /// Handles windows messages
        /// </summary>
        IntPtr FilterMessage(IntPtr hwnd, WindowMessage msg, IntPtr wParam, IntPtr lParam, ref bool handled);

        #endregion
    }
}
