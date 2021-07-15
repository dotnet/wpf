// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//#define DEBUG_LASSO_FEEDBACK // DO NOT LEAVE ENABLED IN CHECKED IN CODE
//
// Description:
//      LassoSelectionBehavior
//


using MS.Internal.Controls;
using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Ink;
using System.Windows.Interop;
using System.Windows.Navigation;
using System.Windows.Media;

namespace MS.Internal.Ink
{
    /// <summary>
    /// Eraser Behavior
    /// </summary>
    internal sealed class LassoSelectionBehavior : StylusEditingBehavior
    {
        //-------------------------------------------------------------------------------
        //
        // Constructors
        //
        //-------------------------------------------------------------------------------

        #region Constructors

        internal LassoSelectionBehavior(EditingCoordinator editingCoordinator, InkCanvas inkCanvas)
            : base(editingCoordinator, inkCanvas)
        {
        }

        #endregion Constructors

        //-------------------------------------------------------------------------------
        //
        // Protected Methods
        //
        //-------------------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Overrides SwitchToMode as the following expectations
        /// 31. From Select To InkAndGesture
        ///     Lasso is discarded. After mode change ink is being collected, gesture event fires. If its not a gesture, StrokeCollected event fires.
        /// 32. From Select To GestureOnly
        ///     Lasso is discarded. After mode change ink is being collected. On StylusUp gesture event fires. Stroke gets removed on StylusUp even if its not a gesture.
        /// 33. From Select To EraseByPoint
        ///     Lasso is discarded. PointErasing is performed after changing the mode.
        /// 34. From Select To EraseByStroke
        ///     Lasso is discarded. StrokeErasing is performed after changing the mode.
        /// 35. From Select To Ink
        ///     Ink collection starts when changing the mode.
        /// 36. From Select To None
        ///     Nothing gets selected.
        /// </summary>
        /// <param name="mode"></param>
        protected override void OnSwitchToMode(InkCanvasEditingMode mode)
        {
            Debug.Assert(EditingCoordinator.IsInMidStroke, "SwitchToMode should only be called in a mid-stroke");

            switch ( mode )
            {
                case InkCanvasEditingMode.Ink:
                case InkCanvasEditingMode.InkAndGesture:
                case InkCanvasEditingMode.GestureOnly:
                    {
                        // Discard the lasso
                        Commit(false);

                        // Change the mode. The dynamic renderer will be reset automatically.
                        EditingCoordinator.ChangeStylusEditingMode(this, mode);
                        break;
                    }
                case InkCanvasEditingMode.EraseByPoint:
                case InkCanvasEditingMode.EraseByStroke:
                    {
                        // Discard the lasso
                        Commit(false);

                        // Change the mode
                        EditingCoordinator.ChangeStylusEditingMode(this, mode);

                        break;
                    }
                case InkCanvasEditingMode.Select:
                    {
                        Debug.Assert(false, "Cannot switch from Select to Select in mid-stroke");
                        break;
                    }
                case InkCanvasEditingMode.None:
                    {
                        // Discard the lasso.
                        Commit(false);

                        // Change to the None mode
                        EditingCoordinator.ChangeStylusEditingMode(this, mode);
                        break;
                    }
                default:
                    Debug.Assert(false, "Unknown InkCanvasEditingMode!");
                    break;
            }
        }

        /// <summary>
        /// StylusInputBegin
        /// </summary>
        /// <param name="stylusPoints">stylusPoints</param>
        /// <param name="userInitiated">true if the source eventArgs.UserInitiated flag was set to true</param>
        protected override void StylusInputBegin(StylusPointCollection stylusPoints, bool userInitiated)
        {
            Debug.Assert(stylusPoints.Count != 0, "An empty stylusPoints has been passed in.");

            _disableLasso = false;

            bool startLasso = false;

            List<Point> points = new List<Point>();
            for ( int x = 0; x < stylusPoints.Count; x++ )
            {
                Point point = (Point)( stylusPoints[x] );
                if ( x == 0 )
                {
                    _startPoint = point;
                    points.Add(point);
                    continue;
                }

                if ( !startLasso )
                {
                    // If startLasso hasn't be flagged, we should check if the distance between two points is greater than 
                    // our tolerance. If so, we should flag startLasso.
                    Vector vector = point - _startPoint;
                    double distanceSquared = vector.LengthSquared;

                    if ( DoubleUtil.GreaterThan(distanceSquared, LassoHelper.MinDistanceSquared) )
                    {
                        points.Add(point);
                        startLasso = true;
                    }
                }
                else
                {
                    // The flag is set. We just add the point.
                    points.Add(point);
                }
            }

            // Start Lasso if it isn't a tap selection.
            if ( startLasso )
            {
                StartLasso(points);
            }
        }

