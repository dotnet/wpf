// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;
using System.Collections.Generic;
using System.Globalization;

namespace MS.Internal.Ink
{
    #region Lasso

    /// <summary>
    /// Represents a lasso for selecting/cutting ink strokes with.
    /// Lasso is a sequence of points defining a complex region (polygon)
    /// </summary>
    internal class Lasso
    {
        #region Constructors

        /// <summary>
        /// Default c-tor. Used in incremental hit-testing.
        /// </summary>
        internal Lasso()
        {
            _points = new List<Point>();
        }

        #endregion

        #region API

        /// <summary>
        /// Returns the bounds of the lasso
        /// </summary>
        internal Rect Bounds
        {
            get { return _bounds; }
            set { _bounds = value;}
        }

        /// <summary>
        /// Tells whether the lasso captures any area
        /// </summary>
        internal bool IsEmpty
        {
            get
            {
                System.Diagnostics.Debug.Assert(_points != null);
                // The value is based on the assumption that the lasso is normalized
                // i.e. it has no duplicate points or collinear sibling segments.
                return (_points.Count < 3);
            }
        }

        /// <summary>
        /// Returns the count of points in the lasso
        /// </summary>
        internal int PointCount
        {
            get
            {
                System.Diagnostics.Debug.Assert(_points != null);
                return _points.Count;
            }
        }

        /// <summary>
        /// Index-based read-only accessor to lasso points
        /// </summary>
        /// <param name="index">index of the point to return</param>
        /// <returns>a point in the lasso</returns>
        internal Point this[int index]
        {
            get
            {
                System.Diagnostics.Debug.Assert(_points != null);
                System.Diagnostics.Debug.Assert((0 <= index) && (index < _points.Count));

                return _points[index];
            }
        }

        /// <summary>
        /// Extends the lasso by appending more points
        /// </summary>
        /// <param name="points">new points</param>
        internal void AddPoints(IEnumerable<Point> points)
        {
            System.Diagnostics.Debug.Assert(null != points);

            foreach (Point point in points)
            {
                AddPoint(point);
            }
        }

        /// <summary>
        /// Appends a point to the lasso
        /// </summary>
        /// <param name="point">new lasso point</param>
        internal void AddPoint(Point point)
        {
            System.Diagnostics.Debug.Assert(_points != null);
            if (!Filter(point))
            {
                // The point is not filtered, add it to the lasso
                AddPointImpl(point);
            }
        }

        /// <summary>
        /// This method implement the core algorithm to check whether a point is within a polygon
        /// that are formed by the lasso points.
        /// </summary>
        /// <param name="point"></param>
        /// <returns>true if the point is contained within the lasso; false otherwise </returns>
        internal bool Contains(Point point)
        {
            System.Diagnostics.Debug.Assert(_points != null);

            if (false == _bounds.Contains(point))
            {
                return false;
            }

            bool isHigher = false;
            int last = _points.Count;
            while (--last >= 0)
            {
                if (!DoubleUtil.AreClose(_points[last].Y,point.Y))
                {
                    isHigher = (point.Y < _points[last].Y);
                    break;
                }
            }

            bool isInside = false;
            Point prevLassoPoint = _points[_points.Count - 1];
            for (int i = 0; i < _points.Count; i++)
            {
                Point lassoPoint = _points[i];
                if (DoubleUtil.AreClose(lassoPoint.Y, point.Y))
                {
                    if (DoubleUtil.AreClose(lassoPoint.X, point.X))
                    {
                        isInside = true;
                        break;
                    }
                    if ((0 != i) && DoubleUtil.AreClose(prevLassoPoint.Y, point.Y) &&
                        DoubleUtil.GreaterThanOrClose(point.X, Math.Min(prevLassoPoint.X, lassoPoint.X)) &&
                        DoubleUtil.LessThanOrClose(point.X, Math.Max(prevLassoPoint.X, lassoPoint.X)))
                    {
                        isInside = true;
                        break;
                    }
                }
                else if (isHigher != (point.Y < lassoPoint.Y))
                {
                    isHigher = !isHigher;
                    if (DoubleUtil.GreaterThanOrClose(point.X, Math.Max(prevLassoPoint.X, lassoPoint.X)))
                    {
                        // there certainly is an intersection on the left
                        isInside = !isInside;
                    }
                    else if (DoubleUtil.GreaterThanOrClose(point.X, Math.Min(prevLassoPoint.X, lassoPoint.X)))
                    {
                        // The X of the point lies within the x ranges for the segment.
                        // Calculate the x value of the point where the segment intersects with the line.
                        Vector lassoSegment = lassoPoint - prevLassoPoint;
                        System.Diagnostics.Debug.Assert(lassoSegment.Y != 0);
                        double x = prevLassoPoint.X + (lassoSegment.X / lassoSegment.Y) * (point.Y - prevLassoPoint.Y);
                        if (DoubleUtil.GreaterThanOrClose(point.X, x))
                        {
                            isInside = !isInside;
                        }
                    }
                }
                prevLassoPoint = lassoPoint;
            }
            return isInside;
        }

