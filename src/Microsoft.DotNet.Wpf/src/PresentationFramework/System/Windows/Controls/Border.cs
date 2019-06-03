// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Contains the Border Decorator class.
//              Spec at Border.xml
//


using MS.Internal;
using MS.Internal.PresentationFramework;
using MS.Internal.Telemetry.PresentationFramework;
using MS.Utility;
using System;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows.Media;

namespace System.Windows.Controls
{
    /// <summary>
    /// The Border decorator is used to draw a border and/or background around another element.
    /// </summary>
    public class Border : Decorator
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        static Border()
        {
            ControlsTraceLogger.AddControl(TelemetryControls.Border);
        }

        /// <summary>
        ///     Default DependencyObject constructor
        /// </summary>
        /// <remarks>
        ///     Automatic determination of current Dispatcher. Use alternative constructor
        ///     that accepts a Dispatcher for best performance.
        /// </remarks>
        public Border() : base()
        {
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// The BorderThickness property defined how thick a border to draw.  The property's value is a
        /// <see cref="System.Windows.Thickness" /> containing values for each of the Left, Top, Right,
        /// and Bottom sides.  Values of Auto are interpreted as zero.
        /// </summary>
        public Thickness BorderThickness
        {
            get { return (Thickness) GetValue(BorderThicknessProperty); }
            set { SetValue(BorderThicknessProperty, value); }
        }

        /// <summary>
        /// The Padding property inflates the effective size of the child by the specified thickness.  This
        /// achieves the same effect as adding margin on the child, but is present here for convenience.
        /// </summary>
        public Thickness Padding
        {
            get { return (Thickness) GetValue(PaddingProperty); }
            set { SetValue(PaddingProperty, value); }
        }

        /// <summary>
        /// The CornerRadius property allows users to control the roundness of the corners independently by
        /// setting a radius value for each corner.  Radius values that are too large are scaled so that they
        /// smoothly blend from corner to corner.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get { return (CornerRadius) GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        /// <summary>
        /// The BorderBrush property defines the brush used to fill the border region.
        /// </summary>
        public Brush BorderBrush
        {
            get { return (Brush) GetValue(BorderBrushProperty); }
            set { SetValue(BorderBrushProperty, value); }
        }

        /// <summary>
        /// The Background property defines the brush used to fill the area within the border.
        /// </summary>
        public Brush Background
        {
            get { return (Brush) GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }        
        }
        
        /// <summary>
        /// DependencyProperty for <see cref="BorderThickness" /> property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty BorderThicknessProperty
            = DependencyProperty.Register("BorderThickness", typeof(Thickness), typeof(Border),
                                          new FrameworkPropertyMetadata(
                                                new Thickness(), 
                                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
                                                new PropertyChangedCallback(OnClearPenCache)),
                                          new ValidateValueCallback(IsThicknessValid));

        private static void OnClearPenCache(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Border border = (Border)d;
            border.LeftPenCache = null;
            border.RightPenCache = null;
            border.TopPenCache = null;
            border.BottomPenCache = null;
        }

        private static bool IsThicknessValid(object value)
        {
            Thickness t = (Thickness)value;
            return t.IsValid(false, false, false, false);
        }

        /// <summary>
        /// DependencyProperty for <see cref="Padding" /> property.
        /// </summary>
        public static readonly DependencyProperty PaddingProperty
            = DependencyProperty.Register("Padding", typeof(Thickness), typeof(Border),
                                          new FrameworkPropertyMetadata(
                                                new Thickness(),
                                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender),
                                          new ValidateValueCallback(IsThicknessValid));

        /// <summary>
        /// DependencyProperty for <see cref="CornerRadius" /> property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty
            = DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(Border),
                                          new FrameworkPropertyMetadata(
                                                new CornerRadius(),
                                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender),
                                          new ValidateValueCallback(IsCornerRadiusValid));

        private static bool IsCornerRadiusValid(object value)
        {
            CornerRadius cr = (CornerRadius)value;
            return (cr.IsValid(false, false, false, false));
        }

