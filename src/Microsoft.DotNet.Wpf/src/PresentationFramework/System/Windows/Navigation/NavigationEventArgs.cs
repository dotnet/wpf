// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description:
//              This event is fired when a navigation is completed. 
//              This event is fired on INavigator and refired on the Application
// 

using System.Net;

namespace System.Windows.Navigation
{
    /// <summary>
    /// Event args for non-cancelable navigation events - Navigated, LoadCompleted, NavigationStopped
    /// The NavigationEventArgs contain the uri or root element of the content being navigated to, 
    /// and a IsNavigationInitiator property that indicates whether this is a new navigation initiated 
    /// by this navigator, or whether this navigation is being propagated down from a higher level navigation 
    /// taking place in a containing window or frame. 
    /// The developer should check the IsNavigationInitiator property on the NavigationEventArgs to 
    /// determine whether to spin the globe. 
    /// </summary>
    public class NavigationEventArgs : EventArgs
    {
        // Internal constructor
        // <param name="uri">URI of the content navigated to.</param>
        // <param name="content">Root of the element tree being navigated to.</param>
        // <param name="isNavigationInitiator">Indicates whether this navigator is 
        //        initiating the navigation or whether a parent </param>
        internal NavigationEventArgs(Uri uri, Object content, Object extraData, WebResponse response, object Navigator, bool isNavigationInitiator)
        {
            _uri = uri;
            _content = content;
            _extraData = extraData;
            _webResponse = response;
            _isNavigationInitiator = isNavigationInitiator;
            _navigator = Navigator;
        }

        /// <summary>
        /// URI of the markup page navigated to.
        /// </summary>
        public Uri Uri
        {
            get
            {
                return _uri;
            }
        }

        /// <summary>
        /// Root of the element tree navigated to.
        /// Note: Only one of the Content or Uri property will be set, depending on whether the 
        /// navigation was to a Uri or an existing element tree.
        /// </summary>
        public Object Content
        {
            get
            {
                return _content;
            }
        }

        /// <summary>
        /// Indicates whether this navigator is initiating the navigation or whether a parent 
        /// navigator is being navigated (e.g., the current navigator is a frame 
        /// inside a page thats being navigated to inside a parent navigator). A developer 
        /// can use this property to determine whether to spin the globe on a Navigating event or 
        /// to stop spinning the globe on a LoadCompleted event. 
        /// If this property is False, the navigators parent navigator is also navigating and 
        /// the globe is already spinning. 
        /// If this property is True, the navigation was initiated inside the current frame and 
        /// the developer should spin the globe (or stop spinning the globe, depending on 
        /// which event is being handled.)
        /// </summary>
        public bool IsNavigationInitiator
        {
            get
            {
                return _isNavigationInitiator;
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
        /// Exposes the web response to allow access to HTTP headers and other properties.
        /// </summary>
        public WebResponse WebResponse
        {
            get
            {
                return _webResponse;
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

        private Uri _uri;
        private Object _content;
        private Object _extraData;
        private WebResponse _webResponse;
        private bool _isNavigationInitiator;
        object _navigator;
    }
}
