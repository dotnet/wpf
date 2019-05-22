// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.ComponentModel;
using System.Windows.Threading;

using System.Windows.Data;
using System.Windows.Media;

using MS.Utility;
using MS.Internal;
using MS.Internal.Controls;
using MS.Internal.KnownBoxes;
using MS.Internal.Data;
using MS.Internal.PresentationFramework;

namespace System.Windows.Controls
{
    /// <summary>
    ///     The base class for all controls that contain multiple items and have a header.
    /// </summary>
    /// <remarks>
    ///     HeaderedItemsControl adds Header, HeaderTemplate, and Part features to an ItemsControl.
    /// </remarks>
    [DefaultProperty("Header")]
    [Localizability(LocalizationCategory.Menu)]
    public class HeaderedItemsControl : ItemsControl
    {
        #region Constructors

        /// <summary>
        ///     Default HeaderedItemsControl constructor
        /// </summary>
        /// <remarks>
        ///     Automatic determination of current Dispatcher. Use alternative constructor
        ///     that accepts a Dispatcher for best performance.
        /// </remarks>
        public HeaderedItemsControl() : base()
        {
        }

        #endregion

        #region Properties

        /// <summary>
        ///     The DependencyProperty for the Header property.
        ///     Flags:              None
        ///     Default Value:      null
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty HeaderProperty =
                HeaderedContentControl.HeaderProperty.AddOwner(
                        typeof(HeaderedItemsControl),
                        new FrameworkPropertyMetadata(
                                (object) null,
                                new PropertyChangedCallback(OnHeaderChanged)));


        /// <summary>
        ///     Header is the data used to for the header of each item in the control.
        /// </summary>
        [Bindable(true), CustomCategory("Content")]
        public object Header
        {
            get { return GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        /// <summary>
        ///     Called when HeaderProperty is invalidated on "d."
        /// </summary>
        private static void OnHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            HeaderedItemsControl ctrl = (HeaderedItemsControl) d;

            ctrl.SetValue(HasHeaderPropertyKey, (e.NewValue != null) ? BooleanBoxes.TrueBox : BooleanBoxes.FalseBox);
            ctrl.OnHeaderChanged(e.OldValue, e.NewValue);
        }

        /// <summary>
        ///     This method is invoked when the Header property changes.
        /// </summary>
        /// <param name="oldHeader">The old value of the Header property.</param>
        /// <param name="newHeader">The new value of the Header property.</param>
        protected virtual void OnHeaderChanged(object oldHeader, object newHeader)
        {
            // if Header should not be treated as a logical child, there's
            // nothing to do
            if (!IsHeaderLogical())
                return;

            RemoveLogicalChild(oldHeader);
            AddLogicalChild(newHeader);
        }

        /// <summary>
        ///     The key needed set a read-only property.
        /// </summary>
        private static readonly DependencyPropertyKey HasHeaderPropertyKey =
                HeaderedContentControl.HasHeaderPropertyKey;

        /// <summary>
        ///     The DependencyProperty for the HasHeader property.
        ///     Flags:              None
        ///     Other:              Read-Only
        ///     Default Value:      false
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty HasHeaderProperty = HeaderedContentControl.HasHeaderProperty.AddOwner(typeof(HeaderedItemsControl));

        /// <summary>
        ///     True if Header is non-null, false otherwise.
        /// </summary>
        [Bindable(false), Browsable(false)]
        public bool HasHeader
        {
            get { return (bool) GetValue(HasHeaderProperty); }
        }

        /// <summary>
        ///     The DependencyProperty for the HeaderTemplate property.
        ///     Flags:              Can be used in style rules
        ///     Default Value:      null
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty HeaderTemplateProperty =
                HeaderedContentControl.HeaderTemplateProperty.AddOwner(
                        typeof(HeaderedItemsControl),
                        new FrameworkPropertyMetadata(
                                (DataTemplate) null,
                                new PropertyChangedCallback(OnHeaderTemplateChanged)));

        /// <summary>
        ///     HeaderTemplate is the template used to display the header of each item.
        /// </summary>
        [Bindable(true), CustomCategory("Content")]
        public DataTemplate HeaderTemplate
        {
            get { return (DataTemplate) GetValue(HeaderTemplateProperty); }
            set { SetValue(HeaderTemplateProperty, value); }
        }

        /// <summary>
        ///     Called when HeaderTemplateProperty is invalidated on "d."
        /// </summary>
        private static void OnHeaderTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            HeaderedItemsControl ctrl = (HeaderedItemsControl) d;
            ctrl.OnHeaderTemplateChanged((DataTemplate) e.OldValue, (DataTemplate) e.NewValue);
        }

