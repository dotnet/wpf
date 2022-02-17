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
    ///     GeometryCombineMode - This enumeration describes the type of combine operation to 
    ///     be performed.
    /// </summary>
    public enum GeometryCombineMode
    {
        /// <summary>
        ///     Union - Produce a geometry representing the set of points contained in either
        ///     the first or the second geometry.
        /// </summary>
        Union = 0,

        /// <summary>
        ///     Intersect - Produce a geometry representing the set of points common to the first
        ///     and the second geometries.
        /// </summary>
        Intersect = 1,

        /// <summary>
        ///     Xor - Produce a geometry representing the set of points contained in the
        ///     first geometry or the second geometry, but not both.
        /// </summary>
        Xor = 2,

        /// <summary>
        ///     Exclude - Produce a geometry representing the set of points contained in the
        ///     first geometry but not the second geometry.
        /// </summary>
        Exclude = 3,
    }   
}
