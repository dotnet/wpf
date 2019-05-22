// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿
using System.Windows.Documents;

namespace MS.Internal.Documents
{
    /// <summary>
    /// Classes comprising structure of <see cref="Table"/> are <see cref="TextElement"/> derived 
    /// classes, which are in parent-child containment relationship with each other.
    /// For example TableRow contains collection of rows, each Row in turn contains collection of cells.
    /// This interface should be implemented by the child types described above. see example section.
    /// And Parent types should impelement <see cref="IAcceptInsertion"/>
    /// <seealso cref="ContentElementCollection<TParent, TItem>"/>
    /// </summary>
    /// <example>
    /// <see cref="TableCell"/> implements IIndexedChild with TParent == TableRow
    /// whcih means cells are contained in a collection owned by <see cref="TableRow"/> type.
    /// And <see cref="TableRow"/> in turn implements <see cref="IAcceptInsertion"/>
    ///
    /// Note that <see cref="TableRow"/> also implements IIndexedChild with parent <see cref="TableRowGroup"/>
    /// whcih means TableRow itself is contained in a collection owned by TableRowGroup.
    /// </example>
    /// <typeparam name="TParent"></typeparam>
    internal interface IIndexedChild<TParent>
        where TParent : TextElement
    {
        /// <summary>
        /// Callback used to notify about entering parent's collection.
        /// </summary>
        void OnEnterParentTree();
        /// <summary>
        /// Callback used to notify about exitting parent's collection.
        /// </summary>
        void OnExitParentTree();

        /// <summary>
        /// Callback used to notify the RowGroup about exitting model tree.
        /// </summary>
        void OnAfterExitParentTree(TParent parent);

        /// <summary>
        /// Index of this object in the parent's collection.
        /// </summary>
        int Index
        { get; set; }
    }
}
