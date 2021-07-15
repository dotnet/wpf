// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows;
using System.Windows.Media;
using System.Windows.Markup;
using System.Windows.Input;
using System.Windows.Automation.Peers;

using MS.Utility;
using MS.Internal;
using MS.Internal.Controls;
using MS.Internal.Data;
using MS.Internal.Hashing.PresentationFramework;    // HashHelper
using MS.Internal.KnownBoxes;
using MS.Internal.PresentationFramework;
using MS.Internal.Utility;

namespace System.Windows.Controls
{
    /// <summary>
    ///     The base class for all controls that have multiple children.
    /// </summary>
    /// <remarks>
    ///     ItemsControl adds Items, ItemTemplate, and Part features to a Control.
    /// </remarks>
    // Needs DefaultEvent("ItemsChanged") ?
    [DefaultEvent("OnItemsChanged"), DefaultProperty("Items")]
    [ContentProperty("Items")]
    [StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(FrameworkElement))]
    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)] // cannot be read & localized as string
    public class ItemsControl : Control, IAddChild, IGeneratorHost, IContainItemStorage
    {
        #region Constructors

        /// <summary>
        ///     Default ItemsControl constructor
        /// </summary>
        /// <remarks>
        ///     Automatic determination of current Dispatcher. Use alternative constructor
        ///     that accepts a Dispatcher for best performance.
        /// </remarks>
        public ItemsControl() : base()
        {
            ShouldCoerceCacheSizeField.SetValue(this, true);
            this.CoerceValue(VirtualizingPanel.CacheLengthUnitProperty);
        }

        static ItemsControl()
        {
            // Define default style in code instead of in theme files.
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ItemsControl), new FrameworkPropertyMetadata(typeof(ItemsControl)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(ItemsControl));
            EventManager.RegisterClassHandler(typeof(ItemsControl), Keyboard.GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(OnGotFocus));
            VirtualizingStackPanel.ScrollUnitProperty.OverrideMetadata(typeof(ItemsControl), new FrameworkPropertyMetadata(new PropertyChangedCallback(OnScrollingModeChanged), new CoerceValueCallback(CoerceScrollingMode)));
            VirtualizingPanel.CacheLengthProperty.OverrideMetadata(typeof(ItemsControl), new FrameworkPropertyMetadata(new PropertyChangedCallback(OnCacheSizeChanged)));
            VirtualizingPanel.CacheLengthUnitProperty.OverrideMetadata(typeof(ItemsControl), new FrameworkPropertyMetadata(new PropertyChangedCallback(OnCacheSizeChanged), new CoerceValueCallback(CoerceVirtualizationCacheLengthUnit)));
        }

        private static void OnScrollingModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ShouldCoerceScrollUnitField.SetValue(d, true);
            d.CoerceValue(VirtualizingStackPanel.ScrollUnitProperty);
        }

        private static object CoerceScrollingMode(DependencyObject d, object baseValue)
        {
            if (ShouldCoerceScrollUnitField.GetValue(d))
            {
                ShouldCoerceScrollUnitField.SetValue(d, false);
                BaseValueSource baseValueSource = DependencyPropertyHelper.GetValueSource(d, VirtualizingStackPanel.ScrollUnitProperty).BaseValueSource;
                if (((ItemsControl)d).IsGrouping && baseValueSource == BaseValueSource.Default)
                {
                    return ScrollUnit.Pixel;
                }
            }

            return baseValue;
        }

        private static void OnCacheSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ShouldCoerceCacheSizeField.SetValue(d, true);
            d.CoerceValue(e.Property);
        }

        //default VCLU will be Item for the flat non-grouping case
        private static object CoerceVirtualizationCacheLengthUnit(DependencyObject d, object baseValue)
        {
            if (ShouldCoerceCacheSizeField.GetValue(d))
            {
                ShouldCoerceCacheSizeField.SetValue(d, false);
                BaseValueSource baseValueSource = DependencyPropertyHelper.GetValueSource(d, VirtualizingStackPanel.CacheLengthUnitProperty).BaseValueSource;
                if ( !((ItemsControl)d).IsGrouping && !(d is TreeView) && baseValueSource == BaseValueSource.Default )
                {
                    return VirtualizationCacheLengthUnit.Item;
                }
            }

            return baseValue;
        }

        private void CreateItemCollectionAndGenerator()
        {
            _items = new ItemCollection(this);

            // ItemInfos must get adjusted before the generator's change handler is called,
            // so that any new ItemInfos arising from the generator don't get adjusted by mistake
            // (see Win8 690623).
            ((INotifyCollectionChanged)_items).CollectionChanged += new NotifyCollectionChangedEventHandler(OnItemCollectionChanged1);

            // the generator must attach its collection change handler before
            // the control itself, so that the generator is up-to-date by the
            // time the control tries to use it (bug 892806 et al.)
            _itemContainerGenerator = new ItemContainerGenerator(this);

            _itemContainerGenerator.ChangeAlternationCount();

            ((INotifyCollectionChanged)_items).CollectionChanged += new NotifyCollectionChangedEventHandler(OnItemCollectionChanged2);

            if (IsInitPending)
            {
                _items.BeginInit();
            }
            else if (IsInitialized)
            {
                _items.BeginInit();
                _items.EndInit();
            }

            ((INotifyCollectionChanged)_groupStyle).CollectionChanged += new NotifyCollectionChangedEventHandler(OnGroupStyleChanged);
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Items is the collection of data that is used to generate the content
        ///     of this control.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [Bindable(true), CustomCategory("Content")]
        public ItemCollection Items
        {
            get
            {
                if (_items == null)
                {
                    CreateItemCollectionAndGenerator();
                }

                return _items;
            }
        }

        /// <summary>
        /// This method is used by TypeDescriptor to determine if this property should
        /// be serialized.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeItems()
        {
            return HasItems;
        }

        /// <summary>
        ///     The DependencyProperty for the ItemsSource property.
        ///     Flags:              None
        ///     Default Value:      null
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty ItemsSourceProperty
            = DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(ItemsControl),
                                          new FrameworkPropertyMetadata((IEnumerable)null,
                                                                        new PropertyChangedCallback(OnItemsSourceChanged)));

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ItemsControl ic = (ItemsControl) d;
            IEnumerable oldValue = (IEnumerable)e.OldValue;
            IEnumerable newValue = (IEnumerable)e.NewValue;

            ((IContainItemStorage)ic).Clear();

            BindingExpressionBase beb = BindingOperations.GetBindingExpressionBase(d, ItemsSourceProperty);
            if (beb != null)
            {
                // ItemsSource is data-bound.   Always go to ItemsSource mode.
                // Also, extract the source item, to supply as context to the
                // CollectionRegistering event
                ic.Items.SetItemsSource(newValue, (object x)=>beb.GetSourceItem(x) );
            }
            else if (e.NewValue != null)
            {
                // ItemsSource is non-null, but not data-bound.  Go to ItemsSource mode
                ic.Items.SetItemsSource(newValue);
            }
            else
            {
                // ItemsSource is explicitly null.  Return to normal mode.
                ic.Items.ClearItemsSource();
            }

            ic.OnItemsSourceChanged(oldValue, newValue);
        }

        /// <summary>
        /// Called when the value of ItemsSource changes.
        /// </summary>
        protected virtual void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
        }

        /// <summary>
        ///     ItemsSource specifies a collection used to generate the content of
        /// this control.  This provides a simple way to use exactly one collection
        /// as the source of content for this control.
        /// </summary>
        /// <remarks>
        ///     Any existing contents of the Items collection is replaced when this
        /// property is set. The Items collection will be made ReadOnly and FixedSize.
        ///     When ItemsSource is in use, setting this property to null will remove
        /// the collection and restore use to Items (which will be an empty ItemCollection).
        ///     When ItemsSource is not in use, the value of this property is null, and
        /// setting it to null has no effect.
        /// </remarks>
        [Bindable(true), CustomCategory("Content")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IEnumerable ItemsSource
        {
            get { return Items.ItemsSource; }
            set
            {
                if (value == null)
                {
                    ClearValue(ItemsSourceProperty);
                }
                else
                {
                    SetValue(ItemsSourceProperty, value);
                }
            }
        }

        /// <summary>
        /// The ItemContainerGenerator associated with this control
        /// </summary>
        [Bindable(false), Browsable(false), EditorBrowsable(EditorBrowsableState.Advanced)]
        public ItemContainerGenerator ItemContainerGenerator
        {
            get
            {
                if (_itemContainerGenerator == null)
                {
                    CreateItemCollectionAndGenerator();
                }

                return _itemContainerGenerator;
            }
        }

        /// <summary>
        ///     Returns enumerator to logical children
        /// </summary>
        protected internal override IEnumerator LogicalChildren
        {
            get
            {
                if (!HasItems)
                {
                    return EmptyEnumerator.Instance;
                }

                // Items in direct-mode of ItemCollection are the only model children.
                // note: the enumerator walks the ItemCollection.InnerList as-is,
                // no flattening of any content on model children level!
                return this.Items.LogicalChildren;
            }
        }

        // this is called before the generator's change handler
        private void OnItemCollectionChanged1(object sender, NotifyCollectionChangedEventArgs e)
        {
            AdjustItemInfoOverride(e);
        }

        // this is called after the generator's change handler
        private void OnItemCollectionChanged2(object sender, NotifyCollectionChangedEventArgs e)
        {
            SetValue(HasItemsPropertyKey, (_items != null) && !_items.IsEmpty);

            // If the focused item is removed, drop our reference to it.
            if (_focusedInfo != null && _focusedInfo.Index < 0)
            {
                _focusedInfo = null;
            }

            // on Reset, discard item storage
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                ((IContainItemStorage)this).Clear();
            }

            OnItemsChanged(e);
        }

        /// <summary>
        ///     This method is invoked when the Items property changes.
        /// </summary>
        protected virtual void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
        }

        /// <summary>
        ///     Adjust ItemInfos when the Items property changes.
        /// </summary>
        internal virtual void AdjustItemInfoOverride(NotifyCollectionChangedEventArgs e)
        {
            AdjustItemInfo(e, _focusedInfo);
        }

        /// <summary>
        ///     The key needed set a read-only property.
        /// </summary>
        internal static readonly DependencyPropertyKey HasItemsPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "HasItems",
                        typeof(bool),
                        typeof(ItemsControl),
                        new FrameworkPropertyMetadata(BooleanBoxes.FalseBox, OnVisualStatePropertyChanged));

        /// <summary>
        ///     The DependencyProperty for the HasItems property.
        ///     Flags:              None
        ///     Other:              Read-Only
        ///     Default Value:      false
        /// </summary>
        public static readonly DependencyProperty HasItemsProperty =
                HasItemsPropertyKey.DependencyProperty;

        /// <summary>
        ///     True if Items.Count > 0, false otherwise.
        /// </summary>
        [Bindable(false), Browsable(false)]
        public bool HasItems
        {
            get { return (bool) GetValue(HasItemsProperty); }
        }

        /// <summary>
        ///     The DependencyProperty for the DisplayMemberPath property.
        ///     Flags:              none
        ///     Default Value:      string.Empty
        /// </summary>
        public static readonly DependencyProperty DisplayMemberPathProperty =
                DependencyProperty.Register(
                        "DisplayMemberPath",
                        typeof(string),
                        typeof(ItemsControl),
                        new FrameworkPropertyMetadata(
                                string.Empty,
                                new PropertyChangedCallback(OnDisplayMemberPathChanged)));

        /// <summary>
        ///     DisplayMemberPath is a simple way to define a default template
        ///     that describes how to convert Items into UI elements by using
        ///     the specified path.
        /// </summary>
        [Bindable(true), CustomCategory("Content")]
        public string DisplayMemberPath
        {
            get { return (string) GetValue(DisplayMemberPathProperty); }
            set { SetValue(DisplayMemberPathProperty, value); }
        }

        /// <summary>
        ///     Called when DisplayMemberPathProperty is invalidated on "d."
        /// </summary>
        private static void OnDisplayMemberPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ItemsControl ctrl = (ItemsControl) d;

            ctrl.OnDisplayMemberPathChanged((string)e.OldValue, (string)e.NewValue);
            ctrl.UpdateDisplayMemberTemplateSelector();
        }

        // DisplayMemberPath and ItemStringFormat use the ItemTemplateSelector property
        // to achieve the desired result.  When either of these properties change,
        // update the ItemTemplateSelector property here.
        private void UpdateDisplayMemberTemplateSelector()
        {
            string displayMemberPath = DisplayMemberPath;
            string itemStringFormat = ItemStringFormat;

            if (!String.IsNullOrEmpty(displayMemberPath) || !String.IsNullOrEmpty(itemStringFormat))
            {
                // One or both of DisplayMemberPath and ItemStringFormat are desired.
                // Set ItemTemplateSelector to an appropriate object, provided that
                // this doesn't conflict with the user's own setting.
                DataTemplateSelector itemTemplateSelector = ItemTemplateSelector;
                if (itemTemplateSelector != null && !(itemTemplateSelector is DisplayMemberTemplateSelector))
                {
                    // if ITS was actually set to something besides a DisplayMember selector,
                    // it's an error to overwrite it with a DisplayMember selector
                    // unless ITS came from a style and DMP is local
                    if (ReadLocalValue(ItemTemplateSelectorProperty) != DependencyProperty.UnsetValue ||
                        ReadLocalValue(DisplayMemberPathProperty) == DependencyProperty.UnsetValue)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.DisplayMemberPathAndItemTemplateSelectorDefined));
                    }
                }

                // now set the ItemTemplateSelector to use the new DisplayMemberPath and ItemStringFormat
                ItemTemplateSelector = new DisplayMemberTemplateSelector(DisplayMemberPath, ItemStringFormat);
            }
            else
            {
                // Neither property is desired.  Clear the ItemTemplateSelector if
                // we had set it earlier.
                if (ItemTemplateSelector is DisplayMemberTemplateSelector)
                {
                    ClearValue(ItemTemplateSelectorProperty);
                }
            }
        }

        /// <summary>
        ///     This method is invoked when the DisplayMemberPath property changes.
        /// </summary>
        /// <param name="oldDisplayMemberPath">The old value of the DisplayMemberPath property.</param>
        /// <param name="newDisplayMemberPath">The new value of the DisplayMemberPath property.</param>
        protected virtual void OnDisplayMemberPathChanged(string oldDisplayMemberPath, string newDisplayMemberPath)
        {
        }

        /// <summary>
        ///     The DependencyProperty for the ItemTemplate property.
        ///     Flags:              none
        ///     Default Value:      null
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty ItemTemplateProperty =
                DependencyProperty.Register(
                        "ItemTemplate",
                        typeof(DataTemplate),
                        typeof(ItemsControl),
                        new FrameworkPropertyMetadata(
                                (DataTemplate) null,
                                new PropertyChangedCallback(OnItemTemplateChanged)));

        /// <summary>
        ///     ItemTemplate is the template used to display each item.
        /// </summary>
        [Bindable(true), CustomCategory("Content")]
        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate) GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        /// <summary>
        ///     Called when ItemTemplateProperty is invalidated on "d."
        /// </summary>
        /// <param name="d">The object on which the property was invalidated.</param>
        /// <param name="e">EventArgs that contains the old and new values for this property</param>
        private static void OnItemTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ItemsControl) d).OnItemTemplateChanged((DataTemplate) e.OldValue, (DataTemplate) e.NewValue);
        }

        /// <summary>
        ///     This method is invoked when the ItemTemplate property changes.
        /// </summary>
        /// <param name="oldItemTemplate">The old value of the ItemTemplate property.</param>
        /// <param name="newItemTemplate">The new value of the ItemTemplate property.</param>
        protected virtual void OnItemTemplateChanged(DataTemplate oldItemTemplate, DataTemplate newItemTemplate)
        {
            CheckTemplateSource();

            if (_itemContainerGenerator != null)
            {
                _itemContainerGenerator.Refresh();
            }
        }


        /// <summary>
        ///     The DependencyProperty for the ItemTemplateSelector property.
        ///     Flags:              none
        ///     Default Value:      null
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty ItemTemplateSelectorProperty =
                DependencyProperty.Register(
                        "ItemTemplateSelector",
                        typeof(DataTemplateSelector),
                        typeof(ItemsControl),
                        new FrameworkPropertyMetadata(
                                (DataTemplateSelector) null,
                                new PropertyChangedCallback(OnItemTemplateSelectorChanged)));

        /// <summary>
        ///     ItemTemplateSelector allows the application writer to provide custom logic
        ///     for choosing the template used to display each item.
        /// </summary>
        /// <remarks>
        ///     This property is ignored if <seealso cref="ItemTemplate"/> is set.
        /// </remarks>
        [Bindable(true), CustomCategory("Content")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DataTemplateSelector ItemTemplateSelector
        {
            get { return (DataTemplateSelector) GetValue(ItemTemplateSelectorProperty); }
            set { SetValue(ItemTemplateSelectorProperty, value); }
        }

        /// <summary>
        ///     Called when ItemTemplateSelectorProperty is invalidated on "d."
        /// </summary>
        /// <param name="d">The object on which the property was invalidated.</param>
        /// <param name="e">EventArgs that contains the old and new values for this property</param>
        private static void OnItemTemplateSelectorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ItemsControl)d).OnItemTemplateSelectorChanged((DataTemplateSelector) e.OldValue, (DataTemplateSelector) e.NewValue);
        }

        /// <summary>
        ///     This method is invoked when the ItemTemplateSelector property changes.
        /// </summary>
        /// <param name="oldItemTemplateSelector">The old value of the ItemTemplateSelector property.</param>
        /// <param name="newItemTemplateSelector">The new value of the ItemTemplateSelector property.</param>
        protected virtual void OnItemTemplateSelectorChanged(DataTemplateSelector oldItemTemplateSelector, DataTemplateSelector newItemTemplateSelector)
        {
            CheckTemplateSource();

            if ((_itemContainerGenerator != null) && (ItemTemplate == null))
            {
                _itemContainerGenerator.Refresh();
            }
        }

        /// <summary>
        ///     The DependencyProperty for the ItemStringFormat property.
        ///     Flags:              None
        ///     Default Value:      null
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty ItemStringFormatProperty =
                DependencyProperty.Register(
                        "ItemStringFormat",
                        typeof(String),
                        typeof(ItemsControl),
                        new FrameworkPropertyMetadata(
                                (String) null,
                              new PropertyChangedCallback(OnItemStringFormatChanged)));


        /// <summary>
        ///     ItemStringFormat is the format used to display an item (or a
        ///     property of an item, as declared by DisplayMemberPath) as a string.
        ///     This arises only when no template is available.
        /// </summary>
        [Bindable(true), CustomCategory("Content")]
        public String ItemStringFormat
        {
            get { return (String) GetValue(ItemStringFormatProperty); }
            set { SetValue(ItemStringFormatProperty, value); }
        }

        /// <summary>
        ///     Called when ItemStringFormatProperty is invalidated on "d."
        /// </summary>
        private static void OnItemStringFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ItemsControl ctrl = (ItemsControl)d;

            ctrl.OnItemStringFormatChanged((String) e.OldValue, (String) e.NewValue);
            ctrl.UpdateDisplayMemberTemplateSelector();
        }

        /// <summary>
        ///     This method is invoked when the ItemStringFormat property changes.
        /// </summary>
        /// <param name="oldItemStringFormat">The old value of the ItemStringFormat property.</param>
        /// <param name="newItemStringFormat">The new value of the ItemStringFormat property.</param>
        protected virtual void OnItemStringFormatChanged(String oldItemStringFormat, String newItemStringFormat)
        {
        }


        /// <summary>
        ///     The DependencyProperty for the ItemBindingGroup property.
        ///     Flags:              None
        ///     Default Value:      null
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty ItemBindingGroupProperty =
                DependencyProperty.Register(
                        "ItemBindingGroup",
                        typeof(BindingGroup),
                        typeof(ItemsControl),
                        new FrameworkPropertyMetadata(
                                (BindingGroup) null,
                              new PropertyChangedCallback(OnItemBindingGroupChanged)));


        /// <summary>
        ///     ItemBindingGroup declares a BindingGroup to be used as a "master"
        ///     for the generated containers.  Each container's BindingGroup is set
        ///     to a copy of the master, sharing the same set of validation rules,
        ///     but managing its own collection of bindings.
        /// </summary>
        [Bindable(true), CustomCategory("Content")]
        public BindingGroup ItemBindingGroup
        {
            get { return (BindingGroup) GetValue(ItemBindingGroupProperty); }
            set { SetValue(ItemBindingGroupProperty, value); }
        }

        /// <summary>
        ///     Called when ItemBindingGroupProperty is invalidated on "d."
        /// </summary>
        private static void OnItemBindingGroupChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ItemsControl ctrl = (ItemsControl)d;

            ctrl.OnItemBindingGroupChanged((BindingGroup) e.OldValue, (BindingGroup) e.NewValue);
        }

        /// <summary>
        ///     This method is invoked when the ItemBindingGroup property changes.
        /// </summary>
        /// <param name="oldItemBindingGroup">The old value of the ItemBindingGroup property.</param>
        /// <param name="newItemBindingGroup">The new value of the ItemBindingGroup property.</param>
        protected virtual void OnItemBindingGroupChanged(BindingGroup oldItemBindingGroup, BindingGroup newItemBindingGroup)
        {
        }


        /// <summary>
        /// Throw if more than one of DisplayMemberPath, xxxTemplate and xxxTemplateSelector
        /// properties are set on the given element.
        /// </summary>
        private void CheckTemplateSource()
        {
            if (string.IsNullOrEmpty(DisplayMemberPath))
            {
                Helper.CheckTemplateAndTemplateSelector("Item", ItemTemplateProperty, ItemTemplateSelectorProperty, this);
            }
            else
            {
                if (!(this.ItemTemplateSelector is DisplayMemberTemplateSelector))
                {
                    throw new InvalidOperationException(SR.Get(SRID.ItemTemplateSelectorBreaksDisplayMemberPath));
                }
                if (Helper.IsTemplateDefined(ItemTemplateProperty, this))
                {
                    throw new InvalidOperationException(SR.Get(SRID.DisplayMemberPathAndItemTemplateDefined));
                }
            }
        }

        /// <summary>
        ///     The DependencyProperty for the ItemContainerStyle property.
        ///     Flags:              none
        ///     Default Value:      null
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty ItemContainerStyleProperty =
                DependencyProperty.Register(
                        "ItemContainerStyle",
                        typeof(Style),
                        typeof(ItemsControl),
                        new FrameworkPropertyMetadata(
                                (Style) null,
                                new PropertyChangedCallback(OnItemContainerStyleChanged)));

        /// <summary>
        ///     ItemContainerStyle is the style that is applied to the container element generated
        ///     for each item.
        /// </summary>
        [Bindable(true), Category("Content")]
        public Style ItemContainerStyle
        {
            get { return (Style) GetValue(ItemContainerStyleProperty); }
            set { SetValue(ItemContainerStyleProperty, value); }
        }

        /// <summary>
        ///     Called when ItemContainerStyleProperty is invalidated on "d."
        /// </summary>
        /// <param name="d">The object on which the property was invalidated.</param>
        /// <param name="e">EventArgs that contains the old and new values for this property</param>
        private static void OnItemContainerStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ItemsControl) d).OnItemContainerStyleChanged((Style) e.OldValue, (Style) e.NewValue);
        }

        /// <summary>
        ///     This method is invoked when the ItemContainerStyle property changes.
        /// </summary>
        /// <param name="oldItemContainerStyle">The old value of the ItemContainerStyle property.</param>
        /// <param name="newItemContainerStyle">The new value of the ItemContainerStyle property.</param>
        protected virtual void OnItemContainerStyleChanged(Style oldItemContainerStyle, Style newItemContainerStyle)
        {
            Helper.CheckStyleAndStyleSelector("ItemContainer", ItemContainerStyleProperty, ItemContainerStyleSelectorProperty, this);

            if (_itemContainerGenerator != null)
            {
                _itemContainerGenerator.Refresh();
            }
        }


        /// <summary>
        ///     The DependencyProperty for the ItemContainerStyleSelector property.
        ///     Flags:              none
        ///     Default Value:      null
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty ItemContainerStyleSelectorProperty =
                DependencyProperty.Register(
                        "ItemContainerStyleSelector",
                        typeof(StyleSelector),
                        typeof(ItemsControl),
                        new FrameworkPropertyMetadata(
                                (StyleSelector) null,
                                new PropertyChangedCallback(OnItemContainerStyleSelectorChanged)));

        /// <summary>
        ///     ItemContainerStyleSelector allows the application writer to provide custom logic
        ///     to choose the style to apply to each generated container element.
        /// </summary>
        /// <remarks>
        ///     This property is ignored if <seealso cref="ItemContainerStyle"/> is set.
        /// </remarks>
        [Bindable(true), Category("Content")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public StyleSelector ItemContainerStyleSelector
        {
            get { return (StyleSelector) GetValue(ItemContainerStyleSelectorProperty); }
            set { SetValue(ItemContainerStyleSelectorProperty, value); }
        }

        /// <summary>
        ///     Called when ItemContainerStyleSelectorProperty is invalidated on "d."
        /// </summary>
        /// <param name="d">The object on which the property was invalidated.</param>
        /// <param name="e">EventArgs that contains the old and new values for this property</param>
        private static void OnItemContainerStyleSelectorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ItemsControl) d).OnItemContainerStyleSelectorChanged((StyleSelector) e.OldValue, (StyleSelector) e.NewValue);
        }

        /// <summary>
        ///     This method is invoked when the ItemContainerStyleSelector property changes.
        /// </summary>
        /// <param name="oldItemContainerStyleSelector">The old value of the ItemContainerStyleSelector property.</param>
        /// <param name="newItemContainerStyleSelector">The new value of the ItemContainerStyleSelector property.</param>
        protected virtual void OnItemContainerStyleSelectorChanged(StyleSelector oldItemContainerStyleSelector, StyleSelector newItemContainerStyleSelector)
        {
            Helper.CheckStyleAndStyleSelector("ItemContainer", ItemContainerStyleProperty, ItemContainerStyleSelectorProperty, this);

            if ((_itemContainerGenerator != null) && (ItemContainerStyle == null))
            {
                _itemContainerGenerator.Refresh();
            }
        }

        /// <summary>
        ///     Returns the ItemsControl for which element is an ItemsHost.
        ///     More precisely, if element is marked by setting IsItemsHost="true"
        ///     in the style for an ItemsControl, or if element is a panel created
        ///     by the ItemsPresenter for an ItemsControl, return that ItemsControl.
        ///     Otherwise, return null.
        /// </summary>
        public static ItemsControl GetItemsOwner(DependencyObject element)
        {
            ItemsControl container = null;
            Panel panel = element as Panel;

            if (panel != null && panel.IsItemsHost)
            {
                // see if element was generated for an ItemsPresenter
                ItemsPresenter ip = ItemsPresenter.FromPanel(panel);

                if (ip != null)
                {
                    // if so use the element whose style begat the ItemsPresenter
                    container = ip.Owner;
                }
                else
                {
                    // otherwise use element's templated parent
                    container = panel.TemplatedParent as ItemsControl;
                }
            }

            return container;
        }

        internal static DependencyObject GetItemsOwnerInternal(DependencyObject element)
        {
            ItemsControl temp;
            return GetItemsOwnerInternal(element, out temp);
        }

        /// <summary>
        /// Different from public GetItemsOwner
        /// Returns ip.TemplatedParent instead of ip.Owner
        /// More accurate when we want to distinguish if owner is a GroupItem or ItemsControl
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        internal static DependencyObject GetItemsOwnerInternal(DependencyObject element, out ItemsControl itemsControl)
        {
            DependencyObject container = null;
            Panel panel = element as Panel;
            itemsControl = null;

            if (panel != null && panel.IsItemsHost)
            {
                // see if element was generated for an ItemsPresenter
                ItemsPresenter ip = ItemsPresenter.FromPanel(panel);

                if (ip != null)
                {
                    // if so use the element whose style begat the ItemsPresenter
                    container = ip.TemplatedParent;
                    itemsControl = ip.Owner;
                }
                else
                {
                    // otherwise use element's templated parent
                    container = panel.TemplatedParent;
                    itemsControl = container as ItemsControl;
                }
            }

            return container;
        }

        /// <summary>
        ///     The DependencyProperty for the ItemsPanel property.
        ///     Flags:              none
        ///     Default Value:      null
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty ItemsPanelProperty
            = DependencyProperty.Register("ItemsPanel", typeof(ItemsPanelTemplate), typeof(ItemsControl),
                                          new FrameworkPropertyMetadata(GetDefaultItemsPanelTemplate(),
                                                                        new PropertyChangedCallback(OnItemsPanelChanged)));

        private static ItemsPanelTemplate GetDefaultItemsPanelTemplate()
        {
            ItemsPanelTemplate template = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(StackPanel)));
            template.Seal();
            return template;
        }

        /// <summary>
        ///     ItemsPanel is the panel that controls the layout of items.
        ///     (More precisely, the panel that controls layout is created
        ///     from the template given by ItemsPanel.)
        /// </summary>
        [Bindable(false)]
        public ItemsPanelTemplate ItemsPanel
        {
            get { return (ItemsPanelTemplate) GetValue(ItemsPanelProperty); }
            set { SetValue(ItemsPanelProperty, value); }
        }

        /// <summary>
        ///     Called when ItemsPanelProperty is invalidated on "d."
        /// </summary>
        /// <param name="d">The object on which the property was invalidated.</param>
        /// <param name="e">EventArgs that contains the old and new values for this property</param>
        private static void OnItemsPanelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ItemsControl) d).OnItemsPanelChanged((ItemsPanelTemplate) e.OldValue, (ItemsPanelTemplate) e.NewValue);
        }

        /// <summary>
        ///     This method is invoked when the ItemsPanel property changes.
        /// </summary>
        /// <param name="oldItemsPanel">The old value of the ItemsPanel property.</param>
        /// <param name="newItemsPanel">The new value of the ItemsPanel property.</param>
        protected virtual void OnItemsPanelChanged(ItemsPanelTemplate oldItemsPanel, ItemsPanelTemplate newItemsPanel)
        {
            ItemContainerGenerator.OnPanelChanged();
        }


        private static readonly DependencyPropertyKey IsGroupingPropertyKey =
            DependencyProperty.RegisterReadOnly("IsGrouping", typeof(bool), typeof(ItemsControl), new FrameworkPropertyMetadata(BooleanBoxes.FalseBox, new PropertyChangedCallback(OnIsGroupingChanged)));

        /// <summary>
        ///     The DependencyProperty for the IsGrouping property.
        /// </summary>
        public static readonly DependencyProperty IsGroupingProperty = IsGroupingPropertyKey.DependencyProperty;

        /// <summary>
        ///     Returns whether the control is using grouping.
        /// </summary>
        [Bindable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsGrouping
        {
            get
            {
                return (bool)GetValue(IsGroupingProperty);
            }
        }

        private static void OnIsGroupingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ItemsControl)d).OnIsGroupingChanged(e);
        }

        internal virtual void OnIsGroupingChanged(DependencyPropertyChangedEventArgs e)
        {
            ShouldCoerceScrollUnitField.SetValue(this, true);
            CoerceValue(VirtualizingStackPanel.ScrollUnitProperty);

            ShouldCoerceCacheSizeField.SetValue(this, true);
            CoerceValue(VirtualizingPanel.CacheLengthUnitProperty);

            ((IContainItemStorage)this).Clear();
        }

        /// <summary>
        /// The collection of GroupStyle objects that describes the display of
        /// each level of grouping.  The entry at index 0 describes the top level
        /// groups, the entry at index 1 describes the next level, and so forth.
        /// If there are more levels of grouping than entries in the collection,
        /// the last entry is used for the extra levels.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public ObservableCollection<GroupStyle> GroupStyle
        {
            get { return _groupStyle; }
        }

        /// <summary>
        /// This method is used by TypeDescriptor to determine if this property should
        /// be serialized.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeGroupStyle()
        {
            return (GroupStyle.Count > 0);
        }

        private void OnGroupStyleChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_itemContainerGenerator != null)
            {
                _itemContainerGenerator.Refresh();
            }
        }


        /// <summary>
        ///     The DependencyProperty for the GroupStyleSelector property.
        ///     Flags:              none
        ///     Default Value:      null
        /// </summary>
        public static readonly DependencyProperty GroupStyleSelectorProperty
            = DependencyProperty.Register("GroupStyleSelector", typeof(GroupStyleSelector), typeof(ItemsControl),
                                          new FrameworkPropertyMetadata((GroupStyleSelector)null,
                                                                        new PropertyChangedCallback(OnGroupStyleSelectorChanged)));

        /// <summary>
        ///     GroupStyleSelector allows the app writer to provide custom selection logic
        ///     for a GroupStyle to apply to each group collection.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Bindable(true), CustomCategory("Content")]
        public GroupStyleSelector GroupStyleSelector
        {
            get { return (GroupStyleSelector) GetValue(GroupStyleSelectorProperty); }
            set { SetValue(GroupStyleSelectorProperty, value); }
        }

        /// <summary>
        ///     Called when GroupStyleSelectorProperty is invalidated on "d."
        /// </summary>
        /// <param name="d">The object on which the property was invalidated.</param>
        /// <param name="e">EventArgs that contains the old and new values for this property</param>
        private static void OnGroupStyleSelectorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ItemsControl) d).OnGroupStyleSelectorChanged((GroupStyleSelector) e.OldValue, (GroupStyleSelector) e.NewValue);
        }

        /// <summary>
        ///     This method is invoked when the GroupStyleSelector property changes.
        /// </summary>
        /// <param name="oldGroupStyleSelector">The old value of the GroupStyleSelector property.</param>
        /// <param name="newGroupStyleSelector">The new value of the GroupStyleSelector property.</param>
        protected virtual void OnGroupStyleSelectorChanged(GroupStyleSelector oldGroupStyleSelector, GroupStyleSelector newGroupStyleSelector)
        {
            if (_itemContainerGenerator != null)
            {
                _itemContainerGenerator.Refresh();
            }
        }

        /// <summary>
        ///     The DependencyProperty for the AlternationCount property.
        ///     Flags:              none
        ///     Default Value:      0
        /// </summary>
        public static readonly DependencyProperty AlternationCountProperty =
                DependencyProperty.Register(
                        "AlternationCount",
                        typeof(int),
                        typeof(ItemsControl),
                        new FrameworkPropertyMetadata(
                                (int)0,
                                new PropertyChangedCallback(OnAlternationCountChanged)));

        /// <summary>
        ///     AlternationCount controls the range of values assigned to the
        ///     AlternationIndex property attached to each generated container.  The
        ///     default value 0 means "do not set AlternationIndex".  A positive
        ///     value means "assign AlternationIndex in the range [0, AlternationCount)
        ///     so that adjacent containers receive different values".
        /// </summary>
        /// <remarks>
        ///     By referring to AlternationIndex in a trigger or binding (typically
        ///     in the ItemContainerStyle), you can make the appearance of items
        ///     depend on their position in the display.  For example, you can make
        ///     the background color of the items in ListBox alternate between
        ///     blue and white.
        /// </remarks>
        [Bindable(true), CustomCategory("Content")]
        public int AlternationCount
        {
            get { return (int) GetValue(AlternationCountProperty); }
            set { SetValue(AlternationCountProperty, value); }
        }

        /// <summary>
        ///     Called when AlternationCountProperty is invalidated on "d."
        /// </summary>
        private static void OnAlternationCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ItemsControl ctrl = (ItemsControl) d;

            int oldAlternationCount = (int) e.OldValue;
            int newAlternationCount = (int) e.NewValue;

            ctrl.OnAlternationCountChanged(oldAlternationCount, newAlternationCount);
        }

        /// <summary>
        ///     This method is invoked when the AlternationCount property changes.
        /// </summary>
        /// <param name="oldAlternationCount">The old value of the AlternationCount property.</param>
        /// <param name="newAlternationCount">The new value of the AlternationCount property.</param>
        protected virtual void OnAlternationCountChanged(int oldAlternationCount, int newAlternationCount)
        {
            ItemContainerGenerator.ChangeAlternationCount();
        }

        private static readonly DependencyPropertyKey AlternationIndexPropertyKey =
                    DependencyProperty.RegisterAttachedReadOnly(
                                "AlternationIndex",
                                typeof(int),
                                typeof(ItemsControl),
                                new FrameworkPropertyMetadata((int)0));

        /// <summary>
        /// AlternationIndex is set on containers generated for an ItemsControl, when
        /// the ItemsControl's AlternationCount property is positive.  The AlternationIndex
        /// lies in the range [0, AlternationCount), and adjacent containers always get
        /// assigned different values.
        /// </summary>
        public static readonly DependencyProperty AlternationIndexProperty =
                    AlternationIndexPropertyKey.DependencyProperty;

        /// <summary>
        /// Static getter for the AlternationIndex attached property.
        /// </summary>
        public static int GetAlternationIndex(DependencyObject element)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            return (int)element.GetValue(AlternationIndexProperty);
        }

        // internal setter for AlternationIndex.  This property is not settable by
        // an app, only by internal code
        internal static void SetAlternationIndex(DependencyObject d, int value)
        {
            d.SetValue(AlternationIndexPropertyKey, value);
        }

        // internal clearer for AlternationIndex.  This property is not settable by
        // an app, only by internal code
        internal static void ClearAlternationIndex(DependencyObject d)
        {
            d.ClearValue(AlternationIndexPropertyKey);
        }

        /// <summary>
        ///     The DependencyProperty for the IsTextSearchEnabled property.
        ///     Default Value:      false
        /// </summary>
        public static readonly DependencyProperty IsTextSearchEnabledProperty =
                DependencyProperty.Register(
                        "IsTextSearchEnabled",
                        typeof(bool),
                        typeof(ItemsControl),
                        new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        ///     Whether TextSearch is enabled or not on this ItemsControl
        /// </summary>
        public bool IsTextSearchEnabled
        {
            get { return (bool) GetValue(IsTextSearchEnabledProperty); }
            set { SetValue(IsTextSearchEnabledProperty, BooleanBoxes.Box(value)); }
        }

        /// <summary>
        ///     The DependencyProperty for the IsTextSearchCaseSensitive property.
        ///     Default Value:      false
        /// </summary>
        public static readonly DependencyProperty IsTextSearchCaseSensitiveProperty =
                DependencyProperty.Register(
                        "IsTextSearchCaseSensitive",
                        typeof(bool),
                        typeof(ItemsControl),
                        new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        ///     Whether TextSearch is case sensitive or not on this ItemsControl
        /// </summary>
        public bool IsTextSearchCaseSensitive
        {
            get { return (bool) GetValue(IsTextSearchCaseSensitiveProperty); }
            set { SetValue(IsTextSearchCaseSensitiveProperty, BooleanBoxes.Box(value)); }
        }

        #endregion

        #region Mapping methods

        ///<summary>
        /// Return the ItemsControl that owns the given container element
        ///</summary>
        public static ItemsControl ItemsControlFromItemContainer(DependencyObject container)
        {
            UIElement ui = container as UIElement;
            if (ui == null)
                return null;

            // ui appeared in items collection
            ItemsControl ic = LogicalTreeHelper.GetParent(ui) as ItemsControl;
            if (ic != null)
            {
                // this is the right ItemsControl as long as the item
                // is (or is eligible to be) its own container
                IGeneratorHost host = ic as IGeneratorHost;
                if (host.IsItemItsOwnContainer(ui))
                    return ic;
                else
                    return null;
            }

            ui = VisualTreeHelper.GetParent(ui) as UIElement;

            return ItemsControl.GetItemsOwner(ui);
        }

        ///<summary>
        /// Return the container that owns the given element.  If itemsControl
        /// is not null, return a container that belongs to the given ItemsControl.
        /// If itemsControl is null, return the closest container belonging to
        /// any ItemsControl.  Return null if no such container exists.
        ///</summary>
        public static DependencyObject ContainerFromElement(ItemsControl itemsControl, DependencyObject element)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            // if the element is itself the desired container, return it
            if (IsContainerForItemsControl(element, itemsControl))
            {
                return element;
            }

            // start the tree walk at the element's parent
            FrameworkObject fo = new FrameworkObject(element);
            fo.Reset(fo.GetPreferVisualParent(true).DO);

            // walk up, stopping when we reach the desired container
            while (fo.DO != null)
            {
                if (IsContainerForItemsControl(fo.DO, itemsControl))
                {
                    break;
                }

                fo.Reset(fo.PreferVisualParent.DO);
            }

            return fo.DO;
        }

        ///<summary>
        /// Return the container belonging to the current ItemsControl that owns
        /// the given container element.  Return null if no such container exists.
        ///</summary>
        public DependencyObject ContainerFromElement(DependencyObject element)
        {
            return ContainerFromElement(this, element);
        }

        // helper method used by ContainerFromElement
        private static bool IsContainerForItemsControl(DependencyObject element, ItemsControl itemsControl)
        {
            // is the element a container?
            if (element.ContainsValue(ItemContainerGenerator.ItemForItemContainerProperty))
            {
                // does the element belong to the itemsControl?
                if (itemsControl == null || itemsControl == ItemsControlFromItemContainer(element))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion Mapping methods

        #region IAddChild

        ///<summary>
        /// Called to Add the object as a Child.
        ///</summary>
        ///<param name="value">
        /// Object to add as a child
        ///</param>
        void IAddChild.AddChild(Object value)
        {
            AddChild(value);
        }

        /// <summary>
        ///  Add an object child to this control
        /// </summary>
        protected virtual void AddChild(object value)
        {
            Items.Add(value);
        }

        ///<summary>
        /// Called when text appears under the tag in markup
        ///</summary>
        ///<param name="text">
        /// Text to Add to the Object
        ///</param>
        void IAddChild.AddText(string text)
        {
            AddText(text);
        }

        /// <summary>
        ///  Add a text string to this control
        /// </summary>
        protected virtual void AddText(string text)
        {
            Items.Add(text);
        }

        #endregion

        #region IGeneratorHost

        //------------------------------------------------------
        //
        //  Interface - IGeneratorHost
        //
        //------------------------------------------------------

        /// <summary>
        /// The view of the data
        /// </summary>
        ItemCollection IGeneratorHost.View
        {
            get { return Items; }
        }

        /// <summary>
        /// Return true if the item is (or is eligible to be) its own ItemContainer
        /// </summary>
        bool IGeneratorHost.IsItemItsOwnContainer(object item)
        {
            return IsItemItsOwnContainer(item);
        }

        /// <summary>
        /// Return the element used to display the given item
        /// </summary>
        DependencyObject IGeneratorHost.GetContainerForItem(object item)
        {
            DependencyObject container;

            // use the item directly, if possible (bug 870672)
            if (IsItemItsOwnContainerOverride(item))
                container = item as DependencyObject;
            else
                container = GetContainerForItemOverride();

            // the container might have a parent from a previous
            // generation (bug 873118).  If so, clean it up before using it again.
            //
            // Note: This assumes the container is about to be added to a new parent,
            // according to the ItemsControl/Generator/Container pattern.
            // If someone calls the generator and doesn't add the container to
            // a visual parent, unexpected things might happen.
            Visual visual = container as Visual;
            if (visual != null)
            {
                Visual parent = VisualTreeHelper.GetParent(visual) as Visual;
                if (parent != null)
                {
                    Invariant.Assert(parent is FrameworkElement, SR.Get(SRID.ItemsControl_ParentNotFrameworkElement));
                    Panel p = parent as Panel;
                    if (p != null && (visual is UIElement))
                    {
                        p.Children.RemoveNoVerify((UIElement)visual);
                    }
                    else
                    {
                        ((FrameworkElement)parent).TemplateChild = null;
                    }
                }
            }

            return container;
        }

        /// <summary>
        /// Prepare the element to act as the ItemContainer for the corresponding item.
        /// </summary>
        void IGeneratorHost.PrepareItemContainer(DependencyObject container, object item)
        {
            // GroupItems are special - their information comes from a different place
            GroupItem groupItem = container as GroupItem;
            if (groupItem != null)
            {
                groupItem.PrepareItemContainer(item, this);
                return;
            }

            if (ShouldApplyItemContainerStyle(container, item))
            {
                // apply the ItemContainer style (if any)
                ApplyItemContainerStyle(container, item);
            }

            // forward ItemTemplate, et al.
            PrepareContainerForItemOverride(container, item);

            // set up the binding group
            if (!Helper.HasUnmodifiedDefaultValue(this, ItemBindingGroupProperty) &&
                Helper.HasUnmodifiedDefaultOrInheritedValue(container, FrameworkElement.BindingGroupProperty))
            {
                BindingGroup itemBindingGroup = ItemBindingGroup;
                BindingGroup containerBindingGroup =
                    (itemBindingGroup != null)  ? new BindingGroup(itemBindingGroup)
                                                : null;
                container.SetValue(FrameworkElement.BindingGroupProperty, containerBindingGroup);
            }

            if (container == item && TraceData.IsEnabled)
            {
                // issue a message if there's an ItemTemplate(Selector) for "direct" items
                // The ItemTemplate isn't used, which may confuse the user (bug 991101).
                if (ItemTemplate != null || ItemTemplateSelector != null)
                {
                    TraceData.TraceAndNotify(TraceEventType.Error, TraceData.ItemTemplateForDirectItem, null,
                        traceParameters: new object[] { AvTrace.TypeName(item) });
                }
            }

            TreeViewItem treeViewItem = container as TreeViewItem;
            if (treeViewItem != null)
            {
                treeViewItem.PrepareItemContainer(item, this);
            }
        }

        /// <summary>
        /// Undo any initialization done on the element during GetContainerForItem and PrepareItemContainer
        /// </summary>
        void IGeneratorHost.ClearContainerForItem(DependencyObject container, object item)
        {
            // This method no longer does most of the work it used to (bug 1445288).
            // It is called when a container is removed from the tree;  such a
            // container will be GC'd soon, so there's no point in changing
            // its properties.
            //
            // We still call the override method, to give subclasses a chance
            // to clean up anything they may have done during Prepare (bug 1561206).

            GroupItem groupItem = container as GroupItem;
            if (groupItem == null)
            {
                ClearContainerForItemOverride(container, item);

                TreeViewItem treeViewItem = container as TreeViewItem;
                if (treeViewItem != null)
                {
                    treeViewItem.ClearItemContainer(item, this);
                }
            }
            else
            {
                // GroupItems are special - their information comes from a different place
                // Recursively clear the sub-generators, so that ClearContainerForItemOverride
                // is called on the bottom-level containers.
                groupItem.ClearItemContainer(item, this);
            }
        }



        /// <summary>
        /// Determine if the given element was generated for this host as an ItemContainer.
        /// </summary>
        bool IGeneratorHost.IsHostForItemContainer(DependencyObject container)
        {
            // If ItemsControlFromItemContainer can determine who owns the element,
            // use its decision.
            ItemsControl ic = ItemsControlFromItemContainer(container);
            if (ic != null)
                return (ic == this);

            // If the element is in my items view, and if it can be its own ItemContainer,
            // it's mine.  Contains may be expensive, so we avoid calling it in cases
            // where we already know the answer - namely when the element has a
            // logical parent (ItemsControlFromItemContainer handles this case).  This
            // leaves only those cases where the element belongs to my items
            // without having a logical parent (e.g. via ItemsSource) and without
            // having been generated yet. HasItem indicates if anything has been generated.
            DependencyObject parent = LogicalTreeHelper.GetParent(container);
            if (parent == null)
            {
                return IsItemItsOwnContainerOverride(container) &&
                    HasItems && Items.Contains(container);
            }

            // Otherwise it's not mine
            return false;
        }

        /// <summary>
        /// Return the GroupStyle (if any) to use for the given group at the given level.
        /// </summary>
        GroupStyle IGeneratorHost.GetGroupStyle(CollectionViewGroup group, int level)
        {
            GroupStyle result = null;

            // a. Use global selector
            if (GroupStyleSelector != null)
            {
                result = GroupStyleSelector(group, level);
            }

            // b. lookup in GroupStyle list
            if (result == null)
            {
                // use last entry for all higher levels
                if (level >= GroupStyle.Count)
                {
                    level = GroupStyle.Count - 1;
                }

                if (level >= 0)
                {
                    result = GroupStyle[level];
                }
            }

            return result;
        }

        /// <summary>
        /// Communicates to the host that the generator is using grouping.
        /// </summary>
        void IGeneratorHost.SetIsGrouping(bool isGrouping)
        {
            SetValue(IsGroupingPropertyKey, BooleanBoxes.Box(isGrouping));
        }

        /// <summary>
        /// The AlternationCount
        /// <summary>
        int IGeneratorHost.AlternationCount { get { return AlternationCount; } }

        #endregion IGeneratorHost

        #region ISupportInitialize
        /// <summary>
        ///     Initialization of this element is about to begin
        /// </summary>
        public override void BeginInit()
        {
            base.BeginInit();

            if (_items != null)
            {
                _items.BeginInit();
            }
        }

        /// <summary>
        ///     Initialization of this element has completed
        /// </summary>
        public override void EndInit()
        {
            if (IsInitPending)
            {
                if (_items != null)
                {
                    _items.EndInit();
                }

                base.EndInit();
            }
        }

        private bool IsInitPending
        {
            get
            {
                return ReadInternalFlag(InternalFlags.InitPending);
            }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Return true if the item is (or should be) its own item container
        /// </summary>
        public bool IsItemItsOwnContainer(object item)
        {
            return IsItemItsOwnContainerOverride(item);
        }

        /// <summary>
        /// Return true if the item is (or should be) its own item container
        /// </summary>
        protected virtual bool IsItemItsOwnContainerOverride(object item)
        {
            return (item is UIElement);
        }

        /// <summary> Create or identify the element used to display the given item. </summary>
        protected virtual DependencyObject GetContainerForItemOverride()
        {
            return new ContentPresenter();
        }

        /// <summary>
        /// Prepare the element to display the item.  This may involve
        /// applying styles, setting bindings, etc.
        /// </summary>
        protected virtual void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            // Each type of "ItemContainer" element may require its own initialization.
            // We use explicit polymorphism via internal methods for this.
            //
            // Another way would be to define an interface IGeneratedItemContainer with
            // corresponding virtual "core" methods.  Base classes (ContentControl,
            // ItemsControl, ContentPresenter) would implement the interface
            // and forward the work to subclasses via the "core" methods.
            //
            // While this is better from an OO point of view, and extends to
            // 3rd-party elements used as containers, it exposes more public API.
            // Management considers this undesirable, hence the following rather
            // inelegant code.

            HeaderedContentControl hcc;
            ContentControl cc;
            ContentPresenter cp;
            ItemsControl ic;
            HeaderedItemsControl hic;

            if ((hcc = element as HeaderedContentControl) != null)
            {
                hcc.PrepareHeaderedContentControl(item, ItemTemplate, ItemTemplateSelector, ItemStringFormat);
            }
            else if ((cc = element as ContentControl) != null)
            {
                cc.PrepareContentControl(item, ItemTemplate, ItemTemplateSelector, ItemStringFormat);
            }
            else if ((cp = element as ContentPresenter) != null)
            {
                cp.PrepareContentPresenter(item, ItemTemplate, ItemTemplateSelector, ItemStringFormat);
            }
            else if ((hic = element as HeaderedItemsControl) != null)
            {
                hic.PrepareHeaderedItemsControl(item, this);
            }
            else if ((ic = element as ItemsControl) != null)
            {
                if (ic != this)
                {
                    ic.PrepareItemsControl(item, this);
                }
            }
        }

        /// <summary>
        /// Undo the effects of PrepareContainerForItemOverride.
        /// </summary>
        protected virtual void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            HeaderedContentControl hcc;
            ContentControl cc;
            ContentPresenter cp;
            ItemsControl ic;
            HeaderedItemsControl hic;

            if ((hcc = element as HeaderedContentControl) != null)
            {
                hcc.ClearHeaderedContentControl(item);
            }
            else if ((cc = element as ContentControl) != null)
            {
                cc.ClearContentControl(item);
            }
            else if ((cp = element as ContentPresenter) != null)
            {
                cp.ClearContentPresenter(item);
            }
            else if ((hic = element as HeaderedItemsControl) != null)
            {
                hic.ClearHeaderedItemsControl(item);
            }
            else if ((ic = element as ItemsControl) != null)
            {
                if (ic != this)
                {
                    ic.ClearItemsControl(item);
                }
            }
        }

        /// <summary>
        ///     Called when a TextInput event is received.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnTextInput(TextCompositionEventArgs e)
        {
            base.OnTextInput(e);

            // Only handle text from ourselves or an item container
            if (!String.IsNullOrEmpty(e.Text) && IsTextSearchEnabled &&
                (e.OriginalSource == this || ItemsControlFromItemContainer(e.OriginalSource as DependencyObject) == this))
            {
                TextSearch instance = TextSearch.EnsureInstance(this);

                if (instance != null)
                {
                    instance.DoSearch(e.Text);
                    // Note: we always want to handle the event to denote that we
                    // actually did something.  We wouldn't want an AccessKey
                    // to get invoked just because there wasn't a match here.
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        ///     Called when a KeyDown event is received.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (IsTextSearchEnabled)
            {
                // If the pressed the backspace key, delete the last character
                // in the TextSearch current prefix.
                if (e.Key == Key.Back)
                {
                    TextSearch instance = TextSearch.EnsureInstance(this);

                    if (instance != null)
                    {
                        instance.DeleteLastCharacter();
                    }
                }
            }
        }

        internal override void OnTemplateChangedInternal(FrameworkTemplate oldTemplate, FrameworkTemplate newTemplate)
        {
            // Forget about the old ItemsHost we had when the style changes
            _itemsHost = null;
            _scrollHost = null;
            WriteControlFlag(ControlBoolFlags.ScrollHostValid, false);

            base.OnTemplateChangedInternal(oldTemplate, newTemplate);
        }

        /// <summary>
        /// Determine whether the ItemContainerStyle/StyleSelector should apply to the container
        /// </summary>
        /// <returns>true if the ItemContainerStyle should apply to the item</returns>
        protected virtual bool ShouldApplyItemContainerStyle(DependencyObject container, object item)
        {
            return true;
        }

        #endregion

        //------------------------------------------------------
        //
        //  Internal methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Prepare to display the item.
        /// </summary>
        internal void PrepareItemsControl(object item, ItemsControl parentItemsControl)
        {
            if (item != this)
            {
                // copy templates and styles from parent ItemsControl
                DataTemplate itemTemplate = parentItemsControl.ItemTemplate;
                DataTemplateSelector itemTemplateSelector = parentItemsControl.ItemTemplateSelector;
                string itemStringFormat = parentItemsControl.ItemStringFormat;
                Style itemContainerStyle = parentItemsControl.ItemContainerStyle;
                StyleSelector itemContainerStyleSelector = parentItemsControl.ItemContainerStyleSelector;
                int alternationCount = parentItemsControl.AlternationCount;
                BindingGroup itemBindingGroup = parentItemsControl.ItemBindingGroup;

                if (itemTemplate != null)
                {
                    SetValue(ItemTemplateProperty, itemTemplate);
                }
                if (itemTemplateSelector != null)
                {
                    SetValue(ItemTemplateSelectorProperty, itemTemplateSelector);
                }
                if (itemStringFormat != null &&
                    Helper.HasDefaultValue(this, ItemStringFormatProperty))
                {
                    SetValue(ItemStringFormatProperty, itemStringFormat);
                }
                if (itemContainerStyle != null &&
                    Helper.HasDefaultValue(this, ItemContainerStyleProperty))
                {
                    SetValue(ItemContainerStyleProperty, itemContainerStyle);
                }
                if (itemContainerStyleSelector != null &&
                    Helper.HasDefaultValue(this, ItemContainerStyleSelectorProperty))
                {
                    SetValue(ItemContainerStyleSelectorProperty, itemContainerStyleSelector);
                }
                if (alternationCount != 0 &&
                    Helper.HasDefaultValue(this, AlternationCountProperty))
                {
                    SetValue(AlternationCountProperty, alternationCount);
                }
                if (itemBindingGroup != null &&
                    Helper.HasDefaultValue(this, ItemBindingGroupProperty))
                {
                    SetValue(ItemBindingGroupProperty, itemBindingGroup);
                }
            }
        }

        /// <summary>
        /// Undo the effect of PrepareItemsControl.
        /// </summary>
        internal void ClearItemsControl(object item)
        {
            if (item != this)
            {
                // nothing to do
            }
        }

        /// <summary>
        /// Bringing the item passed as arg into view. If item is virtualized it will become realized.
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        internal object OnBringItemIntoView(object arg)
        {
            ItemInfo info = arg as ItemInfo;
            if (info == null)
            {
                info = NewItemInfo(arg);
            }

            return OnBringItemIntoView(info);
        }

        internal object OnBringItemIntoView(ItemInfo info)
        {
            FrameworkElement element = info.Container as FrameworkElement;
            if (element != null)
            {
                element.BringIntoView();
            }
            else if ((info = LeaseItemInfo(info, true)).Index >= 0)
            {
                // We might be virtualized, try to de-virtualize the item.
                // Note: There is opportunity here to make a public OM.
                //
                // Call UpdateLayout first, in case there is a pending Measure
                // that replaces the ItemsHost with a different panel.   We should
                // forward the request to the correct panel, of course.
                if (!FrameworkCompatibilityPreferences.GetVSP45Compat())
                {
                    UpdateLayout();
                }

                VirtualizingPanel itemsHost = ItemsHost as VirtualizingPanel;
                if (itemsHost != null)
                {
                    itemsHost.BringIndexIntoView(info.Index);
                }
            }

            return null;
        }



        internal Panel ItemsHost
        {
            get
            {
                return _itemsHost;
            }
            set { _itemsHost = value; }
        }


        #region Keyboard Navigation

        internal bool NavigateByLine(FocusNavigationDirection direction, ItemNavigateArgs itemNavigateArgs)
        {
            DependencyObject startingElement = Keyboard.FocusedElement as DependencyObject;
            if (!FrameworkAppContextSwitches.KeyboardNavigationFromHyperlinkInItemsControlIsNotRelativeToFocusedElement)
            {
                while (startingElement != null && !(startingElement is FrameworkElement))
                {
                    // if focus is on a non-FE (e.g. Hyperlink), start the navigation
                    // from its nearest FE ancestor
                    startingElement = KeyboardNavigation.GetParent(startingElement) as DependencyObject;
                }
            }
            return NavigateByLine(FocusedInfo, startingElement as FrameworkElement, direction, itemNavigateArgs);
        }

        internal void PrepareNavigateByLine(ItemInfo startingInfo,
            FrameworkElement startingElement,
            FocusNavigationDirection direction,
            ItemNavigateArgs itemNavigateArgs,
            out FrameworkElement container)
        {
            container = null;
            if (ItemsHost == null)
            {
                return;
            }

            // If the focused container/item has been scrolled out of view and they want to
            // start navigating again, scroll it back into view.
            if (startingElement != null)
            {
                MakeVisible(startingElement, direction, false);
            }
            else
            {
                MakeVisible(startingInfo, direction, out startingElement);
            }

            object startingItem = (startingInfo != null) ? startingInfo.Item : null;

            // When we get here if startingItem is non-null, it must be on the visible page.
            NavigateByLineInternal(startingItem,
                direction,
                startingElement,
                itemNavigateArgs,
                false /*shouldFocus*/,
                out container);
        }

        internal bool NavigateByLine(ItemInfo startingInfo,
            FocusNavigationDirection direction,
            ItemNavigateArgs itemNavigateArgs)
        {
            return NavigateByLine(startingInfo, null, direction, itemNavigateArgs);
        }

        internal bool NavigateByLine(ItemInfo startingInfo,
            FrameworkElement startingElement,
            FocusNavigationDirection direction,
            ItemNavigateArgs itemNavigateArgs)
        {
            if (ItemsHost == null)
            {
                return false;
            }

            // If the focused container/item has been scrolled out of view and they want to
            // start navigating again, scroll it back into view.
            if (startingElement != null)
            {
                MakeVisible(startingElement, direction, false);
            }
            else
            {
                MakeVisible(startingInfo, direction, out startingElement);
            }

            object startingItem = (startingInfo != null) ? startingInfo.Item : null;

            // When we get here if startingItem is non-null, it must be on the visible page.
            FrameworkElement container;
            return NavigateByLineInternal(startingItem,
                direction,
                startingElement,
                itemNavigateArgs,
                true /*shouldFocus*/,
                out container);
        }

        private bool NavigateByLineInternal(object startingItem,
            FocusNavigationDirection direction,
            FrameworkElement startingElement,
            ItemNavigateArgs itemNavigateArgs,
            bool shouldFocus,
            out FrameworkElement container)
        {
            container = null;

            //
            // If there is no starting item, just navigate to the first item.
            //
            if (startingItem == null &&
                (startingElement == null || startingElement == this))
            {
                return NavigateToStartInternal(itemNavigateArgs, shouldFocus, out container);
            }
            else
            {
                FrameworkElement nextElement = null;

                //
                // If the container isn't there, it might have been degenerated or
                // it might have been scrolled out of view.  Either way, we
                // should start navigation from the ItemsHost b/c we know it
                // is visible.
                // The generator could have given us an element which isn't
                // actually visually connected.  In this case we should use
                // the ItemsHost as well.
                //
                if (startingElement == null || !ItemsHost.IsAncestorOf(startingElement))
                {
                    //
                    // Bug 991220 makes it so that we have to start from the ScrollHost.
                    // If we try to start from the ItemsHost it will always skip the first item.
                    //
                    startingElement = ScrollHost;
                }
                else
                {
                    // if the starting element is with in an element with contained or cycle scope
                    // then let the default keyboard navigation logic kick in.
                    DependencyObject startingParent = VisualTreeHelper.GetParent(startingElement);
                    while (startingParent != null &&
                        startingParent != ItemsHost)
                    {
                        KeyboardNavigationMode mode = KeyboardNavigation.GetDirectionalNavigation(startingParent);
                        if (mode == KeyboardNavigationMode.Contained ||
                            mode == KeyboardNavigationMode.Cycle)
                        {
                            return false;
                        }
                        startingParent = VisualTreeHelper.GetParent(startingParent);
                    }
                }

                bool isHorizontal = (ItemsHost != null && ItemsHost.HasLogicalOrientation && ItemsHost.LogicalOrientation == Orientation.Horizontal);
                bool treeViewNavigation = (this is TreeView);
                nextElement = KeyboardNavigation.Current.PredictFocusedElement(startingElement,
                    direction,
                    treeViewNavigation) as FrameworkElement;

                if (ScrollHost != null)
                {
                    bool didScroll = false;
                    FrameworkElement viewport = GetViewportElement();
                    VirtualizingPanel virtualizingPanel = ItemsHost as VirtualizingPanel;
                    bool isCycle = KeyboardNavigation.GetDirectionalNavigation(this) == KeyboardNavigationMode.Cycle;

                    while (true)
                    {
                        if (nextElement != null)
                        {
                            if (virtualizingPanel != null &&
                                ScrollHost.CanContentScroll &&
                                VirtualizingPanel.GetIsVirtualizing(this))
                            {
                                Rect currentRect;
                                ElementViewportPosition elementPosition = GetElementViewportPosition(viewport,
                                    TryGetTreeViewItemHeader(nextElement) as FrameworkElement,
                                    direction,
                                    false /*fullyVisible*/,
                                    out currentRect);
                                if (elementPosition == ElementViewportPosition.CompletelyInViewport ||
                                    elementPosition == ElementViewportPosition.PartiallyInViewport)
                                {
                                    if (!isCycle)
                                    {
                                        break;
                                    }
                                    else
                                    {
                                        Rect startingRect;
                                        GetElementViewportPosition(viewport,
                                            startingElement,
                                            direction,
                                            false /*fullyVisible*/,
                                            out startingRect);
                                        bool isInDirection = IsInDirectionForLineNavigation(startingRect, currentRect, direction, isHorizontal);
                                        if (isInDirection)
                                        {
                                            // If the next element in cycle mode is in direction
                                            // then this is a valid candidate, If not then try
                                            // scrolling.
                                            break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                break;
                            }

                            //
                            // We are disregarding the previously predicted element because
                            // it is outside the viewport extents of a VirtualizingPanel
                            //
                            nextElement = null;
                        }

                        double oldHorizontalOffset = ScrollHost.HorizontalOffset;
                        double oldVerticalOffset = ScrollHost.VerticalOffset;

                        switch (direction)
                        {
                            case FocusNavigationDirection.Down:
                                {
                                    didScroll = true;
                                    if (isHorizontal)
                                    {
                                        ScrollHost.LineRight();
                                    }
                                    else
                                    {
                                        ScrollHost.LineDown();
                                    }
                                }
                                break;
                            case FocusNavigationDirection.Up:
                                {
                                    didScroll = true;
                                    if (isHorizontal)
                                    {
                                        ScrollHost.LineLeft();
                                    }
                                    else
                                    {
                                        ScrollHost.LineUp();
                                    }
                                }
                                break;
                        }

                        ScrollHost.UpdateLayout();

                        // If offset does not change, or if offset goes out of range - exit the loop.
                        // The out-of-range check is to defend against buggy implementations of
                        // IScrollInfo;  WPF's implementations always leave the
                        // offset within range.
                        if ((DoubleUtil.AreClose(oldHorizontalOffset, ScrollHost.HorizontalOffset) &&
                            DoubleUtil.AreClose(oldVerticalOffset, ScrollHost.VerticalOffset))
                            || (direction == FocusNavigationDirection.Down &&
                                    (ScrollHost.VerticalOffset > ScrollHost.ExtentHeight ||
                                     ScrollHost.HorizontalOffset > ScrollHost.ExtentWidth))
                            || (direction == FocusNavigationDirection.Up &&
                                    (ScrollHost.VerticalOffset < 0.0 ||
                                     ScrollHost.HorizontalOffset < 0.0)))
                        {
                            if (isCycle)
                            {
                                if (direction == FocusNavigationDirection.Up)
                                {
                                    // If scrollviewer cannot be scrolled any further,
                                    // then cycle and navigate to end.
                                    return NavigateToEndInternal(itemNavigateArgs, true, out container);
                                }
                                else if (direction == FocusNavigationDirection.Down)
                                {
                                    // If scrollviewer cannot be scrolled any further,
                                    // then cycle and navigate to start.
                                    return NavigateToStartInternal(itemNavigateArgs, true, out container);
                                }
                            }
                            break;
                        }

                        nextElement = KeyboardNavigation.Current.PredictFocusedElement(startingElement,
                            direction,
                            treeViewNavigation) as FrameworkElement;
                    }

                    if (didScroll && nextElement != null && ItemsHost.IsAncestorOf(nextElement))
                    {
                        // Adjust offset so that the nextElement is aligned to the edge
                        AdjustOffsetToAlignWithEdge(nextElement, direction);
                    }
                }

                // We can only navigate there if the target element is in the items host.
                if ((nextElement != null) && (ItemsHost.IsAncestorOf(nextElement)))
                {
                    ItemsControl itemsControl = null;
                    object nextItem = GetEncapsulatingItem(nextElement, out container, out itemsControl);
                    container = nextElement;

                    if (shouldFocus)
                    {
                        if (nextItem == DependencyProperty.UnsetValue || nextItem is CollectionViewGroupInternal)
                        {
                            return nextElement.Focus();
                        }
                        else if (itemsControl != null)
                        {
                            return itemsControl.FocusItem(NewItemInfo(nextItem, container), itemNavigateArgs);
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return false;
        }

        internal void PrepareToNavigateByPage(ItemInfo startingInfo,
            FrameworkElement startingElement,
            FocusNavigationDirection direction,
            ItemNavigateArgs itemNavigateArgs,
            out FrameworkElement container)
        {
            container = null;

            if (ItemsHost == null)
            {
                return;
            }

            // If the focused container/item has been scrolled out of view and they want to
            // start navigating again, scroll it back into view.
            if (startingElement != null)
            {
                MakeVisible(startingElement, direction, /*alwaysAtTopOfViewport*/ false);
            }
            else
            {
                MakeVisible(startingInfo, direction, out startingElement);
            }

            object startingItem = (startingInfo != null) ? startingInfo.Item : null;

            // When we get here if startingItem is non-null, it must be on the visible page.
            NavigateByPageInternal(startingItem,
                direction,
                startingElement,
                itemNavigateArgs,
                false /*shouldFocus*/,
                out container);
        }

        internal bool NavigateByPage(FocusNavigationDirection direction, ItemNavigateArgs itemNavigateArgs)
        {
            return NavigateByPage(FocusedInfo, Keyboard.FocusedElement as FrameworkElement, direction, itemNavigateArgs);
        }

        internal bool NavigateByPage(
            ItemInfo startingInfo,
            FocusNavigationDirection direction,
            ItemNavigateArgs itemNavigateArgs)
        {
            return NavigateByPage(startingInfo, null, direction, itemNavigateArgs);
        }

        internal bool NavigateByPage(
            ItemInfo startingInfo,
            FrameworkElement startingElement,
            FocusNavigationDirection direction,
            ItemNavigateArgs itemNavigateArgs)
        {
            if (ItemsHost == null)
            {
                return false;
            }

            // If the focused container/item has been scrolled out of view and they want to
            // start navigating again, scroll it back into view.
            if (startingElement != null)
            {
                MakeVisible(startingElement, direction, /*alwaysAtTopOfViewport*/ false);
            }
            else
            {
                MakeVisible(startingInfo, direction, out startingElement);
            }

            object startingItem = (startingInfo != null) ? startingInfo.Item : null;

            // When we get here if startingItem is non-null, it must be on the visible page.
            FrameworkElement container;
            return NavigateByPageInternal(startingItem,
                direction,
                startingElement,
                itemNavigateArgs,
                true /*shouldFocus*/,
                out container);
        }

        private bool NavigateByPageInternal(object startingItem,
            FocusNavigationDirection direction,
            FrameworkElement startingElement,
            ItemNavigateArgs itemNavigateArgs,
            bool shouldFocus,
            out FrameworkElement container)
        {
            container = null;

            //
            // Move to the last guy on the page if we're not already there.
            //
            if (startingItem == null &&
                (startingElement == null || startingElement == this))
            {
                return NavigateToFirstItemOnCurrentPage(startingItem, direction, itemNavigateArgs, shouldFocus, out container);
            }
            else
            {
                //
                // See if the currently focused guy is the first or last one one the page
                //
                FrameworkElement firstElement;
                object firstItem = GetFirstItemOnCurrentPage(startingElement, direction, out firstElement);

                if ((object.Equals(startingItem, firstItem) ||
                    object.Equals(startingElement, firstElement)) &&
                    ScrollHost != null)
                {
                    bool isHorizontal = (ItemsHost.HasLogicalOrientation && ItemsHost.LogicalOrientation == Orientation.Horizontal);

                    do
                    {
                        double oldHorizontalOffset = ScrollHost.HorizontalOffset;
                        double oldVerticalOffset = ScrollHost.VerticalOffset;

                        switch (direction)
                        {
                            case FocusNavigationDirection.Up:
                                {
                                    if (isHorizontal)
                                    {
                                        ScrollHost.PageLeft();
                                    }
                                    else
                                    {
                                        ScrollHost.PageUp();
                                    }
                                }
                                break;

                            case FocusNavigationDirection.Down:
                                {
                                    if (isHorizontal)
                                    {
                                        ScrollHost.PageRight();
                                    }
                                    else
                                    {
                                        ScrollHost.PageDown();
                                    }
                                }
                                break;
                        }

                        ScrollHost.UpdateLayout();

                        // If offset does not change - exit the loop
                        if (DoubleUtil.AreClose(oldHorizontalOffset, ScrollHost.HorizontalOffset) &&
                            DoubleUtil.AreClose(oldVerticalOffset, ScrollHost.VerticalOffset))
                            break;

                        firstItem = GetFirstItemOnCurrentPage(startingElement, direction, out firstElement);
                    }
                    while (firstItem == DependencyProperty.UnsetValue);
                }

                container = firstElement;
                if (shouldFocus)
                {
                    if (firstElement != null &&
                        (firstItem == DependencyProperty.UnsetValue || firstItem is CollectionViewGroupInternal))
                    {
                        return firstElement.Focus();
                    }
                    else
                    {
                        ItemsControl itemsControl = GetEncapsulatingItemsControl(firstElement);
                        if (itemsControl != null)
                        {
                            return itemsControl.FocusItem(NewItemInfo(firstItem, firstElement), itemNavigateArgs);
                        }
                    }
                }
            }
            return false;
        }

        internal void NavigateToStart(ItemNavigateArgs itemNavigateArgs)
        {
            FrameworkElement container;
            NavigateToStartInternal(itemNavigateArgs, true /*shouldFocus*/, out container);
        }

        internal bool NavigateToStartInternal(ItemNavigateArgs itemNavigateArgs, bool shouldFocus, out FrameworkElement container)
        {
            container = null;

            if (ItemsHost != null)
            {
                if (ScrollHost != null)
                {
                    double oldHorizontalOffset = 0.0;
                    double oldVerticalOffset = 0.0;
                    bool isHorizontal = (ItemsHost.HasLogicalOrientation && ItemsHost.LogicalOrientation == Orientation.Horizontal);

                    do
                    {
                        oldHorizontalOffset = ScrollHost.HorizontalOffset;
                        oldVerticalOffset = ScrollHost.VerticalOffset;

                        if (isHorizontal)
                        {
                            ScrollHost.ScrollToLeftEnd();
                        }
                        else
                        {
                            ScrollHost.ScrollToTop();
                        }

                        // Wait for layout
                        ItemsHost.UpdateLayout();
                    }
                    // If offset does not change - exit the loop
                    while (!DoubleUtil.AreClose(oldHorizontalOffset, ScrollHost.HorizontalOffset) ||
                           !DoubleUtil.AreClose(oldVerticalOffset, ScrollHost.VerticalOffset));
                }

                FrameworkElement firstElement;
                FrameworkElement hopefulFirstElement = FindEndFocusableLeafContainer(ItemsHost, false /*last*/);
                object firstItem = GetFirstItemOnCurrentPage(hopefulFirstElement,
                    FocusNavigationDirection.Up,
                    out firstElement);
                container = firstElement;
                if (shouldFocus)
                {
                    if (firstElement != null &&
                        (firstItem == DependencyProperty.UnsetValue || firstItem is CollectionViewGroupInternal))
                    {
                         return firstElement.Focus();
                    }
                    else
                    {
                        ItemsControl itemsControl = GetEncapsulatingItemsControl(firstElement);
                        if (itemsControl != null)
                        {
                            return itemsControl.FocusItem(NewItemInfo(firstItem, firstElement), itemNavigateArgs);
                        }
                    }
                }
            }
            return false;
        }

        internal void NavigateToEnd(ItemNavigateArgs itemNavigateArgs)
        {
            FrameworkElement container;
            NavigateToEndInternal(itemNavigateArgs, true /*shouldFocus*/, out container);
        }

        internal bool NavigateToEndInternal(ItemNavigateArgs itemNavigateArgs, bool shouldFocus, out FrameworkElement container)
        {
            container = null;

            if (ItemsHost != null)
            {
                if (ScrollHost != null)
                {
                    double oldHorizontalOffset = 0.0;
                    double oldVerticalOffset = 0.0;
                    bool isHorizontal = (ItemsHost.HasLogicalOrientation && ItemsHost.LogicalOrientation == Orientation.Horizontal);

                    do
                    {
                        oldHorizontalOffset = ScrollHost.HorizontalOffset;
                        oldVerticalOffset = ScrollHost.VerticalOffset;

                        if (isHorizontal)
                        {
                            ScrollHost.ScrollToRightEnd();
                        }
                        else
                        {
                            ScrollHost.ScrollToBottom();
                        }

                        // Wait for layout
                        ItemsHost.UpdateLayout();
                    }
                    // If offset does not change - exit the loop
                    while (!DoubleUtil.AreClose(oldHorizontalOffset, ScrollHost.HorizontalOffset) ||
                           !DoubleUtil.AreClose(oldVerticalOffset, ScrollHost.VerticalOffset));
                }

                FrameworkElement lastElement;
                FrameworkElement hopefulLastElement = FindEndFocusableLeafContainer(ItemsHost, true /*last*/);
                object lastItem = GetFirstItemOnCurrentPage(hopefulLastElement,
                    FocusNavigationDirection.Down,
                    out lastElement);
                container = lastElement;
                if (shouldFocus)
                {
                    if (lastElement != null &&
                        (lastItem == DependencyProperty.UnsetValue || lastItem is CollectionViewGroupInternal))
                    {
                        return lastElement.Focus();
                    }
                    else
                    {
                        ItemsControl itemsControl = GetEncapsulatingItemsControl(lastElement);
                        if (itemsControl != null)
                        {
                            return itemsControl.FocusItem(NewItemInfo(lastItem, lastElement), itemNavigateArgs);
                        }
                    }
                }
            }
            return false;
        }

        private FrameworkElement FindEndFocusableLeafContainer(Panel itemsHost, bool last)
        {
            if (itemsHost == null)
            {
                return null;
            }
            UIElementCollection children = itemsHost.Children;
            if (children != null)
            {
                int count = children.Count;
                int i = (last ? count - 1 : 0);
                int incr = (last ? -1 : 1);
                while (i >= 0 && i < count)
                {
                    FrameworkElement fe = children[i] as FrameworkElement;
                    if (fe != null)
                    {
                        ItemsControl itemsControl = fe as ItemsControl;
                        FrameworkElement result = null;
                        if (itemsControl != null)
                        {
                            if (itemsControl.ItemsHost != null)
                            {
                                result = FindEndFocusableLeafContainer(itemsControl.ItemsHost, last);
                            }
                        }
                        else
                        {
                            GroupItem groupItem = fe as GroupItem;
                            if (groupItem != null && groupItem.ItemsHost != null)
                            {
                                result = FindEndFocusableLeafContainer(groupItem.ItemsHost, last);
                            }
                        }
                        if (result != null)
                        {
                            return result;
                        }
                        else if (KeyboardNavigation.IsFocusableInternal(fe))
                        {
                            return fe;
                        }
                    }
                    i += incr;
                }
            }
            return null;
        }

        internal void NavigateToItem(ItemInfo info, ItemNavigateArgs itemNavigateArgs, bool alwaysAtTopOfViewport=false)
        {
            if (info != null)
            {
                NavigateToItem(info.Item, info.Index, itemNavigateArgs, alwaysAtTopOfViewport);
            }
        }

        internal void NavigateToItem(object item, ItemNavigateArgs itemNavigateArgs)
        {
            NavigateToItem(item, -1, itemNavigateArgs, false /* alwaysAtTopOfViewport */);
        }

        internal void NavigateToItem(object item, int itemIndex, ItemNavigateArgs itemNavigateArgs)
        {
            NavigateToItem(item, itemIndex, itemNavigateArgs, false /* alwaysAtTopOfViewport */);
        }

        internal void NavigateToItem(object item, ItemNavigateArgs itemNavigateArgs, bool alwaysAtTopOfViewport)
        {
            NavigateToItem(item, -1, itemNavigateArgs, alwaysAtTopOfViewport);
        }

        private void NavigateToItem(object item, int elementIndex, ItemNavigateArgs itemNavigateArgs, bool alwaysAtTopOfViewport)
        {
            // need to deal with more than 1-D no-wrapping virtualization

            // Perhaps the container isn't generated yet.  In this case we try to shift the view,
            // wait for measure, and then call it again.
            if (item == DependencyProperty.UnsetValue)
            {
                return;
            }

            if (elementIndex == -1)
            {
                elementIndex = Items.IndexOf(item);
                if (elementIndex == -1)
                    return;
            }

            bool isHorizontal = false;
            if (ItemsHost != null)
            {
                isHorizontal = (ItemsHost.HasLogicalOrientation && ItemsHost.LogicalOrientation == Orientation.Horizontal);
            }

            FrameworkElement container;
            FocusNavigationDirection direction = isHorizontal ? FocusNavigationDirection.Right : FocusNavigationDirection.Down;
            MakeVisible(elementIndex, direction, alwaysAtTopOfViewport, out container);

            FocusItem(NewItemInfo(item, container), itemNavigateArgs);
        }

        private object FindFocusable(int startIndex, int direction, out int foundIndex, out FrameworkElement foundContainer)
        {
            // HasItems may be wrong when underlying collection does not notify, but this function
            // only cares about what's been generated and is consistent with ItemsControl state.
            if (HasItems)
            {
                int count = Items.Count;
                for (; startIndex >= 0 && startIndex < count; startIndex += direction)
                {
                    FrameworkElement container = ItemContainerGenerator.ContainerFromIndex(startIndex) as FrameworkElement;

                    // If the UI is non-null it must meet some minimum requirements to consider it for
                    // navigation (focusable, enabled).  If it has no UI we can make no judgements about it
                    // at this time, so it is navigable.
                    if (container == null || Keyboard.IsFocusable(container))
                    {
                        foundIndex = startIndex;
                        foundContainer = container;
                        return Items[startIndex];
                    }
                }
            }

            foundIndex = -1;
            foundContainer = null;
            return null;
        }

        private void AdjustOffsetToAlignWithEdge(FrameworkElement element, FocusNavigationDirection direction)
        {
            Debug.Assert(ScrollHost != null, "This operation to adjust the offset along an edge is only possible when there is a ScrollHost available");

            if (VirtualizingPanel.GetScrollUnit(this) != ScrollUnit.Item)
            {
                ScrollViewer scrollHost = ScrollHost;
                FrameworkElement viewportElement = GetViewportElement();
                element = TryGetTreeViewItemHeader(element) as FrameworkElement;
                Rect elementBounds = new Rect(new Point(), element.RenderSize);
                elementBounds = element.TransformToAncestor(viewportElement).TransformBounds(elementBounds);
                bool isHorizontal = (ItemsHost.HasLogicalOrientation && ItemsHost.LogicalOrientation == Orientation.Horizontal);

                if (direction == FocusNavigationDirection.Down)
                {
                    // Align with the bottom edge of viewport
                    if (isHorizontal)
                    {
                        scrollHost.ScrollToHorizontalOffset(scrollHost.HorizontalOffset - scrollHost.ViewportWidth + elementBounds.Right);
                    }
                    else
                    {
                        scrollHost.ScrollToVerticalOffset(scrollHost.VerticalOffset - scrollHost.ViewportHeight + elementBounds.Bottom);
                    }
                }
                else if (direction == FocusNavigationDirection.Up)
                {
                    // Align with the top edge of viewport
                    if (isHorizontal)
                    {
                        scrollHost.ScrollToHorizontalOffset(scrollHost.HorizontalOffset + elementBounds.Left);
                    }
                    else
                    {
                        scrollHost.ScrollToVerticalOffset(scrollHost.VerticalOffset + elementBounds.Top);
                    }
                }
            }
        }

        //
        // Shifts the viewport to make the given index visible.
        //
        private void MakeVisible(int index, FocusNavigationDirection direction, bool alwaysAtTopOfViewport, out FrameworkElement container)
        {
            container = null;

            if (index >= 0)
            {
                container = ItemContainerGenerator.ContainerFromIndex(index) as FrameworkElement;
                if (container == null)
                {
                    // In case of VirtualizingPanel, the container might not have been
                    // generated yet. Hence try generating it.
                    VirtualizingPanel virtualizingPanel = ItemsHost as VirtualizingPanel;
                    if (virtualizingPanel != null)
                    {
                        virtualizingPanel.BringIndexIntoView(index);
                        UpdateLayout();
                        container = ItemContainerGenerator.ContainerFromIndex(index) as FrameworkElement;
                    }
                }
                MakeVisible(container, direction, alwaysAtTopOfViewport);
            }
        }

        //
        // Shifts the viewport to make the given item visible.
        //
        private void MakeVisible(ItemInfo info, FocusNavigationDirection direction, out FrameworkElement container)
        {
            if (info != null)
            {
                MakeVisible(info.Index, direction, false /*alwaysAtTopOfViewport*/, out container);
                info.Container = container;
            }
            else
            {
                MakeVisible(-1, direction, false /*alwaysAtTopOfViewport*/, out container);
            }
        }

        //
        // Shifts the viewport to make the given index visible.
        //
        internal void MakeVisible(FrameworkElement container, FocusNavigationDirection direction, bool alwaysAtTopOfViewport)
        {
            if (ScrollHost != null && ItemsHost != null)
            {
                double oldHorizontalOffset;
                double oldVerticalOffset;

                FrameworkElement viewportElement = GetViewportElement();

                while (container != null && !IsOnCurrentPage(viewportElement, container, direction, false /*fullyVisible*/))
                {
                    oldHorizontalOffset = ScrollHost.HorizontalOffset;
                    oldVerticalOffset = ScrollHost.VerticalOffset;

                    container.BringIntoView();

                    // Wait for layout
                    ItemsHost.UpdateLayout();

                    // If offset does not change - exit the loop
                    if (DoubleUtil.AreClose(oldHorizontalOffset, ScrollHost.HorizontalOffset) &&
                        DoubleUtil.AreClose(oldVerticalOffset, ScrollHost.VerticalOffset))
                        break;
                }

                if (container != null && alwaysAtTopOfViewport)
                {
                    bool isHorizontal = (ItemsHost.HasLogicalOrientation && ItemsHost.LogicalOrientation == Orientation.Horizontal);

                    FrameworkElement firstElement;
                    GetFirstItemOnCurrentPage(container, FocusNavigationDirection.Up, out firstElement);
                    while (firstElement != container)
                    {
                        oldHorizontalOffset = ScrollHost.HorizontalOffset;
                        oldVerticalOffset = ScrollHost.VerticalOffset;

                        if (isHorizontal)
                        {
                            ScrollHost.LineRight();
                        }
                        else
                        {
                            ScrollHost.LineDown();
                        }

                        ScrollHost.UpdateLayout();

                        // If offset does not change - exit the loop
                        if (DoubleUtil.AreClose(oldHorizontalOffset, ScrollHost.HorizontalOffset) &&
                            DoubleUtil.AreClose(oldVerticalOffset, ScrollHost.VerticalOffset))
                            break;

                        GetFirstItemOnCurrentPage(container, FocusNavigationDirection.Up, out firstElement);
                    }
                }
            }
        }

        private bool NavigateToFirstItemOnCurrentPage(object startingItem, FocusNavigationDirection direction, ItemNavigateArgs itemNavigateArgs, bool shouldFocus, out FrameworkElement container)
        {
            object firstItem = GetFirstItemOnCurrentPage(ItemContainerGenerator.ContainerFromItem(startingItem) as FrameworkElement,
                direction,
                out container);

            if (firstItem != DependencyProperty.UnsetValue)
            {
                if (shouldFocus)
                {
                    return FocusItem(NewItemInfo(firstItem, container), itemNavigateArgs);
                }
            }
            return false;
        }

        private object GetFirstItemOnCurrentPage(FrameworkElement startingElement,
            FocusNavigationDirection direction,
            out FrameworkElement firstElement)
        {
            Debug.Assert(direction == FocusNavigationDirection.Up || direction == FocusNavigationDirection.Down, "Can only get the first item on a page using North or South");

            bool isHorizontal = (ItemsHost.HasLogicalOrientation && ItemsHost.LogicalOrientation == Orientation.Horizontal);
            bool isVertical = (ItemsHost.HasLogicalOrientation && ItemsHost.LogicalOrientation == Orientation.Vertical);

            if (ScrollHost != null &&
                ScrollHost.CanContentScroll &&
                VirtualizingPanel.GetScrollUnit(this) == ScrollUnit.Item &&
                !(this is TreeView) &&
                !IsGrouping)
            {
                int foundIndex = -1;
                if (isVertical)
                {
                    if (direction == FocusNavigationDirection.Up)
                    {
                        return FindFocusable((int)ScrollHost.VerticalOffset, 1, out foundIndex, out firstElement);
                    }
                    else
                    {
                        return FindFocusable((int)(ScrollHost.VerticalOffset + Math.Max(ScrollHost.ViewportHeight - 1, 0)),
                            -1,
                            out foundIndex,
                            out firstElement);
                    }
                }
                else if (isHorizontal)
                {
                    if (direction == FocusNavigationDirection.Up)
                    {
                        return FindFocusable((int)ScrollHost.HorizontalOffset, 1, out foundIndex, out firstElement);
                    }
                    else
                    {
                        return FindFocusable((int)(ScrollHost.HorizontalOffset + Math.Max(ScrollHost.ViewportWidth - 1, 0)),
                            -1,
                            out foundIndex,
                            out firstElement);
                    }
                }
            }

            //
            // We assume we're physically scrolling in both directions now.
            //
            if (startingElement != null)
            {
                FrameworkElement currentElement = startingElement;
                if (isHorizontal)
                {
                    // In horizontal orientation left/right directions must used to
                    // predict the focus.
                    if (direction == FocusNavigationDirection.Up)
                    {
                        direction = FocusNavigationDirection.Left;
                    }
                    else if (direction == FocusNavigationDirection.Down)
                    {
                        direction = FocusNavigationDirection.Right;
                    }
                }

                FrameworkElement viewportElement = GetViewportElement();
                bool treeViewNavigation = (this is TreeView);
                currentElement = KeyboardNavigation.Current.PredictFocusedElementAtViewportEdge(startingElement,
                    direction,
                    treeViewNavigation,
                    viewportElement,
                    viewportElement) as FrameworkElement;

                object returnItem = null;
                firstElement = null;

                if (currentElement != null)
                {
                    returnItem = GetEncapsulatingItem(currentElement, out firstElement);
                }

                if (currentElement == null || returnItem == DependencyProperty.UnsetValue)
                {
                    // Try the startingElement as a candidate.
                    ElementViewportPosition elementPosition = GetElementViewportPosition(viewportElement,
                        startingElement,
                        direction,
                        false /*fullyVisible*/);
                    if (elementPosition == ElementViewportPosition.CompletelyInViewport ||
                        elementPosition == ElementViewportPosition.PartiallyInViewport)
                    {
                        currentElement = startingElement;
                        returnItem = GetEncapsulatingItem(currentElement, out firstElement);
                    }
                }

                if (returnItem != null && returnItem is CollectionViewGroupInternal)
                {
                    firstElement = currentElement;
                }
                return returnItem;
            }

            firstElement = null;
            return null;
        }

        internal FrameworkElement GetViewportElement()
        {
            // NOTE: When ScrollHost is non-null, we use ScrollHost instead of
            //       ItemsHost because ItemsHost in the physically scrolling
            //       case will just have its layout offset shifted, and all
            //       items will always be within the bounding box of the ItemsHost,
            //       and we want to know if you can actually see the element.
            FrameworkElement viewPort = ScrollHost;
            if (viewPort == null)
            {
                viewPort = ItemsHost;
            }
            else
            {
                // Try use the ScrollContentPresenter as the viewport it is it available
                // because that is more representative of the viewport in case of
                // DataGrid when the ColumnHeaders need to be excluded from the
                // dimensions of the viewport.
                ScrollContentPresenter scp = viewPort.GetTemplateChild(ScrollViewer.ScrollContentPresenterTemplateName) as ScrollContentPresenter;
                if (scp != null)
                {
                    viewPort = scp;
                }
            }

            return viewPort;
        }

        /// <summary>
        /// Determines if the given item is on the current visible page.
        /// </summary>
        private bool IsOnCurrentPage(object item, FocusNavigationDirection axis)
        {
            FrameworkElement container = ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;

            if (container == null)
            {
                return false;
            }

            return (GetElementViewportPosition(GetViewportElement(), container, axis, false) == ElementViewportPosition.CompletelyInViewport);
        }

        private bool IsOnCurrentPage(FrameworkElement element, FocusNavigationDirection axis)
        {
            return (GetElementViewportPosition(GetViewportElement(), element, axis, false) == ElementViewportPosition.CompletelyInViewport);
        }

        /// <summary>
        /// Determines if the given element is on the current visible page.
        /// The element must be completely on the page on the given axis, but need
        /// not be completely contained on the page in the perpendicular axis.
        /// For example, if axis == North, then the element's Top and Bottom must
        /// be completely contained on the page.
        /// </summary>
        private bool IsOnCurrentPage(FrameworkElement viewPort, FrameworkElement element, FocusNavigationDirection axis, bool fullyVisible)
        {
            return (GetElementViewportPosition(viewPort, element, axis, fullyVisible) == ElementViewportPosition.CompletelyInViewport);
        }

        internal static ElementViewportPosition GetElementViewportPosition(FrameworkElement viewPort,
            UIElement element,
            FocusNavigationDirection axis,
            bool fullyVisible)
        {
            Rect elementRect;
            return GetElementViewportPosition(viewPort, element, axis, fullyVisible, out elementRect);
        }

        internal static ElementViewportPosition GetElementViewportPosition(FrameworkElement viewPort,
            UIElement element,
            FocusNavigationDirection axis,
            bool fullyVisible,
            out Rect elementRect)
        {
            return GetElementViewportPosition(
                viewPort,
                element,
                axis,
                fullyVisible,
                false,
                out elementRect);
        }

        /// <summary>
        /// Determines if the given element is
        ///     1) Completely in the current visible page along the given axis.
        ///     2) Partially in the current visible page.
        ///     3) Before the current page along the given axis.
        ///     4) After the current page along the given axis.
        /// fullyVisible parameter specifies if the element needs to be completely
        ///     in the current visible page along the perpendicular axis (if it is
        ///     completely in the page along the major axis).
        /// ignorePerpendicularAxis parameter specifies whether the position of
        ///     given element along the secondary axis doesn't matter
        /// </summary>
        internal static ElementViewportPosition GetElementViewportPosition(FrameworkElement viewPort,
            UIElement element,
            FocusNavigationDirection axis,
            bool fullyVisible,
            bool ignorePerpendicularAxis,
            out Rect elementRect)
        {
            elementRect = Rect.Empty;

            // If there's no ScrollHost or ItemsHost, the element is not on the page
            if (viewPort == null)
            {
                return ElementViewportPosition.None;
            }

            if (element == null || !viewPort.IsAncestorOf(element))
            {
                return ElementViewportPosition.None;
            }

            Rect viewPortBounds = new Rect(new Point(), viewPort.RenderSize);
            Rect elementBounds = new Rect(new Point(), element.RenderSize);
            elementBounds = CorrectCatastrophicCancellation(element.TransformToAncestor(viewPort)).TransformBounds(elementBounds);
            bool northSouth = (axis == FocusNavigationDirection.Up || axis == FocusNavigationDirection.Down);
            bool eastWest = (axis == FocusNavigationDirection.Left || axis == FocusNavigationDirection.Right);

            elementRect = elementBounds;

            if (ignorePerpendicularAxis)
            {
                // expand the viewport bounds to infinity along the secondary axis
                if (northSouth)
                {
                    viewPortBounds = new Rect(Double.NegativeInfinity, viewPortBounds.Top,
                                                Double.PositiveInfinity, viewPortBounds.Height);
                }
                else if (eastWest)
                {
                    viewPortBounds = new Rect(viewPortBounds.Left, Double.NegativeInfinity,
                                                viewPortBounds.Width, Double.PositiveInfinity);
                }
            }

            // Return true if the element is completely contained within the page along the given axis.

            if (fullyVisible)
            {
                if (viewPortBounds.Contains(elementBounds))
                {
                    return ElementViewportPosition.CompletelyInViewport;
                }
            }
            else
            {
                if (northSouth)
                {
                    if (DoubleUtil.LessThanOrClose(viewPortBounds.Top, elementBounds.Top)
                        && DoubleUtil.LessThanOrClose(elementBounds.Bottom, viewPortBounds.Bottom))
                    {
                        return ElementViewportPosition.CompletelyInViewport;
                    }
                }
                else if (eastWest)
                {
                    if (DoubleUtil.LessThanOrClose(viewPortBounds.Left, elementBounds.Left)
                        && DoubleUtil.LessThanOrClose(elementBounds.Right, viewPortBounds.Right))
                    {
                        return ElementViewportPosition.CompletelyInViewport;
                    }
                }
            }

            if (ElementIntersectsViewport(viewPortBounds, elementBounds))
            {
                return ElementViewportPosition.PartiallyInViewport;
            }
            else if ((northSouth && DoubleUtil.LessThanOrClose(elementBounds.Bottom, viewPortBounds.Top)) ||
                (eastWest && DoubleUtil.LessThanOrClose(elementBounds.Right, viewPortBounds.Left)))
            {
                return ElementViewportPosition.BeforeViewport;
            }
            else if ((northSouth && DoubleUtil.LessThanOrClose(viewPortBounds.Bottom, elementBounds.Top)) ||
                (eastWest && DoubleUtil.LessThanOrClose(viewPortBounds.Right, elementBounds.Left)))
            {
                return ElementViewportPosition.AfterViewport;
            }
            return ElementViewportPosition.None;
        }

        // in large virtualized hierarchical lists (TreeView or grouping), the transform
        // returned by element.TransformToAncestor(viewport) is vulnerable to catastrophic
        // cancellation.  If element is at the top of the viewport, but embedded in
        // layers of the hierarchy, the contributions of the intermediate elements add
        // up to a large positive number which should exactly cancel out the large
        // negative offset of the viewport's direct child to produce net offset of 0.0.
        // But floating-point drift while accumulating the intermediate offsets and
        // catastrophic cancellation in the last step may produce a very small
        // non-zero number instead (e.g. -0.0000000000006548). This can lead to
        // infinite loops and incorrect decisions in layout.
        // To mitigate this problem, replace near-zero offsets with zero.
        private static GeneralTransform CorrectCatastrophicCancellation(GeneralTransform transform)
        {
            MatrixTransform matrixTransform = transform as MatrixTransform;
            if (matrixTransform != null)
            {
                bool needNewTransform = false;
                Matrix matrix = matrixTransform.Matrix;

                if (matrix.OffsetX != 0.0 && LayoutDoubleUtil.AreClose(matrix.OffsetX, 0.0))
                {
                    matrix.OffsetX = 0.0;
                    needNewTransform = true;
                }

                if (matrix.OffsetY != 0.0 && LayoutDoubleUtil.AreClose(matrix.OffsetY, 0.0))
                {
                    matrix.OffsetY = 0.0;
                    needNewTransform = true;
                }

                if (needNewTransform)
                {
                    transform = new MatrixTransform(matrix);
                }
            }

            return transform;
        }

        private static bool ElementIntersectsViewport(Rect viewportRect, Rect elementRect)
        {
            if (viewportRect.IsEmpty || elementRect.IsEmpty)
            {
                return false;
            }

            if (DoubleUtil.LessThan(elementRect.Right, viewportRect.Left) || LayoutDoubleUtil.AreClose(elementRect.Right, viewportRect.Left) ||
                DoubleUtil.GreaterThan(elementRect.Left, viewportRect.Right) || LayoutDoubleUtil.AreClose(elementRect.Left, viewportRect.Right) ||
                DoubleUtil.LessThan(elementRect.Bottom, viewportRect.Top) || LayoutDoubleUtil.AreClose(elementRect.Bottom, viewportRect.Top) ||
                DoubleUtil.GreaterThan(elementRect.Top, viewportRect.Bottom) || LayoutDoubleUtil.AreClose(elementRect.Top, viewportRect.Bottom))
            {
                return false;
            }
            return true;
        }

        private bool IsInDirectionForLineNavigation(Rect fromRect, Rect toRect, FocusNavigationDirection direction, bool isHorizontal)
        {
            Debug.Assert(direction == FocusNavigationDirection.Up ||
                direction == FocusNavigationDirection.Down);

            if (direction == FocusNavigationDirection.Down)
            {
                if (isHorizontal)
                {
                    // Right
                    return DoubleUtil.GreaterThanOrClose(toRect.Left, fromRect.Left);
                }
                else
                {
                    // Down
                    return DoubleUtil.GreaterThanOrClose(toRect.Top, fromRect.Top);
                }
            }
            else if (direction == FocusNavigationDirection.Up)
            {
                if (isHorizontal)
                {
                    // Left
                    return DoubleUtil.LessThanOrClose(toRect.Right, fromRect.Right);
                }
                else
                {
                    // UP
                    return DoubleUtil.LessThanOrClose(toRect.Bottom, fromRect.Bottom);
                }
            }
            return false;
        }

        private static void OnGotFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ItemsControl itemsControl = (ItemsControl)sender;
            UIElement focusedElement = e.OriginalSource as UIElement;
            if ((focusedElement != null) && (focusedElement != itemsControl))
            {
                object item = itemsControl.ItemContainerGenerator.ItemFromContainer(focusedElement);
                if (item != DependencyProperty.UnsetValue)
                {
                    itemsControl._focusedInfo = itemsControl.NewItemInfo(item, focusedElement);
                }
                else if (itemsControl._focusedInfo != null)
                {
                    UIElement itemContainer = itemsControl._focusedInfo.Container as UIElement;
                    if (itemContainer == null ||
                        !Helper.IsAnyAncestorOf(itemContainer, focusedElement))
                    {
                        itemsControl._focusedInfo = null;
                    }
                }
            }
        }


        /// <summary>
        /// The item corresponding to the UI container which has focus.
        /// Virtualizing panels remove visual children you can't see.
        /// When you scroll the focused element out of view we throw
        /// focus back on to the items control and remember the item which
        /// was focused.  When it scrolls back into view (and focus is
        /// still on the ItemsControl) we'll focus it.
        /// </summary>
        internal ItemInfo FocusedInfo
        {
            get { return _focusedInfo; }
        }

        private ItemInfo _focusedInfo;

        internal class ItemNavigateArgs
        {
            public ItemNavigateArgs(InputDevice deviceUsed, ModifierKeys modifierKeys)
            {
                _deviceUsed = deviceUsed;
                _modifierKeys = modifierKeys;
            }

            public InputDevice DeviceUsed { get { return _deviceUsed; } }

            private InputDevice _deviceUsed;
            private ModifierKeys _modifierKeys;

            public static ItemNavigateArgs Empty
            {
                get
                {
                    if (_empty == null)
                    {
                        _empty = new ItemNavigateArgs(null, ModifierKeys.None);;
                    }
                    return _empty;
                }
            }
            private static ItemNavigateArgs _empty;
        }

        // make this protected
        internal virtual bool FocusItem(ItemInfo info, ItemNavigateArgs itemNavigateArgs)
        {
            object item = info.Item;
            bool returnValue = false;

            if (item != null)
            {
                UIElement container =  info.Container as UIElement;
                if (container != null)
                {
                    returnValue = container.Focus();
                }
            }
            if (itemNavigateArgs.DeviceUsed is KeyboardDevice)
            {
                KeyboardNavigation.ShowFocusVisual();
            }
            return returnValue;
        }

        // ISSUE: IsLogicalVertical and IsLogicalHorizontal are rough guesses as to whether
        //        the ItemsHost is virtualizing in a particular direction.  Ideally this
        //        would be exposed through the IScrollInfo.


        internal bool IsLogicalVertical
        {
            get
            {
                return (ItemsHost != null && ItemsHost.HasLogicalOrientation && ItemsHost.LogicalOrientation == Orientation.Vertical &&
                        ScrollHost != null && ScrollHost.CanContentScroll &&
                        VirtualizingStackPanel.GetScrollUnit(this) == ScrollUnit.Item);
            }
        }

        internal bool IsLogicalHorizontal
        {
            get
            {
                return (ItemsHost != null && ItemsHost.HasLogicalOrientation && ItemsHost.LogicalOrientation == Orientation.Horizontal &&
                        ScrollHost != null && ScrollHost.CanContentScroll &&
                        VirtualizingStackPanel.GetScrollUnit(this) == ScrollUnit.Item);
            }
        }

        internal ScrollViewer ScrollHost
        {
            get
            {
                if (!ReadControlFlag(ControlBoolFlags.ScrollHostValid))
                {
                    if (_itemsHost == null)
                    {
                        return null;
                    }
                    else
                    {
                        // We have an itemshost, so walk up the tree looking for the ScrollViewer
                        for (DependencyObject current = _itemsHost; current != this && current != null; current = VisualTreeHelper.GetParent(current))
                        {
                            ScrollViewer scrollViewer = current as ScrollViewer;
                            if (scrollViewer != null)
                            {
                                _scrollHost = scrollViewer;
                                break;
                            }
                        }

                        WriteControlFlag(ControlBoolFlags.ScrollHostValid, true);
                    }
                }

                return _scrollHost;
            }
        }

        internal static TimeSpan AutoScrollTimeout
        {
            get
            {
                // NOTE: NtUser does the following (file: windows/ntuser/kernel/sysmet.c)
                //     gpsi->dtLBSearch = dtTime * 4;            // dtLBSearch   =  4  * gdtDblClk
                //     gpsi->dtScroll = gpsi->dtLBSearch / 5;  // dtScroll     = 4/5 * gdtDblClk

                return TimeSpan.FromMilliseconds(MS.Win32.SafeNativeMethods.GetDoubleClickTime() * 0.8);
            }
        }

        internal void DoAutoScroll()
        {
            DoAutoScroll(FocusedInfo);
        }

        internal void DoAutoScroll(ItemInfo startingInfo)
        {
            // Attempt to compute positions based on the ScrollHost.
            // If that doesn't exist, use the ItemsHost.
            FrameworkElement relativeTo = ScrollHost != null ? (FrameworkElement)ScrollHost : ItemsHost;
            if (relativeTo != null)
            {
                // Figure out where the mouse is w.r.t. the ItemsControl.

                Point mousePosition = Mouse.GetPosition(relativeTo);

                // Take the bounding box of the ListBox and scroll against that
                Rect bounds = new Rect(new Point(), relativeTo.RenderSize);
                bool focusChanged = false;

                if (mousePosition.Y < bounds.Top)
                {
                    NavigateByLine(startingInfo, FocusNavigationDirection.Up, new ItemNavigateArgs(Mouse.PrimaryDevice, Keyboard.Modifiers));
                    focusChanged = startingInfo != FocusedInfo;
                }
                else if (mousePosition.Y >= bounds.Bottom)
                {
                    NavigateByLine(startingInfo, FocusNavigationDirection.Down, new ItemNavigateArgs(Mouse.PrimaryDevice, Keyboard.Modifiers));
                    focusChanged = startingInfo != FocusedInfo;
                }

                // Try horizontal scroll if vertical scroll did not happen
                if (!focusChanged)
                {
                    if (mousePosition.X < bounds.Left)
                    {
                        FocusNavigationDirection direction = FocusNavigationDirection.Left;
                        if (IsRTL(relativeTo))
                        {
                            direction = FocusNavigationDirection.Right;
                        }

                        NavigateByLine(startingInfo, direction, new ItemNavigateArgs(Mouse.PrimaryDevice, Keyboard.Modifiers));
                    }
                    else if (mousePosition.X >= bounds.Right)
                    {
                        FocusNavigationDirection direction = FocusNavigationDirection.Right;
                        if (IsRTL(relativeTo))
                        {
                            direction = FocusNavigationDirection.Left;
                        }

                        NavigateByLine(startingInfo, direction, new ItemNavigateArgs(Mouse.PrimaryDevice, Keyboard.Modifiers));
                    }
                }
            }
        }

        private bool IsRTL(FrameworkElement element)
        {
            FlowDirection flowDirection = element.FlowDirection;
            return (flowDirection == FlowDirection.RightToLeft);
        }

        private static ItemsControl GetEncapsulatingItemsControl(FrameworkElement element)
        {
            while (element != null)
            {
                ItemsControl itemsControl = ItemsControl.ItemsControlFromItemContainer(element);
                if (itemsControl != null)
                {
                    return itemsControl;
                }
                element = VisualTreeHelper.GetParent(element) as FrameworkElement;
            }
            return null;
        }

        private static object GetEncapsulatingItem(FrameworkElement element, out FrameworkElement container)
        {
            ItemsControl itemsControl = null;
            return GetEncapsulatingItem(element, out container, out itemsControl);
        }

        private static object GetEncapsulatingItem(FrameworkElement element, out FrameworkElement container, out ItemsControl itemsControl)
        {
            object item = DependencyProperty.UnsetValue;
            itemsControl = null;

            while (element != null)
            {
                itemsControl = ItemsControl.ItemsControlFromItemContainer(element);
                if (itemsControl != null)
                {
                    item = itemsControl.ItemContainerGenerator.ItemFromContainer(element);

                    if (item != DependencyProperty.UnsetValue)
                    {
                        break;
                    }
                }

                element = VisualTreeHelper.GetParent(element) as FrameworkElement;
            }

            container = element;
            return item;
        }

        internal static DependencyObject TryGetTreeViewItemHeader(DependencyObject element)
        {
            TreeViewItem treeViewItem = element as TreeViewItem;
            if (treeViewItem != null)
            {
                return treeViewItem.TryGetHeaderElement();
            }
            return element;
        }

        #endregion Keyboard Navigation

        #endregion

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private void ApplyItemContainerStyle(DependencyObject container, object item)
        {
            FrameworkObject foContainer = new FrameworkObject(container);

            // don't overwrite a locally-defined style (bug 1018408)
            if (!foContainer.IsStyleSetFromGenerator &&
                container.ReadLocalValue(FrameworkElement.StyleProperty) != DependencyProperty.UnsetValue)
            {
                return;
            }

            // Control's ItemContainerStyle has first stab
            Style style = ItemContainerStyle;

            // no ItemContainerStyle set, try ItemContainerStyleSelector
            if (style == null)
            {
                if (ItemContainerStyleSelector != null)
                {
                    style = ItemContainerStyleSelector.SelectStyle(item, container);
                }
            }

            // apply the style, if found
            if (style != null)
            {
                // verify style is appropriate before applying it
                if (!style.TargetType.IsInstanceOfType(container))
                    throw new InvalidOperationException(SR.Get(SRID.StyleForWrongType, style.TargetType.Name, container.GetType().Name));

                foContainer.Style = style;
                foContainer.IsStyleSetFromGenerator = true;
            }
            else if (foContainer.IsStyleSetFromGenerator)
            {
                // if Style was formerly set from ItemContainerStyle, clear it
                foContainer.IsStyleSetFromGenerator = false;
                container.ClearValue(FrameworkElement.StyleProperty);
            }
        }

        private void RemoveItemContainerStyle(DependencyObject container)
        {
            FrameworkObject foContainer = new FrameworkObject(container);

            if (foContainer.IsStyleSetFromGenerator)
            {
                container.ClearValue(FrameworkElement.StyleProperty);
            }
        }


        internal object GetItemOrContainerFromContainer(DependencyObject container)
        {
            object item = ItemContainerGenerator.ItemFromContainer(container);

            if (item == DependencyProperty.UnsetValue
                && ItemsControlFromItemContainer(container) == this
                && ((IGeneratorHost)this).IsItemItsOwnContainer(container))
            {
                item = container;
            }

            return item;
        }

        // A version of Object.Equals with paranoia for mismatched types, to avoid problems
        // with classes that implement Object.Equals poorly
        internal static bool EqualsEx(object o1, object o2)
        {
            try
            {
                return Object.Equals(o1, o2);
            }
            catch (System.InvalidCastException)
            {
                // A common programming error: the type of o1 overrides Equals(object o2)
                // but mistakenly assumes that o2 has the same type as o1:
                //     MyType x = (MyType)o2;
                // This throws InvalidCastException when o2 is a sentinel object,
                // e.g. UnsetValue, DisconnectedItem, NewItemPlaceholder, etc.
                // Rather than crash, just return false - the objects are clearly unequal.
                return false;
            }
        }

        #endregion

        #region ItemInfo

        // create an ItemInfo with as much information as can be deduced
        internal ItemInfo NewItemInfo(object item, DependencyObject container=null, int index=-1)
        {
            return new ItemInfo(item, container, index).Refresh(ItemContainerGenerator);
        }

        // create an ItemInfo for the given container
        internal ItemInfo ItemInfoFromContainer(DependencyObject container)
        {
            return NewItemInfo(ItemContainerGenerator.ItemFromContainer(container), container, ItemContainerGenerator.IndexFromContainer(container));
        }

        // create an ItemInfo for the given index
        internal ItemInfo ItemInfoFromIndex(int index)
        {
            return (index >= 0) ? NewItemInfo(Items[index], ItemContainerGenerator.ContainerFromIndex(index), index)
                                : null;
        }

        // create an unresolved ItemInfo
        internal ItemInfo NewUnresolvedItemInfo(object item)
        {
            return new ItemInfo(item, ItemInfo.UnresolvedContainer, -1);
        }

        // return the container corresponding to an ItemInfo
        internal DependencyObject ContainerFromItemInfo(ItemInfo info)
        {
            DependencyObject container = info.Container;
            if (container == null)
            {
                if (info.Index >= 0)
                {
                    container = ItemContainerGenerator.ContainerFromIndex(info.Index);
                    info.Container = container;
                }
                else
                {
                    container = ItemContainerGenerator.ContainerFromItem(info.Item);
                    // don't change info.Container - info is potentially shared by different ItemsControls
                }
            }

            return container;
        }

        // adjust ItemInfos after a generator status change
        internal void AdjustItemInfoAfterGeneratorChange(ItemInfo info)
        {
            if (info != null)
            {
                ItemInfo[] a = new ItemInfo[]{info};
                AdjustItemInfosAfterGeneratorChange(a, claimUniqueContainer:false);
            }
        }

        // adjust ItemInfos after a generator status change
        internal void AdjustItemInfosAfterGeneratorChange(IEnumerable<ItemInfo> list, bool claimUniqueContainer)
        {
            // detect discarded containers and mark the ItemInfo accordingly
            // (also see if there are infos awaiting containers)
            bool resolvePendingContainers = false;
            foreach (ItemInfo info in list)
            {
                DependencyObject container = info.Container;
                if (container == null)
                {
                    resolvePendingContainers = true;
                }
                else if (info.IsRemoved || !ItemsControl.EqualsEx(info.Item,
                            container.ReadLocalValue(ItemContainerGenerator.ItemForItemContainerProperty)))
                {
                    info.Container = null;
                    resolvePendingContainers = true;
                }
            }

            // if any of the ItemInfos correspond to containers
            // that are now realized, record the container in the ItemInfo
            if (resolvePendingContainers)
            {
                // first find containers that are already claimed by the list
                List<DependencyObject> claimedContainers = new List<DependencyObject>();
                if (claimUniqueContainer)
                {
                    foreach (ItemInfo info in list)
                    {
                        DependencyObject container = info.Container;
                        if (container != null)
                        {
                            claimedContainers.Add(container);
                        }
                    }
                }

                // now try to match the pending items with an unclaimed container
                foreach (ItemInfo info in list)
                {
                    DependencyObject container = info.Container;
                    if (container == null)
                    {
                        int index = info.Index;
                        if (index >= 0)
                        {
                            // if we know the index, see if the container exists
                            container = ItemContainerGenerator.ContainerFromIndex(index);
                        }
                        else
                        {
                            // otherwise see if an unclaimed container matches the item
                            object item = info.Item;
                            ItemContainerGenerator.FindItem(
                                delegate(object o, DependencyObject d)
                                    { return ItemsControl.EqualsEx(o, item) &&
                                        !claimedContainers.Contains(d); },
                                out container, out index);
                        }

                        if (container != null)
                        {
                            // update ItemInfo and claim the container
                            info.Container = container;
                            info.Index = index;
                            if (claimUniqueContainer)
                            {
                                claimedContainers.Add(container);
                            }
                        }
                    }
                }
            }
        }

        // correct the indices in the given ItemInfo, in response to a collection change event
        internal void AdjustItemInfo(NotifyCollectionChangedEventArgs e, ItemInfo info)
        {
            if (info != null)
            {
                ItemInfo[] a = new ItemInfo[]{info};
                AdjustItemInfos(e, a);
            }
        }

        // correct the indices in the given ItemInfos, in response to a collection change event
        internal void AdjustItemInfos(NotifyCollectionChangedEventArgs e, IEnumerable<ItemInfo> list)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    // items at NewStartingIndex and above have moved up 1
                    foreach (ItemInfo info in list)
                    {
                        int index = info.Index;
                        if (index >= e.NewStartingIndex)
                        {
                            info.Index = index + 1;
                        }
                    }
                break;

                case NotifyCollectionChangedAction.Remove:
                    // items at OldStartingIndex and above have moved down 1
                    foreach (ItemInfo info in list)
                    {
                        int index = info.Index;
                        if (index > e.OldStartingIndex)
                        {
                            info.Index = index - 1;
                        }
                        else if (index == e.OldStartingIndex)
                        {
                            info.Index = -1;
                        }
                    }
                break;

                case NotifyCollectionChangedAction.Move:
                    // items between New and Old have moved.  The direction and
                    // exact endpoints depends on whether New comes before Old.
                    int left, right, delta;
                    if (e.OldStartingIndex < e.NewStartingIndex)
                    {
                        left = e.OldStartingIndex + 1;
                        right = e.NewStartingIndex;
                        delta = -1;
                    }
                    else
                    {
                        left = e.NewStartingIndex;
                        right = e.OldStartingIndex - 1;
                        delta = 1;
                    }

                    foreach (ItemInfo info in list)
                    {
                        int index = info.Index;
                        if (index == e.OldStartingIndex)
                        {
                            info.Index = e.NewStartingIndex;
                        }
                        else if (left <= index && index <= right)
                        {
                            info.Index = index + delta;
                        }
                    }
                break;

                case NotifyCollectionChangedAction.Replace:
                    // nothing to do
                break;

                case NotifyCollectionChangedAction.Reset:
                    // the indices and containers are no longer valid
                    foreach (ItemInfo info in list)
                    {
                        info.Index = -1;
                        info.Container = null;
                    }
                break;
            }
        }

        // return an ItemInfo like the input one, but owned by this ItemsControl
        internal ItemInfo LeaseItemInfo(ItemInfo info, bool ensureIndex=false)
        {
            // if the original has index data, it's already good enough
            if (info.Index < 0)
            {
                // otherwise create a new info from the original's item
                info = NewItemInfo(info.Item);
                if (ensureIndex && info.Index < 0)
                {
                    info.Index = Items.IndexOf(info.Item);
                }
            }

            return info;
        }

        // refresh an ItemInfo
        internal void RefreshItemInfo(ItemInfo info)
        {
            if (info != null)
            {
                info.Refresh(ItemContainerGenerator);
            }
        }

        [DebuggerDisplay("Index: {Index}  Item: {Item}")]
        internal class ItemInfo
        {
            internal object Item { get; private set; }
            internal DependencyObject Container { get; set; }
            internal int Index { get; set; }

            internal static readonly DependencyObject SentinelContainer = new DependencyObject();
            internal static readonly DependencyObject UnresolvedContainer = new DependencyObject();
            internal static readonly DependencyObject KeyContainer = new DependencyObject();
            internal static readonly DependencyObject RemovedContainer = new DependencyObject();

            static ItemInfo()
            {
                // mark the special DOs as sentinels.  This helps catch bugs involving
                // using them accidentally for anything besides equality comparison.
                SentinelContainer.MakeSentinel();
                UnresolvedContainer.MakeSentinel();
                KeyContainer.MakeSentinel();
                RemovedContainer.MakeSentinel();
            }

            public ItemInfo(object item, DependencyObject container=null, int index=-1)
            {
                Item = item;
                Container = container;
                Index = index;
            }

            internal bool IsResolved { get { return Container != UnresolvedContainer; } }
            internal bool IsKey { get { return Container == KeyContainer; } }
            internal bool IsRemoved { get { return Container == RemovedContainer; } }

            internal ItemInfo Clone()
            {
                return new ItemInfo(Item, Container, Index);
            }

            internal static ItemInfo Key(ItemInfo info)
            {
                return (info.Container == UnresolvedContainer)
                    ? new ItemInfo(info.Item, KeyContainer, -1)
                    : info;
            }

            public override int GetHashCode()
            {
                return (Item != null) ? Item.GetHashCode() : 314159;
            }

            public override bool Equals(object o)
            {
                if (o == (object)this)
                    return true;

                ItemInfo that = o as ItemInfo;
                if (that == null)
                    return false;

                return Equals(that, matchUnresolved:false);
            }

            internal bool Equals(ItemInfo that, bool matchUnresolved)
            {
                // Removed matches nothing
                if (this.IsRemoved || that.IsRemoved)
                    return false;

                // items must match
                if (!ItemsControl.EqualsEx(this.Item, that.Item))
                    return false;

                // Key matches anything, except Unresolved when matchUnresovled is false
                if (this.Container == KeyContainer)
                    return matchUnresolved || that.Container != UnresolvedContainer;
                else if (that.Container == KeyContainer)
                    return matchUnresolved || this.Container != UnresolvedContainer;

                // Unresolved matches nothing
                if (this.Container == UnresolvedContainer || that.Container == UnresolvedContainer)
                    return false;

                return
                    (this.Container == that.Container)
                     ?  (this.Container == SentinelContainer)
                         ?  (this.Index == that.Index)      // Sentinel => negative indices are significant
                         :  (this.Index < 0 || that.Index < 0 ||
                                this.Index == that.Index)   // ~Sentinel => ignore negative indices
                     :  (this.Container == SentinelContainer) ||    // sentinel matches non-sentinel
                        (that.Container == SentinelContainer) ||
                        (   (this.Container == null || that.Container == null) &&   // null matches non-null
                            (this.Index < 0 || that.Index < 0 ||                    // provided that indices match
                                this.Index == that.Index));
            }

            public static bool operator ==(ItemInfo info1, ItemInfo info2)
            {
                return Object.Equals(info1, info2);
            }

            public static bool operator !=(ItemInfo info1, ItemInfo info2)
            {
                return !Object.Equals(info1, info2);
            }

            // update container and index with current values
            internal ItemInfo Refresh(ItemContainerGenerator generator)
            {
                if (Container == null && Index < 0)
                {
                    Container = generator.ContainerFromItem(Item);
                }

                if (Index < 0 && Container != null)
                {
                    Index = generator.IndexFromContainer(Container);
                }

                if (Container == null && Index >= 0)
                {
                    Container = generator.ContainerFromIndex(Index);
                }

                if (Container == SentinelContainer && Index >= 0)
                {
                    Container = null;   // caller explicitly wants null container
                }

                return this;
            }

            // Don't call this on entries used in hashtables - it changes the hashcode
            internal void Reset(object item)
            {
                Item = item;
            }
        }

        #endregion ItemInfo

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

        #region Method Overrides

        /// <summary>
        ///     Returns a string representation of this object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            // HasItems may be wrong when underlying collection does not notify,
            // but this function should try to return what's consistent with ItemsControl state.
            int itemsCount = HasItems ? Items.Count : 0;
            return SR.Get(SRID.ToStringFormatString_ItemsControl, this.GetType(), itemsCount);
        }

        // This should really override OnCreateAutomationPeer, but that API addition
        // isn't an option.   When it becomes an option:
        //  a. rename this method to OnCreateAutomationPeer
        //  b. change its visibilty from internal to protected
        //  c. remove UIElement.OnCreateAutomationPeerInternal, and its use in
        //      UIElement.CreateAutomationPeer
        //  d. change visibility of ItemsControlWrapperAutomationPeer and
        //      ItemsControlItemAutomationPeer from internal to public
        internal override AutomationPeer OnCreateAutomationPeerInternal()
        {
            return new ItemsControlWrapperAutomationPeer(this);
        }

        #endregion

        #region Data

        private ItemCollection _items;                      // Cache for Items property
        private ItemContainerGenerator _itemContainerGenerator;
        private Panel _itemsHost;
        private ScrollViewer _scrollHost;
        private ObservableCollection<GroupStyle> _groupStyle = new ObservableCollection<GroupStyle>();
        private static readonly UncommonField<bool> ShouldCoerceScrollUnitField = new UncommonField<bool>();
        private static readonly UncommonField<bool> ShouldCoerceCacheSizeField = new UncommonField<bool>();

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

    internal enum ElementViewportPosition
    {
        None,
        BeforeViewport,
        PartiallyInViewport,
        CompletelyInViewport,
        AfterViewport
    }
}

