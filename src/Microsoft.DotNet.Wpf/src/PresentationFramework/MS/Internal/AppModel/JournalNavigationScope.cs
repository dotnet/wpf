// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description:
//      JournalNavigationScope is the entity that owns a Journal and handles or dispatches journaling-
//      related operations in a tree of navigators sharing the same journal. 
//      NavigationWindow aggregates a JournalNavigationScope instance, as it always needs to have a 
//      journal. So does Frame but only when its JournalOwnership=OwnsJournal.
//      JournalNavigationScope is not a standalone class. It delegate certain operations to its 
//      "navigator host" via the IJournalNavigationScopeHost interface or directly to the host's 
//      NavigationService.
//

using System;
using System.Collections;
using System.Security;
using System.Diagnostics;

using System.Windows;
using System.Windows.Navigation;
using MS.Internal.KnownBoxes;


namespace MS.Internal.AppModel
{
    internal class JournalNavigationScope : DependencyObject, INavigator
    {
        internal JournalNavigationScope(IJournalNavigationScopeHost host)
        {
            _host = host;
            _rootNavSvc = host.NavigationService;
        }

        #region DependencyProperties
        // These properties are declared here, but actual values are set on NavigationWindow and Frame.
        // See OnBackForwardStateChange().

        // CanGoBack & CanGoForward DPs

        private static readonly DependencyPropertyKey CanGoBackPropertyKey =
            DependencyProperty.RegisterReadOnly(
                    "CanGoBack", typeof(bool), typeof(JournalNavigationScope),
                    new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        internal static readonly DependencyProperty CanGoBackProperty =
            CanGoBackPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey CanGoForwardPropertyKey =
            DependencyProperty.RegisterReadOnly(
                    "CanGoForward", typeof(bool), typeof(JournalNavigationScope),
                    new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        internal static readonly DependencyProperty CanGoForwardProperty =
            CanGoForwardPropertyKey.DependencyProperty;

        // BackStack & ForwardStack DPs

        private static readonly DependencyPropertyKey BackStackPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "BackStack", typeof(IEnumerable), typeof(JournalNavigationScope),
                        new FrameworkPropertyMetadata((IEnumerable)null));

        internal static readonly DependencyProperty BackStackProperty =
                BackStackPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey ForwardStackPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "ForwardStack", typeof(IEnumerable), typeof(JournalNavigationScope),
                        new FrameworkPropertyMetadata((IEnumerable)null));

        internal static readonly DependencyProperty ForwardStackProperty =
                ForwardStackPropertyKey.DependencyProperty;

        #endregion DependencyProperties

        #region INavigatorBase Members

        public Uri Source
        {
            get { return _host.Source; }
            set { _host.Source = value; }
        }
        public Uri CurrentSource
        {
            get { return _host.CurrentSource; }
        }
        public object Content
        {
            get { return _host.Content; }
            set { _host.Content = value; }
        }

        public bool Navigate(Uri source)
        {
            return _host.Navigate(source);
        }
        public bool Navigate(Uri source, object extraData)
        {
            return _host.Navigate(source, extraData);
        }
        public bool Navigate(object content)
        {
            return _host.Navigate(content);
        }
        public bool Navigate(object content, object extraData)
        {
            return _host.Navigate(content, extraData);
        }

        public void StopLoading()
        {
            _host.StopLoading();
        }

        public void Refresh()
        {
            _host.Refresh();
        }

        public event NavigatingCancelEventHandler Navigating
        {
            add { _host.Navigating += value; }
            remove { _host.Navigating -= value; }
        }
        public event NavigationProgressEventHandler NavigationProgress
        {
            add { _host.NavigationProgress += value; }
            remove { _host.NavigationProgress -= value; }
        }
        public event NavigationFailedEventHandler NavigationFailed
        {
            add { _host.NavigationFailed += value; }
            remove { _host.NavigationFailed -= value; }
        }
        public event NavigatedEventHandler Navigated
        {
            add { _host.Navigated += value; }
            remove { _host.Navigated -= value; }
        }
        public event LoadCompletedEventHandler LoadCompleted
        {
            add { _host.LoadCompleted += value; }
            remove { _host.LoadCompleted -= value; }
        }
        public event NavigationStoppedEventHandler NavigationStopped
        {
            add { _host.NavigationStopped += value; }
            remove { _host.NavigationStopped -= value; }
        }
        public event FragmentNavigationEventHandler FragmentNavigation
        {
            add { _host.FragmentNavigation += value; }
            remove { _host.FragmentNavigation -= value; }
        }

        #endregion INavigatorBase

        #region INavigator Members
        //
        // Because the INavigator methods of NavigationWindow and Frame simply forward here,
        // _host.VerifyContextAndObjectState() should be called at every entry point.

        public bool CanGoForward
        {
            get
            {
                _host.VerifyContextAndObjectState();
                return _journal != null && !InAppShutdown && _journal.CanGoForward;
            }
        }
        public bool CanGoBack
        {
            get
            {
                _host.VerifyContextAndObjectState();
                return _journal != null && !InAppShutdown && _journal.CanGoBack;
            }
        }

