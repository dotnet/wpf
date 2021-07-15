// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Ink;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Documents;

namespace MS.Internal.Ink
{
    #region LassoHelper
    /// <summary>
    /// An internal helper class to draw lasso as a sequence of dots
    /// closed with a rubber line. LassoSelectionBehavior creates an object of
    /// this class to render a single lasso, so, for simplicity,
    /// LassoHelper objects are indended for one-time use only.
    /// </summary>
    internal class LassoHelper
    {
        #region Fields

        // Visuals, geometry builders and drawing stuff
        DrawingVisual       _containerVisual = null;
        Brush               _brush = null;
        Pen                 _pen = null;
        //Pen                 _linePen = null;

        //
        bool                _isActivated = false;
        Point               _firstLassoPoint;
        Point               _lastLassoPoint;
        int                 _count = 0;

        // Entire lasso. Collected to hit test InkCanvas' subelements after stylus up.
        List<Point>         _lasso = null;
        Rect                _boundingBox;

        // some of these are probably not in sync
        // with the spec (which is not available at this moment), and also might
        // need to be different for the high contrast mode.
        public const double  MinDistanceSquared     = 49.0;
        const double  DotRadius                     = 2.5;
        const double  DotCircumferenceThickness     = 0.5;
        const double  ConnectLineThickness          = 0.75;
        const double  ConnectLineOpacity            = 0.75;
        static readonly Color DotColor              = Colors.Orange;     //FromArgb(1, 0.89f, 0.3607f, 0.1843f);
        static readonly Color DotCircumferenceColor = Colors.White;

        #endregion

        #region Public API
        /// <summary>
        /// Read-only access to the container visual for dynamic drawing a lasso
        /// </summary>
        public Visual Visual
        {
            get
            {
                EnsureVisual();
                return _containerVisual;
            }
        }

        /// <summary>TBS</summary>
        public Point[] AddPoints(List<Point> points)
        {
            if (null == points)
                throw new ArgumentNullException("points");

            // Lazy initialization.
            EnsureReady();

            List<Point> justAdded = new List<Point>();
            int count = points.Count;
            for ( int i = 0; i < count ; i++ )
            {
                Point point = points[i];

                if (0 == _count)
                {
                    AddLassoPoint(point);

                    justAdded.Add(point);
                    _lasso.Add(point);
                    _boundingBox.Union(point);

                    _firstLassoPoint = point;
                    _lastLassoPoint = point;
                    _count++;
                }
                else
                {
                    Vector last2next = point - _lastLassoPoint;
                    double distanceSquared = last2next.LengthSquared;

                    // Avoid using Sqrt when the distance is equal to the step.
                    if (DoubleUtil.AreClose(MinDistanceSquared, distanceSquared))
                    {
                        AddLassoPoint(point);
                        justAdded.Add(point);
                        _lasso.Add(point);
                        _boundingBox.Union(point);

                        _lastLassoPoint = point;
                        _count++;
}
                    else if (MinDistanceSquared < distanceSquared)
                    {
                        double step = Math.Sqrt(MinDistanceSquared / distanceSquared);
                        Point last = _lastLassoPoint;
                        for (double findex = step; findex < 1.0f; findex += step)
                        {
                            Point lassoPoint = last + (last2next * findex);
                            AddLassoPoint(lassoPoint);
                            justAdded.Add(lassoPoint);
                            _lasso.Add(lassoPoint);
                            _boundingBox.Union(lassoPoint);

                            _lastLassoPoint = lassoPoint;
                            _count++;
}
                    }
                }
            }

            // still working on perf here.
            // Draw a line between the last point and the first one.
            //if (_count > 1)
            //{
            //    DrawingContext dc = _containerVisual.RenderOpen();
            //    dc.DrawLine(_linePen, _firstLassoPoint, _lastLassoPoint);
            //    dc.Close();
            //}

            return justAdded.ToArray();
        }

        ///// <summary>
        ///// Draws a single lasso dot with the center at the given point.
        ///// </summary>
        private void AddLassoPoint(Point lassoPoint)
        {
            DrawingVisual dv = new DrawingVisual();
            DrawingContext dc = null;
            try
            {
                dc = dv.RenderOpen();
                dc.DrawEllipse(_brush, _pen, lassoPoint, DotRadius, DotRadius);
            }
            finally
            {
                if (dc != null)
                {
                    dc.Close();
                }
            }

            // Add the new visual to the container.
            _containerVisual.Children.Add(dv);
        }

        #endregion