        /// <summary>
        /// DependencyProperty for <see cref="BorderBrush" /> property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty BorderBrushProperty
            = DependencyProperty.Register("BorderBrush", typeof(Brush), typeof(Border),
                                          new FrameworkPropertyMetadata(
                                                (Brush)null,
                                                FrameworkPropertyMetadataOptions.AffectsRender |
                                                FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender,
                                                new PropertyChangedCallback(OnClearPenCache)));

        /// <summary>
        /// DependencyProperty for <see cref="Background" /> property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty BackgroundProperty = 
                Panel.BackgroundProperty.AddOwner(typeof(Border),
                        new FrameworkPropertyMetadata(
                                (Brush)null,
                                FrameworkPropertyMetadataOptions.AffectsRender |
                                FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender));

        #endregion Public Properties

        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Updates DesiredSize of the Border.  Called by parent UIElement.  This is the first pass of layout.
        /// </summary>
        /// <remarks>
        /// Border determines its desired size it needs from the specified border the child: its sizing
        /// properties, margin, and requested size.
        /// </remarks>
        /// <param name="constraint">Constraint size is an "upper limit" that the return value should not exceed.</param>
        /// <returns>The Decorator's desired size.</returns>
        protected override Size MeasureOverride(Size constraint)
        {
            UIElement child = Child;
            Size mySize = new Size();
            Thickness borders = this.BorderThickness;
            if (this.UseLayoutRounding && !FrameworkAppContextSwitches.DoNotApplyLayoutRoundingToMarginsAndBorderThickness)
            {
                DpiScale dpi = GetDpi();
                borders = new Thickness(UIElement.RoundLayoutValue(borders.Left, dpi.DpiScaleX), UIElement.RoundLayoutValue(borders.Top, dpi.DpiScaleY),
                   UIElement.RoundLayoutValue(borders.Right, dpi.DpiScaleX), UIElement.RoundLayoutValue(borders.Bottom, dpi.DpiScaleY));
            }
            // Compute the chrome size added by the various elements
            Size border = HelperCollapseThickness(borders);
            Size padding = HelperCollapseThickness(this.Padding);

            //If we have a child
            if (child != null)
            {
                // Combine into total decorating size
                Size combined = new Size(border.Width + padding.Width, border.Height + padding.Height);

                // Remove size of border only from child's reference size.
                Size childConstraint = new Size(Math.Max(0.0, constraint.Width - combined.Width),
                                                Math.Max(0.0, constraint.Height - combined.Height));


                child.Measure(childConstraint);
                Size childSize = child.DesiredSize;           

                // Now use the returned size to drive our size, by adding back the margins, etc.
                mySize.Width = childSize.Width + combined.Width;
                mySize.Height = childSize.Height + combined.Height;
            }
            else
            {
                // Combine into total decorating size
                mySize = new Size(border.Width + padding.Width, border.Height + padding.Height);
            }

            return mySize;
        }


        /// <summary>
        /// Border computes the position of its single child and applies its child's alignments to the child.
        /// 
        /// </summary>
        /// <param name="finalSize">The size reserved for this element by the parent</param>
        /// <returns>The actual ink area of the element, typically the same as finalSize</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            Thickness borders = BorderThickness;
            if (this.UseLayoutRounding && !FrameworkAppContextSwitches.DoNotApplyLayoutRoundingToMarginsAndBorderThickness)
            {
                DpiScale dpi = GetDpi();
                borders = new Thickness(UIElement.RoundLayoutValue(borders.Left, dpi.DpiScaleX), UIElement.RoundLayoutValue(borders.Top, dpi.DpiScaleY),
                   UIElement.RoundLayoutValue(borders.Right, dpi.DpiScaleX), UIElement.RoundLayoutValue(borders.Bottom, dpi.DpiScaleY));
            }
            Rect boundRect = new Rect(finalSize);
            Rect innerRect = HelperDeflateRect(boundRect, borders);
            
            //  arrange child
            UIElement child = Child;
            if (child != null)
            {
                Rect childRect = HelperDeflateRect(innerRect, Padding);
                child.Arrange(childRect);
            }

            CornerRadius radii = CornerRadius;
            Brush borderBrush = BorderBrush;
            bool uniformCorners = AreUniformCorners(radii);

            //  decide which code path to execute. complex (geometry path based) rendering 
            //  is used if one of the following is true:

            //  1. there are non-uniform rounded corners
            _useComplexRenderCodePath = !uniformCorners;

