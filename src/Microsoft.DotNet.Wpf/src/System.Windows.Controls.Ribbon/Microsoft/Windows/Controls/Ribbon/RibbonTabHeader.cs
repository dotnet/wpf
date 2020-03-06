// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon
#else
namespace Microsoft.Windows.Controls.Ribbon
#endif
{
    using System;
    using System.Windows;
    using System.Windows.Automation;
    using System.Windows.Automation.Peers;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Threading;
    using MS.Internal;
#if RIBBON_IN_FRAMEWORK
    using Microsoft.Windows.Controls;
#else
    using Microsoft.Windows.Automation.Peers;
#endif

    /// <summary>
    ///     Header control for RibbonTab
    /// </summary>
    [TemplatePart(Name = RibbonTabHeader.OuterBorderTemplatePartName, Type = typeof(Border))]
    public class RibbonTabHeader : ContentControl
    {
        #region Constructors

        static RibbonTabHeader()
        {
            Type ownerType = typeof(RibbonTabHeader);
            IsEnabledProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(null, new CoerceValueCallback(OnCoerceIsEnabled)));
            DefaultStyleKeyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(ownerType));
            VisibilityProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(null, new CoerceValueCallback(CoerceVisibility)));
            ToolTipService.ShowOnDisabledProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(true));
            StyleProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(OnNotifyPropertyChanged, CoerceStyle));
            ContentProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(OnNotifyPropertyChanged, CoerceContent));
            ContentTemplateProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(OnNotifyPropertyChanged, CoerceContentTemplate));
            ContentTemplateSelectorProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(OnNotifyPropertyChanged, CoerceContentTemplateSelector));
            ContentStringFormatProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(OnNotifyPropertyChanged, CoerceStringFormat));
            KeyTipService.KeyTipProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(null, new CoerceValueCallback(CoerceKeyTip)));
            EventManager.RegisterClassHandler(ownerType, KeyTipService.ActivatingKeyTipEvent, new ActivatingKeyTipEventHandler(OnActivatingKeyTipThunk));
            EventManager.RegisterClassHandler(ownerType, KeyTipService.KeyTipAccessedEvent, new KeyTipAccessedEventHandler(OnKeyTipAccessedThunk));
#if RIBBON_IN_FRAMEWORK
            AutomationProperties.IsOffscreenBehaviorProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(IsOffscreenBehavior.FromClip));
