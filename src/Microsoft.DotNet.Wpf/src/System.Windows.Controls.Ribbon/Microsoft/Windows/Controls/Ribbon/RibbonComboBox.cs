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
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Automation.Peers;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Shapes;
    using System.Windows.Threading;
#if RIBBON_IN_FRAMEWORK
    using Microsoft.Windows.Controls;
    using MS.Internal;
#else
    using Microsoft.Windows.Automation.Peers;
#endif

    #endregion

    /// <summary>
    ///   A ComboBox which can host a RibbonGallery and RibbonMenuItems.
    ///   RibbonComboBox displays selected Text only for first occurrence of a RibbonGallery.
    /// </summary>
    public class RibbonComboBox : RibbonMenuButton
    {
        #region Constructors

        /// <summary>
        ///   Initializes static members of the RibbonComboBox class.  Here we override the
        ///   default style, a coerce callback, and allow tooltips to be shown for disabled controls.
        /// </summary>
        static RibbonComboBox()
        {
            Type ownerType = typeof(RibbonComboBox);

            DefaultStyleKeyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(ownerType));
            IsTextSearchEnabledProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(true));
            InitializeStringContentTemplate();
        }

        private static void InitializeStringContentTemplate()
        {
            DataTemplate template;
            FrameworkElementFactory text;

            // Default template for strings
            template = new DataTemplate();
            text = new FrameworkElementFactory(typeof(TextBlock));
            text.SetValue(TextBlock.TextProperty, new TemplateBindingExtension(ContentPresenter.ContentProperty));
            template.VisualTree = text;
            template.Seal();
            s_StringTemplate = template;
        }

        #endregion

        #region ComboBox Properties

        /// <summary>
        /// DependencyProperty for IsEditable
        /// </summary>
        public static readonly DependencyProperty IsEditableProperty =
                ComboBox.IsEditableProperty.AddOwner(typeof(RibbonComboBox),
                new FrameworkPropertyMetadata(false,
                    new PropertyChangedCallback(OnIsEditableChanged)));

        /// <summary>
        ///     True if this ComboBox is editable.
        /// </summary>
        /// <value></value>
        public bool IsEditable
        {
            get { return (bool)GetValue(IsEditableProperty); }
            set { SetValue(IsEditableProperty, value); }
        }

        private static void OnIsEditableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonComboBox cb = d as RibbonComboBox;
            cb.Update();
        }

        /// <summary>
        ///     DependencyProperty for Text
        /// </summary>
        public static readonly DependencyProperty TextProperty =
                ComboBox.TextProperty.AddOwner(typeof(RibbonComboBox),
                        new FrameworkPropertyMetadata(
                                String.Empty,
                                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Journal,
                                new PropertyChangedCallback(OnTextChanged)));

        /// <summary>
        ///     The text of the currently selected item.  When there is no SelectedItem and IsEditable is true
        ///     this is the text entered in the text box.  When IsEditable is false, this value represent the string version of the selected item.
        /// </summary>
        /// <value></value>
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        // When the Text Property changes, search for an item exactly
        // matching the new text and set the selected index to that item
        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonComboBox cb = (RibbonComboBox)d;

            RibbonComboBoxAutomationPeer peer = UIElementAutomationPeer.FromElement(cb) as RibbonComboBoxAutomationPeer;
            // Raise the propetyChangeEvent for Value if Automation Peer exist, the new Value must
            // be the one in SelctionBoxItem(selected value is the one user will care about)
            if (peer != null)
                peer.RaiseValuePropertyChangedEvent((string)e.OldValue, (string)e.NewValue);


            cb.TextUpdated((string)e.NewValue, false);

        }

        /// <summary>
        ///     DependencyProperty for the IsReadOnlyProperty
        /// </summary>
        public static readonly DependencyProperty IsReadOnlyProperty =
                TextBox.IsReadOnlyProperty.AddOwner(typeof(RibbonComboBox));

        /// <summary>
        ///     When the ComboBox is Editable, if the TextBox within it is read only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for ShowKeyboardCues property.
        /// </summary>
        public static readonly DependencyProperty ShowKeyboardCuesProperty =
            RibbonControlService.ShowKeyboardCuesProperty.AddOwner(typeof(RibbonComboBox));

        /// <summary>
        ///     This property is used to decide when to show the Keyboard FocusVisual.
        /// </summary>
        public bool ShowKeyboardCues
        {
            get { return RibbonControlService.GetShowKeyboardCues(this); }
        }

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            ReevalutateFocusVisual();

            // If we're an editable combobox, forward focus to the TextBox element
            if (!e.Handled)
            {
                if (IsEditable && EditableTextBoxSite != null)
                {
                    RetainFocusOnEscape = RibbonHelper.IsKeyboardMostRecentInputDevice();

                    if (e.OriginalSource == this)
                    {
                        EditableTextBoxSite.Focus();
                        e.Handled = true;
                    }
                    else if (e.OriginalSource == EditableTextBoxSite)
                    {
                        EditableTextBoxSite.SelectAll();
                    }
                }
            }

            base.OnGotKeyboardFocus(e);
        }

        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnLostKeyboardFocus(e);
            ReevalutateFocusVisual();
        }

        private void ReevalutateFocusVisual()
        {
            bool enable = true;
            if (IsDropDownOpen)
            {
                enable = false;
            }
            else if (!(IsKeyboardFocused ||
                (EditableTextBoxSite != null && EditableTextBoxSite.IsKeyboardFocused) ||
                (PartToggleButton != null && PartToggleButton.IsKeyboardFocused)))
            {
                enable = false;
            }
            if (enable)
            {
                RibbonHelper.EnableFocusVisual(this);
            }
            else
            {
                RibbonHelper.DisableFocusVisual(this);
            }
        }

        /// <summary>
        ///     DependencyProperty for TextBoxWidth property.
        /// </summary>
        public static readonly DependencyProperty SelectionBoxWidthProperty =
            DependencyProperty.Register(
                    "SelectionBoxWidth",
                    typeof(double),
                    typeof(RibbonComboBox),
                    new FrameworkPropertyMetadata(0.0d));


        /// <summary>
        ///   Gets or sets the width of the text box (excluding Image and Label).
        /// </summary>
        public double SelectionBoxWidth
        {
            get { return (double)GetValue(SelectionBoxWidthProperty); }
            set { SetValue(SelectionBoxWidthProperty, value); }
        }


        private static readonly DependencyPropertyKey SelectionBoxItemPropertyKey =
            DependencyProperty.RegisterReadOnly("SelectionBoxItem", typeof(object), typeof(RibbonComboBox),
                                                new FrameworkPropertyMetadata(String.Empty));

        // This property is used as a Style Helper.
        // When the SelectedItem is a UIElement a VisualBrush is created and set to the Fill property
        // of a Rectangle. Then we set SelectionBoxItem to that rectangle.
        // For data items, SelectionBoxItem is set to a string.
        /// <summary>
        /// The DependencyProperty for the SelectionBoxItemProperty
        /// </summary>
        public static readonly DependencyProperty SelectionBoxItemProperty = SelectionBoxItemPropertyKey.DependencyProperty;

        /// <summary>
        /// Used to display the selected item
        /// </summary>
        public object SelectionBoxItem
        {
            get { return GetValue(SelectionBoxItemProperty); }
            private set { SetValue(SelectionBoxItemPropertyKey, value); }
        }

        private static readonly DependencyPropertyKey SelectionBoxItemTemplatePropertyKey =
            DependencyProperty.RegisterReadOnly("SelectionBoxItemTemplate", typeof(DataTemplate), typeof(RibbonComboBox),
                                                new FrameworkPropertyMetadata((DataTemplate)null));

        /// <summary>
        /// The DependencyProperty for the SelectionBoxItemTemplate Property
        /// </summary>
        public static readonly DependencyProperty SelectionBoxItemTemplateProperty = SelectionBoxItemTemplatePropertyKey.DependencyProperty;

        /// <summary>
        /// Used to get the item DataTemplate
        /// </summary>
        public DataTemplate SelectionBoxItemTemplate
        {
            get { return (DataTemplate)GetValue(SelectionBoxItemTemplateProperty); }
            private set { SetValue(SelectionBoxItemTemplatePropertyKey, value); }
        }

        private static readonly DependencyPropertyKey SelectionBoxItemTemplateSelectorPropertyKey =
           DependencyProperty.RegisterReadOnly("SelectionBoxItemTemplateSelector", typeof(DataTemplateSelector), typeof(RibbonComboBox),
                                               new FrameworkPropertyMetadata((DataTemplateSelector)null));

        /// <summary>
        /// The DependencyProperty for the SelectionBoxItemTemplateSelector Property
        /// </summary>
        public static readonly DependencyProperty SelectionBoxItemTemplateSelectorProperty = SelectionBoxItemTemplateSelectorPropertyKey.DependencyProperty;

        /// <summary>
        /// Used to get the ItemTemplateSelector
        /// </summary>
        public DataTemplateSelector SelectionBoxItemTemplateSelector
        {
            get { return (DataTemplateSelector)GetValue(SelectionBoxItemTemplateSelectorProperty); }
            private set { SetValue(SelectionBoxItemTemplateSelectorPropertyKey, value); }
        }

        private static readonly DependencyPropertyKey SelectionBoxItemStringFormatPropertyKey =
            DependencyProperty.RegisterReadOnly("SelectionBoxItemStringFormat", typeof(String), typeof(RibbonComboBox),
                                                new FrameworkPropertyMetadata((String)null));

        /// <summary>
        /// The DependencyProperty for the SelectionBoxItemProperty
        /// </summary>
        public static readonly DependencyProperty SelectionBoxItemStringFormatProperty = SelectionBoxItemStringFormatPropertyKey.DependencyProperty;

        /// <summary>
        /// Used to set the item DataStringFormat
        /// </summary>
        public String SelectionBoxItemStringFormat
        {
            get { return (String)GetValue(SelectionBoxItemStringFormatProperty); }
            private set { SetValue(SelectionBoxItemStringFormatPropertyKey, value); }
        }

        /// <summary>
        ///     DependencyProperty for StaysOpenOnEdit
        /// </summary>
        public static readonly DependencyProperty StaysOpenOnEditProperty
            = ComboBox.StaysOpenOnEditProperty.AddOwner(typeof(RibbonComboBox),
                                          new FrameworkPropertyMetadata(false));

        /// <summary>
        ///     Determines whether the ComboBox will remain open when clicking on
        ///     the text box when the drop down is open
        /// </summary>
        /// <value></value>
        public bool StaysOpenOnEdit
        {
            get
            {
                return (bool)GetValue(StaysOpenOnEditProperty);
            }
            set
            {
                SetValue(StaysOpenOnEditProperty, value);
            }
        }

        #endregion

        #region Selector Properties

        /// <summary>
        ///  The first item in the current selection, or null if the selection is empty.
        /// </summary>
        private object SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (_firstGallery != null && !UpdatingSelectedItem)
                {
                    _firstGallery.SelectedItem = value;
                }
                _selectedItem = value;
            }
        }

        private object HighlightedItem
        {
            get
            {
                if (_firstGallery != null)
                {
                    return _firstGallery.HighlightedItem;
                }
                return null;
            }

            set
            {
                if (_firstGallery != null)
                {
                    _firstGallery.HighlightedItem = value;
                }
            }
        }

        // When the selected item (or its content) changes, update
        // The SelectedItem property and the Text properties
        // ComboBoxItem also calls this method when its content changes
        private void SelectedItemUpdated()
        {
            try
            {
                UpdatingSelectedItem = true;

                // If the selection changed as a result of Text or the TextBox
                // changing, don't update the Text property - TextUpdated() will
                if (!UpdatingText)
                {
                    string text = String.Empty;
                    if (_firstGallery != null && _firstGallery.SelectedCategory != null)
                    {
                        text = TextSearchInternal.GetPrimaryTextFromItem(_firstGallery.SelectedCategory, SelectedItem, true);
                        _firstGallery.ScrollIntoView(SelectedItem);
                    }

                    // if _firstGallery.SelectedCategory is null, it means SelectedItem is null and we should set Text to empty string.
                    if (Text != text)
                    {
                        SetValue(TextProperty, text);
                    }
                }

                // Update SelectionItem/TextBox
                Update();
            }
            finally
            {
                UpdatingSelectedItem = false;
            }
        }

        // When the highlighted item changes, update text properties
        private void HighlightedItemUpdated()
        {
            try
            {
                UpdatingHighlightedItem = true;

                // If the highlight changed as a result of Text or the TextBox
                // changing, don't update the Text property - TextUpdated() will
                if (!UpdatingText)
                {
                    string text = String.Empty;
                    if (_firstGallery != null && _firstGallery.HighlightedCategory != null)
                    {
                        text = TextSearchInternal.GetPrimaryTextFromItem(_firstGallery.HighlightedCategory, HighlightedItem, true);
                        _firstGallery.ScrollIntoView(HighlightedItem);
                    }

                    // if _firstGallery.HighlightedCategory is null, it means HighlightedItem is null and we should set Text to empty string.
                    if (Text != text)
                    {
                        SetValue(TextProperty, text);
                    }
                }

                // Update SelectionItem/TextBox
                Update();
            }
            finally
            {
                UpdatingHighlightedItem = false;
            }
        }

        // Updates:
        //    SelectionBox if not editable
        //    EditableTextBox.Text if editable
        private void Update()
        {
            if (IsEditable)
            {
                UpdateEditableTextBox();
            }
            else
            {
                UpdateSelectionBoxItem();
            }
        }

        // Update the editable TextBox to match combobox text
        private void UpdateEditableTextBox()
        {
            if (!UpdatingText)
            {
                try
                {
                    UpdatingText = true;

                    string text = Text;

                    // Copy ComboBox.Text to the editable TextBox
                    if (EditableTextBoxSite != null && EditableTextBoxSite.Text != text)
                    {
                        EditableTextBoxSite.Text = text;
                        EditableTextBoxSite.SelectAll();
                    }
                }
                finally
                {
                    UpdatingText = false;
                }
            }
        }

        /// <summary>
        /// This function updates the selected item in the "selection box".
        /// This is called when selection changes or when the combobox
        /// switches from editable to non-editable or vice versa.
        /// This will also get called in ApplyTemplate in case selection
        /// is set prior to the control being measured.
        /// </summary>
        private void UpdateSelectionBoxItem()
        {
            // propagate the new selected item to the SelectionBoxItem property;
            // this displays it in the selection box
            object item = HighlightedItem;
            bool isHighlightedItem = (item != null);
            item = isHighlightedItem ? item : SelectedItem;
            DataTemplate itemTemplate = null;
            string stringFormat = null;
            DataTemplateSelector itemTemplateSelector = null;

            if (_firstGallery != null)
            {
                RibbonGalleryCategory category = isHighlightedItem ? _firstGallery.HighlightedCategory : _firstGallery.SelectedCategory;
                if (category != null)
                {
                    itemTemplate = category.ItemTemplate;
                    stringFormat = category.ItemStringFormat;
                    itemTemplateSelector = category.ItemTemplateSelector;
                }
            }

            // if Items contains an explicit ContentControl, use its content instead
            // (this handles the case of ComboBoxItem)
            ContentControl contentControl = item as ContentControl;

            if (contentControl != null)
            {
                item = contentControl.Content;
                itemTemplate = contentControl.ContentTemplate;
                stringFormat = contentControl.ContentStringFormat;
            }

            if (_clonedElement != null)
            {
                _clonedElement.LayoutUpdated -= CloneLayoutUpdated;
                _clonedElement = null;
            }

            if (itemTemplate == null && itemTemplateSelector == null && stringFormat == null)
            {
                // if the item is a logical element it cannot be displayed directly in
                // the selection box because it already belongs to the tree (in the dropdown box).
                // Instead, try to extract some useful text from the visual.
                DependencyObject logicalElement = item as DependencyObject;

                if (logicalElement != null)
                {
                    // If the item is a UIElement, create a copy using a visual brush
                    _clonedElement = logicalElement as UIElement;

                    if (_clonedElement != null)
                    {
                        // Create visual copy of selected element
                        VisualBrush visualBrush = new VisualBrush(_clonedElement);
                        visualBrush.Stretch = Stretch.None;

                        //Set position and dimension of content
                        visualBrush.ViewboxUnits = BrushMappingMode.Absolute;
                        visualBrush.Viewbox = new Rect(_clonedElement.RenderSize);

                        //Set position and dimension of tile
                        visualBrush.ViewportUnits = BrushMappingMode.Absolute;
                        visualBrush.Viewport = new Rect(_clonedElement.RenderSize);

                        // If the FlowDirection on cloned element doesn't match the combobox's apply a mirror
                        // If the FlowDirection on cloned element doesn't match its parent's apply a mirror
                        // If both are true, they cancel out so no mirror should be applied
                        FlowDirection elementFD = (FlowDirection)_clonedElement.GetValue(FlowDirectionProperty);
                        DependencyObject parent = VisualTreeHelper.GetParent(_clonedElement);
                        FlowDirection parentFD = parent == null ? FlowDirection : (FlowDirection)parent.GetValue(FlowDirectionProperty);
                        if ((elementFD != this.FlowDirection) != (elementFD != parentFD))
                        {
                            visualBrush.Transform = new MatrixTransform(new Matrix(-1.0, 0.0, 0.0, 1.0, _clonedElement.RenderSize.Width, 0.0));
                        }

                        // Apply visual brush to a rectangle
                        Rectangle rect = new Rectangle();
                        rect.Fill = visualBrush;
                        rect.Width = _clonedElement.RenderSize.Width;
                        rect.Height = _clonedElement.RenderSize.Height;

                        _clonedElement.LayoutUpdated += CloneLayoutUpdated;

                        item = rect;
                        itemTemplate = null;
                    }
                    else
                    {
                        item = ExtractString(logicalElement);
                        itemTemplate = StringContentTemplate;
                    }
                }
            }

            // display a null item by an empty string
            if (item == null)
            {
                item = String.Empty;
                itemTemplate = StringContentTemplate;
            }

            SelectionBoxItem = item;
            SelectionBoxItemTemplate = itemTemplate;
            SelectionBoxItemTemplateSelector = itemTemplateSelector;
            SelectionBoxItemStringFormat = stringFormat;
        }

        // Update our clone's size to match the actual object's size
        private void CloneLayoutUpdated(object sender, EventArgs e)
        {
            Rectangle rect = (Rectangle)SelectionBoxItem;
            rect.Width = _clonedElement.RenderSize.Width;
            rect.Height = _clonedElement.RenderSize.Height;

            VisualBrush visualBrush = (VisualBrush)rect.Fill;
            visualBrush.Viewbox = new Rect(_clonedElement.RenderSize);
            visualBrush.Viewport = new Rect(_clonedElement.RenderSize);
        }

        // When the user types in the TextBox, search for an item that partially
        // matches the new text and set the selected index to that item
        private void OnEditableTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            Debug.Assert(_editableTextBoxSite == sender);

            if (!IsEditable)
            {
                // Don't do any work if we're not editable.
                return;
            }

            TextUpdated(EditableTextBoxSite.Text, true);
        }

        // When selection changes, save the location of the selection start
        // (ignoring changes during compositions)
        private void OnEditableTextBoxSelectionChanged(object sender, RoutedEventArgs e)
        {
#if RIBBON_IN_FRAMEWORK
            if (!Helper.IsComposing(EditableTextBoxSite))
            {
                _textBoxSelectionStart = EditableTextBoxSite.SelectionStart;
            }
#else
            _textBoxSelectionStart = EditableTextBoxSite.SelectionStart;
#endif
        }

        // If TextSearch is enabled search for an item matching the new text
        // (partial search if user is typing, exact search if setting Text)
        private void TextUpdated(string newText, bool textBoxUpdated)
        {
            // Only process this event if it is coming from someone outside setting Text directly
            if (!UpdatingText && !UpdatingSelectedItem && !UpdatingHighlightedItem)
            {
#if RIBBON_IN_FRAMEWORK
                // if a composition is in progress, wait for it to complete
                if (Helper.IsComposing(EditableTextBoxSite))
                {
                    IsWaitingForTextComposition = true;
                    return;
                }
#endif

                try
                {
                    // Set the updating flags so we don't reenter this function
                    UpdatingText = true;

                    // Try searching for an item matching the new text
                    if (IsTextSearchEnabled && _firstGallery != null)
                    {
#if RIBBON_IN_FRAMEWORK
                        if (_updateTextBoxOperation != null)
                        {
                            // cancel any pending async update of the textbox
                            _updateTextBoxOperation.Abort();
                            _updateTextBoxOperation = null;
                        }
#endif

                        ItemsControl matchedGalleryCategory = null;
                        object matchedItem = TextSearchInternal.FindMatchingPrefix(_firstGallery, newText, true, out matchedGalleryCategory);

                        if (matchedItem != null)
                        {
                            // Allow partial matches when updating textbox
                            if (textBoxUpdated)
                            {
                                int selectionStart = EditableTextBoxSite.SelectionStart;
                                // Perform type search when the selection is at the end
                                // of the textbox and the selection start increased
                                if (selectionStart == newText.Length &&
                                    selectionStart > _textBoxSelectionStart)
                                {
                                    // Replace the currently typed text with the text
                                    // from the matched item
                                    string matchedText = TextSearchInternal.GetPrimaryTextFromItem(matchedGalleryCategory, matchedItem, true);

#if RIBBON_IN_FRAMEWORK
                                     // If there's an IME, do the replacement asynchronously so that
                                     // it doesn't get confused with the IME's undo stack.
                                     MS.Internal.Documents.UndoManager undoManager =
                                         EditableTextBoxSite.TextContainer.UndoManager;
                                     if (undoManager != null &&
                                         undoManager.OpenedUnit != null &&
                                         undoManager.OpenedUnit.GetType() != typeof(TextParentUndoUnit))
                                     {
                                         _updateTextBoxOperation = Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                             new DispatcherOperationCallback(UpdateTextBoxCallback),
                                             new object[] {matchedText, newText} );
                                     }
                                     else
#endif
                                     {
                                         // when there's no IME, do it synchronously
                                         UpdateTextBox(matchedText, newText);
                                     }

                                    // ComboBox's text property should be updated with the matched text
                                    newText = matchedText;
                                }
                            }
                            else //Text Property Set
                            {
                                 //Require exact matches when setting TextProperty
                                string matchedText = TextSearchInternal.GetPrimaryTextFromItem(matchedGalleryCategory, matchedItem, true);
                                if (!String.Equals(newText, matchedText, StringComparison.CurrentCulture))
                                {
                                    // Strings not identical, no match
                                    matchedItem = null;
                                }
                            }

                            if (matchedItem != _firstGallery.HighlightedItem)
                            {
                                // Cache the previously selected item for this session.

                                CacheSelectedItem();

                                try
                                {
                                    // Highlight the newly matched item

                                    _firstGallery.ShouldExecuteCommand = IsDropDownOpen;

                                    bool updated = false;

                                    if (matchedGalleryCategory != null && matchedItem != null)
                                    {
                                        RibbonGalleryItem galleryItem =
                                            matchedGalleryCategory.ItemContainerGenerator.ContainerFromItem(matchedItem) as RibbonGalleryItem;
                                        if (galleryItem != null)
                                        {
                                            updated = true;
                                            galleryItem.IsHighlighted = true;
                                        }
                                    }

                                    if (!updated)
                                    {
                                        _firstGallery.HighlightedItem = matchedItem;
                                    }
                                }
                                finally
                                {
                                    _firstGallery.ShouldExecuteCommand = true;
                                }

                                // Scroll the newly matched item into view

                                _firstGallery.ScrollIntoView(matchedItem);
                            }
                        }
                    }


                    // Update TextProperty when TextBox changes and TextBox when TextProperty changes
                    if (textBoxUpdated)
                    {
                        SetValue(TextProperty, newText);
                    }
                    else if (EditableTextBoxSite != null)
                    {
                        EditableTextBoxSite.Text = newText;
                    }
                }
                finally
                {
                    // Clear the updating flag
                    UpdatingText = false;
                }
            }
        }

