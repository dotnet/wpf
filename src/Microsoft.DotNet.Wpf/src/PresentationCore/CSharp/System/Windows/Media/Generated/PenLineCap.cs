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
    ///     PenLineCap - Enum which descibes the drawing of the ends of a line.
    /// </summary>
    public enum PenLineCap
    {
        /// <summary>
        ///     Flat - Flat line cap.
        /// </summary>
        Flat = 0,

        /// <summary>
        ///     Square - Square line cap.
        /// </summary>
        Square = 1,

        /// <summary>
        ///     Round - Round line cap.
        /// </summary>
        Round = 2,

        /// <summary>
        ///     Triangle - Triangle line cap.
        /// </summary>
        Triangle = 3,
    }   
}