#endif
        }

        public RibbonTabHeader()
        {
            IsVisibleChanged += new DependencyPropertyChangedEventHandler(OnIsVisibleChanged);
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Dependency property backing IsRibbonTabSelected
        /// </summary>
        internal static readonly DependencyPropertyKey IsRibbonTabSelectedPropertyKey =
            DependencyProperty.RegisterReadOnly("IsRibbonTabSelected", typeof(bool), typeof(RibbonTabHeader), new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnIsRibbonTabSelectedChanged), new CoerceValueCallback(OnCoerceIsRibbonTabSelected)));

        public static readonly DependencyProperty IsRibbonTabSelectedProperty = IsRibbonTabSelectedPropertyKey.DependencyProperty;

        /// <summary>
        ///     Boolean property which indicates that the RibbonTab
        ///     corresponding to this RibbonTabHeader is selected
        /// </summary>
        public bool IsRibbonTabSelected
        {
            get { return (bool)GetValue(IsRibbonTabSelectedProperty); }
            internal set { SetValue(IsRibbonTabSelectedPropertyKey, value); }
        }

        private static readonly DependencyPropertyKey ContextualTabGroupPropertyKey =
            DependencyProperty.RegisterReadOnly("ContextualTabGroup", typeof(RibbonContextualTabGroup), typeof(RibbonTabHeader), new FrameworkPropertyMetadata(null, null, new CoerceValueCallback(CoerceContextualTabGroup)));

        public static readonly DependencyProperty ContextualTabGroupProperty = ContextualTabGroupPropertyKey.DependencyProperty;

        public RibbonContextualTabGroup ContextualTabGroup
        {
            get { return (RibbonContextualTabGroup)GetValue(ContextualTabGroupProperty); }
            private set { SetValue(ContextualTabGroupPropertyKey, value); }
        }

        private static readonly DependencyPropertyKey IsContextualTabPropertyKey =
            DependencyProperty.RegisterReadOnly("IsContextualTab", typeof(bool), typeof(RibbonTabHeader), new FrameworkPropertyMetadata(false, null, new CoerceValueCallback(CoerceIsContextualTab)));

        public static readonly DependencyProperty IsContextualTabProperty = IsContextualTabPropertyKey.DependencyProperty;

        public bool IsContextualTab
        {
            get { return (bool)GetValue(IsContextualTabProperty); }
            private set { SetValue(IsContextualTabPropertyKey, value); }
        }

        /// <summary>
        ///     DependencyProperty for Ribbon property.
        /// </summary>
        public static readonly DependencyProperty RibbonProperty =
            RibbonControlService.RibbonProperty.AddOwner(typeof(RibbonTabHeader));

        /// <summary>
        ///     This property is used to access Ribbon
        /// </summary>
        public Ribbon Ribbon
        {
            get { return RibbonControlService.GetRibbon(this); }
        }

        /// <summary>
        ///     The corresponding RibbonTab for this RibbonTabHeader
        /// </summary>
        internal RibbonTab RibbonTab
        {
            get
            {
                ItemsControl tabHeaderItemsControl = ItemsControl.ItemsControlFromItemContainer(this);
                Ribbon ribbon = Ribbon;
                if (tabHeaderItemsControl != null && ribbon != null)
                {
                    int index = tabHeaderItemsControl.ItemContainerGenerator.IndexFromContainer(this);
                    return ribbon.ItemContainerGenerator.ContainerFromIndex(index) as RibbonTab;
                }
                return null;
            }
        }

        /// <summary>
        ///     The padding which was set initially. It is used as max padding for a RibbonTabHeader
        /// </summary>
        internal Thickness DefaultPadding
        {
            get
            {
                if (double.IsNaN(_initialPadding.Left))
                {
                    _initialPadding = Padding;
                }

                return _initialPadding;
            }
        }

        /// <summary>
        ///     Indicates whether its default tooltip should be shown or not.
        /// </summary>
        internal bool ShowLabelToolTip
        {
            get { return RibbonHelper.GetIsContentTooltip(VisualChild, Content); }
            set { RibbonHelper.SetContentAsToolTip(this, VisualChild, Content, value); }
        }

        private FrameworkElement VisualChild
        {
            get
            {
                return VisualChildrenCount == 0 ? null : (GetVisualChild(0) as FrameworkElement);
            }
        }

        #endregion

        #region Visual States

        /// <summary>
        ///     DependencyProperty for MouseOverBorderBrush property.
        /// </summary>
        public static readonly DependencyProperty MouseOverBorderBrushProperty =
            RibbonControlService.MouseOverBorderBrushProperty.AddOwner(typeof(RibbonTabHeader));

        /// <summary>
        ///     Outer border brush used in a "hover" state of the RibbonButton.
        /// </summary>
        public Brush MouseOverBorderBrush
        {
            get { return RibbonControlService.GetMouseOverBorderBrush(this); }
            set { RibbonControlService.SetMouseOverBorderBrush(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for MouseOverBackground property.
        /// </summary>
        public static readonly DependencyProperty MouseOverBackgroundProperty =
            RibbonControlService.MouseOverBackgroundProperty.AddOwner(typeof(RibbonTabHeader));

        /// <summary>
        ///     Control background brush used in a "hover" state of the RibbonButton.
        /// </summary>
        public Brush MouseOverBackground
        {
            get { return RibbonControlService.GetMouseOverBackground(this); }
            set { RibbonControlService.SetMouseOverBackground(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for FocusedBackground property.
        /// </summary>
        public static readonly DependencyProperty FocusedBackgroundProperty =
            RibbonControlService.FocusedBackgroundProperty.AddOwner(typeof(RibbonTabHeader));

        /// <summary>
        ///     Control background brush used in a "Focused" state of the RibbonButton.
        /// </summary>
        public Brush FocusedBackground
        {
            get { return RibbonControlService.GetFocusedBackground(this); }
            set { RibbonControlService.SetFocusedBackground(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for FocusedBorderBrush property.
        /// </summary>
        public static readonly DependencyProperty FocusedBorderBrushProperty =
            RibbonControlService.FocusedBorderBrushProperty.AddOwner(typeof(RibbonTabHeader));

        /// <summary>
        ///     Control border brush used to paint a "Focused" state of the RibbonButton.
        /// </summary>
        public Brush FocusedBorderBrush
        {
            get { return RibbonControlService.GetFocusedBorderBrush(this); }
            set { RibbonControlService.SetFocusedBorderBrush(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for CheckedBackground property.
        /// </summary>
        public static readonly DependencyProperty CheckedBackgroundProperty =
            RibbonControlService.CheckedBackgroundProperty.AddOwner(typeof(RibbonTabHeader));

        /// <summary>
        ///     Control background brush used in a "Checked" state of the RibbonToggleButton.
        /// </summary>
        public Brush CheckedBackground
        {
            get { return RibbonControlService.GetCheckedBackground(this); }
            set { RibbonControlService.SetCheckedBackground(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for CheckedBorderBrush property.
        /// </summary>
        public static readonly DependencyProperty CheckedBorderBrushProperty =
            RibbonControlService.CheckedBorderBrushProperty.AddOwner(typeof(RibbonTabHeader));

        /// <summary>
        ///     Control border brush used to paint a "Checked" RibbonToggleButton.
        /// </summary>
        public Brush CheckedBorderBrush
        {
            get { return RibbonControlService.GetCheckedBorderBrush(this); }
            set { RibbonControlService.SetCheckedBorderBrush(this, value); }
        }

        #endregion

        #region Protected Methods

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _outerBorder = GetTemplateChild(OuterBorderTemplatePartName) as Border;
        }
        private Border _outerBorder;
        private const string OuterBorderTemplatePartName = "PART_OuterBorder";

        /// <summary>
        ///     Render callback
        /// </summary>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            // Ensure the Ribbon is fully rendered before calculating ribbonTab.TabHeaderLeft and ribbonTab.TabHeaderRight.
            Dispatcher.BeginInvoke(new Action(RecalculateTabHeaderLeftAndRightCallback), DispatcherPriority.Loaded);
        }

        // Calculates ribbonTab.TabHeaderLeft and ribbonTab.TabHeaderRight.
        private void RecalculateTabHeaderLeftAndRightCallback()
        {
            Ribbon ribbon = Ribbon;
            RibbonTab ribbonTab = RibbonTab;
            if (ribbon != null &&
                ribbonTab != null &&
                ribbon.IsAncestorOf(ribbonTab))
            {
                Thickness margin = _outerBorder != null ? _outerBorder.Margin : new Thickness();
                Thickness borderThickness = _outerBorder != null ? _outerBorder.BorderThickness : new Thickness();
                
                Point leftBottom = new Point(margin.Left + borderThickness.Left, ActualHeight);
                Point rightBottom = new Point(ActualWidth - margin.Right - borderThickness.Right, ActualHeight);

                GeneralTransform transformToRibbon = this.TransformToAncestor(ribbon);
                transformToRibbon.TryTransform(leftBottom, out leftBottom);
                transformToRibbon.TryTransform(rightBottom, out rightBottom);

                GeneralTransform transformToRibbonTab = ribbon.TransformToDescendant(ribbonTab);
                transformToRibbonTab.TryTransform(leftBottom, out leftBottom);
                transformToRibbonTab.TryTransform(rightBottom, out rightBottom);

                ribbonTab.TabHeaderLeft = leftBottom.X;
                ribbonTab.TabHeaderRight = rightBottom.X;
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new RibbonTabHeaderAutomationPeer(this);
        }

        /// <summary>
        ///     Notifies Ribbon when a mouse left button down happens on self.
        /// </summary>
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            Ribbon ribbon = Ribbon;
            if (ribbon != null)
            {
                ribbon.NotifyMouseClickedOnTabHeader(this, e);
                e.Handled = true;
            }

            base.OnMouseLeftButtonDown(e);
        }

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnGotKeyboardFocus(e);
            RibbonTab ribbonTab = RibbonTab;
            if (ribbonTab != null)
            {
                ribbonTab.IsSelected = true;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (!e.Handled)
            {
                if (e.Key == Key.Down)
                {
                    // On arrow down key press on TabHeader, try to move
                    // the focus to first element of the corresponding tab.
                    RibbonTab tab = RibbonTab;
                    if (tab != null)
                    {
                        if (tab.MoveFocus(new TraversalRequest(FocusNavigationDirection.First)))
                        {
                            e.Handled = true;
                        }
                    }
                }
                else if (e.Key == Key.Space ||
                    e.Key == Key.Enter)
                {
                    Ribbon ribbon = Ribbon;
                    if (ribbon != null)
                    {
                        if (ribbon.IsMinimized)
                        {
                            if (!ribbon.IsDropDownOpen)
                            {
                                // Open Ribbon dropdown when space/enter is
                                // pressed on a tab header.
                                ribbon.IsDropDownOpen = true;
                                e.Handled = true;
                            }
                        }
                        else
                        {
                            // Restore the focus if the ribbon is not minimized.
                            Ribbon.RestoreFocusAndCapture(false);
                            e.Handled = true;
                        }
                    }
                }
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        ///     Prepares self for its usage as itemcontainer of RibbonTabHeaderItemsControl.
        /// </summary>
        internal void PrepareRibbonTabHeader()
        {
            CoerceValue(IsContextualTabProperty);
            CoerceValue(ContextualTabGroupProperty);
            CoerceValue(IsRibbonTabSelectedProperty);
            CoerceValue(IsEnabledProperty);
            CoerceValue(VisibilityProperty);
            CoerceValue(KeyTipService.KeyTipProperty);

            InitializeTransferProperties();
        }

        internal void InitializeTransferProperties()
        {
            PropertyHelper.TransferProperty(this, StyleProperty);
            PropertyHelper.TransferProperty(this, ContentTemplateProperty);
            PropertyHelper.TransferProperty(this, ContentTemplateSelectorProperty);
            PropertyHelper.TransferProperty(this, ContentStringFormatProperty);
            PropertyHelper.TransferProperty(this, ContentProperty);
        }

        internal void NotifyPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == StyleProperty || e.Property == RibbonTab.HeaderStyleProperty || e.Property == Ribbon.TabHeaderStyleProperty)
            {
                PropertyHelper.TransferProperty(this, StyleProperty);
            }
            else if (e.Property == ContentProperty || e.Property == RibbonTab.HeaderProperty)
            {
                PropertyHelper.TransferProperty(this, ContentProperty);
            }
            else if (e.Property == ContentTemplateProperty || e.Property == RibbonTab.HeaderTemplateProperty || e.Property == Ribbon.TabHeaderTemplateProperty)
            {
                PropertyHelper.TransferProperty(this, ContentTemplateProperty);
            }
            else if (e.Property == ContentTemplateSelectorProperty || e.Property == RibbonTab.HeaderTemplateSelectorProperty)
            {
                PropertyHelper.TransferProperty(this, ContentTemplateSelectorProperty);
            }
            else if (e.Property == ContentStringFormatProperty || e.Property == RibbonTab.HeaderStringFormatProperty)
            {
                PropertyHelper.TransferProperty(this, ContentStringFormatProperty);
            }
            else if (e.Property == RibbonTab.ContextualTabGroupHeaderProperty)
            {
                CoerceValue(IsContextualTabProperty);
            }
            else if (e.Property == RibbonTab.ContextualTabGroupProperty)
            {
                CoerceValue(ContextualTabGroupProperty);
            }
        }

        #endregion

        #region Private Methods

        private static object CoerceContextualTabGroup(DependencyObject d, object baseValue)
        {
            RibbonTabHeader tabHeader = (RibbonTabHeader)d;
            RibbonTab tab = tabHeader.RibbonTab;
            if (tab != null)
            {
                return tab.ContextualTabGroup;
            }

            return baseValue;
        }

        private static object CoerceIsContextualTab(DependencyObject d, object baseValue)
        {
            RibbonTabHeader tabHeader = (RibbonTabHeader)d;
            RibbonTab tab = tabHeader.RibbonTab;
            if (tab != null)
            {
                return tab.ContextualTabGroupHeader != null;
            }

            return baseValue;
        }

        /// <summary>
        ///     Coercion callback for IsSelected property
        /// </summary>
        /// <param name="d"></param>
        /// <param name="baseValue"></param>
        /// <returns></returns>
        private static object OnCoerceIsRibbonTabSelected(DependencyObject d, object baseValue)
        {
            RibbonTabHeader header = (RibbonTabHeader)d;
            RibbonTab tab = header.RibbonTab;
            if (tab != null)
            {
                return tab.IsSelected;
            }
            return baseValue;
        }

        /// <summary>
        ///     Coercion callback for IsEnabled property
        /// </summary>
        /// <param name="d"></param>
        /// <param name="baseValue"></param>
        /// <returns></returns>
        private static object OnCoerceIsEnabled(DependencyObject d, object baseValue)
        {
            RibbonTabHeader header = (RibbonTabHeader)d;
            RibbonTab tab = header.RibbonTab;
            if (tab != null)
            {
                return tab.IsEnabled;
            }
            return baseValue;
        }

        private static object CoerceVisibility(DependencyObject d, object baseValue)
        {
            RibbonTabHeader header = (RibbonTabHeader)d;
            RibbonTab tab = header.RibbonTab;
            if (tab != null)
            {
                return tab.Visibility;
            }
            return baseValue;
        }

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Panel parentPanel = VisualTreeHelper.GetParent(this) as Panel;
            if (parentPanel != null)
            {
                parentPanel.InvalidateMeasure();
            }
        }

        private static void OnIsRibbonTabSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!((bool)e.NewValue) && Keyboard.FocusedElement == d)
            {
                Keyboard.Focus(null);
            }
        }

        private static void OnNotifyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((RibbonTabHeader)d).NotifyPropertyChanged(e);
        }

        private static object CoerceStyle(DependencyObject d, object baseValue)
        {
            RibbonTabHeader tabHeader = (RibbonTabHeader)d;

            return PropertyHelper.GetCoercedTransferPropertyValue(
                d,
                baseValue,
                StyleProperty,
                tabHeader.RibbonTab,
                RibbonTab.HeaderStyleProperty,
                tabHeader.Ribbon,
                Ribbon.TabHeaderStyleProperty);
        }

        private static object CoerceContent(DependencyObject d, object baseValue)
        {
            RibbonTabHeader tabHeader = (RibbonTabHeader)d;

            return PropertyHelper.GetCoercedTransferPropertyValue(
                d,
                baseValue,
                ContentProperty,
                tabHeader.RibbonTab,
                RibbonTab.HeaderProperty);
        }

        private static object CoerceContentTemplate(DependencyObject d, object baseValue)
        {
            RibbonTabHeader tabHeader = (RibbonTabHeader)d;

            return PropertyHelper.GetCoercedTransferPropertyValue(
                d,
                baseValue,
                ContentTemplateProperty,
                tabHeader.RibbonTab,
                RibbonTab.HeaderTemplateProperty,
                tabHeader.Ribbon,
                Ribbon.TabHeaderTemplateProperty);
        }

        private static object CoerceContentTemplateSelector(DependencyObject d, object baseValue)
        {
            RibbonTabHeader tabHeader = (RibbonTabHeader)d;

            return PropertyHelper.GetCoercedTransferPropertyValue(
                d,
                baseValue,
                ContentTemplateSelectorProperty,
                tabHeader.RibbonTab,
                RibbonTab.HeaderTemplateSelectorProperty);
        }

        private static object CoerceStringFormat(DependencyObject d, object baseValue)
        {
            RibbonTabHeader tabHeader = (RibbonTabHeader)d;

            return PropertyHelper.GetCoercedTransferPropertyValue(
                d,
                baseValue,
                ContentStringFormatProperty,
                tabHeader.RibbonTab,
                RibbonTab.HeaderStringFormatProperty);
        }

        #endregion

        #region Private Data

        private Thickness _initialPadding = new Thickness(double.NaN);
        private const double KeyTipVerticalOffset = 3;

        #endregion        

        #region KeyTips

        private static object CoerceKeyTip(DependencyObject d, object baseValue)
        {
            RibbonTabHeader tabHeader = (RibbonTabHeader)d;
            RibbonTab tab = tabHeader.RibbonTab;
            if (tab != null)
            {
                return tab.KeyTip;
            }
            return baseValue;
        }

        private static void OnActivatingKeyTipThunk(object sender, ActivatingKeyTipEventArgs e)
        {
            ((RibbonTabHeader)sender).OnActivatingKeyTip(e);
        }

        protected virtual void OnActivatingKeyTip(ActivatingKeyTipEventArgs e)
        {
            if (e.OriginalSource == this)
            {
                e.KeyTipHorizontalPlacement = KeyTipHorizontalPlacement.KeyTipCenterAtTargetCenter;
                e.KeyTipVerticalPlacement = KeyTipVerticalPlacement.KeyTipCenterAtTargetCenter;
                e.KeyTipHorizontalOffset = e.KeyTipVerticalOffset = 0;

                Ribbon ribbon = Ribbon;
                if (ribbon != null)
                {
                    RibbonTabHeaderItemsControl tabHeaderItemsControl = ribbon.RibbonTabHeaderItemsControl;
                    if (tabHeaderItemsControl != null &&
                        tabHeaderItemsControl.IsVisible)
                    {
                        GeneralTransform headerToItemsControl = TransformToAncestor(tabHeaderItemsControl);
                        if (headerToItemsControl != null)
                        {
                            Point tabHeaderOrigin = headerToItemsControl.Transform(new Point());
                            double tabHeaderCenterX = tabHeaderOrigin.X + (ActualWidth / 2);
                            if (DoubleUtil.LessThan(tabHeaderCenterX, 0) ||
                                DoubleUtil.GreaterThan(tabHeaderCenterX, tabHeaderItemsControl.ActualWidth))
                            {
                                e.KeyTipVisibility = Visibility.Hidden;
                            }
                            else
                            {
                                GeneralTransform itemsControlToRibbon = tabHeaderItemsControl.TransformToAncestor(ribbon);
                                if (itemsControlToRibbon != null)
                                {
                                    Point tabHeaderItemsControlOrigin = itemsControlToRibbon.Transform(new Point());
                                    double horizontalOffset = tabHeaderCenterX + tabHeaderItemsControlOrigin.X;
                                    if (DoubleUtil.LessThan(horizontalOffset, 0) ||
                                        DoubleUtil.GreaterThan(horizontalOffset, ribbon.ActualWidth))
                                    {
                                        e.KeyTipVisibility = Visibility.Hidden;
                                    }
                                    else
                                    {
                                        // Determine the position of keytip with respect to Ribbon,
                                        // so that it does not get clipped.
                                        e.KeyTipHorizontalOffset = horizontalOffset;
                                        e.KeyTipVerticalOffset = tabHeaderItemsControlOrigin.Y + tabHeaderOrigin.Y + ActualHeight + KeyTipVerticalOffset;
                                        e.KeyTipHorizontalPlacement = KeyTipHorizontalPlacement.KeyTipCenterAtTargetLeft;
                                        e.KeyTipVerticalPlacement = KeyTipVerticalPlacement.KeyTipCenterAtTargetTop;
                                        e.PlacementTarget = ribbon;
                                    }
                                }
                                else
                                {
                                    e.KeyTipVisibility = Visibility.Hidden;
                                }
                            }
                        }
                        else
                        {
                            e.KeyTipVisibility = Visibility.Hidden;
                        }
                    }
                }
            }
        }

        private static void OnKeyTipAccessedThunk(object sender, KeyTipAccessedEventArgs e)
        {
            ((RibbonTabHeader)sender).OnKeyTipAccessed(e);
        }

        protected virtual void OnKeyTipAccessed(KeyTipAccessedEventArgs e)
        {
            if (e.OriginalSource == this)
            {
                RibbonTab tab = RibbonTab;
                if (tab != null)
                {
                    // Select the tab and make it next keytip scope.
                    tab.IsSelected = true;
                    if (KeyTipService.GetIsKeyTipScope(tab))
                    {
                        e.TargetKeyTipScope = tab;
                    }
                }
                // Focus self.
                this.Focus();
                Ribbon ribbon = Ribbon;
                if (ribbon != null)
                {
                    if (ribbon.IsMinimized && !ribbon.IsDropDownOpen)
                    {
                        // Open dropdown of ribbon.
                        ribbon.IsDropDownOpen = true;
                    }
                }

                e.Handled = true;
            }
        }

        #endregion
    }
}
