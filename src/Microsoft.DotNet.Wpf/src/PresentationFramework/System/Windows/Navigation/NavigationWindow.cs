// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//              The NavigationWindow extends the base window class to provide Navigation functionality.
//
//              Using the navigation window it's possible to set the Uri of a Navigation Window, and the
//              Content region of the window displays the markup at that Uri.
//
//              It's also possible to change the outermost "chrome" of markup that hosts the content region
//              of the window.
//

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

using MS.Internal.AppModel;
using MS.Internal.KnownBoxes;
using MS.Internal.Utility;
using MS.Utility;
using MS.Win32;
using MS.Internal.PresentationFramework;

using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Markup;
using System.Windows.Threading;
using System.Windows.Documents;

namespace System.Windows.Navigation
{
    #region NavigationWindow Class

    /// <summary>
    /// Public class NavigationWindow
    /// </summary>
    /// <ExternalAPI/>
    [ContentProperty]
    [TemplatePart(Name = "PART_NavWinCP", Type = typeof(ContentPresenter))]
    public class NavigationWindow : Window, INavigator, INavigatorImpl, IDownloader, IJournalNavigationScopeHost, IUriContext
    {
        #region DependencyProperties

        /// <summary>
        /// DependencyProperty for SandboxExternalContent property.
        /// </summary>
        public static readonly DependencyProperty SandboxExternalContentProperty =
                Frame.SandboxExternalContentProperty.AddOwner(typeof(NavigationWindow));


        /// <summary>
        /// If set to true, the navigated content is isolated.
        /// </summary>
        public bool SandboxExternalContent
        {
            get { return (bool) GetValue(SandboxExternalContentProperty); }
            set
            {
                bool fSandBox = (bool)value;
                SetValue(SandboxExternalContentProperty, fSandBox);
            }
        }


        /// <summary>
        ///    Called when SandboxExternalContentProperty is invalidated on 'd'.  If the value becomes
        ///    true, then the frame is refreshed to sandbox any content.
        /// </summary>
        private static void OnSandboxExternalContentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            NavigationWindow window = (NavigationWindow)d;

            bool fSandBox = (bool)e.NewValue;
            if (fSandBox && !(bool)e.OldValue)
            {
                window.NavigationService.Refresh();
            }
        }


        private static object CoerceSandBoxExternalContentValue(DependencyObject d, object value)
        {
            bool fSandBox = (bool)value;
            return fSandBox;
        }


        /// <summary>
        /// DependencyProperty for ShowsNavigationUI property.
        /// </summary>
        public static readonly DependencyProperty ShowsNavigationUIProperty =
                DependencyProperty.Register(
                        "ShowsNavigationUI",
                        typeof(bool),
                        typeof(NavigationWindow),
                        new FrameworkPropertyMetadata(BooleanBoxes.TrueBox));

        /// <summary>
        /// DependencyProperty for BackStack property.
        /// </summary>
        public static readonly DependencyProperty BackStackProperty =
            JournalNavigationScope.BackStackProperty.AddOwner(typeof(NavigationWindow));

        /// <summary>
        /// DependencyProperty for ForwardStack property.
        /// </summary>
        public static readonly DependencyProperty ForwardStackProperty =
            JournalNavigationScope.ForwardStackProperty.AddOwner(typeof(NavigationWindow));

        /// <summary>
        ///     The DependencyProperty for the CanGoBack property.
        ///     Flags:              None
        ///     Default Value:      false
        ///     Readonly:           true
        /// </summary>
        public static readonly DependencyProperty CanGoBackProperty =
            JournalNavigationScope.CanGoBackProperty.AddOwner(typeof(NavigationWindow));

         /// <summary>
        ///     The DependencyProperty for the CanGoForward property.
        ///     Flags:              None
        ///     Default Value:      false
        ///     Readonly:           true
        /// </summary>
        public static readonly DependencyProperty CanGoForwardProperty =
            JournalNavigationScope.CanGoForwardProperty.AddOwner(typeof(NavigationWindow));

        #endregion DependencyProperties

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
        #region Constructors
        /// <summary>
        /// Constructs a window object
        /// </summary>
        static NavigationWindow()
        {
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(NavigationWindow));

            DefaultStyleKeyProperty.OverrideMetadata(
                    typeof(NavigationWindow),
                    new FrameworkPropertyMetadata(typeof(NavigationWindow)));

