// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MS.Internal;
using MS.Internal.KnownBoxes;

namespace System.Windows.Controls
{
    /// <summary>
    ///     A child of a <see cref="TreeView" />.
    /// </summary>
    [TemplatePart(Name = HeaderPartName, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = ItemsHostPartName, Type = typeof(ItemsPresenter))]
    [StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(TreeViewItem))]
    public class TreeViewItem : HeaderedItemsControl, IHierarchicalVirtualizationAndScrollInfo
    {
        #region Constructors

        static TreeViewItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TreeViewItem), new FrameworkPropertyMetadata(typeof(TreeViewItem)));
            VirtualizingPanel.IsVirtualizingProperty.OverrideMetadata(typeof(TreeViewItem), new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(TreeViewItem));

            KeyboardNavigation.DirectionalNavigationProperty.OverrideMetadata(typeof(TreeViewItem), new FrameworkPropertyMetadata(KeyboardNavigationMode.Continue));
            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(TreeViewItem), new FrameworkPropertyMetadata(KeyboardNavigationMode.None));
            IsTabStopProperty.OverrideMetadata(typeof(TreeViewItem), new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

            IsMouseOverPropertyKey.OverrideMetadata(typeof(TreeViewItem), new UIPropertyMetadata(new PropertyChangedCallback(OnVisualStatePropertyChanged)));
            IsEnabledProperty.OverrideMetadata(typeof(TreeViewItem), new UIPropertyMetadata(new PropertyChangedCallback(OnVisualStatePropertyChanged)));
            Selector.IsSelectionActivePropertyKey.OverrideMetadata(typeof(TreeViewItem), new FrameworkPropertyMetadata(new PropertyChangedCallback(OnVisualStatePropertyChanged)));

            EventManager.RegisterClassHandler(typeof(TreeViewItem), FrameworkElement.RequestBringIntoViewEvent, new RequestBringIntoViewEventHandler(OnRequestBringIntoView));
            EventManager.RegisterClassHandler(typeof(TreeViewItem), Mouse.MouseDownEvent, new MouseButtonEventHandler(OnMouseButtonDown), true);
            AutomationProperties.IsOffscreenBehaviorProperty.OverrideMetadata(typeof(TreeViewItem), new FrameworkPropertyMetadata(IsOffscreenBehavior.FromClip));
        }

        /// <summary>
        ///     Creates an instance of this control.
        /// </summary>
        public TreeViewItem()
        {
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     The DependencyProperty for the <see cref="IsExpanded"/> property.
        ///     Default Value: false
        /// </summary>
        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.Register(
                    "IsExpanded",
                    typeof(bool),
                    typeof(TreeViewItem),
                    new FrameworkPropertyMetadata(
                            BooleanBoxes.FalseBox,
                            new PropertyChangedCallback(OnIsExpandedChanged)));

        /// <summary>
        ///     Specifies whether this item has expanded its children or not.
        /// </summary>
        public bool IsExpanded
        {
            get { return (bool) GetValue(IsExpandedProperty); }
            set { SetValue(IsExpandedProperty, value); }
        }

        private bool CanExpand
        {
            get { return HasItems; }
        }

        private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TreeViewItem item = (TreeViewItem) d;
            bool isExpanded = (bool) e.NewValue;

            TreeView tv = item.ParentTreeView;
            if (tv != null)
            {
                if (!isExpanded)
                {
                    tv.HandleSelectionAndCollapsed(item);
                }
            }

            ItemsPresenter itemsHostPresenter = item.ItemsHostPresenter;
            if (itemsHostPresenter != null)
            {
                // In case a TreeViewItem that wasn't previously expanded is now
                // recycled to represent an entity that is expanded or viceversa, we
                // face a situation where we need to synchronously remeasure the
                // sub tree through the ItemsPresenter leading up to the ItemsHost
                // panel. If we didnt do this the offsets could get skewed.
                item.InvalidateMeasure();
                Helper.InvalidateMeasureOnPath(itemsHostPresenter, item, false /*duringMeasure*/);
            }

            TreeViewItemAutomationPeer peer = UIElementAutomationPeer.FromElement(item) as TreeViewItemAutomationPeer;
            if (peer != null)
            {
                peer.RaiseExpandCollapseAutomationEvent((bool)e.OldValue, isExpanded);
            }

            if (isExpanded)
            {
                item.OnExpanded(new RoutedEventArgs(ExpandedEvent, item));
            }
            else
            {
                item.OnCollapsed(new RoutedEventArgs(CollapsedEvent, item));
            }

            item.UpdateVisualState();
        }

        /// <summary>
        ///     The DependencyProperty for the <see cref="IsSelected"/> property.
        ///     Default Value: false
        /// </summary>
        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register(
                    "IsSelected",
                    typeof(bool),
                    typeof(TreeViewItem),
                    new FrameworkPropertyMetadata(
                            BooleanBoxes.FalseBox,
                            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                            new PropertyChangedCallback(OnIsSelectedChanged)));

        /// <summary>
        ///     Specifies whether this item is selected or not.
        /// </summary>
        public bool IsSelected
        {
            get { return (bool) GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TreeViewItem item = (TreeViewItem)d;
            bool isSelected = (bool) e.NewValue;

            item.Select(isSelected);

            TreeViewItemAutomationPeer peer = UIElementAutomationPeer.FromElement(item) as TreeViewItemAutomationPeer;
            if (peer != null)
            {
                peer.RaiseAutomationIsSelectedChanged(isSelected);
            }

            if (isSelected)
            {
                item.OnSelected(new RoutedEventArgs(SelectedEvent, item));
            }
            else
            {
                item.OnUnselected(new RoutedEventArgs(UnselectedEvent, item));
            }

            item.UpdateVisualState();
        }

        /// <summary>
        ///     DependencyProperty for <see cref="IsSelectionActive" />.
        /// </summary>
        public static readonly DependencyProperty IsSelectionActiveProperty = Selector.IsSelectionActiveProperty.AddOwner(typeof(TreeViewItem));

        /// <summary>
        ///     Indicates whether the keyboard focus is within the TreeView.
        ///     When keyboard focus moves to a Menu or Toolbar, then the selection remains active.
        ///     Use this property to style the TreeViewItem to look different when focus is not within the TreeView.
        /// </summary>
        [Browsable(false), Category("Appearance"), ReadOnly(true)]
        public bool IsSelectionActive
        {
            get
            {
                return (bool)GetValue(IsSelectionActiveProperty);
            }
        }

        #endregion

        #region Public Events

        /// <summary>
        ///     Event fired when <see cref="IsExpanded"/> becomes true.
        /// </summary>
        public static readonly RoutedEvent ExpandedEvent = EventManager.RegisterRoutedEvent("Expanded", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TreeViewItem));

        /// <summary>
        ///     Event fired when <see cref="IsExpanded"/> becomes true.
        /// </summary>
        [Category("Behavior")]
        public event RoutedEventHandler Expanded
        {
            add
            {
                AddHandler(ExpandedEvent, value);
            }

            remove
            {
                RemoveHandler(ExpandedEvent, value);
            }
        }

        /// <summary>
        ///     Called when <see cref="IsExpanded"/> becomes true.
        ///     Default implementation fires the <see cref="Expanded"/> event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnExpanded(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        ///     Event fired when <see cref="IsExpanded"/> becomes false.
        /// </summary>
        public static readonly RoutedEvent CollapsedEvent = EventManager.RegisterRoutedEvent("Collapsed", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TreeViewItem));

        /// <summary>
        ///     Event fired when <see cref="IsExpanded"/> becomes false.
        /// </summary>
        [Category("Behavior")]
        public event RoutedEventHandler Collapsed
        {
            add
            {
                AddHandler(CollapsedEvent, value);
            }

            remove
            {
                RemoveHandler(CollapsedEvent, value);
            }
        }

        /// <summary>
        ///     Called when <see cref="IsExpanded"/> becomes false.
        ///     Default implementation fires the <see cref="Collapsed"/> event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnCollapsed(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        ///     Event fired when <see cref="IsSelected"/> becomes true.
        /// </summary>
        public static readonly RoutedEvent SelectedEvent = EventManager.RegisterRoutedEvent("Selected", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TreeViewItem));

        /// <summary>
        ///     Event fired when <see cref="IsSelected"/> becomes true.
        /// </summary>
        [Category("Behavior")]
        public event RoutedEventHandler Selected
        {
            add
            {
                AddHandler(SelectedEvent, value);
            }

            remove
            {
                RemoveHandler(SelectedEvent, value);
            }
        }

        /// <summary>
        ///     Called when <see cref="IsSelected"/> becomes true.
        ///     Default implementation fires the <see cref="Selected"/> event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnSelected(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        ///     Event fired when <see cref="IsSelected"/> becomes false.
        /// </summary>
        public static readonly RoutedEvent UnselectedEvent = EventManager.RegisterRoutedEvent("Unselected", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TreeViewItem));

        /// <summary>
        ///     Event fired when <see cref="IsSelected"/> becomes false.
        /// </summary>
        [Category("Behavior")]
        public event RoutedEventHandler Unselected
        {
            add
            {
                AddHandler(UnselectedEvent, value);
            }

            remove
            {
                RemoveHandler(UnselectedEvent, value);
            }
        }

        /// <summary>
        ///     Called when <see cref="IsSelected"/> becomes false.
        ///     Default implementation fires the <see cref="Unselected"/> event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnUnselected(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Expands this TreeViewItem and all of the TreeViewItems inside its subtree.
        /// </summary>
        public void ExpandSubtree()
        {
            ExpandRecursive(this);
        }

        #endregion

        #region Internal Methods

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            arrangeSize = base.ArrangeOverride(arrangeSize);

            Helper.ComputeCorrectionFactor(ParentTreeView, this, ItemsHost, HeaderElement);

            return arrangeSize;
        }

        HierarchicalVirtualizationConstraints IHierarchicalVirtualizationAndScrollInfo.Constraints
        {
            get { return GroupItem.HierarchicalVirtualizationConstraintsField.GetValue(this); }
            set
            {
                if (value.CacheLengthUnit == VirtualizationCacheLengthUnit.Page)
                {
                    throw new InvalidOperationException(SR.Get(SRID.PageCacheSizeNotAllowed));
                }
                GroupItem.HierarchicalVirtualizationConstraintsField.SetValue(this, value);
            }
        }

        HierarchicalVirtualizationHeaderDesiredSizes IHierarchicalVirtualizationAndScrollInfo.HeaderDesiredSizes
        {
            get
            {
                FrameworkElement headerElement = HeaderElement;
                Size pixelHeaderSize = this.IsVisible && headerElement != null ? headerElement.DesiredSize : new Size();

                Helper.ApplyCorrectionFactorToPixelHeaderSize(ParentTreeView, this, ItemsHost, ref pixelHeaderSize);

                Size logicalHeaderSize = new Size(DoubleUtil.GreaterThan(pixelHeaderSize.Width, 0) ? 1 : 0,
                                DoubleUtil.GreaterThan(pixelHeaderSize.Height, 0) ? 1 : 0);

                return new HierarchicalVirtualizationHeaderDesiredSizes(logicalHeaderSize, pixelHeaderSize);
            }
        }

        HierarchicalVirtualizationItemDesiredSizes IHierarchicalVirtualizationAndScrollInfo.ItemDesiredSizes
        {
            get
            {
                return Helper.ApplyCorrectionFactorToItemDesiredSizes(this, ItemsHost);
            }
            set
            {
                GroupItem.HierarchicalVirtualizationItemDesiredSizesField.SetValue(this, value);
            }
        }

        Panel IHierarchicalVirtualizationAndScrollInfo.ItemsHost
        {
            get
            {
                return ItemsHost;
            }
        }

        bool IHierarchicalVirtualizationAndScrollInfo.MustDisableVirtualization
        {
            get { return GroupItem.MustDisableVirtualizationField.GetValue(this); }
            set { GroupItem.MustDisableVirtualizationField.SetValue(this, value); }
        }

        bool IHierarchicalVirtualizationAndScrollInfo.InBackgroundLayout
        {
            get { return GroupItem.InBackgroundLayoutField.GetValue(this); }
            set { GroupItem.InBackgroundLayoutField.SetValue(this, value); }
        }

        #endregion

        #region Implementation

        #region Tree

        /// <summary>
        ///     Walks up the parent chain of TreeViewItems to the top TreeView.
        /// </summary>
        internal TreeView ParentTreeView
        {
            get
            {
                ItemsControl parent = ParentItemsControl;
                while (parent != null)
                {
                    TreeView tv = parent as TreeView;
                    if (tv != null)
                    {
                        return tv;
                    }

                    parent = ItemsControl.ItemsControlFromItemContainer(parent);
                }

                return null;
            }
        }

        /// <summary>
        ///     Returns the immediate parent TreeViewItem. Null if the parent is a TreeView.
        /// </summary>
        internal TreeViewItem ParentTreeViewItem
        {
            get
            {
                return ParentItemsControl as TreeViewItem;
            }
        }

        /// <summary>
        ///     Returns the immediate parent ItemsControl.
        /// </summary>
        internal ItemsControl ParentItemsControl
        {
            get
            {
                return ItemsControl.ItemsControlFromItemContainer(this);
            }
        }

        #endregion

        #region Selection

        /// <summary>
        /// Called when the visual parent of this element changes.
        /// </summary>
        /// <param name="oldParent"></param>
        protected internal override void OnVisualParentChanged(DependencyObject oldParent)
        {
            // When TreeViewItem is added to the visual tree we check if IsSelected is set to true
            // In this case we need to update the tree selection
            if (VisualTreeHelper.GetParent(this) != null)
            {
                if (IsSelected)
                {
                    Select(true);
                }
            }

            base.OnVisualParentChanged(oldParent);
        }

        private void Select(bool selected)
        {
            TreeView tree = ParentTreeView;
            ItemsControl parent = ParentItemsControl;
            if ((tree != null) && (parent != null) && !tree.IsSelectionChangeActive)
            {
                // Give the TreeView a reference to this container and its data
                object data = parent.GetItemOrContainerFromContainer(this);
                tree.ChangeSelection(data, this, selected);

                // Making focus of TreeViewItem synchronize with selection if needed.
                if (selected && tree.IsKeyboardFocusWithin && !IsKeyboardFocusWithin)
                {
                    Focus();
                }
            }
        }

        private bool ContainsSelection
        {
            get { return ReadControlFlag(ControlBoolFlags.ContainsSelection); }
            set { WriteControlFlag(ControlBoolFlags.ContainsSelection, value); }
        }

        internal void UpdateContainsSelection(bool selected)
        {
            TreeViewItem parent = ParentTreeViewItem;
            while (parent != null)
            {
                parent.ContainsSelection = selected;
                parent = parent.ParentTreeViewItem;
            }
        }

        #endregion

        #region Input

        /// <summary>
        ///     This method is invoked when the IsFocused property changes to true.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected override void OnGotFocus(RoutedEventArgs e)
        {
            Select(true);
            base.OnGotFocus(e);
        }

        /// <summary>
        ///     Called when the left mouse button is pressed down.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (!e.Handled && IsEnabled)
            {
                bool wasFocused = IsFocused;
                if (Focus())
                {
                    if (wasFocused && !IsSelected)
                    {
                        Select(true);
                    }
                    e.Handled = true;
                }

                if ((e.ClickCount % 2) == 0)
                {
                    SetCurrentValueInternal(IsExpandedProperty, BooleanBoxes.Box(!IsExpanded));
                    e.Handled = true;
                }
            }
            base.OnMouseLeftButtonDown(e);
        }

        /// <summary>
        ///     Called when a keyboard key is pressed down.
        /// </summary>
        /// <param name="e">Event Arguments</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!e.Handled)
            {
                switch (e.Key)
                {
                    case Key.Add:
                        if (CanExpandOnInput && !IsExpanded)
                        {
                            SetCurrentValueInternal(IsExpandedProperty, BooleanBoxes.TrueBox);
                            e.Handled = true;
                        }
                        break;

                    case Key.Subtract:
                        if (CanExpandOnInput && IsExpanded)
                        {
                            SetCurrentValueInternal(IsExpandedProperty, BooleanBoxes.FalseBox);
                            e.Handled = true;
                        }
                        break;

                    case Key.Left:
                    case Key.Right:
                        if (LogicalLeft(e.Key))
                        {
                            if (!IsControlKeyDown && CanExpandOnInput && IsExpanded)
                            {
                                if (IsFocused)
                                {
                                    SetCurrentValueInternal(IsExpandedProperty, BooleanBoxes.FalseBox);
                                }
                                else
                                {
                                    Focus();
                                }
                                e.Handled = true;
                            }
                        }
                        else
                        {
                            if (!IsControlKeyDown && CanExpandOnInput)
                            {
                                if (!IsExpanded)
                                {
                                    SetCurrentValueInternal(IsExpandedProperty, BooleanBoxes.TrueBox);
                                    e.Handled = true;
                                }
                                else if (HandleDownKey(e))
                                {
                                    e.Handled = true;
                                }
                            }
                        }
                        break;

                    case Key.Down:
                        if (!IsControlKeyDown && HandleDownKey(e))
                        {
                            e.Handled = true;
                        }
                        break;

                    case Key.Up:
                        if (!IsControlKeyDown && HandleUpKey(e))
                        {
                            e.Handled = true;
                        }
                        break;
                }
            }
        }

        private bool LogicalLeft(Key key)
        {
            bool invert = (FlowDirection == FlowDirection.RightToLeft);
            return (!invert && (key == Key.Left)) || (invert && (key == Key.Right));
        }

        private static bool IsControlKeyDown
        {
            get
            {
                return ((Keyboard.Modifiers & ModifierKeys.Control) == (ModifierKeys.Control));
            }
        }

        private bool CanExpandOnInput
        {
            get
            {
                return CanExpand && IsEnabled;
            }
        }

        internal bool HandleUpKey(KeyEventArgs e)
        {
            return HandleUpDownKey(true, e);
        }

        internal bool HandleDownKey(KeyEventArgs e)
        {
            return HandleUpDownKey(false, e);
        }

        private bool HandleUpDownKey(bool up, KeyEventArgs e)
        {
            FocusNavigationDirection direction = (up ? FocusNavigationDirection.Up : FocusNavigationDirection.Down);
            if (AllowHandleKeyEvent(direction))
            {
                TreeView treeView = ParentTreeView;
                IInputElement originalFocus = Keyboard.FocusedElement;
                if (treeView != null)
                {
                    FrameworkElement startingContainer = this.HeaderElement;
                    if (startingContainer == null)
                    {
                        startingContainer = this;
                    }
                    ItemsControl parentItemsControl = ItemsControl.ItemsControlFromItemContainer(this);
                    ItemInfo startingInfo = (parentItemsControl != null)
                        ? parentItemsControl.ItemInfoFromContainer(this)
                        : null;

                    return treeView.NavigateByLine(
                        startingInfo,
                        startingContainer,
                        direction,
                        new ItemNavigateArgs(e.Device, Keyboard.Modifiers));
                }
            }

            return false; // Not handled
        }

        private bool AllowHandleKeyEvent(FocusNavigationDirection direction)
        {
            if (!IsSelected)
            {
                return false;
            }

            DependencyObject currentFocus = Keyboard.FocusedElement as DependencyObject;
            if (currentFocus != null)
            {
                DependencyObject predict = UIElementHelper.PredictFocus(currentFocus, direction);
                if (predict != currentFocus)
                {
                    while (predict != null)
                    {
                        TreeViewItem item = predict as TreeViewItem;
                        if (item == this)
                        {
                            return false; // There is a focusable item in the header
                        }
                        else if ((item != null) || (predict is TreeView))
                        {
                            return true;
                        }

                        predict = KeyboardNavigation.GetParent(predict);
                    }
                }
            }

            return true;
        }

        private static void OnMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem tvi = (TreeViewItem)sender;
            TreeView tv = tvi.ParentTreeView;
            if (tv != null)
            {
                tv.HandleMouseButtonDown();
            }
        }
        private static void OnRequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            if (e.TargetObject == sender)
            {
                ((TreeViewItem)sender).HandleBringIntoView(e);
            }
        }

        private void HandleBringIntoView(RequestBringIntoViewEventArgs e)
        {
            TreeViewItem parent = ParentTreeViewItem;
            while (parent != null)
            {
                if (!parent.IsExpanded)
                {
                    parent.SetCurrentValueInternal(IsExpandedProperty, BooleanBoxes.TrueBox);
                }

                parent = parent.ParentTreeViewItem;
            }

            // See FrameworkElement.BringIntoView() comments
            //dmitryt, bug 1126518. On new/updated elements RenderSize isn't yet computed
            //so we need to postpone the rect computation until layout is done.
            //this is accomplished by passing Empty rect here and then asking for RenderSize
            //in IScrollInfo when it actually executes an async MakeVisible command.
            if (e.TargetRect.IsEmpty)
            {
                FrameworkElement header = HeaderElement;
                if (header != null)
                {
                    e.Handled = true;
                    header.BringIntoView();
                }
                else
                {
                    // Header is not generated yet. Could happen if BringIntoView is called on container before layout. Try later.
                    Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new DispatcherOperationCallback(BringItemIntoView), null);
                }
            }
        }

        private object BringItemIntoView(object args)
        {
            FrameworkElement header = HeaderElement;
            if (header != null)
            {
                header.BringIntoView();
            }
            return null;
        }

        internal FrameworkElement HeaderElement
        {
            get
            {
                return GetTemplateChild(HeaderPartName) as FrameworkElement;
            }
        }

        // returns the HeaderElement, or an approximation.   If no acceptable
        // candidate is found, return the TreeViewItem itself.
        internal FrameworkElement TryGetHeaderElement()
        {
            // return HeaderElement, if available
            FrameworkElement header = HeaderElement;
            if (header != null)
                return header;

            // if there's no template yet, return the fallback
            FrameworkTemplate template = TemplateInternal;
            if (template == null)
                return this;

            // if the template doesn't define the header part, we do something
            // special for compat with 4.0 
            // Using the keyboard to move up a tree causes it to jump to
            // the parent folder instead of the next file above it on .net 4.5 machines
            int index = StyleHelper.QueryChildIndexFromChildName(HeaderPartName, template.ChildIndexFromChildName);
            if (index < 0)
            {
                // In 4.0 keyboard navigation worked even when a custom
                // template failed to define the header part.  We make this work
                // by returning an element from the template that looks like it
                // was intended to be the header.  The heuristic we use is:
                // pick the element following the ToggleButton
                ToggleButton toggleButton = Helper.FindTemplatedDescendant<ToggleButton>(this, this);
                if (toggleButton != null)
                {
                    FrameworkElement parent = VisualTreeHelper.GetParent(toggleButton) as FrameworkElement;
                    if (parent != null)
                    {
                        int count = VisualTreeHelper.GetChildrenCount(parent);
                        for (index=0; index < count-1; ++index)
                        {
                            if (VisualTreeHelper.GetChild(parent, index) == toggleButton)
                            {
                                header = VisualTreeHelper.GetChild(parent, index+1) as FrameworkElement;
                                if (header != null)
                                    return header;
                                break;
                            }
                        }
                    }
                }
            }

            // in all other cases, return the fallback
            return this;
        }

        private ItemsPresenter ItemsHostPresenter
        {
            get
            {
                return GetTemplateChild(ItemsHostPartName) as ItemsPresenter;
            }
        }

        #endregion

        #region Containers


        /// <summary>
        ///     Returns true if the item is or should be its own container.
        /// </summary>
        /// <param name="item">The item to test.</param>
        /// <returns>true if its type matches the container type.</returns>
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is TreeViewItem;
        }

        /// <summary>
        ///     Create or identify the element used to display the given item.
        /// </summary>
        /// <returns>The container.</returns>
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new TreeViewItem();
        }

        internal void PrepareItemContainer(object item, ItemsControl parentItemsControl)
        {
            //
            // Clear previously cached items sizes
            //
            Helper.ClearVirtualizingElement((IHierarchicalVirtualizationAndScrollInfo)this);

            IsVirtualizingPropagationHelper(parentItemsControl, this);

            //
            // ItemValueStorage:  restore saved values for this item onto the new container
            //
            if (VirtualizingPanel.GetIsVirtualizing(parentItemsControl))
            {
                Helper.SetItemValuesOnContainer(parentItemsControl, this, item);
            }
        }

        internal void ClearItemContainer(object item, ItemsControl parentItemsControl)
        {
            if (VirtualizingPanel.GetIsVirtualizing(parentItemsControl))
            {
                //
                // ItemValueStorage:  save off values for this container if we're a virtualizing TreeView.
                //

                //
                // Right now we have a hard-coded list of DPs we want to save off.  In the future we could provide a 'register' API
                // so that each ItemsControl could decide what DPs to save on its containers. Maybe we define a virtual method to
                // retrieve a list of DPs the type is interested in.  Alternatively we could have the contract
                // be that ItemsControls use the ItemStorageService inside their ClearContainerForItemOverride by calling into StoreItemValues.
                //
                Helper.StoreItemValues(parentItemsControl, this, item);

                // Tell the panel to clear off all its containers.  This will cause this method to be called
                // recursively down the tree, allowing all descendent data to be stored before we save off
                // the ItemValueStorage DP for this container.

                VirtualizingPanel vp = ItemsHost as VirtualizingPanel;
                if (vp != null)
                {
                    vp.OnClearChildrenInternal();
                }

                ItemContainerGenerator.RemoveAllInternal(true /*saveRecycleQueue*/);
            }

            // this container is going away - forget about its selection
            ContainsSelection = false;
        }

        // Synchronizes the value of the child's IsVirtualizing property with that of the parent's
        internal static void IsVirtualizingPropagationHelper(DependencyObject parent, DependencyObject element)
        {
            SynchronizeValue(VirtualizingPanel.IsVirtualizingProperty, parent, element);
            SynchronizeValue(VirtualizingPanel.IsVirtualizingWhenGroupingProperty, parent, element);
            SynchronizeValue(VirtualizingPanel.VirtualizationModeProperty, parent, element);
            SynchronizeValue(VirtualizingPanel.ScrollUnitProperty, parent, element);
        }

        internal static void SynchronizeValue(DependencyProperty dp, DependencyObject parent, DependencyObject child)
        {
            object value = parent.GetValue(dp);
            child.SetValue(dp, value);
        }

        /// <summary>
        ///     This method is invoked when the Items property changes.
        /// </summary>
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Reset:
                    if (ContainsSelection)
                    {
                        TreeView tree = ParentTreeView;
                        if ((tree != null) && !tree.IsSelectedContainerHookedUp)
                        {
                            ContainsSelection = false;
                            Select(true);
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    if (ContainsSelection)
                    {
                        TreeView tree = ParentTreeView;
                        if (tree != null)
                        {
                            // When Selected item is replaced - remove the selection
                            // Revisit the condition when we support duplicate items in Items collection: if e.OldItems[0] is the same as selected items we will unselect the selected item
                            object selectedItem = tree.SelectedItem;
                            if ((selectedItem != null) && selectedItem.Equals(e.OldItems[0]))
                            {
                                tree.ChangeSelection(selectedItem, tree.SelectedContainer, false);
                            }
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Move:
                    break;

                default:
                    throw new NotSupportedException(SR.Get(SRID.UnexpectedCollectionChangeAction, e.Action));
            }
        }

        /// <summary>
        /// Recursively & syncronously expand all the nodes in this subtree.
        /// </summary>
        private static void ExpandRecursive(TreeViewItem item)
        {
            if (item == null)
            {
                return;
            }

            // Expand the current item
            if (!item.IsExpanded)
            {
                item.SetCurrentValueInternal(IsExpandedProperty, BooleanBoxes.TrueBox);
            }

            // ApplyTemplate in order to generate the ItemsPresenter and the ItemsPanel. Note that in the
            // virtualizing case even if the item is marked expanded we still need to do this step in order to
            // regenerate the visuals because they may have been virtualized away.

            item.ApplyTemplate();
            ItemsPresenter itemsPresenter = (ItemsPresenter)item.Template.FindName(ItemsHostPartName, item);
            if (itemsPresenter != null)
            {
                itemsPresenter.ApplyTemplate();
            }
            else
            {
                item.UpdateLayout();
            }

            VirtualizingPanel virtualizingPanel = item.ItemsHost as VirtualizingPanel;
            item.ItemsHost.EnsureGenerator();

            for (int i = 0, count = item.Items.Count; i < count; i++)
            {
                TreeViewItem subitem;
                if (virtualizingPanel != null)
                {
                    // We need to bring the item into view so that the container will be generated.
                    virtualizingPanel.BringIndexIntoView(i);

                    subitem = (TreeViewItem)item.ItemContainerGenerator.ContainerFromIndex(i);
                }
                else
                {
                    subitem = (TreeViewItem)item.ItemContainerGenerator.ContainerFromIndex(i);

                    // We dont actually need to bring this into view, but we'll do it
                    // anyways to maintain the same behavior as with a virtualizing panel.
                    subitem.BringIntoView();
                }

                if (subitem != null)
                {
                    ExpandRecursive(subitem);
                }
            }
        }

        #endregion

        #region Automation
        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new TreeViewItemAutomationPeer(this);
        }
        #endregion Automation

        #region DTypeThemeStyleKey

        // Returns the DependencyObjectType for the registered ThemeStyleKey's default
        // value. Controls will override this method to return approriate types.
        internal override DependencyObjectType DTypeThemeStyleKey
        {
            get { return _dType; }
        }

        private static DependencyObjectType _dType;

        #endregion DTypeThemeStyleKey

        #endregion

        #region Visual States

        internal override void ChangeVisualState(bool useTransitions)
        {
            // Handle the Common states
            if (!IsEnabled)
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateDisabled, VisualStates.StateNormal);
            }
            else if (IsMouseOver)
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateMouseOver, VisualStates.StateNormal);
            }
            else
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateNormal);
            }

            // Handle the Focused states
            if (IsKeyboardFocused)
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateFocused, VisualStates.StateUnfocused);
            }
            else
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateUnfocused);
            }

            // Handle the Expansion states
            if (IsExpanded)
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateExpanded);
            }
            else
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateCollapsed);
            }

            // Handle the HasItems states
            if (HasItems)
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateHasItems);
            }
            else
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateNoItems);
            }

            // Handle the Selected states
            if (IsSelected)
            {
                if (IsSelectionActive)
                {
                    VisualStates.GoToState(this, useTransitions, VisualStates.StateSelected);
                }
                else
                {
                    VisualStates.GoToState(this, useTransitions, VisualStates.StateSelectedInactive, VisualStates.StateSelected);
                }
            }
            else
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateUnselected);
            }

            base.ChangeVisualState(useTransitions);
        }

        #endregion

        #region Data

        private const string HeaderPartName = "PART_Header";
        private const string ItemsHostPartName = "ItemsHost";

        #endregion
    }
}