        /// <summary>
        ///     This method is invoked when the HeaderTemplate property changes.
        /// </summary>
        /// <param name="oldHeaderTemplate">The old value of the HeaderTemplate property.</param>
        /// <param name="newHeaderTemplate">The new value of the HeaderTemplate property.</param>
        protected virtual void OnHeaderTemplateChanged(DataTemplate oldHeaderTemplate, DataTemplate newHeaderTemplate)
        {
            Helper.CheckTemplateAndTemplateSelector("Header", HeaderTemplateProperty, HeaderTemplateSelectorProperty, this);
        }


        /// <summary>
        ///     The DependencyProperty for the HeaderTemplateSelector property.
        ///     Flags:              none
        ///     Default Value:      null
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty HeaderTemplateSelectorProperty =
                HeaderedContentControl.HeaderTemplateSelectorProperty.AddOwner(
                        typeof(HeaderedItemsControl),
                        new FrameworkPropertyMetadata(
                                (DataTemplateSelector) null,
                                new PropertyChangedCallback(OnHeaderTemplateSelectorChanged)));

        /// <summary>
        ///     HeaderTemplateSelector allows the application writer to provide custom logic
        ///     for choosing the template used to display the header of each item.
        /// </summary>
        /// <remarks>
        ///     This property is ignored if <seealso cref="HeaderTemplate"/> is set.
        /// </remarks>
        [Bindable(true), CustomCategory("Content")]
        public DataTemplateSelector HeaderTemplateSelector
        {
            get { return (DataTemplateSelector) GetValue(HeaderTemplateSelectorProperty); }
            set { SetValue(HeaderTemplateSelectorProperty, value); }
        }

        /// <summary>
        ///     Called when HeaderTemplateSelectorProperty is invalidated on "d."
        /// </summary>
        private static void OnHeaderTemplateSelectorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            HeaderedItemsControl ctrl = (HeaderedItemsControl) d;
            ctrl.OnHeaderTemplateSelectorChanged((DataTemplateSelector) e.OldValue, (DataTemplateSelector) e.NewValue);
        }

        /// <summary>
        ///     This method is invoked when the HeaderTemplateSelector property changes.
        /// </summary>
        /// <param name="oldHeaderTemplateSelector">The old value of the HeaderTemplateSelector property.</param>
        /// <param name="newHeaderTemplateSelector">The new value of the HeaderTemplateSelector property.</param>
        protected virtual void OnHeaderTemplateSelectorChanged(DataTemplateSelector oldHeaderTemplateSelector, DataTemplateSelector newHeaderTemplateSelector)
        {
            Helper.CheckTemplateAndTemplateSelector("Header", HeaderTemplateProperty, HeaderTemplateSelectorProperty, this);
        }

        /// <summary>
        ///     The DependencyProperty for the HeaderStringFormat property.
        ///     Flags:              None
        ///     Default Value:      null
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty HeaderStringFormatProperty =
                DependencyProperty.Register(
                        "HeaderStringFormat",
                        typeof(String),
                        typeof(HeaderedItemsControl),
                        new FrameworkPropertyMetadata(
                                (String) null,
                              new PropertyChangedCallback(OnHeaderStringFormatChanged)));


        /// <summary>
        ///     HeaderStringFormat is the format used to display the header content as a string.
        ///     This arises only when no template is available.
        /// </summary>
        [Bindable(true), CustomCategory("Content")]
        public String HeaderStringFormat
        {
            get { return (String) GetValue(HeaderStringFormatProperty); }
            set { SetValue(HeaderStringFormatProperty, value); }
        }

