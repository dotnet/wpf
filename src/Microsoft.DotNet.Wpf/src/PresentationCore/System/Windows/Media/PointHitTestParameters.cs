// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


//

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

