// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description:
//              The INavigator interface exposes all the properties, methods, 
//              and events required for navigation. This interface is implemented 
//              by NavigationWindow and Frame.
//

using System;
using System.Collections;
using System.ComponentModel;

using MS.Internal.Utility;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Documents;
using System.Windows.Media;

namespace MS.Internal.AppModel
{
    internal interface IDownloader
    {
        NavigationService Downloader
        {
            get;
        }

        // this event is used by NavigationService to listen to its Downloader's 
        // ContentRendered so that it can scroll into view the correct element
        // for fragment navigations
        event EventHandler ContentRendered;
    }

    /// <summary>
    /// INavigatorBase defines the core navigation API that doesn't require having a journal. 
    /// This is the functionality available from Frame when it doesn't have its own journal.
    /// </summary>
    internal interface INavigatorBase
    {
        /// <summary>
        /// Uri for the page currently contained by the Inavigator. 
        /// -Setting this property performs a navigation to the specified Uri.
        /// -Getting this property when a navigation is not in progress returns the URI of 
        /// the current page. Getting this property when a navigation is in progress returns 
        /// the URI of the page being navigated to.
        /// -Note: Supporting navigation via setting a property makes it possible to write 
        /// a NavigationWindow in markup and specify its initial content.
        /// </summary>
        Uri Source
        {
            get;
            set;
        }

        /// <summary>
        /// Uri for the current page in the INavigator. Getting this property always 
        /// returns the URI of the content thats currently displayed in the INavigator, 
        /// regardless of whether the navigation is in progress or not.
        /// </summary>
        Uri CurrentSource
        {
            get;
        }

        /// <summary>
        /// Root element of the content in the INavigator. 
        /// -Setting this property performs a navigation to the specified element. 
        /// -Getting this property returns the root element of the element tree currently 
        /// contained in the INavigator.
        /// </summary>
        Object Content
        {
            get;
            set;
        }

        /// <summary>
        /// Navigates to the Source and downloads the content.   
        /// </summary>
        /// <param name="source">URI of the application or content being navigated to.</param>
        /// <returns>bool indicating whether the navigation was successfully started or not</returns>
        bool Navigate(Uri source);

        /// <summary>
        /// This method navigates this INavigator to the given Uri.
        /// </summary>
        /// <param name="source">The URI to be navigated to.</param>        
        /// <param name="extraData">enables the develeoper to supply an extra object, that will be returned in the NavigatedEventArgs of the Navigated event. The extra data enables the developer 
        /// to identify the source of the navigation, in the presence of 
        /// multiple navigations.
        /// </param>
        /// <returns>bool indicating whether the navigation was successfully started or not</returns>
        bool Navigate(Uri source, Object extraData);

        /// <summary>
        /// Navigates to an existing element tree. 
        /// </summary>
        /// <param name="content">Root of the element tree being navigated to.</param>
        /// <returns>bool indicating whether the navigation was successfully started or not</returns>
        bool Navigate(Object content);

        /// <summary>
        /// This method navigates this INavigator to the 
        /// given Element.
        /// </summary>
        /// <param name="content">The Element to be navigated to.</param>
        /// <param name="extraData">enables the develeoper to supply an extra object, that will be returned in the NavigatedEventArgs of the Navigated event. The extra data enables the developer 
        /// to identify the source of the navigation, in the presence of 
        /// multiple navigations.
        /// </param>
        /// <returns>bool indicating whether the navigation was successfully started or not</returns>
        bool Navigate(Object content, Object extraData);

        /// <summary>
        /// Stops the navigation or download currently in progress. 
        /// The behavior is the same as clicking the Stop button.
        /// </summary>
        ///<exception cref="System.InvalidOperationException">
        /// There is no navigation or download currently in progress
        ///</exception>
        void StopLoading();

        /// <summary>
        /// Reloads the current content. The behavior is the same as clicking the Refresh button.
        /// </summary>
        void Refresh();

        /// <summary>
        /// Raised just before a navigation takes place. This event is fired for frame 
        /// navigations as well as top-level page navigations, so may fire multiple times 
        /// during the download of a page. 
        /// The NavigatingCancelEventArgs contain the uri or root element of the content 
        /// being navigated to and an enum value that indicates the type of navigation. 
        /// Canceling this event prevents the application from navigating.  
        /// Note: An application hosted in the browser cannot prevent navigation away from 
        /// the application by canceling this event.
        /// Note: In the PDC build, if an application hosts the WebOC, this event is not raised 
        /// for navigations within the WebOC. 
        /// </summary>
        event NavigatingCancelEventHandler Navigating;

        /// <summary>
        /// Raised at periodic intervals while a navigation is taking place. 
        /// The NavigationProgressEventArgs tell how many total bytes need to be downloaded and 
        /// how many have been sent at the moment the event is fired. This event can be used to provide 
        /// a progress indicator to the user.
        /// </summary>
        event NavigationProgressEventHandler NavigationProgress;

        /// <summary>
        /// Raised when an error is encountered during a navigation.
        /// The NavigationFailedEventArgs contains 
        /// the exception that was thrown. By default Handled property is set to false, 
        /// which allows the exception to be rethrown. 
        /// The event handler can prevent exception from throwing
        /// to the user by setting the Handled property to true
        /// </summary>
        event NavigationFailedEventHandler NavigationFailed;

