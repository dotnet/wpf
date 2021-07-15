// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//                                             

// Semantics
// =========
//
// DEFINITION:
// A TimeIntervalCollection (TIC) is a set of points on the time line, which may
// range from negative infinity (not including negative infinity itself) up to positive
// infinity (potentially including positive infinity).  It may also include a point Null,
// which does not belong on the time line.  This non-domain point is considered to
// represent a state of 'Stopped'.
//
//
// OPERATIONS:
// For any given time point P, a TIC must know whether it contains P or not.
// For any open interval (A,B), a TIC must know whether it has a non-empty intersection with (A,B).
// For any given TICs T and S, we must be able to determine if T and S have an non-empty intersection.
//
//
// GENERAL DATA REPRESENTATION:
// A TIC is represented by a set of nodes ordered on the real time line.
// Each node is indexed, and has an associated time _nodeTime[x] and two flags:
//   _nodeIsPoint[x] specifies whether the time point at _nodeTime[x] is included in the TIC, and
//   _nodeIsInterval[x] specifies whether the open interval (_nodeTime[x], _nodeTime[x+1])
// is included in the TIC.  If the node at x is the last node, and _nodeIsInterval[x] == true,
// then the TIC includes all points in the open interval (_nodeTime[x], Infinity).
// The presence of the Null point is denoted by the boolean _containsNullPoint.
//
// Example #1:
//   TIC includes closed-open interval [3,6) and point 7.
//
//   Time:    3       6       7     infinity
//        ---[X]=====[ ]-----[X]-------...
//   Index:   0       1       2
//
//       _nodeTime[0] = 3           _nodeTime[1] = 6            _nodeTime[2] = 7
//    _nodeIsPoint[0] = true     _nodeIsPoint[1] = false     _nodeIsPoint[2] = true
// _nodeIsInterval[0] = true  _nodeIsInterval[1] = false  _nodeIsInterval[2] = false
//
// Example #2:
//   TIC includes point 0, the open interval (3,8), and the interval (8,infinity]; does not include point 8.
//
//   Time:    0       3       8     infinity
//        ---[X]-----[ ]=====[ ]=======...
//   Index:   0       1       2
//
//       _nodeTime[0] = 0            _nodeTime[1] = 3            _nodeTime[2] = 8
//    _nodeIsPoint[0] = true      _nodeIsPoint[1] = false     _nodeIsPoint[2] = false
// _nodeIsInterval[0] = false  _nodeIsInterval[1] = true   _nodeIsInterval[2] = true
//
// RULES FOR LEGAL DATA REPRESENTATION:
// In order to keep the TIC and its algorithms optimized, we enforce the following rules:
//
// 1) All nodes are stored in strictly increasing _nodeTime order.  E.g. nodes remain sorted and
//      each node has a unique _nodeTime.
//
// 2) No unnecessary nodes are present: for any x < xMax, in the boolean sequence:
//
//      _nodeIsPoint[x], _nodeIsInterval[x], _nodeIsPoint[x+1], _nodeIsInterval[x+1]
//           [ ]----------------------------------[ ]--------------------------------
//
//    we maintain the following invariants:
//    [A] Out of the last three, at least one is true.
//        Otherwise we don't need node X+1 to represent the same TIC.
//        If all are false, we have an illegal EMPTY node.
//    [B] Out of the last three, at least one is false.
//        Otherwise we don't need node X+1 to represent the same TIC.
//        If all are true, we have an illegal SATURATED node.
//    [C] For the first index x=0, at least one out of _nodeIsPoint[x] or _nodeIsInterval[x] is true.
//        Otherwise we don't need node 0 to represent the same TIC,
//        and we have another case of an illegal EMPTY node.
//
// 3) As a consequence of legal data representation, the TIC contains no points prior to the time
//     of its first node, e.g. if time T < _nodeTime[0] then T is not in the TIC.
//
//
// NOTE:
// Please refer to the above comments and rules when reading documentation for specific methods below.
//


#if DEBUG
#define TRACE
#endif // DEBUG

