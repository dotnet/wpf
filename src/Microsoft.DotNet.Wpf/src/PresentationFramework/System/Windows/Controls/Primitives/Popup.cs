// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Markup;
using System.Windows.Threading;
using System.Text;
using Accessibility;
using MS.Internal;
using MS.Internal.Controls;
using MS.Internal.Data;
using MS.Internal.KnownBoxes;
using MS.Internal.Interop;
using MS.Utility;
using MS.Win32;

using CommonDependencyProperty=MS.Internal.PresentationFramework.CommonDependencyPropertyAttribute;

// Disable pragma warnings to enable PREsharp pragmas
#pragma warning disable 1634, 1691


namespace System.Windows.Controls.Primitives
{
    /// <summary>
    ///     A control that creates a fly-out window that contains content.
    /// </summary>
    /// <remarks>
    ///     Popup creates a new top-level window to display content that can move beyond the bounds of
    ///     an application's window.
    ///     The Popup content is not affected by styles and properties in other trees
    ///     unless specifically bound to them.
    /// </remarks>
    [DefaultEvent("Opened"), DefaultProperty("Child")]
    [Localizability(LocalizationCategory.None)]
    [ContentProperty("Child")]
    public class Popup : FrameworkElement, IAddChild
    {
        #region Constructors

        static Popup()
        {
            EventManager.RegisterClassHandler(typeof(Popup), Mouse.LostMouseCaptureEvent, new MouseEventHandler(OnLostMouseCapture));
            EventManager.RegisterClassHandler(typeof(Popup), DragDrop.DragDropStartedEvent, new RoutedEventHandler(OnDragDropStarted), true);
            EventManager.RegisterClassHandler(typeof(Popup), DragDrop.DragDropCompletedEvent, new RoutedEventHandler(OnDragDropCompleted), true);

            VisibilityProperty.OverrideMetadata(typeof(Popup), new FrameworkPropertyMetadata(VisibilityBoxes.CollapsedBox, null, new CoerceValueCallback(CoerceVisibility)));
        }

        // Force Popup to always be collapsed - computing transform of child assumes popup is collapsed
        private static object CoerceVisibility(DependencyObject d, object value)
        {
            return VisibilityBoxes.CollapsedBox;
        }

        /// <summary>
        ///     Default constructor
        /// </summary>
        public Popup() : base()
        {
            // create popup's security helper
            _secHelper = new PopupSecurityHelper();
        }

        #endregion

        #region Properties

        /// <summary>
        ///     The DependencyProperty for the TreatMousePlacementAsBottom property.
        ///     Flags:              None
        ///     Default Value:      false
        /// </summary>
        internal static readonly DependencyProperty TreatMousePlacementAsBottomProperty =
            DependencyProperty.Register(
                                "TreatMousePlacementAsBottom",
                                typeof(bool),
                                typeof(Popup),
                                new FrameworkPropertyMetadata(
                                            false));

