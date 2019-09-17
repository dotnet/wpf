// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿
using MS.Internal;
using MS.Internal.Interop;
using MS.Internal.PresentationCore;
using MS.Win32.Pointer;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Input.StylusPointer;
using System.Windows.Media;
using System.Windows.Threading;

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
