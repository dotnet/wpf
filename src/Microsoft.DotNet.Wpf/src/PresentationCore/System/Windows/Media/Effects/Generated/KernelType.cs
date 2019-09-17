// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// This file was generated, please do not edit it directly.
//
// Please see MilCodeGen.html for more information.
//

#if PRESENTATION_CORE
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
#else
using SR=System.Windows.SR;
using SRID=System.Windows.SRID;
#endif

namespace System.Windows.Media.Effects
{
    /// <summary>
    ///     KernelType - Type of blur kernel to use.
    /// </summary>
    public enum KernelType
    {
        /// <summary>
        ///     Gaussian - Use a Gaussian filter
        /// </summary>
        Gaussian = 0,

        /// <summary>
        ///     Box - Use a Box filter
        /// </summary>
        Box = 1,
    }   
}