        /// <summary>
        ///     Tooltips should show on Keyboard focus.
        ///     Chooses whether Mouse or MousePoint placement for the popup should be overriden with Bottom.
        ///     This is used to show tooltips when an element receives keyboard focus, and the placement is set to Mouse or MousePoint.
        /// </summary>
        internal bool TreatMousePlacementAsBottom
        {
            get { return (bool)GetValue(TreatMousePlacementAsBottomProperty); }
            set { SetValue(TreatMousePlacementAsBottomProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the Child property.
        ///     Flags:              None
        ///     Default Value:      null
        /// </summary>
        public static readonly DependencyProperty ChildProperty =
                DependencyProperty.Register(
                        "Child",
                        typeof(UIElement),
                        typeof(Popup),
                        new FrameworkPropertyMetadata(
                                (object) null,
                                new PropertyChangedCallback(OnChildChanged)));

        /// <summary>
        ///     The content of the Popup
        /// </summary>
        [Bindable(true), CustomCategory("Content")]
        public UIElement Child
        {
            get { return (UIElement) GetValue(ChildProperty); }
            set { SetValue(ChildProperty, value); }
        }

        private static void OnChildChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Popup popup = (Popup)d;

            UIElement oldChild = (UIElement) e.OldValue;
            UIElement newChild = (UIElement) e.NewValue;

            // If the Popup is open, change the PopupRoot's child to show the new content.
            // Also change if the PopupRoot has a non-null child, to enable that
            // child to participate elsewhere in the visual tree
            if ((popup._popupRoot.Value != null) && (popup.IsOpen || popup._popupRoot.Value.Child != null))
            {
                popup._popupRoot.Value.Child = newChild;
            }

            popup.RemoveLogicalChild(oldChild);

            popup.AddLogicalChild(newChild);
            popup.Reposition();

            popup.pushTextRenderingMode();
        }

        /// <summary>
        /// The attached property maintains an array list of popups registered with an element. The
        /// attached property can be attached to any element.
        /// </summary>
        internal static readonly UncommonField<List<Popup>> RegisteredPopupsField = new UncommonField<List<Popup>>();

        internal override void pushTextRenderingMode()
        {
            //
            // TextRenderingMode is inherited both in the UIElement tree and the graphics tree.
            // This means we don't need to set VisualTextRenderingMode on every single node, we only
            // want to set it on a Visual when it is explicitly set, or set in a manner other than inheritance.
            // The sole exception to this is PopupRoot, which needs to propagate the value to its Visual, because
            // the graphics tree does not inherit across CompositionTarget boundaries.
            //
            if (Child != null)
            {
                System.Windows.ValueSource vs = DependencyPropertyHelper.GetValueSource(Child, TextOptions.TextRenderingModeProperty);
                if (vs.BaseValueSource <= BaseValueSource.Inherited)
                {
                        Child.VisualTextRenderingMode = TextOptions.GetTextRenderingMode(this);
                }
            }
        }

        /// <summary>
        /// Registers this popup with the specified placement target. The descendant walker requires this so that
        /// it can traverse into the popup's element tree.
        /// </summary>
        private static void RegisterPopupWithPlacementTarget(Popup popup, UIElement placementTarget)
        {
            Debug.Assert(popup != null, "Popup must be non-null");
            Debug.Assert(placementTarget != null, "Placement target must be non-null.");

            //
            // The registered popups are stored in an array list on the specified element (which is
            // typically the placement target).
            // The array list for storing the registered popups on the placement target is lazily created.
            //

            List<Popup> registeredPopups = RegisteredPopupsField.GetValue(placementTarget);
            if (registeredPopups == null)
            {
                registeredPopups = new List<Popup>();
                RegisteredPopupsField.SetValue(placementTarget, registeredPopups);
            }
            if (!registeredPopups.Contains(popup))
            {
                registeredPopups.Add(popup);
            }
        }

        /// <summary>
        /// Unregisters the popup from the spefied placement target. For more details see comments on
        /// RegisterPopupWithPlacementTarget.
        /// </summary>
        private static void UnregisterPopupFromPlacementTarget(Popup popup, UIElement placementTarget)
        {
            Debug.Assert(popup != null, "Popup must be non-null");
            Debug.Assert(placementTarget != null, "Placement target must be non-null.");

            List<Popup> registeredPopups = RegisteredPopupsField.GetValue(placementTarget);

            if (registeredPopups != null)
            {
                registeredPopups.Remove(popup);

                // If after removing this popup from the placement targets popup registration list, no more
                // popups are left, we can also get rid of the array list.
                if (registeredPopups.Count == 0)
                {
                    RegisteredPopupsField.SetValue(placementTarget, null);
                }
            }
        }

        /// <summary>
        /// Updates the popup's placement target registration.
        /// This method is only called when IsOpen changes or when PlacementTarget changes,
        /// When IsOpen changes, your before/after is either PlacementTarget or null. When PlacementTarget changes, the before/after are stored in the event args.
        /// </summary>
        private void UpdatePlacementTargetRegistration(UIElement oldValue, UIElement newValue)
        {
            // A popup will be registered with its placement target to enable the descendent walker
            // to traverse into the popup. This is required for style sheet invalidations, etc.
            //
            // To avoid life-time issues, the popup will only be registered with the placement target
            // if the popup is in the Open state. Otherwise the strong-ref from the placement target
            // back to the popup could potentially keep the popup alive even though it has long
            // been closed.

            if (oldValue != null)
            {
                UnregisterPopupFromPlacementTarget(this, oldValue);

                if (newValue == null && VisualTreeHelper.GetParent(this) == null)
                {
                    TreeWalkHelper.InvalidateOnTreeChange(this, null, oldValue, false);
                }
            }
            if (newValue != null)
            {
                //Only register with PlacementTarget if we aren't in a tree
                if (VisualTreeHelper.GetParent(this) == null)
                {
                    RegisterPopupWithPlacementTarget(this, newValue);

                    // A Popup using its placement target as its InheritanceParent is dicey
                    // because the inheritable property or tree change invalidation storm
                    // for the the PlacementTarget is separate from that for the Popup itself.
                    // This causes Popup and its descedents to miss some change notifications.
                    // Thus a Popup that isnt connected to the tree in any way should be
                    // designated standalone and thus IsSelfInheritanceParent = true. 
                    if (!this.IsSelfInheritanceParent)
                    {
                        this.SetIsSelfInheritanceParent();
                    }

                    // Invalidate relevant properties for this subtree
                    TreeWalkHelper.InvalidateOnTreeChange(this, null, newValue, true);
               }
            }
        }

        /// <summary>
        ///     The DependencyProperty for the IsOpen property.
        ///     Flags:              None
        ///     Default Value:      false
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty IsOpenProperty =
                DependencyProperty.Register(
                        "IsOpen",
                        typeof(bool),
                        typeof(Popup),
                        new FrameworkPropertyMetadata(
                                BooleanBoxes.FalseBox,
                                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                                new PropertyChangedCallback(OnIsOpenChanged),
                                new CoerceValueCallback(CoerceIsOpen)));

        /// <summary>
        /// Indicates whether the Popup is visible.
        /// </summary>
        [Bindable(true), Category("Appearance")]
        public bool IsOpen
        {
            get { return (bool) GetValue(IsOpenProperty); }
            set { SetValue(IsOpenProperty, BooleanBoxes.Box(value)); }
        }

        private static object CoerceIsOpen(DependencyObject d, object value)
        {
            if ((bool)value)
            {
                Popup popup = (Popup)d;

                // For popups in the tree, don't open until it is loaded
                if (!popup.IsLoaded && VisualTreeHelper.GetParent(popup) != null)
                {
                    popup.RegisterToOpenOnLoad();
                    return BooleanBoxes.FalseBox;
                }
            }

            return value;
        }

        private void RegisterToOpenOnLoad()
        {
            Loaded += new RoutedEventHandler(OpenOnLoad);
        }

        private void OpenOnLoad(object sender, RoutedEventArgs e)
        {
            // Open popup after main tree has rendered (Loaded is fired before 1st render)
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new DispatcherOperationCallback(delegate(object param)
            {
                CoerceValue(IsOpenProperty);

                return null;
            }), null);
        }

        /// <summary>
        ///     Called when IsOpenProperty is changed on "d."
        /// </summary>
        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Popup popup = (Popup)d;

            // This is actually the current state and not necessary the desired state (i.e. old value)
            bool currentVisible = (popup._secHelper.IsWindowAlive() && (popup._asyncDestroy == null)) || (popup._asyncCreate != null);
            bool visible = (bool) e.NewValue;

            if (visible != currentVisible)
            {
                if (visible)
                {
                    // The popup wants to be visible

                    if (popup._cacheValid[(int)CacheBits.OnClosedHandlerReopen])
                        throw new InvalidOperationException(SR.Get(SRID.PopupReopeningNotAllowed));

                    popup.CancelAsyncDestroy();

                    // Cancel any pending async create requests, we're creating now
                    popup.CancelAsyncCreate();
                    popup.CreateWindow(false /*asyncCall*/);

                    // It is possible that the popup is destroyed by CreateWindow or one of its callbacks
                    if (popup._secHelper.IsWindowAlive())
                    {
                        // Close the popup when it is unloaded from the visual tree
                        if (CloseOnUnloadedHandler == null)
                        {
                            CloseOnUnloadedHandler = new RoutedEventHandler(CloseOnUnloaded);
                        }

                        popup.Unloaded += CloseOnUnloadedHandler;
                    }
                }
                else
                {
                    // The popup wants to hide
                    popup.CancelAsyncCreate();

                    if (popup._secHelper.IsWindowAlive() && (popup._asyncDestroy == null))
                    {
                        // The popup window still exists, get rid of it
                        // There are also no other async destroy requests

                        // Hide the window (synchronously). This will cause repaint messages to be sent
                        // to underlying windows and Render work items to be queued.
                        popup.HideWindow();

                        if (CloseOnUnloadedHandler != null)
                        {
                            popup.Unloaded -= CloseOnUnloadedHandler;
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Called when IsOpen becomes true on this popup.
        /// </summary>
        /// <param name="e">Empty event arguments.</param>
        protected virtual void OnOpened(EventArgs e)
        {
            RaiseClrEvent(OpenedKey, e);
        }

        /// <summary>
        ///     Called when IsOpen becomes false on this popup.
        /// </summary>
        /// <param name="e">Empty event arguments.</param>
        protected virtual void OnClosed(EventArgs e)
        {
            _cacheValid[(int)CacheBits.OnClosedHandlerReopen] = true;
            try
            {
                RaiseClrEvent(ClosedKey, e);
            }
            finally
            {
                _cacheValid[(int)CacheBits.OnClosedHandlerReopen] = false;
            }
        }

        private static void CloseOnUnloaded(object sender, RoutedEventArgs e)
        {
            ((Popup)sender).SetCurrentValueInternal(IsOpenProperty, BooleanBoxes.FalseBox);
        }

        /// <summary>
        ///     The DependencyProperty for the Placement property.
        ///     Flags:              None
        ///     Default Value:      PlacementMode.Bottom
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty PlacementProperty =
                DependencyProperty.Register(
                        "Placement",
                        typeof(PlacementMode),
                        typeof(Popup),
                        new FrameworkPropertyMetadata(
                                PlacementMode.Bottom,
                                new PropertyChangedCallback(OnPlacementChanged)),
                        new ValidateValueCallback(IsValidPlacementMode));

        /// <summary>
        ///     Chooses the behavior of where the Popup should be placed on screen.
        /// </summary>
        [Bindable(true), Category("Layout")]
        public PlacementMode Placement
        {
            get { return (PlacementMode) GetValue(PlacementProperty); }
            set { SetValue(PlacementProperty, value); }
        }

        /// <summary>
        ///     Tooltips should show on Keyboard focus.
        ///     Chooses the behavior of where the Popup should be placed on screen.
        ///     Takes into account TreatMousePlacementAsBottom to place tooltips correctly on keyboard focus.
        /// </summary>
        internal PlacementMode PlacementInternal
        {
            get
            {
                PlacementMode placement = Placement;
                bool isMouseMode = (placement == PlacementMode.Mouse || placement == PlacementMode.MousePoint);
                if (isMouseMode && TreatMousePlacementAsBottom)
                {
                    placement = PlacementMode.Bottom;
                }

                return placement;
            }
        }

        /// <summary>
        ///     Called when Placement is changed on "d."
        /// </summary>
        private static void OnPlacementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Popup popup = (Popup) d;

            popup.Reposition();
        }

        private static bool IsValidPlacementMode(object o)
        {
            PlacementMode value = (PlacementMode)o;
            return value == PlacementMode.Absolute
                || value == PlacementMode.AbsolutePoint
                || value == PlacementMode.Bottom
                || value == PlacementMode.Center
                || value == PlacementMode.Mouse
                || value == PlacementMode.MousePoint
                || value == PlacementMode.Relative
                || value == PlacementMode.RelativePoint
                || value == PlacementMode.Right
                || value == PlacementMode.Left
                || value == PlacementMode.Top
                || value == PlacementMode.Custom;
        }

        /// <summary>
        ///     The DependencyProperty for the CustomPopupPlacementCallback property.
        ///     Flags:              None
        ///     Default Value:      null
        /// </summary>
        public static readonly DependencyProperty CustomPopupPlacementCallbackProperty =
                DependencyProperty.Register(
                        "CustomPopupPlacementCallback",
                        typeof(CustomPopupPlacementCallback),
                        typeof(Popup),
                        new FrameworkPropertyMetadata((object) null));

        /// <summary>
        ///     Chooses the behavior of where the Popup should be placed on screen.
        /// </summary>
        [Bindable(false), Category("Layout")]
        public CustomPopupPlacementCallback CustomPopupPlacementCallback
        {
            get { return (CustomPopupPlacementCallback) GetValue(CustomPopupPlacementCallbackProperty); }
            set { SetValue(CustomPopupPlacementCallbackProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the StaysOpen property.
        ///     Flags:              None
        ///     Default Value:      true
        /// </summary>
        public static readonly DependencyProperty StaysOpenProperty =
                DependencyProperty.Register(
                        "StaysOpen",
                        typeof(bool),
                        typeof(Popup),
                        new FrameworkPropertyMetadata(
                                BooleanBoxes.TrueBox,
                                new PropertyChangedCallback(OnStaysOpenChanged)));

        /// <summary>
        ///     Chooses the behavior of when the Popup should automatically close.
        /// </summary>
        [Bindable(true), Category("Behavior")]
        public bool StaysOpen
        {
            get { return (bool) GetValue(StaysOpenProperty); }
            set { SetValue(StaysOpenProperty, BooleanBoxes.Box(value)); }
        }

        /// <summary>
        ///     Called when StaysOpen property is changed on "d."
        /// </summary>
        private static void OnStaysOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Popup popup = (Popup)d;

            if (popup.IsOpen)
            {
                if ((bool)e.NewValue)
                {
                    popup.ReleasePopupCapture();
                }
                else
                {
                    popup.EstablishPopupCapture();
                }
            }
        }

        /// <summary>
        ///     The DependencyProperty for the HorizontalOffset property.
        ///     Flags:              None
        ///     Default Value:      0
        /// </summary>
        public static readonly DependencyProperty HorizontalOffsetProperty =
                DependencyProperty.Register(
                        "HorizontalOffset",
                        typeof(double),
                        typeof(Popup),
                        new FrameworkPropertyMetadata(
                                0d,
                                new PropertyChangedCallback(OnOffsetChanged)));

        /// <summary>
        ///     Offset from the left of the desired location based on the Placement property.
        ///     Percentages are based on the visual parent, if one exists.
        /// </summary>
        [Bindable(true), Category("Layout")]
        [TypeConverter(typeof(LengthConverter))]
        public double HorizontalOffset
        {
            get { return (double) GetValue(HorizontalOffsetProperty); }
            set { SetValue(HorizontalOffsetProperty, value); }
        }

        /// <summary>
        ///     Called when HorizontalOffset, VerticalOffset, or PlacementRectangle is changed on "d."
        /// </summary>
        private static void OnOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Popup popup = (Popup) d;

            popup.Reposition();
        }

        /// <summary>
        ///     The DependencyProperty for the VerticalOffset property.
        ///     Flags:              None
        ///     Default Value:      0
        /// </summary>
        public static readonly DependencyProperty VerticalOffsetProperty =
                DependencyProperty.Register(
                        "VerticalOffset",
                        typeof(double),
                        typeof(Popup),
                        new FrameworkPropertyMetadata(
                                0d,
                                new PropertyChangedCallback(OnOffsetChanged)));

        /// <summary>
        ///     Offset from the top of the desired location based on the Placement property.
        ///     Percentages are based on the visual parent, if one exists.
        /// </summary>
        [Bindable(true), Category("Layout")]
        [TypeConverter(typeof(LengthConverter))]
        public double VerticalOffset
        {
            get { return (double) GetValue(VerticalOffsetProperty); }
            set { SetValue(VerticalOffsetProperty, value); }
        }

        /// <summary>
        /// The DependencyProperty for the PlacementTarget property
        /// Default value: null
        /// </summary>
        public static readonly DependencyProperty PlacementTargetProperty =
                DependencyProperty.Register(
                        "PlacementTarget",
                        typeof(UIElement),
                        typeof(Popup),
                        new FrameworkPropertyMetadata(
                            (object) null,
                            new PropertyChangedCallback(OnPlacementTargetChanged)));

        /// <summary>
        /// The UIElement relative to which the Popup will be displayed. If PlacementTarget is null (which
        /// it is by default), the Popup is displayed relative to its visual parent.
        /// </summary>
        [Bindable(true), Category("Layout")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public UIElement PlacementTarget
        {
            get { return (UIElement) GetValue(PlacementTargetProperty); }
            set { SetValue(PlacementTargetProperty, value); }
        }

        /// <summary>
        /// When the placement target changes the popup has to be unregistered from its old placement
        /// target and registered with the new placement target.
        /// </summary>
        private static void OnPlacementTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Popup ctrl = (Popup) d;
            if (ctrl.IsOpen)
            {
                // When PlacementTarget changes, the before/after are stored in the event args.
                ctrl.UpdatePlacementTargetRegistration((UIElement)e.OldValue, (UIElement)e.NewValue);
            }
            else if (e.OldValue != null)
            {
                UnregisterPopupFromPlacementTarget(ctrl, (UIElement)e.OldValue);
            }
        }


        /// <summary>
        /// The DependencyProperty for the PlacementRectangle property
        /// Default value: Rect.Empty
        /// </summary>
        public static readonly DependencyProperty PlacementRectangleProperty =
                DependencyProperty.Register(
                        "PlacementRectangle",
                        typeof(Rect),
                        typeof(Popup),
                        new FrameworkPropertyMetadata(
                                Rect.Empty,
                                new PropertyChangedCallback(OnOffsetChanged)));

        /// <summary>
        /// The rectangle relative to which the Popup will be displayed. If PlacementRectangle is null (which
        /// it is by default), the Popup is displayed relative to its visual parent.
        /// </summary>
        [Bindable(true), Category("Layout")]
        public Rect PlacementRectangle
        {
            get { return (Rect) GetValue(PlacementRectangleProperty); }
            set { SetValue(PlacementRectangleProperty, value); }
        }

        /// <summary>
        ///     Indicates whether Right and Left placement modes should drop
        ///     the opposite of normal. This happens when they hit the edge
        ///     of the monitor and have to flip. The next popup needs to know
        ///     to continue going in the opposite direction.
        /// </summary>
        internal bool DropOpposite
        {
            get
            {
                bool opposite = false;

                if (_cacheValid[(int)CacheBits.DropOppositeSet])
                {
                    opposite = _cacheValid[(int)CacheBits.DropOpposite];
                }
                else
                {
                    DependencyObject parent = this;
                    do
                    {
                        parent = VisualTreeHelper.GetParent(parent);
                        PopupRoot popupRoot = parent as PopupRoot;
                        if (popupRoot != null)
                        {
                            Popup popup = popupRoot.Parent as Popup;
                            parent = popup;
                            if (popup != null)
                            {
                                if (popup._cacheValid[(int)CacheBits.DropOppositeSet])
                                {
                                    opposite = popup._cacheValid[(int)CacheBits.DropOpposite];
                                    break;
                                }
                            }
                        }
                    }
                    while (parent != null);
                }

                return opposite;
            }

            set
            {
                _cacheValid[(int)CacheBits.DropOpposite] = value;
                _cacheValid[(int)CacheBits.DropOppositeSet] = true;
            }
        }

        private void ClearDropOpposite()
        {
            _cacheValid[(int)CacheBits.DropOppositeSet] = false;
        }

        /// <summary>
        ///     The DependencyProperty for the PopupAnimation property.
        ///     Flags:              None
        ///     Default Value:      Fade
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty PopupAnimationProperty =
                DependencyProperty.Register(
                        "PopupAnimation",
                        typeof(PopupAnimation),
                        typeof(Popup),
                        new FrameworkPropertyMetadata(PopupAnimation.None,
                                                      null,
                                                      new CoerceValueCallback(CoercePopupAnimation)),
                        new ValidateValueCallback(IsValidPopupAnimation));

        /// <summary>
        ///     The animation type of the popup.
        ///     This value of this property will take effect the next time the popup opens.
        /// </summary>
        [Bindable(true), Category("Appearance")]
        public PopupAnimation PopupAnimation
        {
            get { return (PopupAnimation) GetValue(PopupAnimationProperty); }
            set { SetValue(PopupAnimationProperty, value); }
        }

        // Coerce animation to None if popup is not transparent
        private static object CoercePopupAnimation(DependencyObject o, object value)
        {
            return ((Popup)o).AllowsTransparency ? value : PopupAnimation.None;
        }

        private static bool IsValidPopupAnimation(object o)
        {
            PopupAnimation value = (PopupAnimation)o;
            return value == PopupAnimation.None
                || value == PopupAnimation.Fade
                || value == PopupAnimation.Slide
                || value == PopupAnimation.Scroll;
        }

        /// <summary>
        /// DependencyProperty for AllowsTransparency
        /// </summary>
        public static readonly DependencyProperty AllowsTransparencyProperty =
                Window.AllowsTransparencyProperty.AddOwner(typeof(Popup),
                        new FrameworkPropertyMetadata(
                                BooleanBoxes.FalseBox,
                                new PropertyChangedCallback(OnAllowsTransparencyChanged),
                                new CoerceValueCallback(CoerceAllowsTransparency)));

        /// <summary>
        /// Whether or not the "popup" allows transparent content
        /// </summary>
        public bool AllowsTransparency
        {
            get { return (bool) GetValue(AllowsTransparencyProperty); }
            set { SetValue(AllowsTransparencyProperty, BooleanBoxes.Box(value)); }
        }

        private static void OnAllowsTransparencyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(PopupAnimationProperty);
        }

        private static object CoerceAllowsTransparency(DependencyObject d, object value)
        {
            return ((Popup)d)._secHelper.IsChildPopup ? BooleanBoxes.FalseBox : value;
        }


        private static readonly DependencyPropertyKey HasDropShadowPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "HasDropShadow",
                        typeof(bool),
                        typeof(Popup),
                        new FrameworkPropertyMetadata(
                                BooleanBoxes.FalseBox,
                                null,
                                new CoerceValueCallback(CoerceHasDropShadow)));

        /// <summary>
        /// DependencyProperty for HasDropShadow
        /// </summary>
        public static readonly DependencyProperty HasDropShadowProperty =
                HasDropShadowPropertyKey.DependencyProperty;

        /// <summary>
        /// Whether or not the "popup" should have a drop shadow according to
        /// the DropShadow system parameters
        /// </summary>
        public bool HasDropShadow
        {
            get { return (bool)GetValue(HasDropShadowProperty); }
        }

        private static object CoerceHasDropShadow(DependencyObject d, object value)
        {
            return BooleanBoxes.Box(SystemParameters.DropShadow && ((Popup)d).AllowsTransparency);
        }

        #endregion

        #region Helper Functions

        /// <summary>
        ///     Hooks up a Popup to a child.
        ///     The child will be required to implement the following properties:
        ///         Popup.IsOpenProperty
        ///         Popup.PlacementProperty
        ///         Popup.PlacementRectangleProperty
        ///         Popup.PlacementTargetProperty
        ///         Popup.HorizontalOffsetProperty
        ///         Popup.VerticalOffsetProperty
        /// </summary>
        /// <param name="popup">The parent popup that the child will be hooked up to.</param>
        /// <param name="child">The element to be the child of the popup.</param>
        public static void CreateRootPopup(Popup popup, UIElement child)
        {
            CreateRootPopupInternal(popup, child, false);
        }

        /// <summary>
        ///     Internal implementation of CreateRootPopup to allow tooltips to 
        ///     override the popup's placement in case the tooltip comes from keyboard focus.
        /// </summary>
        /// <param name="popup">The parent popup that the child will be hooked up to.</param>
        /// <param name="child">The element to be the child of the popup.</param>
        /// <param name="bindTreatMousePlacementAsBottomProperty">Whether to bind TreatMousePlacementAsBottomProperty to the child's FromKeyboard property</param>
        internal static void CreateRootPopupInternal(Popup popup, UIElement child, bool bindTreatMousePlacementAsBottomProperty)
        { 
            if (popup == null)
            {
                throw new ArgumentNullException("popup");
            }
            if (child == null)
            {
                throw new ArgumentNullException("child");
            }

            Debug.Assert(!bindTreatMousePlacementAsBottomProperty || child is ToolTip, "child must be a Tooltip to bind TreatMousePlacementAsBottomProperty");

            // When we get here, the Child must not have already been visually or logically parented.
            object currentParent = null;
            if ((currentParent = LogicalTreeHelper.GetParent(child)) != null)
            {
                throw new InvalidOperationException(SR.Get(SRID.CreateRootPopup_ChildHasLogicalParent, child, currentParent));
            }

            if ((currentParent = VisualTreeHelper.GetParent(child)) != null)
            {
                throw new InvalidOperationException(SR.Get(SRID.CreateRootPopup_ChildHasVisualParent, child, currentParent));
            }

            // PlacementTarget must be set before hooking up the child so that resource
            // lookups can work.  The Popup for tooltip and context menu isn't in the tree
            // so FE relies on GetUIParentCore to return the placement target as the
            // effective logical parent
            Binding binding = new Binding("PlacementTarget");
            binding.Mode = BindingMode.OneWay;
            binding.Source = child;
            popup.SetBinding(PlacementTargetProperty, binding);

            // NOTE: this will hook up child as a logical child of Popup.
            // If at a later date this is not desired, then modify the hookup to avoid the logical hookup.
            //
            // NOTE: Logical linking is necessary if property invalidations are to propagate down
            // the tree into the child (unless at a later date an alternate method has been created).
            popup.Child = child;

            binding = new Binding("VerticalOffset");
            binding.Mode = BindingMode.OneWay;
            binding.Source = child;
            popup.SetBinding(VerticalOffsetProperty, binding);

            binding = new Binding("HorizontalOffset");
            binding.Mode = BindingMode.OneWay;
            binding.Source = child;
            popup.SetBinding(HorizontalOffsetProperty, binding);

            binding = new Binding("PlacementRectangle");
            binding.Mode = BindingMode.OneWay;
            binding.Source = child;
            popup.SetBinding(PlacementRectangleProperty, binding);

            binding = new Binding("Placement");
            binding.Mode = BindingMode.OneWay;
            binding.Source = child;
            popup.SetBinding(PlacementProperty, binding);

            binding = new Binding("StaysOpen");
            binding.Mode = BindingMode.OneWay;
            binding.Source = child;
            popup.SetBinding(StaysOpenProperty, binding);

            binding = new Binding("CustomPopupPlacementCallback");
            binding.Mode = BindingMode.OneWay;
            binding.Source = child;
            popup.SetBinding(CustomPopupPlacementCallbackProperty, binding);
            
            if (bindTreatMousePlacementAsBottomProperty)
            {
                binding = new Binding("FromKeyboard");
                binding.Mode = BindingMode.OneWay;
                binding.Source = child;
                popup.SetBinding(TreatMousePlacementAsBottomProperty, binding);
            }

            // Note: IsOpen should always be last in this method
            binding = new Binding("IsOpen");
            binding.Mode = BindingMode.OneWay;
            binding.Source = child;
            popup.SetBinding(IsOpenProperty, binding);
        }

        // This is to check if ContextMenu and ToolTip are still setup properly inside a Popup
        internal static bool IsRootedInPopup(Popup parentPopup, UIElement element)
        {
            // Look for a logical parent first
            object logicalParent = LogicalTreeHelper.GetParent(element);

            // If there's no logical parent, we better not have a visual parent
            if (logicalParent == null && VisualTreeHelper.GetParent(element) != null)
            {
                return false;
            }

            // If the logical parent doesn't match the Popup created by CreateRootPopup,
            // then we should return false.
            if (logicalParent != parentPopup)
            {
                return false;
            }

            return true;
        }

        #endregion

        #region Events

        /// <summary>
        ///     Event indicating that IsOpen has changed to true.
        /// </summary>
        public event EventHandler Opened
        {
            add { EventHandlersStoreAdd(OpenedKey, value); }
            remove { EventHandlersStoreRemove(OpenedKey, value); }
        }
        private static readonly EventPrivateKey OpenedKey = new EventPrivateKey();

        /// <summary>
        ///     Event indicating that IsOpen has changed to false.
        /// </summary>
        public event EventHandler Closed
        {
            add { EventHandlersStoreAdd(ClosedKey, value); }
            remove { EventHandlersStoreRemove(ClosedKey, value); }
        }
        private static readonly EventPrivateKey ClosedKey = new EventPrivateKey();

        private void FirePopupCouldClose()
        {
            if (PopupCouldClose != null)
            {
                PopupCouldClose(this, EventArgs.Empty);
            }
        }

        /// <summary>
        ///     This is a temporary placeholder until we can devise a public API for
        ///     ContextMenus and ToolTips so they can know when we get a deactivate app msg
        ///     or an IME open msg.
        /// </summary>
        internal event EventHandler PopupCouldClose;

        #endregion

        #region Layout

        /// <summary>
        ///     Invoked when remeasuring the control is required.
        ///     The Popup will always return a size of zero because its content is not within this
        ///     visual tree. The content is inside a different window/visual tree.
        /// </summary>
        /// <param name="availableSize">The control cannot return a size larger than the constraint.</param>
        /// <returns>The size (always zero for Popup)</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            // Popup is always zero size. It's the content inside the window that has a size.
            return new Size();
        }


        #endregion

        #region Input

        /// <summary>
        ///     Called when the mouse left button is pressed on this subtree
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            OnPreviewMouseButton(e);
            base.OnPreviewMouseLeftButtonDown(e);
        }

        /// <summary>
        ///     Called when the mouse right button is pressed on this subtree
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreviewMouseRightButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseRightButtonDown(e);

            OnPreviewMouseButton(e);
        }

