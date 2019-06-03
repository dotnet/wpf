// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: GroupItem object - root of the UI subtree generated for a CollectionViewGroup
//
// Specs:       Data Styling.mht
//

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using System.Collections.Generic;
using MS.Internal.Utility;
using MS.Internal.Hashing.PresentationFramework;
using System.Diagnostics;
using MS.Internal;
using System.Windows.Automation;

namespace System.Windows.Controls
{
    /// <summary>
    ///     A GroupItem appears as the root of the visual subtree generated for a CollectionViewGroup.
    /// </summary>
    public class GroupItem : ContentControl, IHierarchicalVirtualizationAndScrollInfo, IContainItemStorage
    {
        static GroupItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(GroupItem), new FrameworkPropertyMetadata(typeof(GroupItem)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(GroupItem));

            // GroupItems should not be focusable by default
            FocusableProperty.OverrideMetadata(typeof(GroupItem), new FrameworkPropertyMetadata(false));
            AutomationProperties.IsOffscreenBehaviorProperty.OverrideMetadata(typeof(GroupItem), new FrameworkPropertyMetadata(IsOffscreenBehavior.FromClip));
        }

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        {
            return new System.Windows.Automation.Peers.GroupItemAutomationPeer(this);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _header = this.GetTemplateChild("PART_Header") as FrameworkElement;

            // GroupItem is generally re-templated to have an Expander.
            // Look for an Expander and store its Header size.g
            _expander = Helper.FindTemplatedDescendant<Expander>(this, this);

            //
            // ItemValueStorage:  restore saved values for this item onto the new container
            //
            if (_expander != null)
            {
                ItemsControl itemsControl = ParentItemsControl;
                if (itemsControl != null && VirtualizingPanel.GetIsVirtualizingWhenGrouping(itemsControl))
                {
                    Helper.SetItemValuesOnContainer(itemsControl, _expander, itemsControl.ItemContainerGenerator.ItemFromContainer(this));
                }

                _expander.Expanded += new RoutedEventHandler(OnExpanded);
            }
        }

        private static void OnExpanded(object sender, RoutedEventArgs e)
        {
            GroupItem groupItem = sender as GroupItem;
            if (groupItem != null && groupItem._expander != null && groupItem._expander.IsExpanded)
            {
                ItemsControl itemsControl = groupItem.ParentItemsControl;
                if (itemsControl != null && VirtualizingPanel.GetIsVirtualizing(itemsControl) && VirtualizingPanel.GetVirtualizationMode(itemsControl) == VirtualizationMode.Recycling)
                {
                    ItemsPresenter itemsHostPresenter = groupItem.ItemsHostPresenter;
                    if (itemsHostPresenter != null)
                    {
                        // In case a GroupItem that wasn't previously expanded is now
                        // recycled to represent an entity that is expanded, we face a situation
                        // where the ItemsHost isn't connected yet but we do need to synchronously
                        // remeasure the sub tree through the ItemsPresenter leading up to the
                        // ItemsHost panel. If we didnt do this the offsets could get skewed.
                        groupItem.InvalidateMeasure();
                        Helper.InvalidateMeasureOnPath(itemsHostPresenter, groupItem, false /*duringMeasure*/);
                    }
                }
            }
        }

        internal override void OnTemplateChangedInternal(FrameworkTemplate oldTemplate,FrameworkTemplate newTemplate)
        {
            base.OnTemplateChangedInternal(oldTemplate, newTemplate);

            if (_expander != null)
            {
                _expander.Expanded -= new RoutedEventHandler(OnExpanded);
                _expander = null;
            }

            _itemsHost = null;
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            arrangeSize = base.ArrangeOverride(arrangeSize);

            Helper.ComputeCorrectionFactor(ParentItemsControl, this, ItemsHost, HeaderElement);

            return arrangeSize;
        }

        /// <summary>
        ///     Gives a string representation of this object.
        /// </summary>
        /// <returns></returns>
        internal override string GetPlainText()
        {
            System.Windows.Data.CollectionViewGroup cvg = Content as System.Windows.Data.CollectionViewGroup;
            if (cvg != null && cvg.Name != null)
            {
                return cvg.Name.ToString();
            }

            return base.GetPlainText();
        }

        //------------------------------------------------------
        //
        // Internal Properties
        //
        //------------------------------------------------------

        internal ItemContainerGenerator Generator
        {
            get { return _generator; }
            set { _generator = value; }
        }

        //------------------------------------------------------
        //
        // Internal Methods
        //
        //------------------------------------------------------

