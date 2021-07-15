// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Table row group implementation
//
//              See spec at WPP TableOM.doc
//

//In order to avoid generating warnings about unknown message numbers and
//unknown pragmas when compiling your C# source code with the actual C# compiler,
//you need to disable warnings 1634 and 1691. (Presharp Documentation)
#pragma warning disable 1634, 1691

using MS.Internal.PtsHost;
using MS.Internal.PtsTable;
using MS.Utility;

using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Markup;
using System.Collections.Generic;
using MS.Internal.Documents;

using MS.Internal;
using MS.Internal.Data;
using MS.Internal.PtsHost.UnsafeNativeMethods;

namespace System.Windows.Documents
{
    /// <summary>
    /// Table row group implementation
    /// </summary>
    [ContentProperty("Rows")]
    public class TableRowGroup : TextElement, IAddChild, IIndexedChild<Table>, IAcceptInsertion
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Creates an instance of a RowGroup
        /// </summary>
        public TableRowGroup()
            : base()
        {
            Initialize();
        }

        // common initialization for all constructors
        private void Initialize()
        {
            _rows = new TableRowCollection(this);
            _rowInsertionIndex = -1;
            _parentIndex = -1;
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

            TableRow row = value as TableRow;
            if (row != null)
            {
                Rows.Add(row);

                return;
            }

            throw (new ArgumentException(SR.Get(SRID.UnexpectedParameterType, value.GetType(), typeof(TableRow)), "value"));
        }

        /// <summary>
        /// <see cref="IAddChild.AddText"/>
        /// </summary>
        void IAddChild.AddText(string text)
        {
            XamlSerializerUtil.ThrowIfNonWhiteSpaceInAddText(text, this);
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Returns the row group's row collection
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public TableRowCollection Rows
        {
            get
            {
                return (_rows);
            }
        }

        /// <summary>
        /// This method is used by TypeDescriptor to determine if this property should
        /// be serialized.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeRows()
        {
            return Rows.Count > 0;
        }

        #endregion Public Properties

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
        void IIndexedChild<Table>.OnEnterParentTree()
        {
            this.OnEnterParentTree();
        }

        /// <summary>
        /// Callback used to notify about exitting model tree.
        /// </summary>
        void IIndexedChild<Table>.OnExitParentTree()
        {
            this.OnExitParentTree();
        }

        void IIndexedChild<Table>.OnAfterExitParentTree(Table parent)
        {
            this.OnAfterExitParentTree(parent);
        }

        int IIndexedChild<Table>.Index
        {
            get { return this.Index; }
            set { this.Index = value; }
        }

        #endregion IIndexedChild implementation

        /// <summary>
        /// Callback used to notify the RowGroup about entering model tree.
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
        /// Callback used to notify the RowGroup about exitting model tree.
        /// </summary>
        internal void OnAfterExitParentTree(Table table)
        {
            table.OnStructureChanged();
        }

        /// <summary>
        /// ValidateStructure
        /// </summary>
        internal void ValidateStructure()
        {
            RowSpanVector rowSpanVector = new RowSpanVector();

            _columnCount = 0;

            for (int i = 0; i < Rows.Count; ++i)
            {
                Rows[i].ValidateStructure(rowSpanVector);

                _columnCount = Math.Max(_columnCount, Rows[i].ColumnCount);
            }

            Table.EnsureColumnCount(_columnCount);
        }


        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Table owner accessor
        /// </summary>
        internal Table Table
        {
            get
            {
                return Parent as Table;
            }
        }

        /// <summary>
        /// RowGroup's index in the parents row group collection.
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
        /// Stores temporary data for where to insert a new row
        /// </summary>
        internal int InsertionIndex
        {
            get { return _rowInsertionIndex; }
            set { _rowInsertionIndex = value; }
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
        /// Called when body receives a new parent (via OM or text tree)
        /// </summary>
        /// <param name="newParent">
        /// New parent of body
        /// </param>
        internal override void OnNewParent(DependencyObject newParent)
        {
            DependencyObject oldParent = this.Parent;

            if (newParent != null && !(newParent is Table))
            {
                throw new InvalidOperationException(SR.Get(SRID.TableInvalidParentNodeType, newParent.GetType().ToString()));
            }

            if (oldParent != null)
            {
                OnExitParentTree();
                ((Table)oldParent).RowGroups.InternalRemove(this);
                OnAfterExitParentTree(oldParent as Table);
            }

            base.OnNewParent(newParent);

            if (newParent != null)
            {
                ((Table)newParent).RowGroups.InternalAdd(this);
                OnEnterParentTree();
            }
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields
        private TableRowCollection _rows;               //  children rows store
        private int _parentIndex;                       //  row group's index in parent's children collection
        private int _rowInsertionIndex;                 //  Insertion index for row
        private int _columnCount;                       //  Column count.

        #endregion Private Fields

        //------------------------------------------------------
        //
        //  Private Structures / Classes
        //
        //------------------------------------------------------
    }
}

#pragma warning restore 1634, 1691

