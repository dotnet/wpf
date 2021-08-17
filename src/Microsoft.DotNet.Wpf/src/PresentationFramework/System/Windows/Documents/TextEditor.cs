// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable 1634, 1691 // To enable presharp warning disables (#pragma suppress) below.
//
// Description: Text editing service for controls.
//

namespace System.Windows.Documents
{
    using MS.Internal;
    using System.Globalization;
    using System.Threading;
    using System.ComponentModel;
    using System.Text;
    using System.Collections; // ArrayList
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Windows.Threading;
    using System.Windows.Input;
    using System.Windows.Controls; // ScrollChangedEventArgs
    using System.Windows.Controls.Primitives;  // CharacterCasing, TextBoxBase
    using System.Windows.Media;
    using System.Windows.Markup;

    using MS.Utility;
    using MS.Win32;
    using MS.Internal.Documents;
    using MS.Internal.Commands; // CommandHelpers

    /// <summary>
    /// Text editing service for controls.
    /// </summary>
    internal class TextEditor
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Initialize the TextEditor
        /// </summary>
        /// <param name="textContainer">
        /// TextContainer representing a content to edit.
        /// </param>
        /// <param name="uiScope">
        /// FrameworkElement on which all events for the user interaction will be
        /// processed.
        /// </param>
        /// <param name="isUndoEnabled">
        /// If true the TextEditor will enable undo support
        /// </param>
        internal TextEditor(ITextContainer textContainer, FrameworkElement uiScope, bool isUndoEnabled)
        {
            // Validate parameters
            Invariant.Assert(uiScope != null);

            // Set non-zero property defaults.
            _acceptsRichContent = true;

            // Attach the editor  instance to the scope
            _textContainer = textContainer;
            _uiScope = uiScope;

            // Enable undo manager for this uiScope
            if (isUndoEnabled && _textContainer is TextContainer)
            {
                ((TextContainer)_textContainer).EnableUndo(_uiScope);
            }

            // Create TextSelection and link it to text container
            _selection = new TextSelection(this);
            textContainer.TextSelection = _selection;

            // Create DragDropProcess
            //  Consider creating this object by demand. Avoid allocating this memory per each textbox
            // or at least make it global, because anyway, dragdrop process is only one in the system.
            _dragDropProcess = new TextEditorDragDrop._DragDropProcess(this);

            // By default we use IBeam cursor
            _cursor = Cursors.IBeam;

            // Add InputLanguageChanged event handler
            TextEditorTyping._AddInputLanguageChangedEventHandler(this);

            // Listen to both TextContainer.EndChanging and TextContainer.Changed events
            TextContainer.Changed += new TextContainerChangedEventHandler(OnTextContainerChanged);

            // Add IsEnabled event handler for cleaning the caret element when uiScope is disabled
            _uiScope.IsEnabledChanged += new DependencyPropertyChangedEventHandler(OnIsEnabledChanged);

            // Attach this instance of text editor to its uiScope
            _uiScope.SetValue(TextEditor.InstanceProperty, this);

            // The IsSpellerEnabled property might have been set before this
            // TextEditor was instantiated -- check if we need to rev
            // up speller support.
            if ((bool)_uiScope.GetValue(SpellCheck.IsEnabledProperty))
            {
                SetSpellCheckEnabled(true);
                SetCustomDictionaries(true);
            }

            // If no IME/TextServices are installed, we have no native reasources
            // to clean up at Finalizer.
            if (!TextServicesLoader.ServicesInstalled)
            {
                GC.SuppressFinalize(this);
            }
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Finalizer
        //
        //------------------------------------------------------

        #region Finalizer

        /// <summary>
        /// The Finalizer will release the resources that were not released earlier.
        /// </summary>
        ~TextEditor()
        {
            // Detach TextStore that TextStore will be unregisted from Cicero.
            // And clean all reference of the native resources.
            DetachTextStore(true /* finalizer */);
        }

        #endregion Finalizer

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Notification that the EditBehavior is being removed from the
        /// scope to which it was attached.
        /// </summary>
        /// <remarks>
        /// innternal - ta make it accessible from TextEditor class.
        /// </remarks>
        internal void OnDetach()
        {
            Invariant.Assert(_textContainer != null);

            // Make sure the speller is shut down.
            SetSpellCheckEnabled(false);

            // Delete UndoManager
            UndoManager undoManager = UndoManager.GetUndoManager(_uiScope);
            if(undoManager != null)
            {
                if (_textContainer is TextContainer)
                {
                    ((TextContainer)_textContainer).DisableUndo(_uiScope);
                }
                else
                {
                    UndoManager.DetachUndoManager(_uiScope);
                }
            }

            // Release TextContainer
            _textContainer.TextSelection = null;

            // Remove InputLanguageChanged event handler
            TextEditorTyping._RemoveInputLanguageChangedEventHandler(this);

            // Remove both TextContainer.Changed event handlers
            _textContainer.Changed -= new TextContainerChangedEventHandler(OnTextContainerChanged);

            // Remove IsEnabled event handler that use for cleaning the caret element when uiScope is disabled
            _uiScope.IsEnabledChanged -= new DependencyPropertyChangedEventHandler(OnIsEnabledChanged);

            // Cancel any pending InitTextStore callback that might still
            // be in the queue.
            _pendingTextStoreInit = false;

            // Shut down the Cicero.
            DetachTextStore(false /* finalizer */);

            // Shut down IMM32.
            if (_immCompositionForDetach != null)
            {
                ImmComposition immComposition;
                if (_immCompositionForDetach.TryGetTarget(out immComposition))
                {
                    // _immComposition comes from getting of the focus on the editor with the enabled IMM.
                    // _immComposition.OnDetach will remove the events handler and then detach editor.
                    immComposition.OnDetach(this);
                }
                _immComposition = null;
                _immCompositionForDetach = null;
            }

            // detach fromm textview
            this.TextView = null;

            // Delete selection object, caret and highlight
            _selection.OnDetach();
            _selection = null;

            _uiScope.ClearValue(TextEditor.InstanceProperty);
            _uiScope = null;

            _textContainer = null;
        }

        /// <summary>
        /// We don't need TextStore after Dispatcher is disposed.
        /// DetachTextStore is called from Finalizer or UICntext.Dispose event callback.
        /// Finalizer calls this to release Cicero's resources. Then we don't need
        /// a call back from UIContex.Dispose any more. And we can release _weakThis.
        /// </summary>
        private void DetachTextStore(bool finalizer)
        {
            // We don't need this TextStore any more.
            // TextStore needs to be unregisted from Cicero so clean all reference
            // of the native resources.
            if (_textstore != null)
            {
                _textstore.OnDetach(finalizer);
                _textstore = null;
            }

            if (_weakThis != null)
            {
                _weakThis.StopListening();
                _weakThis = null;
            }

            if (!finalizer)
            {
                // Cicero's resources have been released.
                // We don't have to get Finalizer called now.
                GC.SuppressFinalize(this);
            }
        }

        // Worker method for set_IsSpellCheckEnabled.
        // Note that enabling the spell checker is also gated on the IsReadOnly
        // and IsEnabled properties of the current UiScope.
        internal void SetSpellCheckEnabled(bool value)
        {
            value = value && !this.IsReadOnly && this._IsEnabled;

            if (value && _speller == null)
            {
                // Start up the speller.
                _speller = new Speller(this);
            }
            else if (!value && _speller != null)
            {
                // Shut down the speller.
                _speller.Detach();
                _speller = null;
            }
        }


        /// <summary>
        /// Loads custom dictionaries
        /// </summary>
        /// <param name="dictionaryLocations"></param>
        /// <returns></returns>
        internal void SetCustomDictionaries(bool add)
        {
            TextBoxBase textBoxBase = _uiScope as TextBoxBase;
            // We want CustomDictionaries to take effect only on TextBoxBase derived classes.
            if (textBoxBase == null)
            {
                return;
            }

            if (_speller != null)
            {
                CustomDictionarySources dictionarySources = (CustomDictionarySources)SpellCheck.GetCustomDictionaries(textBoxBase);
                _speller.SetCustomDictionaries(dictionarySources, add);
            }
        }

        // Forwards a spelling reform property change off to the speller.
        internal void SetSpellingReform(SpellingReform spellingReform)
        {
            if (_speller != null)
            {
                _speller.SetSpellingReform(spellingReform);
            }
        }

        // Queries a FrameworkElement for its TextView
        internal static ITextView GetTextView(UIElement scope)
        {
            IServiceProvider serviceProvider = scope as IServiceProvider;

            return (serviceProvider != null) ? serviceProvider.GetService(typeof(ITextView)) as ITextView : null;
        }

        // Maps a FrameworkElement to its TextSelection, if any.
        internal static ITextSelection GetTextSelection(FrameworkElement frameworkElement)
        {
            TextEditor textEditor = TextEditor._GetTextEditor(frameworkElement);

            return (textEditor == null) ? null : textEditor.Selection;
        }

        // Registers all text editing command handlers for a given control type
        //
        // If registerEventListeners is false, caller is responsible for calling OnXXXEvent methods on TextEditor from
        // UIElement and FrameworkElement virtual overrides (piggy backing on the
        // UIElement/FrameworkElement class listeners).  If true, TextEditor will register
        // its own class listener for events it needs.
        //
        // This method will always register private command listeners.
        internal static void RegisterCommandHandlers(Type controlType, bool acceptsRichContent, bool readOnly, bool registerEventListeners)
        {
            // Check if we already registered handlers for this type
            Invariant.Assert(_registeredEditingTypes != null);
            lock (_registeredEditingTypes)
            {
                for (int i = 0; i < _registeredEditingTypes.Count; i++)
                {
                    // If controlType is or derives from some already registered class - we are done
                    if (((Type)_registeredEditingTypes[i]).IsAssignableFrom(controlType))
                    {
                        return;
                    }

                    // Check if controlType is not a superclass of some registered class.
                    // This is erroneus condition, which must be avoided.
                    // Otherwise the same handlers will be attached to some class twice.
                    if (controlType.IsAssignableFrom((Type)_registeredEditingTypes[i]))
                    {
                        throw new InvalidOperationException(
                            SR.Get(SRID.TextEditorCanNotRegisterCommandHandler, ((Type)_registeredEditingTypes[i]).Name, controlType.Name));
                    }
                }

                // The class was not yet registered. Add it to the list before starting registering handlers.
                _registeredEditingTypes.Add(controlType);
            }

            // Mouse
            TextEditorMouse._RegisterClassHandlers(controlType, registerEventListeners);
            if (!readOnly)
            {
                // Typing
                TextEditorTyping._RegisterClassHandlers(controlType, registerEventListeners);
            }
            // Drag-and-drop
            TextEditorDragDrop._RegisterClassHandlers(controlType, readOnly, registerEventListeners);
            // Cut-Copy-Paste
            TextEditorCopyPaste._RegisterClassHandlers(controlType, acceptsRichContent, readOnly, registerEventListeners);
            // Selection Commands
            TextEditorSelection._RegisterClassHandlers(controlType, registerEventListeners);
            if (!readOnly)
            {
                // Paragraph Formatting
                TextEditorParagraphs._RegisterClassHandlers(controlType, acceptsRichContent, registerEventListeners);
            }
            // ContextMenu
            TextEditorContextMenu._RegisterClassHandlers(controlType, registerEventListeners);
            if (!readOnly)
            {
                // Spelling
                TextEditorSpelling._RegisterClassHandlers(controlType, registerEventListeners);
            }

            if (acceptsRichContent && !readOnly)
            {
                // Character Formatting
                TextEditorCharacters._RegisterClassHandlers(controlType, registerEventListeners);
                // Editing Commands: List Editing
                TextEditorLists._RegisterClassHandlers(controlType, registerEventListeners);
                // Editing Commands: Table Editing
                if (_isTableEditingEnabled)
                {
                    TextEditorTables._RegisterClassHandlers(controlType, registerEventListeners);
                }
            }

            // Focus
            // -----
            if (registerEventListeners)
            {
                EventManager.RegisterClassHandler(controlType, Keyboard.GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(OnGotKeyboardFocus));
                EventManager.RegisterClassHandler(controlType, Keyboard.LostKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(OnLostKeyboardFocus));
                EventManager.RegisterClassHandler(controlType, UIElement.LostFocusEvent, new RoutedEventHandler(OnLostFocus));
            }

            // Undo-Redo
            // ---------
            if (!readOnly)
            {
                CommandHelpers.RegisterCommandHandler(controlType, ApplicationCommands.Undo, new ExecutedRoutedEventHandler(OnUndo), new CanExecuteRoutedEventHandler(OnQueryStatusUndo), KeyGesture.CreateFromResourceStrings(KeyUndo, SR.Get(SRID.KeyUndoDisplayString)), KeyGesture.CreateFromResourceStrings(KeyAltUndo, SR.Get(SRID.KeyAltUndoDisplayString)));
                CommandHelpers.RegisterCommandHandler(controlType, ApplicationCommands.Redo, new ExecutedRoutedEventHandler(OnRedo), new CanExecuteRoutedEventHandler(OnQueryStatusRedo), KeyGesture.CreateFromResourceStrings(KeyRedo, SRID.KeyRedoDisplayString));
            }
        }

        // Worker for TextBox/RichTextBox.GetSpellingErrorAtPosition.
        internal SpellingError GetSpellingErrorAtPosition(ITextPointer position, LogicalDirection direction)
        {
            return TextEditorSpelling.GetSpellingErrorAtPosition(this, position, direction);
        }

        // Returns the error (if any) at the current selection.
        internal SpellingError GetSpellingErrorAtSelection()
        {
            return TextEditorSpelling.GetSpellingErrorAtSelection(this);
        }

        // Worker for TextBox/RichTextBox.GetNextSpellingErrorPosition.
        internal ITextPointer GetNextSpellingErrorPosition(ITextPointer position, LogicalDirection direction)
        {
            return TextEditorSpelling.GetNextSpellingErrorPosition(this, position, direction);
        }

        // Replaces a TextRange's content with a string.
        // Applies LanguageProperty based on current input language.
        // consider renaming this SetCulturedText(CultureInfo, string)
        // and moving it to TextRange.
        internal void SetText(ITextRange range, string text, CultureInfo cultureInfo)
        {
            // Input text
            range.Text = text;

            // mark the range with the current input language on the start position.
            if (range is TextRange)
            {
                MarkCultureProperty((TextRange)range, cultureInfo);
            }
        }

        // Replaces the current selection with a string.
        // Applies any springloaded properties to the text.
        // Applies LanguageProperty based on current input language.
        // Clears the cached caret X offset.
        // consider renaming this SetCulturedText(CultureInfo, string)
        // and moving it to TextSelection.
        internal void SetSelectedText(string text, CultureInfo cultureInfo)
        {
            // Insert the text and tag it with culture property.
            SetText(this.Selection, text, cultureInfo);

            // Apply springload formatting
            ((TextSelection)this.Selection).ApplySpringloadFormatting();

            // Forget previously suggested caret horizontal position.
            TextEditorSelection._ClearSuggestedX(this);
        }

        /// <summary>
        /// Used for marking the span of incoming text with input language
        /// based on the current input language. The default input language is
        /// designated in FrameworkElement.LanguageProperty which has the
        /// default value of "en-US" but this can be changed at any tree node
        /// by xml:lang attribute
        /// </summary>
        internal void MarkCultureProperty(TextRange range, CultureInfo inputCultureInfo)
        {
            //  This method needs some clean-up. It may be perf problem because of repeated element applying - markup fragmentation etc.
            Invariant.Assert(this.UiScope != null);

            if (!this.AcceptsRichContent)
            {
                return;
            }

            // Get the current culture infomation to mark the input culture information
            XmlLanguage language = (XmlLanguage)((ITextPointer)range.Start).GetValue(FrameworkElement.LanguageProperty);

            Invariant.Assert(language != null);

            // Compare the culture info between the current position and the input culture.
            // Set the input culture info if the current has the different culture info with input.
            if (!String.Equals(inputCultureInfo.IetfLanguageTag, language.IetfLanguageTag, StringComparison.OrdinalIgnoreCase))
            {
                range.ApplyPropertyValue(FrameworkElement.LanguageProperty, XmlLanguage.GetLanguage(inputCultureInfo.IetfLanguageTag));
            }

            // Get the input language's flow direction
            FlowDirection inputFlowDirection;
            if (inputCultureInfo.TextInfo.IsRightToLeft)
            {
                inputFlowDirection = FlowDirection.RightToLeft;
            }
            else
            {
                inputFlowDirection = FlowDirection.LeftToRight;
            }

            // Get the current flow direction
            FlowDirection currentFlowDirection = (FlowDirection)((ITextPointer)range.Start).GetValue(FrameworkElement.FlowDirectionProperty);

            // Set the FlowDirection property properly if the input language's flow direction
            // doesn't match with the current flow direction.
            if (currentFlowDirection != inputFlowDirection)
            {
                range.ApplyPropertyValue(FrameworkElement.FlowDirectionProperty, inputFlowDirection);
            }
        }

        internal void RequestExtendSelection(Point point)
        {
            if (_mouseSelectionState == null)
            {
                _mouseSelectionState = new MouseSelectionState();
                _mouseSelectionState.Timer = new DispatcherTimer(DispatcherPriority.Normal);
                _mouseSelectionState.Timer.Tick += new EventHandler(HandleMouseSelectionTick);
                // 400ms is the default value for MenuShowDelay. Creating timer with smaller value may
                // cause Dispatcher queue starvation.
                _mouseSelectionState.Timer.Interval = TimeSpan.FromMilliseconds(Math.Max(SystemParameters.MenuShowDelay, 200));
                _mouseSelectionState.Timer.Start();
                _mouseSelectionState.Point = point;
                // Simulate the first Tick
                HandleMouseSelectionTick(_mouseSelectionState.Timer, EventArgs.Empty);
            }
            else
            {
                _mouseSelectionState.Point = point;
            }
        }

        internal void CancelExtendSelection()
        {
            if (_mouseSelectionState != null)
            {
                _mouseSelectionState.Timer.Stop();
                _mouseSelectionState.Timer.Tick -= new EventHandler(HandleMouseSelectionTick);
                _mouseSelectionState = null;
            }
        }

        /// <summary>
        /// Helper used to check if UiScope has a ToolTip which is open. If so, this method closes the tool tip.
        /// KeyDown and MouseDown event handlers use this helper to check for this condition.
        /// </summary>
        internal void CloseToolTip()
        {
            PopupControlService.Current.DismissToolTipsForOwner(_uiScope);
        }

        /// <summary>
        /// Undo worker.
        /// </summary>
        internal void Undo()
        {
            TextEditorTyping._FlushPendingInputItems(this);

            // Complete the composition string before undo
            CompleteComposition();

            _undoState = UndoState.Undo;

            bool forceLayoutUpdate = this.Selection.CoversEntireContent;

            try
            {
                // REVIEW:benwest: Ideally all the code below should shrink down
                // to:
                //
                // undoManager.Undo(1);
                //
                // The BeginChangeNoUndo, ClearSuggestedX, etc.
                // all belong inside in UndoManager.Undo.

                _selection.BeginChangeNoUndo();
                try
                {
                    UndoManager undoManager = _GetUndoManager();
                    if (undoManager != null && undoManager.UndoCount > undoManager.MinUndoStackCount)
                    {
                        undoManager.Undo(1);
                    }

                    // Forget previously suggested horizontal position
                    // _suggestedX should be part of undo record
                    TextEditorSelection._ClearSuggestedX(this);

                    // Break typing merge for undo
                    TextEditorTyping._BreakTypingSequence(this);

                    // Clear springload formatting
                    if (_selection is TextSelection)
                    {
                        ((TextSelection)_selection).ClearSpringloadFormatting();
                    }
                }
                finally
                {
                    _selection.EndChange();
                }
            }
            finally
            {
                _undoState = UndoState.Normal;
            }

            // If we replaced the entire document content, background layout will
            // kick in.  Force it to complete now.
            if (forceLayoutUpdate)
            {
                this.Selection.ValidateLayout();
            }
        }

        /// <summary>
        /// Redo worker.
        /// </summary>
        internal void Redo()
        {
            TextEditorTyping._FlushPendingInputItems(this);

            _undoState = UndoState.Redo;

            bool forceLayoutUpdate = this.Selection.CoversEntireContent;

            try
            {
                _selection.BeginChangeNoUndo();
                try
                {
                    UndoManager undoManager = _GetUndoManager();
                    if (undoManager != null && undoManager.RedoCount > 0)
                    {
                        undoManager.Redo(1);
                    }

                    // Forget previously suggested horizontal position
                    // _suggestedX should be part of undo record
                    TextEditorSelection._ClearSuggestedX(this);

                    // Break typing merge for undo
                    TextEditorTyping._BreakTypingSequence(this);

                    // Clear springload formatting
                    if (_selection is TextSelection)
                    {
                        ((TextSelection)_selection).ClearSpringloadFormatting();
                    }
                }
                finally
                {
                    _selection.EndChange();
                }
            }
            finally
            {
                _undoState = UndoState.Normal;
            }

            // If we replaced the entire document content, background layout will
            // kick in.  Force it to complete now.
            if (forceLayoutUpdate)
            {
                this.Selection.ValidateLayout();
            }
        }

        internal void OnPreviewKeyDown(KeyEventArgs e)
        {
            TextEditorTyping.OnPreviewKeyDown(_uiScope, e);
        }

        internal void OnKeyDown(KeyEventArgs e)
        {
            TextEditorTyping.OnKeyDown(_uiScope, e);
        }

        internal void OnKeyUp(KeyEventArgs e)
        {
            TextEditorTyping.OnKeyUp(_uiScope, e);
        }

        internal void OnTextInput(TextCompositionEventArgs e)
        {
            TextEditorTyping.OnTextInput(_uiScope, e);
        }

        internal void OnMouseDown(MouseButtonEventArgs e)
        {
            TextEditorMouse.OnMouseDown(_uiScope, e);
        }

        internal void OnMouseMove(MouseEventArgs e)
        {
            TextEditorMouse.OnMouseMove(_uiScope, e);
        }

        internal void OnMouseUp(MouseButtonEventArgs e)
        {
            TextEditorMouse.OnMouseUp(_uiScope, e);
        }

        internal void OnQueryCursor(QueryCursorEventArgs e)
        {
            TextEditorMouse.OnQueryCursor(_uiScope, e);
        }

        internal void OnQueryContinueDrag(QueryContinueDragEventArgs e)
        {
            TextEditorDragDrop.OnQueryContinueDrag(_uiScope, e);
        }

        internal void OnGiveFeedback(GiveFeedbackEventArgs e)
        {
            TextEditorDragDrop.OnGiveFeedback(_uiScope, e);
        }

        internal void OnDragEnter(DragEventArgs e)
        {
            TextEditorDragDrop.OnDragEnter(_uiScope, e);
        }

        internal void OnDragOver(DragEventArgs e)
        {
            TextEditorDragDrop.OnDragOver(_uiScope, e);
        }

        internal void OnDragLeave(DragEventArgs e)
        {
            TextEditorDragDrop.OnDragLeave(_uiScope, e);
        }

        internal void OnDrop(DragEventArgs e)
        {
            TextEditorDragDrop.OnDrop(_uiScope, e);
        }

        internal void OnContextMenuOpening(ContextMenuEventArgs e)
        {
            TextEditorContextMenu.OnContextMenuOpening(_uiScope, e);
        }

        internal void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            OnGotKeyboardFocus(_uiScope, e);
        }

