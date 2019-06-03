// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Holds an existing collection structure
//              (e.g. ObservableCollection or DataSet) inside the ItemCollection.
//
// See specs at ItemCollection.mht
//              IDataCollection.mht
//

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using MS.Utility;
using MS.Internal;              // Invariant.Assert
using MS.Internal.Utility;
using MS.Internal.Data;         // IndexedEnumerable

using System;

namespace System.Windows.Data
{
    /// <summary>
    /// Holds an existing collection structure
    /// (e.g. ObservableCollection or DataSet) for use under a CompositeCollection.
    /// </summary>
    public class CollectionContainer : DependencyObject, INotifyCollectionChanged, IWeakEventListener
    {
        //------------------------------------------------------
        //
        //  Dynamic properties and events
        //
        //------------------------------------------------------

        /// <summary>
        /// Collection to be added into flattened ItemCollection
        /// </summary>
        public static readonly DependencyProperty CollectionProperty =
                DependencyProperty.Register(
                        "Collection",
                        typeof(IEnumerable),
                        typeof(CollectionContainer),
                        new FrameworkPropertyMetadata(new PropertyChangedCallback(OnCollectionPropertyChanged)));


        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static CollectionContainer()
        {
        }

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        //ISSUE/davidjen/030820 perf will potentially degrade if assigned collection
        //                      is only IEnumerable, but will improve if it
        //                      implements ICollection (for Count property), or,
        //                      better yet IList (for IndexOf and forward/backward enum using indexer)
        /// <summary>
        /// Collection to be added into flattened ItemCollection.
        /// </summary>
        public IEnumerable Collection
        {
            get { return (IEnumerable) GetValue(CollectionContainer.CollectionProperty); }
            set { SetValue(CollectionContainer.CollectionProperty, value); }
        }

        /// <summary>
        /// This method is used by TypeDescriptor to determine if this property should
        /// be serialized.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeCollection()
        {
            if (Collection == null)
            {
                return false;
            }

            // Try to see if there is an item in the Collection without
            // creating an enumerator.
            ICollection collection = Collection as ICollection;
            if (collection != null && collection.Count == 0)
            {
                return false;
            }

            // If MoveNext returns true, then the enumerator is non-empty.
            IEnumerator enumerator = Collection.GetEnumerator();
            bool result = enumerator.MoveNext();
            IDisposable d = enumerator as IDisposable;
            if (d != null)
            {
                d.Dispose();
            }

            return result;
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        internal ICollectionView View
        {
            get
            {
                return _view;
            }
        }

        internal int ViewCount
        {
            get
            {
                if (View == null)
                    return 0;

                CollectionView cv = View as CollectionView;
                if (cv != null)
                    return cv.Count;

                ICollection coll = View as ICollection;
                if (coll != null)
                    return coll.Count;

                // As a last resort, use the IList interface or IndexedEnumerable to find the count.
                if (ViewList != null)
                    return ViewList.Count;

                return 0;
            }
        }

        internal bool ViewIsEmpty
        {
            get
            {
                if (View == null)
                    return true;

                ICollectionView cv = View as ICollectionView;
                if (cv != null)
                    return cv.IsEmpty;

                ICollection coll = View as ICollection;
                if (coll != null)
                    return (coll.Count == 0);

                // As a last resort, use the IList interface or IndexedEnumerable to find the count.
                if (ViewList != null)
                {
                    IndexedEnumerable le = ViewList as IndexedEnumerable;
                    if (le != null)
                        return le.IsEmpty;
                    else
                        return (ViewList.Count == 0);
                }

                return true;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal object ViewItem(int index)
        {
            Invariant.Assert(index >= 0 && View != null);

            CollectionView cv = View as CollectionView;
            if (cv != null)
            {
                return cv.GetItemAt(index);
            }

            // As a last resort, use the IList interface or IndexedEnumerable to iterate to the nth item.
            if (ViewList != null)
                return ViewList[index];

            return null;
        }

        internal int ViewIndexOf(object item)
        {
            if (View == null)
                return -1;

            CollectionView cv = View as CollectionView;
            if (cv != null)
            {
                return cv.IndexOf(item);
            }

            // As a last resort, use the IList interface or IndexedEnumerable to look for the item.
            if (ViewList != null)
                return ViewList.IndexOf(item);

            return -1;
        }

        internal void GetCollectionChangedSources(int level, Action<int, object, bool?, List<string>> format, List<string> sources)
        {
            format(level, this, false, sources);
            if (_view != null)
            {
                CollectionView cv = _view as CollectionView;
                if (cv != null)
                {
                    cv.GetCollectionChangedSources(level+1, format, sources);
                }
                else
                {
                    format(level+1, _view, true, sources);
                }
            }
        }

        #endregion Internal Methods

        #region INotifyCollectionChanged

        /// <summary>
        /// Occurs when the contained collection changes
        /// </summary>
        event NotifyCollectionChangedEventHandler INotifyCollectionChanged.CollectionChanged
        {
            add     { CollectionChanged += value; }
            remove  { CollectionChanged -= value; }
        }

        /// <summary>
        /// Occurs when the contained collection changes
        /// </summary>
        protected virtual event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// Called when the contained collection changes
        /// </summary>
        protected virtual void OnContainedCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            if (CollectionChanged != null)
                CollectionChanged(this, args);
        }

        #endregion INotifyCollectionChanged

        #region IWeakEventListener

        /// <summary>
        /// Handle events from the centralized event table
        /// </summary>
        bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            return ReceiveWeakEvent(managerType, sender, e);
        }

        /// <summary>
        /// Handle events from the centralized event table
        /// </summary>
        protected virtual bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            return false;   // this method is no longer used (but must remain, for compat)
        }

