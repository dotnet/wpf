// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//#define DEBUG_RENDERING_FEEDBACK

using MS.Utility;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MS.Internal;
using MS.Internal.Ink;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
using MS.Internal.PresentationCore;

// Primary root namespace for TabletPC/Ink/Handwriting/Recognition in .NET

namespace System.Windows.Ink
{
    /// <summary>
    /// The hit-testing API of Stroke
    /// </summary>
    public partial class Stroke : INotifyPropertyChanged
    {
        #region Public APIs

        #region Public Methods

        /// <summary>
        /// Computes the bounds of the stroke in the default rendering context
        /// </summary>
        /// <returns></returns>
        public virtual Rect GetBounds()
        {
            if (_cachedBounds.IsEmpty)
            {
                StrokeNodeIterator iterator = StrokeNodeIterator.GetIterator(this, this.DrawingAttributes);
                for (int i = 0; i < iterator.Count; i++)
                {
                    StrokeNode strokeNode = iterator[i];
                    _cachedBounds.Union(strokeNode.GetBounds());
                }
            }

            return _cachedBounds;
        }

        /// <summary>
        /// Render the Stroke under the specified DrawingContext. The draw method is a
        /// batch operationg that uses the rendering methods exposed off of DrawingContext
        /// </summary>
        /// <param name="context"></param>
        public void Draw(DrawingContext context)
        {
            if (null == context)
            {
                throw new System.ArgumentNullException("context");
            }

            //our code never calls this public API so we can assume that opacity
            //has not been set up

            //call our public Draw method with the strokes.DA
            this.Draw(context, this.DrawingAttributes);
        }


        /// <summary>
        /// Render the StrokeCollection under the specified DrawingContext. This draw method uses the
        /// passing in drawing attribute to override that on the stroke.
        /// </summary>
        /// <param name="drawingContext"></param>
        /// <param name="drawingAttributes"></param>
        public void Draw(DrawingContext drawingContext, DrawingAttributes drawingAttributes)
        {
            if (null == drawingContext)
            {
                throw new System.ArgumentNullException("context");
            }

            if (null == drawingAttributes)
            {
                throw new System.ArgumentNullException("drawingAttributes");
            }

            //             context.VerifyAccess();

            //our code never calls this public API so we can assume that opacity
            //has not been set up

            if (drawingAttributes.IsHighlighter)
            {
                drawingContext.PushOpacity(StrokeRenderer.HighlighterOpacity);
                try
                {
                    this.DrawInternal(drawingContext, StrokeRenderer.GetHighlighterAttributes(this, this.DrawingAttributes), false);
                }
                finally
                {
                    drawingContext.Pop();
                }
            }
            else
            {
                this.DrawInternal(drawingContext, drawingAttributes, false);
            }
        }


        /// <summary>
        /// Clip with rect. Calculate the after-clipping Strokes. Only the "in-segments" are left after this operation.
        /// </summary>
        /// <param name="bounds">A Rect to clip with</param>
        /// <returns>The after-clipping strokes.</returns>
        public StrokeCollection GetClipResult(Rect bounds)
        {
            return this.GetClipResult(new Point[4] { bounds.TopLeft, bounds.TopRight, bounds.BottomRight, bounds.BottomLeft });
        }


        /// <summary>
        /// Clip with lasso. Calculate the after-clipping Strokes. Only the "in-segments" are left after this operation.
        /// </summary>
        /// <param name="lassoPoints">The lasso points to clip with</param>
        /// <returns>The after-clipping strokes</returns>
        public StrokeCollection GetClipResult(IEnumerable<Point> lassoPoints)
        {
            // Check the input parameters
            if (lassoPoints == null)
            {
                throw new System.ArgumentNullException("lassoPoints");
            }

            if (IEnumerablePointHelper.GetCount(lassoPoints) == 0)
            {
                throw new ArgumentException(SR.Get(SRID.EmptyArray));
            }

            Lasso lasso = new SingleLoopLasso();
            lasso.AddPoints(lassoPoints);
            return this.Clip(this.HitTest(lasso));
        }


