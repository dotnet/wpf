// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: Live shaping functionality for collection views.
//

using System;
using System.Collections.ObjectModel;

namespace System.ComponentModel
{
    ///<summary>
    /// A collection view can implement the ICollectionViewLiveShaping interface if it
    /// supports "live shaping" - the incremental recomputation of sorting, filtering,
    /// and/or grouping after a relevant property value changes.
    ///</summary>
    ///<notes>
    /// A collection view may implement shaping operations itself, or may delegate
    /// one or more of them to another object (such as the underlying collection itself).
    /// In the latter case, the view may have no control over whether the shaping is
    /// live or not;  it must accept the behavior of the object to which it delegates.
    /// Such a view should set its "CanChange..." properties to false.
    /// If it knows whether its delegate does live shaping, it can set its IsLive...
    /// properties to the known value;  otherwise it should set its IsLive...
    /// properties to null.
    ///</notes>
    public interface ICollectionViewLiveShaping
    {
        ///<summary>
        /// Gets a value that indicates whether this view supports turning live sorting on or off.
        ///</summary>
        bool CanChangeLiveSorting { get; }

        ///<summary>
        /// Gets a value that indicates whether this view supports turning live filtering on or off.
        ///</summary>
        bool CanChangeLiveFiltering { get; }

        ///<summary>
        /// Gets a value that indicates whether this view supports turning live grouping on or off.
        ///</summary>
        bool CanChangeLiveGrouping { get; }


        ///<summary>
        /// Gets or sets a value that indicates whether live sorting is enabled.
        /// The value may be null if the view does not know whether live sorting is enabled.
        /// Calling the setter when CanChangeLiveSorting is false will throw an
        /// InvalidOperationException.
        ///</summary
        bool? IsLiveSorting { get; set; }

        ///<summary>
        /// Gets or sets a value that indicates whether live filtering is enabled.
        /// The value may be null if the view does not know whether live filtering is enabled.
        /// Calling the setter when CanChangeLiveFiltering is false will throw an
        /// InvalidOperationException.
        ///</summary>
        bool? IsLiveFiltering { get; set; }

        ///<summary>
        /// Gets or sets a value that indicates whether live grouping is enabled.
        /// The value may be null if the view does not know whether live grouping is enabled.
        /// Calling the setter when CanChangeLiveGrouping is false will throw an
        /// InvalidOperationException.
        ///</summary>
        bool? IsLiveGrouping { get; set; }


        ///<summary>
        /// Gets a collection of strings describing the properties that
        /// trigger a live-sorting recalculation.
        /// The strings use the same format as SortDescription.PropertyName.
        ///</summary>
        ///<notes>
        /// When this collection is empty, the view will use the PropertyName strings
        /// from its SortDescriptions.
        ///
        /// This collection is useful when sorting is described code supplied
        /// by the application  (e.g. ListCollectionView.CustomSort).
        /// In this case the view does not know which properties the code examines;
        /// the application should tell the view by adding the relevant properties
        /// to the LiveSortingProperties collection.
        ///</notes>
        ObservableCollection<string> LiveSortingProperties { get; }

        ///<summary>
        /// Gets a collection of strings describing the properties that
        /// trigger a live-filtering recalculation.
        /// The strings use the same format as SortDescription.PropertyName.
        ///</summary>
        ///<notes>
        /// Filtering is described by a Predicate.  The view does not
        /// know which properties the Predicate examines;  the application should
        /// tell the view by adding the relevant properties to the LiveFilteringProperties
        /// collection.
        ///</notes>
        ObservableCollection<string> LiveFilteringProperties { get; }

        ///<summary>
        /// Gets a collection of strings describing the properties that
        /// trigger a live-grouping recalculation.
        /// The strings use the same format as PropertyGroupDescription.PropertyName.
        ///</summary>
        ///<notes>
        /// When this collection is empty, the view will use the PropertyName strings
        /// from its GroupDescriptions.
        ///
        /// This collection is useful when grouping is described code supplied
        /// by the application (e.g. PropertyGroupDescription.Converter).
        /// In this case the view does not know which properties the code examines;
        /// the application should tell the view by adding the relevant properties
        /// to the LiveGroupingProperties collection.
        ///</notes>
        ObservableCollection<string> LiveGroupingProperties { get; }
    }
}
