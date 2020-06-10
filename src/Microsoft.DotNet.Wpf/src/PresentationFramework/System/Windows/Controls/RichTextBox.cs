// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: The stock rich text editing control.
//

namespace System.Windows.Controls
{
    using MS.Internal;
    using MS.Internal.Documents;
    using System.Windows.Threading;
    using System.Windows.Input; // KeyboardNavigation
    using System.ComponentModel; // DefaultValue
    using System.Windows.Controls.Primitives; // TextBoxBase
    using System.Windows.Documents; // TextEditor
    using System.Windows.Automation.Peers; // AutomationPattern
    using System.Windows.Media; // GlyphRun
    using System.Windows.Markup; // IAddChild
    using System.Collections; // IEnumerator
    using System.Collections.ObjectModel; // ReadOnlyCollection
    using MS.Internal.Automation;     // For TextAdaptor
    using MS.Internal.Controls; // EmptyEnumerator
    using MS.Internal.Telemetry.PresentationFramework;

    /// <summary>
    /// RichTextBox control
    /// </summary>
    [Localizability(LocalizationCategory.Inherit)]
    [ContentProperty("Document")]
    public class RichTextBox : TextBoxBase, IAddChild
    {
        // -----------------------------------------------------------
        //
        // Constructors
        //
        // -----------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Static constructor for RichTextBox.
        /// </summary>
        static RichTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RichTextBox), new FrameworkPropertyMetadata(typeof(RichTextBox)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(RichTextBox));

            // Default value for AcceptsReturn is true
            KeyboardNavigation.AcceptsReturnProperty.OverrideMetadata(typeof(RichTextBox), new FrameworkPropertyMetadata(true));

            // Default value for AutoWordSelection is false.  We want true.
            TextBoxBase.AutoWordSelectionProperty.OverrideMetadata(typeof(RichTextBox), new FrameworkPropertyMetadata(true));

            if (!FrameworkAppContextSwitches.UseAdornerForTextboxSelectionRendering)
            {
                
                // Override the default selection opacity so if FrameworkAppContextSwitches.UseAdornerForTextboxSelectionRendering
                // is false, we still get the appropriate value.
                TextBoxBase.SelectionOpacityProperty.OverrideMetadata(typeof(RichTextBox), new FrameworkPropertyMetadata(TextBoxBase.AdornerSelectionOpacityDefaultValue));
            }

            // We need to transfer all character formatting properties and some behavioral inheriting properties
            // from RichTextBox level into its FlowDocument.
            // For this purpose we set listeners for all these properties:
            HookupInheritablePropertyListeners();

