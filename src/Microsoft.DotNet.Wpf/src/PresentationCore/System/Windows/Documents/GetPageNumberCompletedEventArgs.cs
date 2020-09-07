// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: GetPageNumberCompleted event.
//
//

using System.ComponentModel;        // AsyncCompletedEventArgs

namespace System.Windows.Documents 
{
    /// <summary>
    /// GetPageNumberCompleted event handler.
    /// </summary>
    public delegate void GetPageNumberCompletedEventHandler(object sender, GetPageNumberCompletedEventArgs e);

    /// <summary>
    /// Event arguments for the GetPageNumberCompleted event.
    /// </summary>
    public class GetPageNumberCompletedEventArgs : AsyncCompletedEventArgs
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="contentPosition">The parameter passed into the GetPageNumberAsync call.</param>
        /// <param name="pageNumber">The first page number on which the element appears.</param>
        /// <param name="error">Error occurred during an asynchronous operation.</param>
        /// <param name="cancelled">Whether an asynchronous operation has been cancelled.</param>
        /// <param name="userState">Unique identifier for the asynchronous task.</param>
        public GetPageNumberCompletedEventArgs(ContentPosition contentPosition, int pageNumber, Exception error, bool cancelled, object userState)
            :
            base(error, cancelled, userState)
        {
            _contentPosition = contentPosition;
            _pageNumber = pageNumber;
        }

        /// <summary>
        /// The parameter passed into the GetPageNumberAsync call.
        /// </summary>
        public ContentPosition ContentPosition
        {
            get
            {
                // Raise an exception if the operation failed or was cancelled.
                this.RaiseExceptionIfNecessary();
                return _contentPosition;
            }
        }

        /// <summary>
        /// The first page number on which the element appears.
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
        /// The parameter passed into the GetPageNumberAsync call.
        /// </summary>
        private readonly ContentPosition _contentPosition;

        /// <summary>
        /// The first page number on which the element appears.
        /// </summary>
        private readonly int _pageNumber;
    }
}
