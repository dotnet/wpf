// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Utility;
using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Input;
using MS.Internal;
using MS.Internal.Ink;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Ink
{
    /// <summary>
    /// The hit-testing API of StrokeCollection.
    /// </summary>
    public partial class StrokeCollection : Collection<Stroke>, INotifyPropertyChanged, INotifyCollectionChanged
    {
        #region Public APIs

        /// <summary>
        /// Calculates the combined bounds of all strokes in the collection
        /// </summary>
        /// <returns></returns>
        public Rect GetBounds()
        { 
            Rect bounds = Rect.Empty;
            foreach (Stroke stroke in this)
            {
                // samgeo - Presharp issue
                // Presharp gives a warning when get methods might deref a null.  It's complaining
                // here that 'stroke'' could be null, but StrokeCollection never allows nulls to be added
                // so this is not possible
#pragma warning disable 1634, 1691
#pragma warning suppress 6506
                bounds.Union(stroke.GetBounds());
#pragma warning restore 1634, 1691
            }
            return bounds;
        }

        // ISSUE-2004/12/13-XIAOTU: In M8.2, the following two tap-hit APIs return the top-hit stroke,
        // giving preference to non-highlighter strokes. We have decided not to treat highlighter and
        // non-highlighter differently and only return the top-hit stroke. But there are two remaining
        // open-issues on this:
        //  1. Do we need to make these two APIs virtual, so user can treat highlighter differently if they
        //     want to?
        //  2. Since we are only returning the top-hit stroke, should we use Stroke as the return type?
        //

        /// <summary>
        /// Tap-hit. Hit tests all strokes within a point, and returns a StrokeCollection for these strokes.Internally does Stroke.HitTest(Point, 1pxlRectShape).
        /// </summary>
        /// <returns>A StrokeCollection that either empty or contains the top hit stroke</returns>
        public StrokeCollection HitTest(Point point)
        {
            return PointHitTest(point, new RectangleStylusShape(1f, 1f));
        }

        /// <summary>
        /// Tap-hit
        /// </summary>
        /// <param name="point">The central point</param>
        /// <param name="diameter">The diameter value of the circle</param>
        /// <returns>A StrokeCollection that either empty or contains the top hit stroke</returns>
        public StrokeCollection HitTest(Point point, double diameter)
        {
            if (Double.IsNaN(diameter) || diameter < DrawingAttributes.MinWidth || diameter > DrawingAttributes.MaxWidth)
            {
                throw new ArgumentOutOfRangeException("diameter", SR.Get(SRID.InvalidDiameter));
            }
            return PointHitTest(point, new EllipseStylusShape(diameter, diameter));
        }

        /// <summary>
        /// Hit-testing with lasso
        /// </summary>
        /// <param name="lassoPoints">points making the lasso</param>
        /// <param name="percentageWithinLasso">the margin value to tell whether a stroke
        /// is in or outside of the rect</param>
        /// <returns>collection of strokes found inside the rectangle</returns>
        public StrokeCollection HitTest(IEnumerable<Point> lassoPoints, int percentageWithinLasso)
        {
            // Check the input parameters
            if (lassoPoints == null)
            {
                throw new System.ArgumentNullException("lassoPoints");
            }
            if ((percentageWithinLasso < 0) || (percentageWithinLasso > 100))
            {
                throw new System.ArgumentOutOfRangeException("percentageWithinLasso");
            }

            if (IEnumerablePointHelper.GetCount(lassoPoints) < 3)
            {
                return new StrokeCollection();
            }

            Lasso lasso = new SingleLoopLasso();
            lasso.AddPoints(lassoPoints);

            // Enumerate through the strokes and collect those captured by the lasso.
            StrokeCollection lassoedStrokes = new StrokeCollection();
            foreach (Stroke stroke in this)
            {
                if (percentageWithinLasso == 0)
                {
                    lassoedStrokes.Add(stroke);
                }
                else
                {
                    StrokeInfo strokeInfo = null;
                    try
                    {
                        strokeInfo = new StrokeInfo(stroke);

                        StylusPointCollection stylusPoints = strokeInfo.StylusPoints;
                        double target = strokeInfo.TotalWeight * percentageWithinLasso / 100.0f - Stroke.PercentageTolerance;

                        for (int i = 0; i < stylusPoints.Count; i++)
                        {
                            if (true == lasso.Contains((Point)stylusPoints[i]))
                            {
                                target -= strokeInfo.GetPointWeight(i);
                                if (DoubleUtil.LessThanOrClose(target, 0f))
                                {
                                    lassoedStrokes.Add(stroke);
                                    break;
                                }
                            }
                        }
                    }
                    finally
                    {
                        if (strokeInfo != null)
                        {
                            //detach from event handlers, or else we leak.
                            strokeInfo.Detach();
                        }
                    }
                }
            }

            // Return the resulting collection
            return lassoedStrokes;
        }


        /// <summary>
        /// Hit-testing with rectangle
        /// </summary>
        /// <param name="bounds">hitting rectangle</param>
        /// <param name="percentageWithinBounds">the percentage of the stroke that must be within 
        /// the bounds to be considered hit</param>
        /// <returns>collection of strokes found inside the rectangle</returns>
        public StrokeCollection HitTest(Rect bounds, int percentageWithinBounds)
        {
            // Check the input parameters
            if ((percentageWithinBounds < 0) || (percentageWithinBounds > 100))
            {
                throw new System.ArgumentOutOfRangeException("percentageWithinBounds");
            }
            if (bounds.IsEmpty)
            {
                return new StrokeCollection();
            }

            // Enumerate thru the strokes collect those found within the rectangle.
            StrokeCollection hits = new StrokeCollection();
            foreach (Stroke stroke in this)
            {
                // samgeo - Presharp issue
                // Presharp gives a warning when get methods might deref a null.  It's complaining
                // here that 'stroke'' could be null, but StrokeCollection never allows nulls to be added
                // so this is not possible
#pragma warning disable 1634, 1691
#pragma warning suppress 6506
                if (true == stroke.HitTest(bounds, percentageWithinBounds))
                {
                    hits.Add(stroke);
                }
#pragma warning restore 1634, 1691
            }
            return hits;
        }


        /// <summary>
        /// Issue: what's the return value
        /// </summary>
        /// <param name="path"></param>
        /// <param name="stylusShape"></param>
        /// <returns></returns>
        public StrokeCollection HitTest(IEnumerable<Point> path, StylusShape stylusShape)
        {
            // Check the input parameters
            if (stylusShape == null)
            {
                throw new System.ArgumentNullException("stylusShape");
            }
            if (path == null)
            {
                throw new System.ArgumentNullException("path");
            }
            if (IEnumerablePointHelper.GetCount(path) == 0)
            {
                return new StrokeCollection();
            }

            // validate input
            ErasingStroke erasingStroke = new ErasingStroke(stylusShape, path);
            Rect erasingBounds = erasingStroke.Bounds;
            if (erasingBounds.IsEmpty)
            {
                return new StrokeCollection();
            }
            StrokeCollection hits = new StrokeCollection();
            foreach (Stroke stroke in this)
            {
                // samgeo - Presharp issue
                // Presharp gives a warning when get methods might deref a null.  It's complaining
                // here that 'stroke'' could be null, but StrokeCollection never allows nulls to be added
                // so this is not possible
#pragma warning disable 1634, 1691
#pragma warning suppress 6506
                if (erasingBounds.IntersectsWith(stroke.GetBounds()) &&
                    erasingStroke.HitTest(StrokeNodeIterator.GetIterator(stroke, stroke.DrawingAttributes)))
                {
                    hits.Add(stroke);
                }
#pragma warning restore 1634, 1691
            }

            return hits;
        }

        /// <summary>
        /// Clips out all ink outside a given lasso
        /// </summary>
        /// <param name="lassoPoints">lasso</param>
        public void Clip(IEnumerable<Point> lassoPoints)
        {
            // Check the input parameters
            if (lassoPoints == null)
            {
                throw new System.ArgumentNullException("lassoPoints");
            }

            int length = IEnumerablePointHelper.GetCount(lassoPoints);
            if (length == 0)
            {
                throw new ArgumentException(SR.Get(SRID.EmptyArray));
            }

            if (length < 3)
            {
                //
                // if you're clipping with a point or a line with 
                // two points, it doesn't matter where the line is or if it
                // intersects any of the strokes, the point or line has no region
                // so technically everything in the strokecollection
                // should be removed
                //
                this.Clear(); //raises the appropriate events
                return;
            }

            Lasso lasso = new SingleLoopLasso();
            lasso.AddPoints(lassoPoints);

            for (int i = 0; i < this.Count; i++)
            {
                Stroke stroke = this[i];
                StrokeCollection clipResult = stroke.Clip(stroke.HitTest(lasso));
                UpdateStrokeCollection(stroke, clipResult, ref i);
            }
        }

        /// <summary>
        /// Clips out all ink outside a given rectangle.
        /// </summary>
        /// <param name="bounds">rectangle to clip with</param>
        public void Clip(Rect bounds)
        {
            if (bounds.IsEmpty == false)
            {
                Clip(new Point[4] { bounds.TopLeft, bounds.TopRight, bounds.BottomRight, bounds.BottomLeft });
            }
        }

        /// <summary>
        /// Erases all ink inside a lasso
        /// </summary>
        /// <param name="lassoPoints">lasso to erase within</param>
        public void Erase(IEnumerable<Point> lassoPoints)
        {
            // Check the input parameters
            if (lassoPoints == null)
            {
                throw new System.ArgumentNullException("lassoPoints");
            }
            int length = IEnumerablePointHelper.GetCount(lassoPoints);
            if (length == 0)
            {
                throw new ArgumentException(SR.Get(SRID.EmptyArray));
            }

            if (length < 3)
            {
                return;
            }

            Lasso lasso = new SingleLoopLasso();
            lasso.AddPoints(lassoPoints);
            for (int i = 0; i < this.Count; i++)
            {
                Stroke stroke = this[i];

                StrokeCollection eraseResult = stroke.Erase(stroke.HitTest(lasso));
                UpdateStrokeCollection(stroke, eraseResult, ref i);
            }
        }


        /// <summary>
        /// Erases all ink inside a given rectangle
        /// </summary>
        /// <param name="bounds">rectangle to erase within</param>
        public void Erase(Rect bounds)
        {
            if (bounds.IsEmpty == false)
            {
                Erase(new Point[4] { bounds.TopLeft, bounds.TopRight, bounds.BottomRight, bounds.BottomLeft });
            }
        }


        /// <summary>
        /// Erases all ink hit by the contour of an erasing stroke
        /// </summary>
        /// <param name="eraserShape">Shape of the eraser</param>
        /// <param name="eraserPath">a path making the spine of the erasing stroke </param>
        public void Erase(IEnumerable<Point> eraserPath, StylusShape eraserShape)
        {
            // Check the input parameters
            if (eraserShape == null)
            {
                throw new System.ArgumentNullException(SR.Get(SRID.SCEraseShape));
            }
            if (eraserPath == null)
            {
                throw new System.ArgumentNullException(SR.Get(SRID.SCErasePath));
            }
            if (IEnumerablePointHelper.GetCount(eraserPath) == 0)
            {
                return;
            }

            ErasingStroke erasingStroke = new ErasingStroke(eraserShape, eraserPath);
            for (int i = 0; i < this.Count; i++)
            {
                Stroke stroke = this[i];

                List<StrokeIntersection> intersections = new List<StrokeIntersection>();
                erasingStroke.EraseTest(StrokeNodeIterator.GetIterator(stroke, stroke.DrawingAttributes), intersections);
                StrokeCollection eraseResult = stroke.Erase(intersections.ToArray());

                UpdateStrokeCollection(stroke, eraseResult, ref i);
            }
        }

        /// <summary>
        /// Render the StrokeCollection under the specified DrawingContext.
        /// </summary>
        /// <param name="context"></param>
        public void Draw(DrawingContext context)
        {
             if (null == context)
            {
                throw new System.ArgumentNullException("context");
            }

            //The verification of UI context affinity is done in Stroke.Draw()

            List<Stroke> solidStrokes = new List<Stroke>();
            Dictionary<Color, List<Stroke>> highLighters = new Dictionary<Color, List<Stroke>>();

            for (int i = 0; i < this.Count; i++)
            {
                Stroke stroke = this[i];
                List<Stroke> strokes;
                if (stroke.DrawingAttributes.IsHighlighter)
                {
                    // It's very important to override the Alpha value so that Colors of the same RGB vale
                    // but different Alpha would be in the same list.
                    Color color = StrokeRenderer.GetHighlighterColor(stroke.DrawingAttributes.Color);
                    if (highLighters.TryGetValue(color, out strokes) == false)
                    {
                        strokes = new List<Stroke>();
                        highLighters.Add(color, strokes);
                    }
                    strokes.Add(stroke);
                }
                else
                {
                    solidStrokes.Add(stroke);
                }
            }

            foreach (List<Stroke> strokes in highLighters.Values)
            {
                context.PushOpacity(StrokeRenderer.HighlighterOpacity);
                try
                {
                    foreach (Stroke stroke in strokes)
                    {
                        stroke.DrawInternal(context, StrokeRenderer.GetHighlighterAttributes(stroke, stroke.DrawingAttributes),
                                            false /*Don't draw selected stroke as hollow*/);
                    }
                }
                finally
                {
                    context.Pop();
                }
            }

            foreach(Stroke stroke in solidStrokes)
            {
                stroke.DrawInternal(context, stroke.DrawingAttributes, false/*Don't draw selected stroke as hollow*/);
            }
        }
        #endregion

        #region Incremental hit-testing

        /// <summary>
        /// Creates an incremental hit-tester for hit-testing with a shape.
        /// Scenarios: stroke-erasing and point-erasing
        /// </summary>
        /// <param name="eraserShape">shape of the eraser</param>
        /// <returns>an instance of IncrementalStrokeHitTester</returns>
        public IncrementalStrokeHitTester GetIncrementalStrokeHitTester(StylusShape eraserShape)
        {
            if (eraserShape == null)
            {
                throw new System.ArgumentNullException("eraserShape");
            }
            return new IncrementalStrokeHitTester(this, eraserShape);
        }


        /// <summary>
        /// Creates an incremental hit-tester for selecting with lasso.
        /// </summary>
        /// <param name="percentageWithinLasso">The percentage of the stroke that must be within the lasso to be considered hit</param>
        /// <returns>an instance of incremental hit-tester</returns>
        public IncrementalLassoHitTester GetIncrementalLassoHitTester(int percentageWithinLasso)
        {
            if ((percentageWithinLasso < 0) || (percentageWithinLasso > 100))
            {
                throw new System.ArgumentOutOfRangeException("percentageWithinLasso");
            }
            return new IncrementalLassoHitTester(this, percentageWithinLasso);
        }
        #endregion

        /// <summary>
        /// Return all hit strokes that the StylusShape intersects and returns them in a StrokeCollection
        /// </summary>
        private StrokeCollection PointHitTest(Point point, StylusShape shape)
        {
            // Create the collection to return
            StrokeCollection hits = new StrokeCollection();
            for (int i = 0; i < this.Count; i++)
            {
                Stroke stroke = this[i];
                if (stroke.HitTest(new Point[] { point }, shape))
                {
                    hits.Add(stroke);
                }
            }

            return hits;
        }

        private void UpdateStrokeCollection(Stroke original, StrokeCollection toReplace, ref int index)
        {
            System.Diagnostics.Debug.Assert(original != null && toReplace != null);
            System.Diagnostics.Debug.Assert(index >= 0 && index < this.Count);
            if (toReplace.Count == 0)
            {
                Remove(original);
                index--;
            }
            else if (!(toReplace.Count == 1 && toReplace[0] == original))
            {
                Replace(original, toReplace);

                // Update the current index
                index += toReplace.Count - 1;
            }
        }
    }
}
