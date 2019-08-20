// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Windows;
using System.Security;
using MS.Internal;
using MS.Internal.PresentationCore;                        // SecurityHelper
using System.Windows.Media;
using MS.Win32; // VK translation.

using System;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input
{
    /// <summary>
    ///     The Win32KeyboardDevice class implements the platform specific
    ///     KeyboardDevice features for the Win32 platform
    /// </summary>
    internal sealed class Win32KeyboardDevice : KeyboardDevice
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputManager">
        /// </param>
        internal Win32KeyboardDevice(InputManager inputManager)
            : base(inputManager)
        {
        }

        /// <summary>
        ///     Gets the current state of the specified key from the device from the underlying system
        /// </summary>
        /// <param name="key">
        ///     Key to get the state of
        /// </param>
        /// <returns>                           
        ///     The state of the specified key
        /// </returns>
        protected override KeyStates GetKeyStatesFromSystem(Key key)
        {
            KeyStates keyStates = KeyStates.None;

            int virtualKeyCode = KeyInterop.VirtualKeyFromKey(key);
            int nativeKeyState = UnsafeNativeMethods.GetKeyState(virtualKeyCode);

            if ((nativeKeyState & 0x00008000) == 0x00008000)
                keyStates |= KeyStates.Down;

            if ((nativeKeyState & 0x00000001) == 0x00000001)
                keyStates |= KeyStates.Toggled;

            return keyStates;
        }
    }
}

