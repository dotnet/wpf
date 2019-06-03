// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: AddNewItem functionality for collection views.
//

using System;

namespace System.ComponentModel
{
/// <summary>
/// IAddNewItem is an interface that a collection view
/// can implement to enable functionality for adding a user-supplied item to the
/// underlying collection.
/// </summary>
public interface IEditableCollectionViewAddNewItem : IEditableCollectionView
{
    /// <summary>
    /// Return true if the view supports <seealso cref="AddNewItem"/>.
    /// </summary>
    bool    CanAddNewItem { get; }

    /// <summary>
    /// Add a new item to the underlying collection.  Returns the new item.
    /// After calling AddNewItem and changing the new item as desired, either
    /// <seealso cref="IEditableCollectionView.CommitNew"/> or <seealso cref="IEditableCollectionView.CancelNew"/> should be
    /// called to complete the transaction.
    /// </summary>
    object  AddNewItem(object newItem);
}
}
