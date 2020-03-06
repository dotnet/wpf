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
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Automation;
    using System.Windows.Automation.Peers;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Media;
    using System.Windows.Input;
    using System.Windows.Threading;
    using MS.Internal;
#if RIBBON_IN_FRAMEWORK
    using Microsoft.Windows.Controls;
    using System.Windows.Controls.Ribbon.Primitives;
#else
    using Microsoft.Windows.Automation.Peers;
    using Microsoft.Windows.Controls.Ribbon.Primitives;
#endif

    #endregion

    /// <summary>
    ///   InRibbonGallery is an ItemsControl which displays a primary RibbonGallery and potentially other items as well.
    ///   It is similar to RibbonMenuButton, and uses RibbonMenuItemsPanel to lay out its items.
    ///
    ///   The first RibbonGallery found in its Items collection is the primary RibbonGallery, which will be
    ///   shown inside the Ribbon for in-Ribbon Gallery mode.  While in drop-down mode, the primary RibbonGallery
    ///   and all other items are shown in a drop-down.
    ///
    ///   Its additional Items may contain RibbonMenuItems, RibbonGalleries, and RibbonSeparators. 
    /// </summary>
    [TemplatePart(Name = ItemsPresenterTemplatePartName, Type = typeof(ItemsPresenter))]
    [TemplatePart(Name = ContentPresenterTemplatePartName, Type = typeof(ContentPresenter))]
    [TemplatePart(Name = ScrollButtonsBorderTemplatePartName, Type = typeof(Border))]
    [TemplatePart(Name = ScrollUpRepeatButtonTemplatePartName, Type = typeof(RepeatButton))]
    [TemplatePart(Name = ScrollDownRepeatButtonTemplatePartName, Type = typeof(RepeatButton))]
    public class InRibbonGallery : RibbonMenuButton
    {
        #region Constructors

        static InRibbonGallery()
        {
            Type ownerType = typeof(InRibbonGallery);
            DefaultStyleKeyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(ownerType));
            IsDropDownOpenProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(new PropertyChangedCallback(OnIsDropDownOpenChanged)));
            ControlSizeDefinitionProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(new PropertyChangedCallback(OnControlSizeDefinitionChanged)));
        }

        public InRibbonGallery() : base()
        {
            // TODO: Remove this property completely and calculate it from the template.
            WidthStylePadding = 2;
        }

        #endregion

        #region Keyboard Navigation
        
        /// <summary>
        ///     When IsInInRibbonGalleryMode and PartToggleButton is focused, we need to do a little extra work
        ///     to prevent circular keyboard navigation when a Tab key or arrow key is pressed.  The default handling of
        ///     OnKeyDown may try to give keyboard focus to one of the RibbonGalleryItems, which will cause a loop with
        ///     focus remaining on PartToggleButton.  Here we avoid such conditions and navigate out of the InRibbonGallery
        ///     to the next/previous element as appropriate.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (!e.Handled &&
                IsInInRibbonMode)
            {
                Debug.Assert(Keyboard.FocusedElement == PartToggleButton);

                if (e.Key == Key.Tab)
                {
                    bool shiftPressed = ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift);

                    // Navigate forward if shift is not pressed.
                    FocusNavigationDirection direction = shiftPressed ? DirectionOfPreviousElement : DirectionOfNextElement;

                    e.Handled = TryNavigateToNeighboringElement(e, direction);
                }
                else if (e.Key == Key.Left || e.Key == Key.Right)
                {
                    // Navigate forward if arrow direction matches flow direction.
                    if ((e.Key == Key.Right) == (FlowDirection == FlowDirection.LeftToRight))
                    {
                        e.Handled = TryNavigateToNeighboringElement(e, DirectionOfNextElement);
                    }
                    else
                    {
                        e.Handled = TryNavigateToNeighboringElement(e, DirectionOfPreviousElement);
                    }
                }
            }
        }

        private bool TryNavigateToNeighboringElement(KeyEventArgs e, FocusNavigationDirection direction)
        {
            UIElement neighboringElement = this.PredictFocus(direction) as UIElement;
            if (neighboringElement != null)
            {
                Keyboard.Focus(neighboringElement);
                return neighboringElement.IsKeyboardFocusWithin;
            }

            return false;
        }

        private FocusNavigationDirection DirectionOfNextElement
        {
            get
            {
                return FlowDirection == FlowDirection.LeftToRight ? FocusNavigationDirection.Right : FocusNavigationDirection.Left;
            }
        }

        private FocusNavigationDirection DirectionOfPreviousElement
        {
            get
            {
                return FlowDirection == FlowDirection.LeftToRight ? FocusNavigationDirection.Left : FocusNavigationDirection.Right;
            }
        }

        #endregion

        #region Protected Methods

        protected override Size MeasureOverride(Size constraint)
        {
            if (!IsCollapsed && IsDropDownOpen)
            {
                return _cachedDesiredSize;
            }

            Size desiredSize = base.MeasureOverride(constraint);
            if (IsInInRibbonMode)
            {
                _cachedDesiredSize = desiredSize;

                if (DoubleUtil.AreClose(_scrollButtonsBorderFactor, 0) || 
                    (_contentPresenter != null && DoubleUtil.AreClose(_contentPresenter.MaxWidth, 0)))
                {
                    UpdateInRibbonGalleryModeProperties();
                }
            }

            return desiredSize;
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);
            UpdateFirstGallery();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            PrivateManageRibbonGalleryVisualParent();
        }

        /// <summary>
        /// Cache container reference and RibbonGallery for first gallery.
        /// </summary>
        private void UpdateFirstGallery()
        {
            _firstGalleryItem = null;
            _firstGallery = null;
            CommandTarget = null;

            foreach (object item in Items)
            {
                RibbonGallery firstGallery = ItemContainerGenerator.ContainerFromItem(item) as RibbonGallery;
                if (firstGallery != null)
                {
                    _firstGalleryItem = new WeakReference(item);
                    FirstGallery = firstGallery;
                    CommandTarget = firstGallery.ScrollViewer;
                    return;
                }
            }
        }

        protected override void PrepareContainerForItemOverride(DependencyObject container, object item)
        {
            // If a new Gallery container has been generated for _galleryItem, update _gallery reference.
            RibbonGallery gallery = container as RibbonGallery;
            if (gallery != null)
            {
                if (_firstGalleryItem != null && _firstGalleryItem.IsAlive && _firstGalleryItem.Target.Equals(item))
                {
                    FirstGallery = gallery;
                }
            }

            base.PrepareContainerForItemOverride(container, item);
        }

        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            if (_firstGalleryItem != null && _firstGalleryItem.IsAlive && _firstGalleryItem.Target.Equals(item))
            {
                FirstGallery = null;
            }
            base.ClearContainerForItemOverride(element, item);
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new InRibbonGalleryAutomationPeer(this);
        }

        #endregion

        #region Properties

        /// <summary>
        /// WidthStylePadding is the padding used to specify extra bodrer thicknesses/margins/paddings in direction of width
        /// apart from the main content (Items) in _firstGallery. ControlSizeDefinition of InRibbonGallery does corresponds to
        /// only the main content. Hence this prperty becomes useful in smooth calculations. By Default it is equal to what is 
        /// used in default Layout.
        /// </summary>
        public double WidthStylePadding
        {
            get;
            set;
        }

        /// <summary>
        ///     Indicates whether this InRibbonGallery is collapsed or expanded.
        /// </summary>
        public static readonly DependencyProperty IsCollapsedProperty =
                        DependencyProperty.Register(
                                                "IsCollapsed",
                                                typeof(bool),
                                                typeof(InRibbonGallery),
                                                new FrameworkPropertyMetadata(
                                                                false,
                                                                new PropertyChangedCallback(OnIsCollapsedChanged),
                                                                new CoerceValueCallback(CoerceIsCollapsed)));

        /// <summary>
        ///     Indicates whether this InRibbonGallery is collapsed or expanded. Default value is false which corresponds to expanded.
        /// </summary>
        public bool IsCollapsed
        {
            get { return (bool)GetValue(IsCollapsedProperty); }
            set { SetValue(IsCollapsedProperty, value); }
        }

        // When IsCollapsed changes the InRibbonGallery would be able to acheive IN Ribbon mode and hence visual
        // properties needs to be coerced.
        private static void OnIsCollapsedChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            InRibbonGallery irg = (InRibbonGallery)d;
            irg.PrivateManageRibbonGalleryVisualParent();
        }

        private static object CoerceIsCollapsed(DependencyObject d, object baseValue)
        {
            InRibbonGallery irg = (InRibbonGallery)d;
            if (DependencyPropertyHelper.GetValueSource(irg, IsCollapsedProperty).BaseValueSource != BaseValueSource.Local)
            {
                RibbonControlSizeDefinition csd = irg.ControlSizeDefinition;
                if (csd != null)
                {
                    return csd.IsCollapsed;
                }
            }

            return baseValue;
        }

        // When IsDropDownOpen changes the InRibbonGallery would be able to acheive IN Ribbon mode and hence visual
        // properties needs to be coerced.
        private static void OnIsDropDownOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            InRibbonGallery irg = (InRibbonGallery)d;
            irg.PrivateManageRibbonGalleryVisualParent();

            if ((bool)args.NewValue == false &&
                irg.FilterHasChanged)
            {
                irg.FilterHasChanged = false;
                irg.InvalidateMeasure();
            }

            if ((bool)args.NewValue == false &&
                irg.FirstGallery != null)
            {
                irg.FirstGallery.ChangeHighlight(null, null, false);
            }
        }

        // Updates Width and other layout properties in the Visual tree from RibbonControl to InRibbonGallery wherever needed
        // when attains In Ribbon mode.
        private static void OnControlSizeDefinitionChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            InRibbonGallery me = (InRibbonGallery)d;
            me.CoerceValue(IsCollapsedProperty);
            if (me.IsInInRibbonMode)
            {
                if (me._ribbonControl == null)
                {
                    me._ribbonControl = TreeHelper.FindAncestor(me, delegate(DependencyObject element)
                    {
                        RibbonControl rc = (element as RibbonControl);
                        if (rc != null)
                        {
                            return me == rc.ContentChild;
                        }

                        return false;
                    }) as RibbonControl;
                }
            }

            me.UpdateInRibbonGalleryModeProperties();
        }

        // Used to bind the contained RibbonGaller's ScrollViewer to InRibbonGallery's scroll Buttons.
        internal ScrollViewer CommandTarget
        {
            get { return (ScrollViewer)GetValue(CommandTargetProperty); }
            set { SetValue(CommandTargetProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CommandTarget.  This enables animation, styling, binding, etc... 
        internal static readonly DependencyProperty CommandTargetProperty =
                DependencyProperty.Register("CommandTarget", typeof(ScrollViewer), typeof(InRibbonGallery));

        #endregion

        #region Private Helpers

        // Converts RibbonControlLength when specified in terms of Items (items of Contained _firstGallery) to
        // actual pixel value by using Item's width in pixels.
        private double GetPixelValueFromRibbonControlLength(RibbonControlLength cl)
        {
            if (_firstGallery != null)
            {
                double itemWidth = _firstGallery.MaxColumnWidth;
                if (cl.RibbonControlLengthUnitType == RibbonControlLengthUnitType.Item)
                {
                    return cl.Value * itemWidth;
                }
            }
            if (cl.IsAuto || cl.IsStar)
                return Double.NaN;
            return cl.Value;
        }

        // Updates Width and other layout properties in the Visual tree from RibbonControl to InRibbonGallery wherever needed
        // when attains In Ribbon mode.
        private void UpdateInRibbonGalleryModeProperties()
        {
            if (IsInInRibbonMode && ControlSizeDefinition != null)
            {
                _scrollButtonsBorderFactor = 0;
                if (_scrollButtonsBorder != null)
                {
                    if (DoubleUtil.AreClose(_scrollButtonsBorder.ActualWidth, 0))
                    {
                        _scrollButtonsBorderFactor = 0;
                    }
                    else
                    {
                        _scrollButtonsBorderFactor = Math.Max(_scrollButtonsBorder.ActualWidth, 0);
                    }
                }

                if (_contentPresenter != null)
                {
                    // Converts if the Width property values specified in terms of items and assigns it to 
                    // InRibbonGallery Content Template parts from ControlSizeDefinition
                    double minWidth = GetPixelValueFromRibbonControlLength(ControlSizeDefinition.MinWidth);
                    if (!DoubleUtil.AreClose(minWidth, _contentPresenter.MinWidth))
                    {
                        // TODO: make sure we are robust against retemplating
                        //_contentPresenter.MinWidth = minWidth + _contentPresenter.Margin.Left + _contentPresenter.Margin.Right;
                        _contentPresenter.MinWidth = minWidth;
                    }

                    double maxWidth = GetPixelValueFromRibbonControlLength(ControlSizeDefinition.MaxWidth);
                    if (!DoubleUtil.AreClose(maxWidth, _contentPresenter.MaxWidth))
                    {
                        // TODO: make sure we are robust against retemplating
                        //_contentPresenter.MaxWidth = maxWidth + _contentPresenter.Margin.Left + _contentPresenter.Margin.Right;
                        _contentPresenter.MaxWidth = maxWidth;
                    }

                    double width = GetPixelValueFromRibbonControlLength(ControlSizeDefinition.Width);
                    if (!DoubleUtil.AreClose(width, _contentPresenter.Width) && !(double.IsNaN(width) && double.IsNaN(_contentPresenter.Width)))
                    {
                        _contentPresenter.Width = width;
                    }

                    if (_ribbonControl != null)
                    {
                        // Ribbon Control Length is bound to Min/Max values of ControlSizeDefinition by default
                        // but in case on InRibbonGallery it needs to be taken care specifically as InRibbonGallery 
                        // supports Item unit types as well.
                        maxWidth = _contentPresenter.MaxWidth + _contentPresenter.Margin.Left + _contentPresenter.Margin.Right + _scrollButtonsBorderFactor + WidthStylePadding;
                        if (!DoubleUtil.AreClose(maxWidth, _ribbonControl.MaxWidth))
                        {
                            _ribbonControl.MaxWidth = maxWidth;
                        }

                        minWidth = _contentPresenter.MinWidth + _contentPresenter.Margin.Left + _contentPresenter.Margin.Right + _scrollButtonsBorderFactor + WidthStylePadding;
                        if (!DoubleUtil.AreClose(minWidth, _ribbonControl.MinWidth))
                        {
                            _ribbonControl.MinWidth = minWidth;
                        }

                        // RibbonControl's Horizontal alignment by default is Stretch but InRibbonGallery has 
                        // specific requirement which RibbonControl understands.
                        if (this.HorizontalAlignment != HorizontalAlignment.Left)
                        {
                            this.HorizontalAlignment = HorizontalAlignment.Left;
                        }
                    }
                }
            }
            else
            {
                if (_ribbonControl != null)
                {
                    _ribbonControl.ClearValue(FrameworkElement.MaxWidthProperty);
                    _ribbonControl.ClearValue(FrameworkElement.MinWidthProperty);
                    _ribbonControl.ClearValue(FrameworkElement.HorizontalAlignmentProperty);
                }
            }
        }

        // Coerces the various visual's properties when InRibbonGallery attains in-Ribbon mode.
        private void CoerceVisibilityPropertiesOnRibbonGallery()
        {
            if (_firstGallery != null)
            {
                _firstGallery.CoerceValue(RibbonGallery.CanUserFilterProperty);
                _firstGallery.CoerceValue(ScrollViewer.VerticalScrollBarVisibilityProperty);

                for (int i = 0; i < _firstGallery.Items.Count; i++)
                {
                    RibbonGalleryCategory category = _firstGallery.ItemContainerGenerator.ContainerFromIndex(i) as RibbonGalleryCategory;
                    if (category != null)
                    {
                        category.CoerceValue(RibbonGalleryCategory.HeaderVisibilityProperty);
                    }
                }
            }
        }

        // As InRibbonGallery switches modes from RibbonGallery being inside Ribbon or in Popup, the Visual parents needs to be changed.
        // and Visual properties of some Visuals needs to be coerced.
        private void PrivateManageRibbonGalleryVisualParent()
        {
            CoerceVisibilityPropertiesOnRibbonGallery();

            if (IsInInRibbonMode)
            {
                if (_firstGallery != null)
                {
                    if (_itemsPresenter != null)
                    {
                        RibbonMenuItemsPanel panel = (VisualTreeHelper.GetChildrenCount(_itemsPresenter) > 0 ? VisualTreeHelper.GetChild(_itemsPresenter, 0) : null) as RibbonMenuItemsPanel;
                        if (panel != null)
                        {
                            panel.RemoveFirstGallery(this);
                        }
                    }

                    if (_contentPresenter != null)
                    {
                        _contentPresenter.Content = _firstGallery;
                    }
                }
            }
            else
            {
                // Removes _firstGallery from ContentPresenter and add it to the ItemsPresenter
                if (_contentPresenter != null)
                {
                    _contentPresenter.Content = null;
                }

                if (_itemsPresenter != null)
                {
                    if (VisualTreeHelper.GetChildrenCount(_itemsPresenter) > 0)
                    {
                        RibbonMenuItemsPanel panel = VisualTreeHelper.GetChild(_itemsPresenter, 0) as RibbonMenuItemsPanel;
                        if (panel != null && _firstGallery != null)
                        {
                            panel.ReInsertFirstGallery(this);
                        }
                    }
                }
            }
        }

        #endregion

        #region Internal Properties and Methods

        internal bool IsInInRibbonMode
        {
            get
            {
                return !IsCollapsed && !IsDropDownOpen;
            }
        }

        internal RibbonGallery FirstGallery
        {
            get
            {
                return _firstGallery;
            }
            private set
            {
                if (_firstGallery != null)
                {
                    _firstGallery.ShouldGalleryItemsAcquireFocus = true;
                }
                
                _firstGallery = value;

                if (_firstGallery != null)
                {
                    _firstGallery.ShouldGalleryItemsAcquireFocus = false;
                }
            }
        }
        
        internal RepeatButton ScrollUpButton
        {
            get
            {
                return _scrollUpButton;
            }
        }

        internal RepeatButton ScrollDownButton
        {
            get
            {
                return _scrollDownButton;
            }
        }

        // The InRibbonGallery's Popup must open with the same width as the InRibbonGallery has while
        // in the Ribbon and must not be resizable below that width.
        internal double CalculateGalleryItemsPresenterMinWidth()
        {
            Debug.Assert(this.IsDropDownOpen, "This method is only applicable when IsDropDownOpen is true.");

            if (_contentPresenter != null)
            {
                return _contentPresenter.ActualWidth;
            }
            else
            {
                // If IRG is retemplated but ContentPresenterTemplatePartName is not found, approximate its dimensions.
                // TODO: Revisit these values when finalizing IRG's template.
                // TODO: Are we being too smart here?  Does this affect RibbonGroup's Measure/Arrange?
                double scrollButtonsBorderWidth = _scrollButtonsBorder != null ? _scrollButtonsBorder.ActualWidth : 14.0;
                return this.ActualWidth - scrollButtonsBorderWidth;
            }
        }

        #endregion

        #region Public Overrides

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _itemsPresenter = (ItemsPresenter)GetTemplateChild(ItemsPresenterTemplatePartName);
            _contentPresenter = (ContentPresenter)GetTemplateChild(ContentPresenterTemplatePartName);
            _scrollButtonsBorder = (Border)GetTemplateChild(ScrollButtonsBorderTemplatePartName);

            _scrollUpButton = GetTemplateChild(ScrollUpRepeatButtonTemplatePartName) as RepeatButton;
            if (_scrollUpButton != null)
            {
                _scrollUpButton.SetValue(AutomationProperties.NameProperty, _scrollUpButtonAutomationName);
                _scrollUpButton.SetBinding(RepeatButton.IsEnabledProperty, new Binding("CanLineUp") { Source = this });
                if (_scrollUpButton.CommandTarget == null)
                {
                    _scrollUpButton.SetBinding(RepeatButton.CommandTargetProperty, new Binding("CommandTarget") { Source = this });
                }
            }

            _scrollDownButton = GetTemplateChild(ScrollDownRepeatButtonTemplatePartName) as RepeatButton;
            if (_scrollDownButton != null)
            {
                _scrollDownButton.SetValue(AutomationProperties.NameProperty, _scrollDownButtonAutomationName);
                _scrollDownButton.SetBinding(RepeatButton.IsEnabledProperty, new Binding("CanLineDown") { Source = this });
                if (_scrollDownButton.CommandTarget == null)
                {
                    _scrollDownButton.SetBinding(RepeatButton.CommandTargetProperty, new Binding("CommandTarget") { Source = this });
                }
            }

            RibbonToggleButton partToggleButton = PartToggleButton;
            if (partToggleButton != null)
            {
                Binding labelBinding = new Binding("Label") { RelativeSource = RelativeSource.TemplatedParent };
                partToggleButton.SetBinding(AutomationProperties.NameProperty, labelBinding);
            }

            if (Popup != null && Popup.Child != null)
            {
                Popup.Child.Measure(new Size());
                UpdateFirstGallery();
            }

            UpdateInRibbonGalleryModeProperties();
        }

        #endregion 

        #region KeyTips

        protected override void OnActivatingKeyTip(ActivatingKeyTipEventArgs e)
        {
            if (e.OriginalSource == this)
            {
                if (!IsCollapsed)
                {
                    RibbonHelper.SetKeyTipPlacementForInRibbonGallery(this, e);
                }
                else
                {
                    base.OnActivatingKeyTip(e);
                }
            }
        }

        #endregion

        #region Scrolling
        
        private bool CanLineUp
        {
            get { return (bool)GetValue(CanLineUpProperty); }
            set { SetValue(CanLineUpProperty, value); }
        }

        internal static readonly DependencyProperty CanLineUpProperty =
                    DependencyProperty.Register("CanLineUp",
                                typeof(bool),
                                typeof(InRibbonGallery),
                                new FrameworkPropertyMetadata(true, null, CoerceCanLineUp));

        private static object CoerceCanLineUp(DependencyObject d, object baseValue)
        {
            InRibbonGallery irg = (InRibbonGallery)d;
            return CoerceCanLineUpDown(irg, true);
        }

        private bool CanLineDown
        {
            get { return (bool)GetValue(CanLineDownProperty); }
            set { SetValue(CanLineDownProperty, value); }
        }

        internal static readonly DependencyProperty CanLineDownProperty =
                    DependencyProperty.Register("CanLineDown",
                                typeof(bool),
                                typeof(InRibbonGallery),
                                new FrameworkPropertyMetadata(true, null, CoerceCanLineDown));

        private static object CoerceCanLineDown(DependencyObject d, object baseValue)
        {
            InRibbonGallery irg = (InRibbonGallery)d;
            return CoerceCanLineUpDown(irg, false);
        }

        private static bool CoerceCanLineUpDown(InRibbonGallery irg, bool lineUp)
        {
            if (irg.FirstGallery != null)
            {
                RibbonGalleryCategoriesPanel categoriesPanel = irg.FirstGallery.ItemsHostSite as RibbonGalleryCategoriesPanel;
                if (categoriesPanel != null)
                {
                    return lineUp ? categoriesPanel.InRibbonModeCanLineUp() : categoriesPanel.InRibbonModeCanLineDown();
                }
            }

            return true;
        }

        #endregion

        #region Data
        
        // Flag tracking when FirstGallery's current filter has changed, which forces us to remeasure.
        internal bool FilterHasChanged { get; set; }

        private ItemsPresenter _itemsPresenter;
        private RepeatButton _scrollUpButton;
        private RepeatButton _scrollDownButton;
        private ContentPresenter _contentPresenter;
        private RibbonGallery _firstGallery;
        private WeakReference _firstGalleryItem;
        private RibbonControl _ribbonControl;
        private Border _scrollButtonsBorder;
        private double _scrollButtonsBorderFactor;
        private Size _cachedDesiredSize;

        private const string ItemsPresenterTemplatePartName = "ItemsPresenter";
        private const string ContentPresenterTemplatePartName = "PART_ContentPresenter";
        private const string ScrollButtonsBorderTemplatePartName = "PART_ScrollButtonsBorder";
        private const string ScrollUpRepeatButtonTemplatePartName = "PART_ScrollUp";
        private const string ScrollDownRepeatButtonTemplatePartName = "PART_ScrollDown";

        private static string _scrollUpButtonAutomationName = Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.InRibbonGallery_ScrollUpButtonAutomationName);
        private static string _scrollDownButtonAutomationName = Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.InRibbonGallery_ScrollDownButtonAutomationName);

        #endregion

    }
}
