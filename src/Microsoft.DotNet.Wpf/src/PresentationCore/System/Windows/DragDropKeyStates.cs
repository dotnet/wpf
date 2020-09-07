// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// 
// Description: Specifies the key and mouse states for drag-and-drop operation.
// 
//

using System;

namespace System.Windows
{
    /// <summary>
    /// An enumeration of states of keyboard and mouse.
    /// </summary>
    [Flags]
    public enum DragDropKeyStates
    {
        /// <summary>
        /// No state set.
        /// </summary>
        None = 0,
        /// <summary>
        /// The left mouse button.  
        /// </summary>
        LeftMouseButton = 1,
        /// <summary>
        /// The right mouse button.   
        /// </summary>
        RightMouseButton = 2,
        /// <summary>
        /// The SHIFT key.   
        /// </summary>
        ShiftKey = 4,
        /// <summary>
        /// The CTRL key.
        /// </summary>
        ControlKey = 8,
        /// <summary>
        /// The middle mouse button.
        /// </summary>
        MiddleMouseButton = 16,
        /// <summary>
        /// The ALT key.   
        /// </summary>
        AltKey = 32,
    } 
}

