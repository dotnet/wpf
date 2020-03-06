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
    using System.Windows;
    using System.Windows.Automation.Peers;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using Microsoft.Windows.Input;
#if RIBBON_IN_FRAMEWORK
    using Microsoft.Windows.Controls;
#else
    using Microsoft.Windows.Automation.Peers;
#endif

    #endregion

    /// <summary>
    ///   A TextBox which can be placed in the Ribbon. It implements
    ///   ICommandSource so that it can execute a Command upon pressing 'Enter'.
    /// </summary>
    [TemplatePart(Name = RibbonTextBox.ContentHostTemplatePartName, Type = typeof(ScrollViewer))]
    public class RibbonTextBox : TextBox, ICommandSource
    {
        #region Constructors

        /// <summary>
        ///   Initializes static members of the RibbonTextBox class.  This also overrides
        ///   the default style, adds a coerce callback, and allows ToolTips to be shown
        ///   even when the control is disabled.
        /// </summary>
        static RibbonTextBox()
        {
            Type ownerType = typeof(RibbonTextBox);
            DefaultStyleKeyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(ownerType));
            ToolTipProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(null, new CoerceValueCallback(RibbonHelper.CoerceRibbonToolTip)));
            ToolTipService.ShowOnDisabledProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(true));
            ContextMenuProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(RibbonHelper.OnContextMenuChanged, RibbonHelper.OnCoerceContextMenu));
            ContextMenuService.ShowOnDisabledProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(true));
            EventManager.RegisterClassHandler(ownerType, KeyTipService.ActivatingKeyTipEvent, new ActivatingKeyTipEventHandler(OnActivatingKeyTipThunk));
            EventManager.RegisterClassHandler(ownerType, KeyTipService.KeyTipAccessedEvent, new KeyTipAccessedEventHandler(OnKeyTipAccessedThunk));
        }

        public RibbonTextBox()
        {
            CanExecute = true;
        }

        #endregion

        #region Overrides

        public override void OnApplyTemplate()
        {
            CoerceValue(ControlSizeDefinitionProperty);
            base.OnApplyTemplate();
            PropertyHelper.TransferProperty(this, ContextMenuProperty);   // Coerce to get a default ContextMenu if none has been specified.
            PropertyHelper.TransferProperty(this, RibbonControlService.CanAddToQuickAccessToolBarDirectlyProperty);
            _contentHost = GetTemplateChild(ContentHostTemplatePartName) as ScrollViewer;
            TemplateApplied = true;
        }

        protected override void OnTemplateChanged(ControlTemplate oldTemplate, ControlTemplate newTemplate)
        {
            TemplateApplied = false;
            base.OnTemplateChanged(oldTemplate, newTemplate);
        }

        #endregion Overrides

        #region Commanding

        /// <summary>
        ///   Gets or sets the Command that will be executed when the command source is invoked.
        /// </summary>
        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        /// <summary>
        ///   Using a DependencyProperty as the backing store for CommandProperty.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty CommandProperty =
                    DependencyProperty.Register(
                            "Command",
                            typeof(ICommand),
                            typeof(RibbonTextBox),
                            new FrameworkPropertyMetadata(new PropertyChangedCallback(OnCommandChanged)));

        /// <summary>
        ///   Gets or sets a user defined data value that can be passed to the command when it is executed.
        /// </summary>
        public object CommandParameter
        {
            get { return (object)GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }

        /// <summary>
        ///   Using a DependencyProperty as the backing store for CommandParameterProperty.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty CommandParameterProperty =
                    DependencyProperty.Register(
                            "CommandParameter",
                            typeof(object),
                            typeof(RibbonTextBox),
                            new FrameworkPropertyMetadata(null));

        /// <summary>
        ///   Gets or sets the object that the command is being executed on.
        /// </summary>
        public IInputElement CommandTarget
        {
            get { return (IInputElement)GetValue(CommandTargetProperty); }
            set { SetValue(CommandTargetProperty, value); }
        }

        /// <summary>
        ///   Using a DependencyProperty as the backing store for CommandTargetProperty.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty CommandTargetProperty =
                    DependencyProperty.Register(
                            "CommandTarget",
                            typeof(IInputElement),
                            typeof(RibbonTextBox),
                            new FrameworkPropertyMetadata(null));

        /// <summary>
        ///   Invoked each time a key-down event occurs.  When key-down occurs we
        ///   check to see if that key was 'Enter', and if so we invoke the
        ///   associated Command.
        /// </summary>
        /// <param name="e">A KeyEventArgs that contains the event data.</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            if (e.Key == Key.Enter)
            {
                CommandHelpers.InvokeCommandSource(CommandParameter, null, this, CommandOperation.Execute);

                // Dismiss parent Popups
                RaiseEvent(new RibbonDismissPopupEventArgs());
            }
        }

        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonTextBox textBox = (RibbonTextBox)d;
            ICommand oldCommand = (ICommand)e.OldValue;
            ICommand newCommand = (ICommand)e.NewValue;

            if (oldCommand != null)
            {
                textBox.UnhookCommand(oldCommand);
            }
            if (newCommand != null)
            {
                textBox.HookCommand(newCommand);
            }

            RibbonHelper.OnCommandChanged(d, e);
        }

        private void HookCommand(ICommand command)
        {
#if RIBBON_IN_FRAMEWORK
            CanExecuteChangedEventManager.AddHandler(command, OnCanExecuteChanged);
#else
            _canExecuteChangedHandler = new EventHandler(OnCanExecuteChanged);
            command.CanExecuteChanged += _canExecuteChangedHandler;
#endif
            UpdateCanExecute();
        }

        private void UnhookCommand(ICommand command)
        {
#if RIBBON_IN_FRAMEWORK
            CanExecuteChangedEventManager.RemoveHandler(command, OnCanExecuteChanged);
#else
            if (_canExecuteChangedHandler != null)
            {
                command.CanExecuteChanged -= _canExecuteChangedHandler;
                _canExecuteChangedHandler = null;
            }
#endif
            UpdateCanExecute();
        }

        private void OnCanExecuteChanged(object sender, EventArgs e)
        {
            UpdateCanExecute();
        }

        private void UpdateCanExecute()
        {
            if (Command != null)
            {
                CanExecute = CommandHelpers.CanExecuteCommandSource(CommandParameter, this);
            }
            else
            {
                CanExecute = true;
            }
        }

        /// <summary>
        ///     Fetches the value of the IsEnabled property
        /// </summary>
        /// <remarks>
        ///     The reason this property is overridden is so that RibbonTextBox
        ///     can infuse the value for CanExecute into it.
        /// </remarks>
        protected override bool IsEnabledCore
        {
            get
            {
                return base.IsEnabledCore && CanExecute;
            }
        }

        private bool CanExecute
        {
            get { return _bits[(int)Bits.CanExecute]; }
            set
            {
                _bits[(int)Bits.CanExecute] = value;
                CoerceValue(IsEnabledProperty);
            }
        }

        private bool TemplateApplied
        {
            get { return _bits[(int)Bits.TemplateApplied]; }
            set
            {
                _bits[(int)Bits.TemplateApplied] = value;
            }
        }

