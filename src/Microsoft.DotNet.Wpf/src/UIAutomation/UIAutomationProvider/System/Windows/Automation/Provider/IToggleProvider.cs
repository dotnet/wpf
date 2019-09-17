// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Toggle pattern provider interface

using System;
using System.Windows.Automation;
using System.Runtime.InteropServices;

namespace System.Windows.Automation.Provider
{
    /// <summary>
    /// public interface that represents UI elements that have a multistate (toggleable) value.
    /// </summary>
    [ComVisible(true)]
    [Guid("56d00bd0-c4f4-433c-a836-1a52a57e0892")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal interface IToggleProvider
#else
    public interface IToggleProvider
#endif
    {
        /// <summary>
        /// Request to change the state that this UI element is currently representing
        /// </summary>
        void Toggle( );


        /// <summary>Value of a toggleable control, as a ToggleState enum</summary>
        ToggleState ToggleState
        {
            get;
        }

    }
}