        internal void PrepareItemContainer(object item, ItemsControl parentItemsControl)
        {
            if (Generator == null)
                return;     // user-declared GroupItem - ignore (bug 108423)

            // If a GroupItem is being recycled set back IsItemsHost
            if (_itemsHost != null)
            {
                _itemsHost.IsItemsHost = true;
            }

            bool isVirtualizingWhenGrouping = (parentItemsControl != null && VirtualizingPanel.GetIsVirtualizingWhenGrouping(parentItemsControl));

            // Release any previous containers. Also ensures Items and GroupStyle are hooked up correctly
            if (Generator != null)
            {
                if (!isVirtualizingWhenGrouping)
                {
                    Generator.Release();
                }
                else
                {
                    Generator.RemoveAllInternal(true /*saveRecycleQueue*/);
                }
            }

            ItemContainerGenerator generator = Generator.Parent;
            GroupStyle groupStyle = generator.GroupStyle;

            // apply the container style
            Style style = groupStyle.ContainerStyle;

            // no ContainerStyle set, try ContainerStyleSelector
            if (style == null)
            {
                if (groupStyle.ContainerStyleSelector != null)
                {
                    style = groupStyle.ContainerStyleSelector.SelectStyle(item, this);
                }
            }

            // apply the style, if found
            if (style != null)
            {
                // verify style is appropriate before applying it
                if (!style.TargetType.IsInstanceOfType(this))
                    throw new InvalidOperationException(SR.Get(SRID.StyleForWrongType, style.TargetType.Name, this.GetType().Name));

                this.Style = style;
                this.WriteInternalFlag2(InternalFlags2.IsStyleSetFromGenerator, true);
            }

            // forward the header template information
            if (ContentIsItem || !HasNonDefaultValue(ContentProperty))
            {
                this.Content = item;
                ContentIsItem = true;
            }
            if (!HasNonDefaultValue(ContentTemplateProperty))
                this.ContentTemplate = groupStyle.HeaderTemplate;
            if (!HasNonDefaultValue(ContentTemplateSelectorProperty))
                this.ContentTemplateSelector = groupStyle.HeaderTemplateSelector;
            if (!HasNonDefaultValue(ContentStringFormatProperty))
                this.ContentStringFormat = groupStyle.HeaderStringFormat;

            //
            // Clear previously cached items sizes
            //
            Helper.ClearVirtualizingElement(this);

            //
            // ItemValueStorage:  restore saved values for this item onto the new container
            //
            if (isVirtualizingWhenGrouping)
            {
                Helper.SetItemValuesOnContainer(parentItemsControl, this, item);

                if (_expander != null)
                {
                    Helper.SetItemValuesOnContainer(parentItemsControl, _expander, item);
                }
            }
        }

        internal void ClearItemContainer(object item, ItemsControl parentItemsControl)
        {
            if (Generator == null)
                return;     // user-declared GroupItem - ignore (bug 108423)

            //
            // ItemValueStorage:  save off values for this container if we're a virtualizing Group.
            //
            if (parentItemsControl != null && VirtualizingPanel.GetIsVirtualizingWhenGrouping(parentItemsControl))
            {
                Helper.StoreItemValues((IContainItemStorage)parentItemsControl, this, item);

                if (_expander != null)
                {
                    Helper.StoreItemValues((IContainItemStorage)parentItemsControl, _expander, item);
                }

                // Tell the panel to clear off all its containers.  This will cause this method to be called
                // recursively down the tree, allowing all descendent data to be stored before we save off
                // the ItemValueStorage DP for this container.

                VirtualizingPanel vp = _itemsHost as VirtualizingPanel;
                if (vp != null)
                {
                    vp.OnClearChildrenInternal();
                }

                Generator.RemoveAllInternal(true /*saveRecycleQueue*/);
            }
            else
            {
                Generator.Release();
            }

            ClearContentControl(item);
        }

        #region IHierarchicalVirtualizationAndScrollInfo

        HierarchicalVirtualizationConstraints IHierarchicalVirtualizationAndScrollInfo.Constraints
        {
            get { return HierarchicalVirtualizationConstraintsField.GetValue(this); }
            set
            {
                if (value.CacheLengthUnit == VirtualizationCacheLengthUnit.Page)
                {
                    throw new InvalidOperationException(SR.Get(SRID.PageCacheSizeNotAllowed));
                }
                HierarchicalVirtualizationConstraintsField.SetValue(this, value);
            }
        }

        HierarchicalVirtualizationHeaderDesiredSizes IHierarchicalVirtualizationAndScrollInfo.HeaderDesiredSizes
        {
            get
            {
                FrameworkElement headerElement = HeaderElement;
                Size pixelHeaderSize = new Size();

                if (this.IsVisible && headerElement != null)
                {
                    pixelHeaderSize = headerElement.DesiredSize;
                    Helper.ApplyCorrectionFactorToPixelHeaderSize(ParentItemsControl, this, _itemsHost, ref pixelHeaderSize);
                }

                Size logicalHeaderSize = new Size(DoubleUtil.GreaterThan(pixelHeaderSize.Width, 0) ? 1 : 0,
                                DoubleUtil.GreaterThan(pixelHeaderSize.Height, 0) ? 1 : 0);

                return new HierarchicalVirtualizationHeaderDesiredSizes(logicalHeaderSize, pixelHeaderSize);
            }
        }

        HierarchicalVirtualizationItemDesiredSizes IHierarchicalVirtualizationAndScrollInfo.ItemDesiredSizes
        {
            get
            {
                return Helper.ApplyCorrectionFactorToItemDesiredSizes(this, _itemsHost);
            }
            set
            {
                HierarchicalVirtualizationItemDesiredSizesField.SetValue(this, value);
            }
        }

