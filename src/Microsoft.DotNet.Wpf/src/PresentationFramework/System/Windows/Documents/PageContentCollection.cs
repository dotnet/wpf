// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      Implements the PageContentCollection element
//

namespace System.Windows.Documents
{
    using System;
    using System.Collections.Generic;
    using System.Collections;
    using System.Diagnostics;
    using System.Windows.Markup;


    //=====================================================================
    /// <summary>
    /// PageContentCollection is an ordered collection of PageContent 
    /// </summary>
    public sealed class PageContentCollection : IEnumerable<PageContent>
    {
        //--------------------------------------------------------------------
        //
        // Connstructors
        //
        //---------------------------------------------------------------------

        #region Constructors
        internal PageContentCollection(FixedDocument logicalParent)
        {
            _logicalParent  = logicalParent;
            _internalList   = new List<PageContent>();
        }
        #endregion Constructors
        
        //--------------------------------------------------------------------
        //
        // Public Methods
        //
        //---------------------------------------------------------------------
        
        #region Public Methods
        /// <summary>
        /// Append a new PageContent to end of this collection
        /// </summary>
        /// <param name="newPageContent">New PageContent to be appended</param>
        /// <returns>The index this new PageContent is appended at</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the argument is null
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the page has been added to a PageContentCollection previously
        /// </exception>
        public int Add(PageContent newPageContent)
        {
            if (newPageContent == null)
            {
                throw new ArgumentNullException("newPageContent");
            }

            _logicalParent.AddLogicalChild(newPageContent);

            InternalList.Add(newPageContent);
            int index = InternalList.Count - 1;
            _logicalParent.OnPageContentAppended(index);
            return index;
        }

        #endregion Public Methods

        #region IEnumerable

        /// <summary>
        /// <!-- see cref="System.Collections.Generic.IEnumerable&lt;&gt;.GetEnumerator" / -->
        /// </summary>
        public IEnumerator<PageContent> GetEnumerator()
        {
            return InternalList.GetEnumerator();          
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<PageContent>)this).GetEnumerator();
        }

        #endregion IEnumerable

        //--------------------------------------------------------------------
        //
        // Public Properties
        //
        //---------------------------------------------------------------------

        #region Public Properties
        /// <summary>
        /// Indexer to retrieve individual PageContent contained within this collection
        /// </summary>
        public PageContent this[int pageIndex]
        {
            get
            {
                return InternalList[pageIndex];
            }
        }

        /// <summary>
        /// Number of PageContent items in this collection
        /// </summary>

        public int Count
        {
            get { return InternalList.Count; }
        }
        
        #endregion Public Properties

        //--------------------------------------------------------------------
        //
        // Public Events
        //
        //---------------------------------------------------------------------

        #region Public Event
        #endregion Public Event

        //--------------------------------------------------------------------
        //
        // Internal Methods
        //
        //---------------------------------------------------------------------

        #region Internal Methods

        internal int IndexOf(PageContent pc)
        {
            return InternalList.IndexOf(pc);
        }

        #endregion Internal Methods

        //--------------------------------------------------------------------
        //
        // Internal Properties
        //
        //---------------------------------------------------------------------


        //--------------------------------------------------------------------
        //
        // private Properties
        //
        //---------------------------------------------------------------------

        #region Private Properties
        
        // Aggregated IList
        private IList<PageContent> InternalList
        {
            get
            {
                return _internalList;
            }
        }
        #endregion Private Properties


        //--------------------------------------------------------------------
        //
        // Private Methods
        //
        //---------------------------------------------------------------------

        #region Private Methods
        #endregion Private Methods

        //--------------------------------------------------------------------
        //
        // Private Fields
        //
        //---------------------------------------------------------------------

        #region Private Fields
        private FixedDocument  _logicalParent;
        private List<PageContent> _internalList;
        #endregion Private Fields
    }
}
