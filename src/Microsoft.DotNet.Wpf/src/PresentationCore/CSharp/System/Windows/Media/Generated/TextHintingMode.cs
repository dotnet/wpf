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
    ///     TextHintingMode - Enum used for specifying how text should be rendered with respect 
    ///     to animated or static text
    /// </summary>
    public enum TextHintingMode
    {
        /// <summary>
        ///     Auto - Rendering engine will automatically determine whether to draw text with 
        ///     quality settings appropriate to animated or static text
        /// </summary>
        Auto = 0,

        /// <summary>
        ///     Fixed - Rendering engine will render text for highest static quality
        /// </summary>
        Fixed = 1,

        /// <summary>
        ///     Animated - Rendering engine will render text for highest animated quality
        /// </summary>
        Animated = 2,
    }   
}
