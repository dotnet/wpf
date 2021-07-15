// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: This file contains the intersection details for hit testing.
//
//

using System.Runtime.InteropServices;

using System;

namespace System.Windows.Media 
{
    /// <summary>
    /// IntersectionDetail - Indicates detailed information on the nature
    /// of a geometry intersection operation.  This result is based on the
    /// intersection of the hit geometry and the target geometry (or visual.)
    ///
    /// For example:
    ///     GeometryHitTestResult result = VisualTreeHelper.HitTest(visual, hit_geometry))
    /// or
    ///     IntersectionDetail detail = target.DoesContainWithDetail(hit_geometry)
    ///
    /// </summary>
    public enum IntersectionDetail
    {
        /// <summary>
        /// NotCalculated - No intersection testing has been performed.
        /// </summary>
        NotCalculated = 0,

        /// <summary>
        /// Empty- There is no intersection between the hit geometry and the
        /// target geometry or visual.
        /// </summary>
        Empty = 1,

        /// <summary>
        /// FullyInside - The target geometry or visual is fully inside the
        /// hit geometry.
        /// </summary>
        FullyInside = 2,

        /// <summary>
        /// FullyContains - The target geometry or visual fully contains the
        /// hit geometry.
        /// </summary>
        FullyContains = 3,

        /// <summary>
        /// Intersects - The target geometry or visual overlap the hit geometry
        /// and is neither one contains the other.
        /// </summary>
        Intersects = 4
    }
}


