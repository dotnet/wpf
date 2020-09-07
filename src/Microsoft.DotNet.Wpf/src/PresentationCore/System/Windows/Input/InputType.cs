// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;

namespace System.Windows.Input 
{
    /// <summary>
    ///     The type of input being reported.
    /// </summary>
    /// <remarks>
    ///     The operating system handles keyboard, mouse, and pen input
    ///     specially.
    ///     <para/>
    ///     HID indicates that the input was provided by a Human Interface
    ///     Device that was not a keyboard, a mouse, or a stylus.
    /// </remarks>
    public enum InputType
    {
        /// <remarks>
        ///     Input from a keyboard.
        /// </remarks>
        Keyboard,

        /// <remarks>
        ///     Input from a mouse.
        /// </remarks>
        Mouse,
        
        /// <remarks>
        ///     Input from a stylus.
        /// </remarks>
        Stylus,

        /// <remarks>
        ///     Input from a HID device that is not a keyboard, a mouse, or a
        ///     stylus.
        /// </remarks>
        Hid,

        /// <remarks>
        ///     Direct Text Input.
        /// </remarks>
        Text,

        /// <remarks>
        ///     Input from WM_APPCOMMAND.
        /// </remarks>
        Command
    }
}
