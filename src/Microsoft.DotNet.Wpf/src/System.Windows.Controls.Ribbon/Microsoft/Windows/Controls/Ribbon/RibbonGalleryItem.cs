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
    using System.Collections.Specialized;
    using System.Windows;
    using System.Windows.Automation;
    using System.Windows.Automation.Peers;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Threading;
#if !RIBBON_IN_FRAMEWORK
    using Microsoft.Windows.Automation.Peers;
#endif

    #endregion

    /// <summary>
    ///   RibbonGalleryItem inherits from ContentControl is an Item inside RibbonGalleryCategory which
    ///   itself is an Item of RibbonGallery itself.
    /// </summary>
    public class RibbonGalleryItem : ContentControl, ISyncKeyTipAndContent
    {
        #region Constructors

        /// <summary>
        ///   Initializes static members of the RibbonGalleryItem class.
        /// </summary>
        static RibbonGalleryItem()
        {
            Type ownerType = typeof(RibbonGalleryItem);
            DefaultStyleKeyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(ownerType));
            ContentProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnContentChanged), new CoerceValueCallback(CoerceContent)));
            ToolTipProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(null, new CoerceValueCallback(RibbonHelper.CoerceRibbonToolTip)));
            ToolTipService.ShowOnDisabledProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(true));
#if RIBBON_IN_FRAMEWORK
            AutomationProperties.IsOffscreenBehaviorProperty.OverrideMetadata(typeof(RibbonGalleryItem), new FrameworkPropertyMetadata(IsOffscreenBehavior.FromClip));
