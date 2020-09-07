// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: This is the abstract base class for all paginating layouts. 
//              It provides default implementations for the asynchronous 
//              versions of GetPage and ComputePageCount.
//
//

using System.ComponentModel;        // AsyncCompletedEventArgs
using System.Windows.Media;         // Visual
using MS.Internal.PresentationCore; // SR, SRID

namespace System.Windows.Documents 
{
    /// <summary>
    /// This is the abstract base class for all paginating layouts. It 
    /// provides default implementations for the asynchronous versions of 
    /// GetPage and ComputePageCount.
    /// </summary>
    public abstract class DocumentPaginator
    {
        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Retrieves the DocumentPage for the given page number. PageNumber 
        /// is zero-based.
        /// </summary>
        /// <param name="pageNumber">Page number.</param>
        /// <returns>
        /// Returns DocumentPage.Missing if the given page does not exist.
        /// </returns>
        /// <remarks>
        /// Multiple requests for the same page number may return the same 
        /// object (this is implementation specific).
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Throws ArgumentOutOfRangeException if PageNumber is negative.
        /// </exception>
        public abstract DocumentPage GetPage(int pageNumber);

        /// <summary>
        /// Async version of <see cref="DocumentPaginator.GetPage"/>
        /// </summary>
        /// <param name="pageNumber">Page number.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Throws ArgumentOutOfRangeException if PageNumber is negative.
        /// </exception>
        public virtual void GetPageAsync(int pageNumber)
        {
            GetPageAsync(pageNumber, null);
        }

        /// <summary>
        /// Async version of <see cref="DocumentPaginator.GetPage"/>
        /// </summary>
        /// <param name="pageNumber">Page number.</param>
        /// <param name="userState">Unique identifier for the asynchronous task.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Throws ArgumentOutOfRangeException if PageNumber is negative.
        /// </exception>
        public virtual void GetPageAsync(int pageNumber, object userState)
        {
            DocumentPage page;

            // Page number cannot be negative.
            if (pageNumber < 0)
            {
                throw new ArgumentOutOfRangeException("pageNumber", SR.Get(SRID.PaginatorNegativePageNumber));
            }

            page = GetPage(pageNumber);
            OnGetPageCompleted(new GetPageCompletedEventArgs(page, pageNumber, null, false, userState));
        }

        /// <summary>
        /// Computes the number of pages of content. IsPageCountValid will be 
        /// True immediately after this is called.
        /// </summary>
        /// <remarks>
        /// If content is modified or PageSize is changed (or any other change 
        /// that causes a repagination) after this method is called, 
        /// IsPageCountValid will likely revert to False.
        /// </remarks>
        public virtual void ComputePageCount()
        {
            // Force pagination of entire content.
            GetPage(int.MaxValue);
        }

        /// <summary>
        /// Async version of <see cref="DocumentPaginator.ComputePageCount"/>
        /// </summary>
        public virtual void ComputePageCountAsync()
        {
            ComputePageCountAsync(null);
        }

        /// <summary>
        /// Async version of <see cref="DocumentPaginator.ComputePageCount"/>
        /// </summary>
        /// <param name="userState">Unique identifier for the asynchronous task.</param>
        public virtual void ComputePageCountAsync(object userState)
        {
            ComputePageCount();
            OnComputePageCountCompleted(new AsyncCompletedEventArgs(null, false, userState));
        }

        /// <summary>
        /// Cancels all asynchronous calls made with the given userState. 
        /// If userState is NULL, all asynchronous calls are cancelled.
        /// </summary>
        /// <param name="userState">Unique identifier for the asynchronous task.</param>
        public virtual void CancelAsync(object userState)
        {
        }

        #endregion Public Methods

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Whether PageCount is currently valid. If False, then the value of 
        /// PageCount is the number of pages that have currently been formatted.
        /// </summary>
        /// <remarks>
        /// This value may revert to False after being True, in cases where 
        /// PageSize or content changes, forcing a repagination.
        /// </remarks>
        public abstract bool IsPageCountValid { get; }

        /// <summary>
        /// If IsPageCountValid is True, this value is the number of pages 
        /// of content. If False, this is the number of pages that have 
        /// currently been formatted.
        /// </summary>
        /// <remarks>
        /// Value may change depending upon changes in PageSize or content changes.
        /// </remarks>
        public abstract int PageCount { get; }

        /// <summary>
        /// The suggested size for formatting pages.
        /// </summary>
        /// <remarks>
        /// Note that the paginator may override the specified page size. Users 
        /// should check DocumentPage.Size.
        /// </remarks>
        public abstract Size PageSize { get; set; }

        /// <summary>
        /// A pointer back to the element being paginated.
        /// </summary>
        public abstract IDocumentPaginatorSource Source { get; }

        #endregion Public Properties

        //-------------------------------------------------------------------
        //
        //  Public Events
        //
        //-------------------------------------------------------------------

        #region Public Events

        /// <summary>
        /// Fired when a GetPageAsync call has completed.
        /// </summary>
        public event GetPageCompletedEventHandler GetPageCompleted;

        /// <summary>
        /// Fired when a ComputePageCountAsync call has completed.
        /// </summary>
        public event AsyncCompletedEventHandler ComputePageCountCompleted;

        /// <summary>
        /// Fired when one of the properties of a DocumentPage changes. 
        /// Affected pages must be re-fetched if currently used.
        /// </summary>
        /// <remarks>
        /// Existing DocumentPage objects may be destroyed when this 
        /// event is fired.
        /// </remarks>
        public event PagesChangedEventHandler PagesChanged;

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
        /// <param name="e">Event arguments for the GetPageCompleted event.</param>
        protected virtual void OnGetPageCompleted(GetPageCompletedEventArgs e)
        {
            if (this.GetPageCompleted != null)
            {
                this.GetPageCompleted(this, e);
            }
        }

        /// <summary>
        /// Override for subclasses that wish to add logic when this event is fired.
        /// </summary>
        /// <param name="e">Event arguments for the ComputePageCountCompleted event.</param>
        protected virtual void OnComputePageCountCompleted(AsyncCompletedEventArgs e)
        {
            if (this.ComputePageCountCompleted != null)
            {
                this.ComputePageCountCompleted(this, e);
            }
        }

        /// <summary>
        /// Override for subclasses that wish to add logic when this event is fired.
        /// </summary>
        /// <param name="e">Event arguments for the PagesChanged event.</param>
        protected virtual void OnPagesChanged(PagesChangedEventArgs e)
        {
            if (this.PagesChanged != null)
            {
                this.PagesChanged(this, e);
            }
        }

        #endregion Protected Methods
    }
}
