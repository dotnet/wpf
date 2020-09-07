// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;

using System.Windows.Threading;

using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows;
using System.Windows.Input;
using System.Windows.Documents;

using MS.Internal;
using MS.Internal.KnownBoxes;
using MS.Internal.PresentationFramework;
using MS.Utility;

namespace System.Windows.Controls
{
    /// <summary>
    ///     The base class for all controls.
    /// </summary>
    public class Control : FrameworkElement
    {
        #region Constructors

        static Control()
        {
            FocusableProperty.OverrideMetadata(typeof(Control), new FrameworkPropertyMetadata(BooleanBoxes.TrueBox));

            EventManager.RegisterClassHandler(typeof(Control), UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(HandleDoubleClick), true);
            EventManager.RegisterClassHandler(typeof(Control), UIElement.MouseLeftButtonDownEvent, new MouseButtonEventHandler(HandleDoubleClick), true);
            EventManager.RegisterClassHandler(typeof(Control), UIElement.PreviewMouseRightButtonDownEvent, new MouseButtonEventHandler(HandleDoubleClick), true);
            EventManager.RegisterClassHandler(typeof(Control), UIElement.MouseRightButtonDownEvent, new MouseButtonEventHandler(HandleDoubleClick), true);

            // change handlers to update validation visual state
            IsKeyboardFocusedPropertyKey.OverrideMetadata(typeof(Control), new PropertyMetadata(new PropertyChangedCallback(OnVisualStatePropertyChanged)));
        }

        /// <summary>
        ///     Default Control constructor
        /// </summary>
        /// <remarks>
        ///     Automatic determination of current Dispatcher. Use alternative constructor
        ///     that accepts a Dispatcher for best performance.
        /// </remarks>
        public Control() : base()
        {
            // Initialize the _templateCache to the default value for TemplateProperty.
            // If the default value is non-null then wire it to the current instance.
            PropertyMetadata metadata = TemplateProperty.GetMetadata(DependencyObjectType);
            ControlTemplate defaultValue = (ControlTemplate) metadata.DefaultValue;
            if (defaultValue != null)
            {
                OnTemplateChanged(this, new DependencyPropertyChangedEventArgs(TemplateProperty, metadata, null, defaultValue));
            }
        }

        #endregion

        #region Properties

        /// <summary>
        ///     The DependencyProperty for the BorderBrush property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty BorderBrushProperty
                = Border.BorderBrushProperty.AddOwner(typeof(Control),
                    new FrameworkPropertyMetadata(
                        Border.BorderBrushProperty.DefaultMetadata.DefaultValue,
                        FrameworkPropertyMetadataOptions.None));