            ControlsTraceLogger.AddControl(TelemetryControls.RichTextBox);
        }

        /// <summary>
        /// Initializes a new instance of RichTextBox control.
        /// </summary>
        /// <remarks>
        /// Creates implicit instance of a FlowDocument as its initial content.
        /// The initial document will contain one Paragraph with an empty Run in it.
        /// </remarks>
        public RichTextBox() 
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of RichTextBox control and specifies a FlowDocument as its content
        /// </summary>
        /// <param name="document">
        /// A FlowDocument specified as a content for this instance of RichTextBox.
        /// </param>
        public RichTextBox(FlowDocument document)
            : base()
        {
            // Register static editing command handlers.
            // This only has an effect that first time we make the call.
            // We don't use the static ctor because there are cases
            // where another control will want to alias our properties
            // but doesn't need this overhead.
            TextEditor.RegisterCommandHandlers(typeof(RichTextBox), /*acceptsRichContent:*/true, /*readOnly*/false, /*registerEventListeners*/false);

            // Create TextContainer and TextEditor associated with it
            if (document == null)
            {
                document = new FlowDocument();
                document.Blocks.Add(new Paragraph());

                // Mark the document as implicit.
                // This flag will affect these behaviors:
                //  a) RichTextBox serialization will not output FlowDocument child if it is implicit and still empty
                //  b) IAddChild will allow adding the first child (which will become non-implicit, and would not allow the subsecuent additions)
                //  c) Property inheritance from RichTextBox to its FlowDocument will work only for implicit document
                // This call must be done after Document assignment, as it always clears the _implicitDocument field.
                _implicitDocument = true;
            }

            // Initialize the Document property.
            // Note that _implicitDocument flag was set to true/false just before that.
            // The peemptive flag setting has its effect only when _document is null (this case only),
            // otherwise the new document would be considered as explicit (so flag would be cleared by the Document property assignment).
            this.Document = document;

            // Values that must be set as a side effect of Document assignment,
            // that are required for the RichTextBox instance functioning.
            Invariant.Assert(this.TextContainer != null);
            Invariant.Assert(this.TextEditor != null);
            Invariant.Assert(this.TextEditor.TextContainer == this.TextContainer);
        }

        #endregion Constructors

        // -----------------------------------------------------------
        //
        // Public Methods
        //
        // -----------------------------------------------------------

        #region Public Methods

        // -----------------------------------------------------------
        //
        // IAddChild interface
        //
        // -----------------------------------------------------------

        ///<summary>
        /// This method is called to Add the object as a child of the RichTextBox.  This method is used primarily
        /// by the parser; a more direct way of adding a child to a RichTextBox is to use the <see cref="Document" />
        /// property.
        ///</summary>
        ///<param name="value">
        /// The object to add as a child; it must be a UIElement.
        ///</param>
        void IAddChild.AddChild(Object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (!(value is FlowDocument))
            {
                throw new ArgumentException(SR.Get(SRID.UnexpectedParameterType, value.GetType(), typeof(FlowDocument)), "value");
            }

            if (!_implicitDocument)
            {
                throw new ArgumentException(SR.Get(SRID.CanOnlyHaveOneChild, this.GetType(), value.GetType()));
            }

            this.Document = (FlowDocument)value;
        }

        ///<summary>
        /// This method is called by the parser when text appears under the tag in markup.
        /// As RichTextBox do not support text, calling this method has no effect if the text
        /// is all whitespace.  For non-whitespace text, throw an exception.
        ///</summary>
        ///<param name="text">
        /// Text to add as a child.
        ///</param> 
        void IAddChild.AddText(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            XamlSerializerUtil.ThrowIfNonWhiteSpaceInAddText(text, this);
        }


        ///<summary>
        /// Returns the TextPointer located closest to a supplied Point.
        /// </summary>
        /// <param name="point">
        /// Point to query, in the coordinate space of the RichTextBox.
        /// </param>
        /// <param name="snapToText">
        /// If true, this method will always return a TextPointer --
        /// the closest position as calculated by the control's heuristics. 
        /// If false, this method will return a null position if the test 
        /// point does not fall within any character bounding box.
        /// </param>
        /// <returns>
        /// The closest TextPointer to the supplied Point, or null if
        /// snapToText is false and the supplied Point is not contained
        /// within any character bounding box, or if no content element yet exists.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        /// Throws InvalidOperationException if layout is dirty.
        /// </exception>
        public TextPointer GetPositionFromPoint(Point point, bool snapToText)
        {
            if (this.RenderScope == null)
            {
                return null;
            }

            return (TextPointer)GetTextPositionFromPointInternal(point, snapToText);
        }

        /// <summary>
        /// Returns the associated SpellingError at a specified position.
        /// </summary>
        /// <param name="position">
        /// Position of text to query.
        /// </param>
        /// <remarks>
        /// The position and its LogicalDirection specify a character to query.
        /// If the specificed character is not part of a misspelled word then
        /// this method will return null.
        /// </remarks>
        public SpellingError GetSpellingError(TextPointer position)
        {
            ValidationHelper.VerifyPosition(this.TextContainer, position);

            return this.TextEditor.GetSpellingErrorAtPosition(position, position.LogicalDirection);
        }

        /// <summary>
        /// Returns the TextRange covering a misspelled word at a specified
        /// position.
        /// </summary>
        /// <param name="position">
        /// Position of text to query.
        /// </param>
        /// <remarks>
        /// The position and its LogicalDirection specify a character to query.
        /// If the specificed character is not part of a misspelled word then
        /// this method will return null.
        /// </remarks>
        public TextRange GetSpellingErrorRange(TextPointer position)
        {
            ValidationHelper.VerifyPosition(this.TextContainer, position);

            SpellingError spellingError = this.TextEditor.GetSpellingErrorAtPosition(position, position.LogicalDirection);

            return (spellingError == null) ? null : new TextRange(spellingError.Start, spellingError.End);
        }

        /// <summary>
        /// Returns the position of the next character in a specificed direction
        /// that is the start of a misspelled word.
        /// </summary>
        /// <param name="position">
        /// Position of text to query.
        /// </param>
        /// <param name="direction">
        /// Direction to query.
        /// </param>
        /// <remarks>
        /// The position and its LogicalDirection specify a character to query.
        /// The search includes the spelling error containing the character
        /// specified by position/direction (if any).
        /// 
        /// If no misspelled word is encountered, the method returns null.
        /// </remarks>
        public TextPointer GetNextSpellingErrorPosition(TextPointer position, LogicalDirection direction)
        {
            ValidationHelper.VerifyPosition(this.TextContainer, position);

            return (TextPointer)this.TextEditor.GetNextSpellingErrorPosition(position, direction);
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer() 
        {
            return new RichTextBoxAutomationPeer(this);
        }

        protected override void OnDpiChanged(DpiScale oldDpiScaleInfo, DpiScale newDpiScaleInfo)
        {
            Document?.SetDpi(newDpiScaleInfo);
        }

        /// <summary>
        /// Measurement override. Implement your size-to-content logic here.
        /// </summary>
        /// <param name="constraint">
        /// Sizing constraint.
        /// </param>
        protected override Size MeasureOverride(Size constraint)
        {
            if (constraint.Width == Double.PositiveInfinity)
            {
                // If we're sized to infinity, we won't behave the same way TextBox does under
                // the same conditions.  So, we fake it.
                constraint.Width = this.MinWidth;
            }
            return base.MeasureOverride(constraint);
        }

        // Allocates the initial render scope for this control.
        internal override FrameworkElement CreateRenderScope()
        {
            FlowDocumentView renderScope = new FlowDocumentView();
            renderScope.Document = this.Document;

            // Set a margin so that the BiDi Or Italic caret has room to render at the edges of content.
            // Otherwise, anti-aliasing or italic causes the caret to be partially clipped.
            renderScope.Document.PagePadding = new Thickness(CaretElement.CaretPaddingWidth, 0, CaretElement.CaretPaddingWidth, 0);

            // We want current style to ignore all properties from theme style for renderScope.
            renderScope.OverridesDefaultStyle = true;

            return renderScope;
        }

        #endregion Protected Methods

        // -----------------------------------------------------------
        //
        // Public Properties
        //
        // -----------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// A Property representing a content of this RichTextBox
        /// </summary>
        public FlowDocument Document
        {
            get
            {
                Invariant.Assert(_document != null);
                return _document;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                if (value != _document &&
                    value.StructuralCache != null && value.StructuralCache.TextContainer != null && 
                    value.StructuralCache.TextContainer.TextSelection != null)
                {
                    throw new ArgumentException(SR.Get(SRID.RichTextBox_DocumentBelongsToAnotherRichTextBoxAlready));
                }

                if (_document != null && this.TextSelectionInternal.ChangeBlockLevel > 0)
                {
                    throw new InvalidOperationException(SR.Get(SRID.RichTextBox_CantSetDocumentInsideChangeBlock));
                }

                if (value == _document)
                {
                    // Same document nothing to do.
                    return;
                }

                // Identify the case for the _document initialization
                bool initialSetting = _document == null;
                
                // Detach existing FlowDocument
                if (_document != null)
                {
                    // Detach PageSize change listener
                    _document.PageSizeChanged -= new EventHandler(this.OnPageSizeChangedHandler);

                    // Remove the document from the logical tree
                    this.RemoveLogicalChild(_document);

                    // Stop collecting text changes
                    _document.TextContainer.CollectTextChanges = false;

                    // Clear thereference to a document
                    _document = null;
                }

                // Clear the implicitDocument flag.
                // Any assignment to the Document property clears the _implicitDocument flag
                // expect for the initial one, which may happen from a RichTextBox constructor which
                // can create an empolicit document and sets _implicitDocument flag before
                // the Document property assignment.
                if (!initialSetting)
                {
                    _implicitDocument = false;
                }

                // Store the document for future use - just for comparing when it changes and detaching from it
                _document = value;
                _document.SetDpi(this.GetDpi());

                // Save existing renderScope before calling TextBoxBase.InitializeTextContainer,
                // because it will discard it.
                UIElement renderScope = this.RenderScope;

                // Start collecting text changes
                _document.TextContainer.CollectTextChanges = true;

                // Attach to new text container and re-create TextEditor instance
                this.InitializeTextContainer(_document.TextContainer);

                // Add listener for PageSize - to redirect it to inner renderScope.Width
                _document.PageSizeChanged += new EventHandler(this.OnPageSizeChangedHandler);

                // Add the document as a child to the logical tree
                this.AddLogicalChild(_document);

                // Re-attach to visual tree
                if (renderScope != null)
                {
                    // Re-atach to visual tree if we have a new TextContainer.
                    this.AttachToVisualTree();
                }

                // Make sure that all inherited properties properly transferred
                // to the new document according to its Standalone/Inherited status.
                TransferInheritedPropertiesToFlowDocument();

                // Raise a TextChanged event for all assignments except initializing one.
                if (!initialSetting)
                {
                    //Re-apply the cached undo properties
                    this.ChangeUndoLimit(this.UndoLimit);
                    this.ChangeUndoEnabled(this.IsUndoEnabled);

                    Invariant.Assert(this.PendingUndoAction == UndoAction.None);
                    this.PendingUndoAction = UndoAction.Clear;
                    try
                    {
                        this.OnTextChanged(new TextChangedEventArgs(TextChangedEvent, UndoAction.Clear));
                    }
                    finally
                    {
                        this.PendingUndoAction = UndoAction.None;
                    }
                }
            }
        }

        /// <summary>
        /// This method is used by TypeDescriptor to determine if this property should
        /// be serialized.
        /// </summary>
        // According to the contract with the serializer the (private) method named as
        // ShouldSerialize<PropertyName> tells the serializer whether it should serialize the
        // property for this instance or not.
        // We want to avoid serializing implicitly created document if it does not have any meaningful content.
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeDocument()
        {
            Block firstBlock = _document.Blocks.FirstBlock;

            if (_implicitDocument &&
                (firstBlock == null
                    ||
                    firstBlock == _document.Blocks.LastBlock &&
                    firstBlock is Paragraph))
            {
                Inline firstInline = (firstBlock == null) ? null : ((Paragraph)firstBlock).Inlines.FirstInline;
                if (firstInline == null
                        ||
                        firstInline == ((Paragraph)firstBlock).Inlines.LastInline &&
                        firstInline is Run &&
                        firstInline.ContentStart.CompareTo(firstInline.ContentEnd) == 0)
                {
                    // We have implicit document without any text content.
                    return false;
                }
            }

            // In all other cases we should serialize the FlowDocument child.
            return true;
        }

        /// <summary>
        /// Enables or disables TextElements and UIElements contained in this RichTextBox's FlowDocument.
        /// </summary>
        /// <remarks>
        /// By default child elements have their IsEnabled property coerced
        /// false when hosted by RichTextBox.  Use this property to enable
        /// contained elements.
        /// </remarks>
        public static readonly DependencyProperty IsDocumentEnabledProperty =
            DependencyProperty.Register("IsDocumentEnabled", typeof(bool), typeof(RichTextBox),
            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnIsDocumentEnabledChanged)));

        /// <summary>
        /// Enables or disables TextElements and UIElements contained in this RichTextBox's FlowDocument.
        /// </summary>
        /// <remarks>
        /// <see cref="IsDocumentEnabledProperty" />
        /// </remarks>
        public bool IsDocumentEnabled
        {
            get
            {
                return (bool)GetValue(IsDocumentEnabledProperty);
            }

            set
            {
                SetValue(IsDocumentEnabledProperty, value);
            }
        }

        // ...........................................................
        //
        // Content Accessing Properties
        //
        // ..........................................................

        #region Content Accessing Properties

        /// <summary>
        /// Returns enumerator to logical children
        /// </summary>
        protected internal override IEnumerator LogicalChildren
        {
            get
            {
                if (this._document == null)
                {
                    // Using _document (not .Document),as it can be null on destruction scenarios 
                    // (even though .Document cannot be null) - it is called for property invalidation reasons...
                    return EmptyEnumerator.Instance;
                }
                else
                {
                    return new SingleChildEnumerator(this._document);
                }
            }
        }

        /// <summary>
        /// Text Selection (readonly)
        /// </summary>
        public TextSelection Selection
        {
            get
            {
                return (TextSelection)TextSelectionInternal;
            }
        }

        /// <summary>
        /// Position of the caret.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TextPointer CaretPosition
        {
            get
            {
                return Selection.MovingPosition;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                if (!Selection.Start.IsInSameDocument(value))
                {
                    throw new ArgumentException(SR.Get(SRID.RichTextBox_PointerNotInSameDocument), "value");
                }
                Selection.SetCaretToPosition(value, value.LogicalDirection, /*allowStopAtLineEnd:*/true, /*allowStopNearSpace:*/false);
            }
        }
        
        #endregion Content Accessing Properties

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Returns the DependencyObjectType for the registered ThemeStyleKey's default 
        // value. Controls will override this method to return approriate types.
        internal override DependencyObjectType DTypeThemeStyleKey
        {
            get
            {
                return _dType;
            }
        }

        #endregion Internal Methods

        // -----------------------------------------------------------
        //
        // Private Methods
        //
        // -----------------------------------------------------------

        #region Private Methods

        // ...........................................................
        //
        // Transferring Properties from RichTextBox down to FlowDocument
        //
        // ...........................................................

        // We need to transfer all character formatting properties from RichTextBox level into its FlowDocument.
        //
        // Note that we have to set all properties explicitly - not even trying
        // to bypass some of them when using assumptions about their default values (for non-implicit document)
        // or a coincidence with the current context values (for implicit document) -
        // the FlowDocument mast have all properties explicitly set to keep them intact
        // after being moved to another contextual location (both in Save-Load and in Print scenarios).

        // For this purpose we set listeners for all these properties:
        private static void HookupInheritablePropertyListeners()
        {
            // All inhgeriting formatting properties need to be transferred over the implicit document boundary.
            // This is required for treating UIContext as default setting for implicit document.
            // Note that mechanism is not applied to explicit document.
            PropertyChangedCallback formattingPropertyCallback = new PropertyChangedCallback(OnFormattingPropertyChanged);
            DependencyProperty[] inheritableFormattingProperties = TextSchema.GetInheritableProperties(typeof(FlowDocument));
            for (int i = 0; i < inheritableFormattingProperties.Length; i++)
            {
                inheritableFormattingProperties[i].OverrideMetadata(typeof(RichTextBox), new FrameworkPropertyMetadata(formattingPropertyCallback));
            }

            // Inheriting behavioral properties need to be transferred over any Standalone document boundary.
            PropertyChangedCallback behavioralPropertyCallback = new PropertyChangedCallback(OnBehavioralPropertyChanged);
            DependencyProperty[] inheritableBehavioralProperties = TextSchema.BehavioralProperties;
            for (int i = 0; i < inheritableBehavioralProperties.Length; i++)
            {
                inheritableBehavioralProperties[i].OverrideMetadata(typeof(RichTextBox), new FrameworkPropertyMetadata(behavioralPropertyCallback));
            }
        }

        // Transfer all properties from the current context to this
        // implicit document - to simulate inheritance.
        // This is how implicit document gets its initial values -
        // from the context.
        // After that RichTextBox will maintain such inheritance simulation
        // (for implicit document only) by listening for property
        // change notifications.
        private void TransferInheritedPropertiesToFlowDocument()
        {
            // Implicit document needs all formatting properties be transferred to it
            if (_implicitDocument)
            {
                DependencyProperty[] inheritableFormattingProperties = TextSchema.GetInheritableProperties(typeof(FlowDocument));
                for (int i = 0; i < inheritableFormattingProperties.Length; i++)
                {
                    DependencyProperty property = inheritableFormattingProperties[i];
                    TransferFormattingProperty(property, this.GetValue(property));
                }
            }

            // Behavioral properties must be transferred to any document whether it has Standalone
            // or Inherited FormattingDefaults. All such values are set as local values even
            // in case when they are equal to the ones inherited from the UI context
            // (see TransferBehavioralProperty method implementation).
            // Such strong logic is needed to work correctly in any sequence of
            // setting FormattingDefaults property and adding children.
            DependencyProperty[] inheritableBehavioralProperties = TextSchema.BehavioralProperties;
            for (int i = 0; i < inheritableBehavioralProperties.Length; i++)
            {
                DependencyProperty property = inheritableBehavioralProperties[i];
                TransferBehavioralProperty(property, this.GetValue(property));
            }
        }

        /// <summary>
        /// Callback for changes to the any text formatting property.
        /// Transfers the new value to explisit setting on a FlowDocument.
        /// </summary>
        private static void OnFormattingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RichTextBox richTextBox = (RichTextBox)d;

            if (richTextBox._implicitDocument)
            {
                richTextBox.TransferFormattingProperty(e.Property, e.NewValue);
            }
        }

        // Transfers single formatting property from this RichTextBox to its implicit document
        private void TransferFormattingProperty(DependencyProperty property, object inheritedValue)
        {
            Invariant.Assert(_implicitDocument, "We only supposed to do this for implicit documents");

            object defaultValue = _document.GetValue(property);
            if (!TextSchema.ValuesAreEqual(inheritedValue, defaultValue))
            {
                _document.ClearValue(property);
                defaultValue = _document.GetValue(property);
                if (!TextSchema.ValuesAreEqual(inheritedValue, defaultValue))
                {
                    _document.SetValue(property, inheritedValue);
                }
            }
        }

        /// <summary>
        /// Callback for changes to the any behavioral property.
        /// Transfers the new value to a FlowDocument.
        /// </summary>
        /// <remarks>
        /// Behavioral properties must be transferred to any document whether it has Standalone
        /// or Inherited FormattingDefaults. All such values are set as local values even
        /// in case when they are equal to the ones inherited from the UI context.
        /// Such strong logic is needed to work correctly in any sequence of
        /// setting FormattingDefaults property and adding children.
        /// </remarks>
        private static void OnBehavioralPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RichTextBox richTextBox = (RichTextBox)d;

            richTextBox.TransferBehavioralProperty(e.Property, e.NewValue);
        }

        /// <summary>
        /// Transfers single behavioral property from this RichTextBox to its implicit document
        /// </summary>
        /// <remarks>
        /// Behavioral properties must be transferred to any document whether it has Standalone
        /// or Inherited FormattingDefaults. All such values are set as local values even
        /// in case when they are equal to the ones inherited from the UI context.
        /// Such strong logic is needed to work correctly in any sequence of
        /// setting FormattingDefaults property and adding children.
        /// </remarks>
        private void TransferBehavioralProperty(DependencyProperty property, object inheritedValue)
        {
            // Set the value unconditionally as explicit local value
            _document.SetValue(property, inheritedValue);
        }

        // ...........................................................
        //
        // TextEditor Parameterization Properties Access
        //
        // ...........................................................

        #region TextEditor Parameterization Propeties Access

        private void OnPageSizeChangedHandler(object sender, EventArgs e)
        {
            if (this.RenderScope == null)
            {
                return;
            }

            // Make sure that the TextWrapping property is set correctly
            if (this.Document != null)
            {
                this.Document.TextWrapping = TextWrapping.Wrap;
            }

            // The Document does not have explicit PageWidth set OR Wrap/WrapWithOverflow is requested.
            // The RenderScope must occupy as much space as its content required (no wrapping)
            // We could set Width to positive infinity, but that would make horizontal scrollbar
            // look wrong. So we need to make sure that render scope has a size of
            // its actual content. For that we clear loal value of Width property
            // from RenderScope.

            // To let RenderScope occupy all space we clear Width local value on it.
            this.RenderScope.ClearValue(FlowDocumentView.WidthProperty);

            // Set alighment to Stretch - for RenderScope to occupy the whole viewport.
            this.RenderScope.ClearValue(FrameworkElement.HorizontalAlignmentProperty);
            // Normally TextBox style does not set any balue for HorizontalAlignment property,
            // so clearing would set it to a required default - Stretch.
            // However, if style author sets some other value - it will be set as a result of ClearValue,
            // so we need to enforce that.
            // Note, that trying to clear first saves a memory for inline property application.
            if (this.RenderScope.HorizontalAlignment != HorizontalAlignment.Stretch)
            {
                this.RenderScope.HorizontalAlignment = HorizontalAlignment.Stretch;
            }
        }


        #endregion TextEditor Parameterization Properties Access

        // Callback for IsDocumentEnabledProperty changes.
        // Forces a coercion of the child FlowDocument, with the new setting.
        private static void OnIsDocumentEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RichTextBox richTextBox = (RichTextBox)d;

            if (richTextBox.Document != null)
            {
                richTextBox.Document.CoerceValue(IsEnabledProperty);
            }
        }

        #endregion Private Methods

        // -----------------------------------------------------------
        //
        // Private Fields
        //
        // -----------------------------------------------------------

        #region Private Fields

        private FlowDocument _document;
        private bool _implicitDocument; // true if Document property was set by AddChild or Document setter (not in constructor)

        private static DependencyObjectType _dType; // Needed for property system optimization

        #endregion Private Fields
    }
}
