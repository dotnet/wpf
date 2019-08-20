// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: ShapingOptions enum
//
//

using System;

namespace MS.Internal.Shaping
{
    /// <summary>
    /// Shaping control options
    /// </summary>
    [Flags]
    internal enum ShapingOptions
    {
        /// <summary>
        /// Default behavior
        /// </summary>
        None = 0,

        /// <summary>
        /// Make Unicode control characters visible
        /// </summary>
        DisplayControlCode  = 0x00000001,

        /// <summary>
        /// Ligatures are not to be used for shaping
        /// </summary>
        InhibitLigature     = 0x00000002,
    }    
}