#if !RIBBON_IN_FRAMEWORK
        private EventHandler _canExecuteChangedHandler;
#endif

        #endregion Commanding

        #region RibbonControlService Properties

        /// <summary>
        ///     DependencyProperty for LargeImageSource property.
        /// </summary>
        public static readonly DependencyProperty LargeImageSourceProperty =
            RibbonControlService.LargeImageSourceProperty.AddOwner(typeof(RibbonTextBox));

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
            RibbonControlService.SmallImageSourceProperty.AddOwner(typeof(RibbonTextBox));

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
            RibbonControlService.LabelProperty.AddOwner(typeof(RibbonTextBox));

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
            RibbonControlService.ToolTipTitleProperty.AddOwner(typeof(RibbonTextBox), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

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
            RibbonControlService.ToolTipDescriptionProperty.AddOwner(typeof(RibbonTextBox), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

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
            RibbonControlService.ToolTipImageSourceProperty.AddOwner(typeof(RibbonTextBox), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

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
            RibbonControlService.ToolTipFooterTitleProperty.AddOwner(typeof(RibbonTextBox), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

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
            RibbonControlService.ToolTipFooterDescriptionProperty.AddOwner(typeof(RibbonTextBox), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

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
            RibbonControlService.ToolTipFooterImageSourceProperty.AddOwner(typeof(RibbonTextBox), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

        /// <summary>
        ///     Image source for the footer of the tooltip of the control.
        /// </summary>
        public ImageSource ToolTipFooterImageSource
        {
            get { return RibbonControlService.GetToolTipFooterImageSource(this); }
            set { RibbonControlService.SetToolTipFooterImageSource(this, value); }
        }

        #endregion

        #region Resizing Properties

        /// <summary>
        ///     DependencyProperty for ControlSizeDefinition property.
        /// </summary>
        public static readonly DependencyProperty ControlSizeDefinitionProperty =
            RibbonControlService.ControlSizeDefinitionProperty.AddOwner(typeof(RibbonTextBox));

        /// <summary>
        ///     Size definition, including image size and visibility of label and image for this control.
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
            RibbonControlService.IsInControlGroupProperty.AddOwner(typeof(RibbonTextBox));

        /// <summary>
        ///     This property indicates whether the control is part of a RibbonControlGroup.
        /// </summary>
        public bool IsInControlGroup
        {
            get { return RibbonControlService.GetIsInControlGroup(this); }
            internal set { RibbonControlService.SetIsInControlGroup(this, value); }
        }

        #endregion Resizing Properties

        #region QuickAccessToolBar Properties

        /// <summary>
        ///     DependencyProperty for QuickAccessToolBarControlSizeDefinition property.
        /// </summary>
        public static readonly DependencyProperty QuickAccessToolBarControlSizeDefinitionProperty =
            RibbonControlService.QuickAccessToolBarControlSizeDefinitionProperty.AddOwner(typeof(RibbonTextBox));

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
            RibbonControlService.IsInQuickAccessToolBarProperty.AddOwner(typeof(RibbonTextBox));

        /// <summary>
        ///     This property indicates whether the control is part of a QuickAccessToolBar.
        /// </summary>
        public bool IsInQuickAccessToolBar
        {
            get { return RibbonControlService.GetIsInQuickAccessToolBar(this); }
            internal set { RibbonControlService.SetIsInQuickAccessToolBar(this, value); }
        }

        #endregion QuickAccessToolBar Properties

        #region UI Automation

        /// <summary>
        ///     Get AutomationPeer for RibbonTextBox control
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new RibbonTextBoxAutomationPeer(this);
        }

        #endregion

        #region VisualStates

        /// <summary>
        ///     DependencyProperty for Ribbon property.
        /// </summary>
        public static readonly DependencyProperty RibbonProperty =
            RibbonControlService.RibbonProperty.AddOwner(typeof(RibbonTextBox));

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
            RibbonControlService.MouseOverBorderBrushProperty.AddOwner(typeof(RibbonTextBox));

        /// <summary>
        ///     Outer border brush used in a "hover" state of the RibbonTextBox.
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
            RibbonControlService.MouseOverBackgroundProperty.AddOwner(typeof(RibbonTextBox));

        /// <summary>
        ///     Control background brush used in a "hover" state of the RibbonTextBox.
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
            RibbonControlService.FocusedBackgroundProperty.AddOwner(typeof(RibbonTextBox));

        /// <summary>
        ///     Control background brush used in a "Focused" state of the RibbonTextBox.
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
            RibbonControlService.FocusedBorderBrushProperty.AddOwner(typeof(RibbonTextBox));

        /// <summary>
        ///     Control border brush used to paint a "Focused" state of the RibbonTextBox.
        /// </summary>
        public Brush FocusedBorderBrush
        {
            get { return RibbonControlService.GetFocusedBorderBrush(this); }
            set { RibbonControlService.SetFocusedBorderBrush(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for ShowKeyboardCues property.
        /// </summary>
        public static readonly DependencyProperty ShowKeyboardCuesProperty =
            RibbonControlService.ShowKeyboardCuesProperty.AddOwner(typeof(RibbonTextBox));

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

        #region Public Properties

        /// <summary>
        ///     DependencyProperty for TextBoxWidth property.
        /// </summary>
        public static readonly DependencyProperty TextBoxWidthProperty =
            DependencyProperty.Register(
                    "TextBoxWidth",
                    typeof(double),
                    typeof(RibbonTextBox),
                    new FrameworkPropertyMetadata(0.0d));


        /// <summary>
        ///   Gets or sets the width of the text box.
        /// </summary>
        public double TextBoxWidth
        {
            get { return (double)GetValue(TextBoxWidthProperty); }
            set { SetValue(TextBoxWidthProperty, value); }
        }

        #endregion Public Properties

        #region Private Data

        private enum Bits
        {
            CanExecute = 0x01,
            TemplateApplied = 0x02
        }

        // Packed boolean information
        private BitVector32 _bits = new BitVector32((int)Bits.CanExecute);
        private ScrollViewer _contentHost = null;
        private const string ContentHostTemplatePartName = "PART_ContentHost";

        #endregion Private Data

        #region QAT

        /// <summary>
        ///   DependencyProperty for QuickAccessToolBarId property.
        /// </summary>
        public static readonly DependencyProperty QuickAccessToolBarIdProperty =
            RibbonControlService.QuickAccessToolBarIdProperty.AddOwner(typeof(RibbonTextBox));

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
            RibbonControlService.CanAddToQuickAccessToolBarDirectlyProperty.AddOwner(typeof(RibbonTextBox),
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
            KeyTipService.KeyTipProperty.AddOwner(typeof(RibbonTextBox));

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
            ((RibbonTextBox)sender).OnActivatingKeyTip(e);
        }

        protected virtual void OnActivatingKeyTip(ActivatingKeyTipEventArgs e)
        {
            if (e.OriginalSource == this)
            {
                RibbonHelper.SetKeyTipPlacementForTextBox(this, e, _contentHost);
            }
        }

        private static void OnKeyTipAccessedThunk(object sender, KeyTipAccessedEventArgs e)
        {
            ((RibbonTextBox)sender).OnKeyTipAccessed(e);
        }

        protected virtual void OnKeyTipAccessed(KeyTipAccessedEventArgs e)
        {
            if (e.OriginalSource == this)
            {
                // Open Group DropDown and focus self.
                RibbonHelper.OpenParentRibbonGroupDropDownSync(this, TemplateApplied);
                Focus();
                e.Handled = true;
            }
        }

        #endregion KeyTips
    }
}