        Panel IHierarchicalVirtualizationAndScrollInfo.ItemsHost
        {
            get
            {
                return _itemsHost;
            }
        }

        bool IHierarchicalVirtualizationAndScrollInfo.MustDisableVirtualization
        {
            get { return MustDisableVirtualizationField.GetValue(this); }
            set { MustDisableVirtualizationField.SetValue(this, value); }
        }

        bool IHierarchicalVirtualizationAndScrollInfo.InBackgroundLayout
        {
            get { return InBackgroundLayoutField.GetValue(this); }
            set { InBackgroundLayoutField.SetValue(this, value); }
        }

        #endregion

        #region ItemValueStorage


        object IContainItemStorage.ReadItemValue(object item, DependencyProperty dp)
        {
            return Helper.ReadItemValue(this, item, dp.GlobalIndex);
        }


        void IContainItemStorage.StoreItemValue(object item, DependencyProperty dp, object value)
        {
            Helper.StoreItemValue(this, item, dp.GlobalIndex, value);
        }

        void IContainItemStorage.ClearItemValue(object item, DependencyProperty dp)
        {
            Helper.ClearItemValue(this, item, dp.GlobalIndex);
        }

        void IContainItemStorage.ClearValue(DependencyProperty dp)
        {
            Helper.ClearItemValueStorage(this, new int[] {dp.GlobalIndex});
        }

        void IContainItemStorage.Clear()
        {
            Helper.ClearItemValueStorage(this);
        }

        #endregion

        private ItemsControl ParentItemsControl
        {
            get
            {
                DependencyObject parent = this;
                do
                {
                    parent = VisualTreeHelper.GetParent(parent);
                    ItemsControl parentItemsControl = parent as ItemsControl;
                    if (parentItemsControl != null)
                    {
                        return parentItemsControl;
                    }
                } while (parent != null);

                return null;
            }
        }

        internal IContainItemStorage ParentItemStorageProvider
        {
            get
            {
                DependencyObject parentPanel = VisualTreeHelper.GetParent(this);
                if (parentPanel != null)
                {
                    DependencyObject owner = ItemsControl.GetItemsOwnerInternal(parentPanel);
                    return owner as IContainItemStorage;
                }

                return null;
            }
        }

        internal Panel ItemsHost
        {
            get
            {
                return _itemsHost;
            }
            set { _itemsHost = value; }
        }

        private ItemsPresenter ItemsHostPresenter
        {
            get
            {
                if (_expander != null)
                {
                    return Helper.FindTemplatedDescendant<ItemsPresenter>(_expander, _expander);
                }
                else
                {
                    return Helper.FindTemplatedDescendant<ItemsPresenter>(this, this);
                }
            }
        }

        internal Expander Expander { get { return _expander; } }

        private FrameworkElement ExpanderHeader
        {
            get
            {
                if (_expander != null)
                {
                    return _expander.GetTemplateChild(ExpanderHeaderPartName) as FrameworkElement;
                }

                return null;
            }
        }

        private FrameworkElement HeaderElement
        {
            get
            {
                FrameworkElement headerElement = null;
                if (_header != null)
                {
                    headerElement = _header;
                }
                else if (_expander != null)
                {
                    // Look for Expander. We special case for Expander since its a very common usage of grouping.
                    headerElement = ExpanderHeader;
                }
                return headerElement;
            }
        }

        //------------------------------------------------------
        //
        // Private Fields
        //
        //------------------------------------------------------

        ItemContainerGenerator _generator;
        private Panel _itemsHost;
        FrameworkElement _header;
        Expander _expander;

        internal static readonly UncommonField<bool> MustDisableVirtualizationField = new UncommonField<bool>();
        internal static readonly UncommonField<bool> InBackgroundLayoutField = new UncommonField<bool>();

        internal static readonly UncommonField<Thickness> DesiredPixelItemsSizeCorrectionFactorField = new UncommonField<Thickness>();

        internal static readonly UncommonField<HierarchicalVirtualizationConstraints> HierarchicalVirtualizationConstraintsField =
            new UncommonField<HierarchicalVirtualizationConstraints>();
        internal static readonly UncommonField<HierarchicalVirtualizationHeaderDesiredSizes> HierarchicalVirtualizationHeaderDesiredSizesField =
            new UncommonField<HierarchicalVirtualizationHeaderDesiredSizes>();
        internal static readonly UncommonField<HierarchicalVirtualizationItemDesiredSizes> HierarchicalVirtualizationItemDesiredSizesField =
            new UncommonField<HierarchicalVirtualizationItemDesiredSizes>();

        #region DTypeThemeStyleKey

        // Returns the DependencyObjectType for the registered ThemeStyleKey's default
        // value. Controls will override this method to return approriate types.
        internal override DependencyObjectType DTypeThemeStyleKey
        {
            get { return _dType; }
        }

        private static DependencyObjectType _dType;

        private const string ExpanderHeaderPartName = "HeaderSite";

        #endregion DTypeThemeStyleKey
    }
}

