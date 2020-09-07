// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Threading;

using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;

using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Markup;
using System.Windows.Shapes;

using MS.Utility;
using MS.Internal.KnownBoxes;
using MS.Internal.Telemetry.PresentationFramework;

namespace System.Windows.Controls
{
    #region public enum types

    /// <summary>
    /// Defines how we place the toolbar items
    /// </summary>
    public enum OverflowMode
    {
        /// <summary>
        /// specifies that the item moves between the main and the overflow panels as space permits
        /// </summary>
        AsNeeded,

        /// <summary>
        /// specifies that the item is permanently placed in the overflow panel
        /// </summary>
        Always,

        /// <summary>
        /// specifies that the item is never allowed to overflow
        /// </summary>
        Never

        // NOTE: if you add or remove any values in this enum, be sure to update ToolBar.IsValidOverflowMode()
    }

    #endregion public enum types

    /// <summary>
    ///     ToolBar provides an overflow mechanism which places any items that doesnt fit naturally
    /// fit within a size-constrained ToolBar into a special overflow area.
    /// Also, ToolBars have a tight relationship with the related ToolBarTray control.
    /// </summary>
    [TemplatePart(Name = "PART_ToolBarPanel", Type = typeof(ToolBarPanel))]
    [TemplatePart(Name = "PART_ToolBarOverflowPanel", Type = typeof(ToolBarOverflowPanel))]
    public class ToolBar : HeaderedItemsControl
    {
        #region Constructors

        static ToolBar()
        {
            // Disable tooltips on toolbar when the overflow is open
            ToolTipService.IsEnabledProperty.OverrideMetadata(typeof(ToolBar), new FrameworkPropertyMetadata(null, new CoerceValueCallback(CoerceToolTipIsEnabled)));

            DefaultStyleKeyProperty.OverrideMetadata(typeof(ToolBar), new FrameworkPropertyMetadata(typeof(ToolBar)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(ToolBar));

            IsTabStopProperty.OverrideMetadata(typeof(ToolBar), new FrameworkPropertyMetadata(MS.Internal.KnownBoxes.BooleanBoxes.FalseBox));
            FocusableProperty.OverrideMetadata(typeof(ToolBar), new FrameworkPropertyMetadata(MS.Internal.KnownBoxes.BooleanBoxes.FalseBox));
            KeyboardNavigation.DirectionalNavigationProperty.OverrideMetadata(typeof(ToolBar), new FrameworkPropertyMetadata(KeyboardNavigationMode.Cycle));
            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(ToolBar), new FrameworkPropertyMetadata(KeyboardNavigationMode.Cycle));
            KeyboardNavigation.ControlTabNavigationProperty.OverrideMetadata(typeof(ToolBar), new FrameworkPropertyMetadata(KeyboardNavigationMode.Once));
            FocusManager.IsFocusScopeProperty.OverrideMetadata(typeof(ToolBar), new FrameworkPropertyMetadata(BooleanBoxes.TrueBox));

            EventManager.RegisterClassHandler(typeof(ToolBar), Mouse.MouseDownEvent, new MouseButtonEventHandler(OnMouseButtonDown), true);
            EventManager.RegisterClassHandler(typeof(ToolBar), ButtonBase.ClickEvent, new RoutedEventHandler(_OnClick));

            ControlsTraceLogger.AddControl(TelemetryControls.ToolBar);
        }

        /// <summary>
        ///     Default ToolBar constructor
        /// </summary>
        /// <remarks>
        ///     Automatic determination of current Dispatcher. Use alternative constructor
        ///     that accepts a Dispatcher for best performance.
        /// </remarks>
        public ToolBar() : base()
        {
        }

        #endregion

        #region Properties

        #region Orientation
        /// <summary>
        ///     Property key for OrientationProperty.
        /// </summary>
        private static readonly DependencyPropertyKey OrientationPropertyKey =
                DependencyProperty.RegisterAttachedReadOnly(
                        "Orientation",
                        typeof(Orientation),
                        typeof(ToolBar),
                        new FrameworkPropertyMetadata(
                                Orientation.Horizontal,
                                null,
                                new CoerceValueCallback(CoerceOrientation)));

