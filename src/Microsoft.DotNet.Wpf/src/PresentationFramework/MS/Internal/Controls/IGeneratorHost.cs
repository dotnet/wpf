// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: IGeneratorHost interface
//
// Specs:       Data Styling.mht
//

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace MS.Internal.Controls
{
    /// <summary>
    ///     Interface through which an ItemContainerGenerator
    ///     communicates with its host.
    /// </summary>
    internal interface IGeneratorHost
    {
        /// <summary>
        /// The view of the data
        /// </summary>
        ItemCollection View { get; }

        /// <summary>
        /// Return true if the item is (or should be) its own item container
        /// </summary>
        bool IsItemItsOwnContainer(object item);

        /// <summary>
        /// Return the element used to display the given item
        /// </summary>
        DependencyObject GetContainerForItem(object item);

        /// <summary>
        /// Prepare the element to act as the ItemUI for the corresponding item.
        /// </summary>
        void PrepareItemContainer(DependencyObject container, object item);

        /// <summary>
        /// Undo any initialization done on the element during GetContainerForItem and PrepareItemContainer
        /// </summary>
        void ClearContainerForItem(DependencyObject container, object item);

        /// <summary>
        /// Determine if the given element was generated for this host as an ItemUI.
        /// </summary>
        bool IsHostForItemContainer(DependencyObject container);

        /// <summary>
        /// Return the GroupStyle (if any) to use for the given group at the given level.
        /// </summary>
        GroupStyle GetGroupStyle(CollectionViewGroup group, int level);

        /// <summary>
        /// Communicates to the host that the generator is using grouping.
        /// </summary>
        void SetIsGrouping(bool isGrouping);

        /// <summary>
        /// The AlternationCount
        /// <summary>
        int AlternationCount { get; }
    }
}