        #endregion IWeakEventListener

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        #region Private Properties

        private IndexedEnumerable ViewList
        {
            get
            {
                if (_viewList == null && View != null)
                {
                    _viewList = new IndexedEnumerable(View);
                }
                return _viewList;
            }
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // called when value of CollectionProperty is required by property store
        private static object OnGetCollection(DependencyObject d)
        {
            return ((CollectionContainer) d).Collection;
        }

        // Called when CollectionProperty is changed on "d."
        private static void OnCollectionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CollectionContainer cc = (CollectionContainer) d;
            cc.HookUpToCollection((IEnumerable) e.NewValue, true);
        }

        // To prevent CollectionContainer memory leak:
        // HookUpToCollection() is called to start listening to CV only when
        // the Container is being used by a CompositeCollectionView.
        // When the last CCV stops using the container (or the CCV is GC'ed),
        // HookUpToCollection() is called to stop listening to its CV, so that
        // this container can be GC'ed if no one else is holding on to it.

        // unhook old collection/view and hook up new collection/view
        private void HookUpToCollection(IEnumerable newCollection, bool shouldRaiseChangeEvent)
        {
            // clear cached helper
            _viewList = null;

            // unhook from the old collection view
            if (View != null)
            {
                CollectionChangedEventManager.RemoveHandler(View, OnCollectionChanged);

                if (_traceLog != null)
                    _traceLog.Add("Unsubscribe to CollectionChange from {0}",
                            TraceLog.IdFor(View));
            }

            // change to the new view
            if (newCollection != null)
                _view = CollectionViewSource.GetDefaultCollectionView(newCollection, this);
            else
                _view = null;

            // hook up to the new collection view
            if (View != null)
            {
                CollectionChangedEventManager.AddHandler(View, OnCollectionChanged);

                if (_traceLog != null)
                    _traceLog.Add("Subscribe to CollectionChange from {0}", TraceLog.IdFor(View));
            }

            if (shouldRaiseChangeEvent) // it's as if this were a refresh of the container's collection
                OnContainedCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // forward the event to CompositeCollections that use this container
            OnContainedCollectionChanged(e);
        }

        #endregion Private Methods

        // this method is here just to avoid the compiler error
        // error CS0649: Warning as Error: Field '..._traceLog' is never assigned to, and will always have its default value null
        void InitializeTraceLog()
        {
            _traceLog = new TraceLog(20);
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private TraceLog        _traceLog;
        private ICollectionView _view;
        private IndexedEnumerable _viewList;      // cache of list wrapper for view

        #endregion Private Fields
    }
}