        /// <summary>
        /// Specifies the orientation (horizontal or vertical) of the flow
        /// </summary>
        public static readonly DependencyProperty OrientationProperty =
            OrientationPropertyKey.DependencyProperty;

        private static object CoerceOrientation(DependencyObject d, object value)
        {
            ToolBarTray toolBarTray = ((ToolBar) d).ToolBarTray;
            return (toolBarTray != null) ? toolBarTray.Orientation : value;
        }

        /// <summary>
        /// Specifies the orientation of the ToolBar. This read-only property get its value from the ToolBarTray parent
        /// </summary>
        public Orientation Orientation
        {
            get
            {
                return (Orientation) GetValue(OrientationProperty);
            }
        }
        #endregion Orientation

        #region Band
        /// <summary>
        /// Specify the band number where ToolBar should be located withing the ToolBarTray
        /// </summary>
        public static readonly DependencyProperty BandProperty =
                DependencyProperty.Register(
                        "Band",
                        typeof(int),
                        typeof(ToolBar),
                        new FrameworkPropertyMetadata(
                                0,
                                FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        /// <summary>
        /// Specify the band number where ToolBar should be located withing the ToolBarTray
        /// </summary>
        /// <value></value>
        public int Band
        {
            get { return (int) GetValue(BandProperty); }
            set { SetValue(BandProperty, value); }
        }
        #endregion Band

        #region BandIndex
        /// <summary>
        /// Specify the band index number where ToolBar should be located within the band of ToolBarTray
        /// </summary>
        public static readonly DependencyProperty BandIndexProperty =
                DependencyProperty.Register(
                        "BandIndex",
                        typeof(int),
                        typeof(ToolBar),
                        new FrameworkPropertyMetadata(
                                0,
                                FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        /// <summary>
        /// Specify the band index number where ToolBar should be located withing the band of ToolBarTray
        /// </summary>
        /// <value></value>
        public int BandIndex
        {
            get { return (int) GetValue(BandIndexProperty); }
            set { SetValue(BandIndexProperty, value); }
        }
        #endregion BandIndex

        #region IsOverflowOpen
        /// <summary>
        /// DependencyProperty for IsOverflowOpen
        /// </summary>
        public static readonly DependencyProperty IsOverflowOpenProperty =
                DependencyProperty.Register(
                        "IsOverflowOpen",
                        typeof(bool),
                        typeof(ToolBar),
                        new FrameworkPropertyMetadata(
                                BooleanBoxes.FalseBox,
                                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                                new PropertyChangedCallback(OnOverflowOpenChanged),
                                new CoerceValueCallback(CoerceIsOverflowOpen)));

        /// <summary>
        /// Whether or not the "popup" for this control is currently open
        /// </summary>
        [Bindable(true), Browsable(false), Category("Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsOverflowOpen
        {
            get { return (bool) GetValue(IsOverflowOpenProperty); }
            set { SetValue(IsOverflowOpenProperty, BooleanBoxes.Box(value)); }
        }

        private static object CoerceIsOverflowOpen(DependencyObject d, object value)
        {
            if ((bool)value)
            {
                ToolBar tb = (ToolBar)d;
                if (!tb.IsLoaded)
                {
                    tb.RegisterToOpenOnLoad();
                    return BooleanBoxes.FalseBox;
                }
            }

            return value;
        }

        private static object CoerceToolTipIsEnabled(DependencyObject d, object value)
        {
            ToolBar tb = (ToolBar) d;
            return tb.IsOverflowOpen ? BooleanBoxes.FalseBox : value;
        }

        private void RegisterToOpenOnLoad()
        {
            Loaded += new RoutedEventHandler(OpenOnLoad);
        }

        private void OpenOnLoad(object sender, RoutedEventArgs e)
        {
            // Open overflow after toolbar has rendered (Loaded is fired before 1st render)
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new DispatcherOperationCallback(delegate(object param)
            {
                CoerceValue(IsOverflowOpenProperty);

                return null;
            }), null);
        }

        private static void OnOverflowOpenChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
        {
            ToolBar toolBar = (ToolBar) element;

            if ((bool) e.NewValue)
            {
                // When the drop down opens, take capture
                Mouse.Capture(toolBar, CaptureMode.SubTree);
                toolBar.SetFocusOnToolBarOverflowPanel();
            }
            else
            {
                // If focus is still within the ToolBarOverflowPanel, make sure we the focus is restored to the main focus scope
                ToolBarOverflowPanel overflow = toolBar.ToolBarOverflowPanel;
                if (overflow != null && overflow.IsKeyboardFocusWithin)
                {
                    Keyboard.Focus(null);
                }

                if (Mouse.Captured == toolBar)
                {
                    Mouse.Capture(null);
                }
            }

            toolBar.CoerceValue(ToolTipService.IsEnabledProperty);
        }

        private void SetFocusOnToolBarOverflowPanel()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new DispatcherOperationCallback(delegate(object param)
            {
                if (ToolBarOverflowPanel != null)
                {
                    // If the overflow is opened by keyboard - focus the first item
                    // otherwise - set focus on the panel itself
                    if (KeyboardNavigation.IsKeyboardMostRecentInputDevice())
                    {
                        ToolBarOverflowPanel.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    }
                    else
                    {
                        ToolBarOverflowPanel.Focus();
                    }
                }
                return null;
            }), null);
        }

        #endregion IsOverflowOpen

        #region HasOverflowItems

        /// <summary>
        ///     The key needed set a read-only property.
        /// </summary>
        internal static readonly DependencyPropertyKey HasOverflowItemsPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "HasOverflowItems",
                        typeof(bool),
                        typeof(ToolBar),
                        new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        ///     The DependencyProperty for the HasOverflowItems property.
        ///     Flags:              None
        ///     Default Value:      false
        /// </summary>
        public static readonly DependencyProperty HasOverflowItemsProperty =
                HasOverflowItemsPropertyKey.DependencyProperty;

        /// <summary>
        /// Whether we have overflow items
        /// </summary>
        public bool HasOverflowItems
        {
            get { return (bool) GetValue(HasOverflowItemsProperty); }
        }
        #endregion HasOverflowItems

        #region IsOverflowItem
        /// <summary>
        ///     The key needed set a read-only property.
        /// Attached property to indicate if the item is placed in the overflow panel
        /// </summary>
        internal static readonly DependencyPropertyKey IsOverflowItemPropertyKey =
                DependencyProperty.RegisterAttachedReadOnly(
                        "IsOverflowItem",
                        typeof(bool),
                        typeof(ToolBar),
                        new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        ///     The DependencyProperty for the IsOverflowItem property.
        ///     Flags:              None
        ///     Default Value:      false
        /// </summary>
        public static readonly DependencyProperty IsOverflowItemProperty =
                IsOverflowItemPropertyKey.DependencyProperty;

        /// <summary>
        /// Writes the attached property IsOverflowItem to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        internal static void SetIsOverflowItem(DependencyObject element, object value)
        {
            element.SetValue(IsOverflowItemPropertyKey, value);
        }

        /// <summary>
        /// Reads the attached property IsOverflowItem from the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <returns>The property's value.</returns>
        public static bool GetIsOverflowItem(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (bool)element.GetValue(IsOverflowItemProperty);
        }

        #endregion

        #region OverflowMode

        /// <summary>
        /// Attached property to indicate if the item should be placed in the overflow panel
        /// </summary>
        public static readonly DependencyProperty OverflowModeProperty =
                DependencyProperty.RegisterAttached(
                        "OverflowMode",
                        typeof(OverflowMode),
                        typeof(ToolBar),
                        new FrameworkPropertyMetadata(
                                OverflowMode.AsNeeded,
                                new PropertyChangedCallback(OnOverflowModeChanged)),
                        new ValidateValueCallback(IsValidOverflowMode));

        private static void OnOverflowModeChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
        {
            // When OverflowMode changes on a child container of a ToolBar,
            // invalidate layout so that the child can be placed in the correct
            // location (in the main bar or the overflow menu).
            ToolBar toolBar = ItemsControl.ItemsControlFromItemContainer(element) as ToolBar;
            if (toolBar != null)
            {
                toolBar.InvalidateLayout();
            }
        }

        private void InvalidateLayout()
        {
            // Reset the calculated min and max size
            _minLength = 0.0;
            _maxLength = 0.0;

            // Min and max sizes are calculated in ToolBar.MeasureOverride
            InvalidateMeasure();

            ToolBarPanel toolBarPanel = this.ToolBarPanel;
            if (toolBarPanel != null)
            {
                // Whether elements are in the overflow or not is decided
                // in ToolBarPanel.MeasureOverride.
                toolBarPanel.InvalidateMeasure();
            }
        }

        private static bool IsValidOverflowMode(object o)
        {
            OverflowMode value = (OverflowMode)o;
            return value == OverflowMode.AsNeeded
                || value == OverflowMode.Always
                || value == OverflowMode.Never;
        }

        /// <summary>
        /// Writes the attached property OverflowMode to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="mode">The property value to set</param>
        public static void SetOverflowMode(DependencyObject element, OverflowMode mode)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(OverflowModeProperty, mode);
        }

        /// <summary>
        /// Reads the attached property OverflowMode from the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <returns>The property's value.</returns>
        [AttachedPropertyBrowsableForChildren(IncludeDescendants = true)]
        public static OverflowMode GetOverflowMode(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (OverflowMode)element.GetValue(OverflowModeProperty);
        }
        #endregion

        #endregion Properties

        #region Override methods

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ToolBarAutomationPeer(this);
        }

        /// <summary>
        /// Prepare the element to display the item.  This may involve
        /// applying styles, setting bindings, etc.
        /// </summary>
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            // For certain known types, automatically change their default style
            // to point to a ToolBar version.
            FrameworkElement fe = element as FrameworkElement;
            if (fe != null)
            {
                Type feType = fe.GetType();
                ResourceKey resourceKey = null;
                if (feType == typeof(Button))
                    resourceKey = ButtonStyleKey;
                else if (feType == typeof(ToggleButton))
                    resourceKey = ToggleButtonStyleKey;
                else if (feType == typeof(Separator))
                    resourceKey = SeparatorStyleKey;
                else if (feType == typeof(CheckBox))
                    resourceKey = CheckBoxStyleKey;
                else if (feType == typeof(RadioButton))
                    resourceKey = RadioButtonStyleKey;
                else if (feType == typeof(ComboBox))
                    resourceKey = ComboBoxStyleKey;
                else if (feType == typeof(TextBox))
                    resourceKey = TextBoxStyleKey;
                else if (feType == typeof(Menu))
                    resourceKey = MenuStyleKey;

                if (resourceKey != null)
                {
                    bool hasModifiers;
                    BaseValueSourceInternal vs = fe.GetValueSource(StyleProperty, null, out hasModifiers);

                    if (vs <= BaseValueSourceInternal.ImplicitReference)
                        fe.SetResourceReference(StyleProperty, resourceKey);
                    fe.DefaultStyleKey = resourceKey;
                }
            }
        }

        internal override void OnTemplateChangedInternal(FrameworkTemplate oldTemplate, FrameworkTemplate newTemplate)
        {
            // Invalidate template references
            _toolBarPanel = null;
            _toolBarOverflowPanel = null;

            base.OnTemplateChangedInternal(oldTemplate, newTemplate);
        }

        /// <summary>
        ///     This method is invoked when the Items property changes.
        /// </summary>
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            // When items change, invalidate layout so that the decision
            // regarding which items are in the overflow menu can be re-done.
            InvalidateLayout();

            base.OnItemsChanged(e);
        }