        /// <summary>
        /// StylusInputContinue
        /// </summary>
        /// <param name="stylusPoints">stylusPoints</param>
        /// <param name="userInitiated">true if the source eventArgs.UserInitiated flag was set to true</param>
        protected override void StylusInputContinue(StylusPointCollection stylusPoints, bool userInitiated)
        {
            // Check whether Lasso has started.
            if ( _lassoHelper != null )
            {
                //
                // pump packets to the LassoHelper, it will convert them into an array of equidistant
                // lasso points, render those lasso point and return them to hit test against.
                //
                List<Point> points = new List<Point>();
                for ( int x = 0; x < stylusPoints.Count; x++ )
                {
                    points.Add((Point)stylusPoints[x]);
                }
                Point[] lassoPoints = _lassoHelper.AddPoints(points);
                if ( 0 != lassoPoints.Length )
                {
                    _incrementalLassoHitTester.AddPoints(lassoPoints);
                }
            }
            else if ( !_disableLasso )
            {
                // If Lasso hasn't start and been disabled, we should try to start it when it is needed.
                bool startLasso = false;

                List<Point> points = new List<Point>();
                for ( int x = 0; x < stylusPoints.Count; x++ )
                {
                    Point point = (Point)( stylusPoints[x] );

                    if ( !startLasso )
                    {
                        // If startLasso hasn't be flagged, we should check if the distance between two points is greater than 
                        // our tolerance. If so, we should flag startLasso.
                        Vector vector = point - _startPoint;
                        double distanceSquared = vector.LengthSquared;

                        if ( DoubleUtil.GreaterThan(distanceSquared, LassoHelper.MinDistanceSquared) )
                        {
                            points.Add(point);
                            startLasso = true;
                        }
                    }
                    else
                    {
                        // The flag is set. We just add the point.
                        points.Add(point);
                    }
                }

                // Start Lasso if it isn't a tap selection.
                if ( startLasso )
                {
                    StartLasso(points);
                }
            }
        }


        /// <summary>
        /// StylusInputEnd
        /// </summary>
        /// <param name="commit">commit</param>
        protected override void StylusInputEnd(bool commit)
        {
            // Initialize with empty selection
            StrokeCollection selectedStrokes = new StrokeCollection();
            List<UIElement> elementsToSelect = new List<UIElement>();

            if ( _lassoHelper != null )
            {
                // This is a lasso selection.

                //
                // end dynamic rendering
                //
                selectedStrokes = InkCanvas.EndDynamicSelection(_lassoHelper.Visual);

                //
                // hit test for elements
                //
                // NOTE: HitTestForElements uses the _lassoHelper so it must be alive at this point
                elementsToSelect = HitTestForElements();

                _incrementalLassoHitTester.SelectionChanged -= new LassoSelectionChangedEventHandler(OnSelectionChanged);
                _incrementalLassoHitTester.EndHitTesting();
                _incrementalLassoHitTester = null;
                _lassoHelper = null;
            }
            else
            {
                // This is a tap selection.

                // Now try the tap selection
                Stroke tappedStroke;
                UIElement tappedElement;

                TapSelectObject(_startPoint, out tappedStroke, out tappedElement);

                // If we have a pre-selected object, we should select it now.
                if ( tappedStroke != null )
                {
                    Debug.Assert(tappedElement == null);
                    selectedStrokes = new StrokeCollection();
                    selectedStrokes.Add(tappedStroke);
                }
                else if ( tappedElement != null )
                {
                    Debug.Assert(tappedStroke == null);
                    elementsToSelect.Add(tappedElement);
                }
            }

            SelfDeactivate();

            if ( commit )
            {
                InkCanvas.ChangeInkCanvasSelection(selectedStrokes, elementsToSelect.ToArray());
            }
        }

