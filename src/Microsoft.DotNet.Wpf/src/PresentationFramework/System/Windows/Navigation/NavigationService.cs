// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Implements the Avalon basic Navigation unit class
//

using System;
using System.Timers;
using System.IO;
using System.IO.Packaging;
using System.Globalization;
using System.Windows.Threading;
using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Diagnostics;
using System.Security;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net;
using System.Net.Cache;
using MS.Internal;
using MS.Internal.Navigation;
using MS.Internal.Utility;
using MS.Internal.AppModel;
using MS.Internal.Controls;
using MS.Utility;

using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Markup;

//In order to avoid generating warnings about unknown message numbers and
//unknown pragmas when compiling your C# source code with the actual C# compiler,
//you need to disable warnings 1634 and 1691. (Presharp Documentation)
#pragma warning disable 1634, 1691

namespace System.Windows.Navigation
{
    #region NavigationService Class
    /// <summary>
    /// NavigationService provides the Navigation functionality
    /// </summary>
    /// All security sensitive classes should be sealed or protected with InheritanceDemand
    // NavigationService does implement INavigator, but it's not declared explicitly in order
    // to prevent inadvertently passing NavigationService instead of its navigator host
    // (NavigationWindow or Frame).
    public sealed class NavigationService : IContentContainer /*See comment above*/
    {
        #region Constructors

        /// <summary>
        /// Internal class used to host content and handles all navigations
        /// </summary>
        /// <param name="nav">
        /// Parent navigator that uses and owns this NS. (It's either NavigationWindow or Frame.)
        /// </param>
        internal NavigationService(INavigator nav)
        {
            INavigatorHost = nav;

            if (!(nav is NavigationWindow)) // NW has null GUID.
                GuidId = Guid.NewGuid();
        }
        #endregion Constructors

        #region Private Methods

        private void ResetPendingNavigationState(NavigationStatus newState)
        {
            // If this container is done loading decrement the window's NavigationService bytes by the final amts of this container
            JournalNavigationScope jns = JournalScope;
            if (jns != null && jns.RootNavigationService != this)
            {
                // If there were two child frames loading simultaneously, then rootNavigationService will reflect
                // only the remaining child's progress now else this will reset window's totals to zero
                jns.RootNavigationService.BytesRead -= _bytesRead;
                jns.RootNavigationService.MaxBytes -= _maxBytes;
            }

            _navStatus = newState;
            _bytesRead = 0;
            _maxBytes = 0;

        #if DEBUG
            // We should only be replacing queue items that aren't already posted
            Debug.Assert(_navigateQueueItem == null || _navigateQueueItem.IsPosted == false);
        #endif
            _navigateQueueItem = null;
            _request = null;
        }

        // Navigate event fired by Hyperlink
        private void OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Debug.Assert(e != null, "Hyperlink fired Navigate event with null NavigateEventArgs");

            e.Handled = true;

            string target = e.Target;
            Uri bpu = e.Uri;

            // If the Uri is absolute uri, we just take the uri. Otherwise we require sender to implement
            // IUriContext so we can resolve with its base uri.
            if ((bpu != null) && (bpu.IsAbsoluteUri == false))
            {
                DependencyObject dobj = e.OriginalSource as DependencyObject;

                Debug.Assert(dobj != null, "RequestNavigateEventArgs.OriginalSource should be DependencyObject");

                //This is usually a pack uri, with the path relative to the base of the application.
                //      The app base is abstracted out to pack://application:,,,/ in the pack Uri.
                //This is set by the baml record reader but other implementors can choose to return any Uri.
                IUriContext uc = dobj as IUriContext;

                //Throw an exception if IUriContext is not implemented, any element can raise this event since it is public.
                if (uc == null)
                    throw new Exception(SR.Get(SRID.MustImplementIUriContext, typeof(IUriContext)));

                bpu = BindUriHelper.GetUriToNavigate(dobj, uc.BaseUri, e.Uri);
            }

            INavigatorBase navigator = null;
            bool inSameThread = true;

            if (!String.IsNullOrEmpty(target))
            {
                // Need spec for this behavior
                // if (target == "NewWindow")
                // {
                // create a new NavigationWindow here

                // }

                // Specially handle the target as the root of this navigation window.
                // what special ID should we use this Navigator.
                // maybe we can use "Root" as the target name for this case.

                // The below code is for other case.

                // First check from the current NavigationService. It is needed for the island frame case.
                // Island frame can be inside a Window (instead of a NavWin). And JounralScope is not null only
                // after the initial navigation.
                // But it is still unsupported case: Two "island" frames within a non-NavigationWindow.
                // Then one can't target the other.
                navigator = this.FindTarget(target);

                // Try the current JournalNavigationScope before the entire window.
                if ((navigator == null) && (JournalScope != null))
                {
                    navigator = JournalScope.FindTarget(target);
                }

                if (navigator == null)
                {
                    // We should at the very least check current window -if we have one- before we iterate rest of the windows
                    NavigationWindow navWin = FindNavigationWindow();
                    if (navWin != null)
                    {
                        navigator = FindTargetInNavigationWindow(navWin, target);
                    }

                    // Didn't find it in the window, try the NavigationWindows in the WindowsCollection in the application
                    if (navigator == null)
                    {
                        navigator = FindTargetInApplication(target);

                        if (navigator != null)
                        {
                            inSameThread = (((DispatcherObject)navigator).CheckAccess() == true);
                        }
                    }
                }
            }
            else
            {
                navigator = INavigatorHost;
            }

            if (navigator != null)
            {
                if (inSameThread)
                {
                    navigator.Navigate(bpu);
                }
                else
                {
                    ((DispatcherObject)navigator).Dispatcher.BeginInvoke(
                                DispatcherPriority.Send,
                                (DispatcherOperationCallback)delegate(object unused)
                                {
                                    return navigator.Navigate(bpu);
                                },
                                null);
                }
            }
            else
            {
                throw new System.ArgumentException(SR.Get(SRID.HyperLinkTargetNotFound));
            }
        }

        // Tests if two uris resolve to the same Uri.  The Uri fragments are also
        // compared.  Neither comparison is case sensitive.
        static private bool IsSameUri(Uri baseUri, Uri a, Uri b, bool withFragment)
        {
            if (object.ReferenceEquals(a, b)) // also handles both null
            {
                return true;
            }
            if (a == null || b == null)
            {
                return false;
            }

            Uri aResolved = BindUriHelper.GetResolvedUri(baseUri, a);
            Uri bResolved = BindUriHelper.GetResolvedUri(baseUri, b);
            bool isSame = aResolved.Equals(bResolved);

            if (isSame && withFragment)
            {
                isSame = isSame &&
                         (string.Compare(aResolved.Fragment, bResolved.Fragment,
                                        StringComparison.OrdinalIgnoreCase) == 0);
            }

            return isSame;
        }


        /// <summary>
        /// Navigates to a fragment within the current page AND/OR replays a CustomContentState
        /// within the current page, and updates the journal.
        /// </summary>
        /// <remarks>
        /// Currently we do not strictly distinguish between fragment and CustomContentState
        /// navigations, which may not work well for all applications. CustomContentState is requested
        /// (but not required) and replayed for fragment navigations too. This will work for Mongoose,
        /// for example, but if an application needs to handle the two navigation types differently,
        /// it will have to use the FragmentNavigation callback, raise an internal flag, and possibly
        /// return null from its GetContentState() implementation or alter the JournalEntryName to
        /// reflect the original fragment location.
        /// </remarks>
        private void NavigateToFragmentOrCustomContentState(Uri uri, object navState)
        {
            Debug.Assert(_bp != null, "NavigationService should not handle a nav from a hyperlink thats not in its hosted tree");

            NavigateInfo navInfo = navState as NavigateInfo;
            JournalEntry destinationEntry = null;
            if (navInfo != null)
            {
                Debug.Assert(IsConsistent(navInfo));
                destinationEntry = navInfo.JournalEntry; // null for new navigation
            }
            NavigationMode navMode = navInfo == null ? NavigationMode.New : navInfo.NavigationMode;
            // * This method should work with navMode=Refresh.

            // Root Viewer state is saved first because the fragment navigation can change view state
            // (BringIntoView()).
            CustomJournalStateInternal rootViewerState = GetRootViewerState(JournalReason.FragmentNavigation);

            string fragmentName = uri != null ? BindUriHelper.GetFragment(uri) : null;
            bool hasCustomContentState =
                destinationEntry != null && destinationEntry.CustomContentState != null;
            // Note: The assertion earlier implies that CustomContentState can be replayed only
            // for Back or Forward navigations. If this method is called for a New navigation,
            // it is fragment-only.

            // About the second parameter to NavigateToFragment():
            // Fragment navigation may include storing/replaying CustomContentState. One special
            // case here is when doing journal navigation and the given URI doesn't include a
            // fragment name. Then we don't know whether the destinationEntry was originally created
            // as a result of fragment navigation or AddBackEntry(). Fragment re-navigation to the
            // base URI is supposed to scroll the content to the top. But for replaying
            // CustomContentState only, that may be undesirable. (If an application happens to require
            // this behavior, it can include the scroll position in the CustomContentState.)
            bool targetElementExists = NavigateToFragment(fragmentName, !hasCustomContentState);

            // Do not record a new [fragment] navigation if the address bar will not change.
            if (navMode == NavigationMode.Back || navMode == NavigationMode.Forward ||
                (targetElementExists &&
                 !IsSameUri(null, _currentSource, uri, true /* with Fragment */)))
            {
                Debug.Assert(navMode != NavigationMode.Refresh); // because of !IsSameUri() above

                try
                {
                    _rootViewerStateToSave = rootViewerState;
                    UpdateJournal(navMode, JournalReason.FragmentNavigation, destinationEntry);
                }
                finally
                {
                    _rootViewerStateToSave = null;
                }

                // Remember the new location and the original relative uri
                Uri resolvedUri = BindUriHelper.GetResolvedUri(_currentSource, uri);
                _currentSource = resolvedUri;
                _currentCleanSource = BindUriHelper.GetUriRelativeToPackAppBase(uri);
            }

            // Fire the Navigated event here since we're bypassing the normal navigation path
            // HandleNavigated has the logic to fire LoadComplete as needed.
            // It also replays CustomContentState.
            Debug.Assert(_navStatus == NavigationStatus.Navigating);
            _navStatus = NavigationStatus.Navigated;
            HandleNavigated(navState, false/*navigatedToNewContent*/);
        }


        /// <summary>
        /// Attempt to find the object with the specified elementId in the visual tree, and scroll to it.
        /// The elementId is typically the fragment part of a URI (without the leading '#'). If an
        /// element with the correct ID can't be found, try the root of the tree.
        /// </summary>
        /// <param name="elementId">The id of the element to find and scroll to</param>
        /// <param name="scrollToTopOnEmptyFragment"> See note in NavigateToFragmentOrCustomContentState() </param>
        /// <returns>True if the element was found and scrolled to or handled by the FragmentNavigation event.  Otherwise returns false.</returns>
        private bool NavigateToFragment(string elementId, bool scrollToTopOnEmptyFragment)
        {
            // NavigateToFragment should return true or false based on whether
            // a target element was found && if scroll was successful. (Bug 839381 blocked on scrollviewer task)
            // A scroll may not always be successful especially if the element marked itself
            // as hidden OR layout is not done. Due to the latter we call this API from the
            // ContentRendered event for navigations to urls of the form `http://www.example.com/page1.xaml#bookmark

            if (FireFragmentNavigation(elementId))
            {
                return true;
            }

            DependencyObject targetElement = null;

            // Try to find the target element
            if (String.IsNullOrEmpty(elementId))
            {
                if (!scrollToTopOnEmptyFragment)
                {
                    return false;
                }
                // This is the case where we navigate from source#bookmark to source, so scroll the root element into view
                ScrollContentToTop();
                return true;
            }

            targetElement = LogicalTreeHelper.FindLogicalNode((DependencyObject)_bp, elementId) as DependencyObject;

            // Try to bring the target element into view
            BringIntoView(targetElement);

            return targetElement != null;
        }

        private void ScrollContentToTop()
        {
            if (_bp != null)
            {
                // Supposedly temporary solution: handling the common case of a ScrollViewer inside a Page.
                // This special case has to come first because the wrong ScrollViewer (one enclosing a frame)
                // may respond if the ScrollBar.ScrollToTopCommand is tried first.
                FrameworkElement fe = _bp as FrameworkElement;
                if (fe != null)
                {
                    IEnumerator children = fe.LogicalChildren;
                    if (children != null && children.MoveNext())
                    {
                        ScrollViewer sv = children.Current as ScrollViewer;
                        if (sv != null)
                        {
                            sv.ScrollToTop();
                            return;
                        }
                    }
                }

                // This works when _bp is a ScrollViewer or there is one in the visual tree (provided
                // by a style).
                IInputElement elem = _bp as IInputElement;
                if (elem != null)
                {
                    if (ScrollBar.ScrollToTopCommand.CanExecute(null, elem))
                    {
                        ScrollBar.ScrollToTopCommand.Execute(null, elem);
                        return;
                    }
                }

                // Fallback. This works for the DocumentViewerBase derivatives.
                BringIntoView(_bp as DependencyObject);
            }
        }

        private static void BringIntoView(DependencyObject elem)
        {
            FrameworkElement fe = elem as FrameworkElement;
            if (fe != null)
            {
                fe.BringIntoView();
            }
            else
            {
                FrameworkContentElement fce = elem as FrameworkContentElement;
                if (fce != null)
                {
                    fce.BringIntoView();
                }
            }
        }

        /// <summary>
        /// <see cref="JournalScope"/> property
        /// </summary>
        /// <returns> Can be null </returns>
        private JournalNavigationScope EnsureJournal()
        {
            if (_journalScope == null && _navigatorHost != null)
            {
                _journalScope = _navigatorHost.GetJournal(true/*do create*/);
            }
            //Throw if no JNS?
            return _journalScope;
        }

        bool IsConsistent(NavigateInfo navInfo)
        {
            return navInfo == null
                || navInfo.IsConsistent
                   && (navInfo.JournalEntry == null || navInfo.JournalEntry.NavigationServiceId == _guidId);
        }

        private bool IsJournalNavigation(NavigateInfo navInfo)
        {
            return navInfo != null &&
                (navInfo.NavigationMode == NavigationMode.Back || navInfo.NavigationMode == NavigationMode.Forward);
        }

        private CustomJournalStateInternal GetRootViewerState(JournalReason journalReason)
        {
            if (_navigatorHostImpl != null && !(_bp is Visual))
            {
                Visual v = _navigatorHostImpl.FindRootViewer();
                IJournalState ijs = v as IJournalState;
                if (ijs != null)
                {
                    return ijs.GetJournalState(journalReason);
                }
            }
            return null;
        }

        private bool RestoreRootViewerState(CustomJournalStateInternal rvs)
        {
            Debug.Assert(!(_bp is Visual));
            Visual v = _navigatorHostImpl.FindRootViewer();
            if (v == null)
                return false; // Template may not be applied yet.
            IJournalState ijs = v as IJournalState;
            if (ijs != null)
            {
                ijs.RestoreJournalState(rvs);
            }
            //else: maybe type of viewer changed. Still returning true so that restoring state
            //  is not reattempted in this case.
            return true;
        }


        #endregion Private Methods

        #region Internal Methods

        /// <summary>
        /// </summary>
        /// <param name="targetName"></param>
        /// <returns></returns>
        static internal INavigatorBase FindTargetInApplication(string targetName)
        {
            // Application has two window collections. One for Application windows (windows
            // created on the same thread as the app) and the other for all other windows.
            // we will try to find target in all of these windows.
            if (Application.Current == null)
                return null;

            // WindowsInternal takes a lock to access the storage.  We want to clone it and use the copy.
            // Otherwise, while we iterate over it some other thread could modify the collection.
            // Typically, there won't be a lot of windows in an App, so this should not be that costly
            //
            // Same argument goes for NonAppWindowsInternal.Clone() below
            //
            INavigatorBase navigator = FindTargetInWindowCollection(Application.Current.WindowsInternal.Clone(), targetName);

            // if we didn't find the target in one of the App windows, search for it in windows on
            // non app thread
            if (navigator == null)
            {
                navigator = FindTargetInWindowCollection(Application.Current.NonAppWindowsInternal.Clone(), targetName);
            }

            return navigator;
        }

