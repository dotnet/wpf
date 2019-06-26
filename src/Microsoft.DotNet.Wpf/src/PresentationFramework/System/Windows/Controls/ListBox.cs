// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Internal;
using MS.Utility;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Data;
using System.Windows.Automation.Peers;

using System;
using MS.Internal.Commands; // CommandHelpers
using MS.Internal.KnownBoxes;
using MS.Internal.Telemetry.PresentationFramework;

namespace System.Windows.Controls
{
    /// <summary>
    ///     Control that implements a list of selectable items.
    /// </summary>
    [Localizability(LocalizationCategory.ListBox)]
    [StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(ListBoxItem))]
    public class ListBox : Selector
    {
        internal const string ListBoxSelectAllKey = "Ctrl+A";

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
        public ListBox() : base()
        {
            Initialize();
        }

        // common code for all constructors
        private void Initialize()
        {
            SelectionMode mode = (SelectionMode) SelectionModeProperty.GetDefaultValue(DependencyObjectType);
            ValidateSelectionMode(mode);
        }

        static ListBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ListBox), new FrameworkPropertyMetadata(typeof(ListBox)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(ListBox));

            IsTabStopProperty.OverrideMetadata(typeof(ListBox), new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));
            KeyboardNavigation.DirectionalNavigationProperty.OverrideMetadata(typeof(ListBox), new FrameworkPropertyMetadata(KeyboardNavigationMode.Contained));
            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(ListBox), new FrameworkPropertyMetadata(KeyboardNavigationMode.Once));

            IsTextSearchEnabledProperty.OverrideMetadata(typeof(ListBox), new FrameworkPropertyMetadata(BooleanBoxes.TrueBox));

            ItemsPanelTemplate template = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(VirtualizingStackPanel)));
            template.Seal();
            ItemsPanelProperty.OverrideMetadata(typeof(ListBox), new FrameworkPropertyMetadata(template));

            // Need handled events too here because any mouse up should release our mouse capture
            EventManager.RegisterClassHandler(typeof(ListBox), Mouse.MouseUpEvent, new MouseButtonEventHandler(OnMouseButtonUp), true);
            EventManager.RegisterClassHandler(typeof(ListBox), Keyboard.GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(OnGotKeyboardFocus));

            CommandHelpers.RegisterCommandHandler(typeof(ListBox), ListBox.SelectAllCommand, new ExecutedRoutedEventHandler(OnSelectAll), new CanExecuteRoutedEventHandler(OnQueryStatusSelectAll), KeyGesture.CreateFromResourceStrings(ListBoxSelectAllKey, SR.Get(SRID.ListBoxSelectAllKeyDisplayString)));

            ControlsTraceLogger.AddControl(TelemetryControls.ListBox);
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        #region Public Methods

        /// <summary>
        ///     Select all the items
        /// </summary>
        public void SelectAll()
        {
            if (CanSelectMultiple)
            {
                SelectAllImpl();
            }
            else
            {
                throw new NotSupportedException(SR.Get(SRID.ListBoxSelectAllSelectionMode));
            }
        }

        /// <summary>
        ///     Clears all of the selected items.
        /// </summary>
        public void UnselectAll()
        {
            UnselectAllImpl();
        }

        /// <summary>
        /// Causes the object to scroll into view.  If it is not visible, it is aligned either at the top or bottom of the viewport.
        /// </summary>
        /// <param name="item"></param>
        public void ScrollIntoView(object item)
        {
            if (ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                OnBringItemIntoView(item);
            }
            else
            {
                // The items aren't generated, try at a later time
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new DispatcherOperationCallback(OnBringItemIntoView), item);
            }
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        ///     SelectionMode DependencyProperty
        /// </summary>
        public static readonly DependencyProperty SelectionModeProperty =
                DependencyProperty.Register(
                        "SelectionMode",
                        typeof(SelectionMode),
                        typeof(ListBox),
                        new FrameworkPropertyMetadata(
                                SelectionMode.Single,
                                new PropertyChangedCallback(OnSelectionModeChanged)),
                        new ValidateValueCallback(IsValidSelectionMode));

        /// <summary>
        ///     Indicates the selection behavior for the ListBox.
        /// </summary>
        public SelectionMode SelectionMode
        {
            get { return (SelectionMode) GetValue(SelectionModeProperty); }
            set { SetValue(SelectionModeProperty, value); }
        }

        private static void OnSelectionModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ListBox listBox = (ListBox)d;
            listBox.ValidateSelectionMode(listBox.SelectionMode);
        }

        private static object OnGetSelectionMode(DependencyObject d)
        {
            return ((ListBox)d).SelectionMode;
        }


        private static bool IsValidSelectionMode(object o)
        {
            SelectionMode value = (SelectionMode)o;
            return value == SelectionMode.Single
                || value == SelectionMode.Multiple
                || value == SelectionMode.Extended;
        }

        private void ValidateSelectionMode(SelectionMode mode)
        {
            CanSelectMultiple = (mode != SelectionMode.Single);
        }

        /// <summary>
        /// A read-only IList containing the currently selected items
        /// </summary>
        public static readonly DependencyProperty SelectedItemsProperty = Selector.SelectedItemsImplProperty;

        /// <summary>
        /// The currently selected items.
        /// </summary>
        [Bindable(true), Category("Appearance"), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IList SelectedItems
        {
            get
            {
                return SelectedItemsImpl;
            }
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        {
            return new System.Windows.Automation.Peers.ListBoxAutomationPeer(this);
        }

        /// <summary>
        /// Select multiple items.
        /// </summary>
        /// <param name="selectedItems">Collection of items to be selected.</param>
        /// <returns>true if all items have been selected.</returns>
        protected bool SetSelectedItems(IEnumerable selectedItems)
        {
            return SetSelectedItemsImpl(selectedItems);
        }

        /// <summary>
        /// Prepare the element to display the item.  This may involve
        /// applying styles, setting bindings, etc.
        /// </summary>
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            if (item is Separator)
                Separator.PrepareContainer(element as Control);
        }

        /// <summary>
        ///     Adjust ItemInfos when the Items property changes.
        /// </summary>
        internal override void AdjustItemInfoOverride(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            AdjustItemInfo(e, _anchorItem);

            // If the anchor item is removed, drop our reference to it.
            if (_anchorItem != null && _anchorItem.Index < 0)
            {
                _anchorItem = null;
            }

            base.AdjustItemInfoOverride(e);
        }

        /// <summary>
        ///     Adjust ItemInfos when the generator finishes.
        /// </summary>
        internal override void AdjustItemInfosAfterGeneratorChangeOverride()
        {
            AdjustItemInfoAfterGeneratorChange(_anchorItem);
            base.AdjustItemInfosAfterGeneratorChangeOverride();
        }


        /// <summary>
        /// A virtual function that is called when the selection is changed. Default behavior
        /// is to raise a SelectionChangedEvent
        /// </summary>
        /// <param name="e">The inputs for this event. Can be raised (default behavior) or processed
        ///   in some other way.</param>
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);

            // In a single selection mode we want to move anchor to the selected element
            if (SelectionMode == SelectionMode.Single)
            {
                ItemInfo info = InternalSelectedInfo;
                ListBoxItem listItem = (info != null) ? info.Container as ListBoxItem : null;

                if (listItem != null)
                    UpdateAnchorAndActionItem(info);
            }

            if (    AutomationPeer.ListenerExists(AutomationEvents.SelectionPatternOnInvalidated)
                ||  AutomationPeer.ListenerExists(AutomationEvents.SelectionItemPatternOnElementSelected)
                ||  AutomationPeer.ListenerExists(AutomationEvents.SelectionItemPatternOnElementAddedToSelection)
                ||  AutomationPeer.ListenerExists(AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection)   )
            {
                ListBoxAutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(this) as ListBoxAutomationPeer;
                if (peer != null)
                    peer.RaiseSelectionEvents(e);
            }
        }

        /// <summary>
        ///     This is the method that responds to the KeyDown event.
        /// </summary>
        /// <param name="e">Event Arguments</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            bool handled = true;
            Key key = e.Key;
            switch (key)
            {
                case Key.Divide:
                case Key.Oem2:
                    // Ctrl-Fowardslash = Select All
                    if (((Keyboard.Modifiers & ModifierKeys.Control) == (ModifierKeys.Control)) && (SelectionMode == SelectionMode.Extended))
                    {
                        SelectAll();
                    }
                    else
                    {
                        handled = false;
                    }

                    break;

                case Key.Oem5:
                    // Ctrl-Backslash = Select the item with focus.
                    if (((Keyboard.Modifiers & ModifierKeys.Control) == (ModifierKeys.Control)) && (SelectionMode == SelectionMode.Extended))
                    {
                        ListBoxItem focusedItemUI = (FocusedInfo != null) ? FocusedInfo.Container as ListBoxItem : null;
                        if (focusedItemUI != null)
                        {
                            MakeSingleSelection(focusedItemUI);
                        }
                    }
                    else
                    {
                        handled = false;
                    }

                    break;

                case Key.Up:
                case Key.Left:
                case Key.Down:
                case Key.Right:
                    {
                        KeyboardNavigation.ShowFocusVisual();

                        // Depend on logical orientation we decide to move focus or just scroll
                        // shouldScroll also detects if we can scroll more in this direction
                        bool shouldScroll = ScrollHost != null;
                        if (shouldScroll)
                        {
                            shouldScroll =
                                ((key == Key.Down && IsLogicalHorizontal && DoubleUtil.GreaterThan(ScrollHost.ScrollableHeight, ScrollHost.VerticalOffset))) ||
                                ((key == Key.Up   && IsLogicalHorizontal && DoubleUtil.GreaterThan(ScrollHost.VerticalOffset, 0d))) ||
                                ((key == Key.Right&& IsLogicalVertical && DoubleUtil.GreaterThan(ScrollHost.ScrollableWidth, ScrollHost.HorizontalOffset))) ||
                                ((key == Key.Left && IsLogicalVertical && DoubleUtil.GreaterThan(ScrollHost.HorizontalOffset, 0d)));
                        }

                        if (shouldScroll)
                        {
                            ScrollHost.ScrollInDirection(e);
                        }
                        else
                        {
                            if ((ItemsHost != null && ItemsHost.IsKeyboardFocusWithin) || IsKeyboardFocused)
                            {
                                if (!NavigateByLine(KeyboardNavigation.KeyToTraversalDirection(key),
                                        new ItemNavigateArgs(e.Device, Keyboard.Modifiers)))
                                {
                                    handled = false;
                                }
                            }
                            else
                            {
                                handled = false;
                            }
                        }
                    }
                    break;

                case Key.Home:
                    NavigateToStart(new ItemNavigateArgs(e.Device, Keyboard.Modifiers));
                    break;

                case Key.End:
                    NavigateToEnd(new ItemNavigateArgs(e.Device, Keyboard.Modifiers));
                    break;

                case Key.Space:
                case Key.Enter:
                    {
                        if (e.Key == Key.Enter && (bool)GetValue(KeyboardNavigation.AcceptsReturnProperty) == false)
                        {
                            handled = false;
                            break;
                        }

                        // If the event came from a ListBoxItem that's a child of ours, then look at it.
                        ListBoxItem source = e.OriginalSource as ListBoxItem;

                        // If ALT is down & Ctrl is up, then we shouldn't handle this. (system menu)
                        if ((Keyboard.Modifiers & (ModifierKeys.Control|ModifierKeys.Alt)) == ModifierKeys.Alt)
                        {
                            handled = false;
                            break;
                        }

                        // If the user hits just "space" while text searching, do not handle the event
                        // Note: Space cannot be the first character in a string sent to ITS.
                        if (IsTextSearchEnabled && Keyboard.Modifiers == ModifierKeys.None)
                        {
                            TextSearch instance = TextSearch.EnsureInstance(this);
                            // If TextSearch enabled and Prefix is not empty
                            // then let this SPACE go so ITS can process it.
                            if (instance != null && (instance.GetCurrentPrefix() != String.Empty))
                            {
                                handled = false;
                                break;
                            }
                        }

                        if (source != null && ItemsControlFromItemContainer(source) == this)
                        {
                            switch (SelectionMode)
                            {
                                case SelectionMode.Single:
                                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                                    {
                                        MakeToggleSelection(source);
                                    }
                                    else
                                    {
                                        MakeSingleSelection(source);
                                    }

                                    break;

                                case SelectionMode.Multiple:
                                    MakeToggleSelection(source);
                                    break;

                                case SelectionMode.Extended:
                                    if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == ModifierKeys.Control)
                                    {
                                        // Only CONTROL
                                        MakeToggleSelection(source);
                                    }
                                    else if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == ModifierKeys.Shift)
                                    {
                                        // Only SHIFT
                                        MakeAnchorSelection(source, true /* clearCurrent */);
                                    }
                                    else if ((Keyboard.Modifiers & ModifierKeys.Shift) == 0)
                                    {
                                        MakeSingleSelection(source);
                                    }
                                    else
                                    {
                                        handled = false;
                                    }

                                    break;
                            }
                        }
                        else
                        {
                            handled = false;
                        }
                    }
                    break;

                case Key.PageUp:
                    NavigateByPage(FocusNavigationDirection.Up, new ItemNavigateArgs(e.Device, Keyboard.Modifiers));
                    break;

                case Key.PageDown:
                    NavigateByPage(FocusNavigationDirection.Down, new ItemNavigateArgs(e.Device, Keyboard.Modifiers));
                    break;

                default:
                    handled = false;
                    break;
            }
            if (handled)
            {
                e.Handled = true;
            }
            else
            {
                base.OnKeyDown(e);
            }
        }

        /// <summary>
        ///     An event reporting a mouse move.
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            // If we get a mouse move and we have capture, then the mouse was
            // outside the ListBox.  We should autoscroll.
            if (e.OriginalSource == this && Mouse.Captured == this)
            {
                if (Mouse.LeftButton == MouseButtonState.Pressed)
                {
                    DoAutoScroll();
                }
                else
                {
                    // We missed the mouse up, release capture
                    ReleaseMouseCapture();
                    ResetLastMousePosition();
                }
            }

            base.OnMouseMove(e);
        }

        private static void OnMouseButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                ListBox listBox = (ListBox)sender;

                listBox.ReleaseMouseCapture();
                listBox.ResetLastMousePosition();
            }
        }

        private static void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ListBox listbox = (ListBox)sender;

            // Focus drives the selection when keyboardnavigation is used
            if (!KeyboardNavigation.IsKeyboardMostRecentInputDevice())
                return;

            // Only in case focus moves from one ListBoxItem to another we want the selection to follow focus
            ListBoxItem newListBoxItem = e.NewFocus as ListBoxItem;
            if (newListBoxItem != null && ItemsControlFromItemContainer(newListBoxItem) == listbox)
            {
                DependencyObject oldFocus = e.OldFocus as DependencyObject;
                Visual visualOldFocus = oldFocus as Visual;
                if (visualOldFocus == null)
                {
                    ContentElement ce = oldFocus as ContentElement;
                    if (ce != null)
                        visualOldFocus = KeyboardNavigation.GetParentUIElementFromContentElement(ce);
                }

                if ((visualOldFocus != null && listbox.IsAncestorOf(visualOldFocus))
                    || oldFocus == listbox)
                {
                    listbox.LastActionItem = newListBoxItem;
                    listbox.MakeKeyboardSelection(newListBoxItem);
                }
            }
        }

        /// <summary>
        /// Called when IsMouseCaptured changes on this element.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnIsMouseCapturedChanged(DependencyPropertyChangedEventArgs e)
        {
            // When we take capture, we should start a timer to call
            // us back and do auto scrolling behavior.
            if (IsMouseCaptured)
            {
                Debug.Assert(_autoScrollTimer == null, "IsMouseCaptured went from true to true");
                if (_autoScrollTimer == null)
                {
                    _autoScrollTimer = new DispatcherTimer(DispatcherPriority.SystemIdle);
                    _autoScrollTimer.Interval = AutoScrollTimeout;
                    _autoScrollTimer.Tick += new EventHandler(OnAutoScrollTimeout);
                    _autoScrollTimer.Start();
                }
            }
            else
            {
                if (_autoScrollTimer != null)
                {
                    _autoScrollTimer.Stop();
                    _autoScrollTimer = null;
                }
            }

            base.OnIsMouseCapturedChanged(e);
        }



        /// <summary>
        /// Return true if the item is (or is eligible to be) its own ItemContainer
        /// </summary>
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return (item is ListBoxItem);
        }

        /// <summary> Create or identify the element used to display the given item. </summary>
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new ListBoxItem();
        }

        /// <summary>
        ///     If control has a scrollviewer in its style and has a custom keyboard scrolling behavior when HandlesScrolling should return true.
        /// Then ScrollViewer will not handle keyboard input and leave it up to the control.
        /// </summary>
        protected internal override bool HandlesScrolling
        {
            get
            {
                return true;
            }
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        private static void OnQueryStatusSelectAll(object target, CanExecuteRoutedEventArgs args)
        {
            ListBox listBox = target as ListBox;
            if (listBox.SelectionMode == SelectionMode.Extended)
            {
                args.CanExecute = true;
            }
        }

        private static void OnSelectAll(object target, ExecutedRoutedEventArgs args)
        {
            ListBox listBox = target as ListBox;
            if (listBox.SelectionMode == SelectionMode.Extended)
            {
                listBox.SelectAll();
            }
        }

        internal void NotifyListItemClicked(ListBoxItem item, MouseButton mouseButton)
        {
            // When a ListBoxItem is left clicked, we should take capture
            // so we can auto scroll through the list.
            if (mouseButton == MouseButton.Left && Mouse.Captured != this)
            {
                Mouse.Capture(this, CaptureMode.SubTree);
                SetInitialMousePosition(); // Start tracking mouse movement
            }

            switch (SelectionMode)
            {
                case SelectionMode.Single:
                    {
                        if (!item.IsSelected)
                        {
                            item.SetCurrentValueInternal(IsSelectedProperty, BooleanBoxes.TrueBox);
                        }
                        else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                        {
                            item.SetCurrentValueInternal(IsSelectedProperty, BooleanBoxes.FalseBox);
                        }

                        UpdateAnchorAndActionItem(ItemInfoFromContainer(item));
                    }
                    break;

                case SelectionMode.Multiple:
                    MakeToggleSelection(item);
                    break;

                case SelectionMode.Extended:
                    // Extended selection works only with Left mouse button
                    if (mouseButton == MouseButton.Left)
                    {
                        if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == (ModifierKeys.Control | ModifierKeys.Shift))
                        {
                            MakeAnchorSelection(item, false);
                        }
                        else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                        {
                            MakeToggleSelection(item);
                        }
                        else if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                        {
                            MakeAnchorSelection(item, true);
                        }
                        else
                        {
                            MakeSingleSelection(item);
                        }
                    }
                    else if (mouseButton == MouseButton.Right) // Right mouse button
                    {
                        // Shift or Control combination should not trigger any action
                        // If only Right mouse button is pressed we should move the anchor
                        // and select the item only if element under the mouse is not selected
                        if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == 0)
                        {
                            if (item.IsSelected)
                                UpdateAnchorAndActionItem(ItemInfoFromContainer(item));
                            else
                                MakeSingleSelection(item);
                        }
                    }

                    break;
            }
        }

        internal void NotifyListItemMouseDragged(ListBoxItem listItem)
        {
            if ((Mouse.Captured == this) && DidMouseMove())
            {
                NavigateToItem(ItemInfoFromContainer(listItem), new ItemNavigateArgs(Mouse.PrimaryDevice, Keyboard.Modifiers));
            }
        }

        private void UpdateAnchorAndActionItem(ItemInfo info)
        {
            object item = info.Item;
            ListBoxItem listItem = info.Container as ListBoxItem;

            if (item == DependencyProperty.UnsetValue)
            {
                AnchorItemInternal = null;
                LastActionItem = null;
            }
            else
            {
                AnchorItemInternal = info;
                LastActionItem = listItem;
            }
            KeyboardNavigation.SetTabOnceActiveElement(this, listItem);
        }

        private void MakeSingleSelection(ListBoxItem listItem)
        {
            if (ItemsControlFromItemContainer(listItem) == this)
            {
                ItemInfo info = ItemInfoFromContainer(listItem);

                SelectionChange.SelectJustThisItem(info, true /* assumeInItemsCollection */);

                listItem.Focus();

                UpdateAnchorAndActionItem(info);
            }
        }

        private void MakeToggleSelection(ListBoxItem item)
        {
            bool select = !item.IsSelected;

            item.SetCurrentValueInternal(IsSelectedProperty, BooleanBoxes.Box(select));

            UpdateAnchorAndActionItem(ItemInfoFromContainer(item));
        }

        private void MakeAnchorSelection(ListBoxItem actionItem, bool clearCurrent)
        {
            ItemInfo anchorInfo = AnchorItemInternal;

            if (anchorInfo == null)
            {
                if (_selectedItems.Count > 0)
                {
                    // If we haven't set the anchor, then just use the last selected item
                    AnchorItemInternal = _selectedItems[_selectedItems.Count - 1];
                }
                else
                {
                    // There was nothing selected, so take the first child element
                    AnchorItemInternal = NewItemInfo(Items[0], null, 0);
                }

                if ((anchorInfo = AnchorItemInternal) == null)
                {
                    // Can't do anything
                    return;
                }
            }

            // Find the indexes of the elements
            int start, end;

            start = ElementIndex(actionItem);
            end = AnchorItemInternal.Index;

            // Ensure start is before end
            if (start > end)
            {
                int index = start;

                start = end;
                end = index;
            }

            bool beganSelectionChange = false;
            if (!SelectionChange.IsActive)
            {
                beganSelectionChange = true;
                SelectionChange.Begin();
            }
            try
            {
                if (clearCurrent)
                {
                    // Unselect items not within the selection range
                    for (int index = 0; index < _selectedItems.Count; index++)
                    {
                        ItemInfo info = _selectedItems[index];
                        int itemIndex = info.Index;

                        if ((itemIndex < start) || (end < itemIndex))
                        {
                            SelectionChange.Unselect(info);
                        }
                    }
                }

                // Select the children in the selection range
                IEnumerator enumerator = ((IEnumerable)Items).GetEnumerator();
                for (int index = 0; index <= end; index++)
                {
                    enumerator.MoveNext();
                    if (index >= start)
                    {
                        SelectionChange.Select(NewItemInfo(enumerator.Current, null, index), true /* assumeInItemsCollection */);
                    }
                }

                IDisposable d = enumerator as IDisposable;
                if (d != null)
                {
                    d.Dispose();
                }
            }
            finally
            {
                if (beganSelectionChange)
                {
                    SelectionChange.End();
                }
            }

            LastActionItem = actionItem;
            GC.KeepAlive(anchorInfo);
        }

        private void MakeKeyboardSelection(ListBoxItem item)
        {
            if (item == null)
            {
                return;
            }

            switch (SelectionMode)
            {
                case SelectionMode.Single:
                    // Navigating when control is down shouldn't select the item
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
                    {
                        MakeSingleSelection(item);
                    }
                    break;

                case SelectionMode.Multiple:
                    UpdateAnchorAndActionItem(ItemInfoFromContainer(item));
                    break;

                case SelectionMode.Extended:
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                    {
                        bool clearCurrentSelection = (Keyboard.Modifiers & ModifierKeys.Control) == 0;
                        MakeAnchorSelection(item, clearCurrentSelection);
                    }
                    else if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
                    {
                        MakeSingleSelection(item);
                    }

                    break;
            }
        }

        private int ElementIndex(ListBoxItem listItem)
        {
            return ItemContainerGenerator.IndexFromContainer(listItem);
        }

        private ListBoxItem ElementAt(int index)
        {
            return ItemContainerGenerator.ContainerFromIndex(index) as ListBoxItem;
        }

        private object GetWeakReferenceTarget(ref WeakReference weakReference)
        {
            if (weakReference != null)
            {
                return weakReference.Target;
            }

            return null;
        }

        private void OnAutoScrollTimeout(object sender, EventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                DoAutoScroll();
            }
        }

        /// <summary>
        ///     Called when an item is being focused
        /// </summary>
        internal override bool FocusItem(ItemInfo info, ItemNavigateArgs itemNavigateArgs)
        {
            // Base will actually focus the item
            bool returnValue = base.FocusItem(info, itemNavigateArgs);

            ListBoxItem listItem = info.Container as ListBoxItem;

            if (listItem != null)
            {
                LastActionItem = listItem;

                // pass in the modifier keys!!  Use items instead as well.
                MakeKeyboardSelection(listItem);
            }
            return returnValue;
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        protected object AnchorItem
        {
            get { return AnchorItemInternal; }

            set
            {
                if (value != null && value != DependencyProperty.UnsetValue)
                {
                    ItemInfo info = NewItemInfo(value);
                    ListBoxItem listBoxItem = info.Container as ListBoxItem;
                    if (listBoxItem == null)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.ListBoxInvalidAnchorItem, value));
                    }

                    AnchorItemInternal = info;
                    LastActionItem = listBoxItem;
                }
                else
                {
                    AnchorItemInternal = null;
                    LastActionItem = null;
                }
            }
        }

        /// <summary>
        ///     "Anchor" of the selection.  In extended selection, it is the pivot/anchor of the extended selection.
        /// </summary>
        internal ItemInfo AnchorItemInternal
        {
            get { return _anchorItem; }
            set { _anchorItem = (value != null) ? value.Clone() : null; }   // clone, so that adjustments to selection and anchor don't double-adjust
        }

        /// <summary>
        ///     Last item to be acted upon -- and the element that has focus while selection is happening.
        ///     AnchorItemInternal != null implies LastActionItem != null.
        /// </summary>
        internal ListBoxItem LastActionItem
        {
            get
            {
                return GetWeakReferenceTarget(ref _lastActionItem) as ListBoxItem;
            }
            set
            {
                _lastActionItem = new WeakReference(value);
            }
        }

        private ItemInfo _anchorItem;

        private WeakReference _lastActionItem;

        private DispatcherTimer _autoScrollTimer;

        private static RoutedUICommand SelectAllCommand =
            new RoutedUICommand(SR.Get(SRID.ListBoxSelectAllText), "SelectAll", typeof(ListBox));

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

    /// <summary>
    ///     The selection behavior for the ListBox.
    /// </summary>
    public enum SelectionMode
    {
        /// <summary>
        ///     Only one item can be selected at a time.
        /// </summary>
        Single,
        /// <summary>
        ///     Items can be toggled selected.
        /// </summary>
        Multiple,
        /// <summary>
        ///     Items can be selected in groups using the SHIFT and mouse or arrow keys.
        /// </summary>
        Extended

        // NOTE: if you add or remove any values in this enum, be sure to update ListBox.IsValidSelectionMode()
    }
}
