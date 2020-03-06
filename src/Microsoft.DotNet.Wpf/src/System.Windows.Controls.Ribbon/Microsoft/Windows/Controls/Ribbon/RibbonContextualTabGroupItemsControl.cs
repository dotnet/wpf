// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon
#else
namespace Microsoft.Windows.Controls.Ribbon
#endif
{
    using System.Windows;
    using System.Windows.Automation.Peers;
    using System.Windows.Controls;
    using System;
#if RIBBON_IN_FRAMEWORK
    using System.Windows.Controls.Ribbon.Primitives;
    using Microsoft.Windows.Controls;
#else
    using Microsoft.Windows.Automation.Peers;
    using Microsoft.Windows.Controls.Ribbon.Primitives;
#endif
    
    /// <summary>
    ///     The itemscontrol which host contextual tab group headers for Ribbon.
    /// </summary>
    public class RibbonContextualTabGroupItemsControl : ItemsControl
    {
        #region Constructors

        static RibbonContextualTabGroupItemsControl()
        {
            Type ownerType = typeof(RibbonContextualTabGroupItemsControl);
            DefaultStyleKeyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(ownerType));
            ItemTemplateProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(null, OnNotifyPropertyChanged, CoerceItemTemplate));
            ItemContainerStyleProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(null, OnNotifyPropertyChanged, CoerceItemContainerStyle));
            VisibilityProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(null, new CoerceValueCallback(CoerceVisibility)));
            FocusableProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(false));

            FrameworkElementFactory factory = new FrameworkElementFactory(typeof(RibbonContextualTabGroupsPanel));
            ItemsPanelTemplate itemsPanel = new ItemsPanelTemplate(factory);
            itemsPanel.Seal();
            ItemsPanelProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(itemsPanel));
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     DependencyProperty for Ribbon property.
        /// </summary>
        public static readonly DependencyProperty RibbonProperty =
            RibbonControlService.RibbonProperty.AddOwner(typeof(RibbonContextualTabGroupItemsControl));

        /// <summary>
        ///     This property is used to access visual style brushes defined on the Ribbon class.
        /// </summary>
        public Ribbon Ribbon
        {
            get { return RibbonControlService.GetRibbon(this); }
        }

        #endregion

        #region Internal Properties

        /// <summary>
        ///     Items panel instance of this ItemsControl
        /// </summary>
        internal Panel InternalItemsHost
        {
            get
            {
                return _itemsHost;
            }
            set
            {
                _itemsHost = value;
            }
        }

        /// <summary>
        /// First visible ContextualTabGroupHeader
        /// </summary>
        internal RibbonContextualTabGroup FirstContextualTabHeader
        {
            get
            {
                return RibbonHelper.FindContainer(this, 0, 1, null, HasTabs) as RibbonContextualTabGroup;
            }
        }

        /// <summary>
        /// Last visible ContextualTabGroupHeader
        /// </summary>
        internal RibbonContextualTabGroup LastContextualTabHeader
        {
            get
            {
                return RibbonHelper.FindContainer(this, Items.Count - 1, -1, null, HasTabs) as RibbonContextualTabGroup;
            }
        }

        internal bool ForceCollapse
        {
            get
            {
                return _forceCollapse;
            }
            set
            {
                if (_forceCollapse != value)
                {
                    _forceCollapse = value;
                    CoerceValue(VisibilityProperty);
                }
            }
        }

        #endregion

        #region Protected Methods

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new RibbonContextualTabGroup();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return (item is RibbonContextualTabGroup);
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new RibbonContextualTabGroupItemsControlAutomationPeer(this);
        }

        public override void OnApplyTemplate()
        {
            // If a new template has just been generated then 
            // be sure to clear any stale ItemsHost references
            if (InternalItemsHost != null && !this.IsAncestorOf(InternalItemsHost))
            {
                InternalItemsHost = null;
            }

            base.OnApplyTemplate();
            SyncProperties();
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);
            RibbonContextualTabGroup tabGroupHeader = element as RibbonContextualTabGroup;
            if (tabGroupHeader != null)
            {
                tabGroupHeader.PrepareTabGroupHeader(item, ItemTemplate, ItemTemplateSelector, ItemStringFormat);
            }
        }

        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            base.ClearContainerForItemOverride(element, item);
            RibbonContextualTabGroup tabGroupHeader = element as RibbonContextualTabGroup;
            if (tabGroupHeader != null)
            {
                tabGroupHeader.ClearTabGroupHeader();
            }

        }

        #endregion

        #region Private Methods

        private bool HasTabs(FrameworkElement container)
        {
            RibbonContextualTabGroup tabGroupHeader = container as RibbonContextualTabGroup;
            if (tabGroupHeader == null ||
                !tabGroupHeader.IsVisible)
            {
                return false;
            }
            foreach (RibbonTab tab in tabGroupHeader.Tabs)
            {
                // DDVSO: 170997 - ContextualTabGroupHeader isn't shown when the WPF Ribbon is hidden.
                // RibbonTab.Visibility shall be used to determine its visibility instead of RibbonTab.IsVisible.
                // When a RibbonTab is collapsed, its IsVisible is false to hide the content of the tab, while its Visibility is still Visible to show the header.
                if (tab != null && tab.Visibility == Visibility.Visible)
                {
                    return true;
                }
            }
            return false;
        }

        private static void OnNotifyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((RibbonContextualTabGroupItemsControl)d).NotifyPropertyChanged(e);
        }

        internal void NotifyPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == ItemTemplateProperty || e.Property == Ribbon.ContextualTabGroupHeaderTemplateProperty)
            {
                PropertyHelper.TransferProperty(this, ItemTemplateProperty);
            }
            else if (e.Property == ItemContainerStyleProperty || e.Property == Ribbon.ContextualTabGroupStyleProperty)
            {
                PropertyHelper.TransferProperty(this, ItemContainerStyleProperty);
            }
        }

        private static object CoerceItemTemplate(DependencyObject d, object baseValue)
        {
            RibbonContextualTabGroupItemsControl me = (RibbonContextualTabGroupItemsControl)d;
            return PropertyHelper.GetCoercedTransferPropertyValue(
                me,
                baseValue,
                ItemTemplateProperty,
                me.Ribbon,
                Ribbon.ContextualTabGroupHeaderTemplateProperty);
        }

        private static object CoerceItemContainerStyle(DependencyObject d, object baseValue)
        {
            RibbonContextualTabGroupItemsControl me = (RibbonContextualTabGroupItemsControl)d;
            return PropertyHelper.GetCoercedTransferPropertyValue(
                me,
                baseValue,
                ItemContainerStyleProperty,
                me.Ribbon,
                Ribbon.ContextualTabGroupStyleProperty);
        }

        private void SyncProperties()
        {
            PropertyHelper.TransferProperty(this, ItemTemplateProperty);
            PropertyHelper.TransferProperty(this, ItemContainerStyleProperty);
        }

        private static object CoerceVisibility(DependencyObject d, object baseValue)
        {
            RibbonContextualTabGroupItemsControl headerItemsControl = (RibbonContextualTabGroupItemsControl)d;
            if (headerItemsControl.ForceCollapse)
            {
                return Visibility.Collapsed;
            }
            return baseValue;
        }

        #endregion

        internal RibbonContextualTabGroup FindHeader(object content)
        {
            int count = this.Items.Count;
            for (int i = 0; i < count; i++)
            {
                RibbonContextualTabGroup tabGroup = this.ItemContainerGenerator.ContainerFromIndex(i) as RibbonContextualTabGroup;
                if (tabGroup != null && Object.Equals(tabGroup.Header, content))
                    return tabGroup;
            }
            return null;
        }

        #region Private Data
        
        private Panel _itemsHost; // ItemsPanel instance for this ItemsControl
        bool _forceCollapse = false;
        
        #endregion
    }
}
