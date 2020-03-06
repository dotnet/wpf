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
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Automation;
    using System.Windows.Automation.Peers;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using System.Windows.Threading;
#if RIBBON_IN_FRAMEWORK
    using System.Windows.Controls.Ribbon.Primitives;
    using Microsoft.Windows.Controls;
#else
    using Microsoft.Windows.Automation.Peers;
    using Microsoft.Windows.Controls.Ribbon.Primitives;
#endif
    using MS.Internal;

    #endregion

    /// <summary>
    ///   A RibbonGroup represents a logical group of controls as they appear on
    ///   a RibbonTab.  These groups resize independently of one another to layout
    ///   their controls in the largest available space.
    /// </summary>
    [TemplatePart(Name = RibbonGroup.CollapsedDropDownButtonTemplatePartName, Type = typeof(RibbonToggleButton))]
    [TemplatePart(Name = RibbonGroup.HeaderContentPresenterTemplatePartName, Type = typeof(ContentPresenter))]
    [TemplatePart(Name = RibbonGroup.HotBackgroundBorderTemplatePartName, Type = typeof(Border))]
    [TemplatePart(Name = RibbonGroup.ItemsPresenterTemplatePartName, Type = typeof(ItemsPresenter))]
    [TemplatePart(Name = RibbonGroup.PopupGridTemplatePartName, Type = typeof(Grid))]
    [TemplatePart(Name = RibbonGroup.PopupTemplatePartName, Type = typeof(Popup))]
    [TemplatePart(Name = RibbonGroup.TemplateContentControlTemplatePartName, Type = typeof(ContentControl))]
    public class RibbonGroup : HeaderedItemsControl
    {

        #region Constructors

        /// <summary>
        ///   Initializes static members of the RibbonGroup class.
        ///   This overrides the default style.
        /// </summary>
        static RibbonGroup()
        {
            Type ownerType = typeof(RibbonGroup);

            DefaultStyleKeyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(ownerType));
            ItemsPanelProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(new ItemsPanelTemplate(new FrameworkElementFactory(typeof(RibbonGroupItemsPanel)))));
            ToolTipService.ShowOnDisabledProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(true));
            HeaderProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnHeaderChanged)));
            RibbonControlService.IsInQuickAccessToolBarPropertyKey.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(new PropertyChangedCallback(OnIsInQuickAccessToolBarChanged)));
            ToolTipProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(null, new CoerceValueCallback(CoerceToolTip)));
            ContextMenuProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(RibbonHelper.OnContextMenuChanged, RibbonHelper.OnCoerceContextMenu));
            ContextMenuService.ShowOnDisabledProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(true));
            FocusableProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(false));
            ForegroundProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(new PropertyChangedCallback(OnForegroundChanged)));
#if RIBBON_IN_FRAMEWORK
            AutomationProperties.IsOffscreenBehaviorProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(IsOffscreenBehavior.FromClip));
#endif

            EventManager.RegisterClassHandler(ownerType, Mouse.PreviewMouseDownOutsideCapturedElementEvent, new MouseButtonEventHandler(OnClickThroughThunk));
            EventManager.RegisterClassHandler(ownerType, Mouse.LostMouseCaptureEvent, new MouseEventHandler(OnLostMouseCaptureThunk), true /* handledEventsToo */);
            EventManager.RegisterClassHandler(ownerType, RibbonControlService.DismissPopupEvent, new RibbonDismissPopupEventHandler(OnDismissPopupThunk));
            EventManager.RegisterClassHandler(ownerType, Mouse.MouseDownEvent, new MouseButtonEventHandler(OnMouseDownThunk), true);
            EventManager.RegisterClassHandler(ownerType, FrameworkElement.ContextMenuOpeningEvent, new ContextMenuEventHandler(OnContextMenuOpeningThunk), true);
            EventManager.RegisterClassHandler(ownerType, FrameworkElement.ContextMenuClosingEvent, new ContextMenuEventHandler(OnContextMenuClosingThunk), true);
            EventManager.RegisterClassHandler(ownerType, KeyTipService.ActivatingKeyTipEvent, new ActivatingKeyTipEventHandler(OnActivatingKeyTipThunk));
            EventManager.RegisterClassHandler(ownerType, KeyTipService.KeyTipAccessedEvent, new KeyTipAccessedEventHandler(OnKeyTipAccessedThunk));
        }

        /// <summary>
        ///   Creates an instance of the RibbonGroup class.
        /// </summary>
        public RibbonGroup()
        {
            RibbonGroupSizeDefinitionBaseCollection collection = new RibbonGroupSizeDefinitionBaseCollection();
            _defaultGroupSizeDefinitionsRef = new WeakReference(collection);
            GroupSizeDefinitions = collection;

            this.Loaded += delegate
            {
                // Because we queue up asynchronous group size definition updates on the dispatcher
                // with Loaded priority, we need to update group size definitions once here to prevent an
                // unnecessary 2nd render on startup. Before the GroupSizeDefinitions value has been
                // properly coerced, we fall back to _defaultGroupSizeDefinitions, and controls under 
                // RibbonContentPresenters will initially be laid out in their large variant, regardless
                // of the current width constraint. Note that there still exists a corner case that
                // is not handled: if the RibbonGroup starts out with a width that is too small to
                // accommodate the large variant, and an item is added to the items collection, that
                // new item will get rendered in the large variant once, and then will get re-rendered
                // at its correct size variant once the asynchronous callback at Loaded priority comes
                // through.

                UpdateGroupSizeDefinitionsCallback();
            };

            // Enables custom keytip siblings for the group
            KeyTipService.SetCustomSiblingKeyTipElements(this, new RibbonGroupCustomKeyTipSiblings(this));
        }

        #endregion

        #region Overrides

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _headerContentPresenter = this.GetTemplateChild(HeaderContentPresenterTemplatePartName) as ContentPresenter;
            _collapsedDropDownButton = this.GetTemplateChild(CollapsedDropDownButtonTemplatePartName) as FrameworkElement;
            _templateContentControl = GetTemplateChild(TemplateContentControlTemplatePartName) as ContentControl;
            _itemsPresenter = GetTemplateChild(ItemsPresenterTemplatePartName) as ItemsPresenter;
            _collapsedGroupPopup = GetTemplateChild(PopupTemplatePartName) as Popup;

            if (_hotBackgroundBorder != null)
            {
                _mouseEnterStoryboard = null;
                _mouseLeaveStoryboard = null;
            }

            _hotBackgroundBorder = GetTemplateChild(HotBackgroundBorderTemplatePartName) as Border;

            if (_hotBackgroundBorder != null)
            {
                _mouseEnterStoryboard = new Storyboard();
                _mouseEnterStoryboard.Children.Add(CreateOpacityAnimation(true, _hotBackgroundBorder));
                _mouseLeaveStoryboard = new Storyboard();
                _mouseLeaveStoryboard.Children.Add(CreateOpacityAnimation(false, _hotBackgroundBorder));

                Grid popupGrid = this.GetTemplateChild(PopupGridTemplatePartName) as Grid;
                if (popupGrid != null)
                {
                    popupGrid.MouseEnter += (s, e) =>
                        {
                            _mouseEnterStoryboard.Stop();
                            _mouseEnterStoryboard.Begin();
                        };

                    popupGrid.MouseLeave += (s, e) =>
                        {
                            _mouseLeaveStoryboard.Stop();
                            _mouseLeaveStoryboard.Begin();
                        };
                }
            }

            CoerceValue(ToolTipProperty);
            RibbonHelper.SetContentAsToolTip(this, this.VisualChild, this.Header, (IsCollapsed && !IsDropDownOpen));

            PropertyHelper.TransferProperty(this, ContextMenuProperty);   // Coerce to get a default ContextMenu if none has been specified.
            PropertyHelper.TransferProperty(this, RibbonControlService.CanAddToQuickAccessToolBarDirectlyProperty);

            RibbonGroupSizeDefinitionBaseCollection groupSizeDefinitions = GroupSizeDefinitions;
            if (groupSizeDefinitions != null &&
                _sizeDefinitionIndex >= 0 &&
                _sizeDefinitionIndex < groupSizeDefinitions.Count)
            {
                SetAppropriatePresenterVisibility(groupSizeDefinitions[_sizeDefinitionIndex] is RibbonGroupSizeDefinition ? Visibility.Visible : Visibility.Collapsed);
            }
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);

            if (_mouseEnterStoryboard != null &&
                !IsCollapsed)
            {
                _mouseEnterStoryboard.Stop();
                _mouseEnterStoryboard.Begin();
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            if (_mouseLeaveStoryboard != null &&
                !IsCollapsed)
            {
                _mouseLeaveStoryboard.Stop();
                _mouseLeaveStoryboard.Begin();
            }
        }

