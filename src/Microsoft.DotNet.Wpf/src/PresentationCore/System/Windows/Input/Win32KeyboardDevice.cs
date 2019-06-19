// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Windows;
using System.Security;
using System.Security.Permissions;
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

            bool getKeyStatesFromSystem = false;
            if(IsActive)
            {
                // Our keyboard device is only active if some WPF window in
                // this AppDomain has focus.  It is always safe to return
                // the state of keys.
                getKeyStatesFromSystem = true;
            }
            else if (SecurityHelper.AppDomainGrantedUnrestrictedUIPermission)
            {
                // This is a trusted AppDomain, so we are willing to expose
                // the state of keys regardless of whether or not a WPF
                // window has focus.  This is important for child HWND
                // hosting scenarios.
                getKeyStatesFromSystem = true;
            }
            else
            {
                // Security Mitigation: 
                // No WPF window has focus in this AppDomain, and this is a
                // partially-trusted AppDomain, so we do not generally want
                // to expose the state of keys.  However, we make an exception
                // for modifier keys, as they are considered safe.
                switch (key)
                {
                    case Key.LeftAlt:
                    case Key.RightAlt:
                    case Key.LeftCtrl:
                    case Key.RightCtrl:
                    case Key.LeftShift:
                    case Key.RightShift:
                        getKeyStatesFromSystem = true;
                        break;
                }
            }

            if (getKeyStatesFromSystem)
            {
                int virtualKeyCode = KeyInterop.VirtualKeyFromKey(key);
                int nativeKeyState;

                nativeKeyState = UnsafeNativeMethods.GetKeyState(virtualKeyCode);

                if( (nativeKeyState & 0x00008000) == 0x00008000 )
                    keyStates |= KeyStates.Down;

                if( (nativeKeyState & 0x00000001) == 0x00000001 )
                    keyStates |= KeyStates.Toggled;
            }

            return keyStates;
        }
    }
}

