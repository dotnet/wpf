// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using MS.Internal.PresentationCore;

namespace System.Windows.Input
{
    /// <summary>
    ///     The MouseButton enumeration describes the buttons available on
    ///     the mouse device.
    /// </summary>
    /// <remarks>
    ///     You must update MouseButtonUtilities.Validate if any changes are made to this type
    /// </remarks>
    public enum MouseButton
    {
        /// <summary>
        ///    The left mouse button.
        /// </summary>
        Left,
        
        /// <summary>
        ///    The middle mouse button.
        /// </summary>
        Middle,

        /// <summary>
        ///    The right mouse button.
        /// </summary>
        Right,
        
        /// <summary>
        ///    The fourth mouse button.
        /// </summary>
        XButton1,

        /// <summary>
        ///    The fifth mouse button.
        /// </summary>
        XButton2
    }

    /// <summary>
    ///     Utility class for MouseButton
    /// </summary>
    internal sealed class MouseButtonUtilities
    {
        /// <summary>
        ///     Private placeholder constructor
        /// </summary>
        /// <remarks>
        ///     There is present to supress the autogeneration of a public one, which
        ///     triggers an FxCop violation, as this is an internal class that is never instantiated
        /// </remarks>
        private MouseButtonUtilities()
        {
        }
        
        /// <summary>
        ///     Ensures MouseButton is set to a valid value.
        /// </summary>
        /// <remarks>
        ///     There is a proscription against using Enum.IsDefined().  (it is slow)
        ///     So we manually validate using a switch statement.
        /// </remarks>
        [FriendAccessAllowed]
        internal static void Validate(MouseButton button)
        {
            switch(button)
            {
                case MouseButton.Left:
                case MouseButton.Middle:
                case MouseButton.Right:
                case MouseButton.XButton1:
                case MouseButton.XButton2:
                    break;
                default:
                    throw new  System.ComponentModel.InvalidEnumArgumentException("button", (int)button, typeof(MouseButton));
            }
        }
}
}

