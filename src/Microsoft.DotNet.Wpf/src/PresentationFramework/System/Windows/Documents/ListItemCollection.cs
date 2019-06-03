// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: Collection of ListItem elements
//

namespace System.Windows.Documents
{
    using MS.Internal; // Invariant

    /// <summary>
    /// Collection of ListItem elements
    /// </summary>
    public class ListItemCollection : TextElementCollection<ListItem>
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        // This kind of collection is suposed to be created by owning List elements only.
        // Note that when a SiblingListItems collection is created for a ListItem, the owner of collection is that member ListItem object.
        // Flag isOwnerParent indicates whether owner is a parent or a member of the collection.
        internal ListItemCollection(DependencyObject owner, bool isOwnerParent)
            : base(owner, isOwnerParent)
        {
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <value>
        /// Returns a first ListItem of this collection
        /// </value>
        public ListItem FirstListItem
        {
            get
            {
                return this.FirstChild;
            }
        }

        /// <value>
        /// Returns a last ListItem of this collection
        /// </value>
        public ListItem LastListItem
        {
            get
            {
                return this.LastChild;
            }
        }

        #endregion Public Properties
    }
}