        static private INavigatorBase FindTargetInWindowCollection(WindowCollection wc, string targetName)
        {
            INavigatorBase navigator = null;
            NavigationWindow nw = null;

            for (int i = 0; i < wc.Count; i++)
            {
                nw = wc[i] as NavigationWindow;

                if (nw != null)
                {
                    // if we're on the same thread as that of nw then we can simple try to
                    // find target in nw, else we need to find target on the nw's thread.
                    // We do that below by using nw.Dispatcher.Invoke
                    if (nw.CheckAccess() == true)
                    {
                        navigator = FindTargetInNavigationWindow(nw, targetName);
                    }
                    else
                    {
                        navigator = (INavigator)nw.Dispatcher.Invoke(
                            DispatcherPriority.Send,
                            (DispatcherOperationCallback)delegate(object unused)
                            {
                                return FindTargetInNavigationWindow(nw, targetName);
                            },
                            null
                        );
                    }

                    if (navigator != null)
                    {
                        return navigator;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Find a navigator tree for the given navigator ID
        /// </summary>
        /// <param name="navigationWindow">Navigation Window</param>
        /// <param name="navigatorId">NavigatorId to search</param>
        /// <returns></returns>
        static private INavigatorBase FindTargetInNavigationWindow(NavigationWindow navigationWindow, string navigatorId)
        {
            if (navigationWindow != null)
            {
                return navigationWindow.NavigationService.FindTarget(navigatorId);
            }
            return null;
        }

        internal void InvalidateJournalNavigationScope()
        {
            // If there is a pending journal navigation (Back/Fwd), the JournalNavigationScope cannot
            // be changed. (If it is a _new_ navigation, we're OK; it will be recorded in the new
            // applicable journal.)
            // _navStatus or _navigateQueueItem are not checked here, because they are set only after
            // raising the Navigating event, while an event handler might cause journal ownership to change.
            if (_journalScope != null && _journalScope.Journal.HasUncommittedNavigation)
                throw new InvalidOperationException(SR.Get(SRID.InvalidOperation_CantChangeJournalOwnership));

            _journalScope = null;

            for (int i = ChildNavigationServices.Count - 1; i >= 0; i--)
            {
                ((NavigationService)ChildNavigationServices[i]).InvalidateJournalNavigationScope();
            }
        }

        internal void OnParentNavigationServiceChanged()
        {
            NavigationService oldParent = _parentNavigationService;
            NavigationService newParent = ((DependencyObject)INavigatorHost).GetValue(NavigationServiceProperty) as NavigationService;

            if (newParent == oldParent)
                return;

            if (oldParent != null)
            {
                // Remove from old parent's list
                oldParent.RemoveChild(this);
            }

            if (newParent != null)
            {
                // Add to new parent's list
                newParent.AddChild(this);
                Debug.Assert(_parentNavigationService == newParent);
            }
        }

        internal void AddChild(NavigationService ncChild)
        {
            // This can happen when a Frame is navigated to the page containing it (object navigation).
            if (ncChild == this)
                throw new Exception(SR.Get(SRID.LoopDetected, _currentCleanSource));

            Invariant.Assert(ncChild.ParentNavigationService == null);
            Invariant.Assert(ncChild.JournalScope == null || ncChild.IsJournalLevelContainer,
                "Parentless NavigationService has a reference to a JournalNavigationScope its host navigator doesn't own.");

            ChildNavigationServices.Add(ncChild);
            ncChild._parentNavigationService = this;

            if (JournalScope != null)
            {
                // The view may need to be changed if NavigationContainers came or went
                JournalScope.Journal.UpdateView();
            }

            // If parent's navigation was stopped, stop pending navigations in the child as well
            if (this.NavStatus == NavigationStatus.Stopped)
            {
                ncChild.INavigatorHost.StopLoading();
                return;
            }

            // Add child to pendinglist if both child and parent are navigating
            if ((ncChild.NavStatus != NavigationStatus.Idle && ncChild.NavStatus != NavigationStatus.Stopped) &&
                (this.NavStatus != NavigationStatus.Idle && this.NavStatus != NavigationStatus.Stopped))
            {
                PendingNavigationList.Add(ncChild);
            }
        }

        internal void RemoveChild(NavigationService ncChild)
        {
            Debug.Assert(ChildNavigationServices.Contains(ncChild), "Child NavigationService must already exist");

            // Remove won't cause an exception if not in the arraylist
            ChildNavigationServices.Remove(ncChild);
            ncChild._parentNavigationService = null;
            if (!ncChild.IsJournalLevelContainer)
            {
                ncChild.InvalidateJournalNavigationScope();
            }

            if (JournalScope != null)
            {
                // The view may need to be changed if NavigationContainers came or went
                JournalScope.Journal.UpdateView();
            }

            // Do we need to stop navigations in the child?
            // If no, then just remove from our PendingNavigationList
            // If yes, replace the call below with StopLoading() which will remove from the pendinglist.
            if (PendingNavigationList.Contains(ncChild))
            {
                PendingNavigationList.Remove(ncChild);

                // Fire LoadCompleted if appropriate - i.e. if this was the final child we were waiting for
                HandleLoadCompleted(null);
            }
        }

        // We have been doing a depth first search, does it make more sense to do a breadth first
        // search for hyperlink targetting?
        /// <summary>
        /// Find the NavigationService in the tree rooted at -this- NavigationService
        /// </summary>
        /// <param name="navigationServiceId"></param>
        /// <returns></returns>
        internal NavigationService FindTarget(Guid navigationServiceId)
        {
            if (this.GuidId == navigationServiceId)
                return this;

            NavigationService result = null;
            foreach (NavigationService ns in ChildNavigationServices)
            {
                // Possible optimization: Don't recurse into a Frame with its own journal.
                result = ns.FindTarget(navigationServiceId);
                if (result != null)
                    return result;
            }

            return null;
        }

        /// <summary>
        /// Find the node with the given Navigator Name in this
        /// subtree rooted by this node. It is possible to have more than one
        /// node in the tree with the same Name, then we return the first one found
        /// </summary>
        /// <param name="name">the navigator Name to search</param>
        /// <returns>Navigator which matches the given id</returns>
        internal INavigatorBase FindTarget(string name)
        {
            FrameworkElement fe = INavigatorHost as FrameworkElement;

            Debug.Assert(fe != null, "INavigatorHost needs to be FrameworkElement");
            if (String.Compare(name, fe.Name, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return INavigatorHost;
            }

            INavigatorBase target = null;

            foreach (NavigationService xcChild in ChildNavigationServices)
            {
                target = xcChild.FindTarget(name);

                if (target != null)
                    return target;
            }

            return target;
        }

        // JournalEntry.KeepAlive value map to journal method
        //
        //      JournalEntry.KeepAlive          true                        false
        //      Navigation by Uri               by KeepAlive                by Uri
        //      Navigation by Object            by KeepAlive                by KeepAlive
        //      Navigation to PageFunction      by KeepAlive                by class
        //
        // Additional note:
        // 1. Return true for KeepAlive.
        // 2. When return false, it means by Uri for uri nav; by class for pagefunction.
        // 3. For object nav, always return true (KeepAlive).
        // 4. For null object/uri nav, always return true (KeepAlive).
        internal bool IsContentKeepAlive()
        {
            bool keepAlive = true;
            DependencyObject o = _bp as DependencyObject;

            // Anything with null Content or when Content is not DO is KeepAlive, since we can't get an attached
            // DP from a null reference.
            if (o != null)
            {
                // Get the content from the attached DP
                keepAlive = JournalEntry.GetKeepAlive(o);

                if (keepAlive == false)
                {
                    PageFunctionBase pf = o as PageFunctionBase;

                    // For object navigation, always return true (KeepAlive).
                    bool noUriToReloadFrom = !CanReloadFromUri;
                    if (pf == null && noUriToReloadFrom)
                    {
                        keepAlive = true;
                    }
                }
            }

            return keepAlive;
        }
        #endregion Internal Methods

        //
        // Set Uri to root element's BaseUri DependencyProperty.
        //
        private void SetBaseUri(DependencyObject dobj, Uri fullUri)
        {
            Invariant.Assert((dobj != null) && (! dobj.IsSealed));

            Uri curBaseUri;

            // If the BaseUri was set already, don't bother to reset it.
            // This could happen for navigating to element, and /or KeepAlive.

            curBaseUri = (Uri)(dobj.GetValue(BaseUriHelper.BaseUriProperty));

            if (curBaseUri == null && fullUri != null)
            {
                //
                // Get BaseUri from current Uri, and set it into root element of the new tree.
                //

                Uri baseUri = fullUri;
                dobj.SetValue(BaseUriHelper.BaseUriProperty, baseUri);
            }
        }

        private bool UnhookOldTree(Object oldTree)
        {
            //--------------------------------------------------------------------------------
            //
            // Step 1: Clear NavigationService property
            //
            DependencyObject dobj = oldTree as DependencyObject;

            // Currently there is no public API to seal a DO other than Freezable. In other
            // words, you can only seal Freezable. You cannot seal Visual, UIElement, FrameworkElement.
            // Since we enable navigation to any element, we should not crash when the object is sealed.
            if ((dobj != null) && (! dobj.IsSealed))
            {
                dobj.SetValue(NavigationServiceProperty, null);
            }
            //
            //--------------------------------------------------------------------------------



            //--------------------------------------------------------------------------------
            //
            // Step 2: Deal with Focus issues
            //
            // 1. Make sure that we remove keyboard focus from the old tree.
            // 2. The mouse will keep its reference until it detects it needs to re-hitttest.
            //    An example of such a case is when layout happens. Since the new navigation will cause
            //    a layout, we don't need to do anything specifically here.
            //
            // IInputElement.IsKeyboardFocusWithin works across subtrees as well so don't have to drill down subframes explicitly
            IInputElement iie = oldTree as IInputElement;
            if ((iie != null) && iie.IsKeyboardFocusWithin)
            {
                // We will need to set FocusedElement to null before setting Keyboard device focus to null.
                // The behavior for setting Keyboard device focus to null is setting to the root visual (e.g, NavWin);
                // the root element will then delegate it to the FocusedElement. If we do not set FocusedElement to null first,
                // Keyboard device will not set the focus to NavWin, but to the FocusedElement.

                // Ideally we should not need to do this. When a tree is removed focusedelement should be updated to null, keyboard device
                // should set the focus to root automatically. However Hyperlink does not have IsVisible property that heyboard
                // device checks to updated the focus. We will have to work around this issue.
                if (dobj != null && JournalScope != null)
                {
                    DependencyObject focusScope = (DependencyObject)INavigatorHost;
                    // If the NavigationHost is a focus scope, it is able to clear the FocusedElement.
                    // However, when it is not, we need to get the closest focus scope and clear the FocusedElement.
                    if (!((bool) focusScope.GetValue(FocusManager.IsFocusScopeProperty)))
                    {
                        focusScope = FocusManager.GetFocusScope(focusScope);
                    }
                    FocusManager.SetFocusedElement(focusScope, null);
                }
                Keyboard.PrimaryDevice.Focus(null);
            }
            //
            //--------------------------------------------------------------------------------



            //--------------------------------------------------------------------------------
            //
            // Step 3: Deal with PageFunction  issues
            //
            // Detach the Finish handler so we don't hold a reference to the PageFunction.
            PageFunctionBase currentPF = oldTree as PageFunctionBase;
            if (currentPF != null)
            {
                currentPF.FinishHandler = null;
            }
            //
            // Dispose the old tree here. TEMP until Bug 864908 is fixed
            // if the root is a PageFunction whose KeepAlive is set to TRUE, or a page
            // with JournalMode=KeepAlive, we should not dispose the old tree.
            bool canDispose = true;

            if (IsContentKeepAlive())
            {
                canDispose = false;
            }
            //
            //--------------------------------------------------------------------------------

            return canDispose;
        }

        /// <returns> False, if the navigation is canceled. This can currently happen only when
        /// a PageFunction returns to a non-PF parent page, and the Return event handler starts a
        /// new navigation. This case should be handled consistently with HandleFinish().
        /// </returns>
        private bool HookupNewTree(Object newTree, NavigateInfo navInfo, Uri newUri)
        {
            Debug.Assert(_navigateQueueItem == null && _navStatus == NavigationStatus.Navigated);

            // Restore the page state
            if (newTree != null && IsJournalNavigation(navInfo))
            {
                navInfo.JournalEntry.RestoreState(newTree);
                // Note: When a PageFunction is being resumed because its child finished, RestoreState()
                // is called earlier. Because it clears the JournalDataStreams, the call here will do
                // nothing.

                // Note: journalEntry.CustomContentState.Replay() is called from HandleNavigated().
            }

            //--------------------------------------------------------------------------------
            //
            // Step 1: Do PageFunction related stuff
            // This step is intentionally put as the first for event handler exception continuality:
            // if an exception occurs in the Return event handler, we would maintain a clean state.

            PageFunctionReturnInfo pfReturnInfo = navInfo as PageFunctionReturnInfo;
            // This will be non-null IFF a PageFunction with a non-PageFunction parent has finished.
            // Then navInfo.NavigationMode may be Back or New.
            // (New iff finishingChildPageFunction.RemoveFromJournal==false).
            PageFunctionBase finishingChildPageFunction = (pfReturnInfo != null) ? pfReturnInfo.FinishingChildPageFunction : null;
            Debug.Assert(finishingChildPageFunction == null ||
                !IsPageFunction(newTree) &&
                (finishingChildPageFunction.RemoveFromJournal && navInfo.NavigationMode == NavigationMode.Back ||
                 !finishingChildPageFunction.RemoveFromJournal && navInfo.NavigationMode == NavigationMode.New));

            // Reattach the Return Event handler and fire the child PageFunction's Return event
            // if we are about to switch to the non-PageFunction parent of a PageFunction that
            // has just finished
            if (finishingChildPageFunction != null)
            {
                object returnEventArgs = (pfReturnInfo != null) ? pfReturnInfo.ReturnEventArgs : null;

                if (newTree != null)
                {
                    FireChildPageFunctionReturnEvent(newTree, finishingChildPageFunction, returnEventArgs);

                    if (_navigateQueueItem != null)
                    {
                        // Return event handler should not be left attached.
                        Debug.Assert(finishingChildPageFunction._Return == null);

                        if (pfReturnInfo.JournalEntry != null)
                        {
                            pfReturnInfo.JournalEntry.SaveState(newTree);
                        }
                        return false;
                    }
                }
                // else
                // {
                //      If the parent was a page which was not rooted in a UIElement, its
                //      Return event handler will not fire. Right now this is not a problem because
                //      events cannot be attached in style. If and when we want to do this, we will
                //      have to remember the parameters to FireChildPageFunctionReturnEvent
                //      and will then have to fire it when Visuals are available.
                // }
            }

            // Note this special case: finishingChildPageFunction=null, but Content is PageFunctionBase.
            // This happens when navigating to a PF and then doing GoBack. Then the special
            // OnReturn/OnFinish PF handling is not done.

            if (IsPageFunction(newTree))
            {
                // Attach the handler to the new one so we know when it Finishes
                SetupPageFunctionHandlers(newTree);

                // If a page function is started without attaching a Return event handler to it,
                // it doesn't know which parent page to return to. So, set it here in this case.
                // (See also PageFunctionBase._AddEventHandler().)
                if ((navInfo == null || navInfo.NavigationMode == NavigationMode.New)
                    && !_doNotJournalCurrentContent) // the current PF may have been RemoveFromJournal'ed
                {
                    Debug.Assert(pfReturnInfo == null);
                    PageFunctionBase pf = (PageFunctionBase)newTree;
                    // pf._Resume=true when a PF returns and recording a new navigation for the parent PF
                    if (!pf._Resume && pf.ParentPageFunctionId == Guid.Empty && _bp is PageFunctionBase)
                    {
                        pf.ParentPageFunctionId = ((PageFunctionBase)_bp).PageFunctionId;
                        Debug.Assert(pf.ParentPageFunctionId != Guid.Empty);
                    }
                }
            }
            //
            //--------------------------------------------------------------------------------

            //--------------------------------------------------------------------------------
            //
            // Step 2: Set NavigationService property and WebBrowser
            //
            DependencyObject dobj = newTree as DependencyObject;
            if ((dobj != null) && (! dobj.IsSealed))
            {
                // Note: setting NavigationService has a non-obvious side effect -
                // if dobj has any data-bound properties that use ElementName binding,
                // the name will be resolved in the "inner scope", not the "outer
                // scope".  (Bug 1765041)
                dobj.SetValue(NavigationServiceProperty, this);

                // Set BaseUriHelper.BaseUriProperty.
                // Special case: When returning to a Source-less element tree in which fragment
                // navigation was done, newUri will be just "#fragment". Don't set it then.
                if (newUri != null && !BindUriHelper.StartWithFragment(newUri))
                {
                    SetBaseUri(dobj, newUri);
                }
            }

            _webBrowser = newTree as WebBrowser;
            //
            //--------------------------------------------------------------------------------

            return true;
        }

        /// <returns> whether to continue with committing the navigation to the new content </returns>
        private bool OnBeforeSwitchContent(Object newBP, NavigateInfo navInfo, Uri newUri)
        {
            Debug.Assert(IsConsistent(navInfo));

#if DEBUG_CLR_MEM
            bool clrTracingEnabled = false;

            if (CLRProfilerControl.ProcessIsUnderCLRProfiler &&
               (CLRProfilerControl.CLRLoggingLevel >= CLRProfilerControl.CLRLogState.Verbose))
            {
                clrTracingEnabled = true;
                ++_navigationCLRPass;
                CLRProfilerControl.CLRLogWriteLine("Begin_OnBeforeSwitchContent_{0}", _navigationCLRPass);
            }
#endif // DEBUG_CLR_MEM

            // The order of those actions are:
            // 1. Config the new tree, which includes two steps: PageFunction related stuff (where Child PageFunction's Return event is fired)
            //    and setting NavigationServiceProperty to this.
            // 2. Journal is updated with current page
            // 3. Clean up the old tree, which includes three steps: setting NavigationServiceProperty to null, setting focus to null, and
            //    PageFunction related stuff.
            // 4. Dispose the old tree if it can be disposed.
            // We intentionally fires the PageFunction Return event at the beginning for exception continuality: if an exception occurs in
            // the event handler, we would maintain a clean state.
            if (newBP != null && !HookupNewTree(newBP, navInfo, newUri))
            {
                Debug.Assert(!JournalScope.Journal.HasUncommittedNavigation);
                return false;
            }

            Debug.Assert(_navigateQueueItem == null);


            if (navInfo == null)
            {
                UpdateJournal(NavigationMode.New, JournalReason.NewContentNavigation, null);
            }
            else if (navInfo.NavigationMode != NavigationMode.Refresh)
            {
                UpdateJournal(navInfo.NavigationMode, JournalReason.NewContentNavigation, navInfo.JournalEntry);
            }

            // Future: 
            // The journal entry of the new page that is navigated to might be lost because the navigation is
            // cancelled after the current page being added to jounral. E.g, The journal looks like:
            //  Page1
            //  Page2
            //  Page1 <- current index
            //  Page2
            //  Page1
            // User clicks forward and then back. If reentrance happens with this code path, Page1 will be added
            // to journal twice. One Page2 journal entry will be lost.
            // We added the call (see above, GetTop) to browser before we update journal. So the chances of this
            // happening should be miminum. When it does go through this code path, we think that is better than
            // crashing the xbap. Post Orcas, we should investigate it further.
            if (_navigateQueueItem != null)
            {
                return false;
            }

            bool canDispose = UnhookOldTree(_bp);

#if DEBUG_CLR_MEM
            if (clrTracingEnabled && CLRProfilerControl.CLRLoggingLevel >= CLRProfilerControl.CLRLogState.Verbose)
            {
                CLRProfilerControl.CLRLogWriteLine("End_OnBeforeSwitchContent_{0}", _navigationCLRPass);
            }
#endif // DEBUG_CLR_MEM

            //
            // Dispose the old tree after all the required work is done.
            //
            if (canDispose)
            {
                DisposeTreeQueueItem disposeItem = new DisposeTreeQueueItem(_bp);
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(disposeItem.Dispatch), null);
            }

            return true;
        }

        /// <summary>
        ///     Called when style is actually applied.
        /// </summary>
        internal void VisualTreeAvailable(Visual v)
        {
            if (!ReferenceEquals(v, _oldRootVisual))
            {
                if (_oldRootVisual != null)
                {
                    // Step 1: Remove the inherited NavigationService property
                    // This will cause a property invalidation and sub-frames will remove themselves from the parent's list
                    // That will cause a Journal view update so back/fwd state reflects the state of the new tree
                    _oldRootVisual.SetValue(NavigationServiceProperty, null);
                }

                if (v != null)
                {
                    // Step 1: Set the inherited NavigationService property
                    // This will cause a property invalidation and sub-frames will remove themselves from the parent's list
                    // That will cause a Journal view update so back/fwd state reflects the state of the new tree
                    // Note: setting NavigationService has a non-obvious side effect -
                    // if v has any data-bound properties that use ElementName binding,
                    // the name will be resolved in the "inner scope", not the "outer
                    // scope".  (Bug 1765041)
                    v.SetValue(NavigationServiceProperty, this);
                }

                _oldRootVisual = v;
            }
        }

        #region IContentContainer Implementation

        /// <summary>
        /// The callback that happens when the bind product corresponding to a
        /// URI has been created.
        /// </summary>
        /// <param name="contentType">MIME type from which product was created</param>
        /// <param name="bp">Content created.</param>
        /// <param name="bpu">Absolute URI of content, or null</param>
        /// <param name="navState"></param>
        void IContentContainer.OnContentReady(ContentType contentType, Object bp, Uri bpu, Object navState)
        {
            Invariant.Assert(bpu == null || bpu.IsAbsoluteUri, "Content URI must be absolute.");
            if (IsDisposed)
            {
                return;
            }

            // If an invalid root element is passed to Navigation Service, throw exception here.
            if (IsValidRootElement(bp) == false)
            {
                throw new InvalidOperationException(SR.Get(SRID.WrongNavigateRootElement, bp.ToString()));
            }

            /*TODO: Uncomment after new loader design is implemented or sync bind reentrancy is resolved
            Debug.Assert(_navStatus == NavigationStatus.Navigating,
                         "Navigation State Machine is messed up, Expected: " + NavigationStatus.Navigating + "; Current: " + _navStatus);*/

            ResetPendingNavigationState(NavigationStatus.Navigated);

            NavigateInfo navInfo = navState as NavigateInfo;
            NavigationMode navMode = navInfo == null ? NavigationMode.New : navInfo.NavigationMode;

            Debug.Assert(bpu == null ||
                         navInfo == null ||
                         navInfo.Source == null ||
                         IsSameUri(null, navInfo.Source, bpu, false /* withFragment */),
                         "Source in OnContentReady does not match source in NavigateInfo");
            if (bpu == null)
            {
                bpu = (navInfo == null) ? null : navInfo.Source;
            }

            Uri bpuClean = BindUriHelper.GetUriRelativeToPackAppBase(bpu);

            // This gives the Application a chance to see if this bind needs an AppWindow.
            // This will happen if this is the first tree we're loading and it doesn't
            // have a toplevel Window element.
            if (PreBPReady != null)
            {
                // ok to pass resolved Uri here because this is internal
                BPReadyEventArgs args = new BPReadyEventArgs(bp, bpu);
                PreBPReady(this, args);
                if (args.Cancel)
                {
                    _navStatus = NavigationStatus.Idle;
                    return;
                }
            }

            bool objectRefresh = false;
            if (object.ReferenceEquals(bp, _bp))
            {
                Debug.Assert(navMode == NavigationMode.Refresh,
                    "OnContentReady() should not be called with the same object except for Refresh.");
                objectRefresh = true;
                // Note: The converse is not true: When refreshing from a URI, bp will be a different object.

                // To force full refresh, the Content object is detached from the tree and reattached.
                // (Just invalidating layout would not cause ContentRendered to be raised.)
                _bp = null;
                if (BPReady != null)
                {
                    BPReady(this, new BPReadyEventArgs(null, null));
                }
            }
            else
            {
                // send resolved Uri here because OnBeforeSwitchContent sets it as the new base uri
                if (!OnBeforeSwitchContent(bp, navInfo, bpu))
                {
                    Debug.Assert(!JournalScope.Journal.HasUncommittedNavigation);
                    return;
                }

                // On Refresh, keep the current ContentId. On journal navigation, restore it from the
                // journal entry instead of assigning a new one. This will ensure that fragment navigation
                // to other entries associated with the same content will still work properly.
                // (See Navigate(Uri, object).)
                if (navMode != NavigationMode.Refresh)
                {
                    if (navInfo == null || navInfo.JournalEntry == null) // new navigation?
                    {
                        _contentId++; // Note: this is done even when bp==null.
                        _journalEntryGroupState = null; // start anew
                    }
                    else
                    {
                        Debug.Assert(navMode == NavigationMode.Back || navMode == NavigationMode.Forward);

                        _contentId = navInfo.JournalEntry.ContentId;
                        Debug.Assert(_contentId != 0);

                        // The JournalEntryGroupState object from the JE must be reused because other JEs
                        // may have references to it.
                        Debug.Assert(_journalEntryGroupState == null ||
                            _journalEntryGroupState.ContentId != _contentId); // because _bp != bp
                        _journalEntryGroupState = navInfo.JournalEntry.JEGroupState;
                    }

                    // Set the source to the original source
                    _currentSource = bpu;
                    _currentCleanSource = bpuClean;
                }
            }

            _bp = bp;
            if (BPReady != null)
            {
                BPReady(this, new BPReadyEventArgs(_bp, bpu));
            }

            // This will fire Navigated event and LoadCompleted event if all sub-loads are done
            HandleNavigated(navState, !objectRefresh/*navigatedToNewContent*/);
        }

        // <summary>
        // Function that gets called each time number of bytes equal to
        // bytesInterval is read
        // </summary>
        // <param name="sourceUri">Uri for which the progress event is being fired</param>
        // <param name="bytesRead">Bytes Read</param>
        // <param name="maxBytes">Max Bytes</param>
        void IContentContainer.OnNavigationProgress(Uri sourceUri, long bytesRead, long maxBytes)
        {
            if (IsDisposed)
            {
                return;
            }

            // LoadXaml/LoadBaml cannot be aborted when the loading is not async.
            // The currrent navigation could have been cancelled by the application
            // in the NavigationProgress event handler. We should not raise the NavigationProgress event
            // when the uri we start with is not the same as the current one that we are navigating to.
            if (!sourceUri.Equals(Source))
            {
                return;
            }

            NavigationService rootNavigationService = null;

            // Fire with cumulative totals at the top level container also unless this is the top level one.
            if (JournalScope != null && JournalScope.RootNavigationService != this)
            {
                rootNavigationService = JournalScope.RootNavigationService;

                // Update cumulative totals on the Window. Do this before we update current totals
                rootNavigationService.BytesRead += bytesRead - _bytesRead;
                rootNavigationService.MaxBytes += maxBytes - _maxBytes;
            }

            // We get cumulative bytesRead and maxBytes from Loader. maxBytes -may- get
            // updated dynamically if ContentLength was not known beforehand
            // When bytesRead == maxBytes, then we know that the download is done
            _bytesRead = bytesRead;
            _maxBytes = maxBytes;

            // FireNavigationProgress for this container, this will also fire on the application
            // with this container's progress bytes
            FireNavigationProgress(sourceUri);

            // Fire with cumulative totals at the top level container also unless this is the top level one.
            if (rootNavigationService == null)
                return;

            // Since we are using rootUri, fire with root's INavigatorHost
            rootNavigationService.FireNavigationProgress(sourceUri);

            // If this navigation gets Stopped or finishes completely, this containers cumulative totals
            // will get decremented from the root container in ResetPendingNavigationState()
        }

        void IContentContainer.OnStreamClosed(Uri sourceUri)
        {
            // LoadXaml/LoadBaml cannot be aborted when the loading is not async.
            // The currrent navigation could have been cancelled by the application
            // in the NavigationProgress event handler. We should not raise the LoadCompleted event
            // when the uri we start with is not the same as the current one that we are navigating to.
            if (!sourceUri.Equals(Source))
            {
                return;
            }

            // Cannot close the WebResponse here because we hand out the response in the Navigated and LoadCompleted event args.
            // Have to wait to close it until then. The stream has been closed though.

            // If it was async parsing, it is finished when we get this call.
            _asyncObjectConverter = null;
            HandleLoadCompleted(null);
        }

        #endregion IContentContainer Implementation

        # region public method and property
        /// <summary>
        /// Attached inherited DependencyProperty. It gives an element the NavigationService of the navigation container it's in.
        /// </summary>
        internal static readonly DependencyProperty NavigationServiceProperty =
                DependencyProperty.RegisterAttached(
                        "NavigationService",
                        typeof(NavigationService),
                        typeof(NavigationService),
                        new FrameworkPropertyMetadata(
                                (NavigationService)null,
                                FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// Gets NavigationService of the navigation container the given dependencyObject is in.
        /// </summary>
        /// <param name="dependencyObject"></param>
        /// <returns></returns>
        public static NavigationService GetNavigationService(DependencyObject dependencyObject)
        {
            if (dependencyObject == null)
            {
                throw new ArgumentNullException("dependencyObject");
            }

            return dependencyObject.GetValue(NavigationServiceProperty) as NavigationService;
        }


        #region INavigator Implementation
        //
        // Uri INavigator.Source
        //
        /// <summary>
        /// Source Uri
        /// </summary>
        /// <value></value>
        public Uri Source
        {
            get
            {
                if (IsDisposed)
                {
                    return null;
                }

                if (_recursiveNavigateList.Count > 0)
                {
                    // If we are in the middle of a recursive Navigate call (could happen if Navigating
                    // event handler called Navigate), then return the Uri from the deepest callstack
                    return BindUriHelper.GetUriRelativeToPackAppBase((_recursiveNavigateList[_recursiveNavigateList.Count - 1] as NavigateQueueItem).Source);
                }
                else if (_navigateQueueItem != null)
                {
                    // Else return the Uri from the queued item (could still be waiting to be posted
                    // or in progress)
                    return BindUriHelper.GetUriRelativeToPackAppBase(_navigateQueueItem.Source);
                }
                else
                {
                    // Return the one and only
                    return _currentCleanSource;
                }
            }
            set
            {
                // IsDisposed is checked in Navigate()
                this.Navigate(value);
            }
        }

        //
        // Uri INavigator.CurrentSource
        //
        /// <summary>
        /// Current Source Uri
        /// </summary>
        /// <value></value>
        public Uri CurrentSource
        {
            get
            {
                if (IsDisposed)
                {
                    return null;
                }

                return _currentCleanSource;
            }
        }

        //
        // UIElement INavigator.Content
        //
        /// <summary>
        /// Current Content property
        /// </summary>
        /// <value></value>
        public Object Content
        {
            get
            {
                if (IsDisposed)
                {
                    return null;
                }

                return _bp;
            }
            set
            {
                // IsDisposed is checked in Navigate()
                this.Navigate(value);
            }
        }

        /// <summary>
        /// Adds a new journal entry to NavigationWindow's back history.
        /// </summary>
        /// <param name="state"> The custom content state (or view state) to be encapsulated in the
        /// journal entry. If null, IProvideCustomContentState.GetContentState() will be called on
        /// the NavigationWindow.Content or Frame.Content object.
        /// </param>
        public void AddBackEntry(CustomContentState state)
        {
            if (IsDisposed)
            {
                return;
            }
            if (_bp == null)
                throw new InvalidOperationException(SR.Get(SRID.InvalidOperation_AddBackEntryNoContent));

            _customContentStateToSave = state;
            JournalEntry je = UpdateJournal(NavigationMode.New, JournalReason.AddBackEntry, null);
            // Controls state is not saved by design (saveContent=false). If client applications
            // require it to be synchronized with the CustomContentState, they can explicitly
            // include it.

            _customContentStateToSave = null;

            // Since state=null is allowed on input, make sure we get an object either via the
            // IProvideCustomContentState interface or from a Navigating event handler.
            // Otherwise it doesn't make sense to add a journal entry.
            if (je != null && je.CustomContentState == null)
            {
                RemoveBackEntry();
                throw new InvalidOperationException(
                    SR.Get(SRID.InvalidOperation_MustImplementIPCCSOrHandleNavigating,
                            _bp != null ? _bp.GetType().ToString() : "null"));
            }
        }

        /// <summary>
        /// Remove the first JournalEntry from NavigationWindow's back history
        /// </summary>
        public JournalEntry RemoveBackEntry()
        {
            if (IsDisposed)
            {
                return null;
            }
            if (JournalScope == null)
                return null; //(Normally, no exception is thrown if there is no back entry.)
            return JournalScope.RemoveBackEntry();
        }

        //
        // bool INavigator.Navigate(Uri source)
        //
        /// <summary>
        /// Navigate to source
        /// </summary>
        /// <value></value>
        public bool Navigate(Uri source)
        {
            return this.Navigate(source, null, false, false);
        }

        //
        // bool INavigator.Navigate(UIElement bp)
        //
        /// <summary>
        /// Navigate to content tree.
        /// </summary>
        /// <value></value>
        public bool Navigate(Object root)
        {
            return this.Navigate(root, null);
        }

        /// <summary>
        /// Navigate to the source. Null source results in clearing existing content
        /// </summary>
        /// <value>returns bool to indicate if a navigation was started i.e. Navigating event was not cancelled</value>
        public bool Navigate(Uri source, Object navigationState)
        {
            return this.Navigate(source, navigationState, false, false);
        }

        /// <summary>
        /// Navigate to the source. Null source results in clearing existing content
        /// </summary>
        /// <value>returns bool to indicate if a navigation was started i.e. Navigating event was not cancelled</value>
        public bool Navigate(Uri source, Object navigationState, bool sandboxExternalContent)
        {
            return Navigate(source, navigationState, sandboxExternalContent, false);
        }

        /// <summary>
        /// Navigate to the source. Null source results in clearing existing content
        /// </summary>
        /// <value>returns bool to indicate if a navigation was started i.e. Navigating event was not cancelled</value>
#pragma warning disable 6506  // Both source and navigationState can accept null as valid input.
        internal bool Navigate(Uri source, Object navigationState, bool sandboxExternalContent, bool navigateOnSourceChanged)
        {
            if (IsDisposed)
            {
                return false;
            }

            NavigateInfo navInfo = navigationState as NavigateInfo;

            if (EventTrace.IsEnabled(EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info))
            {
                EventTrace.EventProvider.TraceEvent(
                    EventTrace.Event.Wpf_NavigationStart, EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info,
                    navInfo != null ? navInfo.NavigationMode.ToString() : NavigationMode.New.ToString(),
                    source != null ? "\"" + source.ToString() + "\"" : "(null)");
            }

            Invariant.Assert(IsConsistent(navInfo));

            WebRequest newRequest = null;
            bool isFragment = false;
            Uri resolvedSource = null;

            if (source != null)
            {
                // If it's fragment, we will need to resolve with _currentSource,
                // because BaseUri doesn't contain the last part of the path: filename,
                // and fragment navigation's context should be currentSource.
                // If previous navigation is object navigation, the _currentsource is null, the fragment
                // navigation uri can be pack://application,,,/#fragment, so check GetUriRelativeToPackAppBase(source).
                if (BindUriHelper.StartWithFragment(source) ||
                    BindUriHelper.StartWithFragment(BindUriHelper.GetUriRelativeToPackAppBase(source)))
                {
                    resolvedSource = BindUriHelper.GetResolvedUri(_currentSource, source);
                    isFragment = true;
                }
                else
                {
                    resolvedSource = BindUriHelper.GetResolvedUri(source);
                    // Special case (bugs 1187603 & 1187613): Navigating back/fwd to a different instance
                    // of the current page. Then it's not fragment navigation. The test below
                    // distinguishes back/fwd navigations within the current page from navigations
                    // between two different instances of the same page (URI).
                    isFragment = (navInfo == null || navInfo.JournalEntry == null
                                   || navInfo.JournalEntry.ContentId == _contentId)
                        && IsSameUri(null, resolvedSource, _currentSource, false /* without Fragment */);
                }

                // If this is a refresh, we want to refresh the whole page so set isFragment to false
                // so we renavigate the whole page.
                if ((navInfo != null && navInfo.NavigationMode == NavigationMode.Refresh))
                {
                    isFragment = false;
                }

                // If it's Uri navigation, we allow user to configure the webrequest in Navigating event.
                // So we create the WebRequest here and pass it in event args.
                // If source != null or it's not fragment navigation, we need to create a webrequest
                if (!isFragment)
                {
                    newRequest = CreateWebRequest(resolvedSource, navInfo);

                    //
                    // Check for unable to create a WebRequest.
                    // May have delegated back to browser for x-domain case.
                    // (`http:// only, not file--see CreateWebRequest()).
                    //
                    if (newRequest == null)
                        return false;
                }
            }

            // HandleNavigating will call DoStopLoading which aborts current webrequest if there is any.
            if (HandleNavigating(resolvedSource, null, navigationState, newRequest, navigateOnSourceChanged) == false)
            {
                return false;
            }

            // Short-circuit re-navigating to null. This should be done after HandleNavigating because
            // there might be a pending navigation to cancel first.
            if (source == null && _bp == null)
            {
                ResetPendingNavigationState(NavigationStatus.Idle);
                return true;
            }

            // If we're navigating within the same file, try to just scroll or page
            // the right element into view
            if (isFragment)
            {
                NavigateToFragmentOrCustomContentState(resolvedSource, navigationState);
                return true;
            }

            // Post the navigate Dispatcher operation
            _navigateQueueItem.PostNavigation();

            return true;
        }

        private void InformBrowserAboutStoppedNavigation()
        {
            if (Application != null && Application.CheckAccess())
            {
                Application.PerformNavigationStateChangeTasks(true, false, Application.NavigationStateChange.Stopped);
            }
        }

#pragma warning restore 6506

        //
        // bool Navigate(Object root, Object navigationState)
        //
        /// <summary>
        /// Navigate to content tree. Async state can be passed across the navigation
        /// and can be retrieved from the Navigation events.
        /// </summary>
        /// <value></value>
#pragma warning disable 6506  // Both root and navigationState can accept null as vaild input.
        public bool Navigate(Object root, Object navigationState)
        {
            if (IsDisposed)
            {
                return false;
            }

            NavigateInfo navigateInfo = navigationState as NavigateInfo;

            if (EventTrace.IsEnabled(EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info))
            {
                EventTrace.EventProvider.TraceEvent(
                    EventTrace.Event.Wpf_NavigationStart, EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info,
                    navigateInfo != null ? navigateInfo.NavigationMode.ToString() : NavigationMode.New.ToString(),
                    root != null ? root.ToString() : "(null)");
            }

            Invariant.Assert(IsConsistent(navigateInfo));

            // Prevent re-starting the same PageFunction object before it has returned first.
            if (navigateInfo == null) // not called internally, from NavigateToParentPage()
            {
                PageFunctionBase pf = root as PageFunctionBase;
                // This won't detect the case when no Return event handler was attached, but then
                // we don't run the risk of overwriting the ReturnEventSaver.
                if (pf != null && (pf._Resume || pf._Saver != null))
                    throw new InvalidOperationException(SR.Get(SRID.InvalidOperation_CannotReenterPageFunction));
            }

            Uri source = navigateInfo == null ? null : navigateInfo.Source;

            // HandleNavigating will set the pending Uri from navigationState if available
            // See comments in NavigateInfo class
            if (HandleNavigating(source, root, navigationState, null, false) == false)
            {
                return false;
            }

            // root==_bp occurs in these cases:
            //   - Navigate(object) was called with the current Content object. This is handled as fragment
            //      navigation, scrolling content to top.
            //   - Refresh(). We'll go through the entire navigation sequence.
            //   - Going back/fwd to a journal entry associated with the same object. This is also handled
            //      as fragment navigation.
            if (object.ReferenceEquals(root, _bp) && (navigateInfo == null || navigateInfo.NavigationMode != NavigationMode.Refresh))
            {
                NavigateToFragmentOrCustomContentState(source, navigationState);

                // Special case: Non-consecutive navigations to the same content object will create
                // different journal entry groups (with different ContentIds). On journal navigation,
                // the right state has to be restored. This is done after updating the journal so that
                // the journal entry created for the previous position in the journal is associated
                // with the right JournalEntryGroupState.
                if (IsJournalNavigation(navigateInfo))
                {
                    _journalEntryGroupState = navigateInfo.JournalEntry.JEGroupState;
                    _contentId = _journalEntryGroupState.ContentId;

                    // The JournalEntryStacks need to be invalidated after changing _contentId. (Bug 1613984)
                    _journalScope.Journal.UpdateView();
                }

                return true;
            }

            // Post the navigate Dispatcher operation
            _navigateQueueItem.PostNavigation();

            return true;
        }

#pragma warning restore 6506

        //
        // bool INavigator.CanGoForward
        //
        /// <summary>
        /// Property to determine if current NavigationWindow's CanGoForward is enabled
        /// </summary>
        /// <value></value>
        public bool CanGoForward
        {
            get { return JournalScope != null && JournalScope.CanGoForward; }
        }

        //
        // bool INavigator.CanGoBack
        //
        /// <summary>
        /// Property to determine if current NavigationWindow's CanGoBack is enabled
        /// </summary>
        /// <value></value>
        public bool CanGoBack
        {
            get { return JournalScope != null && JournalScope.CanGoBack; }
        }

        //
        // bool INavigator.GoForward()
        //
        /// <summary>
        /// Navigate to the next entry in the Journal
        /// </summary>
        /// <value></value>
        public void GoForward()
        {
            if (JournalScope == null)
                throw new InvalidOperationException(SR.Get(SRID.NoForwardEntry));
            JournalScope.GoForward();
        }

        //
        // bool INavigator.GoBack
        //
        /// <summary>
        /// Navigate to the next entry in the Journal
        /// </summary>
        /// <value></value>
        public void GoBack()
        {
            if (JournalScope == null)
                throw new InvalidOperationException(SR.Get(SRID.NoBackEntry));
            JournalScope.GoBack();
        }

        //
        // void INavigator.StopLoading()
        //

        /// <summary>
        /// StopLoading aborts asynchronous navigations that haven't been processed yet or that are
        /// still being downloaded. SopLoading does not abort parsing of the downloaded streams.
        /// The NavigationStopped event is fired only if the navigation was aborted.
        /// </summary>
        /// <value></value>
        public void StopLoading()
        {
            // Not checking for IsDisposed since that checks for app shutdown too and we need to
            // stop current loads during app shutdown

            DoStopLoading(true/*clearRecursiveNavigations*/, true/*fireEvents*/);
        }

        /// <summary>
        /// Stop navigations that are in progress in current and child containers.
        /// DoStopLoading is called from HandleNavigating and the public StopLoading.
        /// When called from the former it will be to stop a previous navigation in progress
        /// but not the source for which Navigating event is being fired.If StopLoading was called from
        /// any of the events raised from the Navigating call, then we will abort loading even
        /// the one Navigating event is being fired for
        /// </summary>
        private void DoStopLoading(bool clearRecursiveNavigations, bool fireEvents)
        {
            // Note that this will fire the event top down.
            // Stop binds and fire the NavigationStopped event only if there was a pending navigation
            bool fireStopped = false;
            object extraData = null;

            //
            // This method is called when the navigation is stopped/cancelled, or
            // when a new navigation is started.
            //
            // If the new navigation is started in the Navigated event handler for previous
            // navigation, in some case, it might suppress the LoadCompleted event. Then
            // the WebResponse object for the previous navigation might not be cleaned up.
            //
            // So moving below cleanup code here to make sure it always clean up the
            // webresponse object no matter if _navigateQueueItem is set or not.
            //

            // Stop parsing first. It might be async parsing.
            if (_asyncObjectConverter != null)
            {
                _asyncObjectConverter.CancelAsync();
                _asyncObjectConverter = null;

                // _webResponse cannot be null for async parsing.
                Invariant.Assert(_webResponse != null);
                _webResponse.Close();
                _webResponse = null;
            }
            // If _asyncObjectConverter is null, it means we called XamlReader.LoadBaml,
            // which we cannot stop. It is sync operation. We get here when StopLoading is called or a
            // new navigation is started in NavigationProgress event handler. Parser still holds on to the stream.
            // We will have to wait for parsing to finish. In GetObjectFromResponse when the baml loading
            // call returns we will check whether the navigation has been cancelled. If it has, we will
            // do the cleaning up. So only close the _webResponse when we are navigated.
            else if ((_navStatus != NavigationStatus.Navigating) && (_webResponse != null))
            {
                _webResponse.Close();
                _webResponse = null;
            }

            // Change the state whether we have pending navigations or not because
            // the child NavigationServices will stop their navigations when trying to add
            // themselves as children and see that the parent navigation has been stopped.
            _navStatus = NavigationStatus.Stopped;

            if (_navigateQueueItem != null)
            {
                _navigateQueueItem.Stop();

                if (JournalScope != null)
                {
                    // When a navigation is started, this method is called, with
                    // clearRecursiveNavigations=false. In such a case the Journal shouldn't be
                    // reset. If it is and GoBack or GoForward occurs while the new navigation is
                    // underway, the wrong journal entry will be selected. (Previously, GoFwd
                    // worked when issued twice without waiting, but not when issued more than twice,
                    // because the journal was reset.)
                    if (clearRecursiveNavigations)
                    {
                        JournalScope.AbortJournalNavigation();
                    }
                }

                // _request can be null for object navigation
                if (_request != null)
                {
                    // Abort the WebRequest
                    try
                    {
                        // WebRequest.Abort() wants to call the AsyncCallback that was passed to
                        // BeginGetResponse(). Our HandleWebResponse() has to know that the request was
                        // aborted. That's why _request is cleared before calling Abort(). 
                        WebRequest request = _request;
                        _request = null;
                        request.Abort();
                    }
                    //These catch stmts are by design. We don't know what WebRequest object
                    //we will end up with and which support Abort and which don't. These are
                    //not fatal errors so we safely ignore them.

#pragma warning disable 6502
                    //Documented exception thrown by this method
                    catch (NotSupportedException)
                    {
                    }
                    //This is what we really see, so catching both
                    catch (NotImplementedException)
                    {
                    }
#pragma warning restore 6502
                }

                extraData = _navigateQueueItem.NavState;
                ResetPendingNavigationState(NavigationStatus.Stopped);
                fireStopped = true;
            }

            if (clearRecursiveNavigations && _recursiveNavigateList.Count > 0)
            {
                _recursiveNavigateList.Clear();
                fireStopped = true;
            }

            if (_navigatorHostImpl != null)
            {
                _navigatorHostImpl.OnSourceUpdatedFromNavService(true /* journalOrCancel */);
            }

            // Event handler exception continuality: if exception occurs in NavigationStopped event handler,
            // we want to finish stopping navigation.
            bool succeed = false;
            try
            {
                if (fireEvents && fireStopped)
                {
                    FireNavigationStopped(extraData);
                }
                succeed = true;
            }
            finally
            {
                // Event handler exception continuality: when trying to stop child navigation, if exception occurs for one child
                // we want to continue to stop the rest child navigations.
                int i = 0;
                try
                {
                    // Stop all binds in the children NavigationServices
                    // Not using the PendingNavigationList here because the child containers will add themselves
                    // to the parent's list only if the navigation was started at the parent level.
                    // But Stop invoked on the parent level should stop all navigations in the child tree as well
                    // whether or not the parent itself is navigating
                    for (; i < _childNavigationServices.Count; ++i)
                    {
                        // if there is an exception (succeed == false), we want to stop children's loading without
                        // firing the events.
                        ((NavigationService)_childNavigationServices[i]).DoStopLoading(true, succeed/*fireEvent: we only fire when succeed*/);
                    }
                }
                finally
                {
                    // If i+1 is less then the total count, it means that exception occurs in the number i child StopLoading,
                    // we should finish stoploading for the rest of children without firing any events.
                    if (++i < _childNavigationServices.Count)
                    {
                        for (; i < _childNavigationServices.Count; ++i)
                        {
                            ((NavigationService)_childNavigationServices[i]).DoStopLoading(true, false/*fireEvents*/);
                        }
                    }

                    // We don't need to recursively fire on all XC's in the PendingNavigationList
                    // If they added themselved to the list here, then they must be hooked up, so
                    // the recursive call above to stop binds in the children XCs should take care of it.
                    // The assert is to find any scenarios I missed.
                    Debug.Assert(PendingNavigationList.Count == 0,
                                 "Navigations in child containers have not been stopped");

                    // Incase the Loader did not notify about bind errors (eg. exceptions that
                    // were not caught by Loader when aborting the binds) then the List will never
                    // be cleared. So clean it up here to be on the safe side
                    // The assert above is for catching these conditions during development so we can fix them
                    PendingNavigationList.Clear();

                    if (_parentNavigationService != null)
                    {
                        if (_parentNavigationService.PendingNavigationList.Contains(this))
                        {
                            _parentNavigationService.PendingNavigationList.Remove(this);

                            if (fireEvents)
                            {
                                // Fire LoadCompleted on the parent if appropriate.
                                // This will happen if the navigation was started at the parent level
                                // and navigation in this frame was stopped.
                                _parentNavigationService.HandleLoadCompleted(null);
                            }
                        }
                    }
                }
            }
        }

        //
        // void INavigator.Refresh()
        //
        /// <summary>
        /// Refresh the current content
        /// </summary>
        /// <value></value>
        public void Refresh()
        {
            if (IsDisposed)
            {
                return;
            }

            //OK to use _currentCleanSource, the Navigate codepath will take care of
            //handing out relative uri to events

            // Any pending navigations are first stopped before the page is refreshed
            if (CanReloadFromUri)
            {
                Navigate(_currentSource, new NavigateInfo(_currentSource, NavigationMode.Refresh));
            }
            else if (_bp != null)
            {
                // Content refreshes are usually a no-op. We will go through the motions of the navigation
                // and fire the appropriate events so developers can take appropriate action eg, clearing
                // user input etc.  This will also stop any pending navigations
                Navigate(_bp, new NavigateInfo(_currentSource, NavigationMode.Refresh));
            }
        }


        /// <summary>
        /// This event is fired when an error is encountered during a navigation
        /// </summary>
        public event NavigationFailedEventHandler NavigationFailed;

        //
        //  INavigator.Navigating
        //
        /// <summary>
        /// event NavigatingCancelEventHandler NavigationService.Navigating
        /// </summary>
        /// <value></value>
        public event NavigatingCancelEventHandler Navigating
        {
            add { _navigating += value; }
            remove { _navigating -= value; }
        }

        NavigatingCancelEventHandler _navigating;

        /// <summary>
        /// Fires the Navigating event and returns a bool to indicate whether a navigation is
        /// allowed or not
        /// </summary>
        /// <param name="source"></param>
        /// <param name="bp"></param>
        /// <param name="navState"></param>
        /// <param name="request"></param>
        /// <returns>bool indicating whether the Navigating is allowed or not</returns>
        private bool FireNavigating(Uri source, Object bp, Object navState, WebRequest request)
        {
            NavigateInfo navigateInfo = navState as NavigateInfo;
            Uri cleanSource = BindUriHelper.GetUriRelativeToPackAppBase(source);

            // For Application's startup Uri case, we navigate in NavigationService created in
            // ether then if there was no window tag, we create a new NavigationWindow and navigate in it
            // with the Content that was already created from the StartupUri navigation. This will cause
            // the Navigating event to fire a second time. So don't fire it a second time here
            // This or avoid firing the event if this container is App Startup container
            // and let the window fire the events instead. This means the first Navigating event will
            // be a little delayed and the user won't have a chance to cancel the Navigating event
            // until we already downloaded.
            if (bp != null &&
                navigateInfo != null &&
                !(navigateInfo is PageFunctionReturnInfo ||
                    bp is PageFunctionBase && (bp as PageFunctionBase)._Resume) &&
                navigateInfo.Source != null &&
                navigateInfo.NavigationMode == NavigationMode.New)
            {
                // This should happen only for the Application case when processing the Startup Uri
                Debug.Assert(this.Application != null &&
                             this.Application.CheckAccess() == true &&
                             IsSameUri(null, Application.StartupUri,
                                                     navigateInfo.Source, false /* withFragment */),
                             "Encountered unexpected condition in FireNavigating, see comments in the file");
                // Only allow this navigation to continue if the user has not
                // reqeusted another navigation in the mean time.
                return _navigateQueueItem == null;
            }

            CustomContentState customContentState =
                (navigateInfo != null && navigateInfo.JournalEntry != null) ? navigateInfo.JournalEntry.CustomContentState : null;
            // do not expose navState if it is NavigateInfo
            object extraData = navigateInfo == null ? navState : null;
            NavigatingCancelEventArgs e = new NavigatingCancelEventArgs(
                                                            cleanSource,
                                                            bp,
                                                            customContentState,
                                                            extraData,
                                                            navigateInfo == null ? NavigationMode.New : navigateInfo.NavigationMode,
                                                            request,
                                                            INavigatorHost,
                                                            IsNavigationInitiator);

            if (_navigating != null)
            {
                _navigating(INavigatorHost, e);
            }
            if (!e.Cancel && this.Application != null && this.Application.CheckAccess())
            {
                this.Application.FireNavigating(e, _bp == null);
            }

            // If this is null, the IProvideCustomContentState callback will be used later on.
            _customContentStateToSave = e.ContentStateToSave;

            if (e.Cancel)
            {
                if (JournalScope != null)
                {
                    JournalScope.AbortJournalNavigation();
                }
            }

            return (!e.Cancel && !IsDisposed);
        }

        // returns whether or not to navigate
        private bool HandleNavigating(Uri source, Object content, Object navState, WebRequest newRequest, bool navigateOnSourceChanged)
        {
            NavigateInfo navigateInfo = navState as NavigateInfo;

            if (navigateInfo != null)
            {
                Debug.Assert(navigateInfo.IsConsistent);
                Debug.Assert(source == null ||
                             navigateInfo.Source == null ||
                             IsSameUri(null, navigateInfo.Source, source, false /* withFragment */),
                             "Source argument does not match NavigateInfo.Source");
                // Don't want to overwrite one passed in
                if (source == null)
                {
                    source = navigateInfo.Source;
                }
            }

            NavigateQueueItem localNavigateQueueItem = new NavigateQueueItem(source,
                                                                             content,
                                                                             navigateInfo != null ? navigateInfo.NavigationMode : NavigationMode.New,
                                                                             navState,
                                                                             this);

            // Set the pending state. _navigateQueue item may get overwritten in a recursive StopLoading
            // or Navigate call (called from FireNavigating). If so then we need to cancel this navigation
            // since the last StopLoading and Navigate call will supercede this call. We need to cancel
            // this navigation is such a case even if this event was not explicitly cancelled
            _recursiveNavigateList.Add(localNavigateQueueItem);

            // For each new navigation we need to re-determine if we are the initial navigator
            _isNavInitiatorValid = false;

            // If this is not a navigation started by Source DP change, we notify the INavigatorHost
            // that source changed.
            if ((_navigatorHostImpl != null) && (!navigateOnSourceChanged))
            {
                _navigatorHostImpl.OnSourceUpdatedFromNavService(IsJournalNavigation(navigateInfo) /* journalOrCancel */);
            }

            // Event handler exception continuality: if exception occurs in Navigating event handler, the cleanup action is
            // the same as the event being cancelled.
            bool allowNavigation = false;
            try
            {
                allowNavigation = FireNavigating(source, content, navState, newRequest);
            }
            catch
            {
                CleanupAfterNavigationCancelled(localNavigateQueueItem);

                throw;
            }

            if (allowNavigation == true)
            {
                DoStopLoading(false /*clearRecursiveLoads*/, true /*fireEvents*/);
                Debug.Assert(PendingNavigationList.Count == 0,
                             "Pending child navigations were not stopped before starting a new navigation");

                // NavigationStopped event handler could have caused a new navigation.
                if (_recursiveNavigateList.Contains(localNavigateQueueItem) == false)
                    return false;

                _recursiveNavigateList.Clear();

                // Continue with the navigation
                Debug.Assert(_navigateQueueItem == null, "Previous nav queue item should be cleared by now.");
                _navigateQueueItem = localNavigateQueueItem;

                _request = newRequest;

                _navStatus = NavigationStatus.Navigating;
            }
            else
            {
                CleanupAfterNavigationCancelled(localNavigateQueueItem);
            }

            return allowNavigation;
        }

        private void CleanupAfterNavigationCancelled(NavigateQueueItem localNavigateQueueItem)
        {
            if (JournalScope != null)
            {
                JournalScope.AbortJournalNavigation();
            }

            // If event was canceled then we need to remove it.
            // If the event was canceled AND superceded by StopLoading or Navigate, it won't be
            // in the list but Remove won't throw an exception so not doing an if check here
            // Don't clear the whole list here since this could be an intermediate Navigate in a recursive callstack
            // and the caller could now proceed with the navigation
            _recursiveNavigateList.Remove(localNavigateQueueItem);

            if (_navigatorHostImpl != null)
            {
                _navigatorHostImpl.OnSourceUpdatedFromNavService(true /* journalOrCancel */);
            }

            // Browser downloading state not reset; case 4.
            InformBrowserAboutStoppedNavigation();
        }

        //
        // INavigator.Navigated
        //
        /// <summary>
        /// event NavigatedEventHandler NavigationService.Navigated
        /// </summary>
        /// <value></value>
        public event NavigatedEventHandler Navigated
        {
            add { _navigated += value; }
            remove { _navigated -= value; }
        }

        NavigatedEventHandler _navigated;

        private void FireNavigated(object navState)
        {
            // do not expose navState if it is NavigateInfo
            object extraData = navState is NavigateInfo ? null : navState;

            // Event handler exception continuality: if exception occurs in Navigated event handler, the cleanup action is
            // the same as StopLoading().
            try
            {
                // How will be know the navigationInitiator here to create NavigationEventArgs with?
                NavigationEventArgs e = new NavigationEventArgs(CurrentSource, Content, extraData, _webResponse, INavigatorHost, IsNavigationInitiator);

                if (_navigated != null)
                {
                    _navigated(INavigatorHost, e);
                }

                // Fire it on the Application
                if (this.Application != null && this.Application.CheckAccess())
                {
                    this.Application.FireNavigated(e);
                }
            }
            catch
            {
                DoStopLoading(true, false);

                throw;
            }
        }

        private void HandleNavigated(object navState, bool navigatedToNewContent)
        {
            Debug.Assert(_navStatus == NavigationStatus.Navigated);
            BrowserInteropHelper.IsInitialViewerNavigation = false;

            NavigateInfo navInfo = navState as NavigateInfo;

            // For scrolling to #fragment and for restoring root viewer state, the FC/FCE.Loaded event
            // is preferably used. (It occurs before first rendering.) If _bp is neither FE nor FCE,
            // we fall back to ContentRendered (wired in the INavigatorHost setter).
            bool handleContentLoadedEvent = false;
            if (navigatedToNewContent && _currentSource != null)
            {
                // Scrolling to named target element may not succeed before first layout is done.
                string fragment = BindUriHelper.GetFragment(_currentSource);
                handleContentLoadedEvent = !string.IsNullOrEmpty(fragment);
            }

            if (navInfo != null && navInfo.JournalEntry != null) // Was this journal navigation?
            {
                JournalEntry je = navInfo.JournalEntry;
                if (je.CustomContentState != null)
                {
                    je.CustomContentState.Replay(this, navInfo.NavigationMode);
                    je.CustomContentState = null; // Object not needed anymore.

                    if (_navStatus != NavigationStatus.Navigated)
                        return; // Replay() probably started another navigation.
                }
                // Note: navInfo.Restore(), which restores the controls state, is called earlier in
                // the navigation sequence, from HookupNewTree(). This should be done only on
                // Content (_bp) change, whereas CustomContentState is restored after each
                // custom journal entry navigation or fragment navigation.

                if (je.RootViewerState != null && _navigatorHostImpl != null)
                {
                    if (!navigatedToNewContent)
                    {
                        RestoreRootViewerState(je.RootViewerState);
                        je.RootViewerState = null;
                    }
                    else
                    {   // Template may not be applied yet. Need to wait for layout.
                        // (Even if there is currently a Visual under the navigatorHost's ContentPresenter,
                        // it may be associated with the previous Content object.)
                        handleContentLoadedEvent = true;
                    }
                }
            }

            if (handleContentLoadedEvent)
            {
                FrameworkContentElement fce = _bp as FrameworkContentElement;
                if (fce != null)
                {
                    fce.Loaded += OnContentLoaded;
                }
                else
                {
                    FrameworkElement fe = _bp as FrameworkElement;
                    if (fe != null)
                    {
                        fe.Loaded += OnContentLoaded;
                    }
                }
                // ContentRendered handling will be canceled in the Loaded handler.
                _cancelContentRenderedHandling = false;
            }

            if (JournalScope != null)
            {
                NavigateQueueItem currentItem = _navigateQueueItem;
                // The view may need to be changed if NavigationContainers came or went
                JournalScope.Journal.UpdateView();

                // Immediately stop processing this navigation - its been preempted
                // by another navigation from the browser
                if (_navigateQueueItem != currentItem)
                {
                    return;
                }
            }

            ResetPendingNavigationState(NavigationStatus.Navigated);

            FireNavigated(navState);

            // PF.Start is called after Navigated per spec
            if (navigatedToNewContent && IsPageFunction(_bp))
            {
                HandlePageFunction(navInfo);
            }

            HandleLoadCompleted(navState);
        }

        //
        //  INavigator.NavigationProgress
        //
        /// <summary>
        /// event NavigationProgressEventHandler NavigationService.NavigationProgress
        /// </summary>
        /// <value></value>
        public event NavigationProgressEventHandler NavigationProgress
        {
            add { _navigationProgress += value; }
            remove { _navigationProgress -= value; }
        }

        NavigationProgressEventHandler _navigationProgress;

        private void FireNavigationProgress(Uri source)
        {
            // Fire accessibility event for Frame, NavigationWindow, etc.
            UIElement navigatorHost = INavigatorHost as UIElement;
            if (navigatorHost != null)
            {
                AutomationPeer peer = UIElementAutomationPeer.FromElement(navigatorHost) as AutomationPeer;
                if (peer != null)
                {
                    NavigationWindowAutomationPeer.RaiseAsyncContentLoadedEvent(peer, BytesRead, MaxBytes);
                }
            }

            NavigationProgressEventArgs e = new NavigationProgressEventArgs(source, BytesRead, MaxBytes, INavigatorHost);

            // Event handler exception continuality: if exception occurs in NavigationProgress event handler, the cleanup action is
            // the same as StopLoading().
            try
            {
                if (_navigationProgress != null)
                {
                    _navigationProgress(INavigatorHost, e);
                }

                if (this.Application != null && this.Application.CheckAccess())
                {
                    this.Application.FireNavigationProgress(e);
                }
            }
            catch
            {
                DoStopLoading(true, false);

                throw;
            }
        }

        //
        //  INavigator.LoadCompleted
        //
        /// <summary>
        /// event LoadCompletedEventHandler NavigationService.LoadCompleted
        /// </summary>
        /// <value></value>
        public event LoadCompletedEventHandler LoadCompleted
        {
            add { _loadCompleted += value; }
            remove { _loadCompleted -= value; }
        }

        LoadCompletedEventHandler _loadCompleted;

        private void FireLoadCompleted(bool isNavInitiator, object navState)
        {
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info, EventTrace.Event.Wpf_NavigationEnd);

            // do not expose navState if it is NavigateInfo
            object extraData = navState is NavigateInfo ? null : navState;
            NavigationEventArgs e = new NavigationEventArgs(CurrentSource, Content, extraData, _webResponse, INavigatorHost, isNavInitiator);

            // Event handler exception continuality: if exception occurs in LoadCompleted event handler, the cleanup action is
            // the same as StopLoading().
            try
            {
                if (_loadCompleted != null)
                {
                    // If the Navigator is Frame or NavigationWindow, the
                    // relative event handlers would be called here.
                    // Since Frame and NavigationWIndow just transferred their
                    // event handlers to their own NavigationService.
                    _loadCompleted(INavigatorHost, e);
                }

                if (this.Application != null && this.Application.CheckAccess())
                {
                    this.Application.FireLoadCompleted(e);
                }
            }
            catch
            {
                DoStopLoading(true, false);

                throw;
            }
        }

        #region FragmentNavigation Event

        /// <summary>
        /// This event is fired when the navigating uri contains a fragment.
        /// It allows the listeners to take a custom action when a fragment is
        /// encountered.
        /// </summary>
        public event FragmentNavigationEventHandler FragmentNavigation
        {
            add { _fragmentNavigation += value; }
            remove { _fragmentNavigation -= value; }
        }

        private FragmentNavigationEventHandler _fragmentNavigation;

        // Returns true if a listener has handled the fragment and no more processing is necessary
        // False indicates that NavigationService should continue with the default behaviour
        private bool FireFragmentNavigation(string fragment)
        {
            if (string.IsNullOrEmpty(fragment))
            {
                // A navigation to a null or empty fragment is a scroll to the top of the page.
                // This is not intuitively a fragment navigation so we should not fire this event.
                return false;
            }

            FragmentNavigationEventArgs e = new FragmentNavigationEventArgs(fragment, INavigatorHost);

            // Event handler exception continuality: if exception occurs in FragmentNavigation event handler, the cleanup action is
            // the same as StopLoading().
            try
            {
                if (_fragmentNavigation != null)
                {
                    _fragmentNavigation(this, e);
                }

                if (Application != null && Application.CheckAccess())
                {
                    Application.FireFragmentNavigation(e);
                }
            }
            catch
            {
                DoStopLoading(true, false);

                throw;
            }

            return e.Handled;
        }

        #endregion

        // <summary>
        // Fire load completed on current NavigationService first.
        // Remove the search entity from its ParentNavigationService's pendinglist,
        // if the parent NavigationService's pendinglist reaches to Zero, Fire
        // the loadcompleted event on the ParentNavigationService.
        // </summary>
        private void HandleLoadCompleted(object navState)
        {
            // if this is this frame finishing we need to remember navState until all children fire
            if (navState != null)
            {
                _navState = navState;
            }

            // If it was async parsing and  _asyncObjectConverter is not null here, it means
            // parser is not done with parsing the stream (async parsing). This is currently the only case that this could happen.
            // When parser is done, OnStreamClosed will be called where _asyncObjectConverter will be set to null.
            if (_asyncObjectConverter != null) return;

            // Not the right time to fire it
            // need to save navState if it is non null
            if (!(PendingNavigationList.Count == 0 && _navStatus == NavigationStatus.Navigated))
                return;

            NavigationService ncParent = this.ParentNavigationService;

            /*TODO: Uncomment after new loader design is implemented or sync bind reentrancy is resolved
            Debug.Assert(_navStatus == NavigationStatus.Navigated,
                         "Navigation State Machine is messed up, Expected: " + NavigationStatus.Navigated + "; Current: " + _navStatus);*/

            _navStatus = NavigationStatus.Idle;

            bool isNavInitiator = IsNavigationInitiator;

            FireLoadCompleted(isNavInitiator, _navState);

            // now that we have fired LoadComplete we do not need to remember our navigation state (extra data) or the web response
            _navState = null;

            // Response object should be closed so that the underlying connection can be
            // used for the subsequent requests.  Waiting for GC to close the object could be too late for
            // some scenarios.

            // Do not close and null it before firing LoadCompleted because we pass webresponse out in Navigated and LoadCompleted event args.
            if (_webResponse != null)
            {
                _webResponse.Close();
                _webResponse = null;
            }

            if (!isNavInitiator && ncParent != null)
            {
                ncParent.PendingNavigationList.Remove(this);
                // Inform parent so it can Fire LoadCompleted if appropriate
                ncParent.HandleLoadCompleted(null);
            }
        }

        //
        //  INavigator.NavigationStopped
        //
        /// <summary>
        /// event NavigationStoppedEventHandler NavigationService.NavigationStopped
        /// </summary>
        /// <value></value>
        public event NavigationStoppedEventHandler NavigationStopped
        {
            add { _stopped += value; }
            remove { _stopped -= value; }
        }

        NavigationStoppedEventHandler _stopped;

        private void FireNavigationStopped(object navState)
        {
            // do not expose navState if it is NavigateInfo
            object extraData = navState is NavigateInfo ? null : navState;
            NavigationEventArgs e = new NavigationEventArgs(Source, Content, extraData, null, INavigatorHost, IsNavigationInitiator);

            if (_stopped != null)
            {
                _stopped(INavigatorHost, e);
            }
            if (this.Application != null && this.Application.CheckAccess())
            {
                this.Application.FireNavigationStopped(e);
            }
        }

        // FE/FCE.Loaded is raised right after the first layout, before render.
        private void OnContentLoaded(object sender, RoutedEventArgs args)
        {
            Debug.Assert(sender == _bp);
            FrameworkContentElement fce = _bp as FrameworkContentElement;
            if (fce != null)
            {
                fce.Loaded -= OnContentLoaded;
            }
            else
            {
                ((FrameworkElement)_bp).Loaded -= OnContentLoaded;
            }

            OnFirstContentLayout();

            _cancelContentRenderedHandling = true;
        }

        private void ContentRenderedHandler(object sender, EventArgs args)
        {
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info, EventTrace.Event.Wpf_NavigationContentRendered);

            if (_cancelContentRenderedHandling)
            {
                _cancelContentRenderedHandling = false;
            }
            else
            {
                OnFirstContentLayout();
            }
        }

        private void OnFirstContentLayout()
        {
            // Scrolling will fail unless layout is guaranteed to be done, hence dealing with this here.
            if (CurrentSource != null)
            {
                // First scroll to the fragment if there was one in the URI
                string fragment = BindUriHelper.GetFragment(CurrentSource);
                if (!string.IsNullOrEmpty(fragment))
                {
                    // The main navigation has succeeded so fail silently if element with the ID
                    // was not found or if scrolling fails.
                    this.NavigateToFragment(fragment, false);
                }
            }

            // Restore root viewer state. This is in case HandleNavigated() couldn't do it.
            if (_journalScope != null)
            {
                JournalEntry je = _journalScope.Journal.CurrentEntry;
                if (je != null && je.RootViewerState != null)
                {
                    RestoreRootViewerState(je.RootViewerState);
                    je.RootViewerState = null;
                }
            }
        }

        #endregion INavigator Implementation

        # endregion public method and property

        internal void DoNavigate(Uri source, NavigationMode f, Object navState)
        {
            /*TODO: Uncomment after sync bind reentrancy is resolved
            Debug.Assert(_navStatus == NavigationStatus.Navigating,
                         "Navigation State Machine is messed up, Expected: " + NavigationStatus.Navigating + "; Current: " + _navStatus);*/

            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info, EventTrace.Event.Wpf_NavigationAsyncWorkItem);

            // Because shutdown is completed asynchronously, the DoNavigate callback might be called
            // in the meantime.
            if (IsDisposed)
                return;

            // Get or BeginGet WebResponse
            // Special handling PackWebRequest, because it only support sync right now.
            // D2's plan is to support async in V2. Refer to PS #18958 and #17386.
            // We should switch to async after those tasks are done.
            WebResponse response = null;
            try
            {
                if (_request is PackWebRequest)
                {
                    response = WpfWebRequestHelper.GetResponse(_request);
                    if (response == null)
                    {
                        Uri requestUri = BindUriHelper.GetUriRelativeToPackAppBase(_request.RequestUri);
                        throw new Exception(SR.Get(SRID.GetResponseFailed, requestUri.ToString()));
                    }

                    // Have to use source instead of _request.RequestUri because the work around we put in
                    // to make fragment work with FileWebRequest. See function CreateWebRequest for details.

                    // Get Object from response
                    GetObjectFromResponse(_request, response, source, navState);
                }
                else
                {
                    // Have to use source instead of _request.RequestUri because the work around we put in
                    // to make fragment work with FileWebRequest. See function CreateWebRequest for details.
                    RequestState requestState = new RequestState(_request, source, navState, Dispatcher.CurrentDispatcher);

                    // Async WebResponse for everything other than PackWebRequest

                    _request.BeginGetResponse(new AsyncCallback(HandleWebResponseOnRightDispatcher),
                                                                            requestState);
                }
            }
            // Catch WebException and IOException specifically so other types of exceptions do not lose the context.
            catch (WebException e)
            {
                object extraData = navState is NavigateInfo ? null : navState;
                if (! FireNavigationFailed(new NavigationFailedEventArgs(source, extraData, INavigatorHost, _request, response, e)))
                {
                    throw;
                }
            }
            catch (IOException e)
            {
                object extraData = navState is NavigateInfo ? null : navState;
                if (! FireNavigationFailed(new NavigationFailedEventArgs(source, extraData, INavigatorHost, _request, response, e)))
                {
                    throw;
                }
            }
        }


        private bool FireNavigationFailed(NavigationFailedEventArgs e)
        {
            _navStatus = NavigationStatus.NavigationFailed;

            // Event handler exception continuality: if exception occurs in NavigationFailed event handler, the cleanup action is
            // the same as StopLoading().
            try
            {
                if (NavigationFailed != null)
                {
                    NavigationFailed(INavigatorHost, e);
                }

                if (!e.Handled)
                {
                    NavigationWindow navWin = FindNavigationWindow();
                    if ((navWin != null) && (navWin.NavigationService != this))
                    {
                        navWin.NavigationService.FireNavigationFailed(e);
                    }
                }

                if (!e.Handled && this.Application != null && this.Application.CheckAccess())
                {
                    this.Application.FireNavigationFailed(e);
                }
            }
            finally
            {
                if (_navStatus == NavigationStatus.NavigationFailed)
                {
                    DoStopLoading(true, false);
                }
            }

            return e.Handled;
        }

        //
        // Create a web-request.
        //      May delegate to the browser for cross-domain case.
        //      Will return null if unable to create a web-request.
        //
        private WebRequest CreateWebRequest(Uri resolvedDestinationUri, NavigateInfo navInfo)
        {
            WebRequest request = null;

            // Ideally we would want to use RegisterPrefix and WebRequest.Create.
            // However, these two functions regress 700k working set in System.dll and System.xml.dll
            //  which is mostly for logging and config.
            // Call PackWebRequestFactory.CreateWebRequest to bypass the regression if possible
            //  by calling Create on PackWebRequest if uri is pack scheme
            try
            {
                request = PackWebRequestFactory.CreateWebRequest(resolvedDestinationUri);
            }
            catch (NotSupportedException)
            {
                LaunchResult launched = LaunchResult.NotLaunched;

                // Not supported exceptions are thrown for mailto: which we want to support.
                // So we detect mailto: here.
                launched = AppSecurityManager.SafeLaunchBrowserOnlyIfPossible(CurrentSource, resolvedDestinationUri, IsTopLevelContainer);

                if (launched == LaunchResult.NotLaunched)
                    throw;
            }
            catch (SecurityException)
            {
                throw;
            }

            bool isRefresh = navInfo == null ? false : navInfo.NavigationMode == NavigationMode.Refresh;
            WpfWebRequestHelper.ConfigCachePolicy(request, isRefresh);

            return request;
        }

        // Async WebResponse callback.
        // This can be called on any thread. Find the right dispatcher and call on that
        private void HandleWebResponseOnRightDispatcher(IAsyncResult ar)
        {
            if (IsDisposed)
            {
                return;
            }

            Dispatcher callbackDispatcher = ((RequestState)ar.AsyncState).CallbackDispatcher;

            if (Dispatcher.CurrentDispatcher != callbackDispatcher)
            {
                callbackDispatcher.BeginInvoke(
                    DispatcherPriority.Normal,
                    (DispatcherOperationCallback)delegate(object unused)
                {
                    HandleWebResponse(ar);
                    return null;
                },
                null);
            }
            else
            {
                //
                // Since this is for Async WebResponse call, this method call
                // is out of the DispatcherOperation handling, and then out of
                // the Dispatcher.WrappedInvoke scope.
                // If an exception is raised inside HanldeWebRespone, the Dispatcher
                // UnhandledException handler should have chance to catch it.
                //
                callbackDispatcher.Invoke(
                     DispatcherPriority.Send,
                     (DispatcherOperationCallback)delegate(object unused)
                     {
                         HandleWebResponse(ar);
                         return null;
                     },
                     null);
            }
        }

        private void HandleWebResponse(IAsyncResult ar)
        {
            if (IsDisposed)
            {
                return;
            }

            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info, EventTrace.Event.Wpf_NavigationWebResponseReceived);

            // we neeed source, navState (get from ar.AsyncState)
            RequestState requestState = (RequestState)ar.AsyncState;

            // We don't keep a list of previous WebRequests that has been made, because
            // at any time we only handle one WebRequest. If a new WebRequest comes in
            // before the previous one finishes, the previous one is aborted.
            // However if a WebRequest.Abort() is called before its aync callback is called,
            // the async callback will still be called. So we need to check the request. Don't
            // do anything if it's not the current request.
            if (requestState.Request != _request)
            {
                return;
            }

            WebResponse response = null;
            try
            {
                try
                {
                    response = WpfWebRequestHelper.EndGetResponse(_request, ar);
                }
                catch
                {
                    throw;
                }

                // response object will be closed at approrpiate time when it is not used anymore later.
                GetObjectFromResponse(_request, response, requestState.Source, requestState.NavState);
            }
            // Catch WebException and IOException specifically so other types of exceptions do not lose the context.
            catch (WebException e)
            {
                object extraData = requestState.NavState is NavigateInfo ? null : requestState.NavState;
                if (! FireNavigationFailed(new NavigationFailedEventArgs(requestState.Source, extraData, INavigatorHost, _request, response, e)))
                {
                    throw;
                }
            }
            catch (IOException e)
            {
                object extraData = requestState.NavState is NavigateInfo ? null : requestState.NavState;
                if (! FireNavigationFailed(new NavigationFailedEventArgs(requestState.Source, extraData, INavigatorHost, _request, response, e)))
                {
                    throw;
                }
            }
        }

        // Create Object from the return of WebResponse stream
        private void GetObjectFromResponse(WebRequest request, WebResponse response, Uri destinationUri, Object navState)
        {
            bool fHoldResponse = false;

            ContentType contentType = WpfWebRequestHelper.GetContentType(response);

            try
            {
                Stream s = response.GetResponseStream();

                if (s == null)
                {
                    Uri requestUri = BindUriHelper.GetUriRelativeToPackAppBase(_request.RequestUri);

                    throw new Exception(SR.Get(SRID.GetStreamFailed, requestUri.ToString()));
                }

                long contentLength = response.ContentLength;

                Uri cleanSource = BindUriHelper.GetUriRelativeToPackAppBase(destinationUri);
                NavigateInfo navigateInfo = navState as NavigateInfo;

                bool sandBoxContent = SandboxExternalContent && (! BaseUriHelper.IsPackApplicationUri(destinationUri)) && MimeTypeMapper.XamlMime.AreTypeAndSubTypeEqual(contentType);

                // BindStream overrides Read() and calls icc.OnNavigationProgress every 1k byte read
                BindStream bindStream = new BindStream(s, contentLength, cleanSource, (IContentContainer)this, Dispatcher.CurrentDispatcher);

                Invariant.Assert((_webResponse == null) && (_asyncObjectConverter == null));
                _webResponse = response;
                _asyncObjectConverter = null;


                // canUseTopLevelBrowserForHTMLRendering will be true for TopLevel navigation away from browser hosted app. If that is the case
                // o will be null.
                // We don't support browser hosting since .NET Core 3.0, so therefore canUseTopLevelBrowserForHTMLRendering = false
                bool canUseTopLevelBrowserForHTMLRendering = false;
                Object o = MimeObjectFactory.GetObjectAndCloseStream(bindStream, contentType, destinationUri, canUseTopLevelBrowserForHTMLRendering, sandBoxContent, true /*allowAsync*/, IsJournalNavigation(navigateInfo), out _asyncObjectConverter);

                if (o != null)
                {
                    // We don't keep a list of previous WebRequests that has been made, because
                    // at any time we only handle one WebRequest. If a new WebRequest comes in
                    // before the previous one finishes, the previous one is aborted.
                    // However, today we cannot abort LoadXaml and LoadBaml, if user starts a new navigation in Initilaized
                    // event handler, the currrent navigation has been cancelled, we should not call OnContentReady
                    // when the request we start with is the same as the current one.
                    if (_request == request)
                    {
                        ((IContentContainer)this).OnContentReady(contentType, o, destinationUri, navState);
                        fHoldResponse = true;
                    }
                }
                else
                {
                    try
                    {
                        // If o == null, it means we don't know how to convert it.
                        // Currently that's everything other than xaml, baml and html at site
                        // of origin. If this is not a TopLevelContainer, we will throw an exception
                        // if there is no converter for it, else we will try to launch the
                        // browser if safe to do so.
                        // For loose XAML viewing, we can get in this situation if the web server doesn't
                        // return the right MIME type. UrlMon in IE 7+ has some heuristics based on file extension
                        // to detect XAML, so PresentationHost may get invoked, but our 
                        // WpfWebRequestHelper.GetContentType() fails to do the same inference. In particular, 
                        // it appears that UrlMon looks at the Content-Disposition HTTP header, but we don't.
                        if (!IsTopLevelContainer || BrowserInteropHelper.IsInitialViewerNavigation)
                        {
                            throw new InvalidOperationException(SR.Get(SRID.FailedToConvertResource));
                        }

                        DelegateToBrowser(response is PackWebResponse, destinationUri);

                        // Beware reentrancy in the context of the outgoing DelegateNavigation call:
                        // The browser will send us the BrowseStop command before returning from Navigate().
                        // This will lead to DoStopLoading(), which will abort the WebReqest.
                    }
                    finally
                    {
                        DrainResponseStreamForPartialCacheFileBug(s);

                        s.Close();

                        // Should clean the state.
                        ResetPendingNavigationState(_navStatus);
                    }
                }
            }
            finally
            {
                // If the code doesn't want to hold the webresponse,  close it now.
                // otherwise, close the response object when the Navigation is done,
                // or when the navigation is stopped.
                if (!fHoldResponse)
                {
                    response.Close();
                    _webResponse = null;
                    if (_asyncObjectConverter != null)
                    {
                        _asyncObjectConverter.CancelAsync();
                        _asyncObjectConverter = null;
                    }
                }
            }
        }

        private void DelegateToBrowser(bool isPack, Uri destinationUri)
        {
            try
            {
                if (isPack)
                {
                    destinationUri = BaseUriHelper.ConvertPackUriToAbsoluteExternallyVisibleUri(destinationUri);
                }

                if (EventTrace.IsEnabled(EventTrace.Keyword.KeywordHosting, EventTrace.Level.Info))
                {
                    EventTrace.EventProvider.TraceEvent(
                        EventTrace.Event.Wpf_NavigationLaunchBrowser, EventTrace.Keyword.KeywordHosting, EventTrace.Level.Info,
                        destinationUri.ToString());
                }

                AppSecurityManager.SafeLaunchBrowserDemandWhenUnsafe(CurrentSource, destinationUri, IsTopLevelContainer);
            }
            finally
            {
                // Browser downloading state not reset; cases 2 and 3.
                InformBrowserAboutStoppedNavigation();
            }
        }

        private void DrainResponseStreamForPartialCacheFileBug(Stream s)
        {
            // Drain the stream and launch the browser

            // We need to drain the response stream to work around issues with
            // partial cache files in the wininet cache.
            // We request CLR to use the wininet cache for http webrequests
            // When we abort a download in managed code, CLR still commits the
            // partial file to wininet cache (Temporary Internet Files folder)
            // When this file is renavigated to from IE, IE does NOT try to
            // redownload the file if the cache entry has not expired nor will
            // it try to complete the previous download.
            // Opened tracking bug 895912 in Windows Data base. VSWhidbey bug
            // is linked to it


            // Check CachePolicy here because we plan to expose WebRequest & WebResponse
            // in Navigating/Navigated event and allow user to configure it. So we want
            // to check cache policy here.
            if ((_request is HttpWebRequest) &&
                (HttpWebRequest.DefaultCachePolicy != null) &&
                (HttpWebRequest.DefaultCachePolicy is HttpRequestCachePolicy))
            {
                // Use reader for its ReadToEnd ability because response.ContentLength
                // could not be set for HttpWebRequest. It depends on Transfer-Encoding.
                // If Transfer_Encoding is chunked, ContentLength will not be available.
                StreamReader reader = new StreamReader(s);
                reader.ReadToEnd();
                reader.Close();
            }
        }

        internal void DoNavigate(Object bp, NavigationMode navFlags, Object navState)
        {
            /*TODO: Uncomment after sync bind reentrancy is resolved
            Debug.Assert(_navStatus == NavigationStatus.Navigating,
                         "Navigation State Machine is messed up, Expected: " + NavigationStatus.Navigating + "; Current: " + _navStatus);*/

            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info, EventTrace.Event.Wpf_NavigationAsyncWorkItem);

            // Because shutdown is completed asynchronously, the DoNavigate callback might be called
            // in the meantime.
            if (IsDisposed)
                return;

            NavigateInfo navigateInfo = navState as NavigateInfo;
            Debug.Assert(IsConsistent(navigateInfo));
            Invariant.Assert(navFlags != NavigationMode.Refresh ^ object.ReferenceEquals(bp, _bp),
                "Navigating to the same object should be handled as fragment navigation, except for Refresh.");

            Uri source = navigateInfo == null ? null : navigateInfo.Source;
            // The baseUri passed to GetResolvedUri() is null because here we have a new Content
            // object. Its URI is not resolved relative to the URI of the previous Content.
            Uri resolvedSource = BindUriHelper.GetResolvedUri(null, source);

            ((IContentContainer)this).OnContentReady(null, bp, resolvedSource, navState);
        }

        /// <summary> Updates the Journal for a navigation that has completed successfully. </summary>
        /// <exception cref="System.NotImplementedException">
        /// Can't journal by serializing with a URI.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// Can't journal by URI without a URI.
        /// </exception>
        /// <remarks> _bp is still the previous content (before the new navigation). It can be null.
        /// destinationJournalEntry can be null.
        /// No journal entry is created for certain types of Content or when there is no
        /// NavigationWindow [which is where the Journal is].
        /// </remarks>
        private JournalEntry UpdateJournal(
            NavigationMode navigationMode, JournalReason journalReason, JournalEntry destinationJournalEntry)
        {
            Debug.Assert(navigationMode == NavigationMode.New ||
                navigationMode == NavigationMode.Back ||
                navigationMode == NavigationMode.Forward, "The journal should not be updated on Refresh.");
            // The point of this assert is that there should be no destinationJournalEntry for
            // navigationMode=New, but it is always required for Back/Fwd.
            Debug.Assert(destinationJournalEntry == null
                    ^ (navigationMode == NavigationMode.Back || navigationMode == NavigationMode.Forward));

            JournalEntry journalEntry = null;

            if (!_doNotJournalCurrentContent)
            {
                journalEntry = MakeJournalEntry(journalReason);
            }

            if (journalEntry == null)
            {
                _doNotJournalCurrentContent = false;

                // This case will be true when we have navigated to null and then gone back.  We cannot add null to the journal
                // but we still need to commit the back navigation to the journal so the journal state stays sane.
                if ((navigationMode == NavigationMode.Back || navigationMode == NavigationMode.Forward)
                    && JournalScope != null)
                {
                    JournalScope.Journal.CommitJournalNavigation(destinationJournalEntry);
                }
                // There's no need to do anything here for a New navigation.
                return null;
            }

            // EnsureJournal() should be called no earlier than here. Only the second navigation in a
            // NavigationService really requires a journal.
            // In particular, a child Frame should not be forced to create its own journal when it
            // is being re-navigated by DataStreams.Load(), because it doesn't yet have access to the
            // parent JournalNavigationScope.
            JournalNavigationScope journalScope = EnsureJournal();
            if (journalScope == null)
            {
                return null;
            }

            PageFunctionBase pfBase = _bp as PageFunctionBase;
            if (pfBase != null)
            {
                // PageFunctions that don't show UI don't get navigated to in the journal
                // We still need to add it to the journal since we need to resume this when its child finishes
                // This codepath is not executed if this pagefunction finished without launching a child.
                // That case is handled in HandleFinish

                if (navigationMode == NavigationMode.New && pfBase.Content == null)
                {
                    journalEntry.EntryType = JournalEntryType.UiLess;
                }
            }

            journalScope.Journal.UpdateCurrentEntry(journalEntry);


            if (navigationMode == NavigationMode.New)
            {
                journalScope.Journal.RecordNewNavigation();
            }
            else // Back or Forward
            {
                journalScope.Journal.CommitJournalNavigation(destinationJournalEntry);
            }

            _customContentStateToSave = null; // not needed anymore

            return journalEntry;
        }

        /// <summary>
        /// Makes the appropriate kind of journal entry for the current Content and its state.
        /// For certain types of content, no journal entry is created (null is returned).
        /// </summary>
        internal JournalEntry MakeJournalEntry(JournalReason journalReason)
        {
            if (_bp == null)
            {
                return null;
            }

            Debug.Assert(_contentId != 0 &&
                (_journalEntryGroupState == null || _journalEntryGroupState.ContentId == _contentId));
            if (_journalEntryGroupState == null) // First journal entry created for the current Content?
            {
                _journalEntryGroupState = new JournalEntryGroupState(_guidId, _contentId);
            }

            JournalEntry journalEntry;
            bool keepAlive = IsContentKeepAlive();
            PageFunctionBase pfBase = _bp as PageFunctionBase;
            if (pfBase != null)
            {
                if (keepAlive)
                {
                    journalEntry = new JournalEntryPageFunctionKeepAlive(_journalEntryGroupState, pfBase);
                }
                else
                {
                    //
                    // If the PageFunction is navigated from xaml Uri, or navigated from an instance of
                    // PageFunction type, but that PageFunctin type is implemented from xaml file,
                    // we should always get the BaseUri DP value for the root PageFunction element.
                    //
                    // If the code navigates to pure #fragment, the root element should be ready,
                    // if the BaseUri for that root element is set, we should still use JournalEntryPageFunctionUri.
                    // if the BaseUri for that root element is not set, that pagefunction class is not
                    // implemented in xaml file, JournalEntryPageFunctionType is used for journaling.
                    // Navigation service has its own way to get to the element marked by the pure fragment.
                    //
                    Uri baseUri = pfBase.GetValue(BaseUriHelper.BaseUriProperty) as Uri;

                    if (baseUri != null)
                    {
                        Invariant.Assert(baseUri.IsAbsoluteUri == true, "BaseUri for root element should be absolute.");

                        Uri markupUri;

                        //
                        // Set correct uri when creating instance of JournalEntryPageFunctionUri
                        //
                        //   This markupUri is used to create instance of PageFunction from baml stream.
                        //   fragment in original Source doesn't affect the resource loading, and it will
                        //   be set in the JournalEntry.Source for further navigation handling. So the logic
                        //   of setting markupUri for JEPFUri can be simplified as below:
                        //
                        //   If _currentCleanSource is set and it is not a pure fragment uri, take whatever
                        //   value of _currentSource, which should always be an absolute Uri for the page.
                        //
                        //   For all other cases, take whatever value of BaseUri in root element.
                        //
                        if (_currentCleanSource != null && BindUriHelper.StartWithFragment(_currentCleanSource) == false )
                        {
                            markupUri = _currentSource;
                        }
                        else
                        {
                            markupUri = baseUri;
                        }

                        journalEntry = new JournalEntryPageFunctionUri(_journalEntryGroupState, pfBase, markupUri);
                    }
                    else
                    {
                        journalEntry = new JournalEntryPageFunctionType(_journalEntryGroupState, pfBase);
                    }
                }

                journalEntry.Source = _currentCleanSource; // This could be #fragment.
            }
            else
            {
                if (keepAlive)
                {
                    journalEntry = new JournalEntryKeepAlive(_journalEntryGroupState, _currentCleanSource, _bp);
                }
                else
                {
                    journalEntry = new JournalEntryUri(_journalEntryGroupState, _currentCleanSource);
                }
            }

            // _customContentStateToSave can be preset by AddBackEntry() or FireNavigating().
            // If not, try the IProvideCustomContentState callback.
            CustomContentState ccs = _customContentStateToSave;
            if (ccs == null)
            {
                IProvideCustomContentState pccs = _bp as IProvideCustomContentState;
                if (pccs != null)
                {
                    ccs = pccs.GetContentState();
                }
            }
            if (ccs != null)
            {
                // Make sure the object is serializable
                Type type = ccs.GetType();
                if (!type.IsSerializable)
                {
                    throw new SystemException(SR.Get(SRID.CustomContentStateMustBeSerializable, type));
                }
                journalEntry.CustomContentState = ccs;
            }
            // Info: CustomContentState for the current page in child frames is saved in
            // DataStreams.SaveState(). (This requires the IProvideCustomContentState to be implemented.)

            // Root Viewer journaling
            if (_rootViewerStateToSave != null) // state saved in advance?
            {
                journalEntry.RootViewerState = _rootViewerStateToSave;
                _rootViewerStateToSave = null;
            }
            else
            {
                journalEntry.RootViewerState = GetRootViewerState(journalReason);
            }

            // Set the friendly Name of this JournalEntry, it will be used to display
            // in the drop-down list on the Back/Forward buttons
            // Journal entries aren't recycled when going back\forward. A new JournalEntry is always created, so
            // we need to set the name each time
            //  Need to have a way to set JournalEntry.Name per Frame instead of using window's title

            string name = null;
            if (journalEntry.CustomContentState != null)
            {
                name = journalEntry.CustomContentState.JournalEntryName;
            }
            if (string.IsNullOrEmpty(name))
            {
                DependencyObject dependencyObject = _bp as DependencyObject;
                if (dependencyObject != null)
                {
                    name = (string)dependencyObject.GetValue(JournalEntry.NameProperty);

                    if (String.IsNullOrEmpty(name) && dependencyObject is Page)
                    {
                        name = (dependencyObject as Page).Title;
                    }
                }
                if (!String.IsNullOrEmpty(name))
                {
                    if (_currentSource != null)
                    {
                        string fragment = BindUriHelper.GetFragment(_currentSource);
                        if (!string.IsNullOrEmpty(fragment))
                        {
                            name = name + "#" + fragment;
                        }
                    }
                }
                else
                {
                    // Page.WindowTitle is just a shortcut to Window.Title.
                    // The window title is used as a journal entry name only for a top-level container.
                    NavigationWindow navWin =
                        JournalScope == null ? null : JournalScope.NavigatorHost as NavigationWindow;
                    if (navWin != null && this == navWin.NavigationService
                        && !String.IsNullOrEmpty(navWin.Title))
                    {
                        if (CurrentSource != null)
                        {
                            name = String.Format(CultureInfo.CurrentCulture, "{0} ({1})", navWin.Title, JournalEntry.GetDisplayName(_currentSource, SiteOfOriginContainer.SiteOfOrigin));
                        }
                        else
                        {
                            name = navWin.Title;
                        }
                    }
                    else
                    {
                        // if not title was set we use the uri if it is available.
                        if (CurrentSource != null)
                        {
                            name = JournalEntry.GetDisplayName(_currentSource, SiteOfOriginContainer.SiteOfOrigin);
                        }
                        else
                        {
                            name = SR.Get(SRID.Untitled);
                        }
                    }
                }
            }
            journalEntry.Name = name;

            if (journalReason == JournalReason.NewContentNavigation)
            {
                journalEntry.SaveState(_bp);
            }

            return journalEntry;
        }

        /// <summary>
        /// Called by ApplicationProxyInternal when a XAML Browser Application is about to be shut down
        /// and the entire journal needs to be serialized.
        /// A semi-bogus Navigating event is raised to give the application a chance to provide a
        /// CustomContentState, in case it doesn't implement IProvideCustomContentState [Mongoose].
        /// (In case it does, the event is still raised for consistency.)
        /// </summary>
        internal void RequestCustomContentStateOnAppShutdown()
        {
            _isNavInitiator = false; _isNavInitiatorValid = true; // prevent updating the brower's status
            FireNavigating(null, null, null, null); // sets _customContentStateToSave
        }


        /// <summary>
        /// Returns the current Application
        /// </summary>
        internal Application Application
        {
            get { return Application.Current; }
        }

        internal bool AllowWindowNavigation
        {
            private get { return _allowWindowNavigation; }
            set { _allowWindowNavigation = value; }
        }

        internal long BytesRead
        {
            get { return _bytesRead; }
            set { _bytesRead = value; }
        }

        internal long MaxBytes
        {
            get { return _maxBytes; }
            set { _maxBytes = value; }
        }

        /// <summary><see cref="JournalEntry.ContentId"/></summary>
        internal uint ContentId
        {
#if DEBUG
            [DebuggerStepThrough]
#endif
            get { return _contentId; }
        }

        internal Guid GuidId
        {
            get { return _guidId; }
            set { _guidId = value; }
        }

        /// <remarks>
        /// NOTE that the tree of NavigationServices may comprise multiple JournalNavigationScopes.
        /// So, it is possible that this NS has a parent NS but is also the root NS for a JNS
        /// (IsJournalLevelContainer==true). (Practically, this happens when a Frame has its own
        /// journal and is hosted in NavigationWindow or another Frame.)
        /// </remarks>
        internal NavigationService ParentNavigationService
        {
            get { return _parentNavigationService; }
        }

        internal bool CanReloadFromUri
        {
            get
            {
                // Special case: Doing fragment navigation within an element tree that doesn't
                // have a source URI. Then _currentCleanSource will be either null or something
                // like pack://application,,,/#fragment. (This pseudo-absolute URI is currently
                // malfored; that's why the complicated check below. The same situation occurs
                // in Navigate(uri, navState).)
                return !(_currentCleanSource == null
                        || BindUriHelper.StartWithFragment(_currentCleanSource)
                        || BindUriHelper.StartWithFragment(BindUriHelper.GetUriRelativeToPackAppBase(_currentCleanSource)));
            }
        }

        internal ArrayList ChildNavigationServices
        {
            get { return _childNavigationServices; }
        }

        private FinishEventHandler FinishHandler
        {
            get
            {
                if (_finishHandler == null)
                {
                    _finishHandler = new FinishEventHandler(HandleFinish);
                }

                return _finishHandler;
            }
        }

        private bool IsTopLevelContainer
        {
            get
            {
                // NavigationService should only look in the App if App exists and if
                // this NavigationService is on the same thread as the App. If NavService
                // is not on the same thread as App it means that this NavService is part of
                // a NavigationWindow/Frame that exists on a non-App thread and thus looking
                // into App to determine top level container does not make sense.
                return (INavigatorHost is NavigationWindow ||
                        (this.Application != null &&
                        this.Application.CheckAccess() == true &&
                        this.Application.NavService == this)
                        );
            }
        }

        private bool IsJournalLevelContainer
        {
            get
            {
                JournalNavigationScope jns = JournalScope;
                return jns != null && jns.RootNavigationService == this;
            }
        }

        private bool SandboxExternalContent
        {
            get
            {
                DependencyObject navigator = INavigatorHost as DependencyObject;

                if (navigator == null)
                    return false;

                return (bool)navigator.GetValue(Frame.SandboxExternalContentProperty);
            }
}

        internal INavigator INavigatorHost
        {
#if DEBUG
            [DebuggerStepThrough]
#endif
            get { return _navigatorHost; }
            set
            {
                RequestNavigateEventHandler navHandler = new RequestNavigateEventHandler(OnRequestNavigate);

                if (_navigatorHost != null)
                {
                    IInputElement iie = _navigatorHost as IInputElement;
                    if (iie != null)
                    {
                        iie.RemoveHandler(Hyperlink.RequestNavigateEvent, navHandler);
                    }

                    IDownloader oldDownloader = _navigatorHost as IDownloader;
                    if (oldDownloader != null)
                    {
                        oldDownloader.ContentRendered -= new EventHandler(ContentRenderedHandler);
                    }
                }

                if (value != null)
                {
                    IInputElement iie = value as IInputElement;
                    if (iie != null)
                    {
                        iie.AddHandler(Hyperlink.RequestNavigateEvent, navHandler);
                    }

                    // We want to listen to ContentRendered of the INavigatorHost so
                    // that we can scroll into view the correct element if needed
                    IDownloader newDownloader = value as IDownloader;
                    if (newDownloader != null)
                    {
                        newDownloader.ContentRendered += new EventHandler(ContentRenderedHandler);
                    }
                }

                _navigatorHost = value;
                _navigatorHostImpl = value as INavigatorImpl;
            }
        }

        internal NavigationStatus NavStatus
        {
            get { return _navStatus; }
            set { _navStatus = value; }
        }

        internal ArrayList PendingNavigationList
        {
            get { return _pendingNavigationList; }
        }

        // A new WebBrowser is created per new navigation.
        // At any time, an NavigationService can only have one WebBrowser;
        // a WebBrowser can belong to only one NavigationService.
        internal WebBrowser WebBrowser
        {
            get
            {
                return _webBrowser;
            }
        }

        internal bool IsDisposed
        {
            get
            {
                // NavigationService should only look in the App if App exists and if
                // this NavigationService is on the same thread as the App. If NavService
                // is not on the same thread as App it means that this NavService is part of
                // a NavigationWindow/Frame that exists on a non-App thread and thus looking
                // into App to determine if app is shuttind down does not make sense.
                bool isAppShuttingDown = false;
                if ((this.Application != null) &&
                    (this.Application.CheckAccess() == true) &&
                    (Application.IsShuttingDown == true))
                {
                    isAppShuttingDown = true;
                }

                return _disposed || isAppShuttingDown;
            }
        }

        // We shouldn't need Dispose since we don't own unmanaged resources.
        // Whereever we call Dispose, think it should change to calling StopBinds directly??
        // Maybe the name is just a minomer?
        // Per Murray null Uri is an expensive operation????!! Check on that....
        internal void Dispose()
        {
            _disposed = true;

            StopLoading();

            foreach (NavigationService ns in ChildNavigationServices)
            {
                ns.Dispose();
            }

            _journalScope = null;
            _bp = null;
            _currentSource = null;
            _currentCleanSource = null;
            _oldRootVisual = null;
            _childNavigationServices.Clear();
            _parentNavigationService = null;
            _webBrowser = null;
        }

        #region Private Functions

        /// <summary>
        /// NOTE: This method should be used only when the NavigationWindow is really needed.
        /// Normal operation should use the JournalNavigationScope (JournalScope property).
        /// </summary>
        private NavigationWindow FindNavigationWindow()
        {
            NavigationService ns = this;
            while (ns != null && ns.INavigatorHost != null)
            {
                NavigationWindow nw = ns.INavigatorHost as NavigationWindow;
                if (nw != null)
                    return nw;
                ns = ns.ParentNavigationService;
            }
            return null;
        }

        static internal bool IsPageFunction(object content)
        {
            return (content as PageFunctionBase == null ? false : true);
        }
        //
        // The pagefunction model works by allowing listeners to attach to events before a navigation occurs.
        // After navigation occurs, the "caller" may be serialized - so he can't remain attached as
        // a listener.
        //
        // SetupPageFunctionHandlers job is to remove any listeners on the PageFunction
        // so these can be stored at persistence time.
        //
        //    bp - the result of the Navigation, i.e. the PageFunction we're about to navigate to.
        //
        private void SetupPageFunctionHandlers(Object bp)
        {
            PageFunctionBase pf = bp as PageFunctionBase;
            // Frame can call this when the tree is being torn down to detach Finish handler on the PF it holds
            // This won't go thru the regular navigation path, so we need to detach everything here.
            if (bp == null)
                return;

            pf.FinishHandler = FinishHandler;

            // we're undoing the delegate here so that
            // there are no references among page functions
            // Since every page function has exactly one parent,
            // we store the info for the parent's delegate on the
            // pagefunction itself

            ReturnEventSaver saver = new ReturnEventSaver();
            saver._Detach(pf);
        }

        private void HandlePageFunction(NavigateInfo navInfo)
        {
            PageFunctionBase ps = (PageFunctionBase)_bp;

            if (IsJournalNavigation(navInfo))
            {
                Debug.Assert(ps._Resume); // should've been set by JournalEntryPFxx.ResumePageFunction()
                ps._Resume = true;
            }

            // Need to Check for refresh on history navigations as well and call LoadHistory instead?
            if (ps._Resume == false)
            {
                ps.CallStart();
            }
            else
            {
                // Need to call: ps.CallResume();
            }
        }

        private void HandleFinish(PageFunctionBase endingPF, object ReturnEventArgs)
        {
            if (EventTrace.IsEnabled(EventTrace.Keyword.KeywordHosting, EventTrace.Level.Info))
            {
                EventTrace.EventProvider.TraceEvent(
                    EventTrace.Event.Wpf_NavigationPageFunctionReturn, EventTrace.Keyword.KeywordHosting, EventTrace.Level.Info,
                    endingPF.ToString());
            }

            // 
            // handle this situation gracefully -
            // this happens if someone calls Navigate() and then Finishes
            // before we have a chance to navigate.
            // Investigate what this is....
            Debug.Assert(_navigateQueueItem == null,
                    "There's a navigation pending - see kusumav for details");

            // NOTE: It is not always that endingPF==_bp. A PF may end itself when its child ends. Then
            // HandleFinish() will be called for the grandparent PF while _bp is still the child PF.

            if (JournalScope == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.WindowAlreadyClosed));
            }