        internal StrokeIntersection[] HitTest(StrokeNodeIterator iterator)
        {
            System.Diagnostics.Debug.Assert(_points != null);
            System.Diagnostics.Debug.Assert(iterator != null);

            if (_points.Count < 3)
            {
                //
                // it takes at least 3 points to create a lasso
                //
                return Array.Empty<StrokeIntersection>();
            }

            //
            // We're about to perform hit testing with a lasso.
            // To do so we need to iterate through each StrokeNode.
            // As we do, we calculate the bounding rect between it
            // and the previous StrokeNode and store this in 'currentStrokeSegmentBounds'
            //
            // Next, we check to see if that StrokeNode pair's bounding box intersects
            // with the bounding box of the Lasso points.  If not, we continue iterating through
            // StrokeNode pairs.
            //
            // If it does, we do a more granular hit test by pairing points in the Lasso, getting
            // their bounding box and seeing if that bounding box intersects our current StrokeNode
            // pair
            //

            Point lastNodePosition = new Point();
            Point lassoLastPoint = _points[_points.Count - 1];
            Rect currentStrokeSegmentBounds = Rect.Empty;

            // Initilize the current crossing to be an empty one
            LassoCrossing currentCrossing = LassoCrossing.EmptyCrossing;

            // Creat a list to hold all the crossings
            List<LassoCrossing> crossingList = new List<LassoCrossing>();
            for (int i = 0; i < iterator.Count; i++)
            {
                StrokeNode strokeNode = iterator[i];
                Rect nodeBounds = strokeNode.GetBounds();
                currentStrokeSegmentBounds.Union(nodeBounds);

                // Skip the node if it's outside of the lasso's bounds
                if (currentStrokeSegmentBounds.IntersectsWith(_bounds) == true)
                {
                    // currentStrokeSegmentBounds, made up of the bounding box of
                    // this StrokeNode unioned with the last StrokeNode,
                    // intersects the lasso bounding box.
                    //
                    // Now we need to iterate through the lasso points and find out where they cross
                    //
                    Point lastPoint = lassoLastPoint;
                    foreach (Point point in _points)
                    {
                        //
                        // calculate a segment of the lasso from the last point
                        // to the current point
                        //
                        Rect lassoSegmentBounds = new Rect(lastPoint, point);

                        //
                        // see if this lasso segment intersects with the current stroke segment
                        //
                        if (!currentStrokeSegmentBounds.IntersectsWith(lassoSegmentBounds))
                        {
                            lastPoint = point;
                            continue;
                        }

                        //
                        // the lasso segment DOES intersect with the current stroke segment
                        // find out precisely where
                        //
                        StrokeFIndices strokeFIndices = strokeNode.CutTest(lastPoint, point);

                        lastPoint = point;
                        if (strokeFIndices.IsEmpty)
                        {
                            // current lasso segment does not hit the stroke segment, continue with the next lasso point
                            continue;
                        }

                        // Create a potentially new crossing for the current hit testing result.
                        LassoCrossing potentialNewCrossing = new LassoCrossing(strokeFIndices, strokeNode);

                        // Try to merge with the current crossing. If the merge is succussful (return true), the new crossing is actually
                        // continueing the current crossing, so do not start a new crossing. Otherwise, start a new one and add the existing
                        // one to the list.
                        if (!currentCrossing.Merge(potentialNewCrossing))
                        {
                            // start a new crossing and add the existing on to the list
                            crossingList.Add(currentCrossing);
                            currentCrossing = potentialNewCrossing;
                        }
                    }
}

                // Continue with the next node
                currentStrokeSegmentBounds = nodeBounds;
                lastNodePosition = strokeNode.Position;
            }


            // Adding the last crossing to the list, if valid
            if (!currentCrossing.IsEmpty)
            {
                crossingList.Add(currentCrossing);
            }

            // Handle the special case of no intersection at all
            if (crossingList.Count == 0)
            {
                // the stroke was either completely inside the lasso
                // or outside the lasso
                if (this.Contains(lastNodePosition))
                {
                    StrokeIntersection[] strokeIntersections = new StrokeIntersection[1];
                    strokeIntersections[0] = StrokeIntersection.Full;
                    return strokeIntersections;
                }
                else
                {
                    return Array.Empty<StrokeIntersection>();
                }
            }

            // It is still possible that the current crossing list is not sorted or overlapping.
            // Sort the list and merge the overlapping ones.
            SortAndMerge(ref crossingList);

            // Produce the hit test results and store them in a list
            List<StrokeIntersection> strokeIntersectionList = new List<StrokeIntersection>();
            ProduceHitTestResults(crossingList, strokeIntersectionList);

            return strokeIntersectionList.ToArray();
        }

