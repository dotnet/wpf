// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;

namespace System.Windows.Controls
{
    /// <summary>
    ///     Provides access to the new item during the InitializingNewItem event.
    /// </summary>
    public class InitializingNewItemEventArgs : EventArgs
    {
        /// <summary>
        ///     Instantiates a new instance of this class.
        /// </summary>
        public InitializingNewItemEventArgs(object newItem)
        {
            _newItem = newItem;
        }

        /// <summary>
        ///     The new item.
        /// </summary>
        public object NewItem
        {
            get { return _newItem; }
        }

        private object _newItem;
    }
}