            Journal journal = JournalScope.Journal;
            PageFunctionBase parentPF = null;

            int parentIndex = JournalEntryPageFunction.GetParentPageJournalIndex(this, journal, endingPF);

            if (endingPF.RemoveFromJournal)
            {
                DoRemoveFromJournal(endingPF, parentIndex);
            }

            // If the parent page is a PF, resume it and let it know the child PF returned.
            // If it's not a PF, the Return event will be raised later on - see NavigateToParentPage().
            if (parentIndex != _noParentPage)
            {
                JournalEntryPageFunction parentPfEntry = journal[parentIndex] as JournalEntryPageFunction;
                if (parentPfEntry != null)
                {
                    parentPF = parentPfEntry.ResumePageFunction();

                    // Need to set the FinishHandler here because the PF's Return event handler
                    // may decide to call OnReturn().
                    parentPF.FinishHandler = this.FinishHandler;

                    FireChildPageFunctionReturnEvent(parentPF, endingPF, ReturnEventArgs);
                }
            }

            // if the parent requested a new child, don't navigate to the parent
            // Need to determine what happens if the parent requests a navigation AND
            // says it'd done. this should probably be considered a PageFunction bug,
            // but we need to figure out what we want to happen in that case.
            if (_navigateQueueItem == null)
            {
                // Navigate to the Parent page.
                // Two cases:
                //     Parent is a PageFunction:  bParentIsPF is true, parentPF is not null.
                //     Parent is a Non PageFunction: bParentIsPF is false, parentPF is null,
                //                                   the valid info are parentIndex and ReturnEventArgs.

                // There may have been recursive calls into HandleFinish(). As we are unwinding here,
                // parentIndex may point to a journal entry that was already removed. If the parent PF
                // started a navigation (new or to its parent), we'd be in the 'else' case. But if that
                // navigation was canceled, _navigateQueueItem==null. One special case in which this
                // happens is when the entire "wizard" window is closed. Then NS is disposed.
                if (parentIndex != _noParentPage && parentIndex < journal.TotalCount && !IsDisposed)
                {
                    NavigateToParentPage(endingPF, parentPF, ReturnEventArgs, parentIndex);
                }

                // Need to prune here (resumed parent, it showed itself and then finished)
                // For now bcos of the correct instance of parent resume bug, this should not happen
            }
            else
            {
                // The parent requested a navigation(usually to another child PF but could be a regular Xaml)
                // Update the parent's state in the journal
                // Special case: the parent PF has the RemoveFromJournal flag, and it returned to its parent.
                // Then parentIndex is not valid anymore.
                if (parentIndex < journal.TotalCount)
                {
                    JournalEntryPageFunction entry = (JournalEntryPageFunction)journal[parentIndex];
                    entry.SaveState(parentPF);
                }
                // Return event handler should not be left attached.
                Debug.Assert(parentPF._Return == null);
                parentPF.FinishHandler = null;
            }
        }

        //
        // This method will reattach the return handler to the parent page.
        // and then fire the return event on the child pagefunction.
        //
        private void FireChildPageFunctionReturnEvent(object parentElem, PageFunctionBase childPF, object ReturnEventArgs)
        {
            ReturnEventSaver saver = childPF._Saver;         // get the endingPF's saved info

            if (saver != null)
            {
                saver._Attach(parentElem, childPF);         // reattach the parent to the child

                // When the Return event handler is invoked on the parent element, the parent is not in the tree.
                // But developers need to access the NavigationService from the Return event handler (to be able to
                // start new navigation). To make this scenario straightforward, set the NavigationService property
                // before raising the Return event and clear it afterwards. See details of the scenario in .
                // Similar issue with Window.GetWindow()...
                Window window = null;
                DependencyObject dobj = parentElem as DependencyObject;
                if ((dobj != null) && (!dobj.IsSealed))
                {
                    dobj.SetValue(NavigationServiceProperty, this);

                    var host = this.INavigatorHost as DependencyObject;
                    if (host != null && (window = Window.GetWindow(host)) != null)
                    {
                        dobj.SetValue(Window.IWindowServiceProperty, window);
                    }
                }

                // Event handler exception continuality: if exception occurs in Return event handler, we are going to stop loading
                // and stop at the child pagefunction and not returning to parent.
                try
                {
                    childPF._OnFinish(ReturnEventArgs);         // then call the endingPF to fire it's event
                }
                catch
                {
                    DoStopLoading(true, false);
                    throw;
                }
                finally
                {
                    saver._Detach(childPF);                     // now detach the event handler since we're done
                    if ((dobj != null) && (!dobj.IsSealed))
                    {
                        dobj.ClearValue(NavigationServiceProperty);
                        if (window != null)
                        {
                            dobj.ClearValue(Window.IWindowServiceProperty);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Deletes everything in this NavigationService from the *first* instance of the
        /// finishing PageFunction on.
        /// </summary>
        private void DoRemoveFromJournal(PageFunctionBase finishingChildPageFunction, int parentEntryIndex/* = -1 */)
        {
            if (!finishingChildPageFunction.RemoveFromJournal)
                return;

            bool deleting = false;
            Journal journal = JournalScope.Journal;
            int journalEntryIndex = parentEntryIndex + 1;
            while (journalEntryIndex < journal.TotalCount)
            {
                if (!deleting) // we haven't found the first one yet
                {
                    // is this the first one?
                    JournalEntryPageFunction journalPageFunction =
                        journal[journalEntryIndex] as JournalEntryPageFunction;
                    deleting = (journalPageFunction != null) &&
                        (journalPageFunction.PageFunctionId == finishingChildPageFunction.PageFunctionId);
                }
                if (deleting)
                {
                    journal.RemoveEntryInternal(journalEntryIndex);
                }
                else
                {
                    journalEntryIndex++;
                }
            }
            if (deleting)
            {
                journal.UpdateView(); // RemoveEntryInternal() doesn't do this.
            }
            else
            {
                // If the PF is not found, it simply wasn't journaled, and it must be the
                // current page.
                if (object.ReferenceEquals(_bp, finishingChildPageFunction))
                {
                    Debug.Assert(parentEntryIndex < journal.CurrentIndex);
                    journal.ClearForwardStack();
                }
                else
                {
                    Debug.Fail("Could not find the finishing PageFunction in the journal.");
                }
            }

            // When the next navigation occurs (back to parent or new), the current page
            // (finishingChildPageFunction or another PF started by it) should not be journaled.
            _doNotJournalCurrentContent = true;
        }

        // Navigate to the Parent page.
        // Two cases:
        //     Parent is a PageFunction:  parentPF is not null.
        //     Parent is a Non PageFunction: parentPF is null,
        //                                   the valid info are parentIndex and ReturnEventArgs.
        // The kind of navigation depends on finishingChildPageFunction.RemoveFromJournal:
        //   - True: then do journal navigation to the parent page (and no journal entry created
        //      for the finishing PF)
        //   - False: do new navigation to the parent page.
        private void NavigateToParentPage(PageFunctionBase finishingChildPageFunction, PageFunctionBase parentPF, object returnEventArgs, int parentIndex)
        {
            JournalEntry parentEntry = (JournalScope.Journal)[parentIndex];


            if (parentPF != null)
            {
                // We shouldn't be navigating to a PageFunction that's UiLess at this stage.
                // By now it should have started another navigation it was delegating to a child PF.
                if (parentEntry.EntryType == JournalEntryType.UiLess)
                    throw new InvalidOperationException(SR.Get(SRID.UiLessPageFunctionNotCallingOnReturn));

                NavigateInfo navInfo = finishingChildPageFunction.RemoveFromJournal ?
                    new NavigateInfo(parentEntry.Source, NavigationMode.Back, parentEntry) :
                    new NavigateInfo(parentEntry.Source, NavigationMode.New);
                Navigate(parentPF, navInfo);
                return;
            }

            // Handle the NonPF parent page case.
            // Passing PageFunctionReturnInfo signals that the Return event should be raised for
            // the finishing child PF.
            PageFunctionReturnInfo pfRetInfo =
                finishingChildPageFunction.RemoveFromJournal ?
                new PageFunctionReturnInfo(finishingChildPageFunction, parentEntry.Source,
                    NavigationMode.Back, parentEntry, returnEventArgs) :
                new PageFunctionReturnInfo(finishingChildPageFunction, parentEntry.Source,
                    NavigationMode.New, null, returnEventArgs);
            if (parentEntry is JournalEntryUri)
            {
                this.Navigate(parentEntry.Source, pfRetInfo);
            }
            else if (parentEntry is JournalEntryKeepAlive)
            {
                object root = ((JournalEntryKeepAlive)parentEntry).KeepAliveRoot;
                this.Navigate(root, pfRetInfo);
            }
            else
            {
                Debug.Fail("Unhandled scenario: PageFunction returning to " + parentEntry.GetType().Name);
            }
        }

        //
        // Check if the passed object is a valid root element.
        //
        private bool IsValidRootElement(object bp)
        {
            bool isValidRoot = true;

            // Future:
            //    Work out a final logic to determine what object is valid root element for Navigation.
            //    Please also update the exception message for WrongNavigateRootElement in message file
            //    ExceptionStringTable.txt.
            //
            // For now, only block Window as root element.
            if (AllowWindowNavigation == false &&
                bp != null &&
                bp is Window)
            {
                isValidRoot = false;
            }

            return isValidRoot;
        }

        #endregion Private Functions

        #region Events

        // <summary>
        // BPReady event
        // </summary>
        internal event BPReadyEventHandler BPReady;
        internal event BPReadyEventHandler PreBPReady;

        #endregion

        #region Private Properties

        /// <summary>
        /// This property returns a JournalNavigationScope if available but doesn't force creating one.
        /// So, a Frame with JournalOwnership=Automatic for which there is no parent JNS available
        /// (must be rooted in something other than NavigationWindow) will not be forced to create
        /// its own JNS/journal. If a journal is really needed (for example, to journal a page from
        /// which we are navigating away), call EnsureJournal(). However, because navigator trees can be
        /// constructed bottom-up, most times this property should be used instead of EnsureJournal().
        /// This will prevent prematurely forcing Frame to establish its own JournalNavigationScope.
        /// </summary>
        /// <remarks> The tree of NavigationServices may comprise multiple JournalNavigationScopes.
        /// See the ParentNavigationService property.
        /// </remarks>
        private JournalNavigationScope JournalScope
        {
            get
            {
                if (_journalScope == null && _navigatorHost != null)
                {
                    _journalScope = _navigatorHost.GetJournal(false/*don't create*/);
                }
                return _journalScope;
            }
        }

        // This property indicates if this was the navigation service that initiated the navigation
        private bool IsNavigationInitiator
        {
            get
            {
                if (!_isNavInitiatorValid)
                {
                    // If we are the top level container then we have no parent and must be the initiator of this navigation.
                    // If we are not top level we may still be the initiator but we default to false and then query our
                    // parent navigation service to see if it is also navigating.
                    _isNavInitiator = IsTopLevelContainer;

                    if (_parentNavigationService != null)
                    {
                        if (!_parentNavigationService.PendingNavigationList.Contains(this))
                        {
                            // if the parent NavigationService doesn't contain this NavigationService object,
                            // it means the parent NavigationService's host tree is not changed. this NavigationService
                            // is the topmost level that a navigation was started at
                            _isNavInitiator = true;
                        }
                    }
                    // We'd like to fix the IsNavInitiator property for island frame, more details in .
                    // However, it is a breaking change. In the Dev10 time frame, the breaking change bar is high.
                    // So instead of fixing it with the right logic, we limit the scope of the change to those that matter
                    // most - the scenario is navigation of the island Frame; the timing is after starting up.
                    // This change does not affect startup or other initial tree construction scenarios except when the
                    // Frame is explicitly marked to be island frame.
                    else if (IsJournalLevelContainer)
                    {
                        _isNavInitiator = true;
                    }

                    _isNavInitiatorValid = true;
                }

                return _isNavInitiator;
            }
        }

        #endregion Private Properties

        #region Private Fields
        private object _bp;
        /// <summary><see cref="JournalEntry.ContentId"/></summary>
        private uint _contentId;
        /// <summary>
        /// This must always be in absolute URI format (or null, for object navigation).
        /// If it's just fragment name, then pack://application,,,/#fragment.
        /// </summary>
        private Uri _currentSource;
        private Uri _currentCleanSource;
        private JournalEntryGroupState _journalEntryGroupState;
        private bool _doNotJournalCurrentContent;
        private bool _cancelContentRenderedHandling;
        /// <summary><see cref="NavigatingCancelEventArgs.ContentStateToSave"/></summary>
        private CustomContentState _customContentStateToSave;
        private CustomJournalStateInternal _rootViewerStateToSave;
        private WebRequest _request;
        private object _navState;
        private WebResponse _webResponse;
        private XamlReader _asyncObjectConverter;
        private bool _isNavInitiator;
        private bool _isNavInitiatorValid;
        private bool _allowWindowNavigation;

        private Guid _guidId = Guid.Empty;
        private INavigator _navigatorHost;
        private INavigatorImpl _navigatorHostImpl;

        /// <summary>
        /// Cached reference to the applicable JNS. Normally, should not be accessed directly.
        /// See the JournalScope property.
        /// </summary>
        private JournalNavigationScope _journalScope;
        private ArrayList _childNavigationServices = new ArrayList(2);
        private NavigationService _parentNavigationService;

        private bool _disposed;

        // IUI-specific data
        private FinishEventHandler _finishHandler;

        private NavigationStatus _navStatus = NavigationStatus.Idle;


        //
        // The next group of variables hold state for the pending navigation
        //
        // Contains a list of child frames that are still being loaded
        private ArrayList _pendingNavigationList = new ArrayList(2);
        // Contains a list of recursive navigate items, last one in the list will supercede
        // (see comments in HandleNavigating and DoStopLoading)
        private ArrayList _recursiveNavigateList = new ArrayList(2);
        // Navigation currently in progress (either waiting for DispatcherOperation to be invoked or being actively downloaded)
        private NavigateQueueItem _navigateQueueItem;
        private long _bytesRead;
        private long _maxBytes;
        private Visual _oldRootVisual;


        private const int _noParentPage = -1;

        //  Can CLRProfiler code go under ifdef PROFILING?
#if DEBUG_CLR_MEM
        private static int _navigationCLRPass = 0;
#endif

        private WebBrowser _webBrowser;
        #endregion Private Fields
    }

    #endregion NavigationService Class

    #region public Delegates

    /// <summary>
    /// Delegate for the Navigating event
    /// </summary>
    public delegate void NavigatingCancelEventHandler(Object sender, NavigatingCancelEventArgs e);

    /// <summary>
    /// Delegate for the NavigationProgress event
    /// </summary>
    public delegate void NavigationProgressEventHandler(Object sender, NavigationProgressEventArgs e);

    /// <summary>
    /// Delegate for the NavigationFailed event
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void NavigationFailedEventHandler(Object sender, NavigationFailedEventArgs e);

    /// <summary>
    /// Delegate for the Navigated event
    /// </summary>
    public delegate void NavigatedEventHandler(Object sender, NavigationEventArgs e);

    /// <summary>
    /// Delegate for the LoadCompleted event
    /// </summary>
    public delegate void LoadCompletedEventHandler(Object sender, NavigationEventArgs e);

    /// <summary>
    /// Delegate for the NavigationStopped event
    /// </summary>
    public delegate void NavigationStoppedEventHandler(Object sender, NavigationEventArgs e);

    /// <summary>
    /// Delegate for FragmentNavigation event
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void FragmentNavigationEventHandler(object sender, FragmentNavigationEventArgs e);

    #endregion public Delegates

    #region internal Delegates

    internal delegate void BPReadyEventHandler(Object sender, BPReadyEventArgs e);
    internal delegate void FinishEventHandler(PageFunctionBase sender, object ReturnEventArgs);

    #endregion internal Delegates

    #region internal class

    #region RequestState class
    internal class RequestState
    {
        internal RequestState(WebRequest request, Uri source, Object navState, Dispatcher callbackDispatcher)
        {
            _request = request;
            _source = source;
            _navState = navState;
            _callbackDispatcher = callbackDispatcher;
        }

        internal WebRequest Request
        {
            get
            {
                return _request;
            }
        }

        internal Uri Source
        {
            get
            {
                return _source;
            }
        }

        internal Object NavState
        {
            get
            {
                return _navState;
            }
        }

        internal Dispatcher CallbackDispatcher
        {
            get
            {
                return _callbackDispatcher;
            }
        }

        private WebRequest _request;
        private Uri _source;
        private Object _navState;
        private Dispatcher _callbackDispatcher;
    }
    #endregion RequestState class

    #region BPReadyEventArgs Class

    // <summary>
    // EventArgs for BPReady events
    // </summary>
    internal class BPReadyEventArgs : CancelEventArgs
    {
        // <summary>
        // constructor
        // </summary>
        internal BPReadyEventArgs(Object content, Uri uri)
            : base()
        {
            _content = content;
            _uri = uri;
        }

        // <summary>
        // property for Root
        // </summary>
        internal Object Content
        {
            get
            {
                return _content;
            }
        }

        internal Uri Uri
        {
            get
            {
                return _uri;
            }
        }

        Object _content;
        Uri _uri;
    }

    #endregion BPReadyEventArgs Class

    #region NavigateInfo class
    internal class NavigateInfo
    {
        internal NavigateInfo(Uri source)
        {
            _source = source;
        }

        internal NavigateInfo(Uri source, NavigationMode navigationMode)
        {
            _source = source;
            _navigationMode = navigationMode;
        }

        internal NavigateInfo(Uri source, NavigationMode navigationMode, JournalEntry journalEntry)
        {
            _source = source;
            _navigationMode = navigationMode;
            _journalEntry = journalEntry;
        }

        internal Uri Source
        {
            get { return _source; }
        }

        internal NavigationMode NavigationMode
        {
#if DEBUG
            [DebuggerStepThrough]
#endif
            get { return _navigationMode; }
        }

        internal JournalEntry JournalEntry
        {
#if DEBUG
            [DebuggerStepThrough]
#endif
            get { return _journalEntry; }
        }

        /// <summary>
        /// Assumption: For new navigations, there is no preexisting journal entry to go back to.
        /// For Back/Fwd, there must be an existing entry.
        /// </summary>
        internal bool IsConsistent
        {
            get
            {
                return (_navigationMode == NavigationMode.New ^ _journalEntry != null)
                    || _navigationMode == NavigationMode.Refresh;
            }
        }

        // Uri is only used for Navigate(object) codepaths to pass the pending source for Startup Uri and
        // KeepAlive journal navigations which have a Uri associated with it though we are navigating
        // by content trees
        private Uri _source;
        private NavigationMode _navigationMode = NavigationMode.New;
        private JournalEntry _journalEntry;
    }

    #endregion NavigateInfo class

    #region PageFunctionReturnInfo class
    //
    // This NavigateInfo is only used in the below case :
    // The child PageFunction is done, and the parent page is not a PageFunction.
    // In the FinishHandler, it needs to navigate to the parent, this NavigationInfo
    // is passed at that moment.
    //
    internal class PageFunctionReturnInfo : NavigateInfo
    {
        internal PageFunctionReturnInfo(PageFunctionBase finishingChildPageFunction, Uri source, NavigationMode navigationMode, JournalEntry journalEntry, object returnEventArgs)
            : base(source, navigationMode, journalEntry)
        {
            _returnEventArgs = returnEventArgs;
            _finishingChildPageFunction = finishingChildPageFunction;
        }

        internal object ReturnEventArgs
        {
            get { return _returnEventArgs; }
        }

        internal PageFunctionBase FinishingChildPageFunction
        {
            get { return _finishingChildPageFunction; }
        }

        private object _returnEventArgs;
        private PageFunctionBase _finishingChildPageFunction;
    }

    #endregion PageFunctionReturnInfo class

    #region NavigateQueueItem class
    internal class NavigateQueueItem
    {
        internal NavigateQueueItem(Uri source, object content, NavigationMode mode, Object navState, NavigationService nc)
        {
            _source = source;
            _content = content;
            _navState = navState;
            _nc = nc;
            _navigationMode = mode;
        }

    #if DEBUG
        internal bool IsPosted
        {
            get
            {
                return _postedOp != null;
            }
        }
    #endif
        internal void PostNavigation()
        {
            Debug.Assert(_postedOp == null);
            _postedOp = Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Normal, new DispatcherOperationCallback(this.Dispatch), null);
        }

        internal void Stop()
        {
            // Stop all pending navigations - ones that have been posted but not dispatched yet
            // and the ones in progress.

            // Abort dispatched navigation operations
            if (_postedOp != null)
            {
                _postedOp.Abort();
                _postedOp = null;
            }
        }

        internal Uri Source
        {
            get
            {
                return _source;
            }
        }

        internal object NavState
        {
            get
            {
                return _navState;
            }
        }

        private object Dispatch(object obj)
        {
            _postedOp = null;

            // The second check is to cover null content/null source navigations.
            // Null source navigation will be transformed to a null content navigation since we
            // cannot bind to a null source.
            if (_content != null || _source == null)
            {
                _nc.DoNavigate(_content, _navigationMode, _navState);
            }
            else
            {
                _nc.DoNavigate(_source, _navigationMode, _navState);
            }

            return null;
        }

        Uri _source;
        object _content;
        Object _navState;
        NavigationService _nc;
        NavigationMode _navigationMode = NavigationMode.New;
        DispatcherOperation _postedOp;
    }

    #endregion NavigateQueueItem class

    #region DisposeTreeQueueItem class
    /// This class walks the logical tree. We don't need to walk the visual tree
    /// since Visuals don't need to be explicitly disposed now.
    internal class DisposeTreeQueueItem
    {
        internal object Dispatch(object o)
        {
            this.DisposeElement(_root);
            return null;
        }

        /// <summary>
        /// Dispose the elements in the tree, children first.
        /// </summary>
        /// <param name="node">The node to dispose.</param>
        internal void DisposeElement(Object node)
        {
            DependencyObject dobj = node as DependencyObject;
            if (dobj != null)
            {
                bool hasChildren = false;
                IEnumerator children = LogicalTreeHelper.GetLogicalChildren(dobj);
                if (children != null)
                {
                    // Recurse into each child
                    while (children.MoveNext())
                    {
                        hasChildren = true;
                        object child = children.Current;
                        Debug.Assert(child != null);
                        DisposeElement(child);
                    }
                }
                if (!hasChildren)
                {
                    // This case is needed specifically for Frame when it has WebControl in it. (1521096)
                    // Frame.Content is not exposed as a logical child of Frame.
                    ContentControl cc = dobj as ContentControl;
                    if (cc != null && cc.ContentIsNotLogical && cc.Content != null)
                    {
                        DisposeElement(cc.Content);
                    }
                }
            }

            // Now that we've recursed through all descendants, dispose this node if it needs it
            IDisposable disposable = node as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }
        }

        internal DisposeTreeQueueItem(Object node)
        {
            Debug.Assert(node != null, "Trying to dispose a null Logical Tree Node");
            _root = node;
        }

        private Object _root;
    }
    #endregion DisposeTreeQueueItem class

    #endregion internal class
}
