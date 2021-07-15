// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: IContainItemStorage interface
//

using System;

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    ///     Interface through which a layout panel (such as a VirtualizingPanel) communicates
    ///     with a host to store and retrieve values of a DependencyProperty of a container being virtualized.
    ///     
    /// </summary>
    public interface IContainItemStorage 
    {
        /// <summary>
        /// Stores the given value in ItemValueStorage, associating it with the given item and DependencyProperty.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="dp">DependencyProperty</param>
        /// <param name="value"></param>
        void StoreItemValue(object item, DependencyProperty dp, object value);

        /// <summary>
        /// Returns the value storaed gainst the given DependencyProperty and item.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="dp"></param>
        /// <returns></returns>
        object ReadItemValue(object item, DependencyProperty dp);

        /// <summary>
        /// Clears the value in ItemValueStorage, associating it with the given item and DependencyProperty.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="dp">DependencyProperty</param>
        void ClearItemValue(object item, DependencyProperty dp);
        
        /// <summary>
        /// Clears the given DependencyProperty starting at the current element including all nested storage bags.
        /// </summary>
        /// <param name="dp">DependencyProperty</param>
        void ClearValue(DependencyProperty dp);

        /// <summary>
        /// Clears the item storage on the current element entirely.
        /// </summary>
        void Clear();
    }
}
