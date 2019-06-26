// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
//
// File: RootBrowserWindow.cs
//
// Description: This class will implement the hosting and bridging logic
//              between the browser and Avalon.  This will be the top
//              level "frame" that hosts all content in IE.  This class
//              will not be public.
//
//
//
//
//
//---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Navigation;
using MS.Internal.Commands;
using MS.Internal.Controls;
using MS.Win32;

//In order to avoid generating warnings about unknown message numbers and
//unknown pragmas when compiling your C# source code with the actual C# compiler,
//you need to disable warnings 1634 and 1691. (Presharp Documentation)
#pragma warning disable 1634, 1691


namespace MS.Internal.AppModel
{

    /// <summary>
    ///
    /// </summary>
    internal sealed class RootBrowserWindow : NavigationWindow, IWindowService, IJournalNavigationScopeHost
    {
        //----------------------------------------------
        //
        // Constructors
        //
        //----------------------------------------------
        #region Constructors


        static RootBrowserWindow()
        {
            CommandHelpers.RegisterCommandHandler(typeof(RootBrowserWindow), ApplicationCommands.Print,
                    new ExecutedRoutedEventHandler(OnCommandPrint), new CanExecuteRoutedEventHandler(OnQueryEnabledCommandPrint));
        }

        /// <summary>
        ///
        /// </summary>
        private RootBrowserWindow():base(true)
        {
            // Allow tabbing out to the browser - see KeyInputSite and OnKeyDown().

            // IE 6 doesn't provide the necessary support; notice IsDownlevelPlatform covers Firefox too,
            // hence the additional IsHostedInIE check.
            // By checking for the negative top-level case, we allow to tab out of XBAPs hosted in an iframe;
            // this support is enabled regardless of the browser we're on (to avoid browser-specifics here).
            bool isIE6 = IsDownlevelPlatform && BrowserInteropHelper.IsHostedInIEorWebOC;
            if (!(isIE6 && BrowserInteropHelper.IsAvalonTopLevel))
            {
                SetValue(KeyboardNavigation.TabNavigationProperty, KeyboardNavigationMode.Continue);
                SetValue(KeyboardNavigation.ControlTabNavigationProperty, KeyboardNavigationMode.Continue);
            }
        }
        #endregion Constructors

        //----------------------------------------------
        //
        // Protected Methods
        //
        //----------------------------------------------
        #region Protected Methods

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new RootBrowserWindowAutomationPeer(this);
        }

        protected override void OnInitialized(EventArgs args)
        {
            AddHandler(Hyperlink.RequestSetStatusBarEvent, new RoutedEventHandler(OnRequestSetStatusBar_Hyperlink));
            base.OnInitialized(args);
        }

        protected override Size MeasureOverride(Size constraint)
        {
            return base.MeasureOverride(GetSizeInLogicalUnits());
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            // Get the size of the avalon window and pass it to
            // the base implementation. The return value tells the Size
            // that we are occupying.  Since, we are RBW we will occupy the
            // entire available size and thus we don't care what our child wants.
            base.ArrangeOverride(GetSizeInLogicalUnits());
            return arrangeBounds;
        }

        protected override void OnStateChanged(EventArgs e)
        {
        }

        protected override void OnLocationChanged(EventArgs e)
        {
        }

        protected override void OnClosing(CancelEventArgs e)
        {
        }