        /// <summary>
        /// Measure the content and store the desired size of the content
        /// </summary>
        /// <param name="constraint"></param>
        /// <returns></returns>
        protected override Size MeasureOverride(Size constraint)
        {
            // Perform a normal layout
            Size desiredSize = base.MeasureOverride(constraint);

            //
            // MinLength and MaxLength are used by ToolBarTray to determine
            // its layout. ToolBarPanel will calculate its version of these values.
            // ToolBar needs to add on the space used up by elements around the ToolBarPanel.
            //
            // Note: This calculation is not 100% accurate. If a scale transform is applied
            // within the template of the ToolBar (between the ToolBar and the ToolBarPanel),
            // then the coordinate spaces will not match and the values will be wrong.
            //
            // Note: If a ToolBarPanel is not contained within the ToolBar's template,
            // then these values will always be zero, and ToolBarTray will not layout correctly.
            //
            ToolBarPanel toolBarPanel = ToolBarPanel;
            if (toolBarPanel != null)
            {
                // Calculate the extra length from the extra space allocated between the ToolBar and the ToolBarPanel.
                double extraLength;
                Thickness margin = toolBarPanel.Margin;
                if (toolBarPanel.Orientation == Orientation.Horizontal)
                {
                    extraLength = Math.Max(0.0, desiredSize.Width - toolBarPanel.DesiredSize.Width + margin.Left + margin.Right);
                }
                else
                {
                    extraLength = Math.Max(0.0, desiredSize.Height - toolBarPanel.DesiredSize.Height + margin.Top + margin.Bottom);
                }

                // Add the calculated extra length to the lengths provided by ToolBarPanel
                _minLength = toolBarPanel.MinLength + extraLength;
                _maxLength = toolBarPanel.MaxLength + extraLength;
            }

            return desiredSize;
        }

