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
    using System.Windows.Media;
#if RIBBON_IN_FRAMEWORK
    using Microsoft.Windows.Controls;
#else
    using Microsoft.Windows.Automation.Peers;
#endif

    #endregion

    /// <summary>
    ///   A SplitButton which can be placed in the Ribbon.
    ///   This button can be Checked, fire a Command and have a Submenu hosting RibbonMenuItems and RibbonGallery. 
    /// </summary>
    [TemplatePart(Name = RibbonSplitButton.HeaderButtonTemplatePartName, Type = typeof(ButtonBase))]
    [TemplatePart(Name = RibbonMenuButton.ToggleButtonTemplatePartName, Type = typeof(RibbonToggleButton))]
    public class RibbonSplitButton : RibbonMenuButton , ICommandSource
    {
        #region Constructors

        /// <summary>
        ///   Initializes static members of the RibbonSplitButton class.  It also overrides the default style.
        /// </summary>
        static RibbonSplitButton()
        {
            Type ownerType = typeof(RibbonSplitButton);
            DefaultStyleKeyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(ownerType));
            BorderThicknessProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(new Thickness(), new PropertyChangedCallback(OnBorderThicknessChanged)));
            ControlSizeDefinitionProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnBorderThicknessChanged)));
            IsInControlGroupProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnBorderThicknessChanged)), RibbonControlService.IsInControlGroupPropertyKey);
        }

        #endregion

        #region Overrides

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_headerButton != null)
            {
                _headerButton.Click -= new RoutedEventHandler(OnHeaderClicked);
            }
            _headerButton = GetTemplateChild(RibbonSplitButton.HeaderButtonTemplatePartName) as ButtonBase;

            if (_headerButton != null)
            {
                _headerButton.Click += new RoutedEventHandler(OnHeaderClicked);
            }

            // Set BorderThickess on TemplateParts
            SetBorderThickess();

            PropertyHelper.TransferProperty(this, ContextMenuProperty);   // Coerce to get a default ContextMenu if none has been specified.
            PropertyHelper.TransferProperty(this, RibbonControlService.CanAddToQuickAccessToolBarDirectlyProperty);

            _toggleButton = GetTemplateChild(RibbonSplitButton.ToggleButtonTemplatePartName) as RibbonToggleButton;
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new RibbonSplitButtonAutomationPeer(this);
        }

        #endregion Overrides

        #region Visual States

        /// <summary>
        ///     DependencyProperty for CheckedBackground property.
        /// </summary>
        public static readonly DependencyProperty CheckedBackgroundProperty =
            RibbonControlService.CheckedBackgroundProperty.AddOwner(typeof(RibbonSplitButton));

        /// <summary>
        ///     Control background brush used in a "Checked" state of the RibbonSplitButton.
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
            RibbonControlService.CheckedBorderBrushProperty.AddOwner(typeof(RibbonSplitButton));

        /// <summary>
        ///     Control border brush used to paint a "Checked" RibbonSplitButton.
        /// </summary>
        public Brush CheckedBorderBrush
        {
            get { return RibbonControlService.GetCheckedBorderBrush(this); }
            set { RibbonControlService.SetCheckedBorderBrush(this, value); }
        }

        #endregion
        
        #region ToolTip Properties

        /// <summary>
        ///     DependencyProperty for DropDownToolTipTitle property.
        /// </summary>
        public static readonly DependencyProperty DropDownToolTipTitleProperty =
            DependencyProperty.Register("DropDownToolTipTitle",
            typeof(string),
            typeof(RibbonSplitButton), 
            new FrameworkPropertyMetadata(new PropertyChangedCallback(OnDropDownToolTipPropertyChanged)));

        /// <summary>
        ///     Title text for the tooltip on the DropDown portion of the RibbonSplitButton
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
            DependencyProperty.Register("DropDownToolTipDescription",
            typeof(string),
            typeof(RibbonSplitButton), 
            new FrameworkPropertyMetadata(new PropertyChangedCallback(OnDropDownToolTipPropertyChanged)));

        /// <summary>
        ///     Description text for the tooltip on the DropDown portion of the RibbonSplitButton
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
            DependencyProperty.Register("DropDownToolTipImageSource",
            typeof(ImageSource),
            typeof(RibbonSplitButton), 
            new FrameworkPropertyMetadata(new PropertyChangedCallback(OnDropDownToolTipPropertyChanged)));

        /// <summary>
        ///     Image source for the tooltip on the DropDown portion of the RibbonSplitButton
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
            DependencyProperty.Register("DropDownToolTipFooterTitle",
            typeof(string),
            typeof(RibbonSplitButton),
            new FrameworkPropertyMetadata(new PropertyChangedCallback(OnDropDownToolTipPropertyChanged)));

        /// <summary>
        ///     Title text for the footer of tooltip on the DropDown portion of the RibbonSplitButton
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
            DependencyProperty.Register("DropDownToolTipFooterDescription",
            typeof(string),
            typeof(RibbonSplitButton),
            new FrameworkPropertyMetadata(new PropertyChangedCallback(OnDropDownToolTipPropertyChanged)));

        /// <summary>
        ///     Description text for the footer of the tooltip on the DropDown portion of the RibbonSplitButton
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
            DependencyProperty.Register("DropDownToolTipFooterImageSource",
            typeof(ImageSource),
            typeof(RibbonSplitButton),
            new FrameworkPropertyMetadata(new PropertyChangedCallback(OnDropDownToolTipPropertyChanged)));

        /// <summary>
        ///     Image source for the footer of the tooltip on the DropDown portion of the RibbonSplitButton
        /// </summary>
        public ImageSource DropDownToolTipFooterImageSource
        {
            get { return (ImageSource)GetValue(DropDownToolTipFooterImageSourceProperty); }
            set { SetValue(DropDownToolTipFooterImageSourceProperty, value); }
        }

        private static void OnDropDownToolTipPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonSplitButton splitButton = (RibbonSplitButton)d;
            if (splitButton.PartToggleButton != null)
            {
                splitButton.PartToggleButton.CoerceValue(FrameworkElement.ToolTipProperty);
            }
        }

        #endregion

        #region Button Properties

        /// <summary>
        ///     Event corresponds to left mouse button click
        /// </summary>
        public static readonly RoutedEvent ClickEvent = MenuItem.ClickEvent.AddOwner(typeof(RibbonSplitButton));

        /// <summary>
        ///     Add / Remove Click handler
        /// </summary>
        public event RoutedEventHandler Click
        {
            add
            {
                AddHandler(RibbonSplitButton.ClickEvent, value);
            }

            remove
            {
                RemoveHandler(RibbonSplitButton.ClickEvent, value);
            }
        }

        #endregion

        #region SplitButton Properties

        /// <summary>
        ///   A property indicating the relative position of the RibbonSplitButton's label.
        /// </summary>
        public RibbonSplitButtonLabelPosition LabelPosition
        {
            get { return (RibbonSplitButtonLabelPosition)GetValue(LabelPositionProperty); }
            set { SetValue(LabelPositionProperty, value); }
        }

        /// <summary>
        ///   Using a DependencyProperty as the backing store for LabelPositionProperty.  This enables animation, styling, binding, etc...
        ///     Flags:              None
        ///     Default Value:      Header
        /// </summary>
        public static readonly DependencyProperty LabelPositionProperty =
                    DependencyProperty.Register(
                            "LabelPosition",
                            typeof(RibbonSplitButtonLabelPosition),
                            typeof(RibbonSplitButton),
                            new FrameworkPropertyMetadata(RibbonSplitButtonLabelPosition.Header));

        /// <summary>
        ///     The DependencyProperty for the IsCheckable property.
        ///     Flags:              None
        ///     Default Value:      false
        /// </summary>
        public static readonly DependencyProperty IsCheckableProperty =
                DependencyProperty.Register(
                        "IsCheckable",
                        typeof(bool),
                        typeof(RibbonSplitButton),
                        new FrameworkPropertyMetadata(false));

        /// <summary>
        ///     IsCheckable determines the user ability to check/uncheck the RibbonSplitButton.
        /// </summary>
        public bool IsCheckable
        {
            get { return (bool)GetValue(IsCheckableProperty); }
            set { SetValue(IsCheckableProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the IsChecked property.
        ///     Flags:              None
        ///     Default Value:      false
        /// </summary>
        public static readonly DependencyProperty IsCheckedProperty =
                DependencyProperty.Register(
                        "IsChecked",
                        typeof(bool),
                        typeof(RibbonSplitButton),
                        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnIsCheckedChanged)));
                                

        /// <summary>
        ///     When the RibbonSplitButton is checked.
        /// </summary>
        public bool IsChecked
        {
            get { return (bool)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }

        #endregion

        #region Commands

        /// <summary>
        /// Get or set the Command property
        /// </summary>
        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for RoutedCommand
        /// </summary>
        public static readonly DependencyProperty CommandProperty =
                DependencyProperty.Register(
                        "Command",
                        typeof(ICommand),
                        typeof(RibbonSplitButton),
                        new FrameworkPropertyMetadata((ICommand)null, RibbonHelper.OnCommandChanged));

        /// <summary>
        /// Get or set the CommandParameter property
        /// </summary>
        public object CommandParameter
        {
            get { return GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }

        /// <summary>
        /// The DependencyProperty for the CommandParameter
        /// </summary>
        public static readonly DependencyProperty CommandParameterProperty =
                DependencyProperty.Register(
                        "CommandParameter",
                        typeof(object),
                        typeof(RibbonSplitButton),
                        new FrameworkPropertyMetadata((object)null));


        /// <summary>
        /// Get or set the CommandTarget property
        /// </summary>
        public IInputElement CommandTarget
        {
            get { return (IInputElement)GetValue(CommandTargetProperty); }
            set { SetValue(CommandTargetProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for Target property
        ///     Flags:              None
        ///     Default Value:      null
        /// </summary>
        public static readonly DependencyProperty CommandTargetProperty =
                DependencyProperty.Register(
                        "CommandTarget",
                        typeof(IInputElement),
                        typeof(RibbonSplitButton),
                        new FrameworkPropertyMetadata((IInputElement)null));

        #endregion

        #region Protected Methods

        private void OnHeaderClicked(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(RibbonSplitButton.ClickEvent, this));

            if (!IsCheckable && AutomationPeer.ListenerExists(AutomationEvents.InvokePatternOnInvoked))
            {
                RibbonSplitButtonAutomationPeer peer = UIElementAutomationPeer.FromElement(this) as RibbonSplitButtonAutomationPeer;
                if (peer != null)
                {
                    peer.RaiseInvokeAutomationEvent();
                }
            }
        }

        private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonSplitButton splitButton = (RibbonSplitButton)d;
            if (splitButton.IsCheckable)
            {
                RibbonSplitButtonAutomationPeer peer = UIElementAutomationPeer.FromElement(splitButton) as RibbonSplitButtonAutomationPeer;
                if (peer != null)
                {
                    peer.RaiseToggleStatePropertyChangedEvent((bool)e.OldValue, (bool)e.NewValue);
                }
            }
        }

        private static void OnBorderThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((RibbonSplitButton)d).SetBorderThickess();
        }

        private void SetBorderThickess()
        {
            if (PartToggleButton != null && _headerButton != null && ControlSizeDefinition != null )
            {
                if (ControlSizeDefinition.ImageSize == RibbonImageSize.Large)
                {
                    if (!IsInControlGroup)
                    {
                        // Top = 0.0
                        PartToggleButton.BorderThickness = new Thickness(BorderThickness.Left, 0.0, BorderThickness.Right, BorderThickness.Bottom);
                    }
                    else 
                    {
                        PartToggleButton.BorderThickness = new Thickness();
                        _headerButton.BorderThickness = new Thickness(0.0, 0.0, 0.0, 1.0);
                    }
                }
                else
                {
                    if (!IsInControlGroup)
                    {
                        // Left = 0.0
                        PartToggleButton.BorderThickness = new Thickness(0.0, BorderThickness.Top, BorderThickness.Right, BorderThickness.Bottom);
                    }
                    else
                    {
                        PartToggleButton.BorderThickness = new Thickness();
                        _headerButton.BorderThickness = new Thickness(0.0, 0.0, 1.0, 0.0);
                    }
                }
            }
        }

        internal override void TransferPseudoInheritedProperties()
        {
            // Dont call base here
        }

        internal ButtonBase HeaderButton
        {
            get { return _headerButton; }
        }

        #endregion

        #region QAT

        /// <summary>
        ///     The DependencyProperty for the HeaderQuickAccessToolBarIdProperty property.
        ///     Flags:              None
        ///     Default Value:      null
        /// </summary>
        public static readonly DependencyProperty HeaderQuickAccessToolBarIdProperty =
                DependencyProperty.Register(
                        "HeaderQuickAccessToolBarId",
                        typeof(object),
                        typeof(RibbonSplitButton),
                        new FrameworkPropertyMetadata(null));

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

        #region Private Data

        private const string HeaderButtonTemplatePartName = "PART_HeaderButton";
        private ButtonBase _headerButton;
        private RibbonToggleButton _toggleButton;
        
        #endregion

        #region KeyTips

        // KeyTip for the header part of the split button.
        public string HeaderKeyTip
        {
            get { return (string)GetValue(HeaderKeyTipProperty); }
            set { SetValue(HeaderKeyTipProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HeaderKeyTip.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HeaderKeyTipProperty =
            DependencyProperty.Register("HeaderKeyTip", typeof(string), typeof(RibbonSplitButton), new FrameworkPropertyMetadata(null));

        private Image GetHeaderImage()
        {
            Image imagePart = null;
            RibbonButton button = _headerButton as RibbonButton;
            if (button != null)
            {
                imagePart = button.Image;
            }
            else
            {
                RibbonToggleButton toggleButton = _headerButton as RibbonToggleButton;
                if (toggleButton != null)
                {
                    imagePart = toggleButton.Image;
                }
            }
            return imagePart;
        }

        protected override void OnActivatingKeyTip(ActivatingKeyTipEventArgs e)
        {
            if (e.OriginalSource == this)
            {
                // Dropdown keytip
                RibbonHelper.SetKeyTipPlacementForSplitButtonDropDown(this,
                    e,
                    _toggleButton);
            }
            else if (e.OriginalSource == _headerButton)
            {
                // Header keytip
                RibbonHelper.SetKeyTipPlacementForSplitButtonHeader(this,
                    e,
                    GetHeaderImage());
            }
        }

        #endregion
    }
}
