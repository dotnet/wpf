// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: Base class for group descriptions.
//
// See spec at http://avalon/connecteddata/Specs/Grouping.mht
//

using System.Collections;               // IComparer
using System.Collections.ObjectModel;   // ObservableCollection
using System.Collections.Specialized;   // NotifyCollectionChangedEvent*
using System.Globalization;             // CultureInfo
using MS.Internal;                      // Invariant.Assert

namespace System.ComponentModel
{
    /// <summary>
    /// Base class for group descriptions.
    /// A GroupDescription describes how to divide the items in a collection
    /// into groups.
    /// </summary>
    public abstract class GroupDescription : INotifyPropertyChanged
    {
        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of GroupDescription.
        /// </summary>
        protected GroupDescription()
        {
            _explicitGroupNames = new ObservableCollection<object>();
            _explicitGroupNames.CollectionChanged += new NotifyCollectionChangedEventHandler(OnGroupNamesChanged);
        }

        #endregion Constructors

        #region INotifyPropertyChanged

        /// <summary>
        ///     This event is raised when a property of the group description has changed.
        /// </summary>
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add
            {
                PropertyChanged += value;
            }
            remove
            {
                PropertyChanged -= value;
            }
        }

        /// <summary>
        /// PropertyChanged event (per <see cref="INotifyPropertyChanged" />).
        /// </summary>
        protected virtual event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// A subclass can call this method to raise the PropertyChanged event.
        /// </summary>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, e);
            }
        }

        #endregion INotifyPropertyChanged

        #region Public Properties

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        /// <summary>
        /// This list of names is used to initialize a group with a set of
        /// subgroups with the given names.  (Additional subgroups may be
        /// added later, if there are items that don't match any of the names.)
        /// </summary>
        public ObservableCollection<object> GroupNames
        {
            get { return _explicitGroupNames; }
        }

        /// <summary>
        /// This method is used by TypeDescriptor to determine if this property should
        /// be serialized.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeGroupNames()
        {
            return (_explicitGroupNames.Count > 0);
        }

        /// <summary>
        /// Collection of Sort criteria to sort the groups.
        /// </summary>
        public SortDescriptionCollection SortDescriptions
        {
            get
            {
                if (_sort == null)
                    SetSortDescriptions(new SortDescriptionCollection());
                return _sort;
            }
        }

        /// <summary>
        /// This method is used by TypeDescriptor to determine if this property should
        /// be serialized.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeSortDescriptions()
        {
            return (_sort != null && _sort.Count > 0);
        }

        /// <summary>
        /// Set a custom comparer to sort groups using an object that implements IComparer.
        /// </summary>
        /// <remarks>
        /// Note: Setting the custom comparer object will clear previously set <seealso cref="GroupDescription.SortDescriptions"/>.
        /// </remarks>
        public IComparer CustomSort
        {
            get { return _customSort; }
            set
            {
                _customSort = value;
                SetSortDescriptions(null);
                OnPropertyChanged(new PropertyChangedEventArgs("CustomSort"));
            }
        }

        #endregion Public Properties

        #region Public Methods

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Return the group name(s) for the given item
        /// </summary>
        public abstract object GroupNameFromItem(object item, int level, CultureInfo culture);

        /// <summary>
        /// Return true if the names match (i.e the item should belong to the group).
        /// </summary>
        public virtual bool NamesMatch(object groupName, object itemName)
        {
            return Object.Equals(groupName, itemName);
        }

        #endregion Public Methods

        #region Internal Properties

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        /// <summary>
        /// Collection of Sort criteria to sort the groups.  Does not do lazy initialization.
        /// </summary>
        internal SortDescriptionCollection SortDescriptionsInternal
        {
            get { return _sort; }
        }

        #endregion Internal Properties

        #region Private Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        void OnGroupNamesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(new PropertyChangedEventArgs("GroupNames"));
        }

        // set new SortDescription collection; rehook collection change notification handler
        private void SetSortDescriptions(SortDescriptionCollection descriptions)
        {
            if (_sort != null)
            {
                ((INotifyCollectionChanged)_sort).CollectionChanged -= new NotifyCollectionChangedEventHandler(SortDescriptionsChanged);
            }

            bool raiseChangeEvent = (_sort != descriptions);

            _sort = descriptions;

            if (_sort != null)
            {
                Invariant.Assert(_sort.Count == 0, "must be empty SortDescription collection");
                ((INotifyCollectionChanged)_sort).CollectionChanged += new NotifyCollectionChangedEventHandler(SortDescriptionsChanged);
            }

            if (raiseChangeEvent)
            {
                OnPropertyChanged(new PropertyChangedEventArgs("SortDescriptions"));
            }
        }

        // SortDescription was added/removed, notify listeners
        private void SortDescriptionsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // adding to SortDescriptions overrides custom sort
            if (_sort.Count > 0)
            {
                if (_customSort != null)
                {
                    _customSort = null;
                    OnPropertyChanged(new PropertyChangedEventArgs("CustomSort"));
                }
            }

            OnPropertyChanged(new PropertyChangedEventArgs("SortDescriptions"));
        }


        #endregion Private Methods

        #region Private fields

        //------------------------------------------------------
        //
        //  Private fields
        //
        //------------------------------------------------------

        ObservableCollection<object> _explicitGroupNames;
        SortDescriptionCollection _sort;
        IComparer _customSort;

        #endregion Private fields
    }
}