            if (    !_useComplexRenderCodePath
                &&  borderBrush != null )
            {
                SolidColorBrush originIndependentBrush = borderBrush as SolidColorBrush;

                bool uniformBorders = borders.IsUniform;

                _useComplexRenderCodePath = 
                //  2. the border brush is origin dependent (the only origin independent brush is a solid color brush)
                        (originIndependentBrush == null)
                //  3. the border brush is semi-transtarent solid color brush AND border thickness is not uniform
                //     (for uniform semi-transparent border Border.OnRender draws rectangle outline - so it works fine)
                    || ((originIndependentBrush.Color.A < 0xff) && !uniformBorders)
                //  4. there are rounded corners AND the border thickness is not uniform
                    || (!DoubleUtil.IsZero(radii.TopLeft) && !uniformBorders);
            }

            if (_useComplexRenderCodePath)
            {
                Radii innerRadii = new Radii(radii, borders, false);

                StreamGeometry backgroundGeometry = null;

                //  calculate border / background rendering geometry
                if (!DoubleUtil.IsZero(innerRect.Width) && !DoubleUtil.IsZero(innerRect.Height))
                {
                    backgroundGeometry = new StreamGeometry();

                    using (StreamGeometryContext ctx = backgroundGeometry.Open())
                    {
                        GenerateGeometry(ctx, innerRect, innerRadii);
                    }

                    backgroundGeometry.Freeze();
                    BackgroundGeometryCache = backgroundGeometry;
                }
                else
                {
                    BackgroundGeometryCache = null;
                }

                if (!DoubleUtil.IsZero(boundRect.Width) && !DoubleUtil.IsZero(boundRect.Height))
                {
                    Radii outerRadii = new Radii(radii, borders, true);
                    StreamGeometry borderGeometry = new StreamGeometry();

                    using (StreamGeometryContext ctx = borderGeometry.Open())
                    {
                        GenerateGeometry(ctx, boundRect, outerRadii);

                        if (backgroundGeometry != null)
                        {
                            GenerateGeometry(ctx, innerRect, innerRadii);
                        }
                    }

                    borderGeometry.Freeze();
                    BorderGeometryCache = borderGeometry;
                }
                else
                {
                    BorderGeometryCache = null;
                }
            }
            else 
            {
                BackgroundGeometryCache = null;
                BorderGeometryCache = null;
            }