        #region ArePointsInLasso
        /// <summary>Copy-pasted Platform's Lasso.Contains(...)</summary>
        public bool ArePointsInLasso(Point[] points, int percentIntersect)
        {
            System.Diagnostics.Debug.Assert(null != points);
            System.Diagnostics.Debug.Assert((0 <= percentIntersect) && (100 >= percentIntersect));

            // Find out how many of the points need to be inside the lasso to satisfy the percentIntersect.
            int marginCount = (points.Length * percentIntersect) / 100;

            if ((0 == marginCount) || (50 <= ((points.Length * percentIntersect) % 100)))
            {
                marginCount++;
            }

            // Check if any point on the stroke is within the lasso or not.
            // This is done by checking all segments on the left side of the point.
            // If the no of such segments is odd then the point is within the lasso otherwise not.
            int countPointsInLasso = 0;

            foreach (Point point in points)
            {
                if (true == Contains(point))
                {
                    countPointsInLasso++;
                    if (countPointsInLasso == marginCount)
                        break;
                }
            }

            return (countPointsInLasso == marginCount);
        }

        /// <summary>TBS</summary>
        private bool Contains(Point point)
        {
            if (false == _boundingBox.Contains(point))
            {
                return false;
            }

            bool isHigher = false;
            int last = _lasso.Count;

            while (--last >= 0)
            {
                if (false == DoubleUtil.AreClose(_lasso[last].Y, point.Y))
                {
                    isHigher = point.Y < _lasso[last].Y;
                    break;
                }
            }

            bool isInside = false, isOnClosingSegment = false;
            Point prevLassoPoint = _lasso[_lasso.Count - 1];

            for (int i = 0; i < _lasso.Count; i++)
            {
                Point lassoPoint = _lasso[i];

                if (DoubleUtil.AreClose(lassoPoint.Y, point.Y))
                {
                    if (DoubleUtil.AreClose(lassoPoint.X, point.X))
                    {
                        isInside = true;
                        break;
                    }

                    if ((0 != i) && DoubleUtil.AreClose(prevLassoPoint.Y, point.Y)
                        && DoubleUtil.GreaterThanOrClose(point.X, Math.Min(prevLassoPoint.X, lassoPoint.X))
                        && DoubleUtil.LessThanOrClose(point.X, Math.Max(prevLassoPoint.X, lassoPoint.X)))
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

                        // The closing segment is the only exclusive one. Special case it.
                        if ((0 == i) && DoubleUtil.AreClose(point.X, Math.Max(prevLassoPoint.X, lassoPoint.X)))
                        {
                            isOnClosingSegment = true;
                        }
                    }
                    else if (DoubleUtil.GreaterThanOrClose(point.X, Math.Min(prevLassoPoint.X, lassoPoint.X)))
                    {
                        // The X of the point lies within the x ranges for the segment.
                        // Calculate the x value of the point where the segment intersects with the line.
                        Vector lassoSegment = lassoPoint - prevLassoPoint;
                        double x = prevLassoPoint.X + (lassoSegment.X / lassoSegment.Y) * (point.Y - prevLassoPoint.Y);

                        if (DoubleUtil.GreaterThanOrClose(point.X, x))
                        {
                            isInside = !isInside;
                            if ((0 == i) && DoubleUtil.AreClose(point.X, x))
                            {
                                isOnClosingSegment = true;
                            }
                        }
                    }
                }

                prevLassoPoint = lassoPoint;
            }

            return isInside ? !isOnClosingSegment : false;
        }
        #endregion

        #region Implementation helpers
        /// <summary> Creates the container visual when needed.</summary>
        private void EnsureVisual()
        {
            if (null == _containerVisual)
            {
                _containerVisual = new DrawingVisual();
            }
        }

        /// <summary>
        /// Creates and initializes objects required for drawing
        /// </summary>
        private void EnsureReady()
        {
            if (false == _isActivated)
            {
                _isActivated = true;

                EnsureVisual();

                _brush = new SolidColorBrush(DotColor);
                _brush.Freeze();

                //_linePen = new Pen(new SolidColorBrush(Colors.DarkGray), ConnectLineThickness);
                //_linePen.Brush.Opacity = ConnectLineOpacity;
                //_linePen.LineJoin = PenLineJoin.Round;

                _pen = new Pen(new SolidColorBrush(DotCircumferenceColor), DotCircumferenceThickness);
                _pen.LineJoin = PenLineJoin.Round;
                _pen.Freeze();

                _lasso = new List<Point>(100);
                _boundingBox = Rect.Empty;

                _count = 0;
            }
        }

        #endregion
    }

    #endregion
}
