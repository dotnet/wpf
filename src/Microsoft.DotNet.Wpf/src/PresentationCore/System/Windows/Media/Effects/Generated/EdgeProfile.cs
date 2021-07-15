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
    ///     EdgeProfile - Type of edge profile to use.
    /// </summary>
    public enum EdgeProfile
    {
        /// <summary>
        ///     Linear - Use a Linear edge profile
        /// </summary>
        Linear = 0,

        /// <summary>
        ///     CurvedIn - Use a curved in edge profile
        /// </summary>
        CurvedIn = 1,

        /// <summary>
        ///     CurvedOut - Use a curved out edge profile
        /// </summary>
        CurvedOut = 2,

        /// <summary>
        ///     BulgedUp - Use a bulged up edge profile
        /// </summary>
        BulgedUp = 3,
    }   
}
