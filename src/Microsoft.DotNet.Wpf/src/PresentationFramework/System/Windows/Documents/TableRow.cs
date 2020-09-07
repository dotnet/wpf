// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Table row implementation
//
//              See spec at WPP TableOM.doc
//

using MS.Internal;
using MS.Internal.PtsHost;
using MS.Internal.PtsTable;
using MS.Internal.Text;
using MS.Utility;

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Security;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Markup;
using System.Collections.Generic;
using MS.Internal.Documents;
using MS.Internal.PtsHost.UnsafeNativeMethods;

namespace System.Windows.Documents
{
    /// <summary>
    /// Table row
    /// </summary>

    [ContentProperty("Cells")]
    public class TableRow : TextElement, IAddChild, IIndexedChild<TableRowGroup>, IAcceptInsertion
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Creates an instance of a Row
        /// </summary>
        public TableRow()
            : base()
        {
            PrivateInitialize();
        }

        #endregion

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// <see cref="IAddChild.AddChild"/>
        /// </summary>
        void IAddChild.AddChild(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            TableCell cell = value as TableCell;
            if (cell != null)
            {
                Cells.Add(cell);
                return;
            }

            throw (new ArgumentException(SR.Get(SRID.UnexpectedParameterType, value.GetType(), typeof(TableCell)), "value"));
        }

        /// <summary>
        /// <see cref="IAddChild.AddText"/>
        /// </summary>
        void IAddChild.AddText(string text)
        {
            XamlSerializerUtil.ThrowIfNonWhiteSpaceInAddText(text, this);
        }


