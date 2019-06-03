// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// 
// Description: Specifies the effects of drag-and-drop operation.
//
// 
//

using System;

namespace System.Windows
{
    /// <summary>
    /// An enumeration of the DragDropEffects that can be notified to the dragsource.
    /// </summary>
    [Flags]
    public enum DragDropEffects
    {
        /// <summary>
        /// A drop would not be allowed. 
        /// </summary>
        None = 0,
        /// <summary>
        /// A copy operation would be performed.
        /// </summary>
        Copy = 1,
        /// <summary>
        /// A move operation would be performed.
        /// </summary>
        Move = 2,
        /// <summary>
        /// A link from the dropped data to the original data would be established.
        /// </summary>
        Link = 4,
        /// <summary>
        /// A drag scroll operation is about to occur or is occurring in the target. 
        /// </summary>
        Scroll = unchecked((int)0x80000000),
        /// <summary>
        /// All operation is about to occur data is copied or removed from the drag source, and
        /// scrolled in the drop target. 
        /// </summary>
        All = Copy | Move | Scroll,
    }
}

