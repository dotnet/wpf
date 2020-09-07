// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Table implementation
//
//              See spec at WPP TableOM.doc
//

using MS.Internal;
using MS.Internal.PtsHost;
using MS.Internal.PtsTable;
using MS.Utility;
using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Media;
using System.Windows.Markup;
using MS.Internal.PtsHost.UnsafeNativeMethods;
using MS.Internal.Documents;

#pragma warning disable 1634, 1691  // suppressing PreSharp warnings

namespace System.Windows.Documents
{
    /// <summary>
    /// Table implements 
    /// </summary>
    [ContentProperty("RowGroups")]
    public class Table : Block, IAddChild, IAcceptInsertion
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors 

        /// <summary>
        /// Static ctor.  Initializes property metadata.
        /// </summary>
        static Table()
        {
            MarginProperty.OverrideMetadata(typeof(Table), new FrameworkPropertyMetadata(new Thickness(Double.NaN)));
        }

        /// <summary>
        /// Table constructor.
        /// </summary>
        public Table()
        {
            PrivateInitialize();
        }

        #endregion Constructors 

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

            TableRowGroup rowGroup = value as TableRowGroup;
            if (rowGroup != null)
            {
                RowGroups.Add(rowGroup);
                return;
            }

            throw (new ArgumentException(SR.Get(SRID.UnexpectedParameterType, value.GetType(), typeof(TableRowGroup)), "value"));
        }

        /// <summary>
        /// <see cref="IAddChild.AddText"/>
        /// </summary>
        void IAddChild.AddText(string text)
        {
            XamlSerializerUtil.ThrowIfNonWhiteSpaceInAddText(text, this);
        }

        /// <summary>
        ///     Initialization of this element is about to begin
        /// </summary>
        public override void BeginInit()
        {
            base.BeginInit();
            _initializing = true;
        }

        /// <summary>
        ///     Initialization of this element has completed
        /// </summary>
        public override void EndInit()
        {
            _initializing = false;
            OnStructureChanged();
            base.EndInit();
        }

        /// <summary>
        /// <see cref="FrameworkContentElement.LogicalChildren"/>
        /// </summary>
        /// <remarks>
        /// children enumerated in the following order:
        /// * columns
        /// * rowgroups
        /// </remarks>
        protected internal override IEnumerator LogicalChildren
        {
            get { return (new TableChildrenCollectionEnumeratorSimple(this)); }
        }

        #endregion Public Methods 

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties 

        /// <summary>
        /// Returns table column collection.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public TableColumnCollection Columns { get { return (_columns); } }

        /// <summary>
        /// This method is used by TypeDescriptor to determine if this property should
        /// be serialized.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeColumns()
        {
            return (Columns.Count > 0);
        }

        /// <summary>
        /// Returns table row group.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public TableRowGroupCollection RowGroups
        {
            get { return (_rowGroups); }
        }

        /// <summary>
        /// Cell spacing property.
        /// </summary>
        [TypeConverter(typeof(LengthConverter))]
        public double CellSpacing
        {
            get { return (double) GetValue(CellSpacingProperty); }
            set { SetValue(CellSpacingProperty, value); }
        }

        #endregion Public Properties 

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods 

        /// <summary>
        /// Creates AutomationPeer (<see cref="ContentElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new TableAutomationPeer(this);
        }

        #endregion Protected Methods 


        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties 

        /// <summary>
        /// Internal cell spacing getter
        /// </summary>
        internal double InternalCellSpacing
        {
            get { return Math.Max(CellSpacing, 0); }
        }

        int IAcceptInsertion.InsertionIndex
        {
            get { return this.InsertionIndex; }
            set { this.InsertionIndex = value; }
        }
        /// <summary>
        /// Stores temporary data for where to insert a new row group
        /// </summary>
        internal int InsertionIndex
        {
            get { return _rowGroupInsertionIndex; }
            set { _rowGroupInsertionIndex = value; }
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

        #endregion Internal Properties 


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Updates table actual column count
        /// </summary>
        /// <param name="columnCount">Count of column to account for</param>
        internal void EnsureColumnCount(int columnCount)
        {
            if (_columnCount < columnCount)
                _columnCount = columnCount;
        }

        /// <summary>
        /// OnStructureChanged - Called to rebuild structure.
        /// </summary>
        internal void OnStructureChanged()
        {
            if (!_initializing)
            {
                if (TableStructureChanged != null)
                {
                    TableStructureChanged(this, EventArgs.Empty);
                }

                ValidateStructure();

                // Table structure changes affect number of rows and colums. Need to notify peer about it.
                TableAutomationPeer peer = ContentElementAutomationPeer.FromElement(this) as TableAutomationPeer;
                if (peer != null)
                {
                    peer.OnStructureInvalidated();
                }
            }
        }

        /// <summary>
        /// ValidateStructure
        /// </summary>
        internal void ValidateStructure()
        {
            if (!_initializing)
            {
                //
                //  validate row groups structural cache
                //
                _columnCount = 0;
                for (int i = 0; i < _rowGroups.Count; ++i)
                {
                    _rowGroups[i].ValidateStructure();
                }

                _version++;
            }
        }

        /// <summary>
        /// Notifies the text container that some property change has occurred, requiring a revalidation of table.
        /// </summary>
        internal void InvalidateColumns()
        {
            NotifyTypographicPropertyChanged(true /* affectsMeasureOrArrange */, true /* localValueChanged */, null);
        }

        /// <summary>
        /// Returns true if the given rowGroupIndex is the first non-empty one
        /// </summary>
        internal bool IsFirstNonEmptyRowGroup(int rowGroupIndex)
        {
            rowGroupIndex--;

            while(rowGroupIndex >= 0)
            {
                if(RowGroups[rowGroupIndex].Rows.Count > 0)
                {
                    return false;
                }

                rowGroupIndex--;
            }

            return true;
        }

        /// <summary>
        /// Returns true if the given rowGroupIndex is the last non-empty one
        /// </summary>
        internal bool IsLastNonEmptyRowGroup(int rowGroupIndex)
        {
            rowGroupIndex++;

            while(rowGroupIndex < RowGroups.Count)
            {
                if(RowGroups[rowGroupIndex].Rows.Count > 0)
                {
                    return false;
                }

                rowGroupIndex++;
            }

            return true;
        }


        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Events
        //
        //------------------------------------------------------

        #region Internal Events

        /// <summary>
        /// Fired when the table changes structurally
        /// </summary>
        internal event EventHandler TableStructureChanged;

        #endregion

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
            // Acquire new PTS Context.
            _columns = new TableColumnCollection(this);
            _rowGroups = new TableRowGroupCollection(this);
            _rowGroupInsertionIndex = -1;
        }

        private static bool IsValidCellSpacing(object o)
        {
            double spacing = (double)o;
            double maxSpacing = Math.Min(1000000, PTS.MaxPageSize);
            if (Double.IsNaN(spacing))
            {
                return false;
            }
            if (spacing < 0 || spacing > maxSpacing)
            {
                return false;
            }
            return true;
        }

        #endregion Private Methods 

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields 
        private TableColumnCollection _columns;         //  collection of columns
        private TableRowGroupCollection _rowGroups;     //  collection of row groups
        private int _rowGroupInsertionIndex;            //  insertion index used by row group collection
        private const double c_defaultCellSpacing = 2;  //  default value of cell spacing
        private int _columnCount;
        private int _version = 0;
        private bool _initializing;                     //  True if the table is being initialized

        #endregion Private Fields 

        //------------------------------------------------------
        //
        //  Private Structures / Classes
        //
        //------------------------------------------------------

        #region Private Structures Classes 

        /// <summary>
        /// Implementation of a simple enumerator of table's children
        /// </summary>
        private class TableChildrenCollectionEnumeratorSimple : IEnumerator, ICloneable
        {
            internal TableChildrenCollectionEnumeratorSimple(Table table)
            {
                Debug.Assert(table != null);
                _table = table;
                _version = _table._version;
                _columns = ((IEnumerable)_table._columns).GetEnumerator();
                _rowGroups = ((IEnumerable)_table._rowGroups).GetEnumerator();
            }

            public Object Clone()
            {
                return (MemberwiseClone());
            }

            public bool MoveNext()
            {
                if (_version != _table._version)
                {
                    throw new InvalidOperationException(SR.Get(SRID.EnumeratorVersionChanged));
                }

                // Strange design, but iterator must spin on contained column iterator
                if ((_currentChildType != ChildrenTypes.Columns) && (_currentChildType != ChildrenTypes.RowGroups))
                    _currentChildType++;

                Object currentChild = null;

                while (_currentChildType < ChildrenTypes.AfterLast)
                {
                    switch (_currentChildType)
                    {
                        case (ChildrenTypes.Columns):
                            if (_columns.MoveNext())
                            { 
                                currentChild = _columns.Current; 
                            }
                            break;
                        case (ChildrenTypes.RowGroups):
                            if (_rowGroups.MoveNext())
                            {
                                currentChild = _rowGroups.Current;
                            }
                            break;
                    }

                    if (currentChild != null)  
                    { 
                        _currentChild = currentChild;
                        break;
                    }

                    _currentChildType++;
                }

                Debug.Assert(_currentChildType != ChildrenTypes.BeforeFirst);
                return (_currentChildType != ChildrenTypes.AfterLast);
            }

            public Object Current
            {
                get
                {
                    if (_currentChildType == ChildrenTypes.BeforeFirst)
                    {
                        #pragma warning suppress 6503 // IEnumerator.Current is documented to throw this exception
                        throw new InvalidOperationException(SR.Get(SRID.EnumeratorNotStarted));
                    }
                    if (_currentChildType == ChildrenTypes.AfterLast)
                    {
                        #pragma warning suppress 6503 // IEnumerator.Current is documented to throw this exception
                        throw new InvalidOperationException(SR.Get(SRID.EnumeratorReachedEnd));
                    }

                    return (_currentChild);
                }
            }

            public void Reset()
            {
                if (_version != _table._version)
                {
                    throw new InvalidOperationException(SR.Get(SRID.EnumeratorVersionChanged));
                }

                _columns.Reset();
                _rowGroups.Reset();
                _currentChildType = ChildrenTypes.BeforeFirst;
                _currentChild = null;
            }

            private Table _table;
            private int _version;
            private IEnumerator _columns;
            private IEnumerator _rowGroups;
            private ChildrenTypes _currentChildType;
            private Object _currentChild;

            private enum ChildrenTypes : int
            {
                BeforeFirst     = 0,
                Columns         = 1,
                RowGroups       = 2,
                AfterLast       = 3,
            }
        }

        #endregion Private Structures Classes 

        //------------------------------------------------------
        //
        //  Properties
        //
        //------------------------------------------------------

        #region Properties 

        /// <summary>
        /// Cell spacing property.
        /// </summary>
        public static readonly DependencyProperty CellSpacingProperty =
                DependencyProperty.Register(
                        "CellSpacing", 
                        typeof(double), 
                        typeof(Table),
                        new FrameworkPropertyMetadata(
                                c_defaultCellSpacing, 
                                FrameworkPropertyMetadataOptions.AffectsMeasure),
                        new ValidateValueCallback(IsValidCellSpacing));

        #endregion Properties 

        //------------------------------------------------------
        //
        //  Debug / Performance
        //
        //------------------------------------------------------

        #if TABLEPARANOIA
        internal int ParanoiaVersion = 0;
        #endif // TABLEPARANOIA
    }
}

