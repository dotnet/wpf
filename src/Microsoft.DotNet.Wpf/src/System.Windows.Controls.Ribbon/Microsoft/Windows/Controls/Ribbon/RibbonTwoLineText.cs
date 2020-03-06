// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon
#else
namespace Microsoft.Windows.Controls.Ribbon
#endif
{
    #region Using declarations

    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Automation.Peers;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Media;
    using System.Windows.Shapes;
#if RIBBON_IN_FRAMEWORK
    using Microsoft.Windows.Controls;
#else
    using Microsoft.Windows.Automation.Peers;
#endif

    #endregion

    /// <summary>
    ///   A specialized label that can lay itself out on at most two lines, and
    ///   also has the ability to display a special Geometry inline with its text.
    /// </summary>
    [TemplatePart(Name = "PART_TextBlock1", Type = typeof(TextBlock))]
    [TemplatePart(Name = "PART_TextBlock2", Type = typeof(TextBlock))]
    [TemplatePart(Name = "PART_Path", Type = typeof(Path))]
    public class RibbonTwoLineText : Control
    {
        #region Constructors
        
        static RibbonTwoLineText()
        {
            Type ownerType = typeof(RibbonTwoLineText);

            DefaultStyleKeyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(ownerType));
            FocusableProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(false));
        }

        #endregion

        #region Fields

        private TextBlock _textBlock1, _textBlock2;
        private Path _path;

        #endregion

        #region Public Properties

        public TextDecorationCollection TextDecorations
        {
            get { return (TextDecorationCollection)GetValue(TextDecorationsProperty); }
            set { SetValue(TextDecorationsProperty, value); }
        }

        public static readonly DependencyProperty TextDecorationsProperty = TextBlock.TextDecorationsProperty.AddOwner(typeof(RibbonTwoLineText));

        public TextEffectCollection TextEffects
        {
            get { return (TextEffectCollection)GetValue(TextEffectsProperty); }
            set { SetValue(TextEffectsProperty, value); }
        }

        public static readonly DependencyProperty TextEffectsProperty = TextBlock.TextEffectsProperty.AddOwner(typeof(RibbonTwoLineText));
        
        public double BaselineOffset
        {
            get { return (double)GetValue(BaselineOffsetProperty); }
            set { SetValue(BaselineOffsetProperty, value); }
        }

        public static readonly DependencyProperty BaselineOffsetProperty = TextBlock.BaselineOffsetProperty.AddOwner(typeof(RibbonTwoLineText));
        
        new public Thickness Padding
        {
            get { return (Thickness)GetValue(PaddingProperty); }
            set { SetValue(PaddingProperty, value); }
        }

        new public static readonly DependencyProperty PaddingProperty = TextBlock.PaddingProperty.AddOwner(typeof(RibbonTwoLineText));

        /// <summary>
        ///   Gets or sets the TextAlignment set on the internal text block.
        /// </summary>
        public TextAlignment TextAlignment
        {
            get { return (TextAlignment)GetValue(TextAlignmentProperty); }
            set { SetValue(TextAlignmentProperty, value); }
        }

        /// <summary>
        ///   Adds RibbonTwoLineText as an owner of this property.
        /// </summary>
        public static readonly DependencyProperty TextAlignmentProperty =
            TextBlock.TextAlignmentProperty.AddOwner(typeof(RibbonTwoLineText));

        /// <summary>
        ///   Gets or sets the TextTrimming set on the internal text block.
        /// </summary>
        public TextTrimming TextTrimming
        {
            get { return (TextTrimming)GetValue(TextTrimmingProperty); }
            set { SetValue(TextTrimmingProperty, value); }
        }

        /// <summary>
        ///   Adds RibbonTwoLineText as an owner of this property.
        /// </summary>
        public static readonly DependencyProperty TextTrimmingProperty =
            TextBlock.TextTrimmingProperty.AddOwner(typeof(RibbonTwoLineText));


        /// <summary>
        ///   Gets or sets the LineHeight set on the internal text blocks.
        /// </summary>
        [TypeConverter(typeof(LengthConverter))]
        public double LineHeight
        {
            get { return (double)GetValue(LineHeightProperty); }
            set { SetValue(LineHeightProperty, value); }
        }

        /// <summary>
        ///   Adds RibbonTwoLineText as an owner of this property.
        /// </summary>
        public static readonly DependencyProperty LineHeightProperty =
            TextBlock.LineHeightProperty.AddOwner(typeof(RibbonTwoLineText));
        
        /// <summary>
        ///   Gets or sets the LineStackingStrategy set on the internal text blocks.
        /// </summary>
        public LineStackingStrategy LineStackingStrategy
        {
            get { return (LineStackingStrategy)GetValue(LineStackingStrategyProperty); }
            set { SetValue(LineStackingStrategyProperty, value); }
        }

        /// <summary>
        ///   Adds RibbonTwoLineText as an owner of this property.
        /// </summary>
        public static readonly DependencyProperty LineStackingStrategyProperty =
            TextBlock.LineStackingStrategyProperty.AddOwner(typeof(RibbonTwoLineText));

        /// <summary>
        ///   Gets or sets the fill of the label's appending geometry.
        /// </summary>
        public Brush PathFill
        {
            get { return (Brush)GetValue(PathFillProperty); }
            set { SetValue(PathFillProperty, value); }
        }

        /// <summary>
        ///   Using a DependencyProperty as the backing store for PathFillProperty.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty PathFillProperty =
                    DependencyProperty.Register(
                            "PathFill",
                            typeof(Brush),
                            typeof(RibbonTwoLineText),
                            null);

        /// <summary>
        ///   Gets or sets the fill of the label's appending geometry.
        /// </summary>
        public Brush PathStroke
        {
            get { return (Brush)GetValue(PathStrokeProperty); }
            set { SetValue(PathStrokeProperty, value); }
        }

        /// <summary>
        ///   Using a DependencyProperty as the backing store for PathStrokeProperty.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty PathStrokeProperty =
                    DependencyProperty.Register(
                            "PathStroke",
                            typeof(Brush),
                            typeof(RibbonTwoLineText),
                            null);

        /// <summary>
        ///   Gets or sets the label's text.
        /// </summary>
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        /// <summary>
        ///   Using a DependencyProperty as the backing store for TextProperty.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty TextProperty =
                    TextBlock.TextProperty.AddOwner(typeof(RibbonTwoLineText), 
                    new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        ///   Defines Path data for the label's appending Geometry.
        /// </summary>
        public static readonly DependencyProperty PathDataProperty =
                    DependencyProperty.RegisterAttached(
                            "PathData",
                            typeof(Geometry),
                            typeof(RibbonTwoLineText),
                            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static Geometry GetPathData(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (Geometry)element.GetValue(PathDataProperty);
        }

        public static void SetPathData(DependencyObject element, Geometry value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(PathDataProperty, value);
        }

        /// <summary>
        ///   Using a DependencyProperty as the backing store for HasTwoLinesProperty.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty HasTwoLinesProperty
            = DependencyProperty.RegisterAttached("HasTwoLines",
                                                    typeof(bool),
                                                    typeof(RibbonTwoLineText),
                                                    new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure));
        
        /// <summary>
        ///   Gets or sets a value indicating whether the RibbonTwoLineText should be allowed to
        ///   layout on two lines or if it should be restricted to one.
        /// </summary>
        public static bool GetHasTwoLines(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (bool)element.GetValue(HasTwoLinesProperty);
        }

        public static void SetHasTwoLines(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(HasTwoLinesProperty, value);
        }

        #endregion

        #region Protected Methods

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _textBlock1 = this.GetTemplateChild("PART_TextBlock1") as TextBlock;
            _textBlock2 = this.GetTemplateChild("PART_TextBlock2") as TextBlock;
            _path = this.GetTemplateChild("PART_Path") as Path;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (_textBlock1 != null && _textBlock2 != null && _path != null)
            {
                bool hasTwoLines = GetHasTwoLines(this);
                if (!hasTwoLines)
                {
                    // Collapse first line
                    _textBlock1.Text = null;
                    _textBlock1.Visibility = Visibility.Collapsed;

                    // All text shown in second line
                    _textBlock2.Text = this.Text;
                    _textBlock2.Visibility = Visibility.Visible;

                    // Mark measure dirty all UIElements in the visual tree from _textBlock2 upto RibbonTwoLineText
                    TreeHelper.InvalidateMeasureForVisualAncestorPath<RibbonTwoLineText>(_textBlock2);
                }
                else   // HasTwoLines
                {
                    // Querying the ContentStart property for the very first time has a potential of invalidating 
                    // measure on the TextBlock. So we want to do this once before we call Measure and Arrange to 
                    // guarantee valid layout
                    TextPointer secondLinePointer = _textBlock1.ContentStart;
 
                    // Determine LineBreak with availableSize
                    MeasureWithConstraint(availableSize, out secondLinePointer);

                    if (secondLinePointer == null)
                    {
                        MeasureWithoutConstraint(out secondLinePointer);
                    }

                    if (secondLinePointer != null)
                    {
                        // We need to split Text manually
                        string firstLineText = secondLinePointer.GetTextInRun(LogicalDirection.Backward);
                        string secondLineText = secondLinePointer.GetTextInRun(LogicalDirection.Forward);

                        // Display first line in full 
                        _textBlock1.Text = firstLineText;
                        _textBlock1.Visibility = Visibility.Visible;

                        // remaining text in second line. Trimming will be applied if TextTrimming is set,
                        // or else extra text will be clipped.
                        _textBlock2.Text = secondLineText;
                        _textBlock2.Visibility = Visibility.Visible;

                        // Mark measure dirty all UIElements in the visual tree from _textBlock2 upto RibbonTwoLineText
                        TreeHelper.InvalidateMeasureForVisualAncestorPath<RibbonTwoLineText>(_textBlock2);
                    }
                    else
                    {
                        // Text is small enough to be displayed in first line alone
                        _textBlock1.Text = this.Text;
                        _textBlock1.Visibility = Visibility.Visible;

                        // Collapse second line
                        _textBlock2.Text = null;
                        _textBlock2.Visibility = Visibility.Collapsed;
                    }
                }
            }
            return base.MeasureOverride(availableSize);
        }
        
        /// <summary>
        /// Layout Text into two lines when width is restricted and determine if a LineBreak occurs.
        /// </summary>
        /// <param name="availableSize">constraint</param>
        /// <param name="secondLinePointer">TextPointer to where a LineBreak occurs</param>
        private void MeasureWithConstraint(Size availableSize, out TextPointer secondLinePointer)
        {
            Debug.Assert(_textBlock1 != null && _textBlock2 != null);

            // Temporarily assign _textBlock1.Text to measure the length it would require
            _textBlock1.Text = this.Text;
            _textBlock1.TextWrapping = TextWrapping.WrapWithOverflow;
            _textBlock1.Visibility = Visibility.Visible;

            secondLinePointer = null;

            _textBlock1.Measure(availableSize);

            // Dev11 Bug 65031 - Text: Invariant Assert hit in TextBlock due to loss of precision error
            // Under very rare cases, a loss of precision due to the UseLayoutRounding feature may
            // occur on 64-bit systems that can mean the difference between rounding and not.
            //
            // To work around this, we augment up _textBlock1's DesiredSize.Width by 0.5 pixels
            // before arranging it.  This is safe, because this measure/arrange pass justs determines
            // what should go on the second line of the TwoLineText and is not the actual measure/arrange.
            Size bufferedSize = new Size(_textBlock1.DesiredSize.Width + 0.5,
                                         _textBlock1.DesiredSize.Height);
            _textBlock1.Arrange(new Rect(bufferedSize));

            secondLinePointer = _textBlock1.ContentStart.GetLineStartPosition(1);

            // Clear local value
            _textBlock1.ClearValue(TextBlock.TextWrappingProperty);
        }

        /// <summary>
        /// Layout Text into two lines without any constraint and determine if a LineBreak occurs.
        /// </summary>
        /// <param name="secondLinePointer">TextPointer to where a LineBreak occurs</param>
        private void MeasureWithoutConstraint(out TextPointer secondLinePointer)
        {
            Debug.Assert(_textBlock1 != null && _textBlock2 != null);

            secondLinePointer = null;

            // Temporarily assign _textBlock1.Text to measure the length it would require
            _textBlock1.Text = this.Text;
            _textBlock1.TextWrapping = TextWrapping.WrapWithOverflow;
            _textBlock1.Visibility = Visibility.Visible;
            
            // This measures the TextBlock with infinite bounds to see what its ideal
            // (one-line) Width would be.  If this width is greater than the minimum
            // width for the label, we try halving its site to
            // spread the label to two lines.
            Size infinity = new Size(double.PositiveInfinity, double.PositiveInfinity);
            _textBlock1.Measure(infinity);
            
            double width = _textBlock1.DesiredSize.Width;
            int wordCount = string.IsNullOrEmpty(Text) ? 0 :  Text.Split().Length;

            if ( wordCount > 1)
            {
                // Set the TextBlock's width to half of its desired with to spread it
                // to two lines, and re-run layout.
                _textBlock1.Width = Math.Max(1.0, width / 2.0);
                _textBlock1.Measure(infinity);
                _textBlock1.Arrange(new Rect(_textBlock1.DesiredSize));

                // If text flows into more than 2 lines
                TextPointer currentLinePointer = _textBlock1.ContentStart.GetLineStartPosition(2);
                if (currentLinePointer != null)
                {
                    double extraLength = 0.0;
                    Rect lastCharacter, firstCharacter;

                    // Iterate through 3rd to (n-1)th line and add up their lengths.
                    TextPointer nextLinePointer = currentLinePointer.GetLineStartPosition(1);
                    while (nextLinePointer != null)
                    {
                        lastCharacter = nextLinePointer.GetCharacterRect(LogicalDirection.Backward);
                        firstCharacter = currentLinePointer.GetCharacterRect(LogicalDirection.Forward);
                        extraLength += Math.Abs(lastCharacter.X - firstCharacter.X);
                        currentLinePointer = nextLinePointer;
                        nextLinePointer = nextLinePointer.GetLineStartPosition(1);
                    }

                    // Add length of last line
                    lastCharacter = _textBlock1.ContentEnd.GetCharacterRect(LogicalDirection.Backward);
                    firstCharacter = currentLinePointer.GetCharacterRect(LogicalDirection.Forward);
                    extraLength += Math.Abs(lastCharacter.X - firstCharacter.X);

                    // Redistribute the extraLength among first two lines
                    _textBlock1.Width = _textBlock1.Width + extraLength / 2;
                    _textBlock1.Measure(infinity);
                    _textBlock1.Arrange(new Rect(_textBlock1.DesiredSize));
                }
                
                secondLinePointer = _textBlock1.ContentStart.GetLineStartPosition(1);
                
                // Clear the temporarily assigned values
                _textBlock1.ClearValue(TextBlock.WidthProperty);
            }

            _textBlock1.ClearValue(TextBlock.TextWrappingProperty);
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new RibbonTwoLineTextAutomationPeer(this);
        }

        #endregion
    }
}
