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
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media;

    #endregion
    
    /// <summary>
    /// A variation of RibbonMenuItem which shows Checked state differently. 
    /// Its Submenu can be opened or closed independent of IsCheckable.
    /// </summary>
    [TemplatePart(Name = RibbonSplitMenuItem.ArrowButtonTemplatePart, Type = typeof(RibbonToggleButton))]
    [TemplatePart(Name = RibbonSplitMenuItem.HeaderButtonTemplatePart, Type = typeof(RibbonButton))]
    [TemplatePart(Name = RibbonMenuItem.SideBarBorderTemplatePartName, Type = typeof(Border))]
    public class RibbonSplitMenuItem : RibbonMenuItem
    {
        #region Constructor

        static RibbonSplitMenuItem()
        {
            Type ownerType = typeof(RibbonSplitMenuItem);
            DefaultStyleKeyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(ownerType));
            BorderThicknessProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(new Thickness(), new PropertyChangedCallback(OnBorderThicknessChanged)));
            IsCheckedProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(new PropertyChangedCallback(OnIsCheckedChanged)));
        }

        #endregion

        #region ToolTip Properties

        /// <summary>
        ///     DependencyProperty for DropDownToolTipTitle property.
        /// </summary>
        public static readonly DependencyProperty DropDownToolTipTitleProperty =
            RibbonSplitButton.DropDownToolTipTitleProperty.AddOwner(
            typeof(RibbonSplitMenuItem),
            new FrameworkPropertyMetadata(new PropertyChangedCallback(OnDropDownToolTipPropertyChanged)));

        /// <summary>
        ///     Title text for the tooltip on the DropDown portion of the RibbonSplitMenuItem
        /// </summary>
        public string DropDownToolTipTitle
        {
            get { return (string)GetValue(DropDownToolTipTitleProperty); }
            set { SetValue(DropDownToolTipTitleProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for DropDownToolTipDescription property.
        /// </summary>
        public static readonly DependencyProperty DropDownToolTipDescriptionProperty =
            RibbonSplitButton.DropDownToolTipDescriptionProperty.AddOwner(
            typeof(RibbonSplitMenuItem),
            new FrameworkPropertyMetadata(new PropertyChangedCallback(OnDropDownToolTipPropertyChanged)));

        /// <summary>
        ///     Description text for the tooltip on the DropDown portion of the RibbonSplitMenuItem
        /// </summary>
        public string DropDownToolTipDescription
        {
            get { return (string)GetValue(DropDownToolTipDescriptionProperty); }
            set { SetValue(DropDownToolTipDescriptionProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for DropDownToolTipImageSource property.
        /// </summary>
        public static readonly DependencyProperty DropDownToolTipImageSourceProperty =
            RibbonSplitButton.DropDownToolTipImageSourceProperty.AddOwner(
            typeof(RibbonSplitMenuItem),
            new FrameworkPropertyMetadata(new PropertyChangedCallback(OnDropDownToolTipPropertyChanged)));

        /// <summary>
        ///     Image source for the tooltip on the DropDown portion of the RibbonSplitMenuItem
        /// </summary>
        public ImageSource DropDownToolTipImageSource
        {
            get { return (ImageSource)GetValue(DropDownToolTipImageSourceProperty); }
            set { SetValue(DropDownToolTipImageSourceProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for DropDownToolTipFooterTitle property.
        /// </summary>
        public static readonly DependencyProperty DropDownToolTipFooterTitleProperty =
            RibbonSplitButton.DropDownToolTipFooterTitleProperty.AddOwner(
            typeof(RibbonSplitMenuItem),
            new FrameworkPropertyMetadata(new PropertyChangedCallback(OnDropDownToolTipPropertyChanged)));

        /// <summary>
        ///     Title text for the footer of tooltip on the DropDown portion of the RibbonSplitMenuItem
        /// </summary>
        public string DropDownToolTipFooterTitle
        {
            get { return (string)GetValue(DropDownToolTipFooterTitleProperty); }
            set { SetValue(DropDownToolTipFooterTitleProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for DropDownToolTipFooterDescription property.
        /// </summary>
        public static readonly DependencyProperty DropDownToolTipFooterDescriptionProperty =
            RibbonSplitButton.DropDownToolTipFooterDescriptionProperty.AddOwner(
            typeof(RibbonSplitMenuItem),
            new FrameworkPropertyMetadata(new PropertyChangedCallback(OnDropDownToolTipPropertyChanged)));

        /// <summary>
        ///     Description text for the footer of the tooltip on the DropDown portion of the RibbonSplitMenuItem
        /// </summary>
        public string DropDownToolTipFooterDescription
        {
            get { return (string)GetValue(DropDownToolTipFooterDescriptionProperty); }
            set { SetValue(DropDownToolTipFooterDescriptionProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for DropDownToolTipFooterImageSource property.
        /// </summary>
        public static readonly DependencyProperty DropDownToolTipFooterImageSourceProperty =
            RibbonSplitButton.DropDownToolTipFooterImageSourceProperty.AddOwner(
            typeof(RibbonSplitMenuItem),
            new FrameworkPropertyMetadata(new PropertyChangedCallback(OnDropDownToolTipPropertyChanged)));

        /// <summary>
        ///     Image source for the footer of the tooltip on the DropDown portion of the RibbonSplitMenuItem
        /// </summary>
        public ImageSource DropDownToolTipFooterImageSource
        {
            get { return (ImageSource)GetValue(DropDownToolTipFooterImageSourceProperty); }
            set { SetValue(DropDownToolTipFooterImageSourceProperty, value); }
        }

        private static void OnDropDownToolTipPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonSplitMenuItem splitMenuItem = (RibbonSplitMenuItem)d;
            if (splitMenuItem._partArrowButton != null)
            {
                splitMenuItem._partArrowButton.CoerceValue(FrameworkElement.ToolTipProperty);
            }
        }

        #endregion

        #region Protected Methods

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_headerButton != null)
            {
                _headerButton.Click -= new RoutedEventHandler(OnHeaderClicked);
            }
            _headerButton = GetTemplateChild(HeaderButtonTemplatePart) as ButtonBase;

            if (_headerButton != null)
            {
                _headerButton.Click += new RoutedEventHandler(OnHeaderClicked);
            }

            _partArrowButton = GetTemplateChild(ArrowButtonTemplatePart) as RibbonToggleButton;
            _highlightLeftBorder = GetTemplateChild("HighlightLeftBorder") as Border;
            _highlightRightBorder = GetTemplateChild("HighlightRightBorder") as Border;

            SetIsPressedBinding();
            SetBorderThickness();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (HasItems && e.Key == Key.Enter)
            {
                // Fire Command on Enter, not open Submenu
                OnClick();
                e.Handled = true;
                return;
            }

            base.OnKeyDown(e);
        }

        private void OnHeaderClicked(object sender, RoutedEventArgs e)
        {
            OnClick();
        }

        private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonSplitMenuItem splitMenuItem = (RibbonSplitMenuItem)d;

            RibbonToggleButton toggleButton = splitMenuItem._headerButton as RibbonToggleButton;
            if (toggleButton != null)
            {
                toggleButton.IsChecked = splitMenuItem.IsChecked;
            }
        }

        // UIElement.IsEnabledCore's getter simply returns true (e.g. RibbonSplitButton follows this path), but MenuItem
        // overrides that and returns the value of the Command's CanExecute.  For RibbonSplitMenuItem, however, we don't
        // want the Command's CanExecute state to affect the IsEnabled state of the entire control. PART_HeaderButton
        // handles the Command and reflects its own enabled/disabled state accordingly.
        protected override bool IsEnabledCore
        {
            get
            {
                return true;
            }
        }
        
        #endregion

        #region QAT

        /// <summary>
        ///     The DependencyProperty for the HeaderQuickAccessToolBarId property.
        ///     Flags:              None
        ///     Default Value:      null
        /// </summary>
        public static readonly DependencyProperty HeaderQuickAccessToolBarIdProperty =
                RibbonSplitButton.HeaderQuickAccessToolBarIdProperty.AddOwner(typeof(RibbonSplitMenuItem));

        /// <summary>
        /// Identifier for the Header part of the RibbonSplitButton when in QuickAccessToolBar.
        /// Set this property to allow Header to be added to QuickAccessToolBar by itself.
        /// </summary>
        public object HeaderQuickAccessToolBarId
        {
            get { return GetValue(HeaderQuickAccessToolBarIdProperty); }
            set { SetValue(HeaderQuickAccessToolBarIdProperty, value); }
        }

        #endregion QAT

        #region Private Methods

        internal override void OnDismissPopup(RibbonDismissPopupEventArgs e)
        {
            if (e.OriginalSource == _partArrowButton)
            {
                // Clicking on Arrow should not dismiss the parent popup
                e.Handled = true;
                return;
            }
            base.OnDismissPopup(e);
        }

        private void SetIsPressedBinding()
        {
            if (_partHeaderButton != null)
            {
                // Clear any existing Binding
                BindingOperations.ClearBinding(this, IsPressedInternalProperty);
            }

            _partHeaderButton = GetTemplateChild(HeaderButtonTemplatePart) as ButtonBase;

            // Clicking on HeaderToggleButton eats up MouseDown input, hence MenuItem.IsPressed doesnt get updated.
            // Bind to Button.IsPressed and set MenuItem.IsPressed manually.
            if (_partHeaderButton != null)
            {
                Binding binding = new Binding("IsPressed");
                binding.Source = _partHeaderButton;
                this.SetBinding(IsPressedInternalProperty, binding);
            }
        }

        private static void OnIsPressedInternalChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            RibbonSplitMenuItem splitMenuItem = (RibbonSplitMenuItem)sender;
            splitMenuItem.IsPressed = splitMenuItem.IsPressedInternal;
        }

        private static void OnBorderThicknessChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            RibbonSplitMenuItem splitMenuItem = (RibbonSplitMenuItem)sender;
            splitMenuItem.SetBorderThickness();
        }

        private void SetBorderThickness()
        {
            if (_highlightLeftBorder != null)
            {
                // Right = 0.0
                _highlightLeftBorder.BorderThickness = new Thickness(BorderThickness.Left, BorderThickness.Top, 0.0, BorderThickness.Bottom);
            }
            if (_highlightRightBorder != null)
            {
                // Left = 0.0
                _highlightRightBorder.BorderThickness = new Thickness(0.0, BorderThickness.Top, BorderThickness.Right, BorderThickness.Bottom);
            }
        }

        #endregion

        #region Internal Properties

        internal override bool CanOpenSubMenu
        {
            get
            {
                return HasItems;
            }
        }

        /// <summary>
        /// A private read/write property which can bind to ToggleButton's IsPressedProperty.
        /// </summary>
        private static readonly DependencyProperty IsPressedInternalProperty = DependencyProperty.Register("IsPressedInternal",
                                                                                                            typeof(bool),
                                                                                                            typeof(RibbonSplitMenuItem),
                                                                                                            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnIsPressedInternalChanged)));

        private bool IsPressedInternal
        {
            get { return (bool)GetValue(IsPressedInternalProperty); }
            set { SetValue(IsPressedInternalProperty, value); }
        }

        internal RibbonToggleButton ArrowToggleButton
        {
            get
            {
                return _partArrowButton;
            }
        }

        internal ButtonBase HeaderButton
        {
            get
            {
                return _partHeaderButton;
            }
        }

        #endregion

        #region Private Data
        private const string HeaderButtonTemplatePart = "PART_HeaderButton";
        private const string ArrowButtonTemplatePart = "PART_ArrowToggleButton";

        ButtonBase _headerButton;
        RibbonToggleButton _partArrowButton;
        ButtonBase _partHeaderButton;
        Border _highlightLeftBorder, _highlightRightBorder;
        #endregion

        #region KeyTips

        public string HeaderKeyTip
        {
            get { return (string)GetValue(HeaderKeyTipProperty); }
            set { SetValue(HeaderKeyTipProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HeaderKeyTip.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HeaderKeyTipProperty =
            RibbonSplitButton.HeaderKeyTipProperty.AddOwner(typeof(RibbonSplitMenuItem));


        protected override void OnActivatingKeyTip(ActivatingKeyTipEventArgs e)
        {
            if (e.OriginalSource == this)
            {
                if (!CanOpenSubMenu)
                {
                    e.KeyTipVisibility = Visibility.Collapsed;
                }
                else
                {
                    e.KeyTipHorizontalPlacement = KeyTipHorizontalPlacement.KeyTipCenterAtTargetCenter;
                    e.KeyTipVerticalPlacement = KeyTipVerticalPlacement.KeyTipTopAtTargetCenter;
                    e.KeyTipHorizontalOffset = e.KeyTipVerticalOffset = 0;
                    e.PlacementTarget = ArrowToggleButton;
                }
            }
            else if (e.OriginalSource == HeaderButton)
            {
                e.KeyTipHorizontalPlacement = KeyTipHorizontalPlacement.KeyTipLeftAtTargetCenter;
                e.KeyTipVerticalPlacement = KeyTipVerticalPlacement.KeyTipTopAtTargetCenter;
                e.KeyTipHorizontalOffset = e.KeyTipVerticalOffset = 0;
                e.PlacementTarget = SideBarBorder;
            }
        }

        protected override void OnKeyTipAccessed(KeyTipAccessedEventArgs e)
        {
            if (e.OriginalSource == this)
            {
                if (CanOpenSubMenu)
                {
                    FocusOrSelect();
                    IsSubmenuOpen = true;
                    RibbonHelper.NavigateToNextMenuItemOrGallery(this, -1, BringIndexIntoView);
                    UIElement popupChild = Popup.TryGetChild();
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
