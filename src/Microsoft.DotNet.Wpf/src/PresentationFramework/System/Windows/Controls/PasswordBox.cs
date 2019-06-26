// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: The stock password control.
//

using System.Diagnostics;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Security;
using System.Text;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using MS.Internal;
using MS.Internal.KnownBoxes;
using MS.Internal.Telemetry.PresentationFramework;
using System.Windows.Controls.Primitives;

#pragma warning disable 1634, 1691  // suppressing PreSharp warnings

namespace System.Windows.Controls
{
    /// <summary>
    /// The stock password control.
    /// </summary>
#if OLD_AUTOMATION
    [Automation(AccessibilityControlType = "Edit")]
#endif
    [TemplatePart(Name = "PART_ContentHost", Type = typeof(FrameworkElement))]
    public sealed class PasswordBox : Control, ITextBoxViewHost
#if OLD_AUTOMATION
        , IAutomationPatternProvider, IAutomationPropertyProvider
#endif
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Static constructor for PasswordBox.
        /// </summary>
        static PasswordBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PasswordBox), new FrameworkPropertyMetadata(typeof(PasswordBox)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(PasswordBox));

            // PasswordBox properties
            // ------------------
            PasswordCharProperty.OverrideMetadata(typeof(PasswordBox),
                    new FrameworkPropertyMetadata(new PropertyChangedCallback(OnPasswordCharChanged)));

            // Declaree listener for Padding property
            Control.PaddingProperty.OverrideMetadata(typeof(PasswordBox),
                new FrameworkPropertyMetadata(new PropertyChangedCallback(OnPaddingChanged)));

            // Prevent journaling
            NavigationService.NavigationServiceProperty.OverrideMetadata(typeof(PasswordBox), new FrameworkPropertyMetadata(new PropertyChangedCallback(OnParentNavigationServiceChanged)));

            InputMethod.IsInputMethodEnabledProperty.OverrideMetadata(typeof(PasswordBox),
                    new FrameworkPropertyMetadata(
                            BooleanBoxes.FalseBox,
                            FrameworkPropertyMetadataOptions.Inherits,
                            null,
                            // replace this Coerce callback with a
                            // ValidateValue callback when we support VVC on override metadata
                            new CoerceValueCallback(ForceToFalse)));

            // VSM
            IsEnabledProperty.OverrideMetadata(typeof(PasswordBox), new UIPropertyMetadata(new PropertyChangedCallback(OnVisualStatePropertyChanged)));
            IsMouseOverPropertyKey.OverrideMetadata(typeof(PasswordBox), new UIPropertyMetadata(new PropertyChangedCallback(OnVisualStatePropertyChanged)));

            TextBoxBase.SelectionBrushProperty.OverrideMetadata(typeof(PasswordBox),
                new FrameworkPropertyMetadata(new PropertyChangedCallback(UpdateCaretElement)));
            TextBoxBase.SelectionTextBrushProperty.OverrideMetadata(typeof(PasswordBox),
                new FrameworkPropertyMetadata(new PropertyChangedCallback(UpdateCaretElement)));
            TextBoxBase.SelectionOpacityProperty.OverrideMetadata(typeof(PasswordBox), 
                new FrameworkPropertyMetadata(new PropertyChangedCallback(UpdateCaretElement)));
            TextBoxBase.CaretBrushProperty.OverrideMetadata(typeof(PasswordBox),
                new FrameworkPropertyMetadata(new PropertyChangedCallback(UpdateCaretElement)));

            ControlsTraceLogger.AddControl(TelemetryControls.PasswordBox);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public PasswordBox() : base()
        {
            Initialize();
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Replaces the current selection in the passwordbox with the contents
        /// of the Clipboard
        /// </summary>
        public void Paste()
        {
            RoutedCommand command = ApplicationCommands.Paste;
            command.Execute(null, this);
        }

        /// <summary>
        /// Select all text in the PasswordBox
        /// </summary>
        public void SelectAll()
        {
            Selection.Select(TextContainer.Start, TextContainer.End);
        }

        /// <summary>
        /// Clear all the content in the PasswordBox control.
        /// </summary>
        public void Clear()
        {
            this.Password = String.Empty;
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Contents of the PasswordBox.
        /// </summary>
        /// <remarks>
        /// Use the SecurePassword property in place of this one when possible.
        /// Doing so reduces the risk of revealing content that should be kept secret.
        /// </remarks>
        [DefaultValue("")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Password
        {
            get
            {
                string password;

                using (SecureString securePassword = this.SecurePassword)
                {
                    IntPtr ptr = System.Runtime.InteropServices.Marshal.SecureStringToBSTR(securePassword);

                    try
                    {
                        unsafe
                        {
                            password = new string((char*)ptr);
                        }
                    }
                    finally
                    {
                        System.Runtime.InteropServices.Marshal.ZeroFreeBSTR(ptr);
                    }
                }

                return password;
            }

            set
            {
                if (value == null)
                {
                    value = String.Empty;
                }

                using (SecureString securePassword = new SecureString())
                {
                    #pragma warning suppress 6506 // value is set to String.Empty if it was null.
                    for (int i = 0; i < value.Length; i++)
                    {
                        securePassword.AppendChar(value[i]);
                    }

                    SetSecurePassword(securePassword);
                }
            }
        }

        /// <summary>
        /// Contents of the PasswordBox.
        /// </summary>
        /// <remarks>
        /// Reading the Password always returns a copy which may be safely
        /// Disposed.
        ///
        /// Setting the value always stores a copy of the supplied value.
        /// </remarks>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public SecureString SecurePassword
        {
            get
            {
                return this.TextContainer.GetPasswordCopy();
            }
        }

        /// <summary>
        /// The DependencyID for the PasswordChar property.
        /// Default Value:     '*'
        /// </summary>
        public static readonly DependencyProperty PasswordCharProperty =
                DependencyProperty.RegisterAttached(
                        "PasswordChar", // Property name
                        typeof(char), // Property type
                        typeof(PasswordBox), // Property owner
                        new FrameworkPropertyMetadata('*')); // Flags

        /// <summary>
        /// Character to display instead of the actual password.
        /// </summary>
        public char PasswordChar
        {
            get { return (char) GetValue(PasswordCharProperty); }
            set { SetValue(PasswordCharProperty, value); }
        }

        /// <summary>
        /// The limit number of characters that the PasswordBox or other editable controls can contain.
        /// if it is 0, means no-limitation.
        /// User can set this value for some simple single line PasswordBox to restrict the text number.
        /// By default it is 0.
        /// </summary>
        /// <remarks>
        /// When this property is set to zero, the maximum length of the text that can be entered
        /// in the control is limited only by available memory. You can use this property to restrict
        /// the length of text entered in the control for values such as postal codes and telephone numbers.
        /// You can also use this property to restrict the length of text entered when the data is to be entered
        /// in a database.
        /// You can limit the text entered into the control to the maximum length of the corresponding field in the database.
        /// Note:   In code, you can set the value of the Text property to a value that is larger than
        /// the value specified by the MaxLength property.
        /// This property only affects text entered into the control at runtime.
        /// </remarks>
        public static readonly DependencyProperty MaxLengthProperty =
                TextBox.MaxLengthProperty.AddOwner(typeof(PasswordBox));


        /// <summary>
        /// Maximum number of characters the PasswordBox can accept
        /// </summary>
        [DefaultValue((int)0)]
        public int MaxLength
        {
            get { return (int) GetValue(MaxLengthProperty); }
            set { SetValue(MaxLengthProperty, value); }
        }

        /// <summary>
        /// <see cref="TextBoxBase.SelectionBrushProperty"/>
        /// </summary>
        public static readonly DependencyProperty SelectionBrushProperty = 
            TextBoxBase.SelectionBrushProperty.AddOwner(typeof(PasswordBox));

        /// <summary>
        /// <see cref="TextBoxBase.SelectionBrushProperty" />
        /// </summary>
        public Brush SelectionBrush
        {
            get { return (Brush)GetValue(SelectionBrushProperty); }
            set { SetValue(SelectionBrushProperty, value); }
        }

        /// <summary>
        /// <see cref="TextBoxBase.SelectionTextBrushProperty"/>
        /// </summary>
        public static readonly DependencyProperty SelectionTextBrushProperty =
            TextBoxBase.SelectionTextBrushProperty.AddOwner(typeof(PasswordBox));

        /// <summary>
        /// <see cref="TextBoxBase.SelectionTextBrushProperty"/>
        /// </summary>
        public Brush SelectionTextBrush
        {
            get { return (Brush)GetValue(SelectionTextBrushProperty); }
            set { SetValue(SelectionTextBrushProperty, value); }
        }

        /// <summary>
        /// <see cref="TextBoxBase.SelectionOpacityProperty"/>
        /// </summary>
        public static readonly DependencyProperty SelectionOpacityProperty =
            TextBoxBase.SelectionOpacityProperty.AddOwner(typeof(PasswordBox));

        /// <summary>
        /// <see cref="TextBoxBase.SelectionOpacityProperty"/>
        /// </summary>
        public double SelectionOpacity
        {
            get { return (double)GetValue(SelectionOpacityProperty); }
            set { SetValue(SelectionOpacityProperty, value); }
        }

        /// <summary>
        /// <see cref="TextBoxBase.CaretBrushProperty"/>
        /// </summary>
        public static readonly DependencyProperty CaretBrushProperty =
            TextBoxBase.CaretBrushProperty.AddOwner(typeof(PasswordBox));

        /// <summary>
        /// <see cref="CaretBrushProperty" />
        /// </summary>
        public Brush CaretBrush
        {
            get { return (Brush)GetValue(CaretBrushProperty); }
            set { SetValue(CaretBrushProperty, value); }
        }

        /// <summary>
        /// <see cref="TextBoxBase.IsSelectionActiveProperty"/>
        /// </summary>
        public static readonly DependencyProperty IsSelectionActiveProperty =
            TextBoxBase.IsSelectionActiveProperty.AddOwner(typeof(PasswordBox));

        /// <summary>
        /// <see cref="TextBoxBase.IsSelectionActive"/>
        /// </summary>
        public bool IsSelectionActive
        {
            get { return (bool)GetValue(IsSelectionActiveProperty); }
        }

        /// <summary>
        /// <see cref="TextBoxBase.IsInactiveSelectionHighlightEnabledProperty"/>
        /// </summary>
        public static readonly DependencyProperty IsInactiveSelectionHighlightEnabledProperty =
            TextBoxBase.IsInactiveSelectionHighlightEnabledProperty.AddOwner(typeof(PasswordBox));

        /// <summary>
        /// <see cref="TextBoxBase.IsInactiveSelectionHighlightEnabled"/>
        /// </summary>
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
        /// <remarks>
        /// Unlike most RoutedEvents on Controls, PasswordChangedEvent does not
        /// have a matching protected virtual OnPasswordChanged method --
        /// because PasswordBox is sealed.
        /// </remarks>
        public static readonly RoutedEvent PasswordChangedEvent = EventManager.RegisterRoutedEvent(
            "PasswordChanged", // Event name
            RoutingStrategy.Bubble, //
            typeof(RoutedEventHandler), //
            typeof(PasswordBox)); //

        /// <summary>
        /// Event fired from this text box when its inner content
        /// has been changed.
        /// </summary>
        /// <remarks>
        /// It is redirected from inner TextContainer.Changed event.
        /// </remarks>
        public event RoutedEventHandler PasswordChanged
        {
            add
            {
                AddHandler(PasswordChangedEvent, value);
            }

            remove
            {
                RemoveHandler(PasswordChangedEvent, value);
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
            if (!IsEnabled)
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateDisabled, VisualStates.StateNormal);
            }
            else if (IsMouseOver)
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateMouseOver, VisualStates.StateNormal);
            }
            else
            {
                VisualStateManager.GoToState(this, VisualStates.StateNormal, useTransitions);
            }

            if (IsKeyboardFocused)
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateFocused, VisualStates.StateUnfocused);
            }
            else
            {
                VisualStateManager.GoToState(this, VisualStates.StateUnfocused, useTransitions);
            }

            base.ChangeVisualState(useTransitions);
        }

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new PasswordBoxAutomationPeer(this);
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

        ///
        /// <see cref="FrameworkElement.OnPropertyChanged"/>
        ///
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            //  always call base.OnPropertyChanged, otherwise Property Engine will not work.
            base.OnPropertyChanged(e);

            if (this.RenderScope != null)
            {
                FrameworkPropertyMetadata fmetadata = e.Property.GetMetadata(typeof(PasswordBox)) as FrameworkPropertyMetadata;
                if (fmetadata != null)
                {
                    if (e.IsAValueChange || e.IsASubPropertyChange)
                    {
                        if (fmetadata.AffectsMeasure || fmetadata.AffectsArrange ||
                            fmetadata.AffectsParentMeasure || fmetadata.AffectsParentArrange ||
                            e.Property == Control.HorizontalContentAlignmentProperty || e.Property == Control.VerticalContentAlignmentProperty)
                        {
                            ((TextBoxView)this.RenderScope).Remeasure();
                        }
                        else if (fmetadata.AffectsRender &&
                                (e.IsAValueChange || !fmetadata.SubPropertiesDoNotAffectRender))
                        {
                            ((TextBoxView)this.RenderScope).Rerender();
                        }
                    }
                }
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

            _textEditor.OnKeyDown(e);
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

            _textEditor.OnKeyUp(e);
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

            _textEditor.OnTextInput(e);
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

            _textEditor.OnMouseDown(e);
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

            _textEditor.OnMouseMove(e);
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

            _textEditor.OnMouseUp(e);
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

            _textEditor.OnQueryCursor(e);
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

            _textEditor.OnQueryContinueDrag(e);
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

            _textEditor.OnGiveFeedback(e);
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

            _textEditor.OnDragEnter(e);
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

            _textEditor.OnDragOver(e);
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

            _textEditor.OnDragLeave(e);
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

            _textEditor.OnDrop(e);
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

            _textEditor.OnContextMenuOpening(e);
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

            _textEditor.OnGotKeyboardFocus(e);
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

            _textEditor.OnLostKeyboardFocus(e);
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

            _textEditor.OnLostFocus(e);
        }

        #endregion Protected Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        // A TextContainer covering the PasswordBox's inner content.
        internal PasswordTextContainer TextContainer
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

        internal ScrollViewer ScrollViewer
        {
            get
            {
                if (_scrollViewer == null)
                {
                    if (_textEditor != null)
                    {
                        _scrollViewer = _textEditor._Scroller as ScrollViewer;
                    }
                }
                return _scrollViewer;
            }
        }

        // ITextContainer holding the Control content.
        ITextContainer ITextBoxViewHost.TextContainer
        {
            get
            {
                return this.TextContainer;
            }
        }

        // Set true when typography property values are all default values.
        bool ITextBoxViewHost.IsTypographyDefaultValue
        {
            get
            {
                return true;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // Worker for the ctors, initializes a new PasswordBox instance.
        private void Initialize()
        {
            // Register static editing command handlers.
            // This only has an effect that first time we make the call.
            // We don't use the static ctor because there are cases
            // where another control will want to alias our properties
            // but doesn't need this overhead.
            TextEditor.RegisterCommandHandlers(typeof(PasswordBox), /*acceptsRichContent:*/false, /*readOnly*/false, /*registerEventListeners*/false);

            // Create TextContainer
            InitializeTextContainer(new PasswordTextContainer(this));

            // PasswordBox only accepts plain text, so change TextEditor's default to that.
            _textEditor.AcceptsRichContent = false;

            // PasswordBox does not accetps tabs.
            _textEditor.AcceptsTab = false;
        }

        // Attaches this control to a new TextContainer.
        private void InitializeTextContainer(PasswordTextContainer textContainer)
        {
            Invariant.Assert(textContainer != null);

            // Uninitialize previous TextEditor
            if (_textContainer != null)
            {
                Invariant.Assert(_textEditor != null);
                Invariant.Assert(_textEditor.TextContainer == _textContainer);

                // Detach existing editor from VisualTree
                DetachFromVisualTree();

                // Discard TextEditor - must release text container
                _textEditor.OnDetach();
            }

            // Save text container
            _textContainer = textContainer;

            // Use static class handler
            ((ITextContainer)_textContainer).Changed += new TextContainerChangedEventHandler(OnTextContainerChanged);

            // Create a text editor, initialize undo manager for it, and link it to text container
            _textEditor = new TextEditor(_textContainer, this, true);
        }

        // Disable IME input unconditionally.  We really can't support
        // IMEs in this control, because PasswordTextContainer doesn't
        // round-trip content, which breaks the cicero contract.
        // Additionally, from a UI standpoint, we don't want to break
        // user expectations by allowing IME input.  IMEs do cool things
        // like learn from user corrections, so the same keystroke sequence
        // might produce different text over time.
        private static object ForceToFalse(DependencyObject d, object value)
        {
            return BooleanBoxes.FalseBox;
        }

        /// <summary>
        /// Callback for changes to the PasswordChar property.
        /// </summary>
        private static void OnPasswordCharChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PasswordBox passwordBox = (PasswordBox)d;

            // Force a layout refresh to display the new char.
            if (passwordBox._renderScope != null)
            {
                passwordBox._renderScope.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Handler for text array change notifications.
        /// </summary>
        /// <param name="sender">
        /// sender
        /// </param>
        /// <param name="e">
        /// event args
        /// </param>
        private void OnTextContainerChanged(object sender, TextContainerChangedEventArgs e)
        {
            // If only properties on the text changed, don't fire a content change event.
            // This can happen even in a plain text TextBox if we switch logical trees.
            if (!e.HasContentAddedOrRemoved)
            {
                return;
            }

            RaiseEvent(new RoutedEventArgs(PasswordChangedEvent));
        }

        /// <summary>
        /// Walk the visual tree until we find a Text control with IsPasswordBoxContent == true
        /// and a ScrollViewer
        /// </summary>
        private void SetRenderScopeToContentHost(TextBoxView renderScope)
        {
            // Clear the content host from previous render scope (if any)
            ClearContentHost();

            // Find ContentHostTemplateName in the style
            _passwordBoxContentHost = GetTemplateChild(ContentHostTemplateName) as FrameworkElement;

            // Note that we allow ContentHostTemplateName to be optional.
            // This simplifies toolability of our control styling.
            // When the ContentHostTemplateName is not found or incorrect
            // PasswordBox goes into disabled state, but does not throw.

            // Add renderScope as a child of ContentHostTemplateName
            _renderScope = renderScope;
            if (_passwordBoxContentHost is ScrollViewer)
            {
                ScrollViewer scrollViewer = (ScrollViewer)_passwordBoxContentHost;
                if (scrollViewer.Content != null)
                {
                    throw new NotSupportedException(SR.Get(SRID.TextBoxScrollViewerMarkedAsTextBoxContentMustHaveNoContent));
                }
                else
                {
                    scrollViewer.Content = _renderScope;
                }
            }
            else if (_passwordBoxContentHost is Decorator)
            {
                Decorator decorator = (Decorator)_passwordBoxContentHost;
                if (decorator.Child != null)
                {
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
                if (_passwordBoxContentHost != null)
                {
                    _passwordBoxContentHost = null;
                    //  Remove the exception
                    throw new NotSupportedException(SR.Get(SRID.PasswordBoxInvalidTextContainer));
                }
            }

            // Attach render scope to TextEditor
            InitializeRenderScope();

            FrameworkElement element = _renderScope;
            while (element != this && element != null)  // checking both just to be safe
            {
                if (element is Border)
                {
                    _border = (Border)element;
                }
                element = element.Parent as FrameworkElement;
            }
        }

        private void ClearContentHost()
        {
            // Detach render scope from TextEditor
            UninitializeRenderScope();

            // Render scope has been created by us,
            // so we need to extract if from visual tree.
            if (_passwordBoxContentHost is ScrollViewer)
            {
                ((ScrollViewer)_passwordBoxContentHost).Content = null;
            }
            else if (_passwordBoxContentHost is Decorator)
            {
                ((Decorator)_passwordBoxContentHost).Child = null;
            }
            else
            {
                Invariant.Assert(_passwordBoxContentHost == null, "_passwordBoxContentHost must be null here");
            }

            _passwordBoxContentHost = null;
        }

        // Initializes a new render scope.
        private void InitializeRenderScope()
        {
            if (_renderScope == null)
            {
                return;
            }

            // Attach the renderScope to TextEditor as its ITextView member.
            ITextView textview = TextEditor.GetTextView(_renderScope);
            _textEditor.TextView = textview;
            this.TextContainer.TextView = textview;

            if (this.ScrollViewer != null)
            {
                this.ScrollViewer.CanContentScroll = true;
            }
        }

        // Uninitializes a render scope and clears this control's reference.
        private void UninitializeRenderScope()
        {
            // Clear TextView property in TextEditor
            _textEditor.TextView = null;
        }

        // Resets the selection to the start of the content.
        // Called after non-TOM changes to the content, like
        // set_Text
        private void ResetSelection()
        {
            Select(0, 0);

            if (this.ScrollViewer != null)
            {
                this.ScrollViewer.ScrollToHome();
            }
        }

        /// <summary>
        /// Select the text in the given position and length.
        /// </summary>
        private void Select(int start, int length)
        {
            ITextPointer selectionStart;
            ITextPointer selectionEnd;

            //             VerifyAccess();
            if (start < 0)
            {
                throw new ArgumentOutOfRangeException("start", SR.Get(SRID.ParameterCannotBeNegative));
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length", SR.Get(SRID.ParameterCannotBeNegative));
            }

            // Identify new selection start position
            selectionStart = this.TextContainer.Start.CreatePointer();
            while (start-- > 0 && selectionStart.MoveToNextInsertionPosition(LogicalDirection.Forward))
            {
            }

            // Identify new selection end position
            selectionEnd = selectionStart.CreatePointer();
            while (length-- > 0 && selectionEnd.MoveToNextInsertionPosition(LogicalDirection.Forward))
            {
            }

            Selection.Select(selectionStart, selectionEnd);
        }

        /// <summary>
        /// Callback for TextBox.Padding property setting
        /// </summary>
        /// <param name="d">
        /// TextBoxBase on which the property is changed
        /// </param>
        /// <param name="e">event args</param>
        private static void OnPaddingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PasswordBox passwordBox = (PasswordBox)d;

            if (passwordBox.ScrollViewer != null)
            {
                // translate this change into inner property set on ScrollViewer
                object padding = passwordBox.GetValue(Control.PaddingProperty);
                if (padding is Thickness)
                {
                    passwordBox.ScrollViewer.Padding = (Thickness)padding;
                }
                else
                {
                    passwordBox.ScrollViewer.ClearValue(Control.PaddingProperty);
                }
            }
        }

        // Set up listener for Navigating event
        private static void OnParentNavigationServiceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            PasswordBox passwordBox = (PasswordBox)o;
            NavigationService navService = NavigationService.GetNavigationService(o);
            if (passwordBox._navigationService != null)
            {
                passwordBox._navigationService.Navigating -= new NavigatingCancelEventHandler(passwordBox.OnNavigating);
            }
            if (navService != null)
            {
                navService.Navigating += new NavigatingCancelEventHandler(passwordBox.OnNavigating);
                passwordBox._navigationService = navService;
            }
            else
            {
                passwordBox._navigationService = null;
            }
        }

        // Clear password on navigation to prevent journaling.
        private void OnNavigating(Object sender, NavigatingCancelEventArgs e)
        {
            this.Password = String.Empty;
        }

        /// <summary>
        /// Detaches the editor from old visual tree and attaches it to a new one
        /// </summary>
        private void AttachToVisualTree()
        {
            DetachFromVisualTree();

            // Walk the visual tree to find our Text element
            SetRenderScopeToContentHost(new TextBoxView(this));

            // Attach scroll handler to the new scroll viewer
            // Note that this.ScrollViewer will walk the tree from current TextEditor's render scope up to its ui scope.
            if (this.ScrollViewer != null)
            {
                this.ScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
                this.ScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                this.ScrollViewer.Focusable = false;

                if (this.ScrollViewer.Background == null)
                {
                    // prevent hit-testing through padding
                    this.ScrollViewer.Background = Brushes.Transparent;
                }
                OnPaddingChanged(this, new DependencyPropertyChangedEventArgs());
            }

            // Set border properties
            if (_border != null)
            {
                _border.Style = null;
            }
        }

        /// <summary>
        /// Clear our layout-specific data, and detach our current renderScope from our text editor.
        /// </summary>
        private void DetachFromVisualTree()
        {
            if (_textEditor != null)
            {
                _textEditor.Selection.DetachFromVisualTree();
            }

            // Invalidate our cached copy of scroll viewer.
            _scrollViewer = null;
            _border = null;

            ClearContentHost();
        }

        /// <summary>
        /// Sets the content of the control.
        /// </summary>
        private void SetSecurePassword(SecureString value)
        {
            this.TextContainer.BeginChange();
            try
            {
                this.TextContainer.SetPassword(value);
                this.ResetSelection();
            }
            finally
            {
                this.TextContainer.EndChange();
            }
        }

        /// <summary>
        /// PropertyChanged callback for a property that affects the selection or caret rendering.
        /// </summary>
        private static void UpdateCaretElement(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PasswordBox passwordBox = (PasswordBox)d;

            if (passwordBox.Selection != null)
            {
                CaretElement caretElement = passwordBox.Selection.CaretElement;
                if (caretElement != null)
                {
                    if (e.Property == CaretBrushProperty)
                    {
                        caretElement.UpdateCaretBrush(TextSelection.GetCaretBrush(passwordBox.Selection.TextEditor));
                    }

                    caretElement.InvalidateVisual();
                }

                
                // If the TextBox is rendering its own selection we need to invalidate arrange here
                // in order to ensure the selection is updated.
                var textBoxView = passwordBox?.RenderScope as TextBoxView;

                if ((textBoxView as ITextView)?.RendersOwnSelection == true)
                {
                    textBoxView.InvalidateArrange();
                }
            }
        }

        #endregion Private methods

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        #region Private Properties

        /// <summary>
        /// Text Selection (readonly)
        /// </summary>
        private ITextSelection Selection
        {
            get
            {
                return _textEditor.Selection;
            }
        }


        #endregion Private Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // TextEditor working in this control instance
        private TextEditor _textEditor;

        // Backing store for the control's content.
        private PasswordTextContainer _textContainer;

        // Encapsulated control that renders our TextContainer.
        private TextBoxView _renderScope;

        // ScrollViewer
        private ScrollViewer _scrollViewer;

        // Border
        private Border _border;

        // An element marked as ContentHostTemplateName which we assign our _renderScope as a child.
        private FrameworkElement _passwordBoxContentHost;

        // Default size for the control.
        private const int _defaultWidth = 100;
        private const int _defaultHeight = 20;

        // Part name used in the style. The class TemplatePartAttribute should use the same name
        private const string ContentHostTemplateName = "PART_ContentHost";

        #endregion Private Fields

        #region DTypeThemeStyleKey

        // Returns the DependencyObjectType for the registered ThemeStyleKey's default
        // value. Controls will override this method to return approriate types.
        internal override DependencyObjectType DTypeThemeStyleKey
        {
            get { return _dType; }
        }

        private static DependencyObjectType _dType;

        private NavigationService _navigationService;
        #endregion DTypeThemeStyleKey
    }
}
