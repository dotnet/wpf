// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Internal;
using MS.Internal.KnownBoxes;
using MS.Internal.Media;
using MS.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Markup;
using System.Windows.Threading;
using System.Security;

namespace System.Windows.Controls
{
    /// <summary>
    ///     Service class that provides the input for the ContextMenu and ToolTip services.
    /// </summary>
    internal sealed class PopupControlService
    {
        #region Creation

        internal PopupControlService()
        {
            InputManager.Current.PostProcessInput += new ProcessInputEventHandler(OnPostProcessInput);
        }
        
        #endregion

        #region Input Handling

        /////////////////////////////////////////////////////////////////////
        private void OnPostProcessInput(object sender, ProcessInputEventArgs e)
        {
            if (e.StagingItem.Input.RoutedEvent == InputManager.InputReportEvent)
            {
                InputReportEventArgs report = (InputReportEventArgs)e.StagingItem.Input;
                if (!report.Handled)
                {
                    if (report.Report.Type == InputType.Mouse)
                    {
                        RawMouseInputReport mouseReport = (RawMouseInputReport)report.Report;
                        if ((mouseReport.Actions & RawMouseActions.AbsoluteMove) == RawMouseActions.AbsoluteMove)
                        {
                            if ((Mouse.LeftButton == MouseButtonState.Pressed) ||
                                (Mouse.RightButton == MouseButtonState.Pressed))
                            {
                                DismissToolTips();
                            }
                            else
                            {
                                IInputElement directlyOver = Mouse.PrimaryDevice.RawDirectlyOver;

                                if (directlyOver != null)
                                {
                                    // If possible, check that the mouse position is within the render bounds
                                    // (avoids mouse capture confusion).
                                    if (Mouse.CapturedMode != CaptureMode.None)
                                    {
                                        // Get the root visual
                                        PresentationSource source = PresentationSource.CriticalFromVisual((DependencyObject)directlyOver);
                                        UIElement rootAsUIElement = source != null ? source.RootVisual as UIElement : null;
                                        if (rootAsUIElement != null)
                                        {
                                            // Get mouse position wrt to root
                                            Point pt = Mouse.PrimaryDevice.GetPosition(rootAsUIElement);

                                            // Hittest to find the element the mouse is over
                                            IInputElement enabledHit;
                                            rootAsUIElement.InputHitTest(pt, out enabledHit, out directlyOver);
                                        }
                                        else
                                        {
                                            directlyOver = null;
                                        }
                                    }

                                    if (directlyOver != null)
                                    {
                                        // Process the mouse move
                                        OnMouseMove(directlyOver, mouseReport);
                                    }
                                }
                            }
                        }
                        else if ((mouseReport.Actions & RawMouseActions.Deactivate) == RawMouseActions.Deactivate)
                        {
                            DismissToolTips();
                            LastMouseDirectlyOver = null;

                            // When the user moves the cursor outside of the window,
                            // clear the LastMouseToolTipOwner property so if the user returns
                            // the mouse to the same item, the tooltip will reappear.  If
                            // the deactivation is coming from a window grabbing capture
                            // (such as Drag and Drop) do not clear the property.
                            if (MS.Win32.SafeNativeMethods.GetCapture() == IntPtr.Zero)
                            {
                                LastMouseToolTipOwner = null;
                            }
                        }
                    }
                }
            }
            else if (e.StagingItem.Input.RoutedEvent == Keyboard.KeyDownEvent)
            {
                ProcessKeyDown(sender, (KeyEventArgs)e.StagingItem.Input);
            }
            else if (e.StagingItem.Input.RoutedEvent == Keyboard.KeyUpEvent)
            {
                ProcessKeyUp(sender, (KeyEventArgs)e.StagingItem.Input);
            }
            else if (e.StagingItem.Input.RoutedEvent == Mouse.MouseUpEvent)
            {
                ProcessMouseUp(sender, (MouseButtonEventArgs)e.StagingItem.Input);
            }
            else if (e.StagingItem.Input.RoutedEvent == Mouse.MouseDownEvent)
            {
                DismissToolTips();
            }
            else if (e.StagingItem.Input.RoutedEvent == Keyboard.GotKeyboardFocusEvent)
            {
                ProcessGotKeyboardFocus(sender, (KeyboardFocusChangedEventArgs)e.StagingItem.Input);
            }
            else if (e.StagingItem.Input.RoutedEvent == Keyboard.LostKeyboardFocusEvent)
            {
                ProcessLostKeyboardFocus(sender, (KeyboardFocusChangedEventArgs)e.StagingItem.Input);
            }
        }

        private void OnMouseMove(IInputElement directlyOver, RawMouseInputReport mouseReport)
        {
            if (MouseHasLeftSafeArea(mouseReport))
            {
                DismissCurrentToolTip();
            }

            if (directlyOver != LastMouseDirectlyOver)
            {
                LastMouseDirectlyOver = directlyOver;
                DependencyObject owner = FindToolTipOwner(directlyOver, ToolTipService.TriggerAction.Mouse);

                BeginShowToolTip(owner, ToolTipService.TriggerAction.Mouse);
            }
        }

        /////////////////////////////////////////////////////////////////////
        private void ProcessGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            // any focus change dismisses tooltips triggered from the keyboard
            DismissKeyboardToolTips();

            // focus changes caused by keyboard navigation can show a tooltip
            if (KeyboardNavigation.IsKeyboardMostRecentInputDevice())
            {
                IInputElement focusedElement = e.NewFocus;
                DependencyObject owner = FindToolTipOwner(focusedElement, ToolTipService.TriggerAction.KeyboardFocus);

                BeginShowToolTip(owner, ToolTipService.TriggerAction.KeyboardFocus);
            }
        }

