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
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Automation;
    using System.Windows.Automation.Peers;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
#if RIBBON_IN_FRAMEWORK
    using System.Windows.Controls.Ribbon.Primitives;
    using Microsoft.Windows.Controls;
#else
    using Microsoft.Windows.Automation.Peers;
    using Microsoft.Windows.Controls.Ribbon.Primitives;
#endif

    #endregion

    [StyleTypedProperty(Property = "HeaderStyle", StyleTargetType = typeof(RibbonTabHeader))]
    public class RibbonTab : HeaderedItemsControl
    {
        #region Constructor
        
        static RibbonTab()
        {
            Type ownerType = typeof(RibbonTab);

            IsEnabledProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(new PropertyChangedCallback(OnIsEnabledChanged)));
            DefaultStyleKeyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(ownerType));
            ItemsPanelProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(new ItemsPanelTemplate(new FrameworkElementFactory(typeof(RibbonGroupsPanel)))));
            HeaderProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(new PropertyChangedCallback(OnHeaderChanged)));
            VisibilityProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(Visibility.Visible, new PropertyChangedCallback(OnVisibilityChanged), new CoerceValueCallback(CoerceVisibility)));
            HeaderTemplateProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(new PropertyChangedCallback(OnNotifyHeaderPropertyChanged), new CoerceValueCallback(CoerceHeaderTemplate)));
            HeaderTemplateSelectorProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(new PropertyChangedCallback(OnNotifyHeaderPropertyChanged)));
            HeaderStringFormatProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(new PropertyChangedCallback(OnNotifyHeaderPropertyChanged)));
            FocusableProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(false));
            EventManager.RegisterClassHandler(ownerType, KeyTipService.ActivatingKeyTipEvent, new ActivatingKeyTipEventHandler(OnActivatingKeyTipThunk));
            EventManager.RegisterClassHandler(ownerType, KeyTipService.KeyTipAccessedEvent, new KeyTipAccessedEventHandler(OnKeyTipAccessedThunk));
            KeyTipService.KeyTipProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(new PropertyChangedCallback(OnKeyTipChanged)));
#if RIBBON_IN_FRAMEWORK
            AutomationProperties.IsOffscreenBehaviorProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(IsOffscreenBehavior.FromClip));
#endif
        }

        #endregion

        #region Properties

#if RIBBON_IN_FRAMEWORK
        protected internal override bool HandlesScrolling
#else
        protected override bool HandlesScrolling
