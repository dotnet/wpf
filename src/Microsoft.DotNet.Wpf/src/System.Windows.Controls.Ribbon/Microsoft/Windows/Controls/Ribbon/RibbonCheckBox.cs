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
    using System.Windows.Media;
    using System.Collections.Specialized;
    using System.Windows.Input;
#if RIBBON_IN_FRAMEWORK
    using Microsoft.Windows.Controls;
#else
    using Microsoft.Windows.Automation.Peers;
#endif

    #endregion

    /// <summary>
    ///   A CheckBox which can be placed in the Ribbon. It implements 
    ///   ToolTip property coercion to allow use of Ribbon tooltips.
    /// </summary>
    [TemplatePart(Name = RibbonCheckBox.CheckBorderTemplatePart, Type = typeof(Border))]
    public class RibbonCheckBox : CheckBox
    {
        #region Constructors

        /// <summary>
        ///   Initializes static members of the RibbonCheckBox class.  Here we override the
        ///   default style, a couple callbacks, and allow tooltips to be shown for disabled controls.
        /// </summary>
        static RibbonCheckBox()
        {
            Type ownerType = typeof(RibbonCheckBox);

            DefaultStyleKeyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(ownerType));
            FocusableProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(null, new CoerceValueCallback(OnCoerceFocusable)));
            ToolTipProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(null, new CoerceValueCallback(RibbonHelper.CoerceRibbonToolTip)));
            ToolTipService.ShowOnDisabledProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(true));
            CommandProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(RibbonHelper.OnCommandChanged));
            ContextMenuProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(RibbonHelper.OnContextMenuChanged, RibbonHelper.OnCoerceContextMenu));
            ContextMenuService.ShowOnDisabledProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(true));
            EventManager.RegisterClassHandler(ownerType, KeyTipService.ActivatingKeyTipEvent, new ActivatingKeyTipEventHandler(OnActivatingKeyTipThunk));
            EventManager.RegisterClassHandler(ownerType, KeyTipService.KeyTipAccessedEvent, new KeyTipAccessedEventHandler(OnKeyTipAccessedThunk));
        }

        #endregion

        #region Overrides

        public override void OnApplyTemplate()
        {
            CoerceValue(ControlSizeDefinitionProperty);
            base.OnApplyTemplate();
            PropertyHelper.TransferProperty(this, ContextMenuProperty);   // Coerce to get a default ContextMenu if none has been specified.
            PropertyHelper.TransferProperty(this, RibbonControlService.CanAddToQuickAccessToolBarDirectlyProperty);
            _checkBorder = GetTemplateChild(CheckBorderTemplatePart) as Border;
        }

        #endregion Overrides

        #region RibbonControlService Properties

        /// <summary>
        ///     DependencyProperty for LargeImageSource property.
        /// </summary>
        public static readonly DependencyProperty LargeImageSourceProperty =
            RibbonControlService.LargeImageSourceProperty.AddOwner(typeof(RibbonCheckBox));

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
            RibbonControlService.SmallImageSourceProperty.AddOwner(typeof(RibbonCheckBox));

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
            RibbonControlService.LabelProperty.AddOwner(typeof(RibbonCheckBox));

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
            RibbonControlService.ToolTipTitleProperty.AddOwner(typeof(RibbonCheckBox), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

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
            RibbonControlService.ToolTipDescriptionProperty.AddOwner(typeof(RibbonCheckBox), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

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
            RibbonControlService.ToolTipImageSourceProperty.AddOwner(typeof(RibbonCheckBox), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

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
            RibbonControlService.ToolTipFooterTitleProperty.AddOwner(typeof(RibbonCheckBox), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

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
            RibbonControlService.ToolTipFooterDescriptionProperty.AddOwner(typeof(RibbonCheckBox), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

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
            RibbonControlService.ToolTipFooterImageSourceProperty.AddOwner(typeof(RibbonCheckBox), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

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
            RibbonControlService.ControlSizeDefinitionProperty.AddOwner(typeof(RibbonCheckBox));

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
            RibbonControlService.IsInControlGroupProperty.AddOwner(typeof(RibbonCheckBox));

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
            RibbonControlService.QuickAccessToolBarControlSizeDefinitionProperty.AddOwner(typeof(RibbonCheckBox));

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
            RibbonControlService.IsInQuickAccessToolBarProperty.AddOwner(typeof(RibbonCheckBox));

        /// <summary>
        ///     This property indicates whether the control is part of a QuickAccessToolBar.
        /// </summary>
        public bool IsInQuickAccessToolBar
        {
            get { return RibbonControlService.GetIsInQuickAccessToolBar(this); }
            internal set { RibbonControlService.SetIsInQuickAccessToolBar(this, value); }
        }

 
        #endregion

        #region UI Automation

        /// <summary>
        ///     Get AutomationPeer for RibbonButton control
        /// </summary>
        /// <returns></returns>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new RibbonCheckBoxAutomationPeer(this);
        }
        
        #endregion

        #region VisualStates

        /// <summary>
        ///     DependencyProperty for Ribbon property.
        /// </summary>
        public static readonly DependencyProperty RibbonProperty =
            RibbonControlService.RibbonProperty.AddOwner(typeof(RibbonCheckBox));

        /// <summary>
        ///     This property is used to access visual states brushes defined on the Ribbon class.
        /// </summary>
        public Ribbon Ribbon
        {
            get { return RibbonControlService.GetRibbon(this); }
        }

        /// <summary>
        ///     DependencyProperty for MouseOverBorderBrush property.
        /// </summary>
        public static readonly DependencyProperty MouseOverBorderBrushProperty =
            RibbonControlService.MouseOverBorderBrushProperty.AddOwner(typeof(RibbonCheckBox));

        /// <summary>
        ///     Outer border brush used in a "hover" state of the RibbonCheckBox.
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
            RibbonControlService.MouseOverBackgroundProperty.AddOwner(typeof(RibbonCheckBox));

        /// <summary>
        ///     Control background brush used in a "hover" state of the RibbonCheckBox.
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
            RibbonControlService.PressedBorderBrushProperty.AddOwner(typeof(RibbonCheckBox));

        /// <summary>
        ///     Outer border brush used in a "pressed" state of the RibbonCheckBox.
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
            RibbonControlService.PressedBackgroundProperty.AddOwner(typeof(RibbonCheckBox));

        /// <summary>
        ///     Control background brush used in a "pressed" state of the RibbonCheckBox.
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
            RibbonControlService.CheckedBackgroundProperty.AddOwner(typeof(RibbonCheckBox));

        /// <summary>
        ///     Control background brush used in a "Checked" state of the RibbonCheckBox.
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
            RibbonControlService.CheckedBorderBrushProperty.AddOwner(typeof(RibbonCheckBox));

        /// <summary>
        ///     Control border brush used to paint a "Checked" RibbonCheckBox.
        /// </summary>
        public Brush CheckedBorderBrush
        {
            get { return RibbonControlService.GetCheckedBorderBrush(this); }
            set { RibbonControlService.SetCheckedBorderBrush(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for FocusedBackground property.
        /// </summary>
        public static readonly DependencyProperty FocusedBackgroundProperty =
            RibbonControlService.FocusedBackgroundProperty.AddOwner(typeof(RibbonCheckBox));

        /// <summary>
        ///     Control background brush used in a "Focused" state of the RibbonCheckBox.
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
            RibbonControlService.FocusedBorderBrushProperty.AddOwner(typeof(RibbonCheckBox));

        /// <summary>
        ///     Control border brush used to paint a "Focused" RibbonCheckBox.
        /// </summary>
        public Brush FocusedBorderBrush
        {
            get { return RibbonControlService.GetFocusedBorderBrush(this); }
            set { RibbonControlService.SetFocusedBorderBrush(this, value); }
        }

        /// <summary>
        ///     This override ensures that the base call doesn't cause the control 
        ///     to take keyboard focus. And it does so by temporarily coercing the 
        ///     FocusableProperty to false.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            try
            {
                try
                {
                    CoerceFocusable = true;
                    CoerceValue(FocusableProperty);
                }
                finally
                {
                    CoerceFocusable = false;
                }

                base.OnMouseLeftButtonDown(e);
            }
            finally
            {
                CoerceValue(FocusableProperty);
            }
        }

        private static object OnCoerceFocusable(DependencyObject d, object baseValue)
        {
            RibbonCheckBox checkBox = (RibbonCheckBox)d;
            if (checkBox.CoerceFocusable)
            {
                return false;
            }

            return baseValue;
        }

        private bool CoerceFocusable
        {
            get { return _bits[(int)Bits.CoerceFocusable]; }
            set { _bits[(int)Bits.CoerceFocusable] = value; }
        }

        /// <summary>
        ///     DependencyProperty for ShowKeyboardCues property.
        /// </summary>
        public static readonly DependencyProperty ShowKeyboardCuesProperty =
            RibbonControlService.ShowKeyboardCuesProperty.AddOwner(typeof(RibbonCheckBox));

        /// <summary>
        ///     This property is used to decide when to show the Keyboard FocusVisual.
        /// </summary>
        public bool ShowKeyboardCues
        {
            get { return RibbonControlService.GetShowKeyboardCues(this); }
        }

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnGotKeyboardFocus(e);
            RibbonHelper.EnableFocusVisual(this);
        }

        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnLostKeyboardFocus(e);
            RibbonHelper.DisableFocusVisual(this);
        }

        #endregion VisualStates

        #region Private Data

        private enum Bits
        {
            CoerceFocusable = 0x01
        }

        // Packed boolean information
        private BitVector32 _bits = new BitVector32(0);
        Border _checkBorder = null;
        private const string CheckBorderTemplatePart = "PART_CheckBorder";

        #endregion Private Data

        #region DismissPopup

        protected override void OnClick()
        {
            base.OnClick();

            // Dismiss parent Popups
            RaiseEvent(new RibbonDismissPopupEventArgs());
        }

        #endregion DismissPopup

        #region QAT

        /// <summary>
        ///   DependencyProperty for QuickAccessToolBarId property.
        /// </summary>
        public static readonly DependencyProperty QuickAccessToolBarIdProperty =
            RibbonControlService.QuickAccessToolBarIdProperty.AddOwner(typeof(RibbonCheckBox));

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
            RibbonControlService.CanAddToQuickAccessToolBarDirectlyProperty.AddOwner(typeof(RibbonCheckBox),
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
            KeyTipService.KeyTipProperty.AddOwner(typeof(RibbonCheckBox));

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
            ((RibbonCheckBox)sender).OnActivatingKeyTip(e);
        }

        protected virtual void OnActivatingKeyTip(ActivatingKeyTipEventArgs e)
        {
            if (e.OriginalSource == this)
            {
                RibbonHelper.SetKeyTipPlacementForTextBox(this, e, _checkBorder);
            }
        }

        private static void OnKeyTipAccessedThunk(object sender, KeyTipAccessedEventArgs e)
        {
            ((RibbonCheckBox)sender).OnKeyTipAccessed(e);
        }

        protected virtual void OnKeyTipAccessed(KeyTipAccessedEventArgs e)
        {
            if (e.OriginalSource == this)
            {
                OnClick();
                e.Handled = true;
            }
        }

        #endregion KeyTips
    }
}