        /// <summary>
        ///     Called when this element loses mouse capture.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            base.OnLostMouseCapture(e);

            // ToolBar has a capture when its overflow panel is open
            // close the overflow panel is case capture is set to null
            if (Mouse.Captured == null)
            {
                Close();
            }
        }

        #endregion Override methods

        #region Private implementation

        /// <summary>
        /// Gets reference to ToolBar's ToolBarPanel element.
        /// </summary>
        internal ToolBarPanel ToolBarPanel
        {
            get
            {
                if (_toolBarPanel == null)
                    _toolBarPanel = FindToolBarPanel();

                return _toolBarPanel;
            }
        }

        private ToolBarPanel FindToolBarPanel()
        {
            DependencyObject child = GetTemplateChild(ToolBarPanelTemplateName);
            ToolBarPanel toolBarPanel = child as ToolBarPanel;
            if (child != null && toolBarPanel == null)
                throw new NotSupportedException(SR.Get(SRID.ToolBar_InvalidStyle_ToolBarPanel, child.GetType()));
            return toolBarPanel;
        }

        /// <summary>
        /// Gets reference to ToolBar's ToolBarOverflowPanel element.
        /// </summary>
        internal ToolBarOverflowPanel ToolBarOverflowPanel
        {
            get
            {
                if (_toolBarOverflowPanel == null)
                    _toolBarOverflowPanel = FindToolBarOverflowPanel();

                return _toolBarOverflowPanel;
            }
        }

