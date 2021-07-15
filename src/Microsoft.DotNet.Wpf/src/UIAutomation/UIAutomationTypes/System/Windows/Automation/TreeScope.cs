// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: TreeScope flags for listening to events

using System;

namespace System.Windows.Automation 
{
    /// <summary>
    /// TreeScope flags for listening to events
    /// </summary>
    [Flags]
#if (INTERNAL_COMPILE)
    internal enum TreeScope
#else
    public enum TreeScope
#endif
    {
        /// <summary>Include the element itself</summary>
        Element = 0x1,
        /// <summary>Include the element's immediate children</summary>
        Children = 0x2,
        /// <summary>Include the element's descendants (includes children)</summary>
        Descendants = 0x4,
        /// <summary>Include the element's parent</summary>
        Parent = 0x8,
        /// <summary>Include the element's ancestors (includes parent)</summary>
        Ancestors = 0x10,
        /// <summary>Include any element in the element tree</summary>
        Subtree = Element | Children | Descendants
    }
}