            return (finalSize);
        }

        /// <summary>
        /// In addition to the child, Border renders a background + border.  The background is drawn inside the border.
        /// </summary>
        protected override void OnRender(DrawingContext dc)
        {           
            bool useLayoutRounding = this.UseLayoutRounding;
            DpiScale dpi = GetDpi();

            if (_useComplexRenderCodePath)
            {
                Brush brush;
                StreamGeometry borderGeometry = BorderGeometryCache;
                if (    borderGeometry != null
                    &&  (brush = BorderBrush) != null   )
                {
                    dc.DrawGeometry(brush, null, borderGeometry);
                }

                StreamGeometry backgroundGeometry = BackgroundGeometryCache;
                if (    backgroundGeometry != null
                    &&  (brush = Background) != null    )
                {
                    dc.DrawGeometry(brush, null, backgroundGeometry);
                }
            }
            else
            {
                Thickness border = BorderThickness;
                Brush borderBrush;

                CornerRadius cornerRadius = CornerRadius;
                double outerCornerRadius = cornerRadius.TopLeft; // Already validated that all corners have the same radius
                bool roundedCorners = !DoubleUtil.IsZero(outerCornerRadius);

                // If we have a brush with which to draw the border, do so.
                // NB: We double draw corners right now.  Corner handling is tricky (bevelling, &c...) and
                //     we need a firm spec before doing "the right thing."  (greglett, ffortes)
                if (!border.IsZero
                    && (borderBrush = BorderBrush) != null)
                {
                    // Initialize the first pen.  Note that each pen is created via new()
                    // and frozen if possible.  Doing this avoids the pen 
                    // being copied when used in the DrawLine methods.
                    Pen pen = LeftPenCache;
                    if (pen == null)
                    {
                        pen = new Pen();
                        pen.Brush = borderBrush;

                        if (useLayoutRounding)
                        {
                            pen.Thickness = UIElement.RoundLayoutValue(border.Left, dpi.DpiScaleX);
                        }
                        else
                        {
                            pen.Thickness = border.Left;
                        }
                        if (borderBrush.IsFrozen)
                        {
                            pen.Freeze();
                        }

                        LeftPenCache = pen;
                    }

                    double halfThickness;
                    if (border.IsUniform)
                    {
                        halfThickness = pen.Thickness * 0.5;


                        // Create rect w/ border thickness, and round if applying layout rounding.
                        Rect rect = new Rect(new Point(halfThickness, halfThickness),
                                             new Point(RenderSize.Width - halfThickness, RenderSize.Height - halfThickness));

                        if (roundedCorners)
                        {
                            dc.DrawRoundedRectangle(
                                null,
                                pen,
                                rect,
                                outerCornerRadius,
                                outerCornerRadius);
                        }
                        else
                        {
                            dc.DrawRectangle(
                                null,
                                pen,
                                rect);
                        }
                    }
                    else
                    {
                        // Nonuniform border; stroke each edge.
                        if (DoubleUtil.GreaterThan(border.Left, 0))
                        {
                            halfThickness = pen.Thickness * 0.5;
                            dc.DrawLine(
                                pen,
                                new Point(halfThickness, 0),
                                new Point(halfThickness, RenderSize.Height));
                        }

                        if (DoubleUtil.GreaterThan(border.Right, 0))
                        {
                            pen = RightPenCache;
                            if (pen == null)
                            {
                                pen = new Pen();
                                pen.Brush = borderBrush;

                                if (useLayoutRounding)
                                {
                                    pen.Thickness = UIElement.RoundLayoutValue(border.Right, dpi.DpiScaleX);
                                }
                                else
                                {
                                    pen.Thickness = border.Right;
                                }

                                if (borderBrush.IsFrozen)
                                {
                                    pen.Freeze();
                                }

                                RightPenCache = pen;
                            }

                            halfThickness = pen.Thickness * 0.5;
                            dc.DrawLine(
                                pen,
                                new Point(RenderSize.Width - halfThickness, 0),
                                new Point(RenderSize.Width - halfThickness, RenderSize.Height));
                        }

                        if (DoubleUtil.GreaterThan(border.Top, 0))
                        {
                            pen = TopPenCache;
                            if (pen == null)
                            {
                                pen = new Pen();
                                pen.Brush = borderBrush;
                                if (useLayoutRounding)
                                {
                                    pen.Thickness = UIElement.RoundLayoutValue(border.Top, dpi.DpiScaleY);
                                }
                                else
                                {
                                    pen.Thickness = border.Top;
                                }

                                if (borderBrush.IsFrozen)
                                {
                                    pen.Freeze();
                                }

                                TopPenCache = pen;
                            }

                            halfThickness = pen.Thickness * 0.5;
                            dc.DrawLine(
                                pen,
                                new Point(0, halfThickness),
                                new Point(RenderSize.Width, halfThickness));
                        }

                        if (DoubleUtil.GreaterThan(border.Bottom, 0))
                        {
                            pen = BottomPenCache;
                            if (pen == null)
                            {
                                pen = new Pen();
                                pen.Brush = borderBrush;
                                if (useLayoutRounding)
                                {
                                    pen.Thickness = UIElement.RoundLayoutValue(border.Bottom, dpi.DpiScaleY);
                                }
                                else
                                {
                                    pen.Thickness = border.Bottom;
                                }
                                if (borderBrush.IsFrozen)
                                {
                                    pen.Freeze();
                                }

                                BottomPenCache = pen;
                            }

                            halfThickness = pen.Thickness * 0.5;
                            dc.DrawLine(
                                pen,
                                new Point(0, RenderSize.Height - halfThickness),
                                new Point(RenderSize.Width, RenderSize.Height - halfThickness));
                        }
                    }
                }

                // Draw background in rectangle inside border.
                Brush background = Background;
                if (background != null)
                {
                    // Intialize background 
                    Point ptTL, ptBR;

                    if (useLayoutRounding)
                    {
                        ptTL = new Point(UIElement.RoundLayoutValue(border.Left, dpi.DpiScaleX),
                                         UIElement.RoundLayoutValue(border.Top, dpi.DpiScaleY));
                        
                        if(FrameworkAppContextSwitches.DoNotApplyLayoutRoundingToMarginsAndBorderThickness)
                        {
                            ptBR = new Point(UIElement.RoundLayoutValue(RenderSize.Width - border.Right, dpi.DpiScaleX),
                                         UIElement.RoundLayoutValue(RenderSize.Height - border.Bottom, dpi.DpiScaleY));
                        }
                        else
                        {
                            ptBR = new Point(RenderSize.Width - UIElement.RoundLayoutValue(border.Right, dpi.DpiScaleX),
                                         RenderSize.Height - UIElement.RoundLayoutValue(border.Bottom, dpi.DpiScaleY));
                        }                        
                    }
                    else
                    {
                        ptTL = new Point(border.Left, border.Top); 
                        ptBR = new Point(RenderSize.Width - border.Right, RenderSize.Height - border.Bottom);
                    }

                    // Do not draw background if the borders are so large that they overlap.
                    if (ptBR.X > ptTL.X && ptBR.Y > ptTL.Y)
                    {
                        if (roundedCorners)
                        {
                            Radii innerRadii = new Radii(cornerRadius, border, false); // Determine the inner edge radius
                            double innerCornerRadius = innerRadii.TopLeft;  // Already validated that all corners have the same radius
                            dc.DrawRoundedRectangle(background, null, new Rect(ptTL, ptBR), innerCornerRadius, innerCornerRadius);
                        }
                        else
                        {
                            dc.DrawRectangle(background, null, new Rect(ptTL, ptBR));
                        }
                    }
                }
            }
        }

        #endregion Protected Methods

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        // Helper function to add up the left and right size as width, as well as the top and bottom size as height
        private static Size HelperCollapseThickness(Thickness th)
        {
            return new Size(th.Left + th.Right, th.Top + th.Bottom);
        }

        private static bool AreUniformCorners(CornerRadius borderRadii)
        {
            double topLeft = borderRadii.TopLeft;
            return DoubleUtil.AreClose(topLeft, borderRadii.TopRight) &&
                DoubleUtil.AreClose(topLeft, borderRadii.BottomLeft) &&
                DoubleUtil.AreClose(topLeft, borderRadii.BottomRight);
        }

        /// Helper to deflate rectangle by thickness
        private static Rect HelperDeflateRect(Rect rt, Thickness thick)
        {
            return new Rect(rt.Left + thick.Left,
                            rt.Top + thick.Top,
                            Math.Max(0.0, rt.Width - thick.Left - thick.Right),
                            Math.Max(0.0, rt.Height - thick.Top - thick.Bottom));
        }

        /// <summary>
        ///     Generates a StreamGeometry.
        /// </summary>
        /// <param name="ctx">An already opened StreamGeometryContext.</param>
        /// <param name="rect">Rectangle for geomentry conversion.</param>
        /// <param name="radii">Corner radii.</param>
        /// <returns>Result geometry.</returns>
        private static void GenerateGeometry(StreamGeometryContext ctx, Rect rect, Radii radii)
        {
            //
            //  compute the coordinates of the key points
            //

            Point topLeft = new Point(radii.LeftTop, 0);
            Point topRight = new Point(rect.Width - radii.RightTop, 0);
            Point rightTop = new Point(rect.Width, radii.TopRight);
            Point rightBottom = new Point(rect.Width, rect.Height - radii.BottomRight);
            Point bottomRight = new Point(rect.Width - radii.RightBottom, rect.Height);
            Point bottomLeft = new Point(radii.LeftBottom, rect.Height);
            Point leftBottom = new Point(0, rect.Height - radii.BottomLeft);
            Point leftTop = new Point(0, radii.TopLeft);

            //
            //  check keypoints for overlap and resolve by partitioning radii according to
            //  the percentage of each one.  
            //

            //  top edge is handled here
            if (topLeft.X > topRight.X)
            {
                double v = (radii.LeftTop) / (radii.LeftTop + radii.RightTop) * rect.Width;
                topLeft.X = v;
                topRight.X = v;
            }

            //  right edge
            if (rightTop.Y > rightBottom.Y)
            {
                double v = (radii.TopRight) / (radii.TopRight + radii.BottomRight) * rect.Height;
                rightTop.Y = v;
                rightBottom.Y = v;
            }

            //  bottom edge
            if (bottomRight.X < bottomLeft.X)
            {
                double v = (radii.LeftBottom) / (radii.LeftBottom + radii.RightBottom) * rect.Width;
                bottomRight.X = v;
                bottomLeft.X = v;
            }

            // left edge
            if (leftBottom.Y < leftTop.Y)
            {
                double v = (radii.TopLeft) / (radii.TopLeft + radii.BottomLeft) * rect.Height;
                leftBottom.Y = v;
                leftTop.Y = v;
            }

            //
            //  add on offsets
            //

            Vector offset = new Vector(rect.TopLeft.X, rect.TopLeft.Y);
            topLeft += offset;
            topRight += offset;
            rightTop += offset;
            rightBottom += offset;
            bottomRight += offset;
            bottomLeft += offset;
            leftBottom += offset;
            leftTop += offset;

            //
            //  create the border geometry
            //
            ctx.BeginFigure(topLeft, true /* is filled */, true /* is closed */);

            // Top line
            ctx.LineTo(topRight, true /* is stroked */, false /* is smooth join */);

            // Upper-right corner
            double radiusX = rect.TopRight.X - topRight.X;
            double radiusY = rightTop.Y - rect.TopRight.Y;
            if (!DoubleUtil.IsZero(radiusX)
                || !DoubleUtil.IsZero(radiusY))
            {
                ctx.ArcTo(rightTop, new Size(radiusX, radiusY), 0, false, SweepDirection.Clockwise, true, false);
            }

            // Right line
            ctx.LineTo(rightBottom, true /* is stroked */, false /* is smooth join */);

            // Lower-right corner
            radiusX = rect.BottomRight.X - bottomRight.X;
            radiusY = rect.BottomRight.Y - rightBottom.Y;
            if (!DoubleUtil.IsZero(radiusX)
                || !DoubleUtil.IsZero(radiusY))
            {
                ctx.ArcTo(bottomRight, new Size(radiusX, radiusY), 0, false, SweepDirection.Clockwise, true, false);
            }

            // Bottom line
            ctx.LineTo(bottomLeft, true /* is stroked */, false /* is smooth join */);

            // Lower-left corner
            radiusX = bottomLeft.X - rect.BottomLeft.X;
            radiusY = rect.BottomLeft.Y - leftBottom.Y;
            if (!DoubleUtil.IsZero(radiusX)
                || !DoubleUtil.IsZero(radiusY))
            {
                ctx.ArcTo(leftBottom, new Size(radiusX, radiusY), 0, false, SweepDirection.Clockwise, true, false);
            }

            // Left line
            ctx.LineTo(leftTop, true /* is stroked */, false /* is smooth join */);

            // Upper-left corner
            radiusX = topLeft.X - rect.TopLeft.X;
            radiusY = leftTop.Y - rect.TopLeft.Y;
            if (!DoubleUtil.IsZero(radiusX)
                || !DoubleUtil.IsZero(radiusY))
            {
                ctx.ArcTo(topLeft, new Size(radiusX, radiusY), 0, false, SweepDirection.Clockwise, true, false);
            }
        }

        //
        //  This property
        //  1. Finds the correct initial size for the _effectiveValues store on the current DependencyObject
        //  2. This is a performance optimization
        //
        internal override int EffectiveValuesInitialSize
        {
            get { return 9; }
        }

        #endregion Private Methods

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        private bool _useComplexRenderCodePath;

        #endregion Private Fields

        #region Cache

        private static readonly UncommonField<StreamGeometry> BorderGeometryField = new UncommonField<StreamGeometry>();
        private static readonly UncommonField<StreamGeometry> BackgroundGeometryField = new UncommonField<StreamGeometry>();
        private static readonly UncommonField<Pen> LeftPenField = new UncommonField<Pen>();
        private static readonly UncommonField<Pen> RightPenField = new UncommonField<Pen>();
        private static readonly UncommonField<Pen> TopPenField = new UncommonField<Pen>();
        private static readonly UncommonField<Pen> BottomPenField = new UncommonField<Pen>();

        private StreamGeometry BorderGeometryCache
        {
            get
            {
                return BorderGeometryField.GetValue(this);
            }

            set
            {
                if (value == null)
                {
                    BorderGeometryField.ClearValue(this);
                }
                else
                {
                    BorderGeometryField.SetValue(this, value);
                }
            }
        }

        private StreamGeometry BackgroundGeometryCache
        {
            get
            {
                return BackgroundGeometryField.GetValue(this);
            }

            set
            {
                if (value == null)
                {
                    BackgroundGeometryField.ClearValue(this);
                }
                else
                {
                    BackgroundGeometryField.SetValue(this, value);
                }
            }
        }

        private Pen LeftPenCache
        {
            get
            {
                return LeftPenField.GetValue(this);
            }

            set
            {
                if (value == null)
                {
                    LeftPenField.ClearValue(this);
                }
                else
                {
                    LeftPenField.SetValue(this, value);
                }
            }
        }

        private Pen RightPenCache
        {
            get
            {
                return RightPenField.GetValue(this);
            }

            set
            {
                if (value == null)
                {
                    RightPenField.ClearValue(this);
                }
                else
                {
                    RightPenField.SetValue(this, value);
                }
            }
        }

        private Pen TopPenCache
        {
            get
            {
                return TopPenField.GetValue(this);
            }

            set
            {
                if (value == null)
                {
                    TopPenField.ClearValue(this);
                }
                else
                {
                    TopPenField.SetValue(this, value);
                }
            }
        }

        private Pen BottomPenCache
        {
            get
            {
                return BottomPenField.GetValue(this);
            }

            set
            {
                if (value == null)
                {
                    BottomPenField.ClearValue(this);
                }
                else
                {
                    BottomPenField.SetValue(this, value);
                }
            }
        }

        #endregion Cache

        //-------------------------------------------------------------------
        //
        //  Private Structures Classes
        //
        //-------------------------------------------------------------------

        #region Private Structures Classes

        private struct Radii
        {
            internal Radii(CornerRadius radii, Thickness borders, bool outer)
            {
                double left     = 0.5 * borders.Left;
                double top      = 0.5 * borders.Top;
                double right    = 0.5 * borders.Right;
                double bottom   = 0.5 * borders.Bottom;

                if (outer)
                {
                    if (DoubleUtil.IsZero(radii.TopLeft))
                    {
                        LeftTop = TopLeft = 0.0;
                    }
                    else
                    {
                        LeftTop = radii.TopLeft + left;
                        TopLeft = radii.TopLeft + top;
                    }
                    if (DoubleUtil.IsZero(radii.TopRight))
                    {
                        TopRight = RightTop = 0.0;
                    }
                    else
                    {
                        TopRight = radii.TopRight + top;
                        RightTop = radii.TopRight + right;
                    }
                    if (DoubleUtil.IsZero(radii.BottomRight))
                    {
                        RightBottom = BottomRight = 0.0;
                    }
                    else
                    {
                        RightBottom = radii.BottomRight + right;
                        BottomRight = radii.BottomRight + bottom;
                    }
                    if (DoubleUtil.IsZero(radii.BottomLeft))
                    {
                        BottomLeft = LeftBottom = 0.0;
                    }
                    else
                    {
                        BottomLeft = radii.BottomLeft + bottom;
                        LeftBottom = radii.BottomLeft + left;
                    }
                }
                else
                {
                    LeftTop     = Math.Max(0.0, radii.TopLeft - left);
                    TopLeft     = Math.Max(0.0, radii.TopLeft - top);
                    TopRight    = Math.Max(0.0, radii.TopRight - top);
                    RightTop    = Math.Max(0.0, radii.TopRight - right);
                    RightBottom = Math.Max(0.0, radii.BottomRight - right);
                    BottomRight = Math.Max(0.0, radii.BottomRight - bottom);
                    BottomLeft  = Math.Max(0.0, radii.BottomLeft - bottom);
                    LeftBottom  = Math.Max(0.0, radii.BottomLeft - left);
                }
            }

            internal double LeftTop;
            internal double TopLeft;
            internal double TopRight;
            internal double RightTop;
            internal double RightBottom;
            internal double BottomRight;
            internal double BottomLeft;
            internal double LeftBottom;
        }

        #endregion Private Structures Classes
    }
}