        /// <summary>
        /// OnCommitWithoutStylusInput
        /// </summary>
        /// <param name="commit"></param>
        protected override void OnCommitWithoutStylusInput(bool commit)
        {
            // We only deactivate LSB in this case.
            SelfDeactivate();
        }

        /// <summary>
        /// Get the current cursor for this editing mode
        /// </summary>
        /// <returns></returns>
        protected override Cursor GetCurrentCursor()
        {
            // By default return cross cursor.
            return Cursors.Cross;
        }

        #endregion Protected Methods

        //-------------------------------------------------------------------------------
        //
        // Private Methods
        //
        //-------------------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Private event handler that updates which strokes are actually selected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSelectionChanged(object sender, LassoSelectionChangedEventArgs e)
        {
            this.InkCanvas.UpdateDynamicSelection(e.SelectedStrokes, e.DeselectedStrokes);
        }


        /// <summary>
        /// Private helper that will hit test for elements
        /// </summary>
        private List<UIElement> HitTestForElements()
        {
            List<UIElement> elementsToSelect = new List<UIElement>();

            if ( this.InkCanvas.Children.Count == 0 )
            {
                return elementsToSelect;
            }

            for (int x = 0; x < this.InkCanvas.Children.Count; x++)
            {
                UIElement uiElement = this.InkCanvas.Children[x];
                HitTestElement(InkCanvas.InnerCanvas, uiElement, elementsToSelect);
            }

            return elementsToSelect;
        }

        /// <summary>
        /// Private helper that will turn an element in any nesting level into a stroke
        /// in the InkCanvas's coordinate space.  This method calls itself recursively
        /// </summary>
        private void HitTestElement(InkCanvasInnerCanvas parent, UIElement uiElement, List<UIElement> elementsToSelect)
        {
            ElementCornerPoints elementPoints = LassoSelectionBehavior.GetTransformedElementCornerPoints(parent, uiElement);
            if (elementPoints.Set != false)
            {
                Point[] points = GeneratePointGrid(elementPoints);

                //
                // perform hit testing against our lasso
                //
                System.Diagnostics.Debug.Assert(null != _lassoHelper);
                if (_lassoHelper.ArePointsInLasso(points, _percentIntersectForElements))
                {
                    elementsToSelect.Add(uiElement);
                }
            }
            //
            // we used to recurse into the childrens children.  That is no longer necessary
            //
        }

        /// <summary>
        /// Private helper that takes an element and transforms it's 4 points
        /// into the InkCanvas's space
        /// </summary>
        private static ElementCornerPoints GetTransformedElementCornerPoints(InkCanvasInnerCanvas canvas, UIElement childElement)
        {
            Debug.Assert(canvas != null);
            Debug.Assert(childElement != null);

            Debug.Assert(canvas.CheckAccess());

            ElementCornerPoints elementPoints = new ElementCornerPoints();
            elementPoints.Set = false;

            if (childElement.Visibility != Visibility.Visible)
            {
                //
                // this little one's not worth it...
                //
                return elementPoints;
            }

            //
            // get the transform from us to our parent InkCavas
            //
            GeneralTransform parentTransform = childElement.TransformToAncestor(canvas);            
            
            // REVIEW: any of the methods below may not actually perform the transformation
            // Do we need to do anything special in that scenario?
            parentTransform.TryTransform(new Point(0, 0), out elementPoints.UpperLeft);
            parentTransform.TryTransform(new Point(childElement.RenderSize.Width, 0), out elementPoints.UpperRight);
            parentTransform.TryTransform(new Point(0, childElement.RenderSize.Height), out elementPoints.LowerLeft);
            parentTransform.TryTransform(new Point(childElement.RenderSize.Width, childElement.RenderSize.Height), out elementPoints.LowerRight);

            elementPoints.Set = true;
            return elementPoints;
        }

