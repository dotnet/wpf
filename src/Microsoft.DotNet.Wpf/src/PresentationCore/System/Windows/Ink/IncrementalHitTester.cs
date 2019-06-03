// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Ink;
using System.Windows.Media;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MS.Internal.Ink;
using MS.Utility;
using MS.Internal;
using System.Diagnostics;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Ink
{
    #region IncrementalHitTester Abstract Base Class

    /// <summary>
    /// This class serves as both the base class and public interface for
    /// incremental hit-testing implementaions.
    /// </summary>
    public abstract class IncrementalHitTester
    {
        #region Public API

        /// <summary>
        /// Adds a point representing an incremental move of the hit-testing tool
        /// </summary>
        /// <param name="point">a point that represents an incremental move of the hitting tool</param>
        public void AddPoint(Point point)
        {
            AddPoints(new Point[1] { point });
        }

        /// <summary>
        /// Adds an array of points representing an incremental move of the hit-testing tool
        /// </summary>
        /// <param name="points">points representing an incremental move of the hitting tool</param>
        public void AddPoints(IEnumerable<Point> points)
        {
            if (points == null)
            {
                throw new System.ArgumentNullException("points");
            }

            if (IEnumerablePointHelper.GetCount(points) == 0)
            {
                throw new System.ArgumentException(SR.Get(SRID.EmptyArrayNotAllowedAsArgument), "points");
            }

            if (false == _fValid)
            {
                throw new System.InvalidOperationException(SR.Get(SRID.EndHitTestingCalled));
            }

            System.Diagnostics.Debug.Assert(_strokes != null);

            AddPointsCore(points);
        }

        /// <summary>
        /// Adds a StylusPacket representing an incremental move of the hit-testing tool
        /// </summary>
        /// <param name="stylusPoints">stylusPoints</param>
        public void AddPoints(StylusPointCollection stylusPoints)
        {
            if (stylusPoints == null)
            {
                throw new System.ArgumentNullException("stylusPoints");
            }

            if (stylusPoints.Count == 0)
            {
                throw new System.ArgumentException(SR.Get(SRID.EmptyArrayNotAllowedAsArgument), "stylusPoints");
            }

            if (false == _fValid)
            {
                throw new System.InvalidOperationException(SR.Get(SRID.EndHitTestingCalled));
            }

            System.Diagnostics.Debug.Assert(_strokes != null);

            Point[] points = new Point[stylusPoints.Count];
            for (int x = 0; x < stylusPoints.Count; x++)
            {
                points[x] = (Point)stylusPoints[x];
            }

            AddPointsCore(points);
        }


        /// <summary>
        /// Release as many resources as possible for this enumerator
        /// </summary>
        public void EndHitTesting()
        {
            if (_strokes != null)
            {
                // Detach the event handler
                _strokes.StrokesChangedInternal -= new StrokeCollectionChangedEventHandler(OnStrokesChanged);
                _strokes = null;
                int count = _strokeInfos.Count;
                for ( int i = 0; i < count; i++)
                {
                    _strokeInfos[i].Detach();
                }
                _strokeInfos = null;
            }
            _fValid = false;
        }

        /// <summary>
        /// Accessor to see if the Hit Tester is still valid
        /// </summary>
        public bool IsValid { get { return _fValid; } }
        #endregion

        #region Internal

        /// <summary>
        /// C-tor.
        /// </summary>
        /// <param name="strokes">strokes to hit-test</param>
        internal IncrementalHitTester(StrokeCollection strokes)
        {
            System.Diagnostics.Debug.Assert(strokes != null);

            // Create a StrokeInfo object for each stroke.
            _strokeInfos = new List<StrokeInfo>(strokes.Count);
            for (int x = 0; x < strokes.Count; x++)
            {
                Stroke stroke = strokes[x];
                _strokeInfos.Add(new StrokeInfo(stroke));
            }

            _strokes = strokes;

            // Attach an event handler to the strokes' changed event
            _strokes.StrokesChangedInternal += new StrokeCollectionChangedEventHandler(OnStrokesChanged);
        }

        /// <summary>
        /// The implementation behind AddPoint/AddPoints.
        /// Derived classes are supposed to override this method.
        /// </summary>
        protected abstract void AddPointsCore(IEnumerable<Point> points);


        /// <summary>
        /// Accessor to the internal collection of StrokeInfo objects
        /// </summary>
        internal List<StrokeInfo> StrokeInfos { get { return _strokeInfos; } }

        #endregion

        #region Private

        /// <summary>
        /// Event handler associated with the stroke collection.
        /// </summary>
        /// <param name="sender">Stroke collection that was modified</param>
        /// <param name="args">Modification that occurred</param>
        /// <remarks>
        /// Update our _strokeInfos cache.  We get notified on StrokeCollection.StrokesChangedInternal which
        /// is raised first so we can assume we're the first delegate in the call chain
        /// </remarks>
        private void OnStrokesChanged(object sender, StrokeCollectionChangedEventArgs args)
        {
            System.Diagnostics.Debug.Assert((_strokes != null) && (_strokeInfos != null) && (_strokes == sender));

            StrokeCollection added = args.Added;
            StrokeCollection removed = args.Removed;

            if (added.Count > 0)
            {
                int firstIndex = _strokes.IndexOf(added[0]);
                for (int i = 0; i < added.Count; i++)
                {
                    _strokeInfos.Insert(firstIndex, new StrokeInfo(added[i]));
                    firstIndex++;
                }
            }

            if (removed.Count > 0)
            {
                StrokeCollection localRemoved = new StrokeCollection(removed);
                //we have to assume that removed strokes can be in any order in _strokes
                for (int i = 0; i < _strokeInfos.Count && localRemoved.Count > 0; )
                {
                    bool found = false;
                    for (int j = 0; j < localRemoved.Count; j++)
                    {
                        if (localRemoved[j] == _strokeInfos[i].Stroke)
                        {
                            _strokeInfos.RemoveAt(i);
                            localRemoved.RemoveAt(j);

                            found = true;
                        }
                    }
                    //we didn't find a removed stroke at index i in _strokeInfos, so advance i
                    if (!found)
                    {
                        i++;
                    }
                }
            }

            //validate our cache
            if (_strokes.Count != _strokeInfos.Count)
            {
                Debug.Assert(false, "Benign assert.  IncrementalHitTester's _strokeInfos cache is out of sync, rebuilding.");
                RebuildStrokeInfoCache();
                return;
            }
            for (int i = 0; i < _strokeInfos.Count; i++)
            {
                if (_strokeInfos[i].Stroke != _strokes[i])
                {
                    Debug.Assert(false, "Benign assert.  IncrementalHitTester's _strokeInfos cache is out of sync, rebuilding.");
                    RebuildStrokeInfoCache();
                    return;
                }
            }
        }

        /// <summary>
        /// IHT's can get into a state where their StrokeInfo cache is too 
        /// out of sync with the StrokeCollection to incrementally update it.
        /// When we detect this has happened, we just rebuild the entire cache.
        /// </summary>
        private void RebuildStrokeInfoCache()
        {
            List<StrokeInfo> newStrokeInfos = new List<StrokeInfo>(_strokes.Count);
            foreach (Stroke stroke in _strokes)
            {
                bool found = false;
                for (int x = 0; x < _strokeInfos.Count; x++)
                {
                    StrokeInfo strokeInfo = _strokeInfos[x];
                    if (strokeInfo != null && stroke == strokeInfo.Stroke)
                    {
                        newStrokeInfos.Add(strokeInfo);
                        //just set to null instead of removing and shifting
                        //we're about to GC _strokeInfos
                        _strokeInfos[x] = null;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    //we didn't find an existing strokeInfo
                    newStrokeInfos.Add(new StrokeInfo(stroke));
                }
            }

            //detach the remaining strokeInfo's from their strokes
            for (int x = 0; x < _strokeInfos.Count; x++)
            {
                StrokeInfo strokeInfo = _strokeInfos[x];
            
                if (strokeInfo != null)
                {
                    strokeInfo.Detach();
                }
            }

            _strokeInfos = newStrokeInfos;

#if DEBUG
            Debug.Assert(_strokeInfos.Count == _strokes.Count);
            for (int x = 0; x < _strokeInfos.Count; x++)
            {
                Debug.Assert(_strokeInfos[x].Stroke == _strokes[x]);
            }
#endif
        }

        #endregion

        #region Fields

        /// <summary> Reference to the stroke collection under test </summary>
        private StrokeCollection _strokes;
        /// <summary> A collection of helper objects mapped to the stroke colection</summary>
        private List<StrokeInfo> _strokeInfos;

        private bool _fValid = true;

        #endregion
    }

    #endregion

    #region IncrementalLassoHitTester

    /// <summary>
    /// IncrementalHitTester implementation for hit-testing with lasso
    /// </summary>
    public class IncrementalLassoHitTester : IncrementalHitTester
    {
        #region public APIs
        /// <summary>
        /// Event
        /// </summary>
        public event LassoSelectionChangedEventHandler SelectionChanged;
        #endregion
        #region C-tor and the overrides

        /// <summary>
        /// C-tor.
        /// </summary>
        /// <param name="strokes">strokes to hit-test</param>
        /// <param name="percentageWithinLasso">a hit-testing parameter that defines the minimal
        /// percent of nodes of a stroke to be inside the lasso to consider the stroke hit</param>
        internal IncrementalLassoHitTester(StrokeCollection strokes, int percentageWithinLasso)
            : base(strokes)
        {
            System.Diagnostics.Debug.Assert((percentageWithinLasso >= 0) && (percentageWithinLasso <= 100));
            _lasso = new SingleLoopLasso();
            _percentIntersect = percentageWithinLasso;
        }

        /// <summary>
        /// The implementation behind the public methods AddPoint/AddPoints
        /// </summary>
        /// <param name="points">new points to add to the lasso</param>
        protected override void AddPointsCore(IEnumerable<Point> points)
        {
            System.Diagnostics.Debug.Assert((points != null) && (IEnumerablePointHelper.GetCount(points)!= 0));

            // Add the new points to the lasso
            int lastPointIndex = (0 != _lasso.PointCount) ? (_lasso.PointCount - 1) : 0;
            _lasso.AddPoints(points);

            // Do nothing if there's not enough points, or there's nobody listening
            // The points may be filtered out, so if all the points are filtered out, (lastPointIndex == (_lasso.PointCount - 1).
            // For this case, check if the incremental lasso is disabled (i.e., points modified).
            if ((_lasso.IsEmpty) || (lastPointIndex == (_lasso.PointCount - 1) && false == _lasso.IsIncrementalLassoDirty)
                || (SelectionChanged == null))
            {
                return;
            }

            // Variables for possible HitChanged events to fire
            StrokeCollection strokesHit = null;
            StrokeCollection strokesUnhit = null;

            // Create a lasso that represents the current increment
            Lasso lassoUpdate = new Lasso();

            if (false == _lasso.IsIncrementalLassoDirty)
            {
                if (0 < lastPointIndex)
                {
                    lassoUpdate.AddPoint(_lasso[0]);
                }

                // Only the points the have been successfully added to _lasso will be added to
                // lassoUpdate.
                for (; lastPointIndex < _lasso.PointCount; lastPointIndex++)
                {
                    lassoUpdate.AddPoint(_lasso[lastPointIndex]);
                }
            }

            // Enumerate through the strokes and update their hit-test results
            foreach (StrokeInfo strokeInfo in this.StrokeInfos)
            {
                Lasso lasso;
                if (true == strokeInfo.IsDirty || true == _lasso.IsIncrementalLassoDirty)
                {
                    // If this is the first time this stroke gets hit-tested with this lasso,
                    // or if the stroke (or its DAs) has changed since the last hit-testing,
                    // or if the lasso points have been modified,
                    // then (re)hit-test this stroke against the entire lasso.
                    lasso = _lasso;
                    strokeInfo.IsDirty = false;
                }
                else
                {
                    // Otherwise, hit-test it against the lasso increment first and then only
                    // those ink points that are in that small lasso need to be hit-tested
                    // against the big (entire) lasso.
                    // This is supposed to be a significant piece of optimization, since
                    // lasso increments are usually very small, they are defined by just
                    // a few points and they don't capture and/or release too many ink nodes.
                    lasso = lassoUpdate;
                }

                // Skip those stroke which bounding box doesn't even intersects with the lasso bounds
                double hitWeightChange = 0f;
                if (lasso.Bounds.IntersectsWith(strokeInfo.StrokeBounds))
                {
                    // Get the stroke node points for the hit-testing.
                    StylusPointCollection stylusPoints = strokeInfo.StylusPoints;

                    // Find out if the lasso update has changed the hit count of the stroke.
                    for (int i = 0; i < stylusPoints.Count; i++)
                    {
                        // Consider only the points that become captured/released with this particular update
                        if (true == lasso.Contains((Point)stylusPoints[i]))
                        {
                            double weight = strokeInfo.GetPointWeight(i);

                            if (lasso == _lasso || _lasso.Contains((Point)stylusPoints[i]))
                            {
                                hitWeightChange += weight;
                            }
                            else
                            {
                                hitWeightChange -= weight;
                            }
                        }
                    }
                }

                // Update the stroke hit weight and check whether it has crossed the margin
                // in either direction since the last update.
                if ((hitWeightChange != 0) || (lasso == _lasso))
                {
                    strokeInfo.HitWeight = (lasso == _lasso) ? hitWeightChange : (strokeInfo.HitWeight + hitWeightChange);
                    bool isHit = DoubleUtil.GreaterThanOrClose(strokeInfo.HitWeight, strokeInfo.TotalWeight * _percentIntersect / 100f - Stroke.PercentageTolerance);

                    if (strokeInfo.IsHit != isHit)
                    {
                        strokeInfo.IsHit = isHit;
                        if (isHit)
                        {
                            // The hit count became greater than the margin percentage, the stroke
                            // needs to be reported for selection
                            if (null == strokesHit)
                            {
                                strokesHit = new StrokeCollection();
                            }
                            strokesHit.Add(strokeInfo.Stroke);
                        }
                        else
                        {
                            // The hit count just became less than the margin percentage,
                            // the stroke needs to be reported for de-selection
                            if (null == strokesUnhit)
                            {
                                strokesUnhit = new StrokeCollection();
                            }
                            strokesUnhit.Add(strokeInfo.Stroke);
                        }
                    }
                }
            }

            _lasso.IsIncrementalLassoDirty = false;
            // Raise StrokesHitChanged event if any strokes has changed thier
            // hit status and there're the event subscribers.
            if ((null != strokesHit) || (null != strokesUnhit))
            {
                OnSelectionChanged(new LassoSelectionChangedEventArgs (strokesHit, strokesUnhit));
            }
        }

        /// <summary>
        /// SelectionChanged event raiser
        /// </summary>
        /// <param name="eventArgs"></param>
        protected void OnSelectionChanged(LassoSelectionChangedEventArgs  eventArgs)
        {
            System.Diagnostics.Debug.Assert(eventArgs != null);
            if (SelectionChanged != null)
            {
                SelectionChanged(this, eventArgs);
            }
        }


        #endregion
        #region Fields

        private Lasso   _lasso;
        private int     _percentIntersect;

        #endregion
    }

    #endregion

    #region IncrementalStrokeHitTester

    /// <summary>
    /// IncrementalHitTester implementation for hit-testing with a shape, PointErasing .
    /// </summary>
    public class IncrementalStrokeHitTester  : IncrementalHitTester
    {
        /// <summary>
        ///
        /// </summary>
        public event StrokeHitEventHandler StrokeHit;

        #region C-tor and the overrides

        /// <summary>
        /// C-tor
        /// </summary>
        /// <param name="strokes">strokes to hit-test for erasing</param>
        /// <param name="eraserShape">erasing shape</param>
        internal IncrementalStrokeHitTester(StrokeCollection strokes, StylusShape eraserShape)
            : base(strokes)
        {
            System.Diagnostics.Debug.Assert(eraserShape != null);

            // Create an ErasingStroke objects that implements the actual hit-testing
            _erasingStroke = new ErasingStroke(eraserShape);
        }

        /// <summary>
        /// The implementation behind the public methods AddPoint/AddPoints
        /// </summary>
        /// <param name="points">a set of points representing the last increment
        /// in the moving of the erasing shape</param>
        protected override void AddPointsCore(IEnumerable<Point> points)
        {
            System.Diagnostics.Debug.Assert((points != null) && (IEnumerablePointHelper.GetCount(points) != 0));
            System.Diagnostics.Debug.Assert(_erasingStroke != null);

            // Move the shape through the new points and build the contour of the move.
            _erasingStroke.MoveTo(points);
            Rect erasingBounds = _erasingStroke.Bounds;
            if (erasingBounds.IsEmpty)
            {
                return;
            }

            List<StrokeHitEventArgs> strokeHitEventArgCollection = null;
            // Do nothing if there's nobody listening to the events
            if (StrokeHit != null)
            {
                List<StrokeIntersection> eraseAt = new List<StrokeIntersection>();

                // Test stroke by stroke and collect the results.
                for (int x = 0; x < this.StrokeInfos.Count; x++)
                {
                    StrokeInfo strokeInfo = this.StrokeInfos[x];

                    // Skip the stroke if its bounding box doesn't intersect with the one of the hitting shape.
                    if ((erasingBounds.IntersectsWith(strokeInfo.StrokeBounds) == false) ||
                        (_erasingStroke.EraseTest(StrokeNodeIterator.GetIterator(strokeInfo.Stroke, strokeInfo.Stroke.DrawingAttributes), eraseAt) == false))
                    {
                        continue;
                    }

                    // Create an event args to raise after done with hit-testing
                    // We don't fire these events right away because user is expected to
                    // modify the stroke collection in her event handler, and that would
                    // invalidate this foreach loop.
                    if (strokeHitEventArgCollection == null)
                    {
                        strokeHitEventArgCollection = new List<StrokeHitEventArgs>();
                    }
                    strokeHitEventArgCollection.Add(new StrokeHitEventArgs(strokeInfo.Stroke, eraseAt.ToArray()));
                    // We must clear eraseAt or it will contain invalid results for the next strokes
                    eraseAt.Clear();
                }
            }

            // Raise StrokeHit event if needed.
            if (strokeHitEventArgCollection != null)
            {
                System.Diagnostics.Debug.Assert(strokeHitEventArgCollection.Count != 0);
                for (int x = 0; x < strokeHitEventArgCollection.Count; x++)
                {
                    StrokeHitEventArgs eventArgs = strokeHitEventArgCollection[x];

                    System.Diagnostics.Debug.Assert(eventArgs.HitStroke != null);
                    OnStrokeHit(eventArgs);
                }
            }
        }

        /// <summary>
        /// Event raiser for StrokeHit
        /// </summary>
        protected void OnStrokeHit(StrokeHitEventArgs eventArgs)
        {
            System.Diagnostics.Debug.Assert(eventArgs != null);
            if (StrokeHit != null)
            {
                StrokeHit(this, eventArgs);
            }
        }

        #endregion

        #region Fields

        private ErasingStroke _erasingStroke;

        #endregion
    }


    #endregion

    #region EventArgs and delegates

    /// <summary>
    /// Declaration for LassoSelectionChanged event handler. Used in lasso-selection
    /// </summary>
    public delegate void LassoSelectionChangedEventHandler(object sender, LassoSelectionChangedEventArgs e);


    /// <summary>
    /// Declaration for StrokeHit event handler. Used in point-erasing
    /// </summary>
    public delegate void StrokeHitEventHandler(object sender, StrokeHitEventArgs e);


    /// <summary>
    /// Event arguments for LassoSelectionChanged event
    /// </summary>
    public class LassoSelectionChangedEventArgs  : EventArgs
    {
        internal LassoSelectionChangedEventArgs(StrokeCollection selectedStrokes, StrokeCollection deselectedStrokes)
        {
            _selectedStrokes = selectedStrokes;
            _deselectedStrokes = deselectedStrokes;
        }

        /// <summary>
        /// Collection of strokes which were hit with the last increment
        /// </summary>
        public StrokeCollection SelectedStrokes
        {
            get
            {
                if (_selectedStrokes != null)
                {
                    StrokeCollection sc = new StrokeCollection();
                    sc.Add(_selectedStrokes);
                    return sc;
                }
                else
                {
                    return  new StrokeCollection();
                }
            }
        }

        /// <summary>
        /// Collection of strokes which were unhit with the last increment
        /// </summary>
        public StrokeCollection DeselectedStrokes
        {
            get
            {
                if (_deselectedStrokes != null)
                {
                    StrokeCollection sc = new StrokeCollection();
                    sc.Add(_deselectedStrokes);
                    return sc;
                }
                else
                {
                    return new StrokeCollection();
                }
            }
        }

        private StrokeCollection _selectedStrokes;
        private StrokeCollection _deselectedStrokes;
    }

    /// <summary>
    /// Event arguments for StrokeHit event
    /// </summary>
    public class StrokeHitEventArgs : EventArgs
    {
        /// <summary>
        /// C-tor
        /// </summary>
        internal StrokeHitEventArgs(Stroke stroke, StrokeIntersection[] hitFragments)
        {
            System.Diagnostics.Debug.Assert(stroke != null && hitFragments != null && hitFragments.Length > 0);
            _stroke = stroke;
            _hitFragments = hitFragments;
        }

        /// <summary>Stroke that was hit</summary>
        public Stroke HitStroke { get { return _stroke; } }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public StrokeCollection GetPointEraseResults()
        {
            return _stroke.Erase(_hitFragments);
        }

        private Stroke                  _stroke;
        private StrokeIntersection[]    _hitFragments;
}

    #endregion
}

namespace MS.Internal.Ink
{
    #region StrokeInfo

    /// <summary>
    /// A helper class associated with a stroke. Used for caching the stroke's
    /// bounding box, hit-testing results, and for keeping an eye on the stroke changes
    /// </summary>
    internal class StrokeInfo
    {
        #region API (used by incremental hit-testers)

        /// <summary>
        /// StrokeInfo
        /// </summary>
        internal StrokeInfo(Stroke stroke)
        {
            System.Diagnostics.Debug.Assert(stroke != null);
            _stroke = stroke;
            _bounds = stroke.GetBounds();

            // Start listening to the stroke events
            _stroke.DrawingAttributesChanged += new PropertyDataChangedEventHandler(OnStrokeDrawingAttributesChanged);
            _stroke.StylusPointsReplaced += new StylusPointsReplacedEventHandler(OnStylusPointsReplaced);
            _stroke.StylusPoints.Changed += new EventHandler(OnStylusPointsChanged);
            _stroke.DrawingAttributesReplaced += new DrawingAttributesReplacedEventHandler(OnDrawingAttributesReplaced);
        }

        /// <summary>The stroke object associated with this helper structure</summary>
        internal Stroke Stroke { get { return _stroke; } }

        /// <summary>Pre-calculated bounds of the stroke </summary>
        internal Rect StrokeBounds { get { return _bounds; } }

        /// <summary>Tells whether the stroke or its drawing attributes have been modified
        /// since the last use (hit-testing)</summary>
        internal bool IsDirty
        {
            get { return _isDirty; }
            set { _isDirty = value; }
        }

        /// <summary>Tells whether the stroke was found (and reported) as hit </summary>
        internal bool IsHit
        {
            get { return _isHit; }
            set { _isHit = value; }
        }

        /// <summary>
        /// Cache teh stroke points
        /// </summary>
        internal StylusPointCollection StylusPoints
        {
            get
            {
                if (_stylusPoints == null)
                {
                    if (_stroke.DrawingAttributes.FitToCurve)
                    {
                        _stylusPoints = _stroke.GetBezierStylusPoints();
                    }
                    else
                    {
                        _stylusPoints = _stroke.StylusPoints;
                    }
                }
                return _stylusPoints;
            }
        }

        /// <summary>
        /// Holds the current hit-testing result for the stroke. Represents the length of
        /// the stroke "inside" and "hit" by the lasso
        /// </summary>
        internal double HitWeight
        {
            get { return _hitWeight; }
            set
            {
                // it is ok to clamp this off, rounding error sends it over or under by a minimal amount.
                if (DoubleUtil.GreaterThan(value, TotalWeight))
                {
                    _hitWeight = TotalWeight;
                }
                else if (DoubleUtil.LessThan(value, 0f))
                {
                    _hitWeight = 0f;
                }
                else
                {
                    _hitWeight = value;
                }
            }
        }

        /// <summary>
        /// Get the total weight of the stroke. For this implementation, it is the total length of the stroke.
        /// </summary>
        /// <returns></returns>
        internal double TotalWeight
        {
            get
            {
                if (!_totalWeightCached)
                {
                    _totalWeight= 0;
                    for (int i = 0; i < StylusPoints.Count; i++)
                    {
                        _totalWeight += this.GetPointWeight(i);
                    }
                    _totalWeightCached = true;
                }
                return _totalWeight;
            }
        }

        /// <summary>
        /// Calculate the weight of a point.
        /// </summary>
        internal double GetPointWeight(int index)
        {
            StylusPointCollection stylusPoints = this.StylusPoints;
            DrawingAttributes da = this.Stroke.DrawingAttributes;
            System.Diagnostics.Debug.Assert(stylusPoints != null && index >= 0 && index < stylusPoints.Count);

            double weight = 0f;
            if (index == 0)
            {
                weight += Math.Sqrt(da.Width*da.Width + da.Height*da.Height) / 2.0f;
            }
            else
            {
                Vector spine = (Point)stylusPoints[index] - (Point)stylusPoints[index - 1];
                weight += Math.Sqrt(spine.LengthSquared) / 2.0f;
            }

            if (index == stylusPoints.Count - 1)
            {
                weight += Math.Sqrt(da.Width*da.Width + da.Height*da.Height) / 2.0f;
            }
            else
            {
                Vector spine = (Point)stylusPoints[index + 1] - (Point)stylusPoints[index];
                weight += Math.Sqrt(spine.LengthSquared) / 2.0f;
            }

            return weight;
        }
        /// <summary>
        /// A kind of disposing method
        /// </summary>
        internal void Detach()
        {
            if (_stroke != null)
            {
                // Detach the event handlers
                _stroke.DrawingAttributesChanged -= new PropertyDataChangedEventHandler(OnStrokeDrawingAttributesChanged);
                _stroke.StylusPointsReplaced -= new StylusPointsReplacedEventHandler(OnStylusPointsReplaced);
                _stroke.StylusPoints.Changed -= new EventHandler(OnStylusPointsChanged);
                _stroke.DrawingAttributesReplaced -= new DrawingAttributesReplacedEventHandler(OnDrawingAttributesReplaced);

                _stroke = null;
            }
        }

        #endregion

        #region Stroke event handlers (Private)

        /// <summary>Event handler for stroke data changed events</summary>
        private void OnStylusPointsChanged(object sender, EventArgs args)
        {
            Invalidate();
        }

        /// <summary>Event handler for stroke data changed events</summary>
        private void OnStylusPointsReplaced(object sender, StylusPointsReplacedEventArgs args)
        {
            Invalidate();
        }

        /// <summary>
        /// Event handler for stroke's drawing attributes changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnStrokeDrawingAttributesChanged(object sender, PropertyDataChangedEventArgs args)
        {
            // Only enforce rehittesting of the whole stroke when the DrawingAttribute change may affect hittesting
            if(DrawingAttributes.IsGeometricalDaGuid(args.PropertyGuid))
            {
                Invalidate();
            }
        }

        private void OnDrawingAttributesReplaced(Object sender, DrawingAttributesReplacedEventArgs args)
        {
            // If the drawing attributes change involves Width, Height, StylusTipTransform, IgnorePressure, or FitToCurve,
            // we need to invalidate
            if (false == DrawingAttributes.GeometricallyEqual(args.NewDrawingAttributes, args.PreviousDrawingAttributes))
            {
                Invalidate();
            }
        }

        /// <summary>Implementation for the event handlers above</summary>
        private void Invalidate()
        {
            _totalWeightCached = false;
            _stylusPoints = null;
            _hitWeight = 0;

            // Let the hit-tester know that it should not use incremental hit-testing
            _isDirty = true;

            // The Stroke.GetBounds may be overriden in the 3rd party code.
            // The out-side code could throw exception. If an exception is thrown, _bounds will keep the original value.
            // Re-compute the stroke bounds
            _bounds = _stroke.GetBounds();
        }
        #endregion

        #region Fields

        private Stroke                      _stroke;
        private Rect                        _bounds;
        private double                      _hitWeight = 0f;
        private bool                        _isHit = false;
        private bool                        _isDirty = true;
        private StylusPointCollection       _stylusPoints;   // Cache the stroke rendering points
        private double                      _totalWeight = 0f;
        private bool                        _totalWeightCached = false;
        #endregion
    }

    #endregion // StrokeInfo
}

// The following code is for Stroke-Erasing scenario. Currently the IncrementalStrokeHitTester
// can be used for Stroke-erasing but the following class is faster. If in the future there's a
// perf issue with Stroke-Erasing, consider adding the following code.
//#region Commented Code for IncrementalStrokeHitTester
//#region IncrementalStrokeHitTester

///// <summary>
///// IncrementalHitTester implementation for hit-testing with a shape, StrokeErasing .
///// </summary>
//public class IncrementalStrokeHitTester : IncrementalHitTester
//{
//    /// <summary>
//    /// event
//    /// </summary>
//    public event StrokesHitEventHandler StrokesHit;

//    #region C-tor and the overrides

//    /// <summary>
//    /// C-tor
//    /// </summary>
//    /// <param name="strokes">strokes to hit-test for erasing</param>
//    /// <param name="eraserShape">erasing shape</param>
//    internal IncrementalStrokeHitTester(StrokeCollection strokes, StylusShape eraserShape)
//        : base(strokes)
//    {
//        System.Diagnostics.Debug.Assert(eraserShape != null);

//        // Create an ErasingStroke objects that implements the actual hit-testing
//        _erasingStroke = new ErasingStroke(eraserShape);
//    }

//    /// <summary>
//    ///
//    /// </summary>
//    /// <param name="eventArgs"></param>
//    internal protected void OnStrokesHit(StrokesHitEventArgs eventArgs)
//    {
//        if (StrokesHit != null)
//        {
//            StrokesHit(this, eventArgs);
//        }
//    }

//    /// <summary>
//    /// The implementation behind the public methods AddPoint/AddPoints
//    /// </summary>
//    /// <param name="points">a set of points representing the last increment
//    /// in the moving of the erasing shape</param>
//    internal protected override void AddPointsCore(Point[] points)
//    {
//        System.Diagnostics.Debug.Assert((points != null) && (points.Length != 0));
//        System.Diagnostics.Debug.Assert(_erasingStroke != null);

//        // Move the shape through the new points and build the contour of the move.
//        _erasingStroke.MoveTo(points);
//        Rect erasingBounds = _erasingStroke.Bounds;
//        if (erasingBounds.IsEmpty)
//        {
//            return;
//        }

//        StrokeCollection strokesHit = null;
//        if (StrokesHit != null)
//        {
//            // Test stroke by stroke and collect hits.
//            foreach (StrokeInfo strokeInfo in StrokeInfos)
//            {
//                // Skip strokes that have already been reported hit or which bounds
//                // don't intersect with the bounds of the erasing stroke.
//                if ((strokeInfo.IsHit == false) && erasingBounds.IntersectsWith(strokeInfo.StrokeBounds)
//                    && _erasingStroke.HitTest(StrokeNodeIterator.GetIterator(strokeInfo.Stroke, strokeInfo.Overrides)))
//                {
//                    if (strokesHit == null)
//                    {
//                        strokesHit = new StrokeCollection();
//                    }
//                    strokesHit.Add(strokeInfo.Stroke);
//                    strokeInfo.IsHit = true;
//                }
//            }
//        }

//        // Raise StrokesHitChanged event if any strokes have been hit and there're listeners to the event.
//        if (strokesHit != null)
//        {
//            System.Diagnostics.Debug.Assert(strokesHit.Count != 0);
//            OnStrokesHit(new StrokesHitEventArgs(strokesHit));
//        }
//    }

//    #endregion

//    #region Fields

//    private ErasingStroke _erasingStroke;

//    #endregion
//}

//#endregion

///// <summary>
///// Declaration for StrokesHit event handler. Used in stroke-erasing
///// </summary>
//public delegate void StrokesHitEventHandler(object sender, StrokesHitEventArgs e);

///// <summary>
///// Event arguments for StrokesHit event
///// </summary>
//public class StrokesHitEventArgs : EventArgs
//{
//    internal StrokesHitEventArgs(StrokeCollection hitStrokes)
//    {
//        System.Diagnostics.Debug.Assert(hitStrokes != null && hitStrokes.Count > 0);
//        _hitStrokes = hitStrokes;
//    }

//    /// <summary>
//    ///
//    /// </summary>
//    public StrokeCollection HitStrokes
//    {
//        get { return _hitStrokes; }
//    }

//    private StrokeCollection _hitStrokes;
//}

//#endregion
