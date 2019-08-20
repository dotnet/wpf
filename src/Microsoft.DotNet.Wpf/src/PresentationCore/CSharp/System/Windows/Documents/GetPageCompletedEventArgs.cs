// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: GetPageCompleted event.
//
//

using System.ComponentModel;        // AsyncCompletedEventArgs

namespace System.Windows.Documents 
{
    /// <summary>
    /// GetPageCompleted event handler.
    /// </summary>
    public delegate void GetPageCompletedEventHandler(object sender, GetPageCompletedEventArgs e);

    /// <summary>
    /// Event arguments for the GetPageCompleted event.
    /// </summary>
    public class GetPageCompletedEventArgs : AsyncCompletedEventArgs
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="page">The DocumentPage object for the requesed Page.</param>
        /// <param name="pageNumber">The page number of the returned page.</param>
        /// <param name="error">Error occurred during an asynchronous operation.</param>
        /// <param name="cancelled">Whether an asynchronous operation has been cancelled.</param>
        /// <param name="userState">Unique identifier for the asynchronous task.</param>
        public GetPageCompletedEventArgs(DocumentPage page, int pageNumber, Exception error, bool cancelled, object userState)
            :
            base(error, cancelled, userState)
        {
            _page = page;
            _pageNumber = pageNumber;
        }

        /// <summary>
        /// The DocumentPage object for the requesed Page.
        /// </summary>
        public DocumentPage DocumentPage
        {
            get
            {
                // Raise an exception if the operation failed or was cancelled.
                this.RaiseExceptionIfNecessary(); 
                return _page;
            }
        }

        /// <summary>
        /// The page number of the returned page.
        /// </summary>
        public int PageNumber
        {
            get
            {
                // Raise an exception if the operation failed or was cancelled.
                this.RaiseExceptionIfNecessary(); 
                return _pageNumber;
            }
        }

        /// <summary>
        /// The DocumentPage object for the requesed Page.
        /// </summary>
        private readonly DocumentPage _page;

        /// <summary>
        /// The page number of the returned page.
        /// </summary>
        private readonly int _pageNumber;
    }
}