        /// <summary>
        /// Sort and merge the crossing list
        /// </summary>
        /// <param name="crossingList">The crossing list to sort/merge</param>
        private static void SortAndMerge(ref List<LassoCrossing> crossingList)
        {
            // Sort the crossings based on the BeginFIndex values
            crossingList.Sort();

            List<LassoCrossing> mergedList = new List<LassoCrossing>();
            LassoCrossing mcrossing = LassoCrossing.EmptyCrossing;
            foreach (LassoCrossing crossing in crossingList)
            {
                System.Diagnostics.Debug.Assert(!crossing.IsEmpty && crossing.StartNode.IsValid && crossing.EndNode.IsValid);
                if (!mcrossing.Merge(crossing))
                {
                    System.Diagnostics.Debug.Assert(!mcrossing.IsEmpty && mcrossing.StartNode.IsValid && mcrossing.EndNode.IsValid);
                    mergedList.Add(mcrossing);
                    mcrossing = crossing;
                }
            }
            if (!mcrossing.IsEmpty)
            {
                System.Diagnostics.Debug.Assert(!mcrossing.IsEmpty && mcrossing.StartNode.IsValid && mcrossing.EndNode.IsValid);
                mergedList.Add(mcrossing);
            }
            crossingList = mergedList;
        }


        /// <summary>
        /// Helper function to find out whether a point is inside the lasso
        /// </summary>
        private bool SegmentWithinLasso(StrokeNode strokeNode, double fIndex)
        {
            bool currentSegmentWithinLasso;
            if (DoubleUtil.AreClose(fIndex, StrokeFIndices.BeforeFirst))
            {
                // This should check against the very first stroke node
                currentSegmentWithinLasso = this.Contains(strokeNode.GetPointAt(0f));
            }
            else if (DoubleUtil.AreClose(fIndex, StrokeFIndices.AfterLast))
            {
                // This should check against the last stroke node
                currentSegmentWithinLasso = this.Contains(strokeNode.Position);
            }
            else
            {
                currentSegmentWithinLasso = this.Contains(strokeNode.GetPointAt(fIndex));
            }

            return currentSegmentWithinLasso;
        }