        /////////////////////////////////////////////////////////////////////
        private void ProcessLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            // any focus change dismisses tooltips triggered from the keyboard
            DismissKeyboardToolTips();
        }

        /////////////////////////////////////////////////////////////////////
        private void ProcessMouseUp(object sender, MouseButtonEventArgs e)
        {
            DismissToolTips();

            if (!e.Handled)
            {
                if ((e.ChangedButton == MouseButton.Right) &&
                    (e.RightButton == MouseButtonState.Released))
                {
                    IInputElement directlyOver = Mouse.PrimaryDevice.RawDirectlyOver;
                    if (directlyOver != null)
                    {
                        Point pt = Mouse.PrimaryDevice.GetPosition(directlyOver);
                        if (RaiseContextMenuOpeningEvent(directlyOver, pt.X, pt.Y,e.UserInitiated))
                        {
                            e.Handled = true;
                        }
                    }
                }
            }
        }

        /////////////////////////////////////////////////////////////////////
        private void ProcessKeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Handled)
            {
                const ModifierKeys ModifierMask = ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Windows;
                ModifierKeys modifierKeys = Keyboard.Modifiers & ModifierMask;

                if ((e.SystemKey == Key.F10) && (modifierKeys == (ModifierKeys.Control | ModifierKeys.Shift)))
                {
                    e.Handled = OpenOrCloseToolTipViaShortcut();
                }
                else if ((e.SystemKey == Key.F10) && (modifierKeys == ModifierKeys.Shift))
                {
                    RaiseContextMenuOpeningEvent(e);
                }

                // track the last key-down, to detect Ctrl-KeyUp trigger
                _lastCtrlKeyDown = Key.None;
                if ((CurrentToolTip?.FromKeyboard ?? false) && (modifierKeys == ModifierKeys.Control) &&
                        (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl))
                {
                    _lastCtrlKeyDown = e.Key;
                }
            }
        }

        /////////////////////////////////////////////////////////////////////
        private void ProcessKeyUp(object sender, KeyEventArgs e)
        {
            if (!e.Handled)
            {
                if (e.Key == Key.Apps)
                {
                    RaiseContextMenuOpeningEvent(e);
                }

                // dismiss the keyboard ToolTip when user presses and releases Ctrl
                if ((_lastCtrlKeyDown != Key.None) && (e.Key == _lastCtrlKeyDown) &&
                        (Keyboard.Modifiers == ModifierKeys.None) && (CurrentToolTip?.FromKeyboard ?? false))
                {
                    DismissCurrentToolTip();
                }
                _lastCtrlKeyDown = Key.None;
            }
        }

        #endregion

        #region ToolTip

        private bool OpenOrCloseToolTipViaShortcut()
        {
            DependencyObject owner = FindToolTipOwner(Keyboard.FocusedElement, ToolTipService.TriggerAction.KeyboardShortcut);
            if (owner == null)
                return false;


            // if the owner's tooltip is open, dismiss it.  Otherwise, show it.
            if (owner == GetOwner(CurrentToolTip))
            {
                DismissCurrentToolTip();
            }
            else
            {
                if (owner == GetOwner(PendingToolTip))
                {
                    // discard a previous pending request, so that the new one isn't ignored.
                    // This ensures that the tooltip opens immediately.
                    DismissPendingToolTip();
                }

                BeginShowToolTip(owner, ToolTipService.TriggerAction.KeyboardShortcut);
            }

            return true;
        }

        /// <summary>
        /// Initiate the process of showing a tooltip.
        /// Make a pending request, updating the pending and history state accordingly.
        /// Prepare to promote the pending tooltip to "current", which happens either
        /// immediately or after a delay.
        /// </summary>
        /// <param name="o">The tooltip owner</param>
        /// <param name="triggerAction">The action that triggered showing the tooltip</param>
        private void BeginShowToolTip(DependencyObject o, ToolTipService.TriggerAction triggerAction)
        {
            if (triggerAction == ToolTipService.TriggerAction.Mouse)
            {
                // ignore a mouse request if the mouse hasn't moved off the owner since the last mouse request
                if (o == LastMouseToolTipOwner)
                    return;
                LastMouseToolTipOwner = o;

                // cancel a pending mouse request if the mouse has moved off its owner
                if (PendingToolTip != null && !PendingToolTip.FromKeyboard && o != GetOwner(PendingToolTip))
                {
                    DismissPendingToolTip();
                }
            }

            // ignore a request if no owner, or already showing or pending its tooltip
            if (o == null || o == GetOwner(PendingToolTip) || o == GetOwner(CurrentToolTip))
                return;

            // discard the previous pending request
            DismissPendingToolTip();

            // record a pending request
            PendingToolTip = SentinelToolTip(o, triggerAction);

            // decide when to promote to current
            int showDelay;
            switch (triggerAction)
            {
                case ToolTipService.TriggerAction.Mouse:
                case ToolTipService.TriggerAction.KeyboardFocus:
                    showDelay = _quickShow ? 0 : ToolTipService.GetInitialShowDelay(o);
                    break;
                case ToolTipService.TriggerAction.KeyboardShortcut:
                default:
                    showDelay = 0;
                    break;
            }

            // promote now, or schedule delayed promotion
            if (showDelay == 0)
            {
                PromotePendingToolTipToCurrent(triggerAction);
            }
            else
            {
                PendingToolTipTimer = new DispatcherTimer(DispatcherPriority.Normal);
                PendingToolTipTimer.Interval = TimeSpan.FromMilliseconds(showDelay);
                PendingToolTipTimer.Tick += new EventHandler((s, e) => { PromotePendingToolTipToCurrent(triggerAction); });
                PendingToolTipTimer.Start();
            }
        }

        private void PromotePendingToolTipToCurrent(ToolTipService.TriggerAction triggerAction)
        {
            DependencyObject o = GetOwner(PendingToolTip);

            DismissToolTips();

            if (o != null)
            {
                ShowToolTip(o, ToolTipService.IsFromKeyboard(triggerAction));
            }
        }

        /// <summary>
        ///     Initiates the process of opening the tooltip popup,
        ///     and makes the tooltip "current".
        /// </summary>
        /// <param name="o">The owner of the tooltip</param>
        /// <param name="fromKeyboard">True if the tooltip is triggered by keyboard</param>
        private void ShowToolTip(DependencyObject o, bool fromKeyboard)
        {
            Debug.Assert(_currentToolTip == null);
            ResetCurrentToolTipTimer();
            OnForceClose(null, EventArgs.Empty);

            bool show = true;

            IInputElement element = o as IInputElement;
            if (element != null)
            {
                ToolTipEventArgs args = new ToolTipEventArgs(opening:true);
                // ** Public callout - re-entrancy is possible **//
                element.RaiseEvent(args);

                // [re-examine _currentToolTip, re-entrancy can change it]
                show = !args.Handled && (_currentToolTip == null);
            }

            if (show)
            {
                object tooltip = ToolTipService.GetToolTip(o);
                ToolTip tip = tooltip as ToolTip;
                if (tip != null)
                {
                    _currentToolTip = tip;
                }
                else
                {
                    _currentToolTip = new ToolTip();
                    _currentToolTip.SetValue(ServiceOwnedProperty, BooleanBoxes.TrueBox);

                    // Bind the content of the tooltip to the ToolTip attached property
                    Binding binding = new Binding();
                    binding.Path = new PropertyPath(ToolTipService.ToolTipProperty);
                    binding.Mode = BindingMode.OneWay;
                    binding.Source = o;
                    _currentToolTip.SetBinding(ToolTip.ContentProperty, binding);
                }

                if (!_currentToolTip.StaysOpen)
                {
                    // The popup takes capture in this case, which causes us to hit test to the wrong window.
                    // We do not support this scenario. Cleanup and then throw and exception.
                    throw new NotSupportedException(SR.Get(SRID.ToolTipStaysOpenFalseNotAllowed));
                }

                _currentToolTip.SetValue(OwnerProperty, o);
                _currentToolTip.Opened += OnToolTipOpened;
                _currentToolTip.Closed += OnToolTipClosed;
                _currentToolTip.FromKeyboard = fromKeyboard;
                _currentToolTip.IsOpen = true;

                CurrentToolTipTimer = new DispatcherTimer(DispatcherPriority.Normal);
                CurrentToolTipTimer.Interval = TimeSpan.FromMilliseconds(ToolTipService.GetShowDuration(o));
                CurrentToolTipTimer.Tick += new EventHandler(OnShowDurationTimerExpired);
                CurrentToolTipTimer.Start();
            }
        }

        private void OnShowDurationTimerExpired(object sender, EventArgs e)
        {
            DismissCurrentToolTip();
        }

        // called from ToolTip.OnContentChanged, when the owner of the current
        // tooltip changes its ToolTip property from a non-ToolTip to a ToolTip.
        internal void ReplaceCurrentToolTip()
        {
            ToolTip currentToolTip = _currentToolTip;
            if (currentToolTip == null)
                return;

            // get information from the current tooltip, before it goes away
            DependencyObject owner = GetOwner(currentToolTip);
            bool fromKeyboard = currentToolTip.FromKeyboard;

            // dismiss the current tooltip, then show a new one in its stead
            DismissCurrentToolTip();
            ShowToolTip(owner, fromKeyboard);
        }

        internal void DismissToolTipsForOwner(DependencyObject o)
        {
            if (o == GetOwner(PendingToolTip))
            {
                DismissPendingToolTip();
            }

            if (o == GetOwner(CurrentToolTip))
            {
                DismissCurrentToolTip();
            }
        }

        private void DismissToolTips()
        {
            DismissPendingToolTip();
            DismissCurrentToolTip();
        }

        private void DismissKeyboardToolTips()
        {
            if (PendingToolTip?.FromKeyboard ?? false)
            {
                DismissPendingToolTip();
            }

            if (CurrentToolTip?.FromKeyboard ?? false)
            {
                DismissCurrentToolTip();
            }
        }

        private void DismissPendingToolTip()
        {
            if (PendingToolTipTimer != null)
            {
                PendingToolTipTimer.Stop();
                PendingToolTipTimer = null;
            }

            if (PendingToolTip != null)
            {
                PendingToolTip = null;
                _sentinelToolTip.SetValue(OwnerProperty, null);
            }
        }

        private void DismissCurrentToolTip()
        {
            ToolTip currentToolTip = _currentToolTip;
            _currentToolTip = null;
            CloseToolTip(currentToolTip);
        }

        // initiate the process of closing the tooltip's popup.
        private void CloseToolTip(ToolTip tooltip)
        {
            if (tooltip == null)
                return;

            SetSafeArea(null);
            ResetCurrentToolTipTimer();

            // cache the owner now, in case re-entrancy clears it
            DependencyObject owner = GetOwner(tooltip);

            try
            {
                // notify listeners that the tooltip is closing
                if (tooltip.IsOpen)
                {
                    IInputElement element = owner as IInputElement;
                    if (element != null)
                    {
                        // ** Public callout - re-entrancy is possible **//
                        element.RaiseEvent(new ToolTipEventArgs(opening:false));
                    }
                }
            }
            finally
            {
                // close the tooltip popup
                // [re-examine IsOpen - re-entrancy could change it]
                if (tooltip.IsOpen)
                {
                    // ** Public callout - re-entrancy is possible **//
                    tooltip.IsOpen = false;

                    // allow time for the popup's fade-out or slide animation
                    _forceCloseTimer = new DispatcherTimer(DispatcherPriority.Normal);
                    _forceCloseTimer.Interval = Popup.AnimationDelayTime;
                    _forceCloseTimer.Tick += new EventHandler(OnForceClose);
                    _forceCloseTimer.Tag = tooltip;
                    _forceCloseTimer.Start();

                    // begin the BetweenShowDelay interval, during which another tooltip
                    // can open without the usual delay
                    int betweenShowDelay = ToolTipService.GetBetweenShowDelay(owner);
                    _quickShow = (betweenShowDelay > 0);
                    if (_quickShow)
                    {
                        CurrentToolTipTimer = new DispatcherTimer(DispatcherPriority.Normal);
                        CurrentToolTipTimer.Interval = TimeSpan.FromMilliseconds(betweenShowDelay);
                        CurrentToolTipTimer.Tick += new EventHandler(OnBetweenShowDelay);
                        CurrentToolTipTimer.Start();
                    }
                }
                else
                {
                    ClearServiceProperties(tooltip);
                }
            }
        }

        /// <summary>
        ///     Clean up any service-only properties we may have set on the given tooltip
        /// </summary>
        /// <param name="tooltip"></param>
        private void ClearServiceProperties(ToolTip tooltip)
        {
            if (tooltip != null)
            {
                tooltip.ClearValue(OwnerProperty);
                tooltip.FromKeyboard = false;

                if ((bool)tooltip.GetValue(ServiceOwnedProperty))
                {
                    BindingOperations.ClearBinding(tooltip, ToolTip.ContentProperty);
                }
            }
        }

        private DependencyObject FindToolTipOwner(IInputElement element, ToolTipService.TriggerAction triggerAction)
        {
            if (element == null)
                return null;

            DependencyObject owner = null;
            switch (triggerAction)
            {
                case ToolTipService.TriggerAction.Mouse:
                    // look up the tree for the nearest tooltip owner
                    FindToolTipEventArgs args = new FindToolTipEventArgs(triggerAction);
                    element.RaiseEvent(args);
                    owner = args.TargetElement;
                    break;

                case ToolTipService.TriggerAction.KeyboardFocus:
                case ToolTipService.TriggerAction.KeyboardShortcut:
                    // use the element itself, if it is a tooltip owner
                    owner = element as DependencyObject;
                    if (owner != null && !ToolTipService.ToolTipIsEnabled(owner, triggerAction))
                    {
                        owner = null;
                    }
                    break;
            }

            // ignore nested tooltips
            if (WithinCurrentToolTip(owner))
            {
                owner = null;
            }

            return owner;
        }

        private bool WithinCurrentToolTip(DependencyObject o)
        {
            // If no current tooltip, then no need to look
            if (_currentToolTip == null)
            {
                return false;
            }

            DependencyObject v = o as Visual;
            if (v == null)
            {
                ContentElement ce = o as ContentElement;
                if (ce != null)
                {
                    v = FindContentElementParent(ce);
                }
                else
                {
                    v = o as Visual3D;
                }
            }

            return (v != null) &&
                   ((v is Visual && ((Visual)v).IsDescendantOf(_currentToolTip)) ||
                    (v is Visual3D && ((Visual3D)v).IsDescendantOf(_currentToolTip)));
        }

        private void ResetCurrentToolTipTimer()
        {
            if (CurrentToolTipTimer != null)
            {
                CurrentToolTipTimer.Stop();
                CurrentToolTipTimer = null;
                _quickShow = false;
            }
        }

        /// <summary>
        /// Event handler for ToolTip.Opened
        /// </summary>
        private void OnToolTipOpened(object sender, EventArgs e)
        {
            ToolTip toolTip = (ToolTip)sender;
            toolTip.Opened -= OnToolTipOpened;

            SetSafeArea(toolTip);
        }

        // Clear service properties when tooltip has closed
        private void OnToolTipClosed(object sender, EventArgs e)
        {
            ToolTip toolTip = (ToolTip)sender;
            toolTip.Closed -= OnToolTipClosed;
            ClearServiceProperties(toolTip);
        }

        // The previous tooltip hasn't closed and we are trying to open a new one
        private void OnForceClose(object sender, EventArgs e)
        {
            if (_forceCloseTimer != null)
            {
                _forceCloseTimer.Stop();
                ToolTip toolTip = (ToolTip)_forceCloseTimer.Tag;
                toolTip.ForceClose();
                _forceCloseTimer = null;
            }
        }

        private void OnBetweenShowDelay(object source, EventArgs e)
        {
            ResetCurrentToolTipTimer();
        }

        private ToolTip PendingToolTip
        {
            get { return _pendingToolTip; }
            set { _pendingToolTip = value; }
        }

        private DispatcherTimer PendingToolTipTimer
        {
            get { return _pendingToolTipTimer; }
            set { _pendingToolTipTimer = value; }
        }

        internal ToolTip CurrentToolTip
        {
            get { return _currentToolTip; }
        }

        private DispatcherTimer CurrentToolTipTimer
        {
            get { return _currentToolTipTimer; }
            set { _currentToolTipTimer = value; }
        }

        private IInputElement LastMouseDirectlyOver
        {
            get { return _lastMouseDirectlyOver.GetValue(); }
            set { _lastMouseDirectlyOver.SetValue(value); }
        }

        private DependencyObject LastMouseToolTipOwner
        {
            get { return _lastMouseToolTipOwner.GetValue(); }
            set { _lastMouseToolTipOwner.SetValue(value); }
        }

        private DependencyObject GetOwner(ToolTip t)
        {
            return t?.GetValue(OwnerProperty) as DependencyObject;
        }

        // a pending request is represented by a sentinel ToolTip object that carries
        // the owner and the trigger action (only).  There's never more than one
        // pending request, so we reuse the same sentinel object.
        private ToolTip SentinelToolTip(DependencyObject o, ToolTipService.TriggerAction triggerAction)
        {
            // lazy creation, because we cannot create it in the ctor (infinite loop with FrameworkServices..ctor)
            if (_sentinelToolTip == null)
            {
                _sentinelToolTip = new ToolTip();
            }

            _sentinelToolTip.SetValue(OwnerProperty, o);
            _sentinelToolTip.FromKeyboard = ToolTipService.IsFromKeyboard(triggerAction);
            return _sentinelToolTip;
        }

        #region Safe Area

        private void SetSafeArea(ToolTip tooltip)
        {
            SafeArea = null;     // default is no safe area

            // safe area is only needed for tooltips triggered by mouse
            if (tooltip != null && !tooltip.FromKeyboard)
            {
                DependencyObject owner = GetOwner(tooltip);
                PresentationSource presentationSource = (owner != null) ? PresentationSource.CriticalFromVisual(owner) : null;

                if (presentationSource != null)
                {
                    // build a list of (native) rects, in the presentationSource's client coords
                    List<NativeMethods.RECT> rects = new List<NativeMethods.RECT>();

                    // add the owner rect(s)
                    UIElement ownerUIE;
                    ContentElement ownerCE;
                    if ((ownerUIE = owner as UIElement) != null)
                    {
                        // tooltip is owned by a UIElement.
                        Rect rectElement = new Rect(new Point(0, 0), ownerUIE.RenderSize);
                        Rect rectRoot = PointUtil.ElementToRoot(rectElement, ownerUIE, presentationSource);
                        Rect ownerRect = PointUtil.RootToClient(rectRoot, presentationSource);

                        if (!ownerRect.IsEmpty)
                        {
                            rects.Add(PointUtil.FromRect(ownerRect));
                        }
                    }
                    else if ((ownerCE = owner as ContentElement) != null)
                    {
                        // tooltip is owned by a ContentElement (e.g. Hyperlink).
                        IContentHost ichParent = null;
                        UIElement uieParent = KeyboardNavigation.GetParentUIElementFromContentElement(ownerCE, ref ichParent);
                        Visual visualParent = ichParent as Visual;

                        if (visualParent != null && uieParent != null)
                        {
                            IReadOnlyCollection<Rect> ownerRects = ichParent.GetRectangles(ownerCE);

                            // we're going to do the same transformations as in the UIElement case above.
                            // But using the PointUtil convenience methods would recompute transforms that
                            // are the same for each rect.  Instead, do the usual optimization of computing
                            // common expressions before the loop, leaving only loop-dependent work inside.
                            GeneralTransform transformToRoot = visualParent.TransformToAncestor(presentationSource.RootVisual);
                            CompositionTarget target = presentationSource.CompositionTarget;
                            Matrix matrixRootTransform = PointUtil.GetVisualTransform(target.RootVisual);
                            Matrix matrixDPI = target.TransformToDevice;

                            foreach (Rect rect in ownerRects)
                            {
                                Rect rectRoot = transformToRoot.TransformBounds(rect);
                                Rect rectRootUntransformed = Rect.Transform(rectRoot, matrixRootTransform);
                                Rect rectClient = Rect.Transform(rectRootUntransformed, matrixDPI);
                                rects.Add(PointUtil.FromRect(rectClient));
                            }
                        }
                    }

                    // add the tooltip rect
                    Rect screenRect = tooltip.GetScreenRect();
                    Point clientPt = PointUtil.ScreenToClient(screenRect.Location, presentationSource);
                    Rect tooltipRect = new Rect(clientPt, screenRect.Size);

                    if (!tooltipRect.IsEmpty)
                    {
                        rects.Add(PointUtil.FromRect(tooltipRect));
                    }

                    // find the convex hull
                    SafeArea = new ConvexHull(presentationSource, rects);
                }
            }
        }

        private bool MouseHasLeftSafeArea(RawMouseInputReport mouseReport)
        {
            return !(SafeArea?.ContainsPoint(mouseReport.InputSource, mouseReport.X, mouseReport.Y) ?? true);
        }

        private ConvexHull SafeArea { get; set; }

        #endregion

        #endregion

        #region ContextMenu

        /// <summary>
        ///     Event that fires on ContextMenu when it opens.
        ///     Located here to avoid circular dependencies.
        /// </summary>
        internal static readonly RoutedEvent ContextMenuOpenedEvent =
                EventManager.RegisterRoutedEvent("Opened", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(PopupControlService));

        /// <summary>
        ///     Event that fires on ContextMenu when it closes.
        ///     Located here to avoid circular dependencies.
        /// </summary>
        internal static readonly RoutedEvent ContextMenuClosedEvent =
            EventManager.RegisterRoutedEvent("Closed", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(PopupControlService));

        /////////////////////////////////////////////////////////////////////
        private void RaiseContextMenuOpeningEvent(KeyEventArgs e)
        {
            IInputElement source = e.OriginalSource as IInputElement;
            if (source != null)
            {
                if (RaiseContextMenuOpeningEvent(source, -1.0, -1.0,e.UserInitiated))
                {
                    e.Handled = true;
                }
            }
        }

        private bool RaiseContextMenuOpeningEvent(IInputElement source, double x, double y,bool userInitiated)
        {
            // Fire the event
            ContextMenuEventArgs args = new ContextMenuEventArgs(source, true /* opening */, x, y);
            DependencyObject sourceDO = source as DependencyObject;
            if (userInitiated && sourceDO != null)
            {
                if (InputElement.IsUIElement(sourceDO))
                {
                    ((UIElement)sourceDO).RaiseEvent(args, userInitiated);
                }
                else if (InputElement.IsContentElement(sourceDO))
                {
                    ((ContentElement)sourceDO).RaiseEvent(args, userInitiated);
                }
                else if (InputElement.IsUIElement3D(sourceDO))
                {
                    ((UIElement3D)sourceDO).RaiseEvent(args, userInitiated);
                }
                else
                {
                    source.RaiseEvent(args);
                }
            }
            else
            {
                source.RaiseEvent(args);
            }


            if (!args.Handled)
            {
                // No one handled the event, auto show any available ContextMenus

                // Saved from the bubble up the tree where we looked for a set ContextMenu property
                DependencyObject o = args.TargetElement;
                if ((o != null) && ContextMenuService.ContextMenuIsEnabled(o))
                {
                    // Retrieve the value
                    object menu = ContextMenuService.GetContextMenu(o);
                    ContextMenu cm = menu as ContextMenu;
                    cm.SetValue(OwnerProperty, o);
                    cm.Closed += new RoutedEventHandler(OnContextMenuClosed);

                    if ((x == -1.0) && (y == -1.0))
                    {
                        // We infer this to mean that the ContextMenu was opened with the keyboard
                        cm.Placement = PlacementMode.Center;
                    }
                    else
                    {
                        // If there is a CursorLeft and CursorTop, it was opened with the mouse.
                        cm.Placement = PlacementMode.MousePoint;
                    }

                    // Clear any open tooltips
                    DismissToolTips();

                    cm.SetCurrentValueInternal(ContextMenu.IsOpenProperty, BooleanBoxes.TrueBox);

                    return true; // A menu was opened
                }

                return false; // There was no menu to open
            }

            // Clear any open tooltips since someone else opened one
            DismissToolTips();

            return true; // The event was handled by someone else
        }


        private void OnContextMenuClosed(object source, RoutedEventArgs e)
        {
            ContextMenu cm = source as ContextMenu;
            if (cm != null)
            {
                cm.Closed -= OnContextMenuClosed;

                DependencyObject o = (DependencyObject)cm.GetValue(OwnerProperty);
                if (o != null)
                {
                    cm.ClearValue(OwnerProperty);

                    UIElement uie = GetTarget(o);
                    if (uie != null)
                    {
                        if (!IsPresentationSourceNull(uie))
                        {
                            IInputElement inputElement = (o is ContentElement || o is UIElement3D) ? (IInputElement)o : (IInputElement)uie;
                            ContextMenuEventArgs args = new ContextMenuEventArgs(inputElement, false /*opening */);
                            inputElement.RaiseEvent(args);
                        }
                    }
                }
            }
        }

        private static bool IsPresentationSourceNull(DependencyObject uie)
        {
            return PresentationSource.CriticalFromVisual(uie) == null;
        }

        #endregion

        #region Helpers

        internal static DependencyObject FindParent(DependencyObject o)
        {
            // see if o is a Visual or a Visual3D
            DependencyObject v = o as Visual;
            if (v == null)
            {
                v = o as Visual3D;
            }

            ContentElement ce = (v == null) ? o as ContentElement : null;

            if (ce != null)
            {
                o = ContentOperations.GetParent(ce);
                if (o != null)
                {
                    return o;
                }
                else
                {
                    FrameworkContentElement fce = ce as FrameworkContentElement;
                    if (fce != null)
                    {
                        return fce.Parent;
                    }
                }
            }
            else if (v != null)
            {
                return VisualTreeHelper.GetParent(v);
            }

            return null;
        }

        internal static DependencyObject FindContentElementParent(ContentElement ce)
        {
            DependencyObject nearestVisual = null;
            DependencyObject o = ce;

            while (o != null)
            {
                nearestVisual = o as Visual;
                if (nearestVisual != null)
                {
                    break;
                }

                nearestVisual = o as Visual3D;
                if (nearestVisual != null)
                {
                    break;
                }

                ce = o as ContentElement;
                if (ce != null)
                {
                    o = ContentOperations.GetParent(ce);
                    if (o == null)
                    {
                        FrameworkContentElement fce = ce as FrameworkContentElement;
                        if (fce != null)
                        {
                            o = fce.Parent;
                        }
                    }
                }
                else
                {
                    // This could be application.
                    break;
                }
            }

            return nearestVisual;
        }

        internal static bool IsElementEnabled(DependencyObject o)
        {
            bool enabled = true;
            UIElement uie = o as UIElement;
            ContentElement ce = (uie == null) ? o as ContentElement : null;
            UIElement3D uie3D = (uie == null && ce == null) ? o as UIElement3D : null;

            if (uie != null)
            {
                enabled = uie.IsEnabled;
            }
            else if (ce != null)
            {
                enabled = ce.IsEnabled;
            }
            else if (uie3D != null)
            {
                enabled = uie3D.IsEnabled;
            }

            return enabled;
        }

        internal static PopupControlService Current
        {
            get
            {
                return FrameworkElement.PopupControlService;
            }
        }

        /// <summary>
        ///     Returns the UIElement target
        /// </summary>
        private static UIElement GetTarget(DependencyObject o)
        {
            UIElement uie = o as UIElement;
            if (uie == null)
            {
                ContentElement ce = o as ContentElement;
                if (ce != null)
                {
                    DependencyObject ceParent = FindContentElementParent(ce);

                    // attempt to cast to a UIElement
                    uie = ceParent as UIElement;
                    if (uie == null)
                    {
                        // target can't be a UIElement3D - so get the nearest containing UIElement
                        UIElement3D uie3D = ceParent as UIElement3D;
                        if (uie3D != null)
                        {
                            uie = UIElementHelper.GetContainingUIElement2D(uie3D);
                        }
                    }
                }
                else
                {
                    // it wasn't a UIElement or ContentElement, try one last cast to UIElement3D
                    // target can't be a UIElement3D - so get the nearest containing UIElement
                    UIElement3D uie3D = o as UIElement3D;

                    if (uie3D != null)
                    {
                        uie = UIElementHelper.GetContainingUIElement2D(uie3D);
                    }
                }
            }

            return uie;
        }

        /// <summary>
        ///     Indicates whether the service owns the tooltip
        /// </summary>
        internal static readonly DependencyProperty ServiceOwnedProperty =
            DependencyProperty.RegisterAttached("ServiceOwned",                 // Name
                                                typeof(bool),                   // Type
                                                typeof(PopupControlService),    // Owner
                                                new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        ///     Stores the original element on which to fire the closed event
        /// </summary>
        internal static readonly DependencyProperty OwnerProperty =
            DependencyProperty.RegisterAttached("Owner",                        // Name
                                                typeof(DependencyObject),       // Type
                                                typeof(PopupControlService),    // Owner
                                                new FrameworkPropertyMetadata((DependencyObject)null, // Default Value
                                                                               new PropertyChangedCallback(OnOwnerChanged)));

        // When the owner changes, coerce all attached properties from the service
        private static void OnOwnerChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is ContextMenu)
            {
                o.CoerceValue(ContextMenu.HorizontalOffsetProperty);
                o.CoerceValue(ContextMenu.VerticalOffsetProperty);
                o.CoerceValue(ContextMenu.PlacementTargetProperty);
                o.CoerceValue(ContextMenu.PlacementRectangleProperty);
                o.CoerceValue(ContextMenu.PlacementProperty);
                o.CoerceValue(ContextMenu.HasDropShadowProperty);
            }
            else if (o is ToolTip)
            {
                o.CoerceValue(ToolTip.HorizontalOffsetProperty);
                o.CoerceValue(ToolTip.VerticalOffsetProperty);
                o.CoerceValue(ToolTip.PlacementTargetProperty);
                o.CoerceValue(ToolTip.PlacementRectangleProperty);
                o.CoerceValue(ToolTip.PlacementProperty);
                o.CoerceValue(ToolTip.HasDropShadowProperty);
            }
        }

        // Returns the value of dp on the Owner if it is set there,
        // otherwise returns the value set on o (the tooltip or contextmenu)
        internal static object CoerceProperty(DependencyObject o, object value, DependencyProperty dp)
        {
            DependencyObject owner = (DependencyObject)o.GetValue(OwnerProperty);
            if (owner != null)
            {
                bool hasModifiers;
                if (owner.GetValueSource(dp, null, out hasModifiers) != BaseValueSourceInternal.Default || hasModifiers)
                {
                    // Return a value if it is set on the owner
                    return owner.GetValue(dp);
                }
                else if (dp == ToolTip.PlacementTargetProperty || dp == ContextMenu.PlacementTargetProperty)
                {
                    UIElement uie = GetTarget(owner);

                    // If it is the PlacementTarget property, return the owner itself
                    if (uie != null)
                        return uie;
                }
            }
            return value;
        }

        #endregion

        #region Private Types

        struct WeakRefWrapper<T> where T : class
        {
            private WeakReference<T> _storage;

            public T GetValue()
            {
                T value;
                if (_storage != null)
                {
                    if (!_storage.TryGetTarget(out value))
                    {
                        _storage = null;
                    }
                }
                else
                {
                    value = null;
                }

                return value;
            }

            public void SetValue(T value)
            {
                if (value == null)
                {
                    _storage = null;
                }
                else if (_storage == null)
                {
                    _storage = new WeakReference<T>(value);
                }
                else
                {
                    _storage.SetTarget(value);
                }
            }
        }

        // A region is convex if every line segment connecting two points of the region lies
        // within the region.  The convex hull of a set of points is the smallest convex region
        // that contains the points.  This is just what we need for the safe area of a tooltip
        // and its owner:  the tooltip should remain open as long as the mouse lies on a line
        // segment connecting some point in the owner to some point in the tooltip, i.e. as long
        // as the mouse is in the convex hull of the corners of the owner and tooltip rectangles.
        //
        // There are several aspects of this use-case we can exploit.
        //  * The points come from WM_MOUSEMOVE messages, in the coords of the hwnd's client area.
        //      This means they are 16-bit integers.  We can compute cross-products using
        //      integer multiplication without fear of overflow.
        //  * The convex hull is built from only 8 points - the corners of the two rectangles.
        //      We can use simple algorithms with low overhead, ignoring their less-than-optimal
        //      asymptotic cost.
        //  * The convex hull will have between 4 and 8 edges, at least 4 of which are axis-aligned.
        //      We can test these edges by simple integer comparison, no multiplications needed.
        //
        // These remarks apply to the case when the tooltip owner is a UIElement, and thus has a
        // single bounding rectangle.  When the owner is a ContentElement, it's bounding area can
        // be the union of many rectangles.  Nevertheless, the remarks still apply qualitatively:
        // the top-down scan is still efficient in practice (the rectangles usually arrive in
        // top-down order already), and the majority of edges in the resulting convex hull are
        // axis-aligned.
        class ConvexHull
        {
            internal ConvexHull(PresentationSource source, List<NativeMethods.RECT> rects)
            {
                _source = source;
                PointList points = new PointList();

                if (rects.Count == 1)
                {
                    // special-case optimization:  the hull of a single rectangle is the rectangle itself
                    AddPoints(points, rects[0], rectIsHull: true);
                    _points = points.ToArray();
                }
                else
                {
                    foreach (NativeMethods.RECT rect in rects)
                    {
                        AddPoints(points, rect);
                    }

                    SortPoints(points);
                    BuildHullIncrementally(points);
                }
            }

            // sort by y (and by x among equal y's)
            private void SortPoints(PointList points)
            {
                // insertion sort is good enough.  We're dealing with a small
                // set of points that are nearly in the right order already.
                for (int i=1, N=points.Count; i<N; ++i)
                {
                    Point p = points[i];
                    int j;
                    for (j=i-1; j>=0; --j)
                    {
                        int d = points[j].Y - p.Y;
                        if (d > 0 || (d == 0 && (points[j].X > p.X)))
                        {
                            points[j + 1] = points[j];
                        }
                        else break;
                    }
                    points[j + 1] = p;
                }
            }

            // build the convex hull
            // Precondition:  the points are sorted, in the sense of SortPoints
            private void BuildHullIncrementally(PointList points)
            {
                int N = points.Count;
                int currentIndex = 0;
                int hullCount = 0;
                int prevLeftmostIndex = 0, prevRightmostIndex = 0;

                // loop invariant:
                //  * given a value Y = points[currentIndex].Y, partition
                //      the original points into two sets:  a "small" set - points
                //      whose y < Y, and a "large" set - points whose y >= Y
                //  * the first hullCount points, points[0 ... hullCount-1], are the
                //      convex hull (in counterclockwise order) of the small points
                //  * the large points are in their original positions in
                //      points[currentIndex ... N-1], and haven't been examined.

                while (currentIndex < N)
                {
                    // Each iteration will deal with all the points whose y == Y,
                    // incrementally extending the convex hull to include them.
                    int Y = points[currentIndex].Y;

                    // find the leftmost and rightmost points whose y == Y
                    // (given that the points are sorted, these are simply the
                    // first and last points whose y == Y)
                    Point leftmost = points[currentIndex];
                    int next = currentIndex + 1;
                    while (next<N && points[next].Y == Y)
                    {
                        ++next;
                    }
                    Point rightmost = points[next - 1];

                    // remember if these are the same point, and advance currentIndex
                    // past the points whose y == Y
                    int pointsToAdd = (next == currentIndex + 1) ? 1 : 2;
                    currentIndex = next;

                    // add these point(s) to the partial convex hull
                    if (hullCount == 0)
                    {
                        // the first iteration is special: there are no points
                        // to remove, and we have to add the new points in the
                        // opposite order to get "counterclockwise" correct.
                        if (pointsToAdd == 2)
                        {
                            points[0] = rightmost;
                            points[1] = leftmost;
                            prevLeftmostIndex = 1;
                        }
                        else
                        {
                            points[0] = leftmost;
                            prevLeftmostIndex = 0;
                        }
                        prevRightmostIndex = hullCount = pointsToAdd;
                    }
                    else
                    {
                        // in the remaining iterations, the new point(s) replace
                        // a (possibly empty) segment of the current hull.  To
                        // identify that segment, locate the two points on the
                        // current convex hull that have the minimum polar angle with
                        // leftmost, and the maximum polar angle with rightmost.
                        // (It's possible to use binary search for this, but that
                        // adds overhead that wouldn't pay off in our small scenarios.)

                        // First examine the points in clockwise order, starting with the
                        // previous iteration's leftmost point.  The polar angle with
                        // leftmost will decrease for a while, then increase. The first
                        // increase (or termination) occurs at the desired minimum.
                        int minIndex = prevLeftmostIndex;
                        for (; minIndex > 0; --minIndex)
                        {
                            if (Cross(leftmost, points[minIndex], points[minIndex - 1]) > 0)
                                break;
                        }

                        // Similarly, examine the points in counterclockwise order, starting
                        // with the previous iteration's rightmost point.  The polar angle
                        // with rightmost will increase for a while, and the first decrease
                        // occurs at the desired maximum.
                        int maxIndex = prevRightmostIndex;
                        for (; maxIndex < hullCount; ++maxIndex)
                        {
                            int wrapIndex = maxIndex + 1;
                            if (wrapIndex == hullCount) wrapIndex = 0;
                            if (Cross(rightmost, points[maxIndex], points[wrapIndex]) < 0)
                                break;
                        }

                        // replace the segment of the hull between these two points with
                        // the leftmost and rightmost point(s)
                        int pointsToRemove = maxIndex - minIndex - 1;
                        int delta = pointsToAdd - pointsToRemove;

                        // move retained points to their new position
                        // (the hull is a subset of the original points, which
                        // guarantees that the indices into points are
                        // always in bounds).
                        if (delta < 0)
                        {
                            for (int i=maxIndex; i<hullCount; ++i)
                            {
                                points[i + delta] = points[i];
                            }
                        }
                        else if (delta > 0)
                        {
                            for (int i=hullCount-1; i>=maxIndex; --i)
                            {
                                points[i + delta] = points[i];
                            }
                        }

                        // insert the new point(s), and update the hull size
                        points[minIndex + 1] = leftmost;
                        prevLeftmostIndex = prevRightmostIndex = minIndex + 1;
                        if (pointsToAdd == 2)
                        {
                            points[minIndex + 2] = rightmost;
                            prevRightmostIndex = minIndex + 2;
                        }
                        hullCount += delta;
                    }
                }

                // when the loop terminates, the loop invariant plus the condition
                // (currentIndex >= N) imply points[0 ... hullCount-1] describe the
                // convex hull of the original points.  All that's left is to discard
                // any extra points, and compute the directions.
                points.RemoveRange(hullCount, N - hullCount);
                _points = points.ToArray();
                SetDirections();
            }

            // set the Direction field on each point.  This enables optimizations during
            // ContainsPoint.
            private void SetDirections()
            {
                for (int i=0, N=_points.Length; i<N; ++i)
                {
                    int next = i + 1;
                    if (next == N) next = 0;

                    if (_points[i].X == _points[next].X)
                    {
                        _points[i].Direction = (_points[i].Y >= _points[next].Y) ? Direction.Up : Direction.Down;
                    }
                    else if (_points[i].Y == _points[next].Y)
                    {
                        _points[i].Direction = (_points[i].X >= _points[next].X) ? Direction.Left : Direction.Right;
                    }
                    else
                    {
                        _points[i].Direction = Direction.Skew;
                    }
                }
            }

            private void AddPoints(PointList points, in NativeMethods.RECT rect, bool rectIsHull=false)
            {
                if (rectIsHull)
                {
                    // caller is asserting the convex hull is the rect itself,
                    // add its corner points in counterclockwise order with directions set
                    points.Add(new Point(rect.right, rect.top, Direction.Left));
                    points.Add(new Point(rect.left, rect.top, Direction.Down));
                    points.Add(new Point(rect.left, rect.bottom, Direction.Right));
                    points.Add(new Point(rect.right, rect.bottom, Direction.Up));
                }
                else
                {
                    // otherwise add the corner points in an order favorable to SortPoints
                    points.Add(new Point(rect.left, rect.top));
                    points.Add(new Point(rect.right, rect.top));
                    points.Add(new Point(rect.left, rect.bottom));
                    points.Add(new Point(rect.right, rect.bottom));
                }
            }

            // Test whether a given mouse point (x,y) lies within the convex hull
            internal bool ContainsPoint(PresentationSource source, int x, int y)
            {
                // points from the wrong source are not included
                if (source != _source)
                    return false;

                // a point is included if it's in the left half-plane of every
                // edge.  We test this in two passes, to postpone (and perhaps
                // avoid) multiplications, and to get the customary "exclusive"
                // behavior for edges that came from the bottom or right edges
                // of the original rectangles.

                // Pass 1 - handle the axis-aligned edges
                for (int i = 0, N = _points.Length; i < N; ++i)
                {
                    switch (_points[i].Direction)
                    {
                        case Direction.Left:
                            if (y < _points[i].Y) return false;
                            break;
                        case Direction.Right:
                            if (y >= _points[i].Y) return false;
                            break;
                        case Direction.Up:
                            if (x >= _points[i].X) return false;
                            break;
                        case Direction.Down:
                            if (x < _points[i].X) return false;
                            break;
                    }
                }

                // Pass 2 - handle the skew edges
                for (int i = 0, N = _points.Length; i < N; ++i)
                {
                    switch (_points[i].Direction)
                    {
                        case Direction.Skew:
                            int next = i + 1;
                            if (next == N) next = 0;
                            Point p = new Point(x, y);

                            if (Cross(_points[i], _points[next], p) > 0)
                                return false;
                            break;
                    }
                }

                // the point is on the correct side of all the edges
                return true;
            }

            // returns c's position relative to the line extending segment a -> b:
            //  <0  if c is in the left half-plane
            //   0  if c is on the line
            //  >0  if c is in the right half-plane
            private static int Cross(in Point a, in Point b, in Point c)
            {
                return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
            }

            enum Direction { Skew, Left, Right, Up, Down }

            [DebuggerDisplay("{X} {Y} {Direction}")]
            struct Point
            {
                public int X { get; set; }
                public int Y { get; set; }
                public Direction Direction { get; set; }

                public Point(int x, int y, Direction d=Direction.Skew)
                {
                    X = x;
                    Y = y;
                    Direction = d;
                }
            }

            class PointList : List<Point>
            { }

            Point[] _points;
            PresentationSource _source;
        }

        #endregion

        #region Data

        // pending ToolTip
        private ToolTip _pendingToolTip;
        private DispatcherTimer _pendingToolTipTimer;
        private ToolTip _sentinelToolTip;

        // current ToolTip
        private ToolTip _currentToolTip;
        private DispatcherTimer _currentToolTipTimer;
        private DispatcherTimer _forceCloseTimer;
        private Key _lastCtrlKeyDown;

        // ToolTip history
        private WeakRefWrapper<IInputElement> _lastMouseDirectlyOver;
        private WeakRefWrapper<DependencyObject> _lastMouseToolTipOwner;
        private bool _quickShow = false;        // true if a tool tip closed recently

        #endregion
    }
}