#if RIBBON_IN_FRAMEWORK
        protected internal override void OnRenderSizeChanged(SizeChangedInfo info)
#else
        protected override void OnRenderSizeChanged(SizeChangedInfo info)
#endif
        {
            base.OnRenderSizeChanged(info);
            if (info.WidthChanged)
            {
                RibbonGroupsPanel groupsPanel = VisualTreeHelper.GetParent(this) as RibbonGroupsPanel;
                if (groupsPanel != null)
                {
                    groupsPanel.OnChildGroupRenderSizeChanged(this, info.PreviousSize.Width);
                }
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets a value indicating whether or not the RibbonGroup is displayed in a "Collapsed"
        ///     state. In this state the RibbonGroup looks like a drop-down button.
        /// </summary>
        public bool IsCollapsed
        {
            get { return (bool)GetValue(IsCollapsedProperty); }
            internal set { SetValue(IsCollapsedPropertyKey, value); }
        }

        /// <summary>
        ///   Key for the IsCollapsed DependencyProperty.
        /// </summary>
        private static readonly DependencyPropertyKey IsCollapsedPropertyKey =
                    DependencyProperty.RegisterReadOnly(
                            "IsCollapsed",
                            typeof(bool),
                            typeof(RibbonGroup),
                            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnIsCollapsedChanged), new CoerceValueCallback(CoerceIsCollapsed)));

        private static void OnIsCollapsedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonGroup ribbonGroup = (RibbonGroup)d;
            RibbonHelper.DelayCoerceProperty(ribbonGroup, IsDropDownOpenProperty);
            ribbonGroup.CoerceValue(ToolTipProperty);
            RibbonHelper.SetContentAsToolTip(ribbonGroup, ribbonGroup.VisualChild, ribbonGroup.Header, (ribbonGroup.IsCollapsed && !ribbonGroup.IsDropDownOpen));
        }

        private static object CoerceIsCollapsed(DependencyObject d, object baseValue)
        {
            RibbonGroup group = (RibbonGroup)d;
            if (group.IsInQuickAccessToolBar)
            {
                return true;
            }
            return baseValue;
        }

        /// <summary>
        ///   Using a DependencyProperty as the backing store for IsCollapsedProperty.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty IsCollapsedProperty =
                IsCollapsedPropertyKey.DependencyProperty;


        /// <summary>
        ///     Gets or sets the GroupSizeDefinitions property.  This is a collection of RibbonGroupSizeDefinitions which describe
        ///     how the controls in the RibbonGroup should be sized for different size variations of the RibbonGroup itself.
        /// </summary>
        public RibbonGroupSizeDefinitionBaseCollection GroupSizeDefinitions
        {
            get { return (RibbonGroupSizeDefinitionBaseCollection)GetValue(GroupSizeDefinitionsProperty); }
            set { SetValue(GroupSizeDefinitionsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for GroupSizeDefinitions.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GroupSizeDefinitionsProperty =
            DependencyProperty.Register(
                "GroupSizeDefinitions", 
                typeof(RibbonGroupSizeDefinitionBaseCollection), 
                typeof(RibbonGroup), 
                new FrameworkPropertyMetadata(new PropertyChangedCallback(OnGroupSizeDefinitionsChanged), new CoerceValueCallback(CoerceGroupSizeDefinitions)));

        #endregion

        #region RibbonControlService Properties

        /// <summary>
        ///     DependencyProperty for SmallImageSource property.
        /// </summary>
        public static readonly DependencyProperty SmallImageSourceProperty =
            RibbonControlService.SmallImageSourceProperty.AddOwner(typeof(RibbonGroup));

        /// <summary>
        ///     ImageSource property which is normally a 16X16 icon.
        /// </summary>
        public ImageSource SmallImageSource
        {
            get { return RibbonControlService.GetSmallImageSource(this); }
            set { RibbonControlService.SetSmallImageSource(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for LargeImageSource property.
        /// </summary>
        public static readonly DependencyProperty LargeImageSourceProperty =
            RibbonControlService.LargeImageSourceProperty.AddOwner(typeof(RibbonGroup));

        /// <summary>
        ///     ImageSource property which is normally a 32X32 icon.
        /// </summary>
        public ImageSource LargeImageSource
        {
            get { return RibbonControlService.GetLargeImageSource(this); }
            set { RibbonControlService.SetLargeImageSource(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for ToolTipTitle property.
        /// </summary>
        public static readonly DependencyProperty ToolTipTitleProperty =
            RibbonControlService.ToolTipTitleProperty.AddOwner(typeof(RibbonGroup), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

        /// <summary>
        ///     Title text for the tooltip of the control.
        /// </summary>
        public string ToolTipTitle
        {
            get { return RibbonControlService.GetToolTipTitle(this); }
            set { RibbonControlService.SetToolTipTitle(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for ToolTipDescription property.
        /// </summary>
        public static readonly DependencyProperty ToolTipDescriptionProperty =
            RibbonControlService.ToolTipDescriptionProperty.AddOwner(typeof(RibbonGroup), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

        /// <summary>
        ///     Description text for the tooltip of the control.
        /// </summary>
        public string ToolTipDescription
        {
            get { return RibbonControlService.GetToolTipDescription(this); }
            set { RibbonControlService.SetToolTipDescription(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for ToolTipImageSource property.
        /// </summary>
        public static readonly DependencyProperty ToolTipImageSourceProperty =
            RibbonControlService.ToolTipImageSourceProperty.AddOwner(typeof(RibbonGroup), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

        /// <summary>
        ///     Image source for the tooltip of the control.
        /// </summary>
        public ImageSource ToolTipImageSource
        {
            get { return RibbonControlService.GetToolTipImageSource(this); }
            set { RibbonControlService.SetToolTipImageSource(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for ToolTipFooterTitle property.
        /// </summary>
        public static readonly DependencyProperty ToolTipFooterTitleProperty =
            RibbonControlService.ToolTipFooterTitleProperty.AddOwner(typeof(RibbonGroup), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

        /// <summary>
        ///     Title text for the footer of tooltip of the control.
        /// </summary>
        public string ToolTipFooterTitle
        {
            get { return RibbonControlService.GetToolTipFooterTitle(this); }
            set { RibbonControlService.SetToolTipFooterTitle(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for ToolTipFooterDescription property.
        /// </summary>
        public static readonly DependencyProperty ToolTipFooterDescriptionProperty =
            RibbonControlService.ToolTipFooterDescriptionProperty.AddOwner(typeof(RibbonGroup), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

        /// <summary>
        ///     Description text for the footer of the tooltip of the control.
        /// </summary>
        public string ToolTipFooterDescription
        {
            get { return RibbonControlService.GetToolTipFooterDescription(this); }
            set { RibbonControlService.SetToolTipFooterDescription(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for ToolTipFooterImageSource property.
        /// </summary>
        public static readonly DependencyProperty ToolTipFooterImageSourceProperty =
            RibbonControlService.ToolTipFooterImageSourceProperty.AddOwner(typeof(RibbonGroup), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

        /// <summary>
        ///     Image source for the footer of the tooltip of the control.
        /// </summary>
        public ImageSource ToolTipFooterImageSource
        {
            get { return RibbonControlService.GetToolTipFooterImageSource(this); }
            set { RibbonControlService.SetToolTipFooterImageSource(this, value); }
        }

        #endregion

        #region Visual States

        /// <summary>
        ///     DependencyProperty for Ribbon property.
        /// </summary>
        public static readonly DependencyProperty RibbonProperty =
            RibbonControlService.RibbonProperty.AddOwner(typeof(RibbonGroup));

        /// <summary>
        ///     This property is used to access visual style brushes defined on the Ribbon class.
        /// </summary>
        public Ribbon Ribbon
        {
            get { return RibbonControlService.GetRibbon(this); }
        }

        /// <summary>
        ///     DependencyProperty for MouseOverBorderBrush property.
        /// </summary>
        public static readonly DependencyProperty MouseOverBorderBrushProperty =
            RibbonControlService.MouseOverBorderBrushProperty.AddOwner(typeof(RibbonGroup));

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
            RibbonControlService.MouseOverBackgroundProperty.AddOwner(typeof(RibbonGroup));

        /// <summary>
        ///     Control background brush used in a "hover" state of the RibbonButton.
        /// </summary>
        public Brush MouseOverBackground
        {
            get { return RibbonControlService.GetMouseOverBackground(this); }
            set { RibbonControlService.SetMouseOverBackground(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for IsInQuickAccessToolBar property.
        /// </summary>
        public static readonly DependencyProperty IsInQuickAccessToolBarProperty =
            RibbonControlService.IsInQuickAccessToolBarProperty.AddOwner(typeof(RibbonGroup));

        /// <summary>
        ///     This property indicates whether the control is part of a QuickAccessToolBar.
        /// </summary>
        public bool IsInQuickAccessToolBar
        {
            get { return RibbonControlService.GetIsInQuickAccessToolBar(this); }
            internal set { RibbonControlService.SetIsInQuickAccessToolBar(this, value); }
        }

        #endregion

        #region Private Properties

        /// <summary>
        ///  Gets the first visual child
        /// </summary>
        private FrameworkElement VisualChild
        {
            get
            {
                return VisualChildrenCount == 0 ? null : (GetVisualChild(0) as FrameworkElement);
            }
        }

        /// <summary>
        ///   Gets or sets the GroupSizeDefinitionsInternal property.  This property supplies default
        ///   values for the GroupSizeDefinitions property if no other value has been assigned.
        /// </summary>
        private RibbonGroupSizeDefinitionBaseCollection GroupSizeDefinitionsInternal
        {
            get
            {
                RibbonGroupSizeDefinition large = GetLargeGroupSizeDefinition();
                if (large == null)
                {
                    return null;
                }

                RibbonGroupSizeDefinitionBaseCollection result = new RibbonGroupSizeDefinitionBaseCollection();
                result.Add(large);

                if (Items.Count > 3)
                {
                    // Create successively smaller group size definitions by reducing
                    // groups of 3 consecutive controls that have the same size, looping backwards
                    // cyclically, starting from the end.

                    // Examples:
                    //   L L L L -> L M M M -> L S S S -> Collapsed
                    //   L L L L L L -> L L L M M M -> M M M M M M -> M M M S S S -> S S S S S S -> Collapsed

                    RibbonGroupSizeDefinition last = large;
                    int lastRepeatStartIndex = last.ControlSizeDefinitions.Count - 1;

                    while (true)
                    {
                        RibbonGroupSizeDefinition reduced = ReduceGroupSizeDefinition(last, ref lastRepeatStartIndex);
                        if (reduced == null)
                        {
                            break;
                        }

                        result.Add(reduced);
                        last = reduced;

                        if (lastRepeatStartIndex < 3)
                        {
                            lastRepeatStartIndex = last.ControlSizeDefinitions.Count - 1;
                        }
                    }
                }
                else if (Items.Count == 3)
                {
                    // Special case: L L L -> M M M -> Collapsed (M M M doesn't reduce to S S S)

                    if (large.ControlSizeDefinitions[0].ImageSize == RibbonImageSize.Large &&
                        large.ControlSizeDefinitions[0].IsLabelVisible &&
                        large.ControlSizeDefinitions[1].ImageSize == RibbonImageSize.Large &&
                        large.ControlSizeDefinitions[1].IsLabelVisible &&
                        large.ControlSizeDefinitions[2].ImageSize == RibbonImageSize.Large &&
                        large.ControlSizeDefinitions[2].IsLabelVisible)
                    {
                        RibbonGroupSizeDefinition medium = new RibbonGroupSizeDefinition();

                        medium.ControlSizeDefinitions.Add(new RibbonControlSizeDefinition() { ImageSize = RibbonImageSize.Small, IsLabelVisible = true });
                        medium.ControlSizeDefinitions.Add(new RibbonControlSizeDefinition() { ImageSize = RibbonImageSize.Small, IsLabelVisible = true });
                        medium.ControlSizeDefinitions.Add(new RibbonControlSizeDefinition() { ImageSize = RibbonImageSize.Small, IsLabelVisible = true });

                        result.Add(medium);
                    }
                }
                else if (Items.Count == 2)
                {
                    // Special case: L L -> M M -> Collapsed

                    if (large.ControlSizeDefinitions[0].ImageSize == RibbonImageSize.Large &&
                        large.ControlSizeDefinitions[0].IsLabelVisible &&
                        large.ControlSizeDefinitions[1].ImageSize == RibbonImageSize.Large &&
                        large.ControlSizeDefinitions[1].IsLabelVisible)
                    {
                        RibbonGroupSizeDefinition medium = new RibbonGroupSizeDefinition();

                        medium.ControlSizeDefinitions.Add(new RibbonControlSizeDefinition() { ImageSize = RibbonImageSize.Small, IsLabelVisible = true });
                        medium.ControlSizeDefinitions.Add(new RibbonControlSizeDefinition() { ImageSize = RibbonImageSize.Small, IsLabelVisible = true });

                        result.Add(medium);
                    }
                }

                RibbonGroupSizeDefinition collapsed = new RibbonGroupSizeDefinition() { IsCollapsed = true };
                result.Add(collapsed);

                result.Freeze();
                return result;
            }
        }

        private static void OnForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonGroup ribbonGroup = (RibbonGroup)d;
            if (ribbonGroup._headerContentPresenter != null)
            {
                BaseValueSource newValueSource = DependencyPropertyHelper.GetValueSource(ribbonGroup, ForegroundProperty).BaseValueSource;
                if (newValueSource > BaseValueSource.Inherited && !SystemParameters.HighContrast)
                {
                    ribbonGroup._headerContentPresenter.SetValue(TextElement.ForegroundProperty, e.NewValue);
                }
                else
                {
                    ribbonGroup._headerContentPresenter.ClearValue(TextElement.ForegroundProperty);
                }
            }
        }

        public bool IsDropDownOpen
        {
            get { return (bool)GetValue(IsDropDownOpenProperty); }
            set { SetValue(IsDropDownOpenProperty, value); }
        }

        public static readonly DependencyProperty IsDropDownOpenProperty = 
                                                        DependencyProperty.Register("IsDropDownOpen",
                                                        typeof(bool), 
                                                        typeof(RibbonGroup), 
                                                        new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnIsDropDownOpenChanged), new CoerceValueCallback(CoerceIsDropDownOpen)));
                    
        private static void OnIsDropDownOpenChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            RibbonGroup group = (RibbonGroup)sender;

            // If the drop down is closed due to
            // an action of context menu or if the 
            // ContextMenu for a parent  
            // was opened by right clicking this 
            // instance then ContextMenuClosed 
            // event is never raised. 
            // Hence reset the flag.
            group.InContextMenu = false;

            if (group._collapsedGroupPopup != null)
            {
                UIElement popupChild = group._collapsedGroupPopup.TryGetChild();
                RibbonHelper.HandleIsDropDownChanged(group,
                        delegate() { return group.IsDropDownOpen; },
                        popupChild,
                        popupChild);
            }

            if ((bool)(e.NewValue))
            {
                group.RetainFocusOnEscape = RibbonHelper.IsKeyboardMostRecentInputDevice();
            }

            group.CoerceValue(ToolTipProperty);
            RibbonHelper.SetContentAsToolTip(group, group.VisualChild, group.Header, (group.IsCollapsed && !group.IsDropDownOpen));

            RibbonGroupAutomationPeer peer = UIElementAutomationPeer.FromElement(group) as RibbonGroupAutomationPeer;
            if (peer != null)
            {
                peer.RaiseExpandCollapseAutomationEvent((bool)e.OldValue, (bool)e.NewValue);
            }
        }

        private static object CoerceIsDropDownOpen(DependencyObject d, object baseValue)
        {
            RibbonGroup group = (RibbonGroup)d;
            if ((bool)baseValue)
            {
                if (!group.IsLoaded)
                {
                    group.RegisterToOpenOnLoad();
                    return false;
                }

                if (!group.IsVisible)
                {
                    group.RegisterOpenOnVisible();
                    return false;
                }
            }

            if (!group.IsCollapsed)
            {
                return false;
            }
            return baseValue;
        }

        private void RegisterToOpenOnLoad()
        {
            Loaded += new RoutedEventHandler(OpenOnLoad);
        }

        private void OpenOnLoad(object sender, RoutedEventArgs e)
        {
            RibbonHelper.DelayCoerceProperty(this, IsDropDownOpenProperty);
            Loaded -= new RoutedEventHandler(OpenOnLoad);
        }

        private void RegisterOpenOnVisible()
        {
            IsVisibleChanged += new DependencyPropertyChangedEventHandler(HandleIsVisibleChanged);
        }

        void HandleIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            RibbonHelper.DelayCoerceProperty(this, IsDropDownOpenProperty);
            IsVisibleChanged -= new DependencyPropertyChangedEventHandler(HandleIsVisibleChanged);
        }

        internal FrameworkElement CollapsedDropDownButton
        {
            get { return _collapsedDropDownButton; }
        }

        internal ContentPresenter HeaderContentPresenter
        {
            get { return _headerContentPresenter; }
        }

        internal ItemsPresenter ItemsPresenter
        {
            get
            {
                return _itemsPresenter;
            }
        }
        
        #endregion

        #region Protected Methods

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new RibbonGroupAutomationPeer(this);
        }

        /// <summary>
        ///     Generates RibbonControl as container
        /// </summary>
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new RibbonControl();
        }

        /// <summary>
        ///     An item is its own container if it is RibbonControl
        /// </summary>
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return (item is RibbonControl);
        }

        /// <summary>
        ///     Prepare container after generation
        /// </summary>
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);
            PrepareContainerSize(element);
        }

        /// <summary>
        ///     Clear container before detach
        /// </summary>
        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            // We need to make sure we clear ControlSizeDefinition here. This is especially 
            // important when IsCollapsed changes to true to make sure measure is correctly 
            // invalidated for this element's subtree before the descendants get re-inserted into 
            // the new ItemsPresenter in the collapsed template. See Dev10 bug 899738 for more 
            // information.

            element.ClearValue(RibbonControlService.ControlSizeDefinitionProperty);
            base.ClearContainerForItemOverride(element, item);
        }

        /// <summary>
        ///     Gets called when items collection changes.
        /// </summary>
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);
            UpdateGroupSizeDefinitionsAsync();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (IsInQuickAccessToolBar && _sizeDefinitionIndex < 0 && GroupSizeDefinitions.Count > 0)
            {
                _sizeDefinitionIndex = 0;
                ApplyGroupSizeDefinitionBase(GroupSizeDefinitions[0]);
            }
            return base.MeasureOverride(availableSize);
        }
        
        #endregion

        #region Internal Methods

        /// <summary>
        ///     Applies appropriate GroupSizeDefinitionBase whenever
        ///     the container for group gets prepared.
        /// </summary>
        internal void PrepareRibbonGroup()
        {
            UpdateGroupSizeDefinitionsAsync();
            GroupPrepared = true;          
        }

        internal void ClearRibbonGroup()
        {
            GroupPrepared = false;
        }

        /// <summary>
        ///     Called whenever the RibbonGroup should try to increase its size.  Moves the size
        ///     definition index counter to the next largest group size definition and applies it.
        /// </summary>
        /// <returns>Returns true if the resize was successful, false otherwise.</returns>
        internal bool IncreaseGroupSize(bool update)
        {
            if (_sizeDefinitionIndex > 0 && this.GroupSizeDefinitions.Count > 0)
            {
                if (update)
                {
                    ApplyGroupSizeDefinitionBase(this.GroupSizeDefinitions[--_sizeDefinitionIndex]);
                }
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Called whenever the RibbonGroup should try to decrease its size.  Moves the size
        ///     definition index counter to the next smallest group size definition and applies it.
        /// </summary>
        /// <returns>Returns true if the resize was successful, false otherwise.</returns>
        internal bool DecreaseGroupSize()
        {
            if (_sizeDefinitionIndex >= 0 && _sizeDefinitionIndex < this.GroupSizeDefinitions.Count - 1)
            {
                ApplyGroupSizeDefinitionBase(this.GroupSizeDefinitions[++_sizeDefinitionIndex]);
                return true;
            }

            return false;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Constructs the largest group size definition intelligently based on the image sizes
        /// that have been set on the underlying controls.
        /// </summary>
        private RibbonGroupSizeDefinition GetLargeGroupSizeDefinition()
        {
            if (Items.Count == 0)
            {
                return null;
            }

            RibbonGroupSizeDefinition largeGroupSizeDefinition = new RibbonGroupSizeDefinition();

            if (IsCollapsed && _itemsPresenter != null)
            {
                if (_itemsPresenter.ApplyTemplate())
                {
                    // If the group is collapsed then force the container generation if not already
                    // done since the containers are needed for determining default GDSs collection.
                    _itemsPresenter.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                }
            }

            for (int i = 0; i < Items.Count; i++)
            {
                RibbonControl ribbonControl = ItemContainerGenerator.ContainerFromIndex(i) as RibbonControl;
                RibbonControlSizeDefinition controlSizeDefinition = null;
                if (ribbonControl != null)
                {
                    UIElement contentChild = ribbonControl.ContentChild;
                    if (contentChild != null)
                    {
                        controlSizeDefinition = RibbonControlService.GetDefaultControlSizeDefinition(contentChild);
                        if (controlSizeDefinition == null)
                        {
                            contentChild.CoerceValue(RibbonControlService.DefaultControlSizeDefinitionProperty);
                            controlSizeDefinition = RibbonControlService.GetDefaultControlSizeDefinition(contentChild);
                        }
                    }
                }
                if (controlSizeDefinition == null)
                {
                    // While there is no container available assume 
                    // the control is in its largest variant.
                    controlSizeDefinition = new RibbonControlSizeDefinition();
                }

                largeGroupSizeDefinition.ControlSizeDefinitions.Add(controlSizeDefinition);
            }

            return largeGroupSizeDefinition;
        }

        /// <summary>
        /// Constructs a smaller group size definition by decreasing the size of 3 consecutive
        /// controls with the same size, starting the search backwards from repeatStartIndex.
        /// </summary>
        private static RibbonGroupSizeDefinition ReduceGroupSizeDefinition(RibbonGroupSizeDefinition groupSizeDefinition, ref int repeatStartIndex)
        {
            RibbonControlSizeDefinition lastControlSize = groupSizeDefinition.ControlSizeDefinitions[repeatStartIndex];
            int sameSizeCount = 1;

            // Look for 3 consecutive controls with the same size that can be made smaller.

            for (int i = repeatStartIndex - 1; i >= 0; i--)
            {
                RibbonControlSizeDefinition controlSize = groupSizeDefinition.ControlSizeDefinitions[i];

                if (controlSize.ImageSize != RibbonImageSize.Collapsed &&
                    (controlSize.IsLabelVisible || controlSize.ImageSize == RibbonImageSize.Large) &&
                    controlSize.ImageSize == lastControlSize.ImageSize &&
                    controlSize.IsLabelVisible == lastControlSize.IsLabelVisible)
                {
                    if (++sameSizeCount == 3)
                    {
                        repeatStartIndex = i;
                        break;
                    }
                }
                else
                {
                    sameSizeCount = 1;
                }

                lastControlSize = controlSize;
            }

            if (sameSizeCount != 3)
            {
                // We didn't find 3 consecutive controls with the same size, so we're done.
                return null;
            }

            RibbonGroupSizeDefinition reduced = new RibbonGroupSizeDefinition();

            // Add everything before the consecutive 3 controls unchanged.

            for (int i = 0; i < repeatStartIndex; i++)
            {
                RibbonControlSizeDefinition controlSize = (RibbonControlSizeDefinition)groupSizeDefinition.ControlSizeDefinitions[i].Clone();
                reduced.ControlSizeDefinitions.Add(controlSize);
            }

            // Decrease the size of the 3 consecutive controls.

            RibbonControlSizeDefinition repeatedControlSizeDefinition = groupSizeDefinition.ControlSizeDefinitions[repeatStartIndex];
            bool isNewLabelVisible = (repeatedControlSizeDefinition.ImageSize == RibbonImageSize.Large && repeatedControlSizeDefinition.IsLabelVisible);

            for (int i = 0; i < 3; i++)
            {
                RibbonControlSizeDefinition sameSize = new RibbonControlSizeDefinition()
                {
                    ImageSize = RibbonImageSize.Small,
                    IsLabelVisible = isNewLabelVisible,
                };

                reduced.ControlSizeDefinitions.Add(sameSize);
            }

            // Add everything after the consecutive 3 unchanged.

            for (int i = repeatStartIndex + 3; i < groupSizeDefinition.ControlSizeDefinitions.Count; i++)
            {
                RibbonControlSizeDefinition controlSize = (RibbonControlSizeDefinition)groupSizeDefinition.ControlSizeDefinitions[i].Clone();
                reduced.ControlSizeDefinitions.Add(controlSize);
            }

            return reduced;
        }

        /// <summary>
        /// Updates group size definitions asynchronously to give our child controls time to get
        /// inserted into the visual tree. We do this because our group size definition generation
        /// depends on querying properties on these controls.
        /// </summary>
        internal void UpdateGroupSizeDefinitionsAsync()
        {
            if (!GroupSizeUpdatePending)
            {
                GroupSizeUpdatePending = true;
                Dispatcher.BeginInvoke(new Action(UpdateGroupSizeDefinitionsCallback), DispatcherPriority.Loaded);
            }
        }

        private void UpdateGroupSizeDefinitionsCallback()
        {
            CoerceValue(GroupSizeDefinitionsProperty);

            RibbonGroupSizeDefinitionBaseCollection collection = GroupSizeDefinitions;
            if (collection != null && collection.Count > 0 && GroupPrepared)
            {
                if (_sizeDefinitionIndex < 0)
                {
                    _sizeDefinitionIndex = 0;
                }
                if (_sizeDefinitionIndex >= collection.Count)
                {
                    _sizeDefinitionIndex = collection.Count - 1;
                }

                ApplyGroupSizeDefinitionBase(collection[_sizeDefinitionIndex]);
                SetAppropriatePresenterVisibility(GroupSizeDefinitions[_sizeDefinitionIndex] is RibbonGroupSizeDefinition ? Visibility.Visible : Visibility.Collapsed);

                RibbonGroupsPanel panel = TreeHelper.FindVisualAncestor<RibbonGroupsPanel>(this);
                if (panel != null)
                {
                    panel.InvalidateCachedMeasure();
                }
            }

            GroupSizeUpdatePending = false;
        }

        private static void OnHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonGroup ribbonGroup = (RibbonGroup)d;
            RibbonHelper.SetContentAsToolTip(ribbonGroup, ribbonGroup.VisualChild, ribbonGroup.Header, (ribbonGroup.IsCollapsed && !ribbonGroup.IsDropDownOpen));
        }

        /// <summary>
        ///     Coerces the GroupSizeDefinitions DependencyProperty to either use its non-null developer assigned value,
        ///     or the default internal GroupSizeDefinitions property value.
        /// </summary>
        /// <param name="d">The RibbonGroup whose GroupSizeDefinitions property changed.</param>
        /// <param name="baseValue">The new value of the GroupSizeDefinitions property, prior to any coercion attempt.</param>
        /// <returns>The coerced value of the GroupSizeDefinitions property.</returns>
        private static object CoerceGroupSizeDefinitions(DependencyObject d, object baseValue)
        {
            RibbonGroup group = (RibbonGroup)d;
            RibbonGroupSizeDefinitionBaseCollection defaultCollection = 
                group._defaultGroupSizeDefinitionsRef.Target as RibbonGroupSizeDefinitionBaseCollection;
            RibbonGroupSizeDefinitionBaseCollection returnValue = baseValue as RibbonGroupSizeDefinitionBaseCollection;
            if (baseValue == null ||
                ((baseValue == defaultCollection) &&
                 (defaultCollection == null || defaultCollection.Count == 0)))
            {
                if (group.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
                {
                    // When the containers are not generated yet,
                    // return the existing value as is (which might
                    // be old coerced value too), assuming that
                    // the coercion will happen again after
                    // container generation.
                    returnValue = group.GroupSizeDefinitions;
                }
                else
                {
                    returnValue = group.GroupSizeDefinitionsInternal;
                }
            }

            if (returnValue == null)
            {
                if (defaultCollection == null)
                {
                    defaultCollection = new RibbonGroupSizeDefinitionBaseCollection();
                    group._defaultGroupSizeDefinitionsRef = new WeakReference(defaultCollection);
                }
                returnValue = defaultCollection;
            }
            return returnValue;
        }

        /// <summary>
        ///     Prepares a child control by applying appropriate size definition
        /// </summary>
        private void PrepareContainerSize(DependencyObject element)
        {
            int index = ItemContainerGenerator.IndexFromContainer(element);
            RibbonGroupSizeDefinitionBaseCollection groupSizeDefinitions = GroupSizeDefinitions;
            int groupSizeDefinitionsCount = groupSizeDefinitions.Count;
            if (_sizeDefinitionIndex >= 0 && _sizeDefinitionIndex < groupSizeDefinitionsCount)
            {
                RibbonGroupSizeDefinition groupDefinition = groupSizeDefinitions[_sizeDefinitionIndex] as RibbonGroupSizeDefinition;
                if (groupDefinition != null)
                {
                    RibbonControlSizeDefinition controlSizeDefinition = null;
                    RibbonControlSizeDefinitionCollection controlSizeDefinitions = groupDefinition.ControlSizeDefinitions;
                    if (controlSizeDefinitions != null &&
                        index < controlSizeDefinitions.Count)
                    {
                        controlSizeDefinition = controlSizeDefinitions[index];
                    }

                    // If the group is collapsed, then search for an
                    // appropriate control size definition
                    if (IsCollapsed &&
                        (controlSizeDefinitions == null || controlSizeDefinitions.Count == 0))
                    {
                        RibbonControlSizeDefinitionCollection targetControlSizeDefinitions = GetControlDefinitionsForCollapsedGroup(groupDefinition);
                        if (targetControlSizeDefinitions != null && index < targetControlSizeDefinitions.Count)
                        {
                            controlSizeDefinition = targetControlSizeDefinitions[index];
                        }
                    }
                    if (controlSizeDefinition != null)
                    {
                        RibbonControlService.SetControlSizeDefinition(element, controlSizeDefinition);
                    }
                    else
                    {
                        element.ClearValue(RibbonControlService.ControlSizeDefinitionProperty);
                    }
                }
            }
        }

        /// <summary>
        ///     Applies the RibbonGroupSizeDefinitionBase.
        /// </summary>
        private void ApplyGroupSizeDefinitionBase(RibbonGroupSizeDefinitionBase definition)
        {
            if (definition == null)
            {
                definition = _defaultGroupSizeDefinition;
            }

            bool remeasure = false;
            if (definition.IsCollapsed != IsCollapsed)
            {
                IsCollapsed = definition.IsCollapsed;
                remeasure = true;
            }

            RibbonGroupSizeDefinition groupSizeDefinition = definition as RibbonGroupSizeDefinition;
            if (groupSizeDefinition != null)
            {
                // Apply RibbonGroupSizeDefinition
                if (SetAppropriatePresenterVisibility(Visibility.Visible))
                {
                    TreeHelper.InvalidateMeasureForVisualAncestorPath<RibbonGroup>(_itemsPresenter);
                    remeasure = true;
                }
                if (remeasure)
                {
                    Measure(DesiredSize);
                }
                ApplyGroupSizeDefinition(groupSizeDefinition);
            }
            else
            {
                RibbonGroupTemplateSizeDefinition groupTemplateSizeDefinition = definition as RibbonGroupTemplateSizeDefinition;
                if (groupTemplateSizeDefinition != null)
                {
                    // Apply RibbonGroupTemplateSizeDefinition
                    SetAppropriatePresenterVisibility(Visibility.Collapsed);
                    if (remeasure)
                    {
                        Measure(DesiredSize);
                    }
                    ApplyGroupTemplateSizeDefinition(groupTemplateSizeDefinition);
                }
            }
        }

        private bool SetAppropriatePresenterVisibility(Visibility itemsPresenterVisibility)
        {
            bool remeasure = false;
            if (_itemsPresenter != null && _itemsPresenter.Visibility != itemsPresenterVisibility)
            {
                _itemsPresenter.Visibility = itemsPresenterVisibility;
                if (itemsPresenterVisibility == Visibility.Visible)
                {
                    remeasure = true;
                }
            }

            if (_templateContentControl != null)
            {
                _templateContentControl.Visibility = (itemsPresenterVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible);
            }
            return remeasure;
        }

        /// <summary>
        ///     Helper method which searches for
        ///     appropriate control size definition collection
        ///     in case of collapsed group.
        /// </summary>
        private RibbonControlSizeDefinitionCollection GetControlDefinitionsForCollapsedGroup(RibbonGroupSizeDefinition groupSizeDefinition)
        {
            Debug.Assert(groupSizeDefinition != null && groupSizeDefinition.IsCollapsed);
            RibbonGroupSizeDefinitionBaseCollection groupSizeDefinitions = GroupSizeDefinitions;
            int groupSizeDefCount = groupSizeDefinitions.Count;
            for (int i = 0; i < groupSizeDefCount; i++)
            {
                RibbonGroupSizeDefinitionBase currentGroupSizeDefinitionBase = groupSizeDefinitions[i];
                if (currentGroupSizeDefinitionBase == groupSizeDefinition)
                {
                    return null;
                }
                RibbonGroupSizeDefinition currentGroupSizeDefinition = currentGroupSizeDefinitionBase as RibbonGroupSizeDefinition;
                if (currentGroupSizeDefinition != null)
                {
                    RibbonControlSizeDefinitionCollection currentControlSizeDefinitions = currentGroupSizeDefinition.ControlSizeDefinitions;
                    if (currentControlSizeDefinitions != null &&
                        currentControlSizeDefinitions.Count > 0)
                    {
                        return currentGroupSizeDefinition.ControlSizeDefinitions;
                    }
                }
            }
            return null;
        }

        /// <summary>
        ///     Logic to apply ControlSizeDefinitions from RibbonGroupSizeDefinition
        /// </summary>
        private void ApplyGroupSizeDefinition(RibbonGroupSizeDefinition groupSizeDefinition)
        {
            RibbonControlSizeDefinitionCollection controlSizeDefinitions = groupSizeDefinition.ControlSizeDefinitions;
            if (IsCollapsed && (controlSizeDefinitions == null || controlSizeDefinitions.Count == 0))
            {
                controlSizeDefinitions = GetControlDefinitionsForCollapsedGroup(groupSizeDefinition);
            }
            int numDefinedSizes = 0;
            if (controlSizeDefinitions != null)
            {
                numDefinedSizes = controlSizeDefinitions.Count;
            }

            int itemCount = Items.Count;
            for (int i = 0; i < itemCount; i++)
            {
                DependencyObject d = ItemContainerGenerator.ContainerFromIndex(i);
                if (d != null)
                {
                    if (i < numDefinedSizes)
                    {
                        RibbonControlSizeDefinition def = controlSizeDefinitions[i];
                        RibbonControlService.SetControlSizeDefinition(d, def);
                    }
                    else
                    {
                        d.ClearValue(RibbonControlService.ControlSizeDefinitionProperty);
                    }
                    TreeHelper.InvalidateMeasureForVisualAncestorPath<RibbonGroup>(d);
                }
            }
        }

        /// <summary>
        ///     Logic to apply RibbonGroupTemplateSizeDefinition.
        /// </summary>
        /// <param name="grouptemplateSizeDefinition"></param>
        private void ApplyGroupTemplateSizeDefinition(RibbonGroupTemplateSizeDefinition grouptemplateSizeDefinition)
        {
            DataTemplate contentTemplate = grouptemplateSizeDefinition.ContentTemplate;
            if (IsCollapsed && contentTemplate == null)
            {
                RibbonGroupSizeDefinitionBaseCollection groupSizeDefinitions = GroupSizeDefinitions;
                int groupSizeDefCount = groupSizeDefinitions.Count;
                for (int i = 0; i < groupSizeDefCount; i++)
                {
                    RibbonGroupSizeDefinitionBase currentGroupSizeDefinitionBase = groupSizeDefinitions[i];
                    if (currentGroupSizeDefinitionBase == grouptemplateSizeDefinition)
                    {
                        contentTemplate = null;
                        break;
                    }
                    RibbonGroupTemplateSizeDefinition currentGroupSizeDefinition = currentGroupSizeDefinitionBase as RibbonGroupTemplateSizeDefinition;
                    if (currentGroupSizeDefinition != null && currentGroupSizeDefinition.ContentTemplate != null)
                    {
                        contentTemplate = currentGroupSizeDefinition.ContentTemplate;
                        break;
                    }
                }
            }

            if (contentTemplate != null && _templateContentControl != null)
            {
                _templateContentControl.ContentTemplate = contentTemplate;
                if (!IsCollapsed)
                {
                    _templateContentControl.Measure(_templateContentControl.DesiredSize);
                    RibbonHelper.FixMeasureInvalidationPaths(_templateContentControl);
                    TreeHelper.InvalidateMeasureForVisualAncestorPath<RibbonGroup>(_templateContentControl);
                }
            }
        }

        /// <summary>
        ///     property changed callback to update ownership and event handlers
        ///     when GroupSizeDefinitions change.
        /// </summary>
        private static void OnGroupSizeDefinitionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonGroup ribbonGroup = (RibbonGroup)d;
            RibbonGroupSizeDefinitionBaseCollection collection = ribbonGroup.GroupSizeDefinitions;
            if (collection != null)
            {
                if (ribbonGroup._sizeDefinitionIndex >= collection.Count)
                {
                    ribbonGroup._sizeDefinitionIndex = collection.Count - 1;
                }
                if (ribbonGroup.GroupPrepared && ribbonGroup._sizeDefinitionIndex >= 0)
                {
                    ribbonGroup.ApplyGroupSizeDefinitionBase(collection[ribbonGroup._sizeDefinitionIndex]);
                }
            }
        }

        private static void OnIsInQuickAccessToolBarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(IsCollapsedProperty);
        }

        private static object CoerceToolTip(DependencyObject d, object value)
        {
            RibbonGroup ribbonGroup = (RibbonGroup)d;
            if (value == null && ribbonGroup.IsCollapsed && !ribbonGroup.IsDropDownOpen)
            {
                return RibbonHelper.CoerceRibbonToolTip(d, value);
            }
            return value;
        }

        // Instantiates a DoubleAnimation for showing/hiding the glow effect.
        // This template part toggles a glow effect on the Ribbon group when mouse enters/leaves.
        private static DoubleAnimation CreateOpacityAnimation(bool shouldTurnOn, DependencyObject target)
        {
            DoubleAnimation opacityAnimation;
            if (shouldTurnOn)
            {
                TimeSpan twoTenthsOfASeconds = new TimeSpan(0, 0, 0, 0, 200);
                opacityAnimation = new DoubleAnimation(1, new Duration(twoTenthsOfASeconds));
            }
            else
            {
                TimeSpan fourTenthsOfASeconds = new TimeSpan(0, 0, 0, 0, 400);
                opacityAnimation = new DoubleAnimation(0, new Duration(fourTenthsOfASeconds));
            }

            opacityAnimation.SetValue(Storyboard.TargetProperty, target);
            opacityAnimation.SetValue(Storyboard.TargetPropertyProperty, new PropertyPath("Opacity"));

            return opacityAnimation;
        }

        #endregion

        #region Private Data

        private enum Bits
        {
            GroupPrepared = 0x01,
            RetainFocusOnEscape = 0x02,
            GroupSizeUpdatePending = 0x04,
            InContextMenu = 0x08
        }

        private bool GroupPrepared
        {
            get { return _bits[(int)Bits.GroupPrepared]; }
            set { _bits[(int)Bits.GroupPrepared] = value; }
        }

        private bool RetainFocusOnEscape
        {
            get { return _bits[(int)Bits.RetainFocusOnEscape]; }
            set { _bits[(int)Bits.RetainFocusOnEscape] = value; }
        }

        private bool GroupSizeUpdatePending
        {
            get { return _bits[(int)Bits.GroupSizeUpdatePending]; }
            set { _bits[(int)Bits.GroupSizeUpdatePending] = value; }
        }

        private bool InContextMenu
        {
            get { return _bits[(int)Bits.InContextMenu]; }
            set { _bits[(int)Bits.InContextMenu] = value; }
        }

        private int _sizeDefinitionIndex = -1; // The current position of the RibbonGroup ControlSizeDefinition index.
        private FrameworkElement _collapsedDropDownButton; // The SplitButton that replaces RibbonGroup's normal appearance when Collapsed
        private Popup _collapsedGroupPopup;
        private Border _hotBackgroundBorder;  // Used in an animation to give a highlighting effect when mouse-entering a RibbonGroup.
        private ContentPresenter _headerContentPresenter;   // Header
        private ContentControl _templateContentControl;
        private ItemsPresenter _itemsPresenter;
        private WeakReference _defaultGroupSizeDefinitionsRef;
        private static RibbonGroupSizeDefinition _defaultGroupSizeDefinition = new RibbonGroupSizeDefinition();
        private BitVector32 _bits = new BitVector32(0);
        private Storyboard _mouseEnterStoryboard;
        private Storyboard _mouseLeaveStoryboard;

        private const double KeyTipVerticalOffsetDelta = 2;
        private const string CollapsedDropDownButtonTemplatePartName = "PART_ToggleButton";
        private const string HeaderContentPresenterTemplatePartName = "PART_Header";
        private const string HotBackgroundBorderTemplatePartName = "PART_HotBackground";
        private const string ItemsPresenterTemplatePartName = "ItemsPresenter";
        private const string PopupGridTemplatePartName = "PART_PopupGrid";
        private const string PopupTemplatePartName = "PART_Popup";
        private const string TemplateContentControlTemplatePartName = "PART_TemplateContentControl";

        #endregion

        #region DismissPopup

        private static void OnLostMouseCaptureThunk(object sender, MouseEventArgs e)
        {
            RibbonGroup group = (RibbonGroup)sender;
            group.OnLostMouseCaptureThunk(e);
        }

        private void OnLostMouseCaptureThunk(MouseEventArgs e)
        {
            UIElement popupChild = _collapsedGroupPopup.TryGetChild();
            RibbonHelper.HandleLostMouseCapture(this,
                    e,
                    delegate() { return (IsDropDownOpen && !InContextMenu); },
                    delegate(bool value) { IsDropDownOpen = value; },
                    popupChild,
                    popupChild);
        }

        private static void OnClickThroughThunk(object sender, MouseButtonEventArgs e)
        {
            RibbonGroup ribbonGroup = (RibbonGroup)sender;
            ribbonGroup.OnClickThrough(e);
        }

        private void OnClickThrough(MouseButtonEventArgs e)
        {
            RibbonHelper.HandleClickThrough(this, e, _collapsedGroupPopup.TryGetChild());
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            if (IsDropDownOpen)
            {
                // Close the drop down if the click happened on the toggle button.
                if (RibbonHelper.IsMousePhysicallyOver(_collapsedDropDownButton))
                {
                    IsDropDownOpen = false;
                    e.Handled = true;
                }
            }
        }

        private static void OnDismissPopupThunk(object sender, RibbonDismissPopupEventArgs e)
        {
            RibbonGroup ribbonGroup = (RibbonGroup)sender;
            ribbonGroup.OnDismissPopup(e);
        }

        private void OnDismissPopup(RibbonDismissPopupEventArgs e)
        {
            UIElement popupChild = _collapsedGroupPopup.TryGetChild();
            RibbonHelper.HandleDismissPopup(e, 
                delegate(bool value) { IsDropDownOpen = value; }, 
                delegate(DependencyObject d) { return d  == _collapsedDropDownButton; },
                popupChild,
                this);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!e.Handled && IsCollapsed)
            {
                RibbonHelper.HandleDropDownKeyDown(this, e,
                    delegate { return IsDropDownOpen; },
                    delegate(bool value) { IsDropDownOpen = value; },
                    RetainFocusOnEscape ? _collapsedDropDownButton : null,
                    _itemsPresenter);
            }
        }

        private static void OnMouseDownThunk(object sender, MouseButtonEventArgs e)
        {
            ((RibbonGroup)(sender)).OnAnyMouseDown();
        }

        private void OnAnyMouseDown()
        {
            RetainFocusOnEscape = false;
        }

        #endregion DismissPopup

        #region Context Menu

        private static void OnContextMenuOpeningThunk(object sender, ContextMenuEventArgs e)
        {
            ((RibbonGroup)sender).OnContextMenuOpeningInternal();
        }

        private void OnContextMenuOpeningInternal()
        {
            InContextMenu = true;
        }

        private static void OnContextMenuClosingThunk(object sender, ContextMenuEventArgs e)
        {
            ((RibbonGroup)sender).OnContextMenuClosingInternal();
        }

        private void OnContextMenuClosingInternal()
        {
            InContextMenu = false;
            if (IsDropDownOpen)
            {
                UIElement popupChild = _collapsedGroupPopup.TryGetChild();
                RibbonHelper.AsyncSetFocusAndCapture(this,
                    delegate() { return IsDropDownOpen; },
                    popupChild,
                    popupChild);
            }
        }

        #endregion

        #region QAT

        /// <summary>
        ///   DependencyProperty for QuickAccessToolBarId property.
        /// </summary>
        public static readonly DependencyProperty QuickAccessToolBarIdProperty =
            RibbonControlService.QuickAccessToolBarIdProperty.AddOwner(typeof(RibbonGroup));

        /// <summary>
        ///   This property is used as a unique identifier to link a control in the Ribbon with its counterpart in the QAT.
        /// </summary>
        public object QuickAccessToolBarId
        {
            get { return RibbonControlService.GetQuickAccessToolBarId(this); }
            set { RibbonControlService.SetQuickAccessToolBarId(this, value); }
        }

        /// <summary>
        ///   DependencyProperty for CanAddToQuickAccessToolBarDirectly property.
        /// </summary>
        public static readonly DependencyProperty CanAddToQuickAccessToolBarDirectlyProperty =
            RibbonControlService.CanAddToQuickAccessToolBarDirectlyProperty.AddOwner(typeof(RibbonGroup),
            new FrameworkPropertyMetadata(true));


        /// <summary>
        ///   Property determining whether a control can be added to the RibbonQuickAccessToolBar directly.
        /// </summary>
        public bool CanAddToQuickAccessToolBarDirectly
        {
            get { return RibbonControlService.GetCanAddToQuickAccessToolBarDirectly(this); }
            set { RibbonControlService.SetCanAddToQuickAccessToolBarDirectly(this, value); }
        }

        #endregion QAT

        #region Custom KeyTip Siblings

        private class RibbonGroupCustomKeyTipSiblings : IEnumerable<DependencyObject>
        {
            #region Constructor and Properties

            public RibbonGroupCustomKeyTipSiblings(RibbonGroup group)
            {
                RibbonGroup = group;
            }

            RibbonGroup RibbonGroup
            {
                get;
                set;
            }

            #endregion

            #region IEnumerable<DependencyObject> Members

            public IEnumerator<DependencyObject> GetEnumerator()
            {
                if (!RibbonGroup.IsInQuickAccessToolBar)
                {
                    // Return all the items in group which have Keytip
                    // including those which are inside a ControlGroup.
                    // This should not be done when in QAT.
                    foreach (object item in RibbonGroup.Items)
                    {
                        DependencyObject element = item as DependencyObject;
                        if (element != null)
                        {
                            if (!string.IsNullOrEmpty(KeyTipService.GetKeyTip(element)))
                            {
                                yield return element;
                            }

                            RibbonControlGroup controlGroup = element as RibbonControlGroup;
                            if (controlGroup != null &&
                                !KeyTipService.GetIsKeyTipScope(controlGroup))
                            {
                                foreach (object controlGroupItem in controlGroup.Items)
                                {
                                    DependencyObject controlGroupElement = controlGroupItem as DependencyObject;
                                    if (controlGroupItem != null &&
                                        !string.IsNullOrEmpty(KeyTipService.GetKeyTip(controlGroupElement)))
                                    {
                                        yield return controlGroupElement;
                                    }
                                }
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

        #region KeyTips

        /// <summary>
        ///     DependencyProperty for KeyTip property.
        /// </summary>
        public static readonly DependencyProperty KeyTipProperty =
            KeyTipService.KeyTipProperty.AddOwner(typeof(RibbonGroup));

        /// <summary>
        ///     KeyTip string for the control.
        /// </summary>
        public string KeyTip
        {
            get { return KeyTipService.GetKeyTip(this); }
            set { KeyTipService.SetKeyTip(this, value); }
        }

        private static void OnActivatingKeyTipThunk(object sender, ActivatingKeyTipEventArgs e)
        {
            ((RibbonGroup)sender).OnActivatingKeyTip(e);
        }

        protected virtual void OnActivatingKeyTip(ActivatingKeyTipEventArgs e)
        {
            if (e.OriginalSource == this)
            {
                if (!IsCollapsed)
                {
                    // KeyTip should be hidden when not collapsed
                    e.KeyTipVisibility = Visibility.Hidden;
                }
                else
                {
                    if (IsInQuickAccessToolBar)
                    {
                        e.KeyTipHorizontalPlacement = KeyTipHorizontalPlacement.KeyTipCenterAtTargetCenter;
                        e.KeyTipVerticalPlacement = KeyTipVerticalPlacement.KeyTipTopAtTargetCenter;
                        e.KeyTipHorizontalOffset = e.KeyTipVerticalOffset = 0;
                    }
                    else
                    {
                        Ribbon ribbon = Ribbon;
                        if (ribbon != null)
                        {
                            if (ribbon.IsMinimized)
                            {
                                SetMinimizedRibbonKeyTipPlacement(ribbon, e);
                            }
                            else
                            {
                                SetUnminimizedRibbonKeyTipPlacement(ribbon, e);
                            }
                        }
                    }
                }
            }
            else if (_itemsPresenter != null)
            {
                UIElement placementTarget = e.PlacementTarget;
                if (placementTarget == null)
                {
                    placementTarget = RibbonHelper.GetContainingUIElement(e.OriginalSource as DependencyObject);
                }
                if (placementTarget != null &&
                    TreeHelper.IsVisualAncestorOf(_itemsPresenter, placementTarget))
                {
                    // For all the visual descendant set this property,
                    // so that they can be nudged to top/bottom axis if
                    // needed.
                    e.OwnerRibbonGroup = this;
                }
            }
        }

        private void SetUnminimizedRibbonKeyTipPlacement(Ribbon ribbon, ActivatingKeyTipEventArgs e)
        {
            GeneralTransform groupToRibbon = TransformToAncestor(ribbon);
            if (groupToRibbon != null)
            {
                Point groupOrigin = groupToRibbon.Transform(new Point());
                double horizontalOffset = groupOrigin.X + (ActualWidth / 2);
                if (DoubleUtil.GreaterThanOrClose(horizontalOffset, 0) &&
                    DoubleUtil.LessThanOrClose(horizontalOffset, ribbon.ActualWidth))
                {
                    e.KeyTipHorizontalPlacement = KeyTipHorizontalPlacement.KeyTipCenterAtTargetLeft;
                    e.KeyTipVerticalPlacement = KeyTipVerticalPlacement.KeyTipTopAtTargetTop;
                    e.KeyTipHorizontalOffset = horizontalOffset;
                    e.KeyTipVerticalOffset = groupOrigin.Y + ActualHeight + KeyTipVerticalOffsetDelta;
                    e.PlacementTarget = ribbon;
                }
                else
                {
                    e.KeyTipVisibility = Visibility.Hidden;
                }
            }
            else
            {
                e.KeyTipVisibility = Visibility.Hidden;
            }
        }

        private void SetMinimizedRibbonKeyTipPlacement(Ribbon ribbon, ActivatingKeyTipEventArgs e)
        {
            UIElement ribbonPopupChild = ribbon.ItemsPresenterPopup.TryGetChild();
            if (ribbonPopupChild != null)
            {
                Point popupChildOrigin = ribbon.PointFromScreen(ribbonPopupChild.PointToScreen(new Point()));
                GeneralTransform groupToPopup = TransformToAncestor(ribbonPopupChild);
                if (groupToPopup != null)
                {
                    double horizontalOffset = groupToPopup.Transform(new Point()).X + (ActualWidth / 2);
                    if (DoubleUtil.GreaterThanOrClose(horizontalOffset, 0) &&
                        DoubleUtil.LessThanOrClose(horizontalOffset, ribbonPopupChild.RenderSize.Width))
                    {
                        e.KeyTipHorizontalPlacement = KeyTipHorizontalPlacement.KeyTipCenterAtTargetLeft;
                        e.KeyTipVerticalPlacement = KeyTipVerticalPlacement.KeyTipTopAtTargetTop;
                        e.KeyTipHorizontalOffset = horizontalOffset + popupChildOrigin.X;
                        e.KeyTipVerticalOffset = ribbonPopupChild.RenderSize.Height + popupChildOrigin.Y + KeyTipVerticalOffsetDelta;
                        e.PlacementTarget = ribbon;
                    }
                    else
                    {
                        e.KeyTipVisibility = Visibility.Hidden;
                    }
                }
                else
                {
                    e.KeyTipVisibility = Visibility.Hidden;
                }
            }
        }

        private static void OnKeyTipAccessedThunk(object sender, KeyTipAccessedEventArgs e)
        {
            ((RibbonGroup)sender).OnKeyTipAccessed(e);
        }

        protected virtual void OnKeyTipAccessed(KeyTipAccessedEventArgs e)
        {
            if (e.OriginalSource == this)
            {
                if (IsCollapsed)
                {
                    // Open the dropdown.
                    IsDropDownOpen = true;
                    UIElement popupChild = _collapsedGroupPopup.TryGetChild();
                    if (popupChild != null)
                    {
                        KeyTipService.SetIsKeyTipScope(popupChild, true);
                        e.TargetKeyTipScope = popupChild;
                    }
                }
                else
                {
                    RibbonTab tab = ItemsControl.ItemsControlFromItemContainer(this) as RibbonTab;
                    if (tab != null &&
                        KeyTipService.GetIsKeyTipScope(tab))
                    {
                        e.TargetKeyTipScope = tab;
                    }
                }
                e.Handled = true;
            }
        }

        #endregion
    }
}
