// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Diagnostics;
using MS.Internal;
using MS.Internal.KnownBoxes;
using MS.Utility;
using System.Windows.Threading;
using System.ComponentModel;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows;
using System.Windows.Data;

using System.Windows.Controls.Primitives;

// Disable CS3001: Warning as Error: not CLS-compliant
#pragma warning disable 3001

namespace System.Windows.Controls
{
    /// <summary>
    ///     A child item of TabControl.
    /// </summary>
    [DefaultEvent("IsSelectedChanged")]
    public class TabItem : HeaderedContentControl
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Default DependencyObject constructor
        /// </summary>
        /// <remarks>
        ///     Automatic determination of current Dispatcher. Use alternative constructor
        ///     that accepts a Dispatcher for best performance.
        /// </remarks>
        public TabItem() : base()
        {
        }

        static TabItem()
        {
            EventManager.RegisterClassHandler(typeof(TabItem), AccessKeyManager.AccessKeyPressedEvent, new AccessKeyPressedEventHandler(OnAccessKeyPressed));

            DefaultStyleKeyProperty.OverrideMetadata(typeof(TabItem), new FrameworkPropertyMetadata(typeof(TabItem)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(TabItem));
            KeyboardNavigation.DirectionalNavigationProperty.OverrideMetadata(typeof(TabItem), new FrameworkPropertyMetadata(KeyboardNavigationMode.Contained));
            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(TabItem), new FrameworkPropertyMetadata(KeyboardNavigationMode.Local));

            IsEnabledProperty.OverrideMetadata(typeof(TabItem), new UIPropertyMetadata(new PropertyChangedCallback(OnVisualStatePropertyChanged)));
            IsMouseOverPropertyKey.OverrideMetadata(typeof(TabItem), new UIPropertyMetadata(new PropertyChangedCallback(OnVisualStatePropertyChanged)));
            AutomationProperties.IsOffscreenBehaviorProperty.OverrideMetadata(typeof(TabItem), new FrameworkPropertyMetadata(IsOffscreenBehavior.FromClip));
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Properties
        //
        //-------------------------------------------------------------------

        #region Properties
        /// <summary>
        ///     Indicates whether this TabItem is selected.
        /// </summary>
        public static readonly DependencyProperty IsSelectedProperty =
                Selector.IsSelectedProperty.AddOwner(typeof(TabItem),
                        new FrameworkPropertyMetadata(BooleanBoxes.FalseBox,
                                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsParentMeasure | FrameworkPropertyMetadataOptions.Journal,
                                new PropertyChangedCallback(OnIsSelectedChanged)));

        /// <summary>
        ///     Indicates whether this TabItem is selected.
        /// </summary>
        [Bindable(true), Category("Appearance")]
        public bool IsSelected
        {
            get { return (bool) GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, BooleanBoxes.Box(value)); }
        }

        private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TabItem tabItem = d as TabItem;

            bool isSelected = (bool)e.NewValue;

            TabControl parentTabControl = tabItem.TabControlParent;
            if (parentTabControl != null)
            {
                parentTabControl.RaiseIsSelectedChangedAutomationEvent(tabItem, isSelected);
            }

            if (isSelected)
            {
                tabItem.OnSelected(new RoutedEventArgs(Selector.SelectedEvent, tabItem));
            }
            else
            {
                tabItem.OnUnselected(new RoutedEventArgs(Selector.UnselectedEvent, tabItem));
            }


            // KeyboardNavigation use bounding box reduced with DirectionalNavigationMargin when calculating the next element in directional navigation
            // Because TabItem use negative margins some TabItems overlap which would changes the directional navigation if we don't reduce the bounding box
            if (isSelected)
            {
                Binding binding = new Binding("Margin");
                binding.Source = tabItem;
                BindingOperations.SetBinding(tabItem, KeyboardNavigation.DirectionalNavigationMarginProperty, binding);
            }
            else
            {
                BindingOperations.ClearBinding(tabItem, KeyboardNavigation.DirectionalNavigationMarginProperty);
            }

            tabItem.UpdateVisualState();
        }

        /// <summary>
        ///     Event indicating that the IsSelected property is now true.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected virtual void OnSelected(RoutedEventArgs e)
        {
            HandleIsSelectedChanged(true, e);
        }

        /// <summary>
        ///     Event indicating that the IsSelected property is now false.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected virtual void OnUnselected(RoutedEventArgs e)
        {
            HandleIsSelectedChanged(false, e);
        }

        private void HandleIsSelectedChanged(bool newValue, RoutedEventArgs e)
        {
#if OLD_AUTOMATION
            if (AutomationProvider.IsActive)
            {
                RaiseAutomationIsSelectedChanged(!newValue, newValue);
            }
#endif

            RaiseEvent(e);
        }

#if OLD_AUTOMATION
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void RaiseAutomationIsSelectedChanged(bool oldValue, bool newValue)
        {
            AutomationProvider.RaiseAutomationPropertyChangedEvent(this, SelectionItemPatternIdentifiers.IsSelectedProperty, oldValue, newValue);
        }
