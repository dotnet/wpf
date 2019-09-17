// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      Frame is a ContentControl with navigation and journaling capabilities, much like NavigationWindow.
//      It can use its own journal ("island frame") or its prent's, if available.
//

using System;
using System.Net;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Threading;
using System.Security;

using System.Windows;
using System.Windows.Input;
using System.Windows.Automation.Peers;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Markup;

using MS.Internal;
using MS.Internal.AppModel;
using MS.Internal.Utility;
using MS.Internal.KnownBoxes;
using MS.Utility;
using MS.Internal.Controls;
using MS.Internal.Telemetry.PresentationFramework;
using System.Collections.Generic;

using SecurityHelper=MS.Internal.PresentationFramework.SecurityHelper;


namespace System.Windows.Navigation
{
    /// <summary>
    /// Journaling options for Frame
    /// </summary>
    [Serializable]
    public enum JournalOwnership
    {
        /// <summary>
        /// Whether or not this Frame will create and use its own journal depends on its parent.
        /// If the Frame is hosted by another Frame or a NavigationWindow, it behaves as though
        /// UseParentJournal was set. If it is not hosted by a Frame or NavigationWindow or all
        /// containing frames have the UsesParentJournal setting, this frame will use its own journal.
        /// Once a frame creates its own journal, switching to Automatic has no effect.
        /// </summary>
        Automatic = 0,

        /// <summary>
        /// The Frame has its own Journal which operates independent of the hosting container’s
        /// journal (if it has one).
        /// </summary>
        OwnsJournal,

        /// <summary>
        /// The Frame’s journal entries are merged into the hosting container’s journal, if available.
        /// Otherwise navigations in this frame are not journaled.
        /// </summary>
        UsesParentJournal
    };

    /// <summary>
    /// </summary>
    public enum NavigationUIVisibility
    {
        /// <summary>
        /// The navigation UI is visible when Frame has its own journal.
        /// </summary>
        Automatic = 0,
        /// <summary>
        /// </summary>
        Visible,
        /// <summary>
        /// </summary>
        Hidden
    };
}

namespace System.Windows.Controls
{
    /// <summary>
    /// Frame control is an area that is used for loading a tree of elements.
    /// It uses the application navigation model to populate its content.
    /// Hence its content model is dictated solely by the NavigationService it aggregates which has
    /// a Uri property that points to the Uri of the page that is to be loaded into the Frame.
    /// There is also a Content property that returns the root element of the Framework tree being loaded from the Uri.
    /// It is also possible to create a tree for the Frames content programmatically and set the Content property to it.
    /// </summary>
    [DefaultProperty("Source"), DefaultEvent("Navigated")]
    [Localizability(LocalizationCategory.Ignore)]
#if OLD_AUTOMATION
    [Automation(AccessibilityControlType = "Window")]
#endif
    [ContentProperty]
    [TemplatePart(Name = "PART_FrameCP", Type = typeof(ContentPresenter))]
    public class Frame : ContentControl, INavigator, INavigatorImpl, IJournalNavigationScopeHost, IDownloader, IJournalState, IAddChild, IUriContext
    {
        #region Constructors

        /// <summary>
        ///     Default constructor
        /// </summary>
        /// <remarks>
        ///     Automatic determination of current Dispatcher. Use alternative constructor
        ///     that accepts a Dispatcher for best performance.
        /// </remarks>
        public Frame() : base()
        {
            Init();
        }

        static Frame()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Frame), new FrameworkPropertyMetadata(typeof(Frame)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(Frame));