        /// <summary>
        /// Private helper that will generate a grid of points 5 px apart given the elements bounding points
        /// this works with any affline transformed points
        /// </summary>
        private Point[] GeneratePointGrid(ElementCornerPoints elementPoints)
        {
            if (!elementPoints.Set)
            {
                return new Point[]{};
            }
            ArrayList pointArray = new ArrayList();

            UpdatePointDistances(elementPoints);

            //
            // add our original points
            //
            pointArray.Add(elementPoints.UpperLeft);
            pointArray.Add(elementPoints.UpperRight);
            FillInPoints(pointArray, elementPoints.UpperLeft, elementPoints.UpperRight);

            pointArray.Add(elementPoints.LowerLeft);
            pointArray.Add(elementPoints.LowerRight);
            FillInPoints(pointArray, elementPoints.LowerLeft, elementPoints.LowerRight);

            FillInGrid( pointArray,
                        elementPoints.UpperLeft,
                        elementPoints.UpperRight,
                        elementPoints.LowerRight,
                        elementPoints.LowerLeft);

            Point[] retPointArray = new Point[pointArray.Count];
            pointArray.CopyTo(retPointArray);
            return retPointArray;
        }

        /// <summary>
        /// Private helper that fills in the points between two points by calling itself
        /// recursively in a divide and conquer fashion
        /// </summary>
        private void FillInPoints(ArrayList pointArray, Point point1, Point point2)
        {
            // this algorithm improves perf by 20%
            if(!PointsAreCloseEnough(point1, point2))
            {
                Point midPoint = LassoSelectionBehavior.GeneratePointBetweenPoints(point1, point2);
                pointArray.Add(midPoint);

                if(!PointsAreCloseEnough(point1, midPoint))
                {
                    FillInPoints(pointArray, point1, midPoint);
                }

                //sort the right
                if(!PointsAreCloseEnough(midPoint, point2))
                {
                    FillInPoints(pointArray, midPoint, point2);
                }
            }
        }

        /// <summary>
        /// Private helper that fills in the points between four points by calling itself
        /// recursively in a divide and conquer fashion
        /// </summary>
        private void FillInGrid(ArrayList pointArray,
                                Point upperLeft,
                                Point upperRight,
                                Point lowerRight,
                                Point lowerLeft)
        {
            // this algorithm improves perf by 20%
            if(!PointsAreCloseEnough(upperLeft, lowerLeft))
            {
                Point midPointLeft = LassoSelectionBehavior.GeneratePointBetweenPoints(upperLeft, lowerLeft);
                Point midPointRight = LassoSelectionBehavior.GeneratePointBetweenPoints(upperRight, lowerRight);
                pointArray.Add(midPointLeft);
                pointArray.Add(midPointRight);
                FillInPoints(pointArray, midPointLeft, midPointRight);

                if(!PointsAreCloseEnough(upperLeft, midPointLeft))
                {
                    FillInGrid(pointArray, upperLeft, upperRight, midPointRight, midPointLeft);
                }

                //sort the right
                if(!PointsAreCloseEnough(midPointLeft, lowerLeft))
                {
                    FillInGrid(pointArray, midPointLeft, midPointRight, lowerRight, lowerLeft);
                }
            }
        }

        /// <summary>
        /// Private helper that will generate a new point between two points
        /// </summary>
        private static Point GeneratePointBetweenPoints(Point point1, Point point2)
        {
            //
            // compute the new point in the middle of the previous two
            //
            double maxX = point1.X > point2.X ? point1.X : point2.X;
            double minX = point1.X < point2.X ? point1.X : point2.X;
            double maxY = point1.Y > point2.Y ? point1.Y : point2.Y;
            double minY = point1.Y < point2.Y ? point1.Y : point2.Y;

            return new Point( (minX + ((maxX - minX) * 0.5f)),
                                (minY + ((maxY - minY) * 0.5f)));
        }