#endif

        #region TabStripPlacement
        /// <summary>
        ///     Property key for TabStripPlacementProperty.
        /// </summary>
        private static readonly DependencyPropertyKey TabStripPlacementPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "TabStripPlacement",
                        typeof(Dock),
                        typeof(TabItem),
                        new FrameworkPropertyMetadata(
                                Dock.Top,
                                null,
                                new CoerceValueCallback(CoerceTabStripPlacement)));

        /// <summary>
        /// Specifies the placement of the TabItem
        /// </summary>
        public static readonly DependencyProperty TabStripPlacementProperty =
            TabStripPlacementPropertyKey.DependencyProperty;

        private static object CoerceTabStripPlacement(DependencyObject d, object value)
        {
            TabControl tabControl = ((TabItem)d).TabControlParent;
            return (tabControl != null) ? tabControl.TabStripPlacement : value;
        }

        /// <summary>
        /// Specifies the placement of the TabItem. This read-only property get its value from the TabControl parent
        /// </summary>
        public Dock TabStripPlacement
        {
            get
            {
                return (Dock)GetValue(TabStripPlacementProperty);
            }
        }

        internal override void OnAncestorChanged()
        {
            // TabStripPlacement depends on the logical parent -- so invalidate it when that changes
            CoerceValue(TabStripPlacementProperty);
        }

        #endregion TabStripPlacement

        #endregion

        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

        internal override void ChangeVisualState(bool useTransitions)
        {
            if (!IsEnabled)
            {
                VisualStateManager.GoToState(this, VisualStates.StateDisabled, useTransitions);
            }
            else if (IsMouseOver)
            {
                VisualStateManager.GoToState(this, VisualStates.StateMouseOver, useTransitions);
            }
            else
            {
                VisualStateManager.GoToState(this, VisualStates.StateNormal, useTransitions);
            }

            // Update the SelectionStates group
            if (IsSelected)
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateSelected, VisualStates.StateUnselected);
            }
            else
            {
                VisualStateManager.GoToState(this, VisualStates.StateUnselected, useTransitions);
            }

            if (IsKeyboardFocused)
            {
                VisualStateManager.GoToState(this, VisualStates.StateFocused, useTransitions);
            }
            else
            {
                VisualStateManager.GoToState(this, VisualStates.StateUnfocused, useTransitions);
            }

            base.ChangeVisualState(useTransitions);
        }

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new TabItemWrapperAutomationPeer(this);
        }

        /// <summary>
        /// This is the method that responds to the MouseLeftButtonDownEvent event.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            // We should process only the direct events in case TabItem is the selected one
            // otherwise we are getting this event when we click on TabItem content because it is in the logical subtree
            if (e.Source == this || !IsSelected)
            {
                if (SetFocus())
                    e.Handled = true;
            }
            base.OnMouseLeftButtonDown(e);
        }

        /// <summary>
        /// Focus event handler
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreviewGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnPreviewGotKeyboardFocus(e);
            if (!e.Handled && e.NewFocus == this)
            {
                if (FrameworkAppContextSwitches.SelectionPropertiesCanLagBehindSelectionChangedEvent)
                {
                    // old ("useless") behavior - retained for app-compat
                    if (!IsSelected && TabControlParent != null)
                    {
                        SetCurrentValueInternal(IsSelectedProperty, BooleanBoxes.TrueBox);
                        // If focus moved in result of selection - handle the event to prevent setting focus back on the new item
                        if (e.OldFocus != Keyboard.FocusedElement)
                        {
                            e.Handled = true;
                        }
                        else if (GetBoolField(BoolField.SetFocusOnContent))
                        {
                            TabControl parentTabControl = TabControlParent;
                            if (parentTabControl != null)
                            {
                                // Save the parent and check for null to make sure that SetCurrentValue didn't have a change handler
                                // that removed the TabItem from the tree.
                                ContentPresenter selectedContentPresenter = parentTabControl.SelectedContentPresenter;
                                if (selectedContentPresenter != null)
                                {
                                    parentTabControl.UpdateLayout(); // Wait for layout
                                    bool success = selectedContentPresenter.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));

                                    // If we successfully move focus inside the content then don't set focus to the header
                                    if (success)
                                        e.Handled = true;
                                }
                            }
                        }
                    }
                }
                else
                {
                    // new behavior.  Fixes the case when selection
                    // changes while focus is in the old SelectedContent
                    if (!IsSelected && TabControlParent != null)
                    {
                        SetCurrentValueInternal(IsSelectedProperty, BooleanBoxes.TrueBox);
                        // If focus moved in result of selection - handle the event to prevent setting focus back on the new item
                        if (e.OldFocus != Keyboard.FocusedElement)
                        {
                            e.Handled = true;
                        }
                    }

                    if (!e.Handled && GetBoolField(BoolField.SetFocusOnContent))
                    {
                        TabControl parentTabControl = TabControlParent;
                        if (parentTabControl != null)
                        {
                            // Save the parent and check for null to make sure that SetCurrentValue didn't have a change handler
                            // that removed the TabItem from the tree.
                            ContentPresenter selectedContentPresenter = parentTabControl.SelectedContentPresenter;
                            if (selectedContentPresenter != null)
                            {
                                parentTabControl.UpdateLayout(); // Wait for layout
                                bool success = selectedContentPresenter.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));

                                // If we successfully move focus inside the content then don't set focus to the header
                                if (success)
                                    e.Handled = true;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The Access key for this control was invoked.
        /// </summary>
        protected override void OnAccessKey(AccessKeyEventArgs e)
        {
            SetFocus();
        }

        /// <summary>
        ///     This method is invoked when the Content property changes.
        /// </summary>
        /// <param name="oldContent">The old value of the Content property.</param>
        /// <param name="newContent">The new value of the Content property.</param>
        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);

            // If this is the selected TabItem then we should update TabControl.SelectedContent
            if (IsSelected)
            {
                TabControl tabControl = TabControlParent;
                if (tabControl != null)
                {
                    if (newContent == BindingExpressionBase.DisconnectedItem)
                    {
                        // don't let {DisconnectedItem} bleed into the UI
                        newContent = null;
                    }

                    tabControl.SelectedContent = newContent;
                }
            }
        }

        /// <summary>
        ///     This method is invoked when the ContentTemplate property changes.
        /// </summary>
        /// <param name="oldContentTemplate">The old value of the ContentTemplate property.</param>
        /// <param name="newContentTemplate">The new value of the ContentTemplate property.</param>
        protected override void OnContentTemplateChanged(DataTemplate oldContentTemplate, DataTemplate newContentTemplate)
        {
            base.OnContentTemplateChanged(oldContentTemplate, newContentTemplate);

            // If this is the selected TabItem then we should update TabControl.SelectedContentTemplate
            if (IsSelected)
            {
                TabControl tabControl = TabControlParent;
                if (tabControl != null)
                {
                    tabControl.SelectedContentTemplate = newContentTemplate;
                }
            }
        }

        /// <summary>
        ///     This method is invoked when the ContentTemplateSelector property changes.
        /// </summary>
        /// <param name="oldContentTemplateSelector">The old value of the ContentTemplateSelector property.</param>
        /// <param name="newContentTemplateSelector">The new value of the ContentTemplateSelector property.</param>
        protected override void OnContentTemplateSelectorChanged(DataTemplateSelector oldContentTemplateSelector, DataTemplateSelector newContentTemplateSelector)
        {
            base.OnContentTemplateSelectorChanged(oldContentTemplateSelector, newContentTemplateSelector);

            // If this is the selected TabItem then we should update TabControl.SelectedContentTemplateSelector
            if (IsSelected)
            {
                TabControl tabControl = TabControlParent;
                if (tabControl != null)
                {
                    tabControl.SelectedContentTemplateSelector = newContentTemplateSelector;
                }
            }
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        private static void OnAccessKeyPressed(object sender, AccessKeyPressedEventArgs e)
        {
            if (!e.Handled && e.Scope == null)
            {
                TabItem tabItem = sender as TabItem;

                if (e.Target == null)
                {
                    e.Target = tabItem;
                }
                else if (!tabItem.IsSelected) // If TabItem is not active it is a scope for its content elements
                {
                    e.Scope = tabItem;
                    e.Handled = true;
                }
            }
        }

        internal bool SetFocus()
        {
            bool returnValue = false;

            if (!GetBoolField(BoolField.SettingFocus))
            {
                TabItem currentFocus = Keyboard.FocusedElement as TabItem;

                // If current focus was another TabItem in the same TabControl - dont set focus on content
                bool setFocusOnContent = (FrameworkAppContextSwitches.SelectionPropertiesCanLagBehindSelectionChangedEvent || !IsKeyboardFocusWithin)
                                            && ((currentFocus == this) || (currentFocus == null) || (currentFocus.TabControlParent != this.TabControlParent));
                SetBoolField(BoolField.SettingFocus, true);
                SetBoolField(BoolField.SetFocusOnContent, setFocusOnContent);
                try
                {
                    returnValue = Focus() || setFocusOnContent;
                }
                finally
                {
                    SetBoolField(BoolField.SettingFocus, false);
                    SetBoolField(BoolField.SetFocusOnContent, false);
                }
            }

            return returnValue;
        }

        private TabControl TabControlParent
        {
            get
            {
                return ItemsControl.ItemsControlFromItemContainer(this) as TabControl;
            }
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        private bool GetBoolField(BoolField field)
        {
            return (_tabItemBoolFieldStore & field) != 0;
        }

        private void SetBoolField(BoolField field, bool value)
        {
            if (value)
            {
                _tabItemBoolFieldStore |= field;
            }
            else
            {
                _tabItemBoolFieldStore &= (~field);
            }
        }

        [Flags]
        private enum BoolField
        {
            SetFocusOnContent      = 0x10, // This flag determine if we want to set focus on active TabItem content
            SettingFocus           = 0x20, // This flag indicates that the TabItem is in the process of setting focus

            // By default ListBoxItem is selectable
            DefaultValue = 0,
        }

        BoolField _tabItemBoolFieldStore = BoolField.DefaultValue;

        #endregion Private Fields

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