        /// <summary>
        ///     Called when the mouse left button is released on this subtree
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            OnPreviewMouseButton(e);
            base.OnPreviewMouseLeftButtonUp(e);
        }

        /// <summary>
        ///     Called when the mouse right button is released on this subtree
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreviewMouseRightButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseRightButtonUp(e);

            OnPreviewMouseButton(e);
        }

        private void OnPreviewMouseButton(MouseButtonEventArgs e)
        {
            // We should only react to mouse buttons if we are in an auto close mode (where we have capture)
            if (_cacheValid[(int)CacheBits.CaptureEngaged] && !StaysOpen &&
                !_cacheValid[(int)CacheBits.IsIgnoringMouseEvents])
            {
                Debug.Assert( Mouse.Captured == _popupRoot.Value, "_cacheValid[(int)CacheBits.CaptureEngaged] == true but Mouse.Captured != _popupRoot");

                // If we got a mouse press/release and the mouse isn't on the popup (popup root), dismiss.
                // When captured to subtree, source will be the captured element for events outside the popup.
                if (_popupRoot.Value != null && e.OriginalSource == _popupRoot.Value)
                {
                    // When we have capture we will get all mouse button up/down messages.
                    // We should close if the press was outside.  The MouseButtonEventArgs don't tell whether we get this
                    // message because we have capture or if it was legit, so we have to do a hit test.
                    if (_popupRoot.Value.InputHitTest(e.GetPosition(_popupRoot.Value)) == null)
                    {
                        // The hit test didn't find any element; that means the click happened outside the popup.
                        SetCurrentValueInternal(IsOpenProperty, BooleanBoxes.FalseBox);
                    }
                }
            }

            // once a mouse event arrives with neither button pressed, stop ignoring
            if (_cacheValid[(int)CacheBits.IsIgnoringMouseEvents] &&
                e.LeftButton == MouseButtonState.Released &&
                e.RightButton == MouseButtonState.Released)
            {
                _cacheValid[(int)CacheBits.IsIgnoringMouseEvents] = false;
            }
        }

        private void EstablishPopupCapture(bool isRestoringCapture=false)
        {
            if (!_cacheValid[(int)CacheBits.CaptureEngaged] && (_popupRoot.Value != null) &&
                (!StaysOpen))
            {
                IInputElement capturedElement = Mouse.Captured;
                PopupRoot parentPopupRoot = capturedElement as PopupRoot;
                if (parentPopupRoot != null)
                {
                    if (isRestoringCapture)
                    {
                        // if the other PopupRoot is restoring capture back to this
                        // popup, ignore mouse button events until both buttons have been
                        // released.  Otherwise a mouse click outside a chain of
                        // "nested" popups would dismiss two of them - one on MouseDown
                        // and another on MouseUp.
                        if (Mouse.LeftButton != MouseButtonState.Released ||
                            Mouse.RightButton != MouseButtonState.Released)
                        {
                            _cacheValid[(int)CacheBits.IsIgnoringMouseEvents] = true;
                        }
                    }
                    else
                    {
                        // this is a "nested" popup, invoked while another popup is open.
                        // We need to restore capture to the previous popup root when
                        // we're done
                        ParentPopupRootField.SetValue(this, parentPopupRoot);
                    }

                    // in either case, taking capture away from the other PopupRoot is OK.
                    capturedElement = null;
                }

                if (capturedElement == null)
                {
                    // When the mouse is not already captured, we will consider the following:
                    // In all cases but Modeless, we want the popup and subtree to receive
                    // mouse events and prevent other elements from receiving those messages.
                    Mouse.Capture(_popupRoot.Value, CaptureMode.SubTree);
                    _cacheValid[(int)CacheBits.CaptureEngaged] = true;
                }
            }
        }

        private void ReleasePopupCapture()
        {
            if (_cacheValid[(int)CacheBits.CaptureEngaged])
            {
                PopupRoot parentPopupRoot = ParentPopupRootField.GetValue(this);
                ParentPopupRootField.ClearValue(this);

                // Only give up capture if we have it (someone may have taken it from us).
                if (Mouse.Captured == _popupRoot.Value)
                {
                    if (parentPopupRoot == null)
                    {
                        Mouse.Capture(null);
                    }
                    else
                    {
                        // restore capture to popup we took it from, if there was one
                        Popup parentPopup = parentPopupRoot.Parent as Popup;
                        if (parentPopup != null)
                        {
                            parentPopup.EstablishPopupCapture(isRestoringCapture:true);
                        }
                    }
                }
                _cacheValid[(int)CacheBits.CaptureEngaged] = false;
            }
        }

        /// <summary>
        ///     Called when this element loses capture.
        /// </summary>
        /// <param name="sender">The instance of Popup that caught the event.</param>
        /// <param name="e">Event arguments.</param>
        private static void OnLostMouseCapture(object sender, MouseEventArgs e)
        {
            Popup popup = sender as Popup;

            // Try to accomplish "subcapture" -- allowing elements within our
            // subtree to take mouse capture and reclaim it when they lose capture.
            // This is a workaround until we can get real subcapture:
            //   * Bug 940198: Need real solution for subcapture
            //
            if (!popup.StaysOpen)
            {
                PopupRoot root = popup._popupRoot.Value;

                // Reestablish capture if an element within us lost capture
                // (hence we receive the LostCapture routed event) and capture
                // is not being acquired anywhere else.
                //
                // Note we do not reestablish capture if we are losing capture
                // ourselves.
                bool reestablishCapture = e.OriginalSource != root && Mouse.Captured == null && MS.Win32.SafeNativeMethods.GetCapture() == IntPtr.Zero;

                if(reestablishCapture)
                {
                    popup.EstablishPopupCapture();
                    e.Handled = true;
                }
                else
                {
                    if(Mouse.Captured != root)
                    {
                        popup._cacheValid[(int)CacheBits.CaptureEngaged] = false;
                    }

                    PopupRoot newRoot = Mouse.Captured as PopupRoot;
                    Popup newPopup = (newRoot == null) ? null : newRoot.Parent as Popup;
                    bool childPopupTookCapture = newPopup != null && root != null &&
                        root == ParentPopupRootField.GetValue(newPopup);

                    bool newCaptureInsidePopup = childPopupTookCapture || (Mouse.Captured != null && MenuBase.IsDescendant(root, Mouse.Captured as DependencyObject));
                    bool newCaptureOutsidePopup = !newCaptureInsidePopup && Mouse.Captured != root;
                    if(newCaptureOutsidePopup && !popup.IsDragDropActive)
                    {
                        // Capture is moving outside the popup, and we are not
                        // in a drag/drop operation, so we will lose the ability to
                        // know about mouse actions that should dismiss the
                        // popup, so we proactively dismiss the popup now.
                        popup.SetCurrentValueInternal(IsOpenProperty, BooleanBoxes.FalseBox);
                    }
                }
            }
        }

        private static void OnDragDropStarted(object sender, RoutedEventArgs e)
        {
            Popup popup = (Popup)sender;
            popup.IsDragDropActive = true;
        }

        private static void OnDragDropCompleted(object sender, RoutedEventArgs e)
        {
            Popup popup = (Popup)sender;
            popup.IsDragDropActive = false;

            if (!popup.StaysOpen)
            {
                // A drag drop operation steals capture from the Popup because
                // there is an intermediate hwnd created internally by OleDragDrop
                // which holds capture temporarily.  So upon completion of the
                // operation we re-establish capture.
                popup.EstablishPopupCapture();
            }
        }

        #endregion

        #region IAddChild

        ///<summary>
        /// Called to Add the object as a Child.
        ///</summary>
        ///<param name="value">
        /// Object to add as a child
        ///</param>
        void IAddChild.AddChild(Object value)
        {
            UIElement element = value as UIElement;
            if (element == null && value != null)
            {
                throw new ArgumentException(SR.Get(SRID.UnexpectedParameterType, value.GetType(), typeof(UIElement)), "value");
            }

            this.Child = element;
        }

        ///<summary>
        /// Called when text appears under the tag in markup
        ///</summary>
        ///<param name="text">
        /// Text to Add to the Object
        ///</param>
        void IAddChild.AddText(string text)
        {
            TextBlock lbl = new TextBlock();
            lbl.Text = text;

            Child = lbl;
        }

        #endregion

        #region Tree Behavior

        // Invalidate resources on the popup root
        internal override void OnThemeChanged()
        {
            if (_popupRoot.Value != null)
                TreeWalkHelper.InvalidateOnResourcesChange(_popupRoot.Value, null, ResourcesChangeInfo.ThemeChangeInfo);
        }

        /// <summary>
        /// Blocks ReverseInherited properties from propagating changes to logical/visual parents.
        /// </summary>
        internal override bool BlockReverseInheritance()
        {
            // We want the popup to block reverse inheritance in most cases.
            // In the case that the Popup has a TemplatedParent we don't want
            // to block reverse inheritance because the Popup is considered
            // part of that tree.
            return this.TemplatedParent == null;
        }

        /// <summary>
        ///     Called to get the UI parent of this element when there is
        ///     no visual parent.
        /// </summary>
        /// <returns>
        ///     Returns a non-null value when some framework implementation
        ///     of this method has a non-visual parent connection,
        /// </returns>
        protected internal override DependencyObject GetUIParentCore()
        {
            // If we are in the ether or otherwise the root of the tree and there is a
            // PlacementTarget, then send the event route over there.
            if (Parent == null) // We already know we don't have a visual parent, check logical as well
            {
                UIElement placementTarget = PlacementTarget;
                // Use the placement target as the logical parent while the popup is open
                if (placementTarget != null && (IsOpen || _secHelper.IsWindowAlive()))
                {
                    return placementTarget;
                }
            }

            return base.GetUIParentCore();
        }

        internal override bool IgnoreModelParentBuildRoute(RoutedEventArgs e)
        {
            // When we don't have a parent, we should not be passing
            // input events to our model parent (the placement target)
            // Except for LostMouseCaptureEvents needed for menu/combobox subcapture

            return Parent == null &&
                e.RoutedEvent != Mouse.LostMouseCaptureEvent;
        }

        /// <summary>
        ///     Returns enumerator to logical children.
        /// </summary>
        protected internal override IEnumerator LogicalChildren
        {
            get
            {
                object content = Child;

                if (content == null)
                {
                    return EmptyEnumerator.Instance;
                }

                return new PopupModelTreeEnumerator(this, content);
            }
        }

        private class PopupModelTreeEnumerator : ModelTreeEnumerator
        {
            internal PopupModelTreeEnumerator(Popup popup, object child)
                : base(child)
            {
                Debug.Assert(popup != null, "popup should be non-null.");
                Debug.Assert(child != null, "child should be non-null.");

                _popup = popup;
            }

            protected override bool IsUnchanged
            {
                get
                {
                    return Object.ReferenceEquals(Content, _popup.Child);
                }
            }

            private Popup _popup;
        }

        #endregion

        #region Implementation

        // Gets the root visual of the tree containing child
        private static Visual GetRootVisual(Visual child)
        {
            Debug.Assert(child != null, "child should be non-null");

            DependencyObject parent;
            DependencyObject root = child;
            while ((parent = VisualTreeHelper.GetParent(root)) != null)
            {
                root = parent;
            }
            return root as Visual;
        }

        // Returns the element the popup should position relative to
        private Visual GetTarget()
        {
            Visual targetVisual = PlacementTarget;

            if (targetVisual == null)
            {
                targetVisual = VisualTreeHelper.GetContainingVisual2D(VisualTreeHelper.GetParent(this));
            }

            return targetVisual;
        }

        private void SetHitTestable(bool hitTestable)
        {
            _popupRoot.Value.IsHitTestVisible = hitTestable;

            if (IsTransparent)
            {
                // Make the Win32 window transparent so input is routed under the popup (for tooltips)
                _secHelper.SetHitTestable(hitTestable);
            }
        }

        private static object AsyncCreateWindow(object arg)
        {
            Popup popup = (Popup)arg;
            popup._asyncCreate = null;
            popup.CreateWindow(true /*asyncCall*/);

            return null;
        }

        private void CreateNewPopupRoot()
        {
            if (_popupRoot.Value == null)
            {
                _popupRoot.Value = new PopupRoot();
                AddLogicalChild(_popupRoot.Value);
                // Allow users to set Width/Height properties on the Popup and have them
                // apply to the content.
                _popupRoot.Value.SetupLayoutBindings(this);
            }
        }

        private void CreateWindow(bool asyncCall)
        {
            // Clear any previously cached value and let the current setup make a new determination
            ClearDropOpposite();

            // get target's visual
            Visual targetVisual = GetTarget();
            // defer creation?
            if ((targetVisual != null) && PopupSecurityHelper.IsVisualPresentationSourceNull(targetVisual))
            {
                // This is a case where the Popup is in a tree and its target is not hooked up to a window.
                if (!asyncCall)
                {
                    // We'll defer until later if not already in an async call.
                    _asyncCreate = Dispatcher.BeginInvoke(DispatcherPriority.Input, new DispatcherOperationCallback(AsyncCreateWindow), this);
                }

                return;
            }

            // Clear previously saved position info when opening the window
            if (_positionInfo != null)
            {
                _positionInfo.MouseRect = Rect.Empty;
                _positionInfo.ChildSize = Size.Empty;
            }

            // create a new window?
            bool makeNewWindow = !_secHelper.IsWindowAlive();

            // When running in Per-Monitor DPI aware mode, always create a new window
            // This ensures that a recycled HWND that is moving from one display
            // to another does not undergo a WM_DPICHANGED event and thus cause a 
            // cascading failure.
            if (PopupInitialPlacementHelper.IsPerMonitorDpiScalingActive)
            {
                DestroyWindowImpl();
                _positionInfo = null;
                makeNewWindow = true;
            }

            if (makeNewWindow)
            {
                // create the window
                BuildWindow(targetVisual);
                CreateNewPopupRoot();
            }

            UIElement child = Child;
            if (_popupRoot.Value.Child != child)
            {
                _popupRoot.Value.Child = child;
            }

            // When opening, set the placement target registration
            UpdatePlacementTargetRegistration(null, PlacementTarget);

            UpdateTransform();

            bool isWindowAlive = true;
            if (makeNewWindow)
            {
                // Setting popup root will cause window to resize and reposition
                SetRootVisualToPopupRoot();

                // It is possible that the popup is destroyed while setting the RootVisual
                isWindowAlive = _secHelper.IsWindowAlive();
                if (isWindowAlive)
                {
                    _secHelper.ForceMsaaToUiaBridge(_popupRoot.Value);
                }
            }
            else
            {
                // Update position manually
                UpdatePosition();
                isWindowAlive = _secHelper.IsWindowAlive();
            }

            if (isWindowAlive)
            {
                ShowWindow();
                OnOpened(EventArgs.Empty);
            }
        }

        private void SetRootVisualToPopupRoot()
        {
            if (PopupAnimation != PopupAnimation.None && IsTransparent)
            {
                // When the Popup is transparent, hide the content.
                // Later when the window is made visible, opacity is set to 1
                // This is to prevent the first frame of the popup animations
                // from displaying
                _popupRoot.Value.Opacity = 0.0;
            }

            _secHelper.SetWindowRootVisual(_popupRoot.Value);
        }

        private void BuildWindow(Visual targetVisual)
        {
            // AllowsTransparency is applied to popup only at creation time
            CoerceValue(AllowsTransparencyProperty);
            CoerceValue(HasDropShadowProperty);
            IsTransparent = AllowsTransparency;

            // We many not have attempted to position the popup yet
            //
            // If we don't have prior position information and we are currently running in Per-Monitor DPI Aware mode, 
            // we should build the window by specifying a point on the current monitor. 
            // Doing so ensures that the underlying HWND is created with the right DPI. Otherwise, the HWND that is created at
            // (0,0) and then shown on another monitor with a different DPI, will immediately receive a WM_DPICHANGED message. This
            // will in turn cause the HWND to be resized, and its layout to be updated. This layout-update can result in the dismissal
            // of the popup itself, esp. if this Popup is rooted on another Popup.
            //
            // PopupInitialPlacementHelper.GetPlacementOrigin() will return (0,0) when running in SystemAware and Unaware mode.
            // When running in Per-Monitor DPI Aware mode, this method will obtain the screen coordinates of the (left, top) of
            // the Display on which the PopupRoot is situated, and return that value here. 
            var origin =
                _positionInfo != null
                ? new NativeMethods.POINTSTRUCT(_positionInfo.X, _positionInfo.Y)
                : PopupInitialPlacementHelper.GetPlacementOrigin(this);

            _secHelper.BuildWindow(origin.x, origin.y, targetVisual, IsTransparent, PopupFilterMessage, OnWindowResize, OnDpiChanged);
        }

        /// <summary>
        /// Destroys the underlying window (HWND) if it is alive
        /// </summary>
        /// <returns>true if the window was destroyed, otherwise false</returns>
        private bool DestroyWindowImpl()
        {
            if (_secHelper.IsWindowAlive())
            {
                _secHelper.DestroyWindow(PopupFilterMessage, OnWindowResize, OnDpiChanged);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Destroys the window, and does additional book-keeping
        /// like releasing the capture, raising Closed event, and
        /// clearing placement-target registration
        /// </summary>
        private void DestroyWindow()
        {
            if (_secHelper.IsWindowAlive())
            {
                if (DestroyWindowImpl())
                {
                    ReleasePopupCapture();

                    // Raise closed event after popup has actually closed
                    OnClosed(EventArgs.Empty);

                    // When closing, clear the placement target registration
                    UpdatePlacementTargetRegistration(PlacementTarget, null);
                }
            }
        }

        // Open the window
        private void ShowWindow()
        {
            if (_secHelper.IsWindowAlive())
            {
                _popupRoot.Value.Opacity = 1.0;

                SetupAnimations(true);

                // Always set hittestable for non layered windows
                SetHitTestable(HitTestable || !IsTransparent);
                EstablishPopupCapture();

                _secHelper.ShowWindow();
            }
        }

        // Close the window
        private void HideWindow()
        {
            bool animating = SetupAnimations(false);

            SetHitTestable(false);
            ReleasePopupCapture();

            // NOTE: It is important that we destroy the windows at less than Render priority because Menus will allow
            //       all Render-priority queue items to be processed before firing the click event and we don't want
            //       to have disposed the window at the time that we route the event.
            //       Setting to inactive to allow any animations in ShowWindow to take effect first.

            _asyncDestroy = new DispatcherTimer(DispatcherPriority.Input);
            _asyncDestroy.Tick += delegate(object sender, EventArgs args)
            {
                _asyncDestroy.Stop();
                _asyncDestroy = null;

                DestroyWindow();
            };

            // Wait for the animation (if any) to complete before destroying the window
            _asyncDestroy.Interval = animating ? AnimationDelayTime : TimeSpan.Zero;
            _asyncDestroy.Start();

            if (!animating)
                _secHelper.HideWindow();
        }

        // Starts animations on the popup root
        private bool SetupAnimations(bool visible)
        {
            PopupAnimation animation = PopupAnimation;

            _popupRoot.Value.StopAnimations();

            // Only animate if popup is transparent
            if (animation != PopupAnimation.None && IsTransparent)
            {
                if (animation == PopupAnimation.Fade)
                {
                    _popupRoot.Value.SetupFadeAnimation(AnimationDelayTime, visible);
                    return true;
                }
                else if (visible) // only translate when showing popup
                {
                    // translate the content
                    _popupRoot.Value.SetupTranslateAnimations(animation, AnimationDelayTime, AnimateFromRight, AnimateFromBottom);
                    return true;
                }
            }
            return false;
        }

        private void CancelAsyncCreate()
        {
            if (_asyncCreate != null)
            {
                _asyncCreate.Abort();
                _asyncCreate = null;
            }
        }

        private void CancelAsyncDestroy()
        {
            if (_asyncDestroy != null)
            {
                _asyncDestroy.Stop();
                _asyncDestroy = null;
            }
        }

        internal void ForceClose()
        {
            if (_asyncDestroy != null)
            {
                CancelAsyncDestroy();
                DestroyWindow();
            }
        }

        private IntPtr PopupFilterMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch ((WindowMessage)msg)
            {
                case WindowMessage.WM_MOUSEACTIVATE:
                    // Don't let the popup become active -- we don't want the main window
                    // to become inactive because of the popup.
                    handled = true;
                    return new IntPtr(NativeMethods.MA_NOACTIVATE);

                case WindowMessage.WM_ACTIVATEAPP:
                    if (wParam == IntPtr.Zero)
                    {
                        // The app is deactivating, handle on the correct context
                        Dispatcher.BeginInvoke(DispatcherPriority.Normal, new DispatcherOperationCallback(HandleDeactivateApp), null);
                    }
                    break;

                case WindowMessage.WM_WINDOWPOSCHANGING:
                    if(_secHelper.IsChildPopup)
                    {
                        // The lParam is a pointer to a WINDOWPOS structure
                        // that contains information about the size and
                        // position that the window is changing to.  Note that
                        // modifying this structure during WM_WINDOWPOSCHANGING
                        // will change what happens to the window.
                        unsafe
                        {
                            NativeMethods.WINDOWPOS * windowPos = (NativeMethods.WINDOWPOS *)lParam;

                            // Windows has an optimization to copy pixels
                            // around to reduce the amount of repainting
                            // needed when moving or resizing a window.
                            // Unfortunately, this is not compatible with WPF
                            // in many cases due to our use of DirectX for
                            // rendering from our rendering thread.
                            // To be safe, we disable this optimization and
                            // pay the cost of repainting.
                            windowPos->flags |= NativeMethods.SWP_NOCOPYBITS;
                        }
                    }

                    break;
            }
            return IntPtr.Zero;
        }

        private object HandleDeactivateApp(object arg)
        {
            if (!StaysOpen)
            {
                // If we are in an auto-close mode and the app is deactivating, close the popup.
                SetCurrentValueInternal(IsOpenProperty, BooleanBoxes.FalseBox);
            }

            FirePopupCouldClose();

            return null;
        }

        // Updates the transform applied to the decorator in PopupRoot
        // For non transparent windows, this is restricted to scale transforms only
        private void UpdateTransform()
        {
            // Directly apply layout and render transforms for the popup because collapsed
            // elements' transforms are not accounted for in TransformToAncestor
            Matrix popupTransform = LayoutTransform.Value * RenderTransform.Value;

            // When the popup is not in a visual tree, do not apply the transform from target to root
            DependencyObject parent = VisualTreeHelper.GetParent(this);

            // Sometimes it is possible that the Popup isn't connected to the root through purely
            // visual links. In that case, we can't calculate a transform matrix and we'll get
            // an InvalidOperationException from TransformToAncestor.
            // Since this isn't an illegal state for Popup to be in, we're walking the
            // target's visual parent chain to find the top-most root possible.
            // Catching the exception was rejected by architects.
            Visual rootVisual = parent == null ? null : GetRootVisual(this);
            if (rootVisual != null)
            {
                // Apply all transforms from target to window coordinate space
                popupTransform = popupTransform *                                         //Transform applied directly to popup
                                 TransformToAncestor(rootVisual).AffineTransform.Value *  //Transform between popup and root (Affine only)
                                 PointUtil.GetVisualTransform(rootVisual);                //Transform applied directly to root
            }

            // Transparent popups can have any type of transforms applied to them
            // For non-transparent popups, generate a scale matrix from the original transform
            if (IsTransparent)
            {
                // Undo mirror transform from Flow Direction - popup root will get its own
                if (parent != null && (FlowDirection)parent.GetValue(FlowDirectionProperty) == FlowDirection.RightToLeft)
                {
                    // Undo FlowDirection Mirror
                    popupTransform.Scale(-1.0, 1.0);
                }
            }
            else
            {
                // Only apply scaling transforms
                // Estimate the scale by seeing how much sides on a square grew
                Vector transformedUnitX = popupTransform.Transform(new Vector(1.0, 0.0));
                Vector transformedUnitY = popupTransform.Transform(new Vector(0.0, 1.0));

                // replace the transform with a scale only transform
                popupTransform = new Matrix();
                popupTransform.Scale(transformedUnitX.Length, transformedUnitY.Length);
            }

            _popupRoot.Value.Transform = new MatrixTransform(popupTransform);
        }

        private void OnWindowResize(object sender, AutoResizedEventArgs e)
        {
            // _positionInfo can be null if an exception aborted the measure process.
            // We can't recover from this, but we can let the app/user know what
            // caused the original exception.
            if (_positionInfo == null)
            {
                Exception nre = new NullReferenceException();
                throw new NullReferenceException(nre.Message, SavedException);
            }
            else
            {
                // if the app has recovered from original exception, clear the field
                SavedExceptionField.ClearValue(this);
            }

            if (e.Size != _positionInfo.ChildSize)
            {
                _positionInfo.ChildSize = e.Size;

                // Reposition the popup
                Reposition();
            }
        }

        private void OnDpiChanged(object sender, HwndDpiChangedEventArgs e)
        {
            // Popups do not handle layout updates due to DPI changes very well  when they are visible. 
            // Ignore DPI change induced layout-updates when visible. 
            // This brings the behavior of Popups in line with .NET 4.7.2. Currently, 
            // there is no reliable way to opt-into the DPI improvements made in .NET 4.8
            // wholesale for Popups. By creating the Popups more intelligently on the right
            // target monitor, we will vastly improve the DPI scaling of the Popups
            // in .NET 4.8. In rare situations where a DPI change requires a visible Popups
            // to adapt and resize itself on-the-fly while continuing to remain visible, 
            // it will fail to adapt to that particular DPI change.
            if (IsOpen)
            {
                e.Handled = true;
            }
        }

        #region Saved exception

        // When an exception aborts the measure of PopupRoot's subtree, we save
        // it in an uncommon field.  The exception can cause a null-reference much
        // later on (during a subsequent and unrelated layout pass), by which time
        // it's impossible to determine what happened.   We mitigate this by
        // reporting the original exception as the InnerException of the null-ref.

        private static readonly UncommonField<Exception> SavedExceptionField = new UncommonField<Exception>();

        internal Exception SavedException
        {
            get { return SavedExceptionField.GetValue(this); }
            set { SavedExceptionField.SetValue(this, value); }
        }

        #endregion Saved exception

        #region Positioning

        /// <summary>
        /// Reposition the Popup
        /// </summary>
        internal void Reposition()
        {
            if (IsOpen && _secHelper.IsWindowAlive())
            {
                if (CheckAccess())
                {
                    UpdatePosition();
                }
                else
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, new DispatcherOperationCallback(delegate(object param)
                    {
                        Debug.Assert(CheckAccess(), "AsyncReposition not called on the dispatcher thread.");

                        Reposition();

                        return null;
                    }), null);
                }
            }
        }

        private static bool IsAbsolutePlacementMode(PlacementMode placement)
        {
            switch (placement)
            {
                case PlacementMode.MousePoint:
                case PlacementMode.Mouse:
                case PlacementMode.AbsolutePoint:
                case PlacementMode.Absolute:
                    return true;
            }

            return false;
        }

        // Indicies into InterestPoint point array
        private enum InterestPoint
        {
            TopLeft     = 0,
            TopRight    = 1,
            BottomLeft  = 2,
            BottomRight = 3,
            Center      = 4,
        }

        // This struct is returned by GetPointCombination to indicate
        // which points on the target can align with points on the child
        private struct PointCombination
        {
            public PointCombination(InterestPoint targetInterestPoint, InterestPoint childInterestPoint)
            {
                TargetInterestPoint = targetInterestPoint;
                ChildInterestPoint = childInterestPoint;
            }

            public InterestPoint TargetInterestPoint;
            public InterestPoint ChildInterestPoint;
        }

        private class PositionInfo
        {
            // The position of the upper left corner of the popup after nudging
            public int X;
            public int Y;

            // The size of the popup
            public Size ChildSize;

            // The screen rect of the mouse
            public Rect MouseRect = Rect.Empty;
        }

        // To position the popup, we find the InterestPoints of the placement rectangle/point
        // in the screen coordinate space.  We also find the InterestPoints of the child in
        // the popup's space.  Then we attempt all valid combinations of matching InterestPoints
        // (based on PlacementMode) to find the position that best fits on the screen.
        // NOTE: any reference to the screen implies the monitor for full trust and
        //       the browser area for partial trust
        private void UpdatePosition()
        {
            if (_popupRoot.Value == null)
                return;

            PlacementMode placement = PlacementInternal;

            // Get a list of the corners of the target/child in screen space
            Point[] placementTargetInterestPoints = GetPlacementTargetInterestPoints(placement);
            Point[] childInterestPoints = GetChildInterestPoints(placement);

            // Find bounds of screen and child in screen space
            Rect targetBounds = GetBounds(placementTargetInterestPoints);
            Rect screenBounds;
            Rect childBounds = GetBounds(childInterestPoints);

            double childArea = childBounds.Width * childBounds.Height;

            // Rank possible positions
            int bestIndex = -1;
            Vector bestTranslation = new Vector(_positionInfo.X, _positionInfo.Y);
            double bestScore = -1;
            PopupPrimaryAxis bestAxis = PopupPrimaryAxis.None;

            int positions;

            CustomPopupPlacement[] customPlacements = null;

            // Find the number of possible positions
            if (placement == PlacementMode.Custom)
            {
                CustomPopupPlacementCallback customCallback = CustomPopupPlacementCallback;
                if (customCallback != null)
                {
                    customPlacements = customCallback(childBounds.Size, targetBounds.Size, new Point(HorizontalOffset, VerticalOffset));
                }
                positions = customPlacements == null ? 0 : customPlacements.Length;

                // Return if callback closed the popup
                if (!IsOpen)
                    return;
            }
            else
            {
                positions = GetNumberOfCombinations(placement);
            }

            // Try each position until the best one is found
            for (int i = 0; i < positions; i++)
            {
                Vector popupTranslation;

                bool animateFromRight = false;
                bool animateFromBottom = false;

                PopupPrimaryAxis axis;

                // Get the ith Position to rank
                if (placement == PlacementMode.Custom)
                {
                    // The custom callback only calculates relative to 0,0
                    // so the placementTarget's top/left need to be re-applied.
                    popupTranslation = ((Vector)placementTargetInterestPoints[(int)InterestPoint.TopLeft])
                                      + ((Vector)customPlacements[i].Point);  // vector from origin

                    axis = customPlacements[i].PrimaryAxis;
                }
                else
                {
                    PointCombination pointCombination = GetPointCombination(placement, i, out axis);

                    InterestPoint targetInterestPoint = pointCombination.TargetInterestPoint;
                    InterestPoint childInterestPoint = pointCombination.ChildInterestPoint;

                    // Compute the vector from the screen origin to the top left corner of the popup
                    // that will cause the the two interest points to overlap
                    popupTranslation = placementTargetInterestPoints[(int)targetInterestPoint]
                                       - childInterestPoints[(int)childInterestPoint];

                    // Check the matching points to see which direction to animate
                    animateFromRight = childInterestPoint == InterestPoint.TopRight || childInterestPoint == InterestPoint.BottomRight;
                    animateFromBottom = childInterestPoint == InterestPoint.BottomLeft || childInterestPoint == InterestPoint.BottomRight;
                }

                // Find percent of popup on screen by translating the popup bounds
                // and calculating the percent of the bounds that is on screen
                // Note: this score is based on the percent of the popup that is on screen
                //       not the percent of the child that is on screen.  For certain
                //       scenarios, this may produce in counter-intuitive results.
                //       If this is a problem, more complex scoring is needed
                Rect tranlsatedChildBounds = Rect.Offset(childBounds, popupTranslation);
                screenBounds = GetScreenBounds(targetBounds, placementTargetInterestPoints[(int)InterestPoint.TopLeft]);
                Rect currentIntersection = Rect.Intersect(screenBounds, tranlsatedChildBounds);

                // Calculate area of intersection
                double score = currentIntersection != Rect.Empty ? currentIntersection.Width * currentIntersection.Height : 0;

                // If current score is better than the best score so far, save the position info
                if (score - bestScore > Tolerance)
                {
                    bestIndex = i;
                    bestTranslation = popupTranslation;
                    bestScore = score;
                    bestAxis = axis;

                    AnimateFromRight = animateFromRight;
                    AnimateFromBottom = animateFromBottom;

                    // Stop when we find a popup that is completely on screen
                    if (Math.Abs(score - childArea) < Tolerance)
                    {
                        break;
                    }
                }
            }

            // When going left/right, if the edge of the monitor is hit
            // the next popup going left/right must also go in the opposite direction
            if ((bestIndex >= 2) && (placement == PlacementMode.Right || placement == PlacementMode.Left))
            {
                // We switched sides, so flip the DropOpposite flag
                DropOpposite = !DropOpposite;
            }

            // Check to see if the pop needs to be nudged onto the screen.
            // Popups are not nudged if their axes do not align with the screen axes

            // Use the size of the popupRoot in case it is clipping the popup content
            childBounds = new Rect((Size)_secHelper.GetTransformToDevice().Transform((Point)_popupRoot.Value.RenderSize));

            childBounds.Offset(bestTranslation);
            screenBounds = GetScreenBounds(targetBounds, placementTargetInterestPoints[(int)InterestPoint.TopLeft]);
            Rect intersection = Rect.Intersect(screenBounds, childBounds);

            // See if width/height of intersection are less than child's
            if (Math.Abs(intersection.Width - childBounds.Width) > Tolerance ||
                Math.Abs(intersection.Height - childBounds.Height) > Tolerance)
            {
                // Nudge Horizontally
                Point topLeft = placementTargetInterestPoints[(int)InterestPoint.TopLeft];
                Point topRight = placementTargetInterestPoints[(int)InterestPoint.TopRight];

                // Create a vector pointing from the top of the placement target to the bottom
                // to determine which direction the popup should be nudged in.
                // If the vector is zero (NaN's after normalization), nudge horizontally
                Vector horizontalAxis = topRight - topLeft;
                horizontalAxis.Normalize();

                // See if target's horizontal axis is aligned with screen
                // (For opaque windows always translate horizontally)
                if (!IsTransparent || double.IsNaN(horizontalAxis.Y) || Math.Abs(horizontalAxis.Y) < Tolerance)
                {
                    // Nudge horizontally
                    if (childBounds.Right > screenBounds.Right)
                    {
                        bestTranslation.X = screenBounds.Right - childBounds.Width;
                    }
                    else if (childBounds.Left < screenBounds.Left)
                    {
                        bestTranslation.X = screenBounds.Left;
                    }
                }
                else if (IsTransparent && Math.Abs(horizontalAxis.X) < Tolerance)
                {
                    // Nudge vertically, limit horizontally
                    if (childBounds.Bottom > screenBounds.Bottom)
                    {
                        bestTranslation.Y = screenBounds.Bottom - childBounds.Height;
                    }
                    else if (childBounds.Top < screenBounds.Top)
                    {
                        bestTranslation.Y = screenBounds.Top;
                    }
                }

                // Nudge Vertically
                Point bottomLeft = placementTargetInterestPoints[(int)InterestPoint.BottomLeft];

                // Create a vector pointing from the top of the placement target to the bottom
                // to determine which direction the popup should be nudged in
                // If the vector is zero (NaN's after normalization), nudge vertically
                Vector verticalAxis = topLeft - bottomLeft;
                verticalAxis.Normalize();

                // Axis is aligned with screen, nudge
                if (!IsTransparent || double.IsNaN(verticalAxis.X) || Math.Abs(verticalAxis.X) < Tolerance)
                {
                    if (childBounds.Bottom > screenBounds.Bottom)
                    {
                        bestTranslation.Y = screenBounds.Bottom - childBounds.Height;
                    }
                    else if (childBounds.Top < screenBounds.Top)
                    {
                        bestTranslation.Y = screenBounds.Top;
                    }
                }
                else if (IsTransparent && Math.Abs(verticalAxis.Y) < Tolerance)
                {
                    if (childBounds.Right > screenBounds.Right)
                    {
                        bestTranslation.X = screenBounds.Right - childBounds.Width;
                    }
                    else if (childBounds.Left < screenBounds.Left)
                    {
                        bestTranslation.X = screenBounds.Left;
                    }
                }
            }

            // Finally, take the best position and apply it to the popup
            int bestX = DoubleUtil.DoubleToInt(bestTranslation.X);
            int bestY = DoubleUtil.DoubleToInt(bestTranslation.Y);
            if (bestX != _positionInfo.X || bestY != _positionInfo.Y)
            {
                _positionInfo.X = bestX;
                _positionInfo.Y = bestY;
                _secHelper.SetPopupPos(true, bestX, bestY, false, 0, 0);
            }
        }

        // Finds the screen size and limiting dimension.
        // Popups are restricted in the orthogonal dimension to the primary/nudge axis
        // This prevents the popup from overlapping the placement target.
        private void GetPopupRootLimits(out Rect targetBounds, out Rect screenBounds, out Size limitSize)
        {
            PlacementMode placement = PlacementInternal;

            // Get a list of the corners of the target/child in screen space
            Point[] placementTargetInterestPoints = GetPlacementTargetInterestPoints(placement);

            // Find bounds of screen and child in screen space
            targetBounds = GetBounds(placementTargetInterestPoints);
            screenBounds = GetScreenBounds(targetBounds, placementTargetInterestPoints[(int)InterestPoint.TopLeft]);

            PopupPrimaryAxis nudgeAxis = GetPrimaryAxis(placement);

            limitSize = new Size(Double.PositiveInfinity, Double.PositiveInfinity);

            if (nudgeAxis == PopupPrimaryAxis.Horizontal)
            {
                // limit vertically
                Point topLeft = placementTargetInterestPoints[(int)InterestPoint.TopLeft];
                Point bottomLeft = placementTargetInterestPoints[(int)InterestPoint.BottomLeft];

                // Create a vector pointing from the top of the placement target to the bottom
                // to determine which direction the popup should be restricted in
                // If the vector is zero (NaN's after normalization), restrict vertically
                Vector verticalAxis = bottomLeft - topLeft;
                verticalAxis.Normalize();

                // Axis is aligned with screen, limit
                if (!IsTransparent || double.IsNaN(verticalAxis.X) || Math.Abs(verticalAxis.X) < Tolerance)
                {
                    limitSize.Height = Math.Max(0.0, Math.Max(screenBounds.Bottom - targetBounds.Bottom, targetBounds.Top - screenBounds.Top));
                }
                else if (IsTransparent && Math.Abs(verticalAxis.Y) < Tolerance)
                {
                    limitSize.Width = Math.Max(0.0, Math.Max(screenBounds.Right - targetBounds.Right, targetBounds.Left - screenBounds.Left));
                }
            }
            else if (nudgeAxis == PopupPrimaryAxis.Vertical)
            {
                // limit horizontally
                Point topLeft = placementTargetInterestPoints[(int)InterestPoint.TopLeft];
                Point topRight = placementTargetInterestPoints[(int)InterestPoint.TopRight];

                // Create a vector pointing from the left of the placement target to the right
                // to determine which direction the popup should be restricted in
                // If the vector is zero (NaN's after normalization), restrict horizontally
                Vector horizontalAxis = topRight - topLeft;
                horizontalAxis.Normalize();

                // Axis is aligned with screen, limit
                if (!IsTransparent || double.IsNaN(horizontalAxis.X) || Math.Abs(horizontalAxis.Y) < Tolerance)
                {
                    limitSize.Width = Math.Max(0.0, Math.Max(screenBounds.Right - targetBounds.Right, targetBounds.Left - screenBounds.Left));
                }
                else if (IsTransparent && Math.Abs(horizontalAxis.X) < Tolerance)
                {
                    limitSize.Height = Math.Max(0.0, Math.Max(screenBounds.Bottom - targetBounds.Bottom, targetBounds.Top - screenBounds.Top));
                }
            }
        }

        // Retrieves a list of the interesting points of the popup target in screen space
        private Point[] GetPlacementTargetInterestPoints(PlacementMode placement)
        {
            if (_positionInfo == null)
            {
                _positionInfo = new PositionInfo();
            }

            // Calculate the placement rectangle, which is the rectangle that popup will position relative to.
            Rect placementRect = PlacementRectangle;

            Point[] interestPoints;

            UIElement target = GetTarget() as UIElement;

            Vector offset = new Vector(HorizontalOffset, VerticalOffset);

            // Popup positioning is based on the PlacementTarget or the Placement mode
            if (target == null || IsAbsolutePlacementMode(placement))
            {
                // When the Mode is Mouse, the placement rectangle is the mouse position
                if (placement == PlacementMode.Mouse || placement == PlacementMode.MousePoint)
                {
                    if (_positionInfo.MouseRect == Rect.Empty)
                    {
                        // Everytime something changes we will reposition the popup.  We generally don't
                        // want to get a new position for the mouse at every reposition (for example,
                        // if the popup's content size is animated the popup will keep repositioning,
                        // but we should not pick up a new position for the mouse).
                        _positionInfo.MouseRect = GetMouseRect(placement);
                    }

                    placementRect = _positionInfo.MouseRect;
                }
                else if (placementRect == Rect.Empty)
                {
                    placementRect = new Rect();
                }

                offset = _secHelper.GetTransformToDevice().Transform(offset);

                // Offset the rect
                placementRect.Offset(offset);

                // These points are already positioned in screen coordinates
                // no transformations are necessary
                interestPoints = InterestPointsFromRect(placementRect);
            }
            else
            {
                // If no rectangle was given, then use the render bounds of the target
                if (placementRect == Rect.Empty)
                {
                    if (placement != PlacementMode.Relative && placement != PlacementMode.RelativePoint)
                        placementRect = new Rect(0.0, 0.0, target.RenderSize.Width, target.RenderSize.Height);
                    else // For relative and relative point use upperleft corner of target
                        placementRect = new Rect();
                }

                // Offset the rect
                placementRect.Offset(offset);

                // Get the points int the target's coordinate space
                interestPoints = InterestPointsFromRect(placementRect);

                // Next transform from the target's space to the screen space
                Visual rootVisual = GetRootVisual(target);
                GeneralTransform targetToClientTransform = TransformToClient(target, rootVisual);

                // transform point to the screen coordinate space
                for (int i = 0; i < 5; i++)
                {
                    targetToClientTransform.TryTransform(interestPoints[i], out interestPoints[i]);

                    interestPoints[i] = _secHelper.ClientToScreen(rootVisual, interestPoints[i]);
                }
            }

            return interestPoints;
        }

        private static void SwapPoints(ref Point p1, ref Point p2)
        {
            Point temp = p1;
            p1 = p2;
            p2 = temp;
        }

        // Retrieves a list of the interesting points of the popups child in the popup window space
        private Point[] GetChildInterestPoints(PlacementMode placement)
        {
            UIElement child = Child;

            if (child == null)
            {
                return InterestPointsFromRect(new Rect());
            }

            Point[] interestPoints = InterestPointsFromRect(new Rect(new Point(), child.RenderSize));


            UIElement target = GetTarget() as UIElement;

            // Popup positioning is based on the PlacementTarget or the Placement mode
            if (target != null && !IsAbsolutePlacementMode(placement))
            {
                // In scenarios where the flow direction is different between the
                // child and target, the child rect should be treated as it is flipped
                if ((FlowDirection)target.GetValue(FlowDirectionProperty) !=
                    (FlowDirection)child.GetValue(FlowDirectionProperty))
                {
                    SwapPoints(ref interestPoints[(int)InterestPoint.TopLeft], ref interestPoints[(int)InterestPoint.TopRight]);
                    SwapPoints(ref interestPoints[(int)InterestPoint.BottomLeft], ref interestPoints[(int)InterestPoint.BottomRight]);
                }
            }

            // Use remove the render transform translation from the child
            Vector offset = _popupRoot.Value.AnimationOffset;

            // Transform InterestPoints to popup's space
            GeneralTransform childToPopupTransform = TransformToClient(child, _popupRoot.Value);

            for (int i = 0; i < 5; i++)
            {
                // subtract Animation offset and transform point to the screen coordinate space
                childToPopupTransform.TryTransform(interestPoints[i] - offset, out interestPoints[i]);
            }

            return interestPoints;
        }

        // Returns an array of the InterestPoints of the Rect, each displaced by offset
        private static Point[] InterestPointsFromRect(Rect rect)
        {
            Point[] points = new Point[5];

            points[(int)InterestPoint.TopLeft] = rect.TopLeft;
            points[(int)InterestPoint.TopRight] = rect.TopRight;
            points[(int)InterestPoint.BottomLeft] = rect.BottomLeft;
            points[(int)InterestPoint.BottomRight] = rect.BottomRight;
            points[(int)InterestPoint.Center] = new Point(rect.Left + rect.Width / 2.0,
                                                          rect.Top + rect.Height / 2.0);

            return points;
        }

        // Returns a transform from visual to client area of the window
        private static GeneralTransform TransformToClient(Visual visual, Visual rootVisual)
        {
            GeneralTransformGroup visualToClientTransform = new GeneralTransformGroup();

            // Add transform from visual to root
            visualToClientTransform.Children.Add(visual.TransformToAncestor(rootVisual));

            // Add root and composition target's transfrom
            visualToClientTransform.Children.Add(new MatrixTransform(
                PointUtil.GetVisualTransform(rootVisual) *
                PopupSecurityHelper.GetTransformToDevice(rootVisual)
                ));

            return visualToClientTransform;
        }

        // Gets the smallest rectangle that contains all points in the list
        private Rect GetBounds(Point[] interestPoints)
        {
            double left, right, top, bottom;

            left = right = interestPoints[0].X;
            top = bottom = interestPoints[0].Y;

            for (int i = 1; i < interestPoints.Length; i++)
            {
                double x = interestPoints[i].X;
                double y = interestPoints[i].Y;
                if (x < left)   left = x;
                if (x > right)  right = x;
                if (y < top)    top = y;
                if (y > bottom) bottom = y;
            }
            return new Rect(left, top, right - left, bottom - top);
        }

        // Gets the number of InterestPoint combinations for the given placement
        private static int GetNumberOfCombinations(PlacementMode placement)
        {
            switch (placement)
            {
                case PlacementMode.Bottom:
                case PlacementMode.Top:
                case PlacementMode.Mouse:
                    return 2;

                case PlacementMode.Right:
                case PlacementMode.Left:
                case PlacementMode.RelativePoint:
                case PlacementMode.MousePoint:
                case PlacementMode.AbsolutePoint:
                    return 4;

                case PlacementMode.Custom:
                    return 0;

                case PlacementMode.Absolute:
                case PlacementMode.Relative:
                case PlacementMode.Center:
                default:
                    return 1;
            }
        }

        // Returns the ith possible alignment for the given PlacementMode
        private PointCombination GetPointCombination(PlacementMode placement, int i, out PopupPrimaryAxis axis)
        {
            Debug.Assert(i >= 0 && i < GetNumberOfCombinations(placement));

            bool dropFromRight = SystemParameters.MenuDropAlignment;

            switch (placement)
            {
                case PlacementMode.Bottom:
                case PlacementMode.Mouse:
                    axis = PopupPrimaryAxis.Horizontal;
                    if (dropFromRight)
                    {
                        if (i == 0) return new PointCombination(InterestPoint.BottomRight, InterestPoint.TopRight);
                        if (i == 1) return new PointCombination(InterestPoint.TopRight, InterestPoint.BottomRight);
                    }
                    else
                    {
                        if (i == 0) return new PointCombination(InterestPoint.BottomLeft, InterestPoint.TopLeft);
                        if (i == 1) return new PointCombination(InterestPoint.TopLeft, InterestPoint.BottomLeft);
                    }
                    break;


                case PlacementMode.Top:
                    axis = PopupPrimaryAxis.Horizontal;
                    if (dropFromRight)
                    {
                        if (i == 0) return new PointCombination(InterestPoint.TopRight, InterestPoint.BottomRight);
                        if (i == 1) return new PointCombination(InterestPoint.BottomRight, InterestPoint.TopRight);
                    }
                    else
                    {
                        if (i == 0) return new PointCombination(InterestPoint.TopLeft, InterestPoint.BottomLeft);
                        if (i == 1) return new PointCombination(InterestPoint.BottomLeft, InterestPoint.TopLeft);
                    }
                    break;


                case PlacementMode.Right:
                case PlacementMode.Left:
                    axis = PopupPrimaryAxis.Vertical;
                    dropFromRight |= DropOpposite;

                    if ((dropFromRight && placement == PlacementMode.Right) ||
                        (!dropFromRight && placement == PlacementMode.Left))
                    {
                        if (i == 0) return new PointCombination(InterestPoint.TopLeft, InterestPoint.TopRight);
                        if (i == 1) return new PointCombination(InterestPoint.BottomLeft, InterestPoint.BottomRight);
                        if (i == 2) return new PointCombination(InterestPoint.TopRight, InterestPoint.TopLeft);
                        if (i == 3) return new PointCombination(InterestPoint.BottomRight, InterestPoint.BottomLeft);
                    }
                    else
                    {
                        if (i == 0) return new PointCombination(InterestPoint.TopRight, InterestPoint.TopLeft);
                        if (i == 1) return new PointCombination(InterestPoint.BottomRight, InterestPoint.BottomLeft);
                        if (i == 2) return new PointCombination(InterestPoint.TopLeft, InterestPoint.TopRight);
                        if (i == 3) return new PointCombination(InterestPoint.BottomLeft, InterestPoint.BottomRight);
                    }
                    break;

                case PlacementMode.Relative:
                case PlacementMode.RelativePoint:
                case PlacementMode.MousePoint:
                case PlacementMode.AbsolutePoint:
                    axis = PopupPrimaryAxis.Horizontal;
                    if (dropFromRight)
                    {
                        if (i == 0) return new PointCombination(InterestPoint.TopLeft, InterestPoint.TopRight);
                        if (i == 1) return new PointCombination(InterestPoint.TopLeft, InterestPoint.TopLeft);
                        if (i == 2) return new PointCombination(InterestPoint.TopLeft, InterestPoint.BottomRight);
                        if (i == 3) return new PointCombination(InterestPoint.TopLeft, InterestPoint.BottomLeft);
                    }
                    else
                    {
                        if (i == 0) return new PointCombination(InterestPoint.TopLeft, InterestPoint.TopLeft);
                        if (i == 1) return new PointCombination(InterestPoint.TopLeft, InterestPoint.TopRight);
                        if (i == 2) return new PointCombination(InterestPoint.TopLeft, InterestPoint.BottomLeft);
                        if (i == 3) return new PointCombination(InterestPoint.TopLeft, InterestPoint.BottomRight);
                    }
                    break;

                case PlacementMode.Center:
                    axis = PopupPrimaryAxis.None;
                    return new PointCombination(InterestPoint.Center, InterestPoint.Center);

                case PlacementMode.Absolute:
                case PlacementMode.Custom:
                default:
                    axis = PopupPrimaryAxis.None;
                    return new PointCombination(InterestPoint.TopLeft, InterestPoint.TopLeft);
            }

            return new PointCombination(InterestPoint.TopLeft, InterestPoint.TopRight);
        }

        // Gets the primary axis for the specified placement mode
        private static PopupPrimaryAxis GetPrimaryAxis(PlacementMode placement)
        {
            switch (placement)
            {
                case PlacementMode.Right:
                case PlacementMode.Left:
                    return PopupPrimaryAxis.Vertical;

                case PlacementMode.Bottom:
                case PlacementMode.Top:
                case PlacementMode.RelativePoint:
                case PlacementMode.AbsolutePoint:
                    return PopupPrimaryAxis.Horizontal;

                case PlacementMode.Relative:
                case PlacementMode.Mouse:
                case PlacementMode.MousePoint:
                case PlacementMode.Center:
                case PlacementMode.Absolute:
                case PlacementMode.Custom:
                default:
                    return PopupPrimaryAxis.None;
            }
        }

        // Limit size to 75% of maxDimension's area and restrict to be smaller than limitDimension
        internal Size RestrictSize(Size desiredSize)
        {
            // Make sure screen bounds and limit dimensions are up to date
            Rect targetBounds, screenBounds;
            Size limitSize;
            GetPopupRootLimits(out targetBounds, out screenBounds, out limitSize);

            // Convert from popup's space to screen space
            desiredSize = (Size)_secHelper.GetTransformToDevice().Transform((Point)desiredSize);

            desiredSize.Width = Math.Min(desiredSize.Width, screenBounds.Width);
            desiredSize.Width = Math.Min(desiredSize.Width, limitSize.Width);

            double maxHeight = RestrictPercentage * screenBounds.Width * screenBounds.Height / desiredSize.Width;

            desiredSize.Height = Math.Min(desiredSize.Height, screenBounds.Height);
            desiredSize.Height = Math.Min(desiredSize.Height, maxHeight);
            desiredSize.Height = Math.Min(desiredSize.Height, limitSize.Height);

            // Convert back from screen space to popup's space
            desiredSize = (Size)_secHelper.GetTransformFromDevice().Transform((Point)desiredSize);

            return desiredSize;
        }

        // Paramter: Point p should be the most interesting point of a pop up positioning.  The top left.
        // Return the maximum boundingRect for the popup.
        // If this is not a child-popup
        //      and the Point p passed in is inside of the Work Area then return the monitor work area rect
        //          A tooltip opened above the taskbar will never display over/under the taskbar.
        //          To accomodate this the work area of the screen is returned to allow the pop up to
        //          respect the reserved area of the teskbar.
        //       and the Point p passed in is outside of the work area then return the monitor rect
        //          this can happen If the program is an appbar program (http://msdn.microsoft.com/en-us/library/cc144177(VS.85).aspx)
        //          and the tooltip is in the area removed from the work area.  In this case the tooltip can
        //          be placed without regard for the work area bounds.
        // Else this is the BoundingRect of either the placement target's window or the parent of the popup's window.
        private Rect GetScreenBounds(Rect boundingBox, Point p)
        {
            if (_secHelper.IsChildPopup)
            {
                // The "monitor" is the main window for child windows.
                return _secHelper.GetParentWindowRect();
            }

            NativeMethods.RECT rect = new NativeMethods.RECT(0, 0, 0, 0);

            NativeMethods.RECT nativeBounds = PointUtil.FromRect(boundingBox);
            IntPtr monitor = SafeNativeMethods.MonitorFromRect(ref nativeBounds, NativeMethods.MONITOR_DEFAULTTONEAREST);

            if (monitor != IntPtr.Zero)
            {
                NativeMethods.MONITORINFOEX monitorInfo = new NativeMethods.MONITORINFOEX();

                monitorInfo.cbSize = Marshal.SizeOf(typeof(NativeMethods.MONITORINFOEX));
                SafeNativeMethods.GetMonitorInfo(new HandleRef(null, monitor), monitorInfo);

                //If this is a pop up for a menu or ToolTip then respect the work area if opening in the work area.
                if (((this.Child is MenuBase)
                    || (this.Child is ToolTip)
                    || (this.TemplatedParent is MenuItem))
                    && ((p.X >= monitorInfo.rcWork.left)
                        && (p.X <= monitorInfo.rcWork.right)
                        && (p.Y >= monitorInfo.rcWork.top)
                        && (p.Y <= monitorInfo.rcWork.bottom)))
                {
                    // Context Menus, MenuItems, and ToolTips shouldn't go over the Taskbar
                    rect = monitorInfo.rcWork;
                }
                else
                {
                    rect = monitorInfo.rcMonitor;
                }
            }

            return PointUtil.ToRect(rect);
        }

        private Rect GetMouseRect(PlacementMode placement)
        {
            NativeMethods.POINT mousePoint = _secHelper.GetMouseCursorPos(GetTarget());

            if (placement == PlacementMode.Mouse)
            {
                // In Mouse mode, the bounding box of the mouse cursor becomes the target
                int cursorWidth, cursorHeight, hotX, hotY;
                GetMouseCursorSize(out cursorWidth, out cursorHeight, out hotX, out hotY);

                // Add a margin of 1 px above and below the mouse
                return new Rect(mousePoint.x, mousePoint.y - 1, Math.Max(0, cursorWidth - hotX), Math.Max(0, cursorHeight - hotY + 2));
            }
            else
            {
                // In MousePoint mode, the mouse position is the target
                return new Rect(mousePoint.x, mousePoint.y, 0, 0);
            }
        }

        // Not interested in the unmanaged error codes. Error return values detected and handled. Extended error information not needed.