        public void GoForward()
        {
            // CanGoForward checks the calling thread and InAppShutdown as well
            if (CanGoForward == false)
                throw new InvalidOperationException(SR.Get(SRID.NoForwardEntry));

            if (!_host.GoForwardOverride())
            {
                JournalEntry je = Journal.BeginForwardNavigation();
                // If je is null it indicates that we are going going "forward" to the currently
                // displayed page which has no journal entry. The comment in BeginForwardNavigation
                // explains how this can happen.
                if (je == null)
                {
                    _rootNavSvc.StopLoading();
                    return;
                }
                NavigateToEntry(je);
                // NavigateToEntry() should call AbortJournalNavigation() if navigation is canceled.
            }
        }

        public void GoBack()
        {
            // CanGoBack checks the calling thread and InAppShutdown as well
            if (CanGoBack == false)
                throw new InvalidOperationException(SR.Get(SRID.NoBackEntry));

            if (!_host.GoBackOverride())
            {
                JournalEntry je = Journal.BeginBackNavigation();
                if (je == null) // See comment in GoForwardInternal().
                {
                    _rootNavSvc.StopLoading();
                    return;
                }
                NavigateToEntry(je);
                // NavigateToEntry() should call Journal.AbortJournalNavigation() if navigation is canceled.
            }
        }

        public void AddBackEntry(CustomContentState state)
        {
            _host.VerifyContextAndObjectState();
            _rootNavSvc.AddBackEntry(state);
        }

        public JournalEntry RemoveBackEntry()
        {
            _host.VerifyContextAndObjectState();
            return _journal == null ? null : _journal.RemoveBackEntry();
        }

        public System.Collections.IEnumerable BackStack
        {
            get
            {
                _host.VerifyContextAndObjectState();
                return Journal.BackStack;
            }
        }

        public System.Collections.IEnumerable ForwardStack
        {
            get
            {
                _host.VerifyContextAndObjectState();
                return Journal.ForwardStack;
            }
        }

        JournalNavigationScope INavigator.GetJournal(bool create)
        {
            return this;
        }

        #endregion INavigator

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        #region Internal Methods

        /// <summary>
        /// The Journal instance needs to be lazily-created to allow restoring a deserialized journal
        /// from the TravelLog or from a child Frame's journaled state (Frame.FramePersistState).
        /// Call this method to make the back stack and forward stack objects available and to allow
        /// anyone to register journal change event handlers.
        /// </summary>
        internal void EnsureJournal()
        {
            // The getter lazily creates Journal and notifies the host.
            Journal journal = Journal;
            Debug.Assert(journal != null);
        }

        internal bool CanInvokeJournalEntry(int entryId)
        {
            // _journal could be null for .deploy apps since the main (second) app is created after a delay
            // (the first app is created to show the progress UI and the main app is created when bits are ready)
            if (_journal == null)
                return false;

            int realIndex = _journal.FindIndexForEntryWithId(entryId);
            if (realIndex == -1)
                return false;

            JournalEntry entry = _journal[realIndex];
            // Journal.IsNavigable() will apply the filter, which is normally IsEntryNavigable().
            return _journal.IsNavigable(entry);
        }

        internal bool NavigateToEntry(int index)
        {
            JournalEntry entry = Journal[index];
            return NavigateToEntry(entry);
        }

        internal bool NavigateToEntry(JournalEntry entry)
        {
            if (entry == null)
            {
                Debug.Fail("Tried to navigate to a null JournalEntry.");
                return false;
            }
            if (!Journal.IsNavigable(entry))
            {
                Debug.Fail("Tried to navigate to a non-navigable journal entry.");
                return false;
            }

            NavigationService navigationService = _rootNavSvc.FindTarget(entry.NavigationServiceId);
            Debug.Assert(navigationService != null, "NavigationService cannot be null for journal navigations");

            NavigationMode mode = Journal.GetNavigationMode(entry);
            bool navigated = false;
            try
            {
                navigated = entry.Navigate(navigationService.INavigatorHost, mode);
            }
            finally
            {
                if (!navigated)
                {
                    AbortJournalNavigation();
                }
            }
            return navigated;
        }

        internal void AbortJournalNavigation()
        {
            if (_journal != null)
            {
                _journal.AbortJournalNavigation();
            }
        }

        internal INavigatorBase FindTarget(string name)
        {
            return _rootNavSvc.FindTarget(name);
        }

