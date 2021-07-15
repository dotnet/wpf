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
    using System.Windows;
    using System.Windows.Automation.Peers;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
#if RIBBON_IN_FRAMEWORK
    using Microsoft.Windows.Controls;
#else
    using Microsoft.Windows.Automation.Peers;
#endif

    #endregion Using declarations

    /// <summary>
    ///   Implements the functionality for a Ribbon's ApplicationMenu. 
    /// </summary>
    [TemplatePart(Name = RibbonApplicationMenu.PopupToggleButtonTemplateName, Type = typeof(RibbonToggleButton))]
    [TemplatePart(Name = RibbonApplicationMenu.PopupTemplateName, Type = typeof(Popup))]
    [TemplatePart(Name = RibbonApplicationMenu.SubmenuTemplateName, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = RibbonApplicationMenu.FooterPaneTemplateName, Type = typeof(ContentPresenter))]
    [TemplatePart(Name = RibbonApplicationMenu.AuxiliaryPaneTemplateName, Type = typeof(ContentPresenter))]
    [StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(RibbonApplicationMenuItem))]
    public class RibbonApplicationMenu : RibbonMenuButton
    {
        #region Fields

        /// <summary>
        ///   The Popup for the RibbonApplicationMenu.
        /// </summary>
        private Popup _popup;

        /// <summary>
        ///   The RibbonToggleButton in the RibbonApplicationMenu's style that overlays the Popup
        ///   in order to for the popup to look as if it is placed behind its placement target. 
        /// </summary>
        private RibbonToggleButton _popupToggleButton;

        /// <summary>
        ///   The ContentPresenter for the FooterPane's content.
        /// </summary>
        private ContentPresenter _footerPaneHost;

        /// <summary>
        ///   The ContentPresenter for the AuxiliaryPane's content.
        /// </summary>
        private ContentPresenter _auxiliaryPaneHost;

        /// <summary>
        /// Template parts names
        /// </summary>
        private const string PopupTemplateName = "PART_Popup";
        private const string PopupToggleButtonTemplateName = "PART_PopupToggleButton";
        private const string SubmenuTemplateName = "PART_SubmenuPlaceholder";
        private const string FooterPaneTemplateName = "PART_FooterPaneContentPresenter";
        private const string AuxiliaryPaneTemplateName = "PART_AuxiliaryPaneContentPresenter";
        #endregion

        #region Constructors

        /// <summary>
        ///   Initializes static members of the RibbonApplicationMenu class.  Also, overrides
        ///   the default style, and registers class handlers for a couple events.
        /// </summary>
        static RibbonApplicationMenu()
        {
            Type ownerType = typeof(RibbonApplicationMenu);

            DefaultStyleKeyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(ownerType));

            RibbonControlService.IsInControlGroupPropertyKey.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(false, null, new CoerceValueCallback(CoerceToFalse)));
            RibbonControlService.IsInQuickAccessToolBarPropertyKey.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(false, null, new CoerceValueCallback(CoerceToFalse)));
            RibbonControlService.CanAddToQuickAccessToolBarDirectlyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(false, null, new CoerceValueCallback(CoerceToFalse)));

            RibbonMenuButton.CanUserResizeVerticallyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(false, null, new CoerceValueCallback(CoerceToFalse)));
            RibbonMenuButton.CanUserResizeHorizontallyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(false, null, new CoerceValueCallback(CoerceToFalse)));
        }

        #endregion

        #region ContentModel

        /// <summary>
        ///   Gets or sets the FooterPaneContent of the RibbonApplicationMenu.
        /// </summary>
        public Object FooterPaneContent
        {
            get { return (Object)GetValue(FooterPaneContentProperty); }
            set { SetValue(FooterPaneContentProperty, value); }
        }

        /// <summary>
        ///   Using a DependencyProperty as the backing store for FooterPaneContent.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty FooterPaneContentProperty =
                     DependencyProperty.Register(
                            "FooterPaneContent",
                            typeof(Object),
                            typeof(RibbonApplicationMenu),
                            new FrameworkPropertyMetadata(null));
        /// <summary>
        ///   Gets or sets the FooterPaneContentTemplate of the RibbonApplicationMenu.
        /// </summary>
        public DataTemplate FooterPaneContentTemplate
        {
            get { return (DataTemplate)GetValue(FooterPaneContentTemplateProperty); }
            set { SetValue(FooterPaneContentTemplateProperty, value); }
        }

        /// <summary>
        ///   Using a DependencyProperty as the backing store for FooterPaneContent.
        /// </summary>
        public static readonly DependencyProperty FooterPaneContentTemplateProperty =
                     DependencyProperty.Register(
                            "FooterPaneContentTemplate",
                            typeof(DataTemplate),
                            typeof(RibbonApplicationMenu),
                            new FrameworkPropertyMetadata(null));

        /// <summary>
        ///   Gets or sets the FooterPaneContentTemplateSelector of the RibbonApplicationMenu.
        /// </summary>
        public DataTemplateSelector FooterPaneContentTemplateSelector
        {
            get { return (DataTemplateSelector)GetValue(FooterPaneContentTemplateSelectorProperty); }
            set { SetValue(FooterPaneContentTemplateSelectorProperty, value); }
        }

        /// <summary>
        ///   Using a DependencyProperty as the backing store for FooterPaneContent.
        /// </summary>
        public static readonly DependencyProperty FooterPaneContentTemplateSelectorProperty =
                     DependencyProperty.Register(
                            "FooterPaneContentTemplateSelector",
                            typeof(DataTemplateSelector),
                            typeof(RibbonApplicationMenu),
                            new FrameworkPropertyMetadata(null));

        /// <summary>
        ///   Gets or sets the AuxiliaryPaneContent of the RibbonApplicationMenu.
        /// </summary>
        public object AuxiliaryPaneContent
        {
            get { return (object)GetValue(AuxiliaryPaneContentProperty); }
            set { SetValue(AuxiliaryPaneContentProperty, value); }
        }

        /// <summary>
        ///   Using a DependencyProperty as the backing store for AuxiliaryPaneContent.
        /// </summary>
        public static readonly DependencyProperty AuxiliaryPaneContentProperty =
                    DependencyProperty.Register(
                            "AuxiliaryPaneContent",
                            typeof(object),
                            typeof(RibbonApplicationMenu),
                            new FrameworkPropertyMetadata(null));

        /// <summary>
        ///   Gets or sets the AuxiliaryPaneContentTemplate of the RibbonApplicationMenu.
        /// </summary>
        public DataTemplate AuxiliaryPaneContentTemplate
        {
            get { return (DataTemplate)GetValue(AuxiliaryPaneContentTemplateProperty); }
            set { SetValue(AuxiliaryPaneContentTemplateProperty, value); }
        }

        /// <summary>
        ///   Using a DependencyProperty as the backing store for AuxiliaryPaneContentTemplate.
        /// </summary>
        public static readonly DependencyProperty AuxiliaryPaneContentTemplateProperty =
                    DependencyProperty.Register(
                            "AuxiliaryPaneContentTemplate",
                            typeof(DataTemplate),
                            typeof(RibbonApplicationMenu),
                            new FrameworkPropertyMetadata(null));

        /// <summary>
        ///   Gets or sets the AuxiliaryPaneContentTemplateSelector of the RibbonApplicationMenu.
        /// </summary>
        public DataTemplateSelector AuxiliaryPaneContentTemplateSelector
        {
            get { return (DataTemplateSelector)GetValue(AuxiliaryPaneContentTemplateSelectorProperty); }
            set { SetValue(AuxiliaryPaneContentTemplateSelectorProperty, value); }
        }

        /// <summary>
        ///   Using a DependencyProperty as the backing store for AuxiliaryPaneContentTemplateSelector.
        /// </summary>
        public static readonly DependencyProperty AuxiliaryPaneContentTemplateSelectorProperty =
                    DependencyProperty.Register(
                            "AuxiliaryPaneContentTemplateSelector",
                            typeof(DataTemplateSelector),
                            typeof(RibbonApplicationMenu),
                            new FrameworkPropertyMetadata(null));


        #endregion ContentModel

        #region ContainerGeneration

        private object _currentItem;

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            bool ret = (item is RibbonApplicationMenuItem) || (item is RibbonApplicationSplitMenuItem) || (item is RibbonSeparator) || (item is RibbonGallery);
            if (!ret)
            {
                _currentItem = item;
            }

            return ret;
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            object currentItem = _currentItem;
            _currentItem = null;

            if (UsesItemContainerTemplate)
            {
                DataTemplate itemContainerTemplate = ItemContainerTemplateSelector.SelectTemplate(currentItem, this);
                if (itemContainerTemplate != null)
                {
                    object itemContainer = itemContainerTemplate.LoadContent();
                    if (itemContainer is RibbonApplicationMenuItem || itemContainer is RibbonApplicationSplitMenuItem || itemContainer is RibbonSeparator || itemContainer is RibbonGallery)
                    {
                        return itemContainer as DependencyObject;
                    }
                    else
                    {
                        throw new InvalidOperationException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.InvalidApplicationMenuOrItemContainer, this.GetType().Name, itemContainer));
                    }
                }
            }

            return new RibbonApplicationMenuItem();
        }

        protected override bool ShouldApplyItemContainerStyle(DependencyObject container, object item)
        {
            if (container is RibbonApplicationSplitMenuItem ||
                container is RibbonSeparator ||
                container is RibbonGallery)
            {
                return false;
            }
            else
            {
                return base.ShouldApplyItemContainerStyle(container, item);
            }
        }

        #endregion ContainerGeneration

        #region UIAutomation

        internal UIElement FooterPaneHost 
        {
            get { return _footerPaneHost; }
        }

        internal UIElement AuxiliaryPaneHost
        {
            get { return _auxiliaryPaneHost; }
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new RibbonApplicationMenuAutomationPeer(this);
        }

        #endregion UIAutomation

        #region Popup Placement

        /// <summary>
        ///   Invoked whenever the control's template is applied.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Cleanup the previous template elements
            if (_popup != null)
            {
                _popup.Opened -= new EventHandler(this.OnPopupOpened);
            }

            _popup = this.GetTemplateChild(PopupTemplateName) as Popup;
            SubmenuPlaceholder = this.GetTemplateChild(SubmenuTemplateName) as FrameworkElement;
            _popupToggleButton = this.GetTemplateChild(PopupToggleButtonTemplateName) as RibbonToggleButton;
            _footerPaneHost = this.GetTemplateChild(FooterPaneTemplateName) as ContentPresenter;
            _auxiliaryPaneHost = this.GetTemplateChild(AuxiliaryPaneTemplateName) as ContentPresenter;

            if (_popup != null)
            {
                _popup.Opened += new EventHandler(this.OnPopupOpened);
            }
        }

        /// <summary>
        ///   Using a DependencyProperty as the backing store for SubmenuPlaceholder.
        /// </summary>
        internal static readonly DependencyProperty SubmenuPlaceholderProperty =
                     DependencyProperty.Register(
                            "SubmenuPlaceholder",
                            typeof(FrameworkElement),
                            typeof(RibbonApplicationMenu));

        /// <summary>
        ///   Gets or sets the SubmenuPlaceholder of the RibbonApplicationMenu. This 
        ///   internal DependencyProperty is bound to by all of the top level 
        ///   RibbonApplicationMenuItems and RibbonApplicationSplitMenuItems as the 
        ///   PlacementTarget for their submenu Popups.
        /// </summary>
        internal FrameworkElement SubmenuPlaceholder
        {
            get { return (FrameworkElement)GetValue(SubmenuPlaceholderProperty); }
            set { SetValue(SubmenuPlaceholderProperty, value); }
        }

        /// <summary>
        ///   Called when the RibbonApplicationMenu's popup is opened. This code is placing a 
        ///   popup toggle button on top of the main toggle button to achieve a look of popup
        ///   being under the menu button.
        /// </summary>
        /// <param name="sender">The RibbonApplicationMenu whose Popup is opening.</param>
        /// <param name="e">The event data.</param>
        private void OnPopupOpened(object sender, EventArgs e)
        {
            // Position the inner ToggleButton to render on top of main ToggleButton
            if (_popupToggleButton != null && PartToggleButton != null)
            {
                double currentXPosition = Canvas.GetLeft(_popupToggleButton);
                if (double.IsNaN(currentXPosition))
                {
                    currentXPosition = 0;
                }

                double currentYPosition = Canvas.GetTop(_popupToggleButton);
                if (double.IsNaN(currentYPosition))
                {
                    currentYPosition = 0;
                }

                Point mainToggleButtonScreenPosition = PartToggleButton.PointToScreen(new Point());
                Point popupToggleButtonOffset = _popupToggleButton.PointFromScreen(mainToggleButtonScreenPosition);
                Canvas.SetLeft(_popupToggleButton, currentXPosition + popupToggleButtonOffset.X);
                Canvas.SetTop(_popupToggleButton, currentYPosition + popupToggleButtonOffset.Y);
            }
        }

        #endregion

        #region Private Methods

        private static object CoerceToFalse(DependencyObject d, object value)
        {
            return false;
        }

        #endregion

        #region KeyTips

        protected override void OnActivatingKeyTip(ActivatingKeyTipEventArgs e)
        {
            if (e.OriginalSource == this)
            {
                e.KeyTipHorizontalPlacement = KeyTipHorizontalPlacement.KeyTipLeftAtTargetCenter;
                e.KeyTipVerticalPlacement = KeyTipVerticalPlacement.KeyTipTopAtTargetCenter;
                e.KeyTipHorizontalOffset = e.KeyTipVerticalOffset = 0;
            }
        }

        #endregion

        #region Input

        /// <summary>
        ///     Helper method to move focus into auxiliary pane.
        /// </summary>
        internal bool AuxiliaryPaneMoveFocus(FocusNavigationDirection direction)
        {
            UIElement auxiliaryPaneHost = AuxiliaryPaneHost;
            if (auxiliaryPaneHost != null &&
                auxiliaryPaneHost.IsVisible &&
                auxiliaryPaneHost.IsEnabled &&
                auxiliaryPaneHost.MoveFocus(new TraversalRequest(direction)))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        ///     Helper method to move focus into the footer pane
        /// </summary>
        internal bool FooterPaneMoveFocus(FocusNavigationDirection direction)
        {
            UIElement footerPaneHost = FooterPaneHost;
            if (footerPaneHost != null &&
                footerPaneHost.IsVisible &&
                footerPaneHost.IsEnabled &&
                footerPaneHost.MoveFocus(new TraversalRequest(direction)))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        ///     Helper method to move focus into the items pane.
        /// </summary>
        internal bool ItemsPaneMoveFocus(FocusNavigationDirection direction)
        {
            UIElement subMenuScrollViewer = SubMenuScrollViewer;
            if (subMenuScrollViewer != null &&
                subMenuScrollViewer.MoveFocus(new TraversalRequest(direction)))
            {
                return true;
            }
            return false;
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Handled)
            {
                return;
            }
            if (e.Key == Key.Down)
            {
                DependencyObject element = e.OriginalSource as DependencyObject;
                if (element != null)
                {
                    UIElement footerPaneHost = FooterPaneHost;
                    if (footerPaneHost != null &&
                        footerPaneHost.IsKeyboardFocusWithin &&
                        TreeHelper.IsVisualAncestorOf(footerPaneHost, element))
                    {
                        DependencyObject nextFocus = RibbonHelper.PredictFocus(element, FocusNavigationDirection.Down);
                        if (nextFocus == null ||
                            nextFocus == element)
                        {
                            // If the focus is on the last element of footer pane,
                            // then try moving focus into items pane and then into
                            // auxiliary pane if needed.
                            if (ItemsPaneMoveFocus(FocusNavigationDirection.First) ||
                                AuxiliaryPaneMoveFocus(FocusNavigationDirection.First))
                            {
                                e.Handled = true;
                            }
                        }
                    }
                }
            }
            else if (e.Key == Key.Up)
            {
                UIElement popupChild = _popup.TryGetChild();
                if (popupChild != null &&
                    !popupChild.IsKeyboardFocusWithin)
                {
                    // If the popup does not have focus with in then try moving focus to
                    // last element of FooterPane and then to the last element of
                    // auxiliary pane if needed.
                    if (FooterPaneMoveFocus(FocusNavigationDirection.Last) ||
                        AuxiliaryPaneMoveFocus(FocusNavigationDirection.Last))
                    {
                        e.Handled = true;
                    }
                }
                else
                {
                    DependencyObject element = e.OriginalSource as DependencyObject;
                    if (element != null)
                    {
                        UIElement auxilaryPaneHost = AuxiliaryPaneHost;
                        if (auxilaryPaneHost != null && 
                            auxilaryPaneHost.IsKeyboardFocusWithin && 
                            TreeHelper.IsVisualAncestorOf(auxilaryPaneHost, element))
                        {
                            DependencyObject nextFocus = RibbonHelper.PredictFocus(element, FocusNavigationDirection.Up);
                            if (nextFocus == null ||
                                nextFocus == element)
                            {
                                // If the focus is on last first element of auxiliary pane,
                                // then try moving focus to last element of Items Pane and
                                // then to last element of FooterPane if needed.
                                if (ItemsPaneMoveFocus(FocusNavigationDirection.Last) ||
                                    FooterPaneMoveFocus(FocusNavigationDirection.Last))
                                {
                                    e.Handled = true;
                                }
                            }
                        }
                    }
                }
            }
            else if (e.Key == Key.Left ||
                e.Key == Key.Right)
            {
                DependencyObject element = e.OriginalSource as DependencyObject;
                if (element != null)
                {
                    if ((e.Key == Key.Left) == (FlowDirection == FlowDirection.LeftToRight))
                    {
                        UIElement auxilaryPaneHost = AuxiliaryPaneHost;
                        if (auxilaryPaneHost != null &&
                            auxilaryPaneHost.IsKeyboardFocusWithin &&
                            TreeHelper.IsVisualAncestorOf(auxilaryPaneHost, element))
                        {
                            // If the effective key is left and the focus is on left most element
                            // of auxiliary pane, then move focus to nearest element to the left of
                            // auxiliarypane.
                            DependencyObject nextFocus = RibbonHelper.PredictFocus(element, FocusNavigationDirection.Left);
                            if (nextFocus != null &&
                                !TreeHelper.IsVisualAncestorOf(auxilaryPaneHost, nextFocus))
                            {
                                if (RibbonHelper.Focus(nextFocus))
                                {
                                    e.Handled = true;
                                }
                            }
                        }
                    }
                    else if (e.Key == Key.Left)
                    {
                        ScrollViewer subMenuScrollViewer = SubMenuScrollViewer;
                        if (subMenuScrollViewer != null &&
                            subMenuScrollViewer.IsKeyboardFocusWithin &&
                            TreeHelper.IsVisualAncestorOf(subMenuScrollViewer, element))
                        {
                            // If the flow direction is RightToLeft and the key is Left,
                            // and the focus is in items pane, move the the focus outside teh
                            // items pane if needed.
                            RibbonMenuItem menuItem = element as RibbonMenuItem;
                            if (menuItem == null)
                            {
                                menuItem = TreeHelper.FindVisualAncestor<RibbonMenuItem>(element);
                            }
                            if (menuItem != null &&
                                !menuItem.CanOpenSubMenu)
                            {
                                DependencyObject nextFocus = menuItem.PredictFocus(FocusNavigationDirection.Right);
                                if (nextFocus != null)
                                {
                                    if (RibbonHelper.Focus(nextFocus))
                                    {
                                        e.Handled = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            base.OnPreviewKeyDown(e);
        }

        #endregion
    }
}