#pragma warning disable 6523

        /// <summary>
        ///     Returns information about the mouse cursor size.
        /// </summary>
        /// <param name="width">The width of the mouse cursor.</param>
        /// <param name="height">The height of the mouse cursor.</param>
        /// <param name="hotX">The X position of the hotspot.</param>
        /// <param name="hotY">The Y position of the hotspot.</param>
        private static void GetMouseCursorSize(out int width, out int height, out int hotX, out int hotY)
        {
            /*
                The code for this function is based upon
                shell\comctl32\v6\tooltips.cpp _GetHcursorPdy3
                -------------------------------------------------------------------------
                With the current mouse drivers that allow you to customize the mouse
                pointer size, GetSystemMetrics returns useless values regarding
                that pointer size.

                Assumption:
                1. The pointer's width is equal to its height. We compute
                   its height and infer its width.

                This function looks at the mouse pointer bitmap
                to find out the dimensions of the mouse pointer and the
                hot spot location.
                -------------------------------------------------------------------------
            */

            // If there is no mouse cursor, these should be 0
            width = height = hotX = hotY = 0;

            // First, retrieve the mouse cursor
            IntPtr hCursor = SafeNativeMethods.GetCursor();
            if (hCursor != IntPtr.Zero)
            {
                // In case we can't figure out the dimensions, this is a best guess
                width = height = 16;

                // Get the cursor information
                NativeMethods.ICONINFO iconInfo = new NativeMethods.ICONINFO();
                bool gotIconInfo = true;
                try
                {
                    UnsafeNativeMethods.GetIconInfo(new HandleRef(null, hCursor), out iconInfo);
                }
                catch(Win32Exception)
                {
                    gotIconInfo = false;
                }

                if(gotIconInfo)
                {
                    // Get a handle to the bitmap
                    NativeMethods.BITMAP bm = new NativeMethods.BITMAP();
                    int resultOfGetObject =  UnsafeNativeMethods.GetObject(iconInfo.hbmMask.MakeHandleRef(null), Marshal.SizeOf(typeof(NativeMethods.BITMAP)), bm);

                    if (resultOfGetObject != 0)
                    {
                        // Extract the bitmap bits
                        int max = (bm.bmWidth * bm.bmHeight / 8);
                        byte[] curMask = new byte[max * 2]; // Enough space for the mask and the xor mask
                        if (UnsafeNativeMethods.GetBitmapBits(iconInfo.hbmMask.MakeHandleRef(null), curMask.Length, curMask) != 0)
                        {
                            bool hasXORMask = false;
                            if (iconInfo.hbmColor.IsInvalid)
                            {
                                // if no color bitmap, then the hbmMask is a double height bitmap
                                // with the cursor and the mask stacked.
                                hasXORMask = true;
                                max /= 2;
                            }

                            // Go through the bitmap looking for the bottom of the image and/or mask
                            bool empty = true;
                            int bottom = max;
                            for (bottom--; bottom >= 0; bottom--)
                            {
                                if (curMask[bottom] != 0xFF || (hasXORMask && (curMask[bottom + max] != 0)))
                                {
                                    empty = false;
                                    break;
                                }
                            }

                            if (!empty)
                            {
                                // Go through the bitmap looking for the top of the image and/or mask
                                int top;
                                for (top = 0; top < max; top++)
                                {
                                    if (curMask[top] != 0xFF || (hasXORMask && (curMask[top + max] != 0)))
                                        break;
                                }

                                // Calculate the left, right, top, bottom points

                                // byteWidth = bytes per row AND bytes per vertical pixel
                                int byteWidth = bm.bmWidth / 8;
                                int right /*px*/ = (bottom /*bytes*/ % byteWidth) * 8 /*px/byte*/;
                                bottom /*px*/ = bottom /*bytes*/ / byteWidth /*bytes/px*/;
                                int left /*px*/ = top /*bytes*/ % byteWidth * 8 /*px/byte*/;
                                top /*px*/ = top /*bytes*/ / byteWidth /*bytes/px*/;

                                // (Final value) Convert LRTB to Width and Height
                                width = right - left + 1;
                                height = bottom - top + 1;

                                // (Final value) Calculate the hotspot relative to top/left
                                hotX = iconInfo.xHotspot - left;
                                hotY = iconInfo.yHotspot - top;
                            }
                            else
                            {
                                // (Final value) We didn't find anything in the bitmap.
                                // So, we'll make a guess with the information that we have.
                                // Note: This seems to happen on I-Beams and Cross-hairs -- cursors that
                                // are all inverted. Strangely, their hbmColor is non-null.
                                width = bm.bmWidth;
                                height = bm.bmHeight;
                                hotX = iconInfo.xHotspot;
                                hotY = iconInfo.yHotspot;
                            }
                        }
                    }

                    iconInfo.hbmColor.Dispose();
                    iconInfo.hbmMask.Dispose();
                }
            }
        }

        internal Rect GetParentWindowRect()
        {
            return _secHelper.GetParentWindowRect();
        }

        internal Rect GetWindowRect()
        {
            return _secHelper.GetWindowRect();
        }