        /// <summary>
        /// Helper function to find out the hit test result
        /// </summary>
        private void ProduceHitTestResults(
                                List<LassoCrossing> crossingList, List<StrokeIntersection> strokeIntersections)
        {
            bool previousSegmentInsideLasso = false;
            for (int x = 0; x <= crossingList.Count; x++)
            {
                bool currentSegmentWithinLasso = false;
                bool canMerge = true;
                StrokeIntersection si = new StrokeIntersection();
                if (x == 0)
                {
                    si.HitBegin = StrokeFIndices.BeforeFirst;
                    si.InBegin = StrokeFIndices.BeforeFirst;
                }
                else
                {
                    si.InBegin = crossingList[x - 1].FIndices.EndFIndex;
                    si.HitBegin = crossingList[x - 1].FIndices.BeginFIndex;
                    currentSegmentWithinLasso = SegmentWithinLasso(crossingList[x - 1].EndNode, si.InBegin);
                }

                if (x == crossingList.Count)
                {
                    // For a special case when the last intersection is something like (1.2, AL).
                    // As a result the last InSegment should be empty.
                    if (DoubleUtil.AreClose(si.InBegin, StrokeFIndices.AfterLast))
                    {
                        si.InEnd = StrokeFIndices.BeforeFirst;
                    }
                    else
                    {
                        si.InEnd = StrokeFIndices.AfterLast;
                    }
                    si.HitEnd = StrokeFIndices.AfterLast;
                }
                else
                {
                    si.InEnd = crossingList[x].FIndices.BeginFIndex;

                    // For a speical case when the first intersection is something like (BF, 0.67).
                    // As a result the first InSegment should be empty
                    if (DoubleUtil.AreClose(si.InEnd, StrokeFIndices.BeforeFirst))
                    {
                        System.Diagnostics.Debug.Assert(DoubleUtil.AreClose(si.InBegin, StrokeFIndices.BeforeFirst));
                        si.InBegin = StrokeFIndices.AfterLast;
                    }

                    si.HitEnd = crossingList[x].FIndices.EndFIndex;
                    currentSegmentWithinLasso = SegmentWithinLasso(crossingList[x].StartNode, si.InEnd);

                    // If both the start and end position of the current crossing is
                    // outside the lasso, the crossing is a hit-only intersection, i.e., the in-segment is empty.
                    if (!currentSegmentWithinLasso && !SegmentWithinLasso(crossingList[x].EndNode, si.HitEnd))
                    {
                        currentSegmentWithinLasso = true;
                        si.HitBegin = crossingList[x].FIndices.BeginFIndex;
                        si.InBegin = StrokeFIndices.AfterLast;
                        si.InEnd = StrokeFIndices.BeforeFirst;
                        canMerge = false;
                    }
                }

                if (currentSegmentWithinLasso)
                {
                    if (x > 0 && previousSegmentInsideLasso && canMerge)
                    {
                        // we need to consolidate with the previous segment
                        StrokeIntersection previousIntersection = strokeIntersections[strokeIntersections.Count - 1];

                        // For example: previousIntersection = [BF, AL, BF, 0.0027], si = [BF, 0.0027, 0.049, 0.063]
                        if (previousIntersection.InSegment.IsEmpty)
                        {
                            previousIntersection.InBegin = si.InBegin;
                        }
                        previousIntersection.InEnd = si.InEnd;
                        previousIntersection.HitEnd = si.HitEnd;
                        strokeIntersections[strokeIntersections.Count - 1] = previousIntersection;
                    }
                    else
                    {
                        strokeIntersections.Add(si);
                    }

                    if (DoubleUtil.AreClose(si.HitEnd, StrokeFIndices.AfterLast))
                    {
                        // The strokeIntersections already cover the end of the stroke. No need to continue.
                        return;
                    }
                }
                previousSegmentInsideLasso = currentSegmentWithinLasso;
            }
        }

        /// <summary>
        /// This flag is set to true when a lasso point has been modified or removed
        /// from the list, which will invalidate incremental lasso hitteting
        /// </summary>
        internal bool IsIncrementalLassoDirty
        {
            get
            {
                return _incrementalLassoDirty;
            }
            set
            {
                _incrementalLassoDirty = value;
            }
        }

        /// <summary>
        /// Get a reference to the lasso points store
        /// </summary>
        protected List<Point> PointsList
        {
            get
            {
                return _points;
            }
        }