        /// <summary>
        /// Called when tablerow gets new parent
        /// </summary>
        /// <param name="newParent">
        /// New parent of cell
        /// </param>
        internal override void OnNewParent(DependencyObject newParent)
        {
            DependencyObject oldParent = this.Parent;

            if (newParent != null && !(newParent is TableRowGroup))
            {
                throw new InvalidOperationException(SR.Get(SRID.TableInvalidParentNodeType, newParent.GetType().ToString()));
            }

            if (oldParent != null)
            {
                ((TableRowGroup)oldParent).Rows.InternalRemove(this);
            }

            base.OnNewParent(newParent);

            if (newParent != null)
            {
                ((TableRowGroup)newParent).Rows.InternalAdd(this);
            }
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods
        #endregion Protected Methods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        #region IIndexedChild implementation
        /// <summary>
        /// Callback used to notify about entering model tree.
        /// </summary>
        void IIndexedChild<TableRowGroup>.OnEnterParentTree()
        {
            this.OnEnterParentTree();
        }

        /// <summary>
        /// Callback used to notify about exitting model tree.
        /// </summary>
        void IIndexedChild<TableRowGroup>.OnExitParentTree()
        {
            this.OnExitParentTree();
        }

        void IIndexedChild<TableRowGroup>.OnAfterExitParentTree(TableRowGroup parent)
        {
            this.OnAfterExitParentTree(parent);
        }
        int IIndexedChild<TableRowGroup>.Index
        {
            get { return this.Index; }
            set { this.Index = value; }
        }
        #endregion IIndexedChild implementation

        /// <summary>
        /// Callback used to notify the Row about entering model tree.
        /// </summary>
        internal void OnEnterParentTree()
        {
            if (Table != null)
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
        /// Callback used to notify the Row about exitting model tree.
        /// </summary>
        internal void OnAfterExitParentTree(TableRowGroup rowGroup)
        {
            if (rowGroup.Table != null)
            {
                Table.OnStructureChanged();
            }
        }

        /// <summary>
        /// ValidateStructure
        /// </summary>
        internal void ValidateStructure(RowSpanVector rowSpanVector)
        {
            Debug.Assert(rowSpanVector != null);

            SetFlags(!rowSpanVector.Empty(), Flags.HasForeignCells);
            SetFlags(false, Flags.HasRealCells);

            _formatCellCount = 0;
            _columnCount = 0;

            int firstAvailableIndex;
            int firstOccupiedIndex;

            rowSpanVector.GetFirstAvailableRange(out firstAvailableIndex, out firstOccupiedIndex);
            for (int i = 0; i < _cells.Count; ++i)
            {
                TableCell cell = _cells[i];

                // Get cloumn span and row span. Row span is limited to the number of rows in the row group.
                // Since we do not know the number of columns in the table at this point, column span is limited only
                // by internal constants
                int columnSpan = cell.ColumnSpan;
                int rowSpan = cell.RowSpan;

                while (firstAvailableIndex + columnSpan > firstOccupiedIndex)
                {
                    rowSpanVector.GetNextAvailableRange(out firstAvailableIndex, out firstOccupiedIndex);
                }

                Debug.Assert(i <= firstAvailableIndex);

                cell.ValidateStructure(firstAvailableIndex);

                if (rowSpan > 1)
                {
                    rowSpanVector.Register(cell);
                }
                else
                {
                    _formatCellCount++;
                }

                firstAvailableIndex += columnSpan;
            }

            _columnCount = firstAvailableIndex;

            bool isLastRowOfAnySpan = false;
            rowSpanVector.GetSpanCells(out _spannedCells, out isLastRowOfAnySpan);
            Debug.Assert(_spannedCells != null);

            if ((_formatCellCount > 0) ||
               isLastRowOfAnySpan == true)
            {
                SetFlags(true, Flags.HasRealCells);
            }

            _formatCellCount += _spannedCells.Length;

            Debug.Assert(_cells.Count <= _formatCellCount);
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// RowGroup owner accessor
        /// </summary>
        internal TableRowGroup RowGroup
        {
            get
            {
                return (Parent as TableRowGroup);
            }
        }

        /// <summary>
        /// Table owner accessor
        /// </summary>
        internal Table Table { get { return (RowGroup != null ? RowGroup.Table : null); } }

        /// <summary>
        /// Returns the row's cell collection
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public TableCellCollection Cells { get { return (_cells); } }

        /// <summary>
        /// This method is used by TypeDescriptor to determine if this property should
        /// be serialized.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeCells()
        {
            return (Cells.Count > 0);
        }

        /// <summary>
        /// Row's index in the parents collection.
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

        int IAcceptInsertion.InsertionIndex
        {
            get { return this.InsertionIndex; }
            set { this.InsertionIndex = value; }
        }
        /// <summary>
        /// Stores temporary data for where to insert a new cell
        /// </summary>
        internal int InsertionIndex
        {
            get { return _cellInsertionIndex; }
            set { _cellInsertionIndex = value; }
        }

        /// <summary>
        /// Returns span cells vector
        /// </summary>
        internal TableCell[] SpannedCells
        {
            get { return (_spannedCells); }
        }

        /// <summary>
        /// Count of columns in the table
        /// </summary>
        internal int ColumnCount
        {
            get
            {
                return (_columnCount);
            }
        }

        /// <summary>
        /// Returns "true" if there are row spanned cells belonging to previous rows
        /// </summary>
        internal bool HasForeignCells
        {
            get { return (CheckFlags(Flags.HasForeignCells)); }
        }

        /// <summary>
        /// Returns "true" if there are row spanned cells belonging to previous rows
        /// </summary>
        internal bool HasRealCells
        {
            get { return (CheckFlags(Flags.HasRealCells)); }
        }

        /// <summary>
        /// Count of columns in the table
        /// </summary>
        internal int FormatCellCount
        {
            get
            {
                return (_formatCellCount);
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
            _cells = new TableCellCollection(this);
            _parentIndex = -1;
            _cellInsertionIndex = -1;
        }

        /// <summary>
        /// SetFlags is used to set or unset one or multiple flags on the row.
        /// </summary>
        private void SetFlags(bool value, Flags flags)
        {
            _flags = value ? (_flags | flags) : (_flags & (~flags));
        }

        /// <summary>
        /// CheckFlags returns true if all flags in the bitmask flags are set.
        /// </summary>
        private bool CheckFlags(Flags flags)
        {
            return ((_flags & flags) == flags);
        }


        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields
        private TableCellCollection _cells;             //  collection of cells belonging to the row
        private TableCell[] _spannedCells;              //  row spanned cell storage

        private int _parentIndex;                       //  row's index in parent's children collection
        private int _cellInsertionIndex;                // Insertion index a cell
        private int _columnCount;

        private Flags _flags;                           //  flags reflecting various aspects of row's state
        private int _formatCellCount;                   //  count of the cell to be formatted in this row


        #endregion Private Fields

        //------------------------------------------------------
        //
        //  Private Structures / Classes
        //
        //------------------------------------------------------

        #region Private Structures Classes

        [System.Flags]
        private enum Flags
        {
            //
            //  state flags
            //
            HasForeignCells = 0x00000010, //  there are overhanging cells from the previous rows
            HasRealCells = 0x00000020, // real cells in row (not just spanning) (Only known by validation, not format)
        }

        #endregion Private Structures Classes
    }
}