#if RIBBON_IN_FRAMEWORK
        // When the IME composition we're waiting for completes, run the text search logic
        private void OnEditableTextBoxPreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (IsWaitingForTextComposition &&
                e.TextComposition.Source == EditableTextBoxSite &&
                e.TextComposition.Stage == System.Windows.Input.TextCompositionStage.Done)
            {
                IsWaitingForTextComposition = false;
                TextUpdated(EditableTextBoxSite.Text, true);

                // ComboBox.Text has just changed, but EditableTextBoxSite.Text hasn't.
                // As a courtesy to apps and controls that expect a TextBox.TextChanged
                // event after ComboTox.Text changes, raise such an event now.
                // (A notable example is TFS's WpfFieldControl - see Dev11 964048)
                EditableTextBoxSite.RaiseCourtesyTextChangedEvent();
            }
        }

        object UpdateTextBoxCallback(object arg)
        {
            _updateTextBoxOperation = null;

            object[] args = (object[])arg;
            string matchedText = (string)args[0];
            string newText = (string)args[1];

            try
            {
                UpdatingText = true;
                UpdateTextBox(matchedText, newText);
            }
            finally
            {
                UpdatingText = false;
            }

            return null;
        }

        void UpdateTextBox(string matchedText, string newText)
        {
            // Replace the TextBox's text with the matched text and
            // select the text beyond what the user typed
            EditableTextBoxSite.Text = matchedText;
            EditableTextBoxSite.SelectionStart = newText.Length;
            EditableTextBoxSite.SelectionLength = matchedText.Length - newText.Length;
        }
