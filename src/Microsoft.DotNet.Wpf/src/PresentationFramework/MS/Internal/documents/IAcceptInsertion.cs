// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿
namespace MS.Internal.Documents
{
    /// <summary>
    /// This interface should be implemented by <see cref="System.Windows.Documents.Table"/> related types
    /// which can hold collection of other Table related objects.
    /// to provide insertion index where item will be inserted.
    /// Refer to <see cref="IIndexedChild<TParent>"/> for additional information.
    /// For an example of usage see <see cref="TableTextElementCollectionInternal"/>
    /// </summary>
    internal interface IAcceptInsertion
    {
        /// <summary>
        /// Provides value for the index where new item will be inserted.
        /// </summary>
        int InsertionIndex
        { get; set; }
    }
}
