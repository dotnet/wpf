//---------------------------------------------------------------------------
//
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// This file was generated, please do not edit it directly.
//
//
//---------------------------------------------------------------------------

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
    ///     TextRenderingMode - Enum used for specifying what filter mode text should be 
    ///     rendered with (ClearType, grayscale, aliased).
    /// </summary>
    public enum TextRenderingMode
    {
        /// <summary>
        ///     Auto - Rendering engine will use a rendering mode compatible with the 
        ///     TextFormattingMode specified for the control
        /// </summary>
        Auto = 0,

        /// <summary>
        ///     Aliased - Rendering engine will render text with aliased filtering when possible
        /// </summary>
        Aliased = 1,

        /// <summary>
        ///     Grayscale - Rendering engine will render text with grayscale filtering when 
        ///     possible
        /// </summary>
        Grayscale = 2,

        /// <summary>
        ///     ClearType - Rendering engine will render text with ClearType filtering when 
        ///     possible
        /// </summary>
        ClearType = 3,
    }   
}
