// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//              This event is fired before we start a navigation. The NavigationMode
//              property specifies the type of navigation (normal, refresh, journal)
//              This event is fired on INavigator and refired on the Application
// 
using System.ComponentModel;
using System.Net;

namespace System.Windows.Navigation
{
    /// <summary>
    /// Event args for Navigating event.
    /// The NavigatingCancelEventArgs contain the uri or root element of the content being navigated to 
    /// and an enum value that indicates the type of navigation. Canceling this event prevents the 
    /// application from navigating. By default, Cancel is set to false.
    /// Note: An application hosted in the browser cannot prevent navigation away from the application 
    /// by canceling this event.
    /// Note: In the PDC build, if an application hosts the WebOC, this event is not raised for 
    /// navigations within the WebOC. 
    /// </summary>
    public class NavigatingCancelEventArgs : CancelEventArgs
    {
        // Internal constructor
        // <param name="uri">URI of the markup page being navigated to.</param>
        // <param name="content">Root of the element tree being navigated to.</param>
        // <param name="navigationMode">Enum {New, Back, Forward, Refresh}</param>
	    // <param name="Naviagtor">navigator that raised this event</param>
        internal NavigatingCancelEventArgs(
            Uri uri, Object content, CustomContentState customContentState,
            Object extraData, NavigationMode navigationMode, 
            WebRequest request, object Navigator, bool isNavInitiator)
        {
            _uri = uri;
            _content = content;
            _targetContentState = customContentState;
            _navigationMode = navigationMode;
            _extraData = extraData;
            _webRequest = request;
            _isNavInitiator = isNavInitiator;
            _navigator = Navigator;
        }

        /// <summary>
        /// URI of the markup page being navigated to.
        /// </summary>
        public Uri Uri
        {
            get
            {
                return _uri;
            }
        }

        /// <summary>
        /// Root of the element tree being navigated to.
        /// Note: Only one of the Content or Uri property will be set, depending on whether 
        /// the navigation was to a Uri or an existing element tree.
        /// </summary>
        public Object Content
        {
            get
            {
                return _content;
            }
        }

        /// <summary> Target custom content state or view state </summary>
        public CustomContentState TargetContentState
        {
            get { return _targetContentState; }
        }

        /// <summary>
        /// An event handler can set this property to the current content state or view state, to be
        /// saved in a journal entry. If not provided here, the framework will call
        /// IProvideCustomContentState.GetContentState() on the current Content object.
        /// </summary>
        public CustomContentState ContentStateToSave
        {
            set { _contentStateToSave = value; }
            get { return _contentStateToSave; }
        }

        /// <summary>
        /// Exposes extra data object which was optionally passed as a parameter to Navigate.
        /// </summary>
        public Object ExtraData
        {
            //Though we are handing out an object that may potentially contain
            //sensitive information, no one can use it except the app developer
            //unless they have type information for this object. One cannot de-serialize
            //this without Serialization permissions which are not granted by default
            //in partial trust scenarios.
            get
            {
                return _extraData;
            }
        }

        /// <summary>
        /// NavigationMode Enum {New, Back, Forward, Refresh} - where New means a new navigation, 
        /// Forward, Back, and Refresh mean the navigation was initiated from the GoForward, GoBack, 
        /// or Refresh method (or corresponding UI button).
        /// </summary>
        public NavigationMode NavigationMode
        {
            get
            {
                return _navigationMode;
            }
        }

        /// <summary>
        /// Exposes the WebRequest used to retrieve content.  This enables access to HTTP headers.
        /// </summary>
        public WebRequest WebRequest
        {
            get
            {
                return _webRequest;
            }
        }

        /// <summary>
        /// Indicates whether this navigator is initiating the navigation or whether a parent 
        /// navigator is being navigated (e.g., the current navigator is a frame 
        /// inside a page thats being navigated to inside a parent navigator). A developer 
        /// can use this property to determine whether to spin the globe on a LoadStarted event or 
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
                return _isNavInitiator;
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
        private CustomContentState _targetContentState, _contentStateToSave;
        private Object _extraData;
        private NavigationMode _navigationMode;
        private WebRequest _webRequest;
        private bool _isNavInitiator;
	    object _navigator;
    }
}