        /// <summary>
        /// Erase with a rect. Calculate the after-erasing Strokes. Only the "out-segments" are left after this operation.
        /// </summary>
        /// <param name="bounds">A Rect to clip with</param>
        /// <returns>The after-erasing strokes</returns>
        public StrokeCollection GetEraseResult(Rect bounds)
        {
            return this.GetEraseResult(new Point[4] { bounds.TopLeft, bounds.TopRight, bounds.BottomRight, bounds.BottomLeft });
        }

        /// <summary>
        /// Erase with lasso points.
        /// </summary>
        /// <param name="lassoPoints">Lasso points to erase with</param>
        /// <returns>The after-erasing strokes</returns>
        public StrokeCollection GetEraseResult(IEnumerable<Point> lassoPoints)
        {
            // Check the input parameters
            if (lassoPoints == null)
            {
                throw new System.ArgumentNullException("lassoPoints");
            }

            if (IEnumerablePointHelper.GetCount(lassoPoints) == 0)
            {
                throw new ArgumentException(SR.Get(SRID.EmptyArray));
            }

            Lasso lasso = new SingleLoopLasso();
            lasso.AddPoints(lassoPoints);
            return this.Erase(this.HitTest(lasso));
        }

        /// <summary>
        /// Erase with an eraser with passed in shape
        /// </summary>
        /// <param name="eraserPath">The path to erase</param>
        /// <param name="eraserShape">Shape of the eraser</param>
        /// <returns></returns>
        public StrokeCollection GetEraseResult(IEnumerable<Point> eraserPath, StylusShape eraserShape)
        {
            // Check the input parameters
            if (eraserShape == null)
            {
                throw new System.ArgumentNullException("eraserShape");
            }
            if (eraserPath == null)
            {
                throw new System.ArgumentNullException("eraserPath");
            }

            return this.Erase(this.EraseTest(eraserPath, eraserShape));
        }


        /// <summary>
        /// Tap-hit. Hit tests with a point. Internally does Stroke.HitTest(Point, 1pxlRectShape).
        /// </summary>
        /// <param name="point">The location to do the hitest</param>
        /// <returns>True is this stroke is hit, false otherwise</returns>
        public bool HitTest(Point point)
        {
            return HitTest(new Point[]{point}, new EllipseStylusShape(TapHitPointSize, TapHitPointSize, TapHitRotation));
        }

        /// <summary>
        /// Tap-hit. Hit tests with a point.
        /// </summary>
        /// <param name="point">The location to do the hittest</param>
        /// <param name="diameter">diameter of the tip</param>
        /// <returns>true if hit, false otherwise</returns>
        public bool HitTest(Point point, double diameter)
        {
            if (Double.IsNaN(diameter) || diameter < DrawingAttributes.MinWidth || diameter > DrawingAttributes.MaxWidth)
            {
                throw new ArgumentOutOfRangeException("diameter", SR.Get(SRID.InvalidDiameter));
            }
            return HitTest(new Point[]{point}, new EllipseStylusShape(diameter, diameter, TapHitRotation));
        }

