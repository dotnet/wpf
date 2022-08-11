// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
// Implementation of Ellipse shape element.
//


using System.Windows.Shapes;
using System.Diagnostics;
using System.Windows.Threading;

using System.Windows;
using System.Windows.Media;
using MS.Internal;
using System.ComponentModel;

using System;

namespace System.Windows.Shapes
{
    /// <summary>
    /// The ellipse shape element
    /// This element (like all shapes) belongs under a Canvas,
    /// and will be presented by the parent canvas.
    /// </summary>
    /// <ExternalAPI/>
    public sealed class Ellipse : Shape
    {
        #region Constructors
        /// <summary>
        /// Instantiates a new instance of a Ellipse.
        /// </summary>
        /// <ExternalAPI/>
        public Ellipse()
        {
        }

        // The default stretch mode of Ellipse is Fill
        static Ellipse()
        {
            StretchProperty.OverrideMetadata(typeof(Ellipse), new FrameworkPropertyMetadata(Stretch.Fill));
        }

        #endregion Constructors

        #region Dynamic Properties
  
        // For an Ellipse, RenderedGeometry = defining geometry and GeometryTransform = Identity

        /// <summary>
        /// The RenderedGeometry property returns the final rendered geometry
        /// </summary>
        public override Geometry RenderedGeometry
        {
            get
            {
                // RenderedGeometry = defining geometry
                return DefiningGeometry;
            }
        }

        /// <summary>
        /// Return the transformation applied to the geometry before rendering
        /// </summary>
        public override Transform GeometryTransform
        {
            get
            {
                return Transform.Identity;
            }
        }

        #endregion Dynamic Properties

        #region Protected

        /// <summary>
        /// Updates DesiredSize of the Ellipse.  Called by parent UIElement.  This is the first pass of layout.
        /// </summary>
        /// <param name="constraint">Constraint size is an "upper limit" that Ellipse should not exceed.</param>
        /// <returns>Ellipse's desired size.</returns>
        protected override Size MeasureOverride(Size constraint)
        {
            if (Stretch == Stretch.UniformToFill)
            {
                double width = constraint.Width;
                double height = constraint.Height;

                if (Double.IsInfinity(width) && Double.IsInfinity(height))
                {
                    return GetNaturalSize();
                }
                else if (Double.IsInfinity(width) || Double.IsInfinity(height))
                {
                    width = Math.Min(width, height);
                }
                else
                {
                    width = Math.Max(width, height);
                }

                return new Size(width, width);
            }

            return GetNaturalSize();
        }

        /// <summary>
        /// Returns the final size of the shape and caches the bounds.
        /// </summary>
        protected override Size ArrangeOverride(Size finalSize)
        {
            // We construct the rectangle to fit finalSize with the appropriate Stretch mode.  The rendering
            // transformation will thus be the identity.

            double penThickness = GetStrokeThickness();
            double margin = penThickness / 2;

            _rect = new Rect(
                margin, // X
                margin, // Y
                Math.Max(0, finalSize.Width - penThickness),    // Width
                Math.Max(0, finalSize.Height - penThickness));  // Height

            switch (Stretch)
            {
                case Stretch.None:
                    // A 0 Rect.Width and Rect.Height rectangle
                    _rect.Width = _rect.Height = 0;
                    break;

                case Stretch.Fill:
                    // The most common case: a rectangle that fills the box.
                    // _rect has already been initialized for that.
                    break;

                case Stretch.Uniform:
                    // The maximal square that fits in the final box
                    if (_rect.Width > _rect.Height)
                    {
                        _rect.Width = _rect.Height;
                    }
                    else  // _rect.Width <= _rect.Height
                    {
                        _rect.Height = _rect.Width;
                    }
                    break;

                case Stretch.UniformToFill:

                    // The minimal square that fills the final box
                    if (_rect.Width < _rect.Height)
                    {
                        _rect.Width = _rect.Height;
                    }
                    else  // _rect.Width >= _rect.Height
                    {
                        _rect.Height = _rect.Width;
                    }
                    break;
            }

            ResetRenderedGeometry();

            return finalSize;
        }
        
        /// <summary>
        /// Get the ellipse that defines this shape
        /// </summary>
        protected override Geometry DefiningGeometry
        {
            get
            {
                if (_rect.IsEmpty)
                {
                    return Geometry.Empty;
                }

                return new EllipseGeometry(_rect);
            }
        }

        /// <summary>
        /// Render callback.
        /// </summary>
        protected override void OnRender(DrawingContext drawingContext)
        {
            if (!_rect.IsEmpty)
            {
                Pen pen = GetPen();
                drawingContext.DrawGeometry(Fill, pen, new EllipseGeometry(_rect));
            }
        }

        #endregion Protected
        
        #region Internal Methods

        internal override void CacheDefiningGeometry()
        {
            double margin = GetStrokeThickness() / 2;

            _rect = new Rect(margin, margin, 0, 0);
        }

        /// <summary>
        /// Get the natural size of the geometry that defines this shape
        /// </summary>
        internal override Size GetNaturalSize()
        {
            double strokeThickness = GetStrokeThickness();
            return new Size(strokeThickness, strokeThickness);
        }

        /// <summary>
        /// Get the bonds of the rectangle that defines this shape
        /// </summary>
        internal override Rect GetDefiningGeometryBounds()
        {
            return _rect;
        }
        
        //
        //  This property
        //  1. Finds the correct initial size for the _effectiveValues store on the current DependencyObject
        //  2. This is a performance optimization
        //
        internal override int EffectiveValuesInitialSize
        {
            get { return 13; }
        }

        #endregion Internal Methods

        #region Private Fields
        
        private Rect _rect = Rect.Empty;

        #endregion Private Fields
    }
}
