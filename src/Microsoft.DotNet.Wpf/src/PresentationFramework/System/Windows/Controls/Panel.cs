// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Controls.Primitives;   // IItemContainerGenerator
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Markup; // IAddChild, ContentPropertyAttribute
using System.Windows.Threading;
using MS.Internal;
using MS.Internal.Controls;
using MS.Internal.KnownBoxes;
using MS.Internal.PresentationFramework;
using MS.Utility;

namespace System.Windows.Controls
{
    /// <summary>
    ///     Base class for all layout panels.
    /// </summary>
    [Localizability(LocalizationCategory.Ignore)]
    [ContentProperty("Children")]
    public abstract class Panel : FrameworkElement, IAddChild
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
        protected Panel() : base()
        {
            _zConsonant = (int)ZIndexProperty.GetDefaultValue(DependencyObjectType);
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        #region Public Methods

        /// <summary>
        ///     Fills in the background based on the Background property.
        /// </summary>
        protected override void OnRender(DrawingContext dc)
        {
            Brush background = Background;
            if (background != null)
            {
                // Using the Background brush, draw a rectangle that fills the
                // render bounds of the panel.
                Size renderSize = RenderSize;
                dc.DrawRectangle(background,
                                 null,
                                 new Rect(0.0, 0.0, renderSize.Width, renderSize.Height));
            }
        }

        ///<summary>
        /// This method is called to Add the object as a child of the Panel.  This method is used primarily
        /// by the parser.
        ///</summary>
        ///<param name="value">
        /// The object to add as a child; it must be a UIElement.
        ///</param>
        /// <ExternalAPI/>
        void IAddChild.AddChild (Object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            if(IsItemsHost)
            {
                throw new InvalidOperationException(SR.Get(SRID.Panel_BoundPanel_NoChildren));
            }

            UIElement uie = value as UIElement;

            if (uie == null)
            {
                throw new ArgumentException(SR.Get(SRID.UnexpectedParameterType, value.GetType(), typeof(UIElement)), "value");
            }

            Children.Add(uie);
        }

        ///<summary>
        /// This method is called by the parser when text appears under the tag in markup.
        /// As default Panels do not support text, calling this method has no effect.
        ///</summary>
        ///<param name="text">
        /// Text to add as a child.
        ///</param>
        void IAddChild.AddText (string text)
        {
            XamlSerializerUtil.ThrowIfNonWhiteSpaceInAddText(text, this);
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Properties + Avalon Dependency ID's
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// The Background property defines the brush used to fill the area between borders.
        /// </summary>
        public Brush Background
        {
            get { return (Brush) GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="Background" /> property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty BackgroundProperty =
                DependencyProperty.Register("Background",
                        typeof(Brush),
                        typeof(Panel),
                        new FrameworkPropertyMetadata((Brush)null,
                                FrameworkPropertyMetadataOptions.AffectsRender |
                                FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender));

        /// <summary>
        /// Returns enumerator to logical children.
        /// </summary>
        protected internal override IEnumerator LogicalChildren
        {
            get
            {
                if ((this.VisualChildrenCount == 0) || IsItemsHost)
                {
                    // empty panel or a panel being used as the items
                    // host has *no* logical children; give empty enumerator
                    return EmptyEnumerator.Instance;
                }

                // otherwise, its logical children is its visual children
                return this.Children.GetEnumerator();
            }
        }

        /// <summary>
        /// Returns a UIElementCollection of children for user to add/remove children manually
        /// Returns read-only collection if Panel is data-bound (no manual control of children is possible,
        /// the associated ItemsControl completely overrides children)
        /// Note: the derived Panel classes should never use this collection for
        /// internal purposes like in their MeasureOverride or ArrangeOverride.
        /// They should use InternalChildren instead, because InternalChildren
        /// is always present and either is a mirror of public Children collection (in case of Direct Panel)
        /// or is generated from data binding.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public UIElementCollection Children
        {
            get
            {
                //When we will change from UIElementCollection to IList<UIElement>, we might
                //consider returning a wrapper IList here which coudl be read-only for mutating methods
                //while INternalChildren could be R/W even in case of Generator attached.
                return InternalChildren;
            }
        }

        /// <summary>
        /// This method is used by TypeDescriptor to determine if this property should
        /// be serialized.
        /// </summary>
        // Should serialize property Children only if it is non empty
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeChildren()
        {
            if (!IsItemsHost)
            {
                if (Children != null && Children.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        ///     The DependencyProperty for the IsItemsHost property.
        ///     Flags:              NotDataBindable
        ///     Default Value:      false
        /// </summary>
        public static readonly DependencyProperty IsItemsHostProperty =
                DependencyProperty.Register(
                        "IsItemsHost",
                        typeof(bool),
                        typeof(Panel),
                        new FrameworkPropertyMetadata(
                                BooleanBoxes.FalseBox, // defaultValue
                                FrameworkPropertyMetadataOptions.NotDataBindable,
                                new PropertyChangedCallback(OnIsItemsHostChanged)));

        /// <summary>
        ///     IsItemsHost is set to true to indicate that the panel
        ///     is the container for UI generated for the items of an
        ///     ItemsControl.  It is typically set in a style for an ItemsControl.
        /// </summary>
        [Bindable(false), Category("Behavior")]
        public bool IsItemsHost
        {
            get { return (bool) GetValue(IsItemsHostProperty); }
            set { SetValue(IsItemsHostProperty, BooleanBoxes.Box(value)); }
        }

        private static void OnIsItemsHostChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Panel panel = (Panel) d;

            panel.OnIsItemsHostChanged((bool) e.OldValue, (bool) e.NewValue);
        }

        /// <summary>
        ///     This method is invoked when the IsItemsHost property changes.
        /// </summary>
        /// <param name="oldIsItemsHost">The old value of the IsItemsHost property.</param>
        /// <param name="newIsItemsHost">The new value of the IsItemsHost property.</param>
        protected virtual void OnIsItemsHostChanged(bool oldIsItemsHost, bool newIsItemsHost)
        {
            // GetItemsOwner will check IsItemsHost first, so we don't have
            // to check that IsItemsHost == true before calling it.
            DependencyObject parent = ItemsControl.GetItemsOwnerInternal(this);
            ItemsControl itemsControl = parent as ItemsControl;
            Panel oldItemsHost = null;

            if (itemsControl != null)
            {
                // ItemsHost should be the "root" element which has
                // IsItemsHost = true on it.  In the case of grouping,
                // IsItemsHost is true on all panels which are generating
                // content.  Thus, we care only about the panel which
                // is generating content for the ItemsControl.
                IItemContainerGenerator generator = itemsControl.ItemContainerGenerator as IItemContainerGenerator;
                if (generator != null && generator == generator.GetItemContainerGeneratorForPanel(this))
                {
                    oldItemsHost = itemsControl.ItemsHost;
                    itemsControl.ItemsHost = this;
                }
            }
            else
            {
                GroupItem groupItem = parent as GroupItem;
                if (groupItem != null)
                {
                    IItemContainerGenerator generator = groupItem.Generator as IItemContainerGenerator;
                    if (generator != null && generator == generator.GetItemContainerGeneratorForPanel(this))
                    {
                        oldItemsHost = groupItem.ItemsHost;
                        groupItem.ItemsHost = this;
                    }
                }
            }

            if (oldItemsHost != null && oldItemsHost != this)
            {
                // when changing ItemsHost panels, disconnect the old one
                oldItemsHost.VerifyBoundState();
            }

            VerifyBoundState();
        }

        /// <summary>
        ///     This is the public accessor for protected property LogicalOrientation.
        /// </summary>
        public Orientation LogicalOrientationPublic
        {
            get { return LogicalOrientation; }
        }

        /// <summary>
        ///     Orientation of the panel if its layout is in one dimension.
        /// Otherwise HasLogicalOrientation is false and LogicalOrientation should be ignored
        /// </summary>
        protected internal virtual Orientation LogicalOrientation
        {
            get { return Orientation.Vertical; }
        }

        /// <summary>
        ///     This is the public accessor for protected property HasLogicalOrientation.
        /// </summary>
        public bool HasLogicalOrientationPublic
        {
            get { return HasLogicalOrientation; }
        }

        /// <summary>
        ///     HasLogicalOrientation is true in case the panel layout is only one dimension (Stack panel).
        /// </summary>
        protected internal virtual bool HasLogicalOrientation
        {
            get { return false; }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Returns a UIElementCollection of children - added by user or generated from data binding.
        /// Panel-derived classes should use this collection for all internal purposes, including
        /// MeasureOverride/ArrangeOverride overrides.
        /// </summary>
        protected internal UIElementCollection InternalChildren
        {
            get
            {
                VerifyBoundState();

                if (IsItemsHost)
                {
                    EnsureGenerator();
                }
                else
                {
                    if (_uiElementCollection == null)
                    {
                        // First access on a regular panel
                        EnsureEmptyChildren(/* logicalParent = */ this);
                    }
                }

                return _uiElementCollection;
            }
        }

        /// <summary>
        /// Gets the Visual children count.
        /// </summary>
        protected override int VisualChildrenCount
        {
            get
            {
                if (_uiElementCollection == null)
                {
                    return 0;
                }
                else
                {
                    return _uiElementCollection.Count;
                }
            }
        }

        /// <summary>
        /// Gets the Visual child at the specified index.
        /// </summary>
        protected override Visual GetVisualChild(int index)
        {
            if (_uiElementCollection == null)
            {
                throw new ArgumentOutOfRangeException("index", index, SR.Get(SRID.Visual_ArgumentOutOfRange));
            }

            if (IsZStateDirty) { RecomputeZState(); }
            int visualIndex = _zLut != null ? _zLut[index] : index;
            return _uiElementCollection[visualIndex];
        }

        /// <summary>
        /// Creates a new UIElementCollection. Panel-derived class can create its own version of
        /// UIElementCollection -derived class to add cached information to every child or to
        /// intercept any Add/Remove actions (for example, for incremental layout update)
        /// </summary>
        protected virtual UIElementCollection CreateUIElementCollection(FrameworkElement logicalParent)
        {
            return new UIElementCollection(this, logicalParent);
        }

        /// <summary>
        ///     The generator associated with this panel.
        /// </summary>
        internal IItemContainerGenerator Generator
        {
            get
            {
                return _itemContainerGenerator;
            }
        }

        #endregion

        #region Internal Properties

        //
        // Bool field used by VirtualizingStackPanel
        //
        internal bool VSP_IsVirtualizing
        {
            get
            {
                return GetBoolField(BoolField.IsVirtualizing);
            }

            set
            {
                SetBoolField(BoolField.IsVirtualizing, value);
            }
        }

        //
        // Bool field used by VirtualizingStackPanel
        //
        internal bool VSP_HasMeasured
        {
            get
            {
                return GetBoolField(BoolField.HasMeasured);
            }

            set
            {
                SetBoolField(BoolField.HasMeasured, value);
            }
        }


        //
        // Bool field used by VirtualizingStackPanel
        //
        internal bool VSP_MustDisableVirtualization
        {
            get
            {
                return GetBoolField(BoolField.MustDisableVirtualization);
            }

            set
            {
                SetBoolField(BoolField.MustDisableVirtualization, value);
            }
        }

        //
        // Bool field used by VirtualizingStackPanel
        //
        internal bool VSP_IsPixelBased
        {
            get
            {
                return GetBoolField(BoolField.IsPixelBased);
            }

            set
            {
                SetBoolField(BoolField.IsPixelBased, value);
            }
        }

        //
        // Bool field used by VirtualizingStackPanel
        //
        internal bool VSP_InRecyclingMode
        {
            get
            {
                return GetBoolField(BoolField.InRecyclingMode);
            }

            set
            {
                SetBoolField(BoolField.InRecyclingMode, value);
            }
        }

        //
        // Bool field used by VirtualizingStackPanel
        //
        internal bool VSP_MeasureCaches
        {
            get
            {
                return GetBoolField(BoolField.MeasureCaches);
            }

            set
            {
                SetBoolField(BoolField.MeasureCaches, value);
            }
        }

        #endregion

        #region Private Methods

        private bool VerifyBoundState()
        {
            // If the panel becomes "unbound" while attached to a generator, this
            // method detaches it and makes it really behave like "unbound."  This
            // can happen because of a style change, a theme change, etc. It returns
            // the correct "bound" state, after the dust has settled.
            //
            // This is really a workaround for a more general problem that the panel
            // needs to release resources (an event handler) when it is "out of the tree."
            // Currently, there is no good notification for when this happens.

            bool isItemsHost = (ItemsControl.GetItemsOwnerInternal(this) != null);

            if (isItemsHost)
            {
                if (_itemContainerGenerator == null)
                {
                    // Transitioning from being unbound to bound
                    ClearChildren();
                }

                return (_itemContainerGenerator != null);
            }
            else
            {
                if (_itemContainerGenerator != null)
                {
                    // Transitioning from being bound to unbound
                    DisconnectFromGenerator();
                    ClearChildren();
                }

                return false;
            }
        }

        //"actually data-bound and using generator" This is true if Panel is
        //not only marked as IsItemsHost but actually has requested Generator to
        //generate items and thus "owns" those items.
        //In this case, Children collection becomes read-only
        //Cases when it is not true include "direct" usage - IsItemsHost=false and
        //usages when panel is data-bound but derived class avoid accessing InternalChildren or Children
        //and rather calls CreateUIElementCollection and then drives Generator itself.
        internal bool IsDataBound
        {
            get
            {
                return IsItemsHost && _itemContainerGenerator != null;
            }
        }

        /// <summary> Used by subclasses to decide whether to call through a profiling stub </summary>
        internal static bool IsAboutToGenerateContent(Panel panel)
        {
            return panel.IsItemsHost && panel._itemContainerGenerator == null;
        }

        private void ConnectToGenerator()
        {
            Debug.Assert(_itemContainerGenerator == null, "Attempted to connect to a generator when Panel._itemContainerGenerator is non-null.");

            ItemsControl itemsOwner = ItemsControl.GetItemsOwner(this);
            if (itemsOwner == null)
            {
                // This can happen if IsItemsHost=true, but the panel is not nested in an ItemsControl
                throw new InvalidOperationException(SR.Get(SRID.Panel_ItemsControlNotFound));
            }

            IItemContainerGenerator itemsOwnerGenerator = itemsOwner.ItemContainerGenerator;
            if (itemsOwnerGenerator != null)
            {
                _itemContainerGenerator = itemsOwnerGenerator.GetItemContainerGeneratorForPanel(this);
                if (_itemContainerGenerator != null)
                {
                    _itemContainerGenerator.ItemsChanged += new ItemsChangedEventHandler(OnItemsChanged);
                    ((IItemContainerGenerator)_itemContainerGenerator).RemoveAll();
                }
            }
        }

        private void DisconnectFromGenerator()
        {
            Debug.Assert(_itemContainerGenerator != null, "Attempted to disconnect from a generator when Panel._itemContainerGenerator is null.");

            _itemContainerGenerator.ItemsChanged -= new ItemsChangedEventHandler(OnItemsChanged);
            ((IItemContainerGenerator)_itemContainerGenerator).RemoveAll();
            _itemContainerGenerator = null;
        }

        private void EnsureEmptyChildren(FrameworkElement logicalParent)
        {
            if ((_uiElementCollection == null) || (_uiElementCollection.LogicalParent != logicalParent))
            {
                _uiElementCollection = CreateUIElementCollection(logicalParent);
            }
            else
            {
                ClearChildren();
            }
        }

        internal void EnsureGenerator()
        {
            Debug.Assert(IsItemsHost, "Should be invoked only on an ItemsHost panel");

            if (_itemContainerGenerator == null)
            {
                // First access on an items presenter panel
                ConnectToGenerator();

                // Children of this panel should not have their logical parent reset
                EnsureEmptyChildren(/* logicalParent = */ null);

                GenerateChildren();
            }
        }


        private void ClearChildren()
        {
            if (_itemContainerGenerator != null)
            {
                ((IItemContainerGenerator)_itemContainerGenerator).RemoveAll();
            }

            if ((_uiElementCollection != null) && (_uiElementCollection.Count > 0))
            {
                _uiElementCollection.ClearInternal();
                OnClearChildrenInternal();
            }
        }

        internal virtual void OnClearChildrenInternal()
        {
        }

        internal virtual void GenerateChildren()
        {
            // This method is typically called during layout, which suspends the dispatcher.
            // Firing an assert causes an exception "Dispatcher processing has been suspended, but messages are still being processed."
            // Besides, the asserted condition can actually arise in practice, and the
            // code responds harmlessly.
            //Debug.Assert(_itemContainerGenerator != null, "Encountered a null _itemContainerGenerator while being asked to generate children.");

            IItemContainerGenerator generator = (IItemContainerGenerator)_itemContainerGenerator;
            if (generator != null)
            {
                using (generator.StartAt(new GeneratorPosition(-1, 0), GeneratorDirection.Forward))
                {
                    UIElement child;
                    while ((child = generator.GenerateNext() as UIElement) != null)
                    {
                        _uiElementCollection.AddInternal(child);
                        generator.PrepareItemContainer(child);
                    }
                }
            }
        }

        private void OnItemsChanged(object sender, ItemsChangedEventArgs args)
        {
            if (VerifyBoundState())
            {
                Debug.Assert(_itemContainerGenerator != null, "Encountered a null _itemContainerGenerator while receiving an ItemsChanged from a generator.");

                bool affectsLayout = OnItemsChangedInternal(sender, args);

                if (affectsLayout)
                {
                    InvalidateMeasure();
                }
            }
        }

        // This method returns a bool to indicate if or not the panel layout is affected by this collection change
        internal virtual bool OnItemsChangedInternal(object sender, ItemsChangedEventArgs args)
        {
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddChildren(args.Position, args.ItemCount);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    RemoveChildren(args.Position, args.ItemUICount);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    ReplaceChildren(args.Position, args.ItemCount, args.ItemUICount);
                    break;
                case NotifyCollectionChangedAction.Move:
                    MoveChildren(args.OldPosition, args.Position, args.ItemUICount);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    ResetChildren();
                    break;
            }

            return true;
        }

        private void AddChildren(GeneratorPosition pos, int itemCount)
        {
            Debug.Assert(_itemContainerGenerator != null, "Encountered a null _itemContainerGenerator while receiving an Add action from a generator.");

            IItemContainerGenerator generator = (IItemContainerGenerator)_itemContainerGenerator;
            using (generator.StartAt(pos, GeneratorDirection.Forward))
            {
                for (int i = 0; i < itemCount; i++)
                {
                    UIElement e = generator.GenerateNext() as UIElement;
                    if(e != null)
                    {
                        _uiElementCollection.InsertInternal(pos.Index + 1 + i, e);
                        generator.PrepareItemContainer(e);
                    }
                    else
                    {
                        _itemContainerGenerator.Verify();
                    }
                }
            }
        }

        private void RemoveChildren(GeneratorPosition pos, int containerCount)
        {
            // If anything is wrong, I think these collections should do parameter checking
            _uiElementCollection.RemoveRangeInternal(pos.Index, containerCount);
        }

        private void ReplaceChildren(GeneratorPosition pos, int itemCount, int containerCount)
        {
            Debug.Assert(itemCount == containerCount, "Panel expects Replace to affect only realized containers");
            Debug.Assert(_itemContainerGenerator != null, "Encountered a null _itemContainerGenerator while receiving an Replace action from a generator.");

            IItemContainerGenerator generator = (IItemContainerGenerator)_itemContainerGenerator;
            using (generator.StartAt(pos, GeneratorDirection.Forward, true))
            {
                for (int i = 0; i < itemCount; i++)
                {
                    bool isNewlyRealized;
                    UIElement e = generator.GenerateNext(out isNewlyRealized) as UIElement;

                    Debug.Assert(e != null && !isNewlyRealized, "Panel expects Replace to affect only realized containers");
                    if(e != null && !isNewlyRealized)
                    {
                        _uiElementCollection.SetInternal(pos.Index + i, e);
                        generator.PrepareItemContainer(e);
                    }
                    else
                    {
                        _itemContainerGenerator.Verify();
                    }
                }
            }
        }

        private void MoveChildren(GeneratorPosition fromPos, GeneratorPosition toPos, int containerCount)
        {
            if (fromPos == toPos)
                return;

            Debug.Assert(_itemContainerGenerator != null, "Encountered a null _itemContainerGenerator while receiving an Move action from a generator.");

            IItemContainerGenerator generator = (IItemContainerGenerator)_itemContainerGenerator;
            int toIndex = generator.IndexFromGeneratorPosition(toPos);

            UIElement[] elements = new UIElement[containerCount];

            for (int i = 0; i < containerCount; i++)
                elements[i] = _uiElementCollection[fromPos.Index + i];

            _uiElementCollection.RemoveRangeInternal(fromPos.Index, containerCount);

            for (int i = 0; i < containerCount; i++)
            {
                _uiElementCollection.InsertInternal(toIndex + i, elements[i]);
            }
        }

        private void ResetChildren()
        {
            EnsureEmptyChildren(null);
            GenerateChildren();
        }

        private bool GetBoolField(BoolField field)
        {
            return (_boolFieldStore & field) != 0;
        }

        private void SetBoolField(BoolField field, bool value)
        {
            if (value)
            {
                 _boolFieldStore |= field;
            }
            else
            {
                 _boolFieldStore &= (~field);
            }
        }

        //
        //  This property
        //  1. Finds the correct initial size for the _effectiveValues store on the current DependencyObject
        //  2. This is a performance optimization
        //
        internal override int EffectiveValuesInitialSize
        {
            get { return 9; }
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        [System.Flags]
        private enum BoolField : byte
        {
            IsZStateDirty                               = 0x01,   //  "1" when Z state needs to be recomputed
            IsZStateDiverse                             = 0x02,   //  "1" when children have different ZIndexProperty values
            IsVirtualizing                              = 0x04,   //  Used by VirtualizingStackPanel
            HasMeasured                                 = 0x08,   //  Used by VirtualizingStackPanel
            IsPixelBased                                = 0x10,   //  Used by VirtualizingStackPanel
            InRecyclingMode                             = 0x20,   //  Used by VirtualizingStackPanel
            MustDisableVirtualization                   = 0x40,   //  Used by VirtualizingStackPanel
            MeasureCaches                               = 0x80,   //  Used by VirtualizingStackPanel
        }

        private UIElementCollection _uiElementCollection;
        private ItemContainerGenerator _itemContainerGenerator;
        private BoolField _boolFieldStore;

        private const int c_zDefaultValue = 0;              //  default ZIndexProperty value
        private int _zConsonant;                            //  iff (_boolFieldStore.IsZStateDiverse == 0) then this is the value all children have
        private int[] _zLut;                                //  look up table for converting from logical to visual indices

        #endregion Private Fields

        #region ZIndex Support

        /// <summary>
        /// <see cref="Visual.OnVisualChildrenChanged"/>
        /// </summary>
        protected internal override void OnVisualChildrenChanged(
            DependencyObject visualAdded,
            DependencyObject visualRemoved)
        {
            if (!IsZStateDirty)
            {
                if (IsZStateDiverse)
                {
                    //  if children have different ZIndex values,
                    //  then _zLut have to be recomputed
                    IsZStateDirty = true;
                }
                else if (visualAdded != null)
                {
                    //  if current children have consonant ZIndex values,
                    //  then _zLut have to be recomputed, only if the new
                    //  child makes z state diverse
                    int zNew = (int)visualAdded.GetValue(ZIndexProperty);
                    if (zNew != _zConsonant)
                    {
                        IsZStateDirty = true;
                    }
                }
            }

            base.OnVisualChildrenChanged(visualAdded, visualRemoved);
            // Recompute the zLut array and invalidate children rendering order.
            if (IsZStateDirty)
            {
                RecomputeZState();
                InvalidateZState();
            }
        }

        /// <summary>
        /// ZIndex property is an attached property. Panel reads it to alter the order
        /// of children rendering. Children with greater values will be rendered on top of
        /// children with lesser values.
        /// In case of two children with the same ZIndex property value, order of rendering
        /// is determined by their order in Panel.Children collection.
        /// </summary>
        public static readonly DependencyProperty ZIndexProperty =
                DependencyProperty.RegisterAttached(
                        "ZIndex",
                        typeof(int),
                        typeof(Panel),
                        new FrameworkPropertyMetadata(
                                c_zDefaultValue,
                                new PropertyChangedCallback(OnZIndexPropertyChanged)));

        /// <summary>
        /// Helper for setting ZIndex property on a UIElement.
        /// </summary>
        /// <param name="element">UIElement to set ZIndex property on.</param>
        /// <param name="value">ZIndex property value.</param>
        public static void SetZIndex(UIElement element, int value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(ZIndexProperty, value);
        }

        /// <summary>
        /// Helper for reading ZIndex property from a UIElement.
        /// </summary>
        /// <param name="element">UIElement to read ZIndex property from.</param>
        /// <returns>ZIndex property value.</returns>
        public static int GetZIndex(UIElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return ((int)element.GetValue(ZIndexProperty));
        }

        /// <summary>
        /// <see cref="PropertyMetadata.PropertyChangedCallback"/>
        /// </summary>
        private static void OnZIndexPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            int oldValue = (int)e.OldValue;
            int newValue = (int)e.NewValue;

            if (oldValue == newValue)
                return;

            UIElement child = d as UIElement;
            if (child == null)
                return;

            Panel panel = child.InternalVisualParent as Panel;
            if (panel == null)
                return;


            panel.InvalidateZState();
        }

        /// <summary>
        /// Sets the Z state to be dirty
        /// </summary>
        internal void InvalidateZState()
        {
            if (!IsZStateDirty
             && _uiElementCollection != null)
            {
                InvalidateZOrder();
            }

            IsZStateDirty = true;
        }

        private bool IsZStateDirty
        {
            get { return GetBoolField(BoolField.IsZStateDirty); }
            set { SetBoolField(BoolField.IsZStateDirty, value); }
        }

        private bool IsZStateDiverse
        {
            get { return GetBoolField(BoolField.IsZStateDiverse); }
            set { SetBoolField(BoolField.IsZStateDiverse, value); }
        }

        //  Helper method to update this panel's state related to children rendering order handling
        private void RecomputeZState()
        {
            int count = (_uiElementCollection != null) ? _uiElementCollection.Count : 0;
            bool isDiverse = false;
            bool lutRequired = false;
            int zIndexDefaultValue = (int)ZIndexProperty.GetDefaultValue(DependencyObjectType);
            int consonant = zIndexDefaultValue;
            System.Collections.Generic.List<Int64> stableKeyValues = null;

            if (count > 0)
            {
                if (_uiElementCollection[0] != null)
                {
                    consonant = (int)_uiElementCollection[0].GetValue(ZIndexProperty);
                }

                if (count > 1)
                {
                    stableKeyValues = new System.Collections.Generic.List<Int64>(count);
                    stableKeyValues.Add((Int64)consonant << 32);

                    int prevZ = consonant;

                    int i = 1;
                    do
                    {
                        int z = _uiElementCollection[i] != null
                            ? (int)_uiElementCollection[i].GetValue(ZIndexProperty)
                            : zIndexDefaultValue;

                        //  this way of calculating values of stableKeyValues required to
                        //  1)  workaround the fact that Array.Sort is not stable (does not preserve the original
                        //      order of elements if the keys are equal)
                        //  2)  avoid O(N^2) performance of Array.Sort, which is QuickSort, which is known to become O(N^2)
                        //      on sorting N eqial keys
                        stableKeyValues.Add(((Int64)z << 32) + i);
                        //  look-up-table is required iff z's are not monotonically increasing function of index.
                        //  in other words if stableKeyValues[i] >= stableKeyValues[i-1] then calculated look-up-table
                        //  is guaranteed to be degenerated...
                        lutRequired |= z < prevZ;
                        prevZ = z;

                        isDiverse |= (z != consonant);
                    } while (++i < count);
                }
            }

            if (lutRequired)
            {
                stableKeyValues.Sort();

                if (_zLut == null || _zLut.Length != count)
                {
                    _zLut = new int[count];
                }

                for (int i = 0; i < count; ++i)
                {
                    _zLut[i] = (int)(stableKeyValues[i] & 0xffffffff);
                }
            }
            else
            {
                _zLut = null;
            }

            IsZStateDiverse = isDiverse;
            _zConsonant = consonant;
            IsZStateDirty = false;
        }

        #endregion ZIndex Support
    }
}
