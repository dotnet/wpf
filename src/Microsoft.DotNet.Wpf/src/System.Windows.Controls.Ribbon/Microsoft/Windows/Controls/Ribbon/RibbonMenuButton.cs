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
    using System.Reflection;
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
    ///   RibbonMenuButton is an ItemsControl which on clicking displays a Menu. Its Items could be either RibbonMenuItems, RibbonGallerys or Separators.
    /// </summary>
    [TemplatePart(Name = RibbonMenuButton.ResizeThumbTemplatePartName, Type = typeof(Thumb))]
    [TemplatePart(Name = RibbonMenuButton.ToggleButtonTemplatePartName, Type = typeof(RibbonToggleButton))]
    [TemplatePart(Name = RibbonMenuButton.PopupTemplatePartName, Type = typeof(Popup))]
    [TemplatePart(Name = RibbonMenuButton.SubMenuScrollViewerTemplatePartName, Type = typeof(ScrollViewer))]
    [StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(RibbonMenuItem))]
    public class RibbonMenuButton : Menu
    {
        #region Constructors

        /// <summary>
        ///   Initializes static members of the RibbonMenuButton class.  Here we override the default
        ///   style, a coerce callback, and allow tooltips to be shown for disabled commands.
        /// </summary>
        static RibbonMenuButton()
        {
            Type ownerType = typeof(RibbonMenuButton);
            DefaultStyleKeyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(ownerType));

            ItemsPanelTemplate template = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(RibbonMenuItemsPanel)));
            template.Seal();
            ItemsPanelProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(template));

            ToolTipProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(null, new CoerceValueCallback(RibbonHelper.CoerceRibbonToolTip)));
            ToolTipService.ShowOnDisabledProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(true));
            ContextMenuProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(RibbonHelper.OnContextMenuChanged, RibbonHelper.OnCoerceContextMenu));
            ContextMenuService.ShowOnDisabledProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(true));
            IsMainMenuProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(false));

            EventManager.RegisterClassHandler(ownerType, UIElement.LostMouseCaptureEvent, new MouseEventHandler(OnLostMouseCaptureThunk));
            EventManager.RegisterClassHandler(ownerType, RibbonControlService.DismissPopupEvent, new RibbonDismissPopupEventHandler(OnDismissPopupThunk));
            EventManager.RegisterClassHandler(ownerType, UIElement.GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(OnGotKeyboardFocusThunk), true);

            EventManager.RegisterClassHandler(ownerType, Mouse.PreviewMouseDownOutsideCapturedElementEvent, new MouseButtonEventHandler(OnClickThroughThunk));
            EventManager.RegisterClassHandler(ownerType, Mouse.MouseDownEvent, new MouseButtonEventHandler(OnMouseDownThunk), true);

            // pseudo-inherited properties
            IsInControlGroupProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(OnPseudoInheritedPropertyChanged), RibbonControlService.IsInControlGroupPropertyKey);

            // This should not be a focus scope since Ribbon will be a focus scope.
            FocusManager.IsFocusScopeProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(false));

            EventManager.RegisterClassHandler(ownerType, RibbonMenuButton.RibbonIsSelectedChangedEvent, new RoutedPropertyChangedEventHandler<bool>(OnRibbonIsSelectedChanged));

            KeyboardNavigation.DirectionalNavigationProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(KeyboardNavigationMode.Local));

            EventManager.RegisterClassHandler(ownerType, FrameworkElement.ContextMenuOpeningEvent, new ContextMenuEventHandler(OnContextMenuOpeningThunk), true);
            EventManager.RegisterClassHandler(ownerType, FrameworkElement.ContextMenuClosingEvent, new ContextMenuEventHandler(OnContextMenuClosingThunk), true);
            EventManager.RegisterClassHandler(ownerType, KeyTipService.ActivatingKeyTipEvent, new ActivatingKeyTipEventHandler(OnActivatingKeyTipThunk));
            EventManager.RegisterClassHandler(ownerType, KeyTipService.KeyTipAccessedEvent, new KeyTipAccessedEventHandler(OnKeyTipAccessedThunk));
        }

        #endregion

        #region RibbonControlService Properties

        /// <summary>
        ///     DependencyProperty for LargeImageSource property.
        /// </summary>
        public static readonly DependencyProperty LargeImageSourceProperty =
            RibbonControlService.LargeImageSourceProperty.AddOwner(typeof(RibbonMenuButton));

        /// <summary>
        ///     ImageSource property which is normally a 32X32 icon.
        /// </summary>
        public ImageSource LargeImageSource
        {
            get { return RibbonControlService.GetLargeImageSource(this); }
            set { RibbonControlService.SetLargeImageSource(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for SmallImageSource property.
        /// </summary>
        public static readonly DependencyProperty SmallImageSourceProperty =
            RibbonControlService.SmallImageSourceProperty.AddOwner(typeof(RibbonMenuButton));

        /// <summary>
        ///     ImageSource property which is normally a 16X16 icon.
        /// </summary>
        public ImageSource SmallImageSource
        {
            get { return RibbonControlService.GetSmallImageSource(this); }
            set { RibbonControlService.SetSmallImageSource(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for Label property.
        /// </summary>
        public static readonly DependencyProperty LabelProperty =
            RibbonControlService.LabelProperty.AddOwner(typeof(RibbonMenuButton));

        /// <summary>
        ///     Primary label text for the control.
        /// </summary>
        public string Label
        {
            get { return RibbonControlService.GetLabel(this); }
            set { RibbonControlService.SetLabel(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for ToolTipTitle property.
        /// </summary>
        public static readonly DependencyProperty ToolTipTitleProperty =
            RibbonControlService.ToolTipTitleProperty.AddOwner(typeof(RibbonMenuButton), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

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
            RibbonControlService.ToolTipDescriptionProperty.AddOwner(typeof(RibbonMenuButton), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

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
            RibbonControlService.ToolTipImageSourceProperty.AddOwner(typeof(RibbonMenuButton), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

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
            RibbonControlService.ToolTipFooterTitleProperty.AddOwner(typeof(RibbonMenuButton), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

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
            RibbonControlService.ToolTipFooterDescriptionProperty.AddOwner(typeof(RibbonMenuButton), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

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
            RibbonControlService.ToolTipFooterImageSourceProperty.AddOwner(typeof(RibbonMenuButton), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

        /// <summary>
        ///     Image source for the footer of the tooltip of the control.
        /// </summary>
        public ImageSource ToolTipFooterImageSource
        {
            get { return RibbonControlService.GetToolTipFooterImageSource(this); }
            set { RibbonControlService.SetToolTipFooterImageSource(this, value); }
        }

        #endregion

        #region PseudoInheritedProperties

        /// <summary>
        ///     DependencyProperty for ControlSizeDefinition property.
        /// </summary>
        public static readonly DependencyProperty ControlSizeDefinitionProperty =
            RibbonControlService.ControlSizeDefinitionProperty.AddOwner(typeof(RibbonMenuButton));

        /// <summary>
        ///     Size definition, including image size and visibility of label and image, for this control
        /// </summary>
        public RibbonControlSizeDefinition ControlSizeDefinition
        {
            get { return RibbonControlService.GetControlSizeDefinition(this); }
            set { RibbonControlService.SetControlSizeDefinition(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for IsInControlGroup property.
        /// </summary>
        public static readonly DependencyProperty IsInControlGroupProperty =
            RibbonControlService.IsInControlGroupProperty.AddOwner(typeof(RibbonMenuButton));

        /// <summary>
        ///     This property indicates whether the control is part of a RibbonControlGroup.
        /// </summary>
        public bool IsInControlGroup
        {
            get { return RibbonControlService.GetIsInControlGroup(this); }
            internal set { RibbonControlService.SetIsInControlGroup(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for QuickAccessToolBarControlSizeDefinition property.
        /// </summary>
        public static readonly DependencyProperty QuickAccessToolBarControlSizeDefinitionProperty =
            RibbonControlService.QuickAccessToolBarControlSizeDefinitionProperty.AddOwner(typeof(RibbonMenuButton));

        /// <summary>
        ///     Size definition to apply to this control when it's placed in a QuickAccessToolBar.
        /// </summary>
        public RibbonControlSizeDefinition QuickAccessToolBarControlSizeDefinition
        {
            get { return RibbonControlService.GetQuickAccessToolBarControlSizeDefinition(this); }
            set { RibbonControlService.SetQuickAccessToolBarControlSizeDefinition(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for IsInQuickAccessToolBar property.
        /// </summary>
        public static readonly DependencyProperty IsInQuickAccessToolBarProperty =
            RibbonControlService.IsInQuickAccessToolBarProperty.AddOwner(typeof(RibbonMenuButton));

        /// <summary>
        ///     This property indicates whether the control is part of a QuickAccessToolBar.
        /// </summary>
        public bool IsInQuickAccessToolBar
        {
            get { return RibbonControlService.GetIsInQuickAccessToolBar(this); }
            internal set { RibbonControlService.SetIsInQuickAccessToolBar(this, value); }
        }

        private static void OnPseudoInheritedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonMenuButton menuButton = (RibbonMenuButton)d;
            menuButton.TransferPseudoInheritedProperties();
        }

        internal virtual void TransferPseudoInheritedProperties()
        {
            if (_partToggleButton != null && RibbonControlService.GetIsInControlGroup(this))
            {
                RibbonControlService.SetIsInControlGroup(_partToggleButton, true);
            }
        }

        #endregion

        #region Visual States

        /// <summary>
        ///     DependencyProperty for Ribbon property.
        /// </summary>
        public static readonly DependencyProperty RibbonProperty =
            RibbonControlService.RibbonProperty.AddOwner(typeof(RibbonMenuButton));

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
            RibbonControlService.MouseOverBorderBrushProperty.AddOwner(typeof(RibbonMenuButton));

        /// <summary>
        ///     Outer border brush used in a "hover" state of the RibbonMenuButton.
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
            RibbonControlService.MouseOverBackgroundProperty.AddOwner(typeof(RibbonMenuButton));

        /// <summary>
        ///     Control background brush used in a "hover" state of the RibbonMenuButton.
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
            RibbonControlService.PressedBorderBrushProperty.AddOwner(typeof(RibbonMenuButton));

        /// <summary>
        ///     Outer border brush used in a "pressed" state of the RibbonMenuButton.
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
            RibbonControlService.PressedBackgroundProperty.AddOwner(typeof(RibbonMenuButton));

        /// <summary>
        ///     Control background brush used in a "pressed" state of the RibbonMenuButton.
        /// </summary>
        public Brush PressedBackground
        {
            get { return RibbonControlService.GetPressedBackground(this); }
            set { RibbonControlService.SetPressedBackground(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for FocusedBackground property.
        /// </summary>
        public static readonly DependencyProperty FocusedBackgroundProperty =
            RibbonControlService.FocusedBackgroundProperty.AddOwner(typeof(RibbonMenuButton));

        /// <summary>
        ///     Control background brush used in a "Focused" state of the RibbonMenuButton.
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
            RibbonControlService.FocusedBorderBrushProperty.AddOwner(typeof(RibbonMenuButton));

        /// <summary>
        ///     Control border brush used to paint a "Focused" state of the RibbonMenuButton.
        /// </summary>
        public Brush FocusedBorderBrush
        {
            get { return RibbonControlService.GetFocusedBorderBrush(this); }
            set { RibbonControlService.SetFocusedBorderBrush(this, value); }
        }

        #endregion

        #region DropDownProperties

        /// <summary>
        ///     DropDown Open event
        /// </summary>
        public event EventHandler DropDownOpened;

        /// <summary>
        ///     DropDown Close event
        /// </summary>
        public event EventHandler DropDownClosed;


        public static readonly DependencyProperty IsDropDownOpenProperty =
            ComboBox.IsDropDownOpenProperty.AddOwner(typeof(RibbonMenuButton),
                new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnIsDropDownOpenChanged), new CoerceValueCallback(CoerceIsDropDownOpen)));

        public bool IsDropDownOpen
        {
            get { return (bool)GetValue(IsDropDownOpenProperty); }
            set { SetValue(IsDropDownOpenProperty, value); }
        }

        public static readonly DependencyProperty DropDownHeightProperty = DependencyProperty.Register("DropDownHeight",
                                                                                                        typeof(double),
                                                                                                        typeof(RibbonMenuButton),
                                                                                                        new FrameworkPropertyMetadata(double.NaN,
                                                                                                            null,
                                                                                                            new CoerceValueCallback(CoerceDropDownHeightProperty)),
                                                                                                        new ValidateValueCallback(IsHeightValid));

        /// <summary>
        /// Gets or Sets the height of the Popup that is displayed when this button is invoked
        /// Applicable only when Items collection has atleast one RibbonGallery.
        /// </summary>
        public double DropDownHeight
        {
            get { return (double)GetValue(DropDownHeightProperty); }
            set { SetValue(DropDownHeightProperty, value); }
        }

        public static readonly DependencyProperty CanUserResizeVerticallyProperty = DependencyProperty.Register("CanUserResizeVertically",
                                                                                                        typeof(bool),
                                                                                                        typeof(RibbonMenuButton),
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

        public static readonly DependencyProperty CanUserResizeHorizontallyProperty = DependencyProperty.Register("CanUserResizeHorizontally",
                                                                                                        typeof(bool),
                                                                                                        typeof(RibbonMenuButton),
                                                                                                        new FrameworkPropertyMetadata(false,
                                                                                                            null,
                                                                                                            new CoerceValueCallback(CoerceCanUserResizeProperty)));

        /// <summary>
        /// Gets or sets whether user is allowed to resize this button's dropdown Popup horizontally
        /// Applicable only when Items collection has atleast one RibbonGallery
        /// and when CanUserResizeVertically is also set to true
        /// </summary>
        public bool CanUserResizeHorizontally
        {
            get { return (bool)GetValue(CanUserResizeHorizontallyProperty); }
            set { SetValue(CanUserResizeHorizontallyProperty, value); }
        }

        internal static readonly DependencyPropertyKey HasGalleryPropertyKey = DependencyProperty.RegisterReadOnly("HasGallery",
                                                                                                                typeof(bool),
                                                                                                                typeof(RibbonMenuButton),
                                                                                                                new FrameworkPropertyMetadata(false,
                                                                                                                    new PropertyChangedCallback(OnHasGalleryChanged)));

        public static readonly DependencyProperty HasGalleryProperty = HasGalleryPropertyKey.DependencyProperty;

        /// <summary>
        /// Indicates that there is atleast one RibbonGallery in Items collection.
        /// </summary>
        public bool HasGallery
        {
            get { return (bool)GetValue(HasGalleryProperty); }
            private set { SetValue(HasGalleryPropertyKey, value); }
        }

        private static readonly DependencyPropertyKey IsDropDownPositionedAbovePropertyKey = DependencyProperty.RegisterReadOnly("IsDropDownPositionedAbove",
                                                                                                                typeof(bool),
                                                                                                                typeof(RibbonMenuButton),
                                                                                                                new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty IsDropDownPositionedAboveProperty = IsDropDownPositionedAbovePropertyKey.DependencyProperty;

        /// <summary>
        /// Indicates whether the Dropdown Popup position has been flipped to top
        /// because Popup was near a screen edge.
        /// </summary>
        public bool IsDropDownPositionedAbove
        {
            get { return (bool)GetValue(IsDropDownPositionedAboveProperty); }
            private set { SetValue(IsDropDownPositionedAbovePropertyKey, value); }
        }

        #endregion DropDownProperties

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

            // RibbonComboBox containers are pre-generated.
            // When dropdown is opened for the first time ever and ItemContainerGenerator
            // is hooked up to ItemsPanel, existing containers are cleared, causing _galleryCount to be -ve.
            // Hence the check for _galleryCount > 0
            if (element is RibbonGallery && _galleryCount > 0)
            {
                HasGallery = (--_galleryCount > 0);
            }
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

#if !RIBBON_IN_FRAMEWORK
        /// <summary>
        ///     DependencyProperty for ItemContainerTemplateSelector property.
        /// </summary>
        public static readonly DependencyProperty ItemContainerTemplateSelectorProperty =
            DependencyProperty.Register(
                "ItemContainerTemplateSelector",
                typeof(ItemContainerTemplateSelector),
                typeof(RibbonMenuButton),
                new FrameworkPropertyMetadata(new DefaultItemContainerTemplateSelector()));

        /// <summary>
        ///     ItemContainerTemplateSelector property which provides the DataTemplate to be used to create an instance of the ItemContainer.
        /// </summary>
        public ItemContainerTemplateSelector ItemContainerTemplateSelector
        {
            get { return (ItemContainerTemplateSelector)GetValue(ItemContainerTemplateSelectorProperty); }
            set { SetValue(ItemContainerTemplateSelectorProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for UsesItemContainerTemplateSelector property.
        /// </summary>
        public static readonly DependencyProperty UsesItemContainerTemplateProperty =
            DependencyProperty.Register(
                "UsesItemContainerTemplate",
                typeof(bool),
                typeof(RibbonMenuButton));

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

#if RIBBON_IN_FRAMEWORK
        protected internal override bool HandlesScrolling
#else
        protected override bool HandlesScrolling
#endif
        {
            get
            {
                return true;
            }
        }

        public override void OnApplyTemplate()
        {
            // If a new template has just been generated then
            // be sure to clear any stale ItemsHost references
            if (InternalItemsHost != null && !this.IsAncestorOf(InternalItemsHost))
            {
                InternalItemsHost = null;
            }

            CoerceValue(ControlSizeDefinitionProperty);
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
            _partToggleButton = GetTemplateChild(ToggleButtonTemplatePartName) as RibbonToggleButton;
            _popup = GetTemplateChild(PopupTemplatePartName) as Popup;
            _popupRoot = null;

            TransferPseudoInheritedProperties();

            PropertyHelper.TransferProperty(this, ContextMenuProperty);   // Coerce to get a default ContextMenu if none has been specified.
            PropertyHelper.TransferProperty(this, RibbonControlService.CanAddToQuickAccessToolBarDirectlyProperty);
            TemplateApplied = true;

            ItemContainerGenerator.StatusChanged += new EventHandler(OnItemContainerGeneratorStatusChanged);

            _submenuScrollViewer = GetTemplateChild(SubMenuScrollViewerTemplatePartName) as ScrollViewer;
            if (_submenuScrollViewer != null)
            {
                KeyTipService.SetCanClipKeyTip(_submenuScrollViewer, false);
            }
        }


        protected override void OnTemplateChanged(ControlTemplate oldTemplate, ControlTemplate newTemplate)
        {
            TemplateApplied = false;
            base.OnTemplateChanged(oldTemplate, newTemplate);

            if ((oldTemplate != null) && (_partToggleButton != null))
            {
                RibbonHelper.ClearPseudoInheritedProperties(_partToggleButton);
                _partToggleButton = null;
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            HandleSpaceEnterUp = false;
            if (e.OriginalSource == _partToggleButton ||
                e.OriginalSource == this)
            {
                if (e.Key == Key.Space ||
                    e.Key == Key.Enter)
                {
                    // Do not let _partToggleButton handle space and enter.
                    // This is because it will open dropdown on space/enter
                    // down where as we need to open on key up.
                    HandleSpaceEnterUp = true;
                    e.Handled = true;
                }
            }
            base.OnPreviewKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (!IsDropDownOpen && HandleSpaceEnterUp)
            {
                if (e.OriginalSource == this ||
                    e.OriginalSource == _partToggleButton)
                {
                    if (e.Key == Key.Space ||
                        e.Key == Key.Enter)
                    {
                        IsDropDownOpen = true;
                        RibbonHelper.NavigateToNextMenuItemOrGallery(this, -1, BringIndexIntoView);
                        e.Handled = true;
                    }
                }
            }
            HandleSpaceEnterUp = false;
            base.OnKeyUp(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            RibbonHelper.HandleDropDownKeyDown(this, e,
                    delegate { return IsDropDownOpen; },
                    delegate(bool value) { IsDropDownOpen = value; },
                    RetainFocusOnEscape ? _partToggleButton : null,
                    _popup.TryGetChild());

            // Do not call base because base's logic interferes
            // with that of RibbonMenuButton.
            // base.OnKeyDown(e);

            if (e.Handled)
                return;

            OnNavigationKeyDown(e);
        }

        internal void OnNavigationKeyDown(KeyEventArgs e)
        {
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
            bool handled = false;
            DependencyObject focusedElement = Keyboard.FocusedElement as DependencyObject;
            switch (e.Key)
            {
                case Key.Home:
                    if (IsDropDownOpen)
                    {
                        handled = RibbonHelper.NavigateToNextMenuItemOrGallery(this, -1, BringIndexIntoView);
                    }
                    break;
                case Key.End:
                    if (IsDropDownOpen)
                    {
                        handled = RibbonHelper.NavigateToPreviousMenuItemOrGallery(this, -1, BringIndexIntoView);
                    }
                    break;
                case Key.Down:
                    if (IsDropDownOpen)
                    {
                        if (itemNavigateFromCurrentFocused)
                        {
                            // event could have bubbled up from MenuItem
                            // when it could not navigate to the next item (for eg. gallery)
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
                    if (IsDropDownOpen)
                    {
                        if (itemNavigateFromCurrentFocused)
                        {
                            // event could have bubbled up from MenuItem
                            // when it could not navigate to the previous item (for eg. gallery)
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
                    if (IsDropDownOpen &&
                        (IsFocused || (focusedElement != null && TreeHelper.IsVisualAncestorOf(this, focusedElement))))
                    {
                        if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                        {
                            handled = RibbonHelper.NavigateToPreviousMenuItemOrGallery(this, Items.Count, BringIndexIntoView);
                        }
                        else
                        {
                            handled = RibbonHelper.NavigateToNextMenuItemOrGallery(this, -1, BringIndexIntoView);
                        }
                    }
                    break;
            }

            e.Handled = handled;
        }

        #endregion

        #region Selection

        /// <summary>
        /// Event raised by either RibbonMenuItem or RibbonGallery to indicate to its parent that it is currently selected.
        ///
        ///     We use the Ribbon prefix to disambiguate this property from MenuBase.IsSelectedChangedEvent.
        /// </summary>
        internal static readonly RoutedEvent RibbonIsSelectedChangedEvent = EventManager.RegisterRoutedEvent(
                                                                    "RibbonIsSelectedChanged", RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<bool>), typeof(RibbonMenuButton));

        /// <summary>
        ///     Called when RibbonIsSelected changed on this element or any descendant.
        /// </summary>
        private static void OnRibbonIsSelectedChanged(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            FrameworkElement selectionItem = e.OriginalSource as FrameworkElement;

            if (selectionItem != null)
            {
                RibbonMenuButton menu = (RibbonMenuButton)sender;

                // If the selected item is a child of ours, make it the current selection.
                // If the selection changes from a menu item with its submenu
                // open to another then close the old selection's submenu.
                if (e.NewValue)
                {
                    ItemsControl parentItemsControl = ItemsControl.ItemsControlFromItemContainer(selectionItem);
                    if (menu == parentItemsControl)
                    {
                        menu.RibbonCurrentSelection = selectionItem;
                    }
                }
                else
                {
                    // As in MenuItem.OnRibbonIsSelectedChanged, if the item is deselected
                    // and it's our current selection, set RibbonCurrentSelection to null.
                    if (menu.RibbonCurrentSelection == selectionItem)
                    {
                        menu.RibbonCurrentSelection = null;
                    }
                }

                e.Handled = true;
            }
        }

        /// <summary>
        ///     Currently selected item in this menu or submenu.
        ///
        ///     We use the Ribbon prefix to disambiguate this property from MenuBase.CurrentSelection.
        /// </summary>
        /// <value></value>
        internal FrameworkElement RibbonCurrentSelection
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

        #region UI Automation

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new RibbonMenuButtonAutomationPeer(this);
        }

        #endregion

        #region Dropdown Resizing

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
                /* IsDropDownPositionedLeft */ false,
                IsDropDownPositionedAbove,
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
                /* IsDropDownPositionedLeft */ false,
                IsDropDownPositionedAbove,
                _screenBounds,
                _popupRoot,
                horizontalDelta,
                verticalDelta);
        }

        #endregion

        #region Private Methods

        internal void BringIndexIntoView(int index)
        {
            if (_itemsHost != null)
            {
                _itemsHost.BringIndexIntoViewInternal(index);
            }
        }

        private void OnDropDownOpened(EventArgs e)
        {
            if (DropDownOpened != null)
            {
                DropDownOpened(this, e);
            }
        }

        private void OnDropDownClosed(EventArgs e)
        {
            if (DropDownClosed != null)
            {
                DropDownClosed(this, e);
            }
        }

        private static void OnIsDropDownOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonMenuButton menuButton = (RibbonMenuButton)d;
            menuButton.OnIsDropDownOpenChanged(e);
        }

        internal virtual void OnIsDropDownOpenChanged(DependencyPropertyChangedEventArgs e)
        {
            // If the drop down is closed due to
            // an action of context menu or if the
            // ContextMenu for a parent (Ribbon)
            // was opened by right clicking this
            // RibbonMenuButton (RibbonApplicationMenu)
            // then ContextMenuClosed event is never raised.
            // Hence reset the flag.
            InContextMenu = false;

            RibbonHelper.HandleIsDropDownChanged(this,
                    delegate() { return IsDropDownOpen; },
                    this,
                    this);

            if ((bool)e.NewValue)
            {
                RetainFocusOnEscape = RibbonHelper.IsKeyboardMostRecentInputDevice();
                BaseOnIsKeyboardFocusWithin();
                OnDropDownOpened(EventArgs.Empty);

                // Clear local values
                // so that when DropDown opens it shows in it original size and PlacementMode
                RibbonDropDownHelper.ClearLocalValues(_itemsPresenter, _popup);

                // DropDownHeight refers to the initial Height of the popup
                // The size of the popup can change dynamically by resizing.
                if (_itemsPresenter != null && HasGallery)
                {
                    _itemsPresenter.Height = DropDownHeight;
                }

                // IsDropDownPositionedAbove is updated asynchronously.
                // As a result the resize thumb would change position and we could see a visual artifact of this change after Popup opens.
                Dispatcher.BeginInvoke(new DispatcherOperationCallback(UpdateDropDownPosition), DispatcherPriority.Loaded, new object[] { null });
            }
            else
            {
                if (Mouse.Captured == this)
                {
                    // If the capture is still on menubutton even after
                    // closing the drop down, then release the capture.
                    // This usually happens due to the interference of
                    // MenuMode.
                    Mouse.Capture(null);
                }

                RibbonCurrentSelection = null;
                OnDropDownClosed(EventArgs.Empty);
            }

            // Raise UI Automation Events
            RibbonMenuButtonAutomationPeer peer = UIElementAutomationPeer.FromElement(this) as RibbonMenuButtonAutomationPeer;
            if (peer != null)
            {
                peer.RaiseExpandCollapseAutomationEvent(!(bool)e.OldValue, !(bool)e.NewValue);
            }
        }

        private static object CoerceIsDropDownOpen(DependencyObject d, object baseValue)
        {
            RibbonMenuButton menuButton = (RibbonMenuButton)d;
            if ((bool)baseValue)
            {
                if (!menuButton.IsLoaded)
                {
                    menuButton.RegisterToOpenOnLoad();
                    return false;
                }

                if (!menuButton.IsVisible)
                {
                    menuButton.RegisterOpenOnVisible();
                    return false;
                }
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

        private static bool IsHeightValid(object value)
        {
            double v = (double)value;
            return (DoubleUtil.IsNaN(v)) || (v >= 0.0d && !Double.IsPositiveInfinity(v));
        }

        private static void OnHasGalleryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(CanUserResizeHorizontallyProperty);
            d.CoerceValue(CanUserResizeVerticallyProperty);
            d.CoerceValue(DropDownHeightProperty);

            RibbonMenuButton menuButton = (RibbonMenuButton)d;
            if (menuButton.IsDropDownOpen)
            {
                // First time the dropdown opens, HasGallery is always false
                // because item container generation never happened yet and hence
                // it ignores DropDownHeight. Hence reuse DropDownHeight when HasGallery
                // value changes.
                RibbonHelper.SetDropDownHeight(menuButton._itemsPresenter, (bool)e.NewValue, menuButton.DropDownHeight);
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

            RibbonHelper.InvalidateScrollBarVisibility(menuButton._submenuScrollViewer);
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
            RibbonMenuButton menuButton = (RibbonMenuButton)d;

            if ((menuButton.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated) &&
                !menuButton.HasGallery)
            {
                return double.NaN;
            }
            return baseValue;
        }

        private static object CoerceCanUserResizeProperty(DependencyObject d, object baseValue)
        {
            RibbonMenuButton menuButton = (RibbonMenuButton)d;

            if ((menuButton.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated) &&
                !menuButton.HasGallery)
            {
                return false;
            }
            return baseValue;
        }

        private object UpdateDropDownPosition(object arg)
        {
            if (!IsDropDownOpen)
            {
                return null;
            }

            UIElement popupChild = _popup.TryGetChild();
            if (popupChild != null)
            {
                Point targetTopLeftCorner;
                if (_popup.PlacementTarget != null)
                {
                    targetTopLeftCorner = _popup.PlacementTarget.PointToScreen(new Point());
                }
                else
                {
                    targetTopLeftCorner = this.PointToScreen(new Point());
                }
                Point popupBottomRightCorner = popupChild.PointToScreen(new Point(popupChild.RenderSize.Width, popupChild.RenderSize.Height));

                IsDropDownPositionedAbove = DoubleUtil.LessThanOrClose(popupBottomRightCorner.Y, targetTopLeftCorner.Y);
                if (IsDropDownPositionedAbove)
                {
                    // Anchor the popup so that its position doesnt change when resizing.
                    _popup.Placement = PlacementMode.Top;
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

        #endregion

        #region Internal Properties

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

        internal Popup Popup
        {
            get { return _popup; }
        }

        internal ScrollViewer SubMenuScrollViewer
        {
            get { return _submenuScrollViewer; }
        }

        #endregion

        #region Private Data

        private const string ResizeThumbTemplatePartName = "PART_ResizeThumb";
        private const string ItemsPresenterTemplatePartName = "ItemsPresenter";
        private const string PopupTemplatePartName = "PART_Popup";
        // internal because RibbonGallery's filter must refer to this as well.
        internal const string ToggleButtonTemplatePartName = "PART_ToggleButton";
        private const string SubMenuScrollViewerTemplatePartName = "PART_SubMenuScrollViewer";

        private ScrollViewer _submenuScrollViewer;
        private RibbonToggleButton _partToggleButton;
        private Thumb _resizeThumb;
        private FrameworkElement _itemsPresenter;
        private RibbonMenuItemsPanel _itemsHost;
        private Popup _popup;
        private FrameworkElement _ribbonCurrentSelection;     // could be a RibbonMenuItem or RibbonGallery
        private BitVector32 _bits = new BitVector32(0);
        private Rect _screenBounds;
        private UIElement _popupRoot;
        private int _galleryCount;

        private enum Bits
        {
            PseudoIsKeyboardFocusWithin = 0x01,
            RetainFocusOnEscape = 0x02,
            InContextMenu = 0x04,
            TemplateApplied = 0x08,
            HandleSpaceEnterUp = 0x10
        }

        private bool PseudoIsKeyboardFocusWithin
        {
            get { return _bits[(int)Bits.PseudoIsKeyboardFocusWithin]; }
            set { _bits[(int)Bits.PseudoIsKeyboardFocusWithin] = value; }
        }

        internal bool RetainFocusOnEscape
        {
            get { return _bits[(int)Bits.RetainFocusOnEscape]; }
            set { _bits[(int)Bits.RetainFocusOnEscape] = value; }
        }

        private bool InContextMenu
        {
            get { return _bits[(int)Bits.InContextMenu]; }
            set { _bits[(int)Bits.InContextMenu] = value; }
        }

        internal bool TemplateApplied
        {
            get { return _bits[(int)Bits.TemplateApplied]; }
            set { _bits[(int)Bits.TemplateApplied] = value; }
        }

        private bool HandleSpaceEnterUp
        {
            get { return _bits[(int)Bits.HandleSpaceEnterUp]; }
            set { _bits[(int)Bits.HandleSpaceEnterUp] = value; }
        }

        #endregion

        #region DismissPopup

        /// <summary>
        /// Expose toggle button to the derived classes
        /// </summary>
        internal RibbonToggleButton PartToggleButton
        {
            get
            {
                return _partToggleButton;
            }
        }

        /// <summary>
        /// base exits MenuMode on any Mouse clicks. We want to prevent that.
        /// </summary>
        /// <param name="e"></param>
        protected override void HandleMouseButton(MouseButtonEventArgs e)
        {
            FrameworkElement source = e.OriginalSource as FrameworkElement;
            if (source != null && (source == this || source.TemplatedParent == this))
            {
                e.Handled = true;
            }
        }

        private static void OnLostMouseCaptureThunk(object sender, MouseEventArgs e)
        {
            RibbonMenuButton menuButton = (RibbonMenuButton)sender;
            menuButton.OnLostMouseCaptureThunk(e);
        }

        private void OnLostMouseCaptureThunk(MouseEventArgs e)
        {
            RibbonHelper.HandleLostMouseCapture(this,
                    e,
                    delegate() { return (IsDropDownOpen && !InContextMenu); },
                    delegate(bool value) { IsDropDownOpen = value; },
                    this,
                    this);
        }

        private static void OnDismissPopupThunk(object sender, RibbonDismissPopupEventArgs e)
        {
            RibbonMenuButton ribbonMenuButton = (RibbonMenuButton)sender;
            ribbonMenuButton.OnDismissPopup(e);
        }

        protected virtual void OnDismissPopup(RibbonDismissPopupEventArgs e)
        {
            UIElement popupChild = _popup.TryGetChild();
            RibbonHelper.HandleDismissPopup(e,
                delegate(bool value) { IsDropDownOpen = value; },
                delegate(DependencyObject d) { return d == _partToggleButton; },
                popupChild,
                this);
        }

        private static void OnClickThroughThunk(object sender, MouseButtonEventArgs e)
        {
            RibbonMenuButton ribbonMenuButton = (RibbonMenuButton)sender;
            ribbonMenuButton.OnClickThrough(e);
        }

        private void OnClickThrough(MouseButtonEventArgs e)
        {
            UIElement popupChild = _popup.TryGetChild();
            RibbonHelper.HandleClickThrough(this, e, popupChild);
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            if (IsDropDownOpen)
            {
                // Close the drop down if the click happened on the toggle button.
                if (RibbonHelper.IsMousePhysicallyOver(this))
                {
#if IN_RIBBON_GALLERY
                    // Skip this logic for the InRibbonGallery case.
                    if (this is InRibbonGallery)
                        return;
#endif
                    IsDropDownOpen = false;
                    e.Handled = true;
                }
            }
        }

        protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
        {
            // If IsKeyboardFocusWithin has become true, then do not
            // call base.OnIsKeyboardFocusWithinChanged right away.
            // Defer the bases call until DropDown gets opened or
            // one of the descendants get focus.
            if (!IsKeyboardFocusWithin)
            {
                PseudoIsKeyboardFocusWithin = false;
                base.OnIsKeyboardFocusWithinChanged(e);

                // DevDiv:650335:  Not calling base.OnIsKeyboardFocusWithingChanged() when IsKeyboardFocusWithin=true
                // can result in MenuBase having PushedMenuMode without setting IsMenuMode=true.  Since we no longer
                // have keyboard focus, IsMenuMode should now be false, but since it may not have ever been set to true,
                // MenuBase may not have called PopMenuMode().  Manually call that now, if MenuMode has been pushed.
                Type type = typeof(MenuBase);
                Debug.Assert(GetType().IsSubclassOf(type)); // RibbonMenuButton is a subclass of MenuBase

                // If HasPushedMenuMode=true...
                PropertyInfo property = type.GetProperty("HasPushedMenuMode", BindingFlags.NonPublic | BindingFlags.Instance);
                Debug.Assert(property != null);
                if (property != null && (bool)property.GetValue(this, null) == true)
                {
                    // ...call PopMenuMode.
                    MethodInfo method = type.GetMethod("PopMenuMode", BindingFlags.NonPublic | BindingFlags.Instance);
                    Debug.Assert(method != null);
                    if (method != null)
                    {
                        method.Invoke(this, null);
                    }
                }
            }
        }

        private static void OnGotKeyboardFocusThunk(object sender, KeyboardFocusChangedEventArgs e)
        {
            RibbonMenuButton menuButton = (RibbonMenuButton)sender;
            menuButton.OnGotKeyboardFocusThunk(e);
        }

        private void OnGotKeyboardFocusThunk(KeyboardFocusChangedEventArgs e)
        {
            // Call base.OnIsKeyboardFocusWithinChanged only if the new focus
            // is not a direct descendant of menu button.
            // It's possible to get here when disabled, which can lead to a
            // focus war resulting in StackOverflow. Don't start the war.
            if (e.OriginalSource != this &&
                this.IsEnabled &&
                !TreeHelper.IsVisualAncestorOf(this, e.OriginalSource as DependencyObject))
            {
                BaseOnIsKeyboardFocusWithin();
            }
        }

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnGotKeyboardFocus(e);

            FrameworkElement ribbonCurrentSelection = RibbonCurrentSelection;
            if (ribbonCurrentSelection != null &&
                IsDropDownOpen)
            {
                // If the drop down is open and the ribbonCurrentSelection is valid
                // but still popup doesnt have focus within,
                // then focus the current selection.
                // It's possible to get here when disabled, or when an app explicitly
                // moves focus in a GotKeyboardFocus handler called earlier in the
                // bubbling route.  Either of these can lead to a
                // focus war resulting in StackOverflow. Don't start the war
                UIElement popupChild = _popup.TryGetChild();
                if (popupChild != null &&
                    this.IsEnabled &&
                    this.IsKeyboardFocusWithin &&
                    !popupChild.IsKeyboardFocusWithin)
                {
                    ribbonCurrentSelection.Focus();
                }
            }
        }

        /// <summary>
        ///     Helper method to call the base.OnIsKeyboardFocusWithinChanged
        ///     method the first time after deferal.
        /// </summary>
        private void BaseOnIsKeyboardFocusWithin()
        {
            if (!PseudoIsKeyboardFocusWithin && IsKeyboardFocusWithin)
            {
                PseudoIsKeyboardFocusWithin = true;
                DependencyPropertyChangedEventArgs eventArgs = new DependencyPropertyChangedEventArgs(IsKeyboardFocusWithinProperty, false, true);
                base.OnIsKeyboardFocusWithinChanged(eventArgs);
            }
        }

        private static void OnMouseDownThunk(object sender, MouseButtonEventArgs e)
        {
            ((RibbonMenuButton)(sender)).OnAnyMouseDown(e);
        }

        internal virtual void OnAnyMouseDown(MouseButtonEventArgs e)
        {
            RetainFocusOnEscape = false;
        }

        #endregion DismissPopup

        #region Context Menu

        private static void OnContextMenuOpeningThunk(object sender, ContextMenuEventArgs e)
        {
            ((RibbonMenuButton)sender).OnContextMenuOpeningInternal();
        }

        private void OnContextMenuOpeningInternal()
        {
            InContextMenu = true;
        }

        private static void OnContextMenuClosingThunk(object sender, ContextMenuEventArgs e)
        {
            ((RibbonMenuButton)sender).OnContextMenuClosingInternal();
        }

        private void OnContextMenuClosingInternal()
        {
            InContextMenu = false;
            if (IsDropDownOpen)
            {
                RibbonHelper.AsyncSetFocusAndCapture(this,
                    delegate() { return IsDropDownOpen; },
                    this,
                    this);
            }
        }

        #endregion

        #region QAT

        /// <summary>
        ///   DependencyProperty for QuickAccessToolBarId property.
        /// </summary>
        public static readonly DependencyProperty QuickAccessToolBarIdProperty =
            RibbonControlService.QuickAccessToolBarIdProperty.AddOwner(typeof(RibbonMenuButton));

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
            RibbonControlService.CanAddToQuickAccessToolBarDirectlyProperty.AddOwner(typeof(RibbonMenuButton),
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

        /// <summary>
        ///     DependencyProperty for KeyTip property.
        /// </summary>
        public static readonly DependencyProperty KeyTipProperty =
            KeyTipService.KeyTipProperty.AddOwner(typeof(RibbonMenuButton));

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
            ((RibbonMenuButton)sender).OnActivatingKeyTip(e);
        }

        protected virtual void OnActivatingKeyTip(ActivatingKeyTipEventArgs e)
        {
            if (e.OriginalSource == this)
            {
                RibbonHelper.SetKeyTipPlacementForButton(this, e, _partToggleButton == null ? null : _partToggleButton.Image);
            }
        }

        private static void OnKeyTipAccessedThunk(object sender, KeyTipAccessedEventArgs e)
        {
            ((RibbonMenuButton)sender).OnKeyTipAccessed(e);
        }

        protected virtual void OnKeyTipAccessed(KeyTipAccessedEventArgs e)
        {
            if (e.OriginalSource == this)
            {
                // Open the dropdown of parent group and self.
                RibbonHelper.OpenParentRibbonGroupDropDownSync(this, TemplateApplied);
                IsDropDownOpen = true;
                RibbonHelper.NavigateToNextMenuItemOrGallery(this, -1, BringIndexIntoView);
                UIElement popupChild = _popup.TryGetChild();
                if (popupChild != null)
                {
                    KeyTipService.SetIsKeyTipScope(popupChild, true);
                    e.TargetKeyTipScope = popupChild;
                }
                e.Handled = true;
            }
        }

        #endregion
    }
}