#endif
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        ///     The name collection representing the order in which groups should be reduced.
        /// </summary>
        [TypeConverter(typeof(StringCollectionConverter))]
        public StringCollection GroupSizeReductionOrder
        {
            get { return (StringCollection)GetValue(GroupSizeReductionOrderProperty); }
            set { SetValue(GroupSizeReductionOrderProperty, value); }
        }

        /// <summary>
        ///     Dependency property backing GroupSizeReductionOrder
        /// </summary>
        public static readonly DependencyProperty GroupSizeReductionOrderProperty =
                DependencyProperty.Register(
                            "GroupSizeReductionOrder",
                            typeof(StringCollection),
                            typeof(RibbonTab),
                            new FrameworkPropertyMetadata(null));

        /// <summary>
        ///     Boolean indicating whether this RibbonTab is selected
        /// </summary>
        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        /// <summary>
        ///     Dependency property backing IsSelected
        /// </summary>
        public static readonly DependencyProperty IsSelectedProperty =
                Selector.IsSelectedProperty.AddOwner(typeof(RibbonTab),
                        new FrameworkPropertyMetadata(false,
                                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsParentMeasure | FrameworkPropertyMetadataOptions.Journal,
                                new PropertyChangedCallback(OnIsSelectedChanged)));

        /// <summary>
        ///     Name of the ContextualTabGroupHeader to which this RibbonTab belongs.
        /// </summary>
        public object ContextualTabGroupHeader
        {
            get { return GetValue(ContextualTabGroupHeaderProperty); }
            set { SetValue(ContextualTabGroupHeaderProperty, value); }
        }

        /// <summary>
        ///     Using a DependencyProperty as the backing store for ContextualTabGroupHeader.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty ContextualTabGroupHeaderProperty =
            DependencyProperty.Register("ContextualTabGroupHeader", typeof(object), typeof(RibbonTab), new UIPropertyMetadata(null, new PropertyChangedCallback(OnContextualTabGroupHeaderChanged)));

        private static readonly DependencyPropertyKey ContextualTabGroupPropertyKey =
            DependencyProperty.RegisterReadOnly("ContextualTabGroup", typeof(RibbonContextualTabGroup), typeof(RibbonTab), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnNotifyHeaderPropertyChanged)));

        public static readonly DependencyProperty ContextualTabGroupProperty = ContextualTabGroupPropertyKey.DependencyProperty;

        public RibbonContextualTabGroup ContextualTabGroup
        {
            get { return (RibbonContextualTabGroup)GetValue(ContextualTabGroupProperty);  }
            internal set { SetValue(ContextualTabGroupPropertyKey, value);  }
        }

        public Style HeaderStyle
        {
            get { return (Style)GetValue(HeaderStyleProperty); }
            set { SetValue(HeaderStyleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HeaderStyle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HeaderStyleProperty =
            DependencyProperty.Register("HeaderStyle", typeof(Style), typeof(RibbonTab), new FrameworkPropertyMetadata(null, OnNotifyHeaderPropertyChanged, CoerceHeaderStyle));

        /// <summary>
        ///     DependencyProperty for Ribbon property.
        /// </summary>
        public static readonly DependencyProperty RibbonProperty =
            RibbonControlService.RibbonProperty.AddOwner(typeof(RibbonTab));

        /// <summary>
        ///     This property is used to access Ribbon
        /// </summary>
        public Ribbon Ribbon
        {
            get { return RibbonControlService.GetRibbon(this); }
        }

        /// <summary>
        ///     Boolean indicating if this RibbonTab is a contextual tab.
        /// </summary>
        internal bool IsContextualTab
        {
            get
            {
                return ContextualTabGroupHeader != null;
            }
        }

        /// <summary>
        ///     Property which returns the corresponding RibbonTabHeader for this RibbonTab
        /// </summary>
        internal RibbonTabHeader RibbonTabHeader
        {
            get
            {
                Ribbon ribbon = Ribbon;
                if (ribbon != null)
                {
                    int index = ribbon.ItemContainerGenerator.IndexFromContainer(this);
                    if (index >= 0)
                    {
                        RibbonTabHeaderItemsControl headerItemsControl = ribbon.RibbonTabHeaderItemsControl;
                        if (headerItemsControl != null)
                        {
                            return headerItemsControl.ItemContainerGenerator.ContainerFromIndex(index) as RibbonTabHeader;
                        }
                    }
                }
                return null;
            }
        }

        private static readonly DependencyPropertyKey TabHeaderLeftPropertyKey =
            DependencyProperty.RegisterReadOnly("TabHeaderLeft", typeof(double), typeof(RibbonTab), null);

        public static readonly DependencyProperty TabHeaderLeftProperty = TabHeaderLeftPropertyKey.DependencyProperty;

        /// <summary>
        ///     This is position of the left edge of the corresponding RibbonTabHeader in the coordinate space of this RibbonTab
        /// </summary>
        public double TabHeaderLeft
        {
            get { return (double)GetValue(TabHeaderLeftProperty); }
            internal set { SetValue(TabHeaderLeftPropertyKey, value); }
        }

        private static readonly DependencyPropertyKey TabHeaderRightPropertyKey =
            DependencyProperty.RegisterReadOnly("TabHeaderRight", typeof(double), typeof(RibbonTab), null);

        public static readonly DependencyProperty TabHeaderRightProperty = TabHeaderRightPropertyKey.DependencyProperty;

        /// <summary>
        ///     This is position of the right edge of the corresponding RibbonTabHeader in the coordinate space of this RibbonTab
        /// </summary>
        public double TabHeaderRight
        {
            get { return (double)GetValue(TabHeaderRightProperty); }
            internal set { SetValue(TabHeaderRightPropertyKey, value); }
        }

        #endregion

        #region Protected Methods
        
        /// <summary>
        ///     Returns a new RibbonGroup as the item container
        /// </summary>
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new RibbonGroup();
        }

        /// <summary>
        ///     An item is its own container if it is RibbonGroup.
        /// </summary>
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return (item is RibbonGroup);
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            ItemsControl childItemsControl = element as ItemsControl;
            if (childItemsControl != null)
            {
                // copy templates and styles from this ItemsControl
                var itemTemplate = RibbonHelper.GetValueAndValueSource(childItemsControl, ItemsControl.ItemTemplateProperty);
                var itemTemplateSelector = RibbonHelper.GetValueAndValueSource(childItemsControl, ItemsControl.ItemTemplateSelectorProperty);
                var itemStringFormat = RibbonHelper.GetValueAndValueSource(childItemsControl, ItemsControl.ItemStringFormatProperty);
                var itemContainerStyle = RibbonHelper.GetValueAndValueSource(childItemsControl, ItemsControl.ItemContainerStyleProperty);
                var itemContainerStyleSelector = RibbonHelper.GetValueAndValueSource(childItemsControl, ItemsControl.ItemContainerStyleSelectorProperty);
                var alternationCount = RibbonHelper.GetValueAndValueSource(childItemsControl, ItemsControl.AlternationCountProperty);
                var itemBindingGroup = RibbonHelper.GetValueAndValueSource(childItemsControl, ItemsControl.ItemBindingGroupProperty);

                base.PrepareContainerForItemOverride(element, item);

                // Call this function to work around a restriction of supporting hetrogenous 
                // ItemsCotnrol hierarchy. The method takes care of both ItemsControl and
                // HeaderedItemsControl (in this case) and assign back the default properties
                // whereever appropriate.
                RibbonHelper.IgnoreDPInheritedFromParentItemsControl(
                    childItemsControl,
                    this,
                    itemTemplate,
                    itemTemplateSelector,
                    itemStringFormat,
                    itemContainerStyle,
                    itemContainerStyleSelector,
                    alternationCount,
                    itemBindingGroup,
                    null,
                    null,
                    null);
            }
            else
            {
                base.PrepareContainerForItemOverride(element, item);
            }

            RibbonGroup ribbonGroup = element as RibbonGroup;
            if (ribbonGroup != null)
            {
                ribbonGroup.PrepareRibbonGroup();
            }
        }

        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            base.ClearContainerForItemOverride(element, item);
            RibbonGroup ribbonGroup = element as RibbonGroup;
            if (ribbonGroup != null)
            {
                ribbonGroup.ClearRibbonGroup();
            }
        }

        /// <summary>
        ///     Raises event indicating that the IsSelected property is now true.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected virtual void OnSelected(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        ///     Raises event indicating that the IsSelected property is now false.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected virtual void OnUnselected(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    _groupAutoResizeIndex = null;
                    _groupReduceOrderLocation = -1;
                    _automaticResizeOrder.Clear();
                    _groupReductionResizeStatus.Clear();
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (GroupSizeReductionOrder != null)
                    {
                        int removedCount = e.OldItems.Count;
                        for (int i = 0; i < removedCount; i++)
                        {
                            int deletedItemIndex = i + e.OldStartingIndex;
                            for (int index = 0; index < _automaticResizeOrder.Count; index++)
                            {
                                if (deletedItemIndex == _automaticResizeOrder[index])
                                {
                                    _automaticResizeOrder.RemoveAt(index--);
                                }
                                else if (deletedItemIndex < _automaticResizeOrder[index])
                                {
                                    _automaticResizeOrder[index]--;
                                }
                            }

                            if (_groupAutoResizeIndex != null &&
                                _groupAutoResizeIndex.Value > deletedItemIndex)
                            {
                                _groupAutoResizeIndex--;

                                // If we have underflowed our Groups collection, start again at the end.  This
                                // is what makes our group reduction cyclical.
                                if (_groupAutoResizeIndex < 0)
                                {
                                    _groupAutoResizeIndex = Items.Count - 1;
                                }
                            }
                        }
                    }
                    break;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (!e.Handled)
            {
                DependencyObject focusedElement = Keyboard.FocusedElement as DependencyObject;
                RibbonTabHeader tabHeader = RibbonTabHeader;
                if (e.Key == Key.Up && focusedElement != null && tabHeader != null)
                {
                    // On arrow up key press if the focus goes out of the tab,
                    // then force it to move to the corresponding TabHeader.
                    DependencyObject upObj = RibbonHelper.PredictFocus(focusedElement, FocusNavigationDirection.Up);
                    if (!RibbonHelper.IsAncestorOf(this, upObj))
                    {
                        if (tabHeader.Focus())
                        {
                            e.Handled = true;
                        }
                    }
                }
            }
        }

        #endregion

        #region Resizing Logic

        /// <summary>
        ///     Method which finds the next group to
        ///     increase size of and increases it.
        /// </summary>
        internal bool IncreaseNextGroupSize()
        {
            RibbonGroup nextGroup = null;
            return IncreaseNextGroupSize(true, out nextGroup);
        }

        /// <summary>
        ///     Method which finds the next group to 
        ///     increase the size of.
        /// </summary>
        internal RibbonGroup GetNextIncreaseSizeGroup()
        {
            RibbonGroup nextGroup = null;
            IncreaseNextGroupSize(false, out nextGroup);
            return nextGroup;
        }

        /// <summary>
        ///     If the application developer has specified a GroupSizeReductionOrder, this
        ///     takes the next group in that order and tells it to increase to its next size.
        ///     If no GroupSizeReductionOrder was specified, or if we collapsed RibbonGroups
        ///     beyond what was specified by the developer, we expand groups in reverse order
        ///     of their reduction.
        /// </summary>
        /// <returns>True if a group was able to be expanded in size, false otherwise.</returns>
        private bool IncreaseNextGroupSize(bool update, out RibbonGroup nextRibbonGroup)
        {
            nextRibbonGroup = null;
            bool resizeSuccessful = false;
            int automaticResizeOrderCount = _automaticResizeOrder.Count;
            while (automaticResizeOrderCount > 0 && !resizeSuccessful)
            {
                int nextGroupIndex = _automaticResizeOrder[automaticResizeOrderCount - 1];
                nextRibbonGroup = ItemContainerGenerator.ContainerFromIndex(nextGroupIndex) as RibbonGroup;
                if (nextRibbonGroup != null)
                {
                    resizeSuccessful = nextRibbonGroup.IncreaseGroupSize(update);
                }
                if (update)
                {
                    _automaticResizeOrder.RemoveAt(automaticResizeOrderCount - 1);
                    _groupAutoResizeIndex = nextGroupIndex;
                }
                automaticResizeOrderCount--;
            }

            if (!resizeSuccessful)
            {
                if (GroupSizeReductionOrder != null &&
                    _groupReduceOrderLocation >= 0)
                {
                    int groupReduceOrderLocation = _groupReduceOrderLocation;
                    int resizeStatusCount = _groupReductionResizeStatus.Count;
                    while (groupReduceOrderLocation >= 0 && !resizeSuccessful)
                    {
                        Debug.Assert(resizeStatusCount > 0);
                        bool wasResizeSuccessful = _groupReductionResizeStatus[resizeStatusCount - 1];
                        if (update)
                        {
                            _groupReductionResizeStatus.RemoveAt(resizeStatusCount - 1);
                        }
                        resizeStatusCount--;
                        if (!wasResizeSuccessful)
                        {
                            groupReduceOrderLocation--;
                            continue;
                        }

                        // Find the RibbonGroup whose name is specified next in the GroupSizeReductionOrder.
                        nextRibbonGroup = FindRibbonGroupWithName(GroupSizeReductionOrder[groupReduceOrderLocation--]);

                        if (nextRibbonGroup == null)
                        {
                            resizeSuccessful = false;
                        }
                        else
                        {
                            // A group was found, tell it to increase its size.
                            resizeSuccessful = nextRibbonGroup.IncreaseGroupSize(update);
                        }
                    }
                    if (update)
                    {
                        _groupReduceOrderLocation = groupReduceOrderLocation;
                    }
                }
            }

            if (!resizeSuccessful)
            {
                nextRibbonGroup = null;
            }
            return resizeSuccessful;
        }

        /// <summary>
        ///     If the application developer has specified a GroupSizeReductionOrder, this
        ///     takes the next group in that order and tells it to reduce to its next size.
        ///     If no GroupSizeReductionOrder was specified, or if we need to collapse RibbonGroups
        ///     beyond what was specified by the developer, we reduce groups from right-to-left,
        ///     step by step in cyclical order.
        /// </summary>
        /// <returns>
        ///     Returns true if a group was located and resized successfully, false otherwise.
        /// </returns>
        internal bool DecreaseNextGroupSize()
        {
            bool resizeSuccessful = false;
            if (GroupSizeReductionOrder != null)
            {
                while (_groupReduceOrderLocation < GroupSizeReductionOrder.Count - 1 && !resizeSuccessful)
                {
                    // Find the group who's next to be reduced.
                    RibbonGroup targetGroup = FindRibbonGroupWithName(GroupSizeReductionOrder[++_groupReduceOrderLocation]);

                    if (targetGroup == null)
                    {
                        resizeSuccessful = false;
                    }
                    else
                    {
                        resizeSuccessful = targetGroup.DecreaseGroupSize();
                    }
                    _groupReductionResizeStatus.Add(resizeSuccessful);
                }
            }

            if (!resizeSuccessful)
            {
                // Either no GroupSizeReductionOrder was specified, or we've run out of predefined orderings.
                // In this case we should begin reducing groups in size right-to-left, step by step, in cyclical
                // order.
                resizeSuccessful = DefaultCyclicalReduceGroup();
            }

            return resizeSuccessful;
        }

        /// <summary>
        ///     From right-to-left, finds the next group who can be reduced in size.  If the leftmost
        ///     group is encountered reduction will continue in a cyclical fashion back at the rightmost
        ///     RibbonGroup.
        /// </summary>
        /// <returns>True if a group was successfully located and reduced in size, false otherwise.</returns>
        private bool DefaultCyclicalReduceGroup()
        {
            bool resizeSuccessful = false;

            if (_groupAutoResizeIndex == null)
            {
                _groupAutoResizeIndex = Items.Count - 1;
            }

            bool resizesRemain = true;

            while (resizesRemain && !resizeSuccessful)
            {
                int numAttempts = 0;
                do
                {
                    numAttempts++;
                    RibbonGroup group = ItemContainerGenerator.ContainerFromIndex((_groupAutoResizeIndex--).Value) as RibbonGroup;
                    if (group != null)
                    {
                        resizeSuccessful = group.DecreaseGroupSize();
                    }

                    if (resizeSuccessful == true)
                    {
                        _automaticResizeOrder.Add(_groupAutoResizeIndex.Value + 1);
                    }

                    // If we have underflowed our Groups collection, start again at the end.  This
                    // is what makes our group reduction cyclical.
                    if (_groupAutoResizeIndex.Value < 0)
                    {
                        _groupAutoResizeIndex = Items.Count - 1;
                        break;
                    }
                } while (resizeSuccessful == false);

                // If we failed to resize during this pass, and we attempted to resize for every
                // group, then there are no reamining groups to resize.
                if (numAttempts == Items.Count)
                {
                    resizesRemain = false;
                }
            }

            return resizeSuccessful;
        }
        #endregion

        #region Automation

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new RibbonTabAutomationPeer(this);
        }

        #endregion

        #region Internal Methods

        internal void PrepareRibbonTab()
        {
            if (ContextualTabGroupHeader != null && Ribbon.ContextualTabGroupItemsControl != null)
            {
                ContextualTabGroup = Ribbon.ContextualTabGroupItemsControl.FindHeader(ContextualTabGroupHeader);
            }

            CoerceValue(VisibilityProperty);

            RibbonTabHeader tabHeader = RibbonTabHeader;
            if (tabHeader != null)
            {
                tabHeader.InitializeTransferProperties();
            }
        }

        internal void NotifyPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == HeaderStyleProperty || e.Property == Ribbon.TabHeaderStyleProperty)
            {
                PropertyHelper.TransferProperty(this, HeaderStyleProperty);
            }
            else if (e.Property == HeaderTemplateProperty || e.Property == Ribbon.TabHeaderTemplateProperty)
            {
                PropertyHelper.TransferProperty(this, HeaderTemplateProperty);
            }
        }

        #endregion

        #region Private Methods

        private static object CoerceVisibility(DependencyObject d, object value)
        {
            Visibility baseVisibility = (Visibility)value;
            Visibility contextualVisibility = Visibility.Visible;
            RibbonTab tab = (RibbonTab)d;
            bool contextualHeaderSet = tab.ContextualTabGroupHeader != null;

            if (tab.ContextualTabGroup == null && contextualHeaderSet)
            {
                if (tab.Ribbon != null && tab.Ribbon.ContextualTabGroupItemsControl != null)
                {
                    tab.ContextualTabGroup = tab.Ribbon.ContextualTabGroupItemsControl.FindHeader(tab.ContextualTabGroupHeader);
                }
            }

            if (tab.ContextualTabGroup != null)
            {
                contextualVisibility = tab.ContextualTabGroup.Visibility;
            }
            else if (contextualHeaderSet)
            {
                contextualVisibility = Visibility.Collapsed;
            }

            if (baseVisibility != Visibility.Visible ||
                contextualVisibility != Visibility.Visible)
            {
                return Visibility.Collapsed;
            }
            else
            {
                return Visibility.Visible;
            }
        }

        private static void OnVisibilityChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            RibbonTab tab = (RibbonTab)sender;
            if (tab.RibbonTabHeader != null)
            {
                tab.RibbonTabHeader.CoerceValue(VisibilityProperty);
            }

            // If the selected tab goes from visible to no longer visible, then reset the Ribbon's selected tab.
            Ribbon ribbon = tab.Ribbon;
            if (ribbon != null &&
                tab.IsSelected &&
                (Visibility)e.OldValue == Visibility.Visible &&
                (Visibility)e.NewValue != Visibility.Visible)
            {
                ribbon.ResetSelection();
            }
        }

        /// <summary>
        ///     Property changed callback for Header property
        /// </summary>
        private static void OnHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonTab tab = (RibbonTab)d;
            Ribbon ribbon = tab.Ribbon;
            if (ribbon != null)
            {
                ribbon.NotifyTabHeaderChanged();
            }
            OnNotifyHeaderPropertyChanged(d, e);
        }

        /// <summary>
        ///     Property changed called back for IsSelected property
        /// </summary>
        private static void OnIsSelectedChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            RibbonTab ribbonTab = (RibbonTab)sender;
            if (ribbonTab.IsSelected)
            {
                ribbonTab.OnSelected(new RoutedEventArgs(Selector.SelectedEvent, ribbonTab));
            }
            else
            {
                ribbonTab.OnUnselected(new RoutedEventArgs(Selector.UnselectedEvent, ribbonTab));
            }
            RibbonTabHeader header = ribbonTab.RibbonTabHeader;
            if (header != null)
            {
                header.CoerceValue(RibbonTabHeader.IsRibbonTabSelectedProperty);
            }

            // Raise UI automation events on this RibbonTab
            if ( AutomationPeer.ListenerExists(AutomationEvents.SelectionItemPatternOnElementSelected)
                || AutomationPeer.ListenerExists(AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection))
            {
                RibbonTabAutomationPeer peer = RibbonTabAutomationPeer.CreatePeerForElement(ribbonTab) as RibbonTabAutomationPeer;
                if (peer != null)
                {
                    peer.RaiseTabSelectionEvents();
                }
            }
        }

        /// <summary>
        ///     Property changed callback for IsEnabled property.
        /// <summary>
        private static void OnIsEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            RibbonTab ribbonTab = (RibbonTab)sender;
            RibbonTabHeader header = ribbonTab.RibbonTabHeader;
            if (header != null)
            {
                header.CoerceValue(RibbonTabHeader.IsEnabledProperty);
            }
        }

        /// <summary>
        ///     Property changed callback for ContextualTabGroupHeader property
        /// </summary>
        private static void OnContextualTabGroupHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonTab ribbonTab = (RibbonTab)d;
            Ribbon ribbon = ribbonTab.Ribbon;
            if (ribbon != null)
            {
                ribbon.NotifyTabContextualTabGroupHeaderChanged();
                if (e.NewValue != null && ribbonTab.Ribbon.ContextualTabGroupItemsControl != null)
                {
                    ribbonTab.ContextualTabGroup = ribbonTab.Ribbon.ContextualTabGroupItemsControl.FindHeader(e.NewValue);
                }
                else
                {
                    ribbonTab.ContextualTabGroup = null;
                }
                OnNotifyHeaderPropertyChanged(d, e);

                ribbonTab.CoerceValue(VisibilityProperty);
            }
        }

        private RibbonGroup FindRibbonGroupWithName(string groupName)
        {
            if (groupName != null)
            {
                groupName = groupName.Trim();
            }
            if (string.IsNullOrEmpty(groupName))
            {
                return null;
            }
            int itemCount = Items.Count;
            for (int i = 0; i < itemCount; i++)
            {
                RibbonGroup group = ItemContainerGenerator.ContainerFromIndex(i) as RibbonGroup;
                if (group != null && group.Name == groupName)
                {
                    return group;
                }
            }
            return null;
        }

        private static void OnNotifyHeaderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonTab tab = (RibbonTab)d;
            tab.NotifyPropertyChanged(e);
            RibbonTabHeader tabHeader = tab.RibbonTabHeader;
            if (tabHeader != null)
            {
                tabHeader.NotifyPropertyChanged(e);
            }
        }

        private static object CoerceHeaderStyle(DependencyObject d, object baseValue)
        {
            RibbonTab ribbonTab = (RibbonTab)d;
            return PropertyHelper.GetCoercedTransferPropertyValue(ribbonTab,
                baseValue,
                HeaderStyleProperty,
                ribbonTab.Ribbon,
                Ribbon.TabHeaderStyleProperty);
        }

        private static object CoerceHeaderTemplate(DependencyObject d, object baseValue)
        {
            RibbonTab ribbonTab = (RibbonTab)d;
            return PropertyHelper.GetCoercedTransferPropertyValue(ribbonTab,
                baseValue,
                HeaderTemplateProperty,
                ribbonTab.Ribbon,
                Ribbon.TabHeaderTemplateProperty);
        }

        #endregion

        #region Private Data

        private int _groupReduceOrderLocation = -1;
        private int? _groupAutoResizeIndex;
        private Collection<int> _automaticResizeOrder = new Collection<int>();
        private Collection<bool> _groupReductionResizeStatus = new Collection<bool>();

        #endregion

        #region KeyTips

        private static void OnKeyTipChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonTab tab = (RibbonTab)d;
            RibbonTabHeader tabHeader = tab.RibbonTabHeader;
            if (tabHeader != null)
            {
                tabHeader.CoerceValue(KeyTipService.KeyTipProperty);
            }
        }

        /// <summary>
        ///     DependencyProperty for KeyTip property.
        /// </summary>
        public static readonly DependencyProperty KeyTipProperty =
            KeyTipService.KeyTipProperty.AddOwner(typeof(RibbonTab));

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
            ((RibbonTab)sender).OnActivatingKeyTip(e);
        }

        protected virtual void OnActivatingKeyTip(ActivatingKeyTipEventArgs e)
        {
            if (e.OriginalSource == this)
            {
                // Disable the keytip. The KeyTip is
                // actually used by RibbonTabHeader and hence
                // that will take care of this.
                e.KeyTipVisibility = Visibility.Collapsed;
            }
        }

        private static void OnKeyTipAccessedThunk(object sender, KeyTipAccessedEventArgs e)
        {
            ((RibbonTab)sender).OnKeyTipAccessed(e);
        }

        protected virtual void OnKeyTipAccessed(KeyTipAccessedEventArgs e)
        {
        }

        #endregion
    }
}