        protected override void OnClosed(EventArgs e)
        {
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            // Posting READYSTATE_COMPLETE triggers the WebOC's DocumentComplete event.
            // Media Center, in particular, uses this to make the WebOC it hosts visible.
            if (!_loadingCompletePosted)
            {
                Browser.PostReadyStateChange(READYSTATE_COMPLETE);
                _loadingCompletePosted = true;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            // In browser apps, Ctrl+Tab switches tabs. F6 simulates it here.
            if (e.Key == Key.F6 && (e.KeyboardDevice.Modifiers & ~ModifierKeys.Shift) == 0)
            {
                if (KeyboardNavigation.Navigate(
                        e.KeyboardDevice.FocusedElement as DependencyObject ?? this,
                        Key.Tab, e.KeyboardDevice.Modifiers | ModifierKeys.Control))
                {
                    e.Handled = true;
                }
            }

            if (!e.Handled)
            {
                base.OnKeyDown(e);
            }
        }

        #endregion Protected methods

        //----------------------------------------------
        //
        // Internal Methods
        //
        //----------------------------------------------
        #region Internal Methods

        /// <summary>
        ///     Creates the RBW object and sets the Style property on it
        ///     to the correct value
        /// </summary>
        internal static RootBrowserWindow CreateAndInitialize()
        {
            RootBrowserWindow rbw = new RootBrowserWindow();
            rbw.InitializeRBWStyle();
            return rbw;
        }

        internal override void CreateAllStyle()
        {
            Invariant.Assert(App != null, "RootBrowserWindow must be created in an Application");

            IHostService ihs = (IHostService)App.GetService(typeof(IHostService));

            Invariant.Assert(ihs!=null, "IHostService in RootBrowserWindow cannot be null");
            Invariant.Assert(ihs.HostWindowHandle != IntPtr.Zero, "IHostService.HostWindowHandle in RootBrowserWindow cannot be null");

            //This sets the _ownerHandle correctly and will be used to create the _sourceWindow
            //with the correct parent
            this.OwnerHandle = ihs.HostWindowHandle;
            this.Win32Style = NativeMethods.WS_CHILD | NativeMethods.WS_CLIPCHILDREN | NativeMethods.WS_CLIPSIBLINGS;

        }

        internal override HwndSourceParameters CreateHwndSourceParameters()
        {
            HwndSourceParameters parameters = base.CreateHwndSourceParameters();
            parameters.TreatAsInputRoot = true;
            parameters.TreatAncestorsAsNonClientArea = true;
            return parameters;
        }

        /// <summary>
        ///     Override for SourceWindow creation.
        ///     Virtual only so that we may assert.
        /// </summary>
        internal override void CreateSourceWindowDuringShow()
        {
            Browser.OnBeforeShowNavigationWindow();

            base.CreateSourceWindowDuringShow();
            Invariant.Assert(IsSourceWindowNull == false, "Failed to create HwndSourceWindow for browser hosting");

            // _sourceWindowCreationCompleted specifies that HwndSource creation has completed.  This is used
            // to minimize the SetWindowPos calls from ResizeMove when Height/Width is set prior to calling
            // show on RBW (this also occurs when Height/Width is set in the style of RBW)
            _sourceWindowCreationCompleted = true;

            if (_rectSet)
            {
                _rectSet = false;
                ResizeMove(_xDeviceUnits, _yDeviceUnits, _widthDeviceUnits, _heightDeviceUnits);
            }
            // The above window move (resize) operation causes message dispatching, which allows reentrancy
            // from the browser, which can initiate premature shutdown.
            if (IsSourceWindowNull)
                return;

            SetUpInputHooks();

            //
            // While the RBW is created and shown, the Top Browser window should have already been created and shown,
            // Browser doesn't notify this RBW of the activation status again unless user activates/deactivates the main
            // window. It is time to transfer the Top Browser Window's activation status to this RBW so that user's
            // code can get correct status through property IsActivate.
            //
            IntPtr topWindow = UnsafeNativeMethods.GetAncestor(new HandleRef(this, CriticalHandle), 2/*GA_ROOT*/);
            Debug.Assert(topWindow != IntPtr.Zero);
            IntPtr activeWindow = UnsafeNativeMethods.GetForegroundWindow();
            HandleActivate(activeWindow == topWindow);
        }

        // No need to clear App.MainWindow for RBW case.  It throws an exception if attempted
        internal override void TryClearingMainWindow()
        {
        }

        internal override void CorrectStyleForBorderlessWindowCase()
        {
        }

        internal override void GetRequestedDimensions(ref double requestedLeft, ref double requestedTop, ref double requestedWidth, ref double requestedHeight)
        {
            requestedTop = 0;
            requestedLeft = 0;
            requestedWidth = this.Width;
            requestedHeight = this.Height;
        }

        internal override void SetupInitialState(double requestedTop, double requestedLeft, double requestedWidth, double requestedHeight)
        {
            // If RBW Height/Width was set before calling show in RBW, we need
            // to update the browser with that size now
            SetBrowserSize();
            SetRootVisual();
        }

        internal override int nCmdForShow()
        {
            return NativeMethods.SW_SHOW;
        }

        internal override bool HandleWmNcHitTestMsg(IntPtr lParam, ref IntPtr refInt)
        {
            return false;
        }

        internal override WindowMinMax GetWindowMinMax()
        {
            return new WindowMinMax(0, double.PositiveInfinity);
        }

        // RBW overrides WmMoveChangedHelper default behavior where it calls
        // either SetValue/CoerceValue on Top/Left DPs since that will
        // throw an exception for RBW.  Furthermore, in RBW, we don't want
        // to fire LocationChanged event.
        internal override void WmMoveChangedHelper()
        {
        }

        /// <summary>
        ///     Resizes and moves the RBW (which is a WS_CHILD window).  This is called by
        ///     AppProxyInternal when the host callsbacks into it with the new size/location.
        ///     We need this internal since Height/Width/Top/Left on RBW govern the host'ss
        ///     properties.
        /// </summary>
        /// <remarks>
        ///     The location of WS_CHILD window is relative to it parent window thus when the
        ///     browser moves it does not call this method since the relative position of this window
        ///     wrt the browser hasn't changed.
        /// </remarks>
        /// <param name="xDeviceUnits">New left of the RBW</param>
        /// <param name="yDeviceUnits">New top of the RBW</param>
        /// <param name="widthDeviceUnits">New width of the RBW</param>
        /// <param name="heightDeviceUnits">New height of the RBW</param>
        internal void ResizeMove(int xDeviceUnits, int yDeviceUnits, int widthDeviceUnits, int heightDeviceUnits)
        {
            // _sourceWindowCreationCompleted specifies that HwndSource creation has completed.  This is used
            // to minimize the SetWindowPos calls from ResizeMove when Height/Width is set prior to calling
            // show on RBW (this also occurs when Height/Width is set in the style of RBW).  Thus, we want to
            // resize the underlying avalon hwnd only after its creation is completed
            if (_sourceWindowCreationCompleted == false)
            {
                _xDeviceUnits = xDeviceUnits;
                _yDeviceUnits = yDeviceUnits;
                _widthDeviceUnits = widthDeviceUnits;
                _heightDeviceUnits = heightDeviceUnits;

                _rectSet = true;

                return;
            }

            Invariant.Assert(IsSourceWindowNull == false, "sourceWindow cannot be null if _sourceWindowCreationCompleted is true");

            HandleRef handleRef;
            handleRef = new HandleRef( this, CriticalHandle ) ;

            UnsafeNativeMethods.SetWindowPos( handleRef ,
                           NativeMethods.NullHandleRef,
                           xDeviceUnits,
                           yDeviceUnits,
                           widthDeviceUnits,
                           heightDeviceUnits,
                           NativeMethods.SWP_NOZORDER
                           | NativeMethods.SWP_NOACTIVATE
                           | NativeMethods.SWP_SHOWWINDOW);
        }

        /// <summary>
        ///     This is called when the Title dependency property changes in the Window.
        /// </summary>
        internal override void UpdateTitle(string titleStr)
        {
            IBrowserCallbackServices ibcs = Browser;
            if (ibcs != null) // null on shutdown
            {
                string title = PruneTitleString(titleStr);

                // It's not the end of the world if this fails.
                // VS's browser in particular returns RPC_E_CALL_REJECTED.
                // Not sure of other exceptions that might be thrown here,
                // so keeping this scoped to what's been reported.
                const int RPC_E_CALL_REJECTED = unchecked((int)0x80010001);
                try
                {
                    // SHOULD NOT CALL BASE; BASE IMPLEMENTS TEXT PROPERTY ON WINDOW
                    BrowserInteropHelper.HostBrowser.SetTitle(title);
                }
                catch (COMException e)
                {
                    if (e.ErrorCode != RPC_E_CALL_REJECTED)
                    {
                        throw;
                    }
                }
            }
        }

        /// <summary>
        ///     This is called by NavigationService to set the status bar content
        ///     for the browser
        /// </summary>
        /// <remarks>
        ///     We propagate object.ToString() to the browser's status bar.
        /// </remarks>
        internal void SetStatusBarText(string statusString)
        {
            if (BrowserInteropHelper.HostBrowser != null) // could be null if shutting down
            {
                BrowserInteropHelper.HostBrowser.SetStatusText(statusString);
            }
        }

        /// <summary>
        ///     Called to update Height of the browser.  Currently, this method is called from
        ///     two places:
        ///
        ///     1) OnHeightInvalidated from window.cs
        ///     2) SetBrowserSize in RBW
        /// </summary>
        internal override void UpdateHeight(double newHeightLogicalUnits)
        {
            Point sizeDeviceUnits = LogicalToDeviceUnits(new Point(0, newHeightLogicalUnits));
            uint heightDeviceUnits = (uint)Math.Round(sizeDeviceUnits.Y);

            if (BrowserInteropHelper.HostBrowser != null)
            {
                //
                // Note: right now IE is clipping browser height to desktop size.
                // However it should be clipped to available desktop size. See Windows OS Bug #1045038
                // The code below is to fix this for now.
                // Even if IE's code is changed - likely we keep this as defense in-depth.
                //

                uint maxHeightDeviceUnits = GetMaxWindowHeight();
                heightDeviceUnits = heightDeviceUnits > maxHeightDeviceUnits ? maxHeightDeviceUnits : heightDeviceUnits;
                heightDeviceUnits = heightDeviceUnits < MIN_BROWSER_HEIGHT_DEVICE_UNITS ? MIN_BROWSER_HEIGHT_DEVICE_UNITS : heightDeviceUnits;
                BrowserInteropHelper.HostBrowser.SetHeight(heightDeviceUnits);
            }
        }

        internal override void UpdateWidth(double newWidthLogicalUnits)
        {
            Point sizeDeviceUnits = LogicalToDeviceUnits(new Point(newWidthLogicalUnits, 0));
            uint widthDeviceUnits  = (uint)Math.Round(sizeDeviceUnits.X);

            if (BrowserInteropHelper.HostBrowser != null)
            {
                //
                // Note: right now IE is clipping browser width to desktop size.
                // However it should be clipped to available desktop size. See Windows OS Bug #1045038
                // The code below is to fix this for now.
                // Even if IE's code is changed - likely we keep this as defense in-depth.
                //
                uint maxWidthDeviceUnits = GetMaxWindowWidth();
                widthDeviceUnits = widthDeviceUnits > maxWidthDeviceUnits ? maxWidthDeviceUnits : widthDeviceUnits;

                widthDeviceUnits = widthDeviceUnits < MIN_BROWSER_WIDTH_DEVICE_UNITS ? MIN_BROWSER_WIDTH_DEVICE_UNITS : widthDeviceUnits;
                BrowserInteropHelper.HostBrowser.SetWidth(widthDeviceUnits);
            }
        }

        /// <summary>
        /// When being reloaded from history in the browser, we need to
        /// set up the journal again
        /// </summary>
        /// <param name="journal"></param>
        internal void SetJournalForBrowserInterop(Journal journal)
        {
            Invariant.Assert(journal != null, "Failed to get Journal for browser integration");
            base.JournalNavigationScope.Journal = journal;
        }

        void IJournalNavigationScopeHost.OnJournalAvailable()
        {
            base.Journal.BackForwardStateChange += new EventHandler(HandleBackForwardStateChange);
        }

        // For downlevel platforms, we don't have integration with the journal, so we have to
        // use our own Journal.
        bool IJournalNavigationScopeHost.GoBackOverride()
        {
            if (HasTravelLogIntegration)
            {
                if (BrowserInteropHelper.HostBrowser != null)
                {
                    try
                    {
                        BrowserInteropHelper.HostBrowser.GoBack();
                    }
#pragma warning disable 6502 // PRESharp - Catch statements should not have empty bodies
                    catch (OperationCanceledException)
                    {
                        // Catch the OperationCanceledException when the navigation is canceled.
                        // See comments in applicationproxyinternal._LoadHistoryStreamDelegate.
                    }
#pragma warning restore 6502
                }
                return true;
            }
            return false; // Proceed with internal GoBack.
        }

        // For downlevel platforms, we don't have integration with the journal, so we have to
        // use our own Journal.
        bool IJournalNavigationScopeHost.GoForwardOverride()
        {
            if (HasTravelLogIntegration)
            {
                if (BrowserInteropHelper.HostBrowser != null)
                {
                    try
                    {
                        BrowserInteropHelper.HostBrowser.GoForward();
                    }
#pragma warning disable 6502 // PRESharp - Catch statements should not have empty bodies
                    catch (OperationCanceledException)
                    {
                        // Catch the OperationCanceledException when the navigation is canceled.
                        // See comments in applicationproxyinternal._LoadHistoryStreamDelegate.
                    }
#pragma warning restore 6502
                }
                return true;
            }
            return false; // Proceed with internal GoForward.
        }

        internal override void VerifyApiSupported()
        {
            throw new InvalidOperationException(SR.Get(SRID.NotSupportedInBrowser));
        }

        internal override void ClearResizeGripControl(Control oldCtrl)
        {
            // don't do anything here since we do not support
            // ResizeGrip for RBW
        }

        internal override void SetResizeGripControl(Control ctrl)
        {
            // don't do anything here since we do not support
            // ResizeGrip for RBW
        }

        // This is called in weboc's static constructor.
        internal void AddLayoutUpdatedHandler()
        {
            LayoutUpdated += new EventHandler(OnLayoutUpdated);
        }

        internal void TabInto(bool forward)
        {
            TraversalRequest tr = new TraversalRequest(
                forward ? FocusNavigationDirection.First : FocusNavigationDirection.Last);
            MoveFocus(tr);
        }

        #endregion Internal Methods

        //----------------------------------------------
        //
        // Private Methods
        //
        //----------------------------------------------
        #region Private methods
        /// <summary>
        ///     Sets the correct style property on the RBW object based on the
        ///     browser version
        /// </summary>
        private void InitializeRBWStyle()
        {
            // If we are on a downlevel platform, then we don't integrate with the browser's
            // travellog, so we have to use our own Journal and supply our own navigation chrome.
            if (!HasTravelLogIntegration)
            {
                SetResourceReference(StyleProperty, SystemParameters.NavigationChromeDownLevelStyleKey);

                // if the Template property is not defined in a custom style, the property system gets
                // the Template property value for the NavigationWindow from the theme file.  Since,
                // we want to get the Template property value from the browser styles, we need to
                // set the DefaultStyleKeyProperty here.
                SetValue(DefaultStyleKeyProperty, SystemParameters.NavigationChromeDownLevelStyleKey);
            }
            else
            {
                SetResourceReference(StyleProperty, SystemParameters.NavigationChromeStyleKey);

                // if the Template property is not defined in a custom style, the property system gets
                // the Template property value for the NavigationWindow from the theme file.  Since,
                // we want to get the Template property value from the browser styles, we need to
                // set the DefaultStyleKeyProperty here.
                SetValue(DefaultStyleKeyProperty, SystemParameters.NavigationChromeStyleKey);
            }
        }

        private void SetUpInputHooks()
        {
            IKeyboardInputSink sink;

            _inputPostFilter = new HwndWrapperHook(BrowserInteropHelper.PostFilterInput);
            HwndSource hwndSource = base.HwndSourceWindow;
            hwndSource.HwndWrapper.AddHookLast(_inputPostFilter);

            sink = (IKeyboardInputSink)hwndSource;

            Debug.Assert(sink.KeyboardInputSite == null);
            sink.KeyboardInputSite = new KeyInputSite(new SecurityCriticalData<IKeyboardInputSink>(sink));
        }

        /// <summary>
        ///     Updates browser size if Height/Width is not set to default value (NaN).  This
        ///     means that Height/Width was set prior to calling Show on RBW and we need to
        ///     propagate that to the browser.  This method is called from SetupInitialize which
        ///     is called from CreateSourceWindowImpl
        /// </summary>
        private void SetBrowserSize()
        {
            Point requestedSizeDeviceUnits = LogicalToDeviceUnits(new Point(this.Width, this.Height));

            // if Width was specified
            if (!DoubleUtil.IsNaN(this.Width))
            {
                // at this stage, ActualWidth/Height is not set since
                // layout has not happened (it happens when we set the
                // RootVisual of the HwndSource)
                UpdateWidth(requestedSizeDeviceUnits.X);
            }

            // if Height was specified
            if (!DoubleUtil.IsNaN(this.Height))
            {
                // at this stage, ActualWidth/Height is not set since
                // layout has not happened (it happens when we set the
                // RootVisual of the HwndSource)
                UpdateHeight(requestedSizeDeviceUnits.Y);
            }
        }

        private string PruneTitleString(string rawString)
        {
            StringBuilder sb = new StringBuilder();
            bool inMiddleOfWord = false;

            for (int i=0; i < rawString.Length; i++)
            {
                if (Char.IsWhiteSpace(rawString[i]) == false)
                {
                    sb.Append(rawString[i]);
                    inMiddleOfWord = true;
                }
                else
                {
                    if (inMiddleOfWord == true)
                    {
                        sb.Append(' ');
                        inMiddleOfWord = false;
                    }
                }
            }

            // remove the last space if it exists
            return sb.ToString().TrimEnd(' ');
        }

        private void OnLayoutUpdated(object obj, EventArgs args)
        {
            try
            {
                VerifyWebOCOverlap(NavigationService);
            }
            finally
            {
                _webBrowserList.Clear();
            }
        }

        private void VerifyWebOCOverlap(NavigationService navigationService)
        {
            for (int i = 0; i < navigationService.ChildNavigationServices.Count; i++)
            {
                NavigationService childNavService = (NavigationService)(navigationService.ChildNavigationServices[i]);
                WebBrowser webBrowser = childNavService.WebBrowser;
                if (webBrowser != null)
                {
                    for (int j = 0; j < _webBrowserList.Count; j++)
                    {
                        // Note: We are using WebBrowser.BoundRect, which is relative to parent window.
                        // Since WebBrowsers are all siblings child windows right now, e.g., we don't allow WebOC inside
                        // Popup window, this is a better performed way. If we change that, we should make sure the rects
                        // to compare are relative to desktop.
                        Rect rect = Rect.Intersect(webBrowser.BoundRect, _webBrowserList[j].BoundRect);
                        // Only when the intersect rect's Width and Height are both bigger than 0, we consider them overlapping.
                        // Even when 2 edges are next to each other, it is considered as intersect by the Rect class.
                        if ((rect.Width > 0) && (rect.Height > 0))
                        {
                            throw new InvalidOperationException(SR.Get(SRID.WebBrowserOverlap));
                        }
                    }
                    _webBrowserList.Add(webBrowser);
                }
                else
                {
                    VerifyWebOCOverlap(childNavService);
                }
            }
        }

        /// <summary>
        /// We are not using the CanGoBack/CanGoForward property change since that is fired only
        /// if the value changes eg. if we had 3 entries in the backstack, then a back navigation
        /// won't fire this since the value is still 'true' and has not changed.
        /// What we need is the UpdateView() notification or the BackForwardState change
        /// notification which is fired from UpdateView() of the Journal.
        /// Trying to hook the event will create the journal even if there was no navigation
        /// so just using an virtual override to do the work.
        /// </summary>
        private void HandleBackForwardStateChange(object sender, EventArgs args)
        {
            //Nothing to do for downlevel platform
            if (!HasTravelLogIntegration)
                return;

            IBrowserCallbackServices ibcs = Browser;
            if (ibcs != null)
            {
                ibcs.UpdateBackForwardState();
            }
        }


        ///<summary>
        /// Given a proposed width - and curWidth - return the MaxWidth the window can be opened to.
        /// Used to prevent sizing of window > desktop bounds in browser.
        ///</summary>
        private uint GetMaxWindowWidth()
        {
            NativeMethods.RECT desktopArea = WorkAreaBoundsForNearestMonitor;
            int browserLeft = BrowserInteropHelper.HostBrowser.GetLeft();
            uint curBrowserWidth = BrowserInteropHelper.HostBrowser.GetWidth();

            uint availableWidth = (uint)(desktopArea.right - browserLeft);
            uint maxWidth = availableWidth > curBrowserWidth ? availableWidth : curBrowserWidth;
            return maxWidth;
        }

        ///<summary>
        /// Given a proposed height - and curHeight - return the MaxHeight the window can be opened to.
        /// Used to prevent sizing of window > desktop bounds in browser.
        ///</summary>
        private uint GetMaxWindowHeight()
        {
            NativeMethods.RECT desktopArea = WorkAreaBoundsForNearestMonitor;
            int browserTop = BrowserInteropHelper.HostBrowser.GetTop();
            uint curBrowserHeight = BrowserInteropHelper.HostBrowser.GetHeight();

            uint availableHeight = (uint)(desktopArea.bottom - browserTop);
            uint maxHeight = availableHeight > curBrowserHeight ? availableHeight : curBrowserHeight;
            return maxHeight;
        }


        ///<summary>
        ///For browser hosting cases, we get the rects through OLE activation before we create the
        ///RootBrowserWindow. Even if SourceWindow or Handle are null, we can return the cached rects
        ///</summary>
        private Size GetSizeInLogicalUnits()
        {
            Size size;

            // Adding check for IsCompositionTargetInvalid
            if (IsSourceWindowNull || IsCompositionTargetInvalid)
            {
                // return _widthDeviceUnits & _heightDeviceUnits if hwndsource is not yet available.
                // We will resize when hwnd becomes available, because the DeviceToLogicalUnits calculation
                // depends on hwnd being available. If it's not high dpi, the second resize will be optimized
                // by layout system.
                size = new Size(_widthDeviceUnits, _heightDeviceUnits);
            }
            else
            {
                // It's better to get WindowSize instead of doing WindowSize.Width & WindowSize.Height
                // because WindowSize queries HwndSource.
                size = WindowSize;
                Point ptLogicalUnits = DeviceToLogicalUnits(new Point(size.Width, size.Height));
                size = new Size(ptLogicalUnits.X, ptLogicalUnits.Y);
            }

            return size;
        }

        private void OnRequestSetStatusBar_Hyperlink(object sender, RoutedEventArgs e)
        {
            RequestSetStatusBarEventArgs statusEvent = e as RequestSetStatusBarEventArgs;

            if ( statusEvent != null )
            {
                SetStatusBarText(statusEvent.Text);
            }
        }

        ///<summary>
        /// Prints the content of the App's MainWindow.  The logic is that if the content is not visual but IInputElement
        /// we will try to let it handle the command first. If it does not handle the print command we will get the corresponding
        /// visual in the visual tree and use that to print.
        ///</summary>
        private static void OnCommandPrint(object sender, ExecutedRoutedEventArgs e)
        {
#if !DONOTREFPRINTINGASMMETA
            RootBrowserWindow rbw = sender as RootBrowserWindow;
            Invariant.Assert(rbw != null);

            if (! rbw._isPrintingFromRBW)
            {
                Visual vis = rbw.Content as Visual;

                if (vis == null)
                {
                    // If the content is not Visual but IInputElement, try to let it handle the command first.
                    // This is for the document scenario. Printing a document is different from printing a visual.
                    // Printing a visual is to print how it is rendered on screen. Printing a doc prints the full
                    // doc inculding the part that is not visible. There might be other functionalities that are
                    // specific for document. FlowDocument's viewer knows how to print the doc.
                    IInputElement target = rbw.Content as IInputElement;

                    if (target != null)
                    {
                        // CanExecute will bubble up. If nobody between the content and rbw can handle it,
                        // It would call back on RBW again. Use _isPrintingFromRBW to prevent the loop.
                        rbw._isPrintingFromRBW = true;
                        try
                        {
                            if (ApplicationCommands.Print.CanExecute(null, target))
                            {
                                ApplicationCommands.Print.Execute(null, target);
                                return;
                            }
                        }
                        finally
                        {
                            rbw._isPrintingFromRBW = false;
                        }
                    }
                }

                // Let the user choose a printer and set print options.
                PrintDialog printDlg = new PrintDialog();

                // If the user pressed the OK button on the print dialog, we proceed
                if (printDlg.ShowDialog() == true)
                {
                    string printJobDescription = GetPrintJobDescription(App.MainWindow);

                    // If the root is not visual and does not know how to print itself, we find the
                    // corresponding visual and use that to print.
                    if (vis == null)
                    {
                        INavigatorImpl navigator = rbw as INavigatorImpl;
                        Invariant.Assert(navigator != null);

                        vis = navigator.FindRootViewer();
                        Invariant.Assert(vis != null);
                    }

                    // Area we can print to for the chosen printer
                    Rect imageableRect = GetImageableRect(printDlg);

                    // We print Visuals aligned with the top/left corner of the printable area.
                    // We do not attempt to print very large Visuals across multiple pages.
                    // Any portion that doesn't fit on a single page will get cropped by the
                    // print system.

                    // Used to draw our visual into another visual for printing purposes
                    VisualBrush visualBrush = new VisualBrush(vis);
                    visualBrush.Stretch = Stretch.None;

                    // Visual we will print - containing a rectangle the size of our
                    // original Visual but offset into the printable area
                    DrawingVisual drawingVisual = new DrawingVisual();
                    DrawingContext context = drawingVisual.RenderOpen();
                    context.DrawRectangle(visualBrush,
                                          null,
                                          new Rect(imageableRect.X,
                                                   imageableRect.Y,
                                                   vis.VisualDescendantBounds.Width,
                                                   vis.VisualDescendantBounds.Height));
                    context.Close();

                    printDlg.PrintVisual(drawingVisual, printJobDescription);
                }
            }
#endif // DONOTREFPRINTINGASMMETA
        }

        private static void OnQueryEnabledCommandPrint(object sender, CanExecuteRoutedEventArgs  e)
        {
            RootBrowserWindow rbw = sender as RootBrowserWindow;
            Invariant.Assert(rbw != null);

            if ((!e.Handled) && (!rbw._isPrintingFromRBW))
            {
                // While we could print null it doesn't really make sense to do so
                e.CanExecute = rbw.Content != null;
            }
        }

#if !DONOTREFPRINTINGASMMETA   
        private static Rect GetImageableRect(PrintDialog dialog)
        {
            Rect imageableRect = Rect.Empty;
            
            Invariant.Assert(dialog != null, "Dialog should not be null.");

            System.Printing.PrintQueue queue = null;
            System.Printing.PrintCapabilities capabilities = null;
            System.Printing.PageImageableArea imageableArea = null;
            
            // This gets the PringDocumentImageableArea.OriginWidth/OriginHeight 
            // of the PrintQueue the user chose in the dialog.
            queue = dialog.PrintQueue;
            if (queue != null)
            {
                capabilities = queue.GetPrintCapabilities();
            }

            if (capabilities != null)
            {
                imageableArea = capabilities.PageImageableArea;
                if (imageableArea != null)
                {
                    imageableRect = new Rect(imageableArea.OriginWidth,
                                             imageableArea.OriginHeight,
                                             imageableArea.ExtentWidth,
                                             imageableArea.ExtentHeight);
                }
            }

            // If for any reason we couldn't get the actual printer's values
            // we fallback to a constant and the values available from the
            // PrintDialog.
            if (imageableRect == Rect.Empty)
            {
                imageableRect = new Rect(NON_PRINTABLE_MARGIN,
                    NON_PRINTABLE_MARGIN,
                    dialog.PrintableAreaWidth,
                    dialog.PrintableAreaHeight);
            }
            
            return imageableRect;
        }
#endif

        /// <summary>
        ///    Generate the title of the print job for a given Window.
        /// </summary>
        private static string GetPrintJobDescription(Window window)
        {
            Invariant.Assert(window != null, "Window should not be null.");

            string description = null;
            string pageTitle = null;

            // Get the window title
            string windowTitle = window.Title;
            if (windowTitle != null)
            {
                windowTitle = windowTitle.Trim();
            }

            // Get the page title, if available
            Page page = window.Content as Page;
            if (page != null)
            {
                pageTitle = page.Title;
                if (pageTitle != null)
                {
                    pageTitle = pageTitle.Trim();
                }
            }

            // If window and page title are available, use them together,
            // otherwise use which ever is available
            if (!String.IsNullOrEmpty(windowTitle))
            {
                if (!String.IsNullOrEmpty(pageTitle))
                {
                    description = SR.Get(SRID.PrintJobDescription, windowTitle, pageTitle);
                }
                else
                {
                    description = windowTitle;
                }
            }

            // No window title so use the page title on its own
            if (description == null && !String.IsNullOrEmpty(pageTitle))
            {
                description = pageTitle;
            }

            // If neither window or page titles are available, try and use
            // the source URI for the content
            if (description == null && BrowserInteropHelper.Source != null)
            {
                Uri source = BrowserInteropHelper.Source;
                if (source.IsFile)
                {
                    description = source.LocalPath;
                }
                else
                {
                    description = source.ToString();
                }
            }

            // If no other option, use a localized constant
            if (description == null)
            {
                description = SR.Get(SRID.UntitledPrintJobDescription);
            }

            return description;
        }


        #endregion Private methods

        //----------------------------------------------
        //
        // Private Properties
        //
        //----------------------------------------------
        #region Private properties
        private static Application App
        {
            get { return Application.Current; }
        }


        private static IBrowserCallbackServices Browser
        {
            get
            {
                // There will be no IBrowserCallbackServices available in some situations, e.g., during shutdown
                IBrowserCallbackServices ibcs = (App == null ? null : App.BrowserCallbackServices);
                return ibcs;
            }
        }

        private bool IsDownlevelPlatform
        {
            get
            {
                if (!_isDownlevelPlatformValid)
                {
                    IBrowserCallbackServices ibcs = Browser;
                    _isDownlevelPlatform = (ibcs != null) ? ibcs.IsDownlevelPlatform() : false;

                    _isDownlevelPlatformValid = true;
                }

                return _isDownlevelPlatform;
            }
        }

        internal bool HasTravelLogIntegration
        {
            get
            {
                return !IsDownlevelPlatform && BrowserInteropHelper.IsAvalonTopLevel;
            }
        }

        #endregion Private properties

        //----------------------------------------------
        //
        // Private Classes
        //
        //----------------------------------------------
        #region Private Classes

        /// <summary>
        /// This class is used to print objects which are not directly supported by XpsDocumentWriter.
        /// It's sole purpose is to make a non-abstract version of Visual.
        /// </summary>
        private class PrintVisual : ContainerVisual { }


        private class KeyInputSite : IKeyboardInputSite
        {
            internal KeyInputSite(SecurityCriticalData<IKeyboardInputSink> sink)
            {
                _sink = sink;
            }

            void IKeyboardInputSite.Unregister()
            {
                _sink = new SecurityCriticalData<IKeyboardInputSink>(null);
            }

            IKeyboardInputSink IKeyboardInputSite.Sink
            {
                get { return _sink.Value; }
            }

            bool IKeyboardInputSite.OnNoMoreTabStops(TraversalRequest request)
            {
                return Browser.TabOut(request.FocusNavigationDirection == FocusNavigationDirection.Next);
                // i. Tabbing-in is handled by ApplicationProxyInternal.
            }

            SecurityCriticalData<IKeyboardInputSink> _sink;
        };

        #endregion Private Classes


        //----------------------------------------------
        //
        // Private Members
        //
        //----------------------------------------------
        #region private members

        //Cache the values until the HwndSourceWindow is created
        private int  _xDeviceUnits, _yDeviceUnits, _widthDeviceUnits, _heightDeviceUnits;
        private bool _rectSet;

        private bool _isPrintingFromRBW;
        private bool _isDownlevelPlatformValid;
        private bool _isDownlevelPlatform;
        private bool _sourceWindowCreationCompleted;

        private List<WebBrowser> _webBrowserList = new List<WebBrowser>();

        /// <summary>
        /// The event delegate has to be stored because HwndWrapper keeps only a weak reference to it.
        /// </summary>
        private HwndWrapperHook _inputPostFilter;

        private bool _loadingCompletePosted;
        const int READYSTATE_COMPLETE = 4;

        // Fallback constant for the size of non-printable margin.  This is used if
        // we cant retrieve the actual size from the PrintQueue.
        private const int NON_PRINTABLE_MARGIN = 15;

        //
        // Setting these as the min-browser width & height.
        // Note that we can't use User's min window-width & height - these are specifically about browser window.
        //
        private const int MIN_BROWSER_WIDTH_DEVICE_UNITS = 200;

        private const int MIN_BROWSER_HEIGHT_DEVICE_UNITS = 200;
        #endregion private members
    }
}
