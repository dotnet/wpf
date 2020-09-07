// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: Manages a view of a collection of data items.
//
// See spec at http://avalon/connecteddata/Specs/CollectionView.mht
//
//

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;

namespace System.ComponentModel
{
/// <summary>
/// ICollectionView is an interface that applications writing their own
/// collections can implement to enable current record management, sorting,
/// filtering, grouping etc in a custom way.
/// </summary>
public interface ICollectionView : IEnumerable, INotifyCollectionChanged
{
    /// <summary>
    /// Culture contains the CultureInfo used in any operations of the
    /// ICollectionView that may differ by Culture, such as sorting.
    /// </summary>
    CultureInfo Culture { get; set; }

    /// <summary>
    /// Return true if the item belongs to this view.  No assumptions are
    /// made about the item. This method will behave similarly to IList.Contains().
    /// If the caller knows that the item belongs to the
    /// underlying collection, it is more efficient to call <seealso cref="Filter"/>.
    /// </summary>
    bool Contains (object item);

    /// <summary>
    /// SourceCollection is the original un-filtered collection of which
    /// this ICollectionView is a view.
    /// </summary>
    IEnumerable SourceCollection { get; }

    /// <summary>
    /// Filter is a callback set by the consumer of the ICollectionView
    /// and used by the implementation of the ICollectionView to determine if an
    /// item is suitable for inclusion in the view.
    /// </summary>
    Predicate<object> Filter{ get; set; }

    /// <summary>
    /// Indicates whether or not this ICollectionView can do any filtering.
    /// </summary>
    bool CanFilter { get; }

    /// <summary>
    /// Collection of Sort criteria to sort items in this view over the SourceCollection.
    /// </summary>
    /// <remarks>
    /// <p>
    /// Simpler implementations do not support sorting and will return an empty
    /// and immutable / read-only SortDescription collection.
    /// Attempting to modify such a collection will cause NotSupportedException.
    /// Use <seealso cref="CanSort"/> property on CollectionView to test if sorting is supported
    /// before modifying the returned collection.
    /// </p>
    /// <p>
    /// One or more sort criteria in form of <seealso cref="SortDescription"/>
    /// can be added, each specifying a property and direction to sort by.
    /// </p>
    /// </remarks>
    SortDescriptionCollection SortDescriptions { get; }

    /// <summary>
    /// Whether or not this ICollectionView does any sorting.
    /// </summary>
    bool CanSort { get; }

    /// <summary>
    /// Returns true if this view really supports grouping.
    /// When this returns false, the rest of the interface is ignored.
    /// </summary>
    bool CanGroup { get; }

    /// <summary>
    /// The description of grouping, indexed by level.
    /// </summary>
    ObservableCollection<GroupDescription> GroupDescriptions { get; }

    /// <summary>
    /// The top-level groups, constructed according to the descriptions
    /// given in GroupDescriptions.
    /// </summary>
    ReadOnlyObservableCollection<object> Groups { get; }

    /// <summary>
    /// Returns true if the resulting (filtered) view is emtpy.
    /// </summary>
    bool IsEmpty { get; }

    /// <summary>
    /// Re-create the view, using any <seealso cref="SortDescriptions"/>.
    /// </summary>
    void Refresh();

    /// <summary>
    /// Enter a Defer Cycle.
    /// Defer cycles are used to coalesce changes to the ICollectionView.
    /// </summary>
    IDisposable DeferRefresh();

    // CurrentItem

    /// <summary>
    /// Return current item.
    /// </summary>
    object CurrentItem { get; }

    /// <summary>
    /// The ordinal position of the <seealso cref="CurrentItem"/> within the (optionally
    /// sorted and filtered) view.
    /// </summary>
    int CurrentPosition { get; }

    /// <summary>
    /// Return true if <seealso cref="CurrentItem"/> is beyond the end (End-Of-File).
    /// </summary>
    bool IsCurrentAfterLast { get; }

    /// <summary>
    /// Return true if <seealso cref="CurrentItem"/> is before the beginning (Beginning-Of-File).
    /// </summary>
    bool IsCurrentBeforeFirst { get; }

    /// <summary>
    /// Move <seealso cref="CurrentItem"/> to the first item.
    /// </summary>
    /// <returns>true if <seealso cref="CurrentItem"/> points to an item within the view.</returns>
    bool MoveCurrentToFirst();

    /// <summary>
    /// Move <seealso cref="CurrentItem"/> to the last item.
    /// </summary>
    /// <returns>true if <seealso cref="CurrentItem"/> points to an item within the view.</returns>
    bool MoveCurrentToLast();

    /// <summary>
    /// Move <seealso cref="CurrentItem"/> to the next item.
    /// </summary>
    /// <returns>true if <seealso cref="CurrentItem"/> points to an item within the view.</returns>
    bool MoveCurrentToNext();

    /// <summary>
    /// Move <seealso cref="CurrentItem"/> to the previous item.
    /// </summary>
    /// <returns>true if <seealso cref="CurrentItem"/> points to an item within the view.</returns>
    bool MoveCurrentToPrevious();

    /// <summary>
    /// Move <seealso cref="CurrentItem"/> to the given item.
    /// </summary>
    /// <param name="item">Move CurrentItem to this item.</param>
    /// <returns>true if <seealso cref="CurrentItem"/> points to an item within the view.</returns>
    bool MoveCurrentTo( object item );

    /// <summary>
    /// Move <seealso cref="CurrentItem"/> to the item at the given index.
    /// </summary>
    /// <param name="position">Move CurrentItem to this index</param>
    /// <returns>true if <seealso cref="CurrentItem"/> points to an item within the view.</returns>
    bool MoveCurrentToPosition( int position);



    /// <summary>
    /// Raise this event before change of current item pointer.  Handlers can cancel the change.
    /// </summary>
    ///<remarks>
    /// <p>Classes implementing ICollectionView should use the following pattern:</p>
    /// <p>
    /// Raise the CurrentChanging event before any change of currency and check the
    /// return value before proceeding and raising CurrentChanged event:
    /// <code>
    ///      void MoveCurrentToNext()
    ///      {
    ///          CurrentChangingEventArgs args = new CurrentChangingEventArgs();
    ///          OnCurrentChanging(args);
    ///          if (!args.Cancel)
    ///          {
    ///              // ... update private data structures ...
    ///              CurrentChanged();
    ///          }
    ///      }
    /// </code>
    /// </p>
    ///</remarks>
    event CurrentChangingEventHandler CurrentChanging;

    /// <summary>
    /// Raise this event after changing to a new current item.
    /// </summary>
    event EventHandler  CurrentChanged;
}
}