        /// <summary>
        /// Filter out duplicate points (and maybe in the futuer colinear points).
        /// Return true if the point should be filtered
        /// </summary>
        protected virtual bool Filter(Point point)
        {
            // First point should not be filtered
            if (0 == _points.Count)
            {
                return false;
            }
            // ISSUE-2004/06/14-vsmirnov - If the new segment is collinear with the last one,
            // don't add the point but modify the last point instead.
            Point lastPoint = _points[_points.Count - 1];
            Vector vector = point - lastPoint;

            // The point will be filtered out, i.e. not added to the list, if the distance to the previous point is
            // within the tolerance
            return (Math.Abs(vector.X) < MinDistance && Math.Abs(vector.Y) < MinDistance);
        }

        /// <summary>
        /// Implemtnation of add point
        /// </summary>
        /// <param name="point"></param>
        protected virtual void AddPointImpl(Point point)
        {
            _points.Add(point);
            _bounds.Union(point);
        }
        #endregion

        #region Fields

        private List<Point>             _points;
        private Rect                    _bounds                 = Rect.Empty;
        private bool                    _incrementalLassoDirty  = false;
        private static readonly double  MinDistance             = 1.0;

        #endregion

        /// <summary>
        /// Simple helper struct used to track where the lasso crosses a stroke
        /// we should consider making this a class if generics perf is bad for structs
        /// </summary>
        private struct LassoCrossing : IComparable
        {
            internal StrokeFIndices FIndices;
            internal StrokeNode StartNode;
            internal StrokeNode EndNode;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="newFIndices"></param>
            /// <param name="strokeNode"></param>
            public LassoCrossing(StrokeFIndices newFIndices, StrokeNode strokeNode)
            {
                System.Diagnostics.Debug.Assert(!newFIndices.IsEmpty);
                System.Diagnostics.Debug.Assert(strokeNode.IsValid);
                FIndices = newFIndices;
                StartNode = EndNode = strokeNode;
            }

            /// <summary>
            /// ToString
            /// </summary>
            public override string ToString()
            {
                return FIndices.ToString();
            }

            /// <summary>
            /// Construct an empty LassoCrossing
            /// </summary>
            public static LassoCrossing EmptyCrossing
            {
                get
                {
                    LassoCrossing crossing = new LassoCrossing();
                    crossing.FIndices = StrokeFIndices.Empty;
                    return crossing;
                }
            }

            /// <summary>
            /// Return true if this crossing is an empty one; false otherwise
            /// </summary>
            public bool IsEmpty
            {
                get { return FIndices.IsEmpty;}
            }

            /// <summary>
            /// Implement the interface used for comparison
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public int CompareTo(object obj)
            {
                System.Diagnostics.Debug.Assert(obj is LassoCrossing);
                LassoCrossing crossing = (LassoCrossing)obj;
                if (crossing.IsEmpty && this.IsEmpty)
                {
                    return 0;
                }
                else if (crossing.IsEmpty)
                {
                    return 1;
                }
                else if (this.IsEmpty)
                {
                    return -1;
                }
                else
                {
                    return FIndices.CompareTo(crossing.FIndices);
                }
            }

            /// <summary>
            /// Merge two crossings into one.
            /// </summary>
            /// <param name="crossing"></param>
            /// <returns>Return true if these two crossings are actually overlapping and merged; false otherwise</returns>
            public bool Merge(LassoCrossing crossing)
            {
                if (crossing.IsEmpty)
                {
                    return false;
                }

                if (FIndices.IsEmpty && !crossing.IsEmpty)
                {
                    FIndices = crossing.FIndices;
                    StartNode = crossing.StartNode;
                    EndNode = crossing.EndNode;
                    return true;
                }

                if(DoubleUtil.GreaterThanOrClose(crossing.FIndices.EndFIndex, FIndices.BeginFIndex) &&
                    DoubleUtil.GreaterThanOrClose(FIndices.EndFIndex, crossing.FIndices.BeginFIndex))
                {
                    if (DoubleUtil.LessThan(crossing.FIndices.BeginFIndex, FIndices.BeginFIndex))
                    {
                        FIndices.BeginFIndex = crossing.FIndices.BeginFIndex;
                        StartNode = crossing.StartNode;
                    }

                    if (DoubleUtil.GreaterThan(crossing.FIndices.EndFIndex, FIndices.EndFIndex))
                    {
                        FIndices.EndFIndex =  crossing.FIndices.EndFIndex;
                        EndNode = crossing.EndNode;
                    }
                    return true;
                }

                return false;
            }
        }
    }
    #endregion


