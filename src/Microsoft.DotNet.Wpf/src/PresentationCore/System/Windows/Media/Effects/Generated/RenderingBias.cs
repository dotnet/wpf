//---------------------------------------------------------------------------
//
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// This file was generated, please do not edit it directly.
//
//
//---------------------------------------------------------------------------

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
    ///     RenderingBias - Type of bias to give rendering of the effect
    /// </summary>
    public enum RenderingBias
    {
        /// <summary>
        ///     Performance - Bias towards performance
        /// </summary>
        Performance = 0,

        /// <summary>
        ///     Quality - Bias towards quality
        /// </summary>
        Quality = 1,
    }   
}
