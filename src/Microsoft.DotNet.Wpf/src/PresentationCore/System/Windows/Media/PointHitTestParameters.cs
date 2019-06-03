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
    /// This is the class for specifying parameters hit testing with a point.
    /// </summary>
    public class PointHitTestParameters : HitTestParameters
    {
        /// <summary>
        /// The constructor takes the point to hit test with.
        /// </summary>
        public PointHitTestParameters(Point point) : base()
        {
            _hitPoint = point;
        }
    
        /// <summary>
        /// The point to hit test against.
        /// </summary>
        public Point HitPoint
        {
            get
            {
                return _hitPoint;
            }
        }

        internal void SetHitPoint(Point hitPoint)
        {
            _hitPoint = hitPoint;
        }

        private Point _hitPoint;
    }
}