        /// <summary>
        ///     An object that describes the border background.
        ///     This will only affect controls whose template uses the property
        ///     as a parameter. On other controls, the property will do nothing.
        /// </summary>
        [Bindable(true), Category("Appearance")]
        public Brush BorderBrush
        {
            get { return (Brush) GetValue(BorderBrushProperty); }
            set { SetValue(BorderBrushProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the BorderThickness property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty BorderThicknessProperty
                = Border.BorderThicknessProperty.AddOwner(typeof(Control),
                    new FrameworkPropertyMetadata(
                        Border.BorderThicknessProperty.DefaultMetadata.DefaultValue,
                        FrameworkPropertyMetadataOptions.None));

        /// <summary>
        ///     An object that describes the border thickness.
        ///     This will only affect controls whose template uses the property
        ///     as a parameter. On other controls, the property will do nothing.
        /// </summary>
        [Bindable(true), Category("Appearance")]
        public Thickness BorderThickness
        {
            get { return (Thickness) GetValue(BorderThicknessProperty); }
            set { SetValue(BorderThicknessProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the Background property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty BackgroundProperty =
                Panel.BackgroundProperty.AddOwner(typeof(Control),
                    new FrameworkPropertyMetadata(
                        Panel.BackgroundProperty.DefaultMetadata.DefaultValue,
                        FrameworkPropertyMetadataOptions.None));


        /// <summary>
        ///     An object that describes the background.
        ///     This will only affect controls whose template uses the property
        ///     as a parameter. On other controls, the property will do nothing.
        /// </summary>
        [Bindable(true), Category("Appearance")]
        public Brush Background
        {
            get { return (Brush) GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the Foreground property.
        ///     Flags:              Can be used in style rules
        ///     Default Value:      System Font Color
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty ForegroundProperty =
                TextElement.ForegroundProperty.AddOwner(
                        typeof(Control),
                        new FrameworkPropertyMetadata(SystemColors.ControlTextBrush,
                            FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        ///     An brush that describes the foreground color.
        ///     This will only affect controls whose template uses the property
        ///     as a parameter. On other controls, the property will do nothing.
        /// </summary>
        [Bindable(true), Category("Appearance")]
        public Brush Foreground
        {
            get { return (Brush) GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the FontFamily property.
        ///     Flags:              Can be used in style rules
        ///     Default Value:      System Dialog Font
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty FontFamilyProperty =
                TextElement.FontFamilyProperty.AddOwner(
                        typeof(Control),
                        new FrameworkPropertyMetadata(SystemFonts.MessageFontFamily,
                            FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        ///     The font family of the desired font.
        ///     This will only affect controls whose template uses the property
        ///     as a parameter. On other controls, the property will do nothing.
        /// </summary>
        [Bindable(true), Category("Appearance")]
        [Localizability(LocalizationCategory.Font)]
        public FontFamily FontFamily
        {
            get { return (FontFamily) GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the FontSize property.
        ///     Flags:              Can be used in style rules
        ///     Default Value:      System Dialog Font Size
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty FontSizeProperty =
                TextElement.FontSizeProperty.AddOwner(
                        typeof(Control),
                        new FrameworkPropertyMetadata(SystemFonts.MessageFontSize,
                            FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        ///     The size of the desired font.
        ///     This will only affect controls whose template uses the property
        ///     as a parameter. On other controls, the property will do nothing.
        /// </summary>
        [TypeConverter(typeof(FontSizeConverter))]
        [Bindable(true), Category("Appearance")]
        [Localizability(LocalizationCategory.None)]
        public double FontSize
        {
            get { return (double) GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the FontStretch property.
        ///     Flags:              Can be used in style rules
        ///     Default Value:      FontStretches.Normal
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty FontStretchProperty
            = TextElement.FontStretchProperty.AddOwner(typeof(Control),
                    new FrameworkPropertyMetadata(TextElement.FontStretchProperty.DefaultMetadata.DefaultValue,
                        FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        ///     The stretch of the desired font.
        ///     This will only affect controls whose template uses the property
        ///     as a parameter. On other controls, the property will do nothing.
        /// </summary>
        [Bindable(true), Category("Appearance")]
        public FontStretch FontStretch
        {
            get { return (FontStretch) GetValue(FontStretchProperty); }
            set { SetValue(FontStretchProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the FontStyle property.
        ///     Flags:              Can be used in style rules
        ///     Default Value:      System Dialog Font Style
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty FontStyleProperty =
                TextElement.FontStyleProperty.AddOwner(
                        typeof(Control),
                        new FrameworkPropertyMetadata(SystemFonts.MessageFontStyle,
                            FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        ///     The style of the desired font.
        ///     This will only affect controls whose template uses the property
        ///     as a parameter. On other controls, the property will do nothing.
        /// </summary>
        [Bindable(true), Category("Appearance")]
        public FontStyle FontStyle
        {
            get { return (FontStyle) GetValue(FontStyleProperty); }
            set { SetValue(FontStyleProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the FontWeight property.
        ///     Flags:              Can be used in style rules
        ///     Default Value:      System Dialog Font Weight
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty FontWeightProperty =
                TextElement.FontWeightProperty.AddOwner(
                        typeof(Control),
                        new FrameworkPropertyMetadata(SystemFonts.MessageFontWeight,
                            FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        ///     The weight or thickness of the desired font.
        ///     This will only affect controls whose template uses the property
        ///     as a parameter. On other controls, the property will do nothing.
        /// </summary>
        [Bindable(true), Category("Appearance")]
        public FontWeight FontWeight
        {
            get { return (FontWeight) GetValue(FontWeightProperty); }
            set { SetValue(FontWeightProperty, value); }
        }

        /// <summary>
        /// HorizontalContentAlignment Dependency Property.
        ///     Flags:              Can be used in style rules
        ///     Default Value:      HorizontalAlignment.Left
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty HorizontalContentAlignmentProperty =
                    DependencyProperty.Register(
                                "HorizontalContentAlignment",
                                typeof(HorizontalAlignment),
                                typeof(Control),
                                new FrameworkPropertyMetadata(HorizontalAlignment.Left),
                                new ValidateValueCallback(FrameworkElement.ValidateHorizontalAlignmentValue));

        /// <summary>
        ///     The horizontal alignment of the control.
        ///     This will only affect controls whose template uses the property
        ///     as a parameter. On other controls, the property will do nothing.
        /// </summary>
        [Bindable(true), Category("Layout")]
        public HorizontalAlignment HorizontalContentAlignment
        {
            get { return (HorizontalAlignment) GetValue(HorizontalContentAlignmentProperty); }
            set { SetValue(HorizontalContentAlignmentProperty, value); }
         }

        /// <summary>
        /// VerticalContentAlignment Dependency Property.
        ///     Flags:              Can be used in style rules
        ///     Default Value:      VerticalAlignment.Top
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty VerticalContentAlignmentProperty =
                    DependencyProperty.Register(
                                "VerticalContentAlignment",
                                typeof(VerticalAlignment),
                                typeof(Control),
                                new FrameworkPropertyMetadata(VerticalAlignment.Top),
                                new ValidateValueCallback(FrameworkElement.ValidateVerticalAlignmentValue));

        /// <summary>
        ///     The vertical alignment of the control.
        ///     This will only affect controls whose template uses the property
        ///     as a parameter. On other controls, the property will do nothing.
        /// </summary>
        [Bindable(true), Category("Layout")]
        public VerticalAlignment VerticalContentAlignment
        {
            get { return (VerticalAlignment) GetValue(VerticalContentAlignmentProperty); }
            set { SetValue(VerticalContentAlignmentProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the TabIndex property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty TabIndexProperty
                = KeyboardNavigation.TabIndexProperty.AddOwner(typeof(Control));

        /// <summary>
        ///     TabIndex property change the order of Tab navigation between Controls.
        ///     Control with lower TabIndex will get focus before the Control with higher index
        /// </summary>
        [Bindable(true), Category("Behavior")]
        public int TabIndex
        {
            get { return (int) GetValue(TabIndexProperty); }
            set { SetValue(TabIndexProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the IsTabStop property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty IsTabStopProperty
                = KeyboardNavigation.IsTabStopProperty.AddOwner(typeof(Control));

        /// <summary>
        ///     Determine is the Control should be considered during Tab navigation.
        ///     If IsTabStop is false then it is excluded from Tab navigation
        /// </summary>
        [Bindable(true), Category("Behavior")]
        public bool IsTabStop
        {
            get { return (bool) GetValue(IsTabStopProperty); }
            set { SetValue(IsTabStopProperty, BooleanBoxes.Box(value)); }
        }

        /// <summary>
        /// PaddingProperty
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty PaddingProperty
            = DependencyProperty.Register( "Padding",
                                        typeof(Thickness), typeof(Control),
                                        new FrameworkPropertyMetadata(
                                                new Thickness(),
                                                FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        private static bool IsMarginValid(object value)
        {
            Thickness t = (Thickness)value;
            return (t.Left >= 0.0d
                    && t.Right >= 0.0d
                    && t.Top >= 0.0d
                    && t.Bottom >= 0.0d);
        }

        /// <summary>
        /// Padding Property
        /// </summary>
        [Bindable(true), Category("Layout")]
        public Thickness Padding
        {
            get { return (Thickness) GetValue(PaddingProperty); }
            set { SetValue(PaddingProperty, value); }
        }

        /// <summary>
        /// TemplateProperty
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty TemplateProperty =
                DependencyProperty.Register(
                        "Template",
                        typeof(ControlTemplate),
                        typeof(Control),
                        new FrameworkPropertyMetadata(
                                (ControlTemplate) null,  // default value
                                FrameworkPropertyMetadataOptions.AffectsMeasure,
                                new PropertyChangedCallback(OnTemplateChanged)));


        /// <summary>
        /// Template Property
        /// </summary>
        public ControlTemplate Template
        {
            get { return _templateCache; }
            set { SetValue(TemplateProperty, value); }
        }

        // Internal Helper so the FrameworkElement could see this property
        internal override FrameworkTemplate TemplateInternal
        {
            get { return Template; }
        }

        // Internal Helper so the FrameworkElement could see the template cache
        internal override FrameworkTemplate TemplateCache
        {
            get { return _templateCache; }
            set { _templateCache = (ControlTemplate) value; }
        }

        // Internal helper so FrameworkElement could see call the template changed virtual
        internal override void OnTemplateChangedInternal(FrameworkTemplate oldTemplate, FrameworkTemplate newTemplate)
        {
            OnTemplateChanged((ControlTemplate)oldTemplate, (ControlTemplate)newTemplate);
        }

        // Property invalidation callback invoked when TemplateProperty is invalidated
        private static void OnTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Control c = (Control) d;
            StyleHelper.UpdateTemplateCache(c, (FrameworkTemplate) e.OldValue, (FrameworkTemplate) e.NewValue, TemplateProperty);
        }

        /// <summary>
        ///     Template has changed
        /// </summary>
        /// <remarks>
        ///     When a Template changes, the VisualTree is removed. The new Template's
        ///     VisualTree will be created when ApplyTemplate is called
        /// </remarks>
        /// <param name="oldTemplate">The old Template</param>
        /// <param name="newTemplate">The new Template</param>
        protected virtual void OnTemplateChanged(ControlTemplate oldTemplate, ControlTemplate newTemplate)
        {
        }

        /// <summary>
        ///     If control has a scrollviewer in its style and has a custom keyboard scrolling behavior when HandlesScrolling should return true.
        /// Then ScrollViewer will not handle keyboard input and leave it up to the control.
        /// </summary>
        protected internal virtual bool HandlesScrolling
        {
            get { return false; }
        }

        internal bool VisualStateChangeSuspended
        {
            get { return ReadControlFlag(ControlBoolFlags.VisualStateChangeSuspended); }
            set { WriteControlFlag(ControlBoolFlags.VisualStateChangeSuspended, value); }
        }

        #endregion

        #region Public Methods


        /// <summary>
        ///     Returns a string representation of this object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string plainText = null;

            // GetPlainText overrides may try to access thread critical data
            if (CheckAccess())
            {
                plainText = GetPlainText();
            }
            else
            {
                //Not on dispatcher, try posting to the dispatcher with 20ms timeout
                plainText = (string)Dispatcher.Invoke(DispatcherPriority.Send, new TimeSpan(0, 0, 0, 0, 20), new DispatcherOperationCallback(delegate(object o) {
                    return GetPlainText();
                }), null);
            }

            // If there is plain text associated with this control, show it too.
            if (!String.IsNullOrEmpty(plainText))
            {
                return SR.Get(SRID.ToStringFormatString_Control, base.ToString(), plainText);
            }

            return base.ToString();
        }


        #endregion

        #region Events

        /// <summary>
        ///     PreviewMouseDoubleClick event
        /// </summary>
        public static readonly RoutedEvent PreviewMouseDoubleClickEvent = EventManager.RegisterRoutedEvent("PreviewMouseDoubleClick", RoutingStrategy.Direct, typeof(MouseButtonEventHandler), typeof(Control));

        /// <summary>
        ///     An event reporting a mouse button was pressed twice in a row.
        /// </summary>
        public event MouseButtonEventHandler PreviewMouseDoubleClick
        {
            add { AddHandler(PreviewMouseDoubleClickEvent, value); }
            remove { RemoveHandler(PreviewMouseDoubleClickEvent, value); }
        }

        /// <summary>
        ///     An event reporting a mouse button was pressed twice in a row.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected virtual void OnPreviewMouseDoubleClick(MouseButtonEventArgs e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        ///     MouseDoubleClick event
        /// </summary>
        public static readonly RoutedEvent MouseDoubleClickEvent = EventManager.RegisterRoutedEvent("MouseDoubleClick", RoutingStrategy.Direct, typeof(MouseButtonEventHandler), typeof(Control));

        /// <summary>
        ///     An event reporting a mouse button was pressed twice in a row.
        /// </summary>
        public event MouseButtonEventHandler MouseDoubleClick
        {
            add { AddHandler(MouseDoubleClickEvent, value); }
            remove { RemoveHandler(MouseDoubleClickEvent, value); }
        }

        /// <summary>
        ///     An event reporting a mouse button was pressed twice in a row.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected virtual void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            RaiseEvent(e);
        }

        private static void HandleDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                Control ctrl = (Control)sender;
                MouseButtonEventArgs doubleClick = new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, e.ChangedButton, e.StylusDevice);

                if ((e.RoutedEvent == UIElement.PreviewMouseLeftButtonDownEvent) ||
                    (e.RoutedEvent == UIElement.PreviewMouseRightButtonDownEvent))
                {
                    doubleClick.RoutedEvent = PreviewMouseDoubleClickEvent;
                    doubleClick.Source = e.OriginalSource; // Set OriginalSource because initially is null
                    doubleClick.OverrideSource(e.Source);
                    ctrl.OnPreviewMouseDoubleClick(doubleClick);
                }
                else
                {
                    doubleClick.RoutedEvent = MouseDoubleClickEvent;
                    doubleClick.Source = e.OriginalSource; // Set OriginalSource because initially is null
                    doubleClick.OverrideSource(e.Source);
                    ctrl.OnMouseDoubleClick(doubleClick);
                }

                // If MouseDoubleClick event is handled - we delegate the state to original MouseButtonEventArgs
                if (doubleClick.Handled)
                    e.Handled = true;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Suspends visual state changes.
        /// </summary>
        internal override void OnPreApplyTemplate()
        {
            VisualStateChangeSuspended = true;
            base.OnPreApplyTemplate();
        }

        /// <summary>
        /// Restores visual state changes & updates the visual state without transitions.
        /// </summary>
        internal override void OnPostApplyTemplate()
        {
            base.OnPostApplyTemplate();

            VisualStateChangeSuspended = false;
            UpdateVisualState(false);
        }


        /// <summary>
        /// Update the current visual state of the control using transitions
        /// </summary>
        internal void UpdateVisualState()
        {
            UpdateVisualState(true);
        }

        /// <summary>
        /// Update the current visual state of the control
        /// </summary>
        /// <param name="useTransitions">
        /// true to use transitions when updating the visual state, false to
        /// snap directly to the new visual state.
        /// </param>
        internal void UpdateVisualState(bool useTransitions)
        {
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordGeneral | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info, EventTrace.Event.UpdateVisualStateStart);
            if (!VisualStateChangeSuspended)
            {
                ChangeVisualState(useTransitions);
            }
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordGeneral | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info, EventTrace.Event.UpdateVisualStateEnd);
        }

        /// <summary>
        ///     Change to the correct visual state for the Control.
        /// </summary>
        /// <param name="useTransitions">
        ///     true to use transitions when updating the visual state, false to
        ///     snap directly to the new visual state.
        /// </param>
        internal virtual void ChangeVisualState(bool useTransitions)
        {
            ChangeValidationVisualState(useTransitions);
        }

        /// <summary>
        ///     Common code for putting a control in the validation state.  Controls that use the should register
        ///     for change notification of Validation.HasError.
        /// </summary>
        /// <param name="useTransitions"></param>
        internal void ChangeValidationVisualState(bool useTransitions)
        {
            if (Validation.GetHasError(this))
            {
                if (IsKeyboardFocused)
                {
                    VisualStateManager.GoToState(this, VisualStates.StateInvalidFocused, useTransitions);
                }
                else
                {
                    VisualStateManager.GoToState(this, VisualStates.StateInvalidUnfocused, useTransitions);
                }
            }
            else
            {
                VisualStateManager.GoToState(this, VisualStates.StateValid, useTransitions);
            }
        }


        internal static void OnVisualStatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Due to inherited properties, its safer not to cast to control because this might get fired for
            // non-controls.
            var control = d as Control;
            if (control != null)
            {
                control.UpdateVisualState();
            }
        }

        /// <summary>
        ///     Default control measurement is to measure only the first visual child.
        ///     This child would have been created by the inflation of the
        ///     visual tree from the control's style.
        ///
        ///     Derived controls may want to override this behavior.
        /// </summary>
        /// <param name="constraint">The measurement constraints.</param>
        /// <returns>The desired size of the control.</returns>
        protected override Size MeasureOverride(Size constraint)
        {
            int count = this.VisualChildrenCount;

            if (count > 0)
            {
                UIElement child = (UIElement)(this.GetVisualChild(0));
                if (child != null)
                {
                    child.Measure(constraint);
                    return child.DesiredSize;
                }
            }

            return new Size(0.0, 0.0);
        }

        /// <summary>
        ///     Default control arrangement is to only arrange
        ///     the first visual child. No transforms will be applied.
        /// </summary>
        /// <param name="arrangeBounds">The computed size.</param>
        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            int count = this.VisualChildrenCount;

            if (count>0)
            {
                UIElement child = (UIElement)(this.GetVisualChild(0));
                if (child != null)
                {
                    child.Arrange(new Rect(arrangeBounds));
                }
            }
            return arrangeBounds;
        }

        internal bool ReadControlFlag(ControlBoolFlags reqFlag)
        {
            return (_controlBoolField & reqFlag) != 0;
        }

        internal void WriteControlFlag(ControlBoolFlags reqFlag, bool set)
        {
            if (set)
            {
                _controlBoolField |= reqFlag;
            }
            else
            {
                _controlBoolField &= (~reqFlag);
            }
        }

        #endregion Methods

        #region Data

        internal enum ControlBoolFlags : ushort
        {
            ContentIsNotLogical                 = 0x0001,            // used in contentcontrol.cs
            IsSpaceKeyDown                      = 0x0002,            // used in ButtonBase.cs
            HeaderIsNotLogical                  = 0x0004,            // used in HeaderedContentControl.cs, HeaderedItemsControl.cs
            CommandDisabled                     = 0x0008,            // used in ButtonBase.cs, MenuItem.cs
            ContentIsItem                       = 0x0010,            // used in contentcontrol.cs
            HeaderIsItem                        = 0x0020,            // used in HeaderedContentControl.cs, HeaderedItemsControl.cs
            ScrollHostValid                     = 0x0040,            // used in ItemsControl.cs
            ContainsSelection                   = 0x0080,            // used in TreeViewItem.cs
            VisualStateChangeSuspended          = 0x0100,            // used in Control.cs
        }

        // Property caches
        private ControlTemplate         _templateCache;
        internal ControlBoolFlags       _controlBoolField;   // Cache valid bits

        #endregion
    }
}

