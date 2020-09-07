// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Implementation of table cell
//
//              See spec at WPP TableOM.doc
//
//
//  Future:
//  1)  OnPropertyInvalidated is expensive and requires investigation:
//      a)  Cell calls to base class implementation which fires back tree 
//          change event - Cell.OnTextContainerChange. As a result Cell.Invalidate
//          format is called twice. 
//      b)  base.OnPropertyInvalidated *always* creates DTR for the whole cell.
//          Why even AffectsRender causes it? 

using MS.Internal;
using MS.Internal.PtsHost;
using MS.Internal.PtsTable;
using MS.Internal.Text;
using MS.Utility;
using System.Diagnostics;
using System.Security;
using System.Windows.Threading;
using System.Collections;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Markup;
using System.ComponentModel; // TypeConverter
using System.Collections.Generic;
using MS.Internal.Documents;

using System;

using MS.Internal.PtsHost.UnsafeNativeMethods;

namespace System.Windows.Documents
{
    /// <summary>
    /// Implementation of table cell 
    /// </summary>
    [ContentProperty("Blocks")]
    public class TableCell : TextElement, IIndexedChild<TableRow>
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public TableCell()
            : base()
        {
            PrivateInitialize();
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TableCell(Block blockItem)
            : base()
        {
            PrivateInitialize();

            if (blockItem == null)
            {
                throw new ArgumentNullException("blockItem");
            }

            this.Blocks.Add(blockItem);
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods 

        /// <summary>
        /// Called when tablecell gets new parent
        /// </summary>
        /// <param name="newParent">
        /// New parent of cell
        /// </param>
        internal override void OnNewParent(DependencyObject newParent)
        {
            DependencyObject oldParent = this.Parent;
            TableRow newParentTR = newParent as TableRow;

            if((newParent != null) && (newParentTR == null))
            {
                throw new InvalidOperationException(SR.Get(SRID.TableInvalidParentNodeType, newParent.GetType().ToString()));
            }

            if(oldParent != null)
            {
                ((TableRow)oldParent).Cells.InternalRemove(this);
            } 

            base.OnNewParent(newParent);

            if ((newParentTR != null) && (newParentTR.Cells != null)) // keep PreSharp happy
            {
                newParentTR.Cells.InternalAdd(this);
            }
        }

        #endregion Public Methods 

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties 

        /// <value>
        /// Collection of Blocks contained in this Section.
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
        /// Column span property.
        /// </summary>
        public int ColumnSpan
        {
            get { return (int) GetValue(ColumnSpanProperty); }
            set { SetValue(ColumnSpanProperty, value); }
        }

        /// <summary>
        /// Row span property.
        /// </summary>
        public int RowSpan
        {
            get { return (int) GetValue(RowSpanProperty); }
            set { SetValue(RowSpanProperty, value); }
        }

        //--------------------------------------------------------------------
        //
        // Protected Methods
        //
        //---------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Creates AutomationPeer (<see cref="ContentElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new TableCellAutomationPeer(this);
        }

        #endregion Protected Methods

        //.....................................................................
        //
        // Block Properties
        //
        //.....................................................................

        #region Block Properties

        /// <summary>
        /// DependencyProperty for <see cref="Padding" /> property.
        /// </summary>
        public static readonly DependencyProperty PaddingProperty =
                Block.PaddingProperty.AddOwner(
                        typeof(TableCell),
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
                        typeof(TableCell),
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
                        typeof(TableCell),
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
                Block.TextAlignmentProperty.AddOwner(typeof(TableCell));

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
                Block.FlowDirectionProperty.AddOwner(typeof(TableCell));

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
                Block.LineHeightProperty.AddOwner(typeof(TableCell));

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
                Block.LineStackingStrategyProperty.AddOwner(typeof(TableCell));

        /// <summary>
        /// The LineStackingStrategy property specifies how lines are placed
        /// </summary>
        public LineStackingStrategy LineStackingStrategy
        {
            get { return (LineStackingStrategy)GetValue(LineStackingStrategyProperty); }
            set { SetValue(LineStackingStrategyProperty, value); }
        }

        #endregion Block Properties

        #endregion Public Properties 

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods 

        #region IIndexedChild implementation
        void IIndexedChild<TableRow>.OnEnterParentTree()
        {
            this.OnEnterParentTree();
        }
        void IIndexedChild<TableRow>.OnExitParentTree()
        {
            this.OnExitParentTree();
        }
        void IIndexedChild<TableRow>.OnAfterExitParentTree(TableRow parent)
        {
            this.OnAfterExitParentTree(parent);
        }
        int IIndexedChild<TableRow>.Index
        {
            get { return this.Index; }
            set { this.Index = value; }
        }
        #endregion IIndexedChild implementation

        /// <summary>
        /// Callback used to notify the Cell about entering model tree.
        /// </summary>
        internal void OnEnterParentTree()
        {
            if(Table != null)
            {
                Table.OnStructureChanged();
            }
        }

        /// <summary>
        /// Callback used to notify the RowGroup about exitting model tree.
        /// </summary>
        internal void OnExitParentTree()
        {
        }

        /// <summary>
        /// Callback used to notify the Cell after it has exitted model tree. (Structures are all updated)
        /// </summary>
        internal void OnAfterExitParentTree(TableRow row)
        {
            if(row.Table != null)
            {
                row.Table.OnStructureChanged();
            }
        }


        /// <summary>
        /// ValidateStructure -- caches column index
        /// </summary>
        internal void ValidateStructure(int columnIndex)
        {
            Invariant.Assert(columnIndex >= 0);

            _columnIndex = columnIndex;
        }


        #endregion Internal Methods 

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties 

        /// <summary>
        /// Row owner accessor
        /// </summary>
        internal TableRow Row { get { return Parent as TableRow; } }

        /// <summary>
        /// Table owner accessor
        /// </summary>
        internal Table Table { get { return Row != null ? Row.Table : null; } }

        /// <summary>
        /// Cell's index in the parents collection.
        /// </summary>
        internal int Index
        {
            get 
            {
                return (_parentIndex);
            }
            set 
            {
                Debug.Assert(value >= -1 && _parentIndex != value);
                _parentIndex = value;
            }
        }

        /// <summary>
        /// Returns cell's parenting row index.
        /// </summary>
        internal int RowIndex
        {
            get
            {
                return (Row.Index);
            }
        }

        /// <summary>
        /// Returns cell's parenting row group index.
        /// </summary>
        internal int RowGroupIndex
        {
            get
            {
                return (Row.RowGroup.Index);
            }
        }

        /// <summary>
        /// Returns or sets cell's parenting column index.
        /// </summary>
        /// <remarks>
        /// Called by the parent Row during (re)build of the StructuralCache.
        /// Change of column index causes Layout Dirtyness of the cell.
        /// </remarks>
        internal int ColumnIndex
        {
            get 
            {
                return (_columnIndex);
            }
            set
            {
                _columnIndex = value;
            }
        }

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

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods 

        /// <summary>
        /// Private ctor time initialization.
        /// </summary>
        private void PrivateInitialize()
        {
            _parentIndex = -1;
            _columnIndex = -1;
        }


        #endregion Private Methods 

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        #region Private Properties

        #endregion Private Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields 

        private int _parentIndex;                               //  cell's index in parent's children collection
        private int _columnIndex;                               // Column index for cell.

        #endregion Private Fields 

        //------------------------------------------------------
        //
        //  Properties
        //
        //------------------------------------------------------

        #region Properties 

        /// <summary>
        /// Column span property.
        /// </summary>
        public static readonly DependencyProperty ColumnSpanProperty =
                DependencyProperty.Register(
                        "ColumnSpan", 
                        typeof(int), 
                        typeof(TableCell),
                        new FrameworkPropertyMetadata(
                                1, 
                                FrameworkPropertyMetadataOptions.AffectsMeasure,
                                new PropertyChangedCallback(OnColumnSpanChanged)),
                        new ValidateValueCallback(IsValidColumnSpan));

        /// <summary>
        /// Row span property.
        /// </summary>
        public static readonly DependencyProperty RowSpanProperty =
                DependencyProperty.Register(
                        "RowSpan", 
                        typeof(int), 
                        typeof(TableCell),
                        new FrameworkPropertyMetadata(
                                1, 
                                FrameworkPropertyMetadataOptions.AffectsMeasure,
                                new PropertyChangedCallback(OnRowSpanChanged)),
                        new ValidateValueCallback(IsValidRowSpan));

        #endregion Properties 

        //------------------------------------------------------
        //
        //  Property Invalidation 
        //
        //------------------------------------------------------

        #region Property Invalidation 

        /// <summary>
        /// <see cref="DependencyProperty.ValidateValueCallback"/>
        /// </summary>
        private static bool IsValidRowSpan(object value)
        {
            // Maximum row span is limited to 1000000. We do not have any limits from PTS or other formatting restrictions for this value.
            int span = (int)value;
            return (span >= 1 && span <= 1000000);
        }

        /// <summary>
        /// <see cref="DependencyProperty.ValidateValueCallback"/>
        /// </summary>
        private static bool IsValidColumnSpan(object value)
        {
            int span = (int)value;
            const int maxSpan = PTS.Restrictions.tscTableColumnsRestriction;
            return (span >= 1 && span <= maxSpan);
        }

        /// <summary>
        /// <see cref="PropertyMetadata.PropertyChangedCallback"/>
        /// </summary>
        private static void OnColumnSpanChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TableCell cell = (TableCell) d;

            if(cell.Table != null)
            {
                cell.Table.OnStructureChanged();
            }

            // Update AutomaitonPeer.
            TableCellAutomationPeer peer = ContentElementAutomationPeer.FromElement(cell) as TableCellAutomationPeer;
            if (peer != null)
            {
                peer.OnColumnSpanChanged((int)e.OldValue, (int)e.NewValue);
            }
        }

        /// <summary>
        /// <see cref="PropertyMetadata.PropertyChangedCallback"/>
        /// </summary>
        private static void OnRowSpanChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TableCell cell = (TableCell) d;

            if(cell.Table != null)
            {
                cell.Table.OnStructureChanged();
            }

            // Update AutomaitonPeer.
            TableCellAutomationPeer peer = ContentElementAutomationPeer.FromElement(cell) as TableCellAutomationPeer;
            if (peer != null)
            {
                peer.OnRowSpanChanged((int)e.OldValue, (int)e.NewValue);
            }
        }

        #endregion Property Invalidation 
    }
}