using System;
using System.Collections;
using System.Diagnostics;
using MS.Internal;

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// A list of timing events observed internally by a TimelineClock object.
    /// </summary>
    internal struct TimeIntervalCollection
    {
        #region External interface

        #region Methods

        /// <summary>
        /// Creates an empty collection
        /// </summary>
        private TimeIntervalCollection(bool containsNullPoint)
        {
            Debug.Assert(_minimumCapacity >= 2);

            _containsNullPoint = containsNullPoint;

            _count = 0;
            _current = 0;
            _invertCollection = false;

            _nodeTime = null;
            _nodeIsPoint = null;
            _nodeIsInterval = null;
        }
        
        /// <summary>
        /// Creates a collection containing a single time point.
        /// </summary>
        private TimeIntervalCollection(TimeSpan point)
            : this(false)
        {
            InitializePoint(point);
        }

        /// <summary>
        /// Reuses an existing collection so it now contains a single time point.
        /// </summary>
        private void InitializePoint(TimeSpan point)
        {
            Debug.Assert(IsEmpty);  // We should start with a new or Clear()-ed collection first

            EnsureAllocatedCapacity(_minimumCapacity);

            _nodeTime[0] = point;
            _nodeIsPoint[0] = true;
            _nodeIsInterval[0] = false;
            Debug.Assert(_nodeIsInterval[0] == false);

            _count = 1;
        }

        /// <summary>
        /// Creates a collection that spans from a single point to infinity.
        /// </summary>
        private TimeIntervalCollection(TimeSpan point, bool includePoint)
            : this(false)
        {
            InitializePoint(point);

            _nodeIsPoint[0] = includePoint;
            _nodeIsInterval[0] = true;
        }

        /// <summary>
        /// Creates a collection containing a single interval.
        /// If from == to and the interval is not open-open, then a single point is created.
        /// </summary>
        /// <param name="from">
        /// The first endpoint time.
        /// </param>
        /// <param name="includeFrom">
        /// Specifies whether the point from is included in the TIC.
        /// </param>
        /// <param name="to">
        /// The last endpoint time.
        /// </param>
        /// <param name="includeTo">
        /// Specifies whether the point to is included in the TIC.
        /// </param>
        private TimeIntervalCollection(TimeSpan from, bool includeFrom, TimeSpan to, bool includeTo)
            : this(false)
        {
            EnsureAllocatedCapacity(_minimumCapacity);

            _nodeTime[0] = from;

            if (from == to)  // Create single point
            {
                if (includeFrom || includeTo)  // Make sure we aren't trying to create a point from an open-open interval
                {
                    _nodeIsPoint[0] = true;
                    _count = 1;
                }
                else  // We are trying to create an open interval (x, x), so just create an empty TIC
                {
                    Debug.Assert(_count == 0);  // The boolean constructor already did the job for us
                }
            }
            else  // from != to
            {
                if (from < to)
                {
                    _nodeIsPoint[0] = includeFrom;
                    _nodeIsInterval[0] = true;

                    _nodeTime[1] = to;
                    _nodeIsPoint[1] = includeTo;
                }
                else  // We are given reversed coordinates
                {
                    _nodeTime[0] = to;
                    _nodeIsPoint[0] = includeTo;
                    _nodeIsInterval[0] = true;

                    _nodeTime[1] = from;
                    _nodeIsPoint[1] = includeFrom;
                }

                _count = 2;
            }
        }

        /// <summary>
        /// Removes all time intervals from the collection.
        /// </summary>
        internal void Clear()
        {
            // Deallocate ONLY if we have previously expanded beyond default length to avoid redundant
            // reallocation.  If we called Clear, we are likely to reuse the collection soon.
            if (_nodeTime != null && _nodeTime.Length > _minimumCapacity)
            {
                _nodeTime = null;
                _nodeIsPoint = null;
                _nodeIsInterval = null;
            }

            _containsNullPoint = false;

            _count = 0;
            _current = 0;
            _invertCollection = false;
        }

        // Used for optimizing slip computation in Clock
        internal bool IsSingleInterval
        {
            get
            {
                return (_count < 2) || (_count == 2 && _nodeIsInterval[0]);
            }
        }

        // Used for optimizing slip computation in Clock
        internal TimeSpan FirstNodeTime
        {
            get
            {
                Debug.Assert(_count > 0);
                return _nodeTime[0];
            }
        }

        // Used for optimizing slip computation in Clock
        // This method will discard nodes beyond the first two nodes.
        // The only scenario where this method is called on a larger-than-size-2 TIC is
        // when the parent of a Media wraps around in a Repeat.  Then we only enter
        // the Media's active period on the wraparound part of the TIC, so it is the only important
        // part to leave.
        // Example: the parent has Duration=10 and RepeatBehavior=Forever.  It went from 9ms to 2ms (wraparound).
        // Our default TIC is {[0, 2], (9, 10)}.  Slipping this by 1 will change it to {[1, 2]}.  It is apparent
        // that this is the only part of the parent that actually overlaps our active zone.
        internal TimeIntervalCollection SlipBeginningOfConnectedInterval(TimeSpan slipTime)
        {
            if (slipTime == TimeSpan.Zero)  // The no-op case
            {
                return this;
            }            

            TimeIntervalCollection slippedCollection;
            if (_count < 2 || slipTime > _nodeTime[1] - _nodeTime[0])
            {
                // slipTime > the connected duration, which basically eliminates the parent TIC interval for us;
                // This would only happen when media "outruns" the parent container, producing negative slip.
                slippedCollection = TimeIntervalCollection.Empty;
            }
            else
            {
                // Just shift the first node by slipAmount; the constructor handles the a==b case.
                slippedCollection = new TimeIntervalCollection(_nodeTime[0] + slipTime, _nodeIsPoint[0],
                                                               _nodeTime[1]           , _nodeIsPoint[1]);
            }

            if (this.ContainsNullPoint)
            {
                slippedCollection.AddNullPoint();
            }
            return slippedCollection;
        }

        // Used for DesiredFrameRate adjustments in Clock
        internal TimeIntervalCollection SetBeginningOfConnectedInterval(TimeSpan beginTime)
        {
#if DEBUG
            Debug.Assert(IsSingleInterval);
#endif
            Debug.Assert(0 < _count && _count <= 2);

            if (_count == 1)
            {
                return new TimeIntervalCollection(_nodeTime[0], _nodeIsPoint[0],
                                                  beginTime,    true);
            }
            else  // _count == 2
            {
                Debug.Assert(beginTime <= _nodeTime[1]);
                return new TimeIntervalCollection(beginTime,    false,
                                                  _nodeTime[1], _nodeIsPoint[1]);
            }
        }

        /// <summary>
        /// Creates a collection containing a single time point
        /// </summary>
        static internal TimeIntervalCollection CreatePoint(TimeSpan time)
        {
            return new TimeIntervalCollection(time);
        }

        /// <summary>
        /// Creates a collection containing a closed-open time interval [from, to)
        /// </summary>
        static internal TimeIntervalCollection CreateClosedOpenInterval(TimeSpan from, TimeSpan to)
        {
            return new TimeIntervalCollection(from, true, to, false);
        }

        /// <summary>
        /// Creates a collection containing an open-closed time interval (from, to]
        /// </summary>
        static internal TimeIntervalCollection CreateOpenClosedInterval(TimeSpan from, TimeSpan to)
        {
            return new TimeIntervalCollection(from, false, to, true);
        }

        /// <summary>
        /// Creates a collection containing a closed time interval [from, infinity)
        /// </summary>
        static internal TimeIntervalCollection CreateInfiniteClosedInterval(TimeSpan from)
        {
            return new TimeIntervalCollection(from, true);
        }

        /// <summary>
        /// Creates an empty collection
        /// </summary>
        static internal TimeIntervalCollection Empty
        {
            get
            {
                return new TimeIntervalCollection();
            }
        }

        /// <summary>
        /// Creates a collection with the null point
        /// </summary>
        static internal TimeIntervalCollection CreateNullPoint()
        {
            return new TimeIntervalCollection(true);
        }

        /// <summary>
        /// Adds the null point to an existing collection
        /// </summary>
        internal void AddNullPoint()
        {
            _containsNullPoint = true;
        }


        /// <returns>
        /// Returns whether the time point is contained in the collection
        /// </returns>
        // RUNNING TIME: O(log2(_count)) worst-case
        // IMPLEMENTATION FOR CONTAINS(TIME) OPERATION:
        // To determine if point at time T is contained in the TIC, do the following:
        //
        //   1) Find the largest index x, such that _nodeTime[x] <= T
        //
        //   2) IF no such x exists, then _nodeTime[x] > T for every valid x;
        //        then T comes earlier than any node and cannot be in the TIC by Rule #3 above.
        //        Diagram: ----T----[0]----[1]----[2]---...
        //
        //   3) ELSE IF x exists and _nodeTime[x] == T, then T happens to coincide with a TIC node.
        //        We check if TIC contains _nodeTime[x] by querying and RETURNING _nodeIsPoint[x].
        //        Diagram -----[ ]----[T,x]----[ ]----...
        //
        //   4) ELSE x exists and _nodeTime[x] < T, then T happens to fall after a TIC node at x, but before
        //        the next TIC node if any later nodes exist.  We check if TIC contains the open interval
        //        (_nodeTime[x], _nodeTime[x+1]) or (_nodeTime[x], infinity) if node x was the last node.
        //        We do this by querying and RETURNING _nodeIsInterval[x].
        //        Diagram: -----[x]----T----[x+1]----[x+2]--....
        //          =OR=   -----[x]----T----infinity
        //
        internal bool Contains(TimeSpan time)
        {
            int index = Locate(time);  // Find the previous or equal time

            if (index < 0)  // Queried time lies before the earliest interval's begin time
            {
                return false;
            }
            else if (_nodeTime[index] == time)  // Queried time falls exactly onto a node
            {
                return _nodeIsPoint[index];
            }
            else  // Queried time comes after the node
            {
                Debug.Assert(_nodeTime[index] < time);

                return _nodeIsInterval[index];
            }
        }


        /// <returns>
        /// Returns whether the open interval (from, to) has an intersection with this collection
        /// </returns>
        // RUNNING TIME: O(log2(_count)) worst-case
        // IMPLEMENTATION FOR INTERSECTS(FROM,TO) OPERATION:
        // We want to determine if the open interval (From,To), abbreviated (F,T), has a non-zero intersection
        // with the TIC.  Assert F<T because if F=T then (F,T) is a non-interval.  Do the following:
        //
        //   1) Find the largest index fromIndex, such that _nodeTime[fromIndex] <= F (set to -1 if none exists)
        //   2) Find the largest index   toIndex, such that _nodeTime[  toIndex] <= T (set to -1 if none exists)
        //
        //   3) IF fromIndex is equal to toIndex, and they have a non-negative index, then:
        //
        //       * Suppose fromIndex==toIndex==-1.  Then then the entire interval (F,T) comes
        //         before the first node of the TIC, and by Rule #3 above, no point inside (F,T) can
        //         be contained in the TIC.
        //         Therefore, the intersection between (F,T) and the TIC is null and we RETURN FALSE.
        //
        //         Diagram:     (F)====(T)
        //                  ----------------[0]-----[1]---[2]------....
        //
        //       * Else fromIndex,toIndex >= 0.  F comes right at or after _nodeTime[fromIndex] and before
        //         any next node; T comes strictly after _nodeTime[fromIndex] (because we asserted F<T) and
        //         also before any next node.  Then we can treat (F,T) as a single point P, because any
        //         point P inside (F,T) will strictly fit inside the open interval
        //         (_nodeTime[fromIndex], nextNodeTime_or_Infinity).
        //         Thus we can simply RETURN _nodeIsInterval[fromIndex].
        //
        //         Diagram (lowercase f denotes fromIndex; t denotes toIndex):
        //                              (F)=========(T)
        //                  ----[f,t]-----------------------[ ]---.....
        //
        //         The entire clause can be generalized with the statement:
        //         RETURN (toIndex >= 0) && _nodeIsInterval[toIndex]
        //
        //         Notice that this clause is good to short-circuit early, because it traps cases of
        //         complete mismatches, where the interval is not in the TIC's normal range.
        //
        //   4) ELSE IF the difference between fromIndex and toIndex is exactly 1 (e.g. fromIndex+1 == toIndex), then:
        //
        //       * Suppose fromIndex is -1, thus F falls before the first node.
        //         Then toIndex is at least 0, thus T falls at least aligned with the first node.
        //         Now it matters whether T is at or after the first node.  If T is at the first node,
        //         then all points in (F,T) lie *before* the first node and we have no possible intersection,
        //         so we have to return FALSE.  Else T is after the first node, then some point in (F,T) lies
        //         exactly on the first node, and some points lie after it.  By rule #2C, one of these two parts
        //         must be contained in the TIC.  So then we return TRUE.
        //
        //         This is simplified as RETURN (_nodeTime[toIndex] < T)
        //
        //         Diagram (lowercase t denotes toIndex):
        //                     (F)=======(T)
        //                  -------------[t]-----[ ]----.....
        //
        //                     (F)=========(T)
        //                  ----------[t]--------[ ]----.....
        //
        //       * Else fromIndex is non-negative, thus F falls at or right after node at [fromIndex].
        //         Then toIndex falls at least at or right after node at [fromIndex+1].
        //         (F,T) now must overlap the open interval (_nodeTime[fromIndex], _nodeTime[toIndex]),
        //         and IFF _nodeTime[toIndex] < T then it will also overlap the point at _nodeTime[toIndex]
        //         and part of the open interval (_nodeTime[toIndex], nextNodeTime_or_Infinity).  In the
        //         first case we merely check _nodeIsInterval[fromIndex].  In the second case, we invoke
        //         rule #2B and conclude that an intersection must exist somewhere between all three parts.
        //         Hence we RETURN _nodeIsInterval[fromIndex] || (_nodeTime[toIndex] < T).
        //
        //         Diagram (lowercase t denotes toIndex; t denotes toIndex):
        //                        (F)=======(T)
        //                  --[f]-----------[t]-----[ ]----.....
        //
        //                     (F)===========(T)
        //                  ---[f]------[t]--------[ ]----.....
        //
        //         The entire clause can now be further simplified as the following statement:
        //         RETURN (_nodeTime[toIndex] < T) || (fromIndex >= 0 && _nodeIsInterval[fromIndex])
        //
        //   5) ELSE the difference between fromIndex and toIndex is greater than 1 (e.g. fromIndex+1 < toIndex), then:
        //
        //       * Suppose fromIndex is -1, thus F falls before the first node.
        //         Then toIndex is at least 1, thus T falls at least aligned with the second node.
        //         Then (F,T) overlaps at least point _nodeTime[0] and open interval (_nodeTime[0], _nodeTime[1]).
        //         By rule #2C above, at least one of those two must be in the TIC, hence some point in (F,T)
        //         is also in the TIC, we have a non-null intersection and RETURN TRUE.
        //
        //         Diagram (lowercase t denotes toIndex):
        //                      (F)=========(T)
        //                  ----------[ ]---[t]----[ ]----.....
        //
        //                      (F)=============(T)
        //                  --------[ ]-----[t]------[ ]----.....
        //
        //       * Else fromIndex is non-negative, thus F falls at or right after node at [fromIndex].
        //         Then toIndex falls at least at or after node at [fromIndex+2].
        //         Then the following parts of the TIC must partially overlap the interval:
        //            (A) open interval (_nodeTime[fromIndex], _nodeTime[fromIndex+1])
        //            (B) point at _nodeTime[fromIndex+1]
        //            (C) open interval (_nodeTime[fromIndex+1], _nodeTime[fromIndex+2])
        //         By rule #2B, at least one of the consecutive parts in the above sequence must be included in the TIC.
        //         Therefore, a point in the interval (F,T) must be contained in the TIC, and we RETURN TRUE.
        //
        //         Diagram (lowercase f denotes fromIndex; t denotes toIndex):
        //                         (F)=========(T)
        //                  --[f]--------[ ]---[t]----[ ]----.....
        //
        //                      (F)============(T)
        //                  ----[f]----[ ]-----[t]------[ ]----.....
        //
        //       Both sub-clauses lead to the same result, so we uniformly RETURN TRUE when reaching this clause.
        //
        internal bool Intersects(TimeSpan from, TimeSpan to)
        {
            if (from == to)  // The open interval (x, x) is null and has no intersections
            {
                return false;
            }
            else if (from > to)  // If to and from are reversed, swap them back
            {
                TimeSpan temp = from;
                from = to;
                to = temp;
            }

            int fromIndex = Locate(from);  // Find the nearest indices for to and from
            int   toIndex = Locate(to);

            Debug.Assert(fromIndex <= toIndex);

            if (fromIndex == toIndex)
            {
                // Since we are testing an *open* interval, the only way we can intersect is by checking
                // if the interior of the arc is part of the TIC.
                return (toIndex >= 0) && _nodeIsInterval[toIndex];
            }
            // The interval only overlaps one TIC node; fromIndex may be -1 here
            else if (fromIndex + 1 == toIndex)
            {
                Debug.Assert(toIndex >= 0); // Since fromIndex!=toIndex, toIndex must be >= 0

                // By rule #2B and C, if we fall across an arc boundary, we must therefore intersect the TIC.
                return (to > _nodeTime[toIndex]) || (fromIndex >= 0 && _nodeIsInterval[fromIndex]);
            }
            else
            {
                Debug.Assert(fromIndex + 1 < toIndex);

                // We must fall across an arc boundary, and by rule #2B we must therefore intersect the TIC.
                return true;
            }
        }

        /// <returns>
        /// Returns whether this collection has a non-empty intersection with the other collection
        /// </returns>
        // RUNNING TIME: O(_count) worst-case
        // IMPLEMENTATION FOR INTERSECTS(OTHER) OPERATION:
        //
        //  We implement intersection by "stacking" the two TICs atop each other and seeing if
        //  there is any point or interval common to both.  We do this by having two indexers,
        //  index1 and index2, traverse the lengths of both TICs simultaneously.  We maintain
        //  the following invariant: each indexer, when "projected" onto the other TIC than the one
        //  it actually indexes into, falls less than a node ahead of the other indexer.
        //  To rephrase intuitively, the indexers never fall out of step by having one get
        //  too far ahead of the other.
        //
        //  Example:
        //
        //  this    ----[0]----[1]--------------------[2]----[3]-----------[4]---------[5]------...
        //  other   --------------------[0]----[1]------------------[2]----------------[3]------...
        //                      ^index1
        //                               ^index2
        //
        //  Our invariant means that one of the indexed nodes either coincides exactly with
        //  the other, as is the case for nodes this[4] and other[2] in the above example,
        //  or "projects" into the other node's subsequent interval; in the above example,
        //  other[index2] projects onto the interval of this[index1].
        //
        //  At each iteration, we check for an intersection at:
        //    A) the latter of the indexed nodes, and
        //    B) the interval right after the latter indexed node
        //
        //  3 possible scenarios:
        //  CASE I.   index1 < index2  intersects if _nodeIsInterval[index1] && (_nodeIsPoint[index2] || _nodeIsInterval[index2])
        //  CASE II.  index1 > index2  intersects if _nodeIsInterval[index2] && (_nodeIsPoint[index1] || _nodeIsInterval[index1])
        //  CASE III. index1 = index2  intersects if (_nodeIsPoint[index1] && _nodeIsPoint[index2]) || (_nodeIsInterval[index1] && _nodeIsInterval[index2])
        //
        //  We say that in Case I, index1 is dominant in the sense that index2 points to a node on index1's "turf";
        //  We move index2 through index1's entire interval to check for intersections against it.  Once index2 passes
        //  index1's interval, we advance index1 as well.  Then we again check which scenario we end up in.
        //
        //  Case II is treated anti-symmetrically to Case I.
        //
        //  Case III is special, because we cannot treat it the same as Case I or II.  This is becasue we have to check
        //  for a point-point intersection, and check which indexer should be advanced next.  It is possible that both
        //  indexers need to be advanced if the next 2 nodes are also equal.
        //
        //  We continue advancing the pointers until we find an intersection or run out of nodes on either of the TICs.
        //
        internal bool Intersects(TimeIntervalCollection other)
        {
            Debug.Assert(!_invertCollection);  // Make sure we never leave inverted mode enabled

            if (this.ContainsNullPoint && other.ContainsNullPoint)  // Short-circuit null point intersections
            {
                return true;
            }
            else if (this.IsEmptyOfRealPoints || other.IsEmptyOfRealPoints)  // Only intersection with an empty TIC is at null points, which case is already handled
            {
                return false;
            }
            else  // Both TICs are non-empty and don't intersect at the null point
            {
                return IntersectsHelper(other);
            }
        }

        // This method was made separate to detect intersections with inverses when needed
        private bool IntersectsHelper(TimeIntervalCollection other)
        {
            // Make sure the indexers are starting next to each other
            IntersectsHelperPrepareIndexers(ref this, ref other);

            // The outer loop does not bail, rather we return directly from inside the loop
            bool intersectionFound = false;
            while (true)
            {
                // The inner loops iterate through the subset of a TIC

                // CASE I.
                // In this case, index1 is the dominant indexer: index2 is on its turf and we keep advancing index2 and checking for intesections
                // After this helper, index2 will no longer be ahead of index1
                if ((this.CurrentNodeTime < other.CurrentNodeTime) &&
                    IntersectsHelperUnequalCase(ref this, ref other, ref intersectionFound))
                {
                    return intersectionFound;
                }


                // CASE II.
                // In this case, index2 is the dominant indexer: index1 is on its turf and we keep advancing index1 and checking for intesections
                // After this helper, index1 will no longer be ahead of index2
                if ((this.CurrentNodeTime > other.CurrentNodeTime) &&
                    IntersectsHelperUnequalCase(ref other, ref this, ref intersectionFound))
                {
                    return intersectionFound;
                }


                // CASE III.
                // In this case, neither indexer is dominant: they are pointing to the same point in time
                // We keep doing this until the indices are no longer equal
                while (this.CurrentNodeTime == other.CurrentNodeTime)
                {
                    if (IntersectsHelperEqualCase(ref this, ref other, ref intersectionFound))
                    {
                        return intersectionFound;
                    }
                }
            }
        }

        // Make sure the indexers are starting next to each other
        static private void IntersectsHelperPrepareIndexers(ref TimeIntervalCollection tic1, ref TimeIntervalCollection tic2)
        {
            Debug.Assert(!tic1.IsEmptyOfRealPoints);  // We shouldn't reach here if either TIC is empty
            Debug.Assert(!tic2.IsEmptyOfRealPoints);

            tic1.MoveFirst();  // Point _current to the first node in both TICs
            tic2.MoveFirst();

            // First bring tic1._current and tic2._current within an interval of each other
            if (tic1.CurrentNodeTime < tic2.CurrentNodeTime)
            {
                // Keep advancing tic1._current as far as possible while keeping _nodeTime[tic1._current] < _nodeTime[tic2._current]
                while (!tic1.CurrentIsAtLastNode && (tic1.NextNodeTime <= tic2.CurrentNodeTime))
                {
                    tic1.MoveNext();
                }
            }
            else if (tic2.CurrentNodeTime < tic1.CurrentNodeTime)
            {
                // Keep advancing tic2._current as far as possible while keeping _nodeTime[tic1._current] > _nodeTime[tic2._current]
                while (!tic2.CurrentIsAtLastNode && (tic2.NextNodeTime <= tic1.CurrentNodeTime))
                {
                    tic2.MoveNext();
                }
            }
        }

        // Returns true if we know at this point whether an intersection is possible between tic1 and tic2
        // The fact of whether an intersection was found is stored in the ref parameter intersectionFound
        static private bool IntersectsHelperUnequalCase(ref TimeIntervalCollection tic1, ref TimeIntervalCollection tic2, ref bool intersectionFound)
        {
            Debug.Assert(!intersectionFound);  // If an intersection was already found, we should not reach this far

            if (tic1.CurrentNodeIsInterval)  // If we are within an interval in tic1, we immediately have an intersection
            {
                // If we have gotten into this method, tic1._current comes earlier than does tic2._current;
                // Suppose the following assert is false; then by Rule #2A, tic2's previous interval must be included;
                // If this was the case, then tic2's previous interval overlapped tic1's current interval.  Since it's
                // included, we would have encountered an intersection before even reaching this method!  Then you
                // should not even be here now.  Else suppose we are at tic2's first node, then the below Assert
                // follows directly from Rule #3.
                Debug.Assert(tic2.CurrentNodeIsPoint || tic2.CurrentNodeIsInterval);

                intersectionFound = true;
                return true;
            }
            else if (tic1.CurrentIsAtLastNode)  // // If we are already at the end of tic1, we ran out of nodes that may have an intersection
            {
                intersectionFound = false;
                return true;
            }
            else  // Else we are inside a non-included interval in tic1, no intersection is possible, but keep advancing tic2._current
            {
                while (!tic2.CurrentIsAtLastNode && (tic2.NextNodeTime <= tic1.NextNodeTime))
                {
                    tic2.MoveNext();
                }

                // If nextNodeTime1 is null, we should never get here because the IF statement would have caught it and quit
                Debug.Assert(!tic1.CurrentIsAtLastNode);  // Thus tic1._current can be safely advanced now

                // Now tic1._current can be safely advanced forward
                tic1.MoveNext();

                // If we broke out of Case I, its conditional should no longer hold true:
                Debug.Assert(tic1.CurrentNodeTime >= tic2.CurrentNodeTime);

                // Enforce our invariant: neither index gets too far ahead of the other.
                Debug.Assert(tic2.CurrentIsAtLastNode || (tic1.CurrentNodeTime < tic2.NextNodeTime));
                Debug.Assert(tic1.CurrentIsAtLastNode || (tic2.CurrentNodeTime < tic1.NextNodeTime));

                // Tell the main algorithm to continue working
                return false;
            }
        }

        // Returns true if we know at this point whether an intersection is possible between tic1 and tic2
        // The fact of whether an intersection was found is stored in the ref parameter intersectionFound
        static private bool IntersectsHelperEqualCase(ref TimeIntervalCollection tic1, ref TimeIntervalCollection tic2, ref bool intersectionFound)
        {
            // If the nodes match exactly, check if the points are both included, or if the intervals are both included
            if ((tic1.CurrentNodeIsPoint && tic2.CurrentNodeIsPoint) ||
                (tic1.CurrentNodeIsInterval && tic2.CurrentNodeIsInterval))
            {
                intersectionFound = true;
                return true;
            }
            // We did not find an intersection, but advance whichever index has a closer next node
            else if (!tic1.CurrentIsAtLastNode && (
                tic2.CurrentIsAtLastNode || (tic1.NextNodeTime < tic2.NextNodeTime)))
            {
                tic1.MoveNext();
            }
            else if (!tic2.CurrentIsAtLastNode && (
                     tic1.CurrentIsAtLastNode || (tic2.NextNodeTime < tic1.NextNodeTime)))
            {
                tic2.MoveNext();
            }
            else if (!tic1.CurrentIsAtLastNode && !tic2.CurrentIsAtLastNode)
            {
                // If both indices have room to advance, and we haven't yet advanced either one, it must be the next nodes are also exactly equal
                Debug.Assert(tic1.NextNodeTime == tic2.NextNodeTime);

                // It is necessary to advance both indices simultaneously, otherwise we break our invariant - one will be too far ahead
                tic1.MoveNext();
                tic2.MoveNext();
            }
            else  // The only way we could get here is if both indices are pointing to the last nodes
            {
                Debug.Assert(tic1.CurrentIsAtLastNode && tic2.CurrentIsAtLastNode);

                // We have exhausted all the nodes and not found an intersection; bail
                intersectionFound = false;
                return true;
            }

            // Enforce our invariant: neither index gets too far ahead of the other.
            Debug.Assert(tic2.CurrentIsAtLastNode || (tic1.CurrentNodeTime < tic2.NextNodeTime));
            Debug.Assert(tic1.CurrentIsAtLastNode || (tic2.CurrentNodeTime < tic1.NextNodeTime));

            // Tell the main algorithm to continue working
            return false;
        }

        /// <returns>
        /// Returns whether this collection has a non-empty intersection with the inverse of the other collection
        /// </returns>
        internal bool IntersectsInverseOf(TimeIntervalCollection other)
        {
            Debug.Assert(!_invertCollection);  // Make sure we never leave inverted mode enabled

            if (this.ContainsNullPoint && !other.ContainsNullPoint)  // Intersection at null points
            {
                return true;
            }
            if (this.IsEmptyOfRealPoints)  // We are empty, and have no null point; we have nothing to intersect
            {
                return false;
            }
            else if (other.IsEmptyOfRealPoints ||  // We are non-empty, and other is the inverse of empty (e.g. covers all real numbers, so we must intersect), OR...
                     this._nodeTime[0] < other._nodeTime[0])  // Neither TIC is empty, and we start first; this means the inverted "other" by necessity
                                                              // overlaps our first node, so it must intersect either our node or subsequent interval.
            {
                return true;
            }
            else  // Neither TIC is empty, and other starts no later than we do; then use regular intersection logic with inverted boolean flags
            {
                other.SetInvertedMode(true);

                bool returnValue = IntersectsHelper(other);

                other.SetInvertedMode(false);  // Make sure we don't leave other TIC in an inverted state!

                return returnValue;
            }
        }


        /// <returns>
        /// Returns whether this collection has a non-empty intersection with a potentially infinite
        /// periodic collection defined by a set of parameters.
        /// </returns>
        /// <remarks>
        /// The periodic TIC, or PTIC, represents the subset of the active period in a timeline where time
        /// flows non-linearly.  Specifically, it contains the points of reversal in autoreversing timelines,
        /// and the accel and decel periods in timelines with acceleration.
        /// </remarks>
        /// <param name="beginTime">Begin time of the periodic collection.</param>
        /// <param name="period">Length of a single iteration in the periodic collection.</param>
        /// <param name="appliedSpeedRatio">Ratio by which to scale down the periodic collection.</param>
        /// <param name="accelRatio">Ratio of the length of the accelerating portion of the iteration.</param>
        /// <param name="decelRatio">Ratio of the length of the decelerating portion of the iteration.</param>
        /// <param name="isAutoReversed">Indicates whether reversed arcs should follow after forward arcs.</param>
        internal bool IntersectsPeriodicCollection(TimeSpan beginTime,  Duration period, double appliedSpeedRatio,
                                                   double accelRatio, double decelRatio,
                                                   bool isAutoReversed)
        {
            Debug.Assert(!_invertCollection);  // Make sure we never leave inverted mode enabled

            if ( IsEmptyOfRealPoints                                      // If we have no real points, no intersection with the PTIC is possible
              || (period == TimeSpan.Zero)                                // The PTIC has no nonzero period, we define such intersections nonexistent
              || (accelRatio == 0 && decelRatio == 0 && !isAutoReversed)  // The PTIC has no non-linear points, no intersection possible
              || !period.HasTimeSpan                                      // We have an indefinite period, e.g. we are not periodic
              || appliedSpeedRatio > period.TimeSpan.Ticks)               // If the speed ratio is high enough the period will effectively be 0
            {
                return false;
            }

            // By now, we know that:
            // (A) Both we and the PTIC are non-empty
            // (B) We are a subset of the active period, which is the superset of the PTIC

            // Find the smallest n such that _nodeTime[n+1] > beginTime; n can be the last index, so that _nodeTime[n+1] is infinity

            MoveFirst();

            Debug.Assert(beginTime <= CurrentNodeTime);  // The PTIC is clipped by the active period, and we are a subset of the active period
            Debug.Assert(CurrentIsAtLastNode || beginTime < NextNodeTime);

            long beginTimeInTicks = beginTime.Ticks;
            
            long periodInTicks = (long)((double)period.TimeSpan.Ticks / appliedSpeedRatio);

            // PeriodInTicks may overflow if appliedSpeedRatio is sufficiently small.
            // The best we can do is clamp the value to MaxValue.
            if (periodInTicks < 0)
            {
                periodInTicks = Int64.MaxValue / 2;
            }

            long doublePeriod = 2 * periodInTicks;

            long accelPeriod = (long)(accelRatio * (double)periodInTicks);
            long decelPeriod = (long)((1.0 - decelRatio) * (double)periodInTicks);  // This is where deceleration BEGINS.

            // We walk through the TIC and convert from TIC's coordinates into wrapped-around PTIC coordinates:
            //
            //  *======o   Linear   *============o  ...(wraparound to front)
            //   Accel *============o    Decel
            //         ^            ^            ^
            //    accelPeriod  decelPeriod  periodInTicks

            while (_current < _count)
            {
                long projectedCurrentNodeTime;
                bool isOnReversingArc = false;

                if (isAutoReversed)  // If autoreversed, our effective period is doubled and we check for reversed arcs
                {
                    projectedCurrentNodeTime = ((CurrentNodeTime.Ticks - beginTimeInTicks) % doublePeriod);
                    if (projectedCurrentNodeTime >= periodInTicks)
                    {
                        projectedCurrentNodeTime = doublePeriod - projectedCurrentNodeTime;  // We are on a reversed arc
                        isOnReversingArc = true;
                    }
                }
                else  // Default, non-autoreversed case
                {
                    projectedCurrentNodeTime = (CurrentNodeTime.Ticks - beginTimeInTicks) % periodInTicks;
                }


                if ((0 < projectedCurrentNodeTime && projectedCurrentNodeTime < accelPeriod)  // If we fall strictly into the accel zone, or...
                 || (decelPeriod < projectedCurrentNodeTime))                    // We fall strictly into the decel zone
                                                                                 // (note we KNOW that projectedCNT < periodInTicks by definition of modulo)
                {
                    return true;
                }
                else if ((projectedCurrentNodeTime == 0 || projectedCurrentNodeTime == decelPeriod)
                      && CurrentNodeIsPoint)  // We fall exactly onto the beginning of an accel or decel zone, point intersection
                {
                    return true;
                }
                else if (CurrentNodeIsInterval)
                {
                    if ((projectedCurrentNodeTime == 0 && accelPeriod > 0)
                     || (projectedCurrentNodeTime == decelPeriod && (decelPeriod < periodInTicks)))
                    // We fall exactly onto the beginning of an accel or decel zone and have the interval intersect
                    {
                        return true;
                    }
                    else  // Else our node falls into the linear zone, but our interval may overlap a later Accel/Decel zone.
                          // Check if the interval is just long enough to stretch to the next non-linear zone.
                    {
                        long projectedTimeUntilIntersection;
                        if (isOnReversingArc)
                        {
                            projectedTimeUntilIntersection = projectedCurrentNodeTime - accelPeriod;
                        }
                        else
                        {
                            projectedTimeUntilIntersection = decelPeriod - projectedCurrentNodeTime;
                        }

                        if (CurrentIsAtLastNode
                          || (NextNodeTime.Ticks - CurrentNodeTime.Ticks >= projectedTimeUntilIntersection))
                          // We have an intersection, so long as we aren't clipped by endTime
                        {
                            return true;
                        }
                    }
                }

                // We haven't found any intersection at the present node and interval, advance to the next node
                MoveNext();
            }

            return false;  // We have exhausted all nodes and found no intersection.
        }

        /// <returns>
        /// Returns whether this collection has intersections with multiple distinct periods of a
        /// potentially infinite periodic collection defined by a set of parameters.
        /// </returns>
        /// <remarks>
        /// The periodic TIC, or PTIC, represents the subset of the active period in a timeline where time
        /// flows non-linearly.  Specifically, it contains the points of reversal in autoreversing timelines,
        /// and the accel and decel periods in timelines with acceleration.
        /// </remarks>
        /// <param name="beginTime">Begin time of the periodic collection.</param>
        /// <param name="period">Length of a single iteration in the periodic collection.</param>
        /// <param name="appliedSpeedRatio">Ratio by which to scale down the periodic collection.</param>
        internal bool IntersectsMultiplePeriods(TimeSpan beginTime, Duration period, double appliedSpeedRatio)
        {
            Debug.Assert(!_invertCollection);  // Make sure we never leave inverted mode enabled

            if (_count < 2                                    // If we have 0-1 real points, no intersection with multiple periods is possible
              || (period == TimeSpan.Zero)                    // The PTIC has no nonzero period, we define such intersections nonexistent
              || !period.HasTimeSpan                          // We have an indefinite period, e.g. we are not periodic
              || appliedSpeedRatio > period.TimeSpan.Ticks)   // If the speed ratio is high enough the period will effectively be 0              
            {
                return false;
            }
            
            long periodInTicks = (long)((double)period.TimeSpan.Ticks / appliedSpeedRatio);

            // PeriodInTicks may overflow if appliedSpeedRatio is sufficiently small;
            // In this case we will effectively have a single huge period, so nothing to detect here.
            if (periodInTicks <= 0)
            {
                return false;
            }
            else  // Normal case, compare the period in which the first and last nodes fall
            {
                long firstNodePeriod = (FirstNodeTime - beginTime).Ticks / periodInTicks;
                TimeSpan lastNodeTime = _nodeTime[_count - 1];
                long lastNodePeriod = (lastNodeTime - beginTime).Ticks / periodInTicks;

                return (firstNodePeriod != lastNodePeriod);
            }
        }

        /// <summary>
        /// Used for projecting the end of a fill period.  When calling, we already know that we intersect the fill period
        /// but not the active period.
        /// </summary>
        /// <returns>
        /// Returns a collection which is the projection of the argument point onto the defined periodic function.
        /// </returns>
        /// <param name="projection">An empty output projection, passed by reference to allow TIC reuse.</param>
        /// <param name="beginTime">Begin time of the periodic function.</param>
        /// <param name="endTime">The end (expiration) time of the periodic function.</param>
        /// <param name="period">Length of a single iteration in the periodic collection.</param>
        /// <param name="appliedSpeedRatio">Ratio by which to scale down the periodic collection.</param>
        /// <param name="accelRatio">Ratio of the length of the accelerating portion of the iteration.</param>
        /// <param name="decelRatio">Ratio of the length of the decelerating portion of the iteration.</param>
        /// <param name="isAutoReversed">Indicates whether reversed arcs should follow after forward arcs.</param>
        internal void ProjectPostFillZone(ref TimeIntervalCollection projection,
                                          TimeSpan beginTime, TimeSpan endTime, Duration period,
                                          double appliedSpeedRatio, double accelRatio, double decelRatio,
                                          bool isAutoReversed)
        {
            Debug.Assert(projection.IsEmpty);  // Make sure the projection was properly cleared first
            Debug.Assert(!_invertCollection);  // Make sure we never leave inverted mode enabled
            Debug.Assert(beginTime <= endTime);     // Ensure legitimate begin/end clipping parameters

            Debug.Assert(!IsEmptyOfRealPoints);  // We assume this function is ONLY called when this collection overlaps the postfill zone.  So we cannot be empty.
            Debug.Assert(!period.HasTimeSpan || period.TimeSpan > TimeSpan.Zero || beginTime == endTime);  // Check the consistency of degenerate case where simple duration is zero; expiration time should equal beginTime
            Debug.Assert(!_nodeIsInterval[_count - 1]);  // We should not have an infinite domain set

            long outputInTicks;

            if (beginTime == endTime)  // Degenerate case when our active period is a single point; project only that point
            {
                outputInTicks = 0;
            }
            else  // The case of non-zero active duration
            {
                outputInTicks = (long)(appliedSpeedRatio * (double)(endTime - beginTime).Ticks);

                if (period.HasTimeSpan)  // Case of finite simple duration; in the infinite case we are already done
                {
                    long periodInTicks = period.TimeSpan.Ticks;  // Start by folding the point into its place inside a simple duration

                    if (isAutoReversed)
                    {
                        long doublePeriod = periodInTicks << 1;  // Fast multiply by 2
                        outputInTicks = outputInTicks % doublePeriod;

                        if (outputInTicks > periodInTicks)
                        {
                            outputInTicks = doublePeriod - outputInTicks;
                        }
                    }
                    else
                    {
                        outputInTicks = outputInTicks % periodInTicks;
                        if (outputInTicks == 0)
                        {
                            outputInTicks = periodInTicks;  // If we are at the end, stick to the max value
                        }
                    }

                    if (accelRatio + decelRatio > 0)  // Now if we have acceleration, warp the point by the correct amount
                    {
                        double dpPeriod = (double)periodInTicks;
                        double inversePeriod = 1 / dpPeriod;
                        double halfMaxRate = 1 / (2 - accelRatio - decelRatio);  // Constants to simplify 
                        double t;

                        long accelEnd = (long)(dpPeriod * accelRatio);
                        long decelStart = periodInTicks - (long)(dpPeriod * decelRatio);

                        if (outputInTicks < accelEnd)  // We are in accel zone
                        {
                            t = (double)outputInTicks;
                            outputInTicks = (long)(halfMaxRate * inversePeriod * t * t / accelRatio);
                        }
                        else if (outputInTicks <= decelStart)  // We are in the linear zone
                        {
                            t = (double)outputInTicks;
                            outputInTicks = (long)(halfMaxRate * (2 * t - accelRatio));
                        }
                        else  // We are in decel zone
                        {
                            t = (double)(periodInTicks - outputInTicks);
                            outputInTicks = periodInTicks - (long)(halfMaxRate * inversePeriod * t * t / decelRatio);
                        }
                    }
                }
            }

            projection.InitializePoint(TimeSpan.FromTicks(outputInTicks));
        }


        /// <returns>
        /// Returns a collection which is the projection of this collection onto the defined periodic function.
        /// </returns>
        /// <remarks>
        /// The object on which this method is called is a timeline's parent's collection of intervals.
        /// The periodic collection passed via parameters describes the active/fill periods of the timeline.
        /// The output is the projection of (this) object using the parameter function of the timeline.
        /// 
        /// We assume this function is ONLY called when this collection overlaps the active zone.
        /// 
        /// The periodic function maps values from domain to range within its activation period of [beginTime, endTime);
        /// in the fill period [endTime, endTime+fillDuration) everything maps to a constant post-fill value, and outside of
        /// those periods every value maps to null.
        /// 
        /// The projection process can be described as three major steps:
        /// 
        /// (1) NORMALIZE this collection: offset the TIC's coordinates by BeginTime and scale by SpeedRatio.
        /// 
        /// (2) FOLD this collection.  This means we convert from parent-time coordinate space into the space of
        ///     a single simpleDuration for the child.  This is equivalent to "cutting up" the parent TIC into
        ///     equal-length segments (of length Period) and overlapping them -- taking their union.  This lets us
        ///     know exactly which values inside the simpleDuration we have reached on the child.  In the case of
        ///     autoreversed timelines, we do the folding similiar to folding a strip of paper -- alternating direction.
        /// 
        /// (3) WARP the resulting collection.  We now convert from simpleDuration domain coordinates into
        ///     coordinates in the range of the timeline function.  We do this by applying the "warping" effects of
        ///     acceleration, and deceleration.
        /// 
        /// In the special case of infinite simple duration, we essentially are done after performing NORMALIZE,
        /// because no periodicity or acceleration is present.
        /// 
        /// In the ultimate degenerate case of zero duration, we terminate early and project the zero point.
        /// 
        /// </remarks>
        /// <param name="projection">An empty output projection, passed by reference to allow TIC reuse.</param>
        /// <param name="beginTime">Begin time of the periodic function.</param>
        /// <param name="endTime">The end (expiration) time of the periodic function.  Null indicates positive infinity.</param>
        /// <param name="fillDuration">The fill time appended at the end of the periodic function.  Zero indicates no fill period.  Forever indicates infinite fill period.</param>
        /// <param name="period">Length of a single iteration in the periodic collection.</param>
        /// <param name="appliedSpeedRatio">Ratio by which to scale down the periodic collection.</param>
        /// <param name="accelRatio">Ratio of the length of the accelerating portion of the iteration.</param>
        /// <param name="decelRatio">Ratio of the length of the decelerating portion of the iteration.</param>
        /// <param name="isAutoReversed">Indicates whether reversed arcs should follow after forward arcs.</param>
        internal void ProjectOntoPeriodicFunction(ref TimeIntervalCollection projection,
                                                  TimeSpan beginTime, Nullable<TimeSpan> endTime,
                                                  Duration fillDuration, Duration period,
                                                  double appliedSpeedRatio, double accelRatio, double decelRatio,
                                                  bool isAutoReversed)
        {
            Debug.Assert(projection.IsEmpty);
            Debug.Assert(!_invertCollection);  // Make sure we never leave inverted mode enabled
            Debug.Assert(!endTime.HasValue || beginTime <= endTime);     // Ensure legitimate begin/end clipping parameters

            Debug.Assert(!IsEmptyOfRealPoints);  // We assume this function is ONLY called when this collection overlaps the active zone.  So we cannot be empty.
            Debug.Assert(!endTime.HasValue || endTime >= _nodeTime[0]);  // EndTime must come at or after our first node (it can be infinite)
            Debug.Assert(_nodeTime[_count - 1] >= beginTime);  // Our last node must come at least at begin time (since we must intersect the active period)
            Debug.Assert(endTime.HasValue || fillDuration == TimeSpan.Zero);   // Either endTime is finite, or it's infinite hence we cannot have any fill zone
            Debug.Assert(!period.HasTimeSpan || period.TimeSpan > TimeSpan.Zero || (endTime.HasValue && beginTime == endTime));  // Check the consistency of degenerate case where simple duration is zero; expiration time should equal beginTime
            Debug.Assert(!_nodeIsInterval[_count - 1]);  // We should not have an infinite domain set

            // We initially project all intervals into a single period of the timeline, creating a union of the projected segments.
            // Then we warp the time coordinates of the resulting TIC from domain to range, applying the effects of speed/accel/decel

            bool nullPoint = _containsNullPoint  // Start by projecting the null point directly, then check whether we fall anywhere outside of the active and fill period

             || _nodeTime[0] < beginTime  // If we intersect space before beginTime, or...

              || (endTime.HasValue && fillDuration.HasTimeSpan  // ...the active and fill periods don't stretch forever, and...
               && (_nodeTime[_count - 1] > endTime.Value + fillDuration.TimeSpan  // ...we intersect space after endTime+fill, or...
                || (_nodeTime[_count - 1] == endTime.Value + fillDuration.TimeSpan  // ...as we fall right onto the end of fill zone...
                 && _nodeIsPoint[_count - 1] && (endTime > beginTime || fillDuration.TimeSpan > TimeSpan.Zero))));  // ...we may have a point intersection with the stopped zone

            // Now consider the main scenarios:

            if (endTime.HasValue && beginTime == endTime)  // Degenerate case when our active period is a single point; project only the point
            {
                projection.InitializePoint(TimeSpan.Zero);
            }
            else  // The case of non-zero active duration
            {
                bool includeFillPeriod = !fillDuration.HasTimeSpan || fillDuration.TimeSpan > TimeSpan.Zero;  // This variable represents whether we have a non-zero fill zone

                if (period.HasTimeSpan)  // We have a finite TimeSpan period and non-zero activation duration
                {
                    TimeIntervalCollection tempCollection = new TimeIntervalCollection();

                    ProjectionNormalize(ref tempCollection, beginTime, endTime, includeFillPeriod, appliedSpeedRatio);

                    long periodInTicks = period.TimeSpan.Ticks;
                    Nullable<TimeSpan> activeDuration;
                    bool includeMaxPoint;

                    if (endTime.HasValue)
                    {
                        activeDuration = endTime.Value - beginTime;
                        includeMaxPoint = includeFillPeriod && (activeDuration.Value.Ticks % periodInTicks == 0);  // Fill starts at a boundary
                    }
                    else
                    {
                        activeDuration = null;
                        includeMaxPoint = false;
                    }

                    projection.EnsureAllocatedCapacity(_minimumCapacity);
                    tempCollection.ProjectionFold(ref projection, activeDuration, periodInTicks, isAutoReversed, includeMaxPoint);

                    if (accelRatio + decelRatio > 0)
                    {
                        projection.ProjectionWarp(periodInTicks, accelRatio, decelRatio);
                    }
                }
                else  // Infinite period degenerate case; we perform straight 1-1 linear mapping, offset by begin time and clipped
                {
                    ProjectionNormalize(ref projection, beginTime, endTime, includeFillPeriod, appliedSpeedRatio);
                }
            }

            projection._containsNullPoint = nullPoint;  // Ensure we have the null point properly set
        }

        /// <summary>
        /// Performs the NORMALIZE operation, as described in the comments to the general projection function.
        /// Clip begin and end times, normalize by beginTime, scale by speedRatio.
        /// </summary>
        /// <param name="projection">The normalized collection to create.</param>
        /// <param name="beginTime">Begin time of the active period for clipping.</param>
        /// <param name="endTime">End time of the active period for clipping.</param>
        /// <param name="speedRatio">The ratio by which to scale begin and end time.</param>
        /// <param name="includeFillPeriod">Whether a non-zero fill period exists.</param>
        private void ProjectionNormalize(ref TimeIntervalCollection projection,
                                         TimeSpan beginTime, Nullable<TimeSpan> endTime, bool includeFillPeriod, double speedRatio)
        {
            Debug.Assert(!IsEmptyOfRealPoints);
            Debug.Assert(projection.IsEmpty);
            
            projection.EnsureAllocatedCapacity(this._nodeTime.Length);

            this.MoveFirst();
            projection.MoveFirst();

            // Get to the non-clipped zone; we must overlap the active zone, so we should terminate at some point.
            while (!CurrentIsAtLastNode && NextNodeTime <= beginTime)
            {
                MoveNext();
            }

            if (CurrentNodeTime < beginTime)  // This means we have an interval clipped by beginTime
            {
                if (CurrentNodeIsInterval)
                {
                    projection._count++;
                    projection.CurrentNodeTime = TimeSpan.Zero;
                    projection.CurrentNodeIsPoint = true;
                    projection.CurrentNodeIsInterval = true;
                    projection.MoveNext();
                }
                this.MoveNext();
            }

            while(_current < _count && (!endTime.HasValue || CurrentNodeTime < endTime))  // Copy the main set of segments, transforming them
            {
                double timeOffset = (double)((this.CurrentNodeTime - beginTime).Ticks);
                
                projection._count++;
                projection.CurrentNodeTime = TimeSpan.FromTicks((long)(speedRatio * timeOffset));
                projection.CurrentNodeIsPoint = this.CurrentNodeIsPoint;
                projection.CurrentNodeIsInterval = this.CurrentNodeIsInterval;

                projection.MoveNext();
                this.MoveNext();
            }

            Debug.Assert(_current > 0);  // The only way _current could stay at zero is if the collection begins at (or past) the end of active period
            if (_current < _count  // We have an interval reaching beyond the active zone, clip that interval
             && (_nodeIsInterval[_current - 1]
              || (CurrentNodeTime == endTime.Value && CurrentNodeIsPoint && includeFillPeriod)))
            {
                Debug.Assert(endTime.HasValue && CurrentNodeTime >= endTime.Value);

                double timeOffset = (double)((endTime.Value - beginTime).Ticks);

                projection._count++;
                projection.CurrentNodeTime = TimeSpan.FromTicks((long)(speedRatio * timeOffset));
                projection.CurrentNodeIsPoint = includeFillPeriod && (CurrentNodeTime > endTime.Value || CurrentNodeIsPoint);
                projection.CurrentNodeIsInterval = false;
            }
        }

        /// <summary>
        /// Performs the FOLD operation, as described in the comments to the general projection function.
        /// We assume this method is only called with a finite, non-zero period length.
        /// The TIC is normalized so beginTime = 0.
        /// NOTE: projection should have allocated arrays.
        /// </summary>
        /// <param name="projection">The output projection.</param>
        /// <param name="activeDuration">The duration of the active period.</param>
        /// <param name="periodInTicks">The length of a simple duration in ticks.</param>
        /// <param name="isAutoReversed">Whether we have auto-reversing.</param>
        /// <param name="includeMaxPoint">Whether the fill zone forces the max point to be included.</param>
        private void ProjectionFold(ref TimeIntervalCollection projection, Nullable<TimeSpan> activeDuration,
                                    long periodInTicks, bool isAutoReversed, bool includeMaxPoint)
        {
            Debug.Assert(!IsEmptyOfRealPoints);  // The entire projection process assumes we are not empty (have an intersection with the active zone).
            Debug.Assert(periodInTicks > 0);  // We do not handle the degenerate case here.

            // Find the smallest n such that _nodeTime[n+1] > beginTime; if n is the last index, then consider _nodeTime[n+1] to be infinity
            MoveFirst();
            Debug.Assert(CurrentNodeTime >= TimeSpan.Zero);  // Verify that we are already clipped

            bool quitFlag = false;

            // As we walk, we maintain the invarant that the interval BEFORE _current is not included.
            // Otherwise we handle the interval and skip the interval's last node.

            // Process the remaining points and segments
            do
            {
                if (CurrentNodeIsInterval)  // Project the interval starting here
                {
                    quitFlag = ProjectionFoldInterval(ref projection, activeDuration, periodInTicks, isAutoReversed, includeMaxPoint);  // Project and break up the clipped segment
                    _current += NextNodeIsInterval ? 1 : 2;  // Step over the next node if it's merely the end of this interval
                }
                else  // This must be a lone point; the previous interval is no included by our invariant
                {
                    Debug.Assert(CurrentNodeIsPoint);
                    ProjectionFoldPoint(ref projection, activeDuration, periodInTicks, isAutoReversed, includeMaxPoint);
                    _current++;
                }
} while (!quitFlag && (_current < _count));
            // While we haven't run out of indices, and haven't moved past endTime
        }

        /// <summary>
        /// Take a single projection point and insert into the output collection.
        /// NOTE: projection should have allocated arrays.
        /// </summary>
        /// <param name="projection">The output collection.</param>
        /// <param name="activeDuration">The duration of the active period.</param>
        /// <param name="periodInTicks">The length of a simple duration in ticks.</param>
        /// <param name="isAutoReversed">Whether autoreversing is enabled</param>
        /// <param name="includeMaxPoint">Whether the fill zone forces the max point to be included.</param>
        private void ProjectionFoldPoint(ref TimeIntervalCollection projection, Nullable<TimeSpan> activeDuration,
                                         long periodInTicks, bool isAutoReversed, bool includeMaxPoint)
        {
            Debug.Assert(CurrentNodeIsPoint);  // We should only call this method when we project a legitimate point
            Debug.Assert(!CurrentNodeIsInterval);

            long currentProjection;
            if (isAutoReversed)  // Take autoreversing into account
            {
                long doublePeriod = periodInTicks << 1;
                currentProjection = CurrentNodeTime.Ticks % doublePeriod;

                if (currentProjection > periodInTicks)
                {
                    currentProjection = doublePeriod - currentProjection;
                }
            }
            else  // No autoReversing
            {
                if (includeMaxPoint && activeDuration.HasValue && CurrentNodeTime == activeDuration)
                {
                    currentProjection = periodInTicks;  // Exceptional end case: we are exactly at the last point
                }
                else
                {
                    currentProjection = CurrentNodeTime.Ticks % periodInTicks;
                }
            }

            projection.MergePoint(TimeSpan.FromTicks(currentProjection));
        }

        /// <summary>
        /// Take a single projection segment [CurrentNodeTime, NextNodeTime], break it into parts and merge the
        /// folded parts into this collection.
        /// NOTE: the TIC is normalized so beginTime = TimeSpan.Zero and we are already clipped.
        /// NOTE: projection should have allocated arrays.
        /// </summary>
        /// <param name="projection">The output projection.</param>
        /// <param name="activeDuration">The duration of the active period.</param>
        /// <param name="periodInTicks">The length of a simple duration in ticks.</param>
        /// <param name="isAutoReversed">Whether autoreversing is enabled</param>
        /// <param name="includeMaxPoint">Whether the fill zone forces the max point to be included.</param>
        private bool ProjectionFoldInterval(ref TimeIntervalCollection projection, Nullable<TimeSpan> activeDuration,
                                            long periodInTicks, bool isAutoReversed, bool includeMaxPoint)
        {
            // Project the begin point for the segment, then look if we are autoreversing or not.
            long intervalLength = (NextNodeTime - CurrentNodeTime).Ticks;
            long timeBeforeNextPeriod, currentProjection;

            // Now see how the segment falls across periodic boundaries:
            // Case 1: segment stretches across a full period (we can exit early, since we cover the entire range of values)
            // Case 2: NON-AUTEREVERSED: segment stretches across two partial periods (we need to split into two segments and insert them into the projection)
            // Case 2: AUTOREVERSED: we need to pick the larger half of the partial period and project only that half, since it fully overlaps the other.
            // Case 3: segment is fully contained within a single period (just add the segment into the projection)
            // These cases are handled very differently for AutoReversing and non-AutoReversing timelines.

            if (isAutoReversed)  // In the autoreversed case, we "fold" the segment onto itself and eliminate the redundant parts
            {
                bool beginOnReversingArc;
                long doublePeriod = periodInTicks << 1;
                currentProjection = CurrentNodeTime.Ticks % doublePeriod;

                if (currentProjection < periodInTicks)  // We are on a forward-moving segment
                {
                    beginOnReversingArc = false;
                    timeBeforeNextPeriod = periodInTicks - currentProjection;
                }
                else  // We are on a reversing segment, adjust the values accordingly
                {
                    beginOnReversingArc = true;
                    currentProjection = doublePeriod - currentProjection;
                    timeBeforeNextPeriod = currentProjection;
                }

                Debug.Assert(timeBeforeNextPeriod > 0);

                long timeAfterNextPeriod = intervalLength - timeBeforeNextPeriod;  // How much of our interval protrudes into the next period(s); this may be negative if we don't reach it.
                // See which part of the segment -- before or after part -- "dominates" when we fold them unto each other.
                if (timeAfterNextPeriod > 0)  // Case 1 or 2: we reach into the next period but don't know if we completely cover it
                {
                    bool collectionIsSaturated;
                        
                    if (timeBeforeNextPeriod >= timeAfterNextPeriod)  // Before "dominates"
                    {
                        bool includeTime = CurrentNodeIsPoint;

                        if (timeBeforeNextPeriod == timeAfterNextPeriod)  // Corner case where before and after overlap exactly, find the IsPoint union
                        {
                            includeTime = includeTime || NextNodeIsPoint;
                        }

                        if (beginOnReversingArc)
                        {
                            projection.MergeInterval(TimeSpan.Zero,                         true,
                                                     TimeSpan.FromTicks(currentProjection), includeTime);
                            collectionIsSaturated = includeTime && (currentProjection == periodInTicks);
                        }
                        else
                        {
                            projection.MergeInterval(TimeSpan.FromTicks(currentProjection), includeTime,
                                                     TimeSpan.FromTicks(periodInTicks),     true);
                            collectionIsSaturated = includeTime && (currentProjection == 0);
                        }
                    }
                    else  // After "dominates"
                    {
                        if (beginOnReversingArc)
                        {
                            long clippedTime = timeAfterNextPeriod < periodInTicks ? timeAfterNextPeriod : periodInTicks;
                            
                            projection.MergeInterval(TimeSpan.Zero,                   true,
                                                     TimeSpan.FromTicks(clippedTime), NextNodeIsPoint);
                            collectionIsSaturated = NextNodeIsPoint && (clippedTime == periodInTicks);
                        }
                        else
                        {
                            long clippedTime = timeAfterNextPeriod < periodInTicks ? periodInTicks - timeAfterNextPeriod : 0;

                            projection.MergeInterval(TimeSpan.FromTicks(clippedTime),   NextNodeIsPoint,
                                                     TimeSpan.FromTicks(periodInTicks), true);
                            collectionIsSaturated = NextNodeIsPoint && (clippedTime == 0);
                        }
                    }
                    return collectionIsSaturated;  // See if we just saturated the collection
                }
                else  // Case 3: timeAfterNextPeriod < 0, we are fully contained in the current period
                {
                    // No need to split anything, insert the interval directly
                    if (beginOnReversingArc)  // Here the nodes are reversed
                    {
                        projection.MergeInterval(TimeSpan.FromTicks(currentProjection - intervalLength), NextNodeIsPoint,
                                                 TimeSpan.FromTicks(currentProjection),                  CurrentNodeIsPoint);
                    }
                    else
                    {
                        projection.MergeInterval(TimeSpan.FromTicks(currentProjection),                  CurrentNodeIsPoint,
                                                 TimeSpan.FromTicks(currentProjection + intervalLength), NextNodeIsPoint);
                    }
                    return false;  // Keep computing the projection
                }
            }
            else  // No AutoReversing
            {                
                currentProjection = CurrentNodeTime.Ticks % periodInTicks;
                timeBeforeNextPeriod = periodInTicks - currentProjection;

                // The only way to get 0 is if we clipped by endTime which equals CurrentNodeTime, which should not have been allowed
                Debug.Assert(intervalLength > 0);

                if (intervalLength > periodInTicks)  // Case 1. We may stretch across a whole arc, even if we start from the end and wrap back around
                {
                    // Quickly transform the collection into a saturated collection
                    projection._nodeTime[0] = TimeSpan.Zero;
                    projection._nodeIsPoint[0] = true;
                    projection._nodeIsInterval[0] = true;

                    projection._nodeTime[1] = TimeSpan.FromTicks(periodInTicks);
                    projection._nodeIsPoint[1] = includeMaxPoint;
                    projection._nodeIsInterval[1] = false;

                    _count = 2;
                    return true;  // Bail early, we have the result ready
                }
                else if (intervalLength >= timeBeforeNextPeriod)  // Case 2. We stretch until the next period begins (but not long enough to cover the length of a full period)
                {
                    // Split the segment into two projected segments by wrapping around the period boundary
                    projection.MergeInterval(TimeSpan.FromTicks(currentProjection),                     CurrentNodeIsPoint,
                                             TimeSpan.FromTicks(periodInTicks),                         false);
                    if (intervalLength > timeBeforeNextPeriod)  // See if we have a legitimate interval in the second clipped part
                    {
                        projection.MergeInterval(TimeSpan.Zero,                                             true,
                                                 TimeSpan.FromTicks(intervalLength - timeBeforeNextPeriod), NextNodeIsPoint);
                    }
                    else if (NextNodeIsPoint)  // We only seem to have a point, wrapped around at zero (or in the exceptional case, at the max)
                    {
                        if (includeMaxPoint && activeDuration.HasValue && NextNodeTime == activeDuration)  // Exceptional end case: we are exactly at the last point
                        {
                            projection.MergePoint(TimeSpan.FromTicks(periodInTicks));
                        }
                        else
                        {
                            projection.MergePoint(TimeSpan.Zero);
                        }
                    }
                    return false;  // Keep computing the projection
                }
                else  // Case 3: We fall within a single period
                {
                    // No need to split anything, insert the interval directly
                    projection.MergeInterval(TimeSpan.FromTicks(currentProjection),                    CurrentNodeIsPoint,
                                             TimeSpan.FromTicks(currentProjection + intervalLength),   NextNodeIsPoint);
                    return false;  // Keep computing the projection
                }
            }
        }

        /// <summary>
        /// Merges a point into this collection so it becomes the union of itself and the point.
        /// Consequentialy, this does nothing if the point is already a subset of the collection;
        /// Otherwise adjusts the collection so that the result obeys the rules of a proper TIC.
        /// NOTE: _current will shift so as to be the same distance from the end as before.
        /// </summary>
        /// <param name="point">The point to merge.</param>
        private void MergePoint(TimeSpan point)
        {
            int index = Locate(point);

            if (index >= 0 && _nodeTime[index] == point)  // Point coincides with an existing node 
            {
                if(!_nodeIsPoint[index])  // The node is not already in the TIC
                {
                    // See if we need to insert the node, or cancel out the node when it "saturates" an interval-point-interval segment
                    if (index == 0 || !_nodeIsInterval[index - 1] || !_nodeIsInterval[index])
                    {
                        _nodeIsPoint[index] = true;
                    }
                    else  // Else we should cancel the node as it is redundant (===O=== saturated case)
                    {
                        for (int n = index; n + 1 < _count; n++)  // Shift over the contents
                        {
                            _nodeTime[n]       = _nodeTime[n + 1];
                            _nodeIsPoint[n]    = _nodeIsPoint[n + 1];
                            _nodeIsInterval[n] = _nodeIsInterval[n + 1];
                        }
                        _count--;
                    }
                }
            }
            else if (index == -1 || !_nodeIsInterval[index])  // Point falls within the interior of a non-included interval
            {
                Debug.Assert(index == -1 || _nodeTime[index] < point);

                // Then we need to insert a point into the collection
                EnsureAllocatedCapacity(_count + 1);

                for (int n = _count - 1; n > index; n--)  // Shift over the contents
                {
                    _nodeTime[n + 1]       = _nodeTime[n];
                    _nodeIsPoint[n + 1]    = _nodeIsPoint[n];
                    _nodeIsInterval[n + 1] = _nodeIsInterval[n];
                }
                _nodeTime[index + 1]       = point;  // Insert the node
                _nodeIsPoint[index + 1]    = true;
                _nodeIsInterval[index + 1] = false;

                _count++;
            }
        }

        /// <summary>
        /// Merges an interval into this collection so it becomes the union of itself and the interval.
        /// Consequentialy, this does nothing if the interval is already a subset of the collection;
        /// Otherwise adjusts the collection so that the result obeys the rules of a proper TIC.
        /// </summary>
        /// <param name="from">Start of the interval.</param>
        /// <param name="includeFrom">Whether the start point is included.</param>
        /// <param name="to">End of the interval.</param>
        /// <param name="includeTo">Whether the end point is included.</param>
        private void MergeInterval(TimeSpan from, bool includeFrom,
                                   TimeSpan to,   bool includeTo)
        {
            Debug.Assert(from < to);  // Our code should never call MergeInterval for a point or reversed interval

            if (IsEmptyOfRealPoints)  // We have no points yet, simply create a new collection with those points
            {
                _nodeTime[0] = from;
                _nodeIsPoint[0] = includeFrom;
                _nodeIsInterval[0] = true;

                _nodeTime[1] = to;
                _nodeIsPoint[1] = includeTo;
                _nodeIsInterval[1] = false;

                _count = 2;
            }
            else  // We are not empty, hence there must be existing intervals allocated and assigned
            {
                Debug.Assert(_nodeTime.Length >= _minimumCapacity);  // Assert that we indeed have memory allocated

                int fromIndex = Locate(from);  // Find the nearest nodes to the left of from and to (possibly equal)
                int toIndex   = Locate(to);

                // From a structural standpoint, we do the following:
                //  before  ----o---o----?----o---o---?----o----  (? means there may or may not be a node here)
                //                       F            T
                //  after   ----o---o----?------------?----o----  (? means the node may be added, kept, or removed here)

                // The array reshuffling takes place as following:
                // 1) Check if more memory is needed, then dynamically resize and move the contents to new arrays
                // 2) Perform in-place blitting depending whether we contract or expand the array

                bool insertNodeAtFrom = false;
                bool insertNodeAtTo   = false;

                int netIncreaseInNodes = fromIndex - toIndex;  // The default is we remove all the "intermediate" nodes
                int nextInsertionIndex = fromIndex + 1;  // Place to begin inserting new nodes if needed; by default start from [fromIndex+1]
                int lastNodeToDelete   = toIndex;  // By default, delete nodes up through [toIndex]

                // If FROM falls within an interval, and we don't have IntervalIncluded, create a node here.
                //   Otherwise don't create that node.
                // Else FROM coincides with a node; if we have PreviousIntervalIncluded && (CoincidingNode||includeStart), cancel the saturated node.
                //   Otherwise keep that node.

                if (fromIndex == -1 || _nodeTime[fromIndex] < from)  // We don't fall exactly onto a preexisting node
                {
                    // Keep the node at fromIndex; see if we need to insert a new node

                    if (fromIndex == -1 || !_nodeIsInterval[fromIndex])
                    {
                        insertNodeAtFrom = true;
                        netIncreaseInNodes++;  // We previously assumed we don't insert any new nodes
                    }
                }
                else  // We fall exactly onto a preexisting node; in this case, it is redundant to insert another node here.
                {
                    Debug.Assert(_nodeTime[fromIndex] == from);

                    if (fromIndex > 0 && _nodeIsInterval[fromIndex - 1]  // Delete the node at fromIndex, it will become saturated
                      && (includeFrom || _nodeIsPoint[fromIndex]))
                    {
                        netIncreaseInNodes--;  // We previously assumed that we would NOT delete the node at fromIndex
                        nextInsertionIndex--;
                    }
                    else  // Keep the node at fromIndex
                    {
                        _nodeIsPoint[fromIndex] = includeFrom || _nodeIsPoint[fromIndex];  // Update the node's IsPoint status
                    }
                }

                // If TO falls within an interval, and we don't have IntervalIncluded, create a node here.
                //   Otherwise don't create that node.
                // Else TO coincides with a node; if we have (IncludeCoincidingNode||includeEnd) && IntervalIncluded, allow the node to be deleted
                //   Otherwise arrange to keep that node (this is not what we do by default).
                if (toIndex == -1 || _nodeTime[toIndex] < to)  // We don't fall exactly onto a preexisting node
                {
                    // The previous node is strictly smaller, so it is redundant and we allow it to be deleted.
                    // We don't decrement netIncreaseInNodes here because we assumed that we delete the node at toIndex

                    if (toIndex == -1 || !_nodeIsInterval[toIndex])  // If we aren't inside an included interval, insert a node
                    {
                        insertNodeAtTo = true;
                        netIncreaseInNodes++;  // We previously assumed we don't insert any new nodes
                    }
                }
                else  // We fall exactly onto a preexisting node; in this case, it is redundant to insert another node here.
                {
                    Debug.Assert(_nodeTime[toIndex] == to);
                    Debug.Assert(fromIndex < toIndex);

                    // The default is we delete the node at toIndex, unless it does not saturate the resulting TIC.

                    if (!_nodeIsInterval[toIndex] || (!includeTo && !_nodeIsPoint[toIndex])) // Keep the node at toIndex, it is not going to be saturated
                    {
                        // We previously assumed that we WOULD delete the node at toIndex, now it turns out we should keep it
                        netIncreaseInNodes++;
                        lastNodeToDelete--;

                        _nodeIsPoint[toIndex] = includeTo || _nodeIsPoint[toIndex];  // Update the node's IsPoint status
                    }
                }

                // Eliminate all nodes with index FROM <= index <= TOINDEX, observing deletion rules:
                //
                //        Index:   fromIndex==toIndex
                // ShouldDelete:       no(default)
                //
                //        Index:    fromIndex      toIndex
                // ShouldDelete:   no(default)   yes(default)
                //
                //        Index:    fromIndex    a    b    c     toIndex
                // ShouldDelete:   no(default)  yes  yes  yes  yes(default)
                //

                // The effect of the move on the array is that we make the transition:
                //   AAA[DDDD]BBB  -->  AAA[II]BBB
                // Where we can have any number of D's (deleted nodes) and from 0 to 2 I's (inserted nodes).
                // What we need to find is how many A's and B's we have, and which way to shift them.

                Debug.Assert(_count + netIncreaseInNodes >= 2);  // We should never shrink past size 2

                if (netIncreaseInNodes > 0)  // We need to grow the array
                {
                    EnsureAllocatedCapacity(_count + netIncreaseInNodes);  // Make sure we have enough space allocated
                    for (int n = _count - 1; n > lastNodeToDelete; n--)
                    {
                        _nodeTime[n + netIncreaseInNodes]       = _nodeTime[n];
                        _nodeIsPoint[n + netIncreaseInNodes]    = _nodeIsPoint[n];
                        _nodeIsInterval[n + netIncreaseInNodes] = _nodeIsInterval[n];
                    }
                }
                else if (netIncreaseInNodes < 0)  // We need to shrink the array
                {
                    // Copy the elements
                    for (int n = lastNodeToDelete + 1; n < _count; n++)
                    {
                        _nodeTime[n + netIncreaseInNodes]       = _nodeTime[n];  // Note that netIncreaseInNodes is negative here
                        _nodeIsPoint[n + netIncreaseInNodes]    = _nodeIsPoint[n];
                        _nodeIsInterval[n + netIncreaseInNodes] = _nodeIsInterval[n];
                    }
                }

                _count += netIncreaseInNodes;  // Update the array size

                if (insertNodeAtFrom)
                {
                    _nodeTime[nextInsertionIndex]       = from;
                    _nodeIsPoint[nextInsertionIndex]    = includeFrom;
                    _nodeIsInterval[nextInsertionIndex] = true;  // We are inserting an interval, so this is true

                    nextInsertionIndex++;
                }

                if (insertNodeAtTo)
                {
                    _nodeTime[nextInsertionIndex]       = to;
                    _nodeIsPoint[nextInsertionIndex]    = includeTo;
                    _nodeIsInterval[nextInsertionIndex] = false;  // We are terminating an interval, so this is false
                }
            }
        }


        private void EnsureAllocatedCapacity(int requiredCapacity)
        {
            if (_nodeTime == null)
            {
                Debug.Assert(_nodeIsPoint == null);
                Debug.Assert(_nodeIsInterval == null);

                _nodeTime = new TimeSpan[requiredCapacity];
                _nodeIsPoint = new bool[requiredCapacity];
                _nodeIsInterval = new bool[requiredCapacity];
            }
            else if (_nodeTime.Length < requiredCapacity)  // We may need to grow by up to 2 units
            {
                Debug.Assert(_nodeIsPoint != null);
                Debug.Assert(_nodeIsInterval != null);

                int newCapacity = _nodeTime.Length << 1;  // Dynamically grow by a factor of 2

                TimeSpan[] newNodeTime   = new TimeSpan[newCapacity];
                bool[] newNodeIsPoint    = new bool[newCapacity];
                bool[] newNodeIsInterval = new bool[newCapacity];

                for (int n = 0; n < _count; n++)
                {
                    newNodeTime[n]       = _nodeTime[n];
                    newNodeIsPoint[n]    = _nodeIsPoint[n];
                    newNodeIsInterval[n] = _nodeIsInterval[n];
                }

                _nodeTime       = newNodeTime;
                _nodeIsPoint    = newNodeIsPoint;
                _nodeIsInterval = newNodeIsInterval;
            }
        }


        /// <summary>
        /// Apply the effects of Accel, Decel to the nodes in this TIC.
        /// This should ONLY get called when the period in finite and non-zero, and accel+decel > 0.
        /// </summary>
        /// <param name="periodInTicks">The length of a simple duration in ticks.</param>
        /// <param name="accelRatio">The accelerating fraction of the simple duration.</param>
        /// <param name="decelRatio">The decelerating fraction of the simple duration.</param>
        private void ProjectionWarp(long periodInTicks, double accelRatio, double decelRatio)
        {
            Debug.Assert(periodInTicks > 0);
            Debug.Assert(accelRatio + decelRatio > 0);

            double dpPeriod = (double)periodInTicks;
            double inversePeriod = 1 / dpPeriod;
            double halfMaxRate = 1 / (2 - accelRatio - decelRatio);  // Constants to simplify 

            TimeSpan accelEnd = TimeSpan.FromTicks((long)(dpPeriod * accelRatio));
            TimeSpan decelStart = TimeSpan.FromTicks(periodInTicks - (long)(dpPeriod * decelRatio));

            double t;  // Current progress, which ranges from 0 to 1

            MoveFirst();

            // Perform accel warping
            while (_current < _count && CurrentNodeTime < accelEnd)
            {
                t = (double)_nodeTime[_current].Ticks;
                _nodeTime[_current] = TimeSpan.FromTicks((long)(halfMaxRate * inversePeriod * t * t / accelRatio));
                MoveNext();
            }

            // Perform linear zone warping
            while (_current < _count && CurrentNodeTime <= decelStart)  // We bias the edge points towards the simpler linear computation, which yields the same result
            {
                t = (double)_nodeTime[_current].Ticks;
                _nodeTime[_current] = TimeSpan.FromTicks((long)(halfMaxRate * (2 * t - (accelRatio * dpPeriod))));
                MoveNext();
            }

            // Perform decel warping
            while (_current < _count)
            {
                t = (double)(periodInTicks - _nodeTime[_current].Ticks);  // We actually use the complement from 100% progress
                _nodeTime[_current] = TimeSpan.FromTicks(periodInTicks - (long)(halfMaxRate * inversePeriod * t * t / decelRatio));
                MoveNext();
            }
        }

#if TEST_TIMING_CODE
        /// <summary>
        /// Creates several collections and runs test operations on them
        /// </summary>
        static internal void RunDiagnostics()
        {
            TimeIntervalCollection t = TimeIntervalCollection.CreatePoint(TimeSpan.FromSeconds(3.85));
            TimeIntervalCollection t2;

            // Case 1      --x--*-----
            t2 = TimeIntervalCollection.CreatePoint(TimeSpan.FromSeconds(3.70));
            Debug.Assert(!t.Intersects(t2));
            Debug.Assert(!t2.Intersects(t));
            Debug.Assert(!t.Contains(TimeSpan.FromSeconds(3.70)));
            Debug.Assert(t.IntersectsInverseOf(t2));
            Debug.Assert(t2.IntersectsInverseOf(t));

            // Empty
            Debug.Assert(!t2.IntersectsPeriodicCollection(TimeSpan.FromSeconds(2.0),
                                                         TimeSpan.FromSeconds(1.0), 1, 0, 0, false));
            // Accel only
            Debug.Assert(!t2.IntersectsPeriodicCollection(TimeSpan.FromSeconds(2.0),
                                                         TimeSpan.FromSeconds(1.0), 1, 0.3, 0, false));
            // Decel only
            Debug.Assert(t2.IntersectsPeriodicCollection(TimeSpan.FromSeconds(2.0),
                                                         TimeSpan.FromSeconds(1.0), 1, 0, 0.3, false));
            // Accel+decel
            Debug.Assert(t2.IntersectsPeriodicCollection(TimeSpan.FromSeconds(2.0),
                                                         TimeSpan.FromSeconds(1.0), 1, 0.1, 0.3, false));
            // Accel+decel+autoreverse (boundary case 1)
            Debug.Assert(!t2.IntersectsPeriodicCollection(TimeSpan.FromSeconds(2.0),
                                                         TimeSpan.FromSeconds(1.0), 1, 0.3, 0.1, true));
            // Accel+decel+autoreverse (boundary case 2)
            Debug.Assert(t2.IntersectsPeriodicCollection(TimeSpan.FromSeconds(2.0),
                                                         TimeSpan.FromSeconds(1.0), 1, 0.301, 0.1, true));
            // Accel+decel+autoreverse disabled for check
            Debug.Assert(!t2.IntersectsPeriodicCollection(TimeSpan.FromSeconds(2.0),
                                                         TimeSpan.FromSeconds(1.0), 1, 0.3, 0.1, false));
            // Insufficient decel to provoke intersection
            Debug.Assert(!t2.IntersectsPeriodicCollection(TimeSpan.FromSeconds(2.0),
                                                         TimeSpan.FromSeconds(1.0), 1, 0.1, 0.2, false));
            // Autoreverse-only
            Debug.Assert(t2.IntersectsPeriodicCollection(TimeSpan.FromSeconds(1.7),
                                                         TimeSpan.FromSeconds(1.0), 1, 0, 0, true));
            // Large decel zone
            Debug.Assert(t2.IntersectsPeriodicCollection(TimeSpan.FromSeconds(2.0),
                                                         TimeSpan.FromSeconds(1.0), 1, 0.1, 0.5, false));

            // Case 2      -----x-----
            t2 = TimeIntervalCollection.CreatePoint(TimeSpan.FromSeconds(3.85));
            Debug.Assert(t.Intersects(t2));
            Debug.Assert(t2.Intersects(t));
            Debug.Assert(t.Contains(TimeSpan.FromSeconds(3.85)));
            Debug.Assert(!t.IntersectsInverseOf(t2));
            Debug.Assert(!t2.IntersectsInverseOf(t));

            // Case 3      -----*--x--
            t2 = TimeIntervalCollection.CreatePoint(TimeSpan.FromSeconds(3.95));
            Debug.Assert(!t.Intersects(t2));
            Debug.Assert(!t2.Intersects(t));
            Debug.Assert(!t.Contains(TimeSpan.FromSeconds(3.95)));
            Debug.Assert(t.IntersectsInverseOf(t2));
            Debug.Assert(t2.IntersectsInverseOf(t));

            t.Clear();

            Debug.Assert(!t.Contains(TimeSpan.FromSeconds(3.70)));  // No intersection with empty set
            Debug.Assert(!t.Contains(TimeSpan.FromSeconds(3.85)));
            Debug.Assert(!t.Contains(TimeSpan.FromSeconds(3.95)));

            t = TimeIntervalCollection.CreateClosedOpenInterval(TimeSpan.FromSeconds(3.85), TimeSpan.FromSeconds(3.95));

            // Case 1      --x--*=====.-----
            t2 = TimeIntervalCollection.CreatePoint(TimeSpan.FromSeconds(3.7));

            Debug.Assert(!t.Intersects(t2));
            Debug.Assert(!t2.Intersects(t));
            Debug.Assert(!t.Contains(TimeSpan.FromSeconds(3.70)));
            Debug.Assert(t.IntersectsInverseOf(t2));
            Debug.Assert(t2.IntersectsInverseOf(t));

            // Case 2      -----x=====.-----
            t2 = TimeIntervalCollection.CreatePoint(TimeSpan.FromSeconds(3.85));

            Debug.Assert(t.Intersects(t2));
            Debug.Assert(t2.Intersects(t));
            Debug.Assert(t.Contains(TimeSpan.FromSeconds(3.85)));
            Debug.Assert(t.IntersectsInverseOf(t2));
            Debug.Assert(!t2.IntersectsInverseOf(t));

            // Case 3      -----*==x==.-----
            t2 = TimeIntervalCollection.CreatePoint(TimeSpan.FromSeconds(3.90));

            Debug.Assert(t.Intersects(t2));
            Debug.Assert(t2.Intersects(t));
            Debug.Assert(t.Contains(TimeSpan.FromSeconds(3.90)));
            Debug.Assert(t.IntersectsInverseOf(t2));
            Debug.Assert(!t2.IntersectsInverseOf(t));

            // Case 4      -----*=====x-----
            t2 = TimeIntervalCollection.CreatePoint(TimeSpan.FromSeconds(3.95));

            Debug.Assert(!t.Intersects(t2));
            Debug.Assert(!t2.Intersects(t));
            Debug.Assert(!t.Contains(TimeSpan.FromSeconds(3.95)));
            Debug.Assert(t.IntersectsInverseOf(t2));
            Debug.Assert(t2.IntersectsInverseOf(t));

            // Case 5      -----*=====.--x--
            t2 = TimeIntervalCollection.CreatePoint(TimeSpan.FromSeconds(4.00));

            Debug.Assert(!t.Intersects(t2));
            Debug.Assert(!t2.Intersects(t));
            Debug.Assert(!t.Contains(TimeSpan.FromSeconds(4.00)));
            Debug.Assert(t.IntersectsInverseOf(t2));
            Debug.Assert(t2.IntersectsInverseOf(t));

            //// Case 1      --x--*=====.-----    (x is the starting point for t2)

            t2 = TimeIntervalCollection.CreateClosedOpenInterval(TimeSpan.FromSeconds(3.7), TimeSpan.FromSeconds(3.75));

            Debug.Assert(!t.Intersects(t2));
            Debug.Assert(!t2.Intersects(t));
            Debug.Assert(!t2.Intersects(TimeSpan.FromSeconds(3.85), TimeSpan.FromSeconds(3.95)));
            Debug.Assert(t.IntersectsInverseOf(t2));
            Debug.Assert(t2.IntersectsInverseOf(t));

            t2 = TimeIntervalCollection.CreateClosedOpenInterval(TimeSpan.FromSeconds(3.7), TimeSpan.FromSeconds(3.85));

            Debug.Assert(!t.Intersects(t2));
            Debug.Assert(!t2.Intersects(t));
            Debug.Assert(!t2.Intersects(TimeSpan.FromSeconds(3.85), TimeSpan.FromSeconds(3.95)));
            Debug.Assert(t.IntersectsInverseOf(t2));
            Debug.Assert(t2.IntersectsInverseOf(t));

            t2 = TimeIntervalCollection.CreateClosedOpenInterval(TimeSpan.FromSeconds(3.7), TimeSpan.FromSeconds(3.90));

            Debug.Assert(t.Intersects(t2));
            Debug.Assert(t2.Intersects(t));
            Debug.Assert(t2.Intersects(TimeSpan.FromSeconds(3.85), TimeSpan.FromSeconds(3.95)));
            Debug.Assert(t.IntersectsInverseOf(t2));
            Debug.Assert(t2.IntersectsInverseOf(t));

            t2 = TimeIntervalCollection.CreateClosedOpenInterval(TimeSpan.FromSeconds(3.7), TimeSpan.FromSeconds(3.95));

            Debug.Assert(t.Intersects(t2));
            Debug.Assert(t2.Intersects(t));
            Debug.Assert(t2.Intersects(TimeSpan.FromSeconds(3.85), TimeSpan.FromSeconds(3.95)));
            Debug.Assert(!t.IntersectsInverseOf(t2));
            Debug.Assert(t2.IntersectsInverseOf(t));

            t2 = TimeIntervalCollection.CreateClosedOpenInterval(TimeSpan.FromSeconds(3.7), TimeSpan.FromSeconds(4.0));

            Debug.Assert(t.Intersects(t2));
            Debug.Assert(t2.Intersects(t));
            Debug.Assert(t2.Intersects(TimeSpan.FromSeconds(3.85), TimeSpan.FromSeconds(3.95)));
            Debug.Assert(!t.IntersectsInverseOf(t2));
            Debug.Assert(t2.IntersectsInverseOf(t));

            //// Case 2      -----x=====.-----    (x is the starting point for t2)

            t2 = TimeIntervalCollection.CreateClosedOpenInterval(TimeSpan.FromSeconds(3.85), TimeSpan.FromSeconds(3.90));

            Debug.Assert(t.Intersects(t2));
            Debug.Assert(t2.Intersects(t));
            Debug.Assert(t2.Intersects(TimeSpan.FromSeconds(3.85), TimeSpan.FromSeconds(3.95)));
            Debug.Assert(t.IntersectsInverseOf(t2));
            Debug.Assert(!t2.IntersectsInverseOf(t));

            t2 = TimeIntervalCollection.CreateClosedOpenInterval(TimeSpan.FromSeconds(3.85), TimeSpan.FromSeconds(3.95));

            Debug.Assert(t.Intersects(t2));
            Debug.Assert(t2.Intersects(t));
            Debug.Assert(t2.Intersects(TimeSpan.FromSeconds(3.85), TimeSpan.FromSeconds(3.95)));
            Debug.Assert(!t.IntersectsInverseOf(t2));
            Debug.Assert(!t2.IntersectsInverseOf(t));

            t2 = TimeIntervalCollection.CreateClosedOpenInterval(TimeSpan.FromSeconds(3.85), TimeSpan.FromSeconds(4.0));

            Debug.Assert(t.Intersects(t2));
            Debug.Assert(t2.Intersects(t));
            Debug.Assert(t2.Intersects(TimeSpan.FromSeconds(3.85), TimeSpan.FromSeconds(3.95)));
            Debug.Assert(!t.IntersectsInverseOf(t2));
            Debug.Assert(t2.IntersectsInverseOf(t));

            // Case 3      -----*==x==.-----    (x is the starting point for t2)

            t2 = TimeIntervalCollection.CreateClosedOpenInterval(TimeSpan.FromSeconds(3.87), TimeSpan.FromSeconds(3.90));

            Debug.Assert(t.Intersects(t2));
            Debug.Assert(t2.Intersects(t));
            Debug.Assert(t2.Intersects(TimeSpan.FromSeconds(3.85), TimeSpan.FromSeconds(3.95)));
            Debug.Assert(t.IntersectsInverseOf(t2));
            Debug.Assert(!t2.IntersectsInverseOf(t));

            t2 = TimeIntervalCollection.CreateClosedOpenInterval(TimeSpan.FromSeconds(3.87), TimeSpan.FromSeconds(3.95));

            Debug.Assert(t.Intersects(t2));
            Debug.Assert(t2.Intersects(t));
            Debug.Assert(t2.Intersects(TimeSpan.FromSeconds(3.85), TimeSpan.FromSeconds(3.95)));
            Debug.Assert(t.IntersectsInverseOf(t2));
            Debug.Assert(!t2.IntersectsInverseOf(t));

            t2 = TimeIntervalCollection.CreateClosedOpenInterval(TimeSpan.FromSeconds(3.87), TimeSpan.FromSeconds(4.0));

            Debug.Assert(t.Intersects(t2));
            Debug.Assert(t2.Intersects(t));
            Debug.Assert(t2.Intersects(TimeSpan.FromSeconds(3.85), TimeSpan.FromSeconds(3.95)));
            Debug.Assert(t.IntersectsInverseOf(t2));
            Debug.Assert(t2.IntersectsInverseOf(t));

            // Case 4      -----*=====x-----    (x is the starting point for t2)

            t2 = TimeIntervalCollection.CreateClosedOpenInterval(TimeSpan.FromSeconds(3.95), TimeSpan.FromSeconds(4.0));

            Debug.Assert(!t.Intersects(t2));
            Debug.Assert(!t2.Intersects(t));
            Debug.Assert(!t2.Intersects(TimeSpan.FromSeconds(3.85), TimeSpan.FromSeconds(3.95)));
            Debug.Assert(t.IntersectsInverseOf(t2));
            Debug.Assert(t2.IntersectsInverseOf(t));

            // Case 5      -----*=====.--x--    (x is the starting point for t2)

            t2 = TimeIntervalCollection.CreateClosedOpenInterval(TimeSpan.FromSeconds(3.98), TimeSpan.FromSeconds(4.0));

            Debug.Assert(!t.Intersects(t2));
            Debug.Assert(!t2.Intersects(t));
            Debug.Assert(!t2.Intersects(TimeSpan.FromSeconds(3.85), TimeSpan.FromSeconds(3.95)));
            Debug.Assert(t.IntersectsInverseOf(t2));
            Debug.Assert(t2.IntersectsInverseOf(t));

            // Merge testing
            t = TimeIntervalCollection.CreateClosedOpenInterval(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(5.5));
            t.MergePoint(TimeSpan.FromSeconds(8));
            t.MergePoint(TimeSpan.FromSeconds(12));
            t.MergeInterval(TimeSpan.FromSeconds(14.5), true, TimeSpan.FromSeconds(19), true);

            //t2 = t.ProjectOntoPeriodicFunction(beginTime, endTime,
            //                                   fillDuration, period,
            //                                   appliedSpeedRatio, accelRatio, decelRatio, isAutoReversed);
            t2.Clear();
            t.ProjectOntoPeriodicFunction(ref t2, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(4),
                                               Duration.Forever, Duration.Forever,
                                               1, 0, 0, false);
            t2.Clear();
            t.ProjectOntoPeriodicFunction(ref t2, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(4),
                                               Duration.Forever, TimeSpan.FromSeconds(10),
                                               1, 0, 0, false);
            t2.Clear();
            t.ProjectOntoPeriodicFunction(ref t2, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(17),
                                               Duration.Forever, TimeSpan.FromSeconds(4),
                                               1, 0, 0, true);
        }
#endif


        #endregion // Methods
        #endregion // External interface

        #region Private

        /// <summary>
        /// Sets _current to the largest index N where nodeTime[N] is less or equal to time.
        /// Returns -1 if no such index N exists.
        /// </summary>
        /// <remarks>
        /// Uses a binary search to curb worst-case time to log2(_count)
        /// </remarks>
        private int Locate(TimeSpan time)
        {
            if (_count == 0 || time < _nodeTime[0])
            {
                return -1;
            }
            else  // time is at least at the first node
            {
                Debug.Assert(_count > 0);  // Count cannot be negative

                int current;
                int left = 0;
                int right = _count - 1;

                // Maintain invariant: T[left] < time < T[right]
                while (left + 1 < right)  // Compute until we have at most 1-unit long interval
                {
                    current = (left + right) >> 1;  // Fast divide by 2
                    if (time < _nodeTime[current])
                    {
                        right = current;
                    }
                    else  // time >= nodeTime[current]
                    {
                        left = current;
                    }
                }

                if (time < _nodeTime[right])
                {
                    return left;
                }
                else  // This case should only be reached when we are at or past the last node
                {
                    Debug.Assert(right == _count - 1);
                    return right;
                }
            }
        }

        internal bool IsEmptyOfRealPoints
        {
            get
            {
                return (_count == 0);
            }
        }

        internal bool IsEmpty
        {
            get
            {
                return (_count == 0 && !_containsNullPoint);
            }
        }

        private void MoveFirst()
        {
            _current = 0;
        }

        private void MoveNext()
        {
            _current++;
            Debug.Assert(_current <= _count);
        }

        private bool CurrentIsAtLastNode
        {
            get
            {
                return (_current + 1 == _count);
            }
        }

        private TimeSpan CurrentNodeTime
        {
            get
            {
                Debug.Assert(_current < _count);
                return _nodeTime[_current];
            }
            set
            {
                Debug.Assert(_current < _count);
                _nodeTime[_current] = value;
            }
        }

        private bool CurrentNodeIsPoint
        {
            get
            {
                Debug.Assert(_current < _count);
                return _nodeIsPoint[_current] ^ _invertCollection;
            }
            set
            {
                Debug.Assert(_current < _count);
                _nodeIsPoint[_current] = value;
            }
        }

        private bool CurrentNodeIsInterval
        {
            get
            {
                Debug.Assert(_current < _count);
                return _nodeIsInterval[_current] ^ _invertCollection;
            }
            set
            {
                Debug.Assert(_current < _count);
                _nodeIsInterval[_current] = value;
            }
        }

        private TimeSpan NextNodeTime
        {
            get
            {
                Debug.Assert(_current + 1 < _count);
                return _nodeTime[_current + 1];
            }
        }

        private bool NextNodeIsPoint
        {
            get
            {
                Debug.Assert(_current + 1 < _count);
                return _nodeIsPoint[_current + 1] ^ _invertCollection;
            }
        }

        private bool NextNodeIsInterval
        {
            get
            {
                Debug.Assert(_current + 1 < _count);
                return _nodeIsInterval[_current + 1] ^ _invertCollection;
            }
        }

        internal bool ContainsNullPoint
        {
            get
            {
                return _containsNullPoint ^ _invertCollection;
            }
        }

        private void SetInvertedMode(bool mode)
        {
            Debug.Assert(_invertCollection != mode);  // Make sure we aren't redundantly setting the mode
            _invertCollection = mode;
        }

        #endregion // Private

        #region Data

        private TimeSpan[]   _nodeTime;   // An interval's begin time
        private bool[]    _nodeIsPoint;   // Whether [begin time] is included in the interval
        private bool[] _nodeIsInterval;   // Whether the open interval (begin time)--(next begin time, or infinity) is included

        private bool _containsNullPoint;  // The point representing off-domain (Stopped) state

        private int _count;               // How many nodes are stored in the TIC
        private int _current;             // Enumerator pointing to the current node
        private bool _invertCollection;   // A flag used for operating on the inverse of a TIC

        private const int _minimumCapacity = 4;  // This should be at least 2 for dynamic growth to work correctly (by 2 each time)

        #endregion // Data
    }
}