#endif

        private static string ExtractString(DependencyObject d)
        {
            TextBlock text;
            Visual visual;
            TextElement textElement;
            string strValue = String.Empty;

            if ((text = d as TextBlock) != null)
            {
                strValue = text.Text;
            }
            else if ((visual = d as Visual) != null)
            {
                int count = VisualTreeHelper.GetChildrenCount(visual);
                for (int i = 0; i < count; i++)
                {
                    strValue += ExtractString((DependencyObject)(VisualTreeHelper.GetChild(visual, i)));
                }
            }
            else if ((textElement = d as TextElement) != null)
            {
                strValue += textElement.ContentStart.GetTextInRun(LogicalDirection.Forward);
            }

            return strValue;
        }

        internal override void OnIsDropDownOpenChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnIsDropDownOpenChanged(e);

            ReevalutateFocusVisual();
            if ((bool)e.NewValue)
            {
                // Select text if editable
                if (IsEditable && EditableTextBoxSite != null)
                {
                    EditableTextBoxSite.Focus();
                    EditableTextBoxSite.SelectAll();
                }

                // Cache the previously selected item for this session
                CacheSelectedItem();

                Dispatcher.BeginInvoke((Action)delegate()
                {
                    if (_firstGallery != null)
                    {
                        // Scroll the highlighted item into view. Note that we need to do the
                        // scroll in a Dispatcher operation because the scroll operation wont
                        // succeed until the Popup contents are Loaded and connected to a
                        // PresentationSource. We need to allow time for that to happen.

                        _firstGallery.ScrollIntoView(_firstGallery.HighlightedItem);
                    }
                },
                DispatcherPriority.Render);
            }
        }

        private object SelectedValue
        {
            get { return _selectedValue; }
            set { _selectedValue = value; }
        }

        private string SelectedValuePath
        {
            get { return _selectedValuePath; }
            set { _selectedValuePath = value; }
        }

        #endregion

        #region Protected override Methods

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new RibbonComboBoxAutomationPeer(this);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Handled)
            {
                return;
            }

            bool handled = false;
            if ((e.Key == Key.Escape) &&
                IsSelectedItemCached &&
                (IsDropDownOpen || IsEditable))
            {
                // Cancel changes of escape
                if (IsEditable && !IsDropDownOpen)
                {
                    handled = true;
                }
                CommitOrCancelChanges(false /* cancelChanges */);
            }

            if ((e.Key == Key.F4) &&
                IsSelectedItemCached &&
                IsDropDownOpen)
            {
                // Cancel changes on F4
                CommitOrCancelChanges(false /* cancelChanges */);
            }

            UIElement targetFocusOnFalse = null;
            if (IsEditable)
            {
                targetFocusOnFalse = EditableTextBoxSite;
            }
            else if (RetainFocusOnEscape)
            {
                targetFocusOnFalse = PartToggleButton;
            }
            RibbonHelper.HandleDropDownKeyDown(this,
                e,
                delegate() { return IsDropDownOpen; },
                delegate(bool value) { IsDropDownOpen = value; },
                targetFocusOnFalse,
                Popup.TryGetChild());

            if (e.Handled)
            {
                return;
            }

            Key key = e.Key;
            if (key == Key.System)
            {
                key = e.SystemKey;
            }

            switch (key)
            {
                case Key.Enter:
                case Key.F10:
                    if (IsEditable || IsDropDownOpen)
                    {
                        // Commit changes on Enter or F10.
                        CommitOrCancelChanges(true /* commitChanges */);

                        // Dismiss parent Popups
                        RaiseEvent(new RibbonDismissPopupEventArgs());

                        handled = true;
                    }
                    break;
            }

            if (handled)
            {
                e.Handled = true;
            }

            base.OnKeyDown(e);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            // Handles the navigation scenarios for the first gallery.
            if (NavigateInFirstGallery)
            {
                bool handled = false;
                Key key = e.Key;
                if (FlowDirection == FlowDirection.RightToLeft)
                {
                    // In Right to Left mode we switch Right and Left keys
                    if (key == Key.Left)
                    {
                        key = Key.Right;
                    }
                    else if (key == Key.Right)
                    {
                        key = Key.Left;
                    }
                }

                // Note that Keyboard focus is never on the first gallery (except for its filter
                // area). In any other region of first gallery gets focus it is assumed that the
                // focus is delegate back to non-popup parts of combobox. Hence here we handle only
                // the cases where focus is in non-popup parts of combobox. For other regions we assume
                // that their default keyboard navigation will kick in.
                RibbonGalleryItem focusedGalleryItem = null;
                if (IsDropDownOpen && _firstGallery != null)
                {
                    focusedGalleryItem = _firstGallery.HighlightedContainer;
                    if (focusedGalleryItem == null && _firstGallery.SelectedContainers.Count > 0)
                    {
                        focusedGalleryItem = _firstGallery.SelectedContainers[0];
                    }
                }

                switch (key)
                {
                    case Key.Tab:
                        if (IsDropDownOpen)
                        {
                            bool shiftPressed = ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift);
                            if (focusedGalleryItem == null)
                            {
                                // Move focus to the beggining or the end of popup.
                                if (shiftPressed)
                                {
                                    handled = RibbonHelper.NavigateToPreviousMenuItemOrGallery(this, Items.Count, BringIndexIntoView);
                                }
                                else
                                {
                                    handled = RibbonHelper.NavigateToNextMenuItemOrGallery(this, -1, BringIndexIntoView);
                                }
                            }
                            if (!handled && focusedGalleryItem != null)
                            {
                                // Move focus to the next/previous element.
                                focusedGalleryItem.MoveFocus(new TraversalRequest(shiftPressed ? FocusNavigationDirection.Previous : FocusNavigationDirection.Next));
                                handled = true;
                            }
                        }
                        break;
                    case Key.Up:
                        if (IsDropDownOpen)
                        {
                            if (focusedGalleryItem == null)
                            {
                                // Navigate to the bottom of the popup.
                                handled = RibbonHelper.NavigateToPreviousMenuItemOrGallery(this, Items.Count, BringIndexIntoView);
                            }
                            if (!handled)
                            {
                                // Navigate and highlight the gallery item above the current.
                                handled = RibbonHelper.NavigateAndHighlightGalleryItem(focusedGalleryItem, FocusNavigationDirection.Up);
                            }
                            if (!handled)
                            {
                                // Navigate outside the first gallery.
                                _firstGallery.OnNavigationKeyDown(e, focusedGalleryItem);
                            }
                            handled = true;
                        }
                        break;

                    case Key.Down:
                        {
                            if (!IsDropDownOpen && IsEditable)
                            {
                                // Open drop down
                                IsDropDownOpen = true;
                                handled = true;
                            }
                            else if (IsDropDownOpen)
                            {
                                if (focusedGalleryItem == null)
                                {
                                    // Navigate to the top of the popup.
                                    handled = RibbonHelper.NavigateToNextMenuItemOrGallery(this, -1, BringIndexIntoView);
                                }
                                if (!handled)
                                {
                                    // Navigate and highlight the gallery item below the current.
                                    handled = RibbonHelper.NavigateAndHighlightGalleryItem(focusedGalleryItem, FocusNavigationDirection.Down);
                                }
                                if (!handled)
                                {
                                    // Navigate outside the gallery.
                                    _firstGallery.OnNavigationKeyDown(e, focusedGalleryItem);
                                }
                                handled = true;
                            }
                        }
                        break;

                    case Key.Right:
                        if (IsDropDownOpen)
                        {
                            if (!IsEditable)
                            {
                                // Navigate and highlight the gallery item to the right.
                                RibbonHelper.NavigateAndHighlightGalleryItem(focusedGalleryItem, FocusNavigationDirection.Right);
                                handled = true;
                            }
                        }
                        break;

                    case Key.Left:
                        if (IsDropDownOpen)
                        {
                            if (!IsEditable)
                            {
                                // Navigate and highlight the gallery item to the left.
                                RibbonHelper.NavigateAndHighlightGalleryItem(focusedGalleryItem, FocusNavigationDirection.Left);
                                handled = true;
                            }
                        }
                        break;

                    case Key.PageUp:
                        if (IsDropDownOpen)
                        {
                            RibbonHelper.NavigatePageAndHighlightRibbonGalleryItem(_firstGallery, focusedGalleryItem, FocusNavigationDirection.Up);
                            handled = true;
                        }
                        break;

                    case Key.PageDown:
                        if (IsDropDownOpen)
                        {
                            RibbonHelper.NavigatePageAndHighlightRibbonGalleryItem(_firstGallery, focusedGalleryItem, FocusNavigationDirection.Down);
                            handled = true;
                        }
                        break;
                }

                if (handled)
                {
                    e.Handled = true;
                    return;
                }
            }
            else
            {
                base.OnPreviewKeyDown(e);
            }
        }

        private bool NavigateInFirstGallery
        {
            get
            {
                // Note that the _firstGallery does not physically take keyboard focus.
                // Hence if either the combobox itself or its editable textbox or its
                // toggle button has focus, then we assume navigation for first gallery.
                return _firstGallery != null &&
                    (this.IsKeyboardFocused ||
                    (EditableTextBoxSite != null && EditableTextBoxSite.IsKeyboardFocused) ||
                    (PartToggleButton != null && PartToggleButton.IsKeyboardFocused));
            }
        }

        private bool IsFocusWithinFirstGalleryItemsHostSite
        {
            get
            {
                if (_firstGallery != null &&
                    _firstGallery.IsKeyboardFocusWithin)
                {
                    Panel galleryItemsHostSite = _firstGallery.ItemsHostSite;
                    if (galleryItemsHostSite != null)
                    {
                        return galleryItemsHostSite.IsKeyboardFocusWithin;
                    }

                    DependencyObject focusedElement = Keyboard.FocusedElement as DependencyObject;
                    if (focusedElement != null &&
                        (TreeHelper.FindVisualAncestor<RibbonGalleryCategory>(focusedElement) != null))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (e.OriginalSource == this ||
                e.OriginalSource == PartToggleButton)
            {
                if (e.Key == Key.Space ||
                    e.Key == Key.Enter)
                {
                    IsDropDownOpen = true;
                    e.Handled = true;
                }
            }

            if (!e.Handled)
            {
                base.OnKeyUp(e);
            }
        }

        protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.OldValue)
            {
                // Whenever focus is moving outside the ComboBox we attempt
                // to commit pending changes. Please note that the ESC key
                // is special and that we would have already cancelled the
                // pending changes during the PreviewKeyDown listener in that
                // case and the current attempt to commit the changes will
                // no-op. For all other cases though we will commit pending
                // changes.
                Dispatcher.BeginInvoke(
                    (Action)delegate()
                    {
                        if (!IsKeyboardFocusWithin)
                        {
                            CommitOrCancelChanges(true /* commitChanges */);
                        }
                    },
                    DispatcherPriority.Normal,
                    null);
            }

            base.OnIsKeyboardFocusWithinChanged(e);
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            if (IsEditable)
            {
                Visual originalSource = e.OriginalSource as Visual;
                Visual textBox = EditableTextBoxSite;

                if (originalSource != null && textBox != null
                    && textBox.IsAncestorOf(originalSource))
                {
                    if (IsDropDownOpen && StaysOpenOnEdit)
                    {
                        // clicking the text box should not close the combobox.
                        // so return without calling base.OnPreviewMouseDown.
                        // dont mark e.Handled because TextBox needs to recieve MouseDown
                        return;
                    }
                    else if (!IsKeyboardFocusWithin)
                    {
                        // If textBox is clicked, claim focus
                        Focus();
                        e.Handled = true;   // Handle so that textbox won't try to update cursor position
                    }
                }
            }
            base.OnPreviewMouseDown(e);
        }

        protected override void OnTextInput(TextCompositionEventArgs e)
        {
            // Only handle text from ourselves or a GalleryItem
            if (!String.IsNullOrEmpty(e.Text) && IsTextSearchEnabled && NavigateInFirstGallery)
            {
                TextSearchInternal instance = TextSearchInternal.EnsureInstance(_firstGallery);

                if (instance != null)
                {
                    instance.DoHierarchicalSearch(e.Text);
                    // Note: we always want to handle the event to denote that we
                    // actually did something.  We wouldn't want an AccessKey
                    // to get invoked just because there wasn't a match here.
                    e.Handled = true;
                }
            }

            // Dont call base as we dont want to have TextSearch on Items other than _firstGallery
            // base.OnTextInput(e);
        }

        public override void OnApplyTemplate()
        {
            CoerceValue(ControlSizeDefinitionProperty);
            base.OnApplyTemplate();

            EditableTextBoxSite = GetTemplateChild(EditableTextBoxTemplateName) as TextBox;

            // EditableTextBoxSite should have been set by now if it's in the visual tree
            if (EditableTextBoxSite != null)
            {
                EditableTextBoxSite.TextChanged += new TextChangedEventHandler(OnEditableTextBoxTextChanged);
                EditableTextBoxSite.SelectionChanged += new RoutedEventHandler(OnEditableTextBoxSelectionChanged);
#if RIBBON_IN_FRAMEWORK
                EditableTextBoxSite.PreviewTextInput += new TextCompositionEventHandler(OnEditableTextBoxPreviewTextInput);
#endif
            }

            // At startup ComboBox needs SelectedItem from its first RibbonGallery in order to populate its TextBox.
            // RibbonGallery needs to be hooked up to the Visual tree, so that its DataContext is inherited.
            // Similarly RibbonGalleryCategory and RibbonGalleryItem should be in Visual tree to extract SelectedItem and the container for the SelectedItem
            // Therefore its not sufficient to just generate containers, we need a full Layout on RibbonGallery.
            if (Popup != null && Popup.Child != null)
            {
                Popup.Child.Measure(new Size());
                UpdateFirstGallery();
            }
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);
            UpdateFirstGallery();
        }

        /// <summary>
        /// Cache Container and item of first occurrence of a RibbonGallery
        /// </summary>
        private void UpdateFirstGallery()
        {
            _firstGalleryItem = null;
            RibbonGallery firstGallery = null;

            foreach(object item in Items)
            {
                firstGallery = ItemContainerGenerator.ContainerFromItem(item) as RibbonGallery;
                if (firstGallery != null)
                {
                    _firstGalleryItem = new WeakReference(item);
                    break;
                }
            }

            FirstGallery = firstGallery;
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            // If a new Gallery container has been generated for _galleryItem, update _gallery reference.
            RibbonGallery gallery = element as RibbonGallery;
            if (gallery != null)
            {
                if (_firstGalleryItem != null && _firstGalleryItem.IsAlive && _firstGalleryItem.Target.Equals(item))
                {
                    FirstGallery = gallery;
                }
            }

            base.PrepareContainerForItemOverride(element, item);
        }

        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            if (_firstGalleryItem != null && _firstGalleryItem.IsAlive && _firstGalleryItem.Target.Equals(item))
            {
                FirstGallery = null;
            }
            base.ClearContainerForItemOverride(element, item);
        }

        internal void UpdateSelectionProperties()
        {
            try
            {
                UpdatingSelectedItem = true;

                // Update selected* properties, SelectedItem could become null if there is no gallery.
                object selectedItem = null, selectedValue = null;
                string selectedValuePath = string.Empty;

                if (_firstGallery != null)
                {
                    selectedItem = _firstGallery.SelectedItem;
                    selectedValue = _firstGallery.SelectedValue;
                    selectedValuePath = _firstGallery.SelectedValuePath;
                }

                SelectedItem = selectedItem;
                SelectedValue = selectedValue;
                SelectedValuePath = selectedValuePath;

                // Update editableTextBox Text with the selectedItem
                SelectedItemUpdated();
            }
            finally
            {
                UpdatingSelectedItem = false;
            }
        }

        void OnGallerySelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            UpdateSelectionProperties();
        }

        void OnGalleryItemSelectionChanged(object sender, RoutedEventArgs e)
        {
            UpdateSelectionProperties();
        }

        void OnGalleryHighlightChanged(object sender, EventArgs e)
        {
            // Note that the _firstGallery does not physically take keyboard focus.

            // Note that IsKeyboardMostRecentInputDevice results in false positives because
            // InputManager.Current.MostRecentInputDevice is only updated upon MouseDown/Up
            // event not during a MouseMove. Thus we end up processing the HighlightItem
            // changes caused via a MouseMove after a key navigation. Hence the additional
            // logic to detect this scenario in RibbonGallery.

            //if (RibbonHelper.IsKeyboardMostRecentInputDevice() && navigateInFirstGallery)
            if (_firstGallery != null &&
                !_firstGallery.HasHighlightChangedViaMouse &&
                (NavigateInFirstGallery || IsFocusWithinFirstGalleryItemsHostSite))
            {
                HighlightedItemUpdated();
            }
        }

        void OnGalleryGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            // When on of the GalleryItems within the _firstGallery acquires Keyboard
            // focus reinstate focus to the parent based on the IsEditable mode

            RibbonGalleryItem focusedGalleryItem = Keyboard.FocusedElement as RibbonGalleryItem;
            if (focusedGalleryItem != null)
            {
                if (IsEditable && EditableTextBoxSite != null)
                {
                    EditableTextBoxSite.Focus();
                }
                else if (!IsEditable && PartToggleButton != null)
                {
                    PartToggleButton.Focus();
                }
                else
                {
                    Focus();
                }
            }
        }

        internal override void TransferPseudoInheritedProperties()
        {
            //base.TransferPseudoInheritedProperties();
        }

        #endregion

        #region Input

        private void CommitOrCancelChanges(bool commitChanges)
        {
            // Close the dropdown and commit the selection if requested.
            if (commitChanges)
            {
                DiscardCachedSelectedItem();
            }
            else
            {
                RestoreCachedSelectedItem();
            }

            SelectedItemUpdated();
        }

        private void CacheSelectedItem()
        {
            if (_firstGallery != null && !IsSelectedItemCached)
            {
                try
                {
                    // Temporarily clear Gallery's selection
                    _firstGallery.ShouldExecuteCommand = false;
                    IsSelectedItemCached = true;
                    _cachedSelectedItem = _firstGallery.SelectedItem;
                    _firstGallery.SelectedItem = null;
                }
                finally
                {
                    _firstGallery.ShouldExecuteCommand = true;
                }

                _firstGallery.HighlightedItem = _cachedSelectedItem;
            }
        }

        private void RestoreCachedSelectedItem()
        {
            if (_firstGallery != null && IsSelectedItemCached)
            {
                try
                {
                    // Restore Gallery's selection

                    _firstGallery.ShouldExecuteCommand = false;
                    SelectedItem = _cachedSelectedItem;
                    _cachedSelectedItem = null;
                    IsSelectedItemCached = false;
                }
                finally
                {
                    _firstGallery.ShouldExecuteCommand = true;
                }

                HighlightedItem = null;
            }
        }

        private void DiscardCachedSelectedItem()
        {
            if (IsSelectedItemCached)
            {
                // Discard cached selection and set selection
                // to be the same as highlight

                if (HighlightedItem != null)
                {
                    SelectedItem = HighlightedItem;
                    HighlightedItem = null;
                }
                else
                {
                    SelectedItem = _cachedSelectedItem;
                }
                _cachedSelectedItem = null;
                IsSelectedItemCached = false;
            }
        }

        #endregion

        #region KeyTips

        protected override void OnActivatingKeyTip(ActivatingKeyTipEventArgs e)
        {
            if (e.OriginalSource == this)
            {
                RibbonHelper.SetKeyTipPlacementForTextBox(this, e, EditableTextBoxSite);
            }
        }

        protected override void OnKeyTipAccessed(KeyTipAccessedEventArgs e)
        {
            if (e.OriginalSource == this)
            {
                if (this.IsEditable)
                {
                    if (EditableTextBoxSite != null)
                    {
                        RibbonHelper.OpenParentRibbonGroupDropDownSync(this, TemplateApplied);
                        EditableTextBoxSite.Focus();
                        EditableTextBoxSite.SelectAll();
                    }
                    e.Handled = true;
                }
                else
                {
                    base.OnKeyTipAccessed(e);
                }
            }
            else
            {
                base.OnKeyTipAccessed(e);
            }
        }

        #endregion

        #region Private Properties

        internal RibbonGallery FirstGallery
        {
            get { return _firstGallery; }
            private set
            {
                if (_firstGallery != null)
                {
                    _firstGallery.ShouldGalleryItemsAcquireFocus = true;

                    _firstGallery.HighlightChanged -= new EventHandler(OnGalleryHighlightChanged);
                    _firstGallery.SelectionChanged -= new RoutedPropertyChangedEventHandler<object>(OnGallerySelectionChanged);
                    _firstGallery.RemoveHandler(RibbonGalleryItem.SelectedEvent, new RoutedEventHandler(OnGalleryItemSelectionChanged));
                    _firstGallery.RemoveHandler(RibbonGalleryItem.UnselectedEvent, new RoutedEventHandler(OnGalleryItemSelectionChanged));
                    _firstGallery.RemoveHandler(Keyboard.GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(OnGalleryGotKeyboardFocus));
                }

                _firstGallery = value;

                if (_firstGallery != null)
                {
                    _firstGallery.ShouldGalleryItemsAcquireFocus = false;

                    _firstGallery.HighlightChanged += new EventHandler(OnGalleryHighlightChanged);
                    _firstGallery.SelectionChanged += new RoutedPropertyChangedEventHandler<object>(OnGallerySelectionChanged);
                    _firstGallery.AddHandler(RibbonGalleryItem.SelectedEvent, new RoutedEventHandler(OnGalleryItemSelectionChanged));
                    _firstGallery.AddHandler(RibbonGalleryItem.UnselectedEvent, new RoutedEventHandler(OnGalleryItemSelectionChanged));
                    _firstGallery.AddHandler(Keyboard.GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(OnGalleryGotKeyboardFocus), true);
                }
                UpdateSelectionProperties();
            }
        }

        internal TextBox EditableTextBoxSite
        {
            get
            {
                return _editableTextBoxSite;
            }
            set
            {
                _editableTextBoxSite = value;
            }
        }

        private static DataTemplate StringContentTemplate
        {
            get { return s_StringTemplate; }
        }

        // Used to indicate that the Text Properties are changing
        // Don't reenter callbacks
        private bool UpdatingText
        {
            get { return _cacheValid[(int)CacheBits.UpdatingText]; }
            set { _cacheValid[(int)CacheBits.UpdatingText] = value; }
        }

        // Selected item is being updated; Don't reenter callbacks
        private bool UpdatingSelectedItem
        {
            get { return _cacheValid[(int)CacheBits.UpdatingSelectedItem]; }
            set { _cacheValid[(int)CacheBits.UpdatingSelectedItem] = value; }
        }

        // Says if the SelectedItem has been temporarily mutated as the text is being updated
        internal bool IsSelectedItemCached
        {
            get { return _cacheValid[(int)CacheBits.IsSelectedItemCached]; }
            private set { _cacheValid[(int)CacheBits.IsSelectedItemCached] = value; }
        }

        // Highlighted item is being updated; Don't reenter callbacks
        private bool UpdatingHighlightedItem
        {
            get { return _cacheValid[(int)CacheBits.UpdatingHighlightedItem]; }
            set { _cacheValid[(int)CacheBits.UpdatingHighlightedItem] = value; }
        }

        // A text composition is active (in the EditableTextBoxSite);  postpone Text changes
        private bool IsWaitingForTextComposition
        {
            get { return _cacheValid[(int)CacheBits.IsWaitingForTextComposition]; }
            set { _cacheValid[(int)CacheBits.IsWaitingForTextComposition] = value; }
        }

        #endregion

        #region Private Data

        private const string EditableTextBoxTemplateName = "PART_EditableTextBox";

        private TextBox _editableTextBoxSite;
        private int _textBoxSelectionStart; // the location of selection before call to TextUpdated.
        private RibbonGallery _firstGallery;
        private WeakReference _firstGalleryItem;
        private BitVector32 _cacheValid = new BitVector32(0);   // Condense boolean bits
        private UIElement _clonedElement;
        private DispatcherOperation _updateTextBoxOperation;
        private static DataTemplate s_StringTemplate;
        private object _selectedItem, _selectedValue, _cachedSelectedItem;
        private string _selectedValuePath;

        private enum CacheBits
        {
            IsMouseOverItemsHost = 0x01,
            HasMouseEnteredItemsHost = 0x02,
            IsContextMenuOpen = 0x04,
            UpdatingText = 0x08,
            UpdatingSelectedItem = 0x10,
            CanExecute = 0x20,
            IsSelectedItemCached = 0x40,
            UpdatingHighlightedItem = 0x80,
            IsWaitingForTextComposition = 0x100,
        }

        #endregion Private Data
    }
}