        /// <summary>
        /// Raised after navigation the target has been found and the download has begun. This event 
        /// is fired for frame navigations as well as top-level page navigations, so may fire 
        /// multiple times during the download of a page. 
        /// For an asynchronous navigation, this event indicates that a partial element tree 
        /// has been handed to the parser, but more bits are still coming. 
        /// For a synchronous navigation, this event indicates the entire tree has been 
        /// handed to the parser.
        /// The NavigationEventArgs contain the uri or root element of the content being navigated to.
        /// This event is informational only, and cannot be canceled.
        /// </summary>
        event NavigatedEventHandler Navigated;

        /// <summary>
        /// Raised after the entire page, including all images and frames, has been downloaded 
        /// and parsed. This is the event to handle to stop spinning the globe. The developer 
        /// should check the IsNavigationInitiator property on the NavigationEventArgs to determine 
        /// whether to stop spinning the globe.
        /// The NavigationEventArgs contain the uri or root element of the content being navigated to, 
        /// and a IsNavigationInitiator property that indicates whether this is a new navigation 
        /// initiated by this INavigator, or whether this navigation is being propagated down 
        /// from a higher level navigation taking place in a containing window or frame. 
        /// This event is informational only, and cannot be canceled.
        /// </summary>
        event LoadCompletedEventHandler LoadCompleted;

        /// <summary>
        /// Raised when a navigation or download has been interrupted because the user clicked 
        /// the Stop button, or the Stop method was invoked. 
        /// The NavigationEventArgs contain the uri or root element of the content being navigated to. 
        /// This event is informational only, and cannot be canceled.
        /// </summary>
        event NavigationStoppedEventHandler NavigationStopped;

        /// <summary>
        /// Raised when a navigation uri contains a fragment.  This event is fired before the element is scrolled
        /// into view and allows the listener to respond to the fragment in a custom way.
        /// </summary>
        event FragmentNavigationEventHandler FragmentNavigation;
    };

    internal interface INavigator : INavigatorBase
    {
        /// <summary>
        /// Asks the navigator implementing this interface to provide its applicable JournalNavigationScope.
        /// </summary>
        /// <param name="create"> Whether a new JournalNavigationScope should be established at the level
        /// of this navigator if none is available from a parent navigator. Because a tree of navigators
        /// is often built bottom-up, create=true should be passed only when a journal is really needed,
        /// typically to add an entry for the previous page. This makes sure that a frame will not be 
        /// prematurely forced to create its own journal when it's initially navigated but before a 
        /// journal becomes available from its parent.
        /// </param>
        /// <returns></returns>
        JournalNavigationScope GetJournal(bool create);

        /// <summary>
        /// Tells whether there are any entries in the Forward branch of the Journal. 
        /// This property can be used to enable the Forward button.
        /// </summary>
        bool CanGoForward
        {
            get;
        }

        /// <summary>
        /// Tells whether there are any entries in the Back branch of the Journal. 
        /// This property can be used to enable the Back button.
        /// </summary>
        bool CanGoBack
        {
            get;
        }

        /// <summary>
        /// Navigates to the next entry in the Forward branch of the Journal, if one exists. 
        /// If there is no entry in the Forward stack of the journal, the method throws an 
        /// exception. The behavior is the same as clicking the Forward button.
        /// </summary>
        ///<exception cref="System.InvalidOperationException">
        /// There is no entry in the Back stack of the journal to navigate to.
        ///</exception>
        ///<returns>Bool indicating whether Forward navigation was started or not</returns>
        void GoForward();

        /// <summary>
        /// Navigates to the previous entry in the Back branch of the Journal, if one exists. 
        /// The behavior is the same as clicking the Back button.
        /// </summary>
        ///<exception cref="System.InvalidOperationException">
        /// There is no entry in the Forward stack of the journal to navigate to.
        ///</exception>
        ///<returns>Bool indicating whether Back navigation was started or not</returns>
        void GoBack();

        /// <summary>
        /// Adds a new journal entry to NavigationWindow's back history. The journal entry
        /// encapsulates the given custom content state (or view state).
        /// </summary>
        void AddBackEntry(CustomContentState state);

        /// <summary>
        /// Remove the first JournalEntry from NavigationWindow's back history
        /// </summary>
        /// <returns>The JournalEntry removed</returns>
        JournalEntry RemoveBackEntry();

        /// <summary>
        /// The back stack of the navigator, when it owns a journal (JournalNavigationScope).
        /// </summary>
        IEnumerable BackStack
        {
            get;
        }
        /// <summary>
        /// The forward stack of the navigator, when it owns a journal (JournalNavigationScope).
        /// </summary>
        IEnumerable ForwardStack
        {
            get;
        }
    };

    /// <summary>
    /// Defines implementation-level services that NavigationService needs from its host 
    /// (NavigationWindow or Frame).
    /// </summary>
    internal interface INavigatorImpl
    {
        /// <summary>
        /// This method is called from NavService whenever the NavService's Source value is updated.
        /// The INavigator can use this to update its SourceProperty. 
        /// </summary>
        /// <param name="journalOrCancel">It indicates whether the NavService's Source value is as a result of 
        /// calling Navigate API directly or from GoBack/GoForward, journal navigation, a cancellation</param>
        void OnSourceUpdatedFromNavService(bool journalOrCancel);

        /// <summary>
        /// Returns the root of the visual tree created for the navigator's current Content.
        /// If Content is visual, it is returned. Otherwise, the control/view instantiated through
        /// a DataTemplate for Content (as first child of the navigator's ContentPresenter) is returned.
        /// This can be null, if layout hasn't been done yet.
        /// </summary>
        Visual FindRootViewer();
    };
}
