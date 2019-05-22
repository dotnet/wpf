// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: Editing functionality for collection views.
//
// See spec at http://sharepoint/sites/wpftsv/Documents/DataGrid/DataGrid_CollectionView.mht
//

using System;

namespace System.ComponentModel
{
/// <summary>
/// Describes the desired position of the new item placeholder in an
/// <seealso cref="IEditableCollectionView"/>.
/// </summary>
public enum NewItemPlaceholderPosition
{
    /// <summary> Do not include a placeholder. </summary>
    None,
    /// <summary> Put the placeholder at the beginning. </summary>
    AtBeginning,
    /// <summary> Put the placeholder at the end. </summary>
    AtEnd,
}

/// <summary>
/// IEditableCollectionView is an interface that a collection view
/// can implement to enable editing-related functionality.
/// </summary>
public interface IEditableCollectionView
{
    #region Adding new items

    /// <summary>
    /// Indicates whether to include a placeholder for a new item, and if so,
    /// where to put it.
    /// </summary>
    NewItemPlaceholderPosition NewItemPlaceholderPosition { get; set; }

    /// <summary>
    /// Return true if the view supports <seealso cref="AddNew"/>.
    /// </summary>
    bool    CanAddNew { get; }

    /// <summary>
    /// Add a new item to the underlying collection.  Returns the new item.
    /// After calling AddNew and changing the new item as desired, either
    /// <seealso cref="CommitNew"/> or <seealso cref="CancelNew"/> should be
    /// called to complete the transaction.
    /// </summary>
    object  AddNew();

    /// <summary>
    /// Complete the transaction started by <seealso cref="AddNew"/>.  The new
    /// item remains in the collection, and the view's sort, filter, and grouping
    /// specifications (if any) are applied to the new item.
    /// </summary>
    void    CommitNew();

    /// <summary>
    /// Complete the transaction started by <seealso cref="AddNew"/>.  The new
    /// item is removed from the collection.
    /// </summary>
    void    CancelNew();

    /// <summary>
    /// Returns true if an <seealso cref="AddNew"/> transaction is in progress.
    /// </summary>
    bool    IsAddingNew { get; }

    /// <summary>
    /// When an <seealso cref="AddNew"/> transaction is in progress, this property
    /// returns the new item.  Otherwise it returns null.
    /// </summary>
    object  CurrentAddItem { get; }

    #endregion Adding new items

    #region Removing items

    /// <summary>
    /// Return true if the view supports <seealso cref="Remove"/> and
    /// <seealso cref="RemoveAt"/>.
    /// </summary>
    bool    CanRemove { get; }

    /// <summary>
    /// Remove the item at the given index from the underlying collection.
    /// The index is interpreted with respect to the view (not with respect to
    /// the underlying collection).
    /// </summary>
    void    RemoveAt(int index);

    /// <summary>
    /// Remove the given item from the underlying collection.
    /// </summary>
    void    Remove(object item);

    #endregion Removing items

    #region Transactional editing of an item

    /// <summary>
    /// Begins an editing transaction on the given item.  The transaction is
    /// completed by calling either <seealso cref="CommitEdit"/> or
    /// <seealso cref="CancelEdit"/>.  Any changes made to the item during
    /// the transaction are considered "pending", provided that the view supports
    /// the notion of "pending changes" for the given item.
    /// </summary>
    void    EditItem(object item);

    /// <summary>
    /// Complete the transaction started by <seealso cref="EditItem"/>.
    /// The pending changes (if any) to the item are committed.
    /// </summary>
    void    CommitEdit();

    /// <summary>
    /// Complete the transaction started by <seealso cref="EditItem"/>.
    /// The pending changes (if any) to the item are discarded.
    /// </summary>
    void    CancelEdit();

    /// <summary>
    /// Returns true if the view supports the notion of "pending changes" on the
    /// current edit item.  This may vary, depending on the view and the particular
    /// item.  For example, a view might return true if the current edit item
    /// implements <seealso cref="IEditableObject"/>, or if the view has special
    /// knowledge about the item that it can use to support rollback of pending
    /// changes.
    /// </summary>
    bool    CanCancelEdit { get; }

    /// <summary>
    /// Returns true if an <seealso cref="EditItem"/> transaction is in progress.
    /// </summary>
    bool    IsEditingItem { get; }

    /// <summary>
    /// When an <seealso cref="EditItem"/> transaction is in progress, this property
    /// returns the affected item.  Otherwise it returns null.
    /// </summary>
    object  CurrentEditItem { get; }

    #endregion Transactional editing of an item
}
}