            ContentProperty.OverrideMetadata(
                    typeof(Frame),
                    new FrameworkPropertyMetadata(
                            null,
                            new CoerceValueCallback(CoerceContent)));

            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(Frame), new FrameworkPropertyMetadata(KeyboardNavigationMode.Local));
            KeyboardNavigation.ControlTabNavigationProperty.OverrideMetadata(typeof(Frame), new FrameworkPropertyMetadata(KeyboardNavigationMode.Once));

            NavigationService.NavigationServiceProperty.OverrideMetadata(
                    typeof(Frame),
                    new FrameworkPropertyMetadata(new PropertyChangedCallback(OnParentNavigationServiceChanged)));

            ControlsTraceLogger.AddControl(TelemetryControls.Frame);
        }

        private static object CoerceContent(DependencyObject d, object value)
        {
            // whenever content changes, defer the change until the Navigate comes in
            Frame f = (Frame) d;

            if (f._navigationService.Content == value)
            {
                return value;
            }

            f.Navigate(value);
            return DependencyProperty.UnsetValue;
        }

        /// <summary>
        /// Initialize
        /// </summary>
        private void Init()
        {
            InheritanceBehavior = InheritanceBehavior.SkipToAppNow;
            ContentIsNotLogical = true;
            _navigationService = new NavigationService(this);
            _navigationService.BPReady += new BPReadyEventHandler(_OnBPReady);
        }

        #endregion

        #region IUriContext implementation
        /// <summary>
        ///     Accessor for the base uri of the frame
        /// </summary>
        Uri IUriContext.BaseUri
        {
            get
            {
                return  BaseUri;
            }
            set
            {
                BaseUri = value;
            }
        }

        /// <summary>
        ///    Implementation for BaseUri
        /// </summary>
        protected virtual Uri BaseUri
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

        #endregion IUriContext implementation

        #region IDownloader implementation
        NavigationService IDownloader.Downloader
        {
            get { return _navigationService; }
        }

        /// <summary>
        ///     Rasied when Content is rendered and ready for user interaction.
        /// </summary>
        public event EventHandler ContentRendered;

        /// <summary>
        ///     This override fires the ContentRendered event.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnContentRendered(EventArgs args)
        {
            // After the content is rendered we want to check if there is an element that needs to be focused
            // If there is - set focus to it
            DependencyObject doContent = Content as DependencyObject;
            if (doContent != null)
            {
                IInputElement focusedElement = FocusManager.GetFocusedElement(doContent) as IInputElement;
                if (focusedElement != null)
                    focusedElement.Focus();
            }

            if (ContentRendered != null)
            {
                ContentRendered(this, args);
            }
        }
        #endregion IDownloader implementation

        #region Properties
        /// <summary>
        ///
        /// </summary>
        public static readonly DependencyProperty SourceProperty =
                DependencyProperty.Register(
                        "Source",
                        typeof(Uri),
                        typeof(Frame),
                        new FrameworkPropertyMetadata(
                                (Uri) null,
                                // The Journal flag tells the parser not to re-assign the property
                                // when doing journal navigation. See ParserContext.SkipJournaledProperties.
                                FrameworkPropertyMetadataOptions.Journal,
                                new PropertyChangedCallback(OnSourcePropertyChanged),
                                new CoerceValueCallback(CoerceSource)));

        private static object CoerceSource(DependencyObject d, object value)
        {
            Frame frame = (Frame)d;

            // If the Source property is coerced from NavService as a result of navigation, not from other
            // source, e.g, SetValue, DataBinding, Style..., we should use NavService.Source.
            if (frame._sourceUpdatedFromNavService)
            {
                Invariant.Assert(frame._navigationService != null, "_navigationService should never be null here");
                // Turn this Assert on after fix the issue that NavService.Source is not absolute for SiteOfOrigin.
                //Invariant.Assert(frame._navigationService.Source != null ? !frame._navigationService.Source.IsAbsoluteUri : true, "NavService's Source should always be relative");
                return frame._navigationService.Source;
            }

            return value;
        }

        /// <summary>
        ///    Called when SourceProperty is invalidated on 'd'
        /// </summary>
        private static void OnSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Frame frame = (Frame)d;

            // Don't navigate if the Source value change is from NavService as a result of a navigation happening.
            if (! frame._sourceUpdatedFromNavService)
            {
                // We used to unhook first visual child here.
                // Since we enabled styling for Frame. We're relying on Content property and ContentPresenter in Frame's style
                // to add/remove content from VisualTree.
                Uri uriToNavigate = BindUriHelper.GetUriToNavigate(frame, ((IUriContext)frame).BaseUri, (Uri)e.NewValue);

                // Calling the internal Navigate from Frame and NavWin's Source DP's property changed callbacks
                // We would not set value back in this case.
                frame._navigationService.Navigate(uriToNavigate, null, false, true/* navigateOnSourceChanged */);
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
        /// URI to navigate to.
        /// </summary>
        [Bindable(true), CustomCategory("Navigation")]
        public Uri Source
        {
            get { return (Uri) GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the CanGoBack property.
        ///     Flags:              None
        ///     Default Value:      false
        ///     Readonly:           true
        /// </summary>
        public static readonly DependencyProperty CanGoBackProperty =
            JournalNavigationScope.CanGoBackProperty.AddOwner(typeof(Frame));

        /// <summary>
        ///     The DependencyProperty for the CanGoForward property.
        ///     Flags:              None
        ///     Default Value:      false
        ///     Readonly:           true
        /// </summary>
        public static readonly DependencyProperty CanGoForwardProperty =
            JournalNavigationScope.CanGoForwardProperty.AddOwner(typeof(Frame));

        /// <summary>
        /// List of back journal entries. Available only when the frame owns its own journal.
        /// </summary>
        public static readonly DependencyProperty BackStackProperty =
            JournalNavigationScope.BackStackProperty.AddOwner(typeof(Frame));

        /// <summary>
        /// List of back journal entries. Available only when the frame owns its own journal.
        /// </summary>
        public static readonly DependencyProperty ForwardStackProperty =
            JournalNavigationScope.ForwardStackProperty.AddOwner(typeof(Frame));

        /// <summary>
        /// </summary>
        public static readonly DependencyProperty NavigationUIVisibilityProperty =
            DependencyProperty.Register(
                "NavigationUIVisibility", typeof(NavigationUIVisibility), typeof(Frame),
                new PropertyMetadata(NavigationUIVisibility.Automatic));

        /// <summary>
        /// </summary>
        public NavigationUIVisibility NavigationUIVisibility
        {
            get { return (NavigationUIVisibility)GetValue(NavigationUIVisibilityProperty); }
            set { SetValue(NavigationUIVisibilityProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for SandboxExternalContent property.
        /// </summary>
        public static readonly DependencyProperty SandboxExternalContentProperty =
                DependencyProperty.Register(
                        "SandboxExternalContent",
                        typeof(bool),
                        typeof(Frame),
                        new FrameworkPropertyMetadata(BooleanBoxes.FalseBox,
                                new PropertyChangedCallback(OnSandboxExternalContentPropertyChanged), new CoerceValueCallback(CoerceSandBoxExternalContentValue)));

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
            Frame frame = (Frame)d;
            bool fSandBox = (bool)e.NewValue;
            if (fSandBox && !(bool)e.OldValue)
            {
                frame.NavigationService.Refresh();
            }
        }

        private static object CoerceSandBoxExternalContentValue(DependencyObject d, object value)
        {
            bool fSandBox = (bool)value;
            return fSandBox;
        }

        //
        // JournalOwnership

        /// <summary>
        /// DependencyProperty for the JournalOwnership property
        /// </summary>
        public static readonly DependencyProperty JournalOwnershipProperty =
                DependencyProperty.Register(
                    "JournalOwnership", typeof(JournalOwnership), typeof(Frame),
                    new FrameworkPropertyMetadata(
                        JournalOwnership.Automatic,
                        new PropertyChangedCallback(OnJournalOwnershipPropertyChanged),
                        new CoerceValueCallback(CoerceJournalOwnership)),
                    new ValidateValueCallback(ValidateJournalOwnershipValue));

        private static bool ValidateJournalOwnershipValue(object value)
        {
            JournalOwnership jo = (JournalOwnership)value;
            return jo == JournalOwnership.Automatic || jo == JournalOwnership.UsesParentJournal
                || jo == JournalOwnership.OwnsJournal;
        }

        private static void OnJournalOwnershipPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Frame)d).OnJournalOwnershipPropertyChanged((JournalOwnership)e.NewValue);
        }

        private void OnJournalOwnershipPropertyChanged(JournalOwnership newValue)
        {
            // NavigationService.InvalidateJournalNavigationScope() will be called (recursively)
            // when making an ownership change and will throw if there is a navigation in progress.

            switch (_journalOwnership/*previous value*/)
            {
                case JournalOwnership.Automatic:
                    switch (newValue)
                    {
                        case JournalOwnership.OwnsJournal:
                            SwitchToOwnJournal();
                            break;
                        case JournalOwnership.UsesParentJournal:
                            SwitchToParentJournal();
                            break;
                    }
                    break;
                case JournalOwnership.OwnsJournal:
                    Debug.Assert(_ownJournalScope != null);
                    switch (newValue)
                    {
                        case JournalOwnership.Automatic:
                            Debug.Fail("UsesOwnJournal->Automatic transition should be blocked by CoerceJournalOwnership().");
                            break;
                        case JournalOwnership.UsesParentJournal:
                            SwitchToParentJournal();
                            break;
                    }
                    break;
                case JournalOwnership.UsesParentJournal:
                    Debug.Assert(_ownJournalScope == null);
                    switch (newValue)
                    {
                        case JournalOwnership.Automatic:
                            // The effective journal ownership is not going to change unless the
                            // frame is reparented (or, more unlikely, the parent's journal becomes
                            // unavailable). This invalidation is done only so that the next
                            // navigation causes a switch to UsesParentJournal, to be consistent
                            // with the initial transition from Automatic.
                            _navigationService.InvalidateJournalNavigationScope();
                            break;
                        case JournalOwnership.OwnsJournal:
                            SwitchToOwnJournal();
                            break;
                    }
                    break;
            }

            _journalOwnership = newValue;
        }

        private static object CoerceJournalOwnership(DependencyObject d, object newValue)
        {
            JournalOwnership prevValue = ((Frame)d)._journalOwnership;
            // Switching from OwnsJournal to Automatic is not defined to have any useful effect.
            // (Even if reparented, Frame will not relinquish its own journal to start using its new
            // parent's one.) But in order to be able to maintain some stronger invariants, this transition
            // is blocked here.
            if (prevValue == JournalOwnership.OwnsJournal && (JournalOwnership)newValue == JournalOwnership.Automatic)
            {
                return JournalOwnership.OwnsJournal;
            }
            return newValue;
        }

        /// <summary>
        /// Journal ownership setting for this frame
        /// </summary>
        public JournalOwnership JournalOwnership
        {
            get
            {
                Debug.Assert(_journalOwnership == (JournalOwnership)GetValue(JournalOwnershipProperty));
                return _journalOwnership;
            }
            set
            {
                if (value != _journalOwnership)
                {
                    SetValue(JournalOwnershipProperty, value);
                }
            }
        }

        /// <summary>
        /// Frame's associated NavigationService.
        /// </summary>
        public NavigationService NavigationService
        {
            get
            {
                VerifyAccess();
                return _navigationService;
            }
        }

        #endregion

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
            return new FrameAutomationPeer(this);
        }

        /// <summary>
        ///  Add an object child to this control
        /// </summary>
        protected override void AddChild(object value)
        {
            throw new InvalidOperationException(SR.Get(SRID.FrameNoAddChild));
        }

        /// <summary>
        ///  Add a text string to this control
        /// </summary>
        protected override void AddText(string text)
        {
            XamlSerializerUtil.ThrowIfNonWhiteSpaceInAddText(text, this);
        }
        #endregion

        #region Event Handlers

        private void _OnBPReady(Object o, BPReadyEventArgs e)
        {
            // update content property
            SetCurrentValueInternal(ContentProperty, e.Content);

            // Need to refresh everytime the child changed
            InvalidateMeasure();

            // We post a dispatcher work item to fire ContentRendered
            // only if this is Loaded in the tree.  If not, we will
            // post it from the LoadedHandler.  This guarantees that
            // we don't fire ContentRendered on a subtree that is not
            // connected to a PresentationSource
            if (IsLoaded == true)
            {
                PostContentRendered();
            }
            else
            {
                // _postContentRenderedFromLoadedHandler == true means
                // that we deferred to the Loaded event to PostConetentRendered
                // for the previous content change and Loaded has not fired yet.
                // Thus we don't want to hook up another event handler
                if (_postContentRenderedFromLoadedHandler == false)
                {
                    this.Loaded += new RoutedEventHandler(LoadedHandler);
                    _postContentRenderedFromLoadedHandler = true;
                }
            }
        }

        private void LoadedHandler(object sender, RoutedEventArgs args)
        {
            if (_postContentRenderedFromLoadedHandler == true)
            {
                PostContentRendered();
                _postContentRenderedFromLoadedHandler = false;
                this.Loaded -= new RoutedEventHandler(LoadedHandler);
            }
        }

        /// <remarks> Keep this method in sync with Window.PostContentRendered(). </remarks>
        private void PostContentRendered()
        {
            // Post the firing of ContentRendered as Input priority work item so
            // that ContentRendered will be fired after render query empties.
            if (_contentRenderedCallback != null)
            {
                // Content was changed again before the previous rendering completed (or at least
                // before the Dispatcher got to Input priority callbacks).
                _contentRenderedCallback.Abort();
            }
            _contentRenderedCallback = Dispatcher.BeginInvoke(DispatcherPriority.Input,
                                   (DispatcherOperationCallback) delegate (object unused)
                                   {
                                       _contentRenderedCallback = null;
                                       OnContentRendered(EventArgs.Empty);
                                       return null;
                                   },
                                   this);
        }

        private void OnQueryGoBack(object sender, CanExecuteRoutedEventArgs e)
        {
            Debug.Assert(sender == this && _ownJournalScope != null);
            e.CanExecute = _ownJournalScope.CanGoBack;
            e.Handled = true;
        }
        private void OnGoBack(object sender, ExecutedRoutedEventArgs e)
        {
            Debug.Assert(sender == this && _ownJournalScope != null);
            _ownJournalScope.GoBack();
            e.Handled = true;
        }

        private void OnQueryGoForward(object sender, CanExecuteRoutedEventArgs e)
        {
            Debug.Assert(sender == this && _ownJournalScope != null);
            e.CanExecute = _ownJournalScope.CanGoForward;
            e.Handled = true;
        }
        private void OnGoForward(object sender, ExecutedRoutedEventArgs e)
        {
            Debug.Assert(sender == this && _ownJournalScope != null);
            _ownJournalScope.GoForward();
            e.Handled = true;
        }

        private void OnNavigateJournal(object sender, ExecutedRoutedEventArgs e)
        {
            Debug.Assert(sender == this && _ownJournalScope != null);

            // The following checks are needed because anyone could send the NavigateJournal command.
            FrameworkElement journalEntryUIElem = e.Parameter as FrameworkElement;
            if (journalEntryUIElem != null)
            {
                JournalEntry je = journalEntryUIElem.DataContext as JournalEntry;
                if (je != null)
                {
                    if (_ownJournalScope.NavigateToEntry(je))
                    {
                        e.Handled = true;
                    }
                }
            }
        }

        private void OnQueryRefresh(object sender, CanExecuteRoutedEventArgs e)
        {
            Debug.Assert(sender == this && _ownJournalScope != null);
            e.CanExecute = Content != null;
        }
        private void OnRefresh(object sender, ExecutedRoutedEventArgs e)
        {
            Debug.Assert(sender == this && _ownJournalScope != null);
            _navigationService.Refresh();
            e.Handled = true;
        }

        private void OnBrowseStop(object sender, ExecutedRoutedEventArgs e)
        {
            Debug.Assert(sender == this && _ownJournalScope != null);
            _ownJournalScope.StopLoading();
            e.Handled = true;
        }

        #endregion

        #region Event Management

        /// <summary>
        ///     When a Frame-content's event bubbling up Visual tree, we need to adjust
        /// the source of the event to the frame itself.
        /// </summary>
        /// <param name="e">
        ///     Routed Event Args
        /// </param>
        /// <returns>
        ///     Returns new source. (Current Frame object)
        /// </returns>
        internal override object AdjustEventSource(RoutedEventArgs e)
        {
            e.Source=this;
            return this;
        }

        #endregion

        #region Overiding ContentControl implementation
        /// <summary>
        ///    Return text representing this Frame
        /// </summary>
        internal override string GetPlainText()
        {
            if (this.Source != null)
            {
                return Source.ToString();
            }
            else
            {
                return String.Empty;
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
            Invariant.Assert(_navigationService != null, "_navigationService should never be null here");
            return ( !_navigationService.CanReloadFromUri && Content != null);
        }

        #endregion Overding ContentControl implementation


        #region NavigationService support

        private static void OnParentNavigationServiceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Debug.Assert(d as Frame != null && ((Frame)d).NavigationService != null);
            ((Frame) d).NavigationService.OnParentNavigationServiceChanged();
        }

        /// <summary>
        /// Called when the template's visual tree is created.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Get the root element of the style
            Visual v = TemplateChild;
            if (v != null)
            {
                NavigationService.VisualTreeAvailable(v);
            }
        }

        #region INavigatorImpl members

        // Note: OnSourceUpdatedFromNavService is next to the other Source-related members of Frame.

        Visual INavigatorImpl.FindRootViewer()
        {
            return NavigationHelper.FindRootViewer(this, "PART_FrameCP");
        }

        #endregion INavigatorImpl

        #endregion NavigationService support

        #region INavigator implementation

        JournalNavigationScope INavigator.GetJournal(bool create)
        {
            return GetJournal(create);
        }

        /// <summary>
        /// <see cref="INavigator.GetJournal"/>
        /// </summary>
        private JournalNavigationScope GetJournal(bool create)
        {
            Invariant.Assert(_ownJournalScope != null ^ _journalOwnership != JournalOwnership.OwnsJournal);

            if (_ownJournalScope != null)
                return _ownJournalScope;

            JournalNavigationScope jns = GetParentJournal(create);
            if (jns != null)
            {
                SetCurrentValueInternal(JournalOwnershipProperty, JournalOwnership.UsesParentJournal);
                return jns;
            }

            if (create && _journalOwnership == JournalOwnership.Automatic)
            {
                SetCurrentValueInternal(JournalOwnershipProperty, JournalOwnership.OwnsJournal);
            }
            return _ownJournalScope;
        }

        /// <summary>
        /// True when the frame has its own journal and there is a navigable entry in the forward stack.
        /// If the frame doesn't own a journal, its parent or corresponding NavigationService may be
        /// able to navigate it forward.
        /// </summary>
        public bool CanGoForward
        {
            get
            {
                bool canGoFwd = _ownJournalScope != null && _ownJournalScope.CanGoForward;
                Debug.Assert(canGoFwd == (bool)GetValue(CanGoForwardProperty));
                return canGoFwd;
            }
        }

        /// <summary>
        /// True when the frame has its own journal and there is a navigable entry in the back stack.
        /// If the frame doesn't own a journal, its parent or corresponding NavigationService may be
        /// able to navigate it back.
        /// </summary>
        public bool CanGoBack
        {
            get
            {
                bool canGoBack = _ownJournalScope != null && _ownJournalScope.CanGoBack;
                Debug.Assert(canGoBack == (bool)GetValue(CanGoBackProperty));
                return canGoBack;
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
            VerifyAccess();
            _navigationService.AddBackEntry(state);
        }

        /// <summary>
        /// Removes the first JournalEntry from the frame's back stack.
        /// </summary>
        /// <exception cref="InvalidOperationException"> The frame doesn't own a journal. </exception>
        public JournalEntry RemoveBackEntry()
        {
            if (_ownJournalScope == null)
                throw new InvalidOperationException(SR.Get(SRID.InvalidOperation_NoJournal));
            return _ownJournalScope.RemoveBackEntry();
        }

        /// <summary>
        /// Navigates to the Uri and downloads the content. Whether the navigation is
        /// performed synchronously or asynchronously depends on the current default navigation behavior.
        /// </summary>
        /// <param name="source">URI of the application or content being navigated to.</param>
        /// <returns>bool indicating whether the navigation was successfully started or not</returns>
        public bool Navigate(Uri source)
        {
            VerifyAccess();
            return _navigationService.Navigate(source);
        }

        //
        //  INavigator.Navigate
        //
        /// <summary>
        /// This method navigates this Frame to the given Uri.
        /// </summary>
        /// <param name="source">The URI to be navigated to.</param>
        /// <param name="extraData">enables the develeoper to supply an extra object, that will be returned in the NavigationEventArgs of the Navigated event. The extra data enables the developer
        /// to identify the source of the navigation, in the presence of
        /// multiple navigations.
        /// </param>
        /// <returns>bool indicating whether the navigation was successfully started or not</returns>
        public bool Navigate(Uri source, Object extraData)
        {
            VerifyAccess();
            return _navigationService.Navigate(source, extraData);
        }

        /// <summary>
        /// Navigates synchronously to an existing element tree.
        /// </summary>
        /// <param name="content">Root of the element tree being navigated to.</param>
        /// <returns>bool indicating whether the navigation was successfully started or not</returns>
        public bool Navigate(Object content)
        {
            VerifyAccess();
            return _navigationService.Navigate(content);
        }

        //
        //  INavigator.Navigate(object)
        //
        /// <summary>
        /// This method synchronously navigates this Frame to the
        /// given Element.
        /// </summary>
        /// <param name="content">The Element to be navigated to.</param>
        /// <param name="extraData">enables the develeoper to supply an extra object, that will be returned in the NavigationEventArgs of the Navigated event. The extra data enables the developer
        /// to identify the source of the navigation, in the presence of
        /// multiple navigations.
        /// </param>
        /// <returns>bool indicating whether the navigation was successfully started or not</returns>
        public bool Navigate(Object content, Object extraData)
        {
            VerifyAccess();
            return _navigationService.Navigate(content, extraData);
        }

        /// <summary>
        /// Navigates the frame to the next journal entry. The operation is available only when
        /// the frame has its own journal. If not, try navigating the parent or the corresponding
        /// NavigationService.
        /// </summary>
        /// <exception cref="InvalidOperationException"> The frame doesn't own a journal. </exception>
        /// <exception cref="InvalidOperationException"> There is no forward journal entry to go to. </exception>
        public void GoForward()
        {
            if (_ownJournalScope == null)
                throw new InvalidOperationException(SR.Get(SRID.InvalidOperation_NoJournal));
            _ownJournalScope.GoForward();
        }

        /// <summary>
        /// Navigates the frame to the previous journal entry. The operation is available only when
        /// the frame has its own journal. If not, try navigating the parent or the corresponding
        /// NavigationService.
        /// </summary>
        /// <exception cref="InvalidOperationException"> The frame doesn't own a journal. </exception>
        /// <exception cref="InvalidOperationException"> There is no back journal entry to go to. </exception>
        public void GoBack()
        {
            if(_ownJournalScope == null)
                throw new InvalidOperationException(SR.Get(SRID.InvalidOperation_NoJournal));
            _ownJournalScope.GoBack();
        }

        /// <summary>
        /// StopLoading aborts asynchronous navigations that haven't been processed yet or that are
        /// still being downloaded. SopLoading does not abort parsing of the downloaded streams.
        /// The NavigationStopped event is fired only if the navigation was aborted.
        /// </summary>
        public void StopLoading()
        {
            VerifyAccess();
            _navigationService.StopLoading();
        }

        /// <summary>
        /// Reloads the current content.
        /// </summary>
        public void Refresh()
        {
            VerifyAccess();
            _navigationService.Refresh();
        }

        /// <summary>
        /// Uri for the current page. Getting this property always
        /// returns the URI of the content thats currently displayed.
        /// regardless of whether a navigation is in progress or not.
        /// </summary>
        /// <value></value>
        public Uri CurrentSource
        {
            get
            {
                return _navigationService.CurrentSource;
            }
        }

        /// <summary>
        /// List of back journal entries. Available only when the frame has its own journal.
        /// </summary>
        public IEnumerable BackStack
        {
            get
            {
                IEnumerable backStack = _ownJournalScope == null ? null : _ownJournalScope.BackStack;
                Debug.Assert(backStack == GetValue(BackStackProperty));
                return backStack;
            }
        }
        /// <summary>
        /// List of forward journal entries. Available only when the frame has its own journal.
        /// </summary>
        public IEnumerable ForwardStack
        {
            get
            {
                IEnumerable fwdStack = _ownJournalScope == null ? null : _ownJournalScope.ForwardStack;
                Debug.Assert(fwdStack == GetValue(ForwardStackProperty));
                return fwdStack;
            }
        }

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
                _navigationService.Navigating += value;
            }
            remove
            {
                _navigationService.Navigating -= value;
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
                _navigationService.NavigationProgress += value;
            }
            remove
            {
                _navigationService.NavigationProgress -= value;
            }
        }

        /// <summary>
        /// Raised an error is encountered during a navigation.
        /// The NavigationFailedEventArgs contains
        /// the exception that was thrown. By default Handled property is set to false,
        /// which allows the exception to be rethrown.
        /// The event handler can prevent exception from throwing
        /// to the user by setting the Handled property to true
        /// </summary>
        public event NavigationFailedEventHandler NavigationFailed
        {
            add
            {
                _navigationService.NavigationFailed += value;
            }
            remove
            {
                _navigationService.NavigationFailed -= value;
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
                _navigationService.Navigated += value;
            }
            remove
            {
                _navigationService.Navigated -= value;
            }
        }

        //
        //  INavigator.LoadCompleted
        //
        /// <summary>
        /// Raised after the entire page, including all images and frames, has been downloaded
        /// and parsed. This is the event to handle to stop spinning the globe. The developer
        /// should check the IsNavigationInitiator property on the NavigationEventArgs to determine
        /// whether to stop spinning the globe.
        /// The NavigationEventArgs contain the uri or root element of the content being navigated to,
        /// and a IsNavigationInitiator property that indicates whether this is a new navigation
        /// initiated by this Frame, or whether this navigation is being propagated down
        /// from a higher level navigation taking place in a containing window or frame.
        /// This event is informational only, and cannot be canceled.
        /// </summary>
        public event LoadCompletedEventHandler LoadCompleted
        {
            add
            {
                _navigationService.LoadCompleted += value;
            }
            remove
            {
                _navigationService.LoadCompleted -= value;
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
                _navigationService.NavigationStopped += value;
            }
            remove
            {
                _navigationService.NavigationStopped -= value;
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
                _navigationService.FragmentNavigation += value;
            }
            remove
            {
                _navigationService.FragmentNavigation -= value;
            }
        }

        #endregion INavigator implementation

        #region IJournalNavigationScopeHost Members

        void IJournalNavigationScopeHost.VerifyContextAndObjectState()
        {
            VerifyAccess();
        }

        void IJournalNavigationScopeHost.OnJournalAvailable()
        {
            // BackStackProperty & ForwardStackProperty should be set by JournalNavigationScope.
            Debug.Assert(GetValue(BackStackProperty) == _ownJournalScope.BackStack);
        }

        bool IJournalNavigationScopeHost.GoBackOverride()
        {
            return false; // not overriding here
        }
        bool IJournalNavigationScopeHost.GoForwardOverride()
        {
            return false;
        }

        #endregion

        #region IJournalState Members

        /// <summary>
        /// When Frame's parent container navigates away and if its content is not keep-alive,
        /// the frame's current page needs to be remembered. In addition, if the frame has its own
        /// journal, it has to be preserved too. Class FramePersistState captures this "metajournaling"
        /// state. It will become part of the journal entry created for the navigation in the parent
        /// container (stored within a DataStreams instance).
        /// </summary>
        [Serializable]
        private class FramePersistState : CustomJournalStateInternal
        {
            internal JournalEntry JournalEntry;
            internal Guid NavSvcGuid;
            internal JournalOwnership JournalOwnership;
            internal Journal Journal;

            internal override void PrepareForSerialization()
            {
                if (JournalEntry != null)
                {
                    if (JournalEntry.IsAlive()) // not serializable
                    {
                        JournalEntry = null;
                        // Only the NavigationService GUID will be restored.
                        // See related case and explanation in Frame.GetJournalState().
                    }
                    else
                    {
                        Debug.Assert(JournalEntry.GetType().IsSerializable);
                    }
                }
                if (Journal != null)
                {
                    Journal.PruneKeepAliveEntries();
                }
            }
        };

        CustomJournalStateInternal IJournalState.GetJournalState(JournalReason journalReason)
        {
            if (journalReason != JournalReason.NewContentNavigation)
            {
                return null;
            }

            FramePersistState state = new FramePersistState();

            // Save a JournalEntry for the current content.
            state.JournalEntry = _navigationService.MakeJournalEntry(JournalReason.NewContentNavigation);
            // The current Content may be null or may not want to be journaled (=> JournalEntry=null).
            // But we still need to save and then restore the NS GUID - there may be other JEs keyed
            // by this GUID value.
            // i. There is a somewhat similar case in ApplicationProxyInternal._GetSaveHistoryBytesDelegate().
            state.NavSvcGuid = _navigationService.GuidId;

            state.JournalOwnership = _journalOwnership;
            if (_ownJournalScope != null)
            {
                Debug.Assert(_journalOwnership == JournalOwnership.OwnsJournal);
                // No need to make a copy here because this Frame object will be discarded.
                // (Supposedly the parent container is navigating away.)
                state.Journal = _ownJournalScope.Journal;
            }

            return state;
        }

        void IJournalState.RestoreJournalState(CustomJournalStateInternal cjs)
        {
            FramePersistState state = (FramePersistState)cjs;

            _navigationService.GuidId = state.NavSvcGuid;

            // Because the JournalOwnershipProperty doesn't have the FrameworkPropertyMetadataOptions.Journal
            // flag, the parser will always set the value specified in markup, which may be different from
            // state.JournalOwnership. So, at this point JournalOwnership is not necessarily Automatic
            // (the default).
            JournalOwnership = state.JournalOwnership;
            if(_journalOwnership == JournalOwnership.OwnsJournal)
            {
                Invariant.Assert(state.Journal != null);
                _ownJournalScope.Journal = state.Journal;
            }

            if(state.JournalEntry != null)
            {
                state.JournalEntry.Navigate(this, NavigationMode.Back);
            }
        }
        #endregion IJournalState

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        #region Internal Methods

        internal override void OnPreApplyTemplate()
        {
            base.OnPreApplyTemplate();

            if (_ownJournalScope != null)
            {
                // This causes the Journal instance to be created. BackStackProperty and ForwardStackProperty
                // should be set before the navigation chrome data-binds to them but after any Journal is
                // restored from FramePersistState.
                _ownJournalScope.EnsureJournal();
            }
        }

        // Invalidate resources on the frame content if the content isn't
        // reachable via the visual/logical tree
        internal override void OnThemeChanged()
        {
            // If the frame does not have a template generated tree then its
            // content is not reachable via a tree walk.
            DependencyObject d;
            if (!HasTemplateGeneratedSubTree && (d = Content as DependencyObject) != null)
            {
                FrameworkElement fe;
                FrameworkContentElement fce;
                Helper.DowncastToFEorFCE(d, out fe, out fce, false);

                if (fe != null || fce != null)
                {
                    TreeWalkHelper.InvalidateOnResourcesChange(fe, fce, ResourcesChangeInfo.ThemeChangeInfo);
                }
            }
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        #region Private Methods

        private JournalNavigationScope GetParentJournal(bool create)
        {
            JournalNavigationScope jns = null;
            NavigationService parentNS = _navigationService.ParentNavigationService;
            if (parentNS != null)
            {
                jns = parentNS.INavigatorHost.GetJournal(create);
            }
            return jns;
        }

        /// <remarks> Does not update the JournalOwnershipProperty. </remarks>
        private void SwitchToOwnJournal()
        {
            Debug.Assert(_ownJournalScope == null ^ _journalOwnership == JournalOwnership.OwnsJournal);
            if (_ownJournalScope == null)
            {
                // Entries created for this frame in the parent's journal have to be removed.
                JournalNavigationScope parentJns = GetParentJournal(false/*don't create*/);
                if (parentJns != null)
                {
                    parentJns.Journal.RemoveEntries(_navigationService.GuidId);
                }

                _ownJournalScope = new JournalNavigationScope(this);
                _navigationService.InvalidateJournalNavigationScope();

                // BackStackProperty & ForwardStackProperty should become available immediately if
                // OwnsJournal is set *after* Frame is already loaded.
                // See comment in OnPreApplyTemplate().
                if (IsLoaded)
                {
                    _ownJournalScope.EnsureJournal();
                }

                AddCommandBinding(new CommandBinding(NavigationCommands.BrowseBack, OnGoBack, OnQueryGoBack));
                AddCommandBinding(new CommandBinding(NavigationCommands.BrowseForward, OnGoForward, OnQueryGoForward));
                AddCommandBinding(new CommandBinding(NavigationCommands.NavigateJournal, OnNavigateJournal));
                AddCommandBinding(new CommandBinding(NavigationCommands.Refresh, OnRefresh, OnQueryRefresh));
                AddCommandBinding(new CommandBinding(NavigationCommands.BrowseStop, OnBrowseStop));
            }
            _journalOwnership = JournalOwnership.OwnsJournal;
        }

        /// <remarks> Does not update the JournalOwnershipProperty. </remarks>
        private void SwitchToParentJournal()
        {
            Debug.Assert(_ownJournalScope == null ^ _journalOwnership == JournalOwnership.OwnsJournal);
            if (_ownJournalScope != null)
            {
                _ownJournalScope = null;
                _navigationService.InvalidateJournalNavigationScope();

                JournalNavigationScope.ClearDPValues(this);

                foreach (CommandBinding cb in _commandBindings)
                {
                    base.CommandBindings.Remove(cb);
                }
                _commandBindings = null;
            }
            _journalOwnership = JournalOwnership.UsesParentJournal;
        }

        private void AddCommandBinding(CommandBinding b)
        {
            base.CommandBindings.Add(b);

            // Store the CommandBinding reference so that it can be removed in case the frame loses
            // its JournalNavigationScope.
            if (_commandBindings == null)
            {
                _commandBindings = new List<CommandBinding>(6);
            }
            _commandBindings.Add(b);
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        #region Private Fields

        private bool                _postContentRenderedFromLoadedHandler = false;
        private DispatcherOperation _contentRenderedCallback;
        private NavigationService    _navigationService;
        private bool               _sourceUpdatedFromNavService;

        /// <remarks> All changes should be made via the JournalOwnership property setter. </remarks>
        private JournalOwnership _journalOwnership = JournalOwnership.Automatic;
        private JournalNavigationScope  _ownJournalScope;
        private List<CommandBinding>    _commandBindings;

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
}