        internal void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            OnLostKeyboardFocus(_uiScope, e);
        }

        internal void OnLostFocus(RoutedEventArgs e)
        {
            OnLostFocus(_uiScope, e);
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        //......................................................
        //
        //  Dependency Properties
        //
        //......................................................

        #region Dependency Properties

        /// <summary>
        /// IsReadOnly attached property speficies if the content within a scope
        /// of some FrameworkElement is editable.
        /// </summary>
        internal static readonly DependencyProperty IsReadOnlyProperty =
                DependencyProperty.RegisterAttached(
                        "IsReadOnly",
                        typeof(bool),
                        typeof(TextEditor),
                        new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.Inherits,
                                new PropertyChangedCallback(OnIsReadOnlyChanged)));

        /// <summary>
        /// TextEditor.AllowOvertype property controls how TextEditor treats INS key.
        /// When set to true INS key toggles overtype mode.
        /// Otherwise it is ignored.
        /// </summary>
        /// <remarks>
        /// Choose a name of the property.
        /// Make this property public.
        /// Decide how to treat "false" value (INS as paste?)
        /// </remarks>
        internal static readonly DependencyProperty AllowOvertypeProperty =
                DependencyProperty.RegisterAttached(
                        "AllowOvertype",
                        typeof(bool),
                        typeof(TextEditor),
                        new FrameworkPropertyMetadata(true));

        /// <summary>
        /// TextEditor.PageHeight attached property for pageup/down
        /// </summary>
        internal static readonly DependencyProperty PageHeightProperty =
                DependencyProperty.RegisterAttached(
                        "PageHeight",
                        typeof(double),
                        typeof(TextEditor),
                        new FrameworkPropertyMetadata(0d));

        #endregion Dependency Properties

        //......................................................
        //
        //  Properties - Relations With Other Components
        //
        //......................................................

        #region Properties - Relations With Other Components

        // The content TextContainer.
        internal ITextContainer TextContainer
        {
            get
            {
                return _textContainer;
            }
        }

        /// <summary>
        /// A FrameworkElement to which this instance on TextEditor
        /// is attached.
        /// </summary>
        /// <value></value>
        internal FrameworkElement UiScope
        {
            get { return _uiScope; }
        }

        /// <summary>
        /// A FrameworkElement to which this instance on TextEditor
        /// is attached.
        /// </summary>
        /// <value></value>
        internal ITextView TextView
        {
            get
            {
                return _textView;
            }
            set
            {
                if (value != _textView)
                {
                    if (_textView != null)
                    {
                        // Remove layout updated handler.
                        _textView.Updated -= new EventHandler(OnTextViewUpdated);

                        _textView = null;

                        // Make sure that caret is destroyed for this text view (if any)
                        // This must be called after clearing _textView
                        _selection.UpdateCaretAndHighlight();
                    }

                    if (value != null)
                    {
                        _textView = value;

                        // Init a layout invalidation listener.
                        _textView.Updated += new EventHandler(OnTextViewUpdated);

                        // Make sure that caret is present for this text view
                        _selection.UpdateCaretAndHighlight();
                    }
                }
            }
        }

        /// <summary>
        /// The TextSelection associated with this TextEditor.
        /// </summary>
        internal ITextSelection Selection
        {
            get { return _selection; }
        }

        // TextStore - needed in TextSelection to notify it about movements
        //  Instead TextStore should subscribe for TextSelection Moved event
        internal TextStore TextStore
        {
            get { return _textstore; }
        }

        /// <summary>
        /// ImmComposition implementation, used when _immEnabled.
        /// </summary>
        internal ImmComposition ImmComposition
        {
            get
            {
                return _immEnabled ? _immComposition : null;
            }
        }

        #endregion Properties - Rlations With Other Components

        //......................................................
        //
        //  Properties - Text Editor Behavior Parameterization
        //
        //......................................................

        #region Properties - Text Editor Behavior Parameterization

        /// <summary>
        /// If true, the TextEditor will accept the return/enter key,
        /// otherwise it will be ignored.  Default is true.
        /// </summary>
        internal bool AcceptsReturn
        {
            get
            {
                return _uiScope == null ? true : (bool)_uiScope.GetValue(KeyboardNavigation.AcceptsReturnProperty);
            }
        }

        /// <summary>
        /// If true, the TextEditor will accept the tab key, otherwise
        /// it will be ignored.  Default is true.
        /// </summary>
        internal bool AcceptsTab
        {
            get
            {
                return _uiScope == null ? true : (bool)_uiScope.GetValue(TextBoxBase.AcceptsTabProperty);
            }
            set
            {
                Invariant.Assert(_uiScope != null);
                if (AcceptsTab != value)
                {
                    _uiScope.SetValue(TextBoxBase.AcceptsTabProperty, value);
                }
            }
        }

        /// <summary>
        /// If true, text selection will be enabled but the TextEditor will
        /// not modify content.  Default is false.
        /// </summary>
        /// <remarks>
        /// Use TextSelection.HideCaret to stop the caret from rendering.
        /// </remarks>
        internal bool IsReadOnly
        {
            get
            {
                // We use local flag _isReadOnly for masking inheritable setting of IsReadOnly
                if (_isReadOnly)
                {
                    return true;
                }
                else
                {
                    return _uiScope == null ? false : (bool)_uiScope.GetValue(TextEditor.IsReadOnlyProperty);
                }
            }
            set
            {
                // This setting does not affect logical tree setting;
                // it only applies to this particular editor.
                // Nested editors be in editable or non-editable state
                // independently on this flag.
                _isReadOnly = value;
            }
        }

        /// <summary>
        /// Enables and disables spell checking on the document.
        /// </summary>
        /// <remarks>
        /// Defaults to false.
        /// </remarks>
        internal bool IsSpellCheckEnabled
        {
            get
            {
                return _uiScope == null ? false : (bool)_uiScope.GetValue(SpellCheck.IsEnabledProperty);
            }
            set
            {
                Invariant.Assert(_uiScope != null);
                _uiScope.SetValue(SpellCheck.IsEnabledProperty, value);
            }
        }

        /// <summary>
        /// If true, the TextEditor will accept xml markup for paragraphs and inline formatting.
        /// Default is true.
        /// </summary>
        internal bool AcceptsRichContent
        {
            get
            {
                return _acceptsRichContent;
            }
            set
            {
                _acceptsRichContent = value;
            }
        }

        /// <summary>
        /// Clr accessor to AllowOvertypeProperty.
        /// </summary>
        internal bool AllowOvertype
        {
            get
            {
                return _uiScope == null ? true : (bool)_uiScope.GetValue(TextEditor.AllowOvertypeProperty);
            }
        }

        /// <summary>
        /// Maximum length of text being edited (_selection.Text).  More precisely, the
        /// user is not allowed to input text beyond this length.
        /// Default is 0, which means unlimited.
        /// </summary>
        internal int MaxLength
        {
            get
            {
                return _uiScope == null ? 0 : (int)_uiScope.GetValue(TextBox.MaxLengthProperty);
            }
        }

        /// <summary>
        /// Controls whether input text is converted to upper or lower case.
        /// Default is CharacterCasing.Normal, which causes no conversion.
        /// </summary>
        internal CharacterCasing CharacterCasing
        {
            get
            {
                return _uiScope == null ? CharacterCasing.Normal : (CharacterCasing)_uiScope.GetValue(TextBox.CharacterCasingProperty);
            }
        }

        /// <summary>
        /// Controls whether heuristics for selecting whole words on mouse drag are active
        /// Default is false.
        /// </summary>
        internal bool AutoWordSelection
        {
            get
            {
                return _uiScope == null ? false : (bool)_uiScope.GetValue(RichTextBox.AutoWordSelectionProperty);
            }
        }

        /// <summary>
        /// Controls whether or not the caret is visible when the text editor is read-only.
        /// Default is false.
        /// </summary>
        internal bool IsReadOnlyCaretVisible
        {
            get
            {
                return _uiScope == null ? false : (bool)_uiScope.GetValue(TextBoxBase.IsReadOnlyCaretVisibleProperty);
            }
        }

        #endregion Properties - Text Editor Behavior Parameterization

        // A property exposing our current undo context.
        //
        // UndoState.Undo while inside OnUndo or
        // UndoState.Redo while inside OnRedo or
        // UndoState.Normal otherwise.
        internal UndoState UndoState
        {
            get
            {
                return _undoState;
            }
        }

        // Flag that indicates whether the UiScope has a context menu open.
        internal bool IsContextMenuOpen
        {
            get
            {
                return _isContextMenuOpen;
            }

            set
            {
                _isContextMenuOpen = value;
            }
        }

        // Speller instance for TextEditorSpelling.
        internal Speller Speller
        {
            get
            {
                return _speller;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Class Internal Methods
        //
        //------------------------------------------------------

        #region Class Internal Methods

        // Maps a FrameworkElement to its TextEditor, if any.
        internal static TextEditor _GetTextEditor(object element)
        {
            return (element is DependencyObject) ? (((DependencyObject)element).ReadLocalValue(InstanceProperty) as TextEditor) : null;
        }

        /// <summary>
        /// Returns the UndoManager, if any, associated with this editor instance.
        /// </summary>
        internal UndoManager _GetUndoManager()
        {
            UndoManager undoManager = null;

            if (this.TextContainer is TextContainer)
            {
                undoManager = ((TextContainer)this.TextContainer).UndoManager;
            }

            return undoManager;
        }

        /// <summary>
        /// Filter input text based on MaxLength, and CharacterCasing.
        /// </summary>
        /// <param name="textData">
        /// text to filter
        /// </param>
        /// <param name="range">
        /// target range to be inserted or replaced
        /// </param>
        /// <returns>
        /// filtered text
        /// </returns>
        internal string _FilterText(string textData, ITextRange range)
        {
            return _FilterText(textData, range.Start.GetOffsetToPosition(range.End));
        }

        internal string _FilterText(string textData, int charsToReplaceCount)
        {
            return _FilterText(textData, charsToReplaceCount, true);
        }

        internal string _FilterText(string textData, ITextRange range, bool filterMaxLength)
        {
            return _FilterText(textData, range.Start.GetOffsetToPosition(range.End), filterMaxLength);
        }

        internal string _FilterText(string textData, int charsToReplaceCount, bool filterMaxLength)
        {
            // We only filter text for plain text content
            if (!this.AcceptsRichContent)
            {
                if (filterMaxLength && this.MaxLength > 0)
                {
                    ITextContainer textContainer = this.TextContainer;
                    int currentLength = textContainer.SymbolCount - charsToReplaceCount;

                    int extraCharsAllowed = Math.Max(0, this.MaxLength - currentLength);

                    // Is there room to insert text?
                    if (extraCharsAllowed == 0)
                    {
                        return string.Empty;
                    }

                    // Does textData length exceed allowed char length?
                    if (textData.Length > extraCharsAllowed)
                    {
                        int splitPosition = extraCharsAllowed;
                        if (IsBadSplitPosition(textData, splitPosition))
                        {
                            splitPosition--;
                        }
                        textData = textData.Substring(0, splitPosition);
                    }

                    // Is there room for low surrogate?
                    if (textData.Length == extraCharsAllowed && Char.IsHighSurrogate(textData, extraCharsAllowed - 1))
                    {
                        textData = textData.Substring(0, extraCharsAllowed-1);
                    }
                    // Does the starting low surrogate have a matching high surrogate in the previously inserted content?
                    if (!string.IsNullOrEmpty(textData) && Char.IsLowSurrogate(textData, 0))
                    {
                        string textAdjacent = textContainer.TextSelection.AnchorPosition.GetTextInRun(LogicalDirection.Backward);
                        if (string.IsNullOrEmpty(textAdjacent) || !Char.IsHighSurrogate(textAdjacent, textAdjacent.Length - 1))
                        {
                            return string.Empty;
                        }
                    }
                }

                if (string.IsNullOrEmpty(textData))
                {
                    return textData;
                }

                if (this.CharacterCasing == CharacterCasing.Upper)
                {
                    // Get CultureInfo from the current input language for ToUpper/ToLower.
                    textData = textData.ToUpper(InputLanguageManager.Current.CurrentInputLanguage);
                }
                else if (this.CharacterCasing == CharacterCasing.Lower)
                {
                    // Get CultureInfo from the current input language for ToUpper/ToLower.
                    textData = textData.ToLower(InputLanguageManager.Current.CurrentInputLanguage);
                }

                if (!this.AcceptsReturn)
                {
                    int endOfFirstLine = textData.IndexOf(Environment.NewLine, StringComparison.Ordinal);
                    if (endOfFirstLine >= 0)
                    {
                        textData = textData.Substring(0, endOfFirstLine);
                    }
                    endOfFirstLine = textData.IndexOfAny(TextPointerBase.NextLineCharacters);
                    if (endOfFirstLine >= 0)
                    {
                        textData = textData.Substring(0, endOfFirstLine);
                    }
                }

                if (!this.AcceptsTab)
                {
                    textData = textData.Replace('\t', ' ');
                }
            }

            return textData;
        }

        // This checks if the source is in UiScope or its style.
        // The key events or text input events should not be handled if the source is out side of
        // element (or style).
        // For example, when focus elment is UiScope's context menu,
        // TextEditor should ignore those events.
        internal bool _IsSourceInScope(object source)
        {
            if (source == this.UiScope)
            {
                return true;
            }

            if ((source is FrameworkElement) && ((FrameworkElement)source).TemplatedParent == this.UiScope)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Complete the composition string.
        /// </summary>
        internal void CompleteComposition()
        {
            if (TextStore != null)
            {
                TextStore.CompleteComposition();
            }

            if (ImmComposition != null)
            {
                ImmComposition.CompleteComposition();
            }
        }

        #endregion Class Internal Methods

        //------------------------------------------------------
        //
        //  Class Internal Properties
        //
        //------------------------------------------------------

        #region Class Internal Properties

        /// <summary>
        /// Returns true if the IsEnabled ptorperty is set to true for ui scope of the editor.
        /// </summary>
        internal bool _IsEnabled
        {
            get
            {
                return _uiScope == null ? false : _uiScope.IsEnabled;
            }
        }

        internal bool _OvertypeMode
        {
            get
            {
                return _overtypeMode;
            }
            set
            {
                _overtypeMode = value;
            }
        }

        // Find the scroller from the render scope of TextEdior
        internal FrameworkElement _Scroller
        {
            get
            {
                FrameworkElement scroller = this.TextView == null ? null : (this.TextView.RenderScope as FrameworkElement);

                while (scroller != null && scroller != this.UiScope)
                {
                    scroller = FrameworkElement.GetFrameworkParent(scroller) as FrameworkElement;

                    if (scroller is ScrollViewer || scroller is ScrollContentPresenter)
                    {
                        return scroller;
                    }
                }

                return null;
            }
        }

        // TLS for TextEditor and dependent classes.
        //
        // Note we demand allocate, but then never clear the TLS slot.
        // This means we will leak one TextEditorThreadLocalStore per
        // thread if TextEditors are allocated then freed on the thread.
        // The alternative, ref counting the TLS, would require a finalizer
        // which is at least as expensive as one object per thread, and
        // much more complicated.
        internal static TextEditorThreadLocalStore _ThreadLocalStore
        {
            get
            {
                TextEditorThreadLocalStore store;

                store = (TextEditorThreadLocalStore)Thread.GetData(_threadLocalStoreSlot);

                if (store == null)
                {
                    store = new TextEditorThreadLocalStore();
                    Thread.SetData(_threadLocalStoreSlot, store);
                }

                return store;
            }
        }

        // Content change counter
        internal long _ContentChangeCounter
        {
            get
            {
                return _contentChangeCounter;
            }
        }

        // Enables or disables table editing commands.
        // False by default, only enabled via reflection for lexicon.exe in V1.
        internal static bool IsTableEditingEnabled
        {
            get
            {
                return _isTableEditingEnabled;
            }

            set
            {
                _isTableEditingEnabled = value;
            }
        }

        // Cached position used to restore selection moving position
        // after colliding with document start or end handling
        // SelectUp/DownByLine commands.
        internal ITextPointer _NextLineAdvanceMovingPosition
        {
            get
            {
                return _nextLineAdvanceMovingPosition;
            }

            set
            {
                _nextLineAdvanceMovingPosition = value;
            }
        }

        // If true _nextLineAdvanceMovingPosition represents a position at the head
        // of the document (stored in response to a OnSelectUpByLineCommand).  Otherwise,
        // _nextLineAdvanceMovingPosition represents a position at the tail
        // of the document (stored in response to a OnSelectDownByLineCommand).
        internal bool _IsNextLineAdvanceMovingPositionAtDocumentHead
        {
            get
            {
                return _isNextLineAdvanceMovingPositionAtDocumentHead;
            }

            set
            {
                _isNextLineAdvanceMovingPositionAtDocumentHead = value;
            }
        }

        #endregion Class Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        //......................................................
        //
        //  Misceleneous
        //
        //......................................................

        /// <summary>
        /// Helper for _FilterText().
        /// Inspects if text[position-1] and text[position] form a surrogate pair or Environment.NewLine.
        /// <param name="text">Input string to inspect.</param>
        /// <param name="position">Split position.</param>
        /// <returns>True if bad split position, false otherwise.</returns>
        /// </summary>
        private bool IsBadSplitPosition(string text, int position)
        {
            //Need to handle more cases for international input such as combining, GB18030, composite characters.
            if ((text[position - 1] == '\r' && text[position] == '\n')
                ||
                (Char.IsHighSurrogate(text, position - 1) && Char.IsLowSurrogate(text, position)))
            {
                return true;
            }

            return false;
        }

        private void HandleMouseSelectionTick(object sender, EventArgs e)
        {
            if (_mouseSelectionState != null && !_mouseSelectionState.BringIntoViewInProgress &&
                this.TextView != null && this.TextView.IsValid && TextEditorSelection.IsPaginated(this.TextView))
            {
                _mouseSelectionState.BringIntoViewInProgress = true;
                this.TextView.BringPointIntoViewCompleted += new BringPointIntoViewCompletedEventHandler(HandleBringPointIntoViewCompleted);
                this.TextView.BringPointIntoViewAsync(_mouseSelectionState.Point, this);
            }
        }

        /// <summary>
        /// Handler for ITextView.BringPointIntoViewCompleted event.
        /// </summary>
        private void HandleBringPointIntoViewCompleted(object sender, BringPointIntoViewCompletedEventArgs e)
        {
            ITextPointer cursorPosition;
            Rect lastCharacterRect;

            Invariant.Assert(sender is ITextView);
            ((ITextView)sender).BringPointIntoViewCompleted -= new BringPointIntoViewCompletedEventHandler(HandleBringPointIntoViewCompleted);

            // If the mouse selection state is not available, it means that the mouse was
            // already released. It may happen when there is delay in view transitions
            // (i.e. page transition in DocumentViewerBase).
            // In such case ignore this event.
            if (_mouseSelectionState == null)
            {
                return;
            }
            _mouseSelectionState.BringIntoViewInProgress = false;

            if (e != null && !e.Cancelled && e.Error == null)
            {
                Invariant.Assert(e.UserState == this && this.TextView == sender);

                cursorPosition = e.Position;

                if (cursorPosition != null)
                {
                    // Check end-of-container condition
                    if (cursorPosition.GetNextInsertionPosition(LogicalDirection.Forward) == null &&
                        cursorPosition.ParentType != null) //  This check is a work around of bug that Parent can be null for some text boxes.
                    {
                        // We are at the end of text container. Check whether mouse is farther than a last character
                        lastCharacterRect = cursorPosition.GetCharacterRect(LogicalDirection.Backward);
                        if (e.Point.X > lastCharacterRect.X + lastCharacterRect.Width)
                        {
                            cursorPosition = this.TextContainer.End;
                        }
                    }

                    // Move the caret/selection to match the cursor position.
                    this.Selection.ExtendSelectionByMouse(cursorPosition, _forceWordSelection, _forceParagraphSelection);
                }
                else
                {
                    CancelExtendSelection();
                }
            }
            else
            {
                CancelExtendSelection();
            }
        }

        // This method is called asynchronously after the first layout update.
        private object InitTextStore(object o)
        {
            // We might have been detached before this callback got dispatched.
            if (!_pendingTextStoreInit)
            {
                return null;
            }

            // Init a TSF TextStore if any TIPs/IMEs are installed.
            if (_textContainer is TextContainer && TextServicesHost.Current != null)
            {
                // We need to make sure we get back a valid thread manager first since it's possible for
                // TextServicesLoader.ServicesInstalled to return true without TSF being usable.
                UnsafeNativeMethods.ITfThreadMgr threadManager = TextServicesLoader.Load();
                if (threadManager != null)
                {
                    // Demand create the TextStore.
                    if (_textstore == null)
                    {
                        _textstore = new TextStore(this);
                        _weakThis = new TextEditorShutDownListener(this);
                    }

                    _textstore.OnAttach();
                    Marshal.ReleaseComObject(threadManager);
                }
            }

            _pendingTextStoreInit = false;

            return null;
        }


        // ................................................................
        //
        // Event Handlers: Internal Events
        //
        // ................................................................

        /// <summary>
        /// Handler for TextContainer.Changed event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTextContainerChanged(object sender, TextContainerChangedEventArgs e)
        {
            // Set short short-term dirty indicator to true.
            // The indicator is used in TextEditorMouse.MoveFocusToUiScope to check
            // that there is no side effects happened in content during focus movement
            this._contentChangeCounter++;
        }

        // TextView.Updated event handler.
        private void OnTextViewUpdated(object sender, EventArgs e)
        {
            // The TextSelection needs to know about the change now.
            _selection.OnTextViewUpdated();

            // The TextView calls this method synchronously, before it finishes its Arrange
            // pass, so defer the remaining work until the TextView is valid.
            this.UiScope.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new DispatcherOperationCallback(OnTextViewUpdatedWorker), EventArgs.Empty);

            if (!_textStoreInitStarted)
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(InitTextStore), null);
                _pendingTextStoreInit = true;
                _textStoreInitStarted = true;
            }
        }

        // Responds to OnTextViewUpdated calls.
        private object OnTextViewUpdatedWorker(object o)
        {
            // Ignore the event if the editor has been detached from its scope
            if (this.TextView == null)
            {
                return null;
            }

            if (_textstore != null)
            {
                _textstore.OnLayoutUpdated();
            }

            // IMM32's OnLostFocus handler. Clean the composition string if it exists.
            if (_immEnabled)
            {
                if (_immComposition != null)
                {
                    _immComposition.OnLayoutUpdated();
                }
            }

            return null;
        }

        // IsEnabledChanged event handler for cleaning the caret element when uiScope is disabled.
        private static void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            TextEditor This = TextEditor._GetTextEditor((FrameworkElement)sender);

            if (This == null)
            {
                return;
            }

            This._selection.UpdateCaretAndHighlight();

            // Update the speller checker status.
            This.SetSpellCheckEnabled(This.IsSpellCheckEnabled);
            This.SetCustomDictionaries(This.IsSpellCheckEnabled);
        }

        // Callback for chagnes to the IsReadOnly property.
        private static void OnIsReadOnlyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement frameworkElement = sender as FrameworkElement;
            if (frameworkElement == null)
            {
                return;
            }

            TextEditor This = TextEditor._GetTextEditor(frameworkElement);
            if (This == null)
            {
                return;
            }

            // Update the spell check status.
            This.SetSpellCheckEnabled(This.IsSpellCheckEnabled);

            // Finalize any active IME composition when transitioning to true.
            if ((bool)e.NewValue == true && This._textstore != null)
            {
                This._textstore.CompleteCompositionAsync();
            }
        }

        // ................................................................
        //
        // Focus
        //
        // ................................................................

        // GotKeyboardFocusEvent handler.
        private static void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            // Ignore the event if the sender is not new focus element.
            if (sender != e.NewFocus)
            {
                return;
            }

            TextEditor This = TextEditor._GetTextEditor((FrameworkElement)sender);

            if (This == null)
            {
                return;
            }

            // Ignore the event if the editor has been detached from its scope
            // or if the element getting focus isn't our uiScope (in which case, it's our child)
            if (!This._IsEnabled)
            {
                return;
            }

            // Cicero's OnGotKeyboardFocus handler. It updates the focus DIM.
            if (This._textstore != null)
            {
                This._textstore.OnGotFocus();
            }

            // IMM32's OnGotFocus handler. Ready for the composition string.
            if (_immEnabled)
            {
                This._immComposition = ImmComposition.GetImmComposition(This._uiScope);

                if (This._immComposition != null)
                {
                    This._immCompositionForDetach = new WeakReference<ImmComposition>(This._immComposition);
                    This._immComposition.OnGotFocus(This);
                }
                else
                {
                    This._immCompositionForDetach = null;
                }
            }

            // Redraw the caret to show the BiDi/normal caret.
            This._selection.RefreshCaret();

            // Make selection visible
            // Note: Do not scroll to bring caret into view upon GotKeyboardFocus.
            This._selection.UpdateCaretAndHighlight();
        }

        // LostKeyboardFocusEvent handler
        //
        // Stop the caret from blinking
        private static void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            // Ignore the event if the sender is not old focus element.
            if (sender != e.OldFocus)
            {
                return;
            }

            TextEditor This = TextEditor._GetTextEditor((FrameworkElement)sender);

            if (This == null)
            {
                return;
            }

            // Ignore the event if the editor has been detached from its scope
            // or the element losing focus isn't our uiScope (in which case, it's our child)
            if (!This._IsEnabled)
            {
                return;
            }

            // Note: Do not scroll to bring caret into view upon LostKeyboardFocus.
            This._selection.UpdateCaretAndHighlight();

            // Call the TextStore's OnLostfocus handler.  Finalizes the curernt composition, if any.
            if (This._textstore != null)
            {
                This._textstore.OnLostFocus();
            }

            // IMM32's OnLostFocus handler. Clean the composition string if it exists.
            if (_immEnabled)
            {
                if (This._immComposition != null)
                {
                    // Call ImmComposition OnLostFocus to clean up the event handler(SelectionChanged).
                    This._immComposition.OnLostFocus();

                    // Set _immComposition as null not to access it until get new from the getting focus.
                    This._immComposition = null;
                }
            }
        }

        // LostFocusedElementEvent handler.
        private static void OnLostFocus(object sender, RoutedEventArgs e)
        {
            TextEditor This = TextEditor._GetTextEditor((FrameworkElement)sender);

            if (This == null)
            {
                return;
            }

            // Un-vanish the cursor.
            TextEditorTyping._ShowCursor();

            // Ignore the event if the editor has been detached from its scope
            // or the element losing focus isn't our uiScope (in which case, it's our child)
            if (!This._IsEnabled)
            {
                return;
            }

            // Flush input queue and complete typing undo unit
            TextEditorTyping._FlushPendingInputItems(This);
            TextEditorTyping._BreakTypingSequence(This);

            // Release column resizing adorner, and interrupt table resising process (if any)
            if (This._tableColResizeInfo != null)
            {
                This._tableColResizeInfo.DisposeAdorner();
                This._tableColResizeInfo = null;
            }

            // Hide selection
            This._selection.UpdateCaretAndHighlight();
        }

        // ................................................................
        //
        // Undo-Redo
        //
        // ................................................................

        /// <summary>
        /// Undo command event handler.
        /// </summary>
        private static void OnUndo(object target, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor((FrameworkElement)target);

            if (This == null)
            {
                return;
            }

            // Ignore the event if the editor has been detached from its scope
            if (!This._IsEnabled)
            {
                return;
            }

            if (This.IsReadOnly)
            {
                return;
            }

            This.Undo();
        }

        /// <summary>
        ///     Redo command event handler.
        /// </summary>
        private static void OnRedo(object target, ExecutedRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor((FrameworkElement)target);

            if (This == null)
            {
                return;
            }

            // Ignore the event if the editor has been detached from its scope
            if (!This._IsEnabled)
            {
                return;
            }

            if (This.IsReadOnly)
            {
                return;
            }

            This.Redo();
        }

        /// <summary>
        /// Undo command QueryStatus handler.
        /// </summary>
        private static void OnQueryStatusUndo(object sender, CanExecuteRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor((FrameworkElement)sender);

            if (This == null)
            {
                return;
            }

            UndoManager undoManager = This._GetUndoManager();

            if (undoManager != null && undoManager.UndoCount > undoManager.MinUndoStackCount)
            {
                args.CanExecute = true;
            }
        }

        /// <summary>
        /// Redo command QueryStatus handler.
        /// </summary>
        private static void OnQueryStatusRedo(object sender, CanExecuteRoutedEventArgs args)
        {
            TextEditor This = TextEditor._GetTextEditor((FrameworkElement)sender);

            if (This == null)
            {
                return;
            }

            UndoManager undoManager = This._GetUndoManager();

            if (undoManager != null && undoManager.RedoCount > 0)
            {
                args.CanExecute = true;
            }
        }

        #endregion Private methods

        //------------------------------------------------------
        //
        //  Private Types
        //
        //------------------------------------------------------

        #region Private Types

        // We need to an event handler for Dispatcher's Dispose but we don't want to have
        // a strong referrence fromDispatcher. So TextEditorShutDownListener wraps this.
        private sealed class TextEditorShutDownListener : ShutDownListener
        {
            public TextEditorShutDownListener(TextEditor target)
                : base(target, ShutDownEvents.DomainUnload | ShutDownEvents.DispatcherShutdown)
            {
            }

            internal override void OnShutDown(object target, object sender, EventArgs e)
            {
                TextEditor editor = (TextEditor)target;
                editor.DetachTextStore(false /* finalizer */);
            }
        }

        private class MouseSelectionState
        {
            internal DispatcherTimer Timer;
            internal Point Point;
            internal bool BringIntoViewInProgress;
        }

        #endregion Private Types

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Attached property used to map FrameworkElement arguments
        // to command handlers back to TextEditor instances.
        private static readonly DependencyProperty InstanceProperty = DependencyProperty.RegisterAttached( //
            "Instance", typeof(TextEditor), typeof(TextEditor), //
            new FrameworkPropertyMetadata((object)null));

        //  Remove this member
        internal Dispatcher _dispatcher;

        // Read-only setting for this level of an editor.
        // This flag is supposed to mask inheritable IsReadOnly property when set to true.
        // When the flag is false we are supposed to get IsReadOnly from UIScope
        private bool _isReadOnly;

        // Register for classes to which we already added static command handlers
        private static ArrayList _registeredEditingTypes = new ArrayList(4);

        // TextConatiner representing a text subject to editing
        private ITextContainer _textContainer;

        // Content change counter is supposed to be used in various
        // dirty-condition detection scenarios.
        // In particular, it is used in TextEditorMouse.MoveFocusToUiScope to check
        // that there is no side effects happened in content during focus movement
        private long _contentChangeCounter;

        // Control to which this editor is attached
        private FrameworkElement _uiScope;
        private ITextView _textView;

        // TextSelection maintained within this _uiScope
        private ITextSelection _selection;

        // Flag turned true to indicate overtype mode
        private bool _overtypeMode;

        // Preserved horizontal position for verical caret movement
        internal Double _suggestedX;

        // ITextStoreACP implementation, used when text services (IMEs, etc.)
        // are available.
        private TextStore _textstore;

        // We need to an event handler for Dispatcher's Dispose but we don't want to have
        // a strong referrence fromDispatcher. So TextEditorShutDownListener wraps this.
        private TextEditorShutDownListener _weakThis;

        // The speller.  If null, spell check is not enabled.
        private Speller _speller;

        // Flag set true after scheduling a callback to InitTextStore.
        private bool _textStoreInitStarted;

        // If true, we're waiting for the Dispatcher to dispatch a callback
        // to InitTextStore.
        private bool _pendingTextStoreInit;

        // Mouse cursor defined in MouseMove handler to be used in OnQueryCursor method
        internal Cursor _cursor;

        // Merged typing undo unit.
        // Undo unit creation is nontrivial here:
        // It requires a logic for merging consequtive typing.
        // For that purpose we store a unit created by typing and reopen it
        // when next typing occurs.
        // We should however discard this unit each time when selection
        // moves to another position.
        //  could we just check the top of the undo stack
        // for an unlocked TextParentUndoUnit instead of caching here?
        internal IParentUndoUnit _typingUndoUnit;

        // An object for storing dragdrop state during dragging
        internal TextEditorDragDrop._DragDropProcess _dragDropProcess;

        internal bool _forceWordSelection;
        internal bool _forceParagraphSelection;

        // Resizing operation information for table column
        internal TextRangeEditTables.TableColumnResizeInfo _tableColResizeInfo;

        // Tracking whether or not a given change is part of an undo or redo
        private UndoState _undoState = UndoState.Normal;

        // If true, the TextEditor will accept xml markup for paragraphs and inline formatting.
        // Default is true.
        private bool _acceptsRichContent;

        // If the system is IMM enabled, this is true.
        private static bool _immEnabled = SafeSystemMetrics.IsImmEnabled ;

        // ImmComposition implementation, used when _immEnabled.
        private ImmComposition _immComposition;

        // Weak-ref to the most recent ImmComposition - used when detaching
        private WeakReference<ImmComposition> _immCompositionForDetach;

        // Thread local storage for TextEditor and dependent classes.
        private static LocalDataStoreSlot _threadLocalStoreSlot = Thread.AllocateDataSlot();

        // Flag indicating that MouseDown handler is in progress,
        // to ignore all MouseMoves caused by CaptureMouse call.
        internal bool _mouseCapturingInProgress;

        // Mouse selection support.
        private MouseSelectionState _mouseSelectionState;

        // Flag that indicates whether the UiScope has a context menu open.
        // This flag is set/reset in ContextMenuOpening/ContextMenuClosing event handlers
        // respectively in TextEditorContextMenu.cs.
        // TextSelection.UpdateCaretAndHighlight() uses this flag to detect the case
        // when the UiScope has a context menu open.
        private bool _isContextMenuOpen;

        // Enables or disables table editing commands.
        // False by default, only enabled via reflection for lexicon.exe in V1.
        private static bool _isTableEditingEnabled;

        // Cached position used to restore selection moving position
        // after colliding with document start or end handling
        // SelectUp/DownByLine commands.
        private ITextPointer _nextLineAdvanceMovingPosition;

        // If true _nextLineAdvanceMovingPosition represents a position at the head
        // of the document (stored in response to a OnSelectUpByLineCommand).  Otherwise,
        // _nextLineAdvanceMovingPosition represents a position at the tail
        // of the document (stored in response to a OnSelectDownByLineCommand).
        internal bool _isNextLineAdvanceMovingPositionAtDocumentHead;

        #endregion Private Fields

        private const string KeyAltUndo = "Alt+Backspace";
        private const string KeyRedo = "Ctrl+Y";
        private const string KeyUndo = "Ctrl+Z";
    }
}
