// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Internal;
using MS.Internal.KnownBoxes;
using MS.Internal.Media;
using System;
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
            _focusChangedEventHandler = new KeyboardFocusChangedEventHandler(OnFocusChanged);
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

        private void OnFocusChanged(object sender, KeyboardFocusChangedEventArgs e)
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
                if ((e.SystemKey == Key.F10) && ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift) && ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control))
                {
                    e.Handled = OpenOrCloseToolTipViaShortcut();
                }
                else if ((e.SystemKey == Key.F10) && ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift))
                {
                    RaiseContextMenuOpeningEvent(e);
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
            }
        }

        #endregion

        #region ToolTip

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

        private void SetSafeArea(ToolTip tooltip)
        {
            CurrentSafeArea = null;     // default

            if (tooltip != null && !tooltip.FromKeyboard)
            {
                // get owner and tooltip rectangles, in owner window's client coords
                UIElement owner = GetOwner(tooltip) as UIElement;
                PresentationSource presentationSource = (owner == null) ? null : PresentationSource.CriticalFromVisual(owner);
                if (presentationSource != null)
                {
                    Rect rectElement = new Rect(new Point(0, 0), owner.RenderSize);
                    Rect rectRoot = PointUtil.ElementToRoot(rectElement, owner, presentationSource);
                    Rect ownerRect = PointUtil.RootToClient(rectRoot, presentationSource);

                    Rect screenRect = tooltip.GetScreenRect();
                    Point clientPt = PointUtil.ScreenToClient(screenRect.Location, presentationSource);
                    Rect tooltipRect = new Rect(clientPt, screenRect.Size);
                }
            }
        }

        private bool MouseHasLeftSafeArea(RawMouseInputReport mouseReport)
        {
            if (CurrentSafeArea == null)
                return false;

            // TODO:
            return false;
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
                    _quickShow = true;
                    CurrentToolTipTimer = new DispatcherTimer(DispatcherPriority.Normal);
                    CurrentToolTipTimer.Interval = TimeSpan.FromMilliseconds(ToolTipService.GetBetweenShowDelay(owner));
                    CurrentToolTipTimer.Tick += new EventHandler(OnBetweenShowDelay);
                    CurrentToolTipTimer.Start();
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

        private IInputElement LastMouseDirectlyOver
        {
            get { return _lastMouseDirectlyOver.GetValue(); }
            set { _lastMouseDirectlyOver.SetValue(value); }
        }

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

        internal ToolTip CurrentToolTip
        {
            get
            {
                return _currentToolTip;
            }
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

        private DispatcherTimer CurrentToolTipTimer
        {
            get { return _currentToolTipTimer; }
            set { _currentToolTipTimer = value; }
        }

        private DependencyObject LastMouseToolTipOwner
        {
            get { return _lastMouseToolTipOwner.GetValue(); }
            set { _lastMouseToolTipOwner.SetValue(value); }
        }

        private SafeArea CurrentSafeArea { get; set; }

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

        internal KeyboardFocusChangedEventHandler FocusChangedEventHandler
        {
            get
            {
                return _focusChangedEventHandler;
            }
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

        class SafeArea
        { }

        #endregion

        #region Data

        private KeyboardFocusChangedEventHandler _focusChangedEventHandler;

        // pending ToolTip
        private ToolTip _pendingToolTip;
        private DispatcherTimer _pendingToolTipTimer;
        private ToolTip _sentinelToolTip;

        // current ToolTip
        private ToolTip _currentToolTip;
        private DispatcherTimer _currentToolTipTimer;
        private DispatcherTimer _forceCloseTimer;

        // ToolTip history
        private WeakRefWrapper<IInputElement> _lastMouseDirectlyOver;
        private WeakRefWrapper<DependencyObject> _lastMouseToolTipOwner;
        private bool _quickShow = false;        // true if a tool tip closed recently

        #endregion
    }
}


