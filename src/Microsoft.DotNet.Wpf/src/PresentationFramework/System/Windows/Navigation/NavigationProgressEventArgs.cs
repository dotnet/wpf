// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description:
//              This event is fired when a navigation is in progress. It is fired for 
//              every chunk of 1024 bytes read. 
//              This event is fired on INavigator and refired on the NavigationWindow 
//              and Application. When the event is re-fired on the 
//              NavigationWindow, the bytesRead and maxBytes are the cumulative
//              totals of all navigations in progress in that window. The uri is the
//              uri that is contributing to this event, for frame level this is the frame's
//              uri, for window level it is the INavigator's Uri which received this
//              notification from the Loader
//

namespace System.Windows.Navigation
{
    /// <summary>
    /// Event args for the NavigationProgress event. 
    /// The NavigationProgressEventArgs tell how many total bytes need to be downloaded and 
    /// how many have been sent at the moment the event is fired. 
    /// </summary>
    public class NavigationProgressEventArgs : EventArgs
    {
        // Internal constructor
        // <param name="uri">URI of the markup page to navigate to.</param>
        // <param name="bytesRead">The number of bytes that have already been downloaded.</param>
        // <param name="maxBytes">The maximum number of bytes to be downloaded.</param>
        // <param name="Navigator">navigator that raised this event</param>
        internal NavigationProgressEventArgs(Uri uri, long bytesRead, long maxBytes, object Navigator)
        {
            _uri = uri;
            _bytesRead = bytesRead;
            _maxBytes = maxBytes;
            _navigator = Navigator;
        }

        /// <summary>
        /// URI of the markup page to navigate to.
        /// </summary>
        public Uri Uri
        {
            get
            {
                return _uri;
            }
        }

        /// <summary>
        /// The number of bytes that have already been downloaded.
        /// </summary>
        public long BytesRead
        {
            get
            {
                return _bytesRead;
            }
        }

        /// <summary>
        /// The maximum number of bytes to be downloaded.
        /// </summary>
        public long MaxBytes
        {
            get
            {
                return _maxBytes;
            }
        }

        /// <summary>
        /// The navigator that raised this event
        /// </summary>
        public object Navigator
        {
            get
            {
                return _navigator;
            }
        }
        Uri _uri;
        long _bytesRead;
        long _maxBytes;
        object _navigator;
    }
}
