// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: Implements a List element, a container for ListElementItems: '
//              block elements designed to be formatted with markers such as 
//              bullets and numbering. 
//

using System.ComponentModel;
using System.Windows.Markup;
using MS.Internal;
using MS.Internal.PtsHost.UnsafeNativeMethods;      // PTS restrictions

namespace System.Windows.Documents 
{
    /// <summary>
    /// Implements a List element, a container for ListItems: block 
    /// elements designed to be formatted with markers such as bullets and 
    /// numbering.
    /// </summary>
    [ContentProperty("ListItems")]
    public class List : Block
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// List static constructor. Registers metadata for its properties.
        /// </summary>
        static List()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(List), new FrameworkPropertyMetadata(typeof(List)));
        }

        /// <summary>
        /// Initializes a new instance of a List class.
        /// </summary>
        public List() 
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of a List class specifying its first ListItem child.
        /// </summary>
        /// <param name="listItem">
        /// ListItem to be inserted as a first child of this List.
        /// </param>
        public List(ListItem listItem)
            : base()
        {
            if (listItem == null)
            {
                throw new ArgumentNullException("listItem");
            }
            this.ListItems.Add(listItem);
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <value>
        /// Collection of ListItems contained in this List.
        /// </value>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public ListItemCollection ListItems
        {
            get
            {
                return new ListItemCollection(this, /*isOwnerParent*/true);
            }
        }

        /// <summary>
        /// DependencyProperty for <see cref="MarkerStyle" /> property.
        /// </summary>
        public static readonly DependencyProperty MarkerStyleProperty = 
                DependencyProperty.Register(
                        "MarkerStyle", 
                        typeof(TextMarkerStyle), 
                        typeof(List), 
                        new FrameworkPropertyMetadata(
                                TextMarkerStyle.Disc, 
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender), 
                        new ValidateValueCallback(IsValidMarkerStyle));

        /// <summary>
        /// Type of bullet or number to be used by default with ListElementItems 
        /// contained by this List
        /// </summary>
        public TextMarkerStyle MarkerStyle
        {
            get { return (TextMarkerStyle)GetValue(MarkerStyleProperty); }
            set { SetValue(MarkerStyleProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="MarkerOffset" /> property.
        /// </summary>
        public static readonly DependencyProperty MarkerOffsetProperty = 
                DependencyProperty.Register(
                        "MarkerOffset", 
                        typeof(double), 
                        typeof(List),
                        new FrameworkPropertyMetadata(
                                Double.NaN, 
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender),
                                new ValidateValueCallback(IsValidMarkerOffset));

        /// <summary>
        /// Desired distance between each contained ListItem's content and 
        /// near edge of the associated marker.
        /// </summary>
        [TypeConverter(typeof(LengthConverter))]
        public double MarkerOffset
        {
            get { return (double)GetValue(MarkerOffsetProperty); }
            set { SetValue(MarkerOffsetProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="StartIndex" /> property.
        /// </summary>
        public static readonly DependencyProperty StartIndexProperty = 
                DependencyProperty.Register(
                        "StartIndex", 
                        typeof(int), 
                        typeof(List), 
                        new FrameworkPropertyMetadata(
                                1, 
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender), 
                        new ValidateValueCallback(IsValidStartIndex));

        /// <summary>
        /// Item index of the first ListItem that is immediate child of 
        /// this List.
        /// </summary>
        public int StartIndex
        {
            get { return (int)GetValue(StartIndexProperty); }
            set { SetValue(StartIndexProperty, value); }
        }

        #endregion Public Properties

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Returns the integer "index" of a specified ListItem that is an immediate child of 
        /// this List. This index is defined to be a sequential counter of ListElementItems only 
        /// (skipping other elements) among this List's immediate children. 
        /// 
        /// The list item index of the first child of type ListItem is specified by 
        /// this.StartListIndex, which has a default value of 1. 
        /// 
        /// The index returned by this method is used in the formation of some ListItem 
        /// markers such as "(b)" and "viii." (as opposed to others, like disks and wedges, 
        /// which are not sequential-position-dependent). 
        /// </summary>
        /// <param name="item">The item whose index is to be returned.</param>
        /// <returns>Returns the index of a specified ListItem.</returns>
        internal int GetListItemIndex(ListItem item)
        {
            // Check for valid arg
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            if (item.Parent != this)
            {
                throw new InvalidOperationException(SR.Get(SRID.ListElementItemNotAChildOfList));
            }

            // Count ListItem siblings (not other element types) back to first item.
            int itemIndex = StartIndex;
            TextPointer textNav = new TextPointer(this.ContentStart);
            while (textNav.CompareTo(this.ContentEnd) != 0)
            {
                // ListItem is a content element, so look for ElementStart runs only
                if (textNav.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementStart)
                {
                    DependencyObject element = textNav.GetAdjacentElementFromOuterPosition(LogicalDirection.Forward);
                    if (element is ListItem)
                    {
                        if (element == item)
                        {
                            break;
                        }
                        if (itemIndex < int.MaxValue)
                        {
                            ++itemIndex;
                        }
                    }
                    // Skip entire content element content, because we are looking
                    // only for immediate children.
                    textNav.MoveToPosition(((TextElement)element).ElementEnd);
                }
                else
                {
                    textNav.MoveToNextContextPosition(LogicalDirection.Forward);
                }
            }
            return itemIndex;
        }

        /// <summary>
        /// Inserts a List around a sequence of Blocks
        /// starting from firstBlock ending with lastBlock.
        /// the List must be empty and not inserted in a tree
        /// before the operation
        /// </summary>
        /// <param name="firstBlock"></param>
        /// <param name="lastBlock"></param>
        internal void Apply(Block firstBlock, Block lastBlock)
        {
            Invariant.Assert(this.Parent == null, "Cannot Apply List Because It Is Inserted In The Tree Already.");
            Invariant.Assert(this.IsEmpty, "Cannot Apply List Because It Is Not Empty.");
            Invariant.Assert(firstBlock.Parent == lastBlock.Parent, "Cannot Apply List Because Block Are Not Siblings.");

            TextContainer textContainer = this.TextContainer;

            textContainer.BeginChange();
            try
            {
                // Wrap all block items into this List element
                this.Reposition(firstBlock.ElementStart, lastBlock.ElementEnd);

                // Add ListItem elements
                Block block = firstBlock;
                while (block != null)
                {
                    ListItem listItem;
                    if (block is List)
                    {
                        // To wrap List into list item we pull it into previous ListItem (if any) as sublist
                        listItem = block.ElementStart.GetAdjacentElement(LogicalDirection.Backward) as ListItem;
                        if (listItem != null)
                        {
                            // Wrap the List into preceding ListItem
                            listItem.Reposition(listItem.ContentStart, block.ElementEnd);
                        }
                        else
                        {
                            // No preceding ListItem. Create new one
                            listItem = new ListItem();
                            listItem.Reposition(block.ElementStart, block.ElementEnd);
                        }
                    }
                    else
                    {
                        // To wrap paragraph into list item we need to create a new one
                        //  Decide what to do with other blocks: Table, Section
                        listItem = new ListItem();
                        listItem.Reposition(block.ElementStart, block.ElementEnd);

                        // MS Word-like heuristic: clear margin from a paragraph before wrapping it into a list item
                        // Note: using TextContainer to make sure that undo unit is created.
                        block.ClearValue(Block.MarginProperty);
                        block.ClearValue(Block.PaddingProperty);
                        block.ClearValue(Paragraph.TextIndentProperty);
                    }

                    // Stop when the last paragraph is covered
                    block = block == lastBlock ? null : (Block)listItem.ElementEnd.GetAdjacentElement(LogicalDirection.Forward);
                }

                // We need to set appropriate FlowDirection property on the new List and its paragraph children. 
                // We take the FlowDirection value from the first paragraph's FlowDirection value.

                TextRangeEdit.SetParagraphProperty(this.ElementStart, this.ElementEnd,
                    Paragraph.FlowDirectionProperty, firstBlock.GetValue(Paragraph.FlowDirectionProperty));
            }
            finally
            {
                textContainer.EndChange();
            }
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        // Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        private static bool IsValidMarkerStyle(object o)
        {
            TextMarkerStyle value = (TextMarkerStyle)o;
            return value == TextMarkerStyle.None
                || value == TextMarkerStyle.Disc
                || value == TextMarkerStyle.Circle
                || value == TextMarkerStyle.Square
                || value == TextMarkerStyle.Box
                || value == TextMarkerStyle.LowerRoman
                || value == TextMarkerStyle.UpperRoman
                || value == TextMarkerStyle.LowerLatin
                || value == TextMarkerStyle.UpperLatin
                || value == TextMarkerStyle.Decimal;
        }

        private static bool IsValidStartIndex(object o)
        {
            int value = (int)o;
            return (value > 0);
        }

        private static bool IsValidMarkerOffset(object o)
        {
            double value = (double)o;
            double maxOffset = Math.Min(1000000, PTS.MaxPageSize);
            double minOffset = -maxOffset;

            if (Double.IsNaN(value))
            {
                // Default
                return true;
            }
            if (value < minOffset || value > maxOffset)
            {
                return false;
            }
            return true;
        }

        #endregion Private Methods
    }
}
