// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Figure element. 
//

using System.ComponentModel;        // TypeConverter
using System.Windows.Controls;      // TextBlock
using MS.Internal;
using MS.Internal.PtsHost.UnsafeNativeMethods; // PTS restrictions

namespace System.Windows.Documents
{
    /// <summary>
    /// Figure element.
    /// </summary>
    public class Figure : AnchoredBlock
    {
        //-------------------------------------------------------------------
        //
        // Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Initialized the new instance of a Figure
        /// </summary>
        public Figure() : this(null)
        {
        }

        /// <summary>
        /// Initialized the new instance of a Figure specifying a Block added
        /// to a Figure as its first child.
        /// </summary>
        /// <param name="childBlock">
        /// Block added as a first initial child of the Figure.
        /// </param>
        public Figure(Block childBlock) : this(childBlock, null)
        {
        }

        /// <summary>
        /// Creates a new Figure instance.
        /// </summary>
        /// <param name="childBlock">
        /// Optional child of the new Figure, may be null.
        /// </param>
        /// <param name="insertionPosition">
        /// Optional position at which to insert the new Figure. May
        /// be null.
        /// </param>
        public Figure(Block childBlock, TextPointer insertionPosition) : base(childBlock, insertionPosition)
        {
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        // Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// DependencyProperty for <see cref="HorizontalAnchor" /> property.
        /// </summary>
        public static readonly DependencyProperty HorizontalAnchorProperty = 
                DependencyProperty.Register(
                        "HorizontalAnchor", 
                        typeof(FigureHorizontalAnchor), 
                        typeof(Figure), 
                        new FrameworkPropertyMetadata(
                                FigureHorizontalAnchor.ColumnRight, 
                                FrameworkPropertyMetadataOptions.AffectsParentMeasure), 
                        new ValidateValueCallback(IsValidHorizontalAnchor));

        /// <summary>
        /// 
        /// </summary>
        public FigureHorizontalAnchor HorizontalAnchor
        {
            get { return (FigureHorizontalAnchor)GetValue(HorizontalAnchorProperty); }
            set { SetValue(HorizontalAnchorProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="VerticalAnchor" /> property.
        /// </summary>
        public static readonly DependencyProperty VerticalAnchorProperty = 
                DependencyProperty.Register(
                        "VerticalAnchor", 
                        typeof(FigureVerticalAnchor), 
                        typeof(Figure), 
                        new FrameworkPropertyMetadata(
                                FigureVerticalAnchor.ParagraphTop, 
                                FrameworkPropertyMetadataOptions.AffectsParentMeasure), 
                        new ValidateValueCallback(IsValidVerticalAnchor));

        /// <summary>
        /// 
        /// </summary>
        public FigureVerticalAnchor VerticalAnchor
        {
            get { return (FigureVerticalAnchor)GetValue(VerticalAnchorProperty); }
            set { SetValue(VerticalAnchorProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="HorizontalOffset" /> property.
        /// </summary>
        public static readonly DependencyProperty HorizontalOffsetProperty = 
                DependencyProperty.Register(
                        "HorizontalOffset", 
                        typeof(double), 
                        typeof(Figure), 
                        new FrameworkPropertyMetadata(
                                0.0, 
                                FrameworkPropertyMetadataOptions.AffectsParentMeasure),
                        new ValidateValueCallback(IsValidOffset));

        /// <summary>
        /// 
        /// </summary>
        [TypeConverter(typeof(LengthConverter))]
        public double HorizontalOffset
        {
            get { return (double)GetValue(HorizontalOffsetProperty); }
            set { SetValue(HorizontalOffsetProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="VerticalOffset" /> property.
        /// </summary>
        public static readonly DependencyProperty VerticalOffsetProperty = 
                DependencyProperty.Register(
                        "VerticalOffset", 
                        typeof(double), 
                        typeof(Figure), 
                        new FrameworkPropertyMetadata(
                                0.0, 
                                FrameworkPropertyMetadataOptions.AffectsParentMeasure),
                        new ValidateValueCallback(IsValidOffset));

        /// <summary>
        /// 
        /// </summary>
        [TypeConverter(typeof(LengthConverter))]
        public double VerticalOffset
        {
            get { return (double)GetValue(VerticalOffsetProperty); }
            set { SetValue(VerticalOffsetProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="CanDelayPlacement" /> property.
        /// </summary>
        public static readonly DependencyProperty CanDelayPlacementProperty = 
                DependencyProperty.Register(
                        "CanDelayPlacement", 
                        typeof(bool), 
                        typeof(Figure), 
                        new FrameworkPropertyMetadata(
                                true, 
                                FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        /// <summary>
        /// 
        /// </summary>
        public bool CanDelayPlacement
        {
            get { return (bool)GetValue(CanDelayPlacementProperty); }
            set { SetValue(CanDelayPlacementProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="WrapDirection" /> property.
        /// </summary>
        public static readonly DependencyProperty WrapDirectionProperty = 
                DependencyProperty.Register(
                        "WrapDirection", 
                        typeof(WrapDirection), 
                        typeof(Figure), 
                        new FrameworkPropertyMetadata(
                                WrapDirection.Both, 
                                FrameworkPropertyMetadataOptions.AffectsParentMeasure), 
                        new ValidateValueCallback(IsValidWrapDirection));

        /// <summary>
        /// 
        /// </summary>
        public WrapDirection WrapDirection
        {
            get { return (WrapDirection)GetValue(WrapDirectionProperty); }
            set { SetValue(WrapDirectionProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="Width" /> property.
        /// </summary>
        public static readonly DependencyProperty WidthProperty = 
                DependencyProperty.Register(
                        "Width", 
                        typeof(FigureLength), 
                        typeof(Figure),
                        new FrameworkPropertyMetadata(
                                new FigureLength(1.0, FigureUnitType.Auto),
                                FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// The Width property specifies the width of the element.
        /// </summary>
        public FigureLength Width
        {
            get { return (FigureLength)GetValue(WidthProperty); }
            set { SetValue(WidthProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="Height" /> property.
        /// </summary>
        public static readonly DependencyProperty HeightProperty = 
                DependencyProperty.Register(
                        "Height", 
                        typeof(FigureLength), 
                        typeof(Figure),
                        new FrameworkPropertyMetadata(
                                new FigureLength(1.0, FigureUnitType.Auto),
                                FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// The Height property specifies the height of the element.
        /// </summary>
        public FigureLength Height
        {
            get { return (FigureLength)GetValue(HeightProperty); }
            set { SetValue(HeightProperty, value); }
        }

        #endregion Public Properties

        //-------------------------------------------------------------------
        //
        // Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        private static bool IsValidHorizontalAnchor(object o)
        {
            FigureHorizontalAnchor value = (FigureHorizontalAnchor)o;
            return value == FigureHorizontalAnchor.ContentCenter
                || value == FigureHorizontalAnchor.ContentLeft
                || value == FigureHorizontalAnchor.ContentRight
                || value == FigureHorizontalAnchor.PageCenter
                || value == FigureHorizontalAnchor.PageLeft
                || value == FigureHorizontalAnchor.PageRight
                || value == FigureHorizontalAnchor.ColumnCenter
                || value == FigureHorizontalAnchor.ColumnLeft
                || value == FigureHorizontalAnchor.ColumnRight;
                // || value == FigureHorizontalAnchor.CharacterCenter
                // || value == FigureHorizontalAnchor.CharacterLeft
                // || value == FigureHorizontalAnchor.CharacterRight;
        }

        private static bool IsValidVerticalAnchor(object o)
        {
            FigureVerticalAnchor value = (FigureVerticalAnchor)o;
            return value == FigureVerticalAnchor.ContentBottom
                || value == FigureVerticalAnchor.ContentCenter
                || value == FigureVerticalAnchor.ContentTop
                || value == FigureVerticalAnchor.PageBottom
                || value == FigureVerticalAnchor.PageCenter
                || value == FigureVerticalAnchor.PageTop
                || value == FigureVerticalAnchor.ParagraphTop;
                // || value == FigureVerticalAnchor.CharacterBottom
                // || value == FigureVerticalAnchor.CharacterCenter
                // || value == FigureVerticalAnchor.CharacterTop;
        }

        private static bool IsValidWrapDirection(object o)
        {
            WrapDirection value = (WrapDirection)o;
            return value == WrapDirection.Both
                || value == WrapDirection.None
                || value == WrapDirection.Left
                || value == WrapDirection.Right;
        }

        private static bool IsValidOffset(object o)
        {
            double offset = (double)o;
            double maxOffset = Math.Min(1000000, PTS.MaxPageSize);
            double minOffset = -maxOffset;
            if (Double.IsNaN(offset))
            {
                return false;
            }
            if (offset < minOffset || offset > maxOffset)
            {
                return false;
            }
            return true;
        }

        #endregion Private Methods
    }
}
