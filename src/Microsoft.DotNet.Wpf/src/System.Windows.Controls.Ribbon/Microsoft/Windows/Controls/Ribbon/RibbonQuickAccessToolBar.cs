// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon
#else
namespace Microsoft.Windows.Controls.Ribbon
#endif
{
    #region Using declarations

    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Automation.Peers;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Markup;
    using System.Windows.Media;
#if RIBBON_IN_FRAMEWORK
    using System.Windows.Controls.Ribbon.Primitives;
    using Microsoft.Windows.Controls;
#else
    using Microsoft.Windows.Automation.Peers;
    using Microsoft.Windows.Controls.Ribbon.Primitives;
#endif

    #endregion

    /// <summary>
    ///   Implements the Ribbon's QuickAccessToolbar.
    /// </summary>
    [TemplatePart(Name = MainPanelTemplatePartName, Type = typeof(RibbonQuickAccessToolBarPanel))]
    [TemplatePart(Name = OverflowPanelTemplatePartName, Type = typeof(RibbonQuickAccessToolBarOverflowPanel))]
    [TemplatePart(Name = OverflowPopupTemplatePartName, Type = typeof(Popup))]
    [TemplatePart(Name = OverflowButtonTemplatePartName, Type = typeof(RibbonToggleButton))]
    public class RibbonQuickAccessToolBar : ItemsControl
    {
        #region Fields

        private const string MainPanelTemplatePartName = "PART_MainPanel";
        private const string OverflowPanelTemplatePartName = "PART_OverflowPanel";
        private const string OverflowPopupTemplatePartName = "PART_OverflowPopup";
        private const string OverflowButtonTemplatePartName = "PART_OverflowButton";

        private RibbonQuickAccessToolBarPanel _mainPanel;
        private RibbonQuickAccessToolBarOverflowPanel _overflowPanel;
        private Popup _overflowPopup;                                   // The Popup that hosts the overflow panel.
        private RibbonToggleButton _overflowButton;                     // The ToggleButton that hosts the overflow panel popup.
        private BitVector32 _bits = new BitVector32(0);
        private static string _overflowButtonToolTipText = Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.RibbonQuickAccessToolBar_OverflowButtonToolTip);

        private enum Bits
        {
            InContextMenu = 0x01,
            RetainFocusOnEscape = 0x02,
        }

        private bool InContextMenu
        {
            get { return _bits[(int)Bits.InContextMenu]; }
            set { _bits[(int)Bits.InContextMenu] = value; }
        }

        private bool RetainFocusOnEscape
        {
            get { return _bits[(int)Bits.RetainFocusOnEscape]; }
            set { _bits[(int)Bits.RetainFocusOnEscape] = value; }
        }

        #endregion

        #region Panels

        internal RibbonQuickAccessToolBarPanel MainPanel
        {
            get
            {
                return _mainPanel;
            }
        }

        internal RibbonQuickAccessToolBarOverflowPanel OverflowPanel
        {
            get
            {
                return _overflowPanel;
            }
        }

        #endregion Panels

        #region Constructors

        /// <summary>
        ///   Initializes static members of the RibbonQuickAccessToolBar class.
        /// </summary>
        static RibbonQuickAccessToolBar()
        {
            Type ownerType = typeof(RibbonQuickAccessToolBar);
            DefaultStyleKeyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(ownerType));
            FocusableProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(false));

            // If the DirectionalNaviation is default (Continue) then
            // in classic theme the first tab header is closer to first qat control than the
            // second qat control. Meaning a right arrow key from first qat control takes focus
            // to first tab header which is not expected. Hence setting the direction navigation
            // to Local so that a local search in qat is made before making a ribbon wide search.
            KeyboardNavigation.DirectionalNavigationProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(KeyboardNavigationMode.Local));

            EventManager.RegisterClassHandler(ownerType, Mouse.PreviewMouseDownOutsideCapturedElementEvent, new MouseButtonEventHandler(OnClickThroughThunk));
            EventManager.RegisterClassHandler(ownerType, Mouse.LostMouseCaptureEvent, new MouseEventHandler(OnLostMouseCaptureThunk), true /* handledEventsToo */);
            EventManager.RegisterClassHandler(ownerType, RibbonControlService.DismissPopupEvent, new RibbonDismissPopupEventHandler(OnDismissPopupThunk));
            EventManager.RegisterClassHandler(ownerType, Mouse.MouseDownEvent, new MouseButtonEventHandler(OnMouseDownThunk), true);
            EventManager.RegisterClassHandler(ownerType, FrameworkElement.ContextMenuOpeningEvent, new ContextMenuEventHandler(OnContextMenuOpeningThunk), true);
            EventManager.RegisterClassHandler(ownerType, FrameworkElement.ContextMenuClosingEvent, new ContextMenuEventHandler(OnContextMenuClosingThunk), true);
            EventManager.RegisterClassHandler(ownerType, KeyTipService.ActivatingKeyTipEvent, new ActivatingKeyTipEventHandler(OnActivatingKeyTipThunk));
            EventManager.RegisterClassHandler(ownerType, KeyTipService.PreviewKeyTipAccessedEvent, new KeyTipAccessedEventHandler(OnPreviewKeyTipAccessedThunk));
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     DependencyProperty for Ribbon property.
        /// </summary>
        public static readonly DependencyProperty RibbonProperty =
            RibbonControlService.RibbonProperty.AddOwner(typeof(RibbonQuickAccessToolBar));

        /// <summary>
        ///     This property is used to access visual style brushes defined on the Ribbon class.
        /// </summary>
        public Ribbon Ribbon
        {
            get { return RibbonControlService.GetRibbon(this); }
        }

        /// <summary>
        ///   Gets or sets a value indicating whether or not the overflow popup is currently open.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsOverflowOpen
        {
            get { return (bool)GetValue(IsOverflowOpenProperty); }
            set { SetValue(IsOverflowOpenProperty, value); }
        }

        /// <summary>
        ///   Using a DependencyProperty as the backing store for IsOverflowOpen.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty IsOverflowOpenProperty = DependencyProperty.Register(
            "IsOverflowOpen",
            typeof(bool),
            typeof(RibbonQuickAccessToolBar),
            new FrameworkPropertyMetadata(
                false,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                new PropertyChangedCallback(OnIsOverflowOpenChanged),
                new CoerceValueCallback(OnCoerceIsOverflowOpen)));

        private static void OnIsOverflowOpenChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            RibbonQuickAccessToolBar qat = (RibbonQuickAccessToolBar)sender;

            // If the drop down is closed due to
            // an action of context menu or if the 
            // ContextMenu for a parent  
            // was opened by right clicking this 
            // instance then ContextMenuClosed 
            // event is never raised. 
            // Hence reset the flag.
            qat.InContextMenu = false;

            UIElement popupChild = qat._overflowPopup.TryGetChild();
            RibbonHelper.HandleIsDropDownChanged(
                qat,
                delegate() { return qat.IsOverflowOpen; },
                popupChild,
                popupChild);

            if ((bool)(e.NewValue))
            {
                qat.RetainFocusOnEscape = RibbonHelper.IsKeyboardMostRecentInputDevice();
            }

            // Raise UI Automation Events
            RibbonQuickAccessToolBarAutomationPeer peer = UIElementAutomationPeer.FromElement(qat) as RibbonQuickAccessToolBarAutomationPeer;
            if (peer != null)
            {
                peer.RaiseExpandCollapseAutomationEvent(!(bool)e.OldValue, !(bool)e.NewValue);
            }
        }

        private static object OnCoerceIsOverflowOpen(DependencyObject d, object baseValue)
        {
            RibbonQuickAccessToolBar qat = (RibbonQuickAccessToolBar)d;
            if (!qat.HasOverflowItems)
            {
                return false;
            }
            return baseValue;
        }

        /// <summary>
        ///   Gets a value indicating whether we have overflow items.
        /// </summary>
        public bool HasOverflowItems
        {
            get { return (bool)GetValue(HasOverflowItemsProperty); }
            internal set { SetValue(HasOverflowItemsPropertyKey, value); }
        }
        
        /// <summary>
        ///   The DependencyPropertyKey needed to set read-only HasOverflowItems property.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        private static readonly DependencyPropertyKey HasOverflowItemsPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "HasOverflowItems",
                        typeof(bool),
                        typeof(RibbonQuickAccessToolBar),
                        new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnHasOverflowItemsChanged)));

        /// <summary>
        ///   Using a DependencyProperty as the backing store for HasOverflowItems.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty HasOverflowItemsProperty =
                HasOverflowItemsPropertyKey.DependencyProperty;


        private static void OnHasOverflowItemsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            RibbonQuickAccessToolBar qat = (RibbonQuickAccessToolBar)sender;
            qat.CoerceValue(RibbonQuickAccessToolBar.IsOverflowOpenProperty);
        }

        #endregion

        #region IsOverflowItem

        /// <summary>
        ///     The key needed set a read-only property.
        /// Attached property to indicate if the item is placed in the overflow panel
        /// </summary>
        internal static readonly DependencyPropertyKey IsOverflowItemPropertyKey =
                DependencyProperty.RegisterAttachedReadOnly(
                        "IsOverflowItem",
                        typeof(bool),
                        typeof(RibbonQuickAccessToolBar),
                        new FrameworkPropertyMetadata(false));

        /// <summary>
        ///     The DependencyProperty for the IsOverflowItem property.
        ///     Flags:              None
        ///     Default Value:      false
        /// </summary>
        public static readonly DependencyProperty IsOverflowItemProperty =
                IsOverflowItemPropertyKey.DependencyProperty;

        /// <summary>
        /// Writes the attached property IsOverflowItem to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        internal static void SetIsOverflowItem(DependencyObject element, object value)
        {
            element.SetValue(IsOverflowItemPropertyKey, value);
        }

        /// <summary>
        /// Reads the attached property IsOverflowItem from the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <returns>The property's value.</returns>
        public static bool GetIsOverflowItem(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (bool)element.GetValue(IsOverflowItemProperty);
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///   Invoked when the QuickAccessToolbar's template is applied.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_mainPanel != null)
            {
                _mainPanel.Children.Clear();
            }

            if (_overflowPanel != null)
            {
                _overflowPanel.Children.Clear();
            }

            _mainPanel = GetTemplateChild(MainPanelTemplatePartName) as RibbonQuickAccessToolBarPanel;
            _overflowPanel = GetTemplateChild(OverflowPanelTemplatePartName) as RibbonQuickAccessToolBarOverflowPanel;
            _overflowPopup = GetTemplateChild(OverflowPopupTemplatePartName) as Popup;
            _overflowButton = GetTemplateChild(OverflowButtonTemplatePartName) as RibbonToggleButton;
            if (_overflowButton != null)
            {
                _overflowButton.ToolTipTitle = _overflowButtonToolTipText;
            }

            // Set KeyTipAutoGenerationElements property on self.
            IEnumerable<DependencyObject> keyTipAutoGenerationElements = new KeyTipAutoGenerationElements(this);
            KeyTipService.SetKeyTipAutoGenerationElements(this, keyTipAutoGenerationElements);
            if (_overflowPanel != null)
            {
                // Set KeyTipAutoGenerationElements property on overflow panel which helps
                // auto generation of keytips on elements of overflow.
                KeyTipService.SetKeyTipAutoGenerationElements(_overflowPanel, keyTipAutoGenerationElements);
            }
        }

        #endregion

        #region Protected Methods

        private void InvalidateLayout()
        {
            InvalidateMeasure();

            RibbonQuickAccessToolBarPanel toolBarPanel = this.MainPanel;
            if (toolBarPanel != null)
            {
                toolBarPanel.InvalidateMeasure();
            }
        }

        /// <summary>
        ///   Invoked when the QuickAccessToolbar's items collection changes.
        /// </summary>
        /// <param name="e">The event data.</param>
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            InvalidateLayout();
            base.OnItemsChanged(e);
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new RibbonQuickAccessToolBarAutomationPeer(this);
        }

        #endregion

        #region DismissPopup

        private static void OnClickThroughThunk(object sender, MouseButtonEventArgs e)
        {
            RibbonQuickAccessToolBar qat = (RibbonQuickAccessToolBar)sender;
            qat.OnClickThrough(e);
        }

        private void OnClickThrough(MouseButtonEventArgs e)
        {
            UIElement popupChild = _overflowPopup.TryGetChild();
            RibbonHelper.HandleClickThrough(this, e, popupChild);
        }

        private static void OnLostMouseCaptureThunk(object sender, MouseEventArgs e)
        {
            RibbonQuickAccessToolBar qat = (RibbonQuickAccessToolBar)sender;
            qat.OnLostMouseCaptureThunk(e);
        }

        private void OnLostMouseCaptureThunk(MouseEventArgs e)
        {
            UIElement popupChild = _overflowPopup.TryGetChild();
            RibbonHelper.HandleLostMouseCapture(
                this,
                e,
                delegate() { return (IsOverflowOpen && !InContextMenu); },
                delegate(bool value) { IsOverflowOpen = value; },
                popupChild,
                popupChild);
        }

        private static void OnDismissPopupThunk(object sender, RibbonDismissPopupEventArgs e)
        {
            RibbonQuickAccessToolBar qat = (RibbonQuickAccessToolBar)sender;
            qat.OnDismissPopup(e);
        }

        private void OnDismissPopup(RibbonDismissPopupEventArgs e)
        {
            UIElement popupChild = _overflowPopup.TryGetChild();
            RibbonHelper.HandleDismissPopup(
                e,
                delegate(bool value) { IsOverflowOpen = value; },
                delegate(DependencyObject d) { return d == _overflowButton; },
                popupChild,
                this);
        }

        private static void OnMouseDownThunk(object sender, MouseButtonEventArgs e)
        {
            RibbonQuickAccessToolBar qat = (RibbonQuickAccessToolBar)sender;
            qat.OnAnyMouseDown();
        }

        private void OnAnyMouseDown()
        {
            RetainFocusOnEscape = false;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!e.Handled)
            {
                RibbonHelper.HandleDropDownKeyDown(
                    this,
                    e,
                    delegate { return IsOverflowOpen; },
                    delegate(bool value) { IsOverflowOpen = value; },
                    RetainFocusOnEscape ? _overflowButton : null,
                    _overflowPopup.TryGetChild());
            }
        }

        #endregion DismissPopup

        #region ContextMenu

        private static void OnContextMenuOpeningThunk(object sender, ContextMenuEventArgs e)
        {
            RibbonQuickAccessToolBar qat = (RibbonQuickAccessToolBar)sender;
            qat.OnContextMenuOpeningInternal();
        }

        private void OnContextMenuOpeningInternal()
        {
            InContextMenu = true;
        }

        private static void OnContextMenuClosingThunk(object sender, ContextMenuEventArgs e)
        {
            RibbonQuickAccessToolBar qat = (RibbonQuickAccessToolBar)sender;
            qat.OnContextMenuClosingInternal();
        }

        private void OnContextMenuClosingInternal()
        {
            InContextMenu = false;
            if (IsOverflowOpen)
            {
                UIElement popupChild = _overflowPopup.TryGetChild();
                RibbonHelper.AsyncSetFocusAndCapture(
                    this,
                    delegate() { return IsOverflowOpen; },
                    popupChild,
                    popupChild);
            }
        }

        #endregion ContextMenu

        #region ContanerGeneration

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new RibbonControl();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is RibbonControl;
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            
            RibbonControl ribbonControl = (RibbonControl)element;
            ribbonControl.IsInQuickAccessToolBar = true;

            ribbonControl.Content = item;   //foo4
        }

        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            base.ClearContainerForItemOverride(element, item);

            RibbonHelper.ClearPseudoInheritedProperties(element);
        }

        #endregion ContainerGeneration

        #region CloneEvent

        public static readonly RoutedEvent CloneEvent = EventManager.RegisterRoutedEvent("Clone", RoutingStrategy.Bubble, typeof(RibbonQuickAccessToolBarCloneEventHandler), typeof(RibbonQuickAccessToolBar));

        public static void AddCloneHandler(DependencyObject element, RibbonQuickAccessToolBarCloneEventHandler handler)
        {
            RibbonHelper.AddHandler(element, CloneEvent, handler);
        }

        public static void RemoveCloneHandler(DependencyObject element, RibbonQuickAccessToolBarCloneEventHandler handler)
        {
            RibbonHelper.RemoveHandler(element, CloneEvent, handler);
        }

        #endregion

        #region ID

        // Determine whether the QAT contains an element with the given QAT ID.
        internal bool ContainsId(object targetID)
        {
            foreach (object o in this.Items)
            {
                DependencyObject dependencyObject = o as DependencyObject;
                if (dependencyObject != null)
                {
                    object currentID = RibbonControlService.GetQuickAccessToolBarId(dependencyObject);
                    if (object.Equals(currentID, targetID))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion ID

        #region ContentModel

        public static readonly DependencyProperty CustomizeMenuButtonProperty =
            DependencyProperty.Register(
                    "CustomizeMenuButton",
                    typeof(RibbonMenuButton),
                    typeof(RibbonQuickAccessToolBar));

        public RibbonMenuButton CustomizeMenuButton
        {
            get { return (RibbonMenuButton)GetValue(CustomizeMenuButtonProperty); }
            set { SetValue(CustomizeMenuButtonProperty, value); }
        }

        #endregion ContentModel

        #region KeyTips

        #region KeyTipAutoGenerationElements

        private class KeyTipAutoGenerationElements : IEnumerable<DependencyObject>
        {
            #region Constructor And Properties

            public KeyTipAutoGenerationElements(RibbonQuickAccessToolBar quickAccessToolBar)
            {
                QuickAccessToolBar = quickAccessToolBar;
            }

            RibbonQuickAccessToolBar QuickAccessToolBar
            {
                get;
                set;
            }

            #endregion

            #region IEnumerable<DependencyObject> Members

            public IEnumerator<DependencyObject> GetEnumerator()
            {
                int itemCount = QuickAccessToolBar.Items.Count;
                int overflowStartIndex = -1;

                // Set KeyTip for all non-overflow items
                for (int i = 0; i < itemCount; i++)
                {
                    RibbonControl ribbonControl = QuickAccessToolBar.ItemContainerGenerator.ContainerFromIndex(i) as RibbonControl;
                    if (ribbonControl != null)
                    {
                        if (GetIsOverflowItem(ribbonControl))
                        {
                            overflowStartIndex = i;
                            break;
                        }
                        else if (ribbonControl.IsVisible)
                        {
                            UIElement contentChild = ribbonControl.ContentChild;
                            if (contentChild != null &&
                                contentChild.IsVisible &&
                                PropertyHelper.IsDefaultValue(contentChild, KeyTipService.KeyTipProperty))
                            {
                                yield return contentChild;
                            }
                        }
                    }
                }

                if (overflowStartIndex != -1)
                {
                    // Set KeyTip for overflow items.
                    for (int i = overflowStartIndex; i < itemCount; i++)
                    {
                        RibbonControl ribbonControl = QuickAccessToolBar.ItemContainerGenerator.ContainerFromIndex(i) as RibbonControl;
                        if (ribbonControl != null &&
                            ribbonControl.Visibility == Visibility.Visible &&
                            GetIsOverflowItem(ribbonControl))
                        {
                            UIElement contentChild = ribbonControl.ContentChild;
                            if (contentChild != null &&
                                contentChild.Visibility == Visibility.Visible &&
                                PropertyHelper.IsDefaultValue(contentChild, KeyTipService.KeyTipProperty))
                            {
                                yield return contentChild;
                            }
                        }
                    }
                }
            }

            #endregion

            #region IEnumerable Members

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            #endregion
        }

        #endregion

        private static void OnActivatingKeyTipThunk(object sender, ActivatingKeyTipEventArgs e)
        {
            ((RibbonQuickAccessToolBar)sender).OnActivatingKeyTip(e);
        }

        protected virtual void OnActivatingKeyTip(ActivatingKeyTipEventArgs e)
        {
            if (e.OriginalSource == _overflowButton)
            {
                if (_overflowButton.IsVisible)
                {
                    RibbonHelper.SetDefaultQatKeyTipPlacement(e);
                }
                else
                {
                    e.KeyTipVisibility = Visibility.Collapsed;
                }
            }
        }

        private static void OnPreviewKeyTipAccessedThunk(object sender, KeyTipAccessedEventArgs e)
        {
            ((RibbonQuickAccessToolBar)sender).OnPreviewKeyTipAccessed(e);
        }

        protected virtual void OnPreviewKeyTipAccessed(KeyTipAccessedEventArgs e)
        {
            if (e.OriginalSource == _overflowButton)
            {
                // Handle KeyTip accessed for overflow button.
                if (HasOverflowItems &&
                    !IsOverflowOpen)
                {
                    IsOverflowOpen = true;
                    UIElement popupChild = _overflowPopup.TryGetChild();
                    if (popupChild != null)
                    {
                        KeyTipService.SetIsKeyTipScope(popupChild, true);
                        e.TargetKeyTipScope = popupChild;
                    }
                }
                e.Handled = true;
            }
        }

        #endregion
    }

}
