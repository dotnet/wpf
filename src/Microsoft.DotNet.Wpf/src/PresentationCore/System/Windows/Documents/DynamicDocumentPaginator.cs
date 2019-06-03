// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: Defines advanced methods and properties for paginating layouts, 
//              such as background pagination and methods for tracking content 
//              positions across repaginations.
//
//

using System.ComponentModel;        // AsyncCompletedEventArgs
using MS.Internal.PresentationCore; // SR, SRID

namespace System.Windows.Documents 
{
    /// <summary>
    /// Defines advanced methods and properties for paginating layouts, such 
    /// as background pagination and methods for tracking content positions 
    /// across repaginations.
    /// </summary>
    public abstract class DynamicDocumentPaginator : DocumentPaginator
    {
        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Returns the page number on which the ContentPosition appears.
        /// </summary>
        /// <param name="contentPosition">Content position.</param>
        /// <returns>
        /// Returns the page number on which the ContentPosition appears.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Throws ArgumentException if the ContentPosition does not exist within 
        /// this element's tree.
        /// </exception>
        public abstract int GetPageNumber(ContentPosition contentPosition);

        /// <summary>
        /// Async version of <see cref="DynamicDocumentPaginator.GetPageNumber"/>
        /// </summary>
        /// <param name="contentPosition">Content position.</param>
        /// <exception cref="ArgumentException">
        /// Throws ArgumentException if the ContentPosition does not exist within 
        /// this element's tree.
        /// </exception>
        public virtual void GetPageNumberAsync(ContentPosition contentPosition)
        {
            GetPageNumberAsync(contentPosition, null);
        }

        /// <summary>
        /// Async version of <see cref="DynamicDocumentPaginator.GetPageNumber"/>
        /// </summary>
        /// <param name="contentPosition">Content position.</param>
        /// <param name="userState">Unique identifier for the asynchronous task.</param>
        /// <exception cref="ArgumentException">
        /// Throws ArgumentException if the ContentPosition does not exist within 
        /// this element’s tree.
        /// </exception>
        public virtual void GetPageNumberAsync(ContentPosition contentPosition, object userState)
        {
            int pageNumber;

            // Content position cannot be null.
            if (contentPosition == null)
            {
                throw new ArgumentNullException("contentPosition");
            }
            // Content position cannot be Missing.
            if (contentPosition == ContentPosition.Missing)
            {
                throw new ArgumentException(SR.Get(SRID.PaginatorMissingContentPosition), "contentPosition");
            }

            pageNumber = GetPageNumber(contentPosition);
            OnGetPageNumberCompleted(new GetPageNumberCompletedEventArgs(contentPosition, pageNumber, null, false, userState));
        }

        /// <summary>
        /// Returns the ContentPosition for the given page.
        /// </summary>
        /// <param name="page">Document page.</param>
        /// <returns>Returns the ContentPosition for the given page.</returns>
        /// <exception cref="ArgumentException">
        /// Throws ArgumentException if the page is not valid.
        /// </exception>
        public abstract ContentPosition GetPagePosition(DocumentPage page);

        /// <summary>
        /// Returns the ContentPosition for an object within the content.
        /// </summary>
        /// <param name="value">Object within this element's tree.</param>
        /// <returns>Returns the ContentPosition for an object within the content.</returns>
        /// <exception cref="ArgumentException">
        /// Throws ArgumentException if the object does not exist within this element's tree.
        /// </exception>
        public abstract ContentPosition GetObjectPosition(Object value);

        #endregion Public Methods

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Whether content is paginated in the background. 
        /// When True, the Paginator will paginate its content in the background, 
        /// firing the PaginationCompleted and PaginationProgress events as appropriate. 
        /// Background pagination begins immediately when set to True. If the 
        /// PageSize is modified and this property is set to True, then all pages 
        /// will be repaginated and existing pages may be destroyed. 
        /// The default value is False.
        /// </summary>
        public virtual bool IsBackgroundPaginationEnabled
        {
            get { return false; }
            set { }
        }

        #endregion Public Properties

        //-------------------------------------------------------------------
        //
        //  Public Events
        //
        //-------------------------------------------------------------------

        #region Public Events

        /// <summary>
        /// Fired when a GetPageNumberAsync call has completed.
        /// </summary>
        public event GetPageNumberCompletedEventHandler GetPageNumberCompleted;

        /// <summary>
        /// Fired when all document content has been paginated. After this event 
        /// IsPageCountValid will be True.
        /// </summary>
        public event EventHandler PaginationCompleted;

        /// <summary>
        /// Fired when background pagination is enabled, indicating which pages 
        /// have been formatted and are available.
        /// </summary>
        public event PaginationProgressEventHandler PaginationProgress;

        #endregion Public Events

        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Override for subclasses that wish to add logic when this event is fired.
        /// </summary>
        /// <param name="e">Event arguments for the GetPageNumberCompleted event.</param>
        protected virtual void OnGetPageNumberCompleted(GetPageNumberCompletedEventArgs e)
        {
            if (this.GetPageNumberCompleted != null)
            {
                this.GetPageNumberCompleted(this, e);
            }
        }

        /// <summary>
        /// Override for subclasses that wish to add logic when this event is fired.
        /// </summary>
        /// <param name="e">Event arguments for the PaginationProgress event.</param>
        protected virtual void OnPaginationProgress(PaginationProgressEventArgs e)
        {
            if (this.PaginationProgress != null)
            {
                this.PaginationProgress(this, e);
            }
        }

        /// <summary>
        /// Override for subclasses that wish to add logic when this event is fired.
        /// </summary>
        /// <param name="e">Event arguments for the PaginationCompleted event.</param>
        protected virtual void OnPaginationCompleted(EventArgs e)
        {
            if (this.PaginationCompleted != null)
            {
                this.PaginationCompleted(this, e);
            }
        }

        #endregion Protected Methods
    }
}