        private ToolBarOverflowPanel FindToolBarOverflowPanel()
        {
            DependencyObject child = GetTemplateChild(ToolBarOverflowPanelTemplateName);
            ToolBarOverflowPanel toolBarOverflowPanel = child as ToolBarOverflowPanel;
            if (child != null && toolBarOverflowPanel == null)
                throw new NotSupportedException(SR.Get(SRID.ToolBar_InvalidStyle_ToolBarOverflowPanel, child.GetType()));
            return toolBarOverflowPanel;
        }

        /// <summary>
        /// This is the method that responds to the KeyDown event.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            UIElement newFocusElement = null;
            UIElement currentFocusElement = e.Source as UIElement;
            if (currentFocusElement != null && ItemsControl.ItemsControlFromItemContainer(currentFocusElement) == this)
            {
                // itemsHost should be either ToolBarPanel or ToolBarOverflowPanel
                Panel itemsHost = VisualTreeHelper.GetParent(currentFocusElement) as Panel;
                if (itemsHost != null)
                {
                    switch (e.Key)
                    {
                        // itemsHost.Children.Count is greater than zero because itemsHost is visual parent of currentFocusElement
                        case Key.Home:
                            newFocusElement = VisualTreeHelper.GetChild(itemsHost, 0) as UIElement;
                            break;
                        case Key.End:
                            newFocusElement = VisualTreeHelper.GetChild(itemsHost, VisualTreeHelper.GetChildrenCount(itemsHost)-1) as UIElement;
                            break;
                        case Key.Escape:
                            {
                                // If focus is within ToolBarOverflowPanel - move focus the the toggle button
                                ToolBarOverflowPanel overflow = ToolBarOverflowPanel;
                                if (overflow != null && overflow.IsKeyboardFocusWithin)
                                {
                                    MoveFocus(new TraversalRequest(FocusNavigationDirection.Last));
                                }
                                else
                                {
                                    Keyboard.Focus(null);
                                }

                                // Close the overflow the Esc is pressed
                                Close();
                            }
                            break;
                    }

                    if (newFocusElement != null)
                    {
                        if (newFocusElement.Focus())
                            e.Handled = true;
                    }
                }
            }

