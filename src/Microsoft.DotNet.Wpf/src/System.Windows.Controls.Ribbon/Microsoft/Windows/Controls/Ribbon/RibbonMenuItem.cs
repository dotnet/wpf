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
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Automation.Peers;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using System.Windows.Media;
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
    ///   A Ribbon-specific sublclass of MenuItem
    ///   Handles Mouse and Keyboard input differently than MenuItem.
    /// </summary>
    [TemplatePart(Name = RibbonMenuItem.ResizeThumbTemplatePartName, Type = typeof(Thumb))]
    [TemplatePart(Name = RibbonMenuItem.PopupTemplatePartName, Type = typeof(Popup))]
    [TemplatePart(Name = RibbonMenuItem.SideBarBorderTemplatePartName, Type = typeof(Border))]
    [TemplatePart(Name = RibbonMenuItem.SubMenuScrollViewerTemplatePartName, Type = typeof(ScrollViewer))]
    [StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(RibbonMenuItem))]
    public class RibbonMenuItem : MenuItem, ISyncKeyTipAndContent
    {
        #region Constructors

        /// <summary>
        ///   Initializes static members of the RibbonMenuItem class.
        /// </summary>
        static RibbonMenuItem()
        {
            Type ownerType = typeof(RibbonMenuItem);

            ItemsPanelTemplate template = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(RibbonMenuItemsPanel)));
            template.Seal();
            ItemsPanelProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(template));

            DefaultStyleKeyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(ownerType));
            ToolTipProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(null, new CoerceValueCallback(RibbonHelper.CoerceRibbonToolTip)));
            ToolTipService.ShowOnDisabledProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(true));
            CommandProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(RibbonHelper.OnCommandChanged));
            ContextMenuProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(RibbonHelper.OnContextMenuChanged, RibbonHelper.OnCoerceContextMenu));
            ContextMenuService.ShowOnDisabledProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(true));
            IsSubmenuOpenProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnIsSubmenuOpenChanged)));
            IsCheckedProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnIsCheckedChanged)));
            HasGalleryProperty.OverrideMetadata(typeof(RibbonMenuItem), new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnHasGalleryChanged)), RibbonMenuButton.HasGalleryPropertyKey);
            HeaderProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnHeaderChanged), new CoerceValueCallback(CoerceHeader)));
            
            EventManager.RegisterClassHandler(ownerType, Mouse.PreviewMouseDownOutsideCapturedElementEvent, new MouseButtonEventHandler(OnClickThroughThunk));
            EventManager.RegisterClassHandler(ownerType, RibbonMenuButton.RibbonIsSelectedChangedEvent, new RoutedPropertyChangedEventHandler<bool>(OnRibbonIsSelectedChanged));
            EventManager.RegisterClassHandler(ownerType, RibbonControlService.DismissPopupEvent, new RibbonDismissPopupEventHandler(OnDismissPopupThunk));
            EventManager.RegisterClassHandler(ownerType, KeyTipService.ActivatingKeyTipEvent, new ActivatingKeyTipEventHandler(OnActivatingKeyTipThunk));
            EventManager.RegisterClassHandler(ownerType, KeyTipService.KeyTipAccessedEvent, new KeyTipAccessedEventHandler(OnKeyTipAccessedThunk));
            EventManager.RegisterClassHandler(ownerType, FrameworkElement.ContextMenuOpeningEvent, new ContextMenuEventHandler(OnContextMenuOpeningThunk), true);
            EventManager.RegisterClassHandler(ownerType, FrameworkElement.ContextMenuClosingEvent, new ContextMenuEventHandler(OnContextMenuClosingThunk), true);
        }

        #endregion

        #region ToolTip Properties

        /// <summary>
        ///     DependencyProperty for ToolTipTitle property.
        /// </summary>
        public static readonly DependencyProperty ToolTipTitleProperty =
            RibbonControlService.ToolTipTitleProperty.AddOwner(typeof(RibbonMenuItem), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

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
            RibbonControlService.ToolTipDescriptionProperty.AddOwner(typeof(RibbonMenuItem), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

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
            RibbonControlService.ToolTipImageSourceProperty.AddOwner(typeof(RibbonMenuItem), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

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
            RibbonControlService.ToolTipFooterTitleProperty.AddOwner(typeof(RibbonMenuItem), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

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
            RibbonControlService.ToolTipFooterDescriptionProperty.AddOwner(typeof(RibbonMenuItem), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

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
            RibbonControlService.ToolTipFooterImageSourceProperty.AddOwner(typeof(RibbonMenuItem), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

        /// <summary>
        ///     Image source for the footer of the tooltip of the control.
        /// </summary>
        public ImageSource ToolTipFooterImageSource
        {
            get { return RibbonControlService.GetToolTipFooterImageSource(this); }
            set { RibbonControlService.SetToolTipFooterImageSource(this, value); }
        }

        #endregion ToolTip Properties

        #region RibbonControlService Properties

        /// <summary>
        ///     DependencyProperty for ImageSource property.
        /// </summary>
        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.Register("ImageSource", 
                                        typeof(ImageSource), 
                                        typeof(RibbonMenuItem), 
                                        new FrameworkPropertyMetadata(new PropertyChangedCallback(OnImageSourceChanged)));

        /// <summary>
        /// Gets or sets the ImageSource for the RibbonMenuItem.
        /// </summary>
        public ImageSource ImageSource
        {
            get { return (ImageSource)GetValue(ImageSourceProperty); }
            set { SetValue(ImageSourceProperty, value); }
        }

        private static void OnImageSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(QuickAccessToolBarImageSourceProperty);
        }

        /// <summary>
        ///     DependencyProperty for QuickAccessToolBarImageSource property.
        /// </summary>
        public static readonly DependencyProperty QuickAccessToolBarImageSourceProperty =
            DependencyProperty.Register("QuickAccessToolBarImageSource",
                                        typeof(ImageSource),
                                        typeof(RibbonMenuItem),
                                        new FrameworkPropertyMetadata(null, new CoerceValueCallback(CoerceQuickAccessToolBarImageSource)));

        /// <summary>
        /// Gets or sets the QuickAccessToolBarImageSource for the RibbonMenuItem. 
        /// This property is used as the image for this MenuItem when added to the 
        /// QAT. By default the value of this property is coerced to be the same as 
        /// the ImageSource property. This property is available separate from the 
        /// ImageSource property because there may be instances where the MenuItems 
        /// within a drop down do not show any Image, but when that MenuItem is added 
        /// to the QAT, it shows an image. (Notice the default green icon that shows 
        /// for MenuItem within Office apps.)
        /// </summary>
        public ImageSource QuickAccessToolBarImageSource
        {
            get { return (ImageSource)GetValue(QuickAccessToolBarImageSourceProperty); }
            set { SetValue(QuickAccessToolBarImageSourceProperty, value); }
        }

        private static object CoerceQuickAccessToolBarImageSource(DependencyObject d, object baseValue)
        {
            if (baseValue == null)
            {
                return ((RibbonMenuItem)d).ImageSource;
            }

            return baseValue;
        }

        #endregion

        #region Drop Down Properties

        private static readonly DependencyPropertyKey IsDropDownPositionedLeftPropertyKey = DependencyProperty.RegisterReadOnly("IsDropDownPositionedLeft",
                                                                                                                typeof(bool),
                                                                                                                typeof(RibbonMenuItem),
                                                                                                                new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty IsDropDownPositionedLeftProperty = IsDropDownPositionedLeftPropertyKey.DependencyProperty;

        /// <summary>
        /// Indicates whether the Dropdown Popup position has been flipped to the left side of MenuItem
        /// because Popup was near a screen edge.
        /// </summary>
        public bool IsDropDownPositionedLeft
        {
            get { return (bool)GetValue(IsDropDownPositionedLeftProperty); }
            private set { SetValue(IsDropDownPositionedLeftPropertyKey, value); }
        }

        public static readonly DependencyProperty CanUserResizeVerticallyProperty = RibbonMenuButton.CanUserResizeVerticallyProperty.AddOwner(
                                                                                                        typeof(RibbonMenuItem),
                                                                                                        new FrameworkPropertyMetadata(false,
                                                                                                            null,
                                                                                                            new CoerceValueCallback(CoerceCanUserResizeProperty)));
        /// <summary>
        /// Gets or sets whether user is allowed to resize this button's dropdown Popup vertically
        /// Applicable only when Items collection has atleast one RibbonGallery.
        /// </summary>
        public bool CanUserResizeVertically
        {
            get { return (bool)GetValue(CanUserResizeVerticallyProperty); }
            set { SetValue(CanUserResizeVerticallyProperty, value); }
        }

        public static readonly DependencyProperty CanUserResizeHorizontallyProperty = RibbonMenuButton.CanUserResizeHorizontallyProperty.AddOwner(
                                                                                                        typeof(RibbonMenuItem),
                                                                                                        new FrameworkPropertyMetadata(false,
                                                                                                            null,
                                                                                                            new CoerceValueCallback(CoerceCanUserResizeProperty)));
        
        /// <summary>
        /// Gets or sets whether user is allowed to resize this button's dropdown Popup horizontally
        /// Applicable only when Items collection has atleast one RibbonGallery.
        /// and when CanUserResizeVertically is also set to true.
        /// </summary>
        public bool CanUserResizeHorizontally
        {
            get { return (bool)GetValue(CanUserResizeHorizontallyProperty); }
            set { SetValue(CanUserResizeHorizontallyProperty, value); }
        }

        public static readonly DependencyProperty DropDownHeightProperty = RibbonMenuButton.DropDownHeightProperty.AddOwner(typeof(RibbonMenuItem),
                                                                                                       new FrameworkPropertyMetadata(double.NaN,
                                                                                                            null,
                                                                                                            new CoerceValueCallback(CoerceDropDownHeightProperty)));
            

        /// <summary>
        /// Gets or Sets the height of the Popup that is displayed when this button is invoked
        /// Applicable only when Items collection has atleast one RibbonGallery.
        /// </summary>
        public double DropDownHeight
        {
            get { return (double)GetValue(DropDownHeightProperty); }
            set { SetValue(DropDownHeightProperty, value); }
        }

        public static readonly DependencyProperty HasGalleryProperty = RibbonMenuButton.HasGalleryPropertyKey.DependencyProperty.AddOwner(typeof(RibbonMenuItem));

        /// <summary>
        /// Indicates that there is atleast one RibbonGallery in Items collection.
        /// </summary>
        public bool HasGallery
        {
            get { return (bool)GetValue(HasGalleryProperty); }
            private set { SetValue(RibbonMenuButton.HasGalleryPropertyKey, value); }
        }

        private static void OnHasGalleryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(CanUserResizeHorizontallyProperty);
            d.CoerceValue(CanUserResizeVerticallyProperty);
            d.CoerceValue(DropDownHeightProperty);

            RibbonMenuItem menuItem = (RibbonMenuItem)d;
            if (menuItem.IsSubmenuOpen)
            {
                // First time the dropdown opens, HasGallery is always false
                // because item container generation never happened yet and hence
                // it ignores DropDownHeight. Hence reuse DropDownHeight when HasGallery
                // value changes.
                RibbonHelper.SetDropDownHeight(menuItem._itemsPresenter, (bool)e.NewValue, menuItem.DropDownHeight);
            }

            // Note that when the HasGallery property changes we expect that the 
            // VerticalScrollBarVisibilityProperty for the primary _submenuScrollViewer 
            // that hosts galleries and/or menu items is updated. Even though this 
            // property is marked AffectsMeasure it doesn't exactly cause the additonal 
            // call to ScrollViewer.MeasureOverrider because HasGallery is typically 
            // updated during a Measure pass when PrepareContainerForItemOverride is 
            // called and thus the invalidation noops. To ensure that we call 
            // ScrollViewer.MeasureOverride another time after this property has been 
            // updated, we need to wait for the current Measure pass to subside and 
            // then InvalidateMeasure on the _submenuScrollViewer.

            RibbonHelper.InvalidateScrollBarVisibility(menuItem._submenuScrollViewer);
        }

        private void OnItemContainerGeneratorStatusChanged(object sender, EventArgs e)
        {
            if (ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                ItemContainerGenerator.StatusChanged -= OnItemContainerGeneratorStatusChanged;

                CoerceValue(CanUserResizeHorizontallyProperty);
                CoerceValue(CanUserResizeVerticallyProperty);
                CoerceValue(DropDownHeightProperty);
            }
        }

        private static object CoerceDropDownHeightProperty(DependencyObject d, object baseValue)
        {
            RibbonMenuItem menuItem = (RibbonMenuItem)d;

            if ((menuItem.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated) &&
                !menuItem.HasGallery)
            {
                return double.NaN;
            }
            return baseValue;
        }

        private static object CoerceCanUserResizeProperty(DependencyObject d, object baseValue)
        {
            RibbonMenuItem menuItem = (RibbonMenuItem)d;

            if ((menuItem.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated) &&
                !menuItem.HasGallery)
            {
                return false;
            }
            return baseValue;
        }

        #endregion

        #region Visual States

        /// <summary>
        ///     DependencyProperty for Ribbon property.
        /// </summary>
        public static readonly DependencyProperty RibbonProperty =
            RibbonControlService.RibbonProperty.AddOwner(typeof(RibbonMenuItem));

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
            RibbonControlService.MouseOverBorderBrushProperty.AddOwner(typeof(RibbonMenuItem));

        /// <summary>
        ///     Outer border brush used in a "hover" state of the RibbonMenuItem.
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
            RibbonControlService.MouseOverBackgroundProperty.AddOwner(typeof(RibbonMenuItem));

        /// <summary>
        ///     Control background brush used in a "hover" state of the RibbonMenuItem.
        /// </summary>
        public Brush MouseOverBackground
        {
            get { return RibbonControlService.GetMouseOverBackground(this); }
            set { RibbonControlService.SetMouseOverBackground(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for PressedBorderBrush property.
        /// </summary>
        public static readonly DependencyProperty PressedBorderBrushProperty =
            RibbonControlService.PressedBorderBrushProperty.AddOwner(typeof(RibbonMenuItem));

        /// <summary>
        ///     Outer border brush used in a "pressed" state of the RibbonMenuItem.
        /// </summary>
        public Brush PressedBorderBrush
        {
            get { return RibbonControlService.GetPressedBorderBrush(this); }
            set { RibbonControlService.SetPressedBorderBrush(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for PressedBackground property.
        /// </summary>
        public static readonly DependencyProperty PressedBackgroundProperty =
            RibbonControlService.PressedBackgroundProperty.AddOwner(typeof(RibbonMenuItem));

        /// <summary>
        ///     Control background brush used in a "pressed" state of the RibbonMenuItem.
        /// </summary>
        public Brush PressedBackground
        {
            get { return RibbonControlService.GetPressedBackground(this); }
            set { RibbonControlService.SetPressedBackground(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for CheckedBackground property.
        /// </summary>
        public static readonly DependencyProperty CheckedBackgroundProperty =
            RibbonControlService.CheckedBackgroundProperty.AddOwner(typeof(RibbonMenuItem));

        /// <summary>
        ///     Control background brush used in a "Checked" state of the RibbonMenuItem.
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
            RibbonControlService.CheckedBorderBrushProperty.AddOwner(typeof(RibbonMenuItem));

        /// <summary>
        ///     Control border brush used to paint a "Checked" RibbonMenuItem.
        /// </summary>
        public Brush CheckedBorderBrush
        {
            get { return RibbonControlService.GetCheckedBorderBrush(this); }
            set { RibbonControlService.SetCheckedBorderBrush(this, value); }
        }

        #endregion

        #region ContainerGeneration

        private object _currentItem;

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            bool ret = (item is RibbonMenuItem) || (item is RibbonSeparator) || (item is RibbonGallery);
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
                    if (itemContainer is RibbonMenuItem || itemContainer is RibbonGallery || itemContainer is RibbonSeparator)
                    {
                        return itemContainer as DependencyObject;
                    }
                    else
                    {
                        throw new InvalidOperationException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.InvalidMenuButtonOrItemContainer, this.GetType().Name, itemContainer));
                    }
                }
            }

            return new RibbonMenuItem();
        }

        protected override bool ShouldApplyItemContainerStyle(DependencyObject container, object item)
        {
            if (container is RibbonSeparator ||
                container is RibbonGallery)
            {
                return false;
            }
            else
            {
                return base.ShouldApplyItemContainerStyle(container, item);
            }
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            if (element is RibbonGallery)
            {
                HasGallery = (++_galleryCount > 0);
            }
            else
            {
                RibbonSeparator separator = element as RibbonSeparator;
                if (separator != null)
                {
                    ValueSource vs = DependencyPropertyHelper.GetValueSource(separator, StyleProperty);
                    if (vs.BaseValueSource <= BaseValueSource.ImplicitStyleReference)
                        separator.SetResourceReference(StyleProperty, MenuItem.SeparatorStyleKey);

                    separator.DefaultStyleKeyInternal = MenuItem.SeparatorStyleKey;
                }
            }
        }

        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            base.ClearContainerForItemOverride(element, item);

            if (element is RibbonGallery)
            {
                HasGallery = (--_galleryCount > 0);
            }
        }

#if !RIBBON_IN_FRAMEWORK
        /// <summary>
        ///     DependencyProperty for ItemContainerTemplateSelector property.
        /// </summary>
        public static readonly DependencyProperty ItemContainerTemplateSelectorProperty =
            RibbonMenuButton.ItemContainerTemplateSelectorProperty.AddOwner(
                typeof(RibbonMenuItem),
                new FrameworkPropertyMetadata(new DefaultItemContainerTemplateSelector()));

        /// <summary>
        ///     DataTemplateSelector property which provides the DataTemplate to be used to create an instance of the ItemContainer.
        /// </summary>
        public ItemContainerTemplateSelector ItemContainerTemplateSelector
        {
            get { return (ItemContainerTemplateSelector)GetValue(ItemContainerTemplateSelectorProperty); }
            set { SetValue(ItemContainerTemplateSelectorProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for UsesItemContainerTemplate property.
        /// </summary>
        public static readonly DependencyProperty UsesItemContainerTemplateProperty =
            RibbonMenuButton.UsesItemContainerTemplateProperty.AddOwner(typeof(RibbonMenuItem));

        /// <summary>
        ///     UsesItemContainerTemplate property which says whether the ItemContainerTemplateSelector property is to be used.
        /// </summary>
        public bool UsesItemContainerTemplate
        {
            get { return (bool)GetValue(UsesItemContainerTemplateProperty); }
            set { SetValue(UsesItemContainerTemplateProperty, value); }
        }
#endif

        #endregion ContainerGeneration
        
        #region Protected Methods

        public override void OnApplyTemplate()
        {
            // If a new template has just been generated then 
            // be sure to clear any stale ItemsHost references
            if (InternalItemsHost != null && !this.IsAncestorOf(InternalItemsHost))
            {
                InternalItemsHost = null;
            }
            
            base.OnApplyTemplate();

            if (_resizeThumb != null)
            {
                _resizeThumb.DragStarted -= new DragStartedEventHandler(OnPopupResizeStarted);
                _resizeThumb.DragDelta -= new DragDeltaEventHandler(OnPopupResize);
            }

            _resizeThumb = GetTemplateChild(ResizeThumbTemplatePartName) as Thumb;
            if (_resizeThumb != null)
            {
                _resizeThumb.DragStarted += new DragStartedEventHandler(OnPopupResizeStarted);
                _resizeThumb.DragDelta += new DragDeltaEventHandler(OnPopupResize);
            }

            _itemsPresenter = GetTemplateChild(ItemsPresenterTemplatePartName) as ItemsPresenter;

            if (_popup != null)
            {
                _popup.Opened -= new EventHandler(OnPopupOpened);
                _popup.CustomPopupPlacementCallback -= new CustomPopupPlacementCallback(PlacePopup);
            }
            
            _popup = GetTemplateChild(PopupTemplatePartName) as Popup;
            
            if (_popup != null)
            {
                _popup.Opened += new EventHandler(OnPopupOpened);
                _popup.CustomPopupPlacementCallback += new CustomPopupPlacementCallback(PlacePopup);
            }

            _sideBarBorder = GetTemplateChild(SideBarBorderTemplatePartName) as UIElement;

            PropertyHelper.TransferProperty(this, ContextMenuProperty);   // Coerce to get a default ContextMenu if none has been specified.
            PropertyHelper.TransferProperty(this, RibbonControlService.CanAddToQuickAccessToolBarDirectlyProperty);

            ItemContainerGenerator.StatusChanged += new EventHandler(OnItemContainerGeneratorStatusChanged);

            _submenuScrollViewer = GetTemplateChild(SubMenuScrollViewerTemplatePartName) as ScrollViewer;
            if (_submenuScrollViewer != null)
            {
                KeyTipService.SetCanClipKeyTip(_submenuScrollViewer, false);
            }
        }

        /// <summary>
        /// ToolTip is always opened as soon as the cursor moves over it,
        /// it stays invisible for the duration of the InitialShowDelay.
        /// Because the submenu popup is opened with a delay, it covers up the tooltip.
        /// To avoid this, we disable ToolTipService, however ToolTIp service does not
        /// close previously opened tooltips when it is disabled. So we need to
        /// force it closed as soon as the popup comes up.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPopupOpened(object sender, EventArgs e)
        {
            RibbonToolTip toolTip = ToolTip as RibbonToolTip;
            if (toolTip != null)
            {
                toolTip.IsOpen = false;
            }
        }

        /// <summary>
        /// RibbonMenuItems are hosted directly in a Menu, therefore TopLevelHeader should behave same as SubmenuHeader
        /// In base MenuItem ToplevelHeaders are opened immediately, 
        /// but we want them to be opened after MenuShowDelay delay
        /// Therefore reimplement base.OnMouseEnter here.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseEnter(System.Windows.Input.MouseEventArgs e)
        {
            FocusOrSelect();
            if (CanOpenSubMenu)
            {
                SetTimerToOpenSubmenu();
            }

            UpdateIsPressed();
        }


        /// <summary>
        /// base MenuItem closes submenu when mouse moves away. 
        /// Therefore reimplement base.OnMouseLeave ourselves without closing submenu's. 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeave(System.Windows.Input.MouseEventArgs e)
        {
            if (IgnoreNextMouseLeave)
            {
                IgnoreNextMouseLeave = false;
                return;
            }

            if (!IsSubmenuOpen)
            {
                // Either this is a leaf node (SubmenuItem or ToplevelItem role)
                // or Mouse left a header node before MenuShowDelay time. 
                IsHighlighted = false;
                RibbonIsSelected = false;

                if (IsKeyboardFocusWithin)
                {
                    ItemsControl parent = ItemsControl.ItemsControlFromItemContainer(this);
                    if (parent != null)
                    {
                        parent.Focus();
                    }
                }
            }

            UpdateIsPressed();
        }


        /// <summary>
        ///     Focus this item or, if that fails, just mark it selected.
        /// </summary>
        internal void FocusOrSelect()
        {
            // Setting focus will cause the item to be selected,
            // but if we fail to focus we should still select.
            // (This is to help enable focusless menus).
            // Check IsKeyboardFocusWithin to allow rich content within the menuitem.
            if (!IsKeyboardFocusWithin)
            {
                Focus();
            }

            if (!RibbonIsSelected)
            {
                // If it's already focused, make sure it's also selected.
                RibbonIsSelected = true;
            }

            // If the item is selected we should ensure that it's highlighted.
            if (RibbonIsSelected && !IsHighlighted)
            {
                IsHighlighted = true;
            }
        }

        private void UpdateIsPressed()
        {
            Rect itemBounds = new Rect(new Point(), RenderSize);

            if ((Mouse.LeftButton == MouseButtonState.Pressed) &&
                IsMouseOver &&
                itemBounds.Contains(Mouse.GetPosition(this)))
            {
                IsPressed = true;
            }
            else
            {
                IsPressed = false;
            }
        }

        internal bool HandleLeftKeyDown(DependencyObject originalSource)
        {
            UIElement popupChild = _popup.TryGetChild();
            if (popupChild != null &&
                originalSource != null &&
                (!popupChild.IsKeyboardFocusWithin || TreeHelper.IsVisualAncestorOf(popupChild, originalSource)))
            {
                if (IsSubmenuOpen)
                {
                    this.Focus();
                    CloseSubmenu();
                    return true;
                }
            }
            return false;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (CanOpenSubMenu)
            {
                RibbonHelper.HandleDropDownKeyDown(this, e,
                    delegate { return IsSubmenuOpen; },
                    delegate(bool value) { IsSubmenuOpen = value; },
                    this,
                    _popup.TryGetChild());
            }

            if (e.Handled)
            {
                return;
            }

            if (CanOpenSubMenu || IsSubmenuOpen)
            {
                Key key = e.Key;
                // In Right to Left mode we switch Right and Left keys
                if (FlowDirection == FlowDirection.RightToLeft)
                {
                    if (key == Key.Right)
                    {
                        key = Key.Left;
                    }
                    else if (key == Key.Left)
                    {
                        key = Key.Right;
                    }
                }

                bool handled = false;
                int focusedIndex = ItemContainerGenerator.IndexFromContainer(e.OriginalSource as DependencyObject);
                bool itemNavigateFromCurrentFocused = true;
                if (focusedIndex < 0)
                {
                    UIElement popupChild = _popup.TryGetChild();
                    if (popupChild != null &&
                        popupChild.IsKeyboardFocusWithin)
                    {
                        // If the popup already has focus within,
                        // then the focused element is not the item container
                        // itself, but is inside one such container (eg. filter button
                        // of gallery). Hence do not navigate in such case, but do default
                        // keyboard navigation.
                        itemNavigateFromCurrentFocused = false;
                    }
                }
                switch (key)
                {
                    case Key.Enter:
                    case Key.Space:
                    case Key.Right:
                        if (!IsSubmenuOpen)
                        {
                            OpenRibbonSubmenuWithKeyboard();
                            if (key != Key.Right)
                            {
                                _handleNextUpKey = key;
                            }
                            handled = true;
                        }
                        else if (_ribbonCurrentSelection == null)
                        {
                            if (e.Key == Key.Right)
                            {
                                handled = RibbonHelper.NavigateToNextMenuItemOrGallery(this, -1, BringIndexIntoView);
                            }
                            else
                            {
                                CloseSubmenu();
                                handled = true;
                            }
                        }
                        break;

                    case Key.Left:
                        handled = HandleLeftKeyDown(e.OriginalSource as DependencyObject);
                        break;

                    // If a menuitem gets a down or up key and the submenu is open, we should focus the first or last
                    // item in the submenu (respectively).  If the submenu is not opened, this will be handled by Menu.
                    case Key.Down:
                        if (IsSubmenuOpen)
                        {
                            if (itemNavigateFromCurrentFocused)
                            {
                                handled = RibbonHelper.NavigateToNextMenuItemOrGallery(this, focusedIndex, BringIndexIntoView);
                            }
                            else
                            {
                                RibbonHelper.MoveFocus(FocusNavigationDirection.Down);
                                handled = true;
                            }
                        }
                        break;

                    case Key.Up:
                        if (IsSubmenuOpen)
                        {
                            if (itemNavigateFromCurrentFocused)
                            {
                                handled = RibbonHelper.NavigateToPreviousMenuItemOrGallery(this, focusedIndex, BringIndexIntoView);
                            }
                            else
                            {
                                RibbonHelper.MoveFocus(FocusNavigationDirection.Up);
                                handled = true;
                            }
                        }
                        break;
                    case Key.Tab:
                        if (IsSubmenuOpen && _popup != null && !_popup.IsKeyboardFocusWithin)
                        {
                            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                            {
                                handled = RibbonHelper.NavigateToPreviousMenuItemOrGallery(this, focusedIndex, BringIndexIntoView);
                            }
                            else
                            {
                                handled = RibbonHelper.NavigateToNextMenuItemOrGallery(this, focusedIndex, BringIndexIntoView);
                            }
                        }
                        break;
                    case Key.Home:
                        if (IsSubmenuOpen)
                        {
                            handled = RibbonHelper.NavigateToNextMenuItemOrGallery(this, -1, BringIndexIntoView);
                        }
                        break;
                    case Key.End:
                        if (IsSubmenuOpen)
                        {
                            handled = RibbonHelper.NavigateToPreviousMenuItemOrGallery(this, -1, BringIndexIntoView);
                        }
                        break;
                }
                e.Handled = handled;
            }
            else
            {
                if (e.Key == Key.Space)
                {
                    OnClick();
                    e.Handled = true;
                }
            }

            if( !e.Handled &&
                e.Key != Key.Left &&
                e.Key != Key.Right &&
                e.Key != Key.Tab)
                base.OnKeyDown(e);
        }

        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            if (e.Key == _handleNextUpKey)
            {
                // Space/Enter key down opens the dropdown and sets focus
                // on the first child. Such a child (gallery item) may handle
                // key up of Space/Enter and select self closing the dropdown.
                // Hence we eat away the first up of space/enter in such case.
                e.Handled = true;
                _handleNextUpKey = Key.None;
            }
            base.OnPreviewKeyUp(e);
        }

        /// <summary>
        /// Called when the focus is no longer on or within this element.
        /// </summary>
        protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
        {
            // Do not call base, since its selection logic interferes with ours.
            // base.OnIsKeyboardFocusWithinChanged(e);

            if (IsKeyboardFocusWithin && !RibbonIsSelected)
            {
                // If an item within us got focus (probably programatically), we need to become selected
                RibbonIsSelected = true;
            }
        }

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            // Do not call base, since its selection logic interferes with ours.
            // base.OnGotKeyboardFocus(e);

            FrameworkElement ribbonCurrentSelection = RibbonCurrentSelection;
            if (ribbonCurrentSelection != null &&
                IsSubmenuOpen)
            {
                UIElement popupChild = _popup.TryGetChild();
                if (popupChild != null &&
                    !popupChild.IsKeyboardFocusWithin)
                {
                    // If the drop down is open and the ribbonCurrentSelection is valid
                    // but still popup doesnt have focus within,
                    // then focus the current selection.
                    ribbonCurrentSelection.Focus();
                }
            }
        }

        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnLostKeyboardFocus(e);
            if (e.OriginalSource == this)
            {
                Dispatcher.BeginInvoke(
                    (Action)delegate()
                    {
                        if (!IsKeyboardFocusWithin && !InContextMenu)
                        {
                            RibbonIsSelected = false;
                        }
                    },
                    DispatcherPriority.Input,
                    null);
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (Role == MenuItemRole.TopLevelHeader && IsSubmenuOpen)
            {
                // For TopLevelHeader menu items, base closes the submenu on mouse down.
                // Hence do the needed and handle the event.
                UpdateIsPressed();
                e.Handled = true;
            }
            base.OnMouseLeftButtonDown(e);
        }

        /// <summary>
        /// Used by the derived classes to place popup properly.
        /// </summary>
        internal Popup Popup
        {
            get { return _popup; }
        }

        #endregion

        #region Dropdown resizing

        void OnPopupResizeStarted(object sender, DragStartedEventArgs e)
        {
            RibbonDropDownHelper.OnPopupResizeStarted(_itemsPresenter);
            
            // Clear selection and close submenus when resizing.
            if (RibbonCurrentSelection != null)
            {
                RibbonMenuItem selectedMenuItem = RibbonCurrentSelection as RibbonMenuItem;
                RibbonCurrentSelection = null;
                if (selectedMenuItem != null)
                {
                    selectedMenuItem.IsSubmenuOpen = false;
                }
            }

            e.Handled = true;
        }

        void OnPopupResize(object sender, DragDeltaEventArgs e)
        {
            RibbonDropDownHelper.ResizePopup(_itemsPresenter,
                RibbonDropDownHelper.GetMinDropDownSize(_itemsHost, _popup, BorderThickness),
                CanUserResizeHorizontally,
                CanUserResizeVertically,
                IsDropDownPositionedLeft,
                false /*IsDropDownPositionedAbove */,
                _screenBounds,
                _popupRoot,
                e.HorizontalChange, 
                e.VerticalChange);
            e.Handled = true;
        }

        /// <summary>
        /// Called from UIA Peers.
        /// </summary>
        /// <param name="newWidth"></param>
        /// <param name="newHeight"></param>
        /// <returns></returns>
        internal bool ResizePopupInternal(double newWidth, double newHeight)
        {
            if (double.IsNaN(_itemsPresenter.Width) || double.IsNaN(_itemsPresenter.Height))
            {
                RibbonDropDownHelper.OnPopupResizeStarted(_itemsPresenter);
            }

            double horizontalDelta = newWidth - _itemsPresenter.Width;
            double verticalDelta = newHeight - _itemsPresenter.Height;

            return RibbonDropDownHelper.ResizePopup(_itemsPresenter,
                RibbonDropDownHelper.GetMinDropDownSize(_itemsHost, _popup, BorderThickness),
                CanUserResizeHorizontally,
                CanUserResizeVertically,
                IsDropDownPositionedLeft,
                false /* IsDropDownPositionedAbove */,
                _screenBounds,
                _popupRoot,
                horizontalDelta,
                verticalDelta);
        }

        #endregion

        #region Private Methods

        private void SetTimerToOpenSubmenu()
        {
            if (IsSubmenuOpen)
            {
                StopTimer(ref _closeSubmenuTimer);
            }
            else
            {
                if (_openSubmenuTimer == null)
                {
                    _openSubmenuTimer = new DispatcherTimer(DispatcherPriority.Normal);
                    _openSubmenuTimer.Tick += (EventHandler)delegate(object sender, EventArgs e)
                    {
                        StopTimer(ref _openSubmenuTimer);
                        OpenSubmenu();
                        KeyTipService.DismissKeyTips();
                    };
                }
                else
                {
                    _openSubmenuTimer.Stop();
                }

                StartTimer(_openSubmenuTimer);
            }
        }

        internal void SetTimerToCloseSubmenu()
        {
            if (!IsSubmenuOpen)
            {
                StopTimer(ref _openSubmenuTimer);
            }
            else
            {
                if (_closeSubmenuTimer == null)
                {
                    _closeSubmenuTimer = new DispatcherTimer(DispatcherPriority.Normal);
                    _closeSubmenuTimer.Tick += (EventHandler)delegate(object sender, EventArgs e)
                    {
                        StopTimer(ref _closeSubmenuTimer);
                        CloseSubmenu();
                    };
                }
                else
                {
                    _closeSubmenuTimer.Stop();
                }

                StartTimer(_closeSubmenuTimer);
            }
        }

        private static void StopTimer(ref DispatcherTimer timer)
        {
            if (timer != null)
            {
                timer.Stop();
                timer = null;
            }
        }

        private void StartTimer(DispatcherTimer timer)
        {
            Debug.Assert(timer != null, "timer should not be null.");
            Debug.Assert(!timer.IsEnabled, "timer should not be running.");

            if (timer == _closeSubmenuTimer)
            {
                timer.Interval = TimeSpan.FromMilliseconds(SystemParameters.MenuShowDelay + CloseSubmenuTimerDelayBuffer);
            }
            else
            {
                timer.Interval = TimeSpan.FromMilliseconds(SystemParameters.MenuShowDelay);
            }

            timer.Start();
        }

        internal DispatcherTimer CloseSubmenuTimer
        {
            get { return _closeSubmenuTimer; }
        }

        // Please see RibbonApplicationMenuItem's 
        // override for this property for an explanation.

        internal virtual int CloseSubmenuTimerDelayBuffer
        {
            get { return 0; }
        }

        private void OpenRibbonSubmenuWithKeyboard()
        {
            OpenSubmenu();
            // Select first Item. 
            RibbonHelper.NavigateToNextMenuItemOrGallery(this, -1, BringIndexIntoView);
        }

        private void OpenSubmenu()
        {
            if (CanOpenSubMenu)
            {
                IsSubmenuOpen = true;
            }
        }

        internal virtual bool CanOpenSubMenu
        {
            get
            {
                return (HasItems && !IsCheckable);
            }
        }

        private void CloseSubmenu()
        {
            IsSubmenuOpen = false;
        }

        internal void BringIndexIntoView(int index)
        {
            if (_itemsHost != null)
            {
                _itemsHost.BringIndexIntoViewInternal(index);
            }
        }

        private static bool IsContainerFocusable(FrameworkElement container)
        {
            return container != null && container.Focusable;
        }

        private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonMenuItemAutomationPeer peer = UIElementAutomationPeer.FromElement((RibbonMenuItem)d) as RibbonMenuItemAutomationPeer;
            if (peer != null)
            {
                peer.RaiseToggleStatePropertyChangedEvent((bool)e.OldValue, (bool)e.NewValue);
            }
        }

        private static void OnIsSubmenuOpenChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            RibbonMenuItem menuItem = (RibbonMenuItem)sender;

            // If the drop down is closed due to
            // an action of context menu or if the 
            // ContextMenu for a parent  
            // was opened by right clicking this 
            // instance then ContextMenuClosed 
            // event is never raised. 
            // Hence reset the flag.
            menuItem.InContextMenu = false;

            if ((bool)e.OldValue)
            {
                StopTimer(ref menuItem._closeSubmenuTimer);
                if (menuItem.IsMouseOver && menuItem.CanOpenSubMenu)
                {
                    // If the mouse is inside the subtree, then we will get a mouse leave, but we want to ignore it
                    // to maintain the highlight.
                    menuItem.IgnoreNextMouseLeave = true;
                }
            }
            else
            {
                StopTimer(ref menuItem._openSubmenuTimer);
                
                // Clear local values 
                // so that when DropDown opens it shows in it original size and PlacementMode
                if (menuItem._itemsPresenter != null)
                {
                    menuItem._itemsPresenter.ClearValue(FrameworkElement.HeightProperty);
                    menuItem._itemsPresenter.ClearValue(FrameworkElement.WidthProperty);
                }
                menuItem._popupOffsetY = double.NaN;

                // DropDownHeight refers to the initial Height of the popup
                // The size of the popup can change dynamically by resizing.
                if (menuItem._itemsPresenter != null && menuItem.HasGallery )
                {
                    menuItem._itemsPresenter.Height = menuItem.DropDownHeight;
                }

                // IsDropDownPositionedLeft is updated asynchronously.
                // As a result the resize thumb would change position and we could see a visual artifact of this change after Popup opens. 
                menuItem.Dispatcher.BeginInvoke(new DispatcherOperationCallback(menuItem.UpdateDropDownPosition), DispatcherPriority.Loaded, new object[] { null });
            }

            menuItem.RibbonCurrentSelection = null;

            RibbonMenuItemAutomationPeer peer = UIElementAutomationPeer.FromElement(menuItem) as RibbonMenuItemAutomationPeer;
            if (peer != null)
            {
                peer.RaiseExpandCollapseAutomationEvent((bool)e.OldValue, (bool)e.NewValue);
            }
        }

        private object UpdateDropDownPosition(object arg)
        {
            if (_popup != null)
            {
                UIElement popupChild = _popup.TryGetChild();
                if (popupChild != null)
                {
                    Point targetTopLeftCorner = this.PointToScreen(new Point());
                    Point popupTopLeftCorner = popupChild.PointToScreen(new Point());
                    bool isDropDownPhysicallyPositionedLeft = DoubleUtil.LessThanOrClose(popupTopLeftCorner.X, targetTopLeftCorner.X);
                    
                    IsDropDownPositionedLeft = 
                        (isDropDownPhysicallyPositionedLeft && FlowDirection == FlowDirection.LeftToRight) ||
                        (!isDropDownPhysicallyPositionedLeft && FlowDirection == FlowDirection.RightToLeft);

                    // Cache top edge of the popup
                    _popupOffsetY = targetTopLeftCorner.Y - popupTopLeftCorner.Y;
                }
            }

            // Cache the screen bounds of the monitor in which the dropdown is opened
            _screenBounds = RibbonDropDownHelper.GetScreenBounds(_itemsPresenter, _popup);

            // Also cache the PopupRoot if opened for the first time
            if (_popupRoot == null && _itemsPresenter != null)
            {
                _popupRoot = TreeHelper.FindVisualRoot(_itemsPresenter) as UIElement;
            }

            return null;
        }

        private CustomPopupPlacement[] PlacePopup(Size popupSize, Size targetSize, Point offset)
        {
            double popupChildMargin = 0.0;
            FrameworkElement popupChild;
            if ((popupChild = _popup.Child as FrameworkElement) != null)
            {
                popupChildMargin = (FlowDirection == FlowDirection.LeftToRight) ?  popupChild.Margin.Left : popupChild.Margin.Right ;
            }

            if (double.IsNaN(_popupOffsetY))
            {
                // Popup is opened for the first time, there could be two positions. 
                // to logical right of Target or logical left of Target

                CustomPopupPlacement logicalRightPosition;
                CustomPopupPlacement logicalLeftPosition;
                if (FlowDirection == FlowDirection.LeftToRight)
                {
                    logicalRightPosition = new CustomPopupPlacement(new Point(targetSize.Width, offset.Y), PopupPrimaryAxis.Vertical);
                    logicalLeftPosition = new CustomPopupPlacement(new Point(-popupSize.Width - popupChildMargin, offset.Y), PopupPrimaryAxis.Vertical);
                }
                else
                {
                    logicalRightPosition = new CustomPopupPlacement(new Point(-targetSize.Width - popupSize.Width - popupChildMargin, offset.Y), PopupPrimaryAxis.Vertical);
                    logicalLeftPosition = new CustomPopupPlacement(new Point(- popupChildMargin, offset.Y), PopupPrimaryAxis.Vertical);
                }

                return new CustomPopupPlacement[] { logicalRightPosition, logicalLeftPosition };
            }
            
            // Either resizing or when popup contents changed in size (e.g. Gallery filtering)
            // Top edge should remain constant constant
            double topEdge = _popupOffsetY + offset.Y;
            if (!IsDropDownPositionedLeft)
            {
                if (FlowDirection == FlowDirection.LeftToRight)
                {
                    // Anchor to right side of Target
                    return new CustomPopupPlacement[] { new CustomPopupPlacement(new Point(targetSize.Width, -topEdge), PopupPrimaryAxis.Vertical) };
                }
                else
                {
                    // Anchor to logical right side of Target
                    return new CustomPopupPlacement[] { new CustomPopupPlacement(new Point(-targetSize.Width - popupSize.Width - popupChildMargin, -topEdge), PopupPrimaryAxis.Vertical) };
                }
            }
            else
            {
                if (FlowDirection == FlowDirection.LeftToRight)
                {
                    // Anchor to left side of Target
                    return new CustomPopupPlacement[] { new CustomPopupPlacement(new Point(-popupSize.Width - popupChildMargin, -topEdge), PopupPrimaryAxis.Vertical) };
                }
                else
                {
                    // Anchor to logical left side of Target
                    return new CustomPopupPlacement[] { new CustomPopupPlacement(new Point(-popupChildMargin, -topEdge), PopupPrimaryAxis.Vertical) };
                }
            }
        }

        internal RibbonMenuItemsPanel InternalItemsHost
        {
            get
            {
                return _itemsHost;
            }
            set
            {
                _itemsHost = value;
            }
        }

        #endregion

        #region Selection

        /// <summary>
        ///     Return the current sibling of this MenuItem -- the
        ///     RibbonCurrentSelection of the parent as long as it isn't us.
        /// </summary>
        private FrameworkElement CurrentSibling
        {
            get
            {
                ItemsControl parent = ItemsControlFromItemContainer(this);
                RibbonMenuItem menuItemParent = parent as RibbonMenuItem;
                FrameworkElement sibling = null;

                if (menuItemParent != null)
                {
                    sibling = menuItemParent.RibbonCurrentSelection;
                }
                else
                {
                    RibbonMenuButton menuButtonParent = parent as RibbonMenuButton;
                    if (menuButtonParent != null)
                    {
                        sibling = menuButtonParent.RibbonCurrentSelection;
                    }
                }

                if (sibling == this)
                {
                    sibling = null;
                }

                return sibling;
            }
        }

        /// <summary>
        ///     True if this MenuItem is the current MenuItem of its parent.
        ///     Focus drives Selection, but not vice versa.  This will enable
        ///     focusless menus.
        ///
        ///     We use the Ribbon prefix to disambiguate this property from MenuItem.IsSelected.
        /// </summary>
        internal bool RibbonIsSelected
        {
            get { return (bool)GetValue(RibbonIsSelectedProperty); }
            set { SetValue(RibbonIsSelectedProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for RibbonIsSelected property.
        /// </summary>
        internal static readonly DependencyProperty RibbonIsSelectedProperty = DependencyProperty.Register("RibbonIsSelected",
                                                                                                           typeof(bool),
                                                                                                           typeof(RibbonMenuItem),
                                                                                                           new FrameworkPropertyMetadata(false,
                                                                                                           new PropertyChangedCallback(OnRibbonIsSelectedChanged)));

        private static void OnRibbonIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonMenuItem menuItem = (RibbonMenuItem)d;

            // When RibbonIsSelected changes, IsHighlighted should reflect RibbonIsSelected
            menuItem.IsHighlighted = (bool)e.NewValue;
            
            if (menuItem.RibbonIsSelected)
            {
                StopTimer(ref menuItem._closeSubmenuTimer);
            }
            else
            {
                menuItem.SetTimerToCloseSubmenu();
            }

            menuItem.RaiseEvent(new RoutedPropertyChangedEventArgs<bool>((bool)e.OldValue, (bool)e.NewValue, RibbonMenuButton.RibbonIsSelectedChangedEvent));
        }

        /// <summary>
        ///     Called when RibbonIsSelected changed on this element or any descendant.
        /// </summary>
        private static void OnRibbonIsSelectedChanged(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            // If RibbonIsSelected changed on a child of the MenuItem, change RibbonCurrentSelection
            // to the element that sent the event and handle the event.
            if (sender != e.OriginalSource)
            {
                RibbonMenuItem menuItem = (RibbonMenuItem)sender;
                FrameworkElement selectionItem = e.OriginalSource as FrameworkElement;
                if (e.NewValue)
                {
                    if (selectionItem != null)
                    {
                        ItemsControl parentItemsControl = ItemsControl.ItemsControlFromItemContainer(selectionItem);
                        if (menuItem == parentItemsControl)
                        {
                            menuItem.RibbonCurrentSelection = selectionItem;
                        }
                    }
                }
                else
                {
                    // If the item is no longer selected
                    // If the MenuItem has been deselected and it's the RibbonCurrentSelection,
                    // set our RibbonCurrentSelection to null.
                    if (menuItem.RibbonCurrentSelection == selectionItem)
                    {
                        menuItem.RibbonCurrentSelection = null;
                    }
                }

                e.Handled = true;
            }
        }

        /// <summary>
        ///     Tracks the current selection in the items collection (i.e. submenu or gallery)
        ///     of this MenuItem.
        ///
        ///     We use the Ribbon prefix to disambiguate this property from MenuItem.CurrentSelection.
        /// </summary>
        private FrameworkElement RibbonCurrentSelection
        {
            get
            {
                return _ribbonCurrentSelection;
            }
            set
            {
                if (_ribbonCurrentSelection != value)
                {
                    RibbonMenuItem selectedMenuItem = _ribbonCurrentSelection as RibbonMenuItem;
                    if (selectedMenuItem != null)
                    {
                        selectedMenuItem.RibbonIsSelected = false;
                    }
                    else
                    {
                        RibbonGallery selectedGallery = _ribbonCurrentSelection as RibbonGallery;
                        if (selectedGallery != null)
                        {
                            selectedGallery.RibbonIsSelected = false;
                        }
                    }

                    _ribbonCurrentSelection = value;

                    selectedMenuItem = _ribbonCurrentSelection as RibbonMenuItem;
                    if (selectedMenuItem != null)
                    {
                        selectedMenuItem.RibbonIsSelected = true;
                    }
                    else
                    {
                        RibbonGallery selectedGallery = _ribbonCurrentSelection as RibbonGallery;
                        if (selectedGallery != null)
                        {
                            selectedGallery.RibbonIsSelected = true;
                        }
                    }
                }
            }
        }

        #endregion

        #region Private Data

        private const string ResizeThumbTemplatePartName = "PART_ResizeThumb";
        private const string ItemsPresenterTemplatePartName = "ItemsPresenter";
        private const string PopupTemplatePartName = "PART_Popup";
        internal const string SideBarBorderTemplatePartName = "PART_SideBarBorder";
        private const string SubMenuScrollViewerTemplatePartName = "PART_SubMenuScrollViewer";

        DispatcherTimer _closeSubmenuTimer, _openSubmenuTimer;
        private Thumb _resizeThumb;
        private ItemsPresenter _itemsPresenter;
        RibbonMenuItemsPanel _itemsHost;
        private Popup _popup;
        private FrameworkElement _ribbonCurrentSelection; // can be a RibbonMenuItem or RibbonGallery
        Rect _screenBounds;
        private UIElement _popupRoot;
        private UIElement _sideBarBorder = null;
        private ScrollViewer _submenuScrollViewer;
        private int _galleryCount;
        double _popupOffsetY;
        private Key _handleNextUpKey = Key.None;
        private BitVector32 _bits = new BitVector32(0);

        private enum Bits
        {
            AreKeyTipAndContentInSync = 0x01,
            IsKeyTipSyncSource = 0x02,
            SyncingKeyTipAndContent = 0x04,
            IgnoreNextMouseLeave = 0x08,
            InContextMenu = 0x10
        }

        bool ISyncKeyTipAndContent.KeepKeyTipAndContentInSync
        {
            get { return _bits[(int)Bits.AreKeyTipAndContentInSync]; }
            set { _bits[(int)Bits.AreKeyTipAndContentInSync] = value; }
        }

        bool ISyncKeyTipAndContent.IsKeyTipSyncSource
        {
            get { return _bits[(int)Bits.IsKeyTipSyncSource]; }
            set { _bits[(int)Bits.IsKeyTipSyncSource] = value; }
        }

        bool ISyncKeyTipAndContent.SyncingKeyTipAndContent
        {
            get { return _bits[(int)Bits.SyncingKeyTipAndContent]; }
            set { _bits[(int)Bits.SyncingKeyTipAndContent] = value; }
        }

        bool IgnoreNextMouseLeave
        {
            get { return _bits[(int)Bits.IgnoreNextMouseLeave]; }
            set { _bits[(int)Bits.IgnoreNextMouseLeave] = value; }
        }

        bool InContextMenu
        {
            get { return _bits[(int)Bits.InContextMenu]; }
            set { _bits[(int)Bits.InContextMenu] = value; }
        }

        #endregion

        #region DismissPopup

        protected override void OnClick()
        {
            base.OnClick();

            // If StaysOpenOnClick is true we should not be dismissing Popups.

            if (!StaysOpenOnClick)
            {
                if (Role == MenuItemRole.SubmenuItem || Role == MenuItemRole.TopLevelItem)
                {
                    // Dismiss parent Popups
                    RaiseEvent(new RibbonDismissPopupEventArgs());
                }
            }
        }

        private static void OnDismissPopupThunk(object sender, RibbonDismissPopupEventArgs e)
        {
            RibbonMenuItem ribbonMenuItem = (RibbonMenuItem)sender;
            ribbonMenuItem.OnDismissPopup(e);
        }

        internal virtual void OnDismissPopup(RibbonDismissPopupEventArgs e)
        {
            // For a RibbonSplitMenuItem we will receive a DismissPopup notification 
            // when the header is clicked. We need to handle that event and render 
            // the operation cancelled in the StaysOpenOnClick scenario so that the 
            // Popups further up th chain aren't dismissed.

            UIElement popupChild = _popup.TryGetChild();
            RibbonHelper.HandleDismissPopup(e,
                delegate(bool value) { IsSubmenuOpen = value; },
                delegate(DependencyObject d) { return StaysOpenOnClick && e.DismissMode == RibbonDismissPopupMode.Always; },
                popupChild,
                this);
        }

        private static void OnClickThroughThunk(object sender, MouseButtonEventArgs e)
        {
            RibbonMenuItem ribbonMenuItem = (RibbonMenuItem)sender;
            ribbonMenuItem.OnClickThrough(e);
        }

        private void OnClickThrough(MouseButtonEventArgs e)
        {
            UIElement popupChild = _popup.TryGetChild();
            RibbonHelper.HandleClickThrough(this, e, popupChild);
        }

        #endregion DismissPopup

        #region UI Automation

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new RibbonMenuItemAutomationPeer(this);
        }

        internal void ClickItemInternal()
        {
            OnClick();
        }

        #endregion

        #region QAT

        /// <summary>
        ///   DependencyProperty for QuickAccessToolBarId property.
        /// </summary>
        public static readonly DependencyProperty QuickAccessToolBarIdProperty =
            RibbonControlService.QuickAccessToolBarIdProperty.AddOwner(typeof(RibbonMenuItem));

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
            RibbonControlService.CanAddToQuickAccessToolBarDirectlyProperty.AddOwner(typeof(RibbonMenuItem),
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

        #region KeyTips

        internal UIElement SideBarBorder
        {
            get
            {
                return _sideBarBorder;
            }
        }

        /// <summary>
        ///     DependencyProperty for KeyTip property.
        /// </summary>
        public static readonly DependencyProperty KeyTipProperty =
            KeyTipService.KeyTipProperty.AddOwner(typeof(RibbonMenuItem), 
                new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnKeyTipChanged), new CoerceValueCallback(CoerceKeyTip)));

        /// <summary>
        ///     KeyTip string for the control.
        /// </summary>
        public string KeyTip
        {
            get { return KeyTipService.GetKeyTip(this); }
            set { KeyTipService.SetKeyTip(this, value); }
        }

        internal void SyncKeyTipAndContent()
        {
            KeyTipAndContentSyncHelper.Sync(this, HeaderProperty);
        }

        private static void OnKeyTipChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            KeyTipAndContentSyncHelper.OnKeyTipChanged((ISyncKeyTipAndContent)d, HeaderProperty);
        }

        private static object CoerceKeyTip(DependencyObject d, object baseValue)
        {
            return KeyTipAndContentSyncHelper.CoerceKeyTip((ISyncKeyTipAndContent)d, baseValue, HeaderProperty);
        }

        private static void OnHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            KeyTipAndContentSyncHelper.OnContentPropertyChanged((ISyncKeyTipAndContent)d, HeaderProperty);
        }

        private static object CoerceHeader(DependencyObject d, object baseValue)
        {
            return KeyTipAndContentSyncHelper.CoerceContentProperty((ISyncKeyTipAndContent)d, baseValue);
        }

        private static void OnActivatingKeyTipThunk(object sender, ActivatingKeyTipEventArgs e)
        {
            ((RibbonMenuItem)sender).OnActivatingKeyTip(e);
        }

        protected virtual void OnActivatingKeyTip(ActivatingKeyTipEventArgs e)
        {
            if (e.OriginalSource == this)
            {
                e.KeyTipHorizontalPlacement = KeyTipHorizontalPlacement.KeyTipLeftAtTargetCenter;
                e.KeyTipVerticalPlacement = KeyTipVerticalPlacement.KeyTipTopAtTargetCenter;
                e.KeyTipHorizontalOffset = e.KeyTipVerticalOffset = 0;
                e.PlacementTarget = _sideBarBorder;
            }
        }

        private static void OnKeyTipAccessedThunk(object sender, KeyTipAccessedEventArgs e)
        {
            ((RibbonMenuItem)sender).OnKeyTipAccessed(e);
        }

        protected virtual void OnKeyTipAccessed(KeyTipAccessedEventArgs e)
        {
            if (e.OriginalSource == this)
            {
                if (CanOpenSubMenu)
                {
                    FocusOrSelect();
                    IsSubmenuOpen = true;
                    RibbonHelper.NavigateToNextMenuItemOrGallery(this, -1, BringIndexIntoView);
                    UIElement popupChild = _popup.TryGetChild();
                    if (popupChild != null)
                    {
                        KeyTipService.SetIsKeyTipScope(popupChild, true);
                        e.TargetKeyTipScope = popupChild;
                    }
                }
                else
                {
                    OnClick();
                }
                e.Handled = true;
            }
        }
        #endregion

        #region Context Menu

        private static void OnContextMenuOpeningThunk(object sender, ContextMenuEventArgs e)
        {
            ((RibbonMenuItem)sender).OnContextMenuOpeningInternal();
        }

        private void OnContextMenuOpeningInternal()
        {
            if (CanOpenSubMenu)
            {
                // Track whether a non-leaf menuitem is in
                // context menu so that its submenu is
                // not dismissed due to lost focus.
                InContextMenu = true;
            }
        }

        private static void OnContextMenuClosingThunk(object sender, ContextMenuEventArgs e)
        {
            ((RibbonMenuItem)sender).OnContextMenuClosingInternal();
        }

        private void OnContextMenuClosingInternal()
        {
            InContextMenu = false;
        }

        #endregion
    }
}
