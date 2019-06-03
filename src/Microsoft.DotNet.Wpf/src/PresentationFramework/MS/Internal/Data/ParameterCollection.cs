// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: ParameterCollection with a simple change notification callback
//              and can be made Read-Only.  Created for ObjectDataProvider.
//

using System;
using System.Collections;   // IList
using System.Collections.ObjectModel;   // Collection<T>
using System.Windows;   // SR

namespace MS.Internal.Data
{
    internal class ParameterCollection : Collection<object>, IList
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        public ParameterCollection(ParameterCollectionChanged parametersChanged)
            : base()
        {
            _parametersChanged = parametersChanged;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Interface Properties
        //
        //------------------------------------------------------

        #region Interface Properties

        bool IList.IsReadOnly
        {
            get
            {
                return this.IsReadOnly;
            }
        }

        bool IList.IsFixedSize
        {
            get
            {
                return this.IsFixedSize;
            }
        }

        #endregion Interface Properties

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        // Methods

        protected override void ClearItems()
        {
            CheckReadOnly();
            base.ClearItems();
            OnCollectionChanged();
        }

        protected override void InsertItem(int index, object value)
        {
            CheckReadOnly();
            base.InsertItem(index, value);
            OnCollectionChanged();
        }

        protected override void RemoveItem(int index)
        {
            CheckReadOnly();
            base.RemoveItem(index);
            OnCollectionChanged();
        }

        protected override void SetItem(int index, object value)
        {
            CheckReadOnly();
            base.SetItem(index, value);
            OnCollectionChanged();
        }

        #endregion Protected Methods

        //------------------------------------------------------
        //
        //  Protected Properties
        //
        //------------------------------------------------------

        #region Protected Properties

        protected virtual bool IsReadOnly
        {
            get
            {
                return _isReadOnly;
            }
            set
            {
                _isReadOnly = value;
            }
        }

        protected bool IsFixedSize
        {
            get
            {
                return this.IsReadOnly;
            }
        }

        #endregion Protected Properties

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// sets whether the collection is read-only
        /// </summary>
        internal void SetReadOnly(bool isReadOnly)
        {
            this.IsReadOnly = isReadOnly;
        }

        /// <summary>
        /// silently clear the list.
        /// </summary>
        /// <remarks>
        /// this internal method is not affected by the state of IsReadOnly.
        /// </remarks>
        internal void ClearInternal()
        {
            base.ClearItems();
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private void CheckReadOnly()
        {
            if (this.IsReadOnly)
            {
                throw new InvalidOperationException(SR.Get(SRID.ObjectDataProviderParameterCollectionIsNotInUse));
            }
        }

        /// <summary>
        /// notify ObjectDataProvider that the parameters have changed
        /// </summary>
        private void OnCollectionChanged()
        {
            _parametersChanged(this);
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private bool _isReadOnly = false;
        private ParameterCollectionChanged _parametersChanged;

        #endregion Private Fields
    }

    internal delegate void ParameterCollectionChanged(ParameterCollection parameters);
}
