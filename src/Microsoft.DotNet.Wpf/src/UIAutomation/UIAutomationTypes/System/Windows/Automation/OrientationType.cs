// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Orientation types


using System;
using System.Runtime.InteropServices;

namespace System.Windows.Automation 
{
    /// <summary>
    /// Used to represent Orientation property enumerated types.
    /// </summary>
    [ComVisible(true)]
    [Guid("5F8A77B4-E685-48c1-94D0-8BB6AFA43DF9")]
#if (INTERNAL_COMPILE)
    internal enum OrientationType
#else
    public enum OrientationType
#endif
    {
        /// <summary>No orientation.</summary>
        None,
        /// <summary>Horizontally oriented.</summary>
        Horizontal,
        /// <summary>Vertically oriented.</summary>
        Vertical,
    }
}
