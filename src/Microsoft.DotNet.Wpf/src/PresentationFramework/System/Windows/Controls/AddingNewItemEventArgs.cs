// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;

namespace System.Windows.Controls
{
    /// <summary>
    ///     Provides access to the new item during the AddingNewItem event.
    /// </summary>
    public class AddingNewItemEventArgs : EventArgs
    {
        /// <summary>
        ///     Instantiates a new instance of this class.
        /// </summary>
        public AddingNewItemEventArgs()
        {
        }

        /// <summary>
        ///     The new item.    If an event handler sets this property, the
        ///     value is the item added to the DataGrid's items source.
        /// </summary>
        public object NewItem
        {
            get { return _newItem; }
            set { _newItem = value; }
        }

        private object _newItem;
    }
}

