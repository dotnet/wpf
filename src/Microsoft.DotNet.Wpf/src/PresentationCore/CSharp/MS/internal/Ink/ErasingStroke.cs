// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//#define POINTS_FILTER_TRACE

using System;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;
using System.Collections.Generic;


namespace MS.Internal.Ink
{
    #region ErasingStroke

    /// <summary>
    /// This class represents a contour of an erasing stroke, and provides
    /// internal API for static and incremental stroke_contour vs stroke_contour
    /// hit-testing.
    /// </summary>
    internal class ErasingStroke
    {
        #region Constructors

        /// <summary>
        /// Constructor for incremental erasing
        /// </summary>
        /// <param name="erasingShape">The shape of the eraser's tip</param>
        internal ErasingStroke(StylusShape erasingShape)
        {
            System.Diagnostics.Debug.Assert(erasingShape != null);
            _nodeIterator = new StrokeNodeIterator(erasingShape);
        }

        /// <summary>
        /// Constructor for static (atomic) erasing
        /// </summary>
        /// <param name="erasingShape">The shape of the eraser's tip</param>
        /// <param name="path">the spine of the erasing stroke</param>
        internal ErasingStroke(StylusShape erasingShape, IEnumerable<Point> path)
            : this(erasingShape)
        {
            MoveTo(path);
        }

        #endregion

        #region API

        /// <summary>
        /// Generates stroke nodes along a given path.
        /// Drops any previously genererated nodes.
        /// </summary>
        /// <param name="path"></param>
        internal void MoveTo(IEnumerable<Point> path)
        {
            System.Diagnostics.Debug.Assert((path != null) && (IEnumerablePointHelper.GetCount(path) != 0));
            Point[] points = IEnumerablePointHelper.GetPointArray(path);

            if (_erasingStrokeNodes == null)
            {
                _erasingStrokeNodes = new List<StrokeNode>(points.Length);
            }
            else
            {
                _erasingStrokeNodes.Clear();
            }


            _bounds = Rect.Empty;
            _nodeIterator = _nodeIterator.GetIteratorForNextSegment(points.Length > 1 ? FilterPoints(points) : points);
            for (int i = 0; i < _nodeIterator.Count; i++)
            {
                StrokeNode strokeNode = _nodeIterator[i];
                _bounds.Union(strokeNode.GetBoundsConnected());
                _erasingStrokeNodes.Add(strokeNode);
            }
#if POINTS_FILTER_TRACE
            _totalPointsAdded += path.Length;
            System.Diagnostics.Debug.WriteLine(String.Format("Total Points added: {0} screened: {1} collinear screened: {2}", _totalPointsAdded, _totalPointsScreened, _collinearPointsScreened));
#endif

        }

        /// <summary>
        /// Returns the bounds of the eraser's last move.
        /// </summary>
        /// <value></value>
        internal Rect Bounds { get { return _bounds; } }