#pragma warning restore 6523

        #endregion Positioning


        #endregion

        #region Private Properties

        private bool IsTransparent
        {
            get { return _cacheValid[(int)CacheBits.IsTransparent]; }
            set { _cacheValid[(int)CacheBits.IsTransparent] = value; }
        }

        private bool AnimateFromRight
        {
            get { return _cacheValid[(int)CacheBits.AnimateFromRight]; }
            set { _cacheValid[(int)CacheBits.AnimateFromRight] = value; }
        }

        private bool AnimateFromBottom
        {
            get { return _cacheValid[(int)CacheBits.AnimateFromBottom]; }
            set { _cacheValid[(int)CacheBits.AnimateFromBottom] = value; }
        }

        internal bool HitTestable
        {
            // Store complement of value so default is true
            get { return !_cacheValid[(int)CacheBits.HitTestable]; }
            set { _cacheValid[(int)CacheBits.HitTestable] = !value; }
        }

        private bool IsDragDropActive
        {
            get { return _cacheValid[(int)CacheBits.IsDragDropActive]; }
            set { _cacheValid[(int)CacheBits.IsDragDropActive] = value; }
        }

        #endregion

        #region Data

        internal const double Tolerance = 1.0e-2; // allow errors in double calculations

        private const int AnimationDelay = 150;
        internal static TimeSpan AnimationDelayTime = new TimeSpan(0, 0, 0, 0, AnimationDelay);
        internal static RoutedEventHandler CloseOnUnloadedHandler;
        private static readonly UncommonField<PopupRoot> ParentPopupRootField = new UncommonField<PopupRoot>();

        private PositionInfo _positionInfo;

        private SecurityCriticalDataForSet<PopupRoot> _popupRoot;
        private DispatcherOperation _asyncCreate;
        private DispatcherTimer _asyncDestroy;

        // holder of the popup's security helper
        private PopupSecurityHelper _secHelper;

        private BitVector32 _cacheValid = new BitVector32(0);   // Condense boolean bits

        private enum CacheBits
        {
            CaptureEngaged          = 0x01,
            IsTransparent           = 0x02,
            OnClosedHandlerReopen   = 0x04,
            DropOppositeSet         = 0x08,
            DropOpposite            = 0x10,
            AnimateFromRight        = 0x20,
            AnimateFromBottom       = 0x40,
            HitTestable             = 0x80,  // False for tooltips
            IsDragDropActive        = 0x100,
            IsIgnoringMouseEvents   = 0x200,
        }

        #endregion

        #region Popup Security Helper

        private const double RestrictPercentage = 0.75; // This is how much the max dimensions will be reduced by

        //
        //  This property
        //  1. Finds the correct initial size for the _effectiveValues store on the current DependencyObject
        //  2. This is a performance optimization
        //
        internal override int EffectiveValuesInitialSize
        {
            get { return 19; }
        }

        /// <summary>
        /// Helper for popup's security data
        /// </summary>
        private class PopupSecurityHelper
        {
            /// <summary>
            /// Helper constructor
            /// </summary>
            internal PopupSecurityHelper()
            {
            }

            /////////////////////////////////////////////////////////////////////////////////////
            // Helper's Critical-TreatAsSafe Methods - Safe to expose
            /////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// returns whether this is a restricted popup window or not
            /// </summary>
            internal bool IsChildPopup
            {
                get
                {
                    if (!_isChildPopupInitialized)
                    {
                        _isChildPopup = false;
                        _isChildPopupInitialized = true;
                    }
                    return (_isChildPopup);
                }
            }

            internal bool IsWindowAlive()
            {
                if (_window != null)
                {
                    HwndSource hwnd = _window.Value;
                    return (hwnd != null) && !hwnd.IsDisposed;
                }

                return false;
            }

            internal Point ClientToScreen(Visual rootVisual, Point clientPoint)
            {
                // Get the HwndSource of the target element.
                HwndSource targetWindow = PopupSecurityHelper.GetPresentationSource(rootVisual) as HwndSource;

                if (targetWindow != null)
                {
                    return PointUtil.ToPoint(ClientToScreen(targetWindow, clientPoint));
                }

                return clientPoint;
            }

            private NativeMethods.POINT ClientToScreen(HwndSource hwnd, Point clientPt)
            {
                bool isChildPopup = IsChildPopup;
                HwndSource parent = null;
                if (isChildPopup)
                {
                     parent = HwndSource.CriticalFromHwnd(ParentHandle);
                }

                Point devicePoint = clientPt;
                if (!isChildPopup || (parent != hwnd))
                {
                    // Transform to screen coordinates.
                    devicePoint = PointUtil.ClientToScreen(clientPt, hwnd);
                }

                if (isChildPopup && (parent != hwnd))
                {
                    // A child window's "screen" is actually the parent window's client area.
                    // Transform the coordinates into that space.
                    devicePoint = PointUtil.ScreenToClient(devicePoint, parent);
                }

                return new NativeMethods.POINT((int)devicePoint.X, (int)devicePoint.Y);
            }

            internal NativeMethods.POINT GetMouseCursorPos(Visual targetVisual)
            {
                if (Mouse.DirectlyOver != null)
                {
                    // get target window info
                    HwndSource hwndSource = null;
                    if (targetVisual != null)
                    {
                        hwndSource = PopupSecurityHelper.GetPresentationSource(targetVisual) as HwndSource;
                    }

                    IInputElement relativeTarget = targetVisual as IInputElement;

                    if (relativeTarget != null)
                    {
                        Point pt = Mouse.GetPosition(relativeTarget);

                        if ((hwndSource != null) && !hwndSource.IsDisposed)
                        {
                            Visual rootVisual = hwndSource.RootVisual;
                            CompositionTarget ct = hwndSource.CompositionTarget;

                            if ((rootVisual != null) && (ct != null))
                            {
                                // Transform the point from the targetVisual to client device units
                                GeneralTransform transformTo = targetVisual.TransformToAncestor(rootVisual);
                                Matrix transform = PointUtil.GetVisualTransform(rootVisual) * ct.TransformToDevice;
                                transformTo.TryTransform(pt, out pt);
                                pt = transform.Transform(pt);

                                // Convert from device client units to screen units
                                return ClientToScreen(hwndSource, pt);
                            }
                        }
                    }
                }

                // This is a fallback if we couldn't convert Mouse.GetPosition
                NativeMethods.POINT mousePoint = new NativeMethods.POINT(0, 0);

                UnsafeNativeMethods.TryGetCursorPos(mousePoint);

                return mousePoint;
            }

            internal void SetPopupPos(bool position, int x, int y, bool size, int width, int height)
            {
                int flags = NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE;
                if (!position)
                {
                    flags |= NativeMethods.SWP_NOMOVE;
                }
                if (!size)
                {
                    flags |= NativeMethods.SWP_NOSIZE;
                }

                UnsafeNativeMethods.SetWindowPos(new HandleRef(null, Handle), new HandleRef(null, IntPtr.Zero),
                    x, y, width, height, flags);
            }

            internal Rect GetParentWindowRect()
            {
                NativeMethods.RECT rect = new NativeMethods.RECT(0, 0, 0, 0);

                IntPtr parent = ParentHandle;
                if (parent != IntPtr.Zero)
                {
                    SafeNativeMethods.GetClientRect(new HandleRef(null, parent), ref rect);
                }

                return PointUtil.ToRect(rect);
            }

            internal Rect GetWindowRect()
            {
                NativeMethods.RECT rect = new NativeMethods.RECT(0, 0, 0, 0);

                IntPtr hwnd = Handle;
                if (hwnd != IntPtr.Zero)
                {
                    SafeNativeMethods.GetWindowRect(new HandleRef(null, hwnd), ref rect);
                }

                return PointUtil.ToRect(rect);
            }

            internal Matrix GetTransformToDevice()
            {
                CompositionTarget ct = _window.Value.CompositionTarget;
                if (ct != null && !ct.IsDisposed)
                {
                    return ct.TransformToDevice;
                }

                return Matrix.Identity;
            }

            internal static Matrix GetTransformToDevice(Visual targetVisual)
            {
                HwndSource hwndSource = null;
                if (targetVisual != null)
                {
                    hwndSource = PopupSecurityHelper.GetPresentationSource(targetVisual) as HwndSource;
                }

                if (hwndSource != null)
                {
                    CompositionTarget ct = hwndSource.CompositionTarget;
                    if (ct != null && !ct.IsDisposed)
                    {
                        return ct.TransformToDevice;
                    }
                }

                return Matrix.Identity;
            }

            internal Matrix GetTransformFromDevice()
            {
                CompositionTarget ct = _window.Value.CompositionTarget;
                if (ct != null && !ct.IsDisposed)
                {
                    return ct.TransformFromDevice;
                }

                return Matrix.Identity;
            }

            internal void SetWindowRootVisual(Visual v)
            {
                _window.Value.RootVisual = v;
            }

            internal static bool IsVisualPresentationSourceNull(Visual visual)
            {
                return (PopupSecurityHelper.GetPresentationSource(visual) == null);
            }

            internal void ShowWindow()
            {
                if (IsChildPopup)
                {
                    IntPtr lastWebOCHwnd = GetLastWebOCHwnd();

                    // If there is WebOC present, put the Popup behind the last WebOC in the z-order. Otherwise, bring it to front.
                    UnsafeNativeMethods.SetWindowPos(new HandleRef(null, Handle),
                        lastWebOCHwnd == IntPtr.Zero ? NativeMethods.HWND_TOP : new HandleRef(null, lastWebOCHwnd),
                        0, 0, 0, 0,
                        NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_SHOWWINDOW);
                }
                else
                {
                    if (!FrameworkCompatibilityPreferences.GetUseSetWindowPosForTopmostWindows())
                    {
                        UnsafeNativeMethods.ShowWindow(new HandleRef(null, Handle), NativeMethods.SW_SHOWNA);
                    }
                    else
                    {
                        UnsafeNativeMethods.SetWindowPos(new HandleRef(null, Handle),
                            NativeMethods.HWND_TOPMOST,
                            0, 0, 0, 0,
                            NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOOWNERZORDER | NativeMethods.SWP_SHOWWINDOW);
                    }
                }
            }


            internal void HideWindow()
            {
                UnsafeNativeMethods.ShowWindow(new HandleRef(null, Handle), NativeMethods.SW_HIDE);
            }

            /// <remarks>
            ///     We are searching among Popup's sibling child windows. Since WebBrowsers and Popup can only be siblings child windows right now,
            ///     e.g., we don't allow WebOC inside Popup window, this is a better performed way. If we change that, we should make sure we are comparing
            ///     to all weboc on the page. We can get a list of weboc by walking the navigationservice tree.
            /// </remarks>
            private IntPtr GetLastWebOCHwnd()
            {
                // Get the bottom hwnd in z-order.
                IntPtr lastHwnd = UnsafeNativeMethods.GetWindow(new HandleRef(null, Handle), NativeMethods.GW_HWNDLAST);

                StringBuilder sb = new StringBuilder(NativeMethods.MAX_PATH);
                // Search up from the last one until we find the first weboc hwnd.
                while (lastHwnd != IntPtr.Zero)
                {
                    if (UnsafeNativeMethods.GetClassName(new HandleRef(null, lastHwnd), sb, NativeMethods.MAX_PATH) != 0)
                    {
                        if (String.Compare(sb.ToString(), WebOCWindowClassName, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            break;
                        }
                        else
                        {
                            lastHwnd = UnsafeNativeMethods.GetWindow(new HandleRef(null, lastHwnd), NativeMethods.GW_HWNDPREV);
                        }
                    }
                    else
                    {
                        throw new Win32Exception();
                    }
                }

                return lastHwnd;
            }

            internal void SetHitTestable(bool hitTestable)
            {

                // get the window handle
                IntPtr handle = Handle;

                Int32 styles = UnsafeNativeMethods.GetWindowLong(new HandleRef(this, handle), NativeMethods.GWL_EXSTYLE);

                int flags = styles;

                if (((flags & NativeMethods.WS_EX_TRANSPARENT) == 0) != hitTestable)
                {
                    if (hitTestable)
                    {
                        styles = (Int32)(flags & ~NativeMethods.WS_EX_TRANSPARENT);
                    }
                    else
                    {
                        styles = (Int32)(flags | NativeMethods.WS_EX_TRANSPARENT);
                    }

                    UnsafeNativeMethods.CriticalSetWindowLong(new HandleRef(null, handle), NativeMethods.GWL_EXSTYLE, (IntPtr)styles);
                }
            }

            private static Visual FindMainTreeVisual(Visual v)
            {
                DependencyObject root = null;
                DependencyObject dependencyObject = v;

                while (dependencyObject != null)
                {
                    root = dependencyObject;

                    PopupRoot popupRoot = dependencyObject as PopupRoot;
                    if (popupRoot != null)
                    {
                        dependencyObject= popupRoot.Parent;

                        // Look for the placement target of the popup
                        Popup popup = dependencyObject as Popup;

                        if (popup != null)
                        {
                            UIElement target = popup.PlacementTarget;
                            if (target != null)
                            {
                                dependencyObject = target;
                            }
                        }
                    }
                    else
                    {
                        dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
                    }
                }

                return root as Visual;
            }

// newWindow is referenced by the _window static field, but
// PreSharp will think that newWindow is local and should be disposed.
#pragma warning disable 6518

            internal void BuildWindow(int x, int y, Visual placementTarget,
                bool transparent, HwndSourceHook hook, AutoResizedEventHandler handler, HwndDpiChangedEventHandler dpiChangedHandler)
            {
                Debug.Assert(!IsChildPopup || (IsChildPopup && !transparent), "Child popups cannot be transparent");
                transparent = transparent && !IsChildPopup;

                Visual mainTreeVisual = placementTarget;
                if (IsChildPopup)
                {
                    // If the popup is nested inside other popups, get out into the main tree
                    // before querying for the presentation source.
                    mainTreeVisual = FindMainTreeVisual(placementTarget);
                }

                // get visual's PresentationSource
                HwndSource hwndSource = PopupSecurityHelper.GetPresentationSource(mainTreeVisual) as HwndSource;

                // get parent handle
                IntPtr parent = IntPtr.Zero;
                if (hwndSource != null)
                {
                    parent = PopupSecurityHelper.GetHandle(hwndSource);
                }

                int classStyle = 0;
                int style = NativeMethods.WS_CLIPSIBLINGS;
                int styleEx = NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE;

                if (IsChildPopup)
                {
                    // The popup was created in an environment where it should be a child window, not a popup window.
                    style |= NativeMethods.WS_CHILD;
                }
                else
                {
                    style |= NativeMethods.WS_POPUP;
                    styleEx |= NativeMethods.WS_EX_TOPMOST;
                }

                // set window parameters
                HwndSourceParameters param = new HwndSourceParameters(String.Empty);
                param.WindowClassStyle = classStyle;
                param.WindowStyle = style;
                param.ExtendedWindowStyle = styleEx;
                param.SetPosition(x, y);

                if (IsChildPopup)
                {
                    if ( parent != IntPtr.Zero )
                    {
                        param.ParentWindow = parent;
                    }
                }
                else
                {
                    param.UsesPerPixelOpacity = transparent;
                    if ((parent != IntPtr.Zero) && ConnectedToForegroundWindow(parent))
                    {
                        param.ParentWindow = parent;
                    }
                }

                // create popup's window object
                HwndSource newWindow = new HwndSource(param);

                // add hook to the popup's window
                newWindow.AddHook(hook);

                // initialize the private critical window object
                _window = new SecurityCriticalDataClass<HwndSource>(newWindow);

                // Set background color
                HwndTarget hwndTarget = (HwndTarget)newWindow.CompositionTarget;
                hwndTarget.BackgroundColor = transparent ? Colors.Transparent : Colors.Black;

                // add AddAutoResizedEventHandler event handler
                newWindow.AutoResized += handler;

                // add the DpiChagnedEventHandler 
                newWindow.DpiChanged += dpiChangedHandler;
            }

#pragma warning restore 6518

            private static bool ConnectedToForegroundWindow(IntPtr window)
            {
                IntPtr foregroundWindow = UnsafeNativeMethods.GetForegroundWindow();

                while (window != IntPtr.Zero)
                {
                    if (window == foregroundWindow)
                    {
                        return true;
                    }
                    window = UnsafeNativeMethods.GetParent(new HandleRef(null, window));
                }

                return false;
            }

            /////////////////////////////////////////////////////////////////////////////////////
            // Helper's Critical Methods - NOT safe to expose
            /////////////////////////////////////////////////////////////////////////////////////

            private static IntPtr GetHandle(HwndSource hwnd)
            {
                // add hook to the popup's window
                return (hwnd!=null ? hwnd.CriticalHandle : IntPtr.Zero);
            }

            private static IntPtr GetParentHandle(HwndSource hwnd)
            {
                if (hwnd != null)
                {
                    IntPtr child = GetHandle(hwnd);
                    if (child != IntPtr.Zero)
                    {
                        return UnsafeNativeMethods.GetParent(new HandleRef(null, child));
                    }
                }

                return IntPtr.Zero;
            }

            private IntPtr Handle
            {
                get
                {
                    return (GetHandle(_window.Value));
                }
            }

            private IntPtr ParentHandle
            {
                get
                {
                    return (GetParentHandle(_window.Value));
                }
            }

            private static PresentationSource GetPresentationSource(Visual visual)
            {
                return (visual != null ? PresentationSource.CriticalFromVisual(visual) : null);
            }

            /// <summary>
            /// This function is required to force the MSAAtoUIA bridge for popups which is broken due the deficiencey in UIAutomationCore
            /// for not able to determine the connection between the PopupRoot Window and the main Window due to different Hwnds of both.
            ///
            /// </summary>
            internal void ForceMsaaToUiaBridge(PopupRoot popupRoot)
            {
                if (Handle != IntPtr.Zero && (UnsafeNativeMethods.IsWinEventHookInstalled(NativeMethods.EVENT_OBJECT_FOCUS) || UnsafeNativeMethods.IsWinEventHookInstalled(NativeMethods.EVENT_OBJECT_STATECHANGE)))
                {
                    PopupRootAutomationPeer popupRootAutomationPeer = UIElementAutomationPeer.CreatePeerForElement(popupRoot) as PopupRootAutomationPeer;
                    if (popupRootAutomationPeer != null)
                    {
                        if (popupRootAutomationPeer.Hwnd == IntPtr.Zero)
                            popupRootAutomationPeer.Hwnd = Handle;
                        IRawElementProviderSimple RootProviderForHwnd = popupRootAutomationPeer.ProviderFromPeer(popupRootAutomationPeer);
                        IntPtr lResult = AutomationInteropProvider.ReturnRawElementProvider(Handle, IntPtr.Zero, new IntPtr(NativeMethods.OBJID_CLIENT), RootProviderForHwnd);
                        if (lResult != IntPtr.Zero)
                        {
                            IAccessible acc = null;
                            int hr = NativeMethods.S_FALSE;
                            Guid iid = new Guid(MS.Internal.AppModel.IID.Accessible);
                            hr = UnsafeNativeMethods.ObjectFromLresult(lResult, ref iid, IntPtr.Zero, ref acc);
                            if (hr == NativeMethods.S_OK && acc != null)
                            {
                                // Release IAccessible(acc) object, just trusting the GC
                                ;
                            }
                        }
                    }
                }
            }

            internal void DestroyWindow(HwndSourceHook hook, AutoResizedEventHandler onAutoResizedEventHandler, HwndDpiChangedEventHandler onDpiChagnedEventHandler)
            {
                // Do this first to prevent infinite loops in dispose
                HwndSource hwnd = _window.Value;

                _window = null;

                if (!hwnd.IsDisposed)
                {
                    hwnd.AutoResized -=  onAutoResizedEventHandler ;
                    hwnd.DpiChanged -= onDpiChagnedEventHandler;
                    hwnd.RemoveHook(hook);
                    hwnd.RootVisual = null;
                    hwnd.Dispose();
                }
            }

            private bool _isChildPopup;

            /// <summary>
            /// determines whether _isChildPopup was initialized or not yet.
            /// </summary>
            private bool _isChildPopupInitialized;

            private SecurityCriticalDataClass<HwndSource> _window;

            private const string WebOCWindowClassName = "Shell Embedding";
        }

        #endregion

        /// <summary>
        /// Helper to find the (left, top) of the monitor that contains the placement target, in screen coordinates. 
        /// </summary>
        /// <remarks>
        /// Normally, the HWND associated with a Popup is created at (0,0), and then 'moved' to the appropriate location.This can
        /// lead to a DPI change when (0,0) lies on another monitor with a different DPI. DPI changes typically lead to size changes
        /// as well, which can lead to dismissals of Popups. To prevent this, we should create the HWND associated with a Popup
        /// on the correct monitor. This helper will identify the origin of the monitor associated with the placement target to help
        /// with this. 
        /// </remarks>
        private static class PopupInitialPlacementHelper
        {
            /// <summary>
            /// Decides whether this helper should be used. 
            /// This helper is used when - 
            ///     a. WPF supports DPI scaling (HwndTarget.IsPerMonitorDpiScalingEnabled), and 
            ///     b. The process is PMA (HwndTarget.IsProcessPerMonitorDpiAware)
            /// </summary>
            /// <remarks>
            internal static bool IsPerMonitorDpiScalingActive
            {
                get
                {
                    if (!HwndTarget.IsPerMonitorDpiScalingEnabled)
                    {
                        return false;
                    }

                    if (HwndTarget.IsProcessPerMonitorDpiAware.HasValue)
                    {
                        return HwndTarget.IsProcessPerMonitorDpiAware.Value;
                    }

                    // WPF supports Per-Monitor scaling, but HwndTarget has not 
                    // yet been initialized with the first HWND, and therefore 
                    // HwndTarget.IsProcessPerMonitorDpiAware is not queryable. 
                    // Let's use the current process' DPI awareness as a proxy. 
                    return DpiUtil.GetProcessDpiAwareness(IntPtr.Zero) == NativeMethods.PROCESS_DPI_AWARENESS.PROCESS_PER_MONITOR_DPI_AWARE;
                }
            }

            /// <summary>
            /// Finds the screen coordinates of the PlacementTarget's (left, top)
            /// </summary>
            private static NativeMethods.POINTSTRUCT? GetPlacementTargetOriginInScreenCoordinates(Popup popup)
            {
                var target = popup?.GetTarget() as UIElement;
                if (target != null)
                {
                    var rootVisual = Popup.GetRootVisual(target);
                    var targetToClientTransform = Popup.TransformToClient(target, rootVisual);

                    Point ptPlacementTargetOrigin;

                    // Transform (0,0) of the placement target to screen-coordinate
                    if (targetToClientTransform.TryTransform(new Point(0, 0), out ptPlacementTargetOrigin))
                    {
                        var screenOrigin = popup._secHelper.ClientToScreen(rootVisual, ptPlacementTargetOrigin);
                        return new NativeMethods.POINTSTRUCT((int)screenOrigin.X, (int)screenOrigin.Y);
                    }
                }

                return null;
            }

            /// <summary>
            /// Finds the (top,left) screen coordinates of the monitor that contains the placement target
            /// </summary>
            internal static NativeMethods.POINTSTRUCT GetPlacementOrigin(Popup popup)
            {
                var placementOrigin = new NativeMethods.POINTSTRUCT(0, 0);

                if (IsPerMonitorDpiScalingActive)
                {
                    var screenOrigin = GetPlacementTargetOriginInScreenCoordinates(popup);
                    if (screenOrigin.HasValue)
                    {
                        try
                        {
                            IntPtr hMonitor = SafeNativeMethods.MonitorFromPoint(screenOrigin.Value, NativeMethods.MONITOR_DEFAULTTONEAREST);

                            var info = new NativeMethods.MONITORINFOEX();
                            SafeNativeMethods.GetMonitorInfo(new HandleRef(null, hMonitor), info);

                            placementOrigin.x = info.rcMonitor.left;
                            placementOrigin.y = info.rcMonitor.top;
                        }
                        catch(Win32Exception)
                        {
                            // Do not let a failure here cause a crash
                        }
                    }
                }

                return placementOrigin;
            }
        }
    }
}