#endif
            EventManager.RegisterClassHandler(ownerType, KeyTipService.ActivatingKeyTipEvent, new ActivatingKeyTipEventHandler(OnActivatingKeyTipThunk));
            EventManager.RegisterClassHandler(ownerType, KeyTipService.KeyTipAccessedEvent, new KeyTipAccessedEventHandler(OnKeyTipAccessedThunk));
        }

        #endregion Constructors

        #region Tree

        internal RibbonGalleryCategory RibbonGalleryCategory
        {
            get;
            set;
        }

        internal RibbonGallery RibbonGallery
        {
            get
            {
                return RibbonGalleryCategory != null ? RibbonGalleryCategory.RibbonGallery : null;
            }
        }

        #endregion Tree

        #region Selection

        /// <summary>
        ///     Raised when the item's IsSelected property becomes true.
        /// </summary>
        public static readonly RoutedEvent SelectedEvent = 
            EventManager.RegisterRoutedEvent("Selected", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(RibbonGalleryItem));
        
        /// <summary>
        ///     Raised when the item's IsSelected property becomes true.
        /// </summary>
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
        protected internal virtual void OnSelected(RoutedEventArgs e)
        {
            RaiseEvent(e);
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (DispatcherOperationCallback)delegate(object unused)
            {
                BringIntoView();
                return null;
            }, null);

            RibbonGalleryItemAutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(this) as RibbonGalleryItemAutomationPeer;
            if (peer != null)
            {
                peer.RaiseAutomationIsSelectedChanged(true);
                peer.RaiseAutomationSelectionEvent(AutomationEvents.SelectionItemPatternOnElementSelected);
            }
        }

        /// <summary>
        ///     Raised when the item's IsSelected property becomes false.
        /// </summary>
        public static readonly RoutedEvent UnselectedEvent = 
            EventManager.RegisterRoutedEvent("Unselected", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(RibbonGalleryItem));
        
        /// <summary>
        ///     Raised when the item's IsSelected property becomes false.
        /// </summary>
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
        protected internal virtual void OnUnselected(RoutedEventArgs e)
        {
            RaiseEvent(e);

            RibbonGalleryItemAutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(this) as RibbonGalleryItemAutomationPeer;
            if (peer != null)
            {
                peer.RaiseAutomationIsSelectedChanged(false);
                peer.RaiseAutomationSelectionEvent(AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection);
            }
        }

        /// <summary>
        ///     Indicates whether this RibbonGalleryItem is selected.
        /// </summary>
        public static readonly DependencyProperty IsSelectedProperty =
                DependencyProperty.Register(
                    "IsSelected",
                    typeof(bool),
                    typeof(RibbonGalleryItem),
                    new FrameworkPropertyMetadata(
                            false,
                            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                            new PropertyChangedCallback(OnIsSelectedChanged)));

        /// <summary>
        ///     Indicates whether this RibbonGalleryItem is selected.
        /// </summary>
        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonGalleryItem galleryItem = (RibbonGalleryItem)d;
            bool isSelected = (bool)e.NewValue;

            RibbonGalleryCategory category = galleryItem.RibbonGalleryCategory;

            if( category != null )
            {
                RibbonGallery gallery = category.RibbonGallery;
                if (gallery != null)
                {
                    // Give the RibbonGallery a reference to this container and its data
                    object item = category.ItemContainerGenerator.ItemFromContainer(galleryItem);
                    if (item == DependencyProperty.UnsetValue)
                    {
                        item = galleryItem;
                    }
                    gallery.ChangeSelection(item, galleryItem, isSelected);
                }
            }
        }

        #endregion Selection

        #region Highlight
        
        /// <summary>
        ///     Indicates whether this RibbonGalleryItem is highlighted.
        /// </summary>
        private static readonly DependencyPropertyKey IsHighlightedPropertyKey =
                DependencyProperty.RegisterReadOnly(
                    "IsHighlighted",
                    typeof(bool),
                    typeof(RibbonGalleryItem),
                    new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnIsHighlightedChanged)));

        /// <summary>
        ///     The DependencyProperty for the IsHighlighted property.
        ///     Flags:              None
        ///     Default Value:      false
        /// </summary>
        public static readonly DependencyProperty IsHighlightedProperty =
                IsHighlightedPropertyKey.DependencyProperty;
                            

        /// <summary>
        ///     Indicates whether this RibbonGalleryItem is highlighted.
        /// </summary>
        public bool IsHighlighted
        {
            get { return (bool)GetValue(IsHighlightedProperty); }
            internal set { SetValue(IsHighlightedPropertyKey,value); }
        }

        private static void OnIsHighlightedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonGalleryItem galleryItem = (RibbonGalleryItem)d;
            bool isHighlighted = (bool)e.NewValue;

            RibbonGalleryCategory category = galleryItem.RibbonGalleryCategory;
            if (category != null)
            {
                RibbonGallery gallery = category.RibbonGallery;
                if (gallery != null)
                {
                    // Give the RibbonGallery a reference to this container and its data
                    object item = category.ItemContainerGenerator.ItemFromContainer(galleryItem);
                    gallery.ChangeHighlight(item, galleryItem, isHighlighted);
                }
            }
        }

        #endregion Highlight

        #region Pressed

        /// <summary>
        ///     The key needed set a read-only property.
        /// </summary>
        private static readonly DependencyPropertyKey IsPressedPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "IsPressed",
                        typeof(bool),
                        typeof(RibbonGalleryItem),
                        new FrameworkPropertyMetadata(
                                false));

        /// <summary>
        ///     The DependencyProperty for the IsPressed property.
        ///     Flags:              None
        ///     Default Value:      false
        /// </summary>
        public static readonly DependencyProperty IsPressedProperty =
                IsPressedPropertyKey.DependencyProperty;

        /// <summary>
        ///     IsPressed is the state of a button indicates that left mouse button is pressed or space key is pressed over the button.
        /// </summary>
        public bool IsPressed
        {
            get { return (bool)GetValue(IsPressedProperty); }
            private set { SetValue(IsPressedPropertyKey, value); }
        }

        #endregion Press

        #region Input Handling

        /// <summary>
        ///   This is the method that responds to the MouseButtonEvent event.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            // Parent constrols such as RibbonComboBox don't want this item to acquire focus

            RibbonGallery gallery = RibbonGallery;
            if (gallery != null)
            {
                if (gallery.ShouldGalleryItemsAcquireFocus)
                {
                    Focus();
                }

                try
                {
                    gallery.HasHighlightChangedViaMouse = true;
                    IsHighlighted = true;
                }
                finally
                {
                    gallery.HasHighlightChangedViaMouse = false;
                }

                if (e.ButtonState == MouseButtonState.Pressed)
                {
                    IsPressed = true;
                }

                e.Handled = true;
            }

            base.OnMouseLeftButtonDown(e);
        }

        /// <summary>
        ///   This is the method that responds to the MouseButtonEvent event.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (IsHighlighted)
            {
                SetSelectedOnInput();
            }

            IsPressed = false;
            e.Handled = true;

            base.OnMouseLeftButtonUp(e);
        }

        /// <summary>
        ///     An event reporting the mouse left this element.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnMouseLeave(MouseEventArgs e)
        {
            RibbonGallery gallery = RibbonGallery;
            if (gallery != null && gallery.DidMouseMove(e))
            {
                try
                {
                    gallery.HasHighlightChangedViaMouse = true;
                    IsHighlighted = false;
                }
                finally
                {
                    gallery.HasHighlightChangedViaMouse = false;
                }
                IsPressed = false;
                e.Handled = true;
            }

            base.OnMouseLeave(e);
        }

        /// <summary>
        ///   This is the method that responds to the MouseEvent event.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            RibbonGallery gallery = RibbonGallery;            
            if (gallery != null && gallery.DidMouseMove(e))
            {
                // Parent constrols such as RibbonComboBox don't want this item to acquire focus

                if (gallery.ShouldGalleryItemsAcquireFocus)
                {
                    Focus();
                }

                try
                {
                    gallery.HasHighlightChangedViaMouse = true;
                    IsHighlighted = true;
                }
                finally
                {
                    gallery.HasHighlightChangedViaMouse = false;
                }
                e.Handled = true;
            }

            base.OnMouseMove(e);
        }

        /// <summary>
        ///   This method is invoked when the IsFocused property changes to true.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            IsHighlighted = true;
            e.Handled = true;

            base.OnGotKeyboardFocus(e);
        }

        /// <summary>
        ///   This is the method that responds to the KeyDown event.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                // Alt+Space should bring up system menu, we shouldn't handle it.
                if ((Keyboard.Modifiers & ModifierKeys.Alt) != ModifierKeys.Alt)
                {
                    if (e.OriginalSource == this)
                    {
                        IsPressed = true;
                        e.Handled = true;
                    }
                }
            }
            else if (e.Key == Key.Enter)
            {
                if (e.OriginalSource == this)
                {
                    if (IsHighlighted)
                    {
                        SetSelectedOnInput();
                        e.Handled = true;
                    }
                }
            }

            base.OnKeyDown(e);
        }

        /// <summary>
        ///   This is the method that responds to the KeyUp event.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                // Alt+Space should bring up system menu, we shouldn't handle it.
                if ((Keyboard.Modifiers & ModifierKeys.Alt) != ModifierKeys.Alt)
                {
                    if (e.OriginalSource == this)
                    {
                        SetSelectedOnInput();
                        IsPressed = false;
                        e.Handled = true;
                    }
                }
            }

            base.OnKeyUp(e);
        }
        
        /// <summary>
        ///   An event announcing that the keyboard is no longer focused
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            // ShouldGalleryITemsAcquireFocus is set on the first Gallery in a RibbonComboBox. 
            // If this flag is set GalleryItems do not acquire focus upon being Highlighted. 
            // On the same lines we shouldn't be de-highlighting upon loss of focus either. 
            // The scenario for loss of focus arises when the GalleryItem is navigated to via 
            // regular keyboard navigation logic from a sibling RibbonMenuItem or Gallery. 
            // In this case RibbonComboBox has logic to reacquire focus itself. But we do not 
            // want this operation to trigger a dehighlight.

            if (e.OriginalSource == this && RibbonGallery != null && RibbonGallery.ShouldGalleryItemsAcquireFocus)
            {
                IsPressed = false;
                IsHighlighted = false;
                e.Handled = true;
            }

            base.OnLostKeyboardFocus(e);
        }

        private void SetSelectedOnInput()
        {
            IsSelected = true;
            RaiseEvent(new RibbonDismissPopupEventArgs());
        }

        #endregion Input Handling

        #region Automation
        
        ///
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new RibbonGalleryItemAutomationPeer(this);
        }

        #endregion Automation

        #region VisualStates
        /// <summary>
        ///     DependencyProperty for Ribbon property.
        /// </summary>
        public static readonly DependencyProperty RibbonProperty =
            RibbonControlService.RibbonProperty.AddOwner(typeof(RibbonGalleryItem));

        /// <summary>
        ///     This property is used to access visual style brushes defined on the Ribbon class.
        /// </summary>
        public Ribbon Ribbon
        {
            get { return RibbonControlService.GetRibbon(this); }
        }

        /// <summary>
        ///     DependencyProperty for MouseOverBorderBrush property.
        /// </summary>
        public static readonly DependencyProperty MouseOverBorderBrushProperty =
            RibbonControlService.MouseOverBorderBrushProperty.AddOwner(typeof(RibbonGalleryItem));

        /// <summary>
        ///     Outer border brush used in a "hover" state of the RibbonGalleryItem.
        /// </summary>
        public Brush MouseOverBorderBrush
        {
            get { return RibbonControlService.GetMouseOverBorderBrush(this); }
            set { RibbonControlService.SetMouseOverBorderBrush(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for MouseOverBackground property.
        /// </summary>
        public static readonly DependencyProperty MouseOverBackgroundProperty =
            RibbonControlService.MouseOverBackgroundProperty.AddOwner(typeof(RibbonGalleryItem));

        /// <summary>
        ///     Control background brush used in a "hover" state of the RibbonGalleryItem.
        /// </summary>
        public Brush MouseOverBackground
        {
            get { return RibbonControlService.GetMouseOverBackground(this); }
            set { RibbonControlService.SetMouseOverBackground(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for PressedBorderBrush property.
        /// </summary>
        public static readonly DependencyProperty PressedBorderBrushProperty =
            RibbonControlService.PressedBorderBrushProperty.AddOwner(typeof(RibbonGalleryItem));

        /// <summary>
        ///     Outer border brush used in a "pressed" state of the RibbonGalleryItem.
        /// </summary>
        public Brush PressedBorderBrush
        {
            get { return RibbonControlService.GetPressedBorderBrush(this); }
            set { RibbonControlService.SetPressedBorderBrush(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for PressedBackground property.
        /// </summary>
        public static readonly DependencyProperty PressedBackgroundProperty =
            RibbonControlService.PressedBackgroundProperty.AddOwner(typeof(RibbonGalleryItem));

        /// <summary>
        ///     Control background brush used in a "pressed" state of the RibbonGalleryItem.
        /// </summary>
        public Brush PressedBackground
        {
            get { return RibbonControlService.GetPressedBackground(this); }
            set { RibbonControlService.SetPressedBackground(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for CheckedBackground property.
        /// </summary>
        public static readonly DependencyProperty CheckedBackgroundProperty =
            RibbonControlService.CheckedBackgroundProperty.AddOwner(typeof(RibbonGalleryItem));

        /// <summary>
        ///     Control background brush used in a "Checked" state of the RibbonGalleryItem.
        /// </summary>
        public Brush CheckedBackground
        {
            get { return RibbonControlService.GetCheckedBackground(this); }
            set { RibbonControlService.SetCheckedBackground(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for CheckedBorderBrush property.
        /// </summary>
        public static readonly DependencyProperty CheckedBorderBrushProperty =
            RibbonControlService.CheckedBorderBrushProperty.AddOwner(typeof(RibbonGalleryItem));

        /// <summary>
        ///     Control border brush used to paint a "Checked" RibbonGalleryItem.
        /// </summary>
        public Brush CheckedBorderBrush
        {
            get { return RibbonControlService.GetCheckedBorderBrush(this); }
            set { RibbonControlService.SetCheckedBorderBrush(this, value); }
        }

        #endregion VisualStates

        #region RibbonControlService Properties
        
        /// <summary>
        ///     DependencyProperty for ToolTipTitle property.
        /// </summary>
        public static readonly DependencyProperty ToolTipTitleProperty =
            RibbonControlService.ToolTipTitleProperty.AddOwner(typeof(RibbonGalleryItem), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

        /// <summary>
        ///     Title text for the tooltip of the control.
        /// </summary>
        public string ToolTipTitle
        {
            get { return RibbonControlService.GetToolTipTitle(this); }
            set { RibbonControlService.SetToolTipTitle(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for ToolTipDescription property.
        /// </summary>
        public static readonly DependencyProperty ToolTipDescriptionProperty =
            RibbonControlService.ToolTipDescriptionProperty.AddOwner(typeof(RibbonGalleryItem), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

        /// <summary>
        ///     Description text for the tooltip of the control.
        /// </summary>
        public string ToolTipDescription
        {
            get { return RibbonControlService.GetToolTipDescription(this); }
            set { RibbonControlService.SetToolTipDescription(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for ToolTipImageSource property.
        /// </summary>
        public static readonly DependencyProperty ToolTipImageSourceProperty =
            RibbonControlService.ToolTipImageSourceProperty.AddOwner(typeof(RibbonGalleryItem), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

        /// <summary>
        ///     Image source for the tooltip of the control.
        /// </summary>
        public ImageSource ToolTipImageSource
        {
            get { return RibbonControlService.GetToolTipImageSource(this); }
            set { RibbonControlService.SetToolTipImageSource(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for ToolTipFooterTitle property.
        /// </summary>
        public static readonly DependencyProperty ToolTipFooterTitleProperty =
            RibbonControlService.ToolTipFooterTitleProperty.AddOwner(typeof(RibbonGalleryItem), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

        /// <summary>
        ///     Title text for the footer of tooltip of the control.
        /// </summary>
        public string ToolTipFooterTitle
        {
            get { return RibbonControlService.GetToolTipFooterTitle(this); }
            set { RibbonControlService.SetToolTipFooterTitle(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for ToolTipFooterDescription property.
        /// </summary>
        public static readonly DependencyProperty ToolTipFooterDescriptionProperty =
            RibbonControlService.ToolTipFooterDescriptionProperty.AddOwner(typeof(RibbonGalleryItem), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

        /// <summary>
        ///     Description text for the footer of the tooltip of the control.
        /// </summary>
        public string ToolTipFooterDescription
        {
            get { return RibbonControlService.GetToolTipFooterDescription(this); }
            set { RibbonControlService.SetToolTipFooterDescription(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for ToolTipFooterImageSource property.
        /// </summary>
        public static readonly DependencyProperty ToolTipFooterImageSourceProperty =
            RibbonControlService.ToolTipFooterImageSourceProperty.AddOwner(typeof(RibbonGalleryItem), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

        /// <summary>
        ///     Image source for the footer of the tooltip of the control.
        /// </summary>
        public ImageSource ToolTipFooterImageSource
        {
            get { return RibbonControlService.GetToolTipFooterImageSource(this); }
            set { RibbonControlService.SetToolTipFooterImageSource(this, value); }
        }

        #endregion RibbonControlService Properties

        #region Private Data

        private enum Bits
        {
            AreKeyTipAndContentInSync = 0x01,
            IsKeyTipSyncSource = 0x02,
            SyncingKeyTipAndContent = 0x04
        }
        private BitVector32 _bits = new BitVector32(0);

        bool ISyncKeyTipAndContent.KeepKeyTipAndContentInSync
        {
            get { return _bits[(int)Bits.AreKeyTipAndContentInSync]; }
            set { _bits[(int)Bits.AreKeyTipAndContentInSync] = value; }
        }

        bool ISyncKeyTipAndContent.IsKeyTipSyncSource
        {
            get { return _bits[(int)Bits.IsKeyTipSyncSource]; }
            set { _bits[(int)Bits.IsKeyTipSyncSource] = value; }
        }

        bool ISyncKeyTipAndContent.SyncingKeyTipAndContent
        {
            get { return _bits[(int)Bits.SyncingKeyTipAndContent]; }
            set { _bits[(int)Bits.SyncingKeyTipAndContent] = value; }
        }

        #endregion

        #region KeyTips

        /// <summary>
        ///     DependencyProperty for KeyTip property.
        /// </summary>
        public static readonly DependencyProperty KeyTipProperty =
            KeyTipService.KeyTipProperty.AddOwner(typeof(RibbonGalleryItem), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnKeyTipChanged), new CoerceValueCallback(CoerceKeyTip)));

        /// <summary>
        ///     KeyTip string for the control.
        /// </summary>
        public string KeyTip
        {
            get { return KeyTipService.GetKeyTip(this); }
            set { KeyTipService.SetKeyTip(this, value); }
        }

        internal void SyncKeyTipAndContent()
        {
            KeyTipAndContentSyncHelper.Sync(this, ContentProperty);
        }

        private static void OnKeyTipChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            KeyTipAndContentSyncHelper.OnKeyTipChanged((ISyncKeyTipAndContent)d, ContentProperty);
        }

        private static object CoerceKeyTip(DependencyObject d, object baseValue)
        {
            return KeyTipAndContentSyncHelper.CoerceKeyTip((ISyncKeyTipAndContent)d, baseValue, ContentProperty);
        }

        private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            KeyTipAndContentSyncHelper.OnContentPropertyChanged((ISyncKeyTipAndContent)d, ContentProperty);
        }

        private static object CoerceContent(DependencyObject d, object baseValue)
        {
            return KeyTipAndContentSyncHelper.CoerceContentProperty((ISyncKeyTipAndContent)d, baseValue);
        }

        private static void OnActivatingKeyTipThunk(object sender, ActivatingKeyTipEventArgs e)
        {
            ((RibbonGalleryItem)sender).OnActivatingKeyTip(e);
        }

        protected virtual void OnActivatingKeyTip(ActivatingKeyTipEventArgs e)
        {
            if (e.OriginalSource == this)
            {
                e.KeyTipHorizontalPlacement = KeyTipHorizontalPlacement.KeyTipCenterAtTargetCenter;
                e.KeyTipVerticalPlacement = KeyTipVerticalPlacement.KeyTipCenterAtTargetCenter;
                e.KeyTipHorizontalOffset = e.KeyTipVerticalOffset = 0;
            }
        }

        private static void OnKeyTipAccessedThunk(object sender, KeyTipAccessedEventArgs e)
        {
            ((RibbonGalleryItem)sender).OnKeyTipAccessed(e);
        }

        protected virtual void OnKeyTipAccessed(KeyTipAccessedEventArgs e)
        {
            if (e.OriginalSource == this)
            {
                SetSelectedOnInput();
                e.Handled = true;
            }
        }

        #endregion KeyTips
    }
}
