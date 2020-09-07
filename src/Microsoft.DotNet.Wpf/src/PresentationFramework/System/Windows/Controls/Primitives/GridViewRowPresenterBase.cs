// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



using System.Collections;               // IEnumerator
using System.Collections.Generic;       // List<T>
using System.Collections.Specialized;   // NotifyCollectionChangedEventHandler
using System.Collections.ObjectModel;   // Collection
using System.ComponentModel;            // PropertyChangedEventArgs
using System.Diagnostics;               // Debug
using System.Windows.Media;             // VisualOperations

using MS.Internal.Controls;             // EmptyEnumerator


namespace System.Windows.Controls.Primitives
{
    /// <summary>
    /// Base class for GridViewfRowPresenter and HeaderRowPresenter.
    /// </summary>
    public abstract class GridViewRowPresenterBase : FrameworkElement, IWeakEventListener
    {
        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        #region Public Methods

        /// <summary>
        ///     Returns a string representation of this object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return SR.Get(SRID.ToStringFormatString_GridViewRowPresenterBase,
                this.GetType(),
                (Columns != null) ? Columns.Count : 0);
        }

        #endregion

        //-------------------------------------------------------------------
        //
        // Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        ///  Columns DependencyProperty
        /// </summary>
        public static readonly DependencyProperty ColumnsProperty =
            DependencyProperty.Register(
                "Columns",
                typeof(GridViewColumnCollection),
                typeof(GridViewRowPresenterBase),
                new FrameworkPropertyMetadata(
                    (GridViewColumnCollection)null /* default value */,
                    FrameworkPropertyMetadataOptions.AffectsMeasure,
                    new PropertyChangedCallback(ColumnsPropertyChanged))
            );

        /// <summary>
        /// Columns Property
        /// </summary>
        public GridViewColumnCollection Columns
        {
            get { return (GridViewColumnCollection)GetValue(ColumnsProperty); }
            set { SetValue(ColumnsProperty, value); }
        }

        #endregion

        //-------------------------------------------------------------------
        //
        // Protected Methods / Properties
        //
        //-------------------------------------------------------------------

        #region Protected Methods / Properties

        /// <summary>
        /// Returns enumerator to logical children.
        /// </summary>
        protected internal override IEnumerator LogicalChildren
        {
            get
            {
                if (InternalChildren.Count == 0)
                {
                    // empty GridViewRowPresenterBase has *no* logical children; give empty enumerator
                    return EmptyEnumerator.Instance;
                }

                // otherwise, its logical children is its visual children
                return InternalChildren.GetEnumerator();
            }
        }

        /// <summary>
        /// Gets the Visual children count.
        /// </summary>
        protected override int VisualChildrenCount
        {
            get
            {
                if (_uiElementCollection == null)
                {
                    return 0;
                }
                else
                {
                    return _uiElementCollection.Count;
                }
            }
        }

        /// <summary>
        /// Gets the Visual child at the specified index.
        /// </summary>
        protected override Visual GetVisualChild(int index)
        {
            if (_uiElementCollection == null)
            {
                throw new ArgumentOutOfRangeException("index", index, SR.Get(SRID.Visual_ArgumentOutOfRange));
            }
            return _uiElementCollection[index];
        }

        #endregion

        //-------------------------------------------------------------------
        //
        // Internal Methods / Properties
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// process the column collection chagned event
        /// </summary>
        internal virtual void OnColumnCollectionChanged(GridViewColumnCollectionChangedEventArgs e)
        {
            if (DesiredWidthList != null)
            {
                if (e.Action == NotifyCollectionChangedAction.Remove
                    || e.Action == NotifyCollectionChangedAction.Replace)
                {
                    // NOTE: The steps to make DesiredWidthList.Count <= e.ActualIndex
                    //
                    //  1. init with 3 auto columns;
                    //  2. add 1 column to the column collection with width 90.0;
                    //  3. remove the column we jsut added to the the collection;
                    //
                    //  Now we have DesiredWidthList.Count equals to 3 while the removed column
                    //  has  ActualIndex equals to 3.
                    //
                    if (DesiredWidthList.Count > e.ActualIndex)
                    {
                        DesiredWidthList.RemoveAt(e.ActualIndex);
                    }
                }
                else if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    DesiredWidthList = null;
                }
            }
        }

        /// <summary>
        /// process the column property chagned event
        /// </summary>
        internal abstract void OnColumnPropertyChanged(GridViewColumn column, string propertyName);

        /// <summary>
        /// ensure ShareStateList have at least columns.Count items
        /// </summary>
        internal void EnsureDesiredWidthList()
        {
            GridViewColumnCollection columns = Columns;

            if (columns != null)
            {
                int count = columns.Count;

                if (DesiredWidthList == null)
                {
                    DesiredWidthList = new List<double>(count);
                }

                int c = count - DesiredWidthList.Count;
                for (int i = 0; i < c; i++)
                {
                    DesiredWidthList.Add(Double.NaN);
                }
            }
        }

        /// <summary>
        /// list of currently reached max value of DesiredWidth of cell in the column
        /// </summary>
        internal List<double> DesiredWidthList
        {
            get { return _desiredWidthList; }
            private set { _desiredWidthList = value; }
        }

        /// <summary>
        /// if visual tree is out of date
        /// </summary>
        internal bool NeedUpdateVisualTree
        {
            get { return _needUpdateVisualTree; }
            set { _needUpdateVisualTree = value; }
        }

        /// <summary>
        /// collection if children
        /// </summary>
        internal UIElementCollection InternalChildren
        {
            get
            {
                if (_uiElementCollection == null) //nobody used it yet
                {
                    _uiElementCollection = new UIElementCollection(this /* visual parent */, this /* logical parent */);
                }

                return _uiElementCollection;
            }
        }

        // the minimum width for dummy header when measure
        internal const double c_PaddingHeaderMinWidth = 2.0;

        #endregion

        //-------------------------------------------------------------------
        //
        // Private Methods / Properties / Fields
        //
        //-------------------------------------------------------------------

        #region Private Methods / Properties / Fields

        // Property invalidation callback invoked when ColumnCollectionProperty is invalidated
        private static void ColumnsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GridViewRowPresenterBase c = (GridViewRowPresenterBase)d;

            GridViewColumnCollection oldCollection = (GridViewColumnCollection)e.OldValue;

            if (oldCollection != null)
            {
                InternalCollectionChangedEventManager.RemoveHandler(oldCollection, c.ColumnCollectionChanged);

                // NOTE:
                // If the collection is NOT in view mode (a.k.a owner isn't GridView),
                // RowPresenter is responsible to be or to find one to be the collection's mentor.
                //
                if (!oldCollection.InViewMode && oldCollection.Owner == c.GetStableAncester())
                {
                    oldCollection.Owner = null;
                }
            }

            GridViewColumnCollection newCollection = (GridViewColumnCollection)e.NewValue;

            if (newCollection != null)
            {
                InternalCollectionChangedEventManager.AddHandler(newCollection, c.ColumnCollectionChanged);

                // Similar to what we do to oldCollection. But, of course, in a reverse way.
                if (!newCollection.InViewMode && newCollection.Owner == null)
                {
                    newCollection.Owner = c.GetStableAncester();
                }
            }

            c.NeedUpdateVisualTree = true;
            c.InvalidateMeasure();
        }

        //
        // NOTE:
        //
        // If the collection is NOT in view mode, RowPresenter should be mentor of the Collection.
        // But if presenter + collection are used to restyle ListBoxItems and the ItemsPanel is
        // VSP, there are 2 problems:
        //
        //  1. each RowPresenter want to be the mentor, too many context change event
        //  2. when doing scroll, VSP will dispose those LB items which are out of view. But they
        //      are still referenced by the Collecion (at the Owner property) - memory leak.
        //
        // Solution:
        //  If RowPresenter is inside an ItemsControl (IC\LB\CB), use the ItemsControl as the
        //  mentor. Therefore,
        //      - context change is minimized because ItemsControl for different items is the same;
        //      - no memory leak because when viturlizing, only dispose items not the IC itself.
        //
        private FrameworkElement GetStableAncester()
        {
            ItemsControl ic = ItemsControl.ItemsControlFromItemContainer(TemplatedParent);

            return (ic != null) ? ic : (FrameworkElement)this;
        }

        // if and only if both conditions below are satisfied, row presenter visual is ready.
        // 1. is initialized, which ensures RowPresenter is created
        // 2. !NeedUpdateVisualTree, which ensures all visual elements generated by RowPresenter are created
        private bool IsPresenterVisualReady
        {
            get { return (IsInitialized && !NeedUpdateVisualTree); }
        }

        /// <summary>
        /// Handle events from the centralized event table
        /// </summary>
        bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs args)
        {
            return false;   // this method is no longer used (but must remain, for compat)
        }

        /// <summary>
        /// Handler of GridViewColumnCollection.CollectionChanged event.
        /// </summary>
        private void ColumnCollectionChanged(object sender, NotifyCollectionChangedEventArgs arg)
        {
            GridViewColumnCollectionChangedEventArgs e = arg as GridViewColumnCollectionChangedEventArgs;

            if (e != null
                && IsPresenterVisualReady)// if and only if rowpresenter's visual is ready, shall rowpresenter go ahead process the event.
            {
                // Property of one column changed
                if (e.Column != null)
                {
                    OnColumnPropertyChanged(e.Column, e.PropertyName);
                }
                else
                {
                    OnColumnCollectionChanged(e);
                }
            }
        }

        private UIElementCollection _uiElementCollection;
        private bool _needUpdateVisualTree = true;
        private List<double> _desiredWidthList;

        #endregion
    }

    /// <summary>
    /// Manager for the GridViewColumnCollection.CollectionChanged event.
    /// </summary>
    internal class InternalCollectionChangedEventManager : WeakEventManager
    {
        #region Constructors

        //
        //  Constructors
        //

        private InternalCollectionChangedEventManager()
        {
        }

        #endregion Constructors

        #region Public Methods

        //
        //  Public Methods
        //

        /// <summary>
        /// Add a listener to the given source's event.
        /// </summary>
        public static void AddListener(GridViewColumnCollection source, IWeakEventListener listener)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (listener == null)
                throw new ArgumentNullException("listener");

            CurrentManager.ProtectedAddListener(source, listener);
        }

        /// <summary>
        /// Remove a listener to the given source's event.
        /// </summary>
        public static void RemoveListener(GridViewColumnCollection source, IWeakEventListener listener)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (listener == null)
                throw new ArgumentNullException("listener");

            CurrentManager.ProtectedRemoveListener(source, listener);
        }

        /// <summary>
        /// Add a handler for the given source's event.
        /// </summary>
        public static void AddHandler(GridViewColumnCollection source, EventHandler<NotifyCollectionChangedEventArgs> handler)
        {
            if (handler == null)
                throw new ArgumentNullException("handler");

            CurrentManager.ProtectedAddHandler(source, handler);
        }

        /// <summary>
        /// Remove a handler for the given source's event.
        /// </summary>
        public static void RemoveHandler(GridViewColumnCollection source, EventHandler<NotifyCollectionChangedEventArgs> handler)
        {
            if (handler == null)
                throw new ArgumentNullException("handler");

            CurrentManager.ProtectedRemoveHandler(source, handler);
        }

        #endregion Public Methods

        #region Protected Methods

        //
        //  Protected Methods
        //

        /// <summary>
        /// Return a new list to hold listeners to the event.
        /// </summary>
        protected override ListenerList NewListenerList()
        {
            return new ListenerList<NotifyCollectionChangedEventArgs>();
        }

        /// <summary>
        /// Listen to the given source for the event.
        /// </summary>
        protected override void StartListening(object source)
        {
            GridViewColumnCollection typedSource = (GridViewColumnCollection)source;
            typedSource.InternalCollectionChanged += new NotifyCollectionChangedEventHandler(OnCollectionChanged);
        }

        /// <summary>
        /// Stop listening to the given source for the event.
        /// </summary>
        protected override void StopListening(object source)
        {
            GridViewColumnCollection typedSource = (GridViewColumnCollection)source;
            typedSource.InternalCollectionChanged -= new NotifyCollectionChangedEventHandler(OnCollectionChanged);
        }

        #endregion Protected Methods

        #region Private Properties

        //
        //  Private Properties
        //

        // get the event manager for the current thread
        private static InternalCollectionChangedEventManager CurrentManager
        {
            get
            {
                Type managerType = typeof(InternalCollectionChangedEventManager);
                InternalCollectionChangedEventManager manager = (InternalCollectionChangedEventManager)GetCurrentManager(managerType);

                // at first use, create and register a new manager
                if (manager == null)
                {
                    manager = new InternalCollectionChangedEventManager();
                    SetCurrentManager(managerType, manager);
                }

                return manager;
            }
        }

        #endregion Private Properties

        #region Private Methods

        //
        //  Private Methods
        //

        // event handler for CollectionChanged event
        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            DeliverEvent(sender, args);
        }

        #endregion Private Methods
    }
}
