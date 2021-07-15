// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.ComponentModel;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Threading;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using MS.Utility;
using MS.Internal.Telemetry.PresentationFramework;

using System;

namespace System.Windows.Controls
{
    /// <summary>
    ///     TabControl allows a developer to arrange visual content in a compacted and organized form.
    /// The real-world analog of the control might be a tabbed notebook,
    /// in which visual content is displayed in discreet pages which are accessed
    /// by selecting the appropriate tab.  Each tab/page is encapsulated by a TabItem,
    /// the generated item of TabControl.
    /// A TabItem has a Header property which corresponds to the content in the tab button
    /// and a Content property which corresponds to the content in the tab page.
    /// This control is useful for minimizing screen space usage while allowing an application to expose a large amount of data.
    /// The user navigates through TabItems by clicking on a tab button using the mouse or by using the keyboard.
    /// </summary>
    [StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(TabItem))]
    [TemplatePart(Name = "PART_SelectedContentHost", Type = typeof(ContentPresenter))]
    public class TabControl : Selector
    {
        #region Constructors

        static TabControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TabControl), new FrameworkPropertyMetadata(typeof(TabControl)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(TabControl));
            IsTabStopProperty.OverrideMetadata(typeof(TabControl), new FrameworkPropertyMetadata(MS.Internal.KnownBoxes.BooleanBoxes.FalseBox));
            KeyboardNavigation.DirectionalNavigationProperty.OverrideMetadata(typeof(TabControl), new FrameworkPropertyMetadata(KeyboardNavigationMode.Contained));

            IsEnabledProperty.OverrideMetadata(typeof(TabControl), new UIPropertyMetadata(new PropertyChangedCallback(OnVisualStatePropertyChanged)));

            ControlsTraceLogger.AddControl(TelemetryControls.TabControl);
        }

        /// <summary>
        ///     Default TabControl constructor
        /// </summary>
        /// <remarks>
        ///     Automatic determination of current Dispatcher. Use alternative constructor
        ///     that accepts a Dispatcher for best performance.
        /// </remarks>
        public TabControl() : base()
        {
        }

        #endregion

        #region Properties

        /// <summary>
        ///     The DependencyProperty for the TabStripPlacement property.
        ///     Flags:              None
        ///     Default Value:      Dock.Top
        /// </summary>
        public static readonly DependencyProperty TabStripPlacementProperty =
                    DependencyProperty.Register(
                            "TabStripPlacement",
                            typeof(Dock),
                            typeof(TabControl),
                            new FrameworkPropertyMetadata(
                                    Dock.Top,
                                    new PropertyChangedCallback(OnTabStripPlacementPropertyChanged)),
                            new ValidateValueCallback(DockPanel.IsValidDock));

        // When TabControl TabStripPlacement is changing we need to invalidate its TabItem TabStripPlacement
        private static void OnTabStripPlacementPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TabControl tc = (TabControl)d;
            ItemCollection tabItemCollection = tc.Items;
            for (int i = 0; i < tabItemCollection.Count; i++)
            {
                TabItem ti = tc.ItemContainerGenerator.ContainerFromIndex(i) as TabItem;
                if (ti != null)
                    ti.CoerceValue(TabItem.TabStripPlacementProperty);
            }
        }

        /// <summary>
        ///     TabStripPlacement specify how tab headers align relatively to content
        /// </summary>
        [Bindable(true), Category("Behavior")]
        public Dock TabStripPlacement
        {
            get
            {
                return (Dock)GetValue(TabStripPlacementProperty);
            }
            set
            {
                SetValue(TabStripPlacementProperty, value);
            }
        }

        private static readonly DependencyPropertyKey SelectedContentPropertyKey = DependencyProperty.RegisterReadOnly("SelectedContent", typeof(object), typeof(TabControl), new FrameworkPropertyMetadata((object)null));

        /// <summary>
        ///     The DependencyProperty for the SelectedContent property.
        ///     Flags:              None
        ///     Default Value:      null
        /// </summary>
        public static readonly DependencyProperty SelectedContentProperty = SelectedContentPropertyKey.DependencyProperty;

        /// <summary>
        ///     SelectedContent is the Content of current SelectedItem.
        /// This property is updated whenever the selection is changed.
        /// It always keeps a reference to active TabItem.Content
        /// Used for aliasing in default TabControl Style
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public object SelectedContent
        {
            get
            {
                return GetValue(SelectedContentProperty);
            }
            internal set
            {
                SetValue(SelectedContentPropertyKey, value);
            }
        }

        private static readonly DependencyPropertyKey SelectedContentTemplatePropertyKey = DependencyProperty.RegisterReadOnly("SelectedContentTemplate", typeof(DataTemplate), typeof(TabControl), new FrameworkPropertyMetadata((DataTemplate)null));

        /// <summary>
        ///     The DependencyProperty for the SelectedContentTemplate property.
        ///     Flags:              None
        ///     Default Value:      null
        /// </summary>
        public static readonly DependencyProperty SelectedContentTemplateProperty = SelectedContentTemplatePropertyKey.DependencyProperty;

        /// <summary>
        ///     SelectedContentTemplate is the ContentTemplate of current SelectedItem.
        /// This property is updated whenever the selection is changed.
        /// It always keeps a reference to active TabItem.ContentTemplate
        /// It is used for aliasing in default TabControl Style
        /// </summary>
        /// <value></value>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DataTemplate SelectedContentTemplate
        {
            get
            {
                return (DataTemplate)GetValue(SelectedContentTemplateProperty);
            }
            internal set
            {
                SetValue(SelectedContentTemplatePropertyKey, value);
            }
        }

        private static readonly DependencyPropertyKey SelectedContentTemplateSelectorPropertyKey = DependencyProperty.RegisterReadOnly("SelectedContentTemplateSelector", typeof(DataTemplateSelector), typeof(TabControl), new FrameworkPropertyMetadata((DataTemplateSelector)null));

        /// <summary>
        ///     The DependencyProperty for the SelectedContentTemplateSelector property.
        ///     Flags:              None
        ///     Default Value:      null
        /// </summary>
        public static readonly DependencyProperty SelectedContentTemplateSelectorProperty = SelectedContentTemplateSelectorPropertyKey.DependencyProperty;

        /// <summary>
        ///     SelectedContentTemplateSelector allows the app writer to provide custom style selection logic.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DataTemplateSelector SelectedContentTemplateSelector
        {
            get
            {
                return (DataTemplateSelector)GetValue(SelectedContentTemplateSelectorProperty);
            }
            internal set
            {
                SetValue(SelectedContentTemplateSelectorPropertyKey, value);
            }
        }

        private static readonly DependencyPropertyKey SelectedContentStringFormatPropertyKey =
                DependencyProperty.RegisterReadOnly("SelectedContentStringFormat",
                        typeof(String),
                        typeof(TabControl),
                        new FrameworkPropertyMetadata((String)null));

        /// <summary>
        ///     The DependencyProperty for the SelectedContentStringFormat property.
        ///     Flags:              None
        ///     Default Value:      null
        /// </summary>
        public static readonly DependencyProperty SelectedContentStringFormatProperty =
                SelectedContentStringFormatPropertyKey.DependencyProperty;


        /// <summary>
        ///     ContentStringFormat is the format used to display the content of
        ///     the control as a string.  This arises only when no template is
        ///     available.
        /// </summary>
        public String SelectedContentStringFormat
        {
            get { return (String) GetValue(SelectedContentStringFormatProperty); }
            internal set { SetValue(SelectedContentStringFormatPropertyKey, value); }
        }


        /// <summary>
        ///     The DependencyProperty for the ContentTemplate property.
        ///     Flags:              None
        ///     Default Value:      null
        /// </summary>
        public static readonly DependencyProperty ContentTemplateProperty = DependencyProperty.Register("ContentTemplate", typeof(DataTemplate), typeof(TabControl), new FrameworkPropertyMetadata((DataTemplate)null));

        /// <summary>
        /// ContentTemplate is the ContentTemplate to apply to TabItems
        /// that do not have the ContentTemplate or ContentTemplateSelector properties
        /// defined
        /// </summary>
        /// <value></value>
        public DataTemplate ContentTemplate
        {
            get
            {
                return (DataTemplate)GetValue(ContentTemplateProperty);
            }
            set
            {
                SetValue(ContentTemplateProperty, value);
            }
        }

        /// <summary>
        ///     The DependencyProperty for the ContentTemplateSelector property.
        ///     Flags:              None
        ///     Default Value:      null
        /// </summary>
        public static readonly DependencyProperty ContentTemplateSelectorProperty = DependencyProperty.Register("ContentTemplateSelector", typeof(DataTemplateSelector), typeof(TabControl), new FrameworkPropertyMetadata((DataTemplateSelector)null));

        /// <summary>
        ///     ContentTemplateSelector allows the app writer to provide custom style selection logic.
        /// </summary>
        public DataTemplateSelector ContentTemplateSelector
        {
            get
            {
                return (DataTemplateSelector)GetValue(ContentTemplateSelectorProperty);
            }
            set
            {
                SetValue(ContentTemplateSelectorProperty, value);
            }
        }

        /// <summary>
        ///     The DependencyProperty for the ContentStringFormat property.
        ///     Flags:              None
        ///     Default Value:      null
        /// </summary>
        public static readonly DependencyProperty ContentStringFormatProperty =
                DependencyProperty.Register(
                        "ContentStringFormat",
                        typeof(String),
                        typeof(TabControl),
                        new FrameworkPropertyMetadata((String) null));


        /// <summary>
        ///     ContentStringFormat is the format used to display the content of
        ///     the control as a string.  This arises only when no template is
        ///     available.
        /// </summary>
        public String ContentStringFormat
        {
            get { return (String) GetValue(ContentStringFormatProperty); }
            set { SetValue(ContentStringFormatProperty, value); }
        }

        #endregion

        #region Overrided Methods

        internal override void ChangeVisualState(bool useTransitions)
        {
            if (!IsEnabled)
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateDisabled, VisualStates.StateNormal);
            }
            else
            {
                VisualStateManager.GoToState(this, VisualStates.StateNormal, useTransitions);
            }

            base.ChangeVisualState(useTransitions);
        }

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new TabControlAutomationPeer(this);
        }

        /// <summary>
        ///     This virtual method in called when IsInitialized is set to true and it raises an Initialized event
        /// </summary>
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            CanSelectMultiple = false;
            ItemContainerGenerator.StatusChanged += new EventHandler(OnGeneratorStatusChanged);
        }
        /// <summary>
        /// Called when the Template's tree has been generated. When Template gets expanded we ensure that SelectedContent is in sync
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            UpdateSelectedContent();
        }

        /// <summary>
        /// A virtual function that is called when the selection is changed. Default behavior
        /// is to raise a SelectionChangedEvent
        /// </summary>
        /// <param name="e">The inputs for this event. Can be raised (default behavior) or processed
        ///   in some other way.</param>
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            if (MS.Internal.FrameworkAppContextSwitches.SelectionPropertiesCanLagBehindSelectionChangedEvent)
            {
                // old ("useless") behavior, retained for app-compat
                base.OnSelectionChanged(e);
                if (IsKeyboardFocusWithin)
                {
                    // If keyboard focus is within the control, make sure it is going to the correct place
                    TabItem item = GetSelectedTabItem();
                    if (item != null)
                    {
                        item.SetFocus();
                    }
                }
                UpdateSelectedContent();
            }
            else
            {
                // new behavior - change SelectedContent and focus
                // before raising SelectionChanged.
                bool isKeyboardFocusWithin = IsKeyboardFocusWithin;

                UpdateSelectedContent();
                if (isKeyboardFocusWithin)
                {
                    // If keyboard focus is within the control, make sure it is going to the correct place
                    TabItem item = GetSelectedTabItem();
                    if (item != null)
                    {
                        item.SetFocus();
                    }
                }
                base.OnSelectionChanged(e);
            }

            if (    AutomationPeer.ListenerExists(AutomationEvents.SelectionPatternOnInvalidated)
                ||  AutomationPeer.ListenerExists(AutomationEvents.SelectionItemPatternOnElementSelected)
                ||  AutomationPeer.ListenerExists(AutomationEvents.SelectionItemPatternOnElementAddedToSelection)
                ||  AutomationPeer.ListenerExists(AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection)   )
            {
                TabControlAutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(this) as TabControlAutomationPeer;
                if (peer != null)
                    peer.RaiseSelectionEvents(e);
            }
        }

        /// <summary>
        /// Updates the current selection when Items has changed
        /// </summary>
        /// <param name="e">Information about what has changed</param>
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);
            if (e.Action == NotifyCollectionChangedAction.Remove && SelectedIndex == -1)
            {
                // If we remove the selected item we should select the previous item
                int startIndex = e.OldStartingIndex + 1;
                if (startIndex > Items.Count)
                    startIndex = 0;
                TabItem nextTabItem = FindNextTabItem(startIndex, -1);
                if (nextTabItem != null)
                    nextTabItem.SetCurrentValueInternal(TabItem.IsSelectedProperty, MS.Internal.KnownBoxes.BooleanBoxes.TrueBox);
            }
        }

        /// <summary>
        /// This is the method that responds to the KeyDown event.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            TabItem nextTabItem = null;

            // Handle [Ctrl][Shift]Tab, Home and End cases
            // We have special handling here because if focus is inside the TabItem content we cannot
            // cycle through TabItem because the content is not part of the TabItem visual tree

            int direction = 0;
            int startIndex = -1;
            switch (e.Key)
            {
                case Key.Tab:
                    if ((e.KeyboardDevice.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    {
                        startIndex = ItemContainerGenerator.IndexFromContainer(ItemContainerGenerator.ContainerFromItem(SelectedItem));
                        if ((e.KeyboardDevice.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                            direction = -1;
                        else
                            direction = 1;
                    }
                    break;
                case Key.Home:
                    direction = 1;
                    startIndex = -1;
                    break;
                case Key.End:
                    direction = -1;
                    startIndex = Items.Count;
                    break;
            }

            nextTabItem = FindNextTabItem(startIndex, direction);

            if (nextTabItem != null && nextTabItem != SelectedItem)
            {
                e.Handled = nextTabItem.SetFocus();
            }

            if (!e.Handled)
                base.OnKeyDown(e);
        }

        private TabItem FindNextTabItem(int startIndex, int direction)
        {
            TabItem nextTabItem = null;
            if (direction != 0)
            {
                int index = startIndex;
                for (int i = 0; i < Items.Count; i++)
                {
                    index += direction;
                    if (index >= Items.Count)
                        index = 0;
                    else if (index < 0)
                        index = Items.Count - 1;

                    TabItem tabItem = ItemContainerGenerator.ContainerFromIndex(index) as TabItem;
                    if (tabItem != null && tabItem.IsEnabled && tabItem.Visibility == Visibility.Visible)
                    {
                        nextTabItem = tabItem;
                        break;
                    }
                }
            }
            return nextTabItem;
        }

        /// <summary>
        /// Return true if the item is (or is eligible to be) its own ItemUI
        /// </summary>
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return (item is TabItem);
        }

        /// <summary> Create or identify the element used to display the given item. </summary>
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new TabItem();
        }

        #endregion

        #region private helpers

        internal ContentPresenter SelectedContentPresenter
        {
            get
            {
                return GetTemplateChild(SelectedContentHostTemplateName) as ContentPresenter;
            }
        }

        private void OnGeneratorStatusChanged(object sender, EventArgs e)
        {
            if (ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                if (HasItems && _selectedItems.Count == 0)
                {
                    SetCurrentValueInternal(SelectedIndexProperty, 0);
                }

                UpdateSelectedContent();
            }
        }

        private TabItem GetSelectedTabItem()
        {
            object selectedItem = SelectedItem;
            if (selectedItem != null)
            {
                // Check if the selected item is a TabItem
                TabItem tabItem = selectedItem as TabItem;
                if (tabItem == null)
                {
                    // It is a data item, get its TabItem container
                    tabItem = ItemContainerGenerator.ContainerFromIndex(SelectedIndex) as TabItem;

                    // Due to event leapfrogging, we may have the wrong container.
                    // If so, re-fetch the right container using a more expensive method.
                    // (BTW, the previous line will cause a debug assert in this case) 
                    if (tabItem == null ||
                        !ItemsControl.EqualsEx(selectedItem, ItemContainerGenerator.ItemFromContainer(tabItem)))
                    {
                        tabItem = ItemContainerGenerator.ContainerFromItem(selectedItem) as TabItem;
                    }
                }

                return tabItem;
            }

            return null;
        }

        // When selection is changed we need to copy the active TabItem content in SelectedContent property
        // SelectedContent is aliased in the TabControl style
        private void UpdateSelectedContent()
        {
            if (SelectedIndex < 0)
            {
                SelectedContent = null;
                SelectedContentTemplate = null;
                SelectedContentTemplateSelector = null;
                SelectedContentStringFormat = null;
                return;
            }

            TabItem tabItem = GetSelectedTabItem();
            if (tabItem != null)
            {
                FrameworkElement visualParent = VisualTreeHelper.GetParent(tabItem) as FrameworkElement;

                if (visualParent != null)
                {
                    KeyboardNavigation.SetTabOnceActiveElement(visualParent, tabItem);
                    KeyboardNavigation.SetTabOnceActiveElement(this, visualParent);
                }

                SelectedContent = tabItem.Content;
                ContentPresenter scp = SelectedContentPresenter;
                if (scp != null)
                {
                    scp.HorizontalAlignment = tabItem.HorizontalContentAlignment;
                    scp.VerticalAlignment = tabItem.VerticalContentAlignment;
                }

                // Use tabItem's template or selector if specified, otherwise use TabControl's
                if (tabItem.ContentTemplate != null || tabItem.ContentTemplateSelector != null || tabItem.ContentStringFormat != null)
                {
                    SelectedContentTemplate = tabItem.ContentTemplate;
                    SelectedContentTemplateSelector = tabItem.ContentTemplateSelector;
                    SelectedContentStringFormat = tabItem.ContentStringFormat;
                }
                else
                {
                    SelectedContentTemplate = ContentTemplate;
                    SelectedContentTemplateSelector = ContentTemplateSelector;
                    SelectedContentStringFormat = ContentStringFormat;
                }
             }
        }

        #endregion private helpers

        #region private data

        // Part name used in the style. The class TemplatePartAttribute should use the same name
        private const string SelectedContentHostTemplateName = "PART_SelectedContentHost";

        #endregion

        #region DTypeThemeStyleKey

        // Returns the DependencyObjectType for the registered ThemeStyleKey's default
        // value. Controls will override this method to return approriate types.
        internal override DependencyObjectType DTypeThemeStyleKey
        {
            get { return _dType; }
        }

        private static DependencyObjectType _dType;

        #endregion DTypeThemeStyleKey
    }
}