            ContentProperty.OverrideMetadata(
                    typeof(NavigationWindow),
                    new FrameworkPropertyMetadata(
                            null,
                            new CoerceValueCallback(CoerceContent)));

            SandboxExternalContentProperty.OverrideMetadata(
                    typeof(NavigationWindow),
                    new FrameworkPropertyMetadata(new PropertyChangedCallback(OnSandboxExternalContentPropertyChanged),
                     new CoerceValueCallback(CoerceSandBoxExternalContentValue)));


            CommandManager.RegisterClassCommandBinding(
                    typeof(NavigationWindow),
                    new CommandBinding(
                            NavigationCommands.BrowseBack,
                            new ExecutedRoutedEventHandler(OnGoBack),
                            new CanExecuteRoutedEventHandler(OnQueryGoBack)));

            CommandManager.RegisterClassCommandBinding(
                    typeof(NavigationWindow),
                    new CommandBinding(
                            NavigationCommands.BrowseForward,
                            new ExecutedRoutedEventHandler(OnGoForward),
                            new CanExecuteRoutedEventHandler(OnQueryGoForward)));

            CommandManager.RegisterClassCommandBinding(
                    typeof(NavigationWindow),
                    new CommandBinding(NavigationCommands.NavigateJournal, new ExecutedRoutedEventHandler(OnNavigateJournal)));

            CommandManager.RegisterClassCommandBinding(
                    typeof(NavigationWindow),
                    new CommandBinding(
                            NavigationCommands.Refresh,
                            new ExecutedRoutedEventHandler(OnRefresh),
                            new CanExecuteRoutedEventHandler(OnQueryRefresh)));

