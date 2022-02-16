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

namespace System.Windows
{
    /// <summary>
    ///     TextDecorationUnit - The unit type of text decoration value
    /// </summary>
    public enum TextDecorationUnit
    {
        /// <summary>
        ///     FontRecommended - The unit is the calculated value by layout system
        /// </summary>
        FontRecommended = 0,

        /// <summary>
        ///     FontRenderingEmSize - The unit is the rendering Em size
        /// </summary>
        FontRenderingEmSize = 1,

        /// <summary>
        ///     Pixel - The unit is one pixel
        /// </summary>
        Pixel = 2,
    }   
}