    #region Single-Loop Lasso

    /// <summary>
    /// Implement a special lasso that considers only the first loop
    /// </summary>
    internal class SingleLoopLasso : Lasso
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        internal SingleLoopLasso() : base(){}

        /// <summary>
        /// Return true if the point will be filtered out and should NOT be added to the list
        /// </summary>
        protected override bool Filter(Point point)
        {
            List<Point> points = PointsList;

            // First point should not be filtered
            if (0 == points.Count)
            {
                // Just add the new point to the lasso
                return false;
            }

            // Don't add this point if the lasso already has a loop; or
            // if it's filtered by base class's filter.
            if (true == _hasLoop || true == base.Filter(point))
            {
                // Don't add this point to the lasso.
                return true;
            }

            double intersection = 0f;

            // Now check whether the line lastPoint->point intersect with the
            // existing lasso.

            if (true == GetIntersectionWithExistingLasso(point, ref intersection))
            {
                System.Diagnostics.Debug.Assert(intersection >= 0 && intersection <= points.Count - 2);

                if (intersection == points.Count - 2)
                {
                    return true;
                }

                // Adding the new point will form a loop
                int i = (int) intersection;

                if (!DoubleUtil.AreClose(i, intersection))
                {
                    // Move points[i] to the intersection position
                    Point intersectionPoint = new Point(0, 0);
                    intersectionPoint.X = points[i].X + (intersection - i) * (points[i + 1].X - points[i].X);
                    intersectionPoint.Y = points[i].Y + (intersection - i) * (points[i + 1].Y - points[i].Y);
                    points[i] = intersectionPoint;
                    IsIncrementalLassoDirty = true;
                }

                // Since the lasso has a self loop and the loop starts at points[i], points[0] to
                // points[i-1] should be removed
                if (i > 0)
                {
                    points.RemoveRange(0, i /*count*/);   // Remove points[0] to points[i-1]
                    IsIncrementalLassoDirty = true;
                }

                if (true == IsIncrementalLassoDirty)
                {
                    // Update the bounds
                    Rect bounds = Rect.Empty;
                    for (int j = 0; j < points.Count; j++)
                    {
                        bounds.Union(points[j]);
                    }
                    Bounds = bounds;
                }

                // The lasso has a self_loop, any more points will be neglected.
                _hasLoop = true;

                // Don't add this point to the lasso.
                return true;
            }

            // Just add the new point to the lasso
            return false;
        }

        protected override void AddPointImpl(Point point)
        {
            _prevBounds = Bounds;
            base.AddPointImpl(point);
        }

        /// <summary>
        /// If the line _points[Count -1]->point insersect with the existing lasso, return true
        /// and bIndex value is set to a doulbe value representing position of the intersection.
        /// </summary>
        private bool GetIntersectionWithExistingLasso(Point point, ref double bIndex)
        {
            List<Point> points = PointsList;
            int count = points.Count;

            Rect newRect = new Rect(points[count - 1], point);

            if (false == _prevBounds.IntersectsWith(newRect))
            {
                // The point is not contained in the bound of the existing lasso, no intersection.
                return false;
            }

            for (int i = 0; i < count -2; i++)
            {
                Rect currRect = new Rect(points[i], points[i+1]);
                if (!currRect.IntersectsWith(newRect))
                {
                    continue;
                }

                double s = FindIntersection(points[count-1] - points[i],            /*hitBegin*/
                                                    point - points[i],              /*hitEnd*/
                                                    new Vector(0, 0),               /*orgBegin*/
                                                    points[i+1] - points[i]         /*orgEnd*/);
                if (s >=0 && s <= 1)
                {
                    // Intersection found, adjust the fIndex
                    bIndex = i + s;
                    return true;
                }
            }

            // No intersection
            return false;
        }


