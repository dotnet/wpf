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

namespace System.Windows.Media.Imaging
{
    /// <summary>
    ///     Rotation - The rotation to be applied; only multiples of 90 degrees is supported.
    /// </summary>
    public enum Rotation
    {
        /// <summary>
        ///     Rotate0 - Do not rotate
        /// </summary>
        Rotate0 = 0,

        /// <summary>
        ///     Rotate90 - Rotate 90 degress
        /// </summary>
        Rotate90 = 1,

        /// <summary>
        ///     Rotate180 - Rotate 180 degrees
        /// </summary>
        Rotate180 = 2,

        /// <summary>
        ///     Rotate270 - Rotate 270 degrees
        /// </summary>
        Rotate270 = 3,
    }   
}