            CommandManager.RegisterClassCommandBinding(
                    typeof(NavigationWindow),
                    new CommandBinding(
                            NavigationCommands.BrowseStop,
                            new ExecutedRoutedEventHandler(OnBrowseStop),
                            new CanExecuteRoutedEventHandler(OnQueryBrowseStop)));
}

        /// <summary>
        /// Constructs a NavigationWindow object
        /// </summary>
        /// <remarks>
        ///     Automatic determination of current Dispatcher. Use alternative constructor
        ///     that accepts a Dispatcher for best performance.
        ///
        ///     Initialize set _framelet to false and init commandlinks
        /// </remarks>
        public NavigationWindow()
        {
            this.Initialize();
        }

        internal NavigationWindow(bool inRbw): base(inRbw)
        {
            this.Initialize();
        }

        private void Initialize()
        {
            Debug.Assert(_navigationService == null && _JNS == null);

            _navigationService = new NavigationService(this);
            _navigationService.BPReady += new BPReadyEventHandler(OnBPReady);

            _JNS = new JournalNavigationScope(this);

            _fFramelet = false;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
        #region Public Methods

        #region IDownloader implementation
        NavigationService IDownloader.Downloader
        {
            get { return _navigationService; }
        }
        #endregion IDownloader implementation

        #region INavigator Methods
        /// <summary>
        /// Navigates to the Uri and downloads the content.
        /// </summary>
        /// <param name="source">URI of the application or content being navigated to.</param>
        /// <returns>bool indicating whether the navigation was successfully started or not</returns>
        public bool Navigate(Uri source)
        {
            VerifyContextAndObjectState();
            return NavigationService.Navigate(source);
        }

        /// <summary>
        /// This method navigates this window to the given Uri.
        /// </summary>
        /// <param name="source">The URI to be navigated to.</param>
        /// <param name="extraData">
        ///     Enables the develeoper to supply an extra object, that will be returned in the NavigatedEventArgs of the Navigated event. The extra data enables the developer
        ///     to identify the source of the navigation, in the presence of
        ///     multiple navigations.
        /// </param>
        /// <returns>bool indicating whether the navigation was successfully started or not</returns>
        public bool Navigate(Uri source, Object extraData)
        {
            VerifyContextAndObjectState();

            // Close/update PS844614 to investigate why we need to delay create WNC instead of
            // creating and calling Navigate immediately here
            return NavigationService.Navigate(source, extraData);
        }

        /// <summary>
        /// Navigates to an existing element tree.
        /// </summary>
        /// <param name="content">Root of the element tree being navigated to.</param>
        /// <returns>bool indicating whether the navigation was successfully started or not</returns>
        public bool Navigate(Object content)
        {
            VerifyContextAndObjectState();

            // Close/update PS844614 to investigate why we need to delay create WNC instead of
            // creating and calling Navigate immediately here
            return NavigationService.Navigate(content);
}

        /// <summary>
        /// This method navigates this window to the
        /// given Element.
        /// </summary>
        /// <param name="content">The Element to be navigated to.</param>
        /// <param name="extraData">enables the develeoper to supply an extra object, that will be returned in the NavigatedEventArgs of the Navigated event. The extra data enables the developer
        /// to identify the source of the navigation, in the presence of
        /// multiple navigations.
        /// </param>
        /// <returns>bool indicating whether the navigation was successfully started or not</returns>
        public bool Navigate(Object content, Object extraData)
        {
            VerifyContextAndObjectState();

            // Close/update PS844614 to investigate why we need to delay create WNC instead of
            // creating and calling Navigate immediately here
            return NavigationService.Navigate(content, extraData);
         }

         JournalNavigationScope INavigator.GetJournal(bool create)
         {
             Debug.Assert(_JNS != null);
             return _JNS;
         }

        /// <summary>
        /// Navigates to the next entry in the Forward branch of the Journal, if one exists.
        /// If there is no entry in the Forward stack of the journal, the method throws an
        /// exception. The behavior is the same as clicking the Forward button.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        /// There is no entry in the Forward stack of the journal to navigate to.
        /// </exception>
        public void GoForward()
        {
            _JNS.GoForward();
        }

        /// <summary>
        /// Navigates to the previous entry in the Back branch of the Journal, if one exists.
        /// The behavior is the same as clicking the Back button.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        /// There is no entry in the Back stack of the journal to navigate to.
        /// </exception>
        public void GoBack()
        {
            _JNS.GoBack();
        }

        /// <summary>
        /// StopLoading aborts asynchronous navigations that haven't been processed yet or that are
        /// still being downloaded. SopLoading does not abort parsing of the downloaded streams.
        /// The NavigationStopped event is fired only if the navigation was aborted.
        /// The behavior is the same as clicking the Stop button.
        /// </summary>
        public void StopLoading()
        {
            VerifyContextAndObjectState();

            if (InAppShutdown)
                return;

            NavigationService.StopLoading();
        }

        /// <summary>
        /// Reloads the current content. The behavior is the same as clicking the Refresh button.
        /// </summary>
        public void Refresh()
        {
            VerifyContextAndObjectState();

            if (InAppShutdown)
            {
                return;
            }

            NavigationService.Refresh();
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
            VerifyContextAndObjectState();
            NavigationService.AddBackEntry(state);
        }

        /// <summary>
        /// Remove the first JournalEntry from NavigationWindow's back history
        /// </summary>
        public JournalEntry RemoveBackEntry()
        {
            return _JNS.RemoveBackEntry();
        }
        #endregion INavigator Methods

        #region IUriContext Members

        Uri IUriContext.BaseUri
        {
            get
            {
                return (Uri)GetValue(BaseUriHelper.BaseUriProperty);
            }
            set
            {
                SetValue(BaseUriHelper.BaseUriProperty, value);
            }
        }

        #endregion
        /// <summary>
        /// Called when style is actually applied.
        /// </summary>
        /// <remarks>
        /// We need to turn off the system icon, title and contextmenu when it is using our style.
        /// The plan is to have a property on Window to turn it on and off. EnsureVisual and the ID will be removed when we do that.
        /// This is tracked in task #12401
        /// </remarks>
        public override void OnApplyTemplate()
        {
            VerifyContextAndObjectState( );

            base.OnApplyTemplate();

            // base actually changed something.  sniff
            // around to see if there are any framelet
            // objects we need to hook up.

            // Get the root element of the style
            FrameworkElement root = (this.GetVisualChild(0)) as FrameworkElement;

            if (_navigationService != null)
            {
                _navigationService.VisualTreeAvailable(root);
            }

            // did we just apply the framelet style?
            if ((root != null) && (root.Name == "NavigationBarRoot"))
            {
                if (!_fFramelet)
                {
                    // transitioning to Framelet style

                    // Use Window property for this once available
                    // turn off drawing of title in window header
                    // This is tracked in task # 12401.

#if WCP_SYSTEM_THEMES_ENABLED
                    NativeMethods.WTA_OPTIONS wo = new NativeMethods.WTA_OPTIONS();
                    wo.dwFlags = (NativeMethods.WTNCA_NODRAWCAPTION | NativeMethods.WTNCA_NODRAWICON | NativeMethods.WTNCA_NOSYSMENU);
                    wo.dwMask  = NativeMethods.WTNCA_VALIDBITS;

                    // call to turn off theme parts
                    UnsafeNativeMethods.SetWindowThemeAttribute( new HandleRef(this,Handle), NativeMethods.WINDOWTHEMEATTRIBUTETYPE.WTA_NONCLIENT, wo );
#endif // WCP_SYSTEM_THEMES_ENABLED
                    _fFramelet = true;
                }
            }
            else
            {
                if (_fFramelet)
                {
                    // Use Window property for this once available
                    // turn on drawing of title in window header

#if WCP_SYSTEM_THEMES_ENABLED
                    NativeMethods.WTA_OPTIONS wo = new NativeMethods.WTA_OPTIONS();
                    wo.dwFlags = 0;
                    wo.dwMask  = NativeMethods.WTNCA_VALIDBITS;

                    // call to turn off theme parts
                    UnsafeNativeMethods.SetWindowThemeAttribute( new HandleRef(this,Handle), NativeMethods.WINDOWTHEMEATTRIBUTETYPE.WTA_NONCLIENT, wo );
#endif // WCP_SYSTEM_THEMES_ENABLED

                    // no longer in framelet mode
                    _fFramelet = false;
                }
            }
        }

        /// <summary>
        /// This method is used by TypeDescriptor to determine if this property should
        /// be serialized.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool ShouldSerializeContent()
        {
            // When uri of NavigationService is valid and can be used to
            // relaod, we do not serialize content
            if (_navigationService != null)
            {
                return !_navigationService.CanReloadFromUri;
            }

            return true;
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
        #region Public Properties
        /// <summary>
        /// NavigationWindow's NavigationService
        /// </summary>
        public NavigationService NavigationService
        {
            #if DEBUG
            [DebuggerStepThrough]
            #endif
            get
            {
                VerifyContextAndObjectState();
                return _navigationService;
            }
        }

        /// <summary>
        /// List of back journal entries
        /// </summary>
        public IEnumerable BackStack
        {
            get { return _JNS.BackStack; }
        }

        /// <summary>
        /// List of forward journal entries
        /// </summary>
        public IEnumerable ForwardStack
        {
            get { return _JNS.ForwardStack; }
        }

        /// <summary>
        /// Determines whether to show the default navigation UI.
        /// </summary>
        public bool ShowsNavigationUI
        {
            get
            {
                VerifyContextAndObjectState();
                return (bool)GetValue(ShowsNavigationUIProperty);
            }

            set
            {
                VerifyContextAndObjectState();
                SetValue(ShowsNavigationUIProperty, value);
            }
        }

        #region INavigator Properties
        /// <summary>
        ///
        /// </summary>
        public static readonly DependencyProperty SourceProperty =
                Frame.SourceProperty.AddOwner(
                        typeof(NavigationWindow),
                        new FrameworkPropertyMetadata(
                                (Uri)null,
                                new PropertyChangedCallback(OnSourcePropertyChanged)));

        /// <summary>
        ///    Called when SourceProperty is invalidated on 'd'
        /// </summary>
        private static void OnSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            NavigationWindow navWin = (NavigationWindow)d;

            // Don't navigate if the Source value change is from NavService as a result of a navigation happening.
            if (! navWin._sourceUpdatedFromNavService )
            {
                // We used not to need to resolve the Uri here because we did not allow navigating to a xaml rooted inside a NavWin.
                // Now after we allow this, we will need to support navigating to the following xaml,
                //        <NavigationWindow ... Source="a relative uri." ...>
                // When the xaml is rooted in a NavigationWindow with source pointing to a relative uri, the relative uri will need
                // to be resolved with its baseuri unless it is a fragment navigation or there is no baseuri.
                // In this case NavWin is always the root, parser will set the BaseUriProperty for the root element so NavWin doesn't need to
                // implement IUriContext.
                Uri uriToNavigate = BindUriHelper.GetUriToNavigate(navWin, d.GetValue(BaseUriHelper.BaseUriProperty) as Uri, (Uri)e.NewValue);

                // Calling the internal Navigate from Frame and NavWin's Source DP's property changed callbacks
                // We would not set value back in this case.
                navWin._navigationService.Navigate(uriToNavigate, null, false, true/* navigateOnSourceChanged */);
            }
        }

        // This method is called from NavService whenever the NavService's Source value is updated.
        // The INavigator uses this to update its SourceProperty.
        // <param name="journalOrCancel">It indicates whether the NavService's Source value is as a result of
        // calling Navigate API directly or from GoBack/GoForward, journal navigation, a cancellation</param>
        void INavigatorImpl.OnSourceUpdatedFromNavService(bool journalOrCancel)
        {
            try
            {
                _sourceUpdatedFromNavService = true;
                SetCurrentValueInternal(SourceProperty, _navigationService.Source);
            }
            finally
            {
                _sourceUpdatedFromNavService = false;
            }
        }

        /// <summary>
        /// Uri for the page currently contained by the NavigationWindow
        ///     - Setting this property performs a navigation to the specified Uri.
        ///     - Getting this property when a navigation is not in progress returns the URI of
        ///       the current page. Getting this property when a navigation is in progress returns
        ///       the URI of the page being navigated to.
        /// </summary>
        /// <remarks>
        /// Supporting navigation via setting a property makes it possible to write
        /// a NavigationWindow in markup and specify its initial content.
        /// </remarks>
        [DefaultValue(null)]
        public Uri Source
        {
            get { return (Uri)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        /// <summary>
        /// Uri for the current page in the window. Getting this property always
        /// returns the URI of the content thats currently displayed in the window,
        /// regardless of whether a navigation is in progress or not.
        /// </summary>
        public Uri CurrentSource
        {
            get
            {
                VerifyContextAndObjectState( );
                return (_navigationService == null ? null : _navigationService.CurrentSource);
            }
        }

        /// <summary>
        /// Tells whether there are any entries in the Forward branch of the Journal.
        /// This property can be used to enable the Forward button.
        /// </summary>
        public bool CanGoForward
        {
            get
            {
                return _JNS.CanGoForward;
            }
        }

        /// <summary>
        /// Tells whether there are any entries in the Back branch of the Journal.
        /// This property can be used to enable the Back button.
        /// </summary>
        public bool CanGoBack
        {
            get
            {
                return _JNS.CanGoBack;
            }
        }

        #endregion INavigator Properties

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------
        #region Public Events

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
        public event NavigatingCancelEventHandler Navigating
        {
            add
            {
                VerifyContextAndObjectState( );
                NavigationService.Navigating += value;
            }
            remove
            {
                VerifyContextAndObjectState( );
                NavigationService.Navigating -= value;
            }
        }

        /// <summary>
        /// Raised at periodic intervals while a navigation is taking place.
        /// The NavigationProgressEventArgs tell how many total bytes need to be downloaded and
        /// how many have been sent at the moment the event is fired. This event can be used to provide
        /// a progress indicator to the user.
        /// </summary>
        public event NavigationProgressEventHandler NavigationProgress
        {
            add
            {
                VerifyContextAndObjectState( );
                NavigationService.NavigationProgress += value;
            }
            remove
            {
                VerifyContextAndObjectState( );
                NavigationService.NavigationProgress -= value;
            }
        }

        /// <summary>
        /// Raised when an error is encountered during a navigation.
        /// The NavigationFailedEventArgs contains
        /// the exception that was thrown. By default Handled property is set to false,
        /// which allows the exception to be rethrown.
        /// The event handler can prevent exception from throwing
        /// to the user by setting the Handled property to true.
        /// </summary>
        public event NavigationFailedEventHandler NavigationFailed
        {
            add
            {
                VerifyContextAndObjectState();
                NavigationService.NavigationFailed += value;
            }
            remove
            {
                VerifyContextAndObjectState();
                NavigationService.NavigationFailed -= value;
            }
        }

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
        public event NavigatedEventHandler Navigated
        {
            add
            {
                VerifyContextAndObjectState( );
                NavigationService.Navigated += value;
            }
            remove
            {
                VerifyContextAndObjectState( );
                NavigationService.Navigated -= value;
            }
        }

        /// <summary>
        /// Raised after the entire page, including all images and frames, has been downloaded
        /// and parsed. This is the event to handle to stop spinning the globe. The developer
        /// should check the IsNavigationInitiator property on the NavigationEventArgs to determine
        /// whether to stop spinning the globe.
        /// The NavigationEventArgs contain the uri or root element of the content being navigated to,
        /// and a IsNavigationInitiator property that indicates whether this is a new navigation
        /// initiated by this window, or whether this navigation is being propagated down
        /// from a higher level navigation taking place in a containing window or frame.
        /// This event is informational only, and cannot be canceled.
        /// </summary>
        public event LoadCompletedEventHandler LoadCompleted
        {
            add
            {
                VerifyContextAndObjectState( );
                NavigationService.LoadCompleted += value;
            }
            remove
            {
                VerifyContextAndObjectState( );
                NavigationService.LoadCompleted -= value;
            }
        }

        /// <summary>
        /// Raised when a navigation or download has been interrupted because the user clicked
        /// the Stop button, or the Stop method was invoked.
        /// The NavigationEventArgs contain the uri or root element of the content being navigated to.
        /// This event is informational only, and cannot be canceled.
        /// </summary>
        public event NavigationStoppedEventHandler NavigationStopped
        {
            add
            {
                VerifyContextAndObjectState( );
                NavigationService.NavigationStopped += value;
            }
            remove
            {
                VerifyContextAndObjectState( );
                NavigationService.NavigationStopped -= value;
            }
        }

        /// <summary>
        /// Raised when a navigation uri contains a fragment.  This event is fired before the element is scrolled
        /// into view and allows the listener to respond to the fragment in a custom way.
        /// </summary>
        public event FragmentNavigationEventHandler FragmentNavigation
        {
            add
            {
                VerifyContextAndObjectState();
                NavigationService.FragmentNavigation += value;
            }
            remove
            {
                VerifyContextAndObjectState();
                NavigationService.FragmentNavigation -= value;
            }
        }

        #endregion Public Events

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------
        #region Protected Methods
        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new NavigationWindowAutomationPeer(this);
        }

        /// <summary>
        ///  Add an object child to this control
        /// </summary>
        protected override void AddChild(object value)
        {
            throw new InvalidOperationException(SR.Get(SRID.NoAddChild));
        }

        /// <summary>
        ///  Add a text string to this control
        /// </summary>
        protected override void AddText(string text)
        {
            XamlSerializerUtil.ThrowIfNonWhiteSpaceInAddText(text, this);
        }

        /// <summary>
        ///     This even fires when window is closed. This event is non cancelable and is
        ///     for user infromational purposes
        /// </summary>
        /// <remarks>
        ///     This method follows the .Net programming guideline of having a protected virtual
        ///     method that raises an event, to provide a convenience for developers that subclass
        ///     the event. If you override this method - you need to call Base.OnClosed(...) for
        ///     the corresponding event to be raised.
        /// </remarks>
        protected override void OnClosed(EventArgs args)
        {
            VerifyContextAndObjectState( );
            // We override OnClosed here to Dispose of the NCTree.
            base.OnClosed( args ) ;

            // detach the event handlers on the NC
            if(_navigationService != null)
                _navigationService.Dispose();
        }

        #endregion Protected Methods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        #region Internal Methods

        internal override void OnPreApplyTemplate()
        {
            base.OnPreApplyTemplate();

            // This causes the Journal instance to be created. BackStackProperty and ForwardStackProperty
            // have to be set before the navigation chrome data-binds to them but after RootBrowserWindow
            // .SetJournalForBrowserInterop() is called.
            _JNS.EnsureJournal();
        }

        #region IJournalNavigationScopeHost Members

        void IJournalNavigationScopeHost.VerifyContextAndObjectState()
        {
            VerifyContextAndObjectState();
        }

        void IJournalNavigationScopeHost.OnJournalAvailable()
        {
        }

        bool IJournalNavigationScopeHost.GoBackOverride()
        {
            return false; // not overriding here; RBW does
        }
        bool IJournalNavigationScopeHost.GoForwardOverride()
        {
            return false;
        }

        NavigationService IJournalNavigationScopeHost.NavigationService
        {
            get { return _navigationService; }
        }

        #endregion

        #region INavigatorImpl members

        // Note: OnSourceUpdatedFromNavService is next to the other Source-related members of NW.

        Visual INavigatorImpl.FindRootViewer()
        {
            return NavigationHelper.FindRootViewer(this, "PART_NavWinCP");
        }

        #endregion INavigatorImpl

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------
        #region Internal Properties

        /// <summary>
        /// Journal for the window. Maintains Back/Forward navigation history.
        /// </summary>
        #if DEBUG
        // to prevent creating the Journal instance prematurely while debugging
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        #endif
        internal Journal Journal
        {
            get
            {
                return _JNS.Journal;
            }
        }

        internal JournalNavigationScope JournalNavigationScope
        {
            #if DEBUG
            [DebuggerStepThrough]
            #endif
            get
            {
                return _JNS;
            }
        }

        #endregion Internal Properties


        #region Private Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        private static object CoerceContent(DependencyObject d, object value)
        {
            // whenever content changes, defer the change until the Navigate comes in
            NavigationWindow w = (NavigationWindow) d;

            if (w.NavigationService.Content == value)
            {
                return value;
            }

            w.Navigate(value);
            return DependencyProperty.UnsetValue;
        }

        private void OnBPReady(Object sender, BPReadyEventArgs e)
        {
            // set Window.ContentProperty
            Content = e.Content;
        }


        private static void OnGoBack(object sender, ExecutedRoutedEventArgs args)
        {
            NavigationWindow nw = sender as NavigationWindow;
            Debug.Assert(nw != null, "sender must be of type NavigationWindow.");
            nw.GoBack();
        }

        private static void OnQueryGoBack(object sender, CanExecuteRoutedEventArgs e)
        {
            NavigationWindow nw = sender as NavigationWindow;
            Debug.Assert(nw != null, "sender must be of type NavigationWindow.");
            e.CanExecute = nw.CanGoBack;
            e.ContinueRouting = !nw.CanGoBack;
        }

        private static void OnGoForward(object sender, ExecutedRoutedEventArgs e)
        {
            NavigationWindow nw = sender as NavigationWindow;
            Debug.Assert(nw != null, "sender must be of type NavigationWindow.");
            nw.GoForward();
        }

        private static void OnQueryGoForward(object sender, CanExecuteRoutedEventArgs e)
        {
            NavigationWindow nw = sender as NavigationWindow;
            Debug.Assert(nw != null, "sender must be of type NavigationWindow.");
            e.CanExecute = nw.CanGoForward;
            e.ContinueRouting = !nw.CanGoForward;
        }

        private static void OnRefresh(object sender, ExecutedRoutedEventArgs e)
        {
            NavigationWindow nw = sender as NavigationWindow;
            Debug.Assert(nw != null, "sender must be of type NavigationWindow.");
            nw.Refresh();
        }

        private static void OnQueryRefresh(object sender, CanExecuteRoutedEventArgs e)
        {
            NavigationWindow nw = sender as NavigationWindow;
            Debug.Assert(nw != null, "sender must be of type NavigationWindow.");
            e.CanExecute = nw.Content != null;
        }

        private static void OnBrowseStop(object sender, ExecutedRoutedEventArgs e)
        {
            NavigationWindow nw = sender as NavigationWindow;
            Debug.Assert(nw != null, "sender must be of type NavigationWindow.");
            nw.StopLoading();
        }

        private static void OnQueryBrowseStop(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private static void OnNavigateJournal(object sender, ExecutedRoutedEventArgs e)
        {
            NavigationWindow nw = sender as NavigationWindow;
            Debug.Assert(nw != null, "sender must be of type NavigationWindow.");

            FrameworkElement journalEntryUIElem = e.Parameter as FrameworkElement;
            if (journalEntryUIElem != null)
            {
                // Get journal entry from MenuItem
                JournalEntry je = journalEntryUIElem.DataContext as JournalEntry;
                if (je != null)
                {
                    nw.JournalNavigationScope.NavigateToEntry(je);
                }
            }
        }

        #endregion Private Methods

        #region Private Properties

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        private bool InAppShutdown
        {
            get
            {
                return System.Windows.Application.IsShuttingDown;
            }
}

        //
        //  This property
        //  1. Finds the correct initial size for the _effectiveValues store on the current DependencyObject
        //  2. This is a performance optimization
        //
        internal override int EffectiveValuesInitialSize
        {
            get { return 42; }
        }

        #endregion Private Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private NavigationService       _navigationService;
        private JournalNavigationScope  _JNS;
        private bool                  _sourceUpdatedFromNavService;

        // Framelet stuff

        private bool                    _fFramelet;

        #endregion Private Fields

        #region DTypeThemeStyleKey

        // Returns the DependencyObjectType for the registered ThemeStyleKey's default
        // value. Controls will override this method to return approriate types.
        internal override DependencyObjectType DTypeThemeStyleKey
        {
            get { return _dType; }
        }

        private static DependencyObjectType _dType;

        #endregion DTypeThemeStyleKey
    }

    #endregion NavigationWindow Class
 }

