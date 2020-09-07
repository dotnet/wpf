// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
// Implementation of Line shape element.
//

using System.Windows.Shapes;
using System.Diagnostics;
using System.Windows.Threading;

using System.Windows;
using System.Windows.Media;
using System.ComponentModel;
using MS.Internal;

using System;

namespace System.Windows.Shapes
{
    /// <summary>
    /// The line shape element
    /// This element (like all shapes) belongs under a Canvas,
    /// and will be presented by the parent canvas.
    /// </summary>
    public sealed class Line : Shape
    {
        #region Constructors

        /// <summary>
        /// Instantiates a new instance of a line.
        /// </summary>
        public Line()
        {
        }

        #endregion Constructors

        #region Dynamic Properties

        /// <summary>
        /// X1 property
        /// </summary>
        public static readonly DependencyProperty X1Property =
            DependencyProperty.Register( "X1", typeof(double), typeof(Line), 
                new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender),
                new ValidateValueCallback(Shape.IsDoubleFinite));

        /// <summary>
        /// X1 property
        /// </summary>
        [TypeConverter(typeof(LengthConverter))]
        public double X1
        {
            get
            {
                return (double)GetValue(X1Property);
            }
            set
            {
                SetValue(X1Property, value);
            }
        }

        /// <summary>
        /// Y1 property
        /// </summary>
        public static readonly DependencyProperty Y1Property =
            DependencyProperty.Register( "Y1", typeof(double), typeof(Line),
                new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender),
                new ValidateValueCallback(Shape.IsDoubleFinite));

        /// <summary>
        /// Y1 property
        /// </summary>
        [TypeConverter(typeof(LengthConverter))]
        public double Y1
        {
            get
            {
                return (double)GetValue(Y1Property);
            }
            set
            {
                SetValue(Y1Property, value);
            }
        }

        /// <summary>
        /// X2 property
        /// </summary>
        public static readonly DependencyProperty X2Property =
            DependencyProperty.Register( "X2", typeof(double), typeof(Line),
                new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender),
                new ValidateValueCallback(Shape.IsDoubleFinite));

        /// <summary>
        /// X2 property
        /// </summary>
        [TypeConverter(typeof(LengthConverter))]
        public double X2
        {
            get
            {
                return (double)GetValue(X2Property);
            }
            set
            {
                SetValue(X2Property, value);
            }
        }

        /// <summary>
        /// Y2 property
        /// </summary>
        public static readonly DependencyProperty Y2Property =
            DependencyProperty.Register( "Y2", typeof(double), typeof(Line),
                new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender),
                new ValidateValueCallback(Shape.IsDoubleFinite));

        /// <summary>
        /// Y2 property
        /// </summary>
        [TypeConverter(typeof(LengthConverter))]
        public double Y2
        {
            get
            {
                return (double)GetValue(Y2Property);
            }
            set
            {
                SetValue(Y2Property, value);
            }
        }


        #endregion Dynamic Properties

        #region Protected Methods and Properties

        
        /// <summary>
        /// Get the line that defines this shape
        /// </summary>
        protected override Geometry DefiningGeometry
        {
            get
            {
                return _lineGeometry;
            }
        }

        #endregion

        #region Internal Methods
        internal override void CacheDefiningGeometry()
        {
            Point point1 = new Point(X1, Y1);
            Point point2 = new Point(X2, Y2);

            // Create the Line geometry
            _lineGeometry = new LineGeometry(point1, point2);
        }
        #endregion Internal Methods

        #region Private Methods and Members

        private LineGeometry _lineGeometry;

        #endregion
   }
}
