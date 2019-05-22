// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
// Implementation of Polyline shape element.
//


using System.Windows.Shapes;
using System.Diagnostics;
using System.Windows.Threading;

using System.Windows;
using System.Windows.Media;
using MS.Internal;

using System;

namespace System.Windows.Shapes
{
    /// <summary>
    /// The Polyline shape element
    /// This element (like all shapes) belongs under a Canvas,
    /// and will be presented by the parent canvas.
    /// </summary>
    public sealed class Polyline : Shape
    {
        #region Constructors

        /// <summary>
        /// Instantiates a new instance of a Polyline.
        /// </summary>
        public Polyline()
        {
        }

        #endregion Constructors

        #region Dynamic Properties

        /// <summary>
        /// Points property
        /// </summary>
        public static readonly DependencyProperty PointsProperty = DependencyProperty.Register(
                "Points", typeof(PointCollection), typeof(Polyline), 
                new FrameworkPropertyMetadata(new FreezableDefaultValueFactory(PointCollection.Empty), FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Points property
        /// </summary>
        public PointCollection Points
        {
            get
            {
                return (PointCollection)GetValue(PointsProperty);
            }
            set
            {
                SetValue(PointsProperty, value);
            }
        }

        /// <summary>
        /// FillRule property
        /// </summary>
        public static readonly DependencyProperty FillRuleProperty = DependencyProperty.Register(
            "FillRule", 
            typeof(FillRule), 
            typeof(Polyline), 
            new FrameworkPropertyMetadata(
                FillRule.EvenOdd, 
                FrameworkPropertyMetadataOptions.AffectsRender),
            new ValidateValueCallback(System.Windows.Media.ValidateEnums.IsFillRuleValid)
            );

        /// <summary>
        /// FillRule property
        /// </summary>
        public FillRule FillRule
        {
            get
            {
                return (FillRule)GetValue(FillRuleProperty);
            }
            set
            {
                SetValue(FillRuleProperty, value);
            }
        }
        #endregion Dynamic Properties

        #region Protected Methods and Properties


        /// <summary>
        /// Get the polyline that defines this shape
        /// </summary>
        protected override Geometry DefiningGeometry
        {
            get
            {
                return _polylineGeometry;
            }
        }

        #endregion

        #region Internal methods
        
        internal override void CacheDefiningGeometry()
        {
            PointCollection pointCollection = Points;
            PathFigure pathFigure = new PathFigure();

            // Are we degenerate?
            // Yes, if we don't have data
            if (pointCollection == null)
            {
                _polylineGeometry = Geometry.Empty;
                return;
            }

            // Create the Polyline PathGeometry
            // ISSUE-rajatg-07/11/2003 - Bug 859068
            // The constructor for PathFigure that takes a PointCollection is internal in the Core
            // so the below causes an A/V. Consider making it public.
            if (pointCollection.Count > 0)
            {
                pathFigure.StartPoint = pointCollection[0];

                if (pointCollection.Count > 1)
                {
                    Point[] array = new Point[pointCollection.Count - 1];

                    for (int i = 1; i < pointCollection.Count; i++)
                    {
                        array[i - 1] = pointCollection[i];
                    }

                    pathFigure.Segments.Add(new PolyLineSegment(array, true));
                }
            }

            PathGeometry polylineGeometry = new PathGeometry();
            polylineGeometry.Figures.Add(pathFigure);

            // Set FillRule
            polylineGeometry.FillRule = FillRule;

            if (polylineGeometry.Bounds == Rect.Empty)
            {
                _polylineGeometry = Geometry.Empty;
            }
            else
            {
                _polylineGeometry = polylineGeometry;
            }
        }

        #endregion Internal methods

        #region Private Methods and Members

        private Geometry _polylineGeometry;

        #endregion
   }
}