        /// <summary>
        /// Private helper used to determine if we're close enough between two points
        /// </summary>
        private bool PointsAreCloseEnough(Point point1, Point point2)
        {
            double x = point1.X - point2.X;
            double y = point1.Y - point2.Y;
            if ((x < _xDiff && x > -_xDiff) && (y < _yDiff && y > -_yDiff))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Used to calc the diff between points on the x and y axis
        /// </summary>
        private void UpdatePointDistances(ElementCornerPoints elementPoints)
        {
            //
            // calc the x and y diffs
            //
            double width = elementPoints.UpperLeft.X - elementPoints.UpperRight.X;
            if (width < 0)
            {
                width = -width;
            }

            double height = elementPoints.UpperLeft.Y - elementPoints.LowerLeft.Y;
            if (height < 0)
            {
                height = -height;
            }

            _xDiff = width * 0.25f;
            if (_xDiff > _maxThreshold)
            {
                _xDiff = _maxThreshold;
            }
            else if (_xDiff < _minThreshold)
            {
                _xDiff = _minThreshold;
            }

            _yDiff = height * 0.25f;
            if (_yDiff > _maxThreshold)
            {
                _yDiff = _maxThreshold;
            }
            else if (_yDiff < _minThreshold)
            {
                _yDiff = _minThreshold;
            }
        }

        /// <summary>
        /// StartLasso
        /// </summary>
        /// <param name="points"></param>
        private void StartLasso(List<Point> points)
        {
            Debug.Assert(!_disableLasso && _lassoHelper == null, "StartLasso is called unexpectedly.");

            if ( InkCanvas.ClearSelectionRaiseSelectionChanging() // If user cancels clearing the selection, we shouldn't initiate Lasso.
                // 
                // If the active editng mode is no longer as Select, we shouldn't activate LassoSelectionBehavior.
                // Note the order really matters here. This checking has to be done 
                // after ClearSelectionRaiseSelectionChanging is invoked.
                && EditingCoordinator.ActiveEditingMode == InkCanvasEditingMode.Select )
            {
                //
                // obtain a dynamic hit-tester for selecting with lasso
                //
                _incrementalLassoHitTester =
                                    this.InkCanvas.Strokes.GetIncrementalLassoHitTester(_percentIntersectForInk);
                //
                // add event handler
                //
                _incrementalLassoHitTester.SelectionChanged += new LassoSelectionChangedEventHandler(OnSelectionChanged);

                //
                // start dynamic rendering
                //

                _lassoHelper = new LassoHelper();
                InkCanvas.BeginDynamicSelection(_lassoHelper.Visual);

                Point[] lassoPoints = _lassoHelper.AddPoints(points);
                if ( 0 != lassoPoints.Length )
                {
                    _incrementalLassoHitTester.AddPoints(lassoPoints);
                }
            }
            else
            {
                // If we fail on clearing the selection or switching to Select mode, we should just disable lasso.
                _disableLasso = true;
            }
        }

        /// <summary>
        /// Pre-Select the object which user taps on.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="tappedStroke"></param>
        /// <param name="tappedElement"></param>
        private void TapSelectObject(Point point, out Stroke tappedStroke, out UIElement tappedElement)
        {
            tappedStroke = null;
            tappedElement = null;

            StrokeCollection hitTestStrokes = InkCanvas.Strokes.HitTest(point, 5.0d);
            if ( hitTestStrokes.Count > 0 )
            {
                tappedStroke = hitTestStrokes[hitTestStrokes.Count - 1];
            }
            else
            {
                GeneralTransform transformToInnerCanvas = InkCanvas.TransformToVisual(InkCanvas.InnerCanvas);
                Point pointOnInnerCanvas = transformToInnerCanvas.Transform(point);

                // Try to find out whether we have a pre-select object.
                tappedElement = InkCanvas.InnerCanvas.HitTestOnElements(pointOnInnerCanvas);
            }
}

        #endregion Private Methods

        //-------------------------------------------------------------------------------
        //
        // Private Fields
        //
        //-------------------------------------------------------------------------------

        #region Private Fields

        private Point                       _startPoint;
        private bool                        _disableLasso;

        private LassoHelper                 _lassoHelper = null;
        private IncrementalLassoHitTester   _incrementalLassoHitTester;
        private double                  _xDiff;
        private double                  _yDiff;
        private const double            _maxThreshold = 50f;
        private const double            _minThreshold = 15f;
        private const int               _percentIntersectForInk = 80;
        private const int               _percentIntersectForElements = 60;


#if DEBUG_LASSO_FEEDBACK
        private ContainerVisual _tempVisual;
#endif
        /// <summary>
        /// Private struct
        /// </summary>
        private struct ElementCornerPoints
        {
            internal Point UpperLeft;
            internal Point UpperRight;
            internal Point LowerRight;
            internal Point LowerLeft;
            internal bool Set;
        }

        #endregion Private Fields
    }
}
