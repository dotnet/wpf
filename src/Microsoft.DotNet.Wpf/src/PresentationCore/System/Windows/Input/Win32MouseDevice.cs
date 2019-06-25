// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Collections;
using System.Windows;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Security;
using MS.Internal;
using MS.Internal.PresentationCore;                        // SecurityHelper
using MS.Win32; // *NativeMethods
using System.Runtime.InteropServices;
using System;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input
{
    /// <summary>
    ///     The Win32MouseDevice class implements the platform specific
    ///     MouseDevice features for the Win32 platform
    /// </summary>
    internal sealed class Win32MouseDevice : MouseDevice
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="inputManager">
        /// </param>
        internal Win32MouseDevice(InputManager inputManager)
            : base(inputManager)
        {
        }

        /// <summary>
        ///     Gets the current state of the specified button from the device from the underlying system
        /// </summary>
        /// <param name="mouseButton">
        ///     The mouse button to get the state of
        /// </param>
        /// <returns>
        ///     The state of the specified mouse button
        /// </returns>
        internal override MouseButtonState GetButtonStateFromSystem(MouseButton mouseButton)
        {
            MouseButtonState mouseButtonState = MouseButtonState.Released;

            // Security Mitigation: do not give out input state if the device is not active.
            if(IsActive)
            {
                int virtualKeyCode = 0;

                switch( mouseButton )
                {
                    case MouseButton.Left:
                        virtualKeyCode = NativeMethods.VK_LBUTTON;
                        break;
                    case MouseButton.Right:
                        virtualKeyCode = NativeMethods.VK_RBUTTON;
                        break;
                    case MouseButton.Middle:
                        virtualKeyCode = NativeMethods.VK_MBUTTON;
                        break;
                    case MouseButton.XButton1:
                        virtualKeyCode = NativeMethods.VK_XBUTTON1;
                        break;
                    case MouseButton.XButton2:
                        virtualKeyCode = NativeMethods.VK_XBUTTON2;
                        break;
                }

                mouseButtonState = ( UnsafeNativeMethods.GetKeyState(virtualKeyCode) & 0x8000 ) != 0 ? MouseButtonState.Pressed : MouseButtonState.Released;
            }

            return mouseButtonState;
        }
    }
}