        /// <summary>
        /// Hit-testing for stroke erase scenario.
        /// </summary>
        /// <param name="iterator">the stroke nodes to iterate</param>
        /// <returns>true if the strokes intersect, false otherwise</returns>
        internal bool HitTest(StrokeNodeIterator iterator)
        {
            System.Diagnostics.Debug.Assert(iterator != null);

            if ((_erasingStrokeNodes == null) || (_erasingStrokeNodes.Count == 0))
            {
                return false;
            }

            Rect inkSegmentBounds = Rect.Empty;
            for (int i = 0; i < iterator.Count; i++)
            {
                StrokeNode inkStrokeNode = iterator[i];
                Rect inkNodeBounds = inkStrokeNode.GetBounds();
                inkSegmentBounds.Union(inkNodeBounds);

                if (inkSegmentBounds.IntersectsWith(_bounds))
                {
                    // can be optimized (using pre-computed bounds
                    // of parts of the erasing stroke)
                    foreach (StrokeNode erasingStrokeNode in _erasingStrokeNodes)
                    {
                        if (inkSegmentBounds.IntersectsWith(erasingStrokeNode.GetBoundsConnected())
                            && erasingStrokeNode.HitTest(inkStrokeNode))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Hit-testing for point erase.
        /// </summary>
        /// <param name="iterator"></param>
        /// <param name="intersections"></param>
        /// <returns></returns>
        internal bool EraseTest(StrokeNodeIterator iterator, List<StrokeIntersection> intersections)
        {
            System.Diagnostics.Debug.Assert(iterator != null);
            System.Diagnostics.Debug.Assert(intersections != null);
            intersections.Clear();

            List<StrokeFIndices> eraseAt = new List<StrokeFIndices>();

            if ((_erasingStrokeNodes == null) || (_erasingStrokeNodes.Count == 0))
            {
                return false;
            }

            Rect inkSegmentBounds = Rect.Empty;
            for (int x = 0; x < iterator.Count; x++)
            {   
                StrokeNode inkStrokeNode = iterator[x];
                Rect inkNodeBounds = inkStrokeNode.GetBounds();
                inkSegmentBounds.Union(inkNodeBounds);

                if (inkSegmentBounds.IntersectsWith(_bounds))
                {
                    // can be optimized (using pre-computed bounds
                    // of parts of the erasing stroke)
                    int index = eraseAt.Count;
                    foreach (StrokeNode erasingStrokeNode in _erasingStrokeNodes)
                    {
                        if (false == inkSegmentBounds.IntersectsWith(erasingStrokeNode.GetBoundsConnected()))
                        {
                            continue;
                        }

                        StrokeFIndices fragment = inkStrokeNode.CutTest(erasingStrokeNode);
                        if (fragment.IsEmpty)
                        {
                            continue;
                        }

                        // Merge it with the other results for this ink segment
                        bool inserted = false;
                        for (int i = index; i < eraseAt.Count; i++)
                        {
                            StrokeFIndices lastFragment = eraseAt[i];
                            if (fragment.BeginFIndex < lastFragment.EndFIndex)
                            {
                                // If the fragments overlap, merge them
                                if (fragment.EndFIndex > lastFragment.BeginFIndex)
                                {
                                    fragment = new StrokeFIndices(
                                        Math.Min(lastFragment.BeginFIndex, fragment.BeginFIndex),
                                        Math.Max(lastFragment.EndFIndex, fragment.EndFIndex));

                                    // If the fragment doesn't go beyond lastFragment, break
                                    if ((fragment.EndFIndex <= lastFragment.EndFIndex) || ((i + 1) == eraseAt.Count))
                                    {
                                        inserted = true;
                                        eraseAt[i] = fragment;
                                        break;
                                    }
                                    else
                                    {
                                        eraseAt.RemoveAt(i);
                                        i--;
                                    }
                                }
                                // insert otherwise
                                else
                                {
                                    eraseAt.Insert(i, fragment);
                                    inserted = true;
                                    break;
                                }
                            }
                        }

                        // If not merged nor inserted, add it to the end of the list
                        if (false == inserted)
                        {
                            eraseAt.Add(fragment);
                        }
                        // Break out if the entire ink segment is hit - {BeforeFirst, AfterLast}
                        if (eraseAt[eraseAt.Count - 1].IsFull)
                        {
                            break;
                        }
                    }
                    // Merge inter-segment overlapping fragments
                    if ((index > 0) && (index < eraseAt.Count))
                    {
                        StrokeFIndices lastFragment = eraseAt[index - 1];
                        if (DoubleUtil.AreClose(lastFragment.EndFIndex, StrokeFIndices.AfterLast) )
                        {
                            if (DoubleUtil.AreClose(eraseAt[index].BeginFIndex, StrokeFIndices.BeforeFirst))
                            {
                                lastFragment.EndFIndex = eraseAt[index].EndFIndex;
                                eraseAt[index - 1] = lastFragment;
                                eraseAt.RemoveAt(index);
                            }
                            else
                            {
                                lastFragment.EndFIndex = inkStrokeNode.Index;
                                eraseAt[index - 1] = lastFragment;
                            }
                        }
                    }
                }
                // Start next ink segment
                inkSegmentBounds = inkNodeBounds;
            }
            if (eraseAt.Count != 0)
            {
                foreach (StrokeFIndices segment in eraseAt)
                {
                    intersections.Add(new StrokeIntersection(segment.BeginFIndex, StrokeFIndices.AfterLast,
                                            StrokeFIndices.BeforeFirst, segment.EndFIndex));
                }
            }
            return (eraseAt.Count != 0);
        }

        #endregion

        #region private API
        private Point[] FilterPoints(Point[] path)
        {
            System.Diagnostics.Debug.Assert(path.Length > 1);
            Point back2, back1;
            int i;
            List<Point> newPath = new List<Point>();
            if (_nodeIterator.Count == 0)
            {
                newPath.Add(path[0]);
                newPath.Add(path[1]);
                back2 = path[0];
                back1 = path[1];
                i = 2;
            }
            else
            {
                newPath.Add(path[0]);
                back2 = _nodeIterator[_nodeIterator.Count - 1].Position;
                back1 = path[0];
                i = 1;
            }

            while (i < path.Length)
            {
                if (DoubleUtil.AreClose(back1, path[i]))
                {
                    // Filter out duplicate points
                    i++;
                    continue;
                }

                Vector begin = back2 - back1;
                Vector end = path[i] - back1;
                //On a line defined by begin & end,  finds the findex of the point nearest to the origin (0,0).
                double findex = StrokeNodeOperations.GetProjectionFIndex(begin, end);

                if (DoubleUtil.IsBetweenZeroAndOne(findex))
                {
                    Vector v = (begin + (end - begin) * findex);
                    if (v.LengthSquared < CollinearTolerance)
                    {
                        // The point back1 can be considered as on the line from back2 to the toTest StrokeNode.
                        // Modify the previous point.
                        newPath[newPath.Count - 1] = path[i];
                        back1 = path[i];
                        i++;
#if POINTS_FILTER_TRACE
                        _collinearPointsScreened ++;
#endif
                        continue;
                    }
                }

                // Add the surviving point into the list.
                newPath.Add(path[i]);
                back2 = back1;
                back1 = path[i];
                i++;
            }
#if POINTS_FILTER_TRACE
            _totalPointsScreened += path.Length - newPath.Count;
#endif
            return newPath.ToArray();
        }

        #endregion

        #region Fields

        private StrokeNodeIterator      _nodeIterator;
        private List<StrokeNode>        _erasingStrokeNodes = null;
        private Rect                    _bounds = Rect.Empty;

#if POINTS_FILTER_TRACE
        private int                     _totalPointsAdded = 0;
        private int                     _totalPointsScreened = 0;
        private int                     _collinearPointsScreened = 0;
#endif

        // The collinear tolerance used in points filtering algorithm. The valie
        // should be further tuned considering trade-off of performance and accuracy.
        // In general, the larger the value, more points are filtered but less accurate.
        // For a value of 0.5, typically 70% - 80% percent of the points are filtered out.
        private static readonly double CollinearTolerance = 0.1f;

        #endregion
    }

    #endregion
}

