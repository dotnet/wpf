// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: Block element. 
//    Block element - an abstract class, a base for elements allowed 
//    as siblings of Paragraphs.
//

using System.ComponentModel;    // TypeConverter
using System.Windows.Controls;  // Border
using System.Windows.Media;     // Brush
using MS.Internal;              // DoubleUtil
using MS.Internal.Text;         // Text DPI restrictions
using MS.Internal.PtsHost.UnsafeNativeMethods; // PTS restrictions

namespace System.Windows.Documents 
{
    /// <summary>
    /// Block element - an abstract class, a base for elements allowed 
    /// as siblings of Paragraphs.
    /// Only Blocks are allowed on top level of FlowDocument
    /// and other structural text elements like Section, ListItem, TableCell.
    /// </summary>
    public abstract class Block : TextElement
    {
        //-------------------------------------------------------------------
        //
        // Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <value>
        /// A collection of Blocks containing this Block in its sequential tree.
        /// May return null if this Block is not inserted into any tree.
        /// </value>
        public BlockCollection SiblingBlocks
        {
            get
            {
                if (this.Parent == null)
                {
                    return null;
                }

                return new BlockCollection(this, /*isOwnerParent*/false);
            }
        }

        /// <summary>
        /// Returns a Block immediately following this one
        /// on the same level of siblings
        /// </summary>
        public Block NextBlock
        {
            get
            {
                return this.NextElement as Block;
            }
        }

        /// <summary>
        /// Returns a block immediately preceding this one
        /// on the same level of siblings
        /// </summary>
        public Block PreviousBlock
        {
            get
            {
                return this.PreviousElement as Block;
            }
        }

        /// <summary>
        /// DependencyProperty for hyphenation property.
        /// </summary>
        public static readonly DependencyProperty IsHyphenationEnabledProperty = 
                DependencyProperty.RegisterAttached(
                        "IsHyphenationEnabled", 
                        typeof(bool), 
                        typeof(Block), 
                        new FrameworkPropertyMetadata(
                                false, 
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// CLR property for hyphenation
        /// </summary>
        public bool IsHyphenationEnabled
        {
            get { return (bool)GetValue(IsHyphenationEnabledProperty); }
            set { SetValue(IsHyphenationEnabledProperty, value); }
        }

        /// <summary>
        /// DependencyProperty setter for <see cref="IsHyphenationEnabled" /> property.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        public static void SetIsHyphenationEnabled(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(IsHyphenationEnabledProperty, value);
        }

        /// <summary>
        /// DependencyProperty getter for <see cref="IsHyphenationEnabled" /> property.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        public static bool GetIsHyphenationEnabled(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (bool)element.GetValue(IsHyphenationEnabledProperty);
        }

        /// <summary>
        /// DependencyProperty for <see cref="Margin" /> property.
        /// </summary>
        public static readonly DependencyProperty MarginProperty = 
                DependencyProperty.Register(
                        "Margin", 
                        typeof(Thickness), 
                        typeof(Block), 
                        new FrameworkPropertyMetadata(
                                new Thickness(), 
                                FrameworkPropertyMetadataOptions.AffectsMeasure), 
                        new ValidateValueCallback(IsValidMargin));