        /// <summary>
        /// Check whether a certain percentage of the stroke is within the Rect passed in.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="percentageWithinBounds"></param>
        /// <returns></returns>
        public bool HitTest(Rect bounds, int percentageWithinBounds)
        {
            if ((percentageWithinBounds < 0) || (percentageWithinBounds > 100))
            {
                throw new System.ArgumentOutOfRangeException("percentageWithinBounds");
            }

            if (percentageWithinBounds == 0)
            {
                return true;
            }

            StrokeInfo strokeInfo = null;
            try
            {
                strokeInfo = new StrokeInfo(this);

                StylusPointCollection stylusPoints = strokeInfo.StylusPoints;
                double target = strokeInfo.TotalWeight * percentageWithinBounds / 100.0f - PercentageTolerance;

                for (int i = 0; i < stylusPoints.Count; i++)
                {
                    if (true == bounds.Contains((Point)stylusPoints[i]))
                    {
                        target -= strokeInfo.GetPointWeight(i);
                        if (DoubleUtil.LessThanOrClose(target, 0d))
                        {
                            return true;
                        }
                    }
                }

                return false;
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

        /// <summary>
        /// Check whether a certain percentage of the stroke is within the lasso
        /// </summary>
        /// <param name="lassoPoints"></param>
        /// <param name="percentageWithinLasso"></param>
        /// <returns></returns>
        public bool HitTest(IEnumerable<Point> lassoPoints, int percentageWithinLasso)
        {
            if (lassoPoints == null)
            {
                throw new System.ArgumentNullException("lassoPoints");
            }

            if ((percentageWithinLasso < 0) || (percentageWithinLasso > 100))
            {
                throw new System.ArgumentOutOfRangeException("percentageWithinLasso");
            }

            if (percentageWithinLasso == 0)
            {
                return true;
            }


            StrokeInfo strokeInfo = null;
            try
            {
                strokeInfo = new StrokeInfo(this);

                StylusPointCollection stylusPoints = strokeInfo.StylusPoints;
                double target = strokeInfo.TotalWeight * percentageWithinLasso / 100.0f - PercentageTolerance;

                Lasso lasso = new SingleLoopLasso();
                lasso.AddPoints(lassoPoints);

                for (int i = 0; i < stylusPoints.Count; i++)
                {
                    if (true == lasso.Contains((Point)stylusPoints[i]))
                    {
                        target -= strokeInfo.GetPointWeight(i);
                        if (DoubleUtil.LessThan(target, 0f))
                        {
                            return true;
                        }
                    }
                }

                return false;
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

        /// <summary>
        ///
        /// </summary>
        /// <param name="path"></param>
        /// <param name="stylusShape"></param>
        /// <returns></returns>
        public bool HitTest(IEnumerable<Point> path, StylusShape stylusShape)
        {
            // Check the input parameters
            if (path == null)
            {
                throw new System.ArgumentNullException("path");
            }
            if (stylusShape == null)
            {
                throw new System.ArgumentNullException("stylusShape");
            }

            if (IEnumerablePointHelper.GetCount(path) == 0)
            {
                return false;
            }

            ErasingStroke erasingStroke = new ErasingStroke(stylusShape);
            erasingStroke.MoveTo(path);

            Rect erasingBounds = erasingStroke.Bounds;

            if (erasingBounds.IsEmpty)
            {
                return false;
            }

            if (erasingBounds.IntersectsWith(this.GetBounds()))
            {
                return erasingStroke.HitTest(StrokeNodeIterator.GetIterator(this, this.DrawingAttributes));
            }

            return false;
        }

        #endregion

        #endregion

        #region Protected APIs

        /// <summary>
        /// The core functionality to draw a stroke. The function can be called from the following code paths.
        ///     i) From StrokeVisual.OnRender
        ///         a. Highlighter strokes have been grouped and the correct opacity has been set on the container visual.
        ///         b. For a highlighter stroke with color.A != 255, the DA passed in is a copy with color.A set to 255.
        ///         c. _drawAsHollow can be true, i.e., Selected stroke is drawn as hollow
        ///     ii) From StrokeCollection.Draw.
        ///         a. Highlighter strokes have been grouped and the correct opacity has been pushed.
        ///         b. For a highlighter stroke with color.A != 255, the DA passed in is a copy with color.A set to 255.
        ///         c. _drawAsHollow is always false, i.e., Selected stroke is not drawn as hollow
        ///     iii) From Stroke.Draw
        ///         a. The correct opacity has been pushed for a highlighter stroke
        ///         b. For a highlighter stroke with color.A != 255, the DA passed in is a copy with color.A set to 255.
        ///         c. _drawAsHollow is always false, i.e., Selected stroke is not drawn as hollow
        /// We need to document the following:
        /// 1) our default implementation so developers can see what we've done here -
        ///    including how we handle IsHollow
        /// 2) the fact that opacity has already been set up correctly for the call.
        /// 3) that developers should not call base.DrawCore if they override this
        /// </summary>
        /// <param name="drawingContext">DrawingContext to draw on</param>
        /// <param name="drawingAttributes">DrawingAttributes to draw with</param>
        protected virtual void DrawCore(DrawingContext drawingContext, DrawingAttributes drawingAttributes)
        {
            if (null == drawingContext)
            {
                throw new System.ArgumentNullException("drawingContext");
            }

            if (null == drawingAttributes)
            {
                throw new System.ArgumentNullException("drawingAttributes");
            }

            if (_drawAsHollow == true)
            {
                // Draw as hollow. Our profiler result shows that the two-pass-rendering approach is about 5 times
                // faster that using GetOutlinePathGeometry.
                // also, the minimum display size for selected ink is our default width / height

                Matrix innerTransform, outerTransform;
                DrawingAttributes selectedDA = drawingAttributes.Clone();
                selectedDA.Height = Math.Max(selectedDA.Height, DrawingAttributes.DefaultHeight);
                selectedDA.Width = Math.Max(selectedDA.Width, DrawingAttributes.DefaultWidth);
                CalcHollowTransforms(selectedDA, out innerTransform, out outerTransform);

                // First pass drawing. Use drawingAttributes.Color to create a solid color brush. The stroke will be drawn as
                // 1 avalon-unit higher and wider (HollowLineSize = 1.0f)
                selectedDA.StylusTipTransform = outerTransform;
                SolidColorBrush brush = new SolidColorBrush(drawingAttributes.Color);
                brush.Freeze();
                drawingContext.DrawGeometry(brush, null, GetGeometry(selectedDA));

                //Second pass drawing with a white color brush. The stroke will be drawn as
                // 1 avalon-unit shorter and narrower (HollowLineSize = 1.0f) if the actual-width/height (considering StylusTipTransform)
                // is larger than HollowLineSize. Otherwise the same size stroke is drawn.
                selectedDA.StylusTipTransform = innerTransform;
                drawingContext.DrawGeometry(Brushes.White, null, GetGeometry(selectedDA));
            }
            else
            {
#if DEBUG_RENDERING_FEEDBACK
                //render debug feedback?
                Guid guid = new Guid("52053C24-CBDD-4547-AAA1-DEFEBF7FD1E1");
                if (this.ContainsPropertyData(guid))
                {
                    double thickness = (double)this.GetPropertyData(guid);

                    //first, draw the outline of the stroke
                    drawingContext.DrawGeometry(null,
                                                new Pen(Brushes.Black, thickness),
                                                GetGeometry());

                    Geometry g2;
                    Rect b2;
                    //next, overlay the connecting quad points
                    StrokeRenderer.CalcGeometryAndBounds(StrokeNodeIterator.GetIterator(this, drawingAttributes),
                                                         drawingAttributes,
                                                         drawingContext, thickness, true,
                                                         true, //calc bounds
                                                         out g2,
                                                         out b2);
                    
                }
                else
                {
#endif
                SolidColorBrush brush = new SolidColorBrush(drawingAttributes.Color);
                brush.Freeze();
                drawingContext.DrawGeometry(brush, null, GetGeometry(drawingAttributes));
#if DEBUG_RENDERING_FEEDBACK
                }
#endif
            }
        }

        /// <summary>
        /// Returns the Geometry of this stroke.
        /// </summary>
        /// <returns></returns>
        public Geometry GetGeometry()
        {
            return GetGeometry(this.DrawingAttributes);
        }

        /// <summary>
        /// Get the Geometry of the Stroke
        /// </summary>
        /// <param name="drawingAttributes"></param>
        /// <returns></returns>
        public Geometry GetGeometry(DrawingAttributes drawingAttributes)
        {
            if (drawingAttributes == null)
            {
                throw new ArgumentNullException("drawingAttributes");
            }

            bool geometricallyEqual = DrawingAttributes.GeometricallyEqual(drawingAttributes, this.DrawingAttributes);

            // need to recalculate the PathGemetry if the DA passed in is "geometrically" different from
            // this DA, or if the cached PathGeometry is dirty.
            if (false == geometricallyEqual || (true == geometricallyEqual && null == _cachedGeometry))
            {
                //Recalculate _pathGeometry;
                StrokeNodeIterator iterator = StrokeNodeIterator.GetIterator(this, drawingAttributes);
                Geometry geometry;
                Rect bounds;
                StrokeRenderer.CalcGeometryAndBounds(iterator,
                                                     drawingAttributes,
#if DEBUG_RENDERING_FEEDBACK
                                                     null, 0d, false,
#endif
                                                     true, //calc bounds
                                                     out geometry,
                                                     out bounds);

                // return the calculated value directly. We cannot cache the result since the DA passed in
                // is "geometrically" different from this.DrawingAttributes.
                if (false == geometricallyEqual)
                {
                    return geometry;
                }

                // Cache the value and set _isPathGeometryDirty to false;
                SetGeometry(geometry);
                SetBounds(bounds);

                return geometry;
            }

            // return a ref to our _cachedGeometry
            System.Diagnostics.Debug.Assert(_cachedGeometry != null && _cachedGeometry.IsFrozen);
            return _cachedGeometry;
        }

        #endregion

        #region Internal APIs

        /// <summary>
        /// our code - StrokeVisual.OnRender and StrokeCollection.Draw - always calls this
        /// so we can assume the correct opacity has already been pushed on dc. The flag drawAsHollow is set
        /// to true when this function is called from Renderer and this.IsSelected == true.
        /// </summary>
        [FriendAccessAllowed] // Built into Core, also used by Framework.
        internal void DrawInternal(DrawingContext dc, DrawingAttributes DrawingAttributes, bool drawAsHollow)
        {
            if (drawAsHollow == true)
            {
                // The Stroke.DrawCore may be overriden in the 3rd party code.
                // The out-side code could throw exception. We use try/finally block to protect our status.
                try
                {
                    _drawAsHollow = true;  // temporarily set the flag to be true
                    this.DrawCore(dc, DrawingAttributes);
                }
                finally
                {
                    _drawAsHollow = false;  // reset _drawAsHollow
                }
            }
            else
            {
                // IsSelected can be true or false, but _drawAsHollow must be false
                System.Diagnostics.Debug.Assert(false == _drawAsHollow);
                this.DrawCore(dc, DrawingAttributes);
            }
        }


        /// <summary>
        /// Used by Inkcanvas to draw selected stroke as hollow.
        /// </summary>
        [FriendAccessAllowed] // Built into Core, also used by Framework.
        internal bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;

                    // Raise Invalidated event. This will cause Renderer to repaint and call back DrawCore
                    OnInvalidated(EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Set the path geometry
        /// </summary>
        internal void SetGeometry(Geometry geometry)
        {
            System.Diagnostics.Debug.Assert(geometry != null);
            _cachedGeometry = geometry;
        }

        /// <summary>
        /// Set the bounds
        /// </summary>
        internal void SetBounds(Rect newBounds)
        {
            System.Diagnostics.Debug.Assert(newBounds.IsEmpty == false);
            _cachedBounds = newBounds;
        }

        /// <summary>Hit tests all segments within a contour generated with shape and path</summary>
        /// <param name="shape"></param>
        /// <param name="path"></param>
        /// <returns>StrokeIntersection array for these segments</returns>
        internal StrokeIntersection[] EraseTest(IEnumerable<Point> path, StylusShape shape)
        {
            System.Diagnostics.Debug.Assert(shape != null);
            System.Diagnostics.Debug.Assert(path != null);
            if (IEnumerablePointHelper.GetCount(path) == 0)
            {
                return new StrokeIntersection[0];
            }

            ErasingStroke erasingStroke = new ErasingStroke(shape, path);
            List<StrokeIntersection> intersections = new List<StrokeIntersection>();
            erasingStroke.EraseTest(StrokeNodeIterator.GetIterator(this, this.DrawingAttributes), intersections);
            return intersections.ToArray();
        }

        /// <summary>
        /// Hit tests all segments within the lasso loops
        /// </summary>
        /// <returns> a StrokeIntersection array for these segments</returns>
        internal StrokeIntersection[] HitTest(Lasso lasso)
        {
            // Check the input parameters
            System.Diagnostics.Debug.Assert(lasso != null);
            if (lasso.IsEmpty)
            {
                return new StrokeIntersection[0];
            }

            // The following will check whether all the points are within the lasso.
            // If yes, return the whole stroke as being hit.
            if (!lasso.Bounds.IntersectsWith(this.GetBounds()))
            {
                return new StrokeIntersection[0];
            }
            return lasso.HitTest(StrokeNodeIterator.GetIterator(this, this.DrawingAttributes));
        }


        /// <summary>
        /// Calculate the after-erasing Strokes. Only the "out-segments" are left after this operation.
        /// </summary>
        /// <param name="cutAt">Array of intersections indicating the erasing locations</param>
        /// <returns></returns>
        internal StrokeCollection Erase(StrokeIntersection[] cutAt)
        {
            System.Diagnostics.Debug.Assert(cutAt != null);

            // Nothing needs to be erased
            if(cutAt.Length == 0)
            {
                StrokeCollection strokes = new StrokeCollection();
                strokes.Add(this.Clone()); //clip and erase always return clones for this condition
                return strokes;
            }

            // Two assertions are deferred to the private erase function to avoid duplicate code.
            // 1. AssertSortedNoOverlap
            // 2. Check whether the insegments are out of range with the packets
            StrokeFIndices[] hitSegments = StrokeIntersection.GetHitSegments(cutAt);
            return this.Erase(hitSegments);
        }

        /// <summary>
        /// Calculate the after-clipping Strokes. Only the "in-segments" are left after this operation.
        /// </summary>
        /// <param name="cutAt">Array of intersections indicating the clipping locations</param>
        /// <returns>The resulting StrokeCollection</returns>
        internal StrokeCollection Clip(StrokeIntersection[] cutAt)
        {
            System.Diagnostics.Debug.Assert(cutAt != null);

            // Nothing is inside
            if (cutAt.Length == 0)
            {
                return new StrokeCollection();
            }


            // Get the "in-segments"
            StrokeFIndices[] inSegments = StrokeIntersection.GetInSegments(cutAt);

            // For special case like cutAt is {BF, AL, BF, 0.67}, the inSegments are empty
            if (inSegments.Length == 0)
            {
                return new StrokeCollection();
            }

            // Two other validations are deferred to the private clip function to avoid duplicate code.
            // 1. ValidateSortedNoOverlap
            // 2. Check whether the insegments are out of range with the packets
            return this.Clip(inSegments);
        }


        internal double TapHitPointSize = 1.0;
        internal double TapHitRotation = 0;
        #endregion

        #region Private APIs

        /// <summary>
        /// Calculate the two transforms for two-pass rendering used to draw as hollow. The resulting outerTransform will make the
        /// first-pass-rendering 1 avalon-unit wider/heigher. The resulting innerTransform will make the second-pass-rendering 1 avalon-unit
        /// narrower/shorter.
        /// </summary>
        private static void CalcHollowTransforms(DrawingAttributes originalDa, out Matrix innerTransform, out Matrix outerTransform)
        {
            System.Diagnostics.Debug.Assert(DoubleUtil.IsZero(originalDa.StylusTipTransform.OffsetX) && DoubleUtil.IsZero(originalDa.StylusTipTransform.OffsetY));

            innerTransform = outerTransform = Matrix.Identity;
            Point w = originalDa.StylusTipTransform.Transform(new Point(originalDa.Width, 0));
            Point h = originalDa.StylusTipTransform.Transform(new Point(0, originalDa.Height));

            // the newWidth and newHeight are the actual width/height of the stylus shape considering StylusTipTransform.
            // The assumption is TylusTipTransform has no translation component.
            double newWidth = Math.Sqrt(w.X * w.X + w.Y * w.Y);
            double newHeight = Math.Sqrt(h.X * h.X + h.Y * h.Y);

            double xTransform = DoubleUtil.GreaterThan(newWidth, HollowLineSize) ?
                                (newWidth - HollowLineSize) / newWidth : 1.0f;
            double yTransform = DoubleUtil.GreaterThan(newHeight, HollowLineSize) ?
                                (newHeight - HollowLineSize) / newHeight : 1.0f;

            innerTransform.Scale(xTransform, yTransform);
            innerTransform *= originalDa.StylusTipTransform;

            outerTransform.Scale((newWidth + HollowLineSize) / newWidth,
                                 (newHeight + HollowLineSize) / newHeight);
            outerTransform *= originalDa.StylusTipTransform;
        }

        #region Private fields

        private Geometry                _cachedGeometry     = null;
        private bool                    _isSelected         = false;
        private bool                    _drawAsHollow       = false;
        private bool                    _cloneStylusPoints  = true;
        private bool                    _delayRaiseInvalidated  = false;
        private static readonly double  HollowLineSize      = 1.0f;
        private Rect                    _cachedBounds       = Rect.Empty;

        // The private PropertyChanged event
        private PropertyChangedEventHandler _propertyChanged;

        private const string DrawingAttributesName = "DrawingAttributes";
        private const string StylusPointsName = "StylusPoints";

        #endregion

        internal static readonly double PercentageTolerance = 0.0001d;
        #endregion
    }
}
