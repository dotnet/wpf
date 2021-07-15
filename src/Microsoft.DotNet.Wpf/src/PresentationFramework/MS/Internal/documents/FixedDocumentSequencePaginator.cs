// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: This is the abstract base class for all paginating layouts. 
//              It provides default implementations for the asynchronous 
//              versions of GetPage and ComputePageCount.
//

using System;                       // IServiceProvider
using System.ComponentModel;        // AsyncCompletedEventArgs
using System.Windows;               // Size
using System.Windows.Documents;     // DocumentPaginator
using System.Windows.Media;         // Visual

namespace MS.Internal.Documents
{
    /// <summary>
    /// This is the abstract base class for all paginating layouts. It 
    /// provides default implementations for the asynchronous versions of 
    /// GetPage and ComputePageCount.
    /// </summary>
    internal class FixedDocumentSequencePaginator : DynamicDocumentPaginator, IServiceProvider
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        internal FixedDocumentSequencePaginator(FixedDocumentSequence document)
        {
            _document = document;
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// <see cref="DocumentPaginator.GetPage"/>
        /// </summary>
        public override DocumentPage GetPage(int pageNumber)
        {
            return _document.GetPage(pageNumber);
        }

        /// <summary>
        /// <see cref="DocumentPaginator.GetPageAsync(int,object)"/>
        /// </summary>
        public override void GetPageAsync(int pageNumber, object userState)
        {
            _document.GetPageAsync(pageNumber, userState);
        }

        /// <summary>
        /// <see cref="DocumentPaginator.CancelAsync"/>
        /// </summary>
        public override void CancelAsync(object userState)
        {
            _document.CancelAsync(userState);
        }

        /// <summary>
        /// <see cref="DynamicDocumentPaginator.GetPageNumber"/>
        /// </summary>
        public override int GetPageNumber(ContentPosition contentPosition)
        {
            return _document.GetPageNumber(contentPosition);
        }

        /// <summary>
        /// <see cref="DynamicDocumentPaginator.GetPagePosition"/>
        /// </summary>
        public override ContentPosition GetPagePosition(DocumentPage page)
        {
            return _document.GetPagePosition(page);
        }

        /// <summary>
        /// <see cref="DynamicDocumentPaginator.GetObjectPosition"/>
        /// </summary>
        public override ContentPosition GetObjectPosition(Object o)
        {
            return _document.GetObjectPosition(o);
        }

        #endregion Public Methods

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// <see cref="DocumentPaginator.IsPageCountValid"/>
        /// </summary>
        public override bool IsPageCountValid
        {
            get { return _document.IsPageCountValid; }
        }

        /// <summary>
        /// <see cref="DocumentPaginator.PageCount"/>
        /// </summary>
        public override int PageCount
        {
            get { return _document.PageCount; }
        }

        /// <summary>
        /// <see cref="DocumentPaginator.PageSize"/>
        /// </summary>
        public override Size PageSize
        {
            get { return _document.PageSize; }
            set { _document.PageSize = value; }
        }

        /// <summary>
        /// <see cref="DocumentPaginator.Source"/>
        /// </summary>
        public override IDocumentPaginatorSource Source
        {
            get { return _document; }
        }

        #endregion Public Properties

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        internal void NotifyGetPageCompleted(GetPageCompletedEventArgs e)
        {
            OnGetPageCompleted(e);
        }

        internal void NotifyPaginationCompleted(EventArgs e)
        {
            OnPaginationCompleted(e);
        }

        internal void NotifyPaginationProgress(PaginationProgressEventArgs e)
        {
            OnPaginationProgress(e);
        }

        internal void NotifyPagesChanged(PagesChangedEventArgs e)
        {
            OnPagesChanged(e);
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        private readonly FixedDocumentSequence _document;

        #endregion Private Fields

        //-------------------------------------------------------------------
        //
        //  IServiceProvider Members
        //
        //-------------------------------------------------------------------

        #region IServiceProvider Members

        /// <summary>
        /// Returns service objects associated with this control.
        /// </summary>
        /// <param name="serviceType">Specifies the type of service object to get.</param>
        object IServiceProvider.GetService(Type serviceType)
        {
            return ((IServiceProvider)_document).GetService(serviceType);
        }

        #endregion IServiceProvider Members
    }
}
