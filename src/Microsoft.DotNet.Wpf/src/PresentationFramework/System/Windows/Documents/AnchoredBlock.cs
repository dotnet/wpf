// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: AnchoredBlock element. 
//    AnchoredBlock element - an abstract class used as a base for
//    TextElements incorporated (anchored) into text flow on inline level,
//    but appearing visually in separate block.
//    Derived concrete classes are: Floater and Figure.
//

using System.ComponentModel;        // TypeConverter
using System.Windows.Media;         // Brush
using System.Windows.Markup; // ContentProperty
using MS.Internal;

namespace System.Windows.Documents
{
    /// <summary>
    /// AnchoredBlock element - an abstract class used as a base for
    /// TextElements incorporated (anchored) into text flow on inline level,
    /// but appearing visually in separate block.
    /// Derived concrete classes are: Flowter and Figure.
    /// </summary>
    [ContentProperty("Blocks")]
    public abstract class AnchoredBlock : Inline
    {
        //-------------------------------------------------------------------
        //
        // Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Creates a new AnchoredBlock instance.
        /// </summary>
        /// <param name="block">
        /// Optional child of the new AnchoredBlock, may be null.
        /// </param>
        /// <param name="insertionPosition">
        /// Optional position at which to insert the new AnchoredBlock. May
        /// be null.
        /// </param>
        protected AnchoredBlock(Block block, TextPointer insertionPosition)
        {
            if (insertionPosition != null)
            {
                insertionPosition.TextContainer.BeginChange();
            }
            try
            {
                if (insertionPosition != null)
                {
                    // This will throw InvalidOperationException if schema validity is violated.
                    insertionPosition.InsertInline(this);
                }

                if (block != null)
                {
                    this.Blocks.Add(block);
                }
            }
            finally
            {
                if (insertionPosition != null)
                {
                    insertionPosition.TextContainer.EndChange();
                }
            }
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        // Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <value>
        /// Collection of Blocks contained in this element
        /// </value>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public BlockCollection Blocks
        {
            get
            {
                return new BlockCollection(this, /*isOwnerParent*/true);
            }
        }

        /// <summary>
        /// DependencyProperty for <see cref="Margin" /> property.
        /// </summary>
        public static readonly DependencyProperty MarginProperty =
                Block.MarginProperty.AddOwner(
                        typeof(AnchoredBlock),
                        new FrameworkPropertyMetadata(
                                new Thickness(Double.NaN),
                                FrameworkPropertyMetadataOptions.AffectsMeasure)); 

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
                Block.PaddingProperty.AddOwner(
                        typeof(AnchoredBlock), 
                        new FrameworkPropertyMetadata(
                                new Thickness(Double.NaN),
                                FrameworkPropertyMetadataOptions.AffectsMeasure));

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
                Block.BorderThicknessProperty.AddOwner(
                        typeof(AnchoredBlock),
                        new FrameworkPropertyMetadata(
                            new Thickness(), 
                            FrameworkPropertyMetadataOptions.AffectsMeasure));

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
                Block.BorderBrushProperty.AddOwner(
                        typeof(AnchoredBlock),
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
                Block.TextAlignmentProperty.AddOwner(typeof(AnchoredBlock));

        /// <summary>
        /// 
        /// </summary>
        public TextAlignment TextAlignment
        {
            get { return (TextAlignment)GetValue(TextAlignmentProperty); }
            set { SetValue(TextAlignmentProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="LineHeight" /> property.
        /// </summary>
        public static readonly DependencyProperty LineHeightProperty = 
                Block.LineHeightProperty.AddOwner(typeof(AnchoredBlock));

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
        /// DependencyProperty for <see cref="LineStackingStrategy" /> property.
        /// </summary>
        public static readonly DependencyProperty LineStackingStrategyProperty =
                Block.LineStackingStrategyProperty.AddOwner(typeof(AnchoredBlock));

        /// <summary>
        /// The LineStackingStrategy property specifies how lines are placed
        /// </summary>
        public LineStackingStrategy LineStackingStrategy
        {
            get { return (LineStackingStrategy)GetValue(LineStackingStrategyProperty); }
            set { SetValue(LineStackingStrategyProperty, value); }
        }

        #endregion Public Proterties

        #region Internal Methods

        /// <summary>
        /// This method is used by TypeDescriptor to determine if this property should
        /// be serialized.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeBlocks(XamlDesignerSerializationManager manager)
        {
            return manager != null && manager.XmlWriter == null;
        }
        
        #endregion

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
    }
}
