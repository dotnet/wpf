// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: ListItem acts by default like a Paragraph, but with 
//              different margins/padding and features to support markers 
//              such as bullets and numbering. 
//

using MS.Internal;                  // Invariant
using System.Windows.Markup; // ContentProperty
using System.ComponentModel;        // TypeConverter
using System.Windows.Media;         // Brush

namespace System.Windows.Documents 
{
    /// <summary>
    /// ListItem acts by default like a Paragraph, but with different 
    /// margins/padding and features to support markers such as bullets and 
    /// numbering.
    /// </summary>
    [ContentProperty("Blocks")]
    public class ListItem : TextElement
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Initializes a new instance of a ListItem element.
        /// </summary>
        public ListItem() 
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of a ListItem element specifying a Paragraph element as its initial child.
        /// </summary>
        /// <param name="paragraph">
        /// Paragraph added as a single child of a ListItem
        /// </param>
        public ListItem(Paragraph paragraph)
            : base()
        {
            if (paragraph == null)
            {
                throw new ArgumentNullException("paragraph");
            }

            this.Blocks.Add(paragraph);
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        // Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <value>
        /// Parent element as IBlockItemParent through which a Children collection
        /// containing this BlockItem is available for Adding/Removing purposes.
        /// </value>
        public List List
        {
            get
            {
                return (this.Parent as List);
            }
        }

        /// <value>
        /// Collection of BlockItems contained in this ListItem.
        /// Usually this collection contains only one Paragraph element.
        /// In case of nested lists it can contain List element as a second
        /// item of the collection.
        /// More Paragraphs can be added to a collection as well.
        /// </value>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public BlockCollection Blocks
        {
            get
            {
                return  new BlockCollection(this, /*isOwnerParent*/true);
            }
        }

        /// <value>
        /// A collection of ListItems containing this one in its sequential tree.
        /// May return null if an element is not inserted into any tree.
        /// </value>
        public ListItemCollection SiblingListItems
        {
            get
            {
                if (this.Parent == null)
                {
                    return null;
                }

                return new ListItemCollection(this, /*isOwnerParent*/false);
            }
        }

        /// <summary>
        /// Returns a ListItem immediately following this one
        /// </summary>
        public ListItem NextListItem
        {
            get
            {
                return this.NextElement as ListItem;
            }
        }

        /// <summary>
        /// Returns a block immediately preceding this one
        /// on the same level of siblings
        /// </summary>
        public ListItem PreviousListItem
        {
            get
            {
                return this.PreviousElement as ListItem;
            }
        }

        /// <summary>
        /// DependencyProperty for <see cref="Margin" /> property.
        /// </summary>
        public static readonly DependencyProperty MarginProperty =
                Block.MarginProperty.AddOwner(
                        typeof(ListItem),
                        new FrameworkPropertyMetadata(
                                new Thickness(),
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
                        typeof(ListItem),
                        new FrameworkPropertyMetadata(
                                new Thickness(), 
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
                        typeof(ListItem),
                        new FrameworkPropertyMetadata(
                                new Thickness(), 
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

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
                        typeof(ListItem),
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
                Block.TextAlignmentProperty.AddOwner(typeof(ListItem));

        /// <summary>
        /// 
        /// </summary>
        public TextAlignment TextAlignment
        {
            get { return (TextAlignment)GetValue(TextAlignmentProperty); }
            set { SetValue(TextAlignmentProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="FlowDirection" /> property.
        /// </summary>
        public static readonly DependencyProperty FlowDirectionProperty = 
                Block.FlowDirectionProperty.AddOwner(typeof(ListItem));

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
                Block.LineHeightProperty.AddOwner(typeof(ListItem));

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
                Block.LineStackingStrategyProperty.AddOwner(typeof(ListItem));

        /// <summary>
        /// The LineStackingStrategy property specifies how lines are placed
        /// </summary>
        public LineStackingStrategy LineStackingStrategy
        {
            get { return (LineStackingStrategy)GetValue(LineStackingStrategyProperty); }
            set { SetValue(LineStackingStrategyProperty, value); }
        }

        #endregion Public Properties
        
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
