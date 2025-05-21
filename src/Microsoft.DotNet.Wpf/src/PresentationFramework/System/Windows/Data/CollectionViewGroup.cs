// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: A CollectionViewGroup, as created by a CollectionView according to a GroupDescription.
//
// See spec at Grouping.mht
//

using System.Collections.ObjectModel;
using System.ComponentModel;

namespace System.Windows.Data
{
    /// <summary>
    /// A CollectionViewGroup, as created by a CollectionView according to a GroupDescription.
    /// </summary>
    abstract public class CollectionViewGroup : INotifyPropertyChanged
    {
        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of CollectionViewGroup.
        /// </summary>
        protected CollectionViewGroup(object name)
        {
            _name = name;
            _itemsRW = new ObservableCollection<object>();
            _itemsRO = new ReadOnlyObservableCollection<object>(_itemsRW);
        }

        #endregion Constructors

        #region Public Properties

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        /// <summary>
        /// The name of the group, i.e. the common value of the
        /// property used to divide data items into groups.
        /// </summary>
        public object Name
        {
            get { return _name; }
        }

        /// <summary>
        /// The immediate children of the group.
        /// These may be data items (leaves) or subgroups.
        /// </summary>
        public ReadOnlyObservableCollection<object> Items
        {
            get { return _itemsRO; }
        }

        /// <summary>
        /// The number of data items (leaves) in the subtree under this group.
        /// </summary>
        public int ItemCount
        {
            get { return _itemCount; }
        }

        /// <summary>
        /// Is this group at the bottom level (not further subgrouped).
        /// </summary>
        public abstract bool IsBottomLevel
        {
            get;
        }

        #endregion Public Properties

        #region INotifyPropertyChanged

        /// <summary>
        /// PropertyChanged event (per <see cref="INotifyPropertyChanged" />).
        /// </summary>
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add     { PropertyChanged += value; }
            remove  { PropertyChanged -= value; }
        }

        /// <summary>
        /// PropertyChanged event (per <see cref="INotifyPropertyChanged" />).
        /// </summary>
        protected virtual event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises a PropertyChanged event (per <see cref="INotifyPropertyChanged" />).
        /// </summary>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, e);
            }
        }

        #endregion INotifyPropertyChanged

        #region Protected Properties

        //------------------------------------------------------
        //
        //  Protected Properties
        //
        //------------------------------------------------------

        /// <summary>
        /// The items of the group.
        /// Derived classes can add or remove items using this property;
        /// the changes will be reflected in the public Items property.
        /// </summary>
        protected ObservableCollection<object> ProtectedItems
        {
            get { return _itemsRW; }
        }

        /// <summary>
        /// The number of data items (leaves) in the subtree under this group.
        /// Derived classes can change the count using this property;
        /// the changes will be reflected in the public ItemCount property.
        /// </summary>
        protected int ProtectedItemCount
        {
            get { return _itemCount; }
            set
            {
                _itemCount = value;
                OnPropertyChanged(new PropertyChangedEventArgs("ItemCount"));
            }
        }

        #endregion Protected Properties

        #region Private Fields

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        object                                  _name;
        ObservableCollection<object>            _itemsRW;
        ReadOnlyObservableCollection<object>    _itemsRO;
        int                                     _itemCount;

        #endregion Private Fields
    }
}