        /// <summary>
        /// Finds the intersection between the segment [hitBegin, hitEnd] and the segment [orgBegin, orgEnd].
        /// </summary>
        private static double FindIntersection(Vector hitBegin, Vector hitEnd, Vector orgBegin, Vector orgEnd)
        {
            System.Diagnostics.Debug.Assert(hitEnd != hitBegin && orgBegin != orgEnd);

            //----------------------------------------------------------------------
            // Source: http://isc.faqs.org/faqs/graphics/algorithms-faq/
            // Subject 1.03: How do I find intersections of 2 2D line segments?
            //
            // Let A,B,C,D be 2-space position vectors.  Then the directed line
            // segments AB & CD are given by:
            //
            // AB=A+r(B-A), r in [0,1]
            // CD=C+s(D-C), s in [0,1]
            //
            // If AB & CD intersect, then
            //
            // A+r(B-A)=C+s(D-C), or  Ax+r(Bx-Ax)=Cx+s(Dx-Cx)
            // Ay+r(By-Ay)=Cy+s(Dy-Cy)  for some r,s in [0,1]
            //
            // Solving the above for r and s yields
            //
            //      (Ay-Cy)(Dx-Cx)-(Ax-Cx)(Dy-Cy)
            //  r = -----------------------------  (eqn 1)
            //      (Bx-Ax)(Dy-Cy)-(By-Ay)(Dx-Cx)
            //
            //      (Ay-Cy)(Bx-Ax)-(Ax-Cx)(By-Ay)
            //  s = -----------------------------  (eqn 2)
            //      (Bx-Ax)(Dy-Cy)-(By-Ay)(Dx-Cx)
            //
            // Let P be the position vector of the intersection point, then
            //
            //  P=A+r(B-A) or Px=Ax+r(Bx-Ax) and Py=Ay+r(By-Ay)
            //
            // By examining the values of r & s, you can also determine some
            // other limiting conditions:
            //  If 0 <= r <= 1 && 0 <= s <= 1, intersection exists
            //  r < 0 or r > 1 or s < 0 or s > 1 line segments do not intersect
            //  If the denominator in eqn 1 is zero, AB & CD are parallel
            //  If the numerator in eqn 1 is also zero, AB & CD are collinear.
            //  If they are collinear, then the segments may be projected to the x-
            //  or y-axis, and overlap of the projected intervals checked.
            //
            // If the intersection point of the 2 lines are needed (lines in this
            // context mean infinite lines) regardless whether the two line
            // segments intersect, then
            //  If r > 1, P is located on extension of AB
            //  If r < 0, P is located on extension of BA
            //  If s > 1, P is located on extension of CD
            //  If s < 0, P is located on extension of DC
            // Also note that the denominators of eqn 1 & 2 are identical.
            //
            // References:
            // [O'Rourke (C)] pp. 249-51
            // [Gems III] pp. 199-202 "Faster Line Segment Intersection,"
            //----------------------------------------------------------------------

            // Calculate the vectors.
            Vector AB = orgEnd - orgBegin;          // B - A
            Vector CA = orgBegin - hitBegin;        // A - C
            Vector CD = hitEnd - hitBegin;          // D - C
            double det = Vector.Determinant(AB, CD);

            if (DoubleUtil.IsZero(det))
            {
                // The segments are parallel. no intersection
                return NoIntersection;
            }

            double r = AdjustFIndex(Vector.Determinant(AB, CA) / det);

            if (r >= 0 && r <= 1)
            {
                // The line defined AB does cross the segment CD.
                double s = AdjustFIndex(Vector.Determinant(CD, CA) / det);
                if (s >= 0 && s <= 1)
                {
                    // The crossing point is on the segment AB as well.
                    // Intersection found.
                    return s;
                }
            }

            // No intersection found
            return NoIntersection;
        }

        /// <summary>
        /// Clears double's computation fuzz around 0 and 1
        /// </summary>
        internal static double AdjustFIndex(double findex)
        {
            return DoubleUtil.IsZero(findex) ? 0 : (DoubleUtil.IsOne(findex) ? 1 : findex);
        }

        private bool _hasLoop                           = false;
        private Rect _prevBounds                        = Rect.Empty;
        private static readonly double NoIntersection   = StrokeFIndices.BeforeFirst;
    }
    #endregion
}
