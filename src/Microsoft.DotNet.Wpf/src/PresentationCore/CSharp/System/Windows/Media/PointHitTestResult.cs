// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

using System.Collections;
using System.Diagnostics;
using MS.Internal;

namespace System.Windows.Media 
{
    /// <summary>
    /// This class returns the point and visual hit during a hit test pass.
    /// </summary>
    public class PointHitTestResult : HitTestResult
    {
        private Point _pointHit;

        /// <summary>
        /// This constructor takes a visual and point respresenting a hit.
        /// </summary>
        public PointHitTestResult(Visual visualHit, Point pointHit) : base(visualHit)
        {
            _pointHit = pointHit;
        }
        
        /// <summary>
        /// The point in local space of the hit visual.
        /// </summary>
        public Point PointHit
        {
            get
            {
                return _pointHit;
            }
        }

        /// <summary>
        ///     Re-expose Visual property strongly typed to 2D Visual.
        /// </summary>
        public new Visual VisualHit 
        { 
            get
            {
                return (Visual) base.VisualHit;
            }
        }
    }
}
