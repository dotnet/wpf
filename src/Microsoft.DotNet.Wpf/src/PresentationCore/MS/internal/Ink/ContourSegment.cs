// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Windows;
using System.Windows.Media;

namespace MS.Internal.Ink
{
    /// <summary>
    /// A helper structure representing an edge of a contour, where 
    /// the edge is either a straight segment or an arc of a circle.
    /// ContourSegment are alwais directed clockwise (i.e with the contour
    /// inner area being on the right side.
    /// Used in hit-testing a contour vs another contour.
    /// </summary> 
    internal readonly struct ContourSegment
    {
        /// <summary>
        /// Constructor for linear segments
        /// </summary>
        /// <param name="begin">segment's begin point</param>
        /// <param name="end">segment's end point</param>
        internal ContourSegment(Point begin, Point end)
        {
            _begin = begin;
            _vector = DoubleUtil.AreClose(begin, end) ? new Vector(0, 0) : (end - begin);
            _radius = new Vector(0, 0);
        }
        
        /// <summary>
        /// Constructor for arcs
        /// </summary>
        /// <param name="begin">arc's begin point</param>
        /// <param name="end">arc's end point</param>
        /// <param name="center">arc's center</param>
        internal ContourSegment(Point begin, Point end, Point center)
        {
            _begin = begin;
            _vector = end - begin;
            _radius = center - begin;
        }

        /// <summary> Tells whether the segment is arc or straight </summary>
        internal bool IsArc { get { return (_radius.X != 0) || (_radius.Y != 0); } }

        /// <summary> Returns the begin point of the segment </summary>
        internal Point  Begin { get { return _begin; } }

        /// <summary> Returns the end point of the segment </summary>
        internal Point  End { get { return _begin + _vector; } }

        /// <summary> Returns the vector from Begin to End </summary>
        internal Vector Vector { get { return _vector; } }

        /// <summary> Returns the vector from Begin to the center of the circle 
        /// (zero vector for linear segments </summary>
        internal Vector Radius { get { return _radius; } }

        #region Fields

        private readonly Point   _begin;
        private readonly Vector  _vector;
        private readonly Vector  _radius;

        #endregion
    }
}
