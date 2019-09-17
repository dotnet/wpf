// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using MS.Internal;
using MS.Internal.Interop;
using MS.Internal.KnownBoxes;
using MS.Internal.Media;
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
using System.Windows.Input.StylusPlugIns;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows.Media.Effects;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace System.Windows
{
    /// <summary>
    /// UIElement3D is the base class for frameworks building on the Windows Presentation Core.
    /// </summary>
    /// <remarks>
    /// UIElement3D adds to the base visual class "IFE" - Input, Focus, and Eventing.
    /// </remarks>
    public abstract partial class UIElement3D : Visual3D, IInputElement
    {
        static UIElement3D()
        {
            UIElement.RegisterEvents(typeof(UIElement3D));

            // add owner and then override metadata for IsVisibile
            IsVisibleProperty = UIElement.IsVisibleProperty.AddOwner(typeof(UIElement3D));

            _isVisibleMetadata = new ReadOnlyPropertyMetadata(BooleanBoxes.FalseBox,
                                                              new GetReadOnlyValueCallback(GetIsVisible),
                                                              new PropertyChangedCallback(OnIsVisibleChanged));

            IsVisibleProperty.OverrideMetadata(typeof(UIElement3D),
                                               _isVisibleMetadata,
                                               UIElement.IsVisiblePropertyKey);

            // add owner and then override metadta for IsFocused
            IsFocusedProperty = UIElement.IsFocusedProperty.AddOwner(typeof(UIElement3D));
            IsFocusedProperty.OverrideMetadata(typeof(UIElement3D),
                                                new PropertyMetadata(
                                                    BooleanBoxes.FalseBox, // default value
                                                    new PropertyChangedCallback(IsFocused_Changed)),
                                               UIElement.IsFocusedPropertyKey);
        }

        /// <summary>
        /// Constructor. This form of constructor will encounter a slight perf hit since it needs to initialize Dispatcher for the instance.
        /// </summary>
        protected UIElement3D()
        {
            _children = new Visual3DCollection(this);

            Initialize();
        }

        private void Initialize()
        {
            BeginPropertyInitialization();

            VisibilityCache = (Visibility)VisibilityProperty.GetDefaultValue(DependencyObjectType);

            // Note: IsVisibleCache is false by default.

            // to cause the first render to occur
            InvalidateModel();
        }

        #region AllowDrop

        /// <summary>
        ///     The DependencyProperty for the AllowDrop property.
        /// </summary>
        public static readonly DependencyProperty AllowDropProperty =
                    UIElement.AllowDropProperty.AddOwner(
                                typeof(UIElement3D),
                                new PropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        ///     A dependency property that allows the drop object as DragDrop target.
        /// </summary>
        public bool AllowDrop
        {
            get { return (bool)GetValue(AllowDropProperty); }
            set { SetValue(AllowDropProperty, BooleanBoxes.Box(value)); }
        }

        #endregion AllowDrop

        private object CallRenderCallback(object o)
        {
            OnUpdateModel();

            _renderRequestPosted = false;

            return null;
        }

        /// <summary>
        /// Invalidates the rendering of the element.
        /// Causes <see cref="System.Windows.UIElement3D.OnUpdateModel"/> to be called at a later time.
        /// </summary>
        public void InvalidateModel()
        {
            if (!_renderRequestPosted)
            {
                MediaContext.From(Dispatcher).BeginInvokeOnRender(CallRenderCallback, this);
                _renderRequestPosted = true;
            }
        }

        /// <summary>
        ///      This class provides a point for subclasses to access the protected content of the
        ///      UIElement to set what the geometry they should render should be.
        /// </summary>
        protected virtual void OnUpdateModel()
        {
        }

        /// <summary>
        /// OnVisualParentChanged is called when the parent of the Visual3D is changed.
        /// </summary>
        /// <param name="oldParent">Old parent or null if the Visual3D did not have a parent before.</param>
        protected internal override void OnVisualParentChanged(DependencyObject oldParent)
        {
            // Synchronize ForceInherit properties
            if (InternalVisualParent != null)
            {
                DependencyObject parent = InternalVisualParent;

                if (!InputElement.IsUIElement(parent) && !InputElement.IsUIElement3D(parent))
                {
                    Visual parentAsVisual = parent as Visual;

                    if (parentAsVisual != null)
                    {
                        // We are being plugged into a non-UIElement visual. This
                        // means that our parent doesn't play by the same rules we
                        // do, so we need to track changes to our ancestors in
                        // order to bridge the gap.
                        parentAsVisual.VisualAncestorChanged += new Visual.AncestorChangedEventHandler(OnVisualAncestorChanged_ForceInherit);
                    }
                    else
                    {
                        Visual3D parentAsVisual3D = parent as Visual3D;

                        if (parentAsVisual3D != null)
                        {
                            // We are being plugged into a non-UIElement visual. This
                            // means that our parent doesn't play by the same rules we
                            // do, so we need to track changes to our ancestors in
                            // order to bridge the gap.
                            parentAsVisual3D.VisualAncestorChanged += new Visual.AncestorChangedEventHandler(OnVisualAncestorChanged_ForceInherit);
                        }
                    }

                    // find a UIElement(3D) ancestor to use for coersion
                    parent = InputElement.GetContainingUIElement(parentAsVisual);
                }

                if (parent != null)
                {
                    UIElement.SynchronizeForceInheritProperties(null, null, this, parent);
                }
                else
                {
                    // We don't have a UIElement parent or ancestor, so no
                    // coersions are necessary at this time.
                }
            }
            else
            {
                DependencyObject parent = oldParent;

                if (!InputElement.IsUIElement(parent) && !InputElement.IsUIElement3D(parent))
                {
                    // We are being unplugged from a non-UIElement visual. This
                    // means that our parent didn't play by the same rules we
                    // do, so we started track changes to our ancestors in
                    // order to bridge the gap.  Now we can stop.
                    if (oldParent is Visual)
                    {
                        ((Visual)oldParent).VisualAncestorChanged -= new Visual.AncestorChangedEventHandler(OnVisualAncestorChanged_ForceInherit);
                    }
                    else if (oldParent is Visual3D)
                    {
                        ((Visual3D)oldParent).VisualAncestorChanged -= new Visual.AncestorChangedEventHandler(OnVisualAncestorChanged_ForceInherit);
                    }

                    // Try to find a UIElement ancestor to use for coersion.
                    parent = InputElement.GetContainingUIElement(oldParent);
                }

                if (parent != null)
                {
                    UIElement.SynchronizeForceInheritProperties(null, null, this, parent);
                }
                else
                {
                    // We don't have a UIElement parent or ancestor, so no
                    // coersions are necessary at this time.
                }
            }

            // Synchronize ReverseInheritProperty Flags
            //
            // NOTE: do this AFTER synchronizing force-inherited flags, since
            // they often effect focusability and such.
            this.SynchronizeReverseInheritPropertyFlags(oldParent, true);
        }

        private void OnVisualAncestorChanged_ForceInherit(object sender, AncestorChangedEventArgs e)
        {
            // NOTE:
            //
            // We are forced to listen to AncestorChanged events because
            // a UIElement may have raw Visuals between it and its nearest
            // UIElement parent.  We only care about changes that happen
            // to the visual tree BETWEEN this UIElement and its nearest
            // UIElement parent.  This is because we can rely on our
            // nearest UIElement parent to notify us when its force-inherit
            // properties change.

            DependencyObject parent = null;
            if (e.OldParent == null)
            {
                // We were plugged into something.

                // Find our nearest UIElement parent.
                parent = InputElement.GetContainingUIElement(InternalVisualParent);

                // See if this parent is a child of the ancestor who's parent changed.
                // If so, we don't care about changes that happen above us.
                if (parent != null && VisualTreeHelper.IsAncestorOf(e.Ancestor, parent))
                {
                    parent = null;
                }
            }
            else
            {
                // we were unplugged from something.

                // Find our nearest UIElement parent.
                parent = InputElement.GetContainingUIElement(InternalVisualParent);

                if (parent != null)
                {
                    // If we found a UIElement parent in our subtree, the
                    // break in the visual tree must have been above it,
                    // so we don't need to respond.
                    parent = null;
                }
                else
                {
                    // There was no UIElement parent in our subtree, so we
                    // may be detaching from some UIElement parent above
                    // the break point in the tree.
                    parent = InputElement.GetContainingUIElement(e.OldParent);
                }
            }

            if (parent != null)
            {
                UIElement.SynchronizeForceInheritProperties(null, null, this, parent);
            }
        }

        internal void OnVisualAncestorChanged(object sender, AncestorChangedEventArgs e)
        {
            UIElement3D uie3D = sender as UIElement3D;
            if (null != uie3D)
                PresentationSource.OnVisualAncestorChanged(uie3D, e);
        }

        internal DependencyObject GetUIParent(bool continuePastVisualTree)
        {
            return UIElementHelper.GetUIParent(this, continuePastVisualTree);
        }

        /// <summary>
        ///     Called to get the UI parent of this element when there is
        ///     no visual parent.
        /// </summary>
        /// <returns>
        ///     Returns a non-null value when some framework implementation
        ///     of this method has a non-visual parent connection,
        /// </returns>
        protected internal DependencyObject GetUIParentCore()
        {
            // When we get FrameworkElement3D make this function virtual.  See
            // UIElement for the similar usage.
            return null;
        }


        #region LoadedAndUnloadedEvents

        ///<summary>
        ///     Initiate the processing for [Un]Loaded event broadcast starting at this node
        /// </summary>
        internal virtual void OnPresentationSourceChanged(bool attached)
        {
            // Reset the FocusedElementProperty in order to get LostFocus event
            if (!attached && FocusManager.GetFocusedElement(this) != null)
                FocusManager.SetFocusedElement(this, null);
        }

        #endregion LoadedAndUnloadedEvents

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

        #region new
        /// <summary>
        ///     Asynchronously re-evaluate the reverse-inherited properties.
        /// </summary>
        [FriendAccessAllowed]
        internal void SynchronizeReverseInheritPropertyFlags(DependencyObject oldParent, bool isCoreParent)
        {
            if (IsKeyboardFocusWithin)
            {
                Keyboard.PrimaryDevice.ReevaluateFocusAsync(this, oldParent, isCoreParent);
            }

            // Reevelauate the stylus properties first to guarentee that our property change
            // notifications fire before mouse properties.
            if (IsStylusOver)
            {
                StylusLogic.CurrentStylusLogicReevaluateStylusOver(this, oldParent, isCoreParent);
            }

            if (IsStylusCaptureWithin)
            {
                StylusLogic.CurrentStylusLogicReevaluateCapture(this, oldParent, isCoreParent);
            }

            if (IsMouseOver)
            {
                Mouse.PrimaryDevice.ReevaluateMouseOver(this, oldParent, isCoreParent);
            }

            if (IsMouseCaptureWithin)
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
        ///     Controls like popup want to control updating parent properties. This method
        ///     provides an opportunity for those controls to participate and block it.
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

        #endregion new

        /// <summary>
        ///     A property indicating if the mouse is captured to this element or not.
        /// </summary>
        public bool IsMouseCaptured
        {
            get { return (bool)GetValue(IsMouseCapturedProperty); }
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
            if (Mouse.Captured == this)
            {
                Mouse.Capture(null);
            }
        }

        /// <summary>
        ///     Indicates if mouse capture is anywhere within the subtree
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
            get { return (bool)GetValue(IsStylusCapturedProperty); }
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
        ///     Indicates if stylus capture is anywhere within the subtree
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
        ///     Set a logical focus on the element. If the current keyboard focus is within the same scope move the keyboard on this element.
        /// </summary>
        public bool Focus()
        {
            if (Keyboard.Focus(this) == this)
            {
                
                // In order to show the touch keyboard we need to prompt the WinRT InputPane API.
                // We only do this when the keyboard focus has changed as the keyboard focus dictates
                // our current input targets for the touch and physical keyboards.
                TipTsfHelper.Show(this);

                // Successfully setting the keyboard focus updated the logical focus as well
                return true;
            }

            if (Focusable && IsEnabled)
            {
                // If we cannot set keyboard focus then set the logical focus only
                // Find element's FocusScope and set its FocusedElement if not already set
                // If FocusedElement is already set we don't want to steal focus for that scope
                DependencyObject focusScope = FocusManager.GetFocusScope(this);
                if (FocusManager.GetFocusedElement(focusScope) == null)
                {
                    FocusManager.SetFocusedElement(focusScope, (IInputElement)this);
                }
            }

            // Return false because current KeyboardFocus is not set on the element - only the logical focus is set
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
        ///     The access key for this element was invoked. Base implementation sets focus to the element.
        /// </summary>
        /// <param name="e">The arguments to the access key event</param>
        protected virtual void OnAccessKey(AccessKeyEventArgs e)
        {
            this.Focus();
        }

        /// <summary>
        ///     A property indicating if the inptu method is enabled.
        /// </summary>
        public bool IsInputMethodEnabled
        {
            get { return (bool)GetValue(InputMethod.IsInputMethodEnabledProperty); }
        }

        /// <summary>
        ///     The Visibility property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty VisibilityProperty =
                            UIElement.VisibilityProperty.AddOwner(
                                typeof(UIElement3D),
                                new PropertyMetadata(
                                    VisibilityBoxes.VisibleBox,
                                    new PropertyChangedCallback(OnVisibilityChanged)));

        private static void OnVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement3D uie = (UIElement3D)d;

            Visibility newVisibility = (Visibility)e.NewValue;
            uie.VisibilityCache = newVisibility;
            uie.switchVisibilityIfNeeded(newVisibility);

            // The IsVisible property depends on this property.
            uie.UpdateIsVisibleCache();
        }

        private static bool ValidateVisibility(object o)
        {
            Visibility value = (Visibility)o;
            return (value == Visibility.Visible) || (value == Visibility.Hidden) || (value == Visibility.Collapsed);
        }

        /// <summary>
        ///     Visibility accessor
        /// </summary>
        [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
        public Visibility Visibility
        {
            get { return VisibilityCache; }
            set { SetValue(VisibilityProperty, VisibilityBoxes.Box(value)); }
        }

        private void switchVisibilityIfNeeded(Visibility visibility)
        {
            switch (visibility)
            {
                case Visibility.Visible:
                    ensureVisible();
                    break;

                case Visibility.Hidden:
                    ensureInvisible(false);
                    break;

                case Visibility.Collapsed:
                    ensureInvisible(true);
                    break;
            }
        }

        private void ensureVisible()
        {
            InternalIsVisible = true;
        }

        private void ensureInvisible(bool collapsed)
        {
            InternalIsVisible = false;

            if (!ReadFlag(CoreFlags.IsCollapsed) && collapsed) //Hidden or Visible->Collapsed
            {
                WriteFlag(CoreFlags.IsCollapsed, true);
            }
            else if (ReadFlag(CoreFlags.IsCollapsed) && !collapsed) //Collapsed -> Hidden
            {
                WriteFlag(CoreFlags.IsCollapsed, false);
            }
        }

        // Internal accessor for AccessKeyManager class
        internal void InvokeAccessKey(AccessKeyEventArgs e)
        {
            OnAccessKey(e);
        }

        /// <summary>
        ///     GotFocus event
        /// </summary>
        public static readonly RoutedEvent GotFocusEvent = FocusManager.GotFocusEvent.AddOwner(typeof(UIElement3D));

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
        public static readonly RoutedEvent LostFocusEvent = FocusManager.LostFocusEvent.AddOwner(typeof(UIElement3D));

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
        public static readonly DependencyProperty IsFocusedProperty;

        private static void IsFocused_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement3D uiElement3D = ((UIElement3D)d);

            if ((bool)e.NewValue)
            {
                uiElement3D.OnGotFocus(new RoutedEventArgs(GotFocusEvent, uiElement3D));
            }
            else
            {
                uiElement3D.OnLostFocus(new RoutedEventArgs(LostFocusEvent, uiElement3D));
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
            get { return (bool)GetValue(IsFocusedProperty); }
        }

        //*********************************************************************
        #region IsEnabled Property
        //*********************************************************************

        /// <summary>
        ///     The DependencyProperty for the IsEnabled property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty IsEnabledProperty =
                    UIElement.IsEnabledProperty.AddOwner(
                                typeof(UIElement3D),
                                new UIPropertyMetadata(
                                            BooleanBoxes.TrueBox, // default value
                                            new PropertyChangedCallback(OnIsEnabledChanged),
                                            new CoerceValueCallback(CoerceIsEnabled)));


        /// <summary>
        ///     A property indicating if this element is enabled or not.
        /// </summary>
        public bool IsEnabled
        {
            get { return (bool)GetValue(IsEnabledProperty); }
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
        ///     This method is virtual is so that controls derived from UIElement3D
        ///     can combine additional requirements into the coersion logic.
        ///     <P/>
        ///     It is important for anyone overriding this property to also
        ///     call CoerceValue when any of their dependencies change.
        /// </remarks>
        protected virtual bool IsEnabledCore
        {
            get
            {
                return true;
            }
        }

        private static object CoerceIsEnabled(DependencyObject d, object value)
        {
            UIElement3D uie = (UIElement3D)d;

            // We must be false if our parent is false, but we can be
            // either true or false if our parent is true.
            //
            // Another way of saying this is that we can only be true
            // if our parent is true, but we can always be false.

            if ((bool)value)
            {
                // Our parent can constrain us.  We can be plugged into either
                // a "visual" or "content" tree.  If we are plugged into a
                // "content" tree, the visual tree is just considered a
                // visual representation, and is normally composed of raw
                // visuals, not UIElement3Ds, so we prefer the content tree.
                //
                // The content tree uses the "logical" links.  But not all
                // "logical" links lead to a content tree.
                //
                DependencyObject parent = uie.GetUIParentCore() as ContentElement;
                if (parent == null)
                {
                    parent = InputElement.GetContainingUIElement(uie.InternalVisualParent);
                }

                if (parent == null || (bool)parent.GetValue(UIElement.IsEnabledProperty))
                {
                    return BooleanBoxes.Box(uie.IsEnabledCore);
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
            UIElement3D uie = (UIElement3D)d;

            // Raise the public changed event.
            uie.RaiseDependencyPropertyChanged(UIElement.IsEnabledChangedKey, e);

            // Invalidate the children so that they will inherit the new value.
            uie.InvalidateForceInheritPropertyOnChildren(e.Property);

            // The input manager needs to re-hittest because something changed
            // that is involved in the hit-testing we do, so a different result
            // could be returned.
            InputManager.SafeCurrentNotifyHitTestInvalidated();

            //Notify Automation in case it is interested.
            AutomationPeer peer = uie.GetAutomationPeer();
            if (peer != null)
                peer.InvalidatePeer();

        }


        //*********************************************************************
        #endregion IsEnabled Property
        //*********************************************************************

        //*********************************************************************
        #region IsHitTestVisible Property
        //*********************************************************************

        /// <summary>
        ///     The DependencyProperty for the IsHitTestVisible property.
        /// </summary>
        public static readonly DependencyProperty IsHitTestVisibleProperty =
                        UIElement.IsHitTestVisibleProperty.AddOwner(
                                typeof(UIElement3D),
                                new UIPropertyMetadata(
                                            BooleanBoxes.TrueBox, // default value
                                            new PropertyChangedCallback(OnIsHitTestVisibleChanged),
                                            new CoerceValueCallback(CoerceIsHitTestVisible)));

        /// <summary>
        ///     A property indicating if this element is hit test visible or not.
        /// </summary>
        public bool IsHitTestVisible
        {
            get { return (bool)GetValue(IsHitTestVisibleProperty); }
            set { SetValue(IsHitTestVisibleProperty, BooleanBoxes.Box(value)); }
        }

        /// <summary>
        ///     IsHitTestVisibleChanged event
        /// </summary>
        public event DependencyPropertyChangedEventHandler IsHitTestVisibleChanged
        {
            add { EventHandlersStoreAdd(IsHitTestVisibleChangedKey, value); }
            remove { EventHandlersStoreRemove(IsHitTestVisibleChangedKey, value); }
        }
        internal static readonly EventPrivateKey IsHitTestVisibleChangedKey = new EventPrivateKey(); // Used by ContentElement

        private static object CoerceIsHitTestVisible(DependencyObject d, object value)
        {
            UIElement3D uie = (UIElement3D)d;

            // We must be false if our parent is false, but we can be
            // either true or false if our parent is true.
            //
            // Another way of saying this is that we can only be true
            // if our parent is true, but we can always be false.
            if ((bool)value)
            {
                // Our parent can constrain us.  We can be plugged into either
                // a "visual" or "content" tree.  If we are plugged into a
                // "content" tree, the visual tree is just considered a
                // visual representation, and is normally composed of raw
                // visuals, not UIElements, so we prefer the content tree.
                //
                // The content tree uses the "logical" links.  But not all
                // "logical" links lead to a content tree.
                //
                // However, ContentElements don't understand IsHitTestVisible,
                // so we ignore them.
                //
                DependencyObject parent = InputElement.GetContainingUIElement(uie.InternalVisualParent);

                if (parent == null || UIElementHelper.IsHitTestVisible(parent))
                {
                    return BooleanBoxes.TrueBox;
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

        private static void OnIsHitTestVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement3D uie = (UIElement3D)d;

            // Raise the public changed event.
            uie.RaiseDependencyPropertyChanged(IsHitTestVisibleChangedKey, e);

            // Invalidate the children so that they will inherit the new value.
            uie.InvalidateForceInheritPropertyOnChildren(e.Property);

            // The input manager needs to re-hittest because something changed
            // that is involved in the hit-testing we do, so a different result
            // could be returned.
            InputManager.SafeCurrentNotifyHitTestInvalidated();
        }


        //*********************************************************************
        #endregion IsHitTestVisible Property
        //*********************************************************************

        //*********************************************************************
        #region IsVisible Property
        //*********************************************************************

        // both _isVisibleMetadata and IsVisibileProperty are constructed in UIElement3D's static constructor
        private static PropertyMetadata _isVisibleMetadata;

        /// <summary>
        ///     The DependencyProperty for the IsVisible property.
        /// </summary>
        public static readonly DependencyProperty IsVisibleProperty;

        /// <summary>
        ///     A property indicating if this element is Visible or not.
        /// </summary>
        public bool IsVisible
        {
            get { return ReadFlag(CoreFlags.IsVisibleCache); }
        }

        private static object GetIsVisible(DependencyObject d, out BaseValueSourceInternal source)
        {
            source = BaseValueSourceInternal.Local;
            return ((UIElement3D)d).IsVisible ? BooleanBoxes.TrueBox : BooleanBoxes.FalseBox;
        }

        /// <summary>
        ///     IsVisibleChanged event
        /// </summary>
        public event DependencyPropertyChangedEventHandler IsVisibleChanged
        {
            add { EventHandlersStoreAdd(UIElement.IsVisibleChangedKey, value); }
            remove { EventHandlersStoreRemove(UIElement.IsVisibleChangedKey, value); }
        }
        internal static readonly EventPrivateKey IsVisibleChangedKey = new EventPrivateKey(); // Used by ContentElement

        internal void UpdateIsVisibleCache() // Called from PresentationSource
        {
            // IsVisible is a read-only property.  It derives its "base" value
            // from the Visibility property.
            bool isVisible = (Visibility == Visibility.Visible);

            // We must be false if our parent is false, but we can be
            // either true or false if our parent is true.
            //
            // Another way of saying this is that we can only be true
            // if our parent is true, but we can always be false.
            if (isVisible)
            {
                bool constraintAllowsVisible = false;

                // Our parent can constrain us.  We can be plugged into either
                // a "visual" or "content" tree.  If we are plugged into a
                // "content" tree, the visual tree is just considered a
                // visual representation, and is normally composed of raw
                // visuals, not UIElements, so we prefer the content tree.
                //
                // The content tree uses the "logical" links.  But not all
                // "logical" links lead to a content tree.
                //
                // However, ContentElements don't understand IsVisible,
                // so we ignore them.
                //
                DependencyObject parent = InputElement.GetContainingUIElement(InternalVisualParent);

                if (parent != null)
                {
                    constraintAllowsVisible = UIElementHelper.IsVisible(parent);
                }
                else
                {
                    // We cannot be visible if we have no visual parent, unless:
                    // 1) We are the root, connected to a PresentationHost.
                    PresentationSource presentationSource = PresentationSource.CriticalFromVisual(this);
                    if (presentationSource != null)
                    {
                        constraintAllowsVisible = true;
                    }
                    else
                    {
                        // What if We are the root of a VisualBrush?  How can we tell?
                    }

                }

                if (!constraintAllowsVisible)
                {
                    isVisible = false;
                }
            }

            if (isVisible != IsVisible)
            {
                // Our IsVisible force-inherited property has changed.  Update our
                // cache and raise a change notification.

                WriteFlag(CoreFlags.IsVisibleCache, isVisible);
                NotifyPropertyChange(new DependencyPropertyChangedEventArgs(IsVisibleProperty, _isVisibleMetadata, !isVisible, isVisible));
            }
        }

        private static void OnIsVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement3D uie = (UIElement3D)d;

            // Raise the public changed event.
            uie.RaiseDependencyPropertyChanged(IsVisibleChangedKey, e);

            // Invalidate the children so that they will inherit the new value.
            uie.InvalidateForceInheritPropertyOnChildren(e.Property);

            // The input manager needs to re-hittest because something changed
            // that is involved in the hit-testing we do, so a different result
            // could be returned.
            InputManager.SafeCurrentNotifyHitTestInvalidated();
        }

        //*********************************************************************
        #endregion IsVisible Property
        //*********************************************************************


        //*********************************************************************
        #region Focusable Property
        //*********************************************************************

        /// <summary>
        ///     The DependencyProperty for the Focusable property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty FocusableProperty =
                UIElement.FocusableProperty.AddOwner(
                        typeof(UIElement3D),
                         new UIPropertyMetadata(
                                BooleanBoxes.FalseBox, // default value
                                new PropertyChangedCallback(OnFocusableChanged)));

        /// <summary>
        ///     Gettor and Settor for Focusable Property
        /// </summary>
        public bool Focusable
        {
            get { return (bool)GetValue(FocusableProperty); }
            set { SetValue(FocusableProperty, BooleanBoxes.Box(value)); }
        }

        /// <summary>
        ///     FocusableChanged event
        /// </summary>
        public event DependencyPropertyChangedEventHandler FocusableChanged
        {
            add { EventHandlersStoreAdd(UIElement.FocusableChangedKey, value); }
            remove { EventHandlersStoreRemove(UIElement.FocusableChangedKey, value); }
        }

        private static void OnFocusableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement3D uie = (UIElement3D)d;

            // Raise the public changed event.
            uie.RaiseDependencyPropertyChanged(UIElement.FocusableChangedKey, e);
        }

        //*********************************************************************
        #endregion Focusable Property
        //*********************************************************************

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

        // Cache for the Visibility property.  Storage is in Visual._nodeProperties.
        private Visibility VisibilityCache
        {
            get
            {
                if (CheckFlagsAnd(VisualFlags.VisibilityCache_Visible))
                {
                    return Visibility.Visible;
                }
                else if (CheckFlagsAnd(VisualFlags.VisibilityCache_TakesSpace))
                {
                    return Visibility.Hidden;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
            set
            {
                Debug.Assert(value == Visibility.Visible || value == Visibility.Hidden || value == Visibility.Collapsed);

                switch (value)
                {
                    case Visibility.Visible:
                        SetFlags(true, VisualFlags.VisibilityCache_Visible);
                        SetFlags(false, VisualFlags.VisibilityCache_TakesSpace);
                        break;

                    case Visibility.Hidden:
                        SetFlags(false, VisualFlags.VisibilityCache_Visible);
                        SetFlags(true, VisualFlags.VisibilityCache_TakesSpace);
                        break;

                    case Visibility.Collapsed:
                        SetFlags(false, VisualFlags.VisibilityCache_Visible);
                        SetFlags(false, VisualFlags.VisibilityCache_TakesSpace);
                        break;
                }
            }
        }

        #region ForceInherit property support

        // This is called from the force-inherit property changed events.
        internal static void InvalidateForceInheritPropertyOnChildren(Visual3D v, DependencyProperty property)
        {
            int cChildren = v.InternalVisual2DOr3DChildrenCount;
            for (int iChild = 0; iChild < cChildren; iChild++)
            {
                DependencyObject vChild = v.InternalGet2DOr3DVisualChild(iChild);
                if (vChild != null)
                {
                    UIElement element = vChild as UIElement;
                    UIElement3D element3D = vChild as UIElement3D;

                    if (element3D != null)
                    {
                        if (property == IsVisibleProperty)
                        {
                            // For Read-Only force-inherited properties, use
                            // a private update method.
                            element3D.UpdateIsVisibleCache();
                        }
                        else
                        {
                            // For Read/Write force-inherited properties, use
                            // the standard coersion pattern.
                            element3D.CoerceValue(property);
                        }
                    }
                    else if (element != null)
                    {
                        if (property == IsVisibleProperty)
                        {
                            // For Read-Only force-inherited properties, use
                            // a private update method.
                            element.UpdateIsVisibleCache();
                        }
                        else
                        {
                            // For Read/Write force-inherited properties, use
                            // the standard coersion pattern.
                            element.CoerceValue(property);
                        }
                    }
                    else
                    {
                        // We have to "walk through" non-UIElement visuals.
                        if (vChild is Visual)
                        {
                            ((Visual)vChild).InvalidateForceInheritPropertyOnChildren(property);
                        }
                        else
                        {
                            ((Visual3D)vChild).InvalidateForceInheritPropertyOnChildren(property);
                        }
                    }
                }
            }
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

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private readonly Visual3DCollection _children;

        private bool _renderRequestPosted;

        #endregion Private Fields

        // Perf analysis showed we were not using these fields enough to warrant
        // bloating each instance with the field, so storage is created on-demand
        // in the local store.
        internal static readonly UncommonField<EventHandlersStore> EventHandlersStoreField = new UncommonField<EventHandlersStore>();
        internal static readonly UncommonField<InputBindingCollection> InputBindingCollectionField = new UncommonField<InputBindingCollection>();
        internal static readonly UncommonField<CommandBindingCollection> CommandBindingCollectionField = new UncommonField<CommandBindingCollection>();
        private static readonly UncommonField<AutomationPeer> AutomationPeerField = new UncommonField<AutomationPeer>();

        internal bool HasAutomationPeer
        {
            get { return ReadFlag(CoreFlags.HasAutomationPeer); }
            set { WriteFlag(CoreFlags.HasAutomationPeer, value); }
        }
    }
}
