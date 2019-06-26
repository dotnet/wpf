// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Internal;
using MS.Internal.Interop;
using MS.Internal.KnownBoxes;
using MS.Internal.PresentationCore;
using MS.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Security;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using System.Windows.Threading;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows
{
    /// <summary>
    ///     ContentElement Class is a DependencyObject with IFE - input, focus, and events
    /// </summary>
    /// <ExternalAPI/>
    public partial class ContentElement : DependencyObject, IInputElement, IAnimatable
    {
        #region Construction

        /// <summary>
        ///     Static Constructor for ContentElement
        /// </summary>
        static ContentElement()
        {
            UIElement.RegisterEvents(typeof(ContentElement));
            RegisterProperties();

            UIElement.IsFocusedPropertyKey.OverrideMetadata(
                typeof(ContentElement),
                new PropertyMetadata(
                    BooleanBoxes.FalseBox, // default value
                    new PropertyChangedCallback(IsFocused_Changed)));
        }
                
        #endregion Construction

        #region DependencyObject

        #endregion

        /// <summary>
        /// Helper, gives the UIParent under control of which
        /// the OnMeasure or OnArrange are currently called.
        /// This may be implemented as a tree walk up until
        /// LayoutElement is found.
        /// </summary>
        internal DependencyObject GetUIParent()
        {
            return GetUIParent(false);
        }

        internal DependencyObject GetUIParent(bool continuePastVisualTree)
        {
            DependencyObject e = null;

            // Try to find a UIElement parent in the visual ancestry.
            e = InputElement.GetContainingInputElement(_parent) as DependencyObject;

            // If there was no InputElement parent in the visual ancestry,
            // check along the logical branch.
            if(e == null && continuePastVisualTree)
            {
                DependencyObject doParent = GetUIParentCore();
                e = InputElement.GetContainingInputElement(doParent) as DependencyObject;
            }

            return e;
        }

        /// <summary>
        ///     Called to get the UI parent of this element when there is
        ///     no visual parent.
        /// </summary>
        /// <returns>
        ///     Returns a non-null value when some framework implementation
        ///     of this method has a non-visual parent connection,
        /// </returns>
        protected virtual internal DependencyObject GetUIParentCore()
        {
            return null;
        }

        internal DependencyObject Parent
        {
            get
            {
                return _parent;
            }
        }

        /// <summary>
        /// OnContentParentChanged is called when the parent of the content element is changed.
        /// </summary>
        /// <param name="oldParent">Old parent or null if the content element did not have a parent before.</param>
        [FriendAccessAllowed] // Built into Core, also used by Framework.
        internal virtual void OnContentParentChanged(DependencyObject oldParent)
        {
            SynchronizeReverseInheritPropertyFlags(oldParent, true);
        }

        #region Automation

        /// <summary>
        /// Called by the Automation infrastructure when AutomationPeer
        /// is requested for this element. The element can return null or
        /// the instance of AutomationPeer-derived clas, if it supports UI Automation
        /// </summary>
        protected virtual AutomationPeer OnCreateAutomationPeer() { return null; }

        /// <summary>
        /// Called by the Automation infrastructure or Control author
        /// to make sure the AutomationPeer is created. The element may
        /// create AP or return null, depending on OnCreateAutomationPeer override.
        /// </summary>
        internal AutomationPeer CreateAutomationPeer()
        {
            VerifyAccess(); //this will ensure the AP is created in the right context

            AutomationPeer ap = null;

            if (HasAutomationPeer)
            {
                ap = AutomationPeerField.GetValue(this);
            }
            else
            {
                ap = OnCreateAutomationPeer();

                if (ap != null)
                {
                    AutomationPeerField.SetValue(this, ap);
                    HasAutomationPeer = true;
                }
            }
            return ap;
        }

        /// <summary>
        /// Returns AutomationPeer if one exists.
        /// The AutomationPeer may not exist if not yet created by Automation infrastructure
        /// or if this element is not supposed to have one.
        /// </summary>
        internal AutomationPeer GetAutomationPeer()
        {
            VerifyAccess();

            if (HasAutomationPeer)
                return AutomationPeerField.GetValue(this);

            return null;
        }

        // If this element is currently listening to synchronized input, add a pre-opportunity handler to keep track of event routed through this element.
        internal void AddSynchronizedInputPreOpportunityHandler(EventRoute route, RoutedEventArgs args)
        {
            if (InputManager.IsSynchronizedInput)
            {
                if (SynchronizedInputHelper.IsListening(this, args))
                {
                    RoutedEventHandler eventHandler = new RoutedEventHandler(this.SynchronizedInputPreOpportunityHandler);
                    SynchronizedInputHelper.AddHandlerToRoute(this, route, eventHandler, false);
                }
            }
        }

        // If this element is currently listening to synchronized input, add a handler to post process the synchronized input otherwise
        // add a synchronized input pre-opportunity handler from parent if parent is listening.
        internal void AddSynchronizedInputPostOpportunityHandler(EventRoute route, RoutedEventArgs args)
        {
            if (InputManager.IsSynchronizedInput)
            {
                if (SynchronizedInputHelper.IsListening(this, args))
                {
                    RoutedEventHandler eventHandler = new RoutedEventHandler(this.SynchronizedInputPostOpportunityHandler);
                    SynchronizedInputHelper.AddHandlerToRoute(this, route, eventHandler, true);
                }
                else
                {
                    // Add a preview handler from the parent.
                    SynchronizedInputHelper.AddParentPreOpportunityHandler(this, route, args);
                }
            }

        }

        // This event handler to be called before all the class & instance handlers for this element.
        internal void SynchronizedInputPreOpportunityHandler(object sender, RoutedEventArgs args)
        {
            if (!args.Handled)
            {
                SynchronizedInputHelper.PreOpportunityHandler(sender, args);
            }
        }
        // This event handler to be called after class & instance handlers for this element.
        internal void SynchronizedInputPostOpportunityHandler(object sender, RoutedEventArgs args)
        {
            if (args.Handled && (InputManager.SynchronizedInputState == SynchronizedInputStates.HadOpportunity))
            {
                SynchronizedInputHelper.PostOpportunityHandler(sender, args);
            }
        }


        // Called by automation peer, when called this element will be the listening element for synchronized input.   
        internal bool StartListeningSynchronizedInput(SynchronizedInputType inputType)
        {
            if (InputManager.IsSynchronizedInput)
            {
                return false;
            }
            else
            {
                InputManager.StartListeningSynchronizedInput(this, inputType);
                return true;
            }
        }

        // When called, input processing will return to normal mode.
        internal void CancelSynchronizedInput()
        {
            InputManager.CancelSynchronizedInput();
        }                     
        
        #endregion Automation

        #region Input

        /// <summary>
        ///     A property indicating if the mouse is over this element or not.
        /// </summary>
        public bool IsMouseDirectlyOver
        {
            get
            {
                // We do not return the cached value of reverse-inherited seed properties.
                //
                // The cached value is only used internally to detect a "change".
                //
                // More Info:
                // The act of invalidating the seed property of a reverse-inherited property
                // on the first side of the path causes the invalidation of the
                // reverse-inherited properties on both sides.  The input system has not yet
                // invalidated the seed property on the second side, so its cached value can
                // be incorrect.
                //
                return IsMouseDirectlyOver_ComputeValue();
            }
        }

        private bool IsMouseDirectlyOver_ComputeValue()
        {
            return (Mouse.DirectlyOver == this);
        }

        /// <summary>
        ///     Asynchronously re-evaluate the reverse-inherited properties.
        /// </summary>
        [FriendAccessAllowed]
        internal void SynchronizeReverseInheritPropertyFlags(DependencyObject oldParent, bool isCoreParent)
        {
            if(IsKeyboardFocusWithin)
            {
                Keyboard.PrimaryDevice.ReevaluateFocusAsync(this, oldParent, isCoreParent);
            }

            // Reevelauate the stylus properties first to guarentee that our property change
            // notifications fire before mouse properties.
            if(IsStylusOver)
            {
                StylusLogic.CurrentStylusLogicReevaluateStylusOver(this, oldParent, isCoreParent);
            }

            if(IsStylusCaptureWithin)
            {
                StylusLogic.CurrentStylusLogicReevaluateCapture(this, oldParent, isCoreParent);
            }

            if(IsMouseOver)
            {
                Mouse.PrimaryDevice.ReevaluateMouseOver(this, oldParent, isCoreParent);
            }

            if(IsMouseCaptureWithin)
            {
                Mouse.PrimaryDevice.ReevaluateCapture(this, oldParent, isCoreParent);
            }

            if (AreAnyTouchesOver)
            {
                TouchDevice.ReevaluateDirectlyOver(this, oldParent, isCoreParent);
            }

            if (AreAnyTouchesCapturedWithin)
            {
                TouchDevice.ReevaluateCapturedWithin(this, oldParent, isCoreParent);
            }
        }

        /// <summary>
            /// BlockReverseInheritance method when overriden stops reverseInheritProperties from updating their parent level properties.
        /// </summary>
        internal virtual bool BlockReverseInheritance()
        {
            return false;
        }

        /// <summary>
        ///     A property indicating if the mouse is over this element or not.
        /// </summary>
        public bool IsMouseOver
        {
            get
            {
                return ReadFlag(CoreFlags.IsMouseOverCache);
            }
        }

        /// <summary>
        ///     A property indicating if the stylus is over this element or not.
        /// </summary>
        public bool IsStylusOver
        {
            get
            {
                return ReadFlag(CoreFlags.IsStylusOverCache);
            }
        }

        /// <summary>
        ///     Indicates if Keyboard Focus is anywhere
        ///     within in the subtree starting at the
        ///     current instance
        /// </summary>
        public bool IsKeyboardFocusWithin
        {
            get
            {
                return ReadFlag(CoreFlags.IsKeyboardFocusWithinCache);
            }
        }

        /// <summary>
        ///     A property indicating if the mouse is captured to this element or not.
        /// </summary>
        public bool IsMouseCaptured
        {
            get { return (bool) GetValue(IsMouseCapturedProperty); }
        }

        /// <summary>
        ///     Captures the mouse to this element.
        /// </summary>
        public bool CaptureMouse()
        {
            return Mouse.Capture(this);
        }

        /// <summary>
        ///     Releases the mouse capture.
        /// </summary>
        public void ReleaseMouseCapture()
        {
            Mouse.Capture(null);
        }

        /// <summary>
        ///     Indicates if mouse capture is anywhere within in the subtree
        ///     starting at the current instance
        /// </summary>
        public bool IsMouseCaptureWithin
        {
            get
            {
                return ReadFlag(CoreFlags.IsMouseCaptureWithinCache);
            }
        }

        /// <summary>
        ///     A property indicating if the stylus is over this element or not.
        /// </summary>
        public bool IsStylusDirectlyOver
        {
            get
            {
                // We do not return the cached value of reverse-inherited seed properties.
                //
                // The cached value is only used internally to detect a "change".
                //
                // More Info:
                // The act of invalidating the seed property of a reverse-inherited property
                // on the first side of the path causes the invalidation of the
                // reverse-inherited properties on both sides.  The input system has not yet
                // invalidated the seed property on the second side, so its cached value can
                // be incorrect.
                //
                return IsStylusDirectlyOver_ComputeValue();
            }
        }

        private bool IsStylusDirectlyOver_ComputeValue()
        {
            return (Stylus.DirectlyOver == this);
        }

        /// <summary>
        ///     A property indicating if the stylus is captured to this element or not.
        /// </summary>
        public bool IsStylusCaptured
        {
            get { return (bool) GetValue(IsStylusCapturedProperty); }
        }

        /// <summary>
        ///     Captures the stylus to this element.
        /// </summary>
        public bool CaptureStylus()
        {
            return Stylus.Capture(this);
        }

        /// <summary>
        ///     Releases the stylus capture.
        /// </summary>
        public void ReleaseStylusCapture()
        {
            Stylus.Capture(null);
        }

        /// <summary>
        ///     Indicates if stylus capture is anywhere within in the subtree
        ///     starting at the current instance
        /// </summary>
        public bool IsStylusCaptureWithin
        {
            get
            {
                return ReadFlag(CoreFlags.IsStylusCaptureWithinCache);
            }
        }

        /// <summary>
        ///     A property indicating if the keyboard is focused on this
        ///     element or not.
        /// </summary>
        public bool IsKeyboardFocused
        {
            get
            {
                // We do not return the cached value of reverse-inherited seed properties.
                //
                // The cached value is only used internally to detect a "change".
                //
                // More Info:
                // The act of invalidating the seed property of a reverse-inherited property
                // on the first side of the path causes the invalidation of the
                // reverse-inherited properties on both sides.  The input system has not yet
                // invalidated the seed property on the second side, so its cached value can
                // be incorrect.
                //
                return IsKeyboardFocused_ComputeValue();
            }
        }

        private bool IsKeyboardFocused_ComputeValue()
        {
            return (Keyboard.FocusedElement == this);
        }

        /// <summary>
        ///     Focuses the keyboard on this element.
        /// </summary>
        public bool Focus()
        {
            if (Keyboard.Focus(this) == this)
            {
                
                // In order to show the touch keyboard we need to prompt the WinRT InputPane API.
                // We only do this when the keyboard focus has changed as the keyboard focus dictates
                // our current input targets for the touch and physical keyboards.
                TipTsfHelper.Show(this);

                return true;
            }

            return false;
        }

        /// <summary>
        ///     Request to move the focus from this element to another element
        /// </summary>
        /// <param name="request">Determine how to move the focus</param>
        /// <returns> Returns true if focus is moved successfully. Returns false if there is no next element</returns>
        public virtual bool MoveFocus(TraversalRequest request)
        {
            return false;
        }

        /// <summary>
        ///     Request to predict the element that should receive focus relative to this element for a
        /// given direction, without actually moving focus to it.
        /// </summary>
        /// <param name="direction">The direction for which focus should be predicted</param>
        /// <returns>
        ///     Returns the next element that focus should move to for a given FocusNavigationDirection.
        /// Returns null if focus cannot be moved relative to this element.
        /// </returns>
        public virtual DependencyObject PredictFocus(FocusNavigationDirection direction)
        {
            return null;
        }

        /// <summary>
        ///     GotFocus event
        /// </summary>
        public static readonly RoutedEvent GotFocusEvent = FocusManager.GotFocusEvent.AddOwner(typeof(ContentElement));

        /// <summary>
        ///     An event announcing that IsFocused changed to true.
        /// </summary>
        public event RoutedEventHandler GotFocus
        {
            add { AddHandler(GotFocusEvent, value); }
            remove { RemoveHandler(GotFocusEvent, value); }
        }

        /// <summary>
        ///     LostFocus event
        /// </summary>
        public static readonly RoutedEvent LostFocusEvent = FocusManager.LostFocusEvent.AddOwner(typeof(ContentElement));

        /// <summary>
        ///     An event announcing that IsFocused changed to false.
        /// </summary>
        public event RoutedEventHandler LostFocus
        {
            add { AddHandler(LostFocusEvent, value); }
            remove { RemoveHandler(LostFocusEvent, value); }
        }

        /// <summary>
        ///     The DependencyProperty for IsFocused.
        ///     Flags:              None
        ///     Read-Only:          true
        /// </summary>
        public static readonly DependencyProperty IsFocusedProperty =
                    UIElement.IsFocusedProperty.AddOwner(
                                typeof(ContentElement));

        private static void IsFocused_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ContentElement ce = ((ContentElement) d);

            if ((bool) e.NewValue)
            {
                ce.OnGotFocus(new RoutedEventArgs(GotFocusEvent, ce));
            }
            else
            {
                ce.OnLostFocus(new RoutedEventArgs(LostFocusEvent, ce));
            }
        }

        /// <summary>
        ///     This method is invoked when the IsFocused property changes to true
        /// </summary>
        /// <param name="e">RoutedEventArgs</param>
        protected virtual void OnGotFocus(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        ///     This method is invoked when the IsFocused property changes to false
        /// </summary>
        /// <param name="e">RoutedEventArgs</param>
        protected virtual void OnLostFocus(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        ///     Gettor for IsFocused Property
        /// </summary>
        public bool IsFocused
        {
            get { return (bool) GetValue(IsFocusedProperty); }
        }

        /// <summary>
        ///     The DependencyProperty for the IsEnabled property.
        /// </summary>
        public static readonly DependencyProperty IsEnabledProperty =
                    UIElement.IsEnabledProperty.AddOwner(
                                typeof(ContentElement),
                                new UIPropertyMetadata(
                                            BooleanBoxes.TrueBox, // default value
                                            new PropertyChangedCallback(OnIsEnabledChanged),
                                            new CoerceValueCallback(CoerceIsEnabled)));


        /// <summary>
        ///     A property indicating if this element is enabled or not.
        /// </summary>
        public bool IsEnabled
        {
            get { return (bool) GetValue(IsEnabledProperty); }
            set { SetValue(IsEnabledProperty, BooleanBoxes.Box(value)); }
        }

        /// <summary>
        ///     IsEnabledChanged event
        /// </summary>
        public event DependencyPropertyChangedEventHandler IsEnabledChanged
        {
            add { EventHandlersStoreAdd(UIElement.IsEnabledChangedKey, value); }
            remove { EventHandlersStoreRemove(UIElement.IsEnabledChangedKey, value); }
        }

        /// <summary>
        ///     Fetches the value that IsEnabled should be coerced to.
        /// </summary>
        /// <remarks>
        ///     This method is virtual is so that controls derived from UIElement
        ///     can combine additional requirements into the coersion logic.
        ///     <P/>
        ///     It is important for anyone overriding this property to also
        ///     call CoerceValue when any of their dependencies change.
        /// </remarks>
        protected virtual bool IsEnabledCore
        {
            get
            {
                // As of 1/25/2006, the following controls override this method:
                // Hyperlink.IsEnabledCore: CanExecute
                return true;
            }
        }

        private static object CoerceIsEnabled(DependencyObject d, object value)
        {
            ContentElement ce = (ContentElement) d;

            // We must be false if our parent is false, but we can be
            // either true or false if our parent is true.
            //
            // Another way of saying this is that we can only be true
            // if our parent is true, but we can always be false.
            if((bool) value)
            {
                // Use the "logical" parent.  This is different that UIElement, which
                // uses the visual parent.  But the "content parent" is not a complete
                // tree description (for instance, we don't track "content children"),
                // so the best we can do is use the logical tree for ContentElements.
                //
                // Note: we assume the "logical" parent of a ContentElement is either
                // a UIElement or ContentElement.  We explicitly assume that there
                // is never a raw Visual as the parent.

                DependencyObject parent = ce.GetUIParentCore();

                if(parent == null || (bool)parent.GetValue(IsEnabledProperty))
                {
                    return BooleanBoxes.Box(ce.IsEnabledCore);
                }
                else
                {
                    return BooleanBoxes.FalseBox;
                }
            }
            else
            {
                return BooleanBoxes.FalseBox;
            }
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ContentElement ce = (ContentElement)d;

            // Raise the public changed event.
            ce.RaiseDependencyPropertyChanged(UIElement.IsEnabledChangedKey, e);

            // Invalidate the children so that they will inherit the new value.
            ce.InvalidateForceInheritPropertyOnChildren(e.Property);

            // The input manager needs to re-hittest because something changed
            // that is involved in the hit-testing we do, so a different result
            // could be returned.
            InputManager.SafeCurrentNotifyHitTestInvalidated();
        }

        //*********************************************************************
        #region Focusable Property
        //*********************************************************************

        /// <summary>
        ///     The DependencyProperty for the Focusable property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty FocusableProperty =
                UIElement.FocusableProperty.AddOwner(
                        typeof(ContentElement),
                        new UIPropertyMetadata(
                                BooleanBoxes.FalseBox, // default value
                                new PropertyChangedCallback(OnFocusableChanged)));

        /// <summary>
        ///     Gettor and Settor for Focusable Property
        /// </summary>
        public bool Focusable
        {
            get { return (bool) GetValue(FocusableProperty); }
            set { SetValue(FocusableProperty, BooleanBoxes.Box(value)); }
        }

        /// <summary>
        ///     FocusableChanged event
        /// </summary>
        public event DependencyPropertyChangedEventHandler FocusableChanged
        {
            add {EventHandlersStoreAdd(UIElement.FocusableChangedKey, value);}
            remove {EventHandlersStoreRemove(UIElement.FocusableChangedKey, value);}
        }

        private static void OnFocusableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ContentElement ce = (ContentElement) d;

            // Raise the public changed event.
            ce.RaiseDependencyPropertyChanged(UIElement.FocusableChangedKey, e);
        }

        //*********************************************************************
        #endregion Focusable Property
        //*********************************************************************

        /// <summary>
        ///     A property indicating if the inptu method is enabled.
        /// </summary>
        public bool IsInputMethodEnabled
        {
            get { return (bool) GetValue(InputMethod.IsInputMethodEnabledProperty); }
        }


        #endregion Input

        #region Operations

        private void RaiseMouseButtonEvent(EventPrivateKey key, MouseButtonEventArgs e)
        {
            EventHandlersStore store = EventHandlersStore;
            if (store != null)
            {
                Delegate handler = store.Get(key);
                if (handler != null)
                {
                    ((MouseButtonEventHandler)handler)(this, e);
                }
            }
        }

        // Helper method to retrieve and fire Clr Event handlers for DependencyPropertyChanged event
        private void RaiseDependencyPropertyChanged(EventPrivateKey key, DependencyPropertyChangedEventArgs args)
        {
            EventHandlersStore store = EventHandlersStore;
            if (store != null)
            {
                Delegate handler = store.Get(key);
                if (handler != null)
                {
                    ((DependencyPropertyChangedEventHandler)handler)(this, args);
                }
            }
        }

        #endregion Operations

        #region AllowDrop

        /// <summary>
        ///     The DependencyProperty for the AllowDrop property.
        /// </summary>
        public static readonly DependencyProperty AllowDropProperty =
                    UIElement.AllowDropProperty.AddOwner(
                                typeof(ContentElement),
                                new PropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        ///     A dependency property that allows the drop object as DragDrop target.
        /// </summary>
        public bool AllowDrop
        {
            get { return (bool) GetValue(AllowDropProperty); }
            set { SetValue(AllowDropProperty, BooleanBoxes.Box(value)); }
        }

        #endregion AllowDrop

        #region ForceInherit property support

        // This has to be virtual, since there is no concept of "core" content children,
        // so we have no choice by to rely on FrameworkContentElement to use logical
        // children instead.
        internal virtual void InvalidateForceInheritPropertyOnChildren(DependencyProperty property)
        {
        }

        #endregion

        #region Touch

        /// <summary>
        ///     A property indicating if any touch devices are over this element or not.
        /// </summary>
        public bool AreAnyTouchesOver
        {
            get { return ReadFlag(CoreFlags.TouchesOverCache); }
        }

        /// <summary>
        ///     A property indicating if any touch devices are directly over this element or not.
        /// </summary>
        public bool AreAnyTouchesDirectlyOver
        {
            get { return (bool)GetValue(AreAnyTouchesDirectlyOverProperty); }
        }

        /// <summary>
        ///     A property indicating if any touch devices are captured to elements in this subtree.
        /// </summary>
        public bool AreAnyTouchesCapturedWithin
        {
            get { return ReadFlag(CoreFlags.TouchesCapturedWithinCache); }
        }

        /// <summary>
        ///     A property indicating if any touch devices are captured to this element.
        /// </summary>
        public bool AreAnyTouchesCaptured
        {
            get { return (bool)GetValue(AreAnyTouchesCapturedProperty); }
        }

        /// <summary>
        ///     Captures the specified device to this element.
        /// </summary>
        /// <param name="touchDevice">The touch device to capture.</param>
        /// <returns>True if capture was taken.</returns>
        public bool CaptureTouch(TouchDevice touchDevice)
        {
            if (touchDevice == null)
            {
                throw new ArgumentNullException("touchDevice");
            }

            return touchDevice.Capture(this);
        }

        /// <summary>
        ///     Releases capture from the specified touch device.
        /// </summary>
        /// <param name="touchDevice">The device that is captured to this element.</param>
        /// <returns>true if capture was released, false otherwise.</returns>
        public bool ReleaseTouchCapture(TouchDevice touchDevice)
        {
            if (touchDevice == null)
            {
                throw new ArgumentNullException("touchDevice");
            }

            if (touchDevice.Captured == this)
            {
                touchDevice.Capture(null);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        ///     Releases capture on any touch devices captured to this element.
        /// </summary>
        public void ReleaseAllTouchCaptures()
        {
            TouchDevice.ReleaseAllCaptures(this);
        }

        /// <summary>
        ///     The touch devices captured to this element.
        /// </summary>
        public IEnumerable<TouchDevice> TouchesCaptured
        {
            get
            {
                return TouchDevice.GetCapturedTouches(this, /* includeWithin = */ false);
            }
        }

        /// <summary>
        ///     The touch devices captured to this element and any elements in the subtree.
        /// </summary>
        public IEnumerable<TouchDevice> TouchesCapturedWithin
        {
            get
            {
                return TouchDevice.GetCapturedTouches(this, /* includeWithin = */ true);
            }
        }

        /// <summary>
        ///     The touch devices which are over this element and any elements in the subtree.
        ///     This is particularly relevant to elements which dont take capture (like Label).
        /// </summary>
        public IEnumerable<TouchDevice> TouchesOver
        {
            get
            {
                return TouchDevice.GetTouchesOver(this, /* includeWithin = */ true);
            }
        }

        /// <summary>
        ///     The touch devices which are directly over this element.
        ///     This is particularly relevant to elements which dont take capture (like Label).
        /// </summary>
        public IEnumerable<TouchDevice> TouchesDirectlyOver
        {
            get
            {
                return TouchDevice.GetTouchesOver(this, /* includeWithin = */ false);
            }
        }

        #endregion

        internal bool HasAutomationPeer
        {
            get { return ReadFlag(CoreFlags.HasAutomationPeer); }
            set { WriteFlag(CoreFlags.HasAutomationPeer, value); }
        }

        #region Data

        internal DependencyObject _parent;

        ///// ATTACHED STORAGE /////
        internal static readonly UncommonField<EventHandlersStore> EventHandlersStoreField = UIElement.EventHandlersStoreField;
        internal static readonly UncommonField<InputBindingCollection> InputBindingCollectionField = UIElement.InputBindingCollectionField;
        internal static readonly UncommonField<CommandBindingCollection> CommandBindingCollectionField = UIElement.CommandBindingCollectionField;
        private static readonly UncommonField<AutomationPeer> AutomationPeerField = new UncommonField<AutomationPeer>();

        // Caches the ContentElement's DependencyObjectType
        private static DependencyObjectType ContentElementType = DependencyObjectType.FromSystemTypeInternal(typeof(ContentElement));
      
        #endregion Data
    }
}