        /// <summary>
        /// The Margin property specifies the margin of the element.
        /// </summary>
        public Thickness Margin
        {
            get { return (Thickness)GetValue(MarginProperty); }
            set { SetValue(MarginProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="Padding" /> property.
        /// </summary>
        public static readonly DependencyProperty PaddingProperty = 
                DependencyProperty.Register(
                        "Padding", 
                        typeof(Thickness), 
                        typeof(Block), 
                        new FrameworkPropertyMetadata(
                                new Thickness(), 
                                FrameworkPropertyMetadataOptions.AffectsMeasure), 
                        new ValidateValueCallback(IsValidPadding));

        /// <summary>
        /// The Padding property specifies the padding of the element.
        /// </summary>
        public Thickness Padding
        {
            get { return (Thickness)GetValue(PaddingProperty); }
            set { SetValue(PaddingProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="BorderThickness" /> property.
        /// </summary>
        public static readonly DependencyProperty BorderThicknessProperty = 
                DependencyProperty.Register(
                        "BorderThickness", 
                        typeof(Thickness), 
                        typeof(Block), 
                        new FrameworkPropertyMetadata(
                                new Thickness(), 
                                FrameworkPropertyMetadataOptions.AffectsMeasure), 
                        new ValidateValueCallback(IsValidBorderThickness));

        /// <summary>
        /// The BorderThickness property specifies the border of the element.
        /// </summary>
        public Thickness BorderThickness
        {
            get { return (Thickness)GetValue(BorderThicknessProperty); }
            set { SetValue(BorderThicknessProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="BorderBrush" /> property.
        /// </summary>
        public static readonly DependencyProperty BorderBrushProperty = 
                DependencyProperty.Register(
                        "BorderBrush", 
                        typeof(Brush), 
                        typeof(Block),
                        new FrameworkPropertyMetadata(
                                null,
                                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// The BorderBrush property specifies the brush of the border.
        /// </summary>
        public Brush BorderBrush
        {
            get { return (Brush)GetValue(BorderBrushProperty); }
            set { SetValue(BorderBrushProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="TextAlignment" /> property.
        /// </summary>
        public static readonly DependencyProperty TextAlignmentProperty = 
                DependencyProperty.RegisterAttached(
                        "TextAlignment", 
                        typeof(TextAlignment), 
                        typeof(Block), 
                        new FrameworkPropertyMetadata(
                                TextAlignment.Left, 
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits), 
                        new ValidateValueCallback(IsValidTextAlignment));

        /// <summary>
        /// 
        /// </summary>
        public TextAlignment TextAlignment
        {
            get { return (TextAlignment)GetValue(TextAlignmentProperty); }
            set { SetValue(TextAlignmentProperty, value); }
        }

        /// <summary>
        /// DependencyProperty setter for <see cref="TextAlignment" /> property.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        public static void SetTextAlignment(DependencyObject element, TextAlignment value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(TextAlignmentProperty, value);
        }

        /// <summary>
        /// DependencyProperty getter for <see cref="TextAlignment" /> property.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        public static TextAlignment GetTextAlignment(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (TextAlignment)element.GetValue(TextAlignmentProperty);
        }

        /// <summary>
        /// DependencyProperty for <see cref="FlowDirection" /> property.
        /// </summary>
        public static readonly DependencyProperty FlowDirectionProperty = 
                FrameworkElement.FlowDirectionProperty.AddOwner(typeof(Block));

        /// <summary>
        /// The FlowDirection property specifies the flow direction of the element.
        /// </summary>
        public FlowDirection FlowDirection
        {
            get { return (FlowDirection)GetValue(FlowDirectionProperty); }
            set { SetValue(FlowDirectionProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="LineHeight" /> property.
        /// </summary>
        public static readonly DependencyProperty LineHeightProperty = 
                DependencyProperty.RegisterAttached(
                        "LineHeight", 
                        typeof(double), 
                        typeof(Block), 
                        new FrameworkPropertyMetadata(
                                double.NaN, 
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits), 
                        new ValidateValueCallback(IsValidLineHeight));

        /// <summary>
        /// The LineHeight property specifies the height of each generated line box.
        /// </summary>
        [TypeConverter(typeof(LengthConverter))]
        public double LineHeight
        {
            get { return (double)GetValue(LineHeightProperty); }
            set { SetValue(LineHeightProperty, value); }
        }

        /// <summary>
        /// DependencyProperty setter for <see cref="LineHeight" /> property.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        public static void SetLineHeight(DependencyObject element, double value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(LineHeightProperty, value);
        }

        /// <summary>
        /// DependencyProperty getter for <see cref="LineHeight" /> property.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        [TypeConverter(typeof(LengthConverter))]
        public static double GetLineHeight(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (double)element.GetValue(LineHeightProperty);
        }

        /// <summary>
        /// DependencyProperty for <see cref="LineStackingStrategy" /> property.
        /// </summary>
        public static readonly DependencyProperty LineStackingStrategyProperty =
                DependencyProperty.RegisterAttached(
                        "LineStackingStrategy",
                        typeof(LineStackingStrategy),
                        typeof(Block),
                        new FrameworkPropertyMetadata(
                                LineStackingStrategy.MaxHeight,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits),
                        new ValidateValueCallback(IsValidLineStackingStrategy));

        /// <summary>
        /// The LineStackingStrategy property specifies how lines are placed
        /// </summary>
        public LineStackingStrategy LineStackingStrategy
        {
            get { return (LineStackingStrategy)GetValue(LineStackingStrategyProperty); }
            set { SetValue(LineStackingStrategyProperty, value); }
        }

        /// <summary>
        /// DependencyProperty setter for <see cref="LineStackingStrategy" /> property.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        public static void SetLineStackingStrategy(DependencyObject element, LineStackingStrategy value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(LineStackingStrategyProperty, value);
        }

        /// <summary>
        /// DependencyProperty getter for <see cref="LineStackingStrategy" /> property.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        public static LineStackingStrategy GetLineStackingStrategy(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (LineStackingStrategy)element.GetValue(LineStackingStrategyProperty);
        }

        /// <summary>
        /// Page break property, replaces PageBreak element. Indicates that a break should occur before this page.
        /// </summary>
        public static readonly DependencyProperty BreakPageBeforeProperty = 
                DependencyProperty.Register(
                        "BreakPageBefore", 
                        typeof(bool), 
                        typeof(Block), 
                        new FrameworkPropertyMetadata(
                                false, 
                                FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        /// <summary>
        /// The BreakPageBefore property indicates that a break should occur before this page
        /// </summary>
        public bool BreakPageBefore
        {
            get { return (bool)GetValue(BreakPageBeforeProperty); }
            set { SetValue(BreakPageBeforeProperty, value); }
        }

        /// <summary>
        /// Column break property, replaces ColumnBreak element. Indicates that a break should occur before this column.
        /// </summary>
        public static readonly DependencyProperty BreakColumnBeforeProperty = 
                DependencyProperty.Register(
                        "BreakColumnBefore", 
                        typeof(bool), 
                        typeof(Block), 
                        new FrameworkPropertyMetadata(
                                false, 
                                FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        /// <summary>
        /// The BreakColumnBefore property indicates that a break should occur before this column
        /// </summary>
        public bool BreakColumnBefore
        {
            get { return (bool)GetValue(BreakColumnBeforeProperty); }
            set { SetValue(BreakColumnBeforeProperty, value); }
        }

        /// <summary>
        /// ClearFloaters property, replaces FloaterClear element. Clears floater in specified WrapDirection 
        /// </summary>
        public static readonly DependencyProperty ClearFloatersProperty = 
                DependencyProperty.Register(
                        "ClearFloaters", 
                        typeof(WrapDirection), 
                        typeof(Block), 
                        new FrameworkPropertyMetadata(
                                WrapDirection.None, 
                                FrameworkPropertyMetadataOptions.AffectsParentMeasure), 
                        new ValidateValueCallback(IsValidWrapDirection));

        /// <summary>
        /// ClearFloaters property, replaces FloaterClear element. Clears floater in specified WrapDirection 
        /// </summary>
        public WrapDirection ClearFloaters
        {
            get { return (WrapDirection)GetValue(ClearFloatersProperty); }
            set { SetValue(ClearFloatersProperty, value); }
        }

        #endregion Public Properties

        //-------------------------------------------------------------------
        //
        // Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        internal static bool IsValidMargin(object o)
        {
            Thickness t = (Thickness)o;
            return IsValidThickness(t, /*allow NaN*/true);
        }

        internal static bool IsValidPadding(object o)
        {
            Thickness t = (Thickness)o;
            return IsValidThickness(t, /*allow NaN*/true);
        }

        internal static bool IsValidBorderThickness(object o)
        {
            Thickness t = (Thickness)o;
            return IsValidThickness(t, /*allow NaN*/false);
        }

        #endregion Internal Methods


        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Marks this element's left edge as visible to IMEs.
        /// This means element boundaries will act as word breaks.
        /// </summary>
        internal override bool IsIMEStructuralElement
        {
            get
            {
                return true;
            }
        }

        #endregion Internal Properties

        //-------------------------------------------------------------------
        //
        // Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        private static bool IsValidLineHeight(object o)
        {
            double lineHeight = (double)o;
            double minLineHeight = TextDpi.MinWidth;
            double maxLineHeight = Math.Min(1000000, PTS.MaxFontSize);

            if (Double.IsNaN(lineHeight))
            {
                return true;
            }
            if (lineHeight < minLineHeight)
            {
                return false;
            }
            if (lineHeight > maxLineHeight)
            {
                return false;
            }
            return true;
        }

        private static bool IsValidLineStackingStrategy(object o)
        {
            LineStackingStrategy value = (LineStackingStrategy)o;
            return (value == LineStackingStrategy.MaxHeight 
                    || value == LineStackingStrategy.BlockLineHeight);
        }

        private static bool IsValidTextAlignment(object o)
        {
            TextAlignment value = (TextAlignment)o;
            return value == TextAlignment.Center
                || value == TextAlignment.Justify
                || value == TextAlignment.Left
                || value == TextAlignment.Right;
        }

        private static bool IsValidWrapDirection(object o)
        {
            WrapDirection value = (WrapDirection)o;
            return value == WrapDirection.None
                || value == WrapDirection.Left
                || value == WrapDirection.Right
                || value == WrapDirection.Both;
        }

        internal static bool IsValidThickness(Thickness t, bool allowNaN)
        {
            double maxThickness = Math.Min(1000000, PTS.MaxPageSize);
            if (!allowNaN)
            {
                if (Double.IsNaN(t.Left) || Double.IsNaN(t.Right) || Double.IsNaN(t.Top) || Double.IsNaN(t.Bottom))
                {
                    return false;
                }
            }
            if (!Double.IsNaN(t.Left) && (t.Left < 0 || t.Left > maxThickness))
            {
                return false;
            }
            if (!Double.IsNaN(t.Right) && (t.Right < 0 || t.Right > maxThickness))
            {
                return false;
            }
            if (!Double.IsNaN(t.Top) && (t.Top < 0 || t.Top > maxThickness))
            {
                return false;
            }
            if (!Double.IsNaN(t.Bottom) && (t.Bottom < 0 || t.Bottom > maxThickness))
            {
                return false;
            }
            return true;
        }

        #endregion Private Methods
    }
}