            if (!e.Handled)
                base.OnKeyDown(e);
        }

        private static void OnMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            ToolBar toolBar = (ToolBar)sender;
            // Close the overflow for all unhandled mousedown in ToolBar
            if (!e.Handled)
            {
                toolBar.Close();
                e.Handled = true;
            }
        }

        // ButtonBase.Click class handler
        // When we get a click event from toolbar item - close the overflow panel
        private static void _OnClick(object e, RoutedEventArgs args)
        {
            ToolBar toolBar = (ToolBar)e;
            ButtonBase bb = args.OriginalSource as ButtonBase;
            if (toolBar.IsOverflowOpen && bb != null && bb.Parent == toolBar)
                toolBar.Close();
        }

        internal override void OnAncestorChanged()
        {
            // Orientation depends on the logical parent -- so invalidate it when that changes
            CoerceValue(OrientationProperty);
        }

        private void Close()
        {
            SetCurrentValueInternal(IsOverflowOpenProperty, BooleanBoxes.FalseBox);
        }

        private ToolBarTray ToolBarTray
        {
            get
            {
                return Parent as ToolBarTray;
            }
        }

        internal double MinLength
        {
            get { return _minLength; }
        }

        internal double MaxLength
        {
            get { return _maxLength; }
        }

        #endregion Private implementation

        #region private data
        private ToolBarPanel _toolBarPanel;
        private ToolBarOverflowPanel _toolBarOverflowPanel;

        private const string ToolBarPanelTemplateName = "PART_ToolBarPanel";
        private const string ToolBarOverflowPanelTemplateName = "PART_ToolBarOverflowPanel";

        private double _minLength = 0d;
        private double _maxLength = 0d;

        #endregion private data

        #region DTypeThemeStyleKey

        // Returns the DependencyObjectType for the registered ThemeStyleKey's default
        // value. Controls will override this method to return approriate types.
        internal override DependencyObjectType DTypeThemeStyleKey
        {
            get { return _dType; }
        }

        private static DependencyObjectType _dType;

        #endregion DTypeThemeStyleKey

        #region ItemsStyleKey
        /// <summary>
        ///     Resource Key for the ButtonStyle
        /// </summary>
        public static ResourceKey ButtonStyleKey
        {
            get
            {
                return SystemResourceKey.ToolBarButtonStyleKey;
            }
        }

        /// <summary>
        ///     Resource Key for the ToggleButtonStyle
        /// </summary>
        public static ResourceKey ToggleButtonStyleKey
        {
            get
            {
                return SystemResourceKey.ToolBarToggleButtonStyleKey;
            }
        }

        /// <summary>
        ///     Resource Key for the SeparatorStyle
        /// </summary>
        public static ResourceKey SeparatorStyleKey
        {
            get
            {
                return SystemResourceKey.ToolBarSeparatorStyleKey;
            }
        }

        /// <summary>
        ///     Resource Key for the CheckBoxStyle
        /// </summary>
        public static ResourceKey CheckBoxStyleKey
        {
            get
            {
                return SystemResourceKey.ToolBarCheckBoxStyleKey;
            }
        }

        /// <summary>
        ///     Resource Key for the RadioButtonStyle
        /// </summary>
        public static ResourceKey RadioButtonStyleKey
        {
            get
            {
                return SystemResourceKey.ToolBarRadioButtonStyleKey;
            }
        }

        /// <summary>
        ///     Resource Key for the ComboBoxStyle
        /// </summary>
        public static ResourceKey ComboBoxStyleKey
        {
            get
            {
                return SystemResourceKey.ToolBarComboBoxStyleKey;
            }
        }

        /// <summary>
        ///     Resource Key for the TextBoxStyle
        /// </summary>
        public static ResourceKey TextBoxStyleKey
        {
            get
            {
                return SystemResourceKey.ToolBarTextBoxStyleKey;
            }
        }

        /// <summary>
        ///     Resource Key for the MenuStyle
        /// </summary>
        public static ResourceKey MenuStyleKey
        {
            get
            {
                return SystemResourceKey.ToolBarMenuStyleKey;
            }
        }

        #endregion ItemsStyleKey
    }
}
