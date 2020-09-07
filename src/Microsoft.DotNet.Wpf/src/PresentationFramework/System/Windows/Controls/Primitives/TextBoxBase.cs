// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: The base class for TextBox and RichTextBox.
//

namespace System.Windows.Controls.Primitives
{
    using MS.Internal;
    using System.Threading;
    using System.Collections.ObjectModel;
    using System.ComponentModel; // DefaultValue

    using System.Security;

    using System.Windows.Automation; // TextPattern
    using System.Windows.Automation.Provider; // AutomationProvider
    using System.Windows.Data; // Binding
    using System.Windows.Documents; // TextEditor
    using System.Windows.Input; // MouseButtonEventArgs
    using System.Windows.Markup; // XamlDesignerSerializer

    using MS.Internal.Documents;    // Undo

    using System.Windows.Media; // VisualTreeHelper

    //------------------------------------------------------
    //
    //  Public Enumerations
    //
    //------------------------------------------------------

    #region Public Enumerations

    #endregion Public Enumerations

    /// <summary>
    /// The base class for text editing controls.
    /// </summary>
    [Localizability(LocalizationCategory.Text)]
    [TemplatePart(Name = "PART_ContentHost", Type = typeof(FrameworkElement))]
    public abstract class TextBoxBase : Control
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Static constructor - provides metadata for some properties
        /// </summary>
        static TextBoxBase()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TextBoxBase), new FrameworkPropertyMetadata(typeof(TextBoxBase)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(TextBoxBase));

            // Declaree listener for Padding property
            Control.PaddingProperty.OverrideMetadata(typeof(TextBoxBase),
                new FrameworkPropertyMetadata(new PropertyChangedCallback(OnScrollViewerPropertyChanged)));

            // Listner for InputMethod enabled/disabled property
            // TextEditor needs to set the document manager focus.
            InputMethod.IsInputMethodEnabledProperty.OverrideMetadata(typeof(TextBoxBase),
                new FrameworkPropertyMetadata(new PropertyChangedCallback(OnInputMethodEnabledPropertyChanged)));

            IsEnabledProperty.OverrideMetadata(typeof(TextBoxBase), new UIPropertyMetadata(new PropertyChangedCallback(OnVisualStatePropertyChanged)));
            IsMouseOverPropertyKey.OverrideMetadata(typeof(TextBoxBase), new UIPropertyMetadata(new PropertyChangedCallback(OnVisualStatePropertyChanged)));
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        internal TextBoxBase() : base()
        {
            // Subclass is expected to do three things:
            // a) Register class command handlers
            // b) create TextContainer and call InitializeTextContainer
            // c) configure TextEditor by setting appropriate properties
            CoerceValue(HorizontalScrollBarVisibilityProperty);
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Appends text to the current text of text box
        /// You can use this method to add text to the existing text
        /// in the control instead of using the concatenation operator
        /// (+) to concatenate text to the Text property
        /// </summary>
        /// <param name="textData">
        /// The text to append to the current contents of the text box
        /// </param>
        /// <remarks>
        /// For RichTextBox this method works similar to TextRange.set_Text:
        /// every NewLine combination will insert a new Paragraph element.
        /// </remarks>
        public void AppendText(string textData)
        {
            if (textData == null)
            {
                return;
            }

            TextRange range = new TextRange(_textContainer.End, _textContainer.End);
            range.Text = textData; // Note that in RichTextBox this assignment will convert NewLines into Paragraphs
        }

        /// <summary>
        /// Called when the Template's tree has been generated
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            AttachToVisualTree();
        }

        /// <summary>
        /// Copy the current selection in the text box to the clipboard
        /// </summary>
        public void Copy()
        {
            TextEditorCopyPaste.Copy(this.TextEditor, false);
        }

        /// <summary>
        /// Moves the current selection in the textbox to the clipboard
        /// </summary>
        public void Cut()
        {
            TextEditorCopyPaste.Cut(this.TextEditor, false);
        }

        /// <summary>
        /// Replaces the current selection in the textbox with the contents
        /// of the Clipboard
        /// </summary>
        public void Paste()
        {
            TextEditorCopyPaste.Paste(this.TextEditor);
        }

        /// <summary>
        /// Select all text in the TextBox
        /// </summary>
        public void SelectAll()
        {
            using (this.TextSelectionInternal.DeclareChangeBlock())
            {
                TextSelectionInternal.Select(_textContainer.Start, _textContainer.End);
            }
        }

        /// <summary>
        /// Scroll content by one line to the left.
        /// </summary>
        public void LineLeft()
        {
            if (this.ScrollViewer != null)
            {
                UpdateLayout();
                this.ScrollViewer.LineLeft();
            }
        }

        /// <summary>
        /// Scroll content by one line to the right.
        /// </summary>
        public void LineRight()
        {
            if (this.ScrollViewer != null)
            {
                UpdateLayout();
                this.ScrollViewer.LineRight();
            }
        }

        /// <summary>
        /// Scroll content by one page to the left.
        /// </summary>
        public void PageLeft()
        {
            if (this.ScrollViewer != null)
            {
                UpdateLayout();
                this.ScrollViewer.PageLeft();
            }
        }

        /// <summary>
        /// Scroll content by one page to the right.
        /// </summary>
        public void PageRight()
        {
            if (this.ScrollViewer != null)
            {
                UpdateLayout();
                this.ScrollViewer.PageRight();
            }
        }

        /// <summary>
        /// Scroll content by one line to the top.
        /// </summary>
        public void LineUp()
        {
            UpdateLayout();
            DoLineUp();
        }

        /// <summary>
        /// Scroll content by one line to the bottom.
        /// </summary>
        public void LineDown()
        {
            UpdateLayout();
            DoLineDown();
        }

        /// <summary>
        /// Scroll content by one page to the top.
        /// </summary>
        public void PageUp()
        {
            if (this.ScrollViewer != null)
            {
                UpdateLayout();
                this.ScrollViewer.PageUp();
            }
        }

        /// <summary>
        /// Scroll content by one page to the bottom.
        /// </summary>
        public void PageDown()
        {
            if (this.ScrollViewer != null)
            {
                UpdateLayout();
                this.ScrollViewer.PageDown();
            }
        }

        /// <summary>
        /// Vertically scroll to the beginning of the content.
        /// </summary>
        public void ScrollToHome()
        {
            if (this.ScrollViewer != null)
            {
                UpdateLayout();
                this.ScrollViewer.ScrollToHome();
            }
        }

        /// <summary>
        /// Vertically scroll to the end of the content.
        /// </summary>
        public void ScrollToEnd()
        {
            if (this.ScrollViewer != null)
            {
                UpdateLayout();
                this.ScrollViewer.ScrollToEnd();
            }
        }

        /// <summary>
        /// Scroll horizontally to the specified offset.
        /// </summary>
        public void ScrollToHorizontalOffset(double offset)
        {
            if (Double.IsNaN(offset))
            {
                throw new ArgumentOutOfRangeException("offset");
            }

            if (this.ScrollViewer != null)
            {
                UpdateLayout();
                this.ScrollViewer.ScrollToHorizontalOffset(offset);
            }
        }

        /// <summary>
        /// Scroll vertically to the specified offset.
        /// </summary>
        public void ScrollToVerticalOffset(double offset)
        {
            if (Double.IsNaN(offset))
            {
                throw new ArgumentOutOfRangeException("offset");
            }

            if (this.ScrollViewer != null)
            {
                UpdateLayout();
                this.ScrollViewer.ScrollToVerticalOffset(offset);
            }
        }

        /// <summary>
        /// Undo the most recent undo unit on the stack.
        /// </summary>
        /// <returns>
        /// true if undo succeeds, false otherwise (including when the stack is empty)
        /// </returns>
        public bool Undo()
        {
            UndoManager undoManager = UndoManager.GetUndoManager(this);
            if (undoManager != null && undoManager.UndoCount > undoManager.MinUndoStackCount)
            {
                this.TextEditor.Undo();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Redo the most recent undo unit on the stack.
        /// </summary>
        /// <returns>
        /// true if redo succeeds, false otherwise (including when the stack is empty)
        /// </returns>
        public bool Redo()
        {
            UndoManager undoManager = UndoManager.GetUndoManager(this);
            if (undoManager != null && undoManager.RedoCount > 0)
            {
                this.TextEditor.Redo();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Lock the most recently added undo unit, preventing it from being reopened.
        /// </summary>
        /// <remarks>
        /// This method is intended to be called by an application when a non-text undo unit is added
        /// to the application's own undo stack on top of a text undo unit.  By calling this method,
        /// the next text undo unit will not attempt to merge with the previous one.
        /// </remarks>
        public void LockCurrentUndoUnit()
        {
            UndoManager undoManager = UndoManager.GetUndoManager(this);
            if (undoManager != null)
            {
                // find the deepest open unit, and lock the last unit added to it
                IParentUndoUnit openedUnit = undoManager.OpenedUnit;
                if (openedUnit != null)
                {
                    while (openedUnit.OpenedUnit != null)
                    {
                        openedUnit = openedUnit.OpenedUnit;
                    }
                    if (openedUnit.LastUnit is IParentUndoUnit)
                    {
                        openedUnit.OnNextAdd();
                    }
                }
                else if (undoManager.LastUnit is IParentUndoUnit)
                {
                    ((IParentUndoUnit)undoManager.LastUnit).OnNextAdd();  // Should IParentUndoUnit have a Lock() instead, now that Undo is internal?
                }
            }
        }

        //.........................................................
        //
        //  Change Notifications
        //
        //.........................................................

        /// <summary>
        /// Begins a change block.
        /// </summary>
        public void BeginChange()
        {
            this.TextEditor.Selection.BeginChange();
        }

        /// <summary>
        /// Ends a change block.
        /// </summary>
        public void EndChange()
        {
            if (this.TextEditor.Selection.ChangeBlockLevel == 0)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextBoxBase_UnmatchedEndChange));
            }

            this.TextEditor.Selection.EndChange();
        }

        /// <summary>
        /// Creates and returns a change block
        /// </summary>
        /// <returns>IDisposable</returns>
        public IDisposable DeclareChangeBlock()
        {
            return this.TextEditor.Selection.DeclareChangeBlock();
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Alias for TextEditor.IsReadOnly dependency property.
        /// Enables editing within this textbox.
        /// </summary>
        public static readonly DependencyProperty IsReadOnlyProperty =
                TextEditor.IsReadOnlyProperty.AddOwner(
                    typeof(TextBoxBase),
                    new FrameworkPropertyMetadata(
                        false,
                        FrameworkPropertyMetadataOptions.Inherits,
                        new PropertyChangedCallback(OnVisualStatePropertyChanged)));

        /// <summary>
        /// Whether or not the Textbox is read-only
        /// </summary>
        public bool IsReadOnly
        {
            get { return (bool) GetValue(TextEditor.IsReadOnlyProperty); }
            set { SetValue(TextEditor.IsReadOnlyProperty, value); }
        }

        /// <summary>
        /// DependencyProperty backing IsReadOnlyCaretVisible.
        /// </summary>
        public static readonly DependencyProperty IsReadOnlyCaretVisibleProperty =
            DependencyProperty.Register("IsReadOnlyCaretVisible",
                typeof(bool),
                typeof(TextBoxBase),
                new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnIsReadOnlyCaretVisiblePropertyChanged)));

        /// <summary>
        /// Whether or not the caret is visible when the textbox is read-only.
        /// </summary>
        public bool IsReadOnlyCaretVisible
        {
            get { return (bool)GetValue(IsReadOnlyCaretVisibleProperty); }
            set { SetValue(IsReadOnlyCaretVisibleProperty, value); }
        }

        /// <summary>
        /// Indicates if VK_Return character is accepted as a normal new-line character, if it is true, it will insert a new-line to the textbox
        /// or other editable controls, if it is false, it will not insert a new-line character to the controls's content, but just
        /// activates the control with focus.
        ///
        /// Default: true.
        /// TextBox and/or RichTextBox need to set this value appropriately
        /// </summary>
        public static readonly DependencyProperty AcceptsReturnProperty =
                KeyboardNavigation.AcceptsReturnProperty.AddOwner(typeof(TextBoxBase));

        /// <summary>
        /// Whether or not the Textbox accepts newlines
        /// </summary>
        public bool AcceptsReturn
        {
            get { return (bool) GetValue(AcceptsReturnProperty); }
            set { SetValue(AcceptsReturnProperty, value); }
        }

        /// <summary>
        /// Indicates if VK_TAB character is accepted as a normal tab char, if it is true, it will insert a tab character to the control's content,
        /// otherwise, it will not insert new tab to the content of control, instead, it will navigate the focus to the next IsTabStop control.
        ///
        /// Default: false.
        ///
        /// TextBox and RichTextBox need to set the value appropriately.
        /// </summary>
        public static readonly DependencyProperty AcceptsTabProperty =
                DependencyProperty.Register(
                        "AcceptsTab", // Property name
                        typeof(bool), // Property type
                        typeof(TextBoxBase), // Property owner
                        new FrameworkPropertyMetadata(false /*default value*/));

        /// <summary>
        /// Whether or not the Textbox accepts tabs
        /// </summary>
        public bool AcceptsTab
        {
            get { return (bool) GetValue(AcceptsTabProperty); }
            set { SetValue(AcceptsTabProperty, value); }
        }

        /// <summary>
        /// Access to all spelling options.
        /// </summary>
        public SpellCheck SpellCheck
        {
            get
            {
                return new SpellCheck(this);
            }
        }

        /// <summary>
        /// Exposes ScrollViewer's HorizontalScrollBarVisibility property
        /// Default: Hidden
        /// </summary>
        public static readonly DependencyProperty HorizontalScrollBarVisibilityProperty =
                ScrollViewer.HorizontalScrollBarVisibilityProperty.AddOwner(
                        typeof(TextBoxBase),
                        new FrameworkPropertyMetadata(
                                ScrollBarVisibility.Hidden,
                                new PropertyChangedCallback(OnScrollViewerPropertyChanged))); // PropertyChangedCallback

        /// <summary>
        /// Whether or not a horizontal scrollbar is shown
        /// </summary>
        public ScrollBarVisibility HorizontalScrollBarVisibility
        {
            get { return (ScrollBarVisibility) GetValue(HorizontalScrollBarVisibilityProperty); }
            set { SetValue(HorizontalScrollBarVisibilityProperty, value); }
        }

        /// <summary>
        /// Exposes ScrollViewer's VerticalScrollBarVisibility property
        /// Default: Hidden
        /// </summary>
        public static readonly DependencyProperty VerticalScrollBarVisibilityProperty =
                ScrollViewer.VerticalScrollBarVisibilityProperty.AddOwner(
                        typeof(TextBoxBase),
                        new FrameworkPropertyMetadata(ScrollBarVisibility.Hidden,
                        new PropertyChangedCallback(OnScrollViewerPropertyChanged)));

        /// <summary>
        /// Whether or not a vertical scrollbar is shown
        /// </summary>
        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get { return (ScrollBarVisibility) GetValue(VerticalScrollBarVisibilityProperty); }
            set { SetValue(VerticalScrollBarVisibilityProperty, value); }
        }

        /// <summary>
        /// Horizontal size of scrollable content; 0.0 if TextBox is non-scrolling
        /// </summary>
        public double ExtentWidth
        {
            get
            {
                return (this.ScrollViewer != null) ? this.ScrollViewer.ExtentWidth : 0.0;
            }
        }

        /// <summary>
        /// Vertical size of scrollable content; 0.0 if TextBox is non-scrolling
        /// </summary>
        public double ExtentHeight
        {
            get
            {
                return (this.ScrollViewer != null) ? this.ScrollViewer.ExtentHeight : 0.0;
            }
        }

        /// <summary>
        /// Horizontal size of scroll area; 0.0 if TextBox is non-scrolling
        /// </summary>
        public double ViewportWidth
        {
            get
            {
                return (this.ScrollViewer != null) ? this.ScrollViewer.ViewportWidth : 0.0;
            }
        }

        /// <summary>
        /// Vertical size of scroll area; 0.0 if TextBox is non-scrolling
        /// </summary>
        public double ViewportHeight
        {
            get
            {
                return (this.ScrollViewer != null) ? this.ScrollViewer.ViewportHeight : 0.0;
            }
        }


        /// <summary>
        /// Actual HorizontalOffset contains the ScrollViewer's current horizontal offset.
        /// This is a computed value, depending on the state of ScrollViewer, its Viewport, Extent
        /// and previous scrolling commands.
        /// </summary>
        public double HorizontalOffset
        {
            get
            {
                return (this.ScrollViewer != null) ? this.ScrollViewer.HorizontalOffset : 0.0;
            }
        }

        /// <summary>
        /// Actual VerticalOffset contains the ScrollViewer's current vertical offset.
        /// This is a computed value, depending on the state of ScrollViewer, its Viewport, Extent
        /// and previous scrolling commands.
        /// </summary>
        public double VerticalOffset
        {
            get
            {
                return (this.ScrollViewer != null) ? this.ScrollViewer.VerticalOffset : 0.0;
            }
        }

        /// <summary>
        /// Can the most recent action on the text box be undone?  Since we will frequently be called
        /// during a TextChanged event when the undo stack hasn't yet been modified to reflect the
        /// pending change, just looking at the UndoCount isn't sufficient.  TextEditor caches
        /// the pending undo action with us so we can refer to it now.
        /// </summary>
        public bool CanUndo
        {
            get
            {
                UndoManager undoManager;

                undoManager = UndoManager.GetUndoManager(this);
                if (undoManager != null && (_pendingUndoAction != UndoAction.Clear &&
                    (undoManager.UndoCount > undoManager.MinUndoStackCount ||
                    (undoManager.State != UndoState.Undo && _pendingUndoAction == UndoAction.Create))))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Can the most recent undone action on the text box be redone?
        /// </summary>
        public bool CanRedo
        {
            get
            {
                UndoManager undoManager;

                undoManager = UndoManager.GetUndoManager(this);
                if (undoManager != null && (_pendingUndoAction != UndoAction.Clear &&
                    (undoManager.RedoCount > 0 ||
                    (undoManager.State == UndoState.Undo && _pendingUndoAction == UndoAction.Create))))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Enables or disabled undo support on this Control.
        /// </summary>
        /// <remarks>
        /// Defaults to true.
        ///
        /// The control's undo record is cleared when this property transitions
        /// to false.
        /// </remarks>
        public static readonly DependencyProperty IsUndoEnabledProperty =
            DependencyProperty.Register("IsUndoEnabled", typeof(bool), typeof(TextBoxBase),
            new FrameworkPropertyMetadata(true, new PropertyChangedCallback(OnIsUndoEnabledChanged)));

        /// <summary>
        /// Enables or disabled undo support on this Control.
        /// </summary>
        /// <remarks>
        /// Defaults to true.
        ///
        /// The control's undo record is cleared when this property transitions
        /// to false.
        /// </remarks>
        public bool IsUndoEnabled
        {
            get
            {
                return (bool)GetValue(IsUndoEnabledProperty);
            }

            set
            {
                SetValue(IsUndoEnabledProperty, value);
            }
        }

        /// <summary>
        /// Sets the number of actions stored in the undo queue.
        /// </summary>
        /// <remarks>
        /// The value must be >= -1.
        ///
        /// -1 sets the storage to "infinite", limited only by available memory.
        /// 0 disables undo.
        ///
        /// Setting any value will clear the existing queue.
        ///
        /// An InvalidOperationException is thrown if the value is set inside to
        /// context of a BeginChange/EndChange pair while IsUndoEnabled is true
        /// (ie, you may not set this property while an undo unit is open).
        ///
        /// The default value is -1.
        /// </remarks>
        public static readonly DependencyProperty UndoLimitProperty =
            DependencyProperty.Register("UndoLimit", typeof(int), typeof(TextBoxBase),
            new FrameworkPropertyMetadata(UndoManager.UndoLimitDefaultValue, new PropertyChangedCallback(OnUndoLimitChanged)),
            new ValidateValueCallback(UndoLimitValidateValue));

        /// <summary>
        /// <see cref="UndoLimitProperty"/>
        /// </summary>
        public int UndoLimit
        {
            get
            {
                return (int)GetValue(UndoLimitProperty);
            }

            set
            {
                SetValue(UndoLimitProperty, value);
            }
        }

        /// <summary>
        /// The DependencyID for the AutoWordSelection property.
        /// Flags:              Can be used in style rules
        /// Default Value:      false
        /// </summary>
        public static readonly DependencyProperty AutoWordSelectionProperty =
            DependencyProperty.Register(
                "AutoWordSelection", // Property name
                typeof(bool), // Property type
                typeof(TextBoxBase), // Property owner
                new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Whether or not dragging with the mouse automatically selects words
        /// </summary>
        public bool AutoWordSelection
        {
            get
            {
                return (bool)GetValue(AutoWordSelectionProperty);
            }

            set
            {
                SetValue(AutoWordSelectionProperty, value);
            }
        }

        /// <summary>
        /// Brush used for selection fill.
        /// </summary>
        /// <remarks>
        /// If set to null, the selection will not be rendered.
        /// </remarks>
        public static readonly DependencyProperty SelectionBrushProperty =
            DependencyProperty.Register("SelectionBrush", typeof(Brush), typeof(TextBoxBase),
                new FrameworkPropertyMetadata(GetDefaultSelectionBrush(), new PropertyChangedCallback(UpdateCaretElement)));

        /// <summary>
        /// <see cref="SelectionBrushProperty"/>
        /// </summary>
        public Brush SelectionBrush
        {
            get { return (Brush)GetValue(SelectionBrushProperty); }
            set { SetValue(SelectionBrushProperty, value); }
        }

        /// <summary>
        /// Brush used for selected text.
        /// </summary>
        /// <remarks>
        /// If set to null, the selected text will not be rendered.
        /// </remarks>
        public static readonly DependencyProperty SelectionTextBrushProperty =
            DependencyProperty.Register("SelectionTextBrush", typeof(Brush), typeof(TextBoxBase),
                new FrameworkPropertyMetadata(GetDefaultSelectionTextBrush(), new PropertyChangedCallback(UpdateCaretElement)));

        /// <summary>
        /// <see cref="SelectionTextBrushProperty"/>
        /// </summary>
        public Brush SelectionTextBrush
        {
            get { return (Brush)GetValue(SelectionTextBrushProperty); }
            set { SetValue(SelectionTextBrushProperty, value); }
        }

        internal const double AdornerSelectionOpacityDefaultValue = 0.4;
        internal const double NonAdornerSelectionOpacityDefaultValue = 1;

        /// <summary>
        /// The default to use for SelectionOpacity.
        /// When using the adorner layer to draw selections, we need it to be translucent so that the underlying text
        /// can be seen.  When the selection is drawn as a background to the text, there is no need for this so the
        /// selection can be fully opaque.
        /// </summary>
        private static double SelectionOpacityDefaultValue =
            (FrameworkAppContextSwitches.UseAdornerForTextboxSelectionRendering) ? AdornerSelectionOpacityDefaultValue : NonAdornerSelectionOpacityDefaultValue;

        /// <summary>
        /// Opacity used for drawing the selection.
        /// </summary>
        /// <remarks>
        /// The opacity of SelectionBrush will also be respected (multiplied with this one during render).
        /// </remarks>
        public static readonly DependencyProperty SelectionOpacityProperty =
            DependencyProperty.Register("SelectionOpacity", typeof(double), typeof(TextBoxBase),
                new FrameworkPropertyMetadata(SelectionOpacityDefaultValue,
                    new PropertyChangedCallback(UpdateCaretElement)));

        /// <summary>
        /// <see cref="SelectionOpacityProperty"/>
        /// </summary>
        public double SelectionOpacity
        {
            get { return (double)GetValue(SelectionOpacityProperty); }
            set { SetValue(SelectionOpacityProperty, value); }
        }

        /// <summary>
        /// Brush used for the caret.
        /// </summary>
        /// <remarks>
        /// If set to null, the default behavior of setting the caret color to the inverse of the background color will be used.
        /// </remarks>
        public static readonly DependencyProperty CaretBrushProperty =
            DependencyProperty.Register("CaretBrush", typeof(Brush), typeof(TextBoxBase),
                new FrameworkPropertyMetadata(null, new PropertyChangedCallback(UpdateCaretElement)));

        /// <summary>
        /// <see cref="CaretBrushProperty"/>
        /// </summary>
        public Brush CaretBrush
        {
            get { return (Brush)GetValue(CaretBrushProperty); }
            set { SetValue(CaretBrushProperty, value); }
        }

        internal static readonly DependencyPropertyKey IsSelectionActivePropertyKey =
                DependencyProperty.RegisterAttachedReadOnly(
                        "IsSelectionActive",
                        typeof(bool),
                        typeof(TextBoxBase),
                        new FrameworkPropertyMetadata(MS.Internal.KnownBoxes.BooleanBoxes.FalseBox));

        public static readonly DependencyProperty IsSelectionActiveProperty =
            IsSelectionActivePropertyKey.DependencyProperty;

        public bool IsSelectionActive
        {
            get { return (bool)GetValue(IsSelectionActiveProperty); }
        }

        public static readonly DependencyProperty IsInactiveSelectionHighlightEnabledProperty =
            DependencyProperty.Register("IsInactiveSelectionHighlightEnabled", typeof(bool), typeof(TextBoxBase));

        public bool IsInactiveSelectionHighlightEnabled
        {
            get { return (bool)GetValue(IsInactiveSelectionHighlightEnabledProperty); }
            set { SetValue(IsInactiveSelectionHighlightEnabledProperty, value); }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        #region Public Events

        /// <summary>
        /// Event for "Text has changed"
        /// </summary>
        public static readonly RoutedEvent TextChangedEvent = EventManager.RegisterRoutedEvent(
            "TextChanged", // Event name
            RoutingStrategy.Bubble, //
            typeof(TextChangedEventHandler), //
            typeof(TextBoxBase)); //

        /// <summary>
        /// Event fired from this text box when its inner content
        /// has been changed.
        /// </summary>
        /// <remarks>
        /// The event itself is defined on TextEditor.
        /// </remarks>
        public event TextChangedEventHandler TextChanged
        {
            add
            {
                AddHandler(TextChangedEvent, value);
            }

            remove
            {
                RemoveHandler(TextChangedEvent, value);
            }
        }

        /// <summary>
        /// Event for "Selection has changed"
        /// </summary>
        public static readonly RoutedEvent SelectionChangedEvent = EventManager.RegisterRoutedEvent(
            "SelectionChanged", // Event name
            RoutingStrategy.Bubble, //
            typeof(RoutedEventHandler), //
            typeof(TextBoxBase)); //

        /// <summary>
        /// Event fired from this text box when its selection has been changed.
        /// </summary>
        public event RoutedEventHandler SelectionChanged
        {
            add
            {
                AddHandler(SelectionChangedEvent, value);
            }

            remove
            {
                RemoveHandler(SelectionChangedEvent, value);
            }
        }

        #endregion Public Events

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        internal override void ChangeVisualState(bool useTransitions)
        {
            // See ButtonBase.ChangeVisualState.
            // This method should be exactly like it, except we have a ReadOnly state instead of Pressed
            if (!IsEnabled)
            {
                VisualStateManager.GoToState(this, VisualStates.StateDisabled, useTransitions);
            }
            else if (IsReadOnly)
            {
                VisualStateManager.GoToState(this, VisualStates.StateReadOnly, useTransitions);
            }
            else if (IsMouseOver)
            {
                VisualStateManager.GoToState(this, VisualStates.StateMouseOver, useTransitions);
            }
            else
            {
                VisualStateManager.GoToState(this, VisualStates.StateNormal, useTransitions);
            }

            if (IsKeyboardFocused)
            {
                VisualStateManager.GoToState(this, VisualStates.StateFocused, useTransitions);
            }
            else
            {
                VisualStateManager.GoToState(this, VisualStates.StateUnfocused, useTransitions);
            }

            base.ChangeVisualState(useTransitions);
        }

        /// <summary>
        /// Called when content in this Control changes.
        /// Raises the TextChanged event.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnTextChanged(TextChangedEventArgs e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        /// Called when the caret or selection changes position.
        /// Raises the SelectionChanged event.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnSelectionChanged(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        /// Template has changed
        /// </summary>
        /// <param name="oldTemplate">
        /// </param>
        /// <param name="newTemplate">
        /// </param>
        protected override void OnTemplateChanged(ControlTemplate oldTemplate, ControlTemplate newTemplate)
        {
            base.OnTemplateChanged(oldTemplate, newTemplate);

            if (oldTemplate!=null && newTemplate!= null && oldTemplate.VisualTree != newTemplate.VisualTree)
            {
                DetachFromVisualTree();
            }
        }

        /// <summary>
        /// ScrollViewer marks all mouse wheel events as handled, even if no scrolling occurs.  This means that
        /// when mousewheeling through a document, if the cursor happens to land on a textbox, scrolling will
        /// stop when the textbox reaches the end of its content.  We want the scroll event to continue to the
        /// outer control in such a case so that outer control continues scrolling.
        /// </summary>
        /// <param name="e">MouseWheelEventArgs</param>
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }

            if (this.ScrollViewer != null)
            {
                // Only raise the event on ScrollViewer if we're actually going to scroll
                if ((e.Delta > 0 && VerticalOffset != 0) /* scrolling up */ || (e.Delta < 0 && VerticalOffset < this.ScrollViewer.ScrollableHeight) /* scrolling down */ )
                {
                    Invariant.Assert(this.RenderScope is IScrollInfo);
                    if (e.Delta > 0)
                    {
                        ((IScrollInfo)this.RenderScope).MouseWheelUp();
                    }
                    else
                    {
                        ((IScrollInfo)this.RenderScope).MouseWheelDown();
                    }
                    e.Handled = true;
                }
            }
            base.OnMouseWheel(e);
        }

        /// <summary>
        ///     Virtual method reporting a key was pressed
        /// </summary>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Handled)
            {
                return;
            }

            if (_textEditor != null)
            {
                _textEditor.OnPreviewKeyDown(e);
            }
        }

        /// <summary>
        ///     Virtual method reporting a key was pressed
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Handled)
            {
                return;
            }

            if (_textEditor != null)
            {
                _textEditor.OnKeyDown(e);
            }
        }

        /// <summary>
        ///     Virtual method reporting a key was released
        /// </summary>
        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            if (e.Handled)
            {
                return;
            }

            if (_textEditor != null)
            {
                _textEditor.OnKeyUp(e);
            }
        }

        /// <summary>
        ///     Virtual method reporting text composition
        /// </summary>
        protected override void OnTextInput(TextCompositionEventArgs e)
        {
            base.OnTextInput(e);

            if (e.Handled)
            {
                return;
            }

            if (_textEditor != null)
            {
                _textEditor.OnTextInput(e);
            }
        }

        /// <summary>
        ///     Virtual method reporting the mouse button was pressed
        /// </summary>
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Handled)
            {
                return;
            }

            if (_textEditor != null)
            {
                _textEditor.OnMouseDown(e);
            }
        }

        /// <summary>
        ///     Virtual method reporting a mouse move
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (e.Handled)
            {
                return;
            }

            if (_textEditor != null)
            {
                _textEditor.OnMouseMove(e);
            }
        }

        /// <summary>
        ///     Virtual method reporting the mouse button was released
        /// </summary>
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            if (e.Handled)
            {
                return;
            }

            if (_textEditor != null)
            {
                _textEditor.OnMouseUp(e);
            }
        }

        /// <summary>
        ///     Virtual method reporting the cursor to display was requested
        /// </summary>
        protected override void OnQueryCursor(QueryCursorEventArgs e)
        {
            base.OnQueryCursor(e);

            if (e.Handled)
            {
                return;
            }

            if (_textEditor != null)
            {
                _textEditor.OnQueryCursor(e);
            }
        }

        /// <summary>
        ///     Virtual method reporting the query continue drag is going to happen
        /// </summary>
        protected override void OnQueryContinueDrag(QueryContinueDragEventArgs e)
        {
            base.OnQueryContinueDrag(e);

            if (e.Handled)
            {
                return;
            }

            if (_textEditor != null)
            {
                _textEditor.OnQueryContinueDrag(e);
            }
        }

        /// <summary>
        ///     Virtual method reporting the give feedback is going to happen
        /// </summary>
        protected override void OnGiveFeedback(GiveFeedbackEventArgs e)
        {
            base.OnGiveFeedback(e);

            if (e.Handled)
            {
                return;
            }

            if (_textEditor != null)
            {
                _textEditor.OnGiveFeedback(e);
            }
        }

        /// <summary>
        ///     Virtual method reporting the drag enter is going to happen
        /// </summary>
        protected override void OnDragEnter(DragEventArgs e)
        {
            base.OnDragEnter(e);

            if (e.Handled)
            {
                return;
            }

            if (_textEditor != null)
            {
                _textEditor.OnDragEnter(e);
            }
        }

        /// <summary>
        ///     Virtual method reporting the drag over is going to happen
        /// </summary>
        protected override void OnDragOver(DragEventArgs e)
        {
            base.OnDragOver(e);

            if (e.Handled)
            {
                return;
            }

            if (_textEditor != null)
            {
                _textEditor.OnDragOver(e);
            }
        }

        /// <summary>
        ///     Virtual method reporting the drag leave is going to happen
        /// </summary>
        protected override void OnDragLeave(DragEventArgs e)
        {
            base.OnDragLeave(e);

            if (e.Handled)
            {
                return;
            }

            if (_textEditor != null)
            {
                _textEditor.OnDragLeave(e);
            }
        }

        /// <summary>
        ///     Virtual method reporting the drag enter is going to happen
        /// </summary>
        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);

            if (e.Handled)
            {
                return;
            }

            if (_textEditor != null)
            {
                _textEditor.OnDrop(e);
            }
        }

        /// <summary>
        ///     Called when ContextMenuOpening is raised on this element.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnContextMenuOpening(ContextMenuEventArgs e)
        {
            base.OnContextMenuOpening(e);

            if (e.Handled)
            {
                return;
            }

            if (_textEditor != null)
            {
                _textEditor.OnContextMenuOpening(e);
            }
        }

        /// <summary>
        ///     Virtual method reporting that the keyboard is focused on this element
        /// </summary>
        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnGotKeyboardFocus(e);

            if (e.Handled)
            {
                return;
            }

            if (_textEditor != null)
            {
                _textEditor.OnGotKeyboardFocus(e);
            }
        }

        /// <summary>
        ///     Virtual method reporting that the keyboard is no longer focusekeyboard is no longer focuseed
        /// </summary>
        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnLostKeyboardFocus(e);

            if (e.Handled)
            {
                return;
            }

            if (_textEditor != null)
            {
                _textEditor.OnLostKeyboardFocus(e);
            }
        }

        /// <summary>
        ///     This method is invoked when the IsFocused property changes to false
        /// </summary>
        /// <param name="e">RoutedEventArgs</param>
        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            if (e.Handled)
            {
                return;
            }

            if (_textEditor != null)
            {
                _textEditor.OnLostFocus(e);
            }
        }

        // Allocates the initial render scope for this control.
        internal abstract FrameworkElement CreateRenderScope();

        /// <summary>
        /// Handler for TextContainer.Changed event.  Raises the TextChanged event on UiScope.
        /// for editing controls.
        /// </summary>
        /// <param name="sender">
        /// sender
        /// </param>
        /// <param name="e">
        /// event args
        /// </param>
        internal virtual void OnTextContainerChanged(object sender, TextContainerChangedEventArgs e)
        {
            // If only properties on the text changed, don't fire a content change event.
            // This can happen even in a plain text TextBox if we switch logical trees.
            if (!e.HasContentAddedOrRemoved && !e.HasLocalPropertyValueChange)
            {
                return;
            }

            UndoManager undoManager = UndoManager.GetUndoManager(this);

            UndoAction undoAction;
            if (undoManager != null) // Will be null for controls like PasswordBox that don't use undo.
            {
                if (_textEditor.UndoState == UndoState.Redo)
                {
                    undoAction = UndoAction.Redo;
                }
                else if (_textEditor.UndoState == UndoState.Undo)
                {
                    undoAction = UndoAction.Undo;
                }
                else if (undoManager.OpenedUnit == null)
                {
                    undoAction = UndoAction.Clear;
                }
                else if (undoManager.LastReopenedUnit == undoManager.OpenedUnit)
                {
                    undoAction = UndoAction.Merge;
                }
                else
                {
                    undoAction = UndoAction.Create;
                }
            }
            else
            {
                undoAction = UndoAction.Create;
            }

            // The undo stack hasn't yet been modified by this change, so CanUndo will not
            // necessarily yield the correct result if queried during the TextChange event.
            // Store the undo action in the uiScope, so CanUndo can
            // reference it to provide the correct result.
            _pendingUndoAction = undoAction;
            try
            {
                OnTextChanged(new TextChangedEventArgs(TextChangedEvent, undoAction, new ReadOnlyCollection<TextChange>(e.Changes.Values)));
            }
            finally
            {
                _pendingUndoAction = UndoAction.None;
            }
        }

        #endregion Protected Methods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Attaches this control to a new TextContainer.
        internal void InitializeTextContainer(TextContainer textContainer)
        {
            Invariant.Assert(textContainer != null);
            Invariant.Assert(textContainer.TextSelection == null);

            // Uninitialize previous TextEditor
            if (_textContainer != null)
            {
                Invariant.Assert(_textEditor != null);
                Invariant.Assert(_textEditor.TextContainer == _textContainer);
                Invariant.Assert(_textEditor.TextContainer.TextSelection == _textEditor.Selection);

                // Detach existing editor from VisualTree
                DetachFromVisualTree();

                // Discard TextEditor - must release text container
                _textEditor.OnDetach();
            }

            // Save text container
            _textContainer = textContainer;
            _textContainer.Changed += new TextContainerChangedEventHandler(OnTextContainerChanged);

            // Create a text editor, initialize undo manager for it, and link it to text container
            _textEditor = new TextEditor(_textContainer, this, true);
            _textEditor.Selection.Changed += new EventHandler(OnSelectionChangedInternal);

            // Init a default undo limit.
            UndoManager undoManager = UndoManager.GetUndoManager(this);
            if (undoManager != null)
            {
                undoManager.UndoLimit = this.UndoLimit;
            }

            // Delay raising automation events until the automation subsystem is activated by a client.
            // ISSUE-2005/01/23-vsmirnov - Adding an event listener to AutomationProvider apparently
            // causes memory leaks because TextBoxBase is never released. I comment it out for now just
            // to fix the build break (perf DRT failure). Need to find a right fix later.
            // AutomationProvider.Activated += new AutomationActivatedEventHandler(OnAutomationActivated);
        }

        /// <summary>
        /// Returns a TextPosition matching the specified pixel coordinates.
        /// </summary>
        /// <param name="point">
        /// Pixel coordinate to hittest with.
        /// point is expected to be in the coordinate space of this TextBox.
        /// </param>
        /// <param name="snapToText">
        /// If true, heuristics are applied to find the closest character
        /// position to point, even if point does not intersect any character
        /// bounding box.
        /// </param>
        /// <returns>
        /// A TextPosition and its orientation matching the specified pixel.
        /// May return null if snapToText is false and point does not fall
        /// within any character bounding box.
        /// </returns>
        internal TextPointer GetTextPositionFromPointInternal(Point point, bool snapToText)
        {
            TextPointer position;

            // Transform to content coordinates.
            GeneralTransform transform = this.TransformToDescendant(this.RenderScope);

            if (transform != null)
            {
                transform.TryTransform(point, out point);
            }

            if (TextEditor.GetTextView(this.RenderScope).Validate(point))
            {
                position = (TextPointer)TextEditor.GetTextView(this.RenderScope).GetTextPositionFromPoint(point, snapToText);
            }
            else
            {
                position = snapToText ? this.TextContainer.Start : null;
            }

            return position;
        }

        /// <summary>
        /// Retrieves the height and offset, in pixels, of the edge of
        /// the object/character represented by position.
        /// </summary>
        /// <param name="position">
        /// Position of an object/character.
        /// </param>
        /// <param name="rect">
        /// Receives the bounding box.
        /// </param>
        /// <returns>
        /// Returns false if no layout is available.  In this case rect will be set empty.
        /// </returns>
        /// <remarks>
        /// Coordinates of the return value are relative to this TextBox.
        ///
        /// Rect.Width is always 0.
        ///
        /// If the content is empty, then this method returns the expected
        /// height of a character, if placed at the specified position.
        ///</remarks>
        internal bool GetRectangleFromTextPosition(TextPointer position, out Rect rect)
        {
            Point offset;

            if (position == null)
            {
                throw new ArgumentNullException("position");
            }

            // Validate layout information on TextView
            if (TextEditor.GetTextView(this.RenderScope).Validate(position))
            {
                // Get the rect in local content coordinates.
                rect = TextEditor.GetTextView(this.RenderScope).GetRectangleFromTextPosition(position);

                // Transform to RichTextBox control coordinates.
                offset = new Point(0, 0);
                GeneralTransform transform = this.TransformToDescendant(this.RenderScope);
                if (transform != null)
                {
                    transform.TryTransform(offset, out offset);
                }
                rect.X -= offset.X;
                rect.Y -= offset.Y;
            }
            else
            {
                rect = Rect.Empty;
            }

            return rect != Rect.Empty;
        }

        /// <summary>
        /// Detaches the editor from old visual tree and attaches it to a new one
        /// </summary>
        internal virtual void AttachToVisualTree()
        {
            DetachFromVisualTree();

            // Walk the visual tree to find our Text element
            SetRenderScopeToContentHost();

            // Set properties on ScrollViewer
            // Note that this.ScrollViewer will walk the tree from current TextEditor's render scope up to its ui scope.
            if (this.ScrollViewer != null)
            {
                this.ScrollViewer.ScrollChanged += new ScrollChangedEventHandler(OnScrollChanged);

                //  We may delete the TextEditor.PageHeightProperty and use _Scroller.ViewportHeight direction
                SetValue(TextEditor.PageHeightProperty, this.ScrollViewer.ViewportHeight);

                // Need to make scroll viewer non-focusable, otherwise it will eat keyboard navigation from editor
                this.ScrollViewer.Focusable = false;

                // Prevent mouse wheel scrolling from breaking when there's no more content in the direction of the scroll
                this.ScrollViewer.HandlesMouseWheelScrolling = false;

                if (this.ScrollViewer.Background == null)
                {
                    // prevent hit-testing through padding
                    this.ScrollViewer.Background = Brushes.Transparent;
                }

                OnScrollViewerPropertyChanged(this, new DependencyPropertyChangedEventArgs(ScrollViewer.HorizontalScrollBarVisibilityProperty, null /* old value */, this.GetValue(HorizontalScrollBarVisibilityProperty)));
                OnScrollViewerPropertyChanged(this, new DependencyPropertyChangedEventArgs(ScrollViewer.VerticalScrollBarVisibilityProperty, null /* old value */, this.GetValue(VerticalScrollBarVisibilityProperty)));
                OnScrollViewerPropertyChanged(this, new DependencyPropertyChangedEventArgs(ScrollViewer.PaddingProperty, null /* old value */, this.GetValue(PaddingProperty)));
            }
            else
            {
                ClearValue(TextEditor.PageHeightProperty);
            }
        }

        // Do the work of line up.  Can be overridden by subclass to implement true line up.
        internal virtual void DoLineUp()
        {
            if (this.ScrollViewer != null)
            {
                this.ScrollViewer.LineUp();
            }
        }

        // Do the work of line down.  Can be overridden by subclass to implement true line down.
        internal virtual void DoLineDown()
        {
            if (this.ScrollViewer != null)
            {
                this.ScrollViewer.LineDown();
            }
        }

        /// <summary>
        /// When RenderScope is FlowDocumentView, events can bypass our nested ScrollViewer.
        /// We want to make sure that ScrollViewer-- and any other elements in our style--
        /// always gets a `crack at mouse events.
        /// </summary>
        internal override void AddToEventRouteCore(EventRoute route, RoutedEventArgs args)
        {
            base.AddToEventRouteCore(route, args);

            // Walk up the tree from the RenderScope to this, adding each element to the route
            Visual visual = this.RenderScope;
            while (visual != this && visual != null)
            {
                if (visual is UIElement)
                {
                    ((UIElement)visual).AddToEventRoute(route, args);
                }
                visual = VisualTreeHelper.GetParent(visual) as Visual;
            }
        }


        /// <summary>
        /// Helper method to update the IsUndoEnabled flag within UndoManager
        /// attached to the TextBox.
        /// </summary>
        /// <param name="value">
        /// New Value of IsUndoEnabled flag.
        /// </param>
        internal void ChangeUndoEnabled(bool value)
        {
            // REVIEW:benwest:9/1/2005: does throwing exceptions here really work?
            // Isn't the property value already changed?  We don't want the property value
            // to change....
            if (this.TextSelectionInternal.ChangeBlockLevel > 0)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextBoxBase_CantSetIsUndoEnabledInsideChangeBlock));
            }

            UndoManager undoManager = UndoManager.GetUndoManager(this);
            if (undoManager != null)
            {
                if (!value && undoManager.IsEnabled)
                {
                    undoManager.Clear();
                }
                undoManager.IsEnabled = value;
            }
        }

        /// <summary>
        /// Helper method to update the UndoLimit in UndoManager
        /// attached to the TextBox.
        /// </summary>
        /// <param name="value">
        /// New Value of UndoLimit.
        /// </param>
        internal void ChangeUndoLimit(object value)
        {
            UndoManager undoManager = UndoManager.GetUndoManager(this);
            if (undoManager != null)
            {
                if (undoManager.OpenedUnit != null)
                {
                    // the exception text isn't exactly right, but we can't
                    // introduce new strings in v3.5.
                    throw new InvalidOperationException(SR.Get(SRID.TextBoxBase_CantSetIsUndoEnabledInsideChangeBlock));
                }

                int limit;

                if (value == DependencyProperty.UnsetValue)
                {
                    limit = UndoManager.UndoLimitDefaultValue;
                }
                else
                {
                    limit = (int)value;
                }

                undoManager.UndoLimit = limit;
            }
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Access to the ScrollViewer in textbox style
        /// </summary>
        internal ScrollViewer ScrollViewer
        {
            get
            {
                if (_scrollViewer == null)
                {
                    if (_textEditor != null)
                    {
                        // TextEditor's _Scroller property finds a ScrollViewer found
                        // by a tree walk from the editor's render scope within ui scope.
                        _scrollViewer = _textEditor._Scroller as ScrollViewer;
                    }
                }
                return _scrollViewer;
            }
        }

        /// <summary>
        /// Text Selection (readonly)
        /// </summary>
        internal TextSelection TextSelectionInternal
        {
            get
            {
                return (TextSelection)_textEditor.Selection;
            }
        }

        /// <summary>
        /// A TextContainer covering the TextBox's inner content.
        /// Never returns null, throws SystemException if unavailable.
        /// </summary>
        internal TextContainer TextContainer
        {
            get
            {
                return _textContainer;
            }
        }

        /// <summary>
        /// readonly access to internal content control
        /// </summary>
        internal FrameworkElement RenderScope
        {
            get
            {
                return _renderScope;
            }
        }

        // Expose _pendingUndoAction for DrtEditing.exe, via reflection.
        internal UndoAction PendingUndoAction
        {
            get
            {
                return _pendingUndoAction;
            }

            set
            {
                _pendingUndoAction = value;
            }
        }

        // TextEditor attached to this control.
        internal TextEditor TextEditor
        {
            get
            {
                return _textEditor;
            }
        }

        // True if style has been applied to the control and
        // ContentHostTemplateName was successfully found in it.
        internal bool IsContentHostAvailable
        {
            get
            {
                return _textBoxContentHost != null;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Clear our layout-specific data, and detach our current renderScope from our text editor.
        /// </summary>
        private void DetachFromVisualTree()
        {
            if (_textEditor != null)
            {
                _textEditor.Selection.DetachFromVisualTree();
            }

            // Detach scroll handler from old scroll viewer.
            // Note that this.ScrollViewer will walk the tree from current TextEditor's render scope up to its ui scope.
            if (this.ScrollViewer != null)
            {
                this.ScrollViewer.ScrollChanged -= new ScrollChangedEventHandler(OnScrollChanged);
            }

            // Invalidate our cached copy of scroll viewer.
            _scrollViewer = null;

            ClearContentHost();
        }

        // Initializes a new render scope.
        private void InitializeRenderScope()
        {
            if (_renderScope == null)
            {
                return;
            }

            // Map the TextContainer and TextView.
            ITextView textView = (ITextView)((IServiceProvider)_renderScope).GetService(typeof(ITextView));

            this.TextContainer.TextView = textView;
            _textEditor.TextView = textView; // REVIEW:benwest: this is redundant!  TextEditor already has a ref to TextContainer!

            if (this.ScrollViewer != null)
            {
                this.ScrollViewer.CanContentScroll = true;
            }
        }

        // Uninitializes a render scope and clears this control's reference.
        private void UninitializeRenderScope()
        {
            TextBoxView tbv;
            FlowDocumentView fdv;

            // Clear TextView property in TextEditor
            _textEditor.TextView = null;

            // Remove our content from the renderScope
            if ((tbv = _renderScope as TextBoxView) != null)
            {
                tbv.RemoveTextContainerListeners();
            }
            else if ((fdv = _renderScope as FlowDocumentView) != null)
            {
                if (fdv.Document != null)
                {
                    fdv.Document.Uninitialize();
                    fdv.Document = null;
                }
            }
            else
            {
                Invariant.Assert(_renderScope == null, "_renderScope must be null here");
            }
        }

        /// <summary>
        /// Creates the default brush used for selection rendering.
        /// </summary>
        private static Brush GetDefaultSelectionBrush()
        {
            Brush selectionBrush = new SolidColorBrush(SystemColors.HighlightColor);
            selectionBrush.Freeze();
            return selectionBrush;
        }

        /// <summary>
        /// Creates the default brush used for selection text rendering.
        /// </summary>
        private static Brush GetDefaultSelectionTextBrush()
        {
            Brush selectionTextBrush = new SolidColorBrush(SystemColors.HighlightTextColor);
            selectionTextBrush.Freeze();
            return selectionTextBrush;
        }

        /// <summary>
        /// Callback for PageHeight GetValue.
        /// </summary>
        /// <param name="d">
        /// dependency object
        /// </param>
        /// <returns>
        /// </returns>
        private static object OnPageHeightGetValue(DependencyObject d)
        {
            return ((TextBoxBase)d).ViewportHeight;
        }

        /// <summary>
        /// Finds an element in a style temaplte marked as ContentHostTemplateName
        /// where our render scope must be placed as a child.
        /// </summary>
        private void SetRenderScopeToContentHost()
        {
            FrameworkElement renderScope = CreateRenderScope();

            // Clear the content host from previous render scope (if any)
            ClearContentHost();

            // Find ContentHostTemplateName in the style
            _textBoxContentHost = GetTemplateChild(ContentHostTemplateName) as FrameworkElement;
            // Note that we allow ContentHostTemplateName to be optional.
            // This simplifies toolability of our control styling.
            // When the ContentHostTemplateName is not found or incorrect
            // TextBox goes into disabled state, but not throw.

            // Add renderScope as a child of ContentHostTemplateName
            _renderScope = renderScope;
            if (_textBoxContentHost is ScrollViewer)
            {
                ScrollViewer scrollViewer = (ScrollViewer)_textBoxContentHost;

                if (scrollViewer.Content != null)
                {
                    _renderScope = null;
                    _textBoxContentHost = null;
                    //  Do not throw exception
                    throw new NotSupportedException(SR.Get(SRID.TextBoxScrollViewerMarkedAsTextBoxContentMustHaveNoContent));
                }
                else
                {
                    scrollViewer.Content = _renderScope; // this may replace old render scope in case of upgrade scenario in TextBox
                }
            }
            else if (_textBoxContentHost is Decorator)
            {
                Decorator decorator = (Decorator)_textBoxContentHost;
                if (decorator.Child != null)
                {
                    _renderScope = null;
                    _textBoxContentHost = null;
                    //  Do not throw exception
                    throw new NotSupportedException(SR.Get(SRID.TextBoxDecoratorMarkedAsTextBoxContentMustHaveNoContent));
                }
                else
                {
                    decorator.Child = _renderScope; // this may replace old render scope in case of upgrade scenario in TextBox
                }
            }
            else
            {
                // When we implement TextContainer setting via TextView interface
                // all text containing element will become allowed here.
                _renderScope = null;

                // Explicitly not throwing an exception here when content host = null
                // -- designers need us to support no content scenarios
                if (_textBoxContentHost != null)
                {
                    _textBoxContentHost = null;
                    //  Remove the exception
                    throw new NotSupportedException(SR.Get(SRID.TextBoxInvalidTextContainer));
                }
            }

            // Attach render scope to TextEditor
            InitializeRenderScope();
        }

        private void ClearContentHost()
        {
            // Detach render scope from TextEditor
            UninitializeRenderScope();

            // Render scope has been created by us,
            // so we need to extract if from visual tree.
            if (_textBoxContentHost is ScrollViewer)
            {
                ((ScrollViewer)_textBoxContentHost).Content = null;
            }
            else if (_textBoxContentHost is Decorator)
            {
                ((Decorator)_textBoxContentHost).Child = null;
            }
            else
            {
                Invariant.Assert(_textBoxContentHost == null, "_textBoxContentHost must be null here");
            }

            _textBoxContentHost = null;
        }

        /// <summary>
        /// PropertyChanged handler for IsReadOnlyCaretVisibleProperty.
        /// </summary>
        private static void OnIsReadOnlyCaretVisiblePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TextBoxBase textBox = (TextBoxBase)d;
            textBox.TextSelectionInternal.UpdateCaretState(CaretScrollMethod.None);
            ((ITextSelection)textBox.TextSelectionInternal).RefreshCaret();
        }

        /// <summary>
        /// Handler for ScrollViewer's OnScrollChanged event.
        /// </summary>
        internal virtual void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            //  We should avoid adding per-instance handlers in TextBox
            if (e.ViewportHeightChange != 0)
            {
                SetValue(TextEditor.PageHeightProperty, e.ViewportHeight);
            }
        }

        /// <summary>
        /// TextSelection.Moved event listener.
        /// </summary>
        private void OnSelectionChangedInternal(object sender, EventArgs e)
        {
#if OLD_AUTOMATION
            // It the automation subsystem is active, notify automation clients
            // about the selection change.
            if (AutomationProvider.IsActive)
            {
                RaiseSelectionChangedEvent();
            }
#endif
            OnSelectionChanged(new RoutedEventArgs(SelectionChangedEvent));
        }

#if OLD_AUTOMATION
        /// <summary>
        /// A helper to raise AutomationEvents.
        /// The reason this method is standalone with MethodImplOptions.NoInlining is to avoid
        /// loading UIAutomation unless there's a client already present.
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void RaiseSelectionChangedEvent()
        {
            AutomationProvider.RaiseAutomationEvent(TextPatternIdentifiers.TextSelectionChangedEvent, this);
        }
#endif
        ///
        // Returns the DependencyObjectType for the registered ThemeStyleKey's default
        // value. Controls will override this method to return approriate types.
        internal override DependencyObjectType DTypeThemeStyleKey
        {
            get
            {
                return _dType;
            }
        }

        /// <summary>
        /// Callback for changed ScrollViewer properties, forwarding values from TextBoxBase to ScrollViewer.
        /// </summary>
        /// <param name="d">
        /// TextBoxBase on which the property is changed
        /// </param>
        /// <param name="e">event args</param>
        internal static void OnScrollViewerPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TextBoxBase textBox = d as TextBoxBase;

            if (textBox != null && textBox.ScrollViewer != null)
            {
                object value = e.NewValue;
                if (value == DependencyProperty.UnsetValue)
                {
                    textBox.ScrollViewer.ClearValue(e.Property);
                }
                else
                {
                    textBox.ScrollViewer.SetValue(e.Property, value);
                }
            }
        }

        // Callback for IsUndoEnabledProperty changes.
        private static void OnIsUndoEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TextBoxBase textBox = (TextBoxBase)d;
            textBox.ChangeUndoEnabled((bool)e.NewValue);
        }

        /// <summary>
        /// <see cref="DependencyProperty.ValidateValueCallback"/>
        /// </summary>
        private static bool UndoLimitValidateValue(object value)
        {
            return ((int)value) >= -1;
        }

        // Callback for UndoLimitProperty changes.
        private static void OnUndoLimitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TextBoxBase textBox = (TextBoxBase)d;
            textBox.ChangeUndoLimit(e.NewValue);
        }

        /// <summary>
        /// Callback for changed InputMethodEnabled properties
        /// </summary>
        /// <param name="d">
        /// TextBoxBase on which the property is changed
        /// </param>
        /// <param name="e">event args</param>
        private static void OnInputMethodEnabledPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TextBoxBase textBox = (TextBoxBase)d;

            if ((textBox.TextEditor != null) && (textBox.TextEditor.TextStore != null))
            {
                bool value = (bool)e.NewValue;
                if (value)
                {
                    if (Keyboard.FocusedElement == textBox)
                    {
                         // Call TextStore.OnGotFocus() to set up the focus dim correctly.
                         textBox.TextEditor.TextStore.OnGotFocus();
                    }
                }
            }
        }

        /// <summary>
        /// PropertyChanged callback for a property that affects the selection or caret rendering.
        /// </summary>
        private static void UpdateCaretElement(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TextBoxBase textBoxBase = (TextBoxBase)d;

            if (textBoxBase.TextSelectionInternal != null)
            {
                CaretElement caretElement = textBoxBase.TextSelectionInternal.CaretElement;
                if (caretElement != null)
                {
                    if (e.Property == CaretBrushProperty)
                    {
                        caretElement.UpdateCaretBrush(TextSelection.GetCaretBrush(textBoxBase.TextEditor));
                    }

                    caretElement.InvalidateVisual();
                }

                
                // If the TextBox is rendering its own selection we need to invalidate arrange here
                // in order to ensure the selection is updated.
                var textBoxView = textBoxBase?.RenderScope as TextBoxView;

                if ((textBoxView as ITextView)?.RendersOwnSelection == true)
                {
                    textBoxView.InvalidateArrange();
                }
            }
        }

        #endregion Private methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private static DependencyObjectType _dType;

        // Text content owned by this TextBox.
        //  we could get this off _textEditor.TextContainer, it's redundant state.
        private TextContainer _textContainer;

        // Text editor
        private TextEditor _textEditor;

        // An element marked as TextBoxContentto which we assign our _renderScope as a anonymous child.
        // In case when TextBoxContent is not an anonymouse child this member is null.
        private FrameworkElement _textBoxContentHost;

        // Encapsulated control that holds/implements our TextContainer.
        private FrameworkElement _renderScope;

        // ScrollViewer
        private ScrollViewer _scrollViewer;

        /// When TextEditor fires a TextChanged event, listeners may want to use the event to
        /// update their Undo/Redo UI.  But the undo stack hasn't yet been modified by the event,
        /// so querying that stack won't give us the information we need to report correctly.
        /// TextBoxBase therefore caches the UndoAction here, so that CanUndo can reference it
        /// and make the right determination.
        private UndoAction _pendingUndoAction;

        // Part name used in the style. The class TemplatePartAttribute should use the same name
        internal const string ContentHostTemplateName = "PART_ContentHost";

        #endregion Private Fields
    }
}

