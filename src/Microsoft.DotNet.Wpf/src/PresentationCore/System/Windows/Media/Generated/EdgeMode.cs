// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// This file was generated, please do not edit it directly.
//
// Please see MilCodeGen.html for more information.
//

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using MS.Internal.PresentationCore;
#if PRESENTATION_CORE
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
#else
using SR=System.Windows.SR;
using SRID=System.Windows.SRID;
#endif

namespace System.Windows.Media
{
    /// <summary>
    ///     EdgeMode - Enum which descibes the manner in which we render edges of non-text 
    ///     primitives.
    /// </summary>
    public enum EdgeMode
    {
        /// <summary>
        ///     Unspecified - No edge mode specfied - do not alter the current edge mode applied to 
        ///     this content.
        /// </summary>
        Unspecified = 0,

        /// <summary>
        ///     Aliased - Render edges of non-text primitives as aliased edges.
        /// </summary>
        Aliased = 1,
    }   
}
