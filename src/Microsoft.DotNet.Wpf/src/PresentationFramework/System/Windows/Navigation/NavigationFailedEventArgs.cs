// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description:
//              This event is fired when an error is encountered during a navigation.
//              The NavigationFailedEventArgs contains the error status code and 
//              the exception that was thrown. By default Handled property is set to false, 
//              which allows the exception to be rethrown. 
//              The event handler can prevent exception from throwing
//              to the user by setting the Handled property to true
//
//              This event is fired on navigation container and refired on the NavigationApplication
//

using System.ComponentModel;
using System.Net;

namespace System.Windows.Navigation
{
    /// <summary>
    /// Event args for NavigationFailed event
    /// The NavigationFailedEventArgs contains the exception that was thrown.
    /// By default Handled property is set to false.
    /// The event handler can prevent the exception from being throwing to the user by setting 
    /// the Handled property to true
    /// </summary>
    public class NavigationFailedEventArgs : EventArgs
    {
        // Internal constructor        
        internal NavigationFailedEventArgs(Uri uri, Object extraData, Object navigator, WebRequest request, WebResponse response, Exception e)
        {
            _uri = uri;
            _extraData = extraData;
            _navigator = navigator;
            _request = request;
            _response = response;
            _exception = e;
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
        /// Exposes extra data object which was optionally passed as a parameter to Navigate.
        /// </summary>
        public Object ExtraData
        {
            get
            {
                return _extraData;
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

        /// <summary>
        /// Exposes the WebRequest used to retrieve content.
        /// </summary>
        public WebRequest WebRequest
        {
            get
            {
                return _request;
            }
        }

        /// <summary>
        /// Exposes the WebResponse used to retrieve content.
        /// </summary>
        public WebResponse WebResponse
        {
            get
            {
                return _response;
            }
        }

        /// <summary>
        /// Exception that was thrown during the navigation
        /// </summary>
        public Exception Exception
        {
            get 
            { 
                return _exception; 
            }
        }

        /// <summary>
        /// Returns a boolean flag indicating if or not this event has been handled.
        /// </summary>
        public bool Handled
        {
            get
            {
                return _handled;
            }
            set
            {
                _handled = value;
            }
        }

        Uri _uri;
        Object _extraData;
        Object _navigator;
        WebRequest _request;
        WebResponse _response;
        Exception _exception;
        bool _handled = false;
    }
}