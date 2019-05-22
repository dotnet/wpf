// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;

namespace System.Windows.Input 
{
    /// <summary>
    ///    The raw actions being reported from the keyboard.
    /// </summary>
    /// <remarks>
    ///     Note that multiple actions can be reported at once.
    /// </remarks>
    [Flags]
    internal enum RawKeyboardActions
    {
        /// <summary>
        ///     No keyboard actions.
        /// </summary>
        None = 0x0,

        /// <summary>
        ///     The keyboard attributes have changed.  The application needs to
        ///     query the keyboard attributes.
        /// </summary>
        AttributesChanged = 0x1,

        /// <summary>
        ///     The keyboard became active in the application.  The application
        ///     may need to refresh its keyboard state.
        /// </summary>
        Activate = 0x2,
            
        /// <summary>
        ///     The keyboard became inactive in the application.  The application
        ///     may need to clear its keyboard state.
        /// </summary>
        Deactivate = 0x4,
            
        /// <summary>
        ///     A key was pressed.
        /// </summary>
        KeyDown = 0x8,
            
        /// <summary>
        ///     A key was released.
        /// </summary>
        KeyUp = 0x10,

        // Change the IsValid helper in RawKeyboardInputReport.cs  when this enum changes.
    }
}

