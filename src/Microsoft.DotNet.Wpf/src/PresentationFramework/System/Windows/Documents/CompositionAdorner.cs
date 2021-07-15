// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Composition adorner to render the composition display attribute.
// 

namespace System.Windows.Documents
{
    using System.Collections; // ArrayList
    using System.Diagnostics;
    using System.Windows.Media; // Brush, Transform
    using System.Windows.Controls; // TextBox
    using System.Windows.Controls.Primitives; // TextBoxBase
    using System.Windows.Input; // InputLanguageManager
    using System.Windows.Threading; // Dispatcher
    using MS.Win32;             // TextServices
    using MS.Internal; // Invariant

    internal class CompositionAdorner : Adorner
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        static CompositionAdorner()
        {
            // Provide a new default value for the composition adorner so that it is not hit-testable.
            IsEnabledProperty.OverrideMetadata(typeof(CompositionAdorner), new FrameworkPropertyMetadata(false));
        }
        
        /// <summary>
        /// Creates new instance of CompositionAdorner.
        /// </summary>
        /// <param name="textView">
        /// TextView to which this CompositionAdorner is attached as adorner.
        /// </param>
        internal CompositionAdorner(ITextView textView) : this(textView, new ArrayList())
        {
        }

        /// <summary>
        /// Creates new instance of CompositionAdorner.
        /// </summary>
        /// <param name="textView">
        /// TextView to which this CompositionAdorner is attached as adorner.
        /// </param>
        /// <param name="attributeRanges">
        /// Attribute ranges
        /// </param>
        internal CompositionAdorner(ITextView textView, ArrayList attributeRanges)
            : base(textView.RenderScope)
        {
            Debug.Assert(textView != null && textView.RenderScope != null);

            // TextView to which this CompositionAdorner is attached as adorner and it will
            // als be used for GetRectangleFromTextPosition/GetLineRange
            _textView = textView;

            // Create ArrayList for the composition attribute ranges and composition lines
            _attributeRanges = attributeRanges;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Add a transform so that the composition adorner gets positioned at the correct spot within the text being edited
        /// </summary>
        /// <param name="transform">
        /// The transform applied to the object the adorner adorns
        /// </param>
        /// <returns>
        /// Transform to apply to the adorner
        /// </returns>
        public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
        {            
            TranslateTransform translation;
            GeneralTransformGroup group = new GeneralTransformGroup();

            // Get the matrix transform out, skip all non affine transforms
            Transform t = transform.AffineTransform;
            if (t == null)
            {                
                t = Transform.Identity;                
            }

            // Translate the adorner to (0, 0) point
            translation = new TranslateTransform(-(t.Value.OffsetX), -(t.Value.OffsetY));

            group.Children.Add(translation);

            if (transform != null)
            {
                group.Children.Add(transform);
            }

            return group;
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------
 
        #region Protected Methods

        /// <summary>
        /// Render override to render the composition adorner here.
        /// </summary>
        protected override void OnRender(DrawingContext drawingContext)
        {
            // Get the matrix from AdornedElement to the visual parent to get the transformed
            // start/end point
            Visual parent2d = VisualTreeHelper.GetParent(this.AdornedElement) as Visual;
            if (parent2d == null)
            {
                return;
            }

            GeneralTransform transform = AdornedElement.TransformToAncestor(parent2d);
            if (transform == null)
            {
                return;
            }

            // Please note that we do the highlight adornment for the CONVERTED text only 
            // for Simplified Chinese IMEs. Doing this uniformly across all IMEs wasnt possible 
            // because it was noted that some of them (viz. Japanese) werent being consistent 
            // about this attribute.

            bool isChinesePinyin = chinesePinyin.Equals(InputLanguageManager.Current.CurrentInputLanguage.IetfLanguageTag);

            // Render the each of the composition string attribute from the attribute ranges.
            for (int i = 0; i < _attributeRanges.Count; i++)
            {
                DoubleCollection dashArray;

                // Get the composition attribute range from the attribute range lists
                AttributeRange attributeRange = (AttributeRange)_attributeRanges[i];

                // Skip the rendering composition lines if the composition line doesn't exist.
                if (attributeRange.CompositionLines.Count == 0)
                {
                    continue;
                }

                // Set the line bold and squiggle
                bool lineBold = attributeRange.TextServicesDisplayAttribute.IsBoldLine ? true : false;
                bool squiggle = false;
                bool hasVirtualSelection = (attributeRange.TextServicesDisplayAttribute.AttrInfo & UnsafeNativeMethods.TF_DA_ATTR_INFO.TF_ATTR_TARGET_CONVERTED) != 0;

                Brush selectionBrush = null;
                double selectionOpacity = -1;
                Pen selectionPen = null;

                if (isChinesePinyin && hasVirtualSelection)
                {
                    DependencyObject owner = _textView.TextContainer.Parent;
                    selectionBrush = (Brush)owner.GetValue(TextBoxBase.SelectionBrushProperty);
                    selectionOpacity = (double)owner.GetValue(TextBoxBase.SelectionOpacityProperty);
                }

                // Set the line height and cluse gap value that base on the ratio of text height
                double height = attributeRange.Height;
                double lineHeight = height * (lineBold ? BoldLineHeightRatio : NormalLineHeightRatio);
                double clauseGap = height * ClauseGapRatio;

                // Create Pen for drawing the composition lines with the specified line color
                Pen pen = new Pen(new SolidColorBrush(Colors.Black), lineHeight);

                // Set the pen style that based on IME's composition line style
                switch (attributeRange.TextServicesDisplayAttribute.LineStyle)
                {
                    case UnsafeNativeMethods.TF_DA_LINESTYLE.TF_LS_DOT:
                        // Add the dot length and specify the start/end line cap as the round
                        dashArray = new DoubleCollection();
                        dashArray.Add(DotLength);
                        dashArray.Add(DotLength);

                        pen.DashStyle = new DashStyle(dashArray, 0);
                        pen.DashCap = System.Windows.Media.PenLineCap.Round;
                        pen.StartLineCap = System.Windows.Media.PenLineCap.Round;
                        pen.EndLineCap = System.Windows.Media.PenLineCap.Round;

                        // Update the line height for the dot line. Dot line will be more thickness than
                        // other line to show it clearly.
                        lineHeight = height * (lineBold ? BoldDotLineHeightRatio : NormalDotLineHeightRatio);

                        break;

                    case UnsafeNativeMethods.TF_DA_LINESTYLE.TF_LS_DASH:
                        double dashLength = height * (lineBold ? BoldDashRatio : NormalDashRatio);
                        double dashGapLength = height * (lineBold ? BoldDashGapRatio : NormalDashGapRatio);

                        // Add the dash and dash gap legth
                        dashArray = new DoubleCollection();
                        dashArray.Add(dashLength);
                        dashArray.Add(dashGapLength);

                        pen.DashStyle = new DashStyle(dashArray, 0);
                        pen.DashCap = System.Windows.Media.PenLineCap.Round;
                        pen.StartLineCap = System.Windows.Media.PenLineCap.Round;
                        pen.EndLineCap = System.Windows.Media.PenLineCap.Round;

                        break;

                    case UnsafeNativeMethods.TF_DA_LINESTYLE.TF_LS_SOLID:
                        pen.StartLineCap = System.Windows.Media.PenLineCap.Round;
                        pen.EndLineCap = System.Windows.Media.PenLineCap.Round;

                        break;

                    case UnsafeNativeMethods.TF_DA_LINESTYLE.TF_LS_SQUIGGLE:
                        squiggle = true;
                        break;
                }

                double halfLineHeight = lineHeight / 2;

                // Draw the each of the composition line
                for (int j = 0; j < attributeRange.CompositionLines.Count; j++)
                {
                    CompositionLine compositionLine = (CompositionLine)attributeRange.CompositionLines[j];

                    // Get the start/end point for composition adorner.
                    // Currently Text doesn't aware of the spaceroom for the drawing of the composition 
                    // adorner(like as normal/bold dot/line/squggle), so we should draw the composition adorners
                    // to the closest area of the bottom text.
                    Point startPoint = new Point(compositionLine.StartPoint.X + clauseGap, compositionLine.StartPoint.Y - halfLineHeight);
                    Point endPoint = new Point(compositionLine.EndPoint.X - clauseGap, compositionLine.EndPoint.Y - halfLineHeight);
                    
                    // Apply composition line color which is actually the foreground of text as well
                    pen.Brush = new SolidColorBrush(compositionLine.LineColor);

                    // Apply matrix to start/end point
                    // REVIEW: if the points can't be transformed, should we not draw anything?
                    transform.TryTransform(startPoint, out startPoint);
                    transform.TryTransform(endPoint, out endPoint);

                    if (isChinesePinyin && hasVirtualSelection)
                    {
                        Rect rect = Rect.Union(compositionLine.StartRect, compositionLine.EndRect);
                        rect = transform.TransformBounds(rect);
                        
                        drawingContext.PushOpacity(selectionOpacity);
                    
                        drawingContext.DrawRectangle(selectionBrush, selectionPen, rect);
                    
                        drawingContext.Pop();
                    }
                    
                    if (squiggle)
                    {
                        // Draw the squiggle line with using of the PathFigure and DrawGemetry.
                        // We may revisit this logic to render the smooth squiggle line.
                        Point pathPoint = new Point(startPoint.X, startPoint.Y - halfLineHeight);

                        double squiggleGap = halfLineHeight;

                        PathFigure pathFigure = new PathFigure();
                        pathFigure.StartPoint = pathPoint;

                        int indexPoint = 0;

                        while (indexPoint < ((endPoint.X - startPoint.X) / (squiggleGap)))
                        {
                            if (indexPoint % 4 == 0 || indexPoint % 4 == 3)
                            {
                                pathPoint = new Point(pathPoint.X + squiggleGap, pathPoint.Y + halfLineHeight);
                                pathFigure.Segments.Add(new LineSegment(pathPoint, true));
                            }
                            else if (indexPoint % 4 == 1 || indexPoint % 4 == 2)
                            {
                                pathPoint = new Point(pathPoint.X + squiggleGap, pathPoint.Y - halfLineHeight);
                                pathFigure.Segments.Add(new LineSegment(pathPoint, true));
                            }

                            indexPoint++;
                        }

                        PathGeometry pathGeometry = new PathGeometry();
                        pathGeometry.Figures.Add(pathFigure);

                        // Draw the composition line with the squiggle
                        drawingContext.DrawGeometry(null, pen, pathGeometry);
                    }
                    else
                    {
                        drawingContext.DrawLine(pen, startPoint, endPoint);
                    }
                }
            }
        }

        #endregion Protected Events

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Add the composition attribute range that will be rendered.
        /// Used in TextServicesDisplayAttributePropertyRanges (for composition display attribute)
        /// </summary>
        internal void AddAttributeRange(ITextPointer start, ITextPointer end, TextServicesDisplayAttribute textServiceDisplayAttribute)
        {
            // Set the range start/end point's logical direction
            ITextPointer rangeStart = start.CreatePointer(LogicalDirection.Forward);
            ITextPointer rangeEnd = end.CreatePointer(LogicalDirection.Backward);

            // Add the composition attribute range
            _attributeRanges.Add(new AttributeRange(_textView, rangeStart, rangeEnd, textServiceDisplayAttribute));
        }

        /// <summary>
        /// Invalidates the CompositionAdorner render.
        /// Used in TextServicesDisplayAttributePropertyRanges (for composition display attribute)
        /// </summary>
        internal void InvalidateAdorner()
        {
            for (int i = 0; i < _attributeRanges.Count; i++)
            {
                // Get the composition attribute range from the attribute range lists
                AttributeRange attributeRange = (AttributeRange)_attributeRanges[i];

                // Add the composition lines for rendering the composition lines
                attributeRange.AddCompositionLines();
            }

            // Invalidate the CompositionAdorner to update the rendering.
            AdornerLayer adornerLayer = VisualTreeHelper.GetParent(this) as AdornerLayer;
            if (adornerLayer != null)
            {
                adornerLayer.Update(AdornedElement);
                adornerLayer.InvalidateArrange();
            }
        }

        /// <summary>
        /// Add CompositionAdorner to the scoping AdornerLayer.
        /// </summary>
        internal void Initialize(ITextView textView)
        {
            Debug.Assert(_adornerLayer == null, "Attempt to overwrite existing AdornerLayer!");

            _adornerLayer = AdornerLayer.GetAdornerLayer(textView.RenderScope);

            if (_adornerLayer != null)
            {
                // Add the CompositionAdorner to the scoping of AdornerLayer
                _adornerLayer.Add(this);
            }
        }

        /// <summary>
        /// Remove this CompositionAdorner from its AdornerLayer.
        /// </summary>
        internal void Uninitialize()
        {
            if (_adornerLayer != null)
            {
                // Remove CompositionAdorner form the socping of AdornerLayer
                _adornerLayer.Remove(this);
                _adornerLayer = null;
            }
        }

        #endregion Internal methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // AdornerLayer holding this CompositionAdorner visual
        private AdornerLayer _adornerLayer;

        // TextView to which this CompositionAdorner is attached as adorner
        private ITextView _textView;

        // ArrayList for the composition attribute ranges
        private readonly ArrayList _attributeRanges;

        // Composition line's dot length
        private const double DotLength = 1.2;

        // Ratio of the composition line
        private const double NormalLineHeightRatio = 0.06;

        // Ratio of the bold composition line
        private const double BoldLineHeightRatio = 0.08;

        // Ratio of the composition line of dot
        private const double NormalDotLineHeightRatio = 0.08;

        // Ratio of the bold composition line of dot
        private const double BoldDotLineHeightRatio = 0.10;

        // Ratio of the composition line's dash 
        private const double NormalDashRatio = 0.27;

        // Ratio of the bold composition line's dash 
        private const double BoldDashRatio = 0.39;

        // Ratio of the composition line's clause gap 
        private const double ClauseGapRatio = 0.09;

        // Ratio of the composition line's dash gap 
        private const double NormalDashGapRatio = 0.04;

        // Ratio of the bold composition line's dash gap 
        private const double BoldDashGapRatio = 0.06;

        // The culture name for Chinese Pinyin
        private const string chinesePinyin = "zh-CN";

        #endregion Private Fields

        //------------------------------------------------------
        //
        //  Private Class
        //
        //------------------------------------------------------

        private class AttributeRange
        {
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            internal AttributeRange(ITextView textView, ITextPointer start, ITextPointer end, TextServicesDisplayAttribute textServicesDisplayAttribute)
            {
                _textView = textView;
                _startOffset = start.Offset;
                _endOffset = end.Offset;

                _textServicesDisplayAttribute = textServicesDisplayAttribute;

                _compositionLines = new ArrayList(1);
            }

            #endregion Constructors

            //------------------------------------------------------
            //
            //  Internal Methods
            //
            //------------------------------------------------------

            #region Internal Methods

            // Add the composition lines for rendering the composition lines.
            internal void AddCompositionLines()
            {
                // Erase any current lines.
                _compositionLines.Clear();

                ITextPointer start = _textView.TextContainer.Start.CreatePointer(_startOffset, LogicalDirection.Forward);
                ITextPointer end = _textView.TextContainer.Start.CreatePointer(_endOffset, LogicalDirection.Backward);

                while (start.CompareTo(end) < 0 && start.GetPointerContext(LogicalDirection.Forward) != TextPointerContext.Text)
                {
                    start.MoveToNextContextPosition(LogicalDirection.Forward);
                }

                Invariant.Assert(start.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text);

                if (end.HasValidLayout)
                {
                    // Get the rectangle for start/end position
                    _startRect = _textView.GetRectangleFromTextPosition(start);
                    _endRect = _textView.GetRectangleFromTextPosition(end);

                    // Check whether the composition line is single or multiple lines
                    if (_startRect.Top != _endRect.Top)
                    {
                        // Add the composition lines to be rendered for the composition string
                        AddMultipleCompositionLines(start, end);
                    }
                    else
                    {
                        // Set the start/end pointer to draw the line
                        Color lineColor = _textServicesDisplayAttribute.GetLineColor(start);
                        // Add the composition line to be rendered
                        _compositionLines.Add(new CompositionLine(_startRect, _endRect, lineColor));
                    }
                }
            }

            #endregion Internal Methods

            //------------------------------------------------------
            //
            //  Internal Properties
            //
            //------------------------------------------------------

            #region Internal Properties

            /// <summary>
            /// Height of the composition attribute range.
            /// </summary>
            internal double Height
            {
                get
                {
                    return _startRect.Bottom - _startRect.Top;
                }
            }

            /// <summary>
            /// CompositionLines of the composition attribute range.
            /// </summary>
            internal ArrayList CompositionLines
            {
                get
                {
                    return _compositionLines;
                }
            }

            /// <summary>
            /// Composition attribute information.
            /// </summary>
            internal TextServicesDisplayAttribute TextServicesDisplayAttribute
            {
                get
                {
                    return _textServicesDisplayAttribute;
                }
            }

            #endregion Internal Properties

            //------------------------------------------------------
            //
            //  Private Methods
            //
            //------------------------------------------------------

            #region Private Methods

            private void AddMultipleCompositionLines(ITextPointer start, ITextPointer end)
            {
                // Initalize the start/end line pointer
                ITextPointer startLinePointer = start;
                ITextPointer endLinePointer = startLinePointer;

                // Get all composition lines that includes the start/end pointer
                while (endLinePointer.CompareTo(end) < 0)
                {
                    TextSegment textSegment = _textView.GetLineRange(endLinePointer);

                    if (textSegment.IsNull)
                    {
                        // endLinePointer is not within the TextView's definition of a line.
                        // Skip ahead to text on the next iteration.
                        startLinePointer = endLinePointer;
                    }
                    else
                    {
                        Debug.Assert(start.CompareTo(startLinePointer) <= 0, "The start pointer is positioned after the composition start line pointer!");

                        if (startLinePointer.CompareTo(textSegment.Start) < 0)
                        {
                            // Update the start line pointer
                            startLinePointer = textSegment.Start;
                        }

                        if (endLinePointer.CompareTo(textSegment.End) < 0)
                        {
                            if (end.CompareTo(textSegment.End) < 0)
                            {
                                // Update the end line pointer
                                endLinePointer = end.CreatePointer();
                            }
                            else
                            {
                                // Update the end line pointer
                                endLinePointer = textSegment.End.CreatePointer(LogicalDirection.Backward);
                            }
                        }
                        else
                        {
                            Debug.Assert(endLinePointer.CompareTo(textSegment.End) == 0, "The end line pointer is positioned after the composition text range end pointer!");
                        }

                        // Get the rectangle for start/end position
                        Rect startRect = _textView.GetRectangleFromTextPosition(startLinePointer);
                        Rect endRect = _textView.GetRectangleFromTextPosition(endLinePointer);

                        // Add the composition line to be rendered
                        _compositionLines.Add(new CompositionLine(startRect, endRect, _textServicesDisplayAttribute.GetLineColor(startLinePointer)));

                        startLinePointer = textSegment.End.CreatePointer(LogicalDirection.Forward);
                    }

                    // Move the start pointer to the next text line. startLinePointer must be a pointer to start 
                    // text.
                    while ((startLinePointer.GetPointerContext(LogicalDirection.Forward) != TextPointerContext.None) &&
                           (startLinePointer.GetPointerContext(LogicalDirection.Forward) != TextPointerContext.Text))
                    {
                        startLinePointer.MoveToNextContextPosition(LogicalDirection.Forward);
                    }
                    endLinePointer = startLinePointer;
                }
            }

            #endregion Private methods

            //------------------------------------------------------
            //
            //  Private Fields
            //
            //------------------------------------------------------

            #region Private Fields

            // TextView to which this CompositionAdorner is attached as adorner
            private ITextView _textView;

            // Start rect of the composition attribute range
            private Rect _startRect;

            // End rect of the composition attribute range
            private Rect _endRect;

            // Start position offset of the composition attribute range
            private readonly int _startOffset;

            // End position offset of the composition attribute range
            private readonly int _endOffset;

            // Composition display attribute that is specified from IME
            private readonly TextServicesDisplayAttribute _textServicesDisplayAttribute;

            // ArrayList for the composition lines
            private readonly ArrayList _compositionLines;

            #endregion Private Fields
        }

        private class CompositionLine
        {
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            internal CompositionLine(Rect startRect, Rect endRect, Color lineColor)
            {
                _startRect = startRect;
                _endRect = endRect;
                _color = lineColor;
            }

            #endregion Constructors

            //------------------------------------------------------
            //
            //  Internal Properties
            //
            //------------------------------------------------------

            #region Internal Properties

            /// <summary>
            /// Start point of the composition line draw
            /// </summary>
            internal Point StartPoint
            {
                get
                {
                    return _startRect.BottomLeft;
                }
            }

            /// <summary>
            /// End point of the composition line draw
            /// </summary>
            internal Point EndPoint
            {
                get
                {
                    return _endRect.BottomRight;
                }
            }

            /// <summary>
            /// Start rect of the composition line draw
            /// </summary>
            internal Rect StartRect
            {
                get 
                { 
                    return _startRect; 
                }
            }
            
            /// <summary>
            /// End rect of the composition line draw
            /// </summary>
            internal Rect EndRect
            {
                get 
                { 
                    return _endRect; 
                }
            }
            
            /// <summary>
            /// Color of the composition line draw
            /// </summary>
            internal Color LineColor
            {
                get
                {
                    return _color;
                }
            }

            #endregion Internal Properties

            //------------------------------------------------------
            //
            //  Private Fields
            //
            //------------------------------------------------------

            #region Private Fields

            // Start point of the composition line draw
            private Rect _startRect;

            // End point of the composition line draw
            private Rect _endRect;

            // Color of the composition line draw
            private Color _color;

            #endregion Private Fields
        }
    }
}