        /// <summary>
        ///     Called when HeaderStringFormatProperty is invalidated on "d."
        /// </summary>
        private static void OnHeaderStringFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            HeaderedItemsControl ctrl = (HeaderedItemsControl)d;
            ctrl.OnHeaderStringFormatChanged((String) e.OldValue, (String) e.NewValue);
        }

        /// <summary>
        ///     This method is invoked when the HeaderStringFormat property changes.
        /// </summary>
        /// <param name="oldHeaderStringFormat">The old value of the HeaderStringFormat property.</param>
        /// <param name="newHeaderStringFormat">The new value of the HeaderStringFormat property.</param>
        protected virtual void OnHeaderStringFormatChanged(String oldHeaderStringFormat, String newHeaderStringFormat)
        {
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Prepare to display the item.
        /// </summary>
        internal void PrepareHeaderedItemsControl(object item, ItemsControl parentItemsControl)
        {
            bool headerIsNotLogical = item != this;
            // don't treat Header as a logical child
            WriteControlFlag(ControlBoolFlags.HeaderIsNotLogical, headerIsNotLogical);

            // copy styles from parent ItemsControl
            PrepareItemsControl(item, parentItemsControl);

            if (headerIsNotLogical)
            {
                if (HeaderIsItem || !HasNonDefaultValue(HeaderProperty))
                {
                    Header = item;
                    HeaderIsItem = true;
                }

                DataTemplate itemTemplate = parentItemsControl.ItemTemplate;
                DataTemplateSelector itemTemplateSelector = parentItemsControl.ItemTemplateSelector;
                string itemStringFormat = parentItemsControl.ItemStringFormat;

                if (itemTemplate != null)
                {
                    SetValue(HeaderTemplateProperty, itemTemplate);
                }
                if (itemTemplateSelector != null)
                {
                    SetValue(HeaderTemplateSelectorProperty, itemTemplateSelector);
                }
                if (itemStringFormat != null &&
                    Helper.HasDefaultValue(this, HeaderStringFormatProperty))
                {
                    SetValue(HeaderStringFormatProperty, itemStringFormat);
                }

                PrepareHierarchy(item, parentItemsControl);
            }
        }

        /// <summary>
        /// Undo the effect of PrepareHeaderedItemsControl.
        /// </summary>
        internal void ClearHeaderedItemsControl(object item)
        {
            ClearItemsControl(item);

            if (item != this)
            {
                if (HeaderIsItem)
                {
                    Header = BindingExpressionBase.DisconnectedItem;
                }
            }
        }

        /// <summary>
        ///     Gives a string representation of this object.
        /// </summary>
        /// <returns></returns>
        internal override string GetPlainText()
        {
            return ContentControl.ContentObjectToString(Header);
        }

        #endregion

        #region Method Overrides

        /// <summary>
        ///     Gives a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            string typeText = this.GetType().ToString();
            string headerText = String.Empty;
            int itemCount = 0;
            bool valuesDefined = false;

            // Accessing Header's content may be thread sensitive
            if (CheckAccess())
            {
                headerText = ContentControl.ContentObjectToString(Header);
                // HasItems may be wrong when underlying collection does not notify,
                // but this function should try to return what's consistent with ItemsControl state.
                itemCount = HasItems ? Items.Count : 0;
                valuesDefined = true;
            }
            else
            {
                //Not on dispatcher, try posting to the dispatcher with 20ms timeout
                Dispatcher.Invoke(DispatcherPriority.Send, new TimeSpan(0, 0, 0, 0, 20), new DispatcherOperationCallback(delegate(object o)
                {
                    headerText = ContentControl.ContentObjectToString(Header);
                    // HasItems may be wrong when underlying collection does not notify,
                    // but this function should try to return what's consistent with ItemsControl state.
                    itemCount = HasItems ? Items.Count : 0;
                    valuesDefined = true;
                    return null;
                }), null);
            }

            // If header and items count are defined
            if (valuesDefined)
            {
                return SR.Get(SRID.ToStringFormatString_HeaderedItemsControl, typeText, headerText, itemCount);
            }

            // Not able to access the dispatcher
            return typeText;
        }

        #endregion

        #region LogicalTree

        /// <summary>
        ///     Returns enumerator to logical children
        /// </summary>
        protected internal override IEnumerator LogicalChildren
        {
            get
            {
                object header = Header;

                if (ReadControlFlag(ControlBoolFlags.HeaderIsNotLogical) || header == null)
                {
                    return base.LogicalChildren;
                }

                return new HeaderedItemsModelTreeEnumerator(this, base.LogicalChildren, header);
            }
        }

        #endregion

        #region Private methods

        // As a convenience for hierarchical data, get the header template and
        // if it's a HierarchicalDataTemplate, set the ItemsSource, ItemTemplate,
        // ItemTemplateSelector, and ItemStringFormat properties from the template.
        void PrepareHierarchy(object item, ItemsControl parentItemsControl)
        {
            // get the effective header template
            DataTemplate headerTemplate = HeaderTemplate;

            if (headerTemplate == null)
            {
                DataTemplateSelector selector = HeaderTemplateSelector;
                if (selector != null)
                {
                    headerTemplate = selector.SelectTemplate(item, this);
                }

                if (headerTemplate == null)
                {
                    headerTemplate = (DataTemplate)FindTemplateResourceInternal(this, item, typeof(DataTemplate));
                }
            }

            // if the effective template is a HierarchicalDataTemplate, forward
            // the special properties
            HierarchicalDataTemplate hTemplate = headerTemplate as HierarchicalDataTemplate;
            if (hTemplate != null)
            {
                bool templateMatches = (ItemTemplate == parentItemsControl.ItemTemplate);
                bool containerStyleMatches = (ItemContainerStyle == parentItemsControl.ItemContainerStyle);

                if (hTemplate.ItemsSource != null && !HasNonDefaultValue(ItemsSourceProperty))
                {
                    SetBinding(ItemsSourceProperty, hTemplate.ItemsSource);
                }

                if (hTemplate.IsItemStringFormatSet && ItemStringFormat == parentItemsControl.ItemStringFormat)
                {
                    // if the HDT defines a string format, turn off the
                    // forwarding of ItemTemplate[Selector] (which would get in the way).
                    ClearValue(ItemTemplateProperty);
                    ClearValue(ItemTemplateSelectorProperty);

                    // forward the HDT's string format
                    ClearValue(ItemStringFormatProperty);
                    bool setItemStringFormat = (hTemplate.ItemStringFormat != null);
                    if (setItemStringFormat)
                    {
                        ItemStringFormat = hTemplate.ItemStringFormat;
                    }
                }

                if (hTemplate.IsItemTemplateSelectorSet && ItemTemplateSelector == parentItemsControl.ItemTemplateSelector)
                {
                    // if the HDT defines a template selector, turn off the
                    // forwarding of ItemTemplate (which would get in the way).
                    ClearValue(ItemTemplateProperty);

                    // forward the HDT's template selector
                    ClearValue(ItemTemplateSelectorProperty);
                    bool setItemTemplateSelector = (hTemplate.ItemTemplateSelector != null);
                    if (setItemTemplateSelector)
                    {
                        ItemTemplateSelector = hTemplate.ItemTemplateSelector;
                    }
                }

                if (hTemplate.IsItemTemplateSet && templateMatches)
                {
                    // forward the HDT's template
                    ClearValue(ItemTemplateProperty);
                    bool setItemTemplate = (hTemplate.ItemTemplate != null);
                    if (setItemTemplate)
                    {
                        ItemTemplate = hTemplate.ItemTemplate;
                    }
                }

                if (hTemplate.IsItemContainerStyleSelectorSet && ItemContainerStyleSelector == parentItemsControl.ItemContainerStyleSelector)
                {
                    // if the HDT defines a container-style selector, turn off the
                    // forwarding of ItemContainerStyle (which would get in the way).
                    ClearValue(ItemContainerStyleProperty);

                    // forward the HDT's container-style selector
                    ClearValue(ItemContainerStyleSelectorProperty);
                    bool setItemContainerStyleSelector = (hTemplate.ItemContainerStyleSelector != null);
                    if (setItemContainerStyleSelector)
                    {
                        ItemContainerStyleSelector = hTemplate.ItemContainerStyleSelector;
                    }
                }

                if (hTemplate.IsItemContainerStyleSet && containerStyleMatches)
                {
                    // forward the HDT's container style
                    ClearValue(ItemContainerStyleProperty);
                    bool setItemContainerStyle = (hTemplate.ItemContainerStyle != null);
                    if (setItemContainerStyle)
                    {
                        ItemContainerStyle = hTemplate.ItemContainerStyle;
                    }
                }

                if (hTemplate.IsAlternationCountSet && AlternationCount == parentItemsControl.AlternationCount)
                {
                    // forward the HDT's alternation count
                    ClearValue(AlternationCountProperty);
                    bool setAlternationCount = true;
                    if (setAlternationCount)
                    {
                        AlternationCount = hTemplate.AlternationCount;
                    }
                }

                if (hTemplate.IsItemBindingGroupSet && ItemBindingGroup == parentItemsControl.ItemBindingGroup)
                {
                    // forward the HDT's ItemBindingGroup
                    ClearValue(ItemBindingGroupProperty);
                    bool setItemBindingGroup = (hTemplate.ItemBindingGroup != null);
                    if (setItemBindingGroup)
                    {
                        ItemBindingGroup = hTemplate.ItemBindingGroup;
                    }
                }
            }
        }

        // return true if the dp is bound via the given Binding
        bool IsBound(DependencyProperty dp, Binding binding)
        {
            BindingExpressionBase bindExpr = BindingOperations.GetBindingExpression(this, dp);
            return (bindExpr != null && bindExpr.ParentBindingBase == binding);
        }

        // return true if the Header should be a logical child
        bool IsHeaderLogical()
        {
            // use cached result, if available
            if (ReadControlFlag(ControlBoolFlags.HeaderIsNotLogical))
                return false;

            // if Header property is data-bound, it should not be logical
            if (BindingOperations.IsDataBound(this, HeaderProperty))
            {
                WriteControlFlag(ControlBoolFlags.HeaderIsNotLogical, true);
                return false;
            }

            // otherwise, Header is logical
            return true;
        }

        // return true if the Header is a data item
        bool HeaderIsItem
        {
            get { return ReadControlFlag(ControlBoolFlags.HeaderIsItem); }
            set { WriteControlFlag(ControlBoolFlags.HeaderIsItem, value); }
        }

        #endregion Private methods
    }
}