        internal static void ClearDPValues(DependencyObject navigator)
        {
            navigator.SetValue(CanGoBackPropertyKey, BooleanBoxes.FalseBox);
            navigator.SetValue(CanGoForwardPropertyKey, BooleanBoxes.FalseBox);

            navigator.SetValue(BackStackPropertyKey, null);
            navigator.SetValue(ForwardStackPropertyKey, null);
        }

#if DEBUG
        /// <summary>
        /// DO NOT USE - Public debug method to test the journal state for PageFunctions
        /// </summary>
        internal string PrintJournal()
        {
            Journal journal = Journal;
            String s = "";
            for (int i = 0; i < journal.TotalCount; i++)
            {
                if (journal[i].IsNavigable())
                {
                    s += "o"  ;
                }
                else
                {
                    switch (journal[i].EntryType)
                    {
                        case JournalEntryType.Navigable:
                            s += "o";
                            break;
                        case JournalEntryType.UiLess:
                            s += "u";
                            break;
                        default:
                            Invariant.Assert(false, "Invalid JournalEntryType: " + journal[i].EntryType);
                            break;
                    }
                }
            }
            return s;
        }
#endif
        #endregion

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------
        #region Internal Properties

        /// <summary>
        /// The Journal instance needs to be lazily-created to allow restoring a deserialized journal
        /// from the TravelLog or from a child Frame's journaled state (Frame.FramePersistState).
        /// The getter triggers the Journal creation and notifes the host.
        /// <seealso cref="EnsureJournal"/>
        /// </summary>
#if DEBUG
        // to prevent creating the Journal instance prematurely while debugging
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
#endif
        internal Journal Journal
        {
            get
            {
                if (_journal == null)
                {
                    Journal = new Journal();
                }
                return _journal;
            }
            // Used by the getter; also by RootBrowserWindow and Frame to install a deserialized Journal.
            set
            {
                Debug.Assert(_journal == null && value != null,
                    "The Journal should be set only once and never removed."); // see bug 1367999
                _journal = value;
                _journal.Filter = new JournalEntryFilter(this.IsEntryNavigable);
                _journal.BackForwardStateChange += new EventHandler(OnBackForwardStateChange);

                DependencyObject navigator = (DependencyObject)_host;
                navigator.SetValue(BackStackPropertyKey, _journal.BackStack);
                navigator.SetValue(ForwardStackPropertyKey, _journal.ForwardStack);

                _host.OnJournalAvailable();
            }
        }

        internal NavigationService RootNavigationService
        {
            get { return _rootNavSvc; }
        }

        internal INavigatorBase NavigatorHost
        {
            get { return _host; }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        #region Private Methods

        private void OnBackForwardStateChange(object sender, EventArgs e)
        {
            Debug.Assert(sender == _journal);

            // Update CanGoBack and CanGoForward on the host navigator, only if actually changed.
            DependencyObject navigator = (DependencyObject)_host;
            bool canGoBackFwdChanged = false;
            bool newState = _journal.CanGoBack;
            if (newState != (bool)navigator.GetValue(CanGoBackProperty))
            {
                navigator.SetValue(CanGoBackPropertyKey, BooleanBoxes.Box(newState));
                canGoBackFwdChanged = true;
            }
            newState = _journal.CanGoForward;
            if (newState != (bool)navigator.GetValue(CanGoForwardProperty))
            {
                navigator.SetValue(CanGoForwardPropertyKey, BooleanBoxes.Box(newState));
                canGoBackFwdChanged = true;
            }

            if (canGoBackFwdChanged)
            {
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }

        /// <summary>
        /// This is the filter callback for Journal. To keep a single code path and to avoid
        /// inconsistent results, use Journal.IsNavigable() instead of this method.
        /// </summary>
        private bool IsEntryNavigable(JournalEntry entry)
        {
            if (entry == null || !entry.IsNavigable())
                return false;
            // If the entry is associated with a child frame, the frame has to be currently available.
            // For a given journal entry group, only the "exit" entry is made visible. Effectively, 
            // this collapses all fragment-navigation and CustomContentState-navigation entries for
            // a page (other than the current one) to a single entry. That's what IE does.
            NavigationService ns = _rootNavSvc.FindTarget(entry.NavigationServiceId);
            return ns != null
                && (ns.ContentId == entry.ContentId || entry.JEGroupState.GroupExitEntry == entry);
        }

        private bool InAppShutdown
        {
            get { return System.Windows.Application.IsShuttingDown; }
        }

        #endregion

        //------------------------------------------------------    
        //    
        //  Private Fields    
        //    
        //------------------------------------------------------

        #region Private Fields

        private IJournalNavigationScopeHost _host;
        private NavigationService _rootNavSvc; // == _host.NavigationService
        private Journal _journal; // lazily-created; see Journal property

        #endregion Private Fields
    };


    /// <summary>
    /// The interface through which JournalNavigationScope talks back to its navigator host.
    /// </summary>
    internal interface IJournalNavigationScopeHost : INavigatorBase
    {
        NavigationService NavigationService { get; }

        void VerifyContextAndObjectState();

        void OnJournalAvailable();

        /// <summary>
        /// Allows the navigator host to do its own handling of GoBack. Currently used by 
        /// RootBrowserWindow to do the TravelLog integration.
        /// </summary>
        /// <returns> True to cancel the normal handling of GoBack </returns>
        bool GoBackOverride();
        bool GoForwardOverride();
    };
}
