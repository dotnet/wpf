// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;

namespace System.Windows.Input 
{
    /// <summary>
    ///    The raw actions being reported from the mouse.
    /// </summary>
    /// <remarks>
    ///     Note that multiple actions can be reported at once.
    /// </remarks>
    [Flags]
    internal enum RawMouseActions
    {
        /// <summary>
        ///     No mouse actions.
        /// </summary>
        None = 0x0,

        /// <summary>
        ///     The mouse attributes have changed.  The application needs to
        ///     query the mouse attributes.
        /// </summary>
        AttributesChanged = 0x1,

        /// <summary>
        ///     The mouse became active in the application.  The application
        ///     may need to refresh its mouse state.
        /// </summary>
        Activate = 0x2,
            
        /// <summary>
        ///     The mouse became inactive in the application.  The application
        ///     may need to clear its mouse state.
        /// </summary>
        Deactivate = 0x4,
            
        /// <summary>
        ///     The mouse moved, and the position is reported relative to
        ///     the last reported position.
        /// </summary>
        RelativeMove = 0x8,
            
        /// <summary>
        ///     The mouse moved, and the position is reported in absolute
        ///     coordinates.
        /// </summary>
        AbsoluteMove = 0x10,
            
        /// <summary>
        ///     The mouse moved, and the position is reported in coordinates
        ///     relative to the virtual desktop.
        /// </summary>
        VirtualDesktopMove = 0x20,
            
        /// <summary>
        ///     The first button was pressed.
        /// </summary>
        Button1Press = 0x40,
            
        /// <summary>
        ///     The first button was released.
        /// </summary>
        Button1Release = 0x80,
            
        /// <summary>
        ///     The second button was pressed.
        /// </summary>
        Button2Press = 0x100,
            
        /// <summary>
        ///     The second button was released.
        /// </summary>
        Button2Release = 0x200,
            
        /// <summary>
        ///     The third button was pressed.
        /// </summary>
        Button3Press = 0x400,
            
        /// <summary>
        ///     The third button was released.
        /// </summary>
        Button3Release = 0x800,
            
        /// <summary>
        ///     The fourth button was pressed.
        /// </summary>
        Button4Press = 0x1000,
            
        /// <summary>
        ///     The fourth button was released.
        /// </summary>
        Button4Release = 0x2000,
            
        /// <summary>
        ///     The fifth button was pressed.
        /// </summary>
        Button5Press = 0x4000,
            
        /// <summary>
        ///     The fifth button was released.
        /// </summary>
        Button5Release = 0x8000,
            
        /// <summary>
        ///     The vertical wheel was roteated.
        /// </summary>
        VerticalWheelRotate = 0x10000,
            
        /// <summary>
        ///     The horizontal wheel was roteated.
        /// </summary>
        HorizontalWheelRotate = 0x20000,

        /// <summary>
        ///     The mouse cursor was queried.
        /// </summary>
        QueryCursor = 0x40000,

        /// <summary>
        ///     The mouse capture was lost.
        /// </summary>
        CancelCapture = 0x80000

        // update the IsValid method in RawMouseInputReport when this enum is changed
    }
}
